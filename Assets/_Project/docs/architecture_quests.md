# Архитектура — Система квестов

> Покрывает: домен квестов, QuestService, QuestProgressService, GameFlowService,
> IQuestStatusSource, IFreeRoamSessionSource, DaySessionOrchestrator, Quest UI (MVVM).
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 1. Контекст

Квесты выдаются автоматически при входе в сегмент free roam через `@activateDayQuests`.
При завершении всех квестов дня `GameFlowService` возобновляет нарратив.
Нарративный скрипт не знает о квестах и точках возврата — он только рассказывает историю и вызывает `@exitNarrative`.

---

## 2. Доменный слой (Pure C#)

Никаких зависимостей на Unity и Naninovel. Полностью тестируемо без движка.
Следует паттерну `HotspotData` / `HotspotLogic` из системы локаций.

### QuestObjectiveType (enum)

```csharp
public enum QuestObjectiveType { VisitLocation, FindItem, PlayMiniGame }
```

### QuestEventId (static class — единственный источник строковых ID событий)

```csharp
public static class QuestEventId
{
    // Локации: совпадают с LocationId из LocationConfigSO
    // Предметы: совпадают с HotspotId из HotspotEntry
    // Мини-игры: совпадают с MiniGameId из MiniGameConfigSO
    // Используется в QuestObjectiveData.EventId и при вызове ReportEvent()
}
```

Строки нигде не литеральны — только через эти константы. Это единственная защита от тихих опечаток.

### QuestObjectiveDefinition ([Serializable] data class)

```csharp
[Serializable]
public class QuestObjectiveDefinition
{
    public QuestObjectiveType Type;
    public string DisplayText;
    public string EventId;       // только через QuestEventId.*
    public int RequiredCount;    // 1 для VisitLocation/PlayMiniGame, N для FindItem
}
```

> **Видимость целей:** Все квесты дня видны одновременно. Каждый квест отображает только первый незавершённый objective через `GetCurrentObjective()`. Когда objective выполнен — квест обновляет отображаемый текст на следующий. Когда все objectives выполнены — квест завершается.

### QuestDefinition (pure C# DTO — аналог HotspotData)

```csharp
public class QuestDefinition
{
    public string GetDisplayName()                 { ... }
    public QuestObjectiveDefinition[] GetObjectives()  { ... }
    
    public QuestDefinition(string displayName, QuestObjectiveDefinition[] objectivesArray)
    {
        // Порядок внутри одного квеста всегда определяется порядком objectivesArray[].
    }
}
```

`QuestDefinitionSO` — это только контейнер:
```csharp
public class QuestDefinitionSO : ScriptableObject
{
    // поля совпадают с QuestDefinition
    public QuestDefinition ToDefinition() => new QuestDefinition(...);
}
```

### QuestObjectiveInstance

```csharp
public class QuestObjectiveInstance
{
    public QuestObjectiveDefinition GetDefinition() { ... }
    public int GetCurrentCount()  { ... }
    public bool IsCompleted()  { ... }

    public QuestObjectiveInstance(QuestObjectiveDefinition definition)
    {
        // RequiredCount >= 1 — защита в конструкторе
    }

    // Возвращает true если прогресс был засчитан
    public bool TryReport(string eventId)
    {
        if (IsCompleted() || eventId != GetDefinition().EventId) return false;
        // Инкрементирует internal счётчик
        return true;
    }
}
```

### QuestInstance

```csharp
public class QuestInstance
{
    public QuestDefinition GetDefinition()     { ... }
    public QuestObjectiveInstance[] GetObjectives() { ... }
    public bool IsCompleted() { ... }

    // Первый незавершённый objective — то что показывает UI
    public QuestObjectiveInstance GetCurrentObjective() =>
        GetObjectives().FirstOrDefault(o => !o.IsCompleted());

    // TryReport форвардит во все невыполненные объективы,
    // возвращает тот что был прогрессирован (или null)
    public QuestObjectiveInstance TryReport(string eventId) { ... }
}
```

### QuestSession

