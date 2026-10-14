using System;
using System.Collections.Generic;
using ITKamianets.Engine.Audio;
using ITKamianets.Engine.Character;
using ITKamianets.Engine.Input;
using Temnorid.Arena.Core;
using Unity.Netcode;
using UnityEngine;

namespace Temnorid.Arena.Spells
{
    /// <summary>
    /// Casts spells for a combatant, player or bot. The owner picks a spell (keys 1-4) and casts
    /// it (the main button) at what the camera looks at; the server checks it is alive, the
    /// cooldown and the mana, then casts: it flies projectiles, applies damage and teleports, so
    /// hits cannot be faked. Every client sees the effects. Bots cast through <see cref="ServerCast"/>.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    public class SpellCaster : NetworkBehaviour
    {
        [Tooltip("Read keys 1-4 and the main button on the owner. Off for bots.")]
        [SerializeField] private bool playerControlled = true;

        public IReadOnlyList<SpellDefinition> Spells => SpellBook.Starter;

        public int SelectedSlot { get; private set; }

        public event Action<int> SelectedChanged;

        /// <summary>Everyone: a spell was cast (slot), for sounds and the HUD.</summary>
        public event Action<int> Cast;

        public Combatant Combatant { get; private set; }

        private CharacterMotor _motor;
        private float[] _readyAt;
        private GameObject _bubble;
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly RaycastHit[] _hits = new RaycastHit[16];

        private void Awake()
        {
            Combatant = GetComponent<Combatant>();
            _motor = GetComponent<CharacterMotor>();
            _readyAt = new float[Spells.Count];
        }

        public void Select(int slot)
        {
            if (slot >= 0 && slot < Spells.Count && slot != SelectedSlot)
            {
                SelectedSlot = slot;
                SelectedChanged?.Invoke(slot);
            }
        }

        public float CooldownRemaining(int slot)
        {
            return Mathf.Max(0f, _readyAt[slot] - Time.time);
        }

        /// <summary>0 when ready, 1 right after casting.</summary>
        public float CooldownFraction(int slot)
        {
            var cooldown = Spells[slot].Cooldown;
            return cooldown <= 0f ? 0f : Mathf.Clamp01(CooldownRemaining(slot) / cooldown);
        }

        public bool CanCast(int slot)
        {
            return Combatant.Alive.Value && CooldownRemaining(slot) <= 0f && Combatant.Mana.Value >= Spells[slot].ManaCost;
        }

        private void Update()
        {
            if (IsSpawned && IsOwner && playerControlled)
            {
                ReadInput();
            }

            if (IsServer)
            {
                StepProjectiles(Time.deltaTime);
            }

            var shielded = IsSpawned && Combatant.Shield.Value > 0f && Combatant.Alive.Value;
            if (shielded && _bubble == null)
            {
                _bubble = SpellEffects.Bubble(transform, SpellBook.Starter[2].Color);
            }
            else if (!shielded && _bubble != null)
            {
                Destroy(_bubble);
            }
        }

        private void ReadInput()
        {
            var input = EngineInput.Default;
            if (!input.PlayerEnabled || !Combatant.Alive.Value)
            {
                return;
            }

            var slot = input.Player.SlotPressedThisFrame();
            if (slot > 0)
            {
                Select(slot - 1);
            }

            if (input.Player.Primary.WasPressedThisFrame())
            {
                TryCast(SelectedSlot, AimPoint());
            }
        }

        /// <summary>Owner: asks the server to cast. False when it is plainly not possible (cooldown, mana).</summary>
        public bool TryCast(int slot, Vector3 aimPoint)
        {
            if (!CanCast(slot))
            {
                return false;
            }

            if (IsServer)
            {
                return Execute(slot, aimPoint, 0f);
            }

            // Shown as cooling down at once; the server runs its own cooldown.
            _readyAt[slot] = Time.time + Spells[slot].Cooldown;
            CastRpc(slot, aimPoint);
            return true;
        }

        /// <summary>What the middle of the screen points at, ignoring this combatant.</summary>
        public Vector3 AimPoint()
        {
            var camera = Camera.main;
            var ray = camera != null ? camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)) : new Ray(_motor.EyePosition, transform.forward);
            return NearestHit(ray.origin, 0f, ray.direction, 200f, out var hit, out _) ? hit.point : ray.GetPoint(100f);
        }

        [Rpc(SendTo.Server)]
        private void CastRpc(int slot, Vector3 aimPoint)
        {
            // A little slack: the owner's clock started the cooldown a moment before this arrived.
            Execute(slot, aimPoint, 0.15f);
        }

        /// <summary>Server: casts if alive, off cooldown and with enough mana (how bots cast).</summary>
        public bool ServerCast(int slot, Vector3 aimPoint)
        {
            return Execute(slot, aimPoint, 0f);
        }

        private bool Execute(int slot, Vector3 aimPoint, float cooldownSlack)
        {
            if (!IsServer || slot < 0 || slot >= Spells.Count || !Combatant.Alive.Value)
            {
                return false;
            }

            var spell = Spells[slot];

            if (_readyAt[slot] - Time.time > cooldownSlack)
            {
                return false;
            }

            if (!Combatant.TrySpendMana(spell.ManaCost))
            {
                return false;
            }

            _readyAt[slot] = Time.time + spell.Cooldown;
            var eye = _motor.EyePosition;

            switch (spell.Kind)
            {
                case SpellKind.Projectile:
                    var direction = (aimPoint - eye).normalized;
                    if (direction == Vector3.zero)
                    {
                        direction = transform.forward;
                    }

                    var origin = eye + direction * 0.6f;
                    _projectiles.Add(new Projectile { Spell = spell, Position = origin, Direction = direction, Remaining = spell.Range });
                    CastVisualRpc(slot, origin, direction, 0f);
                    break;

                case SpellKind.Nova:
                    foreach (var other in Combatant.All)
                    {
                        if (other != Combatant && other.Alive.Value && Vector3.Distance(other.transform.position, transform.position) <= spell.Radius)
                        {
                            other.ApplyDamage(spell.Damage, Combatant, spell.Id);
                        }
                    }

                    CastVisualRpc(slot, transform.position, Vector3.zero, 0f);
                    break;

                case SpellKind.Shield:
                    Combatant.GiveShield(spell.Amount, spell.Duration);
                    CastVisualRpc(slot, transform.position, Vector3.zero, 0f);
                    break;

                case SpellKind.Blink:
                    var from = transform.position;
                    var to = BlinkDestination(spell, aimPoint);
                    _motor.Teleport(to, _motor.Yaw);
                    CastVisualRpc(slot, from, to, _motor.Yaw);
                    break;
            }

            return true;
        }

        [Rpc(SendTo.Everyone)]
        private void CastVisualRpc(int slot, Vector3 a, Vector3 b, float yaw)
        {
            var spell = Spells[slot];
            switch (spell.Kind)
            {
                case SpellKind.Projectile:
                    var ball = SpellEffects.Ball(a, spell.Radius, spell.Color);
                    ball.AddComponent<FlyingBall>().Begin(this, b, spell);
                    break;
                case SpellKind.Nova:
                    SpellEffects.Ring(a, spell.Radius, spell.Color);
                    break;
                case SpellKind.Shield:
                    SpellEffects.Flash(a + Vector3.up, 2f, spell.Color);
                    break;
                case SpellKind.Blink:
                    SpellEffects.Flash(a + Vector3.up, 1.5f, spell.Color);
                    SpellEffects.Flash(b + Vector3.up, 1.5f, spell.Color);
                    if (!IsServer)
                    {
                        _motor.Teleport(b, yaw);
                    }

                    break;
            }

            var sound = $"spell.{spell.Id}.cast";
            if (EngineAudio.Default.HasEvent(sound))
            {
                EngineAudio.Default.Play(sound, transform.position);
            }

            Cast?.Invoke(slot);
        }

        // Forward along the look direction (level), stopping before walls, then down to the ground.
        private Vector3 BlinkDestination(SpellDefinition spell, Vector3 aimPoint)
        {
            var direction = Vector3.ProjectOnPlane(aimPoint - transform.position, Vector3.up);
            direction = direction.sqrMagnitude > 0.01f ? direction.normalized : Quaternion.Euler(0f, _motor.Yaw, 0f) * Vector3.forward;

            var start = transform.position + Vector3.up * 1f;
            var distance = NearestHit(start, 0.35f, direction, spell.Range, out var wall, out _) ? Mathf.Max(0f, wall.distance - 0.4f) : spell.Range;
            var destination = transform.position + direction * distance;

            if (Physics.Raycast(destination + Vector3.up * 2f, Vector3.down, out var ground, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                destination.y = ground.point.y + 0.05f;
            }

            return destination;
        }

        private void StepProjectiles(float deltaTime)
        {
            for (var i = _projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = _projectiles[i];
                var step = projectile.Spell.Speed * deltaTime;

                if (NearestHit(projectile.Position, projectile.Spell.Radius, projectile.Direction, step, out _, out var target))
                {
                    if (target != null)
                    {
                        target.ApplyDamage(projectile.Spell.Damage, Combatant, projectile.Spell.Id);
                    }

                    _projectiles.RemoveAt(i);
                    continue;
                }

                projectile.Position += projectile.Direction * step;
                projectile.Remaining -= step;
                if (projectile.Remaining <= 0f)
                {
                    _projectiles.RemoveAt(i);
                }
                else
                {
                    _projectiles[i] = projectile;
                }
            }
        }

        /// <summary>The nearest solid thing along a path, skipping this combatant, dead combatants and triggers.</summary>
        internal bool NearestHit(Vector3 origin, float radius, Vector3 direction, float distance, out RaycastHit nearest, out Combatant combatant)
        {
            var count = radius > 0f
                ? Physics.SphereCastNonAlloc(origin, radius, direction, _hits, distance, ~0, QueryTriggerInteraction.Ignore)
                : Physics.RaycastNonAlloc(origin, direction, _hits, distance, ~0, QueryTriggerInteraction.Ignore);

            nearest = default;
            combatant = null;
            var found = false;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (hit.collider.transform.IsChildOf(transform))
                {
                    continue;
                }

                var other = hit.collider.GetComponentInParent<Combatant>();
                if (other != null && !other.Alive.Value)
                {
                    continue;
                }

                if (!found || hit.distance < nearest.distance)
                {
                    nearest = hit;
                    combatant = other;
                    found = true;
                }
            }

            return found;
        }

        private struct Projectile
        {
            public SpellDefinition Spell;
            public Vector3 Position;
            public Vector3 Direction;
            public float Remaining;
        }

        // What clients see of a projectile: it flies the same path and stops on what it meets.
        private sealed class FlyingBall : MonoBehaviour
        {
            private SpellCaster _caster;
            private Vector3 _direction;
            private SpellDefinition _spell;
            private float _remaining;

            public void Begin(SpellCaster caster, Vector3 direction, SpellDefinition spell)
            {
                _caster = caster;
                _direction = direction;
                _spell = spell;
                _remaining = spell.Range;
            }

            private void Update()
            {
                var step = _spell.Speed * Time.deltaTime;
                if (_caster != null && _caster.NearestHit(transform.position, _spell.Radius, _direction, step, out var hit, out _))
                {
                    SpellEffects.Flash(hit.point, 1f, _spell.Color);
                    Destroy(gameObject);
                    return;
                }

                transform.position += _direction * step;
                _remaining -= step;
                if (_remaining <= 0f || _caster == null)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
