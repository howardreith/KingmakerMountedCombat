# Known issues remediation, Sprint 3 closeout — M04 destination-marker ownership (2026-11-17)

This closeout completes Sprint 3's remaining item, **M04** (an extra or stale green destination marker while mounted).
It is offline engineering work only: nothing was launched, staged or installed, and nothing here is native gameplay
evidence. Sprint 5 was not started.

## Identity

| Item | Value |
|---|---|
| Base | `14f1b474c9967eb4f6d02a1541403c741ca95dbb`, the accepted Sprint 4 head (product build `03685720`) |
| Branch and worktree | `codex/kmc-known-issues-sprint3-closeout`, `worktrees/known-issues-sprint3-closeout` |
| Draft PR target | `codex/kmc-known-issues-sprint4` (PR #48), so the PR shows only this closeout |
| Commits | `c64c695f`: the product correction, observation, tests and new contract. `bae082b0`: a contract pin follows the prefix's new shape (the product-build commit). Then the records commit carrying this file, which is the branch head |
| Product DLL (clean Release build of `bae082b0`) | SHA-256 `b0333fe5e75c284c985f8e77427001816da4cf799f47f937cbe9969f85db9ab7`, MVID `255b54bb-5762-47d5-90a0-14303f51d4b9`; frozen copy `artifacts/known-issues-sprint3-closeout/head-bae082b0/` |
| Base DLL (accepted Sprint 4 build) | SHA-256 `7ce8f88a99aaa74d142e6835590d3051a9a48ce984589090b9407eb01efd59c8`, MVID `efdc12b6-0098-4027-ad49-f8c8f33851bc`; frozen as `artifacts/known-issues-sprint3-closeout/base-14f1b474/` |
| Preserved | PRs #45–#48, their branches, the main checkout and every other worktree are untouched |

## Final disposition of M04

| Question | Answer |
|---|---|
| Marker defects established by source | Four. Sprint 3 found two: (1) the carried entrance showed the rider's marker beside the mount's; (2) the scene regroup walk asked for a marker. The closeout found two more: (3) the mount's door approach asked for a marker that a native door click never shows; (4) the native cannot-attack click approach left a mounted rider holding an order it can never walk, with its marker visible until something replaced the order. |
| Repaired | All four, offline: (1) and (2) by Sprint 3, (3) and (4) by this closeout (`c64c695f`). |
| Another defect found by the closeout | Yes: (3) and (4) above. |
| The original incident | **Not attributable.** Its log and context are unavailable, and none of the four paths can be tied to it. Before 0.2.5, (1) and (3) were reachable by ordinary play, and (4) requires a rider that cannot attack (for example under Serenity). |
| Product paths left unclassified | None. Every product-built marker-capable command and every native creator that can reach a mounted rider is classified below. |
| Native or manual verification only | Seeing each repaired path in play, the new anomaly line (or its absence) in a real session, and the owner's own marker report |
| Remaining known limitation (not a marker-ownership defect) | A designer `RunCommand` action that moves a seated rider outside any scene cannot complete while mounted. Its marker is the native marker of a live scripted move, in the same class as the C01 in-combat limit. The observer reports it as `RiderHeldWhileMountMoves`. |

**M04 is closed as "engineering investigation complete; all identified product marker defects repaired".** The
historical owner incident cannot be assigned to any one of the repaired paths, because its original context is
unavailable. What remains is native and manual verification only.

**Sprint 3 engineering is closed:**
- M01–M03 and M05 were already accepted.
- M04 now has an evidence-supported engineering conclusion.
- T04–T08 are separate stale-harness findings and not part of Sprint 3's closeout.
- S01 keeps its disposition.

## The native marker contract (installed Kingmaker 2.1.7b)

All rows are IL read and pinned in `Test-MarkerOwnershipContracts.ps1`. Whole-assembly scans are marked.

| Aspect | Contract |
|---|---|
| Existence | `ClickPointerManager.m_UnitMarks` holds one pointer object per unit. It is touched only by the manager's initializer, `AddPointer`, `Update` and the `UnitMarks` getter (whole-assembly scan). No manager method removes or clears an entry, so native never removes a pointer. The getter's only readers are the three local-map marks. |
| Placement | `AddPointer` creates the pointer if needed, shows it and moves it to the given world position. Its only caller is `UnitEntityView.OnMovementStarted` (0600184E) (whole-assembly scan). |
| What calls the placement | `OnMovementStarted` is called by `UnitMovementAgent.OnPathComplete` (a completed plan of the unit's own stock agent, including `FollowPrecomputedPath` in turn-based play) and by `ClickGroundHandler.ShowDestination`'s path callback (a queued or paused ground order) (whole-assembly scan). |
| When a placement happens | Only for a directly controllable unit, outside Cutscene mode, whose `Move` slot (or first queued) `UnitMoveTo` asks for a marker, or which holds a `UnitAreaTransition`. |
| Visibility | `ClickPointerManager.Update` shows a unit's pointer while that unit holds a live `UnitMoveTo` with `ShowTargetMarker` (the `Move` slot, or the first queued) or a live `UnitAreaTransition`. It is full opacity when the unit is selected (the product counts a mounted pair's mount as selected when its rider is) and half otherwise. Otherwise it is hidden. |
| The live command that causes visibility | That `UnitMoveTo` or `UnitAreaTransition` of the pointer's own unit |
| Whether that command placed the pointer | Not checked natively. The pointer is shown where the unit's last placement put it, which for a new command is an earlier command's place until its own plan completes. A plan that fails returns before placing. |
| Default | Every `UnitMoveTo` constructor sets `ShowTargetMarker = true`. |
| Teardown | `ClickPointerManager` belongs to the area's UI. `OnDisable` only unsubscribes. The table goes with the manager. |
| Local map | Shows a unit's last pointer position while its entry exists, whether visible or not. This is native for every party unit. |

## Native creators of marker-capable commands

This is the whole-assembly scan: every `newobj` of `UnitMoveTo`, a subclass, or `UnitAreaTransition`. All nineteen are
pinned.

| Native creator | Can it reach a mounted rider? | Disposition |
|---|---|---|
| `ClickGroundHandler.RunCommand` | Yes: the ground click | Routed. The rider's order is rewritten to the mount, and a selected mount's duplicate is suppressed. One marker, the mount's. |
| `AreaTransitionController.RunUnitTransitionCommand`, `EntityOvertipVM.RunUnitTransitionCommand` | Yes: an entrance or exit | Carried entrance. Sprint 3 repair (1): one marker, the mount's. |
| `ClickUnitHandler.OnClick` | Yes, when the rider cannot attack (`UnitCondition.CanNotAttack`), as a `UnitMoveTo` toward the clicked enemy | **Repaired by the closeout (4).** |
| `CommandMoveUnit.OnRun`, `CommandMoveParty.OnRun` | Yes: scene commands | Out of combat the pair is handed off to staging, and the free rider gets native markers (no placement in Cutscene mode). In combat (C01, unsupported) the scene's own marker lasts until the scene's `OnStop` interrupts the move. Native and bounded. |
| `RunCommand` designer action | Rarely: a designer moves a party member outside a scene | Remaining limitation (see the disposition). |
| `UnitFearController.TickOnUnit` | No | Panic makes a unit not directly controllable, which breaks the mounted invariant, so the pair is dismounted and the free rider flees natively. |
| `BlueprintAiCastSpell`, `BlueprintAiTouch`, `BlueprintAiSwitchWeapon` (`CreateCommand`) | Not in ordinary play | AI error fallbacks only. AI runs a controllable unit only with the game's `CompanionsAI` flag. Such a move carries an AI action, so it is not rerouted, and the observer would report it. |
| `UnitCombatState.ReturnToStartingPositionIfNeeded` | No | Its return position is set only for non-player AI commands. |
| `SummonedUnitsController.TickUnitFollowMaster` | No | Summons only. |
| `UnitRoamingController.TickOnUnit`, `ImpactsOnSpawn.MoveToPlayer`, `WolfTrigger.WolfGoTo1`, `Test.WaypointMovement.Update` | No | NPCs and tests. |
| `CampPositionsSelector.MoveParty`, `CampPlaceView.MoveToRandomBuildingPosition` | No | Pairs are suspended before camp's approach orders. `MoveParty` asks for no marker anyway. |

## Product marker-path inventory

Compiled product IL inventory, pinned. The play sites are every product-built marker-capable command outside the
`Diagnostics` namespace. The diagnostic sites are reached only by the runtime automation host, which exists only when
the game is launched with its command line. Fifteen diagnostic sites exist and are not play paths.

| Path | Command holder | Physical mover | Requested by | Marker requested | Pointer key | Ended or replaced by | Rider and mount both marker-visible? | Old pointer shown for a new command? | Correction or native behaviour |
|---|---|---|---|---|---|---|---|---|---|
| Ordinary routed ground click (also local map and gamepad point orders, via `RunCommand`) | Mount | Mount | Player | Yes (native) | Mount | Arrival, Stop, replacement, native interruption | No: the rider holds nothing, and a duplicate mount order is suppressed | Only during the native bounded transition, until the new plan completes | Native, one marker |
| Entrance and exit approach | Rider (member command) and mount (carrier) | Mount | Player | Yes (`UnitAreaTransition`) | Mount | The transition ending | No: the rider's placement is skipped while the mount executes its carrier | The rider's earlier pointer is retired when the carrier is admitted | Sprint 3 repair (1) |
| Gamepad stick movement | Mount (`UnitMoveContiniously`) | Mount | Player | No (not a `UnitMoveTo`) | — | Stick release | No | No | Native |
| Door click | Rider (door interaction), mount (`MountedDoorApproachMove`) | Mount | Player | **Now no** (was yes) | — | The door command | No | No | **Closeout repair (3)** |
| Trap, loot and other carried interactions | Rider (native interaction), mount (`MountedDoorApproachMove` via `MountedCarriedApproach`) | Mount | Player | No | — | The interaction | No | No | Native rule: an interaction is not marked |
| Attack approach (pair attack) | Mount (`UnitMoveTo`) | Mount | Pair operation | No | — | The attack command | No | No | As built |
| Spell approach | Rider (`UnitUseAbility`) | — | Player | No (not a `UnitMoveTo`) | — | The cast | No | No | Native rule |
| Cannot-attack enemy click | Was the rider (`UnitMoveTo`, never walkable); now the mount | Mount | Player | Yes (native), now on the mount | Mount | Arrival, Stop, replacement | No | No | **Closeout repair (4)** |
| Camp approach | Free units | Units | Native | No (`MoveParty`) | — | Native | No | No | Pairs suspended first |
| Scene staging | Free rider (scene command) | Rider | Scene | Native (no placement in Cutscene mode) | Rider | The scene's `OnStop` | No: the pair is handed off | Native | Native |
| Scene regroup walk | Mount | Mount | Pair restoration | No | — | Arrival or deadline | No | No | Sprint 3 repair (2) |
| Door occupancy recovery | Mount | Mount | Pair cleanup | No | — | Arrival | No | No | As built |
| World-map and local-area restoration | — (no move issued; native placement) | — | — | — | — | — | No | No | No marker-capable command |
| Retained rider command while the mount moves | Rider | Mount | Varies | Varies | Rider | Its own lifecycle | Possible | Possible | Classified by the observer as `RiderHeldWhileMountMoves`. Known sources: entrance (repaired), click approach (repaired), scene in combat (C01, native, bounded), designer `RunCommand` (limitation). |

## The repairs

### (3) Door approach marker

- **Defect:** `MountedDoorInteractionCommand.BeginDelegatedMove` built the mount's approach with
  `ShowTargetMarker = true`. A native door click (`UnitInteractWithObject`) is never marked, and every other delegated
  approach asks for none, so a mounted door click showed a green marker an unmounted one never does. This is
  source-established, with no recorded rationale in the history (`c51fff3e`).
- **Correction:** `ShowTargetMarker = MountedDestinationMarkerPolicy.DelegatedInteractionApproachShowsMarker`
  (`false`). The approach itself, its `CreatedByPlayer` and the door lifecycle are unchanged.

### (4) Cannot-attack click approach

- **Defect:** native `ClickUnitHandler.OnClick` gives each selected unit with `CanNotAttack` a `UnitMoveTo` toward the
  clicked enemy, its one `UnitMoveTo`, outside the ground-click routing.
  - A mounted rider's stock agent is off, so the rider cannot walk it, yet that agent plans it.
  - Native therefore placed and showed the rider's marker, and the order lived until something replaced it.
  - The pair's later routed orders go to the mount, so the rider's marker stayed visible under them.
- **Correction:**
  - A frame-bounded scope marks the native unit click: opened by its prefix when native proceeds, closed by its
    registered postfix.
  - Inside it, `UnitCommandRunPrefix` (after the scene check, before the pair's command gates) asks
    `RerouteRiderClickApproach`.
  - `MountedDestinationMarkerPolicy.RoutesRiderClickApproach` decides: mounted pair, the rider's own commands, an exact
    `UnitMoveTo`, no AI action, inside the click, not simulated.
  - The order then goes through the existing ground-order lifecycle: `GroundCommandPrefix` (admission and routing), a
    player-created `UnitMoveTo` with the native move's approach radius and marker request on the routed unit, then
    `GroundCommandPostfix` in a `finally`.
  - The rider's copy is refused.
  - If admission refuses, nothing runs and native feedback stands. If routing keeps the order on the rider, native
    runs it unchanged.

### What was not done

- No global clear and no hiding of a live marker.
- No completion of a blocked order.
- No other unit's pointer touched, no pointer repositioned, and no retry.
- The accepted entrance skip and retirement, and the regroup suppression, are unchanged.

## Ownership classification and bounded observation

- **`MountedMarkerOwnershipPolicy`** (Domain) classifies one unit's pointer from these facts: entry, visible, live
  qualifying command, unordered, placed for that command, seconds seen, moving. The classes are:
  - **Native:** `NoPointer`, `HiddenPointer` (never stale), `ExpectedPhysicalMover`, `ExpectedUnfinishedMove` (a live
    blocked or short-stopping move, kept), and `PlacementPending` (inside the 3 s bound).
  - **Anomalies:** `DuplicatePairMarkers`, `VisibleWithoutQualifyingCommand`, `PlacedForEarlierCommand` (past the
    bound), `RiderHeldWhileMountMoves`, and `InternalMoveRequestsMarker`.
- **Pair rule:** a mounted pair's rider never shows a marker of its own; otherwise the pair's state is the mount's.
  Unmounted units are each their own mover.
- **Placement record:** a postfix on `OnMovementStarted` records, with a `WeakReference`, the command each placement
  that ran was made for. The prefix reports through `__state` whether it skipped the placement; skipped and preview
  placements are not recorded.
- **The observer line:** `MountedExplorationObserver` evaluates each mounted pair at its existing 4 Hz, in Default mode
  (exploration and real-time combat), and writes a line only on an anomaly transition. It writes once per pair,
  generation, class and the identities of the commands keeping the pointers visible. The line holds:
  - the rider's and mount's entries (entry, destroyed, visible, position);
  - the commands that keep them visible (type, identity, executor, target, finished, created by player, marker,
    queued, cutscene, AI);
  - whether each was placed for that command, and the placement frame;
  - the classes, the generation, area, mode, combat, selection and mount movement, and a disposition text (repaired
    path, native, or unresolved).
- **Containment:**
  - Each pair's observation is in its own catch, and so is its log line.
  - The reroute's call site and the placement postfix are in catches, reporting through one bounded fault report.
  - A reader that is unavailable, a destroyed pointer object or a table that changed are read safely. A destroyed
    pointer is reported as `destroyed=True`, not as visible.
- **Cleanup:**
  - Marker state is cleared at every mode or load boundary.
  - Entries of pairs no longer mounted are dropped each sample.
  - `Reset` (called by the composition root when disabled and when disposed) clears the observer, the placement table
    and the click scope.
  - Placements are weak, so nothing keeps a unit or command alive across a world replacement.
- **Not a repair:** this logging closes nothing by itself. It supports the classification above, and gives the next
  real session the missing observation.

## Validation (budget `known-issues-sprint3-closeout`)

| Check | Result |
|---|---|
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint3-closeout`, focused: marker ownership, physical mover pace, continuous drive, post-load view readiness, persistence, world ownership, scene placement, Candlemere | First run: FAIL at `Test-PhysicalMoverPaceContracts.ps1` (log `04-fast-tier.log`, preserved), because its pin looked up the placement prefix by its old shape. After `bae082b0`: TIER FAST PASS=11 FAIL=0, 0.74 min; budget spent 1.73 of 90 minutes over 2 entries |
| Components | 1375/0 (1370 at the base; +5 closeout tests) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MarkerOwnershipContracts.ps1` (new, 13 checks) | 13/0. Accepted Sprint 4 build: 7/6. The native checks pass, and the product inventory names the door site asking for a marker. The six failures are routing, placement record, cleanup, containment and the compiled decisions (log `02-marker-contracts-base-14f1b474-final.log`). An earlier base run, `01-`, scored 7/5 because it predates the containment check |
| `Test-PhysicalMoverPaceContracts.ps1` | 27/0 on both builds after the pin update (it accepts either prefix shape and asserts that an unskipped placement reports no skip) |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 |
| Clean Release build of `bae082b0` (outputs removed first) | PASS; identity above |
| Closure-critical mutations (each restored), log `03-marker-mutations.log` | X1 duplicate pair marker accepted. X2 legitimate blocked marker treated as stale. X3 old pointer accepted under a replacement command (no bound). X4 the click routing touches a unit that is not the rider. X5 a delegated interaction approach requests a marker. X6 a throwing logger or failing pair stops the rest. Each is caught: X1–X5 by a component test and the contract, X6 by the contract's containment check |

- Logs are in `artifacts/known-issues-sprint3-closeout/`, numbered 01–08.
- Not run: Full, Candidate, historical campaigns, release qualification.

## Evidence kinds

| Kind | What |
|---|---|
| Production code executed offline | `MountedDestinationMarkerPolicy` (routing decision, constants), `MountedMarkerOwnershipPolicy`, `CommandRouter`, `MountedPlayerMoveWatch` and the Sprint 3 entrance policies (component tests); the compiled decisions (contract) |
| Installed native methods executed detached | None in this closeout |
| Installed IL inspected | The pointer manager, placement and visibility, the nineteen creators, the unit click, the scene stop, and the controllability rule (whole-assembly scans where stated) |
| Represented pointer and command behaviour | The marker test model (one pointer per unit, the native visibility rule, plan completion) |
| Modelled orchestration | The click, door and routing sequences in the model |
| Native gameplay | None; not run |

## Remaining native evidence limits

- None of the four repaired paths has been seen in play. Specifically:
  - a mounted exit click after the rider walked on foot;
  - a scene leaving the mount behind;
  - a mounted door click;
  - a cannot-attack enemy click while mounted.
- The anomaly line has not been written by a real session.
- The owner's original incident remains unattributed.
- The designer `RunCommand` limitation is unexercised.
