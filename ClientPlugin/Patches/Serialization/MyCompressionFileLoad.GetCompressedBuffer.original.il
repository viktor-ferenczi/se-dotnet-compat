// 96ba1edf
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
ldc.i4.0
conv.i8
callvirt abstract virtual System.Void System.IO.Stream::set_Position(System.Int64 value)
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.ovf.i
newarr System.Byte
stloc.0
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
ldloc.0
ldc.i4.0
ldarg.0
ldfld System.IO.FileStream VRage.Library.Compression.MyCompressionFileLoad::m_input
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.i4
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldloc.0
ret
