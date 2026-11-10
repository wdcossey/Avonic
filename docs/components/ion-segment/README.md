# ion-segment, ion-segment-button, ion-segment-view and ion-segment-content

Status: done · [#28](https://github.com/wdcossey/Avonic/issues/28)

`AvonicSegment` is a row of `AvonicSegmentButton`s, one of them checked: the one whose `Value` is the segment's. A tap, a drag of
the indicator, or the keys change it and raise `IonChange`, and the indicator slides over from the last one. Buttons with a
`ContentId` page an `AvonicSegmentView` of `AvonicSegmentContent`s, which a finger can page too, and the segment follows. The design
and the evidence are in [PLAN.md](PLAN.md), and the comparison with real Ionic is in [COMPARISON.md](COMPARISON.md).

```xml
<c:AvonicSegment Value="photos" IonChange="OnSegmentChange">
    <c:AvonicSegmentButton Value="photos" ContentId="photos">
        <c:AvonicLabel Content="Photos" />
    </c:AvonicSegmentButton>
    <c:AvonicSegmentButton Value="music" ContentId="music">
        <c:AvonicLabel Content="Music" />
        <c:AvonicIcon IconName="musical-notes" />
    </c:AvonicSegmentButton>
</c:AvonicSegment>
<c:AvonicSegmentView Height="200">
    <c:AvonicSegmentContent Name="photos">…</c:AvonicSegmentContent>
    <c:AvonicSegmentContent Name="music">…</c:AvonicSegmentContent>
</c:AvonicSegmentView>
```

## API

### ion-segment: `AvonicSegment`, an `ItemsControl`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `color` | `Color` | Its buttons' checked text, indicator and ripple |
| [x] | `disabled` | `IsEnabled` | Writes every button's `IsEnabled`, or its view's, as Ionic writes their `disabled` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [x] | `scrollable` | `Scrollable` | Buttons at their content's width, scrolling sideways, the checked one centred; no drag |
| [x] | `selectOnFocus` | `SelectOnFocus` | The arrows, Home and End check what they focus |
| [x] | `swipeGesture` | `SegmentSwipeGesture` (`true`) | The drag. Prefixed, as `InputElement.SwipeGesture` is Avalonia's event |
| [x] | `value` | `Value` (`object?`, two-way) | Matched to a button's by `Equals`, as `===`. Null is `undefined` |
| [x] | `ionChange` | `IonChange` (`SegmentChangeEventArgs.Value`) | A user's change only, after the release |
| [x] | `--background` | `Avonic.Segment.Background` | transparent |

Its buttons are its `Items`, as `core.json` lists no slot. Pseudoclasses: `:in-toolbar`, `:in-toolbar-color`, `:scrollable`,
`:segment-activated` (the indicator held).

### ion-segment-button: `AvonicSegmentButton`, an `ItemsControl`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `contentId` | `ContentId` | The `Name` of an `AvonicSegmentContent`. Such a button can't be disabled |
| [x] | `disabled` | `IsEnabled` | 0.3, no input, skipped by the keys and the drag |
| [x] | `layout` | `Layout` (`SegmentButtonLayout`, `IconTop`) | `IconTop`, `IconBottom`, `IconStart`, `IconEnd`, `IconHide`, `LabelHide` |
| [ ] | `mode` | — | Not exposed (ADR 0001) |
| [ ] | `type` | — | Not applicable: HTML form |
| [x] | `value` | `Value` (`object?`) | `ion-sb-N` until set |
| [x] | `native`, `indicator`, `indicator-background` parts | `PART_Native`, `PART_Indicator`, `PART_IndicatorBackground` | Also `PART_Overlay` (`::after`), `PART_Ripple` |

Its label and icon are its `Items`. Pseudoclasses: `:checked`, `:after-checked`, `:segment-button-activated`, `:in-segment`,
`:in-segment-color`, `:in-segment-scrollable`, `:in-toolbar`, `:in-toolbar-color`, `:has-label`, `:has-icon`, `:has-label-only`,
`:has-icon-only`, `:layout-icon-top` and the other five layouts. It's a tab item to automation, selected when checked.

Its 28 CSS variables are `Avonic.SegmentButton.<Name>` keys in `Tokens/Segment.axaml`, listed with their md values in
[PLAN.md](PLAN.md#ion-segment-button-css-variables-28); all are mapped but `--border-style`. `--indicator-transition` is a
`Transitions` whose first transition's `Duration`, `Delay` and `Easing` the segment runs on `AvonicConfig`'s clock.

### ion-segment-view: `AvonicSegmentView`, an `ItemsControl`

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `disabled` | `IsEnabled` | No swiping, 0.3 |
| [x] | `swipeGesture` | `SegmentViewSwipeGesture` (`true`) | `false`: no swiping or wheel; code still pages it |
| [x] | `ionSegmentViewScroll` | `IonSegmentViewScroll` (`SegmentViewScrollEventArgs`: `ScrollRatio`, `IsManualScroll`) | On every scroll |

### ion-segment-content: `AvonicSegmentContent`, a `ContentControl`

No props. A page the view's size, scrolling vertically with no bar. Its `Name` is what a button's `ContentId` names.

## Behaviours

Each has a test, in `AvonicSegmentTests`, `AvonicSegmentFamilyTests` and `AvonicSegmentViewTests`; the list with its sources is in
[PLAN.md](PLAN.md#behaviours):

- A tap checks the button under the release, at the checked button's height, with one `IonChange`. A press on the checked button
  holds the indicator, which a drag moves past each edge, never onto a disabled button.
- The indicator slides from the last button's place and width, 250 ms, `cubic-bezier(0.4, 0, 0.2, 1)`.
- The arrows, Home and End move the focus, wrapping, and Enter and Space check; with `SelectOnFocus` the moves check too.
- Not scrollable, columns start at each button's `MinWidth` (90) and grow equally to 360, centred. Scrollable, they're at least their
  content's width, sharing what's left, and the checked one is centred, smoothly when it changes: √distance / 60 s, eased in and out,
  as Chromium.
- With a view: the view jumps to the checked content at load and scrolls to it on every change, with no hit testing until 100 ms
  after; a person's scroll moves the checked button.

## Extensions

None.

## Known gaps

- **A vertical drag that starts on a segment doesn't scroll the page around it,** as the gesture captures at the press. See
  [COMPARISON.md](COMPARISON.md#deliberate-differences), with the other deliberate differences.
- **`--ion-toolbar-segment-*` aren't mapped,** nor **`--border-style`**.
- **Not tried in a real window or on a device.**
