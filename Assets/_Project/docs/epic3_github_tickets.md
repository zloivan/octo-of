# Epic 3: Интеграция нарратива
## GitHub Projects — Tickets

> Зависимость эпика: Epic 1 (LocationService — для `@goto` в Point&Click секциях), Epic 2 (QuestService — для квестовых триггеров в нарративе)

---

## Метки (Labels)

| Label | Назначение |
|---|---|
| `feature` | Новая функциональность |
| `config` | ScriptableObject / data assets / NaniNovel конфигурация |
| `integration` | Связывание систем |
| `narrative` | Работа с `.nani` скриптами и диалогами |
| `qa-tooling` | QA debug инструменты |

---

## Assembly Definitions

Нарративные скрипты (`.nani`) — ресурсы NaniNovel, assembly не создаётся.
Кастомные команды размещаются в:

```
Assets/Scripts/Narrative/
  OnlyFarms.Narrative.asmdef
    References:
      - Naninovel.Runtime
      - OnlyFarms.Locations
      - OnlyFarms.Quests
```

---

## 🏁 Milestone: Demo — Пролог + День 1

---

### Ticket 3.1.1 — Конфигурация персонажей NaniNovel

**Labels:** `config`, `feature`
**Estimate:** 2h
**Milestone:** Demo

#### Описание

Зарегистрировать всех персонажей нарратива в NaniNovel Characters configuration и подключить их спрайты/плейсхолдеры.

**Персонажи:** Kyle, Emily, Roxy, Hanna, Grace, Hank, Jason.

**Emotion states из narrative_doc.txt:** `Idle`, `Flirty`, `Angry` — по наличию у каждого персонажа.

**Что делать:**
1. В NaniNovel Character Manager добавить каждого персонажа с уникальным ID (совпадает с именем в скрипте).
2. Для каждого emotion-state назначить спрайт (или дефолтный плейсхолдер если ассета ещё нет).
3. Проверить, что `@char Kyle.Idle` и `@hide Kyle.Idle` не выдают ошибок.

#### Acceptance Criteria

- [ ] Все 7 персонажей зарегистрированы, команды `@char` и `@hide` не выдают ошибок в консоли.
- [ ] Отсутствующие спрайты заменены явным плейсхолдером — редактор не падает.

---

### Ticket 3.1.2 — Конфигурация фонов (backgrounds) NaniNovel

**Labels:** `config`, `feature`
**Estimate:** 1.5h
**Milestone:** Demo

#### Описание

Зарегистрировать все фоны, используемые в Прологе и Дне 1, в NaniNovel Background Manager.

**Фоны из narrative_doc.txt:** `MC_house`, `Front_yard`, `Goat_barn`, `Field_tractor`, `Bar`, `Jason_house`.

**Что делать:**
1. Добавить каждый ID фона в Background Manager.
2. Назначить ассет (или плейсхолдер).
3. Проверить `@back MC_house` — переход без ошибок.

#### Acceptance Criteria

- [ ] Все `@back` команды в Prologue и Day1 скриптах переходят без ошибок.
- [ ] Отсутствующие ассеты — явные плейсхолдеры.

---

### Ticket 3.2.1 — Создание Prologue.nani

**Labels:** `narrative`, `feature`
**Estimate:** 3h
**Milestone:** Demo

#### Описание

Конвертировать секцию Пролога из `Assets/_Project/docs/narrative_doc.txt` в файл `Assets/Resources/Naninovel/Scripts/Prologue.nani`.

**Что делать:**
1. Перенести все `@back`, `@char`, `@hide` команды и диалоги 1-в-1.
2. Статики пролога (`; prologue_static1..4`) — заглушка `@wait 1` или переход на соответствующую сцену.
3. Секцию `; TODO затемнение экрана` реализовать через `@fadeIn`/`@fadeOut`.
4. Финальная реплика пролога (`Kyle: Я трахну всех девчонок...`) → `@goto Day1` .

#### Acceptance Criteria

- [ ] `Prologue.nani` воспроизводится от начала до конца без ошибок компилятора.
- [ ] Переход на `Day1.nani` происходит в финале пролога.

---

### Ticket 3.2.2 — Создание Day1.nani

**Labels:** `narrative`, `feature`
**Estimate:** 5h
**Milestone:** Demo

#### Описание

Конвертировать секцию «1 день» из `narrative_doc.txt` в файл `Assets/Resources/Naninovel/Scripts/Day1.nani`.

**Что делать:**
1. Перенести все команды и диалоги.
2. Оформить ветки выбора через `@choice` (6 точек ATT/SUS в течение дня):
   - ATT/SUS-эффекты заглушить — шкалы исключены из демо.
   - Ветки ведут к разным репликам, но функционально сходятся.
