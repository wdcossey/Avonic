# ion-toolbar family — Plan

Five controls in one PR: `AvonicHeader`, `AvonicFooter`, `AvonicToolbar`, `AvonicTitle` and `AvonicButtons`. They only make
sense together, so they share this folder; `ion-header/`, `ion-footer/`, `ion-title/` and `ion-buttons/` hold a short README
each that points here. Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide, with the scenes listed at the top
of [COMPARISON.md](COMPARISON.md): `s1`, the reference render, and `s2` and `s3`, whose elements have `id`s. In `s1`, `#t1` to
`#t10` are its toolbars from the top (`#t5` is `#tsmall`, `#t6` `#tlarge`, `#t7` `#tlong`), `#b1`… its `ion-buttons` in order,
`#ti1` its first title, `#h` and `#f` its header and footer, `#menu` and `#edit` the header's buttons, and `#psolid` and `#pclear`
the plain toolbar's solid and `success` buttons. Throwaway measuring scripts stayed outside the repo.

## Fidelity checklist

`node tools/ionic-api.js` for `ion-header`, `ion-footer`, `ion-toolbar`, `ion-title` and `ion-buttons`.

### Props

| ✓ | Control | Ionic | Avonic | Notes |
|---|---|---|---|---|
| [x] | toolbar | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | title | `color` | `Color` | |
| [x] | title | `size` | `Size` (`TitleSize?`: `Large`, `Small`) | Null is Ionic's `default`. `Small` has md rules. `Large` has none in md, so it looks like the default, as measured (`#tlarge`); its rules are all in `title.ios.css` |
| [x] | header | `collapse` | `Collapse` (`HeaderCollapse?`: `Condense`, `Fade`) | `checkCollapsibleHeader` does nothing unless ios, but md CSS has `.header-md.header-collapse-condense { display: none }` and `getRoleType` gives it no role: it's the header an ios page puts in its content for the large title, and md hides it. `Fade` has no md rule, so it looks like the default |
| [ ] | header, footer | `translucent` | — | Not applicable: only ios renders `.header-background`, and md has no `header-translucent` rule |
| [ ] | footer | `collapse` | — | Not applicable: `checkCollapsibleFooter` returns unless ios, and md has no `footer-collapse-fade` rule |
| [ ] | buttons | `collapse` | — | Not applicable: `buttons-collapse` only has rules in ios CSS, for the collapsible large title |
| [ ] | all five | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods

