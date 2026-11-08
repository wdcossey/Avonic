# ion-badge, ion-avatar and ion-thumbnail — Plan

Three display atoms in one PR: `AvonicBadge`, a small coloured count; `AvonicAvatar`, a round picture; `AvonicThumbnail`,
a square one. None has behaviour of its own beyond showing up and hiding when empty, and what matters is how each sits
in `ion-item`. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide, wrapped in `ion-app`; they're in
[`ref/measurements.json`](ref/measurements.json), keyed by the names in brackets below, with the page in
[`ref/measurements.png`](ref/measurements.png).

## Fidelity checklist

`node tools/ionic-api.js ion-badge`, `ion-avatar` and `ion-thumbnail`.

### Props

| ✓ | Control | Ionic | Avonic | Notes |
|---|---|---|---|---|
| [x] | badge | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [ ] | badge | `mode` | — | Not exposed (ADR 0001) |

`ion-avatar` and `ion-thumbnail` have no props, in `core.json` or in their `.js`.

### Slots, parts, events, methods

All three have only the default slot, which is `Content` and `ContentTemplate` (`ContentControl`). No parts, events or
methods on any of them.

### CSS variables

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | badge `--background` | `Avonic.Badge.Background` | brush | Unset → `Avonic.Color.PrimaryBrush` |
| [x] | badge `--color` | `Avonic.Badge.Color` | brush | Unset → `Avonic.Color.PrimaryContrastBrush` |
| [x] | badge `--padding-top` | `Avonic.Badge.PaddingTop` | double | 3 |
| [x] | badge `--padding-end` | `Avonic.Badge.PaddingEnd` | double | 4 |
| [x] | badge `--padding-bottom` | `Avonic.Badge.PaddingBottom` | double | 4 |
| [x] | badge `--padding-start` | `Avonic.Badge.PaddingStart` | double | 4 |
| [x] | avatar `--border-radius` | `Avonic.Avatar.BorderRadius` | `CornerRadius`, double, or a string as CSS writes `border-radius` | `50%` |
| [x] | thumbnail `--border-radius` | `Avonic.Thumbnail.BorderRadius` | same | `0` |
| [x] | thumbnail `--size` | `Avonic.Thumbnail.Size` | double | Unset → `Avonic.Thumbnail.Md.Size`, 48, or `Avonic.Thumbnail.Md.SizeInItem`, 56, in an item's slot |

`badge.md.css` sets the paddings twice: `3 8 3 8` in the shared block and then `3 4 4 4` for md, and the second wins, as
measured (`alone`: `padding: 3px 4px 4px`). `ion-avatar` has no `--size` in md: `avatar.md.css` hard-codes 64 × 64, so
that's a literal in the theme, and `::slotted(ion-avatar) { width: 40px; height: 40px }` in `item.md.css` is a literal in
`Styles/Avatar.axaml`. `--size` is only `ion-thumbnail`'s.

`--border-radius` is `50%` in Ionic, which no Avalonia `CornerRadius` can say, and an app sets it in px as often as not. So
the key takes either: a `CornerRadius` (or a plain number) is used as it is, and a string is read as CSS reads the
`border-radius` shorthand, one to four lengths or percentages, a percentage being of the box's width across and its height down. The converter is `BorderRadius`, in `Controls/Avatar/`, and both controls read it the same way.

`--size` is 48 on the host and 56 from the item's `::slotted(ion-thumbnail)`. A page rule `ion-thumbnail { --size: 30px }`
beats the item's `::slotted` rule, as the outer tree wins in CSS scoping, so the key is unset by default and the theme's
bindings say what it falls back to: 48 alone, 56 in a slot. Setting the key, globally or in a thumbnail's `Resources`,
wins everywhere.

## Behaviours

Each becomes a test, in `AvonicBadgeTests`, `AvonicAvatarTests` and `AvonicThumbnailTests`, named after its number.

### Badge

