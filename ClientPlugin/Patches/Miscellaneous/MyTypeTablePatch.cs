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
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyTypeTable.IsSerializableClass))]
    private static bool IsSerializableClassPrefix(Type type, out bool __result)
    {
        // These types lost Serializable after .NET Framework.
        __result =
            type.FullName is "System.Delegate" or "System.MulticastDelegate"
            || (
                type.HasAttribute<SerializableAttribute>()
                && !type.HasAttribute<CompilerGeneratedAttribute>()
            )
            || type.IsEnum
            || typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);

        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyTypeTable.Serialize))]
    private static bool SerializePrefix(MyTypeTable __instance, BitStream stream)
    {
        var idToType = __instance.m_idToType;
        var hashLookup = __instance.m_hashLookup;

        if (stream.Writing)
        {
            stream.WriteVariant((uint)idToType.Count);
            foreach (var t in idToType)
                stream.WriteInt32(t.TypeHash);

            return false;
        }

        var num = (int)stream.ReadUInt32Variant();
        if (idToType.Count != num)
        {
            // Read the server list before failing so the log can show the difference.
            var serverHashes = new int[num];
            for (var i = 0; i < num; i++)
                serverHashes[i] = stream.ReadInt32();
            LogTypeTableMismatch(serverHashes, idToType, hashLookup);

            throw new Exception(
                $"Bad number of types from server. Received {num}, have {idToType.Count}"
            );
        }

        for (var i = 0; i < num; i++)
            idToType[i] = null;

        var staticEventTable = new MyEventTable(null);
        __instance.m_staticEventTable = staticEventTable;
        for (var j = 0; j < num; j++)
        {
            var num2 = stream.ReadInt32();
            if (!hashLookup.TryGetValue(num2, out var mySynchronizedTypeInfo))
                throw new Exception("Type hash not found! Value: " + num2);
            idToType[j] = mySynchronizedTypeInfo;
            staticEventTable.AddStaticEvents(mySynchronizedTypeInfo.Type);
        }

        for (var i = 0; i < num; i++)
            if (idToType[i] == null)
                throw new Exception(
                    $"Type ID {i} is missing after the reordering based on server response"
                );

        return false;
    }

    private static void LogTypeTableMismatch(
        int[] serverHashes,
        List<MySynchronizedTypeInfo> idToType,
        Dictionary<int, MySynchronizedTypeInfo> hashLookup
    )
    {
        var serverSet = new HashSet<int>(serverHashes);
        var clientSet = new HashSet<int>(idToType.Select(t => t.TypeHash));

        var serverOnly = serverHashes.Where(h => !clientSet.Contains(h)).ToList();
        var clientOnly = idToType.Where(t => !serverSet.Contains(t.TypeHash)).ToList();

        MyLog.Default.Log(
            MyLogSeverity.Warning,
            "[DotNetCompat] Replication type-table mismatch: server={0} client={1} (server-only={2}, client-only={3})",
            serverHashes.Length,
            idToType.Count,
            serverOnly.Count,
            clientOnly.Count
        );

        foreach (var hash in serverOnly)
        {
            var name =
                hashLookup.TryGetValue(hash, out var info) && info.Type != null
                    ? info.Type.FullName + " (in hashLookup but not idToType)"
                    : "<unknown type>";
            MyLog.Default.Log(
                MyLogSeverity.Warning,
                "[DotNetCompat]   missing on client: hash=0x{0:X8} type={1}",
                hash,
                name
            );
        }

        foreach (var info in clientOnly)
        {
            var name = info.Type != null ? info.Type.FullName : "<null type>";
            MyLog.Default.Log(
                MyLogSeverity.Warning,
                "[DotNetCompat]   extra on client: hash=0x{0:X8} type={1}",
                info.TypeHash,
                name
            );
        }
    }
}
