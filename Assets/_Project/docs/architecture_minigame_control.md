# Архитектура — Механика "Контроль"

> Покрывает: ControlMiniGame, ControlMiniGameConfig, IControlEnvironment, ControlBarLogic, три реализации (Goats, RoxyMilking, MechanicalBull), SFX.
> Внешние зависимости: `IMiniGame` из [`architecture_minigames.md`](architecture_minigames.md) §17.3; `TimerBarView`, `ScoreCounterView` из [`architecture_minigames.md`](architecture_minigames.md)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 20. Механика "Контроль"

### 20.1 Контекст

Мини-игра на удержание баланса. Реализует `IMiniGame`. Три вариации (`goats_balance`, `roxy_milking`, `mechanical_bull`) используют **один и тот же префаб** `ControlMiniGame`, но получают разные `ControlMiniGameConfig`.

**Ключевая идея разделения:** core gameplay loop (удержание ЛКМ → заполнение шкалы → state transitions → провал/победа) одинаков во всех вариациях. Всё что меняется — визуальный и нарративный контекст — живёт в `IControlEnvironment` prefab'е, подключаемом через конфиг.

---

### 20.2 ControlMiniGameConfig

```
ControlMiniGameConfig : ScriptableObject
  ├── fillRate: float                    ← скорость роста FillValue при удержании ЛКМ
  ├── decayRate: float                   ← скорость падения FillValue при отпускании
  ├── tenseTriggerIntervalMin: float     ← мин. пауза между Tense-триггерами
  ├── tenseTriggerIntervalMax: float     ← макс. пауза
  ├── tenseDuration: float               ← сколько держится Tense до перехода в Critical
  ├── criticalCountdown: float           ← ~1 сек до провала при удержании в Critical
  ├── difficultyRampPerCycle: float      ← множитель сокращения интервала (для isCyclic)
  ├── sessionDifficultyRamp: float       ← скорость сокращения tenseTriggerInterval по времени (0 = нет рампа)
  ├── sessionDuration: float             ← время раунда (0 = без лимита)
  ├── isCyclic: bool                     ← true для goats_balance
  └── environmentPrefabRef: AssetReference  ← prefab с IControlEnvironment компонентом
```

`sessionDifficultyRamp` используется для **не**-цикличных вариаций (например `mechanical_bull`), где сложность должна нарастать с течением времени сессии. Значение задаёт долю от `tenseTriggerIntervalMin`, на которую интервал сокращается за секунду. При значении `0` рамп отсутствует.

```csharp
public enum BarState { Normal, Tense, Critical }
```

---

### 20.3 IControlEnvironment

Чистый event sink — знает только о визуальных реакциях. Не владеет победой/поражением — это зона ответственности `ControlMiniGame`.

```csharp
public interface IControlEnvironment
{
    void Initialize(ControlMiniGameConfig config);
    void OnBarStateChanged(BarState state);   // обновить цвет/анимацию среды
    void OnCycleComplete();                   // следующая коза / смена стейта видео
    void OnFail();                            // анимация провала в конкретном контексте
}
```

`ControlMiniGame` не знает что за интерфейсом: анимированные козы, адалт-видео с Рокси или механический бык. Замена реализации — только внутри prefab'а.

---

### 20.4 ControlBarLogic (plain C#)

Вся доменная логика шкалы. Нет зависимостей от Unity.

```csharp
public class ControlBarLogic
{
    private float _fillRate;
    private float _decayRate;
    private float _tenseDuration;
    private float _criticalCountdown;

    private bool _isHolding;
    private float _tenseTimer;
    private float _criticalTimer;
    private float _nextTenseTrigger;    // случайный интервал в [intervalMin, intervalMax]
    private float _elapsedNormal;      // время в Normal с последнего триггера

    // Для sessionDifficultyRamp (не-цикличный режим)
    private float _sessionDifficultyRamp;
    private float _sessionElapsed;
    private float _baseIntervalMin;
    private float _baseIntervalMax;

    public float FillValue { get; private set; }   // [0, 1]
    public BarState State { get; private set; }

    public event Action<BarState> OnStateChanged;
    public event Action OnFillComplete;   // FillValue достиг 1.0
    public event Action OnFail;           // criticalCountdown истёк

    public ControlBarLogic(ControlMiniGameConfig config)
    {
        // инициализация из конфига
        _sessionDifficultyRamp = config.sessionDifficultyRamp;
        _baseIntervalMin = config.tenseTriggerIntervalMin;
        _baseIntervalMax = config.tenseTriggerIntervalMax;
    }

    public void SetHolding(bool isHolding) => _isHolding = isHolding;

    public void Tick(float deltaTime)
    {
        _sessionElapsed += deltaTime;

        // Применяем sessionDifficultyRamp: уменьшаем текущие интервалы по времени сессии
        if (_sessionDifficultyRamp > 0f)
        {
            float rampFactor = Mathf.Max(0f, 1f - _sessionDifficultyRamp * _sessionElapsed);
            _tenseTriggerIntervalMin = Mathf.Max(0.5f, _baseIntervalMin * rampFactor);
            _tenseTriggerIntervalMax = Mathf.Max(0.5f, _baseIntervalMax * rampFactor);
        }

        // state machine (см. ниже)
    }

    // Вызывается при старте следующего цикла (следующая коза)
    public void ResetCycle(float newIntervalMin, float newIntervalMax, float newTenseDuration)
    {
        FillValue = 0f;
        State = BarState.Normal;
        _elapsedNormal = 0f;
        _tenseTriggerIntervalMin = newIntervalMin;
        _tenseTriggerIntervalMax = newIntervalMax;
        _tenseDuration = newTenseDuration;
        PickNextTenseTrigger();
    }
}
```

