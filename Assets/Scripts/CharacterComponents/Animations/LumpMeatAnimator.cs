using UnityEngine;

namespace CharacterComponents.Animations
{
    public class LumpMeatAnimator : BaseCharacterComponent
    {
        [SerializeField] private float blinkEyeDelayMin = 2;
        [SerializeField] private float blinkEyeDelayMax = 7;
        [Space] [SerializeField] private EyeAnimator eye0;
        [SerializeField] private EyeAnimator eye1;
        [SerializeField] private EyeAnimator eye2;
        [SerializeField] private EyeAnimator eye3;
        [SerializeField] private EyeAnimator eye4;
        [SerializeField] private EyeAnimator eye5;
        [SerializeField] private EyeAnimator eye7;
        [SerializeField] private EyeAnimator eye8;
        [Space] [SerializeField] private JawAnimator jaw;
        [SerializeField, HideInInspector] private EyeAnimator[] eyes;
        private Stats Stats => character.Stats;
        private float _timerBlink = 0;

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
            if (Stats.MaxHealth != 8)
            {
                Debug.LogWarning($"Max health is {Stats.MaxHealth}, should be {8}");
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

        private void ResetEyes(int num)
        {
            int flag = 0;

            foreach (var eye in eyes)
            {
                if (flag < Stats.CurrentHealth)
                {
                    if (eye.isBlocked)
                    {
                        eye.isBlocked = false;
                        eye.Play(EyeAnimator.Animations.OpenEye);
                    }
                    flag++;
                }
                else
                {
                    if (!eye.isBlocked)
                    {
                        eye.Play(EyeAnimator.Animations.CloseEye);
                        eye.isBlocked = true;
                    }
                    
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
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.OpenJaw);
            }

            jaw.Play(JawAnimator.Animations.OpenJaw);
        }

        public void CloseJaw()
        {
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.CloseJaw);
            }

            jaw.Play(JawAnimator.Animations.CloseJaw);
        }

        public void BlinkEyes()
        {
            foreach (var eye in eyes)
            {
                eye.Play(EyeAnimator.Animations.BlinkEye);
            }
        }
    }
}