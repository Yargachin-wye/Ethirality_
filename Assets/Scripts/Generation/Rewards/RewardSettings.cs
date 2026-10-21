using System;
using Definitions;
using UnityEngine;

namespace Generation
{
    [Serializable]
    public class RewardSettings
    {
        [SerializeField] private CharacterDefinition rewardDefinition;
        [SerializeField, Min(0), Tooltip("Первый слой с наградой")] private int firstRewardLayer = 2;
        [SerializeField, Min(1), Tooltip("Награда гарантирована не реже чем раз в N слоёв")] private int rewardEveryLayers = 3;
        [SerializeField, Tooltip("Мин/макс наград на слой с наградой")] private Vector2Int rewardsPerLayer = new(1, 2);

        public CharacterDefinition RewardDefinition => rewardDefinition;
        public int FirstRewardLayer => firstRewardLayer;
        public int RewardEveryLayers => Mathf.Max(1, rewardEveryLayers);
        public int MinRewards => Mathf.Max(1, rewardsPerLayer.x);
        public int MaxRewards => Mathf.Max(MinRewards, rewardsPerLayer.y);

        public bool IsRewardLayer(int layerIndex) =>
            rewardDefinition != null && layerIndex >= firstRewardLayer &&
            (layerIndex - firstRewardLayer) % RewardEveryLayers == 0;
    }
}