**State machine в Tick():**

```
Normal:
  → если isHolding: FillValue += fillRate * dt; если >= 1 → OnFillComplete
  → если !isHolding: FillValue -= decayRate * dt (clamp 0)
  → _elapsedNormal += dt; если >= _nextTenseTrigger → State = Tense, OnStateChanged

Tense:
  → FillValue -= decayRate * dt (шкала падает пока ждём отпускания)
  → если !isHolding → State = Normal, PickNextTenseTrigger(), OnStateChanged
  → _tenseTimer += dt; если >= tenseDuration → State = Critical, OnStateChanged

Critical:
  → если !isHolding → State = Normal, PickNextTenseTrigger(), OnStateChanged
  → _criticalTimer += dt; если >= criticalCountdown → OnFail
```

---

### 20.5 ControlBarView (MonoBehaviour)

Пассивный View вертикальной шкалы.

**Поля (сериализованы в префабе):**
- `Image _fillImage` — `fillAmount` = `FillValue`
- `Color _normalColor`, `_tenseColor`, `_criticalColor`

**Публичный API:**
```csharp
public void SetFill(float value)           // Image.fillAmount = value
public void SetState(BarState state)       // _fillImage.color по состоянию
```

---

### 20.6 ControlMiniGame : MonoBehaviour, IMiniGame

Владеет игровым циклом. Координирует все компоненты.

**Поля:**
```csharp
ControlMiniGameConfig _config;
ControlBarLogic _barLogic;
IControlEnvironment _environment;

ControlBarView _barView;           // сериализован в префабе
TimerBarView _timerBar;            // переиспользуется из Эпика 4
ScoreCounterView _scoreCounter;    // переиспользуется из Эпика 4

int _score;
int _cycleIndex;
bool _isFailed;
bool _isVictory;
```

**Initialize(ScriptableObject mechanicConfig):**
- Кастует до `ControlMiniGameConfig`
- Создаёт `new ControlBarLogic(config)`
- Подписывается на `OnStateChanged` / `OnFillComplete` / `OnFail`

**RunAsync(RectTransform container, CancellationToken ct):**
```
1. Instantiate environmentPrefabRef → environment.Initialize(config)
2. Запустить таймер (если sessionDuration > 0)
3. Каждый кадр (UniTask.Yield):
     _barLogic.SetHolding(Input.GetMouseButton(0))
     _barLogic.Tick(deltaTime)
     _barView.SetFill(_barLogic.FillValue)
4. Проверять условия завершения
5. Возвращает _score
```

**Обработчики событий ControlBarLogic:**
```csharp
// OnStateChanged:
_barView.SetState(state);
_environment.OnBarStateChanged(state);
PlayAudio(state);

// OnFillComplete:
if (_config.isCyclic)
{
    _score++;
    _scoreCounter.SetScore(_score);
    _environment.OnCycleComplete();
    float ramp = Mathf.Pow(_config.difficultyRampPerCycle, _cycleIndex);
    _barLogic.ResetCycle(intervalMin * ramp, intervalMax * ramp, tenseDuration);
    _cycleIndex++;
}
else
{
    _isVictory = true;  // → выход из RunAsync
}

// OnFail:
_isFailed = true;
_environment.OnFail();
// → выход из RunAsync
```

**Условия завершения:**

