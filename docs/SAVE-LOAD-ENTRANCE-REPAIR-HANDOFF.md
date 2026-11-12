# Save/load and entrance approach repair — review handoff (2026-11-12)

**Status: RUNNING-GAME LOADING AND ENTRANCE APPROACH REPAIRED IN SOURCE, WITH OFFLINE, INSTALLED-IL AND DETACHED
ADAPTER CHECKS; SAVING UNRESOLVED (NO SAVE EXECUTION OBSERVED; WHERE THE ATTEMPT STOPPED IS NOT ESTABLISHED; BOUNDED
DIAGNOSTICS ADDED); NATIVE NOT RUN (BLOCKED); OWNER ACCEPTANCE SEPARATE.** No merge, release, tag or install.

- **Branch:** `codex/kmc-save-load-entrance-repair`, from main `bcc176ec` (v0.2.4; `origin/main` fetched at intake and
  unchanged). Draft PR [#31](https://github.com/howardreith/KingmakerMountedCombat/pull/31); repair commit `07468efb`; review
  corrections on top (see "Review of PR #31").
- **Owner report (0.2.4):** "Alright got through the door OK, but now saving and loading a game are completely broken.
  Also clicking on the entrance to the door does not result in the party moving toward it."
- **Evidence:** `output_log(3).txt` (2,357,817 bytes, SHA-256 `da0f9578…2729`, 10,916 LF lines). It reports KMC 0.2.4.
  The owner's DLL was not byte-matched. The log is kept local.
  - 10847–10858: the same-area teleport repair ran exactly as designed (begin, suspension, local teleport, native
    placement, remount).
  - 10911: a turn-based paired activation was prepared (combat).
  - 10912–10916: four "Area change refused" lines. The 0.2.4 message does not name its caller, so the log alone does
    not identify each line as a load attempt. The source establishes a defect that refuses an in-game load during a
    paired combat turn with exactly this message, which fits the owner's report.
  - No save execution line (deferral, snapshot, serialization or failure) appears after the door. This does not say
    where an attempted save stopped (see SAVING).

## RUNNING-GAME LOADING — cause established, repaired in source

**Cause (installed IL plus KMC source).**
- `Game.LoadGame` (`06000CE0`) runs `PrecheckSaveCorruption`, then synchronously calls the full `Game.LoadArea`
  (`06000CD5`) with the selected `SaveInfo`.
- KMC's `AreaTransitionPrefix` applied the exploration *travel* guard (`AdmitChargeAreaTransition`) to every call before
  looking at `saveInfo`.
- In a combat turn, `TrySettleExplorationParticipation` returns false as soon as an actor is in combat, so the guard
  refuses the load with the same message the log shows.
- In-game loading (the load window, quickload and the game-over load) calls `Game.LoadGame` and was refused.
- `LoadGameFromMainMenu` runs load housekeeping first, so main-menu and cold loads kept working. KMC's own native
  harness only ever loaded through that entry (`Chunk7PersistenceScenario.Replay.cs`). That is how this escaped
  qualification.
- The same order also meant the documented recovery from any latched restore failure, "load a valid save", was refused
  in a running game.
- This defect predates 0.2.4. 0.2.4 did not change this order.
- A refused load left no native state behind: `LoadGame` had only run its read-only precheck, and KMC's settlement call
  returns before any mutation in combat.

**Change.**
- `MountedWorldBoundaryAdmissionPolicy` classifies the full `LoadArea` by its `saveInfo`.
  - **Travel** (null): the 0.2.4 terms, in the same order and with the same lazy evaluation. The log now names the
    refusing term and the first unsettled participant (rider and mount ids, owner id and generation, combat state, and
    activation).
  - **Saved-world replacement** (non-null): still refused by an archive worker that can commit, an active save, a queued
    save admission, a charge fence, retained charge cleanup on **any** pair, or the selected archive's own admission
    (`CanLoadBeforeWorldReplacement`, bound to the exact descriptor `Game.LoadGame` admitted in the same frame and
    re-run otherwise).
  - **Charge cleanup on every pair (review of PR #31).** A pair can retain charge ownership without a charge fence
    (its native ability process has not ended), and the unloading cleanup cannot veto an unload. Once the save guards
    admit, the decision drains every pair's charge custody (`DrainWorldCharge`) before the native unload can begin; a
    drain that throws refuses. `CanLoadBeforeWorldReplacement` previously checked and drained only the primary pair;
    it now uses `AnyChargeFence` and the same all-pair drain, so the `Game.LoadGame` admission is covered too.
    Ordinary participation is still never consulted.
- **Admission releases nothing.** The outgoing world is released by the established load path:
  - the native unload's `OnAreaBeginUnloading` cleanup;
  - then `WrapLoadRoutine`, which retires the previous load scope and then calls `BeginLoadHousekeeping`;
  - housekeeping releases participation without End or forfeit, and clears the latch only after cleanup succeeds.
- A refused load changes nothing.
- Each in-session load logs one bounded admit or refuse line. It carries the operation, the entry, the selected and
  outgoing campaign and area, the binding, the combat, turn-based and activation state, and the transfer, latch, save
  and charge state.

## SAVING — unresolved: no save execution observed; diagnostics added

**Traced path (installed IL).**
1. Quicksave (`MakeQuickSave`) and the save window go through `Game.SaveGame` (`06000CE4`).
2. `SaveGame` refuses through `IsSaveAllowed`, with a UI warning only.
3. `IsSaveAllowed`'s combat term is KMC's `NativeCombatBlocksSave`.
4. Then `SaveRoutine` (KMC `SavePrefix` → `WrapSaveRoutine` → deferred save).

**Findings.**
- With the shipped preset (paired activation on, unified turn and scheduler off), KMC's combat gate admits a turn-based
  combat save.
- `CombatRestorationPending` could not have been true: it refuses every command, and the party was moving and casting.
- `WrapSaveRoutine` constructs and returns the wrapped operation **without logging**. The deferral notice, the snapshot
  and serialization lines and any failure report are written only when that operation later executes. Their absence
  therefore excludes an execution that began, not a save that was constructed or queued and never started.
- Ruled out as a cross-mod cause: KingmakerGunslinger's production source at 0.0.184 does not patch the save gate. Only
  its test harness mentions it.
- A refused load does not leave native loading state that disables saving.

**Conclusion.** The log contains no observed save execution after the door. It does not establish where an attempted
save stopped. The owner's saving report stays **open**. Nothing in source establishes a KMC save defect, so no save
behaviour was changed. The next investigation must tell apart:
1. an action that never reaches `Game.SaveGame` (the UI or quicksave path);
2. a refusal by its admission check (`IsSaveAllowed`, a UI warning only);
3. a queued operation that never starts;
4. an operation that starts but does not finish.

**Diagnostics.**
- **Request line:** an observation-only prefix on `Game.SaveGame` logs one line per request (manual, quick or auto):
  - the type and name, and whether native `IsSaveAllowed` admits;
  - the refusing native term, if any: combat (with KMC's named reason), dialog, cutscene, game over, random encounter;
  - the turn-based and mode state;
  - the save, fence, draining, latch and loading state.
  It observes only calls that reach `Game.SaveGame`; a missing request line still requires checking the originating UI
  or quicksave path (stage 1).
- **Completion line:** a terminal "Mounted save completed" line records the archive and the commit count for the
  operation. It is written only after the scope's ownership, serialization suspension and save references are cleared,
  and a logging failure is isolated and counted (review of PR #31), so the diagnostic cannot leave saving or loading
  blocked.
- **Next owner log:** stage 2 shows as a request line with a refusing term; stage 3 as a request line with no
  deferral or snapshot after it; stage 4 as execution lines with no completion line.

## ENTRANCE APPROACH — cause established, repaired in source

**Cause (installed IL plus KMC source).**
- `AreaTransitionController.StartAreaTransition` builds the native `AreaTransitionGroupCommand` over
  `GetPartyCharactersForGroupCommand`: the directly controllable party units that can move, are not at death's door and
  share the navmesh area. A mounted horse is a member.
- It then calls `SelectAll`, whose `MultiSelect` KMC normalizes. That projects a mounted horse onto its rider.
- It gives each *selected* unit its own `UnitAreaTransition` through `RunUnitTransitionCommand`.
- The horse, still a group member, receives no command. The rider's own approach cannot move, because KMC disables the
  rider's agent while mounted.
- The group starts only once every member has acted or stands within 6 m of the transition, so it never starts.
- The other companions walk to their slots and wait. The mounted pair stays put.
- From close range the 6 m rule lets it pass. That is why crossing worked once the owner had walked there. Ordinary
  ground clicks are a different path (`ClickGroundHandler.RunCommand`, which KMC routes to the mount).

**Change.**
- In the `UnitCommands.Run` postfix, after native `Run` executed the rider's exact `UnitAreaTransition` while mounted
  and out of combat, the horse receives its native member command in the *same* group at the rider's slot. That is
  `new UnitAreaTransition(group, riderTarget)` with the rider's speed limit, the command native would have issued had
  the horse been selected.
- `MountedEntranceCarryPolicy` admits it only when all of these hold:
  - the rider's command is live, so a refused or replaced command carries nothing;
  - the group has not started or finished;
  - the horse is a native member, so it never becomes an extra arrival;
  - the horse has no live member command for this group already, so nothing is duplicated.
- **Unchanged:** the rider's own command is never replaced, held or completed by this change.
- **Native semantics kept:**
  - group arrival counting (each member once);
  - the 6 m rule;
  - the single dispatch;
  - a new order replacing a member command, which interrupts the whole group through `UnitGroupCommand.OnEnded`;
  - repeated clicks, which natively create a new group.
- **Untouched paths:** unmounted parties, the horse's own commands, ground moves, physical doors (`StandardDoor`
  routing), loot and the camp workflow.
- **Diagnostics:** one bounded line per carried entrance click (rider and mount ids, member count, transition, target
  and both distances) or the named reason no carrier was issued. The existing exploration observer still reports a
  rider-held `UnitAreaTransition` that stalls.

## Verification

**Offline (this branch).** Results: see "Checks".
- `SaveLoadEntranceRepairTests` runs 11 cases through the production policies:
  - a load during a prepared paired turn (the log's order) is a replacement, not travel;
  - a latch refuses travel only;
  - the archive worker, active save, queued save, charge fence and incompatible archive refusals are kept;
  - a load refused by a writer, a queued save or charge custody reads no archive, drains no charge and releases nothing;
  - retained charge cleanup on a secondary pair with no charge fence refuses the load until it settles, with a paired
    turn control;
  - the world charge drain runs once, after the save guards and before the selected archive;
  - travel keeps its terms, order and laziness;
  - classification follows `SaveInfo`;
  - the entrance carrier is issued once, is not applicable to unmounted parties, the horse's own commands, other
    commands or combat, and is refused for a dead command, a non-pending group or a non-member horse.
- `Test-PersistenceContracts.ps1` adds 9 installed-IL checks:
  - `LoadGame` passes the selected `SaveInfo` to `06000CD5`;
  - `SaveGame` checks `IsSaveAllowed` before `SaveRoutine`;
  - an entrance click builds its group from the party list, then calls `SelectAll`, then orders only the selected units;
  - each selected unit's command goes through `UnitCommands.Run`;
  - `SelectAll` goes through `MultiSelect`;
  - membership comes from the controllable party;
  - the 6 m start rule;
  - `OnEnded` interrupts or counts;
  - the public constructor and field the carrier uses.
- `Test-AssemblyContracts.ps1` lists the `UnitAreaTransition` constructor.
- **Detached adapter probes over the compiled DLL (review of PR #31).**
  - New `Test-WorldBoundaryChargeContracts.ps1` (8 checks). It drives the real `DecideSavedWorldReplacement` over two
    real `MountedCombatController` owners in a mounted world: primary clean; secondary retaining a charge owner
    (native partial terminal) with no fence. The load is refused (`ChargeCleanupUnresolved`) and retains the exact owner
    and command until the native terminal is observed, then admitted. An active save refuses before any drain. No
    unified-turn coordinator is bound, so consulting participation would throw.
  - `Test-PersistenceContracts.ps1` adds 7 checks. They drive the real `ReleaseSaveScope` with an `IModLogger` that
    throws: the release does not throw; the scope, its relationships' serialization suspension and the active,
    draining and releasing references are released; the completion is recorded once and the failed report counted;
    a following operation acquires and releases normally.

**Not covered offline (needs the game).**
- the in-session load end to end from a live turn-based paired turn: the `OnAreaBeginUnloading` cleanup, then the
  `WrapLoadRoutine` housekeeping that releases that participation. Out-of-combat in-session loads already used this
  order, but it was never reachable during combat before;
- the carried horse's approach and the group dispatch;
- KMC's mount movement handling under a native `UnitAreaTransition`.

**Native: NOT RUN (blocked).** Rechecked at intake:
- KMC is not installed (`Mods` has no KMC folder);
- the save root holds only protected human saves, with no KMC fixture and no disposable Ratnook save;
- the lab `runtime-state` holds only budgets.

Acceptance rows still required:
- an exploration manual save and quicksave before and after the Ratnook transfer, each committed and reloaded;
- a running-game load during a settled mounted combat turn, from the escape menu and from quickload, with and without a
  prior Ratnook traversal;
- the cold/main-menu control;
- a load refused while a save is writing;
- a distant Ratnook entrance click with the rider selected and with the whole party, then an unmounted control, a
  ground click and a physical door;
- repeated, replaced and canceled entrance orders;
- the combined sequence: approach → cross → remount → save → running-game load → move → entrance.

## Owner recovery until a fixed build

Load from the main menu: quit to the main menu, then load. This goes through the main-menu path, which was never
affected. Nothing indicates a corrupt save. Unsaved progress after the last successful save is not recoverable. To use
an entrance while mounted, walk the party next to it with ordinary clicks first, then click it.

## Independent review

A fresh-context reviewer read the first version of the patch read-only. Resolutions:

| Finding | Resolution |
|---|---|
| **F1 (P2/P1):** the first version ran `BeginLoadHousekeeping` in the `LoadArea` prefix while the previous load's scope (`restoreLoad`, archive state) was still in place. That re-armed combat restoration of the *previous* save onto the live outgoing world (saved native state replayed) before `LoadRoutine` began. | Admission no longer releases anything. Release stays in the established order, in which `WrapLoadRoutine` retires the previous scope before housekeeping (the reviewer's preferred fix). |
| **F2 (P2):** a cleanup failure at admission refused the load after partly tearing down the live combat world. | Resolved by the same change; nothing is torn down at admission. |
| **S2:** the entrance carry ran before the rider's own request ticket settled. | It now runs after `CompletePairCommandRun`. |
| **S3:** nothing verified that the `UnitAreaTransition` constructor assigns the group. | Contract check: `UnitAreaTransition..ctor` calls `UnitGroupCommand..ctor`, which stores `GroupCommand` (`04001A4A`). |
| **S4 (P3):** the save diagnostic misattributed a KMC refusal outside combat. | Corrected. |
| **P3:** tests covered policy branches production skipped. | Production now classifies through `MountedWorldBoundaryAdmissionPolicy.Classify`, and the entrance controller always decides through `MountedEntranceCarryPolicy`. |
| **S1:** the load could stop after admission. | Moot, since admission no longer releases anything. |

**Remaining limit (accepted).** The component tests are policy-level; the detached probes drive the compiled
admission decision and save release, but not `AdmitSavedWorldReplacement`'s frame binding, the native unload, the
`WrapLoadRoutine` housekeeping or the carrier against native `Run`, which need the game. No offline regression can drive
the F1 state, because it lives in Unity-side load scopes. The structural fix removes the call that caused it.

## Review of PR #31

A second reviewer read head `c1c652ce`. Resolutions:

| Finding | Resolution |
|---|---|
| **P2:** loading could bypass unfinished charge cleanup on a second pair. The replacement path checked only `AnyChargeFence`; `CanLoadBeforeWorldReplacement` drained only the primary pair; a pair can retain ownership without a fence, and `OnAreaBeginUnloading` cleanup cannot veto the unload. | The replacement decision drains every pair (`DrainWorldCharge`) after the save guards and before the selected archive and native unload; `CanLoadBeforeWorldReplacement` uses `AnyChargeFence` and the same drain. `BeginLoadHousekeeping` is still not called at admission. Regression: the two-controller probe above plus 2 component cases. |
| **P2:** the new completion log could leave saving and loading blocked if logging threw. `ReleaseSaveScope` marked the scope released, then logged inside `RecordCompletedSave`, before clearing suspension and save references. | `RecordCompletedSave` records state only, as before this PR. `ReportCompletedSave` writes the line after all ownership is cleared, inside an exception guard, and counts a failure. The new boundary admit and refuse lines (one is written after a transition began) are guarded the same way. Regression: the throwing-logger probe above. |
| **Report overstated the old log (saving).** Absence of execution lines does not exclude a save constructed or queued but never started. | Corrected above: saving is open, with the four stages the next investigation must separate. |
| **Report overstated the old log (loading).** The four refusal lines do not identify themselves as load attempts. | Corrected above: the source defect fits the report; the log does not prove each line was a load. |
| **Entrance:** no blocking defect; native execution outstanding. | No change. The acceptance rows below stand. |

## Checks

Budget `save-load-entrance`: 2.98 of 90 min.

| Run | Result |
|---|---|
| Fast, focused `Test-PersistenceContracts.ps1` and `Test-WorldBoundaryChargeContracts.ps1` (review corrections) | PASS 5/0 in 0.75 min: components 1158/0 (11 in this suite), Validate-Source 151/0, persistence contracts 214/0, world boundary charge 8/0 |
| Fast, focused `Test-PersistenceContracts.ps1` (first PR head) | PASS 4/0 in 0.59 min: components 1156/0, Validate-Source 151/0, persistence contracts 207/0 (including 9 new installed-IL checks) |
| `Test-ChargeOwnershipContracts.ps1` (pre-existing, not selected by Fast) | FAIL at its 7th pair-ownership check, "compiled two-pair native adapter refuses a third reservation without native mutation", **identically on the unchanged head `c1c652ce`** (rebuilt and rerun). The check predates this branch and stops the script before its later probes, so the review's two-controller probe was placed in its own script instead. Not modified here. |
| Earlier first-version runs | One build attempt failed on a contract-script variable collision (fixed); otherwise the same counts |
| Fast `-SkipBuild`, focused `Test-AssemblyContracts.ps1` (first version) | 758 PASS, 3 FAIL |

**Assembly-contract failures.** The 3 failures are the UnityModManager identity pins: 0.33.2 is pinned, this machine
has 0.33.0. They are environment drift, classified on the previous branch and unchanged here. The new
`UnitAreaTransition..ctor` row passes. The row list has not changed since that run.

**Build.** Local Release DLL from the review-corrected source: SHA-256
`cd244464537defd1732a61f20eb776ec8afc61aa828739ebdee7647b7607840f` (first PR head: `de2cffde…285e`). Not packaged; the
version stays 0.2.4.
