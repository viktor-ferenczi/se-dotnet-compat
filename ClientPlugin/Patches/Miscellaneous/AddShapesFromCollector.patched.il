// b14b2107
ldc.i4.0
stloc.0
ldc.i4.0
stloc.1
br L0
L7:
ldarg.0
ldfld Sandbox.Game.Entities.Cube.MyCubeBlockCollector Sandbox.Game.Entities.Cube.MyGridShape::m_blockCollector
ldfld System.Collections.Generic.List`1<Sandbox.Game.Entities.Cube.ShapeInfo> Sandbox.Game.Entities.Cube.MyCubeBlockCollector::ShapeInfos
ldloc.1
callvirt virtual Sandbox.Game.Entities.Cube.ShapeInfo System.Collections.Generic.List`1<Sandbox.Game.Entities.Cube.ShapeInfo>::get_Item(System.Int32 index)
stloc.2
ldnull
stloc.3
ldloca.s 4 (System.Span`1[Havok.HkShape])
initobj System.Span`1[Havok.HkShape]
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
ldc.i4 256
bge.s L1
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
stloc.s 5 (System.Int32)
ldloc.s 5 (System.Int32)
conv.u
sizeof Havok.HkShape
mul.ovf.un
localloc
ldloc.s 5 (System.Int32)
newobj System.Void System.Span`1<Havok.HkShape>::.ctor(System.Void* pointer, System.Int32 length)
stloc.s 4 (System.Span`1[Havok.HkShape])
br.s L2
L1:
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
newarr Havok.HkShape
dup
stloc.3
call static System.Span`1<Havok.HkShape> System.Span`1<Havok.HkShape>::op_Implicit(Havok.HkShape[] array)
stloc.s 4 (System.Span`1[Havok.HkShape])
L2:
ldc.i4.0
stloc.s 6 (System.Int32)
br.s L3
L4:
ldloca.s 4 (System.Span`1[Havok.HkShape])
ldloc.s 6 (System.Int32)
call Havok.HkShape& System.Span`1<Havok.HkShape>::get_Item(System.Int32 index)
ldarg.0
ldfld Sandbox.Game.Entities.Cube.MyCubeBlockCollector Sandbox.Game.Entities.Cube.MyGridShape::m_blockCollector
ldfld System.Collections.Generic.List`1<Havok.HkShape> Sandbox.Game.Entities.Cube.MyCubeBlockCollector::Shapes
ldloc.0
ldloc.s 6 (System.Int32)
add
callvirt virtual Havok.HkShape System.Collections.Generic.List`1<Havok.HkShape>::get_Item(System.Int32 index)
stobj Havok.HkShape
ldloc.s 6 (System.Int32)
ldc.i4.1
add
stloc.s 6 (System.Int32)
L3:
ldloc.s 6 (System.Int32)
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
blt.s L4
ldloc.0
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
add
stloc.0
ldarg.0
ldflda Havok.HkGridShape Sandbox.Game.Entities.Cube.MyGridShape::m_root
call System.Int32 Havok.HkGridShape::get_ShapeCount()
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
add
ldc.i4 64879
ble.s L5
ldsfld Sandbox.Game.Gui.MyHudNotifications Sandbox.Game.Gui.MyHud::Notifications
ldc.i4.s 43
callvirt Sandbox.Game.Gui.MyHudNotificationBase Sandbox.Game.Gui.MyHudNotifications::Add(Sandbox.Game.Gui.MyNotificationSingletons singleNotification)
pop
L5:
ldarg.0
ldflda Havok.HkGridShape Sandbox.Game.Entities.Cube.MyGridShape::m_root
call System.Int32 Havok.HkGridShape::get_ShapeCount()
ldloc.2
ldfld System.Int32 Sandbox.Game.Entities.Cube.ShapeInfo::Count
add
ldc.i4 65536
bge.s L6
ldarg.0
ldflda Havok.HkGridShape Sandbox.Game.Entities.Cube.MyGridShape::m_root
ldloc.s 4 (System.Span`1[Havok.HkShape])
ldloc.2
ldfld VRageMath.Vector3I Sandbox.Game.Entities.Cube.ShapeInfo::Min
newobj System.Void VRageMath.Vector3S::.ctor(VRageMath.Vector3I vec)
ldloc.2
ldfld VRageMath.Vector3I Sandbox.Game.Entities.Cube.ShapeInfo::Max
newobj System.Void VRageMath.Vector3S::.ctor(VRageMath.Vector3I vec)
call System.Void Havok.HkGridShape::AddShapes(System.Span`1<Havok.HkShape> shapes, VRageMath.Vector3S min, VRageMath.Vector3S max)
L6:
ldloc.3
call static System.Void System.GC::KeepAlive(System.Object obj)
ldloc.1
ldc.i4.1
add
stloc.1
L0:
ldloc.1
ldarg.0
ldfld Sandbox.Game.Entities.Cube.MyCubeBlockCollector Sandbox.Game.Entities.Cube.MyGridShape::m_blockCollector
ldfld System.Collections.Generic.List`1<Sandbox.Game.Entities.Cube.ShapeInfo> Sandbox.Game.Entities.Cube.MyCubeBlockCollector::ShapeInfos
callvirt virtual System.Int32 System.Collections.Generic.List`1<Sandbox.Game.Entities.Cube.ShapeInfo>::get_Count()
blt L7
ret
