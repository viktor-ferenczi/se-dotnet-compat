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

The post-build step (`Deploy.bat`) copies `DotNetCompat.dll` into `%AppData%\Magnetar\Interim\Local`.

It is intended to be loaded by Magnetar, a Pulsar-equivalent loader for the Dedicated Server.

## Project layout

```
ServerPlugin/
├── ServerPlugin.cs                    IPlugin entry point (Harmony PatchCategory "Init")
├── Preloader.cs                       Preloader entry: TargetDLLs + Patch (Cecil) / Finish (Harmony "Finish")
├── App.config                         Binding redirects (copied verbatim from ClientPlugin)
├── Deploy.bat                         Post-build deploy to %AppData%\Magnetar\Interim\Local
├── ServerPlugin.csproj                net10.0; DS64 references; publicizer/Harmony/Cecil
├── Tools/
│   ├── GameAssembliesToPublicize.cs   IgnoresAccessChecksTo attributes for SE assemblies
│   ├── IgnoresAccessChecksToAttribute.cs
│   ├── Hashing.cs                     IL hash helper for VerifyCodeHash
│   ├── PreloaderHelpers.cs            Cecil IL record/verify helpers
│   └── TranspilerHelpers.cs           Harmony IL record helpers
├── Patches/
│   ├── Analytics/
│   │   └── MySpaceAnalyticsPatch.cs           Disables StartSession
│   ├── CrashReporting/
│   │   ├── MyCrashReportingPatch.cs           Disables PrepareCrashAnalyticsReporting / ExtractCrashAnalyticsReport
│   │   ├── MyInitializerPatch.cs              Transpiler for InitExceptionHandling (hash 0561eef4)
│   │   ├── MySandboxGamePatch.cs              InitModAPI replacement (GUI hotfix popup dropped)
│   │   └── MyWindowsWindowsPatch.cs           Redirects MessageBox dialogs to stderr (headless server)
│   ├── ImageProcessing/
│   │   └── DecodePixelDataPrepatch.cs         Cecil prepatch on SixLabors.ImageSharp PngDecoderCore (hash 8e787d98)
│   ├── Miscellaneous/
│   │   ├── MyGridShapePatch.cs                AddShapesFromCollector replacement (avoids stackalloc crash)
│   │   └── MyTypeTablePatch.cs                IsSerializableClass / Serialize replacements (network replication compat)
│   ├── NullSafety/
│   │   ├── MyAnalyticsBasePatch.cs            Disables ReportEvent
│   │   ├── MyCharacterDiscoveryComponentPatch.cs   Skips OnFactionDiscovered on null player
│   │   ├── MyHeightMapLoadingSystemPrepatch.cs     Cecil prepatch on Sandbox.Game (hash 62847c5b)
│   │   └── MyPropertySyncStateGroupPatch.cs        Constructor null check for MyMultiplayer.Static
│   ├── Scripting/
│   │   ├── MyScriptCompilerPatch.cs           AddReferencedAssemblies / AddImplicitInGameNamespacesFromTypes
│   │   ├── MyScriptWhitelistPatch.cs          ResolveTypeSymbol fix for generic types
│   │   ├── MySpaceGameDefaultIlCheckerPatch.cs    AllDeclaredMembers / AllowDefaultNamespaces replacements
│   │   ├── MySpaceGameDefaultIlCompilerPatch.cs   InitIlCompiler replacement
│   │   └── PerfCountingRewriterPatch.cs       Disables PerfCountingRewriter.Rewrite
│   ├── Serialization/
│   │   ├── StreamReadPatch.cs                 Stream.Read retry transpiler (multiple targets)
│   │   └── XmlSerializationPrepatch.cs        Cecil prepatch for VRage XML helpers (hashes c8bac690 / 320acfb0)
│   └── Windows/
│       ├── MyFileProviderAggregatorPatch.cs   Sorts GetFiles result (Net48 implicit ordering)
│       ├── MyProgramPrepatch.cs               Cecil prepatch stripping the WinForms/Drawing configurator block from SpaceEngineersDedicated.MyProgram.Main
│       ├── MyWindowsSystemPatch.cs            GetOsName / LogEnvironmentInformation / GetInfoCPU patches
│       └── WindowsServicePrepatch.cs          Cecil prepatch stripping Windows Service code from VRage.Dedicated
└── Rewriter/
    ├── CompilerHook.cs                        CSharpCompilation hook for extension method conflicts
    ├── ConflictingExtensionCollector.cs       CSharpSyntaxWalker
    └── ConflictingExtensionRewriter.cs        CSharpSyntaxRewriter
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
- `Patches/ImageProcessing/MyFileTextureImageCachePatch` — GPU texture cache (the `DecodePixelDataPrepatch`
  PNG fix in that folder **is** ported, since the server still decodes heightmaps)
- `Patches/Miscellaneous/MyGuiScreenMainMenuBasePatch` — main menu GUI
- `Patches/NullSafety/MyCharacterPatch` — client-side character builder
- `Patches/NullSafety/MyGridClipboardPatch` — clipboard
- `Patches/NullSafety/MyRenderContextStatisticsPatch` — render statistics
- `Patches/Scripting/MyDependencyCollectorTpaPatch`, `Patches/Scripting/MyVisualSyntaxFunctionNodeNetCoreLookupPatch`
  — visual-scripting (Frostbite) compile fixes, not yet ported
- `Patches/Serialization/MyInventoryHelperPatch` — Steam inventory (client only)
- `MySandboxGamePatch.OnDotNetHotfixPopupClosed` — modal GUI prompt

Server-only additions with no ClientPlugin equivalent:

- `Patches/CrashReporting/MyWindowsWindowsPatch` — redirects `MyWindowsWindows.MessageBox` to stderr
- `Patches/Windows/WindowsServicePrepatch` — strips the Windows Service host out of `VRage.Dedicated`
- `Patches/Windows/MyProgramPrepatch` — strips the WinForms/Drawing configurator block out of the DS
  entry point `MyProgram.Main` (removes the `System.Windows.Forms` / `System.Drawing` dependency)

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
- `DefineConstants`: `DEBUG;TRACE;DEV_BUILD` (Debug) / `TRACE;DEV_BUILD` (Release)
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

The last three are not real compile-time dependencies of the plugin — they are shipped so the **game's**
net48 code can resolve them at runtime on a plain net10.0 host. They are copied into `Bin` by
`CopyBinDependencies` and explicitly loaded by `Preloader.Finish` (the same DLLs are declared in
`DotNetCompatServer.xml`'s `<NuGetReferences>` for the source-built GitHub plugin path).

Publicized assemblies: `Sandbox.Game, Sandbox.Graphics, Sandbox.ObjectBuilders, SpaceEngineers,
SpaceEngineers.Game, VRage, VRage.Audio, VRage.Dedicated, VRage.EOS, VRage.Network,
VRage.Platform.Windows, VRage.Render11, VRage.Scripting`.

A `DoNotPublicize` list excludes a handful of GUI events whose private accessors clash with the
publicizer's rewriting (carried over from the client config — harmless on the server).

Build events:

- **Pre-build**: `verify_props.bat` checks that `$(DS64)` resolves to a real folder.
- **`CopyBinDependencies` (after Build, before PostBuildEvent)**: copies `System.Management.dll`,
  `System.Drawing.Common.dll` and `System.Diagnostics.PerformanceCounter.dll` into `$(OutputPath)Bin`
  so they can be shipped with the plugin; `Preloader.Finish` loads them from `Bin` at runtime. Runs
  before the post-build deploy so `Bin` is populated when `Deploy.bat` copies it.
- **Post-build**: `Deploy.bat` copies `DotNetCompat.dll` and the `Bin` dependencies to
  `%AppData%\Magnetar\Interim\Local`.

## Notes / open items

- A Pulsar-equivalent plugin loader for the Dedicated Server (Magnetar) is required for the post-build
  deploy to succeed and for the plugin to be loaded at runtime.
- Code is currently duplicated between `ClientPlugin` and `ServerPlugin`. Extracting a shared project
  is intentionally deferred until both plugins are stable.
