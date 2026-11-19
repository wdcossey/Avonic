# ADR 0005 — Don't copy an Ionic bug

**Date:** November 2026 · **Status:** accepted

## Context

CLAUDE.md's rule 8 and [ADR 0002](0002-decisions-from-the-first-swarm.md) allow a deviation from Ionic when Avalonia forces it.
Two families found behaviour that Avalonia could copy, but that is a bug in Ionic:

- `ion-menu` ([#244](https://github.com/wdcossey/Avonic/pull/244)): two menus opened in the same tick both open, by a race in
  the controller (measured), and three paths leave a menu "animating" for good, after which no menu opens.
- `ion-toast` ([#245](https://github.com/wdcossey/Avonic/pull/245)): Tab never reaches a toast's buttons, as its host has
  `tabindex="-1"`, so a keyboard user can't reach Undo; and while a toast shows, Escape and the hardware back button dismiss
  nothing, not even an alert under it (both measured in Chromium).

Each PR recorded them in `docs/infra/ionic-issues.md` and did what Ionic evidently means instead. The human decided on
2026-11-19 to keep that.

## Decision

**Where Ionic's behaviour is a bug, Avonic does what Ionic means, not what it ships.** A bug is behaviour that goes against
Ionic's own code comments or documentation, leaves the component unusable (stuck, unreachable by keyboard, a key that does
nothing), or is a race. A difference of taste, or what Avalonia's own controls happen to do, isn't one: follow Ionic.

A deviation taken this way:

- **is measured** in Chromium where it can be, or read from Ionic's code, and says which;
- **is recorded** in `docs/infra/ionic-issues.md` (for the human to report; agents never file upstream), and as a deliberate
  difference in the control's `COMPARISON.md` and README;
- **has a test** of what Avonic does;
- **is revisited at a migration**: if Ionic fixes it, Avonic follows Ionic's fix where it differs.

## Consequences

- Rule 8 reads: a deviation is allowed when Avalonia forces it, or when Ionic's behaviour is a bug by this ADR.
- The menu's and the toast's deliberate differences stand as merged.
