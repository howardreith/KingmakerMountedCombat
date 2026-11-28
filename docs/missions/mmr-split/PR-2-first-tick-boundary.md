# PR-2 first-tick replacement boundary (R1-F6)

Owner instructions 2026-11-27d and 2026-11-27e (sections 3 and 4); review rounds 7 to 12 (owner instruction 2026-11-27f for rounds 10 to 12). Sections 2-5 describe the round-7 path; sections 6 to 10 record the
round-8 to round-12 changes, which supersede any earlier passage they contradict (marked *superseded* below). This note records how the restricted single-action approach is decided
and how a click reaches that decision, checked against the installed assemblies. Round 7 replaces round 6's bare mount carrier with a
**provisional order on the rider** (R1-F6, R5-F3, R6-F1..R6-F7). The round-5 TwoActions workaround stays removed; the player's single-action
selection is never rewritten. R5-F1 and R5-F2 are unchanged. Round 8 keeps the verified round-7 path (R1-F6, R6-F3..R6-F6) and corrects
R5-F3, R6-F1, R6-F2, R6-F7 and R7-F1..R7-F5 (section 6). Line numbers below are at the round-8 commit.

## 1. Installed contracts

Kingmaker 2.1.7b `Assembly-CSharp.dll` at the install path in `environment-intake.json`, read by reflection in
`scripts/Test-MmrTbApproachNativeContract.ps1` ("PR-2 R1-F6 (round 6)" and "(round 7)").

