# Ratnook Hill door repair — review handoff (2026-11-12)

**Status: SOURCE REPAIR WITH OFFLINE REGRESSIONS AND INSTALLED-ASSEMBLY CONTRACT CHECKS; NATIVE RATNOOK ACCEPTANCE NOT
RUN (BLOCKED: NO KMC FIXTURE OR DISPOSABLE SAVE ON THIS MACHINE); OWNER ACCEPTANCE SEPARATE.** No merge, release, tag
or install.

- **Branch:** `codex/kmc-ratnook-door-repair`, from main `44b6f606` (the v0.2.3 release merge; `origin/main` was
  fetched at intake and was unchanged). Draft PR: see the PR body.
- **Owner report:** "Just buffed and went through a door at Ratnook hill. On going through the door, my rider dismounted
  and now the party can't move." The supplied log (`output_log(2).txt`, 2,431,057 bytes, SHA-256 `42c16ec9…365d`,
  11,484 LF lines) reports KMC 0.2.3. The owner's DLL was not byte-matched to the release. The log is kept local and is
  not committed.

## Established cause

**Native route (installed-assembly contract, Kingmaker SHA-256 `3b6450ff…5afb`, MVID `07fa1e4d-…`).**
1. `AreaTransitionGroupCommand.ExecuteTransition` (`0600263E`) is the only raiser of `IPartyLeaveAreaHandler`. It
   calls `Game.LoadArea(BlueprintAreaEnterPoint, AutoSaveMode)` (`06000CC9`) synchronously, with no branch in between.
2. `06000CC9` compares `CurrentlyLoadedArea` with `enterPoint.Area`. An enter point of the loaded area goes to
   `Game.Teleport` (`06000CCB`) and returns. Only another area reaches the full `LoadArea` (`06000CD5`), the one KMC
   patched.
3. `Game.Teleport` queues the `TeleportParty` loading process. It places the party (`Player.MoveCharacters`
   `06000DCC`, the same `AreaEnterPoint.PositionCharacters` placement a full load uses), then raises
   `ITeleportHandler.HandlePartyTeleport` (`06007512`). It never unloads scenes and never queues `OnAreaLoaded`
   (`06000CD7`). `OnAreaLoaded` is queued only by `06000CD5` and the smoke-test loader.

**Log evidence for this route (lines are physical LF lines).**
- 11460–11462: the door's `UnitAreaTransition`, then suspension.
- Every earlier full load in the same process (five) logs:
  - Buff Planner `OnAreaBeginUnloading`;
  - Unity's "Unloading N Unused Serialized files";
  - `OnAreaLoadingComplete`.
- The final transfer has none of these, and KMC logged no "Area change refused". Party-leave comes only from a door.
  Elimination therefore leaves the same-area `Teleport` branch.
- Remaining uncertainty: the log does not name the door or its enter-point blueprint. No native run reproduced it.

**KMC defect (0.2.3).**
- `HandlePartyLeaveArea` began and suspended the transfer. Restoration was attached only to `OnAreaLoaded`.
- After `TeleportParty` finished, the queue went idle. `CompleteAreaTransitionIfReady` found `ResumeAttempted == false`
  and latched `areaRestoreFailure`.
- `CombatSemanticRestorationPending` made the global `UnitCommandRunPrefix` refuse every unit's command, so the whole
  party was locked. Saving was blocked by the same condition.
- The new Mount request at 11476–11483 reached `CastRequested` and was then refused the same way.

**Secondary hypothesis, ruled out as cause.** Suspension's `InterruptMove` on the rider's `UnitAreaTransition` cannot
cancel the dispatch. Party-leave is raised inside `ExecuteTransition`, which then calls `LoadArea` unconditionally
(contract check below). Command interruption was not changed.

## Change

The transfer now records its observed native route in `Domain/MountedAreaTransferLifecycle.cs`. That is pure state, and
`MountedAreaPersistence` delegates every decision to it.

