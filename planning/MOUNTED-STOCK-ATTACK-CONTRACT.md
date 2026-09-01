# Phase 3D Mounted Stock Attack Contract

Status: IN PROGRESS

## Native admission seam

Installed Kingmaker `ClickUnitHandler.OnClick` `0x060093ED` constructs an ordinary stock `UnitAttack` for each selected unit and submits it through `UnitCommands.Run` `0x060026B2`. Phase 3C intentionally rejected exact mounted-pair stock `UnitAttack` commands in `MountedCombatController.ShouldAllowStockCommand`; that rejection is the proven cause of the silent hostile-click behavior.

Phase 3D must accept the actual stock-created rider `UnitAttack` as the player input. The exact `UnitCommands.Run` prefix may consume that command only after recording its owner, target, weapon, target-selection state, and `CreatedByPlayer` identity, then establish a bounded mounted attack intent. It must not synthesize credit from a downstream controller call.

Explicit Rider Primary and Mount Primary use a related but distinct native admission seam. Their stock `UnitUseAbility` commands are Free intent shells; the mounted child `UnitAttack` remains the actor-owned Standard command. Exact Kingmaker `UnitUseAbility.Init` normally gives the shell `NeedLoS=true`, which can strand pre-dispatch approach on the rider's disabled movement agent. Exact RTWP `UnitActionController.UpdateCooldowns` also maps an acted Free shell to rider Move cooldown, unlike TB's no-cost Free handling. Phase 3D sets `NeedLoS=false` and `IgnoreCooldown=true` only on reference-identical KMC primary shells cast by the exact active rider in the exact mounted pair. The shell still must be admitted through the selected-ability handler, and the child transaction retains native approach-range/LoS and sole actor-owned Standard resource consumption. No ordinary stock hostile-click command and no foreign ability receives this adapter.

## Principal and ownership

- The rider is the only required selected unit.
- The mount owns approach pathfinding and physical movement.
- Rider attacks execute as rider-native `UnitAttack` children and spend rider cooldowns.
- Mount primary attacks execute as mount-native `UnitAttack` children and spend mount cooldowns.
- One input may create at most one rider attack and one mount attack per legal action/cooldown opportunity.
- Explicit Rider Primary and Mount Primary remain single-actor alternatives.

## Melee behavior

For a rider melee weapon, the pair approaches to a mount-origin position satisfying the rider's native reach bridge. The deterministic shared-turn default is rider first, then mount when its exact primary natural attack and Standard action are legal. Native full-attack/iterative behavior is not claimed unless reused without duplicate rules or costs.

The delegated `UnitMoveTo` targets a world point and therefore must not require LoS. Exact installed `UnitCommand.IsUnitEnoughClose` applies `NeedLoS` through `GetTargetLOSObjectId`; a point target returns object ID zero, allowing the hostile itself to remain an unignored blocker after the mount has physically reached the point. The delegated point move uses `NeedLoS=false`; the native child attack alone retains rider-to-hostile `HasLOS`, range, weapon, and rule admission. This is not a global LoS bypass.

Qualification isolates explicit-primary damage from persistent stock-melee damage. The explicit Rider and Horse primary controls first retire and verify their bounded diagnostic target; the normal hostile-click control then receives a distinct target and may begin only after two stable frames prove the rider principal, a native melee weapon, bidirectional hostility/combat memory, both independent Standards ready, idle rider/Horse/target command and animation surfaces, idle rider/Horse equipment updates, healthy mounted pose, and no active pair command, ground movement, exact mount movement, or stock intent. PASS evidence binds distinct target IDs, successful prior cleanup, exactly one native hostile click/request/intent, positive Horse approach, at least two rider and one Horse dispatch, matching native attack/roll/damage cardinality, zero duplicates, retained relationship, and exact cancellation. A reused durable target cannot qualify persistent intent.

## Persistent RTWP intent

One real hostile click records one target intent. When no child command is active, the controller may submit a legal rider attack and then a legal mount primary according to their independent native cooldowns. Intent survives individual successful attacks and ordinary cooldown waits. It ends on Stop/Hold, new ground/target command, explicit cancellation, target death/invalidation, relationship invalidation, mode/lifecycle boundary, or command rejection. No attack may start after cancellation.

## Turn-based behavior

The same stock input is admitted only during the rider-led shared turn. Available rider and mount Standard actions are evaluated and consumed independently. The player can interrupt the automatic two-actor sequence by cancellation or use an explicit primary to spend only one actor's action.

## Rejection and telemetry

Invalid target, wrong turn, unavailable action, incompatible body/lifecycle, busy command surface, or path failure returns precise visible feedback and leaves the mounted relationship intact. Telemetry records input surface, stock command identity, selected principal, target, actor, command/resource owner, weapon, cooldowns before/after, rule/roll/damage counts, cancellation, terminal result, and duplicate counts.

## Regression rule

Rider Primary activation, target cancel, rejection, miss, hit, movement, or shared-turn transition must never call relationship cleanup. Only a named lifecycle invalidation may end the relationship; automatic remount is forbidden.
