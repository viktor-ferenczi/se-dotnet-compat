// 69c1a812
call static System.Int32 System.Runtime.CompilerServices.Unsafe::SizeOf()
newarr System.Byte
stloc.0
ldarg.0
ldloc.0
ldc.i4.0
ldloc.0
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.0
dup
stloc.s 4 (System.Byte[])
brfalse.s L0
ldloc.s 4 (System.Byte[])
ldlen
conv.i4
brtrue.s L1
L0:
ldc.i4.0
conv.u
stloc.3
br.s L2
L1:
ldloc.s 4 (System.Byte[])
ldc.i4.0
ldelema System.Byte
conv.u
stloc.3
L2:
ldloc.3
call static System.IntPtr System.IntPtr::op_Explicit(System.Void* value)
call static VRage.Audio.ChunkHeader System.Runtime.InteropServices.Marshal::PtrToStructure(System.IntPtr ptr)
stloc.1
ldnull
stloc.s 4 (System.Byte[])
ldloc.1
ldfld System.UInt32 VRage.Audio.ChunkHeader::ChunkSize
newarr System.Byte
stloc.2
ldarg.0
ldloc.2
ldc.i4.0
ldloc.1
ldfld System.UInt32 VRage.Audio.ChunkHeader::ChunkSize
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
ldloc.2
dup
stloc.s 4 (System.Byte[])
brfalse.s L3
ldloc.s 4 (System.Byte[])
ldlen
conv.i4
brtrue.s L4
L3:
ldc.i4.0
conv.u
stloc.s 5 (System.Byte*)
br.s L5
L4:
ldloc.s 4 (System.Byte[])
ldc.i4.0
ldelema System.Byte
conv.u
stloc.s 5 (System.Byte*)
L5:
ldloc.s 5 (System.Byte*)
call static System.IntPtr System.IntPtr::op_Explicit(System.Void* value)
ldtoken VRage.Audio.MySoundStream+At9WaveFormat+FactChunk
call static System.Type System.Type::GetTypeFromHandle(System.RuntimeTypeHandle handle)
call static System.Object System.Runtime.InteropServices.Marshal::PtrToStructure(System.IntPtr ptr, System.Type structureType)
unbox.any VRage.Audio.MySoundStream+At9WaveFormat+FactChunk
ret
