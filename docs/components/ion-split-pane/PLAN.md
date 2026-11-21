# ion-split-pane — Plan

`AvonicSplitPane`: Ionic's split pane, which shows its side menu beside the main content once the window passes a breakpoint, and
leaves it an ordinary menu below it ([#78](https://github.com/wdcossey/Avonic/issues/78)). What it does is mostly done by the menu:
`menu.js` reads its split pane (`getSplitPane`, `isVisible()`, `ionSplitPaneVisible`), and `menu.md.css` holds the side pane's
layout, "to allow for menus to be wrapped in custom components". `AvonicMenu` has none of that yet, so this PR adds it to the menu
(see [The menu's side](#the-menus-side)): without it the split pane does nothing an app can see.

Measured in real Ionic 9.0.11 in headless Chromium, md, on `render.js`'s phone (390 x 844 at 2x, mobile, touch) and at other
viewport sizes; the raw output is [`ref/measurements.txt`](ref/measurements.txt) (S1 to S23) and
[`ref/media-queries.txt`](ref/media-queries.txt) (`matchMedia` for 59 queries at 600 x 800), from the scripts named in
[COMPARISON.md](COMPARISON.md).

## Fidelity checklist

`node tools/ionic-api.js ion-split-pane`.

### Props (3)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `contentId` | `ContentId` | `string?` | | The `Name` of the main content, one of its own children, looked for when it's attached (`styleMainElement` in `connectedCallback`). None found, or two: a warning logged, as Ionic's `printIonWarning` |
| [x] | `disabled` | `Disabled` | `bool` | `false` | Not `IsEnabled`: the split pane holds the whole app, and `IsEnabled` false would disable and fade every descendant, where Ionic's `disabled` only hides the split (CLAUDE.md's row for a `disabled` that leaves the children enabled, as `AvonicReorderGroup.Disabled`) |
| [x] | `when` | `When` | `SplitPaneWhen` | `(min-width: 992px)` | `boolean \| string`: a struct with implicit conversions from `bool` and `string` and a `TypeConverter` (as `ModalCanDismiss`). See [`when`](#when) |

### Events (1), methods (0), slots (1), parts (0)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionSplitPaneVisible` | `IonSplitPaneVisible`, `SplitPaneVisibleEventArgs.Visible` | Bubbles. Raised when the visible state changes, and only then, including at the first attach (S2: Stencil's watcher is ready before `connectedCallback`) |
| [x] | `isVisible()` (`@internal`) | internal `Visible` | Internal, as Ionic's, which only the menu calls. The state shows as `:split-pane-visible` |
| [x] | default slot | `Items` | CLAUDE.md: a default slot for many children that the component manages (its `.js` reads its children) and lays out (a flex row) is `Items`. A control is its own container, so its logical parent is the split pane |

### CSS custom properties (4)

| ✓ | Ionic | Avonic resource key | md default | Notes |
|---|---|---|---|---|
| [x] | `--border` | `Avonic.SplitPane.BorderWidth`, `Avonic.SplitPane.BorderColor` | `1`; unset, following `Avonic.ItemBorderColorBrush` | `1px solid var(--ion-item-border-color, var(--ion-border-color, …, rgba(0, 0, 0, 0.13)))`. A shorthand has no Avalonia value, so it's two keys, as `ion-tab-bar`'s `--border`; Avalonia draws solid lines only |
| [x] | `--side-max-width` | `Avonic.SplitPane.SideMaxWidth` | `28%` | A CSS length, read as `AlertPanel.Length` does: `px` or a number, `%` of the split pane's width, `vw`, `vh`, `calc()`, `min()`, `auto` |
| [x] | `--side-min-width` | `Avonic.SplitPane.SideMinWidth` | `270` | |
| [x] | `--side-width` | `Avonic.SplitPane.SideWidth` | `100%` | Unset (`CssUnset`), the menu's own `--width`, `Avonic.Menu.Width`, as `var(--side-width, var(--width))` (S12c: 304) |

## What Ionic does

From `split-pane.js`:

1. **`updateState`**, at `connectedCallback` and whenever `disabled` or `when` changes: the old media listener is removed; `disabled`
   → not visible; a boolean `when` → visible is the boolean; a string is a shortcut (`xs` `(min-width: 0px)`, `sm` 576, `md` 768,
   `lg` 992, `xl` 1200, `never` `''`) or else a media query; an empty one → not visible; otherwise `matchMedia`, visible while it
   matches, followed by a listener.
2. **`visibleChanged`**: `ionSplitPaneVisible` with `{ visible }`, when the state changes (S5: none from 768 to 800 wide).
3. **`styleMainElement`**, at `connectedCallback` only: the direct child whose `id` is `contentId` gets `split-pane-main`; a second one
   warns "Cannot have more than one main node" and stops; none warns "Does not have a specified main node".
4. **`render`**: the host has `split-pane-visible` while visible.
5. `disconnectedCallback` removes the media listener.

From `split-pane.md.css`: the host fills its parent (`position: absolute; inset: 0`), a flex row; while visible the main is `flex: 1`,
`position: relative`, `overflow: hidden`, `z-index: 0`, no shadow. `--side-width: 100%`, `--side-min-width: 270px`,
`--side-max-width: 28%`, `--border: 1px solid …`. `::slotted(.split-pane-side:not(ion-menu)) { display: none }` is dead: only
`ion-menu` puts `split-pane-side` on itself, and `setPaneClass` is only called for the main.

From `menu.js` and `menu.md.css`, the menu's side of it:

6. **`getSplitPane()`**: the closest `ion-split-pane`, unless the closest `.split-pane-main` is inside it (the menu is in the main,
   so it's an ordinary menu). Read in `render`: `split-pane-side` on the host when there is one.
7. **`componentDidLoad`**: `isPaneVisible = await splitPane.isVisible()`, then `updateState`.
8. **`onSplitPaneChanged`**, `@Listen('ionSplitPaneVisible', { target: 'body' })`: from its own split pane only, `isPaneVisible` is
   the detail's `visible`, then `updateState`.
9. **`_isActive()`** is `!disabled && !isPaneVisible`: a menu shown in the pane can't open, close or swipe (S23: `open()` is false),
   and **`updateState`** closes it at once with `ionDidClose` and the `gesture` role (S9: an open menu closes when the pane shows).
10. `menu-pane-visible` on the host. With `split-pane-side` and `menu-enabled`: `display: flex`, `position: relative`, `z-index: 0`,
    `flex-shrink: 0`, `order: -1` (`1` for `side="end"`), `width: var(--side-width, var(--width))`, `min-width: var(--side-min-width)`,
    `max-width: var(--side-max-width)`, `border-inline-end: var(--border)` (`border-inline-start` at the end). `.menu-inner` fills
    it (`left: 0; right: 0; width: auto; transform: none`). A disabled menu stays `display: none` (S10).
11. **`ion-menu-button`, `ion-menu-toggle`** listen to `ionSplitPaneVisible` on `body` too, and run `visibilityChanged`: hidden while
    their menu isn't active (S2, S19).

## Measured

| | What | Value |
|---|---|---|
| S1, S2 | Default `when` at 390 and 1024 wide | Hidden at 390; at 1024 the menu 286.72 wide (28%), its inner 285.72, the border 1 px `rgba(0, 0, 0, 0.13)` at its inline end; the main 737.28 from 286.72 |
| S3 | `when="xs"` at 390 | Menu 270 (the minimum beats 28%), main 120 from 270 |
| S4 | `side="end"`, the menu after the main in the DOM | Main 0 to 120, menu 120 to 390, the border at its left, the inner from 121 |
| S5 | `md`, 767 → 768 → 800 → 500 | Visible at 768 (one `ionDidClose(gesture)`, then one event), nothing more at 800, hidden at 500 (one event, no `ionDidClose`) |
| S6 | `(min-width: 500px) and (orientation: portrait)` | Visible at 600 x 800, hidden at 800 x 600 |
| S7 | `when="true"` as an HTML attribute | A string, so a media query `true`, which matches nothing: hidden. As a property, `true` shows it (S8) |
| S8 | `when` true, false, `never`, `xs`, `''`, `disabled`, `garbage(((` | Shown, hidden, hidden, shown, hidden, hidden (then shown again when enabled), hidden |
| S9 | Menu open, then the pane shows | `ionDidClose(gesture)` before `ionSplitPaneVisible` reaches `document`; `isOpen()` false, `isActive()` false, `open()` false; back at 390, `isActive()` true and the menu closed |
| S10 | A disabled menu in a visible pane | `display: none`; the main fills the 390 |
| S11 | `dir="rtl"` | The start menu at the right (120 to 390), its border at its left |
| S12 | `--side-width: 200px; --side-min-width: 100px; --side-max-width: 50%` | 195; `40%` → 156; `--side-width: initial` → the menu's `--width`, 304 |
| S13 | 992, 1200, 1400 wide | 277.75, 336, 392 (28%) |
| S14, S15 | A plain `div` beside them | In the row at its content's width (38.97), after the menu; the main takes what's left. With no main found, the `.ion-page` stays `position: absolute` over the whole pane, and a warning is logged |
| S16 | A menu inside the main | Not a side: no `split-pane-side`, still active |
| S17 | Two menus, start and end, at 1024 | 286.72 each, the main 450.56 between them; one menu button, hidden |
| S18 | `ion-content` as the main | Fills what's left (120 at 390) |
| S19 | `ion-menu-toggle`, `autoHide` true and false | The first hidden when the pane shows, the second not |
| S20 | `contentId` changed while connected; detached and attached again | Nothing at the change (no watcher); after attaching again, both children have `split-pane-main` and share the room (60 each) |
| S22 | `type="push"` in the pane | The main keeps the push type's `box-shadow` (`.md .menu-content-push` in the light DOM beats `::slotted`): drawn over the menu's last 12 px |

## `when`

`SplitPaneWhen` holds a `bool` or a `string`. `True` and `False` in XAML, in any case, are booleans, as an Angular, React or Vue
binding passes them; Ionic's own HTML attribute `when="true"` is a string, a media query that matches nothing (S7), which no
Avonic app would mean. Any other string is a shortcut or a media query; null is an empty string, which hides it (Ionic's
`null.length` throws).

- **A shortcut** (`xs`, `sm`, `md`, `lg`, `xl`) reads `GridViewport`, the grid's breakpoint for the `TopLevel`, whose minimum widths
  are the same 0, 576, 768, 992 and 1200, and its `Changed`. `never` hides it (Ionic's `''`).
- **A media query** is read by `MediaQuery`, internal, against the `TopLevel`'s client size (a popup's: the `TopLevel` it belongs
  to, as `GridViewport`), again on each change of `ClientSize`. It covers Media Queries 4 as far as a split pane needs:
  a list (`,`, any matching), `not` and `only`, media types (`all` and `screen` match; `print`, `tv`, any other don't), `and`,
  `or`, nested brackets, `width` and `height` with `min-` and `max-`, the plain and the range forms (`(width >= 600px)`,
  `(400px < width < 700px)`), `orientation`, `aspect-ratio` with `min-` and `max-`, and lengths in `px`, `em` and `rem` (16 px),
  `cm`, `mm`, `in`, `pt`, `pc`, `q`, or `0`. A feature it doesn't know (`hover`, `prefers-color-scheme`, `resolution`) is unknown,
  and so is a bracket it can't parse (Chromium's general-enclosed), which, as in Chromium, makes `not` unknown too and only
  matches through an `or` or a comma; a query malformed outside its brackets is `not all`. Exponents (`9.92e2px`) are read.
  Every row of `ref/media-queries.txt` it reads is a test case, at 600 x 800. `calc()` in a query isn't read (false).

## Design

- **`AvonicSplitPane : ItemsControl`**, a control its own container, with `SplitPanePanel` as its panel. `ContentId`, `Disabled`,
  `When`, `IonSplitPaneVisible`; pseudoclass `:split-pane-visible`; `:split-pane-main` on the main child, through its
  `IPseudoClasses` (CLAUDE.md's row for a class a container puts on a child). No automation role, as Ionic's host has none: a
  `NoneAutomationPeer`, as `AvonicRow`.
- **`SplitPanePanel`**, the layout of `split-pane.md.css` and the menu's pane rules. Hidden: every child fills the pane, as the menu
  (`position: absolute; inset: 0`) and an `.ion-page`, `ion-router-outlet` or `ion-content` main do. Visible: a row in the split
  pane's direction: each **side** (a child that is, or holds, a menu shown in this pane) at `clamp(SideWidth, SideMinWidth,
  SideMaxWidth)` of the pane's width, the maximum then the minimum (S12), start sides first and end sides last (`order`), with its
  border at its inline end (start) or start (end) drawn by the panel inside that width; the main (`flex: 1`) takes what's left, at
  least 0; any other child sits in the row at its desired width, in order (S15). `SideWidth`, `SideMinWidth`, `SideMaxWidth`,
  `BorderBrush`, `BorderThickness` are the panel's, set by the theme from the keys.
- **`overflow: hidden` on the main** while visible: `ClipToBounds` at style priority, bound by the split pane to each item when it's
  added, before it's styled, and given a value on the main only, so an app's own style for it wins, as an app's rule beats Ionic's
  `::slotted`. It costs nothing in the style budget, where a `::slotted` rule in `Styles/` would add an alternative for every
  element (it went over the budget, 287 of 286, so it was moved).
- **`when`**: above. The split pane listens only while attached; while detached it keeps its last state, and reads it again when
  attached (Ionic's `matchMedia` reads a window that's always there).

### The menu's side

What `menu.js` and `menu.md.css` do with a split pane, added to `AvonicMenu` (another control's files, in this PR because the split
pane does nothing without them; flagged at the top of the PR):

- `:split-pane-side` and `:menu-pane-visible`, Ionic's host classes, as public constants.
- At each attach: `GetSplitPane()` from its visual ancestors (the nearest `AvonicSplitPane`, unless a `:split-pane-main` comes first),
  `:split-pane-side`, and `isPaneVisible` read from the pane, then a listener for `IonSplitPaneVisible` on the `TopLevel`, Ionic's
  `body`, for its own pane only. Ionic reads it at `componentDidLoad` only; here it's every attach, as a menu here is detached and
  attached again with its page far more often, and misses the events while it's away.
- `IsActive()` is `IsEnabled && !isPaneVisible`, so `UpdateState` closes it and turns its gesture off, as Ionic.
- Shown (`display: flex`) while it's a side, the pane is visible and it's enabled; `ZIndex` 0 then (`z-index: 0`).
- Its theme: `:menu-pane-visible` puts the container at `transform: none` and the panel at `ContentWidth: 100%` (`left: 0; right: 0;
  width: auto`).
- `ion-menu-button` and `ion-menu-toggle` listen to `ionSplitPaneVisible` to run `visibilityChanged`. Avonic's listen to the
  controller's internal `MenuChanged`, Ionic's `ionMenuChange`, so the menu raises it after it takes the pane's state: the same
  check, at the same moment, with no change to those two files.

### Not done, with why

| What | Why |
|---|---|
| `::slotted(.split-pane-side:not(ion-menu)) { display: none }` | Dead in Ionic: nothing but `ion-menu` has the class |
| The push type's main shadow over the menu (S22) | Avonic draws the content's shadow in the menu's own template, which clips it to the menu; Ionic's comes from a light-DOM rule that beats the split pane's `box-shadow: none`, likely by accident. Recorded |
| `split-pane-main` kept on an old main after attaching again with another `contentId` (S20) | An Ionic bug (two mains share the room; Ionic itself warns that there can be only one): the mark moves to the new main ([ADR 0005](../../decisions/0005-dont-copy-an-ionic-bug.md)) |
| `ios` mode's `split-pane.ios.css` | ADR 0001: md only |

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| B1 | Default `when` (992): hidden at 390, shown at 1024; the menu 286.72 at the start, the main the rest | 1, 10, S1, S2 |
| B2 | `IonSplitPaneVisible` when the state changes, and only then, at the first attach too | 2, S5 |
| B3 | Shortcuts: `md` shown from 768, not at 767, following the window | 1, S5 |
| B4 | A media query string, `and` an orientation, follows the window; the `MediaQuery` table against Chromium | 1, S6, media-queries.txt |
| B5 | `When` true, false, `never`, empty, garbage | 1, S8 |
| B6 | `Disabled` hides it, enabling shows it again | 1, S8 |
| B7 | `ContentId` marks the main (`:split-pane-main`), at attach; none found or two warn | 3, S14 |
| B8 | Side widths: min beats 28% at 390 (270); 992, 1200; custom widths and percentages; unset `SideWidth` → the menu's width | 10, S3, S12, S13 |
| B9 | The border: 1 px inside the side's width at its inline end; at the start for an end side; mirrored in RTL | 10, S4, S11 |
| B10 | End side after the main, two sides around it | 10, S4, S17 |
| B11 | A menu in the pane is not active: an open one closes (`ionDidClose` gesture) when the pane shows; `Open()` false; the swipe off; active again below | 9, S9, S23 |
| B12 | A disabled menu stays hidden and the main fills | 10, S10 |
| B13 | A menu inside the main is an ordinary menu | 6, S16 |
| B14 | Menu button and toggle (`AutoHide`) hide while the pane shows, and come back | 11, S2, S19 |
| B15 | The main clips while visible (`overflow: hidden`), not while hidden | css |
| B16 | A plain child sits in the row at its width | S15 |
| B17 | A menu wrapped in another control is still the side | menu.md.css's comment |
| B18 | Detached and attached again: the menu reads the pane's state again | 7, deviation |
| B19 | No automation role of its own | render |

## Deliberate differences

| What | Why |
|---|---|
| `Disabled`, not `IsEnabled` | See the props |
| `When="True"` is a boolean | See [`when`](#when) |
| The border is drawn by the split pane's panel, inside the side's width | The menu's template has no host border, and its root is laid out left to right; the line is in the same place, the same width and colour |
| A plain child is in the row while visible and fills the pane while hidden | Avalonia has no `position`; while hidden every child fills, as a menu and a main do |
| The menu reads the pane's state at every attach, and hears the event even if an app marked it handled | See [The menu's side](#the-menus-side): Avalonia's `Handled` isn't `stopPropagation()` to most code that sets it |

## Extensions

None.

## Dependencies

`AvonicMenu`, `AvonicMenuButton`, `AvonicMenuToggle` (#44, merged), `GridViewport` (#14), `AlertPanel.Length` (#41).
