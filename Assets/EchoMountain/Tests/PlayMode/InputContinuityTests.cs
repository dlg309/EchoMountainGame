using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class InputContinuityTests
    {
        [UnityTest]
        public IEnumerator ActiveControllerLossPausesUntilExplicitKeyboardResume()
        {
            var background = InputSystem.settings.backgroundBehavior;
            var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            float timeScale = Time.timeScale;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            var pad = InputSystem.AddDevice<Gamepad>();
            var unused = InputSystem.AddDevice<Gamepad>();
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = board.AddComponent<Rigidbody>();
            body.useGravity = false;
            var rider = board.AddComponent<LabRider>();
            try
            {
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = Vector2.right, leftTrigger = 0.6f });
                yield return null;
                yield return null;
                Assert.That(rider.UsingGamepad, Is.True);
                Assert.That(rider.SteerInput, Is.GreaterThan(0.8f));
                InputSystem.RemoveDevice(unused);
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(timeScale), "An unused controller must not interrupt the rider.");
                body.linearVelocity = Vector3.forward * 5f;
                rider.RequestJump();
                InputSystem.RemoveDevice(pad);
                yield return null;
                Assert.That(Time.timeScale, Is.Zero);
                Assert.That(rider.SteerInput, Is.Zero);
                Assert.That(rider.BrakeInput, Is.Zero);
                Assert.That(rider.UsingGamepad, Is.False);
                Vector3 paused = body.position;
                pad = InputSystem.AddDevice<Gamepad>();
                for (int i = 0; i < 5; i++) yield return null;
                Assert.That(Time.timeScale, Is.Zero, "Reconnecting must not resume without a deliberate action.");
                Assert.That(body.position, Is.EqualTo(paused));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                yield return null;
                yield return null;
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                yield return null;
                yield return null;
                Assert.That(rider.UsingGamepad, Is.False);
                Assert.That(rider.SteerInput, Is.LessThan(-0.8f));
            }
            finally
            {
                // Disable listeners before removing test devices and restoring global time.
                board.SetActive(false);
                if (pad.added) InputSystem.RemoveDevice(pad);
                if (unused.added) InputSystem.RemoveDevice(unused);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
                Time.timeScale = timeScale;
                Object.Destroy(board);
            }
        }

        [UnityTest]
        public IEnumerator LosingWindowFocusPausesWithoutAutomaticResume()
        {
            float previous = Time.timeScale;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.AddComponent<Rigidbody>().useGravity = false;
            var rider = board.AddComponent<LabRider>();
            try
            {
                rider.SendMessage("OnApplicationFocus", false);
                yield return null;
                Assert.That(Time.timeScale, Is.Zero);
                rider.SendMessage("OnApplicationFocus", true);
                yield return null;
                Assert.That(Time.timeScale, Is.Zero);
                board.SetActive(false);
                Assert.That(Time.timeScale, Is.EqualTo(previous), "Leaving the riding state restores time.");
            }
            finally
            {
                board.SetActive(false);
                Time.timeScale = previous;
                Object.Destroy(board);
            }
        }
    }
}
