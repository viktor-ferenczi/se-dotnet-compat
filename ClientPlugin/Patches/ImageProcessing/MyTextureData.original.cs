using System;
using System.IO;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct3D11;
using SixLabors.ImageSharp.PixelFormats;
using VRage.Render.Image;
using VRage.Render11.Common;
using VRage.Render11.Resources;
using VRageMath;

namespace VRageRender;

internal class MyTextureData : MyImmediateRC
{
	internal static bool ToFile(IResource res, string path, MyImage.FileFormat fmt)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write))
			{
				Save(res, fileStream, fmt);
				fileStream.Close();
			}
			return true;
		}
		catch (Exception arg)
		{
			MyRender11.Log.WriteLine("SaveResourceToFile()");
			MyRender11.Log.IncreaseIndent();
			MyRender11.Log.WriteLine($"Failed to save screenshot {path}: {arg}");
			MyRender11.Log.DecreaseIndent();
			return false;
		}
	}

	internal static byte[] ToData(IResource res, byte[] screenData, MyImage.FileFormat fmt)
	{
		try
		{
			using MemoryStream memoryStream = ((screenData == null) ? new MemoryStream() : new MemoryStream(screenData, writable: true));
			Save(res, memoryStream, fmt);
			return memoryStream.GetBuffer();
		}
		catch (Exception arg)
		{
			MyRender11.Log.WriteLine("MyTextureData.ToData()");
			MyRender11.Log.IncreaseIndent();
			MyRender11.Log.WriteLine($"Failed to extract data: {arg}");
			MyRender11.Log.DecreaseIndent();
			return null;
		}
	}

	private static void Save(IResource res, Stream stream, MyImage.FileFormat fileFormat)
	{
		Texture2D texture2D = res.Resource as Texture2D;
		Format format = texture2D.Description.Format;
		Texture2D texture2D2 = new Texture2D(MyRender11.DeviceInstance, new Texture2DDescription
		{
			Width = texture2D.Description.Width,
			Height = texture2D.Description.Height,
			MipLevels = 1,
			ArraySize = 1,
			Format = format,
			Usage = ResourceUsage.Staging,
			SampleDescription = new SampleDescription(1, 0),
			BindFlags = BindFlags.None,
			CpuAccessFlags = CpuAccessFlags.Read,
			OptionFlags = ResourceOptionFlags.None
		});
		MyImmediateRC.RC.CopyResource(res, texture2D2);
		DataStream stream2;
		DataBox dataBox = MyImmediateRC.RC.MapSubresource(texture2D2, 0, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None, out stream2);
		Save(format, stream, fileFormat, stream2.DataPointer, dataBox.RowPitch, res.Size);
		MyImmediateRC.RC.UnmapSubresource(texture2D2, 0);
		texture2D2.Dispose();
	}

	private static void Save(Format format, Stream stream, MyImage.FileFormat fileFormat, IntPtr dataPointer, int srcPitch, Vector2I size)
	{
		switch (format)
		{
		case Format.R32G32B32A32_Typeless:
		case Format.R32G32B32A32_Float:
		case Format.R32G32B32A32_UInt:
		case Format.R32G32B32A32_SInt:
			MyImage.Save<RgbaVector>(stream, fileFormat, dataPointer, srcPitch, size, 16u);
			break;
		case Format.R16G16B16A16_Float:
			MyImage.Save<HalfVector4>(stream, fileFormat, dataPointer, srcPitch, size, 8u);
			break;
		case Format.R16G16B16A16_Typeless:
		case Format.R16G16B16A16_UNorm:
		case Format.R16G16B16A16_UInt:
		case Format.R16G16B16A16_SNorm:
		case Format.R16G16B16A16_SInt:
			MyImage.Save<Rgba64>(stream, fileFormat, dataPointer, srcPitch, size, 8u);
			break;
		case Format.R10G10B10A2_Typeless:
		case Format.R10G10B10A2_UNorm:
		case Format.R10G10B10A2_UInt:
		case Format.R10G10B10_Xr_Bias_A2_UNorm:
			MyImage.Save<Rgba1010102>(stream, fileFormat, dataPointer, srcPitch, size, 4u);
			break;
		case Format.R8G8B8A8_Typeless:
		case Format.R8G8B8A8_UNorm:
		case Format.R8G8B8A8_UNorm_SRgb:
		case Format.R8G8B8A8_UInt:
		case Format.R8G8B8A8_SNorm:
		case Format.R8G8B8A8_SInt:
			MyImage.Save<Rgba32>(stream, fileFormat, dataPointer, srcPitch, size, 4u);
			break;
		case Format.R16G16_Float:
			MyImage.Save<HalfVector2>(stream, fileFormat, dataPointer, srcPitch, size, 4u);
			break;
		case Format.R16G16_Typeless:
		case Format.R16G16_UNorm:
		case Format.R16G16_UInt:
		case Format.R16G16_SNorm:
		case Format.R16G16_SInt:
			MyImage.Save<Rg32>(stream, fileFormat, dataPointer, srcPitch, size, 4u);
			break;
		case Format.B8G8R8A8_UNorm:
		case Format.B8G8R8X8_UNorm:
		case Format.B8G8R8A8_Typeless:
		case Format.B8G8R8A8_UNorm_SRgb:
		case Format.B8G8R8X8_Typeless:
		case Format.B8G8R8X8_UNorm_SRgb:
			MyImage.Save<Bgra32>(stream, fileFormat, dataPointer, srcPitch, size, 4u);
			break;
		case Format.R8G8_Typeless:
		case Format.R8G8_UNorm:
		case Format.R8G8_UInt:
		case Format.R8G8_SNorm:
		case Format.R8G8_SInt:
			MyImage.Save<NormalizedByte2>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.R16_Float:
			MyImage.Save<HalfSingle>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.R16_Typeless:
		case Format.D16_UNorm:
		case Format.R16_UNorm:
		case Format.R16_UInt:
		case Format.R16_SNorm:
		case Format.R16_SInt:
			MyImage.Save<Gray16>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.B5G6R5_UNorm:
			MyImage.Save<Bgr565>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.B5G5R5A1_UNorm:
			MyImage.Save<Bgra5551>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.B4G4R4A4_UNorm:
			MyImage.Save<Bgra4444>(stream, fileFormat, dataPointer, srcPitch, size, 2u);
			break;
		case Format.R8_Typeless:
		case Format.R8_UNorm:
		case Format.R8_UInt:
		case Format.R8_SNorm:
		case Format.R8_SInt:
		case Format.A8_UNorm:
			MyImage.Save<Gray8>(stream, fileFormat, dataPointer, srcPitch, size, 1u);
			break;
		}
	}

	public static MyImage.FileFormat GetFormat(string ext)
	{
		switch (ext)
		{
		case ".png":
			return MyImage.FileFormat.Png;
		case ".jpg":
		case ".jpeg":
			return MyImage.FileFormat.Jpg;
		case ".bmp":
			return MyImage.FileFormat.Bmp;
		default:
			MyRender11.Log.WriteLine("GetFormat: Unhandled extension for image file format.");
			return MyImage.FileFormat.Png;
		}
	}
}
