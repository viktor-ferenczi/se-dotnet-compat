using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using VRage.Network;
using VRage.Utils;

namespace ServerPlugin.Patches.Miscellaneous;

// Delegate and MulticastDelegate lost Serializable after .NET Framework, but
// both still belong in every replication table.
[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyTypeTable))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyTypeTablePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("IsSerializableClass")]
    private static bool IsSerializableClassPrefix(Type type, out bool __result)
    {
        __result = type.FullName is "System.Delegate" or "System.MulticastDelegate"

                   || (type.HasAttribute<SerializableAttribute>() && !type.HasAttribute<CompilerGeneratedAttribute>())
                   || type.IsEnum || typeof(MulticastDelegate).IsAssignableFrom(type.BaseType);

        return false;
    }
}

[HarmonyPatchCategory("Finish")]
[HarmonyPatch(typeof(MyReplicationLayerBase))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class MyReplicationLayerBasePatch
{
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
