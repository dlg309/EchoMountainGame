using UnityEngine;

namespace EchoMountain.Riding
{
    // Diagnostic prototype: collision provides support, probes measure the nearby surface.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class LabRider : MonoBehaviour
    {
        [SerializeField] private float probeLength = 0.65f;
        [SerializeField] private float edgeGrip = 7f;
        [SerializeField] private float maxGripAcceleration = 12f;
        [SerializeField] private float braking = 5f;
        [SerializeField] private float rollingResistance = 0.22f;
        [SerializeField] private float turnAcceleration = 2.8f;
        [SerializeField] private float popVelocity = 4.2f;
        [SerializeField] private float jumpBufferSeconds = 0.15f;
        [SerializeField] private float contactGraceSeconds = 0.07f;
        [SerializeField] private float resetBelow = -20f;
        private readonly Vector3[] probes = {
            new Vector3(-0.24f, 0f, -0.48f), new Vector3(0.24f, 0f, -0.48f),
            new Vector3(-0.24f, 0f, 0.48f), new Vector3(0.24f, 0f, 0.48f)
        };
        private readonly RaycastHit[] probeHits = new RaycastHit[16];
        private Rigidbody body;
        private LabRideControls controls;
        private LabHud hud;
        private PhysicsMaterial boardMaterial;
        public bool IsGrounded => contact;
        public float BrakeInput => brake;
        public float SpeedKmh => body ? body.linearVelocity.magnitude * 3.6f : 0f;
        public Vector3 ContactNormal => normal;
        public float LateralSlip => slip;
        public float SteerInput => steer;
        public float GripAcceleration => gripForce;
        public string LandingText => landing;
        public void RequestJump() => bufferedJump = jumpBufferSeconds;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steer, brake, bufferedJump, lastContact;
        private bool contact, wasContact;
        private bool landingArmed;
        private float supportedTime;
        private float airborneTime;
        private float recoveryRemaining;
        public bool IsRecovering => recoveryRemaining > 0f;
        private Vector3 lastAirVelocity, lastAirSpin;
        public LandingOutcome? LastLanding { get; private set; }
        public int LandingCount { get; private set; }
        public uint ResetCount { get; private set; }
        private Vector3 normal = Vector3.up;
        private float slip, gripForce;
        private string landing = "--";

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            controls = new LabRideControls();
            if (!TryGetComponent(out hud)) hud = gameObject.AddComponent<LabHud>();
            // The scripted resistance and edge model own board friction.
            boardMaterial = new PhysicsMaterial("Lab board friction")
            {
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f
            };
            GetComponent<Collider>().material = boardMaterial;
            spawnPosition = body.position;
            spawnRotation = body.rotation;
            lastContact = float.NegativeInfinity;
        }

        private void OnEnable() => controls.Enable();

        private void OnDisable() => controls.Disable();

        private void Update()
        {
            bool toggledPause = controls.Pause.WasPressedThisFrame() && hud.isActiveAndEnabled;
            if (toggledPause) hud.SetPaused(!hud.IsPaused);
            if (toggledPause || hud.IsPaused)
            {
                steer = brake = bufferedJump = 0f;
                return;
            }

            steer = controls.Steer;
            brake = controls.Brake;
            if (controls.Pop.WasPressedThisFrame()) RequestJump();
            if (controls.Reset.WasPressedThisFrame() || body.position.y < resetBelow) ResetRider();
        }

