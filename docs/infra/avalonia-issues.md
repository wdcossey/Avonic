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
