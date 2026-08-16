# Targeting, reach, and charge contract

Status: IN PROGRESS

This contract is pair-scoped to the accepted Medium-humanoid/Mammoth profile. It does not alter global unit reach, weapon data, corpulence, target replacement, engagement, or pathfinding.

## Target and attack identity

The combat UI arms exactly one mode: `Rider melee` or `Mammoth primary`. The next ordinary left click on one valid visible enemy supplies exactly one target and clears the armed mode. `ClickUnitHandler.OnClick(GameObject,Vector3,int,bool,bool)` token `0x060093ED` is intercepted only when the exact mounted rider is the sole selected principal and a KMC mode is armed. Simulation, right-click, friendly, dead, untargetable, noncombat, non-pair, and unarmed clicks remain stock or fail closed as documented; KMC never silently converts a spell, ranged attack, or interaction.

The attack child is a native `UnitAttack` with `IsSingleAttack=true`. Rider mode uses the rider as `Executor`. Mammoth mode uses the exact active Mammoth as `Executor`; with its hands disabled, Kingmaker `UnitAttack.CreateSingleAttack` selects the first native additional limb, which is accepted only after runtime evidence identifies it as the Mammoth's primary natural weapon. `RuleAttackWithWeapon`, `RuleAttackRoll`, and `RuleDealDamage` event evidence must agree on initiator and target.

## Spatial origin and range

- Horizontal mechanics origin: the authoritative Mammoth entity/root position. Rider entity X/Z is continuously synchronized to the Mammoth-root attachment, but range is never inferred from the elevated visual seat Y.
- Rider stopping radius: exact active Mammoth corpulence + target corpulence + the rider planned melee weapon range.
- Mammoth stopping radius: stock Mammoth corpulence + target corpulence + its selected primary natural weapon range.
- Line of sight: native command LOS from the actual attack actor must be clear before the child starts.
- Range comparison: native mechanics-distance semantics and a fixed `0.05` world-unit numerical tolerance. A target outside the stopping radius causes one Mammoth path transaction or a clear rejection; it never starts an out-of-range attack.
- No global property, blueprint, weapon, unit, rulebook, or target position is mutated to manufacture reach.

One pair command owns at most one `UnitMoveTo`. It starts from the Mammoth, uses the chosen actor's stopping radius, has a fixed target snapshot, and is replaced only by the same transaction after a bounded target-displacement threshold. Maximum repaths and elapsed time are fixed; exhaustion interrupts without attack. Arrival stops the Mammoth before the child attack begins. A new order, stop/hold, dismount, target invalidation, turn end, death, mode boundary, or exception interrupts movement and attack together.

## Facing

During approach and immediately before attack, the Mammoth faces the target. The mounted synchronization adapter preserves the accepted upright rider yaw from the Mammoth root. The child attack retains native actor animation and hit FX. No new pose profile, saddle, reins, mount species, or global animator hook is introduced.

## Stretch disposition gate

Mounted reach is considered implemented only for the two explicit basic melee paths above and must pass inside/outside boundary tests plus non-mounted controls. It is not generalized to reach weapons, size changes, alternate riders, or alternate mounts.

Attacks of opportunity remain default-off for the mounted pair until core qualification. Kingmaker owns AoO counts and engagement independently per unit, so automatic rider/Mammoth participation could duplicate triggers. If exact runtime evidence cannot isolate one actor without broad engagement patches, status is `DEFER — EVIDENCED`.

Stock `AbilityCustomCharge` drives the caster's own movement agent, applies charge state to that caster, and constructs a caster-owned `UnitAttack`. Using it for the rider would reactivate or compete with the rider pathing seam; using it for the Mammoth would charge the wrong action/turn ledger. A basic charge is therefore permitted only if the already-qualified pair transaction can add a straight/path-valid charge flag and rider cost without a broad patch. Otherwise it is `DEFER — EVIDENCED` and default-off.

Ranged attacks and spellcasting while mounted are explicitly rejected. Non-mounted click, range, AoO, and charge behavior remains stock.
