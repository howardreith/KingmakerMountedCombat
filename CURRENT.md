# Sprint 3 — mounted trap disarm and spell target approach: ENGINEERING PARTIAL, READY FOR REVIEW (2026-11-01, review iteration)

**SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation
is selected.

Review branch `codex/mounted-combat-sprint3-traps-spell-approach` (PR #8) from `main` `6240d91e`. Product commits
`f22e6bd1` (carried approach), `3980e1cf` (turn-based admission of the delegated move) and `6c19b814` (the two
ownership corrections from the PR #8 review). Current candidate sprint3l (`6c19b814`; DLL `8abeffb9…`, MVID
`a09a28ff…`, package `a1987a78…`, suite `sprint3l-6c19b814-20261101`). Budget `sprint3` 80.45 of 90 min.

- **Review correction A (admission before release): done.** The Run prefix only admits; a postfix releases the carried
  approach from what native Run displaced. Refused, simulated, preparation, foreign and duplicate requests release
  nothing; only a player mount order ends the rider's pending command.
- **Review correction B (own movement only): done.** Cleanup ends and sweeps only the carrier's exact move and stops the
  agent only when no successor owns the mount; a busy mount makes the approach yield instead of throwing.
- **Regressions:** nine offline behaviour cases on the production seams; 962 component tests; 63 reader checks.
- **Ownership neighbor natively on sprint3l: PASS** (r18, the held-charge row: Stop, second ordinary click, carried
  delivery, own move restored twice, no successor).
- **SPELL-01: PASS natively** on earlier bytes (real time r08–r16 up to sprint3k; turn-based r14, r17 on sprint3k).
  Those runs predate the corrections; the turn-based neighbor on sprint3l is offline-only (the allowance left, 9.55 min,
  is below a launch's 10-minute admission).
- **TRAP-01: NOT DEMONSTRATED natively; external input required.** The trap-specific safe point and the native Trickery
  check have no native evidence and no offline execution. Neither inspected retained alternative has a reachable trap
  (surface seed: none; dungeon-floor autosave: three traps 88–93 m away behind closed doors and hostile groups). The
  compatible input requirements are in the report's *Review corrections* section.

Records: [docs/SPRINT3-TRAPS-SPELL-APPROACH.md](docs/SPRINT3-TRAPS-SPELL-APPROACH.md), `AUTONOMOUS-RESUME.md`, PR #8.

Live installation at closeout: Mods tree empty, loader parameters and profile root at intake, protected saves and the
owner's originals unchanged, no transaction lock or game process, no candidate installed. Owner play acceptance of
Sprints 1, 2 and 3 remains separate. Sprint 2 state: [CURRENT before Sprint 3](docs/history/CURRENT-before-sprint3-2026-10-31.md).