1. Per frame, `GameMode.Tick` ticks controllers in registration order: `TimeController` (sets `DeltaTime`) < `DefaultPointerController`
   (the click: `TurnController.IgnoreClick`, then the handler) < `UnitMoveController` (each moving agent's `TickMovement` ->
   `CanMoveInTurnBased`, where the product's `MountMovementPrefix` runs) < `UnitActionController` (commands start in `TickCommand`).
   A carrier issued by a click therefore moves first in a later frame, and that frame's tick length is known only at that tick.
2. Under the single-action limit a restricted mount with no Move spent gets exactly one tick: `GetRemainingMovementTime` returns 0 once
   `MoveAction > 0`.
3. `TurnController.TurnStatus`: Preparing = 2, Acting = 3. `get_IsActing` (0x06000C24) is exactly `Status == Acting`.
4. `TurnController.Tick` (0x06000C34): a Preparing turn becomes Acting only when `IsActed()`, when its `m_Commands` is not `Empty`, or when its
   unit is not `IsAbleToAct`. The constructor sets `m_Commands = unit.Commands`, so for the rider's turn it is the **rider's** container. A bare
   mount `UnitMoveTo` (round 6) never moves a fresh Preparing turn, and `CanDelegateMountMovement` needs Acting: that was R1-F6's core.

## 2. Production path from click to strike (file:line at this commit)

1. **Click routing.** `MountedCombatController.TryRouteMountedStockAttack` (MountedCombatController.cs:2039) takes the click's proof. A proof
   admitted in `single-move-first-tick` mode (`proof.FirstTick`) that does not continue the retained order's own target goes to
   `BeginProvisionalOrder` (:2138 -> :1534). Nothing of the previous order, manual target or Hold is touched; native Run of the click's
   `UnitAttack` is refused as for every routed click.
2. **Provisional order.** `BeginProvisionalOrder` refuses before installing anything when any rider or mount work is live
   (`pair-work-live`, `rider-holds-live-work`, `mount-holds-live-work`) or the route is missing. It publishes the request, then calls
   `TryExecuteAction(..., provisional)` (:830; provisional branch :886): the real action evaluation (`CaptureContext` with the click's own
   target-life fact), an ordinary-mode `MountedPairAttackCommand` for target B with the proof's route, plan and revalidator, the click-time
   target-life fact (`AdmitTargetUnconsciousWhenOrdered`), no intent or generation wiring, no `OwnManualTarget`, and
   `MarkProvisionalOrder` (its `MarkManualTarget` is false, so native `UpdateCombatTarget` writes nothing). The wrapper is run in the
   rider's Standard slot. Any failure (a native Run that throws before installation included) refuses and sweeps the request: no gate,
   no active command.
3. **Post-Run target and Hold hooks.** `PatchMethods.ClickManualTargetAssignment` (MountedPatchController.cs:1038) asks
   `DefersClickTargetAssignment` (:1898): the click's own assignment of B to the rider is deferred for a pending provisional order of that
   exact click, and suppressed for one that click refused. `NativeRunHoldWrite` (MountedPatchController.cs:1085) asks `NativeRunKeepsHold`
   (:3266): the Run of the provisional command (rider) or of its carrier (mount) keeps the Hold and records that it did.
4. **Native eligibility and phase.** The rider's own command fills the turn's `m_Commands`, so the installed Tick moves a Preparing turn to
   Acting. The wrapper is `activeCommand`, so `NativeActorEligibleForCommand` (PairedActivationLifecycle.cs:747) admits its carrier through
   `OwnsExactPairedNativeCommand` (MountedCombatController.cs:214, `activeCommand.DelegatedMove`). Nothing globally admits off-turn mount
   commands, forces Acting or grants an action.
5. **Carrier and gate.** The wrapper starts (UnitActionController) and, needing an approach, calls its own `BeginDelegatedMove`
   (MountedPairAttackCommand.cs:1861). Its carrier follows a route revalidated from the **current** origin by `RevalidateAdmittedClickRoute`
   (:1914), which marks the approach `FirstTick` when the grant reason is `single-move-first-tick` (the grant is then only the route-search
   ceiling). Only after the carrier holds the mount's Move slot and its ownership proofs passed is `FirstTickCarrierIssued` invoked
   (MountedPairAttackCommand.cs:1970) -> `BindFirstTickCarrier` (:1586), which binds the request's gate to that exact carrier. Every later
   carrier of the same command (a replan) is bound to the same request: a pending one decides it, a refused one refuses it until the sweep.
6. **Movement hook.** `UnitMoveController` -> `MountMovementPrefix` (MountedPatchController.cs:2055) -> `TryOverrideMountTurnMovement`
   (:3376; the provisional order is the active attack, the turn is Acting) -> `UnifiedMountedTurnCoordinator.TickMountMovement`
   (UnifiedMountedTurnCoordinator.cs:631) -> `MountedMovementStateAdapter.TickDelegated` (MountedMovementStateAdapter.cs:223, gate at :230).
7. **First-tick decision.** The allowance is `min(deltaTime, Remaining) x speed`, before any debit. `DecideFirstTickProposal` (:1634, predicates in `EvaluateFirstTickDecision` :1695) rechecks
   from live state: the turn, mount addressability, that the provisional order is still the active command in the rider's Standard slot,
   that the carrier still holds the Move slot, no other mount work, target validity under the **click-time** target-life fact, and the
   rider's entitlement through the dispatcher's own `RiderMeleeClickAdmitted` (Standard, action eligibility, attack capability; the
   provisional command counts as the pair's own transaction). `PairedAttackClickProbe.EvaluateFirstTick` (PairedAttackClickProbe.cs:195)
   judges the tick from the mount's **current position** toward the carrier path's **next waypoint** (points within 1 mm are passed, as the
   mover passes them): travel = min(allowance, distance to that waypoint), so a tick never crosses a waypoint. In range: commit in place;
   the route end reached: the proven arrival disc; otherwise the interior stop under the travelled heading. *Superseded (rounds 8-9):* the
   carrier's path is no longer cut; the decision reads the agent's own path and cursor and the adapter shortens the tick to the granted
   travel (sections 6 and 7).
8. **Commit.** `CommitProvisionalOrder` -> `AcceptStockRequest(..., committing)` -> `CommitAcceptedProvisional`. *Superseded (round 9):* no
   `CancelCore` runs; the staged, snapshot-protected transfer of section 7 replaces it. Intent B
   starts with the click's facts (its target-life fact), the retained-order ledger records the provisional command and its exact carrier as
   B's own, the supersession check and own-move announcements are wired, the deferred manual-target write and Hold clears are applied once
   unless the player assigned a newer target (then the newer selection is kept), and `stockIntentCommand` is the provisional command.
   Success is read from the actual state (`HasStockAttackIntent`, a new generation that owns B, `stockIntentCommand == activeCommand ==` the
   command, ledger current), never from a routing return value. A failure before retirement leaves everything as it was and refuses; a
   failure after the effects is judged by that same state (the round-7 case shows it committed).
9. **Refusal.** The tick is zeroed before any debit. `SweepFirstTickProposal` (:1835), from `TryDriveStockAttackIntent` (:2834) in the same
   frame's controller update, ends the exact provisional command (`InterruptOwnedCommand`) and its exact carrier (only if it still holds the
   Move slot), clears the active slot, releases the gate and lets the previous order resume. A refused request is never revived; repeated
   sweeps change nothing and never touch newer work.
10. **Ownership, release and strike.** After a commit the order owns the exact carrier as its `delegatedMove`; its `TickApproach` stops and
    removes that carrier at the admitted position (or settles an already finished one) before `StartChildAttack` (MountedPairAttackCommand.cs:2133),
    in either processing order. A provisional order that reaches its strike without a decided tick (no carrier needed, or the target came into
    reach) is committed only in place (`ProvisionalStrikeGate` at :2153, after the three-plan check -> `AdmitProvisionalStrike` :1786), otherwise it ends unstarted.

## 3. Retirement, newer clicks and accepted orders

- Every newer accepted click retires a pending provisional order and its exact carrier first (`RetirePendingReplacement`, :2152), the
  same-target `Continue` branch included (R6-F5). A cancellation does the same (`CancelCore`, :1961). The proposal's own commit is the only
  acceptance that keeps it.
- A pending request is also settled when its command ends, is replaced in the active slot, its carrier is lost, its turn changes, or its
  target becomes invalid under the click's fact.
- **R6-F4.** An accepted route-bound order whose carrier is created (or replanned) after the mount became first-tick-only gets the same gate
  (`BindFirstTickCarrier`, ordinary kind): an affordable tick moves and is debited normally; an unaffordable one is zeroed and the order's
  wrapper is ended by the sweep (its terminal retires the intent unexecuted). A newer click's `Continue` keeps that gate with its carrier.

## 4. The retained-order fixture (R5-F3) - *superseded by section 7*

The round-6 fixture's `Status = Acting` with `IsActing = false` cannot exist (section 1, item 3); the harness's `IsActing` is now derived
from `Status`. An undispatched intent with an idle, attack-eligible rider is not reachable in the installed scheduler: `AcceptStockRequest`
dispatches synchronously on a Preparing or Acting turn, `DecideNext` waits only behind an active pair command (which occupies a slot), and
an intent kept after a terminal (`Keep`) requires a delivered strike, which spends the rider's Standard. A first-tick click needs empty rider
slots, so the reachable prior state is the player's own: the rider's selection A (an earlier click's native assignment through the actual
hook) and the horse's Hold. The round-7 cases use that state and read the actual `Generation`, `IntentTarget` and carrier identity. The
same-target `Continue` branch is also exercised defensively with an intent retained through production acceptance while the game mode
refuses dispatch (labelled as not player-reachable).

## 5. Evidence and limits

- Harness (`scripts/Test-MountedAlpha2AttackPathHarness.ps1`, offline; the engine boundary is simulated): the actual
  `OwnsExactPairedNativeCommand`, `NativeActorEligibleForCommand`, `TryOverrideMountTurnMovement`, `NativeRunKeepsHold`,
  `CommitAcceptedProvisional`, the click-assignment hook and the whole first-tick region are spliced. Each frame runs the installed order:
  the turn's Tick (Preparing branch as in 0x06000C34), the mount's carrier through native eligibility and the movement hook, each unit's
  commands (rider-first or horse-first), then the controller drive. No case calls the deciding tick or sets the turn status.
  `R7ProvisionalLifecycle` covers the owner's eight groups: 9 m/s, 0.12 m and 1.2 m at 1/60 s and 0.1 s from fresh Preparing and Acting
  turns, with and without the prior selection and held horse, both initiative arrangements; a newer selection kept at commit; shifted origins
  and a short waypoint (predicted travel equals the mover's own step); rider Standard or capability lost, a target falling during the wait,
  and a target already down at the click; an ordinary order becoming first-tick-only before its carrier; A -> pending B -> accepted A (both
  forms); rider-first and horse-first processing including both commit-in-place paths; installation and acceptance failures, repeated
  settlement and a later ground move; a replan after a refusal staying refused. The R1-F6 regression shows round 6's bare mount carrier never
  reaching its first tick (gate never asked, ineligible every frame, the Preparing turn never Acting). R5-F1, R5-F2, five-foot-step and
  spending regressions are unchanged and pass.
- Not shown natively (owner playtest N02/N03): the registration order of the turn
  controller relative to `UnitActionController` (the harness ticks the turn first); whether native `CanMoveInTurnBased`'s denial branch ends a
  zeroed carrier (the replan case covers both outcomes); and the turn being Acting after a refused provisional order (no action is spent, as
  for an unmounted rider whose issued attack is interrupted).

## 6. Round 8 corrections

- **R6-F2 (live steering).** In the restricted mode the decision reads the movement agent's own path and cursor (`MountedAgentSteering`:
  `UnitMovementAgent.Path`, `m_NextPointIndex` 0x04001193, `m_NextWaypoint` 0x04001196, the fields `NativeGroundMovementObservation` reads;
  the native contract asserts them). It refuses when the agent's path is not the carrier's (`agent-path-not-carrier`) or the cursor does not
  match its waypoint, and judges travel from the mount's position toward the waypoint at the agent's cursor. The command's `ForcedPath` is no
  longer rewritten: a commit grants only the travel to the predicted stop and `TickDelegated` (MountedMovementStateAdapter.cs:243) shortens
  the tick to it, so the executed stop and the debit are the predicted ones however the agent would round a waypoint.
- **R7-F1 (mode changes).** Every route-bound turn-based carrier is bound to a gate (provisional or ordinary), whatever mode its revalidation
  saw. At the first debit the decision reads the current mode with the production grant reader `PairedMountGrantedMetres`: the restricted tick
  as above; an ordinary grant must cover the carrier's remaining route (then the tick and later ticks move normally); anything else refuses a
  moving approach. A provisional order therefore always commits before its carrier's first debit.
- **R7-F2.** The live wrapper's three-plan comparison (`LivePlanDisagreement`) is required before any commit, and in `StartChildAttack` it now
  runs before the provisional commit gate.
- **R7-F4.** A strike admissible where the pair stands commits in place on the rider's own entitlement; any movement needs the mount's current
  authority from `PairedMountGrantedMetres` (addressable, not needing to stand, able to act and move, movement left).
- **R6-F1.** An actual later write of the rider's target, observed at the installed-command hook (`ObserveWorkAgainstProvisional`, :1873) or a
  later click's assignment, is kept at the commit, the same target included.
- **R7-F3.** Work native accepts on either participant while a provisional order is pending is classified with the existing ledger rule;
  competing work supersedes the request at once (its gate refuses), and the sweep ends only its own command and carrier. Coexisting work and the
  successor keep their normal treatment.
- **R6-F7.** The whole decision is contained (a throwing read refuses and the sweep settles the exact request). The commit stages every fallible
  read (action, weapons, combat state, display name, Hold facts) before retiring the previous generation, and applies both Holds last.
- **R7-F5.** A pending door settlement owns the pair: the provisional installation is declined (`door-settlement-pending`) instead of
  cancelling it; an accepted ordinary retarget still cancels it normally.
- **R5-F3 (round 8; superseded by section 7).** A previously accepted order with an idle, eligible rider IS reachable:
  `SettleOrderTerminal`'s `RiderEndedForHorse` branch keeps a generation after a zero-delivery rider terminal on the rider's own capability
  branch, for the horse's recheck. The round-8 control reaches it through the player's native-accepted click on A and native's turn-based
  force-finish of A's unstarted wrapper (the actual terminal hooks), then clicks B in that interval. It records A's intent and ledger
  generation, B's carrier, the first-tick allowance, both actors' spending, and A's disposition at B's decision (before that frame's ordinary
  drive) and after it (A's own next drive completes it when the horse cannot follow).
- **Residual limit.** A first restricted tick toward a nearby intermediate waypoint is capped at that waypoint (conservative: refused unless
  that stop admits the strike).

## 7. Round 9 corrections (partly superseded by section 8)

- **R6-F7 (exception-safe transfer).** `CommitAcceptedProvisional` runs in three phases. (1) Staging: every fallible read, the native order
  effect and the idle preconditions (no charge, door interaction, door settlement, carried approach or rider ground move); a failure changes
  nothing. (2) Transfer of KMC-owned state only, with no engine write: the previous intent, retained-order ledger, automatic-run ledger,
  click proof and manual-target ownership records are captured (`Domain/MountedStateSnapshot`) and then retired, and intent B, its ledger and
  its exact command and carrier ownership are established; a failure anywhere in this block restores the captured state exactly (same
  generation, intent, ledger and ownership) and the request is refused (`previousIntent=kept`). (3) Deferred engine writes (manual targets,
  both Holds), each applied once and contained; one that fails stays outstanding on the request (`settlingProposal`) and the sweep retries it
  while B is still the current order; the decision reports `effects=outstanding(...)` until then. The sweep performs the exact cleanup of a
  refused request first and drops its reference only after the cleanup completed; a cleanup failure keeps the request (and its refusing gate)
  owned and is retried at the next sweep.
- **R5-F3 (reachability, corrected).** Driven through the normal frame order (the terminal in UnitActionController, the controller drive
  after it, the pointer event in a later frame), the `RiderEndedForHorse` interval is never visible to a click: the intervening drive either
  dispatches the horse's follow-through (A stays the order; B then cannot install because the pair is busy) or completes A when the horse
  cannot follow (B is a fresh request). The round-9 control records both, with the frame order, A's generation, B's carrier, displacement and
  spending. The ledger state from that terminal is still used as a setup for the R6-F7 and R8-F3 protection cases, not as a reachability
  claim. A pending replacement over a retained, idle-rider order is therefore not player-reachable; the replacement logic protects the
  player's own selection and Hold, and any retained KMC state it finds.
- **R8-F1.** The ordinary first-debit branch validates the agent's remaining route from its live position and cursor
  (`PairedAttackClickProbe.EvaluateRemainingRoute`) and the strike's admission at arrival (current target and sight) before any debit.
- **R8-F2.** An ordinary gate refuses when its generation is superseded (`retainedOrder.Superseded`); live mount work is judged with the
  existing accepted-work classification (`CompetingMountWork`), so a compatible in-range Swift coexists with the carrier.
- **R8-F3.** A command whose request was refused issues no further carrier (`FirstTickCarrierPermitted`): no Run, so no Hold write and no
  accepted work against the retained order.
- **R8-F4.** When an order releases its own carrier because its strike became admissible where it stands (`TickApproach`, then
  `CarrierReleasedForStrike`), an ordinary gate settles as a stationary strike and a provisional order is decided in place; genuine carrier
  loss still refuses.
- **R8-F5.** The gate receives the input context the executing movement adapter debits with (`TickDelegated`'s `MovementInputContext(turn)`)
  and reads mode and grant on it (`PairedMountGrantedMetres(..., movementInput)`) with the live mount capability checks.
- **R8-F6 (installed shift).** The native contract traces `UnitMovementAgent.TickMovement` after the movement hook: warm-up returns without
  moving while `m_WarmupTime > 0`; avoidance is skipped in turn-based combat; the speed is `MaxSpeed`, raised to at least
  `MaxSpeed x m_MinSpeed` and cut by `m_SlowDownTime` or by `SlowDown` when the direction must turn more than this tick's angular step; the
  shift is `MoveDirection x speed x` the hook's delta. `TickDelegated` sets `m_MinSpeed 1`, `m_WarmupTime 0`, `m_SlowDownTime 0` for an
  allowed tick. `MaxSpeed` (UnitMovementAgentBase) is `MaxSpeedOverride`, else the unit's `CurrentSpeedMps`. The restricted decision therefore measures distance with the agent's own `MaxSpeed` (allowance and granted time) and refuses
  when the agent's `MoveDirection` differs from the waypoint direction by more than 1 degree (`agent-turning`), the remaining case where the
  shift could differ from the prediction. This is conservative: a first restricted tick taken while the agent is still turning is refused.
- **Residual limits (owner playtest N02/N03).** The registration order of the turn controller relative to `UnitActionController`; whether
  native `CanMoveInTurnBased`'s denial branch ends a zeroed carrier; how often a native first tick starts with the agent already aligned (if
  rarely, restricted first-tick approaches are mostly refused, never spent); the turn being Acting after a refused provisional order (no
  action spent).

## 8. Round 10 corrections (owner instruction 2026-11-27f, section 3; partly superseded by section 9)

- **R6-F7 (cleanup obligations, real rollback evidence).** A refused request's cleanup is a set of obligations tracked on the request,
  independently of native `Result`/`IsFinished`: the command's terminal interruption (issued once; never replayed once native stamped it),
  the carrier's, the carrier's removal from the mount's Move slot, and (after a faulted attempt) the mount's motion stop while no successor
  holds the Move slot. A failure keeps the request owned with its gate refusing; the next sweep does only the unfinished work. The transfer
  fault point now follows the actual retirement mutations (`stockIntent.Cancel`, the ledger cleared, ownership released); the harness records
  that state at the fault before asserting the exact restoration.
- **R8-F1.** `EvaluateRemainingRoute` applies `RouteInsideDiscAdmits` (the click's 1 mm sampling, each segment with its own heading) to the
  live remaining subpath, besides `DiscAdmits` under the final heading. *Superseded (round 11):* the 0.2 mm inward margin is removed (it skipped
  a reachable grazing chord); section 9 evaluates the carrier's own revalidated native route with the full radius.
- **R8-F6 (installed shift at the returned delta).** The one-degree cutoff is gone. `PairedAttackClickProbe.EvaluateFirstTickNative`
  predicts the restricted tick with the installed combat steering rule (traced in IL and asserted by the native contract): waypoint skip while
  (follow-precomputed, not last) distance < `m_Speed x delta`; target `Lerp(MoveDirection, toWaypoint, 0.5)`; `RotateTowards` by
  `m_CombatAngularSpeed x delta` with the last-segment raise; speed `MaxSpeed` (TB combat: `SlowDown` returns at once, `m_MinSpeed` 1);
  shift = new direction x speed x delta; after the move, on the last segment, a waypoint passed or within 0.05 m puts the agent on the
  path's end. `delta` is the time returned to native movement (shortened so the shift does not pass the waypoint the agent steers to); a
  waypoint native would skip at that delta is refused. The harness stand-in runs the same native rule independently with the delta the
  production hook returns. Inputs that cannot be read (or a forced/non-combat agent) refuse.
- **R9-F3/R9-F4 (movement units).** The adapter converts time and distance with the agent's own `MaxSpeed` everywhere it does so (allowance,
  `RemainingOrderMetres`/`RemainingGrantedMetres`, `MountedMovementState.Advance`), so click admission, carrier revalidation (the same
  `PairedMountGrantedMetres`) and the debit price an ordinary Move identically. *Superseded for steps (round 11):* the step ledger's units are
  corrected in section 9 (native counter in nominal units, physical travel separately).
- **R9-F1.** Actual assignments of either actor's manual target observed after the commit (installed-command hook, click hook; the same
  target included) supersede that actor's outstanding deferred write or clear.
- **R9-F2.** An ordinary carrier's stationary release uses the current in-place admission decision (rider entitlement included), and
  `StartChildAttack` asks the order's own first-tick request for accepted route-bound orders too; a refused one starts no child.
- **R9-F5.** The `MaxSpeed` read is contained at the production movement entry (`PhysicalSpeedOf`, `MountedAgentSteering.PhysicalSpeed`): an
  unreadable speed refuses a gated carrier through the existing refusal and exact cleanup and gives an ungated tick no movement.

## 9. Round 11 corrections (owner instruction 2026-11-27f, sections 3-4; partly superseded by section 10)

Line numbers are at the round-11 commit. The verified path above is kept unchanged.

- **R6-F7 (one cleanup path for the exact carrier).** `MountedPairAttackCommand.StopDelegatedMove` (MountedPairAttackCommand.cs:2140) records
  its progress on the command for that exact carrier (interrupted, removed from the mount's slots, motion settled), whichever caller runs it:
  native interruption's `OnEnded` (a target-invalidation callback or native command), the order's own release, or the controller's sweep.
  `SettleCarrierCleanup` (:465) resumes only the unfinished steps on the captured mount with the same successor rule as the normal release
  (`SettleCarrierMotion` :451: a live Move-slot successor, or an unstarted out-of-range command in another slot, including a Standard the
  retried slot cleanup itself promoted from the queue, keeps its movement; otherwise the motion is stopped once). `SweepFirstTickProposal`
  (MountedCombatController.cs:1959) uses that path for the refused request's carrier and drops the request only when it reports every step
  settled; the command's terminal is still issued once and never replayed.
- **R8-F1 (full radius, same points).** The revalidation's untruncated native route and the destination's arc length travel with the carrier
  (`PairedAttackClickProbe.Approach.NativeRoute/DestinationMetres`, set at MountedCombatController.cs:2111; `MountedPairAttackCommand.ApproachOf`
  :427). At the first debit `EvaluateRemainingRoute` (PairedAttackClickProbe.cs:415, called at MountedCombatController.cs:1831) requires the
  agent's path to be that route truncated at the destination, then applies `DiscAdmits` (destination segment heading) and
  `RouteInsideDiscAdmits` (:683) with the FULL arrival radius on those same points, so its samples are bit-identical to the ones that proved the
  route and only a change of live state (target, sight) changes the verdict; the part already travelled (before the mount's projection on the
  segment toward the agent's cursor) is skipped and the mount's current pose is checked when it already stands inside the disc. The
  `BoundaryMarginMetres` inward margin is gone from this check (it remains only as the click walker's step past a failed opening).
- **R9-F3 (two step limits).** `MountedMovementState` (Domain/MountedMovementState.cs:33, :86) keeps `MetresStepped` as the installed
  counter (nominal metres: `CurrentSpeedMps x time`, the unit `TurnController.TickMovement` charges and the adapter writes back to the turn) and
  `MetresStepTravelled` as the physical step travel. A step's remaining time is the smaller of `(1.524 - MetresStepped) / CurrentSpeedMps` and
  `(1.524 - MetresStepTravelled) / MaxSpeed`; `Advance` reserves `MaxSpeed x time` (an upper bound of the shift) and the adapter replaces the
  reservation with the steered displacement actually executed once it is observed (`Allocation.SettleStepTravel`, MountedMovementStateAdapter.cs:35,
  at the next tick :249 and at every allowance read), so claims about metres travelled use the steered displacement. Click admission,
  revalidation (`RemainingOrderMetres`/`RemainingGrantedMetres` with `NominalSpeedOf` :335) and the debit use the same two limits; the existing
  NearTheEnd endpoint tolerance is unchanged. Ordinary Move stays time-limited.
- **R10-F1 (waypoint and delta together).** `EvaluateFirstTickNative` (PairedAttackClickProbe.cs:315; loop at :333) resolves, for each candidate
  waypoint, the delta returned to native (the full allowance, shortened by bisection when the shift would pass the waypoint) and applies native's
  skip test `distance < m_Speed x that delta`; a skipped waypoint moves the prediction to the next one, and the result is accepted only when every
  skipped waypoint is skipped and the selected one is kept at the final delta (`native-waypoint-unresolved` otherwise).
- **R10-F2 (decisive final ordinary tick).** At the first debit an ordinary grant that this tick exhausts (`grant <= deltaTime x MaxSpeed`,
  MountedCombatController.cs:1813) is predicted with the installed steering and arrival rule (`EvaluateFirstTickNative`) and commits only when
  that stop admits the strike (the tick is then shortened to the predicted travel); otherwise it refuses before any debit. At the click
  (:1394), an ordinary grant ending within one tick at the current frame's `TimeController.DeltaTime` (the delta the installed UnitMoveController
  passes to TickMovement; native contract) makes the click a provisional request, so the previous order, manual target and Hold are kept until
  that decision. When more than one tick remains, the route-length check stands (see open questions).
- **R10-F3 (first child entitlement).** A committed movement gate records its request (`firstChildEntitlement`, :1714); `AdmitProvisionalStrike`
  (:1872), asked by `StartChildAttack` before the first child starts, checks the rider's current entitlement (`FirstTickRiderEntitled`, the
  dispatcher's own admission including `HasStandardAction`) once for that order; a refusal starts no child and spends nothing. Released strikes
  keep their native handling.
- **R10-F4 (old target clear survives B).** `ApplyAcceptanceEffects` (:2476) ends B's own effects (its target write, the Hold clears) with B but
  keeps the retired generation's mount-target clear (A's owned selection, released at the transfer) until it succeeds, an actual newer assignment
  on the mount is observed, or the mount no longer holds that exact target; it also runs after B retires.
- **R10-F5.** The restricted-tick detail reports `turnCapacity` with the effective angular speed (last-segment raise included) and
  `baseTurnCapacity` with the base rate.

*Superseded in section 10:* the click's one-tick classification by the click frame (R10-F2), the step's exclusion from the final-tick rule
(R11-F2), the first-child check limited to committed gates (R11-F6), the single settling reference (R11-F8) and the in-route check of a
displaced mount (R11-F3).

## 10. Round 12 corrections (owner instruction 2026-11-27f, sections 3-4) and the current state

Line numbers are at the round-12 commit. Everything verified by review is kept (R1-F6, R5-F1..R5-F3, R6-F1..R6-F6, R7-F1..R7-F5, R8-F1..R8-F7,
R9-F1..R9-F5, R10-F1, R10-F3..R10-F5).

- **R10-F2 (no click-frame dependency; the last debits).** Installed `TimeController.Tick` sets `DeltaTime = Min(PlayerTimeScale x DebugTimeScale
  / 12, frame delta)` and clamps the turn-based `PlayerTimeScale` to 3 (native contract), so no turn-based movement tick exceeds 3/12 s
  (`NativeMaxTurnBasedFrameSeconds`, MountedCombatController.cs:1425). A route-bound ordinary (time-limited) click whose grant is within two such
  ticks (`ShortOrdinaryApproach`) is a provisional request whatever its own frame (:1403): previous order, target and Hold are kept. At the first
  debit (:1866) a grant this tick exhausts is predicted with the restricted tick's rule (`EvaluateFirstTickNative`); a longer short grant is
  decided across its remaining debits with the installed TickMovement rule at this tick's frame (`PairedAttackClickProbe.EvaluateNativeTicks`,
  PairedAttackClickProbe.cs:431: proximity completion at each tick start, waypoint skip at m_Speed x tick, combat steering, MaxSpeed shift,
  passed-waypoint and last-segment arrival; the strike judged at every tick boundary); it commits only when a boundary admits the strike
  before the carrier completes, the path ends or the time is used.
- **R11-F2 (steps are distance-limited).** Only an ordinary grant can be a final tick (`ordinaryTime`, :1866); a step tick reserves a physical
  upper bound and keeps its route-length check. `HasGrantedMovement` (MountedMovementStateAdapter.cs:426) settles completed step travel before
  answering, so the eligibility entry never reports a reserved remainder as spent.
- **R11-F1 (step commitment through save/restore).** The versioned save row has exactly one step field (the archive codec requires the exact
  field set; a new field would invalidate existing saves). `MountedMovementPersistenceRows` (MountedMovementPersistence.cs:52), used by
  `CapturePersistence` (which settles travel first, :18) and `RestorePersistence`, saves the larger of the native counter and the settled physical
  travel and restores both limits from it: the remainder is unchanged when the binding limit is the larger commitment (straight steps at any
  speed, slower-than-nominal steps) and never larger otherwise; older rows restore both limits from their value.
- **R11-F3 (live connecting leg).** When the mount stands off the segment toward its cursor by more than the mover's own passing distance,
  `EvaluateRemainingRoute` (PairedAttackClickProbe.cs:546) validates the live leg `[position -> cursor point]` at the full radius with its own
  heading (its start being the current pose) and the native route from the cursor point on; the arrival disc takes the live heading when the
  cursor point is the destination. On the planned segment the planned samples stay.
- **R11-F4 (longer alternative).** `EvaluateFirstTickNative` (:315, loop :337) evaluates, when the internally consistent stop is blind or
  inconsistent, the resolution starting at the next waypoint (every earlier waypoint must be skipped by native at the delta it returns) until the
  route's last point, before refusing.
- **R11-F5 (approved duration).** A committed gated tick returns `min(frame, granted / MaxSpeed)` and debits it with NearTheEnd disabled for that
  tick only (MountedMovementStateAdapter.cs:292, :322); later ticks keep native's endpoint tolerance.
- **R11-F6 (first child, every accepted wrapper).** `AdmitProvisionalStrike` (MountedCombatController.cs:1942), which `StartChildAttack` asks only
  before the first child, checks the rider's current entitlement for every accepted click-bound order (gate attached at dispatch), including
  one whose carrier was deferred behind older work or never needed.
- **R11-F7 / R6-F7 (cleanup ownership after a committed debit).** The exact carrier's cleanup is a record (`MountedCarrierCleanup`,
  MountedPairAttackCommand.cs:2628) begun by `StopDelegatedMove` (:2097) or `SettleCarrierCleanup` (:442); terminal settlement retains an
  unfinished record (MountedCombatController.cs:4172; the record, never the finished wrapper) and the per-frame sweep resumes it
  (`SettleRetainedCarrierCleanups`, :1587) with the same slot and successor rule; the terminal is reported once by the command.
