# Exact Kingmaker Turn-Based Command Lifecycle Map

Status: IN PROGRESS

## Authority and boundary

This map is for the exact installed Pathfinder: Kingmaker 2.1.7b `Assembly-CSharp.dll` only:

- SHA-256: `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`;
- MVID: `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`;
- length: `7,262,208` bytes;
- inspection tool: `ilspycmd 10.1.1.8388` plus exact reflection metadata-token checks.

No Wrath binary or source is used by production code. This document records bounded interoperability facts, not reconstructed proprietary source.

The immutable Phase 3D dev.21 runtime evidence remains at `C:\Dev\KingmakerMountedCombatLab\runtime-evidence\20260904T011000Z-phase3d-dev21-mammoth-tb-passA`. It proves a reference-exact KMC Mammoth Standard-slot wrapper stayed unstarted for 30 seconds while all recorded ordinary readiness predicates passed. The Phase 3E dev.1 observation at `C:\Dev\KingmakerMountedCombatLab\runtime-evidence\20260904T060000Z-phase3e-dev1-mammoth-tb-observation-passA` now resolves the one enumeration fact dev.21 did not record.

## Controlling question — answered

**Stock `UnitActionController` encounters the exact active mount command and rejects it at the turn-eligibility gate; it is not missing the mount during enumeration.**

The fresh dev.1 process recorded all of the following before its expected 30-second gameplay deadline:

1. the exact Mammoth was `IsAwake=True` and reference-present in `Game.Instance.State.AwakeUnits`;
2. the reference-identical KMC `MountedPairAttackCommand` remained in the Mammoth Standard slot with the Mammoth as executor and no queue entry;
3. the `TickCommandTurnBased` postfix encountered that exact object `2,485` times from Unity frames `3995` through `6479`;
4. stock returned false `2,485` times and true zero times;
5. the rider remained reference-exact `CurrentTurn.Unit`, and the native status remained `Preparing` rather than Acting/Ending;
6. `EnablePairedCommandScheduler=False`, existing admission overrides were zero, scheduler drives were zero, and the command never started, acted, finished, or acquired a result.

This selects **Option A — exact native eligibility extension**. Phase 3E will alter only the returned eligibility for one registered, reference-exact pair lease after independently preserving every other native hard gate and terminal side effect. It must handle both false inputs proven by the run—mount executor inequality and first-action rider `Preparing`—without changing `CurrentTurn.Unit`, writing `TurnController.Status`, invoking another native turn, or explicitly ticking the command.

The live gameplay row remains immutable `FAIL 49/1` at `AwaitOutcome`, as expected for an observation-only build. Its observation objective is `PASS`. The immediate independent postrun WhatIf audit passed exact zero mutation before these files were read.

## Dev.2 native UI-to-start ordering observation

Audited run `20260904T094306Z-phase3e-dev2-mammoth-tb-passA` records the exact Option A lifecycle under the installed build. `UnitCommands.Run` placed the leased command in the Mammoth Standard slot at Unity frame `3982`. `TickCommandTurnBased` continued to encounter it, but the preserved `TurnBasedCombatController.WaitingForUI` gate prevented an eligible return until frame `4293`. KMC granted that first actionable visit once, and stock observed native command start on frame `4294`. The command then completed through stock action/animation/rule/resource/result/slot behavior.

The raw `311`-frame admission-to-grant interval is native UI staging, not scheduler drive latency. The measured scheduler latency is first grant to start: one frame. Runtime acceptance binds both inequalities (`admission <= firstGrant <= start`) and permits at most two frames from first grant to start. The immutable dev.2 row remains `FAIL 69/1` solely because its diagnostic assertion used raw admission as the subtraction origin; dev.3 corrects the assertion without changing this lifecycle or any native gate.

