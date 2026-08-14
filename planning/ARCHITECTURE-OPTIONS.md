# Architecture options

Status: BLOCKED — CRITICAL

The exact fixture gate is PASS: one canonical Baseline, one canonical Working, zero KMC-looking near-matches, distinct paths, exact internal identities, matching campaign/area identity, immutable Baseline, and Working-only write authorization. Current gates are source 21 PASS / 0 FAIL; Release build PASS; pure/component 112 PASS / 0 FAIL; visual-capture contracts 12 PASS / 0 FAIL; guarded harness/protocol 105 PASS / 0 FAIL; and assembly-backed 69 PASS / 0 FAIL (Kingmaker 58, Wrath 11). The final named runtime-row ledger is 22 PASS / 0 attributable FAIL / 3 DEFER — EVIDENCED.

Scores use 1 (unacceptable) through 5 (strong evidence). For complexity, patch surface, and compatibility risk, higher means lower burden/risk. Movement, cleanup/save safety, complexity, compatibility, and maintainability are weighted 2. The scores remain unchanged after runtime qualification: the new evidence increases confidence in Architecture B's bounded pair-only behavior, but doorway repeatability, away/back selection, party formation, combat behavior, persistence, and polished presentation remain unproved.

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

The totals measure engineering safety, not product desirability. D scores highest because it avoids two-entity contracts; that does not establish that its user-facing abstraction is acceptable. B is the only architecture with live pair evidence and remains the provisional product-fit leader. No score is derived from sunk implementation effort.

Confidence is high for licensing, absence of native mounted primitives, the exact patch surface, one-mover authority, tested pair-only movement, cleanup, and external restoration. Confidence is medium for B's command and attachment seams. It remains low for non-pair selection/formation, repeatable indoor behavior, polished pose, combat/action coupling, and persistence. C and D remain structural paper alternatives rather than runtime-qualified implementations.

## A. True dual-entity relationship

- Summary: two independently targetable/action-capable entities remain ordinary movers and combatants.
- Exact Kingmaker seams: two `UnitCommands`, two stock agents, selection/formation, combat state, and view transforms; no paired-command or relationship primitive.
- Required subsystems: reciprocal state, two-way commands, initiative/action/target coupling, collision resolution, selection/formation collapse, persistence repair, and pose system.
- Harmony patch surface: broad central movement, command, targeting, turn, selection, formation, save, and animation hooks.
- Movement result: not implemented; two co-located ordinary agents violate the pre-code one-mover gate.
- Selection result: unknown and structurally high risk because both entities remain independently selectable.
- Cleanup result: deterministic proof absent; residue surface is widest.
- Visual result: native assets do not solve two-root synchronization.
- Future combat feasibility: theoretically strongest independent targeting, but exact Wrath behavior redirects hostile rider targets and uses deep paired commands; A is not Wrath's actual architecture.
- Turn-based / real-time: both require extensive paired action and command ownership; unproven.
- Save strategy: new reciprocal persistent parts plus repair would be required; prohibited for this phase.
- Known incompatibilities: ordinary avoidance, formation slots, target selection, and mods patching central command/movement code.
- Licensing/assets: original code and Kingmaker-native assets could remain independent.
- Estimated complexity: very high; confidence is high that A should not be Phase 2.

## B. Authoritative mount plus rider combat proxy

