# The sample's pictures

`hills.png`, `sea.png`, `mountains.png` and `portrait.png` were drawn for this sample by a script, from gradients, curves and
circles, with no source material. They're part of Avonic, under its Apache-2.0 licence. The ion-img page loads them as
`avares://Avonic.Sample/Assets/Img/<name>.png`, as an app loads the pictures it ships with.

They're `AvaloniaResource` items, through this line in `Avonic.Sample.csproj`:

```xml
<AvaloniaResource Include="Assets/**/*.png" />
```

Without it, the page's own pictures fail, and show the broken image.
