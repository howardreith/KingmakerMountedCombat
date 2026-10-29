# Sprint 1 resume record (2026-10-29, iteration 3)

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
`20261029-sprint1-r05-ownerseed-auto` (PASS); iteration 3's
`20261029-sprint1-r06-ownerseed-rotation-quick` (PASS, Quick overwrite twice through the
handler body), `20261029-sprint1-r07-ownerseed-rotation-quick-cold` (PASS; its first attempt
was refused in preflight and left no evidence directory),
`20261029-sprint1-r08-ownerseed-overwrite-manual` (game PASS, outer FAIL; reader-only
re-evaluation PASS in `analysis-cache\sprint1\r08-reader-only-reevaluation.log`),
`20261029-sprint1-r09-upg01-p05-load-quick` (FAIL in the engine's load). Suites
`sprint1*-…-20261029` and packages `sprint1*-…` are retained; the current ones are
`sprint1j-984ae01a-20261029` and `…preview.98-sprint1j-diagnostic.zip` (DLL `a680dd69…`,
identical to `sprint1i`). Shared budget record:
`runtime-state\workflow-budgets\sprint1.json` (78.47 of 90 min). Owner-seed selections used by
the chains: `analysis-cache\sprint1\*.json`.

## Next action

1. ChatGPT/owner review of the branch; then the owner's own Quick/Auto check with the
   preview.98 package on their reported save (the sprint report's sequence).
2. If the genuine Auto overwrite is wanted: make the area scenario's one-transition
   observation re-entrant so a second `AfterEntry` transition back into the same destination
   replaces `Auto_1` (same-area replacement, the authority's existing contract), as a third
   owner-seed checkpoint case; then its cold load. About 11.5 min of the `sprint1` allowance
   remain; a new candidate costs a Candidate tier (about 4 min without the big reader test).
3. UPG-01 stays blocked in the engine's load of the preview.211 archive; to classify it, a
   route that retains the game log (not base-game-only, or an explicit log capture) is needed.
4. Launch with `-BaseGameOnly` while the owner's live Mods tree is empty. Start launches from
   a shell that carries the chain's environment (a WMI-created process does not inherit it);
   use absolute paths with .NET file APIs in the tool shell (a relative path resolves against
   the root repository).

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
