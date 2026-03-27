# Архитектурный индекс

> Точка входа. Содержит только контекст проекта и навигацию по модулям.
> При изменении любой механики — менять только файл соответствующего модуля, не этот.

---

## Модули

| Файл | Что покрывает |
|------|--------------|
| [`architecture_core.md`](architecture_core.md) | DI, View layer, Persistence, UniTask, запреты |
| [`architecture_locations.md`](architecture_locations.md) | Рендеринг фона, хотспоты, активация, LocationConfig, SFX локаций |
| [`architecture_quests.md`](architecture_quests.md) | Квесты, QuestConfig, QuestService, UI, SFX |
| [`architecture_scales.md`](architecture_scales.md) | Шкалы ATT/SUS, ChoiceEx, SeductionScaleConfig, Contacts UI, SFX |
| [`architecture_minigames.md`](architecture_minigames.md) | Инфраструктура мини-игр, MiniGameConfig, Gallery UI, SFX shell |
| [`architecture_minigame_shooting.md`](architecture_minigame_shooting.md) | Механика "Стрельба", ShootingMiniGameConfig, SFX |
| [`architecture_minigame_fighting.md`](architecture_minigame_fighting.md) | Механика "Борьба", FightingMiniGameConfig, SFX |
| [`architecture_minigame_control.md`](architecture_minigame_control.md) | Механика "Контроль", ControlMiniGameConfig, SFX |
| [`architecture_minigame_reaction.md`](architecture_minigame_reaction.md) | Механика "Реакция", ReactionMiniGameConfig, SFX |
| [`architecture_ui.md`](architecture_ui.md) | Журнал, MainMenu, Pause, Settings, SaveLoad |

---

## 1. Контекст

Рассматривались механики из ГДД:
- **Механика "Перемещение"** — навигация по локациям, интерактивные зоны перехода
- **Механика "Поинт энд клик"** — интерактивные предметы на локациях
- **Механика "Квесты"** — задания в сегментах свободного перемещения, панель квестов, условия возврата в нарратив
- **Механика "Шкалы соблазнения"** — ATT/SUS per персонаж, диалоговые выборы с проверкой, результаты мини-игр
- **Механика "Мини-игры"** — инфраструктура запуска, общий shell UI, таблица рекордов, интеграция с квестами и шкалами
- **Механика "Контроль"** — мини-игра на удержание баланса, state machine шкалы, IControlEnvironment для изоляции визуального контекста

Перемещение и Поинт-энд-клик делят общую техническую инфраструктуру — систему хотспотов. Квесты — отдельная система, связанная с ними через событийный механизм `ReportEvent`. Шкалы — отдельная система, связанная с диалогами через кастомную команду `@choiceEx` и с мини-играми через `@miniGameScaleResult`. Мини-игры — отдельная система; конкретные механики подключаются как `IMiniGame` реализации, shell ничего о них не знает.
