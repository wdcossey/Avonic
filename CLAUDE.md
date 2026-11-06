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
  Controls/Flex/         FlexRow and FlexSlot: display: flex and <slot>, for the parts in a host's template
  Behaviors/             Ionic's utilities as attached behaviours (Activatable = tap-click;
                         FocusVisible = focus-visible; IonStyle = the internal ionStyle event;
                         ColorPalette = the `color` prop, see docs/infra/color-system/README.md;
                         Css = var(), em, padding and transform values for themes)
  AvonicConfig.cs        Ionic global config equivalents; internal clock/scheduler hooks for tests
  Icons/                 generated: all of ionicons, compressed, for AvonicIcon (tools/ionicons/generate.js)
  AvonicComponentsStyles.cs   merges every discovered Controls/**/*.axaml; AvonicTheme includes it
src/Avonic.Themes/       md-mode templates + resource keys (one per Ionic CSS variable); AvonicTheme
                         merges every discovered Tokens/**/*.axaml, Controls/**/*.axaml and Styles/**/*.axaml
  Tokens/Colors.axaml    Ionic's palette; Tokens/Global.axaml: global --ion-* variables
  Tokens/Palettes/       merged by the consumer, not the theme (Dark.axaml)
  Styles/                <Styles> files for ::slotted rules, which a ControlTheme can't reach
src/Avonic/              umbrella NuGet package — references both, no code
tests/Avonic.Components.Tests/   Avalonia.Headless (Skia) + xUnit v3 behaviour tests; frame capture for the compare step;
                                 Performance/ holds the style budget (docs/infra/performance/README.md)
samples/Avonic.Sample/   phone-sized desktop app, one page per control, each marked [SamplePage]; a Debug build
                         shows what each page cost to open (Diagnostics/)
Directory.Build.targets  generates XamlManifest.Paths from a project's AvonicDiscoveredXaml globs
tools/ionic-api.js       core.json → fidelity checklist (the spec for every control)
tools/ionic-ref/         renders the real Ionic component in headless Chromium (md, phone viewport):
                         screenshots, shadow DOM, boxes, resolved CSS vars → docs/components/<tag>/ref/;
                         compare-icons.js compares every icon against it, pixel by pixel; side.js puts two
                         frames side by side
tools/ionicons/          generates src/Avonic.Components/Icons/ from the ionicons package
tools/breakage/          breaks each behaviour in a copy of the repo, and checks that a test notices
tools/real-window/       drives the real sample window with real input and checks it; takes over the PC, so ask first
docs/                    PROCESS, ROADMAP, decisions, per-component plan/comparison/docs; infra/swarm-brief.md
                         for a batch of agents, infra/avalonia-issues.md for what to report to Avalonia
