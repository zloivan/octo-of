# Архитектура — Система квестов

> Покрывает: QuestService, GameFlowService, IQuestStatusSource, ILocationNarrativeSource, IFreeRoamSessionSource, возврат в нарратив.
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 1. Контекст

Квесты выдаются в сегментах free roam. При завершении всех квестов дня — `GameFlowService` возобновляет нарратив. Точка возврата хранится в `IFreeRoamSessionSource` — не в нарративном скрипте.

Нарративный скрипт не знает о квестах и точках возврата. Он только рассказывает историю и вызывает `@exitNarrative`.

---

## 2. QuestService (stub — полная реализация в Эпике 2)

```csharp
[InitializeAtRuntime]
public class QuestService : IStatefulService<GameStateMap>, IQuestStatusSource
{
    public event Func<UniTask> OnAllQuestsCompleted;

    public bool IsQuestCompleted(string conditionValue) { /* stub */ }
    public void ForceComplete() => OnAllQuestsCompleted?.Invoke().Forget(); // только для debug
}
```

`QuestService` реализует `IQuestStatusSource` — единственный контракт который `GameFlowService` использует для подписки.

В Epic 2: добавить `QuestLogic`, `ReportEvent`, `ActiveQuestSnapshot[]`, `SaveServiceState`.

---

## 3. GameFlowService

Оркестратор переходов между нарративом и free roam. Подписывается на события, не предоставляет публичное API для вызова флоу.

```csharp
[InitializeAtRuntime]
public class GameFlowService : IEngineService
{
    public GameFlowService(
        LocationService locationService,
        IScriptPlayer scriptPlayer,
        QuestService questSource,            // как IQuestStatusSource
        ILocationNarrativeSource narrativeSource)

    public void SetSessionSource(IFreeRoamSessionSource source); // временно до Epic 2
    public void SetNarrativeSource(ILocationNarrativeSource source); // только для debug/тестов
}
```

### Подписки

```
QuestService.OnAllQuestsCompleted      → OnAllQuestsCompleted()   → LaunchNarrativeAsync(returnScript, returnLabel)
LocationService.OnLocationEnterStarted → OnLocationEnterStarted() → ILocationNarrativeSource → LaunchNarrativeAsync()
```

### LaunchNarrativeAsync

```csharp
private async UniTask LaunchNarrativeAsync(string scriptName, string label = null)
{
    await _locationService.SetFreeRoamMode(false); // отменяет рендер, чистит сцену
    _scriptPlayer.Stop();

    if (!string.IsNullOrEmpty(label))
        await _scriptPlayer.LoadAndPlayAtLabel(scriptName, label);
    else
        await _scriptPlayer.LoadAndPlay(scriptName);
}
```

---

## 4. Интерфейсы

### IQuestStatusSource

```csharp
public interface IQuestStatusSource
{
    event Func<UniTask> OnAllQuestsCompleted;
}
```

Реализует `QuestService`. В Epic 2 остаётся без изменений.

### ILocationNarrativeSource

```csharp
public interface ILocationNarrativeSource
{
    string GetOnEnterScript(string locationId);
    string GetOnEnterLabel(string locationId); // null = с начала
}
```

Заглушка: `AlwaysNullNarrativeSource` — всегда null.
Debug: `HardcodedNarrativeSource(locationId, script, label)` — для тестирования конкретной локации.
Epic 2: `QuestDrivenNarrativeSource` — проверяет условия (день, флаг, `IScriptPlayer.HasPlayed()`).

### IFreeRoamSessionSource

```csharp
public interface IFreeRoamSessionSource
{
    string ReturnScript { get; }
    string ReturnLabel  { get; }
}
```

Заглушка: `HardcodedSessionSource(returnScript, returnLabel)` — устанавливается из `@exitNarrative`.
Epic 2: `DaySessionSource` — читает из `DayConfig`, `@exitNarrative` перестаёт принимать `returnScript`.

---

## 5. DayConfig — план для Epic 2

```
DayConfig : ScriptableObject
  ├── day: string
  ├── returnScript: string      ← точка возврата в нарратив по завершении квестов
  ├── returnLabel: string
  └── quests: QuestDefinition[]
```

`DaySessionSource` реализует `IFreeRoamSessionSource`, читая `returnScript/returnLabel` из `DayConfig` текущего дня. `@exitNarrative` перестаёт принимать `returnScript` — нарративный скрипт не знает о точках возврата.

---

## 6. Команды

| Команда | Параметры | Действие |
|---|---|---|
| `@exitNarrative` | `[locationId]`, `[returnScript]`, `[returnLabel]` | Выйти в free roam. `returnScript` — временный до Epic 2 |
| `@activateDayQuests` | `day:day1` | Epic 2 — активировать квесты дня |
| `@addQuest` | `id:find_diary` | Epic 2 — добавить квест динамически |

**`@exitNarrative` — текущее поведение:**

```csharp
await new HideAllActors().Execute(token);
await locationService.Enter(locationId, token);
await locationService.SetFreeRoamMode(true);
if (Assigned(ReturnScript))
    gameFlowService.SetSessionSource(new HardcodedSessionSource(ReturnScript.Value, returnLabel));
scriptPlayer.Stop();
```

**`@exitNarrative` — поведение в Epic 2:**
`returnScript` и `returnLabel` удаляются. `GameFlowService` получает `IFreeRoamSessionSource` от `DayConfig`.

---

## 7. Возврат в нарратив — полный флоу

```
Игрок выполняет последний квест
  → QuestService.ForceComplete() / реальная проверка в Epic 2
  → IQuestStatusSource.OnAllQuestsCompleted
  → GameFlowService.OnAllQuestsCompleted()
  → IFreeRoamSessionSource.ReturnScript / ReturnLabel
  → LaunchNarrativeAsync(returnScript, returnLabel)
      → LocationService.SetFreeRoamMode(false)
          → _renderCts.Cancel()
          → HotspotManager.Unload()
          → bg.ChangeVisibility(false)
      → scriptPlayer.Stop()
      → scriptPlayer.LoadAndPlayAtLabel(returnScript, returnLabel)
```

---

## 8. Save/Load

`IsInFreeRoam` сохраняется в `LocationServiceState`. При загрузке с `IsInFreeRoam=true`:
- `scriptPlayer.Stop()`
- `RenderLocation()` восстанавливает сцену

`IFreeRoamSessionSource` (returnScript/returnLabel) — в Epic 2 будет в `GameFlowServiceState`. Сейчас при загрузке returnPoint теряется (приемлемо для demo).

---

## 9. UI — QuestPanelUI (Epic 2)

`QuestPanelUI : CustomUI` — подписана на `QuestService.OnQuestAdded(QuestLogic logic)`.
`QuestEntryView : MonoBehaviour` — `Bind(QuestLogic logic)`, не хранит ссылку на сервис.

---

## 10. Аудио (Epic 2)

| Ключ | Момент |
|---|---|
| `quest_ticked` | Выполнение одного объектива |
| `quest_crossed` | Выполнение квеста |
| `quest_completed` | Выполнение последнего квеста дня |
