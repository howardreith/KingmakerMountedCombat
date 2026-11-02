# Sprint 4: natural attacks and real-time retargeting

**SPRINT 4 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.**

The review of `2e43e289` declined that head because automatic commands could erase Hold and the move hold followed the order,
not the arrival. Both are corrected below. r03 and r05 remain evidence for the binaries they ran.

Automated evidence is not user acceptance, and user acceptance is not automated evidence. This workflow selects no
merge, release, tag or installation.

## Baseline and identities

| Item | Value |
| --- | --- |
| Integrated baseline | `main` `2c2e6fc6` (PR #8 merged 2026-11-01, pre-release `v0.1.0-sprint3-preview.100`) |
| Branch | `codex/mounted-combat-sprint4-natural-attacks-retargeting` |
| Final compiled candidate | sprint4i, commit `526c32e6`, DLL `8236574a…` (full hashes under *Evidence*), MVID `fad7f560-8872-4613-92e6-e38bef777116`, 8,541,184 bytes (limit 9 MiB unchanged) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint4i-diagnostic.zip`, SHA-256 `3801c2d7…` |
| Later commits | Readers, the recovery tool and docs only; no compiled change |

Sprint 3 is owner-accepted for advancement, with the owner's manual trap check pending. Its automated mounted trap
attempt was never executed and stays unexecuted. It is not a Sprint 4 prerequisite.

## ATTACK-01: the helpers are gone

Rider Primary (`27364df6…`) and Mount Primary (`f88a50d6…`) are no longer leased to any unit, so they leave the ability
drawer and every list built from a unit's abilities. Mount Companion and Dismount are unchanged. Ordinary attacks never
depended on them: a hostile click routes through the existing stock intent.

**How saved references behave**

- **Facts and slots in a current save:** the save scope already strips every KMC fact and exact hotbar slot before
  serialization, so a current save carries neither. No migration was needed for them.
- **KMC's own saved slot bindings:** a binding for a helper finds no fact on restore, so it restores nothing.
- **Native auto-use (`m_AutoUseAbility`):** this is serialized and can name a helper. Left alone, the party brain would
  try to cast the retired ability (`BlueprintAiAttack.CreateCommand`).
- **Reconciliation:** every update clears only exact helper slots and an exact helper auto-use selection. Other slots
  keep their content and position, and the check is idempotent.
- **Definitions stay registered** under their original GUIDs, so any older reference resolves. `Evaluate` reports them
  unavailable and invisible unless selected.
- **Diagnostic access:** `RetiredPrimaryHelpersSelected` is set only for automation scenarios other than Sprint 4's.
  Ordinary play never sets it. Historical helper-driven rows were not rerun and are not the oracle here.

## AI-01: the cause and the path

**First mounted/unmounted divergence:** `AiBrainController.SelectAction` proposes through `UnitCommands.Run` about every
0.3 s, with `AiAction` set and `CreatedByPlayer` false. An unmounted unit's proposal simply runs. A rider's reached
`TryRouteMountedStockAttack`, which refused it silently as a command with no observed click, so the rider idled after
its target died. The party AI was working all along; KMC was discarding its answer.

**Selection and dispatch:** the target comes from native perception, hostility, reachability and scoring. KMC scans and
scores nothing. A proposal goes through its own admission (`MountedAutomaticAttackPolicy`) and becomes the same intent a
click makes, so full-attack planning, cooldowns and the native strike sequence are unchanged. Native Run of the proposal
is always refused and the pair controller stays the one dispatcher. The mount's brain stays off under its existing lease.
The pair command is marked automatic: `CreatedByPlayer` false and no manual target written.

**Hold at every native Run.** `UnitCommands.Run(cmd, fromQueue, doNotClearQueue)` writes Hold in one place, clearing it for a
command without `AiAction`, both at admission and when queue advancement runs the command later. The later Run never reaches
the public Run's patches, and the old snapshot-and-restore covered only the first call and only the rider.
`NativeRunHoldBoundary` turns that one write into a decision by the command's identity (`MountedAutomaticRunLedger`), for
whichever actor executes. Nothing is snapshotted, so a Hold the player pressed or cleared between the two boundaries stays
as the player left it. Every other command clears Hold natively, and no `AiAction` or click is forged. A custom handler's
prerequisite (a kineticist blade's activation) keeps Hold only for the exact automatic command it precedes.

**A queued automatic command is re-admitted at its later Run.** A finished, retired, stale-generation or dismounted one never
executes. A Hold that arrived while it waited removes its permission to approach. A click on its target makes it the
player's own, so that later Run has the native player effect. One the queue dropped is swept so it cannot block the pair.

**Precedence.** A proposal is refused while any of these is live: a click-owned intent, an armed action, a pair command,
a door, a door settlement, a carried approach, a ground move, a live rider order, or anything queued on either actor.
Refusal touches nothing, including a click's command-bound evidence. Further rules:

- A manual click replaces the automatic intent, or takes it over if it names the same target.
- A live rider order retires a repeating intent of either origin. This also corrects a pre-existing hole where a
  repeating click-owned dispatch could displace a cast still waiting to start.
- A Free or Swift command already in reach is not an order against the routine, so it does not end it.

**Stop.** Native `SelectionManagerBase.Stop` clears Hold, unstarted commands and both targets, and arms nothing, so a live
brain would propose again within 0.4 s. `MountedAutomaticAttackGate` therefore holds the selected pair's proposals until
a deliberate order (attack, move, cast, item, door or carried interaction), an explicit AI off and on, or the end of the
combat, mode or relationship generation. It is per pair and never saved. This is deliberately stricter than native Stop;
the mission forbids a next-tick restart.

**Move completion.** A new order only supersedes Stop. `MountedPlayerMoveWatch` follows the exact `UnitMoveTo` the native
ground click ran on the mount. Native `UnitMoveTo.OnAction` runs only on arrival and holds the mover only in real-time
combat, so the move's own result answers both questions: arrival in combat holds the pair, whenever it was ordered; arrival
out of combat, interruption, replacement or a move the queue dropped holds nothing. Delegated approaches never pass through
the ground click, so they never count. Dismount, generation and mode changes drop the pending move.

A routed ground click also releases the rider's own native Hold, as it would on an unmounted unit.

**The player's engagement option decides one branch.** Native `AutoPauseController` interrupts every party move when combat
starts unless the option `ContinueMovementOnEngagement` is on. With it off, a move ordered in exploration is interrupted, no
hold applies and automatic attacks may resume, which is the native result. With it on, the move arrives in combat and holds.

**AI off, Hold, pause.** AI off retires a live automatic intent. Under native Hold or a completed player move the pair only
attacks from where it stands, and an approach already under way retires. Pause needs nothing extra: the native brain does
not tick.

**Boundaries and spending.** Dismount, invalid relationship, turn-based combat and combat end clear the intent through
the existing paths. Nothing writes, refunds or synthesizes a resource. Each actor still pays its own native cost.
Automatic acquisition is real-time only.

## Coverage

### Executed natively on the final binary (r08, sprint4i)

| Outcome | Row | Measured |
| --- | --- | --- |
| Move ordered before combat, completed in combat | `S4-move-into-combat` | Ground click out of combat; combat joined 3 frames later with the move unfinished; the exact same move identity arrived 709 frames later: `Success`, acted, in real-time combat. Mount Hold true, gate armed, outcome `CompletedHeld`, 38.2 m from the target. |
| No chase from that destination | `S4-move-into-combat` | 3 s: displacement 0.0, 0 dispatches, 9 live proposals refused (`hold-out-of-reach`). |
| Interruption control | `S4-move-into-combat` | Native Hold pressed, then a new ground click: rider Hold, mount Hold and gate all released. Native `InterruptMove` before arrival: `Interrupt`, not acted, outcome `Interrupted`, no hold. An automatic dispatch then approached the target (displacement 1.0). |
| Automatic command queued, then run later | `S4-queued-hold` | Mount's automatic command (`CreatedByPlayer` false, `AiAction` null) in the native queue at frame 5004, unstarted, not in a slot, behind an uninterruptible command. At frame 5159 the same identity entered the slot through queue advancement: 1 queued run, 1 Hold kept. Rider and mount Hold true at both boundaries and after it finished. Result `Success`, displacement 0.0, the mount's own Standard spent. |
| Later out-of-reach target | `S4-queued-hold` | Target 37.7 m away, 3 s: 0 admissions, 0 dispatches, displacement 0.0, both Holds kept. The brain proposed nothing, because a native Hold filters out-of-reach targets itself. The next row shows that same brain proposing that same target once Stop cleared Hold. |
| Stop and re-arm | `S4-intent-neighbors` | 3 s: 0 dispatches, displacement 0.0, 9 proposals refused (`stop-suppressed`), rider Hold cleared natively. AI off and on resumed. |
| Manual replacement | `S4-intent-neighbors` | Click-owned intent on the new target; 0 attacks on the old target over 3 s. |

**The busy state is test-owned.** In the fixture an automatic command cannot queue behind an ordinary attack: a running
`UnitAttack` merges a same-target attack rather than queuing it, and an actor's own attack leaves it on cooldown for longer
than the attack lasts. So setup holds the mount's Standard slot with a test command that cannot be interrupted, spends nothing
and does nothing, and the mount's Hold is written during it. The queuing, the later Run and everything measured are native.
The one natural route is a kineticist blade's activation; the fixture has no kineticist, so that route is offline only. The
row ran on the mount; a rider-actor queue uses the same boundary and is offline only.

### Retained native evidence on earlier binaries

These ran before the corrections. Their rows were not rerun; `Stop`, manual replacement and the no-chase path were repeated
above. The retarget rows exercised the old snapshot of Hold and the old move hold, so they are retained for what they
measured, not as current-binary execution.

| Outcome | Run | Measured |
| --- | --- | --- |
| Clean UI and compatibility | r03 `S4-ui-clean` | 0 helper facts on either actor, none in the non-hidden ability list the action bar groups read, none on a hotbar; 1 Dismount; both GUIDs resolve. An injected stale slot and stale auto-use were each cleared exactly once; unrelated slots identical. |
| Ranged retarget, Haste and Rapid Shot | r03 `S4-ranged-retarget` | One click. A native Hit raised the first target's damage and it died the next frame. Then automatic routines on the unclicked target: native plan 3, routine plan 3, full attack, 3 of 3 strikes with distinct rule identities. Mount displacement 0.0; no mount attack out of reach; no overlap or duplicate. |
| Melee retarget and mount participation | r03 `S4-melee-retarget` | One click, native death. Rider and mount each attacked the next target on their own automatic commands and native cooldowns. |
| Stop | r03 `S4-controls` | 0 dispatches and 8 refused live proposals over 3 s, AI still on. Explicit AI re-enable resumed. |
| Manual replacement | r03 `S4-controls` | Click-owned intent on the new target; 0 attacks on the old target over 3 s. |
| Pause | r03 `S4-controls` | 0 proposals and 0 dispatches over 1 s of native pause. |
| Cast precedence | r03 `S4-controls` | Mount carried the cast; 0 automatic dispatches during it; native healing landed; automatic attacks resumed afterwards. |
| Hold and AI off | r03 `S4-controls` | Automatic dispatch under Hold in bow reach: Hold kept, displacement 0.0. AI off retired the live intent, reason `ai off`. |
| No chase after a move (order-armed gate, superseded by r08) | r03 `S4-hold-no-chase` | Ordinary ground order, 14 m from the target: displacement 0.0, 0 dispatches, 9 refusals (`hold-out-of-reach`). A click then released it and attacked. |
| Turn-based neighbor | r05 `S4-tb-ordinary-actors` | Rider and mount each attacked by ordinary selection and click. Rider Standard 0→6 with mount unchanged, then mount 0→6. 0 automatic admissions, same turn after 60 settled frames. |

### Offline only

Production policy, gate and intent behavior tests (`tests/MountedAutomaticAttackPolicyTests.cs`) cover: trap and queued
order precedence, door settlement, Swift/Free coexistence, cross-pair isolation of gate and intent, and generation and
mode boundaries.

`tests/MountedPlayerIntentBoundaryTests.cs` drives a small behavior model of the native command container through the
production ledger, watch and gate: immediate and queued runs on either actor, Hold changed between them, click promotion, a
same-target merge, a custom handler's prerequisite, Stop, retired, stale and dropped commands, negative controls for player,
AI, unmounted and foreign commands; then in-combat and exploration-to-combat completion, interruption, refusal, replacement, a
dropped queued move, delegated approaches, and another pair or generation. The model calls the same decisions the adapters
call; the transpiler itself has no offline test and is proved by the patch's exact-site check and r08.

The reader has 111 positive and negative checks (`scripts/Test-Sprint4NaturalReaders.ps1`).

### Not executed

- A native two-pair session (policy and state tests only, as the mission allows).
- A rider-first/mount-first pair of runs: admission changes apply only to brain proposals, and the turn-based path is
  unchanged, so one affected control was run. r05 was not rerun after the corrections: the Hold ledger only ever holds
  real-time automatic commands, and the only turn-based reach is a routed ground move clearing the rider's Hold, as native does.
- The engagement option off: seen once in r06's game log (move ended, automatic attacks resumed), with no recorded artifact,
  so it is an observation rather than a passed row. The decision itself is tested offline.
- A native unmounted Stop control: Stop's semantics were read from the installed assembly, not measured.
- A real older save holding a helper slot: no such save exists, because the serializer strips them. The pre-change
  Working fixture was loaded and the stale references were injected instead.
- Horse pairs, Sprint 3's trap attempt and the Sprint 2 turn-based door remain unexecuted. None is a prerequisite.

### Retained

Unmounted full-attack controls from earlier ordinary-attack evidence. Rapid Shot and Haste counts here are compared with
the native plan computed on the same actor, not with a new unmounted run.

## Evidence

Under `runtime-evidence\`. Eight launches. Seven results record Mods restored and save protection passed. r06 records neither,
because its launcher blocked restoration; it was then fully recovered (below). The live Mods tree was empty at closeout.

| Run | Candidate | Verdict | Cause |
| --- | --- | --- | --- |
| `20261101-sprint4-r01-rt` | sprint4b | FAIL | Fixture: the Druid cannot equip a longbow. Also schema 48 missing from the harness registration. The UI row recorded. |
| `20261101-sprint4-r02-rt` | sprint4c | FAIL | First native observation of automatic retargeting. The row stalled because the stock Mammoth carries the dying condition, so the target fell unconscious; the host deadline then lost the artifact. |
| `20261101-sprint4-r03-rt` | sprint4d, DLL `b4d88d4a…` | **FAIL, re-evaluated PASS** | All five rows recorded. Original FAIL: new rows not yet registered in the reader. See below. |
| `20261101-sprint4-r04-tb` | sprint4e | FAIL | Turn-based click needs a native prediction first; child preamble did not know the family. |
| `20261101-sprint4-r05-tb` | sprint4f, DLL `53671a48…` | **PASS** end to end | |
| `20261102-sprint4-r06-intent` | sprint4g, DLL `83c895d3…` | FAIL, restoration blocked | The engagement option was off, so the exploration move was interrupted natively and the row's last step stalled with no bound. The launcher's 360 s wait expired before the game's 460 s deadline. No scenario artifact. |
| `20261102-sprint4-r07-intent` | sprint4h, DLL `535e2cd0…` | FAIL | Move row passed. Fixture: disposing the first target's service released its AI lease on other party members, so the next spawn's guard refused. The guard was not changed. |
| `20261102-sprint4-r08-intent` | sprint4i, DLL `8236574a…` | **FAIL, re-evaluated PASS** | All three rows recorded. Original FAIL: "later target was not refused under the kept Hold", a wrong reader expectation. See below. |

**r06 recovery.** Transaction recovery restored Mods and saves. `Recover-KmcPersistenceProfile` then refused: the run's
isolated `Areas` tree is settled only on the launcher's normal exit. The recovery tool now performs that same reviewed
settlement under its lock, bound to the recovered transaction's token. Result: Params and PlayerPrefs equal to the snapshot,
profile unchanged, original `Areas` restored exactly, the generated cache kept in the run's backup, lock closed, game absent.
Later launches use the 600 s outer wait, and the scenario deadline sits inside the host deadline.

**r08 reader-only re-evaluation.** The observations are complete and unchanged. A native Hold makes the brain filter
out-of-reach targets, so the far-target window has no proposal to refuse. The corrected reader requires instead: Holds kept,
AI and brain live, no intent, 0 admissions and 0 dispatches, displacement 0.0, and the next row's Stop refusing that exact
target. Admissions matter because the rider's cooldown was running during that window, so 0 dispatches alone would prove little.
Both verdicts are retained in `analysis-cache\sprint4\20261102-sprint4-r08-intent-reader-reevaluation.json` (evidence
SHA-256 `5a7d4089…`, reader commit `fec310ff`). No rerun for a label.

**r03 reader-only re-evaluation.** The observations are complete and unchanged, so per the validation policy the corrected
reader chain ran on the immutable evidence. Both verdicts are retained in
`analysis-cache\sprint4\20261101-sprint4-r03-rt-reader-reevaluation.json` (evidence SHA-256 `524fdee8…`, reader commit
`fdb3adf5`). The reader corrections were: register the row names and the preamble family, and prove the death from its
native hit and resolution, because the observer never records outgoing damage. r03 was not rerun for a green label. Between
r03 and the final candidate `src/` changed only by one row-registry line and the turn-based row's prediction call, so
`S4-*` real-time bytes other than that registry are as run.

Full hashes: sprint4i package `3801c2d79406c29fef61ff3fb7918ed029ec388a71b8cc8289c320a9b44556a1`, DLL
`8236574af6d6d3038a7b2307a1de376916ffb52843b6b8c4b86190a786bf0e21`, suite `sprint4i-526c32e6-20261101`, SHA-256
`48447d8a40733f7acf8d88d345481f8ec98c17ecd8bab169231e30ef5e4364f7`. sprint4f package `e4a685dd01613eb699ff27ab667f508069f99c35272a708776ed1f5c107ae0ce`, DLL
`53671a485d9a387e57c9d9864a6034aeb7bd1deaa353d789413ad88a394b60f0`; sprint4d DLL
`b4d88d4a1b312b93efb88b96d265c7027492d8b4e5e571b7ee202037549300b3`, MVID `a8d5b0b5-575d-4fbb-bbfb-f6b657a435e3`. Suite
`sprint4f-fdb3adf5-20261101`, SHA-256 `cc996b36fa5e2b5ad8933fbe5be8b47be6cf877972d1760ef82e9b50cada56a7`.

## Validation

| Check | Result |
| --- | --- |
| `Test-Tiers -Tier Candidate -SkipBuild -AssemblyTarget Kingmaker -Focused Test-Sprint4NaturalReaders.ps1` on `3e04a9cb` | PASS 7/0, 3.44 min. Also PASS on `8d2eb401`, 3.52 min. Earlier: `fdb3adf5`, 388 s. |
| After `3e04a9cb` (readers and recovery guard only) | Reader regressions 111 PASS; `Test-Chunk7ProfileAreasIsolation` 39/0 |
| Unit tests | 1008 PASS, 0 FAIL |
| Native attempts | 8 launches, batches `sprint4:accept-1` to `-5`; r06 to r08 each had a distinct cause |
| Independent reviews | First pass, before packaging: 0 blockers, 3 majors and 4 minors; three minors and the majors corrected. Corrections pass on `bdf3c5e3`: 1 blocker in the row (it could not reach the queue), 2 majors, 6 minors; the blocker, majors and four minors corrected. Bounded delta pass: no production defect, 5 evidence or recovery findings; the medium and two low findings corrected. |
| Budget `sprint4` | See `CURRENT.md`; ceiling 90 minutes, no adjustment |

No Full tier, historical campaign, persistence matrix or purity sweep was selected.

## Residual risks

1. **Melee rider under native Hold.** The native brain filters a held unit's targets with the rider's own corpulence. A
   target within the pair's reach from the larger mount but outside that radius gets no proposal, so the rider waits
   (measured in `S4-hold-no-chase`: 0 dispatches). No chase and no wrong attack; a click still attacks. The bow row shows
   the automatic dispatch working under Hold.
2. **Auto-use abilities.** A brain proposal that is a `UnitUseAbility` (a rider with an auto-use ability) keeps its previous
   behavior and is not routed through this path.
3. **Mammoth only.** Native rows used the disposable Mammoth pair. Horse pairs share the code but were not run here.
4. **"Absent from the UI" is structural.** The evidence is the absent fact and the ability list the action bar reads, not
   a screenshot.
5. **Recovery tool edges, from the bounded review; both fail safe.** A timed-out run leaving both an unsettled `Areas` tree and
   a new empty achievement-cache leaf is refused rather than recovered. A crash between the `Areas` settlement and its receipt
   leaves that receipt unwritten on a rerun.
6. **Hold press in the move row.** The row records rider Hold released after the second click and the press before it, but no
   separate step with Hold true between them. The queued row, using the same input, records that write.

## Owner play checklist

1. With a mounted character, open the ability drawer and hotbar: no Rider Primary or Mount Primary; Mount and Dismount work.
   Load an older save: hotbar otherwise as you left it.
2. Real time, AI on, melee rider: click an enemy. When it dies, the rider should move to the next enemy unprompted, and the
   mount should join when in reach.
3. Same with a bow: the pair should stay at range, with the mount not walking into melee.
4. During automatic attacks press Stop: everything should stop and stay stopped until you give an order or toggle AI.
5. Click a different enemy mid-attack: the pair should switch and stay on it.
6. Cast a touch spell mid-combat on a distant ally: the mount carries you, the spell lands, and attacks resume.
7. Order a move in combat: the pair should stop at the destination and not chase. Interrupt or replace a move: automatic
   attacks should not stay restricted.
8. Order a move out of combat, then start a fight before it arrives. With the option to continue moving on engagement on, the
   pair should finish the move and stay there; with it off, the move stops at the start of combat.
9. Press Hold in bow range: the pair keeps shooting and does not move when no enemy is left in reach.
10. Turn-based: attack with rider and mount by ordinary selection and click, with no automatic attacks or End Turn.
11. Sprint 3's manual trap check remains yours and is separate.
