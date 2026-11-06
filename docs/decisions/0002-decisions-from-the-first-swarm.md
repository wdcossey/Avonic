# ADR 0002 — Decisions from the first swarm

**Date:** November 2026 · **Status:** accepted

## Context

Five agents built `ion-note`, `ion-text`, the `ion-list` and `ion-card` families, `ion-content` and `ion-button` in parallel, on
top of `ion-label`. Each made conservative choices and listed them for the human, along with questions open since `ion-item`.
The human ruled on all of them. The reasoning for each control's own choices is in its PR and `PLAN.md`. This records the ones
that future controls inherit.

## Decision

**Faithful, with deviations allowed when they're explained.** The human's words: "We are attempting to create a faithful
recreation of Ionic using Avalonia, unfortunately we aren't afforded all the same techniques we'd get in html/css/js. So we'd
need to make some allowances for deviations and where that happens we can document and explain why." So:

- A deviation Avalonia forces is fine if it's recorded with its measurement in `COMPARISON.md` or the plan.
- Differences in how the two render that are out of our control are best effort, not something to chase with fragile workarounds.
- `Status: done` can stand with known gaps, if they're recorded. It isn't a licence to skip measuring, or to deviate where
  Avalonia could do it.

**Rules:**

| Question | Decision |
|---|---|
| Rule 2 said one control per PR, and issues ask for families | One control or family per PR |
| Visual constants with no CSS variable | A literal in the theme, so that it can be restyled. Only what a control's code needs is a constant in `Avonic.Components` |
| `name` props, as `StyledElement.Name` exists | `<Control>Name`: `IconName`, and `SpinnerName` for #15 |
| A control holding many children, with no slot in `core.json` | `Items`, not `Content` |
| A string-union prop with the same values as another control's | Its own enum, as the mapping rule is mechanical |
| `IItemControl` | Stays, as it replaces Ionic's tag-name queries |
| `Command` on button-like controls | An extension, following Avalonia's `Button`. Not `HotKey`, `IsDefault`, `IsCancel` or `ClickMode` |

**Page-level defaults.** `ion-app` was out of scope. Reading Ionic's source showed it does real mobile work, so it's in scope, as
[#65](https://github.com/wdcossey/Avonic/issues/65), for just that: hardware back, keyboard assist, status tap and `SetFocus`. An app
shouldn't have to use it to get working controls. These are decided, and aren't its job:

| Question | Decision |
|---|---|
| Default text size outside a host | Avalonia's. A library shouldn't change an app's default by being added. Documented as a difference, where a web page's is 16 |
| Global `h1`-`h6` typography | An opt-in file the app merges, as Ionic's `typography.css` is, so that it costs nothing unless asked for. See [#70](https://github.com/wdcossey/Avonic/issues/70) |
| `Avonic.SafeArea*` | The app fills them, with an example in [`ion-content`'s docs](../components/ion-content/README.md). They default to 0, as Avalonia pads the root view itself |

**How the work runs.** A real window found a bug that 1,486 tests had missed, and reviews and deliberate breakage found real bugs
right through, but they aren't free:

| Question | Decision |
|---|---|
| Batch size | Two or three components at a time, not five |
| Checks | Two high-effort reviews and full deliberate breakage for new controls and behaviour changes. One review for docs and samples |
| Real window | Run on each batch before calling it ready to merge, asking first, as it takes over the PC |
| Style budget | A limit in CI, meant to be raised on purpose, and a checkpoint at 600 alternatives. We expect to reach it, and then stop, measure on a real phone, and decide. See [performance](../infra/performance/README.md) |
| A real phone | An Android project once P1 is finished, [#66](https://github.com/wdcossey/Avonic/issues/66) |
| Screenshots | In the description of any PR that changes a component or anything visual |
| Avalonia's bugs | [Listed](../infra/avalonia-issues.md) for submitting later, not reported as they're found |

## Consequences

- A control's review can accept a recorded gap, and the docs have to say what it is. Gaps that would be noticed need an issue, so
  that they don't only live in a PR.
- A decision that's deferred to something needs that thing to exist. Three of these were deferred to `ion-app` while it was out of
  scope, and would never have come up.
