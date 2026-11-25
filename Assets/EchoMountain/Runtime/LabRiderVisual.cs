using UnityEngine;
using UnityEngine.Serialization;

namespace EchoMountain.Riding
{
    // A board-mounted pose rig for the lab. All parts are visual and share the board's interpolated pose.
    [RequireComponent(typeof(LabRider))]
    public sealed class LabRiderVisual : MonoBehaviour
    {
        private const float FootSpread = 0.31f;
        private const float SegmentLength = 0.43f;
        [SerializeField] private Material paletteSource;
        [FormerlySerializedAs("sunsetOutfit")]
        [SerializeField] private bool twinTipDeck;
        private readonly Transform[] thighs = new Transform[2];
        private readonly Transform[] shins = new Transform[2];
        private readonly Transform[] sleeves = new Transform[2];
        private readonly Transform[] forearms = new Transform[2];
        private readonly Transform[] gloves = new Transform[2];
        private Transform rig, jacket, helmet;
        private Material trousers, fabric, trim, shell, visor, bindings, topGraphic, baseGraphic;
        private Mesh graphicMesh;
        private Texture2D topTexture, baseTexture;
        private Transform graphicDeck;
        private LabRider rider;
        private Rigidbody body;
        private uint lastReset;
        private float hipHeight = 0.91f;
        private float lean;
        private float tuck;
        private float brakeShift;
        private float brakeEdge;
        private readonly float[] grabBlend = new float[2];
        private bool replaying;
        private float replayEdge;

        public RiderVisualPose CapturePose() => new()
        {
            edge = replaying ? replayEdge : rider.SteerInput, hipHeight = hipHeight,
            lean = lean, tuck = tuck, brakeShift = brakeShift, indy = grabBlend[0], melon = grabBlend[1]
        };

        public void ApplyReplayPose(RiderVisualPose pose)
        {
            replaying = true;
            replayEdge = pose.edge;
            hipHeight = pose.hipHeight;
            lean = pose.lean;
            tuck = pose.tuck;
            brakeShift = pose.brakeShift;
            grabBlend[0] = pose.indy;
            grabBlend[1] = pose.melon;
            if (rig) Pose(0f, 0f);
        }

        public void ClearReplayPose() => replaying = false;

        public void SetPalette(Material source) => paletteSource = source;

        private void Awake()
        {
            rider = GetComponent<LabRider>();
            body = GetComponent<Rigidbody>();
            if (!paletteSource)
            {
                enabled = false;
                return;
            }

            rig = new GameObject("Segmented rider").transform;
            rig.SetParent(transform.parent, false);
            trousers = MakeMaterial("Rider trousers", new Color(0.15f, 0.21f, 0.24f));
            fabric = MakeMaterial("Rider jacket", new Color(0.80f, 0.36f, 0.16f));
            trim = MakeMaterial("Rider trim", new Color(0.03f, 0.06f, 0.08f));
            shell = MakeMaterial("Rider helmet", new Color(0.86f, 0.90f, 0.90f));
            visor = MakeMaterial("Rider visor", new Color(0.12f, 0.56f, 0.65f));
            bindings = MakeMaterial("Binding color", Color.white);
            const PrimitiveType garment = PrimitiveType.Capsule;
            jacket = MakePart(garment, "Jacket", fabric, Vector3.zero, new Vector3(0.26f, 0.28f, 0.48f));
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
                MakePart(PrimitiveType.Cube, label + "binding", bindings, bindingPosition, new Vector3(0.34f, 0.05f, 0.2f));
                var boot = MakePart(PrimitiveType.Cube, label + "boot", trim,
                    bindingPosition + new Vector3(0.025f, 0.08f, 0f), new Vector3(0.29f, 0.13f, 0.16f));
                boot.localRotation = Quaternion.Euler(0f, side * 12f, 0f);
                thighs[i] = MakePart(garment, label + "thigh", trousers, Vector3.zero, Vector3.one);
                shins[i] = MakePart(garment, label + "shin", trousers, Vector3.zero, Vector3.one);
                sleeves[i] = MakePart(garment, label + "sleeve", fabric, Vector3.zero, Vector3.one);
                forearms[i] = MakePart(garment, label + "forearm", fabric, Vector3.zero, Vector3.one);
                gloves[i] = MakePart(PrimitiveType.Sphere, label + "glove", trim, Vector3.zero, new Vector3(0.12f, 0.14f, 0.12f));
            }
            BuildBoardGraphics();
            ApplyCustomization(RiderCustomizationStore.OpenDefault().Look);
            Pose(1f, 1f);
        }

