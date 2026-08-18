# ServerPlugin — Space Engineers Dedicated Server .NET 10 Compatibility

A Harmony + preloader plugin that ports the .NET 10 compatibility fixes from `ClientPlugin` to the
Space Engineers **Dedicated Server**. It applies the same set of IL prepatches and Harmony patches
the client plugin uses, minus the UI/audio/render-only ones that aren't relevant on a headless server.

## Build

Requirements:

- .NET 10 SDK
- A locally installed Space Engineers Dedicated Server
- The path in `Directory.Build.props` (`$(DS64)`) pointing to the server's `DedicatedServer64` folder

Build via the solution:

```
dotnet build DotNetCompat.sln -c Debug
```

It is intended to be loaded by Magnetar, a Pulsar-equivalent loader for the Dedicated Server.

## Project layout

```
Shared/                         Common patches, Roslyn rewriters, and IL tools
ClientPlugin/                   Pulsar entry point and client-only patches
ServerPlugin/                   Magnetar entry point and server-only patches
├── ServerPlugin.cs             IPlugin entry point
├── Preloader.cs                Cecil and early Harmony entry point
├── Patches/                    Dedicated-server-only patches
└── ServerPlugin.csproj         Server references and Shared source import
```

## How it works

The plugin uses the same two-phase preloader pattern as the client plugin:

1. **Preloader phase (Cecil prepatches)**, before any game assembly is loaded.
   `Preloader.Patch` rewrites IL inside specific game DLLs declared in `TargetDLLs`. Each target method
   has its IL hash verified against a known value before patching, so the build refuses to apply if
   the game has changed in an unexpected way.

   Server prepatches:
   - `Sandbox.Game`: `MyHeightMapLoadingSystem` (`62847c5b`)
   - `VRage`: `CustomRootWriter.Init` (`c8bac690`), `MyAbstractXmlSerializer.GetTypeAttribute` (`320acfb0`)
   - `SixLabors.ImageSharp`: `PngDecoderCore.DecodePixelData` (`8e787d98`) and `DecodeInterlacedPixelData`
     (partial-read fix; the interlaced variant has no hash check)
   - `VRage.Dedicated`: structural removal of the `WindowsService` host in `DedicatedServer.Run` and
     `Configurator.SelectInstanceForm` (no hash — patched by IL shape, not a hashed transpile)
   - `SpaceEngineersDedicated` (`.exe`): removal of the WinForms/Drawing configurator setup block from
     `MyProgram.Main`, so the entry point no longer references `System.Windows.Forms` / `System.Drawing`
     and the DS can JIT `Main` on a plain net10.0 host (no hash — patched by IL shape, anchored on the
     `get_SpaceEngineersDSLogo` … `ConfigForm.OnReset` member references)

2. **Harmony phase**, after the game has loaded.
   `Preloader.Finish` calls `Harmony.PatchCategory("Finish")`, and `ServerPlugin.Init` calls
   `Harmony.PatchCategory("Init")`. Patches are split between the two categories based on whether they
   need to apply early (before the game does its own initialization) or late.

## Patches dropped vs. ClientPlugin

