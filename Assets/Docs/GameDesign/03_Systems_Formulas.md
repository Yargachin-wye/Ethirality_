# 03. Системы и формулы

Все формулы — то, что **реально делает код**; ссылки указывают на место реализации.
Обозначения: `i` — индекс слоя (0…), `d` — сложность ∈ [0, 1], `B` — бюджет угроз слоя, `F` — бюджет еды слоя.

## 3.1 Сложность слоя — `DifficultyProfile.EvaluateDifficulty`

```
t_i   = i / (layerCount − 1)                       layerCount = 30
raw_i = clamp01(curve(t_i))                        curve: (0, .03) (.15, .08) (.4, .42) (.6, .78) (.8, .88) (.92, .9) (1, .9)
d_0   = raw_0
d_i   = min(raw_i, d_{i−1} + maxDelta)             maxDelta = 0.05   («pressure budget»)
d_i   = d_{layerCount−1}  при i ≥ layerCount
```
Свойства: `d` монотонна (кривая монотонна), прирост ≤ 0.05/слой, плато 0.9 с ~27-го слоя.

## 3.2 Бюджет угроз — `DifficultyProfile.EvaluateThreat`

```
area       = width · height                         LayerDefinition: 50 × 100 = 5000
threatArea = lerp(minTPA, maxTPA, d)                0.02 … 0.12
pacing_i   = beat.threatMultiplier | 1              см. 3.3
layerMult  = LayerDefinition.threatMultiplier (1) · endless_i
endless_i  = 1 + k · ln(1 + max(0, i − layerCount + 1))     k = 0.5
B_i        = threatArea · area · pacing_i · layerMult
```
Для текущих чисел: `B = (100 + 500·d) · pacing · endless` → 115 на слое 0, 550 на плато,
~900 на слое 59 (endless ≈ 1.68).

### 3.2.1 Бюджет еды — `DifficultyProfile.EvaluateFood`
Записи `EnemySpawnTable` имеют вид `kind ∈ {Threat, Food}`. Еда (безвредные съедобные: Bee, Circle0)
**не тратит** бюджет угроз и катится из собственного бюджета:
```
F_i = min( lerp(minFoodPerArea, maxFoodPerArea, d_i) · area , maxFoodBudget )
      minFoodPerArea = 0.012, maxFoodPerArea = 0.03, maxFoodBudget = 150 (0 = без потолка)
```
Без pacing- и endless-множителей: еда не растёт со временем сверх потолка. При 50×100:
`F` = 60 на слое 0, 141 на плато (потолок 150 не достигается при d ≤ 0.9, но защищает от
изменений площади/кривой). Потолок выбран так, чтобы `PlacementLoss ≤ 10 %` на слоях 0–40.
Награды (`LevelUp`) — вне обоих бюджетов, см. `RewardPlanner`.

## 3.3 Pacing-биты — `DifficultyProfile.TryGetBeat`

```
бит подходит, если i % every == offset % every
проверка идёт с КОНЦА массива → при совпадении нескольких побеждает последний
```
Текущие: `{every 4, offset 3, ×0.45}` → передышка на 3, 7, 11, 15, 23, 27…;
`{every 10, offset 9, ×1.5}` → пик на 9, 19, 29, 39… (19, 39, 59 совпадают с обоими → пик).
Оба бита принудительно ставят тот же `LayerDefinition`, так что влияют только на бюджет.

## 3.4 Выбор определения слоя — `LayerPlanner.PickDefinition`
Взвешенный выбор среди `layerDefinitions` с `minDifficulty ≤ d ≤ maxDifficulty`. Сейчас
определение одно → детерминировано. Ширина/высота — равномерно из диапазонов (сейчас константы).

## 3.5 Ролл состава — `EnemySpawnTable.Roll` / `RollFood`

Ролл выполняется **дважды** за слой (`LayerPopulator.Populate`): сначала угрозы
(`Roll`, бюджет `B`, только `kind = Threat`, учитывает `composition`), затем еда
(`RollFood`, бюджет `F`, только `kind = Food`). Алгоритм ниже одинаков для обоих.

Вес записи при сложности `d`:
```
w(e, d) = 0,                                   d < unlock  ∨  (retire > 0 ∧ d > retire)
        = weight · smoothstep(unlock, full, d) иначе  (= weight, если full ≤ unlock)
```
Алгоритм (без `composition`, как сейчас):
```
пока бюджет > 0:
  кандидаты = { e : threatCost ≤ бюджет, count[e] < maxPerLayer (0 = ∞), w(e,d) > 0 }
  если пусто → стоп
  e ~ кандидаты с вероятностью w/Σw ;  бюджет −= threatCost ;  count[e]++
```
Следствия:
- Ролл **всегда** тратит бюджет почти до нуля. Угрозы упираются в `maxPerLayer` (суммарно
  ≤ 68 ед. стоимости при всех капах), остаток `B` не тратится. Еда безлимитна по счёту
  (Bee до d=0.75, затем Circle Shell), поэтому её объём задаётся только `F`: ≈ 55–100 объектов
  за слой вместо ~250.
