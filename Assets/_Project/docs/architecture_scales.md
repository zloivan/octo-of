# Архитектура — Шкалы соблазнения

> Покрывает: SeductionScaleLogic, SeductionScaleService, SeductionScaleUI, ChoiceEx, конфиг, команды, Contacts UI, SFX.
> Внешние зависимости: нет игровых зависимостей наружу (шкалы — листовой модуль)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 16. Система шкал соблазнения

### 16.1 Контекст

Два атрибута per персонаж: **Attraction (ATT)** и **Suspicion (SUS)**. Оба в диапазоне [0, maxValue]. Источники изменений: диалоговые выборы, результаты мини-игр, клики по предметам.

Каждое изменение можно отследить (диалог выбора показывает, как изменятся шкалы), и изменение влияет на нарративные ветки (условия `@if`).

### 16.2 SeductionScaleConfig

```
SeductionScaleConfig : ScriptableObject
  └── CharacterScaleDefinition[]
        ├── characterId: string
        ├── initialAttraction: int      (default 2)
        ├── initialSuspicion: int       (default 2)
        └── maxValue: int               (default 10)
```

> **Внимание:** Ханна — особый случай. По схеме развития шкал в ГДД ATT у Ханны может достигать 15–16 (6 выборов + 2 мини-игры). `maxValue` для персонажа `hannah` должен быть установлен **не менее 16**. При конфигурировании проверять, что `maxValue >= 16` для этого персонажа.

### 16.3 Доменная логика — SeductionScaleLogic

Plain C# класс. Эффективно — просто словарь `(characterId) → (att, sus)`.

```csharp
public class SeductionScaleLogic
{
    public void Initialize(SeductionScaleConfig config);
    public void ApplyDelta(string characterId, int attDelta, int susDelta);
    public (int att, int sus) GetCharacterScales(string characterId);
    public CharacterScaleSnapshot GetScaleSnapshot(string characterId);
// Возвращает CharacterScaleSnapshot из State.Characters для данного characterId.
// Используется ContactsTabView для отображения ATT/SUS без подписки на события.
    public bool CheckCondition(string characterId, ScaleCondition condition);
    public void Reset();
    
    public event Action<string> OnScaleChanged; // characterId
}

public struct ScaleCondition
{
    public string characterId;
    public int? minAttraction;
    public int? maxAttraction;
    public int? minSuspicion;
    public int? maxSuspicion;
    
    public bool Evaluate(int att, int sus) { /* ... */ }
}


```

### 16.4 ScaleDelta — вспомогательный тип

```csharp
public struct ScaleDelta
{
    public int attractionDelta;
    public int suspicionDelta;
}
```

Используется в:
- Команде `@applyScaleDelta`
- Команде `@miniGameScaleResult`
- `@choiceEx` параметрах
- `InteractableItemConfig` для предметов

### 16.5 Сервис — SeductionScaleService

```
SeductionScaleLogic (plain C#)
        ↑
SeductionScaleService : IEngineService<SeductionScaleService.State>,
                        IStatefulService<SeductionScaleService.State>
        ↑
InitCharacterScalesCommand    ← @initCharacterScales char:grace
ApplyScaleDeltaCommand        ← @applyScaleDelta char:grace att:2 sus:-1
MiniGameScaleResultCommand    ← @miniGameScaleResult char:grace success:true
ChoiceExCommand               ← @choiceEx ... (спайк §16.7)
```

**Persistence State:**
```csharp
[System.Serializable]
public class State
{
    public CharacterScaleSnapshot[] Characters;
}

[System.Serializable]
public class CharacterScaleSnapshot
{
    public string CharacterId;
    public int Attraction;
    public int Suspicion;
}
```

`ResetService()` вызывает `SeductionScaleLogic.Reset()`.

### 16.6 UI — SeductionScaleUI и ScaleBarView

`SeductionScaleUI : CustomUI` — Naninovel Custom UI. Располагается в верхнем углу экрана.

**Логика видимости (самостоятельная):**
- Подписана на `SeductionScaleLogic.OnScaleChanged`.
- При получении события: показывает шкалы нужного персонажа и запускает coroutine-таймер на N секунд, после которого скрывается.
- Если пришло новое событие пока таймер ещё идёт — таймер перезапускается.
- Во время активного `@choiceEx` (кнопки выбора на экране): `SeductionChoiceHandlerUI` вызывает `Show(characterId)` / `Hide()` напрямую, таймер при этом не используется.
- Команды (`@applyScaleDelta`, `@miniGameScaleResult`) **не управляют UI** — они только вызывают `ApplyDelta`, остальное делает сам UI через `OnScaleChanged`.

