# Harmony Patch Skeletons

This directory contains Harmony patch skeletons generated from `ChangesForPulsar.patch`. Each patch class corresponds to changes needed in the Space Engineers game code to support .NET 8.

## Conditional Compilation Symbols

Each category of patches is controlled by a `#define` symbol. To enable a category, define the corresponding symbol in your project or code:

- `DISABLE_ANALYTICS` - Disables analytics and telemetry tracking
- `DISABLE_CRASH_REPORTING` - Disables crash reporting and error analytics
- `NULLABILITY_FIXES` - Adds null safety checks to prevent crashes
- `SYSTEM_FIXES` - Fixes system/platform detection issues (WMI, CPU info)
- `SHARPDX_FIXES` - Updates SharpDX audio API version references
- `SIXLABORS_FIXES` - Updates SixLabors.ImageSharp API usage
- `PROTOBUF_FIXES` - Fixes ProtoBuf serialization API changes
- `SCRIPTING_FIXES` - Fixes mod compilation and IL checking
- `XML_FIXES` - Fixes XML serialization namespace handling
- `MISC_FIXES` - Miscellaneous fixes (type table, grid shape, etc.)

Enable them all by adding:

`DISABLE_ANALYTICS;DISABLE_CRASH_REPORTING;NULLABILITY_FIXES;SYSTEM_FIXES;SHARPDX_FIXES;SIXLABORS_FIXES;PROTOBUF_FIXES;SCRIPTING_FIXES;XML_FIXES;MISC_FIXES`

## Patch Organization

### Analytics (1 file)
**Symbol:** `DISABLE_ANALYTICS`

- `MySpaceAnalyticsPatch.cs` - Disables analytics session startup

### Crash Reporting (3 files)
**Symbol:** `DISABLE_CRASH_REPORTING`

- `MyCrashReportingPatch.cs` - Disables crash analytics reporting
- `MyInitializerPatch.cs` - Removes error reporter initialization (TODO: transpiler)
- `MySandboxGamePatch.cs` - Removes try-catch around mod API init (TODO: transpiler)

### Null Safety (4 files)
**Symbol:** `NULLABILITY_FIXES`

- `MyCharacterPatch.cs` - Null check for `MyCubeBuilder.Static`
- `MyGridClipboardPatch.cs` - Null check for `MyClipboardComponent.Static` (TODO: transpiler)
- `MyPropertySyncStateGroupPatch.cs` - Null check for `MyMultiplayer.Static` (TODO: transpiler)
- `MyHeightMapLoadingSystemPatch.cs` - Null check for heightmap collections

### System (2 files)
**Symbol:** `SYSTEM_FIXES`

- `MyWindowsSystemPatch.cs` - Fixes WMI/platform detection issues

### Audio (2 files)
**Symbol:** `SHARPDX_FIXES`

- `MyXAudio2Patch.cs` - Changes `X3DAudioVersion.Version29` to `Default` (TODO: transpiler)
- `MyPlatformAudioPatch.cs` - Changes `XAudio2Version.Version29` to `Default` (TODO: transpiler)

### Image Processing (3 files)
**Symbol:** `SIXLABORS_FIXES` and `MISC_FIXES`

- `MyImagePatch.cs` - Updates ImageSharp API: `Gray8`→`L8`, `Gray16`→`L16`, metadata access (TODO: transpiler)
- `MyTextureDataPatch.cs` - Updates ImageSharp pixel format names (TODO: transpiler)
- `MyFileTextureImageCachePatch.cs` - Handles .zip to .dds conversion

### ProtoBuf (2 files)
**Symbol:** `PROTOBUF_FIXES`

- `DynamicTypeModelPatch.cs` - Removes `setDefault` parameter (TODO: transpiler)
- `MyObjectBuilderSerializerKeenPatch.cs` - Disables cloning that crashes (TODO: transpiler)

### Scripting (3 files)
**Symbol:** `SCRIPTING_FIXES`

- `MySpaceGameDefaultIlCheckerPatch.cs` - Removes `op_Inequality` method (TODO: transpiler)
- `MySpaceGameDefaultIlCompilerPatch.cs` - Updates assembly loading (TODO: transpiler)
- `PerfCountingRewriterPatch.cs` - Disables performance counting (fully implemented)

### Serialization (2 files)
**Symbol:** `XML_FIXES`

- `CustomRootWriterPatch.cs` - Fixes `xsi:type` attribute writing (TODO: transpiler)
- `MyAbstractXmlSerializerPatch.cs` - Fixes `xsi:type` attribute reading (TODO: transpiler)

### Miscellaneous (5 files)
**Symbol:** `MISC_FIXES`

- `MyGuiScreenMainMenuBasePatch.cs` - Adds framework version to build info (TODO: transpiler)
- `MyGridShapePatch.cs` - Fixes stackalloc span usage (TODO: transpiler)
- `MyEOSLobbyListPatch.cs` - Disables broken lobby code (TODO: transpiler)
- `MyTypeTablePatch.cs` - Adds delegate compatibility + validation (TODO: transpiler)
- `MyAfterMathPatch.cs` - Fixes SkipInit pointer usage (TODO: transpiler)

## Implementation Status

### Fully Implemented (5 patches)
- `MySpaceAnalyticsPatch` - Simple prefix returning false
- `MyCrashReportingPatch` - Prefix patches with default return values
- `MyWindowsSystemPatch` - Replacement methods
- `MyHeightMapLoadingSystemPatch` - Simple null check prefix
- `PerfCountingRewriterPatch` - Returns unmodified syntax tree

### Partially Implemented (1 patch)
- `MyFileTextureImageCachePatch` - Prefix modifying filepath parameter

### TODO: Transpiler Required (21 patches)
The majority of patches require transpiler implementation because they need to modify IL code within methods. These are marked with `// TODO: Implement transpiler` comments.

## Usage

1. Define the symbols you want to enable in your project
2. Implement transpiler patches where marked with TODO
3. The Harmony patching is automatically applied via `[HarmonyPatch]` attributes
4. Plugin.cs already contains `harmony.PatchAll(Assembly.GetExecutingAssembly())`

## Notes

- All patches are designed to be non-invasive when their symbols are not defined
- Transpiler patches will need access to the original game IL code
- Use `TranspilerHelpers` class for logging IL code during development
- Some patches may need the publicizer enabled to access internal/private members
