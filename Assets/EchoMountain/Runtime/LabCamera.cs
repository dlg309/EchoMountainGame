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

        public void SetTarget(Transform value) => target = value;

        private void LateUpdate()
        {
            if (!target) return;

            bool snap = target != followedTarget;
            if (snap)
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

            Vector3 travel = targetBody
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
            Vector3 position = snap && targetBody ? targetBody.position : target.position;
            Vector3 desired = position - direction * 6.5f + Vector3.up * 3f;
            transform.position = snap ? desired : Vector3.Lerp(transform.position, desired, damping);
            float lookAhead = Mathf.Lerp(3.5f, 8f, Mathf.InverseLerp(2f, 22f, speed));
            transform.LookAt(position + direction * lookAhead + Vector3.up * 0.7f, Vector3.up);
        }
    }
}
