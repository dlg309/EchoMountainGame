using System;
using UnityEngine;

namespace EchoMountain.Riding
{
    public enum SnowSurface { Packed, Powder, Ice }

    [Serializable]
    public struct SnowSurfaceZone
    {
        public SnowSurface kind;
        public Rect bounds;
        public int priority;
    }

    public readonly struct SurfaceResponse
    {
        public readonly SnowSurface Kind;
        public readonly float Grip, Roll, Brake;

        public SurfaceResponse(SnowSurface kind, float grip, float roll, float brake)
        {
            Kind = kind;
            Grip = grip;
            Roll = roll;
            Brake = brake;
        }
    }

    public static class SurfaceRules
    {
        // Prototype ratios, not measured material properties. Packed preserves the baseline.
        public static SurfaceResponse Response(SnowSurface kind) => kind switch
        {
            SnowSurface.Powder => new(kind, 0.85f, 1.8f, 1.25f),
            SnowSurface.Ice => new(kind, 0.55f, 0.45f, 0.5f),
            _ => new(SnowSurface.Packed, 1f, 1f, 1f)
        };

        public static SurfaceResponse Sample(SnowSurfaceZone[] zones, Vector3 world)
        {
            SnowSurface kind = SnowSurface.Packed;
            int priority = int.MinValue;
            bool found = false;
            if (zones == null || !Finite(world.x) || !Finite(world.y) || !Finite(world.z)) return Response(kind);
            foreach (var zone in zones)
            {
                Rect b = zone.bounds;
                if (!Finite(b.xMin) || !Finite(b.xMax) || !Finite(b.yMin) || !Finite(b.yMax) ||
                    b.width <= 0f || b.height <= 0f || zone.kind < SnowSurface.Packed || zone.kind > SnowSurface.Ice)
                    continue;
                // Half-open bounds; first serialized zone wins equal-priority overlaps.
                if (world.x < b.xMin || world.x >= b.xMax || world.z < b.yMin || world.z >= b.yMax ||
                    (found && zone.priority <= priority)) continue;
                found = true;
                priority = zone.priority;
                kind = zone.kind;
            }
            return Response(kind);
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    // Explicitly assigned to a rider; labs and existing courses remain packed by default.
    // Bounds are authored in world X/Z and do not modify collision geometry.
    public sealed class SnowSurfaceMap : MonoBehaviour
    {
        public SnowSurfaceZone[] zones = Array.Empty<SnowSurfaceZone>();
        public SurfaceResponse Sample(Vector3 world) => SurfaceRules.Sample(zones, world);
    }
}
