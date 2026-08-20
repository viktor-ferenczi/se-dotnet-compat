using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Scripting;

namespace Shared.Patches.Scripting;

[HarmonyPatch(typeof(MyScriptWhitelist.Batch), nameof(MyScriptWhitelist.Batch.ResolveTypeSymbol))]
[HarmonyPatchCategory("Finish")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyScriptWhitelistPatch
{
    [HarmonyPrefix]
    private static bool ResolveTypeSymbolPrefix(MyScriptWhitelist.Batch __instance, Type type, ref INamedTypeSymbol __result)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (!__instance.m_assemblyMap.TryGetValue(type.Assembly.FullName, out var assemblySymbol))
            {
                throw new MyWhitelistException($"Cannot add {type.FullName} to the batch because {type.Assembly.FullName} has not been added to the compiler.");
            }
            
            var genericSymbol = assemblySymbol.GetTypeByMetadataName(genericTypeDefinition.FullName);

            var typeArguments = type.GetGenericArguments();
            var typeArgumentSymbols = new ITypeSymbol[typeArguments.Length];

            for (var i = 0; i < typeArguments.Length; i++)
            {
                typeArgumentSymbols[i] = assemblySymbol.GetTypeByMetadataName(typeArguments[i].FullName);
            }

            __result = genericSymbol.Construct(typeArgumentSymbols);
            
            return false;
        }

        return true;
    }
}
