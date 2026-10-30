# Sprint 2 resume record (2026-10-30, third iteration)

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
stale lock was recovered (`profile-recovery.json` PASS). Third iteration: packages sprint2j `a1bce12e…` (`5b02d4dd`,
Candidate tier 8/0) and sprint2k `4a3497f9…` (`7d124fad`, suite `sprint2k-7d124fad-20261030` `0e3c7701…`, DLL
`809573ad…` / MVID `2b00f9fa…`); r09 `20261030-sprint2-r09-doors-speed-v4-final` and r10
`20261030-sprint2-r10-traversal-core-dungeon-door-final` both full-run PASS on it, restored (r10 under the Areas
isolation). Budget record `runtime-state\workflow-budgets\sprint2.json`: 73.22 of 90 min, batches
`sprint2:completion-1` to `sprint2:completion-3`.

## Next action

1. Final review of draft PR #7 (product `7d124fad`, DLL `809573ad…`, package sprint2k, suite `sprint2k-7d124fad-20261030`;
   r09 and r10 full-run PASS on it; disposition PARTIAL: turn-based door control and the owner-exact fixtures NOT RUN); then the owner's play checklist (CURRENT.md, report). Owner play acceptance stays
   separate. No merge, release or installation is part of this workflow.
2. To turn the two NOT RUN rows into native evidence: generalize the owner-seed selection to a pre-owned single pair
   (rider `121dc2f0…`, Horse `fb764728…`) for `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE` / `KMC_S2_DUNGEON_FLOOR_CANDIDATE`
   with Call of the Wild 1.14.4c-2.1 staged from the retained selection; a turn-based door control needs the
   owner-seed combat checkpoint chain (three launches).
3. Done in `5b02d4dd`: the probe restores both pairs to their pre-setup starts (r09 passed the full cycle); `chunk4-traversal-core`
   runs under the existing Areas isolation (r10 quarantined the engine tree and restored the intake tree exactly).
4. Turn-based door control: add a door stage to the owner-seed combat checkpoint route (`Chunk7CombatCheckpointDriver`:
   rider's acting turn, cost and movement allocation, rejected off-turn input) and its reader rows; no existing route
   clicks a door in turn-based combat.
5. Tooling facts: the launcher binds the package manifest to the exact HEAD (any commit needs a new package + suite);
   `New-KmcQualificationSuiteSnapshot.ps1` must run in-process with `-Confirm:$false` (a `-File` hop fails in
   ShouldProcess) and refuses while a git process is active; the reader's occupancy bound is radius + the recorded
   recovery distance for recovered rows.

Prior records: [AUTONOMOUS-RESUME before Sprint 2](docs/history/AUTONOMOUS-RESUME-before-sprint2-2026-10-30.md).
