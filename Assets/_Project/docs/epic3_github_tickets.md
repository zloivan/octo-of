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

## 📊 Сводная таблица тикетов

| # | Задача | Оценка | Milestone | Labels |
|---|---|---|---|---|
| 3.1.1 | Конфигурация персонажей | 2h | Demo | `config` `feature` |
| 3.1.2 | Конфигурация фонов | 1.5h | Demo | `config` `feature` |
| 3.2.1 | Prologue.nani | 3h | Demo | `narrative` `feature` |
| 3.2.2 | Day1.nani | 5h | Demo | `narrative` `feature` |
| 3.3.1 | End-to-end проверка | 1.5h | Demo | `integration` `qa-tooling` |

**Итого:** ~13h (с 20% буфером ~15.5h)

---

## ⚠️ Зависимости и блокеры

| Задача | Блокирует / Зависит |
|---|---|
| 3.1.1 + 3.1.2 (конфиги) | Блокируют 3.2.1 и 3.2.2 — скрипты не компилируются без конфигов |
| 3.2.1 (Prologue) | Блокирует 3.3.1 |
| 3.2.2 (Day1) | Блокирует 3.3.1; P&C секции зависят от Epic 1 (LocationService) |
| ATT/SUS в `@choice` | Зависят от шкал (исключены из демо) — заглушить нулевым вызовом |

## 🔓 Открытые вопросы

1. **Адалт Эмили** — в демо-плане «передний двор, кресло-качалка», в скрипте — дома после визита к Джейсону. Уточнить у нарратора до вёрстки Day1.nani.
2. **Point&Click API** — какой метод LocationService вызывать для входа в P&C режим? (уточнить с Epic 1).
3. **Статики** (`; prologue_static1..4`, `; 1day_static1..11`) — это CG-изображения или анимированные сцены? Влияет на реализацию.
