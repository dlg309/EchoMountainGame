using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoMountain.Tests
{
    public sealed class LabRiderTests
    {
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
                    "The board should have meaningful downhill speed after three seconds.");
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

    }
}
