#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
    public sealed class RidingModeParityTests
    {
        public enum Mode { FreeRoam, Pine, Summit, Practice }

        private Scene loaded;
        private MountainRun run;
        private LabRider rider;
        private Rigidbody body;
        private static readonly Mode[] Modes = { Mode.FreeRoam, Mode.Pine, Mode.Summit, Mode.Practice };
        private static readonly float[] Speeds = { 6f, 16f };

        [UnityTearDown]
        public IEnumerator UnloadMode()
        {
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            loaded = default;
        }

        private struct Sample
        {
            public Vector3 position, velocity, angularVelocity;
            public Quaternion rotation;
            public bool grounded;
            public LandingOutcome? landing;
            public int landings;
        }

        [UnityTest]
        public IEnumerator MatchedSlopeAndInputsProduceTheSameRide([ValueSource(nameof(Speeds))] float speed)
        {
            float capture = Time.captureDeltaTime;
            var background = InputSystem.settings.backgroundBehavior;
            var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            Time.captureDeltaTime = Time.fixedDeltaTime;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad = InputSystem.AddDevice<Gamepad>();
            try
            {
                var reference = new List<Sample>();
                foreach (Mode mode in Modes)
                {
                    yield return OpenMode(mode);
                    var samples = new List<Sample>();
                    yield return SampleRide(pad, samples, speed);
                    if (mode == Mode.FreeRoam) reference = samples;
                    else AssertSameRide(mode, reference, samples);
                    yield return UnloadMode();
                }
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = routing;
                Time.captureDeltaTime = capture;
            }
        }

        private IEnumerator SampleRide(Gamepad pad, List<Sample> samples, float speed)
        {
            // Use the same collider above both authored courses, inside both sets of course limits.
            Vector3 origin = new(0f, 300f, 185f);
            Quaternion slope = Quaternion.Euler(9f, 0f, 0f);
            Vector3 normal = slope * Vector3.up;
            Vector3 downhill = slope * Vector3.forward;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(floor, loaded);
            floor.transform.SetPositionAndRotation(origin - normal * 0.5f, slope);
            floor.transform.localScale = new Vector3(46f, 1f, 150f);
            var stance = speed > 10f ? slope * Quaternion.Euler(0f, 180f, 0f) : slope;
            rider.SetSpawn(origin + normal * 0.14f, stance);
            InputSystem.QueueStateEvent(pad, new GamepadState());
            for (int i = 0; i < 25; i++) yield return null;
            Assert.That(rider.IsGrounded);
            Assert.That(rider.ActiveSurface, Is.EqualTo(SnowSurface.Packed));
            body.linearVelocity = downhill * speed;
            body.angularVelocity = Vector3.zero;
            uint resets = rider.ResetCount;

            yield return RideFrames(pad, default, 18, samples);
            if (speed < 10f)
                Assert.That(body.linearVelocity.magnitude, Is.GreaterThan(speed), "Gravity should accelerate the slower board.");
            else
                Assert.That(body.linearVelocity.magnitude, Is.EqualTo(speed).Within(0.2f), "The fast board coasts near terminal speed.");
            for (int leg = 0; leg < 4; leg++)
                yield return RideFrames(pad, new GamepadState { leftStick = (leg % 2 == 0 ? 1f : -1f) * Vector2.right }, 24, samples);

            Vector3 brakeStart = body.position;
            float entrySpeed = body.linearVelocity.magnitude;
            yield return RideFrames(pad, new GamepadState { leftTrigger = 1f }, 36, samples);
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(entrySpeed - 1f));
            float brakeDistance = Vector3.Distance(brakeStart, body.position);

            Vector3 popStart = body.position;
            int airborneStart = samples.Count;
            yield return RideFrames(pad, new GamepadState().WithButton(GamepadButton.South), 7, samples);
            Assert.That(rider.IsGrounded, Is.False);
            Quaternion beforeSpin = body.rotation;
            yield return RideFrames(pad, new GamepadState { rightStick = Vector2.right * 0.6f }, 7, samples);
            Assert.That(Quaternion.Angle(beforeSpin, body.rotation), Is.GreaterThan(5f));
            yield return RideFrames(pad, default, 82, samples);
            Assert.That(rider.LandingCount, Is.EqualTo(1));
            Assert.That(rider.LastLanding, Is.Not.EqualTo(LandingOutcome.Bail));
            Assert.That(rider.ResetCount, Is.EqualTo(resets));
            float apex = 0f;
            for (int i = airborneStart; i < samples.Count; i++)
                apex = Mathf.Max(apex, Vector3.Dot(samples[i].position - popStart, normal));
            Assert.That(apex, Is.GreaterThan(0.55f));
            Debug.Log($"{run.gameObject.scene.name}/{run.Line}{(run.IsPractice ? " practice" : "")} {speed:F0} m/s: brake={brakeDistance:F3} m, pop={apex:F3} m, landing={rider.LastLanding}, samples={samples.Count}");
        }

        private IEnumerator RideFrames(Gamepad pad, GamepadState input, int count, List<Sample> samples)
        {
            InputSystem.QueueStateEvent(pad, input);
            for (int i = 0; i < count; i++)
            {
                yield return null;
                samples.Add(new Sample
                {
                    position = body.position, velocity = body.linearVelocity, angularVelocity = body.angularVelocity,
                    rotation = body.rotation, grounded = rider.IsGrounded, landings = rider.LandingCount,
                    landing = rider.LastLanding
                });
            }
        }

        private static void AssertSameRide(Mode mode, List<Sample> reference, List<Sample> samples)
        {
            Assert.That(samples.Count, Is.EqualTo(reference.Count));
            float position = 0f, velocity = 0f, angular = 0f, rotation = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                var a = reference[i];
                var b = samples[i];
                position = Mathf.Max(position, Vector3.Distance(a.position, b.position));
                velocity = Mathf.Max(velocity, Vector3.Distance(a.velocity, b.velocity));
                angular = Mathf.Max(angular, Vector3.Distance(a.angularVelocity, b.angularVelocity));
                rotation = Mathf.Max(rotation, Quaternion.Angle(a.rotation, b.rotation));
                Assert.That(b.grounded, Is.EqualTo(a.grounded), $"{mode}, frame {i}");
                Assert.That(b.landings, Is.EqualTo(a.landings), $"{mode}, frame {i}");
                Assert.That(b.landing, Is.EqualTo(a.landing), $"{mode}, frame {i}");
            }
            Debug.Log($"{mode} versus Free Roam max deltas: {position:F5} m, {velocity:F5} m/s, {angular:F5} rad/s, {rotation:F5}°");
            Assert.That(position, Is.LessThan(0.003f));
            // The collision solver can report slightly different instantaneous contact impulses.
            Assert.That(velocity, Is.LessThan(0.08f));
            Assert.That(angular, Is.LessThan(0.10f));
            Assert.That(rotation, Is.LessThan(0.12f));
        }

        [UnityTest]
        public IEnumerator RiderRecoversFromInvertedAndBuriedBoard([ValueSource(nameof(Modes))] Mode mode)
        {
            yield return OpenMode(mode);
            var map = FindInScene<FreeRoamMap>();
            Vector3 spawn = map ? map.DropIn(0).position :
                MountainCourse.Point(MountainCourse.LaneX(run.Line), run.StartZ, 0.15f);

            // Compare the pose at the same clearance above each mode's actual collision surface.
            var ground = map ? (Collider)map.Terrain.GetComponent<TerrainCollider>() : FindInScene<MeshCollider>();
            Assert.That(ground.Raycast(new Ray(spawn + Vector3.up * 10f, Vector3.down), out var snow, 20f));
            foreach (bool inverted in new[] { true, false })
            {
                uint resets = rider.ResetCount;
                body.position = snow.point + Vector3.up * (inverted ? 0.17f : -0.70f);
                body.rotation = inverted ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                for (int i = 0; i < 3 && rider.ResetCount == resets; i++) yield return new WaitForFixedUpdate();
                Assert.That(rider.ResetCount, Is.EqualTo(resets + 1), $"{mode}, inverted={inverted}");
                Assert.That(Vector3.Distance(body.position, spawn), Is.LessThan(0.3f));
                Assert.That(Vector3.Dot(body.rotation * Vector3.up, Vector3.up), Is.GreaterThan(0.8f));
                Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
            }
        }

        private IEnumerator OpenMode(Mode mode)
        {
            string path = mode == Mode.FreeRoam ? FreeRoamMap.ScenePath : FreeRoamMap.MainMenuPath;
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
            loaded = SceneManager.GetSceneByPath(path);
            yield return null;
            run = FindInScene<MountainRun>();
            rider = run.GetComponent<LabRider>();
            body = run.GetComponent<Rigidbody>();
            if (mode == Mode.Practice) run.StartPractice();
            else if (mode != Mode.FreeRoam)
            {
                run.SelectLine(mode == Mode.Pine ? MountainLine.Pine : MountainLine.Summit);
                run.StartRun();
            }
            Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
        }

        private T FindInScene<T>() where T : Component
        {
            foreach (var root in loaded.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>();
                if (component) return component;
            }
            return null;
        }
    }
}
#endif
