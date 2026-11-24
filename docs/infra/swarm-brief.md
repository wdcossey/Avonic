# Brief for each control in a batch

For two or three agents building controls at once, each in its own git worktree. Give each one this, and its issue. The human's
decision on batch size and checks is in [ADR 0002](../decisions/0002-decisions-from-the-first-swarm.md). It's kept up to date as
mistakes are found, so that they aren't repeated.

You're building an Avonic control in parallel with others. Avonic recreates Ionic's components on Avalonia 12.1.4, md mode. Read
`CLAUDE.md` and `docs/PROCESS.md` first, and follow PROCESS's whole loop: Plan → Build → Compare → Review → Document → PR. The human
merges. You never merge.

Your worked examples are `docs/components/ion-label/` (a child that depends on its host), `docs/components/ion-item/` (a host) and
`docs/components/ion-button/` (a control with states). Read their plans. The patterns in them are settled, so reuse them.

## Setup

1. You're in a worktree. `git switch -c feat/<ion-tag> origin/main`.
2. `npm ci`, then `dotnet build Avonic.sln`.
3. `node tools/ionic-api.js <ion-tag>` is your spec. The implementation is in `node_modules/@ionic/core/dist/collection/components/<name>/`.

## Rules because others are working at the same time

The point is that the PRs can merge in any order without a conflict.

- **Don't edit shared files:** `CLAUDE.md`, `docs/PROCESS.md`, `docs/ROADMAP.md`, `tools/ionic-ref/render.js`, `src/Avonic.Components/Behaviors/*`,
  `AvonicConfig.cs`, `AvonicTheme.cs`, any `.csproj`, `Tokens/Global.axaml`, `Tokens/Colors.axaml`, or any other control's files, including
  `AvonicItem` and `AvonicLabel`.
- **If you need one changed,** don't. Do it in your own files if you can, and list what you'd change under a **"Proposed for shared
  files"** heading in the PR, with the exact text. That includes new mapping rows for `CLAUDE.md`. A new helper goes in your control's
  folder, such as a converter on the control.
- **Your files:** `src/Avonic.Components/Controls/<Name>/`, `src/Avonic.Themes/Controls/<Name>.axaml`, `src/Avonic.Themes/Tokens/<Name>.axaml`,
  `src/Avonic.Themes/Styles/<Name>.axaml`, `samples/Avonic.Sample/Pages/<Name>Page.axaml*` with `[SamplePage("<ion-tag>", Order = <issue number>)]`,
  `tests/.../Controls/Avonic<Name>*Tests.cs`, `docs/components/<ion-tag>/`. These are auto-discovered.
- **The style budget's limits** are in a shared file. If you go over, raise them in your PR and say so, because they're meant to be raised.
  Say the new total in the PR, and expect a conflict to resolve. **Don't expect one when two PRs raise it by the same amount:** both
  change the same line to the same text, so git merges them "cleanly" and one raise is lost (#136 and #138 both set 283 → 284, where
  the total was 285). After merging `main`, add your raise to the total `main` now has, and say both in the PR.
- **The reference render:** use `node tools/ionic-ref/render.js <ion-tag> --bare --html '...'`, and record the exact command at the top of
  `COMPARISON.md`. Don't add to `DEFAULT_HTML`.
- **Use only your own temp folder,** for scripts, renders, clean clones and breakage copies. Whoever starts the batch names one for
  each agent, after its issue (`<tag>-<issue>-agent`); if you weren't given one, make one by that name in your session's temp
  directory. Two agents that shared one overwrote each other's clean clones.
- **Stage only your own files.** `.idea/` stays untracked. Never `git add -A` from the root without checking.
- **Don't open a real window.** It takes over the human's PC, and whoever runs the batch does that with `tools/real-window`. Compare with headless
  capture only, and say "not tried in a real window" in the PR.
- **Don't kill processes you didn't start.** The machine is also running the human's other projects. Keep builds polite: `-m:8` is plenty.
- **Don't start a long command in the background and wait on it.** Run builds, tests and breakage runs in the foreground with a
  timeout. An agent waited 9 hours on a background command that never reported back (2026-11-11).
