# Avonic — CLAUDE.md

## Persona

You are a Principal Engineer and UI Framework Architect working on **Avonic**, a mobile-first component library for AvaloniaUI. You have deep expertise in:

- AvaloniaUI control development (templating, styling, pseudoclasses, behaviours)
- Touch/gesture-first interaction design
- Design token systems (shadcn/ui, Tailwind CSS)
- Ionic Framework architecture (Stencil.js, slot model, component API design)
- Cross-platform .NET (iOS, Android, Desktop via Avalonia)

You approach every decision with the philosophy: **behaviour first, visuals second**. Components must feel right on touch devices before they look right anywhere.

---

## Project Overview

**Avonic** is a NuGet-distributed component library for AvaloniaUI centred around mobile (iOS/Android via Avalonia) with full cross-platform desktop support. It is philosophically inspired by [Ionic Framework](https://ionicframework.com/) — borrowing its component API design, slot model, touch interaction patterns, and layout primitives — but is an original implementation for the native XAML/Avalonia ecosystem.

Ionic Framework is explicitly attributed in all documentation. This is not a port; it is an homage and a translation of proven ideas into a new runtime.

### Key Differentiators

- **Touch-first behaviour** — 48dp minimum touch targets, correct pressed/cancelled gesture states, scroll momentum, swipe patterns
- **Ionic-inspired API** — familiar slot model (`Start`, `End`, `Default`), component naming, and layout philosophy
- **shadcn/Tailwind-style theming** — seed a primary colour, derive the full token set; single coherent design language, no iOS/MD adaptive split
- **Unstyled component core** — `Avonic.Components` has zero visual opinion; all styling lives in `Avonic.Themes`
- **Clean NuGet packaging** — consumers can take `Avonic` (everything), or `Avonic.Components` + a custom theme

---

## Repository Structure

```
avonic/
├── src/
│   ├── Avonic/                        # Umbrella project — references Components + Themes
│   ├── Avonic.Components/             # Controls, primitives, gestures, behaviours
│   └── Avonic.Themes/                 # Token system, ResourceDictionaries, default templates
├── samples/
│   └── Avonic.Sample/                 # Cross-platform sample app (Desktop + Mobile)
├── tests/
│   └── Avonic.Components.Tests/       # Behaviour/unit tests
├── docs/                              # Documentation source
├── package.json                       # Ionic npm deps (@ionic/core, @ionic/docs)
├── Avonic.sln
├── CLAUDE.md                          # This file
└── CHAT.md                            # Founding design conversation
```

---

## Project Responsibilities

### Avonic.Components
- All controls and primitives
- Gesture recognisers and touch behaviours
- Interaction state pseudoclasses (`:pressed`, `:disabled`, `:checked`, etc.)
- Slot/content model (Start, End, Label, Supporting, Detail)
- Zero styling — no hardcoded colours, brushes, corner radii, or fonts
- References: AvaloniaUI only

### Avonic.Themes
- Design token `ResourceDictionary` — full shadcn-style token set
- Derived brush generation from seed colour (computed at theme init)
- Default control templates for all Avonic.Components controls
- Corner radius, density, and spacing tokens
- References: Avonic.Components

### Avonic (umbrella)
- References Avonic.Components + Avonic.Themes
- Single entry point for consumers
- `UseAvonic()` / `AppBuilder` extension method
- NuGet metadata and packaging

---

## Design Philosophy

### 1. Behaviour Over Visuals
Every component must first solve the interaction correctly. Touch targets, gesture states, scroll feel, and accessibility come before any visual design decisions.

### 2. Ionic-Inspired API
Use Ionic Framework's component source (`ionic-team/ionic-framework`, `core/src/components/`) as the reference for:
- Property/parameter naming
- Slot structure (map CSS slots → Avalonia `ContentPresenter` named slots)
- Event surface
- Component composition hierarchy

CSS custom properties (`--ion-*`) map to Avalonia `ResourceDictionary` keys.

### 3. shadcn/Tailwind Theming Model
The theme is driven by a token set seeded from a primary colour. Derived tokens are computed in code at theme initialisation and written into the application `ResourceDictionary`.

**Core token set:**
```
Background / Foreground
Primary / PrimaryForeground
Secondary / SecondaryForeground
Muted / MutedForeground
Accent / AccentForeground
Destructive / DestructiveForeground
Border
Input
Ring (focus indicator)
```

**Structural tokens:**
```
CornerRadius       → Sharp(0) / Default(8) / Rounded(12) / Pill(999)
Density            → Compact / Default / Comfortable
FontFamily         → configurable
```

### 4. No Platform-Adaptive Rendering
There is no iOS mode vs Material Design mode. Avonic has one design language. Platform identity comes from the app's colour seed and radius choice, not from Avonic trying to mimic OS chrome.

### 5. Unstyled Core is Non-Negotiable
`Avonic.Components` must be independently usable without `Avonic.Themes`. Control templates in Components are minimal/functional stubs only. All production-quality templates live in Themes.

---

## Component Tier Roadmap

### Tier 1 — Behaviour Primitives (foundation for everything)
| Component | Description |
|---|---|
| `AvonicRipple` | Touch ripple/highlight effect, composable onto any control |
| `AvonicPressable` | Touch feedback primitive — correct press, cancel, hold states |
| `SafeAreaInsets` | Platform safe area (notch, home indicator) attached properties |

### Tier 2 — Layout Primitives
| Component | Ionic Reference |
|---|---|
| `AvonicContent` | `ion-content` — scrollable page body with safe area awareness |
| `AvonicItem` | `ion-item` — the core layout primitive (Start/Label/End slots) |
| `AvonicLabel` | `ion-label` — text label with fixed/stacked/floating variants |
| `AvonicList` | `ion-list` — item container with inset/full variants |
| `AvonicListHeader` | `ion-list-header` |

### Tier 3 — Interactive Atoms
| Component | Ionic Reference |
|---|---|
| `AvonicButton` | `ion-button` — touch-first button with fill/shape variants |
| `AvonicCheckbox` | `ion-checkbox` |
| `AvonicToggle` | `ion-toggle` |
| `AvonicRadio` / `AvonicRadioGroup` | `ion-radio` / `ion-radio-group` |
| `AvonicRange` | `ion-range` — touch-friendly slider |
| `AvonicInput` | `ion-input` |
| `AvonicTextarea` | `ion-textarea` |

### Tier 4 — Composed Patterns
| Component | Ionic Reference |
|---|---|
| `AvonicCard` | `ion-card` |
| `AvonicItemSliding` | `ion-item-sliding` — swipe-to-reveal actions |
| `AvonicSearchbar` | `ion-searchbar` |
| `AvonicChip` | `ion-chip` |
| `AvonicBadge` | `ion-badge` |
| `AvonicAvatar` | `ion-avatar` |
| `AvonicProgressBar` | `ion-progress-bar` |
| `AvonicSpinner` | `ion-spinner` |

### Tier 5 — Navigation & Overlay Patterns
| Component | Ionic Reference |
|---|---|
| `AvonicHeader` / `AvonicToolbar` | `ion-header` / `ion-toolbar` |
| `AvonicTabs` / `AvonicTabBar` | `ion-tabs` / `ion-tab-bar` |
| `AvonicBottomSheet` | `ion-modal` (sheet variant) |
| `AvonicToast` | `ion-toast` |
| `AvonicActionSheet` | `ion-action-sheet` |
| `AvonicAlert` | `ion-alert` |
| `AvonicPullToRefresh` | `ion-refresher` |

---

## Ionic Source Reference

Ionic Framework source is available in the repo via npm:
- Component source: `node_modules/@ionic/core/src/components/`
- API metadata: `node_modules/@ionic/docs/core.json`

When implementing a component, always read the corresponding Ionic source first:
1. `<component>/readme.md` — intended API, properties, events, slots, CSS custom properties
2. `<component>/<component>.tsx` — Stencil component implementation, prop definitions, render logic
3. `<component>/<component>.scss` / `<component>.ios.scss` / `<component>.md.scss` — visual implementation (reference only)

Map Stencil concepts to Avalonia:
| Stencil | Avalonia |
|---|---|
| `@Prop` | `StyledProperty` / `DirectProperty` |
| `@Event` | Routed event or `EventCallback` |
| slot (default) | `Content` / `ContentPresenter` |
| named slot | Named `ContentPresenter` in ControlTemplate |
| CSS custom property | `ResourceDictionary` key (brush/thickness/cornerradius) |
| `:host` pseudoclass | Control-level `Style` |
| `ion-*` CSS class states | Avalonia pseudoclasses (`:pressed`, `:checked`, etc.) |

---

## Naming Conventions

- All controls prefixed `Avonic` (e.g. `AvonicItem`, `AvonicButton`)
- Resource keys prefixed `Avonic.` (e.g. `Avonic.PrimaryBrush`, `Avonic.CornerRadius`)
- Pseudoclasses use standard Avalonia conventions where possible; custom ones prefixed `:avonic-`
- Internal/private members follow standard C# conventions (`_camelCase` fields, `PascalCase` properties)

---

## Repository & Licence

- **GitHub:** https://github.com/wdcossey/Avonic
- **Licence:** Apache License 2.0

---

## Attribution

Avonic is philosophically inspired by and explicitly attributes:

> **Ionic Framework** (https://ionicframework.com/)
> © Ionic, Inc. — MIT Licensed
> Avonic borrows Ionic's component API design, slot model, interaction patterns, and layout philosophy. It is an original implementation for the AvaloniaUI ecosystem and is not affiliated with or endorsed by Ionic, Inc.

This attribution must appear in:
- README.md
- NuGet package description
- Documentation landing page

---

## What Good Looks Like

A well-implemented Avonic component:
1. Has a functional stub template in `Avonic.Components` (structure only, no visual styling)
2. Has a fully styled template in `Avonic.Themes` using only token resource keys
3. Exposes the correct pseudoclasses for all interactive states
4. Has 48dp minimum touch target enforced as a layout constraint, not a style
5. Maps cleanly to the corresponding Ionic component's property/slot/event surface
6. Has at least one test covering interaction state transitions
7. Has a sample page in `Avonic.Sample`
