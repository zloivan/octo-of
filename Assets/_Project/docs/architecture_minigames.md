# Архитектура — Инфраструктура мини-игр

> Покрывает: MiniGameService, IMiniGame, MiniGameConfig, AdultStaticConfig, Gallery UI, команды, SFX shell.
> Внешние зависимости: `QuestService.ReportEvent` (вызывается из `StartMiniGameCommand`), `SeductionScaleService.ApplyMiniGameResult` (вызывается из `StartMiniGameCommand`)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 17. Инфраструктура мини-игр

### 17.1 Контекст

Мини-игры — короткие интерактивные вставки. Запускаются по сюжетному триггеру (`@startMiniGame`) или кликом на хотспот в свободном перемещении. Независимо от механики, flow всегда одинаков: туториал (первый раз) → игра → экран результата.

Конкретные механики (Стрельба, Борьба, Контроль, Реакция) реализуют `IMiniGame` и ничего не знают о shell UI. Shell (`MiniGameWindowUI`) ничего не знает о конкретных механиках.

### 17.2 Конфиг

```
MiniGameConfig : ScriptableObject
  └── MiniGameDefinition[]
        ├── id: string                          ← "shooting", "wrestling", "control", "reaction"
        ├── mechanicPrefabRef: AssetReference   ← prefab с IMiniGame компонентом
        ├── mechanicConfigRef: AssetReference   ← тюнинг-конфиг конкретной механики (ShootingMiniGameConfig и т.д.)
        ├── allowSkipDuringPlay: bool           ← показывать Skip во время игры
        ├── victoryScoreThreshold: int          ← минимум очков для победы
        ├── unlockThreshold: int                ← score для разблокировки адалт-статика
        ├── tutorialLocalizationKey: string
        ├── tutorialImageRef: AssetReferenceSprite
        └── npcLeaderboard: NpcLeaderboardEntry[]
              ├── nameLocalizationKey: string
              └── score: int                    ← фиксированный, не меняется

AdultStaticConfig : ScriptableObject   ← реестр адалт-статиков для галереи
  └── AdultStaticEntry[]
        ├── id: string                 ← совпадает с UnlockedAdultIds в MiniGameService.State
        ├── imageRef: AssetReferenceSprite
        └── localizationKey: string    ← подпись в галерее (ManagedTextProvider)
```

NPC-записи — статичные пороги. Достижение 1-го места (score выше всех NPC) разблокирует адалт-статик для данной мини-игры.

### 17.3 IMiniGame — контракт механики

```csharp
public interface IMiniGame
{
    void Initialize(ScriptableObject mechanicConfig);
    UniTask<int> RunAsync(RectTransform container, CancellationToken ct);
}
```

`Initialize` гарантированно вызывается до `RunAsync`. Если механика не требует конфига — реализация оставляет метод пустым.

Механика инстанциирует свой prefab в `container`, управляет им самостоятельно, возвращает итоговый score. При отмене через `ct` — корректно завершается и возвращает текущий score.

`TimerBarView` и `ScoreCounterView` живут в prefab'е каждой механики — не в shell. Переиспользование происходит через одинаковые компоненты, а не через централизованный контейнер.

### 17.4 Flow — MiniGameService

```
@startMiniGame id:shooting char:grace
        ↓
MiniGameService.RunAsync(id)
    1. Проверить ShownTutorialIds → показать TutorialPanelView если первый раз
    2. Показать GameplayPanelView (кнопка Start)
    3. По Start → загрузить mechanicPrefabRef → инстанциировать в gameplayContainer
    4. Вызвать IMiniGame.Initialize(mechanicConfigRef)
    5. IMiniGame.RunAsync(container, ct) → await score
       (если Skip нажат → ct.Cancel() → механика возвращает текущий score, isVictory = false)
    6. Выгрузить prefab механики
    7. Сформировать MiniGameSessionResult (score, isVictory, isNewRecord)
    8. Обновить State (PlayerBestScores, ShownTutorialIds, UnlockedAdultIds)
    9. Показать ResultPanelView
       - Поражение: Retry → вернуться к шагу 3 | Skip → вернуть result (isVictory=false)
       - Победа: Next → вернуть result
   10. Если запуск был из хотспота тренировки (свободное перемещение):
       показать LeaderboardPanelView (таблица рекордов + адалт-статик если разблокирован)
        ↓
StartMiniGameCommand получает MiniGameSessionResult
    → QuestService.ReportEvent("play_minigame_" + id)   ← ВСЕГДА
    → SeductionScaleService.ApplyMiniGameResult(char, isVictory)
```

