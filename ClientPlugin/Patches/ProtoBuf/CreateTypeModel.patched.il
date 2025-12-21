// c5f21c05
ldarg.0
ldnull
call static ProtoBuf.Meta.RuntimeTypeModel ProtoBuf.Meta.RuntimeTypeModel::Create(System.String name)
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
