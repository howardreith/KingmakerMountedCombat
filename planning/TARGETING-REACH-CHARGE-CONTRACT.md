# Targeting, reach, and charge contract

## Bounded mounted reach qualified (2026-08-21)

Status: `PASS` within the exact supported profile and two explicit basic-melee paths. One clean package and suite produced rider RT/TB A/B, Mammoth RT/TB A/B, and same-package core controls at `658/0` total. Each combat target starts outside both independently recomputed Mammoth-origin stopping radii. Rider dispatch occurs inside the unchanged rider planned-weapon boundary; Mammoth dispatch occurs inside its larger stock natural-weapon boundary while the rider boundary remains false. All four rider/Mammoth/target attackability directions remain true, exact weapon/corpulence inputs remain immutable, and only the requested actor emits one attack chain. Controls preserve null/invalid target rejection, exact target-death interruption before child admission, cleanup, and non-mounted stock routing.

This qualification changes no blueprint, weapon, unit size, corpulence, target position, global reach, engagement, or non-mounted rule. It is not a claim for reach weapons, another rider body, another mount anatomy, explicit mounted attacks of opportunity, or charge. Explicit mounted AoO and basic charge remain `DEFER — EVIDENCED`, absent/default-off.

## Basic mounted charge — deferred with evidence (2026-08-21)

Status: `DEFER — EVIDENCED`; the feature remains absent and default-off.

The exact installed Kingmaker charge primitive cannot separate mover from attacker. `AbilityCustomCharge.Deliver` token `0x06002BB6` selects `context.Caster`, forces that caster's `AgentASP`, sets its charging/movement state, applies the stock charge buff, and creates a caster-owned `UnitAttack`. Its turn-based and real-time routines `0x06002BB7` / `0x06002BB8` change that same caster's speed, set `UnitAttack.IsCharge` through `0x06002663`, and queue it in that caster's command container. `Cleanup` `0x06002BB9` restores that caster's agent speed and charging state. `CanTarget` `0x06002BBD` evaluates caster-origin distance, `ObstacleAnalyzer.TraceAlongNavmesh` `0x060017AD`, surrounding-unit clearance, and current-turn movement.

Using the rider as caster would force-path and speed-modify the rider agent, violating Mammoth-only pathfinding and the existing avoidance/attachment lease. Using the Mammoth as caster would assign the charge buff, command, attack initiator, weapon, and RT/TB action ledger to the wrong actor. A custom pair charge would have to duplicate the stock straight-path, min/max range, clearance, double-speed, buff/state, full action-economy, interruption, and cleanup implementation rather than reuse a narrow installed seam. That exceeds the bounded stretch contract and introduces precisely the dual-owner risks Architecture B avoids. Production therefore references no `AbilityCustomCharge`, sets no charge state or rule flag, adds no charge action, and patches no stock charge method. Ordinary non-mounted charge remains stock. A focused future mission would need an original pair-owned charge transaction and full RT/TB, obstacle, cancellation, cost, duplicate, and restoration qualification.

## Explicit mounted attacks of opportunity — deferred with evidence (2026-08-21)

Status: `DEFER — EVIDENCED`; the feature remains absent and default-off. This does not change or weaken the separately qualified active-command opportunity-isolation repair.

The exact installed Kingmaker ownership chain is per unit, not per mounted pair. `UnitCombatState.Disengage` token `0x0600939B` synchronously calls `ShouldAttackOnDisengage(UnitEntityData,bool)` token `0x060093A2` and `AttackOfOpportunity(UnitEntityData,bool)` token `0x060093A1`. The latter evaluates that unit's `CanAttackOfOpportunity` token `0x06009390`, threat hand, memory, motion, conditions, and independent `AttackOfOpportunityCount`, then enqueues a separate free `UnitAttackOfOpportunity` constructor token `0x06002696`. Its `OnAction` token `0x06002699` creates a weapon rule and sets `RuleAttackWithWeapon.IsAttackOfOpportunity` through token `0x06007182`. Rider and Mammoth therefore retain separate engagement sets, counters, threat hands, motion state, commands, and rule initiators.

