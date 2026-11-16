# Known issues remediation, Sprint 3 — mounted pace and the marker report (2026-11-16)

Sprint 3 of the owner's Mounted Combat Known Issues Remediation Epic: **M01** (entrance acceleration), **M02**
(gamepad acceleration), **M03** (group pace inputs) and **M04** (the extra or stale green marker). The current state
of every issue is in [CURRENT-KNOWN-ISSUES.md](CURRENT-KNOWN-ISSUES.md).

## Identity

- **Branch:** `codex/kmc-known-issues-sprint3`, draft PR, no reviewers requested.
- **Base:** `1dffc3e09ffeddc70ba09e3a1f46c1613a997bd7`, the reviewed head of Sprint 2 (draft PR #46, open and
  unmerged when this sprint started and when it was pushed). The branch was created from that commit in its own
  worktree and its PR targets `codex/kmc-known-issues-sprint2`, so the PR shows only the Sprint 3 change. PR #46
  (`1dffc3e0`), PR #45 (`b62c59a8`) and `origin/main` (`7a5cd2c3`) were as the owner's prompt states and were not
  touched.
- **Commits:** `4ff3e4b0` (intake: the S02 size-limit fallback in the ledger), `c5b52387` (M01–M03, code and tests),
  `26b4169e` (M04, code and tests), then the records commit that adds this file.
- **Product:** gameplay code changed (three native read sites and one native placement are patched; one move lost
  its marker request). A build of this branch is a new binary. `Info.json` is unchanged (0.2.11).
- **Artifact:** `bin/Release/KingmakerMountedCombat.dll` built from `26b4169e`: SHA-256
  `E957D4EACE3BCEA4CCBE91490735AD8A6427B42EF1E250CF5BDA92C4A2AA79FF`, MVID `6bacf01f-2aac-4927-b478-48c717573f28`.
  The records commit changes no source. The reviewed-base build used for the before results is the Sprint 2
  build, MVID `58798fc0-cfe2-4c70-9b1c-d5d170bdc6ce`.
- **Save format:** unchanged. Nothing new is saved.
- **Native:** nothing was launched, staged, installed or run in the game. This machine has no KMC installation in
  the game's `Mods` folder and no qualified disposable fixture (`runtime-state` holds no fixture qualification),
  so the focused native check the prompt allows had no prerequisites.

## Dispositions

| ID | Disposition | In one line |
|---|---|---|
| M01 | **Implemented and verified offline** | The mount's carried entrance command now receives the same native `Accelerate()` offer its selected rider's order receives. |
| M02 | **Implemented and verified offline for a following pair.** The stick-driven pair is a separate, larger defect: **M05, open correction.** | The gamepad's follower pass now reaches the mount that executes a following rider's order. A mounted rider as the stick-driven unit gets no movement at all; that is not an acceleration gap and is not repaired here. |
| M03 | **Implemented and verified offline** | The native group speed limit is computed from each pair's mount instead of its rider. The native algorithm, floor and selection are untouched. |
| M04 | **Investigation complete, issue unresolved.** Two adjacent marker defects found by the investigation are implemented and verified offline. | No cause of the owner's report is established; its original log is not available. |
| M05 (new) | **Open correction** | Found while reconciling M02: stick input for a mounted rider is refused by native `Run`, and the source says the pair is then dismounted. Source-established, never observed. |

Nothing here is native-verified.

## Reconciliation against the current source

