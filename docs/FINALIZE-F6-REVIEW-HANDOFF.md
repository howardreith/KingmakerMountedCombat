# F6 review handoff: logged rider falls and final integrated acceptance

## Disposition (2026-11-07)

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
