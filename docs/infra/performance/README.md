# Performance

Status: done

What the theme costs an app, and what stops that creeping up unnoticed. The evidence and the options tried are in
[PLAN.md](PLAN.md).

## What it costs

Avalonia checks **every global style rule against every element**, in any app, even one that uses none of these controls. It
keeps no cache by type, and it visits nested rules whether or not the parent matched. Global rules are the ones in
`src/Avonic.Themes/Styles/`. Rules in a `ControlTheme` only cost that control.

| Against an element it can't match, by the form of the rule | Per element, per rule |
|---|---|
| Targets a type of ours, even through a template and a slot | about 7 to 16 ns |
| A comma of 5 of those | about 84 ns |
| Could match the element, such as `c\|Foo > TextBlock` or `:is(Control)` | about 55 ns |
| A descendant selector on the element, at a depth of 20 | about 385 ns |

Across the real theme it comes to **about 30 ns for each alternative between commas**, whichever kind it is, and 27 to 33 across
runs. That's why alternatives are what's budgeted. These times are from this machine, and the small differences are close to the noise. The
counts are exact.

## The budget

`StyleBudgetTests` fails CI when:

| Check | Now | Limit |
|---|---|---|
| Alternatives in all global rules | 176 | 200 |
| Alternatives getting past a `TextBlock`'s type | 51 | 60 |
| Alternatives getting past the type of any other common element, at most (`Border`) | 19 | 25 |
| Alternatives that look at every ancestor of a common element | 0 | 0 |

**When a control goes over,** the failure lists the largest rules. First see whether the commas can go. If they can't, raise
the limit in that PR and say so in its description, with the new total. The limits are meant to be raised. The point is that
it's a decision.

A selector with `> :is(Panel) >` is two alternatives for each thing it targets, so it's worth a thought.

**600 alternatives is a checkpoint,** about 18 µs per element, decided in [ADR 0002](../../decisions/0002-decisions-from-the-first-swarm.md). A test stops the limit going past it. About 67
Ionic tags remain, and at today's 10 or so each the library ends near 820, or about 25 µs, so **we expect to reach it, after about 44 more.** When we do, stop, and
run the benchmark on a real phone, to decide on real numbers. The way out is controls working out their host's state in code, with the theme reading a pseudoclass,
which removes most of the cost but adds code to every control. Otherwise, raise the checkpoint.

## The trend

Run by hand at each tier's roll-up, on the same machine, and add a row. Timings in CI would mean nothing.

```
AVONIC_BENCHMARK=/absolute/path/row.md dotnet test tests/Avonic.Components.Tests -c Release --filter FullyQualifiedName~StyleCostBenchmark
```

It alternates with and without the theme, 25 rounds, attaching 5,000 elements at a depth of 20, with Avalonia's diagnostics off,
as an app has them. Times are µs per element. A row can go straight in the table.

| When | Controls | Rules | Alternatives | `TextBlock` | `Border` |
|---|---|---|---|---|---|
| 2026-11-06 | 18 | 98 | 174 | 18.5 to 24.2 (+5.6) | 2.2 to 8.0 (+5.8) |
| 2026-11-09 (after P1) | 35 | 146 | 243 | 20.2 to 27.1 (+6.9) | 2.3 to 10.1 (+7.8) |

### On a phone

