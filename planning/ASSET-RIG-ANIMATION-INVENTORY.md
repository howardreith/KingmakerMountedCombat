# Asset, rig, and animation inventory

Status: IN PROGRESS

The metadata candidate and bounded-anchor subgate is PASS; runtime stability and visual classification remain unproven.

No proprietary object was copied, extracted, or committed. The inventory was produced by read-only, in-memory UnityFS/type-tree parsing of exact local Kingmaker files. Only factual metadata, identifiers, hashes, and original conclusions appear here.

## Rejected riding-oriented candidate

| Field | Exact local evidence |
|---|---|
| Blueprint internal name | `CR1_HorseRiding` |
| Blueprint type | `BlueprintUnit` |
| Blueprint asset GUID | `9e9e75c484e68734487e609714565202` |
| Prefab weak-resource ID | `5e0b93738ad54dd4ba101b3513ac4590` |
| Prefab asset path | `Assets/Mechanics/Bundles/Prefabs/Characters/Creatures/Medium/Horse/HorseRiding.prefab` |
| Bundle | `resource_5e0b93738ad54dd4ba101b3513ac4590` |
| Bundle identity | 2,963,744 bytes; SHA-256 `1aada90ae55bab86806afc276247ff7e22498ed22f7ae5effbc95ac5621f2643`; UnityFS format 6; Unity `2018.4.10f1` |
| Manifest identity | 2,965 bytes; SHA-256 `d1f1dbe4eae196d596368cfff025f15c14832b3b4aca9d3e5f64a43dd159f837`; CRC `4104375560`; AssetFileHash `d4b121f6cd61626e8523eb9205aa5e62`; TypeTreeHash `1a005fd7acbe24a046f09945e5acde9e` |
| Blueprint container evidence | `sharedassets1.assets`, 212,953,824 bytes, SHA-256 `cc779caf2fef21d111856a57d40b510677b0c236ad2723d1849564626445f785`; object path ID `99871`, serialized start `160569168` |
| Size | Serialized enum value `5`, which maps to exact Kingmaker `Size.Large` (`Fine=0` through `Medium=4`, `Large=5`) |
| View identity | `aafc2343-db64-42fb-b190-ad9165830711` |

### Why this candidate was initially attractive, and why it is rejected for the slice

The asset is native Kingmaker content, explicitly riding-oriented, Large, uses an ordinary stock movement agent, includes a dedicated horse locomotion set, and has explicit stirrup bones. However, exact component and reverse-reference inspection proves it is not a native animal companion: the BlueprintUnit has only `AddClassLevels` and `Experience`, no `AddPet`/companion owner references it, and its non-library references are ordinary campaign prototype/reference use. Using it would violate the Phase 1 invariant that the mount is the rider's exact active qualifying companion. `CR1_HorseRiding` is therefore rejected as the playable slice mount and retained only as evidence that Kingmaker-native riding presentation assets exist.

## Movement, footprint, and collision metadata

The prefab root is `HorseRiding` and contains 122 GameObjects/transforms. Its stock `UnitMovementAgent` has:

| Field | Value |
|---|---:|
| acceleration | `8.0` |
| minimum speed | `0.2` |
| angular speed | `360` |
| combat angular speed | `720` |
| connected to obstacles | `true` |
| avoidance disabled | `false` |

The serialized `UnitEntityView` has corpulence `0.9`, soft-collider height `2.4`, horizontal-collider flag `false`, and radius field `1.0`. The core collider child scale is `(1.8, 1.8, 2.499)`; the soft-collider child local position is `(0, 1.420199, 0.408288)`. These are historical facts about the rejected candidate, not the Phase 1 traversal control.

## Rig and anchor hypothesis

The generic quadruped rig root is `HorseRiding_body_RIG`. Relevant exact transforms include `LowerTorso`, `UpperTorso`, `Chest`, `L_Stirrup`, and `R_Stirrup`. There is no transform named Saddle, Mount, or Rider.

