using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using EchoMountain.Riding;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public sealed class EchoPerformanceQA : MonoBehaviour
{
    private static readonly FrameTiming[] Timings = new FrameTiming[1];
    private StreamWriter writer;
    private Stopwatch watch;
    private long lastTicks;
    private bool capture;
    private string label;
    private Gamepad pad;
    private MountainRun run;
    private LabRider rider;
    private Rigidbody body;
    private ProfilerRecorder gcBytes, mainThread, renderThread, physics, scripts, render;
    private double driveStart;
    private int lastJump, sampleCount;
    private string output;
    private bool quick;
    private bool straight;
    private bool cadenceOnly;
    private bool railOnly;
    private bool railPopped, railEntered;
    private int grindingFrames;
    private string onlyMode;
    private readonly FrameSample[] samples = new FrameSample[12000];

    private struct FrameSample
    {
        public double wall, cpu, gpu, cpuMain, cpuRender, speed;
        public long gcBytes, main, render, physics, scripts, draw, alloc, reserved, mono;
        public int gc0, grounded, landings, reset, particles, phase, width, height, grinding;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-echoPerf") < 0) return;
        DontDestroyOnLoad(new GameObject("Echo Performance QA").AddComponent<EchoPerformanceQA>().gameObject);
    }

    private void Awake()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-echoPerfOutput");
        output = index >= 0 && index + 1 < args.Length ? args[index + 1] :
            Path.Combine(Application.temporaryCachePath, "echo-performance.csv");
        quick = Array.IndexOf(args, "-echoPerfQuick") >= 0;
        cadenceOnly = Array.IndexOf(args, "-echoPerfCadenceOnly") >= 0;
        railOnly = Array.IndexOf(args, "-echoPerfRailOnly") >= 0;
        int modeIndex = Array.IndexOf(args, "-echoPerfMode");
        onlyMode = modeIndex >= 0 && modeIndex + 1 < args.Length ? args[modeIndex + 1] : null;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
        writer = new StreamWriter(output, false, new UTF8Encoding(false));
        writer.AutoFlush = false;
        writer.WriteLine("label,frame,wall_ms,cpu_ms,gpu_ms,cpu_main_ms,cpu_render_ms,gc_bytes,main_ns,render_ns,physics_ns,scripts_ns,draw_ns,gc0,unity_alloc_bytes,unity_reserved_bytes,mono_used_bytes,speed_kmh,grounded,landings,reset,particles,phase,screen_w,screen_h,grinding");
        watch = Stopwatch.StartNew();
        lastTicks = watch.ElapsedTicks;
        gcBytes = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread");
        physics = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate");
        scripts = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate");
        render = ProfilerRecorder.StartNew(ProfilerCategory.Render, "RenderLoop.Draw");
        var available = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(available);
        var names = new StringBuilder("PERF_QA MARKERS ");
        foreach (var handle in available)
        {
            var description = ProfilerRecorderHandle.GetDescription(handle);
            string name = description.Name;
            if (name.IndexOf("Physics", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Render", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Script", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("GC ", StringComparison.OrdinalIgnoreCase) >= 0)
                names.Append(description.Category).Append('/').Append(name).Append(';');
        }
        Debug.Log(names.ToString());
        pad = InputSystem.AddDevice<Gamepad>();
        StartCoroutine(Sequence());
    }

    private void Update()
    {
        if (!run || run.Phase != MountainPhase.Riding) return;
        if (railOnly)
        {
            InputSystem.QueueStateEvent(pad, default(GamepadState));
            if (!railPopped && body.position.z >= 291f)
            {
                railPopped = true;
                rider.RequestJump();
                Debug.Log($"PERF_QA RAIL_POP z={body.position.z:F2}");
            }
            if (rider.IsGrinding)
            {
                grindingFrames++;
                if (!railEntered)
                {
                    railEntered = true;
                    Debug.Log($"PERF_QA RAIL_ENTER z={body.position.z:F2}");
                }
            }
            return;
        }
        double t = Time.realtimeSinceStartupAsDouble - driveStart;
        float steer = straight ? 0f : (float)(Math.Sin(t * 0.63) * 0.55);
        float brake = !straight && t % 18.0 > 11.0 && t % 18.0 < 15.0 ? 0.65f : 0f;
        InputSystem.QueueStateEvent(pad, new GamepadState {
            leftStick = new Vector2(steer, 0f),
            leftTrigger = brake
        });
        int jump = (int)(t / 13.0);
        if (!straight && jump > lastJump && t > 2.0)
        {
            lastJump = jump;
            rider.RequestJump();
        }
    }

    private void LateUpdate()
    {
        long now = watch.ElapsedTicks;
        double wall = (now - lastTicks) * 1000.0 / Stopwatch.Frequency;
        lastTicks = now;
        FrameTimingManager.CaptureFrameTimings();
        uint count = FrameTimingManager.GetLatestTimings(1, Timings);
        if (!capture || !run || run.Phase != MountainPhase.Riding || !body) return;
        FrameTiming timing = count > 0 ? Timings[0] : default;
        if (sampleCount >= samples.Length) return;
        var spray = run.GetComponent<MountainSnowSpray>();
        samples[sampleCount++] = new FrameSample
        {
            wall = wall, cpu = timing.cpuFrameTime, gpu = timing.gpuFrameTime,
            cpuMain = timing.cpuMainThreadFrameTime, cpuRender = timing.cpuRenderThreadFrameTime,
            gcBytes = gcBytes.Valid ? gcBytes.LastValue : -1,
            main = mainThread.Valid ? mainThread.LastValue : -1,
            render = renderThread.Valid ? renderThread.LastValue : -1,
            physics = physics.Valid ? physics.LastValue : -1,
            scripts = scripts.Valid ? scripts.LastValue : -1,
            draw = render.Valid ? render.LastValue : -1,
            gc0 = GC.CollectionCount(0),
            alloc = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
            reserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(),
            mono = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(),
            speed = rider.SpeedKmh, grounded = rider.IsGrounded ? 1 : 0,
            landings = rider.LandingCount, reset = (int)rider.ResetCount,
            particles = spray ? spray.ParticleCount : 0,
            phase = (int)run.Phase, width = Screen.width, height = Screen.height,
            grinding = rider.IsGrinding ? 1 : 0
        };
    }

    private void Write(double value) { writer.Write(','); writer.Write(value.ToString("F4", System.Globalization.CultureInfo.InvariantCulture)); }
    private void Write(long value) { writer.Write(','); writer.Write(value); }
    private void Write(int value) { writer.Write(','); writer.Write(value); }

    private void FlushSamples()
    {
        for (int i = 0; i < sampleCount; i++)
        {
            var s = samples[i];
            writer.Write(label); writer.Write(','); writer.Write(i + 1);
            Write(s.wall); Write(s.cpu); Write(s.gpu); Write(s.cpuMain); Write(s.cpuRender);
            Write(s.gcBytes); Write(s.main); Write(s.render); Write(s.physics);
            Write(s.scripts); Write(s.draw); Write(s.gc0); Write(s.alloc);
            Write(s.reserved); Write(s.mono); Write(s.speed); Write(s.grounded);
            Write(s.landings); Write(s.reset); Write(s.particles);
            writer.Write(','); writer.Write((MountainPhase)s.phase);
            Write(s.width); Write(s.height); Write(s.grinding);
            writer.WriteLine();
        }
        writer.Flush();
    }

    private IEnumerator Sequence()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.SetResolution(1920, 1080, false);
        yield return new WaitForSecondsRealtime(2f);
        Debug.Log($"PERF_QA START {Application.unityVersion} {SystemInfo.graphicsDeviceName} {SystemInfo.graphicsDeviceType} {Screen.width}x{Screen.height} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} frameTiming={FrameTimingManager.IsFeatureEnabled()} output={output}");
        string[] modes = { "Pine", "Summit", "FreeRoam" };
        if (railOnly)
        {
            yield return LoadMode("FreeRoam", 3);
            yield return new WaitForSecondsRealtime(2f);
            label = "Rail";
            sampleCount = 0;
            lastTicks = watch.ElapsedTicks;
            capture = true;
            yield return new WaitForSecondsRealtime(61f);
            capture = false;
            FlushSamples();
            Debug.Log($"PERF_QA RAIL_RESULT entered={railEntered} grindingFrames={grindingFrames} landings={rider.LandingCount} speed={rider.SpeedKmh:F1}");
            Debug.Log("PERF_QA COMPLETE " + output);
            Application.Quit();
            yield break;
        }
        if (!cadenceOnly) for (int m = 0; m < modes.Length; m++)
        {
            if (onlyMode != null && modes[m] != onlyMode) continue;
            for (int repeat = 1; repeat <= 3; repeat++)
            {
                yield return LoadMode(modes[m], repeat);
                yield return new WaitForSecondsRealtime(3f);
                label = modes[m] + repeat;
                sampleCount = 0;
                lastTicks = watch.ElapsedTicks;
                capture = true;
                yield return new WaitForSecondsRealtime(quick ? 12f : 61f);
                capture = false;
                FlushSamples();
                Debug.Log($"PERF_QA RUN {label} frames={sampleCount} phase={run.Phase} speed={rider.SpeedKmh:F1} landing={rider.LandingCount} reset={rider.ResetCount} memory={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()}");
            }
        }
        if (onlyMode == null && !cadenceOnly) yield return StressCycles();
        if (onlyMode != null)
        {
            Debug.Log("PERF_QA COMPLETE " + output);
            writer.Flush();
            Application.Quit();
            yield break;
        }
        foreach (int fps in new[] { 30, 60, 120 })
        {
            Application.targetFrameRate = fps;
            yield return LoadMode("Summit", 1);
            yield return new WaitForSecondsRealtime(2f);
            label = "Cadence" + fps;
            sampleCount = 0;
            lastTicks = watch.ElapsedTicks;
            capture = true;
            yield return new WaitForSecondsRealtime(cadenceOnly ? 35f : quick ? 12f : 35f);
            capture = false;
            FlushSamples();
            Debug.Log($"PERF_QA CADENCE {fps} frames={sampleCount} pos={body.position} velocity={body.linearVelocity} landing={rider.LandingCount} reset={rider.ResetCount}");
        }
        if (!cadenceOnly) yield return StressReplay();
        Debug.Log("PERF_QA COMPLETE " + output);
        writer.Flush();
        Application.Quit();
    }

    private IEnumerator LoadMode(string mode, int repeat)
    {
        string scene = mode == "FreeRoam" ? FreeRoamMap.ScenePath : FreeRoamMap.MainMenuPath;
        yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        yield return null;
        run = FindFirstObjectByType<MountainRun>();
        rider = run.GetComponent<LabRider>();
        body = run.GetComponent<Rigidbody>();
        if (mode == "Pine") { run.SelectLine(MountainLine.Pine); run.StartRun(); }
        else if (mode == "Summit") { run.SelectLine(MountainLine.Summit); run.StartRun(); }
        else run.MoveToDropIn((repeat - 1) % run.DropInCount);
        driveStart = Time.realtimeSinceStartupAsDouble;
        lastJump = -1;
        Debug.Log($"PERF_QA MODE {mode}{repeat} pos={body.position} screen={Screen.width}x{Screen.height}");
    }

    private IEnumerator StressCycles()
    {
        for (int i = 1; i <= 10; i++)
        {
            yield return SceneManager.LoadSceneAsync(FreeRoamMap.MainMenuPath, LoadSceneMode.Single);
            yield return null;
            run = FindFirstObjectByType<MountainRun>();
            rider = run.GetComponent<LabRider>();
            body = run.GetComponent<Rigidbody>();
            string local = Path.Combine(Path.GetDirectoryName(output), "rider-look-qa.json");
            run.UseCustomization(new RiderCustomizationStore(local));
            for (int part = 0; part < 6; part++) run.ChooseLookPart(part, 1);
            run.StartRun();
            yield return new WaitForSecondsRealtime(0.5f);
            run.RestartRun();
            yield return new WaitForSecondsRealtime(0.5f);
            run.ShowStart();
            yield return LoadMode("FreeRoam", 1);
            yield return new WaitForSecondsRealtime(0.5f);
            run.ShowStart();
            yield return new WaitForSecondsRealtime(0.5f);
            writer.WriteLine($"Stress{i},0,0,0,0,0,0,0,0,0,0,0,0,{GC.CollectionCount(0)},{UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()},{UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong()},{UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong()},0,0,0,0,0,Ready,{Screen.width},{Screen.height}");
            writer.Flush();
            Debug.Log($"PERF_QA STRESS {i} memory={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()} gc0={GC.CollectionCount(0)}");
        }
    }

    private IEnumerator StressReplay()
    {
        Application.targetFrameRate = 60;
        straight = true;
        yield return LoadMode("Pine", 1);
        double deadline = Time.realtimeSinceStartupAsDouble + 130.0;
        while (run.Phase == MountainPhase.Riding && Time.realtimeSinceStartupAsDouble < deadline)
            yield return null;
        Debug.Log($"PERF_QA REPLAY_SOURCE phase={run.Phase} elapsed={run.Elapsed:F1} frames={run.Replay.Clip.Count}");
        if (run.Phase == MountainPhase.Results)
        {
            for (int i = 1; i <= 10; i++)
            {
                run.StartReplay();
                yield return new WaitForSecondsRealtime(0.5f);
                run.EndReplay();
                yield return null;
                Debug.Log($"PERF_QA REPLAY {i} memory={UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()} gc0={GC.CollectionCount(0)}");
            }
        }
        straight = false;
    }

    private void OnDestroy()
    {
        if (pad != null && pad.added) InputSystem.RemoveDevice(pad);
        gcBytes.Dispose(); mainThread.Dispose(); renderThread.Dispose();
        physics.Dispose(); scripts.Dispose(); render.Dispose();
        writer?.Dispose();
    }
}
