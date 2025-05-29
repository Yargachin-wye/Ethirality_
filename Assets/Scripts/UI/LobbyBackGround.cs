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
                .Receive<StartRoundEvent>()
                .Subscribe(data => SetActiveTrue());

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => SetActiveFalse());
        }

        private void SetActiveTrue()
        {
            mainMenuBackGround.SetActive(false);
        }

        private void SetActiveFalse()
        {
            mainMenuBackGround.SetActive(true);
        }
    }
}