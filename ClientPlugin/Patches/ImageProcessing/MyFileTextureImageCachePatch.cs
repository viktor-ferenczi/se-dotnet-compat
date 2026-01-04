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
        // Handle .zip files by replacing extension with .dds if the DDS file exists
        if (filepath.ToLower().EndsWith(".zip"))
        {
            string ddsPath = filepath.Substring(0, filepath.Length - 4) + ".dds";
            if (File.Exists(ddsPath))
            {
                filepath = ddsPath;
            }
            else
            {
                // DDS doesn't exist, return false to skip loading and use missing texture fallback
                return false;
            }
        }

        // Run the original with the potentially updated filepath
        return true;
    }
}
