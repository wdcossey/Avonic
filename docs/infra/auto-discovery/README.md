# Auto-discovery

Status: done · [#1](https://github.com/wdcossey/Avonic/issues/1)

A control adds its own files and is picked up automatically, so parallel PRs don't conflict on
registrations. The tables in `CLAUDE.md` and `docs/ROADMAP.md` are handled separately, by a
[roll-up](../../PROCESS.md#rolling-up-the-tables).

## How a control uses it

| Add this file | Picked up by | Mechanism |
|---|---|---|
| `src/Avonic.Components/Controls/<Name>/<Name>.axaml` | `AvonicComponentsStyles` | `Controls/**/*.axaml` glob |
| `src/Avonic.Themes/Tokens/<Name>.axaml` | `AvonicTheme` | `Tokens/**/*.axaml` glob |
| `src/Avonic.Themes/Controls/<Name>.axaml` | `AvonicTheme` | `Controls/**/*.axaml` glob |
| `samples/Avonic.Sample/Pages/<Name>Page.axaml` | The sample's index | `[SamplePage]` on the page class |

Mark each sample page with its Ionic tag, using the control's issue number as `Order`:

```csharp
[SamplePage("ion-chip", Order = 19)]
public partial class ChipPage : UserControl
```

The index sorts by `Order`, then by title. A page with no `Order` sorts last. The ripple page uses
`Order = 1` because it predates the issues and has no number of its own.

## Rules

- **Root element.** Every discovered `.axaml` must have a `ResourceDictionary` root. Any other root
  throws when the dictionary loads, and `DiscoveryTests` fails.
- **Page classes.** A sample page's class name ends in `Page`, and it needs a public parameterless
  constructor. `DiscoveryTests` shows every page in a window under the theme, and fails if `[SamplePage]`
  or the constructor is missing or the page throws. A page with any other class name that also lacks
  `[SamplePage]` isn't caught.
- **Themes overrides the stubs.** `AvonicTheme` looks in its own dictionaries before
  `AvonicComponentsStyles`, so a Themes `ControlTheme` with the same key replaces the stub.
- **Merge order.** Components is ordinal. Themes is `Tokens/` first, then ordinal. The order only makes
  merging deterministic: templates resolve tokens from the application, and keys are unique, so it
  doesn't change which value a lookup returns.
- **Keys are unique within a project**, per theme variant, because merged dictionaries are searched
  last-to-first and a duplicate would resolve by file name. `DiscoveryTests` fails instead.
- **Every resource must build.** Entries are built on first lookup, so `DiscoveryTests` looks up every
  discovered key with the theme installed. A mistyped `{StaticResource}` token fails there rather than
  when the control is first themed.
- **Test Themes templates with the theme installed.** A template resolves tokens from
  `Application.Styles`, and `TestApp` loads only the stubs. Add the theme to `Application.Current.Styles`
  and remove it afterwards. On a detached `new AvonicTheme()`, looking up a template that uses
  `{StaticResource}` throws, and one using `{DynamicResource}` silently gets no value. Both work once
  installed.

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
  `Sample_Every_Page_Is_Marked_For_Discovery` also catches for classes named `*Page`.
