# Декомпозиция задач: Полный проект

> Источник: ГДД — все механики
> Архитектура: architecture.md
> Статус: готово к оценке
> Порядок реализации: outside-in (UI → Logic → Config → Service → Commands → Integration)

---

## Эпик 1: Свободное перемещение

Сегменты свободного перемещения чередуются с линейным повествованием. Игрок перемещается по локациям, взаимодействует с объектами и мини-играми. Возврат в нарратив — автоматически при выполнении всех квестов сегмента.

### Фича 1.1: Система локаций (инфраструктура)

Общая база для Перемещения и Поинт-энд-клик. Должна быть реализована первой.

| Задача | Категория | Оценка |
|---|---|---|
| 1.1.1 Спайк — Naninovel Custom UI + Background scaling | 🔴 | 2.2ч |
| 1.1.2 ScriptableObject конфиги локаций | 🟢 | 3ч |
| 1.1.3 LocationService + LocationLogic + EnterLocationCommand | 🟡 | 4.3ч |
| 1.1.4 HotspotLayerUI + HotspotView | 🟡 | 5.5ч |
| 1.1.5 Outline шейдер (idle + hover) | 🟡 | 3.3ч |
| 1.1.6 Shimmer шейдер (скрытые предметы) | 🔴 | 5ч |
| **Сумма + 20% буфер** | | **~28ч** |

### Фича 1.2: Механика "Перемещение"

Зависит от: Фича 1.1, QuestService (2.1)

| Задача | Категория | Оценка |
|---|---|---|
| 1.2.1 LocationTransitionLogic | 🟡 | 3.2ч |
| 1.2.2 Кнопка "Назад" | 🟡 | 2.5ч |
| 1.2.3 Логика доступности локаций | 🟡 | 2.5ч |
| 1.2.4 Триггер возврата в нарратив (overlap с 2.1.7) | 🟡 | 3.2ч |
| 1.2.5 Сцены при входе на локацию | 🟢 | 2ч |
| **Сумма + 20% буфер** | | **~16ч** |

### Фича 1.3: Механика "Поинт энд клик"

Зависит от: Фича 1.1, QuestService (2.1), SeductionScaleService (3.1)

| Задача | Категория | Оценка |
|---|---|---|
| 1.3.1 InteractableItemConfig | 🟢 | 2ч |
| 1.3.2 One-shot активация предметов | 🟡 | 2.2ч |
| 1.3.3 Логика клика по предмету | 🟡 | 3.2ч |
| 1.3.4 Ачивка за сбор всех секретов | 🟢 | 2ч |
| **Сумма + 20% буфер** | | **~11.5ч** |

**Итого Эпик 1: ~55.5ч**

---

## Эпик 2: Квесты

Квесты выдаются в сегментах свободного перемещения и отображаются в панели. Каждый квест состоит из объективов, завершаемых событиями через ReportEvent. При выполнении всех квестов — автоматический возврат в нарратив.

### Фича 2.1: Система квестов

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 2.1.1 QuestPanelUI + QuestEntryView | 🟡 | 4.3ч | 1 (UI first) |
| 2.1.2 QuestLogic (доменная логика) | 🟡 | 4.2ч | 2 |
| 2.1.3 QuestConfig (ScriptableObject) | 🟢 | 2ч | 3 |
| 2.1.4 QuestService (Naninovel lifecycle) | 🟡 | 3.2ч | 4 |
| 2.1.5 Naninovel команды (@startFreeRoam, @activateDayQuests, @addQuest) | 🟡 | 3ч | 5 |
| 2.1.6 Интеграция ReportEvent с LocationService | 🟡 | 2.5ч | 6 |
| 2.1.7 Логика возврата в нарратив | 🟡 | 3.2ч | 7 |
| 2.1.8 Аудио квестов | 🟢 | 2ч | 8 |
| **Сумма + 20% буфер** | | **~29.5ч** | |

**Итого Эпик 2: ~29.5ч**

---

## Эпик 3: Шкалы соблазнения

ATT/SUS шкалы per-персонаж. Источники: диалоговые выборы, результаты мини-игр, клики по предметам. Шкалы влияют на доступность выборов и нарративные ветки.

