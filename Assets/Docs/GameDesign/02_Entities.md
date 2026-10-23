# 02. Каталог сущностей

Числа ниже — срез префабов и ассетов на момент написания. **Авторитетный актуальный
срез — таблица «Сущности» в `../Balance/BalanceReport.md`** (генерируется из префабов).
Если документ и отчёт расходятся — прав отчёт, документ надо обновить.

## Системные роли (классификация модели)

Каждая сущность описывается набором булевых признаков, выводимых из компонентов префаба
(`Balance.EnemyProfile`):

| Признак | Правило | Смысл |
|---|---|---|
| `Damageable` | `!Stats.isInvulnerable` | HP уменьшается от урона |
| `DiesFromDamage` | `!Stats.isImmortal` | достигнув 0 HP, умирает от `Damage()` |
| `KillableByDamage` | оба выше | можно убить гарпуном/рывком |
| `IsThreat` | есть `TriggerDamage` по игроку или `EnemyShooter` | наносит урон |
| `IsHazard` | `IsThreat && !KillableByDamage` | неубиваемая ловушка |
| `IsEdible` | есть `BaseFood` | даёт эффект при съедении |
| `EatableByDash(D)` | `IsEdible && (Hp − D ≤ 1 ∨ Damageable)` | можно съесть рывком с уроном D (см. `03`, «Съедение») |

Отсюда пять архетипов: **Еда** (edible, не threat), **Губка** (killable, не edible, не threat),
**Угроза-еда** (threat + edible), **Ловушка** (hazard), **Награда** (`LevelUpFood`).

## Игрок (`Prefabs/Characters/Player/Player.prefab`)

| Параметр | Значение | Компонент |
|---|---|---|
| startHp / maxHp | 4 / 7 | `Stats` (старт реально берётся из `SaveSystem.defaultSaveData.hp = 4`) |
| Масса / drag / gravity | 2 / 0.5 / 1 | `Rigidbody2D` |
| dashDmg / powerDash / dashTime / dashDelay | 1 / 50 (Δv = 25) / 0.7 с / 3 с | `LumpMeatMovable` |
| decelerationRate (freeze) | 0.95 | `LumpMeatMovable` |
| Гарпун: снаряд / speed / shootDelay / maxDistance / ropeDecay | `ArrowProjectile` / 1 / 1 с / 25 / 0.5 | `Shooter` |
| Орбита улучшений: radius / rotationSpeed | 3 / 30°/с (слои: 4, 6, 8… штук, радиус ×(слой+1)) | `ImprovementsComponent` |

Производные (сборка без улучшений): `DashDps = 1/3 ≈ 0.33`, `HarpoonDps = 1/1 = 1.0`.

## Враги и объекты (таблица спавна OpenWorld)

Роли enum `EnemyRole`: 0 Melee, 1 Ranged, 2 Tank, 3 Swarm, 4 Elite, 5 Support, 6 Ambusher, 7 Reward, 8 Hunter.

