using System;
using Definitions;
using UnityEngine;

namespace CharacterComponents
{
    public class ReachingToStartMovable : BaseComponent
    {
        private ReachingToStartMovablePack _reachingToStartMovablePack;
        private Vector2 _startPosition;

        public void Init(ReachingToStartMovablePack reachingToStartMovablePack)
        {
            enabled = reachingToStartMovablePack.isEnable;

            _reachingToStartMovablePack = reachingToStartMovablePack;

            _startPosition = transform.position;
        }

        private void FixedUpdate()
        {
            character.rb2D.position = Vector2.Lerp(character.rb2D.position, _startPosition,
                Vector2.Distance(character.rb2D.position, _startPosition) *
                Time.fixedDeltaTime *
                _reachingToStartMovablePack.speed);
        }
    }
}