### Фича 3.1: Инфраструктура шкал

Базовая система без UI выборов. Должна быть реализована первой.

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 3.1.1 Спайк — кастомный Choice Handler в Naninovel | 🔴 | 4.3ч | 1 |
| 3.1.2 ScriptableObject конфиг шкал | 🟢 | 1.5ч | 2 |
| 3.1.3 SeductionScaleLogic + вспомогательные типы | 🟡 | 3ч | 3 |
| 3.1.4 SeductionScaleService | 🟡 | 2.5ч | 4 |
| 3.1.5 SeductionScaleUI + ScaleBarView | 🟡 | 4ч | 5 |
| 3.1.6 Команды без выборов (@initCharacterScales, @applyScaleDelta, @miniGameScaleResult) | 🟢 | 2.5ч | 6 |
| **Сумма + 20% буфер** | | **~21ч** | |

### Фича 3.2: Диалоговые выборы (@choiceEx)

Зависит от: Фича 3.1, Спайк 3.1.1 (блокирует фичу)

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 3.2.1 ChoiceExEntry + данные выбора | 🟡 | 2ч | 1 |
| 3.2.2 SeductionChoiceHandlerUI | 🔴 | 5.3ч | 2 |
| 3.2.3 ChoiceExButtonView + ScaleEffectIndicatorView | 🟡 | 4.3ч | 3 |
| 3.2.4 ChoiceExCommand | 🔴 | 4.7ч | 4 |
| **Сумма + 20% буфер** | | **~20ч** | |

### Фича 3.3: Интеграция с существующими системами

Модификация уже запланированных задач из Эпика 1.

| Задача | Категория | Оценка |
|---|---|---|
| 3.3.1 Интеграция item click → ApplyDelta | 🟢 | 1.5ч |
| 3.3.2 Аудио шкал | 🟢 | 1.5ч |
| **Сумма + 20% буфер** | | **~3.5ч** |

**Итого Эпик 3: ~44.5ч**

---

## Эпик 4: Инфраструктура мини-игр

Общий каркас запуска, flow управления и UI для всех четырёх механик. Конкретные механики реализуются в Эпиках 5–8 поверх этой инфраструктуры. Эпик 4 блокирует все последующие эпики.

### Фича 4.1: Shell UI и панели

Outside-in: начинаем с видимого.

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.1.1 MiniGameWindowUI + переключение панелей | 🟡 | 3.2ч | 1 |
| 4.1.2 TutorialPanelView | 🟢 | 2ч | 2 |
| 4.1.3 GameplayPanelView | 🟡 | 2.2ч | 3 |
| 4.1.4 ResultPanelView | 🟡 | 3ч | 4 |
| **Сумма + 20% буфер** | | **~12.5ч** | |

### Фича 4.2: Переиспользуемые компоненты и таблица рекордов

Компоненты пишутся один раз — используются всеми четырьмя механиками.

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.2.1 TimerBarView | 🟢 | 1.5ч | 1 |
| 4.2.2 ScoreCounterView | 🟢 | 1ч | 2 |
| 4.2.3 MiniGameLeaderboardLogic | 🟡 | 2.2ч | 3 |
| 4.2.4 LeaderboardPanelView + LeaderboardEntryView | 🟡 | 3.2ч | 4 |
| **Сумма + 20% буфер** | | **~9.5ч** | |

### Фича 4.3: IMiniGame, данные и Config

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.3.1 MiniGameConfig + вспомогательные типы данных | 🟢 | 2ч | 1 |
| 4.3.2 IMiniGame + StubMiniGame | 🟢 | 1.5ч | 2 |
| **Сумма + 20% буфер** | | **~4.2ч** | |

### Фича 4.4: MiniGameService — flow управление и persistence

Зависит от: Фича 4.1, 4.2, 4.3

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.4.1 MiniGameService (регистрация, flow, persistence) | 🟡 | 5.3ч | 1 |
| **Сумма + 20% буфер** | | **~6.4ч** | |

### Фича 4.5: Naninovel команда и интеграция

