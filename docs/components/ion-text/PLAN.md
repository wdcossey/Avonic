# ion-text — Plan

All of `text.css` is one rule, `:host(.ion-color) { color: var(--ion-color-base) }`, on an element that's inline. The
question is what shape that is in Avalonia. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide. It
shares a PR with [`ion-note`](../ion-note/PLAN.md).

## Fidelity checklist

`node tools/ionic-api.js ion-text`.

### Props (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color`, on both `AvonicText` and `AvonicTextSpan` | `ColorPalette.ColorProperty.AddOwner` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods, CSS variables

No slots beyond the default one (`Content`, `ContentTemplate` on `AvonicText`, `Inlines` on `AvonicTextSpan`), no parts,
no events, no methods, no CSS variables. Measured: `--color` on an `ion-text` does nothing (`t-var` stays black).

## Behaviours

Each becomes a test, for both shapes where both apply.

| # | Behaviour | Source |
|---|---|---|
| 1 | With no colour it does nothing: colour, size and font all come from its parent | measured `t-alone`, `t-inh` 30 px serif `rgb(1,2,3)` |
| 2 | `Color` paints the text | `:host(.ion-color)`; measured `t-danger` |
| 3 | An unknown `Color` inherits, or shows a coloured ancestor's base, because `--ion-color-base` is inherited | measured `t-inh-bad` `rgb(1,2,3)`, `t-inner-b` red, `t-color3` blue |
| 4 | Nested: an inner one with no colour takes the outer's, and an inner colour wins | measured `t-inner`, `t-inner-c` |
| 5 | Everything inside takes the colour: blocks and icons | `color` inherits; measured `t-h1`, `t-p`, `t-div-inner`, `t-icon-i` |
| 6 | In a sentence, it flows with the words around it and wraps with them | `display: inline`; measured `t-sentence`: 3 boxes, and the `p` is 200 wide with no gap |
| 7 | In an item: it follows the item's colour, or a coloured item's contrast, and its own `Color` beats that | measured `t-color` white, `t-color2` |
| 8 | In an item's slot: the slot's margins, and centred however tall the row is, unlike a note. As wide as its text | measured `t-end`, `t-start`, `t-end-tall` at y = 34.59 of 88, `t-def` 80.92 wide |
| 9 | Long text wraps and grows the item | measured `t-long`: item 55 high |
| 10 | In a label's `p`: the `p`'s size and line height, with its own colour | measured `t-lp3t`: 14 px, line height 20 |
| 11 | A tap on it doesn't forward to the item's first control, and a tap beside it does | `clickedWithinShadowRoot` |
| 12 | A button item's name includes its text | native `<button>`. For `AvonicText`; words in inlines are a known gap, below |
| 13 | A change of `Color` repaints at once | `reflect`, `createColorClasses` |
| 14 | With `.ion-text-nowrap` on it, it's one line. In a label with that class, also with an ellipsis | The global class; `white-space` inherits; measured `nw-t`: 804 wide, cut off by the label's 358 |

## Design

**Two shapes, because Avalonia has two kinds of element and HTML has one.** `ion-text` is used in two ways:

```html
<p>Some words and <ion-text color="primary">some coloured ones</ion-text> in a sentence.</p>
<ion-text color="danger"><h1>A whole block</h1></ion-text>
```

| Shape | Why it's needed | What it can't do |
|---|---|---|
| `AvonicText : ContentControl`, with `Content` | Mechanical from the mapping rules. It takes any control, including a panel or an icon, and goes in an item's slot | A box can't flow in a sentence. `InlineUIContainer` puts it there as one unbreakable box |
| `AvonicTextSpan : Span`, with `Inlines` | The only way to flow with the words around it and wrap with them, which is what `display: inline` measures as | An inline can only go in a `TextBlock`. It isn't a `Control`, so a slot shows it as its type's name |

Neither can stand in for the other, and a sentence is what `ion-text` is mostly for. The two share everything else:
`ColorPalette.ColorProperty.AddOwner`, and the one rule. This is flagged for the human in the PR.

