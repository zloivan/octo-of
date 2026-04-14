# Архитектура — Локации и Хотспоты

> Покрывает: рендеринг фона, хотспоты, активация, конфиги, команды, SFX.
> Внешние зависимости: `GameFlowService`, `QuestService`
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 1. Рендеринг фона и слой хотспотов

`LocationService` управляет фоном и хотспотами. Фон — Background actor `id: "location"` через `IBackgroundManager`. Хотспоты — независимый `HotspotContainer` в world space.

### Иерархия в runtime

```
Scene (DontDestroyOnLoad)
  ├── [Naninovel] Background "location"   ← управляется LocationService
  ├── [Naninovel] Background "narrative"  ← управляется нарративными @back
  └── HotspotContainer                    ← создаётся HotspotManager, z = -0.1f
```

`Physics2DRaycaster` — на `MainCamera`, добавляется в `LocationService.InitializeService()`.

`PolygonCollider2D` не работает с `GraphicRaycaster` — поэтому World Space, не Custom UI.

### RenderLocation — отмена через CancellationToken

`OnLocationEnterStarted` стреляет до начала рендера. Если `GameFlowService` вызывает `SetFreeRoamMode(false)` синхронно — `_renderCts` отменяет рендер до появления чего-либо на экране:

```csharp
private async UniTask RenderLocation(string locationId, AsyncToken ct)
{
    _renderCts?.Cancel();
    _renderCts = new CancellationTokenSource();
    var renderToken = new AsyncToken(
        CancellationTokenSource.CreateLinkedTokenSource(ct.CancellationToken, _renderCts.Token).Token);

    OnLocationEnterStarted?.Invoke(_locationLogic.GetCurrentLocation());
    if (renderToken.Canceled) return; // GameFlowService отменил синхронно

    var bg = await _backgroundManager.GetOrAddActor(LOCATION_ACTOR);
    bg.ChangeVisibility(true, new Tween(0), token: renderToken).Forget();

    await UniTask.WhenAll(
        bg.ChangeAppearance(definition.BackgroundName, new Tween(definition.TransitionDuration), token: renderToken),
        _hotspotManager.LoadAsync(definition.HotspotPrefabRef, availableHotspots, definition.TransitionDuration, renderToken)
    );

    OnLocationEnterCompleted?.Invoke(_locationLogic.GetCurrentLocation());
}
```

### SetFreeRoamMode — чистое состояние

При `SetFreeRoamMode(false)` — отменяем рендер, сбрасываем курсор, выгружаем хотспоты, скрываем фон:

```csharp
public async UniTask SetFreeRoamMode(bool value, AsyncToken token = default)
{
    _isInFreeRoam = value;
    if (!value)
    {
        _renderCts?.Cancel();
        OnFreeRoamEnded?.Invoke();
        _hotspotCursorController.ResetCursor();
        _hotspotManager.Unload();
        var bg = await _backgroundManager.GetOrAddActor(LOCATION_ACTOR);
        bg.ChangeVisibility(false, new Tween(0.3f), token: token).Forget();
    }
}
```

---

## 2. Форма и визуализация хотспотов

```
HotspotView (GameObject)
  ├── SpriteRenderer       — alpha=0 при спауне, fade-in вместе с фоном
  ├── PolygonCollider2D    — по alpha-контуру спрайта
  └── HotspotView.cs       — passive view, Action-колбэки
```

| Состояние | Реализация |
|---|---|
| Idle | `HotspotOutlineMaterial` |
| Hover | Яркость выше (MaterialPropertyBlock) + Label |
| Секрет | `HotspotShimmerMaterial` |

`HotspotCursorController` подписывается на `IHotspotInput`. Метод `ResetCursor()` вызывается при `SetFreeRoamMode(false)` и при смене локации.

---

## 3. Активация хотспотов

| Класс | Тип | Ответственность |
|---|---|---|
| `ILocationRepository` | interface (domain) | Контракт доступа к данным локаций |
| `IHotspotRepository` | interface (domain) | Контракт доступа к данным хотспотов |
| `IHotspotValidator` | interface (domain) | Проверка доступности хотспота |
| `HotspotValidator` | plain C# (Infrastructure) | Реализация через `IQuestStatusSource` — живёт в Infrastructure, не в Domain |
| `LocationLogic` | plain C# | Навигация: текущая локация, история |
| `HotspotLogic` | plain C# | Хотспоты, consumed items, валидация |
| `LocationHotspotController` | plain C# | `IHotspotInput` → `LocationService` |
| `HotspotManager` | plain C# | Lifecycle `HotspotContainer`, fade |
| `MouseHotspotInput` | MonoBehaviour, `IHotspotInput` | Агрегирует события `HotspotView` |
| `HotspotCursorController` | plain C# | Меняет курсор, `ResetCursor()` |
| `BackButtonUI` | `CustomUI` | Показывается только при `CanGoBack() == true` |

