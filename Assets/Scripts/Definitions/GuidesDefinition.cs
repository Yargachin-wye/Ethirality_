using System;
using System.Collections.Generic;
using UI.Guides;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;

namespace Definitions
{
    [Serializable]
    public class GuideInfo
    {
        [SerializeField, HideInInspector] private int resId;

        public string guideName;
        public string description;
        public VideoClip clip;
        public RenderTexture texture;
        public Vector2 size = new Vector2(440f, 440f);

        public void SetResId(int i)
        {
            resId = i;
        }
    }

    [CreateAssetMenu]
    public class GuidesDefinition : ScriptableObject
    {
        [SerializeField] private List<GuideInfo> guideInfos = new List<GuideInfo>();
        public List<GuideInfo> GuideInfos => guideInfos;
    }
}