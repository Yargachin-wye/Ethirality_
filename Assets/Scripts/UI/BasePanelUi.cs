using System;
using EditorAttributes;
using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

namespace UI
{
    public abstract class BasePanelUi : MonoBehaviour
    {
        protected string[] PanelsCollection = UiConst.Panels;
        [SerializeField, Dropdown("PanelsCollection")]
        protected string panelName;
        [SerializeField] private GameObject panel;

        public virtual void Awake()
        {
            MessageBroker.Default
                .Receive<OpenUiPanelEvent>()
                .Subscribe(data => SetActivePanel(data));
        }

        protected abstract void OnPanelDisable();
        protected abstract void OnPanelEnable();


        protected virtual void SetActivePanel(OpenUiPanelEvent data)
        {
            Debug.Log($"SetActivePanel: {data.PanelName}");
            if (data.PanelName == panelName)
            {
                panel.SetActive(true);
                OnPanelEnable();
            }
            else
            {
                panel.SetActive(false);
                OnPanelDisable();
            }
        }
    }
}