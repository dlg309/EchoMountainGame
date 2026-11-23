using UnityEngine;

namespace EchoMountain.Riding
{
    // Only a descending, aligned board can enter. A rail controls travel until the board pops or rides off.
    internal sealed class FreeRoamRailRide
    {
        private const float BoardClearance = 0.11f;
        private readonly FreeRoamMap map;
        private readonly Rigidbody body;
        private Vector3 lastPosition, origin, direction;
        private float distance, length, speed, duration;
        private bool hasLastPosition, usedGravity;

        internal bool IsGrinding { get; private set; }
        internal Vector3 Velocity => direction * speed;

        internal FreeRoamRailRide(FreeRoamMap map, Rigidbody body)
        {
            this.map = map;
            this.body = body;
        }

        internal bool Step(bool canEnter, bool pop, float popVelocity, TrickScore tricks)
        {
            Vector3 current = body.position;
            if (!IsGrinding)
            {
                if (canEnter && hasLastPosition && body.linearVelocity.y < 0f)
                    TryEnter(lastPosition, current, tricks);
                lastPosition = current;
                hasLastPosition = true;
                return IsGrinding;
            }

            float step = Time.fixedDeltaTime;
            duration += step;
            speed = Mathf.Max(4f, speed + (-direction.y * 9.81f - 0.35f) * step);
            distance = Mathf.Clamp(distance + speed * step, 0f, length);
            Vector3 next = origin + direction * distance + Vector3.up * BoardClearance;
            if (pop || distance >= length)
            {
                body.position = lastPosition = next;
                Stop();
                body.linearVelocity = direction * speed + Vector3.up * (pop ? popVelocity : 0f);
                tricks.FinishGrind(body.rotation.eulerAngles.y, duration);
            }
            else body.MovePosition(next);
            return true;
        }

        internal void Reset()
        {
            Stop();
            hasLastPosition = false;
        }

        private void Stop()
        {
            if (!IsGrinding) return;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            body.isKinematic = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.useGravity = usedGravity;
            IsGrinding = false;
        }

        private void TryEnter(Vector3 previous, Vector3 current, TrickScore tricks)
        {
            var velocity = body.linearVelocity;
            foreach (var rail in map.Rails)
            {
                Vector3 span = rail.end - rail.start;
                Vector3 flat = Vector3.ProjectOnPlane(span, Vector3.up);
                float along = Vector3.Dot(current - rail.start, flat) / flat.sqrMagnitude;
                if (along < 0f || along > 1f) continue;
                Vector3 top = rail.start + along * span + Vector3.up * BoardClearance;
                if ((current - top).sqrMagnitude > rail.halfWidth * rail.halfWidth + 0.36f) continue;
                float oldAlong = Vector3.Dot(previous - rail.start, flat) / flat.sqrMagnitude;
                float before = previous.y - (rail.start.y + oldAlong * span.y + BoardClearance);
                float after = current.y - top.y;
                if (before < 0f || after > 0f) continue;

                Vector3 cross = Vector3.Lerp(previous, current, before / Mathf.Max(0.0001f, before - after));
                float entry = Vector3.Dot(cross - rail.start, flat) / flat.sqrMagnitude;
                Vector3 point = rail.start + span * entry + Vector3.up * BoardClearance;
                if (entry < 0f || entry > 1f || Vector3.ProjectOnPlane(cross - point, Vector3.up).magnitude > rail.halfWidth)
                    continue;

                Vector3 axis = span.normalized;
                float approach = Vector3.Dot(velocity, axis);
                if (Mathf.Abs(approach) < 4f || Mathf.Abs(approach) < velocity.magnitude * 0.70f) continue;
                Vector3 forward = body.rotation * Vector3.forward;
                if (Vector3.Dot(body.rotation * Vector3.up, Vector3.up) < 0.65f ||
                    Mathf.Abs(Vector3.Dot(forward, axis)) < 0.65f) continue;

                direction = axis * Mathf.Sign(approach);
                origin = approach > 0f ? rail.start : rail.end;
                length = span.magnitude;
                distance = Vector3.Dot(point - origin, direction);
                speed = Mathf.Abs(approach);
                duration = 0f;
                body.position = point;
                body.rotation = Quaternion.LookRotation(direction * Mathf.Sign(Vector3.Dot(forward, direction)), Vector3.up);
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.isKinematic = true;
                usedGravity = body.useGravity;
                body.useGravity = false;
                tricks.BeginGrind(body.rotation.eulerAngles.y);
                IsGrinding = true;
                return;
            }
        }
    }
}
