// d898fe86
ldarg.0
ldc.i4.1
sub
switch IL_0185, IL_0185, IL_0185, IL_0185, IL_0258, IL_0258, IL_0258, IL_0258, IL_01a2, IL_0194, IL_01a2, IL_01a2, IL_01a2, IL_01a2, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_01b0, IL_01b0, IL_01b0, IL_0258, IL_01be, IL_01be, IL_01be, IL_01be, IL_01be, IL_01be, IL_01da, IL_01cc, IL_01da, IL_01da, IL_01da, IL_01da, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_01f6, IL_01f6, IL_01f6, IL_01f6, IL_01f6, IL_0212, IL_0204, IL_0212, IL_0212, IL_0212, IL_0212, IL_0212, IL_024a, IL_024a, IL_024a, IL_024a, IL_024a, IL_024a, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0258, IL_0220, IL_022e, IL_01e8, IL_01e8, IL_01b0, IL_01e8, IL_01e8, IL_01e8, IL_01e8
ldarg.0
ldc.i4.s 115
beq IL_023c
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.s 16
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.RgbaVector>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.8
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.HalfVector4>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.8
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Rgba64>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.4
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Rgba1010102>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.4
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Rgba32>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.4
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.HalfVector2>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.4
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Rg32>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.4
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Bgra32>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.NormalizedByte2>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.HalfSingle>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Gray16>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Bgr565>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Bgra5551>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.2
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Bgra4444>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ldarg.1
ldarg.2
ldarg.3
ldarg.s srcPitch (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldc.i4.1
call System.Void VRage.Render.Image.MyImage::Save<SixLabors.ImageSharp.PixelFormats.Gray8>(System.IO.Stream, FileFormat, System.IntPtr, System.Int32, VRageMath.Vector2I, System.UInt32)
ret
ret
