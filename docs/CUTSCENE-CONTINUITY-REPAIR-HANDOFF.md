# Cutscene continuity repair — handoff (2026-11-12)

Branch `codex/kmc-cutscene-mounted-continuity` from main `0754c44a` (release 0.2.7). Mission: the owner's report that
mounted characters dismount whenever a cutscene begins. The owner's requested behaviour supersedes the exploration
sprint's decision to suspend every pair for Dialog/Cutscene.

## Bald Hilltop Mount lockout (added to this PR by owner request, 2026-11-12)

**Status:** an offline repair candidate awaiting native confirmation. The cause is established from installed-assembly
contracts plus elimination. No runtime log came with the report, and Bald Hilltop was not tested in game: this
machine has no KMC install or fixture.

**Reported state:** at Bald Hilltop, during apparently ordinary exploration, Mount/Dismount is disabled and the only
refusal is "Mounting is blocked during loading, area transitions, and cutscenes."

**Refusing condition.** The 0.2.7 `MountedPlayerActionController.IsLoadingOrCutscene` refused when any of these held:
- loading;
- `game.CutsceneLock`;
- `rider.CutceneControlledUnit != null` or `mount.CutceneControlledUnit != null`;
- mode `Cutscene` or `CutsceneGlobalMap`.

**Native contracts (installed 2.1.7b, MVID `07fa1e4d…`):**
- `UnitEntityData.CutceneControlledUnit` (field 0x040054D7) is not serialized (no `JsonProperty`).
- The only store to it in the whole assembly is the lazy creation in `CutsceneControlledUnit.MarkUnit` (0x06009D6B).
  - The ILSpy full IL listing has exactly one `stfld` of the field.
  - The new contract pin scans 51,280 method bodies by token and finds the same single store.
- Nothing ever sets it back to null. `Release` only removes entries.
- After any cutscene command has once controlled the rider or horse (an arrival, story or bark scene), the holder
  therefore stays for the rest of the session, and the old predicate refused Mount until the game was reloaded.
- The game's own notion of current control is the active entry: `GetCurrentlyActive` and `GetControllingPlayer`
  (0x06009D6E reads `m_Active`). The movement agent and animation state use exactly that. A finished
  (`CutscenePlayerData.IsFinished`) or destroyed player is not control.

**Other inputs eliminated:**
- A held `CutsceneLock` keeps a non-Default mode. `CutsceneUIHideController.Activate` stops the Cutscene mode only while
  the lock is clear (pinned). That would add the separate "active world view" refusal and remove player control, and
  the screenshot shows neither.
- The Cutscene modes would add the same world-view refusal.
- Loading is implausible during ordinary exploration.

The holder is the only input consistent with the screenshot. It falls under category 3/4 of the mission: an idle,
never-released association that the mod interpreted as active control.

**Change:**
- `NativeSceneControl.IsUnderLiveSceneControl` is one shared reading, used by both this repair and the continuity
  work: the holder, plus an active entry, plus a live, unfinished cutscene player. The decision itself is
  `MountedSceneContinuityPolicy.ActorUnderLiveSceneControl` and `BlocksPlayerTransition`.
- `IsLoadingOrCutscene` uses this reading. It feeds Mount availability, queued and executing delivery (the same
  `GetAvailability` context) and Dismount availability, so the tooltip and execution cannot disagree.
- Loading, the lock and the cutscene modes still refuse. Live scene control of this exact rider or horse still refuses.
- No native field is cleared or written. There is no area whitelist and no timer. Queue (Pause) and execution (Default)
  admission, combat eligibility and the Move cost, `NativeAdmissionRefusal` and retained ownership are unchanged.
- **Diagnostic:** one line per change of the refused reason set (not per poll). It logs:
  - the action and every reason;
  - the area name and GUID, mode, loading flag and lock;
  - for rider and horse: id, holder, active entry, cutscene name, finished, paused, live;
  - controllability.

**Checks:**
- Fast PASS 3/0: components 1190/0 (4 new `BaldHilltopMountAdmissionTests` through the real
  `MountedPlayerActionEvaluator`), Validate-Source 151/0.
