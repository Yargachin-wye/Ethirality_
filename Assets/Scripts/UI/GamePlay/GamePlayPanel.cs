using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace UI.GamePlay
{
    public class GamePlayPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private Image dashTimerField;
        [SerializeField] private GameObject dashTimer;

        public override void Awake()
        {
            base.Awake();
            MessageBroker.Default
                .Receive<UpdateDashTimerEvent>()
                .Subscribe(data => OnUpdateDashTimer(data));
        }

        private void OnUpdateDashTimer(UpdateDashTimerEvent data)
        {
            if (data.DashTimer <= 0)
            {
                dashTimer.gameObject.SetActive(false);
            }
            else
            {
                dashTimer.gameObject.SetActive(true);
                dashTimerField.fillAmount = data.DashTimer;
            }
        }

        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
        }
    }
}