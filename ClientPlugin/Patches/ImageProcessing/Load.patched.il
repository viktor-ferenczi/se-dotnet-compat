// f6f8bc95
ldarg.0
call SixLabors.ImageSharp.ImageInfo SixLabors.ImageSharp.Image::Identify(System.IO.Stream)
stloc.0
ldarg.0
ldc.i4.0
conv.i8
callvirt System.Void System.IO.Stream::set_Position(System.Int64)
ldarg.1
brtrue.s IL_002c
ldloc.0
callvirt SixLabors.ImageSharp.Metadata.ImageMetadata SixLabors.ImageSharp.ImageInfo::get_Metadata()
call SixLabors.ImageSharp.Formats.Png.PngMetadata SixLabors.ImageSharp.MetadataExtensions::GetPngMetadata(SixLabors.ImageSharp.Metadata.ImageMetadata)
callvirt System.Nullable<SixLabors.ImageSharp.Formats.Png.PngColorType> SixLabors.ImageSharp.Formats.Png.PngMetadata::get_ColorType()
stloc.2
ldc.i4.0
stloc.3
ldloca.s 2 (SixLabors.ImageSharp.Formats.Png.PngMetadata)
call SixLabors.ImageSharp.Formats.Png.PngColorType System.Nullable<SixLabors.ImageSharp.Formats.Png.PngColorType>::GetValueOrDefault()
ldloc.3
ceq
ldloca.s 2 (SixLabors.ImageSharp.Formats.Png.PngMetadata)
call System.Boolean System.Nullable<SixLabors.ImageSharp.Formats.Png.PngColorType>::get_HasValue()
and
starg.s oneChannel (System.Boolean)
ldarg.2
brfalse.s IL_005f
ldarg.1
brfalse.s IL_0058
ldloc.0
callvirt SixLabors.ImageSharp.Formats.PixelTypeInfo SixLabors.ImageSharp.ImageInfo::get_PixelType()
callvirt System.Int32 SixLabors.ImageSharp.Formats.PixelTypeInfo::get_BitsPerPixel()
conv.u1
stloc.1
ldloc.1
ldc.i4.8
beq.s IL_004a
ldloc.1
ldc.i4.s 16
beq.s IL_0051
br.s IL_00c8
ldloc.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.L8>(SixLabors.ImageSharp.ImageInfo)
ret
ldloc.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.L16>(SixLabors.ImageSharp.ImageInfo)
ret
ldloc.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt32>::Create<SixLabors.ImageSharp.PixelFormats.Rgba32>(SixLabors.ImageSharp.ImageInfo)
ret
ldarg.1
brfalse.s IL_0088
ldloc.0
callvirt SixLabors.ImageSharp.Formats.PixelTypeInfo SixLabors.ImageSharp.ImageInfo::get_PixelType()
callvirt System.Int32 SixLabors.ImageSharp.Formats.PixelTypeInfo::get_BitsPerPixel()
conv.u1
stloc.1
ldloc.1
ldc.i4.8
beq.s IL_007a
ldloc.1
ldc.i4.s 16
beq.s IL_0081
br.s IL_00c8
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.L8>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.L16>(System.IO.Stream)
ret
ldloc.0
callvirt SixLabors.ImageSharp.Metadata.ImageMetadata SixLabors.ImageSharp.ImageInfo::get_Metadata()
call SixLabors.ImageSharp.Formats.Png.PngMetadata SixLabors.ImageSharp.MetadataExtensions::GetPngMetadata(SixLabors.ImageSharp.Metadata.ImageMetadata)
stloc.2
ldloc.2
callvirt SixLabors.ImageSharp.Formats.Png.PngColorType SixLabors.ImageSharp.Formats.Png.PngMetadata::get_ColorType()
brtrue.s IL_00c1
ldloc.2
callvirt SixLabors.ImageSharp.Formats.Png.PngBitDepth SixLabors.ImageSharp.Formats.Png.PngMetadata::get_BitDepth()
stloc.1
ldloc.1
ldc.i4.8
beq.s IL_00b3
ldloc.1
ldc.i4.s 16
beq.s IL_00ba
br.s IL_00c8
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.L8>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.L16>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt32>::Create<SixLabors.ImageSharp.PixelFormats.Rgba32>(System.IO.Stream)
ret
ldnull
ret