**`AvonicText` is as wide as its content,** with `HorizontalAlignment` `Left` in the stub, the same as a note. `display:
inline` hugs its text, and so does a flex item. Measured `t-block` is 390 wide around a block, which is a case where
inline's box isn't really a box. For a block that fills the width, set `HorizontalAlignment` to `Stretch`.

**The stub's `Wrap` stops `white-space` inheriting,** because Avalonia's default is `NoWrap`, and a browser's is to wrap. A
style can't tell a default from a value set on an ancestor. So `.ion-text-nowrap`, on the box or on a label around it, has a
rule in `Styles/Text.axaml`, the only file this control has in `Avonic.Themes`, because that class is the theme's too.

**The one rule is in the stub, with no template or tokens in `Avonic.Themes`.** `PROCESS` says a control whose look Ionic
hard-codes with no CSS variable has no theme. This isn't a visual opinion: it's what the `Color` property means, and an app
that replaces the theme still needs it. The palette's values come from whichever theme is there.

**`color: inherit` for an unknown colour** is `Css.Var` with the parent's `Foreground` as the last value, as in
`ion-label`. For the span, the parent can be a `TextBlock` or another `Span`, so it reads `$parent[StyledElement]`.

**`h1`–`h6` and `p` don't apply through it.** `Styles/Label.axaml`'s rules are `c|AvonicLabel > TextBlock.h1` and
`c|AvonicLabel > :is(Panel) > TextBlock.h1`, so only a direct child or a child of a panel directly in a label matches,
and nothing matches outside a label, because Avonic has no equivalent of `core.css`'s global typography. So
`<ion-label><ion-text color="danger"><p>` is written the other way round: the `TextBlock` holds the class, and the span
holds the colour.

```xml
<c:AvonicLabel>
    <TextBlock Classes="p"><c:AvonicTextSpan Color="danger">Red</c:AvonicTextSpan></TextBlock>
</c:AvonicLabel>
```

Adding `c|AvonicLabel > c|AvonicText > TextBlock.p` would repeat the label's literals in another file for a case the span
already covers.

## Deliberate differences

| Difference | Why |
|---|---|
| `<ion-text><p>` in a label, with `AvonicText` around a `TextBlock.p`, doesn't get the `p`'s size. Measured in Ionic: 14 px, line height 20, because `::slotted(*) p` reaches any depth | See the design. Put the span inside the `TextBlock` |
| Around a `p` with no colour, or with an unknown one, Ionic's text inherits, and a span in a `TextBlock.p` shows the `p`'s grey (measured `t-lp2` black) | A span can't undo the `TextBlock` it's in. Leave out the `Classes`, or set `Foreground` |
| `TextBlock.TextWrapping="NoWrap"`, or `.ion-text-nowrap`, on any other ancestor doesn't reach an `AvonicText`, where Ionic's `white-space` does (measured `div-t`) | See the design. Set it on the `AvonicText`. A span follows its `TextBlock` anyway |
| An `AvonicTextSpan` directly in a slot shows its type's name | A slot takes a control. Use `AvonicText`, or put the span in a `TextBlock` |
| A block in `AvonicText` isn't full width by itself (measured `t-h1` 390) | See the design |
| **Known gap:** a button item's automation name leaves out words in inlines, whether they're in a span or not, because `AvonicItem` reads `TextBlock.Text`, which is empty when a `TextBlock` has inlines. A native `<button>` names itself from all its text | The fix is in the item, so it's proposed in the PR. Until then, set `AutomationProperties.Name` on the item |
| A long `AvonicText`'s box is as wide as its longest line, as with a [note](../ion-note/PLAN.md#deliberate-differences) | It's aligned left. The text is in the same place |

## Not in this PR

| What | Goes with |
|---|---|
| `core.css`'s global `h1`–`h6` and `p`: measured `t-h1` is 26 px with margins 20 and 10 | Its own decision. Nothing in Avonic styles a `TextBlock.h1` outside a label |
| The default text size outside a host: Avalonia's, where a web page's is 16 | Already an open question, and it's what this control inherits |

## Dependencies

`ion-item`, `ion-label` and `ion-icon`, done. `ion-toggle` doesn't exist yet, so the tap test uses a stub with
`ItemControlType.Toggle`.
