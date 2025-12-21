// ReSharper disable CheckNamespace
// ReSharper disable InconsistentNaming

using System;
using System.Reflection;
using HarmonyLib;

// IMPORTANT: MUST NOT USE A NAMESPACE, otherwise Pulsar won't find the Preloader class! 
//namespace ClientPlugin;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using ClientPlugin;
using ClientPlugin.Patches.ImageProcessing;
using ClientPlugin.Patches.NullSafety;
using ClientPlugin.Patches.Serialization;
using Mono.Cecil;

// ReSharper disable once UnusedType.Global
public static class Preloader
{
    // ReSharper disable once UnusedMember.Global
    public static IEnumerable<string> TargetDLLs { get; } = [
        "Sandbox.Game.dll",
        "VRage.dll",
        "VRage.Game.dll",
        "VRage.Render.dll",
    ];

    // Assemblies to override with new versions
    private static readonly Dictionary<string, string> assemblyOverrides = new();

    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Patch(AssemblyDefinition asmDef)
    {
        Console.WriteLine($"{Plugin.Name}: Prepatch: {asmDef.Name}");

        AppContext.SetSwitch("System.Reflection.AssemblyLoadContext.EnableDiagnostics", true);
        
#if SIXLABORS_FIXES
        MyImagePrepatch.Prepatch(asmDef);
#endif

#if NULLABILITY_FIXES
        MyHeightMapLoadingSystemPrepatch.Prepatch(asmDef);
#endif
        
#if XML_FIXES
        XmlSerializationPrepatch.Prepatch(asmDef);
#endif
        
#if PROTOBUF_FIXES
        MyObjectBuilderSerializerKeenPrepatch.Prepatch(asmDef);
#endif
    }
    
    // ReSharper disable once UnusedMember.Global
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static void Hook()
    {
        Console.WriteLine($"{Plugin.Name}: Hook");
        
        LoadAssemblyOverrides();

        foreach (var path in assemblyOverrides.Values)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dll_path = path.Replace(@"C:\Users\viktor", home);
            Debug.Assert(File.Exists(dll_path), $"Missing assembly file: {dll_path}");
            try
            {
                Assembly.LoadFrom(dll_path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message}: {dll_path}");
            }
        }

        AppDomain.CurrentDomain.AssemblyResolve += GameAssemblyResolver(@"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64");

        // Enabling BinaryFormatter. This may not work on .Net 9
        // See https://learn.microsoft.com/en-us/dotnet/standard/serialization/binaryformatter-security-guide
        AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true);
        
#if DEBUG
        Harmony.DEBUG = true;
