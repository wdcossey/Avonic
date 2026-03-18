# AvonicProgressBar

A horizontal progress indicator with determinate (value-driven) and indeterminate (looping animation) modes.

**Ionic reference:** [`ion-progress-bar`](https://ionicframework.com/docs/api/progress-bar)

---

## Usage

```xml
<!-- Determinate — 75% complete -->
<avonic:AvonicProgressBar Value="0.75" />

<!-- With buffer indicator (streaming / buffering) -->
<avonic:AvonicProgressBar Value="0.4" Buffer="0.8" />

<!-- Indeterminate — unknown duration -->
<avonic:AvonicProgressBar Type="Indeterminate" />

<!-- Reversed fill -->
<avonic:AvonicProgressBar Value="0.6" Reversed="True" />
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Value` | `double` | `0` | Progress amount, clamped to [0, 1] |
| `Buffer` | `double` | `1` | Secondary buffer amount, clamped to [0, 1] |
| `Type` | `ProgressBarType` | `Determinate` | `Determinate` or `Indeterminate` |
| `Reversed` | `bool` | `false` | Fill grows from right to left |

## Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:determinate` | `Type=Determinate` |
| `:indeterminate` | `Type=Indeterminate` |
| `:reversed` | `Reversed=true` |

## Template parts

| Name | Type | Purpose |
|---|---|---|
| `PART_Track` | `Border` | Background track |
| `PART_Buffer` | `Border` | Buffer fill (width set from `Buffer * ActualWidth`) |
| `PART_Progress` | `Border` | Active fill (width set from `Value * ActualWidth`) |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.ProgressBar.Height` | `4` | Track height |
| `Avonic.ProgressBar.BufferBrush` | `#C8D1D1D6` | Buffer bar colour |
| `Avonic.PrimaryBrush` | `#3880FF` | Active fill colour (via `Foreground`) |
