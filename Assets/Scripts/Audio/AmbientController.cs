using Constants;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;
using Utilities;

namespace Audio
{
    // Lives on the AudioManager object (added by AudioManager.Awake).
    // Gameplay track plays only while a round is running and not paused; everything else
    // (menus, lobby, pause, level-up, guides) plays the UI track. Each track keeps its
    // position when switched away; the gameplay track restarts when a run starts and the
    // UI track restarts when the run ends.
    public class AmbientController : MonoBehaviour
    {
        private bool _roundActive;

        private void Awake()
        {
            MessageBroker.Default
                .Receive<StartRoundEvent>()
                .Subscribe(data =>
                {
                    _roundActive = true;
                    AudioManager.Instance?.RestartAmbient(AudioConst.GameplayAmbient);
                })
                .AddTo(this);

            MessageBroker.Default
                .Receive<StopRoundEvent>()
                .Subscribe(data =>
                {
                    _roundActive = false;
                    AudioManager.Instance?.RestartAmbient(AudioConst.UiAmbient);
                })
                .AddTo(this);
        }

        private void Update()
        {
            var audio = AudioManager.Instance;
            if (audio == null) return;
            bool gameplay = _roundActive && !GameplayPause.IsPaused;
            audio.PlayAmbient(gameplay ? AudioConst.GameplayAmbient : AudioConst.UiAmbient);
        }
    }
}
