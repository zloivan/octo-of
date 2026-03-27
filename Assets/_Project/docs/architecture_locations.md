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

### Спайк 1.1.1 — Масштабирование фона Naninovel
Нужно определить, как Naninovel масштабирует Background actor при смене разрешения:
- Stretch (обрезка) → `HotspotLayerUI` должна синхронизировать Rect с фоном
- Fit (letterbox) → `HotspotLayerUI` может быть независимой

**Блок 1.1.1 (спайк) решает этот вопрос.**

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

## 6. Конфиги (фрагмент — локации)

```
LocationConfig : ScriptableObject      ← корневой конфиг локаций, один на проект
  └── LocationDefinition[]
        ├── id: string
        ├── videoRef: AssetReference
        ├── hotspotPrefabRef: AssetReference     ← префаб хотспотов этой локации
        ├── onEnterScript: string                ← имя .nani скрипта (опционально)
        ├── hasBackButton: bool
        └── hotspots: HotspotEntry[]
              ├── id, localizationKey
              ├── spriteRef: AssetReferenceSprite
              ├── type: HotspotType
              ├── condition: ActivationCondition
              ├── conditionValue: string
              └── itemConfig: AssetReference<InteractableItemConfig>  (только для Item)

InteractableItemConfig : ScriptableObject  ← для предметов типа Item
  ├── type: QuestItem | Secret
  ├── onClickScript: string               ← имя .nani скрипта
  ├── objectiveTag: string                ← тег для QuestService.ReportEvent
  ├── attractionDelta: int
  ├── suspicionDelta: int
  └── characterId: string
```

```csharp
public enum ActivationCondition { Always, RequiresQuestId, RequiresFlag }

public class HotspotEntry {
    public string id;
    public string localizationKey;
    public AssetReferenceSprite spriteRef;
    public HotspotType type;                              // Transition | MiniGame | Item
    public ActivationCondition condition;
    public string conditionValue;
    public AssetReference<InteractableItemConfig> itemConfig; // только для type == Item
}
```

> Все ссылки на ассеты — только через `AssetReference`. Имена .nani скриптов — `string` (Naninovel загружает по имени нативно).

---

## 7. Команды и Bootstrapper (фрагмент — локации)

```csharp
.AddEngineService<LocationService>(locationConfig)
.RegisterCommand<EnterLocationCommand>()         // @enterLocation id:backyard
```

### В .nani скриптах
```
@back videoPath:backyard_video      ← Naninovel показывает фон
@enterLocation id:backyard          ← LocationService.Enter()
```

### LocationService.State (Persistence)
```csharp
[System.Serializable]
public class State
{
    public string CurrentLocationId;
    public string[] LocationHistory;
    public string[] ConsumedItemIds;   // id предметов, уже подобранных игроком (одноразовые)
}
```

`ConsumedItemIds` пополняется в `LocationService.OnInteractableItemClicked()` сразу после выполнения `onClickScript`. При входе на локацию `HotspotLayerUI` пропускает активацию хотспотов, чьи id присутствуют в этом списке.

`ResetService()` очищает `ConsumedItemIds` вместе с остальным состоянием.

### ReportEvent из LocationService
```csharp
public void OnInteractableItemClicked(string itemId) {
    // выполняем onClickScript...
    // добавляем в ConsumedItemIds...

    var eventReporter = Engine.GetService<IQuestEventReporter>();
    eventReporter.ReportEvent(_itemConfigMap[itemId].objectiveTag);
}
```

---

## 10. Аудио (фрагмент — локации)

| Ключ | Момент | Приоритет |
|---|---|---|
| `click_movement_forward` | Клик по хотспоту перехода вперёд | Med |
| `click_movement_back` | Клик по кнопке "Назад" | Med |
| `click_object` | Клик по интерактивному предмету | Med |
