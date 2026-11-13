using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class CameraObstructionTests
    {
        [UnityTest]
        public IEnumerator CameraStaysInFrontOfThinWallAndReturnsAfterItClears()
        {
            float previous = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var view = new GameObject("Obstruction test camera");
            var follow = view.AddComponent<LabCamera>();
            follow.SetTarget(board.transform);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 2f, -3.5f);
            wall.transform.localScale = new Vector3(4f, 6f, 0.15f);
            try
            {
                Physics.SyncTransforms();
                for (int frame = 0; frame < 10; frame++) yield return null;
                Assert.That(view.transform.position.z, Is.GreaterThan(-3.1f),
                    "The camera volume must stay on the rider side of even a thin wall.");
                Assert.That(view.transform.position.z, Is.LessThan(-1f), "Do not collapse into the rider.");
                wall.SetActive(false);
                Physics.SyncTransforms();
                // Camera damping uses real (unscaled) time, even in accelerated tests.
                yield return new WaitForSecondsRealtime(2f);
                Assert.That(view.transform.position.z, Is.EqualTo(-6.5f).Within(0.03f));
                Assert.That(Mathf.Abs(view.transform.right.y), Is.LessThan(0.001f));
            }
            finally
            {
                Time.captureDeltaTime = previous;
                Object.Destroy(view);
                Object.Destroy(board);
                Object.Destroy(wall);
            }
        }

        [UnityTest]
        public IEnumerator CameraIgnoresTriggersAndRiderButClampsNewObstaclesImmediately()
        {
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var view = new GameObject("Obstruction test camera");
            var follow = view.AddComponent<LabCamera>();
            follow.SetTarget(board.transform);
            var ownCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ownCollider.transform.SetParent(board.transform);
            ownCollider.transform.localPosition = new Vector3(0f, 1f, -1f);
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = new Vector3(0f, 2f, -3.5f);
            obstacle.transform.localScale = new Vector3(4f, 6f, 0.15f);
            var collider = obstacle.GetComponent<Collider>();
            collider.isTrigger = true;
            try
            {
                Physics.SyncTransforms();
                yield return null;
                yield return null;
                Assert.That(view.transform.position.z, Is.EqualTo(-6.5f).Within(0.01f));
                collider.isTrigger = false;
                Physics.SyncTransforms();
                yield return null;
                yield return null;
                Assert.That(view.transform.position.z, Is.GreaterThan(-3.1f),
                    "Damping must not leave the camera behind a newly obstructing collider.");
                follow.SetReplayTravel(Vector3.back * 10f);
                follow.SnapNextFrame();
                yield return null;
                yield return null;
                Assert.That(view.transform.position.z, Is.GreaterThan(6f), "Replay direction changes must also resolve obstruction.");
            }
            finally
            {
                Object.Destroy(view);
                Object.Destroy(board);
                Object.Destroy(obstacle);
            }
        }
    }
}
