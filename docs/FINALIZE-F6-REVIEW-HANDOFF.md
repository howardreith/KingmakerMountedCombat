# F6 review handoff: logged rider falls and final integrated acceptance

## Engineering acceptance and integration closeout (2026-11-07)

**F6 ENGINEERING ACCEPTED IN REVIEW AT `4b8a5f02`; SELECTED REFINEMENT ENGINEERING COMPLETE WITH WORLD-02 DEFERRED; OWNER ACCEPTANCE SEPARATE.**

### Review and integration

- **Review:** the owner-forwarded closeout accepted PR #21 at head `4b8a5f0293e190bf945ac2501df7478ecae867a1` in engineering review. It found no new blocking defect in the completion delta.
  - The reviewer inspected the source, the comparisons, the completion scenario, the corrected readers, the synthetic rejection tests, the re-evaluation script and this handoff.
  - The reviewer did not run Kingmaker, and did not inspect the private runtime receipts, the package bytes, the screenshots or the machine restoration.
- **Integration (owner):** the owner merged PR #21 at 2026-11-07T20:27:35Z.
  - Merge commit: `eb16d2f15b1b8e3e53de667a637c70205e9649f8`.
  - Parents: `730f72d1` (the F5 landing) and `4b8a5f02`.
  - The merge tree `b965f288…` is identical to the reviewed head's tree, so the landed source is exactly the reviewed source.
  - Gameplay code was last changed at `c7ca5989`. The frozen launch source `5fe80073` differs from the landing only in readers, tests and records.
  - The closeout made no game launch, rebuild or repackage.
- **Not authorized and not done:** release, tag, publication and permanent installation.

### Accepted candidate (verified read-only at closeout)

| Item | Value |
|---|---|
| Candidate | finalize-f6f; version label `0.1.0-chunk7b-preview.103`. This label is a shared development version, not a unique identity. |
| DLL | SHA-256 `87fed1617da327e49497f5e5bde32c10448faf0efe01362c8a14890e322fa096`, MVID `a7638ec4-9c19-4d03-9166-4cb7bf59331b` |
| Package | `KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f6f-diagnostic.zip`, SHA-256 `74baa915d6e7c8b0561e4f6d2db928009d615e3441b36f4ce4bc36ce46fb56ce`. The hash was re-checked at closeout and matches. |
| Location | Lab file `C:\Dev\KingmakerMountedCombatLab\artifacts\…-finalize-f6f-diagnostic.zip`. It is not a download link, and no private handoff destination has been created. |
| Suite | `finalize-f6f-5fe80073-20261107`, snapshot SHA-256 `82b08cee7544117c07df2e05e06bf16b99666d48bc9fb79f462be38c71ad14ca` |

### Verdicts kept as recorded

- **r05 `20261107-finalize-f6f-completion-05`:**
  - ORIGINAL OUTER FAIL, kept.
  - NATIVE GAME PASS.
  - The corrected-reader re-evaluation (`scripts/Test-Finalize6CompletionReevaluation.ps1`) passes.
  - It ran the declared contract (setup plus both fall rows) and claims no F3 action row.
- **r03 `20261107-finalize-f6c-producer-03`:** ORIGINAL OUTER FAIL / NATIVE GAME PASS, separately re-evaluated.
- **r04 `20261107-finalize-f6e-consumer-04`:** PASS.
- Both r03 and r04 ran on the earlier DLL `b18cc056…`, not the final diagnostic DLL.
- **r01 and r02:** FAIL, kept.

### Player notes

- **No action change from the fall:** the fall service does not spend or refund any action. The existing pair detachment, however, ends the interrupted paired activation.
- **Stand-up:** on the rider's next turn, the native stand-up takes the Move and cancels the order that triggered it. A later action is still subject to the rider's normal resources.
- **DC:** the base DC is 15, and the game's difficulty adjustment is kept; it was 11 on the fixture. The test rolls were deterministic and disclosed. Ordinary play rolls natively at random.
- **Not a feat:** this is not the Mounted Combat feat, and it needs no Swift action or reaction.

### Final support and acceptance map