Dev.3 independently repeated the lifecycle: admission `3987`, first eligibility grant `4309`, stock start `4310`, last drive `4542`, `234` unique-frame drives, one Success terminal, one mount Standard charge, zero rider cost, and exact mount command/rule/weapon ownership while the rider remained current. Its game result is `PASS 70/0`; the outer result remains immutable FAIL only because the first reachable PASS validator expected `actionActorCanActInCombat=false`. All exact dev.21/dev.2/dev.3 evidence records that ordinary readiness value true. Dev.4 corrects only that external expectation; it does not revise this lifecycle map or the selected seam.

## Dev.7 native rider-command attribution and natural-turn correction

Immutable audited run `20260904T195400Z-phase3e-dev7-horse-tb-gate2` is outer/game `FAIL 42/2` at `AwaitCombatMount`; it grants no Gate 2 credit. It nevertheless closes every previously observed fixture predicate before the rider Mount input: the pair was legally adjacent at `0.843796432m` within the `2.9m` bound, both exact reversible AI leases were active, both command containers were empty, the rider was exact `CurrentTurn.Unit`, and rider hands/equipment were idle. One player-created rider `UnitUseAbility` for Mount entered the exact Move slot with the exact Horse target, `CanStart=true`, `IsUnitEnoughClose=true`, no approach, no cooldown, and all recorded `ShouldStartCommand` predicates true. It remained unstarted for 30 seconds and completed only after cleanup restored real time. The independent audit ran before gameplay inspection and proved exact suite/save/Mods/Baseline/Working restoration with no process, lock, sentinel, or deployment residue.

Bounded exact inspection identifies two omitted native gates and one artificial-turn collision:

- `UnitActionController.TickCommandTurnBased` checks `CombatController.WaitingForUI` before executor/current-turn eligibility. `WaitingForUI.get` is `0x06000BFF`; its `CountingGuard.Value` / `GuardCount` getters are `0x06001C13` / `0x06001C15`. `InitiativeTrackerHorizontalPCView.UpdateUnits` `0x0600302B` acquires the guard through `PauseUnitActions(true)` `0x0600302D`; `WaitHoldedCharacter` `0x0600302C` releases it later. The diagnostic had not captured this predicate.
- `BaseUnitController.Tick` reaches only `State.AwakeUnits`, and `UnitActionController.TickOnUnit` returns before slot enumeration while the view rigidbody controller is active or `UnitEntityView.IsGetUp` is true. Exact observation therefore also requires rider `IsAwake`, reference membership in `AwakeUnits`, and the view gates (`IsGetUp` `0x0600183B`; `RigidbodyCreatureController.IsControllingRigidbody` `0x060014B0`).
- Native `CombatController.TickTime` `0x06000BD6` owns the pending `m_NextUnit` (`0x04000652`) transition: it calls `StartTurn(m_NextUnit)` and only then clears that field. The old diagnostic called public `StartTurn(rider)` directly while `CurrentTurn` was null, which creates a real rider turn but does not clear a separately pending `m_NextUnit`. That is not an exact natural-turn fixture and can collide with initiative/UI progression.

Dev.8 therefore makes no production scheduler change. The Horse TB diagnostic waits for Kingmaker to start the natural rider turn, requires `m_NextUnit == null`, `WaitingForUI == false` with `GuardCount == 0`, Default/unpaused mode, rider awake/enumerated, view tick eligibility, no nausea, exact selection, empty commands, and hands/equipment readiness for two stable frames. The existing exact-token postfix passively records the reference-identical rider Mount shell's stock eligibility result before any paired-scheduler extension. Schema-v2 deadline evidence distinguishes: no enumeration; enumerated but stock-ineligible with exact UI/turn state; or stock-eligible but blocked by `ShouldStartCommand`. It never changes the stock Boolean for this unmounted rider command.

## Per-frame controller ordering

`GameModesFactory.Initialize` (`0x06007E08`) registers controllers into the Default-mode `GameMode` array. `GameMode.Tick` (`0x06007E00`) invokes them in registration order and catches/logs each controller exception.

