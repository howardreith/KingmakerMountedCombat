# Player action and UI contract

Status: IN PROGRESS

## Player intent

Expose one unambiguous action that reads `Mount` while eligible and unmounted and `Dismount` while mounted. The selected rider remains the player-facing principal. A rejected action returns one exact primary reason and performs no partial mutation.

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

The final transient surface will be selected only after exact Kingmaker UI and lifecycle contracts are inspected; save safety takes precedence over blueprint-backed polish.
