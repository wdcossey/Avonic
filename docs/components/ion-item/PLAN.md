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
| 3 | A tap on a `Button` item raises `Click`, on release, every time; on a plain or disabled one it doesn't | Extension; measured |
| 4 | A tap on a child inside a `Button` item also raises `Click` | measured: bubbled from `inner-btn` |
| 5 | Enter (on press) and Space (on release) act as a tap when the item has focus, including forwarding | native `<button>`; measured |
| 6 | A `Button` item is a tab stop; a plain or disabled one isn't | measured tab order |
| 7 | Only `Detail="True"` shows the detail icon, even on a `Button` item | `showDetail`, md; measured |
| 8 | `DetailIcon` changes it; it flips in RTL by its name, as `arrow-forward` and `chevron-down` do | measured |
| 9 | Detail icon: 1.25 em, opacity 0.25, the current colour, 8 px before, −6 px after | measured: 20×20 at x=360 |
| 10 | `Lines`: default and inset draw the inner border, full the outer, none neither | measured; `:lines-*` |
| 11 | Full lines make the item 49 high, as the outer border adds to a 48 inner | measured |
| 12 | `Color` paints base and contrast, and borders use shade | measured `rgb(197, 0, 15)` |
| 13 | An unknown `Color`: transparent background, text in `--color`, current colour for borders | measured `badcolor`; `var()` invalid at computed time |
| 14 | No `Color` in a coloured ancestor keeps every default | colour system rule |
| 15 | Hover: the overlay at 0.04, with the mouse only | `@media (any-hover: hover)`; measured |
| 16 | Keyboard focus: the overlay at 0.12, beating hover, with no outline or adorner. A press clears it | measured; `focus-visible.js`; `outline: none` |
| 17 | Pressing with the mouse keeps the hover overlay; `:activated` adds nothing by default | measured |
| 18 | On `:ion-color`, the overlay and the state colours use contrast | measured |
| 19 | Disabled: opacity 0.3, no input, no tab stop | measured |
| 20 | Focus inside the item shows its focus state if the item is focusable, but a plain nested `Button` doesn't make it so | measured: `btn-nested`, `plain-nested` |
| 21 | `multipleInputs`, `isFocusable`, `hasCover`, `canActivate` follow the slotted controls, and a slot change updates them | `setMultipleInputs` etc. |
| 22 | A tap on the padding forwards to the first enabled interactive child, unless there are multiple inputs. Disabled through an ancestor counts as disabled | `getFirstInteractive`, `onClick` |
| 23 | A tap on a slotted child doesn't forward | `clickedWithinShadowRoot` |
| 24 | `:item-control-needs-pointer-cursor` unless the first interactive is an input or textarea | `firstInteractiveNeedsPointerCursor` |
| 25 | `IonStyle` from a descendant sets `:item-<key>`, per child type, and stops there | `itemStyle` |
| 26 | `:item-interactive` swaps the borders; `:item-label-stacked`/`-floating` make the minimum 55, align to the top including the detail icon, and give end content 7 above and below; `:item-textarea` stretches the slots | `item.md.css`; measured |
| 27 | `:item-interactive-disabled` without multiple inputs takes no input | `item.md.css` |
| 28 | Box layout: start outside the inner border, end and detail inside it, all vertically centred | measured rects |
| 29 | Long text in the default slot wraps and grows the item | measured `longtext` |
| 30 | Font size 16; a slotted icon is 1.5 em, 54% text colour, or contrast on `:ion-color` | measured: 24, and 30 at 20 px |
| 31 | Slot margins: start 16 after, end 16 before. Slotted icons: start 32 after, end 16 before, 12 above and below | measured |
| 32 | An icon nested deeper than the slot, or in the default slot's margins, doesn't get the `[slot]` rules | `::slotted` is direct only |
| 33 | RTL mirrors the padding and slots | measured |
| 34 | Overlay and ripple clip to `Avonic.Item.BorderRadius` | `overflow: inherit` |
| 35 | Automation: a `Button` item is a button that can be invoked, and a plain one can't. Disabled and `:item-interactive-disabled` report it | native `<button>`; `aria-disabled`; `onClick` only when clickable |

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
docs, and later controls need it too. (Rules like `::slotted(ion-avatar)` were to go in their own control's file. Those
that set `Margin` are in `Styles/Item.axaml` instead, after the rule for any child in a slot, since #88: see the README's
"Slotted styles".)

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

