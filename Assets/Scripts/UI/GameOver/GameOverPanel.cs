using Audio;
using Constants;
using Leaderboard;
using TMPro;
using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.GameOver
{
    public class GameOverPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private Button backToMenuBtn;
        [SerializeField] private TMP_Text runResultText;

        public override void Awake()
        {
            base.Awake();
            if (runResultText == null)
                runResultText = RunResultText.Create(panel.transform, "Run Result", 140, 140);
            backToMenuBtn.onClick.AddListener(ExitToMenu);
        }

        protected override void OnPanelDisable()
        {
            RankedRunService.OnStatusChanged -= OnRankedStatus;
        }

        protected override void OnPanelEnable()
        {
            RankedRunService.OnStatusChanged += OnRankedStatus;
            Refresh();
        }

        private void OnRankedStatus(RankedRunStatus status) => Refresh();

        private void Refresh()
        {
            var result = LocalRunStore.LastResult;
            runResultText.gameObject.SetActive(result != null);
            if (result == null) return;
            if (!result.valid)
            {
                runResultText.text = "Результат забега недоступен";
                return;
            }
            string label = result.diagnostic ? "Тестовый забег"
                : LocalRunStore.LastWasPersonalBest ? "Новый локальный рекорд!" : "Локальный результат";
            runResultText.text = $"{label}\nВысота: {RunResultText.Height(result.heightUnits)}\n"
                + $"Версия: {result.gameVersion}";

            string activeRunId = RankedRunService.Current != null
                ? RankedRunService.Current.runId
                : RankedRunService.LastFinishedRunId;
            if (result.runId == activeRunId)
            {
                string verdict = RunResultText.Verdict(
                    RankedRunService.Status, RankedRunService.LastRejectReason);
                if (verdict != null) runResultText.text += "\n" + verdict;
            }
        }

        private void ExitToMenu()
        {
            if(!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}