- Summary: the mount owns stock pathfinding and physical footprint; the rider remains the principal logical representation with pair-scoped synchronization and command delegation.
- Exact Kingmaker seams: `AgentOverride`; `AgentASP.Stop`, `AvoidanceDisabled`, and `enabled`; exact `ClickGroundHandler.RunCommand`; public selection; pair-local `LateUpdate`; EventBus lifecycle; and save/load cleanup.
- Required subsystems: runtime coordinator, exact pair adapter, owned rider override, command routing, root-local anchor presentation, selection/cancel forwarding, cleanup, telemetry, and later combat proxy rules.
- Harmony patch surface: eight exact active-pair entry/control guards with an exact Assembly-CSharp MVID gate; no global movement-tick replacement.
- Movement result: PASS for the runnable pair-only subset. Open ground, stop/start, turns/corners, pause/unpause, and destination cancellation each passed A/B in fresh processes. The Mammoth alone remained authoritative; synchronization, oscillation, stuck, command-replacement, selection-loss, and cleanup-residue gates passed. Repeatable doorway qualification remains DEFER — EVIDENCED because native hostiles forced the correct combat cleanup after successful unmounted control and stable mounted entry.
- Selection result: pair selection retention and cleanup restoration passed repeatedly. Required away/back selection and party formation are DEFER — EVIDENCED because the exact Working fixture has no eligible directly controllable non-pair unit; non-mounted party isolation is therefore not proven.
- Cleanup result: lifecycle suites A/B each passed all 8 rows and 339 assertions. Movement and five individual boundary runs ended Unmounted with stock agents, avoidance, override/component state, `ForbidRotation`, attachment parent/lease, commands, pause state, and selection restored without residue. Lifecycle death/combat/area rows remain scoped to direct handler calls unless native delivery was separately observed.
- Visual result: `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. Usable game-camera frames show a stable readable humanoid-on-Mammoth silhouette, but the rider remains rigidly upright with the lower body intersecting or occluded by the Mammoth back and has no seated pose, reins, or saddle. This does not fire K9.
- Future combat feasibility: closest to Wrath's physical/logical split, but targeting, paired attacks, initiative, reach, and action economy remain Phase 2 contracts.
- Turn-based / real-time: both individual boundary rows PASS only for direct lifecycle-handler cleanup; `nativeDeliveryObserved=false`, so real controller/EventBus delivery is not claimed.
- Save strategy: no entity part or mounted JSON. Save-safety PASS directly calls `GuardBoundary(SaveRequested)` and invokes no stock `SaveRoutine` or serialization. Load-safety PASS performs a real exact-Working `Game.LoadGame` through the native `LoadRoutine` prefix. Area-transition PASS directly latches `OnAreaBeginUnloading` cleanup before real `ReloadArea(AutoSaveMode.None)`; native area-event delivery is not independently qualified.
- Known incompatibilities: pre-existing rider override, continuous/gamepad movement, non-Medium rider, non-larger Mammoth, indoor geometry, or another mod patching the same private click seam.
- Licensing/assets: original implementation and Kingmaker-native Mammoth only; no Wrath runtime or asset dependency.
- Estimated complexity: high but bounded. Confidence is high in the qualified movement/cleanup subset, medium in the architecture seam, and low in the mandatory fixture-limited and future-combat contracts.

## C. Composite proxy

- Summary: one visible/controllable composite represents the pair while companion state is hidden or suspended.
- Exact Kingmaker seams: one ordinary unit mover/view, visibility/entity lifecycle, selection, companion resource bookkeeping, and damage/target forwarding.
- Required subsystems: composite presentation, hidden-companion lifecycle, damage/action proxy, restoration, and uninstall policy.
- Harmony patch surface: narrower movement/formation interception than B, broader entity/resource/target redirection.
- Movement result: not implemented; one ordinary mover is structurally favorable.
- Selection result: not implemented; one selection/formation slot is structurally favorable.
- Cleanup result: unknown; hidden-companion leakage is the load-bearing risk.
- Visual result: unknown; no native ready-made rider/Mammoth composite exists.
- Future combat feasibility: viable through one principal entity, with an explicit compromise to independent mount attacks/targeting.
- Turn-based / real-time: likely simpler than B because there is one command owner; unproven.
- Save strategy: early versions must reconstitute before save; future proxy persistence requires a separate contract.
- Known incompatibilities: companion-dependent mechanics and mods expecting a visible/live pet entity.
- Licensing/assets: must use original or Kingmaker-native presentation; Wrath assets remain forbidden.
- Estimated complexity: high; fatal risks are presentation availability and hidden-state residue; confidence medium-low.

## D. Abstract mounted state

- Summary: the rider receives bounded mechanics and optional cosmetic representation; there is no full two-entity movement simulation.
- Exact Kingmaker seams: rider speed/state modifiers, one ordinary command agent, eligibility/resource checks, cleanup events, and an optional view child.
- Required subsystems: abstract rules state, companion eligibility/resource accounting, cosmetic presentation, and uninstall cleanup.
- Harmony patch surface: narrowest; no dual-agent or formation delegation.
- Movement result: not implemented, but inherits ordinary rider movement.
- Selection result: inherits ordinary rider selection/formation.
- Cleanup result: structurally simplest; no pair override has to be restored.
- Visual result: weakest Wrath resemblance; a cosmetic representation is an explicit product compromise.
- Future combat feasibility: simplified mounted bonuses/restrictions are possible; independent mount actions or true dual targeting cannot be claimed.
- Turn-based / real-time: most likely to share ordinary game behavior; still requires rule-specific qualification.
- Save strategy: runtime fact cleared at boundaries or a small explicit future state with uninstall handling.
- Known incompatibilities: product expectation may reject the abstraction; companion mechanics may not align with cosmetic state.
- Licensing/assets: original rules and Kingmaker-native cosmetic only.
- Estimated complexity: low; confidence high in structural stability and low in player acceptance.

## Decision

No kill criterion fired, so the evidence-backed pivot path is unavailable. Architecture B remains the provisional product-fit leader, but Phase 2 is not authorized. The correct top-level status is `BLOCKED — CRITICAL`, not Proceed, Pivot, or Manual Visual Review: the mission's mandatory doorway-with-control, away/back selection, and party-formation rows remain `DEFER — EVIDENCED` under the exact Working fixture. The pair-only movement, lifecycle, visual, boundary, fixture, and restoration evidence cannot substitute for those three core rows or for runtime non-mounted-party isolation.

Unblocking requires a manually prepared Working fixture in the same qualified campaign/area with an eligible directly controllable non-pair unit and a combat-neutral Mammoth-valid doorway. Baseline remains immutable. Only after the exact fixture gate is requalified and those three rows pass their required fresh-process evidence can Architecture B receive an unconditional Phase 2 recommendation.
