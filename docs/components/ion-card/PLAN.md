# ion-card family — Plan

Five controls in one PR: `AvonicCard`, `AvonicCardHeader`, `AvonicCardTitle`, `AvonicCardSubtitle` and
`AvonicCardContent`. They only make sense together, so they share this folder. Measurements are from real Ionic 8.8.1
in Chromium, md, at 390 px wide.

## Fidelity checklist

`node tools/ionic-api.js ion-card`, then `ion-card-header`, `ion-card-title`, `ion-card-subtitle` and `ion-card-content`.

### `ion-card` props (11)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `button` | `Button` (`bool`) | |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `href` | — | Out of scope: router. Use `Button` and the `Click` extension |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only apply with `href` |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [ ] | `type` | — | Not applicable: HTML form |

### `ion-card-header`, `ion-card-title`, `ion-card-subtitle`, `ion-card-content` props

| ✓ | Control | Ionic | Avonic | Notes |
|---|---|---|---|---|
| [x] | header | `color` | `Color` | |
| [ ] | header | `translucent` | — | Not applicable: only applies in ios. `card-header-translucent` has no md rule, so it would be a dead prop |
| [x] | title | `color` | `Color` | |
| [x] | subtitle | `color` | `Color` | |
| [ ] | all four | `mode` | — | Not exposed (ADR 0001) |

### Slots, parts, events, methods

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot, on all five | `Content`, `ContentTemplate` (`ContentControl`) |
| [x] | `native` on `ion-card` | `PART_Native` (`Border`) |

No events or methods on any of them. Other parts of the card are `PART_Host`, which draws the shadow outside the clip,
and `PART_Ripple`.

### CSS variables (4)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `ion-card` `--background` | `Avonic.Card.Background` | brush | Unset → `Avonic.CardBackgroundBrush` |
| [x] | `ion-card` `--color` | `Avonic.Card.Color` | brush | Unset → `Avonic.CardColorBrush` |
| [x] | `ion-card-title` `--color` | `Avonic.CardTitle.Color` | brush | Unset → `Avonic.TextColorStep150Brush` |
| [x] | `ion-card-subtitle` `--color` | `Avonic.CardSubtitle.Color` | brush | Unset → `Avonic.TextColorStep450Brush` |

New global token, only read here: `--ion-card-color` → `Avonic.CardColorBrush`, following `Avonic.TextColorStep450`.
`--ion-card-background` is `Avonic.CardBackgroundBrush`, which `Tokens/Global.axaml` and the dark palette already have.

One key has no CSS variable: `Avonic.Card.BoxShadow`. Ionic hard-codes the shadow, but it needs colours, and rule 5 keeps
those in `Tokens/`. `ion-item`'s `Avonic.ItemIconBrush` is the precedent.

`ion-card-header` declares `--background: transparent` and `--color: inherit` privately. `core.json` doesn't list them, so
no keys. The header's `--color: inherit` inherits the card's `--color`, which is what the card's text is unless the
app sets `color` by hand, so here the header inherits `Foreground`.

## Behaviours

Each becomes a test, named after its number.

