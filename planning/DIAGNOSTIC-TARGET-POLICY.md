# Diagnostic combat target policy

Status: IN PROGRESS

No suitable quest-independent hostile is assumed to exist in the exact Working fixture. Runtime qualification may create one project-owned diagnostic target only after the guarded loader proves the exact `KMC_AUTOMATION_WORKING` identity.

## Selected source and lifecycle

The source unit is the stock, generic, unnamed `AnimalCompanionUnitMammoth` blueprint `e7aa96d15a45238438ae4cfb476f6bb9`. It is already the exact locally proven simple melee creature and introduces no second species or rider. The spawned target is never eligible as a KMC mount, never receives a rider, and is not added to any player, companion, quest, dialogue, summon-pool, or area-script collection.

Creation uses exact `Game.EntityCreator.SpawnUnit(BlueprintUnit,Vector3,Quaternion,SceneEntitiesState)` token `0x0600901F` against the current loaded area's main state. Placement must pass bounded open-position and path checks before activation. The target receives:

- a new per-run GUID recorded in evidence;
- a project-created runtime-only hostile faction whose only attack relation is the player faction;
- `GiveExperienceOnDeath=false` through tokens `0x06008338/39`;
- no added inventory, loot, quest, dialogue, script, summon, or reward surface;
- deterministic stationary/AI-disabled behavior unless a scenario explicitly requires target movement.

The runtime faction and all source state are transient. The stock Mammoth blueprint itself is never modified. Target removal calls `EntityDataBase.Destroy` token `0x06007EA6`, drains the exact destruction controller, verifies absence from scene/global unit collections and commands, then destroys the runtime faction object. Removal is idempotent and is invoked for normal completion, target death, dismount, save/load/area boundary, mod disable, exception recovery, and process teardown.

## Fail-closed gates

The target is forbidden unless all of these are true:

- exact Working fixture identity and read/write authorization are already active;
- no target with the run's ID exists;
- the source blueprint resolves exactly and is the stock Mammoth GUID;
- the chosen position is nav-valid, open, bounded near the pair, and unrelated to a door, interaction, quest, or map object;
- the target is hostile to the pair but not player-controlled, companion-owned, named/quest-bound, or loot/XP-bearing;
- the target has a live view, stock agent, primary natural attack, and no pre-existing commands;
- no save or area transition can proceed while it exists.

Any mismatch destroys the partial target and fails the scenario. Cleanup compares pre/post unit IDs, factions, party XP, inventory/loot observations, commands, and scene entities. A third-party or ordinary real hostile is never commandeered. Baseline is never loaded by game automation and never receives a target. Working is restored byte-for-byte by the outer guarded transaction; the target is not serialized.

Qualification remains `IN PROGRESS` until deterministic lifecycle tests and live create/death/remove failure-path evidence prove zero entity, faction, command, XP, loot, save, and external-state residue.
