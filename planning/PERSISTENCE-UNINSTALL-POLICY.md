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

Selected implementation: a composition-root-owned `KMC_PlayerActionOverlay` GameObject with `HideAndDontSave`, created only while the mod is enabled and destroyed on disable/unload. The overlay delegates evaluation and activation; it owns no relationship state and creates no blueprint, fact, buff, activatable ability, unit part, or hotbar record. This resolves the design choice but does not qualify uninstall safety until the guarded native scenarios pass twice.

## Required proof

Record actual native save request, load start/end, area unload/attach, mode change, combat, view, party, life-state, and UMM disable/unload delivery where safely obtainable. Direct handler/service invocation remains explicitly claim-limited and cannot substitute for native delivery.

The save boundary must record relationship state before request, synchronous cleanup completion, write authorization counters, serialized-state absence, post-boundary unit state, and exact Working/protected-save/Mods restoration.

Exact implemented delivery seams are `SaveManager.SaveRoutine` token `06008029`, `SaveManager.LoadRoutine` token `0600802C`, `ISceneHandler`, `IAreaLoadingStagesHandler`, `ITurnBasedModeEnabledHandler`, `IGameModeHandler`, `IUnitCombatHandler`, `IPartyCombatHandler`, `IUnitViewAttachedUIHandler`, `IUnitHandler`, `IUnitFinallyDeadHandler`, `IPartyHandler`, `IUnitLifeStateChanged`, `IInGameHandler`, and the UMM disable/shutdown path. A bounded 256-record ledger retains ordered source, relationship state before/after, exact cleanup trigger, attempt, and result. Native delivery remains unclaimed until live evidence is bound.
