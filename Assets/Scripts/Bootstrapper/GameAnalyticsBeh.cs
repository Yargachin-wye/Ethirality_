using Bootstrapper.Saves;
using GameAnalyticsSDK;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
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
            
            MessageBroker.Default
                .Receive<GameStartEvent>()
                .Subscribe(data => GameStart());
            
            MessageBroker.Default
                .Receive<GameOverEvent>()
                .Subscribe(data => GameOver());
            
            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => StartRound());
            
            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => CompleteRound());
            
            MessageBroker.Default
                .Receive<AddNewImprovementEvent>()
                .Subscribe(data => PickupUpgrade(data.IsRandom, data.Definition.ResId));
            
            MessageBroker.Default
                .Receive<AddHpEvent>()
                .Subscribe(data => PickupHp());
        }

        private void OnApplicationQuit()
        {
            if (_isRound)
            {
                GameAnalytics.NewProgressionEvent(
                    GAProgressionStatus.Fail, 
                    "round",
                    SaveSystem.Instance.saveData.currentDifficulty);
            }
        }

        public void GameStart()
        {
            GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Start,
                "run",
                SaveSystem.Instance.saveData.currentDifficulty);
        }

        public void GameOver()
        {
            GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Complete,
                "run",
                SaveSystem.Instance.saveData.currentDifficulty);
        }

        public void StartRound()
        {
            GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Start,
                "round",
                SaveSystem.Instance.saveData.currentDifficulty);
            _isRound = true;
        }

        public void CompleteRound()
        {
            GameAnalytics.NewProgressionEvent(
                GAProgressionStatus.Complete,
                "round",
                SaveSystem.Instance.saveData.currentDifficulty);
            _isRound = false;
        }
        
        public void PickupHp()
        {
            GameAnalytics.NewResourceEvent(
                GAResourceFlowType.Source,
                "null",
                0,
                "health",
                "health"
            );
        }
        
        public void PickupUpgrade(bool isRandom,int id)
        {
            if (isRandom)
            {
                GameAnalytics.NewResourceEvent(
                    GAResourceFlowType.Source,
                    "null",
                    0,
                    "random_upgrade",
                    ResManager.Instance.Improvements[id].ImprovementName
                );
                return;
            }
            
            GameAnalytics.NewResourceEvent(
                GAResourceFlowType.Source,
                "null",
                0,
                "upgrade",
                ResManager.Instance.Improvements[id].ImprovementName
            );
        }
    }
}