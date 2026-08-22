# Private-alpha stabilization

Status: `IN PROGRESS`

This continuation repairs player-facing defects found in the first qualified Mammoth private alpha. It does not authorize horses, persistence, ranged mounted combat, mounted spellcasting rules, mounted feats, explicit mounted attacks of opportunity, mounted charge, another body profile, Phase 3 implementation, a public release, or a merge to `main`.

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

### Rider melee and ranged control

The original session has no exact combat-admission record, but screenshot `04` preserves the terminal overlay feedback `Mounted combat cancelled: ground command.` The installed IMGUI combat buttons armed an action without taking a bounded lease over the same mouse activation; the exact ground prefix then cancelled any armed action. That code path is sufficient to explain a combat-button click propagating to the world underneath the overlay, while the absence of an admission log remains truthfully preserved. The repair gives one exact overlay activation a one-shot, two-frame maximum world-click guard. A propagated unit or ground click is suppressed without clearing the armed action; later deliberate clicks remain stock/KMC-routed. It does not install a global pointer patch or retain input ownership.

The player-facing click path also reports ordered rejection codes and exact feedback for selection, relationship/body profile, combat/life state, target validity/visibility/hostility/attackability, turn/action availability, active command, lifecycle mode, weapon eligibility/category, unsupported ranged attack, path, supported range, range-origin mismatch, and command admission. Accepted and terminal commands log actor, target, ownership, child count, repath count, terminal reason, rejection codes, and feedback.

New schema v44/v45 rows use the same player-facing overlay controller arming seam, first prove that one propagated native unit click is suppressed while the action remains armed, and then enter the exact native `ClickUnitHandler.OnClick` direct-view path on the next deliberate click. They require zero admission rejection codes, the already qualified Mammoth-origin range contract, exactly one rider attack/roll and at most one damage event, no duplicate or opportunity chain, rider-only Standard cost, and exact cleanup. Schema v45 additionally mounts in RT, enters TB while mounted, proves both pair actors and target remain in the native roster, performs ordinary rider ground movement with exact Mammoth execution/rider Move accounting/slot restoration, performs rider melee, and returns to RT while the pair remains mounted before cleanup. Its bounded transition observations require an active rider view, exact rider selection, rider-owned active action bar, one active selected rider portrait, rider-owned camera follower, and healthy pose/attachment after both RT-to-TB and TB-to-RT transitions.

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
| `mounted-rider-melee-human-play-path-rt` | schema v44 protocol/validator PASS; fresh guarded runtime A/B pending |
| `mounted-rider-melee-human-play-path-tb` | schema v45 protocol/validator PASS; fresh guarded runtime A/B pending |
| `mounted-enter-turn-based` | schema v45 exact RT-to-TB pair/roster proof pending live A/B |
| `mounted-turn-based-ground-movement` | schema v45 exact command/ledger/slot proof pending live A/B |
| `mounted-exit-turn-based` | schema v45 exact TB-to-RT retained-pair proof pending live A/B |
| `mounted-door-open-and-traverse-regression` | prior doorway proof preserved; fresh guarded regression pending |
| `mounted-ranged-rejected-rt` | exact pair-local stock-command policy PASS; guarded manual RT proof pending |
| `mounted-ranged-rejected-tb` | exact pair-local stock-command policy PASS; guarded manual TB proof pending |
| `non-mounted-ranged-control` | pair-local negative control PASS; guarded manual control pending |

The final stabilization package will not claim UI, polymorph, or ranged human proof before the required manual regression. Saving, loading, and area transition remain intentional clean-dismount boundaries. Door traversal remains a must-preserve regression. Non-body-changing stock spellcasts remain incidental and unqualified; body-changing spells cleanly dismount. Native-toolbar Mount targeting remains backlog-only.
