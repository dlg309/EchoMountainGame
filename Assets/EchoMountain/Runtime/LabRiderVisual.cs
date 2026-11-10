using UnityEngine;

namespace EchoMountain.Riding
{
    // A board-mounted pose rig for the lab. All parts are visual and share the board's interpolated pose.
    [RequireComponent(typeof(LabRider))]
    public sealed class LabRiderVisual : MonoBehaviour
    {
        private const float FootSpread = 0.31f;
        private const float SegmentLength = 0.43f;
        [SerializeField] private Material paletteSource;
        private readonly Transform[] thighs = new Transform[2];
        private readonly Transform[] shins = new Transform[2];
        private readonly Transform[] sleeves = new Transform[2];
        private readonly Transform[] forearms = new Transform[2];
        private readonly Transform[] gloves = new Transform[2];
        private Transform rig, jacket, helmet;
        private Material trousers, fabric, trim, shell, visor;
        private LabRider rider;
        private uint lastReset;
        private float hipHeight = 0.91f;
        private float lean;
        private float tuck;
        private readonly float[] grabBlend = new float[2];
        private bool replaying;
        private float replayEdge;

        public RiderVisualPose CapturePose() => new()
        {
            edge = replaying ? replayEdge : rider.SteerInput, hipHeight = hipHeight,
            lean = lean, tuck = tuck, indy = grabBlend[0], melon = grabBlend[1]
        };

        public void ApplyReplayPose(RiderVisualPose pose)
        {
            replaying = true;
            replayEdge = pose.edge;
            hipHeight = pose.hipHeight;
            lean = pose.lean;
            tuck = pose.tuck;
            grabBlend[0] = pose.indy;
            grabBlend[1] = pose.melon;
            if (rig) Pose(0f);
        }

        public void ClearReplayPose() => replaying = false;

        public void SetPalette(Material source) => paletteSource = source;

        private void Awake()
        {
            if (!paletteSource)
            {
                enabled = false;
                return;
            }

            rider = GetComponent<LabRider>();
            rig = new GameObject("Segmented rider").transform;
            rig.SetParent(transform.parent, false);
            trousers = MakeMaterial("Slate trousers", new Color(0.10f, 0.19f, 0.25f));
            fabric = MakeMaterial("Alpine coral", new Color(0.96f, 0.25f, 0.09f));
            trim = MakeMaterial("Ink", new Color(0.03f, 0.06f, 0.08f));
            shell = MakeMaterial("Pearl helmet", new Color(0.89f, 0.93f, 0.92f));
            visor = MakeMaterial("Glacier visor", new Color(0.12f, 0.67f, 0.82f));
            jacket = MakePart(PrimitiveType.Capsule, "Jacket", fabric, Vector3.zero, new Vector3(0.26f, 0.28f, 0.48f));
            helmet = MakePart(PrimitiveType.Sphere, "Helmet", shell, Vector3.zero, new Vector3(0.25f, 0.27f, 0.26f));
            var goggles = MakePart(PrimitiveType.Cube, "Goggles", visor, Vector3.zero, Vector3.one);
            goggles.SetParent(helmet, false);
            goggles.localPosition = new Vector3(0.48f, -0.04f, 0f);
            goggles.localScale = new Vector3(0.24f, 0.35f, 0.8f);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                string label = i == 0 ? "Rear " : "Front ";
                var bindingPosition = new Vector3(0f, 0.125f, side * FootSpread);
                MakePart(PrimitiveType.Cube, label + "binding", trim, bindingPosition, new Vector3(0.34f, 0.05f, 0.2f));
                var boot = MakePart(PrimitiveType.Cube, label + "boot", trim,
                    bindingPosition + new Vector3(0.025f, 0.08f, 0f), new Vector3(0.29f, 0.13f, 0.16f));
                boot.localRotation = Quaternion.Euler(0f, side * 12f, 0f);
                thighs[i] = MakePart(PrimitiveType.Capsule, label + "thigh", trousers, Vector3.zero, Vector3.one);
                shins[i] = MakePart(PrimitiveType.Capsule, label + "shin", trousers, Vector3.zero, Vector3.one);
                sleeves[i] = MakePart(PrimitiveType.Capsule, label + "sleeve", fabric, Vector3.zero, Vector3.one);
                forearms[i] = MakePart(PrimitiveType.Capsule, label + "forearm", fabric, Vector3.zero, Vector3.one);
                gloves[i] = MakePart(PrimitiveType.Sphere, label + "glove", trim, Vector3.zero, new Vector3(0.12f, 0.14f, 0.12f));
            }
            Pose(1f);
        }

