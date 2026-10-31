# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-31, seventh iteration)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0`. Product commits `5b02d4dd`,
`7d124fad` (DLL `809573ad…`, r09/r10 PASS retained) and `058792e5`/`0c0e39c0` (door supervision on the engine's gameplay
timestep instead of frozen turn-based GameTime; a turn-based approach end re-attempts on a later turn only).
Diagnostics `538a1192`..`de4ff23b` (speed legs v6), readers `d1667bcd`, single-pair turn-based door stage `43c10412`,
`3ad92495`. Current candidate sprint2v (DLL `39a1006c69925d1e3f47b71c76d6e65fb27de8522c4737f2afc9271215437483`, MVID
`df69c928-ccae-46a3-a6a6-c0f9df5711d5`, package `1115d2bc…`, suite `sprint2v-3ad92495-20261031`). Budget `sprint2`
143.58 of 160 min (explicit +30, +20, +20 adjustments on the one record).

- **MOVE-01 on the owner's surface: accepted (r21, sprint2t).** All four legs on the owner's roguelike-hub save with the
  pre-owned pair: party-paced Horse 2.63 m/s (Slow gait, limit 3.048), Horse carrying the rider 4.52, Horse alone
  4.43, rider on foot 2.91; recorded host verdict FAIL on two unknown v6 goal-search keys, re-evaluated PASS by the
  reader-only commit `d1667bcd` (receipt retained). Not rerun.
- **Production clock correction: accepted at source review; real-time door exercise on the corrected clock PASS
  natively (r22, sprint2v).** `chunk4-traversal-core` on the Working fixture: routed mounted door click from distance,
  one interaction, navmesh settlement, doorway traversal, closed-door stop/return control; 257 assertions, 0 failures,
  exact restoration. The chunk 4 evidence does not record the door command's operational-second fields.
- **DOOR-01 / SPACE-01 real time: PASS natively** (r09, r10 on `809573ad…`; r22 on `39a1006c…`).
- **DOOR-01 turn-based control: NOT DEMONSTRATED.** The single-pair stage exists (owner-surface opt-in; shared
  combat-usable selection before any preparation). No retained copy supplies a loadable save with a combat-usable
  door: Working fixture dwarf door inferred `NotInCombat` (Chunk 6A refusal), Old Sycamore's only such door 94 m away
  and unrevealed (r18), the owner's dungeon-floor autosave refused by the persistence authority's `Manual` pin (r23,
  pin deliberately kept). Justified external input: an owner-made Manual save with a revealed, interactable
  `NotInCombat`-false door within 28 m of the mounted pair (details in the report's seventh iteration).

Records: [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md) ("Seventh iteration"
and earlier), `AUTONOMOUS-RESUME.md`, PR #7.

Live installation at closeout: Mods tree empty, loader parameters and profile root (including `Areas\`) at intake,
protected saves and the owner's originals unchanged, no lock or game process, no candidate installed. Owner play
acceptance of Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
