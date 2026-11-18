# Mounted attack stall repair — handoff (2026-11-15)

## Sprint 6 (2026-11-18), superseding

Read with the [Sprint 6 handoff](KNOWN-ISSUES-SPRINT6-HANDOFF.md), as corrected after the reviews of `2ae4cc8b`,
`fa533e97` and `dffd3e72`; nothing below this section is rewritten.

- **"The Serenity and Fascinate blueprints were not decoded":** they are now, from the installed assets.
  - Fascinate from the Lady of Shallows is `FascinateEffectCombatBuff` (`AddCondition: Dazed`), applied by her
    beguiling aura.
  - Serenity applies no condition.
  - The stopped rider's condition is therefore Dazed, which this repair's reader already handles. Live state was not
    observed (A03, settled explanation).
- **"Terminal text":** corrected (A02). A routine stopped by the player while its actor is blocked is now a
  cancellation, not "cannot act".
- **"Real time, automatic order, `CanNotAttack`":** established from the installed brain and AI, and corrected (A04).
  The automatic admission refuses such a proposal. Only a typed `ApproachFailed` end, with the approach's own
  evidence, marks an automatic target unreachable (A05). The player's real-time wait is intentional and unchanged.
- **Terminal causes:** every end of a pair command records a typed cause. A capability end keeps the exact block it
  read. Native's own capability ends are identified at the exact native branch (the turn-based force-finish that
  stalled this repair's original report is `TurnBasedCannotAct`); the terminal snapshot never decides. The facts
  that identify those branches live only in the native invocation itself and hold no command beyond it.

Branch `codex/mounted-attack-stall-repair` from main `474b8667` (release 0.2.10). Scope: a mounted attack order that
hangs turn-based combat when the rider cannot act. No merge, release, tag, version bump or install.

**Native reproduction and in-game fix verification not run.** See the last section.

## Report

- **Player setup:** Mounted Combat 0.2.10, turn-based combat against the Lady of Shallows in Rill and Spill
  (`NereidLairFW/191d887bce4c23743b65acd366f53199`). Royon (`b8a8d2d2-…`) is mounted on his horse (`638ced0f-…`) and
  has Serenity and Fascinate, so he cannot act. Ordering him to full attack (`3b064708-…`) hangs the combat.
- **Log (`output_log(8).txt`, as reported to this task):** one earlier attack on the same target ends
  `result=Success; reason=completed; childAttacks=1`. Later the log alternates 4,922 times, to its end, between
  `Mounted combat command accepted: action=RiderRanged; inputSurface=stock-hostile-click` and
  `Mounted combat command terminal: action=RiderRanged; result=Success; reason=Success; childAttacks=0`.
- **The log is not on this machine.** `Downloads` holds `output_log.txt` (no KMC rider line) and `output_log(2).txt`
  (a 0.2.3 log). Neither contains the cycle and neither was substituted. Everything below about the log is the
  reported evidence, not a reading of the file.
- **What the evidence does not show:** Royon's live condition flags, which condition ended the first command, whether
  the failed commands were planned as full attacks, and how fast the cycle ran. `stock-hostile-click` is also the
  label of a redispatched retained order, so it does not mean thousands of clicks.

## Cause

### Confirmed in source and in the installed 2.1.7b assembly: the continuation defect

