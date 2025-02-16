using System;
using System.Collections;
using UnityEngine;

namespace CharacterComponents.Animations
{
    public class LumpMeatAnimator : MonoBehaviour
    {
        [SerializeField] private EyeAnimator eye0;
        [SerializeField] private EyeAnimator eye1;
        [SerializeField] private EyeAnimator eye2;
        [SerializeField] private EyeAnimator eye3;
        [SerializeField] private EyeAnimator eye4;
        [SerializeField] private EyeAnimator eye5;
        [SerializeField] private EyeAnimator eye7;
        [SerializeField] private EyeAnimator eye8;
        [Space] [SerializeField] private JawAnimator jaw;

        private void OnValidate()
        {
            eye0.Validate();
            eye1.Validate();
            eye2.Validate();
            eye3.Validate();
            eye4.Validate();
            eye5.Validate();
            eye7.Validate();
            eye8.Validate();

            jaw.Validate();
        }

        private void Start()
        {
            eye0.isBlocked = false;
            eye1.isBlocked = true;
            eye2.isBlocked = true;
            eye3.isBlocked = true;
            eye4.isBlocked = true;
            eye5.isBlocked = true;
            eye7.isBlocked = true;
            eye8.isBlocked = true;
            
            eye0.Play(EyeAnimator.Animations.OpenEye);
            
            eye0.Start();
            eye1.Start();
            eye2.Start();
            eye3.Start();
            eye4.Start();
            eye5.Start();
            eye7.Start();
            eye8.Start();
        }

        private void FixedUpdate()
        {
            eye0.Update(Time.fixedDeltaTime);
            eye1.Update(Time.fixedDeltaTime);
            eye2.Update(Time.fixedDeltaTime);
            eye3.Update(Time.fixedDeltaTime);
            eye4.Update(Time.fixedDeltaTime);
            eye5.Update(Time.fixedDeltaTime);
            eye7.Update(Time.fixedDeltaTime);
            eye8.Update(Time.fixedDeltaTime);
            jaw.Update(Time.fixedDeltaTime);
        }

        public void OpenJaw()
        {
            eye0.Play(EyeAnimator.Animations.OpenJaw);
            eye1.Play(EyeAnimator.Animations.OpenJaw);
            eye2.Play(EyeAnimator.Animations.OpenJaw);
            eye3.Play(EyeAnimator.Animations.OpenJaw);
            eye4.Play(EyeAnimator.Animations.OpenJaw);
            eye5.Play(EyeAnimator.Animations.OpenJaw);
            eye7.Play(EyeAnimator.Animations.OpenJaw);
            eye8.Play(EyeAnimator.Animations.OpenJaw);
            jaw.Play(JawAnimator.Animations.OpenJaw);
        }

        public void CloseJaw()
        {
            eye0.Play(EyeAnimator.Animations.CloseJaw);
            eye1.Play(EyeAnimator.Animations.CloseJaw);
            eye2.Play(EyeAnimator.Animations.CloseJaw);
            eye3.Play(EyeAnimator.Animations.CloseJaw);
            eye4.Play(EyeAnimator.Animations.CloseJaw);
            eye5.Play(EyeAnimator.Animations.CloseJaw);
            eye7.Play(EyeAnimator.Animations.CloseJaw);
            eye8.Play(EyeAnimator.Animations.CloseJaw);
            jaw.Play(JawAnimator.Animations.CloseJaw);
        }

        public void BlinkEyes()
        {
            eye0.Play(EyeAnimator.Animations.BlinkEye);
            eye1.Play(EyeAnimator.Animations.BlinkEye);
            eye2.Play(EyeAnimator.Animations.BlinkEye);
            eye3.Play(EyeAnimator.Animations.BlinkEye);
            eye4.Play(EyeAnimator.Animations.BlinkEye);
            eye5.Play(EyeAnimator.Animations.BlinkEye);
            eye7.Play(EyeAnimator.Animations.BlinkEye);
            eye8.Play(EyeAnimator.Animations.BlinkEye);
        }
    }
}