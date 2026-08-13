# Architecture options

Status: IN PROGRESS — evidence-weighted pre-runtime score; final recommendation requires runtime or an evidenced pivot.

Scores use 1 (unacceptable) through 5 (strong evidence). For complexity, patch surface, and compatibility risk, a higher score means lower burden/risk. Weight is 1 unless shown; movement, cleanup/save safety, and maintainability are load-bearing and weighted 2.

| Criterion | Weight | A: true dual entity | B: mount + rider proxy | C: composite proxy | D: abstract state |
|---|---:|---:|---:|---:|---:|
| Movement/pathfinding stability | 2 | 2 | 4 | 5 | 5 |
| Selection/formation compatibility | 1 | 2 | 3 | 4 | 5 |
| Independent targeting potential | 1 | 5 | 3 | 2 | 1 |
| Future action economy correctness | 1 | 3 | 4 | 3 | 2 |
| Turn-based feasibility | 1 | 2 | 3 | 4 | 4 |
| Real-time feasibility | 1 | 2 | 4 | 4 | 5 |
| Save/uninstall safety | 2 | 2 | 3 | 4 | 5 |
| Animation/presentation quality potential | 1 | 3 | 3 | 4 | 2 |
| Indoor behavior | 1 | 2 | 2 | 3 | 5 |
| Implementation complexity | 2 | 1 | 3 | 3 | 5 |
| Harmony patch surface | 1 | 1 | 3 | 4 | 5 |
| Compatibility risk | 2 | 1 | 3 | 4 | 5 |
| Testability | 1 | 2 | 4 | 4 | 5 |
| Maintainability | 2 | 1 | 3 | 4 | 5 |
| Licensing/asset independence | 1 | 5 | 5 | 5 | 5 |
| Weighted total / 100 | — | **41** | **66** | **77** | **89** |

The totals do not automatically select D: they express current certainty and engineering risk. Phase 1 was specifically authorized to test whether B's movement assumptions can be proven. C and D remain the required pivots if B fails movement, cleanup, or presentation gates.

## A. True dual-entity relationship

- Summary: rider and mount retain two ordinary combat-entity roles, independent targeting, commands, and future actions.
- Exact Kingmaker seams: two `UnitCommands`, two stock movement agents, selection APIs, formation helper, combat state, and view transforms. Kingmaker has no relationship/controller/paired-command primitive.
- Required new subsystems: reciprocal runtime state, two-way commands, target/action/initiative coupling, collision resolution, selection/formation collapsing, save repair, and pose system.
- Harmony surface: broad central command, movement, targeting, turn, formation, selection, save, and animation hooks.
- Movement result: not implemented; two normal agents co-located would violate the one-authoritative-mover gate.
- Selection/cleanup/save: high residue risk; independent entity semantics conflict with a one-footprint pair.
- Future combat: highest theoretical independent targeting, but exact Wrath evidence shows even Wrath redirects hostile rider targets to the mount and uses deep paired commands—A is not the actual Wrath architecture.
- Fatal risks: K1, K5, K6, K7, K10. Complexity very high; confidence high that A should not be Phase 2.

## B. Authoritative mount plus rider combat proxy

- Summary: mount owns stock pathfinding/footprint; rider remains principal command/action representation; rider movement/avoidance is suppressed and its entity/view is synchronized.
- Exact Kingmaker seams: rider `AgentOverride`; stock `AgentASP.Stop`, `AvoidanceDisabled`, and `enabled`; private `ClickGroundHandler.RunCommand`; public selection APIs; pair-local `LateUpdate`; EventBus lifecycle; save/load cleanup guard.
- Required new subsystems: runtime-only relationship coordinator, game-state adapter, owned rider override, command routing, pair view anchor, selection/cancel forwarding, lifecycle cleanup, telemetry.
- Harmony surface: exact active-pair prefixes on private ground `RunCommand`, mount-selection redirection, Stop/Hold, continuous-control cleanup, and save/load entry; no global movement-tick replacement.
- Current evidence: contract and native-candidate gate PASS; movement, selection, cleanup, save timing, and visual result have not run.
- Proposed formation compromise: route the rider row to the mount and suppress the mount row. Exact contract inspection predicts one empty slot and unchanged unrelated-unit destinations; runtime behavior is UNKNOWN — MORE EVIDENCE REQUIRED.
- Future combat: closest to exact Wrath physical/logical split but requires substantial future action/target/initiative mapping.
- Save strategy: no serializable relationship; clear before save/load/area; Phase 2 must not assume persistence.
- Known incompatibilities: pre-existing rider `AgentOverride`; gamepad continuous movement; a Mammoth whose current size is not greater than the rider (normally pre-rank-7); indoor geometry; mods that patch the same private click seam.
- Fatal risks: K3, K4, K5, K7, K9, K10, K12. Complexity high but bounded; confidence medium until runtime.

## C. Composite proxy

- Summary: one visible/controllable entity represents the pair; companion is hidden, suspended, or abstracted behind a narrow proxy.
- Exact seams: native unit view/entity spawn/visibility, selection and command ownership, companion resource bookkeeping, runtime cleanup.
- Required new subsystems: composite presentation, hidden-companion lifecycle, target/damage proxy, save/uninstall reconstruction policy.
- Harmony surface: less movement/formation interception than B, but more entity/resource/target redirection.
- Movement/selection: one ordinary mover offers strong stability and one formation slot.
- Visual result: potentially better because rider/mount pose can be authored as a single presentation, but Kingmaker has no ready native composite. Proprietary Wrath assets remain forbidden.
- Future combat: possible through one principal combat entity, but independent mount attacks/targeting become compromises.
- Save strategy: dismount/reconstitute before save in early phases; a future explicit proxy persistence contract would be required.
- Fatal risks: acceptable original composite presentation may be unavailable; hidden-companion state can leak. Complexity high; confidence medium-low without a composite asset prototype.

## D. Abstract mounted state

- Summary: rider receives bounded movement/rules benefits and an optional cosmetic presence; no two-entity movement simulation.
- Exact seams: rider speed/state modifiers, one rider command agent, optional native visual child, cleanup events.
- Required new subsystems: abstract state/rules, cosmetic presentation, companion eligibility/resource gate.
- Harmony surface: narrowest; no formation or dual-agent delegation required.
- Movement/selection/cleanup: inherits ordinary rider behavior and is the safest path.
- Visual result: weakest Wrath resemblance; using a generic horse instead of the rider's exact companion would be an explicit abstract-representation compromise, not a dual-entity relationship.
- Future combat: can deliver simplified mounted rules but cannot honestly claim full two-entity simulation or independent mount actions.
- Save strategy: runtime fact or explicit small state with uninstall cleanup; Phase 1 would still clear at boundaries.
- Fatal risks: product compromise may be too abstract; otherwise few technical kill criteria. Complexity low; confidence high.

## Current recommendation gate

Architecture B is the only authorized movement experiment because exact contracts identify a scoped implementation. It is not yet the final Phase 2 recommendation. If the exact rank-7+ Mammoth fixture cannot be proven, if the pair fails matched doorway/corner controls, if cleanup leaves any movement/avoidance/view/selection residue, or if presentation is unusable, B must be disabled and C/D rescored without a broader workaround.
