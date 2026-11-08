# ion-chip — Plan

A small activatable box holding a row of children: an icon, a label, an avatar. Ionic's chip has four props, two variables
and no events, so most of this plan is which state beats which, and how the row holds its children. Measurements are from
real Ionic 8.8.1 in Chromium, md, at 390 px wide, wrapped in `ion-app` so that `tap-click` runs. States were measured
with a mouse in a context with `any-hover: hover`, and with a finger (CDP touch events) in a mobile context without it, as
a phone has neither hover nor `:hover`.

## Fidelity checklist

`node tools/ionic-api.js ion-chip`.

### Props (4)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `disabled` | `IsEnabled` | Mapping rule. `aria-disabled`, `opacity: 0.4`, `pointer-events: none` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `outline` | `Outline` (`bool`) | Pseudoclass `:outline` |

### Events, methods, slots, parts

None in `core.json`. The default slot holds several children in a row, and `core.json` lists no slot, so it's **`Items`**
(ADR 0002: a control holding many children, with no slot in `core.json`, is `Items`, not `Content`). `AvonicButton` has
`Content` because its slots are in `core.json`; `AvonicList` has `Items` for the same reason as this. The chip is an
`ItemsControl`, as the list is, so a string is a child too, as a text node is in `<ion-chip>Plain text</ion-chip>`
(measured `txt`: 81.59 wide).

