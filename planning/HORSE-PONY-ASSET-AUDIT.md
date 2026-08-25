# Horse and pony native-asset audit

Status: IN PROGRESS

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

Exact previously established prefab facts remain authoritative pending a clean runtime re-observation:

- root `HorseRiding`; 122 transforms;
- rig `HorseRiding_body_RIG`;
- `Chest`, `L_Stirrup`, and `R_Stirrup` exist; no named Saddle, Mount, or Rider transform;
- `Chest` local position `(0, 0.069489, -0.436785)`;
- left/right stirrups are Chest children at approximately `(+/-0.305183, -0.12273, -0.04402)`;
- stock movement agent acceleration `8`, minimum speed `0.2`, angular speed `360`, combat angular speed `720`, obstacle connection enabled, avoidance enabled;
- corpulence `0.9`, soft-collider height `2.4`, radius field `1.0`, core collider scale `(1.8, 1.8, 2.499)`;
- `ArmoredHorseAnimationSet_LocoMotion` and a dedicated controller cover idle, walk, run, stop, hoof, bite, gore, stun, stand-up, and death.

The stock unit is a campaign/prototype horse, not a native animal-companion feature: it has `AddClassLevels` and `Experience`, and no stock `AddPet` ownership contract points to it. That is why it was rejected for Phase 1, but it is suitable native view evidence for the newly authorized original KMC companion blueprint.

## Summoned pony audit

Current status: exact blueprint and resource identity pending one bounded runtime blueprint/resource inventory. Name-based assumptions are forbidden: the complete blueprint library must be queried for pony/summon ownership and each candidate must be resolved through exact component and resource references.

The observation implementation is now qualified offline as `horse-native-asset-audit`. Its one create-new artifact is `horse-native-asset-audit.json`, independently manifested as `horse-asset-audit`. The scanner uses the exact initialized `BlueprintsByAssetId` and `ResourceNamesByAssetId` dictionaries, resolves horse/pony resource IDs back to BlueprintUnit prefabs, and follows bounded object/GUID reference paths. It reports progress every 5,000 blueprint owners, caps reference output at 500, and never registers or instantiates a gameplay unit. Evidence remains pending until one guarded process exits and the independent external-state audit passes.

The runtime observation must record, for both horse and pony:

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

Decision rule: do not resize the pony merely because it is called a pony. Prefer the already proven Large riding horse unless exact evidence shows the pony supplies a superior native companion or riding contract without imported content or broad behavioral compromise.
