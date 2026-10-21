using System.Collections;
using UnityEngine;

namespace Generation
{
    public class RewardGenerator : BaseGenerator
    {
        [SerializeField] private RewardSettings reward = new();
        [SerializeField] private PlacementRules placement = new();
        [SerializeField] private int attemptsPerReward = 10;

        public override IEnumerator Generate(LayerContext layer)
        {
            RewardPlanner.Generate(layer, reward, placement, attemptsPerReward);
            yield break;
        }
    }
}
