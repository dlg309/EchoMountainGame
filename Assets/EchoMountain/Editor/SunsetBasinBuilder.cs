using System;
using System.IO;
using EchoMountain.Riding;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EchoMountain.EditorTools
{
    public static class SunsetBasinBuilder
    {
        internal const string Folder = "Assets/EchoMountain/SunsetBasin";
        private const string Template = "Assets/EchoMountain/Scenes/PhysicsLabRider.unity";
        private static readonly Vector3 SunDirection = new Vector3(0.26f, 0.16f, 1f).normalized;

        [MenuItem("Echo Mountain/Create Sunset Basin (once)")]
        public static void Create()
        {
            if (File.Exists(FreeRoamMap.ScenePath) || AssetDatabase.IsValidFolder(Folder))
                throw new InvalidOperationException("Sunset Basin already has authored assets.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(Template);
            var root = GameObject.Find("Physics Lab").transform;
            var board = root.Find("Diagnostic board");
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child != board) Object.DestroyImmediate(child.gameObject);
            }
            root.name = "Sunset Basin";
            AssetDatabase.CreateFolder("Assets/EchoMountain", "SunsetBasin");
            Terrain ground = BuildTerrain(root);
            var starts = new[]
            {
                Start(root, ground, "High Traverse", 0f, 31f),
                Start(root, ground, "West Ridge", -148f, 133f),
                Start(root, ground, "Switchyard", 132f, 249f)
            };
            var map = root.gameObject.AddComponent<FreeRoamMap>();
            map.Configure(ground, starts, SunsetBasinScenery.Build(root, ground));
            board.SetPositionAndRotation(starts[0].position, starts[0].rotation);
            var rider = new SerializedObject(board.GetComponent<LabRider>());
            rider.FindProperty("quadraticDrag").floatValue = 0.0057f;
            rider.ApplyModifiedPropertiesWithoutUndo();
            var run = new SerializedObject(board.gameObject.AddComponent<MountainRun>());
            run.FindProperty("freeRoam").objectReferenceValue = map;
            run.ApplyModifiedPropertiesWithoutUndo();
            BuildBoard(board);
            SetupLighting();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, FreeRoamMap.ScenePath))
                throw new InvalidOperationException("Could not save Sunset Basin.");
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Echo Mountain/Update Sunset Basin riding geometry")]
        public static void UpdateRidingGeometry()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.GetSceneByPath(FreeRoamMap.ScenePath);
            if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(FreeRoamMap.ScenePath);
            var map = GameObject.Find("Sunset Basin").GetComponent<FreeRoamMap>();
            var board = map.transform.Find("Diagnostic board");
            var deck = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Twin-tip.asset");
            Undo.RecordObjects(new Object[] { map, deck }, "Update Sunset Basin riding geometry");
            var dropIns = new Transform[map.DropInCount];
            for (int i = 0; i < dropIns.Length; i++) dropIns[i] = map.DropIn(i);
            map.Configure(map.Terrain, dropIns, SunsetBasinScenery.UpdatePark(map));
            BuildBoard(board, deck);
            EditorUtility.SetDirty(deck);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException("Could not update Sunset Basin riding geometry.");
            AssetDatabase.SaveAssets();
        }

        private static Terrain BuildTerrain(Transform root)
        {
            const int heightResolution = 513;
            const int paintResolution = 256;
            var origin = SunsetBasinLandforms.Origin;
            var size = SunsetBasinLandforms.Size;
            var data = new TerrainData
            {
                name = "Sunset Basin terrain",
                heightmapResolution = heightResolution,
                alphamapResolution = paintResolution,
                baseMapResolution = 512,
                size = size
            };
            Save(data, "Mountain.asset");
            var heights = new float[heightResolution, heightResolution];
            for (int z = 0; z < heightResolution; z++)
            for (int x = 0; x < heightResolution; x++)
                heights[z, x] = SunsetBasinLandforms.Height(origin.x + size.x * x / (heightResolution - 1),
                    origin.z + size.z * z / (heightResolution - 1)) / size.y;
            data.SetHeights(0, 0, heights);
            data.terrainLayers = new[]
            {
                SnowLayer("Cream snow", new Color(0.88f, 0.85f, 0.80f)),
                SnowLayer("Drift shade", new Color(0.68f, 0.71f, 0.73f)),
                SnowLayer("Burnished snow", new Color(0.80f, 0.74f, 0.64f))
            };
            var paint = new float[paintResolution, paintResolution, 3];
            for (int z = 0; z < paintResolution; z++)
            for (int x = 0; x < paintResolution; x++)
            {
                float u = (x + 0.5f) / paintResolution;
                float v = (z + 0.5f) / paintResolution;
                float worldX = origin.x + size.x * u;
                float worldZ = origin.z + size.z * v;
                float shade = Mathf.SmoothStep(0f, 0.69f, (data.GetSteepness(u, v) - 13f) / 31f);
                float grain = Mathf.PerlinNoise(worldX * 0.032f + 40f, worldZ * 0.027f + 44f);
                shade = Mathf.Clamp01(shade + 0.24f * Mathf.SmoothStep(0f, 1f, (grain - 0.48f) / 0.33f));
                float park = SunsetBasinLandforms.Bell(worldX - 134f, 18f)
                    * Mathf.SmoothStep(0f, 1f, (worldZ - 243f) / 52f)
                    * Mathf.SmoothStep(0f, 1f, (590f - worldZ) / 40f);
                float groomed = 0.12f + 0.48f * park;
                paint[z, x, 0] = (1f - shade) * (1f - groomed);
                paint[z, x, 1] = shade;
                paint[z, x, 2] = (1f - shade) * groomed;
            }
            data.SetAlphamaps(0, 0, paint);
            var terrain = Terrain.CreateTerrainGameObject(data).GetComponent<Terrain>();
            terrain.name = "Continuous mountain snow";
            terrain.transform.SetParent(root, false);
            terrain.transform.position = origin;
            terrain.heightmapPixelError = 5f;
            terrain.basemapDistance = 410f;
            terrain.drawInstanced = true;
            terrain.materialTemplate = Save(new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")), "Basin terrain.mat");
            return terrain;
        }

        private static TerrainLayer SnowLayer(string name, Color color)
        {
            const int resolution = 32;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, true);
            var random = new System.Random(153);
            for (int y = 0; y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                float tone = Mathf.Lerp(0.94f, 1.04f, (float)random.NextDouble());
                // Terrain/Lit reads the diffuse texture's alpha as smoothness.
                texture.SetPixel(x, y, new Color(color.r * tone, color.g * tone, color.b * tone, 0.10f));
            }
            texture.Apply();
            Save(texture, name + " grain.asset");
            return Save(new TerrainLayer
            {
                diffuseTexture = texture,
                tileSize = new Vector2(12f, 12f),
                smoothness = 0.10f
            }, name + ".terrainlayer");
        }

        private static Transform Start(Transform root, Terrain terrain, string name, float x, float z)
        {
            var start = new GameObject(name).transform;
            start.SetParent(root, false);
            start.position = Surface(terrain, x, z, 0.17f);
            Vector3 direction = Surface(terrain, x, z + 1.5f) - Surface(terrain, x, z - 1.5f);
            start.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return start;
        }

        internal static Vector3 Surface(Terrain terrain, float x, float z, float clearance = 0f)
        {
            var point = new Vector3(x, 0f, z);
            point.y = terrain.SampleHeight(point) + terrain.transform.position.y + clearance;
            return point;
        }

        private static void BuildBoard(Transform board, Mesh existing = null)
        {
            const int slices = 32;
            var vertices = new Vector3[2 + slices * 2];
            var triangles = new int[slices * 12];
            const float top = -0.28f;
            const float bottom = -0.50f;
            vertices[0] = Vector3.up * top;
            vertices[1] = Vector3.up * bottom;
            for (int i = 0; i < slices; i++)
            {
                float angle = i * (2f * Mathf.PI / slices);
                float z = Mathf.Sign(Mathf.Sin(angle)) * Mathf.Pow(Mathf.Abs(Mathf.Sin(angle)), 0.34f);
                float x = Mathf.Sign(Mathf.Cos(angle)) * Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)), 0.58f);
                float width = Mathf.Lerp(0.31f, 0.40f, Mathf.Abs(z));
                float rise = 0.23f * Mathf.Pow(Mathf.Abs(z), 8f);
                vertices[2 + 2 * i] = new Vector3(x * width, top + rise, z * 0.62f);
                vertices[3 + 2 * i] = new Vector3(x * width, bottom + rise, z * 0.62f);
                int a = 2 + 2 * i;
                int b = 2 + 2 * ((i + 1) % slices);
                int offset = 12 * i;
                triangles[offset] = 0; triangles[offset + 1] = b; triangles[offset + 2] = a;
                triangles[offset + 3] = 1; triangles[offset + 4] = a + 1; triangles[offset + 5] = b + 1;
                triangles[offset + 6] = a; triangles[offset + 7] = b; triangles[offset + 8] = a + 1;
                triangles[offset + 9] = b; triangles[offset + 10] = b + 1; triangles[offset + 11] = a + 1;
            }
            var mesh = existing ? existing : new Mesh { name = "Basin twin-tip board" };
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            if (!existing)
            {
                Save(mesh, "Twin-tip.asset");
                Material("Graphite board", new Color(0.12f, 0.13f, 0.15f));
                ApplySharedDeck(board);
            }
        }

        internal static void ApplySharedDeck(Transform board)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Folder + "/Twin-tip.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Graphite board.mat");
            if (!mesh || !material)
                throw new InvalidOperationException("The shared twin-tip deck assets are missing.");

            var filter = board.GetComponent<MeshFilter>();
            var renderer = board.GetComponent<MeshRenderer>();
            var visual = board.GetComponent<LabRiderVisual>();
            Undo.RecordObjects(new Object[] { filter, renderer, visual }, "Apply the shared twin-tip deck");
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            var rig = new SerializedObject(visual);
            rig.FindProperty("twinTipDeck").boolValue = true;
            rig.ApplyModifiedProperties();
        }

        private static void SetupLighting()
        {
            var camera = GameObject.Find("Lab Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = 3200f;
            var sun = GameObject.Find("Sun").GetComponent<Light>();
            sun.transform.rotation = Quaternion.LookRotation(-SunDirection, Vector3.up);
            sun.color = new Color(1f, 0.78f, 0.55f);
            sun.intensity = 1.43f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            var sky = new Material(Shader.Find("Echo Mountain/Sunset Basin Sky"));
            sky.SetVector("_SunDirection", SunDirection);
            RenderSettings.skybox = Save(sky, "Amber evening.mat");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.76f, 0.57f, 0.48f);
            RenderSettings.fogDensity = 0.00087f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.77f, 0.78f, 0.84f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.46f, 0.48f);
            RenderSettings.ambientGroundColor = new Color(0.37f, 0.33f, 0.33f);
        }

        internal static Material Material(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color, name = name };
            material.SetFloat("_Smoothness", 0.12f);
            return Save(material, name + ".mat");
        }

        internal static T Save<T>(T asset, string file) where T : Object
        {
            AssetDatabase.CreateAsset(asset, Folder + "/" + file);
            return asset;
        }
    }
}
