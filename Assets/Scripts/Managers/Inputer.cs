using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class Inputer : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera _cameraMain;
        [SerializeField] private float tapDelay;
        [SerializeField] private float joysickMinMagnitude = 0.99f;
        public event Action<bool, Vector2> OnInputFreeze;
        public event Action<Vector2> OnInputDash;
        public event Action<Vector2> OnInputShot;
        public event Action<Vector2> OnInputLook;

        public static Inputer Instance;

        private bool _pressed = false;
        private bool _taped = false;
        private float _tapTimer = 0;
        private Controls _controls;
        private Vector2 _touchPosition;
        private Vector2 _detouchPosition;
        private Vector2 _lookDirection;

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

        private void Update()
        {
            if (!_pressed) return;
            
            OnInputLook?.Invoke(_detouchPosition);
            
            if (_taped) return;
            _tapTimer -= Time.deltaTime;
            if (_tapTimer <= 0)
            {
                _taped = true;
                OnInputFreeze?.Invoke(true, _touchPosition);
            }
        }

        private void OnEnable()
        {
            _controls.Enable();
            _controls.PlayerMovable.Joystick.performed += Joystick;

            _controls.PlayerMovable.TouchScreenPc.started += LmPresed;
            _controls.PlayerMovable.TouchScreenPc.canceled += ReleasedTouch;

            _controls.PlayerMovable.TouchScreenMobile.performed += Touch;
            _controls.PlayerMovable.DetouchScreenMobile.canceled += ReleasedTouch;
        }

        private void OnDisable()
        {
            _controls.PlayerMovable.Joystick.performed -= Joystick;

            _controls.PlayerMovable.TouchScreenPc.started -= LmPresed;
            _controls.PlayerMovable.TouchScreenPc.canceled -= ReleasedTouch;

            _controls.PlayerMovable.TouchScreenMobile.performed -= Touch;
            _controls.PlayerMovable.DetouchScreenMobile.canceled -= ReleasedTouch;

            _controls.Disable();
        }

        public void Touch(InputAction.CallbackContext context)
        {
            if (_pressed) return;
            _taped = false;
            _tapTimer = tapDelay;
            _pressed = true;
            _touchPosition = context.ReadValue<Vector2>();
            _detouchPosition = Vector2.zero;
        }

        public void LmPresed(InputAction.CallbackContext context)
        {
            if (_pressed) return;
            _taped = false;
            _tapTimer = tapDelay;
            _pressed = true;
            _touchPosition = Input.mousePosition;
            _detouchPosition = Vector2.zero;
        }

        public void ReleasedTouch(InputAction.CallbackContext context)
        {
            if (!_pressed) return;
            _pressed = false;
            if (_tapTimer > 0)
            {
                Vector2 v = _cameraMain.ScreenToWorldPoint(_touchPosition);
                OnInputShot?.Invoke(v);
                return;
            }

            if (_detouchPosition.magnitude > joysickMinMagnitude) OnInputDash?.Invoke(_detouchPosition);


            OnInputFreeze?.Invoke(false, Vector2.zero);
        }

        public void Joystick(InputAction.CallbackContext context)
        {
            // if (!context.control.IsActuated()) return;
            _detouchPosition = context.ReadValue<Vector2>();
        }
    }
}