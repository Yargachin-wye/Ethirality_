using Definitions;

namespace UniRxEvents
{
    namespace Improvement
    {
        public struct AddImprovementEvent
        {
            public Improvements.Improvement Improvement;
        }
        public struct RemoveImprovementEvent
        {
            public Improvements.Improvement Improvement;
        }
    }

    namespace GamePlay
    {
        public struct LevelUpEvent
        {
            
        }
        public struct AddNewImprovementEvent
        {
            public ImprovementDefinition Definition;
        }
        public struct AddHpEvent
        {
            public int Hp;
        }
    }
}