| ✓ | Control | Ionic | Avonic |
|---|---|---|---|
| [x] | toolbar | default slot | `Content`, `ContentTemplate` |
| [x] | toolbar | `start`, `secondary`, `primary`, `end` | `StartContent`, `SecondaryContent`, `PrimaryContent`, `EndContent`, each with a `…Template` |
| [x] | toolbar | `background` | `PART_Background` (`Border`) |
| [x] | toolbar | `container` | `PART_Container` (`Border`, holding the `FlexRow`: a panel has no padding) |
| [x] | toolbar | `content` | `PART_Content` (`Border`, around the default slot's `ContentPresenter`) |
| [x] | title | (default slot, not in `core.json`) | `Content`, as a `ContentControl` |
| [x] | header, footer, buttons | (default slot, not in `core.json`) | `Items`: they hold many children and `core.json` lists no slot, so `ItemsControl` (mapping rule, ADR 0002) |

No events or methods on any of them. `ion-title`'s `ionStyle` is internal, and only tells the toolbar the title's size for
`toolbar-title-*` classes, which have rules in ios CSS only, so it isn't raised.

### CSS variables

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | toolbar `--background` | `Avonic.Toolbar.Background` | brush | Unset → `Avonic.ToolbarBackgroundBrush` (already global; the background colour) |
| [x] | toolbar `--color` | `Avonic.Toolbar.Color` | brush | Unset → `Avonic.ToolbarColorBrush` → `Avonic.Toolbar.Md.ColorBrush` (`#424242`) |
| [x] | toolbar `--border-color` | `Avonic.Toolbar.BorderColor` | brush | Unset → `Avonic.ToolbarBorderColorBrush` → `Avonic.Toolbar.Md.BorderColorBrush` (`#C1C4CD`) |
| [x] | toolbar `--border-width` | `Avonic.Toolbar.BorderWidth` | `Thickness` | 0 |
| [ ] | toolbar `--border-style` | — | — | Not possible: a `Border` only draws solid lines. `none` is a width of 0 |
| [x] | toolbar `--min-height` | `Avonic.Toolbar.MinHeight` | `double` | 56 |
| [x] | toolbar `--opacity` | `Avonic.Toolbar.Opacity` | `double` | 1, on the background only |
| [x] | toolbar `--padding-top`, `-bottom`, `-start`, `-end` | `Avonic.Toolbar.PaddingTop` … `PaddingEnd` | `double` | 0 each, on the container |
| [x] | title `--color` | `Avonic.Title.Color` | brush | Unset: `--color: initial` makes `color: var(--color)` invalid, so the title inherits |

New global tokens, in `Tokens/Toolbar.axaml`: `--ion-toolbar-color` → `Avonic.ToolbarColorBrush` and `--ion-toolbar-border-color`
→ `Avonic.ToolbarBorderColorBrush`, both `CssUnset`, as they're unset in Ionic. `Avonic.ToolbarColorBrush` must stay unset by
default, because `button.md.css` gives a button in a toolbar `var(--ion-toolbar-color, …)` before its own colour. The literal
fallbacks are `Avonic.Toolbar.Md.*` keys, because `Avonic.TextColor` and `Avonic.BorderColor` are always set here (`#000`,
13% black), where Ionic leaves `--ion-text-color` and `--ion-border-color` unset and falls back to `#424242` and `#c1c4cd`.
`Avonic.ToolbarBackgroundBrush` is always set, which is faithful: Ionic's light palette leaves it unset and falls back to the
same white, and the dark palette sets it.

## Behaviours

Each becomes a test, named after its number.

| # | Behaviour | Source |
|---|---|---|
| **Toolbar** | | |
| T1 | 56 high at least, the full width, `#424242` text on the background colour, no padding of its own | `toolbar.md.css`; measured `#t1` 390×56, `rgb(66,66,66)` on `#fff` |
| T2 | md's slot order: start, the default slot, secondary, primary, end. The default slot's part takes what's left and can shrink to 0; everything is centred vertically | `order: 2, 3, 4, 5, 6`; `.toolbar-content { flex: 1; min-width: 0 }`; measured `#t1`: start 4–52, content 52–282.75, end 282.75–386; `#t2`: content 0–248.58, secondary 248.58–296.58, primary 296.58–386 |
| T3 | RTL mirrors it: start on the right | measured `#t9`: start at 317.19, end at 4 |
| T4 | The keyboard goes through the slots in Ionic's slot order, start, secondary, default, primary, end, whatever the visual order | the shadow tree's slot order; measured with Tab on `s3`: `sta, sec, def, pri, end` |
| T5 | A long title ends with an ellipsis inside what's left | `.toolbar-title`; measured `#tlong` |
| T6 | `Color` paints the background base and the text contrast. An unknown `Color` leaves the background transparent and the text inherited | `:host(.ion-color)`; measured `#t2`, `#t3` |
| T7 | The variables: background, colour, border colour and width, minimum height, background opacity, the four paddings on the container | `toolbar.md.css` |
| T8 | The global `Avonic.ToolbarBackgroundBrush`, `Avonic.ToolbarColorBrush` and `Avonic.ToolbarBorderColorBrush` apply, and the toolbar's own variables beat them | `var(--ion-toolbar-*, …)` |
| T9 | The safe area's left and right are padding on the host, physical sides, with the background under them | `padding-left/right: var(--ion-safe-area-*)`; measured `s2` `#t1`: padding `20 5 0 7`, container at 7 |
| T10 | The first `AvonicButtons` in the start slot gets 4 at its start, and the last one in end, or else primary, or else secondary, gets 4 at its end. Only one of each | `componentWillLoad`; `::slotted(.buttons-first-slot)`; measured `#b1`, `#b2`, `#b4`, `s2` `#bs1`/`#bs2`/`#be1`/`#be2` |
| **Header and footer** | | |
| H1 | A header stacks its toolbars, full width, as tall as they are | measured `s2` `#h`: 390×132 |
| H2 | md's three-layer shadow below it, drawn over what comes after, as `z-index: 10` does. `Classes="ion-no-border"` removes it | `header.md.css`; measured `#h` box-shadow, `s2` `#h` none |
| H3 | The first toolbar in a header gets `Avonic.SafeAreaTop` as top padding, with the background under it, also when a template made it | `ion-header ion-toolbar:first-of-type`; measured `s2` `#t1` 76 high |
| H4 | Automation: a header is the banner landmark | `role="banner"` (`getRoleType`); measured |
| H5 | `Collapse="Condense"` hides the header and drops its landmark. `Fade` looks like the default | `.header-md.header-collapse-condense`; `getRoleType(…, isCondensed, 'md')` |
| F1 | A footer stacks its toolbars, with the same shadow, which `ion-no-border` removes | `footer.md.css`; measured `#f` |
| F2 | The last toolbar in a footer gets `Avonic.SafeAreaBottom` as bottom padding, also when a template made it | `.footer-toolbar-padding ion-toolbar:last-of-type`; measured `s2` `#t6` 66 high |
| F3 | Automation: a footer is the content-info landmark | `role="contentinfo"`; measured |
| **Title** | | |
| Ti1 | 20 at weight 500, letter spacing 0.0125 em, 20 either side, one line ending in an ellipsis, filling the toolbar's content part, centred, inheriting the text colour | `title.md.css`; measured `#ti1`: 52–282.75, text 72–262.75, 23 high |
| Ti2 | `Size="Small"`: 15 at normal weight, and it wraps | `:host(.title-small)`; measured `#tsmall` 17 high, `white-space: normal` |
| Ti3 | `Size="Large"` looks like the default in md | measured `#tlarge` |
| Ti4 | `Color` paints base. Unknown inherits. `Avonic.Title.Color` sets the text | `:host(.ion-color)` |
| **Buttons, and a button in them** | | |
| B1 | A button in `AvonicButtons`, at any depth, with no `Fill` is clear. A `Fill` it's given wins. A button in a toolbar outside `AvonicButtons` stays solid | `fill = this.inToolbar ? 'clear' : 'solid'`, `inToolbar = closest('ion-buttons')`; measured `#menu` clear, `s2` `#loose` solid 36 high |
| B2 | The buttons sit in a row, centred. Each is 32 high at least, padded 8 and 3, 2 apart from its neighbours with no margin above, 2 round, with no shadow, even solid | `buttons.md.css`; measured `#edit` 51.25×32, `#psolid` |
| B3 | Icon-only and clear: 48×48, padded 12, round, no margin, an icon of 1.8 em (25.2), and the unbounded ripple. Icon-only and solid: 32 high, padded 8 and 0 | measured `#menu`, `s2` `#ioo` 41.19×32; `rippleType` |
| B4 | An icon at the start of a button is 1.4 em with 0.3 em after it, and at the end 1.4 em with 0.4 em before it | measured `s2` `#ic1` 19.6, margin 5.88; `#ic2` 7.84 |
| B5 | In an uncoloured toolbar: clear and outline take the toolbar's text colour, and so does outline's border; solid is the toolbar's text colour behind its background colour. A button's own `Color` wins | measured `#t4`: `rgb(66,66,66)`; solid `rgb(66,66,66)` behind white; `#pclear` green |
| B6 | In a coloured toolbar: clear and outline take its contrast, and so does outline's border in `AvonicButtons`; solid is contrast behind base. An outline button directly in a coloured toolbar keeps its own border colour | measured `#t3`; `s4` `#lod` |
| B7 | Focus and hover overlays are the text colour, at md's opacities. Solid in a coloured toolbar: shade on focus, base on hover. Pressed, the overlay is transparent, whatever `Avonic.Button.BackgroundActivated` says | `buttons.md.css`, `--background-activated: transparent`; measured `s3` with Tab: clear `rgb(66,66,66)` 0.12, coloured clear white 0.12, coloured solid `rgb(0,74,205)` 0.24 |
| B8 | A round button keeps its full rounding and the toolbar's padding | measured `s2` `#rnd`: 999, `3px 8px` |
| B9 | With `Avonic.ToolbarColorBrush` set, a button in an uncoloured toolbar, in `AvonicButtons` or not, takes it as text, and as its border if it's outline, and a solid one as its background with `Avonic.ToolbarBackgroundBrush` as text. Not a button with its own `Color`, nor in a coloured toolbar | `:host(.in-toolbar:not(.ion-color):not(.in-toolbar-color))` |
| B10 | An outline button in an uncoloured toolbar has `Avonic.ToolbarBackgroundBrush` as its background while pressed | `:host(.button-outline.ion-activated.in-toolbar…)`, `var(--ion-toolbar-background, var(--color))`; see the deliberate differences for the light palette |

## Design

**`AvonicToolbar` is a `TemplatedControl` with five `ContentPresenter` slots in a `FlexRow`.** The row is already the project's
`display: flex`, so the slots shrink as flex items do and the content part, with `FlexRow.Flex` 1, takes what's left with no
minimum. `FlexRow` has no `order`, so the md template puts the slots in md's visual order. To keep the keyboard on Ionic's slot
order (T4), each slot is its own tab group with a `TabIndex` in slot order, inside a container that's a group itself, so the
indices only count inside the toolbar.

**`PART_Background` fills the host, padding included,** as `.toolbar-background` is absolute inside the padding box, and
`PART_Container` sits inside the host's `Padding`, with the `--padding-*` variables as its own `Padding` around the `FlexRow`.
The host's padding is the safe area, through `Css.SafeAreaBox`, so a header or footer can set its own side, as `padding-top`
does in CSS.

**`AvonicHeader`, `AvonicFooter` and `AvonicButtons` are `ItemsControl`s,** vertical for the first two and horizontal for the
buttons. A control item is its own container, so `c|AvonicHeader > c|AvonicToolbar:nth-child(1)` is `:first-of-type` for the
usual case of toolbars only, and `c|AvonicHeader > ContentPresenter:nth-child(1) > c|AvonicToolbar` is the same for a toolbar a
template made (mapping rule).

**The shadow is on the header's `Border`, with `ZIndex` 10 on the host,** so it draws over the next sibling, as `z-index: 10`
with `position: relative` does. `ion-no-border` is a style class.

**`buttons-first-slot` and `buttons-last-slot` are pseudoclasses the toolbar sets on its `AvonicButtons`,** worked out from
what its slots present, so also an `AvonicButtons` a slot's template made, a panel in a slot standing for the slot. Ionic works
them out once, in `componentWillLoad`. Here they follow the slots' content, but not a panel's children changing later, which
Ionic doesn't follow either.

**A button in `AvonicButtons` is told by styles in `Styles/Toolbar.axaml`, not by `AvonicButton`.** `fill` resolving to clear is
a `Fill` setter at style priority on `c|AvonicButtons c|AvonicButton`, so a `Fill` the app sets wins, and it follows a button
moving in or out, where Ionic only looks once. Every other rule is on the button's parts, as `Styles/Button.axaml` does for an
item. The theme's rules for a fill or a size have a pseudoclass, which beats a global rule without one, so each rule here
has one that's always true for its case (`:has-icon-only` or `:not(:has-icon-only)`, a fill), and then beats the theme because
global styles beat a `ControlTheme`. A button's `Avonic.Button.*` keys stay first in each value, as they do for an item.

**`--color: initial` is inheriting.** In `ion-buttons`, clear and outline set `--color: initial`, which makes the host's
`color: var(--color)` invalid, so it inherits the toolbar's. Here that's `Foreground` bound to the parent's.

**`AvonicTitle` is a `ContentControl` with a `ControlTheme`.** Its md look doesn't depend on what it's in: the colour inherits.
`TextTrimming` and `TextWrapping` inherit, so the host's values reach the `TextBlock` a string makes.

**Automation:** the header's peer reports the `Banner` landmark and the footer's `ContentInfo`. The toolbar, title and
buttons have no role in Ionic, so they keep Avalonia's default peers.

### Extensions

None.

## Deliberate differences

| Difference | Why |
|---|---|
| `collapse` and `translucent` aren't declared | ios only; each would be a dead prop |
| The keyboard's order is Ionic's, but it comes from `TabIndex` on the slots, so an app's own `TabIndex` inside a slot only orders things within that slot | It's how Avalonia groups tab order |
| `ion-header` and `ion-footer` have `order: -1` and `order: 1` in a page's flex column | Avonic has no page yet; the app's layout puts them in place |
| `ion-buttons` has `z-index: 99` | Only matters where a centred ios title overlaps the buttons; md never overlaps |
| A footer's bottom safe area stays while the keyboard is open, and in tabs with a bottom tab bar | Ionic drops `footer-toolbar-padding` then. Avonic has no tabs yet, and the keyboard needs `TopLevel.InputPane`, which headless tests can't open, so it would ship untested. `Avonic.SafeAreaBottom` is 0 unless the app fills it |
| A header in a menu stays a banner | Ionic makes it `none`. No `ion-menu` yet |
| A toolbar's default text size is Avalonia's, where a web page's is 16 | ADR 0002 |
| `:first-of-type` and `:last-of-type` are the first and last child | Avalonia has no of-type selector (mapping rule). A header or footer holds toolbars |
| A coloured toolbar nested inside an uncoloured one: its buttons also match the uncoloured toolbar's `in-toolbar` rules, where Ionic's `:not(.in-toolbar-color)` excludes any coloured toolbar above | Avalonia can't say "no ancestor like this". The coloured rules come later in the file, so they win where both set a value; only `Avonic.ToolbarColorBrush`, when an app sets it, reaches such a button's text. A toolbar in a toolbar is rare |
| A title's ellipsis reaches every `TextBlock` in its content, where CSS's `text-overflow` is on `.toolbar-title` alone, so a block inside it is clipped without one | `TextTrimming` inherits, which is how the `TextBlock` a string makes gets it. A title holds text |
| `TitleSize.Large` and `HeaderCollapse.Fade` change nothing in md | Faithful: Ionic's md has no rules for them. They're kept so that Ionic markup ports. See "For you to decide" in the PR |
| An outline button's border in an uncoloured toolbar or in `AvonicButtons` follows its text, and `Avonic.Button.BorderColor` doesn't reach it, even in the button's own `Resources` | Ionic's `in-toolbar` rule takes `var(--color)` before `var(--border-color)`, and `ion-buttons` sets `--border-color: currentColor` on the button, which beats a global value. A `style` on the button itself beats it in Ionic (measured `s4` `#bod`), which a style rule here can't tell from a global key |
| **Known gap:** with the dark palette, the toolbar's text and the solid button in `AvonicButtons` stay `#424242`, where Ionic's are white, and the border's fallback stays `#C1C4CD` where Ionic's is `#363636` | `Avonic.Toolbar.Md.ColorBrush` and `Avonic.Toolbar.Md.BorderColorBrush` need a line each in `Tokens/Palettes/Dark.axaml`, which is shared, so they're proposed in the PR. Until then an app sets those two keys in its dark resources. Not `Avonic.ToolbarColorBrush`, which Ionic's dark palette doesn't set, and which a button in a toolbar reads before its own colour |
| `Avonic.ToolbarBackgroundBrush` is always set, where Ionic's light palette leaves `--ion-toolbar-background` unset. Two rules read it before a fallback: a pressed outline button directly in an uncoloured toolbar, and a solid one's text there. So in the light palette, a pressed outline button is white, where Ionic's fills with its own colour, primary, with primary text (measured `s4` `#lo`), and one in `AvonicButtons` is white where Ionic's is transparent (`#bo`, `#bo2` on a tinted toolbar). With the dark palette, or an app that sets the key, they match | The key is in `Tokens/Global.axaml`, which is shared, and the toolbar's own background reads it as set. Making it unset in the light palette is proposed in the PR. A solid button's text is white either way unless an app sets `Avonic.Button.Color` |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-progress-bar)` at the bottom of the toolbar | `ion-progress-bar` |
| `in-toolbar` rules of `ion-back-button`, `ion-menu-button`, `ion-segment`, `ion-searchbar`, `ion-breadcrumbs` | those controls, which don't exist yet |
| The ios collapsible large title | ios, not this skin |

## Dependencies

`ion-button` (#10), whose `Fill`, pseudoclasses and parts the rules here target; `ion-icon`; `ion-content` (#11), which a
header and footer sit around; `FlexRow`; `ColorPalette`; `Css`. All exist.
