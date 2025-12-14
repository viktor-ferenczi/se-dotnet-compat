// b9dd8a92
ldarg.0
call System.Boolean Sandbox.Game.Entities.Cube.MyGridClipboard::get_IsActive()
ldarg.0
ldc.i4.0
ldarg.0
ldfld System.Collections.Generic.List`1<Sandbox.Game.Entities.MyCubeGrid> Sandbox.Game.Entities.Cube.MyGridClipboard::m_previewGrids
ldarg.0
ldfld System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid> Sandbox.Game.Entities.Cube.MyGridClipboard::m_copiedGrids
callvirt virtual System.Void Sandbox.Game.Entities.Cube.MyGridClipboard::ChangeClipboardPreview(System.Boolean visible, System.Collections.Generic.List`1<Sandbox.Game.Entities.MyCubeGrid> previewGrids, System.Collections.Generic.List`1<VRage.Game.MyObjectBuilder_CubeGrid> copiedGrids)
ldarg.0
ldc.i4.0
call System.Void Sandbox.Game.Entities.Cube.MyGridClipboard::set_IsActive(System.Boolean value)
ldarg.0
ldfld System.Action`2<Sandbox.Game.Entities.Cube.MyGridClipboard, System.Boolean> Sandbox.Game.Entities.Cube.MyGridClipboard::Deactivated
stloc.0
brfalse.s L0
ldloc.0
brfalse.s L1
ldloc.0
ldarg.0
ldarg.1
callvirt virtual System.Void System.Action`2<Sandbox.Game.Entities.Cube.MyGridClipboard, System.Boolean>::Invoke(Sandbox.Game.Entities.Cube.MyGridClipboard arg1, System.Boolean arg2)
L0:
L1:
ldsfld Sandbox.Game.SessionComponents.Clipboard.MyClipboardComponent Sandbox.Game.SessionComponents.Clipboard.MyClipboardComponent::Static
dup
brfalse.s L4
callvirt Sandbox.Game.Entities.Cube.MyGridClipboard Sandbox.Game.SessionComponents.Clipboard.MyClipboardComponent::get_Clipboard()
ldarg.0
beq.s L2
ret
L2:
ldsfld Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::Static
brfalse.s L3
ldsfld Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::Static
ldc.i4.0
callvirt System.Void Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::set_Visible(System.Boolean value)
ldsfld Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::Static
callvirt System.Void Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::ResetSelection()
L3:
ldarg.0
ldftn System.Void Sandbox.Game.Entities.Cube.MyGridClipboard::OnCoordinateChange()
newobj System.Void System.Action::.ctor(System.Object object, System.IntPtr method)
call static System.Void Sandbox.Game.GameSystems.CoordinateSystem.MyCoordinateSystem::remove_OnCoordinateChange(System.Action value)
ret
L4:
pop
ret
