# ion-ripple-effect → `AvonicRippleEffect` — Plan

> Status: **awaiting approval**. No code is written until this plan is signed off.
> Sources read: `dist/collection/components/ripple-effect/ripple-effect.js` + `.css`,
> `dist/collection/utils/tap-click/index.js`, `@ionic/docs` core.json 8.8.1.
> Reference render: [`ref/README.md`](ref/README.md) (`node tools/ionic-ref/render.js ion-ripple-effect --tap`).

## What it is

A presentation-only overlay that fills its parent and draws expanding "ink" circles on request.
It does **not** listen to pointer input itself. In Ionic a global utility (`tap-click`) watches
`pointerdown`/`pointerup`/`pointercancel` on every `.ion-activatable` surface, toggles an
`ion-activated` class, and calls `addRipple(x, y)` on the surface's `ion-ripple-effect`.
`ion-item`, `ion-button`, `ion-segment-button`, `ion-fab-button`, … are activatable surfaces.

So this control comes in two parts, and both are in this PR:

1. **`AvonicRippleEffect`** — the component (checklist below).
2. **`Activatable`** — the tap-click behaviour, as an internal, reusable piece that `ion-item`
   and `ion-button` will attach in their own PRs. Without it the ripple cannot be demonstrated
   or compared. (v1 called a version of this `AvonicPressable` and bolted invented API onto it.)

## Fidelity checklist (`node tools/ionic-api.js ion-ripple-effect`)

### Props (1)

| ✓ | Ionic | Avonic | Type | Default | Notes |
|---|---|---|---|---|---|
| [x] | `type` | `Type` | `RippleEffectType { Bounded, Unbounded }` | `Bounded` | pseudoclasses `:type-bounded` / `:type-unbounded` |

### Events (0) — none. Methods (1)

| ✓ | Ionic | Avonic | Notes |
|---|---|---|---|
| [x] | `addRipple(x, y) => Promise<() => void>` | `Action AddRipple(double x, double y)` | `x`,`y` in the ripple's own coordinate space (Ionic takes client coords and subtracts its rect — same thing). Returns the **release** action synchronously; see timing below for how the `Promise`'s 325 ms is preserved. |

### Slots (0), parts (0), CSS variables (0) — none.

Every visual constant below is hard-coded in Ionic's CSS with no variable. They are therefore
**constants in `Avonic.Components`**, not theme resource keys (see decision D2).

## Behaviours to reproduce (each becomes a test)

From `ripple-effect.js`:

| # | Behaviour | Source |
|---|---|---|
| B1 | Host fills its parent exactly (`position:absolute; inset:0`), is not hit-testable (`pointer-events:none`), and is hidden from accessibility (`role=presentation`). | css, render() |
| B2 | `Bounded` clips its ripples to its own bounds (`contain: strict`); `Unbounded` does not (`contain: layout size style`). Rounded-corner clipping is the **parent's** job (parent sets overflow hidden + border-radius), not the ripple's. | css |
| B3 | Geometry on `AddRipple(x, y)`: `w,h` = own size; `hypot = √(w²+h²)`; `maxDim = max(w,h)`; `initialSize = ⌊maxDim × 0.5⌋`; `maxRadius = Unbounded ? maxDim : hypot + 10`; `finalScale = maxRadius / initialSize`. | addRipple |
| B4 | Origin: Bounded → `(x, y)`; Unbounded → `(w/2, h/2)` regardless of arguments. Circle's top-left = origin − `initialSize/2`; circle's width = height = `initialSize`. | addRipple |
| B5 | The circle drifts toward the centre while growing: `translateEnd = (w/2 − originX, h/2 − originY)`. | addRipple |
| B6 | Expand animation: `scale(1) → translate(translateEnd) scale(finalScale)`, **225 ms**, `cubic-bezier(0.4, 0, 0.2, 1)`, fill forwards. Fade-in: opacity `0 → 0.16`, **75 ms**, linear, concurrently. | css |
| B7 | Colour = `currentColor` (the ripple inherits `color` from its parent). | css |
| B8 | The release action cannot take effect before **325 ms** after the ripple started (`225 + 100`, the Promise resolve delay). Invoking it earlier defers; invoking it later acts immediately. | addRipple |
| B9 | Release: opacity `0.16 → 0`, **150 ms**, linear, at the end transform; the circle is removed **200 ms** after release takes effect. | removeRipple, css |
| B10 | Each `AddRipple` adds an independent circle; the component never removes one on its own. | addRipple |

From `tap-click/index.js` (the `Activatable` behaviour):

| # | Behaviour | Source |
|---|---|---|
| A1 | Pointer down on an activatable surface (left/primary button only; `button === 2` ignored) starts a **100 ms** defer (`ADD_ACTIVATED_DEFERS`); *instant* surfaces skip the defer. | pointerDown, setActivatedElement |
| A2 | When the defer fires: surface gets `:activated`; if ripples are enabled, the surface's ripple gets `AddRipple(x, y)` with the **original pointer-down position**; any previous active ripple is released first. | addActivated |
| A3 | Pointer up **before** the defer fires: activate now (so a quick tap still ripples), then release smoothly. | setActivatedElement |
| A4 | Pointer up after activation ("smooth" release): ripple release action invoked (subject to B8); `:activated` cleared **150 ms** after activation (`CLEAR_STATE_DEFERS`) or immediately if already past that. Instant surfaces clear immediately. | removeActivated(true) |
| A5 | Pointer cancel, or a gesture claiming the pointer (`ionGestureCaptured` → in Avonic: pointer capture lost / a scroll gesture starting): defer cancelled, ripple released, `:activated` cleared immediately. A cancel before the defer fires means **no ripple at all**. | cancelActive, removeActivated(false) |
| A6 | A second pointer down while a surface is active is ignored. | pointerDown guard |
| A7 | Global switch: ripples are off when config `animated` or `rippleEffect` is false. | useRippleEffect |

