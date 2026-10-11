using System;
using System.Collections.Generic;
using Definitions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UI.Popups
{
    public enum PopupType
    {
        Damage,
        Heal
    }

    public class PopupManager : MonoBehaviour
    {
        private const int Prewarm = 16;
        private const int SortingOrder = 500;

        private static PopupManager _instance;

        private readonly Queue<PopupItem> _free = new Queue<PopupItem>(Prewarm);

        [SerializeField] private Color playerDamageColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color playerDamageOutline = new Color(0.35f, 0f, 0f);
        [Space]
        [SerializeField] private Color playerHealColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color playerHealOutline = new Color(0f, 0.3f, 0.1f);
        [Space]
        [SerializeField] private Color enemyDamageColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color enemyDamageOutline = new Color(0.35f, 0f, 0f);
        [Space]
        [SerializeField] private Color enemyHealColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color enemyHealOutline = new Color(0f, 0.3f, 0.1f);
        [Space]
        [SerializeField] private Color allDamageColor = new Color(1f, 0.25f, 0.2f);
        [SerializeField] private Color allDamageOutline = new Color(0.35f, 0f, 0f);
        [Space]
        [SerializeField] private Color allHealColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color allHealOutline = new Color(0f, 0.3f, 0.1f);


        private sealed class PopupItem
        {
            public Transform Tr;
            public TextMeshPro Text;
            public Sequence Seq;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
        }

        public static void Show(Vector3 position, int value, PopupType type, Fraction fraction)
        {
            if (_instance == null)
            {
                var go = new GameObject("[Popups]");
                _instance = go.AddComponent<PopupManager>();
                for (int i = 0; i < Prewarm; i++) _instance._free.Enqueue(_instance.Create());
            }

            _instance.Spawn(position, value, type, fraction);
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private PopupItem Create()
        {
            var go = new GameObject("Popup");
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.outlineWidth = 0.25f;
            text.GetComponent<MeshRenderer>().sortingOrder = SortingOrder;
            go.SetActive(false);
            return new PopupItem { Tr = go.transform, Text = text };
        }

        private void Spawn(Vector3 position, int value, PopupType type, Fraction fraction)
        {
            var item = _free.Count > 0 ? _free.Dequeue() : Create();
            bool isDamage = type == PopupType.Damage;
            var text = item.Text;
            var tr = item.Tr;

            float power = Mathf.Clamp01(value / 5f);
            float size = Mathf.Lerp(5f, 8f, power);
            float baseScale = 1f;

            text.text = isDamage ? value.ToString() : "+" + value;
            text.fontSize = size;
            switch (fraction)
            {
                case Fraction.Player:
                    text.color = isDamage ? playerDamageColor : playerHealColor;
                    text.outlineColor = isDamage ? playerDamageOutline : playerHealOutline;
                    break;
                case Fraction.Enemy:
                    text.color = isDamage ? enemyDamageColor : enemyHealColor;
                    text.outlineColor = isDamage ? enemyDamageOutline : enemyHealOutline;
                    break;
                case Fraction.All:
                    text.color = isDamage ? allDamageColor : allHealColor;
                    text.outlineColor = isDamage ? allDamageOutline : allHealOutline;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(fraction), fraction, null);
            }


            text.UpdateMeshPadding();

            float dir = Random.value < 0.5f ? -1f : 1f;
            float driftX = isDamage ? dir * Random.Range(0.4f, 0.9f) : Random.Range(-0.15f, 0.15f);
            float rise = isDamage ? 1.2f : 1.6f;
            float jump = isDamage ? 0.6f : 0f;

            tr.position = position + new Vector3(Random.Range(-0.2f, 0.2f), 0.4f, 0f);
            tr.localScale = Vector3.zero;
            tr.rotation = Quaternion.identity;
            var start = tr.position;
            item.Text.alpha = 1f;
            tr.gameObject.SetActive(true);

            item.Seq?.Kill();
            var seq = DOTween.Sequence().SetLink(tr.gameObject);

            seq.Append(tr.DOScale(baseScale * (isDamage ? 1.4f : 1.2f), 0.12f).SetEase(Ease.OutBack));
            seq.Append(tr.DOScale(baseScale, 0.1f));

            if (isDamage)
            {
                seq.Insert(0f, tr.DOMoveX(start.x + driftX, 0.8f).SetEase(Ease.OutQuad));
                seq.Insert(0f, tr.DOMoveY(start.y + jump, 0.25f).SetEase(Ease.OutQuad));
                seq.Insert(0.25f, tr.DOMoveY(start.y + rise, 0.55f).SetEase(Ease.InQuad));
                seq.Insert(0f, tr.DORotate(new Vector3(0f, 0f, -dir * 12f), 0.8f).SetEase(Ease.OutQuad));
            }
            else
            {
                seq.Insert(0f, tr.DOMoveX(start.x + driftX, 0.9f));
                seq.Insert(0f, tr.DOMoveY(start.y + rise, 0.9f).SetEase(Ease.OutCubic));
            }

            float total = isDamage ? 0.8f : 0.9f;
            seq.Insert(total - 0.3f, DOTween.To(() => text.alpha, a => text.alpha = a, 0f, 0.3f));
            seq.OnComplete(() =>
            {
                tr.gameObject.SetActive(false);
                _free.Enqueue(item);
            });

            item.Seq = seq;
        }
    }
}