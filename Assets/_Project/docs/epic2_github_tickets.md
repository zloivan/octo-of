# Epic 2: Система квестов
## GitHub Projects — Tickets

> Источник: `architecture_quests.md`, `architecture_core.md`, `mechanics_gdd.md`
> Порядок: outside-in (интерфейсы/API → домен → конфиги → сервисы → команды → UI → интеграция)
> Зависимость эпика: Epic 1 (LocationService с событиями `OnLocationEnterStarted`, `OnItemPickedUp`)

---

## Метки (Labels)

| Label | Назначение |
|---|---|
| `feature` | Новая функциональность |
| `config` | ScriptableObject / data assets |
| `domain` | Pure C# доменные классы без Unity-зависимостей |
| `service` | Сервисный слой / доменная логика |
| `command` | Naninovel кастомная команда |
| `ui` | UI компоненты (MonoBehaviour, CustomUI) |
| `integration` | Связывание систем |
| `qa-tooling` | QA debug инструменты |

---

## Assembly Definitions

Все классы модуля размещаются в:

```
Assets/Scripts/Quests/
  OnlyFarms.Quests.asmdef
    References:
      - Naninovel.Runtime
      - OnlyFarms.Locations       (для LocationService в QuestProgressObserver)
      - UniTask (embedded)
```

`QuestSession`, `QuestInstance`, `QuestObjectiveInstance` — plain C# классы без Unity-зависимостей внутри той же assembly. При необходимости unit-тестирования — отдельная `OnlyFarms.Quests.Tests.asmdef` со ссылкой на основную.

---

---

## 🏁 Milestone: Day 1 — Abstractions, Stubs & Domain

> Снаружи во внутрь: интерфейсы → заглушки → домен.
> После этого milestone все `[InitializeAtRuntime]`-сервисы находят свои зависимости в конструкторах — движок стартует без исключений, параллельная разработка разблокирована, весь доменный слой покрыт юнит-тестами.

---

### Ticket 2.1.1 — [FEATURE] QuestEventId + Interfaces (IQuestStatusSource, IQuestProgressReporter)

**Labels:** `feature`, `domain`
**Estimate:** 1.5h
**Milestone:** Day 1 — Abstractions, Stubs & Domain

#### Описание

Создать единственный источник строковых констант и публичные интерфейсы сервисного слоя. Это первое, что нужно создать — всё остальное будет зависеть от этих типов.

**Принцип:** строки для quest event ID нигде не пишутся вручную — только через `QuestEventId.*`. Это единственная защита от тихих опечаток.

---

**Файл:** `Assets/Scripts/Quests/Domain/QuestEventId.cs`
```csharp
public static class QuestEventId
{
    // Заполняется по мере добавления локаций/предметов/мини-игр в конфиге
    // Имена совпадают с: LocationId из LocationConfigSO,
    //                    HotspotId из HotspotEntry,
    //                    MiniGameId из MiniGameConfigSO
    // Пример: public const string Barn = "barn";
    //         public const string CarrotItem = "carrot_item";
    //         public const string ShootingGame = "shooting_game";
}
```

**Файл:** `Assets/Scripts/Quests/IQuestStatusSource.cs`
```csharp
public interface IQuestStatusSource
{
    // Query-интерфейс: HotspotValidator и UI читают состояние квестов через него.
    // Только QuestService реализует этот интерфейс.
    bool IsQuestCompleted(QuestDefinitionSO quest);
    IReadOnlyList<QuestInstance> GetVisibleQuests();

    // Доменные события (sync) — слушают QuestPanelViewModel и QuestSoundObserver
    event Action<QuestInstance>                           OnQuestAdded;
    event Action<QuestInstance, QuestObjectiveInstance>   OnObjectiveTicked;
    event Action<QuestInstance>                           OnQuestCompleted;

    // Async-chain: GameFlowService возобновляет нарратив по завершении всех квестов
    event Func<UniTask> OnAllQuestsCompleted;
}
```

**Файл:** `Assets/Scripts/Quests/IQuestProgressReporter.cs`
```csharp
public interface IQuestProgressReporter
{
    // Единственная точка репорта прогресса.
    // Вызывается QuestProgressObserver (авто) и ReportQuestEventCommand (мини-игры).
    void ReportEvent(string eventId);
}
```

**Файл:** `Assets/Scripts/Quests/IQuestRepository.cs`
```csharp
public interface IQuestRepository
{
    DayConfigSO GetDayConfig(string dayId);
}
```

> `IQuestStatusSource` — для чтения (HotspotValidator, QuestPanelViewModel).
> `IQuestProgressReporter` — для записи (QuestProgressObserver, команды).
> Разделение по принципу ISP: слушатели не видят метод `ReportEvent`, репортёры не видят события.

---

#### Acceptance Criteria
- [ ] `QuestEventId` — static class, нет Unity-зависимостей.
- [ ] `IQuestStatusSource` компилируется (форвард-декларации типов не нужны — типы появятся в Ticket 2.2.x).
- [ ] `IQuestProgressReporter` и `IQuestRepository` — чистые C# интерфейсы.
- [ ] Все файлы размещены в `Assets/Scripts/Quests/`.

---

### Ticket 2.1.2 — [FEATURE] QuestService Stub + QuestProgressService Stub

**Labels:** `feature`, `service`
**Estimate:** 2h
**Milestone:** Day 1 — Abstractions, Stubs & Domain
**Depends on:** Ticket 2.1.1

#### Описание

Создать заглушки сервисов, которые реализуют нужные интерфейсы и регистрируются в Naninovel DI. Реализация внутри — `throw new NotImplementedException()` или no-op.

Цель: после этого тикета движок стартует, DI граф разрешается, и зависимые сервисы (GameFlowService, HotspotValidator) могут начать принимать `IQuestStatusSource` в конструктор — не блокируя параллельную разработку.

---

**Файл:** `Assets/Scripts/Quests/QuestService.cs`
```csharp
[InitializeAtRuntime]
public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
{
    public event Action<QuestInstance>                          OnQuestAdded;
    public event Action<QuestInstance, QuestObjectiveInstance>  OnObjectiveTicked;
    public event Action<QuestInstance>                          OnQuestCompleted;
    public event Func<UniTask>                                  OnAllQuestsCompleted;

    public QuestService(GameConfig gameConfig) { }

    // --- IQuestStatusSource ---
    public bool IsQuestCompleted(QuestDefinitionSO quest)           => false; // stub
    public IReadOnlyList<QuestInstance> GetVisibleQuests()          => Array.Empty<QuestInstance>(); // stub

    // --- Activation (вызывается DaySessionOrchestrator) ---
    public void ActivateDaySession(DayConfigSO dayConfig)           { } // stub

    // --- Debug ---
    public void ForceComplete()                                     { } // stub

    // --- IStatefulService ---
    public UniTask InitializeService()  => UniTask.CompletedTask;
    public void DestroyService()        { }
    public void ResetService()          { }
    public void SaveServiceState(GameStateMap stateMap)             { } // stub
    public UniTask LoadServiceState(GameStateMap stateMap)          => UniTask.CompletedTask; // stub
}
```

