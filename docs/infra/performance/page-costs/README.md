# Page costs, run by run

Each file is one run of the `-p:AvonicPerf=true` build ([how](../README.md#page-costs-on-a-device)), as logged: every sample page
opened three rounds, one line per open. Kept so that any later change, to a control, to the rendering, or to the runtime, is
compared with the same pages on the same device.

| File | Device | Build | Commit |
|---|---|---|---|
| `2026-11-20-pixel8-release.txt` | Pixel 8 (`shiba`), Android API 37, arm64 | Release, AOT, trimmed; .NET 10.0.14 | v2 f5de215 + #247 |
| `2026-11-20-desktop-release.txt` | Windows 11, x64, 64 cores | Release; .NET 10.0.13 | v2 f5de215 + #247 |

The **Pixel's run is the reference.** The desktop is several times faster on every page (5.4× over all of them), so a change that
looks free there may not be; use it for the share of the time that is layout, which Android can't measure
([why](../README.md#page-costs-on-a-device)). One Pixel is still one phone: a slower one would show the floor.

```
node tools/page-costs/table.js 2026-11-20-pixel8-release.txt                     # one run, slowest page first
node tools/page-costs/table.js 2026-11-20-pixel8-release.txt <new run>.txt       # before and after, with the ratio
```

Name a new run `<date>-<device>-<build>.txt`, add a row above with the commit it measured, and say what changed since the run
it's compared with. #248 holds what was found and what to try.

## Changes outside Avonic that may move these

Not counted on, but each is worth a run when it arrives, against the same device's last run:

- **.NET 11**, and its runtime and AOT improvements. Avonic builds with the .NET 10 SDK (`global.json`) and the sample targets
  net10.0, so moving the sample's runtime is its own change.
- **Avalonia's rendering on Android**, should it move to another backend (there has been talk of Impeller). Not announced as a
  plan here; recorded so that a jump in a later run has a known suspect.
