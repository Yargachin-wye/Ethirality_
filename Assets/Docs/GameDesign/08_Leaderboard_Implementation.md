# ТЗ: серверный лидерборд без воспроизведения физики

Статус: задание на реализацию, 13 ноября 2026. Заменяет этапы 2 и 5
из `07_Leaderboard_Plan.md` (воспроизведение на сервере отменено).

## 0. Решения, принятые владельцем

- Проверка честности — только серверные проверки согласованности телеметрии
  и статистики. Серверная физика и журнал ввода по тикам НЕ реализуются.
- Backend: Supabase Free (Postgres + Edge Functions на Deno/TypeScript +
  anonymous auth). Свой сервер и платные сервисы не используются.
- Рейтинг только для OpenWorld, метрика `heightUnits` = `RunScore.HeightUnits`
  (уже считается в `Leaderboard/RunScore.cs`).
- Модель угроз: отсекаем правку файлов/памяти, телепорт, бессмертие, speedhack.
  НЕ отсекаем фабрикацию правдоподобной телеметрии и tool-assisted игру —
  компенсация: статус `review` и ручная модерация топа.

## 1. Существующий код (не переписывать, использовать)

| Файл | Роль |
|---|---|
| `Leaderboard/RunScore.cs` | `RunScore`, `LocalRunResult`, `RunEndReason`, `RulesetId` |
| `Leaderboard/OpenWorldRun.cs` | Жизненный цикл забега, `Finish()` до сброса сцены |
| `Leaderboard/RunInput.cs` | `RunCheckpoint` — готовый формат чекпоинта |
| `Leaderboard/LocalRunStore.cs` | Локальные рекорды, НЕ доказательство для сервера |
| `Bootstrapper/GameAnalyticsBeh.cs` | Дублирующий канал телеметрии (GA) |
| `Leaderboard/Editor/RunReplayProbe.cs` | Только редакторная диагностика, в сервер не идёт |

## 2. Схема Postgres

```
players(id, anon_user_id, nickname, device_hash, created_at, banned_at)
builds(build_id PK, game_version, ruleset_id, status: allowed|revoked)
leaderboards(id PK, ruleset_id, seed, status: open|closing|archived, closes_at)
runs(run_id PK, player_id FK, leaderboard_id FK, build_id FK,
     session_key_hash, started_at, expires_at, last_tick,
     status: active|finished|expired, verdict: pending|verified|rejected|review,
     reject_reason, claimed_height, verified_height)
run_chunks(run_id FK, seq, received_at, payload jsonb, hmac_ok bool,
           PRIMARY KEY(run_id, seq))
entries(leaderboard_id FK, player_id FK, height_units, run_id FK,
        achieved_at, PRIMARY KEY(leaderboard_id, player_id))
moderation(id, entry_id FK, action, note, created_at)
```

Правила: уникальный `(leaderboard_id, player_id)` в `entries`; одна активная
`runs` на игрока; клиент нигде не пишет `verified_height`/`entries`.

## 3. Edge Functions

### `POST /runs/start`
Анонимный JWT → проверить игрока не забанен, сборку `allowed`, таблицу `open`
и нет активной сессии → создать `run`, вернуть `{runId, seed, sessionKey,
expiresAt, fixedDeltaTime}`.

### `POST /runs/{id}/chunks`
Тело: `{seq, tick, checkpoints[], events[], inputHistogram{}, prevHash, hmac}`.
Проверить владельца JWT, `seq` следующий за последним, `tick` монотонен,
сессия активна и не истекла, `hmac` верен. Сохранить payload. Ответ `204`;
идентичный повтор (тот же seq+hash) — тоже `204`, другой payload на тот же
seq — `409`.

Формат чекпоинта = `RunCheckpoint` (без `x`-координаты необязательно, но
`y, vy, hp, dashCooldown, frozen, dashing, alive, upgrades` обязательны).
События: `damage|heal|upgrade|pauseStart|pauseEnd|death` с `tick` и payload.
`inputHistogram` — счётчики по `RunInputKind` за чанк.

### `POST /runs/{id}/finish`
Тело: `{tick, heightUnits, endReason, lastHash}`. Атомарно закрыть сессию,
запустить синхронные проверки (раздел 4), выставить `verdict`.
При `verified` — в транзакции `INSERT .. ON CONFLICT` в `entries` с
`height_units = GREATEST(existing, new)`. Идемпотентен: повтор возвращает
тот же verdict.

### `GET /leaderboards/{id}?offset&limit` и `GET /runs/{id}`
Топ-100 + запись самого игрока; статус и `reject_reason`/`verdict` забега.
Кеширование 30 сек, rate limit.

### Cron `POST /tasks/review-scan` (раз в час)
Статистика раздела 5: переводит `verified → review` при аномалиях,
пишет `moderation`. `review` не попадает в публичную выдачу топа, но виден
владельцу в `GET /runs/{id}`.

## 4. Синхронные проверки в `finish` (любой провал → `rejected` + причина)

P1. `reportedTicks × fixedDeltaTime ≤ wallElapsed(started_at→finish) + 5%`
    — ловит speedhack.
