# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-31, fourth iteration)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0`. Product commits `5b02d4dd` and
`7d124fad` (DLL `809573ad…`, package sprint2k, r09/r10 full-run PASS retained); diagnostics commit `dd02ebfc` (DLL
`70bbf167…`, MVID `8d8eeecb…`, package sprint2l `fc9252a0…`, suite `sprint2l-dd02ebfc-20261031` `da7bdb19…`). Budget
`sprint2` 83.27 of 90 min.

- **DOOR-01 / SPACE-01 real time: PASS natively on `809573ad…`** (r09 Old Sycamore `TavernDoor` with one owned 0.5 m
  recovery, Stop, click-while-moving; r10 `dwarf_dungeon_door_01`). Nine-second mechanism: offline lifecycle regression
  plus native unstarted-while-moving observations; the literal >9 s native route is NOT RUN (accepted equivalent).
- **DOOR-01 turn-based control: route implemented, WhatIf PASS, NOT RUN natively.** `turnBasedDoor` opt-in on the r09
  selection (`analysis-cache\sprint2\owner-seed-exploration-door-open-tb.json`), stage `Chunk7TurnBasedDoorProbe`, row
  `C7-world-turn-based-door`, reader committed and tested.
- **MOVE-01 on the owner's surface: route implemented, WhatIf PASS, NOT RUN natively.** The owner's
  `Manual_301_After_mounting` is registered as `KMC_S2_ROGUELIKE_SURFACE_CANDIDATE`; case `owner-surface-speed`
  (`analysis-cache\sprint2\owner-surface-speed.json`) binds the pre-owned pair only and runs the four aligned legs with
  Call of the Wild staged from retained bytes. r09's legs on the door seed stand (party-paced 2.69 m/s walk gait, pair
  alone 4.55, Horse 4.18, rider 3.02; no KMC clamp).
- **Why not run:** the budget guard refuses a launch whose outer timeout (600 s) exceeds the remaining allowance
  (7.03 min at the attempt); no extension, reset or new budget id is selected. Both runs need a new explicit allowance
  from the owner, then `r11`/`r12` through the recorded wrapper routes. Details and exact artifacts:
  [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md), "Fourth iteration".

Live installation at closeout: Mods tree empty, loader parameters and profile root (including `Areas\`) at intake,
protected saves and the owner's originals unchanged, no lock or game process, no candidate installed. Owner play
acceptance of Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
