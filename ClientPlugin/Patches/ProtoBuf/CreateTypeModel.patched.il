// 229008e8
ldarg.0
ldc.i4.1
call static ProtoBuf.Meta.RuntimeTypeModel ProtoBuf.Meta.RuntimeTypeModel::Create(System.Boolean setDefault)
stfld ProtoBuf.Meta.RuntimeTypeModel VRage.Platform.Windows.Serialization.DynamicTypeModel::m_typeModel
ldarg.0
ldfld ProtoBuf.Meta.RuntimeTypeModel VRage.Platform.Windows.Serialization.DynamicTypeModel::m_typeModel
ldc.i4.1
callvirt System.Void ProtoBuf.Meta.RuntimeTypeModel::set_AutoAddMissingTypes(System.Boolean value)
ldarg.0
ldfld ProtoBuf.Meta.RuntimeTypeModel VRage.Platform.Windows.Serialization.DynamicTypeModel::m_typeModel
ldc.i4.0
callvirt System.Void ProtoBuf.Meta.RuntimeTypeModel::set_UseImplicitZeroDefaults(System.Boolean value)
ret