`ScaleBarView : MonoBehaviour` — одна полоска:
- Поля: иконка (`Image`), цвет заливки, Slider или Image fillAmount.
- Метод `AnimateTo(int newValue, int maxValue)` — плавная анимация fill через coroutine.
- Два экземпляра в `SeductionScaleUI` — один для ATT, один для SUS.

### 16.7 Диалоговые выборы — ChoiceEx (спайк)

**Решение:** `@choiceEx` — кастомная команда, которая взаимодействует с кастомным choice handler `SeductionChoiceHandlerUI`, зарегистрированным в Naninovel.

**Проблема:** Naninovel's choice system требует регистрации кастомного handler. Механизм регистрации и batching нескольких `@choiceEx` в одной сцене — **🔴 спайк 3.1.1**.

**После спайка:**

`ChoiceExCommand : Command` — асинхронная команда:
1. Парсит параметры: `text`, `char`, `attMin/attMax/susMin/susMax` → `ScaleCondition`, `att/sus` → `ScaleDelta`, `goto`.
2. Формирует `ChoiceExEntry` (текст + условие + дельта + goto).
3. Передаёт entry в `SeductionChoiceHandlerUI.AddChoice(entry)`.
4. Если команда последняя в блоке (`wait:true` или implicit) — awaits выбора игрока.
5. После выбора: `SeductionScaleService.ApplyDelta(...)` → navigate to goto label.

`SeductionChoiceHandlerUI : CustomUI` — кастомный choice handler:
- Хранит список `ChoiceExEntry[]` текущего блока выборов.
- При показе: для каждого entry проверяет `SeductionScaleService.CheckCondition()`.
- Инстанциирует `ChoiceExButtonView` для каждого entry.
- Заблокированные кнопки: `interactable = false`, затемнение, tooltip с причиной.
- Вызывает `SeductionScaleUI.Show(characterId)` перед показом кнопок, `SeductionScaleUI.Hide()` после выбора.

`ChoiceExButtonView : MonoBehaviour` — одна кнопка выбора:
- Поля: Button, Label (`ManagedTextProvider`), `ScaleEffectIndicatorView`, overlay-затемнение, tooltip-объект.
- Метод `Setup(ChoiceExEntry entry, bool isBlocked, string blockReason)`.

`ScaleEffectIndicatorView : MonoBehaviour` — иконка-индикатор справа от текста:
- Метод `Setup(ScaleDelta delta)` — формирует строку `ATT↑↑ SUS↑` по знаку и величине дельты.

### 16.8 Команды

```csharp
.AddEngineService<SeductionScaleService>(config.scales)
.RegisterCommand<InitCharacterScalesCommand>()
.RegisterCommand<ApplyScaleDeltaCommand>()
.RegisterCommand<MiniGameScaleResultCommand>()
.RegisterCommand<ChoiceExCommand>()
```

| Команда | Параметры | Действие |
|---|---|---|
| `@initCharacterScales` | `char:grace` | Инициализирует шкалы из конфига. Идемпотентно |
| `@applyScaleDelta` | `char:grace att:2 sus:-1` | Применяет дельту. UI реагирует самостоятельно через `OnScaleChanged`, играет `scale_risky` или `scale_conscious` в зависимости от знака |
| `@miniGameScaleResult` | `char:grace success:true` | `success:true` → `+1 att`, `success:false` → `+1 sus`. UI реагирует через `OnScaleChanged`, играет `scale_neutral` |
| `@choiceEx` | `"текст" char:grace attMin:6 att:2 sus:1 goto:.label` | Добавляет выбор в handler. Последний в блоке ждёт ввода игрока |

---

## 22.3 ContactsTabView

```csharp
public class ContactsTabView : MonoBehaviour
{
    // Левая панель — список персонажей
    [SerializeField] Transform _listContainer;
    // Правая панель — детали
    [SerializeField] Image _portrait;
    [SerializeField] TextMeshProUGUI _nameLabel, _bioLabel;
    [SerializeField] ScaleSnapshotView _scaleView;

    public void Bind(CharacterContactConfig[] contacts, SeductionScaleService scaleService);
    // При клике на ContactListEntryView → обновляет правую панель
    // ATT/SUS через scaleService.GetScaleSnapshot(characterId)
}
```

## 22.4 ScaleSnapshotView

```csharp
public class ScaleSnapshotView : MonoBehaviour
{
    public void SetValues(int att, int sus, int maxValue);
    // Два Image fillAmount (ATT, SUS) + TextMeshProUGUI цифры
}
```

Не подписывается на события — snapshot только при открытии вкладки.

---

## 10. Аудио (фрагмент — шкалы)

| Ключ | Момент | Приоритет |
|---|---|---|
| `scale_risky` | Выбор рискованного варианта в @choiceEx | Med |
| `scale_conscious` | Выбор осторожного варианта в @choiceEx | Med |
| `scale_neutral` | Изменение шкалы после мини-игры (@miniGameScaleResult) | Med |