- **R11-F8 (target cleanup bound to its actors).** Every committed request with outstanding effects is retained in a list (`SettleOutstandingEffects`,
  :1565); each records the exact mount and rider it addresses (:2529) and clears the retired generation's target only on that mount (:2553), only
  while it still holds that target and no newer assignment on that actor was observed (observation over every request, also after the pair is
  unbound, :1153). The controller's per-frame drive (which runs in every relationship state) finishes it after a dismount.
- **R11-F9 (speed facts).** `TickDelegated` reads both speeds once, requires CurrentSpeedMps only for a step (:266), and scopes those facts to the
  allowance reads of its decision and the debit (`ReadSpeeds`, :47); an ordinary Move with an unreadable CurrentSpeedMps is priced by MaxSpeed, a
  step without it is refused before any effect (at the click through `HasGrantedMovement`, at the debit through a NaN allowance).
- **R11-F10 (compatible work).** A short ordinary provisional order coexists with compatible mount work by the existing classification
  (`CompetingMountWork`, :1650); the restricted tick keeps its stricter rule and competing work still refuses.
- **Harness (R6-F7 evidence).** Native `StopMoving` releases the agent's path in the stand-in; the cases record the agent's path owner at the
  fault (the old carrier, path present), after the retry (none, or the successor's own path), a negative control (the release assertion is
  false at the fault), and a later ground move through the same mover (`MoverGroundTick`: displacement measured on the horse, one debit).

