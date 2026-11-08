using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EchoMountain.EditorTools
{
    public static class LabBuild
    {
        private const string Scene = "Assets/EchoMountain/Scenes/PhysicsLabHUD.unity";
        private const string Output = "Builds/PhysicsLab/EchoMountainLab.exe";

        [MenuItem("Echo Mountain/Build Windows physics lab")]
        public static void BuildWindows()
        {
            if (!File.Exists(Scene)) throw new InvalidOperationException("Create and verify the physics lab scene first.");
            if (Directory.Exists(Path.GetDirectoryName(Output))) throw new InvalidOperationException("Build output directory already exists; archive it before building.");
            Directory.CreateDirectory(Path.GetDirectoryName(Output));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            string previousProductName = PlayerSettings.productName;
            BuildReport report;
            try
            {
                PlayerSettings.productName = "Echo Mountain Physics Lab - HUD";
                report = BuildPipeline.BuildPlayer(options);
            }
            finally { PlayerSettings.productName = previousProductName; }
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Physics lab build failed: " + report.summary.result);
            Debug.Log("Physics lab build succeeded: " + Output);
        }
    }
}
