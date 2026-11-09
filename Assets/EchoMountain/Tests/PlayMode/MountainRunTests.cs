#if UNITY_EDITOR
using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class MountainRunTests
    {
        private static readonly MountainLine[] Lines = { MountainLine.Pine, MountainLine.Summit };

        private Scene loaded;

        [UnityTearDown]
        public IEnumerator UnloadCourse()
        {
            if (loaded.IsValid() && loaded.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loaded);
            loaded = default;
        }

        [UnityTest]
        public IEnumerator EachLineFinishesAndCanRetry([ValueSource(nameof(Lines))] MountainLine line)
        {
            const string path = "Assets/EchoMountain/Scenes/GlacierPass.unity";
            float oldScale = Time.timeScale;
            float oldCapture = Time.captureDeltaTime;
            var background = InputSystem.settings.backgroundBehavior;
            var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            Gamepad pad = null;
            try
            {
                Time.captureDeltaTime = 1f / 60f;
                Time.timeScale = 6f;
                if (line == MountainLine.Summit)
                {
                    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                    InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                    pad = InputSystem.AddDevice<Gamepad>();
                }
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                loaded = SceneManager.GetSceneByPath(path);
                yield return null;

                var board = GameObject.Find("Diagnostic board");
                Assert.That(board, Is.Not.Null);
                var run = board.GetComponent<MountainRun>();
                var rider = board.GetComponent<LabRider>();
                var body = board.GetComponent<Rigidbody>();
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Ready));
                Assert.That(body.isKinematic, Is.True);

                run.SelectLine(line);
                run.StartRun();
                uint resets = rider.ResetCount;
                int landings = 0;
                float furthest = body.position.z;
                float maximumLaneOffset = 0f;
                float laneX = MountainCourse.LaneX(line);
                float deadline = Time.fixedTime + 105f;
                while (run.Phase == MountainPhase.Riding && Time.fixedTime < deadline && rider.ResetCount == resets)
                {
                    if (pad != null)
                    {
                        float laneError = laneX - body.position.x;
                        float desiredHeading = Mathf.Clamp(laneError * 6f, -16f, 16f);
                        float heading = Mathf.DeltaAngle(body.rotation.eulerAngles.y, desiredHeading);
                        float steer = Mathf.Clamp(heading * 0.065f - body.angularVelocity.y, -0.8f, 0.8f);
                        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right * steer });
                    }
                    yield return null;
                    furthest = Mathf.Max(furthest, body.position.z);
                    landings = Mathf.Max(landings, rider.LandingCount);
                    maximumLaneOffset = Mathf.Max(maximumLaneOffset, Mathf.Abs(body.position.x - laneX));
                }
                Debug.Log($"Glacier Pass {line}: phase={run.Phase}, time={run.Elapsed:F2}s, peak={run.PeakSpeed:F1}km/h, landings={landings}, lane offset={maximumLaneOffset:F2}m, furthest={furthest:F1}m, position={body.position}, rotation={body.rotation.eulerAngles}, recovery={rider.IsRecovering}");
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Results));
                Assert.That(run.Elapsed, Is.InRange(60f, 90f));
                Assert.That(maximumLaneOffset, Is.LessThan(5f), "Stay within the chosen line.");
                if (line == MountainLine.Summit) Assert.That(landings, Is.GreaterThan(0), "Summit must include a natural landing.");
                Assert.That(run.Progress, Is.EqualTo(1f));
                Assert.That(body.isKinematic, Is.True);

                run.StartRun();
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
                Assert.That(body.isKinematic, Is.False);
                Assert.That(run.Elapsed, Is.Zero);
                Assert.That(run.PeakSpeed, Is.Zero);
                Assert.That(body.position.z, Is.EqualTo(MountainCourse.Start).Within(0.01f));
                Assert.That(body.position.x, Is.EqualTo(MountainCourse.LaneX(line)).Within(0.01f));
            }
            finally
            {
                if (pad != null) InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = routing;
                Time.timeScale = oldScale;
                Time.captureDeltaTime = oldCapture;
            }
        }
    }
}
#endif