Architecture B synchronizes two Kingmaker entities while only the Mammoth owns movement. Selecting exactly one legitimate pair attacker for every enemy disengagement would require a new pair-wide engagement/counter arbitration layer or a broad `Engage`/`Disengage`/`ShouldAttackOnDisengage` patch. Allowing both stock states risks duplicate opportunities; synthesizing one bypasses the owning unit's complete stock trigger/counter path; redirecting it risks the wrong threat hand, command owner, counter, movement state, or RT/TB timing. Those approaches violate the narrow stretch gate. Production therefore creates no `UnitAttackOfOpportunity`, never writes `IsAttackOfOpportunity`, and patches none of those broad engagement methods. Idle mounted, non-mounted, and non-pair stock behavior remains unchanged. A future focused mission would need exact pair-wide trigger ownership, one-counter semantics, RT/TB tests, movement stability, cleanup, and non-mounted controls before enabling this feature.

## Bounded mounted-reach runtime proof checkpoint (2026-08-21)

Status: `IN PROGRESS`; implementation and deterministic validation are complete, but no new runtime credit is claimed yet.

The production seam remains unchanged and pair-scoped. Exact installed Kingmaker contracts are `UnitAttack.GetApproachRadius` token `0x06002685`, `AttackHandInfo.WeaponRange` field `0x04001A32`, `UnitCommand.IsUnitEnoughClose` getter `0x06002784`, `GeometryUtils.MechanicsDistance` token `0x06001C68`, and `UnitEntityData.CanAttack(UnitEntityData)` token `0x0600834E`. Rider melee alone uses Mammoth corpulence + target corpulence + the rider planned melee range from the Mammoth origin, followed by the already bounded native-rider admission bridge. Mammoth primary continues to use its stock native radius and executor. No blueprint, item, corpulence, target position, global reach, engagement, or non-mounted combat rule is modified.

Additive combat schemas v42/v43 now record both exact weapon identities, both derived weapon ranges, Mammoth/target corpulence, independent rider/Mammoth stopping radii, an initial position proven outside both radii, both native probe radii, actor-specific dispatch admission, four bidirectional pair-member targetability results, and exact input immutability. The strict validator recomputes both radius formulas, binds the action-specific radius, and rejects missing/malformed identity, initial inside-range state, probe mismatch, dispatch-distance mismatch, either targetability loss, any input mutation, or an actor outside its own boundary. Historical schemas remain unchanged. Fresh clean-package stationary rider RT/TB A/B and Mammoth-primary RT/TB A/B, plus the already isolated non-mounted control on that package, are required before reach becomes `PASS`.

## Opportunity-isolated targeting requalified on repaired launcher (2026-08-21)

RT A/B `55/0` each and TB A/B `59/0` each pass on clean implementation `4d1737c0ce24502bb3ec7c383aa008dad14a642e`, package `5793c05dffb69bd1417bfd6ce9f239f34be788673092da526fff1df4ffc3e1dd`, and one exact suite snapshot `16315b6f15e8c39fb4046578efedbb5c24dd31307c069aa65be0f42a1bdb0446`. Each target starts outside the unchanged mounted pair radius, remains stationary, and is attacked only after the Mammoth-owned approach satisfies the exact range gate. Each row proves exact target and initiator identity, one melee weapon attack/roll/damage chain, zero incidental `UnitAttackOfOpportunity`, zero duplicates/repath, and exact cleanup/restoration. This closes the repaired-package movement-to-attack claim only; it does not qualify broader mounted reach, explicit mounted attacks of opportunity, or charge.

## Opportunity-isolation TB launcher failure is not feature evidence (2026-08-21)