## Avalonia mapping

| Ionic | Avonic |
|---|---|
| host `position:absolute; inset:0` | `AvonicRippleEffect : TemplatedControl`, `HorizontalAlignment/VerticalAlignment = Stretch`; parent templates place it in the same `Panel` as the content, beneath it |
| `pointer-events:none`, `role=presentation` | `IsHitTestVisible = false`, `AutomationProperties.AccessibilityView = Raw` |
| `contain: strict` / `contain: layout size style` | `ClipToBounds = true` / `false`, driven by `Type` |
| `currentColor` | ripple fill bound to the control's inherited `Foreground` — parents set `Foreground` on it from their own `--ripple-color` key (e.g. `Avonic.Item.RippleColor`) |
| `.ripple-effect` div per ripple | one `Ellipse` per ripple inside `PART_Container` (a `Canvas`), with `RenderTransform` = translate+scale and `Opacity` animated in code |
| CSS keyframes | `Avalonia.Animation.Animation` built in code with the constants above (the timing *is* the behaviour) |
| `addRipple` Promise timing | `AddRipple` returns `Action`; the control records `t0` and runs release at `max(now, t0 + 325 ms)` |
| `.ion-activatable` / `.ion-activatable-instant` / `ion-activated` | `Activatable` attached behaviour: `Activatable.IsEnabled`, `Activatable.IsInstant` attached properties on any `Control`; sets pseudoclass `:activated` on the surface; finds the ripple by walking the surface's visual descendants for an `AvonicRippleEffect` |
| global `animated` / `rippleEffect` config | static `AvonicConfig.RippleEffect` (bool, default true) in Components — the first entry of the Ionic-config equivalent |

Names per `CLAUDE.md`: enum for a string-union prop is `<Component><Prop>` → `RippleEffectType`
(proposing this as a rule; `CLAUDE.md` gains the line if approved).

## Deliverables

| | Path |
|---|---|
| Control | `src/Avonic.Components/Controls/RippleEffect/AvonicRippleEffect.cs`, `RippleEffectType.cs`, `AvonicRippleEffect.axaml` (stub template: the `Canvas`) |
| Behaviour | `src/Avonic.Components/Behaviors/Activatable.cs`, `src/Avonic.Components/AvonicConfig.cs` |
| Theme | nothing for this control (decision D2). `Avonic.Themes` gains its project skeleton (`AvonicTheme` styles class, empty token dictionary) so parents can add keys later |
| Tests | `tests/…/Controls/AvonicRippleEffectTests.cs` (B1–B10), `tests/…/Behaviors/ActivatableTests.cs` (A1–A7). Timing tests use the headless dispatcher with real delays (≤ 0.5 s each) |
| Compare | `COMPARISON.md` with Avonic frames captured from the headless renderer (Skia) at the same 0/60/160/320 ms press and 60/220/420 ms release offsets as `ref/` |
| Sample | `samples/Avonic.Sample/` created (desktop head, 390×844 window, one page per control) with a page: an activatable surface + ripple, bounded and unbounded, custom `Foreground` |
| Docs | `README.md` for the control with the ticked checklist and the behaviour tables above |

## Decisions needed

- **D1 — include `Activatable` in this PR?** Recommended yes (reasons above). Alternative: ripple
  only, with a throwaway sample driver, and `Activatable` in the `ion-item` PR.
- **D2 — where do Ionic's hard-coded visual constants live?** Ionic exposes no CSS variable for
  the ripple's shape, opacity (0.16) or timings. Recommended: constants in `Avonic.Components`
  — they are the component's behaviour, not a theme choice; the theme only supplies the
  *colour* via `Foreground`. This becomes a general rule: *a visual constant Ionic does not
  expose as a CSS variable is a constant in Components.* Alternative: still route them through
  `Avonic.RippleEffect.*` keys — but those would be invented API.
- **D3 — `AddRipple` returns `Action` (sync, timing preserved internally) rather than
  `Task<Action>`.** Recommended; it keeps the observable 325 ms contract with a simpler caller.
- **D4 — tests target the current Avalonia 12 headless platform with Skia rendering enabled**
  (`Avalonia.Skia` added to the test project) so the compare step can capture real frames.

## Risks

- Avalonia's `Border` may not clip children to its `CornerRadius`; if so, parents (item/button)
  will set `Clip` from their radius in *their* templates. Not this control's problem — noted so
  the invented `CornerRadius` prop from v1 does not creep back.
- Headless animation timing: if `Animation` doesn't advance under the headless render timer,
  tests fall back to asserting the scheduled state (start/end values, timestamps) and the
  compare step relies on the sample app. To be confirmed in Build.
