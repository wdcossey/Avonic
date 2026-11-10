# ion-breadcrumbs

Status: done · [#33](https://github.com/wdcossey/Avonic/issues/33)

`AvonicBreadcrumbs` is a wrapping row of `AvonicBreadcrumb`s: where the user came from, with a separator after each but the last,
the last one active, and a collapsed indicator standing for the middle of a long trail. The design and evidence are in
[PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicBreadcrumbs MaxItems="4" IonCollapsedClick="OnCollapsedClick">
    <c:AvonicBreadcrumb Href="#home" Content="Home" Click="OnHome" />
    <c:AvonicBreadcrumb Href="#electronics" Content="Electronics" />
    <c:AvonicBreadcrumb Href="#photography" Content="Photography" />
    <c:AvonicBreadcrumb Href="#cameras" Content="Cameras" />
    <c:AvonicBreadcrumb Content="Film">
        <c:AvonicBreadcrumb.StartContent>
            <c:AvonicIcon IconName="film" />
        </c:AvonicBreadcrumb.StartContent>
    </c:AvonicBreadcrumb>
</c:AvonicBreadcrumbs>
```

## API: AvonicBreadcrumbs

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | The breadcrumbs inside take its base, and its shade on hover over a link |
| [x] | `itemsAfterCollapse` | `ItemsAfterCollapse` (`int`, 1) | 0 makes the indicator's breadcrumb the last |
| [x] | `itemsBeforeCollapse` | `ItemsBeforeCollapse` (`int`, 1) | |
| [x] | `maxItems` | `MaxItems` (`int?`) | Null never collapses. Nothing collapses if before and after come to more |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `ionCollapsedClick` | `IonCollapsedClick` | `BreadcrumbCollapsedClickEventArgs`: `CollapsedBreadcrumbs`, in order, and `IonShadowTarget`, the indicator |

The children are `Items` (no slot in `core.json`), so `ItemsSource` and `ItemTemplate` work too. Breadcrumbs anywhere inside
count, in order. It looks again when `Items` changes, once for all the changes made together as a browser's `slotchange` does,
and when `MaxItems`, `ItemsBeforeCollapse` or `ItemsAfterCollapse` changes.

## API: AvonicBreadcrumb

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `active` | `Active` | The breadcrumbs sets it on the last if none is, as Ionic assigns the prop, so an app reads it back. A binding stays |
| [x] | `color` | `Color` | |
| [x] | `disabled` | `IsEnabled` | 0.5, no input, no tab stop |
| [ ] | `download` | — | Not applicable: only applies when a browser follows `href` |
| [x] | `href` | `Href` | Makes it a link: a tab stop, Enter clicks it, hover and focus show. **It doesn't navigate**: follow it from `Click` or `Command` until there's a router |
| [ ] | `mode` | — | ADR 0001 |
| [ ] | `rel`, `target` | — | Not applicable: only apply when a browser follows `href` |
| [ ] | `routerAnimation`, `routerDirection` | — | Out of scope: router |
| [x] | `separator` | `Separator` (`bool?`) | Null leaves it to the breadcrumbs, which sets true on all but the last. The last never shows one |
| [x] | `ionFocus`, `ionBlur` | `IonFocus`, `IonBlur` | The breadcrumb's own focus, not its indicator's |

Slots, which `core.json` doesn't list and `breadcrumb.js` renders: `StartContent`, `Content` (default), `EndContent` and
`SeparatorContent`, which replaces md's `/`, each with a `…Template`. Parts: `PART_Native` (`Border`), `PART_CollapsedIndicator`
(`Button`) and `PART_Separator`.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background-focused` | `Avonic.Breadcrumb.BackgroundFocused` | Unset: `#fff` |
| [x] | `--color` | `Avonic.Breadcrumb.Color` | Unset: `#677483` |
| [x] | `--color-active` | `Avonic.Breadcrumb.ColorActive` | Unset: `#03060b` |
| [x] | `--color-focused` | `Avonic.Breadcrumb.ColorFocused` | Unset: `#35404e` |
| [x] | `--color-hover` | `Avonic.Breadcrumb.ColorHover` | Unset: `#03060b` |

Set, each beats md's own value, as a variable set from outside beats `:host` in Ionic. `Avonic.Breadcrumb.Md.*` holds md's
literals, including those with no variable: the separator, slotted icons, the indicator and the focus shadow. Ionic leaves the
step colours unset in light, so its literals render, and these don't follow `Avonic.TextColorStep*`.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours). In short:

- **The trail:** a separator after every breadcrumb but the last, the last active unless one is, all worked out again when the
  children or the collapse props change, with Ionic's way of undoing an active breadcrumb it chose.
- **Collapsing:** past `MaxItems`, the middle ones hide behind an `ellipsis-horizontal` button, which raises `IonCollapsedClick`
  on a click, Enter or Space. An app usually lists them in a popover, which Avonic doesn't have yet.
- **A link** (`Href`) is a tab stop, clicks on Enter but not Space, turns `#03060b` under a mouse (the shade over a colour), and
  focused by keyboard shows a white rounded box with a shadow.
- **Colours** follow Ionic's order: its own colour, a coloured breadcrumbs' base, a coloured toolbar's contrast at 0.8, and the
  contrast when active there.
- **In a toolbar** the row has 8 px padding at the sides.

## Extensions

| Extension | Notes |
|---|---|
| `Click` on `AvonicBreadcrumb` | The DOM `click` an Ionic app listens to: on the release of every press, link or not, and Enter on a link. A click on the indicator clicks its breadcrumb too, after `IonCollapsedClick`, as the DOM event bubbles to the host. Until there's a router, this is how to follow `Href` |
| `Command`, `CommandParameter` | Run on `Click`, unless a handler sets `Handled`. One that can't execute disables the breadcrumb |

## Known gaps

- **`Href` doesn't navigate.** There's no router yet. Follow it from `Click`.
- **A link shows the arrow cursor**, where a browser shows the hand over an `<a>`.
- **A breadcrumb wider than the whole row** draws past its edge as in Ionic, but its box is held to the row's width by Avalonia's
  `WrapPanel`, so a tap past the edge misses it.
- **No popover** for `IonCollapsedClick` yet: `ion-popover` comes later.
- **Dark:** `Tokens/Palettes/Dark.axaml` doesn't set the `Avonic.Breadcrumb.Md.*` keys yet, so with it merged the breadcrumbs keep
  md's light colours, where Ionic's dark palette sets the step colours they read. The lines are proposed in the PR.
- **A restyled indicator with `ClickMode="Press"`** would click its breadcrumb twice: on the press, and as the host on the release.
  The theme's indicator clicks on release.
- Not tried in a real window, nor on a device.
