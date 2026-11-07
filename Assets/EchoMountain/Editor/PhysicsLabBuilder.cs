using EchoMountain.Riding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoMountain.EditorTools
{
    public static class PhysicsLabBuilder
    {
        private const string ScenePath = "Assets/EchoMountain/Scenes/PhysicsLab.unity";

        [MenuItem("Echo Mountain/Create physics lab (once)")]
        public static void CreateLab()
        {
            if (System.IO.File.Exists(ScenePath))
            {
                Debug.LogWarning("Physics lab already exists; refusing to overwrite it.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!AssetDatabase.IsValidFolder("Assets/EchoMountain/Scenes"))
                AssetDatabase.CreateFolder("Assets/EchoMountain", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("Physics Lab");
            MakeBlock("Flat start", new Vector3(0f, -0.5f, -9f), new Vector3(16f, 1f, 18f), 0f, root.transform);
            MakeBlock("Slope", new Vector3(0f, -3.7f, 19f), new Vector3(16f, 1f, 40f), 12f, root.transform);
            MakeBlock("Takeoff", new Vector3(0f, -8f, 41f), new Vector3(16f, 1f, 6f), -3f, root.transform);
            MakeBlock("Landing", new Vector3(0f, -11.8f, 55f), new Vector3(20f, 1f, 20f), 12f, root.transform);
            MakeBlock("Runout", new Vector3(0f, -14f, 75f), new Vector3(20f, 1f, 22f), 0f, root.transform);
            MakeBlock("Left boundary", new Vector3(-10.5f, -8f, 40f), new Vector3(1f, 20f, 100f), 0f, root.transform);
            MakeBlock("Right boundary", new Vector3(10.5f, -8f, 40f), new Vector3(1f, 20f, 100f), 0f, root.transform);

            GameObject rider = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rider.name = "Diagnostic board";
            rider.transform.SetParent(root.transform);
            rider.transform.position = new Vector3(0f, 1.2f, 3f);
            rider.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
            rider.transform.localScale = new Vector3(0.55f, 0.2f, 1.25f);
            Rigidbody rb = rider.AddComponent<Rigidbody>();
            rb.mass = 75f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rider.AddComponent<LabRider>();
            GameObject cameraObject = new GameObject("Lab Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.position = rider.transform.position + new Vector3(0f, 4f, -8f);
            cameraObject.AddComponent<LabCamera>().SetTarget(rider.transform);
            camera.nearClipPlane = 0.1f;
            var lightObject = new GameObject("Sun");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                Debug.LogError("Could not save physics lab scene.");
            else Debug.Log("Created physics lab at " + ScenePath + ". Add it to build settings after validation.");
        }

        private static void MakeBlock(string name, Vector3 position, Vector3 size, float angle, Transform parent)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.rotation = Quaternion.Euler(angle, 0f, 0f);
            block.transform.localScale = size;
        }
    }
}
