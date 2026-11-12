# ion-refresher, ion-refresher-content

Status: done · [#35](https://github.com/wdcossey/Avonic/issues/35)

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
| `ion-refresher-content` `pullingIcon` | `PullingIcon`, a `SpinnerName?`, `Circular` when unset |
| `ion-refresher-content` `refreshingSpinner` | `RefreshingSpinner`, a `SpinnerName?`, `AvonicConfig.Spinner ?? Circular` when unset |

Not declared, as they belong to Ionic's legacy refresher, which isn't here: `pullMin`, `pullMax`, `pullFactor`, `closeDuration`,
`snapbackDuration`, `pullingText`, `refreshingText`. `mode` isn't exposed (ADR 0001).

## Checklist

The full checklist, ticked, is in [PLAN.md](PLAN.md#checklist-ion-refresher).

## Behaviours

Each is a test in `AvonicRefresherTests`, listed in [PLAN.md](PLAN.md#behaviours-the-test-list-avonicrefreshertests). In short: the
gesture on the content's scroll element (priority 31, `y`, 5 px) starts only at the top and not while refreshing; the first move after
it captures starts the pull, and each later one steps md's pulling animation to `deltaY / 360`; let go at 0.4 or less and it plays
back over progress × 500 ms and cancels; past it, the icon snaps back in 125 ms and the refresher refreshes, clearing the focus;
`Complete()` and `Cancel()` close it two frames later, after the icon's 200 ms transition, or Ionic's 700 ms fallback.

The refresher's state shows as pseudoclasses on both controls: `:pulling`, `:ready`, `:refreshing`, `:cancelling`, `:completing`,
`:native`, and `:active` on the refresher.

## Template parts

`AvonicRefresherContent`: `PART_Pulling`, `PART_PullingIcon`, `PART_SpinnerArrowContainer`, `PART_PullingSpinner` (turns, holding
`PART_PullingCircle`, a `RefresherCircle`, or `PART_PullingOtherSpinner` for other spinners), `PART_ArrowContainer`, `PART_Arrow`,
`PART_Refreshing`, `PART_RefreshingIcon`, `PART_RefreshingCircle` (circular) and `PART_RefreshingSpinner` (others).

## Resource keys

`refresher.md.css` has no CSS variables. The native icon's literals:

| Key | Default | CSS |
|---|---|---|
| `Avonic.Refresher.Md.NativeIconBorderBrush` | `#ECECEC` | `var(--ion-color-step-200, var(--ion-background-color-step-200, #ececec))` |
| `Avonic.Refresher.Md.NativeIconBackgroundBrush` | `#FFFFFF` | `var(--ion-color-step-250, var(--ion-background-color-step-250, #ffffff))` |
| `Avonic.Refresher.Md.NativeIconBoxShadow` | `0 1 9.4 0 #1A000000` | `0px 1px 6px rgba(0, 0, 0, 0.1)` |

The spinners and the arrow use `Avonic.Color.PrimaryBrush`.

## Extensions

None.

## Known gaps

- Ionic's legacy refresher (see above). With no spinner in either place, the refresher does nothing.
- An upward drag from the top hands the press to the scroller at the gesture's threshold, where Chromium scrolls beside Ionic's passive
  gesture; and the scroll element isn't made `overflow: hidden` during a pull. See [COMPARISON.md](COMPARISON.md#deliberate-differences).
- Ionic's dark palette sets the step colours the icon's border and background read; `Tokens/Palettes/Dark.axaml` doesn't follow
  them yet.
- Tried in a real window with touch, the wheel and the mouse by `tools/real-window`; not on a device.