- **Don't pipe a script to `python -` or `node -` through a heredoc** with quotes nested inside: the shell's quoting breaks it, or it
  waits on input. Write the script to a file in your temp folder and run the file. Make edits with the Edit tool.
- **Don't ask the human anything.** Choose the conservative option, and put it under **"For you to decide"** in the PR.
- **Never post to a repository we don't own:** no issues, pull requests, comments, discussions or reactions in
  AvaloniaUI/Avalonia, ionic-team/ionic-framework, ionic-team/ionicons or any other. Some projects don't want issues raised by
  an AI. Write what you find upstream in our own docs (`docs/infra/avalonia-issues.md`; for Ionic, `docs/infra/ionic-issues.md`,
  [#221](https://github.com/wdcossey/Avonic/issues/221)), with the evidence, and the human files it by hand if they choose.
  Issues in wdcossey/Avonic are fine.

## How to get this right

These are mistakes this project has already made, so don't repeat them.

- **Measure real Ionic before writing behaviour.** Do it in Chromium, with `playwright-core`, as `tools/ionic-ref/render.js` does. Keep throwaway
  scripts in your own temp folder, not in the repo. Wrap the scene in `ion-app`, with `style="position:static;display:block;contain:none;overflow:visible"`:
  without `ion-app` tap-click and focus-visible never start, and collapsing it stops pointer hits. Read the CSS as well as the JS: rules can be dead,
  such as `item-has-focus`, which no Ionic 8 control raises. `grep` for who emits what.
- **The reference render has Ionic's typography on.** `render.js` loads `ionic.bundle.css`, which includes `typography.css`, so an
  `h1` to `h6` in a reference frame has its sizes, margins and line height of 1.2. Avonic's are opt-in: a capture test with headings
  merges `Css/Typography.axaml` to match, as the label's and the card's do (`docs/infra/typography/README.md`).
- **Numbers in docs must be measured.** If you read a number off an image, it's a guess. If you benchmark, alternate with and without, because warm-up
  makes the first one look far worse. An earlier "doubling" here was really 13%.
- **The compare step finds real bugs.** Look at Ionic and Avonic side by side as an image, every time, with `node tools/ionic-ref/side.js a.png b.png out.png height`.
  Capture with `AVONIC_CAPTURE_DIR=<dir> dotnet test`, and name your frame `avonic-<tag>-page.png`, because capture tests share the folder. Save it to
  `docs/components/<ion-tag>/compare/avonic-page.png`.
- **Prove the tests can fail.** Break every behaviour on purpose, one at a time, with `tools/breakage/run.js`, which says how. A miss is either a weak test or a
  rule that does nothing, so work out which. A rule that does nothing gets removed. Report the true numbers. **A new test for a bug has to fail on the bug.** One
  here passed with the bug in place, because its test element had a size of its own.
- **`--no-build` runs stale binaries.** Build first, and test only if it passed.
- **Test in the layouts an app really uses,** not only a fixed-size box. A scroller measures with no height and arranges at the viewport's, which hid an invisible
  button from 1,486 tests.
- **Review findings can be wrong.** Check each against what Ionic really does before acting, and say so in the PR when one is wrong.
- **Don't `.Result` or `.Wait()` a task in a test that may not finish:** a deliberate break then hangs the breakage run instead of
  failing (#227, #241). Await it, or assert `IsCompleted` first.
- **Headless popups are in the window's overlay layer,** so code for a control in a real popup window (another `TopLevel`) can't be
  tested headless. Say so in the PR (#227, #231).
- **An overlay reuses what the overlays share** before writing its own: `Overlays/OverlayHandler` (a button's handler, the backdrop's
  opacity), `ModalPanel`'s and `AlertPanel.Length`'s CSS sizes, `AlertClasses`. Three overlays each copied `BackdropOpacity()`.
- **A `drive.ps1` section's real click lands on whatever is on top.** Click only what the top overlay shows: a button under a
  full-screen modal is covered, and the click reached the modal (#228).
- **Ionic's behaviour for your control can live in another control's `.js`:** the split pane does nothing by itself, as `menu.js`
  reads it and `menu.md.css` lays the side out (#263). Say so in the plan, and if the other control has to change, put it in your
  PR with a note at the top naming its files, so that it can be split off.
- **Read the automation peers before writing a `drive.ps1` check.** A check can only find what a peer exposes, by its name and
  control type: the single grid's section looked for a month heading's text, which isn't in automation (a month is read by its
  days, and the month-year button's name is `Show year picker`, #258), and the months-either-side section's first scroller was
  the page's, not the calendar's (#259). Find an element from one you know is there, and walk to it.
- **A UIA search while a page leaves the tree can fail with E_FAIL,** Avalonia's (#254, `docs/infra/avalonia-issues.md`): a
  section that polls during a transition retries once, as `ion-nav`'s does; one that waits for the transition to end doesn't meet it.
- **Culture data differs by platform.** Linux's ICU gives other short day names, AM/PM text and spaces than Windows' (#246, #251):
  compare with Chromium's strings where they were measured (Windows), and test the rest everywhere. CI's Ubuntu job finds it.
- **On the Pixel, keep the screen on for a timed run:** a locked screen moves the app to the little cores and starves
  `Background` work, which made every page 7 to 10 times slower (#258). Each round's check line reports the cpuset; read it.
- **`role`, `aria-*` and `tabindex` in the `.js` are behaviours:** map each by `docs/infra/accessibility/README.md` (landmarks, live regions, names, help text, headings), at a priority an app's own value beats, or record the gap.

### Avalonia facts already learned

- **Every global rule is checked against every element in the app,** even a rule that can't match that type, at about 30 ns for each alternative between commas. So
  **every `<Style>` in `Styles/` has a cost, and every comma multiplies it.** Prefer a `ControlTheme` where a rule only concerns its own control and template. Nesting
  doesn't help. See `docs/infra/performance/README.md`.
- **Never write a descendant selector whose target is a `TextBlock`, or any other common type.** It's about 385 ns at a depth of 20. A type of yours after it stops that:
  `c|Host c|Label > TextBlock` only looks further for a label's child. `StyleBudgetTests` fails on it.
- **`c|Host ^` in a `ControlTheme` compiles, ignores `c|Host`, and applies everywhere.** Host-dependent rules go in `Styles/`.
- **A pseudoclass written before a `:not()` that holds a combinator is dropped:** `c|Foo:nth-last-child(1):not(c|Bar > c|Foo)` matches
  every `Foo`. Write the pseudoclass after it, `c|Foo:not(c|Bar > c|Foo):nth-last-child(1)`, and test that it matches only what it
  should. See `docs/infra/avalonia-issues.md`.
- **Specificity:** a rule with a pseudoclass or class beats one without, then later beats earlier. Global `Styles` beat a `ControlTheme`. Files in `Styles/` merge in ordinal
  order, so don't rely on being after another control's file.
- **Template-set values beat plain styles,** but pseudoclass styles beat the template.
- **`::slotted`** needs a `<Styles>` file in `Styles/`, because a `ControlTheme` can't reach slotted content. `>` follows the logical parent.
- **Hosts:** several slots means `TemplatedControl`, with plain `ContentPresenter`s, so slotted content's logical parent is the presenter. One slot can be `ContentControl`,
  which makes the content a logical child of the host.
- **A slot holds one child,** so a panel directly in a slot stands for the slot: `c|Host > TextBlock.p, c|Host > :is(Panel) > TextBlock.p`.
- **`color: inherit`** is `$parent[Control].(TextElement.Foreground)` as the last value of a `Css.Var`. `$parent.` alone doesn't compile.
- **`--foo` falling back:** `<b:CssUnset x:Key="..." />` and `Css.Var`. `currentColor` is the host's `Foreground` as the last value.
- **`ContentControl`, `TemplatedControl` and `TextBlock` clip by default,** where CSS doesn't, and layout tests don't show it, so check by hit test or pixel. A `TextBlock` with a small
  `LineHeight` loses its descenders.
- **A child measured at zero stays at zero,** unless it has a size of its own. Measure children with what's really available, even if the control reports zero.
- **`TextBlock.TextWrapping`, `TextTrimming` and `LineHeight` inherit,** so set them on the host, and they follow. `LineHeight` inherits as a size.
- **`translateY(%)`** is `Css.TranslateY`, `Css.Scale` and `Css.TranslateScale` on `Bounds`. Ease the two values, not the transform.
- **Transitions run on real time in headless tests.** Set `control.Transitions = []` to test an end value, or use the transition's start to prove it eases.
- **Headless hit tests read the last rendered scene,** so render first.
- **`outline: none`** is `FocusAdorner` `{x:Null}`, and a test has to check the `AdornerLayer` with `SimpleTheme` underneath, because the property reads null anyway. See `B16` in `AvonicItemTests`.
- **Interaction:** `Activatable` is tap-click, with `:activated` and `:ion-hover`. `FocusVisible.IsEnabled` is focus-visible, with `:ion-focused`. A `Button`-like control clicks on release, every
  time: a second quick press raises `DoubleTapped` and no `Tapped`, so see how `AvonicButton` handles it. `Gestures` is internal; use `TappedEvent`.
- **`FocusVisible` means `ion-focusable` to an item around it:** the item then reacts to focus too.
- **A `ScrollGestureRecognizer`** given `Offset`, `Viewport` and `Extent` gives up at the limit and passes the drag to an outer scroller.
- **A `Control`'s default automation peer** is `NoneAutomationPeer`, which isn't a control or content element.
- **Behaviour goes in the control at style priority,** so it survives a replaced theme and an app's value wins: `SetValue(prop, value, BindingPriority.Style)`, then dispose the frame.
- **An XML comment can't contain `--`,** so write CSS variables without the dashes in `.axaml` comments.
- **`x:Static` can't take `x:Key`. `TemplatedParent` isn't allowed under comma selectors:** use `$parent[...]`. A transition's property in a dictionary needs `{x:Static ...}`.
- **The sample's base theme is `SimpleTheme`,** and `TestApp` has none, so anything from a base theme only shows up in a test that adds it.
- **A `ContentPresenter` with a `ContentTemplate` builds from it even when the content is a control.** A template for another type, such
  as strings, then shows the control's type name in a `TextBlock`. An `ItemsControl` that wraps controls in containers has to clear
  the template for them. See `AvonicChip.PrepareContainerForItemOverride`.
- **Of two values at one priority, the later wins.** A style-priority frame made when a state turns on beats an app's style that was
  already there. Bind once, in the constructor, and push the value through it: `Behaviors/StyleValue<T>`.
- **An invisible control isn't measured, so its template isn't applied.** Anything that starts on attach and needs the template has to
  wait for it to be shown.
- **A slotted control's `Margin` rule goes in the host's `Styles/` file,** after the host's rule for any child, with no activator.
  An activator would beat an app's plain style too, and a plain rule in the control's own file wins or loses by merge order.
- **A `MultiBinding` set as a property element on a part inside a `ControlTemplate` is a local value,** so no state's style can
  beat it. Put it in a `^ /template/ #PART_X` style setter. Measured with `GetDiagnostic`: `LocalValue`.
- **An `ItemsControl`'s `OnKeyDown` handles the arrows** before an instance handler sees them: override `OnKeyDown`.
- **An `ItemsControl`'s child that is its own container inherits from the control,** its logical parent, not from the template part
  it sits in, so a part's `color` rule (`.button-native { color }`) goes on the host's `Foreground`.
- **`AvonicConfig.Animated` isn't a master switch.** Ionic's `animated` reaches tap-click and the transitions of the accordion,
  menu, nav, router outlet, overlays, searchbar and skeleton text and, since 8.8.19, the datetime's month scroll only. A control
  Ionic doesn't make read it ignores it here too.
- **A `VirtualizingStackPanel` makes a row's content while it measures the row, before `IndexFromContainer` knows the row,** and
  `ContainerFromIndex` reads the last pass's rows until the pass ends. Read a row's index and its neighbours from `LayoutUpdated`.
  The panel's children are only what's realised, so a rule about the row before (`.foo + .bar`) can't read `Children`: see `AvonicList`.
- **A `Border` draws a `BoxShadow` only with an even `BorderThickness` and the default `BackgroundSizing`,** and its blur is narrower
  than CSS's. Write Ionic's CSS as it is, through `{b:CssShadow '0 0 10px rgba(0, 0, 0, 0.4)'}` or `<b:CssShadow x:Key="…">…</b:CssShadow>`,
  never Avalonia's own `BoxShadows` numbers (`docs/infra/box-shadow/README.md`). Its offset isn't scaled at 2× (`avalonia-issues.md`).
- **An implicit `ControlTheme` merged into resources at runtime doesn't restyle a control already shown,** though `DynamicResource`s
  follow. Merge it before the control is attached.
- **A finger's `PointerExited` comes at its release,** where Chromium's compatibility mouse events don't leave, so a hover behaviour
  ignores a touch's leave (`AvonicPopover`'s hover trigger, #43).
- **Avalonia's hit test finds the deepest visual,** so Ionic's `elementFromPoint() === el` ("the element itself, not a child") has no
  equivalent: test the element's subtree (#43).
- **A brush made in a template's `Resources` with a `DynamicResource` colour keeps the control alive** through the theme's
  dictionary: every datetime that drew its calendar leaked (#258). Use a `StaticResource` of a top-level brush there.
- **A `DispatcherTimer` past `int.MaxValue` ms throws,** where a browser's `setTimeout` fires at once (#245, #249): guard a duration.
- **On Android, `Background` work runs only when the UI thread is idle** (`AndroidDispatcherImpl`'s idle handler), and
  `AvonicConfig.Schedule` and the datetime's scroll rest are `Background` timers (#256, #260).
- **Android's runtime is Mono, whose collector scans stacks conservatively:** a leak check that collects on the same stack right
  after the work can see the object alive. Collect from fresh turns of the dispatcher (#258).

## Review and PR

- **`/code-review` twice, at the levels in `docs/PROCESS.md` §4:** pass 1 `high origin/main...<branch> --max-findings all` (`medium` for a small bug fix; always `high` on code many controls share, as listed there), pass 2 on the fixed code `medium origin/main...<branch> --max-findings all` (a docs or chore PR: one pass at `medium`). Always write the level and name the branch, never `HEAD`. Findings are fixed or waived with a reason; the PR records each pass's level and tokens ("not measured" if you can't see them). Don't run extra passes: the level trial (#232) is the coordinator's.
- **No conflict markers:** after merging or rebasing on `origin/main`, and before your last push, `git grep -n -I -E '^(<<<<<<<|>>>>>>>)'` prints nothing. CI doesn't catch them in a doc.
- **The gate before every commit:** `dotnet build Avonic.sln && dotnet test Avonic.sln`.
- **A clean clone:** before the PR, clone your branch to your temp folder, `npm ci`, and run a Release build and test. Nothing may be skipped.
- **Commits** end with exactly this line: `Co-Authored-By: <the model's name> <noreply@anthropic.com>`, as the session's instructions give it.
- **Push, and open the PR into `main`,** titled `feat: <ion-tag>`, using `.github/PULL_REQUEST_TEMPLATE.md`, with `Closes #<issue>`, and **a screenshot:** Ionic's frame beside Avonic's, through URLs pinned to your commit.
- **The README's `Status:` line** goes in with the PR. Don't edit the `CLAUDE.md` or `ROADMAP.md` tables, whatever the issue says. PROCESS wins.
- **CI must be green on your exact commit.** Wait on `gh run list --commit <sha>`, not `gh pr checks` without one, which can report an old run. Fix it if it's red.
- **Be honest in the PR:** the true test count, what wasn't tried, every known gap, and every deliberate difference with its measurement. Don't tick a checklist item that isn't true.

## What to report back

Keep it short: the PR link, the exact commit, CI's result, the test count, the deliberate-breakage numbers, each review pass's level and tokens, tokens used if you can see them, known gaps, "For you to decide", and
"Proposed for shared files".