```csharp
// Результат одного вызова ReportEvent — явный именованный тип вместо tuple.
// Struct: живёт на стеке, zero-allocation, immutable.
public readonly struct QuestEventResult
{
    public static readonly QuestEventResult None = default;

    public readonly QuestInstance          Quest;
    public readonly QuestObjectiveInstance Objective;
    public bool IsEmpty => Quest == null;

    public QuestEventResult(QuestInstance quest, QuestObjectiveInstance objective)
    {
        Quest     = quest;
        Objective = objective;
    }
}

public class QuestSession
{
    private readonly List<QuestInstance> _quests;

    public IReadOnlyList<QuestInstance> GetVisibleQuests() { ... } // возвращает ВСЕ квесты
    public bool AreAllCompleted() => _quests.All(q => q.IsCompleted());

    // Возвращает populated result если прогресс был засчитан на ЛЮБОЙ стадии, иначе QuestEventResult.None
    public QuestEventResult ReportEvent(string eventId)
    {
        foreach (var quest in _quests)
        {
            var objective = quest.TryReport(eventId);
            if (objective == null) continue;
            return new QuestEventResult(quest, objective);
        }
        return QuestEventResult.None;
    }

    public QuestSessionSnapshot GetSnapshot();
    public void LoadSnapshot(QuestSessionSnapshot snapshot);
}
```

Все квесты видны одновременно через `GetVisibleQuests()`. Текущий objective каждого квеста определяется `GetCurrentObjective()`.

### QuestSessionSnapshot ([Serializable])

```csharp
[Serializable]
public class QuestSessionSnapshot
{
    public QuestObjectiveProgress[] ObjectiveProgress;
}

[Serializable]
public class QuestObjectiveProgress
{
    public int QuestIndex;
    public int ObjectiveIndex;
    public int CurrentCount;
}
```

Квесты идентифицируются по индексу в `DayConfigSO.Quests[]` — не по SO-ссылке.

**Важно (Ticket 2.4.1):** `DayId` больше НЕ хранится в `QuestSessionSnapshot`, и `VisibleCount` тоже удален. Вместо этого используется `QuestServiceState`:

```csharp
[Serializable]
public class QuestServiceState
{
    public QuestSessionSnapshot Snapshot;
    public string DayId;
}
```

Это разделение необходимо потому что `QuestSessionSnapshot` используется как для сохранения, так и для внутреннего state, но `DayId` требуется только на уровне сервиса для разрешения квестов при загрузке.

---

## 3. Конфиги (ScriptableObjects)

### QuestDefinitionSO

```csharp
public class QuestDefinitionSO : ScriptableObject
{
    public string DisplayName;
    public QuestObjectiveDefinition[] Objectives;

    public QuestDefinition ToDefinition() => new QuestDefinition(DisplayName, Objectives);

    private void OnValidate()
    {
        // Проверка: Objectives.Length > 0, RequiredCount >= 1
    }
}
```

### DayConfigSO

```csharp
public class DayConfigSO : ScriptableObject
{
    public string DayId;
    public QuestDefinitionSO[] Quests;
    public string ReturnScript;  // Naninovel — строка неизбежна
    public string ReturnLabel;

    private void OnValidate()
    {
        // Проверка: Quests.Length > 0, ReturnScript не пустой
    }
}
```

### QuestConfigSO (регистрируется в GameConfig)

```csharp
[EditInProjectSettings]
public class QuestConfigSO : Configuration, IQuestRepository
{
    public DayConfigSO[] Days;

    public QuestDefinition[] GetQuestsOfDay(string dayId)
    {
        var dayConfig = Days.FirstOrDefault(d => d.DayId == dayId)
            ?? throw new ArgumentException($"DayConfig not found: {dayId}");
        return dayConfig.Quests.Select(q => q.ToDefinition()).ToArray();
    }
}

public interface IQuestRepository
{
    QuestDefinition[] GetQuestsOfDay(string dayId);
}
```

`GameConfig` получает поле:
```csharp
public QuestConfigSO QuestConfig;
```

