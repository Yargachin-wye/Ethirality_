using System.Collections.Generic;
using UnityEngine;

namespace UI.Animations
{
    namespace UI.Animations
    {
        public class AnimationHorizontalGroup : MonoBehaviour
        {
            [Header("Wave Settings")]
            [Tooltip("Амплитуда смещения pivot (0–0.5 рекомендуется)")]
            [Range(0f, 0.5f)]
            public float amplitude = 0.1f;

            [Tooltip("Скорость распространения волны (рад/сек)")]
            public float waveSpeed = 2f;

            [Tooltip("Расстояние по фазе между соседними элементами")]
            public float phaseOffset = 0.5f;

            private List<RectTransform> _children;
            private List<float> _initialPivotY;

            private void Awake()
            {
                CacheChildren();
            }

            void OnValidate()
            {
                CacheChildren();
            }

            private void CacheChildren()
            {
                _children = new List<RectTransform>();
                _initialPivotY = new List<float>();

                foreach (Transform t in transform)
                {
                    var rt = t as RectTransform;
                    if (rt != null)
                    {
                        _children.Add(rt);
                        _initialPivotY.Add(rt.pivot.y);
                    }
                }
            }

            void Update()
            {
                if (_children == null || _children.Count == 0)
                    return;

                float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

                for (int i = 0; i < _children.Count; i++)
                {
                    var rt = _children[i];
                    float baseY = _initialPivotY[i];

                    float phase = time * waveSpeed + i * phaseOffset;

                    float newPivotY = baseY + Mathf.Sin(phase) * amplitude;

                    newPivotY = Mathf.Clamp01(newPivotY);

                    rt.pivot = new Vector2(rt.pivot.x, newPivotY);
                }
            }
        }
    }
}