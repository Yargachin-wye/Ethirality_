using Definitions;
using UnityEngine;

namespace Generation
{
    public readonly struct SpawnRequest
    {
        public readonly CharacterDefinition Definition;
        public readonly Vector2 Position;
        public readonly float Rotation;

        public SpawnRequest(CharacterDefinition definition, Vector2 position, float rotation = 0f)
        {
            Definition = definition;
            Position = position;
            Rotation = rotation;
        }
    }
}
