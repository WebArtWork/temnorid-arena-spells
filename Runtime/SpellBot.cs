using ITKamianets.Engine.Character;
using Temnorid.Arena.Core;
using Unity.Netcode;
using UnityEngine;

namespace Temnorid.Arena.Spells
{
    /// <summary>
    /// A bot for the deathmatch. Runs on the server: picks the nearest living enemy, walks toward
    /// it and strafes once close, casts fireballs with some aim error when it can see it, frost
    /// nova up close, a shield when hurt, and blinks away under pressure. It drives the
    /// <see cref="CharacterMotor"/> with commands, like a player's controls.
    /// </summary>
    [RequireComponent(typeof(SpellCaster))]
    public class SpellBot : NetworkBehaviour
    {
        [Tooltip("Keeps about this far from its target.")]
        [SerializeField] private float preferredRange = 11f;

        [Tooltip("Aim error in degrees. Higher is easier to beat.")]
        [SerializeField] private float aimError = 4f;

        [Tooltip("Seconds between decisions to cast.")]
        [SerializeField] private float thinkInterval = 0.35f;

        private SpellCaster _caster;
        private CharacterMotor _motor;
        private Combatant _target;
        private float _nextThink;
        private float _strafeSign = 1f;
        private float _nextStrafeFlip;
        private float _lastHealth;
        private float _stuckTime;

        private void Awake()
        {
            _caster = GetComponent<SpellCaster>();
            _motor = GetComponent<CharacterMotor>();
        }

        private void Update()
        {
            if (!IsServer || !IsSpawned)
            {
                return;
            }

            var self = _caster.Combatant;
            if (!self.Alive.Value)
            {
                _target = null;
                return;
            }

            if (_target == null || !_target.Alive.Value || Time.time >= _nextThink)
            {
                _target = NearestEnemy(self);
            }

            Move(self);

            if (Time.time >= _nextThink)
            {
                _nextThink = Time.time + thinkInterval * Random.Range(0.7f, 1.3f);
                Think(self);
            }

            _lastHealth = self.Health.Value;
        }

        private void Move(Combatant self)
        {
            var command = new CharacterCommand { Yaw = _motor.Yaw };
            if (_target != null)
            {
                var toTarget = _target.transform.position - transform.position;
                toTarget.y = 0f;
                var distance = toTarget.magnitude;
                var forward = distance > 0.01f ? toTarget / distance : transform.forward;
                command.Yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;

                if (Time.time >= _nextStrafeFlip)
                {
                    _strafeSign = Random.value < 0.5f ? -1f : 1f;
                    _nextStrafeFlip = Time.time + Random.Range(1f, 2.5f);
                }

                var right = new Vector3(forward.z, 0f, -forward.x);
                var approach = distance > preferredRange ? 1f : distance < preferredRange * 0.5f ? -0.6f : 0f;
                command.Move = Vector3.ClampMagnitude(forward * approach + right * (_strafeSign * 0.8f), 1f);
                command.Sprint = distance > preferredRange * 1.8f;

                // Walking into something: hop over it, or turn the other way.
                var wantsToMove = command.Move.sqrMagnitude > 0.1f;
                var moving = new Vector3(_motor.Velocity.x, 0f, _motor.Velocity.z).sqrMagnitude > 0.5f;
                _stuckTime = wantsToMove && !moving ? _stuckTime + Time.deltaTime : 0f;
                if (_stuckTime > 0.4f)
                {
                    command.Jump = true;
                    _strafeSign = -_strafeSign;
                    _stuckTime = 0f;
                }
            }

            _motor.SetCommand(command);
        }

        private void Think(Combatant self)
        {
            var spells = _caster.Spells;

            // Shield when hurt and being hit.
            if (self.Health.Value < self.MaxHealth * 0.5f && self.Health.Value < _lastHealth && _caster.CanCast(2))
            {
                _caster.ServerCast(2, transform.position + transform.forward);
                return;
            }

            if (_target == null)
            {
                return;
            }

            var distance = Vector3.Distance(transform.position, _target.transform.position);

            // Under pressure up close: nova, or blink away.
            if (distance < spells[1].Radius * 0.8f && _caster.CanCast(1))
            {
                _caster.ServerCast(1, _target.transform.position);
                return;
            }

            if (self.Health.Value < self.MaxHealth * 0.3f && distance < 6f && _caster.CanCast(3))
            {
                var away = transform.position + (transform.position - _target.transform.position).normalized * 10f;
                _caster.ServerCast(3, away);
                return;
            }

            if (CanSee(_target) && _caster.CanCast(0))
            {
                var aim = _target.Motor.EyePosition - Vector3.up * 0.5f;
                var error = Quaternion.Euler(Random.Range(-aimError, aimError), Random.Range(-aimError, aimError), 0f);
                var eye = _motor.EyePosition;
                _caster.ServerCast(0, eye + error * (aim - eye));
            }
        }

        private bool CanSee(Combatant target)
        {
            var eye = _motor.EyePosition;
            var aim = target.Motor.EyePosition - Vector3.up * 0.5f;
            var direction = aim - eye;
            return _caster.NearestHit(eye, 0f, direction.normalized, direction.magnitude, out _, out var hit) && hit == target;
        }

        private static Combatant NearestEnemy(Combatant self)
        {
            Combatant nearest = null;
            var best = float.MaxValue;
            foreach (var other in Combatant.All)
            {
                if (other == self || !other.Alive.Value)
                {
                    continue;
                }

                var distance = (other.transform.position - self.transform.position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = other;
                }
            }

            return nearest;
        }
    }
}
