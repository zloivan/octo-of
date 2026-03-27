# Epic 1: Свободное перемещение + Поинт-энд-клик
## GitHub Projects — Tickets

> Источник: `architecture_locations.md`, `architecture_core.md`, `DECOMPOSITION_CONSOLIDATED.md`
> Порядок: outside-in (config → view → logic → service → command → integration)
> Зависимость эпика: QuestService (2.1) должен быть реализован до задач 1.2.4 и 1.3.3

---

## Метки (Labels)

| Label | Назначение |
|---|---|
| `feature` | Новая функциональность |
| `spike` | Исследование / proof of concept |
| `config` | ScriptableObject / data assets |
| `ui` | UI компоненты (MonoBehaviour, CustomUI) |
| `shader` | Шейдеры и материалы |
| `service` | Сервисный слой / доменная логика |
| `command` | Naninovel кастомная команда |
| `integration` | Связывание систем |
| `qa-tooling` | QA debug инструменты |
| `profiler-snapshot` | Плановый снэпшот производительности |

---

## Assembly Definitions

Все классы модуля размещаются в:

```
Assets/Scripts/Locations/
  OnlyFarms.Locations.asmdef
    References:
      - Naninovel.Runtime
      - Unity.Addressables
      - UniTask (Cysharp.Threading.Tasks)
```

`LocationLogic` и `LocationTransitionLogic` — plain C# классы без Unity-зависимостей внутри этой же assembly. Если понадобится unit-тестирование — создаётся отдельная `OnlyFarms.Locations.Tests.asmdef` с ссылкой на основную.

---

---

## 🏁 Milestone: Day 1 — Config Foundation

---

### Ticket 1.1.1 — [SPIKE] Naninovel Background Scaling + Custom UI Layer Positioning

**Labels:** `spike`, `feature`
**Estimate:** 2.2h
**Milestone:** Day 1

#### Описание

Цель спайка: определить, как Naninovel масштабирует Background actor при изменении разрешения экрана, и выяснить, нужна ли синхронизация RectTransform между `HotspotLayerUI` и фоном.

Два возможных исхода:
- **Stretch / Crop** — фон обрезается, `HotspotLayerUI` должна динамически синхронизировать свой `RectTransform` с фактическим Rect фона.
- **Letterbox (Fit)** — фон вписывается с чёрными полосами, `HotspotLayerUI` может быть независимой (растянута на весь Canvas), хотспоты просто не попадают в полосы.

**Что делать:**
1. Создать тестовую сцену с Naninovel Background actor и Custom UI поверх него.
2. Запустить при нескольких разрешениях (16:9, 4:3, 21:9, portrait) и проверить, как фон ведёт себя.
3. Разместить тестовый `RawImage` (или Sprite) в Custom UI точно над углами фона — проверить совпадение координат.
4. Задокументировать результат: режим масштабирования, нужна ли синхронизация, нужен ли скрипт-адаптер.
5. Определить финальный Layout для `HotspotLayerUI`: фиксированный RectTransform или динамический.

**Выход спайка:** обновить комментарий в `architecture_locations.md` §2 (Спайк 1.1.1) с реальным результатом. Этот вывод блокирует задачу 1.1.4.

#### Acceptance Criteria
- [ ] Задокументировано поведение Naninovel Background scaling при min 3 разных aspect ratio.
- [ ] Чётко определено: нужна ли динамическая синхронизация `HotspotLayerUI` → Background Rect.
- [ ] Результат зафиксирован в архитектурном файле или PR description.

---

### Ticket 1.1.2 — LocationConfig + HotspotEntry ScriptableObjects

**Labels:** `config`, `feature`
**Estimate:** 3h
**Milestone:** Day 1

#### Описание

Создать все data-классы и ScriptableObject конфиги для системы локаций. Это фундамент, без которого нельзя реализовывать ни `LocationService`, ни `HotspotLayerUI`.

**Что делать:**

**Файл:** `Assets/Scripts/Locations/LocationConfig.cs`
```
LocationConfig : ScriptableObject
  └── LocationDefinition[]
        ├── id: string
        ├── videoRef: AssetReference
        ├── hotspotPrefabRef: AssetReference
        ├── onEnterScript: string           // имя .nani скрипта (опционально, пустая строка = нет)
        ├── hasBackButton: bool
        └── hotspots: HotspotEntry[]
```

**Файл:** `Assets/Scripts/Locations/HotspotEntry.cs`
```csharp
[System.Serializable]
public class HotspotEntry
{
    public string id;
    public string localizationKey;
    public AssetReferenceSprite spriteRef;
    public HotspotType type;               // Transition | MiniGame | Item
    public ActivationCondition condition;
    public string conditionValue;          // questId / flagName в зависимости от condition
    public AssetReference itemConfig;      // только для type == Item
}

public enum HotspotType { Transition, MiniGame, Item }
public enum ActivationCondition { Always, RequiresQuestId, RequiresFlag }
```

**Файл:** `Assets/Scripts/Locations/LocationDefinition.cs`
```csharp
[System.Serializable]
public class LocationDefinition
{
    public string id;
    public AssetReference videoRef;
    public AssetReference hotspotPrefabRef;
    public string onEnterScript;
    public bool hasBackButton;
    public HotspotEntry[] hotspots;
}
```

**Создать asset:**
- `Assets/Configs/Locations/LocationConfig.asset` — один на проект, поле `locations: LocationDefinition[]`
- Добавить в Bootstrapper регистрацию конфига (заготовка, полная регистрация — в задаче 1.1.3)

