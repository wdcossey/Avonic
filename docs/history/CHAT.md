# Avonic — Founding Design Conversation

> This document captures the founding design conversation for Avonic.
> It is preserved here as a reference for architectural decisions, naming rationale,
> and the design philosophy that underpins the library.
>
> Date: March 2026

---

## The Problem

AvaloniaUI's existing controls work cross-platform but are clearly desktop-focused. On mobile (iOS/Android) they feel like "a desktop app that also runs on mobile" rather than something designed for the device.

The gap is **behavioural more than visual**. The theming system can fix look. Behaviour requires actual control work:

- Minimum 48dp touch targets as a layout constraint, not a style suggestion
- Pressed feedback that is immediate and responds to finger lift/cancel correctly
- Scroll containers that feel like the platform (momentum, overscroll bounce or resistance)
- Gesture disambiguation — knowing when a horizontal swipe should cancel a vertical scroll

Specific issues with current Avalonia controls on mobile:
- `ListBox` selection model, scroll inertia, and item sizing are tuned for mouse/keyboard
- `Button` hit areas are whatever the visual bounds are — no minimum touch target enforcement
- No concept of destructive swipe, pull-to-refresh, or bottom sheet as first-class patterns
- `ComboBox` drops down like a desktop dropdown rather than presenting a mobile picker sheet
- Focus rectangles and hover states everywhere, meaningless on touch

---

## The Solution: Avonic

A separate NuGet package for AvaloniaUI centred around mobile (iOS/Android) with full cross-platform desktop support. A fresh suite of components built around the philosophy and foundation of **Ionic Framework**.

### Core Philosophy

> **Ionic behaviour, shadcn/ui theming**

- Components **behave and feel** like Ionic (touch targets, gestures, slot model, interaction model)
- Visually they have a **single coherent design language** — not platform-adaptive (no iOS mode vs MD mode)
- Theming works like **shadcn/Tailwind** — seed a primary colour, the system derives the rest
- The component core is **unstyled** — structure and behaviour in `Avonic.Components`, everything visual in `Avonic.Themes`

This is a deliberately stronger position than trying to replicate iOS/MD adaptive rendering, which is a large maintenance burden and produces apps that look inconsistent across platforms anyway.

---

## Naming

### Library Name: Avonic

Considered options:
- `IonAvalonia` — too literal, reads like a direct port
- `AonMobile` — clever contraction but not immediately readable
- `Avalonik` — Avalonia + Ionic blend, strong but slightly long
- `Nucleus` — thematic (Ionic → atomic), but too generic
- `Valence` — chemistry nod, premium sounding
- **`Avonic`** ✓ — clean blend of Avalonia + Ionic, memorable, works as a NuGet package ID

### Attribution

Ionic Framework is explicitly attributed in all documentation. Avonic borrows Ionic's component API design, slot model, interaction patterns, and layout philosophy. It is an original implementation — not a port, not affiliated with Ionic, Inc.

---

## Project Structure

Three projects, clean separation of concerns:

```
Avonic                  # Umbrella — references Components + Themes, single NuGet entry point
Avonic.Components       # All controls, primitives, gestures, behaviours — zero visual opinion
Avonic.Themes           # Token system, ResourceDictionaries, default control templates
```

Dependency direction: `Themes → Components`, `Avonic → both`.

This means `Avonic.Components` is independently usable and testable without any theme. A consumer can take the components and provide entirely custom templates.

Full repo structure:
```
avonic/
├── src/
│   ├── Avonic/
│   ├── Avonic.Components/
│   └── Avonic.Themes/
├── samples/
│   └── Avonic.Sample/
├── tests/
│   └── Avonic.Components.Tests/
├── docs/
├── package.json                 # @ionic/core, @ionic/docs
├── Avonic.sln
├── CLAUDE.md
└── CHAT.md
```

---

## Theming Model

Inspired by shadcn/ui and Tailwind CSS. Seed a primary colour (HSL works best), derive the full token set at theme initialisation time, write derived brushes into the application `ResourceDictionary`.

### Token Set

```
Background / Foreground
Primary / PrimaryForeground
Secondary / SecondaryForeground
Muted / MutedForeground
Accent / AccentForeground
Destructive / DestructiveForeground
Border
Input
Ring                    ← focus indicator
```

