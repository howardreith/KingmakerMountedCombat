# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-31, fifth iteration)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0`. Product commits `5b02d4dd` and
`7d124fad` (DLL `809573ad…`, package sprint2k, r09/r10 full-run PASS retained). Diagnostics commits `dd02ebfc`
(routes), `fdf69f65`/`c74ab7b7` (review corrections A/B and the review blocker), `4efe4158`/`e76f02f4` (speed legs
v4). Candidates sprint2n (`3f8cd776…`, suite `sprint2n-c74ab7b7-20261031`) and sprint2o (`38a48eb3…`, suite
`sprint2o-e76f02f4-20261031`). Budget `sprint2` 105.87 of 120 min (one explicit +30 adjustment recorded on the record).

- **DOOR-01 / SPACE-01 real time: PASS natively on `809573ad…`** (r09 Old Sycamore `TavernDoor`; r10
  `dwarf_dungeon_door_01`). Nine-second mechanism: offline lifecycle regression plus native unstarted-while-moving
  observations; the literal >9 s native route is NOT RUN (accepted equivalent).
- **DOOR-01 turn-based control: route run natively (r14), NOT DEMONSTRATED.** The corrected contract (movement cost
  through the paired allocation, settlement window, scope labels) is committed and offline-verified; natively the only
  qualifying door on the retained owner seed, `TavernDoor`, is flagged `NotInCombat`, the stage refused before any
  encounter and restored the door exactly. Blocker: no combat-interactable qualified door on this seed.
- **MOVE-01 on the owner's surface: route run natively (r13, r16), NOT DEMONSTRATED.** The corrected reader proves the
  carried rider from ownership (confirmed natively: the Horse is the pair's only mover, the rider has no Move of its own,
  0.47 m from the Horse). The party-paced leg's three movers were interrupted together at the same game time 1.2–1.5 m
  short of the one admitted goal after a terrain climb, twice identically, in the Default mode (no dialog, cutscene, load
  or combat); the solo legs were never reached. The 6.7 m that ran show the Horse at 2.93 m/s against the 3.048 m/s party
  pace (no KMC clamp visible). r09's four legs on the door seed stand (party-paced 2.69 m/s, pair alone 4.55, Horse
  4.18, rider 3.02).
- **Why native work stopped:** two equivalent owner-surface failures with no observation that discriminates a fix (a
  third attempt is not admitted), the door blocker above, and the active ceiling. Exact remaining actions and all
  artifacts: [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md), "Fifth iteration".

Live installation at closeout: Mods tree empty, loader parameters and profile root (including `Areas\`) at intake,
protected saves and the owner's originals unchanged, no lock or game process, no candidate installed. Owner play
acceptance of Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
