#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using EchoMountain.Riding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoMountain.Tests
{
    public sealed class RiderCustomizationTests
    {
        private string directory;

        [SetUp]
        public void Setup()
        {
            directory = Path.Combine(Path.GetTempPath(), "EchoMountain-Look-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (SceneManager.GetActiveScene().path == FreeRoamMap.ScenePath ||
                SceneManager.GetActiveScene().path == FreeRoamMap.MainMenuPath)
            {
                Scene active = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("Customization test cleanup"));
                yield return SceneManager.UnloadSceneAsync(active);
            }
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void LookPersistsAndRejectsInvalidChoices()
        {
            string path = Path.Combine(directory, RiderCustomizationStore.FileName);
            var store = new RiderCustomizationStore(path);
            RiderLook look = store.Look.Copy();
            look.jacket = 2;
            look.pants = 1;
            look.helmet = 3;
            look.goggles = 4;
            look.topside = 1;
            look.underside = 2;
            Assert.That(store.Set(look));
            Assert.That(new RiderCustomizationStore(path).Look.underside, Is.EqualTo(2));
            Assert.That(new RiderCustomizationStore(path).Look.goggles, Is.EqualTo(4));
            look.helmet = -1;
            Assert.That(store.Set(look), Is.False);
            Assert.That(new RiderCustomizationStore(path).Look.helmet, Is.EqualTo(3));
            File.WriteAllText(path, "broken JSON");
            Assert.That(new RiderCustomizationStore(path).Look.topside, Is.Zero);
        }

        [UnityTest]
        public IEnumerator MenuSelectionsAppearOnEveryModeAndSurviveSceneLoad()
        {
            string path = Path.Combine(directory, RiderCustomizationStore.FileName);
            yield return SceneManager.LoadSceneAsync(FreeRoamMap.MainMenuPath);
            yield return null;
            var course = UnityEngine.Object.FindFirstObjectByType<MountainRun>();
            var store = new RiderCustomizationStore(path);
            course.UseCustomization(store);
            for (int part = 0; part < 6; part++) course.ChooseLookPart(part, 1);
            course.ChooseLookPart(5, 1);
            Assert.That(new RiderCustomizationStore(path).Look.topside, Is.EqualTo(1));
            Assert.That(new RiderCustomizationStore(path).Look.underside, Is.EqualTo(2));

            course.SelectLine(MountainLine.Pine);
            course.StartRun();
            yield return null;
            AssertLook(course, store.Look);
            course.ShowStart();
            course.SelectLine(MountainLine.Summit);
            course.StartRun();
            yield return null;
            AssertLook(course, store.Look);
            course.ShowStart();
            course.StartPractice();
            yield return null;
            AssertLook(course, store.Look);
            course.ShowStart();

            course.StartFreeRoam();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (SceneManager.GetActiveScene().path != FreeRoamMap.ScenePath && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(FreeRoamMap.ScenePath));
            var roam = UnityEngine.Object.FindFirstObjectByType<MountainRun>();
            roam.UseCustomization(new RiderCustomizationStore(path));
            AssertLook(roam, store.Look);
        }

        private static void AssertLook(MountainRun run, RiderLook look)
        {
            Transform rig = run.transform.parent.Find("Segmented rider");
            AssertColor(rig.Find("Jacket"), RiderCustomization.Colors[look.jacket]);
            AssertColor(rig.Find("Rear thigh"), RiderCustomization.Colors[look.pants]);
            AssertColor(rig.Find("Helmet"), RiderCustomization.Colors[look.helmet]);
            AssertColor(rig.Find("Helmet/Goggles"), RiderCustomization.Colors[look.goggles]);
            AssertColor(rig.Find("Front binding"), RiderCustomization.DeckAccents[look.topside]);
            var graphic = run.transform.Find("Board graphics").GetComponent<MeshRenderer>();
            Assert.That(graphic.sharedMaterials.Length, Is.EqualTo(2));
            Assert.That(graphic.sharedMaterials[0].GetTexture("_BaseMap"), Is.Not.Null);
            Assert.That(graphic.sharedMaterials[1].GetTexture("_BaseMap"), Is.Not.Null);
            Assert.That(run.GetComponent<MeshFilter>().sharedMesh.name, Is.EqualTo("Twin-tip"));
        }

        private static void AssertColor(Transform part, Color expected)
        {
            Color actual = part.GetComponent<Renderer>().sharedMaterial.color;
            Assert.That(Mathf.Abs(actual.r - expected.r) + Mathf.Abs(actual.g - expected.g) +
                Mathf.Abs(actual.b - expected.b), Is.LessThan(0.001f), part.name);
        }
    }
}
#endif
