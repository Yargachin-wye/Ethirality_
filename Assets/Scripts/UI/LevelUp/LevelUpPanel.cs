using System;
using Bootstrapper;
using Definitions;
using UniRx;
using UniRxEvents.GamePlay;
using UniRxEvents.Ui;
using UnityEngine;
using UnityEngine.UI;
using Utilities;
using Random = UnityEngine.Random;

namespace UI.LevelUp
{
    public class LevelUpPanel : BasePanelUi
    {
        public enum Reward
        {
            Null,
            Hp,
            LevelUp,
            Random,
        }
        
        [Space]
        [SerializeField] private LevelUpSlot levelUpSlotHp;
        [SerializeField] private LevelUpSlot levelUpSlot;
        [SerializeField] private LevelUpSlot levelUpSlotRandom;
        [SerializeField] private Button endBtn;
        [Space]
        [SerializeField] private Sprite spriteHp;
        [SerializeField] private Sprite spriteRandom;

        private ImprovementDefinition _currentLevelUp;
        private Reward _reward;
        
        public override void Awake()
        {
            base.Awake();

            MessageBroker.Default
                .Receive<LevelUpEvent>()
                .Subscribe(data => OnLevelUp(data));

            levelUpSlotHp.selectBtn.onClick.AddListener(SelectHp);
            levelUpSlot.selectBtn.onClick.AddListener(SelectLevelUp);
            levelUpSlotRandom.selectBtn.onClick.AddListener(SelectRandomLevelUp);

            endBtn.onClick.AddListener(EndSelect);
        }

        protected override void OnPanelDisable()
        {
            Time.timeScale = 1;
        }

        protected override void OnPanelEnable()
        {
            Time.timeScale = 0;
        }


        private void OnDestroy()
        {
            Time.timeScale = 1;
        }

        protected void OnLevelUp(LevelUpEvent data)
        {
            _currentLevelUp = RandomImprovement();

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(false);

            levelUpSlotHp.SetView(spriteHp, "Восстановите 1 единицу здоровья", 1);
            levelUpSlot.SetView(_currentLevelUp.Preview, _currentLevelUp.Description, -2);
            levelUpSlotRandom.SetView(spriteRandom, "Получите случайное улучшение", -1);
            endBtn.gameObject.SetActive(false);

            _reward = Reward.Null;
        }

        private ImprovementDefinition RandomImprovement()
        {
            return ResManager.Instance.Improvements[Random.Range(0, ResManager.Instance.Improvements.Length)];
        }


        public void SelectHp()
        {
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(true);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(false);
            _reward = Reward.Hp;
        }

        public void SelectLevelUp()
        {
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(true);
            levelUpSlotRandom.Select(false);
            _reward = Reward.LevelUp;
        }

        public void SelectRandomLevelUp()
        {
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(true);

            _reward = Reward.Random;
        }

        public void EndSelect()
        {
            switch (_reward)
            {
                case Reward.Hp:
                {
                    MessageBroker.Default.Publish(new AddHpEvent { Hp = 1 });
                }
                    break;
                case Reward.LevelUp:
                {
                    MessageBroker.Default.Publish(new AddNewImprovementEvent { Definition = _currentLevelUp });
                }
                    break;
                case Reward.Random:
                {
                    ImprovementDefinition def = RandomImprovement();
                    MessageBroker.Default.Publish(new AddNewImprovementEvent { Definition = def });
                }
                    break;
            }
            MessageBroker.Default.Publish(new OpenUiPanelEvent { PanelName = UiConst.GamePlay });
        }
    }
}