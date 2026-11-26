using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class EchoPerformanceBuild
{
    public static void BuildRelease() => Build(false);
    public static void BuildDevelopment() => Build(true);
    private static void Build(bool development)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-echoBuildOutput");
        if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Missing -echoBuildOutput");
        string output = Path.GetFullPath(args[index + 1]);
        if (Directory.Exists(Path.GetDirectoryName(output))) throw new InvalidOperationException("Output exists");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        bool old = PlayerSettings.enableFrameTimingStats;
        string product = PlayerSettings.productName;
        try
        {
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.productName = "Echo Mountain - Performance QA";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/EchoMountain/Scenes/GlacierPassPolished.unity", "Assets/EchoMountain/Scenes/SunsetBasin.unity" },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("PERF_QA BUILD " + (development ? "development" : "release") + " " + output);
        }
        finally { PlayerSettings.enableFrameTimingStats = old; PlayerSettings.productName = product; }
    }
}