### Developer matrix (owner section 4), with the cases that exercise each cell

| Mode | Click | Revalidation | First debit | Subsequent debit | Initial strike |
|---|---|---|---|---|---|
| Ordinary Move (time) | R10-F2 (round 12) 1/120 s click; R11-F10 Swift / competing; R11-F9 failed CurrentSpeedMps | same grant reader | R10-F2 one tick (0.02 s) and two ticks, aligned / perpendicular; R11-F5 NearTheEnd; R11-F3 live leg; R8-F1 grazing | R10-F2 two-tick aligned; R11-F7 remaining approach | R11-F6 deferred carrier, R10-F3, R9-F2 |
| Step (distance) | R11-F1 restored remainder (0.3 m admitted, 0.4 m refused); R11-F9 refused click | same reader | R11-F2 partial turning step; R11-F9 refused debit | R11-F2 second tick after settlement; R9-F3 ledgers | R11-F2 strike |
| Restricted first tick | R1-F6 groups | R6-F4 | R11-F4 longer alternative, R10-F1, R8-F6 | R8-F5 | R6-F3, R6-F6 |

| Failure phase | Cases |
|---|---|
| Before mutation | R6-F7 entitlement read, native order effect, staged read, installation failure; R9-F5 and R11-F9 speed reads |
| After mutation | R6-F7 transfer fault after `stockIntent.Cancel` |
| After native completion | R6-F7 slot cleanup failure (no successor, Move, queue-promoted Standard); R6-F7/R11-F7 StopMoving fault after target invalidation, refused provisional and accepted after the committed debit (no successor, Move successor), with path ownership and mover ground move |
| Deferred-write retry | R9-F1; R10-F4; R11-F8 dismount, replacement mount with the same target, two outstanding requests |

### Verified, open, pending

- Verified by review: R1-F6, R5-F1..R5-F3, R6-F1..R6-F6, R7-F1..R7-F5, R8-F1..R8-F7, R9-F1..R9-F5, R10-F1, R10-F3, R10-F4, R10-F5.
- Corrected in round 12 with offline evidence (open until reviewed): R6-F7, R10-F2, R11-F1..R11-F10.
- Remaining source questions: the native first-tick `MoveDirection` of a freshly issued carrier and `m_FollowPrecomputedPath` for a ForcedPath
  carrier (both read live); the multi-tick decision assumes the remaining ticks run at the deciding tick's frame (a later frame-rate change, or
  a NearTheEnd correction on an ungated later tick, can move the final stop; no spending beyond the grant); the frame bound assumes the shipped
  DebugTimeScale of 1 (a debug multiplier); after a save/load the step commitment of a faster agent that turned (physical travel below the
  native counter) is restored conservatively (less remainder, never more).
- Pending native observation (owner playtest N02/N03): restricted and short ordinary approaches in the game, the turn controller's
  registration order, native's denial branch ending a zeroed carrier, the turn staying Acting after a refused provisional order (no action
  spent), native step behaviour under a speed override, and a mid-step save/load.

## 11. Round 13: owner amendment (2026-11-27g) and the installed native baseline

Prepared under owner amendments revision 1 sha256 aa3ab2f52199b92d03230c72d2fc9f43f813b176ae5c1ee7003e4f7926de1825 (the owner instruction is
recorded verbatim in `docs/missions/mmr-split/owner-authorization-2026-11-27g.md`). Line numbers are at the round-13 commit. Everything the
round-12 review verified is kept (R1-F6, R5-F1..R5-F3, R6-F1..R6-F6, R7-F1..R7-F5, R8-F1..R8-F7, R9-F1..R9-F5, R10-F1, R10-F3..R10-F5, R11-F2,
R11-F4..R11-F7, R11-F9, R11-F10; R11-F10's expectation changes, see below).

### 11.1 Native baseline (section 3 of the amendment), read-only IL

Basis: the installed `Assembly-CSharp.dll` (Kingmaker 2.1.7b; sha256 prefix 3B6450FFEC440E29, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7),
read-only IL listings; no game run. The connected control is the native contract script (`scripts/Test-MmrTbApproachNativeContract.ps1`,
the "PR-2 round 13 baseline" checks), which asserts each statement below on the installed assembly.

- **Acceptance and target assignment.** `TurnController.IgnoreClick` (06000C2F) drops a turn-based click whose visualized path
  (`PathVisualizer.IsCurrentPathForPoint`, the movement-limited prediction) does not end within the predicted approach radius; nothing
  forecasts later frames. The accepted `UnitAttack` runs and its target becomes the manual target on that acceptance (the existing
  click-assignment hook).
- **Approach and variable ticks.** `UnitCommand.TickApproaching` (060027A6) issues ONE turn-based approach on the visualized path
  (`UnitMovementAgent.FollowPrecomputedPath` with a `ForcedPath`, then `NextApproachTime = TimeSpan.MaxValue`: no re-path); an empty path
  interrupts the command; once in range it stops the mover and turns to the target. `UnitMoveController.Tick` (06009183) ticks every moving
  agent with that frame's `TimeController.DeltaTime`: each tick has its own duration.
- **Exhaustion before reach or sight.** `TurnController.TickMovement` (06000C37) clamps each tick to `GetRemainingMovementTime` (06000C5A),
  except on `UnitMovementAgent.NearTheEnd` (its "Moving on borrowed time" endpoint correction keeps the full delta), and returns without
  movement or debit when the remaining time is zero; it never interrupts or finishes the command.
- **Waiting, completion, cancellation.** `UnitActionController.TickCommandTurnBased` (0600911D) force-finishes (`ForceFinishForTurnBased`)
  an approaching command whose approach was issued only once its agent no longer wants to move (`UnitEntityView.IsMoving` = `WantsToMove`);
  while it wants to move the command waits (until the turn ends or a newer order replaces it). `UnitCommand.Start` (060027A5) starts the
  action only from an actually close-enough position (`IsUnitEnoughClose`; otherwise it interrupts).
- **Standard consumption.** `UnitActionController.TickCommand` (0600911E) calls `UpdateCooldowns` (06009120; turn-based: the unit's own
  `UpdateCooldowns`, which sets the Standard) only when the command becomes `IsActed` after its `Tick`: an approach that never strikes spends
  no Standard; movement is debited tick by tick and stays spent.

Conclusion: the trace supports the amended contract (acceptance is a lawful attempt; execution uses each tick's actual duration; exhaustion
leaves the approach waiting or force-finished, without a strike, a Standard or a refund). No contradiction was found. Not traced: the
concrete agent's `WantsToMove` after a zero-delta tick (whether native waits or force-finishes at exhaustion); both outcomes spend nothing
more and strike nothing, and the KMC carrier keeps the waiting outcome (offline control below).

### 11.2 Corrections and changed expectations

- **Ordinary acceptance (amendment section 4).** `PairedAttackClickProbe.EvaluateNativeTicks` (the 256-tick forecast), the short-ordinary
  branch of `EvaluateFirstTickDecision`, the frame bound `NativeMaxTurnBasedFrameSeconds`, `ShortOrdinaryApproach`, the click's
  `shortApproachClick` and `ClickProof.ShortOrdinary` are removed (they existed only for the replaced eventual-strike guarantee). An ordinary
  or step click whose route the grant covers is an accepted order (MountedCombatController.cs:1419); only the restricted single tick stays a
  provisional request. At the first debit only a genuinely single-tick allowance is decided on its executable stop before the debit
  (MountedCombatController.cs:1889-1912: the restricted tick, an ordinary grant this tick exhausts, a step whose native counter this tick
  makes final); every other allowance applies the common remaining-route and arrival checks whatever the grant size, and later ticks follow
  native.
- **R10-F2 and R12-F6 (changed expectation: the forecast is removed).** The variable-frame examples are execution and accounting controls
  (harness "R10-F2 (round 13 execution/accounting control)"): one tick left aligned strikes, perpendicular is refused before any debit. The
  review's perpendicular schedules: 4 x 1/120 s reach (0.18697, 0.05195), visible, one strike; 1/120 then 1/40 s end blind at
  (0.1747, 0.09071) with the allowance exactly exhausted (Move 3.0 s), the order and carrier waiting with zero-delta ticks, no strike, the
  rider's Standard unchanged, and the player's newer ground order then holds the Move slot with no further debit; a NearTheEnd tick takes
  native's borrowed time (Move 3.00833 s, as installed TickMovement does). Every executed stop, returned duration and debit is recorded; the
  debits never decrease (no refund) and the prior order is not resurrected. The former all-or-nothing guarantee is not claimed.
- **R12-F2 (changed expectation).** A larger ordinary grant no longer selects a forecast branch: it is admitted by the route check and
  executed natively, so it cannot hide an executable strike behind a refusal.
- **R12-F3 (correction by removal).** The short multi-tick branch that bypassed `EvaluateRemainingRoute` is gone; every non-single-tick
  allowance applies it.
- **R11-F3 (correction).** `EvaluateRemainingRoute` (PairedAttackClickProbe.cs:434) drops the 1 mm displacement threshold: the live leg from
  the actual position to the agent's cursor point of the truncated path is validated at the full radius with its own heading (the disc under
  the live heading when the cursor point is the destination), and the established samples are kept from the cursor point on. Only a mount
  ON the planned segment within its numerical identity (`PlannedSegmentIdentityMetres` = 20 micrometres, fifty times finer than the 1 mm
  sampling, with its projection inside the truncated segment) keeps the established samples of that segment (R8-F1: the click and the
  revalidation sampled exactly those points; resampling the same chord at other float positions refused knife-edge destinations). Harness:
  0.8 mm sideways grazing (planned leg 50.2 mm from the destination, live chord inside the disc, blind patch 3.1 mm from every disc sample)
  refused, visible control committed; reverse approach 80 mm past the truncated endpoint, blind patch on the return chord refused, visible
  control committed.
- **R12-F1 (correction).** `MountedMovementState.StepCounterFinal` (Domain/MountedMovementState.cs:90), through the adapter
  (MountedMovementStateAdapter.cs:428) and `PairedMountStepCounterFinal`: a step tick is final when `(1.524 - MetresStepped) / CurrentSpeedMps`
  binds (is not larger than the physical term) and fits the tick; the debit is then decided on the executable stop (`final-step-counter`).
  When the physical reservation binds, the reservation is settled and another lawful debit remains. The click has no separate classification
  any more (changed expectation: the click is native-equivalent admission). Harness: equal speeds, 0.140625 m on both counters, 1/64 s:
  aligned commits and strikes (counter 1.383375 -> 1.519375 m); perpendicular refused before any debit (both ledgers unchanged, no
  displacement, no strike); the faster turning step (18 vs 9 m/s) keeps its route checks and its second tick (R11-F2 case).
