using System;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI
{
    public class JoyStickViewUI : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private new UnityEngine.Camera camera;
        [SerializeField] private Image imageStick;
        [SerializeField] private Image imageStickBg;

        private Inputer _inputer;
        private bool _inited = false;

        private void Start()
        {
            _inputer = Inputer.Instance;
            _inputer.OnInputFreeze += Freeze;

            imageStick.enabled = false;
            imageStickBg.enabled = false;
        }

        private void Update()
        {
            if (!_inited) return;
        }

        private void Freeze(bool b, Vector2 v2)
        {
            imageStick.enabled = b;
            imageStickBg.enabled = b;

            imageStickBg.rectTransform.position = v2;
        }
    }
}