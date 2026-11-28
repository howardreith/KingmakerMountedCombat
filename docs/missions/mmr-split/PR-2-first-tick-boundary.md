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
