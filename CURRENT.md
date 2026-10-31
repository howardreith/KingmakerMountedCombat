# Sprint 3 — mounted trap disarm and spell target approach: ENGINEERING PARTIAL, READY FOR REVIEW (2026-10-31)

**SPRINT 3 ENGINEERING PARTIAL — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.** No merge, release or installation is selected.

Review branch `codex/mounted-combat-sprint3-traps-spell-approach` from `main` `6240d91e` (the PR #7 merge). Product
commits `f22e6bd1` (the mount carries the rider's own native ability and discovered-trap commands to their native
range; `MountedCarriedApproachPolicy`, `MountedCarriedApproach`) and `3980e1cf` (the delegated approach move is admitted
on the rider's turn in turn-based combat: the exact-type test on `UnitMoveTo` had refused it). Diagnostics, readers and
registries `f12662b6`..`845892d7`. Final candidate sprint3k (`845892d7`; DLL `ad961c3d…`, MVID `565f890d…`, package `75521a56…`, suite `sprint3k-845892d7-20261031`). Budget `sprint3` 65.77 of 90 min.

- **SPELL-01 real time: PASS natively** (r08 on sprint3e, r12 on sprint3h, r13/r15/r16 on sprint3i/3j/3k): ordinary Cure Light Wounds on a
  distant ally (mount approach, native cast, one held-touch delivery), the held charge delivered after a Stop and a
  second ordinary click, one offensive touch on a hostile (native touch attack roll, one expenditure; a legal miss keeps
  the charge), one ranged spell beyond range (the mount approaches to the spell's own range, projectile roll).
- **SPELL-01 turn-based: PASS natively** (r10/r11 on sprint3f/3g, r14 natively, r17 end to end on sprint3k): the cast on the rider's turn carries
  the mount's movement on that turn and delivers once; with insufficient movement the carried approach ends by the native
  rule, the charge survives the turn boundary and is delivered on the next rider turn through a second ordinary click.
- **TRAP-01: NOT DEMONSTRATED natively (fixture limit).** The carried trap approach is implemented and proven offline;
  all four traps of the Working fixture sit in navmesh areas disconnected from the party (A* areas 13 and 7 against
  10), the only nearby entrance is a secret passage whose native interaction refuses, and saved discovery does not
  survive the load (r06 to r16). Required input: a disposable save with a reachable trap (report, Risks).
- Unmounted controls are untouched by the carrier (policy regression); an unmounted native trap/spell control row was not
  run natively this sprint.

Records: [docs/SPRINT3-TRAPS-SPELL-APPROACH.md](docs/SPRINT3-TRAPS-SPELL-APPROACH.md), `AUTONOMOUS-RESUME.md`, the PR opened from this branch.

Live installation at closeout: Mods tree empty, loader parameters and profile root at intake, protected saves and the
owner's originals unchanged, no lock or game process, no candidate installed. Owner play acceptance of Sprints 1, 2 and 3
remains separate. Sprint 2 state: [CURRENT before Sprint 3](docs/history/CURRENT-before-sprint3-2026-10-31.md).
