using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Temnorid.Arena.Spells
{
    /// <summary>Cosmetic frost, shielding and displacement. Contains no gameplay or collision logic.</summary>
    internal sealed class ElementalSpellVisual : MonoBehaviour
    {
        private static Material _energy;
        private static Mesh _band;
        private static Mesh _shard;
        private readonly List<Mesh> _ownedMeshes = new List<Mesh>();
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private Color _color;
        private float _age;
        private float _duration;
        private float _radius;
        private int _kind;

        public static GameObject Nova(Vector3 center, float radius, Color color)
        {
            var visual = Create("Frost nova", center + Vector3.up * 0.06f, color, 0, 0.75f);
            visual._radius = radius;
            visual.Part("Freezing wave", Band(), Vector3.zero, new Vector3(radius, 1f, radius), false);
            visual.Part("Inner frost edge", Band(), Vector3.up * 0.02f, new Vector3(radius * 0.91f, 0.6f, radius * 0.91f), false);
            // Reuse a single faceted shard mesh; no individual material allocations.
            for (var i = 0; i < 32; i++)
            {
                var angle = i * Mathf.PI * 2f / 32f;
                var distance = radius * (0.82f + 0.13f * Mathf.Sin(i * 2.39f));
                var shard = visual.Part("Ice splinter", Shard(), new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance),
                    new Vector3(0.09f, 0.24f + 0.35f * Mathf.Abs(Mathf.Sin(i * 3.1f)), 0.11f), false);
                shard.transform.rotation = Quaternion.Euler(12f * Mathf.Sin(i), i * 73f, 15f * Mathf.Cos(i));
            }
            visual.Motes(80, radius * 0.85f, 0.18f, 0.8f, color, true);
            visual.Ready();
            return visual.gameObject;
        }

        public static GameObject Shield(Transform owner, Color color)
        {
            var visual = Create("Arcane shield", owner.position, color, 1, 0f);
            visual.transform.SetParent(owner, false);
            visual.transform.localPosition = Vector3.up * 0.9f;
            var shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var collider = shell.GetComponent<Collider>();
            if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider);
            shell.name = "Transparent field";
            shell.transform.SetParent(visual.transform, false);
            shell.transform.localScale = new Vector3(1.45f, 2.05f, 1.45f);
            visual.Style(shell.GetComponent<Renderer>(), true);
            for (var i = 0; i < 3; i++)
            {
                var points = new Vector3[65];
                for (var j = 0; j < points.Length; j++)
                {
                    var a = j * Mathf.PI * 2f / (points.Length - 1);
                    points[j] = new Vector3(Mathf.Cos(a) * 0.73f, Mathf.Sin(a) * 1.03f, 0f);
                }
                var arc = visual.Ribbon("Flowing field arc", points, 0.008f);
                arc.transform.localRotation = Quaternion.Euler(0f, i * 60f, 15f);
            }
            visual.Ready();
            return visual.gameObject;
        }

        public static GameObject Blink(Vector3 position, bool arriving)
        {
            var color = arriving ? new Color(0.4f, 0.8f, 1f) : new Color(0.65f, 0.46f, 1f);
            var visual = Create(arriving ? "Blink arrival" : "Blink departure", position, color, 2, 0.5f);
            visual._radius = arriving ? 1f : -1f;
            for (var i = 0; i < 7; i++)
            {
                var points = new Vector3[40];
                for (var j = 0; j < points.Length; j++)
                {
                    var t = j / (float)(points.Length - 1);
                    var a = t * Mathf.PI * 2.2f + i * Mathf.PI * 2f / 7f;
                    var r = 0.15f + Mathf.Sin(t * Mathf.PI) * (0.42f + 0.035f * Mathf.Sin(t * 23f + i * 2.1f));
                    points[j] = new Vector3(Mathf.Cos(a) * r, t * 1.95f, Mathf.Sin(a) * r);
                }
                visual.Ribbon("Displacement wisp", points, 0.018f);
            }
            visual.Part("Ground remnant", Band(), Vector3.up * 0.04f, new Vector3(0.62f, 0.4f, 0.62f), false);
            visual.Motes(36, 0.48f, 1.6f, 0.05f, color, false);
            visual.Ready();
            return visual.gameObject;
        }

        private static ElementalSpellVisual Create(string label, Vector3 position, Color color, int kind, float duration)
        {
            if (_energy == null) _energy = new Material(Resources.Load<Shader>("TemnoridElemental")) { name = "Elemental energy" };
            var root = new GameObject(label);
            root.transform.position = position;
            var visual = root.AddComponent<ElementalSpellVisual>();
            visual._color = color;
            visual._kind = kind;
            visual._duration = duration;
            visual._properties = new MaterialPropertyBlock();
            return visual;
        }

        private void Ready()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            Sample(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Sample(_age);
            if (_duration > 0f && _age >= _duration) Destroy(gameObject);
        }

        /// <summary>Also used by the editor art review to sample the actual effect deterministically.</summary>
        public void Sample(float age)
        {
            var t = _duration > 0f ? Mathf.Clamp01(age / _duration) : 0f;
            var alpha = _duration > 0f ? Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.55f)) : 0.8f;
            if (_kind == 0)
                transform.localScale = Vector3.one * Mathf.Lerp(0.08f, 1f, Mathf.Clamp01(age / 0.35f));
            else if (_kind == 2)
            {
                var width = _radius > 0f ? Mathf.Lerp(0.5f, 1.15f, t) : Mathf.Lerp(1.1f, 0.12f, t);
                transform.localScale = new Vector3(width, 1f, width);
                transform.localRotation = Quaternion.Euler(0f, age * 80f * _radius, 0f);
            }
            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(_properties);
                _properties.SetColor("_Tint", new Color(_color.r, _color.g, _color.b, alpha));
                renderer.SetPropertyBlock(_properties);
            }
        }

        private void Style(Renderer renderer, bool membrane)
        {
            renderer.sharedMaterial = _energy;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            _properties.Clear();
            _properties.SetFloat("_Membrane", membrane ? 1f : 0f);
            _properties.SetColor("_Tint", _color);
            renderer.SetPropertyBlock(_properties);
        }

        private GameObject Part(string label, Mesh mesh, Vector3 position, Vector3 scale, bool membrane)
        {
            var go = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            Style(go.GetComponent<Renderer>(), membrane);
            return go;
        }

        private GameObject Ribbon(string label, Vector3[] points, float width)
        {
            var vertices = new Vector3[points.Length * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6];
            for (var i = 0; i < points.Length; i++)
            {
                var tangent = points[Mathf.Min(i + 1, points.Length - 1)] - points[Mathf.Max(0, i - 1)];
                var side = Vector3.Cross(tangent.normalized, (points[i] + Vector3.right * 0.01f).normalized).normalized;
                var taper = Mathf.Sin(Mathf.PI * i / (points.Length - 1));
                vertices[i * 2] = points[i] - side * width * taper;
                vertices[i * 2 + 1] = points[i] + side * width * taper;
                uv[i * 2] = new Vector2(i / (float)(points.Length - 1), 0f);
                uv[i * 2 + 1] = new Vector2(i / (float)(points.Length - 1), 1f);
                if (i == points.Length - 1) continue;
                var n = i * 2; var k = i * 6;
                triangles[k] = n; triangles[k + 1] = n + 2; triangles[k + 2] = n + 1;
                triangles[k + 3] = n + 1; triangles[k + 4] = n + 2; triangles[k + 5] = n + 3;
            }
            var mesh = new Mesh { name = label, vertices = vertices, uv = uv, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            _ownedMeshes.Add(mesh);
            return Part(label, mesh, Vector3.zero, Vector3.one, false);
        }

        private void Motes(short count, float radius, float height, float size, Color color, bool mist)
        {
            var child = new GameObject(mist ? "Cold ground mist" : "Residual motes");
            child.transform.SetParent(transform, false);
            var system = child.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.loop = false; main.playOnAwake = false;
            main.duration = 0.8f; main.startLifetime = Mathf.Min(0.65f, _duration); main.startSpeed = mist ? 0.15f : 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new Color(color.r, color.g, color.b, mist ? 0.2f : 0.8f);
            main.maxParticles = 96;
            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;
            shape.scale = new Vector3(radius, height * 0.5f, radius);
            shape.position = Vector3.up * height * 0.5f;
            var fade = system.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = child.GetComponent<ParticleSystemRenderer>();
            // The shared soft-particle shader keeps the same edge treatment as fire.
            renderer.sharedMaterial = MoteMaterial();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            system.Play();
        }

        private static Material _motes;
        private static Material MoteMaterial()
        {
            if (_motes == null) _motes = new Material(Resources.Load<Shader>("TemnoridFireParticle")) { name = "Elemental mist and motes" };
            return _motes;
        }

        private static Mesh Band()
        {
            if (_band != null) return _band;
            var vertices = new Vector3[192]; var uv = new Vector2[192]; var indices = new int[96 * 6];
            for (var i = 0; i < 96; i++)
            {
                var a = i * Mathf.PI * 2f / 96f;
                var direction = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                vertices[i * 2] = direction * 0.965f;
                vertices[i * 2 + 1] = direction * 1.015f;
                uv[i * 2] = new Vector2(i / 96f, 0f); uv[i * 2 + 1] = new Vector2(i / 96f, 1f);
                var n = i * 2; var next = (i + 1) % 96 * 2; var k = i * 6;
                indices[k] = n; indices[k + 1] = next; indices[k + 2] = n + 1;
                indices[k + 3] = n + 1; indices[k + 4] = next; indices[k + 5] = next + 1;
            }
            _band = new Mesh { name = "Soft wave band", vertices = vertices, uv = uv, triangles = indices };
            _band.RecalculateNormals(); _band.RecalculateBounds(); return _band;
        }

        private static Mesh Shard()
        {
            if (_shard != null) return _shard;
            _shard = new Mesh { name = "Faceted ice splinter", vertices = new[] { new Vector3(-1,0,-1), new Vector3(1,0,-1), new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(0.2f,1,0) },
                triangles = new[] { 0,4,1, 1,4,2, 2,4,3, 3,4,0, 0,1,2, 0,2,3 },
                uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up, new Vector2(0.5f,0.5f) } };
            _shard.RecalculateNormals(); _shard.RecalculateBounds(); return _shard;
        }

        private void OnDestroy()
        {
            foreach (var mesh in _ownedMeshes)
                if (Application.isPlaying) Destroy(mesh); else DestroyImmediate(mesh);
        }
    }
}
