# Horse animal-companion contract

Status: IN PROGRESS — production gate closed pending exact runtime blueprint audit

Date: 2026-08-25

## Owned blueprint identities

The following deterministic KMC identities are reserved. Runtime registration must fail closed if any GUID already resolves to a non-identical blueprint.

| Blueprint | KMC asset GUID |
|---|---|
| `AnimalCompanionUnitHorse` | `4016c7db400ab721ff125aef9e65e202` |
| `AnimalCompanionFeatureHorse` | `7db7c50677e39f09feef56f3831fc723` |
| `AnimalCompanionUpgradeHorse` | `98e651899e6278d938de77af1d69bd32` |

These are runtime blueprints owned by KMC. They are not copied native GUIDs and do not overwrite stock objects.

## Construction seam

Exact Kingmaker 2.1.7b contracts:

- `ResourcesLibrary.LibraryObject.BlueprintsByAssetId` is the loaded blueprint dictionary; `ResourcesLibrary.GetBlueprints<T>()` enumerates the exact library.
- `BlueprintScriptableObject.AssetGuid` and `ComponentsArray` are writable runtime surfaces.
- `BlueprintFeatureSelection.AllFeatures` is the authoritative selectable list.
- native `AddPet` owns spawn, reciprocal `SetMaster`, party event delivery, rank-to-level mapping, upgrade-level application, area-load recovery, and feature deactivation cleanup.
- `AddClassLevels` applies the animal-companion class progression and can level an existing pet when `AddPet.TryUpdatePet` advances it.
- `UnitDescriptor.SetMaster` establishes the reciprocal master/pet link, copies group/faction control, updates unit grouping, and supplies pet identity used by party/selection/save systems.

Implementation must use those native seams. Harmony/UMM callbacks may schedule registration, but blueprint construction and validation belong in a dedicated service. No persistent mounted relationship is added.

## Exact source contracts to resolve before construction

One runtime audit must identify and bind:

1. the current Ranger animal-companion `BlueprintFeatureSelection` and its exact pre-KMC feature array;
2. a proven stock companion feature/unit/upgrade trio, initially Mammoth, including every `AddPet` field;
3. `AnimalCompanionRank`, animal-companion class, progression, portrait, player faction, body natural weapons, upgrade components, death/recovery behavior, and respec path;
4. `CR1_HorseRiding` view, body, stats, movement, and animation fields;
5. the exact uninstallation consequence when a save references a KMC blueprint.

## Product behavior

The horse must be Large, player-controlled through ordinary pet ownership, and fully usable unmounted. It receives its own natural attacks and resource/action ledgers. The Ranger owner receives exactly one companion through the stock `AddPet` relationship; the new option does not remove, reorder unnecessarily, or mutate existing options.

The initial mechanical profile must be evidence-derived from a working native animal companion and the native horse prototype. Blind field cloning is forbidden. Fields intentionally shared by reference must be listed; mutable arrays/components owned by KMC must be copied before modification.

## Progression and lifecycle gates

- creation at the Ranger companion-selection level;
- rank-to-pet-level mapping identical to the native `AddPet` contract;
- correct animal-companion class, BAB/saves/skills/feats, natural armor, ability scores, speed, and upgrade threshold;
- exact `SetMaster`, party list, selection, portrait, and group behavior;
- RT and TB movement and bite/hoof attack coverage while unmounted;
- death, resurrection/recovery, owner death, party removal, respec, save/load, and area transitions;
- KMC disable/uninstall behavior with no mounted residue and an honest save-compatibility policy;
- no effect on another Ranger companion choice, non-Ranger companions, or non-horse units.

Save truth: a selected KMC companion blueprint is necessarily referenced by the save. Therefore "uninstall behavior" cannot truthfully promise that the save remains valid after removing the defining mod. The implementation must fail clearly, never leave a partially registered blueprint set, and document that users must respec away from the KMC horse before uninstalling. This persistence decision is confined to the companion itself; the mounted relationship remains transient.

## Ranger integration transaction

Registration must snapshot the exact stock selection array and append the horse feature once. Disable/unload may restore the exact in-process array only when it is still the KMC-produced value; it must not overwrite later changes by another mod. Duplicate registration is idempotent. A collision, missing stock contract, or third-party mutation ambiguity is a hard failure, not permission to replace the selection.