Зависит от: Фича 4.4, QuestService (2.1), SeductionScaleService (3.1)

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.5.1 StartMiniGameCommand | 🟡 | 3ч | 1 |
| **Сумма + 20% буфер** | | **~3.6ч** | |

**Итого Эпик 4: ~36.2ч**

### Фича 4.6: Журнал — JournalWindowUI + Contacts Tab

Зависит от: Фича 4.2 (LeaderboardPanelView = Scores вкладка), SeductionScaleService (3.1)

| Задача | Категория | Оценка | Порядок |
|---|---|---|---|
| 4.6.1 `JournalWindowUI : CustomUI` — контейнер трёх вкладок (Scores/Contacts/Progress); переключение; `@openJournal` команда | 🟡 | 3.5ч | 1 |
| 4.6.2 `CharacterContactConfig : ScriptableObject` — id, portraitRef, nameLocKey, bioLocKey, likesLocKey, dislikesLocKey | 🟢 | 1.5ч | 2 |
| 4.6.3 `ContactsTabView` — список персонажей слева, детальная панель справа; ATT/SUS через `SeductionScaleService.GetScaleSnapshot` | 🟡 | 5.5ч | 3 |
| 4.6.4 `ScaleSnapshotView` — два Image fillAmount (ATT/SUS) + цифры; snapshot при открытии вкладки | 🟢 | 1.5ч | 4 |
| **Сумма + 20% буфер** | | **~14.4ч** | |

> **Фича 4.7: Progress Tab** — декомпозируется после получения ГДД от Марии.

**Итого Эпик 4 (обновлено): ~50.6ч**

---

## Эпик 5: Механика "Стрельба"

Мини-игра на точность. Прицел с дрожащей красной точкой (`ReticleWobbleLogic` + `CrosshairView`). Попадание определяется позицией точки, не курсора. Один префаб `ShootingMiniGame`, три вариации через конфиг (`shooting`, `grace_shooting`, `beer_pong`). Движение целей — паттерн Стратегия (`LinearBounce` / `Bezier` / `Static`). Зависит от Эпика 4.

### Фича 5.1: Ядро механики

Зависит от: Эпик 4 (IMiniGame, MiniGameService)

