# Затемнение туториала с «дыркой» под кнопку

Весь экран темнеет, а нужная кнопка остаётся яркой, рисуется поверх затемнения и нажимается. Рядом пульсирует рука.

**Это не шейдер.** Эффект собран из вложенных `Canvas` с разным порядком отрисовки (`sortingOrder`). Отдельного материала и шейдера нет, работает на любом рендер-пайплайне.

## Как это работает

Всё на одном экранном канвасе (Screen Space – Overlay), слои рисуются по возрастанию `sortingOrder`:

| Порядок | Что рисуется |
|---|---|
| 0 | Обычный HUD |
| 10 | Затемнение `Dim` — чёрная картинка на весь экран |
| 11 | Кнопка-цель: её собственный `Canvas` на время шага включает `overrideSorting` |
| 12 | Подпись, «Пропустить» и рука |

Затемнение ловит клики (`raycastTarget`), поэтому всё под ним не нажимается. У кнопки-цели свой `GraphicRaycaster`, поэтому она над затемнением кликается. Шаг сменился — у цели выключается `overrideSorting`, и она возвращается под затемнение (или затемнение гаснет).

## Иерархия

```text
Hud                      Canvas (Overlay), sortingOrder 0
├─ ... обычные элементы
├─ UpgradesButton        Canvas (overrideSorting выкл.) + GraphicRaycaster + TutorialFocusTarget
├─ UpgradesPanel/.../Close   то же самое (цель внутри окна)
└─ Tutorial              Canvas, overrideSorting вкл., sortingOrder 10 + TutorialView   ← последний ребёнок HUD
   ├─ Dim                Image чёрный, альфа 0,97, растянут на весь экран, Raycast Target вкл. + CanvasGroup
   └─ Overlay            Canvas, overrideSorting вкл., sortingOrder 12 + GraphicRaycaster
      ├─ CaptionArea / CaptionPanel / Label   подпись (TMP)
      ├─ SkipButton
      └─ Hand            картинка руки
```

## Настройки по объектам

**Корень (`Hud`).** Обычный `Canvas` Screen Space – Overlay, `sortingOrder` 0. Запомни его Additional Shader Channels (у нас TexCoord1, Normal, Tangent).

**`Tutorial`:**
- `Canvas`: Override Sorting вкл., Sort Order 10, те же Additional Shader Channels, что у корня;
- растянут на весь экран;
- стоит последним ребёнком HUD.

**`Dim`:**
- `Image` с белым спрайтом или без спрайта, цвет `(0, 0, 0, 0.97)`, Raycast Target вкл.;
- растянут на весь экран;
- `CanvasGroup` для плавного появления: альфа 0 → 1 за 0,25 с;
- по умолчанию выключен.

**`Overlay`:**
- `Canvas`: Override Sorting вкл., Sort Order 12, те же каналы;
- `GraphicRaycaster`, иначе «Пропустить» не нажмётся.

**Каждая кнопка-цель:**
- `Canvas` с выключенным Override Sorting и теми же каналами;
- `GraphicRaycaster`;
- `TutorialFocusTarget`.

Компоненты добавляются **заранее в префабе**, не в рантайме. Пока Override Sorting выключен, вложенный `Canvas` рисуется как часть родителя, и кнопка выглядит как обычно.

## Код

- `TutorialFocusTarget.cs`:
  - `Focus(order)` включает `overrideSorting` и ставит порядок;
  - `Release()` выключает.
- `TutorialView.cs`:
  - `Point(target, isDimmed)` подсвечивает кнопку: включает затемнение, поднимает цель на порядок 11 (`_focusSortingOrder`) и ставит руку у центра кнопки. Центр считается по `GetWorldCorners` и переводится в координаты области;
  - `ClearPointer()` всё снимает;
  - `Show(caption, isTyped)` — подпись с печатью по буквам, `Raise(bool)` поднимает плашку подписи, пока открыто окно. К затемнению это не относится, можно выкинуть.
  - Анимации на DOTween (`DOFade`, `DOScale`), все с `SetUpdate(true)`, чтобы работали при `timeScale = 0`.

Пример вызова из презентера:

```csharp
_view.Point(upgradesButtonRect, isDimmed: true);   // затемнить всё, кроме кнопки
_view.Point(someRect, isDimmed: false);            // только рука, без затемнения
_view.ClearPointer();                              // снять
```

## Перенос в другой проект

1. Скопируй оба скрипта и поменяй `namespace` под свой проект.
2. Нужны DOTween и TextMeshPro. Если подписи не нужны, удали из `TutorialView` поля и методы `_caption`, `_skip*`, `Show`, `ShowSkipLabel`, `Raise`.
3. Собери иерархию из таблицы выше в префабе своего HUD. `Tutorial` — последним ребёнком корневого канваса.
4. На каждую кнопку, которую туториал будет показывать, повесь `Canvas` (Override Sorting выкл.) + `GraphicRaycaster` + `TutorialFocusTarget`.
5. В `TutorialView` протяни ссылки: `_area` (сам `Tutorial`), `_dim` (CanvasGroup на `Dim`), `_hand`, `_captionPanel`, `_caption`, `_skipButton`, `_skipLabel`.

## Подводные камни

- **Порядок 11 должен быть между затемнением и оверлеем.** Если цель окажется выше 12, она перекроет подпись и руку; ниже 10 — уйдёт под затемнение.
- **Каналы шейдера.** У вложенного `Canvas` Additional Shader Channels должны совпадать с корнем. Иначе TMP-текст и спецэффекты на кнопке (обводка, блик) отрисуются неправильно, пока кнопка поднята.
- **Без `GraphicRaycaster` на цели** кнопка видна поверх затемнения, но клик уходит в затемнение.
- **`overrideSorting` на выключенном объекте.** Если ставить его из editor-скрипта через свойство, значение не сохраняется в префаб. Ставь через `SerializedObject` (`m_OverrideSorting`). В рантайме на активном объекте свойство работает.
- **`CanvasGroup` родителя действует и на вложенный `Canvas`.** Если окно с кнопкой-целью плавно появляется через `CanvasGroup`, поднятая кнопка проявляется вместе с ним, это нормально.
- **Кнопка в окне, которое ещё анимируется** (масштаб при открытии): руку ставь после того, как окно открылось, иначе центр посчитается по промежуточному размеру.
