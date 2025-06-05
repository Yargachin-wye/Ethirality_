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
        [Space]
        [SerializeField] private Image shotTimerField;
        [SerializeField] private GameObject shotTimer;
        public override void Awake()
        {
            base.Awake();
            MessageBroker.Default
                .Receive<UpdateDashTimerEvent>()
                .Subscribe(data => OnUpdateDashTimer(data));
            
            MessageBroker.Default
                .Receive<UpdateShotTimerEvent>()
                .Subscribe(data => OnUpdateShotTimer(data));
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
        
        private void OnUpdateShotTimer(UpdateShotTimerEvent data)
        {
            if (data.ShotTimer <= 0)
            {
                shotTimer.gameObject.SetActive(false);
            }
            else
            {
                shotTimer.gameObject.SetActive(true);
                shotTimerField.fillAmount = data.ShotTimer;
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