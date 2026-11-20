using System;
using System.Collections;
using System.Collections.Generic;
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
    // Top-100 for the current ranked board as individual slots: each player is
    // one row. Ranks 1..10 use `topSlots` — ten separate texts that can be
    // styled individually (drag your own TMP texts into the array, or restyle
    // the generated SlotTop01..10 in the scene). The top slots are always
    // visible: ranks without a player keep a placeholder (`emptyTopSlotText`).
    // Rows 11+ are clones of `slotTemplate`, so all regular rows share one
    // look (same material/color).
    // Visual tree can be edited in the scene: use the component's context
    // menu "Build Panel UI" once to generate the default layout, then tweak
    // it like any other panel. If `panel` is left unassigned the default
    // layout is built at runtime instead.
    public class LeaderboardPanel : BasePanelUi
    {
        private const int TopCount = 10;

        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        // Ranks 1..10: one slot per rank, styled individually.
        [SerializeField] private TMP_Text[] topSlots = new TMP_Text[TopCount];
        // Shown in top slots whose rank has no player yet.
        [SerializeField] private string emptyTopSlotText = "—";
        // Rows 11+ (and fallback for missing top slots): cloned at runtime,
        // all clones share one style.
        [SerializeField] private TMP_Text slotTemplate;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Button closeBtn;

        private readonly List<TMP_Text> regularSlots = new List<TMP_Text>();

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
            ClearSlots();
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

            Populate(parsed);
        }

        private void Populate(BoardResponse parsed)
        {
            ClearSlots();
            int count = parsed.top?.Length ?? 0;
            int topCount = Mathf.Min(count, TopCount);
            // Top slots are always visible: filled ranks show the entry, the
            // rest stay as placeholders.
            for (int i = 0; i < TopCount; i++)
            {
                if (i < topCount)
                {
                    var e = parsed.top[i];
                    ShowSlot(GetTopSlot(i), FormatEntry(e.rank, e.nickname, e.heightUnits));
                }
                else
                {
                    ShowSlot(GetTopSlot(i), $"{i + 1}. {emptyTopSlotText}");
                }
            }
            for (int i = topCount; i < count; i++)
            {
                var e = parsed.top[i];
                ShowSlot(GetRegularSlot(), FormatEntry(e.rank, e.nickname, e.heightUnits));
            }
            if (count == 0)
                ShowSlot(GetRegularSlot(), "Пока пусто - стань первым.");
            if (parsed.self == null) return;
            // The self row is skipped when it is already shown in the top slots.
            bool shownInTop = parsed.self.@public && parsed.top != null &&
                              parsed.self.rank >= 1 &&
                              parsed.self.rank <= Mathf.Min(parsed.top.Length, TopCount);
            if (shownInTop) return;
            string selfText = $"Вы: {parsed.self.rank} место — {RunResultText.Height(parsed.self.heightUnits)}";
            if (!parsed.self.@public)
                selfText += "\n(на проверке — в общем топе не виден)";
            ShowSlot(GetRegularSlot(), selfText);
        }

        private static string FormatEntry(int rank, string nickname, long heightUnits)
        {
            string name = string.IsNullOrEmpty(nickname) ? "Игрок" : nickname;
            return $"{rank}. {name} - {RunResultText.Height(heightUnits)}";
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
            if (closeBtn == null)
                closeBtn = panel.transform.Find("CloseBtn")?.GetComponent<Button>();
            if (topSlots == null || topSlots.Length != TopCount) topSlots = new TMP_Text[TopCount];
            var content = panel.transform.Find("Content");
            if (content == null) return;
            if (contentRoot == null) contentRoot = content as RectTransform;
            for (int i = 0; i < TopCount; i++)
            {
                if (topSlots[i] != null) continue;
                var child = content.Find(SlotTopName(i));
                if (child != null) topSlots[i] = child.GetComponent<TMP_Text>();
            }
            if (slotTemplate == null)
            {
                var tpl = content.Find("SlotRegularTemplate");
                if (tpl != null) slotTemplate = tpl.GetComponent<TMP_Text>();
            }
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
                Debug.Log("LeaderboardPanel: panel already assigned, use \"Rebuild Panel UI\" to regenerate");
                return;
            }
            BuildUi();
            MarkDirty();
        }

        // Destroys everything under the panel and regenerates the default
        // layout (use once when migrating from the old single-text version).
        [ContextMenu("Rebuild Panel UI")]
        private void RebuildUiMenuItem()
        {
            if (panel == null)
            {
                BuildUiMenuItem();
                return;
            }
            bool wasActive = panel.activeSelf;
            var children = new List<GameObject>();
            foreach (Transform child in panel.transform) children.Add(child.gameObject);
            foreach (var child in children)
            {
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
            regularSlots.Clear();
            contentRoot = null;
            slotTemplate = null;
            topSlots = new TMP_Text[TopCount];
            BuildUi();
            if (wasActive) panel.SetActive(true);
            MarkDirty();
        }

        private void MarkDirty()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void BuildUi()
        {
            RectTransform rect;
            if (panel == null)
            {
                panel = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(Image));
                var canvas = GetComponentInParent<Canvas>();
                panel.transform.SetParent(canvas != null ? canvas.transform : transform, false);
                rect = (RectTransform)panel.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
            }
            else
            {
                rect = (RectTransform)panel.transform;
            }

            titleText = RunResultText.Create(rect, "Title", 40, 60);
            statusText = RunResultText.Create(rect, "Status", 100, 40);
            statusText.fontSizeMax = statusText.fontSize = 20;

            if (topSlots == null || topSlots.Length != TopCount) topSlots = new TMP_Text[TopCount];
            EnsureContent();
            for (int i = 0; i < TopCount; i++)
            {
                if (topSlots[i] != null) continue;
                topSlots[i] = CreateSlot(SlotTopName(i), 24);
            }
            EnsureSlotResources();

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

        // Slot container: rows are stacked by a VerticalLayoutGroup, so top
        // slots with different font sizes and regular clones align naturally.
        private void EnsureContent()
        {
            if (contentRoot != null) return;
            var found = panel.transform.Find("Content");
            if (found is RectTransform rect)
            {
                if (rect.GetComponent<VerticalLayoutGroup>() == null)
                    rect.gameObject.AddComponent<VerticalLayoutGroup>();
                contentRoot = rect;
                return;
            }
            var go = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(panel.transform, false);
            rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.1f, 1f);
            rect.anchorMax = new Vector2(0.9f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -160);
            rect.sizeDelta = new Vector2(0, 400);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            contentRoot = rect;
        }

        private void EnsureSlotResources()
        {
            EnsureContent();
            if (slotTemplate == null)
            {
                slotTemplate = CreateSlot("SlotRegularTemplate", 20);
                slotTemplate.gameObject.SetActive(false);
            }
        }

        private TMP_Text CreateSlot(string name, float fontSize)
        {
            EnsureContent();
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(contentRoot, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Left;
            text.fontSize = fontSize;
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        private void ClearSlots()
        {
            if (topSlots != null)
            {
                foreach (var slot in topSlots)
                    if (slot != null) slot.gameObject.SetActive(false);
            }
            for (int i = 0; i < regularSlots.Count; i++)
            {
                var slot = regularSlots[i];
                if (slot == null)
                {
                    regularSlots.RemoveAt(i);
                    i--;
                }
                else slot.gameObject.SetActive(false);
            }
        }

        // Ranks 1..10 prefer the dedicated slot, missing ones fall back to the
        // regular template style.
        private TMP_Text GetTopSlot(int index)
        {
            if (topSlots != null && index < topSlots.Length && topSlots[index] != null)
                return topSlots[index];
            return GetRegularSlot();
        }

        // Reuses an inactive clone from the pool, otherwise clones the
        // template. Clones stay in creation order, so row order matches the
        // API order.
        private TMP_Text GetRegularSlot()
        {
            EnsureSlotResources();
            for (int i = 0; i < regularSlots.Count; i++)
            {
                var slot = regularSlots[i];
                if (slot == null)
                {
                    regularSlots.RemoveAt(i);
                    i--;
                    continue;
                }
                if (!slot.gameObject.activeSelf) return slot;
            }
            var clone = Instantiate(slotTemplate, contentRoot);
            clone.gameObject.SetActive(true);
            clone.name = $"SlotRegular_{regularSlots.Count + 1}";
            regularSlots.Add(clone);
            return clone;
        }

        private static void ShowSlot(TMP_Text slot, string text)
        {
            if (slot == null) return;
            slot.gameObject.SetActive(true);
            slot.text = text;
        }

        private static string SlotTopName(int index) => $"SlotTop{index + 1:00}";
    }
}