**Файл:** `Assets/Scripts/Quests/QuestProgressService.cs`
```csharp
[InitializeAtRuntime]
public class QuestProgressService : IEngineService, IQuestProgressReporter
{
    public QuestProgressService(QuestService questService) { }

    public void ReportEvent(string eventId) { } // stub

    public UniTask InitializeService()  => UniTask.CompletedTask;
    public void DestroyService()        { }
    public void ResetService()          { }
}
```

---

#### Acceptance Criteria
- [ ] Проект компилируется без ошибок.
- [ ] `Engine.GetService<QuestService>()` возвращает экземпляр (не null) в play mode.
- [ ] `Engine.GetService<QuestProgressService>()` возвращает экземпляр.
- [ ] Naninovel DI не выбрасывает исключений при старте.

---

### Ticket 2.1.3 — [FEATURE] DaySessionOrchestrator Stub + ActivateDayQuestsCommand + ReportQuestEventCommand

**Labels:** `feature`, `service`, `command`
**Estimate:** 2h
**Milestone:** Day 1 — Abstractions, Stubs & Domain
**Depends on:** Ticket 2.1.2

#### Описание

Создать оркестратор дня (stub) и обе команды. После этого тикета `.nani` скрипты могут вызывать `@activateDayQuests` и `@reportQuestEvent` — система принимает вызовы без паники, просто пока ничего не делает.

---

**Файл:** `Assets/Scripts/Quests/DaySessionOrchestrator.cs`
```csharp
[InitializeAtRuntime]
public class DaySessionOrchestrator : IEngineService
{
    private readonly QuestService _questService;
    private readonly GameFlowService _gameFlowService;
    private readonly IQuestRepository _questRepository;

    public DaySessionOrchestrator(
        QuestService questService,
        GameFlowService gameFlowService,
        IQuestRepository questRepository)
    {
        _questService      = questService;
        _gameFlowService   = gameFlowService;
        _questRepository   = questRepository;
    }

    public void StartDay(string dayId) { } // stub — полная реализация в Ticket 2.4.1

    public UniTask InitializeService()  => UniTask.CompletedTask;
    public void DestroyService()        { }
    public void ResetService()          { }
}
```

**Файл:** `Assets/Scripts/Quests/Commands/ActivateDayQuestsCommand.cs`
```csharp
[CommandAlias("activateDayQuests")]
public class ActivateDayQuestsCommand : Command
{
    [RequiredParameter]
    public StringParameter Day;

    public override UniTask Execute(AsyncToken asyncToken = default)
    {
        Engine.GetService<DaySessionOrchestrator>().StartDay(Day);
        return UniTask.CompletedTask;
    }
}
```

**Файл:** `Assets/Scripts/Quests/Commands/ReportQuestEventCommand.cs`
```csharp
[CommandAlias("reportQuestEvent")]
public class ReportQuestEventCommand : Command
{
    [RequiredParameter]
    public StringParameter Id;

    public override UniTask Execute(AsyncToken asyncToken = default)
    {
        Engine.GetService<IQuestProgressReporter>().ReportEvent(Id);
        return UniTask.CompletedTask;
    }
}
```

---

#### Пример использования в `.nani` скрипте

```nani
; Начало дня — активируем квесты и переходим во free roam
@activateDayQuests day:day1
@exitNarrative locationId:farm_yard

; По завершению мини-игры
@reportQuestEvent id:shooting_game
```

---

#### Acceptance Criteria
- [ ] `@activateDayQuests day:day1` выполняется без ошибок (stub, ничего не делает).
- [ ] `@reportQuestEvent id:test` выполняется без ошибок.
- [ ] `DaySessionOrchestrator` регистрируется в DI и получает все зависимости.
- [ ] Команды находятся в `Assets/Scripts/Quests/Commands/`.

---

---

### ─── Domain Layer ───

---

### Ticket 2.2.1 — [FEATURE] QuestObjectiveType + QuestObjectiveDefinition + QuestDefinition

**Labels:** `feature`, `domain`
**Estimate:** 1.5h
**Milestone:** Day 1 — Abstractions, Stubs & Domain

#### Описание

Создать типы данных доменного слоя — аналог `HotspotData` из системы локаций. `[Serializable]` нужен только `QuestObjectiveDefinition` (хранится в SO). `QuestDefinition` — чистый DTO.

---

**Файл:** `Assets/Scripts/Quests/Domain/QuestObjectiveType.cs`
```csharp
public enum QuestObjectiveType
{
    VisitLocation,  // автоматически при OnLocationEnterStarted
    FindItem,       // counter: N раз подобрать предмет
    PlayMiniGame    // автоматически после завершения мини-игры
}
```

**Файл:** `Assets/Scripts/Quests/Domain/QuestObjectiveDefinition.cs`
```csharp
[Serializable]
public class QuestObjectiveDefinition
{
    public QuestObjectiveType Type;
    public string DisplayText;
    public string EventId;        // значение только через QuestEventId.*
    public int RequiredCount;     // 1 для VisitLocation/PlayMiniGame, N для FindItem
}
```

**Файл:** `Assets/Scripts/Quests/Domain/QuestDefinition.cs`
```csharp
public class QuestDefinition
{
    public string DisplayText                    { get; }
    public QuestObjectiveDefinition[] Objectives { get; }
    public bool IsSequential                     { get; }

    public QuestDefinition(
        string displayText,
        QuestObjectiveDefinition[] objectives,
        bool isSequential)
    {
        DisplayText  = displayText;
        Objectives   = objectives;
        IsSequential = isSequential;
    }
}
```

> `[Serializable]` — только на `QuestObjectiveDefinition` (она хранится в SO через Inspector).
> `QuestDefinition` — иммутабельный DTO, создаётся через `QuestDefinitionSO.ToDefinition()`.

---

#### Acceptance Criteria
- [ ] Все три файла — pure C# (нет `using UnityEngine`).
- [ ] `QuestObjectiveDefinition` сериализуется Inspector-ом внутри SO-массива.
- [ ] `QuestDefinition` иммутабелен (только `{ get; }` свойства).

---

### Ticket 2.2.2 — [FEATURE] QuestObjectiveInstance + QuestInstance

**Labels:** `feature`, `domain`
**Estimate:** 2h
**Milestone:** Day 1 — Abstractions, Stubs & Domain
**Depends on:** Ticket 2.2.1