| # | Behaviour | Source |
|---|---|---|
| 1 | `Button` makes the card activatable: `:activated`, a ripple, a `Hand` cursor | `isClickable`; `button, a { cursor: pointer }`; measured |
| 2 | A plain card has none of those, and turning `Button` off removes them | `renderCard`: only a slot; measured |
| 3 | A tap on a `Button` card raises `Click`, on release, every time. Not with the secondary button, not on a plain or disabled card, including one disabled during the press, and not when released off it or taken by a gesture | Extension; measured `three quick clicks` (one per click), `release outside` (none), `disbtn` (none) |
| 4 | A tap on a child inside a `Button` card also raises `Click`, unless the child handles it. A tap on a nested `Button` card reaches each card once | measured: `in<inner-btn`; `inner<, outer<` |
| 5 | Enter (on press) and Space (on release) act as a tap when the card has focus | native `<button>`; measured `enter down`, `space up` |
| 6 | A `Button` card is a tab stop; a plain or disabled one isn't | measured tab order |
| 7 | Hover and keyboard focus change nothing: no overlay, no `:ion-focused`, no outline, no adorner | `.card-native { outline: none }`, no `ion-focusable` class; measured: `:focus-visible` is true, classes and colours are the same |
| 8 | The ripple fills the card, draws over the content, clips to its corners and uses the current colour, including contrast on a coloured card. A plain card or item inside doesn't take it | `--ripple-color` is undefined, so `color` inherits; `getRippleEffect` looks in the host's shadow root first; measured `10,210,370,90`, `rgb(115, 115, 115)`, `rgb(255, 255, 255)` |
| 9 | Card box: margin 10, radius 4, the three-layer shadow, clipped content, font size 14, the theme's font, text wraps | `card.md.css`; measured |
| 10 | Card text and background default to `#737373` on the background colour | measured |
| 11 | `Avonic.Card.Background`, `Avonic.Card.Color` and the global `Avonic.CardBackgroundBrush`, `Avonic.CardColorBrush` apply, with the card's own winning | measured `cvar`, `ivar1` |
| 12 | `Color` paints base and contrast. An unknown `Color`: a transparent background and inherited text. No `Color` keeps the defaults, even in a coloured ancestor | measured `danger`, `bad`, `bad2` |
| 13 | Disabled: opacity 0.3, no input, no tab stop | `.card-disabled`; measured |
| 14 | Automation: a `Button` card is a button that can be invoked, named by `AutomationProperties.Name` or the visible text inside. A plain one can't be invoked and takes no name from its text | native `<button>`; `inheritAttributes(['aria-label'])`; measured `btnlabel`, and accessibility tree names `Outer Inner`, `Visible` |
| 15 | Header: padding 16, nothing else of its own. It inherits the text colour and size, in or out of a card | measured `full-h`, `alone-h`, `cvar-h` |
| 16 | Header `Color` paints base and contrast. An unknown `Color`: transparent, or an ancestor's base, and inherited text | measured `hc-h`, `hb-h`, `d-h` |
| 17 | A title or subtitle in a header has 8 above it unless it's first, whatever came first, and it follows a change. Nested deeper, none | `::slotted(...:not(:first-child))`; measured `full-s`, `sub1-t`, `div1-t`, `nest1-t` |
| 18 | Title: size 20, weight 500, line height 1.2 of its own size, also for a `TextBlock` directly in it or in a panel with its own size, `#262626`, without clipping descenders | measured `full-t`, `e-t` |
| 19 | Subtitle: size 14, weight 500, normal line height, `#737373` | measured `full-s` |
| 20 | `Avonic.CardTitle.Color` and `Avonic.CardSubtitle.Color` set the text | measured `tvar`, `svar` |
| 21 | Title and subtitle `Color` paints base. An unknown `Color` inherits | `:host(.ion-color)`; measured `tc-t`, `tc-s`, `tb-t`, `tb-s` |
| 22 | Inside a coloured card or coloured header, at any depth, a header, title or subtitle inherits the text colour, beating its own `Color`. A coloured header keeps its own background. A coloured item doesn't do this | `ion-card.ion-color .ion-inherit-color`, `ion-card-header.ion-color .ion-inherit-color`; measured `danger-t`, `both-t`, `a-h`, `b-t`, `c-t`, `bad-t`, `o-i`, `ii-t` |
| 23 | Automation: a title is a heading at level 2, and a subtitle at level 3, named by the text inside | `role="heading"`, `aria-level` |
| 24 | Content: padding 13 and 16, size 14, line height 1.5 of each `TextBlock`'s own size, directly in it or in a panel, without clipping, inherited text colour, in or out of a card. Text wraps | `card-content.md.css`; measured `co-c`, `alone-c`, `long` |
| 25 | Content straight after a header has no top padding, in or out of a card, even if the header is hidden, and it follows a change. Anything between them stops it | `ion-card-header + .card-content-md`; measured `full-c`, `adj-c`, `gap-c`, `content after removing the gap`, `after hiding the header` |
| 26 | `h1`–`h6` and `p`: sizes 24, 16, 14, 14, 14, 14, 14; weight normal; margins; line heights 1.2, or 1.5 for `p`, of their own size, without clipping. Directly in the content or in a panel directly in it | measured `h1`…`p`, `e-p`, `e-h` |
| 27 | RTL: text starts at the right, and the boxes stay symmetrical | measured `rtl` |
| 28 | The family together: a card with header and content has the header above, the content below, no gap and the full width | measured `full`: `10,36,370,114`, with the header 80 and the content 34 |

