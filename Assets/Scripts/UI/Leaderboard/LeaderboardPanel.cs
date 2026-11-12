using System;
using System.Collections;
using System.Text;
using Audio;
using Constants;
using Leaderboard;
using TMPro;
using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;

namespace UI.LeaderboardPanel
{
    // Top-100 for the current ranked board plus the player's own row. Builds
    // its UI in code (like RunResultText) so it works without prefab changes;
    // states: loading, no-network, archived board.
    public class LeaderboardPanel : BasePanelUi
    {
        private TMP_Text _titleText;
        private TMP_Text _contentText;
        private TMP_Text _statusText;
        private Button _closeBtn;

        [Serializable] private sealed class BoardInfo
        {
            public string id;
            public string ruleset_id;
            public int seed;
            public string status;
            public string closes_at;
        }

        [Serializable] private sealed class TopEntry
        {
            public int rank;
            public string nickname;
            public long heightUnits;
            public string achievedAt;
        }

        [Serializable] private sealed class SelfEntry
        {
            public int rank;
            public long heightUnits;
            public string verdict;
            public bool @public;
        }

        [Serializable] private sealed class BoardResponse
        {
            public BoardInfo leaderboard;
            public TopEntry[] top;
            public SelfEntry self;
        }

        public override void Awake()
        {
            if (string.IsNullOrEmpty(panelName)) panelName = UiConst.Leaderboard;
            if (panel == null) BuildUi();
            base.Awake();
            _closeBtn.onClick.AddListener(Close);
        }

        protected override void OnPanelDisable()
        {
        }

        protected override void OnPanelEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            _contentText.text = "";
            var client = LeaderboardClient.Instance;
            string boardId = RankedRunService.Current != null
                ? RankedRunService.Current.leaderboardId
                : RankedRunService.LastLeaderboardId;
            if (client == null || !client.Configured || boardId == null)
            {
                _titleText.text = "Рейтинг";
                _statusText.text = "Таблица лидеров недоступна";
                return;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                _titleText.text = "Рейтинг";
                _statusText.text = "Нет сети";
                return;
            }
            _statusText.text = "Загрузка…";
            StartCoroutine(Load(client, boardId));
        }

        private IEnumerator Load(LeaderboardClient client, string boardId)
        {
            LeaderboardClient.Result result = null;
            yield return client.StartCoroutine(client.SignedGet(
                $"leaderboards/{boardId}?limit=100", r => result = r));
            if (!IsActive) yield break;
            if (result == null || !result.ok)
            {
                _titleText.text = "Рейтинг";
                _statusText.text = "Не удалось загрузить таблицу";
                yield break;
            }
            var parsed = JsonUtility.FromJson<BoardResponse>(result.body);
            if (parsed?.leaderboard == null)
            {
                _statusText.text = "Не удалось загрузить таблицу";
                yield break;
            }
            var board = parsed.leaderboard;
            _titleText.text = $"{board.ruleset_id} · seed {board.seed}";
            if (board.status != "open")
            {
                _statusText.text = board.status == "archived"
                    ? "Таблица в архиве" : "Таблица закрывается";
            }
            else _statusText.text = "";

            var sb = new StringBuilder(4096);
            if (parsed.top == null || parsed.top.Length == 0)
                sb.AppendLine("Пока пусто — стань первым.");
            else
                foreach (var e in parsed.top)
                    sb.Append(e.rank).Append(". ")
                        .Append(string.IsNullOrEmpty(e.nickname) ? "Игрок" : e.nickname)
                        .Append(" — ").Append(RunResultText.Height(e.heightUnits))
                        .Append('\n');
            if (parsed.self != null)
            {
                sb.Append("\nВы: ").Append(parsed.self.rank).Append(" место — ")
                    .Append(RunResultText.Height(parsed.self.heightUnits));
                if (!parsed.self.@public)
                    sb.Append("\n(на проверке — в общем топе не виден)");
            }
            _contentText.text = sb.ToString();
        }

        private void Close()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(
                new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
        }

        private void BuildUi()
        {
            panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
            var canvas = GetComponentInParent<Canvas>();
            panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var bg = panel.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.9f);

            _titleText = RunResultText.Create(rect, "Title", 40, 60);
            _statusText = RunResultText.Create(rect, "Status", 100, 40);
            _statusText.fontSizeMax = _statusText.fontSize = 20;
            _contentText = RunResultText.Create(rect, "Content", 160, 400);
            _contentText.alignment = TextAlignmentOptions.TopLeft;
            _contentText.fontSizeMax = _contentText.fontSize = 22;

            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(rect, false);
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 40);
            closeRect.sizeDelta = new Vector2(220, 60);
            _closeBtn = closeGo.GetComponent<Button>();
            var closeLabel = RunResultText.Create(closeRect, "Label", 0, 60);
            closeLabel.text = "Назад";
            closeLabel.raycastTarget = false;

            panel.SetActive(false);
        }
    }
}
