using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LevelUp
{
    [Serializable]
    public class LevelUpPanelAnimation
    {
        [Header("References")]
        [SerializeField] private RawImage[] eyeColumns;
        [SerializeField] private CanvasGroup title;
        [SerializeField] private CanvasGroup[] slots;
        [Header("Show")]
        [SerializeField] private float backgroundFade = 0.3f;
        [SerializeField] private float eyeSlideDuration = 0.85f;
        [SerializeField] private float eyeStagger = 0.08f;
        [SerializeField] private float eyeScrollRush = 1.5f;
        [SerializeField, Range(0f, 1f)] private float eyeStartWidth = 0.15f;
        [SerializeField] private float titleDelay = 0.3f;
        [SerializeField] private float titleOffset = 90f;
        [SerializeField] private float slotsDelay = 0.4f;
        [SerializeField] private float slotStagger = 0.09f;
        [SerializeField] private float slotDuration = 0.5f;
        [Header("Hide")]
        [SerializeField] private float hideDuration = 0.45f;

        private sealed class Eye
        {
            public RawImage Image;
            public RectTransform Rt;
            public Vector2 Pos;
            public Vector3 Scale;
            public Color Color;
            public Rect Uv;
            public float Dir;
            public int Rank;
        }

        private sealed class Item
        {
            public RectTransform Rt;
            public CanvasGroup Group;
            public Vector2 Pos;
            public Vector3 Scale;
        }

        private readonly List<Eye> _eyes = new List<Eye>();
        private readonly List<Item> _slots = new List<Item>();
        private Item _title;
        private CanvasGroup _group;
        private GameObject _root;
        private Sequence _seq;

        public bool IsHiding { get; private set; }

        public void Init(GameObject root, CanvasGroup group)
        {
            _root = root;
            _group = group;

            var eyes = eyeColumns
                .OrderBy(r => r.rectTransform.anchoredPosition.x)
                .ToList();

            for (int i = 0; i < eyes.Count; i++)
            {
                var rt = eyes[i].rectTransform;
                _eyes.Add(new Eye
                {
                    Image = eyes[i],
                    Rt = rt,
                    Pos = rt.anchoredPosition,
                    Scale = rt.localScale,
                    Color = eyes[i].color,
                    Uv = eyes[i].uvRect,
                    Dir = i % 2 == 0 ? 1f : -1f,
                });
            }

            var byCenter = _eyes.OrderBy(e => Mathf.Abs(e.Pos.x)).ToList();
            for (int i = 0; i < byCenter.Count; i++) byCenter[i].Rank = i;

            foreach (var slot in slots) _slots.Add(CreateItem(slot));
            if (title != null) _title = CreateItem(title);
        }

        public void Show()
        {
            Kill();
            IsHiding = false;
            _group.alpha = 0f;
            _group.interactable = false;

            _seq = DOTween.Sequence().SetUpdate(true).SetLink(_root);
            _seq.Insert(0f, Fade(_group, 1f, backgroundFade).SetEase(Ease.OutQuad));

            float parentHeight = ((RectTransform)_root.transform).rect.height;
            foreach (var eye in _eyes)
            {
                float delay = eye.Rank * eyeStagger;
                float offset = (parentHeight + eye.Rt.rect.height) * 0.5f + Mathf.Abs(eye.Pos.y) + 50f;

                eye.Rt.anchoredPosition = eye.Pos + Vector2.up * (eye.Dir * offset);
                eye.Rt.localScale = new Vector3(eye.Scale.x * eyeStartWidth, eye.Scale.y, eye.Scale.z);
                eye.Image.color = WithAlpha(eye.Color, 0f);
                SetUvOffset(eye, eyeScrollRush);

                _seq.Insert(delay, DOTween.To(() => eye.Rt.anchoredPosition, v => eye.Rt.anchoredPosition = v, eye.Pos, eyeSlideDuration)
                    .SetEase(Ease.OutExpo));
                _seq.Insert(delay, DOTween.To(() => eye.Image.color.a, a => eye.Image.color = WithAlpha(eye.Color, a), eye.Color.a, eyeSlideDuration * 0.5f)
                    .SetEase(Ease.OutQuad));
                _seq.Insert(delay + eyeSlideDuration * 0.4f, eye.Rt.DOScaleX(eye.Scale.x, eyeSlideDuration * 0.6f)
                    .SetEase(Ease.OutBack, 2.5f));
                _seq.Insert(delay, DOTween.To(() => eyeScrollRush, k => SetUvOffset(eye, k), 0f, eyeSlideDuration * 1.6f)
                    .SetEase(Ease.OutCubic));
            }

            if (_title != null)
            {
                _title.Group.alpha = 0f;
                _title.Rt.anchoredPosition = _title.Pos + Vector2.up * titleOffset;
                _seq.Insert(titleDelay, Fade(_title.Group, 1f, 0.35f));
                _seq.Insert(titleDelay, DOTween.To(() => _title.Rt.anchoredPosition, v => _title.Rt.anchoredPosition = v, _title.Pos, 0.6f)
                    .SetEase(Ease.OutBack));
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                float delay = slotsDelay + i * slotStagger;
                slot.Group.alpha = 0f;
                slot.Rt.localScale = slot.Scale * 0.7f;
                _seq.Insert(delay, Fade(slot.Group, 1f, slotDuration * 0.6f));
                _seq.Insert(delay, slot.Rt.DOScale(slot.Scale, slotDuration).SetEase(Ease.OutBack, 1.8f));
            }

            _seq.InsertCallback(slotsDelay + _slots.Count * slotStagger, () => _group.interactable = true);
        }

        public void Hide(Action onComplete)
        {
            if (IsHiding) return;
            Kill();
            IsHiding = true;
            _group.interactable = false;

            _seq = DOTween.Sequence().SetUpdate(true).SetLink(_root);
            _seq.Insert(0f, Fade(_group, 0f, hideDuration).SetEase(Ease.InQuad));

            float eyeDuration = hideDuration * 0.7f;
            float stagger = (hideDuration - eyeDuration) / Mathf.Max(1, _eyes.Count - 1);
            foreach (var eye in _eyes)
            {
                float delay = (_eyes.Count - 1 - eye.Rank) * stagger;
                _seq.Insert(delay, eye.Rt.DOScaleX(0f, eyeDuration).SetEase(Ease.InBack, 2f));
                _seq.Insert(0f, DOTween.To(() => 0f, k => SetUvOffset(eye, k), -eyeScrollRush * 0.5f, hideDuration)
                    .SetEase(Ease.InQuad));
            }

            foreach (var slot in _slots)
                _seq.Insert(0f, slot.Rt.DOScale(slot.Scale * 0.9f, hideDuration).SetEase(Ease.InQuad));

            if (_title != null)
                _seq.Insert(0f, DOTween.To(() => _title.Rt.anchoredPosition, v => _title.Rt.anchoredPosition = v,
                    _title.Pos + Vector2.up * (titleOffset * 0.5f), hideDuration).SetEase(Ease.InQuad));

            _seq.OnComplete(() =>
            {
                IsHiding = false;
                Restore();
                onComplete?.Invoke();
            });
        }

        public void Pop(Transform target)
        {
            target.DOKill(true);
            target.DOPunchScale(Vector3.one * 0.15f, 0.35f, 6, 0.6f).SetUpdate(true).SetLink(target.gameObject);
        }

        public void Kill()
        {
            _seq?.Kill();
            _seq = null;
        }

        private void Restore()
        {
            foreach (var eye in _eyes)
            {
                eye.Rt.anchoredPosition = eye.Pos;
                eye.Rt.localScale = eye.Scale;
                eye.Image.color = eye.Color;
                eye.Image.uvRect = eye.Uv;
            }

            foreach (var slot in _slots)
            {
                slot.Rt.localScale = slot.Scale;
                slot.Group.alpha = 1f;
            }

            if (_title != null)
            {
                _title.Rt.anchoredPosition = _title.Pos;
                _title.Group.alpha = 1f;
            }
        }

        private static Item CreateItem(CanvasGroup group)
        {
            var rt = (RectTransform)group.transform;
            return new Item { Rt = rt, Group = group, Pos = rt.anchoredPosition, Scale = rt.localScale };
        }

        private static void SetUvOffset(Eye eye, float k)
        {
            var uv = eye.Uv;
            uv.y += eye.Dir * k;
            eye.Image.uvRect = uv;
        }

        private static Tweener Fade(CanvasGroup group, float to, float duration) =>
            DOTween.To(() => group.alpha, a => group.alpha = a, to, duration);

        private static Color WithAlpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
