# Mounted subsystem dependency graph

Status: IN PROGRESS

The solid Wrath nodes below are exact local types. Dashed labels describe responsibilities still being traced. Kingmaker has no direct rider/saddled/controller types; its candidate seams are older movement, command, selection, formation, entity/view, and lifecycle primitives.

```mermaid
flowchart TD
    Entry["Mount / dismount actions"] --> Rider["UnitPartRider"]
    Entry --> Saddled["UnitPartSaddled"]
    Rider <--> Saddled
    Rider --> Controller["SaddledUnitController"]
    Controller --> Movement["movement authority"]
    Controller --> Avoidance["avoidance / collision"]
    Controller --> Entity["entity position / rotation"]
    Controller --> View["view attachment / offsets"]
    Rider --> Commands["command delegation"]
    Commands --> Movement
    Rider --> Selection["selection / active unit"]
    Selection --> Formation["party formation / group move"]
    Rider --> Combat["combat and action coupling"]
    Rider --> Lifecycle["death / view / area / disable cleanup"]
    Rider --> Serialization["entity-part persistence risk"]
```

## Kingmaker candidate seam graph

```mermaid
flowchart LR
    Click["ClickGroundHandler"] --> Group["GroupCommandsController / GroupCommand"]
    Group --> Commands["UnitCommands / UnitCommand"]
    Commands --> Agent["UnitMovementAgentBase / UnitMovementAgent"]
    Agent --> Entity["UnitEntityData"]
    Agent --> View["UnitEntityView"]
    Selection["SelectionManager"] --> Group
    Formation["PartyFormationHelper"] --> Group
    Pet["AddPet + descriptor/player pet contracts"] --> Validation["pair validator"]
    Combat["UnitCombatState / event bus"] --> Cleanup["cleanup coordinator"]
    View --> Cleanup
```

## Required trace closure

| Concern | Wrath closure | Kingmaker closure | Status |
|---|---|---|---|
| Relationship state | Exact part fields/transitions/callers | Original runtime-only domain | IN PROGRESS |
| Command delegation | Rider helper and command call sites | Narrow routing decision and guard | IN PROGRESS |
| Movement authority | Controller-to-agent behavior | One enabled authoritative mover | IN PROGRESS |
| Avoidance/collision | Mounted flag/agent behavior | Reversible scoped override | IN PROGRESS |
| Entity position | Rider and mount entity semantics | Minimum logical synchronization | IN PROGRESS |
| View attachment | Offset/anchor lifecycle | Native transform anchor or bounded offset | IN PROGRESS |
| Selection | Mounted selection redirection | Stable restoration | IN PROGRESS |
| Formation | Mounted pair slot semantics | Single effective party mover | IN PROGRESS |
| Combat/action coupling | Future contract only | Phase 1 cleanup boundary | IN PROGRESS |
| Serialization | Part/reference persistence | No custom serialization in Phase 1 | IN PROGRESS |
| Lifecycle cleanup | All invalidation sources | Idempotent coordinator hooks | IN PROGRESS |