## The flex layout (#71)

The start and end slots were `Auto` columns, so long content was cut off, and a short start label was as wide as its text.
`item.md.css` has two nested flex rows, and this recreates them.

```
.item-native  (flex row, overflow: hidden, align-items: center)
├── slot[start]        display: contents, so its children are the row's items
└── .item-inner        flex: 1, overflow: hidden, align-self: stretch, padding-end 16
    ├── .input-wrapper flex: 1, overflow: hidden, align-self: stretch
    ├── slot[end]
    └── ion-icon
```

Everything else is `flex: 0 1 auto`, except `::slotted(ion-label:not([slot=end])) { flex: 1; width: min-content; max-width: 100% }`.
A `flex: 1` box can't go below its own padding, so `.item-inner` is never under 16.

### What Ionic does

Chromium, md, 390 wide. `W` is the width of the box. The page is [`ref/layout/cases.html`](ref/layout/cases.html), and every box in it is in
[`cases.txt`](ref/layout/cases.txt), from `node tools/ionic-ref/boxes.js docs/components/ion-item/ref/layout/cases.html`. Since #100 it's
measured with the Roboto files the tests use, which changed the numbers that depend on text: F2, F7, F8, F10, F12, F15, F21 and F22.

| # | Content | Measured |
|---|---|---|
| F1 | Label in start, label in default | Start 171 at x=16, inner x=203 W=187, default 171. `(374 - 16 - 16) / 2` |
| F2 | The same, with a long start label | Start 171 wraps, row 96 high |
| F3 | Label in start only | Still 171, with an empty inner |
| F4 | `position="fixed"` in start | 100: `flex: 0 0 100px` wins over `flex: 1` |
| F5 | `position="stacked"` in start | 179 before its scale of 0.75, with no margin to take off, and inner x=195 |
| F6 | Long label in end | 342 at x=32, wraps. Default is squeezed to 0 |
| F7 | A label squeezed to 0 | One character a line, 7 lines for "Default", so the row is 154 high. `max-width: 100%` is what takes its `min-content` away |
| F8 | Plain text, or a note, squeezed to 0 | It keeps its `min-content`, so one word a line: 58 high for plain text, 55 with a note |
| F9 | Long start and end labels | Start 171, end 155 |
| F10 | A word wider than half the row, in a start label | Start 236.6 (`min-content`), inner takes what's left |
| F11 | A word wider than the row, in a start label | Start 374 (`max-width: 100%`), breaks in the word, inner is pushed out |
| F12 | A word wider than the row, in an end label | 677.7, overflowing, clipped by the host |
| F13 | `.ion-text-nowrap` in end | 342, as `overflow: hidden` takes its `min-content` away |
| F14 | Long note in start or end | 342. A label beside it is squeezed to 0 |
| F15 | Long notes in both | Start 342, end its `min-content` of 41.8, clipped. Row 198 high |
| F16 | Icon beside something long, in one row | The icon shrinks with the rest: 15.5 and 14.6 of 24, height unchanged. `contain: strict` means it has no minimum |
| F17 | Detail icon beside a long end label | Label 330 and icon 10 of 20, for the same reason |
| F18 | Plain `span`, `ion-text`, or a `div` in end | 342, wraps |
| F19 | A note beside a tall label | Note at the top, icon centred |
| F20 | RTL | The same, mirrored |
| F21 | Divider: label in start | 30.2, as `::slotted` gives it no `flex` there. A long one is 342 |
| F22 | Divider: long end content | 342. A default label beside it keeps its 44.8 |

### Design

| Piece | What it is |
|---|---|
| `FlexRow` | A `Panel` running the flexbox algorithm for one row: base sizes, then growing or shrinking with `min-content` as the floor, freezing what hits it. `FlexRow.Flex` is CSS's one-number `flex`: 0 is `flex: initial`, and `n` is `flex: n`. On a slot it's read from the slot's child, as a `<slot>` has no box |
| `FlexSlot` | A `ContentPresenter` that stands for a `<slot>`, and is still styled as `ContentPresenter`. It always fills the row. `VerticalContentAlignment` is `align-items`. A child that sets its own `VerticalAlignment` has `align-self`. A panel fills the slot, as it stands for it, except a vertical stack, which is a group of its own |

