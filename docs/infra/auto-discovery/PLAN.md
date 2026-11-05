# Plan: auto-discovery of templates, tokens and sample pages (#1)

## Problem

Each control had to add a line to shared registration points: `AvonicComponentsStyles.cs`,
`AvonicTheme.cs` and the `Pages` array in the sample's `MainWindow.axaml.cs`. Parallel PRs would
conflict on those lines every time. After this change a control adds only its own files.

## Ionic source mapping

None. This is repository infrastructure with no Ionic equivalent, so there is no `ionic-api.js`
checklist and no behaviour to compare.

## Deviation from the issue: not `AssetLoader.GetAssets`

The issue proposes discovering templates with `AssetLoader.GetAssets`. That cannot work, because the
XAML compiler removes compiled `.axaml` files from the asset index. A throwaway headless test on
Avalonia 12.1.4 returned:

```
GetAssets(avares://Avonic.Components/)                     -> 1: [avares://Avonic.Components/!AvaloniaResourceXamlInfo]
GetAssets(avares://Avonic.Components/Controls)             -> 0: []
GetAssets(avares://Avonic.Components/Controls/RippleEffect) -> 0: []
GetAssets(avares://Avonic.Themes/)                         -> 1: [avares://Avonic.Themes/!AvaloniaResourceXamlInfo]
GetAssets(avares://Avonic.Themes/Tokens)                   -> 0: []
Exists(avares://Avonic.Components/Controls/RippleEffect/AvonicRippleEffect.axaml) -> False
Exists(avares://Avonic.Themes/Tokens/AvonicTokens.axaml)   -> False
```

`ResourceInclude` still loads those same URIs, because it goes through the generated
`!XamlLoader.TryLoad` rather than the asset index. So `GetAssets` would have found nothing, and every
control would have silently lost its template.

## Design: a build-time manifest

`Directory.Build.targets` generates `XamlManifest.g.cs` from the `AvonicDiscoveredXaml` items that a
project declares. The result is an internal `string[]` of paths relative to the project root,
compiled into that assembly.

| Project | Globs | Merge order |
|---|---|---|
| `Avonic.Components` | `Controls/**/*.axaml` | ordinal |
| `Avonic.Themes` | `Tokens/**/*.axaml`, `Controls/**/*.axaml` | ordinal |

Order is applied at runtime, because MSBuild globs have no guaranteed order and Windows and Linux can
differ.

The issue asks for tokens first in Themes. That was implemented, then dropped for a single ordinal
sort: templates resolve tokens from the application and keys are unique, so it never changed which
value a lookup returned.

Sample pages use `[SamplePage("ion-…", Order = n)]`, found by reflection. `Order` is the control's
issue number, so parallel PRs don't have to coordinate; ties break on title. Reflection is acceptable
here because the sample is a desktop app that isn't trimmed.

### Alternatives rejected

- **Reflecting over the generated `Build:/…` methods.** Depends on private compiler naming and breaks
  under trimming, which matters for a mobile-first library.
- **A Roslyn source generator.** Needs a new project and analyzer packaging to produce one list.
- **Per-control `[assembly:]` attributes.** Bring back a registration step that is easy to forget.

The Themes globs are recursive, although the issue specifies one level. A template placed in a
per-control folder, as Components does, would otherwise be silently left out.

## Tests

| Test | Guards against |
|---|---|
| `AvonicComponentsStyles` resolves the ripple `ControlTheme` | Empty or broken discovery unstyling every control |
| `AvonicComponentsStyles.OrderForMerge` sorts ordinally (synthetic input) | Merge order varying by OS |
| `AvonicTheme` includes the tokens file | Themes discovery silently empty |
| `AvonicTheme`, installed, still resolves the ripple `ControlTheme` | Lost layering over the stubs |
| `AvonicTheme.OrderForMerge` sorts ordinally (synthetic input) | Merge order varying by OS |
| Every discovered key resolves with the theme installed | A bad path, a wrong root element, or a mistyped token that would only fail when first used |
| No key is defined twice within a project, per theme variant | Parallel PRs colliding on a key with no merge conflict to expose it |
| The catalog finds the ripple page | Sample discovery broken |
| Every page can be shown in a window under the theme | A page that crashes when tapped |
| The catalog sorts by `Order` then title, with omitted `Order` last | Index order varying between PRs |
| Every `Control` named `*Page` in the sample has `[SamplePage]` | A page silently missing from the index |

Ordering uses synthetic input because each project has too few real files to expose a wrong order.

## Deferred

- **A real Themes-over-stub override test.** No control has a Themes template yet, so the first one
  to add a template gets this coverage.
