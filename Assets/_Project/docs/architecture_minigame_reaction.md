# Архитектура — Механика "Реакция"

> Покрывает: ReactionMiniGame, ReactionMiniGameConfig, ReactionZoneLogic, ReactionZoneView, SFX.
> Внешние зависимости: `IMiniGame` из [`architecture_minigames.md`](architecture_minigames.md) §17.3; `TimerBarView`, `ScoreCounterView` из [`architecture_minigames.md`](architecture_minigames.md)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 21. Механика "Реакция" (ReactionMiniGame)

### 21.1 Контекст

Мини-игра на менеджмент кликов. На экране одновременно присутствует до `maxActiveZones` зон. Каждая зона живёт `zoneLifetime` секунд, визуально проходя через стадии Green → Yellow → Red (мигающий). Игрок кликает по зонам — каждый клик инкрементирует `FillProgress` зоны на `progressPerClick`. При исчезновении зоны её прогресс конвертируется в очки. Три вариации — один префаб `ReactionMiniGame`, отличия только в параметрах конфига и теме спрайтов зон.

---

### 21.2 ReactionMiniGameConfig (ScriptableObject)

```csharp
[CreateAssetMenu]
public class ReactionMiniGameConfig : ScriptableObject
{
    public float zoneLifetimeMin;           // минимальное время жизни зоны
    public float zoneLifetimeMax;           // максимальное время жизни зоны
    public float spawnInterval;             // интервал появления новых зон
    public int   maxActiveZones;            // макс. зон одновременно (≤ 3)
    public float progressPerClick;          // инкремент FillProgress за один клик
    public int   baseScorePerZone;          // очки за зону при 100% fill
    public float bonusMultiplier;           // множитель при fillProgress >= 1 (double = 2f)
    public float difficultyRampInterval;    // каждые N секунд ужесточаем параметры
    public float rampSpawnFactor;           // множитель spawnInterval при ramp (< 1)
    public float rampLifetimeFactor;        // множитель zoneLifetime при ramp (< 1)
    public AssetReference zonePrefabRef;    // тематический префаб ReactionZoneView
}
```

**Добавить в `MiniGameConfig` три записи:**

| id | isCyclic | sessionDuration | npcLeaderboard |
|---|---|---|---|
| `tractor_reaction` | false | > 0 | ❌ |
| `car_wash_reaction` | false | > 0 | ❌ |
| `reaction_training` | false | > 0 | ✅ |

---

### 21.3 ReactionZoneLogic (plain C#)

Изолированная логика одной зоны. Без Unity-зависимостей — тестируемый plain C#.

**Поля:**
```csharp
private readonly float _lifetime;
private readonly float _progressPerClick;
private float _elapsed;

public float FillProgress { get; private set; }   // [0..1]
public float NormalizedTimeRemaining               // вычисляемое: 1 - (_elapsed / _lifetime)
    => Mathf.Clamp01(1f - _elapsed / _lifetime);

public ZoneState State => NormalizedTimeRemaining switch  // чистое свойство, не поле
{
    > 0.6f => ZoneState.Green,
    > 0.25f => ZoneState.Yellow,
    _ => ZoneState.Red
};
```

**API:**
```csharp
public bool Tick(float dt)               // возвращает true когда _elapsed >= _lifetime
public void RegisterClick()              // FillProgress += progressPerClick; Clamp01
public ZoneResult GetResult(int baseScore, float bonusMultiplier)
    // FillProgress >= 1 → FloorToInt(baseScore * bonusMultiplier)
    // FillProgress > 0  → FloorToInt(baseScore * FillProgress)
    // FillProgress == 0 → 0
```

---

### 21.4 ReactionZoneView (MonoBehaviour)

Пассивный View одной зоны.

**Поля (сериализованы в префабе):**
```csharp
[SerializeField] Image _fillImage;         // круговой прогресс (fillAmount)
[SerializeField] Image _rimImage;          // ободок — цвет по ZoneState
[SerializeField] Color _greenColor, _yellowColor, _redColor;

public Action OnClicked;                   // подписывается ReactionMiniGame
```

