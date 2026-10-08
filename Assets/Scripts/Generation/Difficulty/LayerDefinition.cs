using UnityEngine;

namespace Generation
{
    [CreateAssetMenu(menuName = "Generation/Layer Definition")]
    public class LayerDefinition : ScriptableObject
    {
        [SerializeField] private EnemySpawnTable spawnTable;
        [SerializeField] private Vector2 widthRange = new(30, 40);
        [SerializeField] private Vector2 heightRange = new(15, 25);
        [SerializeField, Min(0f)] private float weight = 1f;
        [SerializeField, Range(0f, 1f)] private float minDifficulty;
        [SerializeField, Range(0f, 1f)] private float maxDifficulty = 1f;
        [SerializeField, Min(0f)] private float threatMultiplier = 1f;

        public EnemySpawnTable SpawnTable => spawnTable;
        public Vector2 WidthRange => widthRange;
        public Vector2 HeightRange => heightRange;
        public float Weight => weight;
        public float MinDifficulty => minDifficulty;
        public float MaxDifficulty => maxDifficulty;
        public float ThreatMultiplier => threatMultiplier;

        public bool IsAvailable(float difficulty) => difficulty >= minDifficulty && difficulty <= maxDifficulty;
    }
}