`min-content` can't come from `Measure`, which clamps `DesiredSize` to what's available, so `FlexRow` works it out from the
one-line sizes it has just measured: a `TextBlock` is its longest word, from a `TextLayout` in `WrapWithOverflow`, something with a
`Width` is that, a panel adds up or takes the largest, and everything else can't shrink.

**What it costs.** A part with `flex: 1` is measured once, and so is content that fits. A start label is measured on one line and again at its share,
and looks for its longest word only if its share is less than one line. A row that overflows does too. 300 items, the median of 7, in Release, headless, on this PC. It's three runs each, on the final code, as these move about:

| Rows | Open the page, `v2` (ms) | Open the page, now (ms) | Change of width, `v2` (ms) | Change of width, now (ms) |
|---|---|---|---|---|
| A label | 320, 308, 338 | 308, 335, 291 | 76, 70, 78 | 60, 67, 71 |
| A label and a short note | 424, 342, 362 | 298, 383, 356 | 72, 68, 75 | 58, 52, 63 |
| Icons and detail | 388, 358, 381 | 396, 386, 355 | 89, 70, 85 | 71, 82, 62 |
| A start label | 349, 365, 394 | 373, 397, 369 | 63, 65, 68 | 87, 99, 92 |
| A note that wraps | 386, 402, 438 | 397, 443, 432 | 6, 18, 18 | 113, 114, 113 |

Opening is the same within what it moves by, and so is a change of width, except for two rows. A start label costs about 40% more, as it's measured
on one line and again at its share. The last row wasn't doing the same thing before, as it didn't wrap: it's about 0.4 ms a row, on a rotation
rather than a frame. Allocation isn't measured separately. `Resolve` doesn't allocate, and an `Item` is made per child per measure.

### Style budget

| | Alternatives |
|---|---|
| `Label.axaml`: stretching the slots, and everything else to the top | -4 |
| `Note.axaml`: `:has-note`, and the rest of a panel | -2 |
| `Item.axaml`: the rest of a panel, centred, whatever's in it | +1 |
| `ItemDivider.axaml`: the same, as a panel fills its slot there too | +1 |
| `Label.axaml`: `flex: 1` in the default slot. The start slot's rule exists already | +1 |
| **From 174 to** | **171** |

### Tidying `Styles/Label.axaml`

`h1` and `h2` line heights follow their own size through `Css.Em`, `Css.TranslateScale` binds `Bounds.Height` rather than all of `Bounds`,
so that moving doesn't run it, and `:position-floating`'s `Css.Scale` of 1 goes, as that's the default.

### Tests

One per row above, in `AvonicItemLayoutTests`, and `FlexRowTests` for the algorithm with sizes that don't depend on a font. The `B8` note
tests stay, as they say what `:has-note` was for. Two set `Top` on the note's neighbour and expected it to be ignored once the note left,
which was the bookkeeping. A local `Top` is now `align-self`, as it is on the web, so those two lost that.

## Shared press handling, `Command` and the rest (#72)

What the first swarm proposed for shared files, which the human accepted. It's a list rather than one feature, so each part is on its own below.

### What Ionic does

Chromium, md, with `ion-app`, from a script in the PR's session. A click is the DOM's `click`.

| # | Input | Measured |
|---|---|---|
| S1 | A finger that moves, on `ion-button`, `ion-item button` and `ion-card button` | Clicks at 0, 5, 10 and 14 px of movement, and not at 16, 20 or 40. The same on all three |
| S2 | A mouse dragged 60 px, on all three | Clicks |
| S3 | Right and middle button, on an item | No click |
| S4 | A native `<button>` or `ion-button` in a `button` item | Both click: `nb,nestN` and `ib,nestI` |
| S5 | Names of a `button` item's native element | `Hello inline words`, from a label with spans. `Text INNER BTN`, with an `ion-button` in it, in the case it's drawn. `Seen`, leaving out `display: none` and `visibility: hidden`. `With icon`. `Lab Not`, with a note. `Aria name` beats them all |
| S6 | Roles | Host in `ion-list`: `listitem`, a `button` one too, with a `button` inside it. Outside a list: `generic` |
| S7 | `position` on a label in a divider, a group and a list header | It applies in all three, as `:host(.label-stacked)` doesn't depend on `.item`. Stacked is `matrix(0.75, 0, 0, 0.75, 0, 15)` of a height of 30 in a divider, 9 of 18 in a group, and 22.5 of 45 in a header. Floating is 28.8 of 30, and 43.2 of 45. Fixed is 100 wide. A label alone is inline, so its transform does nothing |
| S8 | An end-slot `ion-button` beside a stacked or floating label | Margins `16 7 2 7`, y = 7, and `16 4 2 4` beside a plain one |
| S9 | `z-index` on a slotted `ion-button` | 1 |
| S10 | A red item's native element with a 50% blue line of 10, on green | The line is `rgb(127, 0, 128)`: red shows through it, so the background reaches under the border |
| S11 | A second finger on something else, lifting in either order | No click |
| S12 | `display: none` or `visibility: hidden` during the press | No click |
| S13 | A `button` item in a `button` item | `inner,outer`: one click, which reaches each once |
| S14 | Names, with something inside labelled | `Row Delete`, from an icon-only `ion-button` with `aria-label`. `Row`, leaving out `aria-hidden`. `Labelled`, where a label's `aria-label` replaces its text |
| S15 | A finger held for 100 to 1,000 ms | Clicks, in headless Chromium, which opens no context menu. A phone's browser that opens one doesn't click as well, which isn't measured here |

