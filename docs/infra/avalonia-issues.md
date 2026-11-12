# Avalonia issues

Things in Avalonia 12.1.4 that looked wrong while building Avonic, kept here for submitting later. **None has been reported.** Add
to this as they're found, with the evidence, and say how sure it is. Check each against the current Avalonia before submitting.

## `BoxShadow`'s offset doesn't scale with the render scale

**Measured, and the cause not read.** With a shadow of each kind on its own, drawn headlessly with Skia at each scale, measuring how
far it reaches below a box:

| Shadow | At 1× | At 2× | At 3× |
|---|---|---|---|
| Offset of 10, `0 10 0 0` | 10 device pixels | 10 | 10 |
| Spread of 10, `0 0 0 10` | 10 | 20 | 30 |
| Blur of 20, `0 0 20 0` | 12 | 24 | 35 |

So the spread and blur scale, and the offset doesn't. On `ion-card`, straight below a card, 1× gives 164 against Chromium's 170, 2×
gives 205 against 167, and 3× gives 212 against 166. It only shows on a high-density screen, which is every phone. Avonic accepts it
as best effort, and holds CSS's numbers. It hasn't been tried on a real device or a non-headless renderer.

## `c|Host ^` in a `ControlTheme` ignores `c|Host`

**Measured, and whether it's meant to is unknown.** This compiles, and applies to every `Probe`, inside an `AvonicItem` or not:

```xml
<ControlTheme x:Key="{x:Type c:Probe}" TargetType="c:Probe">
    <Style Selector="c|AvonicItem:ion-color ^">
        <Setter Property="Opacity" Value="0.5" />
    </Style>
</ControlTheme>
```

If it can't be supported, a compile error would be better than a rule that silently applies everywhere. If it could, a control could
hold the rules about its own hosts, and only that control would pay for them.

## Every global style rule is checked against every element

**From the source and a trace, and it may be deliberate.** `StyledElement.ApplyStyles` walks every `Style` in each host's `Styles`, and
`ApplyStyle` then walks its `Children` whether or not the parent matched. `Style.TryAttach` runs the selector each time, and a
result of `NeverThisType` isn't kept.

A trace agrees: with 98 rules, each element got 98 checks. For Avonic it's about 30 ns per alternative between commas, so a bare
`Border` goes from 2.2 µs to 8.0 µs to attach with 174 alternatives. The numbers are in [performance](performance/README.md). A cache of
which rules can ever match a type would remove most of that. An idea, not a bug.

## `StreamGeometry.Parse`

**Measured.** It's what `SvgPath` works around, and the [`ion-icon` plan](../components/ion-icon/PLAN.md) has the detail:

| Problem | Compared with SVG |
|---|---|
| After a non-curve command, a smooth curve (`S`, `T`) starts from the last control point, however old | SVG starts it at the current point. Icons draw wrongly |
| The default fill rule is even-odd | SVG's is nonzero |
| **Anything but a command straight after `Z` loops forever,** including Unicode spaces and digits | A hang, on data that might not be trusted |
| Bad data throws `InvalidDataException`, `IndexOutOfRangeException` or `FormatException` | |
| A path that doesn't start with a moveto draws from the origin | SVG draws nothing |
| Valid numbers such as `+1` and some exponents are rejected | |

## A wrapped `TextBlock` in RTL loses a pixel at its right edge

**Seen in a frame, and the cause not read.** Latin text that wraps in a `TextBlock` with `FlowDirection="RightToLeft"` has about 1 px of its
last letter cut off on each full line, headlessly with Skia at 2×. It's a plain `Border` and `TextBlock`, with no Avonic control, at 155 wide
and 11 px, where the layout reports a width of 154.25 and the box is 155. `ClipToBounds` being on by default is what shows it. Avonic accepts
it as best effort. It hasn't been tried in a real window.

## `ScrollGestureRecognizer` has no axis lock

