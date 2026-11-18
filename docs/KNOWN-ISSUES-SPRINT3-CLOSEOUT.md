# Known issues remediation, Sprint 3 closeout — M04 destination-marker ownership (2026-11-17)

This closeout completes Sprint 3's remaining item, **M04** (an extra or stale green destination marker while mounted).
It is offline engineering work only: nothing was launched, staged or installed, and nothing here is native gameplay
evidence. Sprint 5 was not started.

**Review of `b1a88b4e` (R1–R3), same day.** The owner's review found three remaining defects in repair (4) and the
placement record. All three are corrected (`cdfbe509`); see "Review of b1a88b4e" below. Three claims of the first
closeout version are withdrawn there: that a frame-scoped click flag proved which command was the native cannot-attack
approach, that the replacement order was native-equivalent, and that a visible pointer after `OnMovementStarted` proved
a placement.

## Identity

| Item | Value |
|---|---|
| Base | `14f1b474c9967eb4f6d02a1541403c741ca95dbb`, the accepted Sprint 4 head (product build `03685720`) |
| Branch and worktree | `codex/kmc-known-issues-sprint3-closeout`, `worktrees/known-issues-sprint3-closeout` |
| Draft PR target | `codex/kmc-known-issues-sprint4` (PR #48), so the PR shows only this closeout |
| Commits | `c64c695f`: the product correction, observation, tests and new contract. `bae082b0`: a contract pin follows the prefix's new shape. `b1a88b4e`: the first records commit (reviewed head). `cdfbe509`: review of `b1a88b4e`, R1–R3 (the product-build commit). `826f5120`: the contract pins each registration's exact hooks. Then the records commit carrying this version, which is the branch head |
| Product DLL (clean Release build of `cdfbe509`) | SHA-256 `f584c1c6e0678a9447a6082552dda57c5fcc4a95e475430c20fb5a93210f5035`, MVID `6cb7b950-e97c-4c14-9d83-764b074a3439`; frozen copy `artifacts/known-issues-sprint3-closeout/head-cdfbe509/` |
| Reviewed DLL (`b1a88b4e`, product build `bae082b0`) | SHA-256 `b0333fe5e75c284c985f8e77427001816da4cf799f47f937cbe9969f85db9ab7`, MVID `255b54bb-5762-47d5-90a0-14303f51d4b9`; frozen copy `artifacts/known-issues-sprint3-closeout/head-bae082b0/` |
| Base DLL (accepted Sprint 4 build) | SHA-256 `7ce8f88a99aaa74d142e6835590d3051a9a48ce984589090b9407eb01efd59c8`, MVID `efdc12b6-0098-4027-ad49-f8c8f33851bc`; frozen as `artifacts/known-issues-sprint3-closeout/base-14f1b474/` |
| Preserved | PRs #45–#48, their branches, the main checkout and every other worktree are untouched |

## Final disposition of M04

| Question | Answer |
|---|---|
| Marker defects established by source | Four. Sprint 3 found two: (1) the carried entrance showed the rider's marker beside the mount's; (2) the scene regroup walk asked for a marker. The closeout found two more: (3) the mount's door approach asked for a marker that a native door click never shows; (4) the native cannot-attack click approach left a mounted rider holding an order it can never walk, with its marker visible until something replaced the order. |
| Repaired | All four, offline: (1) and (2) by Sprint 3, (3) and (4) by this closeout (`c64c695f`; (4) corrected after review by `cdfbe509`: call-site authority, the original native command). |
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
`Diagnostics` namespace: six since the review, because the click-approach routing builds no command (it runs the native
one). The diagnostic sites are reached only by the runtime automation host, which exists only when
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
| Cannot-attack enemy click | Was the rider (`UnitMoveTo`, never walkable); now the mount, which runs the original native command | Mount | Player | Yes (native) | Mount | Arrival, Stop, replacement | No | No | **Closeout repair (4)**, authority bound to the one native call site |
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
- **Correction (as corrected after review, `cdfbe509`):**
  - A transpiler replaces exactly one instruction of the installed `OnClick`: the `UnitCommands.Run` whose argument is
    the local the `UnitMoveTo` construction is stored in. It calls `ClickApproachRun` with that call's own container and
    command. Every other `Run`, including the loot and interact `Run`s, and every other instruction are unchanged.
  - `MountedDestinationMarkerPolicy.RoutesRiderClickApproach` decides: a mounted pair, the rider's own commands, an
    exact `UnitMoveTo`, no AI action, at that call site, not simulated, and no scene active or holding the rider.
  - The hostile-click observation is bound to the command, as the rider's own `Run` would have bound it.
  - The ground-order lifecycle runs: `GroundCommandPrefix` (admission and routing), then the open order's identity of
    this command, then the mount's `Run` of **the original command object**, then `GroundCommandPostfix` in a
    `finally`.
  - The rider's `Run` of it never happens.
  - If admission refuses, nothing runs and native feedback stands. If routing keeps the order on the rider, native
    runs it unchanged. A fault falls back to native `Run` only while the command has not been given to another unit.

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
- **Placement record (as corrected after review):** a postfix on native `ClickPointerManager.AddPointer`
  (06002D62) records the command that qualifies at that placement in `MountedPointerPlacementLedger` (a
  `ConditionalWeakTable` keyed by unit, holding a `WeakReference`). A placement native declines (`OnMovementStarted`
  without `AddPointer`) records nothing, so a pointer still showing where an earlier command put it is
  `PlacedForEarlierCommand` once the bound passes. A preview goes to `AddPreviewPointer` and is never recorded.
- **Unordered moves:** a qualifying command counts as unordered only if it is not a pair player order
  (`IsPairPlayerOrder`), so the routed native click approach, which native never marks `CreatedByPlayer`, is not
  reported as `InternalMoveRequestsMarker`.
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

### After the review of `b1a88b4e` (the current state)

| Check | Result |
|---|---|
| Clean Release build of `cdfbe509` (outputs removed first) | PASS; identity above (log `12-clean-release-build-review.log`) |
| Components | 1385/0 (1375 at `b1a88b4e`; +10 review regressions in `MountedClickApproachAuthorityTests`) |
| `Validate-Source.ps1` | 151/0 |
| `Test-MarkerOwnershipContracts.ps1` (18 checks) | 18/0 (log `13-marker-contracts-review.log`). Reviewed build `b1a88b4e`: 10/8, and accepted Sprint 4 build: 10/8, each failing every product check written for R1–R3 (logs `13b-`, `13c-`; with the contract before its hook pin, `09-`, `10-`) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint3-closeout`, focused: marker ownership, physical mover pace, continuous drive, command slot, native input semantics, post-load view readiness, persistence, world ownership, scene placement, Candlemere | PASS 13/0 (0.59 min) (log `14-fast-tier-review.log`); budget spent 2.32 of 90 minutes over 3 entries |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 815/0 (log `15-assembly-contracts-review.log`) |
| Review mutations, each in a disposable detached worktree with its own binaries, the contract run on each mutant DLL (logs `11b-`, `11c-`) | The first run's contract never executed (it resolved its environment from the disposable worktree), so its CAUGHT marks for X7–X13 were vacuous; that log is kept as `11-review-mutations-INVALID-contract-not-run.log`. In the valid run X12 (the record postfix also registered on `OnMovementStarted`) survived, because the contract only required the expected hook names; the contract now pins each registration's complete hook list (`826f5120`) and the same mutant binary (MVID `bf084d67…`) is caught (`11c-`). Mutations: X7 authority outside the call site. X8 the transpiler replacing another `Run`. X9 the order's identity not cleared. X10 `GroundCommandPostfix` outside a `finally`. X11 a `CreatedByPlayer` copy instead of the original. X12 and X12b the record taken after `OnMovementStarted` or inferred from a visible pointer. X13 identity not recognised. X14 the policy ignoring the call site. X15 the policy ignoring scene ownership. X16 the ledger keeping the first command. Each is caught; see the log for which test or contract part |

### The first closeout version (`b1a88b4e`, preserved)

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
| Production code executed offline | `MountedDestinationMarkerPolicy` (routing decision, constants), `MountedMarkerOwnershipPolicy`, `MountedPointerPlacementLedger`, `CommandRouter`, `MountedPlayerMoveWatch`, `ObservedPlayerAttackRequest` and the Sprint 3 entrance policies (component tests); the compiled decisions and ledger, and the product's cannot-attack transpiler run against the installed `OnClick` IL (contract) |
| Installed native methods executed detached | The installed `UnitMoveTo(Vector3, float)` constructor (the state it leaves is the complete configured state of the cannot-attack command) |
| Installed IL inspected | The pointer manager, placement (`AddPointer` and `AddPreviewPointer`) and visibility, the nineteen creators, the unit click and the complete setup of its cannot-attack command up to its `Run`, `UnitCommands.Run`, the scene stop, and the controllability rule (whole-assembly scans where stated); the existing native-input contract pins that native reads `CreatedByPlayer` only for movement acceleration |
| Represented pointer and command behaviour | The marker test model (one pointer per unit, the native visibility rule, plan completion) |
| Modelled orchestration | The click, door and routing sequences in the model; the call-site and frame-scope authorities, nested and throwing clicks, two pairs, the ground-order lifecycle, and `OnMovementStarted` with and without `AddPointer` (`MountedClickApproachAuthorityTests`) |
| Native gameplay | None; not run |

## Review of b1a88b4e: R1–R3

### Withdrawn claims of the first version

- **A frame-scoped click flag proves exact command ownership.** It does not. While it was open, any eligible
  `UnitMoveTo` run on a mounted rider qualified, including:
  - an unrelated synchronous move from a click subscriber or another patch;
  - a move issued after an inner click closed the flag under an outer one;
  - a move issued after a throwing click left the flag open for the rest of its frame;
  - another pair's order in the same click stack.
- **The replacement order is native-equivalent.** It was not established. It copied six values and forced
  `CreatedByPlayer` true, which native never sets on this command. Native reads `CreatedByPlayer` only for movement
  acceleration, so the copy was accelerated out of combat where the native order is not. The hostile-click observation
  also stayed on the original, which never ran.
- **`OnMovementStarted` plus a visible pointer proves `AddPointer` ran.** It does not. Native can decline the
  placement while an older pointer stays visible, so the old record attributed that pointer to the new command and hid
  `PlacedForEarlierCommand`.

### R1 — the command-ownership mechanism

The authority is the exact native call site; nothing is stored.

- **Native call site:** the transpiler replaces only the `Run` of the click command's local in the installed `OnClick`.
  Running it against the installed IL changes exactly one of 343 instructions, index 250.
- **Exact command:** that call's own argument. In `OnClick` the local holds a `UnitMoveTo` only from its one
  construction; its other sources are the cast and attack factories, which build only `UnitUseAbility` and `UnitAttack`.
- **Pair owner and generation:** the owner of the call's own container. That container must be the mounted rider's.
  The generation is captured before admission and must be unchanged, with the pair still mounted, before the mount runs.
- **Exact rider command container:** reference equality with the rider's own `Commands`.
- **One-time consumption:** the hook runs once per native call and acts on its own argument only.
- **Nested clicks:** each invocation of `OnClick` has its own call and argument, so an inner click cannot reach an
  outer one's.
- **Exception safety and boundaries:** nothing is held, so nothing survives a throw, a world replacement or a pair
  boundary. The ground order's identity of the command lives only between `GroundCommandPrefix` and
  `GroundCommandPostfix` (in a `finally`), and the controller's boundary also clears it.

Before and after (modelled orchestration with the production decisions; `MountedClickApproachAuthorityTests`):

| Case | `b1a88b4e` (frame scope) | Corrected (call site) |
|---|---|---|
| Exact command C | Routed, but as a copy | C itself routed once, initialised once on the mount; the rider's `Run` never happens; one marker |
| Unrelated synchronous U before C | U rerouted | U native on the rider; C routed once |
| Nested clicks | The inner click closed the scope; the outer C1 stayed on the rider with its marker | C1 and C2 each routed to their own pair |
| Throwing click | A later rider move in the frame was rerouted | Nothing routed; the thrown click's command never became live |
| Another pair, unrelated selected unit | The other pair's order was rerouted | Untouched; the companion's own approach is native |
| Controls (simulated, native gate before the branch, AI, subclass, unmounted, scene, ground/entrance/door/trap/loot/scene paths) | Native | Native |

### R2 — the native command setup, and reuse

- **Installed IL, from construction to `Run`:**
  - `new UnitMoveTo(targetUnit.EntityData.Position, targetUnit.Corpulence * 1.1)` is stored in the click command's
    local.
  - Nothing reads or configures that local until `UnitCommands.Run(local)`: no field, property or call.
  - In between, `OnAttackRequested(unit, targetUnit)` is raised from a closure that holds no command.
  - `ManualTarget` is set after the `Run`; the transpiler leaves it unchanged.
  - Native marks the loot and interact commands it runs elsewhere in `OnClick` `CreatedByPlayer`, never this one.
- **Installed constructor, run detached:** the configured state is complete:
  - a point target, `ApproachRadius` = corpulence × 1.1, `MaxApproachRadius` 1000000, cooldown ignored;
  - `ShowTargetMarker` true, `CreatedByPlayer` false, `Type` Move;
  - every other field at its default: `Orientation`, `MovementDelay`, `SpeedLimit`, `MovementType` Normal, `Roaming`,
    `AiAction`, `Executor`, `ForcedPath`, `DontInterruptInTurnBasedMode`.
- **Disposition: the original object is reused, and no copier exists.** The proofs:
  - Nothing initialises or attaches it before the hook, because the local is untouched until the `Run`.
  - `UnitCommands.Run` initialises it once: `Executor` is null, so it calls `Init` with the mount. The rider's `Run`
    never happens, and the fault fallback runs native `Run` only while `Executor` is null or is the rider.
  - The mount's `Run` cannot re-enter the routing, because only the `OnClick` call site reaches it and the command-run
    prefix does not.
  - Admission and completion stay balanced, because `GroundCommandPostfix` is in a `finally`.
  - The watched ground move, the turn-based admission and the path-end bridge are this command, recognised by
    identity, because native leaves its `CreatedByPlayer` false.
- **Product readers of `CreatedByPlayer`:** each one that can see the mount's move asks `IsPairPlayerOrder`
  (`CreatedByPlayer`, the open order's identity, or the watched explicit move). That covers the watch binding,
  turn-based admission, the path-end bridge, door admission, container-order travel, the carried-request player test
  and the marker observer.
  - The watch was bound only for `CreatedByPlayer` moves before, so no other command changes.
  - Group-pace classification keeps `CreatedByPlayer`, because native accelerates no cannot-attack approach.
- **No action, budget or preparation is touched:** the mount's native `Run` spends what native spends, and the routing
  makes no cooldown, preparation or refund call.

### R3 — the placement-record seam

- **Seam:** a postfix on the exact `ClickPointerManager.AddPointer(Vector3 clickEventWorldPosition, UnitEntityData
  unit)` (06002D62). Its only caller is `OnMovementStarted`, which uses `AddPreviewPointer` for a preview.
- **What it records:** the unit, the command that qualifies at that placement (weakly), and the frame.
- **Before and after:**

| Case | `b1a88b4e` | Corrected |
|---|---|---|
| `AddPointer` runs for B | Recorded B | Recorded B |
| Native declines `AddPointer`; A's pointer stays visible for B | Recorded B; B's pointer counted as its own | Record stays A; `PlacementPending` inside 3 s, then `PlacedForEarlierCommand` |
| Preview | Not recorded | Not recorded |
| B placed at A's exact coordinates | Recorded B | Recorded B |
| Missing pointer, cleanup boundary, unit or command no longer referenced | Not recorded; reset clears; nothing kept alive | The same (production ledger, collected after GC) |

## Remaining native evidence limits

- None of the four repaired paths has been seen in play. Specifically:
  - a mounted exit click after the rider walked on foot;
  - a scene leaving the mount behind;
  - a mounted door click;
  - a cannot-attack enemy click while mounted.
- The anomaly line has not been written by a real session.
- The call-site transpiler has been run against the installed IL offline only, not applied in a running game.
- No native session has shown the original command walking on the mount, or the turn-based admission or path-end
  bridge accepting it.
- The owner's original incident remains unattributed.
- The designer `RunCommand` limitation is unexercised.