        public void ApplyCustomization(RiderLook look)
        {
            if (!RiderCustomization.Valid(look) || fabric == null) return;
            fabric.color = RiderCustomization.Colors[look.jacket];
            trousers.color = RiderCustomization.Colors[look.pants];
            shell.color = RiderCustomization.Colors[look.helmet];
            visor.color = RiderCustomization.Colors[look.goggles];
            bindings.color = RiderCustomization.DeckAccents[look.topside];
            if (graphicDeck)
            {
                Paint(topTexture, look.topside);
                Paint(baseTexture, look.underside);
            }
        }

        private void BuildBoardGraphics()
        {
            var original = GetComponent<MeshFilter>()?.sharedMesh;
            if (!twinTipDeck || !original || original.vertexCount != 66 || original.triangles.Length != 384) return;
            var vertices = original.vertices;
            var sourceTriangles = original.triangles;
            var top = new int[96];
            var bottom = new int[96];
            for (int slice = 0; slice < 32; slice++)
            {
                int source = slice * 12;
                int destination = slice * 3;
                for (int index = 0; index < 3; index++)
                {
                    top[destination + index] = sourceTriangles[source + index];
                    bottom[destination + index] = sourceTriangles[source + index + 3];
                }
            }
            var uv = new Vector2[vertices.Length];
            for (int index = 0; index < vertices.Length; index++)
            {
                uv[index] = new Vector2(Mathf.InverseLerp(-0.41f, 0.41f, vertices[index].x),
                    Mathf.InverseLerp(-0.64f, 0.64f, vertices[index].z));
                vertices[index].y += index == 0 || (index >= 2 && index % 2 == 0) ? 0.006f : -0.006f;
            }
            graphicMesh = new Mesh { name = "Cosmetic deck surfaces" };
            graphicMesh.vertices = vertices;
            graphicMesh.uv = uv;
            graphicMesh.subMeshCount = 2;
            graphicMesh.SetTriangles(top, 0);
            graphicMesh.SetTriangles(bottom, 1);
            graphicMesh.RecalculateNormals();
            graphicDeck = new GameObject("Board graphics").transform;
            graphicDeck.SetParent(transform, false);
            graphicDeck.gameObject.AddComponent<MeshFilter>().sharedMesh = graphicMesh;
            topGraphic = MakeMaterial("Topside design", Color.white);
            baseGraphic = MakeMaterial("Underside design", Color.white);
            topTexture = new Texture2D(64, 128, TextureFormat.RGBA32, false) { name = "Topside graphic" };
            baseTexture = new Texture2D(64, 128, TextureFormat.RGBA32, false) { name = "Underside graphic" };
            topGraphic.SetTexture("_BaseMap", topTexture);
            baseGraphic.SetTexture("_BaseMap", baseTexture);
            graphicDeck.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { topGraphic, baseGraphic };
        }

        private static void Paint(Texture2D texture, int design)
        {
            Color body = RiderCustomization.DeckColors[design];
            Color accent = RiderCustomization.DeckAccents[design];
            var pixels = new Color[64 * 128];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = (x + 0.5f) / 64f;
                float v = (y + 0.5f) / 128f;
                bool motif = design == 0 ? Mathf.Abs(u - 0.5f) < 0.08f || (v > 0.78f && v < 0.84f)
                    : design == 1 ? Mathf.Abs(u - (0.24f + v * 0.52f)) < 0.11f
                    : (Mathf.Abs(u - 0.5f) < 0.035f || (v > 0.18f && v < 0.22f) || (v > 0.78f && v < 0.82f));
                pixels[y * 64 + x] = motif ? accent : body;
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
        }

        private void LateUpdate()
        {
            if (replaying) { Pose(0f, 0f); return; }
            bool reset = lastReset != rider.ResetCount;
            lastReset = rider.ResetCount;
            if (reset) brakeEdge = 0f;
            Pose(reset ? 1f : 1f - Mathf.Exp(-12f * Time.deltaTime),
                reset ? 1f : 1f - Mathf.Exp(-19f * Time.deltaTime));
        }

