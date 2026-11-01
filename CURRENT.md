# Sprint 3 — mounted trap disarm and spell target approach: ENGINEERING PARTIAL, READY FOR REVIEW (2026-11-01, second review)

**SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation
is selected.

Review branch `codex/mounted-combat-sprint3-traps-spell-approach` (PR #8) from `main` `6240d91e`. Product commits
`f22e6bd1` (carried approach), `3980e1cf` (turn-based admission of the delegated move), `6c19b814` (first-review
ownership corrections A and B) and `24c92355` (second-review queued-order correction, trap-only selection with the
declared door preparation). Current candidate sprint3m (`24c92355`; DLL `9cef7894…`, MVID `2f57647e…`, package
`c9ff0978…`, suite `sprint3m-24c92355-20261101`). Budget `sprint3` 88.75 of 105 min (one +15 adjustment).

- **Ownership corrections A and B (first review): done.** Admission before release; release only the carrier's own
  movement, successor kept.
- **Queued-order correction (second review): done.** Mount-request displacement is classified from the slots native
  `Run` left; a queued order or a non-moving action in another slot no longer releases the approach or cancels the
  rider's command; cleanup keeps its queue-aware successor protection. Four focused behaviour regressions; 966
  component tests; Candidate tier PASS 8/8.
- **Ownership neighbor natively: PASS** on sprint3l (r18, held-charge row).
- **SPELL-01: PASS natively** on earlier bytes (real time up to r16 and turn-based r14, r17 on sprint3k).
- **TRAP-01: NOT DEMONSTRATED natively; external input required.** The one declared Working-fixture door (r19 on
  sprint3m) has an awake two-unit hostile group 19.4 and 23.7 m beyond it, so the route requires combat and stopped
  before any walk or click. The two earlier alternatives have no reachable trap. The save to make in normal play is
  described in the report's *Second review* section.

Records: [docs/SPRINT3-TRAPS-SPELL-APPROACH.md](docs/SPRINT3-TRAPS-SPELL-APPROACH.md), `AUTONOMOUS-RESUME.md`, PR #8.

Live installation at closeout: Mods tree empty, loader parameters and profile root at intake, protected saves and the
owner's originals unchanged, no transaction lock or game process, no candidate installed. Owner play acceptance of
Sprints 1, 2 and 3 remains separate. Sprint 2 state: [CURRENT before Sprint 3](docs/history/CURRENT-before-sprint3-2026-10-31.md).
