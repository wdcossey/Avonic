# ion-tabs

Status: done · [#29](https://github.com/wdcossey/Avonic/issues/29)

`AvonicTabs` shows one of its `AvonicTab`s at a time, with an `AvonicTabBar` of `AvonicTabButton`s to switch them. The four were
built together, and share a [PLAN.md](PLAN.md) and a [COMPARISON.md](COMPARISON.md). This is Ionic's standalone mode: there's no
router (it's out of scope), so the tabs select their first tab at load, and a tab button's click selects its tab.

```xml
<c:AvonicTabs IonTabsDidChange="OnTabChanged">
    <Panel>
        <c:AvonicTab Tab="home"><local:HomePage /></c:AvonicTab>
        <c:AvonicTab Tab="radio" Component="{x:Type local:RadioPage}" />
    </Panel>
    <c:AvonicTabs.BottomContent>
        <c:AvonicTabBar>
            <c:AvonicTabButton Tab="home">
                <c:AvonicIcon IconName="home" />
                <c:AvonicLabel Content="Home" />
            </c:AvonicTabButton>
            <c:AvonicTabButton Tab="radio">
                <c:AvonicIcon IconName="radio" />
                <c:AvonicLabel Content="Radio" />
                <c:AvonicBadge Content="6" />
            </c:AvonicTabButton>
        </c:AvonicTabBar>
    </c:AvonicTabs.BottomContent>
</c:AvonicTabs>
```

## How it fits together

- The tabs' **slots** are `TopContent`, `Content` and `BottomContent`, stacked; the default slot takes the rest of the height. A slot
  holds one child, so several tabs go in a `Panel`, which overlaps them as Ionic's absolutely placed pages are.
- Its **tabs** are the `AvonicTab`s anywhere in the slots, found when they're needed. A hidden tab keeps its content.
- **`Select`** switches: `IonTabsWillChange` with nothing changed, then the new tab shows and the first tab bar in it gets the
  tab's id as its `SelectedTab`, then the old tab hides and `IonTabsDidChange`. A bar marks each button whose `Tab` is its
  `SelectedTab`.
- A **tab button's click** asks the tabs around it to select its `Tab`. A bar on its own does nothing on a click: set its
  `SelectedTab`.
- **`Component`** is built the first time its tab is shown, once: a `Control`, or a `Type` with a parameterless constructor.

## API

### ion-tabs

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionTabsDidChange` | `IonTabsDidChange` (`TabsChangeEventArgs.Tab`) | Direct: it doesn't bubble |
| [x] | `ionTabsWillChange` | `IonTabsWillChange` | Direct |
| [x] | `getSelected()` | `string? GetSelected()` | |
| [x] | `getTab(tab)` | `GetTab(string)`, `GetTab(AvonicTab)` | |
| [x] | `select(tab)` | `bool Select(string)`, `Select(AvonicTab)` | Sync. False if it isn't there, is selected, or a selection is under way, from a handler |
| [x] | `(default)` | `Content`, `ContentTemplate` | |
| [x] | `bottom` | `BottomContent`, `BottomContentTemplate` | |
| [x] | `top` | `TopContent`, `TopContentTemplate` | A tab bar here has its border at the bottom and the top safe area |

### ion-tab

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `component` | `Component` (`object?`) | A `Control` or a `Type`, built at the first activation with class `ion-page` |
| [x] | `tab` | `Tab` (`string?`) | |
| [x] | `setActive()` | `SetActive()` | Shows it, and hides no other |

Read-only `IsActive` is Ionic's internal `active`. A tab without a `Component` has class `ion-page`.

### ion-tab-bar

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `selectedTab` | `SelectedTab` (`string?`) | |
| [x] | `translucent` | `Translucent` (`bool`) | In md, the buttons' background is transparent |

Its buttons are `Items`.

### ion-tab-button

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | |
| [ ] | `download`, `rel`, `target` | — | Not applicable: only with `href` |
| [ ] | `href` | — | Out of scope: router, as on `AvonicButton` |
| [x] | `layout` | `Layout` (`TabButtonLayout`) | Unset, `AvonicConfig.TabButtonLayout` (Ionic's `tabButtonLayout`) on the first attach, at style priority, else `IconTop` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `selected` | `Selected` (`bool`) | The bar writes it |
| [x] | `tab` | `Tab` (`string?`) | |
| [x] | `native` (part) | `PART_Native` | |

Its icon, label and badge are `Items`. It isn't focusable by default, as Ionic's `<a>` with no `href` isn't; with `Focusable` set,
Enter and Space released on it click.

## Behaviours

All of [PLAN.md](PLAN.md#behaviours): T1–T7, A1–A4, B1–B7, N1–N12, each with a test in `AvonicTabsTests`, `AvonicTabBarTests` and
`AvonicTabButtonTests`.

## Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `ion-tab-bar` `--background` | `Avonic.TabBar.Background` | Unset: `Avonic.TabBarBackgroundBrush` |
| [x] | `ion-tab-bar` `--border` | `Avonic.TabBar.BorderWidth`, `Avonic.TabBar.BorderColor` | 1; unset: `Avonic.TabBarBorderColorBrush`, then `rgba(0, 0, 0, 0.07)`; in `Tokens/Palettes/Dark.axaml`, `Avonic.BorderColor`, `#363636` |
| [x] | `ion-tab-bar` `--color` | `Avonic.TabBar.Color` | Unset: `Avonic.TabBarColorBrush`, which follows `Avonic.TextColorStep350` |
| [x] | `--background` | `Avonic.TabButton.Background` | Unset: the bar's |
| [x] | `--background-focused` | `Avonic.TabButton.BackgroundFocused` | Unset: `Avonic.TabBarBackgroundFocusedBrush`, #e0e0e0 |
| [x] | `--background-focused-opacity` | `Avonic.TabButton.BackgroundFocusedOpacity` | 1 |
| [x] | `--color` | `Avonic.TabButton.Color` | Unset: the bar's |
| [x] | `--color-focused` | `Avonic.TabButton.ColorFocused` | Unset: the host's colour |
| [x] | `--color-selected` | `Avonic.TabButton.ColorSelected` | Unset: `Avonic.TabBarColorSelectedBrush`, which follows `Avonic.Color.Primary` |
| [x] | `--padding-*` | `Avonic.TabButton.PaddingTop`, `PaddingEnd`, `PaddingBottom`, `PaddingStart` | 0, 12, 0, 12 |
| [x] | `--ripple-color` | `Avonic.TabButton.RippleColor` | Unset: the selected colour |

The globals `--ion-tab-bar-color`, `--ion-tab-bar-color-selected`, `--ion-tab-bar-background-focused` and
`--ion-tab-bar-border-color` are `Avonic.TabBarColorBrush`, `Avonic.TabBarColorSelectedBrush`, `Avonic.TabBarBackgroundFocusedBrush`
(and `Avonic.TabBarBackgroundFocused`) and `Avonic.TabBarBorderColorBrush`, in `Tokens/Tabs.axaml`. A key set on a bar reaches its
buttons, as a variable set on it does. md's `::slotted` values by layout are the inheriting `TabButtonPanel` properties, which the
button's theme sets and `Styles/TabButton.axaml` reads.

## Extensions

None.

## Known gaps

- **The keyboard.** Ionic hides a bottom bar while the on-screen keyboard is open (`tab-bar-hidden`). Not done: it needs
  `TopLevel.InputPane`, which headless tests can't open, so it would ship untested, as the footer's keyboard rule didn't.
- **`href`, `download`, `rel`, `target`** aren't declared, as on `AvonicButton`: without a router a link would do nothing.
- **With `AvonicConfig.TabButtonLayout` set,** clearing an app's own `Layout` gives the config's back, where Ionic's `undefined`
  after the load renders as icon-top; and a setter in an app's `ControlTheme` loses to it, as CSS can't set the prop. The config's
  value is at style priority so that every style of the app's wins, and it comes back when the style stops.
- **A hidden tab** is hidden at a style trigger's priority, which beats an app's plain style, as `!important` does. An app's own
  `IsVisible` on a tab still wins, as every control's behaviour here lets it, and so does its style with a class or a pseudoclass,
  where Ionic's `display: none !important` beats both.
- **`Component` with `Content`** keeps the content, logs Ionic's error, and doesn't build the component; Ionic appends both.
- **An `IonTabsWillChange` handler that throws** stops the switch and the exception leaves `Select`, or the click. In Ionic a
  listener's error goes to `window.onerror` and the switch goes on. Here the switch is undone, so the selection stays whole.
- **A tab button's children are controls.** Its `ItemsSource` and `ItemTemplate`, which `ItemsControl` has, make containers the
  slotted rules and the layout don't see, as with `AvonicSegmentButton`.
- **A tab selected before the load** is kept, and the first isn't selected first, where Ionic selects the first at its load and
  then runs the call, with both events for each.
- **The bar the tabs mark** is the first in `TopContent`, then `BottomContent`, then `Content`. Ionic takes the first in its
  markup, which the author orders; slots have no order here, so a bar inside a page never takes the main bar's place.
- **Neither `AvonicTabs` nor `AvonicTab` clips,** as `ion-tabs` (`contain` without paint) and `.ion-page` don't: a fullscreen
  content shows behind a bottom bar, and with a top bar its scroll part runs past the tabs' bottom by the bar's height and is
  drawn there, as Ionic's `offsetTop` counts from the tab (measured in Chromium). In an app whose tabs fill the window, that's off
  screen.
