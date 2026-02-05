// a0cc6331
ldc.i4.4
call static System.Int32 SharpDX.Utilities::SizeOf()
add
call static System.Int32 SharpDX.Utilities::SizeOf()
add
stloc.0
ldarg.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
ldloc.0
conv.i8
bge.s L0
ldarg.1
initobj SharpDX.Toolkit.Graphics.ImageDescription
ldarg.2
ldc.i4.0
stind.i4
ldc.i4.0
ret
L0:
ldloc.0
newarr System.Byte
stloc.1
ldarg.0
ldloc.1
ldc.i4.0
ldloc.0
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldloc.1
dup
stloc.3
brfalse.s L1
ldloc.3
ldlen
conv.i4
brtrue.s L2
L1:
ldc.i4.0
conv.u
stloc.2
br.s L3
L2:
ldloc.3
ldc.i4.0
ldelema System.Byte
conv.u
stloc.2
L3:
ldloc.2
newobj System.Void System.IntPtr::.ctor(System.Void* value)
ldloc.0
ldc.i4.0
ldarg.1
ldarg.2
call static System.Boolean SharpDX.Toolkit.Graphics.DDSHelper::DecodeDDSHeader(System.IntPtr headerPtr, System.Int32 size, SharpDX.Toolkit.Graphics.DDSFlags flags, SharpDX.Toolkit.Graphics.ImageDescription& description, SharpDX.Toolkit.Graphics.ConversionFlags& convFlags)
ret
