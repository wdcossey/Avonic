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

**What the trace counts.** `StyleTrace` counts an element's own checks, made as it joins the tree. Its template's parts are
elements with counts of their own, styled later in layout, and aren't added to it. A `ControlTheme`'s checks are counted, as an
opt-in file's are a real cost ([below](#opt-in-files)). Any type of Avonic's own stops an ancestor walk, with or without the prefix
(`FlexSlot`, `LabelledControlPanel`), and a `#` in a property's value isn't a name. With Avalonia's diagnostics off, as the
benchmark's variable leaves them, it refuses to run, where it would see nothing and pass every budget. From CornflowerUI's copy.

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
| | after | 5,275 | 1,978 | 4,944 | 50,776 | 89,057 | 8,119,690 |

**Sticky dividers cost 2 to 7% more on each count** to scroll than before, which didn't keep them stuck: the list's copy of the stuck divider
is made again at each change of section, and that's a layout pass and its styles. Dividers that don't stick cost nothing more.
**Inset lists cost about a third more layout,** and the same in styles: a new row reads the one before only once the pass is
over, as the panel gives it an index only then, and so it's laid out again when its margin changes. A row whose item before isn't
realised makes that item from its template to read it: about one a row, scrolling up. A guess before the pass, tried, laid out
once where the sections are alike, but where they differ the panel recycled and remade the top row for ever.

**A jump** from the top to row 9,000 of 10,000, in the same test: the time of that pass in a Release build, with
diagnostics on, and the rows the `ItemTemplate` made. A list only looks for a section's divider once a sticky one has been a
row, so a plain list pays nothing. It makes rows the panel never realised only within 200 rows of the first one realised.

