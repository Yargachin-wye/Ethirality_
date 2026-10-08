using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    public class EnemyScatterGenerator : BaseGenerator
    {
        [SerializeField] private float edgeMargin = 2f;
        [SerializeField] private float minSpacing = 2f;
        [SerializeField] private int attemptsPerEnemy = 10;

        public override IEnumerator Generate(LayerContext layer)
        {
            if (layer.SpawnTable == null) yield break;

            var enemies = layer.SpawnTable.Roll(layer.Random, layer.Difficulty, layer.ThreatBudget);
            float sqrSpacing = minSpacing * minSpacing;
            var placed = new List<Vector2>();

            foreach (var enemy in enemies)
            {
                for (int a = 0; a < attemptsPerEnemy; a++)
                {
                    Vector2 p = layer.RandomPoint(edgeMargin);
                    if (placed.Exists(o => (o - p).sqrMagnitude < sqrSpacing)) continue;
                    placed.Add(p);
                    layer.AddSpawn(enemy, p);
                    break;
                }
            }
        }
    }
}
