using UnityEngine;

namespace EchoMountain.Riding
{
    // Temporary in-game lab interface; rendering is independent of physics ownership.
    [RequireComponent(typeof(LabRider))]
    public sealed class LabHud : MonoBehaviour
    {
        private LabRider rider;
        private MountainRun run;
        private GUIStyle title, text, large, small, button;
        private readonly Color panel = new Color(0.025f, 0.065f, 0.10f, 0.96f);
        private readonly Color accent = new Color(0.40f, 0.87f, 0.93f, 1f);
        private readonly Color ink = new Color(0.90f, 0.96f, 0.98f, 1f);
        private readonly Color muted = new Color(0.67f, 0.78f, 0.82f, 1f);
        private readonly Color orange = new Color(1f, 0.57f, 0.20f, 1f);

        private float resumeTimeScale;
        private string pauseReason = "";
        internal bool IsPaused { get; private set; }
        internal bool ShowDiagnostics { get; set; } = true;
        internal bool ShowTricks { get; set; }

        private void Awake()
        {
            rider = GetComponent<LabRider>();
            TryGetComponent(out run);
        }

        private void OnDisable() => SetPaused(false);

        internal void PauseForInputLoss(string reason)
        {
            SetPaused(true);
            if (IsPaused) pauseReason = reason;
        }

        internal void SetPaused(bool paused)
        {
            if (paused == IsPaused || (paused && Time.timeScale <= 0f)) return;
            IsPaused = paused;
            pauseReason = "";
            if (paused)
            {
                resumeTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = resumeTimeScale;
            }
        }

