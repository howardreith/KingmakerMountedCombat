# Known issues remediation, Sprint 4 — scene continuity (C04, C01, C02, C03): handoff (2026-11-17)

Sprint 4 of the Mounted Combat Known Issues Remediation Epic, in the owner's order C04, C01, C02, C03. Offline work
only. Nothing was launched, staged or installed, and nothing in this record is native gameplay evidence.

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
- Faults are contained by the bounded fault report. Tracking is dropped when the pair is not mounted, and a new
  world has new unit objects.

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
| Manual dismount, then remount | Correct while dismounted | Tracking dropped and rebuilt |

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

### What would unblock it

A native participation contract for taking a pair out of a running encounter with retained intent: capturing and
restoring an unsettled activation without granting or refunding anything. That is a design decision for the owner,
not a Sprint 4 change.

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
  custody, or an evaluation outside combat, starts afresh. The notice is a routine player report naming the pairs.
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

## Checks run (budget `known-issues-sprint4`, 0.37 of 90 minutes recorded by the tier runner)

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
| C01 | The owner's decision on a native participation contract for an in-combat handoff; until then, unsupported |
| C03 native | A staged pair entering combat, with the single report and the post-combat remount |
| M01–M03, M05 native | Unchanged from Sprint 3 |
| M04 | Original report unresolved (unchanged) |
| S01, T04–T08 | Unchanged |

## Not authorized and not done

Merge, release, tag, version bump, permanent installation, loader replacement, reviewer request, launching the game.
Work stops at this review checkpoint.
