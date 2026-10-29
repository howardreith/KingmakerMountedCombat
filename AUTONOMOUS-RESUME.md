# Sprint 1 resume record (2026-10-29, iteration 2)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`:
iteration 1's `20261029-sprint1-p05-save-quick` (refused), `…-r2` (FAIL, profile recovery
PASS) and `20261029-sprint1-baseline96-p05-save-quick` (FAIL); iteration 2's
`20261029-sprint1-r01-ownerseed-manual` (refused in-game), `…-r2` (FAIL), `…-r3` (game
PASS, outer FAIL; reader-only re-evaluation PASS in
`analysis-cache\sprint1\r01-r3-reader-only-reevaluation.log`), `…-r4` (PASS),
`20261029-sprint1-r02-ownerseed-manual-cold` (PASS), `20261029-sprint1-r03-ownerseed-quick`
(FAIL), `…-r2` (PASS), `20261029-sprint1-r04-ownerseed-quick-cold` (PASS),
`20261029-sprint1-r05-ownerseed-auto` (PASS). Suites `sprint1*-…-20261029` and packages
`sprint1*-…` are retained; the current ones are `sprint1h-aa08ba1a-20261029` and
`…preview.98-sprint1h-aa08ba1a-diagnostic.zip`. Shared budget record:
`runtime-state\workflow-budgets\sprint1.json` (50.4 of 90 min). Owner-seed selections
used by the chain: `analysis-cache\sprint1\*.json`.

## Next action

1. ChatGPT/owner review of the branch; then the owner's own Quick/Auto check with the
   preview.98 package on their reported save (the sprint report's sequence).
2. If a native rotation proof is wanted: add `multi-exploration-quick-rotation` to the
   owner-seed checkpoint family (second `GetNextQuickslot()` under slot count 1, both
   leaves declared) and run it with `-EngineShapedSaveRoot`; do not rerun
   `persistence-p05-save/quick` on the Aug-14 fixture (engine `Player.PostLoad` failure on
   preview.96 and this candidate).
3. Launch with `-BaseGameOnly` while the owner's live Mods tree is empty. Start detached
   launches from a shell that carries the chain's environment (a WMI-created process does
   not inherit it).

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
