// b461d631
ldarg.0
ldfld System.IO.BufferedStream VRage.MyCompressionStreamLoad::m_buffer
ldsfld System.Byte[] VRage.MyCompressionStreamLoad::m_intBytesBuffer
ldc.i4.0
ldc.i4.4
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldsfld System.Byte[] VRage.MyCompressionStreamLoad::m_intBytesBuffer
ldc.i4.0
call static System.Int32 System.BitConverter::ToInt32(System.Byte[] value, System.Int32 startIndex)
stloc.0
ldloc.0
ret