## Design

**All five are `ContentControl`s.** Each has one slot, and `AvonicLabel` settled that: the content is a logical child of
the host, so `c|AvonicCardContent > TextBlock.p` is the rule with no `/template/`, and automation finds the text.

**The card has a `ControlTheme` in `Controls/AvonicCard.axaml`, and the other four are in `Styles/Card.axaml`.** The card's
look depends on nothing around it. The others nearly all do, and `Styles` beat a `ControlTheme`, so like `AvonicLabel` they
have no theme of their own, and `Design.PreviewWith` is in the `Styles` file too.

**The card's interaction is copied from `AvonicItem`,** as this control can't edit it: click on release every time, the
second-press handling, Enter and Space, `Focusable` at style priority, the automation peer. Forwarding to a first
interactive child and the `IonStyle` handling aren't copied, because Ionic's card has neither. A shared helper is listed
under "Proposed for shared files" in the PR.

**The ripple is first in the template's panel, with a `ZIndex` to stay on top.** `Activatable` searches depth first and goes into
controls that aren't activatable, so with the ripple last it found a plain child's hidden ripple, and the card showed
none. Ionic looks in the host's shadow root first. `AvonicItem` has the same problem with a plain card inside it. See the PR.

**A second quick press uses the same movement limit as a first.** `AvonicItem` only checks that it's released on the item,
so there a finger sliding on a second press clicks. Here it doesn't, as neither does a first press.

**A nested `Button` card doesn't raise a second `Click`.** A browser has one click, which reaches each card on its way up.
Here the inner card's `Click` already bubbles through the outer one, so raising another would run its handlers twice.

**No hover or focus state.** The card sets `Activatable` but not `FocusVisible`, because Ionic doesn't set
`ion-focusable`, and `card.md.css` has no `.ion-focused` or `:hover` rule. The result is that keyboard focus on a card is
invisible, as it is in Ionic. It's flagged in the PR.

**The shadow needs a part outside the clip.** `overflow: hidden` clips the children but not the host's own
`box-shadow`, while `ClipToBounds` on a `Border` clips what it draws too. So `PART_Host` draws the shadow and
`PART_Native` clips. Both take the host's `CornerRadius`. `Background` is the host's own property, so an app can set it
directly.

**`ion-card-header + .card-content-md` is a pseudoclass the control sets.** Avalonia has no adjacent sibling selector.
`AvonicCardContent` watches its parent panel's `Children` and sets `:after-card-header` when the one before it is a header.
A hidden header still counts, as `+` ignores `display: none`.

**A header and content in an `ItemsControl` aren't siblings.** Its containers wrap each one, which in CSS terms is a wrapper
`div`, and that stops `+` too. Only a `Panel`'s `Children` counts.

**`:first-child` is `:nth-child(1)`, in the panel that stands for the slot.** `Panel` provides the index, and the
selector follows changes.

**`.ion-inherit-color`** is a descendant selector on our own types, so other types don't pay for it. It uses `c|AvonicCard`
and `c|AvonicCardHeader` by name, as Ionic's rule does, so a coloured item doesn't trigger it.

**`h1`–`h6` and `p`** are `TextBlock` style classes with `>` and `> :is(Panel) >`, as in `Styles/Label.axaml`, with no
descendant selector on a `TextBlock`. `line-height` is a factor, so it's `Css.Em` on the element's own `FontSize`.

### Extensions

- **`Click`** on `AvonicCard`, a routed event, as on `AvonicItem`. It isn't named `IonClick`, because Ionic has no such
  member.

`Command` isn't included. It follows #10.

