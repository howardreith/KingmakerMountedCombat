# Known issues remediation, Sprint 3 — mounted pace and the marker report (2026-11-16)

Sprint 3 of the owner's Mounted Combat Known Issues Remediation Epic: **M01** (entrance acceleration), **M02**
(gamepad acceleration), **M03** (group pace inputs) and **M04** (the extra or stale green marker). The current state
of every issue is in [CURRENT-KNOWN-ISSUES.md](CURRENT-KNOWN-ISSUES.md).

**Read the last two sections first.** The owner reviewed head `1223eb2f` and put direct gamepad control of a mounted
pair in scope. The [continuation section](#continuation-2026-11-16--direct-gamepad-control-m05-the-fault-report-c04)
records that work (M05 repaired offline, the fault report corrected, C04 recorded) and supersedes what the sections
below say about M05 being unrepaired. The owner then reviewed that head, `5e27c9bd`; the
[review section](#review-of-5e27c9bd-2026-11-16--r1-and-r2) records the two corrections to M05's ownership code and
holds the current results. The earlier sections are kept as the record of their own heads.

## Identity

- **Branch:** `codex/kmc-known-issues-sprint3`, draft PR #47, no reviewers requested.
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
| M05 (new) | **Open correction** at head `1223eb2f`; **implemented and verified offline** by the continuation. | Found while reconciling M02: stick input for a mounted rider is refused by native `Run`, and the source says the pair is then dismounted. Source-established, never observed. |

Nothing here is native-verified. This table is the state at head `1223eb2f`; the continuation section has the current
one.

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

Found while reading the gamepad contract. Not repaired at head `1223eb2f`; recorded so M02 is not read as gamepad
support. **Repaired offline by the continuation** (below), after the owner put direct gamepad control in scope.

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

## Continuation (2026-11-16) — direct gamepad control (M05), the fault report, C04

The owner reviewed head `1223eb2f` and set this continuation: direct gamepad control of a mounted pair **is** in
scope; M05 stays separately traceable; repair it within the existing architecture or disposition it precisely;
correct the new fault-reporting fallback; record C04. Nothing here was run in the game.

### Identity of the continuation

- **Base of the continuation:** `1223eb2f5c6eaa0bcede524fdb56ee27f3c69704` (the reviewed head), still on Sprint 2 head
  `1dffc3e0`. PR #47 still targets `codex/kmc-known-issues-sprint2`; PR #46 and PR #45 were not touched.
- **Commits:** `8e4e3a69` (C04 in the ledger), `b65977d8` (the fault report), `99b632f2` (M05), `ca6ecf42` (one more
  native check in the contract script), `e6cc84e2` (one test strengthened after the mutation run), then the records
  commit. The last three change no product source.
- **Artifact:** `bin/Release/KingmakerMountedCombat.dll` built from `99b632f2`: SHA-256
  `F5D35878DA03E8AB2B8E05D3BA6BF120FE2FBFCC194AD43CDD0CE302C99F2ED7`, MVID `02033cfa-df2c-4ce7-86d4-907a8d4a4f77`.
  The reviewed head's build, used for the before results, is the Sprint 3 artifact above (MVID `6bacf01f-…`),
  rebuilt from `1223eb2f` in a separate worktree with the same SHA-256.
- **Save format, settings:** unchanged.

### Dispositions after the continuation

| ID | Disposition |
|---|---|
| M02 | **Implemented and verified offline, in two parts.** Following-pair acceleration (Sprint 3, unchanged). Directly driven pair: native makes its own `Accelerate()` and `Deaccelerate()` calls on the mount's continuous command once M05 lets that command run. **Native: not run.** |
| M05 | **Implemented and verified offline.** The stick drives the pair's mount; the mount's continuous agent is owned by the exact command that installed it. **Native: not run; no gamepad session has ever been run in this project.** |
| Fault report | **Corrected and verified offline.** |
| M01, M03, follower acceleration | Unchanged from Sprint 3. |
| M04 | Unchanged: original report unresolved; the two adjacent marker corrections keep their own evidence. |
| C04 | Recorded, first in Sprint 4. Not investigated. |

### M05 — the mechanism, step by step, and how each step is known

| Step of the call chain | What happens | How it is known |
|---|---|---|
| Console selection and the driven unit | `InGameInputLayer.UpdateMovement` (`06005D0C`) stores `UnitSelectionManager.Instance.CurrentSelectUnit.Value` once and uses that one unit for everything after. It returns before that read while the party is in combat. | Installed IL, read. |
| Construction and `Init` | That method is the only native creator of `UnitMoveContiniously`. `Init` (`060026F0`) installs a `UnitMovementAgentContinious` as the **executor's** `UnitEntityView.AgentOverride` (`GetComponent`, else `AddComponent`). | Installed IL, read (the creator by a scan of every method body). |
| The executor rewrite | The mod's prefix on `Init` (`RouteContinuousMove`) changed a mounted rider executor to the mount. So the override went onto the **mount**. | Product source. |
| The container `Run` is called on | Native then calls `unit.Commands.Run(command)` on the **rider's** container. `UnitCommands.Run` (`060026B3`) refuses a command whose executor is not the container's owner, before it touches a slot. | **Executed:** installed `UnitCommands.Run` on genuine `UnitCommands` and `UnitMoveContiniously` objects takes the refusal branch and admits nothing (execution stops inside the refusal's own log call, which needs the Unity player). The same command on the mount's own container goes past the refusal. |
| The next native statement | `unit.Commands.MoveContiniously.Accelerate()` dereferences the missing command. | Installed IL, read. |
| Installation and retirement of the override | Only `Init` installs it and only the command's own `OnEnded` (`060026F2`) removes it. A command that never ran never ends, so the mount kept the override. | Installed IL, read (the writers by a scan of every method body). |
| Invariant validation | `ValidateMountedInvariants` faulted on any non-null mount override: "The authoritative mount movement agent changed" and a dismount. `MountAgentOverrideAvailable` then refused every later mount attempt. | Product source. |
| Movement synchronisation | The rider is placed from an anchor on the mount's view, so it follows the mount whichever agent moves it. `UnitMoveController.Tick` (`06009183`) moves a unit through `UnitEntityView.MovementAgent`, the override when one is installed. | Product source; installed IL, read. |

**A second reader with the same root.** `ClickGroundHandler.MoveSelectedUnitsToPoint` (`060093DB`) takes a non-null
override on the current unit to mean "this unit is being stick-driven" and gives it no ground order in gamepad
mode. A mounted rider always carries the pair's own rider agent as its override, so by source a mounted current
unit never received a ground order in gamepad mode (cursor clicks included). This too was never observed.

### M05 — the correction

One idea: **the stick drives the pair's physical mover, and the pair owns exactly what that installs.**

- **One unit for initialisation, execution, lookup and cleanup.** A transpiler on `UpdateMovement` passes the one
  store of the driven unit through `GamepadDrivenUnit`: a mounted pair's rider is answered by its mount. Every
  native step then uses the mount: the override reads, `Commands.MoveContiniously`, `InterruptMove`, `Init`,
  `Commands.Run`, `Accelerate`, `Deaccelerate`, and the command's own end on release. The earlier `Init` rewrite
  stays as the fallback when that hook faults.
- **The formation stays anchored on the selected rider.** The two `MakeCompanionsFollow(unit)` calls receive the
  rider (`GamepadFormationUnit`): native reads that unit's party index, and a pet mount has none.
- **The stick-driven test reads the mover.** The one `AgentOverride` read in `MoveSelectedUnitsToPoint` is answered
  with the mount's override for a mounted rider (`GamepadDrivenOverride`). While the pair is driven it gets no
  formation order (it would replace the stick movement); otherwise a ground order reaches it and is routed as in
  mouse play.
- **Ownership.** `MountedContinuousDriveOwner` (pure code) binds, after native `Init`, the exact command whose
  executor is the pair's mount, the exact agent instance on the mount's exact view, and the pair generation.
- **The invariant.** A mount override is admissible only if the owner says it is that agent, of that command,
  unfinished, in the mount's Move slot, for the current generation (or, in the binding frame only, between native
  `Init` and native `Run`). Any other override, a stale command, another generation or a destroyed component is
  rejected exactly as before.
- **The rider's mounted agent** reads `mount.View.MovementAgent` for its moving state and velocity. That is the
  stock agent whenever no stick movement is installed, so mouse play reads what it read before.

**Lifetime.** Native does the ordinary work; the mod acts only on its own record.

| Event | What ends the movement | What the mod does |
|---|---|---|
| Held input | — | Nothing. The record is kept while the command is in the mount's Move slot. |
| Full or eased stick | Native `Accelerate()` / `Deaccelerate()` on the mount's command. | Nothing. |
| Release | Native `OnTick` ends the command; native `OnEnded` removes the override. | Forgets the record. |
| Another command run on the mount (a routed ground order, a carrier, a regroup walk) | Native `Run` interrupts the continuous command; `OnEnded` removes the override. | Forgets the record. |
| An order of Move or Standard type admitted on the **rider's** container, directly or by queue promotion (review R2) | — (native `Run` would have ended a unit's own stick movement; the pair's runs on the mount) | Ends the bound command natively. A stick still held then creates a new command; taking the pair over ends an approaching rider order and leaves a stationary one (review section). |
| Selection change | Nobody feeds the mount's agent; native ends the command within a few ticks. | Forgets the record. |
| The party enters combat | Native stops reading the stick; in turn-based mode it would not tick the command either, leaving it in the mount's Move slot. | Ends the bound command natively at the next validation. |
| `Init` succeeded, `Run` did not admit (refused, queued, an unconscious mount, a save in progress) | Nothing would. | Ends the command without an end event and clears the bound agent, in the `Run` postfix. One warning per streak of refusals. |
| Dismount, world change, disposal | — | Ends the bound command natively before the pair's cleanup. Every world change of a pair goes through its dismount. |
| The bound agent is no longer the mount's override | — | At head `5e27c9bd`: forgot the record and touched nothing, which abandoned the command. Since review R1: ends the owned command and clears no override; the foreign override stays installed and stays a fault. |

**Also at the start of a drive.** The `Init` prefix ends the rider's own moving orders
(`InterruptRiderApproachingOrders` and the rider's native `InterruptMove`) before the existing pair cancellation,
because the native `InterruptMove` of the taken-over unit now reaches the mount. Admission clears the rider's Hold,
as a routed ground order already does.

**Consumers of the mount's movement agent that were inspected, and why they are not changed.**

| Consumer | Reads | Why it stays |
|---|---|---|
| `MountedAnimationAdapter` | `View.MovementAgent` | Already the agent that moves the unit. |
| `MountedFamiliarFollowAdapter` | the stock agent's path, else the mount's position | No path during stick movement, so it follows the position. |
| `MountedExplorationObserver` | the stock agent, for a routed `UnitMoveTo` only | A continuous command is not one. |
| `ClassifyMountGroupOrder` (M01, M02, M04) | an exact `UnitMoveTo` or `UnitAreaTransition` in the mount's Move slot | A continuous command classifies as none. |
| Carried approach, door interaction, entrance carry, regroup | run their own command on the mount | Native `Run` ends the stick movement first; the carried approach waits while the slot is busy. |
| Charge, pair attack, turn-based movement adapters | the stock agent, in combat | The drive is ended when the party enters combat. |
| `NativeSaveEffectBoundary` | commands that need settlement | It already excludes `UnitMoveContiniously`. |
| Mount attempts (`MountAgentOverrideAvailable`), restore checks | the override must be null | Kept strict: nothing owned is left once a drive ends. |

**Not done, on purpose:** no general acceptance of a mount override, no change to the invariant's other terms, no
second movement controller, no replacement of the product's other stock-agent reads, no forced admission, no synthesized
action, no change to native acceleration, speed, settings or combat restrictions.

### The fault report

`ReportGroupPaceFault` called the logger directly inside each hook's `catch`. A logger that threw there escaped the
hook, so the hook never returned its native fallback. It now goes through `MountedBoundedFaultReport`: reported once
as before, and a sink that fails is contained. Nothing else about logging changed.

### Evidence of the continuation

All offline, Release, Windows PowerShell 5.1, from the Sprint 3 worktree. Logs `20-…` to `23-…` and
`mutant-m05-*.log` in `C:\Dev\KingmakerMountedCombatLab\artifacts\known-issues-sprint3\`.

| Check | Reviewed head `1223eb2f` | Continuation, `99b632f2` |
|---|---|---|
| Component test exe | 1290 / 0 | 1313 / 0 (23 new: 22 for M05, 1 for the fault report) |
| `Validate-Source.ps1` | 151 / 0 | 151 / 0 |
| `Test-ContinuousDriveContracts.ps1` (new) | 12 pass, **20 fail** (run with `-ProductDll`) | 32 / 0 |
| `Test-PhysicalMoverPaceContracts.ps1` | 27 / 0 | 27 / 0 |
| `Test-PostLoadViewReadinessContracts.ps1`, `Test-PersistenceContracts.ps1`, `Test-WorldBoundaryChargeContracts.ps1` | 26, 223, 8 | 26, 223, 8 |
| `Test-PairedActivationContracts.ps1` (patch construction) | not selected | pass |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 814 / 0 | 814 / 0 |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint3 -Focused` (the six contract scripts above) | — | **PASS 9/0** at `e6cc84e2` |

Workflow budget `known-issues-sprint3`: 1.78 of 90 minutes, four entries, none failed.

**What was executed, read, or modelled.**

| Kind | What |
|---|---|
| Native code executed | `UnitCommands.Run` with a command whose executor is another unit, on genuine engine objects built without the game: the refusal branch is taken and nothing is admitted. Only this; `UnitMoveContiniously.Init` itself needs Unity components and cannot run offline. |
| Native code read (IL) | The single creator and its one driven-unit store; `Init`, `OnTick`, `OnEnded`; the override's readers and writers across the whole assembly; `UnitMoveController.Tick`; the ground handler's stick-driven test; the combat gate and turn-based ticking. |
| Production code executed | Both transpilers on the installed methods' real instructions, with their branch targets, requiring exactly the recorded insertions and replacement; the compiled hooks with no pair; the compiled record, decisions and retirement plan; the compiled owner on a scripted host (admitted drive, combat, three refused admissions, foreign override); the hooks with a real operational failure and a throwing logger; and, in the component tests, the same owner, record and decisions driven by a model of the native chain. |
| Production code read (IL) | The game adapter (`ContinuousDriveHost`): which unit, slot, field and native call each answer uses. The invariant's call to the policy. Where the drive is settled, observed and released. The rider agent's reads. The patch registrations. |
| Model only | The native stick chain in the component tests (`World`), including the end-of-frame destruction of a component. The `Corrected = false` assertions show the reviewed behaviour in that model; they are illustration, not the before evidence. |

**Failing before, passing after.** The reviewed head fails every product check of the new script, including the
executed one: with an operational failure inside a hook and a throwing logger, the reviewed hook throws
`InvalidOperationException: the logger failed` instead of returning the selection. To show the checks fail for
the behaviour and not for a missing name, 30 one-line mutations of the new production code were each
built and run in the separate scratch worktree:

| Mutant | Change (one line of production source) | Component tests failing | Contract checks failing |
|---|---|---|---|
| A | The driven unit stays the rider | 15 | 1 |
| I | The formation is anchored on the mount | 1 | 1 |
| N | The driven-unit hook is not inserted | 0 | 1 |
| W | The formation hook is not inserted | 0 | 1 |
| O | The stick-driven test is not composed into the ground-order patch | 0 | 1 |
| B | The invariant is strict again (no override admitted) | 12 | 1 |
| C | The invariant admits any override | 2 | 1 |
| M | The invariant never asks the owner | 0 | 1 |
| D | The record owns any agent instance | 1 | 3 |
| E | The record owns across pair generations | 1 | 1 |
| J | Settlement ignores the pair generation | 1 | 1 |
| V | The owner's answer does not test the agent type | 1 | 0 |
| a | The game adapter accepts any agent type | 0 | 1 |
| S | Binding does not test the executor | 1 | 1 |
| F | A refused `Run` keeps its same-frame grace | 4 | 2 |
| R | The admission is not observed after `Run` | 0 | 1 |
| L | The drive is not settled before the invariant | 0 | 1 |
| G | Retirement proceeds without the bound agent installed | 3 | 2 |
| P | Retirement clears the override without comparing it again | 0 | 2 |
| K | The drive is not released at dismount | 0 | 1 |
| X | Combat never ends the drive | 2 | 2 |
| b | The game adapter never reports combat | 0 | 1 |
| Y | A rider order never replaces the drive | 1 | 1 |
| d | The rider order is tested on the mount's container | 0 | 1 |
| U | The rider's orders are not ended at the start of a drive | 0 | 1 |
| c | The rider's Hold is not released | 1 | 1 |
| Z | Every refusal is reported | 1 | 1 |
| Q | The rider's agent reads the stock agent | 0 | 1 |
| H | The bounded report lets a sink failure escape | 1 | 3 |
| T | The fault report calls the logger directly | 0 | 3 |

All 30 are detected. A zero in the first column is a mutation of game-facing glue, which only the contract script
can see. The unmutated tree in that worktree builds to the sprint artifact's exact identity (MVID
`02033cfa-df2c-4ce7-86d4-907a8d4a4f77`, the same SHA-256), and the sprint worktree's binary was never built from
mutated source. Two reruns are included: F did not compile as first written and was rewritten, and S was first
caught by the contract script only, so its component test was strengthened (`e6cc84e2`) and it was run again.

**Independent review.** One independent read of the uncommitted M05 change against the decompiled native code and
the installed IL. It found the transpilers correct (stack shape, branch targets, site conditions) and the
ownership sequences sound. Its findings and what was done:

| Finding | Disposition |
|---|---|
| A bound drive is never ended when the party enters turn-based combat: native neither feeds nor ticks it, and it holds the mount's Move slot. | Confirmed against the native tick code. Corrected: the `InputGated` settlement ends the bound command natively. Test, scripted-host check, mutants X and b. |
| An order given to the rider during a drive neither ended the drive nor was cancelled by it. | Corrected: a Move or Standard order admitted on the rider's container ends the bound command, as native `Run` ends a unit's own. Test, mutants Y and d. |
| The rider's Hold was not cleared by stick movement. | Corrected at admission. Test, mutant c. |
| A refused admission warned every frame while the stick was held. | Corrected: one warning per streak; every retirement is still counted. Test, mutant Z. |
| Counters and messages mislabelled two retirements. | Corrected: one retirement count, an orphan count, and the settlement named in the message. |
| The repath timer's closure now sees the mount, so the rider's stale stock-agent estimate takes part. | Not changed. Limit, below. |
| The transpiler checks could not see branch targets. | Corrected: the probe carries labels through the production transpilers and compares them. |
| The scenario tests ran a copy of the bind, settle and retire logic, not the production code. | Corrected: that sequence is now pure production code behind a host interface (`MountedContinuousDriveOwner`), executed by the tests and, compiled, by the script. The game adapter is still only read. |
| A test hid the native fault that follows a refused admission; two script labels claimed more than they checked; the model had no destroyed-component behaviour. | Corrected: the fault is asserted and named in the label; the labels were reworded; the model destroys components at the end of the frame and a test covers the same-frame adoption. |
| The `Init` prefix's new call was not guarded. | Corrected: a fault in it is reported and the existing cancellation still runs. |

### Limits and uncertainty of the continuation

- **Native: NOT RUN.** No gamepad session has ever been run in this project, mounted or not. Every statement about
  what the stick does is a reading of the installed assembly, except the executed `Run` refusal.
- **Not executed:** the game adapter that answers from live units, the three hooks with a live pair, and the Harmony
  installation of the changed patches.
- **A refused admission still faults natively.** Native dereferences the command its `Run` did not admit, for a
  mounted pair as for any unit, every frame the stick is held. The pair now survives it; the native fault is not
  repaired.
- **Combat.** The drive is ended at the first validation after the party enters combat, a few ticks earlier than a
  native unit would coast to a stop in real time.
- **Followers of a stick-driven pair.** Two native reads still look at the selected rider, not the mount: the
  followers' speed cap to the leader's stick speed (`UnitEntityData.CurrentSpeedMps`) and the stock agent's
  slow-down exemption. Followers of a driven pair are paced by the group speed limit (M03) only. Not changed.
- **Repath cadence.** The native repath timer now includes the rider's unused stock-agent estimate, which biases it
  to its 0.1 s floor: the follower pass can run more often. Not changed.
- **Formation.** It is anchored on the rider's position and facing, which is the saddle, not the mount's feet.
- **Gamepad ground orders.** By source they now reach a mounted current unit that is not being driven. That is a
  change of behaviour nobody has observed in either state.
- **Not examined:** other gamepad-only paths (turn-based play with the cursor, the console selection containing the
  mount itself beyond what the tests cover), and other mods that patch the same two methods.
- **T04–T08:** unchanged and open; none blocked a check.

### Native acceptance rows added by the continuation

| Row | What to observe |
|---|---|
| M05, drive | Gamepad, a mounted rider selected: the pair moves with the stick, stays mounted, runs at full stick, slows when eased, stops on release. The log shows no "authoritative mount movement agent changed". |
| M05, replace | While driving: interact with an object, change the selected character, press an exit. Then drive again. |
| M05, combat | Drive into an encounter in turn-based mode: the pair's first ground order is accepted. |
| M05, dismount | Dismount while holding the stick, then mount again and drive. |
| M05, ground order | Gamepad cursor: a ground click moves a mounted current unit that is not being driven. |
| Fault report | Not observable in play. |

## Review of `5e27c9bd` (2026-11-16) — R1 and R2

The owner reviewed head `5e27c9bd` and returned two findings about M05's ownership code. Both are corrected. The M05
routing repair, follower acceleration, M01, M03, the marker corrections, the bounded fault report and the C04 record
are unchanged. Nothing here was run in the game.

### Identity

- **Base:** the reviewed head `5e27c9bd88b40ff3c857327c3655247a9f782323`, still on the Sprint 2 head `1dffc3e0`. PR #47
  still targets `codex/kmc-known-issues-sprint2`.
- **Commits:** `e98eac31` (R1 and R2: code, tests, contract script), `eb838c9d` (one more executed check in the contract script, after the mutation run; no product source), then the records commit.
- **Artifact:** `bin/Release/KingmakerMountedCombat.dll` built from `e98eac31`: SHA-256 `7B9BACE5E96254CE719527B4F3B481DA5A0BED82B792794E4226E09870BD970C`, MVID
  `8d84952a-eecb-4432-9415-468d06b30729`. The reviewed head's build, used for the before results, was rebuilt from `5e27c9bd` in a
  separate worktree: SHA-256 `F5D35878…2ED7`, MVID `02033cfa-…`, identical to the artifact reported for that head.
- **Save format, settings:** unchanged.

### R1 — the owned command is retired without clearing a foreign override

**The defect.** Native `UnitMoveContiniously.OnEnded` clears the override of the executor's **current** view,
whatever it holds; the command keeps no view or agent of its own. The reviewed `Retire` compared the **captured** view
and agent, discarded its record, and then called native `Interrupt`:

- **A, a replaced view.** If the mount had received another view carrying an unrelated override, the interrupt's
  native end cleared that override.
- **B, a replaced agent.** If a foreign override had replaced the bound agent, the record was dropped and the
  owned command left running. When native later ended it, its end cleared the foreign override.
- **The record.** It was cleared before any cleanup ran, so a failed interrupt or removal left nothing to retry.

**The correction.**

- **A guard inside the native end.** A transpiler on `UnitMoveContiniously.OnEnded` (`060026F2`) routes its one
  `AgentOverride = null` write through `EndedContinuousCommandOverride`, with the ending command. For a command a
  pair bound, that write is made only when the view is the bound view and its override is the bound agent
  (`NativeEndMayClearOverride`). A command no pair bound keeps the native rule. `base.OnEnded`, its end event and
  everything else in the method always run: nothing of `OnEnded` is bypassed but the clearing of an override the
  command does not own. The guard finds the command's owner in a table keyed by the command object, so it still
  answers after the pair was dismounted.
- **Two separate retirements.** The command is ended natively whether or not its agent is still installed. The
  captured agent is cleared only on the exact view it was bound on, and only while that view still holds it. Each
  is attempted on its own; a failure of one does not stop the other.
- **The record is transferred, not dropped.** Retiring moves the exact command, view and agent into a residue
  record. It is forgotten only when the retirement is established: the command is terminal, or no longer held by
  its executor, **and** the captured view no longer holds the captured agent. A terminal command alone is not
  proof: native `Interrupt` (`060027AC`) stamps the result before it calls `OnEnded` and never repeats that
  callback.
- **Retry.** Residue is retried at each settlement (three automatic attempts per record), and always at release
  and at a new binding. A failure is contained, counted and reported once. Residue is never an owned live drive:
  the invariant does not admit its agent.
- **A new command that adopts a residual agent** (native `Init` reuses the component) owns it from then on; the
  earlier record neither clears it nor lets its own command's end clear it.

**Behaviour after the correction.**

| Situation | The owned command | The captured agent (on the captured view) | A foreign override |
|---|---|---|---|
| The mount's view was replaced and the new view carries an unrelated override; dismount or a rider order releases the drive | Ended natively, once | Cleared on the captured view | Survives the release and every later cleanup, tick and termination |
| A foreign override replaced the bound agent on the same view; relationship cleanup runs | Ended natively, once | Not installed, so nothing is cleared | Survives cleanup and the command's later life |
| The same, and native ends the command before cleanup runs | Already ended | Nothing is cleared | Survives: the guard refuses the native clearing while the record exists |
| The interrupt fails once | Still running and held: record kept, retried at the next settlement | Cleared on its own in the first attempt | — |
| The native end fails after the command became terminal | Terminal; not interrupted again | Cleared on its own | — |
| The override removal fails once | Ended | Still installed: record kept, removal retried | Untouched by either attempt |
| The failure persists | Three automatic attempts, one report, record kept; release retries again | | |

### R2 — a rider order admitted through queue promotion

**The defect.** The observer of rider orders ran after the public `UnitCommands.Run(UnitCommand)` only. Native queue
promotion (`RemoveFinishedAndUpdateQueue`, `060026BD`) calls the internal
`Run(UnitCommand, bool fromQueue, bool doNotClearQueue)` (`060026B3`) directly, so an order that was queued and
admitted later never ended the pair's stick movement.

**The correction.** The observation moved to a postfix of the internal `Run`, the one overload that puts a command
into a slot and that both routes reach. An order counts only if it is then in a slot of the rider's own container.

| Case | Result |
|---|---|
| Direct admission of a Move or Standard order on the rider | Ends the drive, once (unchanged) |
| The order is only enqueued (the rider is busy with an uninterruptible action) | The drive continues |
| That order is later promoted and admitted | Ends the drive exactly once: one interrupt, one native end callback, one end event |
| The promotion is refused (by the mod's own prefix, or natively for an unconscious unit) | The drive continues |
| Nested public and internal `Run` calls (a custom handler runs another order first) | The drive ends once |
| A Free order, another unit's order, another pair's rider order, an order with no drive bound | Nothing changes |

**The native slot rules, established before touching the stationary-order case.** Read from the installed assembly
and pinned in the contract script:

- `InterruptMove` (`060026C2`) ends only commands that have approached (`IsMoveUnit`, set only by `TickApproaching`).
  A stationary order is not among them.
- A new Move or Standard order ends the commands in **both** of those slots of the same container
  (`InterruptAndRemoveCommand`, `060026BF`). If any command of the unit is uninterruptible, the new order is queued
  instead.
- So for one native unit, starting stick movement ends that unit's interruptible Standard order, through the
  pairing, and is itself queued behind an uninterruptible one.

**What that means for a pair, and what was not changed.** The pair's stick movement runs in the mount's container,
so the pairing never reaches the rider's slots. The integration forwards the approaching-order interruption only
(`InterruptRiderApproachingOrders` and the rider's native `InterruptMove`). A **stationary** rider Standard order
therefore runs on while the stick drives the mount, exactly as it runs on beside a routed ground order, which has
been the pair's rule since the mount became the mover. That is kept: ending it would be a new behaviour for mouse
and stick alike, and the uninterruptible case has no pair equivalent short of refusing the mount's movement.

The **model** was wrong here, not the integration: its `InterruptMove` ended every non-Free order, which supplied
the old test's "a held stick cancels the rider's order". The model now follows the pinned rules, and the test states
what production does: an in-range Standard order ends the drive when admitted; a held stick takes the pair again,
ends an **approaching** rider order and leaves a **stationary** one.

### Evidence

All offline, Release, Windows PowerShell 5.1, from the Sprint 3 worktree. Logs `30-…` onward and `mutant-r1-*.log`
in `C:\Dev\KingmakerMountedCombatLab\artifacts\known-issues-sprint3\`.

| Check | Reviewed head `5e27c9bd` | After the corrections |
|---|---|---|
| Component test exe | 1313 / 0 | 1326 / 0 (13 more: 9 regressions for R1, 3 new for R2 beside the rewritten rider-order test, 1 of the model itself) |
| `Test-ContinuousDriveContracts.ps1` | 32 / 0 as it was; **31 pass, 12 fail** against the script as extended for R1 and R2 (run with `-ProductDll`) | 43 / 0 |
| `Test-PhysicalMoverPaceContracts.ps1` | 27 / 0 | 27 / 0 |
| `Validate-Source.ps1` | 151 / 0 | 151 / 0 |
| `Test-AssemblyContracts.ps1 -Target Kingmaker` | 814 / 0 | 815 / 0 (the newly patched `OnEnded` is pinned) |
| `Test-Tiers.ps1 -Tier Fast -BudgetId known-issues-sprint3 -Focused` (continuous drive, physical mover pace, post-load view readiness, persistence, world boundary, paired activation) | PASS 9/0 | **PASS 9/0** at `eb838c9d` |

Workflow budget `known-issues-sprint3`: 2.52 of 90 minutes, six entries, none failed.

**The reviewed implementation failing the two regressions.** The extended script runs the **compiled** owner of
each build on a scripted host whose `Interrupt` behaves as the native call does (the command becomes terminal, then
its end clears the override of the mount's current view unless that build's own guard forbids it):

- **Reproduction A on `5e27c9bd`:** fails with "the unrelated override B on the mount's current view was cleared
  by the dismount release".
- **Reproduction B on `5e27c9bd`:** the cleanup does nothing at all (no native call), and the check fails with "the
  abandoned owned command later ended natively and cleared the foreign override".
- **R2 on `5e27c9bd`:** the build has no postfix on the internal `Run`; the registration and wiring checks fail.
  The native route itself is executed (below), so the bypass does not rest on a reading alone.

After the corrections both reproductions pass for the dismount release and the rider-order release, as do the retry
cases.

**What was executed, read, or modelled.**

| Kind | What |
|---|---|
| Production ownership code executed | The compiled owner on the scripted host: reproductions A and B, four failure-and-retry cases, admitted drive, combat, refused admissions. The same owner, record and decisions in the component tests, driven by the model: nine regressions for R1 and four for R2 beside the earlier ones. The production transpiler for `OnEnded` on the installed method's real instructions, branch targets included. The compiled guard with its command table, for a bound and an unbound command. |
| Installed native code executed | `UnitCommands.Run`'s refusal (as before). New: on a genuine `UnitCommands` container, enqueueing behind an occupied slot admits nothing; when the occupant is finished, `RemoveFinishedAndUpdateQueue` takes the queued command and enters the **internal** `Run` with no public `Run` frame on the stack (execution then stops for lack of the running game). |
| Installed native code read (IL) | `Interrupt` stamps before `OnEnded`; `UnitMoveContiniously` has no fields and reads its executor's view at the end; the callers of the internal `Run` across the whole assembly (the public `Run`, its own custom-handler path, queue promotion); `InterruptMove`, the only setter of `IsMoveUnit`, the Move/Standard pairing, the uninterruptible queueing rule. |
| Modelled native behaviour | The stick chain, slots, queue, promotion and the end callback in the component tests' `World`. A unit and its view are separate objects there, and each view keeps its own override. One test asserts the model itself against the pinned slot rules and is labelled so. |
| Not executed | The game adapter's answers that need a live pair (the mount, its Move slot, combat state) and the three postfixes with live units; the guarded native write itself (it needs a live Unity view); the Harmony installation; any gameplay. The adapter's `IsHeld`, `ExecutorOf`, `IsFinished` and `OverrideOf` are executed on genuine engine objects. |

**Mutation checks**, in the separate scratch worktree, never in the worktree whose artifact is reported:

| Mutant | Change (one line of production source) | Component tests failing | Contract checks failing |
|---|---|---|---|
| A | The native end may always clear (the guard always allows) | 8 | 4 |
| B | The guard ignores which agent is installed | 5 | 3 |
| J | The guard ignores residue records | 8 | 2 |
| Y | The guard never allows a bound command's own clearing | 3 | 4 |
| E | The command is ended only while its agent is installed (the reviewed behaviour) | 6 | 3 |
| G | The captured agent is cleared without reading its view again | 6 | 5 |
| C | A terminal command counts as proof that its override is gone | 3 | 2 |
| D | An unfinished, still held command is forgotten | 2 | 2 |
| F | The record is dropped before the retirement is established | 5 | 1 |
| M | Settlement recovers no residue | 4 | 2 |
| H | Automatic retries are unbounded | 1 | 0 |
| L | An interrupt failure escapes the attempt | 4 | 1 |
| K | A failing report escapes | 1 | 0 |
| I | An adopted agent is not handed to the new command | 1 | 0 |
| a | A superseded record keeps the agent the new command adopted | 1 | 0 |
| Z | A re-entrant attempt is not skipped | 1 | 0 |
| b | A retired command is never forgotten | 4 | 0 |
| T | The guard does not ask the owner | 0 | 2 |
| U | A bound command is not registered for the guard | 0 | 1 |
| R | The native-end guard is not registered | 0 | 1 |
| S | The ending command is not passed to the guard | 0 | 1 |
| V | The adapter answers every view with the mount's | 0 | 1 |
| W | The adapter never reports a held command | 0 | 1 |
| c | The adapter ignores a command parked as the previous command | 0 | 1 |
| P | The admission postfix is not registered on the internal `Run` | 0 | 1 |
| Q | The admission postfix observes nothing | 0 | 1 |
| X | An enqueued order counts as admitted | 0 | 1 |

All 27 are detected. A zero in the first column is a mutation of game-facing glue, which only the contract script
can see. The unmutated tree in that worktree builds to the reported artifact's exact SHA-256, and the reported
worktree's binary was never built from mutated source. W was first undetected: the script only counted the calls
`IsHeld` makes. It now runs the compiled adapter's own answers on genuine engine objects (`eb838c9d`), and W was run
again.

**Independent review.** One independent read of the uncommitted R1 and R2 corrections against the decompiled native
code. It found no blocker and no major defect: the `OnEnded` rewrite is correct for the method's shape and branch
targets, a foreign override is never cleared, residue is dropped only through the established test, a healthy drive
still ends normally, and both admission routes are observed once. Its points and what was done:

| Finding | Disposition |
|---|---|
| An end-event handler that comes back into the owner during the native end could establish and drop the record before the guard is asked. No subscriber in the repository does that. | Hardened: nested recovery leaves the running attempt alone, and records are removed by reference. Test, mutant Z. |
| Binding overwrote a record still bound to another command. Not reachable natively. | Corrected: that record is retired, and the agent the new command adopted is handed over. Test, mutant a. |
| The command table might never release its entries on the game's runtime. | Corrected: a command is forgotten when its retirement is established. Tests assert it; mutant b. |
| A held stick bypassed the automatic retry limit, because each new binding recovered explicitly. | Corrected: a new binding recovers automatically, within the limit. |
| `IsHeld` ignored a command parked as the container's `PreviousCommand`. | Corrected in the adapter and pinned; mutant c. |
| The production command table was executed by no check. | Corrected: the compiled guard is run with its table for a bound command. |
| A guard that always refuses would have passed the unit tests. | Corrected: a normal release asserts the owner clears nothing; mutant Y. |
| Some "later termination" lines run on a command that is already terminal and prove nothing more. | Kept as a plain re-check. The real proof is the retained-command case: native ends the command while its record exists, and the foreign override survives. |
| The merge path of native `Run` and commands parked while paused are neither observed nor modelled. | Not changed. Limit, below. |
| The owner's fact reads in `Bind` and `Settle` are outside the contained attempt. | Not changed: the adapter's reads are null-safe and every call site is contained. |

### Remaining uncertainty

- **Native: NOT RUN.** No gamepad session has ever been run in this project. View replacement during stick movement
  and a foreign override on a mount are constructed cases; nothing in the game is known to produce them.
- **The guarded native write is not executed.** The transpiler that installs it is run on the real method and the
  decision behind it is executed, but the hook's own two lines need a live Unity view.
- **Pause.** While the game is paused, native `Run` parks the running command of a slot as the container's
  `PreviousCommand` instead of ending it. A drive displaced that way is retired as an orphan, as before; the paired
  parking of a rider order was not modelled.
- **A stationary rider order and the stick** follow the pair's rule, not the single-unit rule (above). If the owner
  wants the native pairing mirrored onto the rider, that is a behaviour decision for both mouse and stick.
- **A detached owned component.** When a foreign override replaces the bound agent without native's own setter
  destroying it, the continuous-agent component stays on the view, not installed as an override. Nothing clears it;
  native's next `Init` adopts it.
- **Unchanged limits of the continuation:** the followers' native speed cap and slow-down exemption still read the
  selected rider; the repath timer includes the rider's unused stock-agent estimate; a refused admission still
  faults natively.
- **M04** stays unresolved. **T04–T08** are unchanged and open; none blocked a check.

Sprint 4's order stays C04, C01, C02, C03 and is not started.

Not run: no merge, release, tag, installation, loader replacement or reviewer request, and Sprint 4 is not started.
