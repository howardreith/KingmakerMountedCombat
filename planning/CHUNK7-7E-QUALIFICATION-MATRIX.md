# Chunk 7 — 7E final private qualification matrix

Status: **DECLARED, NOT RUN.** This is the finite matrix for the final private candidate.
It was written offline on October 23, 2026 at HEAD `174f5b2b`. No final candidate exists yet,
and nothing here is a PASS. Sources:
- the [requirement table](CHUNK7-COMPLETION-MISSION.md) and its §8–9;
- the [original matrices](CHUNK7-ORIGINAL-SCOPE-AND-MATRICES.md);
- [VALIDATION-POLICY](../docs/VALIDATION-POLICY.md);
- the [F10 reconciliation](../docs/CHUNK7-F10-RECONCILIATION-2026-10-23.md);
- the [F11 protocol](../docs/CHUNK7-F11-ACCEPTANCE-PROTOCOL-2026-10-23.md);
- the [batch192 record](../docs/CHUNK7-BATCH192-DOOR-SEED-COLD-2026-10-23.md);
- the runner's scenario and case registry.

## Reuse rule and its result

Mission §8 says: *"Existing results may be reused only when their executed bytes and relevant
conditions already match."* The validation policy forbids transferring an old PASS to changed
bytes. Every row therefore defaults to **re-run on the final package (F)**.

`git diff --shortstat <evidence commit> HEAD -- src/` on October 23:

| Evidence commit (used by) | Product source drift to HEAD |
| --- | --- |
| `bba4edde` (two-pair TB/RT combat, batches 6–7) | 143 files, +13,803/−755 |
| `20e23743` (owner-seed Manual/cold, batches 3–4) | 152 files, +14,026/−825 |
| `f5fe6fcd` (disable, zero-save, refusals, batches 27–31) | 121 files, +10,986/−620 |
| `77f506a6` (F10 owner cumulative, batches 159–166) | 43 files, +3,458/−201 |
| `62a68e64` (six-pair measure, batch190) | 4 files, +159/−41 |
| `e32c1226` (seed cold s1r, batch192b) | 1 file, +35/−13 |
| `20c4ff91` (door, batch192d) | **empty** |

**One conditional reuse row:** the door row (batch192d). Its source is identical to HEAD. It is
reused only if F's DLL is byte-identical to batch192d's DLL (`b2acbdeb…`, package `a6d4678f…`).
That is true only if F is that very package, or a rebuild that reproduces the same bytes. If F
carries any product change, the door row re-runs; it is already listed in batch B6. Every other
row is a re-run.

Fixtures are inputs, not behavior claims. A seed, or a fixture Manual produced on an earlier
candidate (Small, Medium, six-Horse, Manticore, stock Mammoth), may feed a final-package run
unchanged, keeping its recorded provenance. Every behavior claim comes from the run on F.

## Requirement-to-scenario matrix

Common to every row:
- **Scenario:** `chunk7-owned-two-pair-campaign`, unless the row names another.
- **Selection:** `ownedPairs.case`, plus `PersistenceCase` where the row gives one.
- **Readers:** the runner's outer `Test-RuntimeResult`, plus the family reader shown.
- **Settlement:** every run requires exact restoration.
- **Kind:** re-run on F (**R**), unless the row says otherwise.

Writes are the expected native save writes.

