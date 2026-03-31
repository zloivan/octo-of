# Архитектура — Локации и Хотспоты

> Покрывает: рендеринг фона, хотспоты, активация, конфиги, команды, SFX, спайк масштабирования.
> Внешние зависимости: `QuestService` (ReportEvent), `Engine.GetService<IQuestEventReporter>()`
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 2. Рендеринг фона и слой хотспотов

### Решение
Naninovel Background actor рендерит видео-фон. Поверх него — Naninovel **Custom UI** (`HotspotLayerUI`), зарегистрированный как стандартный CustomUI префаб.

```
Naninovel Canvas
  ├── Background Layer   ← видео через Background actor
  ├── Character Layer
  ├── Text Printer Layer
  └── Custom UI Layer    ← HotspotLayerUI живёт здесь
```

### Почему Custom UI, не отдельный Canvas
Оба слоя в одном Canvas Naninovel → одно пространство координат → не могут разъехаться при смене разрешения.

### Спайк 1.1.1 — ЗАКРЫТ
Хотспоты — World Space `SpriteRenderer` как дочерние объекты Background actor. Позиции хранятся как `Vector2 normalizedPosition [0,1]`, конвертируются в `localPosition` через `mesh.bounds.size` в runtime. Подробности: `spike_1_1_1_report.md`.

---

## 3. Форма и визуализация хотспотов

### Решение
Каждый хотспот — **Sprite произвольной формы**, нарисованный художником по кадру из видео.

- `PolygonCollider2D` генерируется автоматически по alpha-контуру спрайта → click detection точный
- Спрайт позиционируется один раз при создании локации, anchor зафиксирован

### Визуальные состояния
| Состояние | Реализация |
|---|---|
| Idle | `HotspotOutlineMaterial` — тонкий контур |
| Hover | Тот же материал, яркость выше (MaterialPropertyBlock) + fade-in Label |
| Click | Callback → действие |
| Скрытый предмет | `HotspotShimmerMaterial` — эффект блика |

### Смена курсора
`HotspotCursorController` — `Cursor.SetCursor()` при hover, возврат к дефолту при выходе.

### Подписи
`ManagedTextProvider` (Naninovel локализация) на дочернем Text объекте каждого хотспота. Ключ локализации хранится в `HotspotEntry`.

---

## 4. Активация хотспотов

### Решение
Каждая локация имеет свой **отдельный префаб хотспотов**. `HotspotLayerUI` при входе на локацию загружает нужный префаб через `AssetReference` и инстанциирует его как дочерний объект. При смене локации предыдущий инстанс уничтожается.

Внутри префаба все `HotspotView` изначально `SetActive(false)`. `HotspotLayerUI` активирует нужные по условиям из конфига, матчинг — по совпадению `HotspotEntry.id` с `HotspotView.id`. Хотспоты предметов, чьи id присутствуют в `LocationService.State.ConsumedItemIds`, не активируются — они одноразовые.

### Почему префаб на локацию, а не все в одной сцене
Единая сцена со всеми хотспотами всех локаций — лишний расход памяти и коллизии при редактировании. Префаб на локацию изолирует контент, упрощает авторинг художником и согласуется с Addressable-группировкой по дням (PROJECT_GUIDELINES §6).

---

## 6. Слои данных (конфиги и домен)

Система локаций использует два чётко разделённых слоя данных.

### Инфраструктурный слой (Unity SO)

Используется для настройки в Inspector и загрузки ассетов через Addressables. Содержит Unity-специфичные типы (`AssetReference`, `AssetReferenceSprite`). **Не передаётся за пределы `LocationService`.**

```
LocationConfig : ScriptableObject      ← корневой конфиг, один на проект
  └── LocationDefinition[]
        ├── id: string
        ├── videoRef: AssetReference
        ├── hotspotPrefabRef: AssetReference
        ├── onEnterScript: string                ← имя .nani скрипта (опционально)
        ├── hasBackButton: bool
        └── hotspots: HotspotEntry[]
              ├── id, localizationKey
              ├── spriteRef: AssetReferenceSprite
              ├── type: HotspotType
              ├── condition: ActivationCondition
              ├── conditionValue: string
              └── itemConfig: AssetReference<InteractableItemConfig>  (только для Item)

InteractableItemConfig : ScriptableObject
  ├── type: QuestItem | Secret
  ├── onClickScript: string
  ├── objectiveTag: string
  ├── attractionDelta: int
  ├── suspicionDelta: int
  └── characterId: string
```