- **R12-F5 (correction by removal; one-shot control kept).** The click no longer rereads `MaxSpeed` after its grant: the harness counts the
  grant's reads (2) equal to the whole click's (2); a failure armed for the first read after the grant is not consumed by the click (the
  accepted order equals the clean click's) and the perpendicular first debit is decided on its tick (refused, no movement or debit); a
  failure on the grant's pricing read keeps native's decision (prior target and Hold, nothing installed).
- **R11-F1 (correction; versioned extension, amendment section 6).** Archive schema 4 (MountedArchiveData.cs:13) adds `StepTravel`
  (`SavedStepTravel`, SavedCombatData.cs:242: actor id and settled physical step travel); the shared seven-field `SavedMovementValues` row is
  unchanged and its `MetresStepped` is now the exact native counter (the value `CopyGrantedMovementToContext` writes). Capture records the
  travel on the allocation row (`StepTravelled`, not serialized in the shared row), the archive writer stores it, the codec attaches it after
  strict validation (one unique allocation, a bounded value, no travel without combat), and `Restore(row, travel)` sets both limits exactly.
  The explicit schema-3 reader (MountedArchiveCodec.cs:81) requires the schema-3 field set and restores both limits from the single stored
  value (legacy interpretation: the round-12 writer stored the larger commitment, earlier writers the native counter); schema-3 archives
  remain restorable (NativeMountedSaveFormat.cs:24). The legacy schema-2 writer (selected only without the all-pair archive) still has one
  field and stores the larger commitment (`FoldForSingleFieldFormat`). Evidence: the real codec in `tests/Chunk7PersistenceTests.cs`
  (schema-4 round trip with identities, schema-3 historical read, malformed shapes, future data) and the harness (production capture, travel
  rows, restoration and the real click: straight case restores the 0.6 m native counter, turning case 1.09974 m before and after, 1.085 m
  admitted where the former single value gave 1.074 m). The two halves meet at the allocation row; the codec cannot be compiled into the
  harness.
- **Compatibility direction (alpha rollback).** The new build reads schemas 1-4. An older build cannot read a schema-4 archive (it reports
  newer-format data, preserves it unchanged and does not restore it). Rolling the alpha back after saving with it needs a save made before
  the upgrade; loading historical saves in the new build does not make new saves loadable by old binaries.
- **R6-F7 (evidence correction).** The successor takes its real path and cursor (`TakePath`) at the fault, before any retry; path identity,
  content, owner, cursor and cumulative stops are recorded before and immediately after all retries; negative controls (a cleared path and
  the old carrier's path retained) are rejected by the same assertion; the no-successor and later ground-move controls are kept. No
  production change.
- **R11-F8 (correction).** `MountedAcceptedWorkRouting.Route` (MountedPatchController.cs:42, called by `InternalRunPostfix` at :955) delivers
  the assignment to the actor's owner and to any controller of the world's own pair services that still owes an outstanding operation on
  that exact actor (`ObserveOutstandingAssignment`, `ObservesOutstandingActor`, MountedCombatController.cs:1147/1162); no global
  subscription, no retained mounted relationship. Harness: a same-target reassignment on the original horse after dismount and owner
  retirement is kept; the control without reassignment clears A's target.
- **R12-F4 (correction).** `HasOutstandingCleanup` (MountedCombatController.cs:1606: outstanding effects, retained carrier cleanup records, an
  unfinished refused-request cleanup) is part of `HasRetainedPairCommands` (:120, registry retention and boundary reuse) and of the pair's
  teardown readiness (MountedWorldService.cs:186). `MountedLifecycleSubscriber.HandleModDisable` (run by disable and by unload before
  `patches.Dispose`) drains once (`SettleOutstandingPairCleanup`, MountedWorldService.cs:357), refuses while work remains, drains again after
  its own cleanup (work the cancellation created) and reports ready only when none remains (MountedLifecycleSubscriber.cs:302-319); the
  controller's per-frame drive retries it in every relationship state (:1059); `Dispose` refuses with work outstanding (:4055). This branch
  has no `ServiceLifecycleSequence` (accepted PR-4's extraction): composition must keep this readiness ahead of PR-4's unpatch step.
  Evidence: the lifecycle contract (call order on the compiled product) and the harness's readiness-member cases (failed target clear across
  a dismount, unfinished carrier, steering successor): a retained retryable refusal, then completion. A real disable or unload through
  CompositionRoot is not exercised offline.
- **R11-F10 (changed expectation).** Short ordinary clicks are accepted orders: with an older compatible Swift the order commits at its final
  tick and strikes; behind a competing older mount order its carrier is deferred (no movement, debit or strike; the older order runs).
- **Harness re-expression.** The former two-provisional-requests R11-F8 case is not reachable under the amendment (provisional requests exist
  only for the restricted tick, once per Move); it is re-expressed with one restricted request whose outstanding clear survives its cancel.

### 11.3 Residual

- Offline evidence only; native gameplay observation remains the owner's playtest (N02/N03 and the items in section 10).
- `PlannedSegmentIdentityMetres` is a numerical identity tolerance (20 micrometres); a deviation below it uses the established samples.
- The diagnostic runtime scenarios that read a newly written archive now expect schema 4; they were not run (no game launch).

## 12. Round 14 (owner amendment revision 1, 2026-11-27g)

Prepared under owner amendments revision 1 sha256 aa3ab2f52199b92d03230c72d2fc9f43f813b176ae5c1ee7003e4f7926de1825. Round 13 verified R12-F5 and
R6-F7 and accepted the R10-F2/R12-F6 rebuttals; those are kept. This round corrects the items below; the rest stay open (11.3 and the list at the
end).

- **R12-F1.** A step click whose native counter binds the step (`PairedMountStepCounterBinds`: a debit of it can be final at some frame) is a
  provisional request (`finalStepPossible` at the click): the replacement's target, generation and Hold wait for the before-debit decision. A
  refused final step keeps the prior order, target and both Holds; a faster step whose physical reservation binds stays an accepted order and
  keeps its second debit. Changed expectation: the R1-F6 five-foot-step control (equal speeds) is now a provisional request committed at its
  first debit.
- **R12-F2.** In the larger ordinary branch, a shortened stop within the current tick that admits the strike (the installed steering at the
  returned delta, `EvaluateFirstTickNative`) is carried through the debit; otherwise the ordinary route proceeds. Harness: 5/512 s and
  1/60 s + 0.001 s allowances return 0.009766 s and 0.013356 s, stop at (0.04395, 0.04395) and (0.0601, 0.0601), one strike each.
- **R12-F3.** The connected 49.9 mm grazing control (blind, visible, 50.1 mm outside) runs with the 27 m and the 1.8 m ordinary grant.
- **R13-F1.** `SavedStepTravel.Attach` requires a travel row for every current allocation (encode and decode). The single-value interpretation
  is explicit migration only (`SavedStepTravel.MigrateSingleValue`: the schema-3 reader, schema 1-2 migration, the legacy schema-2 adapter);
  `Restore` refuses a row without travel. Codec controls: a two-allocation archive round-trips unequal counters; a missing row is refused on
  decode and encode.
- **R13-F2.** The diagnostic consumers of a newly written or decoded archive expect `MountedArchiveData.Schema` (decoded archives are always
  normalized to it); historical source checks (schema 1 and 2) are unchanged. Not run (no game).
- **R13-F5.** NearTheEnd's borrowed time applies only to ordinary movement; a step tick stays within both step limits (domain arithmetic
  control: 0.0439 m left at 18 m/s returns 0.0024389 s).
- **R11-F8 (part).** Observation follows the retained request even after `ApplyAcceptanceEffects` emptied its list before a read that threw.

Status after this part: none of the six is open as a production defect; see 12.1 (R12-F4 entry points verified by IL order, not executed).

### 12.1 Round 14, second part (the remaining six findings)

Prepared under owner amendments revision 1 sha256 aa3ab2f52199b92d03230c72d2fc9f43f813b176ae5c1ee7003e4f7926de1825. Line numbers are at this commit. The
first part above (R12-F1, R12-F2, R12-F3, R13-F1, R13-F2, R13-F5, the R11-F8 observation correction) and everything the round-13 review verified
(R6-F7, R12-F5 and all earlier verified items; the accepted R10-F2/R12-F6 amendment rebuttals) are kept.

**Installed trace after a zero-delta tick (R13-F4; read-only IL of Assembly-CSharp 2.1.7b, MVID 07fa1e4d-8618-41b3-9b8d-faa17d3b26f7; no game
run).** It supersedes the "Not traced" sentence of 11.1 and the "native waiting" expectation of the R10-F2 exhaustion control.

1. `UnitMovementAgent.TickMovement` (060018AA) first calls `CanMoveInTurnBased(ref delta)` and returns at once when it refuses (no shift, no stuck
   clock).
2. `CanMoveInTurnBased` (060018A9): for the acting current unit it calls `TurnController.TickMovement(ref delta, forceMode)`; a delta left at
   or below zero refuses the tick (`ldc.r4 0; bgt.un.s; ldc.i4.0; stloc.0`). Outside force mode and cutscene control a refused tick then calls
   `CompleteMovement()` unless the unit's Move slot command is `DontInterruptInTurnBasedMode` (then `m_NoMoreTurnBasedMovementForUinterruptableCommand`
   is set and the path is kept). An ordinary `UnitAttack` occupies the Standard slot, so its Move slot is empty: `CompleteMovement`.
3. `CompleteMovement` (060018B0) calls `Stop()` inside the approach radius, otherwise `InterruptMovement()` (060018AF, which also ends in `Stop()`);
   `Stop()` (060018C2) sets `Path = null` and `RequestedPath = null`.
4. `UnitMovementAgent.get_WantsToMove` (060018BE, the override of the abstract `UnitMovementAgentBase.get_WantsToMove`) is
   `Path != null || RequestedPath != null`: false now. `UnitEntityView.IsMoving` (0600184C) returns the agent's `WantsToMove`.
5. `UnitActionController.TickCommandTurnBased` (0600911D): the command is not close enough (`IsUnitEnoughClose` false), so the cooldown branch
   (close commands without the action or cooldown) is skipped; `ShouldUnitApproach` holds, `NextApproachTime.Ticks > 0` (the one issued path set
   it to MaxValue) and `executor.View.IsMoving()` is false, so `ForceFinishForTurnBased(3)` runs, the executor's queue is cleared and false is
   returned. `UnitActionController.TickCommand` (0600911E) returns immediately on that false, before `Tick` and `UpdateCooldowns`:
   `ForceFinish` (060027AA) sets IsActed and ends the command without `OnAction`, so there is no strike and no Standard; the debited movement
   stays spent.

Conclusion: at exhaustion before reach or sight native does not wait; it force-finishes the approach on the next turn-based command tick after
the refused movement tick. (Waiting happens only while the agent still wants to move, e.g. a Move command with DontInterruptInTurnBasedMode.)
The contract script asserts each step (`PR-2 R13-F4 (round 14)` checks) and the round-13 TickMovement label now says that TickMovement itself
does not finish the command.

**Per finding.**

- **R13-F4 (evidence; changed expectation of the R10-F2 control under the amendment).** Trace above. The harness mover models the installed
  denial branch (`World.NativeDenialCompletesMovement`: a tick that grants no movement releases the agent's path, so its `WantsToMove`, now
  `Path != null` as installed, becomes false); the R10-F2 1/120 s + 1/40 s control now ends the order with the native disposition (below)
  instead of "waiting".
- **R13-F3 (correction).** `MountedPairAttackCommand.TickApproach` (MountedPairAttackCommand.cs:1814-1830): an ACCEPTED route-bound ordinary
  turn-based order (not provisional, first-tick carrier permitted, not charge, turn-based) whose carrier finished out of reach, or whose live
  carrier already moved the mount while the mount's agent no longer wants to move (`MountAgentStoppedAfterCarrierMoved`, :425), ends with the
  installed force-finish disposition: no Repath, no further carrier or route revalidation, no strike, no Standard (`NativeApproachEndedCount`,
  cause Cancelled with the disposition detail). Explicit newer orders, a refused provisional request (its own "first-tick request refused"
  path), the charge and real-time paths keep their handling. Deviation noted: native also clears the executor's queue on the force-finish; the
  mounted order does not clear the rider's queue (a queued player order is not discarded).
