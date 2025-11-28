using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenerationNew
{
    // Struct for point data
    [Serializable]
    public struct PointData
    {
        public Vector2 position; // Local position within the chunk
        public Color color;
        public float size;
        public string tag; // e.g., "Wall", "Obstacle", "Platform", "Collectible", etc.
    }

// Interface for chunk generators
    public interface IChunkGenerator
    {
        List<PointData> GeneratePoints(float chunkYStart, float chunkHeight, float width, int seed, float difficulty);
    }
}