using System;
using CharacterComponents.CharacterStat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CharacterComponents.Animations
{
    public class LumpMeatAnimator : BaseCharacterComponent
    {
        [SerializeField] private float blinkEyeDelayMin = 2;
        [SerializeField] private float blinkEyeDelayMax = 7;
        [Space]
        [SerializeField] private EyeAnimator eye0;
        [SerializeField] private EyeAnimator eye1;
        [SerializeField] private EyeAnimator eye2;
        [SerializeField] private EyeAnimator eye3;
        [SerializeField] private EyeAnimator eye4;
        [SerializeField] private EyeAnimator eye5;
        [SerializeField] private EyeAnimator eye7;
        [SerializeField] private EyeAnimator eye8;
        [Space]
        [SerializeField] private JawAnimator jaw;
        
        [SerializeField] private JawAnimator jawUp;
        [SerializeField] private JawAnimator arrowUp;
        [SerializeField] private JawAnimator dashUp;
        
        [SerializeField, HideInInspector] private EyeAnimator[] eyes;
        private Stats Stats => character.Stats;
        private float _timerBlink = 0;
        private bool _isJawOpen = false;
        public bool IsJawOpen => _isJawOpen;

        public void OnValidate()
        {
            eyes = new[] { eye0, eye1, eye2, eye3, eye4, eye5, eye7, eye8 };
            foreach (var eye in eyes)
            {
                eye.Validate();
            }

            jaw.Validate();
            
            jawUp.Validate();
            arrowUp.Validate();
            dashUp.Validate();
        }

        public override void Init()
        {
            
        }

        private void Start()
        {
            if (Stats.MaxHealth != 7)
            {
                Debug.LogWarning($"Max health is {Stats.MaxHealth}, should be {7}");
            }

            Stats.OnCureAction += ResetEyes;
            Stats.OnDmgAction += ResetEyes;

            for (int i = 0; i < eyes.Length; i++)
            {
                if (i < Stats.CurrentHealth)
                {
                    eyes[i].isBlocked = false;
                }
                else
                {
                    eyes[i].isBlocked = true;
                }
            }

            foreach (var eye in eyes)
            {
                eye.Init();
            }

            _timerBlink = Random.Range(blinkEyeDelayMin, blinkEyeDelayMax);
        }

        private void ResetEyes(int num)
        {
            int flag = 0;

            foreach (var eye in eyes)
            {
                if (flag < Stats.CurrentHealth)
                {
                    eye.isBlocked = false;
                    if (eye.IsEyeClosed) eye.Play(EyeAnimator.Animations.OpenEye, _isJawOpen);
                    flag++;
                }
                else
                {
                    if (!eye.IsEyeClosed) eye.Play(EyeAnimator.Animations.CloseEye, _isJawOpen);
                    eye.isBlocked = true;
                }
            }
        }

        private void FixedUpdate()
        {
            foreach (var eye in eyes)
            {
                eye.Update(Time.fixedDeltaTime);
            }

            jaw.Update(Time.fixedDeltaTime);
            
            jawUp.Update(Time.fixedDeltaTime);
            arrowUp.Update(Time.fixedDeltaTime);
            dashUp.Update(Time.fixedDeltaTime);

            if (_timerBlink > 0)
            {
                _timerBlink -= Time.fixedDeltaTime;
                return;
            }

            _timerBlink = Random.Range(blinkEyeDelayMin, blinkEyeDelayMax);
            BlinkEyes();
        }


        public void OpenJaw()
        {
            _isJawOpen = true;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.OpenJaw, _isJawOpen);
            }

            jaw.Play(JawAnimator.Animations.OpenJaw);
            
            jawUp.Play(JawAnimator.Animations.OpenJaw);
            arrowUp.Play(JawAnimator.Animations.OpenJaw);
            dashUp.Play(JawAnimator.Animations.OpenJaw);
        }

        public void CloseJaw()
        {
            _isJawOpen = false;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.CloseJaw, _isJawOpen);
            }

            jaw.Play(JawAnimator.Animations.CloseJaw);
            
            jawUp.Play(JawAnimator.Animations.CloseJaw);
            arrowUp.Play(JawAnimator.Animations.CloseJaw);
            dashUp.Play(JawAnimator.Animations.CloseJaw);
        }

        private void BlinkEyes()
        {
            if (_isJawOpen) return;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.BlinkEye, _isJawOpen);
            }
        }
    }
}