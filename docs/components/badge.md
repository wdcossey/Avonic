# AvonicBadge

A small coloured pill label for counts, statuses, or short text annotations.

**Ionic reference:** [`ion-badge`](https://ionicframework.com/docs/api/badge)

---

## Usage

```xml
<!-- Default (primary colour) -->
<avonic:AvonicBadge>3</avonic:AvonicBadge>

<!-- Override background -->
<avonic:AvonicBadge Background="#EF4444">99+</avonic:AvonicBadge>
<avonic:AvonicBadge Background="#22C55E">New</avonic:AvonicBadge>

<!-- In an item end slot -->
<avonic:AvonicItem>
    <avonic:AvonicLabel>Inbox</avonic:AvonicLabel>
    <avonic:AvonicItem.EndContent>
        <avonic:AvonicBadge>12</avonic:AvonicBadge>
    </avonic:AvonicItem.EndContent>
</avonic:AvonicItem>
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Badge text/count |
| `Background` | `IBrush?` | Primary brush | Background fill |
| `Foreground` | `IBrush?` | Primary foreground | Text colour |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Badge.Padding` | `6,2` | Internal padding |
| `Avonic.CornerRadius.Pill` | `999` | Full-round pill shape |
| `Avonic.FontSize.Small` | `12` | Badge text size |
