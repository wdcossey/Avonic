# Plan: an Android project for the sample

[#66](https://github.com/wdcossey/Avonic/issues/66). Infra, so there's no `core.json` checklist. The Ionic side is the issue's list of
what can only be tried on a phone: touch and the ripple, scrolling momentum, safe areas, shadows at 3×, back, an overlay presented in a
single view, and the style benchmark.

Avalonia 12's own guidance was read from its docs (the "Breaking changes in Avalonia 12" page, Android section) and from its source at
tag `12.1.4`, not from memory:

- the activity derives from the non-generic `AvaloniaMainActivity`, and a class deriving from `AvaloniaAndroidApplication<TApp>` with
  `[Application]` builds the `AppBuilder`, through `CustomizeAppBuilder`;
- Android's lifetime is `IActivityApplicationLifetime`, with a `MainViewFactory`, which the docs recommend. The same object also
  implements `ISingleViewApplicationLifetime`, whose `MainView` setter logs a warning and fills the factory with that one view
  (`src/Android/Avalonia.Android/ApplicationLifetime.cs`);
- the hardware and gesture back raise `TopLevel.BackRequestedEvent` on the `TopLevel`, a bubbling event whose route is the `TopLevel`
  alone, and Android reads `Handled` when it returns: unhandled, the activity goes back, which leaves the app;
- `TopLevel.AutoSafeAreaPadding` is on by default. The `TopLevel` sets its child's `Padding` to `InsetsManager.SafeAreaPadding`, at
  style priority, and follows `SafeAreaChanged`.

## 1. Project structure

| Option | Tests | `drive.ps1` | CI | Notes |
|---|---|---|---|---|
| An Android head referencing today's `Avonic.Sample` exe | no change | no change | no change | **Tried, and it doesn't build.** NETSDK1150: a self-contained app can't reference a framework-dependent exe. With that check off, the head's `RuntimeIdentifier` flows into the exe, which has no `net10.0/android-arm64` assets (NETSDK1047). Getting past both would put RIDs on the sample and Avalonia.Desktop in the APK |
| Multi-targeting `Avonic.Sample` (`net10.0;net10.0-android`) | no change | no change | breaks | Every `dotnet build Avonic.sln` would need the android workload, unless the second target were behind a property, and then `-f net10.0-android` alone wouldn't build. Two output types in one project |
| **Avalonia's split:** `Avonic.Sample` becomes the shared library, `Avonic.Sample.Desktop` the desktop exe, `Avonic.Sample.Android` the Android head | no change: they reference `Avonic.Sample`, which keeps its name, so `avares://Avonic.Sample/...` and every namespace stay | the exe's default path | no change: the sln gains the desktop project | What Avalonia's template does, and where an iOS head would go |

**The split.** The library keeps its assembly name, so the tests, Roboto's `avares://Avonic.Sample/Fonts/Roboto` and the pictures don't change.
`Avonic.Sample.Desktop` has `Program.cs` and nothing else. `MainWindow` stays in the library, as in Avalonia's template, because the tests use
it. The consumers that change are `tools/real-window/drive.ps1`'s default `-Exe` path, its README's build line, and `CLAUDE.md`'s repository
section, which states what exists.

## 2. The single-view shell

`MainWindow`'s content (the header, the page list, a page, the performance line) moves into a `UserControl`, `SampleShell`, with the same
element names. `MainWindow` is a `Window` holding a `SampleShell`, so the desktop sample looks and behaves as before, and `drive.ps1`
finds the same automation names (`Back`, each page's title).

`App` gives a desktop lifetime a `MainWindow`, and any single-view lifetime a `SampleShell` as its **`MainView`**, deliberately not
`IActivityApplicationLifetime.MainViewFactory`:

- `AvonicOverlay.Present()` finds its `TopLevel` through `ISingleViewApplicationLifetime.MainView`. Set through the factory, `MainView`
  stays null on Android, so `Present()` would throw there. That's a gap in `OverlayHost.TopLevelOf`, reported, not worked around in a
  control. Setting `MainView` reaches the single-view branch, which is what the issue wants tried.
- Avalonia warns that a `MainView` is "not fully supported", because a recreated activity gets the same view back. The activity declares
  the configuration changes that would recreate it on rotation and resizing (orientation, screen size and layout, smallest width, UI mode).
- The overlays page gets one button, **"Present one through the app"**, calling `Present()` with no `TopLevel`, where the others call
  `Present(TopLevel)`. That's the branch the device has to try. It also works on desktop, through the active window.

## 3. Back

Overlays handle `BackRequested` on the `TopLevel`, through a handler `OverlayHost` adds when the first overlay is presented. The shell
attaches before that, and handlers on one element run in the order they were added, so a plain handler would run first and go back to the
list under an open overlay. The shell listens to `TopLevel.BackRequestedEvent.RouteFinished` instead, which Avalonia raises after every
handler on the route, before Android reads `Handled`. If nothing handled it, and a page is shown, the shell shows the list and handles it.
On the list it leaves it alone, so Android leaves the app, as an Ionic app with no history does.

The same event comes from a mouse's back button and the platform's back key on desktop, so this works there too, and can be tested headlessly.

## 4. Safe areas

`CLAUDE.md`, `Tokens/Global.axaml` and `docs/components/ion-content/README.md` say `Avonic.SafeArea*` default to 0, because
`AutoSafeAreaPadding` pads the root, and that an app that turns it off and draws edge to edge fills them from `InsetsManager.SafeAreaPadding`.
Both are worth seeing, so **the default is `AutoSafeAreaPadding`**, and a switch on the device page turns on Ionic's way: no padding on the
root, edge to edge, and the four keys filled from the insets and kept up to date. Then the shell's header pads itself by the top, left and
right keys, as a header's first toolbar does in Ionic, and the performance line by the bottom one. The page area isn't padded, as
`ion-content` isn't: items, dividers and list headers pad their own sides.

The filling is in a small class, `SafeAreaMode`, so that a test can fill the keys and see an item read them. The insets themselves only
exist on a device.

## 5. CI

**The Android project stays out of `Avonic.sln`,** so `dotnet build Avonic.sln && dotnet test Avonic.sln` don't need the workload, here or in
CI, and the main job is unchanged. **A second job, in parallel,** installs the workload on `ubuntu-latest`, which has the Android SDK and a
JDK, and builds the debug APK, so that the head can't rot unnoticed. It doesn't add to the main job's time. It hasn't run yet: it runs on the PR.

## 6. Fonts and resources

Roboto and the pictures are `AvaloniaResource` items in the `Avonic.Sample` assembly, which the APK carries as it is, so the same
`avares://` URIs work. The head calls `WithRoboto()` in `CustomizeAppBuilder`, as the desktop's `Program` does. Checked by building the APK
and finding the assembly in it, and on the device by the checklist.

## 7. The style benchmark on a phone

The benchmark in `tests/.../StyleCostBenchmark.cs` runs headlessly. A phone can't run a test, so the sample gets a runner with the same
shape: 5,000 elements at a depth of 20, `TextBlock` then `Border`, 4 warm-up rounds and 25 measured, alternating without and with Avonic's
theme, the median in µs per element, written as a row for the table. The tree, the counts, the median and the row move into the sample's
`Diagnostics/`, and the test uses them, so the two can't drift.

What differs, and is said beside any row it gives: the runner attaches into the live app, under the sample's `SimpleTheme`, and lays out,
where the test shows a headless window per round under the test app's styles. So **a phone's row is compared with a desktop row from the same
runner**, not with the headless test's. The device page runs it a round at a time on the dispatcher, so that the app stays responsive and
Android doesn't call it not responding, shows the row, and writes it to the log. In a Debug build, or with Avalonia's diagnostics on, it says
the row isn't for the table. No numbers go in the docs until it's been run.

## 8. The head

`samples/Avonic.Sample.Android`: `net10.0-android`, minimum API 24, package id `io.github.wdcossey.avonic.sample`, app name "Avonic", **no
icon** (Android's default), and no splash screen. An AppCompat theme with no action bar, as `AvaloniaActivity` is an `AppCompatActivity`.
`MainActivity` and the `Application` class, and nothing else. A Debug build turns Avalonia's diagnostics on, as the desktop's does, for the
performance line.

## Tests

Headless:

- back on a page shows the list and is handled; on the list it isn't handled; a handler that handled it first keeps the page, including one
  added after the shell attached, as `OverlayHost`'s is; and a presented overlay dismisses on back while the page stays;
- the safe-area mode fills and clears the four keys, and an item's padding follows them;
- the benchmark runner, with small counts, gives a row of the table's shape, and puts the theme back where it was, also when a round throws;
- the existing window test of the performance line, through the shell.

The breakage run covers the shell's back, the safe-area mode and the runner.

## Not done here

- Running anything on a device: another agent may be using the phone. [README.md](README.md) says what to run and what to look at.
- An iOS head.
- A change to `OverlayHost.TopLevelOf` for `IActivityApplicationLifetime`. Reported instead.
