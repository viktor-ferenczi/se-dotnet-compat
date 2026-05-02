using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using VRage.Game;
using VRage.Game.VisualScripting;
using VRage.Game.VisualScripting.ScriptBuilder.Nodes;

namespace ClientPlugin.Patches.Scripting;

// Fixes the Frostbite-scenario "Script: ... failed to build" failures
// (ArgumentNullException at MyVisualScriptingProxy.IsSequenceDependent).
//
// Root cause (from the diagnostic patch in this folder, run 22:16 on
// 2026-05-01):
//
//   .vs files store assembly-qualified type names with the legacy mscorlib
//   token, e.g.
//     System.Collections.Generic.List`1[[System.String, mscorlib,
//       Version=4.0.0.0, Culture=neutral,
//       PublicKeyToken=b77a5c561934e089]].Contains(String item)
//
//   On .NET 10 the actual `string` type lives in System.Private.CoreLib, so
//   any MethodInfo.Signature() the proxy builds at runtime carries the
//   System.Private.CoreLib qualifier (Version=10.0.0.0,
//   PublicKeyToken=7cec85d7bea7798e).
//
//   MyVisualSyntaxFunctionNode's ctor (Decompiled/VRage.Scripting/.../
//   MyVisualSyntaxFunctionNode.cs:33-85) tries four lookups, each ultimately
//   doing a string compare between the saved (mscorlib) signature and the
//   runtime (System.Private.CoreLib) signature. They never match, so
//   m_methodInfo stays null, Preprocess crashes when it touches m_methodInfo,
//   and every script that calls a method with a generic-instance parameter
//   (or a stock SDK provider call whose param list contains one) fails to
//   build. In Frostbite that's Mission01_MS, SetupPlayer, FollowPlayer,
//   FrostbiteBark_*, WeatherCycle, Obj00_Setup, etc. — the mission state
//   machine never advances and quest-log/notification calls never fire.
//
//   Type.GetType("...mscorlib...") DOES resolve correctly on .NET 10
//   (returning a System.Private.CoreLib type via the runtime's mscorlib
//   forwarding shim), so Type-level lookups in the chain work. Only the
//   string-keyed signature comparisons miss.
//
// Diagnostic confirmed it's not a registration problem:
//   GetMethods.Count grew 538 → 547 → 556 → 565 over the run as
//   GetMethod(type, sig) calls primed the registry. Init/RegisterLogicProvider
//   ran fine.
//
// Fix strategy: a single retry-postfix on the FunctionNode ctor that runs
// AFTER all four original lookup steps and only triggers when m_methodInfo
// is still null. It builds a normalized lookup key by stripping the
// "AssemblyName, Version=..., Culture=..., PublicKeyToken=..." chunks from
// inside [[ ... ]] brackets, then compares against the normalized
// Signature() of every registered method. Stripping (vs. rewriting
// mscorlib → System.Private.CoreLib) makes the fix stable across future
// .NET version bumps — the comparison reduces to "same type name in same
// namespace" regardless of which assembly the type lives in today.
//
// Two registries are searched: m_visualScriptingMethodsBySignature (via
// MyVisualScriptingProxy.GetMethods(), the proxy's flat string→method map)
// and m_whitelistedMethods (via GetWhitelistedMethods(null), the per-type
// HashSets that hold closed-form generic instances built by
// GetWhitelistedMethods(closedType) — that's where extension methods like
// MyVSCollectionExtensions.At<long> end up after MakeGenericMethod).
//
// Before searching, the patch primes the registry by re-issuing
// GetMethod(extType, sig) and GetMethod(declType, sig). The original ctor
// only invokes those when earlier steps fall through; re-running them here
// is idempotent (cached per-Type) and ensures the closed-generic forms are
// in m_whitelistedMethods before we iterate.
//
// Diagnostic patch in this folder (MyVisualSyntaxFunctionNodeDiagnosticPatch)
// continues to log when m_methodInfo is still null after this retry — i.e.
// it now serves as a regression detector. With this patch in place the
// expected log volume is zero.
[HarmonyPatch(typeof(MyVisualSyntaxFunctionNode), MethodType.Constructor,
    typeof(MyObjectBuilder_ScriptNode), typeof(Type))]
