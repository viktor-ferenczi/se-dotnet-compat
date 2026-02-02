// 509fc85a
ldarg.1
dup
brtrue.s L0
pop
call static System.Text.Encoding System.Text.Encoding::get_UTF8()
L0:
starg.s 1
ldarg.0
call static System.Int32 System.StreamExtensions::Read7BitEncodedInt(System.IO.Stream stream)
stloc.0
call static System.Byte[] System.StreamExtensions::get_Buffer()
stloc.1
ldloc.0
ldloc.1
ldlen
conv.i4
ble.s L1
ldloc.0
newarr System.Byte
stloc.1
L1:
ldarg.0
ldloc.1
ldc.i4.0
ldloc.0
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldarg.1
ldloc.1
ldc.i4.0
ldloc.0
callvirt virtual System.String System.Text.Encoding::GetString(System.Byte[] bytes, System.Int32 index, System.Int32 count)
ret
