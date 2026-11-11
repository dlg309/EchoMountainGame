using UnityEngine;

namespace EchoMountain.Riding
{
    public struct RiderVisualPose
    {
        public float edge, hipHeight, lean, tuck, indy, melon;

        public static RiderVisualPose Lerp(RiderVisualPose a, RiderVisualPose b, float t) => new()
        {
            edge = Mathf.Lerp(a.edge, b.edge, t), hipHeight = Mathf.Lerp(a.hipHeight, b.hipHeight, t),
            lean = Mathf.Lerp(a.lean, b.lean, t), tuck = Mathf.Lerp(a.tuck, b.tuck, t),
            indy = Mathf.Lerp(a.indy, b.indy, t), melon = Mathf.Lerp(a.melon, b.melon, t)
        };
    }

    public struct ReplayFrame
    {
        public float time;
        public Vector3 position, velocity;
        public Quaternion rotation;
        public RiderVisualPose pose;
        public int points, landings;
        public BoardGrab grab;
        public bool grounded;
    }

    // Snapshot playback deliberately does not re-simulate nondeterministic physics inputs.
    public sealed class RunRecording
    {
        public const float MaximumSeconds = 180f;
        public const float Interval = 0.05f;
        public const int MaximumFrames = 3602;
        private readonly ReplayFrame[] frames = new ReplayFrame[MaximumFrames];
        private bool sealedClip, exceeded;
        public int Count { get; private set; }
        public float Duration => Count == 0 ? 0f : frames[Count - 1].time;
        public MountainLine Line { get; private set; }
        public bool Practice { get; private set; }
        public string Course { get; private set; }
        public int CourseVersion { get; private set; }
        public int RulesVersion { get; private set; }
        public string Assist { get; private set; }
        public string Build { get; private set; }
        public string Stance => "regular-lab-v1";
        public int Schema => 1;
        public bool CanReplay => sealedClip && !exceeded && Count > 1 && Duration > 0f;

        public void Begin(MountainLine line, bool practice)
        {
            Count = 0;
            sealedClip = exceeded = false;
            Line = line;
            Practice = practice;
            Course = LocalRunStore.Course;
            CourseVersion = LocalRunStore.CourseVersion;
            RulesVersion = LocalRunStore.RulesVersion;
            Assist = LocalRunStore.Assist;
            Build = Application.version;
        }

        public void Add(ReplayFrame frame, bool final = false)
        {
            if (sealedClip || exceeded) return;
            if (!Valid(frame) || frame.time > MaximumSeconds || Count >= MaximumFrames)
            {
                exceeded = true;
                return;
            }
            if (Count > 0)
            {
                float delta = frame.time - frames[Count - 1].time;
                if (delta <= 0f || (!final && delta < Interval - 0.00001f)) return;
            }
            frames[Count++] = frame;
        }

        public void Seal() => sealedClip = true;

        public ReplayFrame Sample(float seconds)
        {
            if (Count == 0) return default;
            if (!float.IsFinite(seconds) || seconds <= 0f) return frames[0];
            if (seconds >= Duration) return frames[Count - 1];
            int low = 0, high = Count - 1;
            while (high - low > 1)
            {
                int middle = (low + high) / 2;
                if (frames[middle].time <= seconds) low = middle;
                else high = middle;
            }
            var a = frames[low];
            var b = frames[high];
            float t = Mathf.InverseLerp(a.time, b.time, seconds);
            var sample = a; // Discrete events retain their recorded timestamp.
            sample.time = seconds;
            sample.position = Vector3.Lerp(a.position, b.position, t);
            sample.rotation = Quaternion.Slerp(a.rotation, b.rotation, t);
            sample.velocity = Vector3.Lerp(a.velocity, b.velocity, t);
            sample.pose = RiderVisualPose.Lerp(a.pose, b.pose, t);
            return sample;
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
        private static bool Valid(ReplayFrame frame)
        {
            var q = frame.rotation;
            float squareLength = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return float.IsFinite(frame.time) && frame.time >= 0f && Finite(frame.position) && Finite(frame.velocity)
                && float.IsFinite(squareLength) && Mathf.Abs(squareLength - 1f) < 0.01f
                && float.IsFinite(frame.pose.edge) && float.IsFinite(frame.pose.hipHeight)
                && float.IsFinite(frame.pose.lean) && float.IsFinite(frame.pose.tuck)
                && float.IsFinite(frame.pose.indy) && float.IsFinite(frame.pose.melon)
                && frame.points >= 0 && frame.landings >= 0;
        }
    }
}