No shadow parts. The shadow root is `<slot>` then `<ion-ripple-effect>`, md only. Template parts here are `PART_Native`
(the host's box: background, border, radius, clip) and `PART_Ripple`.

### CSS variables (2)

| ✓ | Ionic | Key | Type | md default |
|---|---|---|---|---|
| [x] | `--background` | `Avonic.Chip.Background` | brush | `CssUnset` → `rgba(text, 0.12)`, and the states below. Set, it beats every state on a plain chip, and does nothing on an outline or coloured one, measured `vb`, `vbs`, `vbo`, `vbc` |
| [x] | `--color` | `Avonic.Chip.Color` | brush | `CssUnset` → `rgba(text, 0.87)`. Ignored on a coloured chip, measured `vbc` |

`chip.md.css` gives the states by setting `--background` itself, so an app's value for the variable wins over them: the outer
tree beats `:host(...)` in the cascade. That's what `CssUnset` plus `Css.Var` gives here: the key, then the state's own
`Avonic.Chip.Md.*` value. The `Md` keys hold the `rgba(var(--ion-text-color-rgb), n)` values, which have no variable in Ionic,
so they're brushes on `Avonic.TextColor` with an `Opacity`, as `Avonic.ItemIconBrush` is.

## Behaviours

Each becomes a test. The ids are the chips in the measurement scene.

| # | Behaviour | Source |
|---|---|---|
| 1 | A row as wide as its content: `Default` is 68.36 by 32, with padding 12 and 6, min-height 32, margin 4, radius 16, font 14 at weight 400 in `--ion-font-family`, and the children centred: a 16 px label sits at y + 8 | `:host`; measured `p` |
| 2 | Plain: background `rgba(0, 0, 0, 0.12)`, text `rgba(0, 0, 0, 0.87)` | `--background`, `--color`; measured `p` |
| 3 | Outline: transparent, a 1 px border `rgba(0, 0, 0, 0.32)`, with the padding inside it (border-box): the label is at x + 13, and the height stays 32 | `.chip-outline`; measured `o` |
| 4 | `Color`: background `rgba(base, 0.08)`, text and icon the shade. Outline too: transparent with a border `rgba(base, 0.32)` | `.ion-color`; measured `c` `rgba(0, 84, 233, 0.08)`, `rgb(0, 74, 205)`; `co`; `ic` icon `rgb(173, 0, 13)` |
| 5 | An unknown `Color`: transparent, text inherited, and an outline's border is the current colour. Inside a coloured ancestor it paints that ancestor's base and shade | `var()` invalid at computed time; `--ion-color-base-rgb` inherits; measured `cu` `rgb(0, 0, 0)`, `cuo` border `rgb(0, 0, 0)`, `incol` `rgba(197, 0, 15, 0.08)` and `rgb(173, 0, 13)` |
| 6 | Disabled: opacity 0.4, no input, a tap goes to what's behind, no activation. A disabled ancestor fades it too | `.chip-disabled`; measured `d`, `d2`: `elementFromPoint` is `behind`, and a click reaches it |
| 7 | Activatable: `:activated` on press, after 100 ms or at once on a quick tap, and a bounded ripple from the press point in the current colour, as big as the padding box and clipped by the radius. Off with `AvonicConfig.RippleEffect` | `ion-activatable`, md ripple; measured ripple `type: bounded`, `rgba(0, 0, 0, 0.87)` at 0.16, 68.36 by 32; 68.36 by 30 inside an outline's border; `c` shade; `vc` `rgb(0, 128, 0)`; `cu` `rgb(0, 0, 0)`; `quickTap` activated |
| 8 | Activated background, with a finger: plain 0.2, outline 0.08, colour 0.16 with or without outline, and an app's `--background` stays | `.ion-activated`; measured by touch `p`, `o`, `c`, `co`, `vb` |
| 9 | Hover, mouse and pen only: plain 0.16, outline 0.04, colour 0.12 with or without outline. It beats a press: a mouse press shows the hover value, not the activated one | `@media (any-hover: hover)`, after `.ion-activated` in file order at equal specificity; measured `p` press 0.16, `o` 0.04, `c` 0.12; mobile `any-hover: none` |
| 10 | Not a tab stop and a click leaves focus where it was. With `Focusable="True"` (`tabindex`) it is one, and any focus, keyboard or mouse, shows plain 0.16, outline 0.04, colour 0.12, with no focus adorner. Hover on a focused chip is the same 0.16 | `:host(:focus)`, not `ion-focusable`, which the chip doesn't have; measured `clickPlainFocus` active `BODY`, `tab`, `tabc`, `tabo`, `tabFocusAfterClick`, `outline: none` |
| 11 | `Avonic.Chip.Background` beats hover, focus and press on a plain chip; an outline chip ignores it; a coloured chip ignores it and `Avonic.Chip.Color`. `Avonic.Chip.Color` colours the text and the ripple | measured `vb` red through every state, `vbs` from a stylesheet, `vbo` transparent with 0.04 states, `vbc`, `vc` ripple green |
| 12 | A slotted icon: 1.4285714286 em of the chip, so 20; colour `rgba(0, 0, 0, 0.54)` unless the chip has a colour, when it's the shade. First child: margin −4 start, 8 end, −4 top and bottom, so it's at x + 8. Last child: 8 start, −4 end. An only child takes the last child's. A nested icon gets none of it. RTL mirrors | `::slotted(ion-icon)`; measured `i`, `il`, `io` 48 wide with the icon at + 20, `ic`, `ideep` 14 and 0.87, `bigfont` still 20, `rtl` |
| 13 | A long label: the chip grows to its container and the label wraps; an icon beside it shrinks, as `contain: strict` leaves it no minimum | `inline-flex`, `flex-shrink: 1`; measured `long` 350 by 44, label 2 lines; `longi` icon 14.03 wide |
| 14 | Keys do nothing: no Enter or Space, and arrows don't move focus between what's inside | A plain element. An `ItemsControl` would move focus with the arrows |

## Design

### The row

`display: inline-flex; align-items: center` with any children is a `FlexRow` as the `ItemsPanel`, with each item in a
`FlexSlot`, which is what a `<slot>`'s child is in an item. The chip makes the containers itself (`NeedsContainerOverride`
and `CreateContainerForItemOverride`), so every child, a control or a string, is in a presenter, and `::slotted(x)` is
`c|AvonicChip > ContentPresenter > c|X` in `Styles/Chip.axaml`, as it is for an item. `:first-child` is
`ContentPresenter:nth-child(1) > c|AvonicIcon`, because the presenter is the chip's child, and the chip is the index
provider. `align-items: center` is the slot's `VerticalContentAlignment`, set in code at style priority when the container
is prepared: a theme can't reach a container, and the item's stub sets the same on its slots.

`FlexRow` already does the rest: a label shrinks to its longest word and wraps, and an `AvonicIcon` has no minimum, as
`contain: strict` gives it none, measured `longi`. The theme sets `TextBlock.TextWrapping="Wrap"`, as `white-space: normal`
inherits, so a string child wraps too.

`display: inline-flex` is `HorizontalAlignment="Left"`. `vertical-align: middle` is the control's default
`VerticalAlignment`, `Center`, by `OverrideDefaultValue`, as `AvonicButton` does, so that a slot's `align-items` still reaches
it. A flex parent stretches an Ionic chip (measured `tall`: 72 high, label centred); an app sets `Stretch` for that.

### Which state beats which

Avalonia has no specificity count: a rule with a pseudoclass beats one without, then later beats earlier. `chip.md.css`'s
state rules for each kind of chip have equal specificity, in the order focus, activated, hover, so hover beats a press. The
theme keeps that order, in three groups, so that no group's rules fall on another's:

1. Plain: `:focus`, `:activated`, `:ion-hover`, each `Css.Var` of `Avonic.Chip.Background` then the state's `Md` key.
2. Outline: `:outline` (border, transparent), then its `:focus`, `:activated`, `:ion-hover` with the 0.04 and 0.08 values.
   Later than the plain rules, so `background: transparent` beats `--background`, as measured `vbo`.
