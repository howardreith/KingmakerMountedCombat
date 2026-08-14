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

Offline checkpoint: component action tests `7 PASS / 0 FAIL`; complete component total `122 PASS / 0 FAIL`; harness `134 PASS / 0 FAIL`. Actual UI visibility/click delivery and two fresh-process flows remain `TODO`.
