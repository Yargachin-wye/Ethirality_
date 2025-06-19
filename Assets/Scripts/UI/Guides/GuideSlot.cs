using Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace UI.Guides
{
    public class GuideSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text guideName;
        [SerializeField] private TMP_Text guideDescription;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RectTransform videoRect;
        [SerializeField] private RawImage rawImage;
        [SerializeField] private Button playButton;
        [SerializeField] private GameObject plug;

        private GuidesPanel _guidesPanel;
        private GuideInfo _guideInfo;

        private void Awake()
        {
            playButton.onClick.AddListener(Play);
            Play();
        }

        public void Play()
        {
            
            _guidesPanel.StopOver();
            videoPlayer.Play();
            plug.SetActive(false);
        }

        public void Stop()
        {
            videoPlayer.Stop();
            plug.SetActive(true);
        }

        public void Init(GuideInfo guideInfo, GuidesPanel guidesPanel)
        {
            _guidesPanel = guidesPanel;
            _guideInfo = guideInfo;
            videoRect.sizeDelta = guideInfo.size;
            guideName.text = guideInfo.guideName;
            guideDescription.text = guideInfo.description;

            videoPlayer.clip = guideInfo.clip;
            videoPlayer.targetTexture = guideInfo.texture;
            rawImage.texture = guideInfo.texture;
            Debug.Log("Assigned texture: " + guideInfo.texture.name);
            Play();
        }
    }
}