        private void SetupStyles()
        {
            if (title != null) return;
            title = NewStyle(13, FontStyle.Bold, accent);
            text = NewStyle(16, FontStyle.Normal, ink);
            large = NewStyle(34, FontStyle.Bold, ink);
            small = NewStyle(12, FontStyle.Normal, muted);
            small.wordWrap = true;
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
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

            float trickOffset = ShowTricks ? 70f : 0f;
            float controlTop = height - 176f - trickOffset;
            RideLabel(new Rect(34f, controlTop + 9f, 240f, 22f), "RIDE CONTROLS", title);
            bool pad = rider.UsingGamepad;
            RideLabel(new Rect(34f, controlTop + 34f, 240f, 24f), pad ? "LEFT STICK    Steer" : "A / D     Steer", text);
            RideLabel(new Rect(34f, controlTop + 57f, 240f, 24f), pad ? "LEFT TRIGGER    Brake" : "S             Brake", text);
            RideLabel(new Rect(34f, controlTop + 80f, 240f, 24f), pad ? "SOUTH / A    Pop" : "SPACE   Pop", text);
            RideLabel(new Rect(34f, controlTop + 103f, 240f, 24f), pad ? "EAST / B    Reset" : "R             Reset", text);
            RideLabel(new Rect(34f, controlTop + 126f, 240f, 24f), pad ? "START    Pause" : "ESC         Pause", text);
            if (ShowTricks)
            {
                RideLabel(new Rect(34f, controlTop + 149f, 300f, 24f), pad ? "RIGHT STICK LEFT / RIGHT    Spin" : "Z / C       Spin in the air", text);
                RideLabel(new Rect(34f, controlTop + 172f, 300f, 24f), pad ? "RIGHT STICK UP / DOWN    Flip" : "W / X      Frontflip / Backflip", text);
                RideLabel(new Rect(34f, controlTop + 195f, 240f, 24f), pad ? "RB / LB    Indy / Melon" : "Q / E      Indy / Melon", text);
            }

            if (ShowDiagnostics)
            {
                float right = width - 270f;
                RideLabel(new Rect(right + 16f, 28f, 220f, 22f), "PHYSICS  /  LAB", title);
                RideLabel(new Rect(right + 16f, 53f, 220f, 22f), rider.IsRecovering ? "CONTACT    Recovering" : rider.IsGrounded ? "CONTACT    Grounded" : "CONTACT    Airborne", text);
                RideLabel(new Rect(right + 16f, 77f, 220f, 22f), $"SLIP             {rider.LateralSlip:F1} m/s", text);
                RideLabel(new Rect(right + 16f, 101f, 220f, 22f), $"EDGE           {rider.SteerInput:F2}", text);
                RideLabel(new Rect(right + 16f, 125f, 220f, 22f), $"GRIP             {rider.GripAcceleration:F1} m/s²", text);
                RideLabel(new Rect(right + 16f, 149f, 220f, 22f), $"NORMAL      {rider.ContactNormal.x:F1}, {rider.ContactNormal.y:F1}, {rider.ContactNormal.z:F1}", small);
                RideLabel(new Rect(right + 16f, 171f, 220f, 22f), $"LANDING     {rider.LandingText}", small);
            }

            float gaugeX = width - 270f;
            float gaugeY = height - 184f;
            RideLabel(new Rect(gaugeX + 16f, gaugeY + 9f, 220f, 20f), "SPEED", title);
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
            RideLabel(new Rect(gaugeX + 85f, gaugeY + 117f, 110f, 42f), $"{rider.SpeedKmh:F0}", large);
            RideLabel(new Rect(gaugeX + 154f, gaugeY + 133f, 55f, 20f), "km/h", small);

            if (IsPaused)
            {
                float x = (width - 440f) * 0.5f;
                bool isFreeRoam = run && run.IsFreeRoam;
                float panelHeight = isFreeRoam ? 283f + 40f * run.DropInCount : run ? 256f : 204f;
                float y = (height - panelHeight) * 0.5f;
                DrawPanel(new Rect(x, y, 440f, panelHeight));
                GUI.Label(new Rect(x + 20f, y + 12f, 400f, 28f), "ECHO MOUNTAIN   /   PAUSED", title);
                GUI.Label(new Rect(x + 20f, y + 43f, 400f, 40f), pauseReason, small);
                if (GUI.Button(new Rect(x + 20f, y + 91f, 400f, 36f), "Resume", button))
                    SetPaused(false);
                if (GUI.Button(new Rect(x + 20f, y + 137f, 400f, 36f),
                    isFreeRoam ? "Reset to Drop-in" : run ? "Restart Run" : "Reset run", button))
                {
                    SetPaused(false);
                    if (run) run.RestartRun();
                    else rider.ResetRider();
                }
                if (run && GUI.Button(new Rect(x + 20f, y + 183f, 400f, 36f), "Return to Main Menu", button))
                {
                    SetPaused(false);
                    run.ShowStart();
                }
                if (isFreeRoam)
                {
                    GUI.Label(new Rect(x + 20f, y + 227f, 400f, 22f), "CHANGE DROP-IN", title);
                    for (int i = 0; i < run.DropInCount; i++)
                    {
                        if (!GUI.Button(new Rect(x + 20f, y + 253f + 40f * i, 400f, 34f), run.DropInName(i), button))
                            continue;
                        run.MoveToDropIn(i);
                        SetPaused(false);
                    }
                }
            }

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

        internal static void RideLabel(Rect rect, string value, GUIStyle style)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color previous = GUI.contentColor;
            GUI.contentColor = Color.black;
            for (int offset = -1; offset <= 1; offset += 2)
            {
                GUI.Label(new Rect(rect.x + offset, rect.y, rect.width, rect.height), value, style);
                GUI.Label(new Rect(rect.x, rect.y + offset, rect.width, rect.height), value, style);
            }
            GUI.contentColor = previous;
            GUI.Label(rect, value, style);
        }

        private static void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            Matrix4x4 matrix = GUI.matrix;
            Color old = GUI.color;
            Vector2 direction = end - start;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, start);
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(start.x - 1f, start.y - thickness * 0.5f - 1f,
                direction.magnitude + 2f, thickness + 2f), Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(start.x, start.y - thickness * 0.5f, direction.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = old;
        }
    }
}
