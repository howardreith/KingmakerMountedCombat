# PR-2 first-tick replacement boundary (R1-F6)

Owner instruction 2026-11-27d, sections 3 and 4. This note records the boundary for R1-F6 and how it is implemented, checked against
the installed assemblies. The round-5 TwoActions workaround (`ExcludeFirstTickOnlySingleMove`, `SingleMoveTruncatesRestrictedMove`)
is removed, and the player's single-action selection is no longer rewritten. R5-F1, R5-F2 and R5-F3 are addressed in the same
candidate.

## 1. Installed per-frame order

Kingmaker 2.1.7b `Assembly-CSharp.dll` at the install path in `environment-intake.json`. Read by reflection. The checks are in
`scripts/Test-MmrTbApproachNativeContract.ps1`, under "PR-2 R1-F6 (round 6)".

1. `Game.Tick` (0x06000CBA) calls `GameMode.Tick` (0x06007E00).
2. `GameMode.Tick` calls each `m_Controllers[i].Tick()` in index order. `GameModesFactory.Create` (0x06007E07) builds that array from
   `AllControllers` with order-preserving `Where`/`Select`. Tick order is therefore registration order in
   `GameModesFactory.Initialize` (0x06007E08). The installed IL registers:
   `TimeController` (IL member 15) < `DefaultPointerController` (27) < `UnitMoveController` (65) < `UnitActionController` (121).
3. `TimeController.Tick` (0x060090CF) sets this frame's `DeltaTime` from Unity's frame delta. The value does not exist before that
   point.
4. `PointerController.Tick` (0x060093BA) runs the click: `TurnController.IgnoreClick` (0x06000C2F, where the admission postfix runs),
   then the click's handler, which reaches `TryRouteMountedStockAttack`.
5. `UnitMoveController.Tick` (0x06009183) passes `TimeController.DeltaTime` to each moving agent. `UnitMovementAgent.TickMovement`
   (0x060018AA) then calls `CanMoveInTurnBased` (0x060018A9). For the mount, the product's patch on that method reaches
   `MountedMovementStateAdapter.TickDelegated`.
6. `UnitActionController.TickCommand` (0x0600911E) starts commands. This runs after `UnitMoveController` in the frame.

Under the single-action limit, a restricted mount with no Move spent gets exactly one tick: `GetRemainingMovementTime` returns 0 once
`MoveAction > 0`. Step 6 means a carrier issued by a click first moves in a later frame. Steps 3 and 5 mean that frame's tick length
is known only at that tick.

## 2. Answers to the section-3 questions

- **When a proposal becomes a committed order:** at its own carrier's first `TickDelegated`, before `MountedMovementState.Advance`
  debits. Admission and the click handler do not retire anything:
  - `TryRouteMountedStockAttack` hands a click whose proof is marked `FirstTick` to `BeginFirstTickProposal`. The proof is marked
    when admission ran in `single-move-first-tick` mode. `CancelCore` is not called, and the intent, retained-order ledger and
    manual-target ownership are untouched.
  - The commit runs the normal acceptance path, `AcceptStockRequest`. That is the former tail of `TryRouteMountedStockAttack`,
    unchanged, and it retires the previous order once through `CancelCore`.
- **Native context and actual tick:**
  - The context is the mount's paired movement context, with the player's single-action limit still selected.
  - The allowance is `min(deltaTime, MountedMovementState.Remaining(...)) × speed`, computed in `TickDelegated`. `deltaTime` is the
    value `UnitMoveController` passes from `TimeController.DeltaTime`, and `Remaining` is given the same inputs `Advance` uses.
  - `PairedAttackClickProbe.EvaluateFirstTick` judges the exact stopping position of that one tick on the carrier's route, using the
    production strike admission:
    - **already in range:** commit in place, and the tick does not move;
    - **route end reached:** the proven arrival disc (`DiscAdmits`), because native may complete anywhere inside it;
    - **interior stop:** admission under the travelled segment's heading;
    - **stop past the first waypoint of a bent route:** refused.

    The click's admission uses the native ceiling (3 s × speed) only as a route-search bound.