#### Описание

Runtime-объекты квеста. `QuestObjectiveInstance.TryReport` — центральная логика матчинга событий.

---

**Файл:** `Assets/Scripts/Quests/Domain/QuestObjectiveInstance.cs`
```csharp
public class QuestObjectiveInstance
{
    public QuestObjectiveDefinition Definition { get; }
    public int  CurrentCount { get; private set; }
    public bool IsCompleted  => CurrentCount >= Definition.RequiredCount;

    public QuestObjectiveInstance(QuestObjectiveDefinition definition)
    {
        if (definition.RequiredCount < 1)
            throw new ArgumentException("RequiredCount must be >= 1");
        Definition = definition;
    }

    // Возвращает true если счётчик увеличился
    public bool TryReport(string eventId)
    {
        if (IsCompleted || eventId != Definition.EventId) return false;
        CurrentCount++;
        return true;
    }
}
```

**Файл:** `Assets/Scripts/Quests/Domain/QuestInstance.cs`
```csharp
public class QuestInstance
{
    public QuestDefinition           Definition  { get; }
    public QuestObjectiveInstance[]  Objectives  { get; }
    public bool IsCompleted => Objectives.All(o => o.IsCompleted);

    public QuestInstance(QuestDefinition definition)
    {
        Definition = definition;
        Objectives = definition.Objectives
            .Select(d => new QuestObjectiveInstance(d))
            .ToArray();
    }

    // Форвардит событие во все незавершённые объективы.
    // Возвращает первый прогрессированный (или null если никто не засчитал).
    public QuestObjectiveInstance TryReport(string eventId)
    {
        foreach (var obj in Objectives)
            if (obj.TryReport(eventId)) return obj;
        return null;
    }
}
```

---

#### Acceptance Criteria
- [ ] `TryReport` возвращает `false` если объектив уже завершён.
- [ ] `TryReport` возвращает `false` если `eventId` не совпадает с `Definition.EventId`.
- [ ] `TryReport` увеличивает счётчик ровно на 1 за один вызов — не более.
- [ ] `IsCompleted` на `QuestInstance` верен только если все `Objectives` завершены.
- [ ] `RequiredCount < 1` выбрасывает исключение в конструкторе `QuestObjectiveInstance`.

---

### Ticket 2.2.3 — [FEATURE] QuestSession + QuestSessionSnapshot

**Labels:** `feature`, `domain`
**Estimate:** 2.5h
**Milestone:** Day 1 — Abstractions, Stubs & Domain
**Depends on:** Ticket 2.2.2

#### Описание

`QuestSession` — агрегат дня. Управляет sequential-видимостью квестов (`_visibleCount`). Это единственное место, где решается, виден ли следующий квест.

---

**Файл:** `Assets/Scripts/Quests/Domain/QuestEventResult.cs`
```csharp
// Явный именованный тип вместо tuple — читаемый контракт, zero-allocation (struct).
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
```

**Файл:** `Assets/Scripts/Quests/Domain/QuestSession.cs`
```csharp
public class QuestSession
{
    private readonly List<QuestInstance> _quests;
    private readonly bool _isSequential;
    private int _visibleCount;

    public IReadOnlyList<QuestInstance> AllQuests    => _quests;
    public IReadOnlyList<QuestInstance> VisibleQuests
        => _quests.Take(_visibleCount).ToList();
    public bool AllCompleted => _quests.All(q => q.IsCompleted);

    public QuestSession(IEnumerable<QuestDefinition> definitions)
    {
        _quests      = definitions.Select(d => new QuestInstance(d)).ToList();
        _isSequential = _quests.Count > 0 && _quests[0].Definition.IsSequential;
        _visibleCount = _isSequential ? (Math.Min(1, _quests.Count)) : _quests.Count;
    }

    // Возвращает populated result если прогресс засчитан, иначе QuestEventResult.None
    public QuestEventResult ReportEvent(string eventId)
    {
        foreach (var quest in VisibleQuests)
        {
            var objective = quest.TryReport(eventId);
            if (objective != null)
            {
                if (quest.IsCompleted) AdvanceVisibility();
                return new QuestEventResult(quest, objective);
            }
        }
        return QuestEventResult.None;
    }

    // Раскрывает следующий квест если IsSequential
    private void AdvanceVisibility()
    {
        if (_isSequential && _visibleCount < _quests.Count)
            _visibleCount++;
    }

    public QuestSessionSnapshot GetSnapshot()
    {
        var progress = new List<QuestObjectiveProgress>();
        for (int qi = 0; qi < _quests.Count; qi++)
            for (int oi = 0; oi < _quests[qi].Objectives.Length; oi++)
                if (_quests[qi].Objectives[oi].CurrentCount > 0)
                    progress.Add(new QuestObjectiveProgress
                    {
                        QuestIndex     = qi,
                        ObjectiveIndex = oi,
                        CurrentCount   = _quests[qi].Objectives[oi].CurrentCount
                    });
        return new QuestSessionSnapshot
        {
            ObjectiveProgress = progress.ToArray(),
            VisibleCount      = _visibleCount
        };
    }

    public void LoadSnapshot(QuestSessionSnapshot snapshot)
    {
        // Guard: не применять снепшот другого дня
        if (snapshot.DayId != null && _quests.Count > 0 &&
            snapshot.ObjectiveProgress.Length > 0)
        {
            // snapshot.DayId валидируется в QuestService.LoadServiceState до вызова
        }
        foreach (var p in snapshot.ObjectiveProgress)
        {
            if (p.QuestIndex >= _quests.Count) continue;              // corrupted snapshot guard
            if (p.ObjectiveIndex >= _quests[p.QuestIndex].Objectives.Length) continue;
            var obj = _quests[p.QuestIndex].Objectives[p.ObjectiveIndex];
            for (int i = 0; i < p.CurrentCount; i++) obj.TryReport(obj.Definition.EventId);
        }
        _visibleCount = Mathf.Clamp(snapshot.VisibleCount, 0, _quests.Count);
    }
}
```

**Файл:** `Assets/Scripts/Quests/Domain/QuestSessionSnapshot.cs`
```csharp
[Serializable]
public class QuestSessionSnapshot
{
    public string                  DayId;
    public QuestObjectiveProgress[] ObjectiveProgress;
    public int                     VisibleCount;
}

[Serializable]
public class QuestObjectiveProgress
{
    public int QuestIndex;
    public int ObjectiveIndex;
    public int CurrentCount;
}
```

> Идентификация квестов — по индексу в `DayConfigSO.Quests[]`, не по SO-ссылке.
> `DayId` в снепшоте — для валидации при загрузке (не применять снепшот другого дня).

---

