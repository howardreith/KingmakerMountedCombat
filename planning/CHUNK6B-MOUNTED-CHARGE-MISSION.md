# Chunk 6B — Mounted Charge mission (opened 2026-10-03)

Status: `OPENED — MEASURED; INCREMENT 6B.1 NEXT`. Nothing is implemented, enabled or claimed by this
document. It opens the bounded 6B mission the owner decision of 2026-10-02 (section F) names, on the
integration branch from the stabilized Chunk 6A head (exit record: `CHUNK 6A IMPLEMENTATION STABLE / FINAL
QUALIFICATION DEFERRED TO CHUNK 6 CONSOLIDATION`, docs commit b655a501). Main stays the accepted Chunk 5
delivery; the 87 Chunk 6A rows are not rerun per 6B candidate; final acceptance waits for the Chunk 6
consolidation.

## Governing contracts (unchanged by this document)

- `planning/CHUNK6-ACTION-CONTRACT.md` section 3: the native surface is `AbilityCustomCharge` (type
  `0x02000598`; `CanTarget` `0x06002BBD`, `Deliver` `0x06002BB6`, `RuntimeRoutine` `0x06002BB8`,
  `TurnBasesRoutine` `0x06002BB7`, `IsEngageUnit` `0x06002BB5`, `GetMinRangeMeters` `0x06002BBA`/`0x06002BBB`,
  `GetMaxRangeMeters` `0x06002BBC`, `Cleanup` `0x06002BB9`); Charge owns a forced path plus a queued native
  `UnitAttack` and stamps `RuleAttackWithWeapon.IsCharge` (`0x06007186`). The cost owner is the enclosing
  Standard/full-round ability shell. The 6A obligation holds until a pair charge is qualified: Charge is
  rejected at availability, targeting, click, admission and execution while mounted, before any movement or
  resource expenditure (`MountedChargeSafetyPolicy`/`MountedChargeSafetyService`), and ordinary unmounted
  Charge passes through untouched.
- `planning/TARGETING-REACH-CHARGE-CONTRACT.md`, "Basic mounted charge — deferred with evidence" and the
  stretch disposition gate: the stock primitive cannot separate mover from attacker; a basic charge is
  permitted only if the already-qualified pair transaction can add a straight/path-valid charge flag and the
  rider cost without a broad patch; otherwise the disposition is `DEFER — EVIDENCED` and the feature stays
  absent and default-off.
- The native engine owns every cost: a mounted charge may cost a resource only by being routed through the
  native command that already charges it; prediction and availability stay side-effect free; a transition
  that cannot be resolved lawfully is refused with an exact reason. Production pathing, adjacency and
  collision rules are never weakened.

## Problem statement (from section 3)

The mount must own the forced path and the queued attack while the rider owns the Standard action, and
`IsEngageUnit` must not strand the pair mid-path. The qualified pair attack transaction
(`MountedPairAttackCommand`: the rider-owned Standard attack delivered from the mount's position after a
mount-owned delegated approach, with exact admission, repath, cost, terminal and restoration evidence) is the
only candidate carrier. A charge differs from that transaction in four measurable ways: the approach must be
a straight, clear, path-valid line of at least the minimum and at most the maximum charge distance; the mover
runs at charge speed under the charge state; the single attack carries the charge rule flag and the native
charge modifiers; and the cost is the charge ability's own (full-round in turn-based mode, one native shell).

## Measurement record (2026-10-03; read-only over the pinned installed assembly)

