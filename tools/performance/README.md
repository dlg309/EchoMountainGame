# Standalone performance QA probe

This probe is for an isolated copy of the Unity project. It does not belong in the normal player build. Copy `EchoPerformanceQA.cs` to `Assets/EchoMountain/PerformanceQA/` and `EchoPerformanceBuild.cs` to its `Editor/` subfolder in the isolated copy. Unity generates the metadata there. The source scenes in this repository are not edited.

Build a new Windows player in a fresh output directory with Unity 6000.3.28f1:

```powershell
Unity.exe -batchmode -nographics -projectPath <QA-copy> -executeMethod EchoPerformanceBuild.BuildRelease -echoBuildOutput <QA-copy>/Builds/<fresh-release>/EchoMountain.exe -logFile <QA-copy>/perf-release-build.log -quit
Unity.exe -batchmode -nographics -projectPath <QA-copy> -executeMethod EchoPerformanceBuild.BuildDevelopment -echoBuildOutput <QA-copy>/Builds/<fresh-development>/EchoMountain.exe -logFile <QA-copy>/perf-development-build.log -quit
```

Run one player at a time, with its window visible. Use `-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -force-d3d11 -echoPerf -echoPerfOutput <absolute-csv>`. Optional `-echoPerfMode Pine|Summit|FreeRoam` runs only that mode three times; `-echoPerfCadenceOnly` runs the 30/60/120 comparison; `-echoPerfRailOnly` captures one Switchyard box approach and records the grinding state; `-echoPerfQuick` shortens general mode rides to 12 seconds for development diagnosis. The standard mode pass captures 61 seconds after three seconds of warmup. The cadence pass captures 35 seconds at each cap. `Application.targetFrameRate` is 60 and vSync is off for riding comparisons; cadence changes only the cap. The script moves between scenes, feeds a virtual gamepad with alternating turns and periodic braking, and requests pops every 13 seconds. It logs actual frame intervals, Unity frame timing, memory, collection counts, rider state, and profiler markers to CSV.

The probe buffers numeric samples during each ride, then formats CSV outside capture. It still adds a virtual input device and profiler sampling overhead. Inspect `PERF_QA RUN` and `PERF_QA CADENCE` log entries. Exclude any pass that contains fewer than 60 seconds of rendered frame intervals. The `tools/performance/analyze.py` script summarizes CSV measurements. A development player has profiling overhead and must be reported separately from release-style riding results.