Although failed launcher attempt `20260821T151200Z-opportunity-isolation-tb-passA` later emitted game `PASS 59/0`, it is uncredited because exact final restoration required recovery. Its process-metadata repair does not change targeting, range, reach, opportunity, or charge behavior. Fresh RT/TB A/B on the repaired package remains mandatory; explicit mounted AoO remains separately unqualified.

## Core target and cleanup controls qualified (2026-08-21)

Fresh same-suite A/B processes each pass exact core controls `80/0`, including invalid-target rejection, exact target identity, target death before child admission, and zero attack/roll/damage/AoO/charge chain. Combined credit is `160/0` with exact restoration. This does not qualify mounted reach, movement-to-attack opportunity isolation, explicit mounted AoO, or charge; those boundaries remain separate.

## Core target controls pass in game; final orchestration remains uncredited (2026-08-21)

`20260821T120800Z-core-combat-controls-finalization-passA` proves exact target controls at game `80/0`, including exact pre-child target-death interruption, unique target identities, and zero attack/roll/damage-rule/AoO/charge chain. It is uncredited solely because the launcher did not produce a valid restored final result. The follow-up changes only launcher error observation and fallback evidence construction. No target, range, reach, opportunity, or charge behavior changes; fresh A/B remains required.

## Target-admission repair observed healthy in interrupted process (2026-08-21)

Uncredited `20260821T105600Z-core-combat-controls-admission-passA` passes target-death `24/0`: exact target admission, lethal public damage, target life transition, exact pre-child Interrupt, retained mounted pair, and zero child/attack/roll/damage-rule/AoO/resource chain. The remaining cleanup-row diagnostic and stale artifact manifest do not contradict targeting behavior. Their repairs change only active-command outcome observation and final evidence publication. No target identity, range, reach, engagement, opportunity, or charge rule changes; fresh A/B is still mandatory.

## Exact target-admission observation boundary (2026-08-21)

Repaired process `20260821T102109Z-core-combat-controls-repair-passA` is immutable uncredited `FAIL 77/1` with audit-before-read. It proves exact target death and zero child/rule/resource/AoO behavior, but the game log shows death was injected after wrapper submission and before `OnStart` accepted the target. The diagnostic now observes only the exact command's transaction identity and waits for exact target admission in `Approaching` or `Attacking`, still before any child start, before applying the existing control mutation. The production exact-target pre-child cancellation remains narrow and unchanged. This adds no range, reach, target replacement, engagement, opportunity, or charge behavior; fresh A/B is required.

## Exact pre-child target invalidation repair (2026-08-21)

The first control-suite process is immutable uncredited `FAIL 76/2` with an exact audit-before-read. Its target-death row proves the exact disposable target became dead after public lethal `Damage`, the valid pair remained mounted, no child/rule/resource chain started, and all state restored. Source reconciliation shows generic `RequireLiveExactPair` faulted the transaction before Interrupt could retain target-invalidation semantics. The repair checks only the already accepted exact target before child start, cancels with `target invalidated before child attack`, and then interrupts; wrong target, post-child target state, rider/Mammoth liveness, hostility outside the exact command, and unrelated commands retain existing behavior. This changes no range, reach, engagement, opportunity, or charge contract. Fresh A/B remains required.

## Core target-isolation controls (2026-08-21)

The additive `combat-core-control-suite` places its disposable hostile target at the existing diagnostic approach distance outside the measured Mammoth-origin pair radius. Null rider/Mammoth target clicks must fail closed through the production click path; public lethal target `Damage` must interrupt an already accepted out-of-range rider command before child admission while preserving the valid pair; repeated exception cleanup must terminate that command without a rule chain; and an unmounted pair must return `NotHandled` so stock click routing remains authoritative. All four rows require unique exact target identity, zero attack/roll/damage/AoO/charge evidence, unchanged resources, and complete target/relationship restoration. This adds no reach, opportunity, engagement, or charge behavior and remains runtime-unqualified until fresh A/B.

