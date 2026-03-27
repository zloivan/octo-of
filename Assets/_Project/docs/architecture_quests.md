# Архитектура — Система квестов

> Покрывает: QuestLogic, QuestService, UI, конфиг, команды, ReportEvent, возврат в нарратив, SFX.
> Внешние зависимости: `LocationService` (вызывает ReportEvent), `StartMiniGameCommand` (вызывает ReportEvent), `AttSusDelta` из [`architecture_scales.md`](architecture_scales.md)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 13. Система квестов

### 13.1 Контекст
Квесты выдаются в сегментах свободного перемещения. Каждый квест состоит из объективов. Выполнение объектива происходит через `ReportEvent` когда игрок взаимодействует с миром. При выполнении всех объективов — квест завершён. При выполнении всех квестов дня — возврат в нарратив.

### 13.2 Конфиг

```
QuestConfig : ScriptableObject
  └── QuestDefinition[]
        ├── id: string                    ← "find_diary", "complete_minigame_grace"
        ├── localizationKey: string
        ├── isOrdered: bool               ← если true — квесты дня выдаются последовательно
        ├── day: string                   ← "day1", "day2"
        └── objectives: ObjectiveDefinition[]
              ├── id: string              ← "find_page_1", "find_page_2"
              ├── objectiveTag: string    ← тег ReportEvent ("diary_page", "play_minigame_shooting")
              ├── targetCount: int
              └── rewardDelta: AttSusDelta  ← опционально; тип из architecture_scales.md
```

Отдельного объекта "завершённого квеста" нет. Завершённость — "все объективы выполнены".

### 13.3 QuestLogic (plain C#)

```csharp
public class QuestLogic
{
    private QuestDefinition _definition;
    private ObjectiveProgress[] _progress;

    public void Initialize(QuestDefinition def) { /* ... */ }

    public bool ReportEvent(string objectiveTag) { /* ... */ }  // true если объектив завершён

    public bool IsCompleted => Array.TrueForAll(_progress, p => p.IsCompleted);
    public ObjectiveProgress[] GetProgress() => _progress;

    public event Action<string> OnObjectiveCompleted;  // objectiveId
    public event Action OnQuestCompleted;
}
```

### 13.4 QuestService

```
QuestLogic (plain C#)
        ↑
QuestService : IEngineService<QuestService.State>,
               IStatefulService<QuestService.State>,
               IQuestEventReporter
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

**Событие для UI-слоя:**
```csharp
// QuestService публикует это событие при добавлении каждого нового активного квеста.
// QuestPanelUI подписывается на него и получает QuestLogic для привязки к QuestEntryView.
public event Action<QuestLogic> OnQuestAdded;
```

`QuestService` является единственным владельцем списка `QuestLogic` инстансов. Views не хранят логику — они только получают её по ссылке через `Bind()`.

**Последовательная выдача (`isOrdered`):**
Если `isOrdered = true` — сервис держит очередь, активирует по одному. При `OnQuestCompleted` dequeue следующий, создаёт `QuestLogic`, вызывает `OnQuestAdded`. Если `isOrdered = false` — все квесты дня активируются сразу при `@activateDayQuests`.

**State (Persistence):**
```csharp
[System.Serializable]
public class State
{
    public ActiveQuestSnapshot[] ActiveQuests;
    public string[] CompletedQuestIds;
    public string[] PendingOrderedQuestIds;
}

[System.Serializable]
public class ActiveQuestSnapshot
{
    public string QuestId;
    public ObjectiveProgressSnapshot[] Objectives;
}

