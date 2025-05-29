using System.Collections;
using UnityEngine;

namespace Generator
{
    public abstract class BaseGenerator : MonoBehaviour
    {
        protected System.Random Random;
        protected Vector2 Position;

        public virtual IEnumerator Init(System.Random random, Vector2 position)
        {
            Random = random;
            Position = position;
            
            yield return null;
        }
    }
}