#### Acceptance Criteria
- [ ] При `IsSequential=true` изначально виден только первый квест.
- [ ] После завершения первого квеста `VisibleQuests` включает ровно один новый квест (не все сразу).
- [ ] `ReportEvent` возвращает `QuestEventResult.None` (`IsEmpty == true`) для невидимых квестов.
- [ ] `GetSnapshot` / `LoadSnapshot` восстанавливают `CurrentCount` и `VisibleCount`.
- [ ] Снепшот с невалидным `QuestIndex`/`ObjectiveIndex` — пропускается, не бросает исключение.
- [ ] Двойной round-trip (save→load→save) даёт идентичный снепшот.
- [ ] Снепшот другого дня (`DayId` не совпадает) — не применяется (обрабатывается в `QuestService.LoadServiceState`).

---

---

## 🏁 Milestone: Day 2 — Config & Services

> ScriptableObjects заполняют контейнеры доменных объектов. Стабы из Day 1 заменяются полной реализацией.

---

### Ticket 2.3.1 — [FEATURE] QuestDefinitionSO + DayConfigSO + QuestConfigSO

**Labels:** `feature`, `config`
**Estimate:** 2.5h
**Milestone:** Day 2 — Config & Services
**Depends on:** Ticket 2.2.1

#### Описание

Создать три SO-класса и обновить `GameConfig`. После этого тикета конфиг заполняется через Inspector и QuestService может в него обращаться.

---

**Файл:** `Assets/Scripts/Quests/Config/QuestDefinitionSO.cs`
```csharp
[CreateAssetMenu(menuName = "OnlyFarms/Quest/QuestDefinition")]
public class QuestDefinitionSO : ScriptableObject
{
    public string DisplayText;
    public QuestObjectiveDefinition[] Objectives;
    public bool IsSequential;

    public QuestDefinition ToDefinition()
        => new QuestDefinition(DisplayText, Objectives, IsSequential);

    private void OnValidate()
    {
        if (Objectives == null || Objectives.Length == 0)
            Debug.LogWarning($"{name}: Objectives is empty.", this);
        foreach (var obj in Objectives ?? Array.Empty<QuestObjectiveDefinition>())
            if (obj.RequiredCount < 1)
                Debug.LogError($"{name}: RequiredCount must be >= 1.", this);
    }
}
```

**Файл:** `Assets/Scripts/Quests/Config/DayConfigSO.cs`
```csharp
[CreateAssetMenu(menuName = "OnlyFarms/Quest/DayConfig")]
public class DayConfigSO : ScriptableObject
{
    public string DayId;
    public QuestDefinitionSO[] Quests;
    public string ReturnScript;   // Naninovel script name — строка неизбежна
    public string ReturnLabel;    // метка в скрипте для возврата

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(DayId))
            Debug.LogError($"{name}: DayId is empty.", this);
        if (Quests == null || Quests.Length == 0)
            Debug.LogWarning($"{name}: Quests is empty.", this);
        if (string.IsNullOrWhiteSpace(ReturnScript))
            Debug.LogError($"{name}: ReturnScript is empty.", this);
    }
}
```

**Файл:** `Assets/Scripts/Quests/Config/QuestConfigSO.cs`
```csharp
[EditInProjectSettings]
public class QuestConfigSO : Configuration, IQuestRepository
{
    public DayConfigSO[] Days;

    public DayConfigSO GetDayConfig(string dayId)
        => Days.FirstOrDefault(d => d.DayId == dayId)
           ?? throw new ArgumentException($"DayConfig not found: {dayId}");
}
```

**Изменение:** `Assets/Scripts/Core/GameConfig.cs`
```csharp
// Добавить поле:
public QuestConfigSO QuestConfig;   // Epic 2
```

---

### Место в проекте

```
Assets/
  Scripts/
    Core/
      GameConfig.cs          (изменён — добавлено QuestConfig)
    Quests/
      Config/
        QuestDefinitionSO.cs
        DayConfigSO.cs
        QuestConfigSO.cs
  Configs/
    Quests/
      day1/
        Day1Config.asset
        Quest_HarvestCarrots.asset
        Quest_VisitBarn.asset
```

---

#### Acceptance Criteria
- [ ] `QuestConfigSO` виден в `Naninovel → Configuration` как отдельная секция.
- [ ] `DayConfigSO.asset` создаётся через `Create → OnlyFarms → Quest → DayConfig`.
- [ ] `QuestDefinitionSO.asset` создаётся через `Create → OnlyFarms → Quest → QuestDefinition`.
- [ ] `OnValidate` выводит ошибку если `DayId` пустой или `Objectives.Length == 0`.
- [ ] `GameConfig` компилируется с добавленным `QuestConfig` полем.

---

### Ticket 2.3.2 — [FEATURE] HotspotEntry — миграция на QuestDefinitionSO

**Labels:** `feature`, `config`, `integration`
**Estimate:** 1.5h
**Milestone:** Day 2 — Config & Services
**Depends on:** Ticket 2.3.1

#### Описание

Заменить `string ConditionValue` (для `RequiresQuestId`) на прямую SO-ссылку. Это устраняет строковое связывание между системой локаций и системой квестов.

---

**Изменение:** `Assets/Scripts/Locations/HotspotEntry.cs`
```csharp
[System.Serializable]
public class HotspotEntry
{
    public string id;
    public string localizationKey;
    public AssetReferenceSprite spriteRef;
    public HotspotType type;
    public ActivationCondition condition;
    public QuestDefinitionSO questCondition;    // НОВОЕ: заменяет string conditionValue для RequiresQuestId
    public string conditionValue;               // оставить для RequiresFlag (строка флага)
    public string targetLocationId;
    public AssetReference<InteractableItemConfig> itemConfig;

    public HotspotData ToHotspotData() => new HotspotData
    {
        Id               = id,
        Type             = type,
        Condition        = condition,
        ConditionValue   = conditionValue,      // для RequiresFlag
        TargetLocationId = targetLocationId
    };
}
```

**Изменение:** `Assets/Scripts/Locations/HotspotValidator.cs`
```csharp
// Заменить строковую проверку:
// if (condition == RequiresQuestId) questService.IsQuestCompleted(hotspot.conditionValue)
// на:
// if (condition == RequiresQuestId) questService.IsQuestCompleted(hotspot.questCondition)

public class HotspotValidator
{
    private readonly IQuestStatusSource _questSource;

    public HotspotValidator(IQuestStatusSource questSource) { }

    public bool IsHotspotVisible(HotspotEntry hotspot)
    {
        return hotspot.condition switch
        {
            ActivationCondition.Always          => true,
            ActivationCondition.RequiresQuestId => _questSource.IsQuestCompleted(hotspot.questCondition),
            ActivationCondition.RequiresFlag    => CheckFlag(hotspot.conditionValue),
            _                                  => true
        };
    }
    // ...
}
```

