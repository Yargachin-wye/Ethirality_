using EditorAttributes;
using UnityEngine;
using Utilities;

namespace Definitions
{
    [CreateAssetMenu]
    public class ImprovementDefinition : ScriptableObject
    {
        public static string[] AllImps = ImprovementsConst.AllImps;
        
        [SerializeField, HideInInspector] private int resId;
        public int ResId => resId;

        [SerializeField] private GameObject prefab;
        [SerializeField] private Fraction fraction;
        [SerializeField] private Sprite preview;
        [SerializeField] private string description;
        [SerializeField, Dropdown("AllImps")] private string improvementName;
        [SerializeField] private bool isPassive;
        
        public string ImprovementName => improvementName;

        public GameObject Prefab => prefab;
        public Sprite Preview => preview;
        public Fraction Fraction => fraction;
        public string Description => description;
        public bool IsPassive => isPassive;

        public void SetResId(int i)
        {
            resId = i;
        }
    }
}