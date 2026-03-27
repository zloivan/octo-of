# Архитектура — Механика "Стрельба"

> Покрывает: ShootingMiniGame, ShootingMiniGameConfig, ReticleWobbleLogic, CrosshairView, ITargetMovementStrategy, Beer Pong вариация, SFX.
> Внешние зависимости: `IMiniGame` из [`architecture_minigames.md`](architecture_minigames.md) §17.3; `TimerBarView`, `ScoreCounterView` из [`architecture_minigames.md`](architecture_minigames.md)
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 18. Механика "Стрельба"

### 18.1 Контекст

Мини-игра на точность. Реализует `IMiniGame`. Shell (`MiniGameWindowUI`) и `MiniGameService` ничего не знают о конкретике механики — взаимодействие только через интерфейс.

Три вариации (`shooting`, `grace_shooting`, `beer_pong`) используют **один и тот же префаб** `ShootingMiniGame`, но получают разные `ShootingMiniGameConfig`. Визуальный контекст (фон, спрайты целей) и параметры дрожания полностью задаются конфигом.

**Ключевая геймплейная идея:** игрок управляет прицелом (следует за курсором), внутри прицела — красная точка, отклоняющаяся по случайной траектории. Клик фиксирует позицию красной точки — именно она определяет попадание, не позиция курсора.

---

### 18.2 ShootingMiniGameConfig

```
ShootingMiniGameConfig : ScriptableObject
  ├── backgroundRef: AssetReference          ← видео или спрайт фона
  ├── sessionDuration: float                 ← длительность раунда в секундах (0 = без лимита по времени)
  ├── wobbleAmplitude: float                 ← максимальное отклонение красной точки от центра
  ├── wobbleSpeed: float                     ← скорость движения точки
  ├── onMissWobbleDelta: float               ← прирост wobbleAmplitude при промахе (0 = нет эффекта)
  ├── maxMisses: int                         ← лимит промахов до поражения (0 = без лимита)
  └── grades: TargetGradeDefinition[]
```

`onMissWobbleDelta` и `maxMisses` используются для Beer Pong. В `shooting` и `grace_shooting` оба поля равны 0.

---

### 18.3 TargetGradeDefinition и TargetMovementParams

```
TargetGradeDefinition
  ├── gradeId: string                        ← "common", "uncommon", "rare"
  ├── targetSpriteRef: AssetReferenceSprite
  ├── basePoints: int                        ← очки до применения perfect-бонуса
  ├── spawnWeight: float                     ← вес при взвешенном случайном спавне
  ├── spawnCountPerSession: int              ← количество целей этого грейда за сессию
  ├── movementStrategyType: MovementStrategyType
  └── strategyParams: TargetMovementParams
        ├── speed: float
        ├── directionChangeInterval: float   ← для LinearBounce
        └── curveTightness: float            ← для Bezier

public enum MovementStrategyType { LinearBounce, Bezier, Static }
```

`Static` — для Beer Pong. Стаканы не двигаются.

**Подсчёт очков:**
- Обычное попадание: `score += grade.basePoints`
- Идеальное попадание (отклонение красной точки от центра прицела ≤ 20% от `wobbleAmplitude`): `score += grade.basePoints * 1.5f`
- Промах: 0 очков, `currentMisses++`

---

### 18.4 ITargetMovementStrategy — паттерн Стратегия

```csharp
public interface ITargetMovementStrategy
{
    void Initialize(RectTransform target, TargetMovementParams p, Rect bounds);
    void Tick(float deltaTime);
}
```

**LinearBounceStrategy** — прямолинейное движение с отскоком от `bounds`. При истечении `directionChangeInterval` — случайное новое направление.

**BezierCurveStrategy** — движение по кубической кривой Безье между случайными точками внутри `bounds`. При достижении конечной точки генерирует новую кривую.

**StaticStrategy** — `Tick` пустой. Цель не двигается.

Стратегия инстанциируется в `ShootingTargetView.Initialize()` через switch по `MovementStrategyType`.

---

### 18.5 ReticleWobbleLogic и CrosshairView

#### ReticleWobbleLogic (plain C#)

Отвечает за математику дрожания. Не знает о Unity API.

