# Архитектура — Механика "Борьба"

> Покрывает: FightingMiniGame, FightingMiniGameConfig, IFightingEnvironment, три реализации (Mannekin, HannahAdult, MudFight), SFX, §12 Эпик 6 специфика.
> Внешние зависимости: `IMiniGame` из [`architecture_minigames.md`](architecture_minigames.md) §17.3; `TimerBarView`, `ScoreCounterView` из [`architecture_minigames.md`](architecture_minigames.md)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 19. Механика "Борьба"

### 19.1 Контекст

Мини-игра на реакцию. Реализует `IMiniGame`. Три вариации (`fighting_training`, `hannah_adult_fighting`, `mud_fighting`) используют **один и тот же префаб** `FightingMiniGame`, но получают разные `FightingMiniGameConfig`.

**Ключевая идея разделения:** базовый игровой цикл (индикатор → ввод → грейд → следующий) одинаков во всех вариациях. Всё что меняется — интерпретация результата и визуальный контекст — живёт в `IFightingEnvironment` prefab'е, подключаемом через конфиг.

---

### 19.2 FightingMiniGameConfig

```
FightingMiniGameConfig : ScriptableObject
  ├── environmentPrefabRef: AssetReference     ← prefab с IFightingEnvironment компонентом
  ├── sessionDuration: float                   ← длительность раунда (0 = не ограничено)
  ├── initialIndicatorDuration: float          ← 1.2с — начальное окно тайминга
  ├── minIndicatorDuration: float              ← 0.6с — минимальное окно
  ├── difficultyRampDuration: float            ← за сколько секунд дойти от initial до min
  ├── neutralDisplayDuration: float            ← пауза нейтрального индикатора между основными
  ├── perfectWindowFraction: float             ← доля от duration, считающаяся Perfect (например 0.2)
  ├── penaltyMode: PenaltyModeType             ← ScoreDeduct | TimePenalty | None
  ├── penaltyValue: float                      ← величина штрафа
  └── environmentDrivesVictory: bool           ← если true — победа/поражение решает Environment
```

```csharp
public enum PenaltyModeType { ScoreDeduct, TimePenalty, None }
public enum InputGrade { Perfect, Good, Miss, Wrong }
```

---

### 19.3 IFightingEnvironment

```csharp
public interface IFightingEnvironment
{
    void Initialize(FightingMiniGameConfig config);
    void OnInput(MouseButton button, InputGrade grade);

    bool IsVictory { get; }            // MudFight: HP соперницы = 0; HannahAdult: все стейты пройдены
    event Action OnEnvironmentDefeat;  // HannahAdult: suspicion max; MudFight: HP союзницы = 0
}
```

`FightingMiniGame` знает только об `IFightingEnvironment` — не знает что за ним: манекен с анимацией, видео адалт-сцена или шкалы здоровья. Замена реализации — только внутри prefab'а, без изменений в `FightingMiniGame`.

---

### 19.4 FightingIndicatorLogic (plain C#)

Отвечает за математику тайминга. Нет зависимостей от Unity.

```csharp
public class FightingIndicatorLogic
{
    private float _duration;
    private float _perfectWindowFraction;

    public MouseButton RequiredButton { get; private set; }
    public float ElapsedTime { get; private set; }
    public float NormalizedProgress => ElapsedTime / _duration;  // [0,1] — для анимации View
    public bool IsExpired => ElapsedTime >= _duration;

    public void Reset(MouseButton button, float duration) { ... }
    public void Tick(float deltaTime) => ElapsedTime += deltaTime;

    public InputGrade Evaluate(MouseButton pressed)
    {
        if (pressed != RequiredButton) return InputGrade.Wrong;
        float center = _duration * 0.5f;
        float delta = Mathf.Abs(ElapsedTime - center);
        if (delta <= _duration * _perfectWindowFraction) return InputGrade.Perfect;
        return InputGrade.Good;
    }
}
```

