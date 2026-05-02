using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VRage.Library.Collections;
using VRage.Network;
using VRage.Utils;

namespace ClientPlugin.Patches.Miscellaneous;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyTypeTable))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyTypeTablePatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("IsSerializableClass")]
    private static bool IsSerializableClassPrefix(Type type, out bool __result)
    {
        // Replication layer compatibility with the original server
        // These two items are present in the type table on .NET Framework 4.8
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
        BitStream stream,
        List<MySynchronizedTypeInfo> ___m_idToType,
        ref MyEventTable ___m_staticEventTable,
        Dictionary<int, MySynchronizedTypeInfo> ___m_hashLookup)
    {
        // Replacement implementation with additional error handling to catch issues with the replication tables

        if (stream.Writing)
        {
            stream.WriteVariant((uint)___m_idToType.Count);
            foreach (var t in ___m_idToType) stream.WriteInt32(t.TypeHash);

            // Skip the original implementation
            return false;
        }

        var num = (int)stream.ReadUInt32Variant();
        if (___m_idToType.Count != num)
        {
            // Read the server's hash list so we can report which types differ before bailing.
            // The stream is consumed regardless; we cannot recover the connection from here.
            var serverHashes = new int[num];
            for (var i = 0; i < num; i++) serverHashes[i] = stream.ReadInt32();
            LogTypeTableMismatch(serverHashes, ___m_idToType, ___m_hashLookup);

            // This is a fatal error condition, because of m_idToType[j] in the logic below
            throw new Exception($"Bad number of types from server. Received {num}, have {___m_idToType.Count}");
        }

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

    private static void LogTypeTableMismatch(
        int[] serverHashes,
        List<MySynchronizedTypeInfo> idToType,
        Dictionary<int, MySynchronizedTypeInfo> hashLookup)
    {
        var serverSet = new HashSet<int>(serverHashes);
        var clientSet = new HashSet<int>(idToType.Select(t => t.TypeHash));

        var serverOnly = serverHashes.Where(h => !clientSet.Contains(h)).ToList();
        var clientOnly = idToType.Where(t => !serverSet.Contains(t.TypeHash)).ToList();

        MyLog.Default.Log(MyLogSeverity.Warning,
            "[DotNetCompat] Replication type-table mismatch: server={0} client={1} (server-only={2}, client-only={3})",
            serverHashes.Length, idToType.Count, serverOnly.Count, clientOnly.Count);

        foreach (var hash in serverOnly)
        {
            // Server-only hashes correspond to types the client has not registered.
            // We may still find the hash in the client's hash lookup if the type exists
            // but is not in m_idToType for some reason; report that case explicitly.
            var name = hashLookup.TryGetValue(hash, out var info) && info.Type != null
                ? info.Type.FullName + " (in hashLookup but not idToType)"
                : "<unknown type>";
            MyLog.Default.Log(MyLogSeverity.Warning,
                "[DotNetCompat]   missing on client: hash=0x{0:X8} type={1}", hash, name);
        }

        foreach (var info in clientOnly)
        {
            var name = info.Type != null ? info.Type.FullName : "<null type>";
            MyLog.Default.Log(MyLogSeverity.Warning,
                "[DotNetCompat]   extra on client: hash=0x{0:X8} type={1}", info.TypeHash, name);
        }
    }
}