# Отчёт по балансу

## Сценарий

- Seed: 1337
- Слоёв симулировано: 60
- Попыток размещения на врага: 10
- Награды (вне бюджета): LevelUp, firstRewardLayer = 2, rewardEveryLayers = 3, rewardsPerLayer = [1, 2]
- Константы модели:
  - exposureRanged = 6
  - hitProbRanged = 0.35
  - hitProbHoming = 0.5
  - pContact = 0.15
  - pChase = 0.4
  - eatShare = 0.5
  - pPickHp = 0.5
  - spikeDps = 0.25
  - shieldBlock = 0.25
- Цели баланса:
  - runLengthBase = [8, 14]
  - runLengthFull >= 25
  - maxPlacementLoss = 0.1
  - rewardWindow = 3, rewardCheckFromLayer = 2

## Сущности

| Имя | Вид | Роль | threatCost | weight | unlock | full | retire | maxPerLayer | Hp | Damageable | DiesFromDamage | ContactDmg | Ranged delay | Ranged range | ProjDmg | HealOnEat | IsLevelUp | IsHazard | EatableByDash(1) | EatableByDash(2) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Bee | Food | Swarm | 1 | 10 | 0 | 0.05 | 0.75 | 0 | 1 | да | да | 0 | - | - | - | 1 | нет | нет | да | да |
| Circle0 | Food | Tank | 2 | 6 | 0 | 0.1 | 0 | 0 | 4 | да | да | 0 | - | - | - | 1 | нет | нет | да | да |
| Capsule | Threat | Tank | 3 | 4 | 0.1 | 0.3 | 0 | 4 | 5 | да | да | 0 | - | - | - | 0 | нет | нет | нет | нет |
| Hex | Threat | Tank | 4 | 3 | 0.3 | 0.55 | 0 | 3 | 2 | нет | нет | 0 | - | - | - | 0 | нет | нет | нет | нет |
| HexLaser | Threat | Ambusher | 5 | 2.5 | 0.45 | 0.65 | 0 | 2 | 2 | нет | нет | 1 | - | - | - | 0 | нет | да | нет | нет |
| Pear | Threat | Ranged | 6 | 3 | 0.25 | 0.5 | 0 | 3 | 5 | да | да | 0 | 4 | 12 | 1 | 1 | нет | нет | да | да |
| Circle1 | Threat | Elite | 8 | 1.5 | 0.55 | 0.8 | 0 | 2 | 4 | нет | нет | 1 | - | - | - | 1 | нет | да | нет | нет |
| Chaser | Threat | Hunter | 4 | 3 | 0.2 | 0.45 | 0 | 4 | 2 | да | да | 1 | - | - | - | 1 | нет | нет | да | да |
| Charger | Threat | Ambusher | 6 | 2.5 | 0.35 | 0.6 | 0 | 3 | 3 | да | да | 2 | - | - | - | 1 | нет | нет | да | да |
| Spitter | Threat | Ranged | 7 | 2 | 0.4 | 0.7 | 0 | 2 | 3 | да | да | 0 | 5 | 14 | 1 | 1 | нет | нет | да | да |
| Mine | Threat | Ambusher | 4 | 3 | 0.3 | 0.55 | 0 | 4 | 1 | да | да | 2 | - | - | - | 0 | нет | нет | нет | нет |
| Leaper | Threat | Melee | 5 | 3 | 0.15 | 0.4 | 0 | 3 | 2 | да | да | 1 | - | - | - | 1 | нет | нет | да | да |
| Runner | Food | Swarm | 2 | 4 | 0.1 | 0.4 | 0 | 4 | 1 | да | да | 0 | - | - | - | 2 | нет | нет | да | да |

## Сборки игрока

| Сборка | DashDmg | DashDelay | HarpoonDmg | ShootDelay | Spike | Shield | DashDps | HarpoonDps | Dps | RangedMitigation |
|---|---|---|---|---|---|---|---|---|---|---|
| Base | 1 | 3 | 1 | 1 | 0 | 0 | 0.333 | 1 | 1.333 | 1 |
| Passive | 2 | 1 | 1 | 0.2 | 0 | 0 | 2 | 5 | 7 | 1 |
| Full | 2 | 1 | 1 | 0.2 | 5 | 5 | 2 | 5 | 8.25 | 0.237 |

## Слои

