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
        public float FoodBudget { get; }
        public float MobilityBudget => Climb?.MobilityBudget ?? 0f;
        public ClimbPlan Climb { get; }
        public LayoutPlan Layout { get; }
        public LayerDefinition Definition { get; }
        public Random Random { get; }
        public List<SpawnRequest> Spawns { get; } = new();
        public List<ChainNode> Chain { get; } = new();

        public readonly struct ChainNode
        {
            public readonly CharacterDefinition Definition;
            public readonly Vector2 Position;
            public readonly bool Harmful;

            public ChainNode(CharacterDefinition definition, Vector2 position, bool harmful)
            {
                Definition = definition;
                Position = position;
                Harmful = harmful;
            }
        }

        public EnemySpawnTable SpawnTable => Definition.SpawnTable;

        public LayerContext(int index, Rect bounds, float difficulty, float threatBudget, float foodBudget, LayerDefinition definition, Random random,
            ClimbPlan climb = null, LayoutPlan layout = null)
        {
            Layout = layout;
            Index = index;
            Bounds = bounds;
            Difficulty = difficulty;
            ThreatBudget = threatBudget;
            FoodBudget = foodBudget;
            Climb = climb;
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
