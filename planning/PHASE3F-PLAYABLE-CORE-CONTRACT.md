# Phase 3F playable core — intake and implementation contract

Status: IN PROGRESS. User mission of 2026-09-05 supersedes historical phase/branch restrictions, not runtime or proprietary-reference safeguards. One active rider/companion pair only.

## Intake — 2026-09-05

Clean local and tracked upstream began at `db7eebbf27f8a05dc48febb138218f0df8ef859f`, direct documentation descendant of `d63d05579cc7dbaf72a270decbad334a8ddb992d`. Working branch: `codex/mounted-combat-phase3f-playable-core`. Sandbox remote verification encountered SEC_E_NO_CREDENTIALS; the subsequent host read-only check verified that exact remote descendant. No inherited changes were discarded.

The current Windows log was copied without overwrite to `C:/Dev/KingmakerMountedCombatLab/analysis-cache/phase3f-intake-20260905T1341077001555Z/local-output_log.txt`, marked read-only, and hashed. It and the existing `phase3e-fallback-human-review-20260905/output_log.txt` match supplied human-log SHA-256 `bb4dacc6e7bd85de1c3339b53394090a4f4842392448feb405d98ef5d5a315a0`. Handoff ZIP/screenshots are unavailable in the lab; their bytes and manifest have not been reopened. User-provided findings remain attributed to human review.

Fallback ZIP remains `9451787c08d39ec2164d75f1c36fb4d54245e4228ff12855950fc26798be6698`. Guarded Inspect confirms installed `0.1.0-phase3e-fallback.1`, exact DLL `5bcc3bc61bb1677ea81037fdc5a8ebd740ff4d0753d5255e37fcc789e6407f2f`, plus identical UMM `.65229.cache` file. Starting KMC inventory digest: `22b580f2482c8111c1110979bfc6748b6f3ef8004bfcfd57922eeed462193687`. Restore this actual starting content, including settings/cache, after temporary tests. Free C: space was 149,496,795,136 bytes. No game/UMM process observed.

## Implementation order

1. Reproduce fallback-default ordinary-attack rejection in a behavioral admission regression. Separate RT and actor-local TB admission from experimental flags; preserve exact native request, per-owner selection, cooldowns, range/LoS, and cancellation. Cover group selection without touching unrelated commands. Distinguish combat-start wait from combat-end cancellation.
2. Replace Horse static seating with an owned, cached animated visual projection after native animation. Keep mechanics, heading, navigation and reach independent of animated bones. Keep Mammoth profile unchanged. Revise original native control art and distinguish Mount/Dismount.
3. Audit exact native movement/turn reset side effects, including Standard conversion, time moved and five-foot-step. Reconcile only with verified epochs; otherwise preserve the exact counterexample as a TB blocker. Treat expected target invalidation as cancellation/completion based on the native child lifecycle, not an exception or invented success.
4. Run focused tests during changes, one complete applicable release gate, coherent commits and clean-source private packaging. Use guarded runtime only with exact continuity and actual-start restoration. At most eight transactions, two reserved for same-candidate fresh processes; one observation build/two repairs per cause; repeated unexplained terminal failure ends that investigation. Human visual/pointer checks remain pending until observed.

## Control and resource boundaries

`EnableUnifiedMountedTurn=false`, `EnablePairedCommandScheduler=false`, diagnostic overlay off are shipped acceptance settings. No roster/current-turn replacement, redundant-turn skipping, shared Standards, full attacks, new companion, or permanent deployment. Actor identity and native attack rules remain authoritative. Native request admission is `ClickUnitHandler.OnClick -> OnAttackRequested -> stock UnitAttack -> UnitCommands.Run`; direct invocation is integration evidence only.

RT intent may dispatch legal rider attacks repeatedly and independently legal mount attacks. A ranged rider never approaches for Bite. Separate TB intent is bound to the exact native turn object and current pair actor; no off-turn child dispatch. Ordinary party selection must contain the relevant actor; mount commands from the same RT group click must not create a second pair intent. Stop/Hold, ground, retarget, explicit Primary and lifecycle cleanup retire prior owned intent before any new dispatch.

Physical movement belongs to mount resources, rider attacks to rider resources. Unchanged rider Move is not evidence of free movement. The existing projection must be audited against exact installed native code before reconciliation; no arbitrary rider tax or invented round counter is permitted. TB remains unqualified if a duplication or projection defect cannot be bounded here.

Historical Phase 3D RT evidence does not qualify fallback.1 or Phase 3F. Phase 3E's narrow mount-command execution success and later K9 turn-selection failure remain attached to their original artifacts. Buff Planner exceptions, BugReportCanvas and shader warnings are separate observations, not established causes of KMC input failure.

