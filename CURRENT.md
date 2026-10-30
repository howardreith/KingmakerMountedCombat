# Sprint 2 — exploration speed, doors and collision safety: ENGINEERING PARTIAL, READY FOR FINAL REVIEW (2026-10-30, third iteration)

**SPRINT 2 ENGINEERING PARTIAL — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE PENDING.** No merge, release or
installation is selected.

Review branch `codex/mounted-combat-sprint2-movement-doors` from `main` `05a9efd0`. Product commits `5b02d4dd` and
`7d124fad` over the reviewed `4224afba`. Candidate `0.1.0-chunk7b-preview.100`: DLL `809573ad…`, MVID `2b00f9fa…`,
package sprint2k `4a3497f9…`, suite `sprint2k-7d124fad-20261030` `0e3c7701…`. Candidate tier 8/0; components 942/0;
reader 395/0; budget `sprint2` 73.22 of 90 min.

- **DOOR-01: repaired and PASS natively on the final binary** (r09: one click from 17.9 m, unstarted on every moving
  poll; click while moving replaces the move; native Stop ends an approach cleanly; r10: dwarf dungeon door). A refused
  click (wrong turn, busy mount, other pair command, same door) now mutates nothing (two-phase admission). The nine-second
  mechanism has an offline lifecycle regression plus the native unstarted-while-moving observations; the native >9 s
  route is NOT RUN. Turn-based door control: NOT RUN natively (contract named in the report).
- **SPACE-01: repaired and PASS natively on the final binary.** The settlement evaluates only on a confirmed navmesh;
  a deadline ends it Unresolved with no move and no clear-placement claim. r09: footprint 7/8 → one owned 0.5 m
  recovery, clear afterwards, second pair at the endpoint; r10: dwarf dungeon door settled 0.06 s, footprint 8/8,
  doorway traversed both ways. The owner's exact door: NOT RUN (owner save copied; needs a pre-owned single-pair mode).
- **MOVE-01: measured with the aligned sampler; no KMC clamp** (r09: party-paced 2.69 m/s at the walk gait, pair alone
  4.55, Horse unmounted 4.18, rider on foot 3.02). The owner's surface: NOT RUN natively (same contract).
- **Lab:** `chunk4-traversal-core` runs under the existing Areas isolation (r10: generated tree quarantined, intake tree
  restored exactly); the door probe restores the fixture layout after its inputs (r08's fault). Details:
  [docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md](docs/SPRINT2-MOVEMENT-DOORS-2026-10-30.md), "Third iteration".

Live installation at closeout: Mods tree empty as at intake, loader parameters and profile root (including `Areas\`) at
their intake digests, protected saves unchanged, no lock or game process, no candidate installed. Owner play acceptance
of Sprint 1 and Sprint 2 remains separate. Sprint 1 state: [CURRENT before Sprint 2](docs/history/CURRENT-before-sprint2-2026-10-30.md).
