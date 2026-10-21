# Ethereality — формализованный геймдизайн

Этот раздел — единый источник правды о том, **как устроена игра как система**: какие
сущности есть, какими числами они описаны, по каким формулам числа связаны, какие
инварианты должны выполняться и как это проверяется.

Правило поддержки: **любое изменение игрового числа (префаб, ScriptableObject,
константа в коде) → перегенерировать отчёт (`Tools/Balance/Generate Report`) и прогнать
Edit-mode тесты.** Если тест-цель начала предупреждать — это осознанное решение, которое
фиксируется в `06_Assumptions_Findings.md`.

## Структура

| Файл | Что внутри |
|---|---|
| `01_Core.md` | Фантазия, ядро цикла, глаголы игрока, ресурсы, условия конца |
| `02_Entities.md` | Каталог сущностей (игрок, враги/объекты, снаряды, улучшения) с их системной ролью и текущими числами |
| `03_Systems_Formulas.md` | Формулы всех подсистем: сложность, бюджет угроз, pacing, ролл, размещение, экономика HP, урон, прогрессия |
| `04_Balance_Model.md` | Расчётная модель баланса: метрики, константы, инварианты, цели, процедура проверки |
| `05_Dependency_Graph.md` | Граф зависимостей параметров: что от чего зависит и где это лежит |
| `06_Assumptions_Findings.md` | Реестр неявных допущений, противоречий и рекомендаций с приоритетами |
| `../Balance/BalanceReport.md`, `layers.csv` | **Генерируемые** артефакты: фактические числа, извлечённые из ассетов, и результат симуляции |

## Где живут числа (источники правды)

| Слой данных | Где | Примеры |
|---|---|---|
| Сложность и бюджет | `Assets/Infos/Generation/New Difficulty Profile.asset` | кривая, min/max threat per area, pacing |
| Состав врагов | `Assets/Infos/Generation/New Enemy Spawn Table.asset` | threatCost, weight, unlock/retire, maxPerLayer |
| Геометрия слоя | `Assets/Infos/Generation/New Layer Definition.asset` | 50×100, threatMultiplier |
| Размещение | `OpenWorld.unity` → `EnemyScatterGenerator.placement` | edgeMargin 2, minSpacing 2, safeZone 6 |
| Параметры сущностей | префабы в `Assets/Prefabs/Characters/**`, `Prefabs/Improvements/*` | hp, урон, кулдауны |
| Снаряды | `Assets/Infos/Projectiles/*.asset` | урон, скорость, силы |
| Улучшения (мета) | `Assets/Infos/Improvements/*.asset` | maxLevel, isPassive |
| Стартовое состояние | `Boot.unity` → `SaveSystem.defaultSaveData` | hp 4 |
| Модель баланса | `Assets/Infos/Balance/OpenWorld.asset` (`BalanceScenario`) | константы модели и цели |

## Инструменты

- `Tools/Balance/Create Default Scenario` — создаёт `BalanceScenario` для OpenWorld.
- `Tools/Balance/Generate Report` — офлайн-симуляция 60 слоёв, пишет `Assets/Docs/Balance/`.
- `Window/General/Test Runner` → EditMode → `Balance.*` — инварианты (падают) и цели (предупреждают).
- Код модели: `Assets/Scripts/Balance/` (runtime), `Assets/Scripts/Balance/Editor/`, тесты `Assets/Tests/Editor/Balance/`.
