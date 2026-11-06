# Performance: plan

The human asked to keep on top of performance, so that a library built over many sessions doesn't turn out slow at the end,
and suggested telemetry for Debug builds.

## Evidence

**Avalonia's source, 12.1.4.** `StyledElement.ApplyStyles` walks every `Style` in every host's `Styles`, and `ApplyStyle` then
walks its `Children`, whether or not the parent matched. `Style.TryAttach` runs the selector each time. A result of
`NeverThisType` isn't kept anywhere.

**The trace agrees.** The theme has 98 rules in `Styles/`, and each element was checked against exactly 98.

**Cost by form,** with 200 of each form against 5,000 `TextBlock`s at a depth of 20, tracing off, 15 alternating rounds. The
spread between rounds was 30 to 50 ms, so only the large differences mean anything:

| Form | Over none | Per element, per rule |
|---|---|---|
| `c\|AvonicLabel` | 7.0 ms | 7.0 ns |
| `c\|AvonicLabel:a` | 9.2 ms | 9.2 ns |
| `c\|AvonicItem:a c\|AvonicLabel` | 10.9 ms | 10.9 ns |
| `c\|AvonicItem /template/ ContentPresenter#PART_Start > c\|AvonicIcon` | 16.2 ms | 16.2 ns |
| A comma of 5 own types | 84.0 ms | 84.0 ns |
| `c\|AvonicLabel > TextBlock.h1` | 60.1 ms | 60.1 ns |
| `c\|AvonicLabel > TextBlock` | 54.2 ms | 54.2 ns |
| `c\|AvonicItem /template/ ContentPresenter > :is(Control)` | 54.3 ms | 54.3 ns |
| `c\|AvonicLabel TextBlock` | 385.0 ms | 385.0 ns |
| 199 nested in one parent | 31.1 ms | 31.1 ns |

**Cost by rule in the real theme,** removing rules at runtime, 25 rounds:

| Theme | Rules | Alternatives | µs per `TextBlock` over no theme |
|---|---|---|---|
| Whole | 98 | 174 | 4.8 |
| Only those reaching a `TextBlock` | 23 | 51 | 1.3 |
| Only the others | 75 | 123 | 3.8 |
| None | 0 | 0 | -0.3 |

So about 27 ns an alternative in both groups, and no rule to blame. An earlier guess was that the broad rules were the
problem. They aren't, or not more than the rest.

## Decisions

**Budget alternatives, not rules or time.** Alternatives track cost, and counting rules alone would reward commas. Counts are
exact, so they can run in CI. Timing can't.

**No code in the library.** The suggestion was Debug telemetry. Avalonia already has it, so the library needs nothing, which
also means no `#if DEBUG` behaviour that differs from Release. The line is in the sample.

**The sample's line doesn't trace.** Rule checks are worked out from a count of the rules, because tracing changes what it
measures.

## Tried, and not used

| Option | Why not |
|---|---|
| Nesting rules under one parent, so that a type that can't match skips them all | Avalonia visits children regardless. Measured at 31 ns per rule, not zero |
| A rule for one of our controls in its own `ControlTheme`, as `c\|AvonicItem:ion-color ^`, so that only that control pays | **It compiles and ignores everything left of `^`, so it applies everywhere.** A probe outside any item got the rule's values. For the Avalonia issues list |
| A control working out its host's state in code, and the theme reading a pseudoclass | It works, and it would remove most of the cost, but it's code in every control, and against today's numbers it isn't worth it. **This is what to reach for if the budget ever gets too tight** |

## Not in this PR

- The tool that drives the real window, which is its own PR.
- Trying the sample's line in a real window, which comes with that tool.
- Reducing the existing rules. There's no one rule to fix, and it can wait until the budget says otherwise.
- The issues list for Avalonia, which goes in the roll-up with the other entries.