## Deliberate differences

| Difference | Why |
|---|---|
| `href`, `download`, `rel`, `target`, router props and `type` aren't declared | Each would be a dead prop |
| `translucent` isn't declared | It does nothing in md |
| One child per slot | Mapping rule. Wrap several in a panel, which then stands for the slot |
| Two cards in a `StackPanel` are 20 apart, where CSS collapses their margins to 10 (measured `gap between siblings`) | Avalonia has no margin collapsing, as `ion-label` records. `Spacing="-10"` on the panel gives 10 |
| Margins between two headings add up to 4, where CSS collapses them to 2 | The same. `h1` or `h2` followed by `p`, the usual pairs, are 2 in both |
| `h1`–`h6` and `p` get their rules directly in the content or in a panel directly in it, where Ionic's reach any depth (measured `d-h2`, `d-p`) | A descendant selector on `TextBlock` is checked for every `TextBlock` in the app. `ion-label` measured it at about 13% on attach. Set the values yourself deeper down |
| A `TextBlock` nested deeper than a panel with its own `FontSize` gets line height 21, not 1.5 of its size, and can clip. So does a subtitle in the content with its own `FontSize`. In a title, it's 24 rather than 1.2 of its size | `TextBlock.LineHeight` inherits as a size. The same as `ion-label` |
| `--ion-item-background` and `--ion-item-color` aren't in the fallback (measured `ivar2`: Ionic follows both) | `Avonic.ItemColorBrush` always has a value, following the text colour, where `--ion-item-color` is unset by default, so reading it would turn every card's text black. `Avonic.CardBackgroundBrush` is always set by `Global.axaml` too. See "Proposed for shared files" |
| A disabled plain card also disables what's inside it, including tab stops (measured: a nested `input` is still in the tab order) | `disabled` is `IsEnabled` by the mapping rule, and that cascades. `pointer-events: none` already stops the pointer in both |
| `PART_Native` always exists, where Ionic only renders it for `button` and `href` | It carries the clip and the background. With no `Button` nothing differs |
| **Known gap:** a mouse drag within a `Button` card doesn't click, past Avalonia's small movement limit. Ionic clicks (measured: down, move 60 and 130, up). Touch is meant to match, as a finger that moves is scrolling in both, but this was only run with a mouse | `Tapped` is what the click comes from, copied from `AvonicItem`. The fix is to track press and release directly, which belongs in one shared place. See the PR |
| **Known gap:** a child that handles `Tapped` stops only the first of two quick taps, and has to handle `DoubleTapped` as well. Ionic stops both (measured) | The same |
| **Known gap:** above 1× the shadow's offset and spread don't scale, so a card's bottom edge is lighter and its top a little darker. Measured at 3×, straight below a card: 212 against Chromium's 166. At 1× it matches | Avalonia applies them in device pixels. The numbers are in [COMPARISON.md](COMPARISON.md) |
| Keyboard activation of a nested control doesn't raise the card's `Click` | As on `AvonicItem`. In a browser the click bubbles. Pointer taps do match |
| `Click` is only raised by `Button` cards | As on `AvonicItem`. On the web any element gets `click`. Use `Tapped` for the others |
| A `:disabled` ancestor fades the card too | As on `AvonicItem`: the pseudoclass follows `IsEffectivelyEnabled`. Ionic's class only follows the card's own prop |

## Not in this PR

| What | Goes with |
|---|---|
| `ion-card .list-md { margin: 0 }` | #9, `ion-list`, in its own `Styles/` file |
| The default text size outside a host: Avalonia's, where a web page's is 16. A header outside a card inherits it | Its own decision, already raised at `ion-icon` |
| `--ion-safe-area-*`: `:host` resets them to 0 for what's inside a card | #11, `ion-content`, which defines them |
| `contain: content` | A rendering optimisation in CSS, with no equivalent to need |

## Dependencies

`ion-ripple-effect`, `ion-item` (for `Css` and the tests' pattern) and `ion-label` (for its patterns, and the `h1`–`p` class
names). `Activatable` and `ColorPalette` exist.