---

## 4. Сервисы

### QuestService (изменено в Ticket 2.4.1)

```csharp
[InitializeAtRuntime]
public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
{
    public QuestService(GameConfig gameConfig) { }

    // Вызывается из DaySessionOrchestrator (не из команды напрямую)
    // Разрешает квесты по dayId самостоятельно через _questRepository
    public void ActivateDaySession(string dayId);

    // IQuestStatusSource
    public event Func<UniTask> OnAllQuestsCompleted;

    // Query — используется QuestPanelViewModel и HotspotValidator
    public bool IsQuestCompleted(string questId);
    public IReadOnlyList<QuestInstance> GetVisibleQuests();

    // Доменные события (sync) — слушают QuestPanelViewModel и QuestSoundObserver
    public event Action<QuestInstance>                              OnQuestAdded;
    public event Action<QuestInstance, QuestObjectiveInstance>      OnQuestObjectiveTicked;
    public event Action<QuestInstance>                              OnQuestCompleted;

    // Debug only
    public void ForceComplete();

    // IStatefulService
    public void SaveServiceState(GameStateMap stateMap);
    public UniTask LoadServiceState(GameStateMap stateMap);
}
```

**Изменения:**
- `ActivateDaySession(string dayId)` — теперь берёт `dayId` и разрешает квесты сам через `_questRepository.GetQuestsOfDay(dayId)` (как `LocationService`)
- `IsQuestCompleted(string questId)` — берёт ID квеста, не SO

`OnAllQuestsCompleted` — `event Func<UniTask>` (async-chain для GameFlowService).
Остальные события — sync `event Action<T>` (UI и звук не требуют async).

Важно: `QuestService` **не знает** кто его слушает. Он файрит события и всё.

### QuestProgressService

```csharp
[InitializeAtRuntime]
public class QuestProgressService : IEngineService, IQuestProgressReporter
{
    public QuestProgressService(QuestService questService) { }

    // IQuestProgressReporter — единственный публичный метод
    public void ReportEvent(string eventId);
    // Вызывает QuestService внутренний метод для обновления сессии,
    // QuestService затем файрит нужные события
}

public interface IQuestProgressReporter
{
    void ReportEvent(string eventId);
}
```

Разделение: `QuestService` — оркестрация и стейт. `QuestProgressService` — event sink.

### QuestProgressObserver

```csharp
[InitializeAtRuntime]
public class QuestProgressObserver : IEngineService
{
    public QuestProgressObserver(LocationService locationService, IQuestProgressReporter reporter) { }

    public UniTask InitializeService()
    {
        _locationService.OnLocationEnterStarted += OnLocationEntered;
        _locationService.OnItemPickedUp         += OnItemPickedUp;
        return UniTask.CompletedTask;
    }

    public void DestroyService()
    {
        _locationService.OnLocationEnterStarted -= OnLocationEntered;
        _locationService.OnItemPickedUp         -= OnItemPickedUp;
    }

    private void OnLocationEntered(string locationId) => _reporter.ReportEvent(locationId);
    private void OnItemPickedUp(string itemId)        => _reporter.ReportEvent(itemId);
}
```

Мини-игры репортят через `@reportQuestEvent` (см. Команды).

### QuestSoundObserver

```csharp
[InitializeAtRuntime]
public class QuestSoundObserver : IEngineService
{
    public QuestSoundObserver(QuestService questService, IAudioManager audioManager) { }

    public UniTask InitializeService()
    {
        _questService.OnQuestObjectiveTicked += OnObjectiveTicked;
        _questService.OnQuestCompleted  += OnQuestCompleted;
        _questService.OnAllQuestsCompleted += OnAllQuestsCompleted;
        return UniTask.CompletedTask;
    }

    public void DestroyService()
    {
        _questService.OnQuestObjectiveTicked    -= OnObjectiveTicked;
        _questService.OnQuestCompleted     -= OnQuestCompleted;
        _questService.OnAllQuestsCompleted -= OnAllQuestsCompleted;
    }

    private void OnObjectiveTicked(QuestInstance q, QuestObjectiveInstance o)
        => _audioManager.PlaySfx(SoundConfig.QuestTicked);

    private void OnQuestCompleted(QuestInstance q)
        => _audioManager.PlaySfx(SoundConfig.QuestCrossed);

    private UniTask OnAllQuestsCompleted()
    {
        _audioManager.PlaySfx(SoundConfig.QuestCompleted);
        return UniTask.CompletedTask;
    }
}
```

