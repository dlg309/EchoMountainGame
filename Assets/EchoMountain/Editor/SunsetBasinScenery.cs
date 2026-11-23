using System.Collections.Generic;
using EchoMountain.Riding;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
using Random = System.Random;
using static EchoMountain.EditorTools.SunsetBasinBuilder;

namespace EchoMountain.EditorTools
{
    internal static class SunsetBasinScenery
    {
        internal static FreeRoamMap.Rail[] Build(Transform root, Terrain terrain)
        {
            var random = new Random(629);
            BuildPeaks(root, random);
            BuildFirs(root, terrain, random);
            return BuildPark(root, terrain);
        }

        private static FreeRoamMap.Rail[] BuildPark(Transform root, Terrain ground)
        {
            var park = Group(root, "Switchyard features");
            var steel = Material("Rail graphite", new Color(0.17f, 0.21f, 0.24f));
            var cap = Material("Rail riding surface", new Color(0.51f, 0.60f, 0.64f));
            var wood = Material("Beacon cedar", new Color(0.34f, 0.21f, 0.13f));
            var orange = Material("Beacon ochre", new Color(0.93f, 0.42f, 0.11f));
            var teal = Material("Beacon teal", new Color(0.10f, 0.39f, 0.42f));
            var rails = new[]
            {
                Rail(park, ground, "Low ride-on box", 132f, 300f, 132f, 314f, 0.57f, 0.61f, steel, cap),
                Rail(park, ground, "Long flat rail", 132f, 383f, 132f, 404f, 0.83f, 0.38f, steel, cap),
                Rail(park, ground, "Down rail", 132f, 467f, 127f, 487f, 0.94f, 0.37f, steel, cap)
            };

            var markers = Group(root, "Approach markers");
            foreach (var feature in new[]
            {
                new Vector3(0f, 10f, 126f), new Vector3(-22f, 12f, 273f),
                new Vector3(-148f, 12f, 202f), new Vector3(-161f, 14f, 343f),
                new Vector3(-127f, 12f, 482f), new Vector3(132f, 4.4f, 283f),
                new Vector3(132f, 4.4f, 365f), new Vector3(132f, 4.5f, 450f),
                new Vector3(133f, 8f, 540f), new Vector3(156f, 7f, 387f)
            })
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 point = Surface(ground, feature.x + side * feature.y, feature.z);
                    Block(markers, "Approach pole", point + Vector3.up * 0.70f,
                        new Vector3(0.10f, 1.46f, 0.13f), Quaternion.identity, wood);
                    Material flag = feature.x > 100f && feature.y < 5f ? teal : orange;
                    Block(markers, "Trail pennant", point + new Vector3(side * 0.27f, 1.30f, 0f),
                        new Vector3(0.54f, 0.31f, 0.055f), Quaternion.Euler(0f, 0f, 7f * side), flag);
                }
            }
            return rails;
        }

        private static FreeRoamMap.Rail Rail(Transform root, Terrain terrain, string name, float x, float z,
            float endX, float endZ, float clearance, float halfWidth, Material steel, Material cap)
        {
            var rail = Group(root, name);
            Vector3 start = Surface(terrain, x, z, clearance);
            Vector3 end = Surface(terrain, endX, endZ, clearance);
            // Maintain clearance above the curved snow all the way along a straight rail.
            float raise = 0f;
            for (int i = 1; i < 16; i++)
            {
                Vector3 point = Vector3.Lerp(start, end, i / 16f);
                raise = Mathf.Max(raise, Surface(terrain, point.x, point.z, 0.42f).y - point.y);
            }
            start.y += raise;
            end.y += raise;
            Vector3 axis = end - start;
            var rotation = Quaternion.LookRotation(axis);
            Vector3 mid = (start + end) * 0.5f;
            Block(rail, "Deck sides", mid - Vector3.up * 0.15f,
                new Vector3(halfWidth * 2f, 0.22f, axis.magnitude), rotation, steel);
            Block(rail, "Grind surface", mid - Vector3.up * 0.026f,
                new Vector3(halfWidth * 2f + 0.035f, 0.052f, axis.magnitude), rotation, cap);
            for (int support = 0; support < 4; support++)
            {
                Vector3 point = Vector3.Lerp(start, end, (support + 0.4f) / 3.8f);
                float snow = Surface(terrain, point.x, point.z).y;
                Block(rail, "Rail support", new Vector3(point.x, (point.y + snow) * 0.5f, point.z),
                    new Vector3(0.16f, point.y - snow + 0.12f, 0.19f), Quaternion.identity, steel);
            }
            return new FreeRoamMap.Rail { start = start, end = end, halfWidth = halfWidth + 0.08f };
        }

        private static void BuildFirs(Transform root, Terrain terrain, Random random)
        {
            var forest = Group(root, "Snow-covered fir groves");
            var materials = new[]
            {
                Material("Fir bark", new Color(0.30f, 0.21f, 0.15f)),
                Material("Fir boughs", new Color(0.18f, 0.29f, 0.28f)),
                Material("Fir snow caps", new Color(0.83f, 0.85f, 0.82f))
            };
            var groves = new[]
            {
                new Vector2(-53f, 78f), new Vector2(57f, 116f), new Vector2(-222f, 157f),
                new Vector2(-252f, 283f), new Vector2(-92f, 264f), new Vector2(83f, 253f),
                new Vector2(205f, 207f), new Vector2(200f, 353f), new Vector2(-62f, 409f),
                new Vector2(38f, 452f), new Vector2(216f, 512f), new Vector2(-219f, 547f),
                new Vector2(-178f, 674f), new Vector2(-46f, 671f), new Vector2(68f, 684f),
                new Vector2(185f, 728f), new Vector2(35f, 812f), new Vector2(-251f, 786f),
                new Vector2(-270f, 51f), new Vector2(253f, 78f), new Vector2(265f, 860f)
            };

            for (int index = 0; index < groves.Length; index++)
            {
                Vector2 center = groves[index];
                var batch = new Facets();
                int count = random.Next(8, 18);
                for (int i = 0; i < count; i++)
                {
                    float angle = Range(random, 0f, Mathf.PI * 2f);
                    float radius = Range(random, 4f, 33f);
                    float x = center.x + Mathf.Cos(angle) * radius;
                    float z = center.y + Mathf.Sin(angle) * radius;
                    if (Mathf.Abs(x) > 295f || z > 892f || (z < 585f &&
                        (Mathf.Abs(x - 132f) < 26f || Mathf.Abs(x + 149f) < 25f || Mathf.Abs(x + 4f) < 21f))) continue;
                    float scale = Range(random, 2.9f, 8.9f);
                    Vector3 point = Surface(terrain, x, z, -0.13f);
                    AddFir(batch, point, scale, random);
                    var trunk = Group(forest, "Fir trunk collision");
                    trunk.position = point;
                    var collider = trunk.gameObject.AddComponent<CapsuleCollider>();
                    collider.center = Vector3.up * scale * 0.17f;
                    collider.height = scale * 0.34f;
                    collider.radius = scale * 0.055f;
                }
                AddMesh(forest, "Fir grove " + (index + 1), batch, materials, true);
            }
        }

        private static void AddFir(Facets batch, Vector3 point, float height, Random random)
        {
            const int sides = 7;
            float turn = Range(random, 0f, 6.28f);
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = point + Ring(i, sides, turn, height * 0.045f, 0f);
                Vector3 b = point + Ring(i + 1, sides, turn, height * 0.045f, 0f);
                batch.Quad(0, a, a + Vector3.up * height * 0.34f, b + Vector3.up * height * 0.34f, b);
            }
            for (int layer = 0; layer < 4; layer++)
            {
                float bottom = height * (0.20f + layer * 0.17f);
                float radius = height * (0.23f - layer * 0.043f);
                var tip = point + Vector3.up * (bottom + height * 0.29f);
                var lower = new Vector3[sides];
                var cap = new Vector3[sides];
                for (int side = 0; side < sides; side++)
                {
                    float irregular = Range(random, 0.79f, 1.14f);
                    lower[side] = point + Ring(side, sides, turn, radius * irregular, bottom + Range(random, -0.10f, 0.10f));
                    cap[side] = Vector3.Lerp(lower[side], tip, Range(random, 0.13f, 0.28f));
                }
                for (int side = 0; side < sides; side++)
                {
                    int next = (side + 1) % sides;
                    batch.Quad(1, lower[side], cap[side], cap[next], lower[next]);
                    batch.Triangle(1, lower[side], lower[next], point + Vector3.up * (bottom - 0.08f));
                    batch.Triangle(2, cap[side], tip, cap[next]);
                }
                turn += 0.29f;
            }
        }

        private static void BuildPeaks(Transform root, Random random)
        {
            var ranges = Group(root, "Distant ranges");
            var materials = new[]
            {
                Material("Peak plum rock", new Color(0.39f, 0.34f, 0.39f)),
                Material("Peak lavender snow", new Color(0.63f, 0.62f, 0.69f)),
                Material("Peak illuminated snow", new Color(0.82f, 0.74f, 0.70f))
            };
            var far = new Facets();
            var near = new Facets();
            for (int i = 0; i < 17; i++)
            {
                float angle = i * Mathf.PI * 2f / 17f + Range(random, -0.10f, 0.10f);
                Vector3 direction = new(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                Vector3 point = new Vector3(0f, -64f, 418f) + direction * Range(random, 1000f, 1470f);
                AddPeak(far, point, Range(random, 290f, 505f), Range(random, 195f, 345f), random);
                if (i % 2 == 0)
                {
                    point = new Vector3(0f, -62f, 401f) + direction * Range(random, 720f, 970f);
                    AddPeak(near, point, Range(random, 188f, 309f), Range(random, 140f, 259f), random);
                }
            }
            AddMesh(ranges, "Outer alpine range", far, materials, false);
            AddMesh(ranges, "Fore range", near, materials, false);
        }

        private static void AddPeak(Facets batch, Vector3 position, float radius, float height, Random random)
        {
            const int sides = 13;
            var foot = new Vector3[sides];
            var shoulder = new Vector3[sides];
            var collar = new Vector3[sides];
            Vector3 tip = position + new Vector3(Range(random, -0.25f, 0.25f) * radius, height,
                Range(random, -0.22f, 0.22f) * radius);
            for (int i = 0; i < sides; i++)
            {
                float span = Range(random, 0.76f, 1.23f) * radius;
                foot[i] = position + Ring(i, sides, 0f, span, 0f);
                shoulder[i] = Vector3.Lerp(foot[i], tip, Range(random, 0.30f, 0.61f));
                collar[i] = Vector3.Lerp(shoulder[i], tip, Range(random, 0.27f, 0.64f));
            }
            for (int i = 0; i < sides; i++)
            {
                int next = (i + 1) % sides;
                batch.Quad(0, foot[i], shoulder[i], shoulder[next], foot[next]);
                batch.Quad(i % 3 == 0 ? 0 : 1, shoulder[i], collar[i], collar[next], shoulder[next]);
                batch.Triangle(i % 4 == 2 ? 1 : 2, collar[i], tip, collar[next]);
            }
        }

        private static Vector3 Ring(int index, int sides, float turn, float radius, float y)
        {
            float angle = turn + index * (Mathf.PI * 2f / sides);
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        private static Transform Group(Transform parent, string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static float Range(Random random, float min, float max) =>
            Mathf.Lerp(min, max, (float)random.NextDouble());

        private static void Block(Transform parent, string name, Vector3 position, Vector3 scale,
            Quaternion rotation, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.SetPositionAndRotation(position, rotation);
            block.transform.localScale = scale;
            block.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.isStatic = true;
        }

        private static void AddMesh(Transform parent, string name, Facets facets, Material[] materials, bool shadows)
        {
            var mesh = new Mesh { name = name, subMeshCount = materials.Length };
            mesh.SetVertices(facets.Vertices);
            for (int i = 0; i < materials.Length; i++) mesh.SetTriangles(facets.Triangles[i], i);
            mesh.RecalculateNormals();
            var feature = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            feature.transform.SetParent(parent, false);
            feature.GetComponent<MeshFilter>().sharedMesh = Save(mesh, name + ".asset");
            var renderer = feature.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            if (!shadows) renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private sealed class Facets
        {
            internal readonly List<Vector3> Vertices = new();
            internal readonly List<int>[] Triangles = { new(), new(), new() };

            internal void Triangle(int material, Vector3 a, Vector3 b, Vector3 c)
            {
                var indices = Triangles[material];
                indices.Add(Vertices.Count);
                indices.Add(Vertices.Count + 1);
                indices.Add(Vertices.Count + 2);
                Vertices.Add(a);
                Vertices.Add(b);
                Vertices.Add(c);
            }

            internal void Quad(int material, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Triangle(material, a, b, c);
                Triangle(material, a, c, d);
            }
        }
    }
}
