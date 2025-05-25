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
            if (data.PanelName == panelName)
            {
                if (!panel.activeSelf) StartCoroutine(FadeOut());
                OnPanelEnable();
            }
            else
            {
                if (panel.activeSelf) StartCoroutine(FadeIn());
                OnPanelDisable();
            }
        }

        protected virtual IEnumerator FadeOut()
        {
            panel.SetActive(true);
            canvasGroup.alpha = 0;
            while (canvasGroup.alpha < 1)
            {
                canvasGroup.alpha += Time.unscaledDeltaTime * 2;
                yield return null;
            }

            canvasGroup.alpha = 1;
            OnPanelEnable();
        }

        protected virtual IEnumerator FadeIn()
        {
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