Relevant order within one Default-mode frame:

| Order | Controller | Exact effect relevant to Phase 3E |
|---:|---|---|
| 1 | `TurnBased.Controllers.CombatController` | Registered before unit movement and action controllers. Its `Tick` (`0x06000BD1`) advances the one native `CurrentTurn`, may choose the next unit, and may start a queued turn. |
| 2 | `UnitMoveController` and intervening unit controllers | Advance native movement, visibility, sleep, combat join/prepare, AI, cooldown, engagement, concentration, and activatable-ability state. |
| 3 | `UnitActionController` | `BaseUnitController.Tick` (`0x0600910B`) visits awake units; `TickOnUnit` (`0x0600911C`) advances their raw command slots. |
| 4 | `UnitHandEquipmentController`, projectile/ability controllers, `UnitAnimationController` and later controllers | Finish equipment, projectile, ability, life, and animation work after command dispatch in the registered frame order. |

KMC's UMM update callback is not treated as a substitute for this native order. Any implementation must prove its exact position and avoid a second tick if the stock controller has already advanced the leased command in the same Unity frame.

## Native turn controller

| Type/member | Token | Caller | Key callees / side effects |
|---|---:|---|---|
| `CombatController.CurrentTurn.get` | `0x06000BBE` | UI, command eligibility, KMC observation | Returns the single native `TurnController`. |
| `CombatController.CurrentTurn.set` | `0x06000BBF` | combat-controller lifecycle | Disposes the previous native controller; not a Phase 3E patch seam. |
| `CombatController.Tick` | `0x06000BD1` | `GameMode.Tick` | Updates navigation grid; calls `CurrentTurn.Tick`; calls `ChooseNextUnit` when no active turn/turn ended; disposes ended turn. |
| `CombatController.TickTime` | `0x06000BD6` | time/combat flow | Starts the queued `m_NextUnit` after native delay or advances combat timing. |
| `CombatController.m_NextUnit` | `0x04000652` | `ChooseNextUnit`, `TickTime`, reset/initiative changes | Stores the one pending natural combatant. `TickTime` clears it only after its own `StartTurn(m_NextUnit)` call; an unrelated direct `StartTurn` does not clear it. |
| `CombatController.ChooseNextUnit` | `0x06000BD2` | `Tick` | Chooses from native initiative roster; Phase 3D filters only the exact active mount candidate. |
| `CombatController.StartRound` | `0x06000BD3` | initiative advancement | Advances the native round and prepares sorted initiative participation. |
| `CombatController.StartTurn(UnitEntityData)` | `0x06000BDA` | `TickTime` and diagnostics | Constructs one `TurnController(unit)`, performs native navigation setup, then calls `TurnController.Start`; emits a real native turn. Phase 3E must never call it for the paired mount. |
| `CombatController.IsInTurnBasedCombat` | `0x06000BF6` | controller and command gates | Requires player combat, TB setting, and non-cutscene state; also clears weapon-change state outside TB. |
| `CombatController.CurrentUnit.get` | `0x06000BFA` | `UnitEntityData.IsCurrentUnit` | Projects `CurrentTurn?.Unit`; this is the exact stock executor identity authority. |
| `CombatController.WaitingForUI.get` | `0x06000BFF` | turn/action/time ticks | Returns the reference-counted UI pause guard. Any positive count blocks both `CombatController.Tick` and every TB command in `TickCommandTurnBased`. |
| `CountingGuard.Value / GuardCount` | `0x06001C13 / 0x06001C15` | UI owner and diagnostics | `Value` is exactly `m_GuardCount > 0`; `GuardCount` exposes the exact count. Assigning true increments and false decrements, so it is not an idempotent Boolean. |

`TurnController` (`0x020000D3`) stores exactly one `Unit`, that unit's `UnitCommands`, `UnitCombatState`, and cooldown ledger. It has no native paired-actor collection.

