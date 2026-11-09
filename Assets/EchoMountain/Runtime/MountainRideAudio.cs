using UnityEngine;

namespace EchoMountain.Riding
{
    [RequireComponent(typeof(MountainRun))]
    internal sealed class MountainRideAudio : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private MountainRun run;
        private LabRider rider;
        private LabHud hud;
        private AudioSource wind, snow, touchdown;
        private int lastLanding;
        private bool playing;

        private void Awake()
        {
            run = GetComponent<MountainRun>();
            rider = GetComponent<LabRider>();
            hud = GetComponent<LabHud>();
            wind = AddSource(NoiseLoop("Alpine wind", 79, 0.025f, false), true);
            snow = AddSource(NoiseLoop("Board on snow", 319, 0.24f, true), true);
            touchdown = AddSource(Impact(), false);
        }

        private void Update()
        {
            int landings = rider.LandingCount;
            bool newLanding = landings > lastLanding;
            lastLanding = landings;

            bool audible = run.SoundEnabled && run.Phase == MountainPhase.Riding && !hud.IsPaused;
            if (!audible)
            {
                if (playing) Stop();
                return;
            }

            if (!playing)
            {
                wind.Play();
                snow.Play();
                playing = true;
            }

            float speed = Mathf.InverseLerp(3f, 55f, rider.SpeedKmh);
            float skid = Mathf.InverseLerp(0.35f, 6f, Mathf.Abs(rider.LateralSlip));
            float blend = 1f - Mathf.Exp(-9f * Time.unscaledDeltaTime);
            wind.volume = Mathf.Lerp(wind.volume, 0.025f + speed * 0.25f, blend);
            snow.volume = Mathf.Lerp(snow.volume, rider.IsGrounded ? speed * (0.19f + skid * 0.22f) : 0f, blend);
            snow.pitch = 0.80f + speed * 0.42f;
            if (newLanding)
            {
                touchdown.volume = rider.LastLanding == LandingOutcome.Clean ? 0.23f : 0.36f;
                touchdown.Play();
            }
        }

        private void Stop()
        {
            wind.Stop();
            snow.Stop();
            touchdown.Stop();
            wind.volume = snow.volume = 0f;
            playing = false;
        }

        private void OnDisable()
        {
            if (playing) Stop();
        }

        private void OnDestroy()
        {
            Destroy(wind.clip);
            Destroy(snow.clip);
            Destroy(touchdown.clip);
        }

        private AudioSource AddSource(AudioClip clip, bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = loop;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = 0f;
            return source;
        }

        private static AudioClip NoiseLoop(string name, int seed, float response, bool board)
        {
            var random = new System.Random(seed);
            var samples = new float[SampleRate * 3];
            float filtered = 0f;
            float slow = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float noise = (float)random.NextDouble() * 2f - 1f;
                filtered += response * (noise - filtered);
                slow += 0.006f * (filtered - slow);
                float texture = board ? (filtered - slow) * 0.72f : filtered * 0.95f;
                float seam = Mathf.Clamp01(Mathf.Min(i, samples.Length - i - 1) / 450f);
                samples[i] = texture * seam;
            }
            return Clip(name, samples);
        }

        private static AudioClip Impact()
        {
            var random = new System.Random(1031);
            var samples = new float[SampleRate / 4];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / SampleRate;
                float noise = (float)random.NextDouble() * 2f - 1f;
                float impact = Mathf.Sin(2f * Mathf.PI * (76f - 100f * t) * t) * Mathf.Exp(-18f * t);
                samples[i] = (impact * 0.61f + noise * 0.25f * Mathf.Exp(-27f * t))
                    * Mathf.Clamp01(i / 110f) * Mathf.Clamp01((samples.Length - i - 1) / 220f);
            }
            return Clip("Snow landing", samples);
        }

        private static AudioClip Clip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
