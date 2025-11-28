using System.Collections.Generic;
using GenerationNew.Generators;
using UnityEngine;

namespace GenerationNew
{
    public class ChunkManager : MonoBehaviour
    {
        public float chunkHeight = 100f;
        public float worldWidth = 25f;
        public int seed = 42;
        public Transform player; // Reference to player transform
        public float loadDistance = 50f; // Distance to load next chunk
        public float unloadDistance = 150f; // Distance to unload bottom chunk

        [SerializeField] private List<BaseChunkGenerator> generators = new List<BaseChunkGenerator>(); // List of generators to combine
        [SerializeField] private BoundaryGenerator boundaryGenerator; // Always include boundaries

        private Queue<GameObject> activeChunks = new Queue<GameObject>();
        private Stack<GameObject> chunkPool = new Stack<GameObject>();

        private float currentTopY = 0f;
        private float currentBottomY = 0f;

        void Start()
        {
            // Preload initial chunks
            for (int i = 0; i < 3; i++) // Load 3 chunks initially
            {
                LoadNextChunk();
            }
        }

        void Update()
        {
            float playerY = player.position.y;

            // Load next if player near top
            if (playerY > currentTopY - loadDistance)
            {
                LoadNextChunk();
            }

            // Unload bottom if player far from bottom
            if (playerY > currentBottomY + unloadDistance && activeChunks.Count > 2)
            {
                UnloadBottomChunk();
            }
        }

        private void LoadNextChunk()
        {
            GameObject chunkObj = GetPooledChunk();
    
            // Метаданные чанка
            PointRendererGizmos.ChunkData chunkData = chunkObj.GetComponent<PointRendererGizmos.ChunkData>();
            chunkData.height = chunkHeight;
            chunkData.yStart = currentTopY;

            chunkObj.transform.position = new Vector3(0, currentTopY, 0);

            PointRendererGizmos renderer = chunkObj.GetComponent<PointRendererGizmos>();

            float difficulty = currentTopY / 10000f; // как и раньше

            List<PointData> allPoints = new List<PointData>();

            // Границы всегда
            allPoints.AddRange(boundaryGenerator.GeneratePoints(currentTopY, chunkHeight, worldWidth, seed, difficulty));

            // Остальные генераторы
            foreach (var gen in generators)
            {
                if (ShouldUseGenerator(gen, difficulty))
                    allPoints.AddRange(gen.GeneratePoints(currentTopY, chunkHeight, worldWidth, seed, difficulty));
            }

            renderer.SetPoints(allPoints);

            activeChunks.Enqueue(chunkObj);
            currentTopY += chunkHeight;
        }

        private bool ShouldUseGenerator(BaseChunkGenerator gen, float difficulty)
        {
            // Example logic: Enable based on difficulty thresholds
            if (gen is EmptyGenerator) return difficulty < 0.5f; // Early game
            if (gen is ObstacleGenerator) return difficulty > 0.2f;
            if (gen is PlatformGenerator) return true; // Always
            if (gen is CollectibleGenerator) return difficulty < 0.8f; // Less collectibles later
            return true;
        }

        private void UnloadBottomChunk()
        {
            GameObject chunkToUnload = activeChunks.Dequeue();
            chunkToUnload.SetActive(false);
            chunkPool.Push(chunkToUnload);
            currentBottomY += chunkHeight;
        }

        private GameObject GetPooledChunk()
        {
            GameObject chunkObj;

            if (chunkPool.Count > 0)
            {
                chunkObj = chunkPool.Pop();
            }
            else
            {
                chunkObj = new GameObject("Chunk_Gizmos");
                chunkObj.AddComponent<PointRendererGizmos>();
                chunkObj.AddComponent<PointRendererGizmos.ChunkData>(); // для отображения границ при выборе
            }

            // Сбрасываем состояние
            chunkObj.transform.position = Vector3.zero;
            chunkObj.SetActive(true);
            return chunkObj;
        }
    }
}