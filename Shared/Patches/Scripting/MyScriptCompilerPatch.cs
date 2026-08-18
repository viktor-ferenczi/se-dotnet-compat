using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Scripting;

namespace Shared.Patches.Scripting;

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyScriptCompiler))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyScriptCompilerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyScriptCompiler.AddReferencedAssemblies))]
    public static bool AddReferencedAssembliesPrefix(
        string[] assemblyLocations,
        HashSet<string> ___m_assemblyLocations,
        List<MetadataReference> ___m_metadataReferences)
    {
        foreach (var assemblyLocation in assemblyLocations)
        {
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                Console.WriteLine($"DotNetCompat [WARNING] AddReferencedAssembliesPrefix: Empty assembly location {assemblyLocation}");
#if DEBUG
                Debugger.Break();
#endif
                continue;
            }

            if (___m_assemblyLocations.Add(assemblyLocation))
                ___m_metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocation));
        }

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyScriptCompiler.AddImplicitInGameNamespacesFromTypes))]
    public static bool AddImplicitInGameNamespacesFromTypesPrefix(Type[] types, HashSet<string> ___m_implicitScriptNamespaces)
    {
        foreach (var type in types)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(types));

            if (string.IsNullOrEmpty(type.Namespace))
            {
                Console.WriteLine($"DotNetCompat [WARNING] AddImplicitInGameNamespacesFromTypesPrefix: Empty namespace name {type.Namespace}");
#if DEBUG
                Debugger.Break();
#endif
                continue;
            }

            ___m_implicitScriptNamespaces.Add(type.Namespace);
        }

        return false;
    }
}
