# Chunk 7 owner-seed evidence summary

Tracked summary of each accepted native checkpoint on the owner-seed route ([owner directives](CHUNK7-OWNER-DIRECTIVES-2026-10-16.md)). Raw receipts, saves and logs stay in the private lab (`C:\Dev\KingmakerMountedCombatLab`). Paths below are provenance only; they are not remotely readable proof. Counts are per run, not summed across parent and child suites.

## Seed

| Field | Value |
|---|---|
| Seed id | `KMC_C7_NO_MODS_OWNER_SEED` |
| Owner file | `Manual_303_KMC_C7_NO_MODS_OWNER_SEED.zks` (left untouched) |
| SHA-256 | `3405d7c813668ad646788efd746a62c1b16e1438fad1bc4bcbebc961821b924f` |
| Bytes | 1,239,025 |
| Master | `fixtures/owner-supplied/KMC_C7_NO_MODS_OWNER_SEED.master.zks`: read-only, an ordinary copy, never loaded |
| Header | main campaign, build 2.1.7b, GameId `3f7381f7-aba6-43bf-bae6-39c6a66a7180`, area `b10e86516b6437d45a629931c30936fe` (the spider cave near Fangberry), DLC Tieflings, Varnhold, Endless and Arcane, no KMC metadata, no foreign `$type` |
| Superseded | `Manual_302_KMC_C7_OWNER_SEED` carried Call of the Wild and Gunslinger types. It is preserved but not used. |

## Checkpoint 1: seed qualification by an isolated cold load

| Field | Value |
|---|---|
| Run | `20261017-c7b-world18-ownerseed-s4-cold`: batch 1, scenario 4 |
| Scenario | `chunk7-assisted-cold-inventory`, selection `owner-seed-readonly-inventory`, base-game-only, one load, zero writes |
| Product | `0.1.0-chunk7b-preview.18`, commit `e09924b4`, DLL MVID `b7c9af78-da5d-4bd6-9db3-c98995c61c58` |
| Harness | the same commit (unified successor worktree); suite `chunk7b-world18-20261017` |
| Loader | UMM 0.33.2.0; KMC staged alone |
| Terminal result | **PASS** |

**Observed:**
- A fresh copy loaded natively through the missing-metadata path.
- Party: Human Ranger 3 at 9000 XP (the main character), Valerie 4, Linzi 4, Amiri Barbarian 4. No pets.
- All four header DLC were recognised and enabled.
- No unresolved blueprint, failed type bind, game-logged exception or foreign declared type.
- Party controllable, out of combat. Autosave on (1 slot), OnlyOneSave off, XP rate 100.

**Restoration:** Mods restored, save protection passed, profile restored exactly. The next qualification suite read unchanged save and Mods digests.

**Excluded:** this checkpoint proves no mounted restoration, because the seed has no KMC metadata.

**Earlier attempts (batch 1, all restored):**
- Scenario 1: an ambiguous `GetMethod` in the observer.
- Scenario 2: a JValue-null check.
- Scenario 3: reader assumptions about a null PostLoad `Player.GameId` and the `OnNewRound` hook name. The native artifact was PASS; the reader refused it.

## Checkpoint 2: two-pair movement, selection and control slice

| Field | Value |
|---|---|
| Run | `20261017-c7b-world22-ownerseed-b2s4-twopair`: batch 2, scenario 4 |
| Scenario | `chunk7-owned-two-pair-campaign`, selection `owner-seed-exploration`, base-game-only, one load, zero writes |
| Product | `0.1.0-chunk7b-preview.22`, commit `162a6d14`, DLL MVID `21a80e18-e6d0-4d0a-859e-8fc5bc370da4`, package SHA-256 `de56294ef94cb3e437256632c423981b2ad6f36793f16e53f745c1896e9a1732` |
| Harness | the same commit; suite `chunk7b-world22-20261017` |
| Terminal result | **PASS** |

**Disclosed setup** (fixture assistance; `acquisitionProof` is false):
- Rider A, the main character `73681ef2-…`, took an unassisted native Ranger level 4 through the game's level-up UI. Choices: Hunter's Bond, the Ranger companion selection, then the KMC Horse feature. Result: a Hunter's Bond KMC Horse.
- Rider B, Amiri `fd213210-…`, received native `GainExperience` to exactly the level-8 threshold, then took four native Ranger levels and got her own Horse.
- No save was written. The installed game never registers `LevelUpAutoSaveController`, so a level-up does not autosave.
- Setup ended at a ledgered SETUP-COMPLETE boundary. Money and the shared inventory were unchanged across setup.

