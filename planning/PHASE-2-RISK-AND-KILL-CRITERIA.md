# Phase 2 risk and kill criteria

Status: IN PROGRESS

Phase 1 K1-K12 remain historical controls. None fired. The Phase 2 criteria below are independent and require evidence from the authorized Phase 2 branch; an unrun row is `TODO`, never PASS.

| ID | Criterion | Containment / required decision | Status |
|---|---|---|---|
| P2K1 | Acceptable pose requires proprietary redistribution | Stop the pose path; use original code/data or pivot | TODO |
| P2K2 | Every credible pose path requires unsafe global animator mutation | Stop presentation and evaluate documented C/D pivot | TODO |
| P2K3 | Pose, transform, animator, weapon, or UI state cannot restore residue-free | Stop qualification; repair or pivot | TODO |
| P2K4 | Player action/UI necessarily serializes an orphan or corrupts save state | Reject that surface; use transient fallback or stop | TODO |
| P2K5 | Mounted attacks irreducibly duplicate rule events, damage, or action costs | Disable combat and recommend pivot | TODO |
| P2K6 | Combat requires competing rider and mount movement agents | Reject implementation; preserve Architecture B invariant | TODO |
| P2K7 | Scoped combat patches materially alter non-mounted units | Remove/contain patches; stop if isolation is impossible | TODO |
| P2K8 | Rider/mount attacker or target identity cannot be coherent | Stop core combat qualification | TODO |
| P2K9 | Native save/load/area/death/disable lifecycle can leave a half-mounted pair | Repair or stop; no persistence claim | TODO |
| P2K10 | Runtime target or temporary equipment cannot be removed/restored exactly | Reject diagnostic target/equipment path | TODO |
| P2K11 | Mods, Working, or protected-save restoration cannot be guaranteed | Immediate critical stop; preserve durable transaction evidence | TODO |
| P2K12 | Repeatable RT/TB behavior requires unsafe broad patches | Stop or pivot the affected core feature | TODO |

AoO or charge failure alone is stretch-scoped: disable it by default and record `DEFER — EVIDENCED` unless it proves a core criterion. Architecture C and D remain evidence-triggered pivots only.

## Intake risk state

Exact environment, frozen Phase 1 artifact, canonical fixtures, protected-save authority, absence of runtime residue, and inherited offline gates are PASS at intake. This does not qualify any Phase 2 criterion.
