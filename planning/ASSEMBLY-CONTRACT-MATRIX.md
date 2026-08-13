# Assembly contract matrix

Status: IN PROGRESS

Authority fingerprints are in `planning/ENVIRONMENT-FINGERPRINT.json`. Bounded decompilation is retained only under the ignored lab `analysis-cache`; this document records original summaries, signatures, and evidence.

## Classification

`DIRECT EQUIVALENT`, `OLDER EQUIVALENT`, `USABLE CONTROL POINT`, `PARTIAL PRIMITIVE`, `ABSENT — REIMPLEMENT`, `ABSENT — SIMPLIFY`, `UNSAFE GLOBAL SEAM`, and `UNKNOWN — MORE EVIDENCE REQUIRED`.

## Intake map

| Responsibility | Wrath exact type/member lead | Kingmaker exact candidate or absence | Classification | Evidence / confidence | Next trace |
|---|---|---|---|---|---|
| Relationship state: rider | `Kingmaker.UnitLogic.Parts.UnitPartRider` exists | No type containing exact Rider/Saddled/Mount responsibility was found in the Kingmaker type inventory | ABSENT — REIMPLEMENT | Exact assembly type lists; high for type absence, low for replacement | Record fields, transitions, callers, serialization |
| Relationship state: mount | `Kingmaker.UnitLogic.Parts.UnitPartSaddled` exists | No direct Kingmaker type | ABSENT — REIMPLEMENT | Exact assembly type lists; high | Trace rider/mount entity references and part lifecycle |
| Mounted controller | `Kingmaker.Controllers.Units.SaddledUnitController` exists | No direct Kingmaker type | ABSENT — REIMPLEMENT | Exact assembly type lists; high | Trace tick ownership and controller registration |
| Mount/dismount entry | `ContextActionMount`, `ContextActionDismount`, `ActivatableAbilityMount` exist | No direct Kingmaker type | ABSENT — SIMPLIFY | Exact assembly type lists; high | Phase 1 diagnostic action only |
| Movement agent ownership | `Kingmaker.View.UnitMovementAgentBase` plus mounted collaborators | `Kingmaker.View.UnitMovementAgentBase`, `UnitMovementAgent`, and `UnitMovementAgentContinious` | PARTIAL PRIMITIVE | Bounded type decompilation; medium | Trace destination, stop, tick, avoidance, position writes |
| Command routing | `UnitCommands`, `UnitCommand` plus rider helpers | `Kingmaker.UnitLogic.Commands.UnitCommands`, `UnitCommand`, `UnitGroupCommand`, `GroupCommand` | USABLE CONTROL POINT | Exact types present; low until member/caller trace | Identify narrow interception point and non-mounted guard |
| Ground-click routing | Wrath selection/click graph pending | `Kingmaker.Controllers.Clicks.Handlers.ClickGroundHandler` | USABLE CONTROL POINT | Exact type present; low | Trace destination construction and group command fan-out |
| Group command routing | Wrath group controller pending | `Kingmaker.Controllers.Units.GroupCommandsController` | PARTIAL PRIMITIVE | Exact type present; low | Trace party recipients and cancellations |
| Entity/view position | `UnitEntityData`, `UnitEntityView`, mounted controller | Kingmaker `UnitEntityData`, `UnitEntityView` | PARTIAL PRIMITIVE | Bounded decompilation; low | Establish authoritative position/rotation and attach/detach semantics |
| Selection | `SelectionCharacterController` and mounted helpers | `Kingmaker.UI.Selection.SelectionManager` | PARTIAL PRIMITIVE | Exact types present; low | Trace selected-unit collections and restoration |
| Formation | `PartyFormationManager`, `PartyFormationAuto` | `PartyFormationHelper`, `CustomPartyFormation` | OLDER EQUIVALENT | Exact types present; low | Trace slot inputs and group-move treatment |
| Combat boundary | `Kingmaker.Controllers.Combat.UnitCombatState` plus rider logic | Kingmaker `UnitCombatState` | USABLE CONTROL POINT | Exact types present; low | Identify combat-start observable event without broad patch |
| Companion qualification | Wrath `UnitPartPet`, `UnitPartPetMaster` | Kingmaker `AddPet`, unit descriptor/player pet contracts pending | PARTIAL PRIMITIVE | Kingmaker has no UnitPartPet type; medium | Establish exact master/active-pet identity contract |
| Serialization | Entity parts participate in Wrath persistence; exact attributes pending | No KMC production type exists yet | UNKNOWN — MORE EVIDENCE REQUIRED | Type inspection incomplete | Keep Phase 1 state external to entity serialization |
| Lifecycle cleanup | Wrath controller/parts pending | Kingmaker event interfaces and view lifecycle pending | UNKNOWN — MORE EVIDENCE REQUIRED | Not yet traced | Death, combat, area, view, disable, exit |
| Animation/anchor | `Kingmaker.Visual.Mounts.MountOffsets` exists | Native view transforms/animators exist; candidate metadata pending | PARTIAL PRIMITIVE | Exact Wrath type and Kingmaker view primitives; low | Inventory one native candidate and rider anchor |

## Pre-code gate

The mounted relationship implementation is not authorized yet. Exact member signatures, caller/callee evidence, selection/formation semantics, serialization boundaries, and a narrow Kingmaker movement seam remain IN PROGRESS.
