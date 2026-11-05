# ion-item — Plan

The first control with a real styled template, so this PR also settles how a host control is done: where
`::slotted` rules live, how `currentColor`, `em` and fallback variables map, and how later controls talk to the
item without editing it. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-item`.

### Props (14)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `button` | `Button` (`bool`) | |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `detail` | `Detail` (`bool?`) | md: shown only when `true` |
| [x] | `detailIcon` | `DetailIcon` (`string`, `chevron-forward`) | A name or a source, as `AvonicIcon.Icon` takes |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [x] | `lines` | `Lines` (`ItemLines?`) | `Full`, `Inset`, `None`; null is the default |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `href` | — | Out of scope: router. Use `Button` and the `Click` extension |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [ ] | `type` | — | Not applicable: HTML form |

### Slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `native` | `PART_Native` (`Border`) |
| [x] | `inner` | `PART_Inner` (`Border`) |
| [x] | `container` | `PART_Container` (`ContentPresenter`) |
| [x] | `detail-icon` | `PART_DetailIcon` (`AvonicIcon`) |

No events or methods. Other parts are `PART_Start`, `PART_End`, `PART_Overlay` (`.item-native::after`), `PART_Ripple`
and `PART_Layout`, which holds `.item-native`'s padding. It can't go on `PART_Native`, because the overlay and the ripple
cover the padding.

### CSS variables (31)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Item.Background` | brush | Unset → `Avonic.ItemBackgroundBrush` |
| [x] | `--background-activated` | `Avonic.Item.BackgroundActivated` | brush | transparent |
| [x] | `--background-activated-opacity` | `Avonic.Item.BackgroundActivatedOpacity` | `double` | 0 |
| [x] | `--background-focused` | `Avonic.Item.BackgroundFocused` | brush | Unset → current colour |
| [x] | `--background-focused-opacity` | `Avonic.Item.BackgroundFocusedOpacity` | `double` | 0.12 |
| [x] | `--background-hover` | `Avonic.Item.BackgroundHover` | brush | Unset → current colour |
| [x] | `--background-hover-opacity` | `Avonic.Item.BackgroundHoverOpacity` | `double` | 0.04 |
| [x] | `--border-color` | `Avonic.Item.BorderColor` | brush | Unset → `Avonic.ItemBorderColorBrush` |
| [x] | `--border-radius` | `Avonic.Item.BorderRadius` | `CornerRadius` | 0 |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid |
| [x] | `--border-width` | `Avonic.Item.BorderWidth` | `Thickness` | 0 |
| [x] | `--color` | `Avonic.Item.Color` | brush | Unset → `Avonic.ItemColorBrush` |
| [x] | `--color-activated` | `Avonic.Item.ColorActivated` | brush | Unset → `--color` |
| [x] | `--color-focused` | `Avonic.Item.ColorFocused` | brush | Unset → `--color` |
| [x] | `--color-hover` | `Avonic.Item.ColorHover` | brush | Unset → `--color` |
| [x] | `--detail-icon-color` | `Avonic.Item.DetailIconColor` | brush | Unset → current colour (`initial`, measured) |
| [x] | `--detail-icon-font-size` | `Avonic.Item.DetailIconFontSize` | `double`, in em | 1.25 |
| [x] | `--detail-icon-opacity` | `Avonic.Item.DetailIconOpacity` | `double` | 0.25 |
| [x] | `--inner-border-width` | `Avonic.Item.InnerBorderWidth` | `Thickness` | `0,0,0,1` |
| [x] | `--inner-box-shadow` | `Avonic.Item.InnerBoxShadow` | `BoxShadows` | none |
| [x] | `--inner-padding-top`, `-bottom`, `-start` | `Avonic.Item.InnerPaddingTop`, `…Bottom`, `…Start` | `double` | 0 |
| [x] | `--inner-padding-end` | `Avonic.Item.InnerPaddingEnd` | `double` | 16 |
| [x] | `--min-height` | `Avonic.Item.MinHeight` | `double` | 48 |
| [x] | `--padding-top`, `-bottom`, `-end` | `Avonic.Item.PaddingTop`, `…Bottom`, `…End` | `double` | 0 |
| [x] | `--padding-start` | `Avonic.Item.PaddingStart` | `double` | 16 |
| [x] | `--ripple-color` | `Avonic.Item.RippleColor` | brush | Unset → current colour |
| [x] | `--transition` | `Avonic.Item.Transition` | `Transitions` | opacity and background, 15 ms linear |

New global tokens, only read here: `--ion-item-color` → `Avonic.ItemColorBrush`, `--ion-item-border-color` →
`Avonic.ItemBorderColorBrush`.

