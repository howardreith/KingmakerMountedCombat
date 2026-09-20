# Presentation pose strategy

Status: `PASS` for the accepted Mammoth-specific foundation; final Round 2 human presentation regression is `IN PROGRESS`.

The final technical package retains exact pose/view/attachment cleanup and does not change the accepted profile. Subsequent UI, transition, Wild Shape, and command repairs require one final human check for clipping, rider visibility, physical pointer feel, camera framing, menu fog/world flash, and overall presentation. This does not authorize reuse of Mammoth offsets for a horse.

The exact `medium-humanoid-mammoth-v1` presentation was explicitly accepted for continued private-alpha engineering at commit `09a63729e0847c540ae7e79e9e3876d005ee9afe`. Acceptance retains a slight seat gap, analytical stiffness, no saddle/reins, and the exact one-handed fixture-only coverage as known issues; it confers no support for another rider category or mount anatomy.

## Inherited evidence

Phase 1 qualified a reversible rider root attachment to a Mammoth-root-local point projected from `Spine`. It proved mechanical stability but left the rider rigidly upright with the lower body embedded/occluded. Classification remains `MECHANICALLY VIABLE, NEW ANIMATION/POSE WORK REQUIRED`.

## Investigation order

1. Inspect exact Kingmaker humanoid animation sets, rigs, seated/crouched/riding candidates, layer facilities, IK helpers, and post-animator hooks.
2. Reconfirm exact Mammoth anchor and animation behavior under the unchanged assembly/asset identities.
3. Evaluate native, legally usable references without extracting or redistributing assets.
4. If no suitable native pose exists, implement original code-driven procedural posing in a rider-view-owned adapter.
5. Store only deterministic project-authored profile data; never ship Kingmaker/Wrath meshes, clips, controllers, skeletons, or extracted payloads.

## Preferred fallback contract

- Capture every affected transform/animator value once per attachment lease.
- Apply deterministic pelvis/thigh/knee/lower-leg/foot rotations and offsets after ordinary animation evaluation.
- Derive each frame from the evaluated/captured basis rather than cumulatively mutating prior-frame output.
- Keep spine, shoulders, arms, hands, weapon, and shield available for ordinary upper-body animation where feasible.
- Scope updates to the exact active rider view; no global animator patch.
- Restore every transform and state on dismount, invalidation, native boundary, disable, exception, and destruction.
- Measure visible jitter, entity/view/anchor residual, cleanup residual, and per-frame cost.

## Required profiles and review

The only authorized shipping profile is one supported Medium humanoid body profile on `AnimalCompanionUnitMammoth`. Idle, walk, run, substantial turns, reversals, stop/start, wait, transitions, doorway, formation, selection switches, and safe equipment variants must be observed from ordinary isometric angles. A development calibration surface may exist, but the review profile must be deterministic with calibration closed.

No strategy is accepted until technical gates and the mandatory human visual review both pass.

## Exact local forensics

The implementation is pinned to Kingmaker `Assembly-CSharp.dll` SHA-256 `3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb`, MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`, and `Assembly-CSharp-firstpass.dll` SHA-256 `069a7362ce5e3ccd597206174aec13743c2db5a1bfbc2a42f15a5fbd1ea30d30`, MVID `57f03756-55de-42f5-8bb3-e983306082b2`. Exact relevant surfaces are:

| Surface | Metadata token | Finding |
|---|---:|---|
| `UnitEntityView.Animator` | `06001826` | exact rider animator availability gate |
| `UnitEntityView.CharacterAvatar` | `06001828` | exact supported rig root |
| `UnitEntityView.IkController` | `06001839` | stock IK ownership boundary |
| `IKController.BipedIk` / `GrounderIk` | `06001565` / `06001567` | stock FinalIK hands/grounding exists; no mounted pose contract |
| `Character.OnAnimatorUpdated` / `LateUpdate` | `0600140B` / `0600140C` | ordinary avatar post-animation path |
| Unity `DefaultExecutionOrder(Int32)` | `060001F4` | project-owned late view adapter scheduling seam |

Bounded type/member and asset searches found no Kingmaker-native mounted relationship, supported Mammoth rider layer, rider selector, or redistributable Medium-humanoid-on-Mammoth clip. Stock FinalIK handles ordinary biped/grounding concerns but does not expose a mounted lower-body profile. The native-clip route is therefore `FEATURE-NOT-PRESENT`; the project-authored procedural fallback is selected.

## Selected implementation

`MountedRiderPoseAdapter` is an exact rider-view component with `[DefaultExecutionOrder(32000)]`. It owns only seven transforms:

```text
Pelvis
L_Up_leg, L_leg, L_foot
R_Up_leg, R_leg, R_foot
```

The deterministic profile ID is `medium-humanoid-mammoth-v1`. It applies a bounded pelvis offset/tilt and solves each leg through original analytical two-bone code. Spine, chest, shoulders, arms, hands, weapons, shields, animator/controller state, Mammoth bones, and all non-pair views remain unowned.

Each late frame captures the current evaluated seven-bone basis before mutation. The next Update restores that frame basis before ordinary animation evaluates again, preventing cumulative pose drift. Dismount first deconfigures the pose and verifies the acquisition baseline, then restores the rider attachment, then restores movement authority. Failed restoration retains retryable lease state. View detach, death, combat, area, mode, disable, exception, and process cleanup continue through the existing relationship coordinator.

Eligibility now requires an exact active Animator/CharacterAvatar surface and one unambiguous hierarchy match for every profile bone. Duplicate or missing bones reject mounting without weakening the Medium-rider/Mammoth relationship gate.

## Fixed technical evidence contract

The seven presentation rows export profile/bone identity, component count, applied/healthy frame counts, analytical foot/knee/segment residuals, target clamping, maximum/average apply cost, local pelvis/foot frame deltas, equipment observations, UI identities, camera follow residuals, screenshots, and exact next-frame cleanup.

Technical PASS requires:

- exactly one owned pose component and seven owned bones;
- every observed mounted frame healthy and at least one applied pose frame;
- foot and knee target residual at most `0.025` world units;
- segment-length residual at most `0.001` world units;
- maximum apply cost at most `2000` microseconds and average at most `500` microseconds;
- `pose-idle` local pelvis/foot per-frame deltas at most `0.15` world units;
- exact component removal and verified bone-baseline restoration on the next cleanup frame.

The local gate is PASS at source `21/0`, Release build, component `138/0`, visual-capture contract `12/0`, harness/protocol `139/0`, and assembly contracts `101/0` (`90/0` Kingmaker, `11/0` Wrath). Runtime presentation rows and subjective visual acceptance remain `TODO`; these local results do not claim that the authored offsets look acceptable.
