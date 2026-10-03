# Chunk 6B — Mounted Charge mission (opened 2026-10-03)

Status: `INCREMENT 6B.2 PARTLY QUALIFIED NATIVELY ON PREVIEW.158 (3 of 5 charge rows); DELIVERY CORRECTIONS
IMPLEMENTED (0.1.0-chunk6b-preview.159) - CANDIDATE TIER, FREEZE AND NATIVE QUALIFICATION PENDING`. No product feature is implemented, enabled or claimed by this document (increment 6B.1 is a
diagnostics-only measurement; its record is below). It opens the bounded 6B mission the owner decision of 2026-10-02 (section F) names, on the
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

- 6B.1 (implemented as preview.156; native measurement pending): a diagnostics-only measurement of the pair
  forced path under the existing diagnostic lease:
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

## Increment 6B.1 - implementation record (2026-10-03; preview.156, frozen at commit 13b86ac0)

Diagnostics only; no product behavior changed; the feature stays absent and default-off; the stock Charge stays
rejected while mounted (Chunk 4 protocol, 472/0 in this candidate's FAST tier).

- Scenarios `chunk6b-charge-path-rt` and `chunk6b-charge-path-tb` (compiled:
  `src/KingmakerMountedCombat/Diagnostics/Chunk6bChargePathScenario.cs`, a partial of the Phase 3D horse tranche;
  evidence schema 33; observation contract `chunk6b-pair-forced-path-measurement`). Both are mounted-handoff
  tranche children exactly like the Chunk 4 charge children: the parent performs the exploration preamble Mount
  with the idle disposable party, the child enters combat against a fresh target placed on a straight clear
  line 9 m from the mount, and in turn-based mode it waits for the rider's natural turn and enters Acting
  through a native five-foot step (no Move action).
- Rows `C6B-PATH-straight-arrival` and `C6B-PATH-interrupt-stop`. Each row applies, on the mount agent only,
  the three calls the stock charge makes on its caster's agent (`IsCharging`, `MaxSpeedOverride` raised to
  twice the mount combat speed, `ForcePath(new ForcedPath([mount, target]), 1000000f)`), samples the path
  every 0.1 s (distance, force mode, speed, both command containers, the unified-turn movement accounting, the
  rider turn's `TimeMoved` and `TimeMovedInForceMode`), stops on arrival within the pair reach (first row)
  or interrupts after 1.5 m or 0.4 s through the stock `StopMoving` (second row), restores every leased value
  exactly and records costs, residue and attack rules.
- Acceptance authority: `scripts/runtime/Chunk6bChargePathEvidence.ps1`, dispatched from the shared horse
  evidence reader for schema 33. It refuses any non-straight or blocked geometry, a distance outside the stock
  minimum/maximum range read from the mount, a maximum range other than six times the mount combat speed, a
  lease that touched the rider agent, any command or lost charging flag during the path, any cooldown delta on
  either actor, missing restoration, force-mode/movement/charging residue, an attack rule, lateral deviation
  above 0.75 m, and in turn-based mode any rider movement before the path other than the five-foot step, no
  forced time, or movement outside force mode. Synthetic acceptance and refusal:
  `scripts/Test-Chunk6bChargePath.ps1`, 80/0.
- Harness registration: the launcher allow-list, the request validator, the save-backed, registry and audit
  lists and the horse-evidence suite list of `RuntimeHarness.Common.ps1`, the schema 33 consistency rule, the
  child-entry preamble families (mounted handoff, idle party required, preamble required for
  `0.1.0-chunk6b-preview.N`) and the Chunk 6A identity requirement extended to the `chunk6b` version line.
- Source-inventory pin changed with intent: the harness test "basic mounted charge feature remains absent and
  default-off" still forbids `new AbilityCustomCharge`, `IsCharge = true` and `ChargeBuff` everywhere and
  `IsCharging = true` in every file except exactly one diagnostics file, which it pins to raise the mount
  agent's charging flag once under its restored lease and to manufacture no charge state or attack.
- Controller accessor `LastPairedMovementObservation` (read-only) for the samples; version
  `0.1.0-chunk6b-preview.156`.
- FAST tier (lab receipts `analysis-cache/chunk6b-charge/fast-tier-preview156-{1,2}-receipt.json`): attempt 1
  failed on the two source pins above (the schema pin and the charge-absent guard; no build or test defect),
  attempt 2 14/0 after the pins were updated; component tests 556/0.

## Increment 6B.1 - native measurement outcome (2026-10-03; frozen preview.156)

Frozen preview.156 (commit 13b86ac0, package e7494561, suite `20261003-chunk6b-charge-path-a` / ed6eacec, purity PASS
`c6b-path156-a-whatif`); CANDIDATE tier 25/0 before the freeze. Lab record
`analysis-cache/chunk6b-charge/measurement-preview156.json` (fef66ea0) and outcome
`chunk6b-increment-6b1-outcome.json` (715d4bd4): 2 PASS / 2 FAIL of 4 stages, every transaction a fresh isolated
restored transaction with session logs preserved; nothing merges to main.

- CHARGE-SAFETY-RT and CHARGE-SAFETY-TB (`c6b-path156-a-charge-safety-rt`/`-tb`): PASS 66/0 each. The Chunk 4
  rejection of the stock Charge while mounted and the untouched unmounted Charge are unchanged on this candidate.
- C6B-PATH-RT (`c6b-path156-a-charge-path-rt`): native rows 62/0, external reader FAIL ("row
  C6B-PATH-straight-arrival did not restore the lease exactly"); retained. Measured: after the three stock calls on
  the mount agent the agent entered force mode and moved for exactly one movement tick (0.112 m at 10.17 m/s), then
  stopped (stalled after 1.0 s at 8.888 m; the interrupt row likewise 0.120 m), with both command containers empty
  throughout, no cooldown delta on either actor, charging flag and speed override restored exactly, and force mode
  still set after the stop. Cause, established read-only from the decompiled engine: `UnitActionController.TickOnUnit`
  stops every unit whose command container is empty on every tick (`unit.Commands.Empty` then `View.StopMoving()`);
  the stock charge survives because its caster holds the running engage-unit `UnitUseAbility` for the whole path. A
  forced path on the mount therefore lives only while the mount holds a live command. Second fact:
  `UnitMovementAgent.Stop()` does not clear `m_IsInForceMode`; only the next `OnPathComplete` does, and the stock
  charge leaves the same latch after its `Cleanup`. The row's "force mode cleared after the stop" expectation was
  therefore not an attainable restoration; the reader refused correctly under that rule, and the rule is recalibrated
  below to the measured semantics (latched after the stop, proven cleared by the next lawful path).
- C6B-PATH-TB (`c6b-path156-a-charge-path-tb`): FAIL 60/2, native exception "Native ground input admitted no exact
  player command for <rider>"; retained. The five-foot-step entry looked for the admitted command on the rider, while
  the mounted pair's rider-turn ground movement is admitted as a mount-executed `UnitMoveTo` (session log: "Mounted
  ground movement accepted ... executorId=<mount>; turnStatus=Preparing"). A fixture defect of the measurement scenario;
  no forced path was measured in turn-based mode.

Both causes are established and fixture-side (the diagnostics scenario and its reader); no product behavior is
implicated and none changes. Design consequence for 6B.2/6B.3, now measured rather than assumed: the pair charge's
mover must hold a live command of its own for the whole forced path (the mount-executed carrier the pair transaction
already uses for its delegated approach is the candidate), while the rider's full-round shell owns the cost.

Corrected measurement (increment 6B.1 continued, next candidate): the forced path runs under the pair's own admitted
delegated ground move on the mount as its carrier (a native `UnitMoveTo` created by a native ground click, mount
executor, created by the player: the pathway qualified in Chunk 6A); the three charge calls are applied on the mount
agent while that command is alive and the forced path is re-applied whenever the agent leaves force mode, exactly as
the stock runtime routine re-forces it; the interrupt row interrupts the carrier natively; every leased value is
restored; the latched force mode is recorded and then proven cleared by the next lawful pair path (a residue probe);
and the turn-based entry accepts the admitted five-foot-step command on the mount.

## Increment 6B.1 - corrected measurement (2026-10-03; preview.157, FAST-verified, not yet frozen)

Diagnostics only; no product behavior changed. `src/KingmakerMountedCombat/Diagnostics/Chunk6bChargePathScenario.cs`
now measures the forced path under the pair's own admitted delegated ground move: a native ground click toward a
point one pair reach short of the target creates the mount-executed `UnitMoveTo` carrier (read from the mount
first, the rider otherwise; the KMC feedback and rejection codes are recorded), the three stock calls are applied
on the mount agent while that command lives, the straight forced path to the target position is re-applied
whenever the agent leaves force mode (every application recorded), the row ends the carrier natively
(`Interrupt`) at arrival or at the interrupt point, the leased values are restored exactly, the force mode after
the stop is recorded, and a residue probe (the next native pair ground order, 1.5 m back along the line) proves
the latch cleared at its first movement. The turn-based five-foot-step entry uses the same helper. Costs are
captured before the probe and again after it.

`scripts/runtime/Chunk6bChargePathEvidence.ps1` (the acceptance authority) now requires the mount-executed admitted
carrier created by a native ground click that moved and was ended by the measurement, force mode on the first
forced path, the forced-path applications recorded exactly, no command beside the carrier, exact restoration with
the carrier ended and the mount container empty, the recorded force mode after the stop, an admitted mount-executed
residue probe whose first movement shows force mode cleared and that moved the pair, zero cooldown deltas (in
turn-based mode at most one native frame, 0.05 s, of rider Move before the forced path), and the turn-based rules
on the rider's Acting turn. Synthetic acceptance and refusal: `scripts/Test-Chunk6bChargePath.ps1`, 125/0.

Offline verification of this change set: FAST 14/0 (lab receipt `fast-tier-preview157-1-receipt.json`), component
tests 556/0. Pending, in order: the one CANDIDATE tier, freeze (package qualifier `chunk6b-charge-path-b`, suite
`20261003-chunk6b-charge-path-b` pinned to the preview.156 suite), the purity proof, then C6B-PATH-RT, C6B-PATH-TB,
CHARGE-SAFETY-RT and CHARGE-SAFETY-TB as fresh isolated restored transactions, the offline binding and the
measurement record. Nothing merges to main; no release, tag or HUMAN PLAY claim.

## Increment 6B.1 - native measurement outcome (2026-10-03; frozen preview.157)

Frozen preview.157 (commit e48d9268, committed tree 39188fce, package bba1712c, suite
`20261003-chunk6b-charge-path-b` / 2b31abd8, DLL 2d39aeec, purity PASS `c6b-path157-a-whatif`); CANDIDATE tier
25/0 at the candidate commit itself on a clean worktree. Lab record `measurement-preview157.json` (b49583d5),
outcome `chunk6b-increment-6b1-outcome-preview157.json` (c7c992c1): 2 PASS / 2 FAIL of 4 stages, four fresh
isolated restored transactions, session logs preserved.

- CHARGE-SAFETY-RT and CHARGE-SAFETY-TB: PASS 66/0 each, unchanged.
- **C6B-PATH-RT established the carrier.** Native rows 62/0. The mount ran 6.825 m in 0.791 s at a peak of
  10.324 m/s against a 5.080 m/s mount combat speed (the doubled override was 10.160), arrived 2.111 m from the
  target inside the 2.210 m pair reach, deviated 0.000 m from the straight line, charged no cooldown on either
  actor, restored the charging flag and speed override exactly, and left force mode latched after the stop as
  predicted. The interrupt row stopped after 1.552 m and settled in 0.012 s. The admitted delegated ground move
  on the mount is therefore a sufficient carrier for a forced charge path, which is what 6B.1 existed to decide.
  The external reader refused the artifact on one rule only: one attack rule initiated by the pair was observed
  on arrival, and the artifact did not publish the opportunity split, so it could not distinguish the engine's
  own attack of opportunity (lawful, to be observed) from an attack delivered by the measurement (forbidden).
- **C6B-PATH-TB moved past its preview.156 cause to a product refusal that was correct.** The five-foot-step
  entry now reads the mount-executed command, and the run then failed at the carrier with KMC's own exact
  reason: "The mount has no movement available in this paired activation." (`WrongActionState`), from
  `PairedActivationLifecycle.CanMovePairedMount` through `HasGrantedMovement`. The rider's five-foot-step entry
  had already spent the pair's one granted movement for that activation. Correct product behaviour.

Both causes were fixture-side and are corrected on preview.158: the measurement now publishes the compiled
attack-rule evidence with the opportunity split and the reader requires zero *non-opportunity* pair attacks
while recording opportunity ones; and the turn-based row issues the delegated carrier as the rider turn's first
movement from Preparing, with no five-foot-step entry, which is also the stock charge precondition
(`CurrentTurn.TimeMoved == 0`). No product behaviour changed for either.

## Increment 6B.2 - product implementation (2026-10-03; preview.158, FAST-verified, not yet frozen)

The pair-owned Mounted Charge exists, default-off, and the stock Charge is untouched. The whole feature is one
policy, one lease, one ability component, one controller partial, one command mode and one setting.

- `Domain/MountedChargePolicy.cs`: the pure availability and targeting decision. It carries the five stock
  checks read from the **mount**, because the mount is the mover: distance within the stock minimum (turn-based
  five-foot-step metres plus `GameConsts.MinWeaponRange` plus both corpulences; real-time ten feet plus both
  corpulences) and the stock maximum (`mount.CombatSpeedMps * 6`), a straight navmesh route, the stock
  clearance check one rider weapon reach short of the target (the carried rider is never an obstacle to its own
  mount), plus pair, turn, action-budget, weapon and target preconditions. Every refusal names one existing
  rejection code and one exact reason; no new enum values. Eight component tests, 564/0 overall.
- `Integration/MountedChargeAbilityLogic.cs`: the component of the new ability
  (`AbilityCustomLogic, IAbilityTargetChecker, IAbilityMinRangeProvider, IAbilityAvailabilityProvider,
  IAbilityVisibilityProvider`). `IsEngageUnit` is deliberately false: the stock charge needs an engage-unit
  process because one caster both moves and attacks, whereas here the mover and the attacker are two separate
  native commands and the already-qualified pair transaction owns the bounded path and termination, so a second
  long-lived process would only add a lifetime that could strand.
- `NativeMountedControlService`: a fifth original KMC ability, `KMC_MountedChargeAbility`
  (asset id `d79eaec224a7a832e738eb81baef9d49`, the md5 of its object name), `ActionType Standard` with
  `SetIsFullRoundAction(true)`, enemy target, equipped weapons required, `AbilityRange.Unlimited` so the
  native shell never asks the rider to approach while `CanTarget` stays the authoritative mount-origin gate.
  Availability, targeting and delivery are three one-line branches delegating to the controller, matching the
  four existing controls; the control is leased only while `EnableMountedCharge` is on.
- `Integration/MountedCombatController.Charge.cs`: the admission surface. The charge is admitted as the
  ordinary rider-melee pair transaction carrying a charge lease, so no new action kind, evaluator branch or
  admission path is introduced. The command is queued first on the rider with `IgnoreCooldown()` rather than
  run, exactly as `AbilityCustomCharge` queues its own attack, because the full-round shell has already paid
  through `Spell.Spend` and `Commands.Run` would interrupt the shell that is paying.
- `Integration/MountedChargeLease.cs`: the charge mechanics, in one removable place. On the **mount** agent,
  the three calls the stock charge makes on its caster's agent (`IsCharging`, `MaxSpeedOverride` at twice the
  mount combat speed, `ForcePath` on the straight line to the target, re-forced whenever force mode drops while
  the carrier lives). On the **rider**, the stock charge buff for one round and the native charging state.
  Restoration puts back exactly what the lease set, records the force-mode latch instead of pretending to clear
  it, and never writes or refunds a cooldown.
- `MountedPairAttackCommand`: a `chargeMode` flag and four call sites - apply the lease on the admitted
  carrier, maintain it on each driven approach tick, stamp the native `IsCharge` on this command before the
  engine evaluates the attack plan, and restore the lease on every terminal path.
- `DiagnosticSettings.EnableMountedCharge`, default false.

Diagnostics: `chunk6b-charge-rt` (schema 34, contract `chunk6b-pair-charge-delivery`) drives the player-facing
path only, through the game's own selected-ability handler, and records what the engine did. Five rows:
`C6B-CHARGE-default-off` (absent while the setting is off, leased once it is on), `C6B-CHARGE-positive`,
`C6B-CHARGE-below-minimum`, `C6B-CHARGE-spent-standard` (the repeated-request family) and
`C6B-CHARGE-stock-rejected`. `scripts/runtime/Chunk6bChargeEvidence.ps1` is the one acceptance authority;
`scripts/Test-Chunk6bCharge.ps1` is its synthetic acceptance and refusal regression, 66/0.

Named fixture limitation, recorded rather than claimed: the authorized diagnostic spawn envelope is 3 to 20 m
while the stock maximum charge distance for this mount is 30.48 m, so a **native** beyond-maximum refusal is not
reachable in this fixture. The artifact records `beyondMaximumReachable = false` with the envelope, and the
maximum-range refusal is covered by the pure policy and its component tests only. Obstruction, clearance,
target loss, cancellation, post-commitment interruption, combat end and incapacity are not in this first
delivery pack either; they are the next 6B.2 increment, and 6B.2 is not complete until they pass.

Source-inventory guard changed with intent: the Chunk 4 "charge absent" pin becomes a "charge confined and
default-off" pin. It still forbids constructing, patching or replacing the stock charge and still pins the
Chunk 4 mounted stock-Charge rejection; it now additionally pins that the native charge flag is stamped in
exactly one place under charge mode, that the charge buff and charging state are written only in the lease and
its measurement, that the lease restores exactly and writes no cooldown, and that the setting is never
initialized true and gates the leasing.

Offline verification of this change set: FAST 15/0 (lab receipt `fast-tier-preview158-1-receipt.json`),
component tests 564/0. Pending, in order: the one CANDIDATE tier, freeze (package qualifier
`chunk6b-charge-c`, suite `20261003-chunk6b-charge-c` pinned to the preview.157 suite), the purity proof, then
C6B-CHARGE-RT, C6B-PATH-RT, C6B-PATH-TB, CHARGE-SAFETY-RT and CHARGE-SAFETY-TB as fresh isolated restored
transactions. Nothing merges to main; no release, tag or HUMAN PLAY claim; 6B.2 is not qualified until its
remaining refusal and interruption rows pass.

## Preview.158 native outcome (2026-10-03; frozen preview.158, campaign closed)

Frozen preview.158 (commit 90678e81, committed tree ef36736d, package 03a5bf9d, suite
`20261003-chunk6b-charge-c` / f39e59e5, DLL 83b97011, purity PASS `c6b-charge158-a-whatif`); CANDIDATE 26/0,
component tests 564/0. Lab record `measurement-preview158.json`, outcome
`chunk6b-increment-6b2-outcome-preview158.json` (0eec3dab): 2 PASS / 3 FAIL of 5 stages, five fresh isolated
restored transactions, session logs preserved.

- CHARGE-SAFETY-RT and CHARGE-SAFETY-TB: PASS 66/0 each. The stock Charge stays rejected while mounted and the
  unmounted Charge is untouched **with the new charge feature present in the build**, which is the regression
  that mattered most for this candidate.
- **C6B-CHARGE-RT: the charge fixture ran all five rows and three of them pass the external reader.** Native
  rows 65/0. Offline, per row: `C6B-CHARGE-default-off` PASS (the ability is absent while
  `EnableMountedCharge` is off and leased on the rider once it is on, with the exact asset id, Standard
  full-round action, the KMC component and never the stock one, and the stock Charge still present and still
  stock); `C6B-CHARGE-below-minimum` PASS (a target 3.5 m away, inside the measured 4.448 m stock minimum,
  was refused with no shell, no movement, no cost and no attack); `C6B-CHARGE-stock-rejected` PASS.
  `C6B-CHARGE-positive` FAIL and `C6B-CHARGE-spent-standard` FAIL, both from one fixture timing cause: the
  fixture clicked while the rider was still waiting initiative, so `UnitCombatState.CanActInCombat`
  (`m_InCombat && !IsWaitingInitiative`) was false and the charge was correctly refused by the mod's own
  policy with "Mounted Charge requires a rider who can act." The repeated-request row then had no spent
  standard action to test. **No product defect: the refusal was exact and correct for the state the fixture
  presented.**
- **C6B-PATH-RT: the corrected carrier measurement is now clean except one reader threshold.** Arrival at
  6.817 m with a peak of 10.54 m/s against 5.080 m/s, 70 re-forces of the straight line, zero pair attack
  rules of either kind, the force-mode latch cleared by the next lawful pair path, exact restoration. The only
  refusal was my own "fewer than three samples" rule applied to the interrupt row, which is designed to stop
  within 0.4 s and can only yield two samples.
- **C6B-PATH-TB established how turn-based pair movement is accounted.** The carrier now admits from Preparing
  and the mount runs the forced path (6.816 m arrival, 1.586 m interrupt), but `TurnController.TimeMoved` and
  `TimeMovedInForceMode` stay zero while the samples' `pairMovement` observation advances
  (`mountTime=0.011…`). KMC's movement override replaces the stock turn-based movement gate for the mount, so
  the pair's movement is debited to the paired movement state and its per-activation grant, not to the turn
  counters. That is the Chunk 6A-qualified paired model, not a defect, and the preview.157 refusal of a second
  move in the same activation is the same grant being enforced. My turn-based reader rules had been written
  from the raw-forced-path model and are recalibrated to the measured accounting.

Lab correction recorded, not a re-verdict: `Qualify-Measurement158.ps1` was derived from the preview.157
qualifier, which knows only two stage kinds, so it re-read the new delivery stage offline with the Chunk 4
charge reader and recorded that reader's message. The verdict is unaffected (the launcher's in-transaction
validation used the correct reader, preserved in `runtime-result.json`); `Rebind-ChargeStage158.ps1` re-read
the immutable artifact with the correct reader and recorded the per-row result, and the qualifier now
dispatches the delivery stage correctly.

## Preview.159 - delivery corrections (2026-10-03, FAST-verified, not yet frozen)

Three fixture and reader corrections, no product change:

- The charge fixture waits for `CanActInCombat` and `IsAbleToAct` before clicking, the same native readiness
  the stock charge fixture waits for.
- Every row's economy figures become the increase that row's own attempt caused above its own baseline, so a
  repeated request made while an earlier charge's cost still stands reports zero rather than that cost.
- The repeated request is no longer its own case: it runs inside the lawful charge's own combat, immediately
  after it settles, while the rider's standard action is genuinely spent. The row set is unchanged.
- The carrier measurement samples on frame advance as well as game time and takes the peak speed from the
  agent's own `Speed`, because the turn-based game clock barely advances across the path. The reader requires
  three samples on the arrival row and two on the deliberately short interrupt row, and its turn-based rules
  now assert the paired movement accounting (present, advancing, no mount cost, rider turn, no prior movement)
  instead of forced turn time.

Offline verification: FAST 15/0, charge reader 66/0, carrier reader 133/0, component tests 564/0.