| i | d | pacing | threatBudget | foodBudget | rolled | placed | loss% | threatSpent | foodSpent | rewards | hazards | killableHp | состав | Base TTC | Base dmg | Base heal | Passive TTC | Passive dmg | Passive heal | Full TTC | Full dmg | Full heal |
|---|---|---|---|---|---|---|---|---|---|---|---|---||---|---|---|---|---|---|---|---|---|
| 0 | 0.03 | 1 | 115 | 62.7 | 54 | 54 | 0 | 0 | 62 | 0 | 0 | 78 | Bee×46, Circle0×8 | 58.5 | 0 | 27 | 11.143 | 0 | 27 | 9.455 | 0 | 27 |
| 1 | 0.039 | 1 | 119.486 | 63.507 | 54 | 54 | 0 | 0 | 63 | 0 | 0 | 81 | Bee×45, Circle0×9 | 60.75 | 0 | 27 | 11.571 | 0 | 27 | 9.818 | 0 | 27 |
| 2 | 0.051 | 1 | 125.438 | 64.579 | 57 | 57 | 0 | 0 | 64 | 1 | 0 | 78 | Bee×50, Circle0×7, LevelUp×1 | 58.5 | 0 | 28.5 | 11.143 | 0 | 28.5 | 9.455 | 0 | 28.5 |
| 3 | 0.064 | 0.45 | 59.376 | 65.75 | 49 | 49 | 0 | 0 | 65 | 0 | 0 | 97 | Circle0×16, Bee×33 | 72.75 | 0 | 24.5 | 13.857 | 0 | 24.5 | 11.758 | 0 | 24.5 |
| 4 | 0.076 | 1 | 138.099 | 66.858 | 49 | 49 | 0 | 0 | 66 | 0 | 0 | 100 | Bee×32, Circle0×17 | 75 | 0 | 24.5 | 14.286 | 0 | 24.5 | 12.121 | 0 | 24.5 |
| 5 | 0.106 | 1 | 152.987 | 69.538 | 56 | 56 | 0 | 12 | 69 | 2 | 0 | 123 | Capsule×4, Bee×35, Circle0×17, LevelUp×2 | 92.25 | 0 | 26 | 17.571 | 0 | 26 | 14.909 | 0 | 26 |
| 6 | 0.144 | 1 | 171.773 | 72.919 | 53 | 53 | 0 | 12 | 72 | 0 | 0 | 135 | Capsule×4, Circle0×22, Bee×26, Runner×1 | 101.25 | 0 | 25 | 19.286 | 0 | 25 | 16.364 | 0 | 25 |
| 7 | 0.181 | 0.45 | 85.632 | 76.253 | 63 | 63 | 0 | 27 | 76 | 0 | 0 | 130 | Capsule×4, Leaper×3, Bee×36, Circle0×16, Runner×4 | 97.5 | 1.2 | 31.5 | 18.571 | 1.2 | 31.5 | 15.758 | 1.2 | 31.5 |
| 8 | 0.22 | 1 | 209.889 | 79.78 | 70 | 70 | 0 | 43 | 79 | 2 | 0 | 141 | Capsule×4, Leaper×3, Chaser×4, Bee×39, Circle0×16, Runner×4, LevelUp×2 | 105.75 | 2.8 | 35 | 20.143 | 2.8 | 35 | 17.091 | 2.8 | 35 |
| 9 | 0.264 | 1.5 | 347.842 | 83.741 | 73 | 73 | 0 | 61 | 83 | 0 | 0 | 168 | Chaser×4, Capsule×4, Leaper×3, Pear×3, Circle0×20, Bee×35, Runner×4 | 126 | 4.375 | 36.5 | 24 | 4.375 | 36.5 | 20.364 | 3.174 | 36.5 |
| 10 | 0.314 | 1 | 256.895 | 88.241 | 84 | 84 | 0 | 89 | 88 | 0 | 0 | 179 | Capsule×4, Leaper×3, Chaser×4, Pear×3, Mine×4, Hex×3, Bee×38, Circle0×21, Runner×4 | 134.25 | 5.575 | 38.5 | 25.571 | 5.575 | 38.5 | 21.697 | 4.374 | 38.5 |
| 11 | 0.364 | 0.45 | 126.853 | 92.741 | 88 | 88 | 0 | 107 | 92 | 2 | 0 | 198 | Mine×4, Capsule×4, Pear×3, Leaper×3, Chaser×4, Hex×3, Charger×3, Circle0×24, Bee×36, Runner×4, LevelUp×2 | 148.5 | 6.475 | 40.5 | 28.286 | 6.475 | 40.5 | 24 | 5.274 | 40.5 |
| 12 | 0.414 | 1 | 306.895 | 97.241 | 95 | 95 | 0 | 121 | 97 | 0 | 0 | 209 | Capsule×4, Hex×3, Leaper×3, Charger×3, Chaser×4, Pear×3, Mine×4, Spitter×2, Bee×41, Circle0×24, Runner×4 | 156.75 | 7.675 | 44 | 29.857 | 7.675 | 44 | 25.333 | 5.559 | 44 |
| 13 | 0.464 | 1 | 331.895 | 101.741 | 98 | 98 | 0 | 131 | 101 | 0 | 2 | 219 | Chaser×4, Leaper×3, Mine×4, Pear×3, Capsule×4, Hex×3, Charger×3, HexLaser×2, Spitter×2, Circle0×27, Runner×4, Bee×39 | 164.25 | 7.975 | 44.5 | 31.286 | 7.975 | 44.5 | 26.545 | 5.859 | 44.5 |
| 14 | 0.514 | 1 | 356.895 | 106.241 | 103 | 103 | 0 | 131 | 106 | 1 | 2 | 224 | Pear×3, Mine×4, Spitter×2, Hex×3, Charger×3, Chaser×4, Leaper×3, HexLaser×2, Capsule×4, Circle0×27, Bee×44, Runner×4, LevelUp×1 | 168 | 7.975 | 47 | 32 | 7.975 | 47 | 27.152 | 5.859 | 47 |
| 15 | 0.564 | 0.45 | 171.853 | 110.741 | 108 | 108 | 0 | 147 | 110 | 0 | 4 | 230 | Pear×3, Capsule×4, Charger×3, Spitter×2, Mine×4, Leaper×3, Chaser×4, Hex×3, HexLaser×2, Circle1×2, Bee×46, Circle0×28, Runner×4 | 172.5 | 8.275 | 48.5 | 32.857 | 8.275 | 48.5 | 27.879 | 6.159 | 48.5 |
| 16 | 0.614 | 1 | 406.895 | 115.241 | 107 | 107 | 0 | 147 | 115 | 0 | 4 | 247 | Mine×4, HexLaser×2, Chaser×4, Pear×3, Capsule×4, Leaper×3, Hex×3, Charger×3, Spitter×2, Circle1×2, Bee×39, Circle0×34, Runner×4 | 185.25 | 8.275 | 48 | 35.286 | 8.275 | 48 | 29.939 | 6.159 | 48 |
| 17 | 0.664 | 1 | 431.895 | 119.741 | 124 | 124 | 0 | 147 | 119 | 2 | 4 | 225 | Capsule×4, Mine×4, Spitter×2, Hex×3, HexLaser×2, Circle1×2, Chaser×4, Pear×3, Leaper×3, Charger×3, Runner×4, Bee×69, Circle0×21, LevelUp×2 | 168.75 | 8.275 | 56.5 | 32.143 | 8.275 | 56.5 | 27.273 | 6.159 | 56.5 |
| 18 | 0.714 | 1 | 456.895 | 124.241 | 121 | 121 | 0 | 147 | 124 | 0 | 4 | 246 | Pear×3, Leaper×3, Hex×3, Chaser×4, HexLaser×2, Capsule×4, Spitter×2, Mine×4, Circle1×2, Charger×3, Circle0×29, Bee×58, Runner×4 | 184.5 | 8.275 | 55 | 35.143 | 8.275 | 55 | 29.818 | 6.159 | 55 |
| 19 | 0.764 | 1.5 | 722.842 | 128.741 | 94 | 94 | 0 | 147 | 128 | 0 | 4 | 312 | Capsule×4, Hex×3, Spitter×2, Charger×3, Leaper×3, Circle1×2, Pear×3, HexLaser×2, Mine×4, Chaser×4, Circle0×60, Runner×4 | 234 | 8.275 | 41.5 | 44.571 | 8.275 | 41.5 | 37.818 | 6.159 | 41.5 |
| 20 | 0.814 | 1 | 506.895 | 133.241 | 96 | 96 | 0 | 147 | 132 | 1 | 4 | 320 | Spitter×2, Charger×3, Mine×4, Hex×3, Capsule×4, Pear×3, Leaper×3, Chaser×4, Circle1×2, HexLaser×2, Runner×4, Circle0×62, LevelUp×1 | 240 | 8.275 | 42.5 | 45.714 | 8.275 | 42.5 | 38.788 | 6.159 | 42.5 |
| 21 | 0.85 | 1 | 525.175 | 136.532 | 98 | 98 | 0 | 147 | 136 | 0 | 4 | 328 | Spitter×2, Capsule×4, Leaper×3, HexLaser×2, Charger×3, Pear×3, Chaser×4, Mine×4, Hex×3, Circle1×2, Circle0×64, Runner×4 | 246 | 8.275 | 43.5 | 46.857 | 8.275 | 43.5 | 39.758 | 6.159 | 43.5 |
| 22 | 0.864 | 1 | 531.975 | 137.756 | 98 | 98 | 0 | 147 | 136 | 0 | 4 | 328 | Circle1×2, HexLaser×2, Pear×3, Mine×4, Capsule×4, Leaper×3, Charger×3, Hex×3, Chaser×4, Spitter×2, Runner×4, Circle0×64 | 246 | 8.275 | 43.5 | 46.857 | 8.275 | 43.5 | 39.758 | 6.159 | 43.5 |
| 23 | 0.877 | 0.45 | 242.384 | 138.954 | 99 | 99 | 0 | 147 | 138 | 2 | 4 | 332 | Hex×3, Mine×4, Charger×3, HexLaser×2, Capsule×4, Leaper×3, Pear×3, Spitter×2, Chaser×4, Circle1×2, Runner×4, Circle0×65, LevelUp×2 | 249 | 8.275 | 44 | 47.429 | 8.275 | 44 | 40.242 | 6.159 | 44 |
| 24 | 0.884 | 1 | 542.203 | 139.597 | 99 | 99 | 0 | 147 | 138 | 0 | 4 | 332 | Pear×3, Leaper×3, Capsule×4, Hex×3, Chaser×4, HexLaser×2, Charger×3, Spitter×2, Mine×4, Circle1×2, Runner×4, Circle0×65 | 249 | 8.275 | 44 | 47.429 | 8.275 | 44 | 40.242 | 6.159 | 44 |
| 25 | 0.89 | 1 | 545.181 | 140.133 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Mine×4, HexLaser×2, Circle1×2, Chaser×4, Hex×3, Spitter×2, Pear×3, Leaper×3, Capsule×4, Charger×3, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 26 | 0.896 | 1 | 548.142 | 140.666 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Pear×3, Spitter×2, Mine×4, Charger×3, Capsule×4, Leaper×3, Chaser×4, Hex×3, HexLaser×2, Circle1×2, Runner×4, Circle0×66, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 27 | 0.9 | 0.45 | 247.5 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Leaper×3, Mine×4, Chaser×4, Hex×3, Pear×3, Capsule×4, Charger×3, Circle1×2, HexLaser×2, Spitter×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 28 | 0.9 | 1 | 550 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Spitter×2, Chaser×4, Hex×3, Circle1×2, Pear×3, Mine×4, Leaper×3, Capsule×4, Charger×3, HexLaser×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 29 | 0.9 | 1.5 | 825 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | HexLaser×2, Chaser×4, Charger×3, Capsule×4, Leaper×3, Hex×3, Mine×4, Spitter×2, Pear×3, Circle1×2, Circle0×66, Runner×4, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 30 | 0.9 | 1 | 740.615 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Capsule×4, Hex×3, Spitter×2, Mine×4, Charger×3, HexLaser×2, Pear×3, Chaser×4, Leaper×3, Circle1×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 31 | 0.9 | 0.45 | 383.453 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Pear×3, Circle1×2, Hex×3, HexLaser×2, Charger×3, Spitter×2, Capsule×4, Chaser×4, Leaper×3, Mine×4, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 32 | 0.9 | 1 | 931.231 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Hex×3, Capsule×4, Chaser×4, HexLaser×2, Leaper×3, Mine×4, Pear×3, Charger×3, Spitter×2, Circle1×2, Circle0×66, Runner×4, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 33 | 0.9 | 1 | 992.595 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Charger×3, Mine×4, Leaper×3, Pear×3, Spitter×2, HexLaser×2, Capsule×4, Chaser×4, Hex×3, Circle1×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 34 | 0.9 | 1 | 1042.734 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Circle1×2, Pear×3, HexLaser×2, Leaper×3, Hex×3, Mine×4, Spitter×2, Chaser×4, Capsule×4, Charger×3, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 35 | 0.9 | 0.45 | 488.306 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Charger×3, Mine×4, Pear×3, Hex×3, Capsule×4, Spitter×2, HexLaser×2, Leaper×3, Chaser×4, Circle1×2, Circle0×66, Runner×4, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 36 | 0.9 | 1 | 1121.846 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Capsule×4, Charger×3, HexLaser×2, Mine×4, Spitter×2, Leaper×3, Hex×3, Chaser×4, Pear×3, Circle1×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 37 | 0.9 | 1 | 1154.237 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Spitter×2, Mine×4, Leaper×3, Circle1×2, Charger×3, Chaser×4, Pear×3, Capsule×4, Hex×3, HexLaser×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 38 | 0.9 | 1 | 1183.211 | 141 | 100 | 100 | 0 | 147 | 140 | 1 | 4 | 336 | Capsule×4, Hex×3, Chaser×4, Charger×3, Mine×4, Leaper×3, Spitter×2, Pear×3, HexLaser×2, Circle1×2, Circle0×66, Runner×4, LevelUp×1 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 39 | 0.9 | 1.5 | 1814.132 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | HexLaser×2, Mine×4, Leaper×3, Circle1×2, Chaser×4, Hex×3, Capsule×4, Pear×3, Charger×3, Spitter×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 40 | 0.9 | 1 | 1233.349 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Charger×3, Pear×3, Hex×3, Leaper×3, HexLaser×2, Chaser×4, Circle1×2, Mine×4, Capsule×4, Spitter×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 41 | 0.9 | 1 | 1255.361 | 141 | 100 | 100 | 0 | 147 | 140 | 1 | 4 | 336 | Spitter×2, Leaper×3, Circle1×2, Chaser×4, Pear×3, HexLaser×2, Mine×4, Hex×3, Charger×3, Capsule×4, Runner×4, Circle0×66, LevelUp×1 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 42 | 0.9 | 1 | 1275.741 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Spitter×2, Pear×3, Hex×3, Chaser×4, Leaper×3, Capsule×4, Charger×3, Mine×4, Circle1×2, HexLaser×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 43 | 0.9 | 0.45 | 582.621 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Circle1×2, HexLaser×2, Spitter×2, Leaper×3, Mine×4, Pear×3, Hex×3, Charger×3, Capsule×4, Chaser×4, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 44 | 0.9 | 1 | 1312.462 | 141 | 100 | 100 | 0 | 147 | 140 | 1 | 4 | 336 | Spitter×2, Charger×3, Pear×3, Hex×3, Mine×4, Circle1×2, HexLaser×2, Chaser×4, Capsule×4, Leaper×3, Circle0×66, Runner×4, LevelUp×1 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 45 | 0.9 | 1 | 1329.134 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Pear×3, Charger×3, Spitter×2, Capsule×4, HexLaser×2, Hex×3, Leaper×3, Chaser×4, Mine×4, Circle1×2, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 46 | 0.9 | 1 | 1344.852 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Charger×3, Mine×4, Leaper×3, Circle1×2, Hex×3, HexLaser×2, Spitter×2, Chaser×4, Pear×3, Capsule×4, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 47 | 0.9 | 0.45 | 611.874 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Capsule×4, Pear×3, Circle1×2, HexLaser×2, Chaser×4, Hex×3, Mine×4, Spitter×2, Charger×3, Leaper×3, Runner×4, Circle0×66, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 48 | 0.9 | 1 | 1373.826 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Hex×3, Spitter×2, Chaser×4, Circle1×2, Charger×3, Pear×3, HexLaser×2, Capsule×4, Leaper×3, Mine×4, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 49 | 0.9 | 1.5 | 2080.865 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | HexLaser×2, Pear×3, Spitter×2, Mine×4, Hex×3, Circle1×2, Chaser×4, Capsule×4, Charger×3, Leaper×3, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 50 | 0.9 | 1 | 1400.037 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Hex×3, Circle1×2, Mine×4, HexLaser×2, Leaper×3, Chaser×4, Pear×3, Capsule×4, Charger×3, Spitter×2, Circle0×66, Runner×4, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 51 | 0.9 | 0.45 | 635.517 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Spitter×2, Leaper×3, Hex×3, Chaser×4, Mine×4, HexLaser×2, Capsule×4, Pear×3, Charger×3, Circle1×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 52 | 0.9 | 1 | 1423.965 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | HexLaser×2, Pear×3, Capsule×4, Chaser×4, Leaper×3, Circle1×2, Charger×3, Mine×4, Spitter×2, Hex×3, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 53 | 0.9 | 1 | 1435.191 | 141 | 100 | 100 | 0 | 147 | 140 | 1 | 4 | 336 | Pear×3, Spitter×2, Hex×3, HexLaser×2, Leaper×3, Charger×3, Circle1×2, Mine×4, Capsule×4, Chaser×4, Circle0×66, Runner×4, LevelUp×1 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 54 | 0.9 | 1 | 1445.976 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Pear×3, Hex×3, Mine×4, Chaser×4, Spitter×2, Charger×3, HexLaser×2, Capsule×4, Leaper×3, Circle1×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 55 | 0.9 | 0.45 | 655.36 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | HexLaser×2, Circle1×2, Spitter×2, Capsule×4, Charger×3, Mine×4, Pear×3, Chaser×4, Leaper×3, Hex×3, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 56 | 0.9 | 1 | 1466.356 | 141 | 100 | 100 | 0 | 147 | 140 | 2 | 4 | 336 | Spitter×2, Charger×3, Hex×3, Capsule×4, Mine×4, Pear×3, Chaser×4, Circle1×2, HexLaser×2, Leaper×3, Runner×4, Circle0×66, LevelUp×2 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 57 | 0.9 | 1 | 1476.006 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Mine×4, HexLaser×2, Pear×3, Leaper×3, Capsule×4, Chaser×4, Charger×3, Spitter×2, Hex×3, Circle1×2, Runner×4, Circle0×66 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 58 | 0.9 | 1 | 1485.329 | 141 | 100 | 100 | 0 | 147 | 140 | 0 | 4 | 336 | Mine×4, Chaser×4, Leaper×3, Pear×3, Spitter×2, Hex×3, HexLaser×2, Capsule×4, Circle1×2, Charger×3, Circle0×66, Runner×4 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |
| 59 | 0.9 | 1.5 | 2241.52 | 141 | 100 | 100 | 0 | 147 | 140 | 1 | 4 | 336 | Chaser×4, Leaper×3, Capsule×4, Hex×3, Pear×3, Mine×4, HexLaser×2, Spitter×2, Circle1×2, Charger×3, Runner×4, Circle0×66, LevelUp×1 | 252 | 8.275 | 44.5 | 48 | 8.275 | 44.5 | 40.727 | 6.159 | 44.5 |

