using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace EchoMountain.Tests
{
    public sealed class SurfaceResponseTests
    {
        [Test]
        public void SurfaceBoundsPriorityAndInvalidDataArePredictable()
        {
            var zones = new[]
            {
                new SnowSurfaceZone { kind = SnowSurface.Powder, bounds = new Rect(3f, 330f, 20f, 60f) },
                new SnowSurfaceZone { kind = SnowSurface.Ice, bounds = new Rect(5f, 340f, 5f, 5f), priority = 1 }
            };
            Assert.That(SurfaceRules.Sample(zones, new Vector3(3f, 0f, 330f)).Kind, Is.EqualTo(SnowSurface.Powder));
            Assert.That(SurfaceRules.Sample(zones, new Vector3(3f, 0f, 390f)).Kind, Is.EqualTo(SnowSurface.Packed));
            Assert.That(SurfaceRules.Sample(zones, new Vector3(23f, 0f, 350f)).Kind, Is.EqualTo(SnowSurface.Packed));
            Assert.That(SurfaceRules.Sample(zones, new Vector3(5f, 0f, 340f)).Kind, Is.EqualTo(SnowSurface.Ice));
            zones[1].priority = 0;
            Assert.That(SurfaceRules.Sample(zones, new Vector3(5f, 0f, 340f)).Kind, Is.EqualTo(SnowSurface.Powder));
            zones[0].bounds.width = float.NaN;
            Assert.That(SurfaceRules.Sample(zones, new Vector3(3f, 0f, 330f)).Kind, Is.EqualTo(SnowSurface.Packed));
            Assert.That(SurfaceRules.Sample(zones, new Vector3(float.NaN, 0f, 340f)).Kind, Is.EqualTo(SnowSurface.Packed));
            Assert.That(SurfaceRules.Sample(null, Vector3.zero).Grip, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator SurfacesChangeSupportedMotionButDoNotApplyGripInFlight()
        {
            float oldFixed = Time.fixedDeltaTime;
            var background = InputSystem.settings.backgroundBehavior;
            var routing = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var keyboard = InputSystem.AddDevice<Keyboard>();
            Time.fixedDeltaTime = 1f / 60f;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = Vector3.down * 0.5f;
            ground.transform.localScale = new Vector3(100f, 1f, 100f);
            var boards = new GameObject[3];
            var bodies = new Rigidbody[3];
            var riders = new LabRider[3];
            var tick = new WaitForFixedUpdate();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    boards[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    boards[i].transform.position = new Vector3((i - 1) * 20f, 0.2f, 0f);
                    boards[i].transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
                    bodies[i] = boards[i].AddComponent<Rigidbody>();
                    bodies[i].constraints = RigidbodyConstraints.FreezeRotation;
                    riders[i] = boards[i].AddComponent<LabRider>();
                    var map = boards[i].AddComponent<SnowSurfaceMap>();
                    map.zones = new[] { new SnowSurfaceZone { kind = (SnowSurface)i, bounds = new Rect(-50f, -50f, 100f, 100f) } };
                    riders[i].SetSurfaceMap(map);
                }
                for (int step = 0; step < 60; step++) yield return tick;
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(riders[i].IsGrounded, Is.True);
                    Assert.That(riders[i].ActiveSurface, Is.EqualTo((SnowSurface)i));
                    bodies[i].linearVelocity = new Vector3(3f, 0f, 8f);
                }
                for (int step = 0; step < 30; step++) yield return tick;
                Debug.Log($"Surface fixture velocity packed={bodies[0].linearVelocity}, powder={bodies[1].linearVelocity}, ice={bodies[2].linearVelocity}");
                Assert.That(bodies[2].linearVelocity.x, Is.GreaterThan(bodies[0].linearVelocity.x + 0.15f), "Ice must retain more lateral slip.");
                Assert.That(bodies[1].linearVelocity.z, Is.LessThan(bodies[0].linearVelocity.z - 0.05f), "Powder must add longitudinal resistance.");
                Assert.That(bodies[2].linearVelocity.z, Is.GreaterThan(bodies[0].linearVelocity.z + 0.03f));
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                yield return null;
                yield return null;
                for (int i = 0; i < 3; i++) bodies[i].linearVelocity = Vector3.forward * 8f;
                for (int step = 0; step < 30; step++) yield return tick;
                Assert.That(bodies[1].linearVelocity.z, Is.LessThan(bodies[0].linearVelocity.z - 0.5f), "Powder braking should be stronger.");
                Assert.That(bodies[2].linearVelocity.z, Is.GreaterThan(bodies[0].linearVelocity.z + 0.8f), "Ice braking should be weaker.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                yield return null;
                for (int i = 0; i < 3; i++)
                {
                    bodies[i].position += Vector3.up * 5f;
                    bodies[i].useGravity = false;
                    bodies[i].linearVelocity = new Vector3(3f, 2f, 8f);
                }
                for (int step = 0; step < 30; step++) yield return tick;
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(riders[i].IsGrounded, Is.False);
                    Assert.That(riders[i].GripAcceleration, Is.Zero);
                    Assert.That(Vector3.Distance(bodies[i].linearVelocity, new Vector3(3f, 2f, 8f)), Is.LessThan(0.01f), "Ground coefficients must not affect flight.");
                    riders[i].ResetRider();
                    Assert.That(riders[i].ActiveSurface, Is.Null, "Reset clears the previous display surface until supported.");
                    bodies[i].useGravity = true;
                }
                for (int step = 0; step < 60; step++) yield return tick;
                for (int i = 0; i < 3; i++) Assert.That(riders[i].ActiveSurface, Is.EqualTo((SnowSurface)i));
            }
            finally
            {
                foreach (var board in boards) if (board) Object.Destroy(board);
                InputSystem.RemoveDevice(keyboard);
                InputSystem.settings.backgroundBehavior = background;
                InputSystem.settings.editorInputBehaviorInPlayMode = routing;
                Object.Destroy(ground);
                Time.fixedDeltaTime = oldFixed;
            }
        }
    }
}
