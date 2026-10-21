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

            LayerPopulator.Populate(layer, placement, attemptsPerEnemy);
            LayerValidator.Validate(layer, placement);
        }
    }
}