---

#### Решение по квестовым предметам

`RequiresQuestId` проверяет что квест **выполнен**. Для квестовых предметов (point-and-click) нужна обратная логика — видны пока квест активен. `IsQuestActive` в `IQuestStatusSource` **не добавляем**.

Квестовые предметы используют `condition = Always`. Они появляются на локации потому что находятся в конфиге текущего дня — квест к этому моменту уже выдан. Исчезают через `HotspotLogic.TryConsume` при клике, не через валидатор.

`RequiresQuestId` остаётся только для хотспотов открывающихся **после** завершения квеста (мини-игры, переходы по сюжету).

#### Acceptance Criteria
- [ ] `HotspotEntry.questCondition` принимает `QuestDefinitionSO` asset в Inspector.
- [ ] `HotspotValidator.IsHotspotVisible` использует SO-ссылку для `RequiresQuestId`.
- [ ] Старое поле `conditionValue` не удалено (нужно для `RequiresFlag`).
- [ ] Проект компилируется без ошибок.

---

---

### ─── Service Implementation ───

---

### Ticket 2.4.1 — [FEATURE] QuestService — полная реализация

**Labels:** `feature`, `service`
**Estimate:** 4h
**Milestone:** Day 2 — Config & Services
**Depends on:** Ticket 2.2.3, Ticket 2.3.1

#### Описание

Заменить stub-реализацию на полную. `QuestService` управляет `QuestSession`, файрит события и реализует `IStatefulService<GameStateMap>`.

---

**Файл:** `Assets/Scripts/Quests/QuestService.cs`
```csharp
[InitializeAtRuntime]
public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
{
    public event Action<QuestInstance>                          OnQuestAdded;
    public event Action<QuestInstance, QuestObjectiveInstance>  OnObjectiveTicked;
    public event Action<QuestInstance>                          OnQuestCompleted;
    public event Func<UniTask>                                  OnAllQuestsCompleted;

    private readonly IQuestRepository _questRepository;
    private QuestSession _session;
    private DayConfigSO  _activeConfig;

    public QuestService(GameConfig gameConfig)
    {
        _questRepository = gameConfig.QuestConfig;
    }

    public void ActivateDaySession(DayConfigSO dayConfig)
    {
        _activeConfig = dayConfig;
        var definitions = dayConfig.Quests.Select(q => q.ToDefinition());
        _session = new QuestSession(definitions);

        foreach (var quest in _session.VisibleQuests)
            OnQuestAdded?.Invoke(quest);
    }

    // IQuestStatusSource
    public bool IsQuestCompleted(QuestDefinitionSO quest)
    {
        if (_session == null) return false;
        return _session.AllQuests
            .FirstOrDefault(q => q.Definition.DisplayText == quest.DisplayText)
            ?.IsCompleted ?? false;
    }

    public IReadOnlyList<QuestInstance> GetVisibleQuests()
        => _session?.VisibleQuests ?? (IReadOnlyList<QuestInstance>)Array.Empty<QuestInstance>();

    // Вызывается QuestProgressService
    internal void ReportEventInternal(string eventId)
    {
        if (_session == null) return;

        var prevVisible = _session.VisibleQuests.Count;
        var result = _session.ReportEvent(eventId);
        if (result.IsEmpty) return;

        OnObjectiveTicked?.Invoke(result.Quest, result.Objective);

        if (result.Quest.IsCompleted)
            OnQuestCompleted?.Invoke(result.Quest);

        // Раскрытие нового квеста (sequential)
        var newVisible = _session.VisibleQuests;
        for (int i = prevVisible; i < newVisible.Count; i++)
            OnQuestAdded?.Invoke(newVisible[i]);

        if (_session.AllCompleted)
            OnAllQuestsCompleted?.Invoke().Forget();
    }

    public void ForceComplete()
    {
        if (_session == null) return;
        foreach (var quest in _session.AllQuests)
            foreach (var obj in quest.Objectives)
                while (!obj.IsCompleted) obj.TryReport(obj.Definition.EventId);
        foreach (var quest in _session.AllQuests)
            OnQuestCompleted?.Invoke(quest);
        if (_session.AllCompleted)
            OnAllQuestsCompleted?.Invoke().Forget();
    }

    // IStatefulService
    public UniTask InitializeService()  => UniTask.CompletedTask;
    public void DestroyService()        { }
    public void ResetService()          { _session = null; _activeConfig = null; }

    public void SaveServiceState(GameStateMap stateMap)
    {
        if (_session == null || _activeConfig == null) return;
        var snapshot = _session.GetSnapshot();
        snapshot.DayId = _activeConfig.DayId;
        stateMap.SetState(new QuestServiceState { Snapshot = snapshot });
    }

    public UniTask LoadServiceState(GameStateMap stateMap)
    {
        var state = stateMap.GetState<QuestServiceState>();
        if (state?.Snapshot == null) return UniTask.CompletedTask;

        var config = _questRepository.GetDayConfig(state.Snapshot.DayId);
        ActivateDaySession(config);
        _session.LoadSnapshot(state.Snapshot);
        return UniTask.CompletedTask;
    }
}

[Serializable]
public class QuestServiceState
{
    public QuestSessionSnapshot Snapshot;
}
```

---

#### Acceptance Criteria
- [ ] `ActivateDaySession` файрит `OnQuestAdded` для всех изначально видимых квестов.
- [ ] `ReportEventInternal` файрит `OnObjectiveTicked`, затем `OnQuestCompleted` если квест завершён.
- [ ] `OnAllQuestsCompleted` файрится после завершения последнего квеста.
- [ ] При sequential-квестах: после завершения первого `OnQuestAdded` файрится для второго.
- [ ] `SaveServiceState` / `LoadServiceState` восстанавливают сессию корректно.

---

### Ticket 2.4.2 — [FEATURE] QuestProgressService + QuestProgressObserver + QuestSoundObserver

**Labels:** `feature`, `service`
**Estimate:** 3h
**Milestone:** Day 2 — Config & Services
**Depends on:** Ticket 2.4.1

#### Описание

Три observer-класса: репортёр прогресса, мост от LocationService и звуковой observer.

---

**Файл:** `Assets/Scripts/Quests/QuestProgressService.cs`
```csharp
[InitializeAtRuntime]
public class QuestProgressService : IEngineService, IQuestProgressReporter
{
    private readonly QuestService _questService;

    public QuestProgressService(QuestService questService)
    {
        _questService = questService;
    }

    public void ReportEvent(string eventId)
        => _questService.ReportEventInternal(eventId);

    public UniTask InitializeService()  => UniTask.CompletedTask;
    public void DestroyService()        { }
    public void ResetService()          { }
}
```

