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
#endif

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

    }
}