| List | Unbounded | Bounded, now |
|---|---|---|
| 10,000 plain rows | 7.9 ms, 37 made | 8.3 ms, 37 made |
| A sticky divider every 50 | 8.3 ms, 70 made | 8.2 ms, 70 made |
| One sticky divider, at the top | 40.6 ms, 10,002 made | 17.6 ms, 236 made |

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
render times over that time. The rules are the application's and those in the `Styles` of the page and each of its ancestors, counted
for each page; rules further down cost only their own part, and aren't counted. With Avalonia's diagnostics off, the line says so,
rather than show zeros. A Release build doesn't have it, unless it's built for [page costs](#page-costs-on-a-device). **Nothing is added to the library,** because Avalonia has this
built in, behind a switch that's off by default:

- `AppContext.SetSwitch("Avalonia.Diagnostics.Diagnostic.IsEnabled", true)`, before Avalonia starts
- Meter `Avalonia.Diagnostic.Meter`: `avalonia.ui.measure.time`, `.arrange.time`, `.render.time`, `.input.time`,
  `avalonia.comp.render.time`, `.update.time`, `avalonia.ui.visual.count`, `avalonia.ui.event.handler.count`
- Activity source `Avalonia.Diagnostic.Source`: `Avalonia.AttachingStyle`, with the selector and its result, and
  `EvaluatingStyle`, `FindingResource`, `MeasuringLayoutable`, `ArrangingLayoutable`, `PerformingHitTest`, `RaisingRoutedEvent`

Tracing activities costs far more than the checks being traced, so use its counts and not its times. The sample's line doesn't
trace.

## Page costs on a device

A build with `-p:AvonicPerf=true` is a Release build (AOT and trimmed on Android, as an app ships) with the line on, which
opens every page by itself, three rounds, back to the list between, and writes each line, prefixed `AVONIC-PERF`, to the
console (`SampleShell.PageCosts.cs`). The device page is left out, as its benchmark is its own. The first round pays for
first use; the later rounds are what a user pays each time.

After the sample's pages, each round opens three pages that only this build has (`Diagnostics/PageCostSet.cs`), for the
cost of one datetime (#248): `datetime-x1`, `datetime-x2` and `datetime-x4`, with 1, 2 and 4 plain `AvonicDatetime`s
(`Diagnostics/DatetimeCostView.cs`), the default calendar and time, with `Locale="en-US"` and `Value="2023-06-15T13:47:00"`
pinned, so that every run shows June 2023. Then the same three as single grids (`CalendarRendering="SingleGrid"`, #248's spike
A): `datetime-x1-grid`, `datetime-x2-grid` and `datetime-x4-grid`. Then the scrolled three again with the months either side of the
working one made at once, as before #248's spike B, and only when needed (`-eager` and `-press`, [below](#the-months-either-side)),
against the default pages', made a few frames after the first; and `datetime-x1-idle`, the default named, for its swipes. The
sample's own list doesn't show them.

```
dotnet build samples/Avonic.Sample.Android -c Release -p:AvonicPerf=true
adb install -r samples/Avonic.Sample.Android/bin/Release/net10.0-android/io.github.wdcossey.avonic.sample-Signed.apk
adb logcat -c
adb shell am start -S -n io.github.wdcossey.avonic.sample/io.github.wdcossey.avonic.sample.MainActivity
adb logcat -v raw -s DOTNET    # until "AVONIC-PERF done": about 3 minutes on a Pixel 8, and a minute more of month changes
```

On desktop, `AVONIC_PERF_OUT` names a file to append the lines to, and `AVONIC_PERF_EXIT=1` closes the window after. From
PowerShell 7, on any of the three:

```
$env:AVONIC_PERF_OUT = "$PWD/run.txt"; $env:AVONIC_PERF_EXIT = "1"
dotnet run --project samples/Avonic.Sample.Desktop -c Release -p:AvonicPerf=true
```

### Keep the screen on

**On battery, `svc power stayon true` does nothing**: the screen locks after its timeout (30 s on the Pixel), and Android moves the app
to the `/background` cpuset, the little cores. Every page then opens 7 to 10 times slower and `Background` work hardly gets a turn,
which looks like the app's fault (#258's first runs, and likely #255's). Set the timeout for the run, and put it back after:

```
adb shell settings get system screen_off_timeout            # note it, 30000 on the Pixel
adb shell settings put system screen_off_timeout 1800000    # 30 minutes, for the run
adb shell settings put system screen_off_timeout 30000      # after
```

On Android, each round's check line ends with the app's cpuset (`/proc/self/cpuset`, `/top-app` while it's in front; not read
elsewhere, as a desktop Linux's says nothing of this), and adds the
screen off, the keyguard locked or the activity paused; any of those, or a cpuset other than `/top-app`, adds a
`warning: the app wasn't in front`, which `table.js` shows in bold. A run with it measured a throttled phone: don't keep it.

### Only some pages

`AVONIC_PERF_ONLY` keeps the pages whose titles hold any of its terms, split by commas or spaces, ignoring case; a term
that starts with `=` keeps only the page of that title (`=ion-datetime`, without `ion-datetime-button`). Unset, every page
opens. The run's second line says which it kept. `datetime` keeps `ion-datetime`, `ion-datetime-button` and the thirteen above,
about two minutes on the phone, most of it the month changes and the swipes; `datetime-x` only the thirteen; `datetime-x1` only the five
with the month changes or the swipes. `-grid` keeps the grid pages and `ion-grid`. Use commas, not spaces, through `adb shell`, which splits its arguments again.

On desktop it's the environment variable (`$env:AVONIC_PERF_ONLY = "datetime"`, and `Remove-Item Env:AVONIC_PERF_ONLY` for
every page again). On Android it's an intent extra of the same name, which `MainActivity` reads before the shell starts:

```
adb shell am start -S -n io.github.wdcossey.avonic.sample/io.github.wdcossey.avonic.sample.MainActivity -e AVONIC_PERF_ONLY datetime
```

### Month changes

On `datetime-x1`, and `datetime-x1-grid` (its lines labelled `datetime-month-grid`), after its line, each round changes month 10 times forward and 10 back, as a tap on the calendar's next
and previous month buttons does (`Diagnostics/MonthChangeTiming.cs`). It finds the buttons by their automation names,
`Next month` and `Previous month`, and raises the routed `Click` event their type, or the nearest base type, registers, which
a tap ends in, so a new design of the calendar is timed the same way as long as it keeps those names and changes month on
`Click` (not on a `Command`, which raising the event doesn't run). It makes no automation peer: a peer stays on its control, and
on Android it's what Avalonia's accessibility bridge works from. Each change is a line, and each round ends with
a summary:

```
datetime-month: round 2, next 3, August 2023 to September 2023, first 23.4 ms, heading 412.0 ms, 61 frames, longest 48.0 ms, gap median 8.3 ms, 3 over 16.7 ms, 1 over 33 ms
datetime-month: round 2, 20 changes, first median 21.0 max 35.0 ms, heading median 405.0 max 460.0 ms, longest median 40.0 max 120.0 ms, gap median 8.3 ms, 1212 frames, 33 over 16.7 ms, 12 over 33 ms
```

- **first**: from the click until the first frame with it is drawn: the compositor's next batch, whose `Rendered` task
  completes on the render thread.
- **heading**: from the click until a frame in which the month heading, a `TextBlock` reading the month as en-US's
  `MMMM yyyy`, shows the new month. Ionic's calendar changes it once its scroll has rested; a design that changes it at
  once will read low here, and its transition is in the frames instead.
- **frames**, **longest**, **gap median**, **over 16.7 ms**, **over 33 ms**: the top level's frame callbacks
  (`RequestAnimationFrame`), asked for again from each one, over a window of at least 1 s from the click, and until the
  heading shows the month (5 s at most). Avalonia paces them by its composition batches, one a frame of the render loop, so a
  frame the UI thread was too busy to give shows as a long gap. The first gap is from the click. The median gap is the
  display's frame when nothing is slow: 8.3 ms at 120 Hz. At 60 Hz an ordinary frame is near 16.7 ms, and some count as
  over it, so read the 33 ms column there. With no frame for 10 s (the display off, the app in the background) the round's
  changes stop with a `failed:` line, and the run goes on.

The frame callbacks need no meter, so they work with `MetricsSupport` off ([below](#on-android-only-the-total-is-measured));
`PageCostTests` drives the same code headlessly.

### What the month changes may leave behind

The first run on the Pixel (v2 44824da, `-e AVONIC_PERF_ONLY datetime`) went wrong after the first round's month changes:
every later page took 5 to 10 times as long to open, about 2 ms an element instead of 0.3, and no later month change was
seen, because the calendar never got to its new month. Both wait on `Background` work: the page's line is posted at
`Background`, and the calendar's scroll rest is a `Background` timer. On Android that runs only in the looper's idle time
(Avalonia's idle handler), so something left the app no idle time, while its frames went on at 60 Hz. Headless, nothing was
left: no frame callback asked for, no clock subscriber, nothing scheduled, and later rounds as fast as the first. So the
timing now leaves nothing that could be the cause (#248):

- no automation peer, the one thing it did that only Android answers on its UI thread;
- no frame callback once a change is over, counted by `MonthChangeTiming.PendingFrames`;
- no change going on after its page has gone: a calendar that leaves the tree ends its change at once, where it used to have
  frames drawn for up to 5 s;
- no timer, and no reference to the calendar once it has run.

After each round's month changes, and their page has been left, the round checks it (`Diagnostics/PerfChecks.cs`):

```
datetime-month: round 1, check: 0 frames asked for, page collected, background post 0.4 ms before and 0.6 ms after, accessibility off, touch exploration off, services: none
```

and adds a `warning:` line for a frame loop still running, a page still alive after a full collection, or a `Background` post
that waited more than 100 ms (over 10 s reads `over 10 s`). The Android head adds whether accessibility is on and which services
are, as their queries of Avalonia's accessibility bridge run on the UI thread, also in a `platform:` line at the start.
`table.js` lists the checks under the tables, the warnings in bold. A run with a warning measured something other than the
calendar: keep it out of `page-costs/`.

The baseline's round 1 (3201219) had two failures that later rounds didn't: ten nexts that never left June (`heading n/a`, each
watched the full 5 s), then "the calendar left the tree" at the first previous. Both most likely come from the screen locking
during the round ([above](#keep-the-screen-on)): the phone on the little cores, and the scroll rest, a `Background` timer, never
getting a turn (`PageCostTests.With_No_Idle_Time_The_Calendar_Stays_In_The_Tree` gives the same ten lines with the timer held);
nothing in the calendar or the timing takes a page away. The rounds now write a `note:` line when a back request or the back
button takes the page away, or the shell leaves its top level, and `table.js` lists it, in bold, with the checks. The single
grid's change waits on no timer, only on frames, so it changes month with no idle time at all (the same test).

### Device diagnostics

Made while every page of #258's Pixel runs, and of v2's, grew 7 to 10 times slower after round 1, to tell the collector, a
loop on the UI thread and something left behind apart. It was none of them: the screen had locked ([above](#keep-the-screen-on)),
and these figures were what ruled the app out. They stay, cheap enough for the device (`Diagnostics/DeviceProbe.cs`):

- **On each page's line**, after its own figures: `gc 3/1/0, gc pause 12.3 ms, heap 85.2 MB, cpu 210 ms, ui thread 190 ms, passes 4,
  tree 1234, peers 0`: collections of each generation and their pauses during the open, the heap after it, the process's and the UI
  thread's CPU time (the thread's from `/proc/thread-self/stat`, `n/a` where there's none), the top level's layout passes, and the
  visuals under the top level with how many have an automation peer already. `table.js` reads the line as before.
- **A busy window** of 2 s after each page is left, and after each month timing's checks: `...: busy: 2.0 s, cpu 1830 ms, ui thread
  1790 ms, passes 0, frames 120 longest 16.9 ms, default post 20/20 median 0.3 max 1.2 ms, background post 20/20 median 0.5 max 3.1
  ms, gc 0/0/0, gc pause 0.0 ms, heap 85.2 MB, tree 312, peers 0, left page gone`, with the list on screen and nothing asked of the
  app: its layout passes, its frames (asked for each frame, so a held thread shows as a long gap), how late a `Default` and a
  `Background` post ran (one of each every 100 ms), the collector, the CPU, the tree, and the page just left: gone, or alive, in a
  top level or not, with its visuals and peers, how often its calendar's scroller changed, and whether a collection after the
  window takes it (`then collected`, or `STILL ALIVE`). `table.js` lists these with the checks.

Read them so: UI-thread CPU with no passes and no collections in the window is work above `Background` that isn't layout (a loop);
passes with nothing on screen changing is layout invalidated again and again; pauses and a heap that grows from round to round are
the collector; a tree that grows on the list, or a left page still in a top level, is something left behind; peers where there were
none is Avalonia's accessibility bridge at work. Headless, 20 scrolled month changes leave nothing of this: the busy window after
them is idle (16 ms of CPU, no passes, no collections) and the next page opens no slower.

**A page "still alive" on Android may not be held.** The runtime there is Mono, whose collector scans each thread's stack
conservatively, so a reference left in a stack slot keeps a page until the slot is reused. The check now collects from fresh turns
of the dispatcher, several times over 400 ms, and the busy window collects again at its end; a page that survives both is held.
Headless (CoreCLR, precise), every timed page goes.

### The single grid

#248's spike A ([#258](https://github.com/wdcossey/Avonic/pull/258)) measured a calendar of one 7x6 grid, drawn again in place, against
Ionic's three scrolled months, on the Pixel 8 (`page-costs/2026-11-20-pixel8-release-datetime-spike-a-a119279.txt`, rounds 2 and 3):

| | Scrolled | Single grid |
|---|---:|---:|
| `datetime-x1`, `-x2`, `-x4` to open | 159, 300, 513 ms | 82, 148, 264 ms |
| A month change: the heading shows the new month, median | 389 ms | 119 ms |
| A month change: frames over 33 ms, per round of 20 | 4.5 | 9.5 |

**Decided: the default stays Ionic's `ScrolledMonths`, and `SingleGrid` is opt-in**, for every datetime through
`AvonicConfig.DatetimeCalendarRendering` or for one through its `CalendarRendering` (`docs/components/ion-datetime/README.md`,
Extensions). The single grid halves the open and shows a new month 3 times sooner, but isn't Ionic's (it fades, and its swipe doesn't
follow the finger), and its fade has twice the long frames. The fade's timing stays md's fade through, 90 ms out and 210 ms in, as a
group (`AnimationGroupOpacity`); making it and the swipe's threshold settable is on the roadmap.

### The months either side

#248's spike B keeps Ionic's scrolled calendar and makes less of it up front: only the working month's 42 days at the first layout,
and the months either side's when first needed, or a few frames after the first (`docs/components/ion-datetime/COMPARISON.md`, "The
months either side, made later"). Three variants were built, each a value of an internal switch, which the perf pages pick through the
`AppContext` data `Avonic.Datetime.NeighbourMonths` (a measuring switch, not an API):

- **`AtOnce`**: as Ionic, and as before: all three months at the first layout (the `-eager` pages).
- **`WhenNeeded`**: the months either side only when needed: a touch or a pen pressed on the calendar, a scroll toward one, the next or previous
  month, a value in another month (the `-press` pages).
- **`AfterFirstFrame`**, the default: as `WhenNeeded`, and in any case after the calendar's first two frames, two weeks of days a
  frame, the next month then the previous, one datetime at a time (the plain pages).

Headless, Release, the real clock and frames, 390x844 at 2x, median of 10 rounds alternating the variants, two dropped
(`DatetimeNeighbourCostBenchmark`, `AVONIC_DATETIME_COST=/absolute/path.md dotnet test tests/Avonic.Components.Tests -c Release
--filter FullyQualifiedName~DatetimeNeighbourCostBenchmark`). A sanity check: the desktop skews it, and the Pixel decides.

| Variant | Datetimes | Elements | Open | Longest frame of the 40 after | First press on a calendar |
|---|---:|---:|---:|---:|---:|
| `AtOnce` | 1 | 725 | 29.3 ms | 8.6 ms | 0.1 ms |
| `WhenNeeded` | 1 | 296 | 13.7 ms | 7.2 ms | 16.7 ms |
| `AfterFirstFrame` | 1 | 296 | 13.3 ms | 8.7 ms | 0.0 ms |
| `AtOnce` | 2 | 1442 | 59.2 ms | 10.4 ms | 0.1 ms |
| `WhenNeeded` | 2 | 584 | 27.1 ms | 8.5 ms | 19.9 ms |
| `AfterFirstFrame` | 2 | 584 | 25.8 ms | 25.5 ms | 0.0 ms |
| `AtOnce` | 4 | 2891 | 146.0 ms | 49.2 ms | 0.1 ms |
| `WhenNeeded` | 4 | 1175 | 51.9 ms | 12.0 ms | 42.2 ms |
| `AfterFirstFrame` | 4 | 1175 | 54.1 ms | 33.8 ms | 0.0 ms |

- **Opening costs less than half** with either deferred variant: the two months either side were 429 of the page's 725 elements.
- **`WhenNeeded` moves the cost to the first press**, 17 ms for one calendar headless, about five times that on the phone going by
  the opens: a hitch just as the finger lands, before its first move is drawn.
- **`AfterFirstFrame` spreads it over the frames after**: each of its frames costs 2 to 5 ms headless (two weeks of days), after the
  page's first two frames, which cost 7 to 15 ms in every variant. The longest frames above (25 and 34 ms for 2 and 4 datetimes) come
  in the same frame of each round, and are most likely a collection; one calendar's six frames have none. The total work is the
  same as before, but no frame carries much of it, and the first press finds the months made.

**Kept: `AfterFirstFrame`**, with `WhenNeeded`'s triggers, which every variant has, for whatever comes first. Nothing a user sees
differs from Ionic's, so it's the scrolled calendar itself, not a third `CalendarRendering`.

**On the phone**, each round of a `datetime` run now also times a swipe on `datetime-x1-eager`, `-press` and `-idle` (not the plain
`datetime-x1`, whose month changes are timed as in the runs kept before):
`MonthChangeTiming` with `bySwipe`, a touch pressed on the calendar's scroller, its offset moved a month over 12 frames from the
frames' callbacks, as a finger's moves come, then lifted, and once back. Its lines are the month changes' (`table.js` reads them),
labelled `datetime-swipe-idle` (the default), `datetime-swipe-eager` and `datetime-swipe-press`: the first swipe's `first` and longest
gap show what the press cost. `-e AVONIC_PERF_ONLY datetime-x` runs the thirteen pages with the swipes and both pages' month changes.

### Keeping a run

Each run worth keeping goes in [`page-costs/`](page-costs/README.md), with the device, the build and the commit, and
`node tools/page-costs/table.js` puts one run, or a run before and after, in tables: the pages, and the month changes per
round and over the later rounds. Comparing two runs, its last row sums only the pages both have, so a filtered run compares
with a full one.

Keep the phone unlocked and untouched for the whole run: a touch, a dimmed screen or a lock shows as pages several times
slower.

### On Android only the total is measured

.NET for Android builds with `MetricsSupport` off, so Avalonia's meter is silent and the layout and render columns read 0.
Turned on, it fills them, but makes every page 6 to 10 times slower (Pixel 8, measured 2026-11-20: the datetime page 10 s
instead of 1 s), so the perf build leaves it off. The share of the time that is layout comes from the desktop run, where the
meter costs little.

CI builds the desktop sample and the Android head with `-p:AvonicPerf=true`, so that this code can't rot.

## Opt-in files

[Global typography](../typography/README.md) (`Css/Typography.axaml`) isn't in `AvonicTheme`, so the budget above doesn't count it.
It's a `ControlTheme` for `TextBlock`: merged, it adds 6 alternatives to each `TextBlock` and none to anything else, which
`TypographyBudgetTests` holds.

## Known limits

- **`render` can read low,** if a page opens within a frame of the last one, because its own render pass hasn't happened yet.
- **Only style checks are budgeted.** A slow control, layout or animation wouldn't show here. The line in the sample is how
  to spot those.
- **The table's numbers are from one desktop PC.** A Pixel 8's row and the PC's from the same page are above.
- **Scrolling** is measured in counts only, for a virtualised list: [above](#scrolling-a-virtualised-list). The times are all times to attach.
