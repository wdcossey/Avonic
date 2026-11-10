# ion-range — Plan

A slider: one knob, or two for a lower and an upper value, on a bar, moved by a drag, a tap on the bar, or the arrow keys, with a
label, start and end slots, ticks, a pin and an active bar that can start anywhere. Measured from real Ionic 8 in Chromium, md, at
390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts: a mouse in a desktop context, CDP touch events in a mobile one,
and Chromium's accessibility tree through CDP. The pages are at the top of [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-range`.

### Props (17)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activeBarStart` | `ActiveBarStart` (`double?`) | Single knob only. Clamped into `[Min, Max]` when it's set after the range has loaded, and when it's attached, as `activeBarStartChanged` is a watch and in `connectedCallback`; with `SetCurrentValue`, so that a binding stays |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. Bars, knob, halo, pin and ticks |
| [x] | `debounce` | `Debounce` (`int?`, ms) | `IonInput` only, through `AvonicConfig.Schedule`, as `debounceEvent` is `setTimeout`. `int?` as in `AvonicInput` (#120) |
| [x] | `disabled` | `IsEnabled` | Mapping rule. `:range-disabled` follows the range's own `IsEnabled`, as Ionic's class follows its own prop |
| [x] | `dualKnobs` | `DualKnobs` (`bool`) | Ionic has no watch on it: the ratios are worked out from `Value` when the range is attached and when `Value`, `Min` or `Max` change, as there |
| [x] | `label` | `Label` (`string?`) | Beats `LabelContent`, as Ionic renders the slot only when `label` is undefined. `""` still counts as a label |
| [x] | `labelPlacement` | `LabelPlacement` (`RangeLabelPlacement`) | `End`, `Fixed`, `Stacked`, `Start` (default) |
| [x] | `max` | `Max` (`double`, 100) | Not finite: 100, as `maxChanged` does with `isSafeNumber` |
| [x] | `min` | `Min` (`double`, 0) | Not finite: 0 |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form. It names the hidden `<input>` that `renderHiddenInput` adds. As in #116, #117, #118 and #120 |
| [x] | `pin` | `Pin` (`bool`) | |
| [x] | `pinFormatter` | `PinFormatter` (`Func<double, string>`) | Default: JavaScript's `Math.round` (halves up), as text. Ionic's returns `string \| number`, which the pin shows as text |
| [x] | `snaps` | `Snaps` (`bool`) | |
| [x] | `step` | `Step` (`double`, 1) | Not finite: 1. 0 is allowed, as in Ionic: a continuous value, rounded to the decimals of `Min`, `Max` and `Step` (measured: 33 at a third) |
| [x] | `ticks` | `Ticks` (`bool`, true) | Only with `Snaps` |
| [x] | `value` | `Value` (`RangeValue`, 0) | Ionic's `RangeValue`, `number \| { lower, upper }`, as a struct: `40`, or `new RangeValue(20, 80)`; `"40"` and `"20,80"` in XAML. Two-way by default, as Ionic's is `mutable`, and set with `SetCurrentValue`. Not clamped when set in code (measured: 150 stays 150, with the knob at the end), only when the user commits a change |

### Events (6), methods (0), slots (3)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | `ionChange` | `IonChangeEvent`, `IonChange` (`RangeChangeEventArgs`: `Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionInput` | `IonInputEvent`, `IonInput` (`RangeChangeEventArgs`) |
| [x] | `ionKnobMoveEnd` | `IonKnobMoveEndEvent`, `IonKnobMoveEnd` (`RangeKnobMoveEventArgs`: `Value`) |
| [x] | `ionKnobMoveStart` | `IonKnobMoveStartEvent`, `IonKnobMoveStart` (`RangeKnobMoveEventArgs`) |
| [x] | `end` slot | `EndContent`, `EndContentTemplate` |
| [x] | `label` slot | `LabelContent`, `LabelContentTemplate` |
| [x] | `start` slot | `StartContent`, `StartContentTemplate` |

### Shadow parts (24)

An element has one name, and Ionic puts several parts on one element, and one part on several. So the names are the elements that
are always there, and the rest are style classes and pseudoclasses on them.

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `label` | `PART_Label` (`.label-text-wrapper`) |
| [x] | `bar` | `PART_Bar` |
| [x] | `bar-active` | `PART_BarActive` |
| [x] | `tick`, `tick-active` | Style classes `tick` and `tick-active` on each tick, a `Border` the range makes |
| [x] | `knob-handle` | Style class `knob-handle` on both handles, each a `RangeKnobHandle` |
| [x] | `knob-handle-a`, `knob-handle-b` | `PART_KnobHandleA`, `PART_KnobHandleB` |
| [x] | `knob-handle-lower`, `knob-handle-upper` | `:lower`, `:upper` on the handle, with two knobs. They swap as the knobs cross |
| [x] | `knob`, `knob-a`, `knob-b` | `PART_KnobA`, `PART_KnobB`, style class `knob` |
| [x] | `knob-lower`, `knob-upper` | The handle's `:lower`, `:upper` |
| [x] | `pin`, `pin-a`, `pin-b` | `PART_PinA`, `PART_PinB`, style class `pin` |
| [x] | `pin-lower`, `pin-upper` | The handle's `:lower`, `:upper` |
| [x] | `pressed` | `:pressed` on the handle (`range-knob-pressed`) |
| [x] | `focused` | `:ion-focused` on the handle: Ionic's `ion-focused` class and `focused` part are the same state |
| [x] | `hover` | `:ion-hover` on the handle, from `Activatable`, mouse and pen only. Ionic's part follows `mouseenter`, which Chromium also fires for a touch, and keeps after it: not reproduced |
| [x] | `activated` | `:activated` on the handle, from `Activatable` (`ion-activatable`, tap-click) |

Other parts: `PART_Wrapper` (`.range-wrapper`, a `RangePanel`), `PART_LabelText` (`.label-text`), `PART_LabelContent` (the `label`
slot), `PART_NativeWrapper` (`.native-wrapper`), `PART_Start` and `PART_End` (the slots), `PART_Slider` (`.range-slider`, a
`RangeSlider`, the gesture's element), and in each handle `PART_PinTextA` / `B` and the knob's `PART_HaloA` / `B` (`::before`).

### CSS variables (11)

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--bar-background` | `Avonic.Range.BarBackground` | `rgba(var(--ion-color-primary-rgb), 0.26)` |
| [x] | `--bar-background-active` | `Avonic.Range.BarBackgroundActive` | `var(--ion-color-primary)` |
| [x] | `--bar-border-radius` | `Avonic.Range.BarBorderRadius` | 0. A `CornerRadius`, number or CSS string, read by `BorderRadius.ToCornerRadius` |
| [x] | `--bar-height` | `Avonic.Range.BarHeight` | 2 |
| [x] | `--height` | `Avonic.Range.Height` | 42 |
| [x] | `--knob-background` | `Avonic.Range.KnobBackground` | `var(--bar-background-active)`: `CssUnset`, read through `Css.Var` with `BarBackgroundActive` next |
| [x] | `--knob-border-radius` | `Avonic.Range.KnobBorderRadius` | `50%` |
| [x] | `--knob-box-shadow` | `Avonic.Range.KnobBoxShadow` | none: an empty `BoxShadows` |
| [x] | `--knob-size` | `Avonic.Range.KnobSize` | 18. The handle is twice it (`--knob-handle-size`) |
| [x] | `--pin-background` | `Avonic.Range.PinBackground` | `var(--ion-color-primary)` |
| [x] | `--pin-color` | `Avonic.Range.PinColor` | `var(--ion-color-primary-contrast)` |

md's literals, with no variable, are in the theme: the knob's 0.67 scale (1 pressed or focused with no pin, 0.55 disabled), its
halo's 2× and 0.13 or 0.25, the pin's 28 px and 24 px rise, the 16 px label gap, the 14 px slot gaps, 12 px text, 24 px slot
icons, the 18 px item adjustments, 28 px of room for the pin, the 100 px fixed label, the 200 px slotted label, the stacked label's
0.75 and its 10 and 16 px margins, the disabled grey and white ring, and 0.38 for a disabled label and slots. The transitions: the
knob's transform, 120 ms `ease`; its halo's, 267 ms `cubic-bezier(0, 0, 0.58, 1)`; the pin's transform, 120 ms `ease`.

## Behaviours

From `range.js` and `range.md.css`, each checked in Chromium. Each becomes a test.

| # | Behaviour | Source, measurement |
|---|---|---|
| 1 | The knob's centre is at `(value - min) / (max - min)` of the slider, clamped to it; the active bar runs from the start to the knob. A value out of range isn't clamped | `valueToRatio`, `renderKnob`; measured `r1` (40: bar 114.1 of 285.3), `150` |
| 2 | Setting `Value` in code raises `IonInput` when it differs, never `IonChange`; the value it loads with raises nothing, as a Stencil watch isn't called for it | `valueChanged`; measured `programmatic` |
| 3 | A tap or click on the bar moves the knob to the release point: `IonKnobMoveStart` (the old value), `IonInput` (the new, if it differs), `IonChange`, `IonKnobMoveEnd`. A tap at the knob's own value still raises `IonChange`. A press doesn't move it, and a move under 10 px is still a tap, at the release | `onPointerUp`, `onStart`, `onEnd`; measured `click 75%`, `click same`, `6px move`, `touch tap 20%` |
| 4 | A drag on the slider captures at 10 px, priority 100: `IonKnobMoveStart` at the capture, `IonInput` once a frame, `IonChange` and `IonKnobMoveEnd` at the release. The knob follows the pointer exactly, and the value rounds to `Step`. Meanwhile the range is `:range-pressed` and the knob `:pressed` | `setupGesture`, `onMove`, `update`; measured `drag` |
| 5 | `Snaps` puts the knob on a step as it moves. With `Ticks` too, a tick per step from `Min` to `Max`, active between the two ends of the active bar | `update`, `renderRangeSlider`; measured `r8` (11 ticks, 6 active) |
| 6 | Right and Up on a focused knob add a step, Left and Down take one away, in RTL too; no other key does anything. Each raises start, input (if it changed), change and end, at the limit too | `handleKeyboard`; measured `ArrowRight` to `Enter`, `Right at max` |
| 7 | Two knobs: `Value` is `{ lower, upper }`. A press moves the nearer knob, B on a tie. They can cross: A and B keep their identity, and `:lower` and `:upper` swap. A value set in code goes to the knobs so that they move least. A number given to two knobs is `{ 0, n }`, and a pair given to one is its upper | `setPressedKnob`, `updateRatio`, `getValue`; measured `A drag`, `click 50%`, `equidistant`, `set 70 number`, `single obj` |
| 8 | `ActiveBarStart`: the bar runs between it and the knob, on whichever side the knob is; the ticks between are active | `renderRangeSlider`; measured `r9` (bar from 20 % to 50 %) |
| 9 | `Pin`: a pin over the knob with `PinFormatter(value)`, scaled from 0.01 to 1 and raised 24 px while the knob is pressed or keyboard-focused, with the knob staying at 0.67. Room for it: 28 px of padding above, or under a stacked label | md.css; measured `press`, `release 500`, `r3` (host 70 tall) |
| 10 | The knobs are tab stops, A then B. `IonFocus` as focus enters the range and `IonBlur` as it leaves; nothing between its knobs. A knob focused by keyboard or code is `:ion-focused` (halo 2×, pin up, knob at 1 with no pin), and one focused by a press isn't. A press on a knob focuses it | `onKnobFocus`, `onKnobBlur`, `focusFromPointer`; measured `tabs`, `focused part`, `click same` |
| 11 | Disabled: no drag, tap or key, and not a tab stop; the bar, ticks and knob grey, the knob at 0.55 in a white ring, the label and slots at 0.38 | `disabled`, `tabindex`, md.css; measured `r10` |
| 12 | In an item, at any depth: `:in-item`; with nothing at the start (a start or fixed label, or a start slot) 18 px of padding there, and the same at the end, so that a knob at the limit isn't cut off; a stacked label gets 10 px above. The item counts it as an input. In an item's start or end slot, `flex: 3` (measured: 265.5 of the row, the slider 229.5) | `hostContext('ion-item')`, `needsStartAdjustment`; measured `r1`, `r2`, `r6`, `r4` |
| 13 | The label: start puts it before the slider with 16 px between, end after; fixed is 100 px wide, wrapping; stacked is above at 0.75 from its top start corner, 16 px over the slider. With none, it takes no room. A long label squeezes the slider, to nothing (measured `r12`). A label in the slot is at most 200 px, on one line, with an ellipsis | md.css; measured `r1`, `r4`, `r5`, `r6`, `r12` |
| 14 | Start and end slots: 14 px toward the slider, text at 12 px, an icon at 24 px | md.css; measured `r8`, `r11` |
| 15 | RTL mirrors it: the knob is near the right for a low value, and a press counts from the right. The arrows don't swap | `isRTL`; measured `rtl` |
| 16 | `Color`: the bar in the colour at 0.26, and the active bar, knob, halo, pin and ticks in it | md.css `:host(.ion-color)` |
| 17 | `Debounce`: `IonInput` waits until that long after the last change, with the last value; `IonChange` doesn't | `debounceEvent`; measured `debounced r3 twice` (changes at 795 and 856 ms, input at 1058) |
| 18 | While a knob is dragged inside an `AvonicContent`, its `ScrollY` is off, and back after. A vertical drag doesn't capture, and scrolls | `disableContentScrollY`; measured `vertical drag`, `sideways then up` |
| 19 | The halo: 2× at 0.13 while hovered by a mouse or keyboard-focused, 2× at 0.25 while activated (tap-click, after 100 ms) or pressed | md.css; measured `press 180ms`, `press 580ms` |
| 20 | Automation: each knob is a slider with `Min`, `Max` and its value, read-only when disabled, named by `AutomationProperties.Name` (Ionic's `aria-label`), or else by the label element's text: the label, the start slot, its own pin, the other knob's value and the end slot | `renderKnob` `aria-*`; measured in the AX tree: `"Label"`, `"Pin 60"`, `"Low High"`, `"Aria"`, `"Dual 80"` and `"Dual 20"` |
| 21 | `Min`, `Max` or `Step` not finite go back to 0, 100 and 1. A change of `Min` or `Max` moves the knobs, not the value. A fractional step rounds to its decimals (0.1: 0.7, not 0.7000000000000001) | `minChanged`, `ratioToValue`, `roundToMaxDecimalPlaces`; measured `min 40 at 50`, `step 0.1`, `step 0` |
| 22 | With two knobs, a release puts each on its value; one knob stays where it was let go | `emitValueChange` sets a new object, which `valueChanged` sends to `updateRatio`; measured mid-drag `right: 10.2484%`, after `10%` |
| 23 | Host classes as pseudoclasses: `:range-dual-knobs`, `:range-pressed`, `:range-pressed-a`, `-b`, `-lower`, `-upper`, `:range-has-pin`, `:range-value-min`, `:range-value-max`, `:label-placement-*`, `:range-item-start-adjustment`, `:range-item-end-adjustment`; on the handle `:range-knob-min`, `:range-knob-max` | `render`; measured classes |

### Not reproduced

- **The hidden form input** (`renderHiddenInput`).
- **A tap that focuses after the release.** Chromium focuses a tapped knob from the compatibility mouse events, after `pointerup`
  has cleared `focusFromPointer`, so a tap on a knob leaves it looking keyboard-focused, pin up (measured `release 500`). Avalonia
  focuses on the press, so a tap is a press, as a mouse click is in Chromium. Recorded in the comparison.
- **A press outside a knob blurs the range** in Chromium (measured `6px move`: `ionBlur`), as a browser blurs on any press on
  something that can't take focus. Avalonia keeps the focus; that's the platform's, not the range's.
- **`cursor: grab` and `grabbing`.** Avalonia has neither, and `Hand` stands for both.
- **Ticks with `Step` 0,** or a step too small to change a large value. Ionic's loop never ends and hangs the page; here there
  are none, or the loop stops.
- **`resetContentScrollY` after a tap.** Ionic's `onEnd` turns the content's `scrollY` back on after a tap that never turned it
  off, even if the app had it off; here only a drag that turned it off turns it back on.

## Design

```
RangePanel PART_Wrapper                  .range-wrapper: a row, a reversed row (end) or a column (stacked)
├─ Panel PART_Label                      .label-text-wrapper, hidden with no label
│  ├─ TextBlock PART_LabelText           .label-text, from Label
│  └─ ContentPresenter PART_LabelContent the label slot, while Label is null
└─ Grid PART_NativeWrapper               .native-wrapper: Auto, *, Auto, with 12 px text
   ├─ ContentPresenter PART_Start
   ├─ RangeSlider PART_Slider            .range-slider, 42 px: the gesture's element
   │  ├─ Border PART_Bar, Border PART_BarActive
   │  ├─ Border.tick …                   made by the range
   │  ├─ RangeKnobHandle PART_KnobHandleA  36 × 36, centred on its ratio
   │  │  ├─ Border PART_PinA (TextBlock PART_PinTextA)
   │  │  └─ Panel PART_KnobA (Border PART_HaloA, and the knob's own Border)
   │  └─ RangeKnobHandle PART_KnobHandleB  shown with two knobs
   └─ ContentPresenter PART_End
```

`AvonicRange : TemplatedControl, IItemControl`. The state is Ionic's: `ratioA`, `ratioB`, `pressedKnob`, `focusedKnob`, `hasFocus`,
`focusFromPointer`, `noUpdate`, and each method translates its namesake. `RangeSlider` places a child with `RangeSlider.Start` (a
ratio) at that fraction of its width, and one with `RangeSlider.End` too between the two, as `left` and `right` percentages do; a
handle's negative start margin centres it, as `margin-inline-start: calc(0px - var(--knob-handle-size) / 2)` does. It's mirrored in
RTL, as any panel is, so a press is read in its own coordinates, which then count from the right. `RangePanel` lays out the label
and the native wrapper in a row, a reversed row or a column, as `TogglePanel` does in #116.

The gesture is `Gesture.Create` on `PART_Slider` with Ionic's name, priority and threshold, made once the template is applied and
the range is in the tree, destroyed when it leaves, and enabled with `IsEffectivelyEnabled`. The tap is the slider's
`PointerReleased` for a press the gesture didn't capture, as Ionic's `onPointerUp` is for a press with no `pressedKnob`.

The rules that read the range's state, and the slots' rules, are in the `ControlTheme`: `::slotted([slot=label])` goes on the label
presenter, and `::slotted(ion-icon[slot])` is a pseudoclass that the range works out from what's in each slot, `:start-icon` and
`:end-icon`. Nothing is added to `Styles/`, so the style budget is untouched.

### RTL: a deliberate difference

As in #116: Ionic's `isRTL` reads only the range's own `dir` and the document's. `FlowDirection` inherits, so here RTL is RTL
throughout.

## Dependencies

`ion-item` (#6), `ion-label` (#7), the gesture utility (#4): all merged.
