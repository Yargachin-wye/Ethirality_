using System;
using System.Collections.Generic;
using Definitions;
using Generation;
using UnityEngine;
using Utilities;

namespace Balance
{
    [CreateAssetMenu(menuName = "Balance/Scenario")]
    public class BalanceScenario : ScriptableObject
    {
        [SerializeField] private DifficultyProfile difficultyProfile;
        [SerializeField] private List<LayerDefinition> layerDefinitions = new();
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private List<ImprovementDefinition> improvements = new();
        [SerializeField] private int seed = 1337;
        [SerializeField] private int attemptsPerEnemy = 10;
        [SerializeField] private PlacementRules placement = new();
        [SerializeField] private int simulatedLayers = 60;

        [Header("Награды (вне бюджета угроз)")]
        [SerializeField] private RewardSettings reward = new();

        [Header("Константы модели (калибруются по аналитике)")]
        [SerializeField] private float exposureRanged = 6f;
        [SerializeField] private float hitProbRanged = 0.35f;
        [SerializeField] private float hitProbHoming = 0.5f;
        [SerializeField] private float pContact = 0.15f;
        [SerializeField] private float pChase = 0.4f;
        [SerializeField] private float eatShare = 0.5f;
        [SerializeField] private float pPickHp = 0.5f;
        [SerializeField] private float spikeDps = 0.25f;
        [SerializeField] private float shieldBlock = 0.25f;

        [Header("Цели баланса")]
        [SerializeField] private int runLengthBaseMin = 8;
        [SerializeField] private int runLengthBaseMax = 14;
        [SerializeField] private int runLengthFullMin = 25;
        [SerializeField] private float maxPlacementLoss = 0.10f;
        [SerializeField] private int rewardWindow = 3;
        [SerializeField] private int rewardCheckFromLayer = 2;

        public DifficultyProfile DifficultyProfile => difficultyProfile;
        public IReadOnlyList<LayerDefinition> LayerDefinitions => layerDefinitions;
        public GameObject PlayerPrefab => playerPrefab;
        public IReadOnlyList<ImprovementDefinition> Improvements => improvements;
        public int Seed => seed;
        public int AttemptsPerEnemy => attemptsPerEnemy;
        public PlacementRules Placement => placement;
        public int SimulatedLayers => simulatedLayers;
        public RewardSettings Reward => reward;

        public float ExposureRanged => exposureRanged;
        public float HitProbRanged => hitProbRanged;
        public float HitProbHoming => hitProbHoming;
        public float PContact => pContact;
        public float PChase => pChase;
        public float EatShare => eatShare;
        public float PPickHp => pPickHp;
        public float SpikeDps => spikeDps;
        public float ShieldBlock => shieldBlock;

        public int RunLengthBaseMin => runLengthBaseMin;
        public int RunLengthBaseMax => runLengthBaseMax;
        public int RunLengthFullMin => runLengthFullMin;
        public float MaxPlacementLoss => maxPlacementLoss;
        public int RewardWindow => rewardWindow;
        public int RewardCheckFromLayer => rewardCheckFromLayer;

        public IReadOnlyList<PlayerBuild> Builds()
        {
            var passive = new List<ImprovementDefinition>();
            AddByName(passive, ImprovementsConst.ArrowUp);
            AddByName(passive, ImprovementsConst.DashUp);
            AddByName(passive, ImprovementsConst.JawUp);

            var full = new List<ImprovementDefinition>(passive);
            for (int i = 0; i < 5; i++)
            {
                AddByName(full, ImprovementsConst.Spike);
                AddByName(full, ImprovementsConst.Shield);
            }

            return new List<PlayerBuild>
            {
                PlayerBuild.From("Base", playerPrefab, null),
                PlayerBuild.From("Passive", playerPrefab, passive),
                PlayerBuild.From("Full", playerPrefab, full)
            };
        }

        private void AddByName(List<ImprovementDefinition> list, string improvementName)
        {
            foreach (var improvement in improvements)
            {
                if (improvement != null && improvement.ImprovementName == improvementName)
                {
                    list.Add(improvement);
                    return;
                }
            }
        }
    }
}
