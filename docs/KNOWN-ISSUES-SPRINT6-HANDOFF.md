# Known issues remediation, Sprint 6 — attack requests, terminal causes, Serenity/Fascinate, automatic churn (2026-11-18)

Sprint 6 of the Mounted Combat Known Issues Remediation Epic: A01–A04. Offline engineering only: nothing was launched,
staged or installed, and nothing here is native gameplay evidence. Sprint 7 was not started.

## Identity

| Item | Value |
|---|---|
| Base | `772543e19bfbe827c175c22fb8b3c81dbb1b44e8`, the accepted Sprint 5 checkpoint (draft PR #50, unmerged; verified as the live branch head, parent not advanced). Accepted product build `cdfbe509` |
| Branch and worktree | `codex/kmc-known-issues-sprint6`, `worktrees/known-issues-sprint6` |
| Draft PR target and parent | `codex/kmc-known-issues-sprint5` (parent PR #50) |
| Commits | `3472914d`: A01, A02, A04, the A05 repair, Sprint 5 test wording, tests and the probe extension. `221d748b`: the automatic-only pin. Then the records commit carrying this file, which is the branch head |
| Product-build commit | `3472914d (product sources; built at 221d748b)` |
| Product DLL (clean Release build) | SHA-256 `27579ff8870c5961663c860cdcf60fa8349069de05746bf43d028488db312936`, MVID `e8d32009-c6c5-4aba-908b-11dae9954a31`; frozen copy `artifacts/known-issues-sprint6/head/` |
| Budget | `known-issues-sprint6` |
| Preserved | PRs #45–#50, their branches, the main checkout and every other worktree |

## Dispositions

| ID | Disposition | Summary |
|---|---|---|
| A01 | **Implemented and verified offline** | A same-target click after the dispatched command ended, before the next Update, continued finished work and inherited its dispatched state, so a spent actor's click got no answer. It is now a fresh request (a new generation and its own answer) through the existing replacement path. A click while the command is live still continues and promotes it. |
| A02 | **Implemented and verified offline** | The terminal text read the actor's capability snapshot whatever ended the command, so Stop, a replacement, a boundary or a fault beside a blocked actor read "cannot act". Each end now records a typed cause; the snapshot explains only a capability-caused end. Terminal logging and warning delivery are contained: a failing logger no longer stops retirement. |
| A03 | **Settled explanation; no product change** | Exact asset evidence: "Fascinate" from the Lady of Shallows is `FascinateEffectCombatBuff` (`8a59ee05…`), applied each round by her `NereidBeguilingAura` on a failed Will save. It carries `AddCondition: Dazed`, which makes `UnitState.CanAct` false, and the current reader handles it. Serenity (`SerenityBuff` `0a90088d…`) applies no condition: it damages its owner when the owner makes an attack roll. Live state was not observed. |
| A04 | **Established and corrected (automatic only)** | In real time a rider with `CanNotAttack` and AI on had every brain proposal (about every 0.3–0.4 s) admitted, then interrupted by native `UnitAttack.ShouldBeInterrupted`, two log lines a cycle. The automatic admission now reads the condition, with no state. The player's real-time wait is intentional and unchanged. |
| A05 (new) | **Implemented and verified offline** | Found while reconciling A04. An automatic command ended by its rider's capability, a cancellation, a replacement or a fault was recorded as a failed approach, so the rider's brain kept its target unreachable after a transient condition. Now only a genuinely failed approach records it. |

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
    same actor (a scope opened by the `UnitCommands.Run` prefix and closed by its postfix);
  - `ActorCapability`: `TerminateIfActionActorBlocked`;
  - `Fault`;
  - `TargetInvalid`;
  - `Unrecorded`: native's own end.
- **Capability as cause (`MountedAttackTerminalCausePolicy.CapabilityIsCause`):**
  - true for `ActorCapability`;
  - with nothing recorded, true only on native's installed signatures: an unstarted turn-based Success force-finish,
    a `CanNotAttack` interruption, or a `CanAct`-term interruption of a running command;
  - never beside an explicit cause.
- **Feedback:**
  - Stop or a boundary: "Mounted pair attack cancelled." (code `CommandCancelled`);
  - a replacement: "Mounted pair attack replaced by a newer order.";
  - a capability-caused end keeps "X cannot act right now (…)";
  - completed and delivered work never carries a failure.
  - Failed terminals raise no warning, as before. An unexecuted player order still warns once.
- **Diagnostics:** the outcome keeps the raw result, transaction state, reason, capability block and capability
  details, and adds the cause and whether the capability was the cause. Nothing native is written.

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

## Validation (budget `known-issues-sprint6`)

| Check | Result |
|---|---|
| Clean Release build of `3472914d (product sources; built at 221d748b)` | PASS; identity above (log `05-clean-release-build.log`) |
| Components | 1392/0 (1387 at the base; +5 Sprint 6 tests; the Sprint 5 `Lifetime` case reworded with an added no-consumption assertion) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MountedAttackStallRepair.ps1` (extended: A01, A02 causes, A02 logger, A03 native consumers, A04 terminals, A04 admission, wiring) | 110/0 (log `02-stall-probe-head.log`). Accepted Sprint 5 build: 63 pass, then **6 Sprint 6 sections fail** (A01, A02 causes, A02 logger, A04 terminals, A04 admission, wiring) (log `04-stall-probe-base-cdfbe509.log`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint6`, focused: stall repair, command slot, native input semantics, native turn-completion wrappers, paired activation, marker ownership, ranged movement, continuous drive, persistence, world ownership | PASS 13/0 (0.66 min) (log `06-fast-tier.log`); budget spent 0.65 of 90 minutes over 1 entry |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `07-assembly-contracts.log`) |
| Mutations (disposable detached worktree with its own binaries; the probe reads each mutant DLL by path; log `03-mutations.log`) | **Incomplete.** Twelve were defined (Q1 a terminal click inheriting hasDispatched; Q2 a live-command click treated as fresh; Q3 dispatch state shared between pairs; Q4 an explicit Stop while blocked labelled cannot act; Q5 a capability-caused terminal losing its message; Q6 an automatic intent warning the player; Q7 a sticky automatic refusal surviving the condition and generation; Q8 player real-time orders suppressed by the automatic rule; Q9 the cause inferred from the snapshot alone; Q10 a capability-ended automatic command marking its target; Q11 the admission ignoring CanNotAttack; Q12 uncontained terminal logging). Q1-Q4 ran and were each caught by component tests (log 03-mutations-PARTIAL-stopped-low-memory.log). Claude Code stopped the run for low system memory before Q5; Q5-Q12 were not run and wait for the owner's go-ahead. Their target behaviours are each asserted by the probe sections or component tests above, which is not mutation evidence |

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

## Remaining native evidence limits

- **Not observed in play:**
  - a fresh same-target click after a completed routine;
  - Stop or a replacement while blocked;
  - Royon's actual Fascinate state;
  - a `CanNotAttack` rider with AI on in real time.
- **Native order-replacement scope:** its effect on wording is offline evidence only.
- **Not changed:** `NativeMountedControlService` falls back to the combat controller's last feedback for a rejected
  mounted-control dispatch that has no refusal of its own. That shared surface is not a Sprint 6 terminal path.

Sprint 7 was not started. This is the Sprint 6 review checkpoint.