| Native observation | Hook | Effect |
|---|---|---|
| Party-leave | `IPartyLeaveAreaHandler` (existing) | Transfer `Requested` (enter point, departure area and sequence recorded), then suspension as before |
| Full load admitted | `LoadArea 06000CD5` prefix (existing) | Route `FullLoad`. A newer full load supersedes a pending teleport. A transfer that already attempted restoration is finished, so the newer load starts its own transfer instead of inheriting it. |
| Local teleport dispatched | **new** exact prefix `Game.Teleport 06000CCB` (observation only, faults logged) | Route `LocalTeleport`, only for an enter point of the transfer's destination that is the loaded area |
| Local placement | **new** `ITeleportHandler.HandlePartyTeleport` in `MountedLifecycleSubscriber` | The existing all-pair `RestoreAreaPair`, once, after suspension, and only when no other `TeleportParty` is still queued, so the last queued teleport is the placement |
| Full placement | `OnAreaLoaded` postfix (existing) | `RestoreAreaPair` only for a `FullLoad` transfer |
| Idle loading queue | `CompleteAreaTransitionIfReady` | Restored: release. Suspended and never dispatched: restore in place (below). Dispatched without its placement, or unsuspended and unresumed: the **unchanged** protective latch. |

**Restoration.** Restoration is the existing `RestoreAreaPair`. It keeps the pair, profile, owner id and generation,
participation, hotbar slots and placement markers, and its eligibility, carried-intent, combat-arrival and capital-retirement
rules. The pair stands where native placement put it. Nothing replays commands, grants or refunds resources, or
prepares turns. An unmounted party creates no transfer, so nothing mounts.

**Never-dispatched transfers.** The dispatch is synchronous with party-leave. A suspended transfer with no dispatch at
an idle queue is therefore a refused transition, for example KMC's own full-load admission refusing. The party was not
moved, so the transfer restores through `RestoreAreaPair` in the departure area of the same world.
- It first waits, on state rather than a timer, while a save scope or fence, save draining or suspension, or a charge
  fence is active.
- If a restore failure is already latched, it leaves the pairs dismounted and does not replace that failure's custody
  record.
- Anything else keeps the latch.

**Protection kept.**
- `areaRestoreFailure`, the global command guard, save blocking and load housekeeping are unchanged.
- A restoration that leaves owned state still latches inside `RestoreAreaPair`.
- A dispatched route that idles without its placement (`StopAll` or an exception) still latches, because native state
  is unknown.
- No timeout authorizes a remount.

**Diagnostics.** Each transfer logs at most four bounded lines: begin, route, placement, and outcome. Each line
carries the sequence, the departure and destination area, the enter point, the pair count, the phase flags and the
same-world check.

## Verification

**Offline (this branch).**
- `RatnookDoorRepairTests` drives the production lifecycle through 12 native callback orders:
  - the local door and the 0.2.3 incident order;
  - dispatched-without-placement, which stays protective;
  - refused before dispatch, which restores in place once;
  - a cross-area door and a same-area forced reload, which complete only at `OnAreaLoaded`;
  - placement before suspension;
  - duplicate callbacks;
  - a foreign teleport that was current at the door's dispatch, and two doors before the first teleport ran;
  - a newer full load superseding a teleport;
  - an attempted restore, which finishes without clearing its own latch;
  - no assumed local route.
- `Test-PersistenceContracts.ps1` now reads the exact installed IL. It checks the party-leave → `06000CC9` ordering,
  the `Teleport` / full-load branch, that `Teleport` queues no full load or `OnAreaLoaded`, and that `TeleportParty`
  places the party before `HandlePartyTeleport` under the product's exact process-name constant, that
  `HandlePartyTeleport` has a single native caller, and that `QueuedNames` excludes the current process.
- `Test-AssemblyContracts.ps1` lists the three new exact members.
- Results: see "Checks" below.

**Not covered offline.** The Unity-side wiring needs the game:
- `RestoreAreaPair` inside `HandlePartyTeleport`;
- Harmony installation of the new prefix in the live process;
- the two-pair, unmounted-party and capital cases at a local door.

**Native: NOT RUN (blocked).**
- KMC is not installed on this machine.
- The save root holds only protected human saves: no `KMC_AUTOMATION_BASELINE`/`WORKING` fixture and no disposable
  Ratnook Hill save.
- The lab `runtime-state` holds only budgets.
- Nothing was staged, deployed or launched.
- Ratnook acceptance needs, in an owner-authorized disposable save:
  - the actual door through normal interaction, and its return route if distinct;
  - mounted and unmounted parties, each with and without the buff routine;
  - after arrival: a group move, an unrelated companion move, an ordinary interaction, another transition,
    save/reload, and local → world map → local travel.
