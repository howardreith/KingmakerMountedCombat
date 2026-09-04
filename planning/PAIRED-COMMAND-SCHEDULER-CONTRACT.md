# Phase 3E Paired-Command Scheduler Contract

Status: IN PROGRESS

## Dev.2 implementation checkpoint

The primary Option A implementation is offline-complete on published parent `80a75ee6b3011cb4ec52d1b296776db25f6b0f15`, version `0.1.0-phase3e-dev.2`. `MountedPairCommandScheduler` is an injected runtime-only service and `PairedCommandSchedulerLeaseStateMachine` is a Unity-independent domain object. Harmony retains only the exact installed `TickCommandTurnBased` postfix and no lease fields.

The service registers before `UnitCommands.Run`, confirms the exact mount Standard slot afterward, and may extend only the returned eligibility Boolean for that same object. It independently revalidates pair/generation/turn/executor/slot/queue/origin/AwakeUnits/UI/mode/status gates on every native encounter and every service update. An unexpected stock-true result is forced false and faulted; the implementation never explicitly starts or ticks a command, mutates current turn/status, writes a cooldown/result, or adopts AoO/AI/foreign work.

Offline gates pass source `22/0`, Release, component `315/0`, visual/source-order `18/0`, harness/protocol `241/0`, and exact assembly `388/0`. Schema 56 binds the runtime lease and independent ledgers. This is not runtime credit: fresh clean-package vertical-slice A/B remain mandatory before any broader tranche.

## Dev.2 audited runtime attribution and actionable-frame definition

