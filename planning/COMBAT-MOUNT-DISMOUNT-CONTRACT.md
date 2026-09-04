# Phase 3D Combat Mount/Dismount Contract

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