- Each run must record the candidate build, the fixture, the four transfer trace lines and the observed outcomes.

## Independent source review — resolution

A fresh-context reviewer read the patch read-only. It found no P1 defect reachable on the normal Ratnook path.

| Finding | Resolution |
|---|---|
| P2: a foreign teleport that is the *current* process when the door dispatches, or a second door before the first teleport runs, could restore before the final placement. `QueuedNames` excludes the current process. | Placement now waits while any `TeleportParty` is still queued, so the last teleport is the placement. The foreign-ahead count is removed. Two regressions cover the reviewer's sequences. A new contract check proves `QueuedNames` reads only `m_Queue` by coroutine type name. |
| P2: restore-in-place ignored the save, charge and latched-failure conditions that refused the load | Corrected as above: it waits for save and charge boundaries, and never overwrites an existing latch. |
| P3: contract checks were weaker than their messages | The checks are now opcode-based: no branch between party-leave and `06000CC9`; `Teleport` exactly on the `CurrentlyLoadedArea == enterPoint.Area` branch; `HandlePartyTeleport` has a single native caller, the `TeleportParty` delegate (byte scan of the whole assembly); `QueuedNames` semantics. |
| P3: `scripts/runtime/Chunk7RepeatedWorldEvidence.ps1` hard-codes 13 lifecycle subscriptions | Already stale on main (14 with `IUnitFallProneHandler`). `ITeleportHandler` makes 15. It is a reader of recorded native evidence, so it is **deferred** to the next repeated-world qualification rather than retuned without a native run. |
| P3: tests cover only the pure lifecycle | Accepted. The integration guards (departure area and world match, `WorldMap` reset, destination-area match, and the new fall-through after an attempted restore) run only in game. One assertion on a path the integration never takes was removed. |

The reviewer also raised two pre-existing items, not changed here:
- A full load to *another* area while a transfer is suspended cancels it, and its pairs stay dismounted without intent.
- A teleport whose `MoveCharacters` throws before `HandlePartyTeleport` still latches, failing closed.

## Separate issue left open

**Arrival view check (log 11213–11214).** After the successful full-load restoration at 11193, the invariant check
reported rider and mount views inactive in `Default` mode, which caused a `CompanionInvalidated` dismount. The owner
remounted manually. That is the full-load path at `OnAreaLoadingComplete` and involves no teleport, so this repair does
not address it. It needs its own investigation of view activation timing after a full load. Inactive-view protection
was not weakened.

## Owner workaround until a fixed build

Load a save from before the door, dismount, and cross unmounted. Successful load housekeeping clears the latch. This
follows from the source and is not verified in the supplied session.

## Checks

Budget `ratnook-door`: 1.73 of 90 min spent.

| Run | Result | Duration |
|---|---|---|
| Fast, focused `Test-PersistenceContracts.ps1` (before review) | PASS 4/0: components 1146/0, Validate-Source 151/0, persistence contracts 195/0 | 0.73 min |
| Fast `-SkipBuild`, focused `Test-AssemblyContracts.ps1` (before review) | 756 PASS, 3 FAIL | short |
| Fast, focused `Test-PersistenceContracts.ps1` (after review corrections) | PASS 4/0: components 1147/0 including 12 Ratnook cases, Validate-Source 151/0, persistence contracts 198/0 | 0.77 min |

**The three assembly-contract failures** are the UnityModManager identity pins at `Test-AssemblyContracts.ps1` lines
336–338, which this branch does not change:
- the script pins 0.33.2.0, SHA-256 `f6f7e18a…`;
- this machine has 0.33.0.0, SHA-256 `63e5baf7…`, matching the owner's reported UMM 0.33.0.

They are environment drift, not a regression. The new exact rows `06000CCB`, `06007512` and `0600263E` pass.
`Test-AssemblyContracts.ps1` was not rerun after the review corrections because its list did not change.

**Build.** Local Release DLL from the final source: SHA-256 `71053ef323121c729276ce3697565dc93833b57e69ff7da9f1cfa65e3497676b`. It was not packaged, and the version stays
0.2.3.

**Deferred.**
- Candidate tier.
- The repeated-world reader update.
- All native rows.