| # | Behaviour | Source |
|---|---|---|
| B1 | Alone: 13 px bold, the token font, primary on its contrast, radius 4, padding 3 4 4 4, so 20 tall, with the text in the middle; whatever the parent's size, font or colour, and beside something taller | `badge.md.css`; measured `alone` in a 30 px serif `rgb(1,2,3)` parent: 13 px 700, 20 high, `rgb(0,84,233)` on white |
| B2 | `Color` paints the base behind the contrast, alone and in an item, and follows a change | `:host(.ion-color)`; measured `alone-danger` `rgb(197,0,15)`, `start` |
| B3 | An unknown `Color` has no background and inherits the text colour; in a coloured item it shows the item's base and contrast, as `--ion-color-base` is inherited | measured `alone-bad`: `rgba(0,0,0,0)`, `rgb(1,2,3)`; `end-primary-bad`: `rgb(0,84,233)`, white |
| B4 | The tokens set the background, the text and the paddings, and `Color` beats the first two | measured `alone-vars`: `rgb(10,20,30)`, `rgb(40,50,60)`, `padding: 1px 2px 3px 4px`, 17 high; `alone-vars-color`: danger |
| B5 | Empty, it takes no space, and shows again with content | `:host(:empty) { display: none }`; measured `alone-empty` 0 × 0, `after-empty` at x = 115.59, right after the badge before it |
| B6 | It never wraps, however long | `white-space: nowrap`; measured `alone-long` 547.09 wide in a 390 page, 20 high |
| B7 | At least 10 wide, and otherwise its text plus 8 | `min-width: 10px`; measured `alone-i` 11.63, `alone-1` 15.23 |
| B8 | In an item's slot: the slot's margin of 16, centred in the row however tall, 7 above and below beside a stacked label, and its own size whatever the item's | `::slotted([slot=end])` etc, in `item.md.css`; measured `end` y = 13.5 in 48, `end-tall` y = 33.59 in 88.19, `end-stacked` `margin: 7px 0px 7px 16px`, `end-sized` 20 high in a 30 px item, `def` x = 16 with no margin |
| B9 | A coloured item doesn't change a badge: it stays primary on white | `--background: var(--ion-color-primary)`; measured `end-primary`: `rgb(0,84,233)`, white |
| B10 | RTL: an end badge at the left, with its margin on the right | measured `end-rtl`: x = 16, `margin: 0px 16px 0px 0px` |
| B11 | Text is centred in a wider badge | `text-align: center` |

A button item's name includes a badge's text, as a native `<button>`'s does, but that's `ion-item`'s `AccessibleText`, which reads any text drawn inside, and has its tests there. A hidden empty badge adds nothing to it.

### Avatar

| # | Behaviour | Source |
|---|---|---|
| A1 | Alone: 64 × 64, no margin, whatever is around it | `avatar.md.css`; measured `av-block` |
| A2 | An `Image` in it fills it, cropped to cover from the middle, and is cut to the radius: a circle | `::slotted(img)`; measured `av-block-img` 64 × 64, `object-fit: cover`, `border-radius: 50%` |
| A3 | `Avonic.Avatar.BorderRadius` rounds the image, as a `CornerRadius` or a percentage, globally or in one avatar's `Resources` | measured `av-alone-r8`: 8 px on host and image |
| A4 | A `Background` on the avatar follows the radius | `:host { border-radius }`; measured `av-bg`: `rgb(0,84,233)` at 50% |
| A5 | Content that isn't an image isn't cut to the radius | The host has no `overflow: hidden`; only `::slotted(img)` has the radius |
| A6 | In an item's or a divider's slot: 40 × 40, 8 above and below, 16 towards the content, so a row of 56. In a panel directly in a slot, 40 with 8 above and below | `::slotted(ion-avatar)`, `::slotted(ion-avatar[slot=start])` in `item.md.css` and `item-divider.md.css`; measured `av-start` `margin: 8px 16px 8px 0px`, `av-end` `8px 0px 8px 16px`, `av-def` `8px 0px`, `item-av-start` 56 high, `av-divider` |
| A7 | Nested deeper in a slot, it's 64 again with no margin | measured `av-div`: 64 × 64, `margin: 0px` |
| A8 | Beside a stacked label: 7 above and below | `.item-label-stacked ::slotted([slot=end])` beats `::slotted(ion-avatar)`; measured `av-stacked` `margin: 7px 0px 7px 16px` |
| A9 | Centred in a tall row | `align-items: center`; measured `av-tall` y = 24.09 in 88.19 |
| A10 | `Width` and `Height` on the avatar beat the slot's 40, and the margins stay | measured `av-vars`: 30 × 30, `margin: 8px 16px 8px 0px` |
| A11 | RTL: a start avatar at the right, with its margin on the left | measured `av-rtl`: x = 334, `margin: 8px 0px 8px 16px` |
| A12 | Its size doesn't follow the item's font size | measured `av-sized`: 40 in a 30 px item |

### Thumbnail