- Mutation check: restoring the old holder-presence reading fails all 4.
- Kingmaker contracts 784/3, with 3 new pins (transient holder, single writer, lock-coupled mode stop). The 3
  failures are the pre-existing UMM identity pins.
- **DLL:** SHA-256 `47053c0c5ba848a236582ad28e6a933707b8b9f1e49ae5a4caf01d52429fe6be`, MVID
  `7797eda3-46f5-440b-ace1-b165cc9901e9`. This supersedes the identities below.

**Native: NOT RUN.** In order, check:
1. At Bald Hilltop, Mount is available and completes.
2. Mount is refused while a cutscene controls the rider or horse, and recovers after the cutscene completes or is
   skipped.
3. Paused queueing, then execution after resume.
4. Combat Mount still costs a Move action.

If the owner's next log still shows the refusal, the new diagnostic line names the refusing input.

**Uncertainty and remaining limits:**
- The specific Bald Hilltop scene that first marked the rider or horse is not identified.
- A *paused* cutscene that still has an active entry keeps refusing (conservative native meaning).

## Self-review corrections (2026-11-12, after the first PR revision)

A high-effort review of PR #37 found 10 issues. All are corrected in source; none is demonstrated in game. Where this
section differs from the sections below, this section is current.

| Finding | Correction |
| --- | --- |
| The regroup deadline used real time, so a pause or a holding dialog abandoned the walk. | It runs on native game time (`TimeController.GameTime`), which stops in Pause. |
| The regroup walk was not stopped when the mount arrived, so it kept walking with the rider seated. | Arrival stops the walk before restoration is decided. |
| A timed-out first walk exhausted the regroup. | The timed-out attempt is spent; the second walk is still offered (at most 2). |
| A scripted command on an attached pair actor went through player-order routing, so a scripted stock attack of the mount was refused and the scene stalled. | Scripted scene operations (mount commands, in-combat rider commands, refused handoffs) run natively as the scene authored them. |
| A pair a scene kept controlling retried forever. | Staging patience: 60 s of native game time in settled ordinary play, then the intent is released with "a scene kept control of the pair". |
| A staged pair could join pending camp or world-map custody and inherit its rules. | Scene staging is refused while camp or world-map restoration is pending; the pair stays attached. |
| A scene's direct `Position` write of a seated rider (`TranslocateUnit`, `CommandTeleportParty`) was snapped back; 0.2.7 applied it. | The synchronizer holds (writes nothing) when, during a scene, the rider's own position moved more than 3 m from the position it last wrote and from the saddle. The pair is then handed off at the scene's placement. A refused handoff releases it back to the saddle. A mount-only teleport moves the saddle, and the rider follows as before. |
| Every unit's `Run` scanned relationships. | Player and AI orders leave first. |
| The Rest/ScriptedEvent ternaries were dead code. | Simplified. |
| `ResetRetainedSettle` allocated every held frame. | It no longer allocates. |

**Checks after the corrections:**
- Fast PASS 3/0: components 1186/0 (3 new regressions), Validate-Source 151/0. Budget `cutscene-continuity`: 1.87 →
  about 2.6 of 90 min.
- Kingmaker contracts 781/3 (the same pre-existing UMM identity failures).
- New DLL SHA-256 `3426baf780fab4d258233397a2c8920597dbfe48bc588f9b565bb528f976317d`, MVID
  `021cb4ee-e9a5-4043-9f89-f638447b101c`. This supersedes the identity in the last section.
- Native: still NOT RUN.

**Remaining unsupported after the corrections:**
- In combat there is still no staging handoff; the scene's command runs natively on the seated rider.
- A scene teleport of the rider within 3 m of the saddle is kept on the mount.
- A pair still staged at combat start fights unmounted, then remounts afterwards.

## Evidence classes

