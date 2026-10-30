# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING COMPLETE, READY FOR FINAL REVIEW (2026-10-30)

**SPRINT 2 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0` (the PR #6 merge). HEAD `3ef8119f`
(reader correction) over `63d83e2e` (product + diagnostics). Candidate `0.1.0-chunk7b-preview.100`: DLL `85542fbf…`,
MVID `9d761c39…` (byte-identical in packages sprint2f `b915dba2…` / suite `sprint2f-63d83e2e-20261030` and sprint2g
`6d29b6f8…` / suite `sprint2g-3ef8119f-20261030`). Candidate tier 7/0; reader tests 389/0; budget `sprint2` 46.13 of 90 min.

- **DOOR-01: repaired and PASS natively.** One click opens a door from distance (r05: 17.8 m on the Old Sycamore seed;
  r07: the dwarf dungeon door on the Working fixture); the routed command is never started while the mount moves, so the
  native nine-second interrupt cannot end an approach; a click while the mount moves replaces the move; the native Stop
  ends an approach cleanly; the stall clock follows the actual mode and movement opportunity, not the paired-activation
  toggle.
- **SPACE-01: repaired and PASS natively.** After the door's navmesh cut has landed, the pair's body footprint is sampled;
  a footprint overlapping an obstacle is recovered through exactly one owned native move to a reachable body-clear point
  (r05: 0.5 m, footprint clear afterwards), never a transport; on the dungeon door the settled footprint was clear and the
  pair traversed the doorway both ways. Closing a toggling-cut door over the footprint is refused before any cost.
- **MOVE-01: measured with the aligned sampler; no KMC clamp.** Party-paced pair 2.54 m/s at the native walk gait, pair
  alone 4.17, Horse unmounted 4.33, rider on foot 3.04 (v1 figures 2–4 % higher). Native party pacing and the walk gait
  under 95 % of own speed explain the report.
- **Coverage limits:** the owner's own roguelike-surface and dungeon-floor saves were located and copied read-only
  (`fixtures\owner-supplied\KMC_S2_*_CANDIDATE.*`) but are not admissible through the two-pair owner-seed contract; a
  turn-based door control was not run natively. See the completion section of
  [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md).

Live installation after every run: Mods tree empty as at intake, loader parameters and preferences exact, protected saves
unchanged, profile root at its intake digest, no lock or game process, no candidate installed. Owner play acceptance of
Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
