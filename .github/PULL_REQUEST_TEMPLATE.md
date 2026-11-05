Closes #

## Control

`ion-…` → `Avonic…`

## Review artefacts

- Plan: `docs/components/<ion-tag>/PLAN.md`
- Comparison (Ionic vs Avonic frames + behaviour table): `docs/components/<ion-tag>/COMPARISON.md`
- Docs with ticked checklist: `docs/components/<ion-tag>/README.md`

## Fidelity

- Checklist coverage: _n_ / _m_ rows ticked; omissions and why:
- Deliberate differences from Ionic (also recorded in COMPARISON.md):
- Avonic extensions (documented in the README's Extensions section):

## Review

- `/code-review` findings: _n_ raised, _n_ fixed, _n_ waived (reasons below)
-

## Checks

- [ ] `dotnet build Avonic.sln` — 0 warnings, 0 errors
- [ ] `dotnet test Avonic.sln` — all passing; one test per planned behaviour
- [ ] No declared property without a working implementation and a test
- [ ] No emoji / Unicode glyphs as icons; no hex colours in Themes outside `Tokens/`
- [ ] Only this control's files touched (shared files are auto-discovered)
- [ ] `CLAUDE.md` component row and `docs/ROADMAP.md` updated