These client patches were intentionally **not** ported — they target subsystems that don't run on a
dedicated server (or aren't needed yet):

- `Patches/Audio/MyPlatformAudioPatch`, `Patches/Audio/MyXAudio2Patch` — no audio playback on the server
- `Patches/ImageProcessing/MyFileTextureImageCachePatch` — GPU texture cache (the shared `DecodePixelDataPrepatch`
  PNG fix in that folder **is** ported, since the server still decodes heightmaps)
- `Patches/Miscellaneous/MyGuiScreenMainMenuBasePatch` — main menu GUI
- `Patches/NullSafety/MyCharacterPatch` — client-side character builder
- `Patches/NullSafety/MyGridClipboardPatch` — clipboard
- `Patches/NullSafety/MyRenderContextStatisticsPatch` — render statistics
- `Patches/Scripting/MyDependencyCollectorTpaPatch`, `Patches/Scripting/MyVisualSyntaxFunctionNodeNetCoreLookupPatch`
  — visual-scripting (Frostbite) compile fixes, not yet ported
- `MySandboxGamePatch.OnDotNetHotfixPopupClosed` — modal GUI prompt

Server-only additions with no ClientPlugin equivalent:

- `Patches/CrashReporting/MyWindowsWindowsPatch` — redirects `MyWindowsWindows.MessageBox` to stderr
- `Patches/Windows/WindowsServicePrepatch` — strips the Windows Service host out of `VRage.Dedicated`
- `Patches/Windows/MyProgramPrepatch` — strips the WinForms/Drawing configurator block out of the DS
  entry point `MyProgram.Main` (removes the `System.Windows.Forms` / `System.Drawing` dependency)
- `Patches/Networking/CrossPlatformEosPatch` — hosts crossplay worlds over EOS (see below)

## Crossplay (EOS) hosting

A crossplay dedicated server has to advertise itself on EOS so that EOS/console
players (and, via the ClientPlugin EOS-connect fix, Steam players too) can find
and join it. Stock SE only initializes EOS networking when the config
`NetworkType` is `eos` (or `-eos` is passed); the `CrossPlatform` flag alone only
marks the world's content as console-compatible and leaves the transport on
Steam, so a `CrossPlatform` world hosted with the default `NetworkType=steam`
registers on Steam only and is invisible to crossplay clients.

Two server patches make crossplay hosting work on the .NET 10 server:

- `Patches/Networking/CrossPlatformEosPatch` — postfix on `DedicatedServer.InitConsoleCompatibility`
  (which runs right after `ConfigDedicated.Load()` and right before `InitializeServices`). When the
  world has `CrossPlatform=true` it sets `NetworkType=eos`, so `MyProgram.IsEOS()` returns true and the
  DS brings up `MyEOSService` / `MyEOSGameServer` and creates the public advertised EOS lobby that
  crossplay clients discover. Hosting with an explicit `NetworkType=eos` keeps working unchanged.
- `Shared/Patches/Serialization/MyInventoryHelperPatch` — shared with ClientPlugin. In EOS mode the
  DS uses `MyMockingInventory`, which calls `MyInventoryHelper.GetItemsCheckData` /
  `GetItemCheckData` / `CheckItemData` during the EOS join handshake; those use `BinaryFormatter`,
  which throws on .NET 10. The patch replaces them with manual NRBF write / `NrbfDecoder` read so
  EOS clients can join.

The EOS SDK native library (`EOSSDK-Shipping.dll` → `libEOSSDK-Linux-Shipping.so`) is resolved by
Magnetar's `NativeLibraryPreloader`, so no native-lib handling is needed here.

`Preloader.TargetDLLs` covers the game assemblies prepatched on the server: `HavokWrapper,
Sandbox.Common, Sandbox.Game, Sandbox.Graphics, SpaceEngineers.Game, VRage, VRage.Game, VRage.Library,
VRage.Math, VRage.Network, VRage.Platform.Windows, VRage.Render11, VRage.Scripting`, plus the
server-specific `VRage.Dedicated` and `SpaceEngineersDedicated.exe`, and the `SixLabors.ImageSharp`
dependency.

## Project configuration

Pulled in via `ServerPlugin.csproj`:

- `TargetFramework`: `net10.0` (no `-windows` / WinForms dependency)
- `AssemblyName`: `DotNetCompat` (same as client — they are independent assemblies in different folders)
- `RootNamespace`: `ServerPlugin`
- `LangVersion`: 13
- `DefineConstants`: includes `DEDICATED` so shared code can retain server-specific patch timing and public type names
- `EnableUnsafeBinaryFormatterSerialization=true` — required for `List<MyGameInventoryItem>`
  serialization in `VRage.GameServices.MyInventoryHelper` (deserialization is patched to use
  `NrbfDecoder`).

NuGet packages:

- `Lib.Harmony` 2.4.2
- `Krafs.Publicizer` 2.3.0
- `Mono.Cecil` 0.11.6
- `System.Formats.Nrbf` 10.0.1
- `System.Management` 4.5.0 — referenced by `VRage.Platform.Windows`; not in the plain net10.0 framework
- `System.Drawing.Common` 6.0.0 — game's net48 code references `System.Drawing` (GAC), type-forwarded
  to `System.Drawing.Common` on .NET; not in the plain net10.0 framework
- `System.Diagnostics.PerformanceCounter` 9.0.0 — referenced by `VRage.Platform.Windows`
  `MyWindowsSystem.Init`; not in the plain net10.0 framework

The last three support the game's .NET Framework references on a plain net10.0 host. Magnetar stages
them from `DotNetCompatServer.xml`, and `Preloader` resolves them by name when requested.

Publicized assemblies: `Sandbox.Game, Sandbox.Graphics, Sandbox.ObjectBuilders, SpaceEngineers,
SpaceEngineers.Game, VRage, VRage.Audio, VRage.Dedicated, VRage.EOS, VRage.Network,
VRage.Platform.Windows, VRage.Render11, VRage.Scripting`.

A `DoNotPublicize` list excludes a handful of GUI events whose private accessors clash with the
publicizer's rewriting (carried over from the client config — harmless on the server).

## Notes

A Pulsar-equivalent plugin loader for the Dedicated Server (Magnetar) is required for post-build deployment and runtime loading.
