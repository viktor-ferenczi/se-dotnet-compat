using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.Memory;
using VRage.FileSystem;
using VRageMath;

namespace VRage.Render.Image;

public static class MyImage
{
	public enum FileFormat
	{
		Png,
		Jpg,
		Bmp
	}

	static MyImage()
	{
		Configuration.Default.MemoryAllocator = new SimpleGcMemoryAllocator();
	}

	public static IMyImage Load(Stream stream, bool oneChannel, bool headerOnly = false, string debugName = null)
	{
		IImageInfo imageInfo = SixLabors.ImageSharp.Image.Identify(stream);
		stream.Position = 0L;
		if (!oneChannel)
		{
			oneChannel = imageInfo.MetaData.GetFormatMetaData(PngFormat.Instance).ColorType == PngColorType.Grayscale;
		}
		if (headerOnly)
		{
			if (!oneChannel)
			{
				return MyImage<uint>.Create<Rgba32>(imageInfo);
			}
			switch ((PngBitDepth)(byte)imageInfo.PixelType.BitsPerPixel)
			{
			case PngBitDepth.Bit8:
				return MyImage<byte>.Create<Gray8>(imageInfo);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<Gray16>(imageInfo);
			}
		}
		else if (oneChannel)
		{
			switch ((PngBitDepth)(byte)imageInfo.PixelType.BitsPerPixel)
			{
			case PngBitDepth.Bit8:
				return MyImage<byte>.Create<Gray8>(stream);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<Gray16>(stream);
			}
		}
		else
		{
			PngMetaData formatMetaData = imageInfo.MetaData.GetFormatMetaData(PngFormat.Instance);
			if (formatMetaData.ColorType != PngColorType.Grayscale)
			{
				return MyImage<uint>.Create<Rgba32>(stream);
			}
			switch (formatMetaData.BitDepth)
			{
			case PngBitDepth.Bit8:
				return MyImage<byte>.Create<Gray8>(stream);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<Gray16>(stream);
			}
		}
		return null;
	}

	public unsafe static IMyImage Load(IntPtr pSource, int size, string debugName)
	{
		using UnmanagedMemoryStream stream = new UnmanagedMemoryStream((byte*)pSource.ToPointer(), size);
		return Load(stream, oneChannel: false, headerOnly: false, debugName);
	}

	public static IMyImage Load(string path, bool oneChannel)
	{
		using Stream stream = MyFileSystem.OpenRead(path);
		return Load(stream, oneChannel, headerOnly: false, path);
	}

	public unsafe static void Save<TPixel>(Stream stream, FileFormat format, IntPtr dataPointer, int srcPitch, Vector2I size, uint bytesPerPixel) where TPixel : struct, IPixel<TPixel>
	{
		TPixel[] array = new TPixel[size.X * size.Y];
		Memory<TPixel> pixelMemory = new Memory<TPixel>(array);
		using (MemoryHandle memoryHandle = pixelMemory.Pin())
		{
			uint num = (uint)size.X * bytesPerPixel;
			byte* ptr = (byte*)memoryHandle.Pointer;
			byte* ptr2 = (byte*)dataPointer.ToPointer();
			for (int i = 0; i < size.Y; i++)
			{
				Unsafe.CopyBlockUnaligned(ptr, ptr2, num);
				ptr += num;
				ptr2 += srcPitch;
			}
		}
		using Image<TPixel> source = SixLabors.ImageSharp.Image.WrapMemory(pixelMemory, size.X, size.Y);
		switch (format)
		{
		case FileFormat.Png:
			source.SaveAsPng(stream);
			break;
		case FileFormat.Bmp:
			source.SaveAsBmp(stream);
			break;
		case FileFormat.Jpg:
			source.SaveAsJpeg(stream);
			break;
		default:
			throw new NotImplementedException("Unknown image format.");
		}
	}
}
public class MyImage<TData> : IMyImage<TData>, IMyImage where TData : unmanaged
{
	public Vector2I Size { get; private set; }

	public int Stride { get; private set; }

	public unsafe int BitsPerPixel => sizeof(TData) * 8;

	public TData[] Data { get; private set; }

	object IMyImage.Data => Data;

	public static MyImage<TData> Create<TImage>(string path) where TImage : struct, IPixel<TImage>
	{
		using Stream stream = MyFileSystem.OpenRead(path);
		return Create<TImage>(stream);
	}

	public static MyImage<TData> Create<TImage>(Stream stream) where TImage : struct, IPixel<TImage>
	{
		using Image<TImage> image = SixLabors.ImageSharp.Image.Load<TImage>(stream);
		TData[] data = MemoryMarshal.Cast<TImage, TData>(image.GetPixelSpan()).ToArray();
		return new MyImage<TData>
		{
			Size = new Vector2I(image.Width, image.Height),
			Stride = image.Width,
			Data = data
		};
	}

	public static MyImage<TData> Create<TImage>(IImageInfo image) where TImage : struct, IPixel<TImage>
	{
		return new MyImage<TData>
		{
			Size = new Vector2I(image.Width, image.Height),
			Stride = image.Width,
			Data = null
		};
	}

	private MyImage()
	{
	}
}
