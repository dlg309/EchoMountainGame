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
        private float indyHold, melonHold;
        private int previousSpin;
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
        public int PendingPoints => IsAirborne ? BasePoints(SpinDegrees(), GrabHeld()) : 0;

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
            CurrentGrab = grab;
            if (grab == BoardGrab.Indy) indyHold += seconds;
            if (grab == BoardGrab.Melon) melonHold += seconds;
        }

        public void Land(float heading, LandingOutcome outcome)
        {
            if (!IsAirborne) return;

            TrackHeading(heading);
            int spin = SpinDegrees();
            BoardGrab grab = GrabHeld();
            int basis = BasePoints(spin, grab);
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
            bool repeated = spin == previousSpin && grab == previousGrab;
            bool clean = outcome == LandingOutcome.Clean;
            Chain = clean ? repeated ? 1 : Mathf.Min(Chain + 1, 4) : 0;
            ChainTime = clean ? ChainWindow : 0f;

            LastAward = Mathf.RoundToInt(basis * (clean ? 1f : 0.5f) * (repeated ? 0.6f : 1f) * Multiplier);
            Points += LastAward;
            previousSpin = spin;
            previousGrab = grab;

            string trick = spin == 0 ? grab.ToString() : grab == BoardGrab.None ? spin.ToString() : $"{spin} {grab}";
            string landing = clean ? repeated ? "Repeat" : "Clean" : "Recovered";
            Feedback = $"{trick}   /   {landing}   +{LastAward}";
            FeedbackTime = 2.8f;
        }

        public void CancelAir()
        {
            IsAirborne = false;
            CurrentGrab = BoardGrab.None;
            rotation = indyHold = melonHold = 0f;
        }

        public void Reset()
        {
            CancelAir();
            Points = Chain = LastAward = previousSpin = 0;
            previousGrab = BoardGrab.None;
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
