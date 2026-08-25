# Horse animal-companion contract

Status: PASS — production construction gate open; runtime qualification remains required

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

## Resolved construction inputs

The credited audit `20260825T180000Z-horse-native-asset-audit-repair-passB` binds:

1. `AnimalCompanionSelectionRanger` GUID `ee63330662126374e8785cc901941ac7`, whose exact seven pre-KMC options are Empty, Dog, Ekun, Elk, Leopard, Monitor, and Wolf;
2. stock Mammoth feature/unit/upgrade `6adc3aab7cde56b40aa189a797254271` / `e7aa96d15a45238438ae4cfb476f6bb9` / `6a23d16a4476af644af89d91f9f96790` and its `AddPet` rank `1670990255e4fe948a863bafd5dbda5d`, upgrade level `7`;
3. Dog as a Ranger-present rank-4 companion baseline: feature/unit/upgrade `f894e003d31461f48a02f5caec4e3359` / `918939943bf32ba4a95470ea696c2ba5` / `9763e77bfdcd32541848a9095ac53455`;
4. native `CR1_HorseRiding` Large view/stats/body/movement/rig contract, including `Chest` and both stirrups;
5. all three reserved KMC GUIDs as unclaimed across the initialized 104,660-blueprint library;
6. the save consequence below: a selected KMC companion is persistent content even though mounting remains transient.

Production construction may now begin. It must still fail closed if any exact lookup changes at runtime, and runtime evidence—not construction or compilation—must qualify creation, progression, lifecycle, and uninstall guidance.

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