- **State left unchanged on refusal:** `TickDelegated` zeroes the tick before any debit. The following stay as they were:
  - the previous order's generation, intent and manual target;
  - its carrier, or the absence of one;
  - the mount's Move cooldown and the rider's Standard.

  The provisional carrier is interrupted at the next `SweepFirstTickProposal`.
- **Binding:** the request (`MountedCombatController.FirstTickProposal`) carries:
  - the click proof: target, turn, frame, dispatch and click serial, route, arrival radius and plan identity;
  - the stock request;
  - the principal;
  - its own carrier (`UnitMoveTo` with the proof's `ForcedPath`).

  Its gate is bound on the adapter (`MountedMovementStateAdapter.FirstTickGate`). The gate applies only while the mount's Move slot
  holds that exact carrier. It is never stored on an allocation, and it is released when the request is retired, so it cannot reach
  later ground movement. At the decision, the plan, turn, target life, rider eligibility and absence of other mount work are all
  re-checked.
- **Retirement of the proposal:**
  - cancellation (`CancelCore` refuses and retires it);
  - a newer accepted click (through `CancelCore`) or a newer first-tick click (`BeginFirstTickProposal`);
  - the turn changing;
  - the target becoming invalid;
  - new rider work;
  - a plan mismatch;
  - the carrier ending before its first tick.

  Every case except cancellation and a newer click is settled by `SweepFirstTickProposal`. That runs from
  `TryDriveStockAttackIntent`, which the controller's per-frame `Tick` calls. While a request is pending, the previous order is
  neither retired nor advanced.
- **Why it cannot stall:** the request waits only for a carrier it installed itself (`BeginFirstTickProposal` runs it on the mount and
  checks that it holds the Move slot). That carrier's first tick decides the request. Otherwise the carrier ends, or the turn, target
  or rider changes, and the sweep refuses the request. Its liveness is that of any native carrier. In production,
  `TryOverrideMountTurnMovement` admits this carrier on the rider's acting turn, the same rule as the door approach.

## 3. A previous order holding a live mount command

If the mount holds any unfinished command or a queued one when the request arrives, the request is refused before it installs
anything (`mount-holds-live-work`). The previous order and its mount command are not touched, so two commands never coexist. At the
decision, other mount work that appeared in the meantime also refuses (`newer-mount-work`).

A previous order that has its own carrier cannot coexist with an eligible rider. Admission requires the rider's commands to be
empty, and a carrier is issued only by a wrapper in the rider's slots. The retained-work case therefore preserves "no carrier"
exactly.

## 4. Other changes in this candidate

- **R5-F1:** `LateralCandidates` merges the two tiers by distance. Each tier keeps `LateralTierShare` (three) queries while the other
  still has candidates. The cap of six and returned-heading validation are kept.
- **R5-F2:** `NativeOrdinaryAttackInput.Click(true)` binds its scoped dispatch to its own target and turn. A mismatching override
  keeps native's refusal (`NativeDecision:dispatch-target-mismatch`).
- **Other edits:**
  - `IsOwnDelegatedMove` and `ObserveAcceptedWork` treat the request's carrier as the pair's own, not as competing work;
  - `CancelCore` keeps the committing request's carrier;
  - the commit cuts the carrier's `ForcedPath` at the tick's stopping position, so it completes where its one tick ends. Native ends
    it on the next zero tick anyway.

## 5. Evidence and limits

- Harness cases (offline, through production members; the engine boundary is simulated):
  - `R6F6FirstTick`: 9 m/s at 1/60 s and 0.1 s, with 0.12 m and 1.2 m openings, for a fresh request and for a retained order;
  - the R5-F3 lines;
  - both initiative arrangements;
  - five-foot step, ordinary single move and spent mount Standard controls;
  - four no-stall cases, including cancellation followed by a ground tick.

  `R6F1Interleave` and `R6F2DirectTarget` cover R5-F1 and R5-F2.
- The retained-order fixture is an explicit order that native accepted while the turn was not acting. The production drive retains
  it undispatched. That turn state is simulated.
- Not shown natively:
  - the agent's motion when `ForcedPath` is replaced mid-move. The cut affects completion only;
  - same-frame command order between the committed wrapper's start and the carrier's completion;
  - `CanMoveInTurnBased`'s denial branch after the zeroed tick.

  These remain for the owner's playtest (N02/N03).
