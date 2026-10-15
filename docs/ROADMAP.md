# Roadmap

Ionic has **95** components (`node tools/ionic-api.js --list`). Avonic does not promise all of
them. This file is the only place scope lives; `CLAUDE.md` lists what *exists*.

## Order (one at a time — see `docs/PROCESS.md`)

Foundation first; each depends on the one before it.

| # | Ionic | Status |
|---|---|---|
| 1 | `ion-ripple-effect` (+ the `tap-click` activatable behaviour) | **awaiting sign-off** — [docs](components/ion-ripple-effect/README.md) · [comparison](components/ion-ripple-effect/COMPARISON.md) |
| 2 | `ion-icon` (ionicons pipeline) | planned |
| 3 | `ion-item` | planned |
| 4 | `ion-label` | planned |
| 5 | `ion-list`, `ion-list-header`, `ion-item-divider`, `ion-item-group` | planned |
| 6 | `ion-button` | planned |
| 7 | `ion-content` | planned |

After #7 the next control is chosen when #7 is signed off, not before.

## In scope for v2.x (unordered)

accordion, accordion-group, action-sheet, alert, avatar, backdrop, badge, card (+ header,
title, subtitle, content), checkbox, chip, datetime, fab (+ button, list), footer, header,
img, infinite-scroll, input, input-otp, input-password-toggle, item-option(s), item-sliding,
loading, menu, modal, note, picker, popover, progress-bar, radio, radio-group, range,
refresher, reorder, reorder-group, searchbar, segment (+ button, content, view), select
(+ option, modal), skeleton-text, spinner, tab, tab-bar, tab-button, tabs, text, textarea,
thumbnail, title, toast, toggle, toolbar, buttons, back-button, breadcrumb(s), grid/row/col.

## Out of scope

`ion-app`, `ion-router`, `ion-route`, `ion-route-redirect`, `ion-router-link`,
`ion-router-outlet`, `ion-nav`, `ion-nav-link`, `ion-split-pane`, `ion-menu-button`,
`ion-menu-toggle`, `ion-picker-legacy`, `ion-datetime-button`.
Reason: web-routing or desktop-shell concerns that do not map to Avalonia navigation.
