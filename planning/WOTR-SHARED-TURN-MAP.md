# Exact Wrath Shared-Turn Map for Phase 3D

Phase 3E final reconciliation (2026-09-05): Kingmaker can be extended pair-locally at `UnitActionController.TickCommandTurnBased` (`0x0600911D`) to execute one exact mount command while the rider remains current; dev.4 A/B proves that command lifecycle. Kingmaker still lacks Wrath's durable paired actor/command links and pair-aware turn-completion controller. Final dev.12 runtime evidence shows the independent `CombatController.ChooseNextUnit` path can retain the redundant mount after the leased action is already complete. A rider-owned scheduling shell cannot influence that later decision without assuming broader turn-controller/roster ownership. The bounded Phase 3E disposition is therefore separate-turn fallback, not a Wrath transplant and not a claim that unified TB is accepted.

Phase 3E turn-selection reconciliation (2026-09-05): installed Kingmaker `CombatController.Tick` `0x06000BD1` calls `ChooseNextUnit` `0x06000BD2` before disposing/clearing the ended `CurrentTurn`, and `ChooseNextUnit` searches from `CurrentTurn?.Unit ?? m_NextUnit`. Therefore an exact active-mount candidate is observed/deferred in the selector postfix and skipped only from a post-`Tick` postfix after `CurrentTurn == null`. This preserves Kingmaker's native selector and roster; it does not copy Wrath's controller, mutate the current unit, or call `StartTurn`. Dev.10 exposed the ordering defect; dev.11 runtime proof remains pending.

Phase 3E reconciliation (2026-09-04): Kingmaker Gate 1 now qualifies an original pair-local Option A eligibility extension twice (`140/0`) without importing Wrath control flow. The exact Kingmaker seam is `UnitActionController.TickCommandTurnBased` `0x0600911D`; KMC leases one explicit mount-owned command while the rider remains the native principal. Wrath remains evidence only. Dev.8/dev.9 rider Mount-shell provenance work changes no conclusion in this map and does not add a Wrath dependency.

Status: PASS — bounded interoperability contract

Authority: exact installed Wrath `Assembly-CSharp.dll` SHA-256 `2cb7160b7154d4ffacc77b9c51b1eb26199e1294300f04fdfc073367b2ef8953`, MVID `90a9869c-2792-4c7b-bfb7-5a8b33da7c82`. Findings come from bounded local type/member inspection. No Wrath code, assembly, blueprint, icon, animation, or asset is copied into KMC.

| Responsibility | Exact Wrath finding | Kingmaker Phase 3D disposition |
|---|---|---|
| Relationship | Reciprocal `UnitPartRider`/`UnitPartSaddled`; mount/dismount raise native relationship events. | Retain KMC transient relationship service; no serialization. |
| Initiative choice | `CombatController.ChooseNextUnit` `0x06000E88` skips a unit with `GetRider()!=null`; `UnitCombatState.IsWaitingInitiative` is false for a saddled mount. | Skip only the exact active KMC mount candidate; keep native roster. |
| Shared controller | `CombatController.StartTurn` `0x06000E91` creates a `TurnController` for the rider; controller stores rider and current mount. | Keep real Kingmaker rider controller and add pair-local coordination. |
| Separate ledgers | Wrath stores rider/mount commands, combat state, cooldown, movement stats, attack mode, and action state separately. | Native cooldowns remain on each actor; KMC adds only missing mount movement state. |
| Turn completion | `IsAllActed` and `ContinueActing` consider both actors and both running-command sets. | Scoped continuation patch keeps rider-led turn until both are done or player ends it. |
| Turn setup | `Prepare` clears both cooldown sets, accounts existing acting commands, calls both combat states' new-round hooks, and emits the rider turn event. | Initialize mount once only on a natural rider-turn start; preserve resources on mid-turn merge. |
| Selection | Wrath exposes `SelectedUnit` as rider or mount depending on selection, while the controller's `Rider` remains the initiative principal. | User-authoritative Phase 3D narrows this: rider always remains selected/UI/camera principal. |
| Movement | `TickMovement(unit,...)` selects rider or mount movement stats and cooldown by moving actor. | Mount physical motion charges mount ledger through a scoped Kingmaker adapter. |
| Five-foot step | Per-actor movement stats share one movement-limit mode; step sets shared disengage immunity. | Track exact mount step while preserving stock unmounted and ordinary AoO paths. |
| Rider-to-mount commands | `SaddledUnitController.TickDelegateRiderToMount` links a rider command to a mount attack or move; copies LoS and player/AI origin. | Stock rider `UnitAttack` becomes KMC pair intent; mount movement remains a separate exact command. |
| Melee pair attack | For rider melee, Wrath may create a mount attack and links both commands. | Deterministic rider-first then mount, with independent native attack children and no full-parity claim. |
| Ranged rider | If rider attack is ranged, Wrath avoids unnecessary mount melee; it creates a mount attack only when already legal, otherwise a move toward rider attack radius. | Stop at native rider range/LoS; do not force melee; Mount Primary remains explicit. |
| Mount-to-rider command | A player mount command against a hostile can create/link the corresponding rider command. | Phase 3D keeps rider as the sole ordinary-click principal; no separate portrait click. |
| Mount/dismount in combat | Combat controller reconciles navmesh on mount events; turn controller reads current relationship. Relationship actions are separate from action cost. | Merge/split at scoped boundaries and charge rider Move through KMC native ability. |
| RT to TB | Shared relationship survives; turn roster skips saddled mount. | Synchronize pair initiative, select rider once, preserve actor cooldowns. |
| TB to RT | `CombatController.Disable` converts ordered turn state to RT initiative cooldowns. | Reconcile mount initiative cooldown to rider placement without clearing attack/Move ledgers. |
| Invalidation | `SaddledUnitController` force-dismounts for unconscious/prone/pit/incompatible/enemy state. | Existing KMC lifecycle cleanup remains authoritative and notifies shared-turn pending split/cleanup. |

The reference establishes architecture and UX responsibilities, not source parity. Kingmaker lacks Wrath's relationship parts, paired command fields, and two-actor turn controller, so the implementation must remain bounded and reversible.

## Kingmaker runtime disposition

Dev.21 proves the missing paired-command scheduler is material, not merely structural. With the rider as Kingmaker's real current turn, an admitted Mammoth-owned Standard `UnitAttack` remains unstarted because stock TB advances commands only for their executor when that executor is `CurrentTurn.Unit`. Wrath's native controller explicitly owns and ticks both rider and mount commands; the pair-local Kingmaker coordinator cannot reproduce that behavior through initiative filtering and ledger preparation alone. Further emulation requires a separately authorized architecture and risk budget. The accepted separate-turn fallback remains intact.
