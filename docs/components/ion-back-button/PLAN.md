# ion-back-button — Plan

`AvonicBackButton` ([#47](https://github.com/wdcossey/Avonic/issues/47)): the toolbar button that pops the closest
`AvonicNav` when it can go back, and otherwise goes to `defaultHref`. It shows only when there is somewhere to go: in the header
of a page that has one under it, or with a `defaultHref`.

Measured in real Ionic 9.0.11 in headless Chromium, md, on `render.js`'s phone (390 x 844 at 2x, mobile and touch), in an
`ion-app`. The raw output is [`ref/measurements.txt`](ref/measurements.txt), from a throwaway Playwright script kept outside the
repo (`measure.js`, five scenes: B1 to B5). The nav's pages are plain custom elements (`page-one`, `page-two`), as an app with no
framework writes them.

**Decided on 2026-11-21**, before any code ([§ The contract](#the-contract-for-you-to-decide)): option A with the variant: `DefaultHref`
stays `string?` and raises `DefaultHrefRequested`, and an extension `DefaultPage` holds a page the closest nav goes back to; the
config keys `backButtonText` and `backButtonDefaultHref` are added to `AvonicConfig` in this PR; `aspect-ratio: 1/1` is
reproduced; the coloured toolbar's `ion-buttons` variables aren't mapped. The rest of the plan is as written before the decision,
with the choices it left open recorded in [§ Decisions](#decisions).

## Fidelity checklist

`node tools/ionic-api.js ion-back-button`.

### Props (8)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner` |
| [x] | `defaultHref` | `DefaultHref` (`string?`) | **See the contract.** In every option, not null shows the button (`.show-back-button`); null when it's first attached takes `AvonicConfig.BackButtonDefaultHref` (`componentWillLoad`, `ConfigOnLoad.IfUnset`) |
| [x] | `disabled` | `IsEnabled` (inverted) | Mapping rule |
| [x] | `icon` | `Icon` (`string?`) | An `AvonicIcon.Icon` (a name or a source). Null takes `AvonicConfig.BackButtonIcon`, then md's `arrow-back-sharp`; empty shows no icon |
| [ ] | `mode` | none | Not exposed (ADR 0001) |
| [x] | `routerAnimation` | `RouterAnimation` (`AnimationBuilder?`) | Not only the router's: the nav's `pop` takes it as its `animationBuilder`, so it's kept in every option, as `AvonicNavLink.RouterAnimation` |
| [x] | `text` | `Text` (`string?`) | Null takes `AvonicConfig.BackButtonText`, then md's none; empty shows no text |
| [ ] | `type` | none | Not applicable: HTML form, and dead in Ionic too: the host's click handler calls `preventDefault()` before anything else, so a `type="submit"` back button in a `<form>` never submits it (B5: 0 submits after a click) |

### Events, methods, slots

None in Ionic. Under option A of the contract, one extension event: see [§ Extensions](#extensions).

### Shadow parts (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `native` | `PART_Native` | A `Border`, as `AvonicMenuButton`'s |
| [x] | `icon` | `PART_Icon` | An `AvonicIcon`, shown while the icon isn't null or empty |
| [x] | `text` | `PART_Text` | A `TextBlock`, shown while the text isn't null or empty |

Plus `PART_Ripple` (the `ion-ripple-effect`, md only), unbounded when the button has only an icon, else bounded, and
`PART_Overlay` (`.button-native::after`), as the menu button has.

### CSS custom properties → resource keys (32)

md's values from `back-button.md.css`'s two `:host` blocks; `currentColor` is the host's `Foreground`, read through `Css.Var` as
the last value.

| ✓ | Ionic | Avonic resource key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.BackButton.Background` | transparent |
| [x] | `--background-focused` | `Avonic.BackButton.BackgroundFocused` | unset: `currentColor` |
| [x] | `--background-focused-opacity` | `Avonic.BackButton.BackgroundFocusedOpacity` | 0.12 |
| [x] | `--background-hover` | `Avonic.BackButton.BackgroundHover` | unset: `currentColor` |
| [x] | `--background-hover-opacity` | `Avonic.BackButton.BackgroundHoverOpacity` | 0.04 |
| [x] | `--border-radius` | `Avonic.BackButton.BorderRadius` | 4, and `50%` with only an icon (`Avonic.BackButton.Md.BorderRadiusIconOnly`, by the mapping row for a variable with a different default per host class) |
| [x] | `--color` | `Avonic.BackButton.Color` | unset: `currentColor`, the colour inherited |
| [x] | `--color-focused` | `Avonic.BackButton.ColorFocused` | unset: `currentColor` |
| [x] | `--color-hover` | `Avonic.BackButton.ColorHover` | unset: `currentColor` |
| [x] | `--icon-font-size` | `Avonic.BackButton.IconFontSize` | 24 (`1.5rem`) |
| [x] | `--icon-font-weight` | `Avonic.BackButton.IconFontWeight` | `normal`. An `AvonicIcon` is a path, so a weight changes nothing, as for an SVG `ion-icon` (measured: the same drawing): kept as a key, documented as read by nothing visible |
| [x] | `--icon-margin-top`, `-end`, `-bottom`, `-start` | `Avonic.BackButton.IconMarginTop` and the rest | 0 |
| [x] | `--icon-padding-top`, `-end`, `-bottom`, `-start` | `Avonic.BackButton.IconPaddingTop` and the rest | 0 |
| [x] | `--margin-top`, `-end`, `-bottom`, `-start` | `Avonic.BackButton.MarginTop` and the rest | 0 |
| [x] | `--min-height` | `Avonic.BackButton.MinHeight` | 32; 48 with only an icon (the host's own `min-height: 48px`, a literal in the theme) |
| [x] | `--min-width` | `Avonic.BackButton.MinWidth` | 44; 48 with only an icon (likewise) |
| [x] | `--opacity` | `Avonic.BackButton.Opacity` | 1 |
| [x] | `--padding-top`, `-bottom` | `Avonic.BackButton.PaddingTop`, `.PaddingBottom` | 0 |
| [x] | `--padding-start`, `-end` | `Avonic.BackButton.PaddingStart`, `.PaddingEnd` | 12 |
| [x] | `--ripple-color` | `Avonic.BackButton.RippleColor` | unset: `currentColor` |
| [x] | `--transition` | `Avonic.BackButton.Transition` | `background-color, opacity 100ms linear`: as a string key, documented; Ionic's own value names `background-color` with no duration (so 0 s) and the native's `opacity` with 100 ms, which nothing in md animates (the native's `opacity` is `--opacity`, constant). Applied as a 100 ms `DoubleTransition` on `PART_Native`'s `Opacity` from the theme, so an app's `--opacity` change eases as in Chromium |

No variable: `font-size: 0.875rem` (14), `font-weight: 500`, `text-transform: uppercase`, `line-height: 1`, the text's
`padding-inline: 4px`, the icon-only `min-width`/`min-height` of 48 and `aspect-ratio: 1/1`, `opacity: 0.5` when disabled:
literals in the theme.

## What Ionic does

From `back-button.js`, `back-button.md.css`, `core.css`, `utils/theme.js` (`openURL`), `utils/transition/index.js` and
`buttons.md.css`.

- **Click** (`onClick`, on the host): `preventDefault()`; the closest `ion-nav` in the DOM; if `await nav.canGoBack()`,
  `nav.pop({ animationBuilder: routerAnimation, skipIfBusy: true })`. Otherwise `openURL(defaultHref, ev, 'back', routerAnimation)`:
  if the URL isn't null, doesn't start with `#` and has no scheme, and `document.querySelector('ion-router')` finds one, it's
  `router.push(url, 'back', animation)`; **with no `ion-router` it does nothing** (B3: a click on a shown root-page button with
  `default-href="/x"`: length 1, the URL still `/`, no error). The frameworks (Angular, React, Vue) replace this with their own
  router outlets' history, then `defaultHref`.
- **Shown** (`display: none` on the host by default): `.show-back-button { display: block }` while `defaultHref !== undefined`
  (the empty string shows it), and core.css's `.can-go-back > ion-header ion-back-button { display: block }`: in an `ion-header`
  that is a **direct child** of a page with `can-go-back`, which the nav sets on the entering page at each transition (B3: page
  two's header button shown; a button in its `ion-content` hidden; one in an `ion-header` inside a `div` in the content hidden).
- **`can-go-back` is set only at a transition** (`beforeTransition`), so it goes stale: push, then `removeIndex(0)` leaves one page
  with `can-go-back`, the button shown, and a click does nothing (B3). `push(…, { showGoBack: false })` hides the header button
  over a page that can go back; a shown content button there still pops (B3: length 3 → 2).
- **`skipIfBusy`**: a second click 50 ms into the pop is dropped (B4: 3 → 2, not 1).
- **Icon** (`backButtonIcon`): `icon` if not null, else `config.get('backButtonIcon', arrowBackSharp)`; drawn while truthy.
  `flip-rtl` is `icon === undefined`: the default and the config's icon mirror in RTL; an icon the app sets mirrors by
  `ion-icon`'s own rule (arrows and chevrons). B2: config `home` has `flip-rtl`; `icon="close"` hasn't.
- **Text** (`backButtonText`): `text` if not null, else `config.get('backButtonText', null)` in md; drawn while truthy, in a
  `span.button-text` with 4 px either side, uppercase from the host.
- **Icon only** (`back-button-has-icon-only`: an icon and no text): `--border-radius: 50%`, min 48 x 48, `aspect-ratio: 1/1`,
  and an **unbounded** ripple; otherwise a bounded one. B1: 48 x 48, icon 24 at 12, 12; with `text="Back"`: 92.02 x 32 (padding 12, icon
  24, the text's box 44.02 with its 4 either side, at 40, then 12), radius 4.
- **`aspect-ratio`** with `display: block` outside a flex row: a back button directly in a toolbar or in a plain `div` is as wide
  as the block, **390 x 390** (B1 `tbnobuttons`, `plain`); with text it's 390 x 32.
- **Disabled**: `opacity: 0.5`, `pointer-events: none`, `cursor: default`, the native button `disabled`.
- **Colour**: `:host(.ion-color) .button-native { color: base }` (B1: danger (197, 0, 15) on the native, the host still
  (66, 66, 66)); `:host(.in-toolbar:not(.in-toolbar-color)) { color: var(--ion-toolbar-color, var(--color)) }`; in a primary
  toolbar, white, inherited. Hover and focus: `::after` in `--background-hover` / `--background-focused` at their opacities;
  with a colour, hover's `::after` is the base colour, and the native's colour stays the base.
- **`ion-buttons` in a coloured toolbar** (`:host-context(.ion-color)::slotted(*) .button`, which reaches the back button by its
  `button` class): `--color: initial`, `--color-focused`, `--color-hover`, `--background-focused`, `--background-hover` the
  toolbar's contrast (B1 `intbc`: all `#fff`). The button's `currentColor` there is already the contrast, inherited, so nothing
  drawn changes, except that an app's own `--color` on the button is beaten by it.
- **Accessibility**: the native `<button>` has `aria-label` = the host's `aria-label`, else the text (as written, not
  uppercase: B1 `Back`), else `back`; the icon and the text are `aria-hidden`. `ion-focusable`, `ion-activatable`. The host's
  `tabindex` moves to the native button when it's 0 or more (`createTabindexController`); `-1` stays on the host and takes the
  button out of Tab (B5).
- **Config read**: `backButtonDefaultHref` once, in `componentWillLoad`, if `defaultHref` is undefined (B2: config `/cfg` shows
  it; setting `defaultHref = undefined` later hides it); `backButtonIcon` and `backButtonText` at each render.
- **No hardware back.** `back-button.js` doesn't listen to `ionBackButton`, and nothing listens on its behalf.

## The contract (for you to decide)

Three questions: what `defaultHref` and `routerAnimation` become with no router, how "shown" follows the nav, and how the button
sits beside hardware back.

### The pop: settled by the nav

`AvonicNav` (#130) already is the contract its PLAN wrote for #47: `CanGoBack()`, `Pop(NavOptions)` with `AnimationBuilder` and
`SkipIfBusy`, and `:can-go-back` (`AvonicNav.CanGoBackPseudoClass`) on the entering page, set in `BeforeTransition` exactly as
Ionic's, stale case and `ShowGoBack` included. The back button needs nothing else from the nav, and **no nav file changes**.

- **Click:** if enabled, the closest `AvonicNav` among the visual ancestors; if `nav.CanGoBack()`,
  `nav.Pop(new NavOptions { AnimationBuilder = RouterAnimation, SkipIfBusy = true })`, the same call `AvonicNavLink` makes for
  `RouterDirection.Back`. Otherwise the `defaultHref` path below. Ionic's `await nav.canGoBack()` is a microtask; Avonic's is
  synchronous, which no test can tell apart.
- **`RouterAnimation`** is kept in every option, as above: it's the pop's builder. Under option A it also travels with the event.

### Shown: following the page

`IsVisible` false at style priority (`display: none`), through a `StyleValue<bool>` bound in the constructor, as
`AvonicMenuButton` hides, so an app's own style wins; hidden, it's out of Tab and automation, as `display: none` is. Shown while:

1. `DefaultHref` isn't null (the empty string counts), **or**
2. it's inside an `AvonicHeader` that is a direct child of a page with `:can-go-back`. "Direct child": the header's logical
   parent is the page, or a `Panel` whose logical parent is the page (any number of panels), by CLAUDE.md's rule that a panel in
   a slot stands for the slot: an Avalonia page is a `UserControl` whose content is a `DockPanel` or `Grid` holding the header,
   where Ionic's page holds `ion-header` itself. "Page": a control with class `ion-page`, which the nav gives every page it builds.
   A header inside an `AvonicContent`, or behind a `Border`, isn't one (B3's `b2n`), as in Ionic.

It looks for the page when it's attached, and follows the page's `Classes.CollectionChanged` (pseudoclasses are in `Classes`) while
attached, as `ItemMultipleInputsObserver` follows an item: so it shows and hides the moment the nav sets `:can-go-back`, before the
transition's first frame, as the CSS does. Removing a page from under it without a transition leaves it shown, as in Ionic (the
stale case); a click then does nothing (option A: raises the event if `DefaultHref` is set, as Ionic would call the router).

**With no nav around**, no page has `:can-go-back`, so it shows only with a `DefaultHref`, and a click takes the `defaultHref`
path. A page in an `AvonicTab` has `ion-page` but never `:can-go-back`, as in Ionic.

### Hardware back: separate, as in Ionic

Nothing here touches hardware back, and nothing is duplicated:

- **`AvonicNav.HandleBackButton`** (the nav's extension, default true) already pops the innermost visible nav that can go back on
  Android's back, a mouse's back button or Alt+Left, at priority 0 through `HardwareBackButton`, or `BackRequested` with no
  `AvonicApp`. The back button doesn't register on `IonBackButtonEvent`, as Ionic's doesn't: the two only meet at the same
  `nav.Pop(SkipIfBusy)`, so a hardware back and a tap together pop once, as two taps do. They can differ in the nav they pop
  (the back button's closest; hardware back's innermost visible) and in the animation (only the button passes `RouterAnimation`).
- **`AvonicConfig.HandleUnclaimedBack`** decides what an unclaimed hardware back request does (by default, nothing leaves the
  app). The back button never raises a back request, so it never reaches it; and a hardware back on a root page never follows a
  back button's `DefaultHref`, as in Ionic. An app that wants that registers its own handler at a priority above 0.
- **`AvonicNavLink` with `RouterDirection.Back`** pops the same way, with no visibility rule and no `defaultHref`.

### `defaultHref` with no router: three options

What `defaultHref` does in core Ionic with no `ion-router`: it **shows** the button, and a click with nothing to pop **does
nothing**. With an `ion-router`, the click is `router.push(defaultHref, 'back', routerAnimation)`. Avonic has no router (out of
scope).

#### Option A — kept as data, raised as an event the app routes (recommended)

`DefaultHref` is `string?`, as Ionic's: it shows the button, and `AvonicConfig.BackButtonDefaultHref` is its default. A click with
nothing to pop raises an **extension** routed event, bubbling:

```csharp
public static readonly RoutedEvent<BackButtonDefaultHrefEventArgs> DefaultHrefRequestedEvent;   // Bubble
public event EventHandler<BackButtonDefaultHrefEventArgs>? DefaultHrefRequested;
public sealed class BackButtonDefaultHrefEventArgs : RoutedEventArgs
{
    public string DefaultHref { get; }
    public AnimationBuilder? RouterAnimation { get; }
}
```

raised when `openURL` would hand the URL to a router: not null, not starting with `#`, no scheme (`^[a-z][a-z0-9+\-.]*:`). It's
`router.push(url, 'back', animation)`, with the app as the router: one handler on the window or the `AvonicApp`, as there is one
`ion-router` per document, sees every back button's. With no handler, nothing happens: core Ionic with no router.

- **For:** Ionic's name, type and visibility; core's no-op when nobody routes; an app with its own navigation (a Shell, a
  ReactiveUI `RoutingState`, a view-model stack, several navs) maps a string to whatever it has. Nothing invented about what a
  URL means. Small: one event, two tests.
- **Against:** an app with only an `AvonicNav` writes the handler itself (three lines: `nav.SetRoot(...)` with a back direction).

#### Option B — `defaultHref` as a page pushed onto the nearest nav

`DefaultHref` becomes `object?`, in the nav's page forms (`{x:Type pages:HomePage}`, a `Control`, a factory, a view model; the
`NavComponent` row), and a click with nothing to pop is, on the closest nav,
`nav.SetRoot(component, null, new NavOptions { Direction = NavDirection.Back, AnimationBuilder = RouterAnimation, SkipIfBusy = true })`:
the frameworks' "navigate back to `defaultHref`" (the page replaces the stack, animated as a back).

- **For:** works with no code in an app that has only an `AvonicNav`.
- **Against:** the name says a URL and the value is a page, against the mapping rules' "same name"; a config default
  (`backButtonDefaultHref`) would hold a page type; it invents a behaviour core Ionic doesn't have (core does nothing), so it's a
  deviation to record, not Ionic's; with no nav around it can't do anything; and the choice between `SetRoot`, a `Push` with a
  back direction, or `InsertPages(0)` then `Pop` is ours, not Ionic's.
- A variant: keep option A's `string? DefaultHref`, and add the page as a separate extension property (say
  `DefaultComponent`), used before the event. Both, at the cost of a second way to do it.

#### Option C — omitted

No `DefaultHref`, no config default, no event; `RouterAnimation` stays for the pop.

- **For:** nothing to route.
- **Against:** loses what core Ionic's `defaultHref` does do with no router: show the button on a page with nothing under it (a
  root page reached by a deep link). An app would have to style `IsVisible` itself. And a prop Ionic has, absent, for a job it
  does in Ionic.

**Recommendation: A.** It is Ionic's prop as core Ionic runs it, and the one place Ionic needs a router becomes an event any
app's own navigation can take, without Avonic deciding what a URL means. B can come later as the variant's extension if apps ask.

### Decisions

- **Option A with the variant.** `DefaultHref` (`string?`) and `DefaultHrefRequested`, as A; and `DefaultPage` (`object?`, the nav's
  page forms), an extension. With nothing to pop: a `DefaultPage` and a nav around → `nav.SetRoot(page, null, { Direction: Back,
  AnimationBuilder: RouterAnimation, SkipIfBusy: true })`, and no event; otherwise the event, by `DefaultHref`'s rule. A
  `DefaultPage` shows the button, as a `DefaultHref` does.
- **A `DefaultPage` with no nav around** can't be gone to, so the click is `DefaultHref`'s, as if no page were set: the event if
  `DefaultHref` qualifies, else nothing. Chosen so that a back button in a view used both inside and outside a nav still reaches the
  app's router outside one.
- **`AvonicConfig.BackButtonText` and `.BackButtonDefaultHref`** added here, with `BackButtonIcon`'s summary naming the back button.

## Design (whatever the option)

**`AvonicBackButton : TemplatedControl`** (no slot), in `Controls/BackButton/`, after `AvonicMenuButton`:

- `NativeButton` (click, Enter, Space; press and release), `Activatable` and `FocusVisible` at style priority in the constructor,
  `Focusable` by `OverrideDefaultValue`. `ion-focusable` and `ion-activatable` always.
- `:in-toolbar`, `:in-toolbar-color` worked out from the visual ancestors when it's attached and at each prop change, as
  `AvonicMenuButton.Render` does; `:icon-only` (`back-button-has-icon-only`) from the resolved icon and text.
- Render (Ionic's getters, read at the first attach and each prop change): the icon `Icon ?? AvonicConfig.BackButtonIcon ??
  "arrow-back-sharp"`, `FlipRtl` true when `Icon` is null, else null (the icon's own rule); the text `Text ??
  AvonicConfig.BackButtonText`; each part shown while it isn't null or empty; the ripple's `Type`.
- `TextTransform` as an extension `AddOwner` of `AvonicButton.TextTransformProperty`, set `Uppercase` by the theme, as
  `AvonicItemOption` does (the `text-transform` row), through `AvonicButton.TextTransformConverter`.
- `aspect-ratio: 1/1` with only an icon: `MeasureOverride` gives a square of the larger side; when it's stretched across a finite
  width (Avalonia's `display: block` in a vertical stack), the side is that width, as B1's 390 x 390. In a toolbar's
  `AvonicButtons`, a horizontal row, it's 48 x 48.
- The `ion-buttons` coloured-toolbar variables (above) aren't mapped: they change nothing drawn by default (B1: all equal to the
  inherited `#fff`), and mapping them is a global rule in `Styles/` for each alternative. Recorded as a deliberate difference.
- **Automation:** a `Button` peer with `IInvokeProvider` (a click); its name `AutomationProperties.Name` (the `aria-label`), else
  the resolved text as written, else `back`; the icon and text parts aren't content elements. `TabIndex` and `IsTabStop` are the
  host's own, which covers the `tabindex` controller.

**Theme:** `Controls/AvonicBackButton.axaml` in Themes, a translation of `back-button.md.css` in its order, built from
`AvonicMenuButton.axaml`'s (the native, clip, overlay, ripple, inner); `Tokens/BackButton.axaml` with the 32 keys and
`Avonic.BackButton.Md.*`. No `Styles/` file: every rule is on the host or its template.

## Behaviours to test

| # | Behaviour | Source |
|---|---|---|
| K1 | Hidden by default, with no nav and no `DefaultHref`; shown with a `DefaultHref`, the empty string included; hidden again when it's cleared | `:host { display: none }`, `.show-back-button`; B1, B2 |
| K2 | Shown in the header of a nav's page with `:can-go-back`, hidden on the root page; it shows the moment the push sets the pseudoclass, before the transition ends | core.css; B3 |
| K3 | Not shown in a can-go-back page's content, nor in a header that isn't the page's direct child (inside the content); shown through a `DockPanel` or `Grid` between the page and its header | core.css `>`; B3 `b2c`, `b2n` |
| K4 | Click in a page that can go back pops the closest nav, with `SkipIfBusy`: two clicks 50 ms apart pop once | `onClick`; B3, B4 |
| K5 | Click passes `RouterAnimation` as the pop's builder | `onClick` |
| K6 | Click with nothing to pop: nothing happens (stale `:can-go-back` after `RemoveIndex(0)`: still shown, a click does nothing) | `openURL` with no router; B3 |
| K7 | (Option A) the event with `DefaultHref` and `RouterAnimation`, bubbling, only when nothing pops; not for `#…` or a scheme | `openURL` |
| K8 | `ShowGoBack: false` hides the header button; a shown content button there still pops | B3 |
| K9 | Disabled: no pop, opacity 0.5 | `.back-button-disabled`; B1 |
| K10 | Enter and Space click it; a press from automation (`Invoke`) clicks it | native `<button>` |
| K11 | Icon: md's `arrow-back-sharp`, mirrored in RTL; `AvonicConfig.BackButtonIcon` likewise; an `Icon` set uses the icon's own rule; empty hides it | `backButtonIcon`, `flip-rtl`; B1, B2 |
| K12 | Text: none in md; `Text` shown uppercase; `AvonicConfig.BackButtonText`; empty is none | `backButtonText`; B1, B2 |
| K13 | Icon only: 48 x 48, radius 50%, unbounded ripple; with text: 32 high, radius 4, bounded ripple, 92 wide for `Back` | md.css; B1 |
| K14 | `aspect-ratio`: icon only, stretched in a vertical stack 390 wide, is 390 x 390 | B1 |
| K15 | `DefaultHref` null at the first attach takes `AvonicConfig.BackButtonDefaultHref`; clearing it later hides it | `componentWillLoad`; B2 |
| K16 | Colour: the native's colour is the base; in an uncoloured toolbar, the toolbar's colour; in a coloured one, inherited | md.css; B1 |
| K17 | Hover (mouse) and focus (keyboard): the overlay at 0.04 and 0.12 of the current colour; with a colour, hover's overlay is the base | md.css |
| K18 | Automation: a button named `back`, the text, or `AutomationProperties.Name`; disabled with the host; hidden is out of the tree | `aria-label`, `inheritAriaAttributes` |
| K19 | No hardware back of its own: with the nav's `HandleBackButton` off, a back request doesn't pop through the back button | `back-button.js` |
| K20 | (Extension) `DefaultPage` shows it; with nothing to pop it's the nav's new root, going back with `RouterAnimation`, and no event; a pop comes first; with no nav the click is `DefaultHref`'s | the decision |

Each test fails without its behaviour; `tools/breakage` proves it.

## Proposed for shared files

`AvonicConfig.cs` (a shared file; I haven't edited it). The back button reads `BackButtonIcon`, which exists, and needs two keys
that don't. Exact text:

```csharp
    /// <summary>
    /// Ionic's <c>backButtonText</c> config: the text of an <see cref="AvonicBackButton"/> whose <see cref="AvonicBackButton.Text"/>
    /// is null. Null is md's: no text. Read when the button renders: its first attach and each change of one of its props.
    /// </summary>
    public static string? BackButtonText { get; set; }

    /// <summary>
    /// Ionic's <c>backButtonDefaultHref</c> config: the <see cref="AvonicBackButton.DefaultHref"/> of a back button that has none
    /// when it's first attached, as Ionic reads it in <c>componentWillLoad</c>. Clearing the button's later doesn't read it again.
    /// </summary>
    public static string? BackButtonDefaultHref { get; set; }
```

and `BackButtonIcon`'s summary to name the back button too: "the icon of an `AvonicBackButton` whose `Icon` is null, read when it
renders, and of an `AvonicSearchbar`'s md cancel button …". Until they exist, `Text` and `DefaultHref` would have no config default.

A `CLAUDE.md` mapping row for the visibility, at the roll-up: "`.can-go-back > ion-header ion-back-button` | the back button
follows its header's page's `:can-go-back` through the page's `Classes`, the header the page's logical child or behind panels |
`AvonicBackButton`".

## Deliberate differences (planned)

- The coloured toolbar's `ion-buttons` variables aren't mapped (no visible change by default, measured).
- "Direct child" of the page lets panels between, as an Avalonia page needs one to hold its header and content.
- `--icon-font-weight` draws nothing different: an `AvonicIcon` is a path, as Ionic's SVG.

## Extensions

`DefaultHrefRequested` (option A), `DefaultPage` (the variant), and `TextTransform`, as `AvonicItemOption`'s.

## Dependencies

`AvonicIcon`, `AvonicRippleEffect`, `AvonicToolbar`, `AvonicButtons`, `AvonicHeader`, `AvonicNav`: all merged.
