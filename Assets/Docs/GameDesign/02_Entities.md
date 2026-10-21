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

Роли enum `EnemyRole`: 0 Melee, 1 Ranged, 2 Tank, 3 Swarm, 4 Elite, 5 Support, 6 Ambusher, 7 Reward.

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

Вне таблицы: `GameOverKey` (выход в ручных сценах), `Circle2` (дубль Circle Laser).

Наблюдения, важные для баланса:
- **Единственные источники урона в OpenWorld**: Pear (дальний), Circle Laser (контакт), HexagonLaser
  (контакт, одноразово), `DeadZone`. Всё остальное — HP-губка или еда.
- Bee уходит из пула при d > 0.75 — вместе с ним уходит самый дешёвый источник лечения.
- LevelUp появляется только с d ≥ 0.55 → до этого улучшений нет вовсе.

## Снаряды (`Infos/Projectiles`)

| Снаряд | Кто | Урон | Скорость (×mult) | Жизнь | Поведение при попадании |
|---|---|---|---|---|---|
| ArrowProjectile | игрок | 1 | 1×10 | 2 с | прилипает (`FixedJoint2D`), через 0.25 с наносит урон и тянет: игрока к цели импульсом 50, цель к игроку 20 |
| ArrowMiniProjectile | – | 1 | ×10 | 2 с | прилипает без сил (не используется в таблице) |
| PearProjectile | Pear | 1 | 1×2 | 15 с | разрушается при попадании |

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
