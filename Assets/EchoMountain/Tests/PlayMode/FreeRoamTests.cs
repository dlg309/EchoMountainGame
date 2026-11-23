#if UNITY_EDITOR
using System.Collections;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class FreeRoamTests
    {
        private Scene basin;
        private static readonly int[] NarrowRails = { 1, 2 };
        private bool testedMenu;

        [UnityTearDown]
        public IEnumerator UnloadBasin()
        {
            if (testedMenu)
            {
                testedMenu = false;
                Scene active = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("Free Roam test cleanup"));
                yield return SceneManager.UnloadSceneAsync(active);
            }
            if (basin.IsValid() && basin.isLoaded) yield return SceneManager.UnloadSceneAsync(basin);
        }

        [UnityTest]
        public IEnumerator MainMenuOpensBasinAndReturnsToRouteSelection()
        {
            testedMenu = true;
            Assert.That(Application.CanStreamedLevelBeLoaded(FreeRoamMap.ScenePath));
            yield return SceneManager.LoadSceneAsync(FreeRoamMap.MainMenuPath);
            yield return null;
            var course = Object.FindFirstObjectByType<MountainRun>();
            Assert.That(course.Phase, Is.EqualTo(MountainPhase.Ready));

            course.StartFreeRoam();
            yield return WaitForScene(FreeRoamMap.ScenePath);
            basin = SceneManager.GetSceneByPath(FreeRoamMap.ScenePath);
            var roam = Object.FindFirstObjectByType<MountainRun>();
            Assert.That(roam.IsFreeRoam);
            Assert.That(roam.Phase, Is.EqualTo(MountainPhase.Riding));
            Assert.That(roam.Replay, Is.Null);
            Assert.That(roam.Elapsed, Is.Zero);

            roam.ShowStart();
            yield return WaitForScene(FreeRoamMap.MainMenuPath);
            course = Object.FindFirstObjectByType<MountainRun>();
            Assert.That(course.IsFreeRoam, Is.False);
            Assert.That(course.Phase, Is.EqualTo(MountainPhase.Ready));
        }

        [UnityTest]
        public IEnumerator DropInsAccelerateAndOutOfBoundsAndInvertedBoardsRecover()
        {
            yield return LoadBasin();
            var map = FindInBasin<FreeRoamMap>();
            var run = FindInBasin<MountainRun>();
            var rider = run.GetComponent<LabRider>();
            var body = rider.GetComponent<Rigidbody>();
            Assert.That(map.Terrain.terrainData.size.x, Is.EqualTo(600f));
            Assert.That(map.Terrain.terrainData.size.z, Is.EqualTo(980f));
            Assert.That(run.DropInCount, Is.EqualTo(3));

            for (int index = 0; index < run.DropInCount; index++)
            {
                run.MoveToDropIn(index);
                uint reset = rider.ResetCount;
                for (int tick = 0; tick < 300; tick++) yield return new WaitForFixedUpdate();
                Debug.Log($"Basin {run.DropInName(index)}: {body.position}, {rider.SpeedKmh:F1} km/h, resets {rider.ResetCount - reset}");
                Assert.That(rider.ResetCount, Is.EqualTo(reset), run.DropInName(index));
                Assert.That(rider.SpeedKmh, Is.GreaterThan(18f), run.DropInName(index));
                Assert.That(body.position.z - map.DropIn(index).position.z, Is.GreaterThan(15f), run.DropInName(index));
            }

            uint before = rider.ResetCount;
            body.position = new Vector3(302f, 210f, 450f);
            yield return new WaitForFixedUpdate();
            Assert.That(rider.ResetCount, Is.GreaterThan(before));
            Assert.That(Vector3.Distance(body.position, map.DropIn(2).position), Is.LessThan(0.3f));

            before = rider.ResetCount;
            Vector3 inverted = map.DropIn(2).position;
            body.position = inverted;
            body.rotation = Quaternion.Euler(0f, 0f, 180f);
            yield return new WaitForFixedUpdate();
            Assert.That(rider.ResetCount, Is.GreaterThan(before));
            Assert.That(Vector3.Dot(body.rotation * Vector3.up, Vector3.up), Is.GreaterThan(0.8f));
            Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
            Assert.That(run.Elapsed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RailRequiresAlignmentAndBanksAfterRideOffAndLanding()
        {
            yield return LoadBasin();
            var map = FindInBasin<FreeRoamMap>();
            var run = FindInBasin<MountainRun>();
            var rider = run.GetComponent<LabRider>();
            var body = rider.GetComponent<Rigidbody>();
            var rail = map.Rails[0];
            Assert.That(rail.surface, Is.Not.Null);
            Assert.That(rail.surface.isTrigger, Is.False);
            Vector3 axis = (rail.end - rail.start).normalized;

            Vector3 aboveRail = Vector3.Lerp(rail.start, rail.end, 0.30f) + Vector3.up * 0.85f;
            Vector3 across = Vector3.Cross(Vector3.up, axis).normalized;
            body.position = aboveRail;
            body.rotation = Quaternion.LookRotation(across);
            body.linearVelocity = across * 6f + Vector3.down;
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrinding, Is.False, "A transverse approach must not snap onto the rail.");
            }

            run.MoveToDropIn(2);
            for (int i = 0; i < 530 && body.position.z < 291f; i++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.z, Is.GreaterThanOrEqualTo(291f));
            Assert.That(rider.IsGrounded);
            rider.RequestJump();
            for (int i = 0; i < 140 && !rider.IsGrinding; i++) yield return new WaitForFixedUpdate();
            Assert.That(rider.IsGrinding, $"Popped from the Switchyard approach, board at {body.position}");
            Assert.That(run.Tricks.Points, Is.Zero);
            Assert.That(body.useGravity, Is.True);
            Assert.That(body.isKinematic, Is.False);
            for (int i = 0; i < 140 && rider.IsGrinding; i++) yield return new WaitForFixedUpdate();
            Assert.That(rider.IsGrinding, Is.False);
            Assert.That(body.useGravity);
            Assert.That(body.isKinematic, Is.False);
            Assert.That(run.Tricks.Points, Is.Zero);

            for (int i = 0; i < 100 && rider.LandingCount == 0; i++) yield return new WaitForFixedUpdate();
            Assert.That(rider.LandingCount, Is.EqualTo(1));
            Assert.That(rider.LastLanding, Is.Not.EqualTo(LandingOutcome.Bail));
            Assert.That(run.Tricks.Points, Is.GreaterThan(0));
            Assert.That(run.Tricks.Feedback, Does.Contain("Grind"));
            int bank = run.Tricks.Points;
            run.RestartRun();
            Assert.That(run.Tricks.Points, Is.EqualTo(bank));
            Assert.That(run.Tricks.PendingPoints, Is.Zero);
            Assert.That(run.Elapsed, Is.Zero);
        }

        [UnityTest]
        public IEnumerator HighTraversePopClearsSnowAndReturnsToRiding()
        {
            yield return LoadBasin();
            var map = FindInBasin<FreeRoamMap>();
            var run = FindInBasin<MountainRun>();
            var rider = run.GetComponent<LabRider>();
            var body = rider.GetComponent<Rigidbody>();
            Vector3 start = new(0f, 0f, 113f);
            start.y = map.Terrain.SampleHeight(start) + 0.14f;
            Vector3 ahead = start + Vector3.forward;
            ahead.y = map.Terrain.SampleHeight(ahead) + 0.14f;
            body.position = start;
            body.rotation = Quaternion.LookRotation(ahead - start);
            body.linearVelocity = body.rotation * Vector3.forward * 13f;
            uint resets = rider.ResetCount;
            for (int i = 0; i < 120 && body.position.z < 130f; i++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.z, Is.GreaterThanOrEqualTo(130f));
            Assert.That(rider.IsGrounded);
            rider.RequestJump();
            float maximumClearance = 0f;
            for (int i = 0; i < 180 && rider.LandingCount == 0; i++)
            {
                yield return new WaitForFixedUpdate();
                maximumClearance = Mathf.Max(maximumClearance, body.position.y - map.Terrain.SampleHeight(body.position));
            }
            Debug.Log($"Basin jump: clearance {maximumClearance:F2} m, landing {rider.LastLanding}, position {body.position}");
            Assert.That(maximumClearance, Is.GreaterThan(0.85f));
            Assert.That(rider.LandingCount, Is.EqualTo(1));
            Assert.That(rider.LastLanding, Is.Not.EqualTo(LandingOutcome.Bail));
            Assert.That(rider.ResetCount, Is.EqualTo(resets));
            Assert.That(run.Phase, Is.EqualTo(MountainPhase.Riding));
        }

        [UnityTest]
        public IEnumerator NarrowRailSupportsBoardAndPopsOntoSnow([ValueSource(nameof(NarrowRails))] int index)
        {
            yield return LoadBasin();
            var map = FindInBasin<FreeRoamMap>();
            var run = FindInBasin<MountainRun>();
            var rider = run.GetComponent<LabRider>();
            var body = rider.GetComponent<Rigidbody>();
            var rail = map.Rails[index];
            Vector3 top = Vector3.Lerp(rail.start, rail.end, 0.53f);
            Vector3 normal = rail.surface.transform.up;
            Vector3 direction = rail.surface.transform.forward;
            Assert.That(rail.surface.Raycast(new Ray(top + normal, -normal), out var hit, 2f));
            Assert.That(Vector3.Distance(hit.point, top), Is.LessThan(0.01f));
            Assert.That(rail.surface.transform.lossyScale.x, Is.LessThan(0.30f));

            body.position = top + normal * 0.63f;
            body.rotation = Quaternion.LookRotation(direction, normal);
            body.linearVelocity = direction * 9f - normal * 1.5f;
            for (int step = 0; step < 60 && !rider.IsGrinding; step++) yield return new WaitForFixedUpdate();
            Assert.That(rider.IsGrinding);
            Vector3 entry = body.position;
            for (int step = 0; step < 16; step++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrinding);
            }
            Assert.That(Vector3.Dot(body.position - entry, direction), Is.GreaterThan(1.8f));
            Assert.That(run.Tricks.Points, Is.Zero);
            rider.RequestJump();
            yield return new WaitForFixedUpdate();
            Assert.That(rider.IsGrinding, Is.False);
            Assert.That(Vector3.Dot(body.linearVelocity, normal), Is.GreaterThan(2f));
            for (int step = 0; step < 155 && rider.LandingCount == 0; step++) yield return new WaitForFixedUpdate();
            Assert.That(rider.LandingCount, Is.EqualTo(1));
            Assert.That(run.Tricks.Feedback, Does.Contain("Grind"));
            Assert.That(run.Tricks.Points, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator VisibleDeckMeetsFlatAndSlopingSnow()
        {
            yield return LoadBasin();
            var map = FindInBasin<FreeRoamMap>();
            var rider = FindInBasin<LabRider>();
            var deck = rider.GetComponent<MeshFilter>().sharedMesh;
            float underside = deck.bounds.min.y;
            var flat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Vector3 location = map.DropIn(0).position;
            flat.transform.position = location + Vector3.up * 5f;
            flat.transform.localScale = new Vector3(12f, 1f, 12f);
            try
            {
                rider.SetSpawn(flat.transform.position + Vector3.up * 0.66f, Quaternion.identity);
                for (int step = 0; step < 45; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded);
                Vector3 deckCenter = rider.transform.TransformPoint(0f, underside, 0f);
                float flatGap = deckCenter.y - (flat.transform.position.y + 0.5f);
                Assert.That(flatGap, Is.InRange(-0.015f, 0.025f));

                rider.SetSpawn(location, map.DropIn(0).rotation);
                for (int step = 0; step < 100; step++) yield return new WaitForFixedUpdate();
                Assert.That(rider.IsGrounded);
                deckCenter = rider.transform.TransformPoint(0f, underside, 0f);
                float slopeGap = deckCenter.y - map.Terrain.SampleHeight(deckCenter);
                Debug.Log($"Basin deck gap: flat={flatGap:F3} m, slope={slopeGap:F3} m");
                Assert.That(slopeGap, Is.InRange(-0.02f, 0.05f));
            }
            finally
            {
                Object.Destroy(flat);
            }
        }

        private IEnumerator LoadBasin()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(FreeRoamMap.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));
            basin = SceneManager.GetSceneByPath(FreeRoamMap.ScenePath);
            yield return null;
        }

        private T FindInBasin<T>() where T : Component
        {
            foreach (var root in basin.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>();
                if (component) return component;
            }
            throw new AssertionException($"{typeof(T).Name} missing from Sunset Basin.");
        }

        private static IEnumerator WaitForScene(string path)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().path != path && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(path));
        }
    }
}
#endif