**Файл:** `Assets/Scripts/Quests/QuestProgressObserver.cs`
```csharp
[InitializeAtRuntime]
public class QuestProgressObserver : IEngineService
{
    private readonly LocationService      _locationService;
    private readonly IQuestProgressReporter _reporter;

    public QuestProgressObserver(LocationService locationService, IQuestProgressReporter reporter)
    {
        _locationService = locationService;
        _reporter        = reporter;
    }

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

    public void ResetService() { }

    private void OnLocationEntered(string locationId) => _reporter.ReportEvent(locationId);
    private void OnItemPickedUp(string itemId)        => _reporter.ReportEvent(itemId);
}
```

**Файл:** `Assets/Scripts/Quests/QuestSoundObserver.cs`
```csharp
[InitializeAtRuntime]
public class QuestSoundObserver : IEngineService
{
    private readonly QuestService  _questService;
    private readonly IAudioManager _audioManager;

    public QuestSoundObserver(QuestService questService, IAudioManager audioManager)
    {
        _questService  = questService;
        _audioManager  = audioManager;
    }

    public UniTask InitializeService()
    {
        _questService.OnObjectiveTicked    += OnObjectiveTicked;
        _questService.OnQuestCompleted     += OnQuestCompleted;
        _questService.OnAllQuestsCompleted += OnAllQuestsCompleted;
        return UniTask.CompletedTask;
    }

    public void DestroyService()
    {
        _questService.OnObjectiveTicked    -= OnObjectiveTicked;
        _questService.OnQuestCompleted     -= OnQuestCompleted;
        _questService.OnAllQuestsCompleted -= OnAllQuestsCompleted;
    }

    public void ResetService() { }

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

#### Acceptance Criteria
- [ ] Посещение локации автоматически репортит `eventId` = `locationId` в `QuestProgressService`.
- [ ] Подбор предмета автоматически репортит `eventId` = `itemId`.
- [ ] `QuestProgressObserver` подписывается при `InitializeService` и отписывается при `DestroyService`.
- [ ] Звуки `quest_ticked`, `quest_crossed`, `quest_completed` воспроизводятся корректно.
- [ ] `QuestSoundObserver.OnAllQuestsCompleted` возвращает `UniTask` (async-chain совместим).

---

### Ticket 2.4.3 — [FEATURE] DaySessionOrchestrator + DaySessionSource — полная реализация

**Labels:** `feature`, `service`
**Estimate:** 2h
**Milestone:** Day 2 — Config & Services
**Depends on:** Ticket 2.4.1, Ticket 2.3.1

#### Описание

Заполнить stub `DaySessionOrchestrator.StartDay` и создать `DaySessionSource`. Добавить `GameFlowService.ClearSessionSource`.

---

**Файл:** `Assets/Scripts/Quests/DaySessionOrchestrator.cs`
```csharp
public void StartDay(string dayId)
{
    var config = _questRepository.GetDayConfig(dayId);
    _questService.ActivateDaySession(config);
    _gameFlowService.SetSessionSource(new DaySessionSource(config));
}
```

**Файл:** `Assets/Scripts/Quests/DaySessionSource.cs`
```csharp
public class DaySessionSource : IFreeRoamSessionSource
{
    private readonly DayConfigSO _config;

    public DaySessionSource(DayConfigSO config)
    {
        _config = config;
    }

    public string GetReturnScript() => _config.ReturnScript;
    public string GetReturnLabel()  => _config.ReturnLabel;
}
```

**Изменение:** `Assets/Scripts/Locations/GameFlowService.cs`
```csharp
// Добавить симметричный метод:
public void ClearSessionSource()
{
    _sessionSource = null;
}
```

> `ClearSessionSource` вызывается при возврате из free roam в нарратив — чтобы сервис не держал устаревший source.

---

#### Acceptance Criteria
- [ ] `@activateDayQuests day:day1` запускает `QuestService.ActivateDaySession` и `GameFlowService.SetSessionSource`.
- [ ] `DaySessionSource.GetReturnScript` возвращает значение из `DayConfigSO`.
- [ ] `GameFlowService.ClearSessionSource` обнуляет source без ошибок.
- [ ] `DaySessionOrchestrator` бросает `ArgumentException` если `dayId` не найден в конфиге.

---

---

## 🏁 Milestone: Day 3 — UI, Integration & QA

> View существует в сцене (CustomUI). Никто не создаёт View снаружи. View сама получает свою ViewModel.

---

### Ticket 2.5.1 — [FEATURE] QuestEntryViewModel + QuestPanelViewModel

**Labels:** `feature`, `ui`, `service`
**Estimate:** 3h
**Milestone:** Day 3 — UI, Integration & QA
**Depends on:** Ticket 2.4.1

#### Описание

ViewModel-слой. `QuestPanelViewModel` — `IEngineService`, регистрируется в Naninovel DI. `QuestEntryViewModel` — чистый C# объект, живёт столько же сколько `QuestInstance`.

---

**Файл:** `Assets/Scripts/Quests/UI/QuestEntryViewModel.cs`
```csharp
public class QuestEntryViewModel
{
    public string DisplayText   { get; }
    public string ObjectiveText { get; private set; }
    public int    CurrentCount  { get; private set; }
    public int    RequiredCount { get; private set; }
    public bool   IsCompleted   { get; private set; }

    public event Action OnProgressChanged;
    public event Action OnCompleted;

    public QuestEntryViewModel(QuestInstance instance)
    {
        DisplayText = instance.Definition.DisplayText;
        RefreshFromInstance(instance);
    }

    // Вызывается QuestPanelViewModel при получении OnObjectiveTicked
    internal void NotifyProgress(QuestObjectiveInstance objective)
    {
        CurrentCount  = objective.CurrentCount;
        RequiredCount = objective.Definition.RequiredCount;
        ObjectiveText = objective.Definition.DisplayText;
        OnProgressChanged?.Invoke();
    }

    // Вызывается QuestPanelViewModel при получении OnQuestCompleted
    internal void NotifyCompleted()
    {
        IsCompleted = true;
        OnCompleted?.Invoke();
    }

    private void RefreshFromInstance(QuestInstance instance)
    {
        var first = instance.Objectives.FirstOrDefault(o => !o.IsCompleted)
                    ?? instance.Objectives.FirstOrDefault();
        if (first == null) return;
        ObjectiveText = first.Definition.DisplayText;
        CurrentCount  = first.CurrentCount;
        RequiredCount = first.Definition.RequiredCount;
    }
}
```

**Файл:** `Assets/Scripts/Quests/UI/QuestPanelViewModel.cs`
```csharp
[InitializeAtRuntime]
public class QuestPanelViewModel : IEngineService
{
    private readonly QuestService _questService;
    private readonly List<QuestEntryViewModel> _activeQuests = new();
    private readonly Dictionary<QuestInstance, QuestEntryViewModel> _vmMap = new();