---

## 5. Координация дня — DaySessionOrchestrator

Единственная точка, которая знает что при старте дня нужно сделать два действия.
Команда `@activateDayQuests` вызывает только его.

```csharp
[InitializeAtRuntime]
public class DaySessionOrchestrator : IEngineService
{
    public DaySessionOrchestrator(
        QuestService questService,
        GameFlowService gameFlowService,
        GameConfig gameConfig) { } // GameConfig — единственный способ пробросить конфиги в Naninovel DI

    public void StartDay(string dayId)
    {
        var config = _gameConfig.QuestConfig.GetDayConfig(dayId);  // Разрешает DayConfigSO здесь
        _questService.ActivateDaySession(dayId);                   // Передаёт string, не конфиг
        _gameFlowService.SetSessionSource(new DaySessionSource(config));
    }
}
```

**Изменения (Ticket 2.4.1/2.4.3):**
- `ActivateDaySession(dayId)` теперь берёт string, не `DayConfigSO`
- `DaySessionOrchestrator` по-прежнему разрешает конфиг сам (для `DaySessionSource`)
- `QuestService` разрешает квесты самостоятельно через `IQuestRepository.GetQuestsOfDay(dayId)`

### DaySessionSource

```csharp
public class DaySessionSource : IFreeRoamSessionSource
{
    public DaySessionSource(DayConfigSO config) { }

    public string GetReturnScript() => _config.ReturnScript;
    public string GetReturnLabel()  => _config.ReturnLabel;
}
```

---

## 6. GameFlowService (изменения)

`GameFlowService` остаётся чистым слушателем. Добавляется симметричный `ClearSessionSource`:

```csharp
public void SetSessionSource(IFreeRoamSessionSource source)
{
    if (_sessionSource != null && source != null)
        Debug.LogWarning("Overwriting active session source.");
    _sessionSource = source;
}

public void ClearSessionSource() => _sessionSource = null;
```

`ClearSessionSource()` вызывается при завершении free roam (возврат в нарратив).

---

## 7. Команды

| Команда | Параметры | Действие |
|---|---|---|
| `@exitNarrative` | `[locationId]`, `[returnScript]`, `[returnLabel]` | Без изменений. `returnScript` — временный |
| `@activateDayQuests` | `day:day1` | Вызывает `DaySessionOrchestrator.StartDay(dayId)` |
| `@reportQuestEvent` | `id:minigame_shooting` | Вызывает `IQuestProgressReporter.ReportEvent(id)` — для мини-игр |

`@addQuest` убран — динамическое добавление квестов пока не нужно.

---

## 8. UI — MVVM паттерн

Принцип: **никто не строит View снаружи**. View существует в сцене как часть префаба (CustomUI).
View сама получает свою ViewModel и подписывается на её события.
ViewModel не знает о View — она только предоставляет данные и события.

### QuestEntryViewModel (pure C# class)

```csharp
public class QuestEntryViewModel
{
    public string DisplayText { get; }

    // Текущий видимый объектив (для простоты — первый незавершённый)
    public string ObjectiveText    { get; private set; }
    public int    CurrentCount     { get; private set; }
    public int    RequiredCount    { get; private set; }
    public bool   IsCompleted      { get; private set; }

    public event Action OnProgressChanged;  // count изменился
    public event Action OnCompleted;        // весь квест завершён

    public QuestEntryViewModel(QuestInstance instance) { ... }

    // Вызывается QuestPanelViewModel при получении событий от QuestService
    internal void NotifyProgress(QuestObjectiveInstance objective) { ... }
    internal void NotifyCompleted() { ... }
}
```

