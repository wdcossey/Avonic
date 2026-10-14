# Avonic v1 — Lessons Learned

> Written October 2026, after a review of the v1 codebase (Tiers 1–4, 27 controls, ~163 tests)
> against the stated goal: **a faithful recreation of Ionic Framework's components for AvaloniaUI.**
>
> Verdict: v1 does not meet that goal and cannot be incrementally fixed into meeting it. The
> gap is not polish — it is that the components were never built *from* Ionic. This document
> records why, so v2 starts from the right place.

---

## 1. What actually happened

### 1.1 The reference pipeline was broken from day one — and nobody checked

`CLAUDE.md` and `CHAT.md` both instruct: *"always read `node_modules/@ionic/core/src/components/<name>/<name>.tsx` first."*
**That path does not exist.** The published `@ionic/core` npm package does not ship `src/`.

What *is* in the package, and was never used:

| Real reference | What it gives you |
|---|---|
| `node_modules/@ionic/docs/core.json` | Every component's `props`, `events`, `methods`, `slots`, `parts`, `styles` (CSS vars) — the complete public API, machine-readable |
| `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js` | Unminified Stencil output: the real `render()`, gesture logic, state handling |
| `node_modules/@ionic/core/dist/collection/components/<name>/<name>.ios.css` / `.md.css` | Compiled per-mode styling |

Consequence: every v1 component was written from memory of what Ionic *roughly* does. The
"read the Ionic source first" step was silently skipped for all 27 controls, because the
source wasn't there and the failure was never surfaced.

### 1.2 Fidelity to Ionic is 25–65% per component

Audit of six representative pairs against `core.json` 8.8.1:

| Component | Coverage | Headline gaps |
|---|---|---|
| `ion-item` | ~30% | No `color`, `href`/routing, `detailIcon`, `type`. 0/31 CSS vars. Doesn't forward click to first interactive child. Ripple commented out. |
| `ion-button` | ~40% | **No `Click` or `Command`** — a button with no click. No `href`, `type`, `form`, `icon-only` slot. 0/23 CSS vars. No keyboard activation. |
| `ion-input` | ~25% | No `type` (no password/email/number), `required`, `pattern`, `debounce`, `clearOnEdit`… 21 props missing. `ClearInput` and `Counter` are **declared but dead code**. `ionChange` (commit) missing. 0/18 CSS vars. |
| `ion-range` | ~40% | No `dualKnobs`, `pinFormatter`. `ShowTicks` **renders nothing**. Pin **never positioned** over knob. No keyboard. Changing `Min`/`Max` doesn't re-clamp. |
| `ion-list` | ~65% | `UpdateItemBorders` overwrites every child's `Lines` (contradicts its own doc comment). Handlers stack on re-template. Ignores `ItemsSource`. |
| `ion-toggle` | ~45% | No `fixed` label placement, `enableOnOffLabels`, `helperText`/`errorText`. Ionic fires `ionChange` + haptic on each flip during a drag; Avonic fires once on release. No RTL. |

