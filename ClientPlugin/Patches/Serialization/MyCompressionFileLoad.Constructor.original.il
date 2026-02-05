// 60d6b667
ldarg.0
call System.Void System.Object::.ctor()
ldsfld System.Byte[] VRage.Library.Compression.MyCompressionFileLoad::m_intBytesBuffer
brtrue.s L0
ldc.i4.4
newarr System.Byte
stsfld System.Byte[] VRage.Library.Compression.MyCompressionFileLoad::m_intBytesBuffer
L0:
ldarg.0
ldarg.1
call static System.IO.FileStream System.IO.File::OpenRead(System.String path)
stfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
ldsfld System.Byte[] VRage.Library.Compression.MyCompressionFileLoad::m_intBytesBuffer
ldc.i4.0
ldc.i4.4
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldarg.0
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
ldc.i4.0
newobj System.Void System.IO.Compression.GZipStream::.ctor(System.IO.Stream stream, System.IO.Compression.CompressionMode mode)
stfld System.IO.Compression.GZipStream VRage.Library.Compression.MyCompressionFileLoad::m_gz
ldarg.0
ldarg.0
ldfld System.IO.Compression.GZipStream VRage.Library.Compression.MyCompressionFileLoad::m_gz
ldc.i4 16384
newobj System.Void System.IO.BufferedStream::.ctor(System.IO.Stream stream, System.Int32 bufferSize)
stfld System.IO.BufferedStream VRage.Library.Compression.MyCompressionFileLoad::m_buffer
ret