### QuestPanelViewModel

```csharp
[InitializeAtRuntime]
public class QuestPanelViewModel : IEngineService
{
    public QuestPanelViewModel(QuestService questService) { }

    // View читает это при инициализации (load-time reconstruction)
    public IReadOnlyList<QuestEntryViewModel> ActiveQuests { get; }

    public event Action<QuestEntryViewModel> OnQuestAdded;
    public event Action<QuestEntryViewModel> OnQuestRemoved;
    public event Action                      OnAllQuestsCompleted;
    public event Action                      OnSessionActivated;  // панель становится видимой

    public UniTask InitializeService()
    {
        _questService.OnQuestAdded         += HandleQuestAdded;
        _questService.OnQuestObjectiveTicked    += HandleObjectiveTicked;
        _questService.OnQuestCompleted     += HandleQuestCompleted;
        _questService.OnAllQuestsCompleted += HandleAllCompleted;
        return UniTask.CompletedTask;
    }
}
```

### QuestPanelUI (CustomUI — существует в сцене, не создаётся кодом)

```csharp
public class QuestPanelUI : CustomUI
{
    [SerializeField] private Transform _entriesContainer;
    [SerializeField] private QuestEntryView _entryPrefab;

    private QuestPanelViewModel _viewModel;
    private Dictionary<QuestEntryViewModel, QuestEntryView> _entries = new();

    protected override void Awake()
    {
        base.Awake();
        // View сама получает свою ViewModel — никто её не "строит" снаружи
        _viewModel = Engine.GetService<QuestPanelViewModel>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _viewModel.OnSessionActivated    += Show;
        _viewModel.OnQuestAdded          += AddEntry;
        _viewModel.OnQuestRemoved        += RemoveEntry;
        _viewModel.OnAllQuestsCompleted  += Hide;

        // Восстановление состояния при загрузке сохранения
        foreach (var vm in _viewModel.ActiveQuests)
            AddEntry(vm);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        _viewModel.OnSessionActivated   -= Show;
        _viewModel.OnQuestAdded         -= AddEntry;
        _viewModel.OnQuestRemoved       -= RemoveEntry;
        _viewModel.OnAllQuestsCompleted -= Hide;
    }

    private void AddEntry(QuestEntryViewModel vm)
    {
        var entry = Instantiate(_entryPrefab, _entriesContainer);
        entry.Bind(vm);  // View сама настраивает себя через ViewModel
        _entries[vm] = entry;
    }

    private void RemoveEntry(QuestEntryViewModel vm)
    {
        if (_entries.TryGetValue(vm, out var entry))
        {
            _entries.Remove(vm);
            entry.PlayCompletionAnimation(onComplete: () => Destroy(entry.gameObject));
        }
    }
}
```

### QuestEntryView (MonoBehaviour на префабе)

```csharp
public class QuestEntryView : MonoBehaviour
{
    // Все ссылки на компоненты — через [SerializeField], кэшируются здесь
    [SerializeField] private TMP_Text _displayText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Animator _animator;

    private QuestEntryViewModel _viewModel;

    // Вызывается QuestPanelUI сразу после Instantiate
    public void Bind(QuestEntryViewModel viewModel)
    {
        _viewModel = viewModel;
        _displayText.text = viewModel.DisplayText;
        UpdateProgress();

        _viewModel.OnProgressChanged += UpdateProgress;
        _viewModel.OnCompleted       += OnCompleted;
    }

    private void OnDestroy()
    {
        if (_viewModel == null) return;
        _viewModel.OnProgressChanged -= UpdateProgress;
        _viewModel.OnCompleted       -= OnCompleted;
    }

    private void UpdateProgress()
    {
        _progressText.text = _viewModel.RequiredCount > 1
            ? $"{_viewModel.ObjectiveText} ({_viewModel.CurrentCount}/{_viewModel.RequiredCount})"
            : _viewModel.ObjectiveText;
    }

    private void OnCompleted() => _animator.SetTrigger("Complete");

    // Callback вызывается анимацией через AnimationEvent или вручную
    public void PlayCompletionAnimation(Action onComplete) { ... }
}
```

