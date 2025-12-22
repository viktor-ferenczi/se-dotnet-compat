// 61d6f2d8
ldarg.0
call SixLabors.ImageSharp.IImageInfo SixLabors.ImageSharp.Image::Identify(System.IO.Stream)
stloc.0
ldarg.0
ldc.i4.0
conv.i8
callvirt System.Void System.IO.Stream::set_Position(System.Int64)
ldarg.1
brtrue.s IL_002c
ldloc.0
callvirt SixLabors.ImageSharp.MetaData.ImageMetaData SixLabors.ImageSharp.IImageInfo::get_MetaData()
call SixLabors.ImageSharp.Formats.Png.PngFormat SixLabors.ImageSharp.Formats.Png.PngFormat::get_Instance()
callvirt !!0 SixLabors.ImageSharp.MetaData.ImageMetaData::GetFormatMetaData<SixLabors.ImageSharp.Formats.Png.PngMetaData>(SixLabors.ImageSharp.Formats.IImageFormat<!!0>)
callvirt SixLabors.ImageSharp.Formats.Png.PngColorType SixLabors.ImageSharp.Formats.Png.PngMetaData::get_ColorType()
ldc.i4.0
ceq
starg.s oneChannel (System.Boolean)
ldarg.2
brfalse.s IL_005f
ldarg.1
brfalse.s IL_0058
ldloc.0
callvirt SixLabors.ImageSharp.Formats.PixelTypeInfo SixLabors.ImageSharp.IImageInfo::get_PixelType()
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
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.Gray8>(SixLabors.ImageSharp.IImageInfo)
ret
ldloc.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.Gray16>(SixLabors.ImageSharp.IImageInfo)
ret
ldloc.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt32>::Create<SixLabors.ImageSharp.PixelFormats.Rgba32>(SixLabors.ImageSharp.IImageInfo)
ret
ldarg.1
brfalse.s IL_0088
ldloc.0
callvirt SixLabors.ImageSharp.Formats.PixelTypeInfo SixLabors.ImageSharp.IImageInfo::get_PixelType()
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
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.Gray8>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.Gray16>(System.IO.Stream)
ret
ldloc.0
callvirt SixLabors.ImageSharp.MetaData.ImageMetaData SixLabors.ImageSharp.IImageInfo::get_MetaData()
call SixLabors.ImageSharp.Formats.Png.PngFormat SixLabors.ImageSharp.Formats.Png.PngFormat::get_Instance()
callvirt !!0 SixLabors.ImageSharp.MetaData.ImageMetaData::GetFormatMetaData<SixLabors.ImageSharp.Formats.Png.PngMetaData>(SixLabors.ImageSharp.Formats.IImageFormat<!!0>)
stloc.2
ldloc.2
callvirt SixLabors.ImageSharp.Formats.Png.PngColorType SixLabors.ImageSharp.Formats.Png.PngMetaData::get_ColorType()
brtrue.s IL_00c1
ldloc.2
callvirt SixLabors.ImageSharp.Formats.Png.PngBitDepth SixLabors.ImageSharp.Formats.Png.PngMetaData::get_BitDepth()
stloc.1
ldloc.1
ldc.i4.8
beq.s IL_00b3
ldloc.1
ldc.i4.s 16
beq.s IL_00ba
br.s IL_00c8
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.Byte>::Create<SixLabors.ImageSharp.PixelFormats.Gray8>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt16>::Create<SixLabors.ImageSharp.PixelFormats.Gray16>(System.IO.Stream)
ret
ldarg.0
call VRage.Render.Image.MyImage<TData> VRage.Render.Image.MyImage<System.UInt32>::Create<SixLabors.ImageSharp.PixelFormats.Rgba32>(System.IO.Stream)
ret
ldnull
ret
