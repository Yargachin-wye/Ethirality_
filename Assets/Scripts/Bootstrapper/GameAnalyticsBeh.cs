using GameAnalyticsSDK;
using UnityEngine;

namespace Bootstrapper
{
    public class GameAnalyticsBeh : MonoBehaviour
    {
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
                Debug.Log("Game Analytics initialized!");
            }
            else
            {
                Debug.Log("Game Analytics not initialized");
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