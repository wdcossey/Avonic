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
