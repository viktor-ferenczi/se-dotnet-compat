using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using VRage.FileSystem;
using VRage.Render.Image;
using VRageMath;

namespace ClientPlugin.Patches.ImageProcessing;

// ReSharper disable once UnusedType.Global
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
		ImageInfo imageInfo = SixLabors.ImageSharp.Image.Identify(stream);
		stream.Position = 0L;
		if (!oneChannel)
		{
			oneChannel = imageInfo.Metadata.GetPngMetadata().ColorType == PngColorType.Grayscale;
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
				return MyImage<byte>.Create<L8>(imageInfo);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<L16>(imageInfo);
			}
		}
		else if (oneChannel)
		{
			switch ((PngBitDepth)(byte)imageInfo.PixelType.BitsPerPixel)
			{
			case PngBitDepth.Bit8:
				return MyImage<byte>.Create<L8>(stream);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<L16>(stream);
			}
		}
		else
		{
			var formatMetaData = imageInfo.Metadata.GetPngMetadata();
			if (formatMetaData.ColorType != 0)
			{
				return MyImage<uint>.Create<Rgba32>(stream);
			}
			switch (formatMetaData.BitDepth)
			{
			case PngBitDepth.Bit8:
				return MyImage<byte>.Create<L8>(stream);
			case PngBitDepth.Bit16:
				return MyImage<ushort>.Create<L16>(stream);
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

	public static unsafe void Save<TPixel>(Stream stream, FileFormat format, IntPtr dataPointer, int srcPitch, Vector2I size, uint bytesPerPixel) where TPixel : unmanaged, IPixel<TPixel>
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

	public static MyImage<TData> Create<TImage>(string path) where TImage : unmanaged, IPixel<TImage>
	{
		using Stream stream = MyFileSystem.OpenRead(path);
		return Create<TImage>(stream);
	}

	public static MyImage<TData> Create<TImage>(Stream stream) where TImage : unmanaged, IPixel<TImage>
	{
		using Image<TImage> image = SixLabors.ImageSharp.Image.Load<TImage>(stream);
		TData[] data = MemoryMarshal.Cast<TImage, TData>(image.GetPixelMemoryGroup().Single().Span).ToArray();
		return new MyImage<TData>
		{
			Size = new Vector2I(image.Width, image.Height),
			Stride = image.Width,
			Data = data
		};
	}

	public static MyImage<TData> Create<TImage>(ImageInfo image) where TImage : unmanaged, IPixel<TImage>
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
