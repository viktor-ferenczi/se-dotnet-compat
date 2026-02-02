// d41525c1
newobj System.Void System.IO.MemoryStream::.ctor()
stloc.0
[EX_BeginException]
ldarg.0
call static System.IO.FileStream System.IO.File::OpenRead(System.String path)
stloc.1
[EX_BeginException]
ldloc.1
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldc.i4.4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.1
ldc.i4.0
newobj System.Void System.IO.Compression.GZipStream::.ctor(System.IO.Stream stream, System.IO.Compression.CompressionMode mode)
stloc.2
[EX_BeginException]
ldloc.2
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
stloc.3
br.s L0
L1:
ldloc.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldloc.3
callvirt abstract virtual System.Void System.IO.Stream::Write(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
ldloc.2
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
stloc.3
L0:
ldloc.3
ldc.i4.0
bgt.s L1
leave.s L2
[EX_BeginFinally]
ldloc.2
brfalse.s L3
ldloc.2
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L3:
[EX_EndException]
endfinally
[EX_BeginFinally]
ldloc.1
brfalse.s L4
ldloc.1
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L4:
[EX_EndException]
endfinally
L2:
ldloc.0
ldc.i4.0
conv.i8
callvirt abstract virtual System.Void System.IO.Stream::set_Position(System.Int64 value)
ldarg.0
call static System.IO.FileStream System.IO.File::Create(System.String path)
stloc.s 4 (System.IO.FileStream)
[EX_BeginException]
ldloc.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
stloc.s 5 (System.Int32)
br.s L5
L6:
ldloc.s 4 (System.IO.FileStream)
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldloc.s 5 (System.Int32)
callvirt abstract virtual System.Void System.IO.Stream::Write(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
ldloc.s 4 (System.IO.FileStream)
callvirt abstract virtual System.Void System.IO.Stream::Flush()
ldloc.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldc.i4.0
ldsfld System.Byte[] VRage.MyCompression::m_buffer
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
stloc.s 5 (System.Int32)
L5:
ldloc.s 5 (System.Int32)
ldc.i4.0
bgt.s L6
leave.s L7
[EX_BeginFinally]
ldloc.s 4 (System.IO.FileStream)
brfalse.s L8
ldloc.s 4 (System.IO.FileStream)
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L8:
[EX_EndException]
endfinally
[EX_BeginFinally]
ldloc.0
brfalse.s L9
ldloc.0
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L9:
[EX_EndException]
endfinally
L7:
ret