3. Colour: `:ion-color` (0.08, shade), `:ion-color:outline` (transparent, border 0.32 or the current colour), then
   `:ion-color:focus`, `:activated`, `:ion-hover` at 0.12, 0.16, 0.12. Later than the outline rules, so a coloured
   outline chip shows the colour's states over its transparent rest, as `:host(.ion-color:focus)` beats
   `:host(.chip-outline)` by specificity.

`rgba(var(--ion-color-base-rgb), n)` is `ColorPalette.WithAlpha` on `ColorPalette.BaseColor`, which is null for an unknown
colour, so the background goes transparent, as the invalid value does in CSS. The border on an unknown outline colour is
`currentcolor`, the initial value of `border-color`, so it's `Css.Var` of that brush then the chip's own `Foreground`.

### Focus

The chip is `ion-activatable` but not `ion-focusable`, and has no `tabindex`, so `:host(:focus)` is CSS focus: dead
until an app makes the chip focusable, and then shown for any focus, a mouse click included. So the theme uses `:focus`,
not `:ion-focused`, and the control doesn't attach `FocusVisible`. `Focusable` stays Avalonia's default, false, and an app
sets it as it would set `tabindex`. `outline: none` is `FocusAdorner` `{x:Null}`.

### Ripple

`ion-ripple-effect` comes after the slot in the shadow root, so it paints over the children. Here it's first in the panel
with `ZIndex="1"`, as `AvonicCard` has it, so that `Activatable` finds it before any ripple in the children, as Ionic looks in
the shadow root. Its colour is `currentColor`, which `Foreground` inherits; there's no `--ripple-color` on a chip. It
fills the panel inside the border, as `position: absolute` fills the padding box, and `PART_Native` clips it to the radius.

### Disabled

`opacity: 0.4` on `:disabled`. A disabled control takes no input in Avalonia and lets a tap through to what's behind, as
`pointer-events: none` does. `aria-disabled` is what the automation peer reports as `IsEnabled`.

## Extensions

None. Ionic's chip has no click of its own: an app listens to the host's DOM `click`, which a plain element raises on a
press and release on it, measured: a click, a double click and a drag within it click once each, and a drag off doesn't.
Avalonia's `Tapped` on the control is the nearest built-in, and `PointerReleased` is exact. A `Click` extension through
`NativeButton`, as the card's button mode has, is for the human to decide: it would be the first on a control with no native
`<button>`.

## Deliberate differences

| Difference | Why |
|---|---|
| A disabled ancestor fades the chip to 0.4 too, where a disabled `fieldset` does nothing to an `ion-chip` | A disabled ancestor disables the control in Avalonia, and it should look it, as `ion-button` recorded |
| `VerticalAlignment` is `Center`, where a flex parent stretches an Ionic chip (measured `tall`, 72) | Set `VerticalAlignment="Stretch"`; the children stay centred |
| A string child counts as a child for `:first-child` and `:last-child`, where a text node doesn't in CSS | Every item is in a presenter. `<ion-icon>` then text then `<ion-icon>` (`ib2`) still gives both icons their margins, as the icons are first and last either way |
| A chip in an item's start or end slot has margin `0,0,16,0` or `16,0,0,0`, where Ionic keeps the chip's 4 on the other three sides (measured `initemend`: `4 4 4 16`) | `Styles/Item.axaml`'s slot rule sets all four sides for any control, and merges after this control's file. 4 px at the outer edge; see the proposal in the PR |
| The chip is an `ItemsControl`, so it also has `ItemsSource` and `ItemTemplate` | As `AvonicList`. Nothing Ionic is shadowed |

## Known gaps

| Gap | Why |
|---|---|
| Automation: Chromium gives the chip no role, just its text, and Avalonia's default peer for an `ItemsControl` reports nothing either | Nothing to map; a chip's `aria-disabled` isn't reported |
| Roboto isn't installed here, so Chromium's text is Arial and Avalonia's is Segoe UI | Fonts, as every control so far |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-avatar)`: `flex-shrink: 0`, 1.7142857143 em square (24 at 14); first child margin `−8 8 −4 −4`, last child `8 −8 −4 −4` | `ion-avatar` (#16), as `c\|AvonicChip > ContentPresenter > c\|AvonicAvatar` rules in `Styles/Chip.axaml`, once the type exists |
| `:host(.toolbar-searchbar) ::slotted(ion-chip)` | `ion-toolbar` |

## Dependencies

`ion-icon` and `ion-ripple-effect` are done, and so are `Activatable`, `ColorPalette`, `Css`, `FlexRow` and `FlexSlot`.
`ion-avatar` is in progress (#16).