| Class | What it establishes |
| --- | --- |
| KMC source at `0754c44a` | The three dismount paths below. |
| Installed Kingmaker 2.1.7b `Assembly-CSharp` (MVID `07fa1e4d-8618-41b3-9b8d-faa17d3b26f7`), decompiled to scratch only | The native scene boundaries below. The ones the code relies on are pinned in `scripts/Test-AssemblyContracts.ps1`. |
| Offline checks on this branch | Policy and coordinator behaviour, contract pins and a mutation check (below). |
| In game | **Nothing.** No cutscene log or reproduction came with the report, and the owner's occurrence has not been reproduced. This machine has no KMC install, no KMC Baseline/Working fixture and no `runtime-state\fixture-qualification.json`. Nothing was staged, launched or restored. |

## Cause (source; not demonstrated in game)

1. **Mode-start suspension.** `MountedLifecycleSubscriber.ObserveOrCleanupGameMode` called `TrySuspendForEvent` for
   Dialog, Cutscene and Rest. A successful suspension physically dismounts every pair. A refused one (combat, save scope)
   falls back to a clean `GameModeBoundary` dismount, so a scene during combat lost the pair outright.
2. **Second path.** `MountedGameModePolicy.Classify` put Dialog/Cutscene in `CleanDismount`.
   `KingmakerMountedPairRuntime.ValidateMountedInvariants` rejected the mode, and so did its view-activity check.
   `GameMountedRelationshipService.ValidateActivePair` turned either rejection into a direct dismount.
3. **Combat cancellation.** `MountedRetainedIntentPolicy.Decide` cancelled every retained pair when party combat began
   ("combat began"). As a result, hostile dialogue left the pair dismounted.

## Native boundaries (installed 2.1.7b)

- **Input lock only.** `Game.SetCutsceneLock(true)` (0x06000CE6) only calls `StartMode(Cutscene)`, and
  `CommandLockControls.OnRun` reaches it. The lock also interrupts non-cutscene commands of controllable characters.
  `DialogController` owns `Dialog`. Neither mode itself moves, hides or replaces a unit. `TurnBasedCombatController` and
  `UnitMoveController` keep running in `Cutscene`; `UnitCombatJoinController` runs only in `Default`, so a dialogue's
  fight begins after the scene ends.
- **Staging.** A scene stages a unit only through its own operations:
  - Cutscene commands call `UnitCommands.Run` (0x060026B2): `CommandMoveUnit`, `CommandMoveParty` (party characters
    before pets), `CommandMoveByPath`, and the animation, attack and cast commands.
  - Dialog and script actions (`RunCommand`, `AttackCommand`) call the same `Run`.
  - `TranslocateUnit`, `CommandTeleportParty` and timeouts write `Position` directly.
  - Hide-style actions clear `IsInGame`.
- **Ownership.**
  - Every `UnitCommand` constructor records `Cutscene` (backing field 0x04001A84) when it is built inside a cutscene's
    parameter context.
  - `CutscenePlayerData` marks a command's controlled unit (`CutsceneControlledUnit.MarkUnit`/`ReleaseUnit`), and
    `GetControllingPlayer` (0x06009D6E) names the playing cutscene.
  - `CutscenePlayerData.Stop`/`ForceStop` (skip, abort) stop the command and release the unit without any extra
    callback. A cutscene paused because its unit is in combat has no active controller.

## Change

**Preserved attachment (ordinary scenes).**
- `MountedGameModeDisposition.PreserveScriptedScene` covers Dialog and Cutscene. The relationship is retained and views
  may be hidden while the scene holds the camera.
- `CanAdmitMountedAction` and `CanQueueMountedAction` are unchanged (Default only; Default or Pause), so attachment and
  gameplay input stay separate decisions.
- Identity, reciprocal companion, life state, `IsInGame`/controllability, movement authority, mount agent, AI lease and
  presentation validation all still apply.
- Only Rest still suspends at its mode boundary. `CutsceneGlobalMap` keeps its clean-dismount boundary.

