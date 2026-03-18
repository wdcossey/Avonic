# AvonicCard

A content container with elevation (drop shadow), optionally tappable as a button.

**Ionic reference:** [`ion-card`](https://ionicframework.com/docs/api/card)

---

## Usage

```xml
<!-- Static card -->
<avonic:AvonicCard>
    <StackPanel Padding="16">
        <TextBlock Text="Card Title" FontSize="18" FontWeight="SemiBold" />
        <TextBlock Text="Card body text." Foreground="#71717A" />
    </StackPanel>
</avonic:AvonicCard>

<!-- Interactive card -->
<avonic:AvonicCard Button="True" Click="OnCardClicked">
    <StackPanel Padding="16">
        <TextBlock Text="Tap me" />
    </StackPanel>
</avonic:AvonicCard>
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Main card content |
| `Button` | `bool` | `false` | Makes the card tappable; raises `Click` on release |
| `IsEnabled` | `bool` | `true` | When `false`, disables interaction and dims the card |

## Events

| Event | Description |
|---|---|
| `Click` | Raised when the card is tapped (only when `Button=true`) |

## Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:button` | `Button=true` |
| `:pressed` | Pointer is held down (button mode only) |
| `:disabled` | `IsEnabled=false` |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Card.Margin` | `16,8` | Outer margin applied by default style |
| `Avonic.Color.CardShadow` | `#18000000` | Drop shadow colour |
| `Avonic.CornerRadius.Rounded` | `12` | Corner radius |
