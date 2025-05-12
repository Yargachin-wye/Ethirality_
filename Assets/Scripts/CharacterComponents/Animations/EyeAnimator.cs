using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace CharacterComponents.Animations
{
    [Serializable]
    public class EyeAnimator
    {
        const float FrameDelay = 0.05f;

        public enum Animations
        {
            OpenEye,
            CloseEye,
            BlinkEye,
            OpenJaw,
            CloseJaw
        }

        [SerializeField] private SpriteRenderer image;
        [Space] [SerializeField] private Sprite eyeOpenJawOpen;
        [SerializeField] private Sprite eyeOpenJawMid;
        [SerializeField] private Sprite eyeOpenJawClose;

        [SerializeField] private Sprite eyeCloseJawOpen;
        [SerializeField] private Sprite eyeCloseJawMid;
        [SerializeField] private Sprite eyeCloseJawClose;
        [SerializeField] private List<Sprite> closingEye;

        [SerializeField, HideInInspector] private Sprite[] closingF;
        [SerializeField, HideInInspector] private Sprite[] openingF;
        [SerializeField, HideInInspector] private Sprite[] blinkingF;

        [SerializeField, HideInInspector] private Sprite[] eyeOpenJawCloseF;
        [SerializeField, HideInInspector] private Sprite[] eyeOpenJawOpenF;
        [SerializeField, HideInInspector] private Sprite[] eyeCloseJawCloseF;
        [SerializeField, HideInInspector] private Sprite[] eyeCloseJawOpenF;

        private Animations _animation;
        private float _timer = 0.5f;
        private bool _isAnimating = false;
        private int _index = 0;

        public bool IsEyeClosed { get; private set; } = true;
        public bool isBlocked = false;

        public void Start()
        {
            if (IsEyeClosed)
            {
                image.sprite = eyeCloseJawClose;
            }
            else
            {
                image.sprite = eyeOpenJawClose;
            }
        }

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

            eyeOpenJawCloseF = new[]
            {
                eyeOpenJawOpen,
                eyeOpenJawMid,
                eyeOpenJawClose
            };
            eyeOpenJawOpenF = new[]
            {
                eyeOpenJawClose,
                eyeOpenJawMid,
                eyeOpenJawOpen
            };

            eyeCloseJawCloseF = new[]
            {
                eyeCloseJawOpen,
                eyeCloseJawMid,
                eyeCloseJawClose
            };
            eyeCloseJawOpenF = new[]
            {
                eyeCloseJawClose,
                eyeCloseJawMid,
                eyeCloseJawOpen
            };
        }

        public void Play(Animations animation, bool isJawOpen)
        {
            if (animation != Animations.OpenJaw && animation != Animations.CloseJaw && isBlocked) return;
            if (!isBlocked && isJawOpen && animation == Animations.OpenEye)
            {
                image.sprite = eyeOpenJawOpen;
                IsEyeClosed = false;
                return;
            }

            _index = 0;
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
                    if (_index >= openingF.Length)
                    {
                        _isAnimating = false;
                        IsEyeClosed = false;
                    }

                    break;
                case Animations.CloseEye:
                    nextSprite = closingF[_index++];
                    if (_index >= closingF.Length)
                    {
                        _isAnimating = false;
                        IsEyeClosed = true;
                    }

                    break;
                case Animations.BlinkEye:
                    nextSprite = blinkingF[_index++];
                    if (_index >= blinkingF.Length) _isAnimating = false;
                    break;
                case Animations.CloseJaw:
                    if (IsEyeClosed)
                    {
                        nextSprite = eyeCloseJawCloseF[_index++];
                    }
                    else
                    {
                        nextSprite = eyeOpenJawCloseF[_index++];
                    }

                    if (_index >= eyeOpenJawOpenF.Length) _isAnimating = false;
                    break;
                case Animations.OpenJaw:
                    if (IsEyeClosed)
                    {
                        nextSprite = eyeCloseJawOpenF[_index++];
                    }
                    else
                    {
                        nextSprite = eyeOpenJawOpenF[_index++];
                    }

                    if (_index >= eyeOpenJawOpenF.Length) _isAnimating = false;
                    break;
            }

            image.sprite = nextSprite;
            _timer = FrameDelay;
        }
    }
}