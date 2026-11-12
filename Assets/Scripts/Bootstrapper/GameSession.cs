using UnityEngine;

namespace Bootstrapper
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField, Min(1)] private int startingHp = 4;
        public static GameSession Instance { get; private set; }
        public GameSessionState Session { get; private set; }
        public RunState State => Session.Current;
        public bool GuidesShownThisSession { get; set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("More than one GameSession.");
                return;
            }
            Instance = this;
            Session = new GameSessionState(startingHp);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
