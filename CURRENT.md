# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-30)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0` (the PR #6 merge). HEAD `b3f78482`
(lab recovery fix) over `4c57b5a8` (delta-review fixes), `3ef8119f` (reader correction) and `63d83e2e` (completion
product + diagnostics). Candidate `0.1.0-chunk7b-preview.100`: DLL `ee2f72a9…`, MVID `ee7142af…`, package sprint2i
`91165cf5…`, suite `sprint2i-b3f78482-20261030` `f2235aaa…`. Candidate tier 7/0 (sprint2h bytes = same DLL); reader
tests 389/0; budget `sprint2` 58.35 of 90 min.

- **DOOR-01: repaired and PASS natively.** One click opens a door from distance (r05 17.8 m and r08 25.6 m on the Old
  Sycamore seed; r07 the dwarf dungeon door on the Working fixture); the routed command is never started while the mount
  moves, so the native nine-second interrupt cannot end an approach; a click while the mount moves replaces the move; the
  native Stop ends an approach cleanly; the stall clock keys on the live delegated move and the actual mode/movement
  opportunity, not on the paired-activation toggle. Turn-based door control: NOT RUN natively (offline-proven turn-aware).
- **SPACE-01: repaired and PASS natively.** After the door's navmesh cut has landed, the pair's body footprint is sampled;
  a footprint overlapping an obstacle is recovered through exactly one owned native move to a reachable body-clear point
  (r05/r08: 0.5 m, clear afterwards), never a transport; on the dungeon door the settled footprint was clear and the pair
  traversed the doorway both ways (r07). Closing a toggling-cut door over the footprint is refused before any cost. The
  owner's exact door: NOT RUN (owner save located and copied, not admissible yet).
- **MOVE-01: measured with the aligned sampler; no KMC clamp.** Party-paced pair 2.54–2.82 m/s at the native walk gait,
  pair alone 4.17–4.55, Horse unmounted 4.33, rider on foot 3.04 (v1 figures 2–4 % higher). Native party pacing and the
  walk gait under 95 % of own speed explain the report. The owner's surface: NOT RUN natively (same owner save).
- **Runs on the final binary:** r08's door probe passed in-game; the run then failed in an unrelated pre-existing cycle
  step (route goal search after the longer setup spread the actors). r07's outer verdict failed on the engine `Areas\`
  unpack cache (lab classification; its stale lock was recovered after a one-line recovery fix). Details in the
  completion and review-fix sections of [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md).

Live installation at closeout: Mods tree empty as at intake, loader parameters and profile root at their intake digests,
protected saves unchanged, no lock or game process, no candidate installed. Owner play acceptance of Sprint 1 and
Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
