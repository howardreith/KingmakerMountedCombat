# Private-alpha stabilization

Status: round-2 stabilization `IN PROGRESS`; the exact round-1 automated package failed guarded human regression.

This continuation repairs player-facing defects found in the first qualified Mammoth private alpha. It does not authorize horses, persistence, ranged mounted combat, mounted spellcasting rules, mounted feats, explicit mounted attacks of opportunity, mounted charge, another body profile, Phase 3 implementation, a public release, or a merge to `main`.

## Round-2 schema-v50 native command-terminal observation

Exact clean package `4778122f5a706b2f4234a56c89c90b36ea212f9731f0cc348ff9ad2820c5dc6a` from commit `7a6506e38fb0fc6d16d8b5e3b24fa85fac134f9e` produced fresh RT A/B `81/0` passes in suite `20260823T055100Z-round2-v49-reactive-fields-suite1`. Both independent restoration audits passed before evidence reads. This proves the repaired package's supported RT overlay/native-click path, but not TB.

TB attempt `20260823T075815Z-round2-reactive-suite1-tb-passA` remains immutable and uncredited at `40/2`, with exact audit-before-read restoration. It disproves the earlier unavailable-reactive-value theory: the exact Mammoth native turn was selected, directly controllable, movement-eligible, and exposed an active/interactable action bar with both reactive values true. Ordinary ground input admitted and visibly moved the Mammoth `0.9619302`, charged only Mammoth Move `0 -> 0.23123309`, left rider Move at zero, and emitted no attack. The native command then finished with a non-success result and the post-command action-bar predicate was false.

Schema v49 does not record which native result occurred or which post-command UI/turn token changed. Forward-only schema v50 therefore records exact command finished/result, raw Move-slot terminal state, destination remaining distance, selection, and a complete post-command UI/turn presentation snapshot. Failure messages also carry these values. This is observation-only and does not change behavior, thresholds, timeouts, selection, commands, action bars, movement, or action/resource ownership. No behavioral repair is permitted until fresh guarded evidence identifies the exact terminal boundary.

The complete observation gate passes source/component/visual/harness/assembly `21/237/17/223/294`, Release build, PowerShell/JSON parsers `27/0` / `7/0`, diff, and prohibited-payload validation. Historical v49 evidence remains accepted under its exact prior shape and is never relabeled.

## Round-2 v49 TB observation checkpoint

The first clean-package v49 TB process, `20260823T054000Z-round2-v49-suite1-tb-passA`, is preserved as uncredited `FAIL 36/1`. Exact independent restoration passed before its gameplay evidence was read. The process objectively proved a selected, directly controllable Mammoth native turn with movement eligibility and a visible, active, enabled action bar. It stopped because the two reactive action-bar values were unavailable, before any movement or attack claim.

This was an observation implementation error: exact installed `ActionBarManager.Active` and `CanUseAbilities` are public reactive fields, not properties. KMC now reads either public representation and still requires an exact Boolean `Value`; assembly tokens and deterministic field/property/unavailable tests bind the contract. No gameplay state or threshold changed. Fresh clean-package evidence remains required to reveal whether the human-reported inert TB behavior has an additional cause. The old package's RT A/B cannot be combined with evidence from the repaired package.

## Round-2 human regression correction

The human regression of exact final commit `942262a1173d657db209dc321b298702dfd655cb` used package/manifest/DLL SHA-256 `3bc8d52bed3035ec61c519b035aa726600e6c72d82f26642f5bbf332145aae08` / `de56eb322624e402939f02e7ecbce90e93d6cb6e82cabb53d650fad8764b9cc8` / `d01a75c173c692198963e4c72acb20e1c5891d89a95916a4f649ab7d1d417777`, MVID `f2b69d04-07de-4e4c-a1fb-027ea8178612`. That package and its `434/0` automated result remain immutable history, but the human result rejects any claim that those internal assertions proved ordinary human-play controls.

The exact local session log is `analysis-cache\manual-stabilization-regression-20260822\output_log.txt`, `83286` bytes, SHA-256 `6c8f5ea1258ef6ddb581760ef30836548cbbeb055c0f55ae3aae524d2c3e79dc`. It was read completely before behavioral work. It records repeated stock mounted-attack rejections and one internally successful rider-turn Mammoth ground route, but no supported overlay target-click or attack-command acceptance. Human play nevertheless found TB controls inert, RT attacks ignored/ambiguous, no distant door approach, and visible menu latency plus a brief fog/world flash. Basic mounting/movement, Wild Shape cleanup/visibility, adjacent door opening, reload dismount, and explicit Dismount passed and remain must-preserve controls.

