using UnityEngine;

namespace EchoMountain.Riding
{
    public sealed class LabCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        private Transform followedTarget;
        private Rigidbody targetBody;
        private LabRider rider;
        private uint lastReset;
        private float heading;
        private Vector3 lastTargetPosition;
        private bool forceSnap, replayTravel;
        private Vector3 recordedVelocity;
        private readonly RaycastHit[] obstructionHits = new RaycastHit[32];
        private readonly Collider[] overlaps = new Collider[32];
        private const float CameraRadius = 0.32f;

        public void SnapNextFrame() => forceSnap = true;
        public void SetReplayTravel(Vector3 velocity) { replayTravel = true; recordedVelocity = velocity; }
        public void ClearReplayTravel() => replayTravel = false;

        public void SetTarget(Transform value) => target = value;

        private void LateUpdate()
        {
            if (!target) return;

            bool changed = target != followedTarget;
            bool snap = changed || forceSnap;
            forceSnap = false;
            if (changed)
            {
                followedTarget = target;
                targetBody = target.GetComponent<Rigidbody>();
                rider = target.GetComponent<LabRider>();
            }

            if (rider && lastReset != rider.ResetCount)
            {
                lastReset = rider.ResetCount;
                snap = true;
            }

            Vector3 trackingPosition = targetBody ? targetBody.position : target.position;
            // Course menu transitions can teleport the board without a rider reset.
            snap |= (trackingPosition - lastTargetPosition).sqrMagnitude > 32f * 32f;
            lastTargetPosition = trackingPosition;

            Vector3 travel = replayTravel ? Vector3.ProjectOnPlane(recordedVelocity, Vector3.up) : targetBody && !targetBody.isKinematic
                ? Vector3.ProjectOnPlane(targetBody.linearVelocity, Vector3.up)
                : Vector3.zero;
            float speed = travel.magnitude;
            float damping = 1f - Mathf.Exp(-5f * Time.unscaledDeltaTime);
            if (speed > 1f)
            {
                float travelHeading = Mathf.Atan2(travel.x, travel.z) * Mathf.Rad2Deg;
                heading = snap ? travelHeading : Mathf.LerpAngle(heading, travelHeading, damping);
            }
            else if (snap)
            {
                Vector3 forward = (targetBody ? targetBody.rotation : target.rotation) * Vector3.forward;
                heading = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            }

            Vector3 direction = Quaternion.Euler(0f, heading, 0f) * Vector3.forward;
            Vector3 position = snap ? trackingPosition : target.position;
            Vector3 desired = position - direction * 6.5f + Vector3.up * 3f;
            Vector3 candidate = snap ? desired : Vector3.Lerp(transform.position, desired, damping);
            // Constrain after damping: new obstacles must pull the view in immediately.
            // Moving back out remains damped, avoiding a sudden zoom when a tree clears.
            transform.position = ResolveObstruction(position + Vector3.up * 0.9f, candidate);
            float lookAhead = Mathf.Lerp(3.5f, 8f, Mathf.InverseLerp(2f, 22f, speed));
            transform.LookAt(position + direction * lookAhead + Vector3.up * 0.7f, Vector3.up);
        }

        private bool IsRider(Collider value) => value.transform == target ||
            value.transform.IsChildOf(target) || (targetBody && value.attachedRigidbody == targetBody);

        private Vector3 ResolveObstruction(Vector3 pivot, Vector3 candidate)
        {
            // Sphere casts do not reliably report an initial overlap. If the pivot is
            // obstructed, keep the view at the rider rather than casting through it.
            int count = Physics.OverlapSphereNonAlloc(pivot, CameraRadius, overlaps,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return pivot;
            for (int i = 0; i < count; i++)
                if (!IsRider(overlaps[i])) return pivot;

            Vector3 offset = candidate - pivot;
            float distance = offset.magnitude;
            if (distance < 0.001f) return pivot;
            Vector3 direction = offset / distance;
            count = Physics.SphereCastNonAlloc(pivot, CameraRadius, direction, obstructionHits,
                distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            // Results are unordered; a saturated buffer may omit the nearest wall.
            if (count == obstructionHits.Length) return pivot;
            for (int i = 0; i < count; i++)
                if (!IsRider(obstructionHits[i].collider))
                    distance = Mathf.Min(distance, Mathf.Max(0f, obstructionHits[i].distance - 0.06f));
            return pivot + direction * distance;
        }
    }
}
