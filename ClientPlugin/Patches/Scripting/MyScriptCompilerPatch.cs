using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Scripting;

namespace ClientPlugin.Patches.Scripting;

[HarmonyPatch(typeof(MyScriptCompiler))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
// ReSharper disable once UnusedType.Global
public static class MyScriptCompilerPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyScriptCompiler.AddReferencedAssemblies))]
    // ReSharper disable once UnusedMember.Global
    public static bool AddReferencedAssembliesPrefix(
        string[] assemblyLocations,
        HashSet<string> ___m_assemblyLocations,
        List<MetadataReference> ___m_metadataReferences)
    {
        // Replacement code to skip empty strings
        foreach (var assemblyLocation in assemblyLocations)
        {
            if (string.IsNullOrEmpty(assemblyLocation))
            {
                Console.WriteLine($"{Plugin.Name} [WARNING] AddReferencedAssembliesPrefix: Empty assembly location {assemblyLocation}");
#if DEBUG
                Debugger.Break();
#endif
                continue;
            }

            if (___m_assemblyLocations.Add(assemblyLocation))
                ___m_metadataReferences.Add(MetadataReference.CreateFromFile(assemblyLocation));
        }

        // Skip the original implementation
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyScriptCompiler.AddImplicitInGameNamespacesFromTypes))]
    // ReSharper disable once UnusedMember.Global
    public static bool AddImplicitInGameNamespacesFromTypesPrefix(Type[] types, HashSet<string> ___m_implicitScriptNamespaces)
    {
        // Replacement code to skip empty namespaces (as it turns out there are none, but just in case leaving it here)
        foreach (var type in types)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(types));

            if (string.IsNullOrEmpty(type.Namespace))
            {
                Console.WriteLine($"{Plugin.Name} [WARNING] AddImplicitInGameNamespacesFromTypesPrefix: Empty namespace name {type.Namespace}");
#if DEBUG
                Debugger.Break();
#endif
                continue;
            }

            ___m_implicitScriptNamespaces.Add(type.Namespace);
        }

        // Skip the original implementation
        return false;
    }
}