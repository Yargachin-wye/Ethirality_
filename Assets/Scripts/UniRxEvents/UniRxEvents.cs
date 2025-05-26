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

    namespace Ui
    {
        public struct OpenUiPanelEvent
        {
            public string PanelName;
        }
        public struct SetActivePanelEvent
        {
            public string PanelName;
        } 
    }
    namespace GamePlay
    {
        public struct StartGameplayEvent
        {
            
        }
        public struct StopGameplayEvent
        {
            
        }
        public struct GameOverEvent
        {
            
        }
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