- **R11-F3 (correction).** `PlannedSegmentIdentityMetres` is removed. `EvaluateRemainingRoute` keeps the established samples only when the mount
  stands EXACTLY (bit for bit, horizontally) on the planned segment's start (PairedAttackClickProbe.cs:475; the click and the revalidation then
  sample identical points, so tightly placed destinations at an opening's measured start are not refused). Any other position validates the
  live leg with `LiveLegAdmits` (:507): its in-disc chord in double precision, sampled at the planned segment's own parameters (the live point
  projecting onto each established sample) and, additionally, every part of the live chord inside the disc but outside the planned chord at the
  1 mm spacing with both ends, all with the live heading; a leg the planned segment cannot parameterize is sampled uniformly. Harness: the
  reviewer's counterexample (rotated 11.25 degrees so the chord's normal lies between ring samples): planned leg 50.004 mm, live leg 49.996 mm,
  live chord 1.2615 mm, blind patch 9.7 mm from every ring sample: refused before any debit with the prior order kept
  (`refusedOutsidePlannedChordAt`); visible control committed and struck. The 0.8 mm grazing, reverse-approach and 49.9 mm short/long-grant
  controls still pass.
- **R11-F1 (evidence correction).** The harness now compiles the whole persistence files (SavedCombatData.cs, MountedSaveData.cs,
  MountedArchiveData.cs, MountedArchiveCodec.cs, NativeMountedSaveFormat.cs, MountedLoadAdmissionPolicy.cs, RetainedNativeReentryPolicy.cs: the
  set Test-PersistenceData.ps1 compiles) with the installed Newtonsoft.Json, and splices the adapter's production `CapturePersistence`,
  `RestorePersistence` and `CopyGrantedMovementToContext` (with the installed turn members it writes). Chain per case: production capture ->
  the writer's travel rows (`StepTravel = SavedStepTravel.Detach(Combat.Allocations)`, the assignment of MountedPersistenceService.World.cs) in
  an envelope from the persistence tests' builder -> `MountedArchiveCodec.Encode/Decode` -> `RestorePersistence` -> `CopyGrantedMovementToContext`
  into the restored partner turn, whose `MetersMovedByFiveFootStep` is read before the real click. Straight: partner step field 0.6 m (0.9
  before the copy), physical 1.2 m, grant 0.324 m, 0.3 m route admitted; turning: 0.45 / 0.42426 m, 1.09974 m before and after, 1.085 m
  admitted (the former single value gives 1.074 m); actor, pair, partner-turn actor, round and grant identities equal. Historical schema-3
  rewrite (single 1.25 m): source schema 3 -> 4, both limits and the partner field 1.25 m, grant 0.274 m. Malformed (current archive without its
  travel rows): codec Invalid, nothing restored. The envelope's non-allocation content comes from the persistence tests' builder (the full
  archive writer needs the game's world services).
- **R11-F8 (evidence correction).** The harness splices the production `InternalRunPostfix` and routes native Run through it. Its custody
  lookup (`PatchBridge.CombatFor(commands)`: the pair whose relationship currently holds the actor) returns none for the horse after the
  dismount; the registry (`PatchBridge.Pairs`) keeps the pair only through its ownership-retention term, the production
  `HasRetainedPairCommands` (now spliced) that MountedWorldService binds to the relationship's ownership readiness. An intervening failed retry
  keeps the clear owned; the same-target command on the original horse is routed by the postfix and survives the successful retry; the control
  clears A's target; afterwards the settled pair leaves the registry. (Charge ownership is not modelled by this harness: `HasChargeOwnership`
  is false there.)
- **R12-F4 (correction; evidence partly offline-limited).** `CompositionRoot.SetEnabled(false)` drains outstanding owned pair cleanup once,
  outside the save/load fences, before its `AllTeardownOwnersReady` check (CompositionRoot.cs:241). `UpdateCore` runs a lifecycle-owned drain
  while `IsEnabled` is false (after `PerformFailureCleanup` or a refused disable stopped the ordinary pair update), one attempt per frame,
  never inside the save/load/area/restoration fences (:405-409). `HandleModDisable` consumes both drain verdicts, before the readiness check and
  after the cancellation performed by its own cleanup (MountedLifecycleSubscriber.cs:303-304, :319-320); a refusal precedes any unpatch in
  both disable and unload (Dispose calls HandleModDisable before `patches.Dispose`). Evidence: the lifecycle contract checks this order on the
  compiled product (`PR-2 R12-F4 (round 14)` checks: drain offset before the readiness read and after the fence reads; UpdateCore's drain
  after the IsEnabled read and before UpdatePairs; no discarded drain result). The harness cases exercise the controller drain across a failed
  operation and its recovery (deferred target clear across a dismount, unfinished carrier, steering successor) and are now labelled as the
  controller drain: executing the real `SetEnabled(false)`, `UpdateCore` or `Dispose` needs the composition root's game services and was not
  possible offline.

**Validation of this part (metered).** Build-Local 32.3 s (DLL sha256 3b02ecea..., MVID c10f50de-a207-4194-a83a-73e890958627); attack-path
harness 498/0; native contract 66/0; lifecycle contract 103/0; components 1,545/0; persistence data 56 + Chunk7 106/0; Validate-Source 153/0;
seven read-only IL reads of about 1 s each. Not run: the focused gate, presentation contracts, Test-Tiers, any game.

**Residual.** Offline evidence only (nativeProof=false); N02/N03 remain the owner's playtest. R12-F4's entry points are verified by IL order,
not executed. The R13-F3 control was not run against the round-13 code (by inspection its Repath would have issued a second carrier).
`LiveLegAdmits` treats only a bit-identical start as the planned segment; a moved mount on a tightly placed destination is validated on its
live chord (correct, and it can refuse a destination whose disc begins exactly at the opening's measured start).

## 13. Round 15 (owner amendment revision 1, 2026-11-27g): the thirteen open findings

Prepared under owner amendments revision 1 sha256 aa3ab2f52199b92d03230c72d2fc9f43f813b176ae5c1ee7003e4f7926de1825. Line numbers are at this commit.
Everything the round-14 review verified is kept (R11-F3, R12-F1, R12-F3, R13-F4, R12-F5, R6-F7 and every earlier verified item; the accepted
R10-F2/R12-F6 amendment rebuttals). No change makes SingleAction into TwoActions, manufactures or refunds an action, forces Acting or alters game
time; native spending, explicit successor input, historical-format checks and the protected neighbours are unchanged. All evidence below is
offline (connected harness on the spliced production source, the real codec, IL contracts on the compiled product; nativeProof=false).

### 13.1 Movement and click

- **R13-F5 (correction).** `MountedMovementStateAdapter.TickDelegated` bounds every step tick by the installed endpoint completion before
  `Advance` (MountedMovementStateAdapter.cs:319-332; `StepEndpointCap`, :371-399): when the path's end lies farther than the physical step
  remainder, the shift (speed x tick) is limited to (distance to the end - 0.05 m - 0.1 mm) and to the distance to the last segment's endpoint
  plane, so installed TickMovement can neither snap the mount onto the end nor pass it; a remainder that covers the end keeps the lawful
  completion; ordinary movement and its NearTheEnd borrowed time are unchanged. Connected harness (faster turning step, 18/9 m/s, a second
  NearTheEnd tick 65 mm before the end): with 43.9 mm left the tick returns the capped duration, the mount travels about 14.9 mm and stays
  outside the 50 mm completion distance, the settled travel stays within 1.524 m, the next tick has no lawful movement (native's denial branch
  completes the movement) and the order ends with the force-finish disposition, no strike; the 70 mm control completes onto the end (65 mm)
  and strikes.
- **R14-F1 (correction).** `LiveLegAdmits` (PairedAttackClickProbe.cs:563) samples the live chord by its OWN distance (every 1 mm from its entry,
  both ends) and additionally checks the live points that project onto the planned chord's established samples; every point is explicit
  (position + u x leg), never `Vector3.Lerp`. The planned-leg shortcut needs both live endpoints bit-identical to the planned leg
  (PairedAttackClickProbe.cs:526-530). The harness facade's `Vector3.Lerp` now clamps like Unity's. Harness: the reviewer's oblique case
  (planned approach along x to the destination, live start destination + (-0.001, 0.2) m, blind patch at z = 35 mm between the ring samples)
  is refused before any debit with the prior order kept (`refusedOnLiveChordAt`); its visible control commits; the round-13 reverse leg past the
  truncated endpoint and the round-14 16-micrometre case pass with the clamping facade.
- **R12-F2 (correction).** `EvaluateFirstTickNative` (PairedAttackClickProbe.cs:421-475) evaluates, after the full-duration and
  waypoint-distance candidates, the interior durations of the current steered tick at the established 1 mm resolution of shift, each with the
  installed waypoint selection at that duration, its steering and its last-segment completion; the first admitted stop is returned with
  exactly that duration. Harness (sight z <= 0.052, frame, heading and endpoint fixed): 5/512 s stops at (0.04395, 0.04395) and strikes; the
  1/60 s + 0.001 s allowance returns a shortened interior duration (`ordinary-movement-shortened-strike(first-tick-interior(`), stops visible and
  strikes once.
- **R14-F2 (correction).** The grant carries its step-counter classification: `MountedMovementStateAdapter.RemainingOrderMetres(..., out
  stepCounterBinds)` (MountedMovementStateAdapter.cs:448-462) computes it from the SAME speed observations that price the grant;
  `PairedActivationLifecycle.PairedMountGrantedMetres` passes it out, the click's `MountGrantReader` (MountedCombatController.cs:1377) returns it
  and `AdmitTurnBasedAttackClick` uses it (:1427); `PairedMountStepCounterBinds` (the second read) is removed. Harness: MaxSpeed reads of the
  grant 2 = the whole click 2; a one-shot failure armed for the first read after the step's successful pricing (0.140625 m, equal speeds) is not
  consumed by the click, the click stays the provisional request, and the perpendicular first debit is refused before any movement with the
  generation, target, both Holds and both balances unchanged.
- **R14-F3 (correction).** `BeginProvisionalOrder` (MountedCombatController.cs:1700-1705) classifies a step request (`ClickProof.StepCounterBinds`)
  with the existing accepted-work rule (`CompetingMountWork`): compatible older mount work (an in-range Swift) coexists and the carrier's
  existing deferral path waits behind it; competing work still refuses; the restricted single tick keeps its idle-mount precondition; the
  before-debit decision still protects the prior order where the actual debit is final (`final-step-counter`). Harness: fresh equal-speed step,
  0.45 m route (several frames), older in-range Swift: the request stays pending (not retired), the Swift finishes, the click moves over several
  frames, commits at its first debit and strikes once; control: an older competing mount order refuses the step request with the prior order
  kept; the refused final step remains the R12-F1 perpendicular control.
- **R13-F3 (correction).** `MountedPairAttackCommand.EndIfNativeTurnBasedApproachEnded` (MountedPairAttackCommand.cs:1652-1684) resolves the end
  of the issued ordinary turn-based approach BEFORE the target-drift re-path (:1796) and again after this tick's drive (:1843). It applies to the
  override entry (route-bound with its validated route followed; also its live carrier whose agent no longer wants to move) and to the
  native-accepted rider click (a player-origin order without an override route whose carrier was issued; its finished carrier). Provisional
  requests, charges, real time and automatic orders keep their handling. Harness (`PathfindUnforcedMoves`: the agent takes native's open-fixture
  path for a move without a ForcedPath): (a) override route, the carrier ends blind and the target drifts 0.8 m on that tick: one carrier,
  force-finish disposition, Move unchanged afterwards, no strike, Standard unspent, the player's newer ground order owns the Move slot; (b) a
  native-accepted click whose carrier ends blind with a visible stretch beyond: the same. The accepted R1-F10 retarget (native-accepted click in
  the corner world) still strikes.

### 13.2 Ownership and custody

- **R14-F4 (correction).** `CommitAcceptedProvisional` stages both actors' manual targets once before the transfer
  (MountedCombatController.cs:2524-2531) and every later decision uses the staged values; the deferred effects are owned by their FLAGS
  (`FirstTickProposal.EffectsOutstanding`, :1570; the sweep keeps a request until its flags clear, :1599; registration from the flags, :1821);
  the first application is contained like the retries, and both Holds are written to the staged actors. Harness: a one-shot target read fault
  immediately after a successful transfer: the new generation stays accepted, the request stays retained from its flags with 0 listed
  effects, the retry clears only the retired target (control) and preserves a later same-target reassignment.
- **R11-F8 (evidence correction).** The harness splices the production custody: `PatchBridge.World`, `Owner`, `CombatFor` and `Pairs`,
  `MountedWorldService.Pairs` and the relationship's ownership members (`CommitOwnership`, `CompleteOwnershipAttempt`, `RefreshOwnership`,
  `RefreshRetiredOwnership`) over the real Domain `MountedPairRegistry` with the production `HasRetainedPairCommands`, and routes native Run
  through the production `InternalRunPostfix`. The intervening retry fails on a target READ, so the diagnostic list is empty while the clear is
  owed. Observed production behaviour: a dismount detaches (does not retire) the owner while the clear is owed, so custody still returns the
  pair; after the clear settles the owner retires and custody returns none (the roster entry stays). The same-target command on the original
  horse survives the retry; the control clears the old target and ownership settles. The controller's own dismount view is still the harness's
  `SimulateDismountForHarness`.

