# ion-label — Plan

A small API over a lot of CSS: nearly everything `ion-label` does depends on the `ion-item` around it. So this PR is
mostly how a child control's look follows its host, which `ion-note`, `ion-text` and the form controls will reuse.
Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-label`.

### Props (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `position` | `Position` (`LabelPosition?`) | `Fixed`, `Stacked`, `Floating`; null is the default |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods

No slots beyond the default one (`Content`, `ContentTemplate`), no parts, no public events, no methods.

### CSS variables (1)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--color` | `Avonic.Label.Color` | brush | Unset → inherits. Only read inside an item, as in Ionic |

### Internal events

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionStyle` | `IonStyle.IonStyleEvent` | `{ label: true, label-<position>: true }` |
| [ ] | `ionColor` | — | Measured: it adds `item-label-color` and `ion-color-danger` to the item, and background, text, border and a slotted icon's colour all stay the same. No stylesheet reads `item-label-color` |

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Outside an item, a label has no opinions: it inherits its text colour and size, and `Avonic.Label.Color` and `Position` do nothing | `:host-context(.item)`; measured `alone`, `alone-var`, `alone-stacked` |
| 2 | `Color` paints the text, in or out of an item | `:host(.ion-color)`; measured |
| 3 | An unknown `Color` inherits, or shows a coloured item's base, because `--ion-color-base` is inherited | measured `alone-bad`, `bad`, `colored3` (blue on blue) |
| 4 | In an item: `Avonic.Label.Color` sets the text, the item's colour is the default, `Color` beats it, and the token font applies | measured `var`, `var2`, `colored` |
| 5 | The label raises `IonStyle` when it attaches and when `Position` changes, so the item gets `:item-label` and `:item-label-<position>` | `emitStyle`; measured `chg` |
| 6 | Not inside a range | `inRange` |
| 7 | Margins in an item: default slot `0 10`, start `0 10 16 10`, end `16 10 0 10`, or `16 7 0 7` beside a stacked or floating label, and no end margin-start in a toggle or radio item. Nested deeper, none | `::slotted(ion-label)`; measured `start`, `end`, `ea` |
| 8 | Long text wraps and grows the item | measured `long`: 78 high, three 19 px lines (in Roboto, since #100) |
| 9 | `.ion-text-nowrap`: one line, an ellipsis, clipped, including `h2` and `p` inside | measured `nowrap`, `nw-h2`, `nw-p` |
| 10 | `.ion-text-wrap`: line height 1.5 of each element's own size, without clipping, except where `h1`–`h6` and `p` set their own | measured `wrap`, `wrap-h` |
| 11 | `h1`–`h6` and `p`: sizes 24, 16, 14, 14, 14, 14, 14; weight normal; margins, and line heights 28.8, 19.2, normal, 20 | measured `heads` |
| 12 | `p` is `#666666` as a direct child, the inherited colour nested deeper | `::slotted(p)`; measured `p`, `n-p` |
| 13 | `p` inherits in a coloured item, including a coloured label's colour, but stays `#666666` in a coloured label in a plain item | `.in-item-color`; measured `colored-p`, `colored2-p`, `danger-p` |
| 14 | Fixed: 100 wide, at the start, wrapping | measured `fixed`, `fixedlong` |
| 15 | Stacked: no margin, top-start origin, `translateY(50%) scale(0.75)`, with colour easing over 150 ms | measured `stacked-only`: `matrix(0.75, 0, 0, 0.75, 0, 27)` |
| 16 | A stacked or floating label fills its slot's height, in any slot, which is what its percentages are of. Other content stays at its own height at the top | `flex: 1` in a column, `align-self: stretch`; measured 54 with no sibling, 30 with one, `ec` 54 in the end slot, `dc` 18 |
| 17 | Floating: `translateY(96%)`, with colour and transform easing over 150 ms. A change of size moves it at once | measured `floating`; the computed value stays a percentage |
| 18 | Floating rises to the stacked transform on `.ion-focused`, `item-has-focus`, `item-has-value`, and `item-has-placeholder` unless `item-input` | measured `l-f-f`, `l-hf-f`, `l-hv-f`, `l-hp-f`, `l-hpi-f` |
| 19 | No transitions for the first 1000 ms if floating when it loaded, and only then | `noAnimate`; measured `true/0s` until 1108 ms; `chg` animates |
| 20 | Stacked or floating with focus: primary colour, beating `Avonic.Label.Color`. In a coloured item: contrast. In an unknown colour: inherits. A label's own `Color` wins, and a plain or fixed label doesn't change | measured `l-f-*`, `l-fc-s`, `l-fcb-s`, `l-f-sc`, `l-f-v` |
| 21 | `.ion-invalid.ion-touched` on the item: stacked or floating inherits, even focused or with `Avonic.Label.Color`, because `--highlight-color-invalid` isn't set on an item | measured `l-inv`, `l-invf`, `l-invf2` |
| 22 | `item-interactive-disabled` without `item-multiple-inputs`: opacity 0.3, no input, default cursor | `label.md.css`; measured `l-id` |
| 23 | `item-input`: max 200, no input, so a tap goes through to the item. A stacked or floating label has no maximum | `label.md.css`; measured `l-inp`, `l-hpi-f` |
| 24 | `item-textarea`: at the top, at its own height, but stacked and floating still fill. `item-skeleton-text`: clipped | measured `l-ta`, `l-sk`, and a stacked one: `matrix(0.75, 0, 0, 0.75, 0, 27)` |
| 25 | A tap on a label doesn't forward to the item's first control, even on its blank part, but a tap on the padding does | measured: `toggle: on label blank` leaves it off |
| 26 | A tap on a label in a button item clicks the item | measured |
| 27 | RTL: stacked and floating shrink towards the right, and fixed sits at the right | `.label-rtl`; measured with `dir` on the document: 105.5 to 374, and x = 274 |
| 28 | Automation: a button item's name includes its label's text | native `<button>` |

