# Known issues remediation, Sprint 6 — attack requests, terminal causes, Serenity/Fascinate, automatic churn (2026-11-18)

Sprint 6 of the Mounted Combat Known Issues Remediation Epic: A01–A04, and A05 found on the way. Offline engineering
only: nothing was launched, staged or installed, and nothing here is native gameplay evidence. Sprint 7 was not started.

**Corrected after the review of `2ae4cc8b` (2026-11-18).** The first checkpoint was not accepted. Its five findings are
corrected below (see "Review of 2ae4cc8b"), and the full Q1–Q17 mutation campaign ran on the corrected head. Two claims
of the first checkpoint are withdrawn:
- that A05 recorded only "genuinely failed" approaches (it also accepted `Unrecorded` and `TargetInvalid` ends, and missed
  real approach failures, which ended as `Fault`);
- that the end causes were complete (start faults, charge ends, queued automatic refusals and the scheduler recorded none).

## Identity

| Item | Value |
|---|---|
| Base | `772543e19bfbe827c175c22fb8b3c81dbb1b44e8`, the accepted Sprint 5 checkpoint (draft PR #50, unmerged; verified as the live branch head, parent not advanced). Accepted product build `cdfbe509` |
| Branch and worktree | `codex/kmc-known-issues-sprint6`, `worktrees/known-issues-sprint6` |
| Draft PR target and parent | `codex/kmc-known-issues-sprint5` (parent PR #50) |
| Commits | `3472914d`: A01, A02, A04, the first A05 rule, Sprint 5 test wording, tests and the probe extension. `221d748b`: the automatic-only pin. `2ae4cc8b`: the first records (reviewed head). `ce9b1383`: review of `2ae4cc8b`, findings 1–5 (the product-build commit). Then the records commit carrying this version, which is the branch head |
| Product-build commit | `ce9b1383` |
| Product DLL (clean Release build of `ce9b1383`) | SHA-256 `3e20be03d913c2e7ba1a6a207b8076f018c08b0af6d54b72416d948009b666ae`, MVID `6b47a46b-8fad-40ac-824e-5a3c50f23ba1`; frozen copy `artifacts/known-issues-sprint6/review/head-ce9b1383/` |
| Reviewed DLL (`2ae4cc8b`) | SHA-256 `27579ff8870c5961663c860cdcf60fa8349069de05746bf43d028488db312936`, MVID `e8d32009-c6c5-4aba-908b-11dae9954a31`; frozen copy `artifacts/known-issues-sprint6/head/` |
| Budget | `known-issues-sprint6` |
| Preserved | PRs #45–#50, their branches, the main checkout and every other worktree |

## Dispositions

| ID | Disposition | Summary |
|---|---|---|
| A01 | **Implemented and verified offline** | A same-target click after the dispatched command ended, before the next Update, continued finished work and inherited its dispatched state, so a spent actor's click got no answer. It is now a fresh request (a new generation and its own answer) through the existing replacement path. A click while the command is live still continues and promotes it. |
| A02 | **Implemented and verified offline** | The terminal text read the actor's capability snapshot whatever ended the command, so Stop, a replacement, a boundary or a fault beside a blocked actor read "cannot act". Each end now records a typed cause; the snapshot explains only a capability-caused end. Terminal logging and warning delivery are contained: a failing logger no longer stops retirement. |
| A03 | **Settled explanation; no product change** | Exact asset evidence: "Fascinate" from the Lady of Shallows is `FascinateEffectCombatBuff` (`8a59ee05…`), applied each round by her `NereidBeguilingAura` on a failed Will save. It carries `AddCondition: Dazed`, which makes `UnitState.CanAct` false, and the current reader handles it. Serenity (`SerenityBuff` `0a90088d…`) applies no condition: it damages its owner when the owner makes an attack roll. Live state was not observed. |
| A04 | **Established and corrected (automatic only)** | In real time a rider with `CanNotAttack` and AI on had every brain proposal (about every 0.3–0.4 s) admitted, then interrupted by native `UnitAttack.ShouldBeInterrupted`, two log lines a cycle. The automatic admission now reads the condition, with no state. The player's real-time wait is intentional and unchanged. |
| A05 (new) | **Implemented and verified offline** | Found while reconciling A04. Any non-success automatic end with no strike was recorded as a failed approach (target death, a capability, a cancellation, an in-range interruption), so the rider's brain kept its target unreachable. Now only the typed `ApproachFailed` cause, recorded at the exact movement boundary and carrying the approach's own evidence, records it. |

## Reconciled lifecycle at the accepted base (before any edit)

| Step | Accepted Sprint 5 source |
|---|---|
| Hostile-click provenance | The native `OnAttackRequested` is observed for pair actors and bound in the first step of the clicked actor's `UnitCommands.Run` (`ObservedPlayerAttackRequest`). |
| Routing | `TryRouteMountedStockAttack` refuses native `Run` for every exact stock `UnitAttack` of a mounted pair. Brain proposals go to `ObserveAutomaticProposal`, and inexact clicks are refused with one answer. A same-target click with the same turn, actors, weapons and action continued the retained intent (`CanContinue`, then `PromoteToPlayer`, no new generation). Anything else ran `Cancel` and then `Begin`. |
| Intent | `MountedAttackIntent`: `Begin` increments `Generation` and clears `hasDispatched`; `ObserveDispatch` sets it; `GetRejectionFeedback` and `GetDispatchRejectionFeedback` answer only a request that never dispatched; `Cancel` clears everything. |
| Drive | `TryDriveStockAttackIntent` runs each controller Update and dispatches through `TryExecuteAction`. In turn-based combat, an actor without a Standard completes the intent with `GetRejectionFeedback`, which was silent after dispatch. |
| Terminal | `HandleCommandTerminal`: `MountedAttackTerminalPolicy.Classify`. Executed work (Completed, RangedTailDelivered, DeliveredWithoutCompletion) retains the intent and releases the command reference. EndedWithoutExecution retires it and answers a player order once with `UnexecutedFeedback`. Failed retires it. Feedback came from `DescribeUnexecutedTerminal` or `DescribeTerminalFailure`, both of which used the capability snapshot whenever it was present. |
| Cancellation | `Cancel(reason)` (Stop or Hold through the native Stop/Hold prefixes, a new stock click, a ground order, dismount, boundaries), `TryRetireStockIntent`, the orphan sweep, the door replacement and the charge admission. Each cleared the intent and called `Interrupt`; the reason reached only the scheduler and the log. |
| Automatic | `MountedAutomaticAttackPolicy.Decide` (origin, pair, real time, rider brain, AI on, Stop gate, target, Hold reach, competing operations). An admitted proposal becomes an automatic intent. A non-success automatic terminal with no strike recorded its target in the rider's `AIData.UnreachableUnits`. |
| Modes | Turn-based player orders are refused for a blocked actor (`AdmissionBlock`). Real-time orders are left to native, which keeps a blocked actor's order pending, except that `CanNotAttack` is interrupted. |
| Warnings | `ShowStockFeedback` raises one `IWarningNotificationUIHandler` warning. Terminal lines go through `logger.Info`, uncontained before the intent was retired. |

## A01 — the fresh-request boundary

- **Rule (production, `MountedAttackIntent.IsFreshRequestAfterTerminal`):** an exact same-target click continues the
  intent only while its request has not dispatched, or while the command it dispatched is still live
  (`stockIntentCommand != null && !IsFinished`). Otherwise it is a fresh request: `Cancel` and `Begin` (a new
  generation, `hasDispatched` cleared), then the ordinary drive.
- **Binding:** it is bound to the exact command lifecycle. There is no timer, frame or stored freshness. The
  controller's own `ContinuesStockIntent` asks it.
- **What a fresh request does:**
  - a spent actor gets one no-Standard answer;
  - an actor with a genuine new action dispatches normally;
  - nothing is replayed, revised, refunded or restored.
- **Before and after:** the compiled controller, detached (`Test-MountedAttackStallRepair.ps1` section A01).

| Case | Accepted Sprint 5 build | Sprint 6 build |
|---|---|---|
| Completed single attack, same-target click before the Update | continuation, silent | fresh request; new generation; one answer |
| Completed full attack, mount attack, ranged tail | continuation | fresh request; the mount judged on its own action |
| Click while the command runs | continuation | continuation |
| Automatic intent promoted by a click; later click after its terminal | continuation | promoted while live; fresh after the terminal |
| Two pairs | — | one pair's terminal never changes the other's live continuation |
| After a boundary clears the order | new request | new request; nothing about freshness stored |
| Retarget, another action or weapon | new request | new request (unchanged) |
| After the next ordinary Update | the drive quietly exhausted the retained order, then a new request | the same player-visible result as before the Update: one answer |

## A02 — terminal cause apart from the capability snapshot

- **Cause (production, `MountedAttackEndCause`):** recorded where the command is ended. Values:
  - `Cancelled`: the controller's own ends (Stop or Hold, a new order, dismount, the load, area and combat
    boundaries, retirement, the orphan sweep);
  - `Replaced`: a door interaction or charge admission, or native `Run` ending the command inside a newer order on the
    same actor (the token-exact scope of the public `UnitCommands.Run`; see "Exact native-Run scope" below, which
    replaced the first checkpoint's prefix/postfix scope);
  - `ActorCapability`: `TerminateIfActionActorBlocked`, with the exact block it read;
  - `Fault`;
  - `ApproachFailed`: the exact movement boundary (added after the review);
  - `TargetInvalid`;
  - `Unrecorded`: native's own end.
  - The full path list is the cause-path audit under "Review of 2ae4cc8b".
- **Capability as cause (`MountedAttackTerminalCausePolicy.CausalCapability`, corrected after the review):**
  - for `ActorCapability`, the block recorded at the boundary, whatever the later snapshot;
  - with nothing recorded, the snapshot only on native's installed signatures: an unstarted turn-based Success
    force-finish, a `CanNotAttack` interruption, or a `CanAct`-term interruption of a running command;
  - never beside any other recorded cause.
- **Feedback:**
  - Stop or a boundary: "Mounted pair attack cancelled." (code `CommandCancelled`);
  - a replacement: "Mounted pair attack replaced by a newer order.";
  - a capability-caused end keeps "X cannot act right now (…)";
  - completed and delivered work never carries a failure.
  - Failed terminals raise no warning, as before. An unexecuted player order still warns once.
- **Diagnostics:** the outcome keeps the raw result, transaction state, reason, capability block and capability
  details, and adds the cause, the recorded causal block and whether the capability was the cause. Nothing native is
  written.

| Case | Accepted Sprint 5 build | Sprint 6 build |
|---|---|---|
| Stop while dazed | "Royon cannot act right now (dazed). The attack was not performed." | "Mounted pair attack cancelled." |
| A replacing order while stunned | "cannot act (stunned)" | "replaced by a newer order" |
| A boundary while helpless | "cannot act (helpless)" | "cancelled" |
| A fault beside a stunned actor | "cannot act (stunned)" | the fault's own text |
| Target death beside a dazed actor | target text | target text (unchanged) |
| Daze during the approach | "cannot act (dazed)" | unchanged |
| Native unstarted Success, dazed | "cannot act (dazed)", one warning | unchanged |
| Native `CanNotAttack` interruption | "cannot attack" | unchanged |
| A failing logger at the terminal | the exception escaped; the order stayed retained | the order retires; ownership is released; nothing escapes |

## A03 — Serenity and Fascinate (exact asset evidence)

- **Evidence:** the installed `sharedassets1.assets` was decoded in place, every object listed decoding to its
  recorded size. The summary is in `artifacts/known-issues-sprint6/01-a03-blueprint-evidence.txt`; nothing proprietary
  is in the repository.
- **The enemy:** "Lady of Shallows" is `NereidLairBoss` (`ac131b01…`).
- **Fascinate:**
  - Her facts include `NereidBeguilingAuraBuff` (`ab6181f9…`). Each round, its area effect gives qualifying male (or
    attracted) enemies a Will save.
  - On a failure it applies `FascinateEffectCombatBuff` (`8a59ee05…`, displayed "Fascinate"), with
    `AddCondition: Dazed`. It is removed by incoming damage (granting immunity) and at combat end.
  - `FascinateCommonBuff` (`9c70d2ae…`) is also Dazed, but its appliers belong to other creatures.
- **Serenity:**
  - Her class levels select Serenity (`d316d3d9…`). On a failed Will save it applies `SerenityBuff` (`0a90088d…`).
  - That buff's only component triggers on the owner's attack roll: a one-round cooldown and magic damage. No
    condition, state flag, command or attack restriction.
- **Native consumers (installed IL, pinned):**
  - `AddCondition.OnTurnOn` → `UnitState.AddCondition`;
  - `CanAct` requires `IsAble`, which is false under Dazed;
  - native's turn-based tick force-finishes an unstarted command whose executor cannot act, which is the reported
    terminal signature (native Success, transaction cancelled, nothing delivered).
- **Disposition:** settled explanation. The condition that stops the rider is `UnitCondition.Dazed` from Fascinate,
  and `NativeActorCapability.Read` reads it, so the stall repair already refuses or retires it. No product change.
- **Remaining gap:** Royon's live buffs, his saves and the original `output_log(8).txt` (not on this machine) were not
  observed. The identification rests on the boss's exact content and the reported names, not on his recorded state.

## A04 — automatic proposal churn (established; corrected)

- **Native AI (installed and decoded):**
  - The party brain `Character_Brain` (`cf986dd7…`) has two attack actions:
    - `ManualAttackAiAction` (`a4a34d4a…`): runs even with AI off, manual target only;
    - `AttackAiAction` (`866ffa6c…`).
  - Neither has an actor consideration that excludes a `CanNotAttack` unit.
  - `BlueprintAiAction.IsTargetValid` only limits unreachable targets to reach and line of sight.
  - `AiBrainController.SelectAction` sets `NextCommandTime` to 0.3–0.4 s after each proposal.
  - Native `UnitAttack.ShouldBeInterrupted` interrupts every attack of a `CanNotAttack` executor.
- **At the base:**
  - `Decide` had no capability input. With AI on in real time, each proposal became an automatic intent and a pair
    command that native interrupted, with two log lines a cycle.
  - The terminal also marked the target unreachable (A05). That stops churn for out-of-reach targets, wrongly, but
    not for a target already in reach.
- **Correction:** `MountedAutomaticProposalFacts.RiderCannotAttack` is read fresh from the rider at each proposal, and
  `Decide` refuses "rider-cannot-attack".
  - It holds no state, so a cleared condition, a new target, a player order, Stop, a pair generation, dismount, combat
    end, a load or area boundary, disable or disposal need no reset.
  - Player orders never pass `Decide`; the click route never reads the condition (pinned).
- **Before and after:** thirty proposals with `CanNotAttack` were admitted 30/30 at the base and 0/30 now; the next
  proposal after the condition clears is admitted.
- **Controls:**
  - healthy automatic attacks are admitted;
  - Stop, AI off, turn-based and a live intent keep their own outcomes;
  - player real-time orders keep their native wait;
  - a player `CanNotAttack` order follows native's interruption, unchanged.

## Validation (budget `known-issues-sprint6`), corrected head

| Check | Result |
|---|---|
| Clean Release build of `ce9b1383` | PASS; identity above (log `review/05-clean-release-build.log`). The Fast tier's own rebuild is byte-identical |
| Components | 1395/0 (1387 at the base; Sprint 6 tests: A01 freshness, shapes and counter; A02 causes and the durable causal block; A04 admission; A05 rule; native Run scopes) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MountedAttackStallRepair.ps1` (compiled controller, detached) | **136/0** on the candidate (log `review/02-stall-probe-head-ce9b1383.log`) |
| The same probe on the accepted Sprint 5 build | 10 Sprint 6 sections fail (log `review/04-stall-probe-sprint5-cdfbe509.log`) |
| The same probe on the reviewed `2ae4cc8b` build | the 6 review sections fail: A05 marks, causal block, fault delivery, counter, Run scope, wiring (log `review/04b-stall-probe-reviewed-2ae4cc8b.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint6` | PASS 13/0 (0.54 min; budget 1.18 of 90 min over 2 entries) (log `review/06-fast-tier.log`) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `review/07-assembly-contracts.log`) |
| Mutation campaign Q1–Q17 | **17/17 caught** (log `review/03-mutation-campaign.log`; details under "Review of 2ae4cc8b") |

- **Fast tier coverage:** stall repair, command slot, native input semantics, turn-completion wrappers, paired
  activation, marker ownership, ranged movement, continuous drive, persistence, world ownership.
- **Not run:** Full, Candidate, historical campaigns, release qualification.
- **Stale harnesses, identical at the base:**
  - `Test-OrdinaryAttackControls.ps1` needs a live deployment helper that is absent here;
  - `Test-Finalize6CompletionReevaluation.ps1` needs a native run ID.

## Evidence kinds

| Kind | What |
|---|---|
| Production code executed offline | `MountedAttackIntent`, `MountedAttackTerminalCausePolicy`, `MountedAttackTerminalPolicy`, `MountedAutomaticAttackPolicy`, `UnifiedMountedStockAttackPolicy` (component tests) |
| Production controller executed detached | `HandleCommandTerminal`, `ContinuesStockIntent`, `ClearStockAttackIntent`, `NativeActorCapability.Read`, on genuine native state, condition and command types, behind a substituted engine boundary (`Test-MountedAttackStallRepair.ps1`) |
| Installed native IL inspected | `TickCommandTurnBased`, `UnitAttack.ShouldBeInterrupted`, `CanAct`/`IsAble`, `AddCondition.OnTurnOn`, `AiBrainController`, `BlueprintAiAction`, `BlueprintAiAttack`, `SelectionManagerBase.Stop`/`Hold` |
| Decoded blueprint evidence | NereidLairBoss, NereidBeguilingAura*, FascinateEffectCombatBuff, FascinateCommonBuff, Serenity, SerenityBuff, Character_Brain and its attack actions (metadata summary in the artifacts) |
| Represented engine behaviour | Blank pair commands, outcomes and the warning bus subscriber in the probe |
| Modelled orchestration | None |
| Native gameplay | **NOT RUN** |

## Review of 2ae4cc8b: findings 1–5 (corrected)

### Cause-path audit: every path that ends a `MountedPairAttackCommand`

| Path | Recorded cause |
|---|---|
| Controller `Cancel(reason)`: Stop or Hold, a new stock click, a ground order, dismount, the load, area and combat boundaries | `Cancelled` (`InterruptOwnedCommand`) |
| `TryRetireStockIntent`; the orphaned automatic sweep | `Cancelled` |
| Queued automatic refused (stale, retired); a queued automatic with no pair owner (`InternalRunPrefix`) | `Cancelled` |
| Native trap-order group stop (`InterruptRiderApproachingOrders`) | `Cancelled` |
| Door interaction replacing a stock attack; charge admission replacing the active command | `Replaced` |
| A native `UnitCommands.Run` on the command's own actor for another command (scoped) | `Replaced` (resolved at the terminal) |
| `RequestChargeCleanup`, and the `Interrupt(false)` it causes in `OnStart`, `OnTick` and `StartChildAttack`; `InterruptExact` in charge cleanup | `Cancelled` |
| Charge revalidation failure; charge transaction-order violation; unproven carrier release | `Fault` |
| Charge lease application failure, and every other start or tick exception | `Fault` (`EndAfterFault`, before any diagnostic) |
| Repath allowance exhausted; bounded time exceeded while still approaching after a delegated move began | `ApproachFailed` (`ApproachFailure` thrown at that boundary) |
| Bounded time otherwise; a lost child attack; a transaction that cannot arrive; the admission bridge; other invariants | `Fault` |
| `TryEndExpectedTargetInvalidation` (died, unconscious, despawned, hostility, unattackable) | `TargetInvalid` |
| `TerminateIfActionActorBlocked` | `ActorCapability`, with the exact block |
| Legacy paired command scheduler (not the production authority): fail; disposal or relationship cleanup | `Fault`; `Cancelled` |
| Native only (genuinely Unrecorded) | `Unrecorded`. Covers: `TickCommandTurnBased` force-finish; `UnitActionController` interruption (`ShouldBeInterrupted`, `!CanAct` while running, a target out of state, untargetable or stealthed, `InterruptAsSoonAsPossible`); the paused `PreviousCommand`; native `InterruptMove`/`InterruptAll` reached without a KMC cancel. KMC's Stop/Hold prefix and the Dismounting subscribers cancel first. The runtime's own `InterruptMove` runs only after them, or on plain moves. The retired unified turn's `PrepareExactMountLedger` (not production) also stays Unrecorded |

No path parses a reason string for its cause. The first recorded cause stands, and `RecordEndCause` ignores a finished
command.

### A05 — the exact approach-failure rule

`RecordsAutomaticApproachFailure(automatic, childStarts, rangedTail, nativeSuccess, cause, approachRequiredAtStart,
delegatedMoveStarts)` is true only when all of these hold:
- the order is automatic;
- the cause is `ApproachFailed`, recorded by `EndAfterFault` from an `ApproachFailure` thrown at the repath-exhaustion
  boundary or the bounded time while still approaching;
- `ApproachRequiredAtStart`;
- `DelegatedMoveStartCount > 0`;
- no child attack started;
- neither native Success nor a ranged tail.

`Unrecorded` is never evidence. In the probe, only case 1 of the twelve marks a target:

| # | Case | Marks |
|---|---|---|
| 1 | a genuine failed delegated approach | yes |
| 2–6 | target died, became unconscious, despawned, changed hostility, became unattackable | no |
| 7 | an in-range unrecorded interruption with no delegated move; an unrecorded end after an approach | no |
| 8 | capability | no |
| 9 | cancellation | no |
| 10 | replacement | no |
| 11 | fault | no |
| 12 | a successful attack, a ranged tail | no |

### Durable causal capability

- `RecordEndCause(ActorCapability, detail, block)` saves the exact block read at `TerminateIfActionActorBlocked`; the
  outcome carries it as `EndCauseCapabilityBlock`.
- `ActorCapabilityBlockAtTerminal` stays the separate, later snapshot. Both are logged as `causalBlock` and
  `terminalBlock`.
- `CausalCapability(kind, cause, recordedBlock, terminalBlock, started)`:
  - with `ActorCapability` it returns the recorded block, whatever the snapshot;
  - with any other recorded cause it returns `None`;
  - with `Unrecorded` it returns the snapshot only on native's installed signatures.
  - Feedback uses that block.
- **Cases:**
  1. a recorded daze with the snapshot cleared;
  2. a recorded daze with the snapshot changed to `CanNotAttack`;
  3. a recorded `CanNotAttack` with the snapshot cleared;
  4. a cancellation or fault beside any snapshot;
  5. a native unrecorded terminal;
  6. no condition.
- **Results:** in cases 1–3 the recorded block is the cause, both in production policy and in the compiled controller.
  Case 4 is non-capability, case 5 uses the snapshot, and case 6 gives no capability text.

### Exact native-Run scope

- **Transpiler:** `PatchMethods.NativeRunScopeTranspiler`, on the public `UnitCommands.Run(UnitCommand)` (060026B2),
  replaces its one call of the internal `Run(UnitCommand, bool, bool)` (060026B3) with `MountedNativeRunScope.ScopedRun`.
  Run against the installed IL, it changes exactly that one instruction.
- **The hook:**
  - opens a token holding the exact container and command;
  - calls the internal `Run` (with its own patches) through a delegate;
  - closes that token in a `finally`.
- **Why this design:** Harmony 1.2 has no finalizer, so the C# `finally` inside the original body is the exception-safe
  equivalent. The prefix and postfix no longer touch scopes, so no pair ownership is re-resolved.
- **The scopes (`MountedNativeRunScopes`, Domain):**
  - an ordered stack; nested Runs stack;
  - a close removes its own token and anything stranded above it, never an outer one;
  - an unknown or null token is harmless;
  - `MountedNativeRunScope.Reset` runs at mod disable and disposal.
- **Replacement test:** a pair command that ends inside a scope on its own actor's container, for another command, was
  replaced.
- **Cases (production scopes):**
  1. normal open and close;
  2. a refused command, a scene handoff, or a dismount and remount (the token closes regardless of the owner);
  3. nested on the same pair;
  4. nested on another pair;
  5. an exception;
  6. a stranded token;
  7. a later replacement still classified;
  8. reset.

### A01 counter

- `ObserveStockClick` classifies the click once (`MountedAttackIntent.ClassifyClick`: `Continue`, `FreshAfterTerminal`
  or `NewRequest`) and counts only `FreshAfterTerminal`.
- A retarget, an action, weapon or actor change, or an invalid target is `NewRequest` and is not counted.

### Fault and terminal delivery

- **`EndAfterFault`** (used by `OnStart` and `OnTick`) records the cause, faults the transaction, then logs inside a
  catch, then ends the command (`ForceFinish(Fail)` if acted, else `Interrupt`).
- **`ReportTerminalOnce`** delivers the terminal once, whatever fails along the way:
  - a failing capability read is contained;
  - a throwing logger is contained;
  - an outcome that cannot be fully described is replaced by a minimal one (result, transaction state, cause and causal
    block, approach evidence).
- **Native end:** wrapped so its exception cannot skip the report.
- **Probe:** `OnStart` and `OnTick` faults with a throwing logger and an unreadable actor deliver the terminal, record
  `Fault`, and retire the order.

### Mutation campaign (serial; log `review/03-mutation-campaign.log`)

- **Setup:**
  - one disposable worktree and one `MSBuild /m:1 /nodeReuse:false /p:UseSharedCompilation=false` per mutant, removed
    before the next;
  - the candidate DLL is frozen separately;
  - the probe prints each mutant's path and MVID.
- **First attempt:** kept as `03-mutation-campaign-ATTEMPT1-runner-misclassified-Q6.log`. Its runner counted only probe
  lines beginning with FAIL, so a fatal probe check caught Q6 but was reported as SURVIVED. The runner was corrected and
  the whole campaign rerun from Q1.

| ID | Mutation | Caught by |
|---|---|---|
| Q1 | a terminal same-target click inherits `hasDispatched` | components (A01) |
| Q2 | a live-command click treated as fresh | components (A01) |
| Q3 | dispatch state shared between pairs | components (existing two-pair test) |
| Q4 | Stop while blocked labelled "cannot act" | components (A02) |
| Q5 | a capability-caused terminal loses its message | components (causal block) |
| Q6 | an automatic intent warns the player | probe (automatic retirement check) |
| Q7 | a sticky automatic refusal | components (A04) |
| Q8 | player real-time orders suppressed by the automatic rule | probe (wiring) |
| Q9 | the cause inferred from the snapshot alone | probe (A02 causes) |
| Q10 | a capability-ended automatic command marks its target | components (A05) |
| Q11 | the admission ignores `CanNotAttack` | components (A04) |
| Q12 | uncontained terminal logging | probe (A02 logger) |
| Q13 | target invalidation or an in-range terminal marks the target | components (A05) |
| Q14 | `OnStart` faults without `Fault` | probe (fault delivery) |
| Q15 | the recorded block replaced by the snapshot | components (causal block) |
| Q16 | the Run scope closed outside a `finally` | probe (Run scope) |
| Q17 | a throwing logger in fault handling stops the terminal | probe (fault delivery) |

## Remaining native evidence limits

- **Not observed in play:**
  - a fresh same-target click after a completed routine;
  - Stop or a replacement while blocked;
  - Royon's actual Fascinate state;
  - a `CanNotAttack` rider with AI on in real time.
- **Native order-replacement scope:** the transpiler has been run against the installed IL offline only, not applied in a running game.
- **Not changed:** `NativeMountedControlService` falls back to the combat controller's last feedback for a rejected
  mounted-control dispatch that has no refusal of its own. That shared surface is not a Sprint 6 terminal path.

Sprint 7 was not started. This is the Sprint 6 review checkpoint.
