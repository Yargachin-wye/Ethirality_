using System.Collections;
using System.Collections.Generic;
using Audio;
using Bootstrapper;
using Constants;
using Leaderboard;
using TMPro;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.ChoosingNextLevel
{
    public class ChoosingNextLevelPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private Button randomLevelBtn;
        [SerializeField] private Button openWorldLevelBtn;
        [SerializeField] private Button backBtn;
        [SerializeField] private Image fieldDifficulty;
        [SerializeField] private Button rankedStartBtn;
        [SerializeField] private TMP_Text rankedStatusText;
        private bool _loading;
        private TMP_Text _trainingLabel;

        public override void Awake()
        {
            base.Awake();
            randomLevelBtn.onClick.AddListener(StartTraining);
            openWorldLevelBtn.onClick.AddListener(StartOpenWorld);
            backBtn.onClick.AddListener(BackMenu);
            _trainingLabel = randomLevelBtn.GetComponentInChildren<TMP_Text>(true);

            // The ranked button reuses the OpenWorld button's look; it is
            // created in code so older scenes need no prefab changes.
            if (rankedStartBtn == null)
            {
                var source = openWorldLevelBtn.GetComponent<RectTransform>();
                rankedStartBtn = Instantiate(openWorldLevelBtn, openWorldLevelBtn.transform.parent);
                rankedStartBtn.name = "RankedStartBtn";
                rankedStartBtn.GetComponent<RectTransform>().anchoredPosition =
                    source.anchoredPosition - new Vector2(0, source.rect.height + 12f);
                var label = rankedStartBtn.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.text = "Рейтинговый забег";
            }
            rankedStartBtn.onClick.RemoveAllListeners();
            rankedStartBtn.onClick.AddListener(StartRanked);
            if (rankedStatusText == null)
            {
                var sourceLabel = openWorldLevelBtn.GetComponentInChildren<TMP_Text>(true);
                if (sourceLabel != null)
                {
                    rankedStatusText = Instantiate(sourceLabel, rankedStartBtn.transform.parent);
                    rankedStatusText.name = "RankedStatus";
                    var statusRect = rankedStatusText.GetComponent<RectTransform>();
                    var buttonRect = rankedStartBtn.GetComponent<RectTransform>();
                    statusRect.anchoredPosition = buttonRect.anchoredPosition
                        - new Vector2(0, buttonRect.rect.height / 2f + statusRect.rect.height);
                    rankedStatusText.text = "";
                }
            }
        }


        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            var session = GameSession.Instance.Session;
            int count = ResManager.Instance.DifficultyLevelPacks.Length;
            fieldDifficulty.fillAmount = count > 0 ? (float)session.Current.TrainingStage / count : 0;
            if (_trainingLabel != null)
                _trainingLabel.text = session.TrainingStagePending
                    ? $"Обучение: урок {session.Current.TrainingStage + 1}\nБез рейтинга"
                    : "Обучение\nБез рейтинга";
            RefreshRankedState();
        }

        private void RefreshRankedState()
        {
            var client = LeaderboardClient.Instance;
            bool configured = client != null && client.Configured;
            bool online = Application.internetReachability != NetworkReachability.NotReachable;
            rankedStartBtn.interactable = configured && online && !_loading;
            if (rankedStatusText != null)
                rankedStatusText.text = !configured ? "Рейтинг недоступен"
                    : !online ? "Нет сети" : "";
        }

        private void StartTraining()
        {
            if (!IsActive || _loading) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            var session = GameSession.Instance.Session;
            int stage = session.TrainingStagePending ? session.Current.TrainingStage : 0;
            if (!TrySelectLevel(stage, true, out string scene)) return;
            session.StartTraining();
            StartCoroutine(StartLevel(scene));
        }

        private void StartOpenWorld()
        {
            if (!IsActive || _loading) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            if (!TrySelectLevel(0, false, out string scene)) return;
            GameSession.Instance.Session.StartOpenWorld();
            StartCoroutine(StartLevel(scene));
        }

        // Ranked start asks the server for runId/seed first; generation uses
        // that seed via GeneratorsManager.
        private void StartRanked()
        {
            if (!IsActive || _loading) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            StartCoroutine(StartRankedRoutine());
        }

        private IEnumerator StartRankedRoutine()
        {
            _loading = true;
            rankedStartBtn.interactable = false;
            if (rankedStatusText != null) rankedStatusText.text = "Подключение к рейтингу…";
            bool ok = false;
            var client = LeaderboardClient.Instance;
            if (client != null)
                yield return client.StartCoroutine(
                    RankedRunService.StartAsync(null, null, v => ok = v));
            if (!ok)
            {
                if (rankedStatusText != null)
                    rankedStatusText.text = "Не удалось подключиться к рейтингу";
                _loading = false;
                RefreshRankedState();
                yield break;
            }
            if (!TrySelectLevel(0, false, out string scene))
            {
                RankedRunService.Abandon();
                _loading = false;
                RefreshRankedState();
                yield break;
            }
            GameSession.Instance.Session.StartOpenWorld();
            StartCoroutine(StartLevel(scene));
        }

        private bool TrySelectLevel(int stage, bool training, out string scene)
        {
            scene = null;
            var packs = ResManager.Instance.DifficultyLevelPacks;
            if (stage < 0 || stage >= packs.Length) return false;
            var levels = training ? packs[stage].randomLevelName : packs[stage].openWorldLevelName;
            if (levels == null || levels.Length == 0) return false;
            scene = levels[Random.Range(0, levels.Length)].levelName;
            if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene)) return true;
            Debug.LogError($"Scene is not available in Build Settings: {scene}");
            return false;
        }

        private IEnumerator StartLevel(string nextLevelName)
        {
            _loading = true;
            LocalRunStore.ClearLastResult();
            yield return StartCoroutine(sceneLoader.Load(nextLevelName));
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GamePlay });
            MessageBroker.Default.Publish(new StartRoundEvent());
            if (GameSession.Instance.State.TrainingStage == 0)
            {
                MessageBroker.Default.Publish(new GameStartEvent());
            }

            yield return new WaitForSeconds(0.1f);
            if (GameSession.Instance.State.Mode == GameMode.Training
                && !GameSession.Instance.GuidesShownThisSession)
            {
                GameSession.Instance.GuidesShownThisSession = true;
                MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.Guides });
            }
            _loading = false;
        }

        private void BackMenu()
        {
            if (!IsActive || _loading) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            GameSession.Instance.Session.End();

            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}