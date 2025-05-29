using UnityEngine;

namespace Definitions
{
    [CreateAssetMenu]
    public class ImprovementDefinition : ScriptableObject
    {
        [SerializeField, HideInInspector] private int resId;
        public int ResId => resId;

        [SerializeField] private GameObject prefab;
        [SerializeField] private Fraction fraction;
        [SerializeField] private Sprite preview;
        [SerializeField] private string description;
        [SerializeField] private string improvementName;
        
        public string ImprovementName => improvementName;

        public GameObject Prefab => prefab;
        public Sprite Preview => preview;
        public Fraction Fraction => fraction;
        public string Description => description;

        public void SetResId(int i)
        {
            resId = i;
        }
    }
}