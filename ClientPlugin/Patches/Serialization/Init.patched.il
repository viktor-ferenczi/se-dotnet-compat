// 3ee0349c
ldarg.0
ldarg.2
stfld System.Xml.XmlWriter VRage.CustomRootWriter::m_target
ldarg.0
ldarg.1
stfld System.String VRage.CustomRootWriter::m_customRootType
ldarg.0
ldfld System.Xml.XmlWriter VRage.CustomRootWriter::m_target
ldstr "xsi"
ldstr "type"
ldstr "http://www.w3.org/2001/XMLSchema-instance"
ldarg.0
ldfld System.String VRage.CustomRootWriter::m_customRootType
callvirt System.Void System.Xml.XmlWriter::WriteAttributeString(System.String, System.String, System.String, System.String)
ldarg.0
ldc.i4.0
stfld System.Int32 VRage.CustomRootWriter::m_currentDepth
ret
