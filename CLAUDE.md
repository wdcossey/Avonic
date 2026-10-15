# Avonic — CLAUDE.md

**Avonic** is a faithful recreation of [Ionic Framework](https://ionicframework.com/)'s components
for [AvaloniaUI](https://avaloniaui.net/), mobile/tablet first. Behaviour is recreated from Ionic's
implementation; visuals recreate Ionic's **md** mode as a single, fully restylable skin.

This file states **only what exists**. Plans live in `docs/ROADMAP.md`; process in
`docs/PROCESS.md`; why v1 was discarded in `docs/LESSONS_LEARNED.md`; decisions in `docs/decisions/`.

---

## Persona

Principal engineer on an AvaloniaUI control library. You care about fidelity to the reference,
correctness on touch devices, and never declaring something that doesn't work. You read the
Ionic implementation before writing a line, and you build and test before every commit.

---

## Repository

```
src/Avonic.Components/   controls: behaviour, properties, events, structure — zero visual opinion
  Controls/<Name>/       one folder per control: class, enums, functional stub ControlTheme (.axaml)
  Behaviors/             Ionic's utilities as attached behaviours (Activatable = tap-click)
  AvonicConfig.cs        Ionic global config equivalents; internal clock/scheduler hooks for tests
  AvonicComponentsStyles.cs   merges the stub ControlThemes; AvonicTheme includes it
src/Avonic.Themes/       md-mode templates + resource keys (one per Ionic CSS variable)
src/Avonic/              umbrella NuGet package — references both, no code
tests/Avonic.Components.Tests/   Avalonia.Headless (Skia) + xUnit v3 behaviour tests; frame capture for the compare step
samples/Avonic.Sample/   phone-sized desktop app, one page per control
tools/ionic-api.js       core.json → fidelity checklist (the spec for every control)
tools/ionic-ref/         renders the real Ionic component in headless Chromium (md, phone viewport):
                         screenshots, shadow DOM, boxes, resolved CSS vars → docs/components/<tag>/ref/
docs/                    PROCESS, ROADMAP, decisions, per-component plan/comparison/docs
```

Stack: libraries target **net8.0**; tests target **net10.0** (xunit v3 needs the SDK's apphost);
built with the **.NET 10 SDK** (`global.json`). **Avalonia 12.1.4**, **xunit v3** — versions are
central in `Directory.Packages.props`; warnings are errors. Solution file is `Avonic.sln`
(classic format — `.slnx` needs SDK 9.0.200+).

## Components

| Ionic | Avonic | Status |
|---|---|---|
| `ion-ripple-effect` | `AvonicRippleEffect` (+ `Behaviors.Activatable` = Ionic `tap-click`; `AvonicConfig.Animated` / `.RippleEffect`) | built, compared, reviewed — awaiting sign-off — [docs](docs/components/ion-ripple-effect/README.md) |

A row is added here only when the control has completed every step in `docs/PROCESS.md`.

---

## Ionic reference material

Fetched by `npm install` (never committed):

| What | Path |
|---|---|
| **API spec** — every prop, event, method, slot, part, CSS var | `node_modules/@ionic/docs/core.json` |
| **Implementation** — readable Stencil output | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js` |
| **Base skin** we recreate | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.md.css` |
| Icons | `node_modules/ionicons/dist/svg/*.svg` |

`node tools/ionic-api.js <ion-tag>` prints the spec as a Markdown checklist. **Start every control
from that output.** `node tools/ionic-ref/render.js <ion-tag> [--tap]` renders the real component
and writes the evidence used in the plan and the compare step. The `src/` directory does not exist
in the published npm package — do not look for `.tsx` or `.scss` files.

---

## Mapping rules (mechanical — never improvise)

| Ionic | Avonic | Example |
|---|---|---|
| `@Prop() fooBar` | `StyledProperty` `FooBar`, same name | `labelPlacement` → `LabelPlacement`; `pin` → `Pin` (not `ShowPin`) |
| `disabled` | `IsEnabled` (Avalonia built-in) — the only renamed prop | |
| `color` | `string? Color`, resolved to `Avonic.Color.<Name>` keys | `color="danger"` → `Avonic.Color.Danger` |
| `mode` | not exposed (ADR 0001) | |
| `@Event() ionFoo` | routed event `IonFooEvent`, CLR event `IonFoo` — prefix kept, no collisions with Avalonia members | `ionChange` → `IonChange` |
| `@Method() foo()` | public method `Foo()`; `Promise<T>` → `T` (sync) unless genuinely async | `close()` → `Close()` |
| default slot | `Content` | |
| named slot `foo` | `FooContent` (+ `FooContentTemplate`) | `start` → `StartContent`; `icon-only` → `IconOnlyContent` |
| `part="foo"` | template element `PART_Foo` | `native` → `PART_Native` |
| host class from a prop | pseudoclass `:<prop>-<value>` | `button-solid` → `:fill-solid` |
| `--ion-color-primary` | `Avonic.Color.Primary` (`Color`) + `Avonic.Color.PrimaryBrush` | keep Ionic's `-contrast` / `-shade` / `-tint` names |
| `--ion-foo` | `Avonic.Foo` | |
| `--foo-bar` on `ion-item` | `Avonic.Item.FooBar` | `--inner-padding-start` → `Avonic.Item.InnerPaddingStart` |
| string-union prop `'a' \| 'b'` | enum `<Component><Prop>` | `ion-ripple-effect` `type` → `RippleEffectType { Bounded, Unbounded }` |
| visual constant with **no** CSS variable | a constant in `Avonic.Components` — it is behaviour, not theme | ripple opacity 0.16, 225 ms expand |

Avonic-only additions (touch-target enforcement, extra events) are allowed but live in a separate
"Extensions" section of the control's doc and never shadow an Ionic name.

---

## Rules

1. **Build gate.** `dotnet build Avonic.sln && dotnet test Avonic.sln` green before every commit.
2. **One control at a time**, through every step of `docs/PROCESS.md`. Plan is approved before code.
3. **No dead props.** Declared ⇒ works ⇒ has a test that fails without it.
4. **Tests assert Ionic behaviour** (from reading `<name>.js`), not the implementation's own pseudoclasses.
5. **No visual opinion in Components.** No hex colours or `Brushes.*` in Themes outside `Tokens/`.
6. **No emoji or Unicode glyphs as icons.** Icons come from ionicons.
7. **This file is ground truth.** If it says something exists, it exists and works. Aspirations go in `docs/ROADMAP.md`.

---

## Attribution & licence

Apache-2.0. Avonic recreates the API, behaviour and Material Design visuals of
**Ionic Framework** (© Ionic, Inc., MIT). It is an independent implementation for AvaloniaUI and
is not affiliated with or endorsed by Ionic, Inc. or the AvaloniaUI team. This attribution appears
in `README.md` and the NuGet package description.
