# Архитектура — Ядро (Core)

> Кросс-модульный фундамент. Читать перед любым модульным файлом.
> Меняется только при изменении фундаментальных паттернов проекта.
> Индекс: [`architecture_index.md`](architecture_index.md)

---

## 1. Принципы разработки

### Снаружи внутрь (Outside-In)

Разработка начинается с публичного контракта и видимого поведения, затем движется к реализации:

1. Определить интерфейс / событие / команду
2. Написать заглушку-реализацию
3. Реализовать детали

Заглушки — это не временный костыль, это осознанный выбор: система работает end-to-end с первого дня.

### Абстракции там, где реализация неизвестна или может меняться

Если не знаем как будет реализовано — вводим интерфейс. Примеры из проекта:

```
ILocationNarrativeSource  — кто решает, запускать ли скрипт при входе на локацию
IFreeRoamSessionSource    — кто знает, куда вернуться по завершении фри рума
IQuestStatusSource        — кто сообщает о завершении всех квестов
IHotspotValidator         — кто проверяет доступность хотспота
```

Заглушки реализуют интерфейс с минимальным поведением. В нужный эпик заглушка заменяется реальной реализацией — остальной код не меняется.

### Code Style — методы вместо свойств

В проекте **не используются C# properties**. Вместо них — явные методы:

```csharp
// ❌ не используем
public bool IsCompleted => ...;
public int CurrentCount { get; private set; }

// ✅ используем
public bool IsCompleted() => ...;
public int GetCurrentCount() => ...;
public void SetCurrentCount(int value) => ...;
```

Это применяется ко всем слоям: Domain, Infrastructure, DataAccess.

### Один класс — одна ответственность

Признак нарушения: метод нужно изменить по двум разным причинам. Решение — выделить класс или интерфейс.

### Сервисы vs Команды

- **Сервис** — реализует `IEngineService` или `IStatefulService<T>`, регистрируется через `[InitializeAtRuntime]`
- **Команда** — наследует `Command`, вызывается из `.nani` скриптов

Команды — тонкий слой. Вся логика — в сервисах и plain C# классах.

---

## 2. Слои архитектуры

```
.nani скрипты
      ↓
Команды (Command)           ← тонкий слой, только маршрутизация
      ↓
Сервисы (IEngineService)    ← оркестрация, lifecycle, события
      ↓
Plain C# логика             ← доменная логика, без зависимости на Unity/Naninovel
      ↓
ScriptableObject конфиги    ← данные, только чтение
```

Plain C# классы (`LocationLogic`, `HotspotLogic`) не знают о Unity и Naninovel. Их можно тестировать без движка.

---

## 3. Архитектура сервисов (Naninovel DI)

```csharp
[InitializeAtRuntime]
public class LocationService : IStatefulService<GameStateMap>
{
    public LocationService(GameConfig gameConfig, IBackgroundManager backgroundManager, IScriptPlayer scriptPlayer) { }
}
```

Порядок инициализации — топологически по конструкторным зависимостям. Команды находятся через рефлексию.

### Конфигурация — GameConfig

```csharp
[EditInProjectSettings]
public class GameConfig : Configuration
{
    public LocationConfigSO LocationConfig;
    public GameSoundConfig SoundConfig;
    // QuestConfigSO QuestConfig;   — добавляется в Эпике 2
    // ScaleConfigSO ScaleConfig;   — добавляется в Эпике 3
}
```

### Доступ из любой точки

```csharp
Engine.GetService<LocationService>().Enter("backyard", ct);
```

---

## 4. Игровой флоу — два состояния

Игра всегда находится в одном из двух состояний:

- **Нарратив** — активен `IScriptPlayer`, управляет фоном, текстом, персонажами
- **Free Roam** — `IScriptPlayer` остановлен, активны хотспоты и фон локации

Переход **нарратив → free roam**: команда `@exitNarrative`
Переход **free roam → нарратив**: `GameFlowService` через `IQuestStatusSource.OnAllQuestsCompleted` или `ILocationNarrativeSource`

Половинчатого состояния нет. При переходе в нарратив — `HideAllActors` + `SetFreeRoamMode(false)` скрывают всё от free roam. При переходе в free roam — `scriptPlayer.Stop()` освобождает MainTrack.

### GameFlowService — оркестратор переходов

```
IQuestStatusSource.OnAllQuestsCompleted  →  GameFlowService  →  LaunchNarrativeAsync()
LocationService.OnLocationEnterStarted   →  GameFlowService  →  ILocationNarrativeSource → LaunchNarrativeAsync()
```

