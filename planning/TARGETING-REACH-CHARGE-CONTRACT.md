# Targeting, reach, and charge contract

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

Ranged attacks and spellcasting while mounted are explicitly rejected. Non-mounted click, range, AoO, and charge behavior remains stock.