`ion-button`'s plan already measured that a child's `stopPropagation` stops the click, on one tap and on two quick ones.

### One press behaviour

`Behaviors/NativeButton.cs` is what a native `<button>` does, for `AvonicButton`, `AvonicItem` and `AvonicCard`, which had three copies of it and two different answers. It's internal, as nothing outside the library needs it.

| Behaviour | Rule |
|---|---|
| A click | On release, by the left button or a finger, if the press and release both landed on the control and it still holds the pointer. On every press, so a second quick one counts |
| A mouse | Has no movement limit: S2 |
| A finger | Has a limit of 15 px, from S1, which ends the press for good. A constant in the code, as it's what Chromium has, not a look |
| A second finger anywhere, a cancelled press, disabled or hidden during the press | No click: S11, S12, and as `AvonicButton` measured. The window is only watched for fingers during a press |
| A press that opened a context menu | No click. `ContextRequested` that something handled ends it. A long press with no menu still clicks: S15 |
| A child that handles the press | Stops the click, as `stopPropagation` does: `ion-button`'s measurement |
| A stock `Button` inside | Doesn't count as handling it, though it does: it always handles its press, and S4 says both click. A `RepeatButton` does count, as a browser doesn't click on a scrollbar or a spinner |
| A child's `Click` with `Handled` set | Ends the press, so the item around it doesn't click. It's the real `click.stopPropagation()`, and how an app stops a button in a row from tapping the row. For a stock `Button`, and for anything using this, including a `Button` with `ClickMode.Press`, which clicks before the press reaches here |
| One of the same type inside clicks | The outer one doesn't raise `Click` again, as the inner one's has been through its handlers, and a browser has one click: S13. Its `Command` still runs |
| A child that handles `Tapped` | **Doesn't stop it any more.** It used to on an item and a card, for one of two quick taps, which was the recorded gap. `Tapped` isn't what the click comes from now. This is the one change an app could notice |
| A child that handles the press on a card or an item | **Stops it now,** where it didn't. The same rule as a button, as measured |
| Enter and Space | Enter on press and Space on release, only with focus on the control itself |

The item still marks `Tapped` handled after forwarding a tap, for `stopImmediatePropagation`.

A plain item or card doesn't make one, as most rows have nothing to press: an item makes it when it's a `Button` or has a control to forward to, and a card when it's a `Button`. It's 11 handlers, which measured about 15% on opening a page of plain rows.

**What it costs,** on the same benchmark as the flex layout's, on the final code, three runs each:

| Rows | Open the page, `v2` (ms) | Open the page, now (ms) | Change of width, `v2` (ms) | Change of width, now (ms) |
|---|---|---|---|---|
| A label | 298, 284, 287 | 288, 292, 266 | 54, 51, 55 | 70, 59, 63 |
| A label and a short note | 316, 307, 302 | 329, 337, 339 | 42, 42, 42 | 55, 63, 59 |
| Icons and detail | 302, 323, 296 | 379, 338, 339 | 62, 53, 57 | 59, 56, 52 |
| A start label | 356, 346, 348 | 378, 336, 343 | 74, 80, 75 | 89, 82, 82 |
| A note that wraps | 366, 371, 374 | 385, 394, 376 | 104, 100, 103 | 114, 109, 115 |