`Evaluate` проверяет расстояние от центра тайминга (`_duration * 0.5f`). Perfect = попадание в зону `perfectWindowFraction` вокруг центра.

---

### 19.5 FightingIndicatorView (MonoBehaviour)

Пассивный View. Отображает текущий индикатор.

**Поля (сериализованы в префабе):**
- `Image _circleTimer` — `fillAmount = 1 - NormalizedProgress` (сужается к центру)
- `Image _buttonIcon` — синий (LMB) / красный (RMB) / нейтральный
- `CanvasGroup _root` — для fade эффектов

**Публичный API:**
```csharp
public void SetState(MouseButton? button)   // null = neutral
public void SetProgress(float normalized)   // вызывается каждый кадр из FightingMiniGame
public void PlayHitEffect()                 // pulse animation при правильном нажатии
```

---

### 19.6 FightingCommentView (MonoBehaviour)

Простой label с fade-in/out. `FightingMiniGame` вызывает `Show(string text)` при достижении пороговых условий (Combo — 5 правильных подряд). Счётчик `_comboCount` живёт в `FightingMiniGame`.

```csharp
public void Show(string commentKey)  // fade-in → hold → fade-out
```

---

### 19.7 FightingMiniGame : MonoBehaviour, IMiniGame

Владеет игровым циклом. Координирует все компоненты.

**Поля:**
```csharp
FightingMiniGameConfig _config;
FightingIndicatorLogic _indicatorLogic;
IFightingEnvironment _environment;

FightingIndicatorView _indicatorView;      // сериализован в префабе
TimerBarView _timerBar;                    // переиспользуется из Эпика 4
ScoreCounterView _scoreCounter;            // переиспользуется из Эпика 4
FightingCommentView _commentView;          // сериализован в префабе

int _score;
int _comboCount;
float _remainingTime;
float _currentIndicatorDuration;           // lerp от initial до min
bool _isEnvironmentVictory;
bool _isEnvironmentDefeat;
```

**Initialize(ScriptableObject mechanicConfig):**
- Кастует до `FightingMiniGameConfig`
- Загружает `environmentPrefabRef`, инстанциирует в `container`
- Получает `IFightingEnvironment` с инстанса
- Подписывается на `_environment.OnEnvironmentDefeat`

**RunAsync(RectTransform container, CancellationToken ct):**
```
Цикл:
  1. Рассчитать _currentIndicatorDuration = Lerp(initial, min, sessionElapsed / rampDuration)
  2. Сгенерировать случайный MouseButton → _indicatorLogic.Reset(button, duration)
  3. Показать _indicatorView.SetState(button)
  4. Каждый кадр:
       _indicatorLogic.Tick(deltaTime)
       _indicatorView.SetProgress(_indicatorLogic.NormalizedProgress)
       Проверить ввод (GetMouseButtonDown 0 или 1)
  5. При вводе или expiry → Evaluate → ApplyResult
  6. Показать neutral на neutralDisplayDuration
  7. Проверить условия завершения → выход
```

**ApplyResult:**
```csharp
private void ApplyResult(InputGrade grade, MouseButton pressed)
{
    if (grade == InputGrade.Wrong || grade == InputGrade.Miss)
    {
        _comboCount = 0;
        ApplyPenalty();
        _indicatorView.PlayHitEffect();
    }
    else
    {
        _score += grade == InputGrade.Perfect ? 2 : 1;
        _comboCount++;
        if (_comboCount == 5) _commentView.Show("Combo!");
        _indicatorView.PlayHitEffect();
    }

    _environment.OnInput(pressed, grade);

    if (_environment.IsVictory) _isEnvironmentVictory = true;
}
```

**ApplyPenalty:**
```csharp
private void ApplyPenalty()
{
    switch (_config.penaltyMode)
    {
        case PenaltyModeType.TimePenalty:  _remainingTime -= _config.penaltyValue; break;
        case PenaltyModeType.ScoreDeduct:  _score = Mathf.Max(0, _score - (int)_config.penaltyValue); break;
    }
}
```

