# Known issues remediation, Sprint 4 — scene continuity (C04, C01, C02, C03): handoff (2026-11-17)

Sprint 4 of the Mounted Combat Known Issues Remediation Epic, in the owner's order C04, C01, C02, C03. Offline work
only. Nothing was launched, staged or installed, and nothing in this record is native gameplay evidence.

## Review of `e1c0c7ad` (R7–R9), 2026-11-17 — read this first

The owner accepted, at `e1c0c7ad`:
- R4's owed-versus-spent notice, the no-receiver retry and the loading gate;
- R5's final-orientation supersession while a positional placement is pending;
- R6's independent fault latches;
- C04's mounting-instance observations;
- C01's unsupported disposition and its guards.

R5 was not complete at `e1c0c7ad`. A standalone turn was discarded (R7), and a whole-pair write was missed when one
actor was already at its authored transform (R8). Both are corrected here, together with logging truncation (R9).

| Item | Value |
|---|---|
| Starting head (reviewed) | `e1c0c7ad2235abdfcd6fff15eb3c07859016e289`. Its product DLL is the `e62cad39` build (SHA-256 `c1bec351…1dfa4728`, MVID `9b5abe93-…`), frozen as `artifacts/known-issues-sprint4/head-e62cad39/` |
| Parent | `codex/kmc-known-issues-sprint3` at `e5b92474` |
| Repair commit | `ed83339c` (R7, R8, R9: product, tests and both contract scripts; the product-build commit), then the records commit carrying this section, which is the branch head |
| Product DLL (clean Release build of `ed83339c`) | SHA-256 `e3448787557d3fb7e063c717367f14efa01b626b81b1080ddc8ea1a390c95bc5`, MVID `c94292d4-973f-42e9-bd7c-31969d7f317e`; frozen copy `artifacts/known-issues-sprint4/head-ed83339c/` |

### R7 — an exact rider-only turn with nothing pending

- **Reproduced:**
  1. With the reviewed build's compiled classification and hold, as its `Attribute` composes them, a rider-only turn
     classified `RiderTurn`.
  2. `Reorient` on an empty hold returned `Unchanged`, so nothing was held.
  3. The reviewed component test asserted that swallowed outcome.
- **Repair:** `MountedScenePlacementHold.Reorient(x, y, z, orientation, frame)` changes as follows:
  - With nothing pending, it holds the rider's current position with the authored orientation.
  - That goes through the ordinary bounded scene-placement handoff (applied once, translocation verified).
  - With a placement pending, it still only takes the final orientation.
  - Scope is unchanged: only the three patched operations, the exact mounting instance and synchronizer, and no global
    orientation observer.
- **In combat:** the existing guard applies. There is no suspension: one refusal, one return to the saddle, no
  oscillation. Nothing is granted, prepared, refunded or replayed.
- **After (executed, compiled core and hold):**
  - Nothing pending; a turn to 75° at (4, 7), mount unchanged, in a scene: (4, 7) at 75° is pending.
  - In the component scene: one handoff at the rider's position and 75°.
- **Controls (component, production core and hold):**
  - Outside a scene: nothing.
  - Mount-only turn: nothing.
  - Ordinary turning between operations: nothing.
  - A mounting replaced inside the operation: nothing.
  - Combat: one refusal, never pending again.
  - Repeated turns (75 then 200): one handoff at 200.
  - Nested: the outermost final turn.
  - 10 to 370 (the same facing): nothing. 350 to 10: a turn.
- **Safety:** source evidence showed nothing unsafe in using the existing lifecycle for this case. The handoff,
  refusal and guards are the ones every C02 placement uses.

### R8 — whole-pair intent from the proven target set

**Native contract (installed IL; `OnRun` call sequence pinned):**

| Fact | Kind |
|---|---|
| `CommandTeleportParty.OnRun` (06009E90) reads `m_UnitsList`, calls `Game.Instance.Player.GetCharactersList(m_UnitsList)` once and enumerates it. For each unit it does `Targets[i % Targets.Length].FindView()`, writes `Position` (the locator's position, or zero with an error log if the view is missing) and `Orientation` (the locator's `transform.rotation.y`). There is no filter and no comparison. | IL read and pinned |
| `GetCharactersList`: `ActiveUnits` gives `ControllableCharacters`. `Everyone` gives `AllCharacters`. `EveryoneNotDetached` and `AllDetachedUnits` give the cross-scene units, filtered by `IsDetached`. `DetachedPartyCharacters` and `PartyCharacters` give the `UnitReference` values. | IL read and pinned |
| `UpdateCharacterLists` / `AddCharacterToLists`: pets are added to `ControllableCharacters` (in game, not detached, not ex-companions) and to `AllCharacters`. `PartyCharacters` holds no pets. So the mount is in `ActiveUnits` and `Everyone`, and not in `PartyCharacters`. | IL read (decompiled), not pinned |
| The list is lazily evaluated over live lists that `OnRun`'s setters do not change. Evaluating it in the prefix therefore yields the units native will write. | Inferred from the IL |
| Executing `GetCharactersList` offline | Not feasible: `UnitReference` resolution and `UpdateCharacterLists` need the live entity pool and unit descriptors |

- **Repair:**
  - Each patched operation has its own prefix naming it: `MountedScenePlacementSource`. The display string is no
    longer authority.
  - The `CommandTeleportParty` prefix takes the operation instance and reads `m_UnitsList`. It evaluates
    `GetCharactersList` and records whether this exact rider and mount are in the set (`RiderTargeted`,
    `MountTargeted`).
  - `ClassifyOperationWrite` (eight inputs) counts a proven write of an actor as a write even when its value was
    already there. Both actors written is `WholePair`: the hold is cleared and nothing is moved.
  - Single-unit operations (`TranslocateUnit`, `CommandMoveUnit`) carry no target evidence and classify by changes, as
    before.
- **Before and after:** a rider-only placement A was pending, then `CommandTeleportParty` ran. Executed through the
  reviewed build's compiled classification and hold, as its `Attribute` composes them, and through this build's
  compiled core:

  | Case | Reviewed `e1c0c7ad` | Corrected |
  |---|---|---|
  | 1. Both targeted; rider already at its authored transform, only the mount changes | A stays pending, and is reapplied | Cleared; no handoff |
  | 2. Both targeted; mount already at its transform, only the rider changes | Replaced by another rider-only hold, and handed off | Cleared; not replaced |
  | 3. Both targeted, both change | Cleared | Cleared |
  | 4. Both targeted, both already equal | A reapplied | Cleared; nothing moved |
  | 5. Only the rider targeted (for example `PartyCharacters`) | Rider placement | Rider placement (supersedes A) |
  | 6. Single-unit operation on the mount | A stays | A stays |

