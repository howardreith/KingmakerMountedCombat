# PR-2 first-tick replacement boundary (R1-F6)

Owner instructions 2026-11-27d and 2026-11-27e (sections 3 and 4). This note records how the restricted single-action approach is decided
and how a click reaches that decision, checked against the installed assemblies. Round 7 replaces round 6's bare mount carrier with a
**provisional order on the rider** (R1-F6, R5-F3, R6-F1..R6-F7). The round-5 TwoActions workaround stays removed; the player's single-action
selection is never rewritten. R5-F1 and R5-F2 are unchanged.

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

1. **Click routing.** `MountedCombatController.TryRouteMountedStockAttack` (MountedCombatController.cs:1944) takes the click's proof. A proof
   admitted in `single-move-first-tick` mode (`proof.FirstTick`) that does not continue the retained order's own target goes to
   `BeginProvisionalOrder` (:2043 -> :1525). Nothing of the previous order, manual target or Hold is touched; native Run of the click's
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
   `DefersClickTargetAssignment` (:1803): the click's own assignment of B to the rider is deferred for a pending provisional order of that
   exact click, and suppressed for one that click refused. `NativeRunHoldWrite` (MountedPatchController.cs:1085) asks `NativeRunKeepsHold`
   (:3163): the Run of the provisional command (rider) or of its carrier (mount) keeps the Hold and records that it did.
4. **Native eligibility and phase.** The rider's own command fills the turn's `m_Commands`, so the installed Tick moves a Preparing turn to
   Acting. The wrapper is `activeCommand`, so `NativeActorEligibleForCommand` (PairedActivationLifecycle.cs:747) admits its carrier through
   `OwnsExactPairedNativeCommand` (MountedCombatController.cs:214, `activeCommand.DelegatedMove`). Nothing globally admits off-turn mount
   commands, forces Acting or grants an action.
5. **Carrier and gate.** The wrapper starts (UnitActionController) and, needing an approach, calls its own `BeginDelegatedMove`
   (MountedPairAttackCommand.cs:1859). Its carrier follows a route revalidated from the **current** origin by `RevalidateAdmittedClickRoute`
   (:1819), which marks the approach `FirstTick` when the grant reason is `single-move-first-tick` (the grant is then only the route-search
   ceiling). Only after the carrier holds the mount's Move slot and its ownership proofs passed is `FirstTickCarrierIssued` invoked
   (MountedPairAttackCommand.cs:1966) -> `BindFirstTickCarrier` (:1575), which binds the request's gate to that exact carrier. Every later
   carrier of the same command (a replan) is bound to the same request: a pending one decides it, a refused one refuses it until the sweep.
6. **Movement hook.** `UnitMoveController` -> `MountMovementPrefix` (MountedPatchController.cs:2055) -> `TryOverrideMountTurnMovement`
   (:3273; the provisional order is the active attack, the turn is Acting) -> `UnifiedMountedTurnCoordinator.TickMountMovement`
   (UnifiedMountedTurnCoordinator.cs:631) -> `MountedMovementStateAdapter.TickDelegated` (MountedMovementStateAdapter.cs:223, gate at :230).
7. **First-tick decision.** The allowance is `min(deltaTime, Remaining) x speed`, before any debit. `DecideFirstTickProposal` (:1619) rechecks
   from live state: the turn, mount addressability, that the provisional order is still the active command in the rider's Standard slot,
   that the carrier still holds the Move slot, no other mount work, target validity under the **click-time** target-life fact, and the
   rider's entitlement through the dispatcher's own `RiderMeleeClickAdmitted` (Standard, action eligibility, attack capability; the
   provisional command counts as the pair's own transaction). `PairedAttackClickProbe.EvaluateFirstTick` (PairedAttackClickProbe.cs:196)
   judges the tick from the mount's **current position** toward the carrier path's **next waypoint** (points within 1 mm are passed, as the
   mover passes them): travel = min(allowance, distance to that waypoint), so a tick never crosses a waypoint. In range: commit in place;
   the route end reached: the proven arrival disc; otherwise the interior stop under the travelled heading. On commit the carrier's path is
   cut at the stop, so the tick's actual displacement is the predicted one.
8. **Commit.** `CommitProvisionalOrder` (:1687) -> `AcceptStockRequest(..., committing)` -> `CommitAcceptedProvisional` (:2118). Only the previous
   **intent** is retired: the provisional command is detached from the active slot for that one `CancelCore` and restored after it. Intent B
   starts with the click's facts (its target-life fact), the retained-order ledger records the provisional command and its exact carrier as
   B's own, the supersession check and own-move announcements are wired, the deferred manual-target write and Hold clears are applied once
   unless the player assigned a newer target (then the newer selection is kept), and `stockIntentCommand` is the provisional command.
   Success is read from the actual state (`HasStockAttackIntent`, a new generation that owns B, `stockIntentCommand == activeCommand ==` the
   command, ledger current), never from a routing return value. A failure before retirement leaves everything as it was and refuses; a
   failure after the effects is judged by that same state (the round-7 case shows it committed).
9. **Refusal.** The tick is zeroed before any debit. `SweepFirstTickProposal` (:1766), from `TryDriveStockAttackIntent` (:2731) in the same
   frame's controller update, ends the exact provisional command (`InterruptOwnedCommand`) and its exact carrier (only if it still holds the
   Move slot), clears the active slot, releases the gate and lets the previous order resume. A refused request is never revived; repeated
   sweeps change nothing and never touch newer work.
10. **Ownership, release and strike.** After a commit the order owns the exact carrier as its `delegatedMove`; its `TickApproach` stops and
    removes that carrier at the admitted position (or settles an already finished one) before `StartChildAttack` (MountedPairAttackCommand.cs:2129),
    in either processing order. A provisional order that reaches its strike without a decided tick (no carrier needed, or the target came into
    reach) is committed only in place (`ProvisionalStrikeGate` at :2137 -> `AdmitProvisionalStrike` :1718), otherwise it ends unstarted.

## 3. Retirement, newer clicks and accepted orders

- Every newer accepted click retires a pending provisional order and its exact carrier first (`RetirePendingReplacement`, :2057), the
  same-target `Continue` branch included (R6-F5). A cancellation does the same (`CancelCore`, :1866). The proposal's own commit is the only
  acceptance that keeps it.
- A pending request is also settled when its command ends, is replaced in the active slot, its carrier is lost, its turn changes, or its
  target becomes invalid under the click's fact.
- **R6-F4.** An accepted route-bound order whose carrier is created (or replanned) after the mount became first-tick-only gets the same gate
  (`BindFirstTickCarrier`, ordinary kind): an affordable tick moves and is debited normally; an unaffordable one is zeroed and the order's
  wrapper is ended by the sweep (its terminal retires the intent unexecuted). A newer click's `Continue` keeps that gate with its carrier.

## 4. The retained-order fixture (R5-F3)

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
- Not shown natively (owner playtest N02/N03): the agent's motion when `ForcedPath` is replaced mid-move; the registration order of the turn
  controller relative to `UnitActionController` (the harness ticks the turn first); whether native `CanMoveInTurnBased`'s denial branch ends a
  zeroed carrier (the replan case covers both outcomes); and the turn being Acting after a refused provisional order (no action is spent, as
  for an unmounted rider whose issued attack is interrupted).