| Area | Status |
|---|---|
| F1–F6 selected refinement engineering | Complete. Each sprint was accepted and merged by the owner (F6 as `eb16d2f1`), on its own recorded artifacts. |
| WORLD-02 mounted trap-order native validation | **Deferred, not passed**, under the owner's F4 decision ([deferred items](FINALIZATION-DEFERRED-ITEMS.md)). Closeout does not reopen it. |
| Engine and mod stack | Kingmaker 2.1.7b observed. The lab ran with the retained fixture dependencies, including CotW/CMI. A BaseGameOnly flag with those dependencies proves neither a KMC-only run nor the owner's full mod stack. |
| Fall coverage | Collapse to unconscious, condition and immunity combinations, and non-gameplay lifecycle exclusions are covered by source and offline tests, not by every native combination. A save taken while a rider is still prone is not proven. |
| UI | The combat-log fall line was observed. Physical input and previously untested foreign UI surfaces (other log lines, tooltips, Buff Planner screens) are not claimed. The F3 post-success refusal banner and the disabled-slider polish notes still stand, and no code is selected for them. |
| Saves | Custom KMC content can bind a save to the mod, and clean removal from dependent saves is not generally proven. Roll back with pre-upgrade saves and the matching package and dependencies. Pre-F2 binaries do not support F2-written saves. |
| Owner style and full-stack play acceptance | **Separate and pending**; not marked accepted on the owner's behalf. |
| Publication | A separate, explicit decision. |

### Final accounting and state

- **Budget:** finalize-f6 spent 42.75 of 90 minutes and 5 of 6 starts. The ceiling went from 4 to 6 append-only, the sixth start was never used, and closeout spent nothing.
- **Machine state:** no Kingmaker process, live run transaction or waiter is owned by this assignment.

### Owner playcheck

Use a disposable, backed-up save and the candidate above.
1. Read the bottom-right outcome when a ridden mount dies or falls prone.
2. After a TB hard landing, give the rider an ordinary order on its next turn. The stand-up takes the Move.
3. A mount that falls prone and then dies does not throw its already-unseated rider twice.
4. Save after the fall, reload, and confirm there is no extra fall, check or damage. Remount through the ordinary controls.

Actual owner results are recorded when supplied. A defect found there selects its concrete causal correction, not a new sprint.

## Disposition after the completion mission (2026-11-07)

**F6 ENGINEERING COMPLETE — READY FOR FINAL REVIEW; OWNER ACCEPTANCE SEPARATE.**