| Transform | Parent | Local position | Local rotation quaternion | Interpretation |
|---|---|---|---|---|
| `LowerTorso` | rig root | `(0, 1.554979, -0.441027)` | not required for gate | spinal root |
| `Chest` | `UpperTorso` | `(0, 0.069489, -0.436785)` | identity | first anchor hypothesis |
| `L_Stirrup` | `Chest` | `(0.305183, -0.122729, -0.044022)` | `(0, 0, -0.707107, 0.707107)` | left-foot diagnostic reference |
| `R_Stirrup` | `Chest` | `(-0.305183, -0.122733, -0.044021)` | `(0.707107, 0.707107, 0, 0)` | right-foot diagnostic reference |
| `Center` | root-level | `(0, 2.375, -0.364)` | not required for gate | static fallback only |

`Chest` was the conservative horse anchor because both authored stirrups are its children and it follows the spine during locomotion. `Center` was a root-level alternative but cannot reproduce gait-driven spinal motion. Neither is used by the selected Mammoth experiment.

## Native animation coverage

The prefab uses `ArmoredHorseAnimationSet_LocoMotion` plus a dedicated animator controller. Factual state/clip coverage includes:

- idle: `Idle01`, `Idle02`, `Idle_OnTheAlert`;
- transition/walk: `Into_Walking_02`, `Walking_Cycle_02`, `moving_slow`;
- transition/run: `Into_Running_02`, `RunningCycle02`;
- stop transitions;
- attacks: hoof, bite, and gore;
- reactions/lifecycle: stun, stand-up, and `Dying_Death`.

This proves native idle/walk/run/stop coverage for the horse. It does not prove a Medium humanoid pose, hand/weapon clearance, anchor stability, or aesthetically acceptable gait.

## Selected active-companion candidate

| Candidate | Blueprint GUID | Prefab resource ID | Evidence / decision |
|---|---|---|---|
| `AnimalCompanionUnitMammoth` | `e7aa96d15a45238438ae4cfb476f6bb9` | `7b53a073462398d419f504d542083085` | Selected native companion at companion rank 7+. `AnimalCompanionFeatureMammoth` GUID `6adc3aab7cde56b40aa189a797254271` contains `AddPet` linking exactly to this unit; `SetMaster` supplies the reciprocal runtime relationship. `AnimalCompanionUpgradeMammoth` GUID `6a23d16a4476af644af89d91f9f96790` applies size delta +1 at rank 7. |
| `AnimalCompanionUnitSmilodon` | `8a6986e17799d7d4b90f0c158b31c5b9` | `fab2f65ceb662cd4c972b76ec52fefde` | Native companion; not selected because its bounded rig/anchor metadata was not qualified as completely as the Mammoth's. |

No Wrath model, clip, controller, material, texture, or offset was imported or proposed for redistribution. The Mammoth lacks the horse's authored stirrups and riding-specific rig, so indoor visual/geometry quality is UNKNOWN pending an identical unmounted Mammoth control. It is nevertheless the conservative invariant-correct candidate. The vertical slice must never substitute the horse by weakening companion validation.

### Mammoth exact size and ownership boundary

The `AddPet` component references `AnimalCompanionRank` GUID `1670990255e4fe948a863bafd5dbda5d`, `AnimalCompanionUpgradeMammoth`, and `UpgradeLevel=7`. The upgrade's exact `ChangeUnitSize` component has `Type=Delta` and `SizeDelta=+1`. The base BlueprintUnit size is Medium (`4`), so ordinary rank-7 progression makes it Large (`5`), not Huge. The `Creatures/Huge` prefab folder is not runtime size evidence. Current `UnitState.Size > rider.State.Size` is authoritative at mount time; rank alone cannot override later size effects.

### Mammoth prefab, movement, and footprint