**Measured:**
- Two native Mount inputs.
- An independent native ground move for each pair, each executed by its own Horse, with selection switching.
- Rider A's native Dismount while pair B stayed mounted under the same owner and generation.
- Rider A's unmounted control move.
- Three remount cycles.

**Door:** not applicable. No qualifying door lies within 28 m.

**Restoration:**
- Live `Mods` digest `b592f400…` before and after.
- Save-inventory digest `df434471…` before and after.
- `runtimeUnexpectedWrites` empty. The only staged-tree change was UMM's `KingmakerMountedCombat.dll.<N>.cache`, which was quarantined.
- Profile bytes restored exactly. Protected fixtures unchanged.

**Excluded:** acquisition, door behaviour, combat, mounted save/load.

**Earlier attempts (batch 2, all restored):**
- Scenario 1: setup waited for the nonexistent level-up autosave.
- Scenario 2: a paused ground order was refused. This was a product defect, fixed in `308e7734`.
- Scenario 3: the readers compared Guid text forms and nested a list. The native artifact was PASS.

## Reviews

The lab receipts are under `analysis-cache/chunk7-continuation/owner-seed-review/`:

| Receipt | Verdict |
|---|---|
| `owner-seed-safety-review01` | PASS |
| `umm033-safety-review01` | FAIL |
| `umm033-safety-review02` | FAIL |
| `umm033-safety-review03` | PASS |
| `twopair-safety-review01` | FAIL |
| `twopair-safety-review02` | FAIL |
| `twopair-safety-review03` | FAIL |
| `twopair-safety-review04` | PASS |
| `postreview-delta-review01` (`928987f2..162a6d14`) | PASS |
| `batch3-safety-review01`, `batch3-safety-review02` | FAIL, PASS |
| `lease-scope-review01`, `02`, `03` | FAIL, FAIL, PASS |
| `rt-companion-review01`, `rt-companion-review02` | FAIL (B1), PASS |
| `diag-delta-review01` | PASS |
| `batch6-delta-review01` | PASS |
| `batch6-product-review01` (product: TB transition in the prepared turn) | PASS |
| `batch6-reader-review01` | PASS |

Diagnoses: `owner-seed-batch05/tb-preparing-diagnosis01`, `owner-seed-batch06/rt-aoo-diagnosis01` and `owner-seed-batch06/tb-pending-move-diagnosis01`.

An external targeted review of PR #5 is recorded in Directive 3.

## Checkpoint 3: owner-seed-derived two-pair native Manual save

| Field | Value |
|---|---|
| Run | `20261018-c7b-world26-ownerseed-b3s1d-save`: batch 3, slot 5. This is the source used by checkpoint 4. An earlier PASS, `20261018-c7b-world25-ownerseed-b3s1c-save` on preview.25, was superseded because its candidate predates the load-admission fix. |
| Scenario | `chunk7-owned-two-pair-campaign`, selection `owner-seed-persistence-save`, PersistenceCase `multi-exploration-manual`, base-game-only, one load, exactly one write |
| Product | `0.1.0-chunk7b-preview.26`, commit `20e23743`, DLL MVID `7cf6a912-c79c-4c3d-96aa-e9717974fa1a` |
| Harness | the same commit; suite `chunk7b-world26-20261018` |
| Output | `Manual_304_KMC_C7_SEED_WORLD.zks`, SHA-256 `b4d1ede7843c600f201dcd7b71cd2558cad44210e7e0eb78d9a58f6e89f64003`; the on-disk bytes equal the recorded write |
| Terminal result | **PASS** |

**Observed:**
- The disclosed owner-seed setup, ending at SETUP-COMPLETE, then two native Mounts.
- Dismount and each rider's primary control were bound into empty hotbar slots. This happened after SETUP-COMPLETE and is disclosed.
- Exactly one native Manual, written through `Game.SaveGame(SaveManager.CreateNewSave(...))`. The run observed the request, the `PrepareSave` copy, the KMC serialization barrier, the save worker and completion.
- The schema-3 KMC metadata was read back from the written bytes. It holds two pair records equal to the live registry at the save boundary (owners, generations, riders and Horses), plus 4 control slots.
- Ancestry is recorded as `owner-seed-derived` with disclosed assistance. Acquisition proof is false.

**Restoration:** Mods and save-inventory digests identical before and after; no unexpected runtime write.

**Earlier attempts (batch 3, all restored):**
- Slot 1: a compound pre-save check failed without naming its condition.
- Slot 2: the loader's own descriptor was required to be the same object as the save-list entry. It never can be: `WorkingFixtureLoader` uses `LoadZipSave`.

## Checkpoint 4: separate-process cold load of the derived Manual