### 13.3 Lifecycle

- **R12-F4 (correction; the production sequence is executed).** The teardown order is extracted into `MountedServiceTeardownSequence`
  (Integration/MountedServiceTeardownSequence.cs): `BeginDisable` (SetEnabled(false): drain outside the fences, readiness, load fence,
  HandleModDisable), `RetryWhileDisabled` (UpdateCore's per-frame drain), `Unload` (Dispose) and `ModDisable` (HandleModDisable: drain,
  readiness, charge drain, cleanup, second drain). CompositionRoot (:240-263, :404-405, :458-488; adapters :508-533) and
  MountedLifecycleSubscriber.HandleModDisable delegate to it with the same services. `InterruptOwnedCommand` now retains the carrier cleanup
  even when the cancellation's own stop throws (`try { command.Interrupt(); } finally { RetainCarrierCleanup(command); }`): work created by the
  cancellation keeps its owner (the new case failed before this). Harness (`R12-F4/R14-F5 (round 15, production teardown sequence)`): deferred
  target clear, unfinished carrier, already-steering successor (path, owner, cursor and stops unchanged, no StopMoving) and a
  cancellation-created carrier stop, each through the real sequence with the controller and world drain: refusal with hooks retained, the
  UpdateCore retry, eventual `Ready` with hooks removed. Not executed offline: the Harmony unpatch, the UMM callbacks, the real
  `DrainForTeardown` wait, the relationship's other readiness terms and the Shield release (stand-ins; their binding is checked by the lifecycle
  contract's IL checks and Validate-Source).
- **R14-F5 (correction).** `MountedServiceTeardownSequence.Unload` checks `LoadInFlight` first: an unload during an owned load neither waits,
  drains nor cancels; Dispose logs and throws (Main.OnUnload reports false) and the root, hooks, observers and obligations stay. Harness:
  unload during the load with a healthy engine: `RefusedLoad`, zero drains and cleanups, target unchanged, hooks retained; after the load the
  same sequence settles.

### 13.4 Persistence and archive readers

- **R11-F1 (evidence correction).** The harness splices the production `PairedActivationPersistence` members (`CaptureParticipation`,
  `RestoreParticipation`, which rebinds the activation to the current turn, sets `partnerContext` from the saved partner turn and copies the
  restored allocation into it, and the actor capture helper). The archive participation is the production capture of the ACTIVE pair (Split
  false, current boundary; the grant identity is the activation's own; no substituted actor or grant ids); restoration follows the production
  order (`RestorePersistence`, then `RestoreParticipation`) and the click runs on the restored participation, whose `PairedMovementContext` is
  the restored partner turn (a new object). Recorded: original and restored pair, actor, activation, turn, round and grant identities; straight
  1.2/0.6 m (partner step field 0.6 m, grant 0.324 m, 0.3 m route admitted), turning 0.45/0.42426 m (grant 1.09974 m, 1.085 m admitted; the former
  single value gives 1.074 m), historical schema-3 (source 3 -> 4, 1.25 m), malformed (codec Invalid, nothing restored). Stand-ins: the native
  turn field copy and the participation discard reset (harness-labelled).
- **R13-F2 (correction).** `ReadCurrentRemovalArchive` and `ValidateCurrentRemovalArchive` (Diagnostics/RuntimePersistenceRemovalScenario.CurrentArchive.cs)
  require `MountedArchiveData.Schema` for the selected writer, source and normalized schema (a genuine historical schema-3 archive is read by its
  explicit reader and refused as not that writer's readback); the observation's source schema comes from the read result. The migration/resave
  (`Chunk7PersistenceScenario.Schema2`, two writer sites), `Schema1`, `Schema2Derivative`, `Chunk7ProgressionScenario`,
  `Chunk7WorldScenarioEngine` and `LoadRefusal` guards compare with `MountedArchiveData.Schema`; `CurrentMigration` derives `loadedSourceSchema`
  from its own read. The PowerShell combat evidence reader (`scripts/runtime/Chunk7CombatPersistenceEvidence.ps1`) accepts schema 3 or 4 with
  version-specific exact field sets (schema 4 requires `StepTravel` with exact, unique, finite rows covering every allocation; schema 3 must not
  carry it), derives the source schema from the recorded archive, and covers the Mounted Shield case through the same archive reader;
  `CurrentRemovalEvidence.ps1`, `PersistenceSaveFixtures.ps1` and `Chunk7LoadRefusalFixtures.ps1` accept the current version strictly. Reader
  tests (metered by the developer): current removal archive 49/0, current removal evidence reader 69/0, combat persistence evidence 41, combat
  reader 103 and observation 199 (0 failures; Mounted Shield schema-4 and schema-3 lines, 10 refusals, schema-4 complete artifacts), current
  migration native contract 59/0. Not run: `Test-Chunk7Schema2Resave.ps1`, `Test-Sprint6ShieldReaders.ps1`; readers outside the named set
  (capital arrival, owned pairs, assisted/ordinary fixtures, six-pair, the UPG-01 snapshot check) still expect schema 3 and were not changed.
- **R13-F1 (evidence).** `Chunk7PersistenceTests.RunStepTravelControls` is called by the attack-path harness (part of the focused gate), so the
  candidate log prints the valid two-allocation round trip, the decode refusal after removing one of two rows, the encode refusal, the schema-3
  historical read and the future-version control (`R13-F1 (round 15) the step-travel codec controls above ran on the candidate`).

### 13.5 Validation of this round (metered, on source commit 0bcb235a) and residual

Build-Local 32.7 s (DLL sha256 33b4072b88adb05ecf08db7c2b55ebcdb622f536f77393d767db188779c225c4, MVID 08f12895-05ab-4c40-b5d8-59e1ffe49645);
attack-path harness 532/0 (3.1 s); components 1,545/0; native contract 72/0; lifecycle contract 106/0; the focused selection
(`Test-MmrTbApproachFocused.ps1`, 16 children, 28.9 s: presentation 239/0, Mounted Shield 114/0, Archery 54/0, loader and UMM 0.31.1/0.33.0
bindings pass); persistence data 56 + Chunk7 106/0; Validate-Source 153/0. A discrimination run with the R13-F5 cap, the R14-F3 classification
and the R13-F3 early resolution disabled failed exactly those three round-15 cases (travel 65 mm against 43.9 mm left; the step request refused
behind the Swift; two carriers after the drift) and was reverted. Not run: Test-Tiers, any game.

Residual: offline evidence only (nativeProof=false); N02/N03 remain the owner's playtest. The R14-F1, R14-F2 and R12-F2 cases were not run
against the round-14 code (by inspection the round-14 sampler took only the endpoint sample on the oblique chord, the second speed read was
consumed by the click, and the shortened-stop search had no interior candidate). The step endpoint bound keeps 0.1 mm beyond native's 50 mm
completion distance and may end a step up to 0.1 mm short of a lawful position. The R11-F1 and R12-F4 cases keep the stand-ins named above.

## 14. Round 16 (owner authorization 2026-11-28a; owner amendment revision 1 unchanged)

Authority: owner instruction 2026-11-28a (`docs/missions/mmr-split/owner-authorization-2026-11-28a.md`, sha256
`4bf2995dc4124d63e479725d1cea77e0b0601cec4c86535bef559f39c713f442`). It resumes the round-15 stop to cap 18. OWNER AMENDMENT 1 (revision 1,
sha256 `aa3ab2f5...`) remains the selected amendment and is not replaced or strengthened; the accepted native baseline (R13-F4) is not
reopened. One correction pass covers all seven open findings. Three in-session developer agents worked in separate local worktrees (movement
tick; ownership and teardown; schema-4 readers). The lead inspected each production path and merged the branches. All evidence below is offline
(nativeProof=false); none of it is gameplay.

### 14.1 The executable current tick (R13-F5, R15-F1, R15-F2, R15-F3)

Invariant (owner, section 3): a replacement never commits on a stop that a later adapter adjustment prevents; the returned duration, the
waypoint native selects, endpoint behavior and the debit agree.

- **One production simulation of the installed tick.** `PairedAttackClickProbe.SimulateTick` returns the waypoint native selects at a given
  duration, skipping included (a follow-precomputed, non-last waypoint closer than m_Speed x that duration is skipped), the combat steering
  shift at MaxSpeed x the duration, and the endpoint completion (final plane passed, or within 0.05 m) only when the selected waypoint is the
  path's last. It serves the first-tick decision's waypoint candidates, its interior-stop search, and the step bound.
- **Step bound before the decision.** `MountedMovementStateAdapter.TickDelegated` resolves a step tick's permitted duration (the frame delta
  within the allocation's `Remaining`) to the longest duration whose settled stop keeps physical step travel within the remainder
  (`StepExecutableTick` / `StepExecutableDuration`, replacing the cursor-based `StepEndpointCap`). The gate receives that executable tick and the
  physical remainder (`IMountedFirstTickGate.Decide` gains `stepTravelLeft`); a candidate whose settled travel exceeds the remainder is not
  admitted. After a commit the approved duration is returned and debited unchanged (an alteration would be reported as
  `approvedTickAltered`; none occurs). A step's native-counter finality is judged for the tick actually executed.
- **Route check on every final debit.** The final ordinary tick and the step whose native counter becomes final now run the common
  `EvaluateRemainingRoute` check before their executable-stop decision (`final-ordinary-tick-` / `final-step-counter-route-arrival-not-admitted`),
  as the larger ordinary grant and the non-final step already did. The restricted single-move first tick keeps its executable-stop admission:
  its only movement is this tick's stop under the single-action restriction.

Connected cases through the real `TickDelegated` and the connected scheduler:

| Case | Result |
|---|---|
| Later step tick, reviewer's multi-skip polyline, 0.245 m left | returned 0.0133278 s (permitted 0.0136111 s); native selects interior waypoint #2; settled stop (0.198, 0.192), travel 0.169635 m, no snap; physical 1.279 -> 1.448635 m, native counter 0.762 -> 0.88195 m (round 15: snap, 1.579 m) |
| Interior corner (0,0) -> (0.30,0) -> (0.12,0.05), 0.10 m left | full 0.0055556 s returned; interior waypoint #1; stop (0.175, 0.075), 0.100 m; physical 1.424 -> 1.524 m; then native force-finish, no strike (round 15: no travel) |
| Provisional final-step click displaced to 65 mm, 43.9 mm left, sight only near the endpoint | refused before effects (`route-beyond-current-grant`, 0.065 > 0.0439); returned 0; both counters 1.4801 m unchanged; generation, manual target and both Holds unchanged; no strike |
| Affordable 40 mm endpoint snap | committed (`final-step-counter`), 0.0044444 s, stop on the endpoint, both counters 1.4801 -> 1.5201 m, one strike |
| Blind 2 mm stripe 35 mm before a visible endpoint 0.13 m away, 9 m/s, 1/60 s | refused before movement under the 0.14 m grant (final ordinary tick) and the 0.16 m grant (ordinary branch); visible-route control commits on the endpoint and strikes once under both; lawful shortened stop (0.035, 0.035) commits and strikes once under both |
| Same stripe, step whose counter becomes final vs step whose physical reservation binds | both refused before movement, counters 1.383375 m unchanged; visible controls commit and strike once |

The round-15 65 mm / 70 mm cases still pass. Discrimination: with round-15 behavior restored locally, the six new movement cases failed.
Limits: the multi-skip and corner geometries are injected into the agent's path (the engine stand-in); the 0.1 mm back-off applies at the
searched boundary; unreadable or non-combat steering gives a step no executable movement (conservative).

### 14.2 Native-accepted approach termination (R13-F3)

`MountedPairAttackCommand.EndIfNativeTurnBasedApproachEnded` ends both supported ordinary entries (override route and native-accepted click)
on a live carrier whose issued approach has ended, before the target-drift re-path. "Issued" means the exact carrier acquired its path while
it owned the mount's Move slot: either the agent held a Path other than the one recorded at issue (`NoteCarrierIssued` /
`ObserveCarrierPath`), or a request was seen in flight for that carrier (`WantsToMove` with no Path; 2.1.7b exposes no `RequestedPath`
getter) and later delivered, whatever the Path object (A* pools Path objects). The route-bound carrier also counts its displacement. A carrier
still awaiting its first path keeps the existing handling. Charge, automatic, real-time and provisional orders are unchanged; an
already-legal strike still strikes.

| Case | Result |
|---|---|
| Native-accepted click; carrier acquires a path and moves 0.188 m; path cleared while the carrier is unfinished; target drifts 0.8 m on the same frame | 1 carrier; native force-finish disposition; mount Move 0.0333 s kept; 0 strikes; rider Standard unspent; the player's ground order (through production `CompleteGroundCommandAdmission`) then moves the mount 0.5 m on its own path |
| Same, delivered Path is the pooled object held at issue | path acquired; 1 carrier; same disposition |
| Pending-first-path control (request in flight at the drift) | not acquired; no disposition; the existing re-path serves the order |

### 14.3 Successor preservation through production dismount cleanup (R12-F4)

Under `CleanupTrigger.ModDisabled` only (mod disable and unload), the pair yields a mount Move command it does not own:
- `MountedCombatController.HandleDismounting` calls `CancelCore(..., modDisableYieldsSuccessor: true)`. It releases the admitted rider-turn
  ground order's admission, owner and accounting without interrupting it (`ReleaseRiderTurnGroundMovementForModDisable`), and cancels mount
  movement through `KingmakerMountedPairRuntime.CancelMountMovement(CleanupTrigger)`, which keeps the non-owned successor and still interrupts
  any other `IsMoveUnit` command. (Without this, the cancellation interrupted the successor before the restoration ran; native `InterruptMove`
  interrupts every `IsMoveUnit` command.)
- `KingmakerMountedPairRuntime.RestoreMovementAuthority` consults `PreservesMountSuccessor` before stopping anything: a live non-owned mount
  Move command receives neither `InterruptMove` nor the mount view's `StopMoving`. Leases, rider placement beside the mount and the rider's own
  stops are unchanged.
- Ownership (`MountCommandOwned`, bound by the relationship): the combat owner's `OwnsMountMotionCommand` (exact paired native commands and
  carriers, door/carried/recovery moves, retained carrier cleanups) or the stick movement's bound or retained command. The admitted ground
  order is native's own command (ClickGroundHandler's `UnitMoveTo`, rerouted to the mount), so it is never counted as the pair's.
