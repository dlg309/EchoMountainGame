using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoMountain.EditorTools
{
    // One-time presentation pass on the owned diagnostic scene; never regenerates it.
    public static class PhysicsLabPresentation
    {
        private const string ScenePath = "Assets/EchoMountain/Scenes/PhysicsLab.unity";
        private const string ArtPath = "Assets/EchoMountain/LabArt";

        [MenuItem("Echo Mountain/Add physics lab visual markers (once)")]
        public static void AddMarkers()
        {
            if (!System.IO.File.Exists(ScenePath)) throw new InvalidOperationException("Create the physics lab first.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var root = GameObject.Find("Physics Lab");
            if (!root) throw new InvalidOperationException("Physics Lab root missing.");
            if (root.transform.Find("Visual markers")) { Debug.Log("Lab visual markers already exist; no changes made."); return; }
            if (!AssetDatabase.IsValidFolder(ArtPath)) AssetDatabase.CreateFolder("Assets/EchoMountain", "LabArt");
            var board = MakeMaterial("Board", new Color(0.85f, 0.20f, 0.04f));
            var snow = MakeMaterial("Snow", new Color(0.70f, 0.82f, 0.90f));
            var stripe = MakeMaterial("Marker", new Color(0.03f, 0.24f, 0.36f));
            var takeoff = MakeMaterial("Takeoff", new Color(0.98f, 0.63f, 0.10f));
            SetColor(root.transform, "Diagnostic board", board);
            foreach (var name in new[] { "Flat start", "Slope", "Landing", "Runout" }) SetColor(root.transform, name, snow);
            SetColor(root.transform, "Takeoff", takeoff);
            SetColor(root.transform, "Left boundary", stripe);
            SetColor(root.transform, "Right boundary", stripe);
            var markerRoot = new GameObject("Visual markers");
            markerRoot.transform.SetParent(root.transform, false);
            var slope = root.transform.Find("Slope");
            if (!slope) throw new InvalidOperationException("Slope missing.");
            // World-space markers follow the existing slope top without changing its collider.
            for (int i = 0; i < 10; i++)
            {
                float z = -17f + 4f * i;
                for (int side = -1; side <= 1; side += 2)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.name = "Slope marker " + i + " " + side;
                    UnityEngine.Object.DestroyImmediate(marker.GetComponent<Collider>());
                    marker.transform.SetParent(markerRoot.transform, false);
                    marker.transform.position = slope.position + slope.rotation * new Vector3(side * 5f, 0.52f, z);
                    marker.transform.rotation = slope.rotation;
                    marker.transform.localScale = new Vector3(1.2f, 0.035f, 0.35f);
                    marker.GetComponent<Renderer>().sharedMaterial = stripe;
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save lab markers.");
            AssetDatabase.SaveAssets();
            Debug.Log("Added physics lab visual markers.");
        }

        [MenuItem("Echo Mountain/Add lab rider placeholder (once)")]
        public static void AddRiderPlaceholder()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var board = GameObject.Find("Diagnostic board");
            if (!board) throw new InvalidOperationException("Diagnostic board missing.");
            if (board.transform.Find("Diagnostic rider")) return;
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/Marker.mat");
            if (!material) throw new InvalidOperationException("Create lab markers first.");
            var rider = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rider.name = "Diagnostic rider";
            UnityEngine.Object.DestroyImmediate(rider.GetComponent<Collider>());
            rider.transform.SetParent(board.transform, false);
            rider.transform.localPosition = new Vector3(0f, 4.5f, 0f);
            rider.transform.localRotation = Quaternion.identity;
            rider.transform.localScale = new Vector3(0.72f, 4f, 0.26f);
            rider.GetComponent<Renderer>().sharedMaterial = material;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Could not save lab rider.");
            Debug.Log("Added diagnostic rider placeholder.");
        }

        [MenuItem("Echo Mountain/Copy lab to readable scene (once)")]
        public static void CopyReadableScene()
        {
            const string destination = "Assets/EchoMountain/Scenes/PhysicsLabReadable.unity";
            if (System.IO.File.Exists(destination)) throw new InvalidOperationException("Readable lab already exists.");
            if (!AssetDatabase.CopyAsset(ScenePath, destination)) throw new InvalidOperationException("Could not copy lab scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("Created readable lab scene without replacing original.");
        }

        [MenuItem("Echo Mountain/Add readable lab HUD (once)")]
        public static void AddReadableHud()
        {
            const string path = "Assets/EchoMountain/Scenes/PhysicsLabReadable.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(path);
            var board = GameObject.Find("Diagnostic board");
            if (!board) throw new InvalidOperationException("Diagnostic board missing.");
            if (!board.GetComponent<EchoMountain.Riding.LabHud>()) board.AddComponent<EchoMountain.Riding.LabHud>();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new InvalidOperationException("Could not save lab HUD.");
            Debug.Log("Added readable lab HUD.");
        }

        [MenuItem("Echo Mountain/Copy HUD lab scene (once)")]
        public static void CopyHudScene()
        {
            const string source = "Assets/EchoMountain/Scenes/PhysicsLabReadable.unity";
            const string destination = "Assets/EchoMountain/Scenes/PhysicsLabHUD.unity";
            if (System.IO.File.Exists(destination)) throw new InvalidOperationException("HUD lab already exists.");
            if (!AssetDatabase.CopyAsset(source, destination)) throw new InvalidOperationException("Could not copy HUD lab scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("Created HUD lab scene without replacing original.");
        }

        [MenuItem("Echo Mountain/Create articulated rider lab (once)")]
        public static void CreateRiderScene()
        {
            const string source = "Assets/EchoMountain/Scenes/PhysicsLabHUD.unity";
            const string destination = "Assets/EchoMountain/Scenes/PhysicsLabRider.unity";
            if (System.IO.File.Exists(destination)) throw new InvalidOperationException("Rider lab already exists.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(source);
            var board = GameObject.Find("Diagnostic board");
            if (!board) throw new InvalidOperationException("Diagnostic board missing.");
            var placeholder = board.transform.Find("Diagnostic rider");
            if (!placeholder) throw new InvalidOperationException("Diagnostic rider placeholder missing.");
            var palette = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/Board.mat");
            if (!palette) throw new InvalidOperationException("Lab board material missing.");

            placeholder.gameObject.SetActive(false);
            board.AddComponent<EchoMountain.Riding.LabRiderVisual>().SetPalette(palette);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, destination))
                throw new InvalidOperationException("Could not save the articulated rider lab.");
            AssetDatabase.SaveAssets();
        }

        private static Material MakeMaterial(string name, Color color)
        {
            string path = ArtPath + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("URP/Lit shader not found.");
            var material = new Material(shader) { name = "Lab " + name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void SetColor(Transform root, string name, Material material)
        {
            var item = root.Find(name);
            if (!item || !item.TryGetComponent<Renderer>(out var renderer))
                throw new InvalidOperationException("Missing lab renderer: " + name);
            renderer.sharedMaterial = material;
        }
    }
}