Assembly-CSharp SHA256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID
`07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, read by reflection-only loading and decompilation into the lab
scratchpad (nothing in the installation was touched); the stock unmounted charge is the immutable
`c6a-repeated-request143-i-charge-tb` row `C4-CHARGE-unmounted-rider` (turn-based: charging observed,
7.21 m travelled, maximum rider Standard 6 and Move 3).

1. Consumers of the charge flag. `RuleAttackWithWeapon.IsCharge` is read in code only by
   `AdditionalDiceOnAttack.CheckCondition` (feat and weapon components: extra dice on a charge). The +2 attack
   and -2 armor class of a charge are not read from the flag: they are the stock buff
   `SystemMechanics.ChargeBuff` that `Deliver` adds to the caster for one round. `UnitAttack.IsCharge` is read
   by `TurnController.UpdateActionPredictions` (turn-based prediction), by `UnitAttack.InitAttacks` (a full
   attack only with Pounce while surprising in turn-based mode), by `TryStartNextAttack` and
   `ConfigureAnimations` (the first attack animates as a charge) and by `TriggerAttackRule`, which stamps the
   rule. `UnitState.IsCharging` is read by `UnitAnimationController.TickOnUnit` and the hands-equipment
   animation only; `UnitMovementAgent.IsCharging` selects `ChargingAvoidance` and suppresses `SlowDown`; both
   are set and cleared only by the custom charge, fly and overrun components. Therefore the charge flag on a
   rider-owned `UnitAttack` is honoured exactly as on any attack: it changes the rule stamp, the first-attack
   animation, the Pounce full-attack case and the extra-dice components, nothing else.
2. The cost in both modes. The cost is the enclosing `UnitUseAbility` shell: `AbilityData.RequireFullRoundAction`
   is `ActionType == Standard && Blueprint.IsFullRoundAction` (`BlueprintAbility.SetIsFullRoundAction` exists),
   `UnitUseAbility.Init` sets the three-second turn-based cast time for it, and `UnitCommand.IsFullRoundAbility`
   reports it to the turn controller. The queued `UnitAttack` carries `IgnoreCooldown()` so the already-charged
   Standard does not block it, and `IsFullAttackRestricted` returns false for an ignore-cooldown attack in
   turn-based combat. The measured stock turn-based charge charged the rider Standard 6 and Move 3: the
   full-round shell owns the cost; the forced path is not charged as a separate Move.
3. `CanTarget`. Caster-origin distance must lie within `GetMinRangeMeters` (turn-based: five-foot-step metres
   plus `GameConsts.MinWeaponRange` plus both corpulences; real-time: ten feet plus both corpulences) and
   `GetMaxRangeMeters` (`caster.CombatSpeedMps * 6`); `ObstacleAnalyzer.TraceAlongNavmesh(caster, target)`
   must reach the target; unless the caster's avoidance is disabled, every other awake unit with avoidance
   must stay farther than `0.8 * (casterCorpulence + otherCorpulence)` from the end point a weapon reach short
   of the target; in turn-based combat on the caster's own turn `CurrentTurn.TimeMoved` must be 0. Every check
   reads the caster's position, speed and corpulence; the pair variant must read the mount's.
4. `IsEngageUnit` and interruption. `IsEngageUnit` is read by `UnitUseAbility.IsInterruptible` (an engage-unit
   process is not interruptible), by `OnAction` (an engage-unit command returns `None` and keeps running while
   the delivery process runs) and by `OnTick` (the command force-finishes `Success` when the process ends;
   the command also interrupts itself when `Spell.CanTarget` turns false before it acted). The stock routines
   bound themselves: turn-based six seconds, a lost threat hand, `!State.CanMove`, a missing agent or an agent
   that does not move after a re-forced path; real-time the maximum distance, a lost threat hand, a navmesh
   obstacle on the straight line or `!attack.ShouldUnitApproach`; in every exit the attack is queued first
   (`AddToQueueFirst`) with `IgnoreCooldown` and `IsCharge` only when the approach completed, and `Cleanup`
   resets the charging state and speed. A pair variant must keep an equivalent bounded termination on the
   mount's path and never leave the rider's command running without an ending process.
5. The charging state and the forced path. `Deliver` sets `caster.View.AgentASP.IsCharging`, calls
   `AgentASP.ForcePath(new ForcedPath([casterPosition, targetPosition]), 1000000f)` (force mode, destination
   the target), adds the charge buff, sets `State.IsCharging`, creates `new UnitAttack(target)` and `Init`s it
   on the caster; the routines raise `MaxSpeedOverride` to twice `CombatSpeedMps`; `Cleanup` clears the agent
   charging flag, the speed override and the state flag. All of it acts on the caster's own agent. The mounted
   rider's agent is stopped, avoidance-leased and disabled under the mount's movement authority, so the stock
   component cannot be the carrier (the deferral evidence stands); the equivalent calls on the mount's agent
   are plain native API already used by the stock charge, fly and overrun components.

## Disposition

The stock `AbilityCustomCharge` remains unusable for the pair (it binds mover, attacker, buff, state and cost
to one caster). An original pair-owned charge is feasible without patching any stock charge method: a KMC
ability created by the existing blueprint factory (`CreateAbility`, as the Mount and Dismount controls are)
with `ActionType Standard`, `IsFullRoundAction`, enemy targets and a `MountedChargeAbilityLogic` component
(`AbilityCustomLogic`, `IAbilityAvailabilityProvider`, `IAbilityTargetChecker`, `IAbilityMinRangeProvider`,
`IsEngageUnit` true) whose `CanTarget` evaluates the five stock checks from the mount's position, speed and
corpulence and whose `Deliver` routes the charge through the native primitives on the right actors: the charge
buff and the charging state on the rider (the attacker), the charging flag, the forced straight path and the
doubled speed on the mount (the mover, under the pair's existing movement authority), bounded termination
identical to the stock routines, and at arrival one rider-owned `UnitAttack(target)` with `IgnoreCooldown` and
`IsCharge` queued first on the rider, delivered through the already-qualified bounded Mammoth-origin reach.
The native full-round shell charges the rider; nothing writes, clears or refunds a cooldown. The mission
therefore proceeds as `PROCEED — BOUNDED`, feature default-off behind an explicit setting until qualified, with
the 6A rejection of the stock Charge while mounted unchanged throughout.

Named risks to measure before any product delivery: the mount's forced path during the rider's turn must be
accounted by the unified mounted turn exactly like a delegated approach (no second Move charged to the mount,
no stranded force mode on interruption); the pair's reach and admission at arrival must be the qualified ones;
attacks of opportunity against the moving pair stay native and unpatched; save and load across a pending
charge must restore or lawfully drop it through the existing persistence machinery.

## Increment plan (each increment is its own frozen candidate with focused qualification)

- 6B.1 (next): a diagnostics-only measurement of the pair forced path under the existing diagnostic lease:
  the mount agent's `ForcePath` with doubled `MaxSpeedOverride` and `IsCharging` along a measured straight
  line in real-time and in turn-based mode, observing the unified-turn movement accounting, avoidance, stop
  behaviour, interruption and cleanup, with no ability, no product change and no cost; rows and an external
  reader for the measurements.
- 6B.2: the product ability (blueprint, availability with exact refusal reasons, mount-origin targeting) and
  the real-time delivery through the pair transaction; focused qualification: positive charge, minimum and
  maximum range refusals, straight-line obstruction, clearance refusal, target loss mid-path, cancellation,
  duplicate request, the stock Charge still rejected while mounted, unmounted Charge untouched.
- 6B.3: turn-based delivery (turn-start requirement, full-round cost, the single charge attack on the rider's
  turn, the mount's accounting) with the same refusal rows in turn-based mode.
- 6B.4: persistence and lifecycle (save during a pending charge, cold load, combat end, disable) on the Chunk 5
  machinery, then the 6B development-exit record.

## Development qualification shape (focused; not the 87 6A rows)

- FAST while implementing (component tests, source pins, the charge reader's synthetic acceptance and refusal),
  CANDIDATE once before a native freeze, FULL only at the Chunk 6 consolidation.
- One external PowerShell reader is the acceptance authority for the charge rows; the compiled scenario records
  facts and checks structure only.
- Each native candidate reruns only the charge rows it affects plus the Chunk 4 charge-safety regression
  (`chunk4-charge-safety-rt`/`-tb`), with fresh isolated restored transactions and session logs preserved.
- Historical failures are retained immutably; a commit, candidate, proof or targeted PASS is never
  authorization to merge main or publish a release.