### Доменный слой (plain C#)

Используется только `LocationLogic`. Не содержит `UnityEngine`, `AssetReference` или `MonoBehaviour`. Создаётся `LocationService` при инициализации путём маппинга из инфраструктурного слоя.

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

`HotspotType` и `ActivationCondition` — чистые C# enum, используются обоими слоями.

### Маппинг

Каждый инфра-класс сам знает как спроецировать себя в доменный тип:

```csharp
// LocationDefinition.cs
public LocationData ToLocationData() => new LocationData
{
    Id            = id,
    OnEnterScript = onEnterScript,
    HasBackButton = hasBackButton,
    Hotspots      = hotspots.Select(h => h.ToHotspotData()).ToArray()
};

// HotspotEntry.cs
public HotspotData ToHotspotData() => new HotspotData
{
    Id             = id,
    Type           = type,
    Condition      = condition,
    ConditionValue = conditionValue
};
```

`LocationService.InitializeService()` использует эти методы:

```csharp
var data = _config.locations.Select(d => d.ToLocationData()).ToArray();
_logic = new LocationLogic(data);
```

```
LocationConfig (SO)
      │  .ToLocationData()  на каждом LocationDefinition
      ▼
LocationData[] → LocationLogic
```

За пределы `LocationService` уходит только `LocationData`. `OnLocationEntered` event несёт `LocationData`, не `LocationDefinition`.

> Все ссылки на ассеты — только через `AssetReference`. Имена .nani скриптов — `string` (Naninovel загружает по имени нативно).

---

## 7. Регистрация (фрагмент — локации)

### Регистрация сервиса

`[InitializeAtRuntime]` — Naninovel находит сервис автоматически. `GameConfig` инжектируется через конструктор:

```csharp
[InitializeAtRuntime]
public class LocationService : IStatefulService<LocationServiceState>
{
    public LocationService(GameConfig gameConfig)
    {
        _config = gameConfig.LocationConfig;
    }
}
```

`EnterLocationCommand` регистрировать вручную не нужно — Naninovel находит все `Command`-наследники через рефлексию.

### В .nani скриптах
```
@back videoPath:backyard_video      ← Naninovel показывает фон
@enterLocation id:backyard          ← LocationService.Enter()
```

### LocationServiceState (Naninovel serialization contract)

```csharp
[System.Serializable]
public class LocationServiceState
{
    public string CurrentLocationId;
    public string[] LocationHistory;  // TODO: временно — стек истории переходов.
                                      // По GDD кнопка "Назад" — возврат из тупиковой локации
                                      // в фиксированную родительскую, не произвольная история.
                                      // Заменить на parentLocationId после реализации
                                      // LocationTransitionManager в доменном слое.
    public string[] ConsumedItemIds;
}
```

Runtime-состоянием владеет `LocationLogic`. `LocationServiceState` — только контракт для Naninovel JSON-сериализации, заполняется через `LocationLogicSnapshot`.

### LocationLogicSnapshot

Промежуточный объект между доменным состоянием и Naninovel-контрактом. Чистый C#, без Unity-зависимостей:

```csharp
public readonly struct LocationLogicSnapshot
{
    public readonly string CurrentLocationId;
    public readonly string[] LocationHistory;  // TODO: временно, см. LocationServiceState
    public readonly string[] ConsumedItemIds;
}
```

> **TODO:** `LocationHistory` + `GoBack()` — временная реализация не соответствующая GDD.
> По GDD кнопка "Назад" доступна только в тупиковых локациях и возвращает в фиксированную
> родительскую точку. Текущий стек истории будет заменён на `LocationTransitionManager`
> в доменном слое с `parentLocationId` в конфиге. Текущая реализация функционально корректна
> для демо.

### ReportEvent из LocationService
```csharp
public void OnItemClicked(string itemId)
{
    _logic.MarkConsumed(itemId);
    // Полная реализация — Ticket 1.3.3
    Engine.GetService<IQuestEventReporter>()
        ?.ReportEvent(_itemConfigMap[itemId].objectiveTag); // null-safe
}
```

---

## 10. Аудио (фрагмент — локации)

| Ключ | Момент | Приоритет |
|---|---|---|
| `click_movement_forward` | Клик по хотспоту перехода вперёд | Med |
| `click_movement_back` | Клик по кнопке "Назад" | Med |
| `click_object` | Клик по интерактивному предмету | Med |
