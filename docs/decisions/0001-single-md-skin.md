# ADR 0001 — One visual skin, recreating Ionic's `md` mode

**Date:** October 2026 · **Status:** accepted

## Context

Ionic ships every component with two visual modes (`ios`, `md`) and a `mode` property.
v1 of Avonic chose *no modes* and an invented shadcn-style theme, which made a faithful
recreation impossible by design (see `docs/LESSONS_LEARNED.md` §1.4).

## Decision

- **Behaviour** is recreated faithfully from Ionic's implementation, mobile/tablet first.
- There is **one** visual skin, and it is a recreation of Ionic's **`md` mode** — not an
  invented design language.
- **Every** Ionic CSS custom property is exposed as an `Avonic.*` resource key (rule in
  `CLAUDE.md`) so end users can restyle without forking templates.
- There is **no `mode` property** in v2.0. Token names are chosen so an `ios` skin could be
  added later as a second theme without renaming anything.

## Consequences

- Fidelity is measured on API surface and behaviour (`tools/ionic-api.js` checklist +
  per-control `COMPARISON.md`), and on visual match to Ionic **md**.
- Theme work is 1×, not 2×.
- iOS-flavoured apps are a theming exercise for the consumer or a future `Avonic.Themes.Ios`.
