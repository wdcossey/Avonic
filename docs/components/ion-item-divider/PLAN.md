# ion-item-divider — Plan

A heading row between items, with three slots like an item, and a `sticky` prop that Ionic gets for free from CSS and
Avalonia has no equivalent for. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-item-divider`.

### Props (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `sticky` | `Sticky` (`bool`) | In the control, so it survives a replaced theme |
| [ ] | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `start` | `StartContent`, `StartContentTemplate` |
| [x] | `end` | `EndContent`, `EndContentTemplate` |
| [x] | `inner` | `PART_Inner` (`Border`) |
| [x] | `container` | `PART_Container` (`ContentPresenter`) |

No events or methods.

### CSS variables (10)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.ItemDivider.Background` | brush | Unset → `Avonic.BackgroundColorBrush` |
| [x] | `--color` | `Avonic.ItemDivider.Color` | brush | Unset → `Avonic.TextColorStep600Brush` (`#999999`) |
| [x] | `--padding-top`, `-end`, `-bottom` | `Avonic.ItemDivider.PaddingTop`, `…End`, `…Bottom` | `double` | 0 |
| [x] | `--padding-start` | `Avonic.ItemDivider.PaddingStart` | `double` | 16 |
| [x] | `--inner-padding-top`, `-bottom`, `-start` | `Avonic.ItemDivider.InnerPaddingTop`, `…Bottom`, `…Start` | `double` | 0 |
| [x] | `--inner-padding-end` | `Avonic.ItemDivider.InnerPaddingEnd` | `double` | 16 |

## Behaviours

Each becomes a test.

| # | Behaviour | Source |
|---|---|---|
| 1 | 31 high: a 30 inner and a 1 border. 16 before the inner box, 16 after the wrapper. Text 14, normal, centred | `item-divider.md.css`; measured `d1`: inner `16,374`, wrapper `16,358` |
| 2 | `#999999` on the background, with a bottom border in the item border colour | measured `d1` |
| 3 | `Color` paints base and contrast, and leaves the border, which darkens the colour under it. An unknown colour is transparent, and the text inherits, not its own variable's grey | measured `d-col`, `d-bad`: `rgb(0, 0, 0)` |
| 4 | All ten tokens are read | |
| 5 | Slots: 16 beside the text. Icons: 24, 54% text colour, or the contrast in a colour; start 32 after and end 16 before, 12 above and below; the default slot has the size but no margins; deeper has none of it | `::slotted`; measured `d-slots`, `d-slot2`, `d-icon-def` |
| 6 | A label has 13 above and 10 below, adding a named slot's 16. A panel stands for the slot. Deeper has none | `::slotted(ion-label)`; measured `d-label`, 40 high |
| 7 | A label acts as it does in an item: it inherits, reads `Avonic.Label.Color`, its `Color` wins, it uses the font token, and `ion-text-nowrap` gives an ellipsis | The host's class `item` and `:host-context(.item)`; measured `dl`, `dlv`, `dlc`, `dnw` |
| 8 | A `p` is `#666666`, 14, with 2 below, even in a colour. A panel stands for the slot. Deeper has none | `::slotted(p)`; measured `dp`, `dpc` |
| 9 | Long text wraps and grows the row | measured `d-long`: 49 |
| 10 | It clips what overflows it | `overflow: hidden` on the host and inner |
| 11 | RTL mirrors the padding and slots: the start icon at 350, the end one at 16 | measured `d-rtl` |
| 12 | `Sticky`: stays at the top of the nearest scroller. In a flat list, a later one reaches the top and covers it | `position: sticky; top: 0`; measured `s-flat` |
| 13 | It stays in its parent: at the end of a group it's pushed out by the next. It's never above where it would be | measured `s-group`: −4 at 300, −104 at 400 |
| 14 | In an inset list it doesn't stick, because `overflow: hidden` makes the list its scrollport | measured `s-inset`: −26 at 50 |
| 15 | Off by default, changes live, and leaves nothing behind | measured `s-non` |
| 16 | With nothing scrolling, it stays put | |
| 17 | A stuck divider takes the input at the top | measured: `elementFromPoint` finds it |
| 18 | It's drawn above the rows after it, including when an `ItemsControl` wrapped it in a presenter | `z-index: 100` |
| 19 | An app's own `RenderTransform` wins | style priority |

## Design