| # | Requirement (original IDs) | Scenario / case / PersistenceCase | Source fixture | Family reader | Writes |
| --- | --- | --- | --- | --- | --- |
| 1 | F06 Manual producer, C07 merge/split (exploration) | `owner-seed-persistence-save` / `multi-exploration-manual` | NO_MODS seed `3405d7c8…` | `Chunk7OwnedPairsEvidence` | 1 |
| 2 | F06 separate-process cold load | `owner-seed-persistence-load` / `multi-exploration-manual` | #1 output | same | 0 |
| 3 | F06 Quick save | `owner-seed-checkpoint-save` / `multi-exploration-quick` | #1 output | same | 1 |
| 4 | F06 Quick cold | `owner-seed-checkpoint-load` / `multi-exploration-quick` | #3 | same | 0 |
| 5 | F06 explicit Auto save | `owner-seed-checkpoint-save` / `multi-exploration-auto` | #1 | same | 1 |
| 6 | F06 Auto cold | `owner-seed-checkpoint-load` / `multi-exploration-auto` | #5 | same | 0 |
| 7 | C01, C04–C06, C08, F01 (TB rider-first); TB Charge cost-free refusal; native rider-removal forfeit | `owner-seed-combat-rider-first` | #1 | `Chunk7OwnedPairsEvidence` + cleanup reader | 0 |
| 8 | Same, mount-first initiative (C05 both orders) | `owner-seed-combat-mount-first` | #1 | same | 0 |
| 9 | C02, C09, C10 (RT); concurrent primaries; Stop; unmounted control; RT Charge default-off | `owner-seed-realtime-core` | #1 | `Chunk7RealtimeEvidence` | 0 |
| 10 | F08 disable/re-enable, failed-obligation retry, permanent Horse removal refusal | `owner-seed-combat-disable` | #1 | owned + cleanup readers | 0 |
| 11–12 | F06/C07 RT action debt, save then cold | `owner-seed-checkpoint-save`/`-load` / `multi-rt-debt-manual` | #1 | owned reader | 1 / 0 |
| 13–14 | F06/C07 TB action debt, save then cold | same / `multi-tb-debt-manual` | #1 | same | 1 / 0 |
| 15–16 | F06 zero-mounted TB retained owners, save then cold | same / `zero-mounted-tb-retained-manual` | #1 | same | 1 / 0 |
| 17 | 7B pending-only schema-3 producer | `owner-seed-checkpoint-save` / `legacy-mounted-pending-tb-manual` | #1 | same | 1 |
| 18 | 7B schema-2 derivative native resave | `owner-seed-checkpoint-load` / `legacy-mounted-pending-tb-manual` | #17 derivative | same | 1 |
| 19 | 7B schema-2 to current cold (third process) | `owner-seed-checkpoint-load` / `owned-schema2-pending-current-manual` | #18 | same (corrected reader) | 0 |
| 20–22 | F07 schema-1: source, then resave, then current cold | save / `owned-schema1-manual`; load / `owned-schema1-resave-manual`; load / `owned-schema1-current-manual` | #1 | owned + schema-1 reader | 1 / 1 / 0 |
| 23–24 | F07 malformed and future metadata refusal (no overwrite) | `owner-seed-load-refusal` / `multi-malformed-refusal-manual`, `multi-future-refusal-manual` | #1 derivatives | owned reader (b31 corrected bound) | 0 |
| 25 | F06 worker cancel and drain | `owner-seed-checkpoint-save` / `multi-owned-worker-cancel-manual` | #1 | owned reader | 2 |
| 26 | F06 non-selected-owner global save barrier | `owner-seed-checkpoint-save` / `multi-nonselected-mount-unsettled-manual` | #1 | same | 1 |
| 27 | F06/F08 repeated load lifecycle | `owner-seed-persistence-load` / `multi-exploration-three-loads-manual` | #1 | same | 0 |
| 28–29 | 7B area-triggered Auto, then separate cold | `owner-seed-checkpoint-save`/`-load` / `multi-area-autosave` | Manticore fixture Manual (b55–56 lineage) | owned + area reader | 1 / 0 |
| 30 | 7B ordinary door; exploration movement and selection | `owner-seed-exploration` | Old Sycamore seed `90789101…` | `Chunk7WorldEvidence` | 0 |
| 31 | 7B positive cleanup Manual | `persistence-p07-save` / `prepare-removal` | stock Mammoth fixture (b64) | `CurrentRemovalEvidence` | 1 |
| 32 | 7B genuine DLL-absent cold load | `persistence-p07-load` / `removal-no-dll` | #31 | same (b67 reader) | 0 |
| 33–34 | F09 Small Halfling/Horse, mounted Manual then cold | `small-fixture-persistence-save`/`-load` / `multi-exploration-manual` | Small fixture Manual | owned + Small reader | 1 / 0 |
| 35–36 | F09 Medium Horse/Mammoth, Manual then cold | `medium-fixture-persistence-save`/`-load` / `multi-exploration-manual` | Medium fixture Manual | owned + Medium reader | 1 / 0 |
| 37–38 | F09 six mounted pairs, Manual then cold | `six-horse-mounted-persistence-save`/`-load` / `multi-exploration-manual` | sixth-Horse Manual (b184–185) | six-pair reader | 1 / 0 |
| 39 | F09 six-pair lifecycle stress; F11 six-pair measurement | `six-horse-cold-fixture-lifecycle-stress` (batch190 custody) | same | six-pair + F11 readers | 0 |
| 40 | F11 matched two-pair pool, 0/1/2/0 | `owner-seed-exploration`, matched-pool windows (b157 mode) | NO_MODS seed | `Chunk7WorldEvidence` + F11 reader | 0 |
| 41 | F11 allocation events | `owner-seed-exploration` + `f11AllocationProbe` (b156 observer) | NO_MODS seed | F11 allocation reader | 0 |
| 42 | F11 request-free controls, quiet log; F12 fresh KMC install | `chunk7-normal-player-control` | sixth-Horse Manual 308 `b1e817a6…` (b191 source gate) | `Test-KmcNormalPlayerResult` | 0 |
| 43 | F10 assisted new-campaign producer | `chunk7-assisted-fixture-preparation` / `authored-main-ranger4-manual` | assisted fixture | assisted reader | 1 |
| 44 | F10 producer cold | `chunk7-assisted-cold-inventory` / `assisted-manual-readonly-inventory` | #43 | same | 0 |
| 45 | F10 paid CharGen acquisition (new character) | `chunk7-cumulative-acquisition` / `paid-small-ranger-horse` | #43 | cumulative reader | 1 |
| 46 | F10 existing-campaign Ranger 4 Horse choice | `chunk7-manual-derived-acquisition` / `existing-ranger-horse` | NO_MODS seed | manual-derived reader | 1 |
| 47 | F10 acquired-field cold | `chunk7-acquired-manual-derivative` / `preserve-acquired-field` | #46 | acquired reader | 1 |
| 48–49 | F10 mounted leveling, then cold | `chunk7-native-progression` / `small-horse-native-level`, `small-horse-native-level-cold` | Small fixture | progression reader | 1 / 0 |
| 50 | F10 dismissal | `chunk7-native-progression` / `small-horse-native-dismiss` | Small fixture | same | 0 |
| 51–52 | F10 retraining, then cold | `small-horse-native-retrain-horse`, `small-horse-native-retrain-cold` | Small fixture | same | 1 / 0 |
| 53–54 | F10 Horse-to-Mammoth replacement, then cold | `medium-horse-native-retrain-mammoth`, `medium-horse-native-retrain-cold` | Medium fixture | same | 1 / 0 |
| 55 | F12 upgrade: preview.211 package, then F on the same save | **gap**, see F12 below | owner seed + preview.211 package | new | 1 |
| 56 | F12 rollback: F, then preview.211 on the pre-migration copy | **gap**, see F12 below | #55 pre-migration copy | new | 0 |
| 57 | F08/F03 incapacitation in a mounted pair; healthy neighbour continues; surviving mount gets one independent turn (X1). Precondition: rider B `fd213210` is not essential, not immortal, not the main character and allowed the dying condition (`Chunk7CombatNativeLife.cs:33-35`); a refusal for that reason records the behavior as **uncovered**, not waived | `owner-seed-native-life-cleanup` | #1 output, restored mounted (same chain as rows 7–8; derived TB lineage b7s1 and b8s2 PASS, b5s5 FAIL retained) | `Chunk7NativeLifeEvidence` (derived mounted-start branch). Batch196 pilot FAIL on a diagnostic paired-identity counting defect (see X1); **[batch197](../docs/CHUNK7-BATCH197-7E-PILOTS-2026-10-24.md) development PASS** (9/0, control available) | 0 |
| 58 | F06 capital arrival (owner decision): the non-main pair is dismounted on arrival through the native capital control rule, the main pair is kept, saving stays allowed | owned `persistence-load` case `multi-exploration-capital-arrival-manual` ([batch196](../docs/CHUNK7-BATCH196-7E-PILOTS-2026-10-24.md) pilot FAIL: the product retired the companion pair natively, retired 1, active 1, save allowed, exact UMM line; the fixture then refused because native Oleg's took the hired companion pair out of game, falsifying native assumption 1; no Manual written. [batch197](../docs/CHUNK7-BATCH197-7E-PILOTS-2026-10-24.md) re-pilot after the out-of-scene admission FAIL: native Oleg's also moves the three story companions to remote (out of party, in game, not controllable), beyond the one admitted pair; no Manual written. [batch198](../docs/CHUNK7-BATCH198-7E-PILOTS-2026-10-24.md) re-pilot on `767feaea` **PASS** (4/0): same native remote shape, the three story companions recorded as `admittedNativeRemoteCompanions`, retired 1, active 1, save allowed, exact UMM line, Manual 309 `KMC_C7_CAPITAL` written; development pilot, not exact-package qualification. [batch199](../docs/CHUNK7-BATCH199-7E-PILOTS-2026-10-24.md) on `3842ad94` **PASS** again (4/0), same shape, Manual 309 `d24d86b0…`, recorded `capitalArrival.sourceArea` `b10e8651…`; development pilot): cold load of a two-owner mounted Manual, one native transfer to Oleg's (`fd225a93…`, autoSaveMode None), then one genuine `KMC_C7_CAPITAL` Manual | same-candidate two-owner mounted Manual (x4s shape: `persistence-save` / `multi-exploration-manual`) | `Assert-KmcChunk7CapitalArrivalObservation`: one retired pair (`AreaRetiredPairCount` 1, exact UMM line), main pair restored without Mount or preparation, retired pair separated with no owner or residue, one Manual read back with exactly the main pair | 1 |
| 59 | F06 capital-arrival cold consumer: the Manual from #58 cold-loads cleanly with one mounted pair and the other pair unmounted, no residue | owned `persistence-load` case `multi-exploration-capital-arrival-cold-manual` (blocked by #58 in batch196 and batch197; [batch198](../docs/CHUNK7-BATCH198-7E-PILOTS-2026-10-24.md) first native pilot **FAIL** (1/1, restoration exact): `ReadSource` refused "Staged source lacks its complete successful same-candidate two-owner Manual identity" because it compares the lineage Manual 307 write area (Spider Cave `b10e8651…`) with the load area (capital `ead426a6…`); diagnostic defect, fixed by the exact `sourceArea` lineage pin in `c4945317` (review CLEAR); [batch199](../docs/CHUNK7-BATCH199-7E-PILOTS-2026-10-24.md) on `3842ad94` **PASS** (2/0, restoration exact): Bind's readiness check passed for the three remote story companions, the capital Manual 309 restored exactly the main pair (`all-pair-presentation-restored`, 1 registry/active owner, 2 semantic and 1 presentation restores, 0 Prepare debt, retired 0, save allowed) with pair B unmounted out of scene; development pilot, not exact-package qualification) | #58 | `Assert-KmcChunk7CapitalColdObservation` plus the runner's separate-process proof `Assert-KmcChunk7CapitalColdSource` | 0 |

