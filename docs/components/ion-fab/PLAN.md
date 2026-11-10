# ion-fab family — Plan

`ion-fab`, `ion-fab-button` and `ion-fab-list` ([#30](https://github.com/wdcossey/Avonic/issues/30)): a floating action button,
placed over `ion-content` from its `fixed` slot, which opens lists of smaller buttons beside it. Measurements are from real Ionic
8.8.1 in Chromium, md, at 390 × 844, wrapped in `ion-app` so that `tap-click` runs. The reference page is in [`ref/`](ref/), with
its command in [`COMPARISON.md`](COMPARISON.md). The mobile context has no `any-hover`, so hover is read from the CSS.

## Fidelity checklist

### ion-fab — `node tools/ionic-api.js ion-fab`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` (`bool`) | Sets the first fab button's and every list's `Activated`, on change and at load |
| [x] | `edge` | `Edge` (`bool`) | `:edge`. Half the button over the header or footer |
| [x] | `horizontal` | `Horizontal` (`FabHorizontal?`: `Start`, `Center`, `End`) | `:horizontal-start` … |
| [x] | `vertical` | `Vertical` (`FabVertical?`: `Top`, `Center`, `Bottom`) | `:vertical-top` … |
| [x] | `close()` | `Close()` | `Activated = false`. Sync: there's nothing to wait for |
| [x] | `toggle()` (internal) | `internal Toggle()` | `@internal` in Ionic: the fab button calls it |

No events, slots, parts or variables. The default slot holds the button and the lists, and `core.json` lists no slot, so it's
**`Items`**, as `ion-list`'s is. A control is its own container, so the button and the lists are the fab's logical children.

### ion-fab-button — `node tools/ionic-api.js ion-fab-button`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` (`bool`) | `:close-active`: the close icon in, the content out. Not `:activated`, which is `Activatable`'s press |
| [x] | `closeIcon` | `CloseIcon` (`string`, `"close"`) | `PART_CloseIcon`'s `Icon` |
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `disabled` | `IsEnabled` | Mapping rule. Opacity 0.5, no input |
| [ ] | `download`, `rel`, `target` | — | Not applicable: they only apply with `href`, as on `ion-button` |
| [ ] | `href` | — | Out of scope: router, as on `ion-button`. Use `Click` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [x] | `show` | `Show` (`bool`) | `:show`. Only does anything in a list, measured |
| [x] | `size` | `Size` (`FabButtonSize?`: `Small`) | `:size-small` |
| [ ] | `translucent` | — | Not applicable: only ios draws it (`backdrop-filter` has no md value), as on `ion-header` |
| [ ] | `type` | — | Not applicable: HTML form |
| [x] | `ionBlur`, `ionFocus` | `IonBlur`, `IonFocus` | On the button itself, as on `AvonicButton` |
| [x] | default slot | `Content` | One slot, so a `ContentControl`: the content is the button's logical child |
| [x] | part `native` | `PART_Native` | |
| [x] | part `close-icon` | `PART_CloseIcon` | An `AvonicIcon` |

CSS variables, keys `Avonic.FabButton.*`:

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Background` | Unset → primary; in a list, light |
| [x] | `--background-activated` | `BackgroundActivated` | Unset → transparent |
| [x] | `--background-activated-opacity` | `BackgroundActivatedOpacity` | Unset → 0 |
| [x] | `--background-focused` | `BackgroundFocused` | Unset → the current colour; in a list, light shade |
| [x] | `--background-focused-opacity` | `BackgroundFocusedOpacity` | Unset → 0.24 |
| [x] | `--background-hover` | `BackgroundHover` | Unset → the current colour; in a list, light tint |
| [x] | `--background-hover-opacity` | `BackgroundHoverOpacity` | Unset → 0.08 |
| [x] | `--border-color` | `BorderColor` | Unset → the current colour (`initial`) |
| [x] | `--border-radius` | `BorderRadius` | `50%` |
| [ ] | `--border-style` | — | Not applicable: Avalonia's `Border` only draws solid, as on `ion-button` |
| [x] | `--border-width` | `BorderWidth` | 0 |
| [x] | `--box-shadow` | `BoxShadow` | Unset → 6 dp; 12 dp while pressed |
| [x] | `--close-icon-font-size` | `CloseIconFontSize` | 24 |
| [x] | `--color` | `Color` | Unset → primary contrast; in a list, `rgba(text, 0.54)` |
| [x] | `--color-activated`, `--color-focused`, `--color-hover` | `ColorActivated`, `ColorFocused`, `ColorHover` | Unset → `--color` |
| [ ] | `--padding-top`, `--padding-end`, `--padding-bottom`, `--padding-start` | — | Do nothing in Ionic: every child of `.button-native` is `position: absolute`, so the padding moves nothing. Measured: with 20 px of padding, `.button-inner` stayed at 324, 534, 56 by 56 |
| [x] | `--ripple-color` | `RippleColor` | Unset → the current colour |
| [x] | `--transition` | `Transition` | Box shadow, background and colour, 280 ms `cubic-bezier(0.4, 0, 0.2, 1)`. Its opacity and transform parts animate nothing: `.button-native`'s never change |

### ion-fab-list — `node tools/ionic-api.js ion-fab-list`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `activated` | `Activated` (`bool`) | `:active` (`fab-list-active`): shown, and its buttons shown one by one |
| [x] | `side` | `Side` (`FabListSide`: `Bottom` default, `Top`, `Start`, `End`) | `:side-bottom` … |

No events, methods, slots, parts or variables. The buttons are **`Items`**, for the same reason as the fab's.

## Behaviours

Each becomes a test. Measured on the page in `ref/` and with throwaway scripts.

### Fab

| # | Behaviour | Source |
|---|---|---|
| F1 | `Horizontal` and `Vertical` place it in its container, which is `ion-content`'s box from the `fixed` slot: start and end 10 px in, plus `Avonic.SafeAreaLeft` or `Right` (swapped in RTL); top and bottom 10 px in, with no safe area; centre centred. Measured in a 390 × 732 content at y 56: start 10, centre 167, end 324; top 66, centre 394, bottom 722 | `fab.css` `.fab-horizontal-*`, `.fab-vertical-*` |
| F2 | With neither, it's at the container's top left, as big as its button, as `position: absolute` with no inset is: measured 0, 56 | `:host`, `width: fit-content` |
| F3 | `Edge` with top: the fab's top is the content's top (no 10 px), it's 28 high, and its button is 28 above it, half over the header: measured fab 10, 56, 56 × 28, button 10, 28. With bottom, the same at the bottom, half over the footer. A small button: `(−100% + 16px) / 2`, so it also sits half out: measured 332, 768 in a fab at 760 | `.fab-edge` and its `::slotted` margins |
| F4 | `Activated` sets the first fab button's `Activated` and every list's, at load as well as on change. It does with no list too: the button then shows its close icon, measured | `activatedChanged`, `componentDidLoad` |
| F5 | A click on any fab button inside the fab toggles it, the main one or one in a list: a list button's tap closes the fab, measured. Only if the fab has a list: with none, nothing changes, measured. Not when disabled, measured | `fab-button` `onClick` → `fab.toggle()` |
| F6 | `Close()` sets `Activated` to false | `close()` |
| F7 | Above its siblings: `z-index: 999` | `:host` |
| F8 | Lists sit 10 px outside the button: bottom below it, top above it, start before it, end after it, each centred on it, at least 56 square; with `Edge`, still 10 px outside the button that moved. Measured, a fab at 324, 534: top list 324, 374, 56 × 150; start 214, 534, 100 × 56; end 390, 534; bottom 324, 600 | `fab-list.css` margins `calc(100% + 10px)`; `.fab-edge` list margins |
| F9 | Nothing it holds is cut off or untouchable outside its own box: the edge button and the lists | CSS doesn't clip |

### Fab button

| # | Behaviour | Source |
|---|---|---|
| B1 | 56 square, round, primary with white content, 6 dp shadow, font 14, content centred; a slotted icon is 24 | `fab-button.md.css`; measured |
| B2 | Small: 40 square with 8 px margins | `.fab-button-small` |
| B3 | `Activated`: the close icon goes from `scale(0.4) rotate(-45deg)`, opacity 0, to `scale(1)`, opacity 1, and the content from none to `scale(0.4) rotate(45deg)`, opacity 0, both over 300 ms `ease-in-out`, measured frame by frame. The close icon is 24 (`--close-icon-font-size`), centred | `.close-icon`, `.fab-button-close-active` |
| B4 | `CloseIcon` names the close icon | `icon: this.closeIcon` |
| B5 | A click, Enter or Space, as a native button, with `Click` and `Command` as on `AvonicButton`. Measured: Enter opened the fab and Space closed it | `<button>`; `NativeButton` |
| B6 | `IonFocus` and `IonBlur` | `onFocus`, `onBlur`; measured `focus,blur,focus` |
| B7 | Focus-visible: a keyboard focus shows the overlay at 0.24 of the current colour (white on the main button), and no focus adorner | `ion-focusable`, `:host(.ion-focused)`; measured |
| B8 | Hover, mouse and pen: the overlay at 0.08 of the current colour | `@media (any-hover: hover)` |
| B9 | Press: activatable, a ripple in the current colour, a 12 dp shadow, and no overlay (`--background-activated` transparent at 0) | `ion-activatable`, `:host(.ion-activated)`; measured |
| B10 | `Color`: the base behind and the contrast in front, also when focused, hovered and pressed | `:host(.ion-color)` |
| B11 | Disabled: opacity 0.5 and no input. In a list and shown, opacity 1, as the list's `::slotted` rule wins, measured | `.fab-button-disabled` |
| B12 | In a list (`fab-button-in-list`, any `ion-fab-list` ancestor): 40 square, light background, `rgba(text, 0.54)` content, an 18 px icon; focus light shade, hover light tint. Hidden until `Show`: `scale(0)`, opacity 0, and `visibility: hidden`, which takes no input and no focus. `Show` puts it in at once, with no transition, measured frame by frame | `fab-button.md.css`, `fab-list.css` `::slotted` |
| B13 | `Show` outside a list does nothing, measured | `::slotted(.fab-button-in-list.fab-button-show)` |
| B14 | In a list, 5 px margins along the list: top and bottom in a column, start and end in a row | `fab-list.css` |
| B15 | Automation: a button, named by its text, invoked as a click | native `<button>` |

### Fab list

| # | Behaviour | Source |
|---|---|---|
| L1 | Not shown until `Activated` (`display: none`), so its buttons take no space, input or focus | `:host`, `.fab-list-active` |
| L2 | `Activated` shows its fab buttons (`querySelectorAll`, so at any depth) one by one, 30 ms apart from 0, on `AvonicConfig`'s clock; deactivating hides them all after 0 ms. A new change cancels what's pending, and so does leaving the tree. Measured: a, b, c at 0, ~30, ~60 | `activatedChanged`, `disconnectedCallback` |
| L3 | `Side`: bottom is a column, top a column from the bottom up, start a row from the end, end a row; the cross axis centred, at least 56 square | `.fab-list-side-*` |
| L4 | Tab order is the list's order, not the order on screen: open, Tab went main, a, b, c, then the start list, measured | DOM order |

## Visual states and keys

`fab-button.md.css` sets its states by setting variables on `:host(...)`, so an app's value for a variable wins over them, as
the outer tree beats `:host` in the cascade. Each key is `CssUnset`, read through `Css.Var` with the state's
`Avonic.FabButton.Md.*` default last, as on `ion-button`. `rgba(var(--ion-text-color-rgb), 0.54)` is a brush on
`Avonic.TextColor` with an `Opacity`, as `Avonic.Chip.Md.IconColor` is.

## How it maps to Avalonia

- **Placement.** The fab's alignments come from `:horizontal-*` and `:vertical-*` rules in its theme, and the 10 px insets plus the
  safe area from a `Margin` the theme works out with `AvonicFab.InsetConverter`, with 10 as a literal in the theme. `AvonicContent`
  gives a fixed child `Left` and `Top` at style priority, for `position: absolute` with no inset; a rule with an activator in the
  fab's theme beats that, as the fab's own insets beat the static position in CSS. An app's local alignment still wins.
- **The fab's box.** `FabPanel`, the fab's items panel, lays out what CSS does: the button and anything else in the flow, one
  under another, as wide as the widest (`fit-content`), with `Edge`'s `-50%` top or bottom margin on a fab button worked out from
  that width; and the lists out of the flow (`position: absolute`), not counted in the size, each placed `100%` of the fab's width
  outside it (`50%` beside an edge). The other 10 px of `calc(100% + 10px)` is the list's `Margin` in its theme. Percentages
  of the fab's width have no Avalonia form, which is why it's a panel.
- **The list's box.** `FabListPanel` is the flex row or column, reversed for top and start, centred across. `min-width` and
  `min-height` 56 are the theme's.
- **Not clipping.** The fab, the list and the button don't clip, as a `TemplatedControl` would by default.
- **`visibility: hidden`** isn't Avalonia's: a hidden button in a list is `scale(0)` and transparent from the theme, and takes
  no hit test or focus from the button's code, at style priority.
- **Transitions.** `--transition` is an Avalonia `Transitions` key, as on `ion-button`. The close icon's and content's 300 ms
  are literal `Transitions` in the theme, as they have no variable. The list's stagger is on `AvonicConfig.Schedule`.
- **`::slotted(ion-icon)`** sizes need a `Styles/` file. The list's margins on its buttons are rules in the button's own
  theme on `:in-list`, read from the list's `Side` through a converter, rather than global rules: they cost nothing elsewhere.
  As with any rule with an activator, an app's plain style for a list button's `Margin` loses to it.

## Extensions

`Click`, `Command` and `CommandParameter` on `AvonicFabButton`, as on `AvonicButton`: Ionic gets them from the native button.

## Dependencies

`AvonicIcon` (#3), `AvonicRippleEffect`, `AvonicContent` (#11) for the `fixed` slot, `Activatable`, `FocusVisible`,
`NativeButton`, `ColorPalette`, `BorderRadius`. All merged.
