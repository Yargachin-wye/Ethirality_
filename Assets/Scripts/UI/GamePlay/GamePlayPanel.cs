using UniRx;
using Leaderboard;
using TMPro;
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
        [Space]
        [SerializeField] private GameObject deadZoneAlarm;
        [SerializeField] private TMP_Text runHeightText;
        private long _shownHeight = -1;
        private OpenWorldRun _shownRun;

        public override void Awake()
        {
            base.Awake();
            if (runHeightText == null)
                runHeightText = RunResultText.Create(panel.transform, "Run Height", 65, 44);
            runHeightText.gameObject.SetActive(false);
            MessageBroker.Default
                .Receive<PlayerInDeadZoneEvent>()
                .Subscribe(data => OnPlayerInDeadZone(data));

            MessageBroker.Default
                .Receive<UpdateDashTimerEvent>()
                .Subscribe(data => OnUpdateDashTimer(data));

            MessageBroker.Default
                .Receive<UpdateShotTimerEvent>()
                .Subscribe(data => OnUpdateShotTimer(data));

            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => StartRound());

            deadZoneAlarm.SetActive(false);
        }

        private void StartRound()
        {
            deadZoneAlarm.SetActive(false);
        }

        private void Update()
        {
            if (!IsActive) return;
            var run = OpenWorldRun.Active;
            runHeightText.gameObject.SetActive(run != null);
            if (run == null || (run == _shownRun && run.HeightUnits == _shownHeight)) return;
            _shownRun = run;
            _shownHeight = run.HeightUnits;
            runHeightText.text = $"Высота: {RunResultText.Height(run.HeightUnits)}";
        }

        private void OnPlayerInDeadZone(PlayerInDeadZoneEvent data)
        {
            deadZoneAlarm.SetActive(data.IsDeadZone);
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