The mission (review baseline PR #21 at `ea4fcbc9`) raised the same finalize-f6 record's start ceiling from 4 to 6, append-only. The 90-minute ceiling is unchanged. One focused turn-based run (start 5) demonstrated every outcome that had remained open:
- the safe landing at the exact threshold;
- the native turn-based prone fall;
- prone-then-death with no second throw;
- the native stand-up paying exactly one Move on the rider's own next turn.

Its recorded verdict is FAIL from reader-only defects, now corrected, and the complete corrected reader chain passes over its untouched evidence. The contingency start 6 is unused.

### What changed in this mission

- **Completion variant `finalize6-falls-completion-tb`:**
  - It reuses the F3 encounter setup: both pairs mounted, pair A rider-first and pair B mount-first, a durable hostile, turn-based combat.
  - It then runs the two F6 fall rows directly, without the four F3 action rows. In r02 one of those rows refused before any fall row ran.
  - Its own contract (`finalize6-falls-completion-v1`), schema 55, and it claims no F3 row.
- **Native stand-up contract (installed `TurnController.HandleUnitWillGetUp`):** `m_Cooldown.MoveAction += 3f; Unit.TryCancelCommands()`. The stand-up costs the Move and cancels the triggering order.
  - The A row records the stand-up at the frame prone clears: the turn it was ordered on, native `CanStandUp`, the Move and Standard cooldowns, and the travel after the order.
  - The reader reads the cost on the Move cooldown. While the Standard is unused, native `HasMoveAction()` is `!(MoveAction > 3)`, so it stays true after one Move; the independent review caught this.
- **Snapshots** carry each rider's Mounted Combat reaction state.
- **Healing exclusions:** the measured riders are excluded from the fixture healing for their windows (disclosed). The A rider rejoins once its fall damage is recorded.
- **Product code:** none changed. `src/KingmakerMountedCombat/Integration` and `Domain` are identical to `c7ca5989`.

### Identities

- **Frozen and launched head:** `5fe80073113712e31ab971636b3e76b69a3085b7`, attached branch.
  - Later commits change readers and records only: `b9ac2175` and the records commits. The DLL rebuilt at `b9ac2175` is byte-identical.
- **DLL:** `87fed1617da327e49497f5e5bde32c10448faf0efe01362c8a14890e322fa096`, MVID `a7638ec4-9c19-4d03-9166-4cb7bf59331b`. Only the diagnostics differ from r03/r04's `b18cc056…`.
- **Owner-test package:** `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f6f-diagnostic.zip`, SHA-256 `74baa915d6e7c8b0561e4f6d2db928009d615e3441b36f4ce4bc36ce46fb56ce`.
  - A local lab path, not a download link. Not published or installed.
  - The retained f6e package `7109cdfd…` carries the same gameplay code with the earlier diagnostics (r04).
- **Suite:** `finalize-f6f-5fe80073-20261107`, snapshot SHA-256 `82b08cee7544117c07df2e05e06bf16b99666d48bc9fb79f462be38c71ad14ca`.
- **Native target:** Kingmaker 2.1.7b, BaseGameOnly, with the retained Working-fixture dependency selection. Not the owner's full mod stack.

### r05 `20261107-finalize-f6f-completion-05` (start 5): ORIGINAL OUTER FAIL / NATIVE GAME PASS; corrected re-evaluation PASS

- **Rows in the game:** child PASS, 3/3 (setup, A death fall, B prone fall).
- **Recorded verdict FAIL (reader-only):** "Runtime game-result contains an unknown subscenario: F6-tb-a-death-fall". The F6 rows were missing from `Get-KmcPhase3dHorseRuntimeRows`; r01 stopped earlier and never reached this check. The F6 reader also had two wrong expectations, both corrected:
  - the unseated pair reads `None` once its custody retires;
  - pair B's activation ended through the paired split one frame after the resolution snapshot, with no End Turn input.
- **Re-evaluation:** `scripts/Test-Finalize6CompletionReevaluation.ps1 -RunId 20261107-finalize-f6f-completion-05` passes. It runs the real game-result reader with `-RequirePass` (39/0), the native Mammoth profile (including the child-entry preamble and the F6 reader) and the Phase 3D evidence, and checks that the evidence files are byte-unchanged. The recorded FAIL is kept.
- **A — hard landing, then the native stand-up on the correct turn:**
  - **Fall (round 1, pair A's own activation):** genuine Mastodon death. One fall: d20 2 + Mobility 8 = 10 against DC 11 (DC 15 with the native difficulty adjustment of −4). Native 1d6 = 2, native prone, landing 2.86 m from the mount.
  - **The fall changed nothing else:** cooldowns before and after were equal, and the reaction state was unchanged.
  - **Other pair:** pair B stayed mounted.
  - **Activation end:** the existing paired split ended the interrupted activation, as in r01.
  - **Next turn (round 2, the rider's own):** still natively prone, fresh cooldowns, `CanStandUp` true. One ordinary ground order on that same turn produced the native stand-up.
  - **At the frame prone cleared:** Move cooldown 0 → 3.0 s, Standard 0 → 0. Travel after the order 0.0 m, and no order remained.
  - **No replay:** fall service 1/1 before and after standing.
- **B — safe landing from a genuine TB prone:**
  - **Fall (round 2, pair B's own activation):** the Horse was knocked prone by the native knock-off writer. One fall: d20 9 + Mobility 2 = 11 against DC 11, the exact threshold, so it passed. Landing 2.52 m from the mount.
  - **Outcome:** safe landing. No damage, no new prone, cooldowns 0 before and after, reaction state unchanged.
- **C — prone, then death:**
  - The repeat knock-off found the mount already prone (refused natively).
  - The unseated prone mount was then killed: 27 native damage, then dead.
  - No new fall began or resolved (begun since unseating 0; service 2/2 throughout).
  - Ten frames after the death the former rider still had 0 damage and was not prone.
- **Preparations:** at most one native preparation per actor per round.
- **Restoration:** Mods, Working, save protection and write allowlist all restored. Digest `60e3d6cc…`, as in every F6 and F5 run.
- **Inspected frames (private, normal camera):**
  - `finalize3-f6-a-stood-up.png` (round 2): the native action panel shows **"Stand up" in the MOVE ACTION slot**. The rider stands and the log shows the hard-landing lines.
  - `finalize3-f6-b-prone-fall.png`: the log reads "vbnm: Mobility check succeeded / Horse falls prone; vbnm is thrown: Mobility 11 vs DC 11 - lands safely / Horse receives 27 damage / Horse dies! / Round 3 started". There is no second fall line.

### Coverage map (current against retained)

| Outcome | Evidence |
|---|---|
| FALL-01 death, hard landing (TB) | r05 (current DLL `87fed161`); r01 earlier at f6b `b0031038`, kept FAIL |
| Native stand-up pays exactly one Move on the rider's next native turn, with no travel and no replay | r05 |
| FALL-02 new prone, safe landing at the exact threshold (TB) | r05 |
| FALL-02 new prone, hard landing (RT), equal-name isolation, voluntary Dismount and remount with 0 falls | r03 (separate re-evaluation; DLL `b18cc056`) |
| Prone, then death: no second throw | r05 |
| Post-fall save and fresh-process reload | r03 → r04 PASS, cross-run PASS. **The Manual holds an unmounted, standing rider with saved damage. No save was taken while a rider was still prone, and that is not claimed.** |
| Collapse to unconscious, immunities, lifecycle exclusions, reload seats | offline and source (`MountedRiderFallTests`) |
| F1–F5 integration | retained, on their own artifacts. F3 rows r01 (f6b). F5 names/tack r03/r04. |
| WORLD-02 | **deferred, not passed** |
| Owner art, full-stack play acceptance | separate |

### Budget and state

- **finalize-f6:** 42.75 of 90 minutes and 5 of 6 starts (ceiling 4 → 6 append-only; a seventh is refused). This mission's imports cover the direct reader and re-evaluation runs.
- **Checks:** Candidate 11/0 at `5fe80073`. Test-Finalize6Readers 76/0, Test-Finalize6PinnedSource 9/0, the F3 readers, and the components (1118/0) and Validate-Source (149/0) tiers.
- **State:** no game, lock or waiter remains.

### Owner playcheck (disposable save)

1. In a turn-based fight, let a ridden mount die. Read the log line, and on the rider's next turn give any order. "Stand up" should take the Move.
2. Trip or knock a mount prone. A rider with a good Mobility often lands safely, and the log says so.
3. Kill a mount that is already down. Its former rider should not be thrown again.
4. Save, reload and confirm that nothing replays. Remount with the ordinary control.

**Stop at review.** No merge, tag, release, install or publication. WORLD-02 stays deferred.

## Superseded disposition after the first F6 mission (2026-11-07)

**F6 ENGINEERING PARTIAL — FALL-01 (TB mount death) AND FALL-02 (RT prone) SHOWN NATIVELY WITH HARD LANDINGS, AND THE POST-FALL SAVE/RELOAD PASSES; THE SAFE-LANDING BRANCH, TB PRONE, PRONE-THEN-DEATH AND THE TB STAND-UP COST ARE NOT SHOWN NATIVELY (4 OF 4 STARTS USED); OWNER ACCEPTANCE SEPARATE.**

The product change is complete and reviewed. Native evidence covers:
- one genuine mount death in turn-based combat;
- one genuine new prone in real time with the identically named second pair untouched;
- the harmless voluntary Dismount and remount;
- the native Manual of the post-fall world and its fresh-process reload.

The turn-based prone/success row never ran. Start 2 stopped in an unchanged F3 row before reaching it, and no fifth start is authorized. This is not "ENGINEERING COMPLETE".

## Baseline and identities

- **F5 landing:** main `730f72d1` (owner merge of PR #20). Its tree equals the reviewed head `badef7f0`. The branch is `codex/kmc-finalize-f6-falls-final-acceptance`.
- **Engine and dependencies:** Kingmaker 2.1.7b, with Harmony12 as installed. The lab fixture runs BaseGameOnly with `analysis-cache/sprint1/retained-dependencies-final211.json` (the Working fixture's pinned CotW/CMI entries). This is not the owner's full mod stack.
- **Product source:** final product code at `c7ca5989`. `src/` is unchanged from `c7ca5989` through the frozen head `fbef8638`; later commits are harness, readers and records only.
- **DLL:** `b18cc0560b98d213abd542a48e0d7781eb1859ff86298cee16a8030ea72d7742`, MVID `2a9d61d7-99b2-4995-be43-3210a087ccae`. Byte-identical across packages f6c, f6d and f6e.
- **Owner-test package:** `C:\Dev\KingmakerMountedCombatLab\artifacts\KingmakerMountedCombat-0.1.0-chunk7b-preview.103-finalize-f6e-diagnostic.zip`, SHA-256 `7109cdfde521ecaa6808dd1fcbf29111ba5db0611128801ea0ba7ea4b333eb01`.
  - It is built from `fbef8638`, the consumer's frozen head.
  - It is a local lab path, not a download link. It is not published or installed.
- **Suite:** `finalize-f6e-fbef8638-20261107`, snapshot SHA-256 `916b898cb86de695f1d667bba6d395204b95596af5f79ac4c22693b2e6685992`.
- **Earlier candidates (kept):**
  - f6a `58cb6b21…`, superseded by a harness pin before any start.
  - f6b `0d9d7f98…`, DLL `b0031038…`, MVID `df0953ff…` (r01).
  - f6c `006b5653…` (r02, r03).
  - f6d `7506fbcc…`, superseded by a test-fixture fix before any start.

## What changed (FALL-01, FALL-02)

- **Trigger:**
  - `MountedLifecycleSubscriber` captures a fall for the exact ridden mount before the existing cleanup unseats the rider.
  - The three causes:
    - death: native `HandleUnitDeath`, raised before the life-state event;
    - collapse: Conscious to Unconscious;
    - new prone: `IUnitFallProneHandler`. This now unseats with `CleanupTrigger.MountFellProne`.
  - The rider's own prone, death or incapacity carries no fall cause. So do voluntary Dismount and every lifecycle, area, load, view, disable and party cleanup.
- **Exactly once:**
  - `MountedRiderFallLedger` keys a fall on the relationship and its exact mounted custody object (`PairOwner`).
  - The fallback when no custody object exists is the relationship and its generation, compared by value.
  - Later callbacks of the same seat replay nothing. A remount, or a save restored by a load, is a new seat.
  - A fall resolves once the rider is actually unseated, because a charge barrier may defer it.
  - Loading, restoration, an owned save or an area transition abandons anything pending.
- **Resolution (`MountedRiderFallService`):**
  - **The check:** one native `RuleSkillCheck` on Mobility, DC 15, with the native difficulty adjustment kept.
    - Both native rows showed DC 11 because the fixture's difficulty adjusts by −4. The log states the DC actually rolled against.
  - **Success:** no damage and no new prone.
  - **Failure, or no check possible:** a rider whose native conditions permit no check (unconscious, helpless, stunned, dazed, cannot act) is treated as failing.
    - It takes one native 1d6 bludgeoning `RuleDealDamage`, initiated by the rider. Native reduction, immunity and temporary hit points apply, and no difficulty scaling applies to a party rider.
    - Prone comes through the native knock-off rule.
  - **Log:** one explanatory combat-log line using the current display names.
  - **Untouched:** no action, cooldown, Swift, reaction, feat or initiative state is written, and the Mounted Combat feat is not involved.
- **Unchanged paths:**
  - Placement, command release and the paired-activation split are the existing dismount path. Placement is native `FreePlaceSelector` around the mount.
  - Persistence adds no state. `ProneState` is native serialized state.
- **Native observation (r01; existing behaviour, recorded rather than changed):** when the mount dies during the pair's own activation, the existing paired split force-ends that activation (native `ForceToEnd(setCooldowns: true)`). The rider then acts on its next turn.

## Native runs (finalize-f6 record; original verdicts kept)

| Run | Candidate | Verdict | What it established |
|---|---|---|---|
| r01 `20261107-finalize-f6b-tb-01` | f6b `4dfd2a45` | **FAIL** | **The F3 integration rows passed in the game:** TB setup with both pairs mounted, the move/attack, step and Charge rows.<br>**FALL-01 TB, genuine death:** one fall. The labelled fail rolled d20 2 for Mobility 10 vs DC 11, then a native 1d6 = 3 applied and native prone. The rider's cooldowns were unchanged by the fall. Pair B stayed mounted with the same owner and generation. The log reads "cvb: Mobility check failed / cvb receives 3 damage / Mastodon dies; cvb is thrown: Mobility 10 vs DC 11 - falls hard, 3 damage, knocked prone."<br>**Why it failed:** the death ended the rider's activation, so the scenario's same-turn stand-up order met no turn; that row exceeded its bound. The preamble reader also missed the new scenario name. |
| r02 `20261107-finalize-f6c-tb-02` | f6c `c7ca5989` | **FAIL** | The unchanged F3 row `F3-tb-b-round1` refused its ordinary full-attack input. That row passed in r01 and in F3's r05. The F6 rows did not run, and no F6 conclusion is drawn. |
| r03 `20261107-finalize-f6c-producer-03` | f6c `c7ca5989` | **ORIGINAL OUTER FAIL / NATIVE GAME PASS** | **Names:** the F5 naming rows passed, with both pets named `Ébène l'Ancien`.<br>**Harmless control:** a voluntary Dismount through the public control and a remount began 0 falls.<br>**FALL-02 RT, genuine new prone of the Riding Dog:** one fall, d20 7 for Mobility 10 vs DC 11, native 1d6 = 1, native prone, landed 1.90 m from the dog. The identically named Mastodon pair stayed mounted with the same owner and generation.<br>**Save:** both stood natively, and the one-pair post-fall Manual was written: `b33eda46d65ce242e77a7cb105e8daf18f07134459f59a954025460b93ac2edc`, 857840 bytes.<br>**Why it is recorded FAIL:** the runtime-result reader did not know the row `F6-rider-fall-rt-producer` (reader-only). The row is now registered, and the corrected readers accept its immutable evidence; that re-evaluation is separate and does not rewrite the FAIL. |
| r04 `20261107-finalize-f6e-consumer-04` | f6e `fbef8638`, same DLL | **PASS** | **Load:** a fresh process loaded r03's exact Manual. The archive's one pair was restored with restore counts 2/1/2, no preparation, and no restoration of the unseated pair.<br>**Unseated rider:** still unmounted, standing, with exactly its saved 1 damage.<br>**No replay:** 0 falls, checks or damage.<br>**Names and tack:** both names intact, with 0 naming writes. The tack refits on the unmounted dog and again after the remount.<br>**Controls:** the ordinary Mount control seated the rider again with no fall, and the F5 sheet rows ran. |

- **Pinned reuse of r03:** r03 is admitted as the cold source only through `scripts/runtime/Finalize6PinnedFallSource.ps1`. This follows the existing precedent `Chunk7F09SixthHireSource.ps1`.
  - Six receipts are pinned byte-exact, along with the single recorded error, native PASS, its commit and the identical DLL and MVID.
  - Every other source check is unchanged, including the corrected readers over its evidence.
  - `Test-Finalize6PinnedSource.ps1` passes 9/0 on the real receipts, including refusal of another DLL, another Manual and another reader-only FAIL (F5 r06).
- **Cross-run reader:** `scripts/Test-Finalize6ColdAgainstSource.ps1 -ProducerRunId 20261107-finalize-f6c-producer-03 -ConsumerRunId 20261107-finalize-f6e-consumer-04` passes. It compares the unit IDs, the fallen rider's saved damage, the Mastodon owner and generation, and the names.
- **Restoration:** all four runs: Mods, Working, save protection and write allowlist restored. Save-inventory digest `60e3d6cc271af65408d8b0d7db6c132ca4f0f42f38eba4dee46589a21db9c2a1`, the same as F5. No game, lock or waiter remains.

### Inspected frames (private, normal camera)

- **r03 `finalize5-f6-rt-fall-log.png`:** the ordinary bottom-right battle log shows the Dismount and Mount lines, then "vbnm: Mobility check failed / vbnm receives 1 damage / Ébène l'Ancien falls prone; vbnm is thrown: Mobility 10 vs DC 11 - falls hard, 1 damage, knocked prone." The Mastodon pair is still mounted.
- **r03 `sprint5-f6-rt-fall-side.png`:** the saddled dog and its Small rider stand apart.
- **r01 `finalize3-f6-a-death-fall.png`:** round 3. The dead Mastodon lies to one side and the death log is as quoted. The rider stands partly in shadow.
- **r04 `finalize5-f6-cold-post-fall.png`:** after the load the log shows only "Game loaded", with no replayed fall. The Mastodon pair is mounted and the saddled dog stands unmounted.

Frames, saves and specimens stay in the lab and are not published.

## Reviews and checks

- **Independent read-only review of the delta:** two rounds, then a check of the pinned source. The confirmed findings were fixed before any start:
  - reload suppression of a later fall;
  - readers accepting a hard landing without prone;
  - weak placement assertions;
  - load and save gating;
  - the fallback seat key.
- **Accepted limitation:** a rider and mount dropping in the same native tick. If the rider's own incapacity is delivered first, no soft fall is rolled. Under D5, an already detached rider is not a target of a later mount callback.
- **Offline:** components 1118/0 and Validate-Source 149/0. `MountedRiderFallTests` covers causes, exclusions, dead rider, no replay on load, threshold, outcomes, inability, prone, exactly-once, pending, identical names, remount, reloaded seat, load abandonment, abandonment and log text.
- **Readers:** `Test-Finalize6Readers.ps1` 55/0 (synthetic, labelled). `Test-Finalize6PinnedSource.ps1` 9/0 (real receipts).
- **Candidate:** 11/0 at `fbef8638`, including Test-Harness.

## Support and acceptance map

| Item | Status |
|---|---|
| FALL-01 death → one unseat, native check/1d6/prone, log, debt unchanged, other pair untouched | **Shown natively in TB** (r01, failed check). Collapse to unconscious: source and offline only. |
| FALL-02 new prone → the same event model | **Shown natively in RT** (r03, failed check). TB prone: not shown natively. |
| D5 success branch (safe landing, no damage or prone) | **Offline only.** The native row (TB prone) did not run. |
| Prone-then-death and duplicate callbacks | Offline (ledger) only. Native row not run. |
| Native stand-up cost after a hard landing in TB | Not shown natively (r01 met the forced activation end). RT stand-up shown (r03). |
| Voluntary Dismount, remount and lifecycle exclusions | Voluntary Dismount and remount shown natively, 0 falls (r03). Load no-replay shown natively (r04). Others offline. |
| Duplicate display names | Shown natively: identically named pets, separate falls and owners (r03, r04). |
| Post-fall save/reload | **Shown natively** (r03 producer re-evaluated, r04 consumer PASS, cross-run PASS). |
| F1–F5 integration in this route | F2 public control (r03), F3 move/attack/step/Charge rows (r01), F5 names and tack (r03, r04). F1 creator, F4 AI/chest/entrance: retained earlier evidence, unchanged by F6. |
| WORLD-02 trap | **Deferred, not passed** (docs/FINALIZATION-DEFERRED-ITEMS.md). |
| Physical input, portrait tooltip, Buff Planner | Not exercised. The combat log was inspected for the new line only. |
| Owner play acceptance | Separate, not claimed. |

## Budget

- **finalize-f6:** 32.77 of 90 minutes and 4 of 4 starts (no adjustment). The total includes 30 s imported for direct reader runs and the suite snapshots.
- **No fifth start** is authorized. The F5 record closed at 74.9/90 and 8/8 and is not reused.

## Next (owner decision)

**Recommended:** one more start of `finalize6-falls-tb` on f6e, unchanged. It would show:
- the TB prone fall with a safe landing;
- prone-then-death;
- the stand-up paying its Move on the rider's next native turn.

The F3 row that refused in r02 passed in r01, so a recurrence remains possible. Alternatively, accept FALL-01/02 at the shown scope.

**Release notes:** keep the save-dependency and removal warnings and the pre-upgrade rollback guidance. Keep WORLD-02 deferred.

**Owner playcheck (disposable save):**
1. Ride into a fight and let a mount drop or get tripped. Read the log line and see the rider land beside it.
2. Fail and pass at least once.
3. Stand up on the next turn.
4. Save, reload and confirm that nothing replays.
5. Remount through the ordinary control.

**Stop at review.** No merge, tag, release, install or publication.