`--ion-safe-area-*` is added to the padding in Ionic. It's zero outside a notched device and isn't defined yet. It
belongs with `ion-content` (#11), so it's deferred.

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | `Button` makes the item activatable: `:activated`, a ripple, hover and focus states, a `Hand` cursor | `isClickable`, `canActivate` |
| 2 | A plain item has none of those | measured: `DIV`, no ripple, no hover overlay |
| 3 | A tap on a `Button` item raises `Click`; on a plain or disabled one it doesn't | Extension; measured |
| 4 | A tap on a child inside a `Button` item also raises `Click` | measured: bubbled from `inner-btn` |
| 5 | Enter (on press) and Space (on release) raise `Click` when the item has focus | native `<button>`; measured |
| 6 | A `Button` item is a tab stop; a plain or disabled one isn't | measured tab order |
| 7 | Only `Detail="True"` shows the detail icon, even on a `Button` item | `showDetail`, md; measured |
| 8 | `DetailIcon` changes it; the default flips in RTL | measured |
| 9 | Detail icon: 1.25 em, opacity 0.25, the current colour, 8 px before, −6 px after | measured: 20×20 at x=360 |
| 10 | `Lines`: default and inset draw the inner border, full the outer, none neither | measured; `:lines-*` |
| 11 | Full lines make the item 49 high, as the outer border adds to a 48 inner | measured |
| 12 | `Color` paints base and contrast, and borders use shade | measured `rgb(197, 0, 15)` |
| 13 | An unknown `Color`: transparent background, current colour for borders | measured `badcolor` |
| 14 | No `Color` in a coloured ancestor keeps every default | colour system rule |
| 15 | Hover: the overlay at 0.04, with the mouse only | `@media (any-hover: hover)`; measured |
| 16 | Keyboard focus: the overlay at 0.12, beating hover. A press clears it | measured; `focus-visible.js` |
| 17 | Pressing with the mouse keeps the hover overlay; `:activated` adds nothing by default | measured |
| 18 | On `:ion-color`, the overlay and the state colours use contrast | measured |
| 19 | Disabled: opacity 0.3, no input, no tab stop | measured |
| 20 | Focus inside the item shows its focus state if the item is focusable, but a plain nested `Button` doesn't make it so | measured: `btn-nested`, `plain-nested` |
| 21 | `multipleInputs`, `isFocusable`, `hasCover`, `canActivate` follow the slotted controls, and a slot change updates them | `setMultipleInputs` etc. |
| 22 | A tap on the padding forwards to the first enabled interactive child, unless there are multiple inputs | `getFirstInteractive`, `onClick` |
| 23 | A tap on a slotted child doesn't forward | `clickedWithinShadowRoot` |
| 24 | `:item-control-needs-pointer-cursor` unless the first interactive is an input or textarea | `firstInteractiveNeedsPointerCursor` |
| 25 | `IonStyle` from a descendant sets `:item-<key>`, per child type, and stops there | `itemStyle` |
| 26 | `:item-interactive` swaps the borders; `:item-label-stacked`/`-floating` make the minimum 55 and align to the top | `item.md.css` |
| 27 | `:item-interactive-disabled` without multiple inputs takes no input | `item.md.css` |
| 28 | Box layout: start outside the inner border, end and detail inside it, all vertically centred | measured rects |
| 29 | Long text in the default slot wraps and grows the item | measured `longtext` |
| 30 | Font size 16; a slotted icon is 1.5 em, 54% text colour, or contrast on `:ion-color` | measured: 24, and 30 at 20 px |
| 31 | Slot margins: start 16 after, end 16 before. Slotted icons: start 32 after, end 16 before, 12 above and below | measured |
| 32 | An icon nested deeper than the slot, or in the default slot's margins, doesn't get the `[slot]` rules | `::slotted` is direct only |
| 33 | RTL mirrors the padding and slots | measured |
| 34 | Overlay and ripple clip to `Avonic.Item.BorderRadius` | `overflow: inherit` |
| 35 | Automation: a `Button` item is a button that can be invoked; a disabled one reports it | native `<button>`; `aria-disabled` |

## Design

### Host control pattern

**The default slot is a `TemplatedControl` property, not `ContentControl`.** All three slots then work the same way,
as plain `ContentPresenter`s. `ContentControl`'s named presenter parents its child to the host, which would make
`::slotted` selectors differ between slots.

**`::slotted` needs a global style.** Styles inside a `ControlTheme` only reach the control and its template, so they
can't touch slotted content. A new discovered folder, `src/Avonic.Themes/Styles/*.axaml`, holds `<Styles>` files, one
per control:

```xml
<!-- ::slotted(ion-icon[slot=start]) -->
<Style Selector="c|AvonicItem /template/ ContentPresenter#PART_Start > c|AvonicIcon">
```

`>` follows the logical parent, which is the presenter, so this matches direct children only, as `::slotted` does.
A spike confirmed that a nested icon doesn't match. This is a small change to `AvonicTheme` and the auto-discovery
docs, and later controls need it too, because rules like `::slotted(ion-avatar)` go in their own file.

**Finding slotted controls.** A presenter's `ChildProperty` change is `slotchange`. The presenter holds the child's
logical parent, so the host's logical descendants don't include it. The item scans each presenter's `Child`.

### CSS values

`Behaviors/Css.cs` holds four small pieces, and later controls will need all of them:

| Piece | CSS | How |
|---|---|---|
| `CssUnset` | A variable that isn't set | The default value of a token with a fallback |
| `Css.Var` | `var(--a, var(--b, c))` | Multi-value converter: the first value that's set |
| `Css.Em` | `1.25em` | Multi-value converter: font size × factor |
| `Css.Box` | Four `--padding-*` | Multi-value converter: start, top, end, bottom → `Thickness` |

A spike confirmed `{DynamicResource}` works live inside a `MultiBinding`:

```xml
<!-- --color-hover: var(--color), and --color: var(--ion-item-color, ...) -->
<Setter Property="Foreground">
    <MultiBinding Converter="{x:Static b:Css.Var}">
        <DynamicResource ResourceKey="Avonic.Item.ColorHover" />
        <DynamicResource ResourceKey="Avonic.Item.Color" />
        <DynamicResource ResourceKey="Avonic.ItemColorBrush" />
    </MultiBinding>
</Setter>
```

This solves what the colour system's README lists as a limit, for a control's own tokens: overriding
`Avonic.Item.Color` now moves the hover colour too. `currentColor` is the host's `Foreground` as the last value. The
host's `Foreground` is what CSS calls its `color`, including the state colours.

### Focus and hover

**`Behaviors/FocusVisible.cs`** is Ionic's `focus-visible` utility, as `Activatable` is `tap-click`. A surface with
`FocusVisible.IsEnabled` (class `ion-focusable`) gets `:ion-focused` when focus reaches it, or anything inside it, by
keyboard. A press anywhere, or a key outside Ionic's list, clears it.

**`Activatable` gains `:ion-hover`,** set for mouse and pen only. `:pointerover` also applies during a touch, and on a
phone that would flash the hover overlay on every press, where Ionic's `any-hover` media query turns it off.

### How later controls talk to the item

A control's PR doesn't edit another control, and these already exist in Ionic's design as internals:

- **`IItemControl`** marks a control the item needs to know about, replacing Ionic's tag-name queries. `ItemControlType`
  uses Ionic's names, and the item holds Ionic's lists: covers, inputs, clickables, first-interactive. Avalonia's
  `Button` counts as a clickable without implementing it.
- **`IonStyle`** is Ionic's internal `ionStyle` event, as a routed event in `Behaviors`. `ion-label` and `ion-select`
  raise it, and `ion-toolbar` consumes it too.

Tests use stub controls for both. The first real users are #7, #20–#25 and #45–#46.

### Extensions

- **`Click`**, a routed event. A native `<button>` gives Ionic this for free, and Avonic's item has no other way to
  be tapped. It's not named `IonClick`, because Ionic has no such member.

`Command` isn't included. #10 is where it's decided for buttons, and this control should follow it.

## Deliberate differences

| Difference | Why |
|---|---|
| `href`, `download`, `rel`, `target`, router props and `type` aren't declared | Each would be a dead prop |
| One child per slot | Mapping rule. Wrap several in a panel, as with any Avalonia content. They then don't get `::slotted` rules |
| An explicit `Lines` beats a token | In CSS, a user's `--inner-border-width` beats `:host(.item-lines-none)`. A token here is a default across the app, so explicit `Lines="None"` should win |
| `--detail-icon-font-size` is a factor, not a CSS length | So it stays relative to the item, as `1.25em` is |
| Plain programmatic `Focus()` doesn't show `:ion-focused` | Ionic tracks a global keyboard mode, which starts on. Avalonia reports how focus arrived, which is more exact |
| Keyboard activation of a nested control doesn't raise the item's `Click` | In a browser the click bubbles. Pointer taps do match. A button nested in a button item is rare |
| `Click` is only raised by `Button` items | On the web any element gets `click`. Use `Tapped` for the others |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-label)`, `ion-note`, `ion-avatar`, `ion-thumbnail`, `ion-button`, `.button-small`, `ion-toggle`, `ion-reorder` rules | Those controls, in their own `Styles/` file. Values are in `item.md.css` |
| `.item-has-focus` rules, which target labels | #7 |
| `in-list`, `role="listitem"`, and lines from `ion-list` | #9. `:lines-default` is there for it to target. `item.md.css` has no `in-list` rule |
| `ionColor` | Nothing reads it: `item-label-color` is emitted by `label.js`, and no stylesheet uses it |
| `item-has-start-slot`, `item-rtl` | No md rule uses them |
| `.item-multi-line` | Nothing emits it |
| `flex-direction: column` for stacked labels | One child per slot, so it has no effect |

## Dependencies

`ion-icon` and `ion-ripple-effect`, both done. `Activatable` and `ColorPalette` exist.