Rows 33–34 note (2026-10-24): these rows read the preview.71 F09 producer `KMC_C7_SMALL_FIXTURE`
through the owned-pairs selection. They do **not** depend on the two broken Small calibration
cases, `compiled-manual-derived-small-horse-rt` and `compiled-assisted-two-owner-small-horse-rt`.
Those cases require the retired `saveBeforeFieldSelection` source and never fed a row. The
owner retired those two cases on 2026-10-24 with a prelaunch refusal, keeping rows 33–34
unchanged ([record](../docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md),
[equivalence analysis](../docs/CHUNK7-7E-X6-OFFLINE-INVENTORY-2026-10-23.md)).

Rows not yet runnable are listed under Blockers.

## Explicit exclusions (agreed; not hidden failures)

| Item | Disposition |
| --- | --- |
| TB Charge delivery | Deferred. Its **cost-free refusal is tested** in rows 7–8. |
| RT Charge | Default-off is **tested** in row 9. Enabled RT Charge delivery is its existing supported contract; see blocker X2. |
| Defensive Mounted Combat feat (F05) | Not implemented. Deferred by owner scope. F05 has no row. |
| Paladin Divine Steed and other class grants | Outside this release (original scope). |
| Main-menu CharGen for the player's own main | Not selected. A level-1 main never reaches the Ranger seam, so it is covered by rows 45–46 ([F10 reconciliation](../docs/CHUNK7-F10-RECONCILIATION-2026-10-23.md)). |
| Retired stale level-up Auto families; legacy Manual 302→303 | Retired. Their prelaunch refusals are tested offline (`Test-Chunk7StaleLevelUpFamilies.ps1`). |
| Small calibration `compiled-manual-derived-small-horse-rt`, `compiled-assisted-two-owner-small-horse-rt` | Retired (2026-10-24). Refused before selection admission (`Test-Chunk7RetiredSmallCalibrationCases.ps1`). Replaced by the F09 calibration, rows 33–34 and the owner-seed chain. Lost: the full calibration on a same-candidate paid source ([record](../docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md)). |
| Optional extra mod combination | Not selected by the mission. |
| C12 and HUMAN PLAY (visuals, physical input, feel) | Owner-only. Automation does not generate it. See the owner checklist below. |

