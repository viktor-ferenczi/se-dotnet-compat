.class private auto ansi beforefieldinit VRageRender.MyTextureData
	extends VRage.Render11.Common.MyImmediateRC
{
	// Methods
	.method assembly hidebysig static 
		bool ToFile (
			class VRage.Render11.Resources.IResource res,
			string path,
			valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat fmt
		) cil managed 
	{
		// Method begins at RVA 0x2e4bc
		// Header size: 12
		// Code size: 115 (0x73)
		.maxstack 4
		.locals init (
			[0] class [netstandard]System.IO.FileStream,
			[1] bool,
			[2] class [netstandard]System.Exception
		)

		.try
		{
			IL_0000: ldarg.1
			IL_0001: call string [netstandard]System.IO.Path::GetDirectoryName(string)
			IL_0006: call class [netstandard]System.IO.DirectoryInfo [netstandard]System.IO.Directory::CreateDirectory(string)
			IL_000b: pop
			IL_000c: ldarg.1
			IL_000d: ldc.i4.2
			IL_000e: ldc.i4.2
			IL_000f: newobj instance void [netstandard]System.IO.FileStream::.ctor(string, valuetype [netstandard]System.IO.FileMode, valuetype [netstandard]System.IO.FileAccess)
			IL_0014: stloc.0
			.try
			{
				IL_0015: ldarg.0
				IL_0016: ldloc.0
				IL_0017: ldarg.2
				IL_0018: call void VRageRender.MyTextureData::Save(class VRage.Render11.Resources.IResource, class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat)
				IL_001d: ldloc.0
				IL_001e: callvirt instance void [netstandard]System.IO.Stream::Close()
				IL_0023: leave.s IL_002f
			} // end .try
			finally
			{
				IL_0025: ldloc.0
				IL_0026: brfalse.s IL_002e

				IL_0028: ldloc.0
				IL_0029: callvirt instance void [netstandard]System.IDisposable::Dispose()

				IL_002e: endfinally
			} // end handler

			IL_002f: ldc.i4.1
			IL_0030: stloc.1
			IL_0031: leave.s IL_0071
		} // end .try
		catch [netstandard]System.Exception
		{
			IL_0033: stloc.2
			IL_0034: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0039: ldstr "SaveResourceToFile()"
			IL_003e: callvirt instance void [VRage]VRage.Utils.MyLog::WriteLine(string)
			IL_0043: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0048: callvirt instance void [VRage]VRage.Utils.MyLog::IncreaseIndent()
			IL_004d: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0052: ldstr "Failed to save screenshot {0}: {1}"
			IL_0057: ldarg.1
			IL_0058: ldloc.2
			IL_0059: call string [netstandard]System.String::Format(string, object, object)
			IL_005e: callvirt instance void [VRage]VRage.Utils.MyLog::WriteLine(string)
			IL_0063: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0068: callvirt instance void [VRage]VRage.Utils.MyLog::DecreaseIndent()
			IL_006d: ldc.i4.0
			IL_006e: stloc.1
			IL_006f: leave.s IL_0071
		} // end handler

		IL_0071: ldloc.1
		IL_0072: ret
	} // end of method MyTextureData::ToFile

	.method assembly hidebysig static 
		uint8[] ToData (
			class VRage.Render11.Resources.IResource res,
			uint8[] screenData,
			valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat fmt
		) cil managed 
	{
		// Method begins at RVA 0x2e558
		// Header size: 12
		// Code size: 108 (0x6c)
		.maxstack 3
		.locals init (
			[0] class [netstandard]System.IO.MemoryStream,
			[1] uint8[],
			[2] class [netstandard]System.Exception
		)

		.try
		{
			IL_0000: ldarg.1
			IL_0001: brfalse.s IL_000c

			IL_0003: ldarg.1
			IL_0004: ldc.i4.1
			IL_0005: newobj instance void [netstandard]System.IO.MemoryStream::.ctor(uint8[], bool)
			IL_000a: br.s IL_0011

			IL_000c: newobj instance void [netstandard]System.IO.MemoryStream::.ctor()

			IL_0011: stloc.0
			.try
			{
				IL_0012: ldarg.0
				IL_0013: ldloc.0
				IL_0014: ldarg.2
				IL_0015: call void VRageRender.MyTextureData::Save(class VRage.Render11.Resources.IResource, class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat)
				IL_001a: ldloc.0
				IL_001b: callvirt instance uint8[] [netstandard]System.IO.MemoryStream::GetBuffer()
				IL_0020: stloc.1
				IL_0021: leave.s IL_006a
			} // end .try
			finally
			{
				IL_0023: ldloc.0
				IL_0024: brfalse.s IL_002c

				IL_0026: ldloc.0
				IL_0027: callvirt instance void [netstandard]System.IDisposable::Dispose()

				IL_002c: endfinally
			} // end handler
		} // end .try
		catch [netstandard]System.Exception
		{
			IL_002d: stloc.2
			IL_002e: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0033: ldstr "MyTextureData.ToData()"
			IL_0038: callvirt instance void [VRage]VRage.Utils.MyLog::WriteLine(string)
			IL_003d: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0042: callvirt instance void [VRage]VRage.Utils.MyLog::IncreaseIndent()
			IL_0047: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_004c: ldstr "Failed to extract data: {0}"
			IL_0051: ldloc.2
			IL_0052: call string [netstandard]System.String::Format(string, object)
			IL_0057: callvirt instance void [VRage]VRage.Utils.MyLog::WriteLine(string)
			IL_005c: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
			IL_0061: callvirt instance void [VRage]VRage.Utils.MyLog::DecreaseIndent()
			IL_0066: ldnull
			IL_0067: stloc.1
			IL_0068: leave.s IL_006a
		} // end handler

		IL_006a: ldloc.1
		IL_006b: ret
	} // end of method MyTextureData::ToData

	.method private hidebysig static 
		void Save (
			class VRage.Render11.Resources.IResource res,
			class [netstandard]System.IO.Stream 'stream',
			valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat fileFormat
		) cil managed 
	{
		// Method begins at RVA 0x2e5ec
		// Header size: 12
		// Code size: 232 (0xe8)
		.maxstack 7
		.locals init (
			[0] class [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D,
			[1] valuetype [SharpDX.DXGI]SharpDX.DXGI.Format,
			[2] class [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D,
			[3] class [SharpDX]SharpDX.DataStream,
			[4] valuetype [SharpDX]SharpDX.DataBox,
			[5] valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription
		)

		IL_0000: ldarg.0
		IL_0001: callvirt instance class [SharpDX.Direct3D11]SharpDX.Direct3D11.Resource VRage.Render11.Resources.IResource::get_Resource()
		IL_0006: isinst [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D
		IL_000b: stloc.0
		IL_000c: ldloc.0
		IL_000d: callvirt instance valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D::get_Description()
		IL_0012: ldfld valuetype [SharpDX.DXGI]SharpDX.DXGI.Format [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Format
		IL_0017: stloc.1
		IL_0018: call class [SharpDX.Direct3D11]SharpDX.Direct3D11.Device1 VRageRender.MyRender11::get_DeviceInstance()
		IL_001d: ldloca.s 5
		IL_001f: initobj [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription
		IL_0025: ldloca.s 5
		IL_0027: ldloc.0
		IL_0028: callvirt instance valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D::get_Description()
		IL_002d: ldfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Width
		IL_0032: stfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Width
		IL_0037: ldloca.s 5
		IL_0039: ldloc.0
		IL_003a: callvirt instance valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D::get_Description()
		IL_003f: ldfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Height
		IL_0044: stfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Height
		IL_0049: ldloca.s 5
		IL_004b: ldc.i4.1
		IL_004c: stfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::MipLevels
		IL_0051: ldloca.s 5
		IL_0053: ldc.i4.1
		IL_0054: stfld int32 [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::ArraySize
		IL_0059: ldloca.s 5
		IL_005b: ldloc.1
		IL_005c: stfld valuetype [SharpDX.DXGI]SharpDX.DXGI.Format [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Format
		IL_0061: ldloca.s 5
		IL_0063: ldc.i4.3
		IL_0064: stfld valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.ResourceUsage [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::Usage
		IL_0069: ldloca.s 5
		IL_006b: ldc.i4.1
		IL_006c: ldc.i4.0
		IL_006d: newobj instance void [SharpDX.DXGI]SharpDX.DXGI.SampleDescription::.ctor(int32, int32)
		IL_0072: stfld valuetype [SharpDX.DXGI]SharpDX.DXGI.SampleDescription [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::SampleDescription
		IL_0077: ldloca.s 5
		IL_0079: ldc.i4.0
		IL_007a: stfld valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.BindFlags [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::BindFlags
		IL_007f: ldloca.s 5
		IL_0081: ldc.i4 131072
		IL_0086: stfld valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.CpuAccessFlags [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::CpuAccessFlags
		IL_008b: ldloca.s 5
		IL_008d: ldc.i4.0
		IL_008e: stfld valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.ResourceOptionFlags [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription::OptionFlags
		IL_0093: ldloc.s 5
		IL_0095: newobj instance void [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D::.ctor(class [SharpDX.Direct3D11]SharpDX.Direct3D11.Device, valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2DDescription)
		IL_009a: stloc.2
		IL_009b: call class VRage.Render11.RenderContext.MyRenderContext VRage.Render11.Common.MyImmediateRC::get_RC()
		IL_00a0: ldarg.0
		IL_00a1: ldloc.2
		IL_00a2: callvirt instance void VRage.Render11.RenderContext.MyRenderContext::CopyResource(class VRage.Render11.Resources.IResource, class [SharpDX.Direct3D11]SharpDX.Direct3D11.Resource)
		IL_00a7: call class VRage.Render11.RenderContext.MyRenderContext VRage.Render11.Common.MyImmediateRC::get_RC()
		IL_00ac: ldloc.2
		IL_00ad: ldc.i4.0
		IL_00ae: ldc.i4.0
		IL_00af: ldc.i4.1
		IL_00b0: ldc.i4.0
		IL_00b1: ldloca.s 3
		IL_00b3: callvirt instance valuetype [SharpDX]SharpDX.DataBox VRage.Render11.RenderContext.MyRenderContext::MapSubresource(class [SharpDX.Direct3D11]SharpDX.Direct3D11.Texture2D, int32, int32, valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.MapMode, valuetype [SharpDX.Direct3D11]SharpDX.Direct3D11.MapFlags, class [SharpDX]SharpDX.DataStream&)
		IL_00b8: stloc.s 4
		IL_00ba: ldloc.1
		IL_00bb: ldarg.1
		IL_00bc: ldarg.2
		IL_00bd: ldloc.3
		IL_00be: callvirt instance native int [SharpDX]SharpDX.DataStream::get_DataPointer()
		IL_00c3: ldloc.s 4
		IL_00c5: ldfld int32 [SharpDX]SharpDX.DataBox::RowPitch
		IL_00ca: ldarg.0
		IL_00cb: callvirt instance valuetype [VRage.Math]VRageMath.Vector2I VRage.Render11.Resources.IResource::get_Size()
		IL_00d0: call void VRageRender.MyTextureData::Save(valuetype [SharpDX.DXGI]SharpDX.DXGI.Format, class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I)
		IL_00d5: call class VRage.Render11.RenderContext.MyRenderContext VRage.Render11.Common.MyImmediateRC::get_RC()
		IL_00da: ldloc.2
		IL_00db: ldc.i4.0
		IL_00dc: callvirt instance void VRage.Render11.RenderContext.MyRenderContext::UnmapSubresource(class [SharpDX.Direct3D11]SharpDX.Direct3D11.Resource, int32)
		IL_00e1: ldloc.2
		IL_00e2: callvirt instance void [SharpDX]SharpDX.DisposeBase::Dispose()
		IL_00e7: ret
	} // end of method MyTextureData::Save

	.method private hidebysig static 
		void Save (
			valuetype [SharpDX.DXGI]SharpDX.DXGI.Format format,
			class [netstandard]System.IO.Stream 'stream',
			valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat fileFormat,
			native int dataPointer,
			int32 srcPitch,
			valuetype [VRage.Math]VRageMath.Vector2I size
		) cil managed 
	{
		// Method begins at RVA 0x2e6e0
		// Header size: 12
		// Code size: 601 (0x259)
		.maxstack 6

		IL_0000: ldarg.0
		IL_0001: ldc.i4.1
		IL_0002: sub
		IL_0003: switch (IL_0185, IL_0185, IL_0185, IL_0185, IL_0258, IL_0258, IL_0258, IL_0258, IL_01a2, IL_0194, IL_01a2, IL_01a2, IL_01a2, IL_01a2, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_01b0, IL_01b0, IL_01b0, IL_0258, IL_01be, IL_01be, IL_01be, IL_01be, IL_01be, IL_01be, IL_01da, IL_01cc, IL_01da, IL_01da, IL_01da, IL_01da, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_01f6, IL_01f6, IL_01f6, IL_01f6, IL_01f6, IL_0212, IL_0204, IL_0212, IL_0212, IL_0212, IL_0212, IL_0212, IL_024a, IL_024a, IL_024a, IL_024a, IL_024a, IL_024a, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0220, IL_022e, IL_01e8, IL_01e8, IL_01b0, IL_01e8, IL_01e8, IL_01e8, IL_01e8)

		IL_017c: ldarg.0
		IL_017d: ldc.i4.s 115
		IL_017f: beq IL_023c

		IL_0184: ret

		IL_0185: ldarg.1
		IL_0186: ldarg.2
		IL_0187: ldarg.3
		IL_0188: ldarg.s srcPitch
		IL_018a: ldarg.s size
		IL_018c: ldc.i4.s 16
		IL_018e: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.RgbaVector>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_0193: ret

		IL_0194: ldarg.1
		IL_0195: ldarg.2
		IL_0196: ldarg.3
		IL_0197: ldarg.s srcPitch
		IL_0199: ldarg.s size
		IL_019b: ldc.i4.8
		IL_019c: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.HalfVector4>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01a1: ret

		IL_01a2: ldarg.1
		IL_01a3: ldarg.2
		IL_01a4: ldarg.3
		IL_01a5: ldarg.s srcPitch
		IL_01a7: ldarg.s size
		IL_01a9: ldc.i4.8
		IL_01aa: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rgba64>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01af: ret

		IL_01b0: ldarg.1
		IL_01b1: ldarg.2
		IL_01b2: ldarg.3
		IL_01b3: ldarg.s srcPitch
		IL_01b5: ldarg.s size
		IL_01b7: ldc.i4.4
		IL_01b8: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rgba1010102>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01bd: ret

		IL_01be: ldarg.1
		IL_01bf: ldarg.2
		IL_01c0: ldarg.3
		IL_01c1: ldarg.s srcPitch
		IL_01c3: ldarg.s size
		IL_01c5: ldc.i4.4
		IL_01c6: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rgba32>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01cb: ret

		IL_01cc: ldarg.1
		IL_01cd: ldarg.2
		IL_01ce: ldarg.3
		IL_01cf: ldarg.s srcPitch
		IL_01d1: ldarg.s size
		IL_01d3: ldc.i4.4
		IL_01d4: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.HalfVector2>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01d9: ret

		IL_01da: ldarg.1
		IL_01db: ldarg.2
		IL_01dc: ldarg.3
		IL_01dd: ldarg.s srcPitch
		IL_01df: ldarg.s size
		IL_01e1: ldc.i4.4
		IL_01e2: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rg32>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01e7: ret

		IL_01e8: ldarg.1
		IL_01e9: ldarg.2
		IL_01ea: ldarg.3
		IL_01eb: ldarg.s srcPitch
		IL_01ed: ldarg.s size
		IL_01ef: ldc.i4.4
		IL_01f0: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Bgra32>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_01f5: ret

		IL_01f6: ldarg.1
		IL_01f7: ldarg.2
		IL_01f8: ldarg.3
		IL_01f9: ldarg.s srcPitch
		IL_01fb: ldarg.s size
		IL_01fd: ldc.i4.2
		IL_01fe: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.NormalizedByte2>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_0203: ret

		IL_0204: ldarg.1
		IL_0205: ldarg.2
		IL_0206: ldarg.3
		IL_0207: ldarg.s srcPitch
		IL_0209: ldarg.s size
		IL_020b: ldc.i4.2
		IL_020c: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.HalfSingle>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_0211: ret

		IL_0212: ldarg.1
		IL_0213: ldarg.2
		IL_0214: ldarg.3
		IL_0215: ldarg.s srcPitch
		IL_0217: ldarg.s size
		IL_0219: ldc.i4.2
		IL_021a: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray16>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_021f: ret

		IL_0220: ldarg.1
		IL_0221: ldarg.2
		IL_0222: ldarg.3
		IL_0223: ldarg.s srcPitch
		IL_0225: ldarg.s size
		IL_0227: ldc.i4.2
		IL_0228: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Bgr565>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_022d: ret

		IL_022e: ldarg.1
		IL_022f: ldarg.2
		IL_0230: ldarg.3
		IL_0231: ldarg.s srcPitch
		IL_0233: ldarg.s size
		IL_0235: ldc.i4.2
		IL_0236: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Bgra5551>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_023b: ret

		IL_023c: ldarg.1
		IL_023d: ldarg.2
		IL_023e: ldarg.3
		IL_023f: ldarg.s srcPitch
		IL_0241: ldarg.s size
		IL_0243: ldc.i4.2
		IL_0244: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Bgra4444>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_0249: ret

		IL_024a: ldarg.1
		IL_024b: ldarg.2
		IL_024c: ldarg.3
		IL_024d: ldarg.s srcPitch
		IL_024f: ldarg.s size
		IL_0251: ldc.i4.1
		IL_0252: call void [VRage.Render]VRage.Render.Image.MyImage::Save<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray8>(class [netstandard]System.IO.Stream, valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat, native int, int32, valuetype [VRage.Math]VRageMath.Vector2I, uint32)
		IL_0257: ret

		IL_0258: ret
	} // end of method MyTextureData::Save

	.method public hidebysig static 
		valuetype [VRage.Render]VRage.Render.Image.MyImage/FileFormat GetFormat (
			string ext
		) cil managed 
	{
		// Method begins at RVA 0x2e948
		// Header size: 12
		// Code size: 77 (0x4d)
		.maxstack 2

		IL_0000: ldarg.0
		IL_0001: ldstr ".png"
		IL_0006: call bool [netstandard]System.String::op_Equality(string, string)
		IL_000b: brtrue.s IL_0036

		IL_000d: ldarg.0
		IL_000e: ldstr ".jpg"
		IL_0013: call bool [netstandard]System.String::op_Equality(string, string)
		IL_0018: brtrue.s IL_0038

		IL_001a: ldarg.0
		IL_001b: ldstr ".jpeg"
		IL_0020: call bool [netstandard]System.String::op_Equality(string, string)
		IL_0025: brtrue.s IL_0038

		IL_0027: ldarg.0
		IL_0028: ldstr ".bmp"
		IL_002d: call bool [netstandard]System.String::op_Equality(string, string)
		IL_0032: brtrue.s IL_003a

		IL_0034: br.s IL_003c

		IL_0036: ldc.i4.0
		IL_0037: ret

		IL_0038: ldc.i4.1
		IL_0039: ret

		IL_003a: ldc.i4.2
		IL_003b: ret

		IL_003c: ldsfld class [VRage]VRage.Utils.MyLog VRageRender.MyRender11::Log
		IL_0041: ldstr "GetFormat: Unhandled extension for image file format."
		IL_0046: callvirt instance void [VRage]VRage.Utils.MyLog::WriteLine(string)
		IL_004b: ldc.i4.0
		IL_004c: ret
	} // end of method MyTextureData::GetFormat

	.method public hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x1d873
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void VRage.Render11.Common.MyImmediateRC::.ctor()
		IL_0006: ret
	} // end of method MyTextureData::.ctor

} // end of class VRageRender.MyTextureData
