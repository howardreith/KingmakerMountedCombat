# Sprint 4: natural attacks and real-time retargeting

**SPRINT 4 ENGINEERING COMPLETE — READY FOR REVIEW; OWNER PLAY ACCEPTANCE SEPARATE.**

Automated evidence is not user acceptance, and user acceptance is not automated evidence. This workflow selects no
merge, release, tag or installation.

## Baseline and identities

| Item | Value |
| --- | --- |
| Integrated baseline | `main` `2c2e6fc6` (PR #8 merged 2026-11-01, pre-release `v0.1.0-sprint3-preview.100`) |
| Branch | `codex/mounted-combat-sprint4-natural-attacks-retargeting` |
| Final candidate | sprint4f, commit `fdb3adf5`, DLL `53671a48…` (full hashes under *Evidence*), MVID `d1f732a4-89b9-4379-b439-ef96d59d2f05`, 8,522,752 bytes (limit 9 MiB unchanged) |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.100-sprint4f-diagnostic.zip`, SHA-256 `e4a685dd…` |

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
The pair command is marked automatic: `CreatedByPlayer` false, no manual target written, and a native Hold kept.

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

**AI off, Hold, pause.** AI off retires a live automatic intent. Under native Hold, or after a completed player move
(which natively holds the unit that moved, but here the mount moves), the pair only attacks from where it stands. Pause
needs nothing extra: the native brain does not tick.

**Boundaries and spending.** Dismount, invalid relationship, turn-based combat and combat end clear the intent through
the existing paths. Nothing writes, refunds or synthesizes a resource. Each actor still pays its own native cost.
Automatic acquisition is real-time only.

## Coverage

### Executed natively

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
| No chase after a move | r03 `S4-hold-no-chase` | Ordinary ground order, 14 m from the target: displacement 0.0, 0 dispatches, 9 refusals (`hold-out-of-reach`). A click then released it and attacked. |
| Turn-based neighbor | r05 `S4-tb-ordinary-actors` | Rider and mount each attacked by ordinary selection and click. Rider Standard 0→6 with mount unchanged, then mount 0→6. 0 automatic admissions, same turn after 60 settled frames. |

### Offline only

Production policy, gate and intent behavior tests (`tests/MountedAutomaticAttackPolicyTests.cs`) cover: trap and queued
order precedence, door settlement, Swift/Free coexistence, cross-pair isolation of gate and intent, and generation and
mode boundaries. The reader has 67 positive and negative checks (`scripts/Test-Sprint4NaturalReaders.ps1`).

### Not executed

- A native two-pair session (policy and state tests only, as the mission allows).
- A rider-first/mount-first pair of runs: admission changes apply only to brain proposals, and the turn-based path is
  unchanged, so one affected control was run.
- A native unmounted Stop control: Stop's semantics were read from the installed assembly, not measured.
- A real older save holding a helper slot: no such save exists, because the serializer strips them. The pre-change
  Working fixture was loaded and the stale references were injected instead.
- Horse pairs, Sprint 3's trap attempt and the Sprint 2 turn-based door remain unexecuted. None is a prerequisite.

### Retained

Unmounted full-attack controls from earlier ordinary-attack evidence. Rapid Shot and Haste counts here are compared with
the native plan computed on the same actor, not with a new unmounted run.

## Evidence

Under `runtime-evidence\`. Every one of the five results records Mods restored and save protection passed, and the live Mods tree was empty at
closeout.

| Run | Candidate | Verdict | Cause |
| --- | --- | --- | --- |
| `20261101-sprint4-r01-rt` | sprint4b | FAIL | Fixture: the Druid cannot equip a longbow. Also schema 48 missing from the harness registration. The UI row recorded. |
| `20261101-sprint4-r02-rt` | sprint4c | FAIL | First native observation of automatic retargeting. The row stalled because the stock Mammoth carries the dying condition, so the target fell unconscious; the host deadline then lost the artifact. |
| `20261101-sprint4-r03-rt` | sprint4d, DLL `b4d88d4a…` | **FAIL, re-evaluated PASS** | All five rows recorded. Original FAIL: new rows not yet registered in the reader. See below. |
| `20261101-sprint4-r04-tb` | sprint4e | FAIL | Turn-based click needs a native prediction first; child preamble did not know the family. |
| `20261101-sprint4-r05-tb` | sprint4f, DLL `53671a48…` | **PASS** end to end | |

**r03 reader-only re-evaluation.** The observations are complete and unchanged, so per the validation policy the corrected
reader chain ran on the immutable evidence. Both verdicts are retained in
`analysis-cache\sprint4\20261101-sprint4-r03-rt-reader-reevaluation.json` (evidence SHA-256 `524fdee8…`, reader commit
`fdb3adf5`). The reader corrections were: register the row names and the preamble family, and prove the death from its
native hit and resolution, because the observer never records outgoing damage. r03 was not rerun for a green label. Between
r03 and the final candidate `src/` changed only by one row-registry line and the turn-based row's prediction call, so
`S4-*` real-time bytes other than that registry are as run.

Full hashes: sprint4f package `e4a685dd01613eb699ff27ab667f508069f99c35272a708776ed1f5c107ae0ce`, DLL
`53671a485d9a387e57c9d9864a6034aeb7bd1deaa353d789413ad88a394b60f0`; sprint4d DLL
`b4d88d4a1b312b93efb88b96d265c7027492d8b4e5e571b7ee202037549300b3`, MVID `a8d5b0b5-575d-4fbb-bbfb-f6b657a435e3`. Suite
`sprint4f-fdb3adf5-20261101`, SHA-256 `cc996b36fa5e2b5ad8933fbe5be8b47be6cf877972d1760ef82e9b50cada56a7`.

## Validation

| Check | Result |
| --- | --- |
| `Test-Tiers -Tier Candidate -SkipBuild -AssemblyTarget Kingmaker -Focused Test-Sprint4NaturalReaders.ps1` on `fdb3adf5` | PASS, 388 s (Test-Harness 374 s). An earlier Candidate caught one stale schema pin, updated. |
| Unit tests | 987 PASS, 0 FAIL, including the new policy tests |
| Native attempts | 5 launches, batches `sprint4:accept-1` to `-3`; 3 distinct failure causes changed the experiment each time |
| Independent delta review | One, before packaging: 0 blockers, 3 majors and 4 minors. Majors and three minors corrected; one minor recorded below. |
| Budget `sprint4` | About 52 of 90 minutes at closeout; no adjustment |

No Full tier, historical campaign, persistence matrix or purity sweep was selected.

## Residual risks

1. **Melee rider under native Hold.** The native brain filters a held unit's targets with the rider's own corpulence. A
   target within the pair's reach from the larger mount but outside that radius gets no proposal, so the rider waits
   (measured in `S4-hold-no-chase`: 0 dispatches). No chase and no wrong attack; a click still attacks. The bow row shows
   the automatic dispatch working under Hold.
2. **Move hold edges.** The hold is armed at the order, not at arrival. A move ordered before combat that arrives in combat
   can chase, where native would hold; an interrupted move holds, where native would not.
3. **Auto-use abilities.** A brain proposal that is a `UnitUseAbility` (a rider with an auto-use ability) keeps its previous
   behavior and is not routed through this path.
4. **A rare Hold edge.** If an automatic command is queued behind an animation tail, its later native Run can clear Hold.
5. **Mammoth only.** Native rows used the disposable Mammoth pair. Horse pairs share the code but were not run here.
6. **"Absent from the UI" is structural.** The evidence is the absent fact and the ability list the action bar reads, not
   a screenshot.

## Owner play checklist

1. With a mounted character, open the ability drawer and hotbar: no Rider Primary or Mount Primary; Mount and Dismount work.
   Load an older save: hotbar otherwise as you left it.
2. Real time, AI on, melee rider: click an enemy. When it dies, the rider should move to the next enemy unprompted, and the
   mount should join when in reach.
3. Same with a bow: the pair should stay at range, with the mount not walking into melee.
4. During automatic attacks press Stop: everything should stop and stay stopped until you give an order or toggle AI.
5. Click a different enemy mid-attack: the pair should switch and stay on it.
6. Cast a touch spell mid-combat on a distant ally: the mount carries you, the spell lands, and attacks resume.
7. Order a move in combat: the pair should stop at the destination and not chase.
8. Turn-based: attack with rider and mount by ordinary selection and click, with no automatic attacks or End Turn.
9. Sprint 3's manual trap check remains yours and is separate.
