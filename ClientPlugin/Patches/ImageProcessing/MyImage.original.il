.class public auto ansi abstract sealed VRage.Render.Image.MyImage
	extends [netstandard]System.Object
{
	// Nested Types
	.class nested public auto ansi sealed FileFormat
		extends [netstandard]System.Enum
	{
		// Fields
		.field public specialname rtspecialname int32 value__
		.field public static literal valuetype VRage.Render.Image.MyImage/FileFormat Png = int32(0)
		.field public static literal valuetype VRage.Render.Image.MyImage/FileFormat Jpg = int32(1)
		.field public static literal valuetype VRage.Render.Image.MyImage/FileFormat Bmp = int32(2)

	} // end of class FileFormat


	// Methods
	.method private hidebysig specialname rtspecialname static 
		void .cctor () cil managed 
	{
		// Method begins at RVA 0x6c1b
		// Header size: 1
		// Code size: 16 (0x10)
		.maxstack 8

		IL_0000: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Configuration [SixLabors.ImageSharp]SixLabors.ImageSharp.Configuration::get_Default()
		IL_0005: newobj instance void [SixLabors.Core]SixLabors.Memory.SimpleGcMemoryAllocator::.ctor()
		IL_000a: callvirt instance void [SixLabors.ImageSharp]SixLabors.ImageSharp.Configuration::set_MemoryAllocator(class [SixLabors.Core]SixLabors.Memory.MemoryAllocator)
		IL_000f: ret
	} // end of method MyImage::.cctor

	.method public hidebysig static 
		class VRage.Render.Image.IMyImage Load (
			class [netstandard]System.IO.Stream 'stream',
			bool oneChannel,
			[opt] bool headerOnly,
			[opt] string debugName
		) cil managed 
	{
		.param [3] = bool(false)
		.param [4] = nullref
		// Method begins at RVA 0x6c2c
		// Header size: 12
		// Code size: 202 (0xca)
		.maxstack 2
		.locals init (
			[0] class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo,
			[1] valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngBitDepth,
			[2] class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData
		)

		IL_0000: ldarg.0
		IL_0001: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo [SixLabors.ImageSharp]SixLabors.ImageSharp.Image::Identify(class [netstandard]System.IO.Stream)
		IL_0006: stloc.0
		IL_0007: ldarg.0
		IL_0008: ldc.i4.0
		IL_0009: conv.i8
		IL_000a: callvirt instance void [netstandard]System.IO.Stream::set_Position(int64)
		IL_000f: ldarg.1
		IL_0010: brtrue.s IL_002c

		IL_0012: ldloc.0
		IL_0013: callvirt instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_MetaData()
		IL_0018: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat::get_Instance()
		IL_001d: callvirt instance !!0 [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData::GetFormatMetaData<class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.IImageFormat`1<!!0>)
		IL_0022: callvirt instance valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData::get_ColorType()
		IL_0027: ldc.i4.0
		IL_0028: ceq
		IL_002a: starg.s oneChannel

		IL_002c: ldarg.2
		IL_002d: brfalse.s IL_005f

		IL_002f: ldarg.1
		IL_0030: brfalse.s IL_0058

		IL_0032: ldloc.0
		IL_0033: callvirt instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.PixelTypeInfo [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_PixelType()
		IL_0038: callvirt instance int32 [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.PixelTypeInfo::get_BitsPerPixel()
		IL_003d: conv.u1
		IL_003e: stloc.1
		IL_003f: ldloc.1
		IL_0040: ldc.i4.8
		IL_0041: beq.s IL_004a

		IL_0043: ldloc.1
		IL_0044: ldc.i4.s 16
		IL_0046: beq.s IL_0051

		IL_0048: br.s IL_00c8

		IL_004a: ldloc.0
		IL_004b: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint8>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray8>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo)
		IL_0050: ret

		IL_0051: ldloc.0
		IL_0052: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint16>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray16>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo)
		IL_0057: ret

		IL_0058: ldloc.0
		IL_0059: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint32>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rgba32>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo)
		IL_005e: ret

		IL_005f: ldarg.1
		IL_0060: brfalse.s IL_0088

		IL_0062: ldloc.0
		IL_0063: callvirt instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.PixelTypeInfo [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_PixelType()
		IL_0068: callvirt instance int32 [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.PixelTypeInfo::get_BitsPerPixel()
		IL_006d: conv.u1
		IL_006e: stloc.1
		IL_006f: ldloc.1
		IL_0070: ldc.i4.8
		IL_0071: beq.s IL_007a

		IL_0073: ldloc.1
		IL_0074: ldc.i4.s 16
		IL_0076: beq.s IL_0081

		IL_0078: br.s IL_00c8

		IL_007a: ldarg.0
		IL_007b: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint8>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray8>(class [netstandard]System.IO.Stream)
		IL_0080: ret

		IL_0081: ldarg.0
		IL_0082: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint16>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray16>(class [netstandard]System.IO.Stream)
		IL_0087: ret

		IL_0088: ldloc.0
		IL_0089: callvirt instance class [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_MetaData()
		IL_008e: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngFormat::get_Instance()
		IL_0093: callvirt instance !!0 [SixLabors.ImageSharp]SixLabors.ImageSharp.MetaData.ImageMetaData::GetFormatMetaData<class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.IImageFormat`1<!!0>)
		IL_0098: stloc.2
		IL_0099: ldloc.2
		IL_009a: callvirt instance valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngColorType [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData::get_ColorType()
		IL_009f: brtrue.s IL_00c1

		IL_00a1: ldloc.2
		IL_00a2: callvirt instance valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngBitDepth [SixLabors.ImageSharp]SixLabors.ImageSharp.Formats.Png.PngMetaData::get_BitDepth()
		IL_00a7: stloc.1
		IL_00a8: ldloc.1
		IL_00a9: ldc.i4.8
		IL_00aa: beq.s IL_00b3

		IL_00ac: ldloc.1
		IL_00ad: ldc.i4.s 16
		IL_00af: beq.s IL_00ba

		IL_00b1: br.s IL_00c8

		IL_00b3: ldarg.0
		IL_00b4: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint8>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray8>(class [netstandard]System.IO.Stream)
		IL_00b9: ret

		IL_00ba: ldarg.0
		IL_00bb: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint16>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Gray16>(class [netstandard]System.IO.Stream)
		IL_00c0: ret

		IL_00c1: ldarg.0
		IL_00c2: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<uint32>::Create<valuetype [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.Rgba32>(class [netstandard]System.IO.Stream)
		IL_00c7: ret

		IL_00c8: ldnull
		IL_00c9: ret
	} // end of method MyImage::Load

	.method public hidebysig static 
		class VRage.Render.Image.IMyImage Load (
			native int pSource,
			int32 size,
			string debugName
		) cil managed 
	{
		// Method begins at RVA 0x6d04
		// Header size: 12
		// Code size: 39 (0x27)
		.maxstack 4
		.locals init (
			[0] class [netstandard]System.IO.UnmanagedMemoryStream,
			[1] class VRage.Render.Image.IMyImage
		)

		IL_0000: ldarga.s pSource
		IL_0002: call instance void* [netstandard]System.IntPtr::ToPointer()
		IL_0007: ldarg.1
		IL_0008: conv.i8
		IL_0009: newobj instance void [netstandard]System.IO.UnmanagedMemoryStream::.ctor(uint8*, int64)
		IL_000e: stloc.0
		.try
		{
			IL_000f: ldloc.0
			IL_0010: ldc.i4.0
			IL_0011: ldc.i4.0
			IL_0012: ldarg.2
			IL_0013: call class VRage.Render.Image.IMyImage VRage.Render.Image.MyImage::Load(class [netstandard]System.IO.Stream, bool, bool, string)
			IL_0018: stloc.1
			IL_0019: leave.s IL_0025
		} // end .try
		finally
		{
			IL_001b: ldloc.0
			IL_001c: brfalse.s IL_0024

			IL_001e: ldloc.0
			IL_001f: callvirt instance void [netstandard]System.IDisposable::Dispose()

			IL_0024: endfinally
		} // end handler

		IL_0025: ldloc.1
		IL_0026: ret
	} // end of method MyImage::Load

	.method public hidebysig static 
		class VRage.Render.Image.IMyImage Load (
			string path,
			bool oneChannel
		) cil managed 
	{
		// Method begins at RVA 0x6d48
		// Header size: 12
		// Code size: 31 (0x1f)
		.maxstack 4
		.locals init (
			[0] class [netstandard]System.IO.Stream,
			[1] class VRage.Render.Image.IMyImage
		)

		IL_0000: ldarg.0
		IL_0001: call class [netstandard]System.IO.Stream [VRage.Library]VRage.FileSystem.MyFileSystem::OpenRead(string)
		IL_0006: stloc.0
		.try
		{
			IL_0007: ldloc.0
			IL_0008: ldarg.1
			IL_0009: ldc.i4.0
			IL_000a: ldarg.0
			IL_000b: call class VRage.Render.Image.IMyImage VRage.Render.Image.MyImage::Load(class [netstandard]System.IO.Stream, bool, bool, string)
			IL_0010: stloc.1
			IL_0011: leave.s IL_001d
		} // end .try
		finally
		{
			IL_0013: ldloc.0
			IL_0014: brfalse.s IL_001c

			IL_0016: ldloc.0
			IL_0017: callvirt instance void [netstandard]System.IDisposable::Dispose()

			IL_001c: endfinally
		} // end handler

		IL_001d: ldloc.1
		IL_001e: ret
	} // end of method MyImage::Load

	.method public hidebysig static 
		void Save<valuetype .ctor (class [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.IPixel`1<!!TPixel>, [netstandard]System.ValueType) TPixel> (
			class [netstandard]System.IO.Stream 'stream',
			valuetype VRage.Render.Image.MyImage/FileFormat format,
			native int dataPointer,
			int32 srcPitch,
			valuetype [VRage.Math]VRageMath.Vector2I size,
			uint32 bytesPerPixel
		) cil managed 
	{
		// Method begins at RVA 0x6d84
		// Header size: 12
		// Code size: 223 (0xdf)
		.maxstack 3
		.locals init (
			[0] !!TPixel[],
			[1] valuetype [System.Memory]System.Memory`1<!!TPixel>,
			[2] valuetype [System.Memory]System.Buffers.MemoryHandle,
			[3] uint32,
			[4] uint8*,
			[5] uint8*,
			[6] int32,
			[7] class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!TPixel>
		)

		IL_0000: ldarg.s size
		IL_0002: ldfld int32 [VRage.Math]VRageMath.Vector2I::X
		IL_0007: ldarg.s size
		IL_0009: ldfld int32 [VRage.Math]VRageMath.Vector2I::Y
		IL_000e: mul
		IL_000f: newarr !!TPixel
		IL_0014: stloc.0
		IL_0015: ldloca.s 1
		IL_0017: ldloc.0
		IL_0018: call instance void valuetype [System.Memory]System.Memory`1<!!TPixel>::.ctor(!0[])
		IL_001d: ldloca.s 1
		IL_001f: call instance valuetype [System.Memory]System.Buffers.MemoryHandle valuetype [System.Memory]System.Memory`1<!!TPixel>::Pin()
		IL_0024: stloc.2
		.try
		{
			IL_0025: ldarg.s size
			IL_0027: ldfld int32 [VRage.Math]VRageMath.Vector2I::X
			IL_002c: ldarg.s bytesPerPixel
			IL_002e: mul
			IL_002f: stloc.3
			IL_0030: ldloca.s 2
			IL_0032: call instance void* [System.Memory]System.Buffers.MemoryHandle::get_Pointer()
			IL_0037: stloc.s 4
			IL_0039: ldarga.s dataPointer
			IL_003b: call instance void* [netstandard]System.IntPtr::ToPointer()
			IL_0040: stloc.s 5
			IL_0042: ldc.i4.0
			IL_0043: stloc.s 6
			IL_0045: br.s IL_0064
			// loop start (head: IL_0064)
				IL_0047: ldloc.s 4
				IL_0049: ldloc.s 5
				IL_004b: ldloc.3
				IL_004c: call void [System.Runtime.CompilerServices.Unsafe]System.Runtime.CompilerServices.Unsafe::CopyBlockUnaligned(void*, void*, uint32)
				IL_0051: ldloc.s 4
				IL_0053: ldloc.3
				IL_0054: conv.u
				IL_0055: add
				IL_0056: stloc.s 4
				IL_0058: ldloc.s 5
				IL_005a: ldarg.3
				IL_005b: add
				IL_005c: stloc.s 5
				IL_005e: ldloc.s 6
				IL_0060: ldc.i4.1
				IL_0061: add
				IL_0062: stloc.s 6

				IL_0064: ldloc.s 6
				IL_0066: ldarg.s size
				IL_0068: ldfld int32 [VRage.Math]VRageMath.Vector2I::Y
				IL_006d: blt.s IL_0047
			// end loop

			IL_006f: leave.s IL_007f
		} // end .try
		finally
		{
			IL_0071: ldloca.s 2
			IL_0073: constrained. [System.Memory]System.Buffers.MemoryHandle
			IL_0079: callvirt instance void [netstandard]System.IDisposable::Dispose()
			IL_007e: endfinally
		} // end handler

		IL_007f: ldloc.1
		IL_0080: ldarg.s size
		IL_0082: ldfld int32 [VRage.Math]VRageMath.Vector2I::X
		IL_0087: ldarg.s size
		IL_0089: ldfld int32 [VRage.Math]VRageMath.Vector2I::Y
		IL_008e: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0> [SixLabors.ImageSharp]SixLabors.ImageSharp.Image::WrapMemory<!!TPixel>(valuetype [System.Memory]System.Memory`1<!!0>, int32, int32)
		IL_0093: stloc.s 7
		.try
		{
			IL_0095: ldarg.1
			IL_0096: switch (IL_00a9, IL_00bd, IL_00b3)

			IL_00a7: br.s IL_00c7

			IL_00a9: ldloc.s 7
			IL_00ab: ldarg.0
			IL_00ac: call void [SixLabors.ImageSharp]SixLabors.ImageSharp.ImageExtensions::SaveAsPng<!!TPixel>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0>, class [netstandard]System.IO.Stream)
			IL_00b1: leave.s IL_00de

			IL_00b3: ldloc.s 7
			IL_00b5: ldarg.0
			IL_00b6: call void [SixLabors.ImageSharp]SixLabors.ImageSharp.ImageExtensions::SaveAsBmp<!!TPixel>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0>, class [netstandard]System.IO.Stream)
			IL_00bb: leave.s IL_00de

			IL_00bd: ldloc.s 7
			IL_00bf: ldarg.0
			IL_00c0: call void [SixLabors.ImageSharp]SixLabors.ImageSharp.ImageExtensions::SaveAsJpeg<!!TPixel>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0>, class [netstandard]System.IO.Stream)
			IL_00c5: leave.s IL_00de

			IL_00c7: ldstr "Unknown image format."
			IL_00cc: newobj instance void [netstandard]System.NotImplementedException::.ctor(string)
			IL_00d1: throw
		} // end .try
		finally
		{
			IL_00d2: ldloc.s 7
			IL_00d4: brfalse.s IL_00dd

			IL_00d6: ldloc.s 7
			IL_00d8: callvirt instance void [netstandard]System.IDisposable::Dispose()

			IL_00dd: endfinally
		} // end handler

		IL_00de: ret
	} // end of method MyImage::Save

} // end of class VRage.Render.Image.MyImage

.class public auto ansi beforefieldinit VRage.Render.Image.MyImage`1<valuetype .ctor (class [netstandard]System.ValueType modreq([netstandard]System.Runtime.InteropServices.UnmanagedType)) TData>
	extends [netstandard]System.Object
	implements class VRage.Render.Image.IMyImage`1<!TData>,
	           VRage.Render.Image.IMyImage
{
	.param type TData
		.custom instance void System.Runtime.CompilerServices.IsUnmanagedAttribute::.ctor() = (
			01 00 00 00
		)
	// Fields
	.field private valuetype [VRage.Math]VRageMath.Vector2I '<Size>k__BackingField'
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.field private int32 '<Stride>k__BackingField'
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)
	.field private !TData[] '<Data>k__BackingField'
	.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
		01 00 00 00
	)

	// Methods
	.method public hidebysig static 
		class VRage.Render.Image.MyImage`1<!TData> Create<valuetype .ctor (class [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.IPixel`1<!!TImage>, [netstandard]System.ValueType) TImage> (
			string path
		) cil managed 
	{
		// Method begins at RVA 0x6e8c
		// Header size: 12
		// Code size: 28 (0x1c)
		.maxstack 1
		.locals init (
			[0] class [netstandard]System.IO.Stream,
			[1] class VRage.Render.Image.MyImage`1<!TData>
		)

		IL_0000: ldarg.0
		IL_0001: call class [netstandard]System.IO.Stream [VRage.Library]VRage.FileSystem.MyFileSystem::OpenRead(string)
		IL_0006: stloc.0
		.try
		{
			IL_0007: ldloc.0
			IL_0008: call class VRage.Render.Image.MyImage`1<!0> class VRage.Render.Image.MyImage`1<!TData>::Create<!!TImage>(class [netstandard]System.IO.Stream)
			IL_000d: stloc.1
			IL_000e: leave.s IL_001a
		} // end .try
		finally
		{
			IL_0010: ldloc.0
			IL_0011: brfalse.s IL_0019

			IL_0013: ldloc.0
			IL_0014: callvirt instance void [netstandard]System.IDisposable::Dispose()

			IL_0019: endfinally
		} // end handler

		IL_001a: ldloc.1
		IL_001b: ret
	} // end of method MyImage`1::Create

	.method public hidebysig static 
		class VRage.Render.Image.MyImage`1<!TData> Create<valuetype .ctor (class [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.IPixel`1<!!TImage>, [netstandard]System.ValueType) TImage> (
			class [netstandard]System.IO.Stream 'stream'
		) cil managed 
	{
		// Method begins at RVA 0x6ec4
		// Header size: 12
		// Code size: 89 (0x59)
		.maxstack 4
		.locals init (
			[0] class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!TImage>,
			[1] valuetype [System.Memory]System.Span`1<!TData>,
			[2] !TData[],
			[3] class VRage.Render.Image.MyImage`1<!TData>
		)

		IL_0000: ldarg.0
		IL_0001: call class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0> [SixLabors.ImageSharp]SixLabors.ImageSharp.Image::Load<!!TImage>(class [netstandard]System.IO.Stream)
		IL_0006: stloc.0
		.try
		{
			IL_0007: ldloc.0
			IL_0008: call valuetype [System.Memory]System.Span`1<!!0> [SixLabors.ImageSharp]SixLabors.ImageSharp.Advanced.AdvancedImageExtensions::GetPixelSpan<!!TImage>(class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!0>)
			IL_000d: call valuetype [System.Memory]System.Span`1<!!1> [System.Memory]System.Runtime.InteropServices.MemoryMarshal::Cast<!!TImage, !TData>(valuetype [System.Memory]System.Span`1<!!0>)
			IL_0012: stloc.1
			IL_0013: ldloca.s 1
			IL_0015: call instance !0[] valuetype [System.Memory]System.Span`1<!TData>::ToArray()
			IL_001a: stloc.2
			IL_001b: newobj instance void class VRage.Render.Image.MyImage`1<!TData>::.ctor()
			IL_0020: dup
			IL_0021: ldloc.0
			IL_0022: callvirt instance int32 class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!TImage>::get_Width()
			IL_0027: ldloc.0
			IL_0028: callvirt instance int32 class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!TImage>::get_Height()
			IL_002d: newobj instance void [VRage.Math]VRageMath.Vector2I::.ctor(int32, int32)
			IL_0032: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Size(valuetype [VRage.Math]VRageMath.Vector2I)
			IL_0037: dup
			IL_0038: ldloc.0
			IL_0039: callvirt instance int32 class [SixLabors.ImageSharp]SixLabors.ImageSharp.Image`1<!!TImage>::get_Width()
			IL_003e: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Stride(int32)
			IL_0043: dup
			IL_0044: ldloc.2
			IL_0045: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Data(!0[])
			IL_004a: stloc.3
			IL_004b: leave.s IL_0057
		} // end .try
		finally
		{
			IL_004d: ldloc.0
			IL_004e: brfalse.s IL_0056

			IL_0050: ldloc.0
			IL_0051: callvirt instance void [netstandard]System.IDisposable::Dispose()

			IL_0056: endfinally
		} // end handler

		IL_0057: ldloc.3
		IL_0058: ret
	} // end of method MyImage`1::Create

	.method public hidebysig static 
		class VRage.Render.Image.MyImage`1<!TData> Create<valuetype .ctor (class [SixLabors.ImageSharp]SixLabors.ImageSharp.PixelFormats.IPixel`1<!!TImage>, [netstandard]System.ValueType) TImage> (
			class [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo image
		) cil managed 
	{
		// Method begins at RVA 0x6f3c
		// Header size: 1
		// Code size: 48 (0x30)
		.maxstack 8

		IL_0000: newobj instance void class VRage.Render.Image.MyImage`1<!TData>::.ctor()
		IL_0005: dup
		IL_0006: ldarg.0
		IL_0007: callvirt instance int32 [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_Width()
		IL_000c: ldarg.0
		IL_000d: callvirt instance int32 [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_Height()
		IL_0012: newobj instance void [VRage.Math]VRageMath.Vector2I::.ctor(int32, int32)
		IL_0017: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Size(valuetype [VRage.Math]VRageMath.Vector2I)
		IL_001c: dup
		IL_001d: ldarg.0
		IL_001e: callvirt instance int32 [SixLabors.ImageSharp]SixLabors.ImageSharp.IImageInfo::get_Width()
		IL_0023: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Stride(int32)
		IL_0028: dup
		IL_0029: ldnull
		IL_002a: callvirt instance void class VRage.Render.Image.MyImage`1<!TData>::set_Data(!0[])
		IL_002f: ret
	} // end of method MyImage`1::Create

	.method private hidebysig specialname rtspecialname 
		instance void .ctor () cil managed 
	{
		// Method begins at RVA 0x39ba
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance void [netstandard]System.Object::.ctor()
		IL_0006: ret
	} // end of method MyImage`1::.ctor

	.method public final hidebysig specialname newslot virtual 
		instance valuetype [VRage.Math]VRageMath.Vector2I get_Size () cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6f6d
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldfld valuetype [VRage.Math]VRageMath.Vector2I class VRage.Render.Image.MyImage`1<!TData>::'<Size>k__BackingField'
		IL_0006: ret
	} // end of method MyImage`1::get_Size

	.method private hidebysig specialname 
		instance void set_Size (
			valuetype [VRage.Math]VRageMath.Vector2I 'value'
		) cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6f75
		// Header size: 1
		// Code size: 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld valuetype [VRage.Math]VRageMath.Vector2I class VRage.Render.Image.MyImage`1<!TData>::'<Size>k__BackingField'
		IL_0007: ret
	} // end of method MyImage`1::set_Size

	.method public final hidebysig specialname newslot virtual 
		instance int32 get_Stride () cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6f7e
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldfld int32 class VRage.Render.Image.MyImage`1<!TData>::'<Stride>k__BackingField'
		IL_0006: ret
	} // end of method MyImage`1::get_Stride

	.method private hidebysig specialname 
		instance void set_Stride (
			int32 'value'
		) cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6f86
		// Header size: 1
		// Code size: 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld int32 class VRage.Render.Image.MyImage`1<!TData>::'<Stride>k__BackingField'
		IL_0007: ret
	} // end of method MyImage`1::set_Stride

	.method public final hidebysig specialname newslot virtual 
		instance int32 get_BitsPerPixel () cil managed 
	{
		// Method begins at RVA 0x6f8f
		// Header size: 1
		// Code size: 9 (0x9)
		.maxstack 8

		IL_0000: sizeof !TData
		IL_0006: ldc.i4.8
		IL_0007: mul
		IL_0008: ret
	} // end of method MyImage`1::get_BitsPerPixel

	.method public final hidebysig specialname newslot virtual 
		instance !TData[] get_Data () cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6f99
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldfld !0[] class VRage.Render.Image.MyImage`1<!TData>::'<Data>k__BackingField'
		IL_0006: ret
	} // end of method MyImage`1::get_Data

	.method private hidebysig specialname 
		instance void set_Data (
			!TData[] 'value'
		) cil managed 
	{
		.custom instance void [netstandard]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
			01 00 00 00
		)
		// Method begins at RVA 0x6fa1
		// Header size: 1
		// Code size: 8 (0x8)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: ldarg.1
		IL_0002: stfld !0[] class VRage.Render.Image.MyImage`1<!TData>::'<Data>k__BackingField'
		IL_0007: ret
	} // end of method MyImage`1::set_Data

	.method private final hidebysig specialname newslot virtual 
		instance object VRage.Render.Image.IMyImage.get_Data () cil managed 
	{
		.override method instance object VRage.Render.Image.IMyImage::get_Data()
		// Method begins at RVA 0x6faa
		// Header size: 1
		// Code size: 7 (0x7)
		.maxstack 8

		IL_0000: ldarg.0
		IL_0001: call instance !0[] class VRage.Render.Image.MyImage`1<!TData>::get_Data()
		IL_0006: ret
	} // end of method MyImage`1::VRage.Render.Image.IMyImage.get_Data

	// Properties
	.property instance valuetype [VRage.Math]VRageMath.Vector2I Size()
	{
		.get instance valuetype [VRage.Math]VRageMath.Vector2I VRage.Render.Image.MyImage`1::get_Size()
		.set instance void VRage.Render.Image.MyImage`1::set_Size(valuetype [VRage.Math]VRageMath.Vector2I)
	}
	.property instance int32 Stride()
	{
		.get instance int32 VRage.Render.Image.MyImage`1::get_Stride()
		.set instance void VRage.Render.Image.MyImage`1::set_Stride(int32)
	}
	.property instance int32 BitsPerPixel()
	{
		.get instance int32 VRage.Render.Image.MyImage`1::get_BitsPerPixel()
	}
	.property instance !TData[] Data()
	{
		.get instance !0[] VRage.Render.Image.MyImage`1::get_Data()
		.set instance void VRage.Render.Image.MyImage`1::set_Data(!0[])
	}
	.property instance object VRage.Render.Image.IMyImage.Data()
	{
		.get instance object VRage.Render.Image.MyImage`1::VRage.Render.Image.IMyImage.get_Data()
	}

} // end of class VRage.Render.Image.MyImage`1
