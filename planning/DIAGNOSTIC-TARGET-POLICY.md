# Diagnostic combat target policy

Status: `PASS` for the supported Phase 2 private-alpha scenarios.

Round 2 RT/TB evidence retains exact target identity, stationary/AI leases, attack admission, zero XP/loot/command residue, deterministic destroy/drain, and independent external restoration. The final door-only diagnostic creates no combat-target contract change. Historical investigation details below remain authoritative evidence; a future horse mission must requalify any target use within its own package and scenario boundary.

No suitable quest-independent hostile is assumed to exist in the exact Working fixture. Runtime qualification may create one project-owned diagnostic target only after the guarded loader proves the exact `KMC_AUTOMATION_WORKING` identity.

### Owner-approved owner-seed-derived scope (October 17, 2026)

The owner approved reusing this exact target contract on disposable copies of the owner-seed-derived Manual (`docs/CHUNK7-OWNER-DIRECTIVES-2026-10-16.md`, "Owner decisions on the combat slice"). The Working rule above is unchanged for every other case. The scope is exactly:

- the closed owned two-pair family's owner-seed-derived combat kinds `owner-seed-realtime-core`, `owner-seed-combat-rider-first` and `owner-seed-combat-mount-first`;
- the runner's fresh isolated copy of the Manual `KMC_C7_SEED_WORLD` that one successful owner-seed producer wrote. The selection pins the producer run, its world-evidence SHA-256 and the Manual SHA-256, plus the accepted PASS cold-consumer receipt (`owner-seed-persistence-load`) by run id and evidence SHA-256 on that same Manual;
- the seed and derived-save hashes stand in place of the Working identity: the loaded copy must equal the pinned derived-Manual SHA-256 and must never equal the immutable seed's SHA-256. The seed master is never loaded or written;
- the in-game authorization is derived from the admitted copy (`Chunk7CombatTargetAuthorization.Basis`, basis `owner-seed-derived-disposable-copy`), never passed as a literal. Evidence `targetAuthorization` records the basis, the owner approval, the seed, Manual, producer and consumer pins, the area, the rider and target positions, and `placementValidated`. Its `workingSha256` stays null;
- placement is checked before any measured step (walkable, bounded, about 3.5 m ahead of mount A). A refusal fails closed with the exact reason before the target is spawned or initiative is forced;
- the owner-seed setup kinds (exploration and the Manual producer) and the cold consumer never create a target. The cave's own monsters are never used or commandeered.

Every other fail-closed gate below applies unchanged, with the derived-copy authorization in place of the first gate for these kinds only.
## Selected source and lifecycle

The source unit is the stock, generic, unnamed `AnimalCompanionUnitMammoth` blueprint `e7aa96d15a45238438ae4cfb476f6bb9`. It is already the exact locally proven simple melee creature and introduces no second species or rider. The spawned target is never eligible as a KMC mount, never receives a rider, and is not added to any player, companion, quest, dialogue, summon-pool, or area-script collection.

Creation uses exact `Game.EntityCreator.SpawnUnit(BlueprintUnit,Vector3,Quaternion,SceneEntitiesState)` token `0x0600901F` against the current loaded area's main state. Placement must pass bounded open-position and path checks before activation. The target receives:

- a new per-run GUID recorded in evidence;
- a project-created runtime-only hostile faction whose only attack relation is the player faction, plus one exact per-run non-player unit group;
- a prevalidated natural-melee `Body.EmptyHandWeapon` fallback plus the exact stock-initialized weapon actually resolved after spawn through the `UnitAttack.CreateSingleAttack` order of eligible primary hand, eligible secondary hand, then first weapon-bearing additional limb;
- `GiveExperienceOnDeath=false` through tokens `0x06008338/39`;
- no weapon/body-slot provisioning, loot, quest, dialogue, script, summon, or reward surface;
- deterministic stationary/AI-disabled behavior unless a scenario explicitly requires target movement.
- immediately before the real player-click seam, target-only visibility provisioning clears fog on that transient entity and forces only its owned view visible; exact fog, view, player-visibility, click-object/view identity, rider attackability, and rider melee-weapon gates must all pass. No area/global fog state or non-target view is changed.

The runtime faction and all source state are transient. The stock Mammoth blueprint itself is never modified. Target removal calls `EntityDataBase.Destroy` token `0x06007EA6`, drains the exact destruction controller, verifies absence from scene/global unit collections and commands, then destroys the runtime faction object. Removal is idempotent and is invoked for normal completion, target death, dismount, save/load/area boundary, mod disable, exception recovery, and process teardown.

Exact local contracts and the first guarded diagnostic runs refine that lifecycle without weakening it:

- hostility is owned by `UnitGroup.IsEnemy`, so the target is detached from its stock spawn group before faction switching and placed in one collision-checked `KMC.RuntimeHostile.<runId>` group containing only that target; a pre-spawn group snapshot ensures cleanup disposes only a newly created empty spawn group and never an unrelated pre-existing group;
- the public `IsAIEnabled` getter intentionally reports true for non-controllable units, so the safety gate reads the pinned private `m_AiEnabled` backing field after using the public setter;
- `ItemsCollection.HasLoot`, rather than stock body-inventory count, is the native no-loot predicate;
- native single-attack selection first calculates exact hand attack counts, chooses an eligible primary hand, then an eligible secondary hand, and only then falls back to the first additional limb whose `HasWeapon` is true; the selected blueprint must separately prove `IsNatural=true` and `IsRanged=false`;
- the fresh stock companion template has no additional limb, but this is not evidence of weapon absence: `WeaponSlot.MaybeWeapon` returns `UnitBody.EmptyHandWeapon` for an empty active hand slot, while an initialized primary-hand item remains separately possible. The target must resolve a natural-melee weapon through `PrimaryHand`, prove at least one primary main attack, classify the source as exactly primary-hand item or empty-hand fallback, and retain identical before/after primary item, fallback, and limb identities. No blueprint, target body, active mount, player inventory, or save is mutated;
- cleanup first proves entity absence, then exact empty-group removal and disposal, and finally waits across Unity's deferred object-destruction boundary before claiming the runtime faction removed. Combat evidence publishes all three proofs independently.
- a rejected Harmony click records the controller's exact feedback and armed/active-command state, then enters cleanup immediately; it cannot wait out the row deadline and obscure the original rejection with a secondary timeout.

## Fail-closed gates

The target is forbidden unless all of these are true:

- exact Working fixture identity and read/write authorization are already active (or, for the owner-approved owner-seed-derived combat kinds only, the derived-copy basis above);
- no target with the run's ID exists;
- the source blueprint resolves exactly and is the stock Mammoth GUID;
- the chosen position is nav-valid, open, bounded near the pair, and unrelated to a door, interaction, quest, or map object;
- the target is hostile to the pair but not player-controlled, companion-owned, named/quest-bound, or loot/XP-bearing;
- the target has a live view, stock agent, primary natural attack, and no pre-existing commands;
- no save or area transition can proceed while it exists.

Any mismatch destroys the partial target and fails the scenario. Cleanup compares pre/post unit IDs, factions, party XP, inventory/loot observations, commands, and scene entities. A third-party or ordinary real hostile is never commandeered. Baseline is never loaded by game automation and never receives a target. Working is restored byte-for-byte by the outer guarded transaction; the target is not serialized.

Qualification remains `IN PROGRESS` until deterministic lifecycle tests and live create/death/remove failure-path evidence prove zero entity, faction, command, XP, loot, save, and external-state residue.
