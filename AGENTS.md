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
