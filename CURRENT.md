# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-31, sixth iteration)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0`. Product commits `5b02d4dd`,
`7d124fad` (DLL `809573ad…`, package sprint2k, r09/r10 full-run PASS retained) and, this iteration, `058792e5`/`0c0e39c0`
(door supervision on the engine's gameplay timestep instead of frozen turn-based GameTime; a turn-based approach end
re-attempts on a later turn only). Diagnostics `538a1192`, `ba0eb6e5`, `de4ff23b` (speed legs v6), readers `d1667bcd`.
Final candidate sprint2t (DLL `257bc81146ee1ea09f0543cb63d5b72b618c1d50f632512f82aecf75aa72c372`, MVID
`2a3e3fe4-7aff-409a-908c-67aa3e5497ec`, package `1977a262…`, suite `sprint2t-de4ff23b-20261031`). Budget `sprint2`
130.73 of 140 min (explicit +30 and +20 adjustments recorded on the one record).

- **MOVE-01 on the owner's surface: DEMONSTRATED natively (r21, sprint2t).** On the owner's own roguelike-hub save with
  the pre-owned pair, all four legs completed and were accepted: party-paced Horse 2.63 m/s (Slow gait, limit 3.048),
  Horse carrying the rider 4.52 m/s, Horse alone 4.43 m/s, rider on foot 2.91 m/s; the rider stayed 0.47 m beside the
  Horse with no Move of its own. The recorded host verdict is FAIL on a reader exact-property mismatch of two new v6
  keys; the corrected reader (`d1667bcd`, reader-only) re-evaluated the retained artifact PASS (log in
  `analysis-cache\sprint2`). The r13/r16/r17 party-leg interruption was identified (r17 run-scoped observation) as the
  native path-completion chain ending 1.2-1.5 m short of a goal recorded 1.7 m below the raised surface
  (`GetNearestNode.clampedPosition` keeps the requested height); goals now use the navmesh node height.
- **Production clock correction (review 2026-10-31): done, offline-verified, independently reviewed (no blockers; one
  MAJOR fixed).** `MountedDoorOperationalClock` (accumulated `TimeController.GameDeltaTime` per ticked frame) drives the
  approach retry cooldown, the stall clock, the settlement wait and the recovery stall; campaign GameTime is recorded as
  observed only. Five controlled-time regressions. **No native door run exercised the corrected code** (see next item).
- **DOOR-01 / SPACE-01 real time: PASS natively on `809573ad…`** (r09, r10; unchanged, on the earlier product bytes).
- **DOOR-01 turn-based control: NOT DEMONSTRATED.** The shared door selection now requires `NotInCombat` false before
  any preparation; r18 refused at that gate with the inventory: Old Sycamore's only such door (`MiteYardDoor`) is 94 m
  away, closed, unrevealed and not interactable. Needs an owner-made save near a revealed combat-usable door.

Exact records, runs r17 to r21, the review findings and the remaining action:
[docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md), "Sixth iteration".

Live installation at closeout: Mods tree empty, loader parameters and profile root (including `Areas\`) at intake,
protected saves and the owner's originals unchanged, no lock or game process, no candidate installed. Owner play
acceptance of Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
