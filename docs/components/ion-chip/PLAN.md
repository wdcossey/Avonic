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
| 10 | Not a tab stop, and a click doesn't focus it. With `Focusable="True"` (`tabindex`) it is one, and any focus, keyboard or mouse, shows plain 0.16, outline 0.04, colour 0.12, with no focus adorner. Hover on a focused chip is the same 0.16 | `:host(:focus)`, not `ion-focusable`, which the chip doesn't have; measured `clickPlainFocus` active `BODY`, `tab`, `tabc`, `tabo`, `tabFocusAfterClick`, `outline: none` |
| 11 | `Avonic.Chip.Background` beats hover, focus and press on a plain chip; an outline chip ignores it; a coloured chip ignores it and `Avonic.Chip.Color`. `Avonic.Chip.Color` colours the text and the ripple | measured `vb` red through every state, `vbs` from a stylesheet, `vbo` transparent with 0.04 states, `vbc`, `vc` ripple green |
| 12 | A slotted icon: 1.4285714286 em of the chip, so 20; colour `rgba(0, 0, 0, 0.54)` unless the chip has a colour, when it's the shade. First child: margin −4 start, 8 end, −4 top and bottom, so it's at x + 8. Last child: 8 start, −4 end. An only child takes the last child's. A nested icon gets none of it. RTL mirrors | `::slotted(ion-icon)`; measured `i`, `il`, `io` 48 wide with the icon at + 20, `ic`, `ideep` 14 and 0.87, `bigfont` still 20, `rtl` |
| 13 | A long label: the chip grows to its container and the label wraps; an icon beside it shrinks, as `contain: strict` leaves it no minimum | `inline-flex`, `flex-shrink: 1`; measured `long` 350 by 44, label 2 lines; `longi` icon 14.03 wide |
| 14 | Keys do nothing: no Enter or Space, and arrows don't move focus between what's inside | A plain element. Nothing in the chip does this: an `ItemsControl` moves focus with the arrows only between focusable containers in a navigable panel, and a `FlexSlot` in a `FlexRow` is neither. The first build overrode `OnKeyDown` for it, and breaking that changed nothing, even with a `StackPanel`, so it went |
| 15 | In an item's or a divider's start or end slot, the slot sets the side towards the content, and the chip keeps its own 4 on the other three; beside a stacked or floating label, 7 above and below and its 4 at the end. A panel in a slot keeps it at 4. An app's plain `Margin` style beats all of it (#88) | `::slotted([slot=start])` sets `margin-inline-end` only; measured `c-start` `4px 16px 4px 4px`, `c-end` and `dc-end` `4px 4px 4px 16px`, `c-stacked` and `c-floating` `7px 4px 7px 16px` (with plain text in the chip: an `ion-label` in it takes the item's stacked class away, as `ion-item` keys `ionStyle` by tag), `c-panel` `4px`, `c-app` `3px` |

## Design

### The row

`display: inline-flex; align-items: center` with any children is a `FlexRow` as the `ItemsPanel`, with each item in a
`FlexSlot`, which is what a `<slot>`'s child is in an item. The chip makes the containers itself (`NeedsContainerOverride`
and `CreateContainerForItemOverride`), so every child, a control or a string, is in a slot. `align-items: center` is the
slot's `VerticalContentAlignment`, set in code at style priority when the container is prepared: a theme can't reach a
container, and the item's stub sets the same on its slots.

A control in `Items` stays the chip's logical child, not the slot's, so `::slotted(x)` is `c|AvonicChip > c|X` in
`Styles/Chip.axaml`. `:first-child` is `:nth-child(1)` on the child, and that needs the chip to count it: an `ItemsControl`
counts only containers, so a control held in a slot is at index −1 and matches nothing. The first build had the rules on the
presenter, `ContentPresenter:nth-child(1) > c|AvonicIcon`, and none of them applied; the compare step showed black 14 px
icons with no margins. So the chip implements `IChildIndexProvider` again: a child's index is its item's, and a change to
`Items` raises a reset, so that the margins follow children as they come and go. A slot made by an `ItemTemplate` is counted
by its container's index, and the chip raises the reset again when a container's index changes, as the panel moves its
containers after `Items` raises its change. A control's slot has no `ItemTemplate`, so that it shows the control itself, as a
stock `ItemsControl` does by not wrapping it: a presenter with a template builds from it even for a control. CSS counts elements, not text nodes, so an item shown as its text, a
string or any other value with no template, or any item through `DisplayMemberBinding`, isn't counted: `<ion-icon>` then text gives the icon the last child's margins, as it's the only element, and so does text then `<ion-icon>`: measured `-4px -4px -4px 8px` for both. A
string made into a control by a template is one, whether it's the `ItemTemplate` or a `DataTemplate` around the chip: the chip
asks for one as the slot does, with `FindDataTemplate`.

`FlexRow` already does the rest: a label shrinks to its longest word and wraps, and an `AvonicIcon` has no minimum, as
`contain: strict` gives it none, measured `longi`. The theme sets `TextBlock.TextWrapping="Wrap"`, as `white-space: normal`
inherits, so a string child wraps too.

`display: inline-flex` sizes to fit: as wide as its content on one line, or as the space there is when that's too narrow.
Measured at the space, Avalonia's wrapped text reports its longest line, which left the long chip 15 px narrower than
Ionic's 366. So the chip measures its content on one line first, and takes the whole width when it doesn't fit, or more if
the chip's arrange is clamped to the space anyway, so content that can't shrink is clipped there either way. The button had the
same, and fixed it in `ButtonInner`. Only a chip that doesn't fit is measured twice.

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

### The icon's size

