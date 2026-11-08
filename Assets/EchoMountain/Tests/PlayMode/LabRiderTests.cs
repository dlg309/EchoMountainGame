using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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

    }
}
