using TMPro;
using UnityEngine;

namespace Leaderboard
{
    public static class RunResultText
    {
        // Optional serialized labels can replace these defaults without changing scenes.
        public static TMP_Text Create(Transform parent, string name, float topOffset, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.1f, 1f);
            rect.anchorMax = new Vector2(0.9f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -topOffset);
            rect.sizeDelta = new Vector2(0, height);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 28;
            text.enableAutoSizing = true;
            text.fontSizeMin = 14;
            text.fontSizeMax = 28;
            text.raycastTarget = false;
            text.richText = false;
            return text;
        }

        public static string Height(long units) => (units / (double)RunScore.UnitsPerHeight).ToString("F2");

        // Verdict line shown under the result of a ranked run.
        public static string Verdict(RankedRunStatus status, string rejectReason)
        {
            switch (status)
            {
                case RankedRunStatus.Starting:
                case RankedRunStatus.Recording:
                case RankedRunStatus.Finishing:
                    return "Рейтинг: проверяется…";
                case RankedRunStatus.Verified:
                    return "Рейтинг: принят";
                case RankedRunStatus.Rejected:
                    return string.IsNullOrEmpty(rejectReason)
                        ? "Рейтинг: отклонён"
                        : $"Рейтинг: отклонён — {rejectReason}";
                case RankedRunStatus.Review:
                    return "Рейтинг: на проверке";
                case RankedRunStatus.Failed:
                    return "Рейтинг: нет связи, результат в очереди";
                default:
                    return null;
            }
        }
    }
}
