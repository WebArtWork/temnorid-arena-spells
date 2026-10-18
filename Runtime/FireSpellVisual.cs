using UnityEngine;
using UnityEngine.Rendering;

namespace Temnorid.Arena.Spells
{
    /// <summary>Cosmetic combustion: a compact hot head sheds flames and embers into world space.</summary>
    internal sealed class FireSpellVisual : MonoBehaviour
    {
        private static Material _flameMaterial;
        private static Material _smokeMaterial;
        private Light _light;
        private ParticleSystem[] _systems;
        private bool _stopped;
        private float _flickerSeed;

        private void Awake() { _flickerSeed = Random.value * 100f; }

        public static GameObject Projectile(Vector3 position, Vector3 direction, float radius)
        {
            var root = new GameObject("Fireball");
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
            var visual = root.AddComponent<FireSpellVisual>();
            var core = visual.Emitter("Hot core", 180f, 0.055f, 0.1f, radius * 1.4f, 0.2f, false, false);
            var main = core.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var flames = visual.Emitter("Flame wake", 350f, 0.035f, 0.075f, radius * 2.2f, 0.5f, false, false);
            var velocity = flames.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 1.1f);
            visual.Emitter("Embers", 26f, 0.12f, 0.24f, 0.035f, 1.8f, false, true);
            visual._light = root.AddComponent<Light>();
            visual._light.color = new Color(1f, 0.36f, 0.07f);
            visual._light.range = 2.5f;
            visual._light.intensity = 1.8f;
            visual._light.shadows = LightShadows.None;
            visual._systems = root.GetComponentsInChildren<ParticleSystem>();
            foreach (var system in visual._systems) system.Play();
            return root;
        }

        public static void Impact(Vector3 position, Vector3 normal)
        {
            var root = new GameObject("Fire impact");
            root.transform.SetPositionAndRotation(position + normal * 0.035f, Quaternion.LookRotation(normal));
            var visual = root.AddComponent<FireSpellVisual>();
            visual.Burst(visual.Emitter("Flame burst", 0f, 0.09f, 0.23f, 0.42f, 2.2f, false, false), 18);
            visual.Burst(visual.Emitter("Impact embers", 0f, 0.2f, 0.55f, 0.035f, 4f, false, true), 22);
            var smoke = visual.Emitter("Cooling smoke", 0f, 0.35f, 0.7f, 0.55f, 0.45f, true, false);
            var velocity = smoke.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = 1f;
            visual.Burst(smoke, 7);
            if (Application.isPlaying) Destroy(root, 1.2f);
        }

        /// <summary>Leave the existing wake in place while it burns out, including at range expiry.</summary>
        public void Stop()
        {
            if (_stopped) return;
            _stopped = true;
            foreach (var system in _systems) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_light != null) _light.enabled = false;
            if (Application.isPlaying) Destroy(gameObject, 0.35f);
        }

        private void Update()
        {
            if (_light != null && !_stopped)
                _light.intensity = 1.5f + Mathf.PerlinNoise(Time.time * 22f, _flickerSeed) * 0.6f;
        }

        private void Burst(ParticleSystem system, short count)
        {
            var main = system.main;
            main.loop = false;
            var emission = system.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            system.Play();
        }

        private ParticleSystem Emitter(string label, float rate, float minLife, float maxLife,
            float size, float speed, bool smoke, bool ember)
        {
            var child = new GameObject(label);
            child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.playOnAwake = false;
            main.duration = 1f;
            main.loop = true;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(minLife, maxLife);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = ember ? 0.25f : 0f;
            var emission = system.emission;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = ember ? 0.12f : 0.08f;
            var color = system.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(smoke
                ? new[] { new GradientColorKey(new Color(0.16f, 0.12f, 0.1f), 0f), new GradientColorKey(new Color(0.1f, 0.1f, 0.11f), 1f) }
                : new[] { new GradientColorKey(new Color(2.4f, 1.8f, 0.65f), 0f), new GradientColorKey(new Color(1.6f, 0.48f, 0.035f), 0.3f), new GradientColorKey(new Color(0.7f, 0.055f, 0.005f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(smoke ? 0.23f : 0.85f, 0.12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            var scale = system.sizeOverLifetime;
            scale.enabled = true;
            scale.size = new ParticleSystem.MinMaxCurve(1f, smoke
                ? AnimationCurve.Linear(0f, 0.35f, 1f, 1.8f)
                : new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.22f, 1f), new Keyframe(1f, 0f)));
            var noise = system.noise;
            noise.enabled = !ember;
            noise.strength = smoke ? 0.18f : 0.35f;
            noise.frequency = 3f;
            noise.scrollSpeed = 2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = Material(smoke);
            renderer.renderMode = ember ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            renderer.velocityScale = 0.025f;
            renderer.lengthScale = 1.5f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        private static Material Material(bool smoke)
        {
            var cached = smoke ? _smokeMaterial : _flameMaterial;
            if (cached != null) return cached;
            // Resources keeps this shader in player builds, including mobile and XR variants.
            var shader = Resources.Load<Shader>("TemnoridFireParticle");
            cached = new Material(shader) { name = smoke ? "Fire smoke" : "Fire flame" };
            cached.SetFloat("_DstBlend", smoke ? (float)BlendMode.OneMinusSrcAlpha : (float)BlendMode.One);
            if (smoke) _smokeMaterial = cached;
            else _flameMaterial = cached;
            return cached;
        }
    }
}
