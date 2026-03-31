# Архитектура — Ядро (Core)

> Кросс-модульный фундамент. Читать перед любым модульным файлом.
> Меняется только при изменении фундаментальных паттернов проекта.
> Индекс: [`architecture_index.md`](architecture_index.md)

---

## 5. Архитектура сервисов (Naninovel DI)

### Принцип
Всё через Naninovel DI. Никаких параллельных систем и своих обёрток над Naninovel.

```
LocationLogic (plain C#)              ← доменная логика, runtime-состояние
        ↑
LocationService : IStatefulService    ← Naninovel lifecycle, тонкий фасад
        ↑
EnterLocationCommand : Command        ← @enterLocation id:backyard в .nani
```

### Регистрация сервисов
Каждый кастомный сервис регистрируется атрибутом `[InitializeAtRuntime]` — Naninovel находит и инициализирует его автоматически:

```csharp
[InitializeAtRuntime]
public class LocationService : IStatefulService<LocationServiceState>
{
    public LocationService(GameConfig gameConfig) { ... }
}
```

Порядок инициализации определяется топологически по конструкторным зависимостям — если `LocationService` принимает `GameConfig` в конструкторе, Naninovel гарантирует что `GameConfig` будет готов раньше.

Кастомные команды Naninovel находит автоматически через рефлексию — отдельной регистрации не требуют.

### Конфигурация — GameConfig

Единый корневой конфиг проекта. Наследует `Configuration` — Naninovel инжектирует его в конструкторы сервисов автоматически.

```csharp
[EditInProjectSettings]
public class GameConfig : Configuration
{
    public LocationConfigSO LocationConfig;
    // QuestConfigSO QuestConfig;
    // ScaleConfigSO ScaleConfig;
}
```

`LocationConfigSO`, `QuestConfigSO` и т.д. — обычные `ScriptableObject`. Ссылки на них настраиваются через `Naninovel → Configuration → GameConfig` в редакторе.

Asset хранится в `NaninovelData/Resources/Naninovel/Configuration/GameConfig.asset` — генерируется автоматически при первом открытии меню.

### Доступ из любой точки
```csharp
// Из другого сервиса или команды
Engine.GetService<LocationService>().Enter("backyard");
```

### Почему не @goto или встроенные команды Naninovel для смены локации
`@goto` управляет нарративным скриптом, не игровым состоянием. Смена локации — это игровое состояние (активные хотспоты, условия, история переходов). Это должно жить в `LocationService`.

---

## 8. Слой представления (View / UI)

### Принцип
Views — passive objects. Они не знают о логике, не кэшируют состояние. Они только:
- Экспонируют callbacks и Actions (по одному callback на action)
- Обновляют визуальное состояние через метод (e.g., `SetScore(int score)`)
- Генерируют события через Action<> поля

### Пример: HotspotView
```csharp
public class HotspotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public string id;
    public Action OnHoverEnter;
    public Action OnHoverExit;
    public Action OnClicked;

    // View не кэширует состояние "выбран ли" или "заблокирован ли"
    // Это узнаёт контроллер и вызывает View.SetActive() / SetColor() / SetInteractable()

    public void SetBrightness(float value) { /* ... */ }
    public void SetInteractable(bool value) { /* ... */ }
}
```

### Зависимости View: только UI компоненты (Button, Slider, Text и т.д.)
Никогда не кэшируем сервисы, не вызываем бизнес-логику, не подписываемся на события модели.

### Контроллер (Application Controller / Presenter)
Контроллер — это C# компонент (MonoBehaviour), который:
1. Держит ссылку на View
2. Подписывается на события View
3. Вызывает сервисы в ответ на события View
4. Подписывается на события сервиса
5. Обновляет View на основе событий сервиса

Контроллер создаётся как часть сцены или регистрируется через Naninovel Custom UI.

---

## 9. Persistence (Save/Load)

### Принцип
Каждый stateful сервис реализует `IStatefulService<TState>`:

```csharp
// Реальный интерфейс Naninovel 1.20
public interface IStatefulService<TState> : IEngineService
{
    void SaveServiceState(TState stateMap);
    UniTask LoadServiceState(TState stateMap);
}
```

Naninovel передаёт `stateMap` снаружи. Сервис читает/пишет свои поля в него:

```csharp
public void SaveServiceState(LocationServiceState stateMap)
{
    var snapshot = _logic.GetSnapshot();
    stateMap.CurrentLocationId = snapshot.CurrentLocationId;
    stateMap.LocationHistory   = snapshot.LocationHistory;
    stateMap.ConsumedItemIds   = snapshot.ConsumedItemIds;
}

public UniTask LoadServiceState(LocationServiceState stateMap)
{
    _logic.LoadSnapshot(new LocationLogicSnapshot(
        stateMap.CurrentLocationId,
        stateMap.LocationHistory   ?? Array.Empty<string>(),
        stateMap.ConsumedItemIds   ?? Array.Empty<string>()));
    return UniTask.CompletedTask;
}
```

