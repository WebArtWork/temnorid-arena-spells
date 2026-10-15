using UnityEngine;

namespace Temnorid.Arena.Spells
{
    /// <summary>
    /// Casting feedback in the hand, for the local player aiming with a hand (VR): a glow at the
    /// casting hand in the selected spell's color, bright when it can be cast and dim while it
    /// cools down, and a short aim line toward what it points at. Hidden on flat screens (the
    /// crosshair aims there), for other players and while dead.
    /// </summary>
    [RequireComponent(typeof(SpellCaster))]
    public class SpellAimVisual : MonoBehaviour
    {
        [SerializeField] private float glowSize = 0.07f;

        [Tooltip("Longest aim line, meters.")]
        [SerializeField] private float lineLength = 4f;

        [SerializeField] private float lineWidth = 0.008f;

        private SpellCaster _caster;
        private Transform _glow;
        private LineRenderer _line;
        private Renderer _glowRenderer;
        private int _shownSlot = -1;
        private bool _shownReady;

        private void Awake()
        {
            _caster = GetComponent<SpellCaster>();
        }

        private void OnDisable()
        {
            Show(false);
        }

        private void OnDestroy()
        {
            if (_glow != null)
            {
                Destroy(_glow.gameObject);
            }
        }

        private void LateUpdate()
        {
            var visible = _caster.IsSpawned && _caster.IsOwner && _caster.Combatant.Alive.Value && _caster.AimsFromSource;
            Show(visible);
            if (!visible)
            {
                return;
            }

            var ray = _caster.AimRay();
            var distance = _caster.NearestHit(ray.origin, 0f, ray.direction, lineLength, out var hit, out _) ? hit.distance : lineLength;
            _glow.position = ray.origin;
            _line.SetPosition(0, ray.origin);
            _line.SetPosition(1, ray.GetPoint(distance));

            var slot = _caster.SelectedSlot;
            var ready = _caster.CanCast(slot);
            if (slot != _shownSlot || ready != _shownReady)
            {
                _shownSlot = slot;
                _shownReady = ready;
                var color = _caster.Spells[slot].Color;
                if (!ready)
                {
                    color = Color.Lerp(color, Color.gray, 0.6f);
                }

                var material = SpellEffects.MaterialFor(color);
                _glowRenderer.sharedMaterial = material;
                _line.sharedMaterial = material;
                _glow.localScale = Vector3.one * (ready ? glowSize : glowSize * 0.6f);
            }
        }

        private void Show(bool visible)
        {
            if (visible && _glow == null)
            {
                Build();
            }

            if (_glow != null && _glow.gameObject.activeSelf != visible)
            {
                _glow.gameObject.SetActive(visible);
            }
        }

        private void Build()
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Casting Hand";
            Destroy(glow.GetComponent<Collider>());
            _glow = glow.transform;
            _glowRenderer = glow.GetComponent<Renderer>();
            _glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var lineObject = new GameObject("Aim Line");
            lineObject.transform.SetParent(_glow, false);
            _line = lineObject.AddComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 2;
            _line.startWidth = lineWidth;
            _line.endWidth = lineWidth * 0.25f;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _shownSlot = -1;
        }
    }
}