Player-facing qualification now has two mandatory levels: exact internal state/ownership and actual native user-input admission with player-visible behavior. A test that directly invokes a downstream service or records only selection, action-bar ownership, roster membership, or resource state cannot claim human usability. Rendered/interactable UI and real-mouse behavior remain manual-gated wherever automation cannot observe them reliably.

## Round-2 implementation checkpoint

Exact installed input contracts identify three narrow attributable seams. KMC previously normalized every Mammoth selection to the rider, including the independent native Mammoth TB turn; the repaired policy preserves Mammoth selection only for that exact turn and otherwise retains rider projection. Schema v49 now observes the action-bar GameObject, enabled/hierarchy/reactive state, selected and active turn entity, direct controllability, movement eligibility, pointer mode, command queues, and AI state before issuing ordinary Mammoth ground input. It then separately exercises the existing rider-turn routed ground movement and native target click. RT schema v48 retains the exact overlay-to-native-click proof. These are forward-only schemas; earlier failed or automated evidence receives no retroactive credit.

Stock distant interaction creates `UnitInteractWithObject` on the selected rider, whose stock movement agent is intentionally disabled while mounted. KMC now replaces only an exact active-pair rider command targeting exact `StandardDoor` at the native `UnitCommands.Run` seam. The bounded rider Standard wrapper delegates one Move-slot approach to the Mammoth, invokes `door.Interact(rider)` once, restores the slot, and reports every terminal outcome. New `mounted-distance-door-interaction` uses ordinary `ClickMapObjectHandler.OnClick`, requires a start outside proximity range, then proves open-once and strict mounted traversal. No generic foreign interactable, sibling mod, or rider path is adopted.

The non-world mode handler's prior full portrait/action-bar hierarchy scan was an attributable KMC-owned latency amplifier during screen entry. Mode delivery now records only lightweight relationship/view state; the full UI scan remains available at explicit diagnostic checkpoints. KMC performs no fog, camera, scene, pause, or game-mode write. The reported fog/world flash and stock-comparable latency remain manual acceptance gates rather than fabricated automated PASS claims.

Complete offline gates are source `21/0`, Release, component `234/0`, visual/source-order `17/0`, unfiltered harness `222/0`, exact assembly `292/0`, PowerShell/JSON parsers `26/0` / `7/0`, diff, and prohibited payload. Coherent publication, clean package/suite, mode-specific WhatIf, fresh RT/TB A/B, and the distance-door process remain required.

## Current native TB-to-RT stabilization checkpoint

Clean guarded-published runtime input `e77a3079e2e258a3f1b33ec8929f810854cf5f23` produced package/manifest/DLL SHA-256 `b16535f94ee914c4953b40fd19e88f97dfa117f984f8134855b1eae79fa6eaaf` / `24bca8dd2b3d3b9e8b2d94824b724c14f27005c9930f3ee392ad963d341508e3` / `d01a75c173c692198963e4c72acb20e1c5891d89a95916a4f649ab7d1d417777`, MVID `f2b69d04-07de-4e4c-a1fb-027ea8178612`. One admitted suite snapshot `04cc10c11ff78a1899405c7387d6c990bc035e756545bff9f639694c57af62bd` bound all credited runs after separate RT/TB WhatIf and independent zero-mutation audits.

Fresh RT A/B `20260822T222335Z-private-alpha-stabilization-v47-suite1-rt-passA` / `20260822T224523Z-private-alpha-stabilization-v47-suite1-rt-passB` pass `81/0` each. Fresh TB A/B `20260822T230812Z-private-alpha-stabilization-v47-suite1-tb-passA` / `20260822T233145Z-private-alpha-stabilization-v47-suite1-tb-passB` pass `105/0` each. Doorway `20260822T235432Z-private-alpha-stabilization-v47-suite1-doorway` passes `62/0`. Automated aggregate is `434/0`. Every credited process restored exact save/Mods state before gameplay evidence was opened.

Both TB runs prove the exact native-exit repair repeats deterministically: one arm/attempt/mutation/success with result `reselected-rider`, after one exact owned Mammoth AI reassertion. After TB-to-RT the pair, rider/Mammoth/target roster, active rider view/renderers, pose/attachment, selection, action bar, portrait, and rider camera ownership are coherent. Rider-turn ground movement uses the exact Mammoth Move-slot command while charging only the rider Move ledger; rider melee owns exactly one Standard/attack/roll/damage chain with zero duplicate or opportunity chain. No synthetic turn or shared action ledger was introduced.