3. Секции `; Поинт энд клик` (`; 1day_static9`) — `@stopScript` + переход в LocationService для P&C, возврат по завершении.
4. Секции адалтов (`; Адалт`, `; 1AS_Roxy_1`, `; 1AS_Emily_1`) — `@goto` на заглушечные adult-скрипты (или пустой `@wait`).
5. Финальная сцена (`Emily: Денег нет.`) → `@stop` (конец демо).

#### Acceptance Criteria

- [ ] `Day1.nani` воспроизводится от начала до `; Конец демоверсии` без ошибок.
- [ ] Все 6 `@choice`-веток корректно разветвляются и сходятся.
- [ ] Point&Click секции корректно прерывают нарратив и возвращают управление.
- [ ] Adult-секции переходят на заглушки без break флоу.

---

### Ticket 3.3.1 — End-to-end проверка Пролог → День 1

**Labels:** `integration`, `qa-tooling`
**Estimate:** 1.5h
**Milestone:** Demo

#### Описание

Полный прогон демо от запуска до `; Конец демоверсии` с проверкой всех ветвлений.

**Что делать:**
1. Запустить `Prologue.nani` → проверить переход на `Day1.nani`.
2. Пройти обе ветки каждого из 6 выборов — убедиться что флоу не ломается.
3. Проверить переходы в P&C и возврат в нарратив.
4. Зафиксировать найденные баги в трекере.

#### Acceptance Criteria

- [ ] Обе ветки всех 6 выборов пройдены без крашей.
- [ ] P&C секции входят и выходят без артефактов (дублирование реплик, застрявшие персонажи).
- [ ] Консоль чистая (нет NaniNovel warnings/errors).

---

---

### Ticket 3.0.1 — [FEATURE] NarrativeTriggerEntry + DayNarrativeSource — реализация ILocationNarrativeSource / IItemNarrativeSource

**Labels:** `feature`, `config`, `integration`
**Estimate:** 3.5h
**Milestone:** Demo
**Depends on:** Ticket 2.4.3 (DaySessionOrchestrator), Ticket 2.4.1 (QuestService)

#### Контекст

`ILocationNarrativeSource` и `IItemNarrativeSource` используются в `GameFlowService` для запуска нарративного блока при входе в локацию и при клике на предмет. Оба интерфейса сейчас заглушены (`AlwaysNullNarrativeSource`, `AlwaysNullItemNarrativeSource`). Тикетов на их реализацию в Epic 2 нет — это gap в планировании, зафиксированный в ходе работы над Ticket 2.6.1.

GDD (mechanics_gdd.md) явно описывает оба сценария:
- *"При входе на некоторые локации может автоматически запускаться короткая сюжетная сцена"*
- *"При клике в зоне интерактивного предмета воспроизводится диалог с реакцией ГГ"*

Оба сценария требуют условий (день, состояние квеста) — не каждый вход/клик запускает нарратив.

#### Описание

Ввести `NarrativeTriggerEntry` в `DayConfigSO` по аналогии с `HotspotEntry` в `LocationConfigSO`. На старте дня `DaySessionOrchestrator` создаёт `DayNarrativeSource` из этих данных и передаёт его в `GameFlowService`.

---

**Файл:** `Assets/Scripts/Quests/Config/NarrativeTriggerEntry.cs`
```csharp
public enum NarrativeTriggerType { OnLocationEnter, OnItemPickup }

public enum NarrativeTriggerCondition { Always, RequiresQuestCompleted }

[Serializable]
public class NarrativeTriggerEntry
{
    public NarrativeTriggerType  Type;
    public string                TriggerId;       // locationId или hotspotId
    public string                ScriptName;
    public string                Label;
    public NarrativeTriggerCondition Condition;
    public QuestDefinitionSO     RequiredQuest;   // только для RequiresQuestCompleted
}
```

**Изменение:** `Assets/Scripts/Quests/Config/DayConfigSO.cs`
```csharp
// Добавить поле:
public NarrativeTriggerEntry[] NarrativeTriggers;
```

