# Sprint 2 resume record (2026-10-30, completion + review-fix iteration)

Machine: DESKTOP-SRJJ623. Worktree `C:\Dev\KingmakerMountedCombatLab\worktrees\sprint2-movement-doors`,
branch `codex/mounted-combat-sprint2-movement-doors` (the request validator requires the
`codex/mounted-combat-` prefix). The root repository stays on `main` `05a9efd0` with its frozen
`bin\Release` DLL untouched.

## Custody

No native game, executor, transaction or lock remains. Retained under `runtime-evidence`: the first iteration's
r01/r02/r04 (see the report), `20261030-sprint2-r05-doors-speed-v4` (game PASS 33/0; outer FAIL retained in
runtime-result.json; the corrected reader `3ef8119f` passes the retained artifact offline, log in
`analysis-cache\sprint2`), `20261030-sprint2-r07-traversal-core-dungeon-door` (game PASS 4/4 rows; outer FAIL on the
engine `Areas\` unpack cache; the seven files moved with hashes into
`runtime-backups\20261030-sprint2-r07-traversal-core-dungeon-door\engine-areas-residue\`). r06 was refused before
launch (package bound to the previous HEAD). Immutable inputs: owner seed
`fixtures\owner-supplied\KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN.master.zks` (`90789101…`), the qualified
`KMC_AUTOMATION_WORKING` fixture with retained dependencies `analysis-cache\sprint1\retained-dependencies-final211.json`.
Intake candidates (not admitted, not loaded): `fixtures\owner-supplied\KMC_S2_ROGUELIKE_SURFACE_CANDIDATE.master.zks`
(`1fda72b1…`, Manual_301_After_mounting) and `KMC_S2_DUNGEON_FLOOR_CANDIDATE.master.zks` (`4c1db37e…`, Auto_1121).
Packages: sprint2f `b915dba2…` and sprint2g `6d29b6f8…` (DLL `85542fbf…`, r05/r07); sprint2h `b5168c84…` (suite
freeze refused by r07's stale lock, unused) and sprint2i `91165cf5…` (suite `sprint2i-b3f78482-20261030` `f2235aaa…`,
DLL `ee2f72a9…` / MVID `ee7142af…`, r08). r08 `20261030-sprint2-r08-doors-speed-v4-fixed`: door probe v4 passed
in-game on the review-fixed binary, run FAIL in the unchanged cycle step (route goal search), evidence retained. r07's
stale lock was recovered (`profile-recovery.json` PASS). Budget record `runtime-state\workflow-budgets\sprint2.json`:
58.35 of 90 min, batches `sprint2:completion-1` and `sprint2:completion-2`.

## Next action

1. Final review of draft PR #7 (HEAD `b3f78482`; disposition PARTIAL: turn-based door control and the owner-exact
   fixtures NOT RUN, r08 ended in an unrelated cycle step); then the owner's play checklist (CURRENT.md, report). Owner play acceptance stays
   separate. No merge, release or installation is part of this workflow.
2. To turn the two NOT RUN rows into native evidence: generalize the owner-seed selection to a pre-owned single pair
   (rider `121dc2f0…`, Horse `fb764728…`) for `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE` / `KMC_S2_DUNGEON_FLOOR_CANDIDATE`
   with Call of the Wild 1.14.4c-2.1 staged from the retained selection; a turn-based door control needs the
   owner-seed combat checkpoint chain (three launches).
3. To re-run the two-pair route after r08: make the door probe return pair A to its pre-setup start before the
   engine cycle (the 20 m setup plus the endpoint neighbor left no clear 2 m route for the unmounted control).
4. Lab follow-up: the profile-recovery rule in `scripts/runtime/PersistenceProfileProtection.ps1` admits no new
   leaf under the profile root; Working-fixture scenarios leave the engine's `Areas\` unpack cache there
   (seven files). Decide whether to classify that cache like the achievement cache; it was not changed here.
5. Tooling facts: the launcher binds the package manifest to the exact HEAD (any commit needs a new package + suite);
   `New-KmcQualificationSuiteSnapshot.ps1` must run in-process with `-Confirm:$false` (a `-File` hop fails in
   ShouldProcess) and refuses while a git process is active; the reader's occupancy bound is radius + the recorded
   recovery distance for recovered rows.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
