# ion-list-header — Plan

A heading row for a list. It has lines of its own, which a list's `Lines` doesn't reach, and it isn't an "item": unlike a
divider, it has no class `item`, so a label inside it has none of a label's item rules. Measurements are from real Ionic
8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-list-header`.

### Props (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `lines` | `Lines` (`ListHeaderLines?`) | `Full`, `Inset`, `None`; null is no border, as the variables' defaults are 0 |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `inner` | `PART_Inner` (`Border`) |

No events or methods.

### CSS variables (6)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.ListHeader.Background` | brush | transparent |
| [x] | `--border-color` | `Avonic.ListHeader.BorderColor` | brush | Unset → `Avonic.ItemBorderColorBrush` |
| [ ] | `--border-style` | — | | Not applicable: Avalonia's `Border` only draws solid |
| [x] | `--border-width` | `Avonic.ListHeader.BorderWidth` | `Thickness` | 0 |
| [x] | `--color` | `Avonic.ListHeader.Color` | brush | Unset → `Avonic.TextColorBrush` |
| [x] | `--inner-border-width` | `Avonic.ListHeader.InnerBorderWidth` | `Thickness` | 0 |

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | 45 high at least, 16 of padding at the start and none at the end, text 14 with its weight inherited, centred, black on transparent | `list-header.md.css`; measured `h1` |
| 2 | `Lines`: none by default, full draws the outer border and makes the host 46 with a 45 inner, inset draws the inner border in 45, and none has neither, beating the tokens. A list's `Lines` doesn't reach it | measured `h-full`, `h-inset`, `h-none`, `a3h` |
| 3 | Both borders are `Avonic.ItemBorderColorBrush`, even in a colour, with no shade | measured `h-col`, `h-col2`: `rgba(0, 0, 0, 0.13)` |
| 4 | `Color` paints base and contrast. The translucent border shows the background through it. An unknown colour is transparent, and the text inherits | measured `h-col`, `h-bad` |
| 5 | All five tokens are read | |
| 6 | A label fills the row, has no margin, and inherits its size and colour. Its `Color` wins. `Avonic.Label.Color` isn't read, and a nowrap label clips with no ellipsis | `::slotted(ion-label) { flex: 1 1 auto }`; no class `item`; measured `hl`, `hv`, `hd`, `hnw` |
| 7 | Long text wraps right up to the end, and grows the header | measured `h-long`: 374 wide, 48 high |
| 8 | It clips what overflows it | `overflow: hidden` |
| 9 | RTL mirrors the padding, so the inner box is 0 to 374 | measured `h-rtl` |
| 10 | A label beside something else shares the row, with a `Grid ColumnDefinitions="*,Auto"` | measured `hlb` with `ion-button` |
| 11 | The template has `PART_Inner`, stretched | `part="inner"` |

## Design

**A `ContentControl`,** because it has one slot, so the content is a logical child of the header. That makes
`c|AvonicItemGroup c|AvonicLabel` find a label in a header in a group, as Ionic's `:host-context(.item)` does (`ghl`).

**Its minimum is on `PART_Inner`.** `min-height` on the host is inherited by `.list-header-inner`, and that's the one that
counts: measured, a full line makes the host 46 and the inner 45, and an inset line stays in 45.

**The host's background reaches under its border.** CSS's `background-clip` is `border-box`, and Avalonia's default stops short of
that, so a translucent border over a colour looked lighter than it should. `BackgroundSizing="OuterBorderEdge"` fixes it.
The compare step found this.

**`.ion-color` with an unknown colour** inherits through `$parent[Control].(TextElement.Foreground)`, as `ion-label`'s does.

## Deliberate differences

| Difference | Why |
|---|---|
| One child in the slot, where Ionic's default slot is a flex row | Mapping rule. A `Grid` does what `flex: 1 1 auto` does. Measured `hlb`: the label takes what the button leaves |
| An explicit `Lines` beats a token | The same as `ion-item`'s plan |

## Not in this PR

| What | Goes with |
|---|---|
| `inListHeader`, which sizes an `ion-button` in a header | `ion-button` (#10): it's that control's `closest()`. Read `button.js` |
| A label's `Position` here: measured `hst`, a stacked label's transform applies with no item, and pushes it out of the row | `Styles/Label.axaml` only applies those rules in an item, and it isn't this PR's to edit. Docs for `position` say it's for items |
| `select-popover`'s rule for `ion-list-header` | `ion-select` (#45) |
| `--ion-safe-area-*` | `ion-content` (#11), as for `ion-item` |

## Dependencies

`ion-label`, in #57, which this branch is built on. `Avonic.ItemBorderColorBrush` is in `Tokens/Item.axaml`.
