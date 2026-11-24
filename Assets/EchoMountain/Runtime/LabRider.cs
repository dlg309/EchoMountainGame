using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private float quadraticDrag;
        [SerializeField] private float turnAcceleration = 2.8f;
        [SerializeField] private float popVelocity = 4.2f;
        [SerializeField] private float jumpBufferSeconds = 0.15f;
        [SerializeField] private float contactGraceSeconds = 0.07f;
        [SerializeField] private float resetBelow = -20f;
        [SerializeField] private SnowSurfaceMap surfaceMap;
        public SnowSurface? ActiveSurface { get; private set; }
        public void SetSurfaceMap(SnowSurfaceMap value) => surfaceMap = value;
        private readonly Vector3[] probes = {
            new Vector3(-0.24f, 0f, -0.48f), new Vector3(0.24f, 0f, -0.48f),
            new Vector3(-0.24f, 0f, 0.48f), new Vector3(0.24f, 0f, 0.48f)
        };
        private readonly RaycastHit[] probeHits = new RaycastHit[16];
        private Rigidbody body;
        private LabRideControls controls;
        private LabHud hud;
        private TrickScore tricks;
        private FreeRoamRailRide railRide;
        private float spinInput;
        private bool spinEngaged;
        private float flipInput, flipDirection, flipAngle;
        private Vector3 flipAxis;
        private Quaternion lastFlipRotation;
        private BoardGrab grabInput;
        public BoardGrab ActiveGrab => tricks?.CurrentGrab ?? BoardGrab.None;
        internal void EnableTricks(TrickScore score) => tricks = score;
        internal void EnableFreeRoam(FreeRoamMap map) => railRide = new FreeRoamRailRide(map, body);
        private PhysicsMaterial boardMaterial;
        public bool IsGrounded => contact;
        public float BrakeInput => brake;
        public bool UsingGamepad => controls?.LastGamepad != null && controls.LastGamepad.added && controls.LastGamepad.enabled;
        public bool IsGrinding => railRide?.IsGrinding == true;
        public float SpeedKmh => body ? body.linearVelocity.magnitude * 3.6f : 0f;
        public Vector3 ContactNormal => normal;
        public float LateralSlip => slip;
        public float SteerInput => steer;
        internal float AppliedSteer => smoothedSteer;
        internal float LeanInput => contact
            ? steer * Mathf.Sign(Vector3.Dot(body.linearVelocity, body.rotation * Vector3.forward))
            : 0f;
        public float GripAcceleration => gripForce;
        public string LandingText => landing;
        public void RequestJump() => bufferedJump = jumpBufferSeconds;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steer, smoothedSteer, brake, bufferedJump, lastContact;
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

        private void OnEnable()
        {
            controls.Enable();
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void OnDisable()
        {
            railRide?.Reset();
            InputSystem.onDeviceChange -= OnDeviceChange;
            controls.Disable();
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (device != controls.LastGamepad || !hud.isActiveAndEnabled) return;
            if (change != InputDeviceChange.Disconnected && change != InputDeviceChange.Removed &&
                change != InputDeviceChange.Disabled) return;
            ClearInputs();
            hud.PauseForInputLoss("Controller disconnected. Reconnect or use the keyboard, then resume.");
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && isActiveAndEnabled && hud && hud.isActiveAndEnabled)
            {
                ClearInputs();
                hud.PauseForInputLoss("Window unfocused. Resume when you are ready.");
            }
        }

        private void ClearInputs()
        {
            steer = brake = bufferedJump = spinInput = flipInput = 0f;
            grabInput = BoardGrab.None;
        }

        private void Update()
        {
            bool toggledPause = controls.Pause.WasPressedThisFrame() && hud.isActiveAndEnabled;
            if (toggledPause) hud.SetPaused(!hud.IsPaused);
            if (toggledPause || hud.IsPaused)
            {
                ClearInputs();
                return;
            }

            steer = controls.Steer;
            brake = controls.Brake;
            if (tricks != null)
            {
                spinInput = controls.Spin;
                flipInput = controls.Flip;
                grabInput = controls.Grab;
            }
            if (controls.Pop.WasPressedThisFrame()) RequestJump();
            if (controls.Reset.WasPressedThisFrame() || body.position.y < resetBelow) ResetRider();
        }

        private void FixedUpdate()
        {
            tricks?.Tick(Time.fixedDeltaTime);
            if (recoveryRemaining > 0f)
            {
                recoveryRemaining -= Time.fixedDeltaTime;
                if (recoveryRemaining <= 0f) ResetRider();
                return;
            }
            if (railRide != null && railRide.Step(!contact && airborneTime >= 0.07f,
                bufferedJump > 0f, popVelocity, tricks))
            {
                contact = wasContact = false;
                bufferedJump = 0f;
                lastContact = float.NegativeInfinity;
                landingArmed = true;
                airborneTime = Mathf.Max(airborneTime, 0.12f);
                supportedTime = 0f;
                spinEngaged = false;
                flipDirection = 0f;
                lastAirVelocity = body.linearVelocity;
                lastAirSpin = body.angularVelocity;
                return;
            }
            if (quadraticDrag > 0f)
            {
                Vector3 velocity = body.linearVelocity;
                body.AddForce(-velocity * (quadraticDrag * velocity.magnitude), ForceMode.Acceleration);
            }
            bufferedJump = Mathf.Max(0f, bufferedJump - Time.fixedDeltaTime);
            wasContact = contact;
            contact = false;
            Vector3 sum = Vector3.zero;
            int hits = 0, parkHits = 0;
            foreach (var offset in probes)
            {
                Vector3 origin = body.position + body.rotation * (offset + Vector3.up * 0.15f);
                int count = Physics.RaycastNonAlloc(origin, -(body.rotation * Vector3.up), probeHits,
                    probeLength, ~0, QueryTriggerInteraction.Ignore);
                // Results are unordered. A full buffer can omit the nearest hit, so fail closed.
                if (count == probeHits.Length) continue;
                float nearest = float.PositiveInfinity;
                Vector3 nearestNormal = Vector3.zero;
                Collider nearestCollider = null;
                for (int i = 0; i < count; i++)
                {
                    var hit = probeHits[i];
                    if (hit.rigidbody == body || Vector3.Dot(hit.normal, Vector3.up) <= 0.35f || hit.distance >= nearest)
                        continue;
                    nearest = hit.distance;
                    nearestNormal = hit.normal;
                    nearestCollider = hit.collider;
                }
                // The probe starts 0.15 m above the centre of this 0.20 m board.
                // Hysteresis is deliberately small; do not attach across a lip.
                float supportDistance = wasContact ? 0.35f : 0.30f;
                if (nearest <= supportDistance)
                {
                    bool park = railRide?.IsParkSurface(nearestCollider) == true;
                    if (park && (!railRide.TouchingParkTop || railRide.LeavingRail)) continue;
                    if (park) parkHits++;
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
            if (contact && !wasContact && landingArmed && airborneTime >= 0.12f && parkHits < hits)
            {
                Vector3 travel = Vector3.ProjectOnPlane(lastAirVelocity, normal);
                Vector3 boardForward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal).normalized;
                float alignment = travel.sqrMagnitude > 0.0001f ? Vector3.Dot(travel.normalized, boardForward) : 1f;
                float upright = Vector3.Dot(body.rotation * Vector3.up, normal);
                float impact = Mathf.Max(0f, -Vector3.Dot(lastAirVelocity, normal));
                LandingOutcome outcome = LabLandingRules.Evaluate(upright, alignment, travel.magnitude,
                    impact, lastAirSpin.magnitude, hits);
                if (flipDirection != 0f) TrackFlip();
                FinishLanding(outcome);
            }
            if (contact)
            {
                tricks?.CancelAir();
                spinEngaged = false;
                flipDirection = 0f;
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
                if (landingArmed && tricks != null)
                {
                    if (flipDirection == 0f && Mathf.Abs(flipInput) >= 0.55f &&
                        Vector3.Dot(body.rotation * Vector3.up, Vector3.up) > 0.7f)
                    {
                        Vector3 heading = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, Vector3.up);
                        if (Vector3.Dot(heading, body.linearVelocity) < -0.8f) heading = -heading;
                        flipAxis = Vector3.Cross(Vector3.up, heading).normalized;
                        flipDirection = Mathf.Sign(flipInput);
                        flipAngle = 0f;
                        lastFlipRotation = body.rotation;
                    }
                    tricks.SampleAir(body.rotation.eulerAngles.y, grabInput, Time.fixedDeltaTime,
                        trackSpin: flipDirection == 0f);
                    spinEngaged |= Mathf.Abs(spinInput) > 0.05f;
                    if (flipDirection != 0f)
                    {
                        TrackFlip();
                        float remaining = 2f * Mathf.PI - flipAngle * flipDirection;
                        float rate = flipDirection * Mathf.Clamp(remaining * 20f, -4f, 15f);
                        Vector3 target = flipAxis * rate;
                        body.AddTorque(Vector3.ClampMagnitude((target - body.angularVelocity) * 26f, 270f),
                            ForceMode.Acceleration);
                    }
                    else if (spinEngaged)
                    {
                        float target = spinInput * 9.2f;
                        float acceleration = Mathf.Clamp((target - body.angularVelocity.y) * 18f, -145f, 145f);
                        body.AddTorque(Vector3.up * acceleration, ForceMode.Acceleration);
                    }
                }
            }
            gripForce = 0f;
            if (contact)
            {
                SurfaceResponse surface = surfaceMap ? surfaceMap.Sample(body.position) : SurfaceRules.Response(SnowSurface.Packed);
                ActiveSurface = surface.Kind;
                Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal).normalized;
                Vector3 side = Vector3.Cross(normal, forward).normalized;
                float forwardSpeed = Vector3.Dot(body.linearVelocity, forward);
                slip = Vector3.Dot(body.linearVelocity, side);
                float desiredGrip = Mathf.Clamp(-slip * edgeGrip * surface.Grip,
                    -maxGripAcceleration * surface.Grip, maxGripAcceleration * surface.Grip);
                gripForce = desiredGrip;
                float speed = Mathf.Abs(forwardSpeed);
                Vector3 travel = forward * forwardSpeed + side * slip;
                float stanceAlignment = speed / Mathf.Max(travel.magnitude, 0.01f);
                // An aligned edge steers along the snow with minimal speed loss. Skids and braking
                // retain drag from the board's sideways motion.
                float carve = 0.88f * Mathf.InverseLerp(0.70f, 0.97f, stanceAlignment)
                    * Mathf.InverseLerp(2.5f, 5.5f, speed) * (1f - brake);
                Vector3 gripDirection = Vector3.Lerp(side, Vector3.ProjectOnPlane(side, travel).normalized, carve);
                body.AddForce(gripDirection * desiredGrip, ForceMode.Acceleration);
                float resistance = Mathf.Min(Mathf.Abs(forwardSpeed) / Time.fixedDeltaTime,
                    rollingResistance * surface.Roll + brake * braking * surface.Brake);
                body.AddForce(-forward * Mathf.Sign(forwardSpeed) * resistance, ForceMode.Acceleration);
                // Unload the old edge quickly before building pressure on the opposite edge.
                bool changingEdges = smoothedSteer * steer < 0f;
                float steerGoal = changingEdges ? 0f : steer;
                float edgeRate = changingEdges ? 20f : 11f;
                smoothedSteer = Mathf.MoveTowards(smoothedSteer, steerGoal, edgeRate * Time.fixedDeltaTime);
                float yawRate = Vector3.Dot(body.angularVelocity, normal);
                // Match the available lateral grip at speed, with some reserve for the next edge change.
                float yawLimit = maxGripAcceleration * surface.Grip * 0.82f / Mathf.Max(speed, 1f);
                float yawTarget = smoothedSteer * Mathf.Min(turnAcceleration * 0.40f, yawLimit)
                    * Mathf.Clamp01(speed / 2.2f) * (1f - 0.26f * brake);
                body.AddTorque(normal * ((yawTarget - yawRate) * 7.5f), ForceMode.Acceleration);
                Vector3 tilt = Vector3.Cross(body.rotation * Vector3.up, normal);
                Vector3 tiltRate = Vector3.ProjectOnPlane(body.angularVelocity, normal);
                body.AddTorque(Vector3.ClampMagnitude(tilt * 12f - tiltRate * 4f, 12f), ForceMode.Acceleration);
            }
            else
            {
                slip = 0f;
                smoothedSteer = 0f;
            }
            if (bufferedJump > 0f && Time.fixedTime - lastContact <= contactGraceSeconds)
            {
                body.AddForce(normal * popVelocity, ForceMode.VelocityChange);
                bufferedJump = 0f;
                lastContact = float.NegativeInfinity;
                contact = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            railRide?.ObserveContact(collision);
            if (flipDirection == 0f || contact || !landingArmed || airborneTime < 0.12f) return;
            for (int i = 0; i < collision.contactCount; i++)
            {
                Vector3 supportNormal = collision.GetContact(i).normal;
                if (supportNormal.y <= 0.35f || Vector3.Dot(body.rotation * Vector3.up, supportNormal) >= 0.6f)
                    continue;
                // A nose-first or inverted impact can occur before the sole probes point at the snow.
                FinishLanding(LandingOutcome.Bail);
                return;
            }
        }

        private void OnCollisionStay(Collision collision) => railRide?.ObserveContact(collision);

        private void FinishLanding(LandingOutcome outcome)
        {
            LastLanding = outcome;
            LandingCount++;
            if (outcome == LandingOutcome.Bail) recoveryRemaining = 1.25f;
            landingArmed = false;
            landing = outcome.ToString();
            tricks?.Land(body.rotation.eulerAngles.y, outcome);
        }

        private void TrackFlip()
        {
            Quaternion delta = body.rotation * Quaternion.Inverse(lastFlipRotation);
            float aroundAxis = Vector3.Dot(new Vector3(delta.x, delta.y, delta.z), flipAxis);
            float degrees = 2f * Mathf.Atan2(aroundAxis, delta.w) * Mathf.Rad2Deg;
            flipAngle += Mathf.DeltaAngle(0f, degrees) * Mathf.Deg2Rad;
            lastFlipRotation = body.rotation;
            tricks.SampleFlip(flipAngle * Mathf.Rad2Deg);
        }

        private void OnDestroy()
        {
            controls.Dispose();
            if (boardMaterial) Destroy(boardMaterial);
        }

        public void SetSpawn(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
            ResetRider();
        }

        public void ResetRider()
        {
            railRide?.Reset();
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            bufferedJump = 0f;
            recoveryRemaining = 0f;
            lastContact = float.NegativeInfinity;
            contact = wasContact = landingArmed = false;
            ActiveSurface = null;
            supportedTime = 0f;
            normal = Vector3.up;
            landing = "--";
            airborneTime = 0f;
            lastAirVelocity = lastAirSpin = Vector3.zero;
            LastLanding = null;
            LandingCount = 0;
            steer = smoothedSteer = brake = slip = gripForce = spinInput = flipInput = flipDirection = 0f;
            spinEngaged = false;
            grabInput = BoardGrab.None;
            tricks?.Reset(preservePoints: railRide != null);
            ResetCount++;
        }

    }
}
