# Architecture options

Status: BLOCKED — CRITICAL

Evidence ledger: source 21 PASS / 0 FAIL; pure/component 56 PASS / 0 FAIL; guarded harness/protocol 58 PASS / 0 FAIL; assembly-backed 47 PASS / 0 FAIL (Kingmaker 36, Wrath 11). Runtime mission rows remain 1 PASS / 0 FAIL / 24 DEFER — EVIDENCED. The filename gate is Baseline=1 / Working=0, with rejected near-match `Manual_299_KMC_AUTOMATION_WORKING_.zks`; no fixture archive or mounted-pair runtime sample was opened or produced.

Scores use 1 (unacceptable) through 5 (strong evidence). For complexity, patch surface, and compatibility risk, higher means lower burden/risk. Movement, cleanup/save safety, complexity, compatibility, and maintainability are weighted 2. Scores reflect exact contracts and metadata, not unrun runtime behavior.

| Criterion | Weight | A: true dual entity | B: mount + rider proxy | C: composite proxy | D: abstract state |
|---|---:|---:|---:|---:|---:|
| Movement/pathfinding stability | 2 | 2 | 4 | 5 | 5 |
| Selection/formation compatibility | 1 | 2 | 3 | 4 | 5 |
| Independent targeting potential | 1 | 5 | 3 | 2 | 1 |
| Future action economy correctness | 1 | 3 | 4 | 3 | 2 |
| Turn-based feasibility | 1 | 2 | 3 | 4 | 4 |
| Real-time feasibility | 1 | 2 | 4 | 4 | 5 |
| Save/uninstall safety | 2 | 2 | 3 | 4 | 5 |
| Animation/presentation potential | 1 | 3 | 3 | 4 | 2 |
| Indoor behavior | 1 | 2 | 2 | 3 | 5 |
| Implementation complexity | 2 | 1 | 3 | 3 | 5 |
| Harmony patch surface | 1 | 1 | 3 | 4 | 5 |
| Compatibility risk | 2 | 1 | 3 | 4 | 5 |
| Testability | 1 | 2 | 4 | 4 | 5 |
| Maintainability | 2 | 1 | 3 | 4 | 5 |
| Licensing/asset independence | 1 | 5 | 5 | 5 | 5 |
| Weighted total / 100 | — | **41** | **66** | **77** | **89** |

The totals measure current engineering safety, not product desirability. B remains the only authorized Phase 1 movement experiment because exact Kingmaker seams support a bounded test. D scores highest because it avoids the unproven two-entity contracts; that is not evidence that the user-facing compromise is acceptable.

Score confidence is high for licensing, absence of mounted primitives, patch surface, and relative complexity; medium for command/movement seams; low for B/C presentation and indoor behavior because no fixture-backed view ran. No score is derived from sunk implementation effort.

## A. True dual-entity relationship

- Summary: two independently targetable/action-capable entities remain ordinary movers and combatants.
- Exact Kingmaker seams: two `UnitCommands`, two stock agents, selection/formation, combat state, and view transforms; no paired-command or relationship primitive.
- Required subsystems: reciprocal state, two-way commands, initiative/action/target coupling, collision resolution, selection/formation collapse, persistence repair, and pose system.
- Harmony patch surface: broad central movement, command, targeting, turn, selection, formation, save, and animation hooks.
- Movement result: not implemented; two co-located ordinary agents violate the pre-code one-mover gate.
- Selection result: unknown and structurally high risk because both entities remain independently selectable.
- Cleanup result: deterministic proof absent; residue surface is widest.
- Visual result: unknown; native assets do not solve two-root synchronization.
- Future combat feasibility: theoretically strongest independent targeting, but exact Wrath redirects hostile rider targets and uses deep paired commands; A is not Wrath's real architecture.
- Turn-based / real-time: both require extensive paired action and command ownership; unproven.
- Save strategy: new reciprocal persistent parts plus repair would be required; prohibited for this phase.
- Known incompatibilities: ordinary avoidance, formation slots, target selection, and mods patching central command/movement code.
- Licensing/assets: original code and Kingmaker-native assets could remain independent, but no acceptable presentation is proven.
- Estimated complexity: very high; fatal risks K1/K5/K6/K7/K10; confidence high that A should not be Phase 2.

## B. Authoritative mount plus rider combat proxy

