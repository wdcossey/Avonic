# AvonicChip

A compact interactive tag element, often used for filters, selections, or dismissible labels.

**Ionic reference:** [`ion-chip`](https://ionicframework.com/docs/api/chip)

---

## Usage

```xml
<avonic:AvonicChip>Default</avonic:AvonicChip>
<avonic:AvonicChip Outline="True">Outline</avonic:AvonicChip>
<avonic:AvonicChip IsEnabled="False">Disabled</avonic:AvonicChip>

<!-- With icon -->
<avonic:AvonicChip>
    <StackPanel Orientation="Horizontal" Spacing="6">
        <TextBlock Text="🏷" />
        <TextBlock Text="Tagged" />
    </StackPanel>
</avonic:AvonicChip>
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Chip label/content |
| `Outline` | `bool` | `false` | Renders with a border and transparent background |
| `IsEnabled` | `bool` | `true` | Disables interaction when `false` |

## Events

Inherits `Pressed`, `Released`, `Cancelled` from `AvonicPressable`.

## Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:outline` | `Outline=true` |
| `:pressed` | Pointer held down |
| `:disabled` | `IsEnabled=false` |

## Touch target

Minimum height enforced at **48dp** via `AvonicPressable.MinimumTouchTarget`.

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Chip.Padding` | `12,0` | Internal horizontal padding |
| `Avonic.CornerRadius.Pill` | `999` | Full-round corners |