```csharp
[InitializeAtRuntime]
public class GameFlowService : IEngineService
{
    // Зависимости через конструктор:
    // LocationService, IScriptPlayer, QuestService (как IQuestStatusSource), ILocationNarrativeSource

    public void SetSessionSource(IFreeRoamSessionSource source); // временно до Epic 2
}
```

**Интерфейсы GameFlowService:**

| Интерфейс | Назначение | Заглушка | Epic |
|---|---|---|---|
| `ILocationNarrativeSource` | скрипт при входе на локацию | `AlwaysNullNarrativeSource` | 2 |
| `IFreeRoamSessionSource` | точка возврата в нарратив | `HardcodedSessionSource` | 2 |
| `IQuestStatusSource` | триггер завершения квестов | реализует `QuestService` | stub |

---

## 5. Флоу дня

```
day_01.nani:
  @activateDayQuests day:day1
  @exitNarrative backyard returnScript:day_01 returnLabel:after_free_roam
    → HideAllActors
    → LocationService.Enter("backyard") — фон + хотспоты
    → SetFreeRoamMode(true)
    → GameFlowService.SetSessionSource(new HardcodedSessionSource("day_01", "after_free_roam"))
    → scriptPlayer.Stop() — MainTrack свободен

  ; --- игрок в свободном перемещении ---
  ; QuestService.ForceComplete() / реальное завершение квестов
    → IQuestStatusSource.OnAllQuestsCompleted
    → GameFlowService.LaunchNarrativeAsync("day_01", "after_free_roam")
    → SetFreeRoamMode(false) — хотспоты и фон скрываются
    → scriptPlayer.LoadAndPlayAtLabel("day_01", "after_free_roam")

  # after_free_roam
  ; нарратив продолжается
```

### Нарративная вставка при входе на локацию

```
Игрок переходит на локацию
  → LocationService.RenderLocation()
  → OnLocationEnterStarted?.Invoke()           ← до рендера фона
  → GameFlowService → ILocationNarrativeSource.GetOnEnterScript(locationId)
  → если не null: SetFreeRoamMode(false) + scriptPlayer.Stop() + LoadAndPlay(script)
  → рендер локации отменяется через CancellationToken
  → по @exitNarrative в скрипте — возврат в free roam
```

---

## 6. Слой представления (View / UI)

Views — passive objects:
- Экспонируют callbacks и Actions
- Обновляют визуальное состояние через метод
- Не хранят бизнес-состояние

Контроллер/Presenter подписывается на события View и вызывает сервисы в ответ.

---

## 7. Persistence (Save/Load)

```csharp
public interface IStatefulService<TState> : IEngineService
{
    void SaveServiceState(TState stateMap);
    UniTask LoadServiceState(TState stateMap);
}
```

`LoadServiceState` — только восстановление данных. Рендеринг — через повторное выполнение после загрузки.

При загрузке в free roam: `IsInFreeRoam=true` → `scriptPlayer.Stop()` → `RenderLocation()`.

---

## 8. Аудио

Игровые сервисы не вызывают `IAudioManager` напрямую. Они файрят доменные события. Sound observer'ы — единственная точка связи событий со звуком. Каждая система имеет свой observer; все читают один `GameSoundConfigSO` через `GameConfig`.

```
LocationService.OnNavigatedForward  ──┐
LocationService.OnNavigatedBack     ──┤  LocationSoundObserver  →  IAudioManager
LocationService.OnItemPickedUp      ──┘

QuestService.OnQuestObjectiveTicked ──┐
QuestService.OnQuestCompleted       ──┤  QuestSoundObserver     →  IAudioManager
QuestService.OnAllQuestsCompleted   ──┘
```

Исключение: `@sfx` в `.nani` скриптах — допустимо.

---

## 9. Асинхронность

| Слой | Тип | Причина |
|---|---|---|
| Домен (plain C#) | Синхронный | Только данные, никакого IO |
| Unity-инфраструктура | `AsyncToken` (Naninovel) | Единый async-стек |
| Addressables | `handle.Task.AsUniTask()` | BCL Task → UniTask |
| Async события | `event Func<UniTask>` | Избегаем `async void` |

Cysharp UniTask удалён. Только Naninovel UniTask / AsyncToken.

---

## 10. Что НЕ делаем

- `GameObject.Find()` / `FindObjectOfType()` в runtime
- Синглтоны поверх DI
- Бизнес-логика в `.nani` скриптах — только повествование
- Нарративные скрипты не знают о квестах и точках возврата
- `async void` — только `async UniTask` или `event Func<UniTask>`
- `IAudioManager` напрямую из игровых сервисов — только через sound observer'ы
- `IEngineService` для plain C# вспомогательных классов — только для сервисов с lifecycle
