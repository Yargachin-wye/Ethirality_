using System;
using Managers;
using Leaderboard;
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
#if UNITY_EDITOR
            if (RunReplayProbe.TryQueue(this, new RunInput { kind = RunInputKind.Shot, x = v2.x, y = v2.y })) return;
#endif
            RunRecorder.RecordInput(this, new RunInput { kind = RunInputKind.Shot, x = v2.x, y = v2.y });
            onShotEvent?.Invoke(v2 - (Vector2)transform.position);
        }

        private void OnDash(Vector2 v2)
        {
#if UNITY_EDITOR
            if (RunReplayProbe.TryQueue(this, new RunInput { kind = RunInputKind.Dash, x = v2.x, y = v2.y })) return;
#endif
            RunRecorder.RecordInput(this, new RunInput { kind = RunInputKind.Dash, x = v2.x, y = v2.y });
            onDashEvent?.Invoke(v2);
        }

        private void OnFreeze(bool freez, Vector2 v2)
        {
#if UNITY_EDITOR
            if (RunReplayProbe.TryQueue(this, new RunInput
                { kind = RunInputKind.Freeze, pressed = freez, x = v2.x, y = v2.y })) return;
#endif
            RunRecorder.RecordInput(this, new RunInput
                { kind = RunInputKind.Freeze, pressed = freez, x = v2.x, y = v2.y });
            onFreezeEvent?.Invoke(freez, v2);
        }

        private void OnLook(Vector2 v2)
        {
#if UNITY_EDITOR
            if (RunReplayProbe.TryQueue(this, new RunInput { kind = RunInputKind.Look, x = v2.x, y = v2.y })) return;
#endif
            RunRecorder.RecordInput(this, new RunInput { kind = RunInputKind.Look, x = v2.x, y = v2.y });
            onLookEvent?.Invoke(v2);
        }

#if UNITY_EDITOR
        // Bypasses live-input interception; called before movement's FixedUpdate by the probe.
        public void ApplyRunInput(RunInput input)
        {
            var value = new Vector2(input.x, input.y);
            switch (input.kind)
            {
                case RunInputKind.Look: onLookEvent?.Invoke(value); break;
                case RunInputKind.Freeze: onFreezeEvent?.Invoke(input.pressed, value); break;
                case RunInputKind.Dash: onDashEvent?.Invoke(value); break;
                case RunInputKind.Shot: onShotEvent?.Invoke(value - (Vector2)transform.position); break;
            }
        }
#endif
    }
}