Immutable run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` proves the primary Option A seam executes the complete vertical-slice gameplay lifecycle. The exact command was admitted at frame `3982`, native `WaitingForUI` remained a hard false gate until the first eligible scheduler grant at frame `4293`, and stock observed command start at frame `4294`. It then drove once per frame, completed `Success`, charged the mount Standard exactly once, left rider Standard unchanged, emitted one mount-owned attack/roll/damage chain, retained the rider as `CurrentTurn.Unit`, emitted no native mount turn, and cleaned the exact lease/slot with no fault or residue.

That run remains immutable `FAIL 69/1` and receives no Gate 1 credit because both the in-game assertion and external schema validator incorrectly measured `startObservedFrame - admissionFrame <= 2`. Frames blocked by the preserved native `WaitingForUI` predicate are not actionable. For this contract an **actionable game frame** is a frame on which the exact native hard gates and pair-local lease gates all pass and the scheduler grants eligibility; `firstGrantFrame` is its durable boundary. Acceptance therefore requires `admissionFrame <= firstGrantFrame <= startObservedFrame` and `startObservedFrame - firstGrantFrame <= 2`. It does not weaken, bypass, or place a time bound on native UI staging.

Version `0.1.0-phase3e-dev.3` changes only that diagnostic calculation and its regression tests. The scheduler implementation and schema shape are unchanged. This is an observation/validator correction, not one of the two authorized scheduler repair cycles. Fresh dev.3 A/B runtime remains mandatory and the dev.2 failure will not be relabeled.

## Product model

The rider remains the sole native initiative, portrait, selection, camera, action-bar, and `CurrentTurn.Unit` principal. The active mount may execute exactly one explicitly registered pair-local command during that rider-owned turn. Rider and mount retain separate command containers, Standard/Move/five-foot/Swift cooldown ledgers, weapons, abilities, animations, targets, rule initiators, results, and cleanup.

There is no shared synthetic action pool and no second turn clock. `EnableUnifiedMountedTurn=false` preserves the Phase 3C separate-turn fallback. The new `EnablePairedCommandScheduler` gate defaults false until every required Phase 3E qualification row passes.

## Architecture selection gate

Status: PASS — Option A selected

Dev.1 proved that stock `UnitActionController` visited the exact active Mammoth command `2,485` times while the Mammoth was reference-present in `AwakeUnits`; every stock result was false while the exact rider turn remained `Preparing`. Therefore the primary seam is Option A. Options B-D remain inactive and may not be explored unless Option A reaches an exact recorded kill criterion.

Evaluation order:

1. **Option A — exact native eligibility extension.** Use only if stock visits the exact command. Extend only reference-exact pair eligibility while preserving `WaitingForUI`, target, range/LoS, life, hands, equipment, cooldown, approach, start, tick, resource, event, result, and slot behavior. The rider may be in native `Preparing` only when this exact leased pair command is the first shared-turn action; no global status rewrite is allowed.
2. **Option B — exact native supplied-command seam.** Use only if stock does not visit but private `UnitActionController.TickCommand(UnitCommand,bool)` can be invoked once for the exact lease without duplicating a stock tick. Never invoke `TickOnUnit`, because it advances all mount commands.
3. **Option C — original pair-local explicit scheduler.** Reproduce only the minimum lifecycle unavailable through an exact native seam. Do not emulate global turn control.
4. **Option D — rider-owned scheduling shell.** Authorized only after A-C hit an evidenced kill criterion. The shell consumes no rider action and emits no rider attack; one mount-owned child retains actor, weapon, target, animation, rule, damage, and resource ownership.

Changing `CurrentTurn.Unit`, calling `StartTurn(mount)`, replacing the global controller, advancing all mount commands, or admitting foreign/AI/AoO commands is prohibited.

The selected Harmony postfix is only a policy bridge into the injected scheduler service. It stores no lease state. It may change a false native return to true only after the service proves the exact registered lease and independently rechecks all hard conditions whose original false return is otherwise ambiguous. Stock `TickCommand` remains the sole caller of native approach/start/tick/cooldown/result/removal behavior; KMC does not invoke it a second time.

## State machine

The scheduler state is owned by an injected service, never a Harmony patch field.

| State | Entry | Permitted exit |
|---|---|---|
| `Idle` | no lease | `Registered`, `Disposed` |
| `Registered` | exact command and identities captured before/at `UnitCommands.Run` admission | `AwaitingStart`, `Interrupting`, `Faulted` |
| `AwaitingStart` | exact slot/queue admission and native predicates verified | `Running`, `Interrupting`, `Faulted` |
| `Running` | native `IsStarted && IsRunning` observed | `Finishing`, `Interrupting`, `Faulted` |
| `Finishing` | terminal result/finish transition observed | `Completed`, `Faulted` |
| `Interrupting` | named invalidation requires exact interrupt | `Completed`, `Faulted` |
| `Completed` | one terminal result, exact slot cleanup, observations sealed | `Idle`, `Disposed` |
| `Faulted` | an invariant or native-call failure occurred | exact best-effort interrupt/cleanup, then `Disposed` or safe `Idle` only after proof |
| `Disposed` | relationship/lifecycle/mod boundary | none |

Transitions are monotonic per lease. `Completed`, `Faulted`, and `Disposed` cleanup is idempotent.

## Exact lease identity

Every lease captures immutable registration data:

- exact rider reference and `UniqueId`;
- exact mount reference and `UniqueId`;
- mounted-relationship generation;
- exact native rider `TurnController` reference, rider ID, and round at registration;
- exact command reference, runtime type, executor, command slot, and queue expectation;
- action kind and input origin;
- exact target reference/ID;
- exact weapon or ability reference/blueprint ID when applicable;
- creation and admission Unity frame;
- expected cooldown/resource owner;
- expected attack/rule initiator;
- whether native stock enumeration encountered the command;
- first/last driven frame and drive count;
- native started/running/acted/finished/result observations;
- one terminal reason and cleanup reason.

Mutable state records counters and observations but cannot retarget or adopt a replacement command.

## Admission invariants

Admission is true only when all of these are true:

1. unified mounted turn and paired scheduler gates are enabled;
2. relationship state is exactly `Mounted` with one active pair;
3. exact captured rider and mount references/IDs still match the relationship generation;
4. exact captured rider `TurnController` is still `CombatController.CurrentTurn`;
5. `CurrentTurn.Unit` is the exact rider and remains so before and after scheduling;
6. command executor is the exact mount;
7. command is the reference-identical KMC-created object registered by `MountedCombatController`;
8. command remains the reference-identical live object in the expected mount raw slot, or in the explicitly recorded queue state before promotion;
9. command type/action origin/target/weapon still match registration;
10. command is not `UnitAttackOfOpportunity`, a free/out-of-turn action, AI-created, foreign, stale, already terminal, or already adopted by another lease;
11. native pause, mode, UI wait, actor state, range/LoS, equipment, hands, target, and cooldown predicates remain authoritative;
12. the scheduler has not driven any command in the current Unity frame.

Any failed invariant refuses admission or faults the exact active lease. It never broadens eligibility.

## Drive invariants

- At most one active pair and one active lease exist.
- An exact command can receive at most one lease for its lifetime.
- At most one scheduler/native eligible drive of the leased command occurs per Unity frame.
- Start, first acted transition/resource charge, terminal result, finish event, and cleanup each occur at most once.
- A slot replacement or queue mutation invalidates the lease; the scheduler does not chase the replacement.
- The scheduler never advances rider commands, another companion, another mount, an AI command, or an AoO.
- The rider remains `CurrentTurn.Unit` across every before/after observation.
- The mount remains command executor, action actor, target actor, weapon/ability owner, animation actor, rule initiator, damage initiator, and resource owner.
- The scheduler never writes a campaign/save fact and holds no serializable component/part/state.

## First-action `Preparing` boundary

Exact Kingmaker begins a directly controllable rider turn in `Preparing`. It changes to `Acting` only after the rider has acted, the rider command container becomes nonempty, or the rider becomes unable to act. A mount-only command does not satisfy those stock observations.

Phase 3E may treat the exact registered pair command as eligible while the same rider turn is `Preparing`, but only inside the selected pair-local command seam. It must not mutate `TurnController.Status`, add a rider shell, add a rider command, or emit a turn event merely to cross this boundary. The native rider UI remains in its ordinary controllable-turn state. When a later rider command starts, stock `TurnController` transitions normally.

`TurnBasedCombatController.WaitingForUI` remains independently authoritative during this boundary. Registration/admission may precede UI readiness; no drive is counted until the first frame on which this native gate clears. Runtime must record both raw admission and first grant so non-actionable staging cannot be mistaken for scheduler latency.

## Resource and gameplay ownership

The scheduler does not charge resources. Exact `UnitActionController.TickCommand` detects `IsActed` transitioning false-to-true and calls native `UpdateCooldowns` on the command executor. Acceptance requires:

- mount Standard changes exactly once for mount primary;
- rider Standard/Move/Swift are unchanged by mount primary;
- attack/roll/damage initiator is the mount;
- weapon is the exact native mount natural weapon;
- animation belongs to the mount;
- at most one attack rule, one roll, and one damage event occur for the vertical slice;
- result and slot removal are native and terminal exactly once.

The Phase 3D KMC wrapper may remain the exact top-level Standard-slot command only while its child attack retains these mount-owned semantics. Telemetry must distinguish scheduler ownership from gameplay-action ownership.

## Turn completion

Existing pair-aware `ContinueActing` work may keep the rider turn open only while an exact lease is running or the valid mount retains an actually usable pair action. It must not keep a turn open merely because an ineligible/dead/stale mount has nominal cooldown.

Required completion behavior:

- active lease prevents native turn advancement;
- target death or invalidation cancels the not-yet-started second action;
- cancellation/interruption terminates only exact pair work;
- player end-turn exactly interrupts or disposes pending pair work before native end;
- when rider and mount are finished/ineligible, native rider turn advances once;
- natural `TurnController.Prepare` initializes rider stock state and mount paired ledger exactly once;
- no mid-turn reset, refresh, duplicate round event, duplicate initiative slot, or unrelated reorder occurs.

## Lifecycle cleanup

Registration/drive ends on exact relationship invalidation, rider-turn reference change, TB/RT change, combat end, target invalidation, slot replacement, save/load, area unload/transition, view detach, death/incapacitation, party removal, mod disable, update exception, or process exit where observable.

Cleanup sequence:

1. latch one named reason;
2. prevent any later drive;
3. interrupt only the exact live scheduler-owned command if native state permits;
4. verify terminal state and exact expected slot/queue removal without clearing foreign commands;
5. record pre/post rider and mount ledgers and `CurrentTurn.Unit`;
6. dispose references and event subscriptions idempotently;
7. request relationship cleanup or separate-turn fallback only at its already authorized safe boundary.

Transient failure does not rewrite persistent user settings. `EnablePairedCommandScheduler=false` remains available and inert.

## Telemetry contract

Every runtime row records:

- lease ID/state/generation and each transition frame;
- exact rider, mount, turn, command, executor, slot/queue, target, weapon/ability identities;
- mount awake-list membership and stock encounter count;
- scheduler/native drive source and at-most-once frame guard;
- start/act/finish/interrupt counts and terminal result;
- rider/mount ledgers before admission, first act, terminal, and cleanup;
- command, weapon, animation, rule, roll, damage, and movement ownership/cardinality;
- current-turn identity before/after every drive;
- turn roster/order/round and native turn-start event cardinality;
- cancellation/fault/cleanup reason and exact residue checks;
- fallback gate state and proof disabled mode is inert.

## Initial vertical-slice acceptance

Two fresh processes from one immutable package must each prove one in-range mount primary starts within two actionable game frames after admission (or a separately documented stock staging bound), is driven at most once per frame, starts once, produces one terminal result and one mount Standard charge, costs the rider nothing, emits no duplicate chain or native mount turn, leaves the rider current throughout, and cleans its exact slot/lease with zero unrelated effects.
