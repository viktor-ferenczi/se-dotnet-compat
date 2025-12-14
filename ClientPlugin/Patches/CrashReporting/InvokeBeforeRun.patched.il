// 98bb3e92
ldarg.1
stsfld System.String Sandbox.MyInitializer::m_appName
ldarg.2
call static System.Boolean System.String::IsNullOrEmpty(System.String value)
brtrue.s L0
ldarg.2
stsfld System.String VRage.FileSystem.MyFileSystem::RootPath
L0:
ldarg.3
ldarg.s 7
call static System.Void Sandbox.MyInitializer::InitFileSystem(System.String userDataPath, System.String modsCachePath)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
stloc.0
ldloc.0
isinst VRage.Utils.Keen.MyLogKeen
dup
stloc.1
brfalse.s L1
ldloc.1
ldarg.1
call static System.Text.StringBuilder VRage.Game.MyFinalBuildConstants::get_APP_VERSION_STRING()
ldarg.s 5
callvirt System.Void VRage.Utils.Keen.MyLogKeen::InitWithDateNoCheck(System.String logNameBaseName, System.Text.StringBuilder appVersionString, System.Int32 maxLogAgeInDays)
br.s L2
L1:
ldloc.0
ldarg.1
call static System.Text.StringBuilder VRage.Game.MyFinalBuildConstants::get_APP_VERSION_STRING()
ldarg.s 5
callvirt System.Void VRage.Utils.MyLog::InitWithDate(System.String logNameBaseName, System.Text.StringBuilder appVersionString, System.Int32 maxLogAgeInDays)
L2:
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
call static System.Void VRage.Utils.MyLog::set_Default(VRage.Utils.MyLog value)
call static System.Void Sandbox.MyInitializer::InitExceptionHandling()
ldarg.2
call static System.Boolean System.String::IsNullOrEmpty(System.String value)
brtrue.s L3
ldarg.2
br.s L4
L3:
ldsfld System.String VRage.FileSystem.MyFileSystem::RootPath
L4:
call static System.Boolean System.SteamHelpers::IsSteamPath(System.String path)
stloc.2
ldarg.2
ldarg.0
call static System.Boolean System.SteamHelpers::IsAppManifestPresent(System.String path, System.UInt32 appId)
stloc.3
ldloc.2
brtrue.s L5
ldloc.3
ldc.i4.0
ceq
br.s L6
L5:
ldc.i4.0
L6:
stsfld System.Boolean Sandbox.Engine.Platform.Game::IsPirated
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Is official: {0} {1}{2}{3}"
ldc.i4.4
newarr System.Object
dup
ldc.i4.0
ldc.i4.1
box System.Boolean
stelem.ref
dup
ldc.i4.1
ldsfld System.Boolean Sandbox.Engine.Utils.MyObfuscation::Enabled
brtrue.s L7
ldstr "[NO]"
br.s L8
L7:
ldstr "[O]"
L8:
stelem.ref
dup
ldc.i4.2
ldloc.2
brtrue.s L9
ldstr "[NIS]"
br.s L10
L9:
ldstr "[IS]"
L10:
stelem.ref
dup
ldc.i4.3
ldloc.3
brtrue.s L11
ldstr "[NAMP]"
br.s L12
L11:
ldstr "[AMP]"
L12:
stelem.ref
call static System.String System.String::Format(System.String format, System.Object[] args)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Branch / Sandbox: "
call static System.String Sandbox.Engine.Networking.MyGameService::get_BranchName()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Client Build Number: "
ldsflda Sandbox.Game.MyBasicGameInfo Sandbox.Game.MyPerGameSettings::BasicGameInfo
ldfld System.Nullable`1<System.Int32> Sandbox.Game.MyBasicGameInfo::ClientBuildNumber
box System.Nullable`1[System.Int32]
call static System.String System.String::Concat(System.Object arg0, System.Object arg1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Server Build Number: "
ldsflda Sandbox.Game.MyBasicGameInfo Sandbox.Game.MyPerGameSettings::BasicGameInfo
ldfld System.Nullable`1<System.Int32> Sandbox.Game.MyBasicGameInfo::ServerBuildNumber
box System.Nullable`1[System.Int32]
call static System.String System.String::Concat(System.Object arg0, System.Object arg1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.ProcessorCount: "
call static System.Int32 System.Environment::get_ProcessorCount()
box System.Int32
call static System.String System.String::Concat(System.Object arg0, System.Object arg1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.OSVersion: "
call static VRage.IVRagePlatform VRage.MyVRage::get_Platform()
callvirt abstract virtual VRage.IVRageSystem VRage.IVRagePlatform::get_System()
callvirt abstract virtual System.String VRage.IVRageSystem::GetOsName()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.CommandLine: "
call static System.String System.Environment::get_CommandLine()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.Is64BitProcess: "
call static System.Boolean System.Environment::get_Is64BitProcess()
stloc.s 5 (System.Boolean)
ldloca.s 5 (System.Boolean)
call virtual System.String System.Boolean::ToString()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.Is64BitOperatingSystem: "
call static System.Boolean System.Environment::get_Is64BitOperatingSystem()
stloc.s 5 (System.Boolean)
ldloca.s 5 (System.Boolean)
call virtual System.String System.Boolean::ToString()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.Version: "
call static System.String System.Runtime.InteropServices.RuntimeInformation::get_FrameworkDescription()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Environment.CurrentDirectory: "
call static System.String System.Environment::get_CurrentDirectory()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "CPU Info: "
call static VRage.IVRagePlatform VRage.MyVRage::get_Platform()
callvirt abstract virtual VRage.IVRageSystem VRage.IVRagePlatform::get_System()
ldloca.s 4 (System.UInt32)
ldloca.s 6 (System.UInt32)
callvirt abstract virtual System.String VRage.IVRageSystem::GetInfoCPU(System.UInt32& frequency, System.UInt32& physicalCores)
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
ldloc.s 4 (System.UInt32)
stsfld System.UInt32 Sandbox.MySandboxGame::CPUFrequency
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Default Culture: "
call static System.Globalization.CultureInfo System.Globalization.CultureInfo::get_CurrentCulture()
callvirt virtual System.String System.Globalization.CultureInfo::get_Name()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldstr "Default UI Culture: "
call static System.Globalization.CultureInfo System.Globalization.CultureInfo::get_CurrentUICulture()
callvirt virtual System.String System.Globalization.CultureInfo::get_Name()
call static System.String System.String::Concat(System.String str0, System.String str1)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
call static VRage.IVRagePlatform VRage.MyVRage::get_Platform()
callvirt abstract virtual VRage.IVRageSystem VRage.IVRagePlatform::get_System()
ldsfld VRage.Utils.MyLog Sandbox.MySandboxGame::Log
ldftn System.Void VRage.Utils.MyLog::WriteLineAndConsole(System.String msg)
newobj System.Void System.Action`1<System.String>::.ctor(System.Object object, System.IntPtr method)
callvirt abstract virtual System.Void VRage.IVRageSystem::LogRuntimeInfo(System.Action`1<System.String> log)
ldarg.1
ldstr ".cfg"
call static System.String System.String::Concat(System.String str0, System.String str1)
newobj System.Void Sandbox.Engine.Utils.MyConfig::.ctor(System.String fileName)
stsfld Sandbox.Engine.Utils.MyConfig Sandbox.MySandboxGame::Config
ldarg.s 6
brfalse.s L13
ldsfld Sandbox.Engine.Utils.MyConfig Sandbox.MySandboxGame::Config
ldarg.s 6
callvirt System.Void Sandbox.Engine.Utils.MyConfigBase::add_OnSettingChanged(System.Action value)
L13:
ldsfld Sandbox.Engine.Utils.MyConfig Sandbox.MySandboxGame::Config
callvirt System.Void Sandbox.Engine.Utils.MyConfigBase::Load()
ret
