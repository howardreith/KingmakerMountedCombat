# Persistence and uninstall policy

Status: IN PROGRESS

## Product policy

The Phase 2 mounted relationship is transient and intentionally nonserialized.

- Save begins only after synchronous clean dismount and residue validation.
- Load begins and completes unmounted; no automatic remount.
- Area transition begins only after clean dismount.
- Mod disable/unload/session stop removes relationship, movement, attachment, pose, selection, UI, and diagnostic-target state.
- Uninstall must leave no required custom fact, buff, activatable ability, unit part, hotbar reference, or orphaned rider/mount reference in a save.
- `KMC_AUTOMATION_BASELINE` is immutable. Only exact `KMC_AUTOMATION_WORKING` may be replaced inside the guarded transaction.

## Player-action constraint

Prefer a transient project-owned UI/hotkey surface. Do not add a blueprint-backed fact or hotbar action unless exact registration, serialization, removal, feature-disabled load, mod-absent load, and stale-reference repair are independently proven. If those proofs are unavailable, the private alpha must retain the transient surface even if it is less polished.

## Required proof

Record actual native save request, load start/end, area unload/attach, mode change, combat, view, party, life-state, and UMM disable/unload delivery where safely obtainable. Direct handler/service invocation remains explicitly claim-limited and cannot substitute for native delivery.

The save boundary must record relationship state before request, synchronous cleanup completion, write authorization counters, serialized-state absence, post-boundary unit state, and exact Working/protected-save/Mods restoration.