**Место в проекте:**
```
Assets/
  Scripts/
    Locations/
      LocationConfig.cs
      LocationDefinition.cs
      HotspotEntry.cs
      HotspotType.cs          (enum)
      ActivationCondition.cs  (enum)
  Configs/
    Locations/
      LocationConfig.asset
```

#### Acceptance Criteria
- [ ] `LocationConfig.asset` создан и открывается в Inspector без ошибок.
- [ ] Можно добавить `LocationDefinition` с `HotspotEntry[]` через Inspector.
- [ ] Все `AssetReference` поля видны в Inspector и принимают ассеты корректных типов.

---

---

## 🏁 Milestone: Day 2 — Service Core

---

### Ticket 1.1.3 — LocationLogic + LocationService + EnterLocationCommand

**Labels:** `service`, `command`, `feature`
**Estimate:** 4.3h
**Milestone:** Day 2
**Depends on:** Ticket 1.1.2

#### Описание

Реализовать три слоя системы локаций: доменная логика (plain C#), сервис (Naninovel lifecycle), команда (.nani интеграция).

---

**1. LocationLogic (plain C#)**
**Файл:** `Assets/Scripts/Locations/LocationLogic.cs`

```csharp
public class LocationLogic
{
    // Ctor принимает LocationConfig (инжектируется через LocationService)
    public LocationLogic(LocationConfig config) { }

    // Вернуть определение локации по id. Throw если не найдена.
    public LocationDefinition GetDefinition(string locationId) { }

    // Проверить, существует ли локация с данным id
    public bool Exists(string locationId) { }

    // Получить все id локаций, куда можно перейти из текущей
    // (зависит от списка хотспотов с type == Transition)
    public string[] GetTransitionTargets(string locationId) { }
}
```

Никаких Unity-зависимостей. Никакого `MonoBehaviour`. Тестируется напрямую.

---

**2. LocationService.State (Persistence)**
**Файл:** `Assets/Scripts/Locations/LocationService.cs`

```csharp
[System.Serializable]
public class LocationServiceState
{
    public string CurrentLocationId;
    public string[] LocationHistory;      // стек для кнопки "Назад"
    public string[] ConsumedItemIds;      // одноразовые предметы, уже подобранные
}
```

**LocationService реализует:**
```csharp
public class LocationService : IEngineService, IStatefulService<LocationServiceState>
{
    // Инициализация через Naninovel DI
    public UniTask InitializeServiceAsync(IProgress<float> progress = null) { }

    // Войти на локацию: загрузить определение, обновить State, уведомить HotspotLayerUI
    public void Enter(string locationId) { }

    // Вернуться назад: pop LocationHistory, вызвать Enter с предыдущей локацией
    public void GoBack() { }

    // Вызывается из HotspotView при клике по предмету
    public void OnItemClicked(string itemId) { }

    // Persistence
    public LocationServiceState GetState() { }
    public void SetState(LocationServiceState state) { }
    public void ResetService() { }

    // Event — HotspotLayerUI подписывается
    public event Action<LocationDefinition> OnLocationEntered;
}
```

`OnItemClicked` выполняет:
1. Добавляет `itemId` в `ConsumedItemIds`
2. Запускает `onClickScript` через Naninovel script player
3. Вызывает `Engine.GetService<IQuestEventReporter>().ReportEvent(objectiveTag)` — **только после** того, как `QuestService` (Эпик 2) зарегистрирован; добавить null-check: если сервис не найден — пропустить, не бросать исключение

---

**3. EnterLocationCommand**
**Файл:** `Assets/Scripts/Locations/Commands/EnterLocationCommand.cs`

```csharp
[CommandAlias("enterLocation")]
public class EnterLocationCommand : Command
{
    [RequiredParameter]
    public StringParameter Id;

    public override UniTask ExecuteAsync(AsyncToken asyncToken = default)
    {
        Engine.GetService<LocationService>().Enter(Id);
        return UniTask.CompletedTask;
    }
}
```

**Использование в .nani:**
```
@back videoPath:backyard_video
@enterLocation id:backyard
```

**Регистрация в Bootstrapper:**
```csharp
new Bootstrapper()
    .AddEngineService<LocationService>(locationConfig)
    .RegisterCommand<EnterLocationCommand>()
    .Wire(Engine);
```

**Место в проекте:**
```
Assets/Scripts/Locations/
  LocationLogic.cs
  LocationService.cs
  LocationServiceState.cs
  Commands/
    EnterLocationCommand.cs
```

#### Acceptance Criteria
- [ ] `@enterLocation id:test_location` выполняется без ошибок в Play Mode при наличии `LocationConfig.asset` с этим id.
- [ ] `LocationService.State.CurrentLocationId` обновляется после вызова `Enter()`.
- [ ] `LocationService.GoBack()` восстанавливает предыдущую локацию из `LocationHistory`.
- [ ] `ResetService()` очищает `CurrentLocationId`, `LocationHistory`, `ConsumedItemIds`.

---

---

## 🏁 Milestone: Day 3 — UI Layer

---

### Ticket 1.1.4 — HotspotLayerUI + HotspotView + HotspotCursorController

**Labels:** `ui`, `feature`
**Estimate:** 5.5h
**Milestone:** Day 3
**Depends on:** Ticket 1.1.2, Ticket 1.1.3, Ticket 1.1.1 (результат спайка определяет layout)

#### Описание

Реализовать слой хотспотов поверх Naninovel Canvas и компоненты визуального состояния хотспотов.

---

**1. HotspotLayerUI**
**Файл:** `Assets/Scripts/Locations/UI/HotspotLayerUI.cs`

Зарегистрировать как Naninovel Custom UI.

```csharp
public class HotspotLayerUI : CustomUI
{
    // Текущий инстанциированный префаб хотспотов
    private GameObject _currentHotspotInstance;

    // Вызывается из LocationService.OnLocationEntered
    public async UniTask LoadHotspotsAsync(LocationDefinition definition, CancellationToken ct)
    {
        // 1. Уничтожить предыдущий инстанс (_currentHotspotInstance)
        // 2. Загрузить hotspotPrefabRef через Addressables
        // 3. Инстанциировать как дочерний объект этого RectTransform
        // 4. Получить все HotspotView в инстансе
        // 5. SetActive(false) всем HotspotView
        // 6. Пройти по definition.hotspots (HotspotEntry[])
        //    - Проверить condition (Always / RequiresQuestId / RequiresFlag)
        //    - Если условие выполнено И itemId не в ConsumedItemIds → SetActive(true)
        // 7. Подписать каждый HotspotView.OnClicked на соответствующий handler
    }

    // Деактивировать хотспот (после подбора предмета)
    public void DeactivateHotspot(string hotspotId) { }

    // Скрыть/показать весь слой (во время нарративных сцен)
    public void SetVisible(bool visible) { }
}
```

---

**2. HotspotView**
**Файл:** `Assets/Scripts/Locations/UI/HotspotView.cs`

```csharp
public class HotspotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private string _id;
    [SerializeField] private SpriteRenderer _sprite;           // произвольная форма
    [SerializeField] private PolygonCollider2D _collider;     // auto-generated по alpha
    [SerializeField] private TextMeshProUGUI _label;           // локализованная подпись

    public string Id => _id;

    // Callbacks — HotspotLayerUI подписывается
    public Action OnHoverEnter;
    public Action OnHoverExit;
    public Action OnClicked;

    // View не знает о логике — только визуальные состояния
    public void SetBrightness(float value) { /* MaterialPropertyBlock на _sprite */ }
    public void SetInteractable(bool value) { /* PolygonCollider2D.enabled */ }
    public void SetLabelVisible(bool visible) { /* _label.gameObject.SetActive */ }
    public void SetShimmer(bool enabled) { /* переключить материал — см. задачу 1.1.6 */ }

    // IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    public void OnPointerEnter(PointerEventData eventData) { OnHoverEnter?.Invoke(); }
    public void OnPointerExit(PointerEventData eventData) { OnHoverExit?.Invoke(); }
    public void OnPointerClick(PointerEventData eventData) { OnClicked?.Invoke(); }
}
```

**Поддержка локализации:** `ManagedTextProvider` (Naninovel) на объекте `_label` — ключ берётся из `HotspotEntry.localizationKey`.

---

**3. HotspotCursorController**
**Файл:** `Assets/Scripts/Locations/UI/HotspotCursorController.cs`

```csharp
public class HotspotCursorController : MonoBehaviour
{
    [SerializeField] private Texture2D _pointerCursor;
    [SerializeField] private Vector2 _hotspot = Vector2.zero;

    public void OnHotspotHoverEnter() => Cursor.SetCursor(_pointerCursor, _hotspot, CursorMode.Auto);
    public void OnHotspotHoverExit() => Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
}
```

Инстанс `HotspotCursorController` живёт на `HotspotLayerUI` GameObject. `HotspotLayerUI` подписывает его методы на `HotspotView.OnHoverEnter/OnHoverExit` при загрузке префаба.

**Структура префаба хотспотов (пример для локации "backyard"):**
```
backyard_hotspots (Prefab)
  ├── hotspot_fence (HotspotView) — type: Transition
  ├── hotspot_bucket (HotspotView) — type: Item
  └── hotspot_shed_minigame (HotspotView) — type: MiniGame
```

**Место в проекте:**
```
Assets/
  Scripts/Locations/UI/
    HotspotLayerUI.cs
    HotspotView.cs
    HotspotCursorController.cs
  Prefabs/Locations/
    UI/HotspotLayer.prefab          ← Custom UI префаб, регистрируется в Naninovel
    Hotspots/
      backyard_hotspots.prefab      ← пример локации
```

#### Acceptance Criteria
- [ ] `HotspotLayerUI` загружает и уничтожает префаб хотспотов при смене локации без memory leak (проверить Addressables release).
- [ ] Хотспоты с `condition: Always` становятся активными после `@enterLocation`.
- [ ] Хотспоты из `ConsumedItemIds` остаются `SetActive(false)` при повторном входе на локацию.
- [ ] Hover показывает label, курсор меняется на pointer.
- [ ] `PolygonCollider2D` корректно ограничивает зону клика по форме спрайта.

---

---

## 🏁 Milestone: Day 4 — Shaders

---

### Ticket 1.1.5 — Outline Shader (Idle + Hover states)

**Labels:** `shader`, `feature`
**Estimate:** 3.3h
**Milestone:** Day 4
**Depends on:** Ticket 1.1.4 (нужен HotspotView для привязки материала)

#### Описание

Создать URP Shader Graph или HLSL шейдер для визуальных состояний хотспота.

**Требования:**
- **Idle state:** тонкий контурный outline вокруг спрайта. Ширина outline — параметр материала `_OutlineWidth` (float).
- **Hover state:** тот же outline, яркость выше. Управляется через `MaterialPropertyBlock` — свойство `_Brightness` (float, 0–2).
- Переход Idle → Hover: fade-in за 0.15–0.2 сек (lerp в Update или DoTween/UniTask coroutine в `HotspotView`).

**Что создать:**
- `Assets/Shaders/HotspotOutline.shadergraph` (URP Shader Graph)
  - Inputs: `_MainTex`, `_OutlineColor`, `_OutlineWidth`, `_Brightness`
  - Техника outline: jump flood algorithm или edge detection на alpha channel (выбрать по performance)
- `Assets/Materials/HotspotOutlineMaterial.mat` — инстанс шейдера с дефолтными значениями
- Добавить в `HotspotView` метод `SetBrightness(float value)` через `MaterialPropertyBlock` (не создавать новый Material instance)

```csharp
// В HotspotView
private MaterialPropertyBlock _mpb;
private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

public void SetBrightness(float value)
{
    _mpb ??= new MaterialPropertyBlock();
    _sprite.GetPropertyBlock(_mpb);
    _mpb.SetFloat(BrightnessId, value);
    _sprite.SetPropertyBlock(_mpb);
}
```

**Место в проекте:**
```
Assets/
  Shaders/
    HotspotOutline.shadergraph
  Materials/
    HotspotOutlineMaterial.mat
```

#### Acceptance Criteria
- [ ] Outline виден в Game View на хотспоте с произвольной формой спрайта.
- [ ] `SetBrightness(2f)` визуально ярче, чем `SetBrightness(1f)`.
- [ ] Не создаётся новый Material instance при каждом hover (проверить через `RenderDoc` или Frame Debugger — draw call использует PropertyBlock).

---

### Ticket 1.1.6 — Shimmer Shader (скрытые/секретные предметы)

**Labels:** `shader`, `feature`
**Estimate:** 5h
**Milestone:** Day 4
**Depends on:** Ticket 1.1.4

#### Описание

Создать отдельный шейдер для хотспотов секретных предметов (`HotspotType.Item` с флагом `isSecret`). Shimmer — визуальный намёк на существование скрытого предмета.

**Требования к эффекту** (уточнить с арт-дирекцией — открытый вопрос в декомпозиции):
- Предположительно: плавный пульсирующий блик, движущийся по спрайту (scan-line glimmer).
- Альтернатива: мягкое свечение с синусоидальной анимацией opacity.
- Параметры материала: `_GlimmerSpeed`, `_GlimmerWidth`, `_GlimmerColor`.

**Что создать:**
- `Assets/Shaders/HotspotShimmer.shadergraph`
- `Assets/Materials/HotspotShimmerMaterial.mat`
- В `HotspotView` добавить метод `SetShimmer(bool enabled)`:
  ```csharp
  public void SetShimmer(bool enabled)
  {
      _sprite.material = enabled ? _shimmerMaterial : _outlineMaterial;
  }
  ```
  `_shimmerMaterial` и `_outlineMaterial` — `[SerializeField]` поля на `HotspotView`.

`HotspotLayerUI` вызывает `SetShimmer(true)` для хотспотов у которых `HotspotEntry.type == Item` и условие активации не выполнено (предмет существует, но условие квеста ещё не наступило — это поведение уточнить).

> ⚠️ Открытый вопрос: shimmer показывается ВСЕГДА для секретных предметов или только при выполнении определённого условия? Уточнить с геймдизайнером до реализации условия активации.

**Место в проекте:**
```
Assets/
  Shaders/
    HotspotShimmer.shadergraph
  Materials/
    HotspotShimmerMaterial.mat
```

#### Acceptance Criteria
- [ ] Shimmer эффект анимирован (не статичный) и виден в Game View.
- [ ] Переключение `SetShimmer(true/false)` работает в runtime без создания новых Material instances.

---

---

## 🏁 Milestone: Day 5 — Movement Logic + Week 1 Snapshot

---

### Ticket SNAPSHOT-W1 — Week 1 Profiler Snapshot

**Labels:** `profiler-snapshot`
**Milestone:** Day 5 (конец недели 1)

#### Описание

Собрать плановый снэпшот производительности после завершения фундаментальной инфраструктуры (Feature 1.1).

**Что делать:**
1. Запустить Play Mode с активной локацией (хотспоты загружены, анимации shimmer/outline активны).
2. **Memory Profiler** → `Capture Memory Snapshot` → сохранить как `snapshots/epic1_week1_locations_infra.snap`
3. **CPU Profiler** → записать 300 frames в сцене с активными хотспотами → `Save` → `snapshots/epic1_week1_cpu.data`
4. Отметить baseline-метрики в `profiling/BASELINE.md`:
   - Total allocated memory (MB)
   - Main thread ms/frame при idle (хотспоты не анимированы активно)
   - Main thread ms/frame при hover (outline + cursor change)
5. Эти данные станут точкой сравнения для **Profile Analyzer** в следующих снэпшотах.

```
snapshots/
  epic1_week1_locations_infra.snap
  epic1_week1_cpu.data
profiling/
  BASELINE.md
```

---

### Ticket 1.2.1 — LocationTransitionLogic

**Labels:** `service`, `feature`
**Estimate:** 3.2h
**Milestone:** Day 5
**Depends on:** Ticket 1.1.3

#### Описание

Реализовать логику переходов между локациями: проверка доступности, управление историей, SFX.

**Что добавить в `LocationLogic`:**
```csharp
public class LocationTransitionLogic
{
    // Можно ли перейти из fromId в toId
    // Проверяет: существует ли Transition хотспот с targetId == toId в fromId локации
    public bool CanTransition(string fromId, string toId, LocationDefinition fromDefinition) { }

    // Добавить локацию в историю (push)
    public string[] PushHistory(string[] history, string locationId) { }

    // Pop из истории (кнопка Назад)
    public (string previousId, string[] newHistory) PopHistory(string[] history) { }
}
```

Класс — plain C#, без Unity-зависимостей. Живёт в `Assets/Scripts/Locations/LocationTransitionLogic.cs`.

`LocationService` создаёт инстанс `LocationTransitionLogic` в конструкторе и использует его методы внутри `Enter()` и `GoBack()`.

**SFX интеграция** — вызывать из `LocationService.Enter()`:
```csharp
// Если переход вперёд
_audioManager.PlaySfx("click_movement_forward");
// Если GoBack()
_audioManager.PlaySfx("click_movement_back");
```

SFX-ключи: `click_movement_forward`, `click_movement_back` (см. `architecture_locations.md` §10).

#### Acceptance Criteria
- [ ] `LocationService.GoBack()` восстанавливает предыдущую локацию и SFX воспроизводится.
- [ ] Попытка перехода на несуществующую локацию не бросает uncaught exception (логировать warning).

---

### Ticket 1.2.2 — Кнопка «Назад»

**Labels:** `ui`, `feature`
**Estimate:** 2.5h
**Milestone:** Day 5
**Depends on:** Ticket 1.2.1, Ticket 1.1.4

#### Описание

Реализовать UI кнопку «Назад» внутри `HotspotLayerUI`.

**Что делать:**
- Добавить в `HotspotLayerUI` prefab `[SerializeField] private Button _backButton`.
- Показывать кнопку только если `LocationDefinition.hasBackButton == true` и `LocationHistory.Length > 0`.
- При клике: `LocationService.GoBack()`.
- `HotspotLayerUI.LoadHotspotsAsync()` после загрузки префаба вызывает:
  ```csharp
  _backButton.gameObject.SetActive(definition.hasBackButton && _locationService.HasHistory());
  _backButton.onClick.AddListener(() => _locationService.GoBack());
  ```
- Добавить в `LocationService` метод `bool HasHistory()` → `State.LocationHistory.Length > 0`.

**Место кнопки:** на `HotspotLayer.prefab` (Custom UI), не внутри префаба локации — кнопка универсальна для всех локаций.

#### Acceptance Criteria
- [ ] Кнопка «Назад» видна на локациях с `hasBackButton: true` и скрыта, если история пуста.
- [ ] После нажатия происходит переход на предыдущую локацию.
- [ ] Кнопка скрыта на локациях с `hasBackButton: false`.

---

---

## 🏁 Milestone: Day 6 — Accessibility + Narrative Return

---

### Ticket 1.2.3 — Логика доступности локаций

**Labels:** `service`, `feature`
**Estimate:** 2.5h
**Milestone:** Day 6
**Depends on:** Ticket 1.1.3, Ticket 1.1.2

#### Описание

Хотспоты перехода (`HotspotType.Transition`) должны активироваться только при выполнении условия из `HotspotEntry.condition`.

**Что доработать в `HotspotLayerUI.LoadHotspotsAsync()`:**

Для каждого `HotspotEntry` с `type == Transition`:
```
condition == Always         → активировать всегда
condition == RequiresQuestId → проверить Engine.GetService<QuestService>().IsCompleted(conditionValue)
                              (null-check: если QuestService не зарегистрирован — пропустить, активировать)
condition == RequiresFlag    → зарезервировано для будущего (пока активировать)
```

> ⚠️ **Зависимость:** `QuestService` (Эпик 2) должен быть реализован до того, как условие `RequiresQuestId` заработает в полную силу. До тех пор хотспот активируется по умолчанию (graceful degradation).

**Добавить интерфейс для тестируемости:**
```csharp
public interface IConditionEvaluator
{
    bool Evaluate(ActivationCondition condition, string conditionValue);
}

public class ConditionEvaluator : IConditionEvaluator
{
    public bool Evaluate(ActivationCondition condition, string conditionValue)
    {
        return condition switch
        {
            ActivationCondition.Always => true,
            ActivationCondition.RequiresQuestId =>
                Engine.GetService<QuestService>()?.IsQuestCompleted(conditionValue) ?? true,
            _ => true
        };
    }
}
```

`HotspotLayerUI` принимает `IConditionEvaluator` через конструктор (или `[SerializeField]` компонент).

**Файл:** `Assets/Scripts/Locations/ConditionEvaluator.cs`

#### Acceptance Criteria
- [ ] Хотспот с `condition: Always` активен всегда.
- [ ] Хотспот с `condition: RequiresQuestId` неактивен, если квест не выполнен (при наличии QuestService).
- [ ] При отсутствии QuestService в движке хотспот активируется по умолчанию (нет NullReferenceException).

---

### Ticket 1.2.4 — Триггер возврата в нарратив

**Labels:** `service`, `integration`, `feature`
**Estimate:** 3.2h
**Milestone:** Day 6
**Depends on:** Ticket 1.1.3, QuestService (Эпик 2, задача 2.1.7)

#### Описание

При выполнении всех квестов сегмента свободного перемещения — автоматически вернуться в нарративный скрипт.

> ⚠️ **Overlap с задачей 2.1.7 из Эпика 2.** Это задача со стороны LocationService/инфраструктуры локаций; задача 2.1.7 — со стороны QuestService. Реализовывать вместе или согласованно.

**Механизм:**
1. `QuestService` публикует event `OnAllQuestsCompleted(string returnScriptName)`.
2. `LocationService` подписывается на этот event при инициализации.
3. При получении события `LocationService` вызывает:
   ```csharp
   private void OnAllQuestsCompleted(string returnScriptName)
   {
       HideHotspotLayer();   // HotspotLayerUI.SetVisible(false)
       _scriptPlayer.Play(returnScriptName);  // Naninovel IScriptPlayer
   }
   ```

**Что добавить в `LocationService`:**
```csharp
// В InitializeServiceAsync:
var questService = Engine.GetService<QuestService>();
if (questService != null)
    questService.OnAllQuestsCompleted += OnAllQuestsCompleted;

// В DestroyService (cleanup):
if (_questService != null)
    _questService.OnAllQuestsCompleted -= OnAllQuestsCompleted;
```

**Добавить в `LocationService`:**
```csharp
public void HideHotspotLayer()
{
    Engine.GetService<IUIManager>()
        .GetUI<HotspotLayerUI>()
        ?.SetVisible(false);
}
```

> Если QuestService ещё не реализован: добавить заготовку кода с комментарием `// TODO: wire when QuestService (Epic 2) is ready`.

#### Acceptance Criteria
- [ ] После вызова `QuestService.OnAllQuestsCompleted` (или мокового события) `HotspotLayerUI` скрывается.
- [ ] Naninovel script player возобновляет выполнение указанного скрипта.

---

### Ticket 1.2.5 — OnEnter сцены при входе на локацию

**Labels:** `config`, `integration`
**Estimate:** 2h
**Milestone:** Day 6
**Depends on:** Ticket 1.1.3

#### Описание

Если у `LocationDefinition.onEnterScript` задано имя скрипта — воспроизвести его при входе на локацию. После завершения скрипта — показать хотспоты.

**Доработать `LocationService.Enter()`:**
```csharp
public async UniTask Enter(string locationId)
{
    var definition = _locationLogic.GetDefinition(locationId);
    // ... обновить State ...

    if (!string.IsNullOrEmpty(definition.onEnterScript))
    {
        _hotspotLayerUI.SetVisible(false);
        await _scriptPlayer.PlayAsync(definition.onEnterScript);
        _hotspotLayerUI.SetVisible(true);
    }

    await _hotspotLayerUI.LoadHotspotsAsync(definition, ct);
}
```

Хотспоты не активны пока играет сцена входа. После завершения — слой появляется.

> Использование: кат-сцена при первом посещении локации, короткий диалог NPC при входе.

#### Acceptance Criteria
- [ ] При `onEnterScript: "backyard_intro"` скрипт воспроизводится перед появлением хотспотов.
- [ ] При пустом `onEnterScript` хотспоты появляются сразу.

---

---

## 🏁 Milestone: Day 7 — Point & Click

---

### Ticket 1.3.1 — InteractableItemConfig ScriptableObject

**Labels:** `config`, `feature`
**Estimate:** 2h
**Milestone:** Day 7
**Depends on:** Ticket 1.1.2

#### Описание

Создать конфиг для интерактивных предметов (хотспоты с `HotspotType.Item`).

**Файл:** `Assets/Scripts/Locations/InteractableItemConfig.cs`

```csharp
[CreateAssetMenu(fileName = "ItemConfig", menuName = "OnlyFarms/Locations/InteractableItemConfig")]
public class InteractableItemConfig : ScriptableObject
{
    public ItemType type;          // QuestItem | Secret
    public string onClickScript;   // имя .nani скрипта (пустая строка = нет скрипта)
    public string objectiveTag;    // тег для QuestService.ReportEvent (пустая строка = не отчитывается)
    public int attractionDelta;    // Δ ATT (может быть 0)
    public int suspicionDelta;     // Δ SUS (может быть 0)
    public string characterId;     // к какому персонажу применяется delta
}

public enum ItemType { QuestItem, Secret }
```

`HotspotEntry.itemConfig` ссылается на этот конфиг через `AssetReference<InteractableItemConfig>`.

**Пример asset:**
```
Assets/Configs/Locations/Items/
  bucket_item_config.asset  — type: QuestItem, objectiveTag: "fill_bucket", onClickScript: "bucket_click"
  secret_photo_config.asset — type: Secret, onClickScript: "secret_photo_found"
```

#### Acceptance Criteria
- [ ] `InteractableItemConfig.asset` создаётся через меню `Create → OnlyFarms/Locations/InteractableItemConfig`.
- [ ] Все поля доступны в Inspector.

---

### Ticket 1.3.2 — One-shot активация предметов (ConsumedItemIds)

**Labels:** `service`, `feature`
**Estimate:** 2.2h
**Milestone:** Day 7
**Depends on:** Ticket 1.1.3, Ticket 1.3.1

#### Описание

Предметы одноразовые: после взаимодействия они исчезают навсегда (даже после загрузки сохранения).

**Механизм уже частично заложен в `LocationService.State.ConsumedItemIds`.**

**Что реализовать:**
1. В `LocationService.OnItemClicked(string itemId)`:
   - Добавить `itemId` в `State.ConsumedItemIds` (если ещё не в списке).
2. В `HotspotLayerUI.LoadHotspotsAsync()` при активации хотспотов:
   - Для каждого `HotspotEntry` с `type == Item`: проверить `ConsumedItemIds.Contains(entry.id)` → если да, `SetActive(false)`.
3. В `LocationService` добавить публичный метод:
   ```csharp
   public bool IsItemConsumed(string itemId) => State.ConsumedItemIds.Contains(itemId);
   ```

**Что важно:** `ConsumedItemIds` сохраняется через `IStatefulService<LocationServiceState>` и персистентен между сессиями. Это уже заложено в State (задача 1.1.3) — здесь нужно только убедиться, что логика записи и чтения работает корректно.

#### Acceptance Criteria
- [ ] После клика по предмету он исчезает с локации.
- [ ] После сохранения и загрузки игры предмет остаётся исчезнувшим.
- [ ] Повторный вход на ту же локацию не восстанавливает одноразовый предмет.

---

### Ticket 1.3.3 — Логика клика по предмету

**Labels:** `service`, `integration`, `feature`
**Estimate:** 3.2h
**Milestone:** Day 7
**Depends on:** Ticket 1.3.1, Ticket 1.3.2, QuestService (Эпик 2), SeductionScaleService (Эпик 3)

#### Описание

Полная цепочка обработки клика по интерактивному предмету.

**Реализовать `LocationService.OnItemClicked(string itemId)`:**
```csharp
public async UniTask OnItemClicked(string itemId)
{
    // 1. Загрузить InteractableItemConfig по itemId
    //    (_itemConfigMap: Dictionary<string, InteractableItemConfig>, заполняется при Enter())
    var config = _itemConfigMap[itemId];

    // 2. Добавить в ConsumedItemIds
    AddToConsumed(itemId);

    // 3. Уведомить HotspotLayerUI — деактивировать хотспот немедленно
    _hotspotLayerUI.DeactivateHotspot(itemId);

    // 4. Воспроизвести SFX
    _audioManager.PlaySfx("click_object");

    // 5. Выполнить onClickScript (если задан)
    if (!string.IsNullOrEmpty(config.onClickScript))
    {
        _hotspotLayerUI.SetVisible(false);
        await _scriptPlayer.PlayAsync(config.onClickScript);
        _hotspotLayerUI.SetVisible(true);
    }

    // 6. Применить delta шкал (если задана)
    //    null-check: если SeductionScaleService не зарегистрирован — пропустить
    if ((config.attractionDelta != 0 || config.suspicionDelta != 0)
        && !string.IsNullOrEmpty(config.characterId))
    {
        Engine.GetService<SeductionScaleService>()
            ?.ApplyDelta(config.characterId, config.attractionDelta, config.suspicionDelta);
    }

    // 7. Отчитаться в QuestService (если objectiveTag задан)
    if (!string.IsNullOrEmpty(config.objectiveTag))
    {
        Engine.GetService<IQuestEventReporter>()
            ?.ReportEvent(config.objectiveTag);
    }
}
```

**`_itemConfigMap`** заполняется в `LocationService.Enter()`: для каждого `HotspotEntry` с `type == Item` загружается `itemConfig` через Addressables и добавляется в словарь. Разгрузка происходит при смене локации.

> ⚠️ `SeductionScaleService` (Эпик 3) и `QuestService` (Эпик 2) — опциональные зависимости через null-check. Код компилируется и работает без них.

#### Acceptance Criteria
- [ ] Клик по предмету воспроизводит `click_object` SFX.
- [ ] Если `onClickScript` задан — Naninovel скрипт воспроизводится, хотспоты скрыты во время скрипта.
- [ ] После завершения скрипта хотспоты возвращаются, кликнутый предмет отсутствует.
- [ ] `objectiveTag` репортируется в `QuestService` (при наличии сервиса).

---

---

## 🏁 Milestone: Day 8 — Achievement + QA Tooling

---

### Ticket 1.3.4 — Ачивка за сбор всех секретов

**Labels:** `service`, `feature`
**Estimate:** 2h
**Milestone:** Day 8
**Depends on:** Ticket 1.3.2, Ticket 1.3.3

#### Описание

При подборе последнего `ItemType.Secret` из `LocationConfig` — тригернуть событие "все секреты собраны".

**Что добавить в `LocationLogic`:**
```csharp
// Вернуть все HotspotEntry с type == Item и itemConfig.type == Secret
public HotspotEntry[] GetAllSecretEntries() { }

// Проверить, все ли секреты в ConsumedItemIds
public bool AreAllSecretsCollected(string[] consumedItemIds) { }
```

**Что добавить в `LocationService`:**
```csharp
public event Action OnAllSecretsCollected;

// Вызывать после AddToConsumed() в OnItemClicked, если item.type == Secret:
if (_locationLogic.AreAllSecretsCollected(State.ConsumedItemIds))
    OnAllSecretsCollected?.Invoke();
```

Нарративный скрипт подписывается на это событие или команда `@checkAllSecrets` проверяет состояние.  
**Что НЕ реализуем здесь:** UI ачивки / toast / анимация — это ответственность нарративного скрипта.

#### Acceptance Criteria
- [ ] `OnAllSecretsCollected` вызывается ровно один раз после подбора последнего секрета.
- [ ] Повторный вход на локацию не вызывает событие повторно.

---

### Ticket QA-1 — QA Debug Tool: Location & Hotspot Inspector Panel

**Labels:** `qa-tooling`
**Milestone:** Day 8
**Depends on:** Ticket 1.1.3, Ticket 1.1.4, Ticket 1.3.2

#### Описание

Добавить панель в Naninovel Debug Panel для тестирования системы локаций без прохождения нарратива.

Инструмент критически важен для QA: позволяет телепортироваться на любую локацию, сбрасывать/устанавливать состояние предметов, не переигрывая всю игру.

**Что реализовать:**

**Файл:** `Assets/Scripts/Locations/Debug/LocationDebugPanel.cs`

```csharp
// Регистрируется в Naninovel Debug Panel через [RuntimeInitializeOnLoadMethod]
// или через кастомный debugProvider в Bootstrapper

public class LocationDebugPanel
{
    // === TELEPORT ===
    // Dropdown: все locationId из LocationConfig
    // Button "Enter" → LocationService.Enter(selectedId)

    // === HOTSPOTS ===
    // Button "Force Show All" → активировать все HotspotView на текущей локации
    // Button "Force Hide All" → деактивировать все HotspotView

    // === CONSUMED ITEMS ===
    // Список: все itemId с чекбоксами (checked = в ConsumedItemIds)
    // Чекбокс toggle → добавить/убрать из LocationService.State.ConsumedItemIds
    //                   + обновить HotspotLayerUI.DeactivateHotspot() / перезагрузить слой

    // === HISTORY ===
    // Readonly list: LocationService.State.LocationHistory
    // Button "Clear History"

    // === STATE ===
    // Button "Reset Location State" → LocationService.ResetService()
}
```

> **Изоляция:** весь код в `#if UNITY_EDITOR || DEVELOPMENT_BUILD` или в отдельной assembly с `excludePlatforms`. Debug код не импортируется бизнес-логикой.

**Assembly Definition для debug кода:**
```
Assets/Scripts/Locations/Debug/
  OnlyFarms.Locations.Debug.asmdef
    References:
      - OnlyFarms.Locations
      - Naninovel.Runtime
    Define Constraints: UNITY_EDITOR || DEVELOPMENT_BUILD
```

**Место в проекте:**
```
Assets/Scripts/Locations/Debug/
  LocationDebugPanel.cs
  OnlyFarms.Locations.Debug.asmdef
```

#### Acceptance Criteria
- [ ] Panel открывается в Naninovel Debug Panel в Development Build и Editor.
- [ ] Выбор локации из dropdown + нажатие "Enter" телепортирует на локацию без ошибок.
- [ ] Toggle item в ConsumedItemIds немедленно отражается в HotspotLayerUI (предмет появляется/исчезает).
- [ ] Panel не компилируется в Release Build (проверить `#if` или assembly constraints).

---

---

## 📊 Сводная таблица тикетов

| # | Задача | Декомп. | Оценка | Milestone | Labels |
|---|---|---|---|---|---|
| 1 | [SPIKE] Background Scaling | 1.1.1 | 2.2h | Day 1 | `spike` `feature` |
| 2 | LocationConfig + HotspotEntry | 1.1.2 | 3h | Day 1 | `config` `feature` |
| 3 | LocationLogic + LocationService + EnterLocationCommand | 1.1.3 | 4.3h | Day 2 | `service` `command` `feature` |
| 4 | HotspotLayerUI + HotspotView + CursorController | 1.1.4 | 5.5h | Day 3 | `ui` `feature` |
| 5 | Outline Shader (Idle + Hover) | 1.1.5 | 3.3h | Day 4 | `shader` `feature` |
| 6 | Shimmer Shader (секретные предметы) | 1.1.6 | 5h | Day 4 | `shader` `feature` |
| W1 | **[SNAPSHOT] Week 1 Profiler** | — | — | Day 5 | `profiler-snapshot` |
| 7 | LocationTransitionLogic | 1.2.1 | 3.2h | Day 5 | `service` `feature` |
| 8 | Кнопка «Назад» | 1.2.2 | 2.5h | Day 5 | `ui` `feature` |
| 9 | Логика доступности локаций | 1.2.3 | 2.5h | Day 6 | `service` `feature` |
| 10 | Триггер возврата в нарратив | 1.2.4 | 3.2h | Day 6 | `service` `integration` `feature` |
| 11 | OnEnter сцены | 1.2.5 | 2h | Day 6 | `config` `integration` |
| 12 | InteractableItemConfig | 1.3.1 | 2h | Day 7 | `config` `feature` |
| 13 | One-shot активация предметов | 1.3.2 | 2.2h | Day 7 | `service` `feature` |
| 14 | Логика клика по предмету | 1.3.3 | 3.2h | Day 7 | `service` `integration` `feature` |
| 15 | Ачивка за все секреты | 1.3.4 | 2h | Day 8 | `service` `feature` |
| 16 | QA Debug Tool: Location Panel | — | ~3h | Day 8 | `qa-tooling` |

**Итого разработка:** ~55.5h (с 20% буфером)
**QA Debug Tool:** ~3h (не включён в оценку эпика)
**Снэпшоты:** Day 5 (конец Week 1)

---

## ⚠️ Зависимости и блокеры

| Задача | Блокирует / Зависит |
|---|---|
| 1.1.1 (спайк) | Блокирует 1.1.4 — без результата спайка неизвестен layout |
| 1.1.2 (конфиги) | Блокирует всё — фундамент |
| 1.1.3 (сервис) | Блокирует 1.2.x и 1.3.x |
| 1.2.4 (return) | Зависит от QuestService (Эпик 2, задача 2.1.7) |
| 1.3.3 (item click) | Зависит от QuestService (Эпик 2) + SeductionScaleService (Эпик 3) — null-safe |

## 🔓 Открытые вопросы (уточнить до начала реализации)

1. **Shimmer-эффект** — пульсация, блик или свечение? (блокирует 1.1.6)
2. **Idle outline** — статичный или анимированный контур?
3. **Shimmer условие** — секретный предмет всегда мерцает или только при выполнении условия?
