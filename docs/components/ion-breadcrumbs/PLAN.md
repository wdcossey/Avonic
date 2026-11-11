# ion-breadcrumbs and ion-breadcrumb — Plan

A row of links to where the user came from, with a separator after each but the last, the last one active, and a collapsed
indicator standing for the middle of a long trail. `ion-breadcrumbs` works out which breadcrumb is last, active, separated
and collapsed; `ion-breadcrumb` draws itself from that. One family, one folder, as `ion-card` and `ion-grid` are.

Measurements are from real Ionic 8.8.1 in Chromium, md, at 390 px wide, wrapped in `ion-app` so that tap-click and
focus-visible run, from `ref/` (the scene in `COMPARISON.md`) and from a throwaway script that changed props and children,
pressed keys and hovered (the numbers are quoted where they're used).

## Fidelity checklist: ion-breadcrumbs

`node tools/ionic-api.js ion-breadcrumbs`.

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`. The breadcrumbs paints nothing itself; its breadcrumbs read its palette (`in-breadcrumbs-color`) |
| [x] | `itemsAfterCollapse` | `ItemsAfterCollapse` (`int`, 1) | |
| [x] | `itemsBeforeCollapse` | `ItemsBeforeCollapse` (`int`, 1) | |
| [x] | `maxItems` | `MaxItems` (`int?`, null) | `number \| undefined` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `ionCollapsedClick` | `IonCollapsedClick`, `BreadcrumbCollapsedClickEventArgs` | Detail `collapsedBreadcrumbs` and `ionShadowTarget` → `CollapsedBreadcrumbs`, `IonShadowTarget` |

No methods, slots, parts or variables. Its children have no slot in `core.json`, so they're **`Items`**, as `ion-list`'s are.

## Fidelity checklist: ion-breadcrumb

`node tools/ionic-api.js ion-breadcrumb`.

### Props (11)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `active` | `Active` (`bool`) | Pseudoclass `:active` (`breadcrumb-active`). The breadcrumbs sets it on the last, as Ionic mutates the prop |
| [x] | `color` | `Color` | |
| [x] | `disabled` | `IsEnabled` | Mapping rule |
| [ ] | `download` | — | Not applicable: only applies when a browser follows `href` |
| [x] | `href` | `Href` (`string?`) | Makes it a link: `ion-activatable` and `ion-focusable`, a tab stop, Enter. **Avonic doesn't navigate**: the router family is out of scope ([ROADMAP](../../ROADMAP.md#out-of-scope)), so the app follows it from the `Click` extension. Kept, where the other controls leave it out, because it makes the breadcrumb a link (decided 2026-11-11) |
| [ ] | `mode` | — | ADR 0001 |
| [ ] | `rel` | — | Not applicable: only applies when a browser follows `href` |
| [ ] | `routerAnimation` | — | Out of scope: router |
| [ ] | `routerDirection` | — | Out of scope: router |
| [x] | `separator` | `Separator` (`bool?`) | null is `undefined`: the breadcrumbs decides |
| [ ] | `target` | — | Not applicable: only applies when a browser follows `href` |

### Events (2)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionBlur` | `IonBlur` | On the breadcrumb's own focus, not the indicator's: `onBlur` is on the native element |
| [x] | `ionFocus` | `IonFocus` | Same |

### Slots

`core.json` lists none, but `breadcrumb.js` renders four, and Ionic's docs use `start`, `end` and `separator`. They map by the
slot rule: `start` → `StartContent`, default → `Content`, `end` → `EndContent`, `separator` → `SeparatorContent`, each with a
`…Template`. `SeparatorContent` replaces md's `/`, as a slot's own children are its fallback.

### Shadow parts (3)

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `native` | `PART_Native` (`Border`) |
| [x] | `separator` | `PART_Separator` |
| [x] | `collapsed-indicator` | `PART_CollapsedIndicator` (`Button`: it's a native `<button>`) |

### CSS variables (5)

Each is set on `:host` by `breadcrumb.md.css`, so a value from outside wins: `CssUnset`, read through `Css.Var` with the md
literal as the next value. Ionic leaves the step variables unset in light, so the literals render, measured: `#677483`, not the
ramp's `#666666`. The literals go in `Tokens/Breadcrumb.axaml` as `Avonic.Breadcrumb.Md.*`, as the toolbar's do.

| ✓ | Ionic | Key | md default, measured |
|---|---|---|---|
| [x] | `--background-focused` | `Avonic.Breadcrumb.BackgroundFocused` | `#fff` |
| [x] | `--color` | `Avonic.Breadcrumb.Color` | `#677483` |
| [x] | `--color-active` | `Avonic.Breadcrumb.ColorActive` | `#03060b` |
| [x] | `--color-focused` | `Avonic.Breadcrumb.ColorFocused` | `#35404e` |
| [x] | `--color-hover` | `Avonic.Breadcrumb.ColorHover` | `#03060b` |

Literals with no variable: the separator `#73849a`, slotted icons `#7d8894` and `#222d3a` when active, the indicator's
`#eef1f3`, `#dfe5e8` focused and `#73849a`, and the focus shadow. In `Tokens/` as `Md` keys too.

## Behaviours

From `breadcrumbs.js`, `breadcrumb.js` and the two `md.css` files. Each becomes a test.

### The breadcrumbs (`breadcrumbs.js`)

| # | Behaviour | Source, measured |
|---|---|---|
| 1 | Every breadcrumb but the last gets `separator = true`, unless it has a value; the last's stays unset. A value of `false` hides it, and the last never shows one | `setBreadcrumbSeparator`; `A:sep=false \| B:sep=true \| C:ALsep=true`, shown `[false,true,false]` |
| 2 | If none is active, the last becomes active, and the breadcrumbs remembers it did | `setBreadcrumbSeparator`; `C:AL` |
| 3 | When the children change, or `MaxItems`, `ItemsBeforeCollapse` or `ItemsAfterCollapse` does, the first active breadcrumb is made inactive if the breadcrumbs ever made one active, then 1 and 2 and 5 run again. So a new last breadcrumb takes over, and so can an older one: with C active by default, setting B active and adding E leaves C active, not B or E | `slotChanged`, `maxItemsChanged`, `resetActiveBreadcrumb`; `append D` → `D:AL`, `then append E` → `C:A`, `E:L`. `slotchange` comes once after the task that made the changes: X (active) and Y appended together leave `X:A`, `Y:L`; apart, `Y:AL` |
| 4 | Collapsing: with `MaxItems` set, more breadcrumbs than it, and `before + after <= MaxItems`, the breadcrumbs from index `before` to `count - after` (exclusive) collapse, and the first of them shows the indicator. Otherwise none do | `setMaxItems`; `maxItems=3` → `B:CI C:C D:C`; `before=3 (3+1>3)` → none |
| 5 | With `MaxItems` set and `ItemsAfterCollapse = 0`, the last is the breadcrumb at `before`, whether or not anything collapses: it's active and has no separator, and the real last one has one | `last = breadcrumbs[itemsBeforeCollapse]`; `before=1 after=0` → `B:ACLI`; `maxItems=10 after=0` → `B:AL`, `E:sep=true` |
| 6 | Breadcrumbs are found anywhere inside, in order, not only as direct children | `querySelectorAll`; `nested in div` → `Nested:sep=true` |
| 7 | A click on the indicator, by pointer, Enter or Space, raises `IonCollapsedClick` on the breadcrumbs with the collapsed breadcrumbs in order and the indicator as `IonShadowTarget` | `onCollapsedClick`; `collapsed=B,C target=BUTTON`, the same from Enter and Space |
| 8 | A flex row that wraps, its children centred on each line; a breadcrumb never shrinks, so a long one overflows its line | `display: flex; flex-wrap: wrap; align-items: center`; `flex: 0 0 auto`; at 200 px, `[0,0,79,36] [79,0,78,36] [0,36,239,36]` |
| 9 | In a toolbar: padding 0 8. In a coloured toolbar its text is the contrast | `:host(.in-toolbar)`, `:host(.in-toolbar-color)`; `0px 8px`, `rgb(255, 255, 255)` |

### A breadcrumb (`breadcrumb.js`, `breadcrumb.md.css`)

| # | Behaviour | Source, measured |
|---|---|---|
| 10 | The box: native padding 12 and 6, font 16 at weight 400, line height 24, so 36 tall; `Home` is 93.67 wide with its separator, which is `/` with margins 10 and -1 on top | `:host`, `.breadcrumb-native`, `.breadcrumb-separator`; element 0 |
| 11 | Its separator shows unless it's last; collapsed, only the one with the indicator shows one, unless it's last | `showSeparator` |
| 12 | Collapsed: the native part is hidden. With the indicator, the box is the indicator and the separator, 86.61 by 23 | `.breadcrumb-collapsed .breadcrumb-native { display: none }`; element 10 |
| 13 | Active: `--color-active`, weight 500 | `:host(.breadcrumb-active)`; element 2 `rgb(3, 6, 11)`, 500 |
| 14 | Colours by context, in CSS's order: `Color` is the base; inside a coloured breadcrumbs the base too, separator included; inside a coloured toolbar the contrast at 0.8, separator included, and the contrast when active (of the breadcrumb's own colour, if it has one) | `.ion-color`, `.in-breadcrumbs-color`, `.in-toolbar-color`; `hover u` white, `hover w` `rgb(0, 0, 0)` (success's contrast) |
| 15 | `Href` makes it a link: a tab stop; Enter clicks it and Space doesn't; hovering with a mouse gives `--color-hover`, and over a colour (its own or the breadcrumbs') the shade. Without `Href`, none of these | `isClickable`, `ion-activatable`, `ion-focusable`, `<a>`; `tab 0`, `Enter on link`, `Space on link` `[]`, `hover a`, `hover s` unchanged, `hover c`, `hover p`; in a coloured toolbar, hover beats the 0.8 contrast: `hover t` `rgb(3, 6, 11)` |
| 16 | Focused by keyboard: `--color-focused`, and the native part gets `--background-focused`, radius 4 and the shadow | `:host(.ion-focused)`; element 4 |
| 17 | `IonFocus` and `IonBlur` follow the native element's focus, not the indicator's | `onFocus`, `onBlur`; tab log |
| 18 | Disabled: opacity 0.5, separator included; no input; not a tab stop | `.breadcrumb-disabled`, `href` removed; `tab` skips `Dis`, `aria-disabled="true"` |
| 19 | Slotted icons: 18 px, `#7d8894`, `#222d3a` when active; 8 between a start icon and the text, and the text and an end icon. A separator icon too | `::slotted(ion-icon)`; element 26's separator 18 by 18 |
| 20 | The indicator: a button 32 by 18 with margins 14, radius 2, `#eef1f3`, an `ellipsis-horizontal` icon 22 px in `#73849a`, 1 px down; opacity 0.7 under a mouse; `#dfe5e8` while focused, by any means; named "Show more breadcrumbs"; a tab stop of its own | `.breadcrumbs-collapsed-indicator`; `indicator hover opacity 0.7`, `:focus bg rgb(223, 229, 232)`, `aria-label`. `breadcrumbs.md.css`'s rules for it can't reach the breadcrumb's shadow root: measured unchanged in a coloured toolbar |

### Accessibility

A link is a hyperlink with its content as its name, and a screen reader's activation clicks it. A breadcrumb without `Href` has
no role, its children speak for themselves. The separator is hidden (`aria-hidden`). The indicator is a button named
"Show more breadcrumbs", its icon hidden. The breadcrumbs has no role.

## Visual states and keys

| State | Rule | Keys |
|---|---|---|
| Plain | `color: var(--color)` | `Avonic.Breadcrumb.Color` → `Md.Color` |
| Active | `--color-active`, weight 500 | `.ColorActive` → `Md.ColorActive` |
| Focused | `--color-focused`, native `--background-focused`, radius 4, shadow | `.ColorFocused`, `.BackgroundFocused`, `Md.FocusedBoxShadow` |
| Hover (link, mouse) | `--color-hover`; shade over a colour | `.ColorHover`; `ColorPalette.Shade` |
| Colour | base; contrast at 0.8 in a coloured toolbar | `ColorPalette` |
| Separator | `#73849a`; base, or contrast at 0.8 | `Md.SeparatorColor` |
| Indicator | `#eef1f3`, `#dfe5e8` focused, `#73849a` | `Md.CollapsedIndicator*` |
| Icons | `#7d8894`, `#222d3a` active | `Md.IconColor`, `Md.IconColorActive` |
| Disabled | opacity 0.5 | — |

Order in the theme, later beating earlier as CSS's specificity then order do here: active, focused, in a coloured breadcrumbs,
own colour, in a coloured toolbar, hover, in a coloured breadcrumbs and active, in a coloured toolbar and active, hover over a
colour.

## Avonic decisions

| Decision | Why |
|---|---|
| The breadcrumbs sets `Active` and `Separator` with `SetCurrentValue` | Ionic assigns the props, so an app reads them back, and an app's later value wins. `SetCurrentValue` doesn't take the property's priority, so a style or binding an app changes later still wins, where a local value would beat a style; and a style value couldn't be undone as Ionic undoes an active breadcrumb in 3 |
| `Collapsed`, `Last` and `ShowCollapsedIndicator` are internal | `@internal` in Ionic. `:collapsed` is the pseudoclass for `breadcrumb-collapsed`; the indicator and separator are shown or not by the control, as Ionic renders them or not |
| A change of `Items` is posted to the dispatcher, once for any number of changes before it runs | `slotchange` is a browser event after the task, measured in 3. The watchers stay at once, as `@Watch` is |
| The breadcrumbs looks again when a breadcrumb inside is attached or detached, if that changed which breadcrumbs there are, not only when `Items` changes | A breadcrumb made from an `ItemTemplate` appears after `Items` changes. Ionic's `slotchange` only fires for direct children: a breadcrumb added inside a nested panel later counts here and not there |
| Once it has been attached, changes count while it's detached too | `slotchange` and the watchers fire on an element out of the document. `componentWillLoad` is once, so attaching again doesn't look again |
| Nothing is looked at before it's first attached | `componentWillLoad`, then `slotchange` and the watchers, only come once Ionic has loaded the component. Children added one at a time before then would otherwise undo an app's `Active` by the rule in 3 |
| `in-toolbar-color` isn't a pseudoclass on the breadcrumbs | Its rule sets the toolbar's contrast, which the row inherits from the toolbar anyway, and each breadcrumb sets its own colour |
| `breadcrumbs-collapsed` isn't a pseudoclass | Its state is never set in `breadcrumbs.js`: dead |
| `:in-toolbar` on a breadcrumb isn't a pseudoclass | No md rule reads it |
| `breadcrumbs.md.css`'s indicator rules aren't translated | They're in the breadcrumbs' shadow root and can't reach the breadcrumb's: measured unchanged |
| No `in-toolbar` rule for a breadcrumb's own padding | There is none |

## Extensions

| Extension | Why |
|---|---|
| `Click` on `AvonicBreadcrumb` | The DOM `click` an Ionic app listens to, and the only way to follow an `Href` until there's a router: on the release of every press on the breadcrumb, as on a `<span>` too, and Enter on a link. A press on the indicator clicks the breadcrumb too, after `IonCollapsedClick`, as the DOM click bubbles to the host: measured `click:B` |
| `Command`, `CommandParameter` | As `AvonicButton`'s and `AvonicChip`'s |

## Dependencies

`ion-icon` (the indicator's `ellipsis-horizontal`, and slotted icons): done. `ion-toolbar` for `in-toolbar`: done.
`ion-popover`, which an app opens from `IonCollapsedClick`: not built yet. The event hands over the breadcrumbs and the
indicator, and the sample shows them in a list instead.
