# Phase 3D Combat Mount/Dismount Contract

## Chunk 6A disposition — 2026-09-24 (supersedes the DEFER below)

Status: `IMPLEMENTED — CHUNK 6A`. The owner's Chunk 6A mission authorizes legal
voluntary combat Mount and requalified voluntary combat Dismount through normal
native controls on the accepted paired-activation architecture. The historical
`DEFER — EVIDENCED` disposition below is preserved as the record of why the
Phase 3D/3E attempts stopped; it is no longer the current status.

What changed relative to that history is the evidence, not the ambition. The
Phase 3E blocker was the unified/scheduler turn authority, which no longer runs:
paired activation is the accepted authority and both legacy experiments are off.
The domain guard the dev.9 run recorded rejecting the transition
(`Private-alpha mounting is available only outside combat.`) is replaced in 6A
by an explicit admission mode rather than removed, so exploration, voluntary
combat and saved restore each remain separately testable and a restore flag can
no longer authorize gameplay.

The full frozen contract — the exact native cost machinery, the commitment
boundary, and the transition-round participation rule with its installed-IL
evidence — lives in [planning/CHUNK6-ACTION-CONTRACT.md](CHUNK6-ACTION-CONTRACT.md)
and is not duplicated here. Three points from it correct or sharpen statements
made further down this page:

- **The out-of-combat transition is free because native code charges nothing
  outside combat.** `UnitActionController.UpdateCooldowns` `0x06009120` returns
  without writing when `Executor.IsInCombat` is false. The historical wording
  "free exploration transition" is therefore native behavior, not a KMC
  exception, and no code path treats exploration specially for cost.
- **The commitment boundary is the acted transition, after approach.**
  `TickCommand` `0x0600911E` calls `UpdateCooldowns` only on the `IsActed`
  false-to-true transition produced at `IL_0181` of `UnitCommand.Tick`
  `0x060027A7`, and `TickApproaching` `0x060027A6` has already run by then.
  Cancelling during approach or before commitment costs nothing; once committed
  the cost stands and KMC performs no refund even when a later revalidation
  legitimately refuses the relationship transition.
- **"Mid-combat mount invokes the shared-initiative merge contract" is
  superseded.** There is no merge. `TurnController.Prepare` `0x06000C3C` calls
  `Cooldowns.Clear` `0x0600C3BE` and is itself the per-round grant, so no
  relationship transition may call it. Mid-encounter pair creation instead uses
  one explicit typed adoption operation that takes over the rider's already
  running native turn as the activation boundary through pure bookkeeping, and
  disposes of the mount's transition round from the exact positional observable
  in `CombatController.ChooseNextUnit` `0x06000BD2`: prepared once as the paired
  partner when its own slot is still pending, or left exactly as it stands when
  its slot has already been taken. Mid-combat dismount continues to use the
  accepted pending-split boundary, which the paragraph below describes correctly.

## Phase 3E final disposition

Status: `DEFER — EVIDENCED` for unified-TB combat Mount/Dismount.

The scheduler/turn-completion prerequisite failed under K9, so Tranche 7 was not opened. The existing outside-combat Mount path and accepted RT Dismount behavior remain intact. No Phase 3E claim is made for rider Move cost, upcoming/past native-slot reconciliation, no-extra-turn behavior, or same-round split after combat Dismount. Fallback `0.1.0-phase3e-fallback.1` uses Phase 3C separate turns and both unified experiments default off.

## Phase 3E dev.11 ordering boundary

Dev.10 used an already-qualified out-of-combat mount solely to reach the earlier scheduler/turn-completion tranches. Its audited failure occurred at redundant Horse turn selection, not combat Mount/Dismount. Dev.11 repairs only that exact selection timing. Combat Mount/Dismount remains `TODO` and the out-of-combat setup consumes no combat action or row credit; implementation remains gated until scheduler sequencing plus ordinary TB melee/ranged pass.

## Phase 3E dev.9 ordering boundary

Immutable audited run `20260905T010000Z-phase3e-dev9-horse-tb-gate2` proves the native rider Move-slot Mount shell itself is visited, starts, and terminates `Success` under the natural rider turn. Exact installed logging then records `MountedPairCandidate.Validate` rejecting the transition with `Private-alpha mounting is available only outside combat.` That domain guard remains intentionally unchanged: combat Mount/Dismount implementation and qualification are Tranche 7 work and may begin only after scheduler sequencing and turn-completion gates pass. Dev.10 uses a native out-of-combat mounted pair solely as earlier-tranche diagnostic setup; it assigns no combat-Mount PASS and consumes no rider combat action for setup.

Phase 3E superseding status (2026-09-04): `IN PROGRESS`. Dev.8 proved exact natural rider-turn readiness and one native Mount click/cast admission, but its diagnostic stopped in the admission frame on the incorrect assumption that stock `UnitUseAbility.CreatedByPlayer` would be true. Dev.9 requires the exact stock false flag, null AI action, and native click/cast events. No Phase 3E combat Mount/Dismount resource, no-extra-turn, split-boundary, or invalidation row is credited yet.

