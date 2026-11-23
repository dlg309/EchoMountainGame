using UnityEngine;

namespace EchoMountain.EditorTools
{
    internal static class SunsetBasinLandforms
    {
        internal static readonly Vector3 Origin = new(-300f, 0f, -80f);
        internal static readonly Vector3 Size = new(600f, 260f, 980f);

        // X, lip Z, height, half-width, approach length, landing length.
        private static readonly float[,] lips =
        {
            { 0f, 134f, 2.7f, 16f, 15f, 16f },
            { -22f, 281f, 3.8f, 19f, 18f, 21f },
            { -148f, 213f, 3.4f, 20f, 16f, 13f },
            { -161f, 350f, 4.2f, 24f, 18f, 18f },
            { -127f, 489f, 2.8f, 21f, 17f, 17f },
            { 132f, 294f, 0.95f, 6.5f, 9f, 7f },
            { 132f, 376f, 1.15f, 7f, 11f, 8f },
            { 156f, 396f, 2.6f, 10f, 12f, 14f },
            { 132f, 461f, 1.30f, 8f, 11f, 9f },
            { 133f, 550f, 3.4f, 13f, 16f, 18f },
            { 157f, 569f, 2.3f, 10f, 11f, 19f },
            { -13f, 568f, 2.6f, 23f, 18f, 26f }
        };

        internal static float Height(float x, float z)
        {
            float distance = z + 20f;
            float runout = Mathf.Max(0f, z - 705f);
            float height = 222f - 0.243f * distance + 0.00066f * runout * runout;
            float sideRidge = x + 149f + 15f * Mathf.Sin(z * 0.009f);
            height += 17f * Bell(sideRidge, 74f);
            height += 25f * Bell(x - 237f, 51f) * (0.9f + 0.1f * Mathf.Sin(z * 0.014f));
            height += 12f * Bell(x + 286f, 38f);
            height += 5f * Mathf.Sin(z * 0.026f + x * 0.018f) * (0.25f + 0.75f * Bell(x + 54f, 144f));
            height += 2.7f * Mathf.Sin(x * 0.054f - z * 0.013f) * (1f - Bell(x - 132f, 54f));
            height -= 8.2f * Bell(sideRidge, 36f) * Mathf.SmoothStep(0f, 1f, (z - 219f) / 21f);
            height -= 11f * Bell(x + 160f, 48f) * Mathf.SmoothStep(0f, 1f, (z - 354f) / 25f);
            height += 1.1f * Mathf.PerlinNoise(x * 0.023f + 7f, z * 0.021f + 11f)
                * (1f - 0.87f * Bell(x - 132f, 47f));

            for (int i = 0; i < lips.GetLength(0); i++)
            {
                float offset = z - lips[i, 1];
                float across = Mathf.Abs(x - lips[i, 0]) / lips[i, 3];
                if (across >= 1f || offset < -lips[i, 4] || offset > lips[i, 5]) continue;
                float width = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 1f, across));
                float profile = offset < 0f ? Mathf.Pow(1f + offset / lips[i, 4], 2.2f)
                    : 1f - Mathf.SmoothStep(0f, 1f, offset / lips[i, 5]);
                height += lips[i, 2] * width * profile;
            }
            return height;
        }

        internal static float Bell(float offset, float radius) =>
            Mathf.Exp(-offset * offset / (radius * radius));
    }
}
