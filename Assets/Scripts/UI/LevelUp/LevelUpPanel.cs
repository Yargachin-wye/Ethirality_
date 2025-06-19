using System;
using System.Collections.Generic;
using Audio;
using Bootstrapper;
using Bootstrapper.Saves;
using Constants;
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
        [SerializeField] private Sprite spriteBlocked;

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
            levelUpSlotHp.Select(false);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(false);

            levelUpSlotHp.SetView(spriteHp, "Восстановите 1 единицу здоровья", 1);
            if (RandomImprovement(out _currentLevelUp))
            {
                levelUpSlot.SetView(_currentLevelUp.Preview, _currentLevelUp.Description, -2);
                levelUpSlotRandom.SetView(spriteRandom, "Получите случайное улучшение", -1);
            } 
            else
            {
                levelUpSlot.SetView(spriteBlocked, "Заблокированно", -2,true);
                levelUpSlotRandom.SetView(spriteBlocked, "Заблокированно", -1,true);
            }
            
            endBtn.gameObject.SetActive(false);

            _reward = Reward.Null;
        }

        private bool RandomImprovement(out ImprovementDefinition imp)
        {
            Dictionary<int, int> flags = new Dictionary<int, int>();
            foreach (var upgradeId in SaveSystem.Instance.saveData.playerUpgradeResIds)
            {
                if (!flags.ContainsKey(upgradeId))
                {
                    flags.Add(upgradeId, 1);
                }
                else
                {
                    flags[upgradeId]++;
                }
            }

            List<ImprovementDefinition> availableImprovements = new List<ImprovementDefinition>();

            for (int i = 0; i < ResManager.Instance.Improvements.Length; i++)
            {
                int currentLevel = flags.ContainsKey(i) ? flags[i] : 0;
                if (currentLevel < ResManager.Instance.Improvements[i].MaxLevel)
                {
                    availableImprovements.Add(ResManager.Instance.Improvements[i]);
                }
            }
            
            if (availableImprovements.Count == 0)
            {
                imp = null;
                return false;
            }
            
            imp = availableImprovements[Random.Range(0, availableImprovements.Count)];
            return true;
        }
        
        public void SelectHp()
        {
            if(!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(true);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(false);
            _reward = Reward.Hp;
        }

        public void SelectLevelUp()
        {
            if(!IsActive) return;
            if(levelUpSlot.IsBlocked) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(true);
            levelUpSlotRandom.Select(false);
            _reward = Reward.LevelUp;
        }

        public void SelectRandomLevelUp()
        {
            if(!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
            if(levelUpSlotRandom.IsBlocked) return;
            endBtn.gameObject.SetActive(true);

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(true);

            _reward = Reward.Random;
        }

        public void EndSelect()
        {
            if(!IsActive) return;
            AudioManager.Instance.PlayUISound(AudioConst.UiClick);
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
                    ImprovementDefinition def;
                    if (RandomImprovement(out def))
                    {
                        MessageBroker.Default.Publish(new AddNewImprovementEvent { Definition = def });
                    }
                    else
                    {
                        // MessageBroker.Default.Publish(new AddNewImprovementEvent { Definition = def });
                    }
                }
                    break;
            }
            AudioManager.Instance.PlayUISound(AudioConst.Reward);
            MessageBroker.Default.Publish(new SetActivePanelEvent { PanelName = UiConst.GamePlay });
        }
    }
}