﻿using System;
using Managers;
using UnityEngine;
using UnityEngine.Events;

namespace CharacterComponents
{
    public class PlayerControllable : BaseCharacterComponent
    {
        [SerializeField] private UnityEvent<bool, Vector2> onFreezeEvent;
        [SerializeField] private UnityEvent<Vector2> onDashEvent;
        [SerializeField] private UnityEvent<Vector2> onShotEvent;
        [SerializeField] private UnityEvent<Vector2> onLookEvent;
        private float _gravityScale;

        private void Start()
        {
            Inputer.Instance.OnInputFreeze += OnFreeze;
            Inputer.Instance.OnInputDash += OnDash;
            Inputer.Instance.OnInputShot += OnShot;
            Inputer.Instance.OnInputLook += OnLook;
        }

        private void OnDestroy()
        {
            Inputer.Instance.OnInputFreeze -= OnFreeze;
            Inputer.Instance.OnInputDash -= OnDash;
            Inputer.Instance.OnInputShot -= OnShot;
            Inputer.Instance.OnInputLook -= OnLook;
        }

        public override void Init()
        {
        }

        private void OnShot(Vector2 v2)
        {
            onShotEvent?.Invoke(v2 - (Vector2)transform.position);
        }

        private void OnDash(Vector2 v2)
        {
            onDashEvent?.Invoke(v2);
        }

        private void OnFreeze(bool freez, Vector2 v2)
        {
            onFreezeEvent?.Invoke(freez, v2);
        }

        private void OnLook(Vector2 v2)
        {
            onLookEvent?.Invoke(v2);
        }
    }
}