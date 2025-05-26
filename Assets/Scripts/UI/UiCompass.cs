using System.Collections.Generic;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;

namespace UI
{
    public class UiCompass : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private RectTransform compassImage;

        public static UiCompass Instance;
        [SerializeField] private List<Transform> _exits = new();
        public Vector2 Direction { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("More than one UiCompass instance found!");
                return;
            }

            MessageBroker.Default
                .Receive<StopGameplayEvent>()
                .Subscribe(data => OnStopGamePlay(data));

            Instance = this;
        }

        private void OnStopGamePlay(StopGameplayEvent data)
        {
            _exits = new();
        }

        public void AddExit(Transform exit)
        {
            if (!_exits.Contains(exit))
            {
                _exits.Add(exit);
            }
        }

        public void RemoveExit(Transform exit)
        {
            if (_exits.Contains(exit))
            {
                _exits.Remove(exit);
            }
        }

        private void Update()
        {
            if (_exits.Count <= 0)
            {
                return;
            }

            Transform nearestExit = null;
            float nearestDistanceSqr = float.MaxValue;

            Vector2 cameraPos = cameraTransform.position;
            
            foreach (var exit in _exits)
            {
                Vector2 exitPos = exit.position;
                float distSqr = (exitPos - cameraPos).sqrMagnitude;
                if (distSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distSqr;
                    nearestExit = exit;
                }
            }

            if (nearestExit != null)
            {
                Vector2 directionWorld = (Vector2)(nearestExit.position - cameraTransform.position);
                Direction = directionWorld.normalized;
                
                float angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;
                
                compassImage.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }
        }
    }
}