`1.4285714286em` of 14 is 20.0000000004, which Avalonia's layout rounding takes up to 21, so every chip with an icon was
1 px wider per icon than Ionic's (the only icon chip measured 49, against 48). Chromium's layout units keep 20. The theme
writes `1.42857142857`, a hair under, which gives 20 at 14 and 40 at 28.

### A label

`chip.md.css` has no `::slotted(ion-label)` rule, and `label.md.css`'s rules are all `:host-context(.item)` and the like, which a
chip isn't. So a label in a chip has no rule of its own: it inherits the chip's colour, size and font, as `AvonicLabel` does
outside an item, and `Styles/Chip.axaml` has only the icon's rules. Measured `p`: the label is 14 px in `rgba(0, 0, 0, 0.87)`, `display: block`, `flex: 0 1 auto`.

### Disabled

`opacity: 0.4` on `:disabled`. A disabled control takes no input in Avalonia and lets a tap through to what's behind, as
`pointer-events: none` does. `aria-disabled` is what the automation peer reports as `IsEnabled`.

### Automation

Chromium gives a chip no role, only its text, as a plain element. An `ItemsControl`'s own peer would call it a list, so the
chip has its own, a `Group` that isn't a content element, as a plain `AvonicCard` is. Its children have their own peers.

## Extensions

Added in #88, which the human decided on 2026-11-08.

**`Click`.** Ionic's chip has no click of its own: an app listens to the host's DOM `click`. Avalonia's `Tapped` is the nearest
built-in, but a second quick tap raises `DoubleTapped` and no `Tapped`, so a filter chip tapped twice quickly loses the second.
`Click` goes through `Behaviors/NativeButton`, as the card's button mode does, so it's the same click: on the release of every
press on the chip, measured on a chip (`clicky`):

| Input | Chromium | `Click` |
|---|---|---|
| A click, then a double click | 3 `click`, 1 `dblclick` | 3 |
| Two quick taps with a finger | 2 | 2 |
| A finger that moved 14 px, 16 px | 1, 0 | 1, 0 |
| A mouse dragged off and back, released off | 1, 0 | 1, 0 |
| A disabled chip | 0 | 0, and 0 if it's disabled during the press |
| Enter or Space on a focused chip | 0 (and the chip takes no focus without `tabindex`) | 0: `NativeButton` takes no keys here |

The rest is `NativeButton`'s, measured for a native button in `ion-item`'s plan: only the left button, a second finger
anywhere means no click, a context menu cancels it, and a child that handles the press, or a handled `Click` from a button
inside, stops it. `Behaviors/NativeButtonTests` runs all of it on the chip too.

**`Command` and `CommandParameter`,** as on `AvonicButton`, `AvonicItem` and `AvonicCard`: `AddOwner` of the button's, run
after `Click` unless a handler set `Handled`, and a command that can't execute disables the chip, which then fades to 0.4.
They follow because `NativeButton` already reads them, an MVVM app has no other way to bind a filter chip, and every other
control with a `Click` here has them. The chip is always clickable, so unlike the card's they always apply.

The chip's automation peer stays a `Group`, as Chromium gives a chip no role, and gains `Invoke`, which raises `Click`: a screen reader's activation is a DOM `click` in a browser, and without it a command-bound chip couldn't be activated by one, as it takes no keys. A disabled chip doesn't answer it.

## Deliberate differences

| Difference | Why |
|---|---|
| A click on a chip leaves focus where it was, where Chromium moves it to the body (measured `clickPlainFocus`: active `BODY`) | A click on what can't take focus leaves it alone in Avalonia, for every control. Clearing it would be the chip reaching outside itself |
| A parent taller than the chip centres it, where an inline chip sits at the top of its line (measured `blk`: `y` 4 in a 100 px block) | The same `Center`, which lines chips of different heights up in a row, as `vertical-align: middle` does. Avalonia has no line box. Set `Top` |
| A disabled ancestor fades the chip to 0.4 too, where a disabled `fieldset` does nothing to an `ion-chip` | A disabled ancestor disables the control in Avalonia, and it should look it, as `ion-button` recorded |
| `VerticalAlignment` is `Center`, where a flex parent stretches an Ionic chip (measured `tall`, 72) | Set `VerticalAlignment="Stretch"`; the children stay centred |
| The chip is an `ItemsControl`, so it also has `ItemsSource` and `ItemTemplate` | As `AvonicList`. Nothing Ionic is shadowed. A control made from the template is the slot's logical child, so each slotted rule has a second alternative, `c\|AvonicChip > ContentPresenter > c\|AvonicIcon`, as `AvonicList` has for its templated items, and the chip counts a slot by its container's index |

## Known gaps

| Gap | Why |
|---|---|
| Roboto isn't installed here, so Chromium's text is Arial and Avalonia's is Segoe UI | Fonts, as every control so far |
| Content that can't shrink is clipped at the container's width, where Ionic's chip grows past it (measured `nw`: 308.84 in 200) | Avalonia arranges a child within its space |

## Not in this PR

| What | Goes with |
|---|---|
| `::slotted(ion-avatar)`: `flex-shrink: 0`, 1.7142857143 em square (24 at 14); first child margin `−8 8 −4 −4`, last child `8 −8 −4 −4` | A PR of its own, now that `ion-avatar` is done, as `c\|AvonicChip > c\|AvonicAvatar` rules in `Styles/Chip.axaml`, with the same care over the factor's rounding. `flex-shrink: 0` is `ClipToBounds` unset and a size of its own, which `FlexRow` doesn't shrink |
| `:host(.toolbar-searchbar) ::slotted(ion-chip)` | `ion-toolbar` |

## Dependencies

`ion-icon` and `ion-ripple-effect` are done, and so are `Activatable`, `ColorPalette`, `Css`, `FlexRow` and `FlexSlot`.
`ion-avatar` is done (#16). `NativeButton` gives `Click` (#88).
