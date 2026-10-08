using System.Collections;
using UnityEngine;

namespace Generation
{
    public class EnemyScatterGenerator : BaseGenerator
    {
        [SerializeField] private PlacementRules placement = new();
        [SerializeField] private int attemptsPerEnemy = 10;

        public override IEnumerator Generate(LayerContext layer)
        {
            if (layer.SpawnTable == null) yield break;

            placement.Reset();
            var enemies = layer.SpawnTable.Roll(layer.Random, layer.Difficulty, layer.ThreatBudget);

            foreach (var enemy in enemies)
                placement.TryPlace(layer, enemy, attemptsPerEnemy);

            LayerValidator.Validate(layer, placement);
        }
    }
}
