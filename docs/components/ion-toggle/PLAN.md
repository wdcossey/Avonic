# ion-toggle — Plan

A switch: a track and a handle beside a label, flipped by a click, Space, or a drag on the track. It's the first control with
`labelPlacement`, `justify`, `alignment`, helper and error text, which `ion-checkbox` (#20) and `ion-radio` (#22) share, and the
first to use the [gesture utility](../../infra/gesture/README.md) in a control. Measurements are from real Ionic 8.8.1 in Chromium,
md, at 390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts: a mouse in a desktop context, and CDP touch events in a
mobile one. The commands are at the top of [COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-toggle`.

### Props (13)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `alignment` | `Alignment` (`ToggleAlignment?`) | `Center`, `Start`; null is the default. Cross-axis alignment, and `display: block` |
| [x] | `checked` | `Checked` (`bool`) | Two-way by default, as Ionic's is `mutable` and the toggle sets it itself. `SetCurrentValue`, so a binding survives |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. Only the checked state reads it |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `enableOnOffLabels` | `EnableOnOffLabels` (`bool`) | Ionic's default is `config.get('toggleOnOffLabels')`, which is undefined unless an app sets it. `AvonicConfig` is a shared file, so the config is under "Proposed for shared files", and the default here is `false` |
| [x] | `errorText` | `ErrorText` (`string?`) | Shown instead of the helper text while the toggle has both style classes `ion-touched` and `ion-invalid` |
| [x] | `helperText` | `HelperText` (`string?`) | |
| [x] | `justify` | `Justify` (`ToggleJustify?`) | `End`, `SpaceBetween`, `Start`; null is the default. Main-axis packing, and `display: block` |
| [x] | `labelPlacement` | `LabelPlacement` (`ToggleLabelPlacement`) | `End`, `Fixed`, `Stacked`, `Start` (default) |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `name` | — | Not applicable: HTML form. It only names the hidden `<input>` that `renderHiddenInput` adds for form submission |
| [ ] | `required` | — | Not applicable: it's only `aria-required`, and Avalonia 12's automation has no way to say it. `AutomationProperties.IsRequiredForForm` exists, but no peer or platform reads it, so it would be a dead prop. Recorded for Avalonia |
| [x] | `value` | `Value` (`string?`, `"on"`) | Carried in `IonChange`'s args, as Ionic's detail carries it. Its other use is the hidden form input, which doesn't apply |

### Events, methods, slots

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `ionChange` | `IonChangeEvent`, `IonChange`, with `ToggleChangeEventArgs` (`Checked`, `Value`) |
| [x] | `ionFocus` | `IonFocusEvent`, `IonFocus` |
| [x] | `ionBlur` | `IonBlurEvent`, `IonBlur` |
| [x] | default slot | `Content`, `ContentTemplate`: the label |

No methods.

### Shadow parts (6)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `label` | `PART_Label` (`.label-text-wrapper`): the slot and the supporting text |
| [x] | `supporting-text` | Not a name: an element has one name, and Ionic puts this part on both texts. Each has the style class `supporting-text` |
| [x] | `helper-text` | `PART_HelperText` (`TextBlock`) |
| [x] | `error-text` | `PART_ErrorText` (`TextBlock`) |
| [x] | `track` | `PART_Track` (`.toggle-icon`): the gesture's element |
| [x] | `handle` | `PART_Handle` (`.toggle-inner`) |

Other parts: `PART_Wrapper` (`.toggle-wrapper`, a `TogglePanel`), `PART_LabelContent` (the `<slot>`), `PART_Bottom`
(`.toggle-bottom`), `PART_NativeWrapper` (`.native-wrapper`), `PART_IconWrapper` (`.toggle-icon-wrapper`, which moves) and
`PART_SwitchIcon` (the on/off `ion-icon`).

### CSS variables (12)

Each has an md value on `:host`, so each key has a value, not `CssUnset`.

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--track-background` | `Avonic.Toggle.TrackBackground` | brush | `rgba(var(--ion-text-color-rgb), 0.39)`: `Avonic.TextColor` at 0.39 |
| [x] | `--track-background-checked` | `Avonic.Toggle.TrackBackgroundChecked` | brush | `rgba(var(--ion-color-primary-rgb), 0.5)`: `Avonic.Color.Primary` at 0.5 |
| [x] | `--border-radius` | `Avonic.Toggle.BorderRadius` | `CornerRadius`, number or CSS string | 14, which CSS clamps to 7 on a 14 px track. Read through `BorderRadius.ToCornerRadius`, which clamps as CSS does |
| [x] | `--handle-background` | `Avonic.Toggle.HandleBackground` | brush | `#ffffff` |
| [x] | `--handle-background-checked` | `Avonic.Toggle.HandleBackgroundChecked` | brush | `Avonic.Color.Primary` |
| [x] | `--handle-border-radius` | `Avonic.Toggle.HandleBorderRadius` | as `--border-radius` | `50%` |
| [x] | `--handle-box-shadow` | `Avonic.Toggle.HandleBoxShadow` | `BoxShadows` | md's 1 dp elevation, three shadows |
| [x] | `--handle-height` | `Avonic.Toggle.HandleHeight` | `double` | 20 |
| [x] | `--handle-max-height` | `Avonic.Toggle.HandleMaxHeight` | `double` | `calc(100% + 6px)` of the 14 px track: 20. A percentage can't be a `double`, so it's resolved against md's track, which is a literal in the theme. An app that changes the track's height sets this too |
| [x] | `--handle-spacing` | `Avonic.Toggle.HandleSpacing` | `double` | 0. `left: var(--handle-spacing)`, and `translate(-2 × spacing)` when checked |
| [x] | `--handle-transition` | `Avonic.Toggle.HandleTransition` | `Transitions` | `transform` and `background-color`, 160 ms `cubic-bezier(0.4, 0, 0.2, 1)`, on both the icon wrapper and the handle, as Ionic applies it to both |
| [x] | `--handle-width` | `Avonic.Toggle.HandleWidth` | `double` | 20 |

md's literals, with no variable, are in the theme: the track's 36 × 14, its `background-color 160ms` transition (CSS's default
`ease`), the label's 16 px gap, the item margins, the 100 px fixed label, 0.38 opacity when disabled, and the on/off icon's colour.

## Behaviours

From `toggle.js` and `toggle.md.css`, each checked in Chromium. Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | A click anywhere on the toggle (the label, the track, the gap between, the empty end of the row) flips `Checked` and raises `IonChange` once, with `Checked` and `Value`. On release, every press: a double click flips twice. A finger that moved more than 15 px before lifting doesn't click | `onClick` on the host, `onDivLabelClick`; measured `mouseClickLabel`, `…Track`, `…Gap`, `dblclick`, `touchTap`, `touchDragLabel` |
| 2 | Setting `Checked` in code raises nothing | `@Prop checked`; measured `programmatic` |
| 3 | Space flips it on key down, and again on each repeat. Enter does nothing | `onKeyDown`; measured `space`, `enter`, `spaceRepeat` |
| 4 | Disabled: no click, no key, no drag, not a tab stop, opacity 0.38, and a tap goes through to what's behind | `disabled`, `disabledChanged`, `tabindex`, `pointer-events: none`; measured `disabledClick`, `disabledTabindex`, `spaceDisabledFocused` |
| 5 | A drag on the track, priority 100, captures at 5 px. Then each move whose `deltaX` from the capture is past 10 px toward the other side flips it, with `IonChange` each time: on at more than +10, off again at less than −10. Leaving the track doesn't end it | `setupGesture`, `onMove`, `shouldToggle`; measured `mouseDragTrack` (on at 16 px from the press, off at −6), `dragOutside` |
| 6 | A press on the label that's dragged isn't a gesture: a mouse still clicks on release | gesture `el` is the track; measured `mouseDragFromLabel` |
| 7 | A click within 300 ms of a drag's end is ignored, and one after counts | `lastDrag`; measured `clickRightAfterDrag`, `clickLater` |
| 8 | The drag's start focuses the toggle, and it's `:activated` until the end. `IonFocus` on that focus | `onStart`, `setFocus`, `toggle-activated`; measured `focused`, `activated` |
| 9 | RTL flips the drag: off to on is a drag left of more than 10 px. The handle sits at the right when off | `isRTL`, `shouldToggle`; measured `hostRtlDragLeft`, `hostRtl` |
| 10 | `IonFocus` and `IonBlur` on the toggle's own focus, including a press; a press focuses it; it's a tab stop | `onFocus`, `onBlur`, `tabindex=0`; measured `mouseClickLabel`, `touchTap`, `focusVisible` |
| 11 | Keyboard focus shows the platform's focus ring: there's no `outline: none`, so Chromium draws its own | measured `focusVisible`: `outline: auto 1px` |
| 12 | In an item, a tap on the item's padding clicks the toggle. A disabled toggle isn't the first interactive control | `ion-item`'s `getFirstInteractive`; measured `mouseClickItemPadding`, `mouseClickItemEnd` |
| 13 | In an item (any depth), `:in-item`: the toggle fills the row, the label gets 10 px above and below, stacked 10 and 16, and the track 10 below when stacked | `hostContext('ion-item')`; measured boxes `host`, `t3` |
| 14 | With no label, the label part isn't shown, so its margin goes too, and the track is at the start of the row | `label-text-wrapper-hidden`; measured `t4track` |
| 15 | Label placement start: label, 16 px, track, with the track at the end (space-between). End: track, 16 px, label, packed at the start. Fixed: start, with the label 100 px, trimmed with an ellipsis. Stacked: the label above at 0.75 scale, 16 px over the track, both centred | measured `boxes`, `endItem`, `fixed`, `t3`, `stackedAlone` |
| 16 | Justify start, end and space-between pack the row; `alignment` start puts the stacked label and track at the start, scaled from the top left (top right in RTL) | measured `justStart`, `alignStart` |
| 17 | Outside an item, with no justify or alignment, it's inline-block: as wide as its content. With either, it fills the width | `display: inline-block`, `display: block`; measured `t6`, `t7`, `justStart` |
| 18 | A long label is trimmed with an ellipsis on one line, and doesn't push the track out | `.label-text-wrapper`; measured `fixed` |
| 19 | Helper text under the label, 12 px, `--ion-text-color-step-300`. With `ion-touched` and `ion-invalid` the error text in danger replaces it. Neither shows if both are empty | `renderHintText`, `checkInvalidState`; measured `helper` |
| 20 | Unchecked: track `rgba(text, 0.39)`, white handle. Checked: track primary at 0.5, handle primary, moved right by the track's width less the handle's. A known `Color` replaces primary in both, and beats the tokens | md.css; measured `checkedComputed`, `handleChecked` |
| 21 | The handle and its wrapper move and recolour over 160 ms `cubic-bezier(0.4, 0, 0.2, 1)`, and the track over 160 ms `ease` | measured `transitions` |
| 22 | `EnableOnOffLabels`: an icon in the handle, `remove-outline` when off and `checkmark-outline` when on, 18 px, black when off and the colour's contrast (white) when on | `renderOnOffSwitchLabels` md; measured `icon`, `iconColor` |
| 23 | Automation: a toggle, named by the label and the helper text while valid, or `AutomationProperties.Name`; its help text is the error text when invalid, else the helper text; toggling it is a click | `role=switch`, `aria-labelledby`, `aria-describedby`; measured the AX tree: `"Default"`, `"Helper Helper text"`, `"Helper"` when invalid, `""` with no label |
| 24 | In an item's start or end slot, no margin | `::slotted(ion-toggle[slot])` in `item.md.css` |

### Not reproduced

- **Haptics.** `hapticSelection` on each flip of a drag is Capacitor's Haptics plugin, which does nothing in a browser.
- **The hidden form input.** `renderHiddenInput` is for HTML forms.
- **`aria-invalid`, `aria-required`.** Avalonia 12's automation has neither.

## Design

### Structure

```
TogglePanel PART_Wrapper            .toggle-wrapper (display: flex)
├─ StackPanel PART_Label            .label-text-wrapper (overflow: hidden)
│  ├─ ContentPresenter PART_LabelContent   <slot>
│  └─ StackPanel PART_Bottom        .toggle-bottom
│     ├─ TextBlock PART_HelperText
│     └─ TextBlock PART_ErrorText
└─ Panel PART_NativeWrapper         .native-wrapper
   └─ Border PART_Track             .toggle-icon: the gesture's element
      └─ Panel PART_IconWrapper     .toggle-icon-wrapper: translated when checked
         └─ Border PART_Handle      .toggle-inner: 20 × 20, 3 px over the 14 px track top and bottom
            └─ AvonicIcon PART_SwitchIcon
```

`TogglePanel` is the flex row or column of `.toggle-wrapper`, for the four placements, three justifications and two alignments.
`FlexRow` has no `justify-content` and no column. Its rules, each measured:

- **Row** (start, fixed): label then track. **Row-reverse** (end): track then label. The label shrinks, the track doesn't.
- `justify` null is `space-between`, except for end, whose rule sets `start`. `start` and `end` are the inline start and end,
  not the flex line's, so end placement packs at the left in LTR: measured.
- **Column** (stacked): the label measures with `max-width: calc(100% / 0.75)` and is scaled to 0.75 by the theme, which doesn't
  change its layout box, as a CSS transform doesn't. Cross alignment is centre unless `alignment` is start.
- RTL mirrors it, as Avalonia mirrors any panel.

### Behaviour, in the control

`AvonicToggle : ContentControl, IItemControl`. A click is `NativeButton` with no keys, as for the chip: Ionic's `onClick` is on
the host, a `<div>`, so the plain DOM click on release. Space is `OnKeyDown`. The gesture is created on `PART_Track` when the
template is applied and the toggle is in the tree, destroyed when it leaves, and enabled with `IsEffectivelyEnabled`. A drag
takes Avalonia's pointer capture, which ends `NativeButton`'s press, so the release after a drag doesn't click; Ionic's click
there comes and is dropped by `lastDrag`. Both give no second flip. `lastDrag` reads `AvonicConfig.Now`, so tests run on
`TestClock`.

Pseudoclasses, from Ionic's host classes: `:checked`, `:activated` (`toggle-activated`, which md doesn't style),
`:in-item`, `:justify-*`, `:alignment-*`, `:label-placement-*`, `:label-text-wrapper-hidden`, and `:ion-color` from `ColorPalette`.
RTL is the toggle's `FlowDirection`.

### RTL: a deliberate difference

Ionic's `isRTL` reads only the toggle's own `dir` and the document's. A `dir="rtl"` on an ancestor between them mirrors the layout
(CSS direction inherits) but leaves the handle and the drag in LTR: measured, the checked handle sits at the right of the
track, and a drag left doesn't flip it. Avalonia's `FlowDirection` inherits and can't say where it came from, so here RTL is RTL
throughout, as Ionic is with `dir` on the document or the toggle.

### Item

The item already counts a toggle as an input (`IItemControl`, `ItemControlType.Toggle`), and forwards a tap on its padding to the
first interactive control, which calls `OnItemTap`, Ionic's `click()`. `::slotted(ion-toggle[slot=start|end]) { margin: 0 }`
belongs in `Styles/Item.axaml`, which is the item's file, so it's in `Styles/Toggle.axaml` for now, after the item's rule for any
child in a slot, by ordinal file order. It's under "Proposed for shared files".

## Dependencies

`ion-item` (#6), `ion-label` (#7), the gesture utility (#4), `ion-icon`: all merged.
