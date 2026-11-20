# ion-loading — Plan

`AvonicLoading`: Ionic's loading indicator, md, on the overlay system ([#5](../../infra/overlays/README.md)) with the base's inline
overlays, `IsOpen` and `Trigger` ([#42](../ion-modal/README.md)), and `AvonicSpinner` ([#15](../ion-spinner/README.md)). It's the
simplest of the scoped overlays: a backdrop, and a wrapper of a spinner and a message, centred in the window.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch,
Roboto from `tools/fonts/roboto/`), each loading made from script, as the controller makes one, and presented. The raw output is
[`ref/measure.json`](ref/measure.json) (boxes, colours, attributes, the enter and leave frames paused at times, and the behaviour
scenarios with real taps and keys), and a frame of each case in [`ref/`](ref/), the spinners paused at 0. The script isn't in the repo,
as the swarm brief asks; its cases are the `M` rows below.

## Fidelity checklist

`node tools/ionic-api.js ion-loading`.

### Props (15)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `false` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [x] | `cssClass` | `CssClass` | `string?` | | Style classes on the loading, space-separated (`AlertClasses`). Ionic's array form is the same string |
| [x] | `duration` | `Duration` | `double` | `0` | Ms after the present ends, plus Ionic's 10, before it dismisses itself, with no role; 0 never |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `htmlAttributes` | `HtmlAttributes` | `IReadOnlyDictionary<string, object?>?` | | Spread on the host: `id` is its `AutomationId`; `aria-label` its name when there's no message (`aria-labelledby` beats it). Others have nothing to do in Avalonia, and are ignored |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | `AvonicOverlay`'s |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | `true` | The overlay's |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `message` | `Message` | `object?` | | A string is plain text, Ionic's default; anything else is content, the app's own controls in place of the HTML Ionic can be configured to take (`innerHTMLTemplatesEnabled`), as `AvonicAlert.Message` does. Null renders no `.loading-content`; `""` renders an empty one (M5) |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `showBackdrop` | `ShowBackdrop` | `bool` | `true` | The backdrop's `Visible` |
| [x] | `spinner` | `Spinner` | `SpinnerName?` | | Unset at the first load: `AvonicConfig.LoadingSpinner`, else `AvonicConfig.Spinner`, else `Crescent` (Ionic's `config.get('loadingSpinner', config.get('spinner', 'crescent'))`), given only if it isn't set (`ConfigOnLoad.IfUnset`); `null` is no spinner (M14) |
| [ ] | `translucent` | none | | `false` | Not applicable: it only adds `.loading-translucent`, which only `loading.ios.css` reads; md draws nothing different (M10), so it would be a dead prop (rule 3). The alert and the action sheet omit theirs for the same reason |
| [x] | `trigger` | `Trigger` | `string?` | | `AvonicOverlay`'s |

`overlayIndex`, `delegate` and `hasController` are `@internal`.

### Events (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionLoadingWillPresent` | `IonLoadingWillPresent` | Through `ControlWillPresentEvent`, before `WillPresent` |
| [x] | `ionLoadingDidPresent` | `IonLoadingDidPresent` | |
| [x] | `ionLoadingWillDismiss` | `IonLoadingWillDismiss` | `OverlayEventArgs` |
| [x] | `ionLoadingDidDismiss` | `IonLoadingDidDismiss` | `OverlayEventArgs` |
| [x] | `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | the overlay's | |

### Methods (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `present()` | `Present()` | And `Present(TopLevel)` |
| [x] | `dismiss(data?, role?)` | `Dismiss(data, role)` | `false` before a present (M3) |
| [x] | `onDidDismiss()`, `onWillDismiss()` | the overlay's | `OverlayEventDetail(Data, Role)` |

### Slots, parts: none. The template's parts are named after Ionic's classes: `PART_Backdrop`, `PART_SentinelStart`, `PART_Panel`, `PART_Wrapper` (`.loading-wrapper`), `PART_SpinnerBox` (`.loading-spinner`), `PART_Spinner` (its `ion-spinner`), `PART_Content` (`.loading-content`), `PART_SentinelEnd`.

### CSS custom properties (9)

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--backdrop-opacity` | `Avonic.Loading.BackdropOpacity` | unset, following `Avonic.BackdropOpacity` (0.32); read by the animations |
| [x] | `--background` | `Avonic.Loading.Background` | unset, following `Avonic.BackgroundColorStep50Brush` (`#f2f2f2`) |
| [x] | `--width` | `Avonic.Loading.Width` | `auto` |
| [x] | `--min-width` | `Avonic.Loading.MinWidth` | `auto` |
| [x] | `--max-width` | `Avonic.Loading.MaxWidth` | 280 |
| [x] | `--height` | `Avonic.Loading.Height` | `auto` |
| [x] | `--min-height` | `Avonic.Loading.MinHeight` | `auto` |
| [x] | `--max-height` | `Avonic.Loading.MaxHeight` | `90%` |
| [x] | `--spinner-color` | `Avonic.Loading.SpinnerColor` | unset, following `Avonic.Color.PrimaryBrush` (`#0054e9`) |

A size takes a number (px) or a CSS string, read by `AlertPanel.Length`, as the alert's (#39).

## What Ionic does

From `loading.js`, `animations/md.enter.js`, `md.leave.js`, `loading.md.css` (with the base part of it) and `utils/overlays.js`.

- **Present**: the lock; the shared `present()` (blur, `ionLoadingWillPresent`, `willPresent`, the animation, `ionLoadingDidPresent`,
  `didPresent`, then focus on the host with `keyboardClose`: no `[role="dialog"][tabindex]` inside it, the host is the dialog); then,
  with `duration > 0`, `setTimeout(() => this.dismiss(), duration + 10)`: no data, no role. A second `present()` while presented sets
  another timer, which the first never clears (see below).
- **Dismiss**: the lock; the timer cleared; the shared `dismiss()`. Nothing else: no buttons, no cancel handler.
- **Backdrop**: `ion-backdrop` with `visible={showBackdrop}` and `tappable={backdropDismiss}`; its tap dismisses with `backdrop`. With
  `showBackdrop` false it's transparent (`backdrop-hide`) and still covers the page, so the page under can't be tapped, and still
  dismisses with `backdropDismiss`. A tap on the wrapper does nothing. Escape and the back button dismiss with `backdrop` only with
  `backdropDismiss`.
- **Render**: the host `role="dialog"`, `aria-modal="true"`, `aria-labelledby` the message's id when `message !== undefined`,
  `tabindex="-1"`, then `htmlAttributes` (so its `id` replaces the overlay id, and `aria-label` is set but loses to
  `aria-labelledby`), `z-index: 40000 + overlayIndex`, the `cssClass` classes. Inside: the backdrop, a `tabindex="0"` sentinel, the
  wrapper (`.loading-wrapper.ion-overlay-wrapper`), another sentinel. In the wrapper, when `spinner` is truthy, `.loading-spinner`
  with an `ion-spinner` of that name, `aria-hidden`; when `message !== undefined`, `.loading-content` with it as text.
- **`spinner`'s default** (`componentWillLoad`): only when undefined, `config.get('loadingSpinner', config.get('spinner', 'crescent'))`
  in md; `null` stays null, no spinner.
- **md enter**: 200 ms `ease-in-out`; the backdrop from 0.01 to `var(--backdrop-opacity)`, `pointer-events: none` until it ends; the
  wrapper's keyframes from opacity 0.01 and `scale(1.1)` to 1 and `scale(1)`. **md leave**: 200 ms `ease-in-out`; the backdrop to 0;
  the wrapper from 0.99 and `scale(1)` to 0 and `scale(0.9)`. The wrapper rests at `opacity: 0`, so it's only seen while an animation
  holds it.
- **Look** (`loading.md.css`): the host covers the window (`position: fixed`, inset 0), a flex box centring the wrapper; font
  `--ion-font-family`, 14 px, `color: #262626` (text step 150); `user-select: none`, `touch-action: none`, `contain: strict`. The
  wrapper is a flex row, `align-items` and `justify-content` center (inherited), `--width` by `--height` within the minimums and
  maximums, `--background`, `border-radius: 2px`, padding 24, `box-shadow: 0 16px 20px rgba(0, 0, 0, 0.4)`, `z-index: 10`. The
  spinner is `--spinner-color`; the content after a spinner has `margin-inline-start: 16px`.
- **Focus trap**: scoped. Tab from the host reaches a sentinel, outside the wrapper, which wraps to the first focusable element in
  the wrapper, and there is none, so the host again (M13). Focus outside the loading is left alone.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter | 200 ms `ease-in-out`, `fill: both`; wrapper opacity 0.01, 0.0408, 0.1379, 0.505, 0.8721, 1 and scale 1.1, 1.0969, 1.0871, 1.05, 1.0129, 1 at 0, 25, 50, 100, 150, 200 ms; backdrop 0.01, 0.0196, 0.0500, 0.165, 0.2800, 0.32; the backdrop's `pointer-events: none` until the end, `auto` after |
| M2 | Leave | 200 ms `ease-in-out`; wrapper 0.99, 0.8621, 0.495, 0.1279, 0 and scale 1, 0.9871, 0.95, 0.9129, 0.9 at 0, 50, 100, 150, 200; backdrop 0.32, 0.2787, 0.16, 0.0413, 0; the host `pointer-events: none` throughout |
| M3 | Events | `ionLoadingWillPresent`, `willPresent`, then 229 ms later `ionLoadingDidPresent`, `didPresent`; the dismiss pair with `{data, role}`; `dismiss()` before a present returns `false` |
| M4 | Basic (`Loading...`, crescent) | wrapper 118.44, 382.5, 153.11 x 79; `.loading-spinner` 142.44, 406.5, 28 x 31 (the inline-block spinner on a 14 px line: 3 of descent below it), the spinner 28 x 28 at its top; `.loading-content` 186.44, 414, 61.11 x 16 (`line-height: normal`, 16 at 14 px Roboto); colours: wrapper `#f2f2f2`, text `#262626`, spinner `#0054e9`, backdrop black at 0.32; radius 2; shadow `0 16px 20px rgba(0,0,0,0.4)` |
| M5 | No spinner; no message; `""` | 140.44, 390, 109.11 x 64 (content at 164.44); 157, 382.5, 76 x 79; 149, 382.5, 92 x 79: an empty `.loading-content`, 0 x 0, still 16 after the spinner |
| M6 | A long message | 55, 374, 280 x 96 (`--max-width`); the content 188 wide at 123, 398, three lines of 16; the spinner box centred (406.5) |
| M7 | `--width: 200px; --min-height: 150px` | 95, 347, 200 x 150; the spinner and the text where they are in M4: the row centred both ways |
| M8 | Every spinner name | the same boxes as M4 for all nine: `ion-spinner` is 28 x 28 in md whatever the name |
| M9 | `showBackdrop: false` | the backdrop `backdrop-hide` (transparent); a tap on a button under it: not clicked |
| M10 | `translucent` | `loading-translucent` on the host; nothing drawn differently |
| M11 | `cssClass` with `--background: #222; --spinner-color: #fff; color: #fff` | the wrapper `#222`, the text and the spinner white |
| M12 | RTL | the spinner at 219.56, the content at 142.44: mirrored |
| M13 | Focus and Tab | the host at present; Tab four times: still the host; Shift+Tab: the input before it in the page (outside the overlay, so left alone). With an input focused before: blurred, the host focused, the input focused again after the dismiss; `keyboardClose: false`: blurred, and nothing focused |
| M14 | `spinner` | unset: `crescent`; `null`: none rendered |
| M15 | Backdrop and keys | default: a backdrop tap and Escape do nothing; `backdropDismiss`: each dismisses with `backdrop`; a tap on the wrapper doesn't; `showBackdrop: false` with `backdropDismiss`: a tap where the backdrop is dismisses |
| M16 | `duration: 500` | `willDismiss` 516 ms after `didPresent` (500 + 10 and a frame), no role; dismissed by hand first, the timer is gone (present again: still open) |
| M17 | Attributes | `role="dialog"`, `aria-modal="true"`, `aria-labelledby="loading-2-msg"`, `tabindex="-1"`, `z-index: 40002`; the spinner `role="progressbar"`, `aria-hidden="true"`; `htmlAttributes: {id, aria-label}`: `id="my-loading"`, `aria-label="Busy"` beside `aria-labelledby`; no message: no `aria-labelledby` |
| M18 | Changes while presented | a longer message re-renders (280 x 80, two lines); a new `spinner` name re-renders |
| M19 | `isOpen`, `trigger` | `isOpen` true presents, false dismisses with no role, and isn't set back; a tap on the trigger presents |

**An Ionic bug, not copied:** `present()` sets `this.durationTimeout = setTimeout(…)` after the shared `present()`, which returns at
once when the loading is already presented. A second `present()` while it's up starts a second timer, and the first is lost: `dismiss()`
clears only the last, so the first still fires, and can dismiss a later presentation of the same loading `duration + 10` ms after the
first present. Ionic means one timer per presentation. Avonic starts it only when a present presents, so a second `Present()` does
nothing (ADR 0005, recorded in `docs/infra/ionic-issues.md`, with a test).

## Design

`AvonicLoading : AvonicOverlay`, in `Controls/Loading/`, a scoped overlay: `FocusTrapWrapper` is its wrapper, with two focusable
sentinels around it, as Ionic's `tabindex="0"` divs. `ZIndexBase` 40000.

- **Template**: a `Panel` of the backdrop (`Visible` from `ShowBackdrop`, `Tappable` from `BackdropDismiss`), the sentinels and an
  `AlertPanel` (#39), which places the wrapper as the host's flex box does: centred, `--width` (auto: fit-content) within the
  minimum and maximum, the same as the alert's. Reused as it is, as the swarm brief asks. The wrapper is a `Border` (background,
  radius, padding, shadow) holding a `FlexRow` centred both ways (`justify-content: center`), of the spinner's box and the content,
  each `VerticalAlignment="Center"` (`align-items: center`), so a long message shrinks and wraps, as a flex item does.
- **The 16 between them** is the row's `Spacing`, the gap between two shown children: it's there only when both are, which is
  `.loading-spinner + .loading-content` for a row of these two, with no pseudoclass to work out.
- **`.loading-spinner`**: a `Border` with 3 of padding below the 28 px spinner: the inline-block spinner sits on the baseline of a
  14 px line, whose descent Chromium rounds to 3 (M4). A literal in the theme, so it can be restyled.
- **`.loading-content`**: a `Border` shown when `Message` isn't null, holding a `ContentPresenter` shown when it isn't `""`, so an
  empty message is a 0 x 0 box after the gap, as the empty `div` is, where an empty `TextBlock` is a line high. `line-height: normal`
  is 16 at 14 px Roboto in Chromium (its ascent and descent each rounded), where Avalonia's is 16.41: the theme sets 16, as the
  alert sets its own measured line heights.
- **`AvonicConfig.LoadingSpinner`**, a new key beside `InfiniteLoadingSpinner` and `RefreshingSpinner`, read at the first attach
  through `ConfigOnLoad.IfUnset`, as `AvonicInfiniteScrollContent` reads its own.
- **Automation**: the host's peer is a `Window`, Avalonia's nearest type to a dialog, as the action sheet's, named by a string
  message (`aria-labelledby`), else `htmlAttributes['aria-label']`, else its own name; its `AutomationId` `htmlAttributes.id`. The
  spinner is `AccessibilityView.Raw` (`aria-hidden`).
- **No visual opinion in Components**: sizes, colours and the shadow are in `Avonic.Themes`; the component holds the behaviour and
  the structure.

### Not done, with why

| Not here | Why |
|---|---|
| `translucent` | ios only (M10) |
| `user-select: none` | Avalonia's `TextBlock` isn't selectable |
| `touch-action: none` | The host covers the window and takes every press, so nothing under it scrolls or is tapped; a gesture on it has nothing to do |
| `contain: strict` | A hint to the browser |
| Root `aria-hidden` while it's open | The overlay system's deferral (#65) |
| A controller (`loadingController.create`) | The overlay system's deferral; `new AvonicLoading { … }.Present()` is the same |
| `cssClass` as CSS | Style classes here, for an app's styles |
| `-webkit-font-smoothing` | A hint to the browser |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter: 200 ms `ease-in-out`, the wrapper's opacity 0.01 to 1 and scale 1.1 to 1, the backdrop 0.01 to 0.32, frame by frame on the clock, at M1's values; the backdrop not hit-testable until the end | `md.enter.js`, M1 |
| B2 | md leave: 200 ms, the wrapper 0.99 to 0 and scale 1 to 0.9, the backdrop to 0, at M2's values; the wrapper rests at opacity 0; `Avonic.Loading.BackdropOpacity` beats `Avonic.BackdropOpacity` | `md.leave.js`, M2 |
| B3 | Events: `IonLoadingWillPresent` before `WillPresent` and the rest, with data and role; `Dismiss()` before a present is false | M3 |
| B4 | Focus: the loading at present; back to what had it after; `KeyboardClose` false: blurred, not focused; Tab stays on the loading | M13 |
| B5 | `BackdropDismiss` false (the default): no tap, Escape or back; true: each dismisses with `backdrop`; a tap on the wrapper never | M15 |
| B6 | `ShowBackdrop` false: the backdrop transparent, still covering: the page under isn't clicked, and with `BackdropDismiss` a tap dismisses | M9, M15 |
| B7 | `Duration`: dismisses itself `Duration + 10` ms after the present ends, with no data or role; 0 never; a dismiss first stops it; a second `Present()` adds no timer (the bug above) | `present`, `dismiss`, M16 |
| B8 | `Spinner`: unset at the first attach it becomes `AvonicConfig.LoadingSpinner`, else `AvonicConfig.Spinner`, else `Crescent`; a value set, or `null`, stays; null draws none; the spinner is the name given, coloured by `--spinner-color`, hidden from automation | `componentWillLoad`, M14, M17 |
| B9 | Message: null: no content and no gap; `""`: an empty content after the 16 gap; a string as text, wrapping within the wrapper; content other than a string shown as it is | `render`, M5, M6 |
| B10 | Layout: the measured boxes of M4 to M7 and M12; the sizes from the keys, as numbers or CSS strings | `loading.md.css`, M4 to M8, M12 |
| B11 | Look: the colours, the radius and the shadow from the keys; an app's keys (`cssClass`) change them | M4, M11 |
| B12 | Automation: the loading is a dialog named by its message, else `aria-label`; `id` as its `AutomationId` | render, M17 |
| B13 | `CssClass` on the loading, taken off when it changes | `getClassMap` |
| B14 | `IsOpen` and `Trigger` present it, through the base; `Message` and `Spinner` changed while it's open re-render | M18, M19 |
| B15 | Stacking: `ZIndex` 40000 plus the overlay index: over a modal or an alert, under a toast | `render`, M17 |

## Extensions

None. `Present(TopLevel)` is the overlay's.

## Dependencies

`AvonicOverlay`, `AvonicAnimation`, `AvonicBackdrop` (#5), `AvonicSpinner` and `ConfigSpinner` (#15), `AlertPanel` and
`AlertClasses` (#39), `FlexRow`, `ConfigOnLoad`: all merged.