P2. Суммарная пауза и длительность сессии в лимитах таблицы.
P3. Кинематика по чекпоинтам: `Δy/Δt ≤ maxVerticalSpeed` (константа из
    движения игрока + рывок, с запасом ×1.2); `|y_{i+1} − y_i − v̄·Δt| ≤ допуск`.
P4. Ресурсы: `hp_i = hp_{i-1} − Σdamage + Σheal`; `hp` в диапазоне;
    `alive` переходит в `false` ровно один раз; `death` — последнее событие.
P5. `dashCooldown` не убывает быстрее реального времени; `dashing` не чаще
    кулдауна; `frozen` меняется только через Freeze-ввод.
P6. `upgrades` только из списка `ruleset`, без несуществующих id; число
    событий `upgrade` соответствует `upgrades.Length`.
P7. `claimedHeight` равен `max(y) − startY`, вычисленному из чекпоинтов
    (±1 unit), монотонен.
P8. `inputHistogram` непустой при длине забега > N сек; `look`-события > 0.
P9. Все `IsValid`-проверки чекпоинтов (`RunCheckpoint.IsValid`, `RunInput.IsValid`).
P10. Порядок `seq`/`tick` без дыр и откатов; `prevHash`-цепочка непрерывна.

## 5. Асинхронные статистические проверки (cron)

S1. `height_units` > p99 по таблице → `review`.
S2. Игроку < 3 забегов и результат в топ-10 таблицы → `review`.
S3. Гистограмма ввода аномально плоская/нулевая относительно медианы → `review`.
S4. `device_hash` совпадает у >2 аккаунтов в топе → `review` обоих.
S5. Сверка на уровне популяции (вручную по GA dashboard): число
    `openworld_run Complete` в GA vs число `finish` в БД за сутки.

## 6. Изменения в Unity-клиенте

| Файл/место | Задача |
|---|---|
| `Leaderboard/RunRecorder.cs` (новый) | Пишет чекпоинты раз в ~2 сек игрового времени (не в паузе), события из `MessageBroker` (урон, heal, `AddNewImprovementEvent`, `GameplayPauseChangedEvent`, смерть), считает `inputHistogram` через `RunReplayProbe.TryQueue`-подобный хук в `PlayerControllable`, собирает чанк, считает SHA-256 цепочку и HMAC-SHA256 по `sessionKey` |
| `Leaderboard/LeaderboardClient.cs` (новый) | `UnityWebRequest` + anon auth Supabase; буфер чанков с отправкой раз в 10–15 сек; WAL-буфер в `persistentDataPath` для повторной досылки до `expires_at`; никогда не блокирует UI |
| `Leaderboard/RankedRunService.cs` (новый) | `StartAsync` → `runs/start` → seed/лимиты в контекст до генерации; `FinishAsync` на `OpenWorldRun.Finish(Death)` → `runs/{id}/finish`; статусы наружу для UI |
| `OpenWorldRun.Begin` | Принять `runId`/сессию от сервера вместо локального `Guid` для рейтингового режима; `diagnostic`-забеги не отправляются |
| `GameAnalyticsBeh` | Добавить `runId` в progression/resource события (custom field) — канал сверки S5 |
| `ChoosingNextLevelPanel` | Кнопка рейтингового старта (требует сеть), индикатор статуса |
| `GameOverPanel` / `RunResultText` | Показывать `verdict`: «Проверяется/Принят/Отклонён + причина/На проверке» |
| Новая `LeaderboardPanel` | Топ-100, своё место, версия таблицы, состояния нет-сети/архив |
| `GameplayManager.OnGameOver` | Вызвать `FinishAsync` до сброса сохранения и выгрузки сцены |

`PlayerControllable.ApplyRunInput` уже существует — для гистограммы добавить
подсчёт вызовов при живом `RunRecorder`, без изменения логики ввода.
`RunReplayProbe` и запись полного ввода НЕ выносить в релизную сборку.

## 7. Порядок работ

1. SQL-миграции + seed-данные ruleset/build/leaderboard.
2. Edge Functions `start`/`chunks`/`finish` с проверками P1–P10 (unit-тесты
   Deno на сгенерированных чанках).
3. `GET /leaderboards`, `GET /runs/{id}`, cron review-scan.
4. `LeaderboardClient` + `RunRecorder` + `RankedRunService` в Unity;
   EditMode-тесты сериализации чанков и HMAC рядом с `RunTrackingTests`.
5. Интеграция UI и `GameAnalyticsBeh`.
6. Нагрузочная проверка: 1 чанк/15 сек/игрок при 1000 активных забегов.

## 8. Критерии приёмки

- Правка `local-run-results.json`/`savegame.json` не влияет на топ.
- Журнал с телепортом (скачок y), бессмертием (hp без damage), speedhack
  (ticks > wall-time) или `score` без чекпоинтов → `rejected` с причиной.
- Честный забег с паузами и обрывом сети (досылка до срока) → `verified`.
- Повтор `finish`, повтор чанка, чужой `runId`, истёкшая сессия → корректные
  ответы, без дублей в `entries`.
- Отозванная сборка не может стартовать; начатые забеги досылаются до срока.
- Топ-результат от нового аккаунта → `review`, не виден в публичном топе.

## 9. Явно вне объёма

Серверное воспроизведение, журнал ввода по тикам, аттестация устройства,
антиспам-бот-детект сложнее гистограмм, перенос рейтинга между версиями.
