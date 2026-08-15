# Asset, rig, and animation inventory

Status: PASS

The native-candidate inventory, exact Mammoth ownership/size gate, bounded anchor, live pair stability, and evidence-backed presentation classification are complete for Phase 1. The result is `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`; it is not a polished mounted pose or a claim of broad indoor/party compatibility.

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

No Wrath model, clip, controller, material, texture, or offset was imported or proposed for redistribution. The Mammoth lacks the horse's authored stirrups and riding-specific rig. In the revised F1 fixture, the same-current-Mammoth unmounted matched control and mounted doorway traversal pass twice under unchanged gates. The Mammoth remains the conservative invariant-correct candidate. The vertical slice must never substitute the horse by weakening companion validation.

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

The first bounded anchor hypothesis was animated `Spine`: parent `LowerTorso`, local position `(-1.516907, 0.019180, 0)`, local quaternion `(0, 0, 0.065891, 0.997827)`, local scale `(1, 0.999713, 0.999713)`. Unity vectors are local Cartesian `(x,y,z)` and quaternions are recorded `(x,y,z,w)`. The serialized bind-chain places it approximately `(0, 1.060003, -0.147889)` relative to prefab root after rig scale; that is a metadata inference, not a runtime transform measurement. `UpperTorso`, local position `(-1.612483, 0.000050, 0)` and quaternion `(0, 0, -0.391332, 0.920250)`, remains a higher-pitch metadata alternative.

Live callback-order evidence rejected following the animated full-`Spine` quaternion directly. The qualified adapter instead projects the `Spine` point once into an owned root-local `KMC_RiderPositionAnchor`, inherits authoritative Mammoth-root translation and upright yaw, and holds the rider view through a reversible parent lease. Current visible-view/full-rotation and phase-adjusted logical entity position/yaw are measured separately under the unchanged `0.10` gates; raw logical lag remains exported, authority-bounded, and recovery-gated rather than hidden.

The Mammoth uses `Mastodon_AnimationSet_LocoMotion` with states Idle, moving-near-enemy, Walk, and running. Factual clip identifiers include `idle2`, `idle_on_alert`, `slow_walk`, `walk`, `run`, `death`, `gore_attack`, `gore_attack_02`, `slam_attack`, `slam_attack2`, stun/prone/stand-up/cast/idle variants. No dedicated rider pose, saddle anchor, or authored turn clip was found.

## Live findings and remaining presentation limits

The guarded Working fixture resolved exactly one valid pair:

- rider stable ID `b6628a77-4962-47a4-a17c-88d9836fc9d5`, current size 4 (Medium);
- Mammoth stable ID `d79a4f6c-b74e-4868-95bd-533899131acb`, current size 5 (Large);
- exact active-companion ownership and blueprint identity;
- valid stock agents, views, out-of-combat state, Default mode, and `Spine`-derived anchor.

Lifecycle A/B and all movement A/B rows qualify the root-local attachment across create/clear, idle, open movement, doorway traversal, selection away/back, stock group formation, pause, cancel, stop/restart, repeated turns/corners, and cleanup. Current visible-view position/full rotation and phase-adjusted logical position/yaw remained inside the unchanged `0.10` gates; raw phase lag was bounded and recovered; stationary drift and final outstanding recovery were zero. Cleanup restored the original rider parent, sibling, world pose, local scale, `ForbidRotation` value, stock agent, avoidance lease, and selection without attachment residue.

Usable explicit-camera frames across F0 and F1 show a stable, readable humanoid-on-Mammoth silhouette at ordinary game distance, including doorway, selection, and formation contexts. They also show the decisive presentation defect: the dwarf remains rigidly upright, the lower body is embedded or occluded by the Mammoth back, and there is no seated pose, saddle, or reins. Some historical late-state frames are black, clipped at the frame edge, or otherwise unusable. Camera-only evidence does not prove portrait state, camera-follow behavior, pause UI, or the structured away/back selection state.

The F1 matched doorway control proves the current Mammoth can traverse the selected open StandardDoor geometry unmounted, and mounted traversal passes twice. This is bounded doorway presentation evidence, not proof of broad ceilings, closed doors, alternate maps, or clipping-free indoor compatibility.

The following remain intentionally unqualified:

- the rider's exact body/skeleton and animator-controller resource identity;
- a polished rider offset, seated pose, hand/weapon/foot placement, saddle, or reins;
- body, weapon, foot, door, and ceiling clipping across the full required state matrix;
- portrait, camera-follow, selection-circle, and party-formation UI behavior beyond the structured world-state evidence;
- broad indoor behavior, additional rider bodies/sizes, and additional mount species;
- combat, hit, attack, charge, and mounted action animations.

No generated proprietary metadata output exists to hash: inspection was read-only and in-memory. No Wrath asset was imported, copied, or required.

## Phase 2A bounded pose decision

Exact local assembly inspection confirms the selected rider view exposes an Animator, CharacterAvatar, and stock `IKController`/FinalIK surface, but Kingmaker contains no supported Mammoth-rider clip, mounted animator layer, or rider selector. The authorized fallback therefore owns only the standard Medium-humanoid transforms `Pelvis`, `L_Up_leg`, `L_leg`, `L_foot`, `R_Up_leg`, `R_leg`, and `R_foot`. Runtime eligibility rejects a missing or ambiguous match. The exact profile ID is `medium-humanoid-mammoth-v1`; no asset, animation clip, controller, skeleton, or extracted payload is added to the package.

The pose deliberately leaves spine, arms, hands, and all equipment transforms under ordinary Kingmaker animation/IK. Runtime evidence will inventory every active/nonempty hand set available in the protected Working fixture and report one-handed, two-handed, and shield coverage truthfully; an unavailable safe variant is `FEATURE-NOT-PRESENT`, not synthetic coverage. Analytical residuals and baseline cleanup are technical gates. Pelvis burial, foot placement, silhouette, weapon/shield clearance, and ordinary-angle acceptability remain reserved for the mandatory human review of the exact packaged build.

Visual classification: `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`. This proves one remotely plausible Kingmaker-native presentation and prevents K9 from firing. The mandatory Phase 1 doorway, selection, and formation rows are now complete, but pose/animation quality remains a separately scoped Phase 2 prerequisite. This record does not authorize Phase 2.
