using UnityEngine;
using UnityEngine.Rendering;

namespace EchoMountain.Riding
{
    // Cosmetic contact feedback only; no snow deformation or physics forces.
    [RequireComponent(typeof(MountainRun))]
    public sealed class MountainSnowSpray : MonoBehaviour
    {
        public const int ParticleLimit = 160;
        public int ParticleCount => particles ? particles.particleCount : 0;
        private MountainRun run;
        private LabRider rider;
        private LabHud hud;
        private Rigidbody body;
        private ParticleSystem particles;
        private Material material;
        private Texture2D texture;
        private float emissionRemainder;
        private uint lastReset;
        private int lastLanding;
        private readonly System.Random random = new(1789);

        private void Awake()
        {
            run = GetComponent<MountainRun>();
            rider = GetComponent<LabRider>();
            hud = GetComponent<LabHud>();
            body = GetComponent<Rigidbody>();
            var source = Resources.Load<Material>("EchoMountainSnowSpray");
            if (!source) { enabled = false; return; }
            material = new Material(source);
            texture = SnowTexture();
            material.SetTexture("_BaseMap", texture);
            var effect = new GameObject("Contact snow spray");
            effect.transform.SetParent(transform, false);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = ParticleLimit;
            main.startLifetime = 0.65f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.45f;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(0.82f, 0.92f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0.35f, 0.65f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            if (!particles) return;
            if (run.Phase != MountainPhase.Riding)
            {
                Clear();
                return;
            }
            if (hud.IsPaused)
            {
                if (!particles.isPaused) particles.Pause();
                return;
            }
            if (lastReset != rider.ResetCount)
            {
                Clear();
                lastReset = rider.ResetCount;
                lastLanding = rider.LandingCount;
            }
            if (!particles.isPlaying) particles.Play();
            bool landed = rider.LandingCount > lastLanding;
            lastLanding = rider.LandingCount;
            if (!rider.IsGrounded || rider.IsRecovering) { emissionRemainder = 0f; return; }
            if (landed) Emit(14, true);
            float speed = Mathf.InverseLerp(5f, 50f, rider.SpeedKmh);
            float skid = Mathf.Clamp01(Mathf.Abs(rider.LateralSlip) / 5f);
            float edge = Mathf.Abs(rider.SteerInput);
            emissionRemainder += Time.deltaTime * speed * (12f + 38f * Mathf.Max(skid, edge));
            int count = Mathf.Min(4, Mathf.FloorToInt(emissionRemainder));
            emissionRemainder -= Mathf.Floor(emissionRemainder);
            Emit(count, false);
        }

        private void Emit(int count, bool landing)
        {
            for (int index = 0; index < count; index++)
            {
                float side = random.NextDouble() > 0.5 ? 1f : -1f;
                Vector3 lateral = transform.right * side;
                float scatter = (float)random.NextDouble();
                var particle = new ParticleSystem.EmitParams
                {
                    position = transform.position - rider.ContactNormal * 0.05f + lateral * 0.23f,
                    velocity = body.linearVelocity * 0.12f + rider.ContactNormal * (0.45f + scatter * (landing ? 1.8f : 0.8f))
                        + lateral * (landing ? 1.7f : 0.5f + Mathf.Abs(rider.LateralSlip) * 0.15f),
                    startSize = 0.05f + scatter * (landing ? 0.15f : 0.09f),
                    startLifetime = 0.40f + scatter * 0.35f
                };
                particles.Emit(particle, 1);
            }
        }

        private void Clear()
        {
            emissionRemainder = 0f;
            if (particles && (particles.isPlaying || particles.isPaused || particles.particleCount > 0))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static Texture2D SnowTexture()
        {
            var result = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "Original snow puff", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    Vector2 point = new((x + 0.5f) / 8f - 1f, (y + 0.5f) / 8f - 1f);
                    float alpha = Mathf.Clamp01(1f - point.sqrMagnitude);
                    pixels[y * 16 + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            result.SetPixels(pixels);
            result.Apply(false, true);
            return result;
        }

        private void OnDisable() => Clear();
        private void OnDestroy()
        {
            if (particles) Destroy(particles.gameObject);
            Destroy(material);
            Destroy(texture);
        }
    }
}
