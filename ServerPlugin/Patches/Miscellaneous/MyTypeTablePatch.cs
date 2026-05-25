using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace ServerPlugin.Patches.Miscellaneous;

// Lazily force-registers System.Delegate and System.MulticastDelegate in the replication
// type table the first time MyTypeTable.Serialize writes to the wire. On .NET 10 these two
// types lost their [Serializable] attribute, so MyTypeTable.IsSerializableClass no longer
// admits them and they are not picked up by the normal RegisterFromGameAssemblies scan.
//
// Two-part fix:
//
// 1. IsSerializableClass prefix below: makes MyTypeTable.IsSerializableClass return true for
//    Delegate / MulticastDelegate. As Docs/Fixes.md explains, on the DS this prefix is inert
//    against the scan-time RegisterFromGameAssemblies pass (Pulsar.Legacy.Loader.PluginLoader
//    runs Plugin Init *after* MyTypeTable.Preallocate populates the table), so it cannot
//    inject the types at scan time. But it IS active for any later call into Register, and
//    the lazy-register below is one such call.
//
// 2. SerializePrefix lazy register: the first MyTypeTable.Serialize write happens at
//    client-join time, well after plugin Init, so the IsSerializableClass prefix is in place.
//    Calling Register(typeof(Delegate)) / Register(typeof(MulticastDelegate)) then passes the
//    IsReplicated || HasEvents || IsSerializableClass gate and the two types get appended to
//    m_idToType / m_hashLookup / m_typeLookup. Without both pieces in place the hash list
//    sent to the client is short by exactly these two entries, producing the fatal
//    "Bad number of types from server. Received 712, have 714" mismatch at join time.
//
// Once Register has accepted the types they end up in m_typeLookup, so subsequent joins skip
// the work via the m_typeLookup TryGet at the top of Register. The _delegateTypesRegistered
// flag is just there to keep the log line one-shot per process. Register Delegate first so
// MulticastDelegate's CreateBaseType walk finds it already present.
[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyTypeTable))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyTypeTablePatch
{
    private static bool _delegateTypesRegistered;

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

    // Serializes id to hash list.
    // Server sends the hashlist to client, client reorders type table to same order as server.
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("Serialize")]
    private static bool SerializePrefix(
        MyTypeTable __instance,
        BitStream stream,
        List<MySynchronizedTypeInfo> ___m_idToType,
        ref MyEventTable ___m_staticEventTable,
        Dictionary<int, MySynchronizedTypeInfo> ___m_hashLookup)
    {
        // Replacement implementation with additional error handling to catch issues with the replication tables

        if (stream.Writing)
        {
            if (!_delegateTypesRegistered)
            {
                try
                {
                    var before = ___m_idToType.Count;
                    __instance.Register(typeof(Delegate));
                    __instance.Register(typeof(MulticastDelegate));
                    var after = ___m_idToType.Count;
                    MyLog.Default.Log(MyLogSeverity.Warning,
                        "[DotNetCompat] Lazy-registered Delegate/MulticastDelegate in Serialize: typeTable size {0} -> {1}",
                        before, after);
                }
                catch (Exception ex)
                {
                    MyLog.Default.Log(MyLogSeverity.Error,
                        "[DotNetCompat] Failed to lazy-register Delegate/MulticastDelegate: {0}", ex);
                }
                _delegateTypesRegistered = true;
            }

            stream.WriteVariant((uint)___m_idToType.Count);
            foreach (var t in ___m_idToType) stream.WriteInt32(t.TypeHash);

            // Skip the original implementation
            return false;
        }

        var num = (int)stream.ReadUInt32Variant();
        if (___m_idToType.Count != num)
            // This is a fatal error condition, because of m_idToType[j] in the logic below
            throw new Exception($"Bad number of types from server. Received {num}, have {___m_idToType.Count}");

        for (var i = 0; i < num; i++) ___m_idToType[i] = null;

        var staticEventTable = new MyEventTable(null);
        ___m_staticEventTable = staticEventTable;
        for (var j = 0; j < num; j++)
        {
            var num2 = stream.ReadInt32();
            if (!___m_hashLookup.TryGetValue(num2, out var mySynchronizedTypeInfo)) throw new Exception("Type hash not found! Value: " + num2);
            ___m_idToType[j] = mySynchronizedTypeInfo;
            staticEventTable.AddStaticEvents(mySynchronizedTypeInfo.Type);
        }

        for (var i = 0; i < num; i++)
            if (___m_idToType[i] == null)
                throw new Exception($"Type ID {i} is missing after the reordering based on server response");

        // Skip the original implementation
        return false;
    }
}
