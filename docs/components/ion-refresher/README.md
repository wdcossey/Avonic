# ion-refresher, ion-refresher-content

Status: done · [#35](https://github.com/wdcossey/Avonic/issues/35) · two of Ionic's bugs fixed: [#176](https://github.com/wdcossey/Avonic/issues/176)

`AvonicRefresher` and `AvonicRefresherContent`: Ionic's pull-to-refresh, as md's native refresher. A round icon follows the finger
down from the top of an `AvonicContent`; let go past 0.4 of the pull and it refreshes, with a spinning icon, until the app calls
`Complete()`. The plan is in [PLAN.md](PLAN.md), the comparison with Ionic in [COMPARISON.md](COMPARISON.md).

## Usage

The refresher goes in the content's fixed slot, as Ionic's needs `slot="fixed"`.

```xml
<c:AvonicContent>
    <c:AvonicContent.FixedContent>
        <c:AvonicRefresher IonRefresh="OnRefresh">
            <c:AvonicRefresherContent />
        </c:AvonicRefresher>
    </c:AvonicContent.FixedContent>
    <c:AvonicList>…</c:AvonicList>
</c:AvonicContent>
```

```csharp
private async void OnRefresh(object? sender, RefresherEventArgs e)
{
    await LoadAsync();
    e.Complete();
}
```

With an `AvonicHeader` just before the content in their panel, the icon comes out from under it; without, it scales in.

## API

| Ionic | Avonic |
|---|---|
| `disabled` | `IsEnabled` (inverted) |
| `ionPull`, `ionPullStart`, `ionStart` | `IonPull`, `IonPullStart`, `IonStart` (`RoutedEventArgs`) |
| `ionPullEnd` | `IonPullEnd` (`RefresherPullEndEventArgs.Reason`: `Complete` or `Cancel`) |
| `ionRefresh` | `IonRefresh` (`RefresherEventArgs.Complete()`) |
| `complete()`, `cancel()`, `getProgress()` | `Complete()`, `Cancel()`, `GetProgress()`, synchronous |
| default slot | `Content` |
| `ion-refresher-content` `pullingIcon` | `PullingIcon`, a `SpinnerName?`; unset, `AvonicConfig.RefreshingIcon`, else `Circular` |
| `ion-refresher-content` `refreshingSpinner` | `RefreshingSpinner`, a `SpinnerName?`; unset, `AvonicConfig.RefreshingSpinner`, else `AvonicConfig.Spinner`, else `Circular` |
| config `refreshingIcon`, `refreshingSpinner` | `AvonicConfig.RefreshingIcon`, `AvonicConfig.RefreshingSpinner` |

Not declared, as they belong to Ionic's legacy refresher, which isn't here: `pullMin`, `pullMax`, `pullFactor`, `closeDuration`,
`snapbackDuration`, `pullingText`, `refreshingText`. `mode` isn't exposed (ADR 0001).

## Config

Ionic's `refresher-content.js` reads three config keys in `componentWillLoad`, for a prop that's unset: `pullingIcon` is
`config.get('refreshingIcon', 'circular')` in md (not `spinner`, which only iOS reads there), and `refreshingSpinner` is
`config.get('refreshingSpinner', config.get('spinner', 'circular'))`. Here they're read once, when the content first loads: its
first attach, or its refresher's first look at it, if that's sooner. A change of the config reaches only contents that load after it.
An app's own value wins, and clearing it later gives null, none, as Ionic's `undefined` does after its load.

The keys are `ConfigSpinner?`: null is not set, so the next fallback applies, and `ConfigSpinner.None` is Ionic's `null`, which
`config.get` returns as it is. A pulling icon or a refreshing spinner of none makes it not md's native refresher, so it does nothing
here (the legacy refresher isn't here).

```csharp
AvonicConfig.RefreshingSpinner = SpinnerName.Dots;   // every refresher content's refreshing spinner, unless it has its own
AvonicConfig.Spinner = ConfigSpinner.None;           // with RefreshingSpinner not set: none, so not the native refresher
```

## Scroll host

As Ionic's `connectedCallback`, a control in the content with the class `ion-content-scroll-host` (`AvonicContent.ScrollHostClass`),
the first in the content's `Content` and then its `FixedContent`, is the scroll element, before the content's own scroller: the pull
starts from it, and only when it's at its top. A host that is a `ScrollViewer` is the scroll element itself; otherwise the first
`ScrollViewer` in it, depth first, such as a `ListBox`'s template has, as only a `ScrollViewer` scrolls here: so put the class on the
control that scrolls, as Ionic needs it on the element that scrolls. It's looked for when the refresher is attached; a host whose
scroller isn't there yet (a `ListBox` not yet styled, with the fixed slot placed before the scroller, or a hidden one, which isn't
templated) is looked at again after each layout of the content until it has one, as Ionic waits for the content to be ready. If the
first layout gives it none, the error is logged, once, and nothing pulls until it has one.

## Checklist

The full checklist, ticked, is in [PLAN.md](PLAN.md#checklist-ion-refresher).

## Behaviours

Each is a test in `AvonicRefresherTests`, listed in [PLAN.md](PLAN.md#behaviours-the-test-list-avonicrefreshertests). In short: the
gesture on the content's scroll element, or a scroll host's in it (priority 31, `y`, 5 px), starts only at the top and not while refreshing; the first move after
it captures starts the pull, and each later one steps md's pulling animation to `deltaY / 360`; let go at 0.4 or less and it plays
back over progress × 500 ms and cancels; past it, the icon snaps back in 125 ms and the refresher refreshes, clearing the focus;
`Complete()` and `Cancel()` close it two frames later, after the icon's 200 ms transition, or Ionic's 700 ms fallback. Cancelling
during a refresh shows both icons until then, as Ionic's does, the pulling one as wide as the content ([#167](COMPARISON.md#after-a-cancel-167)).

The refresher's state shows as pseudoclasses on both controls: `:pulling`, `:ready`, `:refreshing`, `:cancelling`, `:completing`,
`:native`, and `:active` on the refresher.

## Template parts

`AvonicRefresherContent`: `PART_Pulling`, `PART_PullingIcon`, `PART_SpinnerArrowContainer`, `PART_PullingSpinner` (turns, holding
`PART_PullingCircle`, a `RefresherCircle`, or `PART_PullingOtherSpinner` for other spinners), `PART_ArrowContainer`, `PART_Arrow`,
`PART_Refreshing`, `PART_RefreshingIcon` and `PART_RefreshingSpinner`, an `AvonicSpinner`, which for `circular` in the native
refresher starts 655 ms in (its internal `AnimationDelay`, Ionic's `animation-delay: -655ms`).

## Resource keys

`refresher.md.css` has no CSS variables. The native icon's literals:

| Key | Default | CSS |
|---|---|---|
| `Avonic.Refresher.Md.NativeIconBorderBrush` | `#ECECEC` | `var(--ion-color-step-200, var(--ion-background-color-step-200, #ececec))` |
| `Avonic.Refresher.Md.NativeIconBackgroundBrush` | `#FFFFFF` | `var(--ion-color-step-250, var(--ion-background-color-step-250, #ffffff))` |
| `Avonic.Refresher.Md.NativeIconBoxShadow` | `0px 1px 6px rgba(0, 0, 0, 0.1)`, through `{b:CssShadow}` | the same |

Ionic's dark palette sets the step colours they read, so `Tokens/Palettes/Dark.axaml` gives the two brushes
`Avonic.BackgroundColorStep200` and `250`: `#414141` and `#4D4D4D`, as measured with `dark.always.css`.

The spinners and the arrow use `Avonic.Color.PrimaryBrush`.

## Extensions

None.

## Known gaps

- Ionic's legacy refresher (see above). With no spinner in either place, the refresher does nothing.
- An upward drag from the top hands the press to the scroller at the gesture's threshold (`GestureConfig.ReleaseAgainstDirection`),
  where Chromium scrolls beside Ionic's passive gesture; and the scroll element isn't made `overflow: hidden` during a pull. See
  [COMPARISON.md](COMPARISON.md#deliberate-differences).
- A scroll host in content that is data, shown through the content's `ContentTemplate`, isn't found: the search reads the controls
  an app puts in the slots and their logical descendants. Of two hosts, one in `Content` and one in `FixedContent`, the one in
  `Content` wins, where Ionic takes the first in the markup.
- `AvonicConfig.RefreshingIcon` takes only spinners, as `PullingIcon` does: Ionic's can name any icon, for its legacy refresher.
- In cancelling, the pulling icon is a pill where Chromium draws an ellipse ([#167](COMPARISON.md#deliberate-differences)).
- **Two of Ionic's own bugs are fixed,** as deliberate differences ([#176](COMPARISON.md#deliberate-differences)), measured in
  Chromium with Ionic 8.8.1:
  - A pull that a `Cancel()`'s end overtakes, let go with no move past 0.4 since, leaves Ionic's refresher off for good: its reset
    destroys the pull's animation, whose end then never comes, nor `gesture.enable(true)` with it (F, G: nothing pulls again). Here
    the gesture comes back on as `IsEnabled` says, as it does after any pull (R3), and the next pull works. The reset has already
    made it inactive and raised `IonPullEnd`, so nothing more is raised. One moved on past 0.4 still refreshes, as Ionic's does.
  - A refresh begun during cancelling is ended by the cancel's reset, as cancelled, while the app still loads (H: `ionRefresh` 416 ms
    after `cancel()`, `ionPullEnd` `cancel` at 733); let go past 0.4 just before the reset, while its 125 ms snap back runs, the
    reset destroys it, and no refresh comes and the gesture stays off. Here a reset does nothing once a pull started after
    `Cancel()` was called has been let go past the threshold: the refresh goes on until the app calls `Complete()`, which ends it
    with `IonPullEnd` `Complete`, and no `Cancel` comes for the refresh the cancel ended, as the refresher never went back to
    inactive in between. A pull held at the call, or let go before it, is the cancel's to end, as in Ionic.
- Tried in a real window with touch, the wheel and the mouse by `tools/real-window`; not on a device. #176's four checks there
  haven't been run yet.
