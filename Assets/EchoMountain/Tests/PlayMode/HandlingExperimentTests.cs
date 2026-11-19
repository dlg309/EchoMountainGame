using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class HandlingExperimentTests
    {
        private static void CreateFixture(out GameObject ground, out GameObject board,
            out Rigidbody body, out LabRider rider)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(240f, 1f, 240f);
            board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = new Vector3(0f, 0.2f, 0f);
            board.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            body = board.AddComponent<Rigidbody>();
            body.mass = 75f;
            rider = board.AddComponent<LabRider>();
        }

        private static void Keys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }

        [UnityTest]
        public IEnumerator PackedSnowSTurnMetrics()
        {
            float oldFixed = Time.fixedDeltaTime;
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            Time.fixedDeltaTime = 1f / 60f;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreateFixture(out var ground, out var board, out var body, out var rider);
            try
            {
                foreach (float speed in new[] { 6f, 16f })
                {
                    Keys(keyboard);
                    rider.ResetRider();
                    yield return null;
                    for (int step = 0; step < 45; step++) yield return new WaitForFixedUpdate();
                    Assert.That(rider.IsGrounded, Is.True);
                    body.linearVelocity = Vector3.forward * speed;
                    float peakYaw = 0f, firstHeading = 0f;
                    int supported = 0;
                    for (int leg = 0; leg < 4; leg++)
                    {
                        Keys(keyboard, leg % 2 == 0 ? Key.D : Key.A);
                        yield return null;
                        for (int step = 0; step < 36; step++)
                        {
                            yield return new WaitForFixedUpdate();
                            if (rider.IsGrounded) supported++;
                            peakYaw = Mathf.Max(peakYaw, Mathf.Abs(body.angularVelocity.y));
                        }
                        if (leg == 0)
                            firstHeading = Vector3.SignedAngle(Vector3.forward,
                                Vector3.ProjectOnPlane(board.transform.forward, Vector3.up), Vector3.up);
                    }
                    Keys(keyboard);
                    yield return null;
                    for (int step = 0; step < 30; step++) yield return new WaitForFixedUpdate();
                    float settledYaw = Mathf.Abs(body.angularVelocity.y);
                    Debug.Log($"HANDLING S speed={speed:F0} firstHeading={firstHeading:F2} peakYaw={peakYaw:F3} supported={supported}/144 settledYaw={settledYaw:F3}");
                    Assert.That(supported, Is.GreaterThanOrEqualTo(137));
                    Assert.That(firstHeading, Is.GreaterThan(5f));
                    if (speed > 8f)
                        Assert.That(peakYaw, Is.LessThan(0.576f), "High-speed yaw should be at least 15% below the measured R2 0.678 rad/s.");
                    Assert.That(settledYaw, Is.LessThan(0.6f));
                    Assert.That(rider.LandingCount, Is.Zero);
                    Assert.That(rider.IsRecovering, Is.False);
                }
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
                Time.fixedDeltaTime = oldFixed;
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }

        [UnityTest]
        public IEnumerator PackedSnowBrakeMetrics()
        {
            float oldFixed = Time.fixedDeltaTime;
            var oldBackground = InputSystem.settings.backgroundBehavior;
            var oldEditor = InputSystem.settings.editorInputBehaviorInPlayMode;
            Time.fixedDeltaTime = 1f / 60f;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            CreateFixture(out var ground, out var board, out var body, out var rider);
            try
            {
                float[] distance = new float[3];
                for (int mode = 0; mode < 3; mode++)
                {
                    Keys(keyboard);
                    rider.ResetRider();
                    yield return null;
                    for (int step = 0; step < 45; step++) yield return new WaitForFixedUpdate();
                    Assert.That(rider.IsGrounded, Is.True);
                    body.linearVelocity = Vector3.forward * 12f;
                    int supported = 0;
                    for (int leg = 0; leg < 4; leg++)
                    {
                        if (mode == 0) Keys(keyboard);
                        else if (mode == 1) Keys(keyboard, Key.S);
                        else Keys(keyboard, Key.S, leg % 2 == 0 ? Key.D : Key.A);
                        yield return null;
                        for (int step = 0; step < 30; step++)
                        {
                            Vector3 before = body.position;
                            yield return new WaitForFixedUpdate();
                            distance[mode] += Vector3.ProjectOnPlane(body.position - before, Vector3.up).magnitude;
                            if (rider.IsGrounded) supported++;
                        }
                    }
                    Debug.Log($"HANDLING brake mode={mode} distance={distance[mode]:F2} supported={supported}/120 finalSpeed={body.linearVelocity.magnitude:F2}");
                    Assert.That(supported, Is.GreaterThanOrEqualTo(114));
                    Assert.That(rider.IsRecovering, Is.False);
                    Assert.That(body.linearVelocity.z, Is.GreaterThan(-0.5f));
                }
                Assert.That(distance[1], Is.LessThan(distance[0] * 0.70f));
                Assert.That(distance[2], Is.LessThan(distance[0] * 0.75f));
                Assert.That(distance[2], Is.LessThanOrEqualTo(13.43f),
                    "Braking through S-turns must not exceed the measured R2 13.33 m by more than 0.10 m.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditor;
                Time.fixedDeltaTime = oldFixed;
                Object.Destroy(board);
                Object.Destroy(ground);
            }
        }
    }
}
