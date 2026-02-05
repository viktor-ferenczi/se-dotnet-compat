// 28871436
call static System.Byte[] System.StreamExtensions::get_Buffer()
stloc.0
ldc.i4.0
stloc.1
ldarg.2
stloc.2
ldarg.2
ldarg.3
add
stloc.3
br.s L0
L3:
ldarg.3
ldloc.0
ldlen
conv.i4
call static System.Int32 System.Math::Min(System.Int32 val1, System.Int32 val2)
stloc.1
ldarg.0
ldloc.0
ldc.i4.0
ldloc.1
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldarg.3
ldloc.1
sub
starg.s 3
ldc.i4.0
stloc.s 4 (System.Int32)
br.s L1
L2:
ldarg.1
ldloc.2
dup
ldc.i4.1
add
stloc.2
add
ldloc.0
ldloc.s 4 (System.Int32)
ldelem.u1
stind.i1
ldloc.s 4 (System.Int32)
ldc.i4.1
add
stloc.s 4 (System.Int32)
L1:
ldloc.s 4 (System.Int32)
ldloc.1
blt.s L2
L0:
ldloc.2
ldloc.3
bne.un.s L3
ret
