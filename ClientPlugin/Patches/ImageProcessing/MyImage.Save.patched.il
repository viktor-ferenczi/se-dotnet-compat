// efd27210
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::X
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::Y
mul
newarr TPixel
stloc.0
ldloca.s 1 (System.Memory<TPixel>)
ldloc.0
call System.Void System.Memory<TPixel>::.ctor(!0[])
ldloca.s 1 (System.Memory<TPixel>)
call System.Buffers.MemoryHandle System.Memory<TPixel>::Pin()
stloc.2
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::X
ldarg.s bytesPerPixel (System.UInt32)
mul
stloc.3
ldloca.s 2 (System.Buffers.MemoryHandle)
call System.Void* System.Buffers.MemoryHandle::get_Pointer()
stloc.s 4 (System.Byte*)
ldarga.s dataPointer (System.IntPtr)
call System.Void* System.IntPtr::ToPointer()
stloc.s 5 (System.Byte*)
ldc.i4.0
stloc.s 6 (System.Int32)
br.s IL_0064
ldloc.s 4 (System.Byte*)
ldloc.s 5 (System.Byte*)
ldloc.3
call System.Void System.Runtime.CompilerServices.Unsafe::CopyBlockUnaligned(System.Void*, System.Void*, System.UInt32)
ldloc.s 4 (System.Byte*)
ldloc.3
conv.u
add
stloc.s 4 (System.Byte*)
ldloc.s 5 (System.Byte*)
ldarg.3
add
stloc.s 5 (System.Byte*)
ldloc.s 6 (System.Int32)
ldc.i4.1
add
stloc.s 6 (System.Int32)
ldloc.s 6 (System.Int32)
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::Y
blt.s IL_0047
leave.s IL_007f
ldloca.s 2 (System.Buffers.MemoryHandle)
constrained. System.Buffers.MemoryHandle
callvirt System.Void System.IDisposable::Dispose()
endfinally
ldloc.1
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::X
ldarg.s size (VRageMath.Vector2I)
ldfld System.Int32 VRageMath.Vector2I::Y
call SixLabors.ImageSharp.Image<!!0> SixLabors.ImageSharp.Image::WrapMemory<TPixel>(System.Memory<!!0>, System.Int32, System.Int32)
stloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
ldarg.1
switch IL_00a9, IL_00bd, IL_00b3
br.s IL_00c7
ldloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
ldarg.0
call System.Void SixLabors.ImageSharp.ImageExtensions::SaveAsPng<TPixel>(SixLabors.ImageSharp.Image<!!0>, System.IO.Stream)
leave.s IL_00de
ldloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
ldarg.0
call System.Void SixLabors.ImageSharp.ImageExtensions::SaveAsBmp<TPixel>(SixLabors.ImageSharp.Image<!!0>, System.IO.Stream)
leave.s IL_00de
ldloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
ldarg.0
call System.Void SixLabors.ImageSharp.ImageExtensions::SaveAsJpeg<TPixel>(SixLabors.ImageSharp.Image<!!0>, System.IO.Stream)
leave.s IL_00de
ldstr "Unknown image format."
newobj System.Void System.NotImplementedException::.ctor(System.String)
throw
ldloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
brfalse.s IL_00dd
ldloc.s 7 (SixLabors.ImageSharp.Image<TPixel>)
callvirt System.Void System.IDisposable::Dispose()
endfinally
ret
