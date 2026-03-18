# AvonicItemSliding / AvonicItemOptions / AvonicItemOption

A list item that reveals contextual action buttons when swiped horizontally.

**Ionic reference:** [`ion-item-sliding`](https://ionicframework.com/docs/api/item-sliding), [`ion-item-options`](https://ionicframework.com/docs/api/item-options), [`ion-item-option`](https://ionicframework.com/docs/api/item-option)

---

## Usage

```xml
<!-- Swipe left to reveal Delete -->
<avonic:AvonicItemSliding>
    <avonic:AvonicItemSliding.EndOptions>
        <avonic:AvonicItemOptions>
            <avonic:AvonicItemOption Color="Danger">Delete</avonic:AvonicItemOption>
        </avonic:AvonicItemOptions>
    </avonic:AvonicItemSliding.EndOptions>

    <avonic:AvonicItem Lines="Inset">
        <avonic:AvonicLabel>Swipe left to delete</avonic:AvonicLabel>
    </avonic:AvonicItem>
</avonic:AvonicItemSliding>

<!-- Start and end options -->
<avonic:AvonicItemSliding>
    <avonic:AvonicItemSliding.StartOptions>
        <avonic:AvonicItemOptions Side="Start">
            <avonic:AvonicItemOption Color="Success">
                <avonic:AvonicItemOption.StartContent>
                    <TextBlock Text="★" FontSize="20" />
                </avonic:AvonicItemOption.StartContent>
                Favourite
            </avonic:AvonicItemOption>
        </avonic:AvonicItemOptions>
    </avonic:AvonicItemSliding.StartOptions>
    <avonic:AvonicItemSliding.EndOptions>
        <avonic:AvonicItemOptions>
            <avonic:AvonicItemOption Color="Warning">Archive</avonic:AvonicItemOption>
            <avonic:AvonicItemOption Color="Danger">Delete</avonic:AvonicItemOption>
        </avonic:AvonicItemOptions>
    </avonic:AvonicItemSliding.EndOptions>

    <avonic:AvonicItem Lines="None">
        <avonic:AvonicLabel>Swipe either side</avonic:AvonicLabel>
    </avonic:AvonicItem>
</avonic:AvonicItemSliding>
```

---

## AvonicItemSliding

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Main item content (typically `AvonicItem`) |
| `StartOptions` | `AvonicItemOptions?` | `null` | Options revealed by swiping right |
| `EndOptions` | `AvonicItemOptions?` | `null` | Options revealed by swiping left |
| `IsEnabled` | `bool` | `true` | Disables swiping when `false` |

### Methods

| Method | Description |
|---|---|
| `Open(SlideSide)` | Programmatically snap open the specified side |
| `Close()` | Snap the item back to closed |

### Events

| Event | Description |
|---|---|
| `Drag` | Raised during drag with `SlidingDragEventArgs.TranslateX` |

### Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:open-start` | Start side is fully open |
| `:open-end` | End side is fully open |
| `:dragging` | User is actively dragging |

### Snap behaviour

The item snaps open if the drag exceeds **40%** of the options panel width; otherwise it snaps closed on release.

---

## AvonicItemOptions

Container for `AvonicItemOption` elements on one side.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Side` | `SlideSide` | `End` | Which side this panel occupies (`Start` or `End`) |

### Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:side-start` | `Side=Start` |
| `:side-end` | `Side=End` |

---

## AvonicItemOption

An individual swipe action button.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Content` | `object?` | `null` | Label text |
| `Color` | `ItemOptionColor` | `Default` | Background colour variant |
| `IconOnly` | `bool` | `false` | Hides the label; icon fills the button |
| `StartContent` | `object?` | `null` | Icon shown above the label |

### ItemOptionColor enum

| Value | Background |
|---|---|
| `Default` | Muted (grey) |
| `Primary` | Primary brand colour |
| `Secondary` | Secondary colour |
| `Success` | Green (`#22C55E`) |
| `Warning` | Amber (`#F59E0B`) |
| `Danger` | Red (`#EF4444`) |

### Pseudoclasses

| Pseudoclass | When active |
|---|---|
| `:color-default` | `Color=Default` |
| `:color-primary` | `Color=Primary` |
| `:color-danger` | `Color=Danger` |
| `:color-success` | `Color=Success` |
| `:color-warning` | `Color=Warning` |
| `:icon-only` | `IconOnly=true` |
| `:pressed` | Pointer held down |

### Tokens

| Key | Default | Purpose |
|---|---|---|
| `Avonic.ItemOption.MinWidth` | `72` | Minimum tap-target width |
