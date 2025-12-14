// c7a1dee7
ldc.i4.1
stsfld System.Boolean VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::ENABLE_PROTOBUFFERS_CLONING
ldc.i4.2
ldc.i4.0
ldc.i4.0
ldnull
ldftn static System.Void VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::SerializeDynamic(VRage.Library.Collections.BitStream stream, System.Type baseType, System.Type& obj)
newobj System.Void VRage.Library.Collections.DynamicSerializerDelegate::.ctor(System.Object object, System.IntPtr method)
ldnull
ldnull
newobj System.Void VRage.Serialization.MySerializeInfo::.ctor(VRage.Serialization.MyObjectFlags flags, VRage.Serialization.MyPrimitiveFlags primitiveFlags, System.UInt16 fixedLength, VRage.Library.Collections.DynamicSerializerDelegate dynamicSerializer, VRage.Serialization.MySerializeInfo keyInfo, VRage.Serialization.MySerializeInfo itemInfo)
stsfld VRage.Serialization.MySerializeInfo VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::Dynamic
call static VRage.IVRagePlatform VRage.MyVRage::get_Platform()
dup
brtrue.s L0
pop
ldnull
br.s L1
L0:
callvirt abstract virtual VRage.Serialization.IProtoTypeModel VRage.IVRagePlatform::GetTypeModel()
L1:
stsfld VRage.Serialization.IProtoTypeModel VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::m_typeModel
ldsfld VRage.Serialization.IProtoTypeModel VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::m_typeModel
dup
brtrue.s L2
pop
ldnull
br.s L3
L2:
callvirt abstract virtual ProtoBuf.Meta.TypeModel VRage.Serialization.IProtoTypeModel::get_Model()
L3:
stsfld ProtoBuf.Meta.TypeModel VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::Serializer
newobj System.Void VRage.ObjectBuilders.MyObjectFactory`2<VRage.ObjectBuilders.MyObjectBuilderDefinitionAttribute, VRage.ObjectBuilders.MyObjectBuilder_Base>::.ctor()
stsfld VRage.ObjectBuilders.MyObjectFactory`2<VRage.ObjectBuilders.MyObjectBuilderDefinitionAttribute, VRage.ObjectBuilders.MyObjectBuilder_Base> VRage.ObjectBuilders.Private.MyObjectBuilderSerializerKeen::m_objectFactory
ret
