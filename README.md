# Avonic

A faithful recreation of [Ionic Framework](https://ionicframework.com/)'s components for
[AvaloniaUI](https://avaloniaui.net/) — mobile and tablet first, desktop supported.

> **Status: v2 bootstrap.** No components yet. v1 was reviewed and discarded; see
> [`docs/LESSONS_LEARNED.md`](docs/LESSONS_LEARNED.md). Components are added one at a time via
> [`docs/PROCESS.md`](docs/PROCESS.md); the order is in [`docs/ROADMAP.md`](docs/ROADMAP.md).

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

Targets .NET 8, Avalonia 12.

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

## Licence

Apache License 2.0 — see [LICENSE](LICENSE).