| Field | Value |
|---|---|
| Run | `20261018-c7b-world26-ownerseed-b4s1-load`: batch 4, slot 1 |
| Scenario | `chunk7-owned-two-pair-campaign`, selection `owner-seed-persistence-load`, pinned to checkpoint 3 by run id, world-evidence SHA-256 `01438210…` and Manual SHA-256 `b4d1ede7…`; same candidate as checkpoint 3 |
| Product | `0.1.0-chunk7b-preview.26`, commit `20e23743` |
| Terminal result | **PASS** |

**Observed:**
- A separate process: cold pid 24060, producer pid 57108.
- It loaded an independent copy of the Manual through the production loader. KMC admitted it through the all-pair archive admission, fixed in `0ccda7cc`.
- Both pairs were restored with the producer's identities:
  - rider A `73681ef2-…` on Horse `e3a3be8b-…`, owner `6639ced4…`, generation 1;
  - rider B `fd213210-…` on Horse `e9cd259e-…`, owner `3a4c6e04…`, generation 4.
- Restoration entries were captured by the native restoration observer. Both restored pairs then moved natively. Zero writes, one load.

**Restoration:** Mods and save-inventory digests identical before and after; no unexpected runtime write.

**Earlier attempt (batch 3, slot 4, `20261018-c7b-world25-ownerseed-b3s2-load`):**
- KMC refused its own schema-3 save at native load admission. The schema-1/2 reader reports all-pair archives as Future. This is a product defect, fixed in `0ccda7cc`.
- The host then threw into the native settings-refresh coroutine, so no result was committed and restoration was blocked.
- Recovered through the reviewed tooling:
  - the game process exited on its own;
  - Mods and saves were restored exactly;
  - PlayerPrefs `KingdomDifficulty` and Unity's session counters were restored to the pre-run snapshot;
  - `Params.xml` stayed byte-identical.

**Excluded:** acquisition (setup is disclosed assistance), Quick/Auto/area/removal/migration persistence, combat after load, door behaviour.

## Combat checkpoints 5–7 (October 18, 2026)

All three run on the same frozen candidate and reuse checkpoints 3 and 4 by pinned receipts. Both pairs start restored MOUNTED by a cold load of an independent copy of the checkpoint 3 Manual. The target is the project-spawned, runtime-only stock Mammoth, approved by the owner for owner-seed derivatives. Each run did one load and zero writes.

| Field | Value |
|---|---|
| Product | `0.1.0-chunk7b-preview.34`, commit `bba4edde`, DLL MVID `012d4ce2-3bdd-4b47-af78-215f12e15899`, package SHA-256 `0aefa9cd…a61644d` |
| Harness | the same commit; suite `chunk7b-world34-20261018` |
| Reviews | `rt-companion-review02`, `diag-delta-review01`, `batch6-delta-review01`, `batch6-product-review01` and `batch6-reader-review01`, all PASS after fixes |

| Checkpoint | Run | Scenario | Terminal result | Evidence SHA-256 |
|---|---|---|---|---|
| 5 | `20261018-c7b-world34-ownerseed-b6s5m-rt` (batch 6, slot 5) | RT with an unrelated companion action | **PASS** | `3252f61c…ec714864` |
| 6 | `20261018-c7b-world34-ownerseed-b6s4m-tbmount` (batch 6, slot 4) | TB mount-before-rider | **PASS** | `d419ed6e…e416d9b` |
| 7 | `20261018-c7b-world34-ownerseed-b7s1-tbrider` (batch 7, slot 1) | TB rider-before-mount | **PASS** | `ec1f8d11…f21591c0` |

**Checkpoint 5 observed (RT, 10/10 subscenarios):**
- RT Charge stayed default-off.
- Two concurrent native primaries: rider A's own attack and Horse B's own attack. Each was one native attack with its own actor as resource owner.
- Valerie's action:
  - she took exactly one ordinary native UnitAttack with its own single cost, then one native Stop;
  - zero pair touch and zero foreign events inside her window;
  - 17 refused-before-Run pair proposals were classified (see known debt);
  - the target was standing, conscious and idle when her window opened.
- Local Stop with an untouched neighbour, unmounted native control, isolated local cleanup failure, and cleanup.

**Checkpoints 6 and 7 observed (TB, 12/12 subscenarios each):**
- Initiative order: in mount-first both Horses led it; in rider-first both riders led it.
- Each pair took a full native activation: a move, a rider primary and a mount primary. A melee rider was carried into reach by its own mount's native approach.
- The TB Charge refusal was cost-free.
- Exact native renewal in the next round.
- A native Dismount as the first action of the rider's prepared turn. This was admitted under the owner's October 18 decision.
- Native rider removal ended in a mounted pending-only survivor through the game's own paired forfeit (`ForceToEnd(setCooldowns: true)` nested on the mount's turn).
- An isolated local cleanup failure, and cleanup.

