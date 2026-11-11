using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoMountain.Riding
{
    [RequireComponent(typeof(MountainRun))]
    public sealed class MountainReplay : MonoBehaviour
    {
        public RunRecording Clip { get; } = new();
        public bool Active { get; private set; }
        public bool Playing { get; private set; }
        public float Cursor { get; private set; }
        public float Speed { get; private set; } = 1f;
        public bool HideOverlay { get; private set; }
        private Rigidbody body;
        private LabRider rider;
        private LabRiderVisual visual;
        private MountainRun run;
        private LabCamera followCamera;
        private Collider boardCollider;
        private bool colliderWasEnabled;
        private Vector3 resultPosition;
        private Quaternion resultRotation;
        private RiderVisualPose resultPose;
        private InputAction pause, back, hide, seekLeft, seekRight;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            rider = GetComponent<LabRider>();
            visual = GetComponent<LabRiderVisual>();
            run = GetComponent<MountainRun>();
            boardCollider = GetComponent<Collider>();
            followCamera = FindFirstObjectByType<LabCamera>();
            pause = Action("Replay play or pause", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            back = Action("Exit replay", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            hide = Action("Hide replay overlay", "<Keyboard>/h", "<Gamepad>/buttonNorth");
            seekLeft = Action("Replay back five seconds", "<Keyboard>/leftArrow", "<Gamepad>/dpad/left");
            seekRight = Action("Replay forward five seconds", "<Keyboard>/rightArrow", "<Gamepad>/dpad/right");
        }

        private static InputAction Action(string name, string keyboard, string gamepad)
        {
            var action = new InputAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            action.Enable();
            return action;
        }

        public void BeginCapture()
        {
            Clip.Begin(run.Line, run.IsPractice);
            Capture(0f);
        }

        public void Capture(float time, bool final = false) => Clip.Add(new ReplayFrame
        {
            time = time, position = body.position, rotation = body.rotation, velocity = body.linearVelocity,
            pose = visual ? visual.CapturePose() : default, points = run.Tricks.Points,
            grounded = rider.IsGrounded, grab = rider.ActiveGrab, landings = rider.LandingCount
        }, final);

        public bool BeginPlayback()
        {
            if (!Clip.CanReplay || Active) return false;
            resultPosition = body.position;
            resultRotation = body.rotation;
            resultPose = visual ? visual.CapturePose() : default;
            colliderWasEnabled = boardCollider.enabled;
            boardCollider.enabled = false;
            Active = Playing = true;
            HideOverlay = false;
            Speed = 1f;
            Scrub(0f);
            return true;
        }

        public void EndPlayback()
        {
            if (!Active) return;
            Active = Playing = false;
            SetPose(resultPosition, resultRotation);
            boardCollider.enabled = colliderWasEnabled;
            if (visual)
            {
                visual.ApplyReplayPose(resultPose);
                visual.ClearReplayPose();
            }
            if (followCamera)
            {
                followCamera.ClearReplayTravel();
                followCamera.SnapNextFrame();
            }
        }

        public void SetPlaying(bool playing)
        {
            if (!Active) return;
            if (playing && Cursor >= Clip.Duration) Scrub(0f);
            Playing = playing;
        }

        public void SetSpeed(float speed)
        {
            if (float.IsFinite(speed)) Speed = Mathf.Clamp(speed, 0.25f, 2f);
        }

        public void Scrub(float seconds)
        {
            if (!Active || !float.IsFinite(seconds)) return;
            Cursor = Mathf.Clamp(seconds, 0f, Clip.Duration);
            Apply();
            if (followCamera) followCamera.SnapNextFrame();
        }

        private void Apply()
        {
            var frame = Clip.Sample(Cursor);
            SetPose(frame.position, frame.rotation);
            if (visual) visual.ApplyReplayPose(frame.pose);
            if (followCamera) followCamera.SetReplayTravel(frame.velocity);
        }

        private void SetPose(Vector3 position, Quaternion rotation)
        {
            body.position = position;
            body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
        }

        private void Update()
        {
            if (!Active) return;
            if (back.WasPressedThisFrame()) { run.EndReplay(); return; }
            if (pause.WasPressedThisFrame()) SetPlaying(!Playing);
            if (hide.WasPressedThisFrame()) HideOverlay = !HideOverlay;
            if (seekLeft.WasPressedThisFrame()) Scrub(Cursor - 5f);
            if (seekRight.WasPressedThisFrame()) Scrub(Cursor + 5f);
            if (!Playing) return;
            Cursor = Mathf.Min(Clip.Duration, Cursor + Time.unscaledDeltaTime * Speed);
            Apply();
            if (Cursor >= Clip.Duration) Playing = false;
        }

        private void OnGUI()
        {
            if (!Active || HideOverlay) return;
            var matrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float x = (Screen.width / scale - 780f) * 0.5f;
            float y = Screen.height / scale - 134f;
            GUI.Box(new Rect(x, y, 780f, 116f), "");
            GUI.Label(new Rect(x + 18f, y + 9f, 744f, 25f),
                $"RUN REPLAY   /   {Cursor:0.0} / {Clip.Duration:0.0}s   /   {Speed:0.00}x   /   {Clip.Sample(Cursor).points:N0} pts");
            float seek = GUI.HorizontalSlider(new Rect(x + 20f, y + 42f, 740f, 22f), Cursor, 0f, Clip.Duration);
            if (!Mathf.Approximately(seek, Cursor)) { SetPlaying(false); Scrub(seek); }
            if (GUI.Button(new Rect(x + 20f, y + 74f, 130f, 29f), Playing ? "Pause [Space / A]" : "Play [Space / A]")) SetPlaying(!Playing);
            if (GUI.Button(new Rect(x + 160f, y + 74f, 62f, 29f), "0.5x")) SetSpeed(0.5f);
            if (GUI.Button(new Rect(x + 229f, y + 74f, 62f, 29f), "1x")) SetSpeed(1f);
            if (GUI.Button(new Rect(x + 298f, y + 74f, 62f, 29f), "2x")) SetSpeed(2f);
            if (GUI.Button(new Rect(x + 378f, y + 74f, 170f, 29f), "Hide overlay [H / Y]")) HideOverlay = true;
            if (GUI.Button(new Rect(x + 566f, y + 74f, 194f, 29f), "Back to results [Esc / B]")) run.EndReplay();
            GUI.matrix = matrix;
        }

        private void OnDisable() => EndPlayback();

        private void OnDestroy()
        {
            pause?.Dispose(); back?.Dispose(); hide?.Dispose();
            seekLeft?.Dispose(); seekRight?.Dispose();
        }
    }
}
