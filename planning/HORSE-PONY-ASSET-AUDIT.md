# Horse and pony native-asset audit

Status: PASS

Date: 2026-08-25

## Evidence boundary

This record contains factual metadata and original conclusions only. Installed Kingmaker files and native resources are read-only. No proprietary object, model, animation, controller, texture, material, assembly, or bulk metadata dump is copied into Git or a KMC package. Wrath assets are excluded.

## Reverified `CR1_HorseRiding` authority

The Phase 1 in-memory UnityFS/type-tree result was re-bound on 2026-08-25 to the unchanged exact installed files:

| Item | Exact result |
|---|---|
| Blueprint | `CR1_HorseRiding` / `BlueprintUnit` |
| Blueprint GUID | `9e9e75c484e68734487e609714565202` |
| Size | `Large` (serialized enum `5`) |
| Prefab resource | `5e0b93738ad54dd4ba101b3513ac4590` |
| Prefab path | `Assets/Mechanics/Bundles/Prefabs/Characters/Creatures/Medium/Horse/HorseRiding.prefab` |
| Bundle | 2,963,744 bytes; SHA-256 `1aada90ae55bab86806afc276247ff7e22498ed22f7ae5effbc95ac5621f2643` |
| Manifest | 2,965 bytes; SHA-256 `d1f1dbe4eae196d596368cfff025f15c14832b3b4aca9d3e5f64a43dd159f837` |
| Blueprint container | `sharedassets1.assets`, 212,953,824 bytes; SHA-256 `cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785` |
| View ID | `aafc2343-db64-42fb-b190-ad9165830711` |

The clean initialized-library/runtime view audit reverified and extended these prefab facts:

- root `HorseRiding`; 122 transforms;
- rig `HorseRiding_body_RIG`;
- `Chest`, `L_Stirrup`, and `R_Stirrup` exist; no named Saddle, Mount, or Rider transform;
- `Chest` local position `(0, 0.069489, -0.436785)`;
- left/right stirrups are Chest children at approximately `(+/-0.305183, -0.12273, -0.04402)`;
- stock movement agent acceleration `8`, minimum speed `0.2`, angular speed `360`, combat angular speed `720`, obstacle connection enabled, avoidance enabled;
- corpulence `0.9`, soft-collider height `2.4`, radius field `1.0`, core collider scale `(1.8, 1.8, 2.499)`;
- the live view exposes the stock `UnitMovementAgent`, two colliders, the dedicated rig/mesh/materials, and native locomotion, special-attack, reaction, stand-up, and idle actions;
- the file-level animation inventory additionally identifies native idle, walk, run, stop, hoof, bite, gore, reaction, stand-up, and death coverage.

The stock unit is a campaign/prototype horse, not a native animal-companion feature: it has `AddClassLevels` and `Experience`, and no stock `AddPet` ownership contract points to it. That is why it was rejected for Phase 1, but it is suitable native view evidence for the newly authorized original KMC companion blueprint.

## Summoned pony audit

Current status: PASS. The one repaired view comparison completed and restored exactly.

The first guarded initialized-library observation, `20260825T162200Z-horse-native-asset-audit-passA`, resolved:

| Item | Exact result |
|---|---|
| Blueprint | `PonySummoned` / `BlueprintUnit` |
| Blueprint GUID | `3f95557fc806db741b500a5735990841` |
| Size | `Medium` (`4`) |
| Prefab resource | `447d2907feec82545b3773fbb4709588` |
| Prefab resource name | `Pony_02` |
| Ability scores | Str 13, Dex 13, Con 14, Int 2, Wis 11, Cha 4 |
| Speed | 40 feet |
| Natural attacks | two native `SmallHoof1d3` additional limbs (`085547b82eded104ba7e1870dd0563bf`) |
| Initialized-library reverse owners | zero after a complete nontruncated bounded scan |

The run is preserved as uncredited `FAIL 15/5`. Four failures arose because the observer requested nonexistent `UnitViewLink.Load()`; exact installed Kingmaker exposes `WeakResourceLink<T>.Load(Boolean ignorePreloadWarning=false)` token `0x06007478`. The fifth failure treated the complete zero-owner result as fatal. Neither establishes an asset or gameplay failure. The attributable repair calls `Load(false)`, pins exact summoned-pony identity, and preserves zero reverse owners as negative evidence while requiring scan completion.

