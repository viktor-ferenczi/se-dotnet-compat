// 82d62957
ldarg.0
call System.Void VRageRender.Import.MyModelImporter::Clear()
ldarg.1
stsfld System.String VRageRender.Import.MyModelImporter::m_debugAssetName
ldarg.1
call static System.Boolean System.IO.Path::IsPathRooted(System.String path)
brtrue.s L2
call static System.String VRage.FileSystem.MyFileSystem::get_ContentPath()
ldarg.1
call static System.String System.IO.Path::Combine(System.String path1, System.String path2)
br.s L3
L2:
ldarg.1
L3:
call static System.IO.Stream VRage.FileSystem.MyFileSystem::OpenRead(System.String path)
stloc.0
[EX_BeginException]
ldloc.0
brfalse.s L4
[EX_BeginException]
ldloc.0
stloc.1
ldloc.0
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
stloc.2
ldloc.2
ldc.i4 4096
conv.i8
ble.s L5
ldloc.2
ldc.i4 4194304
conv.i8
bge.s L6
ldloc.2
conv.i4
stloc.3
ldloc.3
newarr System.Byte
stloc.s 4 (System.Byte[])
ldloc.0
ldloc.s 4 (System.Byte[])
ldc.i4.0
ldloc.3
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldloc.s 4 (System.Byte[])
ldc.i4.0
ldloc.3
ldc.i4.0
newobj System.Void System.IO.MemoryStream::.ctor(System.Byte[] buffer, System.Int32 index, System.Int32 count, System.Boolean writable)
stloc.1
L5:
L6:
ldloc.1
newobj System.Void System.IO.BinaryReader::.ctor(System.IO.Stream input)
stloc.s 5 (System.IO.BinaryReader)
[EX_BeginException]
ldarg.0
ldloc.s 5 (System.IO.BinaryReader)
ldarg.2
call System.Void VRageRender.Import.MyModelImporter::LoadTagData(System.IO.BinaryReader reader, System.String[] tags)
leave.s L7
[EX_BeginFinally]
ldloc.s 5 (System.IO.BinaryReader)
brfalse.s L8
ldloc.s 5 (System.IO.BinaryReader)
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L8:
[EX_EndException]
endfinally
[EX_BeginFinally]
ldloc.0
callvirt virtual System.Void System.IO.Stream::Close()
[EX_EndException]
endfinally
L4:
leave.s L9
[EX_BeginFinally]
ldloc.0
brfalse.s L10
ldloc.0
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L10:
[EX_EndException]
endfinally
L7:
L9:
ret
