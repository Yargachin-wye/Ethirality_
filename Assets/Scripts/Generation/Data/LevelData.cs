using System.Collections.Generic;
using UnityEngine;

namespace Generation
{
    public sealed class LevelData
    {
        public int Seed { get; }
        public List<LayerContext> Layers { get; } = new();
        public List<Vector2> PlayerSpawns { get; } = new();

        public LevelData(int seed)
        {
            Seed = seed;
        }
    }
}
