# Архитектура — Система квестов

> Покрывает: QuestLogic, QuestService, UI, конфиг, команды, ReportEvent, возврат в нарратив, SFX.
> Внешние зависимости: `LocationService` (ReportEvent), `StartMiniGameCommand` (ReportEvent)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 13. Система квестов

### 13.1 Контекст

Квесты выдаются в сегментах свободного перемещения. Каждый квест состоит из объективов. Выполнение объектива — через `ReportEvent`. При выполнении всех квестов дня — `QuestService` сигнализирует `OnAllQuestsCompleted`, что разблокирует `@exitNarrative` и возвращает управление нарративному скрипту.

### 13.2 Конфиг

```
QuestConfig : ScriptableObject
  └── QuestDefinition[]
        ├── id: string
        ├── localizationKey: string
        ├── isOrdered: bool
        ├── day: string
        └── objectives: ObjectiveDefinition[]
              ├── id: string
              ├── objectiveTag: string
              ├── targetCount: int
              └── rewardDelta: AttSusDelta  ← опционально
```

### 13.3 QuestLogic (plain C#)

```csharp
public class QuestLogic
{
    public void Initialize(QuestDefinition def) { }
    public bool ReportEvent(string objectiveTag) { } // true если объектив завершён
    public bool IsCompleted => Array.TrueForAll(_progress, p => p.IsCompleted);
    public ObjectiveProgress[] GetProgress() => _progress;

    public event Action<string> OnObjectiveCompleted; // objectiveId
    public event Action OnQuestCompleted;
}
```

### 13.4 QuestService

```
QuestLogic (plain C#)
        ↑
QuestService : IEngineService, IStatefulService<State>, IQuestEventReporter
        ↑
Naninovel Commands (§13.6)
```

**IQuestEventReporter:**
```csharp
public interface IQuestEventReporter
{
    void ReportEvent(string objectiveTag);
}
```

**События:**
```csharp
// UI-слой подписывается для отображения квестов
public event Action<QuestLogic> OnQuestAdded;

// ExitNarrativeCommand awaits этот UniTask
public UniTask WaitForAllQuestsCompleted(CancellationToken ct);
```

`WaitForAllQuestsCompleted` — возвращает уже завершённый `UniTask` если активных квестов нет, иначе ждёт последнего `OnQuestCompleted`.

**Последовательная выдача (`isOrdered`):**
Если `isOrdered=true` — активируется по одному. При `OnQuestCompleted` — dequeue следующий → `OnQuestAdded`. Если `false` — все сразу при `@activateDayQuests`.

**State:**
```csharp
[System.Serializable]
public class State
{
    public ActiveQuestSnapshot[] ActiveQuests;
    public string[] CompletedQuestIds;
    public string[] PendingOrderedQuestIds;
}
```

### 13.5 UI — QuestPanelUI и QuestEntryView

`QuestPanelUI : CustomUI` — в Naninovel Custom UI Layer.
- Подписана на `QuestService.OnQuestAdded(QuestLogic logic)`
- Инстанциирует `QuestEntryView`, вызывает `entryView.Bind(logic)`

`QuestEntryView : MonoBehaviour`:
- `Bind(QuestLogic logic)` — подписывается на `logic.OnObjectiveCompleted` → анимирует прогресс; на `logic.OnQuestCompleted` → fade out + `quest_crossed` + `Destroy`
- Не хранит ссылку на сервис

```
QuestService (владеет QuestLogic[])
    → OnQuestAdded(logic) →
QuestPanelUI (инстанциирует QuestEntryView)
    → entryView.Bind(logic) →
QuestEntryView (подписывается на logic.On*)
```

### 13.6 Команды

| Команда | Параметры | Действие |
|---|---|---|
| `@activateDayQuests` | `day:day1` | Инициализирует квесты дня. `isOrdered` → только первый; иначе все сразу |
| `@exitNarrative` | `id:backyard` (опц.) | Входит в свободное перемещение, ждёт `WaitForAllQuestsCompleted` |
| `@addQuest` | `id:find_diary` | Добавляет одиночный квест динамически |

**ExitNarrativeCommand** — блокирующая команда, центральный механизм перехода между нарративом и свободным перемещением:

```csharp
[CommandAlias("exitNarrative")]
public class ExitNarrativeCommand : Command
{
    public StringParameter Id; // опциональный locationId

    public override async UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        var locationService = Engine.GetService<LocationService>();
        var questService = Engine.GetService<QuestService>();

        // Войти на локацию (или остаться на текущей)
        var locationId = Id.HasValue ? Id.Value : locationService.CurrentLocationId;
        await locationService.Enter(locationId, asyncToken.CancellationToken);

        // Ждём выполнения всех квестов дня — здесь блокируется скрипт
        await questService.WaitForAllQuestsCompleted(asyncToken.CancellationToken);

        // Возврат в нарратив — скрипт продолжается
    }
}
```

### В .nani скриптах

```
; Стандартный флоу дня
@activateDayQuests day:day1
@exitNarrative id:backyard          ← блокирует до выполнения всех квестов

; Динамически добавить квест по ходу повествования
@addQuest id:bonus_quest

; Войти в свободное перемещение и вернуть на последнюю локацию
@exitNarrative
```

### 13.7 Интеграция ReportEvent

**Из LocationService** (клик по предмету):
```csharp
Engine.GetService<IQuestEventReporter>()?.ReportEvent(itemConfig.objectiveTag);
```

**Из StartMiniGameCommand** (завершение мини-игры):
```csharp
// ReportEvent — ВСЕГДА, независимо от победы
Engine.GetService<IQuestEventReporter>()?.ReportEvent($"play_minigame_{id}");
```

### 13.8 Возврат в нарратив

Механизм — `QuestService.WaitForAllQuestsCompleted()`, который awaits `ExitNarrativeCommand`. При завершении последнего квеста:

1. `QuestLogic.OnQuestCompleted` → `QuestService.CheckAllCompleted()`
2. Все активные квесты завершены → `QuestService` резолвит `UniTask`
3. `@exitNarrative` разблокируется → скрипт дня продолжается
4. Воспроизводится `quest_completed`

Нет отдельного `returnScript`, нет `@goto`. Нарративный скрипт просто продолжает следующую строку.

---

## 10. Аудио (фрагмент — квесты)

| Ключ | Момент | Приоритет |
|---|---|---|
| `quest_ticked` | Выполнение одного действия составного квеста | Med |
| `quest_crossed` | Выполнение квеста | High |
| `quest_completed` | Выполнение последнего квеста дня | High |