| Member | Token | Exact behavior and side effects |
|---|---:|---|
| constructor | `0x06000C2C` | Binds one unit and its command/combat/cooldown objects. |
| `Tick` | `0x06000C34` | Keeps the bound unit selected; transitions `Scrolling -> Prepare`; transitions `Preparing -> Acting` only if the **bound rider** has acted, has a nonempty command container, or cannot act; evaluates `ContinueActing`; transitions Ending to End; updates UI/predictions/movement. |
| `Start(bool)` | `0x06000C3A` | Begins the one native unit's turn lifecycle and camera/scroll path. |
| `Prepare` | `0x06000C3C` | Interrupts bound-unit commands, clears only its cooldown ledger, recharges surviving acting commands, resets AoO state, calls `OnNewRound`, emits `IUnitNewCombatRoundHandler`, ticks AI/facts, sets controllable units to `Preparing`, and emits `ITurnBasedModeHandler.HandleTurnStarted(Unit)`. |
| `ContinueActing` | `0x06000C3D` | Considers only bound-unit commands, motion, action time, extra actions, and AI wait state. It cannot see a mount-only command. |
| `ContinueWaiting` | `0x06000C3E` | Waits for the bound unit's running command or ending delay. |
| `ToEnd` | `0x06000C45` | Sets status `Ending`. |
| `End` | `0x06000C46` | Sets bound Standard to `6`, caps bound Move at `6`, sets `Ended`, and interrupts bound-unit commands. |
| `ForceToEnd(bool)` | `0x06000C47` | Optionally consumes all bound resources, then enters Ending. Not a mount-scheduler seam. |
| `CanEndTurn` | `0x06000C4A` | Allows a directly controllable bound unit to end from Preparing or Acting. |
| `IsActed` | `0x06000C4D` | Reads only bound-unit movement and Standard/Move/Swift cooldowns. |
| `HandleUnitCommandDidStart` | `0x06000C5D` | Updates action UI and camera only when `command.Executor == Unit`. A mount command must not masquerade as rider-owned merely to trigger this. |
| `HandleUnitCommandDidEnd` | `0x06000C5E` | Ignores every command whose executor is not the bound unit; otherwise updates movement/action UI. |
| `InterruptCommands` | `0x06000C62` | Interrupts only bound-unit commands, with a Kineticist exception path. |

Phase 3E implication: a mount-only command entered while the rider remains untouched leaves the native turn in `Preparing`, because `TurnController.Tick` observes only the rider's empty container and rider ledger. Extending only `executor == CurrentTurn.Unit` is insufficient unless exact pair-local eligibility also handles this first-command `Preparing` boundary without emitting another turn.

## Native command container

`UnitCommands` is `0x02000503`. Each unit owns a fixed raw slot array plus one linked queue.

| Member | Token | Admission/container effect |
|---|---:|---|
| `Raw.get` | `0x0600269A` | Exposes the fixed per-command-type array used by `UnitActionController.TickOnUnit`. |
| `Move.get` | `0x0600269F` | Returns only a live `UnitMoveTo` from Move slot; other Move-type commands require `GetCommand`. |
| `Free.get` / `Standard.get` / `Swift.get` | `0x060026A2` / `A3` / `A4` | Return live typed slot values through `GetCommand`. |
| `Queue.get` | `0x060026A5` | Exposes per-unit linked queue. |
| `Empty.get` | `0x060026A6` | True only when every raw slot is null. |
| constructor | `0x060026A7` | Binds the owner and creates one slot for every `CommandType`. |
| `GetCommand(CommandType)` | `0x060026A9` | Returns the live exact raw-slot command or null. |
| `Contains` | `0x060026AA` / `0x060026AB` | Tests the exact container/queue membership paths used by KMC identity checks. |
| public `Run(UnitCommand)` | `0x060026B2` | Calls private Run with `fromQueue=false`. |
| private `Run(UnitCommand,bool,bool)` | `0x060026B3` | Rejects unconscious owner; initializes unbound executor or rejects executor/owner mismatch; ordinarily clears queue; merges or interrupts paired slot; assigns exact raw slot; updates combat target; calls `OnRun`; emits `IUnitRunCommandHandler.HandleUnitRunCommand`. |
| `RemoveFinishedAndUpdateQueue` | `0x060026BD` | Clears each finished raw slot, then promotes only the queue head when its own and paired Standard/Move slots are free. |
| `InterruptAll` | `0x060026C0` / `0x060026C1` | Interrupts matching raw/queued commands and removes them; scheduler cleanup must never call a broad overload on unrelated commands. |

