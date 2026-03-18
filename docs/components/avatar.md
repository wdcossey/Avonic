# AvonicAvatar

A circular container for an image, icon, or initials.

**Ionic reference:** [`ion-avatar`](https://ionicframework.com/docs/api/avatar)

---

## Usage

```xml
<!-- Initials -->
<avonic:AvonicAvatar Background="#3880FF">
    <TextBlock Text="AB" Foreground="White" FontWeight="SemiBold"
               HorizontalAlignment="Center" VerticalAlignment="Center" />
</avonic:AvonicAvatar>

<!-- Larger size -->
<avonic:AvonicAvatar Width="56" Height="56" Background="#22C55E">
    <TextBlock Text="CD" Foreground="White" FontSize="18"
               HorizontalAlignment="Center" VerticalAlignment="Center" />
</avonic:AvonicAvatar>

<!-- Image -->
<avonic:AvonicAvatar>
    <Image Source="/Assets/profile.png" Stretch="UniformToFill" />
</avonic:AvonicAvatar>
```

## Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Inner content (image, text, icon) |
| `Width` / `Height` | `double` | `40` (from token) | Avatar size |
| `Background` | `IBrush?` | `null` | Background fill |

## Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.Avatar.Size` | `40` | Default width and height |
| `Avonic.CornerRadius.Pill` | `999` | Circular clipping |
