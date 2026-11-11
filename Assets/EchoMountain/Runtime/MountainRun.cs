using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoMountain.Riding
{
    public enum MountainPhase { Ready, Riding, Results }

    // Read the landing and any awarded trick after the rider's physics update.
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(LabRider), typeof(LabHud))]
    public sealed class MountainRun : MonoBehaviour
    {
        public MountainPhase Phase { get; private set; }
        public MountainLine Line { get; private set; }
        public float Elapsed { get; private set; }
        public float PeakSpeed { get; private set; }
        public const float PracticeStart = 155f;
        public const float PracticeFinish = 285f;
        public const int PracticeGoal = 250;
        public bool IsPractice { get; private set; }
        public bool SoundEnabled { get; private set; } = true;
        public TrickScore Tricks { get; } = new();
        public float StartZ => IsPractice ? PracticeStart : MountainCourse.Start;
        public float FinishZ => IsPractice ? PracticeFinish : MountainCourse.Finish;
        public float Progress => Mathf.InverseLerp(StartZ, FinishZ, body.position.z);

        private Rigidbody body;
        private LabRider rider;
        private LabHud hud;
        private uint resetCount;
        private InputAction advance, choose, left, right, practice;
        private GUIStyle headline, copy, eyebrow, button;
        private readonly Color ink = new(0.92f, 0.96f, 1f);
        private readonly Color ice = new(0.40f, 0.87f, 0.93f);
        private readonly Color orange = new(1f, 0.65f, 0.29f);
        private readonly Color panel = new(0.025f, 0.065f, 0.10f, 0.96f);

        private void Awake()
        {
            rider = GetComponent<LabRider>();
            body = GetComponent<Rigidbody>();
            hud = GetComponent<LabHud>();
            advance = Button("Start or retry", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            choose = Button("Choose a line", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            left = Button("Pine line", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left");
            right = Button("Summit line", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right");
            practice = Button("Practice jumps", "<Keyboard>/p", "<Gamepad>/buttonNorth");
        }

        private void OnEnable()
        {
            advance.Enable();
            choose.Enable();
            left.Enable();
            right.Enable();
            practice.Enable();
        }

        private void Start()
        {
            hud.ShowDiagnostics = false;
            hud.ShowTricks = true;
            body.maxAngularVelocity = Mathf.Max(body.maxAngularVelocity, 10f);
            rider.EnableTricks(Tricks);
            gameObject.AddComponent<MountainRideAudio>();
            ShowStart();
        }

        private void OnDisable()
        {
            advance.Disable();
            choose.Disable();
            left.Disable();
            right.Disable();
            practice.Disable();
        }

        private void OnDestroy()
        {
            advance.Dispose();
            choose.Dispose();
            left.Dispose();
            right.Dispose();
            practice.Dispose();
        }

        private static InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = new InputAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            return action;
        }

        private void Update()
        {
            if (Phase == MountainPhase.Riding) return;
            if (Phase == MountainPhase.Ready)
            {
                if (left.WasPressedThisFrame()) SelectLine(MountainLine.Pine);
                if (right.WasPressedThisFrame()) SelectLine(MountainLine.Summit);
                if (practice.WasPressedThisFrame())
                {
                    StartPractice();
                    return;
                }
            }
            if (Phase == MountainPhase.Results && choose.WasPressedThisFrame())
                ShowStart();
            else if (advance.WasPressedThisFrame())
                StartRun();
        }

        private void FixedUpdate()
        {
            if (Phase != MountainPhase.Riding) return;

            Vector3 position = body.position;
            if (Mathf.Abs(position.x) > MountainCourse.HalfWidth - 0.25f || position.z < StartZ - 11f || position.y < 4f)
                rider.ResetRider();

            if (resetCount != rider.ResetCount)
            {
                resetCount = rider.ResetCount;
                Elapsed = 0f;
                PeakSpeed = 0f;
                return;
            }

            Elapsed += Time.fixedDeltaTime;
            PeakSpeed = Mathf.Max(PeakSpeed, rider.SpeedKmh);
            if (position.z >= FinishZ && rider.IsGrounded && !rider.IsRecovering)
            {
                Tricks.CancelAir();
                Phase = MountainPhase.Results;
                SetRiding(false);
            }
        }

        public void SelectLine(MountainLine line)
        {
            if (Phase != MountainPhase.Ready) return;
            Line = line;
            float x = MountainCourse.LaneX(line);
            body.position = MountainCourse.Point(x, StartZ, 0.15f);
            body.rotation = MountainCourse.Heading(x, StartZ);
        }

        public void StartRun()
        {
            if (Phase == MountainPhase.Riding) return;
            Phase = MountainPhase.Riding;
            Elapsed = 0f;
            PeakSpeed = 0f;
            SetRiding(true);
            float x = MountainCourse.LaneX(Line);
            rider.SetSpawn(MountainCourse.Point(x, StartZ, 0.15f),
                MountainCourse.Heading(x, StartZ));
            resetCount = rider.ResetCount;
        }

        public void StartPractice()
        {
            if (Phase != MountainPhase.Ready) return;
            IsPractice = true;
            Line = MountainLine.Summit;
            StartRun();
        }

        public void ShowStart()
        {
            IsPractice = false;
            Phase = MountainPhase.Ready;
            SetRiding(false);
            SelectLine(Line);
        }

        private void SetRiding(bool active)
        {
            hud.enabled = active;
            if (active)
            {
                body.isKinematic = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
            else
            {
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.isKinematic = true;
            }
            rider.enabled = active;
        }

        private void OnGUI()
        {
            if (headline == null)
            {
                headline = Style(44, ink, FontStyle.Bold);
                eyebrow = Style(15, ice, FontStyle.Bold);
                copy = Style(19, ink, FontStyle.Normal);
                button = new GUIStyle(GUI.skin.button) { fontSize = 17, fontStyle = FontStyle.Bold };
            }

            Matrix4x4 matrix = GUI.matrix;
            Color color = GUI.color;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            float height = Screen.height / scale;

            if (Phase == MountainPhase.Riding)
            {
                Panel(new Rect(18f, 18f, 326f, 107f), LineColor);
                GUI.Label(new Rect(36f, 28f, 293f, 25f), IsPractice ? "SUMMIT   /   JUMP PRACTICE" : $"GLACIER PASS   /   {LineName}", eyebrow);
                GUI.Label(new Rect(36f, 52f, 293f, 31f), $"{TimeText(Elapsed)}        {Progress:P0} complete", copy);
                Fill(new Rect(38f, 98f, 286f, 5f), new Color(0.18f, 0.26f, 0.32f));
                Fill(new Rect(38f, 98f, 286f * Progress, 5f), LineColor);
                DrawScore(new Rect(width - 344f, 18f, 326f, 148f));
            }
            else
            {
                float x = (width - 644f) * 0.5f;
                float y = (height - 366f) * 0.5f;
                Panel(new Rect(x, y, 644f, 366f), LineColor);
                GUI.Label(new Rect(x + 29f, y + 20f, 580f, 27f), "ECHO MOUNTAIN    /    COURSE 01", eyebrow);
                GUI.Label(new Rect(x + 27f, y + 48f, 590f, 70f),
                    Phase == MountainPhase.Ready ? "Glacier Pass" : IsPractice ? "Practice complete" : "Descent complete", headline);
                if (Phase == MountainPhase.Ready)
                {
                    GUI.Label(new Rect(x + 29f, y + 125f, 580f, 30f), "Two lines. One finish. Choose your drop-in.", copy);
                    DrawChoice(new Rect(x + 29f, y + 178f, 281f, 54f), MountainLine.Pine, "PINE LINE   /   Smooth");
                    DrawChoice(new Rect(x + 334f, y + 178f, 281f, 54f), MountainLine.Summit, "SUMMIT LINE   /   Jumps");
                    GUI.Label(new Rect(x + 29f, y + 246f, 440f, 30f), "Land your spins and grabs to bank points.", copy);
                    SoundButton(new Rect(x + 489f, y + 247f, 126f, 28f));
                    if (GUI.Button(new Rect(x + 29f, y + 299f, 198f, 44f), "Drop in  [Enter / A]", button)) StartRun();
                    if (GUI.Button(new Rect(x + 243f, y + 299f, 254f, 44f), "Jump practice  [P / Y]", button)) StartPractice();
                    if (GUI.Button(new Rect(x + 513f, y + 299f, 102f, 44f), "Quit", button)) Application.Quit();
                }
                else
                {
                    GUI.Label(new Rect(x + 29f, y + 132f, 580f, 33f), $"{LineName}    •    {TimeText(Elapsed)}", copy);
                    GUI.Label(new Rect(x + 29f, y + 179f, 580f, 33f), $"Top speed    {PeakSpeed:F0} km/h", copy);
                    string goal = IsPractice ? $"   /   Target {PracticeGoal:N0}" : "";
                    GUI.Label(new Rect(x + 29f, y + 223f, 580f, 36f), $"{Tricks.Points:N0} pts{goal}", copy);
                    if (GUI.Button(new Rect(x + 29f, y + 299f, 281f, 44f), "Retry  [Enter / A]", button)) StartRun();
                    if (GUI.Button(new Rect(x + 334f, y + 299f, 281f, 44f), "Choose line  [Esc / B]", button)) ShowStart();
                }
            }

            GUI.color = color;
            GUI.matrix = matrix;
        }

        private void DrawScore(Rect rect)
        {
            Panel(rect, LineColor);
            GUI.Label(new Rect(rect.x + 17f, rect.y + 10f, 175f, 24f), "TRICK SCORE", eyebrow);
            if (GUI.Button(new Rect(rect.x + 223f, rect.y + 11f, 86f, 29f), hud.IsPaused ? "Resume" : "Pause"))
                hud.SetPaused(!hud.IsPaused);
            string chain = Tricks.Chain > 0 ? $"      ×{Tricks.Multiplier:0.00}  {Tricks.ChainTime:0.0}s" : "";
            string target = IsPractice ? $" / {PracticeGoal:N0}" : "";
            GUI.Label(new Rect(rect.x + 17f, rect.y + 39f, 291f, 30f), $"{Tricks.Points:N0}{target} pts{chain}", copy);

            string feedback;
            if (Tricks.IsAirborne && (Tricks.CurrentGrab != BoardGrab.None || Mathf.Abs(Tricks.Rotation) >= 15f))
            {
                string grab = Tricks.CurrentGrab == BoardGrab.None ? "" : $" {Tricks.CurrentGrab}";
                feedback = $"{Mathf.Abs(Tricks.Rotation):F0}°{grab}   /   {Tricks.PendingPoints} pending";
            }
            else
            {
                feedback = Tricks.FeedbackTime > 0f ? Tricks.Feedback : "Spin or grab, then land.";
            }
            GUI.Label(new Rect(rect.x + 17f, rect.y + 78f, 295f, 32f), feedback, copy);
            SoundButton(new Rect(rect.x + 215f, rect.y + 110f, 94f, 27f));
        }

        private void SoundButton(Rect rect)
        {
            if (GUI.Button(rect, SoundEnabled ? "Sound on" : "Sound off")) SoundEnabled = !SoundEnabled;
        }

        private string LineName => Line == MountainLine.Pine ? "PINE LINE" : "SUMMIT LINE";
        private Color LineColor => Line == MountainLine.Pine ? ice : orange;
        private static string TimeText(float seconds)
        {
            int centiseconds = Mathf.FloorToInt(seconds * 100f);
            return $"{centiseconds / 6000:00}:{centiseconds / 100 % 60:00}.{centiseconds % 100:00}";
        }

        private void DrawChoice(Rect rect, MountainLine line, string text)
        {
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = Line == line ? LineColor : Color.white;
            if (GUI.Button(rect, text, button)) SelectLine(line);
            GUI.backgroundColor = previous;
        }

        private void Panel(Rect rect, Color accent)
        {
            Fill(rect, panel);
            Fill(new Rect(rect.x, rect.y, rect.width, 4f), accent);
        }

        private static void Fill(Rect rect, Color tint)
        {
            GUI.color = tint;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static GUIStyle Style(int size, Color color, FontStyle weight)
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight };
            style.normal.textColor = color;
            return style;
        }
    }
}