## External-state evidence binding (2026-08-20)

Targeting, reach, opportunity, and charge evidence must bind one exact package-bound qualification-suite snapshot. Foreign save or Mods drift cannot be hidden by a later admission or combined across snapshots; a changed stable between-suite state begins a new suite and a fresh A/B set. This safety change does not grant mounted-reach, explicit mounted-AoO, or charge qualification and does not alter the narrow opportunity-isolation claim under test.

Status: IN PROGRESS

Native incapacitation qualification (2026-08-21) changes no targeting, range, opportunity, or charge rule. The exact rider and Mammoth native-unconsciousness A/B rows clean up before any further mounted command can be admitted and prove zero synthesized attack behavior. Reach, explicit mounted AoO, and charge remain separately unqualified stretch contracts.

Command-termination boundary (2026-08-21): fresh same-suite cancellation/interruption RT/TB A/B proves that terminating the exact active out-of-range rider command before child admission emits zero attack, roll, damage, or `UnitAttackOfOpportunity` chains and restores the exact wrapper/delegated movement state. This is command/action-economy qualification only. It does not broaden mounted reach, synthesize or qualify explicit mounted AoO behavior, grant charge semantics, or alter any target/range threshold.

The narrow opportunity-isolation claim is now qualified for the exact active rider movement-to-attack command. Clean commit/package `632e3710ea1be2d7331eea7f07cf803295b1ad1f` / `0cca1e30181e6021426c4bfdb8906178636db790a3e6fff4c3aff95ab46f6827` produced fresh RT A/B schema v34 `55/0` and TB A/B schema v35 `59/0` under one exact suite snapshot. Every row records one intended non-AoO rider attack chain and zero unexpected pair attacks. Idle mounted, non-mounted, non-pair, unrelated/null-target, and broad engagement behavior remain stock. This result does not qualify explicit mounted attacks of opportunity, generalized reach, or charge.

This contract is pair-scoped to the accepted Medium-humanoid/Mammoth profile. It does not alter global unit reach, weapon data, corpulence, target replacement, engagement, or pathfinding.

## Target and attack identity

The combat UI arms exactly one mode: `Rider melee` or `Mammoth primary`. The next ordinary left click on one valid visible enemy supplies exactly one target and clears the armed mode. `ClickUnitHandler.OnClick(GameObject,Vector3,int,bool,bool)` token `0x060093ED` is intercepted only when the exact mounted rider is the sole selected principal and a KMC mode is armed. Simulation, right-click, friendly, dead, untargetable, noncombat, non-pair, and unarmed clicks remain stock or fail closed as documented; KMC never silently converts a spell, ranged attack, or interaction.

The attack child is a native `UnitAttack` with `IsSingleAttack=true`. Rider mode uses the rider as `Executor`. Mammoth mode uses the exact active Mammoth as `Executor`; Kingmaker `UnitAttack.CreateSingleAttack` calculates hand attack counts, prefers the eligible primary hand, then the eligible secondary hand, and only then falls back to the first weapon-bearing additional limb. The supported Mammoth profile requires the exact natural-melee primary-hand weapon selected by that native order. A stock-initialized explicit primary item and the exact empty-hand fallback are classified separately, and the click-time selection must remain identical when the native child initializes. `RuleAttackWithWeapon`, `RuleAttackRoll`, and `RuleDealDamage` event evidence must agree on initiator and target.

## Spatial origin and range

- Horizontal mechanics origin: the authoritative Mammoth entity/root position. Rider entity X/Z is continuously synchronized to the Mammoth-root attachment, but range is never inferred from the elevated visual seat Y.
- Rider stopping radius: exact active Mammoth corpulence + target corpulence + the rider planned melee weapon range.
- Mammoth stopping radius: stock Mammoth corpulence + target corpulence + its selected primary natural weapon range.
- Line of sight: native command LOS from the actual attack actor must be clear before the child starts.
- Range comparison: native mechanics-distance semantics and a fixed `0.05` world-unit numerical tolerance. A target outside the stopping radius causes one Mammoth path transaction or a clear rejection; it never starts an out-of-range attack.
- No global property, blueprint, weapon, unit, rulebook, or target position is mutated to manufacture reach.