Maps cleanly to Avalonia `ResourceDictionary` keys prefixed `Avonic.*`.

CSS custom properties from Ionic (`--ion-color-primary`, etc.) map directly to these tokens.

### Structural Tokens

```
CornerRadius    → Sharp / Default / Rounded / Pill
Density         → Compact / Default / Comfortable
FontFamily      → configurable
```

CornerRadius and Density tokens give apps significant personality control with minimal effort — the same components can feel sharp and professional or soft and friendly just from these two settings.

---

## Component Approach

### Reference: Ionic Framework Source

Ionic's source (`ionic-team/ionic-framework`) is the reference implementation. Available in the repo via:
- `node_modules/@ionic/core/src/components/` — Stencil component source
- `node_modules/@ionic/docs/core.json` — API metadata (same source used in IonBlazor's component generator)

Each Ionic component's `readme.md` documents the intended API surface (properties, events, slots, CSS custom properties). The `.tsx` file shows the implementation logic. The `.scss` files are visual reference only.

### Stencil → Avalonia Mapping

| Stencil concept | Avalonia equivalent |
|---|---|
| `@Prop` | `StyledProperty` / `DirectProperty` |
| `@Event` | Routed event |
| Default slot | `Content` / `ContentPresenter` |
| Named slot | Named `ContentPresenter` in ControlTemplate |
| CSS custom property | `ResourceDictionary` key |
| `:host` state | Control-level pseudoclass Style |

### Component Tiers

**Tier 1 — Behaviour Primitives**
`AvonicRipple`, `AvonicPressable`, `SafeAreaInsets`

**Tier 2 — Layout Primitives**
`AvonicContent`, `AvonicItem`, `AvonicLabel`, `AvonicList`, `AvonicListHeader`

**Tier 3 — Interactive Atoms**
`AvonicButton`, `AvonicCheckbox`, `AvonicToggle`, `AvonicRadio`/`AvonicRadioGroup`, `AvonicRange`, `AvonicInput`, `AvonicTextarea`

**Tier 4 — Composed Patterns**
`AvonicCard`, `AvonicItemSliding`, `AvonicSearchbar`, `AvonicChip`, `AvonicBadge`, `AvonicAvatar`, `AvonicProgressBar`, `AvonicSpinner`

**Tier 5 — Navigation & Overlay Patterns**
`AvonicHeader`/`AvonicToolbar`, `AvonicTabs`/`AvonicTabBar`, `AvonicBottomSheet`, `AvonicToast`, `AvonicActionSheet`, `AvonicAlert`, `AvonicPullToRefresh`

### What Good Looks Like

A well-implemented Avonic component:
1. Has a functional stub template in `Avonic.Components` (structure only, no visual styling)
2. Has a fully styled template in `Avonic.Themes` using only token resource keys
3. Exposes the correct pseudoclasses for all interactive states
4. Has 48dp minimum touch target enforced as a layout constraint, not a style
5. Maps cleanly to the corresponding Ionic component's property/slot/event surface
6. Has at least one test covering interaction state transitions
7. Has a sample page in `Avonic.Sample`

---

## Key Decisions Log

| Decision | Rationale |
|---|---|
| No iOS/MD adaptive rendering | Huge maintenance burden, produces inconsistent cross-platform look. Single design language is cleaner. |
| Unstyled component core | `Avonic.Components` independently usable and testable. Enables custom themes without forking. |
| shadcn-style token theming | Proven model, low friction for consumers, maps naturally to Avalonia ResourceDictionary. |
| Ionic source as reference | Battle-tested component API design. No need to reinvent the slot model or interaction patterns. |
| `Avonic` prefix on all controls | Clear namespace, avoids collision with Avalonia built-ins, consistent with IonBlazor portfolio. |
| npm package for Ionic source | `@ionic/core` + `@ionic/docs` in repo gives direct access to source and `core.json` metadata. |
| Three-project solution | Clean separation — behaviour, visuals, and umbrella. Mirrors real UI toolkit packaging. |

---

## Author

William Cossey
GitHub: https://github.com/wdcossey
Repository: https://github.com/wdcossey/Avonic
Licence: Apache License 2.0

*Avonic is not affiliated with or endorsed by Ionic, Inc. or the AvaloniaUI team.*
*Licensed under the Apache License 2.0.*