- Плотность больше не упирается в размещение: `PlacementLoss` = 0 на всех 60 слоях (3.6).
- С `composition` (доли по ролям) бюджет делится на квоты; исчерпанная квота
  перераспределяется по долям остальных. В OpenWorld composition пуст.

## 3.6 Размещение — `PlacementRules` + `LayerPopulator`

Сначала размещаются все угрозы, затем еда (угрозы имеют приоритет на места), затем награды.
Для каждого объекта из ролла до `attemptsPerEnemy = 10` попыток случайной точки:
```
принимается, если:
  внутри границ с отступом edgeMargin (2)
  y ≥ yMin + safeZoneHeight (6)                       — безопасный вход
  расстояние до всех размещённых ≥ minSpacing (2)
  для дальнобойных (range ≥ rangedMinRange 2.5):
     dist(точка, вход) ≥ range + rangedEntryClearance (1.5)
     число уже стоящих стрелков, чьи круги пересекаются с этим, < maxCoveringRanged (2)
range(def) = EnemyShooter.detectionRange | экстент коллайдера для TriggerDamage | 0
```
Не размещённые враги **молча отбрасываются** (бюджет на них потрачен). Это главный
скрытый регулятор плотности — модель измеряет его как `PlacementLoss` (см. `04`).

## 3.7 Детерминизм — `LayerPlanner.LayerSeed`
```
seed_i = (seed · 73856093) XOR (i · 19349663) XOR 0x5bd1e995     (unchecked int)
```
Порядок потребления случайных чисел на слой: width → height → ролл угроз → размещение угроз →
ролл еды → размещение еды → награды (`LayerPopulator`, затем `RewardPlanner`).
Слой полностью определяется `(seed, i)`, что и позволяет симулировать его без сцены.

## 3.8 Урон и смерть — `Stats`
```
Damage(v): если isInvulnerable → ничего
           hp −= v ; если hp ≤ 0 ∧ !isImmortal → Dead()
Cure(v):   hp = min(hp + v, maxHp)
игрок: hp зеркалится в SaveSystem при каждом изменении
```

## 3.9 Рывок и съедение — `LumpMeatMovable`
```
Dash: если кулдаун > 0 → нет;  кулдаун = dashDelay ; окно = dashTime ; импульс powerDash вдоль взгляда
во время окна при касании объекта со Stats:
  если у него есть BaseFood и hp − dashDmg ≤ 1 → Eat() (эффект еды) и Dead()  ← игнорирует immortal/invulnerable
  иначе                                       → Damage(dashDmg)
```
Ключевое правило экономики: **лечение даёт только добивание рывком**. Гарпун (и Spike)
убивают без еды. Со `JawUp` (D=2) всё с HP ≤ 3 съедается с первого касания.

## 3.10 Гарпун — `Shooter` + `HarpoonProjectile`
```
Shoot: кулдаун ≤ 0 → в очередь ; поворот к цели (rotationSpeed) ; выстрел при угле < 0.5°
снаряд: импульс speed·speedMultiply (10) ; жизнь lifeTime (2 с) ; при попадании во врага:
   FixedJoint2D к цели ; через attachedForceDelay (0.25 с):
      Damage(1)
      игрок:  v = 0 ; импульс |ownerForce| = 50 к цели      (Δv = 25 при массе 2)
      цель:   импульс |targetForce| = 20 к игроку
   → BreakAway (верёвка гаснет со скоростью ropeDecaySpeed)
снаряд гаснет при смерти цели, по истечении жизни или дальше maxDistanceToProjectile (25)
```
Гарпун — главный **двигатель вверх** (целиться в врага выше) и 1 DPS.

## 3.11 Дальний враг — `EnemyShooter`
```
каждые shootDelay: ближайшая цель чужой фракции в радиусе detectionRange → снаряд speed·speedMultiply по направлению к ней
```
Pear: 1 урон / 4 с, радиус 12, скорость снаряда 2 (медленный, уклоняемый).

## 3.12 Улучшения — `ImprovementsComponent`, `LevelUpPanel`
```
LevelUp съеден → пауза → 3 слота: [+1 HP] [конкретное улучшение X] [случайное]
доступные = { imp : count(imp в сохранении) < imp.maxLevel }
X ~ равномерно(доступные) ; «случайное» — ещё один равномерный ролл при подтверждении
применение: Instantiate(prefab) ; пассивы задают абсолютные значения (3 шт, maxLevel 1)
активные (Shield/Spike) — орбиты по 4/6/8… на кольцо радиуса 3·(k+1), напр. 30°/с с чередованием направления
```

## 3.13 Прогрессия между сценами — `GameplayManager`, `ChoosingNextLevelPanel`
`currentDifficulty` индексирует `ResManager.DifficultyLevelPacks` (наборы сцен). OpenWorld
есть в каждом пакете; его внутренняя сложность от `currentDifficulty` **не зависит** —
две шкалы сложности не связаны (см. `06`).

## 3.14 Границы — `DeadZone`
Вне триггера зоны: 1 урон каждые `dmgDelay` (1 с). Слои центрированы по x, ширина 50.
