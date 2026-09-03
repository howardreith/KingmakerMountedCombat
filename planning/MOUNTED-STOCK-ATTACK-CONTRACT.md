# Phase 3D Mounted Stock Attack Contract

Status: IN PROGRESS

## Fresh RT melee qualification result

Audited dev.10 RT evidence closes isolated melee admission, approach, native actor sequencing, and persistent-repeat behavior at its exercised boundary. The explicit-primary target was destroyed and verified; a distinct six-metre target then passed two stable readiness frames. One ordinary hostile click produced exactly one native request and one mounted intent, positive Horse-owned approach, two rider and one Horse dispatch, matching `3/3/3` native attack/roll/damage cardinality, zero duplicates, target survival, and retained mounted relationship. This is evidence for ordinary RT melee input and persistence, not a claim of full iterative/full-attack parity.

Dev.11 tightens but temporarily reopens cancellation attribution. One native ground command cancelled mounted intent exactly once with no new KMC dispatch and no duplicate count, while one additional rider rule/roll/damage event appeared during the following five frames. The evidence omitted `RuleAttackWithWeapon.IsAttackOfOpportunity`, so it cannot establish whether this was a prohibited late ordinary attack or an independent native rider AoO. Dev.12 records exact rule flags and pair command slots/queues across that boundary. Qualification requires zero post-cancel non-AoO rules; an event may be excluded from canceled-intent cardinality only when exact native evidence marks it as an opportunity attack. No global AoO suppression is permitted.

Dev.11 separately closes the Shortbow input boundary: two stable frames proved the exact selected and nearest rider plus the hostile-target branch, and one real click produced exactly one current native request and one mounted intent. Ranged qualification remains independent from the melee result and still requires its own actor, resource, movement, native-rule, cancellation, and cleanup evidence.

Dev.12's fresh isolated melee input was also admitted exactly, but its first Horse-owned native point move stalled after `4.7231493m` and the bounded pair command ended before any attack rule. The still-valid rider was `1.70507455m` from the target at the diagnostic deadline. Because dev.10 and dev.11 completed the same six-metre control, this single result does not overturn their admission/persistence proof or authorize a broad movement change. Dev.13 adds the omitted terminal outcome, pair mechanics distance/radius, position, LoS, movement-agent, and raw-command evidence. The next run must distinguish diagnostic placement/LoS from an exact pair-local native movement terminal defect before any repair.

Dev.13 then passed the same stock-melee control, and dev.14 again passed ordinary hostile-click melee approach/repeat/cancel with rider/Horse dispatch `2/1`, matching native rules, and zero duplicates. Dev.14's earlier explicit Rider Primary independently reached exact child range/LoS after `6.125914m` of Horse movement while its point move remained nonterminal. Exact installed command flow shows that native `UnitMoveTo` success and the child's legal attack boundary need not become observable on the same tick. Dev.15 therefore ends only the reference-identical KMC delegated move when the unchanged child range/LoS predicate is already true. It records an exclusive native-success or legal-range-stop boundary and still requires slot/queue restoration before child admission. The persistent stock intent, native actor attacks, action ledgers, range, LoS, cancellation, and timeout remain unchanged.

Dev.16 RT A passed every stock mounted melee row and the final isolated unmounted melee control. The latter was a genuine rider-owned, non-AoO stock `UnitAttack` with zero Horse rules and exact Horse AI restoration. Its full attack killed the fresh target after `48` damage. Dev.17 therefore does not alter stock attacks; it creates a second verified-fresh target before the independent unmounted Sling control so one successful control cannot invalidate the next.

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
