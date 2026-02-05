// 02faba71
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
call static System.Boolean VRage.FileSystem.MyFileSystem::FileExists(System.String path)
brtrue.s L0
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
ldstr "vox"
call static System.String System.IO.Path::ChangeExtension(System.String path, System.String extension)
stloc.2
ldarg.0
ldfld System.Boolean Sandbox.Engine.Voxels.<>c__DisplayClass106_0::logInfo
brfalse.s L1
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "    Loading voxel \'{0}\'"
ldloc.2
call static System.String System.String::Format(System.String format, System.Object arg0)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L1:
ldloc.2
call static System.Boolean VRage.FileSystem.MyFileSystem::FileExists(System.String path)
brtrue.s L2
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
ldstr ".vx2"
callvirt System.Int32 System.String::LastIndexOf(System.String value)
stloc.3
ldloc.3
ldc.i4.m1
beq.s L3
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
ldloc.3
callvirt System.String System.String::Remove(System.Int32 startIndex)
stloc.2
ldloc.2
call static System.Boolean VRage.FileSystem.MyFileSystem::FileExists(System.String path)
brtrue.s L4
ldnull
ret
L3:
ldnull
ret
L2:
L4:
ldloc.2
call static System.Void Sandbox.Engine.Voxels.MyStorageBase::UpdateFileFormat(System.String originalVoxFile)
br.s L5
L0:
ldarg.0
ldfld System.Boolean Sandbox.Engine.Voxels.<>c__DisplayClass106_0::logInfo
brfalse.s L6
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "    Loading voxel \'{0}\'"
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
call static System.String System.String::Format(System.String format, System.Object arg0)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
L5:
L6:
ldnull
stloc.0
ldarg.0
ldfld System.String Sandbox.Engine.Voxels.<>c__DisplayClass106_0::absoluteFilePath
call static System.IO.Stream VRage.FileSystem.MyFileSystem::OpenRead(System.String path)
stloc.s 4 (System.IO.Stream)
[EX_BeginException]
ldloc.s 4 (System.IO.Stream)
callvirt abstract virtual System.Int64 System.IO.Stream::get_Length()
conv.ovf.i
newarr System.Byte
stloc.0
ldloc.s 4 (System.IO.Stream)
ldloc.0
ldc.i4.0
ldloc.0
ldlen
conv.i4
call static System.Int32 ClientPlugin.Patches.Serialization.StreamReadPatch::ReplacementRead(System.IO.Stream stream, System.Byte[] array, System.Int32 offset, System.Int32 count)
pop
leave.s L7
[EX_BeginFinally]
ldloc.s 4 (System.IO.Stream)
brfalse.s L8
ldloc.s 4 (System.IO.Stream)
callvirt abstract virtual System.Void System.IDisposable::Dispose()
L8:
[EX_EndException]
endfinally
L7:
ldloc.0
ldloca.s 5 (System.Boolean)
call static Sandbox.Engine.Voxels.MyStorageBase Sandbox.Engine.Voxels.MyStorageBase::Load(System.Byte[] memoryBuffer, System.Boolean& isOldFormat)
stloc.1
ldarg.0
ldflda Sandbox.Engine.Voxels.MyVoxelObjectDefinition Sandbox.Engine.Voxels.<>c__DisplayClass106_0::definition
ldfld System.Collections.Generic.Dictionary`2<System.Byte, System.Byte> Sandbox.Engine.Voxels.MyVoxelObjectDefinition::Changes
brfalse.s L9
ldloc.1
ldarg.0
ldflda Sandbox.Engine.Voxels.MyVoxelObjectDefinition Sandbox.Engine.Voxels.<>c__DisplayClass106_0::definition
ldfld System.Collections.Generic.Dictionary`2<System.Byte, System.Byte> Sandbox.Engine.Voxels.MyVoxelObjectDefinition::Changes
callvirt System.Void Sandbox.Engine.Voxels.MyStorageBase::ChangeMaterials(System.Collections.Generic.Dictionary`2<System.Byte, System.Byte> map)
L9:
ldloc.1
ret
