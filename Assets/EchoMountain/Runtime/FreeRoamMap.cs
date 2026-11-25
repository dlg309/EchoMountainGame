using System;
using UnityEngine;

namespace EchoMountain.Riding
{
    public sealed class FreeRoamMap : MonoBehaviour
    {
        public const string ScenePath = "Assets/EchoMountain/Scenes/SunsetBasin.unity";
        public const string MainMenuPath = "Assets/EchoMountain/Scenes/GlacierPassPolished.unity";
        internal static string ReturnScene { get; set; }

        [Serializable]
        public struct Rail
        {
            public Vector3 start;
            public Vector3 end;
            public BoxCollider surface;
        }

        [SerializeField] private Terrain terrain;
        [SerializeField] private Transform[] dropIns;
        [SerializeField] private Rail[] rails;

        public int DropInCount => dropIns.Length;
        public Transform DropIn(int index) => dropIns[index];
        public Terrain Terrain => terrain;
        public System.Collections.Generic.IReadOnlyList<Rail> Rails => rails;

        public void Configure(Terrain ground, Transform[] starts, Rail[] features)
        {
            terrain = ground;
            dropIns = starts;
            rails = features;
        }

        internal bool TryGetRail(Collider collider, out Rail rail)
        {
            if (collider)
            {
                foreach (var feature in rails)
                {
                    if (feature.surface != collider) continue;
                    rail = feature;
                    return true;
                }
            }
            rail = default;
            return false;
        }

        internal bool IsOutside(Vector3 position)
        {
            Vector3 local = position - terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            return local.x <= 1f || local.x >= size.x - 1f || local.z <= 1f || local.z >= size.z - 1f;
        }

        internal float SnowHeight(Vector3 position) => terrain.SampleHeight(position) + terrain.transform.position.y;
    }
}
