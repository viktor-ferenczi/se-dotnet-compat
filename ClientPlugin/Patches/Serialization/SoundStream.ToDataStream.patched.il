// 871b05a6
ldarg.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.ovf.i
newarr System.Byte
stloc.0
ldarg.0
ldloc.0
ldc.i4.0
ldarg.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
conv.i8
ldarg.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
beq.s L0
ldstr "Unable to get a valid DataStream"
newobj System.Void System.InvalidOperationException::.ctor(System.String message)
throw
L0:
ldloc.0
ldc.i4.1
ldc.i4.1
ldc.i4.0
ldc.i4.1
call static SharpDX.DataStream SharpDX.DataStream::Create(System.Byte[] userBuffer, System.Boolean canRead, System.Boolean canWrite, System.Int32 index, System.Boolean pinBuffer)
ret
