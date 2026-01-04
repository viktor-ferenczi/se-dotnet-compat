// 3e1b3255
IL_0000: ldarg.3
IL_0000: brfalse.s IL_0053
IL_0000: ldarg.2
IL_0001: stloc.0
IL_0002: ldc.i4.0
IL_0003: stloc.1
IL_0004: ldloc.0
IL_0005: ldloca.s 1 (System.Boolean)
IL_0007: call System.Void System.Threading.Monitor::Enter(System.Object, System.Boolean&)
IL_0012: ldarg.2
IL_0013: ldarg.1
IL_0014: ldloca.s 2 (System.Int32)
IL_0016: callvirt System.Boolean System.Collections.Generic.Dictionary<System.String, System.Int32>::TryGetValue(!0, !1&)
IL_0021: pop
IL_0022: ldloc.2
IL_0023: ldc.i4.1
IL_0024: sub
IL_0025: stloc.2
IL_0026: ldarg.2
IL_0027: ldarg.1
IL_0028: ldloc.2
IL_0029: callvirt System.Void System.Collections.Generic.Dictionary<System.String, System.Int32>::set_Item(!0, !1)
IL_0034: ldloc.2
IL_0035: brtrue.s IL_0047
IL_0037: ldarg.2
IL_0038: ldarg.1
IL_0039: callvirt System.Boolean System.Collections.Generic.Dictionary<System.String, System.Int32>::Remove(!0)
IL_0044: pop
IL_0045: ldarg.3
IL_0046: ldarg.1
IL_0047: ldloca.s 3 (T)
IL_0049: callvirt System.Boolean System.Collections.Concurrent.ConcurrentDictionary<System.String, T>::TryRemove(!0, !1&)
IL_0054: pop
IL_0055: ldloc.3
IL_0056: box T
IL_0061: brfalse.s IL_0047
IL_0063: ldarg.s dispose (System.Action<T>)
IL_0065: ldloc.3
IL_0066: callvirt System.Void System.Action<T>::Invoke(!0)
IL_0071: leave.s IL_0053
IL_0073: ldloc.1
IL_0074: brfalse.s IL_0052
IL_0076: ldloc.0
IL_0077: call System.Void System.Threading.Monitor::Exit(System.Object)
IL_0082: endfinally
IL_0083: ret
