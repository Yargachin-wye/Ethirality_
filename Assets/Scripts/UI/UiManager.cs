using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

namespace UI
{
    public class UiManager : MonoBehaviour
    {
        private void Start()
        {
            MessageBroker.Default.Publish(new SetActivePanelEvent { PanelName = UiConst.MainMenu });
        }
    }
}