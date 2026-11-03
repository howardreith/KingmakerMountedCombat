# Sprint 5 resume record (2026-11-03)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint5-small-riders-riding-dog`, branch
`codex/mounted-combat-sprint5-small-riders-riding-dog`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261102-sprint5-r01-small` to `r12-cold`,
`f1-dog`, `f2-horse`, `f3-save`, `f4-cold`, `f5-base`, `f6-dog`, and `g1-horse`, `g2-horse`, `g3-dog`, `g4-horse`, plus `g5-dog`, which
was refused before launch. Reader re-evaluations and the f5 engine log are under `analysis-cache\sprint5\`. f5's engine residue is in
`runtime-backups\profile-20261102-sprint5-f5-base\engine-areas-residue\`. Candidates sprint5a to sprint5s are under `artifacts\`; the
current one is sprint5s, suite `sprint5s-877f4b7f-20261102`. Budget record `runtime-state\workflow-budgets\sprint5.json`.

## Next action

1. Review of the Sprint 5 PR. Owner play acceptance stays separate (checklist in the report).
2. Open, and blocking the completion label: the female near boot defect, and the final Dog seat, which has never been rendered. The
   next native step is a female Horse diagnostic comparing the right foot bone with its baked mesh vertices, plus one Dog launch. That
   is about 15 validation minutes and needs a new owner allowance. The mission allowed one adjustment, already used. Do not extend it,
   reset it, or open a new budget ID without the owner.
3. No merge, release or installation is part of this workflow.
4. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits. A reader or docs-only
   commit keeps the DLL bytes. Changing `docs/SPRINT5-SMALL-SEATS.json` changes the DLL, because its hash is compiled in.
5. Tooling facts: rows run under `sprint5-small-rt` or `sprint5-dog-rt`, schema 49, reader `scripts/runtime/Sprint5SmallEvidence.ps1`,
   tests `scripts/Test-Sprint5SmallReaders.ps1`. The save route is `chunk7-persistence-save` and `-load` with
   `-PersistenceCase riding-dog-exploration-manual`. Launch with `-TimeoutSeconds 600`.
6. A launch reserves 10 budget minutes after its WhatIf and package steps, so admit about 11 before starting one.
7. A view replacement inside the save route must be declared with `Chunk7NativeWorldScope.AdoptDeclaredViewReplacement`.
   `horse-companion-blueprint-registration` with no Call of the Wild leaves engine `Areas` residue and needs manual recovery.

Prior records: [AUTONOMOUS-RESUME before Sprint 5](docs/history/AUTONOMOUS-RESUME-before-sprint5-2026-11-02.md).