#endif

        var harmony = new Harmony("DotNetCompat");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    private static void LoadAssemblyOverrides()
    {
        assemblyOverrides["DotNetCompat"] = Assembly.GetExecutingAssembly().Location;
        
        assemblyOverrides["DirectShowLib"] = @"C:\Users\viktor\.nuget\packages\directshowlib\1.0.0\lib\DirectShowLib.dll";
        assemblyOverrides["GameAnalytics.Mono"] = @"C:\Users\viktor\.nuget\packages\gameanalytics.mono.sdk\3.3.5\lib\net45\GameAnalytics.Mono.dll";
        assemblyOverrides["Microsoft.CodeAnalysis"] = @"C:\Users\viktor\.nuget\packages\microsoft.codeanalysis.common\4.11.0\lib\netstandard2.0\Microsoft.CodeAnalysis.dll";
        assemblyOverrides["Microsoft.CodeAnalysis.CSharp"] = @"C:\Users\viktor\.nuget\packages\microsoft.codeanalysis.csharp\4.11.0\lib\netstandard2.0\Microsoft.CodeAnalysis.CSharp.dll";
        // assemblyOverrides["Newtonsoft.Json"] = @"C:\Users\viktor\.nuget\packages\newtonsoft.json\13.0.3\lib\netstandard2.0\Newtonsoft.Json.dll";
        assemblyOverrides["RestSharp"] = @"C:\Users\viktor\.nuget\packages\restsharp\106.6.10\lib\netstandard2.0\RestSharp.dll";
        // assemblyOverrides["SharpDX"] = @"C:\Users\viktor\.nuget\packages\sharpdx\4.2.0\lib\uap10.0\SharpDX.dll";
        // assemblyOverrides["SharpDX.D3DCompiler"] = @"C:\Users\viktor\.nuget\packages\sharpdx.d3dcompiler\4.2.0\lib\uap10.0\SharpDX.D3DCompiler.dll";
        // assemblyOverrides["SharpDX.DXGI"] = @"C:\Users\viktor\.nuget\packages\sharpdx.dxgi\4.2.0\lib\uap10.0\SharpDX.DXGI.dll";
        // assemblyOverrides["SharpDX.Desktop"] = @"C:\Users\viktor\.nuget\packages\sharpdx.desktop\4.2.0\lib\net45\SharpDX.Desktop.dll";
        // assemblyOverrides["SharpDX.Direct3D11"] = @"C:\Users\viktor\.nuget\packages\sharpdx.direct3d11\4.2.0\lib\uap10.0\SharpDX.Direct3D11.dll";
        // assemblyOverrides["SharpDX.DirectInput"] = @"C:\Users\viktor\.nuget\packages\sharpdx.directinput\4.2.0\lib\netstandard1.3\SharpDX.DirectInput.dll";
        // assemblyOverrides["SharpDX.XAudio2"] = @"C:\Users\viktor\.nuget\packages\sharpdx.xaudio2\4.2.0\lib\uap10.0\SharpDX.XAudio2.dll";
        // assemblyOverrides["SharpDX.XInput"] = @"C:\Users\viktor\.nuget\packages\sharpdx.xinput\4.2.0\lib\uap10.0\SharpDX.XInput.dll";
        assemblyOverrides["SixLabors.ImageSharp"] = @"C:\Users\viktor\.nuget\packages\sixlabors.imagesharp\3.1.5\lib\net6.0\SixLabors.ImageSharp.dll";
        // assemblyOverrides["Steamworks.NET"] = @"C:\Users\viktor\.nuget\packages\steamworks.net\20.1.0\lib\netstandard2.1\Steamworks.NET.dll";
        assemblyOverrides["System.Buffers"] = @"C:\Users\viktor\.nuget\packages\system.buffers\4.5.1\lib\netstandard2.0\System.Buffers.dll";
        assemblyOverrides["System.Collections.Immutable"] = @"C:\Users\viktor\.nuget\packages\system.collections.immutable\8.0.0\lib\net8.0\System.Collections.Immutable.dll";
        assemblyOverrides["System.ComponentModel.Annotations"] = @"C:\Users\viktor\.nuget\packages\system.componentmodel.annotations\4.6.0\lib\netstandard2.1\System.ComponentModel.Annotations.dll";
        // assemblyOverrides["System.Configuration.Install"] = @"C:\Users\viktor\.nuget\packages\microsoft.netframework.referenceassemblies.net461\1.0.3\build\.NETFramework\v4.6.1\System.Configuration.Install.dll";
        assemblyOverrides["System.Management"] = @"C:\Users\viktor\.nuget\packages\system.management\4.5.0\lib\netstandard2.0\System.Management.dll";
        assemblyOverrides["System.Management.dll"] = @"C:\Users\viktor\.nuget\packages\system.management\4.5.0\lib\netstandard2.0\System.Management.dll";
        assemblyOverrides["System.Memory"] = @"C:\Users\viktor\.nuget\packages\system.memory\4.5.5\lib\netstandard2.0\System.Memory.dll";
        assemblyOverrides["System.Runtime.CompilerServices.Unsafe"] = @"C:\Users\viktor\.nuget\packages\system.runtime.compilerservices.unsafe\6.0.0\lib\netstandard2.0\System.Runtime.CompilerServices.Unsafe.dll";
    }

    // ReSharper disable once UnusedMember.Global
    public static ResolveEventHandler GameAssemblyResolver(string bin64Dir)
    {
        return (sender, args) =>
        {
            var targetName = new AssemblyName(args.Name).Name;
            Debug.Assert(targetName != null);
            
            var targetPath = assemblyOverrides.TryGetValue(targetName, out var overriddenPath) ? overriddenPath : Path.Combine(bin64Dir, targetName);
            Console.WriteLine($"Assembly mapping: {targetName} from {targetPath}");

            // switch (targetName)
            // {
            //     case "System.Management":
            //         return Assembly.LoadFrom(@"C:\Users\viktor\.nuget\packages\system.management.dll\1.0.0\lib\System.Management.dll");
            // }

            if (File.Exists(targetPath + ".dll"))
                return Assembly.LoadFrom(targetPath + ".dll");

            if (File.Exists(targetPath + ".exe"))
                return Assembly.LoadFrom(targetPath + ".exe");

            Console.WriteLine($"WARNING: Could not find assembly file (dll or exe): {targetPath}.*");
            return null;
        };
    }    
}
