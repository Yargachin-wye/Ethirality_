using System;
using System.Collections.Generic;

namespace Bootstrapper
{
    public enum GameMode { None, OpenWorld, Training }

    public sealed class RunState
    {
        public GameMode Mode { get; }
        public int TrainingStage { get; }
        public int Hp { get; set; }
        public List<int> UpgradeIds { get; } = new List<int>();

        public RunState(GameMode mode, int hp, int trainingStage = 0)
        {
            Mode = mode;
            Hp = hp;
            TrainingStage = trainingStage;
        }
    }

    // Session-only state: never serialized, loaded from disk or resumed after a restart.
    public sealed class GameSessionState
    {
        public int StartingHp { get; }
        public RunState Current { get; private set; }
        public bool TrainingStagePending { get; private set; }
        public bool TrainingCompleted { get; private set; }

        public GameSessionState(int startingHp)
        {
            if (startingHp <= 0) throw new ArgumentOutOfRangeException(nameof(startingHp));
            StartingHp = startingHp;
            End();
        }

        public void StartOpenWorld()
        {
            End();
            Current = new RunState(GameMode.OpenWorld, StartingHp);
        }

        public void StartTraining()
        {
            int stage = TrainingStagePending ? Current.TrainingStage : 0;
            End();
            Current = new RunState(GameMode.Training, StartingHp, stage);
        }

        public bool CompleteTrainingStage(int stageCount)
        {
            if (stageCount <= 0) throw new ArgumentOutOfRangeException(nameof(stageCount));
            if (Current.Mode != GameMode.Training || TrainingStagePending) return false;
            int next = Current.TrainingStage + 1;
            End();
            if (next >= stageCount)
            {
                TrainingCompleted = true;
                return false;
            }
            // Each lesson has a fresh player; only its position in the course survives the menu.
            Current = new RunState(GameMode.Training, StartingHp, next);
            TrainingStagePending = true;
            return true;
        }

        public void End()
        {
            Current = new RunState(GameMode.None, StartingHp);
            TrainingStagePending = false;
            TrainingCompleted = false;
        }
    }
}
