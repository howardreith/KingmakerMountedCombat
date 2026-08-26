# Horse animal-companion contract

Superseding status (2026-08-26T20:02:37Z): PASS (production topology, RT/TB runtime) - dev.13 proves exact stock RT and TB Bite chains, one attack/roll/damage each, zero unexpected pair attacks, and zero post-dispatch turn restarts. Its later aggregate failure is a guarded-scenario ordering defect at target-selected Mount, not a companion failure. Dev.14 waits for exact production exploration admission before proceeding to the remaining mounted and lifecycle rows. Production companion and Mammoth behavior remain unchanged.

Superseding status (2026-08-26T15:03:02Z): PASS (production topology, partial runtime) - dev.10 proves the complete TB readiness and exact Standard-slot admission boundary but remains historical restored `FAIL 32/1` because Kingmaker's preexisting queued next-unit handoff replaced the first public diagnostic horse turn before the Bite started. Exact installed `CombatController` code confirms that `StartTurn` does not clear `m_NextUnit`. Dev.11 changes only the guarded scenario: it requires two-frame exact horse-turn stability and performs at most one public reassertion after an observed replacement. It neither accesses nor mutates private turn state. Production horse and Mammoth behavior remain unchanged; one clean aggregate still gates mounted-profile admission.

Superseding status (2026-08-26T13:18:19Z): PASS (production topology, partial runtime) - credited dev.9 run `20260826T124422Z-horse-companion-unmounted-dev9-passI` passes registration `13/0` and unmounted RT/progression/movement/ownership/cleanup `18/1`; its only failure is a zero-rule TB Bite submitted on the first merely admissible native turn frame. The TB leaf must follow the already-qualified combat readiness contract: exact horse turn, Preparing/Acting admission, empty command surface after native `StartTurn` preparation, available Standard action, conscious target, attack eligibility, unpaused game, idle hands, and no scheduled equipment update. It must not issue a second blanket `InterruptAll` after native turn preparation. The exact player-created single Bite must own the horse Standard slot, finish successfully, and retain exact `1 attack / 1 roll / 1 damage`, at least one forced D20, and zero unexpected pair attacks. Failure evidence must preserve bounded dispatch/terminal command state. This is a guarded-scenario repair only; production horse and Mammoth behavior remain unchanged.

Superseding status (2026-08-26T11:20:50Z): PASS (production topology, partial runtime) - dev.8 proves reference-identical Standard ownership and one exact RT Bite/roll/damage chain. Its remaining compound failure came from requiring exactly one D20 rule event, contrary to credited stock critical evidence. Dev.9 requires at least one forced D20, serializes RT/TB forced-roll and unexpected-pair counters, and retains exact rule cardinality plus zero duplicates. Production horse behavior is unchanged; TB attack/death/respec remain uncredited pending one clean aggregate.

Superseding status (2026-08-26T09:47:04Z): PASS (production topology, partial runtime) - dev.7 establishes native same-target command merging as a guarded-scenario identity defect. Dev.8 clears only the temporary horse's commands at explicit RT/TB test dispatch and requires exact player-command admission. Production horse behavior is unchanged; combat/death/respec remain uncredited pending one clean aggregate.

Superseding status (2026-08-26T08:08:44Z): PASS (production topology, partial runtime) - dev.6 proves exact Bite/Hoof/Hoof enumeration and every prior unmounted gate. Its first stock RT Bite remained preterminal to the old deadline; dev.7 observes the exact native start/approach boundary without changing production behavior. Combat/death/respec remain uncredited pending that bounded result.

Superseding status (2026-08-26T06:33:22Z): PASS (offline repair) - dev.5 reached combat and exposed one duplicate Bite from the obsolete hands-enabled body. Dev.6 follows the credited native horse's disabled-hands contract and orders natural limbs Bite/Hoof/Hoof; one clean aggregate remains required.

Superseding status (2026-08-26T05:10:00Z): PASS (partial runtime) - dev.4 proves exact native XP handoff, zero duplicate update, creation/ownership/control, selection, view/stats, and stock movement. Combat/death/respec remain uncredited after a later diagnostic-target geometry failure; dev.5 repairs that guarded test only.

Superseding status (2026-08-26T03:40:00Z): PASS (offline) - native progression means committed target class level or exact target-XP handoff for manual companion leveling; one dev.4 runtime aggregate remains required.

Superseding status (2026-08-25T21:29:32Z): PASS — construction and unmounted runtime contracts implemented; live aggregate qualification remains required.

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

## Implemented production checkpoint

Version `0.1.0-phase3b-dev.11` contains the construction and current unmounted-runtime contracts behind `HorseCompanionBlueprintService`:

- original KMC unit/feature/upgrade definitions with the reserved GUIDs above;
- exact native `CR1_HorseRiding` prefab/visual/speed and a proven stock companion class;
- explicit Large size and base ability scores `16/13/15/2/12/6`;
- the exact native no-hands animal topology with null hand fallbacks and ordered natural limbs `[Bite1d4, Hoof1d4, Hoof1d4]`; stock single attack therefore selects Bite and stock full attack enumerates each intended limb once;
- stock `AddPet` with exact `AnimalCompanionRank` and a KMC-owned rank-4 upgrade containing only racial `+2 Strength` and `+2 Constitution`;
- exact zero-level blueprint bootstrap parity with the stock Mammoth and Dog, followed by native `AddPet` rank-to-level application after spawn;
- exact native progression recognition for either committed target class level or exact `XPTable.GetBonus(targetLevel)` settlement ready for manual companion leveling;
- one bounded deferred invocation of the exact native `AddPet.TryUpdatePet` only when the exact owned KMC horse has neither settlement and no `DefaultBuildData` context exists; a second attempt, another pet, ambiguous ownership, exact XP settlement, or an already synchronized horse is rejected;
- a narrow `HorseCompanionAddPet` deactivation extension that first invokes stock master removal and then destroys only the exact KMC horse, preventing respec orphan residue;
- four KMC-owned localization keys and a native/fallback Kingmaker icon;
- a reference-exact, append-only Ranger selection lease with compare-before-restore semantics.

Exact installed Call of the Wild SHA-256 `4ebf8e1ed3e66ffed72ea33ea325595629423dacd5bffa23e3c9109144b26915` establishes why target XP is a valid native outcome: its animal-companion prefix sets `Progression.Experience` to the stock XP-table target, raises the native gain-experience event, applies the upgrade, and preserves manual player level-up rather than committing levels synchronously. KMC does not depend on or patch Call of the Wild and never directly mutates progression state.

The current offline contract is `PASS`: source `21/0`, Release, component `251/0`, visual/source-order `17/0`, harness `231/0`, assembly `349/0` (`325` Kingmaker + `24` Wrath), PowerShell parser `26/0`, JSON parser `7/0`, diff, and prohibited-payload validation. Historical dev.2 through dev.10 aggregate evidence remains immutable and uncredited; each passed exact restoration and progressively established progression, movement, target, natural-attack, RT attack, and TB admission boundaries. One clean dev.11 aggregate remains required; actual disk save/reload stays human-gated under the guarded harness's crash-safe save restriction.
