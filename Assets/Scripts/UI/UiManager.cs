using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

public class UiManager : MonoBehaviour
{
    private void Start()
    {
        MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.MainMenu });
    }
}