**A `TemplatedControl`,** because it has three slots that have to act alike, like `AvonicItem`. The slots are plain
`ContentPresenter`s, so `::slotted` is `c|AvonicItemDivider /template/ ContentPresenter#PART_Start > c|AvonicIcon`.

**`position: sticky` moves the drawing, not the layout.** Sticky doesn't affect layout in CSS, and a `RenderTransform` doesn't in
Avalonia, so the control sets a `TranslateTransform` at style priority. The offset is:

```
natural = where the divider's layout box is, relative to the scroller's viewport
offset  = max(0, min(-natural, parent height - its top in the parent - its height))
```

`min` keeps it in its parent, as the sticky containing block does, and `max` stops it going above where it would be. Hit
testing goes through a render transform, as it does in CSS. It updates on `LayoutUpdated`, because a scroll is a change of
arrangement, and so is a change of content, of `Inset`, or of size. Only a sticky divider pays for this, and the transform
doesn't invalidate layout, so there's no loop.

**The scrollport is the nearest `ScrollViewer`, unless an inset list comes first.** `overflow: hidden` makes an element a
scrollport even though it never scrolls, so measured, a sticky divider in an inset list just moves with the page. Other
ancestors can't be treated this way, because an Avalonia `ClipToBounds` isn't `overflow: hidden`: `ContentControl` clips by
default, and it would stop dividers sticking for no reason you could see.

**An `ItemsControl` wraps templated content in a presenter as tall as the divider.** There's no such box in the DOM, so the
control climbs through presenters, and uses that for the parent's height and for `ZIndex`. Otherwise it could never move, and
the next row would be drawn over it.

**`z-index: 100` is only set while sticky.** It has no effect on a static block in CSS, and `Panel.ZIndex` would have one.

**There's no `:sticky` pseudoclass.** Nothing reads `item-divider-sticky` except the rule that makes it sticky, which is in code.

**`::slotted(ion-icon)`'s `1.7142857143em` is `12/7` to double precision here.** At 14 the short form gives 24.0000000002,
and layout rounding makes that 25. The tests found this.

**The host's background reaches under its border,** as for `ion-list-header`: `BackgroundSizing="OuterBorderEdge"`.

**A label is "in an item" here.** The host has the class `item`, which `:host-context(.item)` reads, so a rule in
`Styles/ItemDivider.axaml` gives it the font and colour, and the nowrap ellipsis, that `Styles/Label.axaml` gives a label in an item.

## Deliberate differences

| Difference | Why |
|---|---|
| **Known gap:** in a flat list with a `VirtualizingStackPanel`, a stuck divider's row gets recycled, so it goes when it leaves the realised range | The panel owns its children. Ionic has no virtualising panel to compare with. Use a group per section, which keeps it in a realised parent. `D18_Known_Gap_…` records it |
| **Known gap:** a label in `StartContent` or `EndContent` is as wide as its text, and long text doesn't wrap | The same `Auto` columns as `ion-item`, and `ion-label`'s plan records the same gap |
| Two things in a slot don't share it by themselves | One child per slot, as for `ion-item`. A panel stands for the slot |
| An app's own `RenderTransform` stops it sticking | It's the same property. An app that transforms a sticky divider has to wrap it |

## Not in this PR

| What | Goes with |
|---|---|
| `h1`–`h6` in a divider. `::slotted(h1)`'s margins are dead in real Ionic: global typography beats them. Measured `dh1` to `dh6`: sizes 26, 24, 22, 20, 18, 16, weight 500, line height 1.2, 16 above except `h1` 20 and `h2` 18, 10 below | Global typography, which has nowhere to live yet. `ion-text` (#8) or a later global style |
| `::slotted(ion-note)`, `ion-avatar`, `ion-thumbnail`, `ion-button`'s `inItem` | Those controls (#8, #16, #10), in their own `Styles` files. Values are in `item-divider.md.css` |
| A label's `Position` here: measured `dst`, `dfl`, `dfx`, the transforms and 100 width apply | `Styles/Label.axaml` only applies those rules in an item, and it isn't this PR's to edit. Docs for `position` say it's for items |
| `--ion-safe-area-*` | `ion-content` (#11), as for `ion-item` |

## Dependencies

`ion-icon`, done. `ion-label`, in #57, which this branch is built on. `Avonic.ItemIconBrush` and `Avonic.ItemBorderColorBrush`
are in `Tokens/Item.axaml`.