        private void Pose(float blend, float leanBlend)
        {
            // The twin-tip deck's center surface is 0.156 m below the diagnostic board's top.
            Vector3 deckOffset = twinTipDeck ? -transform.up * 0.156f : Vector3.zero;
            rig.SetPositionAndRotation(transform.position + deckOffset, transform.rotation);
            float edge = replaying ? replayEdge : rider.SteerInput;
            if (!replaying)
            {
                brakeShift = Mathf.Lerp(brakeShift, BrakeShiftTarget(), blend);
                float targetHeight = rider.IsRecovering ? 0.59f : rider.IsGrinding ? 0.70f
                    : rider.IsGrounded ? 0.91f - 0.12f * Mathf.Abs(edge) - 0.13f * Mathf.Abs(brakeShift) : 0.76f;
                if (rider.ActiveGrab != BoardGrab.None) targetHeight = Mathf.Min(targetHeight, 0.58f);
                hipHeight = Mathf.Lerp(hipHeight, targetHeight, blend);
                lean = Mathf.Lerp(lean, rider.LeanInput * 0.22f, leanBlend);
                tuck = Mathf.Lerp(tuck, rider.IsGrounded ? 0f : 1f, blend);
            }

            float turnLean = lean * (1f - Mathf.Abs(brakeShift));
            var hips = new Vector3(-0.04f + turnLean * 0.32f + brakeShift * 0.14f, hipHeight, 0f);
            var shoulder = hips + new Vector3(0.12f + tuck * 0.05f + turnLean * 0.68f + brakeShift * 0.14f, 0.37f, 0f);
            SetSegment(jacket, hips, shoulder, 0.32f, 0.48f, 0.16f);
            helmet.localPosition = shoulder + new Vector3(0.035f + lean * 0.14f, 0.22f, 0f);
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
                Vector3 elbow = upper + new Vector3(0.06f, -0.23f, side * (0.075f + 0.09f * tuck));
                Vector3 hand = elbow + new Vector3(0.10f + tuck * 0.08f, -0.17f + side * turnLean * 0.22f, side * 0.025f);
                BoardGrab edgeGrab = i == 0 ? BoardGrab.Indy : BoardGrab.Melon;
                grabBlend[i] = Mathf.Lerp(grabBlend[i], rider.ActiveGrab == edgeGrab ? 1f : 0f, blend);
                Vector3 boardEdge = new(-side * (twinTipDeck ? 0.17f : 0.263f), 0.11f, side * -0.06f);
                hand = Vector3.Lerp(hand, boardEdge, grabBlend[i]);
                elbow = Vector3.Lerp(elbow, (upper + hand) * 0.5f + new Vector3(0.04f, 0f, side * 0.15f), grabBlend[i]);
                SetSegment(sleeves[i], upper, elbow, 0.15f, 0.15f, 0.08f);
                SetSegment(forearms[i], elbow, hand, 0.13f, 0.13f, 0.08f);
                gloves[i].localPosition = hand;
            }
        }

        private float BrakeShiftTarget()
        {
            if (!rider.isActiveAndEnabled || !rider.IsGrounded || rider.IsGrinding ||
                rider.IsRecovering || rider.BrakeInput < 0.05f)
            {
                brakeEdge = 0f;
                return 0f;
            }

            // Boots point toward local +X. Load the edge uphill of the skid in either stance.
            float slip = rider.LateralSlip;
            if (Mathf.Abs(slip) > 0.65f)
                brakeEdge = -Mathf.Sign(slip);
            else if (brakeEdge == 0f)
            {
                float travelDirection = Mathf.Sign(Vector3.Dot(body.linearVelocity, body.rotation * Vector3.forward));
                float turnEdge = rider.AppliedSteer * travelDirection;
                brakeEdge = Mathf.Abs(turnEdge) > 0.15f ? Mathf.Sign(turnEdge) : -1f;
            }

            float speed = Vector3.ProjectOnPlane(body.linearVelocity, rider.ContactNormal).magnitude;
            return brakeEdge * rider.BrakeInput * Mathf.InverseLerp(0.5f, 3f, speed);
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
            Destroy(bindings);
            if (graphicDeck) Destroy(graphicDeck.gameObject);
            Destroy(graphicMesh);
            Destroy(topGraphic);
            Destroy(baseGraphic);
            Destroy(topTexture);
            Destroy(baseTexture);
        }
    }
}