- Summary: mount owns stock pathfinding/footprint; rider remains principal logical representation; rider stock pathing/avoidance is suppressed and its state/view is synchronized.
- Exact Kingmaker seams: `AgentOverride`; `AgentASP.Stop`, `AvoidanceDisabled`, `enabled`; exact `ClickGroundHandler.RunCommand`; public selection; pair-local `LateUpdate`; EventBus lifecycle; save/load cleanup.
- Required subsystems: runtime coordinator, exact pair adapter, owned rider override, command routing, anchor presentation, selection/cancel forwarding, cleanup, telemetry, and later combat proxy rules.
- Harmony patch surface: eight exact active-pair entry/control guards with an exact Assembly-CSharp MVID gate; no global movement tick replacement.
- Movement result: implemented default-off but not executed with a pair. Contract predicts one mount mover; stability, drift, doorway, and corners remain unknown.
- Selection result: routing/snapshot mechanism implemented; runtime selection and formation results unknown.
- Cleanup result: rollback, retryable residue ownership, and deterministic cleanup tests PASS; in-game pair cleanup unrun.
- Visual result: rank-7+ Mammoth `Spine` anchor experiment implemented; rider identity, pose, clipping, and animation stability unknown.
- Future combat feasibility: closest to Wrath's physical/logical split, but targeting, paired attacks, initiative, reach, and action economy remain Phase 2 contracts.
- Turn-based / real-time: both plausible from exact seams; Phase 1 clears at mode/combat boundaries. The offline boundary engine directly invokes the exact mode handlers, which tests adapter behavior but does not prove real controller/EventBus delivery timing; neither mode is proven while mounted.
- Save strategy: no entity part or mounted JSON; exact save/load prefixes clean before the guarded boundary; the area scenario pre-cleans before real `ReloadArea(AutoSaveMode.None)`. The save-safety row deliberately issues no stock save and can prove only cleanup, unchanged Working bytes, zero save requests, and no KMC relationship serialization—not a completed Kingmaker save cycle.
- Known incompatibilities: pre-existing rider override, continuous/gamepad movement, non-Medium rider, non-larger Mammoth, indoor geometry, or another mod patching the same private click seam.
- Licensing/assets: original implementation, Kingmaker-native Mammoth only, no Wrath/runtime mod dependency.
- Estimated complexity: high but bounded; fatal risks K3/K4/K5/K7/K9/K10; confidence medium in the seam and low in runtime outcome.

## C. Composite proxy

- Summary: one visible/controllable composite represents the pair while companion state is hidden or suspended.
- Exact Kingmaker seams: one ordinary unit mover/view, visibility/entity lifecycle, selection, companion resource bookkeeping, damage/target forwarding.
- Required subsystems: composite presentation, hidden-companion lifecycle, damage/action proxy, restoration and uninstall policy.
- Harmony patch surface: narrower movement/formation interception than B, broader entity/resource/target redirection.
- Movement result: not implemented; one ordinary mover is structurally favorable.
- Selection result: not implemented; one selection/formation slot is structurally favorable.
- Cleanup result: unknown; hidden-companion leakage is the load-bearing risk.
- Visual result: unknown; no native ready-made rider/Mammoth composite exists.
- Future combat feasibility: viable through one principal entity, with explicit compromise to independent mount attacks/targeting.
- Turn-based / real-time: likely simpler than B because one command owner; unproven.
- Save strategy: early versions must reconstitute before save; future proxy persistence requires a separate contract.
- Known incompatibilities: companion-dependent mechanics and mods expecting a visible/live pet entity.
- Licensing/assets: must use original/Kingmaker-native presentation; Wrath assets remain forbidden.
- Estimated complexity: high; fatal risks are presentation availability and hidden-state residue; confidence medium-low.

## D. Abstract mounted state

- Summary: rider receives bounded mechanics and optional cosmetic representation; no full two-entity movement simulation.
- Exact Kingmaker seams: rider speed/state modifiers, one ordinary command agent, eligibility/resource checks, cleanup events, optional view child.
- Required subsystems: abstract rules state, companion eligibility/resource accounting, cosmetic presentation, uninstall cleanup.
- Harmony patch surface: narrowest; no dual-agent or formation delegation.
- Movement result: not implemented, but inherits ordinary rider movement.
- Selection result: inherits ordinary rider selection/formation.
- Cleanup result: structurally simplest; no pair override has to be restored.
- Visual result: likely weakest Wrath resemblance; a generic horse cosmetic would be an explicit non-identical representation compromise, while Mammoth identity remains visually unproven.
- Future combat feasibility: simplified mounted bonuses/restrictions are possible; independent mount actions or true dual targeting cannot be claimed.
- Turn-based / real-time: most likely to share ordinary game behavior; still requires rule-specific qualification.
- Save strategy: runtime fact cleared at boundaries or a small explicit future state with uninstall handling.
- Known incompatibilities: product expectation may reject the abstraction; companion mechanics may not align with cosmetic state.
- Licensing/assets: original rules and Kingmaker-native cosmetic only.
- Estimated complexity: low; few technical fatal risks; confidence high in stability, low in player acceptance.

## Decision

No final Phase 2 architecture is authorized. The correct outcome is `BLOCKED — CRITICAL`, not a proceed or pivot claim: Architecture B has a plausible scoped seam and a default-off implementation, but zero pair samples; no B kill criterion has fired, and C/D have not been prototyped. The guard is implemented and offline-qualified, but the exact audit is Baseline=1 / Working=0 because the only Working-looking file has a prohibited trailing underscore. After an exact Working fixture is recreated manually and the 1/1 filename plus descriptor gate passes, resume B only for the bounded Phase 1 movement/lifecycle matrix. A B failure at a listed kill criterion must disable it and rescore C/D; a full B qualification may support a later proceed recommendation.
