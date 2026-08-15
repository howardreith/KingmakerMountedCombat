# Player action and UI contract

Status: IN PROGRESS

## Player intent

Expose one unambiguous action that reads `Mount` while eligible and unmounted and `Dismount` while mounted. The selected rider remains the player-facing principal. A rejected action returns exact ordered reason text and performs no partial mutation. Multiple independently failing requirements may be reported together so the player is not forced through one-error-at-a-time discovery.

## Minimum eligibility

- exact supported Medium rider/body profile;
- exact active owned `AnimalCompanionUnitMammoth` companion;
- current mount size strictly larger than rider;
- distinct, alive, conscious, directly controllable pair with valid views and ordinary agents;
- reciprocal active-companion relationship and same valid area state;
- no conflicting mounted relationship, unsupported polymorph/size state, transition, cutscene, loading, or blocked lifecycle boundary.

## Transition contract

Mount validates before mutation, acquires all owned leases transactionally, normalizes selection to rider, and rolls back on any partial failure. Dismount is idempotent and restores movement, avoidance, view parent/transform, pose, selection, and UI state. Double activation, stale action state, and lifecycle interruption fail closed.

## UI/camera observations

Runtime evidence must directly record selected unit identity, rider/mount click result, portrait highlight, selection-circle owner/position, action-bar owner, action label/availability/reason, camera subject while moving and after selection switches, party group routing, and cursor/ground-command recipient. Screenshots must include actual UI where a UI claim is made.

## Selected transient surface

The private alpha uses an owned bottom-right IMGUI overlay, not a blueprint or hotbar action. It is visible in a loaded area even when disabled so it can explain eligibility, becomes an enabled `Mount` only for the exact validated pair, becomes `Dismount` for active or fault-cleanup state, and is absent outside a loaded game. `MountedPlayerActionEvaluator` owns deterministic eligibility; `MountedPlayerActionController` projects exact Kingmaker state and delegates transitions; the `MonoBehaviour` draws and forwards only.

Offline checkpoint: component action tests `7 PASS / 0 FAIL`; complete component total `126 PASS / 0 FAIL`; harness `138 PASS / 0 FAIL`. The registered UMM-toggle native scenario now checks one owned overlay before disable, zero references/objects on the following disabled frame, and exactly one restored overlay after re-enable; live execution remains required twice.

Runtime controller qualification: exact clean commit `a344442fcf81de6ae49ce5770099d05874995de8`, package SHA-256 `7d2287f785d870c967c8d8ba54a1f458f989a030d2718f194e61808ebcd2ff2f`, DLL SHA-256/MVID `6e4a2d9b75f2e6a485b3f4da0234243b33c797df92191872724b393f912a784d` / `95e1f8fc-5aa3-4338-8921-bf5dff209d76`. Availability passed twice at `29/0`; mount/dismount passed twice at `44/0`. Each run used a fresh process, ended `Unmounted`, retained exact rider selection, produced zero relationship/movement/attachment residue, and restored Working, protected saves, and Mods exactly. These rows invoke `MountedPlayerActionController` directly and deliberately record `nativeDeliveryObserved=false`; actual IMGUI visibility/click delivery and UI capture remain `TODO` for Tranche B.
