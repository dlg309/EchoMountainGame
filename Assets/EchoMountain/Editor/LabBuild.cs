using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EchoMountain.EditorTools
{
    public static class LabBuild
    {
        private const string HudScene = "Assets/EchoMountain/Scenes/PhysicsLabHUD.unity";
        private const string RiderScene = "Assets/EchoMountain/Scenes/PhysicsLabRider.unity";

        [MenuItem("Echo Mountain/Build Windows physics lab")]
        public static void BuildWindows() =>
            BuildPlayer(HudScene, "Builds/PhysicsLab/EchoMountainLab.exe", BuildOptions.Development, "Echo Mountain Physics Lab - HUD");

        [MenuItem("Echo Mountain/Build Windows lab playtest")]
        public static void BuildWindowsPlaytest() =>
            BuildPlayer(RiderScene, "Builds/PhysicsLab-RiderPlaytest/EchoMountainLab.exe", BuildOptions.None, "Echo Mountain Rider Lab");

        [MenuItem("Echo Mountain/Build Windows Glacier Pass")]
        public static void BuildWindowsMountain() =>
            BuildPlayer(MountainBuilder.ScenePath, "Builds/GlacierPass-Tricks/EchoMountain.exe", BuildOptions.None, "Echo Mountain - Glacier Pass");

        private static void BuildPlayer(string scene, string output, BuildOptions buildOptions, string productName)
        {
            if (!File.Exists(scene)) throw new InvalidOperationException("Create and verify the scene first.");
            output = ResolveOutput(output);
            string destination = Path.GetDirectoryName(output);
            if (Directory.Exists(destination) || File.Exists(destination))
                throw new InvalidOperationException("Build output already exists. Choose a fresh folder.");

            string staging = destination + ".building-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(staging);
            string stagedPlayer = Path.Combine(staging, Path.GetFileName(output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = stagedPlayer,
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };
            string previousProductName = PlayerSettings.productName;
            try
            {
                PlayerSettings.productName = productName;
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded || !File.Exists(stagedPlayer))
                    throw new InvalidOperationException("Windows build did not produce a player: " + report.summary.result);
                Directory.Move(staging, destination);
                Debug.Log("Physics lab build succeeded: " + output);
            }
            finally
            {
                PlayerSettings.productName = previousProductName;
                if (Directory.Exists(staging))
                    Debug.LogWarning("Partial build retained at " + staging);
            }
        }

        private static string ResolveOutput(string defaultOutput)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(arguments, "-echoBuildOutput");
            if (index < 0) return Path.GetFullPath(defaultOutput);
            if (index + 1 >= arguments.Length ||
                !arguments[index + 1].EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("-echoBuildOutput requires a Windows executable path.");
            return Path.GetFullPath(arguments[index + 1]);
        }
    }
}
