# Архитектура — UI (Журнал и Мета-экраны)

> Покрывает: JournalWindowUI (контейнер вкладок), MainMenuUI, PauseMenuUI, SettingsUI, SaveLoadUI.
> Внешние зависимости: `SeductionScaleService` из [`architecture_scales.md`](architecture_scales.md) (ContactsTabView читает ATT/SUS); `MiniGameService.State.UnlockedAdultIds` из [`architecture_minigames.md`](architecture_minigames.md) (GalleryWindowUI) — см. `architecture_minigames.md` §23.6
> Индекс: [`architecture_index.md`](architecture_index.md) | Ядро: [`architecture_core.md`](architecture_core.md)

---

## 22. Журнал (JournalWindowUI)

### 22.1 Контекст

Журнал — CustomUI с тремя вкладками: Scores, Contacts, Progress.
Scores = существующий LeaderboardPanelView (Фича 4.2).
Contacts — новая вкладка (Фича 4.6).
Progress — TBD, ждём ГДД.

### 22.2 JournalWindowUI

`JournalWindowUI : CustomUI` — контейнер вкладок.

Открывается командой:
```
@openJournal tab:scores|contacts|progress
```

Переключение вкладок — через `SetActiveTab(JournalTab tab)`.
`JournalTab` enum: `Scores`, `Contacts`, `Progress`.

### 22.3 ContactsTabView / 22.4 ScaleSnapshotView

> Описание находится в [`architecture_scales.md`](architecture_scales.md) §22.3–22.4 — меняется вместе с `SeductionScaleService`.

---

## 23. Мета-UI экраны

### 23.1 Принцип

Все экраны — кастомные скины поверх встроенных Naninovel UI.
Никаких параллельных систем Save/Load или Settings — только CustomUI замена встроенных.

### 23.2 MainMenuUI

`MainMenuUI : CustomUI` регистрируется как `ITitleUI` в Naninovel.
- Continue активна только при `IStateManager.AnySaveExists()`
- Gallery кнопка → `GalleryWindowUI.Show()`
- Фоновая анимация через `IBackgroundManager`

### 23.3 PauseMenuUI

`PauseMenuUI : CustomUI`.
Кнопки: Resume → `Hide()`; Settings → `SettingsUI.Show()`; Save/Load → `SaveLoadUI.Show(mode)`; Main Menu → `IStateManager.ResetStateAsync()` + `@goto` title.

### 23.4 SettingsUI

`SettingsUI : CustomUI` заменяет встроенный Naninovel SettingsUI.
- Аудио слайдеры через `IAudioManager.MasterVolume`, `.MusicVolume`, `.SoundVolume`
- Language dropdown через `ILocalizationManager.SelectLanguageAsync(locale)`

### 23.5 SaveLoadUI

`SaveLoadUI : CustomUI` заменяет встроенный Naninovel SaveLoadUI.
Открывается в двух режимах: `Save` и `Load` — один префаб, разное поведение кнопок слотов.
Скриншот слота из `GameStateSlot.QuickSaveThumb`.

### 23.6 GalleryWindowUI

> Описание находится в [`architecture_minigames.md`](architecture_minigames.md) §23.6 — меняется вместе с `MiniGameService.State`.

### 23.7 Что намеренно НЕ делаем

- **Своя система Save/Load** — используем Naninovel IStateManager полностью
- **Своя система Settings** — используем Naninovel IAudioManager / ILocalizationManager
