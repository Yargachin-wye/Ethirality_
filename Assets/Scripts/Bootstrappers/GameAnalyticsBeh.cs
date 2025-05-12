using System.Collections;
using System.Collections.Generic;
using GameAnalyticsSDK;
using TMPro;
using UnityEngine;

namespace Bootstrappers
{
    public class GameAnalyticsBeh : MonoBehaviour
    {
        [SerializeField] private TMP_Text debugText;

        void Start()
        {
            StartCoroutine(StartIE());
        }

        IEnumerator StartIE()
        {
            GameAnalytics.Initialize();
            yield return new WaitForSeconds(1); // Подождать, пока инициализация завершится
            if (GameAnalytics.Initialized)
            {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "round");
                debugText.text = "V";
            }
            else
            {
                debugText.text = "Game Analytics not initialized";
            }
        }
    }
}