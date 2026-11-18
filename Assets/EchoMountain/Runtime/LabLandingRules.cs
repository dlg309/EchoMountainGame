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
            if (supportingProbes >= 2 && uprightAlignment >= 0.87f && direction >= 0.75f &&
                impactSpeed <= 5.5f && residualSpin <= 4f)
                return LandingOutcome.Clean;
            if (uprightAlignment >= 0.6f && direction >= 0.25f && impactSpeed <= 9f && residualSpin <= 10f)
                return LandingOutcome.Recover;
            return LandingOutcome.Bail;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