Standard and Move are paired by stock container admission. A new command can interrupt the same slot and its paired Move/Standard command. A lease is therefore valid only while its command remains reference-identical to the expected raw slot or explicitly expected queue node.

## Native unit action controller

`BaseUnitController` is `0x02001479`; `UnitActionController` is `0x0200147C`.

| Member | Token | Caller | Callees, predicates, and side effects |
|---|---:|---|---|
| `BaseUnitController.Tick` | `0x0600910B` | `GameMode.Tick` | Iterates `State.AwakeUnits` because `TickSleeping == false`; calls private `TickUnit`. |
| `BaseUnitController.TickUnit` | `0x0600910C` | `Tick` | Applies `ShouldTickOnUnit`; catches/logs per-unit exceptions. |
| `BaseUnitController.ShouldTickOnUnit` | `0x0600910E` | `TickUnit` | Excludes dead units. |
| `UnitActionController.TickOnUnit` | `0x0600911C` | `TickUnit` | Interrupts/clears `PreviousCommand`; skips rigidbody/get-up; iterates every non-null `Commands.Raw` entry; calls `TickCommand`; updates movement ordering; calls `RemoveFinishedAndUpdateQueue`; stops view movement when container empty; catches/logs exceptions. |
| `TickCommandTurnBased` | `0x0600911D` | `TickCommand` | Exact TB eligibility and forced-finish gate described below. |
| `TickCommand` | `0x0600911E` | `TickOnUnit` | Returns if TB gate false; validates target; interrupts; ticks approach; calls `ShouldStartCommand` then `Start`; for running commands turns, ticks, detects first `IsActed`, calls `UpdateCooldowns`, and applies terminal interrupt rules. |
| `ShouldStartCommand` | `0x0600911F` | `TickCommand` | Enforces hands, movement finish, `CanStart`, exact range/LoS, state, equipment update, combat cooldown, and nausea. Move/area transition have their native early path. |
| `UpdateCooldowns` | `0x06009120` | `TickCommand` | In TB, calls `command.Executor.UpdateCooldowns(command)`; in RT writes the **executor's** Move/Standard/Swift cooldown. |

### Exact `TickCommandTurnBased` decision

1. Outside TB: return true.
2. When `TurnBasedCombatController.WaitingForUI`: return false.
3. `UnitAttackOfOpportunity`: eligible unconditionally through this identity gate.
4. No current turn: eligible only when executor is not in combat.
5. Otherwise: initial flag is `executor.IsCurrentUnit() && (currentTurn.IsActing || currentTurn.IsEnding)`.
6. For an initially eligible unstarted command, spell-combat and unavailable-ability checks may interrupt it.
7. For any in-range, non-prone, unstarted, `Result=None` command, failed `CanAct`, nausea, `CanActInCombat`, or command cooldown forces TB success, clears the executor queue, and returns false.
8. A stalled approach with a positive next-approach time and no movement also forces TB success, clears the executor queue, and returns false.
9. Otherwise return the initial eligibility flag.

