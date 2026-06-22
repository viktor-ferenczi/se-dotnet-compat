using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VRage.Network;
using VRage.Utils;

namespace ServerPlugin.Patches.Miscellaneous;

// Restores System.Delegate and System.MulticastDelegate to the replication type table on .NET 10.
//
// On .NET Framework 4.8 both types carry [Serializable], so MyTypeTable.IsSerializableClass admits
// them and the scan-time RegisterFromGameAssemblies pass pulls them in (via CreateBaseType, when a
// game-defined delegate type is registered). On .NET 10 they lost [Serializable], so the stock check
// rejects them and the scan produces a table short by exactly these two entries (712 instead of 714),
// which fails the client at join with "Bad number of types from server. Received 712, have 714" and/or
// post-join desync.
//
// CRITICAL — both patches here MUST be in the "Finish" category (applied by Preloader.Finish, before
// the game initializes), NOT "Init":
//
//   * On the client, Pulsar applies "Init" patches early enough to be in place before the scan, so the
//     client's equivalent patch can live in "Init".
//   * On the DS, Magnetar's loader runs Plugin.Init ("Init" category) *after* RegisterFromGameAssemblies
//     has already built and finalized the table. An "Init"-category patch is therefore inert against the
//     scan on the server.
//   * Preloader.Finish ("Finish" category) runs before any game initialization (before MySandboxGame and
//     before every RegisterFromGameAssemblies pass, including the offline layer built in
//     MySandboxGame.Initialize and the server layer built in MyMultiplayerServerBase).
//
// Two cooperating patches, both required:
//
//   1. IsSerializableClassPrefix (below): admits Delegate / MulticastDelegate. This is necessary because
//      MyTypeTable.Register gates on (IsReplicated || HasEvents || IsSerializableClass) — without the
//      prefix, an explicit Register(typeof(Delegate)) call silently no-ops on .NET 10.
//   2. MyReplicationLayerBasePatch.RegisterFromAssemblyPostfix (separate class below): after the scan,
//      EXPLICITLY calls Register(typeof(Delegate)) / Register(typeof(MulticastDelegate)) on the layer's
//      type table. This guarantees both types are present regardless of whether the scan happened to
//      walk up to them via CreateBaseType — the walk depends on which game delegate types the scan
//      materializes, which is exactly the non-determinism that made these "sometimes missing"
//      (and which the 2026-05-01 note observed varying with JIT tiering). In the common case the scan
//      already registered them (the prefix admits them mid-table), so the explicit Register is a no-op;
//      in the edge case where the walk missed them, it appends them. Ordering does not affect
//      correctness: the wire handshake reorders the client's m_idToType to server hash order and
//      server->client lookups are index-based after the reorder, so only the set and count must match.
//
// This deliberately replaces the earlier lazy approach (registering from a Serialize prefix behind a
// process-global flag). That was fragile: the flag was per-process while each MyReplicationLayerBase has
// its own MyTypeTable, so whichever instance serialized first flipped the flag and any later-built table
// (e.g. a fresh server layer) stayed at 712. The Serialize prefix is gone entirely — the stock
// MyTypeTable.Serialize is left in place now that the table is built correctly before any client joins.
[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyTypeTable))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyTypeTablePatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("IsSerializableClass")]
    private static bool IsSerializableClassPrefix(Type type, out bool __result)
    {
        // Replication layer compatibility with the original server.
        // These two items are present in the type table on .NET Framework 4.8 but lose
        // their [Serializable] attribute on .NET 10, so the original check rejects them.
        __result = type.FullName is "System.Delegate" or "System.MulticastDelegate"

                   // Otherwise use the original check
                   || (type.HasAttribute<SerializableAttribute>() && !type.HasAttribute<CompilerGeneratedAttribute>())
                   || type.IsEnum || typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);

        // Skip the original implementation
        return false;
    }
}

// Explicitly registers System.Delegate / System.MulticastDelegate into every replication type table
// right after RegisterFromGameAssemblies finishes its scan. See the header comment on MyTypeTablePatch
// for the full rationale; in short, this removes the dependency on the scan's CreateBaseType walk
// happening to reach the two types, which is what made them "sometimes missing" on the server.
//
// Must be in the "Finish" category so the patch is in place before RegisterFromGameAssemblies runs.
// Depends on MyTypeTablePatch.IsSerializableClassPrefix being active too — otherwise MyTypeTable.Register
// rejects both types at its (IsReplicated || HasEvents || IsSerializableClass) gate and the calls no-op.
[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyReplicationLayerBase))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyReplicationLayerBasePatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MyReplicationLayerBase.RegisterFromAssembly), typeof(IEnumerable<Assembly>))]
    private static void RegisterFromAssemblyPostfix(MyTypeTable ___m_typeTable)
    {
        if (___m_typeTable == null)
            return;

        if (___m_typeTable.Contains(typeof(Delegate)) && ___m_typeTable.Contains(typeof(MulticastDelegate)))
            return;

        try
        {
            // Register Delegate first so MulticastDelegate's CreateBaseType walk finds it already present.
            ___m_typeTable.Register(typeof(Delegate));
            ___m_typeTable.Register(typeof(MulticastDelegate));

            var ok = ___m_typeTable.Contains(typeof(Delegate)) && ___m_typeTable.Contains(typeof(MulticastDelegate));
            MyLog.Default.Log(ok ? MyLogSeverity.Info : MyLogSeverity.Error,
                "[DotNetCompat] Explicitly registered Delegate/MulticastDelegate after scan: present={0}. " +
                "If false, the IsSerializableClass prefix is not active (check it is in the \"Finish\" category).",
                ok);
        }
        catch (Exception ex)
        {
            MyLog.Default.Log(MyLogSeverity.Error,
                "[DotNetCompat] Failed to register Delegate/MulticastDelegate after scan: {0}", ex);
        }
    }
}