        private void FixedUpdate()
        {
            if (recoveryRemaining > 0f)
            {
                recoveryRemaining -= Time.fixedDeltaTime;
                if (recoveryRemaining <= 0f) ResetRider();
                return;
            }
            bufferedJump = Mathf.Max(0f, bufferedJump - Time.fixedDeltaTime);
            wasContact = contact;
            contact = false;
            Vector3 sum = Vector3.zero;
            int hits = 0;
            foreach (var offset in probes)
            {
                Vector3 origin = body.position + body.rotation * (offset + Vector3.up * 0.15f);
                int count = Physics.RaycastNonAlloc(origin, -(body.rotation * Vector3.up), probeHits,
                    probeLength, ~0, QueryTriggerInteraction.Ignore);
                // Results are unordered. A full buffer can omit the nearest hit, so fail closed.
                if (count == probeHits.Length) continue;
                float nearest = float.PositiveInfinity;
                Vector3 nearestNormal = Vector3.zero;
                for (int i = 0; i < count; i++)
                {
                    var hit = probeHits[i];
                    if (hit.rigidbody == body || Vector3.Dot(hit.normal, Vector3.up) <= 0.35f || hit.distance >= nearest)
                        continue;
                    nearest = hit.distance;
                    nearestNormal = hit.normal;
                }
                // The probe starts 0.15 m above the centre of this 0.20 m board.
                // Hysteresis is deliberately small; do not attach across a lip.
                float supportDistance = wasContact ? 0.35f : 0.30f;
                if (nearest <= supportDistance)
                {
                    sum += nearestNormal;
                    hits++;
                }
            }
            // Do not treat a nearby surface as support when moving rapidly away from it.
            if (hits > 0 && Vector3.Dot(body.linearVelocity, sum.normalized) < 1.2f)
            {
                normal = sum.normalized;
                contact = true;
                lastContact = Time.fixedTime;
            }
            if (contact && !wasContact && landingArmed && airborneTime >= 0.12f)
            {
                Vector3 travel = Vector3.ProjectOnPlane(lastAirVelocity, normal);
                Vector3 boardForward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal).normalized;
                float alignment = travel.sqrMagnitude > 0.0001f ? Vector3.Dot(travel.normalized, boardForward) : 1f;
                float upright = Vector3.Dot(body.rotation * Vector3.up, normal);
                float impact = Mathf.Max(0f, -Vector3.Dot(lastAirVelocity, normal));
                LastLanding = LabLandingRules.Evaluate(upright, alignment, travel.magnitude, impact, lastAirSpin.magnitude, hits);
                LandingCount++;
                if (LastLanding == LandingOutcome.Bail) recoveryRemaining = 1.25f;
                landingArmed = false;
                landing = LastLanding.Value.ToString();
            }
            if (contact)
            {
                airborneTime = 0f;
                supportedTime += Time.fixedDeltaTime;
                if (supportedTime >= 0.2f) landingArmed = true;
            }
            else
            {
                supportedTime = 0f;
                airborneTime += Time.fixedDeltaTime;
                lastAirVelocity = body.linearVelocity;
                lastAirSpin = body.angularVelocity;
            }
            gripForce = 0f;
            if (contact)
            {
                Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal).normalized;
                Vector3 side = Vector3.Cross(normal, forward).normalized;
                float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
                slip = Vector3.Dot(body.linearVelocity, side);
                float desiredGrip = Mathf.Clamp(-slip * edgeGrip, -maxGripAcceleration, maxGripAcceleration);
                gripForce = desiredGrip;
                body.AddForce(side * desiredGrip, ForceMode.Acceleration);
                float resistance = Mathf.Min(Mathf.Abs(forwardSpeed) / Time.fixedDeltaTime,
                    rollingResistance + brake * braking);
                body.AddForce(-forward * Mathf.Sign(forwardSpeed) * resistance, ForceMode.Acceleration);
                body.AddTorque(normal * (steer * turnAcceleration * Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f)), ForceMode.Acceleration);
                Vector3 tilt = Vector3.Cross(body.rotation * Vector3.up, normal);
                Vector3 tiltRate = Vector3.ProjectOnPlane(body.angularVelocity, normal);
                body.AddTorque(Vector3.ClampMagnitude(tilt * 12f - tiltRate * 4f, 12f), ForceMode.Acceleration);
            }
            else slip = 0f;
            if (bufferedJump > 0f && Time.fixedTime - lastContact <= contactGraceSeconds)
            {
                body.AddForce(normal * popVelocity, ForceMode.VelocityChange);
                bufferedJump = 0f;
                lastContact = float.NegativeInfinity;
                contact = false;
            }
        }

        private void OnDestroy()
        {
            controls.Dispose();
            if (boardMaterial) Destroy(boardMaterial);
        }

        public void ResetRider()
        {
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            bufferedJump = 0f;
            recoveryRemaining = 0f;
            lastContact = float.NegativeInfinity;
            contact = wasContact = landingArmed = false;
            supportedTime = 0f;
            normal = Vector3.up;
            landing = "--";
            airborneTime = 0f;
            lastAirVelocity = lastAirSpin = Vector3.zero;
            LastLanding = null;
            LandingCount = 0;
            ResetCount++;
        }

    }
}