The earlier suite-2 failure below remains immutable historical diagnosis and receives no credit. The focused human regression remains required for screen open/close visibility, polymorph/revert visibility, mounted ranged rejection, and unmounted ranged stock control; automated evidence does not substitute for those UI observations.

Suite-2 schema-v44 RT A/B each passed `81/0`. Schema-v46 TB attempt `20260822T201000Z-private-alpha-stabilization-v46-suite2-tb-passA` is immutable uncredited `FAIL 103/1` after an immediate exact restoration audit. Every movement, player-click melee, action/resource, attack/rule, pair, view, pose, roster, Pause/Default, and Mammoth AI-lease assertion passed. Native TB shutdown left the exact visible rider without `SelectionManager` or action-bar ownership while portrait and camera ownership remained exact.

Forward-only schema v47 adds one exact native-exit UI-lease repair: only an armed still-mounted pair in exact `Default`, after the owned Mammoth AI boundary, may reselect the exact captured rider once. It rejects replaced views and inexact relationships and does not alter turn, command, movement, attack, action/resource, timeout, or cleanup ownership. Offline gates pass source/component/visual/harness/assembly `21/232/17/219/282`, parsers `27/0` / `7/0`, diff, and prohibited payload. Fresh clean-package RT/TB A/B and doorway evidence remain required; no stabilization runtime qualification is claimed from the failed process.

## Preserved historical artifact

The reported artifact remains immutable qualification history:

| Field | Historical identity |
|---|---|
| Package qualification commit | `eae1abd554e67f8e864571a97d48f479a75304af` |
| Final handoff commit | `ff4bae7ca5dc145373771b718574d15f7babdd5e` |
| Product version | `0.1.0-phase2b-dev.1` |
| Package SHA-256 | `6291a001513b490bf111eb34866634bd86c55183ba789d3dfc531854790e78b0` |
| Manifest SHA-256 | `16d01881350b2ce6172aecf27d47b630588d249ca3a199e1eb27f643e2c14cf4` |
| DLL SHA-256 | `9f715f4ddd4e086cee8f2aa3aaa4e746401cec113fc529809d1376eab1caf6c1` |
| DLL MVID | `292982ba-6486-4807-b4fa-c15dc0558266` |

No stabilization result may be credited to that package.

## Human evidence intake

The complete local evidence directory is `C:\Dev\KingmakerMountedCombatLab\analysis-cache\KingmakerMountedCombat-ManualPlaytest-20260821`. The Unity 2018.4 session log is `output_log.txt`; the absence of `Player.log` is not missing evidence. Screenshots and logs remain local and outside Git. Their immutable intake identities are:

| Evidence | Length | SHA-256 |
|---|---:|---|
| `README-FIRST.md` | 457 | `cabd8f2fb6234b43ba86aa8cdf97384a914a8cf6dbefa1a606547d1342e34fef` |
| `MANUAL-PLAYTEST-BUG-REPORT.md` | 5329 | `95f76fecee4d3fc9b4472f8f8f8aef2e1409553fb102881e8ce64de28fa5c076` |
| `01_mount_ready_outdoors.png` | 3440281 | `12098b4b6e2dbc88b7df6f63cf4c017ec26696c1d56b838f80065d063a9ccee3` |
| `02_mounted_outdoors.png` | 3339299 | `317d0ec072da866c6df8347d6957aaf5ea39390c5665af3ebdb0239491742b08` |
| `03_shapeshift_elemental_rider_missing.png` | 2815100 | `5d8fcc17d02c4ebfa5c3139fe4875ddcd0fa6228573e4685764c3e55ab15c546` |
| `04_turn_based_combat_mounted_controls.png` | 2672389 | `c1981ff22dd88a82d98e8345fd2f31e48441842b2271b14fc458c4a044ea75a8` |
| `output_log.txt` | 107179 | `f68a0d634f98db09020d18b8b5be91ec0bf34134f774fe00a6938c211e59fb63` |

The log records repeated KMC `AreaUnloading` cleanups when ordinary UI screens opened, one `CompanionInvalidated` cleanup during body replacement, and a stock ActionBar null-reference exception. It records no KMC exception and contains no prior structured combat-rejection telemetry. Those absences do not prove a cause for the human melee failure.

