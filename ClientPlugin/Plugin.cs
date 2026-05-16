using HarmonyLib;
using VRage.Plugins;

// Set the assembly version manually if compiled by Pulsar (it won't create what was in AssemblyInfo.cs before)
#if !DEV_BUILD
using System.Reflection;

[assembly: AssemblyVersion("10.0.3.0")]
[assembly: AssemblyFileVersion("10.0.3.0")]
#endif

namespace ClientPlugin;

// ReSharper disable once UnusedType.Global
public class Plugin : IPlugin
{
    public const string Name = "DotNetCompat";

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public void Init(object gameInstance)
    {
        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Init");
    }

    public void Dispose()
    {
    }

    public void Update()
    {
    }
}