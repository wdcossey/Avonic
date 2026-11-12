# Global typography: plan

[#70](https://github.com/wdcossey/Avonic/issues/70). Ionic's `css/typography.css` as a file an app merges, as decided in
[ADR 0002](../../decisions/0002-decisions-from-the-first-swarm.md). Usage is in the [README](README.md); the frames are in
[COMPARISON.md](COMPARISON.md).

## The spec

`node_modules/@ionic/core/css/typography.css` (8.8.1), whole:

| Rule | What it sets |
|---|---|
| `html` | `font-family: var(--ion-font-family)` |
| `@supports (-webkit-touch-callout: none) html` | `font: var(--ion-dynamic-font, 16px var(--ion-font-family))`, iOS Safari only |
| `a` | `background-color: transparent; color: var(--ion-color-primary, #0054e9)` |
| `h1, h2, h3, h4, h5, h6` | `margin-top: 16px; margin-bottom: 10px; font-weight: 500; line-height: 1.2` |
| `h1` … `h6` | `font-size` 1.625rem, 1.5rem, 1.375rem, 1.25rem, 1.125rem, 1rem; `h1` `margin-top: 20px`, `h2` 18px |
| `small` | `font-size: 75%` |
| `sub, sup` | `position: relative; font-size: 75%; line-height: 0; vertical-align: baseline`; `sup` `top: -0.5em`, `sub` `bottom: -0.25em` |

There's no rule for `p`.

## Measured

`node measure.js <repo> <out>`, a script kept out of the repo (as the brief says), which renders `h1`…`h6` and `p` with
`core.css`, `normalize.css` and `structure.css`, with and without `typography.css`, in Chromium at 390 × 844, with the repo's
Roboto: bare in the page, in an `ion-item-divider`, in an `ion-label` in an `ion-item`, and in `ion-card-content`. The values are in
[ref/measure.json](ref/measure.json), by id: `b` bare, `d` divider, `l` label, `c` card content.

| Where | With `typography.css` | Without |
|---|---|---|
| Bare (`bh1`…`bh6`) | 26, 24, 22, 20, 18, 16 px; weight 500; line height 1.2; margins 20/18/16 above, 10 below | The UA's: 2em, 1.5em … bold, normal line height, Times |
| `p` anywhere | No rule: the same both ways | |
| Divider (`dh1`…`dh6`) | **The same as bare.** `::slotted(h1)`'s margins lose: the divider is a shadow root, and a document's rule beats a shadow root's `::slotted` rule whatever the specificity. `::slotted(h3, h4, h5, h6)` is an invalid selector, so it's dropped either way | `h1` margin 0 / 2, `h2` 2 / 2, the UA's sizes and weight |
| Label (`lh1`…`lh6`) | The label's: `h1` 24, `h2` 16, `h3`–`h6` 14, weight 400, its margins. Line height **1.2** on `h1` and `h2`, as the label sets none for them, and normal on `h3`–`h6`, which it sets | The same, with **normal** line height on `h1` and `h2` |
| Card content (`ch1`…`ch6`) | The card's: `h1` 24, `h2` 16, `h3`–`h6` 14, weight 400, its margins. Line height **1.2**, as the card sets none for headings | The same, with line height **1.5**, inherited from `.card-content-md` |

So: the label (scoped, `.sc-ion-label-md-s h1`) and card content (`.card-content-md h1`) beat `h1` by specificity, except
for line height, which they don't set; a shadow root's `::slotted(h1)` loses to it.

## Design

**A `ResourceDictionary` holding a `ControlTheme` for `TextBlock`,** `src/Avonic.Themes/Css/Typography.axaml`, outside the
folders `AvonicTheme` discovers, merged by the app like `Tokens/Palettes/Dark.axaml`. Six rules, `^.h1` … `^.h6`, each with every
value, and no commas.

Why a `ControlTheme` and not global `Styles`, which is what the issue first suggested:

| | Global `Styles` | `ControlTheme` for `TextBlock` |
|---|---|---|
| A label's, a card's rules (more specific in CSS) | Both have a class, so the later wins: merged after `AvonicTheme`, the file beats them, which is wrong. Right only if the app merges it first | A global style beats a `ControlTheme` whatever the order, as CSS's specificity does |
| A divider (where `h1` wins in CSS) | It has no heading rules, so the file applies either way | The same |
| Cost to a `TextBlock` | 6 alternatives | 6 alternatives, and the theme itself, which stops at the type |
| Cost to any other element | 6 alternatives each, about 30 ns each | **None**: a `ControlTheme` is only applied to its type |
| An app's `TextBlock.h1` style | Wins if later | Wins |
| An app's own `ControlTheme` for `TextBlock` | Unaffected | They replace each other: the nearer resource wins |

**Line height in a label and card content.** In CSS they set no line height on headings, so `typography.css`'s 1.2 applies
when it's on, and what's inherited when it's off. A global style always beats the file's `ControlTheme`, so their rules can't
leave it to the file. They read a key instead, which the file sets to 1.2:

| Key | Default, without the file | The file |
|---|---|---|
| `Avonic.Label.HeadingLineHeight` (`h1`, `h2`) | `NaN`, normal | 1.2 |
| `Avonic.CardContent.HeadingLineHeight` (`h1`–`h6`) | 1.5, what they inherit from `.card-content-md` | 1.2 |

A `CssUnset` default that fell through to the inherited value was tried first: a style binding that yields nothing gives the
property's default, not the value of an earlier style (as CLAUDE.md says for `color: inherit`), and the card's tests caught it.

Until now, these rules had 1.2 always, as every reference render used `ionic.bundle.css`, which includes `typography.css`. So without
the file a heading in a label or card now has what Chromium measures without it: a card's `h1` is 36 high, not 28.8.

## Not mapped

| Rule | Why |
|---|---|
| `html { font-family }` | Avalonia has no root element a `ResourceDictionary` can style. `ion-content`, the item, the label and the rest set `Avonic.FontFamily` themselves, as Ionic's do (`font-family: var(--ion-font-family, inherit)`), so it only reaches text outside every Avonic control. An app sets its default font with `FontManagerOptions.DefaultFamilyName`, or `FontFamily` on its window. In the README |
| The iOS `@supports` rule | iOS Safari only, and Avonic recreates md |
| `a` | Avalonia's nearest is `HyperlinkButton`, whose base theme sets its own colours by state; no Avonic control or mapping uses it. A later issue, if an app needs it |
| `small`, `sub`, `sup` | Inlines (`Run`, `Span`) in a `TextBlock`. `font-size: 75%` needs the parent's size, which an inline's style can't bind to, and Avonic has no mapping for these elements yet. A later issue |

## Known differences

- **Vertical margins don't collapse** in a `StackPanel`, as CLAUDE.md records: two headings in a row are 10 + 18 apart, where CSS
  gives 18.
- **Without the file**, a heading outside a host has Avalonia's defaults, not the UA's 2em bold Times: ADR 0002's "Default text size
  outside a host". In a divider, Chromium also gives `h1` and `h2` the divider's 2 px margins, which Avonic doesn't add: they'd be
  global rules in the default theme, costing every element, for 2 px in the case without the file.
- **A wrapping label without the file:** its `h1` and `h2` would inherit 1.5 in Chromium; here they're normal, as the key holds one
  value. With the file, 1.2, as measured.
- **A heading in a badge or a button.** Measured in Chromium with the file, an `h1` in `ion-button`, `ion-badge` and
  `ion-card-title` is 26, 31.2 high, 20 above and 10 below, as anywhere ([ref/hosts-on.json](ref/hosts-on.json)). In a card title
  Avonic matches (`B4_In_A_Card_Title_The_File_Wins`). A button's and a badge's rules for any child are global styles, which beat
  the file: in a button the `h1` keeps its size and line height but takes the button's half-leading margin, not 20 and 10; in a
  badge it also takes the badge's line height of 1, 26. Matching would mean excluding headings from those rules, adding
  alternatives to the default theme for every element, for headings in buttons and badges.

## Tests

`tests/Avonic.Components.Tests/Typography/TypographyTests.cs`: every heading's values; nothing without the file; `p` untouched;
line height following the heading's own size; an app's style and a local value winning; merged locally, inside only; the merge
order; SimpleTheme having no `TextBlock` theme the file would replace; and the label, card content and divider as measured.
`Performance/TypographyBudgetTests.cs` records what merging it costs. `TypographyCaptureTests.cs` captures the sample's page
both ways.