Installed `Assembly-CSharp.dll` SHA-256 `3b6450ff…`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`.

1. **Native ends an unstarted command with Success.** In turn-based combat
   `UnitActionController.TickCommandTurnBased` (0x0600911D) looks at every command that is in reach, not started and
   has no result. If its executor fails `State.CanAct` (or is nauseated), or fails
   `CombatState.CanActInCombat && !HasCooldownForCommand(command)`, it calls
   `command.ForceFinishForTurnBased(ResultType.Success)` and clears the queue. These are the only two native callers
   of that method.
2. **`UnitCommand.ForceFinish` executes nothing.** It sets `IsActed` (turn-based, in combat), stamps the result and
   calls `OnEnded`. The command never starts, `OnAction` never runs and `UnitActionController.UpdateCooldowns` is
   never reached, so no Standard action is spent.
3. **The pair command is always "in reach" before it starts.** `MountedPairAttackCommand.Init` sets an infinite
   approach radius and no line-of-sight requirement, because the mount carries the approach.
4. **The mounted transaction is cancelled, and reported beside native Success.** `OnEnded` completes the transaction
   only for a delivered routine. Otherwise it calls `transaction.Cancel(Result.ToString())`. The terminal therefore
   reads `result=Success; reason=Success; childAttacks=0`: the logged signature.
5. **The old terminal handler kept the order.** `HandleCommandTerminal` cleared the retained order only for a
   non-success native result, and reported "Mounted pair attack completed."
6. **`Update()` dispatched it again.** `TryDriveStockAttackIntent` found no active command, a valid target and
   `rider.HasStandardAction()` still true, and created the next command. The duplicate-dispatch guard only sees a
   command that is still unfinished, and `TryRetireStockIntent` returns early for a player order in turn-based combat.

The probe pins items 1 and 2 against the installed assembly (see Regressions).

### Not verified: which condition stopped Royon

The Serenity and Fascinate blueprints were not decoded and no live state was read.

- **What native `CanAct` reads:** `IsHelpless` (sleeping, paralysed, petrified, unconscious), dazed, stunned,
  `Prone.Active`, `CantAct`, and a blocking animation.
- **Excluded for this signature:**
  - Nauseated: `HasStandardAction()` is already false, so nothing would be dispatched.
  - `CanNotAttack`: native interrupts such an attack (`UnitAttack.ShouldBeInterrupted`), which logs
    `result=Interrupt`, and the old handler already retired that.
- **Still possible besides a condition:** `CanActInCombat` false (initiative not reached) or a command cooldown.
- **Why the turn stayed open (source, not observed):** the paired activation keeps the rider's turn open while the
  mount still has an action.

The repair does not depend on the answer: every native path that ends the command without an executed attack is
retired, and the condition terms are also refused before a command is made.

## Change

### Admission: the acting actor's native conditions (protection A)

- **`Domain/MountedAttackExecutionPolicy.cs` (new), `MountedActorCapabilityPolicy`:** plain facts in, one block out.
  - The facts are the condition terms of native `CanAct` (`IsHelpless`, Dazed, Stunned, `CantAct`) and the attack ban
    `CanNotAttack`. No buff name or GUID is read.
  - **Left to native:** prone and animation (execution delays), the action budget, turn ownership and initiative.
    Each already has its own gate.
  - **Turn-based:** a new order is refused (`AdmissionBlock`). That is where native ends it with Success.
  - **Real time:** nothing is refused, and the pair's real-time flow is unchanged. Native has no Success terminal
    there: it keeps the unstarted order of an actor that cannot act in its slot and starts it when the condition
    ends, and it interrupts an attack-banned actor's order, which already retires it.
- **`Integration/NativeActorCapability.cs` (new):** the only reading of those facts, from the acting actor's
  `UnitState`.
- **`MountedCombatActionContext` / evaluator:** a new `ActionActorCapabilityBlock` beside the unchanged
  `ActionActorHasStandardAction`, and a new rejection code `ActorCannotAct` (appended, so existing values keep their
  numbers). `CaptureContext` fills it for the action actor only.
- **Effect on a click:** the existing rejection path answers once (for example "Royon cannot act right now (dazed)."),
  logs once and retires the order. `ActorCannotAct` is not a routine continuation refusal, so a restriction that
  arrives after an earlier accepted attack still answers once.
- **Partner:** the mount's own order is judged on the mount's state, and the reverse.

### Execution boundary

`MountedPairAttackCommand.StartChildAttack` re-reads the same facts before it releases the attack
(`TerminateIfActionActorBlocked`), in both modes. A restriction that arrives after queuing or during the approach
cancels the transaction with `Action actor cannot attack: <block>` and interrupts the command. Native would interrupt
it on the same tick anyway (`TickCommand`: `!CanAct` or `ShouldBeInterrupted`); this only stops the attack animation
from starting first and names the cause.

### Terminal classification and retirement (protection B)

- **`MountedAttackTerminalPolicy.Classify`:** the native result is one fact among the command's own.
  | Kind | Facts | Order |
  | --- | --- | --- |
  | `Completed` | native Success and the transaction completed | kept |
  | `RangedTailDelivered` | the existing ranged-tail rule | kept |
  | `DeliveredWithoutCompletion` | native Success, attack rule observed, at least one attack delivered | kept |
  | `EndedWithoutExecution` | native Success, no delivered attack | retired |
  | `Failed` | any other native result | retired, as before |
- **Not decided by `childAttacks`.** That count is a sequence-start marker written after `base.OnStart()`. Native
  startup can end the command synchronously before it (`TryStartNextAttack` false leads to `Interrupt`), but cannot
  act: `OnAction` runs only from a later `Tick`. Delivery is read from the transaction state, the observed native
  attack rule and the delivered attack count.
- **`MountedPairAttackOutcome`:** new `TransactionState`, `TransactionCompleted`, `NativeCommandStarted`,
  `ActionActorName`, `ActorCapabilityBlockAtTerminal` and `ActorCapabilityAtTerminal`. The diagnostic scenarios
  serialize the outcome, so their evidence gains these keys.
- **`HandleCommandTerminal`:** an `EndedWithoutExecution` terminal of the order's own command clears that order
  (`ClearStockAttackIntent`), and nothing else. The stored generation advances, so a later click is a new order.
  No action, cooldown, condition, mount state or turn is written, and no suppression is kept.

### Feedback

- **"Mounted pair attack completed."** now requires a completed transaction or delivered attacks.
- **Unexecuted:** "Royon cannot act right now (dazed). The attack was not performed." when the actor's condition is
  the identified cause, otherwise "Royon could not perform the attack; the order was dropped."
- **Player warning:** a player's order that is retired without execution is answered once, with whichever of the two
  texts applies. The retired order cannot repeat it. An automatic order never warns.
- **Interrupted routine:** a native interrupt of a started routine whose actor is blocked now names the actor and the
  condition instead of "Mounted pair attack ended: Interrupt."

## Diagnostics

- **New, once per retired order:**
  `Mounted attack intent retired without execution: origin=player; generation=…; action=RiderRanged; actorId=…; targetId=…; nativeResult=Success; transaction=Cancelled; reason=Success; started=False; sequenceStarted=False; attackRule=False; attacks=0/3; capability={block=…;helpless=…;stunned=…;dazed=…;cantAct=…;canNotAttack=…;nauseated=…;prone=…;nativeCanAct=…;canActInCombat=…;commandCooldown=…;standard=…;turnBased=…}.`
  - `attacks=delivered/planned` shows whether a full attack was planned.
  - The `capability` block is what native saw of the actor when it ended the command. It answers the open question
    above from an owner log.
- **Existing terminal line:** gains `transaction=` and `terminal=` before `feedback=`. The raw `result=` and `reason=`
  are unchanged.
- **Existing dispatch-rejection line:** gains `actorId=` and `targetId=`.
- **Volume:** one line per command terminal as before, one line per retired order, nothing per frame.

## Preserved

- **Native authority:** no action budget, cooldown, preparation, initiative or condition is read for writing or
  written. The repair only declines to create a command, or forgets an order.
- **Executed attacks:** a miss or a hit for no damage runs the native attack rule and completes the transaction, so
  it keeps its order exactly as before.
- **Real-time repetition, full attacks, the ranged tail, target death mid-routine, routine continuation on the next
  target, Stop and retarget:** their terminals are `Completed`, `RangedTailDelivered` or `Failed` and take the same
  branch as before.
- **Released projectiles:** retirement touches no command. The terminal's command has already ended.
- **Explicit actions and charge:** they hold no stock order, so only their feedback text can differ.
- **Unmounted units:** no shared seam was changed.

## Regressions

- **`tests/MountedAttackExecutionTests.cs`:** 12 cases in the component runner, on the production policies, evaluator
  and `MountedAttackIntent`.
  - Turn-based refusal for each block; real time unrefused; the execution boundary in both modes; unknown state.
  - A remaining Standard action does not admit a blocked actor; the single `ActorCannotAct` code and its text.
  - Rider and mount judged separately, in both directions.
  - The refusal answers a fresh and an already-dispatched player order once, and never an automatic one.
  - Terminal classes: unexecuted Success, full and single attacks, the ranged tail, delivered without completion,
    interrupted with and without delivery.
  - One generation retired; a fresh order dispatches and completes.
- **`scripts/Test-MountedAttackStallRepair.ps1`:** focused, 59 checks, detached (no game).
  1. **Native contract pins (7):** the pinned MVID; `TickCommandTurnBased` calls `ForceFinishForTurnBased`,
     `get_CanAct`, `get_CanActInCombat` and `HasCooldownForCommand`; `ForceFinish` calls `SetIsActed`, `set_Result`
     and `OnEnded`; `CanAct`'s composition; `UnitAttack.ShouldBeInterrupted`; the condition names.
  2. **The central regression (13), on the compiled `MountedCombatController`:** a real `MountedAttackIntent` holds a
     player order with its dispatched command. The production `HandleCommandTerminal` receives a production
     `MountedPairAttackOutcome` with the logged signature.
     - The order is retired and its command released.
     - 500 later calls of `TryDriveStockAttackIntent`, the dispatcher `Update()` ends in, find no order: each returns
       at its first guard, dispatches nothing and adds no log line or warning.
     - The contrast: with a retained order the same call goes past that guard toward the next dispatch. Detached, it
       stops at the live combat-mode read, so the dispatch itself is not executed offline.
     - The feedback is not a completion; one retirement line with the identity and progress facts; one warning on
       the real native `EventBus`.
     - A fresh order is a new generation without stale history, and its executed full attack completes.
  3. **Neighbouring terminals (13):** executed 3-attack and 1-attack routines keep the same generation; the ranged
     tail; interrupted routines with and without delivery; a terminal of a command the order does not own; an
     explicit action; an unexplained unexecuted terminal (retired, recorded, one warning); an automatic order (no
     warning).
  4. **The production reader on genuine `UnitState` objects (26):** each block refuses in turn-based combat, ends a
     started routine and is left alone in real time; neither actor's condition reaches the other; native
     `IsHelpless` by sleep and by unconsciousness; eight non-capability conditions (nauseated, staggered, prone and
     others) refuse nothing; an unreadable actor refuses nothing; the terminal diagnostic survives unavailable engine
     readings.
- **Old/new:** the same probe was run against a build of unmodified `474b8667`
  (SHA-256 `f9ec6ce1334fc082ad242215e617e626725fbe8d32c0cb19153793636b846181`). It passes the 7 native pins and the
  setup check, then fails at "native Success without an executed attack retires the retained player order" (exit 1).
  The properties the old build lacks are skipped by the probe, so the failure is the old decision itself.
- **Mutation checks (scratch copies of the final source, working tree untouched):**
  - Retaining `EndedWithoutExecution`: components 1227/2; the probe fails at the same central check.
  - `AdmissionBlock` always `None`: components 1227/2; the probe fails at "genuine Dazed refuses a turn-based order".
- **Limits of these tests:**
  - A miss and a hit for no damage are not separate cases: they produce the same terminal facts as a hit.
  - The component case "an unexecuted terminal retires one intent generation" applies the policy through a test-local
    step. Only the probe runs the controller's own `HandleCommandTerminal`.
  - The probe's outcomes are built by hand, including the capability block.
- **Not covered offline:**
  - `CaptureContext`, `TryExecuteAction`, `TryDriveStockAttackIntent` beyond its first guard, and
    `StartChildAttack`: they need a live game instance, views and a turn controller. Their new lines are wiring of
    the tested reader and policies.
  - That native ends Royon's command for the reason assumed, and that the turn can then be ended or handed to the
    horse.
  - The outcome built by `ReportTerminalOnce` in a real command; the probe builds the same production type by hand.

## Independent review

One read-only review of the uncommitted diff against the installed-assembly source. It found no blocking defect, no
path where an executed attack loses its order and none where an unexecuted one keeps it.

| Finding | Disposition |
| --- | --- |
| An order dropped for a cause other than a condition (for example a stun-exit animation) was silent, where the old loop carried it through to an attack | Corrected: the player is now told once for every retired player order |
| The "500 later updates" check was vacuous on its own | Corrected: the retained-order contrast was added and the check is described as what it is |
| The real-time rationale was wrong for `CanNotAttack` (native interrupts, it does not keep the order pending) | Comment and this handoff corrected; behaviour unchanged |
| Component tests reach the controller only through a test-local step; miss and zero damage are not separate cases | Recorded under "Limits of these tests" |
| Real time: a blocked rider's pending command keeps the mount from attacking for the same order | Not introduced here; recorded under Uncertainties |
| Real time, automatic order, `CanNotAttack`: a possible admit/interrupt cycle at the brain's rate | Not introduced here and not confirmed; recorded under Uncertainties |
| Legacy unified turn: a blocked rider's order is refused whole, so the mount's share of it is not dispatched | Not the required paired configuration; recorded under Uncertainties |
| The terminal's capability reading is taken when the command ends, so a Stop while the actor happens to be blocked is described as "cannot act" | Diagnostic text only; recorded under Uncertainties |

## Checks (budget `mounted-attack-stall`)

| Check | Result |
| --- | --- |
| Fast + focused, before the review | TIER FAST PASS=4 FAIL=0 (components 1229/0; Validate-Source 151/0; focused 57/0) |
| Fast + focused, review corrections | build PASS; components 1229/0; Validate-Source 151/0; focused FAIL (script defect: a stray brace from an edit of the probe, fixed) |
| Fast + focused, final source | `scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-MountedAttackStallRepair.ps1 -BudgetId mounted-attack-stall`: TIER FAST PASS=4 FAIL=0 in 0.15 min (build up to date after the 22.8 s build of the run above; components 1229/0; Validate-Source 151/0; focused 59/0) |
| After these records were written | the same tier was run once more on the committed tree; its result is in the pull request description |
| Old build, same probe | exit 1 at the central regression, 8 checks passed before it |
| Mutations | both caught by the component runner and the probe (above) |
| Product checked | `bin\Release\KingmakerMountedCombat.dll` SHA-256 `6786e45f6c3718ed79f880a87a89b91da1ab1c86e81f4aad2d6820f6e2f94731`, MVID `c162d5a3-f7f7-43d9-a21a-c4ba31ee4823`; not packaged or installed |
| Budget | 1 of 90 min after the final-source run, 3 entries, none interrupted |
| Assembly contracts (Candidate) | not run: the focused probe binds the native members it relies on |
| Package, suite, purity, Full | not selected for this checkpoint |

## Native verification: NOT RUN

**Native reproduction and in-game fix verification not run.**

**Missing capability:** this machine has no KMC installation, no qualified KMC Baseline/Working fixture and no
`runtime-state\fixture-qualification.json`. The installed game is the owner's modded installation, and a human
session may be live. Nothing was staged, launched, replaced or restored, and no save was read or written.
Compilation, component tests and the detached probe do not show this combat fixed.

| Row | Needed evidence | Result |
| --- | --- | --- |
| S1 | Reported state (mounted Royon, turn-based, Lady of Shallows, Serenity and Fascinate): one attack order produces one refusal or one `retired without execution` line, then no further accept/terminal lines | NOT RUN |
| S2 | The `capability={…}` block or the refusal text names why native would not run the command | NOT RUN |
| S3 | Royon's Standard, Move and Swift cooldowns are unchanged by the refused order; no attack roll | NOT RUN |
| S4 | With Royon blocked, the horse's own attack or move works, or the turn ends normally | NOT RUN |
| S5 | After the restriction ends, a fresh attack order executes | NOT RUN |
| S6 | Healthy mounted ranged full attack in turn-based combat: `result=Success; reason=completed; terminal=Completed` | NOT RUN |
| S7 | Healthy real-time repetition, and a miss, keep attacking | NOT RUN |
| S8 | Restriction arriving during an approach: one interrupted terminal, no loop | NOT RUN |
| S9 | Unmounted comparison in the same encounter | NOT RUN |

**The exact remaining check:** an owner session with the branch DLL in the reported encounter, or a qualified fixture.
Order one attack with Royon while he is restricted, then search the log for `retired without execution` and
`Mounted stock attack dispatch rejected`. A substitute condition (any daze or stun on a mounted rider in turn-based
combat) would exercise the same code, but would not show that Serenity or Fascinate is the trigger.

## Uncertainties

- **The trigger.** If Royon's restriction is not one of the condition terms, admission does not refuse and the first
  command is still created. The terminal rule then retires the order after that one command, with the generic text
  and the `capability` record.
- **`HandleUnitCommandDidEnd`.** Native rounds a partly used Move up when any Standard command ends
  (`DoesCommandEmptiesMovement`), including a force-finished one. That is native behaviour for an unmounted unit as
  well. With this repair it can happen at most once per order, and not at all when admission refuses.
- **Real time.** A blocked rider's real-time order still waits in its slot as before, and the mount does not attack
  in its place meanwhile. That is unchanged behaviour, not part of this defect.
- **Real time, automatic order, `CanNotAttack`.** Native interrupts such an attack. If the party brain keeps proposing
  it, each proposal is admitted, interrupted and retired again at the brain's rate, with its log lines. That is
  unchanged, and whether the brain proposes it was not established.
- **Transient native refusals.** When native drops an unstarted turn-based order for a cause that is not a condition
  (an animation that still blocks acting, initiative not reached), the old loop happened to carry the order through
  to an attack once the cause passed. The order is now dropped once, as for an unmounted unit, and the player is told.
  A live log shows this as `retired without execution` with `block=None`. If it appears for an actor that should have
  been able to act, in particular the mount inside a paired activation, that is a defect to report.
- **Legacy unified turn.** Under that non-required configuration a blocked rider's turn-based order is refused whole,
  and the mount's share of it is not dispatched.
- **Terminal text.** The capability reading is taken when the command ends. A started routine stopped by the player
  while its actor happens to be blocked is described as "cannot act".
- **Evidence schema.** Diagnostic scenario evidence gains the new outcome keys. No reader was changed or run.