Plain rows open as they did. Rows with more in them open about 5 to 10% slower, and a change of width is about 10 to 15 ms slower for 300 rows. I don't have a cause for those: nothing in a measure changed, and a row with a note has no `NativeButton`, so it may be the styles. It isn't large, but it's not explained.

`Command` and `CommandParameter` are in it too, as extensions, with one property that the item and the card add as owners. They do nothing without `Button`, and a command that can't execute only disables an item or a card that has it.

### The ripple

`FindRippleEffect` skips a hidden ripple, so a button item with a plain card in it keeps its own. #61 said the card's template order and `ZIndex` could then go. They stay: they also make a card find its own ripple before a visible one in its content, as Ionic looks in its shadow root first.

**In a button item the ripple goes under the content, not over it.** Ionic's is over everything except an `ion-button`, at `z-index: 1` (S9). `ZIndex` in Avalonia only sorts siblings, so a slotted button can't rise above something outside its slot. So it's over nothing. The ripple is the text's colour by default, so text looks the same, and it only shows under an icon or a note, or a `--ripple-color` of another colour.

### Automation

`Behaviors/AccessibleText.cs` reads what's drawn, in visual order: words in inlines, a nested button's text as it's drawn (S5), and no hidden text. Something inside with an `AutomationProperties.Name` counts as that, and something with `AccessibilityView.Raw` doesn't count (S14). The button item, the card and the button use it, and a plain item has no name of its own, as it has no native button. Automation's `Invoke` on an item forwards as a tap does, as a native button's click is what the host forwards. `role="listitem"` moves to the item's peer (S6), so a replaced theme keeps it, and the last rule in `Styles/List.axaml` goes. A button item stays a button: Avalonia has one peer for what Ionic has two of.

### Styles and tokens

| What | How | Evidence |
|---|---|---|
| `IItemHost`, on the item, the divider and the group | Ionic's class `item`. `:host-context(.item)` becomes `:is(c\|IItemHost) c\|AvonicLabel`, which removes the copies in `Styles/ItemDivider.axaml` and `Styles/ItemGroup.axaml`. #59 proposed a pseudoclass, `:item`. That would raise these rules over an app's own plain `AvonicLabel` style, where CSS has the outer style win, and put an activator on every ancestor of every label. A type does neither | The classes in S7 |
| `Position` on a label in a divider, a group and a header | Those rules move to `IItemHost`, and the header's own alternative, as it has no such class. Its presenter becomes a `FlexSlot`, so that a label's `Stretch` reaches it | S7 |
| Icon rules reach an icon in a panel | As #60 listed, except the margins. A panel has the slot's margin already, so the icon's are `0,12,16,12` and `0,12,0,12`, which puts it where one directly in the slot is | A panel stands for the slot |
| End-slot button margin beside a stacked or floating label | `Styles/Item.axaml` says `16,7,2,7` for a button, after the rule for any child | S8 |
| Safe area | `Avonic.SafeArea*` move to `Tokens/Global.axaml`. `Css.SafeAreaBox` adds them at the start, the end or both, swapping sides in RTL. The item, as the issue says, and also the divider and the list header, as their CSS has it too and a row that alone ignored a notch would be noticed | `item.md.css`, `item-divider.md.css`, `list-header.md.css` |
| `Avonic.CardBackgroundBrush` | Unset in `Tokens/Global.axaml`, so a card follows `--ion-item-background`. The dark palette sets it, as Ionic's does | `card.md.css` |
| `BackgroundSizing` on the native border | `OuterBorderEdge`, if a test measures `rgb(127, 0, 128)` with it and not without | S10 |

A panel in a header fills its `FlexSlot`, so what it holds is centred by a rule, as in an item and a divider.

### Style budget

From 171 alternatives to 176, with no limit raised. Icons in a panel add four, an end button beside a stacked label two, the header's panel one and a label's `Position` in a header three. The list's role and the copies in `Styles/ItemDivider.axaml` and `Styles/ItemGroup.axaml` take away five. Because the shared rules are a type, they add no activators.

### Elsewhere

- `AvonicConfig.RequestFrame`, so that `AvonicContent`'s scroll and its `ionScroll` get real frames rather than a 16 ms timer. `TestClock` keeps it at 16 ms.
- `AvonicButton.HalfLeadingConverter` moves to `Css.HalfLeading`.
- The tests' own pixel readers call `Frames.PixelAt`.

### Tests

Each row and rule above has one. In `NativeButtonTests` for the rules, on all three controls where they apply, and in each control's tests for the rest. `Tapped` tests that recorded the old rule change to say the new one.

