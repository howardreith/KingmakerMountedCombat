# Horse group pace — review checkpoint (2026-11-12)

Branch `codex/kmc-horse-group-pace` (draft PR); base main `bd22a045` (0.2.6). Read CURRENT.md and
docs/HORSE-GROUP-PACE-REPAIR-HANDOFF.md.

- **State:** source fix plus diagnostics. Fast PASS on genuine references. Native NOT RUN.
- **Seam:** `UnitCommand.AccelerateIfPathIsLong` 0x060027B4. The routed order's identity is `MountedCombatController.playerMove`, bound only by the ground click's own Run.
- **Native next step:** owner play with the branch DLL, or a qualified KMC fixture. Use a whole-party move longer than 13 m with Accelerated Move on, then the restriction, short-move and turn-based rows in the handoff.
- **Open, same mechanism, not changed:**
  - the entrance-carried `UnitAreaTransition`;
  - gamepad stick acceleration;
  - the group limit computed from the rider rather than the horse.

# Attack completion warnings — review checkpoint after the lab continuation (2026-11-12)

Branch `codex/kmc-attack-completion-warnings`, draft PR #33; base main `680d02cb` (0.2.5). Read CURRENT.md and the top
of docs/ATTACK-COMPLETION-WARNINGS-HANDOFF.md.

- **State:** source correction plus a review refinement (code-classified later-dispatch refusals; all stock dispatch
  rejections logged). Ten regressions in total. Fast PASS 3/0 on genuine references (components 1168/0).
- **Lifetime:** command-terminal cleanup clears `stockIntentCommand` but intentionally retains `stockIntent`. The
  latter remembers accepted dispatch. Read the rejection feedback before clearing the intent; same-target promotion
  must preserve dispatch history.
- **Native next step:** it needs a qualified KMC Baseline/Working fixture and `runtime-state\fixture-qualification.json`
  on this machine (neither exists), or an owner session with the branch DLL. Take the scenarios from the handoff's
  remaining-evidence section.
- **Delivery:** review branch only; no merge/release/tag/install/launch. Existing unrelated defects remain open.

# Exploration repair resume — partial, native blocked (2026-11-12)

Branch `codex/kmc-exploration-markers-speed-events` (draft PR). Read CURRENT.md and docs/EXPLORATION-REPAIR-HANDOFF.md.
- **State:** source repairs for issues 3–7 have the owner review's corrections and pass Fast. Issues 1–2 are parked by
  the owner and have diagnostics only.
- **Saved intent:** archive member `kmc-retained-intent` (Domain `MountedRetainedIntentRecord`). It is written only
  when intent exists, adopted at `OnAreaLoaded` only for the exact campaign, area and `TimeController.GameTime`
  ticks, and never for a pair that `kmc-mounted-state` holds.
- **Native:** every row is NOT RUN. It needs the Working fixture and runtime-state, or an owner session with this DLL.
- **Next:** owner play on the branch DLL. Issues 1–2 wait for the owner's details.

Facts for a successor:
- **World map:** the area whose `CustomUIScene.SceneName == SceneName.CustomUIGlobalMap`. Native `GlobalMapRules` enters
  `GlobalMap` mode during scene init, so no attachment is admitted there.
- **Console selection:** `UnitSelectionManager` (console UI) exists only under gamepad control. The `AgentOverride`
  click-point branch of `MoveSelectedUnitsToPoint` does not run in mouse play.
- **Corpse:** a corpse click is `UnitLootUnit` (Standard, 1 m). Loot bags (`DroppedLoot`) are map objects with a
  `LootComponent`.
- **Camp:** `RestInteraction.OnInteract` issues `UnitApproachCamp` per unit (pets included) under one
  `RestGroupCommand` with no timeout.
- **Familiar:** `TeleportToMaster` copies `Master.View.transform.parent`.

# Finalization resume — F6 accepted and merged; no planned engineering (2026-11-07)

**Integration:** main contains F6 as merge `eb16d2f1`, owner-merged; its tree is identical to the reviewed `4b8a5f02`. Read CURRENT.md and the closeout section at the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.

