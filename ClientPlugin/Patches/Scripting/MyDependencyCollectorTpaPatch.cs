using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Game.VisualScripting.ScriptBuilder;
using VRage.Utils;

namespace ClientPlugin.Patches.Scripting;

// Fixes the silent Roslyn-emit failure that prevents the Frostbite (and any
// other) visual-scripting assembly from compiling on .NET 10 Linux.
//
// Root cause (confirmed by MyVSCompilerEmitDiagnosticPatch in this folder,
// run 23:19 on 2026-05-01):
//
//   Emit FAILED diagnostics: 5408 error(s)
//   CS0012: The type 'ValueType' is defined in an assembly that is not
//     referenced. You must add a reference to assembly 'System.Runtime,
//     Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'.
//
// MyDependencyCollector's parameterless ctor (Decompiled/VRage.Scripting/.../
// MyDependencyCollector.cs:23-35) only adds two metadata references:
//   - netstandard.dll
//   - typeof(object).Assembly.Location  → System.Private.CoreLib.dll
// then later CollectReferences(GameAssembly) adds whatever Sandbox.Game's
// AssemblyName[] GetReferencedAssemblies() lists. On .NET Framework that was
// enough because mscorlib carried every fundamental type. On .NET 10, public
// "primitive" types like System.ValueType are split across forwarding
// assemblies (System.Runtime, System.Collections, System.Linq, …) that
// type-forward to System.Private.CoreLib. Roslyn requires those forwarders
// be present as MetadataReferences even if the actual implementation lives
// in System.Private.CoreLib — the type-resolver walks the forwarders by
// AssemblyName.
//
// Sandbox.Game does not directly list System.Runtime, so the Roslyn compile
// of the .vs-generated .cs files cannot resolve `ValueType` (the base of
// every struct, used pervasively in the generated code), and Emit() returns
// false silently. m_assembly stays null. GetLevelScriptInstances returns
// nothing. Mission01_MS never instantiates. No campaign notifications fire.
//
// Fix: postfix MyDependencyCollector's parameterless ctor with the runtime's
// Trusted Platform Assemblies (TPA) list. AppContext.GetData(
// "TRUSTED_PLATFORM_ASSEMBLIES") returns the full path-separated list of
// every framework assembly the .NET host knows about (System.Runtime,
// System.Collections, System.Linq, …). Adding each as a MetadataReference
// makes Roslyn's symbol resolution behave the same way as the desktop
// .NET-Framework world the original code targeted.
//
// Stable across .NET version bumps: the TPA list is populated by the
// runtime itself, so when the user upgrades .NET (10 → 11 → …) the fix
// keeps working without a path edit. Only relies on `AppContext.GetData`
// which has been part of .NET Standard since 2015.
[HarmonyPatch(typeof(MyDependencyCollector), MethodType.Constructor, new Type[] { })]
[HarmonyPatchCategory("Finish")]
static class MyDependencyCollectorTpaPatch
{
    static void Postfix(MyDependencyCollector __instance)
    {
        try
        {
            var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(tpa))
            {
                MyLog.Default.Log(MyLogSeverity.Warning,
                    "[LinuxCompat VS-DEPS] TRUSTED_PLATFORM_ASSEMBLIES empty; visual-script compile may fail.");
                return;
            }

            // Use RegisterAssembly is impractical (it wants Assembly objects);
            // reach the private m_references HashSet<MetadataReference> directly
            // and add MetadataReference.CreateFromFile entries.
            var refsField = AccessTools.Field(typeof(MyDependencyCollector), "m_references");
            var refs = refsField?.GetValue(__instance) as HashSet<MetadataReference>;
            if (refs == null)
            {
                MyLog.Default.Log(MyLogSeverity.Warning,
                    "[LinuxCompat VS-DEPS] m_references field missing/wrong type; cannot patch references.");
                return;
            }

            int added = 0;
            int skipped = 0;
            foreach (string path in tpa.Split(Path.PathSeparator))
            {
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    refs.Add(MetadataReference.CreateFromFile(path));
                    added++;
                }
                catch
                {
                    // Skip unreadable / non-managed entries silently.
                    skipped++;
                }
            }
            MyLog.Default.Log(MyLogSeverity.Info,
                "[LinuxCompat VS-DEPS] Added {0} TPA reference(s) to MyDependencyCollector ({1} skipped).",
                added, skipped);
        }
        catch (Exception ex)
        {
            MyLog.Default.Log(MyLogSeverity.Warning,
                "[LinuxCompat VS-DEPS] TPA-extension postfix threw: {0}", ex.GetType().Name + ": " + ex.Message);
        }
    }
}
