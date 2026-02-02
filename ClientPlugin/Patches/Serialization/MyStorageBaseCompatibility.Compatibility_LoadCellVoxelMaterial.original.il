// 633fb6de
ldnull
stloc.0
ldarg.1
call static System.Byte System.StreamExtensions::ReadByteNoAlloc(System.IO.Stream stream)
stloc.1
ldloc.1
ldc.i4 255
beq.s L0
ldarg.2
brfalse.s L1
ldarg.2
ldloc.1
callvirt virtual VRage.Game.MyVoxelMaterialDefinition System.Collections.Generic.Dictionary`2<System.Byte, VRage.Game.MyVoxelMaterialDefinition>::get_Item(System.Byte key)
stloc.0
br.s L2
L1:
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
ldloc.1
callvirt VRage.Game.MyVoxelMaterialDefinition Sandbox.Definitions.MyDefinitionManager::GetVoxelMaterialDefinition(System.Byte materialIndex)
stloc.0
br.s L3
L0:
call static System.Text.Encoding System.Text.Encoding::get_UTF8()
ldarg.1
call static System.Byte System.StreamExtensions::ReadByteNoAlloc(System.IO.Stream stream)
stloc.2
ldarg.1
ldarg.0
ldfld System.Byte[] Sandbox.Engine.Voxels.MyStorageBaseCompatibility::m_encodedNameBuffer
ldc.i4.0
ldloc.2
callvirt abstract virtual System.Int32 System.IO.Stream::Read(System.Byte[] buffer, System.Int32 offset, System.Int32 count)
pop
ldarg.0
ldfld System.Byte[] Sandbox.Engine.Voxels.MyStorageBaseCompatibility::m_encodedNameBuffer
ldc.i4.0
ldloc.2
callvirt virtual System.String System.Text.Encoding::GetString(System.Byte[] bytes, System.Int32 index, System.Int32 count)
stloc.3
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
ldloc.3
callvirt VRage.Game.MyVoxelMaterialDefinition Sandbox.Definitions.MyDefinitionManager::GetVoxelMaterialDefinition(System.String name)
stloc.0
L2:
L3:
ldloc.0
brtrue.s L4
call static Sandbox.Definitions.MyDefinitionManager Sandbox.Definitions.MyDefinitionManager::get_Static()
callvirt VRage.Game.MyVoxelMaterialDefinition Sandbox.Definitions.MyDefinitionManager::GetDefaultVoxelMaterialDefinition()
stloc.0
L4:
ldloc.0
ret
