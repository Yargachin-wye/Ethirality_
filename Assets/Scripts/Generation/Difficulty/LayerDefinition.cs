using System;
using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Layer Definition")]
    public class LayerDefinition : ScriptableObject
    {
        [Serializable]
        public struct RoleShare
        {
            public EnemySpawnTable.EnemyRole role;
            [Min(0f)] public float share;
        }

        [SerializeField] private EnemySpawnTable spawnTable;
        [SerializeField, Tooltip("Доли бюджета по ролям (нормализуются). Пусто = свободный взвешенный выбор.")]
        private RoleShare[] composition;
        [SerializeField] private Vector2 widthRange = new(30, 40);
        [SerializeField] private Vector2 heightRange = new(15, 25);
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Range(0f, 1f)] private float minDifficulty;
        [SerializeField, Range(0f, 1f)] private float maxDifficulty = 1f;
        [SerializeField, Min(0f)] private float threatMultiplier = 1f;

        public EnemySpawnTable SpawnTable => spawnTable;
        public IReadOnlyList<RoleShare> Composition => composition;
        public Vector2 WidthRange => widthRange;
        public Vector2 HeightRange => heightRange;
        public float Weight => weight;
        public float MinDifficulty => minDifficulty;
        public float MaxDifficulty => maxDifficulty;
        public float ThreatMultiplier => threatMultiplier;

        public bool IsAvailable(float difficulty) => difficulty >= minDifficulty && difficulty <= maxDifficulty;
    }
}