## Exact installed causes and narrow repairs

### Non-world UI

Installed `GameModeType` identifies `Default=1`, `Pause=4`, `FullScreenUi=5`, `EscMode=6`, and `Cutscene=7`. The old lifecycle subscriber treated every mode other than Default/Pause as `AreaUnloading`; that directly explains character-sheet/map dismounts and the matching log records. The repaired policy retains the exact pair through Pause, FullScreenUi, and EscMode, blocks new mounted actions until Default resumes, and records view activity, parent, sibling, renderer, pose, attachment, selection, and turn-based state as observation detail. Temporary stock visibility changes while the world is covered are not relationship invalidation. Exact view replacement/detachment and true world-changing modes remain cleanup boundaries.

### Polymorph/body replacement

Installed `Polymorph.TryReplaceView` token `0x06002A08` creates the replacement, parents it under the old view's current parent, copies position/rotation, attaches it to the entity, then destroys the old view through its transition. While mounted, that inherited parent is KMC's owned anchor. Old cleanup destroyed the anchor and therefore the replacement body. Before old-view pose/attachment cleanup, KMC now detects the exact replacement child, reparents it to the captured original world parent/sibling while preserving world position, rotation, scale, and `activeSelf`, then verifies the release. It never globally enables renderers or GameObjects. Repeated replacement/detach cleanup remains idempotent. Installed `RestoreView` token `0x06002A09` remains stock-owned.

### Turn-based roster and ground movement

The old exact `CombatController.StartTurn` postfix immediately ended every Mammoth turn unless the KMC action had already been armed, which ordinary play cannot do before turn start. The patch is removed: rider and Mammoth retain their independent native roster entries and ledgers. Rider melee remains rider Standard-owned; Mammoth primary remains Mammoth Standard-owned.

An ordinary selected-rider ground click is rewritten to the Mammoth before `UnitMoveTo` construction. Only that exact created-by-player Mammoth Move-slot command is leased. Installed `TurnController.Tick` leaves a directly controlled rider in `Preparing` until the rider owns a command; because ground movement deliberately creates no synthetic rider command, the exact ground adapter may advance the Mammoth command under the current rider's native `Preparing` or `Acting` window through the rider controller's `TickMovement`. This charges only rider Move. The later rider attack wrapper naturally advances the native turn to Acting. KMC writes no turn status or cooldown and creates no shared ledger.

Exact installed TB exit semantics add two native boundaries to the guarded human-play scenario. `CombatController.Disable` pauses the game when the player remains in combat, so the scenario must first prove TB is disabled and the native Pause is present, then perform the same stock unpause needed to resume real-time play before asserting `Default`. `TurnController.Start` releases `CameraUnitFollower` while retaining its rider unit; therefore TB camera coherence binds exact rider ownership and the native released-follower state rather than requiring a contradictory active follower. The pre-transition camera state is still captured and restored exactly after the row. These are diagnostic qualification semantics, not a KMC-owned synthetic turn, camera, or pause system.

Installed `CombatController.HandleCombatEnd` also enables AI for every controllable character after clearing its TB controller. That stock reset includes the Mammoth even while KMC still owns its bounded AI-disable lease. KMC now arms a one-shot repair only from exact native TB-false delivery, waits for both the TB predicate and controller initialization to clear, requires the exact mounted relationship and existing owned lease, and reasserts only that lease. Forward-only schema v46 records exactly one arm, attempt, mutation, and success. Missing, repeated, premature, unowned, or failed reassertion is rejected. This does not create an AI policy, synthetic turn, or shared action ledger.

The first live v46 row proved that reassertion, then exposed a diagnostic-only target lifetime race: its forced critical killed the sole one-HP target, so native combat, turn, and resource state closed before the TB-to-RT checkpoint. The exact v46 TB human row now uses the existing reversible diagnostic temporary-hit-point lease to keep that disposable target alive only through the transition proof. Native TB disable can also leave exact `Pause` while combat ends between event delivery and the next observation frame; the row therefore recognizes the already-proven TB-false/controller-cleared Pause boundary without requiring a stale `Player.IsInCombat` sample before issuing ordinary stock unpause. Failed evidence retains an explicit not-observed presentation value, preserving strict schema shape. None of these changes affect production combat or target behavior.

### Rider melee and ranged control