A phone can't run the test, so the sample's **device** page runs the same thing in the app (`Diagnostics/StyleCostRunner.cs`): the
same trees, rounds and row, from `Diagnostics/StyleCost.cs`, which the test uses too. Without the theme means
`AvonicComponentsStyles` in the app's `AvonicTheme`'s place, as the test's app has, so that Avonic's controls keep a template. It
attaches into a panel in the shell, outside Avonic's controls, so that a tree's ancestors are the same both ways, and lays out. So its times include the sample's `SimpleTheme`, which the
test's don't, and **a phone's row is compared with a desktop row from the same page**, in the desktop sample's Release build, not with
the rows above. It says when a row isn't for the table: a Debug build, or Avalonia's diagnostics on. How to run it on the Pixel is in
[the Android docs](../android/README.md#on-the-device).

| Where | When | Controls | Rules | Alternatives | `TextBlock` | `Border` |
|---|---|---|---|---|---|---|
| Pixel 8, Android 17 | 2026-11-09 | 35 | 146 | 243 | 55.3 to 107.7 (+52.4) | 15.1 to 53.6 (+38.4) |
| The PC, Windows 11 | 2026-11-09 | 35 | 146 | 243 | 9.7 to 26.8 (+17.1) | 2.7 to 10.2 (+7.6) |

The phone is 3 to 6 times slower than the PC, which is in line with their CPUs ([comparison](../android/COMPARISON.md#the-benchmark)). The cost
is style matching on the UI thread, before anything is drawn, so a different renderer wouldn't change it.

## Scrolling a virtualised list

Measured for [#68](https://github.com/wdcossey/Avonic/issues/68), by hand, in counts from Avalonia's diagnostics, as times
from tracing mean nothing:

```
AVONIC_SCROLL_COST=/absolute/path/rows.md dotnet test tests/Avonic.Components.Tests -c Release --filter FullyQualifiedName~AvonicListScrollCostTests
```

It scrolls a list of 1,000 items in a `VirtualizingStackPanel`, in a `ScrollViewer` 390 by 844, from the top to the end and back,
48 at a time, about 2,000 steps, after the first screen. Each column is a total over the whole run: layout passes, containers the
panel prepared, controls the `ItemTemplate` made, measures, arranges, and style rules checked against an element.

| List | When | Passes | Prepared | Made | Measures | Arranges | Style checks |
|---|---|---|---|---|---|---|---|
| A sticky divider every 20 rows | before #68 | 1,930 | 1,981 | 3,962 | 34,350 | 60,237 | 6,181,072 |
| | after | 2,030 | 1,981 | 4,258 | 35,440 | 61,328 | 6,405,274 |
| A divider every 20 rows, not sticky | before and after | 1,930 | 1,981 | 3,962 | 34,350 | 60,237 | 6,181,072 |
| Inset lists of one item | before #68 | 3,964 | 1,982 | 3,964 | 37,658 | 69,370 | 8,136,110 |
| | after | 3,299 | 1,978 | 3,956 | 36,940 | 63,343 | 8,119,690 |

**Sticky dividers cost 2 to 7% more on each count** to scroll than before, which didn't keep them stuck: the list's copy of the stuck divider
is made again at each change of section, and that's a layout pass and its styles. Dividers that don't stick cost nothing more.
**Inset lists cost a little less,** as a row takes the one before to be alike until the pass is over, and so a run of alike rows lays out once.

In both, about 3,000 style checks a step are the rows the panel makes as they come in, whatever they are. That's what
`StyleBudgetTests` keeps down.

## In a Debug build

The sample shows a line along the bottom when a page opens:

```
ion-card: 160 ms, 143 elements, 98 rules (174), layout 86.2, render 5.8
```

<img src="sample-line.png" alt="The sample in a Debug build, with the line along the bottom" width="300">

This one is from headless capture, and it hasn't been tried in a real window yet.

That's the time to open the page, from before it's made, the elements in it, the global rules and their alternatives, and Avalonia's own layout and
render times over that time. A Release build doesn't have it. **Nothing is added to the library,** because Avalonia has this
built in, behind a switch that's off by default:

- `AppContext.SetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", true)`, before Avalonia starts
- Meter `Avalonia.Diagnostic.Meter`: `avalonia.ui.measure.time`, `.arrange.time`, `.render.time`, `.input.time`,
  `avalonia.comp.render.time`, `.update.time`, `avalonia.ui.visual.count`, `avalonia.ui.event.handler.count`
- Activity source `Avalonia.Diagnostic.Source`: `Avalonia.AttachingStyle`, with the selector and its result, and
  `EvaluatingStyle`, `FindingResource`, `MeasuringLayoutable`, `ArrangingLayoutable`, `PerformingHitTest`, `RaisingRoutedEvent`

Tracing activities costs far more than the checks being traced, so use its counts and not its times. The sample's line doesn't
trace.

## Known limits

- **`render` can read low,** if a page opens within a frame of the last one, because its own render pass hasn't happened yet.
- **Only style checks are budgeted.** A slow control, layout or animation wouldn't show here. The line in the sample is how
  to spot those.
- **The table's numbers are from one desktop PC.** A Pixel 8's row and the PC's from the same page are above.
- **Scrolling** is measured in counts only, for a virtualised list: [above](#scrolling-a-virtualised-list). The times are all times to attach.
