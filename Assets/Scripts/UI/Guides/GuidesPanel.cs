using System.Collections.Generic;
using Definitions;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
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

        public override void Awake()
        {
            base.Awake();

            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => _isGameplay = true);

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => _isGameplay = false);

            MessageBroker.Default
                .Receive<FocusGuideEvent>()
                .Subscribe(data => OnFocusGuide(data));

            backBtn.onClick.AddListener(Exit);

            foreach (var guideInfo in guidesDefinition.GuideInfos)
            {
                var go = Instantiate(guidePrefab, container);
                var gs = go.GetComponent<GuideSlot>();
                gs.Init(guideInfo, this);
                gs.Stop();
                _guideSlots.Add(gs);
            }

            _guideSlots[0].Play();
        }

        private void Exit()
        {
            MessageBroker.Default.Publish(new OpenUiPanelEvent
                { PanelName = _isGameplay ? UiConst.GamePlay : UiConst.MainMenu });
        }

        private void OnFocusGuide(FocusGuideEvent data)
        {
        }

        protected override void OnPanelEnable()
        {
            Time.timeScale = 0;
        }

        protected override void OnPanelDisable()
        {
            Time.timeScale = 1;
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