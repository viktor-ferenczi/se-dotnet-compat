Make sure that all assemblies which are used in the patches are enabled for the Krafs
publicizer by listing them in `GameAssembliesToPublicize.cs`. This applies to any class
patched or used in the patches, including any data types or enums used by the patches.

For each class, struct or enum, please do the following:
1. Search for its name in the decompiled Space Engineers code. You have the tool for it.
2. Once you found its declaration, look for the namespace it is defined in.
3. Identity the game assembly which blongs to it.
4. Add that assembly to the publicized assemblies list if it is not already there.
5. Make sure the in each patch using that symbol it is imported (`using`) at the top in the patch file.

The full list of game assemblies (DLLs):

Here are the DLL (and one EXE) file names extracted:
* `Sandbox.Common.dll`
* `Sandbox.Game.dll`
* `Sandbox.Game.XmlSerializers.dll`
* `Sandbox.Graphics.dll`
* `Sandbox.RenderDirect.dll`
* `SpaceEngineers.exe`
* `SpaceEngineers.Game.dll`
* `SpaceEngineers.ObjectBuilders.dll`
* `SpaceEngineers.ObjectBuilders.XmlSerializers.dll`
* `VRage.Ansel.dll`
* `VRage.Audio.dll`
* `VRage.dll`
* `VRage.EOS.dll`
* `VRage.EOS.XmlSerializers.dll`
* `VRage.Game.dll`
* `VRage.Game.XmlSerializers.dll`
* `VRage.Input.dll`
* `VRage.Library.dll`
* `VRage.Math.dll`
* `VRage.Math.XmlSerializers.dll`
* `VRage.Mod.Io.dll`
* `VRage.NativeAftermath.dll`
* `VRage.NativeWrapper.dll`
* `VRage.Network.dll`
* `VRage.Platform.Windows.dll`
* `VRage.Render.dll`
* `VRage.Render11.dll`
* `VRage.Scripting.dll`
* `VRage.Steam.dll`
* `VRage.UserInterface.dll`
* `VRage.XmlSerializers.dll`
