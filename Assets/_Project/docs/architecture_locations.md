# Архитектура — Локации и Хотспоты

> Покрывает: рендеринг фона, хотспоты, активация, конфиги, команды, SFX.
> Внешние зависимости: `QuestService` (ReportEvent, WaitForAllQuestsCompleted)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 2. Рендеринг фона и слой хотспотов

### Решение

`LocationService` управляет фоном и хотспотами атомарно через единую команду `@exitNarrative`. Фон — выделенный Background actor `id: "location"` через `IBackgroundManager`. Хотспоты — независимый `HotspotContainer` в world space.

```
; Единственная команда нарративщика для входа в свободное перемещение
@exitNarrative id:backyard
```

Нарративный `@back` работает с другими акторами — конфликта нет.

### Иерархия в runtime

```
Scene (DontDestroyOnLoad)
  ├── [Naninovel] Background "location"   ← управляется LocationService
  ├── [Naninovel] Background "narrative"  ← управляется нарративными @back
  └── HotspotContainer                    ← создаётся HotspotManager, z = -0.1f
        ├── HotspotView_A
        └── HotspotView_B
```

### Почему World Space, не Custom UI

| Критерий | Custom UI (отклонено) | World Space (выбор) |
|---|---|---|
| `PolygonCollider2D` | Не работает с `GraphicRaycaster` | Работает с `Physics2DRaycaster` |
| Coordinate conversion | Ломается при crop | Не нужна |

`Physics2DRaycaster` — на `MainCamera`, добавляется в `LocationService.InitializeService()`.

### Почему независимый HotspotContainer, не child Background actor'а

`FindObjectOfType` запрещён. Независимый контейнер не требует поиска родителя и не зависит от внутренней иерархии Naninovel.

### Спайк 1.1.1 — ЗАКРЫТ

`MatchMode=Custom(ratio=1)` — PPU фиксирован = 133.33. Позиции — `normalizedPosition [0,1]` × `backgroundWorldSize` из конфига. Подробности: `spike_1_1_1_report.md`.

---

## 3. Форма и визуализация хотспотов

### Компоненты HotspotView

```
HotspotView (GameObject)
  ├── SpriteRenderer       — alpha=0 при спауне, fade-in вместе с фоном
  ├── PolygonCollider2D    — по alpha-контуру спрайта
  └── HotspotView.cs       — passive view, Action-колбэки
```

### Визуальные состояния

| Состояние | Реализация |
|---|---|
| Idle | `HotspotOutlineMaterial` — тонкий контур |
| Hover | Яркость выше (MaterialPropertyBlock) + Label |
| Click | `IHotspotInput` → `LocationService` |
| Секрет | `HotspotShimmerMaterial` |

### Input-абстракция

```csharp
public interface IHotspotInput
{
    event Action<string> OnHotspotClicked;
    event Action<string> OnHotspotHovered;
    event Action<string> OnHotspotHoverExited;
}
```

`MouseHotspotInput` — текущая реализация. При добавлении геймпада — новая реализация `IHotspotInput`, `LocationService` не меняется.

### Смена курсора

`HotspotCursorController` подписывается на `IHotspotInput`, вызывает `Cursor.SetCursor()`.

---

## 4. Активация хотспотов

### Классы

| Класс | Тип | Ответственность |
|---|---|---|
| `HotspotLogic` | plain C# | Какие хотспоты активны, условия |
| `HotspotManager` | MonoBehaviour | Жизненный цикл `HotspotContainer`, загрузка префаба, fade |
| `MouseHotspotInput` | MonoBehaviour, `IHotspotInput` | Агрегирует события `HotspotView` |
| `HotspotCursorController` | MonoBehaviour | Меняет курсор |

### Поток при входе на локацию

1. `LocationService.Enter()` вызывает `IBackgroundManager.GetActor("location").ChangeAppearanceAsync(videoPath, duration)`
2. Параллельно — `HotspotManager.LoadAsync(definition, locationData, consumedIds, duration)`
3. `HotspotManager`: уничтожает старый контейнер → загружает префаб → `alpha=0`
4. `HotspotLogic` определяет активные `HotspotView`
5. Активные view регистрируются в `MouseHotspotInput`
6. Fade-in параллельно с фоном

### API управления

```csharp
hotspotManager.DeactivateHotspot(string id); // после клика по предмету
hotspotManager.SetVisible(bool visible);     // на время onClickScript
```

---

## 5. Команды

| Команда | Контекст | Действие |
|---|---|---|
| `@exitNarrative` | нарратив | Входим в свободное перемещение на `CurrentLocationId`. Блокирует до `OnAllQuestsCompleted` |
| `@exitNarrative id:backyard` | нарратив | То же, но принудительно устанавливает локацию |
| `@activateDayQuests day:day1` | нарратив | Инициализирует квесты дня перед `@exitNarrative` |
| `@enterLocation id:backyard` | QA / debug | Прямой вход на локацию без ожидания квестов |

`@enterLocation` **не используется** в продакшн нарративе — только для отладки и тестирования через debug panel.

---

## 6. Слои данных

### Инфраструктурный слой (Unity SO)

```
LocationConfig : ScriptableObject
  └── LocationDefinition[]
        ├── id: string
        ├── videoPath: string                  ← appearance для IBackgroundManager
        ├── hotspotPrefabRef: AssetReference
        ├── backgroundWorldSize: Vector2        ← (25.6, 14.4) / (19.2, 10.8)
        ├── transitionDuration: float          ← длительность перехода фона и fade хотспотов
        ├── onEnterScript: string              ← .nani скрипт при входе (опционально)
        ├── hasBackButton: bool
        └── hotspots: HotspotEntry[]
              ├── id, localizationKey
              ├── normalizedPosition: Vector2   ← [0,1]
              ├── spriteRef: AssetReferenceSprite
              ├── type: HotspotType             ← Transition | MiniGame | Item
              ├── condition: ActivationCondition
              ├── conditionValue: string
              └── itemConfig: AssetReference<InteractableItemConfig>
```

### Доменный слой (plain C#)

```csharp
public class LocationData
{
    public string Id;
    public string OnEnterScript;
    public bool HasBackButton;
    public HotspotData[] Hotspots;
}

public class HotspotData
{
    public string Id;
    public HotspotType Type;
    public ActivationCondition Condition;
    public string ConditionValue;
}
```

---

## 7. Регистрация и Save/Load

### LocationService

```csharp
[InitializeAtRuntime]
public class LocationService : IStatefulService<LocationServiceState>
{
    public LocationService(GameConfig gameConfig, IBackgroundManager backgroundManager,
                           ICameraManager cameraManager) { ... }
}
```

### LocationServiceState

```csharp
[System.Serializable]
public class LocationServiceState
{
    public string CurrentLocationId;
    public string[] LocationHistory;   // TODO: временно — заменить на parentLocationId
    public string[] ConsumedItemIds;
    public bool IsInFreeRoam;
}
```

### Save/Load поведение

- `IsInFreeRoam=true` при загрузке → Naninovel восстанавливает строку `@exitNarrative` → `LocationService.Enter()` восстанавливает фон + хотспоты автоматически
- `IsInFreeRoam=false` → нарративный скрипт восстанавливает своё состояние, `LocationService` не вмешивается

---

## 10. Аудио

| Ключ | Момент | Приоритет |
|---|---|---|
| `click_movement_forward` | Клик по transition-хотспоту | Med |
| `click_movement_back` | Клик по кнопке "Назад" | Med |
| `click_object` | Клик по предмету | Med |