`IStatefulService<T>` уже включает `IEngineService` — объявлять его отдельно не нужно.

### Что сохраняется
Каждый модуль документирует свой `State` самостоятельно:
- `LocationService.State` — см. [`architecture_locations.md`](architecture_locations.md)
- `QuestService.State` — см. [`architecture_quests.md`](architecture_quests.md)
- `SeductionScaleService.State` — см. [`architecture_scales.md`](architecture_scales.md)
- `MiniGameService.State` — см. [`architecture_minigames.md`](architecture_minigames.md)

### Что НЕ сохраняется
- Transient UI state (position, visibility)
- Какой элемент выбран (пересчитывается при загрузке локации)
- Кэш ассетов — Addressables управляет самостоятельно

### ResetService()
Вызывается при начале новой игры. Вызывает `SetState(defaultState)` для каждого сервиса.

> **Исключение:** `MiniGameService.ResetService()` не сбрасывает State — рекорды и разблокировки персистентны между сессиями и сбросами состояния Naninovel. См. [`architecture_minigames.md`](architecture_minigames.md) §17.5.

---

## 10. Аудио (интеграция)

### Структура
`IAudioManager` — интерфейс, за реализацию отвечает фреймворк Naninovel (или кастомная обёртка).

SFX-ключи распределены по модульным файлам — каждый модуль документирует свои ключи в секции `## 10. Аудио (фрагмент — ...)`.

### Вызов из кода
```csharp
// Где-то в LocationService
AudioManager.PlaySfx("click_movement_forward");

// Где-то в SeductionScaleService
AudioManager.PlaySfx(_attractionDelta > 0 ? "scale_risky" : "scale_conscious");
```

---

## 11. Асинхронность (UniTask)

### Где используется
- **Команды** — `async/await` внутри `ExecuteAsync()`
- **Загрузка ассетов** — `await Addressables.LoadAssetAsync(...)`
- **Анимации UI** — coroutines через UniTask

### Где НЕ используется
- Сервисы, хранящие состояние (LocationService, QuestService) — синхронные методы
- View-методы (SetScore, SetActive) — синхронные

### CancellationToken
```csharp
public async UniTask<int> RunAsync(RectTransform container, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        // цикл игры
    }
    return _score;
}
```

---

## 12. Что НЕ делаем (слой архитектуры)

- Параллельные event bus'ы — только Naninovel Events
- Синглтоны поверх DI — Naninovel управляет lifecycle
- Прямые ссылки между сервисами `public OtherService other` — только `Engine.GetService<>()`
- Кэш состояния в View
- `GameObject.Find()` / `FindObjectOfType()` в runtime

---

## 15. Сценарии использования (фичи на высоком уровне)

### Сценарий 1: Вход на локацию и клик по предмету
```
Naninovel скрипт: @enterLocation id:backyard
        ↓
LocationService.Enter("backyard")
        ↓
Загружаем hotspotPrefabRef, инстанциируем в HotspotLayerUI
        ↓
HotspotLayerUI активирует нужные хотспоты по условиям
(пропускает id из ConsumedItemIds — одноразовые предметы)
        ↓
Игрок кликает по предмету
        ↓
HotspotView.OnClicked → Контроллер → LocationService.OnItemClicked()
        ↓
Выполняем onClickScript (Naninovel скрипт из InteractableItemConfig)
Добавляем itemId в ConsumedItemIds
        ↓
Во время выполнения скрипта может быть @startMiniGame или диалог с выборами (@choiceEx)
        ↓
Скрипт завершается, игрок снова видит локацию
```

### Сценарий 2: Выполнение мини-игры и воздействие на квесты + шкалы
```
Naninovel скрипт: @startMiniGame id:shooting char:grace
        ↓
StartMiniGameCommand.ExecuteAsync()
        ↓
MiniGameService.RunAsync(id="shooting")
        ↓
Загружаем mechanicPrefabRef, инстанциируем IMiniGame компонент
        ↓
Вызываем IMiniGame.Initialize(ShootingMiniGameConfig)
        ↓
Вызываем IMiniGame.RunAsync(container, ct) — ждём результата
        ↓
Игрок играет, получает score
        ↓
MiniGameService.RunAsync() возвращает MiniGameSessionResult
        ↓
StartMiniGameCommand:
   - QuestService.ReportEvent("play_minigame_shooting") ← ВСЕГДА, независимо от победы
   - SeductionScaleService.ApplyMiniGameResult(char, isVictory) ← по результату
        ↓
Скрипт продолжается (например, показывает реакцию персонажа)
```
