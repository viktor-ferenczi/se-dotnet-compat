// c8bac690
IL_0000: ldarg.0
IL_0001: ldarg.2
IL_0002: stfld System.Xml.XmlWriter VRage.CustomRootWriter::m_target
IL_0007: ldarg.0
IL_0008: ldarg.1
IL_0009: stfld System.String VRage.CustomRootWriter::m_customRootType
IL_0014: ldarg.0
IL_0015: ldfld System.Xml.XmlWriter VRage.CustomRootWriter::m_target
IL_0020: ldstr "xsi:type"
IL_0025: ldarg.0
IL_0026: ldfld System.String VRage.CustomRootWriter::m_customRootType
IL_0031: callvirt System.Void System.Xml.XmlWriter::WriteAttributeString(System.String, System.String)
IL_0036: ldarg.0
IL_0037: ldc.i4.0
IL_0038: stfld System.Int32 VRage.CustomRootWriter::m_currentDepth
IL_0043: ret