| Field | Exact value |
|---|---|
| Prefab asset path | `Assets/Mechanics/Bundles/Prefabs/Characters/Creatures/Huge/Mastodon/MastodonPet.prefab` |
| Bundle | `resource_7b53a073462398d419f504d542083085`, 3,717,344 bytes, SHA-256 `3f7d374792e1edf0d4c967e087810dfafba8326cf53bdb516e8c2ddb39b47523`, UnityFS format 6 / Unity `2018.4.10f1` |
| Manifest | 2,966 bytes, SHA-256 `99cc7c2466995e32fecc893eae30842cad7e1c595a9555f0071fd98aa05ea911`, CRC `2562734889`, AssetFileHash `63e59f64105eb4a57556378707a46d58`, TypeTreeHash `1a005fd7acbe24a046f09945e5acde9e` |
| View unique ID | `ba595338-edaf-40d8-ac8f-ea94a444c699` |
| Movement agent | exact type `Kingmaker.View.UnitMovementAgent`; acceleration `8`, minimum speed `0.2`, angular `360`, combat angular `720`, connected to obstacles `true`, avoidance disabled `false`; ordinary unit movement-speed value remains runtime unresolved |
| Footprint | corpulence `0.7`, soft-collider height `1.2`, horizontal `true`, radius `0.8`, core collider scale `(1.2, 1.2, 2.499)` |

### Mammoth rig and anchor hypothesis

The prefab root is `MastodonPet` with 148 transforms. The back chain is `MastodonPet -> mastodon_body_RIG` (scale `0.3`) `-> Position -> root -> LowerTorso -> Spine -> UpperTorso`. No Saddle, Mount, or Rider transform exists.

The first bounded anchor hypothesis is animated `Spine`: parent `LowerTorso`, local position `(-1.516907, 0.019180, 0)`, local quaternion `(0, 0, 0.065891, 0.997827)`, local scale `(1, 0.999713, 0.999713)`. Unity vectors are local Cartesian `(x,y,z)` and quaternions are recorded `(x,y,z,w)`. The serialized bind-chain places it approximately `(0, 1.060003, -0.147889)` relative to prefab root after rig scale; that is a metadata inference, not a runtime transform measurement. `UpperTorso`, local position `(-1.612483, 0.000050, 0)` and quaternion `(0, 0, -0.391332, 0.920250)`, is a higher-pitch alternative. Rider offset and rotation remain runtime calibration. The present default-off adapter uses `Spine.TransformPoint(offset)` plus `Spine.rotation` and independently measures position/rotation residual; this is an implemented anchor-transform experiment, not runtime stability proof.

The Mammoth uses `Mastodon_AnimationSet_LocoMotion` with states Idle, moving-near-enemy, Walk, and running. Factual clip identifiers include `idle2`, `idle_on_alert`, `slow_walk`, `walk`, `run`, `death`, `gore_attack`, `gore_attack_02`, `slam_attack`, `slam_attack2`, stun/prone/stand-up/cast/idle variants. No dedicated rider pose, saddle anchor, or authored turn clip was found.

## Runtime-only unknowns and acceptance

The following remain UNKNOWN — MORE EVIDENCE REQUIRED:

- exact disposable-fixture rider stable ID and body/skeleton type;
- Mammoth back-anchor stability and vertical displacement during idle, walk, run, turn, and stop;
- calibrated rider local position, rotation, and scale;
- body, weapon, foot, door, and ceiling clipping;
- selection circle and camera-distance presentation;
- maximum residual error during stationary wait and repeated reversals;
- animation/pose classification.
- localized/display name, exact rider body/skeleton identity, animator-controller resource path/ID, and explicit hit-clip coverage.

The selected Mammoth must have an unmounted control run through the same doorway/corner geometry before any mounted failure is classified. No generated proprietary metadata output exists to hash: inspection was read-only and in-memory.

Visual classification: IN PROGRESS. Horse metadata proves a Kingmaker-native presentation asset exists but cannot satisfy pair qualification; Mammoth metadata supports an invariant-correct diagnostic experiment, not `PLAUSIBLE FOR PHASE 2`.
