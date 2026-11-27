# PR-2 first-tick replacement boundary (R1-F6)

Owner instruction 2026-11-27d, section 3. This note records the boundary for R1-F6, checked against the installed assemblies.

**Status: the boundary is established, but its implementation is not in this candidate.** The round-5 workaround
(`PairedActivationControls.ExcludeFirstTickOnlySingleMove`, which writes the TwoActions limit) is still in the source. Removing it
without its replacement would refuse every first-tick approach, which section 3 forbids. R1-F6 stays open, and so does R5-F3,
which needs this boundary's refusal path. Section 6 lists what is missing. R5-F1 and R5-F2 are fixed in this candidate.

## 1. Installed per-frame order

Kingmaker 2.1.7b `Assembly-CSharp.dll` at the install path in `environment-intake.json`. Read by reflection. The checks are in
`scripts/Test-MmrTbApproachNativeContract.ps1`, under "PR-2 R1-F6 (round 6)".

1. `Game.Tick` (0x06000CBA) calls `TimeController.TickRealTime` and then `GameMode.Tick` (0x06007E00).
2. `GameMode.Tick` calls each `m_Controllers[i].Tick()` in index order. `GameModesFactory.Create` (0x06007E07) builds that array from
   `AllControllers` with order-preserving `Where`/`Select`. Tick order is therefore registration order in
   `GameModesFactory.Initialize` (0x06007E08). The installed IL registers:
   `Game.TimeController` (IL member 15) < `Game.DefaultPointerController` (27) < `UnitMoveController` (65) < `UnitActionController` (121).
3. `TimeController.Tick` (0x060090CF) sets `DeltaTime = min(PlayerTimeScale * DebugTimeScale / 12, Time.deltaTime)` for this frame.
   Unity's frame delta is not known before this point. In turn-based combat, `PlayerTimeScale` also depends on whether the current
   unit is moving or running a command.
4. `PointerController.Tick` (0x060093BA) runs the click. It reads `TurnController.IgnoreClick` (0x06000C2F), where the product's
   admission postfix runs, then the click's handler. In the handler, `TryRouteMountedStockAttack` retires the previous order
   (`CancelCore`). That retirement is the destructive step.
5. `UnitMoveController.Tick` (0x06009183) passes `Game.TimeController.DeltaTime` to every moving agent's
   `UnitMovementAgentBase.TickMovement`. `UnitMovementAgent.TickMovement` (0x060018AA) passes it to `CanMoveInTurnBased(ref float)`
   (0x060018A9). The product patches that method (`MountMovementPrefix` → `TryOverrideMountTurnMovement` → `TickMountMovement` →
   `MountedMovementStateAdapter.TickDelegated`). The native body calls `TurnController.TickMovement` (0x06000C37) instead.
6. `UnitActionController.TickCommand` (0x0600911E) starts commands: in `Kingmaker.Controllers.Units`, it is the installed caller of
   `UnitCommand.Start`. An order's wrapper therefore starts here, and so does the carrier it issues. Both happen after this frame's
   `UnitMoveController.Tick`.

Under the single-action limit, a restricted mount with no Move spent gets exactly one tick: `GetRemainingMovementTime` returns 0 once
`MoveAction > 0`. That tick's length is the `DeltaTime` of the frame in which the carrier first moves. Step 6 places that frame after
the click frame. Steps 3 and 5 make its length known only once that frame's `TimeController.Tick` has run.

## 2. Consequence for the boundary

No boundary in the click frame knows the first tick's length. Admission at `IgnoreClick` and the handler's retirement both happen
before the carrier exists. The click frame's own `DeltaTime` is the wrong value: the carrier first moves in a later frame, with a
different Unity frame delta and possibly a different time scale.

The smallest boundary that knows the real allowance before any debit is the carrier's first `TickDelegated`. Its `ref deltaTime`
is that frame's `TimeController.DeltaTime`, before `MountedMovementState.Advance` debits. A per-frame hook between
`TimeController.Tick` and `UnitMoveController.Tick` (a `UnitMoveController.Tick` prefix) knows the same value. It does not help,
because a carrier committed there cannot start until `UnitActionController` runs later in that frame.

Retirement must therefore move to that tick. The replacement has to exist as a provisional request with its own carrier while the
previous order stays intact.

## 3. Answers to the section-3 questions (for the design in section 6)