## Bounded native resource audit — TB unqualified

Exact installed Kingmaker bounded records in `analysis-cache/phase3d-bounded/kingmaker/TurnBased.Controllers.TurnController.decompiled.cs` and `TurnBased.Controllers.CombatController.decompiled.cs` were inspected without copying proprietary source into Git. `Prepare` clears the current actor's cooldown object and then reapplies acting native commands, refreshes native round effects, and chooses movement limits. `ChooseNextUnit` calls `StartRound` when its ordered roster scan crosses the end; `StartRound` sorts and increments `RoundNumber`. This establishes a native roster-cycle number, not by itself a verified borrowed-movement epoch across delay/surprise/mode/remount boundaries.

`TickMovement` reads the current actor's Standard/restriction state through `GetRemainingTime`, not just Move. It also reads the current actor's AgentASP.NearTheEnd, movement limits and speed; writes TimeMoved, TimeMovedByFiveFootStep, MetersMovedByFiveFootStep, movement-limit state, AoO immunity and auto-stop state; and can call native selection Stop. Existing KMC projects only Move and restores only Move in finally. No direct Standard write occurs in this native method: Standard-to-movement capacity is implicit in remaining-time predicates, so unchanged Standard does not prove correct ownership.

Concrete source counterexamples: (1) mount Standard already used, rider Standard unused, mount Move at three seconds: projection can allow another move interval using rider readiness; (2) rider moves mount before its native turn, then mount Prepare clears that debit; (3) reverse order/new round can consult previous mount cooldown before its native reset. Stops/remounts do not define a lawful new epoch. Restoring Move alone also leaves rider TimeMoved/five-foot/auto-stop side effects, including on exceptions. No rider Move tax, early mount cooldown reset, or guessed per-round floor is added. Both turn orders over two rounds, partial movement across surfaces, Standard conversion, native step and mode/remount boundaries remain NOT RUN runtime gates and an explicit resource blocker. This mission does not claim TB resource reconciliation.

The tabletop reference [Mounted Combat, Archives of Nethys](https://aonprd.com/Rules.aspx?ID=196) assigns locomotion to the mount's action. It supports rejecting the proposed arbitrary rider movement tax; it does not choose missing Kingmaker turn-lifecycle semantics.

## Horse visual projection contract

The static KMC root frame remains the mechanics authority. The existing seven-bone pose lease now owns Horse pelvis position after native animation at execution order 32000. It caches the exact Chest and mount-root transforms, captures the posed seat relative to Chest once, and projects the current animated Chest into mount-root coordinates each LateUpdate. A Horse-only root-basis backward correction of 0.18 is a candidate anatomical calibration. Native pelvis translation is replaced by the seat projection, while upper-body/native attack rotations and equipment animation remain native. Angular inheritance from Chest is exactly zero; its uncalibrated rest quaternion is never applied to the rider. No root/entity position, scale, agent, reach, LoS, camera or selection write is added.

The pose frame lease restores all seven bone baselines before the next animation evaluation and on teardown; source destruction/reparenting invalidates health. Mammoth values and its static attachment are unchanged. Numeric post-write seat residual is diagnostic consistency only, not proof of visible animation. Motion, front/side/rear views and indoor/outdoor judgment remain HUMAN VALIDATION PENDING.

## Final offline correction and runtime boundary — 2026-09-05T15:00Z

Implementation `71bb139ba2547cec0d42690d7f09af6a5e26a218` was guarded-published and packaged as preview.1. A subsequent source review found the inherited presentation diagnostic still expected one shared 128px saddle sprite. Preview.2 updates that assertion to exact distinct 96px Mount/Dismount sprites and their separate native ability bindings. It adds no command-driving or readiness mutation. Icon readability remains a human gate.

No live transaction was admitted. The named deployment wrapper initially passed manual-only artifact parameters to an automated scenario; the native parameter guard rejected them before mutation. The wrapper now uses the immutable qualification-suite artifact pins, retaining exact clean package validation. Its current SHA-256 is `421a50e19d41afd7032e35f973507216058af4c123e655aa24560e68cd5beb39`.

The repository's read-only Steam preflight then proved there was no existing Steam client (`Exactly one already-running Steam client is required`). The lengthy read-only runtime WhatIf inventory pass was stopped at that external prerequisite; it is not recorded as a WhatIf PASS. Independent post-stop audit proved exact initial save/Mods inventories and no game, active transaction or sentinel. All eight live transactions remain unused. Starting Steam in verified Offline Mode is a required host action; desktop pointer automation also remains unavailable. The historical manual-review scenario itself establishes Mammoth/overlay presentation and read-only saves, so it must not be mislabeled as a native Horse/overlay-off/save-write playtest.
