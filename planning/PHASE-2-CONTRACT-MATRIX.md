# Phase 2 contract matrix

Status: IN PROGRESS

Authority: `planning/MOUNTED-COMBAT-PHASE-2-MASTER-MISSION.md`, SHA-256 `d4e7340954af34fb011434ec5952e893a2b109769f7941bd6e52c45f455b541e`.

Historical baseline: Phase 1 evidence commit `d5bd7fa9c434f04c6f8487b61ea49e3cf983c397`, final ledger `25 PASS / 0 attributable FAIL / 0 DEFER`, Architecture B selected. Phase 1 claims remain bounded exactly as recorded; no Phase 2 row is qualified by inheritance alone.

| Responsibility | Exact inherited contract | Phase 2 contract / narrow seam | Required evidence | Status |
|---|---|---|---|---|
| Relationship state | Runtime-only coordinator; no serializable unit part | Retain one transient rider/Mammoth pair and explicit transition guards; never intentionally serialize or remount | Component transitions plus native save/load/area/uninstall scenarios | IN PROGRESS |
| Persistence/uninstall | Direct pre-save cleanup only; real load prefix; no save round trip | Clean dismount before save/load/area; no saved fact, unit part, or relationship; disabled/uninstalled mod leaves ordinary units | Exact native delivery, save-safe telemetry, load with feature disabled where safe | TODO |
| Player action | Diagnostic-only activation in Phase 1 | One reversible player-facing Mount/Dismount surface with exact eligibility result; fail closed and rollback | Visibility/availability/reason/action tests and two fresh-process flows | TODO |
| Native lifecycle | Several Phase 1 handlers were invoked directly | Observe and bind actual delivery when safely available; retain truthful claim limits otherwise | Event source, handler entry, relationship before/after, cleanup residue | TODO |
| Movement authority | Mammoth is sole stock pathfinding mover; rider is logical principal | Preserve Architecture B without a second pathing agent or global movement replacement | Narrow Phase 1 regression rows and all new movement-bearing rows | IN PROGRESS |
| Pose/presentation | Stable root-local `Spine` position anchor; rigid upright pose unacceptable | View-owned, deterministic, reversible pose adapter applied after ordinary animation; original code/data only | Baseline capture/restore, idle/walk/run/turn/equipment telemetry, frame cost, images | TODO |
| Selection/UI/camera | Rider selection normalization qualified; portrait/camera/action bar unproved | Rider remains principal selection, portrait/action-bar owner; observe click selection, circle, and camera follow directly | Structured UI identities plus usable UI captures | TODO |
| Rider melee | Not present | After visual acceptance only: one stationary ordinary melee attack owned by rider, mount attack suppressed | RT/TB rule-event identity, attack/damage/action counts, controls | TODO |
| Mount attack | Not present | After visual acceptance only: one explicit Mammoth primary natural attack, rider suppressed | RT/TB rule-event identity, attack/damage/action counts, controls | TODO |
| Action economy | Absent in Kingmaker; Wrath uses paired state | After visual acceptance: one documented pair resource model with no duplicated turn/action | Exact resources before/after, initiative, cancellation, RT/TB | TODO |
| Targeting/reach | Not present | Pair-scoped attacker/target spatial adapter; never global unit reach mutation | Range boundaries, target identity, non-mounted isolation | TODO |
| Diagnostic hostile | No Phase 1 target | Prefer an existing safe hostile; otherwise stock runtime-only, rewardless, removable hostile under exact Working only | Spawn/removal/faction/reward/entity residue and external restoration | TODO |
| Stretch reach/AoO/charge | Not authorized in Phase 1 | Only after core alpha is green; disable/defer independently if unsafe | Feature-specific RT/TB and non-mounted isolation | TODO |
| Packaging/publication | Frozen Phase 1 diagnostic only | New private Phase 2 identities; guarded non-force branch publication; no release/main merge | Clean package allowlist, hashes/MVID, local/remote equality | TODO |

## Ordering gate

Persistence, native lifecycle, player action, presentation, UI, and camera contracts must qualify before the exact manual-review build is published. Combat rows remain `TODO` and production implementation is forbidden until the user explicitly accepts that exact build.