### 17.5 Persistence State

```csharp
[System.Serializable]
public class State
{
    public PlayerBestScore[] BestScores;
    public string[] ShownTutorialIds;
    public string[] UnlockedAdultIds;
}

[System.Serializable]
public class PlayerBestScore
{
    public string MiniGameId;
    public int Score;
}
```

`ResetService()` не сбрасывает State — рекорды и разблокировки персистентны между сессиями и сбросами состояния Naninovel.

### 17.6 Таблица рекордов — MiniGameLeaderboardLogic и LeaderboardPanelView

**MiniGameLeaderboardLogic** — Plain C# класс. Получает `NpcLeaderboardEntry[]` и `playerBestScore` для конкретной игры.

**Методы:**
- `GetRankedEntries(string miniGameId)` → `LeaderboardDisplayEntry[]` — мержит NPC-записи с записью игрока, сортирует по score, возвращает топ-6 (топ-5 NPC + игрок, или игрок встроен в список если вошёл в топ-5)
- `IsAdultUnlocked(string miniGameId)` → `bool` — score игрока >= `unlockThreshold`

**LeaderboardPanelView : CustomUI** — отдельная панель (не часть ResultPanelView).

Структура:
- Левая часть: вкладки четырёх мини-игр; при переключении вкладки контент таблицы обновляется
- Таблица: топ-5 NPC + строка игрока; строка игрока динамически встраивается по месту в рейтинге
- Правая часть: адалт-статик в закрытом/открытом состоянии + условие разблокировки (`unlockThreshold`)

**Когда показывается:**
- После завершения тренировочной мини-игры из хотспота свободного перемещения (шаг 10 в §17.4)
- Не показывается при сюжетном запуске (`@startMiniGame`)

`MiniGameService` знает контекст запуска (сюжетный vs. свободное перемещение) через параметр команды или флаг в `RunAsync`. Показ `LeaderboardPanelView` происходит только при `isTraining = true`.

### 17.7 Команды

```csharp
.AddEngineService<MiniGameService>(config.miniGames)
.RegisterCommand<StartMiniGameCommand>()
```

| Команда | Параметры | Действие |
|---|---|---|
| `@startMiniGame` | `id:shooting char:grace` | Запускает мини-игру, await результата, вызывает QuestService (всегда) + SeductionScaleService (по результату) |

---

## 23.6 GalleryWindowUI

`GalleryWindowUI : CustomUI`.
- Грид `GalleryEntryView` — locked (серый оверлей) / unlocked (спрайт)
- `UnlockedAdultIds` читаются из `MiniGameService.State`
- Клик по unlocked → `GalleryFullscreenView.Show(imageRef)`

`UnlockAllAdultsCommand : Command` (`@unlockAllAdults`) — для демо.
Добавляет все id из `AdultStaticConfig` в `MiniGameService.State.UnlockedAdultIds`.

## 23.7 Что намеренно НЕ делаем (фрагмент — мини-игры)

- **Отдельный сервис для галереи** — UnlockedAdultIds уже в MiniGameService.State

---

## 10. Аудио (фрагмент — мини-игры)

| Ключ | Момент | Приоритет |
|---|---|---|
| `minigame_button` | Нажатие любой кнопки shell UI (Start, Skip, Next, Retry) | Low |
| `minigame_victory` | Победа в мини-игре | High |
| `minigame_defeat` | Поражение в мини-игре | High |
