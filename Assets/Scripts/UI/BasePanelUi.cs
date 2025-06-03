using System;
using System.Collections;
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
        [SerializeField] private CanvasGroup canvasGroup;
        protected bool IsActive = false;
        public virtual void Awake()
        {
            MessageBroker.Default
                .Receive<OpenUiPanelEvent>()
                .Subscribe(data => OpenPanel(data));
            
            MessageBroker.Default
                .Receive<SetActivePanelEvent>()
                .Subscribe(data => SetActivePanel(data));
        }

        private void SetActivePanel(SetActivePanelEvent data)
        {
            if (data.PanelName == panelName)
            {
                if (!panel.activeSelf) panel.SetActive(true);
                IsActive = true;
                OnPanelEnable();
            }
            else
            {
                if (panel.activeSelf) panel.SetActive(false);
                IsActive = false;
                OnPanelDisable();
            }
        }

        protected abstract void OnPanelDisable();
        protected abstract void OnPanelEnable();


        public virtual void OpenPanel(OpenUiPanelEvent data)
        {
            if (data.PanelName == panelName)
            {
                if (!panel.activeSelf) StartCoroutine(FadeOut());
                IsActive = true;
                OnPanelEnable();
                
            }
            else
            {
                if (panel.activeSelf) StartCoroutine(FadeIn());
                IsActive = false;
                OnPanelDisable();
                
            }
        }

        public virtual IEnumerator FadeOut()
        {
            panel.SetActive(true);
            canvasGroup.alpha = 0;
            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.unscaledDeltaTime * 2;
                yield return null;
            }

            canvasGroup.alpha = 1;
            IsActive = true;
            OnPanelEnable();
            
        }

        protected virtual IEnumerator FadeIn()
        {
            IsActive = false;
            OnPanelDisable();
            
            canvasGroup.alpha = 1;
            while (canvasGroup.alpha > 0)
            {
                canvasGroup.alpha -= Time.unscaledDeltaTime * 2;
                yield return null;
            }

            canvasGroup.alpha = 0;
            panel.SetActive(false);
        }
    }
}