Всё визуальное (шрифт, цвет, анимации, layout) — в префабе. Код не трогает `transform.localScale`, `Color`, шейдеры.

---

## 9. HotspotValidator — архитектура и слой

`HotspotValidator` живёт в **Infrastructure**, не в Domain. Domain содержит только `IHotspotValidator` как контракт.

**Причина:** валидатор зависит на `IQuestStatusSource` (Infrastructure) — Domain не может знать об инфраструктурных зависимостях.

**Граница DataAccess → Domain:** `HotspotEntry` хранит `QuestDefinitionSO _questCondition` в Inspector (type-safe drag-drop, без строк). При конвертации в `HotspotData` SO конвертируется в `string` через `_questCondition.name`. `HotspotData` (Domain) хранит только `string ConditionValue` — никаких Unity-типов.

**`IQuestStatusSource.IsQuestCompleted`** принимает `string questId` — runtime-проверка по ID. SO используется только в Editor/Inspector.

```csharp
// Infrastructure/HotspotValidator.cs
public class HotspotValidator : IHotspotValidator
{
    private readonly IQuestStatusSource _questSource;

    public HotspotValidator(IQuestStatusSource questSource) =>
        _questSource = questSource;

    public bool IsAvailable(HotspotData hotspotData)
    {
        return hotspotData.Condition switch
        {
            ActivationCondition.Always          => true,
            ActivationCondition.RequiresQuestId => _questSource?.IsQuestCompleted(hotspotData.ConditionValue) ?? true,
            ActivationCondition.RequiresFlag    => false, // TODO: flag system
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
```

### Квестовые предметы (point-and-click)

ГДД: *"квестовые предметы спаунятся на локации только когда игрок получает квест"*.

**Решение: `ActivationCondition.Always` + `HotspotLogic.TryConsume`.**

Квестовые предметы не используют `RequiresQuestId` — они не блокируются по состоянию квеста. Вместо этого:
- Предмет конфигурируется в `DayConfigSO` / `LocationConfigSO` как обычный `HotspotType.Item` с `condition = Always`.
- Виден всё время пока активен free roam сегмент (квест уже выдан к этому моменту по определению).
- При клике: `HotspotLogic.TryConsume(id)` → `LocationService.OnItemPickedUp` → `QuestProgressObserver.ReportEvent(itemId)`.
- Consume делает предмет невидимым навсегда — повторное взаимодействие невозможно.

`RequiresQuestId` остаётся для хотспотов которые открываются **после** выполнения квеста (мини-игры, переходы, открывающиеся по сюжету). `IsQuestActive` в `IQuestStatusSource` не нужен.

---

## 10. Возврат в нарратив — полный флоу

```
@activateDayQuests day:day1
  → DaySessionOrchestrator.StartDay("day1")
      → var config = _gameConfig.QuestConfig.GetDayConfig("day1")
      → QuestService.ActivateDaySession("day1")         — разрешает квесты сам через GetQuestsOfDay()
      → GameFlowService.SetSessionSource(DaySessionSource)

@exitNarrative backyard returnScript:day_01 returnLabel:after_roam
  → HideAllActors, LocationService.Enter, SetFreeRoamMode(true), scriptPlayer.Stop()

  ; --- игрок в free roam ---
  ; LocationService.OnLocationEnterStarted("backyard")
      → QuestProgressObserver → IQuestProgressReporter.ReportEvent("backyard")
          → QuestService → QuestSession.ReportEvent("backyard")
              → QuestInstance.TryReport → QuestObjectiveInstance.TryReport
                  → OnQuestObjectiveTicked(quest, objective)  → QuestPanelViewModel → QuestEntryViewModel.NotifyProgress
                                                         → QuestSoundObserver → quest_ticked
              → если квест завершён: OnQuestCompleted(quest)
                  → QuestPanelViewModel → QuestEntryViewModel.NotifyCompleted → QuestPanelUI.RemoveEntry
                  → QuestSoundObserver → quest_crossed
              → если все квесты завершены:
                  → OnAllQuestsCompleted (Func<UniTask>)
                      → GameFlowService.LaunchNarrativeAsync(returnScript, returnLabel)
                          → LocationService.SetFreeRoamMode(false)
                          → GameFlowService.ClearSessionSource()
                          → scriptPlayer.LoadAndPlayAtLabel(returnScript, returnLabel)
                      → QuestPanelViewModel → OnAllQuestsCompleted → QuestPanelUI.Hide()
                      → QuestSoundObserver → quest_completed
```

