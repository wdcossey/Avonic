# The component loop

Avonic v2 is built **one control per PR**. The loop below is the only way a control enters
the codebase. No step is skipped, a session works on one control at a time, and nothing is
"done" until the last step is signed off.

```
 ┌────────┐   ┌───────┐   ┌─────────┐   ┌────────┐   ┌──────────┐
 │  PLAN  │ → │ BUILD │ → │ COMPARE │ → │ REVIEW │ → │ DOCUMENT │ → next control
 └────────┘   └───────┘   └─────────┘   └────────┘   └──────────┘
```

## 0. Working from a GitHub issue

Every control and piece of infrastructure is a GitHub issue labelled `P0`…`P4` (priority tier)
and `component` or `infra`. Issues list what blocks them; start one only when its blockers are
closed. A fresh session needs nothing but the repo and the issue:

1. Branch `feat/<ion-tag>` (or `infra/<name>`) from `v2`; `npm install`; `dotnet build Avonic.sln`.
2. Run the loop below. The plan is **not** gated on a human mid-flight: write `PLAN.md`, then
   continue straight through Build → Compare → Review → Document.
3. Open a PR into `v2` titled `feat: <ion-tag>` using the PR template. CI must be green.
   The human review happens **once, at the PR**: plan, comparison, diff. Only the PR merge marks
   the control done: its README's `Status:` line lands and the issue closes with `Closes #N`.
   A control's PR doesn't edit the `CLAUDE.md` or `docs/ROADMAP.md` tables; see
   [Rolling up the tables](#rolling-up-the-tables).

Shared files are auto-discovered, so a control adds its own files and never edits another
control's. Keep it that way. Templates and tokens are found by the globs in each project file
(`Controls/**/*.axaml` in Components; `Tokens/**/*.axaml`, `Controls/**/*.axaml` and `Styles/**/*.axaml` in Themes).
Sample pages are found by `[SamplePage("<ion-tag>", Order = <issue number>)]`. See
[auto-discovery](infra/auto-discovery/README.md).

## 1. Plan

Produce `docs/components/<ion-tag>/PLAN.md` **before any code**. It is reviewed at the PR
together with the comparison, so write it to be read — decisions and omissions must be explicit.

1. Run `node tools/ionic-api.js <ion-tag>` and paste the checklist in. Every row is either
   ticked, or has a one-line reason it is omitted (`out of scope: router`, `not applicable:
   HTML form`). Unexplained gaps are a planning failure.
2. Read the implementation: `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js`.
   List the **behaviours** (not props) that the control must reproduce — e.g. *"ion-item forwards
   a tap on its padding to the first interactive child"*, *"ion-toggle flips on drag past 10px
   and fires ionChange on each flip"*. These become the test list.
3. Read `<name>.md.css`. List the visual states and which resource keys drive them.
4. Note Avonic extensions (touch-target enforcement, etc.) in a separate section. They never
   mix with the Ionic-mapped API.
5. Note dependencies on other controls and whether they exist yet.

## 2. Build

One PR, one control, all of:

| Deliverable | Where |
|---|---|
| Control class — behaviour, properties, events, pseudoclasses, **no visual opinion** | `src/Avonic.Components/Controls/<Name>/` |
| Functional stub template (structure only) | same folder, `<Name>.axaml` |
| Styled template using only resource keys — when the control has a visual (a control whose look Ionic hard-codes with no CSS variable has none; say so in the plan) | `src/Avonic.Themes/Controls/<Name>.axaml` |
| Resource keys — one per Ionic CSS var, named by the rule in `CLAUDE.md` | `src/Avonic.Themes/Tokens/<Name>.axaml` |
| `::slotted` rules, for this control's content or how it sits in another control | `src/Avonic.Themes/Styles/<Name>.axaml` |
| `Design.PreviewWith` block | in the Themes template |
| Behaviour tests — one per behaviour listed in the plan; each must **fail** if the behaviour is removed | `tests/Avonic.Components.Tests/Controls/<Name>Tests.cs` |
| Sample page | `samples/Avonic.Sample/Pages/<Name>Page.axaml` |

Test fixtures that exist for this: `TestClock` replaces Avonic's clock and defer scheduler so
timelines are asserted to the millisecond; `Scene` shows a headless window and closes it when the
test ends; `window.CaptureRenderedFrame()` gives real pixels (Skia) for the compare step.

Rules that are not negotiable:
- `dotnet build` and `dotnet test` are green **before every commit**. No "it's just XAML".
- A declared property works. If it cannot be made to work yet, it is not declared.
- Icons come from ionicons via `AvonicIcon`. No emoji, no Unicode glyphs.
- No hex colours or `Brushes.*` in Themes outside `Tokens/`.

## 3. Compare

Side by side with the real web component in **md** mode at a phone viewport.
`node tools/ionic-ref/render.js <ion-tag> [--html …] [--tap]` produces the Ionic side:
screenshots (and press/release frames), the shadow DOM tree, bounding boxes, computed layout
and the resolved value of every CSS variable, under `docs/components/<ion-tag>/ref/`.

1. Every behaviour in the plan is exercised on both and the result recorded in
   `docs/components/<ion-tag>/COMPARISON.md` — match / deliberate difference / gap.
2. Visual: the Avonic rendering (headless screenshot from the test harness or the sample app)
   is placed next to the Ionic `ref/` images. Differences are listed and either fixed or
   recorded as deliberate.
3. A gap found here goes back to **Build**. If it can't be closed, because Avalonia forces it or it's out of the control's reach, it can
   stand, recorded in `COMPARISON.md` and the README's Known gaps with its measurement and why. `Status: done` means that's been
   done, not that there are no gaps ([ADR 0002](decisions/0002-decisions-from-the-first-swarm.md)). One an app would notice gets an issue.

## 4. Review

- `/code-review` on the change, high effort, **twice** — the second pass on the fixed diff.
  Findings fixed or explicitly waived in the PR description. Docs and samples alone get one pass.
- **Prove the tests can fail,** with [`tools/breakage`](../tools/breakage/run.js). A new test for a bug has to fail on that bug.
- **A PR that changes a component or anything visual has a screenshot** in its description: Ionic's frame beside Avonic's, through
  URLs pinned to the commit, saying whether it's headless or a real window.
- **Before calling a batch ready to merge,** run [the real window](../tools/real-window/README.md) and look at its frames. It takes
  over the PC for about a minute, so ask first. It found a bug that 1,486 tests had missed.
- If `StyleBudgetTests` goes over, the failure lists the largest rules. See whether the commas can go, or raise the limit
  and say so in the PR, with the new total. See [performance](infra/performance/README.md).
- A human reads `PLAN.md`, `COMPARISON.md` and the diff at the PR, and merges or requests changes.

## 5. Document

`docs/components/<ion-tag>/README.md`: usage, the final ticked checklist, behaviours,
extensions, resource keys. Directly under its heading goes the status line, written only now:

```markdown
Status: done · [#N](https://github.com/wdcossey/Avonic/issues/N)
```

---

**Then** that session's next control starts. Not before.

## Rolling up the tables

The component table in `CLAUDE.md` and the Done table in `docs/ROADMAP.md` are shared files, so a
control's PR leaves them alone and parallel PRs can't conflict on them. When every issue in a
priority tier is closed, or sooner if the human asks, one session raises a single PR that adds the
finished rows to both tables. Only one roll-up PR is open at a time.

The tables lag between roll-ups, so don't read a missing row as "not built". The current record is
the status lines, which `git grep -l "^Status: done" docs/components docs/infra` lists, and blockers
are checked against closed issues.
