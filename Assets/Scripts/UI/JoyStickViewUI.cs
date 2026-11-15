using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class JoyStickViewUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private new UnityEngine.Camera camera;
        [SerializeField] private Image imageStick;
        [SerializeField] private Image imageStickBg;

        [Header("Closed jaw (stay)")]
        [SerializeField] private Color closedStickColor = new(0.75f, 0.7f, 0.7f, 0.75f);
        [SerializeField] private Color closedBgColor = new(0.55f, 0.55f, 0.6f, 0.35f);
        [SerializeField] private float closedStickScale = 0.85f;
        [SerializeField] private float closedBgScale = 0.95f;

        [Header("Open jaw (dash)")]
        [SerializeField] private Color openStickColor = new(0.54f, 0.16f, 0.11f, 1f);
        [SerializeField] private Color openBgColor = new(0.14f, 0f, 0f, 1f);
        [SerializeField] private float openStickScale = 1.35f;
        [SerializeField] private float openBgScale = 1.25f;

        [Header("Feel")]
        [SerializeField] private float transitionSpeed = 12f;
        [SerializeField] private float punchStrength = 0.3f;
        [SerializeField] private float punchDecay = 9f;
        [SerializeField] private float pulseSpeed = 14f;
        [SerializeField] private float pulseAmount = 0.07f;
        [SerializeField] private float shakeAmount = 3f;
        [SerializeField] private float stickTravel = 0.35f;

        private Inputer _inputer;
        private bool _active;
        private bool _open;
        private float _blend;
        private float _punch;
        private Vector2 _direction;
        private Vector2 _stickOffset;

        private void Start()
        {
            _inputer = Inputer.Instance;
            _inputer.OnInputFreeze += Freeze;
            _inputer.OnInputLook += Look;
            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (_inputer == null) return;
            _inputer.OnInputFreeze -= Freeze;
            _inputer.OnInputLook -= Look;
        }

        private void Look(Vector2 direction) => _direction = direction;

        private void Freeze(bool b, Vector2 v2)
        {
            _active = b;
            SetVisible(b);
            if (!b) return;

            imageStickBg.rectTransform.position = v2;
            _blend = 0f;
            _open = false;
            _stickOffset = Vector2.zero;
            _punch = 0f;
        }

        private void SetVisible(bool b)
        {
            imageStick.enabled = b;
            imageStickBg.enabled = b;
        }

        private void Update()
        {
            if (!_active) return;

            bool open = _direction.magnitude > Inputer.JoysickMinMagnitude;
            if (open != _open)
            {
                _open = open;
                _punch = open ? punchStrength : -punchStrength * 0.6f;
            }

            float dt = Time.unscaledDeltaTime;
            _blend = Mathf.MoveTowards(_blend, open ? 1f : 0f, dt * transitionSpeed);
            _punch = Mathf.Lerp(_punch, 0f, dt * punchDecay);

            float t = Mathf.SmoothStep(0f, 1f, _blend);
            float pulse = open ? Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount * t : 0f;

            imageStickBg.color = Color.Lerp(closedBgColor, openBgColor, t);
            imageStick.color = Color.Lerp(closedStickColor, openStickColor, t);
            imageStickBg.rectTransform.localScale = Vector3.one * (Mathf.Lerp(closedBgScale, openBgScale, t) * (1f + _punch * 0.5f + pulse * 0.5f));
            imageStick.rectTransform.localScale = Vector3.one * (Mathf.Lerp(closedStickScale, openStickScale, t) * (1f + _punch + pulse));

            Vector2 target = _direction * (_inputer.DragRadiusPixels * stickTravel);
            _stickOffset = Vector2.Lerp(_stickOffset, target, 1f - Mathf.Exp(-40f * dt));
            Vector2 shake = open ? Random.insideUnitCircle * (shakeAmount * t) : Vector2.zero;
            imageStick.rectTransform.position = (Vector2)imageStickBg.rectTransform.position + _stickOffset + shake;
        }
    }
}
