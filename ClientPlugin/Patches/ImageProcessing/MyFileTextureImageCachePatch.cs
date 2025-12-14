using System;
using System.IO;
using HarmonyLib;
using VRage.Render11.Resources;

namespace ClientPlugin.Patches.ImageProcessing;

[HarmonyPatch(typeof(MyFileTextureImageCache))]
// ReSharper disable once UnusedType.Global
public static class MyFileTextureImageCachePatch
{
    [HarmonyPrefix]
    [HarmonyPatch("LoadImage", typeof(string), typeof(bool))]
    // ReSharper disable once UnusedMember.Local
    private static bool LoadImagePrefix(ref string filepath)
    {
        // Handle .zip files by replacing extension with .dds
        if (filepath.ToLower().EndsWith(".zip"))
        {
            filepath = filepath.Substring(0, filepath.Length - 4) + ".dds";
            if (!File.Exists(filepath)) throw new Exception($"DDS file extracted from ZIP is missing: {filepath}");
        }

        // Run the original with the potentially update filepath
        return true;
    }
}