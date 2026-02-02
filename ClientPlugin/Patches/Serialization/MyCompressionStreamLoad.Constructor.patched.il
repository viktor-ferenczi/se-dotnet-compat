// 25f08442
ldarg.0
call System.Void System.Object::.ctor()
ldarg.0
ldarg.1
newobj System.Void System.IO.MemoryStream::.ctor(System.Byte[] buffer)
stfld System.IO.MemoryStream VRage.MyCompressionStreamLoad::m_input
ldarg.0
ldfld System.IO.MemoryStream VRage.MyCompressionStreamLoad::m_input
ldsfld System.Byte[] VRage.MyCompressionStreamLoad::m_intBytesBuffer
ldc.i4.0
ldc.i4.4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldarg.0
ldarg.0
ldfld System.IO.MemoryStream VRage.MyCompressionStreamLoad::m_input
ldc.i4.0
newobj System.Void System.IO.Compression.GZipStream::.ctor(System.IO.Stream stream, System.IO.Compression.CompressionMode mode)
stfld System.IO.Compression.GZipStream VRage.MyCompressionStreamLoad::m_gz
ldarg.0
ldarg.0
ldfld System.IO.Compression.GZipStream VRage.MyCompressionStreamLoad::m_gz
ldc.i4 16384
newobj System.Void System.IO.BufferedStream::.ctor(System.IO.Stream stream, System.Int32 bufferSize)
stfld System.IO.BufferedStream VRage.MyCompressionStreamLoad::m_buffer
ret