        private void LateUpdate()
        {
            if (replaying) { Pose(0f); return; }
            float blend = lastReset != rider.ResetCount ? 1f : 1f - Mathf.Exp(-12f * Time.deltaTime);
            lastReset = rider.ResetCount;
            Pose(blend);
        }

        private void Pose(float blend)
        {
            rig.SetPositionAndRotation(transform.position, transform.rotation);
            float edge = replaying ? replayEdge : rider.SteerInput;
            float targetHeight = rider.IsRecovering ? 0.59f : rider.IsGrounded ? 0.91f - 0.12f * Mathf.Abs(edge) : 0.76f;
            if (rider.ActiveGrab != BoardGrab.None) targetHeight = Mathf.Min(targetHeight, 0.58f);
            hipHeight = Mathf.Lerp(hipHeight, targetHeight, blend);
            lean = Mathf.Lerp(lean, edge * 0.22f, blend);
            tuck = Mathf.Lerp(tuck, rider.IsGrounded ? 0f : 1f, blend);

            var hips = new Vector3(-0.04f, hipHeight, lean * 0.5f);
            var shoulder = hips + new Vector3(0.12f + tuck * 0.05f, 0.37f, lean * 0.42f);
            SetSegment(jacket, hips, shoulder, 0.32f, 0.48f, 0.16f);
            helmet.localPosition = shoulder + new Vector3(0.035f, 0.22f, lean * 0.14f);
            helmet.localRotation = Quaternion.Euler(0f, -42f - edge * 16f, 0f);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                Vector3 ankle = new(0f, 0.24f, side * FootSpread);
                Vector3 hip = hips + new Vector3(0f, 0f, side * 0.17f);
                Vector3 leg = hip - ankle;
                float half = leg.magnitude * 0.5f;
                Vector3 bend = Vector3.ProjectOnPlane(Vector3.right, leg).normalized;
                float kneeOffset = Mathf.Sqrt(Mathf.Max(0f, SegmentLength * SegmentLength - half * half));
                Vector3 knee = (ankle + hip) * 0.5f + bend * kneeOffset;
                SetSegment(thighs[i], hip, knee, 0.18f, 0.17f, 0.08f);
                SetSegment(shins[i], knee, ankle, 0.15f, 0.15f, 0.08f);

                Vector3 upper = shoulder + new Vector3(0f, 0f, side * 0.23f);
                Vector3 elbow = upper + new Vector3(0.10f, -0.18f, side * 0.22f);
                Vector3 hand = elbow + new Vector3(0.15f + tuck * 0.08f, -0.15f + side * lean, side * 0.05f);
                BoardGrab edgeGrab = i == 0 ? BoardGrab.Indy : BoardGrab.Melon;
                grabBlend[i] = Mathf.Lerp(grabBlend[i], rider.ActiveGrab == edgeGrab ? 1f : 0f, blend);
                Vector3 boardEdge = new(-side * 0.263f, 0.14f, side * -0.06f);
                hand = Vector3.Lerp(hand, boardEdge, grabBlend[i]);
                elbow = Vector3.Lerp(elbow, (upper + hand) * 0.5f + new Vector3(0.04f, 0f, side * 0.15f), grabBlend[i]);
                SetSegment(sleeves[i], upper, elbow, 0.15f, 0.15f, 0.08f);
                SetSegment(forearms[i], elbow, hand, 0.13f, 0.13f, 0.08f);
                gloves[i].localPosition = hand;
            }
        }

        private static void SetSegment(Transform part, Vector3 from, Vector3 to, float width, float depth, float overlap)
        {
            Vector3 axis = to - from;
            part.localPosition = (from + to) * 0.5f;
            part.localRotation = Quaternion.FromToRotation(Vector3.up, axis);
            part.localScale = new Vector3(width, (axis.magnitude + overlap) * 0.5f, depth);
        }

        private Transform MakePart(PrimitiveType shape, string label, Material material, Vector3 position, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = label;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
            part.transform.SetParent(rig, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            return part.transform;
        }

        private Material MakeMaterial(string label, Color color) => new(paletteSource) { name = label, color = color };

        private void OnDestroy()
        {
            if (rig) Destroy(rig.gameObject);
            Destroy(trousers);
            Destroy(fabric);
            Destroy(trim);
            Destroy(shell);
            Destroy(visor);
        }
    }
}
