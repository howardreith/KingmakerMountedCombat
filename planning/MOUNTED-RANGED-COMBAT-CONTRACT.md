# Phase 3D Mounted Ranged Combat Contract

Status: IN PROGRESS

## Exact hostile-click admission checkpoint

Installed Kingmaker's `ClickUnitHandler.OnClick` true return is not sufficient attack-admission evidence. The same return value can represent switching to a directly controllable target, handling loot, or completing an attack-loop pass in which a selected unit was skipped. Qualification therefore binds the exact UI selection-manager instance, exactly one selected rider, that rider as `GetNearestSelectedUnit` for the hostile position, a visible hostile target that is outside the party, not directly controllable, and not a dead-loot target, plus the same-frame `IClickActionHandler.OnAttackRequested`-derived KMC request and mounted-intent deltas.

The long-range Shortbow control waits for two stable frames of those facts together with its native weapon lease, combat memory, idle command/hands/equipment surfaces, no active pair intent/movement, real-time unpaused mode, exact mounted pair, and healthy pose. `OnClick=true` must be accompanied by exactly one native request and one mounted intent; otherwise it fails immediately and records admission/input state. This is evidence attribution, not a replacement click path.

## Weapon contract

Ranged participation is determined from the exact native planned `UnitAttack`/weapon blueprint (`IsRanged`, native attack range, command LoS and target APIs), not a hard-coded bow list. No compile-time or runtime reference to Gunslinger or another gameplay mod is permitted.

Exact Kingmaker ranged-AoO delivery is asynchronous. `UnitCombatEngagementController.OnEventDidTrigger(RuleAttackRoll)` adds the initiator to a private provocation set only for `Ranged`/`RangedTouch` rolls with `DoNotProvokeAttacksOfOpportunity=false`. Its later `Tick` enumerates the rider's then-current `CombatState.EngagedBy` set and calls each hostile's ordinary `UnitCombatState.AttackOfOpportunity(rider)`. The latter owns native counter, threat-hand, memory, motion, untargetable, force-move, immunity, and action checks and enqueues the hostile `UnitAttackOfOpportunity`. KMC must neither synthesize that chain nor globally suppress it. A deterministic mounted control therefore requires an idle rider-first ranged admission, exact native `AttackOfOpportunity(rider,true)` preflight, the rider roll's attack type/no-provoke flag, hostile counter consumption, and one exact hostile attack/roll/damage chain.

The native rider attack remains authoritative for ammunition, reload, attack roll, concealment, cover, range increments, line of sight, animation, and ranged AoO behavior. KMC records these surfaces but does not duplicate their rule implementations.

## Approach-to-range

For a stock hostile click outside range:

1. retain the stock rider attack as the intent authority;
2. compute a mount-owned reachable stopping position from the native rider attack radius and LoS requirement;
3. run one exact mount `UnitMoveTo`, without teleportation;
4. stop once rider range and LoS admission are satisfied;
5. start one native rider attack;
6. permit at most one bounded repath when target displacement crosses the existing threshold;
7. stop immediately on cancel/new command/target invalidation.

The mount never closes toward melee merely because it has a natural weapon. During an ordinary ranged intent, the mount may spend one separately owned primary only when its current position already satisfies the exact native natural-weapon range and line-of-sight contract; it may also attack when the user separately invokes Mount Primary. If it is not already in legal melee, ranged intent produces no mount-primary dispatch and no melee approach.

## RTWP and turn-based

RTWP intent repeats rider fire using native cooldown/reload/ammunition behavior until canceled or invalidated. Turn-based mode consumes mount movement resources for approach and rider Standard resources for fire; it never spends or refreshes the other actor's ledger.

## Compatibility statement

Bows, crossbows, slings, and compatible modded ranged weapons may participate through native APIs. Firearms receive no special integration; compatibility is limited to whatever ordinary Kingmaker `UnitAttack`, weapon, ammunition, and reload contracts the providing mod exposes without a dependency.

## Qualification

Credit requires a real hostile click, visible mount approach or stationary fire, one native rider attack chain, exact ammunition/reload observations where applicable, zero forced melee closure, native LoS/cover/concealment/AoO controls, cancellation with no late attack, and unmounted controls unchanged. The outside-range control requires zero mount-primary dispatch. An adjacent control permits at most one mount primary, only with recorded legal-melee readiness and no additional Horse movement.

Each Light Crossbow and Sling control uses its own freshly created hostile after verified destruction of the previous target. Admission waits for stable exact weapon, selection, combat-memory, action, command, hands, equipment, relationship, and pair-intent readiness. Evidence binds distinct previous/current target IDs, one native request, one intent, one child rider attack, and exact rider/Horse rule cardinality. This prevents late same-target rules or an in-flight equipment swap from being mistaken for weapon compatibility evidence.

Dev.10 RT evidence reached the long-range Shortbow target but did not cross this exact admission boundary: the click was reported handled while current native request/intent deltas remained `0/0`, immediately after a friendly Horse click. That failed process is attribution only. Dev.11 adds the exact selection/target branch and same-frame cardinality gate without changing production ranged routing. A second readiness-proven absence requires an architecture disposition rather than a global click patch.

Dev.11, dev.13, and dev.14 each crossed the exact mounted Shortbow boundary and passed approach-to-range, repeated rider-only fire outside melee, cancellation, line of sight, cover/concealment observation, adjacent fire, native ranged AoO, Light Crossbow, Sling, and no-forced-melee rows inside their otherwise failed aggregates. Dev.14's final unmounted Sling control does not receive credit: it clicked immediately after swapping equipment and preserved no stable Standard/equipment/command readiness evidence. Dev.15 waits for two stable exact unmounted RT frames and binds the actual click to a rider-owned native `UnitAttack` in the Standard slot. It changes no ranged production behavior.
