using System;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;

namespace UI
{
    public class LobbyBackGround : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenuBackGround;

        private void Awake()
        {
            MessageBroker.Default
                .Receive<StartGameplayEvent>()
                .Subscribe(data => SetActiveTrue());

            MessageBroker.Default
                .Receive<StopGameplayEvent>()
                .Subscribe(data => SetActiveFalse());
        }

        private void SetActiveTrue()
        {
            mainMenuBackGround.SetActive(true);
        }

        private void SetActiveFalse()
        {
            mainMenuBackGround.SetActive(false);
        }
    }
}