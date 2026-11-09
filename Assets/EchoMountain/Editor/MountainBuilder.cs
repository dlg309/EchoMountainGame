using System;
using System.Collections.Generic;
using System.IO;
using EchoMountain.Riding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EchoMountain.EditorTools
{
    public static class MountainBuilder
    {
        private const string AssetFolder = "Assets/EchoMountain/GlacierPass";
        public const string ScenePath = "Assets/EchoMountain/Scenes/GlacierPass.unity";
        private const string Template = "Assets/EchoMountain/Scenes/PhysicsLabRider.unity";

        [MenuItem("Echo Mountain/Create Glacier Pass (once)")]
        public static void Create()
        {
            if (File.Exists(ScenePath) || AssetDatabase.IsValidFolder(AssetFolder))
                throw new InvalidOperationException("Glacier Pass already has authored assets.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(Template);
            var root = GameObject.Find("Physics Lab").transform;
            var board = root.Find("Diagnostic board");
            foreach (string name in new[] { "Flat start", "Slope", "Takeoff", "Landing", "Runout", "Left boundary", "Right boundary", "Visual markers" })
            {
                var previous = root.Find(name);
                if (previous) Object.DestroyImmediate(previous.gameObject);
            }
            root.name = "Glacier Pass";
            AssetDatabase.CreateFolder("Assets/EchoMountain", "GlacierPass");

            Material snow = Material("Snow", new Color(0.79f, 0.89f, 0.95f));
            Material pineLane = Material("Packed Pine snow", new Color(0.72f, 0.84f, 0.9f));
            Material summitLane = Material("Packed Summit snow", new Color(0.86f, 0.9f, 0.94f));
            Material pine = Material("Pine needles", new Color(0.072f, 0.23f, 0.23f));
            Material blue = Material("Pine blue", new Color(0.05f, 0.62f, 0.84f));
            Material amber = Material("Summit amber", new Color(0.99f, 0.45f, 0.09f));
            Material stone = Material("Blue granite", new Color(0.24f, 0.37f, 0.46f));
            BuildGround(root, snow, pineLane, summitLane);
            BuildScenery(root, pine, snow, stone);
            BuildTrail(root, blue, amber, stone);

            board.position = MountainCourse.Point(MountainCourse.LaneX(MountainLine.Pine), MountainCourse.Start, 0.15f);
            board.rotation = MountainCourse.Heading(board.position.x, board.position.z);
            var tuning = new SerializedObject(board.GetComponent<LabRider>());
            tuning.FindProperty("quadraticDrag").floatValue = 0.0057f;
            tuning.ApplyModifiedPropertiesWithoutUndo();
            board.gameObject.AddComponent<MountainRun>();

            Camera camera = GameObject.Find("Lab Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.57f, 0.76f, 0.87f);
            camera.farClipPlane = 1400f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 115f;
            RenderSettings.fogEndDistance = 590f;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save Glacier Pass.");
            AssetDatabase.SaveAssets();
        }

        private static void BuildGround(Transform parent, Material snow, Material pine, Material summit)
        {
            const int width = 45;
            const int length = 969;
            var positions = new Vector3[width * length];
            var uv = new Vector2[positions.Length];
            var triangles = new[] { new List<int>(), new List<int>(), new List<int>() };
            for (int row = 0; row < length; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    int i = row * width + column;
                    float x = (column - 22) * 2f;
                    float z = row - 16f;
                    positions[i] = MountainCourse.Point(x, z);
                    uv[i] = new Vector2(x, z) * 0.0625f;
                    if (row == length - 1 || column == width - 1) continue;
                    int band = x >= -16f && x < -4f ? 1 : x >= 4f && x < 16f ? 2 : 0;
                    triangles[band].AddRange(new[] { i, i + width, i + 1, i + 1, i + width, i + width + 1 });
                }
            }

            var mesh = new Mesh { name = "Glacier Pass snow surface", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = positions;
            mesh.uv = uv;
            mesh.subMeshCount = triangles.Length;
            for (int i = 0; i < triangles.Length; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, AssetFolder + "/Snow Surface.asset");

            var ground = new GameObject("Connected snow surface");
            ground.transform.SetParent(parent, false);
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterials = new[] { snow, pine, summit };
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
            ground.isStatic = true;
        }

        private static void BuildScenery(Transform parent, Material pine, Material snow, Material stone)
        {
            var tree = new Mesh { name = "Low-poly fir" };
            const int faces = 7;
            var points = new Vector3[faces * 6];
            var indices = new int[points.Length];
            for (int tier = 0; tier < 2; tier++)
            {
                float radius = tier == 0 ? 1f : 0.7f;
                float baseY = tier * 1.45f + 0.25f;
                for (int i = 0; i < faces; i++)
                {
                    float angle = i * 2f * Mathf.PI / faces;
                    float next = (i + 1) * 2f * Mathf.PI / faces;
                    int index = (tier * faces + i) * 3;
                    points[index] = new Vector3(Mathf.Sin(angle) * radius, baseY, Mathf.Cos(angle) * radius);
                    points[index + 1] = Vector3.up * (baseY + 2.3f);
                    points[index + 2] = new Vector3(Mathf.Sin(next) * radius, baseY, Mathf.Cos(next) * radius);
                    indices[index] = index;
                    indices[index + 1] = index + 2;
                    indices[index + 2] = index + 1;
                }
            }
            tree.vertices = points;
            tree.triangles = indices;
            tree.RecalculateNormals();
            tree.RecalculateBounds();
            AssetDatabase.CreateAsset(tree, AssetFolder + "/Fir.asset");
            var random = new System.Random(1834);
            for (int i = 0; i < 82; i++)
            {
                float z = 18f + i * 10.7f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (22.8f + (float)random.NextDouble() * 15f);
                    float size = 0.9f + (float)random.NextDouble() * 1.4f;
                    var fir = new GameObject("Timberline fir");
                    fir.transform.SetParent(parent, false);
                    fir.transform.position = MountainCourse.Point(x, z, -0.3f);
                    fir.transform.localScale = Vector3.one * size;
                    fir.AddComponent<MeshFilter>().sharedMesh = tree;
                    fir.AddComponent<MeshRenderer>().sharedMaterial = pine;
                    fir.isStatic = true;
                }
            }

            Mesh peak = BuildPeak();
            for (int i = 0; i < 15; i++)
            {
                int side = i % 2 == 0 ? -1 : 1;
                float z = 120f + i * 61f;
                float x = side * (132f + (i % 3) * 24f);
                var summit = new GameObject("Glacier ridge");
                summit.transform.SetParent(parent, false);
                summit.transform.position = MountainCourse.Point(x, z, -31f);
                summit.transform.localScale = new Vector3(85f, 70f + i % 4 * 16f, 99f);
                summit.transform.rotation = Quaternion.Euler(0f, i * 17f, 0f);
                summit.AddComponent<MeshFilter>().sharedMesh = peak;
                summit.AddComponent<MeshRenderer>().sharedMaterials = new[] { stone, snow };
                summit.isStatic = true;
            }
        }

        private static Mesh BuildPeak()
        {
            const int sides = 9;
            var vertices = new List<Vector3>();
            var rock = new List<int>();
            var cap = new List<int>();
            Vector3 Ring(int index, bool snowLine)
            {
                float angle = index * 2f * Mathf.PI / sides;
                float radius = 0.78f + 0.13f * Mathf.Cos(3f * angle);
                if (snowLine) radius *= 0.35f + 0.08f * Mathf.Cos(4f * angle);
                return new Vector3(radius * Mathf.Sin(angle), snowLine ? 0.59f + 0.08f * Mathf.Cos(3f * angle) : 0f,
                    radius * Mathf.Cos(angle));
            }

            void Face(List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
            {
                triangles.Add(vertices.Count);
                triangles.Add(vertices.Count + 1);
                triangles.Add(vertices.Count + 2);
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
            }

            var tip = new Vector3(0.11f, 1f, -0.09f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = Ring(i, false);
                Vector3 b = Ring(i + 1, false);
                Vector3 c = Ring(i, true);
                Vector3 d = Ring(i + 1, true);
                Face(rock, a, b, c);
                Face(rock, b, d, c);
                Face(cap, c, d, tip);
            }

            var mesh = new Mesh { name = "Faceted granite peak" };
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(rock, 0);
            mesh.SetTriangles(cap, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, AssetFolder + "/Peak.asset");
            return mesh;
        }

        private static void BuildTrail(Transform parent, Material blue, Material amber, Material stone)
        {
            for (int z = 24; z < 865; z += 24)
            {
                foreach (int x in new[] { -15, -4, 4, 15 })
                {
                    var tint = x < 0 ? blue : amber;
                    Block(parent, "Route stake", MountainCourse.Point(x, z, 0.75f),
                        new Vector3(0.14f, 1.5f, 0.14f), Quaternion.identity, tint);
                    Block(parent, "Route flag", MountainCourse.Point(x + 0.32f, z, 1.3f),
                        new Vector3(0.65f, 0.34f, 0.08f), Quaternion.identity, tint);
                }
            }

            foreach (float crest in new[] { 220f, 408f, 608f })
            {
                foreach (float x in new[] { 5f, 14f })
                {
                    Block(parent, "Takeoff marker", MountainCourse.Point(x, crest, 0.04f),
                        new Vector3(1.35f, 0.07f, 0.50f), MountainCourse.Heading(x, crest), amber);
                    Block(parent, "Landing marker", MountainCourse.Point(x, crest + 11f, 0.04f),
                        new Vector3(1.35f, 0.07f, 0.50f), MountainCourse.Heading(x, crest + 11f), blue);
                }
            }

            foreach (int x in new[] { -19, 19 })
                Block(parent, "Finish column", MountainCourse.Point(x, MountainCourse.Finish, 3f),
                    new Vector3(0.68f, 6f, 0.68f), Quaternion.identity, stone);
            Block(parent, "Finish arch", MountainCourse.Point(0f, MountainCourse.Finish, 6f),
                new Vector3(39f, 0.9f, 0.55f), Quaternion.identity, blue);
            var sign = new GameObject("Glacier Pass finish sign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = MountainCourse.Point(0f, MountainCourse.Finish - 0.31f, 6f);
            var text = sign.AddComponent<TextMesh>();
            text.anchor = TextAnchor.MiddleCenter;
            text.fontSize = 90;
            text.characterSize = 0.35f;
            text.text = "GLACIER PASS   /   FINISH";
            text.color = Color.white;
        }

        private static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(material, AssetFolder + "/" + name + ".mat");
            return material;
        }

        private static void Block(Transform parent, string name, Vector3 position, Vector3 size, Quaternion rotation, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = size;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            block.isStatic = true;
        }
    }
}
