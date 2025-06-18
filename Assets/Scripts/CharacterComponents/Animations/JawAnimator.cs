using System;
using System.Collections.Generic;
using Audio;
using Constants;
using UnityEngine;

namespace CharacterComponents.Animations
{
    [Serializable]
    public class JawAnimator
    {
        const float FrameDelay = 0.05f;
        public enum Animations
        {
            OpenJaw,
            CloseJaw
        }

        [SerializeField] private SpriteRenderer image;
        
        [SerializeField] private List<Sprite> openJawFrames;
        
        [SerializeField, HideInInspector] private Sprite[] openJaw;
        [SerializeField, HideInInspector] private Sprite[] closeJaw;

        private Animations _animation;
        private float _timer = 0.75f;
        private bool _isAnimating = false;
        private int _index = 0;
        
        public SpriteRenderer Image => image;
        
        public void Play(Animations animation)
        {
            _index = 0;
            _isAnimating = true;
            _animation = animation;
        }

        public void Validate()
        {
            openJaw = new Sprite[3] { openJawFrames[0], openJawFrames[1], openJawFrames[2] };
            closeJaw = new Sprite[3] { openJawFrames[2], openJawFrames[1], openJawFrames[0] };
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
                case Animations.OpenJaw:
                    nextSprite = openJaw[_index++];
                    if (_index >= openJaw.Length) _isAnimating = false;
                    break;
                case Animations.CloseJaw:
                    nextSprite = closeJaw[_index++];
                    if (_index >= closeJaw.Length) _isAnimating = false;
                    break;
            }

            image.sprite = nextSprite;
            _timer = FrameDelay;
        }
    }
}