- Preserved: mounting-instance and synchronizer identity, nested operations, the 3 m rule, R7, combat refusal, and no
  native resource or action change.

### R9 — a throwing success logger truncated the loops

- **Reproduced:**
  - The reviewed `Attribute` called `logger.Info` inside its pair loop with no containment.
  - The reviewed `Recheck` called it inside its zone loop. A throw ended the loop at the hook's catch.
  - The reviewed loop shape is transcribed in the component test and shown to skip the second pair. The IL pins for
    "no logging in the loop body" fail on the reviewed build.
- **Repair:**
  - `MountedScenePlacementAttributionCore.Apply` (Domain) applies every pair's own writes to its own hold. A success
    report that throws is contained per pair.
  - `MountedScriptZoneChecks.CheckEach` (Domain) checks every zone and contains a throwing change report. A failing
    native check still ends the pass, so the hook's bounded R6 report still sees it.
  - The Integration code calls them and logs only through their report delegates.
- **After (executed, compiled code):**
  - Two pairs in one operation, with a logger that throws on each success: both holds updated, 2 logger calls, nothing
    escapes. A healthy logger receives both.
  - One rider and two zones, with a throwing logger: both zones checked and both memberships right. A healthy logger
    gets both lines, and a failing check propagates.
  - R6's independent latches are re-executed unchanged: one script-zone and one scene-placement line; a throwing fault
    logger called twice.

### Records corrected by this review

- **R4:** "DeliveredCount counts notice deliveries with at least one successful receiver." The per-dispatch `accepted`
  value in the notifier is the receiver count. `DeliveredCount` is not.
- **R5:** "a rider-only turn alone starts nothing" described the swallowed outcome, not a repair. R7 replaces it.
- **R5:** whole-pair classification by changed values alone missed a proven write. R8 replaces it.
- R5 is complete only with R7 and R8.

### Checks of this review