**Публичный API:**
```csharp
public void SetFill(float value)           // _fillImage.fillAmount = value
public void SetState(ZoneState state)      // цвет ободка; запускает мигание в Red
public void SetPosition(Vector2 anchoredPosition)
```

Мигание в Red-фазе — coroutine внутри View, запускается/останавливается из `SetState`.

---

### 21.5 ReactionMiniGame : MonoBehaviour, IMiniGame

Владеет игровым циклом. Координирует спаун, тикинг и scoring зон.

**Поля:**
```csharp
ReactionMiniGameConfig _config;
List<(ReactionZoneLogic logic, ReactionZoneView view)> _activeZones = new();

TimerBarView  _timerBar;          // из Эпика 4
ScoreCounterView _scoreCounter;   // из Эпика 4
RectTransform _playArea;          // сериализован в префабе — зона спауна зон

int   _totalScore;
float _roundTimer;
float _nextSpawnTime;
float _difficultyTimer;
float _currentSpawnInterval;
float _currentLifetimeMax;
```

**Initialize(ScriptableObject mechanicConfig):**
- Кастует до `ReactionMiniGameConfig`
- Инициализирует difficulty параметры из конфига

**RunAsync(RectTransform container, CancellationToken ct):**
```
Каждый кадр (UniTask.Yield):
  1. _roundTimer += dt; _timerBar.SetFill(_roundTimer / sessionDuration)
  2. TrySpawnZone() если время пришло и _activeZones.Count < maxActiveZones
  3. TickAllZones(dt) — обходим список, Tick() каждой
     → если expired: ScoreZone(logic) → _scoreCounter.SetScore(_totalScore); despawn
  4. TryApplyDifficultyRamp()
  5. Проверка завершения: _roundTimer >= sessionDuration || ct.IsCancellationRequested
```

**TrySpawnZone():**
```csharp
var logic = new ReactionZoneLogic(Random.Range(lifetimeMin, _currentLifetimeMax), progressPerClick);
var view  = Instantiate(zonePrefabRef, _playArea);
view.SetPosition(RandomPositionInPlayArea());
view.OnClicked = () => { logic.RegisterClick(); view.SetFill(logic.FillProgress); };
_activeZones.Add((logic, view));
```

**TryApplyDifficultyRamp():**
```csharp
_difficultyTimer += dt;
if (_difficultyTimer >= _config.difficultyRampInterval)
{
    _difficultyTimer = 0;
    _currentSpawnInterval  *= _config.rampSpawnFactor;    // уменьшается
    _currentLifetimeMax    *= _config.rampLifetimeFactor; // уменьшается
}
```

Возврат `RunAsync`: `_totalScore` — `MiniGameService` сравнивает с `victoryScoreThreshold` из `MiniGameDefinition`.

---

### 21.6 Scoring

При исчезновении зоны (Tick вернул true):
```
FillProgress >= 1f → score = Floor(baseScore * bonusMultiplier)   // double score
FillProgress > 0f  → score = Floor(baseScore * FillProgress)      // пропорционально
FillProgress == 0f → score = 0                                     // ничего
```

---

### 21.7 Аудио

| Ключ | Момент |
|---|---|
| `reaction_click_hit` | `RegisterClick()` успешно вызван |
| `reaction_zone_complete` | Зона исчезла при `FillProgress >= 1f` |
| `reaction_zone_miss` | Зона исчезла при `FillProgress == 0f` |
| `reaction_complete` | Раунд завершён |

Воспроизводятся через `IAudioManager` в `ReactionMiniGame`.

---

### 21.8 Что намеренно НЕ делаем

- **`IReactionEnvironment`** — среды не влияют на game-state; визуальный контекст задаётся только темой спрайтов (`zonePrefabRef` в конфиге)
- **`ZoneState` как поле стейт-машины** — чистое вычисляемое свойство из `NormalizedTimeRemaining`; нет лишних переходов и событий
- **Отдельный `IMiniGame` на каждую вариацию** — три конфига, один префаб
- **SeductionScaleService внутри мини-игры** — связь только через результат flow §17.4