- Every other trigger, the player's ordinary dismount (`Manual`) and update-failure cleanup (`Exception`) included, still stops both actors.

Test path: spliced production `MountedServiceTeardownSequence`, `MountedLifecycleSubscriber.HandleModDisable` and its steps,
`MountedWorldService.Visit`/`SettleOutstandingPairCleanup`, `GameMountedRelationshipService.Dismount` and its ownership members, the real Domain
`MountedRelationshipCoordinator` and `MountedPairRegistry`, the runtime restoration and cancellation members, and the controller's
`HandleDismounting`, `Cancel`/`CancelCore` and retained-cleanup drain. Substituted: engine `InterruptMove`/`StopMoving`, the agent,
`Translocate`, lease acquisition, presentation attachment, Shield release and Harmony unpatch.

| Case | Result |
|---|---|
| Untracked steering successor (Path (0.45,0) -> (-0.05,0), cursor 1) | save-fence disable RefusedSaveOrOwners (0 drains), load-fence unload RefusedLoad, disable Ready (leases released, rider placed, AI restored), unload Ready (hooks removed); command, Path id and contents, owner, cursor, 0 interrupts and 0 stops unchanged at every step |
| Ground-routed successor (it is `activeRiderTurnGroundMove` before the first request) | same verdicts; the controller releases its tracking at disable (no retained pair command, readiness unaffected); command, path, owner, cursor and stops unchanged throughout |
| Failed mount stop in the restoration | RefusedResidue; enabled and hooks kept; recovery then Ready, unload Ready |
| Cancellation-created carrier stop | RefusedResidue; retried; unload Ready, hooks removed |
| Manual dismount control | the successor is interrupted (ordinary dismount unchanged) |

Retry ownership, stated plainly: on a refused attempt the registry owner retires (owners 1 -> 0, because registry teardown readiness does not
include the Faulted relationship). The retry is carried by the world roster, which still lists the pair, and by the Faulted coordinator's
movement-authority residue; the next request revisits the pair (`Cleanup` -> `CleanupOwner` -> `Dismount`), re-runs the restoration and settles.
Discrimination: with the fixes disabled, exactly the pooled-path, untracked-successor and ground-routed-successor cases failed. Limits:
re-enable after the release was not exercised; the real `TryAdmitGroundCommand`/`RouteGroundCommand` are stood in for by their admitted
outcome; native `IsMoveUnit` on a player `UnitMoveTo` is assumed set by TickApproaching, as modelled.

### 14.4 Schema-4 consumers (R13-F2)

The readers that consume this candidate's output share one version definition (`scripts/runtime/Chunk7PersistenceEvidence.ps1`:
`Get-KmcChunk7CurrentWriterSchema` 4, `Get-KmcChunk7PreviousWriterSchema` 3, `Assert-KmcChunk7WriterArchive` with exact versioned field sets).
Concepts kept distinct:
- the observed historical input keeps its own version (schema-1 `inputSchema` 1; schema-2 `inputSchema` and `loadedSourceSchema` 2);
- the normalized graph and every writer-output field (`nativeWrite.sourceSchema`, the written graph, `loadedArchive`/`migratedArchive`) equal
  the producer's `nativeSchema`, its build's writer;
- schema 4 requires `StepTravel` with exact, unique, finite rows covering every movement allocation; schema 3 refuses it and is accepted only as
  the previous writer's self-consistent evidence;
- a parent-child relationship requires parent writer = parent write = child writer.

Routes updated: the current-format migration child and its parent verifier (and the runner-side qualification of that parent), the owned and
legacy schema-1 migrations, the schema-2 migration projection (the codec's single-value `StepTravel` for writer 4), the owner-seed checkpoint
start and candidate writer fields, the ordinary and inherited persistence dispatch, and the capital arrival/cold, repeated-world, six-pair
persistence, rider-fall and world-scenario Manual readers. Readers of frozen or historical inputs (Finalize2 upgrade, stock mammoth,
progression, assisted, bootstrap and ordinary-acquisition fixtures) are unchanged.

New focused-gate child `scripts/chunk7/Test-Chunk7CurrentWriterReaders.ps1`. It compiles the candidate's `MountedArchiveCodec`, binds the
reader constant to `MountedArchiveData.Schema` and `SelectedWriterSchema` (4), re-encodes the synthetic fixtures with the production writer
and drives them through `Assert-KmcChunk7PersistenceObservation` and `Assert-KmcChunk7OwnerSeedCheckpointColdSource`: 89 pass / 0 fail, including
a schema-4 resave and cold child for two current-migration families through the dispatcher and parent verifier, schema-1 and schema-2
historical inputs migrated to schema-4 output, six protected ordinary/inherited current-output cases, schema-3 controls for all ten families,
and 67 refusals (missing, null or extra `StepTravel` fields, malformed rows, wrong-version graphs, historical relabelling, mismatched
parent-child versions). Every observation is labelled offline (`nativeInvoked=false`).

Other reader suites against the changed readers (metered): Mounted Shield readers 220/0; schema-2 resave 41/0; persistence evidence 41/0;
combat persistence evidence 103/0 and observation 199/0; schema-1 persistence 103/0; owned schema-1 evidence 56/0; repeated world 220/0.
Pre-existing failures, not caused by this round:
- owned schema-2 input 40/1 (`$f2Consumer` request-shape facade gap from 3a7f8a84; the same 40/1 at base 6410d150);
- capital arrival 162/3 (frozen Batch196-198 native evidence absent from the lab);
- owned two-pair reader 1,429/52, all 52 `Default-off native Charge refusal differs` in `Chunk7RealtimeEvidence.ps1:108`. That reader, its fixture
  JSON and the suite script are unchanged by this branch.
The six-pair suites stop at start-up on a missing frozen runtime-evidence file. Limits: no schema-4 gate cases for the capital,
repeated-world, six-pair and rider-fall families (their fixtures are suite-local); the runner-side parent qualification has no offline test.

### 14.5 PR-2 / PR-4 lifecycle composition

Recorded in `docs/missions/mmr-split/PR-2-PR-4-lifecycle-composition.md`: disable runs PR-2's safe pre-drain, readiness, load fence and
mod-disable cleanup, then PR-4's Mounted Shield release, selection rollback, disabled state, repair stop and remaining services. Enable and
failure cleanup stay as PR-4 has them; the per-frame retry and unload stay as PR-2 has them. A local probe of that ordering passed 23/0. This
round's R12-F4 changes sit below CompositionRoot, MountedServiceTeardownSequence and MountedLifecycleSubscriber, so the mapping is unaffected.

### 14.6 Validation (metered) and records note

Per stream, by the developers: movement harness 546/0, native contract 74/0, Validate-Source 153/0, Release build OK; ownership harness 525/0,
lifecycle contract 108/0, components 1,545/0, Validate-Source 153/0, Release build OK; schema child 89/0 and the reader suites above. On the
merged source (83d3fa93): attack-path harness, all 554 checks pass including the 22 round-16 cases. The candidate's focused gate (17 children
with the new reader child) is recorded in the developer report and the ledger excerpt. Not run: Test-Tiers Full, any game.

Records note: the cherry-pick of the ownership branch onto this branch left conflict markers in the harness template in commit 66079b32 (a
lead tooling error: a relative path resolved outside the worktree). 83d3fa93 removes them, keeping both added blocks; the stray empty file the
error created in the main checkout was deleted. No history was rewritten.
