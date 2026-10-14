using System.Collections.Generic;
using UnityEngine;

namespace Temnorid.Arena.Spells
{
    /// <summary>Simple shapes for spells until there is art: a glowing ball, an expanding ring, a bubble, a flash.</summary>
    internal static class SpellEffects
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();

        public static Material MaterialFor(Color color)
        {
            if (!Materials.TryGetValue(color, out var material) || material == null)
            {
                // Builds only keep shaders some material uses; Lit always ships with URP.
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Unlit/Color");
                material = new Material(shader) { color = color, name = "Spell" };
                material.SetColor("_BaseColor", color);
                Materials[color] = material;
            }

            return material;
        }

        public static GameObject Ball(Vector3 position, float radius, Color color)
        {
            var go = Shape(PrimitiveType.Sphere, color);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * radius * 2f;
            return go;
        }

        /// <summary>A ring growing to a radius and fading, e.g. a nova.</summary>
        public static void Ring(Vector3 center, float radius, Color color)
        {
            var go = Shape(PrimitiveType.Cylinder, color);
            go.transform.position = center + Vector3.up * 0.1f;
            go.AddComponent<Grow>().Begin(new Vector3(radius * 2f, 0.05f, radius * 2f), 0.35f);
        }

        /// <summary>A short flash, e.g. where a blink starts and ends or a projectile hits.</summary>
        public static void Flash(Vector3 position, float size, Color color)
        {
            var go = Shape(PrimitiveType.Sphere, color);
            go.transform.position = position;
            go.AddComponent<Grow>().Begin(Vector3.one * size, 0.2f);
        }

        public static GameObject Bubble(Transform owner, Color color)
        {
            var go = Shape(PrimitiveType.Sphere, new Color(color.r, color.g, color.b, 1f));
            go.name = "Shield";
            go.transform.SetParent(owner, false);
            go.transform.localPosition = Vector3.up * 0.9f;
            go.transform.localScale = new Vector3(1.4f, 2f, 1.4f);
            return go;
        }

        private static GameObject Shape(PrimitiveType type, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = "Spell effect";
            Object.Destroy(go.GetComponent<Collider>());
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = MaterialFor(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private sealed class Grow : MonoBehaviour
        {
            private Vector3 _size;
            private float _seconds;
            private float _time;

            public void Begin(Vector3 size, float seconds)
            {
                _size = size;
                _seconds = seconds;
                transform.localScale = size * 0.1f;
            }

            private void Update()
            {
                _time += Time.deltaTime;
                var t = Mathf.Clamp01(_time / _seconds);
                transform.localScale = Vector3.Lerp(_size * 0.1f, _size, t);
                if (t >= 1f)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