- **When a proposal becomes a committed order:** at its own carrier's first `TickDelegated`, when `deltaTime * speed` reaches
  the earliest strike-admissible arc length on the proven route. That length comes from the existing walker's arrival disc, not the
  centre of the route. The previous order is retired there, before `Advance` debits.
- **Native context and tick:** the mount's paired movement context (`PairedMovementContext`), with the player's single-action
  limit still selected. The tick is the `deltaTime` that `UnitMoveController.Tick` passes from `TimeController.DeltaTime`.
- **State left unchanged if admission fails:**
  - the previous order's generation, intent, ledger entry, manual-target ownership, wrappers and carrier identity;
  - the mount's Move cooldown, because the refused tick is set to 0 before `Advance`;
  - the rider's Standard.
- **Binding:** the proposal carries:
  - the click proof: target, turn, frame, dispatch and click serial, and plan identity;
  - the activation and pair;
  - the movement mode read on the mount's context;
  - the carrier's identity.

  Its carrier is revalidated by the existing `RevalidateAdmittedClickRoute`.
- **Retirement of the proposal:** the following retire the proposal as refused and release the provisional state bound to the
  carrier:
  - cancellation;
  - a newer click (a new proof replaces it);
  - turn end;
  - target invalidation;
  - a plan or route mismatch at revalidation;
  - the carrier ending before its first tick.
- **Why it cannot stall:** the proposal waits only for a carrier it has itself installed. Every terminal path of that carrier
  decides it. Nothing waits for a carrier that would only be installed after the commit.

## 4. Why the click-time variants fail

- A ceiling (round 4) or TwoActions (round 5) does not honour the selected native limit.
- Any fixed frame length is a guess. Unity frame deltas have no lower bound.
- Refusing every first-tick approach is forbidden. It would also refuse the 0.12 m opening at 1/60 s (0.15 m tick).
- Waiting for `TickMountMovement` with no installed carrier never ends. `TryOverrideMountTurnMovement` runs only for a live carrier.

## 5. What this candidate changes

- **R5-F1:** `PairedAttackClickProbe.LateralCandidates` merges the two tiers by distance. Each tier keeps
  `LateralTierShare = MaxLateralQueries / 2` queries while the other still has candidates. The six-query cap, the returned-heading
  validation and the heading-independent flank (R4-F3) are kept.
- **R5-F2:** `NativeOrdinaryAttackInput.Click(true)` opens its scoped dispatch bound to its own target and the current turn
  (`MountedPatchController.BeginScopedInputDispatch(target, turn)`). `AdmitPairedAttackClick` keeps native's refusal when the
  highlighted target or the turn differs (`NativeDecision:dispatch-target-mismatch`). Native `PointerController.Tick` binds no target.
- **Contract:** six new installed-IL checks for the order in section 1.

## 6. What is missing for R1-F6 and R5-F3

1. A provisional path in `TryRouteMountedStockAttack` for first-tick-dependent proofs that does not call `CancelCore` and does not
   overwrite `stockIntent`, `retainedOrder` or manual-target ownership. The previous generation stays current until commit.
2. The provisional wrapper and carrier are exempt from competing-work supersession (`ObserveAcceptedWork`, `RetainedOrderSuperseded`).
   Otherwise installing them would retire the previous order.
3. `BeginDelegatedMove` binds the required arc length to the carrier's identity.
4. `TickDelegated` decides at that carrier's first tick:
   - **commit:** retire the previous generation through the existing retirement helpers, leaving the provisional wrapper untouched,
     then promote the provisional generation;
   - **refuse:** set `deltaTime = 0`, end the provisional wrapper and carrier, and leave the previous order as it is.
5. Every other terminal path settles the proposal as refused.
6. The case where the previous order holds a live mount command, which installing the provisional carrier would displace, needs a
   decision before installation.
7. Harness cases:
   - 9 m/s at 1/60 s and 0.1 s, with a 0.12 m and a 1.2 m opening;
   - a fresh request and a retained previous order;
   - pending requests reach a decision;
   - cancellation followed by ground movement;
   - both initiative arrangements, the five-foot step, ordinary movement and the spent-Standard controls;
   - R5-F3's retained previous order with the rider still eligible.

   They should replace `R5F6Invariant`. `PairedMountGrantedMetres` should return the first-tick requirement, not its invariant guard.

These changes alter the retained-order lifecycle accepted in PR-1. They were not made within this task's validation allowance.