[HarmonyPatchCategory("Finish")]
static class MyVisualSyntaxFunctionNodeNetCoreLookupPatch
{
    // Strips the assembly-qualifier portion from inside [[ ... ]] brackets.
    // Matches one comma-prefixed run of the canonical four fields that
    // AssemblyQualifiedName produces:
    //     , <AsmName>, Version=N.N.N.N, Culture=<id>, PublicKeyToken=<hex|null>
    // Both the saved (mscorlib, 4.0.0.0, b77a5c561934e089) form and the
    // runtime (System.Private.CoreLib, 10.0.0.0, 7cec85d7bea7798e) form are
    // matched and removed, leaving e.g.  [[System.String]].
    private static readonly Regex s_asmQualifier = new Regex(
        @", [\w.]+, Version=\d+\.\d+\.\d+\.\d+, Culture=\w+, PublicKeyToken=(?:[a-fA-F0-9]+|null)",
        RegexOptions.Compiled);

    private static FieldInfo s_methodInfoField;
    private static MethodInfo s_initUsingMethod;

    // Index of normalized Signature() → MethodInfo, rebuilt when the union
    // registry size changes. The proxy grows the registry as
    // GetWhitelistedMethods(closedGeneric) is called for new types
    // throughout session load, so we can't index once and forget.
    private static Dictionary<string, MethodInfo> s_normalizedIndex;
    private static int s_indexedRegistrySize = -1;

    [HarmonyPostfix]
    [HarmonyPriority(Priority.First)]
    static void RetryWithNormalizedSignature(MyVisualSyntaxFunctionNode __instance,
                                             MethodInfo ___m_methodInfo,
                                             MyObjectBuilder_ScriptNode ob)
    {
        if (___m_methodInfo != null)
            return;

        var fn = ob as MyObjectBuilder_FunctionScriptNode;
        if (fn == null || string.IsNullOrEmpty(fn.Type))
            return;

        // Fast skip: if the saved signature carries no assembly qualifier,
        // the original lookup chain was already definitive and our
        // normalization can't help.
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

        s_methodInfoField ??= AccessTools.Field(typeof(MyVisualSyntaxFunctionNode), "m_methodInfo")
            ?? throw new InvalidOperationException(
                "MyVisualSyntaxFunctionNode.m_methodInfo not found");
        s_methodInfoField.SetValue(__instance, found);

        // Re-run the private InitUsing() the original ctor would have
        // called if the lookup had succeeded — it sets the
        // UsingDirectiveSyntax used by code generation.
        s_initUsingMethod ??= AccessTools.Method(typeof(MyVisualSyntaxFunctionNode), "InitUsing");
        try { s_initUsingMethod?.Invoke(__instance, null); }
        catch { /* best-effort; codegen falls back to no using */ }
    }

    // For closed-generic extension methods (e.g. MyVSCollectionExtensions.At<long>),
    // the closed form only enters m_whitelistedMethods when GetWhitelistedMethods
    // is called with the closed first-parameter type — that's what the ctor's
    // step-4 path does. Replay it here so the index includes the closed form.
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
            // Priming is best-effort; the lookup below is the source of truth.
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

    // Mirrors the original ctor's step-3 fallback (line 67 of
    // MyVisualSyntaxFunctionNode.cs:
    //   foreach (MethodInfo method in MyVisualScriptingProxy.GetMethods())
    //       if (method.Signature().StartsWith(value)) ...
    // ) but on normalized signatures, and also covering the
    // m_whitelistedMethods registry. This catches cases where the saved
    // signature has fewer parameters than the current runtime method —
    // e.g. MyVisualScriptLogicProvider.StoreStringList(String, List<String>)
    // whose runtime form gained two trailing optional params
    // (String secondaryKey, Boolean isStatic). Without the StartsWith
    // fallback such .vs files cannot bind to the live method at all.
    //
    // The boundary check ('next char is ',' or ')') prevents a saved
    // prefix from matching mid-parameter (e.g. "Foo(int a" must not match
    // "Foo(int able)").
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
            // Last writer wins on collisions — closed-generic forms are
            // appended to the registry after open generics, so they
            // overwrite the (less useful) open-form key. Open generics
            // produce malformed Signature() strings on .NET (FullName is
            // null for open generic types), so keeping them keyed under
            // the malformed string is harmless.
            if (!string.IsNullOrEmpty(key))
                idx[key] = m;
        }
        catch
        {
            // Skip methods whose Signature() throws (e.g. DeclaringType=null
            // on dynamic methods).
        }
    }

    public static string NormalizeAssemblyQualifiers(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
        return s_asmQualifier.Replace(s, "");
    }
}
