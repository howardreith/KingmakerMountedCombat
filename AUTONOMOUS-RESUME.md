# Sprint 1 resume record (2026-10-29)

Machine: DESKTOP-SRJJ623. Worktree
`C:\Dev\KingmakerMountedCombatLab\worktrees\sprint1-save-reliability`, branch
`codex/mounted-combat-sprint1-save-reliability` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` with the frozen
preview.96 `bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Runs
`20261029-sprint1-p05-save-quick` (refused pre-staging), `…-r2` (FAIL, profile recovery
PASS) and `20261029-sprint1-baseline96-p05-save-quick` (FAIL, restored) are retained under
`runtime-evidence`. Suites `sprint1-d590822f-20261029` (superseded) and
`sprint1b-d590822f-20261029` (`2320b9d2…`) and packages `sprint1-d590822f` / `sprint1b-d590822f`
are retained. Shared budget record: `runtime-state\workflow-budgets\sprint1.json`.

## Next action

1. ChatGPT/owner review of the three commits; then the owner's own Quick/Auto check with
   the preview.97 package on their reported save (see the sprint report's sequence).
2. If a native rotation proof is still wanted: add a rotation case to the owner-seed
   checkpoint family or refresh the Chunk 5 Working fixture on the current product; do not
   rerun `persistence-p05-save/quick` on the Aug-14 fixture (engine `Player.PostLoad`
   failure on preview.96 and preview.97, see
   [the report](docs/SPRINT1-SAVE-RELIABILITY-2026-10-29.md)).
3. Launch with `-BaseGameOnly` while the owner's live Mods tree is empty; otherwise UMM's
   prune of the absent `CheatMenu` entry leaves the run `restoration-blocked`.

Prior Chunk 7 resume: [AUTONOMOUS-RESUME before Sprint 1](docs/history/AUTONOMOUS-RESUME-before-sprint1-2026-10-29.md).
