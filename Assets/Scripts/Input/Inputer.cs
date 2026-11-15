using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Managers
{
    public class Inputer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _cameraMain;
        [SerializeField] private float tapDelay;
        [SerializeField] private float dragRadius = 0.12f;
        public static float JoysickMinMagnitude = 0.9f;
        public float DragRadiusPixels => Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) * dragRadius);
        public event Action<bool, Vector2> OnInputFreeze;
        public event Action<Vector2> OnInputDash;
        public event Action<Vector2> OnInputShot;
        public event Action<Vector2> OnInputLook;

        public static Inputer Instance;

        private const float StickDeadzone = 0.25f;
        private bool _useStick;
        private bool _pressed = false;
        private bool _taped = false;
        private float _tapTimer = 0;
        private Controls _controls;
        private Vector2 _touchPosition;
        private Vector2 _detouchPosition;
        private Vector2 _lookDirection;
        private Vector2 _stickValue;
        private Vector2 _screenPosition;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("Instance != null");
                return;
            }

            Instance = this;
            _controls = new Controls();
        }
        
        private void OnEnable()
        {
            _controls.Enable();
            _controls.PlayerMovable.Joystick.performed += Joystick;
            _controls.PlayerMovable.Joystick.canceled += Joystick;
            _controls.PlayerMovable.LeftButton.started += Touch;
            _controls.PlayerMovable.LeftButton.canceled += ReleasedTouch;
            _controls.PlayerMovable.ScreenPosition.performed += ReadScreenPosition;
        }

        private void OnDisable()
        {
            _controls.PlayerMovable.Joystick.performed -= Joystick;
            _controls.PlayerMovable.Joystick.canceled -= Joystick;
            _controls.PlayerMovable.LeftButton.started -= Touch;
            _controls.PlayerMovable.LeftButton.canceled -= ReleasedTouch;
            _controls.PlayerMovable.ScreenPosition.performed -= ReadScreenPosition;

            _controls.Disable();
        }

        public void Touch(InputAction.CallbackContext context)
        {
            if (GameplayPause.IsPaused || _pressed) return;
            _taped = false;
            _pressed = true;
            _tapTimer = tapDelay;
            _touchPosition = _screenPosition;
            _lookDirection = _stickValue;
            _detouchPosition = _stickValue;
        }

        public void ReadScreenPosition(InputAction.CallbackContext context)
        {
            var pos = context.ReadValue<Vector2>();
            if (_useStick && (pos - _screenPosition).sqrMagnitude > 1f) _useStick = false;
            _screenPosition = pos;
        }

        public void ReleasedTouch(InputAction.CallbackContext context)
        {
            if (!_pressed) return;
            _pressed = false;

            if (GameplayPause.IsPaused)
            {
                _taped = false;
                OnInputFreeze?.Invoke(false, Vector2.zero);
                return;
            }

            if (_tapTimer > 0)
            {
                Vector2 v = _cameraMain.ScreenToWorldPoint(_touchPosition);
                OnInputShot?.Invoke(v);
                return;
            }

            if (_detouchPosition.magnitude > JoysickMinMagnitude) OnInputDash?.Invoke(_screenPosition);
            
            OnInputFreeze?.Invoke(false, Vector2.zero);
        }

        public void Joystick(InputAction.CallbackContext context)
        {
            if (GameplayPause.IsPaused) return;

            var dev = context.control.device;
            bool ignored = dev.displayName.Contains("vJoy") || dev.name.Contains("vJoy");
            if (ignored) return;
            var value = context.ReadValue<Vector2>();
            if (value.magnitude < StickDeadzone) value = Vector2.zero;
            _lookDirection = _stickValue = value;
            if (value != Vector2.zero)
            {
                _useStick = true;
                _detouchPosition = value;
            }
        }
        
        private void Update()
        {
            if (GameplayPause.IsPaused || !_pressed) return;

            if (!_useStick || _stickValue == Vector2.zero)
            {
                var drag = _screenPosition - _touchPosition;
                float radius = DragRadiusPixels;
                _lookDirection = _detouchPosition = Vector2.ClampMagnitude(drag / radius, 1f);
            }

            OnInputLook?.Invoke(_lookDirection);

            if (_taped) return;
            _tapTimer -= Time.deltaTime;
            if (_tapTimer <= 0)
            {
                _taped = true;
                OnInputFreeze?.Invoke(true, _touchPosition);
            }
        }
    }
}