**Read, not tested.** The recogniser starts a scroll at 5 px on its own axis, with no lock to the drag's main axis, so a touch drag
at 30° from horizontal that has moved 6 px down scrolls a vertical `ScrollViewer`. Chromium locks a touch scroll to its main axis, and
scrolls a vertical scroller only for a drag nearer vertical than horizontal. Avonic's gesture utility hides it in most cases, by
deciding first; it shows for a drag between 40° and 45° near a sideways gesture, which scrolls here and doesn't in Chromium. From the
design of the gesture utility (#4, PR #83), in `docs/infra/gesture/PLAN.md`. An axis lock on the recogniser would match browsers.

## `TextPresenter` brings its caret into view when it isn't focused

**Measured.** Setting a `TextBox`'s `CaretIndex` posts `BringIntoView` on the caret from `TextPresenter.CaretChanged`, whether the box
is focused or not, so a `ScrollViewer` around it scrolls to the box. Headlessly, over `SimpleTheme`, a `TextBox` below the fold with
`Text` alone left the page at 0; with `CaretIndex` set as well, it scrolled the page 118 px to show it. Chromium never scrolls to an
input that isn't focused when its value or selection changes. Found as the sample's `ion-input` page opening scrolled: `AvonicInput`
moves the caret to the end of an assigned value, as Chromium does, and now handles `RequestBringIntoView` from its text box while
the box isn't focused. Bringing the caret into view only while focused would match browsers.

## `Opacity` isn't group opacity

**Measured.** A control's `Opacity` fades each of its parts on its own, so where parts overlap, the one underneath shows through: a
white child over a grey sibling at 0.38 read 233, where CSS's `opacity`, which fades the group as one picture, gives 255.
`AvonicToggle`'s disabled look fades with an `OpacityMask` of a solid brush at that alpha instead, which is a group. Found by
`ion-toggle` (#21, PR #116). Other controls with overlapping parts may need the same: `AvonicTextarea`, `AvonicSearchbar` and
`AvonicInput` now do (#134, #138, #141).

## `AutomationProperties.IsRequiredForForm` has nothing that reads it

**Read, not tested on a screen reader.** The attached property exists in 12.1.4, but no peer reports it and no platform's
automation reads it, so `aria-required` can't be said. `AvonicCheckbox` and `AvonicInput` set it at style priority anyway, and
`AvonicTextarea` as a local value on its native input; `AvonicToggle` leaves `required` out. Found by `ion-checkbox` and `ion-toggle`
(#20, #21); the input and the textarea in #134 and #141.

## No public way to find the current activity's `TopLevel` under `MainViewFactory`

**Read from 12.1.4.** `AndroidActivatableLifetime.CurrentMainActivity` is internal, Android's lifetime isn't an
`ISingleTopLevelApplicationLifetime`, and `KeyboardDevice.Instance` is internal. A library that shows an overlay with no target has
to track top levels itself: `Overlays/OpenTopLevels` keeps the open ones and takes the last still open. Sure of the API; whether a
public way is planned is unknown. Found by #114 (PR #119).

## `TextBox.MaxLength = 0` means no limit

**Measured.** HTML's `maxlength="0"` allows nothing; Avalonia's `0` is no limit. `AvonicInput` sets `0` for no limit and keeps every
character out itself when Ionic's `maxlength` is 0. Found by `ion-input` (#24, PR #120). Also from that PR: headless touch raises no
`PointerEntered`, so a touch's hover can't be tested headlessly.

## A `Border` draws no `BoxShadow` unless its border is even and its `BackgroundSizing` is the default

**Measured for each of three cases, and the cause not read.** The column under a 56 px round fab button read 255 with
`BackgroundSizing="InnerBorderEdge"`; with the default sizing, 177, 187 and 198 from the shadow. `AvonicFabButton` leaves the sizing
alone, where CSS has `background-clip: padding-box`. Found by `ion-fab` (#30, PR #123). `ion-reorder` (#34, PR #136) found the other
two: its first lifted item put the shadow on `AvonicItem`'s `PART_Native`, which has `OuterBorderEdge` (CSS's `border-box`), and drew
it only once the sizing was set back to `CenterBorder`; and with the sizing set back, an item with full lines, whose border is
`0,0,0,1`, still had none. The reorder group now draws the shadow on a `Border` of its own over the item, `PART_Lift`, and
`B14_The_Shadow_Shows_Whatever_The_Items_Lines` holds it for every kind of line. So a shadow can't sit on a part that also needs
CSS's background clip or a bottom line. To reproduce: a 100 × 40 `Border` with `Background="White"` and `BoxShadow="0 0 10 0 Black"`
over white, rendered headlessly; then the same with `BorderThickness="0,0,0,1"`, and with each `BackgroundSizing`. Only the even
border with `CenterBorder` darkens the pixels around it.

## `Visual.IsEffectivelyVisibleChanged` is internal

**Checked by reflection.** A control that has to stop work when it or an ancestor is hidden can't subscribe to its own effective
visibility. `AvonicSpinner` watches `IsVisibleProperty.Changed` for every visual in the app; `AvonicProgressBar` finds out on its next
frame, then watches the hidden visuals in its own chain. Found by `ion-spinner` (#15, PR #82). Making the event public would let a
control subscribe for itself.

## `SplineEasing` is about 2e-3 out of CSS's `cubic-bezier`

**Measured, and the cause not read.** The same curve, `cubic-bezier(0.42, 0, 0.58, 1)`, gives a spinner's dash of 5.0477 at 100 ms
through `SplineEasing`, where Chromium computes 5.04952 ([#92](https://github.com/wdcossey/Avonic/issues/92)). Avonic's
`CubicBezierEasing` solves it as Chromium's `UnitBezier` does, Newton's method then bisection to 1e-7, and the tests hold it within 1e-6 of
`Element.animate` in Chromium. `SplineEasing` also isn't specified for a control point's y outside 0..1, which CSS allows
(`ion-progress-bar`'s keyframes have 1.38179). Small, but it shows in a pixel comparison of a curve's middle.

## `BoxShadow`'s blur isn't CSS's

**Measured on two shadows, and the cause not read.** CSS's `box-shadow` blur `B` is a Gaussian of sigma `B / 2`; Avalonia's, drawn by
Skia, matches a Gaussian of sigma `0.288675 B + 0.5`, so the same number falls off in about two thirds of the distance. `ion-reorder`
(#34, PR #136) measured `0 0 10px rgba(0, 0, 0, 0.4)` in Chromium beside Avonic: a blur of 10 fell off in 7 px instead of 10, and
15.6, which is sigma 5 by that formula, matched within 1 of 255 at every pixel of the fall-off. `Avonic.ReorderGroup.SelectedBoxShadow`
holds 15.6, and `ion-refresher` (#35, PR #142) took the same ratio, 1.56, for `Avonic.Refresher.Md.NativeIconBoxShadow` (6 → 9.4,
within 3 levels of Chromium below the icon; the formula gives 8.66). Every other `BoxShadows` token still holds CSS's number, so each
md elevation draws tighter than Ionic's
([#139](https://github.com/wdcossey/Avonic/issues/139) measures more blurs and converts them). The offset not scaling (above) is
another difference. To reproduce: a white 100 × 40 box with `box-shadow: 0 0 10px rgba(0,0,0,.4)` in Chromium, and a `Border` with
`BoxShadow="0 0 10 0 #66000000"` rendered headlessly at 1×; read the column below each box from its edge until it's 255.

## No public way to keep a `VirtualizingStackPanel`'s element realised

**Read from 12.1.4.** `CacheLength` realises a buffer of rows on each side of the viewport, but nothing public keeps a given element
realised: the panel does that only for a focused element or one being scrolled to, internally, and a subclass can't reach its
realising. CSS's `position: sticky` keeps a divider stuck to the end of its section, but the
panel recycles the divider's row once its place is above the top. `AvonicList` draws a second container for the same item instead,
made by its own `PrepareContainerForItemOverride` (`AvonicList.StuckDivider.cs`), which is another instance: state an app set on the
divider itself doesn't carry over, and an item that is itself a control can't be drawn twice, so it gets none. Found by #68 (PR #146). A public way to pin an element, as a header, would remove the copy. To
reproduce: an `ItemsControl` with a `VirtualizingStackPanel` in a `ScrollViewer`, 1,000 rows of 48, scrolled to 2,000: row 0's
container is gone, and nothing public brings it back without scrolling to it or raising `CacheLength` past it.

## An implicit `ControlTheme` merged at runtime doesn't restyle controls already shown

**Measured in #145's review, and not held by a test.** `Css/Typography.axaml`'s `ControlTheme` for `TextBlock` (`x:Key="{x:Type
TextBlock}"`), merged into an app's `Resources` after a `TextBlock` with `Classes="h1"` is shown, doesn't reach it; one shown after the
merge has it. `DynamicResource`s in the same dictionary do follow at once: a label's heading line height
(`Avonic.Label.HeadingLineHeight`) changes. Removing the dictionary leaves the theme on what has it. Avalonia picks a control's implicit
theme when it's attached, it seems. The typography README says to merge the file at startup, or in a view's `Resources` before it's
shown. Found by #70 (PR #145). To reproduce: show a window with a `TextBlock`, then add a `ResourceDictionary` holding a
`ControlTheme` for `TextBlock` that sets `FontSize` to the window's `Resources.MergedDictionaries`: the shown `TextBlock` keeps its
size, and a `TextBlock` added after it takes the new one.
