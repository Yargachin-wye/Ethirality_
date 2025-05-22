using Bootstrappers;
using Definitions;
using UniRx;
using UniRxEvents.GamePlay;
using UnityEngine;
using UnityEngine.UI;

namespace UI.LevelUp
{
    public class LevelUpUi : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private LevelUpSlot levelUpSlotHp;
        [SerializeField] private LevelUpSlot levelUpSlot;
        [SerializeField] private LevelUpSlot levelUpSlotRandom;
        [SerializeField] private Button endBtn;
        [Space]
        [SerializeField] private Sprite spriteHp;
        [SerializeField] private Sprite spriteRandom;

        private ImprovementDefinition _currentLevelUp;
        private Reward _reward;

        public enum Reward
        {
            Null,
            Hp,
            LevelUp,
            Random,
        }

        private void Awake()
        {
            MessageBroker.Default
                .Receive<LevelUpEvent>()
                .Subscribe(data => OnLevelUp(data));

            levelUpSlotHp.selectBtn.onClick.AddListener(SelectHp);
            levelUpSlot.selectBtn.onClick.AddListener(SelectLevelUp);
            levelUpSlotRandom.selectBtn.onClick.AddListener(SelectRandomLevelUp);

            endBtn.onClick.AddListener(EndSelect);
            panel.SetActive(false);
        }

        private ImprovementDefinition RandomImprovement()
        {
            return ResManager.instance.Improvements[Random.Range(0, ResManager.instance.Improvements.Length)];
        }

        private void OnDisable()
        {
            Time.timeScale = 1;
        }

        private void OnDestroy()
        {
            Time.timeScale = 1;
        }

        private void OnLevelUp(LevelUpEvent data)
        {
            Time.timeScale = 0;

            _currentLevelUp = RandomImprovement();

            levelUpSlotHp.Select(false);
            levelUpSlot.Select(false);
            levelUpSlotRandom.Select(false);

            levelUpSlotHp.SetView(spriteHp, "Восстановите 1 единицу здоровья", 1);
            levelUpSlot.SetView(_currentLevelUp.Preview, _currentLevelUp.Description, -2);
            levelUpSlotRandom.SetView(spriteRandom, "Получите случайное улучшение", -1);

            panel.SetActive(true);
            endBtn.gameObject.SetActive(false);

            _reward = Reward.Null;
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

            panel.SetActive(false);
            Time.timeScale = 1;
        }
    }
}