**State:** the six-sprint selected refinement engineering is complete, except that WORLD-02 stays deferred and not passed (docs/FINALIZATION-DEFERRED-ITEMS.md).

**What remains:**
- the owner's play acceptance on the f6f package `74baa915…`;
- an explicit publication decision.

There is no F7. A defect the owner reports selects only its concrete causal correction.

**Not authorized:** release, tag, publication, installation or a new native start. The finalize-f6 record is at 5 of 6 starts and 42.75 of 90 minutes; the sixth start was never used and is not a testing target.

# F6 resume — final review stop, completion PASS on re-evaluation (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f6; branch codex/kmc-finalize-f6-falls-final-acceptance; draft PR #21. Read CURRENT.md and the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE — READY FOR FINAL REVIEW.
- **Spend:** 5 of 6 starts, 42.75/90 minutes. Start 6 is a contingency for a concrete diagnosed defect only.
- **Not authorized:** merge, release, tag, install or publication.

Facts for a successor:
- **Native stand-up:** TurnController.HandleUnitWillGetUp adds +3 s to Move and cancels the order. While the Standard is unused, HasMoveAction() stays true after one Move, so read the Move cooldown.
- **Every new Phase 3D row name** must be in `Get-KmcPhase3dHorseRuntimeRows`, and every new chunk7 persistence row in `Get-KmcChunk7PersistenceRows`. Otherwise the outer reader refuses the run as an "unknown subscenario".
- **Retired custody:** an unseated pair whose custody retired reads `None`, not `Unmounted`.
- **Split timing:** the paired split can end the activation a frame after the fall resolves.

# F6 resume — review stop, 4 of 4 starts (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f6; branch codex/kmc-finalize-f6-falls-final-acceptance. Read CURRENT.md and the top of docs/FINALIZE-F6-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - Shown natively: the TB death fall, the RT prone fall, and the post-fall save/reload (r03 pinned reader-only source, r04 PASS).
  - Not shown natively: the safe-landing branch, the TB prone, prone-then-death and the TB stand-up cost.
- **Spend:** 32.77/90 minutes, 4/4 starts. No further start is authorized.
- **Not authorized:** merge, release, tag, install or publication.

Facts for a successor:
- **Mount death during the pair's own activation:** the existing paired split force-ends that activation (native ForceToEnd). A stand-up row must wait for the rider's next native turn.
- **New chunk7 persistence row names** must be added to `Get-KmcChunk7PersistenceRows`, or the runtime-result readers refuse the run as an "unknown subscenario".
- **New Phase 3D child scenarios** need the `-clike` lists in `ChildEntryPreambleEvidence.ps1`, not only the exact-name lists.
- **Labelled test outcome:** `MountedRiderFallService.ScenarioOutcome` picks the d20 inside the native check from its own stat and adjusted DC. It is null in play.
- **The F3 row `F3-tb-b-round1`** can refuse its full attack non-deterministically (r02).

# F5 resume — final review stop, cold consumer PASS (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE — READY FOR FINAL REVIEW.
  - Producer r07 and cold consumer r08 both PASS at d87b148e (DLL `8459126c…`), and the cross-run reader passes.
  - Manual `d39f8b46…`, 858008 bytes.
- **Spend:** 8 of 8 starts, 74.9/90 minutes. No further start is authorized.
- **Not authorized:** merge, release, tag, install, publication or F6.
- **Next:** final review and owner acceptance.

# Superseded F5 resume — saddle-diagnosis mission stop (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. Saddle cause proven and corrected. r06 (fd8bb820) is game PASS with the corrected saddle and all naming rows, but its recorded verdict is FAIL (harness UTF-8 defect, fixed in d6c88978). The consumer has not run.
- **Spend:** 6 of 7 starts, 63.97/90 minutes.
- **Not authorized:** reusing r06 (recorded FAIL) as a source, an eighth start, more time, merge, release or F6.
- **To finish once the owner raises the ceiling to 8:**
  - Run a Candidate at the current head.
  - Run `scripts/Package.ps1 -ArtifactQualifier`, then the suite snapshot.
  - Run the producer `companion-names-tack-manual` (chunk7-persistence-save) and inspect its frames.
  - Run the consumer (chunk7-persistence-load) from the producer's run id and Manual hash.
  - Run `scripts/Test-Finalize5ColdAgainstSource.ps1`.
  - Make no records commit between producer and consumer.

