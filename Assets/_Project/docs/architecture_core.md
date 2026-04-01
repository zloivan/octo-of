# Архитектура — Ядро (Core)

> Кросс-модульный фундамент. Читать перед любым модульным файлом.
> Меняется только при изменении фундаментальных паттернов проекта.
> Индекс: [`architecture_index.md`](architecture_index.md)

---

## 5. Архитектура сервисов (Naninovel DI)

### Принцип

Сервисы регистрируются через Naninovel DI (`[InitializeAtRuntime]`). Допустимы целенаправленные обёртки над Naninovel-сервисами когда они решают конкретную проблему и не дублируют существующую функциональность.

> **Пример допустимой обёртки:** `LocationService` использует `IBackgroundManager` для управления фоном локаций через выделенный actor (`id: "location"`). Нарративный `@back` работает с другими акторами и никогда не трогает `"location"`.

```
LocationLogic (plain C#)              ← доменная логика, runtime-состояние
        ↑
LocationService : IStatefulService    ← Naninovel lifecycle, тонкий фасад
        ↑
ExitNarrativeCommand : Command        ← @exitNarrative id:backyard в .nani
```

### Регистрация сервисов

```csharp
[InitializeAtRuntime]
public class LocationService : IStatefulService<LocationServiceState>
{
    public LocationService(GameConfig gameConfig, IBackgroundManager backgroundManager,
                           ICameraManager cameraManager) { ... }
}
```

Порядок инициализации — топологически по конструкторным зависимостям. Команды находятся через рефлексию.

### Конфигурация — GameConfig

```csharp
[EditInProjectSettings]
public class GameConfig : Configuration
{
    public LocationConfigSO LocationConfig;
    // QuestConfigSO QuestConfig;     — добавляется в Эпике 2
    // ScaleConfigSO ScaleConfig;     — добавляется в Эпике 3
}
```

### Доступ из любой точки

```csharp
Engine.GetService<LocationService>().Enter("backyard", ct);
```

### Почему нарративный скрипт — арбитр флоу

`@exitNarrative` — блокирующая команда. Скрипт дня (`day_01.nani`) содержит полный флоу: нарратив → свободное перемещение → нарратив. Нет отдельного координатора — дизайнер видит весь день в одном файле.

```
; day_01.nani
; ... нарратив ...
@activateDayQuests day:day1
@exitNarrative id:backyard      ← блокирует до выполнения всех квестов дня
; ... нарратив продолжается ...
```

---

## 8. Слой представления (View / UI)

### Принцип

Views — passive objects. Они не знают о логике, не кэшируют состояние. Они только:
- Экспонируют callbacks и Actions
- Обновляют визуальное состояние через метод (`SetScore(int score)`)
- Генерируют события через Action<> поля

### Пример: HotspotView

```csharp
public class HotspotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string Id;
    public Action OnHoverEnter;
    public Action OnHoverExit;
    public Action OnClicked;

    public void SetBrightness(float value) { /* MaterialPropertyBlock */ }
    public void SetInteractable(bool value) { /* collider.enabled */ }
}
```

### Контроллер (Presenter)

1. Держит ссылку на View
2. Подписывается на события View
3. Вызывает сервисы в ответ
4. Обновляет View по событиям сервиса

---

## 9. Persistence (Save/Load)

```csharp
public interface IStatefulService<TState> : IEngineService
{
    void SaveServiceState(TState stateMap);
    UniTask LoadServiceState(TState stateMap);
}
```

### Save/Load при @exitNarrative

- `IsInFreeRoam = true` сохраняется в `LocationServiceState`
- При загрузке: Naninovel восстанавливает строку скрипта → выполняет `@exitNarrative` повторно → `LocationService.Enter()` восстанавливает фон + хотспоты

### Что НЕ сохраняется

- Transient UI state
- Выбранный элемент (пересчитывается при загрузке)
- Кэш ассетов (Addressables управляет)

> **Исключение:** `MiniGameService` — рекорды персистентны между сбросами.

---

## 10. Аудио

```csharp
Engine.GetService<IAudioManager>().PlaySfxAsync("click_movement_forward");
```

SFX-ключи — в модульных файлах каждой системы.

---

## 11. Асинхронность (UniTask)

- Команды — `async/await` в `ExecuteAsync()`
- Загрузка ассетов — `Addressables.LoadAssetAsync`
- View-методы — синхронные

```csharp
public override async UniTask ExecuteAsync(AsyncToken asyncToken = default)
{
    await Engine.GetService<LocationService>()
        .Enter(Id, asyncToken.CancellationToken);
    // ждём выполнения всех квестов дня
    await Engine.GetService<QuestService>()
        .WaitForAllQuestsCompleted(asyncToken.CancellationToken);
}
```

---

## 12. Что НЕ делаем

- Параллельные event bus'ы — только Naninovel Events
- Синглтоны поверх DI
- Прямые ссылки между сервисами — только `Engine.GetService<>()`
- Кэш состояния в View
- `GameObject.Find()` / `FindObjectOfType()` в runtime

---

## 15. Сценарии использования

### Сценарий 1: Флоу дня (нарратив → свободное перемещение → нарратив)

```
day_01.nani:
  @activateDayQuests day:day1      ← QuestService инициализирует квесты
  @exitNarrative id:backyard       ← LocationService.Enter("backyard")
                                      IsInFreeRoam = true
                                      ждём QuestService.WaitForAllQuestsCompleted()
  ; --- игрок в свободном перемещении ---
  ; QuestService.OnAllQuestsCompleted срабатывает
  ; @exitNarrative разблокируется
  @back appearance:day1_evening    ← нарратив продолжается
  ; ...
```

### Сценарий 2: Клик по предмету в свободном перемещении

```
Игрок кликает по предмету
  → HotspotView.OnClicked → IHotspotInput → LocationService.OnItemClicked()
  → _logic.MarkConsumed(itemId)
  → HotspotManager.DeactivateHotspot(itemId)
  → PlaySfx("click_object")
  → если onClickScript задан: ScriptPlayer.PlayAsync() (нарратив внутри свободного перемещения)
  → QuestService.ReportEvent(objectiveTag) — null-safe
```

### Сценарий 3: Save/Load в свободном перемещении

```
Игрок сохраняет → LocationServiceState: CurrentLocationId="backyard", IsInFreeRoam=true
Игрок загружает → Naninovel восстанавливает строку @exitNarrative
               → LocationService.Enter("backyard") — фон + хотспоты
               → QuestService восстанавливает прогресс квестов
               → WaitForAllQuestsCompleted() продолжает ждать
```