[System.Serializable]
public class ObjectiveProgressSnapshot
{
    public string ObjectiveId;
    public int CurrentCount;
}
```

### 13.5 UI — QuestPanelUI и QuestEntryView

`QuestPanelUI : CustomUI` — живёт в Naninovel Custom UI Layer.

- Подписана на `QuestService.OnQuestAdded(QuestLogic logic)`
- При событии: инстанциирует `QuestEntryView` в ScrollView, вызывает `entryView.Bind(logic)`
- Уничтожение view — автоматически по `OnQuestCompleted` внутри самого view

`QuestEntryView : MonoBehaviour`:
- Поля: Quest Title (ManagedTextProvider), Progress Text ("1/2"), Progress Bar (Slider)
- `Bind(QuestLogic logic)` — подписывается на `logic.OnObjectiveCompleted` → `AnimateProgress()`; на `logic.OnQuestCompleted` → fade out + `quest_crossed` + `Destroy`
- Не хранит ссылку на сервис — только на переданный `QuestLogic`

**Цепочка владения:**
```
QuestService (владеет QuestLogic[])
    → OnQuestAdded(logic) →
QuestPanelUI (инстанциирует QuestEntryView)
    → entryView.Bind(logic) →
QuestEntryView (подписывается на logic.On*)
```

### 13.6 Команды и Bootstrapper

```csharp
.AddEngineService<QuestService>(questConfig)
.RegisterCommand<ActivateDayQuestsCommand>()    // @activateDayQuests day:day1
.RegisterCommand<StartFreeRoamCommand>()        // @startFreeRoam returnScript:Day1_End
.RegisterCommand<AddQuestCommand>()             // @addQuest id:find_diary
```

### В .nani скриптах
```
@activateDayQuests day:day2
@startFreeRoam returnScript:Day2_Sc3
@addQuest id:bonus_quest
```

| Команда | Параметры | Действие |
|---|---|---|
| `@activateDayQuests` | `day:day1` | Инициализирует квесты дня. `isOrdered` → только первый; иначе все сразу |
| `@startFreeRoam` | `returnScript:Day1_End` | Переводит в свободное перемещение, сохраняет returnScript |
| `@addQuest` | `id:find_diary` | Добавляет одиночный квест динамически |

Все команды — синхронные (структурно просты).

### 13.7 Интеграция ReportEvent

**Из LocationService** при клике по предмету:
```csharp
public void OnInteractableItemClicked(string itemId) {
    // Инициализация мини-игры / воспроизведение события / и т.д.

    // После выполнения действия — сообщаем квестам
    var eventReporter = Engine.GetService<IQuestEventReporter>();
    var itemConfig = _itemConfigMap[itemId];
    eventReporter.ReportEvent(itemConfig.objectiveTag);
}
```

**Из StartMiniGameCommand** при завершении мини-игры:
```csharp
var result = await miniGameService.RunAsync(id);

// ReportEvent вызывается ВСЕГДА — вне зависимости от победы или поражения.
// ГДД: "пункт засчитывается после завершения мини-игры, вне зависимости от результата."
var eventReporter = Engine.GetService<IQuestEventReporter>();
eventReporter.ReportEvent($"play_minigame_{id}");

// Шкалы — только при победе (см. architecture_scales.md §16.5)
if (result.IsVictory)
    SeductionScaleService.ApplyMiniGameResult(char, isVictory: true);
else
    SeductionScaleService.ApplyMiniGameResult(char, isVictory: false);
```

### 13.8 Возврат в нарратив

```csharp
// QuestService слушает OnQuestCompleted для каждого активного квеста
// Когда последний квест завершён:
public void CheckAllQuestsCompleted() {
    if (_activeQuests.All(q => q.IsCompleted)) {
        _returnCommand?.Invoke();  // Вызов @goto returnScript который был сохранён в @startFreeRoam
    }
}
```

---

## 10. Аудио (фрагмент — квесты)

| Ключ | Момент | Приоритет |
|---|---|---|
| `quest_ticked` | Выполнение одного действия внутри составного квеста | Med |
| `quest_crossed` | Выполнение квеста (все действия завершены) | High |
| `quest_completed` | Выполнение последнего квеста дня → возврат в нарратив | High |