The repaired observation implementation remains `horse-native-asset-audit`. Its one create-new artifact is `horse-native-asset-audit.json`, independently manifested as `horse-asset-audit`. The scanner uses the exact initialized `BlueprintsByAssetId` and `ResourceNamesByAssetId` dictionaries, resolves horse/pony resource IDs back to BlueprintUnit prefabs, and follows bounded object/GUID reference paths. It reports progress every 5,000 blueprint owners, caps reference output at 500, and never registers or instantiates a gameplay unit.

The clean retry `20260825T180000Z-horse-native-asset-audit-repair-passB` is credited `PASS 21/0` on implementation commit `9431cbb0beb999900187ee13f9f58f4a18bd9066`, version `0.1.0-phase3a-dev.2`, DLL SHA-256 `450f1758ef8fa07a27ac4993344b4566f954b7fe6afa73ac180ab5e098870686`, MVID `9ee3c613-b957-4d35-8fb7-d6b897edbe39`. Its audit artifact SHA-256 is `fb9a9f5157a427d545a31ab5b41b69746a2c19cc3e710c8dc656242a0d68ac8d`. Immediate independent audit re-proved the pinned suite snapshot `27a4f4c8606b07296164d4efcdf966f475d0348b93d0e7d2d161b550b6632c8e`, save digest `8db675f6866e34399f62a2893ed480fb5246303ca3ec444fb6af9f6a11872726`, Mods digest `9179a5026662fa86b5126c1179fc164e7c24beafa58f41399d8229ca91e5005c`, immutable Baseline, restored Working, and zero process/lock/sentinel/deployment residue before evidence was read.

## Exact horse-versus-pony conclusion

`PonySummoned` and `CR1_HorseRiding` do not share a mount-ready view contract:

| Property | `CR1_HorseRiding` | `PonySummoned` |
|---|---|---|
| Size / root scale | Large / `(1,1,1)` | Medium / `(1,1,1)` |
| Prefab | `HorseRiding` | `Pony_02` |
| Mesh | `horse_riding`, `horse_riding_equip` | `Base` |
| Rig inventory | 33 named bones; `HorseRiding_body_RIG` | 36 named bones; `Pony_body_01` family |
| Seat geometry | `Chest`, `L_Stirrup`, `R_Stirrup` present | all three absent |
| Corpulence | `0.9` | `0.5` |
| Soft collider | radius `1.17`, height `2.4` | radius `0.65`, height `1.8` |
| Natural weapons | two `Hoof1d4` additional limbs | primary plus additional `SmallHoof1d3` |

Both views use stock Kingmaker movement/collider/animation systems, but their prefab, mesh, skeleton, material, footprint, and seat geometry are distinct. The pony supplies no Chest/stirrup evidence and is not resized. The Large native riding horse remains the selected view authority. `HorseSummoned` separately confirms another Large native horse family with Chest/stirrups; it does not supersede exact `CR1_HorseRiding` authority.

The initialized-library reverse scan found zero exact owners of `PonySummoned` and completed without truncation. This is a factual negative result, not a failed audit.

The runtime observation recorded, for both horse and pony:

| Contract | Required comparison |
|---|---|
| Blueprint ownership | GUID, type, internal name, components, spell/ability/campaign reverse owner |
| View | resource ID, native path, view unique ID |
| Geometry | mesh and renderer identities, root scale, child transform count |
| Rig | skeleton root, complete named-bone comparison, Chest and stirrup transforms |
| Animation | animation set, animator controller, state/clip identity and lifecycle coverage |
| Movement | agent type and exact serialized/runtime settings |
| Footprint | corpulence, selection radius/circle, hard and soft collider geometry |
| Combat | body hand sets, bite/hoof/gore weapon identities, additional limbs |
| Materials | native material/renderer identities only; no extraction |

Decision: do not resize the pony. Build the original KMC companion mechanics around the native Large `CR1_HorseRiding` view, and qualify that companion unmounted before using its Chest/stirrup geometry for `medium-humanoid-horse-v1`.