Systemic, across **every** component:
- No `color` property (Ionic's primary theming axis)
- No `mode` property (see §1.4)
- **Zero** of Ionic's CSS custom properties mapped to resource keys
- No `ionFocus` / `ionBlur` equivalents; no focus or keyboard handling at all
- No imperative methods (`setFocus`, `closeSlidingItems`, …)
- Events renamed inconsistently (`ionChange` → `CheckedChanged` here, `ValueChanged` there)
- Invented API with no Ionic counterpart (`StartContentTemplate`, `MinimumTouchTarget`, `Pressed`/`Released`/`Cancelled`)

### 1.3 Declared-but-dead features

Four properties exist on the public API, appear in docs and samples, and do nothing:
`AvonicInput.ClearInput`, `AvonicInput.Counter`, `AvonicRange.ShowTicks`, and the Range pin position.
A missing prop is honest. A dead prop is a bug that ships as a feature.

### 1.4 The founding decisions made "faithful" impossible by design

`CHAT.md` §Key Decisions explicitly chose:
- **"No iOS/MD adaptive rendering"** — but `mode="ios" | "md"` is on every Ionic component and is half of Ionic's identity.
- **"shadcn-style token theming"** — but Ionic's theming *is* its `--ion-*` / per-component CSS variable system.

Those decisions were reasonable for the project `CHAT.md` describes ("Ionic *behaviour*, shadcn *theming*"). They are incompatible with the project the author actually wants ("faithful recreation"). v1 was built to the wrong brief, and the brief was never revisited. **This is the single most important thing to settle before writing v2 code** — see §3.1.

### 1.5 Nothing was ever built or tested

- The cloud environment has no `dotnet` SDK. The last session committed **51 files / ~3,750 lines across two commits without a single compile.** The `AvonicSpinner.Name` XAML clash was caught by eye, not a compiler.
- There is no CI. `.github/` does not exist.
- 163 test methods exist. There is no evidence in the history that they have ever been run.
- Tests were written *against the implementation* ("setting X flips pseudoclass `:x`"), not against Ionic behaviour ("ticks are rendered at each step"). They would pass against the dead features above.

### 1.6 Documentation described aspiration as fact

`CLAUDE.md` / `README.md` state as present:
- "functional stub template in `Avonic.Components`" — Components contains **no** `.axaml` files
- "derived brush generation from seed colour (computed at theme init)" — no such code exists; tokens are ~10 hardcoded hex values
- "`UseAvonic()` / `AppBuilder` extension" — `src/Avonic/` contains only a `.csproj` with two references and no NuGet metadata
- README: "Ring (focus indicator)", "Accent", "Density" tokens — none defined

An LLM reading `CLAUDE.md` as ground truth will confidently build on things that aren't there. It did.

### 1.7 Breadth over depth

27 controls at 25–65% instead of 5 at 95%. Tier 4 was started while Tier 3 had dead features. The roadmap rewarded ticking boxes.

### 1.8 Quality shortcuts that compound

- **Emoji as icons** in theme templates (`🔍` searchbar, `✕` clear, `›` detail, `✓` check, `🔈🔊` range). `ionicons` is already in `node_modules` as a dependency of `@ionic/core` and was never used.
- Hardcoded `Foreground="White"` and hex colours inside theme style files.
- `AvonicSpinner` `Bubbles` / `Circles` silently alias `Lines`.
- `AvonicProgressBar` indeterminate animation uses fixed pixel offsets (`-200 → 600`) — wrong at any width ≠ ~400.
- `AvonicItemSliding.AnimateToTranslate()` doesn't animate.
- `AvonicSearchbar` allocates a new `DispatcherTimer` per keystroke.
- One 1,000-line `MainWindow.axaml` "sample" instead of per-component pages.

### 1.9 Process

- Big-bang commits (27 files in one) with no incremental verification.
- Scope was 27 of Ionic's **95** components without ever saying so.
- Reviews asked "did you add tests/previews/docs?" *after* the code was written, and got "yes" — the checklist was satisfied, the goal was not.

---

## 2. What is worth keeping

- The name, attribution text, Apache-2.0 licence, GitHub repo
- The three-project split (`Components` / `Themes` / umbrella) — correct idea, just unexecuted
- Tier ordering: primitives (ripple, pressable, safe area) before atoms before composites — correct
- The `[PseudoClasses]` + `OnPropertyChanged` + `OnApplyTemplate` pattern for state → pseudoclass
- Avalonia Headless + xUnit test harness (`TestApp.cs`, `[AvaloniaFact]`)
- `Design.PreviewWith` blocks as a habit
- `package.json` pinning `@ionic/core` + `@ionic/docs` as the reference — **with the corrected paths**
- The touch-first additions (48dp min target, press/cancel semantics) — as clearly-labelled *extensions*, not as the API

---

## 3. Principles for v2

### 3.1 Decide the ios/md question first, explicitly, in writing

Three honest options:

| Option | What it means | Cost |
|---|---|---|
| **A. Full fidelity** | `Mode` property on every control; `*.ios.axaml` + `*.md.axaml` theme variants mirroring Ionic's two CSS files | ~2× theme work. This *is* Ionic. |
| **B. API fidelity, one skin** | Every prop/event/method/slot/CSS-var from `core.json`, implemented; a single visual language, but every `--ion-*` var exists as a resource key so an ios/md theme *could* be layered later | Behaviour-faithful; visuals won't match screenshots |
| **C. v1 philosophy** | Ionic-*inspired*, shadcn theming, no `mode` | Not a faithful recreation. Don't call it one. |

Recommendation: **A** if "faithful" is the goal (it was stated as such). **B** is the pragmatic v2.0 with A as v2.x, *provided* the token keys are named for it from day one. C is what v1 was.

### 3.2 `core.json` is the spec. Every component starts from it.

Write `tools/ionic-api.js <ion-tag>` (≈30 lines of Node) that prints a checklist of props / events / methods / slots / parts / CSS vars. The checklist is pasted into the component's doc page as a fidelity table and **every row is ticked or has a written reason it's omitted.** No component is "done" below ~90%.

### 3.3 CSS variables map mechanically to resource keys

`ion-item`'s `--background` → `Avonic.Item.Background`; `--detail-icon-color` → `Avonic.Item.DetailIconColor`; `--ion-color-primary` → `Avonic.Color.Primary`. One rule, no judgement calls, no ad-hoc tokens. Ionic's colour palette (`primary secondary tertiary success warning danger light medium dark`) + `color` property on every control, exactly as Ionic.

### 3.4 Names map mechanically too

`ionChange` → `Change`, `ionInput` → `Input`, `ionFocus` → `Focus`, `ionBlur` → `Blur`. Props keep Ionic's name in PascalCase (`labelPlacement` → `LabelPlacement`, `pin` → `Pin` — not `ShowPin`). Slots keep Ionic's name (`start`, `end`, `icon-only`, `label`). Write the mapping table once in `CLAUDE.md`; never improvise.

### 3.5 Build gate before any component

In order:
1. `dotnet` in the cloud environment (setup script / `SessionStart` hook)
2. GitHub Actions: build + test on every push, all three TFMs
3. **No commit without a green local build and test run.** No exceptions for "it's just XAML."

### 3.6 Depth over breadth; vertical slices

One component = C# + theme + `Design.PreviewWith` + behaviour tests + doc page with fidelity table + sample page — in **one** PR, built and run. Five real components before a sixth is started. Proposed order: `ion-ripple-effect` → `ion-item` → `ion-label` → `ion-list` → `ion-button` → `ion-icon` (via ionicons).

### 3.7 No dead props

If it's declared, it works, and there is a test that **fails** when it doesn't. Tests assert Ionic behaviour (from reading `<name>.js`), not the implementation's own pseudoclasses.

### 3.8 Icons are infrastructure, not emoji

`ionicons` SVGs are in `node_modules/ionicons/dist/svg/`. Build `AvonicIcon` + a generator early (Tier 0/1). Every chevron, checkmark, clear-button and search glyph comes from it.

### 3.9 Extensions are labelled

Touch-target enforcement, press/cancel events, `StartContentTemplate` — fine to have, but in a documented *"Avonic extensions"* section per component, never mixed into the Ionic-mapped API.

### 3.10 `CLAUDE.md` states only what exists

Aspirations go in a `ROADMAP.md`. If `CLAUDE.md` says "X exists at path P", P exists and X works. An agent treats that file as ground truth; it must *be* ground truth.

### 3.11 Scope is explicit

Ionic has 95 components. Say which are in v2.0, which are later, and which are out (router, nav, split-pane are probably out). Breadth is a roadmap column, not an implicit promise.

---

## 4. Suggested v2 bootstrap order

```
Tier 0 — Tooling (nothing else until this is green)
  ├─ dotnet in cloud env; GitHub Actions build+test
  ├─ tools/ionic-api.js  — core.json → fidelity checklist
  ├─ tools/ionicons      — SVG → AvonicIcon resources
  ├─ CLAUDE.md rewritten: correct reference paths, mapping rules, ios/md decision, scope
  └─ Decision record: ios/md (§3.1)

Tier 1 — Foundation (full fidelity each, one PR each)
  ├─ ion-ripple-effect
  ├─ ion-icon (ionicons)
  ├─ ion-item  ← the template for how every component is done
  ├─ ion-label
  ├─ ion-list / ion-list-header / ion-item-divider / ion-item-group
  └─ ion-button

Tier 2+ — only after Tier 1 is ≥90% with green CI
```

---

*The code in v1 is not wasted — it taught us exactly which shortcuts to refuse. But it should not be the base for v2.*
