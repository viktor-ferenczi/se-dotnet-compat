// 30801e3d
newobj System.Void System.IO.MemoryStream::.ctor()
stloc.0
[EX_BeginException]
ldloc.0
ldc.i4.1
ldc.i4.1
newobj System.Void System.IO.Compression.GZipStream::.ctor(System.IO.Stream stream, System.IO.Compression.CompressionMode mode, System.Boolean leaveOpen)
stloc.1
[EX_BeginException]
ldloc.1
ldarg.0
ldc.i4.0
ldarg.0
ldlen
conv.i4
callvirt abstract virtual System.Void System.IO.Stream::Write(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
ldloc.1
callvirt virtual System.Void System.IO.Stream::Close()
ldloc.0
ldc.i4.0
conv.i8
callvirt abstract virtual System.Void System.IO.Stream::set_Position(System.Int64 value)
ldloc.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
ldc.i4.4
conv.i8
add
conv.ovf.i
newarr System.Byte
stloc.2
ldloc.0
ldloc.2
ldc.i4.4
ldloc.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldarg.0
ldlen
conv.i4
call static System.Byte[] System.BitConverter::GetBytes(System.Int32 value)
ldc.i4.0
ldloc.2
ldc.i4.0
ldc.i4.4
call static System.Void System.Buffer::BlockCopy(System.Array src, System.Int32 srcOffset, System.Array dst, System.Int32 dstOffset, System.Int32 count)
ldloc.2
stloc.3
leave.s L0
[EX_BeginFinally]
ldloc.1
brfalse.s L1
ldloc.1
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L1:
[EX_EndException]
endfinally
[EX_BeginFinally]
ldloc.0
brfalse.s L2
ldloc.0
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L2:
[EX_EndException]
endfinally
L0:
ldloc.3
ret
