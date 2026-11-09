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

        private static void BuildPlayer(string scene, string output, BuildOptions buildOptions, string productName)
        {
            if (!File.Exists(scene)) throw new InvalidOperationException("Create and verify the physics lab scene first.");
            if (Directory.Exists(Path.GetDirectoryName(output))) throw new InvalidOperationException("Build output directory already exists; archive it before building.");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = buildOptions
            };
            string previousProductName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                PlayerSettings.productName = productName;
                report = BuildPipeline.BuildPlayer(options);
            }
            finally { PlayerSettings.productName = previousProductName; }
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Physics lab build failed: " + report.summary.result);
            Debug.Log("Physics lab build succeeded: " + output);
        }
    }
}
