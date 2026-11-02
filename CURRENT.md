# Sprint 4 — natural attacks and real-time retargeting (2026-11-01)

**SPRINT 4 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation
is selected.

Branch `codex/mounted-combat-sprint4-natural-attacks-retargeting` (PR #9) from `main` `2c2e6fc6`. Final compiled candidate
sprint4i (`526c32e6`; DLL `8236574a…`, MVID `fad7f560…`, package `3801c2d7…`). Later commits change readers, the recovery tool
and docs only. Budget `sprint4`: 74.3 of 90 minutes, no adjustment.

- **ATTACK-01:** Rider Primary and Mount Primary retired from every ordinary surface; definitions stay resolvable; exact
  stale slots and auto-use selections are reconciled.
- **AI-01:** a rider's native brain proposal has its own admission and becomes the pair's ordinary intent. Clicks, moves,
  casts, Stop, AI off and Hold take precedence.
- **Corrections after the review of `2e43e289`:** Hold survives every native Run of an automatic command, including its later
  run from the queue, on whichever actor executes. The move hold follows the exact move's own native completion.

Sprint 3 is owner-accepted for advancement with the owner's manual trap check pending; its automated trap attempt stays
unexecuted and is not a prerequisite.

Record: [docs/SPRINT4-NATURAL-ATTACKS-RETARGETING.md](docs/SPRINT4-NATURAL-ATTACKS-RETARGETING.md). Prior state:
[CURRENT before Sprint 4](docs/history/CURRENT-before-sprint4-2026-11-01.md).

Live installation at closeout: Mods tree empty, the same as each run's captured start; protected saves unchanged; no transaction
lock or game process; no candidate installed. Owner play acceptance of Sprints 1 to 4 remains separate.