| Условие | Источник |
|---|---|
| `_isVictory` | `OnFillComplete` (не цикличный режим) |
| `_isFailed` | `OnFail` из `ControlBarLogic` |
| `sessionDuration > 0 && timer <= 0` | Таймер раунда |
| `ct.IsCancellationRequested` | Skip нажат |

Возврат: `_score` — для цикличного это количество успешных циклов; для не цикличного: 1 (победа) или 0 (поражение). `MiniGameService` сравнивает с `victoryScoreThreshold` из `MiniGameDefinition`.

---

### 20.7 Три реализации IControlEnvironment

#### GoatsEnvironment

```csharp
public class GoatsEnvironment : MonoBehaviour, IControlEnvironment
{
    [SerializeField] Animator _goatAnimator;
    [SerializeField] int _totalGoats = 5;
    private int _currentGoatIndex;

    public void Initialize(ControlMiniGameConfig config) { }

    public void OnBarStateChanged(BarState state)
    {
        // визуальная реакция козы: нервозность при Tense/Critical
        _goatAnimator.SetInteger("State", (int)state);
    }

    public void OnCycleComplete()
    {
        _currentGoatIndex++;
        if (_currentGoatIndex < _totalGoats)
            _goatAnimator.SetTrigger("NextGoat");
    }

    public void OnFail() => _goatAnimator.SetTrigger("KickAway");  // коза лягнула
}
```

Победа/поражение — через таймер раунда и `_score >= victoryScoreThreshold` в `MiniGameDefinition`.

#### RoxyMilkingEnvironment

```csharp
public class RoxyMilkingEnvironment : MonoBehaviour, IControlEnvironment
{
    [SerializeField] VideoPlayer _videoPlayer;

    public void Initialize(ControlMiniGameConfig config) => _videoPlayer.Play();

    public void OnBarStateChanged(BarState state)
    {
        // реакция Рокси: визуальное/аудио изменение по стейту шкалы
    }

    public void OnCycleComplete() { }  // не используется (isCyclic = false)

    public void OnFail() => _videoPlayer.Stop();  // адалт прерывается
}
```

#### MechanicalBullEnvironment

```csharp
public class MechanicalBullEnvironment : MonoBehaviour, IControlEnvironment
{
    [SerializeField] Animator _bullAnimator;

    public void Initialize(ControlMiniGameConfig config) { }

    public void OnBarStateChanged(BarState state)
    {
        // интенсивность движения быка по стейту: Normal = ритмично, Tense = тряска, Critical = опасно
        _bullAnimator.SetInteger("Intensity", (int)state);
    }

    public void OnCycleComplete() { }  // не используется (isCyclic = false)

    public void OnFail() => _bullAnimator.SetTrigger("ThrowOff");  // Ханна слетает с быка
}
```

---

### 20.8 Конфиги — три MiniGameDefinition

Все три ссылаются на один префаб `ControlMiniGame`. Различаются только `mechanicConfigRef`:

| id | environmentPrefabRef | isCyclic | sessionDuration | sessionDifficultyRamp | npcLeaderboard |
|---|---|---|---|---|---|
| `goats_balance` | `GoatsEnvironment` | true | > 0 | 0 (рамп через difficultyRampPerCycle) | ✅ (рекорд по комбо) |
| `roxy_milking` | `RoxyMilkingEnvironment` | false | > 0 | 0 | ❌ |
| `mechanical_bull` | `MechanicalBullEnvironment` | false | > 0 | **> 0** (нарастающая сложность по ГДД) | ❌ |

---

### 20.9 Аудио

| Ключ | Момент |
|---|---|
| `control_fill` | Шкала заполняется (loop пока isHolding в Normal) |
| `control_tense` | Переход в Tense |
| `control_fail` | Провал в Critical |
| `control_complete` | Успешное завершение цикла (OnFillComplete) |

Воспроизводятся через `IAudioManager` в `ControlMiniGame` в обработчике `OnStateChanged`.

---

### 20.10 Что намеренно НЕ делаем

- **Отдельный IMiniGame на каждую вариацию** — `IControlEnvironment` полностью изолирует визуальный контекст; `ControlMiniGame` не знает что за ним
- **Victory/Defeat в IControlEnvironment** — Environment — только event sink; решение о победе/поражении принадлежит `ControlMiniGame`
- **SeductionScaleService внутри мини-игры** — связь с глобальными шкалами только через результат flow §17.4
- **Цикличный режим для адалт-вариаций** — `roxy_milking` и `mechanical_bull` one-shot; одна заполненная шкала = завершение
- **ControlMiniGame знает о типе вариации** — только через `IControlEnvironment` контракт; никакого switch по типу вариации в основном цикле
