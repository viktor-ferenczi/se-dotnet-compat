// 484b3d3e
ldarg.0
call SixLabors.ImageSharp.Image<!!0> SixLabors.ImageSharp.Image::Load<TImage>(System.IO.Stream)
stloc.0
ldloc.0
call SixLabors.ImageSharp.Memory.IMemoryGroup<TImage> SixLabors.ImageSharp.Advanced.AdvancedImageExtensions::GetPixelMemoryGroup<TImage>(SixLabors.ImageSharp.Image<TImage>)
call System.Memory<TImage> System.Linq.Enumerable::Single<System.Memory<TImage>>(System.Collections.Generic.IEnumerable<System.Memory<TImage>>)
stloc 4 (System.Memory<TImage>)
ldloca 4 (System.Memory<TImage>)
call System.Span<TImage> System.Memory<TImage>::get_Span()
call System.Span<!!1> System.Runtime.InteropServices.MemoryMarshal::Cast<TImage, TData>(System.Span<!!0>)
stloc.1
ldloca.s 1 (System.Span<TData>)
call !0[] System.Span<TData>::ToArray()
stloc.2
newobj System.Void VRage.Render.Image.MyImage<TData>::.ctor()
dup
ldloc.0
callvirt System.Int32 SixLabors.ImageSharp.Image<TImage>::get_Width()
ldloc.0
callvirt System.Int32 SixLabors.ImageSharp.Image<TImage>::get_Height()
newobj System.Void VRageMath.Vector2I::.ctor(System.Int32, System.Int32)
callvirt System.Void VRage.Render.Image.MyImage<TData>::set_Size(VRageMath.Vector2I)
dup
ldloc.0
callvirt System.Int32 SixLabors.ImageSharp.Image<TImage>::get_Width()
callvirt System.Void VRage.Render.Image.MyImage<TData>::set_Stride(System.Int32)
dup
ldloc.2
callvirt System.Void VRage.Render.Image.MyImage<TData>::set_Data(TData[])
stloc.3
leave.s IL_0057
ldloc.0
brfalse.s IL_0056
ldloc.0
callvirt System.Void System.IDisposable::Dispose()
endfinally
ldloc.3
ret
