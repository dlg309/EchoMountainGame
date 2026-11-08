using UnityEngine;

namespace EchoMountain.Riding
{
    // Temporary in-game lab interface; rendering is independent of physics ownership.
    [RequireComponent(typeof(LabRider))]
    public sealed class LabHud : MonoBehaviour
    {
        private LabRider rider;
        private GUIStyle title, text, large, small;
        private readonly Color panel = new Color(0.04f, 0.09f, 0.14f, 0.90f);
        private readonly Color accent = new Color(0.35f, 0.84f, 0.91f, 1f);
        private readonly Color ink = new Color(0.90f, 0.96f, 0.98f, 1f);
        private readonly Color muted = new Color(0.67f, 0.78f, 0.82f, 1f);
        private readonly Color orange = new Color(1f, 0.57f, 0.20f, 1f);

        private void Awake() => rider = GetComponent<LabRider>();

        private void SetupStyles()
        {
            if (title != null) return;
            title = NewStyle(13, FontStyle.Bold, accent);
            text = NewStyle(16, FontStyle.Normal, ink);
            large = NewStyle(34, FontStyle.Bold, ink);
            small = NewStyle(12, FontStyle.Normal, muted);
        }

        private static GUIStyle NewStyle(int size, FontStyle weight, Color color)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, richText = false };
            style.normal.textColor = color;
            return style;
        }

        private void OnGUI()
        {
            if (!rider) return;
            SetupStyles();
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.65f, 1.4f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            DrawPanel(new Rect(18f, height - 153f, 275f, 135f));
            GUI.Label(new Rect(34f, height - 144f, 240f, 22f), "RIDE CONTROLS", title);
            GUI.Label(new Rect(34f, height - 119f, 240f, 24f), "A / D     Steer", text);
            GUI.Label(new Rect(34f, height - 96f, 240f, 24f), "S             Brake", text);
            GUI.Label(new Rect(34f, height - 73f, 240f, 24f), "SPACE   Pop", text);
            GUI.Label(new Rect(34f, height - 50f, 240f, 24f), "R             Reset", text);

            float right = width - 270f;
            DrawPanel(new Rect(right, 18f, 252f, 188f));
            GUI.Label(new Rect(right + 16f, 28f, 220f, 22f), "PHYSICS  /  LAB", title);
            GUI.Label(new Rect(right + 16f, 53f, 220f, 22f), rider.IsGrounded ? "CONTACT    Grounded" : "CONTACT    Airborne", text);
            GUI.Label(new Rect(right + 16f, 77f, 220f, 22f), $"SLIP             {rider.LateralSlip:F1} m/s", text);
            GUI.Label(new Rect(right + 16f, 101f, 220f, 22f), $"EDGE           {rider.SteerInput:F2}", text);
            GUI.Label(new Rect(right + 16f, 125f, 220f, 22f), $"GRIP             {rider.GripAcceleration:F1} m/s²", text);
            GUI.Label(new Rect(right + 16f, 149f, 220f, 22f), $"NORMAL      {rider.ContactNormal.x:F1}, {rider.ContactNormal.y:F1}, {rider.ContactNormal.z:F1}", small);
            GUI.Label(new Rect(right + 16f, 171f, 220f, 22f), $"LANDING     {rider.LandingText}", small);

            float gaugeX = width - 270f;
            float gaugeY = height - 184f;
            DrawPanel(new Rect(gaugeX, gaugeY, 252f, 166f));
            GUI.Label(new Rect(gaugeX + 16f, gaugeY + 9f, 220f, 20f), "SPEED", title);
            Vector2 centre = new Vector2(gaugeX + 126f, gaugeY + 114f);
            for (int i = 0; i <= 10; i++)
            {
                float angle = Mathf.PI - Mathf.PI * i / 10f;
                var direction = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
                DrawLine(centre + direction * 57f, centre + direction * (i % 5 == 0 ? 68f : 64f), 2f, muted);
            }
            float needleAngle = Mathf.PI - Mathf.PI * Mathf.Clamp01(rider.SpeedKmh / 120f);
            var needleDirection = new Vector2(Mathf.Cos(needleAngle), -Mathf.Sin(needleAngle));
            DrawLine(centre, centre + needleDirection * 51f, 3f, orange);
            GUI.Label(new Rect(gaugeX + 85f, gaugeY + 117f, 110f, 42f), $"{rider.SpeedKmh:F0}", large);
            GUI.Label(new Rect(gaugeX + 154f, gaugeY + 133f, 55f, 20f), "km/h", small);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawPanel(Rect rect)
        {
            GUI.color = panel;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = accent;
            GUI.DrawTexture(new Rect(rect.x, rect.y, 3f, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            Color old = GUI.color;
            Vector2 direction = end - start;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, direction.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = old;
        }
    }
}