Facts for a successor:
- **Real dog body:** the dog3 back has a top vertex only every 5–10 cm. Sample surfaces with rays, never with the "highest vertex in a window" rule.
- **Specimen and replay:** the tack specimen (`tack-specimen/` in a run's evidence) plus the replay in `analysis-cache/finalize-f5-saddle-replay/` reproduce any fit offline. `Replay.cs` compiles with the VS 2022 BuildTools Roslyn `csc`, Kingmaker's Newtonsoft.Json, and the UnityEngine and CoreModule DLLs copied beside it for type loading only.
- **Evidence encoding:** game evidence is UTF-8 without a byte-order mark. Read it with `-Encoding UTF8` (`Read-KmcJson` now does).
- **Earlier F5 facts:** the TMP `m_char_buffer`, overtip and post-F2 slot facts are in the superseded section below.

# Superseded F5 resume — completion mission stop (2026-11-07)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack; draft PR #20. Read CURRENT.md and the top of docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. r04 (d32fdc28, f5d) has naming and the Manual PASS game-side; the saddle reader FAILs and the frames show shards. The consumer has not run.
- **Spend:** 4 of 6 starts, 42.52/60 min.
- **Not authorized:** reusing r04 as a source, a start without a diagnosed correction, more time, merge, release or F6.

Facts for a successor:
- This TMP's `SetText` never stores `m_text`; read a rendered label from `TMP_Text.m_char_buffer` (0x040014C2) up to its 0.
- Overtips live in `OvertipManager.m_UnitControllers` (via `Game.Instance.UI.BarkManager`) and draw `CharacterName` with a decorative first letter.
- The tack's centre-line diagnostics can be on design while the rendered pad is wrong. Judge it by `maxRise` and the frames.
- The scratch guard `f5-guard.ps1` derives the ceiling from the record's `startCeilingAdjustments` chain, capped at 6.
# F5 resume — review stop, start ceiling reached (2026-11-07, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f5; branch codex/kmc-finalize-f5-names-dog-tack from main 3f19e9bd. Read CURRENT.md and docs/FINALIZE-F5-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. 3 of 3 starts used, 26.83/60 min. The producer runs r01–r03 all FAILed for causes since corrected; the cold consumer has not run.
- **Not authorized:** a fourth start, merge, release or F6.
- **F4:** integrated by the owner (3f19e9bd). WORLD-02 is deferred, not passed ([deferred items](docs/FINALIZATION-DEFERRED-ITEMS.md)).

Facts for a successor:
- **Post-F2 controls:** a rider can hold the public control (kind 6) and its placement record (kind 7, index 0). A saved hidden Dismount (kind 2) binding is migrated into kind 6, so the old Chunk 7 two-per-rider slot expectations predate F2 (the F5 case binds Rider Primary only).
- **Native text dialog:** while its field holds focus, KeyboardAccess runs no binding, Esc included. Keys sent through `TMP_InputField.ProcessEvent` need `ForceLabelUpdate` before they are drawn.
- **Real dog body:** fur and limbs spike ray casts, and the midline's lowest point drops to the ground ahead of the seat. Tack geometry must smooth from one anchor per sample and be skinned to the spine only.
- **Launch:** freeze from the attached head and commit records only after both runs.

# F4 resume — final review stop, reload PASS (2026-11-06)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** F4 ENGINEERING PARTIAL — WORLD-02 INPUT BLOCKED ONLY.
  - r08 (entrance) and r10 (reload) PASS on the same DLL 3240963f…/cc86d1d6.
  - 10 of 10 starts used, at 68.17/80 min.
- **Not authorized:** an eleventh start, merge, release, publication, or F5.
- **Only remaining item:** the native trap attempt, which needs the owner's Manual trap save.

# F4 resume — final review stop after the final reload execution mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions (head 82689ab7 plus records); draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - The reload is NOT RUN.
  - Start 9 was spent on a pre-staging refusal: the detached HEAD made `git branch --show-current` empty, so `.Trim()` threw in `Assert-KmcPackageManifest`.
  - WORLD-02 is input blocked.
  - 9 of 9 starts are used, at 64.7/80 min.
- **Not authorized:** a tenth start, merge, release, publication, or F5.

Facts for a successor:
- The launcher pins the package manifest's commit *and* branch to the worktree. Never detach to reach a frozen commit.
- Commit records only after the native runs, or package from the head the run will use.
- A refusal after admission counts as a start in the record.

# F4 resume — final review stop after the entrance/reload finishing mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and the current section of docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL.
  - The r08 entrance is PASS.
  - The reload is not run: 8 of 8 starts are used, at 64.47 of 75 min.
  - WORLD-02 is input blocked.
- **Not authorized:** merge, release, publication, a new start, or F5.

Next run, if the owner authorizes one start:
- The scratch helper is `f4run.ps1 -Kind dungeon-entry-auto -SourceRunId 20261106-finalize-f4f-dungeon-08 -SourceSha256 b8e4109ea5bd9deae8e8e77b7b0318e1afa0d42895071d1f3a52a29c284540f5`. It is pinned to f4f; a new ceiling needs the same append-only record entry and a helper change.
- The consumer WhatIf `20261106-finalize-f4f-dungeon-reload-whatif` already passed.

Facts for a successor:
- The product writes the schema-3 archive member (`Pairs[]`). `NativeMountedSaveStorage.Read` / `persistence.LoadedData` is the legacy view and is not Current for schema 3. Use `ReadArchive` / `LoadedArchiveData`, as the cold restore path does.
- The launch guard reserves the 600 s outer timeout, so each start needs at least 10 minutes of budget left.
- `@()` around a comma-wrapped .NET List from a function counts the list as one element. Enumerate it with `foreach`.

# F4 resume — final review stop after the completion mission (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions; draft PR #19. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. 6 of 6 starts are used (47.88/75 min). The launch helper refuses a seventh start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Native `GroupManager.Go` runs `SetGroup`, `Show(false)` and then the go action (the AfterEntry `LoadArea`) in one call. The hub's selector component is destroyed by then, so read it only through Unity's `== null`.
- The workflow batch guard refuses a third equivalent attempt in a batch after two failures. A materially changed candidate runs under a new batch id. A refusal charges no start.
- The Working fixture floor has native hostiles that can join a row's fight and keep the pair in combat. The Sprint 4 melee row then waits for out-of-combat forever.
- Native Hold (`AiMovementForbidden`) filters brain proposals to targets in reach with LOS. A completed player move arms KMC's player-move hold; Stop clears it, and an AI off/on cycle clears the Stop suppression.
- The P07 dungeon entry declares one leaf `Auto_1.zks` with Area = floor and AdmissionArea = hub, write cap 1 and load cap 1. The Working fixture's baseline and working areas are both the floor `9d1278a2…`.

# F4 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f4; branch codex/kmc-finalize-f4-orders-ai-transitions. Read CURRENT.md and docs/FINALIZE-F4-REVIEW-HANDOFF.md.
- **State:** ENGINEERING PARTIAL. All 3 F4 starts are used (23.85/75 min). The launch helper refuses a fourth start.
- **Not authorized:** merge, release, publication, a new start, or F5.

Facts for a successor:
- Registering a new tranche scenario takes more than the Sprint 4 list. The `-clike 'sprint4-*'` prefix dispatches in Chunk6aNativeMammothEvidence.ps1 and ChildEntryPreambleEvidence.ps1 need it, and so do both row lists: RuntimeHarness.Common.ps1 near 4223 and RuntimeProtocol.cs near 1566.
- Owner-surface isolation is zero-write. The native after-entry autosave of a dungeon entry is a fatal violation there.
- The roguelike hub holds 2 LootComponent (ChestForStash) and no trap.
- After an F4 stage moves the pair, the rider-on-foot comparison leg may find no route.
- `UnitAttack.Target` has a private setter, so the continuation sets it through the exact token 0x0600266C.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f4.json.

# F3 resume — final review stop (2026-11-06, completion)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud; draft PR #18. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md.
- **State:** ENGINEERING COMPLETE. TB r05 and RT r04 PASS.
- **Allowance:** 5 of 6 starts used, at 51.47/90 min. The sixth start was a contingency for a diagnosed defect and is not scheduled; the F3 launch helper refuses a seventh.
- **Not authorized:** merge, release, image publication, or F4.

Completion facts:
- **Native full attack in TB.** It needs the attack variant cycled to Full with the native right-click before the left click. The fixture's pair-B rider, at level 11, plans four strikes.
- **Panel.** The prediction panel's slots follow their `TurnController`. The mount's private states only advance on the mount's own command events, so the fill recomputes them on every rider-selected ground hover. `ComputeCurrentActionStates` is a turn-start snapshot: any started Move or conversion counts as gone, and a used step still counts as available.
- **Cleanup.** In r05 it went Pause, Default, then native leave-combat, and EscMode did not recur. r03's opener is unidentified.

# F3 resume — review stop (2026-11-06, superseded above)

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f3; branch codex/kmc-finalize-f3-movement-economy-hud. Read CURRENT.md and docs/FINALIZE-F3-REVIEW-HANDOFF.md. All four F3 starts were used at that stop; the completion mission later raised the ceiling to six.

Facts for a successor:
- The Working fixture's second party member has no pet. The Sprint 6 Ranger acquisition needs four levels of experience granted first.
- A row step bound must stay under the tranche's 30 s leaf deadline, or a stall loses its facts.
- With the rider selected, the TB preview previously read the rider's TurnController. PairedMovementContext now routes it to the mount.
- Native: a partial five-foot step leaves a residual step. A move does not empty movement (`DoesCommandEmptiesMovement` is false).
- Native `UsedStandardAction` ignores the two-move conversion; `HasStandardAction` does not.
- The fixture rider's TB stock attack is a single attack unless it approaches.
- r03's cleanup stalled in EscMode with the party still in combat; the cause is unknown.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f3.json.

# F1 resume — final review stop

Worktree C:\Dev\KingmakerMountedCombatLab\worktrees\finalize-f1; branch codex/kmc-finalize-f1-creation-ui-icons; draft PR16. Read CURRENT.md and docs/FINALIZE-F1-REVIEW-HANDOFF.md. Preserve newer work. Do not restart intake or historical campaigns.

State: F1d run 20261105-finalize-f1d-native-01 passed and restored. All four F1 starts are used. Nothing runs from this resume: no launch, allowance increase, merge, release or F2 is authorized. A review correction that changes product bytes needs a new owner decision, package, suite and start; do not relabel run evidence onto it.

Facts for a successor:
- WhatIf stops before Test-RuntimeRequest. scripts/Test-FinalizeF1Finish.ps1 validates the next request's exact shape directly.
- The package allowlist excludes UnityEngine.UI, so the product reaches those members by exact token through reflection.
- The observer's refresh-scroll probe returns -99 because no parent ScrollRect is found from the layer. It gives no scroll evidence.
- Creator Next requires all ability and skill points spent. UMM's Ctrl+F10 needs real scan codes under synthetic input. Check the class list's scroll before clicking; it differs in a second creator.
- The publishing helper registers images by exact path and SHA, and each new image needs owner approval; see docs/evidence/finalize-f1/publishing-authority.json.

First command: Get-Content C:\Dev\KingmakerMountedCombatLab\runtime-state\workflow-budgets\finalize-f1.json.
