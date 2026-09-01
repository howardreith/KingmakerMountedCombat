# Phase 3D Mounted Ranged Combat Contract

Status: IN PROGRESS

## Weapon contract

Ranged participation is determined from the exact native planned `UnitAttack`/weapon blueprint (`IsRanged`, native attack range, command LoS and target APIs), not a hard-coded bow list. No compile-time or runtime reference to Gunslinger or another gameplay mod is permitted.

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
