# ion-action-sheet — Plan

`AvonicActionSheet`: Ionic's action sheet, md, on the overlay system ([#5](../../infra/overlays/README.md)) and the base's inline
overlays, `IsOpen` and `Trigger` ([#42](../ion-modal/README.md)). Its buttons are plain data, `ActionSheetButton`, as Ionic's are
and as `AlertButton` is ([#39](../ion-alert/README.md)), so that `ion-select` (#45) can build them in code: its
`createActionSheetButtons` makes one per option with `htmlAttributes: { role: 'radio', 'aria-checked' }`, a `handler`, a
`cssClass`, `disabled`, and a `cancel` button last.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch)
and at 1024 x 768; the raw output is [`ref/layout.json`](ref/layout.json) and [`ref/behaviour.json`](ref/behaviour.json), from the
script COMPARISON.md describes.

## Fidelity checklist

`node tools/ionic-api.js ion-action-sheet`.

### Props (14)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `true` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [x] | `buttons` | `Buttons` | `IList<ActionSheetButton>` | empty | `ActionSheetButton` converts from a `string`, Ionic's string form: `{ text }`, with no role (unlike the alert's, a string `"Cancel"` isn't a cancel button: measured) |
| [x] | `cssClass` | `CssClass` | `string?` | | Style classes on the action sheet, space-separated. Ionic's array form is the same string |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `header` | `Header` | `string?` | | `.action-sheet-title` when not null (an empty string still makes it, 60 high) |
| [x] | `htmlAttributes` | `HtmlAttributes` | `IReadOnlyDictionary<string, object?>?` | | Spread on the host. What maps: `aria-label` (its automation name when there's no header, as `aria-labelledby` beats it), `id` (its `AutomationId`). Anything else is an HTML attribute with nothing to do in Avalonia, and is ignored |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | `AvonicOverlay`'s |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | `true` | The overlay's |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `subHeader` | `SubHeader` | `string?` | | `.action-sheet-sub-title`, inside the title, so only with a header, and only when not empty |
| [ ] | `translucent` | none | | `false` | Not applicable: it only adds `.action-sheet-translucent`, which only `action-sheet.ios.css` reads; md draws nothing different, so it would be a dead prop (rule 3). The alert omits its own for the same reason |
| [x] | `trigger` | `Trigger` | `string?` | | `AvonicOverlay`'s |

`overlayIndex`, `delegate` and `hasController` are `@internal`.

### `ActionSheetButton` (Ionic's interface)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `text` | `Text` | |
| [x] | `role` | `Role` | `cancel`: rendered last, in a group of its own, and dismisses with no say from its handler; `cancel` and `backdrop` (`isCancel`) dismiss at once; any role is the dismiss's role, and the pseudoclass `:role-<role>` on its control (Ionic's `action-sheet-<role>`); `selected` is also `:selected` (`.action-sheet-selected`: bold, and its `::after`) |
| [x] | `icon` | `Icon` | An ionicons name (or what `AvonicIcon.Icon` takes), before the text, 24 px, `--color` |
| [x] | `cssClass` | `CssClass` | Style classes on its control |
| [x] | `id` | `Id` | Its `AutomationId`; `action-sheet-button-<overlay index>-<index>` when null, as Ionic's id. Not on the cancel button, as Ionic doesn't render it there |
| [x] | `htmlAttributes` | `HtmlAttributes` | Spread on the `<button>`: `aria-label` names it; `role: "radio"` makes it a radio (below) with `aria-checked: "true"` the checked one; `disabled` disables only the cancel button, as on the others Ionic's own `disabled` comes after the spread; `id` names only the cancel button. Others are ignored |
| [x] | `handler` | `Handler` | `Func<object?>`: `false` keeps the sheet open; a `Task` or `ValueTask` is awaited and its result read the same way; anything else dismisses |
| [x] | `data` | `Data` | The dismiss's data |
| [x] | `disabled` | `Disabled` | Not interactive, opacity 0.4, `--button-color-disabled`. Ignored on the cancel button, as Ionic's docs say and its render does (measured) |

The select's rich options (`startContent`, `endContent`, `description`: Ionic's `SelectActionSheetButton`, not the public
interface) are for #45, as the alert left its own.

### Events (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionActionSheetWillPresent` | `IonActionSheetWillPresent` | Through `ControlWillPresentEvent`, before `WillPresent` |
| [x] | `ionActionSheetDidPresent` | `IonActionSheetDidPresent` | |
| [x] | `ionActionSheetWillDismiss` | `IonActionSheetWillDismiss` | `OverlayEventArgs` |
| [x] | `ionActionSheetDidDismiss` | `IonActionSheetDidDismiss` | `OverlayEventArgs` |
| [x] | `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | the overlay's | |

### Methods (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `present()` | `Present()` | And `Present(TopLevel)` |
| [x] | `dismiss(data?, role?)` | `Dismiss(data, role)` | |
| [x] | `onDidDismiss()`, `onWillDismiss()` | the overlay's | `OverlayEventDetail(Data, Role)` |

### Slots, parts: none. The template's parts are named after Ionic's classes: `PART_Backdrop`, `PART_Wrapper` (`.action-sheet-wrapper`), `PART_Container`, `PART_Group`, `PART_Title`, `PART_SubTitle`, `PART_Buttons`, `PART_CancelGroup`.

### CSS custom properties (24)

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--backdrop-opacity` | `Avonic.ActionSheet.BackdropOpacity` | unset, following `Avonic.BackdropOpacity` (0.32); read by the animations |
| [x] | `--background` | `Avonic.ActionSheet.Background` | unset, following `Avonic.OverlayBackgroundColorBrush` |
| [x] | `--color` | `Avonic.ActionSheet.Color` | `rgba(text, 0.54)`: the title, sub title and icons |
| [x] | `--width` | `Avonic.ActionSheet.Width` | `100%` |
| [x] | `--min-width` | `Avonic.ActionSheet.MinWidth` | `auto` |
| [x] | `--max-width` | `Avonic.ActionSheet.MaxWidth` | 500 |
| [x] | `--height` | `Avonic.ActionSheet.Height` | `auto` |
| [x] | `--min-height` | `Avonic.ActionSheet.MinHeight` | `auto` |
| [x] | `--max-height` | `Avonic.ActionSheet.MaxHeight` | unset: `100%` less `Avonic.SafeAreaTop` and `Avonic.SafeAreaBottom` |
| [x] | `--button-background` | `Avonic.ActionSheet.ButtonBackground` | transparent |
| [x] | `--button-background-activated` | `Avonic.ActionSheet.ButtonBackgroundActivated` | transparent |
| [x] | `--button-background-activated-opacity` | `Avonic.ActionSheet.ButtonBackgroundActivatedOpacity` | 0 |
| [x] | `--button-background-focused` | `Avonic.ActionSheet.ButtonBackgroundFocused` | unset: `currentColor`, the button's `Foreground` |
| [x] | `--button-background-focused-opacity` | `Avonic.ActionSheet.ButtonBackgroundFocusedOpacity` | 0.12 |
| [x] | `--button-background-hover` | `Avonic.ActionSheet.ButtonBackgroundHover` | unset: `currentColor` |
| [x] | `--button-background-hover-opacity` | `Avonic.ActionSheet.ButtonBackgroundHoverOpacity` | 0.04 |
| [x] | `--button-background-selected` | `Avonic.ActionSheet.ButtonBackgroundSelected` | unset: `currentColor` |
| [x] | `--button-background-selected-opacity` | `Avonic.ActionSheet.ButtonBackgroundSelectedOpacity` | 0 |
| [x] | `--button-color` | `Avonic.ActionSheet.ButtonColor` | unset, following `Avonic.TextColorStep150Brush` (`#262626`) |
| [x] | `--button-color-activated` | `Avonic.ActionSheet.ButtonColorActivated` | unset, following `--button-color` |
| [x] | `--button-color-focused` | `Avonic.ActionSheet.ButtonColorFocused` | unset, following `--button-color` |
| [x] | `--button-color-hover` | `Avonic.ActionSheet.ButtonColorHover` | unset, following `--button-color` |
| [x] | `--button-color-selected` | `Avonic.ActionSheet.ButtonColorSelected` | unset, following `--button-color` |
| [x] | `--button-color-disabled` | `Avonic.ActionSheet.ButtonColorDisabled` | unset, following `--button-color` |

## What Ionic does

From `action-sheet.js`, `animations/md.enter.js`, `md.leave.js`, `action-sheet.md.css`, `action-sheet.css` (the base part of the
md file), `utils/overlays.js`, and `normalize.css`'s `button { line-height: 1 }`.

- **Present**: the shared `present()` (blur, `ionActionSheetWillPresent`, `willPresent`, the animation, `ionActionSheetDidPresent`,
  `didPresent`); focus on the host (no `[role="dialog"][tabindex]` inside it: the host is the dialog). Measured: `host`.
- **Dismiss**: the shared one. `ionActionSheetWillDismiss` reaches the sheet's own listener (`dispatchCancelHandler`), which calls
  the first `cancel` button's handler when the role `isCancel` (`cancel`, `backdrop`), not awaited, its result ignored.
- **A button** (`buttonClick`): a `cancel` or `backdrop` role dismisses with the button's `data` and role at once (its handler then
  runs from the will-dismiss listener, once); otherwise the handler, awaited: `false` keeps the sheet open; then
  `dismiss(button.data, button.role)`.
- **Buttons** (`render`): the first `cancel` button goes last, in `.action-sheet-group-cancel`; any other cancel button isn't
  rendered; the rest, in order, in the first group after the title. `ion-icon` before the text; md's ripple in each; classes
  `action-sheet-button`, `ion-activatable` and `ion-focusable` (not when disabled), `action-sheet-<role>`, the `cssClass`. The
  cancel button gets neither `disabled` nor an `id`.
- **Radios** (`htmlAttributes.role === 'radio'`): the group is `role="radiogroup"`; the checked one is the first with
  `aria-checked === 'true'` when the buttons are set (`buttonsChanged`), and stays when new buttons have none; it's
  `.action-sheet-selected` and the one tab stop (`tabindex` 0), or the first radio when none is checked, the others -1. A click
  checks it, then `buttonClick`. Keys on a radio, while presented: ArrowDown and ArrowRight focus and check the next enabled radio,
  ArrowUp and ArrowLeft the previous, wrapping, without a dismiss or a handler; Space and Enter check it and `buttonClick`.
- **Keys**: Tab from the host goes to the first button, through each enabled one (or the radios' one stop) and the cancel button,
  and wraps at the sentinels; Shift+Tab wraps back to the cancel button; Enter and Space click (native buttons); Escape dismisses
  with `backdrop` when `backdropDismiss`. An arrow on a plain button does nothing.
- **Accessibility**: the host is `role="dialog"`, `aria-modal`, `aria-labelledby` the title (which holds the sub title, so both
  name it), else `htmlAttributes['aria-label']`; buttons are `<button type="button">`, named by their text or `aria-label`; radios
  are `role="radio"` with `aria-checked`; icons are `aria-hidden`.
- **md enter**: 400 ms `cubic-bezier(.36,.66,.04,1)`; the backdrop from 0.01 to `--backdrop-opacity`, `pointer-events: none`
  until it ends; the wrapper from `translateY(100%)` to `translateY(0%)`. **md leave**: 450 ms, the same curve, the backdrop to 0,
  the wrapper back to `translateY(100%)`. At rest the wrapper is `translate3d(0, 100%, 0)`, so it's only on screen while an
  animation holds it.
- **Look** (`action-sheet.md.css`): the wrapper is at the bottom, `--width` 100% up to `--max-width` 500, centred, as tall as its
  content up to `--max-height`; its container a column at the end, of the group (`flex-shrink: 2`, `overflow-y: auto`, so it
  shrinks and scrolls) and the cancel group (`flex-shrink: 0`), each with `--background`; the last group padded at the bottom by
  the safe area. The title 16 px, `--color`, padding 20 16 17, at least 60 high, its sub title 14 px with 16 above; each button
  16 px, `--button-color`, padding 12 16, at least 52 high, `line-height: 1`, its content centred in it (Chromium centres a
  `<button>`'s content); the icon 24 px with 32 after it, `--color`, which shrinks with the text when a long label wraps (flex);
  hover, focus, selected and activated states are an `::after` over the button in a colour at an opacity; disabled: opacity 0.4.
  The group's scroll bar is hidden on a touch screen (`any-pointer: coarse`).

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter | 400 ms; at 100 ms local time the wrapper is at 66.735 of 216 (`translateY`) and the backdrop 0.2242, as the curve gives; `pointer-events: none` on the backdrop until the end |
| M2 | Leave | 450 ms, the backdrop to 0, the wrapper to 216; then hidden |
| M3 | Events | `ionActionSheetWillPresent`, `willPresent`, `ionActionSheetDidPresent`, `didPresent`; the dismiss pair with `{data, role}` |
| M4 | Basic (header, Delete, Share, Cancel) | wrapper 0, 628, 390 x 216; group 164 (title 60, two buttons of 52), cancel group 792, 52; title text at 16, 648; labels at x 16, 16 from each button's top |
| M5 | Header, sub header and icons | wrapper 0, 496, 390 x 348; title 88 (sub title 16 + 16 at 535); icons 24 at x 16, centred (y + 14); labels at x 72; colours: title, sub title and icons `rgba(0,0,0,0.54)`, buttons `#262626`, 16 px, 400 |
| M6 | No header, three strings | wrapper 688, 156, one group: `"Cancel"` is a plain button |
| M7 | 30 options and Cancel | wrapper 0, 0, 390 x 844 (`max-height: 100%`); the group 792 high, scrolling 1620; the cancel group stays 52 |
| M8 | Tablet, 1024 x 768 | wrapper 262, 420, 500 x 348: 500 wide, centred; 30 options: 500 x 768, the group 716 |
| M9 | Radios (from a controller) | the group `role="radiogroup"`; Banana `aria-checked="true"`, `tabindex` 0, `.action-sheet-selected`, bold; the others -1; a disabled one at opacity 0.4 |
| M10 | Disabled | opacity 0.4, colour unchanged; a tap does nothing; the cancel button's `disabled: true` is ignored |
| M11 | A long header and a long label | the title 75 (two lines of 19); the button 56: two lines of 16 (`line-height: 1`), the icon shrunk to 14.78 wide (19.77 at 500), the label 32 after it |
| M12 | Focus at present, after | the host; then the opener again |
| M13 | Tab | host, Delete, Share, Play, Favorite, Cancel, Delete …; Shift+Tab from Play: Share, Delete, Cancel; a focused button's `::after` is `#262626` at 0.12 |
| M14 | Radios' keys | Tab: host, Banana (the checked one), Cancel, Banana; ArrowDown from Banana: Cherry focused and checked, no dismiss, no handler; ArrowDown: Apple (skips the disabled Durian, wraps); ArrowUp: Cherry; ArrowLeft: Banana; Space: its handler, dismissed with no data or role |
| M15 | Buttons | Share's handler returning false: still open; Delete: `{data: {action: "delete"}, role: "destructive"}`; a handler's promise of false: still open; Enter on Cancel: `cancel`, its handler once |
| M16 | Cancel, Escape, backdrop | Cancel: `{data, role: "cancel"}`, its handler once (from the will-dismiss listener); Escape and a backdrop tap: `backdrop`, the cancel handler once; `backdropDismiss` false: neither, and no handler |
| M17 | Attributes | the host's `aria-label`, `role="dialog"`, `aria-modal`, `aria-labelledby` the title; `cssClass` on the host; a button's `id`, `cssClass`, `aria-label`; the cancel button's class `action-sheet-cancel`, no id |

**An Ionic bug, not copied:** an action sheet in the page (not made by the controller) with radios loses its checked one at
present: `buttonsChanged` makes the checked button's id from the overlay index, and the present moves the element to `ion-app`,
which gives it a new index, so no rendered id matches and no radio is checked or a tab stop (measured: the sheet in `ref/radios.png`
shows none). The select's sheet, made by the controller, isn't moved, and works. Avonic keys the checked radio by the button's
`Id` or its place, so both work. Ionic's ids also count the cancel button when it's set (`indexOf` in all the buttons) but not when
they're rendered, so a cancel button before the radios would shift them; Avonic counts the rendered ones. Both are recorded in
`docs/infra/ionic-issues.md`.

## Design

`AvonicActionSheet : AvonicOverlay`, in `Controls/ActionSheet/`, a scoped overlay: `FocusTrapWrapper` is its wrapper, with two
focusable sentinels around it, as Ionic's `tabindex="0"` divs.

- **Template**: a `Panel` of the backdrop, the sentinels and an `ActionSheetPanel`, which places the wrapper as `.action-sheet-wrapper`
  is placed: at the bottom, `--width` within `--min-width` and `--max-width`, centred (`margin-inline: auto`), `--height` (auto: its
  content) within `--min-height` and `--max-height`, each a number or a CSS string, read by `AlertPanel.Length` (#39). The wrapper
  holds an `ActionSheetContainer`, the flex column at the end, whose first child (the group) shrinks when the two are taller than
  it may be, and whose second (the cancel group) doesn't. The group is a `Border` with a scroller of a `StackPanel`: the title,
  then the buttons; the cancel group a `Border` with the cancel button.
- **`ActionSheetNativeButton`**: the `<button>` Ionic renders for each `ActionSheetButton`, a type of its own with its own
  `ControlTheme`, as `/template/` can't reach what code makes: `NativeButton`, `Activatable`, `FocusVisible`, md's ripple, a
  highlight part for `::after`, an icon and a label in a `FlexRow` (so the icon shrinks as Chromium's does). Its state is the
  sheet's: `:role-<role>`, `:selected`; to automation a button, or a radio with its checked state.
- **Shared with the alert**: running a handler (`safeCall` and the awaiting of a `Task` or `ValueTask`, and reading its result) and
  reading `var(--backdrop-opacity)` move to `Overlays/OverlayHandler.cs`, a new internal file, which `AvonicAlert` then uses. No
  change to `AvonicOverlay` itself.
- **The data**: `ActionSheetButton` is a plain class. `Buttons` is a list the sheet follows: a new list, or a change to the one it
  has, re-renders after the change, as Stencil renders once for many, which the select needs when its options change while open.
- **No visual opinion in Components**: sizes, colours and state rules are in `Avonic.Themes`; the components hold the layout
  algorithms and the behaviour.

### Not done, with why

| Not here | Why |
|---|---|
| `translucent` | ios only |
| The ios button gesture (`createButtonActiveGesture`) | ios only |
| Rich options (`startContent`, `endContent`, `description`) | `ion-select`'s, internal to Ionic; with #45 |
| `.action-sheet-container`'s `max-height: calc(100dvh - safe areas)` | The wrapper's own `max-height` is the same here, as the host is the window |
| `margin-top: var(--ion-safe-area-top)` on the wrapper | It's placed by `bottom: 0` with `top: auto`, so the margin moves nothing in CSS either |
| Root `aria-hidden` while it's open | The overlay system's deferral (#65) |
| A controller (`actionSheetController.create`) | The overlay system's deferral; `new AvonicActionSheet { … }.Present()` is the same |
| `cssClass` as CSS | Style classes here, for an app's styles |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter: 400 ms `cubic-bezier(.36,.66,.04,1)`, the wrapper from `translateY(100%)`, the backdrop 0.01 to 0.32, frame by frame on the clock; the backdrop not hit-testable until the end | `md.enter.js`, M1 |
| B2 | md leave: 450 ms, the wrapper to `translateY(100%)`, the backdrop to 0; at rest the wrapper is `translateY(100%)`; `Avonic.ActionSheet.BackdropOpacity` beats `Avonic.BackdropOpacity` | `md.leave.js`, M2 |
| B3 | Events: `IonActionSheetWillPresent` before `WillPresent` and the rest, with data and role | M3 |
| B4 | Focus at present: the sheet; back to the opener after | M12 |
| B5 | A button: dismisses with its `Data` and `Role`; the handler runs first; `false` (or a task of it) keeps it open; a `cancel` or `backdrop` role dismisses without waiting on its handler | `buttonClick`, M15 |
| B6 | `ionActionSheetWillDismiss` with `cancel` or `backdrop` calls the first cancel button's handler once (Cancel, Escape, the backdrop), not when its role isn't a cancel one | `dispatchCancelHandler`, M16 |
| B7 | Buttons: the first cancel button last, in its own group; other cancel buttons not rendered; strings are plain buttons; icons; the cancel group only with a cancel button | `render`, M4 to M6 |
| B8 | `Disabled`: not clickable or focusable, opacity 0.4; ignored on the cancel button; `htmlAttributes.disabled` only on the cancel button | `render`, M10 |
| B9 | Radios: the checked one from `aria-checked`, selected and the one tab stop (or the first); a click checks it and runs `buttonClick`; arrows move, check and focus, skipping disabled ones, wrapping, without a dismiss; Space and Enter check and click; an arrow on a plain button does nothing | `buttonsChanged`, `onKeydown`, M9, M14 |
| B10 | Tab from the sheet reaches each button and wraps at the sentinels; Shift+Tab wraps to the last | overlay trap, M13 |
| B11 | `BackdropDismiss` false: no tap, Escape or back; true: each dismisses with `backdrop` | M16 |
| B12 | Layout: the measured boxes of M4 to M8 and M11 on the phone and the tablet; the group shrinks and scrolls when tall; the cancel group doesn't; the safe areas | `action-sheet.md.css`, M4 to M11 |
| B13 | Title: shown when `Header` isn't null; the sub title only with a header, when not empty | render |
| B14 | States: focused, hover, selected, activated, disabled colours and opacities through the keys; `:role-<role>`; `CssClass` on the sheet and a button | `action-sheet.md.css`, `buttonClass` |
| B15 | Automation: the sheet is a dialog named by its header and sub header, else `aria-label`; buttons named by text or `aria-label`, `Id`s as `AutomationId`; radios toggleable with their state | render, M17 |
| B16 | `IsOpen` and `Trigger` present it, through the base; `Buttons` replaced or changed while open re-render | watchers |
| B17 | The group scrolls with a touch drag, and a wheel, on a long list | M7 |

## Extensions

None. `Present(TopLevel)` is the overlay's.

## Dependencies

`AvonicOverlay`, `AvonicAnimation`, `AvonicBackdrop` (#5), `AvonicIcon` (#3), `AvonicRippleEffect`, `Activatable`, `FocusVisible`,
`NativeButton`, `FlexRow`, `AlertPanel.Length` (#39): all merged.
