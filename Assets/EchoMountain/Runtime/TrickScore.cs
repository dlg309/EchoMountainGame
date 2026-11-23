using UnityEngine;

namespace EchoMountain.Riding
{
    public enum BoardGrab { None, Indy, Melon }

    public sealed class TrickScore
    {
        private const float GrabHold = 0.15f;
        private const float ChainWindow = 8f;
        private float previousHeading;
        private float rotation;
        private float currentHold, indyHold, melonHold;
        private int previousSpin;
        private int railPoints, previousRail;
        private BoardGrab previousGrab;

        public bool IsAirborne { get; private set; }
        public float Rotation => rotation;
        public BoardGrab CurrentGrab { get; private set; }
        public int Points { get; private set; }
        public int Chain { get; private set; }
        public float ChainTime { get; private set; }
        public float Multiplier => 1f + 0.25f * Mathf.Max(0, Chain - 1);
        public int LastAward { get; private set; }
        public string Feedback { get; private set; } = "";
        public float FeedbackTime { get; private set; }
        public int PendingPoints => IsAirborne ? BasePoints(SpinDegrees(), GrabHeld()) + railPoints : 0;

        internal void BeginGrind(float heading)
        {
            IsAirborne = true;
            previousHeading = heading;
            CurrentGrab = BoardGrab.None;
            currentHold = 0f;
        }

        internal void FinishGrind(float heading, float duration)
        {
            previousHeading = heading;
            if (duration >= 0.2f)
                railPoints += Mathf.Min(180, Mathf.RoundToInt(65f + 55f * duration));
        }

        public void Tick(float seconds)
        {
            ChainTime = Mathf.Max(0f, ChainTime - seconds);
            FeedbackTime = Mathf.Max(0f, FeedbackTime - seconds);
            if (ChainTime == 0f) Chain = 0;
        }

        public void SampleAir(float heading, BoardGrab grab, float seconds)
        {
            if (!IsAirborne)
            {
                IsAirborne = true;
                previousHeading = heading;
            }

            TrackHeading(heading);
            if (grab != CurrentGrab) currentHold = 0f;
            CurrentGrab = grab;
            if (grab == BoardGrab.None) return;

            currentHold += seconds;
            if (grab == BoardGrab.Indy) indyHold = Mathf.Max(indyHold, currentHold);
            if (grab == BoardGrab.Melon) melonHold = Mathf.Max(melonHold, currentHold);
        }

        public void Land(float heading, LandingOutcome outcome)
        {
            if (!IsAirborne) return;

            TrackHeading(heading);
            int spin = SpinDegrees();
            BoardGrab grab = GrabHeld();
            int rail = railPoints;
            int basis = BasePoints(spin, grab) + rail;
            CancelAir();
            if (outcome == LandingOutcome.Bail)
            {
                LastAward = 0;
                Chain = 0;
                ChainTime = 0f;
                Feedback = "Bail   /   0 pts";
                FeedbackTime = 2.8f;
                return;
            }

            if (basis == 0) return;
            bool repeated = spin == previousSpin && grab == previousGrab && (rail > 0) == (previousRail > 0);
            bool clean = outcome == LandingOutcome.Clean;
            Chain = clean ? repeated ? 1 : Mathf.Min(Chain + 1, 4) : 0;
            ChainTime = clean ? ChainWindow : 0f;

            LastAward = Mathf.RoundToInt(basis * (clean ? 1f : 0.5f) * (repeated ? 0.6f : 1f) * Multiplier);
            Points += LastAward;
            previousSpin = spin;
            previousGrab = grab;
            previousRail = rail;

            string trick = spin == 0 ? (grab == BoardGrab.None ? "" : grab.ToString())
                : grab == BoardGrab.None ? spin.ToString() : $"{spin} {grab}";
            if (rail > 0) trick = trick.Length == 0 ? "Grind" : $"Grind + {trick}";
            string landing = clean ? repeated ? "Repeat" : "Clean" : "Recovered";
            Feedback = $"{trick}   /   {landing}   +{LastAward}";
            FeedbackTime = 2.8f;
        }

        public void CancelAir()
        {
            IsAirborne = false;
            railPoints = 0;
            CurrentGrab = BoardGrab.None;
            rotation = currentHold = indyHold = melonHold = 0f;
        }

        public void Reset(bool preservePoints = false)
        {
            CancelAir();
            if (!preservePoints)
            {
                Points = previousSpin = previousRail = 0;
                previousGrab = BoardGrab.None;
            }
            Chain = LastAward = 0;
            ChainTime = FeedbackTime = 0f;
            Feedback = "";
        }

        private void TrackHeading(float heading)
        {
            rotation += Mathf.DeltaAngle(previousHeading, heading);
            previousHeading = heading;
        }

        private int SpinDegrees()
        {
            float angle = Mathf.Abs(rotation);
            if (Mathf.Abs(angle - 360f) <= 30f) return 360;
            if (Mathf.Abs(angle - 180f) <= 30f) return 180;
            return 0;
        }

        private BoardGrab GrabHeld()
        {
            if (indyHold >= GrabHold && indyHold >= melonHold) return BoardGrab.Indy;
            return melonHold >= GrabHold ? BoardGrab.Melon : BoardGrab.None;
        }

        private static int BasePoints(int spin, BoardGrab grab) =>
            spin + (grab == BoardGrab.Indy ? 80 : grab == BoardGrab.Melon ? 90 : 0);
    }
}
