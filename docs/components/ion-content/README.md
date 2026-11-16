# ion-content

Status: done · [#11](https://github.com/wdcossey/Avonic/issues/11) · keys and the scroll indicator: [#67](https://github.com/wdcossey/Avonic/issues/67) · a scroll bar by config: [#175](https://github.com/wdcossey/Avonic/issues/175)

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
| [x] | `fullscreen` | `Fullscreen` | The background and scroll parts reach the page's edges, and the content's padded back. The page is the closest `AvonicTabs`, else the closest ancestor with class `ion-page`, or else the parent. The top counts from the closest `ion-page`, as `offsetTop` does from its offset parent: in a tab, a fullscreen content reaches behind a bottom tab bar, and with a top bar its top is 0, as measured in Chromium |
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
CSS does. Its own `HorizontalAlignment` and `VerticalAlignment` beat that, and a `Margin` is how to set `right: 10px`. Ionic takes any number
of `slot="fixed"` elements and a slot holds one child here, so a panel that overlaps its children (a plain `Panel`, a `Canvas`, or a
`Grid` with no rows or columns) stands for the slot: it fills the host, and each child places itself in it, as several `AvonicFab`s
do. Where it has no child, a press reaches the content under it, so give it no `Background`, which would take every press. A panel
that lays its children out, such as a `StackPanel` or a `Grid` with columns, is a `div` around them, so it's one block at the corner.
Which it is is read when it's slotted.

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
into view. **None of this has been tried on real touch hardware**, only with Avalonia's headless touch events. There's no bounce. See
[PLAN.md](PLAN.md#known-gaps). The keys and the scroll indicator are a browser's, recreated: see the extensions below.

## Behaviours

Everything is listed and measured in [PLAN.md](PLAN.md#behaviours).

## Extensions

- **`KeyboardOffset`** (`double`): what `--keyboard-offset` sets, as a property, so that `Padding` on the control doesn't lose it. The token sets it.

What a browser gives Ionic for free, measured in Chromium 141 in Android emulation ([#67](https://github.com/wdcossey/Avonic/issues/67)), with no
property: the numbers and the reasons are in [PLAN.md](PLAN.md#keyboard-scrolling-and-the-scroll-indicator-67).

- **Keyboard scrolling.**

  | Key | Scrolls |
  |---|---|
  | Up, Down | 40 |
  | Left, Right | 40, with `ScrollX` on. Physical: in RTL, Left goes towards the end |
  | Page Up, Page Down, Space, Shift+Space, Alt+Up, Alt+Down | a page, 87.5% of the viewport, rounded down: 262 of 300 |
  | Home, End, with or without Ctrl | to the top, the bottom |
  | Cmd+Up, Cmd+Down, on macOS | to the top, the bottom: Chromium's macOS bindings, not measured |

  Any other modifier does nothing, and nor does Avalonia's own `ScrollViewer`, whose Page Down went a whole viewport. Each scroll takes 150 ms of
  `cubic-bezier(0.42, 0, 0.58, 1)`, from the key, whatever the distance, and a key during one adds to where it's going and keeps its speed,
  as Chromium's does. Something else scrolling it stops it, and the next key starts from there. A key that can't scroll it, at its limit or on an axis that's off, goes on to
  the content around, so nested contents chain as Chromium's do. The scroll events report it as any other.

  **Which content.** The one with focus, or the closest around what has it. A content that can scroll **is a tab stop when nothing in it can
  take focus**, and Shift+Tab comes back to it; with something focusable inside, Tab goes into it. Content that fits, or with `ScrollY`
  off, isn't one. **A mouse press on the content, or a tap, focuses it** (Avalonia sends keys only to what has focus, where Chromium
  remembers the press and leaves focus on the page), so the keys work after it, and a focused field loses focus, as in a browser. A
  finger that scrolls moves no focus, so a field being typed in keeps its keyboard. A focused control that
  takes a key keeps it: a button its Space, a text field its arrows, Home, End and Space. Page Up and Page Down in a one-line `TextBox`
  scroll, and a multi-line one keeps them.

  **The focus ring** shows for focus by key only: Chromium's `outline: auto`, 2 px from a pixel outside to a pixel inside, under the content,
  so rows with a background of their own cover its inner half, as they do in Chromium. Its colour is `Avonic.Content.FocusRingBrush`.

- **A scroll indicator,** `ContentScrollIndicator`: two, `PART_VerticalIndicator` and `PART_HorizontalIndicator`, in the content's `ScrollViewer`
  template, over the content and under the fixed slot, taking no input. A 4 px thumb, flush with the end edge (the left in RTL) or the bottom,
  as long as the viewport's share of the extent, rounded down, and at least 4, in `rgba(128, 128, 128, 0.5)`. It shows from the load (its
  attach) until the first scroll by the user, as Chrome on a phone does, and then on each scroll by the user,
  the wheel, a finger or a fling, and not on a script, `ScrollToPoint` and the rest, or a key, all as measured. It stays 300 ms after the
  last scroll, then fades out over 300 ms. Each track leaves the corner when both axes can scroll. **It isn't Avalonia's `ScrollBar`,** and a
  content has no bar to drag, on desktop too, as md here is a phone's, unless an app turns one on: see the next item.

  | Key | md default |
  |---|---|
  | `Avonic.Content.ScrollIndicatorColor`, `…Brush` | `#80808080` |
  | `Avonic.Content.FocusRingColor`, `…Brush` | `#E59700`, Android's |

  `Avonic.Content.FocusRingMargin`, -1, places the ring a pixel outside. The thickness, 4, and the ring's 2 and radius of 3.5 are literals in the theme, as Chromium has no variable for them. A transparent brush
  hides either.

- **A scroll bar to drag, by config** ([#175](https://github.com/wdcossey/Avonic/issues/175)): **`ScrollBars`** (`bool`) on the content, and
  **`AvonicConfig.ContentScrollBars`** (`bool`), its default. Both off, so a content is Ionic's md, as a phone draws it. On, each axis that can
  scroll has a desktop browser's scroll bar in place of the indicator: Chromium's on Windows 11, measured with no emulation, 15 thick, at the end
  edge (the left in RTL) and along the bottom, **taking the width**, as Chromium's does: a content 390 wide has 375 for its content.

  ```xml
  <c:AvonicContent ScrollBars="True">…</c:AvonicContent>
  ```

  ```csharp
  AvonicConfig.ContentScrollBars = true; // at start-up: every content made after this has a bar
  ```

  The key is read when the content is made, as a prop's default from Ionic's config is, at style priority, so a value set on the content or in a
  style wins, and a change of the key reaches only contents made after it. With `ScrollY` off there's no bar, as `overflow: hidden` has none,
  and content that fits has none either.

  The thumb drags, and the content follows it in proportion. An arrow scrolls 40 and the track a page, 87.5% of the viewport along the bar,
  on the press, eased as a key is, as Chromium does; held, they repeat after 250 ms every 50 ms, Chromium's 800 px/s and 20 pages a second, and
  the track stops once the thumb reaches the pointer. The bar is `AvonicContentScrollBar`, a `ControlTheme` of its own, so a base theme's
  `ScrollBar` style doesn't reach it. Differences from Chromium's are in [PLAN.md](PLAN.md#deliberate-differences-2): a thumb dragged far to the
  side doesn't snap back, as Windows makes it, and held, it steps every 50 ms where Chromium scrolls at a steady speed.

  | Key | Default |
  |---|---|
  | `Avonic.Content.ScrollBarTrackColor`, `…Brush` | `#FCFCFC`, the track and the corner |
  | `Avonic.Content.ScrollBarThumbColor`, `…Brush` | `#8B8B8B`, the thumb and the arrows |
  | `Avonic.Content.ScrollBarThumbHoverColor`, `…Brush` | `#636363`, under the pointer, and the thumb while it's dragged |

  The sizes, 15 thick, 18 long buttons, a thumb drawn 9 thick and at least 17 long, are literals in the theme. Ionic's dark palettes set no
  `color-scheme`, so Chromium's bar stays light on a dark page, and these keys have no dark values.