| Check | Result |
|---|---|
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint4`, focused: both Sprint 4 contract scripts, persistence, post-load view readiness, continuous drive, world ownership, physical mover pace | TIER FAST PASS=10 FAIL=0, 0.78 min; budget spent 2.87 of 90 minutes over 4 entries |
| Components | 1367/0 (1362 at `e1c0c7ad`; +2 R7, +1 R8, +1 R9 pairs, +1 R9 zones) |
| `Validate-Source.ps1` | 151/0 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 |
| Clean Release build of `ed83339c` (outputs removed first) | PASS; identity above |
| `Test-ScenePlacementAttributionContracts.ps1` | 19/0. Reviewed `e1c0c7ad` build 12/7: R7, R8 and R9 executed, and the R5/R8 and hook wiring pins |
| `Test-CandlemereCrossingContracts.ps1` | 11/0. Reviewed build 9/2 (the two C04 R9 checks) |
| Targeted mutations (each restored), log `32-review-mutations-r7-r9.log` | M10 standalone turn discarded: 2 component failures and the R7 check. M11 proven targets ignored: R8 component test and the R8 check. M12 pair loop uncontained: R9 component test and the R9 check. M13 zone loop uncontained: C04 R9 component test and the Candlemere R9 check |

- Logs are in `artifacts/known-issues-sprint4/`, numbered 30–36.
- Not run: Full, Candidate, historical campaigns, release qualification, unrelated stale harnesses.

### Evidence kinds for this review

| Kind | What |
|---|---|
| Production code executed offline | `MountedScenePlacementAttributionCore`, `MountedScenePlacementHold`, `ClassifyOperationWrite`, `MountedScriptZoneChecks` and the coordinator (component tests and compiled-product checks) |
| Installed native code executed | None new (R4's `EventBus` checks still run) |
| Installed native IL inspected | `CommandTeleportParty.OnRun`'s call sequence; `Player.GetCharactersList` and the character-list construction |
| Engine behaviour represented | Native position and orientation writes, the capture around operations, the synchronizer, zones and their membership |
| Modelled orchestration | The owner's poll and the evaluation order in the component scene |
| Native gameplay | None; not run |

### Remaining evidence limits

- `GetCharactersList` was not executed offline. That the horse is in `ActiveUnits` and `Everyone` and not in
  `PartyCharacters` is read from decompiled code.
- Not seen in play:
  - a standalone copy-rotation `TranslocateUnit` of a seated rider;
  - a `CommandTeleportParty` with one actor already at its locator;
  - logging failures in either hook;
  - every earlier native gap still listed below.

## Review of `263ac3ea` (R4–R6), 2026-11-17

The owner accepted, at `263ac3ea`:
- C04's mounting-instance observations;
- C02's supersession for different endpoints and its position-changing whole-pair clear;
- C03's custody and encounter deduplication and its use of the native warning interface;
- C01's unsupported disposition and its guards.

This review raised two P2 findings and one P3 diagnostic finding.

| Item | Value |
|---|---|
| Starting head (reviewed) | `263ac3ea7b923c1e4c269c8570d6e1ada8524ffc`. Its product DLL is the `b80534f9` build (SHA-256 `70bbe6f7…a15099`, MVID `fc4bb6b3-…`), frozen again as `artifacts/known-issues-sprint4/head-263ac3ea/` |
| Parent | `codex/kmc-known-issues-sprint3` at `e5b92474` (PR #48 stays stacked on it) |
| Repair commit | `e62cad39` (R4, R5, R6: product, tests and contract script; the product-build commit), then the records commit carrying this section, which is the branch head |
| Product DLL (clean Release build of `e62cad39`) | SHA-256 `c1bec3516b1c1dfd6c0e5b72406ef805e3f54cbcdd6936321de87cf31dfa4728`, MVID `9b5abe93-648e-46c6-941a-18cae1c96568`; frozen copy `artifacts/known-issues-sprint4/head-e62cad39/` |

### R4 — the notice was spent before any receiver existed

**Native contract (installed Kingmaker 2.1.7b):**

| Fact | Kind |
|---|---|
| `EventBus.RaiseEvent<T>(Action<T>)` returns `void`. It passes straight to `GlobalSubscribers.RaiseEvent`. | IL read |
| `SubscriptionManager.RaiseEvent`: with no subscriber list for `T`, it returns without invoking anything. Otherwise it invokes the action once per subscriber. It catches each subscriber's exception and logs it through `UberDebug`, then continues. Nothing reports how many ran. | IL read |
| With nobody subscribed, the compiled notifier's dispatch reaches nobody. With a subscribed sink (a class implementing the installed interface), the sink receives the text once per dispatch. | Executed (installed native `EventBus`) |
| Offline, a throwing subscriber's exception escaped native's catch, because `UberDebug` logging cannot run outside the game. | Executed |
| The installed warning receivers are `Kingmaker.UI.WarningsText`, `Kingmaker.UI.Log.BattleLogManager` and `CombatLogVM` (console UI). | Reflection over the installed assembly |
| `WarningsText` subscribes in `Initialize` and unsubscribes in `Dispose`. `StaticCanvas.Initialize` and `Dispose` reach their child UI elements. They are called from `StaticCanvas.OnAreaDidLoad` and `OnAreaBeginUnloading`. | IL read |
| `OnAreaDidLoad` is raised by `Game.OnAreaLoaded`, the callback of the last `LoadingProcess` step (`InitSpawnersCoroutine`). | IL read |
| The warning receivers are therefore absent from the start of unloading until area-load, and can be present while the load is still in progress. | Inferred from the IL above; not observed in play |
| That `WarningsText` is a child of the `StaticCanvas` children list. | Inferred; the UI scene data was not read |

- **Reproduced:** with the reviewed `263ac3ea` build, run through the compiled notifier and the installed native
  `EventBus`:
  1. Held custody A was evaluated with no sink subscribed, and nobody received anything.
  2. A sink subscribed.
  3. The same still-held custody was evaluated again and received nothing, because the notice had been consumed.
  4. During a load, with a receiver present, the reviewed notifier delivered at once. It has no load gate.
- **Repair:**
  - `CombatHoldNotice` now separates owing from spending:
    - `IsOwed` reserves nothing;
    - `ShouldAttempt` is false while a load is in progress;
    - `Spend` is called by the notifier.
  - `MountedCombatHoldNotifier` counts receivers inside the action that native invokes per subscriber. The notice is
    spent only once at least one receiver was reached, or if the dispatch itself throws (bounded, one attempt).
  - `DeliveredCount` counts notice deliveries with at least one successful receiver (corrected by the review of
    `e1c0c7ad`; the per-dispatch accepted value is the receiver count). `FailedCount` counts spent attempts no
    receiver took. `NoReceiverCount` counts attempts that reached nobody and spent nothing.
  - A dispatch that reaches no receiver is retried at the next evaluation. This happens only while scene-staging
    custody is held in combat, and costs one dictionary lookup per evaluation.
  - The restore loop passes `LoadingProcess.Instance.IsLoadingInProcess`.
  - The notifier owns no retained, combat, restoration, action or cleanup state. Its logging failures are contained.
- **After (executed, corrected build):**
  - No receiver: nothing is spent. After the sink subscribes, the same custody is delivered once.
  - During a load, with a receiver present: nothing, while the compiled `Decide` holds scene-staging custody in combat
    during the load. After the load: once.
  - Repeated evaluations stay suppressed. A later encounter is delivered again, and so is a new custody held directly
    in combat.
  - A refusing display gets one attempt (Failed) and nothing escapes the notifier.
- **Component tests (production `CombatHoldNotice`, receiver represented):** owed until received; held during a load,
  then once.
- **Not shown:** the warning on screen. `WarningsText` rendering and the battle-log entry are native UI and were not
  run.

### R5 — orientation-only scene placements

**Native contract (installed IL, pinned):**

| Operation | Writes `Orientation`? | Condition | Same position possible? | Units |
|---|---|---|---|---|
| `TranslocateUnit.RunAction` (06008FA2) | Yes, once, after `Position` | Only when its `m_CopyRotation` field is set, on its locator branch (not its position-evaluator branch); no position comparison | Yes: it writes the locator's position, which may equal the unit's current one | The one unit its `Unit` evaluator names: rider or mount |
| `CommandTeleportParty.OnRun` (06009E90) | Yes, once per listed character, after `Position` | Unconditional; the value is the locator's `transform.rotation.y` (a quaternion component, as authored natively) | Yes | Each character of its list: rider, mount or both |
| `CommandMoveUnit.IsFinished` (06009E4E) | No | — | — | — |
| `UnitEntityData.set_Orientation` | Stores the value as given, and sets `DesiredOrientation` | — | — | — |

- **Reproduced:** with the reviewed build's compiled classification (positions only, as its attribution composes it):
  placement A wrote P at 30°, then placement B wrote the same P at 120°. B was classified `None`, and P/30 stayed
  pending.
- **Repair:**
  - The capture records both units' orientations.
  - `MountedSceneContinuityPolicy.OrientationWritten` compares by the shortest wrapped difference, beyond 0.001°, so 0
    and 360, and -90 and 270, are the same facing.
  - `ClassifyOperationWrite` sees moves and turns:
    - moving or turning both units is `WholePair`, which clears a pending rider-only placement;
    - moving the rider is `RiderPlacement`;
    - only turning the rider, in a scene, is `RiderTurn`.
  - `MountedScenePlacementHold.Reorient` takes a `RiderTurn` while a placement is held. At `e62cad39` a `RiderTurn` with
    nothing pending started nothing, and the turn was lost. Corrected by R7 of the review of `e1c0c7ad`: it is held at
    the rider's position.
  - The scope is unchanged: only the three patched operations, no global orientation observer, and the 3 m
    unattributed rule untouched.
- **After:** P/120 is pending, and the handoff applies it once. This was executed through the compiled decisions,
  pinned to the attribution by IL, and in the component scene with the production hold.
- **Component cases (production hold and classification):**
  - same endpoint, final orientation (the reviewed position-only attribution, transcribed, keeps 30);
  - a rotation-only whole-pair operation clears the rider-only hold;
  - a mount-only turn keeps it;
  - a rider-only turn alone started nothing (the swallowed outcome; replaced by R7);
  - nested operations take the outermost final turn;
  - a mounting replaced inside the operation is not attributed;
  - combat refuses once;
  - nothing outside a scene;
  - ordinary turning between operations changes nothing.
- **Correction:** R2's claim that the "final endpoint and orientation" stand held only when the endpoint changed. The
  orientation alone was not observed until R5.

### R6 — one fault latch for two hooks

- **Reproduced:** with the reviewed build, real operational failures in the compiled hooks: a stateless relationship
  service makes each hook's own work fail inside production code. The C04 fault was logged once. The C02 fault was
  never logged, because the shared latch was already spent.
- **Repair:** `MountedPatchController` has `scriptZoneFault` (C04) and `scenePlacementFault` (C02 capture and
  attribution), each its own `MountedBoundedFaultReport`.
- **After (executed):**
  - With a recording logger: one script-zone line and one scene-placement line across four rounds of failures in both.
  - With a throwing logger: two attempts, and nothing escapes.
  - Every hook is `void`, so native always runs.
- **Correction:** any earlier statement that C02 and C04 faults were reported independently was not true before
  `e62cad39`.

### Checks of this review

| Check | Result |
|---|---|
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint4`, focused: both Sprint 4 contract scripts, persistence, post-load view readiness, continuous drive, world ownership, physical mover pace | TIER FAST PASS=10 FAIL=0, 0.97 min; budget spent 2.1 of 90 minutes over 3 entries |
| Components | 1362/0 (1356 at `263ac3ea`; +2 R4, +4 R5) |
| `Validate-Source.ps1` | 151/0 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 |
| Clean Release build of `e62cad39` (outputs removed first) | PASS; identity above |
| `Test-ScenePlacementAttributionContracts.ps1` | 14/0. Reviewed build 8/6: R4 reproduction, R4 wiring, R5 wiring, R5 same endpoint, R6, and the C02 hook pin (now requiring C02's own report). The native R5 pin passes on both |
| `Test-CandlemereCrossingContracts.ps1` | 9/0 on both builds (C04 code unchanged) |
| Targeted mutations (each restored), log `21-review-mutations-r4-r6.log` | M7 (a dispatch nobody received spends the notice): R4 check fails. M8 (a same-position turn not observed): 2 component failures and the R5 check. M9 (one shared fault budget): R6 check fails |

- Logs are in `artifacts/known-issues-sprint4/`, numbered 20–26.
- Not run: Full, Candidate, historical campaigns, release qualification, unrelated stale harnesses.

### Evidence kinds for this review

| Kind | What |
|---|---|
| Production code executed offline | `CombatHoldNotice`, orientation comparison and classification, `MountedScenePlacementHold`, the coordinator and handoff sequence (component tests). The compiled notifier, compiled decisions and compiled hooks (contract script) |
| Installed native code executed | `EventBus.Subscribe`, `Unsubscribe` and `RaiseEvent` with an emitted receiver |
| Installed native IL inspected | The `EventBus` and `SubscriptionManager` paths, the warning receivers' subscription points, the three operations' orientation writes, the Orientation setter |
| Engine behaviour represented | The receiver in component tests; position and orientation writes, the capture, the synchronizer and the poll in the component scene |
| Modelled orchestration | The restore loop's evaluation sequence |
| Native gameplay | None; not run |

### Remaining native evidence limits

- The warning on screen and in the battle log after a loaded combat.
- A copy-rotation `TranslocateUnit` at a seated rider's held endpoint.
- A `CommandTeleportParty` that turns a mounted pair in place.
- Faults in either hook in play.

## Review repair of `b30c24f8` (R1–R3, C01 wording), 2026-11-17

The owner's review of the first Sprint 4 checkpoint (`b30c24f8`) left three findings open and narrowed the C01
statement. Each finding was reproduced in source and under matched conditions before its repair. Sections below
this one describe the first pass and are corrected where this review changed a claim.

| Item | Value |
|---|---|
| Reviewed head | `b30c24f8586e1b86d351a6fa214e309226ab83aa` (its DLL is the frozen `head-9a371910` copy; the records commit changed no product code) |
| Repair commits | `60ba91ae` R1; `3cbdf077` R2; `fb7f048d` R3; `b80534f9` R3 test coverage (the product-build commit); then the records commit carrying this section, which is the branch head |
| Product DLL (clean Release build of `b80534f9`) | SHA-256 `70bbe6f75f1e7116dc80f72d94ebaa6948c761df7141e35264446cc778a15099`, MVID `fc4bb6b3-900f-4b7e-999c-a72fac3865ca`; frozen copy `artifacts/known-issues-sprint4/head-b80534f9/` |

### R1 — C04's zone observations outlived the dismount

- **Reproduced (source and component sequence):** `MountedScriptZoneAdapter.Recheck` returned when the relationship's
  rider was null, before its only removal. A normal dismount's cleanup clears the runtime's rider and mount
  references, so the rider-keyed entry survived. Sequence: checked at P inside the zone, dismounted (references
  cleared), walked outside (native membership updated), remounted the stationary mount at P without native motion.
  The first rider-first tick found the old entry at P and did not check: membership stayed "outside" and the crossing
  sent the party back. The reviewed cache, transcribed in the test and labelled as such, does exactly that.
- **Repair:** observations are scoped to the mounting instance. `MountedScriptZoneObservations` (Domain) keys each
  observation by the rider and records the coordinator's `MountedPair`, which every `Attach` creates anew. The adapter
  reads `GameMountedRelationshipService.MountingInstance`; an observation of another instance is never reused. No
  cleanup has to reach it, so reference clearing cannot strand it.
- **Preserved:** a stationary rider within one mounting is not checked again. Native zone enter and exit are untouched.
- **After:** the same sequence checks the rider on the first mounted tick, and the crossing goes across. This holds
  on the same relationship (remount on the same reusable boundary), on another relationship (a second coordinator),
  and for another world's unit (another object).
- **Evidence (executed):**
  - The C04 model now mounts and dismounts through the real `MountedRelationshipCoordinator`, with a runtime that
    clears its references on cleanup. The "tracked=false on dismount" stand-in is gone.
  - Decisions and records are the production observations.
  - The native tick and the zone remain represented.
- **Contract pins:** the adapter asks the observations with the mounting instance, and the compiled observations are
  instance-scoped. The reviewed build fails both.

### R2 — C02 kept the first of two placements

- **Reproduced:** `AttributeScenePlacement` refused while a placement was pending. With placement A, then placement B
  before the owner's poll, the handoff translocated the rider back to A with orientation A. A later whole-pair
  placement left the rider-only target to be reapplied. The unattributed path had the same first-wins hold.
- **Repair:**
  - The pending state is now `MountedScenePlacementHold` (Domain), used by `RiderMovementAgent`. It holds one
    placement, always the scene's latest.
  - A later rider placement supersedes the pending one: final endpoint and orientation.
  - An operation that writes the whole pair (`ClassifyOperationWrite`) clears it. The synchronizer then resumes as
    after any whole-pair placement, so nothing obsolete is reapplied.
  - An unattributed write since the hold supersedes it only by the unchanged 3 m rule, measured from the held target.
    This is applied at every sample and at the owner's poll (`CurrentScenePlacement`), so neither order reapplies an
    earlier target. A near unattributed write, 1 m from the held target, is not taken.
  - The attribution acts only on the mounting instance and synchronizer it captured.
  - Refusal, the frame budget, Configure and Deconfigure release or reset as before.
- **Cases (component, production hold; native writes, capture and poll represented):**

  | Case | Reviewed hold (transcribed) | Corrected |
  |---|---|---|
  | Two rider-only placements before the next poll | Back to the first endpoint and orientation | One handoff, final endpoint and orientation |
  | 6 m then 7 m, attributed | 6 m | 7 m |
  | 6 m then 7 m, unattributed, during the hold | 6 m | 7 m (far from the held target) |
  | Short attributable placements (0.5, 2, 3 m) | Kept | Kept |
  | Later whole-pair placement | Obsolete rider-only target reapplied | Cleared; no handoff; pair together at the mount's placement |
  | Nested operations | First write kept | The rider's position at the end of the outermost operation |
  | Combat, two placements | One refusal | One refusal, one return to the saddle |

- **Contract pins:** the wiring (instance check, classification, hold at sample and poll) and the compiled hold. The
  reviewed build fails both.
- Not connected to the Candlemere incident; no evidence links them.

### R3 — C03's notice reached no player

- **Reproduced (source):** `Report` only sets `Feedback` and `FeedbackIsRoutine` and writes `logger.Info`. No
  gameplay display reads it, and the old feedback panel is gone.
- **Repair:** `MountedCombatHoldNotifier` keeps the custody-aware deduplication (`CombatHoldNotice`) and delivers this
  notice only through the native `IWarningNotificationUIHandler` warning (`HandleWarning(text, addToLog: true)`).
  This is the path the project's other player warnings use. Other routine Reports are unchanged.
- **Text:** "&lt;pairs&gt; stay dismounted for this fight, because they were still part of a scene when it began. After
  the fight they remount automatically if they still can." Identities and the mechanism go to the diagnostic log.
- **Failure:** delivery is contained. The notifier owns no retained or combat state, never throws, and does not repeat.
- **Corrected by review R4:** this repair spent the notice before dispatch and counted every dispatch as delivered, so
  a dispatch nobody received (a loaded combat before the UI subscribes) consumed it. An EventBus call alone does not
  prove delivery. See the R4 section.
- **Evidence (executed against installed native code):** `Test-ScenePlacementAttributionContracts.ps1` runs the
  compiled notifier through the installed native `EventBus`. It delivers to a subscribed sink, a class implementing
  the installed interface that the probe emits. Results:
  - the actual text once across repeated held evaluations;
  - again for a later encounter after an evaluation outside combat;
  - again for a new custody held in combat right after the previous one disappeared;
  - a refusing display neither throws out of the notifier nor gets a second attempt.
- **What that run does not show:** the warning on screen. Offline, native's own catch of a subscriber exception
  rethrew while logging (UberDebug cannot run outside the game), so the notifier contained it (failed=1). In the game,
  native catches and logs it itself.
- **Coverage gap closed:** a targeted mutation (any earlier custody suppresses the notice) passed the component tests,
  which always evaluated outside combat between custodies. `b80534f9` adds the direct case to both layers.

### C01 (wording narrowed; still unsupported, not fixed)

- **Blocker:** the existing scene-staging lifecycle cannot take an actively participating pair out of combat while
  preserving the required restoration intent under its present contract. Its handoff refuses during an encounter, and
  its suspension requires settled participation.
- **Future designs:** capturing and restoring a live activation is not established as the only possible design. A
  combination of the existing native split with a separate deferred remount intent is another candidate. It would
  need its own investigation and verification.
- **Not done:** no refusal was removed or weakened, and no combat-authority design was introduced.

### Checks of this repair

| Check | Result |
|---|---|
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint4`, focused: both Sprint 4 contract scripts, continuous drive, post-load view readiness, persistence, world ownership, physical mover pace | TIER FAST PASS=10 FAIL=0, 0.77 min; budget spent 1.13 of 90 minutes over 2 entries |
| Components | 1356/0 (1349 at `b30c24f8`; +2 R1, +5 R2; C03 cases extended) |
| `Validate-Source.ps1` | 151/0 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 |
| Clean Release build of `b80534f9` (outputs removed first) | PASS; identity above |
| `Test-CandlemereCrossingContracts.ps1` | 9/0; reviewed build 7/2 (the two R1 pins) |
| `Test-ScenePlacementAttributionContracts.ps1` | 9/0; reviewed build 5/4 (the R2 and R3 pins) |
| Targeted mutations (each restored), log `10-review-mutations.log`, rerun `11-review-mutation-m6-rerun.log` | M1 instance ignored: 3 component failures. M2 first wins: 3. M3 whole pair clears nothing: 1. M4 no supersession during the hold: 1. M5 notice not raised: delivery check fails. M6 any earlier custody suppresses: delivery check fails, and after `b80534f9` one component failure too |

- Logs are in `artifacts/known-issues-sprint4/`, numbered 10–16.
- Not run: Full, Candidate, historical campaigns, release qualification, unrelated stale harnesses (T04–T08 untouched).

### Evidence limits after this repair

| Kind | What it covers |
|---|---|
| Executed production code | The observations, hold, classification, notice deduplication, handoff sequence and coordinator; the compiled notifier through native `EventBus` |
| Represented engine behaviour | The native tick and zone membership, native position writes, the capture around operations, the synchronizer's saddle write, the owner's poll |
| Compiled wiring (IL pins) | The adapter, attribution, agent and restore-loop calls |
| Native gameplay | None; still not run |

## Branch and identity

| Item | Value |
|---|---|
| Base (accepted Sprint 3 head) | `e5b9247429cd16a7605de5616ed9934082cb238e` (draft PR #47, open, unmerged) |
| Branch | `codex/kmc-known-issues-sprint4`, worktree `worktrees/known-issues-sprint4` |
| Parent of the first Sprint 4 commit | `e5b92474` |
| Commits | `ef264ff3` C04; `eabbd8c8` C02; `9a371910` C03 and the C01 pins (the product commit); then the records commit carrying this file, which is the branch head |
| Draft PR | Stacked on `codex/kmc-known-issues-sprint3` so it shows only the Sprint 4 delta; no reviewers |
| Product DLL (clean Release build of `9a371910`) | SHA-256 `3ef14c3a36a42ef542689baf5bd746b5cd3d98dd27503c4ddec7e0ebc0558968`, MVID `495b82b0-1b4b-4885-b53b-345cf5f62a0e` |
| Base DLL (clean Release build of `e5b92474`) | SHA-256 `fcbba76ed68df28f2ddb578ae2eb287d556a8c297473b39d639f06ea6e4a2c57`, MVID `16c76ae9-7416-4e49-8de5-937b61be2926` |
| Frozen copies | `artifacts/known-issues-sprint4/base-e5b92474/` and `artifacts/known-issues-sprint4/head-9a371910/` (outside the repository) |
| Installed game | Pathfinder: Kingmaker 2.1.7b, Assembly-CSharp MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`; read in place, never modified |

The records commit changes documentation only, so the product DLL above is the head's DLL.

## Sprint 3 at the start of Sprint 4

- The owner accepted the Sprint 3 correction at `e5b92474` on 2026-11-16 as a review checkpoint. No more Sprint 3
  repair was requested.
- That acceptance is a source review of offline work: M01–M03 and M05 are implemented and verified offline. Their
  native acceptance is still not run.
- Not resolved by the acceptance: M04's original report (investigation complete, issue unresolved), S01, and the
  harness findings T04–T08. They keep their existing dispositions.

## Dispositions

| ID | Disposition | One line |
|---|---|---|
| C04 | Implemented and verified offline; native not run | A seated rider's script-zone membership now follows its carried position, so the crossing chooses the right destination |
| C01 | Explicitly unsupported, with its exact blocker | A scene's move order on a seated rider during combat still waits for the scene's own timeout; no safe lifecycle can take the pair in an encounter |
| C02 | Implemented and verified offline; native not run | A native scene placement operation that writes only the rider is attributed, so placements of 3 m or less keep their endpoint out of combat |
| C03 | Intentional limit documented, plus one separate demonstrated repair | The limits are unchanged and stated below; the combat-hold notice is now given once per custody and encounter, to the player |

## Material categories

The same words are used throughout.

- **Observed:** seen in the game, a game log or a player report. Sprint 4 observed nothing new. The only observed
  material is the owner's report and the owner's description of `output_log(9).txt`.
- **Inspected:** installed files read in place: scene data (Unity serialized files, format 17), native IL and its
  metadata tokens, decompiled reference kept outside the repository.
- **Executed:** production code run offline: the component tests (Domain code linked into the test program) and the
  contract scripts' PRODUCT-EXECUTED checks (the compiled DLL).
- **Modelled:** test code that stands in for the game: frame loops, native ticks, position writes. Every model is
  named in its test's header.
- **Inferred:** a conclusion drawn from inspected and executed material that was not observed in play.

## C04 — the Candlemere crossing while mounted

### Evidence

- `output_log(9).txt` (SHA-256 `4613866bff774bfb8a77b55f04edc4a56558e8303a60028cc56dcd5eb9a2624c`) is **not on this
  machine**. It was searched for again on 2026-11-17 by SHA-256 under Downloads, Desktop, Documents, the lab folder
  and the game's LocalLow folder. Six other `output_log` files exist and none matches, so none was substituted and the
  log was not read.
- The owner's statements about it are recorded as the owner's, not re-read. The area ID
  `37a183451942e4340a096c561e4249d2` is the area, not a crossing interactable.

### The native path (inspected)

| Element | Contract |
|---|---|
| Interaction | Map object `FindWay` in the Candlemere scene (level348). Display name `Interaction_CandlemereTower_FindWay_DisplayName` is "Wade through" (CutWay is "Slash"). Component `InteractionSkillCheck`: Lore (Nature), DC 25, Approach, radius 4, `OnlyCheckOnce` and `TriggerActionsEveryClick` set, no component teleports. |
| Actor selection | `InteractionSkillCheck.OnInteract(user)` (06001A0A). The acting unit is the highest-stat unit among selected units that `CanAct && CanMove`, skipping pets when more than one unit is selected. |
| Check and cost | `GameHelper.CheckSkillResult` once; the stored result is reused on later clicks. No resource cost. |
| Failure | `DisableMapObjectInteraction(InteractedMapObject)`. |
| Success (in order) | `UnlockFlag`; then a Conditional `UnitInScriptZone(InteractingUnit, "SortcutBefore")`: if true, `TeleportParty` to `CandlemereTowerShortcut`, otherwise to `CandlemereTowerShortcutBack`; then `UnlockFlag`; then `DisableMapObjectInteraction(CutWay)`. |
| Mechanism | `TeleportParty.RunAction` (06008F8F), then `Game.Teleport` (06000CCB), then the TeleportParty loading process: `Player.MoveCharacters` (06000DCC) and `AreaEnterPoint.PositionCharacters` (060016F6), which interrupts, stops and writes `Position` directly for every controllable character, pets included. This is the fade the owner saw. |
| Completion | `ITeleportHandler.HandlePartyTeleport`. The product's local-teleport prefix ignores a teleport that is not an area transfer. |
| Geometry | The `Shortcut` point (4.20, 5.20) is outside the polygon `SortcutBefore`; `Shortcut_Back` (4.05, 13.46) and `FindWay` (4.44, 9.14) are inside. The authored crossing distance is 8.26 m. |
| Zone membership | `UnitInScriptZone.CheckCondition` (06008D42) reads `ScriptZone.ContainsUnit` (06001AE9), the zone's `m_Inside` set. `ScriptZoneController.Tick` (060092A0) calls `ScriptZone.CheckUnit` (06001AEB) only for awake units with `HasMotionThisTick` (0600830F, `Position != PreviousPosition`). `UnitMoveController.Tick` (06009183) stamps `PreviousPosition` first. |

### Cause (inferred from inspected material, not observed)

- A seated rider's position is written by the product's synchronizer, not by native motion. When the rider is
  processed before its mount in the awake loop, the rider never shows native motion, so the zone never rechecks it.
  Its stale membership says it is still outside `SortcutBefore` after walking in. The Conditional then teleports the
  party to `CandlemereTowerShortcutBack`, which is where it already stands: the screen fades and nothing crosses.
- Dismounting gives the rider native motion again, which matches the report.
- Not established offline: the actual awake order of rider and mount in the owner's game. With the mount processed
  first, the rider's view is carried through the mount's anchor and shows native motion, and the defect does not
  occur. The report is consistent with rider-first order.

### Correction (`ef264ff3`)

- `MountedScriptZonePolicy.RiderNeedsZoneRecheck` (Domain) decides when a seated rider needs a recheck: the pair is
  mounted, native did not check the rider this tick, and the rider moved since the last recheck, or was never
  rechecked.
- `MountedScriptZoneAdapter.Recheck` (Integration) runs in a postfix of `ScriptZoneController.Tick` (060092A0). It
  calls native `ScriptZone.CheckUnit` on native's own active zones. It writes no position, runs no action and
  replays nothing. Native's checks, failures, costs and teleport are untouched.
- Faults are contained by the bounded fault report. Until review R6 (`e62cad39`) that report was shared with C02's
  hooks, so a C04 fault silenced C02's first diagnostic and the reverse. Each now has its own.
- Corrected by review R1: the first pass claimed tracking was dropped when the pair was not mounted. It was not after
  a normal dismount, whose cleanup clears the references the adapter read first. Observations now belong to the
  mounting instance and are never reused by a later mounting or another world's unit (see the review section).

### Distances kept apart

| Distance | Value | Owner |
|---|---|---|
| Authored crossing distance | 8.26 m between the two enter points | Native scene data; untouched |
| C02 recognition distance | More than 3 m, for unattributed writes only | Product synchronizer; unchanged |
| C03 regroup limits | 2 m restore separation, 30 m regroup, 1 m approach, 2 walks of 20 s | Product restoration; unchanged |

### Cases (component tests, `CandlemereCrossingTests`; the native tick is modelled, the decision is production)

| Case | Base (adapter off) | Corrected |
|---|---|---|
| Mounted, near side, rider-first order | Goes to `CandlemereTowerShortcutBack` (no crossing) | Crosses to `CandlemereTowerShortcut` |
| Both directions, repeated activation | Wrong side on the near side | Right side each time; repeats reuse the stored result |
| Mount-first order | Already correct | Unchanged; no recheck is needed |
| Failed check | Interaction disabled | Same; nothing extra |
| Unmounted control, unrelated pair | Correct | Unchanged; never rechecked |
| Manual dismount, then remount | Correct while dismounted | Checked again on the first mounted tick, because the observation belongs to the earlier mounting (review R1; the first pass's model cleared it unconditionally) |

### Contract script

`scripts/Test-CandlemereCrossingContracts.ps1` has 8 checks: 3 ASSET (FindWay's fields, the action lists, the
geometry), 2 NATIVE-IL, 2 PRODUCT-IL and 1 PRODUCT-EXECUTED. It reads the installed files in place and stores no
extracted data. Corrected build 8/0. Base `e5b92474` build 5/3: the asset and native checks pass and the three
product checks fail.

### Missing native evidence

A Candlemere run with a mounted pair that logs
`Mounted script-zone membership followed the carried rider: ...; zone=SortcutBefore; inside=True` and then crosses.
The original `output_log(9).txt` remains unavailable.

## C01 — scripted rider movement during combat

### The concrete command (inspected)

- `CommandMoveUnit.OnRun` (06009E4D) runs a `UnitMoveTo` on the unit through `UnitCommands.Run`.
- On a seated rider, the product's scene-command observer decides `KeepAttachedInCombat` during an encounter, and the
  command runs natively on the seated rider. `UnitCommand.TickApproaching` asks the rider's view to move, but the
  rider's movement agent follows the saddle, so the rider never reaches the approach point.
- `CommandMoveUnit.IsFinished` (06009E4E) completes only on its timeout (`TakingTooLong`), and then writes `Position`
  to the target directly.
- Native does not pause a cutscene for combat. Of the six installed methods that set a cutscene's `Paused`, only
  `CutsceneController.Activate` reads `IsInCombat`, and only to interrupt AI commands before it pauses background
  cutscenes by `IsBackground`. Pinned.

### Why no supported path exists (the exact blocker)

| Candidate | Blocker |
|---|---|
| The existing staging lifecycle (pair-local suspension with retained intent) | `TryHandOffForStaging` refuses during an encounter before any suspension. Beneath that, `SuspensionRefusal` requires settled participation: `HasAreaParticipationObligations` calls each participant's `TrySettleExplorationParticipation`, which cannot settle while any participation actor `IsInCombat`. The retained custody's participation capture is defined only for settled participation. Lifting either refusal would weaken a safety guard. |
| A plain in-combat dismount (the existing paired split on cleanup triggers) | It keeps the native split but loses the post-combat restoration intent, which must be preserved. |
| Forwarding the order to the mount | Forbidden (no blanket forwarding of commands to the horse). |
| Completing the command early | It would synthesize the scene's timeout, which counts as manufactured completion. |

### The truthful fallback (unchanged, now bounded by C02)

- The command runs natively on the seated rider and waits for the cutscene's own timeout. The pair stays attached and
  spends nothing.
- At the timeout, native writes the rider's target. C02 attributes that write. The in-combat handoff is refused once,
  with one warning line, and the rider returns to the saddle once. Nothing oscillates (component test
  `RefusedHandOffSettlesOnce`).
- Pinned as unchanged: the PRODUCT-IL check "C01 fallback" in `Test-ScenePlacementAttributionContracts.ps1` passes
  on both the base and the corrected build.

### What a future solution would need (narrowed by the review)

The existing scene-staging lifecycle cannot take an actively participating pair out of combat while preserving the
required restoration intent under its present contract. Capturing and restoring a live activation is one conceivable
design, not established as the only one. Combining the existing native split with a separate deferred remount intent
is another. Either would need its own investigation and verification. Neither is a Sprint 4 change.

## C02 — rider-only scene placements of 3 m or less

### Native placement operations (inspected; all write `Position` directly, none through `UnitEntityData.Translocate`)

| Operation | Token | Write |
|---|---|---|
| `TranslocateUnit.RunAction` | 06008FA2 | Stops the unit, writes `Position` (and orientation when copying rotation) |
| `CommandMoveUnit.IsFinished` | 06009E4E | Writes `Position = Target` on its taking-too-long branch only |
| `CommandTeleportParty.OnRun` | 06009E90 | Writes `Position` and orientation for each unit of its character list |

### Before and after

- **Before:** the synchronizer took a rider write for the scene's placement only if it was more than 3 m from the
  saddle and from its own last write, during a scene. Nearer writes cannot be told from synchronization lag by
  distance, so they were snapped back.
- **After (`eabbd8c8`):**
  - Around each of the three operations, a prefix captures every mounted pair's rider and mount positions, and a
    postfix compares them.
  - `MountedSceneContinuityPolicy.AttributesRiderPlacement` decides: pair mounted, scene active, rider written, mount
    not written (Unity's position equality).
  - An attributed write is handed to that rider's synchronizer (`RiderMovementAgent.AttributeScenePlacement`) as the
    scene's own placement, whatever its distance. The existing handoff then keeps it (captured rider, authored
    endpoint, translocation verified within 0.1 m).
  - Corrected by review R2: the first pass kept the first of several placements. The scene's latest now stands, and a
    whole-pair placement clears a pending rider-only one (see the review section).
  - Unattributed writes keep the unchanged rule, now named `HoldsUnattributedPlacement`. The 3 m threshold is not
    lowered, and ordinary saddle correction is never classified as a teleport.

### Acceptance (component tests `ScenePlacementAttributionTests`; frame loop and native write modelled; decisions, handoff sequence and coordinator production)

| Case | Base | Corrected |
|---|---|---|
| Out of combat, rider-only placement at 0.5 m, 2 m, 3 m | Snapped back to the saddle | Endpoint kept, handed off, `HandedOffPlacementPreserved` |
| Out of combat, 6 m | Endpoint kept | Endpoint kept |
| In combat (C01 has no supported path) | Rider at the saddle | One refusal, one return to the saddle, no oscillation |
| Ordinary small movement (lag) | Corrected to the saddle | Same; no handoff |
| Mount-only placement | Rider follows the saddle | Same |
| Rider and mount both written | Pair together at the mount's placement | Same |
| Placement outside a scene | Snapped back | Same |
| Unrelated unit written | No effect | Same |

Identity, save and control ownership and cleanup go through the existing handoff unchanged: pair-local suspension,
exact owner identity and generation, retained custody.

### Contract script

`scripts/Test-ScenePlacementAttributionContracts.ps1` has 7 checks: the native writes; registration of the three
operations by token; the hook chain and the unchanged distance rule; the compiled decisions; and the C01 and C03 pins
listed in their sections. Corrected build 7/0. Base build 3/4: both native checks and the unchanged C01 fallback pass;
the C02 and C03 product checks fail.

### Missing native evidence

A scene that translocates a seated rider within 3 m, logging `Scene placement of a seated rider attributed to ...`
and then a handoff `at the scene's placement`.

## C03 — restoration fallbacks and feedback

### Intentional limits (kept; source unchanged)

| Limit | Value and behaviour | Feedback |
|---|---|---|
| Combat starts while a pair is staged | The pair fights unmounted and remounts after combat without Mount, preparation or refund. Scene staging is held through the encounter (`Decide`: `ScriptedEvent` holds; camp and world-map custody cancel). | Now one routine player report per custody and encounter (repair below) |
| Combat duration | Not the regroup timeout. Every hold evaluation resets the settle state and each pair's eligibility and staging-patience stamps (`ResetRetainedSettle`), so time in combat consumes neither the 10 s eligibility patience nor the 60 s staging patience. A walk in progress is interrupted; its attempt stays spent, and its 20 s deadline restarts with the next walk. | The interruption is logged once per walk |
| Retained-intent arrival patience | Separate from the above: the arrival's own placement geometry (`StandsAtArrivalPlacement`), unchanged | Unchanged |
| Restore separation | Rider within 2 m of the mount | — |
| Regroup | Mount walks to within 1 m, only out of combat, at most 2 walks of 20 s game time each, up to 30 m, same navigation area only | "the mount could not regroup with its rider" or "rider and mount were separated by the event", reported once when the pair is dropped |
| A scene keeps control | 60 s of game time in settled ordinary play | "a scene kept control of the pair", once |

### Exit review

- Drop, restore and cancel each report once and remove the pair from custody.
- Regroup moves are interrupted on drop, cancellation and combat.
- Retry is silent.
- No duplicate commands were found.

### Demonstrated repair (`9a371910`)

- **Defect:** the notice that a staged pair is held through an encounter was a log line behind a bool latch, cleared
  only by an evaluation that was not a hold. A custody dropped during combat (a load, a release) left the latch set.
  A later custody's encounter, held outside combat while its scene ran and then in combat, went unreported. The player
  was never told in any case, because it was only a log line.
- **Correction:** `MountedRetainedIntentPolicy.CombatHoldNotice` decides once per custody and encounter. A different
  custody, or an evaluation outside combat, starts afresh.
- Corrected by review R3: the first pass sent the notice as a routine persistence Report, which reaches no display.
  It is now delivered through the native warning notification (see the review section).
- **Regression:** `CombatHoldNoticeTests` runs the production notice: once per encounter, not per frame; a second
  encounter of the same custody; the dropped-custody sequence. The base latch is reproduced from `e5b92474` and
  labelled as such, only to show that the same sequence reported custody A alone. The PRODUCT-IL check "C03" pins the
  wiring and fails on the base build.

## Preserved Sprint 3 behaviour

- No Sprint 3 code path was edited. Sprint 4 touches the scene-placement detection in `RiderMovementAgent`, the
  combat-hold notice in `MountedRetainedIntent`, and adds patches and adapters.
- Exact command, view and agent ownership; binding generations; the arrival ownership exception; full obligations
  controlling retirement; permissive world transitions for stick cleanup; separate old-world cleanup; bounded retries;
  strict disable, shutdown and patch removal (the new patches are registered through `PatchExact` under the mod's Harmony ID, which the controller's `UnpatchAll` removes with the rest);
  R2 queue promotion; stationary rider actions: covered by the unchanged component suite (1349/0) and the focused
  neighbours below.
- S01, M04 and T04–T08 remain visible in the ledger. `Test-ChargeOwnershipContracts.ps1` still stops at T04 and was
  not part of the selection. No harness repair was needed.

## Checks run in the first pass (at `9a371910`; budget `known-issues-sprint4`, 0.37 of 90 minutes)

| Check | Result | Duration |
|---|---|---|
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint4` with the focused scripts below | TIER FAST PASS=10 FAIL=0 | 0.38 min |
| Components | 1349/0 (1338 at the base; +5 C04, +4 C02, +2 C03) | 2.5 s |
| `Validate-Source.ps1` | 151/0 | 5.7 s |
| Focused: Candlemere crossing, scene placement attribution, continuous drive, post-load view readiness, persistence, world ownership, physical mover pace | All PASS | 0.6–4.5 s each |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 | — |
| Clean Release build (build outputs removed first) | PASS; identity above | — |
| Base runs of the two new contract scripts against the frozen base DLL | 5/3 and 3/4, as expected | — |

- Logs are in `artifacts/known-issues-sprint4/`, numbered 01–07.
- Not run: Full tier, Candidate, historical native campaigns, release qualification, package, suite, purity and
  native checks, and mutation campaigns. Each fix has a matched base-fails/corrected-passes regression instead.

## Unresolved, with the missing evidence

| Item | Missing |
|---|---|
| C04 native | A mounted Candlemere crossing with the new log line; the owner's `output_log(9).txt` |
| C02 native | A scene placement within 3 m of a seated rider, logged as attributed and handed off |
| C01 | Unsupported. Any future design (for example the native split plus a separate deferred remount intent) needs its own investigation |
| C03 native | A staged pair entering combat: the warning on screen and in the log once, then the post-combat remount |
| R2 native | Two scene placements of a seated rider before the poll, logged as superseded, handed off once at the final endpoint |
| M01–M03, M05 native | Unchanged from Sprint 3 |
| M04 | Original report unresolved (unchanged) |
| S01, T04–T08 | Unchanged |

## Not authorized and not done

Merge, release, tag, version bump, permanent installation, loader replacement, reviewer request, launching the game.
Work stops at this review checkpoint.