## Забег

| Сборка | RunLength | Died |
|---|---|---|
| Base | 60 | нет |
| Passive | 60 | нет |
| Full | 60 | нет |

### HP-трасса (первые 20 слоёв)

| Слой | Base | Passive | Full |
|---|---|---|---|
| 0 | 7 | 7 | 7 |
| 1 | 7 | 7 | 7 |
| 2 | 7 | 7 | 7 |
| 3 | 7 | 7 | 7 |
| 4 | 7 | 7 | 7 |
| 5 | 7 | 7 | 7 |
| 6 | 7 | 7 | 7 |
| 7 | 7 | 7 | 7 |
| 8 | 7 | 7 | 7 |
| 9 | 7 | 7 | 7 |
| 10 | 7 | 7 | 7 |
| 11 | 7 | 7 | 7 |
| 12 | 7 | 7 | 7 |
| 13 | 7 | 7 | 7 |
| 14 | 7 | 7 | 7 |
| 15 | 7 | 7 | 7 |
| 16 | 7 | 7 | 7 |
| 17 | 7 | 7 | 7 |
| 18 | 7 | 7 | 7 |
| 19 | 7 | 7 | 7 |

## Находки

### Слои с потерями размещения выше цели
- Нет.

### Окна без наград
- Нет.

### Сборки вне целевого коридора
- Base: RunLength 60 вне [8, 14]

### Несоответствия роли и профиля
- Нет.

