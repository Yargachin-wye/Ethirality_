# Ethereality — заметки для агентов

## Проверка (Unity 2022.3.62f3, редактор должен быть закрыт)
- EditMode-тесты баланса/генерации:
  `"C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testFilter BalanceTests -testResults <tmp>\results.xml -logFile <tmp>\unity.log`
  (exit 0 = всё ок, 2 = есть упавшие тесты). Тесты категории `Balance.Target` дают Inconclusive — это предупреждения, а не ошибки.
- Перегенерация отчёта баланса (`Assets/Docs/Balance/BalanceReport.md`, `layers.csv`):
  `... -batchmode -quit -projectPath . -executeMethod Balance.Editor.BalanceReportGenerator.Generate -logFile <tmp>\report.log`

## Генерация мира
- Пайплайн: `LayerPlanner.Plan` → `LayerPopulator.Populate` (`SupportChain` → `LayoutDecorator` → `PackPlacer`) → `RewardPlanner`.
- Список `LayerDefinition` задаётся в двух местах: в сцене `Assets/Scenes/OpenWorld.unity` (`GeneratorsManager.layerDefinitions`) и в сценарии баланса `Assets/Infos/Balance/OpenWorld.asset`. Держите их синхронными.

## Рендеринг: URP + 2D-свет
- Проект на **URP 14.0.12** с `Renderer2D` (`Assets/Settings/URP/`). Пайплайн-ассет `URP-2D.asset` назначен в Graphics и во все уровни качества. Спрайты без материала рендерятся `Sprite-Lit-Default` (defaultMaterialType=Lit).
- Глобальный свет и постпроцессинг живут на персистентном префабе `Assets/Prefabs/Boot/Camera.prefab`: дочерние `Global Light 2D` (Global, интенсивность 0.35, холодный оттенок) и `Global Volume` (профиль `DarkAtmosphere.asset`: Bloom, Vignette, ColorAdjustments). Постпроцессинг включён в `UniversalAdditionalCameraData`.
- `Light 2D` (Point) расставлены на игроке, снарядах, апгрейдах (`Improvements/`), Mine, GameOverKey, LevelUp. `ShadowCaster2D` — на основном спрайте всех префабов в `Characters/` и `Deads/` (useRendererSilhouette, selfShadows=0).
- Партиклы и LineRenderer используют `Assets/Settings/URP/URP-Particles-Unlit.mat`.
- Повторный прогон миграции: `Tools/Rendering/Run URP 2D Lighting Migration` (`Ethereality.EditorTools.Urp2DLightingMigration.Run`) — идемпотентен; полезен при добавлении новых префабов.
- `Custom/SmoothWarp` переписан под URP HLSL — builtin-шейдеры в URP не работают (розовый экран), при добавлении новых шейдеров писать только URP-совместимые.

## Аудио
- Имена звуков — константы в `AudioConst` (`Assets/Scripts/Constants/Constants.cs`), полный список — `AudioConst.AllSounds`. Библиотека — `Assets/Infos/AudioLibrary.asset`.
- Конвенция имён: файл называется точно как константа (`PlayerDeadSound.wav`) и кидается в `Assets/Audio/SFX/`.
- `Tools/Audio/Autofill Library` (`Audio.Editor.AudioLibraryTools`) — проходит по `AllSounds`, находит клипы по имени через `AssetDatabase.FindAssets` и проставляет `clip`; запись создаётся при отсутствии (`Ui*` → `uiSounds`, остальные → `vfxSounds`).
- `Tools/Audio/Report Missing` — чеклист: имена из `AllSounds` без клипа/записи, строковые поля `*Sound` (`deadSound`/`dmgSound`/`hitSound`/`explosionSound`/…) на префабах без клипа в библиотеке, записи библиотеки вне `AllSounds`.
