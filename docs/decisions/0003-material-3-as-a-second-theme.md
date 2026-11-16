# ADR 0003 — Material 3 as a second theme, after every Ionic component

**Date:** November 2026 · **Status:** accepted, and on hold: no work starts until P3 and P4 are done

## Context

[ADR 0001](0001-single-md-skin.md) chose one skin, Ionic's `md`, and named its keys so that a second theme could be added
without renaming anything. A Material Design 3 theme was proposed for that slot: a second theme project over the same
`Avonic.Components`, which an app installs in place of `AvonicTheme`.

It was reviewed against `v2` at `57bdd3d`, with 52 controls done. What the review measured:

- **It fits.** `Avonic.Themes` is 50 lines of C# and about 11,000 lines of XAML, a theme is keyed by control type, and
  auto-discovery needs no change for a second project.
- **Tests are the larger half.** 100 test files run against `AvonicTheme` (1,456 calls of `InstalledTheme.Run`). A second
  theme needs its own.
- **Components holds some of md's look**, as the mapping table says it should: 50 timing constants in 24 files, and 7 parts
  drawn in code (`CheckboxMark`, `RippleVisual`, `RangeTicks`, `RefresherCircle`, `ProgressBarStream`, `AvonicSpinner`,
  `AvonicIcon`), some named by type in a control's template parts.
- **Hover, focus and pressed states exist already** (`:ion-hover`, `:ion-focused`), so a Material 3 state layer is a part
  of a template, not a new behaviour.
- **The reference is partial.** Google's `@material/web` 2.5.0 has stable components for about 14 of the 52 controls. Card,
  badge, navigation bar and segmented button are under its `labs`. It has no top app bar or search bar, and nothing of M3
  Expressive. About 24 controls have no Material 3 counterpart at all.
- **Cost.** A spike of Content, Card and Button is about 7 PRs. The other controls are an estimated 20 to 25 more, not
  measured.

The human ruled on nine questions on 2026-11-16.

## Decision

**Every Ionic component comes first.** No Material 3 work starts, not even the spike, until P3 and P4 are done. The human's
words: "Let's get all Ionic components done first, may be a higher cost later but easier to rollback to a solid point than
create a fork too early".

When it starts, it's a second theme, `Avonic.Themes.Material3`, with `Material3Theme` beside `AvonicTheme`. There is still
no `mode` property: the app picks a theme, and a control doesn't pick a mode. Avonic stays a recreation of Ionic's API and
behaviour, and Material 3 is a second look for it.

| Question | Decision |
|---|---|
| When | After P3 and P4. The spike (Content, Card, Button) comes first then, with a review of its cost before the rest |
| Material 3's variants that Ionic doesn't have (tonal and elevated buttons, three kinds of card) | Attached properties owned by the Material 3 package. Never values added to Ionic's enums: they'd shadow an Ionic name, do nothing under the Ionic theme, and couldn't be removed after a release. Ionic's own values still map: `Solid` is filled, `Outline` is outlined, `Clear` is text |
| Controls with no Material 3 counterpart | Composed from Material 3's tokens (colour, shape, type, states) on Ionic's geometry, and recorded as having no reference. The human: "We can refine (redesign) them for md3 when/if we need to, keep them tracked!" The list is below |
| Resource key names | The global keys are shared (`Avonic.Color.*`, backgrounds, safe areas, the grid's). Material 3's own go under `Avonic.M3.*`, mirroring its token names, as `Avonic.<Name>.Md.*` already means Ionic's md |
| Colour roles | Static token files, generated in Node with Google's `material-color-utilities`, as the icons are generated. No runtime dependency. A scheme from a seed colour at run time can come later, as an extension |
| Look that Components holds in code | A resource key the code reads, with Ionic's value as the fallback, added only when a Material 3 control needs it. Not a styled property per constant. Where the drawing itself differs, the Material 3 package brings its own part, behind an internal interface |
| Light and dark | Avalonia's `ThemeVariant`, in the Material 3 theme. The Ionic theme keeps its dark palette as a file the app merges, as Ionic's is |
| Is Material 3 part of a control's "done"? | No. It has a status of its own, rolled up as its own tier |
| M3 Expressive | Not planned. Looked at again once ten controls have the baseline theme |

## Controls with no Material 3 counterpart

Tracked here, as asked. Each is composed from tokens when its turn comes, and is a candidate for a design of its own later.

`ion-item-sliding`, `ion-item-options`, `ion-item-option`, `ion-reorder`, `ion-reorder-group`, `ion-accordion`,
`ion-accordion-group`, `ion-breadcrumbs`, `ion-breadcrumb`, `ion-infinite-scroll`, `ion-infinite-scroll-content`,
`ion-picker` and its column and option, `ion-input-otp`, `ion-skeleton-text`, `ion-avatar`, `ion-thumbnail`, `ion-img`,
`ion-note`, `ion-label`, `ion-text`, `ion-list-header`, `ion-item-group`, `ion-segment-view`, `ion-segment-content`,
`ion-footer`, `ion-buttons`, `ion-grid`, `ion-row`, `ion-col`, `ion-content`, `ion-backdrop`, `ion-icon`.

Several of these have no look of their own (the grid, the content, the label), and take only the theme's colours and type.
The components of P3 and P4 are sorted when they exist.

## Consequences

- P3 and P4 keep their cost: one theme each.
- ADR 0001's "one skin" holds until this work starts. Its first PR then amends 0001, and writes the rule for measuring a
  control against `@material/web` or the spec, and the loop a theme-only change follows, as `docs/PROCESS.md` starts from
  Ionic's API.
- The numbers above are from `57bdd3d`. Measure again before planning from them, and check what `@material/web` covers by
  then.
- The Components changes a second theme needs, the ripple first, are found late. That is the cost the human accepted.
- From the first Material 3 control on, a change to a control's parts or pseudoclasses is made in two themes. A test that
  every theme's template has every part a control names is part of the first PR.
- Material 3's elevation is shadows, so its tokens follow the box-shadow rule from
  [#139](https://github.com/wdcossey/Avonic/issues/139).
