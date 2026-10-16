using System.Collections.Generic;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;

namespace Utilities
{
    public static class GameplayPause
    {
        private static readonly HashSet<object> _requests = new();

        public static bool IsPaused => _requests.Count > 0;

        public static void Request(object requester)
        {
            if (IsPaused || !_requests.Add(requester)) return;
            Apply();
        }

        public static void Release(object requester)
        {
            if (!_requests.Remove(requester) || IsPaused) return;
            Apply();
        }

        public static void Reset()
        {
            if (_requests.Count == 0) return;
            _requests.Clear();
            Apply();
        }

        private static void Apply()
        {
            Physics2D.simulationMode = IsPaused ? SimulationMode2D.Script : SimulationMode2D.FixedUpdate;
            MessageBroker.Default.Publish(new GameplayPauseChangedEvent { IsPaused = IsPaused });
        }
    }
}
