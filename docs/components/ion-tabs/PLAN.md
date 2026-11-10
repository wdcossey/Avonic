# ion-tabs family — Plan

`ion-tabs` holds `ion-tab`s, the pages, and an `ion-tab-bar` of `ion-tab-button`s, and shows one page at a time. One family, one
PR (#29). There's no router yet (`ion-router`, `ion-router-outlet`), so this is Ionic's standalone mode, where `ion-tabs` selects
the first tab at load and a tab button's click selects its tab through `select()`.

Measurements are from real Ionic 8 in Chromium, md, 390 px wide, wrapped in `ion-app`, with throwaway Playwright scripts: the
reference page in [COMPARISON.md](COMPARISON.md), and a page of seven bars (every layout, a badge in each, an empty badge, a
disabled button, a colour, the top slot, RTL, a long label, one button). Events were logged on the tabs and the document, clicks
were real mouse clicks, and the keys were pressed with the page focused.

## The navigation contract

- **`AvonicTabs`** is a `TemplatedControl` with three slots: `TopContent`, `Content` (the default slot, `.tabs-inner`, which takes
  the rest of the height) and `BottomContent`. Its tabs are the `AvonicTab`s anywhere in them, in that order, found when it needs them,
  as `querySelectorAll('ion-tab')` finds them. A slot holds one child, so several tabs go in a `Panel`, which overlaps them as
  `.ion-page`'s `position: absolute; inset: 0` does.
- **`AvonicTab`** is a `ContentControl`: the page, hidden until it's active. A hidden tab keeps its content, as Ionic keeps the DOM.
  `Component` is built the first time it's shown, once, which is Ionic's lazy loading.
- **`Select`** switches. A tab button's click asks its tabs to select its `Tab`. The tabs then write the bar's `SelectedTab`, and the
  bar marks its buttons. A bar on its own does nothing on a click: an app sets `SelectedTab`.

## Fidelity checklist

`node tools/ionic-api.js ion-tabs`, `ion-tab`, `ion-tab-bar`, `ion-tab-button`.

### ion-tabs: events (2), methods (3), slots (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `ionTabsDidChange` | `IonTabsDidChange` (`TabsChangeEventArgs.Tab`) | Direct, as Ionic's doesn't bubble. After the new tab shows and the old one hides |
| [x] | `ionTabsWillChange` | `IonTabsWillChange` (`TabsChangeEventArgs.Tab`) | Direct. Before anything changes |
| [x] | `getSelected()` | `string? GetSelected()` | Sync: the promise resolves at once |
| [x] | `getTab(tab)` | `AvonicTab? GetTab(string)`, `GetTab(AvonicTab)` | The tab with that id; a tab is returned as it is, as Ionic returns an element |
| [x] | `select(tab)` | `bool Select(string)`, `Select(AvonicTab)` | Sync. False if it isn't found, is already selected, or a selection is under way |
| [x] | `(default)` | `Content`, `ContentTemplate` | `.tabs-inner`, `flex: 1` |
| [x] | `bottom` | `BottomContent`, `BottomContentTemplate` | |
| [x] | `top` | `TopContent`, `TopContentTemplate` | A tab bar here is `[slot=top]` |

No props, parts or variables. Internal: `useRouter` (out of scope: router), `ionNavWillLoad` (out of scope: router),
`setRouteId`, `getRouteId` (out of scope: router).

### ion-tab: props (2), methods (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `component` | `Component` (`object?`) | A `Control`, or a `Type` with a parameterless constructor, built at the first activation and given class `ion-page`. A tab with both a `Component` and `Content` logs Ionic's error and keeps its `Content` |
| [x] | `tab` | `Tab` (`string?`) | Required in Ionic; a tab without one can only be selected by reference |
| [x] | `setActive()` | `SetActive()` | Builds the component and shows the tab. Like Ionic's, it hides no other tab |

Internal `active` is `IsActive`, read-only; `delegate` is out of scope: framework delegate.

### ion-tab-bar: props (4), variables (3)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | `ColorPalette.ColorProperty.AddOwner`; its buttons get `:in-tab-bar-color` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `selectedTab` | `SelectedTab` (`string?`) | Marks each button whose `Tab` it is |
| [x] | `translucent` | `Translucent` (`bool`) | md has rules for it too: the buttons' background is transparent. `backdrop-filter` is iOS only |
| [x] | `--background` | `Avonic.TabBar.Background` | Unset: `Avonic.TabBarBackgroundBrush` (`--ion-tab-bar-background`), in `Global.axaml` |
| [x] | `--border` | `Avonic.TabBar.BorderWidth` and `Avonic.TabBar.BorderColor` | A shorthand has no Avalonia value, so it's two keys; Avalonia draws solid borders only. Unset colour: `Avonic.TabBarBorderColorBrush`, then the literal |
| [x] | `--color` | `Avonic.TabBar.Color` | Unset: `Avonic.TabBarColorBrush`, then `Avonic.TextColorStep350Brush` (#595959) |

`tab-bar-hidden` (the keyboard) isn't implemented: see Deviations.

### ion-tab-button: props (9), parts (1), variables (11)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | Mapping rule. Opacity 0.4 and no input |
| [ ] | `download` | — | Not applicable: only applies with `href` |
| [ ] | `href` | — | Out of scope: router, as on `AvonicButton`. See "For you to decide" |
| [x] | `layout` | `Layout` (`TabButtonLayout`: `IconTop`, `IconStart`, `IconEnd`, `IconBottom`, `IconHide`, `LabelHide`) | `IconTop` by default, Ionic's fallback for its `tabButtonLayout` config |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `rel` | — | Not applicable: only applies with `href` |
| [x] | `selected` | `Selected` (`bool`) | `:selected`. The bar writes it |
| [x] | `tab` | `Tab` (`string?`) | |
| [ ] | `target` | — | Not applicable: only applies with `href` |
| [x] | `native` (part) | `PART_Native` | The anchor: padding, clipping, the focused overlay and the ripple |

Its children, an `AvonicIcon`, an `AvonicLabel` and an `AvonicBadge`, are `Items`, as `core.json` lists no slot. The button is an
`ItemsControl` whose panel is `.button-inner`'s flex box, `TabButtonPanel`. Its internal `ionTabButtonClick` is the internal routed
event `AvonicTabButton.IonTabButtonClickEvent`.

| ✓ | Variable | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.TabButton.Background` | Unset: the bar's `--background`, which it inherits |
| [x] | `--background-focused` | `Avonic.TabButton.BackgroundFocused` | Unset: `Avonic.TabBarBackgroundFocusedBrush` (`--ion-tab-bar-background-focused`, #e0e0e0) |
| [x] | `--background-focused-opacity` | `Avonic.TabButton.BackgroundFocusedOpacity` | 1 |
| [x] | `--color` | `Avonic.TabButton.Color` | Unset: the bar's `--color` |
| [x] | `--color-focused` | `Avonic.TabButton.ColorFocused` | Unset: the host's colour |
| [x] | `--color-selected` | `Avonic.TabButton.ColorSelected` | Unset: `Avonic.TabBarColorSelectedBrush` (`--ion-tab-bar-color-selected`), then `Avonic.Color.PrimaryBrush` |
| [x] | `--padding-bottom`, `--padding-end`, `--padding-start`, `--padding-top` | `Avonic.TabButton.Padding*` | 0, 12, 12, 0 |
| [x] | `--ripple-color` | `Avonic.TabButton.RippleColor` | Unset: `--color-selected` |

In CSS the button inherits `--background`, `--color`, `--color-selected` and `--background-focused` from the bar, which sets them.
Here the button's keys are unset and fall back to the bar's, and a resource set on the bar reaches the buttons, as a variable set on
it does.

## Behaviours

Each becomes a test. "Measured" is from the scripts above.

### The tabs

| # | Behaviour | Source |
|---|---|---|
| T1 | At load it selects its first tab: that tab shows, the others stay hidden, `IonTabsWillChange` and `IonTabsDidChange` are raised for it, and the bar's `SelectedTab` is its id | `componentWillLoad`; measured `initial` |
| T2 | A click on a tab button selects the tab with the button's `Tab`; a click on the selected one changes nothing | `onTabClicked`; measured `clickRadio`, `clickRadioAgain` |
| T3 | `Select` returns false, changing nothing, for a tab that isn't there (logging Ionic's error), the selected tab, or during a selection (from a handler); true otherwise | `select`, `shouldSwitch`, `getTab`; measured `[true, false, false]`, and `concurrent` |
| T4 | The order: `IonTabsWillChange` with nothing changed; the new tab shows and the bar's `SelectedTab` is written, so its buttons are marked; the old tab hides; `IonTabsDidChange` | `setActive`, `tabSwitch`; measured `will … sel=home`, `barchanged`, `did … sel=radio` |
| T5 | `GetSelected` gives the selected tab's id; `GetTab` the tab with an id, or null | `getSelected`, `getTab`; measured `"library", "home", null` |
| T6 | The bar it writes is the first `AvonicTabBar` in it, the bar slots before the default slot, and it's only written when the id differs | `updateTabBar` |
| T7 | The slots stack: top, the default slot taking the rest, bottom; measured a tab 787 high over a bar 57 high at y 787 | `tabs.css` |

### The tab

| # | Behaviour | Source |
|---|---|---|
| A1 | Hidden until it's active (`display: none`), and hidden from automation | `tab-hidden`, `aria-hidden` |
| A2 | `Component` is built at the first activation, once, with class `ion-page`; without one the tab has class `ion-page` | `prepareLazyLoaded`, `attachComponent`, `render` |
| A3 | `SetActive` builds the component and shows the tab, hiding no other | `setActive` |
| A4 | Automation: a pane, named by the tab button with the same `Tab` | `role="tabpanel"`, `aria-labelledby` |

### The tab bar

| # | Behaviour | Source |
|---|---|---|
| B1 | `SelectedTab` marks each `AvonicTabButton` in it, `Selected` when its `Tab` is the bar's, from its load on; a change before load waits for the load, and null changes nothing | `selectedTabChanged`, `componentDidLoad`, the button's `onTabBarChanged`; measured `selectedAttr` |
| B2 | A click on its button, with no tabs around it, selects nothing | Measured `standaloneClick` |
| B3 | The buttons share the width (`flex: 1`), each at most 168 and at least its content's min-content, centred | Measured: 4 → 97.5 each; 1 → 168 at x 111; a long label → 168, 111, 111 |
| B4 | 56 high inside a 1 px border on top (content-box, 57 in all); in the top slot the border is at the bottom; padding from the safe areas: left, right, and bottom, or top in the top slot | `tab-bar.md.css`, `[slot=top]`; measured 57 |
| B5 | `Color`: the bar's background is the base and its colour the contrast at 0.7; each button's too, a selected one's the contrast, the ripple the contrast and the focused background the shade | `:host(.ion-color)` rules; measured `b4` |
| B6 | `Translucent`: the buttons' background is transparent, whatever their own | `.tab-bar-translucent`; measured `b9` with `--background: red` |
| B7 | Automation: a tab list | `role="tablist"` |

### The tab button

| # | Behaviour | Source |
|---|---|---|
| N1 | A click (release) raises the internal click with its `Tab` and `Selected`, if it has a `Tab`; a disabled one takes no input | `selectTab`, `tab-disabled`; measured `clickDisabled` |
| N2 | Enter or Space released on it does the same. It isn't focusable by default, as an `<a>` with no `href` isn't: Tab never reached one | `onKeyUp`; measured `tab1`, `tab2`, `focusHome` |
| N3 | Disabled: opacity 0.4 | `:host(.tab-disabled)` |
| N4 | Selected: the selected colour | `:host(.tab-selected)`; measured rgb(0, 84, 233) |
| N5 | `:has-label`, `:has-icon`, `:has-label-only`, `:has-icon-only` from an `AvonicLabel` or `AvonicIcon` in it | `hasLabel`, `hasIcon` |
| N6 | `Layout`: icon-top a column, icon-bottom a reversed one, icon-start a row, icon-end a reversed one, the icon first (`order: -1`); icon-hide hides the icon, label-hide the label; each with its margins | `tab-button.md.css`; measured `b2`, `b3` |
| N7 | An icon is 22, or 24 with no label; a label is 12, one line with an ellipsis, or wrapping with no icon | `::slotted(ion-icon)`, `::slotted(ion-label)`; measured |
| N8 | A badge is out of the flow, at `top` 8 or 16 and `inset-inline-start` 50% + 6, 70% or 80% of the inner box, by layout; 8 px text, padding 3 2 2 2, radius 8, at least 12 wide; empty, it's an 8 by 8 dot; mirrored in RTL | `::slotted(ion-badge)`; measured `b1`–`b3`, `b6` |
| N9 | Activatable, with an unbounded ripple in `--ripple-color`, clipped to the button | `ion-activatable`, `ion-ripple-effect type="unbounded"` |
| N10 | With a mouse over it, the selected colour | `a:hover` under `any-hover: hover`; measured `hover` |
| N11 | Focused (if an app makes it focusable): `--color-focused`, and `--background-focused` over it at its opacity | `:host(.ion-focused)` |
| N12 | Automation: a tab item, selected when `Selected`, named by its text | `role="tab"`, `aria-selected` |

## Visual states and keys

- **Bar:** background, colour and border from the keys above; `:ion-color` (base, contrast at 0.7); `:slot-top` (border at the
  bottom, top safe area).
- **Button:** colour and background from the keys; `:selected` (`ColorSelected`); `:ion-hover` (`ColorSelected`); `:ion-focused`
  (`ColorFocused`, the overlay `BackgroundFocused` at `BackgroundFocusedOpacity`); `:disabled` (0.4); `:in-tab-bar-color` and
  `:in-tab-bar-translucent`, which the button works out from its parent bar.
- **Children:** the slotted margins, sizes and the badge's place by layout. md's `::slotted` rules for each state set
  inheriting values on the button, `TabButtonPanel.IconMargin` and so on, from its `ControlTheme`; three global rules in
  `Styles/TabButton.axaml`, one for each kind of child, read them. That's 3 alternatives for the style budget, where a rule per CSS
  rule would be over 20.

## Deviations

| What | Why |
|---|---|
| A tab bar doesn't hide while the on-screen keyboard is open (`tab-bar-hidden`) | It needs `TopLevel.InputPane`, which headless tests can't open, so it would ship untested, as the footer's keyboard rule didn't ship (ion-toolbar PLAN) |
| No `href`, `download`, `rel`, `target` | `AvonicButton`'s precedent: no router, and a link prop that does nothing would be dead |
| `tabButtonLayout` config | `AvonicConfig` is a shared file. `Layout` defaults to Ionic's fallback, `IconTop`; proposed in the PR |
| `--border` is two keys | A CSS shorthand has no Avalonia value |
| A hidden tab is hidden at style priority, not `!important` | An app's own `IsVisible` on a tab wins, as every control's behaviour here lets it |
| `Component` and `Content` together keep the `Content` | A `ContentControl` holds one. Ionic logs an error and appends both |
| The bar the tabs write is the first in the top slot, then the bottom slot, then the default slot | Ionic's `querySelector` takes the first in the markup, which its author orders; slots have no order here, and a bar inside a page, such as a page's own, mustn't take the main bar's place |
| Content in a tab finds the tab as its page, not the tabs | `AvonicContent`'s `getPageElement` doesn't know `AvonicTabs` yet: proposed in the PR |

## Extensions

None.

## Dependencies

`AvonicIcon`, `AvonicLabel`, `AvonicBadge` (#3, #7, #16) and `AvonicRippleEffect`: all merged. `Behaviors.Activatable`,
`FocusVisible`, `NativeButton`, `ColorPalette`, `Css`, `AccessibleText` and `MinContent`: used as they are.