**Staging handoff (pair-local), `MountedSceneContinuityPolicy` and `MountedRetainedIntent.cs`:**
- **Scripted command on the rider.** The `UnitCommands.Run(UnitCommand)` prefix runs before any pair routing. A command
  on the exact rider counts as scripted when it is not a player order, not AI and not KMC's own, and any of these holds:
  - it carries the native `Cutscene` flag;
  - a scene is active (cutscene lock, or Dialog/Cutscene on the mode stack);
  - a cutscene controls the rider.

  Such a command suspends only that pair (`GuardBoundary(EventSuspension)`, rider placed beside the mount) into retained
  custody. The custody keeps the exact rider and mount ids, owner id, generation and campaign/area. Native `Run` then
  executes the scene's command on the free rider, so its completion conditions and story effects run normally and the
  synchronizer no longer writes the rider's position.
- **Scripted command on the mount.** It runs natively, and the mount carries its seated rider.
- **Pair member taken out of the game.** At `IInGameHandler.HandleObjectInGameChaged`, a living, still reciprocal pair
  member that a scene takes out of the game is handed off the same way instead of being invalidated.
- **During an encounter:** no handoff. The attachment, paired participation and every turn resource stay as they are.
- **Refusals.** A handoff is refused when a save, area transfer, load restoration, unsettled participation or charge
  cleanup owns the pair. The pair then stays attached, and one warning names the reason.

