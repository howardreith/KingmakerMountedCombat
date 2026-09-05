# Phase 3D Mounted Ranged Combat Contract

Phase 3E final disposition (2026-09-05): `DEFER — EVIDENCED` for unified TB ranged. The required turn-completion gate failed under K9 before TB Shortbow, Crossbow/reload, Sling, approach-to-range, LoS recovery, and no-forced-melee rows were opened. No ranged, ammunition, reload, LoS, weapon, movement, or AoO production seam changed during the final scheduler repair cycles. The fallback package defaults to accepted separate turns and retains Phase 3D dev.17 RT ranged behavior; it makes no unified-TB ranged claim.

Phase 3E dev.11 checkpoint (2026-09-05): dev.10 stopped at the earlier native duplicate-turn boundary, before any Phase 3E TB ranged input. The dev.11 repair touches only exact redundant-mount initiative selection and no ranged, movement, LoS, ammunition, reload, weapon, or click-routing seam. TB ranged rows remain `TODO` until the same clean Horse run passes explicit mount scheduling, ordinary melee, and then reaches them in mission order.

Phase 3E superseding status (2026-09-04): `IN PROGRESS`. The pair-local command scheduler's in-range mount-primary Gate 1 is qualified, but no Phase 3E TB ranged approach, Shortbow, Crossbow/reload, Sling, LoS recovery, or no-forced-melee row has yet run. Dev.8/dev.9 affect only pre-sequencing combat-Mount diagnostic provenance. Phase 3D RT ranged PASS and separate-turn fallback remain immutable.

Status: BLOCKED — CRITICAL for unified TB movement/action sequencing; RT contract PASS

## Final Phase 3D disposition — 2026-09-04

Clean dev.17 RT A/B passed mounted Shortbow adjacent fire, approach-to-range, auto-fire, cancellation, line of sight, cover/concealment observation, native ranged AoO, no forced melee, Light Crossbow/reload routing, and mounted Sling routing. The Horse stopped at rider-native range/LoS and did not close unnecessarily. No Gunslinger dependency or firearm-specific behavior is claimed. The separate unmounted Sling control remains `DEFER — EVIDENCED` after native prior-command contamination before input.

TB ranged qualification is not credited. Rider fire can retain rider ownership, but any required Horse approach or optional mount attack needs a mount-owned command during the rider-owned turn; dev.21 proves stock Kingmaker will not advance that command. Further work requires the same separately authorized paired-command architecture as unified melee.

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

Dev.15 proved the Horse legal-range stop repair, but one fresh long-range target exposed a distinction the prior pair predicate hid. The second persistent Shortbow dispatch recorded Horse-origin/rider-executor mechanics distances `8.92802048m` / `8.455866m`, both inside the exact `13.7920008m` native radius, immediately after cached `rider.HasLOS(target)` admitted the pair. Exact installed `UnitCommand.IsUnitEnoughClose` token `0x06002784` performs only that distance test followed by direct `LineOfSightGeometry.HasObstacle(Executor.EyePosition, ApproachPoint, GetTargetLOSObjectId())`; `GetTargetPoint` token `0x060027A8` resolves the live hostile position and base `GetTargetLOSObjectId` token `0x060027A9` returns zero. Its false result is therefore exact evidence that the direct native LoS clause, not range or target life, rejected the child.

The bounded recovery contract uses that exact direct native predicate as the child-start authority. If weapon range is satisfied but direct LoS is not, the rider does not attack and KMC does not bypass `NeedLoS`; a Horse-owned move targets the hostile position with a narrower collision-safe stopping radius and is interrupted at the first frame the unchanged native distance+LoS predicate passes. Thus the Horse moves only as far as needed to establish a legal firing position. Target displacement still consumes the existing bounded repath allowance, cancellation still interrupts/removes the exact move and child, and persistent intent still terminates on its existing invalidation boundaries. No global LoS, cover, concealment, projectile, or unmounted command behavior changes.

Dev.16 RT A proves this repair at the exercised boundary. The second persistent Shortbow dispatch changed from the prior blocked direct-LoS state to exact native `Admitted`, the Horse stopped after `14.0967741m`, the rider completed two attacks, and Horse/duplicate dispatch remained `0/0`. All mounted Shortbow, Light Crossbow, Sling, cancellation, no-forced-melee, LoS, cover/concealment, and native ranged-AoO rows passed. The remaining unmounted Sling row failed before input only because its target had been killed by the preceding valid unmounted melee control. Dev.17 gives that row a verified-distinct target; it changes no ranged production rule.
