using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp;
using ServerPlugin.Rewriter;
using VRage.Plugins;
using VRage.Scripting;
// Define assembly version when compiled by Magnetar
#if !LOCAL_BUILD
using System.Reflection;

[assembly: AssemblyVersion("10.0.0.0")]
[assembly: AssemblyFileVersion("10.0.0.0")]

#endif

namespace ServerPlugin;

public class Plugin : IPlugin
{
    public const string Name = "DotNetCompat";

    public static CSharpCompilation Rewrite(CSharpCompilation compilation, MyApiTarget target) =>
        CompilationRewriter.Rewrite(compilation, target);

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining
    )]
    public void Init(object gameInstance)
    {
        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Init");
    }

    public void Dispose() { }

    public void Update() { }
}
