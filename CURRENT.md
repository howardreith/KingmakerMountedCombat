# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR REVIEW (2026-10-30)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release
or installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0` (the PR #6 merge, Sprint 1
accepted). Candidate `0.1.0-chunk7b-preview.99` built at `5caeb275` (sprint2e): DLL `7cb519a4…`, MVID
`a4531312…`, package `…preview.99-sprint2e-diagnostic.zip` `0031ae56…`, suite `sprint2e-5caeb275-20261030`
`96537e39…`. Candidate tier 7/0; budget `sprint2` 22.83 of 90 min.

- **DOOR-01: repaired and PASS natively** on the owner's Old Sycamore door seed (r04): one click from
  18.7 m opens the door (the routed command no longer starts before the mount arrives, so the native
  nine-second command interrupt no longer ends long approaches); a click while the mount carries an
  ordinary move replaces that move natively instead of being refused.
- **SPACE-01: repaired on representative geometry.** The approach targets the native map-object point with
  the door's LOS object and the native range predicate; occupancy is revalidated after the door changes
  state with a bounded nearby repath (never a transport). Both pairs stayed on the navmesh at the door;
  the capture shows the pair outside the doorway. The owner's narrow dungeon door is NOT RUN (no fixture).
- **MOVE-01: measured; no KMC clamp found.** Matched set on the same fixture: party-paced pair at the party
  limit with a native walk gait (the engine selects Slow under 95% of a unit's own speed), pair alone
  4.43 m/s, the same Horse unmounted 4.23 m/s, rider on foot 3.07 m/s. The roguelike surface is NOT RUN
  (no fixture).

Details, batch, cost, restoration and the owner play checklist:
[docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md). Sprint 1 state:
[CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).

Live installation after every run: Mods tree empty as at intake, loader parameters and preferences exact,
protected saves unchanged, no lock or game process, no candidate installed. Owner play acceptance of
Sprint 1 and Sprint 2 remains separate.
