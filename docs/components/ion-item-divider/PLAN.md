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
| 1 | 31 high: a 30 inner and a 1 border. 16 before the inner box, 16 after the wrapper. Text 14, weight inherited, centred | `item-divider.md.css`; measured `d1`: inner `16,374`, wrapper `16,358` |
| 2 | `#999999` on the background, with a bottom border in the item border colour | measured `d1` |
| 3 | `Color` paints base and contrast, and leaves the border, which darkens the colour under it. An unknown colour is transparent, and the text inherits, not its own variable's grey | measured `d-col`, `d-bad`: `rgb(0, 0, 0)` |
| 4 | All ten tokens are read | |
| 5 | Slots: 16 beside the text. Icons: 24, 54% text colour, or the contrast in a colour; start 32 after and end 16 before, 12 above and below; the default slot has the size but no margins; a panel stands for the slot for size and colour; deeper has none of it | `::slotted`; measured `d-slots`, `d-slot2`, `d-icon-def` |
| 6 | A label has 13 above and 10 below, adding a named slot's 16. A panel stands for the slot. Deeper has none | `::slotted(ion-label)`; measured `d-label`, 40 high |
| 7 | A label acts as it does in an item: it inherits, reads `Avonic.Label.Color`, its `Color` wins, it uses the font token, and `ion-text-nowrap` gives an ellipsis | The host's class `item` and `:host-context(.item)`; measured `dl`, `dlv`, `dlc`, `dnw` |
| 8 | A `p` is `#666666`, 14, with 2 below, even in a colour, keeping a named slot's 16. A panel stands for the slot. Deeper has none | `::slotted(p)`; measured `dp`, `dpc` |
| 9 | Long text wraps and grows the row | measured `d-long`: 49 |
| 10 | It clips what overflows it, and the default slot is clipped before the end slot | `overflow: hidden` on the host, inner and wrapper |
| 11 | RTL mirrors the padding and slots: the start icon at 350, the end one at 16 | measured `d-rtl` |
| 12 | `Sticky`: stays at the top of the nearest scroller, inside its padding. In a flat list, a later one reaches the top and covers it | `position: sticky; top: 0`; measured `s-flat`, and 30 with a padding of 30 |
| 13 | It stays in its parent: at the end of a group it's pushed out by the next, with its bottom margin kept. It's never above where it would be | measured `s-group`: −4 at 300, −104 at 400; margins of 8 and 6: −6 at 310 |
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
natural = where the divider's layout box is, relative to the scroller
offset  = max(0, min(scroller's top padding - natural, parent height - its top in the parent - its height - its bottom margin))
```

`min` keeps it in its parent, as the sticky containing block does, and `max` stops it going above where it would be.
Measured with margins of 8 and 6, the border box reaches the top, whatever its top margin, but its bottom margin stays inside the parent. Hit
testing goes through a render transform, as it does in CSS. It updates on `LayoutUpdated`, because a scroll is a change of
arrangement, and so is a change of content, of `Inset`, or of size. Only a sticky divider pays for this, it finds its scroller once
per attach, and the transform doesn't invalidate layout, so there's no loop.

**The scrollport is the nearest `ScrollViewer`, unless an inset list comes first.** `overflow: hidden` makes an element a
scrollport even though it never scrolls, so measured, a sticky divider in an inset list just moves with the page. Other
ancestors can't be treated this way, because an Avalonia `ClipToBounds` isn't `overflow: hidden`: `ContentControl` clips by
default, and it would stop dividers sticking for no reason you could see.

**An `ItemsControl` wraps templated content in a presenter as tall as the divider.** There's no such box in the DOM, so the
control climbs through presenters, and uses that for the parent's height and for `ZIndex`. Otherwise it could never move, and
the next row would be drawn over it. A presenter with a `TemplatedParent` is part of a template, such as a `ScrollViewer`'s, so it stops there.

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
| In a flat `AvonicList` with a `VirtualizingStackPanel`, the list draws a stuck divider once the panel recycles its row: a second container for the same item, from the list's `ItemTemplate`, at the top of the scrollport, under any later sticky divider, until the row is realised again ([#68](https://github.com/wdcossey/Avonic/issues/68)). Rows never realised are made from the template once, with the item as their data, to find the section's divider. An item that's a control isn't drawn twice | The panel owns its children and recycles a row once its place is above the top, and Avalonia has no way to keep one realised. Ionic has no virtualising panel: CSS keeps it stuck to the end of the list. A style's `Sticky` on an unrealised row isn't seen, as it's made outside the tree. `D18_In_A_Flat_Virtualising_Panel_…` |
| Two things in a slot don't share it by themselves | One child per slot, as for `ion-item`. A panel stands for the slot |
| An app's own `RenderTransform` stops it sticking | It's the same property. An app that transforms a sticky divider has to wrap it |
| The end of its parent includes the parent's padding, if that's a `Border` | CSS stops at the content box. `AvonicList` and `AvonicItemGroup` hold it in a `StackPanel`, which has no padding, so it only shows with a padded `Border` directly around a divider |
| A `ScrollViewer` with a custom template that puts something above its presenter measures from the `ScrollViewer`'s top | Avalonia's base themes don't do this. Reading the presenter did nothing in any test, so it was taken out as a rule that does nothing |

## Not in this PR

| What | Goes with |
|---|---|
| `h1`–`h6` in a divider. `::slotted(h1)`'s margins are dead in real Ionic: global typography beats them. Measured `dh1` to `dh6`: sizes 26, 24, 22, 20, 18, 16, weight 500, line height 1.2, 16 above except `h1` 20 and `h2` 18, 10 below | With the opt-in [typography](../../infra/typography/README.md) file ([#70](https://github.com/wdcossey/Avonic/issues/70)), as measured. Without it, Chromium gives `h1` 0 / 2 and `h2` 2 / 2 from `::slotted`, which Avonic doesn't add: a known difference in the typography plan |
| `ion-avatar`, `ion-thumbnail`, `ion-button`'s `inItem` | Those controls (#16, #10), in their own `Styles` files (their margin rules are in `Styles/ItemDivider.axaml` since #88 and its follow-up). Values are in `item-divider.md.css`. `::slotted(ion-note)` was left to `ion-note` the same way, and fell between the two until [#74](https://github.com/wdcossey/Avonic/issues/74) put it in `Styles/Note.axaml` |
| `--ion-safe-area-*` | `ion-content` (#11), as for `ion-item` |

## Dependencies

`ion-icon`, done. `ion-label`, in #57, which this branch is built on. `Avonic.ItemIconBrush` and `Avonic.ItemBorderColorBrush`
are in `Tokens/Item.axaml`.
