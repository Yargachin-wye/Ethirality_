using System;
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
        [SerializeField, HideInInspector] private EyeAnimator[] eyes;
        private Stats Stats => character.Stats;
        private float _timerBlink = 0;
        private bool isJawOpen = false;

        public override void OnValidate()
        {
            base.OnValidate();
            eyes = new[] { eye0, eye1, eye2, eye3, eye4, eye5, eye7, eye8 };
            foreach (var eye in eyes)
            {
                eye.Validate();
            }

            jaw.Validate();
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

            foreach (var eye in eyes)
            {
                eye.isBlocked = true;
            }

            foreach (var eye in eyes)
            {
                eye.Start();
            }

            ResetEyes(0);
            _timerBlink = Random.Range(blinkEyeDelayMin, blinkEyeDelayMax);
        }

        public void OpenEyes()
        {
            foreach (var eye in eyes)
            {
                if (eye.IsEyeClosed && !isJawOpen) eye.Play(EyeAnimator.Animations.OpenEye, isJawOpen);
            }
        }

        private void ResetEyes(int num)
        {
            int flag = 0;

            foreach (var eye in eyes)
            {
                if (flag < Stats.CurrentHealth)
                {
                    eye.isBlocked = false;
                    if (eye.IsEyeClosed) eye.Play(EyeAnimator.Animations.OpenEye, isJawOpen);
                    flag++;
                }
                else
                {
                    if (!eye.IsEyeClosed) eye.Play(EyeAnimator.Animations.CloseEye, isJawOpen);
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
            isJawOpen = true;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.OpenJaw, isJawOpen);
            }

            jaw.Play(JawAnimator.Animations.OpenJaw);
        }

        public void CloseJaw()
        {
            isJawOpen = false;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.CloseJaw, isJawOpen);
            }

            jaw.Play(JawAnimator.Animations.CloseJaw);
        }

        public void BlinkEyes()
        {
            if (isJawOpen) return;
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.BlinkEye, isJawOpen);
            }
        }
    }
}