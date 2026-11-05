# Roadmap

Ionic has **95** components (`node tools/ionic-api.js --list`). Avonic v2 targets **82** of them
as **47 GitHub issues** (families that can only be built and compared together share one issue),
plus the shared infrastructure they stand on. This file is the map; the issues are the work.
`CLAUDE.md` lists only what *exists*.

Process per issue: `docs/PROCESS.md` §0. Start an issue only when its blockers are merged.

## Priority tiers

| Tier | Meaning | Issues |
|---|---|---|
| **P0** | Foundation — sequential, must land before any parallel work | [#1](https://github.com/wdcossey/Avonic/issues/1) swarm-safe repo · [#2](https://github.com/wdcossey/Avonic/issues/2) colour system · [#3](https://github.com/wdcossey/Avonic/issues/3) ion-icon |
| **P1** | Foundations the rest sits on — parallel once P0 is merged | [#4](https://github.com/wdcossey/Avonic/issues/4) gesture utility · [#5](https://github.com/wdcossey/Avonic/issues/5) overlay system + backdrop · [#6](https://github.com/wdcossey/Avonic/issues/6) item · [#7](https://github.com/wdcossey/Avonic/issues/7) label · [#8](https://github.com/wdcossey/Avonic/issues/8) note + text · [#9](https://github.com/wdcossey/Avonic/issues/9) list family · [#10](https://github.com/wdcossey/Avonic/issues/10) button · [#11](https://github.com/wdcossey/Avonic/issues/11) content · [#12](https://github.com/wdcossey/Avonic/issues/12) toolbar family · [#13](https://github.com/wdcossey/Avonic/issues/13) card family · [#14](https://github.com/wdcossey/Avonic/issues/14) grid family · [#15](https://github.com/wdcossey/Avonic/issues/15) spinner · [#16](https://github.com/wdcossey/Avonic/issues/16) badge/avatar/thumbnail · [#17](https://github.com/wdcossey/Avonic/issues/17) img + skeleton-text · [#18](https://github.com/wdcossey/Avonic/issues/18) progress-bar · [#19](https://github.com/wdcossey/Avonic/issues/19) chip |
| **P2** | Form controls and composites — parallel once their blockers are merged | [#20](https://github.com/wdcossey/Avonic/issues/20) checkbox · [#21](https://github.com/wdcossey/Avonic/issues/21) toggle · [#22](https://github.com/wdcossey/Avonic/issues/22) radio · [#23](https://github.com/wdcossey/Avonic/issues/23) range · [#24](https://github.com/wdcossey/Avonic/issues/24) input (+ password toggle) · [#25](https://github.com/wdcossey/Avonic/issues/25) textarea · [#26](https://github.com/wdcossey/Avonic/issues/26) searchbar · [#27](https://github.com/wdcossey/Avonic/issues/27) input-otp · [#28](https://github.com/wdcossey/Avonic/issues/28) segment family · [#29](https://github.com/wdcossey/Avonic/issues/29) tabs family · [#30](https://github.com/wdcossey/Avonic/issues/30) fab family · [#31](https://github.com/wdcossey/Avonic/issues/31) item-sliding family · [#32](https://github.com/wdcossey/Avonic/issues/32) accordion · [#33](https://github.com/wdcossey/Avonic/issues/33) breadcrumbs · [#34](https://github.com/wdcossey/Avonic/issues/34) reorder · [#35](https://github.com/wdcossey/Avonic/issues/35) refresher · [#36](https://github.com/wdcossey/Avonic/issues/36) infinite-scroll · [#37](https://github.com/wdcossey/Avonic/issues/37) picker family |
| **P3** | Overlays — need the overlay system | [#38](https://github.com/wdcossey/Avonic/issues/38) toast · [#39](https://github.com/wdcossey/Avonic/issues/39) alert · [#40](https://github.com/wdcossey/Avonic/issues/40) action-sheet · [#41](https://github.com/wdcossey/Avonic/issues/41) loading · [#42](https://github.com/wdcossey/Avonic/issues/42) modal (+ sheet) · [#43](https://github.com/wdcossey/Avonic/issues/43) popover · [#44](https://github.com/wdcossey/Avonic/issues/44) menu |
| **P4** | Compound — need overlays / pickers / a navigation contract | [#45](https://github.com/wdcossey/Avonic/issues/45) select family · [#46](https://github.com/wdcossey/Avonic/issues/46) datetime · [#47](https://github.com/wdcossey/Avonic/issues/47) back-button |

Priorities were derived from `core.json`'s dependency graph (`ion-ripple-effect` is depended on by
13 components, `ion-backdrop` by 6, `ion-button`/`ion-spinner` by 3) plus the shared infrastructure
that the graph does not record (colour system, icons, gestures, overlays).

## Done

Rolled up at the end of each priority tier, so recently finished work may be missing. For the
latest, see the `Status:` line in each `README.md` under `docs/components/` and `docs/infra/`
([process](PROCESS.md#rolling-up-the-tables)).

| # | Ionic | Status |
|---|---|---|
| — | `ion-ripple-effect` (+ the `tap-click` activatable behaviour) | **done** (signed off 2026-10-15) — [docs](components/ion-ripple-effect/README.md) · [comparison](components/ion-ripple-effect/COMPARISON.md) |
| [#1](https://github.com/wdcossey/Avonic/issues/1) | infra: auto-discovery of templates, tokens and sample pages | **done** — [docs](infra/auto-discovery/README.md) · [plan](infra/auto-discovery/PLAN.md) |

## Out of scope

`ion-app`, `ion-router`, `ion-route`, `ion-route-redirect`, `ion-router-link`,
`ion-router-outlet`, `ion-nav`, `ion-nav-link`, `ion-split-pane`, `ion-menu-button`,
`ion-menu-toggle`, `ion-picker-legacy`, `ion-datetime-button`.
Reason: web-routing or desktop-shell concerns that do not map to Avalonia navigation.
(`ion-back-button`, #47, must first define the minimal navigation contract it binds to.)
