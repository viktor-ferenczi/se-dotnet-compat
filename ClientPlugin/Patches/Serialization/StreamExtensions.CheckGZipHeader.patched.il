// 065852d9
ldarg.0
callvirt abstract virtual System.Boolean System.IO.Stream::get_CanSeek()
brtrue.s L0
ldc.i4.0
ret
L0:
ldarg.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Position()
stloc.0
ldc.i4.2
newarr System.Byte
stloc.1
ldarg.0
ldc.i4.0
conv.i8
ldc.i4.0
callvirt abstract virtual System.Int64 System.IO.Stream::Seek(System.Int64 offset, System.IO.SeekOrigin origin)
pop
ldarg.0
ldloc.1
ldc.i4.0
ldc.i4.2
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.1
ldc.i4.0
ldelem.u1
ldc.i4.s 31
bne.un.s L1
ldloc.1
ldc.i4.1
ldelem.u1
ldc.i4 139
bne.un.s L2
ldarg.0
ldloc.0
ldc.i4.0
callvirt abstract virtual System.Int64 System.IO.Stream::Seek(System.Int64 offset, System.IO.SeekOrigin origin)
pop
ldc.i4.1
ret
L1:
L2:
ldarg.0
ldloc.0
ldc.i4.0
callvirt abstract virtual System.Int64 System.IO.Stream::Seek(System.Int64 offset, System.IO.SeekOrigin origin)
pop
ldc.i4.0
ret
