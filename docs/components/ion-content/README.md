# ion-content

Status: done · [#11](https://github.com/wdcossey/Avonic/issues/11)

`AvonicContent` is the scrolling area of a page: a background, content that scrolls, and a slot for content that doesn't. It reports
scrolling on Ionic's timeline, and can extend behind what's around it. The design and evidence are in [PLAN.md](PLAN.md), and the
comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

**It fills what it's given, and never sizes to its content.** With no height to fill, in a `StackPanel` or an `Auto` row, it's zero high,
as in Ionic. Put it in a `*` row, or give it a height.

```xml
<Grid RowDefinitions="Auto,*,Auto">
    <!-- A ZIndex above 0 keeps these on top when the content is Fullscreen. -->
    <Border ZIndex="10" Height="56" />

    <c:AvonicContent Grid.Row="1" Fullscreen="True" ScrollEvents="True" IonScroll="OnScroll">
        <StackPanel>
            <c:AvonicItem Content="A row" />
            <c:AvonicItem Content="Another row" />
        </StackPanel>

        <c:AvonicContent.FixedContent>
            <Button Content="Stays put" Margin="0,0,16,16"
                    HorizontalAlignment="Right" VerticalAlignment="Bottom" />
        </c:AvonicContent.FixedContent>
    </c:AvonicContent>

    <Border Grid.Row="2" ZIndex="10" Height="40" />
</Grid>
```

```csharp
await content.ScrollToBottom(500);        // finishes when the scroll does
content.ScrollToPoint(null, 100);         // no duration, so it's already there
var viewer = content.GetScrollElement();  // the ScrollViewer
```

## API

### Properties

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | Paints the scroll part. See the [colour system](../../infra/color-system/README.md) |
| [x] | `fixedSlotPlacement` | `FixedSlotPlacement` | `After` is on top and last in the tab order. `Before` is first in the tab order, **but under the scroll part, which takes the pointer**, as in Ionic |
| [x] | `forceOverscroll` | `ForceOverscroll` | Null is off in md. On, content that fits can scroll by a pixel. Avalonia has no bounce to go with it |
| [x] | `fullscreen` | `Fullscreen` | The background and scroll parts reach the page's edges, and the content's padded back. The page is the closest ancestor with class `ion-page`, or else the parent |
| [x] | `scrollEvents` | `ScrollEvents` | Off by default, as in Ionic |
| [x] | `scrollX` | `ScrollX` | On, text doesn't wrap at the viewport unless it's given a width |
| [x] | `scrollY` | `ScrollY` | Off, the user can't scroll it and a script can, and it keeps its position, as `overflow: hidden` does. The exception is with `ScrollX` on: see [PLAN.md](PLAN.md#deliberate-differences) |

`disabled` isn't a prop of `ion-content`. `mode` isn't exposed (ADR 0001).

### Events

All bubble.

| ✓ | Ionic | Avonic | Args |
|---|---|---|---|
| [x] | `ionScrollStart` | `IonScrollStart` | `ScrollBaseDetailEventArgs`: `IsScrolling` |
| [x] | `ionScroll` | `IonScroll` | `ScrollDetailEventArgs`: `Type`, `ScrollTop`, `ScrollLeft`, `StartX`, `StartY`, `StartTime`, `CurrentX`, `CurrentY`, `VelocityX`, `VelocityY`, `DeltaX`, `DeltaY`, `CurrentTime`, `IsScrolling` |
| [x] | `ionScrollEnd` | `IonScrollEnd` | `ScrollBaseDetailEventArgs`: `IsScrolling` |

Times are in milliseconds and velocities in pixels per millisecond, as in Ionic. `IonScrollStart` comes at once, `IonScroll` a frame later,
and `IonScrollEnd` when a 100 ms watchdog finds 120 ms of quiet. `event` and `data` aren't there: there's no DOM event, and Ionic never sets `data`.

### Methods

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | `getScrollElement()` | `ScrollViewer? GetScrollElement()`, synchronous. Null before it's in a tree, so not in a constructor |
| [x] | `scrollToPoint(x, y, duration?)` | `Task ScrollToPoint(double? x, double? y, double duration = 0)` |
| [x] | `scrollByPoint(x, y, duration)` | `Task ScrollByPoint(double x, double y, double duration)` |
| [x] | `scrollToTop(duration?)` | `Task ScrollToTop(double duration = 0)` |
| [x] | `scrollToBottom(duration?)` | `Task ScrollToBottom(double duration = 0)` |

Durations are in milliseconds. Under 32, it jumps and the task has already finished. `AvonicConfig.Animated` doesn't change this, as Ionic's scroll ignores `animated`. Otherwise it waits a frame
and eases out, and the task finishes with it. A scroll asked for before the first layout waits for it. Nothing cancels an animation, as in
Ionic: not a second call, and not the user's finger.

### Slots and parts

| ✓ | Ionic | Avonic |
|---|---|---|
| [x] | default slot | `Content`, `ContentTemplate` |
| [x] | `fixed` | `FixedContent`, `FixedContentTemplate` |
| [x] | `background` | `PART_Background` |
| [x] | `scroll` | `PART_Scroll` |

A child of the default slot is as high as its content, and a child of the fixed slot is as big as its content, at the host's corner, because that's what
CSS does. Its own `HorizontalAlignment` and `VerticalAlignment` beat that, and a `Margin` is how to set `right: 10px`.

### Resource keys

| ✓ | Ionic | Key | md default |
|---|---|---|---|
| [x] | `--background` | `Avonic.Content.Background` | Unset, so `Avonic.BackgroundColorBrush` |
| [x] | `--color` | `Avonic.Content.Color` | Unset, so `Avonic.TextColorBrush`. Not read by the fixed slot |
| [x] | `--padding-start`, `-end`, `-top`, `-bottom` | `Avonic.Content.PaddingStart`, `…End`, `…Top`, `…Bottom` | 0 |
| [x] | `--keyboard-offset` | `Avonic.Content.KeyboardOffset` | 0. Adds to the bottom padding, through the `KeyboardOffset` extension |
| [ ] | `--offset-top`, `--offset-bottom` | — | Not applicable: Ionic overwrites them inline, so a value does nothing. `Fullscreen` sets them |
| [x] | `--ion-safe-area-top`, `-bottom`, `-left`, `-right` | `Avonic.SafeAreaTop`, `…Bottom`, `…Left`, `…Right` | 0, because Avalonia pads its root view by the safe area itself. Not read by this control, as `content.css` doesn't |

As with any token, a value in a control's `Resources` applies to that one control, which is what an inline `style="--padding-top: 10px"` does.
`Padding` on the control beats the padding tokens, and does what `class="ion-padding"` does in Ionic.

## Filling the safe area

The `Avonic.SafeArea*` keys default to 0, because Avalonia pads the root view itself through `TopLevel.AutoSafeAreaPadding`, and adding the inset
again would double it. So only an app that turns that off and draws edge to edge needs them, and it fills them in itself.

**No control reads them yet.** `ion-content` doesn't, as Ionic's doesn't, and `AvonicItem` will in [#72](https://github.com/wdcossey/Avonic/issues/72). Until then this
fills them for your own styles, and leaves your content under the system bars. Run it from your main view, as Avalonia reads the switch from the main view or the
`TopLevel`, once it's attached, as there's no `TopLevel` before that:

```csharp
if (TopLevel.GetTopLevel(this) is { InsetsManager: { } insets })
{
    TopLevel.SetAutoSafeAreaPadding(this, false);
    insets.DisplayEdgeToEdgePreference = true;

    void Apply()
    {
        var padding = insets.SafeAreaPadding;
        var resources = Application.Current!.Resources;
        resources["Avonic.SafeAreaTop"] = padding.Top;
        resources["Avonic.SafeAreaBottom"] = padding.Bottom;
        resources["Avonic.SafeAreaLeft"] = padding.Left;
        resources["Avonic.SafeAreaRight"] = padding.Right;
    }

    insets.SafeAreaChanged += (_, _) => Apply();
    Apply();
}
```

**This hasn't been tried on a device,** so treat it as a starting point until [#66](https://github.com/wdcossey/Avonic/issues/66) has.

## What's inherited from Avalonia

The drag and momentum are Avalonia's `ScrollGestureRecognizer`, not a browser's, with its own feel. Its `ScrollViewer` also scrolls a focused child
into view. **None of this has been tried on real touch hardware**, only with Avalonia's headless touch events. There's no bounce, and no scroll indicator
while scrolling. See [PLAN.md](PLAN.md#known-gaps).

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours).

## Extensions

- **`KeyboardOffset`** (`double`): what `--keyboard-offset` sets, as a property, so that `Padding` on the control doesn't lose it. The token sets it.
