# Page costs, run by run

Each file is one run of the `-p:AvonicPerf=true` build ([how](../README.md#page-costs-on-a-device)), as logged: every sample page
opened three rounds, one line per open (or the pages a filter kept), and the month changes' lines. Kept so that any later
change, to a control, to the rendering, or to the runtime, is compared with the same pages on the same device.

| File | Device | Build | Commit |
|---|---|---|---|
| `2026-11-20-pixel8-release.txt` | Pixel 8 (`shiba`), Android API 37, arm64 | Release, AOT, trimmed; .NET 10.0.14 | v2 f5de215 + #247 |
| `2026-11-20-desktop-release.txt` | Windows 11, x64, 64 cores | Release; .NET 10.0.13 | v2 f5de215 + #247 |
| `2026-11-20-pixel8-release-datetime-3201219.txt` | Pixel 8 (`shiba`), Android API 37, arm64 | Release, AOT, trimmed; .NET 10.0.14; `AVONIC_PERF_ONLY=datetime` | v2 3201219 (#256): **the datetime spikes' baseline**. Checks clean. Round 1 is first use: its `ion-datetime-button` open (2609 ms) and its month changes (stopped after 10, "the calendar left the tree during a change") aren't counted; rounds 2 and 3 are |

The **Pixel's run is the reference.** The desktop is several times faster on every page (5.4× over all of them), so a change that
looks free there may not be; use it for the share of the time that is layout, which Android can't measure
([why](../README.md#on-android-only-the-total-is-measured)). One Pixel is still one phone: a slower one would show the floor.

```
node tools/page-costs/table.js 2026-11-20-pixel8-release.txt                     # one run, slowest page first
node tools/page-costs/table.js 2026-11-20-pixel8-release.txt <new run>.txt       # before and after, with the ratio
```

Name a new run `<date>-<device>-<build>.txt`, add a row above with the commit it measured, and say what changed since the run
it's compared with. #248 holds what was found and what to try.

The two runs above are from before the spike harness, and have no `datetime-x` pages and no month changes. Compared with
one, a newer run's last row sums only the pages both have.

A datetime run made at v2 44824da measured nothing past its first round's month changes, which left the app with no idle time
on the Pixel ([why](../README.md#what-the-month-changes-may-leave-behind)): don't keep one. Use a build with the round's
checks, and a run whose checks have no `warning:`.

## A spike's run beside the baseline

The datetime spikes of #248 (one 7×6 grid updated in place; only the month in view built until the first drag; lighter day
cells) are each measured the same way, so that they compare with each other and with the design they'd replace:

1. **The baseline:** v2 at the commit the spike branched from, `AVONIC_PERF_ONLY=datetime`, on the Pixel 8 Release build. Name
   it `<date>-pixel8-release-datetime-<v2 commit>.txt`, unless a baseline of that commit is already here.
2. **The spike:** its branch, the same filter, the same phone, in the same sitting where it can be. Name it
   `<date>-pixel8-release-datetime-spike-<letter>-<commit>.txt`.
3. `node tools/page-costs/table.js <baseline> <spike>`, and add both rows to the table above, the spike's with its branch.
   Paste the output in the spike's PR and in #248.

The page lines say what opening costs (`datetime-x1`, `-x2` and `-x4` give the cost of one datetime and how it grows); the
month changes say what browsing costs: the time to the first frame drawn, to the heading showing the month, and the longest
gaps between frames, the jank. A spike that wins on one and loses on the other shows both.

## Changes outside Avonic that may move these

Not counted on, but each is worth a run when it arrives, against the same device's last run:

- **.NET 11**, and its runtime and AOT improvements. Avonic builds with the .NET 10 SDK (`global.json`) and the sample targets
  net10.0, so moving the sample's runtime is its own change.
- **Avalonia's rendering on Android**, should it move to another backend (there has been talk of Impeller). Not announced as a
  plan here; recorded so that a jump in a later run has a known suspect.