**Restoration (all three):** process exited, Mods restored, save protection passed, dependencies unchanged, profile bytes restored exactly, zero accepted cache changes.

**Defects found on the way (batches 5–7, all runs restored):**
- **Product:**
  - turn-based Mount/Dismount was refused while the rider's turn was Preparing, so it could never be a turn's first action. The owner decided to admit it (`06ec7180`).
- **Diagnostics and harness:**
  - a refused proposal's null-string executor (`38ad77b7`);
  - the TB driver waiting for Acting before its first input (`73a5c2dc`);
  - stale RT window copies and positive-only identity checks (`37489a08`);
  - an unsettled diagnostic target provoking attacks of opportunity by standing up (`4850445b`);
  - the own-partner approach, the native forfeit and an empty participation record (`c6f0c00c`).

**Excluded:** acquisition (disclosed assistance), human review, door behaviour, Small calibration, stress, and persistence breadth beyond the one Manual.

## Directive 4 takeover checkpoint — October 18, Codex

The earlier exclusions below belong to their original checkpoints. Later executor
records in [the Directive 4 handoff](CODEX-HANDOFF-CHUNK7-DIRECTIVE4-2026-10-18.md)
report accepted RT click integrity and first-action Mount in both orders on
preview.35, Quick/Auto save plus separate loads on preview.36, RT debt save/load on
previews.37/.38, and the TB debt producer on preview.40. Their PASS is not transferred
to a later binary.

Codex independently reviewed Claude's completed-boundary restore fix `f1e54002`
and built preview.41 once from published source
`6652c7fe1e8adb0edb61c28f520275bf5f708b65`. DLL SHA
`2fdce155accf089e30d59970cf5862a92d8423ccb9e5086921c751f781cc74d2`, MVID
`daf5ccc5-3a74-40e6-9563-8e8fa28daa4f`, package SHA
`291d8fb4747d1f1e307a68c259a0d6631e5ecb37909ab8fa0c3bb7f9ed252709`.
Candidate passed 6/6 in 388.1997642 seconds; this is offline qualification.

| Native run | Result | Observations and restoration |
|---|---|---|
| `20261018-c7b-world41-ownerseed-b12s1-tbload` | PASS, 104.4654715 s | Separate-process cold load of accepted b11s1 (`f952f194…`), exact completed A movement 2.286 preserved at both restoration phases, native continuation/cleanup. All restoration/save flags true; profile bytes exact, lock released. |
| `20261018-c7b-world41-ownerseed-b12s2-zerosave` | FAIL, 115.7495532 s | Game PASS; reader refused B's own mounted approach because the final checkpoint is zero-mounted. Actual active-owner snapshots distinguish the phases. All restoration/save flags true; profile bytes exact, lock released. Not an admitted cold-load source. |

Evidence SHA-256 respectively
`2b21042ef0984e53a7da3af114c01c048ad4d01cbfb35bf2e7fc4293e3fe1f8e` and
`5f1c81c90bb237c6365470a4dc5c29ee127e83d6c20df2a21e5fe60c59702178`.
Private raw receipts, exact source/reader/suite pins and old/new diagnostic verdicts
remain under `analysis-cache/chunk7-continuation/owner-seed-batch12/` and the matching
`runtime-evidence/` directories. No private saves/raw logs are published.

The original world41 suite remains bound to the first two runs. The same product
package is frozen in the former successor worktree, with a fresh root suite for
later reader-only runs. No new DLL or inherited gameplay PASS is claimed by that
execution-root change. The active branch is now in the dedicated repository and
uses the original policy-approved push helper.

## Not yet accepted — current Directive 4 remainder

- Zero-mounted producer and separate-process cold load: reader correction under review; original failed producer cannot be reused.
- Pending-only serialization, nonselected-owner save fencing, worker cancellation, area/Auto continuity, disable/removal and historical schema-1/2 paths need their selected native evidence.
- Horse derivatives retain their permanent-reference removal restriction; positive DLL-absent proof needs a qualified reference-free fixture.
- Doors remain opportunistic. Acquisition/progression, Small, six-pair stress/performance, full 7E and human/release acceptance remain later gates.

## Historical exclusions at the Directive 3 checkpoint

- **Acquisition:** the owner-seed setup is disclosed assistance.
- **Quick, Auto, area, removal and migration persistence breadth:** only the one owner-seed-derived Manual and its cold load are proven.
- **Door behaviour:** no qualifying door at the seed location.
- **Small calibration** (catalogue still empty), **stress and performance**, **human review**.
- **Chunk 6A regression:** the historical probe row `CM01-combat-mount-preparing-refused` asserts the superseded R3 rule (see known product debt).
