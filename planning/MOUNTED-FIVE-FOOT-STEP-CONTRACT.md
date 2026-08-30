# Phase 3D Mounted Five-Foot-Step Contract

Status: IN PROGRESS

## Root cause

Installed Kingmaker `TurnController.TickMovement` `0x06000C37` treats `MovementLimit.FiveFootStep` as a native step: it tracks a maximum `7.5 ft`, does not increment ordinary Move action, and sets `ImmuneAttackOfOpportunityOnDisengage` for the tick. `UnitCombatState.ShouldAttackOnDisengage` applies that immunity only when the moving target satisfies `UnitEntityData.IsCurrentUnit()`.

In the Phase 3C model the rider is `CurrentUnit` while the mount physically moves. The mount therefore fails the stock current-unit identity test even though the rider turn is in native five-foot-step mode. That identity mismatch explains the observed AoO; it is not evidence that all mounted movement should suppress AoOs.

## Repair seam

The existing exact `UnitCombatState.AttackOfOpportunity(UnitEntityData,bool)` prefix may suppress only when every condition is true:

- relationship is the exact active pair;
- target is the exact mount physically moving for the current rider-led turn;
- the current movement command is the exact pair command;
- current turn movement limit is native `FiveFootStep`;
- the pair has not exceeded the native step distance;
- the disengage is caused by that exact step;
- no ordinary movement has already disqualified the step.

Ordinary movement, attacks, unrelated actors, unmounted units, and a mounted pair outside that exact step state continue through stock AoO logic.

## Resource and distance behavior

Physical movement and pathfinding belong to the mount. Step distance is measured from the mount agent and capped at the native installed distance. The mount ordinary Move cooldown does not increase for a step. Only one step is available. A step after ordinary movement is rejected; ordinary movement after a step remains stock-restricted. Ordinary mounted movement spends mount movement resources and provokes when stock rules say it should.

## Required control

Runtime qualification places an adjacent hostile with a valid AoO and proves all four outcomes in the same exact package: mounted ordinary movement can provoke, mounted five-foot step does not, unmounted five-foot step remains stock, and no global AoO suppression exists. Distance, movement-limit, cooldown, opportunity request/result, and suppression reason are recorded per row.
