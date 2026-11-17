# ion-alert — Plan

`AvonicAlert`: Ionic's alert, md, built on the overlay system ([#5](../../infra/overlays/README.md)) and the base's inline
overlays, `IsOpen` and `Trigger` ([#42](../ion-modal/README.md)). Its buttons and inputs are plain data, `AlertButton` and
`AlertInput`, as Ionic's are, so that `ion-select` (#45) can build them in code and present the alert, as its `openAlert()` builds
`AlertInput[]` and `AlertButton[]` and calls `alertController.create`.

Measured in real Ionic 9.0.11 in headless Chromium, md, in `ion-app`, on `render.js`'s phone (390 x 844 at 2x, mobile and touch)
and at 1024 x 768; the raw output is [`ref/layout.json`](ref/layout.json) and [`ref/behaviour.json`](ref/behaviour.json), from the
scripts the comparison names.

## Fidelity checklist

`node tools/ionic-api.js ion-alert`.

### Props (16)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `animated` | `Animated` | `bool` | `true` | The overlay's |
| [x] | `backdropDismiss` | `BackdropDismiss` | `bool` | `true` | The backdrop's `Tappable`; Escape and back (`BackdropDismissEnabled`) |
| [x] | `buttons` | `Buttons` | `IList<AlertButton>` | empty | `AlertButton` has an implicit conversion from `string`, which is Ionic's string form: `text`, and the `cancel` role when it says `cancel` in any case |
| [x] | `cssClass` | `CssClass` | `string?` | | Style classes on the alert, space-separated, as Ionic puts them on the host. Ionic's array form is the same string |
| [x] | `enterAnimation` | `EnterAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `header` | `Header` | `string?` | | |
| [x] | `htmlAttributes` | `HtmlAttributes` | `IReadOnlyDictionary<string, object?>?` | | Spread on the wrapper. What maps: `aria-label` (the wrapper's automation name when there's no header or sub header, as `aria-labelledby` beats it in Chromium's name), `id` (its `AutomationId`). Anything else is an HTML attribute with nothing to do in Avalonia, and is ignored |
| [x] | `inputs` | `Inputs` | `IList<AlertInput>` | empty | Text-like types, `textarea`, `radio`, `checkbox`; see below |
| [x] | `isOpen` | `IsOpen` | `bool` | `false` | `AvonicOverlay`'s |
| [x] | `keyboardClose` | `KeyboardClose` | `bool` | `true` | The overlay's |
| [x] | `leaveAnimation` | `LeaveAnimation` | `AnimationBuilder?` | | The overlay's |
| [x] | `message` | `Message` | `object?` | | A string is shown as plain text, Ionic's default (`innerHTMLTemplatesEnabled` is off by default, and then `message` is a text node). Any other value is content, through a presenter: the app's own controls in place of the HTML Ionic can be configured to take, so there's nothing to sanitise |
| [ ] | `mode` | none | | | Not exposed (ADR 0001) |
| [x] | `subHeader` | `SubHeader` | `string?` | | |
| [ ] | `translucent` | none | | `false` | Not applicable: only `alert.ios.css` reads `.alert-translucent`; md draws nothing different |
| [x] | `trigger` | `Trigger` | `string?` | | `AvonicOverlay`'s |

`overlayIndex`, `delegate` and `hasController` are `@internal`.

### `AlertButton` (Ionic's interface)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `text` | `Text` | Upper-cased by md's `text-transform`, as the theme's `TextTransform` on the button |
| [x] | `role` | `Role` | `cancel` (and `backdrop`, `isCancel`) dismisses with the values and no handler; the role is the dismiss's; `alert-button-role-<role>` is the pseudoclass `:role-<role>` on the button |
| [x] | `cssClass` | `CssClass` | Style classes on the button |
| [x] | `id` | `Id` | The button's `AutomationId`, as an `id` is what a test finds it by |
| [x] | `htmlAttributes` | `HtmlAttributes` | Spread on the `<button>`: `aria-label` (its automation name) and `disabled` (it's disabled) map; others are ignored |
| [x] | `handler` | `Handler` | `Func<object?, object?>`, given the values: `false` keeps the alert open; a dictionary is merged into the dismiss's data; a `Task` is awaited first, and its result read the same way |

### `AlertInput` (Ionic's interface)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `type` | `Type` | `AlertInputType?`: `Text` (null), `Number`, `Password`, `Email`, `Search`, `Tel`, `Url`, `Date`, `Time`, `Week`, `Month`, `DatetimeLocal`, `Textarea`, `Radio`, `Checkbox`. The keyboard follows the type, through `NativeTextField.ContentType`; `Password` masks |
| [x] | `name` | `Name` | The key of its value; its index when null |
| [x] | `placeholder` | `Placeholder` | |
| [x] | `value` | `Value` | `object?`; a text field's is its text after an edit |
| [x] | `label` | `Label` | A radio's or checkbox's text |
| [x] | `checked` | `Checked` | |
| [x] | `disabled` | `Disabled` | |
| [x] | `id` | `Id` | The control's `AutomationId`; `alert-input-<overlayIndex>-<index>` when null |
| [x] | `handler` | `Handler` | `Action<AlertInput>`, after a radio or checkbox changes, given Ionic's processed copy |
| [x] | `cssClass` | `CssClass` | Style classes on the input's control |
| [x] | `attributes` | `Attributes` | Spread on a text field: `disabled`, `class`, `maxlength`, `readonly`, `aria-label`, `inputmode` and `autocapitalize` map; others are ignored |
| [ ] | `min`, `max` | none | Not applicable: a browser's step arrows and validity on a number or date `<input>`, which Chromium on a phone doesn't show, and the alert never reads; an Avalonia `TextBox` has neither |
| [ ] | `tabindex` | none | Not applicable: `inputsChanged` overwrites it with its own (0, or -1 for a radio that isn't the focusable one) |

The select's rich options (`startContent`, `endContent`, `description`, `labelPlacement`, `justify`: `SelectAlertInput`, not the
public interface) are for #45; md's defaults (`end`, `start`) are what's drawn here.

### Events (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionAlertWillPresent` | `IonAlertWillPresent` | Through `ControlWillPresentEvent`, before `WillPresent` |
| [x] | `ionAlertDidPresent` | `IonAlertDidPresent` | |
| [x] | `ionAlertWillDismiss` | `IonAlertWillDismiss` | `OverlayEventArgs` |
| [x] | `ionAlertDidDismiss` | `IonAlertDidDismiss` | `OverlayEventArgs` |
| [x] | `willPresent`, `didPresent`, `willDismiss`, `didDismiss` | the overlay's | |

### Methods (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `present()` | `Present()` | And `Present(TopLevel)` |
| [x] | `dismiss(data?, role?)` | `Dismiss(data, role)` | |
| [x] | `onDidDismiss()`, `onWillDismiss()` | the overlay's | |

### Slots, parts: none. The template's parts are named after Ionic's classes: `PART_Backdrop`, `PART_Wrapper` (`.alert-wrapper`), `PART_Head`, `PART_Title`, `PART_SubTitle`, `PART_Message`, `PART_InputGroup`, `PART_ButtonGroup`.

### CSS custom properties (8)

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--backdrop-opacity` | `Avonic.Alert.BackdropOpacity` | unset, falling back to `Avonic.BackdropOpacity` (0.32); read by the animations |
| [x] | `--background` | `Avonic.Alert.Background` | unset, falling back to `Avonic.OverlayBackgroundColorBrush` |
| [x] | `--width` | `Avonic.Alert.Width` | `auto` |
| [x] | `--min-width` | `Avonic.Alert.MinWidth` | 250 |
| [x] | `--max-width` | `Avonic.Alert.MaxWidth` | unset: `Avonic.Alert.Md.MaxWidth` 280; `Avonic.Alert.Md.MaxWidthTablet` 560 at `:breakpoint-md` (`min(100vw - 96px, 560px)`, which is 560 from 768 up) |
| [x] | `--height` | `Avonic.Alert.Height` | `auto` |
| [x] | `--min-height` | `Avonic.Alert.MinHeight` | `auto` |
| [x] | `--max-height` | `Avonic.Alert.MaxHeight` | unset: `Avonic.Alert.Md.MaxHeight` `90%`; at `:breakpoint-md`, `min(100vh - 96px, 560px)`: the window's height less `Avonic.Alert.Md.TabletMargin` 96, at most `Avonic.Alert.Md.MaxHeightTablet` 560 |

## What Ionic does

From `alert.js`, `animations/md.enter.js`, `md.leave.js`, `alert.md.css`, `utils/overlays.js`, `utils/overlay-control-label.js`.

- **Present**: under the lock; the shared `present()` (blur, `ionAlertWillPresent`, `willPresent`, the animation, `ionAlertDidPresent`,
  `didPresent`, focus on the host, as no wrapper has `role="dialog"`); then, still under the lock, the alert's own focus: its one
  button when it has one button and no inputs, else the wrapper (`tabindex="0"`). Not behind `keyboardClose`.
- **Dismiss**: the shared one; `ionAlertWillDismiss` reaches the alert's own listener, which calls the `cancel` button's handler,
  with no argument, when the role `isCancel` (`cancel` or `backdrop`), its result ignored.
- **A button** (`buttonClick`): the values (`getValues`); a `cancel` or `backdrop` role dismisses with `{ values }` and the role at
  once (its handler runs from the will-dismiss listener, above); otherwise the handler, awaited, with the values: `false` keeps it
  open, an object is spread into the data, then `dismiss({ values, ...returned }, role)`.
- **Values**: none with no inputs; a radio alert's checked value; a checkbox alert's checked values, in order; a text alert's object
  of `name` (or index) to value (`''` when empty).
- **Inputs** (`inputsChanged`): copied (the app's objects aren't changed); a type of `text` by default; their types can't mix (a
  warning). Radios: only the first enabled checked one, else the first enabled one, is a tab stop (`tabindex` 0, the others -1);
  `rbClick` checks one, unchecks the rest, moves the tab stop and `aria-activedescendant`, calls its handler. Checkboxes:
  `cbClick` toggles and calls its handler. A text field writes its value on each input.
- **Keys** (`onKeydown` on the document): Shift+Tab on the wrapper focuses the last button. In a radio alert, on a radio,
  ArrowDown and ArrowRight check and focus the next enabled radio, ArrowUp and ArrowLeft the previous, wrapping. Enter on a checkbox
  is prevented, so only Space toggles it. Enter and Space on a radio or a button click it (a native `<button>`). **Enter in a text
  field does nothing** (no form: measured, B13).
- **Buttons** (`renderAlertButtons`): `.alert-button-group-vertical` (a column) with more than two buttons, or when they wrap
  (`checkButtonGroupWrap`, after a frame, from a `ResizeObserver`), and then it stays until the buttons change.
- **Head**: `h2.alert-title` for the header; the sub header is an `h3` under a header, else an `h2`; the head is there, padded,
  even when both are empty.
- **Accessibility**: the wrapper is `role="alertdialog"` with buttons or inputs, else `alert`; `aria-modal`; `aria-labelledby` the
  header and sub header; `aria-describedby` the message; the radio group `role="radiogroup"`; each option a `role="radio"` or
  `checkbox` `<button>` with `aria-checked`.
- **Look** (`alert.md.css`): see the theme; the measured boxes are below.
- **md enter**: 150 ms `ease-in-out`; the backdrop from 0.01 to `--backdrop-opacity`, `pointer-events: none` until it ends; the
  wrapper from opacity 0.01 and `scale(0.9)` to 1 and `scale(1)`. **md leave**: 150 ms `ease-in-out`, the backdrop to 0, the
  wrapper from 0.99 to 0.

## Measured

| | Case | Result |
|---|---|---|
| M1 | Enter | First frame 0.01/0.01/scale 0.9; then 0.0175/0.0340/0.9024 at 15 ms, 0.0819/0.2395/0.9232 at 49, 0.1944/0.5988/0.9595 at 83 … 0.32/1/1 at 148; `pointer-events` back after |
| M2 | Leave | 0.3121/0.9656 at 17 ms, 0.1906/0.5896 at 68 … 0.0078/0.0242 at 134; `pointer-events: none` throughout |
| M3 | Events | `ionAlertWillPresent`, `willPresent`, `ionAlertDidPresent`, `didPresent`; then the dismiss pair with `{data, role}`; focus back on the opener after |
| M4 | Basic box | Header, sub header, message, Cancel and OK: wrapper 250 x 178.19 centred (70, 332.91); head 83.19 (padding 20 24 15; title 24 high, sub 19.19 with 5 above, an `h3`); message 39 (19 + 20 below, none above); button group 56 (padding 8); buttons 72.97 and 38.5 wide, 40 high, 8 apart, right-aligned, 8 from the edge |
| M5 | Width | 250 for short content; 280 for a long label; 263.38 for two long buttons once they've wrapped into a column (the column's own width); 560 on a tablet with a long message |
| M6 | Heights | No header or sub header: the head is 35. No message: the message is 0. Long message: 266, scrolling. 20 radios: the group 266 with its two borders, 264 inside; on a tablet the wrapper's 560 cap shrinks the group to 445 |
| M7 | Buttons | Three: a column, each 226 wide (8 in, 8 at the end), right-aligned text; two that wrap: a column too |
| M8 | Text inputs | Group padding 20 24; each input 37 high (10 + 16 + 10 + its 1 bottom border), 5 above and below; a textarea 53 (two lines) and 66 with its wrapper, its 3 px of baseline gap |
| M9 | Radios | Rows 48; the icon 20 at x 24, centred; the label's text at x 76, padding 13 above and below, `#262626` 16 px; checked: the icon's border primary, the 10 px dot; the group's borders `#d9d9d9` |
| M10 | Checkboxes | Rows 48 (83 for a three-line label); the icon 16 at x 24, radius 2; checked: primary fill and a white tick 6 x 10, rotated 45°, at 3, 0; the label's text at x 72 |
| M11 | Focus at present | The wrapper; the one button when it's the only button and there are no inputs |
| M12 | Tab | From the wrapper: the checked radio (only it is a stop), then the buttons; past the last, the first again; Shift+Tab on the wrapper: the last button |
| M13 | Keys | ArrowDown skips a disabled radio and wraps; ArrowUp wraps; Enter on a checkbox doesn't toggle, Space does; Enter on a radio checks it; **Enter in a text input doesn't dismiss** |
| M14 | Values | Radio: `"red"`; checkboxes: `["a","b"]`; text: `{"1":"pre","first":"Ann"}`, with a handler's `{extra:1}` beside it: `{values, extra: 1}` |
| M15 | Cancel | A cancel button: dismissed with `{values}` and `cancel`, its handler called once with `undefined` (returning false changes nothing); Escape: dismissed with `backdrop`, the cancel handler called too |
| M16 | Backdrop | `backdropDismiss` false: a tap and Escape do nothing; true: a tap dismisses with `backdrop` |
| M17 | Roles | A string `"cancel"` is the cancel role; a `destructive` button's role and classes reach the dismiss and the button |

## Design

`AvonicAlert : AvonicOverlay`, in `Controls/Alert/`, a scoped overlay: `FocusTrapWrapper` is its wrapper, with two focusable
sentinels around it in the template, as Ionic's `tabindex="0"` divs.

- **Template**: a `Panel` of the backdrop, the sentinels and an `AlertPanel`, which lays out the wrapper as the host's flex box
  does: `--width` (auto: CSS's fit-content, the wrapper's max-content up to the room, measured unwrapped first, which is how a
  long label makes it 280) within `--min-width` and `--max-width`, and `--height` within its own two, centred. The wrapper,
  `AlertWrapper : Border` (focusable, a tab stop, `role="alertdialog"` to automation), holds an `AlertColumn`: the head, the
  message's scroller, the input group's scroller and the button group, in a column whose scrollers shrink, in proportion to their
  height, when it's taller than the wrapper may be, as flex items with `overflow: auto` (min-height 0) do.
- **Elements Ionic renders, one per input or button**: types of their own with their own `ControlTheme`s, as `/template/` can't reach
  what code makes: `AlertActionButton` (`.alert-button`, a native button, with a ripple), `AlertRadioButton`, `AlertCheckboxButton`
  (`.alert-radio-button`, `.alert-checkbox-button`), `AlertTextInput` and `AlertTextareaInput` (`.alert-input-wrapper` and its
  field). They take their state from the alert as pseudoclasses (`:checked`, `:role-cancel`) and do nothing on their own.
- **`AlertButtonGroup`**: the button group's panel: a row, wrapping in reverse, at the end; a column when `Vertical`. It says when
  its row wraps, and the alert sets `Vertical` at the next frame, latched until the buttons change, as Ionic's state is.
- **`AvonicOverlay`** gets one hook, `OnPresented()`: what an overlay's own `present()` does after the shared one, under the lock,
  which is where the alert focuses its wrapper or its one button. Listed in the PR under shared files.
- **The data**: `AlertButton` and `AlertInput` are plain classes. The alert keeps processed copies of the inputs, as Ionic does, so
  an app's objects aren't changed and a handler gets the copy. `Buttons` and `Inputs` are lists the alert follows: a new list, or
  a change to the one it has, re-renders, which a select needs when its options change while it's open.
- **The dismiss's data**: `AlertDismissData`, a dictionary with Ionic's `values` key and whatever a handler returned, and a `Values`
  property for the first.
- **`:breakpoint-md`**: the top level's client width is at least 768, for the media queries (`max-width: 767px`'s 266 caps, and the
  tablet's sizes).

### Not done, with why

| Not here | Why |
|---|---|
| `translucent` | ios only |
| `min`, `max`, `tabindex` on an input | Above |
| A native date, time, week or month picker for those types | A browser's own control. Here they're text boxes with the text keyboard; recorded |
| `innerHTMLTemplatesEnabled` and HTML in `message` | `Message` takes content instead |
| The ios button gesture (`createButtonActiveGesture`) | ios only |
| Root `aria-hidden` while it's open | The overlay system's deferral (#65) |
| A controller (`alertController.create`) | The overlay system's deferral; `new AvonicAlert { … }.Present()` is the same |
| `cssClass` and `attributes.class` as CSS | They're style classes here, for an app's styles |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | md enter: 150 ms `ease-in-out`, the backdrop 0.01 to 0.32 and the wrapper 0.01 to 1 and `scale(0.9)` to 1, frame by frame on the clock; the backdrop not hit-testable until the end | `md.enter.js`, M1 |
| B2 | md leave: 150 ms, the backdrop to 0, the wrapper from 0.99 to 0; `Avonic.Alert.BackdropOpacity` beats `Avonic.BackdropOpacity` | `md.leave.js`, M2 |
| B3 | Events: `IonAlertWillPresent` before `WillPresent` and the rest, with data and role | M3 |
| B4 | Focus at present: the wrapper; the button when it's the only one with no inputs, even with `KeyboardClose` off | `present()`, M11 |
| B5 | A button: dismisses with `{values}` and its role; the handler gets the values; `false` keeps it open; a dictionary or a task's is merged; a cancel role dismisses without its handler's say | `buttonClick`, M14, M15 |
| B6 | `ionAlertWillDismiss` with `cancel` or `backdrop` calls the cancel button's handler with no values (Escape, backdrop) | `dispatchCancelHandler`, M15 |
| B7 | Values: none, a radio's, the checkboxes', the text fields' by name or index | `getValues`, M14 |
| B8 | Radios: a tap checks one and unchecks the rest, its handler gets the processed copy; only the checked (or first enabled) one is a tab stop; arrows move and check, skipping disabled ones, wrapping | `rbClick`, `onKeydown`, M12, M13 |
| B9 | Checkboxes: a tap or Space toggles, Enter doesn't; the handler | `cbClick`, `onCheckboxKeydown`, M13 |
| B10 | Text fields: typing sets the value; placeholder, password mask, `maxlength`, disabled, `aria-label`; the app's `AlertInput` isn't changed | `renderInput`, M8 |
| B11 | Tab from the wrapper reaches the inputs then the buttons and wraps through the sentinels; Shift+Tab on the wrapper goes to the last button | `onKeydown`, overlay trap, M12 |
| B12 | `BackdropDismiss` false: no tap, Escape or back; true: each dismisses with `backdrop` | M16 |
| B13 | Enter in a text field doesn't dismiss | M13 |
| B14 | Button group: a row for one or two buttons; a column for three; a column after two wrap, which stays until the buttons change | `renderAlertButtons`, M7 |
| B15 | Layout: the measured boxes of M4 to M10 at 390 x 844 and the tablet's caps at 1024 x 768; the 266 caps; the wrapper's max height shrinks the scrollers | `alert.md.css`, M4 to M10 |
| B16 | Head: title and sub header shown only when set; the head's padding stays | render, M6 |
| B17 | `Message`: a string is text, a control is content | render |
| B18 | `CssClass` on the alert, a button and an input; `:role-<role>` on a button | `getClassMap`, `buttonClass` |
| B19 | Automation: the wrapper is a dialog named by the header and sub header, else `aria-label`; its help text the message; buttons named by their text or `aria-label`; radios and checkboxes toggleable with their state; `Id`s as `AutomationId` | render, `htmlAttributes` |
| B20 | `IsOpen` and `Trigger` present it, through the base | M3 |
| B21 | `Buttons` or `Inputs` replaced or changed while it's open re-render | watchers |

## Extensions

None. `Present(TopLevel)` is the overlay's.

## Dependencies

`AvonicOverlay`, `AvonicAnimation`, `AvonicBackdrop` (#5), `AvonicRippleEffect`, `Activatable`, `FocusVisible`, `NativeButton`,
`CssBoxShadow`, `TextareaNativeInput` and the native text field's theme (#24, #43): all merged.