| Задача | Категория | Оценка |
|---|---|---|
| 5.1.1 Спайк — ITargetMovementStrategy + LinearBounceStrategy | 🔴 | 3.5ч |
| 5.1.2 BezierCurveStrategy + StaticStrategy | 🟡 | 2ч |
| 5.1.3 ReticleWobbleLogic (plain C#, тесты) | 🟡 | 2.5ч |
| 5.1.4 CrosshairView (следование за курсором, зона прицеливания) | 🟡 | 3ч |
| 5.1.5 TargetGradeDefinition + ShootingMiniGameConfig (с wobble-параметрами) | 🟢 | 2ч |
| 5.1.6 IMiniGame.Initialize + MiniGameService wiring | 🟡 | 1.5ч |
| 5.1.7 ShootingTargetView (движение, ContainsCanvasPoint, без OnPointerClick) | 🟡 | 3ч |
| 5.1.8 ShootingMiniGame : IMiniGame (loop, детекция через точку, Beer Pong стейт) | 🟡 | 5ч |
| **Сумма + 20% буфер** | | **~27ч** |

### Фича 5.2: Вариации и аудио

Зависит от: Фича 5.1

| Задача | Категория | Оценка |
|---|---|---|
| 5.2.1 ShootingConfig_Default + MiniGameDefinition shooting | 🟢 | 1ч |
| 5.2.2 ShootingConfig_Grace + MiniGameDefinition grace_shooting | 🟢 | 0.5ч |
| 5.2.3 ShootingConfig_BeerPong + MiniGameDefinition beer_pong | 🟢 | 1ч |
| 5.2.4 Аудио shooting_hit / shooting_perfect / shooting_miss | 🟢 | 1ч |
| **Сумма + 20% буфер** | | **~4.2ч** |

**Итого Эпик 5: ~31.2ч**

---

## Эпик 6: Механика "Борьба"

Мини-игра на реакцию. Индикаторы ЛКМ/ПКМ появляются поочерёдно с окном тайминга. Один префаб `FightingMiniGame`, три вариации через конфиг. Визуальный контекст изолирован в `IFightingEnvironment` — `FightingMiniGame` не знает что за ним: манекен, видео адалт-сцена или шкалы здоровья. Зависит от Эпика 4.

### Фича 6.1: Ядро механики

Зависит от: Эпик 4 (IMiniGame, MiniGameService)

| Задача | Описание | Категория | Оценка |
|---|---|---|---|
| 6.1.1 | `FightingMiniGameConfig` + вспомогательные типы (`PenaltyModeType`, `InputGrade`) | 🟢 | 1.5ч |
| 6.1.2 | `IFightingEnvironment` — интерфейс (`Initialize`, `OnInput`, `IsVictory`, `OnEnvironmentDefeat`) | 🟢 | 0.5ч |
| 6.1.3 | `FightingIndicatorLogic` (plain C#) — тайминг, `NormalizedProgress`, `Evaluate`, тесты | 🟡 | 2.5ч |
| 6.1.4 | `FightingIndicatorView` — `fillAmount` таймер, цвет иконки (синий/красный/нейтральный), `PlayHitEffect` | 🟡 | 3ч |
| 6.1.5 | `FightingCommentView` — fade-in/hold/fade-out label, `Show(string commentKey)` | 🟢 | 1.5ч |
| 6.1.6 | `HealthBarView` — `Image fillAmount`, `SetValue(int current, int max)` | 🟢 | 1ч |
| 6.1.7 | `FightingMiniGame : IMiniGame` — game loop, `ApplyResult`, `ApplyPenalty`, difficulty ramp, условия завершения | 🟡 | 5ч |
| **Сумма + 20% буфер** | | | **~18.6ч** |

### Фича 6.2: Среды (IFightingEnvironment реализации)

Зависит от: Фича 6.1 (контракт `IFightingEnvironment` зафиксирован)

| Задача | Описание | Категория | Оценка |
|---|---|---|---|
| 6.2.1 | `MannekinEnvironment` — `Animator` Hit триггер, `IsVictory = false`, `OnEnvironmentDefeat` не вызывается | 🟢 | 1.5ч |
| 6.2.2 | `HannahAdultEnvironment` — `VideoPlayer` + `_stateCheckpoints[]`, локальный `_localSuspicion`, перемотка при правильном вводе, `Stop()` при поражении | 🟡 | 3.5ч |
| 6.2.3 | `MudFightEnvironment` — два `HealthBarView` (ally/enemy), атака (ЛКМ) / уклонение (ПКМ) / ошибка → урон, `OnEnvironmentDefeat` при ally HP = 0 | 🟡 | 3ч |
| **Сумма + 20% буфер** | | | **~9.6ч** |

### Фича 6.3: Вариации и аудио

Зависит от: Фича 6.1, Фича 6.2

| Задача | Описание | Категория | Оценка |
|---|---|---|---|
| 6.3.1 | `FightingConfig_Training` + `MiniGameDefinition` запись `fighting_training` | 🟢 | 1ч |
| 6.3.2 | `FightingConfig_HannahAdult` + `MiniGameDefinition` запись `hannah_adult_fighting` | 🟢 | 1ч |
| 6.3.3 | `FightingConfig_MudFight` + `MiniGameDefinition` запись `mud_fighting` | 🟢 | 1ч |
| 6.3.4 | Аудио: `fighting_hit_perfect`, `fighting_hit_good`, `fighting_miss`, `fighting_combo` через `IAudioManager` | 🟢 | 1ч |
| **Сумма + 20% буфер** | | | **~4.8ч** |

**Итого Эпик 6: ~33ч**

---

## Эпик 7: Механика "Контроль"

Мини-игра на удержание баланса. Игрок заполняет вертикальную шкалу удержанием ЛКМ. Шкала периодически переходит в Tense → Critical — нужно отпустить кнопку. Один префаб `ControlMiniGame`, три вариации через конфиг. Визуальный контекст изолирован в `IControlEnvironment`. Зависит от Эпика 4.

### Фича 7.1: Ядро механики

Зависит от: Эпик 4 (IMiniGame, MiniGameService)

| Задача | Категория | Оценка |
|---|---|---|
| 7.1.1 `ControlMiniGameConfig` + `BarState` (`Normal`, `Tense`, `Critical`) | 🟢 | 1.5ч |
| 7.1.2 `IControlEnvironment` — интерфейс (`Initialize`, `OnBarStateChanged`, `OnCycleComplete`, `OnFail`) | 🟢 | 0.5ч |
| 7.1.3 `ControlBarLogic` (plain C#) — state machine, `FillValue`, `SetHolding`, `Tick`, `ResetCycle`, тесты | 🟡 | 3ч |
| 7.1.4 `ControlBarView` — `Image.fillAmount`, цвет по `BarState` | 🟢 | 1.5ч |
| 7.1.5 `ControlMiniGame : IMiniGame` — game loop, `Input.GetMouseButton(0)`, цикличный режим, условия завершения | 🟡 | 4.5ч |
| **Сумма + 20% буфер** | | **~13.2ч** |

### Фича 7.2: Среды (IControlEnvironment реализации)

Зависит от: Фича 7.1 (контракт `IControlEnvironment` зафиксирован)

| Задача | Категория | Оценка |
|---|---|---|
| 7.2.1 `GoatsEnvironment` — `Animator` переключение коз на `OnCycleComplete`, реакция на `OnBarStateChanged` | 🟢 | 1.5ч |
| 7.2.2 `RoxyMilkingEnvironment` — `VideoPlayer`, реакция на `OnBarStateChanged`, `OnFail` прерывает адалт | 🟡 | 3ч |
| 7.2.3 `MechanicalBullEnvironment` — анимация быка по `OnBarStateChanged`, `OnFail` = бык скинул | 🟡 | 2.5ч |
| **Сумма + 20% буфер** | | **~8.4ч** |

### Фича 7.3: Вариации и аудио

Зависит от: Фича 7.1, Фича 7.2

| Задача | Категория | Оценка |
|---|---|---|
| 7.3.1 `ControlConfig_Goats` + `MiniGameDefinition` `goats_balance` (`isCyclic=true`, `npcLeaderboard[]`) | 🟢 | 1ч |
| 7.3.2 `ControlConfig_Roxy` + `MiniGameDefinition` `roxy_milking` (без leaderboard) | 🟢 | 0.5ч |
| 7.3.3 `ControlConfig_Bull` + `MiniGameDefinition` `mechanical_bull` (без leaderboard) | 🟢 | 0.5ч |
| 7.3.4 Аудио: `control_fill`, `control_tense`, `control_fail`, `control_complete` через `IAudioManager` | 🟢 | 1ч |
| **Сумма + 20% буфер** | | **~3.6ч** |

**Итого Эпик 7: ~25.2ч**

---

## Эпик 8: Механика "Реакция"

Мини-игра на менеджмент кликов. До `maxActiveZones` (≤ 3) зон одновременно, каждая живёт `zoneLifetime` секунд. Стадии Green → Yellow → Red — визуальный индикатор убывающего времени (прогресс не сбрасывается). Каждый клик инкрементирует `FillProgress` зоны. При исчезновении зона конвертирует прогресс в очки (double score при 100%). Один `ReactionMiniGame` префаб, три вариации через конфиг. Эпик 8 блокируется Эпиком 4.

### Фича 8.1: Ядро механики

Зависит от: Эпик 4 (IMiniGame, MiniGameService)

| Задача | Категория | Оценка |
|---|---|---|
| 8.1.1 `ReactionMiniGameConfig` + `ZoneState` + `ZoneResult` | 🟢 | 1.5ч |
| 8.1.2 `ReactionZoneLogic` (plain C#, тесты) | 🟡 | 3ч |
| 8.1.3 `ReactionZoneView` — прогресс, цвет ободка, мигание в Red, `OnClicked` | 🟡 | 3.5ч |
| 8.1.4 `ReactionMiniGame : IMiniGame` — spawn, tick, scoring, difficulty ramp | 🟡 | 5ч |
| **Сумма + 20% буфер** | | **~15.6ч** |

### Фича 8.2: Вариации и аудио

Зависит от: Фича 8.1

| Задача | Категория | Оценка |
|---|---|---|
| 8.2.1 `ReactionConfig_Tractor` + `MiniGameDefinition` `tractor_reaction` | 🟢 | 0.5ч |
| 8.2.2 `ReactionConfig_CarWash` + `MiniGameDefinition` `car_wash_reaction` | 🟢 | 0.5ч |
| 8.2.3 `ReactionConfig_Training` + `MiniGameDefinition` `reaction_training` с `npcLeaderboard` | 🟢 | 1ч |
| 8.2.4 Аудио `reaction_click_hit` / `reaction_zone_complete` / `reaction_zone_miss` / `reaction_complete` | 🟢 | 1ч |
| **Сумма + 20% буфер** | | **~3.6ч** |

**Итого Эпик 8: ~19.2ч**


---

## Сводная таблица всех эпиков

| Эпик / Фича | Оценка (с буфером) |
|---|---|
| **Эпик 1: Свободное перемещение** | **~55.5ч** |
| 1.1 Инфраструктура локаций | ~28ч |
| 1.2 Перемещение | ~16ч |
| 1.3 Поинт энд клик | ~11.5ч |
| **Эпик 2: Квесты** | **~29.5ч** |
| 2.1 Система квестов | ~29.5ч |
| **Эпик 3: Шкалы соблазнения** | **~44.5ч** |
| 3.1 Инфраструктура шкал | ~21ч |
| 3.2 Диалоговые выборы | ~20ч |
| 3.3 Интеграция шкал | ~3.5ч |
| **Эпик 4: Инфраструктура мини-игр** | **~50.6ч** |
| 4.1 Shell UI и панели | ~12.5ч |
| 4.2 Переиспользуемые компоненты + Leaderboard | ~9.5ч |
| 4.3 IMiniGame + Config | ~4.2ч |
| 4.4 MiniGameService | ~6.4ч |
| 4.5 Команда + интеграция | ~3.6ч |
| 4.6 Журнал (JournalWindowUI + Contacts) | ~14.4ч |
| 4.7 Журнал (Progress Tab) | ❓ pending |
| **Эпик 5: Стрельба** | **~31.2ч** |
| 5.1 Ядро механики | ~27ч |
| 5.2 Вариации и аудио | ~4.2ч |
| **Эпик 6: Борьба** | **~33ч** |
| 6.1 Ядро механики | ~18.6ч |
| 6.2 Среды | ~9.6ч |
| 6.3 Вариации и аудио | ~4.8ч |
| **Эпик 7: Контроль** | **~25.2ч** |
| 7.1 Ядро механики | ~13.2ч |
| 7.2 Среды | ~8.4ч |
| 7.3 Вариации и аудио | ~3.6ч |
| **Эпик 8: Реакция** | **~19.2ч** |
| 8.1 Ядро механики | ~15.6ч |
| 8.2 Вариации и аудио | ~3.6ч |
| **Эпик 9: Мета-UI** | **~32.5ч** |
| 9.1 Main Menu | ~8.5ч |
| 9.2 Pause Menu | ~3.6ч |
| 9.3 Settings | ~4.8ч |
| 9.4 Save / Load | ~4.8ч |
| 9.5 Adult Gallery | ~10.8ч |
| **ИТОГО** | **~353.3ч + ❓** |

---

## Эпик 9: Мета-UI

Кастомные скины поверх встроенных Naninovel UI. Галерея читает `UnlockedAdultIds` из `MiniGameService.State`.

### Фича 9.1: Main Menu

| Задача | Категория | Оценка |
|---|---|---|
| 9.1.1 `MainMenuUI : CustomUI` — New Game, Continue, Settings, Gallery; Continue активна при `IStateManager.AnySaveExists()`; фон через `IBackgroundManager` | 🟡 | 5.5ч |
| 9.1.2 Логика активации Continue | 🟢 | 1.5ч |
| **Сумма + 20% буфер** | | **~8.5ч** |

### Фича 9.2: Pause Menu

Зависит от: Фича 9.3, 9.4

| Задача | Категория | Оценка |
|---|---|---|
| 9.2.1 `PauseMenuUI : CustomUI` — Resume, Settings, Save, Load, Main Menu | 🟢 | 3ч |
| **Сумма + 20% буфер** | | **~3.6ч** |

### Фича 9.3: Settings

| Задача | Категория | Оценка |
|---|---|---|
| 9.3.1 `SettingsUI : CustomUI` — слайдеры через `IAudioManager`; Language через `ILocalizationManager` | 🟡 | 4ч |
| **Сумма + 20% буфер** | | **~4.8ч** |

### Фича 9.4: Save / Load

| Задача | Категория | Оценка |
|---|---|---|
| 9.4.1 `SaveLoadUI : CustomUI` — скин слотов; Save/Load/Delete; скриншот из `GameStateSlot` | 🟡 | 4ч |
| **Сумма + 20% буфер** | | **~4.8ч** |

### Фича 9.5: Adult Gallery

Зависит от: MiniGameService (4.4), AdultStaticConfig (§6 архитектуры)

| Задача | Категория | Оценка |
|---|---|---|
| 9.5.1 `AdultStaticConfig : ScriptableObject` — id, imageRef, localizationKey | 🟢 | 1.5ч |
| 9.5.2 `GalleryWindowUI : CustomUI` — грид `GalleryEntryView`; locked/unlocked; клик → fullscreen | 🟡 | 4.5ч |
| 9.5.3 `GalleryFullscreenView` — полный статик, кнопка закрыть | 🟢 | 2ч |
| 9.5.4 `UnlockAllAdultsCommand` — `@unlockAllAdults` для демо | 🟢 | 1ч |
| **Сумма + 20% буфер** | | **~10.8ч** |

**Итого Эпик 9: ~32.5ч**

---

## Критические зависимости

- **Эпик 1 → Эпик 2**: QuestService требуется для триггера возврата в нарратив (1.2.4) и клика по предмету (1.3.3)
- **Эпик 1 → Эпик 3**: SeductionScaleService требуется для клика по предмету (1.3.3)
- **Эпик 3 → Эпик 2**: Спайк 3.1.1 блокирует Фичу 3.2 (выборы)
- **Эпик 4 → Эпики 2–3**: MiniGameService интегрируется с QuestService и SeductionScaleService в команде 4.5.1
- **Эпик 5 → Эпик 4**: ShootingMiniGame требует IMiniGame и MiniGameService из Эпика 4
- **Эпик 6 → Эпик 4**: FightingMiniGame требует IMiniGame и MiniGameService из Эпика 4
- **Эпик 6, Фича 6.2 → 6.1**: Все три среды зависят от зафиксированного контракта `IFightingEnvironment`
- **Эпик 7 → Эпик 4**: ControlMiniGame требует IMiniGame и MiniGameService из Эпика 4
- **Эпик 7, Фича 7.2 → 7.1**: Все три среды зависят от зафиксированного контракта `IControlEnvironment`

- **Эпик 8 → Эпик 4**: ReactionMiniGame требует IMiniGame и MiniGameService из Эпика 4

## Рекомендуемый порядок разработки

1. **Фича 1.1** (инфраструктура локаций) — базис для остального
2. **Фича 2.1** (квесты) параллельно с Фичей 1.2–1.3
3. **Спайк 3.1.1** (choice handler) — разблокирует Фичу 3.2
4. **Фича 3.1** (инфраструктура шкал)
5. **Фича 3.2** (диалоговые выборы)
6. **Фича 4.1–4.5** (инфраструктура мини-игр)
7. **Фича 5.1–5.2** (механика стрельбы)
8. **Фича 6.1–6.3** (механика борьбы)
9. **Фича 7.1–7.3** (механика контроля)
10. **Фича 8.1–8.2** (механика реакции)

## Открытые вопросы

- **Shimmer-эффект** — как выглядит визуально? (пульсация / блик / свечение)
- **Idle анимация outline** — статичный контур или анимирован?
- **Длительность показа шкал** — на сколько секунд шкалы остаются на экране?
- **Текст блокировки выбора** — что показывать в tooltip заблокированного выбора?
- **Skip во время сюжетной мини-игры** — кнопка Skip во время игры?
- **scoreGoal на ResultPanelView** — откуда берётся цель?