**Условия завершения:**

| Условие | Источник |
|---|---|
| `sessionDuration > 0 && _remainingTime <= 0` | Стандартный таймер (Training) |
| `_isEnvironmentVictory` | `IFightingEnvironment.IsVictory` (HannahAdult, MudFight) |
| `_isEnvironmentDefeat` | `OnEnvironmentDefeat` event (HannahAdult, MudFight) |
| `ct.IsCancellationRequested` | Skip нажат |

Возврат: `environmentDrivesVictory ? (_isEnvironmentVictory ? victoryScore : 0) : _score`

---

### 19.8 Три реализации IFightingEnvironment

#### MannekinEnvironment

```csharp
public class MannekinEnvironment : MonoBehaviour, IFightingEnvironment
{
    [SerializeField] Animator _mannekinAnimator;

    public bool IsVictory => false;         // Training — победа через таймер/счёт
    public event Action OnEnvironmentDefeat; // никогда не вызывается

    public void Initialize(FightingMiniGameConfig config) { }

    public void OnInput(MouseButton button, InputGrade grade)
    {
        if (grade == InputGrade.Perfect || grade == InputGrade.Good)
            _mannekinAnimator.SetTrigger("Hit");  // манекен трясётся
    }
}
```

ЛКМ и ПКМ как разные руки — обе кнопки дают одинаковый `Hit` триггер, анимация манекена визуально показывает два кулака.

#### HannahAdultEnvironment

```csharp
public class HannahAdultEnvironment : MonoBehaviour, IFightingEnvironment
{
    [SerializeField] VideoPlayer _videoPlayer;
    [SerializeField] float[] _stateCheckpoints;  // временные метки в видео для каждого стейта
    [SerializeField] int _maxLocalSuspicion = 5;

    private int _currentState;
    private int _localSuspicion;

    public bool IsVictory => _currentState >= _stateCheckpoints.Length;
    public event Action OnEnvironmentDefeat;

    public void Initialize(FightingMiniGameConfig config)
    {
        _videoPlayer.Play();
    }

    public void OnInput(MouseButton button, InputGrade grade)
    {
        if (grade == InputGrade.Perfect || grade == InputGrade.Good)
        {
            _currentState++;
            if (_currentState < _stateCheckpoints.Length)
                _videoPlayer.time = _stateCheckpoints[_currentState];  // перемотка к следующему стейту
        }
        else
        {
            _localSuspicion++;
            if (_localSuspicion >= _maxLocalSuspicion)
            {
                _videoPlayer.Stop();  // адалт заканчивается досрочно
                OnEnvironmentDefeat?.Invoke();
            }
        }
    }
}
```

`_localSuspicion` — локальная шкала, не связана с глобальным `SeductionScaleService`. Связь с глобальными шкалами — только через результат `MiniGameService` → `StartMiniGameCommand` → `SeductionScaleService` (стандартный flow §17.4).

Видео управляется только внутри prefab'а — `FightingMiniGame` не знает о `VideoPlayer`.

Семантика ЛКМ ("активное действие") и ПКМ ("поддержание контроля") по ГДД реализована внутри `HannahAdultEnvironment` — `FightingMiniGame` об этом не знает. При необходимости изменить интерпретацию кнопок достаточно поправить только реализацию environment.

#### MudFightEnvironment