The equality authority is `UnitEntityData.IsCurrentUnit` (`0x0600838E`), whose exact body is `this == CombatController.CurrentUnit`. It is a passive read, but the enclosing predicate also requires Acting/Ending. Phase 3E must preserve every other condition and forced-finish side effect.

## Base command lifecycle and events

`UnitCommand` is `0x02000512`.

| Stage | Member/token | Exact effect |
|---|---|---|
| construction | constructor `0x06002799` | Stores command type and target; records cutscene context. |
| binding | `Init` `0x0600279C` | Sets exact executor. |
| admission | `UnitCommands.Run` `0x060026B2/B3` | Assigns exact owner slot or queue path, invokes `OnRun`, emits `IUnitRunCommandHandler`. |
| approach | `TickApproaching` `0x060027A6` | Advances native approach/path requests and movement state. |
| start | `Start` `0x060027A5` | Rejects double start, invalid target, or failed exact range; sets `IsStarted`; calls `OnStart`; emits `IUnitCommandStartHandler`. |
| update/action | `Tick` `0x060027A7` | Increments `TimeSinceStart`, calls `OnTick`, detects animation act/no-animation act, calls `OnAction`, sets `IsActed`, emits `IUnitCommandActHandler`, and ends when result plus animation are terminal. Commands over nine seconds are interrupted except continuous movement. |
| forced TB terminal | `ForceFinishForTurnBased` `0x060027AB` | Marks an unacted in-combat executor acted, assigns non-None result, and calls `OnEnded`. |
| interrupt | `Interrupt(bool)` `0x060027AC` | Assigns Interrupt once and calls `OnEnded`; repeated calls are inert after finish. |
| finish | `OnEnded(bool)` `0x060027B2` | Sets `IsFinished`, releases animation, ends attack equipment state, and emits `IUnitCommandEndHandler` unless suppressed. |
| removal | `UnitCommands.RemoveFinishedAndUpdateQueue` `0x060026BD` | Removes terminal slots and may promote one queue head. |

Result assignment occurs inside `Tick` from `OnAction`, inside forced finish, or inside interrupt. `Start`, `Tick`, and `OnEnded` must each occur at most once for a leased command. Resource charging occurs on the transition from `IsActed=false` to true inside `UnitActionController.TickCommand`, not in `UnitCommands.Run` or `Start`.

## Concrete command ownership

| Type | Type token | Relevant method tokens | Lifecycle responsibility |
|---|---:|---|---|
| `UnitAttack` | `0x02000501` | ctor `0x06002678`; `Init` `0x06002679`; `OnStart` `0x0600267E`; `OnTick` `0x06002680`; `OnAction` `0x06002681`; `GetApproachRadius` `0x06002685`; `CreateSingleAttack` `0x0600268C` | Builds native attack sequence, owns weapon/animation timing, calls the native attack rule at action, and supports iterative attacks. |
| `UnitAttackOfOpportunity` | `0x02000502` | ctor `0x06002696`; `Init` `0x06002697`; `OnRun` `0x06002698`; `OnAction` `0x06002699` | Free/out-of-turn special command. It bypasses current-turn identity in `TickCommandTurnBased`; Phase 3E leases exclude it. |
| `UnitMoveTo` | `0x0200050C` | ctors `0x060026FE/FF`; `OnRun` `0x06002701`; `OnAction` `0x06002702` | Native path/motion command. Existing `ExactTurnMovementAdapter` remains authoritative until exact evidence requires scheduler involvement. |
| `UnitUseAbility` | `0x02000510` | ctors `0x06002726/27`; `Init` `0x06002728`; `OnStart` `0x0600272D`; `OnTick` `0x06002734`; `OnAction` `0x06002737`; `OnEnded` `0x06002738` | Native ability, spell, target, animation, and completion lifecycle. It is outside the minimal mount-primary slice. |