**Restoration** (the existing saved-ownership import plus `RestoreSavedPair`, unchanged):
- **Readiness** is read per pair from native state on every evaluation:
  - a live controlling cutscene;
  - an unfinished `Cutscene`-flagged command in a slot;
  - out of the game while a scene is active.

  Because readiness is read from native state, nested scenes are covered, a brief intermediate Default restores nothing
  while staging is owned, and a skip or abort needs no paired end callback. Each pair has its own patience. Restoration
  still needs `Default` (the candidate's `SafeMovementMode`) and the existing 5-frame / 0.3 s settle.
- **Combat** no longer cancels scene-staging intent (`ScriptedEvent` kind). The intent holds through the encounter,
  and any regroup walk is stopped so it spends nothing of the mount's turn. The pair restores after the encounter with
  no Mount action, preparation, refund or replay. Camp and world-map custody keep the cancellation.
- **Placement.** The scene's final placement is respected, and the 6 m restore separation is unchanged. A mount left
  behind within 30 m on the same navigation area walks to its rider with a native `UnitMoveTo(rider, 1.5 m)` (not a
  player order). This happens out of combat only, at most twice, 20 s each. Otherwise the pair stays dismounted and the
  log gives a specific reason.
- **Persistence.** No new member, field or encoding. Staged intent is saved in the existing `kmc-retained-intent`
  member as `ScriptedEvent`. A 0.2.7 save that holds a dialog suspension restores under the new rules. Regroup state is
  transient and is not saved.

**Diagnostics** (one line per boundary, never per frame):
- Attachment preserved through Dialog or Cutscene start or end, with pair ids.
- A scripted mount command carried with its rider seated.
- A handoff, with the pair, owner/generation and native boundary.
- An in-combat or refused handoff, with the reason.
- An encounter hold, regroup start and stop, the restore (pair, owner, boundary, regroup count) and a refused restore
  (pair, boundary, separation, reason).

**Untouched:**
- the horse group-pace carry;
- ground-command routing;
- attack, charge, door, trap and entrance handling;
- paired turn ownership and action accounting;
- `UnifiedMountedTurnCoordinator`;
- the Rest, world-map, area-transition and save/load paths.

## Checks actually run

- **Fast tier** (`Test-Tiers.ps1 -Tier Fast`, budget `cutscene-continuity`): PASS 3/0, 37 s.
  - Components: 1183/0.
  - Validate-Source: 151/0.
  - Two earlier attempts failed and their fixes are on the branch. The first was a test compile error. The second was
    the new regression finding a real gap: a regroup with a non-finite separation kept waiting, and the policy was fixed.
  - Spend: 1.12 of 90 min.
- **New regressions** (`tests/CutsceneContinuityTests.cs`, 8 cases). These drive the production policies with the real
  `MountedRelationshipCoordinator` and a counting runtime, and cover:
  - ordinary dialogue and input-lock scenes;
  - scripted rider and mount commands;
  - staging release and regroup;
  - dialogue into combat;
  - a scene during combat;
  - nested, skipped and aborted scenes;
  - unmounted, invalidated and multiple pairs;
  - regroup bounds.

  Existing tests that encoded the superseded decision were updated: `ExplorationRepairTests` (event modes, combat gate)
  and `MountedStabilizationPolicyTests` (Dialog/Cutscene).
- **Mutation check.** Restoring the old blanket decisions (scene modes suspend and are CleanDismount, combat cancels)
  fails 5 regressions. The committed source passes 1183/0.
- **Kingmaker assembly contracts** (`Test-AssemblyContracts.ps1 -Target Kingmaker`): 781/3. All 16 new scene pins pass.
  The 3 failures are the pre-existing UnityModManager identity pins recorded in the horse group-pace handoff.
- **Limits.** The integration glue (the Run prefix, the in-game handler and the restore loop) compiles against the
  genuine references but is not executed offline. No focused persistence or area-transition script was run: the archive
  record and area code are unchanged apart from transient fields on the in-memory pair.

## Native scenarios: all NOT RUN

Blocked: no KMC install, fixture or fixture qualification on this machine. Each row needs both real time and turn-based
play.

| Scenario | Expected |
| --- | --- |
| Mounted exploration → ordinary dialogue → Default | One "preserved" line at Dialog start and end; no dismount; Mount/Dismount and attack input refused during the dialogue |
| Cutscene with only lock/camera | Same, at Cutscene start and end |
| Hostile dialogue → combat | The pair enters the encounter mounted; no Mount action; native initiative and turn-start entitlement unchanged |
| Scene during an existing encounter (TB and RT) | The pair stays mounted; the current turn, spent Move/Standard and initiative order are unchanged afterwards |
| `CommandMoveParty` / `CommandMoveUnit` on the rider | One handoff line; the scene's move completes; restore (or regroup, then restore) in Default |
| Scene hides the rider | Handoff at the in-game boundary; restored after it reappears and the scene ends |
| Skip a staging cutscene | No stranded custody; restore in Default |
| Two pairs, one staged | The other stays attached throughout |
| Unmounted pair through a scene | Never mounted |

## Unsupported cases and their exact fallback

- **Scripted command on a mounted rider during an encounter:** no handoff. The command runs on the seated rider as
  before (it can stall until the cutscene's own timeout), and the rider stays on the mount.
- **Position writes on a seated rider alone** (`TranslocateUnit`, `CommandTeleportParty` without pets): the
  synchronizer keeps the rider on the mount, so the scene's rider placement is not applied. When the mount is also
  moved, the pair arrives together at the mount's placement.
- **Pair still staged when combat begins:** the pair fights unmounted in that encounter and remounts automatically
  afterwards. Re-pairing inside a running encounter would need a native participation decision, which restoration never
  manufactures.
- **Rider and mount farther apart than 30 m, or on different navigation areas, or regroup failed twice:** the pair stays
  dismounted. Routine feedback says why: "the mount could not regroup with its rider" or "rider and mount were separated
  by the event".
- **Consecutive scripted commands on the rider separated by non-staging steps:** the pair may remount between them and
  be handed off again. Restoration still needs Default, so this does not happen inside one continuous scene mode.

## Candidate identity

- **Commit:** `a4d1f0e2`. The Fast build used the identical source tree.
- **DLL:** `bin\Release\KingmakerMountedCombat.dll`, SHA-256
  `d095efa77033b733b2eec54789e19728b962f770469665e891088f706c4013ca`, MVID `c1cdf27d-1ae4-4a01-a606-53f32123882f`. This
  is a development build, not a package. The version is unchanged at 0.2.7.
- **Installation:** none. Nothing was staged, launched or restored.
- **Review:** an independent review is still required before any live use.
- **Not authorized:** merge, release, tag, version bump or install.
