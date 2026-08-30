# Phase 3D Combat Mount/Dismount Contract

Status: IN PROGRESS

## Product rule

During combat, Mount Companion and Dismount each cost the rider one Move action. Rider and supported owned mount must be adjacent, alive, conscious, directly controllable, in the active area, body-compatible, and free of a lifecycle transition. Faster/free mounting via Ride check is outside this milestone.

Outside combat, the existing action remains free and retains its Phase 3C behavior.

## Native control surface

The KMC-owned Mount Companion and Dismount blueprints currently use `CommandType.Free`; Phase 3D assigns `CommandType.Move` so Kingmaker performs native action admission and cooldown charging in combat. The player-action evaluator must no longer reject combat categorically. It instead reports exact adjacency, turn, action, body, ownership, agent, or lifecycle failures.

Mount requires the rider to own the current turn in TB and have a Move action. Dismount uses the rider-led current turn while mounted. RTWP uses the rider's native Move cooldown. Rejected or canceled targeting performs no transition and no charge.

## Transition accounting

The relationship transition itself must not clear cooldowns. Before/after snapshots bind rider and mount Standard, Move, Swift, and Initiative values. The successful ability activation may charge exactly one rider Move action; neither actor receives refreshed movement, Standard action, initiative, or an immediate extra turn.

Mid-combat mount invokes the shared-initiative merge contract. Mid-combat dismount invokes pending split at the next safe native round boundary. Out-of-combat transitions do neither.

## Rider Primary isolation

Every native mounted ability activation records kind, blueprint GUID, caster, selected unit, target-selection state, relationship before/after, lifecycle ledger sequence, view identity, game/combat mode, transition identity, and cleanup reason. Rider Primary, rejection, cancel, miss, hit, and completion are non-lifecycle outcomes and cannot dismount.

## Exact Wrath reference disposition

Exact local Wrath `ContextActionMount` and `ContextActionDismount` perform relationship mutation, while cost is carried by the enclosing ability/command surface. Phase 3D follows that separation but uses the conservative user-authorized rider Move cost rather than importing a Wrath blueprint or asset.
