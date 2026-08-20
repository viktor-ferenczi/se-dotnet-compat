using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using HarmonyLib;
using Microsoft.CodeAnalysis;
using VRage.Scripting;

namespace Shared.Patches.Scripting;

[HarmonyPatch]
[HarmonyPatchCategory("Finish")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyScriptWhitelistPatch
{
    public static MethodBase TargetMethod()
    {
        var type = typeof(MyScriptWhitelist);
        var nestedType = type.GetNestedType("Batch", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Debug.Assert(nestedType != null, "Couldn't find nested type MyScriptWhitelist.Batch");
        var method = AccessTools.Method(nestedType, "ResolveTypeSymbol");
        Debug.Assert(method != null, "Couldn't find the AllDeclaredMembers method in MyScriptWhitelist");
        return method;
    }
    
    [HarmonyPrefix]
    private static bool ResolveTypeSymbolPrefix(Type type, ref INamedTypeSymbol __result, Dictionary<string, IAssemblySymbol> ___m_assemblyMap)
    {
        if (type.IsGenericType && !type.IsGenericTypeDefinition)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();

            if (!___m_assemblyMap.TryGetValue(type.Assembly.FullName, out var assemblySymbol))
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