```csharp
public class ReticleWobbleLogic
{
    private float _amplitude;
    private float _speed;
    private float _phase;

    public ReticleWobbleLogic(float amplitude, float speed) { ... }

    // Вызывается при каждом промахе (Beer Pong)
    public void ApplyWobbleDelta(float delta) => _amplitude += delta;

    // Вызывается каждый кадр — возвращает текущее смещение красной точки
    public Vector2 Tick(float deltaTime)
    {
        _phase += deltaTime * _speed;
        float x = Mathf.PerlinNoise(_phase, 0f) * 2f - 1f;
        float y = Mathf.PerlinNoise(0f, _phase) * 2f - 1f;
        return new Vector2(x, y) * _amplitude;
    }

    public float CurrentAmplitude => _amplitude;
}
```

#### CrosshairView (MonoBehaviour)

Пассивный View. Следует за курсором, отображает прицел и красную точку.

**Поля (сериализованы в префабе):**
- `RectTransform _crosshairRoot` — корень прицела, следует за курсором
- `RectTransform _reticlePoint` — красная точка внутри прицела
- `RectTransform _aimingZone` — тёмно-серая зона поля

**Публичный API:**
```csharp
public Action OnAimingZoneEnter;
public Action OnAimingZoneExit;

public void SetActive(bool active)
public void SetReticleOffset(Vector2 offset)         // вызывается из ShootingMiniGame каждый кадр
public Vector2 GetReticleWorldPosition()             // позиция красной точки в canvas-координатах
public float GetReticleDeviationNormalized()         // [0,1]: 0 = в центре, 1 = на краю amplitude
```

**Поведение:**
- Пока курсор внутри `_aimingZone`: `_crosshairRoot` следует за курсором через `RectTransformUtility.ScreenPointToLocalPointInRectangle`, OS-курсор скрыт (`Cursor.visible = false`)
- При выходе за зону: прицел скрыт, OS-курсор возвращён

---

### 18.6 ShootingTargetView

`ShootingTargetView : MonoBehaviour` — визуальное представление цели. **Не обрабатывает клики** — детекция попадания происходит на уровне `ShootingMiniGame`.

**Методы:**
- `Initialize(TargetGradeDefinition grade, Rect bounds)` — инстанциирует стратегию, применяет спрайт
- `Tick(float deltaTime)` — делегирует `strategy.Tick()`; если `RectTransform` вышел за `bounds` → вызывает `OnExpired`
- `ContainsCanvasPoint(Vector2 point)` → `bool` — проверяет, находится ли точка внутри `RectTransform` цели
- `Deactivate()` — скрывает объект, помечает как неактивный

**События:**
```csharp
public event Action<ShootingTargetView> OnExpired;  // цель ушла за bounds без попадания
```

Нет `OnPointerClick` — цель не является кликабельным объектом.

---

### 18.7 ShootingMiniGame

`ShootingMiniGame : MonoBehaviour, IMiniGame`

Владеет игровым циклом, детекцией попадания, управлением прицелом и состоянием Beer Pong.

**Поля:**
```csharp
ShootingMiniGameConfig _config;
ReticleWobbleLogic _wobble;
CrosshairView _crosshairView;       // сериализован в префабе
TimerBarView _timerBar;             // сериализован в префабе
ScoreCounterView _scoreCounter;     // сериализован в префабе

int _score;
int _missCount;
List<ShootingTargetView> _activeTargets;
bool _isAiming;
```

**Initialize(ScriptableObject mechanicConfig):**
- Кастует до `ShootingMiniGameConfig`
- Устанавливает фон из `config.backgroundRef`
- Создаёт `new ReticleWobbleLogic(config.wobbleAmplitude, config.wobbleSpeed)`
- Подписывается на `_crosshairView.OnAimingZoneEnter/Exit`

**RunAsync(RectTransform container, CancellationToken ct):**
1. Запускает таймер сессии (`config.sessionDuration`, если > 0)
2. Формирует спавн-очередь по `spawnWeight` и `spawnCountPerSession`
3. На каждый кадр (`UniTask.Yield`):
   - Вызывает `_crosshairView.SetReticleOffset(_wobble.Tick(deltaTime))`
   - Для каждого `_activeTarget` вызывает `target.Tick(deltaTime)`
