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
        private bool _isRound = false;
        
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
                GameAnalytics.NewDesignEvent("TestEvent");
            }
            else
            {
                debugText.text = "Game Analytics not initialized";
            }
        }
        
        private void OnApplicationQuit()
        {
            if (_isRound)
            {
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, "round");
            }
        }

        
        public void StartRound()
        {
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start, "round");
            _isRound = true;
        }

        public void CompleteRound()
        {
            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, "round");
            _isRound = false;
        }
    }
}