| # | Behaviour | Source |
|---|---|---|
| T1 | Alone: 48 × 48, square | `thumbnail.css`; measured `th-block` |
| T2 | An `Image` in it fills it, cropped to cover from the middle, with square corners | `::slotted(img)`; measured `th-block-img` |
| T3 | `Avonic.Thumbnail.Size` sets the size, alone and in an item, where it beats the slot's 56; `Avonic.Thumbnail.BorderRadius` rounds the image, `50%` to a circle | measured `th-alone-vars`: 64 high, `border-radius: 50%`; `th-vars`: 30 in an item |
| T4 | In an item's or a divider's slot: 56 × 56, 8 above and below, 16 towards the content, so a row of 72. In a panel directly in a slot, 56 with 8 above and below | `::slotted(ion-thumbnail) { --size: 56px }` and the margins; measured `th-start`, `th-end`, `th-def` `margin: 8px 0px`, `item-th-start` 72 high, `th-divider` |
| T5 | Beside a stacked label: 7 above and below | measured `th-stacked` `margin: 7px 0px 7px 16px` |
| T6 | `Width` and `Height` on the thumbnail beat the slot's 56 | the inline `style` in `th-vars` |
| T7 | RTL: a start thumbnail at the right, with its margin on the left | the same rule as `av-rtl` |

## Design

**All three are `ContentControl`s,** like `AvonicNote`: one slot, so the content is a logical child, which is what
`c|AvonicAvatar > Image` needs, and the item's automation name finds a badge's text through it.

**The badge's look is a `ControlTheme`** in `Avonic.Themes/Controls/AvonicBadge.axaml`, as nothing about it depends on
its host: `item.md.css` has no `::slotted(ion-badge)` rule, and the slot margins it does get are the item's own rules for
any child, which `Styles/Item.axaml` already has. The one global rule, in `Styles/Badge.axaml`, is `line-height: 1` on the
text, which has to reach the `TextBlock` the presenter makes, and a `ControlTheme` can't (checked: `^ /template/
ContentPresenter > TextBlock` doesn't apply to it). That `TextBlock`'s logical parent is the badge, as an app's own would be,
so the rule is `c|AvonicBadge > TextBlock`, and the same through a panel. It follows `Styles/Button.axaml`: `ClipToBounds="False"` and half the
leading as a margin, through `Css.HalfLeading`.

**An empty badge hides itself in code,** at style priority, so that it survives a replaced theme and an app's `IsVisible`
wins. Empty is `Content` null or an empty string: a template renders no text node for `""`, so `:empty` matches in Ionic
too. `FlexRow` already treats a hidden child as `display: none`, so an empty badge in a slot leaves no margin.

**It clips,** as `contain: content` does, which is a `ContentControl`'s default. Its text isn't cut, as the half leading keeps
it inside. It's centred down by default (`OverrideDefaultValue`, not the theme, as `FlexSlot` would take a theme's value
for `align-self`), so it never stretches to a taller line, as an inline-block doesn't.

**`line-height: 1` is set on the text, not inherited from the badge,** as Avalonia's `LineHeight` inherits as a size and CSS's as a
factor: a `TextBlock` of 20 in a badge has a line of 20, and the badge is 27 high, as in CSS. A panel directly in the badge stands for it.

**The badge's text size is 13 whatever the host's,** because `0.8125rem` is of the root, not the parent (measured
`end-sized`: 20 high in a 30 px item). So it's a literal `FontSize`, not `Css.Em`.

**`:ion-color` paints `ColorPalette.Base` straight,** as `AvonicItem` does: an unknown colour binds null, so there's no
background, which is what an invalid `var()` does to `background`. The text is `Css.Var` of `Contrast` and the parent's
`Foreground`, which is `color` inheriting.

**Avatar and thumbnail are a presenter.** Its `CornerRadius` comes from the key through
`BorderRadius.ToCornerRadius`, with its own `Bounds` for a percentage, and its `Background` is the control's, so an avatar
with a background and initials is round, as `av-bg` is. It doesn't clip its content: the host has no `overflow: hidden`,
and only `::slotted(img)` has the radius. So `Styles/Avatar.axaml` gives an `Image` child `Stretch="UniformToFill"`, which
is `object-fit: cover` with the crop from the middle (checked by pixel), and a `Clip` from `BorderRadius.ToClip`, with the
image's `Bounds` and the same key. For `50%` that's a `RectangleGeometry` with `RadiusX` and `RadiusY` of half each side,
which is CSS's ellipse exactly. Corners of different sizes are a path, scaled down together when two would overlap, as
CSS does. An invalid value is 0, as an invalid `var()` leaves `border-radius` at its initial value. `AvonicImg` doesn't exist yet (#17); when it does, its PR adds `c|AvonicAvatar >
c|AvonicImg` and the thumbnail's to its own file, as rules for a control that doesn't exist go in that control's file.

**The slotted rules need an activator.** `Styles/Item.axaml`'s `::slotted([slot=start])` sets the whole `Margin` on any
child, so a plain `c|AvonicAvatar` rule for `0,8,16,8` would win or lose by file order: `Avatar.axaml` merges before
`Item.axaml` and `Thumbnail.axaml` after it. CSS resolves this by specificity, which Avalonia doesn't have: a rule with a
pseudoclass beats one without. So:

- `::slotted(ion-avatar)`, the 40 and the 8 above and below, has no activator, as only the default slot and a panel take its
  margin, and nothing else sets theirs.
- `::slotted(ion-avatar[slot=start])` uses `:in-item`, which the avatar and the thumbnail set when an `IItemHost` is around
  them, as `AvonicButton` sets Ionic's own `in-item`. It's an activator on the control itself, not its ancestors.
- `::slotted(ion-avatar[slot=end])` is `:is(c|IItemHost):not(:item-label-stacked):not(:item-label-floating)`. In CSS,
  `:host(.item-label-stacked) ::slotted([slot=end])` beats it, with 7 above and below, and this leaves those items to that
  rule. The `:not`s are its activator. With none, `Avatar.axaml`'s rule would lose to `Item.axaml`'s `16,0,0,0`, which comes later; with
  `:in-item` instead, `Thumbnail.axaml`'s 8 would beat `Item.axaml`'s stacked 7, by coming later.

**One alternative covers the item and the divider:** `:is(c|IItemHost) /template/ ContentPresenter#PART_Start > ...`,
checked to match both. `item-divider.md.css` has the same avatar and thumbnail rules as `item.md.css`, word for word, and
the divider's own file left them for these controls. A group is an `IItemHost` with no presenters, so it costs nothing there.

