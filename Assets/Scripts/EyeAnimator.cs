using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace
{
    [Serializable]
    public class EyeAnimator
    {
        public enum Animations
        {
            OpenEye,
            CloseEye,
            BlinkEye
        }

        public const string OpenEye = "open_eye";
        public const string CloseEye = "close_eye";
        public const string BlinkEye = "blink_eye";

        [SerializeField] private SpriteRenderer image;
        [SerializeField] private float frameDelay = 0.1f;
        [Space] [SerializeField] private Sprite eyeOpenJawOpen;
        [SerializeField] private Sprite eyeCloseJawOpen;
        [SerializeField] private Sprite eyeOpenJawClose;
        [SerializeField] private Sprite eyeCloseJawClose;

        [SerializeField] private List<Sprite> closingEye;

        [SerializeField] private Sprite[] closingF;
        [SerializeField] private Sprite[] openingF;
        [SerializeField] private Sprite[] blinkingF;

        private Animations _animation;
        private float _timer = 0.1f;
        private bool _isAnimating = false;
        private int _index = 0;

        public void Validate()
        {
            if (closingEye.Count > 3)
            {
                closingEye.RemoveRange(3, closingEye.Count - 3);
            }
            else if (closingEye.Count < 3)
            {
                while (closingEye.Count < 3)
                {
                    closingEye.Add(default);
                }
            }

            closingF = new[]
            {
                eyeOpenJawClose,
                closingEye[0],
                closingEye[1],
                closingEye[2],
                eyeCloseJawClose
            };
            openingF = new[]
            {
                eyeCloseJawClose,
                closingEye[2],
                closingEye[1],
                closingEye[0],
                eyeOpenJawClose
            };
            blinkingF = new[]
            {
                eyeOpenJawClose,
                closingEye[0],
                closingEye[1],
                closingEye[2],
                eyeCloseJawClose,
                closingEye[2],
                closingEye[1],
                closingEye[0],
                eyeOpenJawClose
            };
        }

        public void Play(Animations animation)
        {
            _isAnimating = true;
            _animation = animation;
        }
        
        public void Update(float delay)
        {
            if (_timer > 0)
            {
                _timer -= delay;
                return;
            }
            
            if (!_isAnimating) return;
            
            Sprite nextSprite = null;
            
            switch (_animation)
            {
                case Animations.OpenEye:
                    nextSprite = openingF[_index++];
                    if (_index >= openingF.Length) _isAnimating = false;
                    break;
                case Animations.CloseEye:
                    nextSprite = closingF[_index++];
                    if (_index >= closingF.Length) _isAnimating = false;
                    break;
                case Animations.BlinkEye:
                    nextSprite = blinkingF[_index++];
                    if (_index >= blinkingF.Length) _isAnimating = false;
                    break;
            }
            
            image.sprite = nextSprite;
            _timer = delay;
        }
    }
}