| Definition → префаб | Роль в таблице | cost | weight | unlock→full / retire | max/слой | HP | Inv/Imm | Урон | Еда | Архетип |
|---|---|---|---|---|---|---|---|---|---|---|
| Bee → Bee | Swarm | 1 | 10 | 0→0.05 / **0.75** | ∞ | 1 | –/– | нет | +1 | **Еда** (блуждает 2–10 ед/с) |
| Circle0 → Circle Shell | Tank | 2 | 6 | 0→0.1 | ∞ | 4 | –/– | нет | +1 | **Еда** (толстая) |
| Capsule → Capsule | Tank | 3 | 4 | 0.1→0.3 | 4 | 5 | –/– | нет | нет | **Губка** |
| Hex → Hexagon | Tank | 4 | 3 | 0.3→0.55 | 3 | 2 | ✓/✓ | нет | нет | **Стена** (неубиваемая, безвредная) |
| HexLaser → HexagonLaser | Ambusher | 5 | 2.5 | 0.45→0.65 | 2 | 2 | ✓/✓ | контакт 1, исчезает после удара | нет | **Ловушка-одноразовая** |
| Pear → Pear | Ranged | 6 | 3 | 0.25→0.5 | 3 | 5 | –/– | снаряд 1 каждые 4 с, радиус 12, скорость 2 | +1 | **Угроза-еда** |
| Circle1 → Circle Laser | Elite | 8 | 1.5 | 0.55→0.8 | 2 | 4 | ✓/✓ | контакт 1, постоянный | +1 (недостижимо) | **Ловушка** |
| LevelUp → LevelUp | Reward | 4 | 1 | 0.55→0.8 | 2 | 1 | –/✓ | нет | LevelUp | **Награда** |
| Chaser → Chaser | Hunter | 4 | 3 | 0.2→0.45 | 4 | 2 | –/– | контакт 1 (после касания отходит ~1 с) | +1 | **Угроза-еда**: преследует в радиусе 8, скорость 4 (рывок игрока ≈ 25) |
| Charger → Charger | Ambusher | 6 | 2.5 | 0.35→0.6 | 3 | 3 | –/– | контакт 2 | +1 | **Угроза-еда**: телеграф 0.7 с → рывок 14 ед/с, 0.6 с → перезарядка 3 с; прицел фиксируется на половине телеграфа |
| Spitter → Spitter | Ranged | 7 | 2 | 0.4→0.7 | 2 | 3 | –/– | самонаводящийся снаряд 1 каждые 5 с, радиус 14, скорость 1.5, поворот 45°/с | +1 | **Угроза-еда** (вариант Pear) |
| Mine → Mine | Ambusher | 4 | 3 | 0.3→0.55 | 4 | 1 | –/– | взрыв 2 в радиусе 2.5 через 1 с после входа игрока в радиус 3 | нет | **Мина**: убивается до взрыва (гарпун/рывок) |
| Leaper → Leaper | Melee | 5 | 3 | 0.15→0.4 | 3 | 2 | –/– | контакт 1 | +1 | **Угроза-еда**: прыжки к игроку (радиус 9, присед 0.4 с, прыжок 10 ед/с × 0.35 с, пауза 1.2 с) |
| Runner → Runner | Swarm (**Food**) | 2 | 4 | 0.1→0.4 | 4 | 1 | –/– | нет | +2 | **Еда**: убегает от игрока (радиус 6, скорость 6) |

Вне таблицы: `GameOverKey` (выход в ручных сценах), `Circle2` (дубль Circle Laser).

Наблюдения, важные для баланса:
- **Источники урона в OpenWorld**: Pear и Spitter (дальние), Circle Laser, HexagonLaser, Chaser, Charger, Leaper
  (контакт), Mine (взрыв), `DeadZone`. Остальное — HP-губка или еда.
- Преследователи (Chaser, Leaper) в `ExpectedDamage` считаются через `pChase`, снаряды с самонаведением — через
  `hitProbHoming`. Chaser/Charger/Leaper используют `TriggerDamage.ignoreDashingTarget`: игрок в рывке урона не получает.
- Bee уходит из пула при d > 0.75 — вместе с ним уходит самый дешёвый источник лечения.
- LevelUp появляется только с d ≥ 0.55 → до этого улучшений нет вовсе.

## Снаряды (`Infos/Projectiles`)

| Снаряд | Кто | Урон | Скорость (×mult) | Жизнь | Поведение при попадании |
|---|---|---|---|---|---|
| ArrowProjectile | игрок | 1 | 1×10 | 2 с | прилипает (`FixedJoint2D`), через 0.25 с наносит урон и тянет: игрока к цели импульсом 50, цель к игроку 20 |
| ArrowMiniProjectile | – | 1 | ×10 | 2 с | прилипает без сил (не используется в таблице) |
| PearProjectile | Pear | 1 | 1×2 | 15 с | разрушается при попадании |
| SpitterProjectile | Spitter | 1 | 1×1.5 | 10 с | самонаводится (`EnemyShooter.homingTurnRate`), разрушается при попадании |

## Улучшения (`Infos/Improvements` + `Prefabs/Improvements`)

