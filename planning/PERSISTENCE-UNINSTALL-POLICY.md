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

Exact implemented delivery seams are `SaveManager.SaveRoutine` token `06008029`, `SaveManager.LoadRoutine` token `0600802C`, `ISceneHandler`, `IAreaLoadingStagesHandler`, `ITurnBasedModeEnabledHandler`, `IGameModeHandler`, `IUnitCombatHandler`, `IPartyCombatHandler`, `IUnitViewAttachedUIHandler`, `IUnitHandler`, `IUnitFinallyDeadHandler`, `IPartyHandler`, `IUnitLifeStateChanged`, `IInGameHandler`, and the UMM disable/shutdown path. A bounded 256-record ledger retains ordered source, relationship state before/after, exact cleanup trigger, attempt, and result.

The Phase 2 native-save probe is deliberately narrower than a save round trip. It revalidates the exact Manual Working descriptor and current file identity, requires ordinary `Game.SaveGame` entry conditions with ironman-only mode off, arms one exact nonfatal Working-write suppression, and calls real `Game.SaveGame`. The exact-token `SaveRoutine` prefix must clean Mounted to Unmounted and consume the suppression once; Harmony substitutes an empty iterator before stock screenshot, descriptor replacement, or serialization work. The callback must complete and Working length, timestamp, and SHA-256 must remain exact. Baseline, Auto, Quick, new-slot, indirect-path, wrong-campaign, and every unarmed write remain fatal.

The Phase 2 native-mode probe uses exact Kingmaker MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7` and pinned `SettingsEntityBool.m_Cached` `04002275`, `SettingsEntityBase.OnOptionUpdatedCallback` `04002269`, `OnInvokeUpdateCallback` `06003359`, `GetSavedValueString` `0600335C`, `EnableTurnBasedMode` `04007C9F`, and `OnlyOneSave` `04007CBC`. It substitutes only the in-memory nullable cache, invokes the already registered `GameSettingsController` callback/EventBus route in both directions, restores the exact raw cache, and requires the persisted settings string to remain identical. It does not invoke a settings setter or claim PlayerPrefs/settings-UI delivery.

The Phase 2 native-area probe calls real `Game.ReloadArea` only after durable pre-boundary evidence and requires ordered native delivery `OnAreaBeginUnloading` < `OnAreaScenesLoaded` < `OnAreaDidLoad` < `OnAreaLoadingComplete`, fresh-world fixture identity, zero KMC residue, and unchanged Working identity. The disable probe invokes the exact registered UMM toggle delegate, requires synchronous Mounted-to-Unmounted cleanup, observes zero overlay/anchor/agent residue on the following disabled frame, and re-enables to exactly one overlay. It does not claim a user UMM click or physical mod-file deletion. All four probes remain unqualified until two fresh-process live passes are bound.
