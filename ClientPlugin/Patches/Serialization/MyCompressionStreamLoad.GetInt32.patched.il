// 47438128
ldarg.0
ldfld System.IO.BufferedStream VRage.MyCompressionStreamLoad::m_buffer
ldsfld System.Byte[] VRage.MyCompressionStreamLoad::m_intBytesBuffer
ldc.i4.0
ldc.i4.4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldsfld System.Byte[] VRage.MyCompressionStreamLoad::m_intBytesBuffer
ldc.i4.0
call static System.Int32 System.BitConverter::ToInt32(System.Byte[] value, System.Int32 startIndex)
stloc.0
ldloc.0
ret