Status: DEFER — EVIDENCED

## Final Phase 3D disposition — 2026-09-04

Dev.19 proved the exact adjacent rider/Horse pair, selected actionable rider turn, untouched separate ledgers, and visible/enabled combat Mount ability before a harness-only dormant Horse-command wait prevented input. Clean dev.17 RT A/B passed native combat Dismount delivery with one rider Move charge and exact cleanup, but the TB no-extra-turn split was not separately credited. The subsequent dev.21 scheduler blocker prevents qualification of the merged turn that combat Mount would create. No action refresh, automatic remount, or synthetic shared resource pool was added.

## Product rule

During combat, Mount Companion and Dismount each cost the rider one Move action. Rider and supported owned mount must be adjacent, alive, conscious, directly controllable, in the active area, body-compatible, and free of a lifecycle transition. Faster/free mounting via Ride check is outside this milestone.

Outside combat, the existing action remains free and retains its Phase 3C behavior.

## Native control surface

The KMC-owned Mount Companion and Dismount blueprints use `CommandType.Move`, so Kingmaker performs native action admission and cooldown charging in combat. The player-action evaluator does not reject combat categorically. It reports exact adjacency, turn, action, body, ownership, agent, or lifecycle failures.

Mount requires the rider to own the current turn in TB and have a Move action. Dismount uses the rider-led current turn while mounted. RTWP uses the rider's native Move cooldown. Rejected or canceled targeting performs no transition and no charge.

The ordinary availability provider performs the complete pre-input check, including rider Move availability. Exact installed `UnitActionController.TickCommand` then calls `UpdateCooldowns` when the admitted shell changes from not acted to acted; RTWP maps that Move shell to `MoveAction = 3 - TimeSinceStart`. Custom ability delivery can occur asynchronously after that native resource commitment. The exact Mount/Dismount delivery callback therefore sets `NativeMoveActionShellAdmitted`: this suppresses only a duplicate post-commit `RiderHasMoveAction` rejection. It does not bypass turn, adjacency, identity, ownership, body, life, control, lifecycle, selection, game-mode, view, agent, or target gates. The native Move shell remains the sole cost owner; KMC never writes, refunds, or refreshes a cooldown.

## Transition accounting

The relationship transition itself must not clear cooldowns. Before/after snapshots bind rider and mount Standard, Move, Swift, and Initiative values. The successful ability activation may charge exactly one rider Move action; neither actor receives refreshed movement, Standard action, initiative, or an immediate extra turn.

Mid-combat mount invokes the shared-initiative merge contract. Mid-combat dismount invokes pending split at the next safe native round boundary. Out-of-combat transitions do neither.

## Rider Primary isolation

Every native mounted ability activation records kind, blueprint GUID, caster, selected unit, target-selection state, relationship before/after, lifecycle ledger sequence, view identity, game/combat mode, transition identity, and cleanup reason. Rider Primary, rejection, cancel, miss, hit, and completion are non-lifecycle outcomes and cannot dismount.

## Dev.13 runtime attribution and dev.14 repair

Audited RT run `20260902T102000Z-phase3d-dev13-rt-passA` passed every reached Phase 3D row through TB-to-RT reconciliation, then failed only at combat Dismount. Immediately before input the relationship was `Mounted`, the rider was selected in RTWP, rider Move cooldown was zero, and `hasMove=true`. One exact Dismount selection/cast request was admitted. Delivery logged `DispatchStarted` followed by `accepted=False`; the relationship stayed mounted until fail-closed scenario cleanup. This immutable aggregate remains uncredited because it ended `FAIL` (`28/1` Phase 3D rows).

The root cause was pair-local double gating: the delivery handler repeated the Move-resource condition after its exact native shell had already committed that resource. Dev.14 implements the admitted-shell context above, adds direct pre/post cooldown, shell-slot, activation, relationship, intent, and command evidence, and requires exactly one accepted relationship-ending delivery with rider Move cooldown `2.5..3.01` and no residual mounted command. Offline regression and protocol gates pass; fresh clean-package RT/TB evidence remains required.

Audited dev.14 RT A `20260902T153000Z-phase3d-dev14-rt-passA` proves this repair at its exercised boundary inside an otherwise failed aggregate. One native Dismount shell produced one accepted delivery, exact manual `Mounted -> Unmounted`, rider Move cooldown `2.97256064`, no action refresh, and no residual pair command/intent. The diagnostic initially reported `inMoveSlot=false` because `UnitCommands.Move` is a typed `UnitMoveTo` view and cannot return a Move-slot `UnitUseAbility`. Exact installed `UnitCommands.GetCommand(CommandType.Move)` returns the raw slot; dev.15 corrects that observation and retains the strict Move-slot requirement. No additional Dismount production change is authorized by dev.14.

## Exact Wrath reference disposition

Exact local Wrath `ContextActionMount` and `ContextActionDismount` perform relationship mutation, while cost is carried by the enclosing ability/command surface. Phase 3D follows that separation but uses the conservative user-authorized rider Move cost rather than importing a Wrath blueprint or asset.
