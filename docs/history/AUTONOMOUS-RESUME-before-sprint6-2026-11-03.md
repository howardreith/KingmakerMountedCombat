# Sprint 5 resume record (2026-11-03)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint5-small-riders-riding-dog`, branch
`codex/mounted-combat-sprint5-small-riders-riding-dog`. The root repository stays on `main`.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: `20261102-sprint5-r01-small` to `r12-cold`,
`f1-dog`, `f2-horse`, `f3-save`, `f4-cold`, `f5-base`, `f6-dog`, `g1-horse`, `g2-horse`, `g3-dog`, `g4-horse`, `g5-dog` (refused before
launch), and `20261103-sprint5-h1-boot`, `h2-dog`, `h3-horse`. Reader re-evaluations and the f5 engine log are under
`analysis-cache\sprint5\`. f5's engine residue is in `runtime-backups\profile-20261102-sprint5-f5-base\engine-areas-residue\`. Candidates
sprint5a to sprint5v are under `artifacts\`; the current one is sprint5v, suite `sprint5v-fcdce675-20261102`. Budget record
`runtime-state\workflow-budgets\sprint5.json`.

## Next action

1. Final review of PR #10, including the [visual packet](https://claude.ai/artifact/T1vJzEguDxW1PJsLzzRsBg). Owner play acceptance stays
   separate (checklist in the report).
2. No merge, release or installation is part of this workflow. The mission allowed one adjustment, now used. Do not extend it, reset it,
   or open a new budget ID without the owner.
3. A further launch needs a new package, because the manifest binds the exact HEAD including docs commits. A reader or docs-only
   commit keeps the DLL bytes. Changing `docs/SPRINT5-SMALL-SEATS.json` changes the DLL, because its hash is compiled in.
4. Tooling facts: rows run under `sprint5-small-rt` or `sprint5-dog-rt`, schema 49, reader `scripts/runtime/Sprint5SmallEvidence.ps1`,
   tests `scripts/Test-Sprint5SmallReaders.ps1`. The save route is `chunk7-persistence-save` and `-load` with
   `-PersistenceCase riding-dog-exploration-manual`. Launch with `-TimeoutSeconds 600`.
5. A launch reserves 10 budget minutes, and its WhatIf and staging prelude spend under a minute first, so admit about 11 before starting
   one. Wait on the controller process, not a marker: a refusal never writes one.
6. A view replacement inside the save route must be declared with `Chunk7NativeWorldScope.AdoptDeclaredViewReplacement`.
   `horse-companion-blueprint-registration` with no Call of the Wild leaves engine `Areas` residue and needs manual recovery.

Prior records: [AUTONOMOUS-RESUME before Sprint 5](docs/history/AUTONOMOUS-RESUME-before-sprint5-2026-11-02.md).
