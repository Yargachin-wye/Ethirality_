using UnityEngine;

namespace CharacterComponents.Moving
{
    public class ReachingToStartMovable : BaseCharacterComponent
    {
        [SerializeField] private float speed;
        
        private Vector2 _startPosition;

        public override void Init()
        {
            _startPosition = transform.position;
        }

        private void FixedUpdate()
        {
            character.rb2D.position = Vector2.Lerp(character.rb2D.position, _startPosition,
                Vector2.Distance(character.rb2D.position, _startPosition) *
                Time.fixedDeltaTime *
                speed);
        }
    }
}