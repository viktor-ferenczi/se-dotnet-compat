using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Havok;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Gui;
using VRageMath;

namespace ClientPlugin.Patches.Miscellaneous;

[HarmonyPatch(typeof(MyGridShape))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
// ReSharper disable once UnusedType.Global
public static class MyGridShapePatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("AddShapesFromCollector")]
    private static bool AddShapesFromCollectorTranspiler(MyCubeBlockCollector ___m_blockCollector, HkGridShape ___m_root)
    {
        // Replacement implementation to avoid crash in stackalloc

        var m_blockCollector = ___m_blockCollector;
        var m_root = ___m_root;

        var shapes = new HkShape[255];

        var num = 0;
        for (var i = 0; i < m_blockCollector.ShapeInfos.Count; i++)
        {
            var shapeInfo = m_blockCollector.ShapeInfos[i];
            HkShape[] obj = null;
            Span<HkShape> span;
            span = shapeInfo.Count >= 256 ? (Span<HkShape>)(obj = new HkShape[shapeInfo.Count]) : shapes.AsSpan(0, shapeInfo.Count);
            for (var j = 0; j < shapeInfo.Count; j++) span[j] = m_blockCollector.Shapes[num + j];
            num += shapeInfo.Count;
            if (m_root.ShapeCount + shapeInfo.Count > 64879) MyHud.Notifications.Add(MyNotificationSingletons.GridReachedPhysicalLimit);
            if (m_root.ShapeCount + shapeInfo.Count < 65536) m_root.AddShapes(span, new Vector3S(shapeInfo.Min), new Vector3S(shapeInfo.Max));
            GC.KeepAlive(obj);
        }

        // Skip the original implementation
        return false;
    }
}