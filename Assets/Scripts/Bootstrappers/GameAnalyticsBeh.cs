using System.Collections;
using GameAnalyticsSDK;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Bootstrappers
{
    public class GameAnalyticsBeh : MonoBehaviour
    {
        [SerializeField] private TMP_Text debugText;
        public static GameAnalyticsBeh Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("More than one instance of GameAnalyticsBeh found!");
                return;
            }

            Instance = this;

            GameAnalytics.Initialize();

            if (GameAnalytics.Initialized)
            {
                debugText.text = "V";
                GameAnalytics.NewDesignEvent("TestEvent"); // Просто для проверки
            }
            else
            {
                debugText.text = "Game Analytics not initialized";
            }
        }


        private void OnApplicationQuit()
        {
        }

        public void StartRound()
        {
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "round");
        }

        public void CompleteRound()
        {
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "round");
        }
    }
}