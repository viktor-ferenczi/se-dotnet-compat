// 3bbf9165
ldarg.0
ldc.i4.3
newobj System.Void SharpDX.XAudio2.XAudio2::.ctor(SharpDX.XAudio2.XAudio2Version requestedVersion)
stfld SharpDX.XAudio2.XAudio2 VRage.Platform.Windows.Audio.MyPlatformAudio::m_audioEngine
ldarg.0
ldfld SharpDX.XAudio2.XAudio2 VRage.Platform.Windows.Audio.MyPlatformAudio::m_audioEngine
ret
