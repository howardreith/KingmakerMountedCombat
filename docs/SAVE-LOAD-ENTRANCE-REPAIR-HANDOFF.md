# Save/load and entrance approach repair — review handoff (2026-11-12)

**Status: RUNNING-GAME LOADING AND ENTRANCE APPROACH REPAIRED IN SOURCE, WITH OFFLINE AND INSTALLED-IL CONTRACT CHECKS;
SAVING DIAGNOSED AS NOT REPRODUCED IN THE EVIDENCE (BOUNDED DIAGNOSTICS ADDED); NATIVE NOT RUN (BLOCKED); OWNER
ACCEPTANCE SEPARATE.** No merge, release, tag or install.

- **Branch:** `codex/kmc-save-load-entrance-repair`, from main `bcc176ec` (v0.2.4; `origin/main` fetched at intake and
  unchanged). Draft PR [#31](https://github.com/howardreith/KingmakerMountedCombat/pull/31); repair commit `07468efb`.
- **Owner report (0.2.4):** "Alright got through the door OK, but now saving and loading a game are completely broken.
  Also clicking on the entrance to the door does not result in the party moving toward it."
- **Evidence:** `output_log(3).txt` (2,357,817 bytes, SHA-256 `da0f9578…2729`, 10,916 LF lines). It reports KMC 0.2.4.
  The owner's DLL was not byte-matched. The log is kept local.
  - 10847–10858: the same-area teleport repair ran exactly as designed (begin, suspension, local teleport, native
    placement, remount).
  - 10911: a turn-based paired activation was prepared (combat).
  - 10912–10916: four "Area change refused" lines.
  - No save request, save deferral, save failure or snapshot line appears after the door.

## RUNNING-GAME LOADING — cause established, repaired in source

**Cause (installed IL plus KMC source).**
- `Game.LoadGame` (`06000CE0`) runs `PrecheckSaveCorruption`, then synchronously calls the full `Game.LoadArea`
  (`06000CD5`) with the selected `SaveInfo`.
- KMC's `AreaTransitionPrefix` applied the exploration *travel* guard (`AdmitChargeAreaTransition`) to every call before
  looking at `saveInfo`.
- In a combat turn, `TrySettleExplorationParticipation` returns false as soon as an actor is in combat, so the guard
  refused the load with exactly the logged message.
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
    save admission, a charge fence, or the selected archive's own admission (`CanLoadBeforeWorldReplacement`, bound to
    the exact descriptor `Game.LoadGame` admitted in the same frame and re-run otherwise).
- **Admission releases nothing.** The outgoing world is released by the established load path:
  - the native unload's `OnAreaBeginUnloading` cleanup;
  - then `WrapLoadRoutine`, which retires the previous load scope and then calls `BeginLoadHousekeeping`;
  - housekeeping releases participation without End or forfeit, and clears the latch only after cleanup succeeds.
- A refused load changes nothing.
- Each in-session load logs one bounded admit or refuse line. It carries the operation, the entry, the selected and
  outgoing campaign and area, the binding, the combat, turn-based and activation state, and the transfer, latch, save
  and charge state.

## SAVING — no failing stage in the evidence; diagnostics added

**Traced path (installed IL).**
1. Quicksave (`MakeQuickSave`) and the save window go through `Game.SaveGame` (`06000CE4`).
2. `SaveGame` refuses through `IsSaveAllowed`, with a UI warning only.
3. `IsSaveAllowed`'s combat term is KMC's `NativeCombatBlocksSave`.
4. Then `SaveRoutine` (KMC `SavePrefix` → `WrapSaveRoutine` → deferred save).

**Findings.**
- With the shipped preset (paired activation on, unified turn and scheduler off), KMC's combat gate admits a turn-based
  combat save.
- `CombatRestorationPending` could not have been true: it refuses every command, and the party was moving and casting.
- A request reaching `SaveRoutine` always logs something: the deferral notice, the snapshot or serialization lines, or
  a failure report. None appears after the door.
- Ruled out as a cross-mod cause: KingmakerGunslinger's production source at 0.0.184 does not patch the save gate. Only
  its test harness mentions it.
- A refused load does not leave native loading state that disables saving.

**Conclusion.** No save request reached KMC's save routine in this session. It was either not attempted, or refused by
native `IsSaveAllowed`, which leaves no log. Nothing in source establishes a KMC save defect, so no save behaviour was
changed.

**Diagnostics.**
- **Request line:** an observation-only prefix on `Game.SaveGame` logs one line per request (manual, quick or auto):
  - the type and name, and whether native `IsSaveAllowed` admits;
  - the refusing native term, if any: combat (with KMC's named reason), dialog, cutscene, game over, random encounter;
  - the turn-based and mode state;
  - the save, fence, draining, latch and loading state.
- **Completion line:** a terminal "Mounted save completed" line records the archive and the commit count for the
  operation.
- **Next owner log:** these lines will say which stage refuses or stalls.

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
- `SaveLoadEntranceRepairTests` runs 9 cases through the production policies:
  - a load during a prepared paired turn (the log's order) is a replacement, not travel;
  - a latch refuses travel only;
  - the archive worker, active save, queued save, charge fence and incompatible archive refusals are kept;
  - a load refused by a writer, a queued save or charge custody reads no archive and releases nothing;
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

**Remaining limit (accepted).** The tests are policy-level. The integration wiring (descriptor binding,
`WrapLoadRoutine` housekeeping, the carrier against native `Run`) needs the game. No offline regression can drive the
F1 state, because it lives in Unity-side load scopes. The structural fix removes the call that caused it.

## Checks

Budget `save-load-entrance`: 1.6 of 90 min.

| Run | Result |
|---|---|
| Fast, focused `Test-PersistenceContracts.ps1` (final source) | PASS 4/0 in 0.59 min: components 1156/0 (including 9 new), Validate-Source 151/0, persistence contracts 207/0 (including 9 new installed-IL checks) |
| Earlier first-version runs | One build attempt failed on a contract-script variable collision (fixed); otherwise the same counts |
| Fast `-SkipBuild`, focused `Test-AssemblyContracts.ps1` (first version) | 758 PASS, 3 FAIL |

**Assembly-contract failures.** The 3 failures are the UnityModManager identity pins: 0.33.2 is pinned, this machine
has 0.33.0. They are environment drift, classified on the previous branch and unchanged here. The new
`UnitAreaTransition..ctor` row passes. The row list has not changed since that run.

**Build.** Local Release DLL from the final source: SHA-256 `de2cffde2ccbad6d4f604925ebdc01401988bbe43a6f8b5c7c18778933e4285e`. Not packaged; the version stays 0.2.4.
