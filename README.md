# Avonic

A mobile-first component library for [AvaloniaUI](https://avaloniaui.net/), philosophically inspired by [Ionic Framework](https://ionicframework.com/).

Avonic brings Ionic's touch-first interaction model, slot-based component API, and coherent design token system to the native XAML/Avalonia ecosystem — as an original implementation, not a port.

---

## Features

- **Touch-first behaviour** — 48dp minimum touch targets, correct press/cancel gesture states, scroll momentum
- **Ionic-inspired API** — familiar slot model (`Start`, `End`, `Default`), component naming, and layout hierarchy
- **shadcn/Tailwind-style theming** — seed a primary colour, derive the full token set; one coherent design language
- **Unstyled component core** — `Avonic.Components` has zero visual opinion; all styling lives in `Avonic.Themes`
- **Self-contained theme** — `AvonicTheme` owns its full style cascade with no dependency on FluentTheme or SimpleTheme
- **Clean NuGet packaging** — take `Avonic` (everything), or `Avonic.Components` + a custom theme

---

## Getting Started

### Installation

```shell
dotnet add package Avonic
```

Or for components + a custom theme separately:

```shell
dotnet add package Avonic.Components
dotnet add package Avonic.Themes
```

### Setup

In your `App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:avonic="using:Avonic.Themes"
             ...>
    <Application.Styles>
        <avonic:AvonicTheme />
    </Application.Styles>
</Application>
```

> **Note:** `AvonicTheme` styles Avonic controls only. If you use native Avalonia controls (`TextBox`, `Button`, etc.) outside of Avonic components, add `<SimpleTheme />` above `<avonic:AvonicTheme />`.

---

## Components

### Tier 1 — Behaviour Primitives

| Component | Description |
|---|---|
| `AvonicPressable` | Touch feedback primitive — press, release, cancel states with 48dp minimum touch target |
| `AvonicRipple` | Touch ripple/highlight effect, composable onto any control |
| `SafeAreaInsets` | Platform safe area attached properties (notch, home indicator) |

### Tier 2 — Layout Primitives

| Component | Ionic Reference |
|---|---|
| `AvonicContent` | `ion-content` — scrollable page body |
| `AvonicItem` | `ion-item` — core layout primitive with Start/Label/End slots |
| `AvonicLabel` | `ion-label` — text label with fixed/stacked position variants |
| `AvonicList` | `ion-list` — item container with inset/full variants |
| `AvonicListHeader` | `ion-list-header` — section title with optional end content |

### Tier 3 — Interactive Atoms

| Component | Ionic Reference |
|---|---|
| `AvonicButton` | `ion-button` — fill (solid/outline/clear), shape, size, expand variants |
| `AvonicCheckbox` | `ion-checkbox` — checked/indeterminate, label placement |
| `AvonicToggle` | `ion-toggle` — tap-to-toggle with drag gesture |
| `AvonicRadio` / `AvonicRadioGroup` | `ion-radio` / `ion-radio-group` — value-based mutually exclusive selection |
| `AvonicRange` | `ion-range` — touch-friendly slider with pin, ticks, start/end slots |
| `AvonicInput` | `ion-input` — single-line text input; label placement, fill, shape, clear button, helper/error text, counter |
| `AvonicTextarea` | `ion-textarea` — multi-line text input; auto-grow, rows |

### Tier 4–5 — Roadmap

Cards, chips, badges, avatars, navigation headers, tab bars, bottom sheets, toasts, action sheets, pull-to-refresh — see [CLAUDE.md](CLAUDE.md) for the full roadmap.

---

## Theming

Avonic uses a shadcn/Tailwind-style token system. Tokens are defined in `Avonic.Themes/Tokens/AvonicTokens.axaml` and cover:

```
Background / Foreground
Primary / PrimaryForeground
Secondary / SecondaryForeground
Muted / MutedForeground
Accent / AccentForeground
Destructive / DestructiveForeground
Border / Input / Ring
CornerRadius (Sharp / Default / Rounded / Pill)
```

Override any token via `Application.Resources` or on individual controls:

```xml
<avonic:AvonicButton>
    <avonic:AvonicButton.Resources>
        <SolidColorBrush x:Key="Avonic.PrimaryBrush" Color="#E91E63" />
    </avonic:AvonicButton.Resources>
    Pay Now
</avonic:AvonicButton>
```

---

## Repository Structure

```
avonic/
├── src/
│   ├── Avonic/                  # Umbrella project — references Components + Themes
│   ├── Avonic.Components/       # Controls, primitives, gestures, behaviours (zero styling)
│   └── Avonic.Themes/           # Token system, ResourceDictionaries, default templates
├── samples/
│   └── Avonic.Sample/           # Cross-platform sample app
├── tests/
│   └── Avonic.Components.Tests/ # Behaviour/unit tests
```

---

## Attribution

Avonic is philosophically inspired by and explicitly attributes:

> **Ionic Framework** (https://ionicframework.com/)
> © Ionic, Inc. — MIT Licensed
>
> Avonic borrows Ionic's component API design, slot model, interaction patterns, and layout philosophy. It is an original implementation for the AvaloniaUI ecosystem and is not affiliated with or endorsed by Ionic, Inc.

---

## Licence

Apache License 2.0 — see [LICENSE](LICENSE) for details.
