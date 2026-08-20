using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Game.VisualScripting.ScriptBuilder;
using VRage.Utils;

namespace ClientPlugin.Patches.Scripting;

// Roslyn needs the runtime facade assemblies to resolve forwarded types in visual scripts.
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

            // RegisterAssembly only accepts loaded assemblies, so update its backing set.
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