```csharp
public class MudFightEnvironment : MonoBehaviour, IFightingEnvironment
{
    [SerializeField] HealthBarView _allyHealthBar;
    [SerializeField] HealthBarView _enemyHealthBar;
    [SerializeField] int _maxHP = 10;

    private int _allyHP;
    private int _enemyHP;

    public bool IsVictory => _enemyHP <= 0;
    public event Action OnEnvironmentDefeat;

    public void Initialize(FightingMiniGameConfig config)
    {
        _allyHP = _enemyHP = _maxHP;
        _allyHealthBar.SetValue(_allyHP, _maxHP);
        _enemyHealthBar.SetValue(_enemyHP, _maxHP);
    }

    public void OnInput(MouseButton button, InputGrade grade)
    {
        bool isHit = grade == InputGrade.Perfect || grade == InputGrade.Good;

        if (isHit)
        {
            if (button == MouseButton.LeftButton)   // атака → урон сопернице
                SetEnemyHP(_enemyHP - 1);
            // ПКМ correct = уклонение = ничего не делаем с HP
        }
        else
        {
            SetAllyHP(_allyHP - 1);                 // ошибка → урон союзнице
        }
    }

    private void SetAllyHP(int value)
    {
        _allyHP = Mathf.Max(0, value);
        _allyHealthBar.SetValue(_allyHP, _maxHP);
        if (_allyHP <= 0) OnEnvironmentDefeat?.Invoke();
    }

    private void SetEnemyHP(int value)
    {
        _enemyHP = Mathf.Max(0, value);
        _enemyHealthBar.SetValue(_enemyHP, _maxHP);
    }
}
```

`HealthBarView` — простой компонент: `Image fillAmount`, метод `SetValue(int current, int max)`. Два экземпляра в префабе `MudFightEnvironment`.

---

### 19.9 Конфиги — три MiniGameDefinition

Все три ссылаются на один префаб `FightingMiniGame`. Различаются только `mechanicConfigRef`:

| id | environmentPrefabRef | sessionDuration | penaltyMode | environmentDrivesVictory |
|---|---|---|---|---|
| `fighting_training` | `MannekinEnvironment` | 60с | `TimePenalty` | false |
| `hannah_adult_fighting` | `HannahAdultEnvironment` | 0 (без лимита) | `None` | true |
| `mud_fighting` | `MudFightEnvironment` | 0 (без лимита) | `None` | true |

`difficultyRampDuration = 40с` для `fighting_training` — за 40 секунд тайминг lerp'ится от 1.2с до 0.6с.

---

### 19.10 Аудио

| Ключ | Момент |
|---|---|
| `fighting_hit_perfect` | Perfect нажатие |
| `fighting_hit_good` | Good нажатие |
| `fighting_miss` | Пропуск или неверная кнопка |
| `fighting_combo` | Combo достигнут |

---

### 19.11 Что намеренно НЕ делаем

- **Отдельный IMiniGame на каждую вариацию** — `IFightingEnvironment` полностью изолирует визуальный контекст; `FightingMiniGame` не знает что за ним
- **Глобальный SeductionScaleService внутри мини-игры** — `HannahAdultEnvironment` владеет локальным `_localSuspicion`; связь с глобальными шкалами только через результат flow §17.4
- **Отдельный HealthBarView для каждой среды** — один переиспользуемый компонент, два экземпляра в префабе `MudFightEnvironment`
- **FightingMiniGame знает о типе вариации** — только через `IFightingEnvironment` контракт; никакого switch по типу вариации в основном цикле
- **VideoPlayer в FightingMiniGame** — видео управляется только внутри `HannahAdultEnvironment`; смена на другой тип воспроизведения не затрагивает механику

---

## 12. Что НЕ делаем (фрагмент — Эпик 6)

**По поводу специфики (Эпик 6 добавления):**
- **Отдельный IMiniGame на каждую вариацию Борьбы** — `IFightingEnvironment` полностью изолирует визуальный контекст; `FightingMiniGame` не знает что за ним
- **Глобальный SeductionScaleService внутри мини-игры** — `HannahAdultEnvironment` владеет локальным `_localSuspicion`; связь с глобальными шкалами только через результат flow §17.4
- **Отдельный HealthBarView для каждой среды** — один переиспользуемый компонент, два экземпляра в префабе `MudFightEnvironment`
- **FightingMiniGame знает о типе вариации** — только через `IFightingEnvironment` контракт; никакого switch по типу вариации в основном цикле
- **VideoPlayer в FightingMiniGame** — видео управляется только внутри `HannahAdultEnvironment`; смена на другой тип воспроизведения не затрагивает механику
