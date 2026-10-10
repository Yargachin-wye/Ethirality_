using System.Collections.Generic;
using CharacterComponents;
using Generation;
using Pools;
using UnityEngine;

namespace Spawner
{
    public class LevelSpawner : MonoBehaviour
    {
        private readonly Dictionary<LayerContext, List<Character>> _spawned = new();
        private static CharactersPool CharactersPool => CharactersPool.Instance;

        public void Spawn(LevelData level)
        {
            foreach (var layer in level.Layers) SpawnLayer(layer);
        }

        public void SpawnLayer(LayerContext layer)
        {
            var list = new List<Character>(layer.Spawns.Count);
            foreach (var request in layer.Spawns)
            {
                Character character = CharactersPool.GetPooledObject(request.Definition);
                character.gameObject.SetActive(true);
                character.transform.position = request.Position;
                character.transform.rotation = Quaternion.Euler(0, 0, request.Rotation);
                character.Init(request.Definition);
                list.Add(character);
            }

            _spawned[layer] = list;
        }

        public void DespawnLayer(LayerContext layer)
        {
            if (!_spawned.Remove(layer, out var list)) return;
            foreach (var character in list)
                if (character != null) character.Off();
        }
    }
}
