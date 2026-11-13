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
    // Top-100 for the current ranked board plus the player's own row.
    // Visual tree can be edited in the scene: use the component's context
    // menu "Build Panel UI" once to generate the default layout, then tweak
    // it like any other panel. If `panel` is left unassigned the default
    // layout is built at runtime instead.
    public class LeaderboardPanel : BasePanelUi
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text contentText;
        [SerializeField] private Button closeBtn;

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
            else AutoWire();
            base.Awake();
            if (closeBtn != null) closeBtn.onClick.AddListener(Close);
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
            if (contentText != null) contentText.text = "";
            var client = LeaderboardClient.Instance;
            string boardId = RankedRunService.Current != null
                ? RankedRunService.Current.leaderboardId
                : RankedRunService.LastLeaderboardId;
            if (string.IsNullOrEmpty(boardId)) boardId = "current";
            if (client == null || !client.Configured)
            {
                titleText.text = "Рейтинг";
                statusText.text = "Таблица лидеров недоступна";
                return;
            }
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                titleText.text = "Рейтинг";
                statusText.text = "Нет сети";
                return;
            }
            statusText.text = "Загрузка…";
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
                titleText.text = "Рейтинг";
                statusText.text = "Не удалось загрузить таблицу";
                yield break;
            }
            var parsed = JsonUtility.FromJson<BoardResponse>(result.body);
            if (parsed?.leaderboard == null)
            {
                statusText.text = "Не удалось загрузить таблицу";
                yield break;
            }
            var board = parsed.leaderboard;
            titleText.text = $"{board.ruleset_id} · seed {board.seed}";
            if (board.status != "open")
            {
                statusText.text = board.status == "archived"
                    ? "Таблица в архиве" : "Таблица закрывается";
            }
            else statusText.text = "";

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
            contentText.text = sb.ToString();
        }

        private void Close()
        {
            if (!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            MessageBroker.Default.Publish(
                new OpenUiPanelEvent { PanelName = UiConst.ChoosingNextLevel });
        }

        // Finds default children by name when fields were not assigned in the
        // inspector. Rename freely if you assign the fields manually.
        private void AutoWire()
        {
            if (titleText == null) titleText = FindText("Title");
            if (statusText == null) statusText = FindText("Status");
            if (contentText == null) contentText = FindText("Content");
            if (closeBtn == null)
                closeBtn = panel.transform.Find("CloseBtn")?.GetComponent<Button>();
        }

        private TMP_Text FindText(string childName)
        {
            var child = panel.transform.Find(childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        [ContextMenu("Build Panel UI")]
        private void BuildUiMenuItem()
        {
            if (panel != null)
            {
                Debug.Log("LeaderboardPanel: panel already assigned");
                return;
            }
            BuildUi();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
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

            titleText = RunResultText.Create(rect, "Title", 40, 60);
            statusText = RunResultText.Create(rect, "Status", 100, 40);
            statusText.fontSizeMax = statusText.fontSize = 20;
            contentText = RunResultText.Create(rect, "Content", 160, 400);
            contentText.alignment = TextAlignmentOptions.TopLeft;
            contentText.fontSizeMax = contentText.fontSize = 22;

            var closeGo = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(rect, false);
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0, 40);
            closeRect.sizeDelta = new Vector2(220, 60);
            closeBtn = closeGo.GetComponent<Button>();
            var closeLabel = RunResultText.Create(closeRect, "Label", 0, 60);
            closeLabel.text = "Назад";
            closeLabel.raycastTarget = false;

            panel.SetActive(false);
        }
    }
}