**A panel directly in a slot stands for the slot,** as in `ion-note`: an avatar in one is 40 with 8 above and below, and
the panel already has the slot's 16 from `Item.axaml`.

## Deliberate differences

| Difference | Why |
|---|---|
| A non-square avatar's own background at `50%` is a stadium, where Ionic's is an ellipse (measured `av-wide`: 61.25 × 40) | `CornerRadius` has one radius per corner. The image's clip is an ellipse, which is what's seen with a picture in it |
| `Avonic.Avatar.BorderRadius` takes a `CornerRadius`, a number, or a string of one to four lengths in px or percentages, where Ionic takes any CSS `border-radius`, `/` and `em` included | There's no Avalonia type for a percentage radius. A string is checked when it's read, and anything else is 0, as an invalid `var()` is |
| `Avonic.Thumbnail.Size` takes a number, not a string such as `64px` | It's a size, as every other size key is |
| `Avonic.Thumbnail.Size` set anywhere beats the item's 56, as the key is unset by default | The outer tree beats `::slotted` in CSS. A thumbnail that should be 56 only in items sets `Width` and `Height` there |
| An empty string is empty, as null is | A framework renders no text node for `""` |
| A badge is a box, not inline, and outside an item it's centred down in what it's given, not on the text's baseline | `vertical-align: baseline` has no equivalent. In an item it's centred, as Ionic's flex item is |
| A badge wider than its parent is cut at the parent's width, where Ionic's runs off the page (measured `alone-long`: 547.09 in 390) | Avalonia gives a child no more than the space it has |
| Text: Roboto isn't installed on this machine, so Chromium uses Arial and Avalonia uses Segoe UI | The badge stays 20 high either way, as `line-height: 1` is of the font size |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-avatar)` in `chip.md.css`, with its em sizes and negative margins | `ion-chip` |
| `::slotted(ion-badge)` in `tab-button.md.css`: absolute position, 8 px text, the empty dot | `ion-tab-button`. Its dot needs `:empty` on the badge, which nothing uses yet, so this PR leaves it out |
| `::slotted(ion-img)` in `avatar.md.css` and `thumbnail.css` | `ion-img` (#17), which adds the rule in its own file |
| `.item-multi-line ::slotted([slot=start])`, margin 16 and `align-self: flex-start` | Nothing: no Ionic 8 code sets `item-multi-line`, and `av-tall` is centred |
| `ion-skeleton-text`'s `hostContext('ion-avatar')` and `hostContext('ion-thumbnail')` | `ion-skeleton-text` |

## Dependencies

`ion-item`, `ion-item-divider` and `ion-label`, done. `ion-img` isn't; the tests use Avalonia's `Image` with a
`DrawingImage` of 120 × 80, the same picture as the inline SVG in the reference render.