The stationary diagnostic row places its transient target near the measured runtime boundary, not at a guessed absolute distance: requested horizontal distance is exact pair approach radius minus `0.12`, navmesh projection may differ by at most `0.06`, and final separation must remain above the independent `0.05` numerical tolerance. This admits Probe F's exact native `2.37020588` radius while still rejecting zero-distance, excessive projection, and out-of-range evidence. It changes no gameplay reach threshold.

One pair command owns at most one `UnitMoveTo`. It starts from the Mammoth, uses the chosen actor's stopping radius, has a fixed target snapshot, and is replaced only by the same transaction after a bounded target-displacement threshold. Maximum repaths and elapsed time are fixed; exhaustion interrupts without attack. Arrival stops the Mammoth before the child attack begins. A new order, stop/hold, dismount, target invalidation, turn end, death, mode boundary, or exception interrupts movement and attack together.

## Facing

During approach and immediately before attack, the Mammoth faces the target. The mounted synchronization adapter preserves the accepted upright rider yaw from the Mammoth root. The child attack retains native actor animation and hit FX. No new pose profile, saddle, reins, mount species, or global animator hook is introduced.

## Stretch disposition gate

Mounted reach is considered implemented only for the two explicit basic melee paths above and must pass inside/outside boundary tests plus non-mounted controls. It is not generalized to reach weapons, size changes, alternate riders, or alternate mounts.

Attacks of opportunity remain default-off for the mounted pair until core qualification. Kingmaker owns AoO counts and engagement independently per unit, so automatic rider/Mammoth participation could duplicate triggers. If exact runtime evidence cannot isolate one actor without broad engagement patches, status is `DEFER — EVIDENCED`.

Stock `AbilityCustomCharge` drives the caster's own movement agent, applies charge state to that caster, and constructs a caster-owned `UnitAttack`. Using it for the rider would reactivate or compete with the rider pathing seam; using it for the Mammoth would charge the wrong action/turn ledger. A basic charge is therefore permitted only if the already-qualified pair transaction can add a straight/path-valid charge flag and rider cost without a broad patch. Otherwise it is `DEFER — EVIDENCED` and default-off.

Mounted ranged attacks are explicitly rejected. General mounted spellcasting remains unqualified rather than globally banned: non-body-changing stock spellcasts may remain incidental when they preserve all mounted invariants, while a body/view-changing spell cleanly dismounts through the replacement-view boundary. Non-mounted click, range, AoO, charge, ranged, and spell behavior remains stock.
# Private-alpha stabilization addendum — 2026-08-21

Supported rider melee continues to use the qualified bounded Mammoth-origin contract and the child-only native rider admission bridge. Stabilization does not increase reach, timeout, repath, or duplicate thresholds. Ordinary player input must expose separate rejection/terminal diagnoses for wrong selection/actor/turn/action state, relationship or body-profile invalidation, missing or ranged weapon, invalid/non-hostile/invisible target, active command, no path, outside supported approach/range, range-origin mismatch, and native command admission failure. Diagnostic rows must enter through the same armed-overlay plus exact `ClickUnitHandler.OnClick` path used by a player, including proof that the one-shot overlay activation guard consumes only the propagated activation click while retaining the armed action.

Mounted ranged combat remains unsupported. The pair-local stock-command guard rejects an exact mounted rider `UnitAttack` outside KMC's armed melee transaction, with explicit ranged feedback when the selected native single-attack weapon is ranged. It does not patch attack rules, weapons, range, or any unmounted/non-rider command.
