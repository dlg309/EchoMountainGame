#if UNITY_EDITOR
using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class MountainTrickTests
    {
        private Scene loaded;
        private static readonly int[] Spins = { 180, 360 };

        [UnityTearDown]
        public IEnumerator UnloadCourse()
        {
            if (loaded.IsValid() && loaded.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loaded);
            loaded = default;
        }

        [Test]
        public void ScoreUsesMeasuredRotationQualityAndVariety()
        {
            var score = new TrickScore();
            Rotate(score, 360, BoardGrab.Indy);
            Assert.That(score.PendingPoints, Is.EqualTo(440));
            Assert.That(score.Points, Is.Zero);
            score.Land(350f, LandingOutcome.Clean);
            Assert.That(score.Points, Is.EqualTo(440));

            score.Land(350f, LandingOutcome.Clean);
            Assert.That(score.Points, Is.EqualTo(440), "Do not score the same landing twice.");
            Rotate(score, 360, BoardGrab.Indy);
            score.Land(350f, LandingOutcome.Clean);
            Assert.That(score.LastAward, Is.EqualTo(264));

            Rotate(score, -180, BoardGrab.Melon);
            score.Land(170f, LandingOutcome.Clean);
            Assert.That(score.LastAward, Is.EqualTo(338));
            Assert.That(score.Chain, Is.EqualTo(2));
            score.Tick(8f);
            Assert.That(score.Chain, Is.Zero);

            Rotate(score, 180, BoardGrab.None);
            score.Land(170f, LandingOutcome.Recover);
            Assert.That(score.LastAward, Is.EqualTo(90));
            Rotate(score, 360, BoardGrab.Melon);
            score.Land(350f, LandingOutcome.Bail);
            Assert.That(score.LastAward, Is.Zero);
            Assert.That(score.Points, Is.EqualTo(1132));
            score.Reset();
            Assert.That(score.Points, Is.Zero);
            Assert.That(score.PendingPoints, Is.Zero);
        }

        private static void Rotate(TrickScore score, int degrees, BoardGrab grab)
        {
            for (int step = 0; step <= 4; step++)
                score.SampleAir(Mathf.Repeat(350f + degrees * step / 4f, 360f), grab, 0.05f);
        }

        [UnityTest]
        public IEnumerator PracticeBanksAnActualSpinAndGrab([ValueSource(nameof(Spins))] int target)
        {
            const string path = "Assets/EchoMountain/Scenes/GlacierPass.unity";
            float oldCapture = Time.captureDeltaTime;
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldRouting = InputSystem.settings.editorInputBehaviorInPlayMode;
            Time.captureDeltaTime = 1f / 60f;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
                loaded = SceneManager.GetSceneByPath(path);
                yield return null;
                var board = GameObject.Find("Diagnostic board");
                var run = board.GetComponent<MountainRun>();
                var rider = board.GetComponent<LabRider>();
                var body = board.GetComponent<Rigidbody>();

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.P));
                for (int frame = 0; frame < 85; frame++) yield return null;
                Assert.That(run.IsPractice, Is.True);
                Assert.That(run.Line, Is.EqualTo(MountainLine.Summit));
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
                Assert.That(rider.IsGrounded, Is.True);

                var grab = target == 180 ? Key.Q : Key.E;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space, Key.C, grab));
                bool released = false;
                float lastRotation = 0f;
                int airborneFrames = 0;
                for (int frame = 0; frame < 100 && rider.LandingCount == 0; frame++)
                {
                    yield return null;
                    if (!run.Tricks.IsAirborne) continue;
                    airborneFrames++;
                    lastRotation = run.Tricks.Rotation;
                    if (!released && lastRotation >= target - 35f)
                    {
                        released = true;
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState(grab));
                    }
                }

                Debug.Log($"Glacier trick {target}: rotation={lastRotation:F1}, airtime={airborneFrames}, landing={rider.LastLanding}, score={run.Tricks.Points}, feedback={run.Tricks.Feedback}");
                Assert.That(released, Is.True, "The physical board must reach the spin target.");
                Assert.That(rider.LastLanding, Is.EqualTo(LandingOutcome.Clean));
                Assert.That(run.Tricks.Points, Is.EqualTo(target + (target == 180 ? 80 : 90)));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                body.position = MountainCourse.Point(MountainCourse.LaneX(MountainLine.Summit), MountainRun.PracticeFinish + 0.2f, 0.15f);
                yield return new WaitForFixedUpdate();
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Results));
                Assert.That(run.Tricks.Points, Is.GreaterThan(0));
                run.StartRun();
                Assert.That(run.IsPractice, Is.True);
                Assert.That(run.Tricks.Points, Is.Zero);
                Assert.That(body.position.z, Is.EqualTo(MountainRun.PracticeStart).Within(0.01f));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldRouting;
                Time.captureDeltaTime = oldCapture;
            }
        }
    }
}
#endif
