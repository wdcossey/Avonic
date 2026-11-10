# ion-range

Status: done · [#23](https://github.com/wdcossey/Avonic/issues/23)

`AvonicRange` is a slider: one knob, or two for a lower and an upper value, moved by a drag, a tap on the bar or the arrow keys,
with a label, start and end slots, ticks, a pin, and an active bar that can start anywhere. The design and evidence are in
[PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicRange Label="Volume" Value="40" IonChange="OnVolume" />
</c:AvonicItem>
<c:AvonicRange DualKnobs="True" Value="20,80" Pin="True" />
<c:AvonicRange Snaps="True" Step="10" Value="50" StartContent="Low" EndContent="High" />
<c:AvonicRange ActiveBarStart="50" Value="20" Color="danger" />
```

`Value` is a `RangeValue`: a number for one knob, `new RangeValue(lower, upper)` for two, `"40"` or `"20,80"` in XAML. A `double`
converts to one, and `(double)value` is the number, or a pair's upper. A binding to a `double` or `int` works both ways.

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activeBarStart` | `ActiveBarStart` (`double?`) | One knob only. Clamped into `Min` to `Max`, once they're set |
| [x] | `color` | `Color` | See the [colour system](../../infra/color-system/README.md) |
| [x] | `debounce` | `Debounce` (`int?`, ms) | Delays `IonInput` only |
| [x] | `disabled` | `IsEnabled` | `:range-disabled` follows the range's own `IsEnabled`, as Ionic's class follows its own prop |
| [x] | `dualKnobs` | `DualKnobs` (`bool`) | |
| [x] | `label` | `Label` (`string?`) | Beats `LabelContent`. `""` still counts as a label |
| [x] | `labelPlacement` | `LabelPlacement` (`RangeLabelPlacement`: `Start`, `End`, `Fixed`, `Stacked`) | `Start` by default |
| [x] | `max` | `Max` (`double`, 100) | Not finite: 100 |
| [x] | `min` | `Min` (`double`, 0) | Not finite: 0 |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [x] | `pin` | `Pin` (`bool`) | |
| [x] | `pinFormatter` | `PinFormatter` (`Func<double, string>`) | JavaScript's `Math.round` by default. Given the knob's value, which is on a step |
| [x] | `snaps` | `Snaps` (`bool`) | |
| [x] | `step` | `Step` (`double`, 1) | Not finite: 1. 0 is a continuous value |
| [x] | `ticks` | `Ticks` (`bool`, true) | With `Snaps` |
| [x] | `value` | `Value` (`RangeValue`, 0) | Two-way by default. Not clamped when set in code, only when the user commits a change |

### Events

| ✓ | Ionic | Avonic | When |
|---|---|---|---|
| [x] | `ionInput` | `IonInput` (`RangeChangeEventArgs`) | The value changed: once a frame while dragging, at a tap or a key, and when it's set in code, but not for the value it loads with. After `Debounce` |
| [x] | `ionChange` | `IonChange` (`RangeChangeEventArgs`) | The user committed a value: at the release, or a key. Even when it's the same value. Never from code |
| [x] | `ionKnobMoveStart` | `IonKnobMoveStart` (`RangeKnobMoveEventArgs`) | A drag captured, a tap or a key began, with the value before |
| [x] | `ionKnobMoveEnd` | `IonKnobMoveEnd` (`RangeKnobMoveEventArgs`) | After `IonChange` |
| [x] | `ionFocus` | `IonFocus` | Focus entered the range; moving between its knobs isn't another |
| [x] | `ionBlur` | `IonBlur` | Focus left the range |

No methods, as in Ionic.

### Slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `start` | `StartContent`, `StartContentTemplate`: 14 px before the slider, text at 12 px, an `AvonicIcon` at 24 px |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `label` | `LabelContent`, `LabelContentTemplate`: at most 200 px, on one line, with an ellipsis |

### Parts and states

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `label` | `PART_Label` |
| [x] | `bar`, `bar-active` | `PART_Bar`, `PART_BarActive` |
| [x] | `tick`, `tick-active` | `PART_Ticks`, a `RangeTicks`, which draws them: `Background` for `tick`, `ActiveBackground` for `tick-active`, `TickSize` |
| [x] | `knob-handle`, `-a`, `-b` | `PART_KnobHandleA`, `PART_KnobHandleB`, `RangeKnobHandle`s with the class `knob-handle` |
| [x] | `knob`, `-a`, `-b` | `PART_KnobA`, `PART_KnobB`, class `knob`; its halo (`::before`) is `PART_HaloA`, `PART_HaloB` |
| [x] | `pin`, `-a`, `-b` | `PART_PinA`, `PART_PinB`, class `pin`; the text is `PART_PinTextA`, `PART_PinTextB` |
| [x] | `…-lower`, `…-upper` | `:lower`, `:upper` on a handle, with two knobs |
| [x] | `pressed`, `focused`, `hover`, `activated` | `:range-knob-pressed`, `:ion-focused`, `:ion-hover`, `:activated` on a handle |

The range's own pseudoclasses are Ionic's host classes: `:in-item`, `:range-disabled`, `:range-dual-knobs`, `:range-pressed`,
`:range-pressed-a`, `-b`, `-lower`, `-upper`, `:range-has-pin`, `:label-placement-*`, `:range-item-start-adjustment`,
`:range-item-end-adjustment`, `:range-value-min`, `:range-value-max`, and `:label-text-wrapper-hidden`. `:start-icon` and `:end-icon`
say that a slot holds an `AvonicIcon`, for `::slotted(ion-icon[slot])`. A handle also has `:range-knob-min` and `:range-knob-max`.

## Behaviours

All 23 in [PLAN.md](PLAN.md#behaviours), each with a test in `AvonicRangeTests`, named `R<number>_…`. In short:

- A drag on the slider decides at 10 px, at priority 100, so a vertical drag still scrolls; then the nearer knob follows the pointer
  exactly, and the value rounds to `Step`. In an `AvonicContent`, its `ScrollY` is off until the release.
- A tap, or a move under 10 px, moves the nearer knob to the release. With two knobs, a tie goes to B, and the knobs can cross.
- Right and Up add a step on a focused knob, Left and Down take one away, in RTL too.
- In an item, 18 px of padding at an end with nothing there, so that a knob at the limit isn't cut off by the item. In an item's
  start or end slot it grows, with `FlexRow.Flex` 3, as `flex: 3`.
- Each knob is a slider for automation, named by `AutomationProperties.Name`, or else as Chromium names it from the label element.

## Resource keys

One per Ionic CSS variable, with md's defaults, in `Avonic.Themes/Tokens/Range.axaml`:

| Ionic | Key | md |
|---|---|---|
| `--bar-background` | `Avonic.Range.BarBackground` | primary at 0.26 |
| `--bar-background-active` | `Avonic.Range.BarBackgroundActive` | primary |
| `--bar-border-radius` | `Avonic.Range.BarBorderRadius` | 0 (a `CornerRadius`, number or CSS string) |
| `--bar-height` | `Avonic.Range.BarHeight` | 2 |
| `--height` | `Avonic.Range.Height` | 42 |
| `--knob-background` | `Avonic.Range.KnobBackground` | unset: the active bar's |
| `--knob-border-radius` | `Avonic.Range.KnobBorderRadius` | `50%` |
| `--knob-box-shadow` | `Avonic.Range.KnobBoxShadow` | none |
| `--knob-size` | `Avonic.Range.KnobSize` | 18; the handle is twice it |
| `--pin-background` | `Avonic.Range.PinBackground` | primary |
| `--pin-color` | `Avonic.Range.PinColor` | primary's contrast |

md's literals are in the theme (see PLAN.md). Two keys aren't Ionic's: `Avonic.Range.Md.HaloBorderRadius` (the halo's own `50%`)
and `Avonic.Range.Md.DisabledKnobOutline` (the white ring).

## Extensions

None.

## Known gaps

See [COMPARISON.md](COMPARISON.md#known-gaps-and-differences): `cursor: grab`, the touch `hover` part, ticks that would hang Ionic,
an overflowing row, and the form input. Not tried in a real window or on a device.
