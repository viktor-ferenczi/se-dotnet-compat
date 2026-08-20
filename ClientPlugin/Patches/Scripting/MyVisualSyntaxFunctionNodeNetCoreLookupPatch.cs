using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HarmonyLib;
using VRage.Game;
using VRage.Game.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder.Nodes;

namespace ClientPlugin.Patches.Scripting;

// Visual scripts store mscorlib in method signatures, but .NET 10 reports
// System.Private.CoreLib. Strip the assembly details before comparing them.
[HarmonyPatch(typeof(MyVisualSyntaxFunctionNode), MethodType.Constructor,
    typeof(MyObjectBuilder_ScriptNode), typeof(Type))]
[HarmonyPatchCategory("Finish")]
static class MyVisualSyntaxFunctionNodeNetCoreLookupPatch
{
    // Matches the assembly portion of a generic argument's qualified name.
    private static readonly Regex s_asmQualifier = new Regex(
        @", [\w.]+, Version=\d+\.\d+\.\d+\.\d+, Culture=\w+, PublicKeyToken=(?:[a-fA-F0-9]+|null)",
        RegexOptions.Compiled);

    // The game registers more closed generic methods while loading a session.
    private static Dictionary<string, MethodInfo> s_normalizedIndex;
    private static int s_indexedRegistrySize = -1;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    static void RetryWithNormalizedSignature(MyVisualSyntaxFunctionNode __instance,
                                             MyObjectBuilder_ScriptNode ob)
    {
        if (__instance.m_methodInfo != null)
            return;

        var fn = ob as MyObjectBuilder_FunctionScriptNode;
        if (fn == null || string.IsNullOrEmpty(fn.Type))
            return;

        string targetSig = NormalizeAssemblyQualifiers(fn.Type);
        if (ReferenceEquals(targetSig, fn.Type) || targetSig == fn.Type)
            return;

        PrimeRegistryFor(fn.ExtOfType, fn.Type);
        PrimeRegistryFor(fn.DeclaringType, fn.Type);

        var found = LookupByNormalized(targetSig);
        if (found == null)
            found = LookupByNormalizedPrefix(targetSig);
        if (found == null)
            return;

        Unsafe.AsRef(in __instance.m_methodInfo) = found;

        // The constructor normally calls InitUsing after a successful lookup.
        try { __instance.InitUsing(); }
        catch { }
    }

    // Looking up the declaring type makes the game register closed generic methods.
    private static void PrimeRegistryFor(string typeName, string sigToProbe)
    {
        if (string.IsNullOrEmpty(typeName))
            return;
        try
        {
            var t = MyVisualScriptingProxy.GetType(typeName);
            if (t != null)
                MyVisualScriptingProxy.GetMethod(t, sigToProbe);
        }
        catch
        {
        }
    }

    private static MethodInfo LookupByNormalized(string targetSig)
    {
        var byName = MyVisualScriptingProxy.GetMethods();
        var whitelisted = MyVisualScriptingProxy.GetWhitelistedMethods(null) as ICollection<MethodInfo>
            ?? new List<MethodInfo>(MyVisualScriptingProxy.GetWhitelistedMethods(null));

        int total = byName.Count + whitelisted.Count;
        if (s_normalizedIndex == null || s_indexedRegistrySize != total)
        {
            var idx = new Dictionary<string, MethodInfo>(total);
            foreach (var m in byName)
                IndexMethod(idx, m);
            foreach (var m in whitelisted)
                IndexMethod(idx, m);
            s_normalizedIndex = idx;
            s_indexedRegistrySize = total;
        }

        s_normalizedIndex.TryGetValue(targetSig, out var found);
        return found;
    }

    // The stock code also accepts a saved signature missing new optional parameters.
    private static MethodInfo LookupByNormalizedPrefix(string targetSig)
    {
        int closeIdx = targetSig.LastIndexOf(')');
        if (closeIdx < 0)
            return null;
        string prefix = targetSig.Substring(0, closeIdx);

        var byName = MyVisualScriptingProxy.GetMethods();
        foreach (var m in byName)
        {
            var hit = TryPrefixMatch(m, prefix);
            if (hit != null) return hit;
        }
        var whitelisted = MyVisualScriptingProxy.GetWhitelistedMethods(null);
        if (whitelisted != null)
        {
            foreach (var m in whitelisted)
            {
                var hit = TryPrefixMatch(m, prefix);
                if (hit != null) return hit;
            }
        }
        return null;
    }

    private static MethodInfo TryPrefixMatch(MethodInfo m, string normalizedPrefix)
    {
        try
        {
            string sig = NormalizeAssemblyQualifiers(m.Signature());
            if (string.IsNullOrEmpty(sig) || !sig.StartsWith(normalizedPrefix))
                return null;
            if (sig.Length == normalizedPrefix.Length)
                return m;
            char next = sig[normalizedPrefix.Length];
            return (next == ',' || next == ')') ? m : null;
        }
        catch
        {
            return null;
        }
    }

    private static void IndexMethod(Dictionary<string, MethodInfo> idx, MethodInfo m)
    {
        try
        {
            var key = NormalizeAssemblyQualifiers(m.Signature());
            // Closed generic methods are registered later and should win collisions.
            if (!string.IsNullOrEmpty(key))
                idx[key] = m;
        }
        catch
        {
        }
    }

    public static string NormalizeAssemblyQualifiers(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s_asmQualifier.Replace(s, "");
    }
}
