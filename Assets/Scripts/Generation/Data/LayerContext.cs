using System.Collections.Generic;
using Definitions;
using UnityEngine;
using Random = System.Random;

namespace Generation
{
    public sealed class LayerContext
    {
        public int Index { get; }
        public Rect Bounds { get; }
        public float Difficulty { get; }
        public float ThreatBudget { get; }
        public LayerDefinition Definition { get; }
        public Random Random { get; }
        public List<SpawnRequest> Spawns { get; } = new();

        public EnemySpawnTable SpawnTable => Definition.SpawnTable;

        public LayerContext(int index, Rect bounds, float difficulty, float threatBudget, LayerDefinition definition, Random random)
        {
            Index = index;
            Bounds = bounds;
            Difficulty = difficulty;
            ThreatBudget = threatBudget;
            Definition = definition;
            Random = random;
        }

        public void AddSpawn(CharacterDefinition definition, Vector2 position, float rotation = 0f)
        {
            Spawns.Add(new SpawnRequest(definition, position, rotation));
        }

        public Vector2 RandomPoint(float margin = 0f)
        {
            float x = Mathf.Lerp(Bounds.xMin + margin, Bounds.xMax - margin, (float) Random.NextDouble());
            float y = Mathf.Lerp(Bounds.yMin + margin, Bounds.yMax - margin, (float) Random.NextDouble());
            return new Vector2(x, y);
        }
    }
}