## Deliberate differences

| Difference | Why |
|---|---|
| A long note or `AvonicText` is as wide as its longest line, where Ionic's box is 342 | They're aligned left, so a tap beside a short one reaches the item. The text is in the same place, and a tap in the gap reaches the item |
| A horizontal `StackPanel` in a slot doesn't wrap what's in it | It gives its children infinite width, whatever it's given. Use a `Grid` with `*,Auto` for a long note and an icon in one slot. Ionic's two `slot="end"` elements have no equivalent, as a slot holds one child |
| A word wider than its box, outside a label, breaks in the word if the box is 64 or wider. In CSS it overflows and is clipped | `TextWrapping.Wrap` breaks it, and looking for `min-content` on every default slot would measure every item twice. Below 64 it does look, as a slot 6 wide would be a row 400 high |
| A child with no height of its own, directly in a panel in a slot, collapses. So a `Grid` holding a background behind text loses the background | The panel stands for the slot, so its children are centred, as `align-items: center` does in CSS. A panel can't say it's one unit rather than several. `VerticalAlignment="Stretch"` on the child, or a `Border` around the `Grid`, gets it back |
| Setting `VerticalAlignment="Stretch"` on a slotted child after it's laid out doesn't move it until something else lays it out | That's also its default, so Avalonia raises no change, and `IsSet` has nothing to tell it. Set it in XAML, or `InvalidateArrange` the slot |
| `PART_Inner` doesn't clip, where `.item-inner` has `overflow: hidden`, so with `Avonic.Item.PaddingEnd` above 0 a word wider than the row draws over that padding, up to the host's edge | `ClipToBounds` on a `Border` clips its own shadow, and CSS's doesn't, so it would take away `--inner-box-shadow`. A word that wide already overflows Ionic's row |
| An icon in a panel keeps its 12 above and below beside a stacked label, where one directly in the slot has 7 | Another two alternatives, for something rare |
| A child that handles the press, around a stock `Button`, doesn't stop the click | The press is handled either way, so the two can't be told apart |
| A `TextBox`'s watermark counts in a button item's name while it's showing, as it's drawn text | An input in a `<button>` isn't valid HTML, so there's nothing to measure |
| A word's width is measured in the `TextBlock`'s own font, so a larger run in its inlines can break at its last letter when it's squeezed down to its longest word | Reading each run would be a layout of its own, for a case this rare |
| A panel in a slot isn't `flex: 1`, even with a label in it | A panel is one item. Ionic's measured `div` is the same (342 wide). Two `slot="start"` elements, where the label would still be `flex: 1`, have no equivalent, as a slot holds one child |
| In a parent that gives no width, such as a horizontal `StackPanel`, an item is one line wide | Measured in an `inline-block`, Ionic's `width: min-content` makes it 168 wide and squeezes it, which isn't something to copy |
| A start label in an `.item-input` is as wide as its text, up to 200. Ionic's is 59 | `width: min-content` with `flex: initial`. Nothing sets `item-input` on `ion-input` any more, measured: a real one leaves the label at 171 |
| `href`, `download`, `rel`, `target`, router props and `type` aren't declared | Each would be a dead prop |
| One child per slot | Mapping rule. Wrap several in a panel, as with any Avalonia content. They then don't get `::slotted` rules |
| An explicit `Lines` beats a token | In CSS, a user's `--inner-border-width` beats `:host(.item-lines-none)`. A token here is a default across the app, so explicit `Lines="None"` should win |
| `--detail-icon-font-size` is a factor, not a CSS length | So it stays relative to the item, as `1.25em` is |
| Plain programmatic `Focus()` doesn't show `:ion-focused` | Ionic tracks a global keyboard mode, which starts on. Avalonia reports how focus arrived, which is more exact |
| Keyboard activation of a nested control doesn't raise the item's `Click` | In a browser the click bubbles. Pointer taps do match. A button nested in a button item is rare |
| The ripple is under a button item's content, where Ionic's is over all of it except an `ion-button` | `ZIndex` only sorts siblings, so a slotted button can't rise over a part outside its slot. It's the text's colour by default, so only an icon, a note or another `--ripple-color` shows the difference |
| `requestAnimationFrame` in a hidden window: nothing fires, so a `ScrollToPoint` waits, as a browser's tab does. Outside a window a frame is a 16 ms timer | Avalonia has no frame to wait for there |
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
