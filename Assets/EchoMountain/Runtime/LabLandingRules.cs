using UnityEngine;

namespace EchoMountain.Riding
{
    public enum LandingOutcome { Clean, Recover, Bail }

    public static class LabLandingRules
    {
        // The alignment uses absolute board/travel direction: switch and regular are equivalent.
        public static LandingOutcome Evaluate(float uprightAlignment, float travelAlignment,
            float tangentSpeed, float impactSpeed, float residualSpin, int supportingProbes)
        {
            if (!IsFinite(uprightAlignment) || !IsFinite(travelAlignment) || !IsFinite(tangentSpeed) ||
                !IsFinite(impactSpeed) || !IsFinite(residualSpin) || supportingProbes < 1)
                return LandingOutcome.Bail;
            float direction = tangentSpeed < 1f ? 1f : Mathf.Abs(travelAlignment);
            if (supportingProbes >= 2 && uprightAlignment >= 0.9f && direction >= 0.8f &&
                impactSpeed <= 5f && residualSpin <= 3f)
                return LandingOutcome.Clean;
            if (uprightAlignment >= 0.65f && direction >= 0.35f && impactSpeed <= 9f && residualSpin <= 6f)
                return LandingOutcome.Recover;
            return LandingOutcome.Bail;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