    public IReadOnlyList<QuestEntryViewModel> ActiveQuests => _activeQuests;

    public event Action<QuestEntryViewModel> OnQuestAdded;
    public event Action<QuestEntryViewModel> OnQuestRemoved;
    public event Action                      OnAllQuestsCompleted;
    public event Action                      OnSessionActivated;

    public QuestPanelViewModel(QuestService questService)
    {
        _questService = questService;
    }

    public UniTask InitializeService()
    {
        _questService.OnQuestAdded          += HandleQuestAdded;
        _questService.OnObjectiveTicked     += HandleObjectiveTicked;
        _questService.OnQuestCompleted      += HandleQuestCompleted;
        _questService.OnAllQuestsCompleted  += HandleAllCompleted;
        return UniTask.CompletedTask;
    }

    public void DestroyService()
    {
        _questService.OnQuestAdded          -= HandleQuestAdded;
        _questService.OnObjectiveTicked     -= HandleObjectiveTicked;
        _questService.OnQuestCompleted      -= HandleQuestCompleted;
        _questService.OnAllQuestsCompleted  -= HandleAllCompleted;
    }

    public void ResetService()
    {
        _activeQuests.Clear();
        _vmMap.Clear();
    }

    private void HandleQuestAdded(QuestInstance quest)
    {
        var vm = new QuestEntryViewModel(quest);
        _activeQuests.Add(vm);
        _vmMap[quest] = vm;
        if (_activeQuests.Count == 1) OnSessionActivated?.Invoke(); // первый квест = сессия началась
        OnQuestAdded?.Invoke(vm);
    }

    private void HandleObjectiveTicked(QuestInstance quest, QuestObjectiveInstance objective)
    {
        if (_vmMap.TryGetValue(quest, out var vm)) vm.NotifyProgress(objective);
    }

    private void HandleQuestCompleted(QuestInstance quest)
    {
        if (_vmMap.TryGetValue(quest, out var vm))
        {
            vm.NotifyCompleted();
            _activeQuests.Remove(vm);
            _vmMap.Remove(quest);
            OnQuestRemoved?.Invoke(vm);
        }
    }

    private UniTask HandleAllCompleted()
    {
        OnAllQuestsCompleted?.Invoke();
        return UniTask.CompletedTask;
    }
}
```

---

#### Acceptance Criteria
- [ ] `Engine.GetService<QuestPanelViewModel>()` возвращает экземпляр.
- [ ] `OnSessionActivated` файрится при добавлении первого квеста.
- [ ] `OnQuestRemoved` файрится при завершении квеста (а не при тике объектива).
- [ ] `ActiveQuests` содержит только незавершённые квесты.
- [ ] `ActiveQuests` заполнен корректно после `LoadServiceState` (для load-safe rebuild).

---

### Ticket 2.5.2 — [FEATURE] QuestPanelUI (CustomUI) + QuestEntryView prefab

**Labels:** `feature`, `ui`
**Estimate:** 3.5h
**Milestone:** Day 3 — UI, Integration & QA
**Depends on:** Ticket 2.5.1

#### Описание

UI-слой. `QuestPanelUI` — `CustomUI`, существует в сцене как часть Naninovel UI. Самостоятельно получает ViewModel в `Awake`. `QuestEntryView` — MonoBehaviour на prefab, `Bind(vm)` единственная точка входа.

---

**Файл:** `Assets/Scripts/Quests/UI/QuestPanelUI.cs`
```csharp
public class QuestPanelUI : CustomUI
{
    [SerializeField] private Transform     _entriesContainer;
    [SerializeField] private QuestEntryView _entryPrefab;

    private QuestPanelViewModel _viewModel;
    private readonly Dictionary<QuestEntryViewModel, QuestEntryView> _entries = new();

    protected override void Awake()
    {
        base.Awake();
        _viewModel = Engine.GetService<QuestPanelViewModel>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        _viewModel.OnSessionActivated   += Show;
        _viewModel.OnQuestAdded         += AddEntry;
        _viewModel.OnQuestRemoved       += RemoveEntry;
        _viewModel.OnAllQuestsCompleted += Hide;

        // Восстановление состояния при загрузке (load-safe rebuild)
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
        if (_entries.ContainsKey(vm)) return; // guard против двойного добавления при rebuild
        var entry = Instantiate(_entryPrefab, _entriesContainer);
        entry.Bind(vm);
        _entries[vm] = entry;
    }

    private void RemoveEntry(QuestEntryViewModel vm)
    {
        if (!_entries.TryGetValue(vm, out var entry)) return;
        _entries.Remove(vm);
        entry.PlayCompletionAnimation(onComplete: () => Destroy(entry.gameObject));
    }
}
```

**Файл:** `Assets/Scripts/Quests/UI/QuestEntryView.cs`
```csharp
public class QuestEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text _displayText;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Animator _animator;

    private QuestEntryViewModel _vm;

    // Единственная точка входа — View сама настраивает себя
    public void Bind(QuestEntryViewModel vm)
    {
        _vm = vm;
        _displayText.text = vm.DisplayText;
        RefreshProgress();

        vm.OnProgressChanged += RefreshProgress;
        vm.OnCompleted       += OnCompleted;
    }

    private void OnDestroy()
    {
        if (_vm == null) return;
        _vm.OnProgressChanged -= RefreshProgress;
        _vm.OnCompleted       -= OnCompleted;
    }

    private void RefreshProgress()
    {
        _progressText.text = _vm.RequiredCount > 1
            ? $"{_vm.ObjectiveText} ({_vm.CurrentCount}/{_vm.RequiredCount})"
            : _vm.ObjectiveText;
    }

    private void OnCompleted()
    {
        _animator.SetTrigger("Complete");
    }

    public void PlayCompletionAnimation(Action onComplete)
    {
        _animator.SetTrigger("FadeOut");
        // Запустить корутину или использовать Animation Event для вызова onComplete
        StartCoroutine(WaitAndCall(onComplete));
    }

    private IEnumerator WaitAndCall(Action onComplete)
    {
        yield return new WaitForSeconds(0.5f); // настраивается по длине анимации
        onComplete?.Invoke();
    }
}
```

---

#### Setup Instructions

1. Создать `QuestPanelUI` prefab в `Assets/UI/Quests/QuestPanelUI.prefab` — наследник `CustomUI`.
2. Зарегистрировать prefab в `Naninovel → UI → Custom UI`.
3. Создать `QuestEntryView` prefab в `Assets/UI/Quests/QuestEntryView.prefab`.
4. Назначить `_entryPrefab` и `_entriesContainer` в Inspector `QuestPanelUI`.
5. Привязать `TMP_Text` компоненты и `Animator` в `QuestEntryView`.

---

#### Acceptance Criteria
- [ ] Панель появляется при начале дня (после `@activateDayQuests`).
- [ ] При тике объектива прогресс-текст обновляется без мигания.
- [ ] После завершения квеста entry исчезает с анимацией.
- [ ] После завершения всех квестов панель скрывается.
- [ ] При загрузке сохранения панель восстанавливается корректно (load-safe rebuild).
- [ ] `QuestEntryView` отписывается от ViewModel в `OnDestroy`.

---

---

### ─── Integration & QA ───

---

### Ticket 2.6.1 — [INTEGRATION] End-to-end .nani скрипт + ручная проверка

**Labels:** `integration`, `feature`
**Estimate:** 2h
**Milestone:** Day 3 — UI, Integration & QA
**Depends on:** все тикеты Day 1–5

#### Описание

Создать минимальный `.nani` тестовый скрипт, покрывающий полный цикл: выдача квестов → прогресс → завершение → возврат в нарратив.

---

**Файл:** `Assets/Scripts/Naninovel/Resources/Naninovel/Scripts/test_quest_flow.nani`
```nani
; === Тест: полный цикл квестов ===