The original session has no exact combat-admission record, but screenshot `04` preserves the terminal overlay feedback `Mounted combat cancelled: ground command.` The installed IMGUI combat buttons armed an action without taking a bounded lease over the same mouse activation; the exact ground prefix then cancelled any armed action. That code path is sufficient to explain a combat-button click propagating to the world underneath the overlay, while the absence of an admission log remains truthfully preserved. The repair gives one exact overlay activation a one-shot, two-frame maximum world-click guard. A propagated unit or ground click is suppressed without clearing the armed action; later deliberate clicks remain stock/KMC-routed. It does not install a global pointer patch or retain input ownership.

The player-facing click path also reports ordered rejection codes and exact feedback for selection, relationship/body profile, combat/life state, target validity/visibility/hostility/attackability, turn/action availability, active command, lifecycle mode, weapon eligibility/category, unsupported ranged attack, path, supported range, range-origin mismatch, and command admission. Accepted and terminal commands log actor, target, ownership, child count, repath count, terminal reason, rejection codes, and feedback.

New schema v44/v46 rows use the same player-facing overlay controller arming seam, first prove that one propagated native unit click is suppressed while the action remains armed, and then enter the exact native `ClickUnitHandler.OnClick` direct-view path on the next deliberate click. They require zero admission rejection codes, the already qualified Mammoth-origin range contract, exactly one rider attack/roll and at most one damage event, no duplicate or opportunity chain, rider-only Standard cost, and exact cleanup. Schema v46 supersedes historical v45 for the TB human path: it mounts in RT, enters TB while mounted, proves both pair actors and target remain in the native roster, performs ordinary rider ground movement with exact Mammoth execution/rider Move accounting/slot restoration, performs rider melee, returns to RT while the pair remains mounted, and proves the one exact native TB-exit AI-lease reassertion before cleanup. Its bounded transition observations require an active rider view, exact rider selection, rider-owned active action bar, one active selected rider portrait, rider-owned camera identity in the native released-follower state, and healthy pose/attachment after both RT-to-TB and TB-to-RT transitions.

Stock mounted rider `UnitAttack` admission is rejected only for the exact mounted rider. A selected ranged weapon receives `Mounted ranged attacks are not supported in this private alpha.` A stock melee click receives direction to use the supported Rider melee surface. Non-mounted and non-pair commands remain stock.

## Regression disposition

| Required scenario | Current proof boundary |
|---|---|
| `mounted-ui-character-sheet-open-close` | deterministic mode/view policy and observation telemetry PASS; guarded manual UI proof pending |
| `mounted-ui-map-open-close` | deterministic mode/view policy and observation telemetry PASS; guarded manual UI proof pending |
| `mounted-ui-combat-menu-open-close` | deterministic mode/view policy and observation telemetry PASS; guarded manual UI proof pending |
| `mounted-polymorph-clean-dismount` | exact installed replacement contract plus deterministic lease cleanup PASS; guarded manual proof pending |
| `mounted-polymorph-view-visible` | replacement release/visibility invariants deterministic PASS; guarded manual proof pending |
| `mounted-polymorph-revert-visible` | stock restore ownership preserved; guarded manual proof pending |
| `mounted-rider-melee-human-play-path-rt` | fresh schema v44 RT A/B PASS `81/0` each |
| `mounted-rider-melee-human-play-path-tb` | fresh schema v47 TB A/B PASS `105/0` each |
| `mounted-enter-turn-based` | schema v47 exact RT-to-TB pair/roster proof PASS twice |
| `mounted-turn-based-ground-movement` | schema v47 exact Mammoth command/rider-ledger/slot proof PASS twice |
| `mounted-exit-turn-based` | schema v47 exact TB-to-RT pair, AI lease, and rider UI-principal proof PASS twice |
| `mounted-door-open-and-traverse-regression` | fresh exact-door regression PASS `62/0` |
| `mounted-ranged-rejected-rt` | exact pair-local stock-command policy PASS; guarded manual RT proof pending |
| `mounted-ranged-rejected-tb` | exact pair-local stock-command policy PASS; guarded manual TB proof pending |
| `non-mounted-ranged-control` | pair-local negative control PASS; guarded manual control pending |

The final stabilization package will not claim UI, polymorph, or ranged human proof before the required manual regression. Saving, loading, and area transition remain intentional clean-dismount boundaries. Door traversal remains a must-preserve regression. Non-body-changing stock spellcasts remain incidental and unqualified; body-changing spells cleanly dismount. Native-toolbar Mount targeting remains backlog-only.
