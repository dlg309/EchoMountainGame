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
        [SerializeField] private float turnAcceleration = 2.8f;
        [SerializeField] private float popVelocity = 4.2f;
        [SerializeField] private float jumpBufferSeconds = 0.15f;
        [SerializeField] private float contactGraceSeconds = 0.07f;
        [SerializeField] private float resetBelow = -20f;
        private readonly Vector3[] probes = {
            new Vector3(-0.24f, 0f, -0.48f), new Vector3(0.24f, 0f, -0.48f),
            new Vector3(-0.24f, 0f, 0.48f), new Vector3(0.24f, 0f, 0.48f)
        };
        private Rigidbody body;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float steer, brake, bufferedJump, lastContact;
        private bool contact, wasContact;
        private Vector3 normal = Vector3.up;
        private float slip, gripForce;
        private string landing = "--";

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            spawnPosition = body.position;
            spawnRotation = body.rotation;
            lastContact = float.NegativeInfinity;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            steer = 0f;
            brake = 0f;
            if (keyboard != null)
            {
                steer += (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                brake = keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f;
                if (keyboard.spaceKey.wasPressedThisFrame) bufferedJump = jumpBufferSeconds;
                if (keyboard.rKey.wasPressedThisFrame) ResetRider();
            }
            if (gamepad != null)
            {
                steer = Mathf.Clamp(steer + gamepad.leftStick.x.ReadValue(), -1f, 1f);
                brake = Mathf.Max(brake, gamepad.leftTrigger.ReadValue());
                if (gamepad.buttonSouth.wasPressedThisFrame) bufferedJump = jumpBufferSeconds;
                if (gamepad.buttonEast.wasPressedThisFrame) ResetRider();
            }
            if (body.position.y < resetBelow) ResetRider();
        }

        private void FixedUpdate()
        {
            bufferedJump = Mathf.Max(0f, bufferedJump - Time.fixedDeltaTime);
            wasContact = contact;
            contact = false;
            Vector3 sum = Vector3.zero;
            int hits = 0;
            foreach (var offset in probes)
            {
                Vector3 origin = body.position + body.rotation * (offset + Vector3.up * 0.15f);
                if (Physics.Raycast(origin, -(body.rotation * Vector3.up), out RaycastHit hit,
                    probeLength, ~0, QueryTriggerInteraction.Ignore) && hit.rigidbody != body &&
                    Vector3.Dot(hit.normal, Vector3.up) > 0.35f)
                {
                    sum += hit.normal;
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
            if (contact && !wasContact)
            {
                Vector3 travel = Vector3.ProjectOnPlane(body.linearVelocity, normal);
                Vector3 board = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
                landing = travel.sqrMagnitude < 1f || Mathf.Abs(Vector3.Dot(travel.normalized, board)) > 0.7f
                    ? "aligned (forward or switch)" : "sideways: review";
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

        public void ResetRider()
        {
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            bufferedJump = 0f;
            lastContact = float.NegativeInfinity;
            contact = wasContact = false;
            normal = Vector3.up;
            landing = "--";
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(12, 12, 410, 142), "PHYSICS LAB — diagnostic prototype");
            GUI.Label(new Rect(24, 40, 390, 105),
                $"Speed: {body.linearVelocity.magnitude * 3.6f:F1} km/h   Contact: {contact}\n" +
                $"Normal: {normal:F2}   Slip: {slip:F2} m/s\n" +
                $"Steer: {steer:F2}   Grip acceleration: {gripForce:F1} m/s²\n" +
                $"Landing: {landing}\nA/D steer  S brake  Space pop  R reset");
        }
    }
}
