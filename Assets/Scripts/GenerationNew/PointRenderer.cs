using System.Collections.Generic;
using UnityEngine;

// Простейший рендерер точек через Gizmos
namespace GenerationNew
{
    [RequireComponent(typeof(ChunkData))] // чтобы было удобно видеть границы чанка в редакторе
    public class PointRendererGizmos : MonoBehaviour
    {
        [SerializeField, HideInInspector] private List<PointData> points = new List<PointData>();
        [SerializeField] private ChunkManager chunkManager;

        // Цвета по тегам (можно расширять)
        private static readonly Dictionary<string, Color> TagColors = new()
        {
            { "Wall",        new Color(0.4f, 0.4f, 0.4f) },
            { "Obstacle",    Color.red },
            { "Platform",    Color.green },
            { "Collectible", Color.yellow },
            { "Background",  Color.white },
        };

        // Вызывается из ChunkManager после генерации
        public void SetPoints(List<PointData> newPoints)
        {
            points = newPoints;
        }

        private void OnDrawGizmos()
        {
            if (points == null || points.Count == 0) return;

            foreach (var p in points)
            {
                Vector3 worldPos = transform.position + new Vector3(p.position.x, p.position.y, 0f);

                // Цвет по тегу, если нет — по данным точки
                Color gizmoColor = TagColors.TryGetValue(p.tag, out Color c) ? c : p.color;
                gizmoColor.a = 1f;

                Gizmos.color = gizmoColor;

                // Рисуем круг (или квадрат, если хочешь)
                Gizmos.DrawSphere(worldPos, p.size * 0.5f);

                // Опционально: маленький квадратик по центру для лучшей видимости
                // Gizmos.DrawWireCube(worldPos, Vector3.one * p.size * 0.3f);
            }
        }

        // Для удобства в редакторе — рисуем границы чанка
        private void OnDrawGizmosSelected()
        {
            ChunkData data = GetComponent<ChunkData>();
            if (data != null)
            {
                Vector3 pos = transform.position;
                Vector3 size = new Vector3(chunkManager.worldWidth, data.height, 0f);
                Gizmos.color = new Color(0f, 0.7f, 1f, 0.3f);
                Gizmos.DrawWireCube(pos + Vector3.up * size.y * 0.5f, size);
            }
        }
        public class ChunkData : MonoBehaviour
        {
            public float height = 100f;
            public float yStart;
        }
    }
}