## Design

**`AvonicLabel` is a `ContentControl`.** `AvonicItem` isn't, because it has three slots that have to behave alike. A
label has one, and its host makes the content a logical child, so an item's automation name finds the text through
it, and `c|AvonicLabel > TextBlock.p` is `::slotted(p)` with no `/template/`. The whole box takes input, because a tap
on its blank part hits the label in Ionic, so the stub's default `Background` is transparent.

**The look is all in `Styles/Label.axaml` (but its default, start and end slot margins, which are in `Styles/Item.axaml` since #88), with no control theme in `Avonic.Themes`.** Nearly every rule depends on an
ancestor, and a `ControlTheme` can't select by ancestor. The rest can't go in a theme either, because `Styles` beat a
`ControlTheme`, and `:host(.ion-color)` has to beat `:host-context(.item)`. One file in CSS order keeps that visible.
The stub holds the template, and `Design.PreviewWith` goes in the `Styles` file.

**`:host-context(.foo)` is a descendant selector:** `c|AvonicItem:foo c|AvonicLabel`. A descendant selector follows the
logical parent, and it crosses the item's template, as `:host-context` does at any depth. New mapping row.

**A class the app sets is a style class of the same name:** `.ion-text-wrap`, `.ion-text-nowrap`, `.ion-invalid`,
`.ion-touched`. New mapping row. These are Ionic's own names, and they're what an app already writes.

**`h1`–`h6` and `p` are style classes on a `TextBlock`:** `<TextBlock Classes="h2" Text="..." />`. Avalonia has no
such elements. New mapping row. `ion-text` (#8) can reuse this.

**A slot holds one child, so a panel directly inside a slot stands for the slot.** `<ion-label><h2/><p/></ion-label>`
has to become a panel holding two `TextBlock`s, which would otherwise make the `p` "nested deeper" and lose its grey. So
`::slotted(p)` also matches `> :is(Panel) > TextBlock.p`, and `::slotted(ion-label)` also matches a label in a panel
directly in the item's slot. Any deeper still doesn't match, as in Ionic.

**`color: inherit` is a binding to `$parent[Control].(TextElement.Foreground)`,** the last value of a `Css.Var`. A binding
that yields nothing gives the property's default, not the inherited value, and this is what the colour system's README
lists as a limit. This control needs it because a label is often inside a coloured item.

**`white-space` and `line-height` inherit, and so do `TextBlock.TextWrapping`, `TextBlock.TextTrimming` and
`TextBlock.LineHeight`.** They're set on the label, so anything inside it follows, as in CSS.

**No rule for a `TextBlock` uses a descendant selector.** A rule for a `TextBlock` is checked for every `TextBlock` in the
app, and a descendant selector then walks up its ancestors, in an app with no labels too. Measured on this PC, attaching
5,000 `TextBlock`s at a depth of 20, alternating with and without so warm-up doesn't count: `::slotted(*) h1` translated
literally, as nine selectors, took 200 ms at the median against 177 ms, about 13%, or 5 µs per `TextBlock`. The styles
here took 205 ms with and 216 ms without, so no measurable cost. It's a judgement call for a rare case, made because every
`TextBlock` pays and later controls would add to it. So they use `c|AvonicLabel > TextBlock.h1` and
`c|AvonicLabel > :is(Panel) > TextBlock.h1`.

**`translateY(50%)` is `Css.TranslateY`, read by `Css.TranslateScale` with `Bounds`.** Avalonia has no percentage
translate, and `Bounds` is what CSS's percentage is of. The theme eases `Css.TranslateY` and `Css.Scale`, not the transform:
in CSS the computed value stays a percentage, so a rotation or a sibling's size moves the label at once, and only a
change of state eases. Measured mid-flight, stacked to floating at 0.8647 and 38.39 is each function interpolated on its
own, which this is too. A `RenderTransform` doesn't affect layout, as a CSS transform doesn't. `FlowDirection` mirrors
the origin, so RTL needs no rule of its own.

**A stacked or floating label fills the height.** The item's theme aligns its slots to the top for these, which is right
for other content, but a label's `flex: 1` and `align-self: stretch` fill the slot. Since #71, `FlexSlot` honours the label's own alignment, so
anything else stays at the top with no rule for it. A
panel stays stretched, because it stands for the slot. With an input beside it, use `Grid RowDefinitions="*,Auto"`, which is
what `flex: 1` beside a fixed-height sibling is.

## Deliberate differences

| Difference | Why |
|---|---|
| A label outside an item fills the width, where Ionic's is inline | Avalonia has no inline flow for controls. Set `HorizontalAlignment` to change it. Text looks the same |
| Two labels in a slot don't share the width by themselves | One child per slot. A `Grid ColumnDefinitions="*,*"` does it, measured 179 each |
| With two labels in an item, the last to report wins, on load and on a change of `Position` | The same as Ionic, which keeps these per tag name. Ionic's order on load depends on what loaded first (measured `a` 55 high, `b` 48). Avonic's is the order it measures the slots: start, end, default, so a label in the default slot wins |
| Margins between two headings add up to 4, where CSS collapses them to 2 | Avalonia has no margin collapsing. `h1` or `h2` followed by `p`, the usual pairs, are 2 in both |
| `item-input`'s label is as wide as its text up to 200, where Ionic's is `min-content` (measured 58, one word per line) | Avalonia has no `min-content`. Nothing in Ionic 8 raises `input`, so only a custom child reaches this |
| A label moved to another item tells the new item | Ionic only emits on first load, so measured `b` gets nothing. Recycled containers are normal in Avalonia. As in Ionic, the old item keeps its classes |
| A right-to-left subtree acts like a right-to-left document, so a stacked label shrinks to the right | Ionic's `label-rtl` only reads `document.dir`, so with `dir` on a `div`, measured, its text ends at 284 and leaves a gap. `FlowDirection` has no separate document level |
| `h1`–`h6` and `p` get their rules directly in the label or in a panel directly in it, where Ionic's reach any depth (measured `n-h2`) | See the design: a descendant selector adds about 13% to attaching every `TextBlock` in the app. Set the values yourself deeper down |
| With `ion-text-wrap`, a `TextBlock` nested deeper than a panel with its own `FontSize` gets 1.5 of the label's size, not its own, and can clip | `TextBlock.LineHeight` inherits as a size. Same reason |
| A label detached in its first second doesn't stay stuck without transitions | Ionic cancels the timer and never clears the state, which is an accident |

## Not in this PR

| What | Goes with |
|---|---|
| The default text size outside an item: Avalonia's, where a web page's is 16 | It affects everything, so it's its own decision. Already raised at `ion-icon` |
| `ionColor` | Nothing visible reads it; see above |
| `::slotted(*) h1`'s `overflow: inherit` nested inside a `div` | Clipping happens at the label either way |
| `.label-rtl` | `FlowDirection` does it |

## Dependencies

`ion-item`, done: `IonStyle`, `IItemControl`, `Css`. `ion-range` doesn't exist yet, so tests use a stub with
`ItemControlType.Range`.
