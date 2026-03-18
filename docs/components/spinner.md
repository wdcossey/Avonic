# AvonicSpinner

An animated loading indicator with multiple visual variants.

**Ionic reference:** [`ion-spinner`](https://ionicframework.com/docs/api/spinner)

---

## Usage

```xml
<!-- Default (circular) -->
<avonic:AvonicSpinner />

<!-- Specific variant -->
<avonic:AvonicSpinner Variant="Dots" />
<avonic:AvonicSpinner Variant="Lines" />
<avonic:AvonicSpinner Variant="Crescent" />
<avonic:AvonicSpinner Variant="Bubbles" />
<avonic:AvonicSpinner Variant="Circles" />

<!-- Custom colour -->
<avonic:AvonicSpinner Foreground="#EF4444" />
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Variant` | `SpinnerName` | `Circular` | Visual animation variant |
| `Duration` | `double` | `0` | Animation duration override in milliseconds; `0` uses the variant's natural duration |
| `Foreground` | `IBrush?` | Primary brush | Spinner colour |

> **Note:** The property is named `Variant` (not `Name`) to avoid conflict with the XAML `Name`/`x:Name` attribute.

## SpinnerName enum

| Value | Description |
|---|---|
| `Circular` | A rotating arc ring (default) |
| `Crescent` | A crescent-shaped rotating arc (same template as Circular) |
| `Dots` | Three pulsing dots with staggered animation |
| `Lines` | Eight short lines arranged in a circle, rotating as a group |
| `Bubbles` | Same as Lines (mapped to the same template) |
| `Circles` | Same as Lines (mapped to the same template) |

## Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:spinner-circular` | `Variant=Circular` |
| `:spinner-crescent` | `Variant=Crescent` |
| `:spinner-dots` | `Variant=Dots` |
| `:spinner-lines` | `Variant=Lines` |
| `:spinner-bubbles` | `Variant=Bubbles` |
| `:spinner-circles` | `Variant=Circles` |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Spinner.Size` | `28` | Default width and height |
| `Avonic.Spinner.DotSize` | `8` | Dot diameter for the Dots variant |
| `Avonic.Spinner.RingThickness` | `3` | Border thickness for the ring variants |