### Зависимости

```
LocationConfigSO : ILocationRepository, IHotspotRepository
        ↓                          ↓
  LocationLogic            HotspotLogic(IHotspotRepository, IHotspotValidator)
        ↑                          ↑
        └──────── LocationService ─┘
                       ↑
             GameFlowService (подписан на OnLocationEnterStarted)
```

### Поток при входе на локацию

1. `LocationService.Enter(locationId, ct)` → `_locationLogic.Enter(locationId)` (идемпотентно)
2. `RenderLocation()`:
   - `OnLocationEnterStarted?.Invoke()` — до рендера, `GameFlowService` может отменить
   - если не отменено: `bg.ChangeAppearance` + `HotspotManager.LoadAsync` параллельно
3. `OnLocationEnterCompleted?.Invoke()` — после рендера

### Поток GoBack

1. `LocationService.GoBack(ct)` → `_locationLogic.GoBack()` → `RenderLocation(targetId)`
2. `OnNavigatedBack?.Invoke()` → `LocationSoundObserver` воспроизводит SFX

`GoBack` вызывает `RenderLocation` напрямую — не `Enter`, чтобы не трогать стек истории.

### LocationLogic — идемпотентность

```csharp
public void Enter(string locationId)
{
    if (_currentLocation?.Id == locationId) return; // история не ломается
    _currentLocation = GetLocation(locationId);
    _locationHistoryStack.Push(locationId);
}
```

---

## 4. События LocationService

```csharp
public event Action<LocationData> OnLocationEnterStarted;  // до рендера — GameFlowService перехватывает здесь
public event Action<LocationData> OnLocationEnterCompleted; // после рендера — BackButtonUI, UI
public event Action OnNavigatedForward;                     // до анимации перехода
public event Action OnNavigatedBack;                        // до анимации возврата
public event Action OnFreeRoamEnded;                        // при SetFreeRoamMode(false)
```

---

## 5. Команды

| Команда | Действие |
|---|---|
| `@exitNarrative [locationId]` | `HideAllActors`, рендер локации, `SetFreeRoamMode(true)`, `scriptPlayer.Stop()` |
| `@enterLocation id:backyard` | Только для QA / debug |

**Параметры `@exitNarrative`:**
- Безымянный (опц.) — `locationId`. Если не задан — `GetCurrentLocationId()`
- `returnScript` (опц.) — передаётся в `GameFlowService.SetSessionSource()`. В Epic 2 убирается.
- `returnLabel` (опц.)

Нарративный скрипт **не знает** о точках возврата в квестовой системе. `returnScript` — временный параметр до реализации `DayConfig` в Epic 2.

---

## 6. Слои данных

### LocationDefinition (инфраструктура, Unity SO)

```
LocationConfigSO
  └── LocationDefinition[]
        ├── Id: string
        ├── BackgroundName: string
        ├── HotspotPrefabRef: AssetReference
        ├── TransitionDuration: float
        ├── HasBackButton: bool
        └── Hotspots: HotspotEntry[]
```

`OnEnterScript` удалён из конфига — логика нарративных вставок при входе управляется `ILocationNarrativeSource` в `GameFlowService`.

### LocationData (домен, plain C#)

```csharp
public sealed class LocationData
{
    public readonly string Id;
    public readonly bool HasBackButton;
}
```

---

## 7. Save/Load

`IsInFreeRoam=true` при загрузке → `scriptPlayer.Stop()` → `RenderLocation()`.
`IsInFreeRoam=false` → нарративный скрипт восстанавливается сам.

`LoadServiceState` только восстанавливает данные — рендеринг происходит здесь, не через перевыполнение `@exitNarrative`.

---

## 8. Async

| Слой | Тип |
|---|---|
| `LocationLogic`, `HotspotLogic` | Синхронный |
| `LocationService`, `HotspotManager` | `AsyncToken` (Naninovel) |
| Addressables | `handle.Task.AsUniTask()` |

---

## 9. Naninovel — настройка актора "location"

1. `Naninovel → Resources → Backgrounds` — ID `location`, реализация Video Background
2. Appearance-ресурсы с именами совпадающими с `LocationDefinition.BackgroundName`
3. Использовать `GetOrAddActor("location")`, не `GetActor()`

---

## 10. Аудио

| Ключ в `GameSoundConfig` | Событие | Момент |
|---|---|---|
| `TransitionForward` | `OnNavigatedForward` | Клик transition-хотспота |
| `TransitionBack` | `OnNavigatedBack` | Нажатие Back |
| `ItemPickup` | `OnItemPickedUp` | Consume item |
