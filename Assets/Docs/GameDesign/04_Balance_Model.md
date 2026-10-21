# 04. Модель баланса

Цель калибровки (решение владельца проекта): **длина забега**. Система должна позволять
сказать: «игрок без улучшений в среднем доходит до слоя N, с полной сборкой — до M», и
проверять это при каждом изменении чисел.

Реализация: `Assets/Scripts/Balance/` (чистый C#, без сцены), конфигурация —
`BalanceScenario` (`Assets/Infos/Balance/OpenWorld.asset`), проверки —
`Assets/Tests/Editor/Balance/`, отчёт — `Tools/Balance/Generate Report`.

## 4.1 Принцип
Симулятор использует **тот же код**, что и игра (`LayerPlanner.Plan` → `LayerPopulator.Populate` (`Roll` угроз + `RollFood` → `PlacementRules.TryPlace`) → `RewardPlanner`), поэтому состав каждого слоя в отчёте побитно совпадает с тем,
что сгенерирует `OpenWorld` при том же seed. Моделируется только то, чего код не считает:
поведение игрока и ожидаемый обмен HP.

## 4.2 Входы
- Профиль врага `EnemyProfile` — извлекается из префаба (см. `02`).
- Сборка игрока `PlayerBuild` — префаб игрока + список улучшений, применённых так же, как в игре:
  `DashDps = dashDmg / dashDelay`, `HarpoonDps = harpoonDmg / shootDelay`,
  `Dps = DashDps + HarpoonDps + Spike · spikeDps`, `RangedMitigation = (1 − shieldBlock)^Shield`.
- Эталонные сборки: **Base** (ничего), **Passive** (ArrowUp + DashUp + JawUp),
  **Full** (Passive + 5 Spike + 5 Shield).

| Сборка | dashDmg | dashDelay | shootDelay | DashDps | HarpoonDps | Dps | Mitigation |
|---|---|---|---|---|---|---|---|
| Base | 1 | 3 | 1 | 0.33 | 1.0 | 1.33 | 1.0 |
| Passive | 2 | 1 | 0.2 | 2.0 | 5.0 | 7.0 | 1.0 |
| Full | 2 | 1 | 0.2 | 2.0 | 5.0 | 8.25 | 0.237 |

## 4.3 Метрики слоя (`LayerSimulator.SimulateLayer`)

| Метрика | Формула | Что показывает |
|---|---|---|
| `Budget` (`threatBudget`) | `B_i` (3.2) | сколько «угрозы» заказано |
| `FoodBudget` | `F_i` (3.2.1) | сколько «еды» заказано |
| `Rolled` / `Placed` | размер ролла угроз+еды / реально размещено (без наград) | — |
| `PlacementLoss` | `1 − Placed/Rolled` | насколько размещение, а не бюджет, задаёт плотность |
| `Spent` (`threatSpent`) | Σ threatCost размещённых записей `kind = Threat` | реальная «угроза» слоя |
| `FoodSpent` | Σ threatCost размещённых записей `kind = Food` | реальный объём еды |
| `KillableHp` | Σ HP убиваемых | объём работы |
| `TimeToClear` | `KillableHp / Dps` | секунды чистого боя на зачистку |
| `ExpectedDamage` | Σ_стрелки `(exposureRanged / shootDelay) · projDmg · hitProbRanged · Mitigation` + Σ_контакт `contactDmg · pContact` | ожидаемая потеря HP |
| `ExpectedHeal` | Σ_{еда, EatableByDash(dashDmg)} `healOnEat · eatShare` | ожидаемое лечение едой |
| `ExpectedLevelUpHp` | `Rewards · pPickHp` | лечение через выбор «+1 HP» |
| `Rewards`, `Hazards`, составы по ролям/именам | счётчики | структура слоя |

## 4.4 Забег (`LayerSimulator.SimulateRun`)
```
hp_0 = startHp (4)
hp_{i+1} = min(maxHp, hp_i − ExpectedDamage_i + ExpectedHeal_i + ExpectedLevelUpHp_i)
RunLength = первый i с hp ≤ 0 (или число слоёв, если не умер)
```
Это детерминированная модель средних значений (не Монте-Карло): она отвечает на вопрос
«в среднем хватает ли лечения, чтобы покрыть урон», а не «с какой вероятностью». Для
дисперсии нужна телеметрия (4.7).

## 4.5 Константы модели (в `BalanceScenario`, калибруются по аналитике)

| Константа | Значение | Смысл | Как калибровать |
|---|---|---|---|
| `exposureRanged` | 6 с | время игрока в радиусе одного стрелка | средняя длительность «Pear видит игрока» |
| `hitProbRanged` | 0.35 | доля попаданий снарядов Pear | попадания / выстрелы |
| `pContact` | 0.15 | вероятность словить урон от одной контактной ловушки за слой | касания / ловушки на слой |
| `eatShare` | 0.5 | доля съедобных, которых игрок реально добивает рывком | съедено / съедобных |
| `pPickHp` | 0.5 | вероятность выбора «+1 HP» | из `AddHpEvent` vs `AddNewImprovementEvent` |
| `spikeDps` | 0.25 | вклад одного Spike в DPS | урон шипов / время |
| `shieldBlock` | 0.25 | доля снарядов, снятая одним щитом | заблокировано / выпущено |

Пока константы не откалиброваны, **абсолютные** значения RunLength — ориентир; надёжны
**относительные** выводы (сборка A vs B, слой i vs j, до/после правки чисел).

## 4.6 Инварианты и цели

**Инварианты** (тесты падают — это ошибка данных/кода):

| Тест | Утверждение |
|---|---|
| `SpawnTableIntegrity` | definition/prefab/Stats есть; cost > 0; weight > 0; unlock ≤ full; retire = 0 ∨ retire > full |
| `DifficultyMonotonicAndBounded` | `d` не убывает; прирост ≤ maxDelta |
| `BudgetNeverExceeded` | `Spent ≤ Budget` **и** `FoodSpent ≤ FoodBudget` на всех 60 слоях |
| `Deterministic` | один seed → одинаковые слои; другой seed → другие |
| `PacingPriority` | при совпадении битов побеждает последний в массиве |
| `ImprovementsIntegrity` | prefab с `Improvement`; имя из `ImprovementsConst`; пассив ⇒ maxLevel 1 |
| `PlayerBuildDerivation` | производные сборок совпадают с числами префабов |

**Цели** (тесты предупреждают — это дизайнерское решение, фиксируется в `06`):

| Тест | Цель | Параметр сценария |
|---|---|---|
| `PlacementLossWithinTarget` | отбрасывается ≤ 10 % ролла на слоях 0…40 | `maxPlacementLoss` |
| `RewardCadence` | в любых 3 подряд слоях (со 2-го) ≥ 1 LevelUp | `rewardWindow`, `rewardCheckFromLayer` |
| `FirstRewardLayer` | первая награда не позже слоя 3 | — |
| `RunLengthTargets` | Base ∈ [8, 14], Full ≥ 25 | `runLengthBase*`, `runLengthFullMin` |
| `RoleConsistency` | роль в таблице согласуется с профилем (Ranged ⇔ стрелок, Reward ⇔ LevelUp, ловушка ∉ Tank/Swarm/Melee, Swarm ⇒ cost ≤ 2) | — |

Целевые числа длины забега (8–14 / ≥ 25) — **предложение**, требующее подтверждения;
они задают, что «средний забег без улучшений — ~10 слоёв ≈ 1000 ед. высоты».

## 4.6.1 Первый прогон (seed 1337, исходные числа)
Инварианты: 7/7 зелёные. Цели: 4 из 5 не достигнуты (статус *Inconclusive*):
`FirstRewardLayer` — 16; `RewardCadence` — окна 2…16 без наград; `PlacementLossWithinTarget` —
слои 37–40 (10–33 %); `RunLengthTargets` — все сборки живы на 60-м слое (урон ≤ 2.2/слой против
лечения 22–300/слой). Разбор и рекомендации — `06`, B1/B4/B13/B14, R0–R4.

## 4.6.2 После выноса наград и разделения бюджетов
Инварианты: 7/7 зелёные. `FirstRewardLayer` (слой 2), `RewardCadence`, `PlacementLossWithinTarget`
(loss = 0 на 60 слоях; было до 44 %) — зелёные. Не достигнуто только `RunLengthTargets`
(Base живёт все 60 слоёв: урон по-прежнему ≤ 2.2/слой, B13/R0).

## 4.7 Процедура проверки изменения баланса
1. Поменять число (префаб/asset/константу).
2. `Tools/Balance/Generate Report` → сравнить `layers.csv` с предыдущим (git diff).
3. Test Runner → EditMode → `Balance` — инварианты зелёные, предупреждения целей прочитаны.
4. Если цель нарушена осознанно — обновить цель в `BalanceScenario` и записать причину в `06`.
5. После релиза — сверить константы 4.5 с GameAnalytics (`GameAnalyticsBeh`): нужны события
   `Design:Layer:Enter(i, hp)`, `Design:Damage(source)`, `Design:Eat(enemy)`, `Design:Shot(hit)`.
   Их сейчас нет — см. `06`, рекомендация R7.
