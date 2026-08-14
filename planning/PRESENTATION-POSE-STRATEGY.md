# Presentation pose strategy

Status: IN PROGRESS

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
