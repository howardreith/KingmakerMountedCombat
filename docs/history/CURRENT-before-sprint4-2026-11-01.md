# Sprint 3 — mounted trap disarm and spell target approach: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-11-01, finishing mission)

**SPRINT 3 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint3-traps-spell-approach` (PR #8) from `main` `6240d91e`. Product commits
`f22e6bd1` (carried approach), `3980e1cf` (turn-based admission of the delegated move), `6c19b814` (first-review
ownership corrections A and B) and `24c92355` (second-review queued-order correction). Finishing-mission commits
`f85e8327`, `54123d23`, `29012367` and `de5abf08` change only the diagnostic trap row and its external reader. Current
candidate sprint3q (`de5abf08`; DLL `fac005d6…`, MVID `6660af4a…`, package `6958aa3c…`, suite
`sprint3q-de5abf08-20261101`). Budget `sprint3` 118.27 of 135 min (+15 and +30 adjustments of the same record).

- **Ownership corrections A and B, queued-order correction: done** (accepted at source review; four slot/queue
  regressions on sprint3m).
- **Ownership neighbor natively: PASS** on sprint3l (r18). **SPELL-01: PASS natively** on earlier bytes (real time up
  to r16; turn-based r14, r17 on sprint3k).
- **TRAP-01: NOT DEMONSTRATED natively.** The declared trap-room preparation works natively on sprint3q (r24):
  - the two declared hostiles died through one recorded lethal rule each, the engine's `Wake` and the native life
    controller, with no combat;
  - the door opened through one ordinary click on a settled navmesh.

  But no active trap joins the mounted pair's navmesh area behind it, so no trap click was possible. r20, r21 and r23
  are retained setup failures with measured native causes; r22 was refused by the batch guard. The remaining input is in
  the report's *Finishing mission* section.

Records: [docs/SPRINT3-TRAPS-SPELL-APPROACH.md](docs/SPRINT3-TRAPS-SPELL-APPROACH.md), `AUTONOMOUS-RESUME.md`, PR #8.

Live installation at closeout: Mods tree empty, protected saves and the owner's originals unchanged (save protection
passed on every run; Working fixture restored), no transaction lock or game process, no candidate installed. Owner play
acceptance of Sprints 1, 2 and 3 remains separate. Sprint 2 state: [CURRENT before Sprint 3](docs/history/CURRENT-before-sprint3-2026-10-31.md).