.mcp.json                Claude Code MCP servers: avalonia-docs (Avalonia's hosted Build MCP — docs and API lookup)
```

A control adds only its own files; templates, tokens and sample pages are auto-discovered
([convention](docs/infra/auto-discovery/README.md)).

Stack: libraries target **net8.0**; tests target **net10.0** (xunit v3 needs the SDK's apphost);
built with the **.NET 10 SDK** (`global.json`). **Avalonia 12.1.4**, **xunit v3** — versions are
central in `Directory.Packages.props`; warnings are errors. Solution file is `Avonic.sln`
(classic format — `.slnx` needs SDK 9.0.200+).

## Components

| Ionic | Avonic | Status |
|---|---|---|
| `ion-ripple-effect` | `AvonicRippleEffect` (+ `Behaviors.Activatable` = Ionic `tap-click`; `AvonicConfig.Animated` / `.RippleEffect`) | done (signed off 2026-10-15) — [docs](docs/components/ion-ripple-effect/README.md) |
| `ion-icon` | `AvonicIcon` (`AvonicIcon.AddIcons` = Ionic `addIcons`) | done — [docs](docs/components/ion-icon/README.md) |
| `ion-item` | `AvonicItem` (+ `IItemControl`, how a control tells an item about itself) | done — [docs](docs/components/ion-item/README.md) |
| `ion-label` | `AvonicLabel` | done — [docs](docs/components/ion-label/README.md) |
| `ion-note` | `AvonicNote` | done — [docs](docs/components/ion-note/README.md) |
| `ion-text` | `AvonicText`, and `AvonicTextSpan` for words in a sentence | done — [docs](docs/components/ion-text/README.md) |
| `ion-list` | `AvonicList` | done — [docs](docs/components/ion-list/README.md) |
| `ion-list-header` | `AvonicListHeader` | done — [docs](docs/components/ion-list-header/README.md) |
| `ion-item-divider` | `AvonicItemDivider` | done — [docs](docs/components/ion-item-divider/README.md) |
| `ion-item-group` | `AvonicItemGroup` | done — [docs](docs/components/ion-item-group/README.md) |
| `ion-button` | `AvonicButton` (+ `ButtonInner`, its template's inner part; `Command`, `TextTransform` as extensions) | done — [docs](docs/components/ion-button/README.md) |
| `ion-content` | `AvonicContent` | done — [docs](docs/components/ion-content/README.md) |
| `ion-card` | `AvonicCard`, `AvonicCardHeader`, `AvonicCardTitle`, `AvonicCardSubtitle`, `AvonicCardContent` | done — [docs](docs/components/ion-card/README.md) |

Every row has completed every step in `docs/PROCESS.md`. Rows are added in a roll-up at the end of
each priority tier, not by the control's own PR, so a recently finished control may be missing. For
the latest, see the `Status:` line in `docs/components/<ion-tag>/README.md`
([process](docs/PROCESS.md#rolling-up-the-tables)).

---

## Ionic reference material

Fetched by `npm install` (never committed):

| What | Path |
|---|---|
| **API spec** — every prop, event, method, slot, part, CSS var | `node_modules/@ionic/docs/core.json` |
| **Implementation** — readable Stencil output | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js` |
| **Base skin** we recreate | `node_modules/@ionic/core/dist/collection/components/<name>/<name>.md.css` |
| Icons | `node_modules/ionicons/dist/svg/*.svg`; `ion-icon` itself is in `node_modules/ionicons/dist/collection/components/icon/`, not `@ionic/core` or `core.json` |

`node tools/ionic-api.js <ion-tag>` prints the spec as a Markdown checklist. **Start every control
from that output.** `node tools/ionic-ref/render.js <ion-tag> [--tap]` renders the real component
and writes the evidence used in the plan and the compare step. The `src/` directory does not exist
in the published npm package — do not look for `.tsx` or `.scss` files.

---

## Mapping rules (mechanical — never improvise)

| Ionic | Avonic | Example |
|---|---|---|
| `@Prop() fooBar` | `StyledProperty` `FooBar`, same name | `labelPlacement` → `LabelPlacement`; `pin` → `Pin` (not `ShowPin`) |
| `disabled` | `IsEnabled` (Avalonia built-in) | |
| `name` | `<Control>Name`, because `StyledElement.Name` exists | `ion-icon` → `IconName` |
| `color` | `string? Color` via `ColorPalette.ColorProperty.AddOwner`, resolved to `Avonic.Color.<Name>` keys | `color="danger"` → `Avonic.Color.Danger` |
| `.ion-color`, `--ion-color-base` | pseudoclass `:ion-color`, `ColorPalette.Base` (also `Contrast`, `Shade`, `Tint`, `BaseColor`, `ContrastColor`) | `:host(.ion-color)` → `^:ion-color` |
| `mode` | not exposed (ADR 0001) | |
| `@Event() ionFoo` | routed event `IonFooEvent`, CLR event `IonFoo` — prefix kept, no collisions with Avalonia members | `ionChange` → `IonChange` |
| `@Method() foo()` | public method `Foo()`; `Promise<T>` → `T` (sync) unless genuinely async | `close()` → `Close()` |
| default slot | `Content` | |
| Many children, with no slot in `core.json` | `Items` | `ion-list` → `AvonicList.Items` |
| named slot `foo` | `FooContent` (+ `FooContentTemplate`) | `start` → `StartContent`; `icon-only` → `IconOnlyContent` |
| `part="foo"` | template element `PART_Foo` | `native` → `PART_Native` |
| host class from a prop | pseudoclass `:<prop>-<value>` | `button-solid` → `:fill-solid` |
| `--ion-color-primary` | `Avonic.Color.Primary` (`Color`) + `Avonic.Color.PrimaryBrush` | keep Ionic's `-contrast` / `-shade` / `-tint` names |
| `--ion-foo` | `Avonic.Foo` | a literal colour is `Color` + `Avonic.FooBrush` |
| `--ion-foo` falling back to another variable | `Avonic.FooBrush` only, following that variable | `--ion-item-background` → `Avonic.ItemBackgroundBrush` |
| `--foo-bar` on `ion-item` | `Avonic.Item.FooBar` | `--inner-padding-start` → `Avonic.Item.InnerPaddingStart` |
| `--foo` falling back to something else | key set to `<b:CssUnset />`, read through `Css.Var` with the fallback as the next value | `--color-hover: var(--color)`; `currentColor` is the host's `Foreground` |
| `1.25em` | `Css.Em` on the font size | `--detail-icon-font-size` → factor `1.25` |
| `color: inherit` | `$parent[Control].(TextElement.Foreground)`, as the last value of a `Css.Var` | A binding that yields nothing gives the default, not the inherited value |
| `translateY(50%) scale(0.75)` | `Css.TranslateY` `0.5` and `Css.Scale` `0.75`, read by `Css.TranslateScale` with `Bounds.Height` | Ease those two, not the transform: a change of size moves it at once in CSS |
| CSS specificity | a rule with a pseudoclass or class beats one without, then later beats earlier | Global `Styles` beat a `ControlTheme`, and files in `Styles/` merge in ordinal order |
| `:host-context(.foo)` | a descendant selector, in `Styles/` | `:host-context(.item-input)` → `c\|AvonicItem:item-input c\|AvonicLabel` |
| A class the app sets | a style class of the same name | `class="ion-text-wrap"` → `Classes="ion-text-wrap"` |
| `<h1>`…`<h6>`, `<p>` | `TextBlock` with that style class | `<TextBlock Classes="h2" />` |
| A CSS rule for every descendant, on text | `>`, and `> :is(Panel) >`, not a descendant selector | A rule for a `TextBlock` is checked for every `TextBlock` in the app, and a descendant selector walks up its ancestors |
| Commas between selectors | Each alternative costs every element in the app about 30 ns, so prefer fewer. `StyleBudgetTests` holds the total | `> :is(Panel) >` is a second alternative for each target |
| `:host-context(.foo)` in a `ControlTheme` | Not possible: `c\|Host ^` compiles, ignores what's left of `^`, and applies everywhere | Use `Styles/` |
| `::slotted(ion-foo[slot=start])` | `c\|AvonicHost /template/ ContentPresenter#PART_Start > c\|AvonicFoo`, in `Styles/` | `>` means direct children, as `::slotted` does |
| `.ion-focusable`, `.ion-focused` | `FocusVisible.IsEnabled`, `:ion-focused` | |
| `outline: none` | `FocusAdorner` set to `{x:Null}` in the theme | Unset falls back to the base theme's dashed rectangle. It reads null either way, so a test has to check the `AdornerLayer` |
| `.ion-activatable:hover` | `:ion-hover`, from `Activatable`, for mouse and pen only | not `:pointerover`, which also applies during a touch |
| A native `<button>`'s click, with no Ionic member | extension `Click` | not `IonClick` |
| Binding a button-like control to a command, with no Ionic member | extensions `Command`, `CommandParameter`, following Avalonia's `Button` | on `AvonicButton` |
| string-union prop `'a' \| 'b'` | enum `<Component><Prop>` | `ion-ripple-effect` `type` → `RippleEffectType { Bounded, Unbounded }` |
| visual constant with **no** CSS variable | a literal in the theme, so that it can be restyled | a card's margin of 10 |
| a constant a control's code needs | a constant in `Avonic.Components` | ripple opacity 0.16, 225 ms expand |
| `:last-of-type` | `:nth-last-child(1)`, and `ContentPresenter:nth-last-child(1) > c\|AvonicFoo` for templated items | Not of-type: Avalonia has no such selector |
| `:only-child` | `:nth-child(1):nth-last-child(1)` | `:nth-last-child(1)` alone is also the last of several |
| `.foo + .bar` | a pseudoclass the control works out from its parent's `Children` | `.list-inset + ion-list.list-inset` → `:after-inset`; `ion-card-header + .card-content-md` → `:after-card-header` |
| `position: sticky` | `RenderTransform` from `LayoutUpdated` at style priority, in the control | `AvonicItemDivider.Sticky`. It follows the nearest `ScrollViewer`, as CSS follows the nearest scroll container |
| `background-clip: border-box` under a translucent border | `BackgroundSizing="OuterBorderEdge"` | CSS's default, not Avalonia's |
| `role` from `hostContext` | `AutomationProperties.ControlTypeOverride` in `Styles/`, or the peer if the control is yours | |
| `role="heading"`, `aria-level` | `AutomationProperties.HeadingLevelProperty.OverrideDefaultValue<T>(n)`, with a peer of type `Text` | `ion-card-title` → 2 |
| An element that's inline in a sentence | An `Inline` beside the control, `Avonic<Name>Span : Span`, in a `TextBlock` | `ion-text` → `AvonicText` and `AvonicTextSpan` |
| `display: flex`, in a row | `FlexRow`, with a `<slot>` in it as `FlexSlot`, which is styled as a `ContentPresenter` | `AvonicItem`'s parts. Not `Auto` and `*` columns, which can't shrink or share |
| `flex: 1` | `FlexRow.Flex`, set on the part or the slotted child. 0 is `flex: initial` | `::slotted(ion-label:not([slot=end]))` → `c:FlexRow.Flex` in `Styles/Label.axaml` |
| `align-items` on a flex row | `VerticalContentAlignment` on its `FlexSlot`s | `.item-label-stacked` → `Top` |
| `align-self` on a slotted child | Its `VerticalAlignment`, which `FlexSlot` honours once it's set | `ion-note` → `Top`. A control aligned some way by default uses `OverrideDefaultValue`, not its theme, whose value counts as set: `AvonicButton`, `AvonicIcon` |
| `overflow: hidden` on a flex item | `ClipToBounds` set on it, as `FlexRow` reads it to take the minimum away | `.ion-text-nowrap`. A `TemplatedControl` clips by default, and that doesn't count |
| A slotted child with no height of its own, in a panel | It collapses, as `align-items: center` does in CSS. `VerticalAlignment="Stretch"` on it fills the panel | A background behind text: put the `Grid` in a `Border`, which isn't a panel |
| `::slotted(ion-foo:not(:first-child))` | `c\|Host > :is(Panel) > c\|Foo:not(:nth-child(1))` | a panel stands for the slot |
| `ion-foo.ion-color .ion-inherit-color` | `c\|Foo:ion-color c\|Bar`, with `Foreground` bound to `$parent[Control].(TextElement.Foreground)` | after `:ion-color`, which it beats |
| `--ion-safe-area-top` | `Avonic.SafeAreaTop` (`double`), 0 by default, as Avalonia pads its root view itself. The app fills them | also `Bottom`, `Left`, `Right`. No control reads them yet: `AvonicItem` will, in #72 |
| `requestAnimationFrame`, `readTask` | `AvonicConfig.Schedule` at 16 ms | so `TestClock` asserts the timeline |
| `overflow: hidden` on a scroller's vertical axis | `Hidden`, with input blocked in the control, not `Disabled`, which resets the position | `ScrollY="False"` |
| `position: absolute` with no inset, on a slot's child | the child's alignments, at style priority, so an app's value wins | `slot="fixed"` → `Left`, `Top` |
| `--foo` with a different default per host class | key set to `<b:CssUnset />`, default from an `Avonic.<Name>.Md.*` key as `Css.Var`'s last value | `--background-hover-opacity` → `Avonic.Button.Md.BackgroundHoverOpacitySolid` |
| `color` on a shadow part, inheriting the host's | `TextElement.Foreground` on the part, falling back to the host's `Foreground` | `.button-native { color: var(--ion-color-contrast) }` |
| `overflow: hidden` beside `box-shadow` | a part of its own inside the `Border`: a `Border`'s `ClipToBounds` clips its own shadow, and a `TemplatedControl` clips by default, so turn that off | `PART_Clip`, `ClipToBoundsProperty.OverrideDefaultValue` |
| `line-height: 1` | `TextBlock.LineHeight` as `Css.Em`, `ClipToBounds="False"`, and half the leading as a margin, because Avalonia keeps a short line at the top | `AvonicButton.HalfLeadingConverter` |
| `text-transform` | a property, applied to strings through the template's converter | `TextTransform` |

Avonic-only additions (touch-target enforcement, extra events) are allowed but live in a separate
"Extensions" section of the control's doc and never shadow an Ionic name.

---

## Rules

1. **Build gate.** `dotnet build Avonic.sln && dotnet test Avonic.sln` green before every commit.
2. **One control or family per PR**, through every step of `docs/PROCESS.md`. Plan is written before code and reviewed at the PR.
3. **No dead props.** Declared ⇒ works ⇒ has a test that fails without it.
4. **Tests assert Ionic behaviour** (from reading `<name>.js`), not the implementation's own pseudoclasses.
5. **No visual opinion in Components.** No hex colours or `Brushes.*` in Themes outside `Tokens/`.
6. **No emoji or Unicode glyphs as icons.** Icons come from ionicons.
7. **This file is ground truth.** If it says something exists, it exists and works. Aspirations go in `docs/ROADMAP.md`.
8. **Faithful, with deviations explained.** Avalonia doesn't offer every technique html, CSS and JS do, so a deviation it forces is allowed, if it's recorded with its measurement and the reason. It isn't a licence to skip measuring, or to deviate where Avalonia could do it ([ADR 0002](docs/decisions/0002-decisions-from-the-first-swarm.md)).

---

## Attribution & licence

Apache-2.0. Avonic recreates the API, behaviour and Material Design visuals of
**Ionic Framework** (© Ionic, Inc., MIT). It is an independent implementation for AvaloniaUI and
is not affiliated with or endorsed by Ionic, Inc. or the AvaloniaUI team. This attribution appears
in `README.md` and the NuGet package description. The bundled artwork is **Ionicons** (MIT); its
notice ships at `src/Avonic.Components/Icons/LICENSE`.
