# Sprint 1 resume record (2026-10-30, iteration 4)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`
(iterations 1–3 as the previous record listed), plus iteration 4's
`20261030-sprint1-r11-area-auto-overwrite` (PASS; the admitted pre-existing `Auto_1`
overwritten by the genuine `AfterEntry` autosave, evidence `observations.areaOverwrite`),
`20261030-sprint1-r12-area-auto-overwrite-cold` (PASS; separate-process cold load of the
overwritten `Auto_1` `15ac93fd…`) and `20261030-sprint1-r10-upg01-p05-load-quick-logtail`
(FAIL as r09 with the engine log tail retained in `runtime-game-result.json`). The r09
Unity player log copy lives in the r09 evidence directory (`recovered-output_log.*`).
Current package `…preview.98-sprint1l-diagnostic.zip` (`3eec930a…`; `sprint1k` is the same
DLL, unused), suite `sprint1l-f4294828-20261030`. Shared budget record
`runtime-state\workflow-budgets\sprint1.json`: 100.98 of 135 min, 77 entries (the two
historical `sprint1:batch1` failed entries are harness-child refusals from before
`f4294828`). Selections: `analysis-cache\sprint1\*.json`. The isolated roots
`runtime-staging\persistence-20261030-sprint1-r1{1,2}-*` hold the written/consumed slots.

## Next action

1. Final review of the branch (PR #6), then the owner's own Quick/Auto check with the
   preview.98 package on their reported save; owner play acceptance stays separate.
2. UPG-01 (classified: fixture/dependency mismatch): stage the archive's dependency — the
   owner's Call of the Wild installation beside KMC in the staged Mods tree (a retained
   snapshot or a reinstalled copy; base-game-only mode cannot carry it, and a new suite must
   pin that Mods digest) — then `persistence-p05-load/quick` on
   `runtime-staging\persistence-final211-p05-save-quick\Saved Games\Quick_1.zks`
   (`83e92e1e…`) → resave → separate-process cold load. Never edit the archive.
3. Test debt outside scope: `scripts/chunk7/Test-Chunk7SixPairExitReader.ps1` is 3/22 at
   `main` (fixture requests carry an older `productVersion` than `version.json`).
4. Launch with `-BaseGameOnly` while the owner's live Mods tree is empty. A WMI-started
   chain wrapper must carry its own env (qualifier, budget id, batch) and writes a UTF-16
   log (`tr -d '\0'` before `grep`). Use absolute paths with .NET file APIs in the tool shell.

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
