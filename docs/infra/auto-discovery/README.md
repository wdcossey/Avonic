# Auto-discovery

A control adds its own files and is picked up automatically. Nothing shared needs editing, so
parallel PRs don't conflict on registrations.

## How a control uses it

| Add this file | Picked up by | Mechanism |
|---|---|---|
| `src/Avonic.Components/Controls/<Name>/<Name>.axaml` | `AvonicComponentsStyles` | `Controls/**/*.axaml` glob |
| `src/Avonic.Themes/Tokens/<Name>.axaml` | `AvonicTheme` | `Tokens/*.axaml` glob |
| `src/Avonic.Themes/Controls/<Name>.axaml` | `AvonicTheme` | `Controls/*.axaml` glob |
| `samples/Avonic.Sample/Pages/<Name>Page.axaml` | The sample's index | `[SamplePage]` on the page class |

Mark each sample page with its Ionic tag, using the control's issue number as `Order`:

```csharp
[SamplePage("ion-ripple-effect", Order = 1)]
public partial class RippleEffectPage : UserControl
```

The index sorts by `Order`, then by title.

## Rules

- **Root element.** Every discovered `.axaml` must have a `ResourceDictionary` root. Any other root
  throws when the dictionary loads, and `DiscoveryTests` fails.
- **Themes files stay one level deep.** `src/Avonic.Themes/Controls/<Name>/<Name>.axaml` is not
  discovered. `Avonic.Components` allows nesting and uses one folder per control.
- **Themes overrides the stubs.** `AvonicTheme` looks in its own dictionaries before
  `AvonicComponentsStyles`, so a Themes `ControlTheme` with the same key replaces the stub.
- **Merge order.** Components is ordinal. Themes is `Tokens/` first, then ordinal.
- **Keys must be unique across files.** Merged dictionaries are searched last-to-first, so a duplicate
  key resolves by file name, which is rarely what you want.

## How it works

`Directory.Build.targets` runs before each compile. It turns the `AvonicDiscoveredXaml` globs in the
project file into `obj/.../XamlManifest.g.cs`, an internal `XamlManifest.Paths` array. The file is
rewritten only when the list changes, so builds stay incremental.

`AssetLoader.GetAssets` can't do this job, because compiled `.axaml` isn't in the asset index. The
evidence is in [PLAN.md](PLAN.md).

## If something goes wrong

- **A control renders nothing.** Check that the file matches a glob above, then look at
  `obj/<Configuration>/net8.0/XamlManifest.g.cs` in that project.
- **A page isn't on the index.** It's missing `[SamplePage]`, which
  `Sample_Every_Page_Is_Marked_For_Discovery` also catches.