Phase 3D's exact mount-primary top-level object is an original KMC `MountedPairAttackCommand` in the mount Standard slot. It creates a mount-owned native single-attack child and never makes the rider the weapon/rule initiator. Phase 3E may schedule only the reference-identical registered wrapper and must retain that ownership chain.

## Action-resource ownership

`UnitEntityData.UpdateCooldowns(UnitCommand)` (`0x0600838F`) charges the executor:

- Move: `MoveAction += 3`;
- Standard: `StandardAction += 6`, and a full-round command also adds `MoveAction += 3`;
- Swift: `SwiftAction += 6`;
- Free does not charge a TB action.

`HasStandardAction`/`HasMoveAction`/`UsedStandardAction`/`UsedTwoMoveAction` remain native ledger queries. The scheduler must not write or mirror rider/mount Standard, Move, Swift, weapon, animation, or rule ownership. It observes a single native charge caused by the mount command's first acted transition.

## Pause, mode, turn completion, and exception behavior

- The Default game mode and `IsInTurnBasedCombat` must remain true for paired scheduling.
- `WaitingForUI` remains a hard false gate.
- A game pause prevents normal controller progress through the containing game loop; KMC does not bypass it.
- `TurnController.ContinueActing` and `IsActed` see only the rider. Existing Phase 3D `ContinueActing` postfix may keep the rider turn open for exact viable mount state, but it must be narrowed to active/eligible pair work and cannot create a second clock.
- Player end-turn input uses the real rider `TurnController`; an active exact lease must either finish or be exactly interrupted before native advancement.
- `Prepare` is the sole natural rider-led new-round boundary. Phase 3D's mount-ledger preparation must execute once per reference-exact rider turn, never mid-turn and never on a synthetic mount turn.
- `BaseUnitController.TickUnit`, `UnitActionController.TickOnUnit`, and `GameMode.Tick` catch/log exceptions. Phase 3E cannot rely on those catches for lease cleanup; scheduler code must fault, interrupt only its exact command, dispose idempotently, and preserve unrelated commands.

## Observation checkpoint result

Status: PASS

The single authorized observation checkpoint added no scheduler drive. For the exact KMC mount-primary wrapper it recorded:

- command reference identity and type;
- executor and exact mount identity;
- raw Standard-slot identity and queue identity;
- current rider turn identity and status;
- mount `IsAwake` and `State.AwakeUnits.Contains(mount)`;
- exact `TickCommandTurnBased` encounter count, first/last Unity frame, and stock result before any KMC eligibility override;
- existing KMC override result/count;
- command started/running/acted/finished/result state;
- zero gameplay mutation beyond the already authorized diagnostic action attempt.

Result identities:

- branch/commit/version: `codex/mounted-combat-phase3e-paired-scheduler` / `62ce96b0618ff1d9074bfcdafd0d42ce14df3406` / `0.1.0-phase3e-dev.1`;
- package SHA-256: `17bc227dd6a1c89e66f9d20009f1f28c470cce7f4dea5663f13178de866ed3b5`;
- DLL SHA-256/MVID: `f684b85baaea3f9fe53fc5a48ef87a6d85e4895795d1bad1c52d22b343be7d95` / `15ea753e-0f0d-4dc4-b0f5-9a7be9926dbf`;
- suite snapshot/SHA-256: `20260904T045000Z-phase3e-dev1-command-lifecycle-observation-suite1` / `290f7fcf7820711a9e31ba4226dc99842315684ff864f0471826f3599ff1c211`;
- live evidence/game/combat/orchestration SHA-256: `20260904T060000Z-phase3e-dev1-mammoth-tb-observation-passA` / `42351c1b67ffc3e21956abeb13acb20d1c7f3d50d11b909e805f4b498426ceee` / `d9b3d9e50ebd0f54b1ce0393093ea552cab77ddfb01a7dae4745c126d5149b97` / `04b459ca62bf79b97f413ae062bb1c93b8a834d3c91d64ed3262c3fb489edf0a`.
