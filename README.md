# Avonic

A faithful recreation of [Ionic Framework](https://ionicframework.com/)'s components for
[AvaloniaUI](https://avaloniaui.net/) — mobile and tablet first, desktop supported.

<table>
  <tr>
    <td><img src="docs/preview/showcase-inbox-light.png" width="190" alt="An inbox: a searchbar, messages with avatars, notes and badges, one swiped open" /></td>
    <td><img src="docs/preview/showcase-discover-dark.png" width="190" alt="A feed of cards with pictures, chips and a fab, in dark" /></td>
    <td><img src="docs/preview/showcase-profile-light.png" width="190" alt="A form: a segment, inputs, a textarea, radios, a checkbox and a button" /></td>
    <td><img src="docs/preview/showcase-settings-dark.png" width="190" alt="Settings: toggles, a range, a select, notes and a progress bar, in dark" /></td>
  </tr>
</table>

Built only from Avonic controls, rendered by Avalonia, in light and Ionic's dark palette. More in **[PREVIEW.md](PREVIEW.md)**:
every screen in both, and a page for each control.

> **Status: in development, with no release yet.** 54 of Ionic's components and families are done, 79 of its elements in all:
> items and lists, buttons, cards, toolbars, the grid, form controls from checkbox to input and searchbar, segment, tabs, fab,
> sliding items, accordion, reorder, refresher, infinite scroll, picker, the app's root, `ion-app`, the overlays alert, action
> sheet, modal (with its sheet) and popover, and select. Each went through every step of
> [`docs/PROCESS.md`](docs/PROCESS.md). The list, with a link to each one's docs, is in [`CLAUDE.md`](CLAUDE.md#components),
> rolled up at the end of each tier, so the newest may be missing; the `Status:` line of each `docs/components/<tag>/README.md`
> is current. Toast, loading, menu, nav, split pane, datetime and back button are next, in
> [`docs/ROADMAP.md`](docs/ROADMAP.md). v1 was reviewed and discarded; see [`docs/LESSONS_LEARNED.md`](docs/LESSONS_LEARNED.md).

## What "faithful" means here

- **Behaviour** is recreated from Ionic's implementation — gestures, state, events, methods.
- **API** mirrors Ionic's: every prop, event, method and slot from `@ionic/docs` is present or has
  a written reason it isn't (`node tools/ionic-api.js <ion-tag>` generates the checklist).
- **Visuals** recreate Ionic's Material Design (`md`) mode as one skin. Every Ionic CSS variable is
  an `Avonic.*` resource key, so you can restyle without forking templates.
  ([ADR 0001](docs/decisions/0001-single-md-skin.md))

## Packages

| Package | Contents |
|---|---|
| `Avonic` | Everything — the one most apps want |
| `Avonic.Components` | Controls only, no visual opinion — bring your own theme |
| `Avonic.Themes` | The md-mode theme and resource keys |

Targets .NET 8, Avalonia 12. None is on NuGet yet: until the first release, reference the projects from a clone.

## Building

```sh
npm install                   # Ionic reference material (not committed)
dotnet build Avonic.sln
dotnet test  Avonic.sln
```

## Attribution

Avonic recreates the API, behaviour and Material Design visuals of **Ionic Framework**
(© Ionic, Inc., MIT licensed). It is an independent implementation for the AvaloniaUI ecosystem and
is not affiliated with or endorsed by Ionic, Inc. or the AvaloniaUI team.

Icons are [Ionicons](https://ionic.io/ionicons) (© Ionic, MIT licensed), bundled unmodified. The notice is
in [`src/Avonic.Components/Icons/LICENSE`](src/Avonic.Components/Icons/LICENSE).

## Licence

Apache License 2.0 — see [LICENSE](LICENSE).