---

## 11. Save/Load (изменено в Ticket 2.4.1)

`QuestService.SaveServiceState` сохраняет `QuestServiceState` (который содержит `Snapshot` + `DayId`) в `GameStateMap`.
`QuestService.LoadServiceState` восстанавливает `QuestSession` из `QuestServiceState`:
1. Вызывает `ActivateDaySession(state.DayId)` — разрешает квесты
2. Вызывает `_session.LoadSnapshot(state.Snapshot)` — восстанавливает прогресс

`QuestSessionSnapshot` теперь содержит только `ObjectiveProgress` — это упрощает контракт между сессией и сервисом.

`QuestPanelUI.OnEnable` проходит по `QuestPanelViewModel.ActiveQuests` и восстанавливает UI —
это покрывает сценарий загрузки сохранения во время free roam.

`IFreeRoamSessionSource` (точка возврата) — пока не персистится (приемлемо для demo).
В Epic 3: добавить `GameFlowServiceState` с `ReturnScript`/`ReturnLabel`.

---

## 12. Тестирование — QuestDebugPanel

**AC тестирование** для `QuestService` покрывается через `QuestDebugPanel` — Editor window.
Расположение: `Assets/_Project/Scripts/Tests/Editor/QuestDebugPanel.cs`.
Доступ в Editor: `Window → OnlyFarms → Quest Debug Panel`.

---

## 13. Граф зависимостей

```
.nani скрипты
  @activateDayQuests  →  DaySessionOrchestrator.StartDay()
  @reportQuestEvent   →  IQuestProgressReporter.ReportEvent()

QuestProgressObserver
  ← LocationService events
  → IQuestProgressReporter

QuestProgressService : IQuestProgressReporter
  → QuestService (internal progress update)

QuestService (файрит события, ни о ком не знает)
  → OnQuestAdded, OnQuestObjectiveTicked, OnQuestCompleted  ← QuestPanelViewModel
                                                        ← QuestSoundObserver
  → OnAllQuestsCompleted (Func<UniTask>)               ← GameFlowService

QuestPanelViewModel (файрит события, не знает о View)
  ← QuestService
  → OnQuestAdded, OnQuestRemoved, OnAllQuestsCompleted ← QuestPanelUI (view сама подписывается)

QuestPanelUI (CustomUI — существует в сцене)
  → получает QuestPanelViewModel сама через Engine.GetService<>()
  → Instantiate(entryPrefab) → entry.Bind(viewModel)

QuestEntryView (MonoBehaviour на префабе)
  → получает QuestEntryViewModel через Bind()
  → подписывается на события VM, обновляет UI

GameFlowService (слушатель)
  ← IQuestStatusSource.OnAllQuestsCompleted
  → LaunchNarrativeAsync()

HotspotValidator (Infrastructure)
  → IQuestStatusSource.IsQuestCompleted(string questId)
```

---

## 14. Что НЕ делаем в квестах

- Строковые ID квестов в рантайме — только `QuestEventId.*` константы
- `QuestDefinitionSO` в доменных классах — только `QuestDefinition` DTO
- Команды знают больше чем об одном сервисе — только через оркестратор
- Менеджер/сервис строит или конфигурирует View через код — только `Bind(viewModel)`
- `QuestService` знает о UI, звуке или GameFlow — только события наружу