; 1. Начало нарратива — персонаж говорит о задании
@char Masha: Сегодня нужно сделать кое-что важное.

; 2. Активируем квесты дня и переходим во free roam
@activateDayQuests day:test_day
@exitNarrative locationId:farm_yard

; --- Во free roam игрок посещает локации ---
; LocationService.OnLocationEnterStarted автоматически репортит
; После посещения barn: @reportQuestEvent автоматически не нужен

; Для мини-игры — вызывается по окончании:
; @reportQuestEvent id:shooting_game

; 3. После завершения всех квестов GameFlowService возвращает сюда:
@label quest_return
@char Masha: Отлично, всё готово!
```

---

#### Acceptance Criteria
- [ ] Скрипт выполняется без ошибок в play mode.
- [ ] Панель квестов появляется после `@activateDayQuests`.
- [ ] Прогресс объективов обновляется при посещении локаций.
- [ ] `@reportQuestEvent` засчитывает прогресс мини-игры.
- [ ] После завершения всех квестов нарратив возобновляется на метке `quest_return`.
- [ ] Сохранение/загрузка во время free roam восстанавливает состояние квестов и UI.

---

### Ticket 2.6.2 — [QA-TOOLING] ForceCompleteQuestsCommand + Debug Inspector

**Labels:** `qa-tooling`
**Estimate:** 1.5h
**Milestone:** Day 3 — UI, Integration & QA
**Depends on:** Ticket 2.4.1

#### Описание

Debug-инструменты для QA: команда для мгновенного завершения квестов и Editor-расширение для просмотра состояния.

---

**Файл:** `Assets/Scripts/Quests/Commands/ForceCompleteQuestsCommand.cs`
```csharp
[CommandAlias("forceCompleteQuests")]
public class ForceCompleteQuestsCommand : Command
{
    public override UniTask Execute(AsyncToken asyncToken = default)
    {
        Engine.GetService<QuestService>().ForceComplete();
        return UniTask.CompletedTask;
    }
}
```

**Файл:** `Assets/Scripts/Quests/Editor/QuestServiceDebugWindow.cs`
```csharp
#if UNITY_EDITOR
public class QuestServiceDebugWindow : EditorWindow
{
    [MenuItem("OnlyFarms/Quest Debug")]
    public static void ShowWindow() => GetWindow<QuestServiceDebugWindow>("Quest Debug");

    private void OnGUI()
    {
        if (!Application.isPlaying) { GUILayout.Label("Play mode only"); return; }

        var service = Engine.GetService<QuestService>();
        if (service == null) { GUILayout.Label("QuestService not found"); return; }

        GUILayout.Label("Visible Quests:", EditorStyles.boldLabel);
        foreach (var quest in service.GetVisibleQuests())
        {
            GUILayout.Label($"  • {quest.Definition.DisplayText} — {(quest.IsCompleted ? "✓" : "...")}");
            foreach (var obj in quest.Objectives)
                GUILayout.Label($"    [{obj.CurrentCount}/{obj.Definition.RequiredCount}] {obj.Definition.DisplayText}");
        }

        if (GUILayout.Button("Force Complete All"))
            service.ForceComplete();
    }
}
#endif
```

---

#### Acceptance Criteria
- [ ] `@forceCompleteQuests` в `.nani` скрипте завершает все квесты и файрит `OnAllQuestsCompleted`.
- [ ] `Quest Debug` окно открывается через `OnlyFarms → Quest Debug`.
- [ ] Окно отображает текущие квесты и прогресс объективов в play mode.
- [ ] Кнопка "Force Complete All" работает корректно.

---

### Ticket 2.6.3 — [INTEGRATION] GameFlowService — подписка на OnAllQuestsCompleted

**Labels:** `integration`
**Estimate:** 1.5h
**Milestone:** Day 3 — UI, Integration & QA
**Depends on:** Ticket 2.4.1, Ticket 2.4.3

#### Описание

`GameFlowService` подписывается на `IQuestStatusSource.OnAllQuestsCompleted` и возобновляет нарратив через `DaySessionSource`. Добавить `ClearSessionSource` вызов после возврата.

---

**Изменение:** `Assets/Scripts/Locations/GameFlowService.cs`
```csharp
// В InitializeService:
_questStatusSource.OnAllQuestsCompleted += OnAllQuestsCompleted;

// В DestroyService:
_questStatusSource.OnAllQuestsCompleted -= OnAllQuestsCompleted;

// Новый метод:
private async UniTask OnAllQuestsCompleted()
{
    if (_sessionSource == null)
    {
        Debug.LogWarning("GameFlowService: OnAllQuestsCompleted but no session source set.");
        return;
    }
    var script  = _sessionSource.GetReturnScript();
    var label   = _sessionSource.GetReturnLabel();
    ClearSessionSource();
    await _scriptPlayer.PreloadScriptAsync(script);
    _scriptPlayer.Play(script, label);
}
```

---

#### Acceptance Criteria
- [ ] После `OnAllQuestsCompleted` нарратив возобновляется на метке `ReturnLabel` скрипта `ReturnScript`.
- [ ] `ClearSessionSource` вызывается перед возобновлением нарратива (не держим устаревший source).
- [ ] Если `_sessionSource == null` — warning, без краша.
- [ ] `GameFlowService` инжектирует `IQuestStatusSource` (не `QuestService` напрямую).

---
