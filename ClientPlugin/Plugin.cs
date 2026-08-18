using HarmonyLib;
using VRage.Plugins;

// Pulsar does not generate the assembly version from the project file.
#if PULSAR
using System.Reflection;

[assembly: AssemblyVersion("10.0.5.0")]
[assembly: AssemblyFileVersion("10.0.5.0")]
#endif

namespace ClientPlugin;

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
