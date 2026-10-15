using Temnorid.Arena.Core;
using Unity.Netcode;
using UnityEngine;

namespace Temnorid.Arena.Spells
{
    /// <summary>
    /// Temnorid Arena's side of a combatant's model: cast animations per spell, the hit reaction,
    /// death, and the accent color that tells combatants apart (you, other players, bots).
    /// Movement animation comes from the engine's <c>CharacterAnimator</c>. Driven by networked
    /// state and events, so every client sees the same thing.
    ///
    /// Animator parameters (triggers): <c>CastProjectile</c>, <c>CastNova</c>, <c>CastShield</c>,
    /// <c>CastBlink</c>, <c>Hit</c>; (bool) <c>Dead</c>.
    /// </summary>
    [RequireComponent(typeof(Combatant))]
    [RequireComponent(typeof(SpellCaster))]
    public class ArenaCharacterVisuals : NetworkBehaviour
    {
        private static readonly int CastProjectile = Animator.StringToHash("CastProjectile");
        private static readonly int CastNova = Animator.StringToHash("CastNova");
        private static readonly int CastShield = Animator.StringToHash("CastShield");
        private static readonly int CastBlink = Animator.StringToHash("CastBlink");
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Dead = Animator.StringToHash("Dead");

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int GltfBaseColor = Shader.PropertyToID("baseColorFactor");

        [Tooltip("Name of the model's material that is recolored per combatant.")]
        [SerializeField] private string accentMaterial = "accent";

        [SerializeField] private Color localPlayer = new Color(0.25f, 0.85f, 1f);
        [SerializeField] private Color otherPlayer = new Color(1f, 0.75f, 0.25f);
        [SerializeField] private Color bot = new Color(0.9f, 0.2f, 0.15f);

        private Combatant _combatant;
        private SpellCaster _caster;
        private Animator _animator;

        private void Awake()
        {
            _combatant = GetComponent<Combatant>();
            _caster = GetComponent<SpellCaster>();
            _animator = GetComponentInChildren<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            _caster.Cast += OnCast;
            _combatant.Damaged += OnDamaged;
            _combatant.AliveChanged += OnAliveChanged;
            OnAliveChanged(_combatant.Alive.Value);
            Tint(_combatant.IsBot ? bot : IsOwner ? localPlayer : otherPlayer);
        }

        public override void OnNetworkDespawn()
        {
            _caster.Cast -= OnCast;
            _combatant.Damaged -= OnDamaged;
            _combatant.AliveChanged -= OnAliveChanged;
        }

        private void OnCast(int slot)
        {
            if (_animator == null)
            {
                return;
            }

            switch (_caster.Spells[slot].Kind)
            {
                case SpellKind.Projectile: _animator.SetTrigger(CastProjectile); break;
                case SpellKind.Nova: _animator.SetTrigger(CastNova); break;
                case SpellKind.Shield: _animator.SetTrigger(CastShield); break;
                case SpellKind.Blink: _animator.SetTrigger(CastBlink); break;
            }
        }

        private void OnDamaged(float amount)
        {
            if (_animator != null && _combatant.Alive.Value)
            {
                _animator.SetTrigger(Hit);
            }
        }

        private void OnAliveChanged(bool alive)
        {
            if (_animator != null)
            {
                _animator.SetBool(Dead, !alive);
            }
        }

        // One property block per renderer, so the shared material asset stays untouched.
        private void Tint(Color color)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].name.StartsWith(accentMaterial))
                    {
                        continue;
                    }

                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor(BaseColor, color);
                    block.SetColor(GltfBaseColor, color);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }
    }
}
