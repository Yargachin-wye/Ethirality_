using System.Collections.Generic;
using Definitions;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities;

namespace UI.Guides
{
    public class GuidesPanel : BasePanelUi
    {
        [Space]
        [SerializeField] private GameObject guidePrefab;
        [SerializeField] private Transform container;
        [Space]
        [SerializeField] private GuidesDefinition guidesDefinition;
        [Space]
        [SerializeField] private Button backBtn;

        private List<GuideSlot> _guideSlots = new List<GuideSlot>();

        private bool _isGameplay;
        private string _previousPanel = "";
        public override void Awake()
        {
            base.Awake();

            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => _isGameplay = true);

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => _isGameplay = false);

            backBtn.onClick.AddListener(Exit);

            foreach (var guideInfo in guidesDefinition.GuideInfos)
            {
                var go = Instantiate(guidePrefab, container);
                var gs = go.GetComponent<GuideSlot>();
                gs.Init(guideInfo, this);
                gs.Stop();
                _guideSlots.Add(gs);
            }
        }
        private void Update()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleGuides();
            }
        }

        private void ToggleGuides()
        {
            if (IsActive)
            {
                MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = _previousPanel });
            }
        }

        private void Exit()
        {
            MessageBroker.Default.Publish(new OpenUiPanelEvent
                { PanelName = _isGameplay ? UiConst.GamePlay : UiConst.MainMenu });
        }

        public override void OpenPanel(OpenUiPanelEvent data)
        {
            base.OpenPanel(data);
            if (data.PanelName != panelName) _previousPanel = data.PanelName;
        }
        
        protected override void OnPanelEnable()
        {
            
            Time.timeScale = 0;
            _guideSlots[0].Play();
            StopOver();
            
        }

        protected override void OnPanelDisable()
        {
            if (IsActive) Time.timeScale = 1;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
        }

        public void StopOver()
        {
            foreach (var guideSlot in _guideSlots)
            {
                guideSlot.Stop();
            }
        }
    }
}