## Blockers before the matrix is complete

| ID | Rows affected | Blocker | Smallest route |
| --- | --- | --- | --- |
| X1 | C03 attack modes, C11 reactions, F04 staged moving actions, and single-pair F03/F08 death and incapacitation | Their only native scenarios (`chunk6*`, `chunk4*`) load the legacy **Working** fixture, whose base-game-only load fails (batch34). Decision: the preview.211 Chunk 6 evidence is **not** reused. | Partly resolved. Incapacitation is implemented as row 57 (`owner-seed-native-life-cleanup`). Its [batch193](../docs/CHUNK7-BATCH193-7E-PILOTS-2026-10-23.md) pilot FAILed on a diagnostic defect (fixed, review CLEAR). The [batch194](../docs/CHUNK7-BATCH194-7E-PILOTS-2026-10-23.md) re-pilot passed the life callback and healthy neighbour, then FAILed because the native turn controller prepares the unconscious rider's own N+1 turn. Decided as native behavior the test misjudged; an exact order-derived expectation plus an unmounted unconscious control was implemented. The [batch196](../docs/CHUNK7-BATCH196-7E-PILOTS-2026-10-24.md) pilot reached that check: the unconscious rider's round-3 turn matched the derived native slot in every field, but the check FAILed on a diagnostic counting defect (a null `pairedGrantIdentity` written from a `string` is a String-typed JValue, so all 41 window events counted as paired). Fixed (`dd360e32`); the [batch197](../docs/CHUNK7-BATCH197-7E-PILOTS-2026-10-24.md) re-pilot **passed** (9/0): exact rider turn, `pairedIdentities` 0, and the unmounted unconscious control available and matching. C03, C11, F04 and death proper are designed below and not implemented; see [X1 design](#x1-owner-seed-combat-representatives). |
| X2 | F02 enabled RT Charge, F03 spells and items, F08 native life (two-pair) | Cases `realtime-charge`, `realtime-casting` and `native-life-cleanup` exist in the selection and readers, but **have never run natively**. They take an acquired-field Manual source (the b165/b166 lineage); caster and charge eligibility of that field is unverified. | [Batch193](../docs/CHUNK7-BATCH193-7E-PILOTS-2026-10-23.md), zero-launch: `realtime-casting` is **uncovered on the b164 source** (no rider holds the prepared healing slot; memorizing needs a native rest no fixture step performs). Two-pair native life shares row 57's scenario and defect. [Batch194](../docs/CHUNK7-BATCH194-7E-PILOTS-2026-10-23.md) ran the chain on one candidate (c1r–c5 PASS). `realtime-charge` FAILed on a fixture limit: KMC correctly refused ("Mounted Charge requires a melee weapon"; the main Ranger wields none). Two-pair native life FAILed on the same native-turn assertion as row 57. [Batch196](../docs/CHUNK7-BATCH196-7E-PILOTS-2026-10-24.md) re-ran the chain (c1–c5 PASS) but did not run the two-pair native life: it shares row 57's diagnostic counting defect. Items: the existing Small presentation case (native scroll and potion while mounted, b136) is the honest F03 item route; [batch197](../docs/CHUNK7-BATCH197-7E-PILOTS-2026-10-24.md): two-pair native life **PASS** (9/0, control available) and the Small presentation item pilot **PASS** (11/0, scroll and potion presentation while mounted). Prepared-spell casting stays uncovered on these sources. |
| X3 | F07 `pending-schema3-control-manual` | **Resolved: retired** ([record](../docs/CHUNK7-RETIRED-SCHEMA3-CONTROL-2026-10-23.md)). It was the batch35 matched diagnostic control. Prelaunch refusal kept. **Residual gap:** it was the only cold load of a pending-only archive written by the live producer with genuine PairId and generation. Row 19 loads row 18's resave through the schema-2 adapter at generation 1. Rows 13–14 cover the live-producer cold load only in the non-pending-only TB-debt shape. | — |
| X4 | F06 three area transfers | `multi-exploration-three-area-transfers-manual` has never run natively. | **Development PASS on the Manticore route** ([batch196](../docs/CHUNK7-BATCH196-7E-PILOTS-2026-10-24.md), candidate `241bbd8b`): producer x4s PASS, then the cold pilot passed Spider Cave → Manticore Lair (`dfbc2423…`) → Spider Cave (`00becd53…`) → Manticore Lair, four worlds with both pairs active, restoration exact. Exact-package 7E qualification remains. History: [Batch194](../docs/CHUNK7-BATCH194-7E-PILOTS-2026-10-23.md): producer x4s PASS; the cold pilot FAILed at its first repeated-world record because the Horse subscriber expectation counted only the Medium Horse profile (the Small Ranger's Horse also subscribes). Fix reviewed. [Batch195](../docs/CHUNK7-BATCH195-7E-PILOTS-2026-10-24.md) re-ran the chain and producer (PASS); the cold pilot passed the first-world subscriber check but hung after the first transfer into Oleg's: all-pair area restoration was refused ("Both units must be directly controllable") and the run ended restoration-blocked. Diagnosed: Oleg's is the capital (route invalid; capital guard added). The original route pins were Spider Cave → Oleg's (`fd225a93…`) → Spider Cave (`00becd53…`) → Oleg's. |
| X5 | Rows 55–56 | **Designed and implemented; independent review CLEAR after three passes (d4af83ae, 020564f7); u1 known-defect admission implemented (review pending); native pilot pending.** Owner-approved Option A. `-UpgradeRehearsalStep u1-previous-install|u2-upgrade|u3-rollback` with a shared `-UpgradeRehearsalId` on `chunk7-normal-player-control`. Exact single-package allowlist (preview.211 `-b`); u2/u3 re-derive a hash-pinned passed u1 wherever read; observed loaded KMC recorded; u1 records must be retained at the lab root for later reads. `Test-Chunk7UpgradeRehearsal.ps1` 111/0. [Batch198](../docs/CHUNK7-BATCH198-7E-PILOTS-2026-10-24.md): pilot **not run**; observer.6 cannot be packaged because `tools/KmcRemovalObserver/Info.json` still says `0.1.0-observer.5` while `ObserverIdentity.cs` says `0.1.0-observer.6`, and `Validate-ObserverPackage.ps1` refuses the pair. [Batch199](../docs/CHUNK7-BATCH199-7E-PILOTS-2026-10-24.md) (observer.6 `eb9a5d87…` at `3842ad94`): u1 **FAIL** (outer; observer 27/0, loaded preview.211 DLL `11e99309…` observed, restoration exact). The released preview.211 logs "[Exception] Horse companion blueprint registration: … no longer has seven stock options" on a base-game-only install (it admits only the CotW seven-option Ranger list; fixed in Chunk 7 `81c191af`), so u1's ordinary quiet-start check refuses. u2/u3 not run (no passed u1). **Lead decision (October 24), implemented offline:** u1 admits exactly that one pinned line, once, only for the exact preview.211 `-b` package (package, manifest, DLL, MVID, version, commit), recorded in the UMM receipt as `admittedOldVersionKnownDefects` (id `preview211-base-game-ranger-allfeatures-horse-registration`, fix `81c191af`); any other text, count, extra error line, package or step refuses. u2 and u3 keep the strict quiet start, and the receipt counts the line so u2 shows it absent. The external reader re-judges the receipt on every normal-player PASS. `Test-Chunk7UpgradeRehearsal.ps1` 152/0 (the reconstructed real b199 u1 log passes u1). **u3 also stages preview.211, so under the strict rule it is expected to refuse on the same line**; that needs a decision before u3 runs. | Fresh native u1 (the b199 outer FAIL stays FAIL; a runner verdict needs a new run), then u2; decide u3's rule for the same preview.211 line. |
| X6 | FULL tier | **Inventoried; facades fixed; remainder classified** ([record](../docs/CHUNK7-7E-X6-OFFLINE-INVENTORY-2026-10-23.md)). 19 stale checks fixed. Remaining: six Chunk 6A historical-ledger checks (fail at the preview.211 tag too), The two Small Manual checks are replaced by the refusal test of their two retired calibration cases ([record](../docs/CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md)). The combat and Working-load facades were fixed on 2026-10-24. `scripts/Test.ps1` omits the Chunk 7 umbrella; run both. | Report the six historical checks as classified unless the owner selects a 6A ledger re-qualification. |
| X7 | Rows 42 and 33–38 | Their source selections pin earlier source run IDs and SHAs. Rows 2–29 consume F's own row-1 output, so their selections need mechanical re-pinning to F's run IDs. | Done by an unchanged reviewed generator per batch. |

## X1: owner-seed combat representatives

All four use the existing two-pair TB engine (`Chunk7CombatScenario`) on the owner-seed-derived
mounted start (row 1 output, cold-restored mounted, as rows 7–8). The **enemy is the existing
owner-approved diagnostic target** (`DiagnosticCombatTargetService`, spawned per run at a validated
placement), so no seed monsters are needed. The Old Sycamore seed's live dungeon monsters are not
used: fighting them would need AI-driven encounters the harness cannot bound. Every case loads
base-game-only, writes nothing, and is read by the shared owned-pairs, cleanup and family readers.

| Behavior | Status | Case | Native stimulus | Assertions (external reader) |
| --- | --- | --- | --- | --- |
| F08/F03 incapacitation | **Implemented** (offline), pilot due. Subject precondition: rider B `fd213210` must be not essential, not immortal, not the main character and allowed to die (`Chunk7CombatNativeLife.cs:33-35` refuses otherwise, before any damage). If the pilot refuses for that reason, the behavior is recorded as uncovered, never waived. Start lineage: the derived TB rider-first runs b7s1 and b8s2 PASSed; b5s5 FAILed and is retained. | `owner-seed-native-life-cleanup` → `chunk7-two-pair-native-life-cleanup` | One labelled native `RuleDealDamage` from the target, bounded to the HP+1 to HP+Con living window; stock `UnitLifeController` tick | Restored mounted start (no Mount input); one `UnitIncapacitated` delivery through the stock Tick/SetLifeState chain; pair B unmounted with its original split round; no granted preparation; healthy pair A acts before and after; surviving mount gets exactly one fresh independent turn in N+1; budgets of unrelated actors unchanged. Reader: `Assert-KmcChunk7NativeLifeEvidence` plus `Assert-KmcChunk7OwnerSeedDerivedCombatStart`. |
| F08 death proper | Designed | variant of the above with damage past HP+Con | Same single labelled `RuleDealDamage`, sized to cross the death threshold | `IsDead`; one `UnitDied`/death delivery, mount survives unmounted with one independent turn; zero writes. Needs a death-trigger reader branch. **Risk:** rider B (`fd213210`) is a story companion; death may be blocked by `Immortality`/essential policy, which must be refused, not bypassed. |
| C11 reactions | Designed | `owner-seed-combat-reaction` (new TB kind) | Pair A, adjacent to the target, receives an ordinary native ground-move command out of the target's threatened area | Exactly one native attack of opportunity by the target against the moving actor, recorded by the rule observer; the target's `AttackOfOpportunityCount` drops by one; the pair's move is spent once on the mount and never refunded; any interruption leaves no stale pair command or budget; pair B and unrelated actors unchanged. A second row uses the documented prone stand-up provocation (target stands from native prone) for the pair's own reaction, if a native, non-fabricated prone source exists in the fixture; otherwise that half is recorded as uncovered. |
| C03 attack modes | Designed; fixture-dependent | `owner-seed-attack-modes` | Rider A full attack on the target with the native Rapid Shot activatable off, then on (two turns), plus an unmounted control | Native full-attack plan size and attack bonuses equal the unmounted control under the same toggle; one native cost per plan; no extra mount action. **Precondition:** rider A must carry a ranged weapon and the Rapid Shot fact. Their presence in the row-1 field is unverified; check with a zero-launch read of the row-1 Manual first. BAB and haste variation need a native source (scroll stack or level); synthetic stat edits are not accepted. |
| F04 staged moving actions | Designed | `owner-seed-staged` | TB: pair ranged attack from beyond range (mount approach, then rider shot), and move then cast a Standard scroll then move | Shot origin equals the rider position after the mount's approach; approach charged once to the mount's move; the cast is stationary at its real location; defensive or motion checks run natively where the rules require; no check is skipped after the fact; movement exhaustion refuses further moves. Seams to reuse: `RiderRanged` pair command, `Chunk6StagedScenario` plans (`C6D-move-cast-move`, `C6D-double-move-ranged`), native scroll stacks (two or more units per measured spend). |

The four designed rows are not admitted to the matrix until each is implemented, reviewed and
piloted. If any has no honest route on this fixture (for example, no ranged weapon or Rapid Shot),
it is recorded as an uncovered behavior, not waived.

## Native batches (at most 5 runs each; producers before consumers)

The minute figures are estimates of wall-clock time: WhatIf, staging, game and restoration.
The game portion measured from evidence write spans is 1.3–3 minutes for most runs, 10–40
minutes for six-pair stress, and about 3 minutes for F11.

| Batch | Runs (rows) | Est. min |
| --- | --- | --- |
| B1 | 1, 2, 3, 4, 5 | 50 |
| B2 | 6, 7, 8, 9, 10 | 55 |
| B3 | 11, 12, 13, 14, 15 | 50 |
| B4 | 16, 17, 18, 19, 20 | 50 |
| B5 | 21, 22, 23, 24, 25 | 50 |
| B6 | 26, 27, 28, 29, 30 | 50 |
| B7 | 31, 32, 33, 34, 35 | 50 |
| B8 | 36, 37, 38, 39 | 80 |
| B9 | 40, 41, 42, 43, 44 | 55 |
| B10 | 45, 46, 47, 48, 49 | 50 |
| B11 | 50, 51, 52, 53, 54 | 50 |
| B12 | 55, 56 (after X5), 57 (after its X1 pilot) | 40 |
| **Total** | **57 runs, 12 batches** | **about 11 h** |

Row 57 (X1) is in batch B12 because B2 is full. The X1, X2 and X4 pilots add their own development batches, and those cases are then added as
rows. A failure follows mission §8: retain it, fix it, review it, freeze a successor, then rerun
the affected rows plus their dependent consumers.

## Offline tiers (order)

1. **Freeze:** `scripts\Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker`. It runs once,
   with the package build, and includes Fast. Do not run Fast separately.
2. **FULL:** `scripts\Test-Tiers.ps1 -Tier Full -AssemblyTarget Kingmaker -SkipBuild`, on F's
   frozen worktree after X6.
3. **Readers:** external readers run per row as each batch settles.

## F11 final measurement (per the predeclared protocol)

Rows 39, 40, 41 and 42 above. They run on F with the protocol's windows, warmups and acceptance
criteria, which are fixed and not moved. The thresholds are 0.5 ms at two pairs and 1.0 ms at
six pairs, plus stability, quiet logging, scaling against the 0-pair noise band, and the
allocation trend. They are reported with their instrumentation limits.

## F12 install, upgrade, rollback, removal (temporary guarded installs only)

Every step uses the existing guarded transaction. That transaction snapshots the whole `Mods`
tree, UMM `Params.xml`, the profile and the saves, then stages and restores exactly. No step
leaves KMC installed and **no permanent human installation is performed.**

| Step | Route | Existing support | Gap |
| --- | --- | --- | --- |
| Fresh install | KMC staged into a base-game-only isolated tree with no prior KMC settings; ordinary start, Mount, move, Dismount (row 42); plus `mod-load-smoke` | Runner, `BaseGameIsolation.ps1`, Mods transaction | none |
| Upgrade | Stage preview.211 (`artifacts\KingmakerMountedCombat-0.1.0-chunk6d-preview.211-chunk6-closeout211-*-diagnostic.zip`), make a Manual on a seed copy, then stage F on the same copy and load (schema migration, settings carry-over) | Transaction, migration rows 17–22 | No runner mode stages a **historical** package. The suite and package pins admit only F. A narrow, reviewed "previous-package" staging mode pinned to the preview.211 manifest is needed. |
| Rollback | Stage preview.211 against the **pre-migration** copy from the upgrade step. Expect a clean load. Confirm that a post-migration save is refused or degraded safely. | Same | Same gap. Expectations are documented in the player guide. |
| Removal | Clean-save preparation, then a genuine DLL-absent cold load (rows 31–32) | `persistence-p07-*`, `CurrentRemovalEvidence.ps1` | none |
| RT and TB sessions | Rows 7–9 on F | Existing | none |

### X5 design: upgrade and rollback (decision needed; not implemented)

**Previous version identity.** The released preview.211 package is
`KingmakerMountedCombat-0.1.0-chunk6d-preview.211-chunk6-closeout211-b-diagnostic.zip`
(package `e89f431a…`, manifest `ca54c37c…`, DLL `11e99309…`). Its manifest commit, the source it was
built from, is `a0d70fa4`; the release tag `v0.1.0-chunk6-preview.211` points at the merge `7fa667b2`. The `-a` package beside it is not the release and must not be admitted.

**The constraint that forces a decision.** A genuine upgrade proof needs a save that preview.211
itself wrote with mounted KMC state. No admitted fixture can produce one:

- preview.211's automation has no owner-seed setup (Ranger 4, native Horse choice, Mount). That
  code is Chunk 7. Its own save scenarios (`persistence-p01…p07`) run on the legacy Working
  fixture, whose base-game-only load fails (batch34), the same root cause as X1.
- Retained preview.211 campaign archives are Working lineage, so they hit the same failure.
  Absent foreign mods may not be reinstalled and archive references may not be stripped.
- Driving preview.211 with the current request protocol is unverified. Its `RuntimeProtocol`
  admits a different scenario set and its validator may refuse current request members.

**Options**

| Option | What it proves | Cost / risk |
| --- | --- | --- |
| **A. Installation-level rehearsal plus format chains (recommended)** | U1: stage preview.211 (pinned to the three hashes above) in an isolated base-game-only tree and cold-load a fresh NO_MODS seed copy (no KMC data, valid for any version): the old mod loads, its log is quiet and its UMM settings are created. U2 (upgrade): replace only the KMC folder's package files with F's, **keeping** the settings written in U1, as a player would; load the same seed and run the row-42 request-free controls (Mount, move, Dismount). U3 (rollback): restore the U1 Mods state and the pre-upgrade save copy; preview.211 loads it cleanly. Save-format upgrade is carried by the schema-1/2 to schema-3 migration chains (rows 17–22). | One narrow "previous-package" staging mode: an exact allowlist of one manifest, never a general second package, with identity bound in the receipt; a settings carry-over step; three short native runs. No new fixture. Does **not** prove that a preview.211-written mounted archive migrates; rows 17–22 use derivatives made from schema-3 archives. |
| B. Genuine preview.211 mounted archive | A real old-binary write migrated by F, then rollback with the pre-migration copy | Needs a working preview.211 save producer: either the Working fixture repaired (blocked by X1 and the foreign-mod rule) or Chunk 7 owner-seed setup back-ported into preview.211 (a new old-version binary, no longer the release). Not recommended. |
| C. Defer F12 upgrade/rollback to owner play | Owner installs preview.211, plays, upgrades, rolls back by hand | Manual intervention, which the owner asked to avoid; no structured evidence. |

**Recommendation:** Option A, with the gap stated in the release notes: "upgrade of mounted
state is qualified on migrated derivatives of every supported historical schema, not on an
archive written by preview.211." The release notes also state that released preview.211 logs
"[Exception] Horse companion blueprint registration: InvalidOperationException - The exact Ranger
companion AllFeatures surface no longer has seven stock options." on a base-game-only install (it
admits only the seven-option Call of the Wild Ranger list; the base game has twelve), and that
upgrading to the Chunk 7 build fixes it (`81c191af`). Rollback is documented as restoring the pre-upgrade save with
the previous package; a post-upgrade save is not certified under preview.211 and is not loaded
in U3.

**Decision (owner, October 23):** Option A, with the gap stated in the release notes.

**Implementation (pending independent review and a native pilot):**
- **Runner:** `-UpgradeRehearsalStep` (`u1-previous-install`, `u2-upgrade`, `u3-rollback`) is admitted
  only with `chunk7-normal-player-control`, before any intake, approval or lock. u2 and u3 require
  `-UpgradeCarrySourceRunId` naming the passed u1 run; u1 refuses one.
- **Previous package:** `Get-KmcChunk7PreviousPackage` admits exactly one package: the `-b` leaf,
  its package, manifest, DLL and Info.json hashes, MVID, version and commit. The staged
  transaction's package hash must equal the request's `upgradeRehearsal.stagedKmc`.
- **Carry (u1 to u2/u3):** only the KMC-folder files the restored u1 transaction classified as
  expected runtime writes. That is the closed UMM assembly-cache shape, re-hashed from u1's retained
  staged-after tree, 1–4 files. They are laid beside the package files in the ready tree, only in the
  observer co-staging mode. A settings write in u1 would be an unexpected write, so u1 would fail
  instead of carrying it.
- **Known preview.211 defect (u1 only):** released preview.211 genuinely logs one Horse registration
  exception on a base-game-only install (batch199). `Get-KmcChunk7UpgradeAdmittedLogDefects` admits that
  exact full line, exactly once, only for step u1 with the exact pinned preview.211 identity; the UMM
  receipt (schema 2) records it under `admittedOldVersionKnownDefects` with its fix commit `81c191af`
  and [the Ranger repair](../docs/CHUNK7-RANGER-BOOTSTRAP-REPAIR.md). Every other error line, a second
  occurrence, altered text, another package and every other step refuse; the receipt counts the line
  (`knownOldVersionDefectLines`) on every run, so u2 must show it absent. u3 keeps the strict rule.
  `Assert-KmcChunk7NormalPlayerRuntimeBinding` re-judges the receipt with the same rule on every PASS.
- **u1 and u3:** observer `0.1.0-observer.6` `previous-package-load` mode loads the reference-free
  NO_MODS owner seed through its registry gate. It checks the exact previous assembly, the quiet
  start, that no save was added and that KMC logged no error. It drives no input.
- **u2:** the candidate over u1's carried folder state runs the request-free controls on the six-Horse
  Manual 308. UMM deletes every `*.cache*` in the mod folder when its own cache name is missing
  (`ModEntry.Load`, UMM 0.33.2.0; the name hashes the DLL's `LastWriteTimeUtc` and the UMM versions).
  So restoration admits removal of exactly the transaction-declared carried files, by path and
  staged bytes, as the named change `removed-carried-cache`. Every other removal stays unexpected.
- **u3:** deliberately lays u1's folder state, not u2's: rollback restores the previous install as
  the player had it.
- **Binding:** all three steps share `-UpgradeRehearsalId`, and u2 and u3 require a passed u1 of the
  same id. "Passed" is proved from u1's own records, each hash-bound to the next: runtime result, then
  game record (its step, id and the observed loaded preview.211), then observer result. Four hashes
  (result, game record, observer result and state) are pinned into the u2/u3 request. Wherever that
  request is read, the whole proof is re-derived from those records, the pins are compared, and the
  carried files are compared with the files that u1's classified writes yield. Carried caches must be byte copies of the previous DLL, under the lab staging
  root and not hard links. The game record names the step, the declared staged package and the KMC
  assembly the observer actually saw loaded.
- **Unenforced obligations for future consumers (not yet wired; no consumer exists):**
  - The row-42 binder must call `Assert-KmcChunk7NormalPlayerControlRow`, which refuses any rehearsal
    step. Until that binder exists, nothing enforces it.
  - The rows 55–56 consumer must enforce rehearsal completeness: one id, a passed u1, then u2, then
    u3, each binding the same u1. Each run only checks its own predecessor u1, so nothing yet proves
    that all three steps ran.
- **u3 folder state:** u3 lays u1's folder state by design, not u2's.
- **Params.xml:** each transaction restores the owner's file, so it is not carried across steps.
- **Tests:** `scripts/chunk7/Test-Chunk7UpgradeRehearsal.ps1` passes 111/0 after the third review's follow-up and 152/0 with the u1 known-defect admission (`Test-Chunk7NormalPlayerControl.ps1` 52/0); the Enter-to-Restore carry path is covered in `scripts/Test-BaseGameIsolation.ps1` (98/0).

## Docs deliverables

| Deliverable | Path (to write) | Contents |
| --- | --- | --- |
| Changelog | `docs/CHUNK7-CHANGELOG.md` | One concise list from preview.211 to F |
| Support and configuration guide | extend `docs/CHUNK7-PLAYER-GUIDE.md` | Supported profiles, up to six pairs, settings, Charge default-off, diagnostics opt-in and inert, refusals |
| Migration, rollback and removal | `docs/CHUNK7-MIGRATION-ROLLBACK-REMOVAL.md` | Keep pre-migration saves; older binaries are not certified for new-format saves; clean-save preparation; a KMC Horse campaign needs the mod |
| Owner play checklist | `docs/CHUNK7-OWNER-PLAY-CHECKLIST.md` | Exact package and save copy; **place Mount Companion and Dismount on the action bar by hand (KMC does not pre-fill slots)**; pointer feel, portraits, seat and contact, weapons, camera; one RT and one TB session; how to report |
| Engineering closeout | `docs/CHUNK7-ENGINEERING-CLOSEOUT-<date>.md` | Artifacts by verified path and identity, row dispositions, limitations |

## Final candidate freeze

1. **Commit.** Commit only after X1–X6 are resolved and reviewed, and the docs are written.
   Freeze from a clean, pushed commit on `codex/mounted-combat-chunk7-successor`.
2. **Worktree.** Create a detached frozen worktree `worktrees\chunk7-final-<sha8>`.
3. **Package.** Package once with `scripts\Package.ps1 -ArtifactQualifier c7-final-<sha8>`.
   Record the package, manifest, DLL SHA-256, MVID and source tree, and the root `bin\Release`
   DLL hash before and after packaging.
4. **Inspect the payload.** The package contains only `Info.json` and
   `KingmakerMountedCombat.dll`. It contains no game or Wrath assemblies, decompiled output,
   saves, logs, credentials or lab material. The preview.211 package stays unchanged.
5. **Candidate tier.** Run it once. Then freeze a new qualification suite with a new SuiteId,
   which pins the package, DLL, MVID and commit.
6. **No changes after freezing.** Make no product or observer change after the freeze. A
   doc-only change uses the frozen worktree and does not rebuild.
