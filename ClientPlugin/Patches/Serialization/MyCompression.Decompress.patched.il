// 9fe86a9f
newobj System.Void System.IO.MemoryStream::.ctor()
stloc.0
[EX_BeginException]
ldarg.0
ldc.i4.0
call static System.Int32 System.BitConverter::ToInt32(System.Byte[] value, System.Int32 startIndex)
stloc.1
ldloc.0
ldarg.0
ldc.i4.4
ldarg.0
ldlen
conv.i4
ldc.i4.4
sub
callvirt abstract virtual System.Void System.IO.Stream::Write(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
ldloc.0
ldc.i4.0
conv.i8
callvirt abstract virtual System.Void System.IO.Stream::set_Position(System.Int64 value)
ldloc.1
newarr System.Byte
stloc.2
ldloc.0
ldc.i4.0
newobj System.Void System.IO.Compression.GZipStream::.ctor(System.IO.Stream stream, System.IO.Compression.CompressionMode mode)
stloc.3
[EX_BeginException]
ldloc.3
ldloc.2
ldc.i4.0
ldloc.2
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.2
stloc.s 4 (System.Byte[])
leave.s L0
[EX_BeginFinally]
ldloc.3
brfalse.s L1
ldloc.3
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
ldloc.s 4 (System.Byte[])
ret
