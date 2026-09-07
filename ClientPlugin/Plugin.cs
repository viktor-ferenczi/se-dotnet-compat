using ClientPlugin.Rewriter;
using HarmonyLib;
using Microsoft.CodeAnalysis.CSharp;
using VRage.Plugins;
using VRage.Scripting;
// Define assembly version when compiled by Pulsar
#if !LOCAL_BUILD
using System.Reflection;

[assembly: AssemblyVersion("10.0.5.0")]
[assembly: AssemblyFileVersion("10.0.5.0")]

#endif

namespace ClientPlugin;

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
        // The game calls IPlugin.Init only after MySandboxGame.Initialize(), and
        // on a dedicated server that call has already loaded the world. Anything
        // the world load depends on belongs in the "Finish" category, applied
        // from Preloader.Finish before the game starts. "Init" is for patches
        // that need a running game instance.
        var harmony = new Harmony("DotNetCompat");
        harmony.PatchCategory("Init");
    }

    public void Dispose() { }

    public void Update() { }
}
