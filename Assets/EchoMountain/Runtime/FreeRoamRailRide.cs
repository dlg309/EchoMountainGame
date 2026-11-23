using UnityEngine;

namespace EchoMountain.Riding
{
    // The cap collider carries the board. Bounded edge and alignment forces assist a valid grind.
    internal sealed class FreeRoamRailRide
    {
        private readonly FreeRoamMap map;
        private readonly Rigidbody body;
        private FreeRoamMap.Rail active, touched;
        private float directionSign, stanceSign, duration, lastContact, entryBlockedUntil;

        internal bool IsGrinding => active.surface;
        internal bool LeavingRail => Time.fixedTime < entryBlockedUntil;
        internal bool TouchingParkTop { get; private set; }

        internal FreeRoamRailRide(FreeRoamMap map, Rigidbody body)
        {
            this.map = map;
            this.body = body;
        }

        internal bool IsParkSurface(Collider collider) => map.TryGetRail(collider, out _);

        internal void ObserveContact(Collision collision)
        {
            if (!map.TryGetRail(collision.collider, out var rail)) return;
            Vector3 normal = rail.surface.transform.up;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Vector3.Dot(collision.GetContact(i).normal, normal) < 0.8f) continue;
                touched = rail;
                return;
            }
        }

        internal bool Step(bool canEnter, bool pop, float popVelocity, TrickScore tricks)
        {
            var contact = touched;
            touched = default;
            TouchingParkTop = contact.surface;
            if (!IsGrinding)
            {
                if (!canEnter || !contact.surface || Time.fixedTime < entryBlockedUntil ||
                    !TryEnter(contact)) return false;
                tricks.BeginGrind(body.rotation.eulerAngles.y);
            }

            Transform surface = active.surface.transform;
            if (contact.surface == active.surface) lastContact = Time.fixedTime;
            Vector3 along = surface.forward * directionSign;
            Vector3 normal = surface.up;
            float speed = Vector3.Dot(body.linearVelocity, along);
            bool supported = Time.fixedTime - lastContact <= 0.045f;
            if (!supported || speed < 1.8f || pop)
            {
                if (pop && supported) body.AddForce(normal * popVelocity, ForceMode.VelocityChange);
                tricks.FinishGrind(body.rotation.eulerAngles.y, duration);
                active = default;
                entryBlockedUntil = Time.fixedTime + 0.15f;
                return pop && supported;
            }

            duration += Time.fixedDeltaTime;
            Vector3 side = surface.right;
            float offset = Vector3.Dot(body.position - surface.position, side);
            float slip = Vector3.Dot(body.linearVelocity, side);
            float lateral = Mathf.Clamp(-offset * 36f - slip * 10f, -14f, 14f);
            body.AddForce(side * lateral - along * 0.35f, ForceMode.Acceleration);
            Vector3 forward = Vector3.ProjectOnPlane(body.rotation * Vector3.forward, normal);
            float yawError = Vector3.SignedAngle(forward, along * stanceSign, normal) * Mathf.Deg2Rad;
            Vector3 tiltError = Vector3.Cross(body.rotation * Vector3.up, normal);
            Vector3 correction = normal * (yawError * 28f) + tiltError * 26f - body.angularVelocity * 9f;
            body.AddTorque(Vector3.ClampMagnitude(correction, 38f), ForceMode.Acceleration);
            return true;
        }

        internal void Reset()
        {
            active = touched = default;
            entryBlockedUntil = 0f;
            TouchingParkTop = false;
        }

        private bool TryEnter(FreeRoamMap.Rail rail)
        {
            Transform surface = rail.surface.transform;
            Vector3 along = surface.forward;
            Vector3 normal = surface.up;
            Vector3 travel = Vector3.ProjectOnPlane(body.linearVelocity, normal);
            float forwardSpeed = Vector3.Dot(travel, along);
            Vector3 boardForward = body.rotation * Vector3.forward;
            float lateral = Mathf.Abs(surface.InverseTransformPoint(body.position).x - rail.surface.center.x);
            if (Mathf.Abs(forwardSpeed) < 3.3f || Mathf.Abs(forwardSpeed) < travel.magnitude * 0.80f ||
                Mathf.Abs(Vector3.Dot(boardForward, along)) < 0.78f ||
                Vector3.Dot(body.rotation * Vector3.up, normal) < 0.73f ||
                lateral > rail.surface.size.x * 0.5f) return false;

            active = rail;
            directionSign = Mathf.Sign(forwardSpeed);
            stanceSign = Mathf.Sign(Vector3.Dot(boardForward, along) * directionSign);
            duration = 0f;
            lastContact = Time.fixedTime;
            return true;
        }
    }
}