| Имя | Тип | maxLevel | Эффект (как применяется в коде) | Числа |
|---|---|---|---|---|
| ArrowUp | пассив | 1 | `Shooter.SetSpeed(shotSpeed)` → `shootDelay = 0.2` | DPS гарпуна 1.0 → **5.0** |
| DashUp | пассив | 1 | `LumpMeatMovable.SetDashDelay(1)` | кулдаун 3 → 1 с, DPS рывка 0.33 → 1.0 |
| JawUp | пассив | 1 | `SetDmg(2)` | урон рывка 1 → 2; съедается с 1 удара всё с HP ≤ 3 |
| Shield | актив | 10 | орбитальный щит, глотает вражеские снаряды; `numberTouches 1`, но `isDestroyOnTrigger false` → **вечный** |
| Spike | актив | 10 | шип на поводке (Leash, maxDistance 2), `TouchDamage 1`, 1000 касаний → практически вечный |

Пассивные применяют значение **абсолютно**, а не множителем: повторное получение (невозможно
при maxLevel 1) ничего не изменило бы. Выбор «случайное» равновероятен среди всех доступных.

## Три ресурса объекта (threat / food / mobility)

Вместо единого `threatCost` каждая запись `EnemySpawnTable.Entry` описана тремя явными ресурсами:

| Поле | Смысл | Значение по умолчанию (из профиля префаба, `EntityResourceModel.Derive`) |
|---|---|---|
| `threat` | опасность для игрока | `threatPerExpectedDamage · ExpectedDamage(Base) + threatPerEffort · KillableHp / Dps(Base)`; 0, если не `IsThreat` |
| `food` | HP при съедании | `HealOnEat`, если `EatableByDash(dashDmg Base)`; LevelUp -> `pPickHp` |
| `mobility` | высота подъёма от опоры, ед. | `referenceMaxGap` для стационарных объектов со `Stats`; 0 для подвижных и для пула Food |

Итог = выведенное значение × ручной множитель (`threatScale/foodScale/mobilityScale`, 0 читается как 1). Значения запечены в ассет
(`Tools/Balance/Migrate Spawn Tables`, идемпотентно); тест `ResourceValuesMatchProfile` ловит расхождение с префабом.
Старое поле `threatCost` оставлено для обратной совместимости: пока запись не мигрирована, оно трактуется как threat/food по `kind`.

`kind` = пул, из бюджета которого платит объект: `Threat`, `Food`, `Mobility` (новый, для безвредных опор: Capsule, Hex).
`mobilityKind`: `HarpoonAnchor` (стационарный объект со Stats: Capsule, Pear, Spitter, Mine, Circle1, HexLaser),
`SolidPlatform` (неуязвимый и безвредный: Hex), `EatDash` (съедобный подвижный: Bee, Runner, Chaser, Leaper, Charger; в коде съедение
не обновляет рывок, поэтому mobility = 0, множитель `eatDashMobilityShare`), `DashPushoff` (зарезервирован), `None`.

Миграция существующих записей (threat сохранён множителем, food = HP):

| Объект | Пул | threat (profile × scale) | food | mobility | mobilityKind |
|---|---|---|---|---|---|
| Bee, Circle0 | Food | 0 | 1 | 0 | EatDash |
| Capsule | Mobility (был Threat 3) | 0 | 0 | 17.5 | HarpoonAnchor |
| Hex | Mobility (был Threat 4) | 0 | 0 | 17.5 | SolidPlatform |
| HexLaser | Threat | 5 (1.5 × 3.33) | 0 | 17.5 | HarpoonAnchor |
| Pear | Threat | 6 (9 × 0.67) | 1 | 17.5 | HarpoonAnchor |
| Circle1 | Threat | 8 (1.5 × 5.33) | 0 | 17.5 | HarpoonAnchor |
| LevelUp | вне таблицы (награда) | 0 | `pPickHp` = 0.5 | 0 | - |

Chaser, Charger, Spitter, Mine, Leaper, Runner мигрированы тем же правилом (точные числа - в таблице «Сущности» отчёта).
Множители показывают, где ручной cost расходился с формулой (R2). Capsule получил `unlock = full = 0`, у опор `maxPerLayer = 0`
(опора нужна при любой сложности). Pear и Spitter получили `maxPerLayer` 3 и 2, чтобы вернуть PlacementLoss <= 10 % (в HEAD капов не было).
