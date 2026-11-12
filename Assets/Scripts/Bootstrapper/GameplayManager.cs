using System;
using System.Collections;
using Leaderboard;
using UI;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using Utilities;

namespace Bootstrapper
{
    public class GameplayManager : MonoBehaviour
    {
        [SerializeField] private SceneLoader sceneLoader;
        [SerializeField] private BasePanelUi gameOverPanel;
        public static GameplayManager Instance;
        private bool _ending;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogError("GameplayManager is more than one instance!");
                return;
            }

            Instance = this;

            MessageBroker.Default
                .Receive<GameOverEvent>()
                .Subscribe(data =>
                {
                    if (_ending) return;
                    _ending = true;
                    StartCoroutine(OnGameOver());
                });

            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data => GameplayPause.Reset());

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data => GameplayPause.Reset());
        }

        public bool IsTransitioning => _ending;

        private IEnumerator OnGameOver()
        {
            // Capture the terminal position once, before the delay, reset or scene unload.
            OpenWorldRun.Active?.Finish(RunEndReason.Death);
#if UNITY_EDITOR
            RunReplayProbe.Active?.EndSegment("death");
#endif
            yield return new WaitForSeconds(1f);
            yield return LeaveToLobby(() =>
            {
                GameSession.Instance.Session.End();
                return UiConst.GameOver;
            });
        }

        // Transitions run on this persistent object, never on food or panels that die with the scene.
        public void StopGameplay()
        {
            if (_ending || GameSession.Instance.State.Mode != GameMode.Training) return;
            _ending = true;
            StartCoroutine(LeaveToLobby(() => GameSession.Instance.Session.CompleteTrainingStage(
                ResManager.Instance.DifficultyLevelPacks.Length)
                ? UiConst.ChoosingNextLevel
                : UiConst.MainMenu));
        }

        public void ExitToMenu()
        {
            if (_ending) return;
            _ending = true;
            OpenWorldRun.Active?.Finish(RunEndReason.SceneExit);
#if UNITY_EDITOR
            RunReplayProbe.Active?.EndSegment("exit to menu");
#endif
            StartCoroutine(LeaveToLobby(() =>
            {
                GameSession.Instance.Session.End();
                return UiConst.MainMenu;
            }));
        }

        // The caller must have set _ending. Session state changes only after the gameplay scene is gone.
        private IEnumerator LeaveToLobby(Func<string> updateSessionAndGetPanel)
        {
            string panel = null;
            MessageBroker.Default.Publish(new StopRoundEvent());
            GameplayPause.Request(this);
            try
            {
                yield return sceneLoader.LoadLobby();
                panel = updateSessionAndGetPanel();
            }
            finally
            {
                GameplayPause.Release(this);
                _ending = false;
            }

            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = panel });
        }
    }
}