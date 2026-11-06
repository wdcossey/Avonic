# ion-note — Plan

A small grey piece of text, usually a date or a count at the end of an item. It has one CSS variable and no behaviour
of its own, and what matters is how it sits in `ion-item`. Measurements are from real Ionic 8.8.1 in Chromium, md, at
390 px wide. It shares a PR with [`ion-text`](../ion-text/PLAN.md).

## Fidelity checklist

`node tools/ionic-api.js ion-note`.

### Props (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods

No slots beyond the default one (`Content`, `ContentTemplate`), no parts, no events, no methods.

### CSS variables (1)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--color` | `Avonic.Note.Color` | brush | Unset → `Avonic.TextColorStep400Brush`, `#666666` |

`--color` is `var(--ion-color-step-600, var(--ion-text-color-step-400, #666666))`. Avonic has no key for
`--ion-color-step-600`, Ionic's older name, so `Avonic.TextColorStep400` is the global to change.

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | Alone: 14 px, the token font, `#666666`, whatever its parent's colour, size and font are | `note.md.css`; measured `alone`, `alone-inh` |
| 2 | `Color` paints the text, in or out of an item | `:host(.ion-color)`; measured `alone-danger`, `end-color2` |
| 3 | An unknown `Color` inherits, or shows a coloured item's base, because `--ion-color-base` is inherited | measured `alone-bad`, `alone-inh-bad` `rgb(1,2,3)`, `end-color3` blue |
| 4 | `Avonic.Note.Color` sets the text, in or out of an item, and `Color` beats it | measured `alone-var`, `alone-var-c`, `end-var` |
| 5 | A coloured item doesn't change a note: it stays `#666666` | measured `end-color` |
| 6 | Directly in any of an item's slots: 11 px, whatever the item's size is. Nested deeper or in a label: 14 | `::slotted(ion-note)`; measured `def`, `start`, `end`, `sized`, `in-div`, `in-label` |
| 7 | In the start or end slot: 18 above and 10 below. In the default slot: none | `::slotted(ion-note[slot])`; measured `end` 40 high, `def` 12 |
| 8 | At the top of the row, in any slot and however tall the row is, where everything else is centred | `align-self: flex-start`; measured `end-tall`, `start-tall`, `def-tall` at y = 0 in a row of 88 |
| 9 | The item's slot margins apply: start `0 0 16 0`, end `16 0 0 0`, and `16 7 0 7` beside a stacked label | `::slotted([slot=end])` beats `::slotted(ion-note)`'s `margin: 0`; measured `start`, `end`, `stacked` at y = 7 |
| 10 | As wide as its text, so it doesn't push or hide what's next to it, and a tap beside it is on the item | flex item; measured `def` 55 wide, `end` 19.58 |
| 11 | Long text wraps: in its parent, and in the default slot at 358 | measured `alone-long` 4 lines, `def-long` 24 high |
| 12 | A tap on a note doesn't forward to the item's first control, even on its padding, but a tap on the item's padding does | `clickedWithinShadowRoot`; measured `tog-n`, `tog2-n` |
| 13 | A button item's name includes the note's text | native `<button>` |
| 14 | RTL: an end note sits at the left, with its margin on the right | measured `rtl`: x = 16, margin-right 16 |
| 15 | With `.ion-text-nowrap` on it, it's one line. In a label with that class, also with an ellipsis | The global class; `white-space` inherits; measured `nw-n`: 703 wide, cut off by the label's 358 |

## Design

**`AvonicNote` is a `ContentControl`,** like `AvonicLabel`: one slot, so its content is a logical child, and the item's
automation name finds the text through it. Text wraps, and the stub's transparent background makes the whole box take
input, because a tap on a note's padding hits the note in Ionic.

**The look is all in `Styles/Note.axaml`, with no control theme in `Avonic.Themes`,** as in `ion-label`. The stub holds
the template, so there's one copy of it, and one file in CSS order shows that 11 px comes after 14.

**`HorizontalAlignment` is `Left` in the stub,** because a note is as wide as its text in all of Ionic's cases: inline
outside an item and a flex item inside one. `FlowDirection` mirrors it. It wraps as usual, because it's still measured
against the available width. It also makes the hit box the same as Ionic's, which is what behaviour 10 needs.

**`margin: 0` needs no rule.** `::slotted(ion-note)`'s `margin: 0` loses to `::slotted([slot=end])`'s, and the default
slot has no margin anyway. A rule that does nothing is left out.

**`align-self: flex-start` is the note's `VerticalAlignment`,** which the item's `FlexSlot` honours since #71. This used to take a `:has-note`
pseudoclass on the presenter. [The item's plan](../ion-item/PLAN.md#the-flex-layout-71) says what replaced it.

**A panel directly in a slot stands for the slot,** as in `ion-label`, because `<ion-note slot="end"><ion-icon slot="end">`
has to be a panel here. So a note in one is 11 px, has its padding, and is at the top, and the others in that panel are
centred, which is `align-items: center`. A slot is a row, so a vertical `StackPanel` doesn't reach the top: it's a group, and the
item centres it, as Ionic does a column in a `<div slot="end">` (measured `group`: y = 27.59, 32 high, in a row of 88).

**The stub's `Wrap` stops `white-space` inheriting,** because Avalonia's default is `NoWrap`, and a browser's is to wrap.
A style can't tell a default from a value set on an ancestor. So `.ion-text-nowrap` has a rule, on the note and on a label
around it, the places Avonic has that class. The second is a descendant selector aimed at this control, which costs other
types nothing.

## Deliberate differences

| Difference | Why |
|---|---|
| A long note's box is as wide as its longest line, where Ionic's is all that's available, 342 in a slot | It's aligned left, for behaviour 10. The text is in the same place, and a tap in the gap reaches the item |
| A note in a panel directly in a slot acts as slotted, where Ionic's in a `<div slot="end">` is 14 px and centred (measured `in-div`) | See the design: this is what two notes or a note and an icon in one slot have to be. Wrap it in a `Border` for what `<div>` does |
| `TextBlock.TextWrapping="NoWrap"`, or `.ion-text-nowrap`, on any other ancestor doesn't reach a note, where Ionic's `white-space` does (measured `div-n`) | See the design. Set it on the note |
| Whether a panel is a vertical stack is read when the slot arranges, and only a `StackPanel` counts. So a `Grid` with rows acts as a row, with the note at the top | A panel can't say which way it flows. Set `VerticalAlignment="Center"` on the panel to centre it as a group |
| A note in a vertical `StackPanel` in a slot is 11 px with its padding, where Ionic's in a column in a `<div slot="end">` is 14 with none (measured `group-n`) | A selector for it would be longer than the case is worth. Wrap the stack in a `Border` |
| A hidden note in a panel still stretches the slot | Nothing shows: the others in the panel are centred anyway. Measured with `display: none`: the icon beside it stays centred |
| A note is a box, not inline, so it can't sit in a sentence | `ion-note` is nearly always a box in practice. A sentence can use a `Run` or an `AvonicTextSpan` |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-note)` in `item-divider.md.css` | `ion-item-divider` (#9): it adds its own rules for the same pseudoclass |
| The default text size outside a host | Not relevant here, because a note sets its own |

## Dependencies

`ion-item` and `ion-label`, done. `ion-toggle` doesn't exist yet, so the tap test uses a stub with `ItemControlType.Toggle`.
