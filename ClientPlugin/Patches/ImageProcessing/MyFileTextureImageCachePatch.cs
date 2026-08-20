using System;
using System.IO;
using HarmonyLib;
using VRage.Render11.Resources;

namespace ClientPlugin.Patches.ImageProcessing;

[HarmonyPatchCategory("Init")]
[HarmonyPatch(typeof(MyFileTextureImageCache))]
public static class MyFileTextureImageCachePatch
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MyFileTextureImageCache.LoadImage), typeof(string), typeof(bool))]
    private static bool LoadImagePrefix(ref string filepath)
    {
        if (filepath.ToLower().EndsWith(".zip"))
        {
            string ddsPath = filepath.Substring(0, filepath.Length - 4) + ".dds";
            if (File.Exists(ddsPath))
            {
                filepath = ddsPath;
            }
            else
            {
                return false;
            }
        }

        return true;
    }
}
