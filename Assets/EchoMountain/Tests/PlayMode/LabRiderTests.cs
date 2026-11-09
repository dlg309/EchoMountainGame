using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace EchoMountain.Tests
{
    public sealed class LabRiderTests
    {
        private float originalFixedDelta;

        [UnitySetUp]
        public IEnumerator SetSixtyHertz()
        {
            originalFixedDelta = Time.fixedDeltaTime;
            Time.fixedDeltaTime = 1f / 60f;
            yield return null;
        }
#if UNITY_EDITOR
        private Scene loadedLabScene;
#endif
        [UnityTearDown]
        public IEnumerator RestoreSimulation()
        {
#if UNITY_EDITOR
            if (loadedLabScene.IsValid() && loadedLabScene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(loadedLabScene);
            loadedLabScene = default;
#endif
            Time.fixedDeltaTime = originalFixedDelta;
            yield break;
        }
        [UnityTest]
        public IEnumerator FlatGroundSupportsBoardAndResetClearsMotion()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 180; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.y, Is.InRange(0.04f, 0.25f), "The board should settle above the flat surface.");
                Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.5f), "Support should not oscillate indefinitely.");
                body.linearVelocity = new Vector3(4f, 3f, 2f);
                body.angularVelocity = new Vector3(1f, 2f, 3f);
                rider.ResetRider();
                Assert.That(Vector3.Distance(body.position, new Vector3(0f, 2f, 0f)), Is.LessThan(0.001f));
                Assert.That(body.linearVelocity.sqrMagnitude, Is.LessThan(0.0001f));
                Assert.That(body.angularVelocity.sqrMagnitude, Is.LessThan(0.0001f));
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }
        [UnityTest]
        public IEnumerator GravityAcceleratesBoardDownGentleSlope()
        {
            var slope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slope.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            slope.transform.localScale = new Vector3(20f, 1f, 40f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = slope.transform.rotation * new Vector3(0f, 1f, -5f);
            board.transform.rotation = slope.transform.rotation;
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            board.AddComponent<LabRider>();
            try
            {
                var start = body.position.z;
                for (int step = 0; step < 150; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.z - start, Is.GreaterThan(2f),
                    "A board released on a 15-degree slope should move downhill under gravity.");
                Assert.That(Vector3.Dot(body.linearVelocity, slope.transform.forward), Is.GreaterThan(1f),
                    "The board should have meaningful downhill speed after the fixed simulation interval.");
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(slope);
            }
        }

        [UnityTest]
        public IEnumerator GroundedBoardCanPopAndLeaveSurface()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.4f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 60; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True, "Ground probes must detect support beneath the board.");
                float before = body.position.y;
                rider.RequestJump();
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.y, Is.GreaterThan(before + 0.25f), "Pop must produce upward travel.");
                Assert.That(rider.IsGrounded, Is.False, "The board must not remain grounded after takeoff.");
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator KeyboardBrakesAndTriggersPop()
        {
            var originalBackground = InputSystem.settings.backgroundBehavior;
            var originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                body.linearVelocity = Vector3.forward * 8f;
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                float unbraked = Vector3.Dot(body.linearVelocity, Vector3.forward);
                rider.ResetRider();
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                InputSystem.Update();
                Assert.That(keyboard.sKey.isPressed, Is.True, "Virtual S key must be pressed.");
                Assert.That(Keyboard.current, Is.SameAs(keyboard), "Virtual keyboard must be current.");
                yield return null;
                body.linearVelocity = Vector3.forward * 8f;
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                float braked = Vector3.Dot(body.linearVelocity, Vector3.forward);
                Assert.That(rider.IsGrounded, Is.True, "Board should remain grounded while braking.");
                Assert.That(keyboard.sKey.isPressed, Is.True, "Virtual S should stay pressed during braking.");
                Assert.That(rider.BrakeInput, Is.GreaterThan(0.5f), "Rider should sample S.");
                Assert.That(braked, Is.LessThan(unbraked - 1f), "S should slow the moving board.");
                rider.ResetRider();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                float before = body.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null;
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.y, Is.GreaterThan(before + 0.15f), "Space should trigger pop.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = originalBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator KeyboardSteersAMovingBoard()
        {
            var originalBackground = InputSystem.settings.backgroundBehavior;
            var originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                body.linearVelocity = Vector3.forward * 8f;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return null;
                for (int step = 0; step < 35; step++) yield return new WaitForFixedUpdate();
                float yaw = Vector3.SignedAngle(Vector3.forward, Vector3.ProjectOnPlane(board.transform.forward, Vector3.up), Vector3.up);
                Assert.That(yaw, Is.GreaterThan(5f), "D should turn a moving board to the right.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = originalBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator GeneratedLabMovesDownhillAndRespondsToSpace()
        {
            var originalBackground = InputSystem.settings.backgroundBehavior;
            var originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/EchoMountain/Scenes/PhysicsLabHUD.unity", new LoadSceneParameters(LoadSceneMode.Additive));
            loadedLabScene = SceneManager.GetSceneByPath("Assets/EchoMountain/Scenes/PhysicsLabHUD.unity");
            try
            {
                var board = GameObject.Find("Diagnostic board");
                Assert.That(board, Is.Not.Null, "Generated lab must contain the board.");
                Assert.That(GameObject.Find("Visual markers"), Is.Not.Null, "The slope needs visible reference markers.");
                var color = board.GetComponent<Renderer>().sharedMaterial.color;
                Assert.That(color.r, Is.GreaterThan(color.b + 0.25f), "The board should contrast with snow.");
                var rider = board.GetComponent<LabRider>();
                var body = board.GetComponent<Rigidbody>();
                float start = body.position.z;
                for (int step = 0; step < 100; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.z - start, Is.GreaterThan(1f), "The actual lab board must move downhill.");
                Assert.That(rider.IsGrounded, Is.True, "The board should be supported before pop.");
                float before = body.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                yield return null;
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.y, Is.GreaterThan(before + 0.05f), "Space should lift the board on the lab slope.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = originalBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
            }
        }

        [UnityTest]
        public IEnumerator RiderRigKeepsBootsOnBoardDuringPop()
        {
            const string scenePath = "Assets/EchoMountain/Scenes/PhysicsLabRider.unity";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            loadedLabScene = SceneManager.GetSceneByPath(scenePath);
            var board = GameObject.Find("Diagnostic board");
            Assert.That(board, Is.Not.Null);
            var rider = board.GetComponent<LabRider>();
            var rig = board.transform.parent.Find("Segmented rider");
            Assert.That(rig, Is.Not.Null);
            var boot = rig.Find("Front boot");
            var jacket = rig.Find("Jacket");
            Assert.That(boot, Is.Not.Null);
            Assert.That(jacket, Is.Not.Null);

            var fixedTick = new WaitForFixedUpdate();
            for (int step = 0; step < 60; step++) yield return fixedTick;
            yield return null;
            Assert.That(rider.IsGrounded, Is.True);
            Assert.That(rig.GetComponentsInChildren<Collider>(), Is.Empty, "The pose rig must not affect physics.");
            var bootOffset = boot.localPosition;
            float standingHeight = jacket.localPosition.y;

            rider.RequestJump();
            for (int step = 0; step < 24; step++) yield return fixedTick;
            yield return null;
            Assert.That(rider.IsGrounded, Is.False);
            Assert.That(jacket.localPosition.y, Is.LessThan(standingHeight - 0.07f), "The rider should tuck in flight.");
            Assert.That(boot.localPosition, Is.EqualTo(bootOffset));
            Assert.That(Vector3.Distance(rig.position, board.transform.position), Is.LessThan(0.35f),
                "The pose rig should follow the interpolated board pose.");
            Assert.That(Vector3.Distance(boot.position, rig.TransformPoint(bootOffset)), Is.LessThan(0.01f));
        }

        private static readonly int[] RenderCadences = { 30, 60, 120 };

        [UnityTest]
        public IEnumerator CenterLineClearsTakeoffAndReachesRunout([ValueSource(nameof(RenderCadences))] int renderHz)
        {
            float previousFrameTime = Time.captureDeltaTime;
            Time.captureFramerate = renderHz;
            try
            {
                const string scenePath = "Assets/EchoMountain/Scenes/PhysicsLabHUD.unity";
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Additive));
                loadedLabScene = SceneManager.GetSceneByPath(scenePath);
                yield return null;
                Assert.That(Time.deltaTime, Is.EqualTo(1f / renderHz).Within(0.0001f));

                var board = GameObject.Find("Diagnostic board");
                Assert.That(board, Is.Not.Null);
                var rider = board.GetComponent<LabRider>();
                var body = board.GetComponent<Rigidbody>();
                float furthest = body.position.z;
                float flightStarted = -1f;
                float flightEnded = -1f;
                float landingZ = -1f;
                float approachSpeed = 0f;
                var fixedTick = new WaitForFixedUpdate();
                float deadline = Time.fixedTime + 12f;
                while (Time.fixedTime < deadline && body.position.z < 69f && !rider.IsRecovering)
                {
                    yield return fixedTick;
                    float z = body.position.z;
                    furthest = Mathf.Max(furthest, z);
                    if (z >= 42f && z < 45f)
                        approachSpeed = Mathf.Max(approachSpeed, body.linearVelocity.z);
                    if (flightStarted < 0f && z >= 43f && z < 46f && !rider.IsGrounded)
                        flightStarted = Time.fixedTime;
                    if (flightStarted >= 0f && flightEnded < 0f && rider.IsGrounded)
                    {
                        flightEnded = Time.fixedTime;
                        landingZ = z;
                    }
                }

                float flightDuration = flightEnded - flightStarted;
                Debug.Log($"Lab descent {renderHz} Hz: approach={approachSpeed:F2} m/s, air={flightDuration:F3} s, landing z={landingZ:F2}, furthest z={furthest:F2}, end={body.position}, landings={rider.LandingCount}, outcome={rider.LastLanding}.");
                Assert.That(approachSpeed, Is.GreaterThan(8f), "The center line should carry speed onto the ramp.");
                Assert.That(flightStarted, Is.GreaterThanOrEqualTo(0f), "The board should become airborne at the lip without requiring pop.");
                Assert.That(flightDuration, Is.EqualTo(0.92f).Within(0.1f), "The drop should produce a sustained flight.");
                Assert.That(landingZ, Is.EqualTo(54.8f).Within(0.6f), "The board should regain support within the landing slope.");
                Assert.That(rider.LandingCount, Is.EqualTo(1), "The descent should register a single landing.");
                Assert.That(rider.IsRecovering, Is.False, "The center line should be rideable through landing.");
                Assert.That(furthest, Is.GreaterThanOrEqualTo(69f), "The rider must reach the runout.");
                Assert.That(rider.IsGrounded, Is.True, "The board must be supported in the runout.");
            }
            finally
            {
                Time.captureDeltaTime = previousFrameTime;
            }
        }
#endif

        [UnityTest]
        public IEnumerator GamepadSteersBrakesPausesPopsAndResets()
        {
            var originalBackground = InputSystem.settings.backgroundBehavior;
            var originalEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            float originalTimeScale = Time.timeScale;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var pad = InputSystem.AddDevice<Gamepad>();
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(50f, 1f, 50f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            var rider = board.AddComponent<LabRider>();
            var fixedTick = new WaitForFixedUpdate();

            try
            {
                for (int step = 0; step < 40; step++) yield return fixedTick;
                Assert.That(rider.IsGrounded, Is.True);
                body.linearVelocity = Vector3.forward * 5f;

                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right, leftTrigger = 0.65f });
                yield return null;
                yield return null;
                Assert.That(rider.SteerInput, Is.GreaterThan(0.8f));
                Assert.That(rider.BrakeInput, Is.EqualTo(0.65f).Within(0.01f));

                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                yield return null;
                Assert.That(Time.timeScale, Is.Zero);
                Vector3 pausedPosition = body.position;

                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                for (int frame = 0; frame < 5; frame++) yield return null;
                Assert.That(body.position, Is.EqualTo(pausedPosition), "Pause must freeze physics.");
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(originalTimeScale));
                for (int step = 0; step < 6; step++) yield return fixedTick;
                Assert.That(rider.IsGrounded, Is.True, "Pop pressed while paused must not be queued.");

                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.South));
                for (int step = 0; step < 12; step++) yield return fixedTick;
                Assert.That(rider.IsGrounded, Is.False, "South should pop after resuming.");
                Assert.That(body.position.y, Is.GreaterThan(pausedPosition.y + 0.2f));

                uint resets = rider.ResetCount;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
                yield return null;
                yield return null;
                Assert.That(rider.ResetCount, Is.EqualTo(resets + 1));
                Assert.That(body.position.z, Is.EqualTo(0f).Within(0.01f));
            }
            finally
            {
                InputSystem.RemoveDevice(pad);
                InputSystem.settings.backgroundBehavior = originalBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = originalEditorInput;
                Time.timeScale = originalTimeScale;
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator CameraLooksAlongSwitchTravelAndSnapsOnReset()
        {
            float previousFrameTime = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var body = board.AddComponent<Rigidbody>();
            body.useGravity = false;
            var rider = board.AddComponent<LabRider>();
            var view = new GameObject("Lab camera");
            view.AddComponent<LabCamera>().SetTarget(board.transform);
            try
            {
                body.linearVelocity = Vector3.forward * 10f;
                for (int frame = 0; frame < 90; frame++) yield return null;
                Assert.That(body.position.z, Is.GreaterThan(10f));
                Assert.That(Vector3.Dot(view.transform.forward, Vector3.forward), Is.GreaterThan(0.8f),
                    "The view should face the direction of travel when the board rides switch.");
                Assert.That(Mathf.Abs(Vector3.Dot(view.transform.right, Vector3.up)), Is.LessThan(0.001f),
                    "The horizon should remain level.");

                rider.ResetRider();
                yield return null;
                Assert.That(Vector3.Distance(view.transform.position, new Vector3(0f, 3f, 6.5f)), Is.LessThan(0.02f),
                    "The camera should return directly behind the spawn heading.");
            }
            finally
            {
                Time.captureDeltaTime = previousFrameTime;
                Object.Destroy(view);
                Object.Destroy(board);
            }
        }

        [Test]
        public void LandingRulesAcceptSwitchAndDistinguishSevereImpacts()
        {
            Assert.That(LabLandingRules.Evaluate(0.98f, 0.95f, 8f, 2f, 0.5f, 3), Is.EqualTo(LandingOutcome.Clean));
            Assert.That(LabLandingRules.Evaluate(0.98f, -0.95f, 8f, 2f, 0.5f, 3), Is.EqualTo(LandingOutcome.Clean));
            Assert.That(LabLandingRules.Evaluate(0.8f, 0.5f, 8f, 6f, 4f, 2), Is.EqualTo(LandingOutcome.Recover));
            Assert.That(LabLandingRules.Evaluate(0.95f, 0.95f, 8f, 10f, 0.5f, 3), Is.EqualTo(LandingOutcome.Bail));
            Assert.That(LabLandingRules.Evaluate(0.95f, 0.95f, 8f, 2f, 0.5f, 0), Is.EqualTo(LandingOutcome.Bail));
            Assert.That(LabLandingRules.Evaluate(float.NaN, 0.95f, 8f, 2f, 0.5f, 3), Is.EqualTo(LandingOutcome.Bail));
        }

        [UnityTest]
        public IEnumerator BoardDoesNotGroundEarlyAndLandingFiresOnce()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.55f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 100; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                Assert.That(rider.LandingCount, Is.Zero, "Initial spawn must not count as a landing.");
                body.position = new Vector3(0f, 0.55f, 0f);
                body.linearVelocity = Vector3.zero;
                body.useGravity = false;
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.False, "A board floating 45 cm above the surface is not supported.");
                body.useGravity = true;
                for (int step = 0; step < 100; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                Assert.That(rider.LandingCount, Is.EqualTo(1), "One flight must produce one landing.");
                Assert.That(rider.LastLanding, Is.EqualTo(LandingOutcome.Clean));
                for (int step = 0; step < 50; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.LandingCount, Is.EqualTo(1), "Resting contact must not repeatedly award landings.");
                rider.ResetRider();
                Assert.That(rider.LandingCount, Is.Zero);
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator SwitchPopCanLandCleanlyOnce()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(50f, 1f, 80f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, -10f);
            board.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 60; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                body.linearVelocity = Vector3.forward * 5f;
                rider.RequestJump();
                bool airborne = false;
                for (int step = 0; step < 150; step++)
                {
                    yield return new WaitForFixedUpdate();
                    airborne |= !rider.IsGrounded;
                }
                Assert.That(airborne, Is.True, "Pop should leave the surface.");
                Assert.That(rider.IsGrounded, Is.True, "Board should return to support.");
                Assert.That(rider.LandingCount, Is.EqualTo(1));
                Assert.That(rider.LastLanding, Is.EqualTo(LandingOutcome.Clean), "Aligned switch travel should be accepted.");
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator SevereSidewaysLandingRecoversToSpawn()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 60; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                rider.RequestJump();
                for (int step = 0; step < 10; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.False);
                body.linearVelocity = new Vector3(12f, body.linearVelocity.y, 0f);
                bool observedBail = false;
                for (int step = 0; step < 65; step++)
                {
                    yield return new WaitForFixedUpdate();
                    observedBail |= rider.IsRecovering && rider.LastLanding == LandingOutcome.Bail;
                }
                Assert.That(observedBail, Is.True, "A severely sideways landing should enter recovery.");
                for (int step = 0; step < 100; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsRecovering, Is.False);
                Assert.That(Vector3.Distance(body.position, new Vector3(0f, 0.1f, 0f)), Is.LessThan(0.3f), "Recovery should return near the safe spawn.");
                Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.5f));
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator FastBoardCrossesFlatSeamWithoutFalseLanding()
        {
            var near = GameObject.CreatePrimitive(PrimitiveType.Cube);
            near.transform.position = new Vector3(0f, -0.5f, -5f);
            near.transform.localScale = new Vector3(20f, 1f, 10f);
            var far = GameObject.CreatePrimitive(PrimitiveType.Cube);
            far.transform.position = new Vector3(0f, -0.5f, 5f);
            far.transform.localScale = new Vector3(20f, 1f, 10f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, -3f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            var body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var rider = board.AddComponent<LabRider>();
            try
            {
                for (int step = 0; step < 60; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded, Is.True);
                body.linearVelocity = Vector3.forward * 12f;
                int longestGap = 0, gap = 0;
                for (int step = 0; step < 45; step++)
                {
                    yield return new WaitForFixedUpdate();
                    gap = rider.IsGrounded ? 0 : gap + 1;
                    longestGap = Mathf.Max(longestGap, gap);
                }
                Assert.That(body.position.z, Is.GreaterThan(3f), "Board should cross the seam at speed.");
                Assert.That(body.position.y, Is.GreaterThan(-0.05f), "The board must not tunnel through the seam.");
                Assert.That(longestGap, Is.LessThanOrEqualTo(3), "Contact should not chatter across a flat seam.");
                Assert.That(rider.LandingCount, Is.Zero, "A flat seam must not register as a landing.");
            }
            finally
            {
                Object.Destroy(board);
                Object.Destroy(near);
                Object.Destroy(far);
            }
        }

    }
}
