using UnityEngine;

namespace EchoMountain.Riding
{
    public enum MountainLine { Pine, Summit }

    public static class MountainCourse
    {
        public const float Start = 5f;
        public const float Finish = 865f;
        public const float HalfWidth = 24f;

        public static float LaneX(MountainLine line) => line == MountainLine.Pine ? -9f : 9f;

        public static float Height(float x, float z)
        {
            float downhill = Mathf.Min(z, Finish);
            float runout = Mathf.Max(0f, z - Finish);
            float baseHeight = 105f - 0.105f * downhill + 0.72f * Mathf.Sin(downhill * 0.027f);
            baseHeight -= 0.105f * Mathf.Min(runout, 12f);
            baseHeight += 0.002f * runout * runout;

            float split = Mathf.SmoothStep(0f, 1f, (z - 105f) / 55f)
                * Mathf.SmoothStep(0f, 1f, (750f - z) / 65f);
            float divider = 0.8f * split * Mathf.SmoothStep(1f, 0f, Mathf.Abs(x) / 4f);
            float edge = Mathf.Clamp(Mathf.Abs(x) - 17f, 0f, 7f);
            float summit = Mathf.SmoothStep(0f, 1f, (x - 3f) / 4f);
            float features = Roller(z, 220f, 1.25f) + Roller(z, 408f, 1.55f) + Roller(z, 608f, 1.65f);
            return baseHeight + divider + 0.056f * edge * edge + summit * features;
        }

        public static Vector3 Point(float x, float z, float clearance = 0f) =>
            new(x, Height(x, z) + clearance, z);

        public static Quaternion Heading(float x, float z)
        {
            Vector3 downhill = Point(x, z + 0.25f) - Point(x, z - 0.25f);
            return Quaternion.LookRotation(downhill, Vector3.up);
        }

        private static float Roller(float z, float crest, float height)
        {
            float distance = z - crest;
            float fraction = Mathf.Abs(distance) / (distance < 0f ? 12f : 8f);
            return Mathf.SmoothStep(height, 0f, fraction);
        }
    }
}
