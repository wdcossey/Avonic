# Global typography

Status: done · [#70](https://github.com/wdcossey/Avonic/issues/70)

Ionic's `css/typography.css`: sizes, weight, margins and line height for `h1` to `h6`. Like Ionic's file, it's **opt-in**:
`AvonicTheme` doesn't merge it, so an app that doesn't ask for it pays nothing. The design and the measurements are in
[PLAN.md](PLAN.md), and the frames in [COMPARISON.md](COMPARISON.md).

## Using it

Merge it into the app's `Resources`, not its `Styles`, at startup. Where `AvonicTheme` is in the styles doesn't matter: the app's
resources are looked up first. In `Application.Styles` the file's keys would lose to `AvonicTheme`'s own, and a label's headings
would keep their line height without it:

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceInclude Source="avares://Avonic.Themes/Css/Typography.axaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

Or into any control's `Resources`, for what's inside it only, as the sample's `typography` page does. Headings are `TextBlock`s
with a class, as everywhere in Avonic:

```xml
<TextBlock Classes="h1" Text="Heading" />
```

| Class | Size | Weight | Line height | Margin above, below |
|---|---|---|---|---|
| `h1` | 26 | 500 | 1.2 × its size | 20, 10 |
| `h2` | 24 | 500 | 1.2 × | 18, 10 |
| `h3` | 22 | 500 | 1.2 × | 16, 10 |
| `h4` | 20 | 500 | 1.2 × | 16, 10 |
| `h5` | 18 | 500 | 1.2 × | 16, 10 |
| `h6` | 16 | 500 | 1.2 × | 16, 10 |

`p` has no rule in `typography.css`, so it has none here.

## Where it wins

As measured in Chromium:

- **In an item divider,** the file wins, as a document's `h1` beats a shadow root's `::slotted(h1)`.
- **In a label and in card content,** their own rules for headings win, as they're more specific in CSS, except for line height,
  which they don't set: there the file's 1.2 applies. They read it from `Avonic.Label.HeadingLineHeight` and
  `Avonic.CardContent.HeadingLineHeight`, which the file sets. Without it they're what Chromium measures without
  `typography.css`: normal in a label, 1.5 in card content.
- **An app's own rules win:** a `TextBlock.h1` style, wherever it's merged, or a local value. The file is a `ControlTheme`, and
  any global style beats one.

## What it costs

A `TextBlock` is checked against 6 more alternatives, one per heading, about 0.2 µs at the measured 30 ns each, and against the
theme itself, which stops at the type. **Nothing else is checked against it at all,** as it's a `ControlTheme` for `TextBlock`, not
a global style. `TypographyBudgetTests` holds those numbers; `StyleBudgetTests`, the default budget, doesn't count the file, and
didn't change.

## Not included

| `typography.css` | Why |
|---|---|
| `html { font-family }` | No root element to style from a dictionary. Avonic's controls set `Avonic.FontFamily` themselves, as Ionic's do. For text outside them, set the app's default font: `.With(new FontManagerOptions { DefaultFamilyName = ... })` on the `AppBuilder`, or `FontFamily` on the window |
| The iOS `@supports` rule | iOS only; Avonic recreates md |
| `a`, `small`, `sub`, `sup` | No Avonic element maps to them yet |

## Known gaps

- **Vertical margins don't collapse** in Avalonia: two headings in a `StackPanel` are 10 + 18 apart, where CSS gives 18.
- **An app's own `ControlTheme` for `TextBlock`** and this file replace each other: the nearer resource wins. Put an app's rules for headings in a
  style instead. Only SimpleTheme has been checked for a `ControlTheme` of its own for `TextBlock` (it has none); FluentTheme
  hasn't, as no project here references it.
- **A `SelectableTextBlock`** gets nothing, as its theme is looked up by its own type. Headings are `TextBlock`s.
- **Merged or removed while the app runs,** it doesn't restyle a `TextBlock` already shown, as Avalonia picks a control's theme
  when it's attached, while the label's and card's keys follow at once. Merge it at startup, or in the `Resources` of a view
  before the view is shown.
- **A plain `TextBlock` style with no class** loses to the file's `h1`, as CSS's `*` does to `h1`.
- **Without the file,** a heading outside a host has Avalonia's defaults, not a browser's; in a divider it lacks Chromium's 2 px
  margins; in a wrapping label `h1` and `h2` are normal, where Chromium inherits 1.5.
- **A heading in a badge, a button or a card title** keeps that host's line height. Not measured.
- Not tried in a real window, and not on a device.
