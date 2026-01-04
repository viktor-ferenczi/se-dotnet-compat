using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Xml;
using HarmonyLib;
using Sandbox;
using Sandbox.Engine.Utils;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRage.Collections;
using VRage.FileSystem;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace ClientPlugin.Patches.Scripting;

[HarmonyPatch(typeof(MySpaceGameDefaultIlCompiler))]
public static class MySpaceGameDefaultIlCompilerPatch
{
    // ReSharper disable once UnusedMember.Local
    [HarmonyPrefix]
    [HarmonyPatch("InitIlCompiler")]
    private static bool InitIlCompilerPrefix(MySpaceGameDefaultIlCompiler __instance)
    {
        // Replacement implementation

        // assemblyPaths
        var list = new List<string>
        {
            Path.Combine(Assembly.Load("netstandard").Location),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Game.dll"),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Common.dll"),
            Path.Combine(MyFileSystem.ExePath, "Sandbox.Graphics.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Library.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Math.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Game.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Render.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Input.dll"),
            Path.Combine(MyFileSystem.ExePath, "VRage.Scripting.dll"),
            Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.ObjectBuilders.dll"),
            Path.Combine(MyFileSystem.ExePath, "SpaceEngineers.Game.dll"),
            Path.Combine(MyFileSystem.ExePath, "ProtoBuf.Net.Core.dll")
        };

        var assemblyLocations = list;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var name = assembly.GetName().Name;
            if (name.StartsWith("System.")) assemblyLocations.Add(assembly.Location);
        }

        MyVRage.Platform.Scripting.Initialize(MySandboxGame.Static.UpdateThread, assemblyLocations, [
            typeof(MyTuple),
            typeof(Vector2),
            typeof(Game),
            typeof(ITerminalAction),
            typeof(IMyGridTerminalSystem),
            typeof(MyModelComponent),
            typeof(IMyComponentAggregate),
            typeof(ListReader<>),
            typeof(MyObjectBuilder_FactionDefinition),
            typeof(IMyCubeBlock),
            typeof(MyIni),
            typeof(ImmutableArray),
            typeof(IMyAirVent),
            typeof(MySprite),
            typeof(VRage.Scripting.MemorySafeTypes.MemorySafeArrayList),

            // Items moved from MyScriptCompiler's constructor
            __instance.GetType(),
            typeof(int),
            typeof(XmlEntity),
            typeof(HashSet<>),
            typeof(Dictionary<,>),
            typeof(Uri)
        ], [
            MySpaceGameDefaultIlCompiler.GetPrefixedBranchName(),
            "STABLE",
            string.Empty,
            string.Empty,
            "VERSION_" + ((Version)MyFinalBuildConstants.APP_VERSION).Minor,
            "BUILD_" + ((Version)MyFinalBuildConstants.APP_VERSION).Build
        ], MyFakes.ENABLE_ROSLYN_SCRIPT_DIAGNOSTICS ? Path.Combine(MyFileSystem.UserDataPath, "ScriptDiagnostics") : null, MyFakes.ENABLE_SCRIPTS_PDB);

        // Skip the original implementation
        return false;
    }
}