4. Обрабатывает клик (`Input.GetMouseButtonDown(0)`):
   - Если `!_isAiming` → игнорировать (курсор вне зоны)
   - Получает `reticlePos = _crosshairView.GetReticleWorldPosition()`
   - Ищет первый `target.ContainsCanvasPoint(reticlePos)`
   - **Попадание:** вычисляет очки (см. ниже), вызывает `target.Deactivate()`, обновляет `_scoreCounter`
   - **Промах:** `_missCount++`; если `config.onMissWobbleDelta > 0` → `_wobble.ApplyWobbleDelta(config.onMissWobbleDelta)`; если `config.maxMisses > 0 && _missCount >= config.maxMisses` → досрочное завершение (поражение)
5. При `ct.IsCancellationRequested` → немедленно завершает
6. Возвращает `_score`

**Подсчёт очков попадания:**
```csharp
float deviation = _crosshairView.GetReticleDeviationNormalized();
bool isPerfect = deviation <= 0.2f;
int points = Mathf.RoundToInt(grade.basePoints * (isPerfect ? 1.5f : 1.0f));
_score += points;
```

---

### 18.8 Beer Pong как вариация

Beer Pong реализуется через тот же `ShootingMiniGame` с `ShootingConfig_BeerPong`:

| Параметр | Значение | Эффект |
|---|---|---|
| `movementStrategyType` | `Static` | Стаканы не двигаются |
| `onMissWobbleDelta` | `> 0` | Каждый промах увеличивает дрожание |
| `maxMisses` | `N` | При N промахах — досрочное поражение (критическое опьянение ГГ) |
| `wobbleAmplitude` | низкое | Старт с малым дрожанием |
| `sessionDuration` | `0` | Без лимита по времени — только по промахам |

Победа: `victoryScoreThreshold` в `MiniGameDefinition` — нужно попасть в N стаканов до исчерпания `maxMisses`.

Отдельная `IMiniGame` реализация не нужна — все отличия полностью закрыты конфигом.

---

### 18.9 Вариации — три MiniGameDefinition

Все три ссылаются на один префаб `ShootingMiniGame`. Различаются только `mechanicConfigRef`:

| id | mechanicConfigRef | Характеристики |
|---|---|---|
| `shooting` | `ShootingConfig_Default` | Стандартное дрожание, LinearBounce/Bezier цели |
| `grace_shooting` | `ShootingConfig_Grace` | Малое дрожание, медленные цели, увеличенный `sessionDuration` |
| `beer_pong` | `ShootingConfig_BeerPong` | Static цели, `onMissWobbleDelta > 0`, `maxMisses > 0` |

---

### 18.10 Аудио

| Ключ | Момент |
|---|---|
| `shooting_hit` | Попадание в цель |
| `shooting_perfect` | Идеальное попадание |
| `shooting_miss` | Клик в зоне, но красная точка вне цели |

Воспроизводятся через `IAudioManager` в `ShootingMiniGame` после определения результата клика.

---

### 18.11 Что намеренно НЕ делаем

- **Клик непосредственно по цели (`OnPointerClick` на `ShootingTargetView`)** — детекция только через позицию красной точки, не курсора
- **Непрерывный accuracyModifier** — только бинарно: обычное попадание или идеальное (+50%)
- **Отдельная IMiniGame для Beer Pong** — все отличия покрыты конфигом
- **Аппаратный курсор поверх прицела** — в зоне прицеливания OS-курсор всегда скрыт

---

## 12. Что НЕ делаем (фрагмент — Эпик 5)

**По поводу специфики (Эпик 5 добавления):**
- **Отдельная IMiniGame-реализация на каждую вариацию Стрельбы** — один `ShootingMiniGame`, три `ShootingMiniGameConfig`; визуальный контекст в конфиге
- **Таймаут цели как жёсткая механика** — цель живёт до выхода за bounds; скорость движения управляет эффективным временем жизни через `strategyParams`
- **Штраф за промах** — 0 очков, цель продолжает движение; добавление штрафа потребует только изменения в `ShootingMiniGame`
