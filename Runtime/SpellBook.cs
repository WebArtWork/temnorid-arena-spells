using System;
using System.Collections.Generic;
using UnityEngine;

namespace Temnorid.Arena.Spells
{
    public enum SpellKind
    {
        /// <summary>Flies straight, damages the first combatant it hits.</summary>
        Projectile,

        /// <summary>Damages everyone around the caster.</summary>
        Nova,

        /// <summary>Absorbs damage on the caster for a while.</summary>
        Shield,

        /// <summary>Teleports the caster forward, stopping at walls.</summary>
        Blink,
    }

    /// <summary>One spell. Balance is data: change the numbers here, not the code that casts.</summary>
    [Serializable]
    public class SpellDefinition
    {
        public string Id;
        public string Name;
        public SpellKind Kind;
        public float ManaCost;
        public float Cooldown;

        [Tooltip("Projectile and nova damage.")]
        public float Damage;

        [Tooltip("Projectile speed, m/s.")]
        public float Speed;

        [Tooltip("Projectile size or nova radius, m.")]
        public float Radius;

        [Tooltip("Projectile reach or blink distance, m.")]
        public float Range;

        [Tooltip("Shield amount.")]
        public float Amount;

        [Tooltip("Shield seconds.")]
        public float Duration;

        public Color Color = Color.white;
    }

    /// <summary>The spells every combatant carries in v0.1, in hotbar order (keys 1-4).</summary>
    public static class SpellBook
    {
        public static readonly IReadOnlyList<SpellDefinition> Starter = new List<SpellDefinition>
        {
            new SpellDefinition
            {
                Id = "fireball", Name = "Fireball", Kind = SpellKind.Projectile,
                ManaCost = 12f, Cooldown = 0.5f, Damage = 25f, Speed = 32f, Radius = 0.25f, Range = 60f,
                Color = new Color(1f, 0.45f, 0.1f),
            },
            new SpellDefinition
            {
                Id = "frost-nova", Name = "Frost nova", Kind = SpellKind.Nova,
                ManaCost = 30f, Cooldown = 6f, Damage = 35f, Radius = 6f,
                Color = new Color(0.45f, 0.8f, 1f),
            },
            new SpellDefinition
            {
                Id = "arcane-shield", Name = "Arcane shield", Kind = SpellKind.Shield,
                ManaCost = 25f, Cooldown = 12f, Amount = 50f, Duration = 5f,
                Color = new Color(0.7f, 0.4f, 1f),
            },
            new SpellDefinition
            {
                Id = "blink", Name = "Blink", Kind = SpellKind.Blink,
                ManaCost = 20f, Cooldown = 5f, Range = 9f,
                Color = new Color(0.9f, 0.9f, 1f),
            },
        };
    }
}
