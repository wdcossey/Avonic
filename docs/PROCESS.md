# The component loop

Avonic v2 is built **one control at a time**. The loop below is the only way a control enters
the codebase. No step is skipped, no two controls are in flight at once, and nothing is
"done" until the last step is signed off.

```
 ┌────────┐   ┌───────┐   ┌─────────┐   ┌────────┐   ┌──────────┐
 │  PLAN  │ → │ BUILD │ → │ COMPARE │ → │ REVIEW │ → │ DOCUMENT │ → next control
 └────────┘   └───────┘   └─────────┘   └────────┘   └──────────┘
```

## 1. Plan

Produce `docs/components/<ion-tag>/PLAN.md` and get it approved **before any code**.

1. Run `node tools/ionic-api.js <ion-tag>` and paste the checklist in. Every row is either
   ticked, or has a one-line reason it is omitted (`out of scope: router`, `not applicable:
   HTML form`). Unexplained gaps are a planning failure.
2. Read the implementation: `node_modules/@ionic/core/dist/collection/components/<name>/<name>.js`.
   List the **behaviours** (not props) that the control must reproduce — e.g. *"ion-item forwards
   a tap on its padding to the first interactive child"*, *"ion-toggle flips on drag past 10px
   and fires ionChange on each flip"*. These become the test list.
3. Read `<name>.md.css`. List the visual states and which resource keys drive them.
4. Note Avonic extensions (touch-target enforcement, etc.) in a separate section. They never
   mix with the Ionic-mapped API.
5. Note dependencies on other controls and whether they exist yet.

## 2. Build

One PR, one control, all of:

| Deliverable | Where |
|---|---|
| Control class — behaviour, properties, events, pseudoclasses, **no visual opinion** | `src/Avonic.Components/Controls/<Name>/` |
| Functional stub template (structure only) | same folder, `<Name>.axaml` |
| Styled template using only resource keys | `src/Avonic.Themes/Controls/<Name>.axaml` |
| Resource keys — one per Ionic CSS var, named by the rule in `CLAUDE.md` | `src/Avonic.Themes/Tokens/<Name>.axaml` |
| `Design.PreviewWith` block | in the Themes template |
| Behaviour tests — one per behaviour listed in the plan; each must **fail** if the behaviour is removed | `tests/Avonic.Components.Tests/Controls/<Name>Tests.cs` |
| Sample page | `samples/Avonic.Sample/Pages/<Name>Page.axaml` |

Rules that are not negotiable:
- `dotnet build` and `dotnet test` are green **before every commit**. No "it's just XAML".
- A declared property works. If it cannot be made to work yet, it is not declared.
- Icons come from ionicons via `AvonicIcon`. No emoji, no Unicode glyphs.
- No hex colours or `Brushes.*` in Themes outside the token file.

## 3. Compare

Side by side with the real web component in **md** mode at a phone viewport:

1. Every behaviour in the plan is exercised on both and the result recorded in
   `docs/components/<ion-tag>/COMPARISON.md` — match / deliberate difference / gap.
2. Visual: the Avonic rendering is placed next to the Ionic rendering. Differences are
   listed and either fixed or recorded as deliberate.
3. Any gap found here goes back to **Build**. The control does not proceed with open gaps.

## 4. Review

- `/code-review` on the PR, high effort. Findings fixed or explicitly waived in the PR.
- A human reads `PLAN.md`, `COMPARISON.md` and the diff, and signs off.

## 5. Document

`docs/components/<ion-tag>/README.md`: usage, the final ticked checklist, behaviours,
extensions, resource keys. The `CLAUDE.md` component table gains one row, and only now.

---

**Then** the next control starts. Not before.
