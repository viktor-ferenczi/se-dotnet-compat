// 44047584
ldarg.2
stloc.0
ldc.i4.0
stloc.1
ldloc.0
ldloca.s 1 (System.Boolean)
call System.Void System.Threading.Monitor::Enter(System.Object, System.Boolean&)
ldarg.2
ldarg.1
ldloca.s 2 (System.Int32)
callvirt System.Boolean System.Collections.Generic.Dictionary<System.String, System.Int32>::TryGetValue(!0, !1&)
pop
ldloc.2
ldc.i4.1
sub
stloc.2
ldarg.2
ldarg.1
ldloc.2
callvirt System.Void System.Collections.Generic.Dictionary<System.String, System.Int32>::set_Item(!0, !1)
ldloc.2
brtrue.s IL_0047
ldarg.2
ldarg.1
callvirt System.Boolean System.Collections.Generic.Dictionary<System.String, System.Int32>::Remove(!0)
pop
ldarg.3
ldarg.1
ldloca.s 3 (T)
callvirt System.Boolean System.Collections.Concurrent.ConcurrentDictionary<System.String, T>::TryRemove(!0, !1&)
pop
ldloc.3
box T
brfalse.s IL_0047
ldarg.s dispose (System.Action<T>)
ldloc.3
callvirt System.Void System.Action<T>::Invoke(!0)
leave.s IL_0053
ldloc.1
brfalse.s IL_0052
ldloc.0
call System.Void System.Threading.Monitor::Exit(System.Object)
endfinally
ret
