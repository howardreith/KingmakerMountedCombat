# Sprint 5 resume record (2026-11-02)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint5-small-riders-riding-dog`, branch
`codex/mounted-combat-sprint5-small-riders-riding-dog`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261102-sprint5-r01-small` to `r12-cold`,
and `f1-dog`, `f2-horse`, `f3-save`, `f4-cold`, `f5-base`, `f6-dog`. Reader re-evaluations and the f5 engine log are under
`analysis-cache\sprint5\`. f5's engine residue is in `runtime-backups\profile-20261102-sprint5-f5-base\engine-areas-residue\`.
Candidates sprint5a to sprint5o are under `artifacts\`; the current one is sprint5o, suite `sprint5o-992fe68e-20261102`. Budget record
`runtime-state\workflow-budgets\sprint5.json`.

## Next action

1. Review of the Sprint 5 PR. Owner play acceptance stays separate (checklist in the report).
2. Owner decision: extend the budget for the fat and thin build presets, about 15 validation minutes, or accept the refusal for now.
3. No merge, release or installation is part of this workflow.
4. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits. A reader or docs-only
   commit keeps the DLL bytes.
5. Tooling facts: rows run under `sprint5-small-rt` or `sprint5-dog-rt`, schema 49, reader `scripts/runtime/Sprint5SmallEvidence.ps1`,
   tests `scripts/Test-Sprint5SmallReaders.ps1`. The save route is `chunk7-persistence-save` and `-load` with
   `-PersistenceCase riding-dog-exploration-manual`. Launch with `-TimeoutSeconds 600`.
6. A view replacement inside the save route must be declared with `Chunk7NativeWorldScope.AdoptDeclaredViewReplacement`.
   `horse-companion-blueprint-registration` with no Call of the Wild leaves engine `Areas` residue and needs manual recovery.

Prior records: [AUTONOMOUS-RESUME before Sprint 5](docs/history/AUTONOMOUS-RESUME-before-sprint5-2026-11-02.md).
