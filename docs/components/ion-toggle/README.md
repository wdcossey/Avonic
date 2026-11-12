# ion-toggle

Status: done · [#21](https://github.com/wdcossey/Avonic/issues/21)

`AvonicToggle` is a switch: a track and a handle beside a label, flipped by a click, Space, or a drag on the track. The design
and evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicItem>
    <c:AvonicToggle Content="Wi-Fi" Checked="{Binding WifiOn}" IonChange="OnWifiChange" />
</c:AvonicItem>

<c:AvonicToggle Content="Notifications" LabelPlacement="Stacked" Alignment="Start" Color="success"
                HelperText="We'll only send what matters" ErrorText="Turn this on to continue" />
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alignment` | `Alignment` (`ToggleAlignment?`) | `Center`, `Start`. Set, the toggle fills the width |
| [x] | `checked` | `Checked` | Two-way by default |
| [x] | `color` | `Color` | The checked track and handle. See the [colour system](../../infra/color-system/README.md) |
| [x] | `disabled` | `IsEnabled` | 0.38, as a group, and a tap goes to what's behind it. A disabled ancestor doesn't add it |
| [x] | `enableOnOffLabels` | `EnableOnOffLabels` | Unset, `AvonicConfig.ToggleOnOffLabels` (`false` by default) when the toggle is made, at style priority, so an app's value wins |
| [x] | `errorText` | `ErrorText` | Shown while the toggle has both style classes `ion-touched` and `ion-invalid` |
| [x] | `helperText` | `HelperText` | Shown otherwise |
| [x] | `justify` | `Justify` (`ToggleJustify?`) | `End`, `SpaceBetween`, `Start`. Set, the toggle fills the width |
| [x] | `labelPlacement` | `LabelPlacement` (`ToggleLabelPlacement`) | `End`, `Fixed`, `Stacked`, `Start` (default) |
| [x] | `value` | `Value` | `"on"`. Carried by `IonChange` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form |
| [ ] | `required` | — | Not applicable: Avalonia's automation can't say it |

### Events

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionChange` | `IonChange` (`ToggleChangeEventArgs`: `Checked`, `Value`), on each flip by the user, a drag's included. Not when `Checked` is set in code |
| [x] | `ionFocus` | `IonFocus` |
| [x] | `ionBlur` | `IonBlur` |

### Content and parts

The default slot is `Content`, the label: a string, or any control. Parts: `PART_Label`, `PART_HelperText` and `PART_ErrorText`
(each with the style class `supporting-text`, Ionic's part on both), `PART_Track` (the drag's element) and `PART_Handle`. Also
`PART_Wrapper` (a `LabelledControlPanel`, shared with the checkbox and the radio), `PART_LabelContent`, `PART_Bottom`, `PART_NativeWrapper`, `PART_IconWrapper` and
`PART_SwitchIcon`.

### Resource keys

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--track-background` | `Avonic.Toggle.TrackBackground` | brush | Text colour at 0.39 |
| [x] | `--track-background-checked` | `Avonic.Toggle.TrackBackgroundChecked` | brush | Primary at 0.5 |
| [x] | `--border-radius` | `Avonic.Toggle.BorderRadius` | `CornerRadius`, number or CSS string | 14, clamped to the track |
| [x] | `--handle-background` | `Avonic.Toggle.HandleBackground` | brush | White |
| [x] | `--handle-background-checked` | `Avonic.Toggle.HandleBackgroundChecked` | brush | Primary |
| [x] | `--handle-border-radius` | `Avonic.Toggle.HandleBorderRadius` | as `BorderRadius` | `50%` |
| [x] | `--handle-box-shadow` | `Avonic.Toggle.HandleBoxShadow` | `BoxShadows` | md's 1 dp elevation |
| [x] | `--handle-height` | `Avonic.Toggle.HandleHeight` | `double` | 20 |
| [x] | `--handle-max-height` | `Avonic.Toggle.HandleMaxHeight` | `double` | 20: `calc(100% + 6px)` of md's 14 px track |
| [x] | `--handle-spacing` | `Avonic.Toggle.HandleSpacing` | `double` | 0 |
| [x] | `--handle-transition` | `Avonic.Toggle.HandleTransition` | `Transitions` | Transform and background, 160 ms `cubic-bezier(0.4, 0, 0.2, 1)` |
| [x] | `--handle-width` | `Avonic.Toggle.HandleWidth` | `double` | 20 |

A known `Color` beats the two checked keys, as its rule beats the variables in Ionic. `Avonic.Toggle.Md.*` holds what md sets
with no variable: the on/off icon's colours and the disabled fade.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **A click** anywhere on the toggle flips it, on release, every time. **Space** flips it on key down, and again on each
  repeat. Enter does nothing. `IonChange` fires for each, and not when `Checked` is set in code.
- **A drag on the track** captures 5 px from the press, and then flips the toggle each time it's more than 10 px past the
  capture towards the other side, with `IonChange` each time. The drag focuses the toggle. A click within 300 ms of a drag's end
  is ignored. A drag that starts on the label isn't one, and a mouse still clicks on release.
- **In an item** it fills the row, and a tap on the item's padding flips it. In the start or end slot it has no margin.
- **Label placement**: start is label, then track at the far end; end is track, then label, at the start; fixed is start with a
  100 px label; stacked puts the label above at 0.75 scale. A long label is trimmed with an ellipsis. With no label, the track
  is at the start.
- **Outside an item** it's as wide as its content, unless `Justify` or `Alignment` is set.
- **Helper text** is under the label, at 12 px. With `Classes="ion-touched ion-invalid"`, the error text replaces it.
- **RTL** is the toggle's `FlowDirection`: the handle starts at the right, and a drag left turns it on.
- **Automation:** a button with the toggle pattern, named by the label and the helper text, which beats
  `AutomationProperties.Name` as `aria-labelledby` beats `aria-label`, with the hint as its help text, and both announced when
  they change. Toggling it is a click.
- **Keyboard focus** shows the platform's focus adorner, as Chromium draws its own ring.

## Extensions

None.

## Known gaps

| Gap | Why |
|---|---|
| A finger flips the toggle 16 px into a drag, where Chromium takes 27 | Chromium holds a finger's moves back for its 15 px slop; Avonic follows Ionic's code. See [COMPARISON.md](COMPARISON.md#deliberate-differences) |
| `dir="rtl"` on an ancestor, not the document or the toggle, leaves Ionic's handle and drag in LTR | Avalonia's `FlowDirection` can't say where it came from, so RTL is RTL throughout. Deliberate |
| A control as the label counts as a label even if it has no text | Ionic reads `textContent`, so an icon alone hides its label part there |
| The focus ring is the base theme's, not Chromium's | Measured `outline: auto 1px` in Chromium; Avalonia draws the base theme's adorner |
| `required`, `aria-invalid` aren't exposed | Avalonia 12's automation has neither |
| The track's colour mid-transition | Avalonia's `BrushTransition` doesn't blend as CSS does; the ends match. See [COMPARISON.md](COMPARISON.md#gaps) |
| An item that met its toggle disabled doesn't forward a padding tap once it's enabled | `AvonicItem`'s, proposed in the PR |
| No haptics on a drag's flip | `hapticSelection` is Capacitor's plugin, which does nothing in a browser |
| Not tried in a real window, or on a device | Real-window run queued |
