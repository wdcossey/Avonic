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
as best effort, and passes CSS's offsets through unchanged. It hasn't been tried on a real device or a non-headless renderer.

#139 confirms it, at 2× on its own scenes, and finds it's the only difference left there: with the blur converted, an offset of 10
and no blur is still 102 levels out at 2× (the whole 102 of black at 0.4, on the rows it misses), and doubling the offsets by hand
puts md's 2 dp, 8 dp, fab, pressed fab and header shadows within 5 of Chromium away from the box's edge
([box-shadow](box-shadow/README.md)).

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
`AvonicInput` now do (#134, #138, #141). `AvonicNav`'s pages fade in a transition as a group too: an animation's `opacity` on an element with
`AnimationGroupOpacity` set is an `OpacityMask` (#130).

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

**Measured on 55 shadows at 1× and 2×, headless Skia only, and the cause inferred from the fit, not read.** CSS's `box-shadow` blur
`B` is a Gaussian of sigma `B / 2`. Avalonia hands Skia a blur `A` as sigma `A / (2√3) + 0.5`, and Skia's CPU rasteriser draws that
as three box blurs `floor(sigma · 3√(2π)/4 + 0.5)` pixels wide, which is about 0.94 of the sigma asked for. So the same number falls
off in about two thirds of CSS's distance: a blur of 10 in 7 px instead of 12, at worst 12 levels out, and 4 to 22 for blurs of 4 px
and up. The best Avalonia blur for each CSS blur moves in steps of about 1.84, one box width, and `2√3 ((B + 0.05) / (3√(2π)/4) − 0.5)` lands
in the best one from 1.5 to 25 px: within 1 to 3 of Chromium from 4 px up, 3 and 7 at 2.5 and 1.5. Avalonia draws no blur at all below 1.03, so a CSS blur of 1 px can't
be matched at 1× (17 levels out at alpha 0.4, either way). Avonic converts every token through `{b:CssShadow}`
([#139](https://github.com/wdcossey/Avonic/issues/139), [docs](box-shadow/README.md)). A blur meaning CSS's, or documented as
Skia's radius, would remove the need. A GPU renderer, which draws a true Gaussian, wasn't measured. To reproduce: a white 100 × 40
box with `box-shadow: 0 0 10px rgba(0,0,0,.4)` in Chromium, and a `Border` with `BoxShadow="0 0 10 0 #66000000"` rendered headlessly
at 1×; read the column below each box from its edge until it's 255: Chromium 208, 215, 222, 229, 235, 240, 244, 248, 250, 252, 254,
254, 255; Avalonia 210, 222, 232, 241, 247, 251, 253, 255.

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

## A pseudoclass before a `:not()` that holds a combinator is dropped

**Measured in #131's roll-up (R4b), and the cause not read.** In `Styles/List.axaml`, the inset list's rule for its last item was
written `c|AvonicList:inset c|AvonicItem:nth-last-child(1):not(c|AvonicItemSliding > c|AvonicItem) /template/ Border#PART_Native`,
and it matched every item in the list, as if `:nth-last-child(1)` weren't there (`AvonicListTests.L12_An_Inset_Lists_Last_Item_Has_No_Border`
failed on the first and middle items). Written the other way round,
`c|AvonicItem:not(c|AvonicItemSliding > c|AvonicItem):nth-last-child(1)`, it works, and the L12 tests hold it. A `:not()` with no
combinator inside, after a pseudoclass, works as expected. To reproduce: swap the two in that rule, and the L12 tests fail. A
reproduction outside Avonic hasn't been tried.

## No public way to stop the headless render timer ticking on its own

**Read from 12.1.4, and held by tests.** Avalonia.Headless drives its render loop from `HeadlessRenderTimer`, a real `DispatcherTimer`
at the platform's frame rate, and `MediaContext` has a 16 ms `_animationsTimer` of its own, so any `Dispatcher.UIThread.RunJobs()` can
compose a frame no test asked for, more often on a slower machine. `AvaloniaHeadlessPlatformOptions` has no option to tick only on
`AvaloniaHeadlessPlatform.ForceRenderTimerTick()`, and `IRenderTimer` is `[PrivateApi]`. The tests' `ManualRenderTimer` swaps the
timer by reflection, into `DefaultRenderLoop._timer`, `HeadlessRenderTimer._forceTick`, `MediaContext.Instance`,
`MediaContext._animationsTimer` and `MediaContext.ScheduleRender(bool)`, and throws naming the member if one has moved. **Check it on
every Avalonia upgrade.** `Harness/ManualRenderTimerTests.cs` holds what it does. From CornflowerUI's harness (PR #157). An option on
the headless platform for a manual render timer would remove the reflection.

## A one-line `TextBox` takes Page Up and Page Down and does nothing with them

**Measured in a headless test with `SimpleTheme`, and the cause not read.** With a one-line `TextBox` focused, Page Up and Page Down come
back handled, and the caret doesn't move, so a `ScrollViewer` around it never sees them. A browser's `<input>` leaves them to the page,
which scrolls by a page (measured in Chromium for #67). Arrows, Home and End move the caret, and Space isn't handled as a key, as
expected. `AvonicContent` takes the two keys on their way down from a one-line field to get around it (`OnContainerKeyDownTunnel`). A
multi-line `TextBox` handling them is right, as a `<textarea>` moves its caret with them. Not checked with `FluentTheme`.

## A change of `AutomationProperties.Name` raises no name change on the peer

**Measured in a headless test, and the cause not read.** With a peer made for a stock `Button` (and for each control in
`AttributeForwardingTests`), `AutomationProperties.SetName` after load changes what `GetName()` returns, but the peer's `PropertyChanged`
raises nothing for `AutomationElementIdentifiers.NameProperty`. A browser fires a name change when an element's `aria-label` changes,
which Ionic 9's `utils/attribute-controller.js` relies on (#207), so a screen reader may not notice a renamed control until it reads it
again. `AvonicToggle` raises it itself, for a name made from its content. Not tried with a real screen reader.

## A `TextBlock` in RTL keeps trailing punctuation at the end of the line

**Measured in a headless test, and the cause not read.** `"Loading..."` in a `TextBlock` with `FlowDirection="RightToLeft"` is one
shaped run at bidi level 2, and draws as `Loading...`, its full stops on the right. Chromium, in a `dir="rtl"` paragraph, resolves the
trailing full stops to the paragraph's direction and draws `...Loading` (measured in `ion-loading`'s RTL frame, #41). A `TextLayout`
made directly with the same text, Roboto 14 and `FlowDirection.RightToLeft` does split it, `...` at level 1 before `Loading` at level 2,
so it's something in how `TextBlock` builds its layout. Avonic accepts it: Latin text in an RTL window ending in punctuation shows
the punctuation at the other end from Chromium's. Not tried in a real window or with Arabic or Hebrew text, where it may not show.
