// 5d66babb
ldarg.0
ldfld SharpDX.XAudio2.XAudio2 VRage.Audio.MyXAudio2::m_audioEngine
brtrue.s L0
ret
L0:
ldstr "default"
stloc.0
ldsfld System.Boolean VRage.Audio.MyXAudio2::DEVICE_DETAILS_SUPPORTED
brfalse.s L1
ldarg.0
ldarg.0
ldfld SharpDX.Multimedia.Speakers VRage.Audio.MyXAudio2::m_channelMask
newobj System.Void SharpDX.X3DAudio.X3DAudio::.ctor(SharpDX.Multimedia.Speakers speakers)
stfld SharpDX.X3DAudio.X3DAudio VRage.Audio.MyXAudio2::m_x3dAudio
ldarg.0
ldflda SharpDX.XAudio2.DeviceDetails VRage.Audio.MyXAudio2::m_deviceDetails
ldfld System.String SharpDX.XAudio2.DeviceDetails::DisplayName
stloc.0
br.s L2
L1:
ldarg.0
ldarg.0
ldfld SharpDX.Multimedia.Speakers VRage.Audio.MyXAudio2::m_channelMask
ldc.i4.0
newobj System.Void SharpDX.X3DAudio.X3DAudio::.ctor(SharpDX.Multimedia.Speakers speakers, SharpDX.X3DAudio.X3DAudioVersion requestVersion)
stfld SharpDX.X3DAudio.X3DAudio VRage.Audio.MyXAudio2::m_x3dAudio
L2:
ldloc.0
ldc.i4.0
callvirt System.Int32 System.String::IndexOf(System.Char value)
stloc.1
ldloc.1
ldc.i4.m1
beq.s L3
ldloc.0
ldc.i4.0
ldloc.1
callvirt System.String System.String::Substring(System.Int32 startIndex, System.Int32 length)
stloc.0
L3:
call static VRage.Utils.MyLog VRage.Utils.MyLog::get_Default()
ldstr "MyAudio.CreateX3DAudio - Device: {0} - Channels #: {1} - Sample rate: {2}"
ldloc.0
ldarg.0
ldfld System.Int32 VRage.Audio.MyXAudio2::m_inputChannelCount
box System.Int32
ldarg.0
call virtual System.Int32 VRage.Audio.MyXAudio2::get_SampleRate()
box System.Int32
call static System.String System.String::Format(System.String format, System.Object arg0, System.Object arg1, System.Object arg2)
callvirt System.Void VRage.Utils.MyLog::WriteLine(System.String msg)
ret
