using CharacterComponents;
using Generation;
using Pools;
using UnityEngine;

namespace Spawner
{
    public class LevelSpawner : MonoBehaviour
    {
        private static CharactersPool CharactersPool => CharactersPool.Instance;

        public void Spawn(LevelData level)
        {
            foreach (var layer in level.Layers)
            {
                foreach (var request in layer.Spawns)
                {
                    Character character = CharactersPool.GetPooledObject(request.Definition);
                    character.gameObject.SetActive(true);
                    character.transform.position = request.Position;
                    character.transform.rotation = Quaternion.Euler(0, 0, request.Rotation);
                    character.Init(request.Definition);
                }
            }
        }
    }
}
