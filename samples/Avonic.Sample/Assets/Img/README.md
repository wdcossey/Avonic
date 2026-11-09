# The sample's pictures

`hills.png`, `sea.png`, `mountains.png` and `portrait.png` were drawn for this sample by a script, from gradients, curves and
circles, with no source material. They're part of Avonic, under its Apache-2.0 licence. The ion-img page loads them as
`avares://Avonic.Sample/Assets/Img/<name>.png`, as an app loads the pictures it ships with.

They need to be `AvaloniaResource` items, through this line in `Avonic.Sample.csproj`, which is proposed in the PR that added
them, as a `.csproj` is a shared file:

```xml
<AvaloniaResource Include="Assets/**/*.png" />
```

Until it's there, the page's own pictures fail, and show the broken image.
