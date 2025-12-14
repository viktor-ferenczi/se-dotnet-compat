// c856885a
newobj System.Void Sandbox.<>c__DisplayClass11_0::.ctor()
stloc.0
ldsfld System.Func`1<System.Int32> Sandbox.<>c::<>9__11_0
dup
brtrue.s L0
pop
ldsfld Sandbox.<>c Sandbox.<>c::<>9
ldftn System.Int32 Sandbox.<>c::<InitExceptionHandling>b__11_0()
newobj System.Void System.Func`1<System.Int32>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Func`1<System.Int32> Sandbox.<>c::<>9__11_0
L0:
call static System.Void Havok.HkCrashHunter::Init(System.Func`1<System.Int32> getFrameCallback)
call static System.Threading.Thread System.Threading.Thread::get_CurrentThread()
callvirt System.String System.Threading.Thread::get_Name()
call static System.Boolean System.String::IsNullOrEmpty(System.String value)
brfalse.s L1
call static System.Threading.Thread System.Threading.Thread::get_CurrentThread()
ldstr "Main thread"
callvirt System.Void System.Threading.Thread::set_Name(System.String value)
br L7
L1:
ldloc.0
call static VRage.IMyCrashReporting Sandbox.MyInitializer::get_ErrorPlatform()
call static VRage.Utils.MyLog VRage.Utils.MyLog::get_Default()
callvirt abstract virtual VRage.IMySimplifiedErrorReporter VRage.IMyCrashReporting::TryToCreateSimplifiedErrorReporter(VRage.Utils.MyLog defaultLog)
stfld VRage.IMySimplifiedErrorReporter Sandbox.<>c__DisplayClass11_0::simplifiedErrorReporter
ldloc.0
ldfld VRage.IMySimplifiedErrorReporter Sandbox.<>c__DisplayClass11_0::simplifiedErrorReporter
brfalse.s L2
call static System.AppDomain System.AppDomain::get_CurrentDomain()
ldloc.0
ldftn System.Void Sandbox.<>c__DisplayClass11_0::<InitExceptionHandling>g__UnhandledManagedException|1(System.Object sender, System.UnhandledExceptionEventArgs args)
newobj System.Void System.UnhandledExceptionEventHandler::.ctor(System.Object object, System.IntPtr method)
callvirt System.Void System.AppDomain::add_UnhandledException(System.UnhandledExceptionEventHandler value)
ret
L2:
call static System.AppDomain System.AppDomain::get_CurrentDomain()
ldnull
ldftn static System.Void Sandbox.MyInitializer::UnhandledExceptionHandler(System.Object sender, System.UnhandledExceptionEventArgs args)
newobj System.Void System.UnhandledExceptionEventHandler::.ctor(System.Object object, System.IntPtr method)
callvirt System.Void System.AppDomain::add_UnhandledException(System.UnhandledExceptionEventHandler value)
call static VRage.IMyCrashReporting Sandbox.MyInitializer::get_ErrorPlatform()
ldsfld System.Action`1<System.IntPtr> Sandbox.<>c::<>9__11_2
dup
brtrue.s L3
pop
ldsfld Sandbox.<>c Sandbox.<>c::<>9
ldftn System.Void Sandbox.<>c::<InitExceptionHandling>b__11_2(System.IntPtr x)
newobj System.Void System.Action`1<System.IntPtr>::.ctor(System.Object object, System.IntPtr method)
dup
stsfld System.Action`1<System.IntPtr> Sandbox.<>c::<>9__11_2
L3:
callvirt abstract virtual System.Void VRage.IMyCrashReporting::SetNativeExceptionHandler(System.Action`1<System.IntPtr> handler)
L7:
call static System.Threading.Thread System.Threading.Thread::get_CurrentThread()
call static System.Globalization.CultureInfo System.Globalization.CultureInfo::get_InvariantCulture()
callvirt System.Void System.Threading.Thread::set_CurrentCulture(System.Globalization.CultureInfo value)
call static System.Threading.Thread System.Threading.Thread::get_CurrentThread()
call static System.Globalization.CultureInfo System.Globalization.CultureInfo::get_InvariantCulture()
callvirt System.Void System.Threading.Thread::set_CurrentUICulture(System.Globalization.CultureInfo value)
ldsfld System.Boolean Sandbox.Engine.Utils.MyFakes::ENABLE_MINIDUMP_SENDING
brfalse.s L4
call static System.Boolean VRage.FileSystem.MyFileSystem::get_IsInitialized()
brfalse.s L5
ldsfld System.Boolean Sandbox.Engine.Utils.MyFakes::COLLECT_SUSPEND_DUMPS
brfalse.s L6
call static VRage.IVRagePlatform VRage.MyVRage::get_Platform()
callvirt abstract virtual VRage.IVRageRender VRage.IVRagePlatform::get_Render()
ldnull
ldftn static System.Void VRage.MyMiniDump::CollectStateDump()
newobj System.Void System.Action::.ctor(System.Object object, System.IntPtr method)
callvirt abstract virtual System.Void VRage.IVRageRender::add_OnSuspending(System.Action value)
L6:
call static System.Void VRage.MyMiniDump::CleanupOldDumps()
br L8
L4:
L5:
call static VRage.IMyCrashReporting Sandbox.MyInitializer::get_ErrorPlatform()
callvirt abstract virtual System.Void VRage.IMyCrashReporting::CleanupCrashAnalytics()
call static System.Void Sandbox.MyErrorReporter::UpdateHangAnalytics()
L8:
ret
