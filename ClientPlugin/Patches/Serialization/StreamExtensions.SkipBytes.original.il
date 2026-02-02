// 443fe357
call static System.Byte[] System.StreamExtensions::get_Buffer()
stloc.0
br.s L0
L3:
ldarg.1
ldloc.0
ldlen
conv.i4
bgt.s L1
ldarg.1
br.s L2
L1:
ldloc.0
ldlen
conv.i4
L2:
stloc.1
ldarg.0
ldloc.0
ldc.i4.0
ldloc.1
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldarg.1
ldloc.1
sub
starg.s 1
L0:
ldarg.1
ldc.i4.0
bgt.s L3
ret