The ledger rows were planning findings. Read against the source at `1dffc3e0` and the installed Kingmaker 2.1.7b
assembly (MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`):

- **M01 holds.** The entrance carry (`MountedCombatController.Entrance.cs`) gives the mount its own
  `UnitAreaTransition`. The native offer walks the commands of selected units, and the mount is not selected.
- **M02 holds for a following pair only.** The planning finding assumed the stick's continuous movement already
  reaches the mount. It does not (see M05). What the finding described exactly is the follower pass.
- **M03 holds.** The handoff's floor is confirmed: `30.Feet().Meters / 3f`, 3.048 m/s.
- **M04:** the earlier finding "one routed click yields exactly one mount-keyed marker" holds. Two other paths
  were found (below).

## M01 — entrance and exit orders

**Native offer boundary.**

- `AreaTransitionController.StartAreaTransition` (and the gamepad's own copy in `InGameInputLayer`) calls
  `SelectAll`, then `ClickGroundHandler.MoveSelectedUnitsToPoint` with a runner that gives each selected unit a
  `UnitAreaTransition` with `CreatedByPlayer = true` and the group speed limit.
- `UnitMovementAgent.OnPathComplete` calls `UnitCommand.AccelerateIfPathIsLong` (`060027B4`) on every command of
  the unit whose new path completed, whatever its type. At 12.7 m or more that command accelerates and then offers
  `Accelerate()` to every command in the slots of every selected unit.
- The offer is therefore the same call as for ground orders, and it runs only inside that native method. The
  existing postfix (`NativeGroupAccelerationPostfix`) fires exactly when the trigger newly accelerated.

**Command ownership.** `MountedGroupPacePolicy.ClassifyOrder` names what the command in the mount's Move slot is
to the pair. It is the rider's order in two cases only:

| Order | Identity |
|---|---|
| Routed ground order (shipped) | Exact `UnitMoveTo`, player-made, executed by the mount, and the command the ground click bound (`MountedPlayerMoveWatch`), now also for the pair generation it was bound for. |
| Carried entrance order (new) | Exact `UnitAreaTransition`, player-made, executed by the mount, the command the entrance carry issued (`MountedEntranceCarrierWatch`: command, group and pair generation), and the rider still holds its own live member command of that same group. |

Everything else is `None`: attack, Charge, spell, door, trap and recovery approaches, AI orders, a replaced or
foreign command, and an entrance command the mount holds for any other reason.

**Change.**

- `CarryNativeGroupAcceleration` classifies the mount's Move-slot command and, as before, calls native
  `Accelerate()` once on it. The decision (`MountedGroupPacePolicy.Decide`) is unchanged: the native offer ran, the
  pair is mounted, the rider is selected, the mount is not, real time only, the order is unfinished and not yet
  accelerated.
- The carrier is issued exactly as before: one command, one `Run`, in the postfix of the rider's own `Run`. It is
  bound for the offer only after that `Run`, and nothing at creation copies the rider's accelerated state.
- The controller's update lets the binding go when the carrier ends or the pair is no longer the one it was issued
  for, so no command or group of an earlier approach is kept across an area load.
- One log line per order, as before, now with `order=RoutedGroundOrder` or `order=CarriedEntranceOrder`.

**Preserved.** Group membership, arrival counting, the single group dispatch, completion and interruption are the
native command's. No speed, limit, gait, agent field or stat is written. Turn-based play is excluded by the policy
and by the native in-combat refusal.

**A fact the carry relies on less than it seems.** The mount's own long path already accelerates its member
command natively, and so does the rider's own plan (the rider's switched-off stock agent still plans the rider's
command; see M04). The carry adds the case the shipped pace repair added for ground orders: another member's long
path is the trigger.

## M02 — gamepad acceleration for a following pair

**Native contract, read separately from the mouse path.** `InGameInputLayer.UpdateMovement` (`06005D0C`), real
time, `Default` mode, out of combat:

1. The driven unit is `UnitSelectionManager.Instance.CurrentSelectUnit` (the console selection, not the mouse
   `SelectionManager`).
2. If it holds no continuous command, one `UnitMoveContiniously` is created, initialised for it and run on its own
   command slots.
3. Every frame: stick magnitude 0.95 or more calls `Accelerate()` on that continuous command, less calls
   `Deaccelerate()`. This is not the long-path trigger.
4. At each repath (0.1 to 0.5 s): `MakeCompanionsFollow` re-orders the other selected units through
   `MoveSelectedUnitsToPoint`, and then **every selected unit's `Commands.Move` receives `Accelerate()` or
   `Deaccelerate()`** according to the driver's command.

**Offer boundary and ownership.** Step 4 is the offer a following pair misses: its rider is in the selection, its
order runs on the mount, and `rider.Commands.Move` is null. A transpiler answers the one `SelectedUnits` read of
that loop with the list to walk (`GamepadFollowerOfferUnits`). A selected rider is replaced by its mount only when
`MountedGroupPacePolicy.DecideGamepadFollower` says so: mounted pair, real time, the mount not itself selected,
and the mount's Move slot holds the **routed ground order** of that rider, unfinished. Native then makes both
calls itself on the mount's command.

**Retirement.** The hook keeps no state. Release (native runs no pass), replacement (the old command is out of the
slot), selection change (the rider is not in the list), dismount, a new pair generation and turn-based combat each
leave the list native. Two pairs answer for their own riders only. Nothing is logged: the pass repeats several
times a second.

## M05 (new) — the stick-driven mounted pair gets no movement

Found while reading the gamepad contract. Not repaired; recorded so M02 is not read as gamepad support.

- **What the source says.** With a mounted rider as the driven unit:
  1. The mod's prefix on `UnitMoveContiniously.Init` rewrites the executor to the mount
     (`RouteContinuousMove`), so native `Init` sets the **mount's** `AgentOverride` to the continuous agent.
  2. Native then runs the command on the **rider's** command slots. `UnitCommands.Run` refuses a command whose
     executor is another unit ("Invalid command, can't run"). No continuous command runs.
  3. The next statement dereferences `unit.Commands.MoveContiniously`, which is null.
  4. The mounted invariant treats a mount `AgentOverride` as "the authoritative mount movement agent changed" and
     dismounts the pair. Nothing clears that override afterwards, and a mount with an override is refused for
     mounting.
- **Also:** the mod projects the mount onto the rider in the mouse `SelectionManager` only. The console
  `UnitSelectionManager` is not patched.
- **Status of this finding:** source reading of the installed assembly and the product. It has never been observed;
  no gamepad session is on record in this project.
- **Why it is not repaired here.** Making stick input drive the mount means the mount moves through the native
  continuous agent, which the pair's movement synchronisation does not know (the product reads the mount's stock
  agent in 39 places), and it means relaxing the mounted invariant. That is a movement mode, not an offer to carry, and it
  cannot be checked offline.
- **Decision for the owner:** whether gamepad play of a mounted pair is in scope. If it is, the first step is a
  native observation of steps 1 to 4 on a current build.

## M03 — group pace from the physical movers

**Native algorithm (unchanged).** `MoveSelectedUnitsToPoint` (`060093DB`): for more than one selected unit,
`limit = max(min over selected units of ModifiedSpeedMps, 3.048)`; for one unit, no limit. The limit goes to every
order of that click, including entrance orders and the gamepad's follow orders.

**Change.** A transpiler replaces the one `Enumerable.Select<UnitEntityData, float>` of that expression with
`GroupPaceSpeedInputs`, a hook of the same shape. It lists each selected unit's physical mover once
(`MountedPaceActors.Project`): a mounted pair's rider is answered by its mount
(`MountedGroupPacePolicy.SpeedInputIsMount`, real time only), everything else by itself. The **native selector**
then reads each speed, and `Min`, the floor, the `Count > 1` gate and every later use of the selection are native.
The selection collection is never changed.

**What it does and does not change.**

| Selection | Before | Now |
|---|---|---|
| Fast rider (40 ft) on a slower horse (35 ft), 40 ft companions | Limit 40 ft; the horse falls behind | Limit 35 ft; the party keeps the horse's pace |
| Slow rider (20 ft) on a fast horse (50 ft), 40 ft companions | Limit 30 ft (the rider's 20, floored) | Limit 40 ft |
| Rider only, or mount only | No limit (one unit) | Same |
| Rider and mount both selected | min(rider, mount), floored | The mount once, floored |
| Two pairs and a companion | Slowest of riders and companion, floored | Slowest of the two mounts and the companion, floored |
| Unmounted party; turn-based combat | Native | Native, same inputs |
| Mount slowed below 30 ft | Limit 30 ft (floor) | Limit 30 ft (floor): the party does not wait below the native floor |

The last row is the native rule, not a gap: a mover slower than 30 ft is bounded by its own speed and the group
walks at the floor, exactly as for an unmounted slow unit.

## M04 — the extra or stale green marker

**Evidence available.** The original log of the report is not on this machine; the report predates the
observation lines added in 0.2.3. Three later owner logs exist (0.2.3, 0.2.4, 0.2.5) and none is the incident:

- 119 mount pace summaries, **no** "mount stopped short of its marked destination" line and no `stalled=True`.
- One "rider-held move command while mounted" line (0.2.3): the rider's own `UnitAreaTransition`, 2.62 s old,
  4.03 m from its target. That is the entrance defect 0.2.5 repaired, in which the rider's command could not
  complete.
- Eight carried entrance approaches at 0.2.5.

**Native marker contract (installed assembly).**

- One pointer per unit in `ClickPointerManager`, never removed natively. `Update` shows it while the unit holds a
  live `UnitMoveTo` with `ShowTargetMarker` or a live `UnitAreaTransition`, and hides it otherwise.
- `UnitEntityView.OnMovementStarted` (`0600184E`) places it; its whole body is that placement. The unit's stock
  agent calls it when a path it requested completes.
- A command that is not close enough is planned through `UnitEntityView.MoveTo`, which always asks the **stock**
  agent, and neither `PathTo` nor the path callback tests whether that agent is enabled. So a mounted rider, whose
  stock agent is switched off and cannot move it, still has every command it holds itself planned and marked.

**The five cases the prompt names.**

| Case | Result |
|---|---|
| Duplicate rider and mount markers | **Established by source for a carried entrance approach** (0.2.5 and later): the mount's marker and, from the rider's own plan, a rider-keyed one. Repaired (1 below). Not for a routed ground click: one order, one mount-keyed marker. |
| Stale marker whose command has ended | Not established. Native hides a pointer the frame its command is finished. A pointer is shown again, where it was last put, only while its unit holds a new live qualifying command and until that command's own plan completes. |
| Legitimate marker of an unfinished move | Native rule, kept. The observer's "stopped short" line reports it; none in the three logs. |
| An order that bypassed routing | One recorded, the 0.2.3 entrance line above. By source that state shows a rider-keyed marker for as long as the command stays, and before 0.2.5 nothing completed it. No rider-held `UnitMoveTo` is on record. |
| Blocked or short-stopping arrival | Not observed. The order stays live and so does its marker, natively, until Stop, replacement or native interruption. |

**Two failing paths established, and repaired.**

1. **Second marker during a carried entrance approach.** The rider keeps its own member command while the mount
   walks, so native places two markers for one pair.
   - A prefix on `OnMovementStarted` skips the placement for the pair's rider **only while** the mount executes the
     member command the carry issued for that same rider command (`MountedDestinationMarkerPolicy.
     CarriedRiderPlacesNoPointer`, decided through the M01 classification).
   - A pointer the rider already had from an earlier walk is retired when the carrier is admitted
     (`MountedDestinationMarkers.RetireRiderPointer`: the entry is removed, then the object destroyed). Without that
     it would be shown at its old place for the whole approach.
   - A rider command nothing carries keeps the marker native gives it. Nothing is hidden for a stuck order.
2. **Marker under the rider for the scene regroup walk** (0.2.8 and later). `StartRegroup` built its `UnitMoveTo`
   with the native default, which asks for a marker. No player ordered that move; it now asks for none, as the
   attack, carried-approach and recovery moves already do not.

**What this does not establish.** Neither path is shown to be what the owner saw. Path 2 did not exist when the
report was made. The pre-0.2.5 entrance state (a rider-held entrance command with a rider-keyed marker and no
walker) is consistent with "an extra or stale marker" and with the paired report that the horse fell behind, but
no log ties the report to an entrance click.

**The missing observation.** For the frame the extra marker is visible: the keys of the native pointer table,
and for each key its position, visibility and the command that keeps it visible (type, executor, finished,
`ShowTargetMarker`, created by player). The product has a reader for the table in a diagnostic scenario
(`F3Marker`) but no play-session log line for it. Adding one was not done: the prompt rules out closing by
logging, and the two paths above were repairable without it.

**Not done, by the prompt's rules.** No global clear, no hiding of a marker whose command is stuck, no
manufactured completion, no retry, no other unit's marker touched.

## Evidence

All offline, Release, from the Sprint 3 worktree with Windows PowerShell 5.1
(`powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File …`). Logs:
`C:\Dev\KingmakerMountedCombatLab\artifacts\known-issues-sprint3\`.

| Check | Reviewed base `1dffc3e0` | This sprint, `26b4169e` |
|---|---|---|
| `Build-Local.ps1 -Configuration Release`, then the component test exe | 1257 pass, 0 fail | 1290 pass, 0 fail (33 new) |
| `Validate-Source.ps1` | 151 / 0 | 151 / 0 |
| `Test-PhysicalMoverPaceContracts.ps1` (new, 27 checks: 11 native, 16 product) | 11 pass, **16 fail** (run with `-ProductDll` on the base build) | 27 / 0 |
| `Test-PostLoadViewReadinessContracts.ps1` | 26 / 0 | 26 / 0 |
| `Test-PersistenceContracts.ps1` | 223 / 0 | 223 / 0 |
| `Test-WorldBoundaryChargeContracts.ps1` | 8 / 0 | 8 / 0 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 810 / 0 | 814 / 0 (four new native pins) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint3 -Focused` (the four contract scripts above) | — | **PASS 7/0**, 0.74 min |

Workflow budget `known-issues-sprint3`: 0.73 of 90 minutes, one entry, none failed.

**What the contract script does.**

- **Native group:** reads the installed methods' instructions for each contract above, including the two facts
  behind M05.
- **Product group:**
  - Runs both production transpilers on the installed methods' real instruction streams and requires exactly one
    changed site each (and that each refuses a method without its site).
  - Executes the compiled hooks with no pair present, the compiled actor projection and every compiled decision.
  - Reads the compiled carry, follower, speed, entrance and marker methods for what they call and in which order,
    and the patch installation for its three exact tokens.

**Failing before, passing after.** The base build fails the product group because the behaviour does not exist
there. To show that each test fails for the behaviour and not only for a missing name, each decision was mutated
in a **separate disposable worktree** (no mutant binary was ever built in the sprint worktree) and the suite run:

| Mutant | Change (one line of production source) | Component tests failing | Contract checks failing |
|---|---|---|---|
| A (M01) | `ClassifyOrder` never returns the carried entrance order | 8 | 1 |
| H (M01) | The carrier is never bound for the offer | 0 | 1 |
| N (M01, M02) | The routed ground order answers without its pair generation | 2 | 0 |
| B (M02) | `DecideGamepadFollower` never answers with the mount | 5 | 1 |
| G (M02) | The follower hook is not inserted | 0 | 1 |
| L (M02) | The follower answer accepts any classified order, not only the routed ground order | 0 | 1 |
| C (M03) | `SpeedInputIsMount` is always false | 6 | 1 |
| F (M03) | The speed transpiler is not composed into the group-move patch | 0 | 1 |
| M (M03) | The speed mover tests the unit against the mount instead of the rider | 0 | 1 |
| D (M04) | An earlier rider pointer is never retired | 2 | 1 |
| I (M04) | The carried rider's placement is never skipped | 4 | 1 |
| J (M04) | The rider's placement is skipped for any rider command, carried or not | 3 | 1 |
| K (M04) | The placement prefix is not installed | 0 | 1 |
| E (M04) | The regroup walk asks for a marker | 1 | 1 |

All fourteen are detected. The unmutated head in that same worktree is commit `26b4169e`; the sprint worktree's
binary was built once from that commit, was never rebuilt from a mutated source, and its MVID
(`6bacf01f-2aac-4927-b478-48c717573f28`) is the one the Fast tier and every result above report. An earlier round
of eight mutants, run before the review corrections and the M04 redesign, is kept beside it
(`mutants-round1-summary.txt`); all eight were detected there too.

**Independent review.** One independent read of the uncommitted diff against the decompiled native code. It found
no production defect in the transpilers, the carry, the hooks or the pointer retirement. Its findings and what was
done:

| Finding | Disposition |
|---|---|
| The integration methods that gather facts from the game are executed by no test; the script only counted calls. | Partly addressed: the script now pins the three mis-wirings the review named (the follower answering for any classified order, the speed mover testing the wrong unit, the identities asked without the pair generation), and mutants L, M and N show those pins fail. The methods are still not executed. Limit, below. |
| Assertions made with `SprintThree = false`, and those about the model's own native refusals, only test the model. | Kept as illustrations and labelled so in the test file. They are not the before evidence; the mutation runs and the base build are. |
| The "newly mounted pair" test supplied its own result: the routed-order classification had no generation term. | Corrected in production: `MountedPlayerMoveWatch.IsCurrent` answers for one pair generation, and the test now leaves the earlier order watched and in the slot. |
| `entranceCarrier` was never cleared (retention across area loads, not authority). | Corrected: `ReleaseIfEnded` from the controller's update, and on disposal. |
| The contract script is in no tier; it runs only when selected. | Not changed (the Sprint 2 script is the same). Noted for the owner under "Limits". |
| Real-time combat and a throwing mover are not modelled; hook executions cover the no-pair path only. | Stated as limits. |

Separately, before the review returned, a cross-check of the native approach code showed that my first M04 repair
rested on a wrong reading (that nothing places a pointer for a carried rider). The repair and its tests were
redone on the contract recorded above before anything was committed.

## Limits and uncertainty

- **Not executed:** `ClassifyMountGroupOrder`, `CarryNativeGroupAcceleration`, `GamepadFollowerOfferMover`,
  `GroupPaceSpeedMover`, `SkipCarriedRiderPointer` and `MountedDestinationMarkers.RetireRiderPointer` read live game
  objects. Their decisions were executed (source and compiled); their fact-gathering was read from IL. A swapped
  argument the pins do not name would not be caught offline.
- **Not executed:** the Harmony installation of the three new patches. The transpilers were run on the installed
  instruction streams; the patched methods were not run.
- **Models:** the pace and marker tests drive production decisions through models of the native code. A model is a
  reading, not native evidence.
- **Gamepad:** nothing about gamepad play has ever been run in this project. The M02 change is exact against the
  installed method, but the mode around it (console selection, M05) is unverified.
- **M04 path 1** assumes the rider's switched-off stock agent completes a plan as the installed code reads. If it
  does not, the new prefix simply never fires and the retirement still removes an old pointer.
- **Rider and mount end points:** whether the two markers of path 1 were visibly apart or overlapping was not
  observed.
- **Contract scripts and tiers:** `Test-PhysicalMoverPaceContracts.ps1` and
  `Test-PostLoadViewReadinessContracts.ps1` run only as focused selections. Adding them to a tier is a workflow
  decision for the owner.
- **T04–T08:** unchanged and open. None blocked a Sprint 3 check, so no harness was corrected.

## Compatibility

- No save format, archive member or settings change.
- Log: the "Mounted group pace" line gains `order=…`; the "Mounted entrance approach" line gains
  `riderPointerRetired=…`. No reader of either line exists in the repository.
- Three more native members are patched: `InGameInputLayer.UpdateMovement` (transpiler),
  `UnitEntityView.OnMovementStarted` (prefix), and a second transpiler stage on
  `ClickGroundHandler.MoveSelectedUnitsToPoint`. Each is pinned by exact token, and a transpiler whose site is not
  as recorded fails the patch installation, as the existing ones do.
- Another mod that also replaces the speed `Select` or the `SelectedUnits` read at those sites would conflict; none
  of the mods installed on this machine was inspected for it.

## Native acceptance, when a fixture exists

| Row | What to observe |
|---|---|
| M01 | Whole party, Accelerated Move on, click an exit more than 13 m away: the "Mounted group pace" line with `order=CarriedEntranceOrder`, and the mount arriving with the party. Repeat with the setting off and on a short walk. |
| M02 | Gamepad, an unmounted character driven, a mounted pair linked: the pair keeps up at full stick and slows with it. |
| M03 | A Small rider (20 ft) on a horse with 40 ft companions: the party's pace on a long walk. A slowed horse: the party at the 30 ft floor. |
| M04 | Click an exit while mounted, after the rider walked on foot earlier in that area: one marker for the pair. A scene that leaves the mount behind: no marker under the rider during the regroup walk. |
| M05 | Gamepad, the mounted rider driven: what the stick does, and whether the pair stays mounted. |

Not run: no merge, release, tag, installation, loader replacement or reviewer request, and Sprint 4 is not started.