**Файл:** `Assets/Scripts/Quests/DayNarrativeSource.cs`
```csharp
public class DayNarrativeSource : ILocationNarrativeSource, IItemNarrativeSource
{
    private readonly NarrativeTriggerEntry[] _triggers;
    private readonly IQuestStatusSource      _questSource;

    public DayNarrativeSource(NarrativeTriggerEntry[] triggers, IQuestStatusSource questSource)
    {
        _triggers    = triggers;
        _questSource = questSource;
    }

    // ILocationNarrativeSource
    public string GetOnEnterScript(string locationId) =>
        GetScript(NarrativeTriggerType.OnLocationEnter, locationId);

    public string GetOnEnterLabel(string locationId) =>
        GetLabel(NarrativeTriggerType.OnLocationEnter, locationId);

    // IItemNarrativeSource
    public string GetOnUseScript(string hotspotId) =>
        GetScript(NarrativeTriggerType.OnItemPickup, hotspotId);

    public string GetOnUseLabel(string hotspotId) =>
        GetLabel(NarrativeTriggerType.OnItemPickup, hotspotId);

    private string GetScript(NarrativeTriggerType type, string id)
    {
        var entry = FindEntry(type, id);
        return entry != null ? entry.ScriptName : null;
    }

    private string GetLabel(NarrativeTriggerType type, string id)
    {
        var entry = FindEntry(type, id);
        return entry?.Label;
    }

    private NarrativeTriggerEntry FindEntry(NarrativeTriggerType type, string id)
    {
        foreach (var e in _triggers)
        {
            if (e.Type != type || e.TriggerId != id) continue;
            if (e.Condition == NarrativeTriggerCondition.Always) return e;
            if (e.Condition == NarrativeTriggerCondition.RequiresQuestCompleted
                && e.RequiredQuest != null
                && _questSource.IsQuestCompleted(e.RequiredQuest.name))
                return e;
        }
        return null;
    }
}
```

**Изменение:** `Assets/Scripts/Quests/DaySessionOrchestrator.cs`
```csharp
public void StartDay(string dayId)
{
    var config = _gameConfig.QuestConfig.GetDayConfig(dayId);
    _questService.ActivateDaySession(dayId);
    _gameFlowService.SetSessionSource(new DaySessionSource(config));

    // Новое: устанавливаем нарративные источники на этот день
    var narrativeSource = new DayNarrativeSource(config.NarrativeTriggers, _questService);
    _gameFlowService.SetLocationNarrativeSource(narrativeSource);
    _gameFlowService.SetItemNarrativeSource(narrativeSource);
}
```

> `DayNarrativeSource` реализует оба интерфейса — один объект, одна точка конфигурации на день.
> Условие `RequiresQuestCompleted` проверяется в рантайме через `IQuestStatusSource` — тот же механизм, что в `HotspotValidator`.
> При `ResetService` в `GameFlowService` оба источника обнуляются до `AlwaysNull*` — источники живут ровно один день.

---

#### Acceptance Criteria

- [ ] `NarrativeTriggerEntry` сериализуется в Inspector внутри `DayConfigSO.NarrativeTriggers[]`.
- [ ] При входе в локацию с настроенным триггером (`condition=Always`) — нарратив запускается автоматически.
- [ ] При `condition=RequiresQuestCompleted` и невыполненном квесте — нарратив не запускается.
- [ ] При клике на предмет с настроенным триггером (`OnItemPickup`) — нарратив запускается после pickup.
- [ ] `DaySessionOrchestrator.StartDay` устанавливает оба источника.
- [ ] Если `NarrativeTriggers` пустой — поведение идентично `AlwaysNull*` (ничего не запускается).

---

## 📊 Сводная таблица тикетов

| # | Задача | Оценка | Milestone | Labels |
|---|---|---|---|---|
| 3.0.1 | NarrativeTriggerEntry + DayNarrativeSource | 3.5h | Demo | `feature` `config` `integration` |
| 3.1.1 | Конфигурация персонажей | 2h | Demo | `config` `feature` |
| 3.1.2 | Конфигурация фонов | 1.5h | Demo | `config` `feature` |
| 3.2.1 | Prologue.nani | 3h | Demo | `narrative` `feature` |
| 3.2.2 | Day1.nani | 5h | Demo | `narrative` `feature` |
| 3.3.1 | End-to-end проверка | 1.5h | Demo | `integration` `qa-tooling` |

**Итого:** ~16.5h (с 20% буфером ~20h)

---

## ⚠️ Зависимости и блокеры

| Задача | Блокирует / Зависит |
|---|---|
| 3.0.1 (NarrativeTriggerEntry) | Зависит от 2.4.3 (DaySessionOrchestrator); блокирует локационные нарративные вставки в 3.2.2 |
| 3.1.1 + 3.1.2 (конфиги) | Блокируют 3.2.1 и 3.2.2 — скрипты не компилируются без конфигов |
| 3.2.1 (Prologue) | Блокирует 3.3.1 |
| 3.2.2 (Day1) | Блокирует 3.3.1; P&C секции зависят от Epic 1 (LocationService) |
| ATT/SUS в `@choice` | Зависят от шкал (исключены из демо) — заглушить нулевым вызовом |

## 🔓 Открытые вопросы

1. **Адалт Эмили** — в демо-плане «передний двор, кресло-качалка», в скрипте — дома после визита к Джейсону. Уточнить у нарратора до вёрстки Day1.nani.
2. **Point&Click API** — какой метод LocationService вызывать для входа в P&C режим? (уточнить с Epic 1).
3. **Статики** (`; prologue_static1..4`, `; 1day_static1..11`) — это CG-изображения или анимированные сцены? Влияет на реализацию.
