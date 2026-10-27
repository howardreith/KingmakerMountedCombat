# Chunk 7 7E blocker X6: offline FULL and Chunk 7 umbrella inventory

Status: **inventory done; stale facades fixed; remaining failures classified.** Offline only.
Nothing here is a FULL PASS. No assertion, threshold or reader was weakened.

## How it ran

- **Source:** a detached worktree at `2372e0e6` holding the root `bin\Release` DLL `b2acbdeb…`
  (the batch192d build, identical to the HEAD product).
- **Runner:** each step of `scripts/Test.ps1` (103 steps) and of
  `scripts/Test-Chunk7NativeDiagnostics.ps1` (184 entries) ran in its own child process, exactly
  as its umbrella runs it. Both umbrellas stop at the first failure; this runner continued instead,
  so every failure is listed. Six umbrella entries that share one line were missed by the first
  pass and were then run separately; all six pass.
- **What FULL contains:** `scripts/Test.ps1` does **not** include the Chunk 7 umbrella. For 7E,
  run both explicitly.

## Fixed (stale test facades, compile lists or fixtures; product unchanged)

| Check | Cause (introducing product or reader change) | Fix |
| --- | --- | --- |
| `Test-ChargeFixtureContracts` | `474f0d85`: the trace's `Complete` requires installed hooks | The blank trace is asserted incomplete, then marked installed |
| `Test-Chunk7AssistedFixtureProbes` | `b6d3b965`: F09 fifth and sixth output constants | Facade constants |
| `Test-Chunk7ArtifactProducer` | Two `SourceLeaf` classes in one file; publisher names the Small and stock Mammoth fixture scenarios (`fbb1116a`) | Constants scoped to the declared class; two scenario facades |
| `Test-Chunk7AssistedAcquisitionWiring` | Legacy cumulative mode completes with Manual 303 (stale Auto retirement) | Legacy fixture takes the Manual 303 leaf |
| `Test-Chunk7NativeFieldRoster` | `053b3542`: two-owner and post-field Manual terms | Facade fields and three checks |
| `Test-Chunk7MediumAcquisition`, `Test-Chunk7MediumContinuation` | `db548da7`: round hook recorded as `UnitCombatState.OnNewRound` (as retained native evidence shows); cumulative Manual projection | The frozen fixtures are projected at load, as other tests already do; the cumulative fixture uses the existing synthetic Manual converter |
| `Test-Chunk7ProgressionHeader` | Header check now keyed on the write's native type | Write type added; Manual header checks added |
| `Test-Chunk7ProgressionSession` | `ee2bdfc9`: owned Manual probe in drain | Facade plus three drain checks |
| `Test-Chunk7SmallCleanup`, `Test-Chunk7SmallManualCleanup`, `Test-Chunk7SmallManualRoster` | `f870f8e8`: presentation fixture, private calibration lease, fixture Manual route | Facades plus checks for each new branch |
| `Test-Chunk7OrdinarySaveDescriptor` | Manual-derived family no longer has a level-up Auto observer (stale-family retirement) | The removed product predicate is no longer extracted; the two retained Auto observers stay tested |
| `Test-Chunk7SaveBoundaryProtocol`, `Test-Chunk7RepeatedWorldProtocol` | `fbb1116a`: `RuntimeProtocol` references the Small and stock Mammoth fixture selections | Both sources added to the compile lists |
| `Test-Chunk7ArchivePostLoadIdentity` | `d96a4b06`: retained pending re-entry guard | Facade; the reset check also asserts its release |
| `Test-Chunk7RepeatedLoadAuthority` | `cba8262a`: zero selected loads is valid (native bootstrap) | Zero is an accepted case; −1 and 129 stay refused |
| `Test-Chunk7OwnedReplayEpochs` | `38318d57`: pending Manticore transfer | Facade plus a release-refusal check |

## Remaining failures (classified, not converted)

| Check | Classification | Root reproduction |
| --- | --- | --- |
| `Test-Chunk6aLedgerProtocol`, `Test-Chunk6aRegressionLedger`, `Test-Chunk6aLegacyLedger`, `Test-Chunk6aAreaLedger`, `Test-UnrelatedFullLedger` | **Historical contract.** The released Chunk 6A ledger pins its reader harness; any changed reader refuses it. Already recorded in `CHUNK7-KNOWN-PRODUCT-DEBT.md`. | Fail identically at the preview.211 tag `7fa667b2` (LF and CRLF checkouts), in the frozen `worktrees/qualification211`, and at the ledger's recorded harness revision `7e49e034`. The closeout's FULL PASS could not be reproduced in the current lab; the cause is unresolved. |
| `Test-Chunk6aSupportingEvidence` | **Historical evidence under a changed contract.** The current reader requires `CM01-combat-mount-preparing-admitted` (owner decision of October 18, `7930313e`); the retained preview.211 supporting artifacts hold the old `-refused` row. | Passes at the preview.211 tag. |
| `Test-Chunk7SmallManualEvidence`, `Test-Chunk7SmallManualSource` | **Resolved: the two cases they test are retired** (owner decision, 2026-10-24; [record](CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md)). The umbrella now runs `Test-Chunk7RetiredSmallCalibrationCases.ps1` in their place. | Introduced by `ed40ef95` (October 21). |
| `Test-Chunk7CombatCleanupAdapter`, `Test-Chunk7ObserverAcquisition` | **Fixed 2026-10-24** (follow-up below). | Introduced by `8a13f714`, `8685fb5f` and `33e729a8` (October 18 and 19). |

## For the 7E FULL run

FULL cannot pass as defined while the six Chunk 6A historical checks run against the current
harness. Running them is a historical replay of the released ledger, not a qualification of the
final candidate. 7E reports them as classified, with this reproduction, unless the owner selects
a Chunk 6A ledger re-qualification. The four deferred Chunk 7 checks must be repaired before
the freeze.

## Follow-up, 2026-10-24 (offline only)

The Chunk 7 umbrella entries were re-run in their own child processes, continuing past failures.
That run was at `37c09c68` plus this follow-up's facades.

Fixed with test facades only. Product source, assertions and counts are unchanged. Each count
equals the last passing count, measured at the commit just before the break.

| Check | Cause | Facade | Count |
| --- | --- | --- | --- |
| `Test-Chunk7CombatCleanupAdapter` | `8a13f714`: cleanup releases the registered-disable observer | `EndRegisteredDisableObservation()`, a no-op: no registered-disable diagnostic is attached in the fixture | 40/0 (40/0 at `7c1bcadc`) |
| `Test-WorkingLoadObservation` | Not in the original table. `38318d57` (October 19) added `SmallFixture`, `fbb1116a` added `StockMammothFixture` and `e1f500ce` added the `F11NativeAllocationSelection` property to the exact `RuntimeRequest` regions that the connection template splices in | Two null `RuntimeRequest` properties and an empty `F11NativeAllocationSelection` type in `WorkingLoadConnectionTests.cs.template` | 9 stages, connection 53/0 (identical at `9e9f8d5a`) |
| `Test-Chunk7ObserverAcquisition` | `8685fb5f`, `33e729a8` | Fixed in `37c09c68` | 72/0 + 13/0 |

### Small Manual: what is needed

Both tests read `tests/Chunk7Diagnostics/SmallManualFixture.json`. Its embedded paid Small
source (`source.acquiredManual.sourceAcquisition`) is the retired Auto-derived acquisition:
`nativeWriteLeaf` `Auto_1.zks`, type `Auto`, `autoLevelup` true and `saveBeforeFieldSelection` true.
Since `ed40ef95`, `Assert-KmcChunk7ManualDerivedSelection` admits only
`Manual_2`/`304`/`307`/`308` Manual-derived leaves. `Test-Chunk7SmallManualSource.ps1` also
builds its source request with `Auto_1.zks`.

A clean projection is not possible. The Small Manual family still asserts the retired contract
in both places:

- **Reader:** `Chunk7SmallCalibrationEvidence.ps1`, lines 528 and 616, requires
  `sourceAcquisition.nativeWrite.saveBeforeFieldSelection == true`.
  `Assert-KmcCumulativePriorSource` requires the old profile-inventory shape, with `masterId`
  on every party row.
- **Product:** `Chunk7SmallCalibrationScenario.Manual.cs`, lines 109 and 231, requires the same
  `saveBeforeFieldSelection == true`.

The current Manual field acquisition writes after the field selection
(`saveAfterFieldSelection`, `Chunk7ManualDerivedScenario.Build.cs` line 380). Its fixture
(`AcquiredManualPaidFixture.json`) has no `saveBeforeFieldSelection` and no `masterId` rows.

Two projections were tried offline in scratch and not committed:

- **Transplanting the regenerated `AcquiredManualPaidFixture` observations** fails on the two
  retired-contract assertions above.
- **Renaming only the selection leaf to `Manual_2`** passes 71/0, but it leaves an observed write
  of type `Auto`, `Auto_1.zks`, saved before the field selection, under a Manual-field
  selection. It is internally inconsistent, so it was rejected.

An owner decision is needed. Either the Small Manual product scenario and reader adopt the Manual
field source contract (`saveAfterFieldSelection`, type `Manual`, the current inventory shape),
after which the fixture can be regenerated from `AcquiredManualPaidFixture`; or the Small Manual
family is retired like the other stale level-up Auto families.

### Small Manual equivalence analysis (2026-10-24, offline and read-only)

**Correction: rows 33–34 do not depend on the broken cases.** Three things are separate here:

- **The broken cases.** Only two Small calibration cases are broken:
  `compiled-manual-derived-small-horse-rt` (call it A1) and
  `compiled-assisted-two-owner-small-horse-rt` (A2). Both require the retired
  `saveBeforeFieldSelection` source (`Chunk7SmallCalibrationScenario.Manual.cs:109` and `:221-245`;
  reader `Chunk7SmallCalibrationEvidence.ps1:528` and `:616`).
  - Neither case is a matrix row, and no native run of either is recorded.
  - A2 is unsatisfiable only by inference: the sole emitter of `saveBeforeFieldSelection` is the
    retired ordinary acquisition (`Chunk7OrdinaryAcquisitionScenario.cs:291`). A2 has not been
    run against an assisted chain.
- **Rows 33–34 (`small-fixture-persistence-save`/`-load`).** These are owned-pairs cases. They
  read the preview.71 F09 producer `KMC_C7_SMALL_FIXTURE`, run `…world71-b53s1-small`
  (`Chunk7OwnedPairsSelection.cs:50-54`, `:131-137`; `Chunk7OwnedPairFixture.cs:316-365`), which
  is unaffected.
- **The F09 calibration cases.** `compiled-owner-seed-fixture-small-horse-rt` and its
  presentation case read the same producer and are also unaffected. That case passed natively 9/9
  on game and reader in [batch72](CHUNK7-BATCH72-SMALL-ORDINARY-NATIVE-2026-10-20.md).

**What each source proves.** In the table:
- **A1** is the broken single-owner case.
- **F09 cal** is the F09 calibration.
- **33–34** are matrix rows 33–34.
- **Owner-seed** is c4 → c5 → x4s → x4c and rows 58/59.

| Proof | A1 | F09 cal | 33–34 | Owner-seed |
| --- | --- | --- | --- | --- |
| Full declared body (`DeclaredSmallRiderBody.Refusal`: preset, MaleHalfling, left-handed false, mirror, scale, Doll set, 12 ordered `CurrentEquipmentSlots`, visual set, Horse `4016c7db…` size 5) | each tick (`Chunk7SmallCalibrationActions.cs:37-45`) | same | through `Resolve` at bind (`Chunk7OwnedPairFixture.cs:119-121`) and in the product's restore and invariant | through `Resolve` at x4s bind, x4c cold bind and rows 58/59. c4/c5 check only race, preset, skeleton, gender, handedness and size 3 |
| Cold body equals the saved source body | A1 source body | all 20 fields (`FixtureManual.cs:93-105`) | not checked | x4c `ProfileId == Resolve` (`Replay.cs:197-201`) |
| Literal `small-default-halfling-horse-v1` in the archive | — | declaration only | yes (`Chunk7OwnedPairsEvidence.ps1:390`, `:424`) | no; checked by regex and self-consistency only |
| Native Mount, mounted Manual write | **no**: zero writes, unmounted source (`Chunk7SmallCalibrationScenario.cs:195-198`, `Manual.cs:133-139`) | **no** | yes | yes, x4s (`Chunk7PersistenceScenario.cs:249`, `:497-533`) |
| Separate cold load of a *mounted* Manual | **no** | **no** | yes, with archive canonical equality (`Chunk7OwnedPairsEvidence.ps1:438-465`) | yes in code (x4c, rows 58/59). Natively it reached restore only; b195 x4c FAILed at the capital transfer; rows 58/59 not run |
| No preparation and no resource change on cold load | yes | yes | owned reader | yes (`Replay.cs:149-157`) |
| Mounted actions (move, two primaries, Dismount) and 7 pose phases (configured, healthy, 7 bones, no foot clamps) | yes | yes | no | no; `ValidateMountedInvariants` view parenting and pose health ran only at area admission, never natively |
| Presentation (scroll and potion while mounted) | no | presentation case only (not run in b194/b195) | no | no |
| Numeric seat, offset or scale threshold | no | no | no | no |
| Same-candidate paid Small lineage | yes (same commit, DLL and MVID) | no; cross-candidate preview.71 | no; preview.71 | yes |

**Decision (owner, 2026-10-24): (a), scoped to A1 and A2.** Implemented in the [retirement record](CHUNK7-RETIRED-SMALL-MANUAL-CALIBRATION-2026-10-24.md).
- Retire the two cases the way the stale families were retired: a prelaunch refusal in the stale
  registry, and the two offline tests become refusal tests.
- **Named replacement:** the F09 calibration for body, pose and action calibration; rows 33–34
  for the mounted Manual and the separate cold load; owner-seed x4s and x4c for current-candidate
  paid Small lineage.
- **Rows 33–34 need no re-pointing.**
- **What is lost:** only A1's pairing of the full calibration (actions, pose phases, and body,
  progression and resource equality) with a *same-candidate* paid Manual-derived source. F09
  calibration proves the same calibration on a cross-candidate unmounted source. Owner-seed proves
  same-candidate lineage only through `Resolve`, with no pose or action checks.
- No row loses a check, because A1 and A2 never fed one.

**Option (b), for comparison: migrate A1 and A2.** This needs:
- product changes to both calibration gates (`Manual.cs:109`, `:221-245`);
- reader changes (`:528`, `:616`, and the `masterId` shape in `Assert-KmcCumulativePriorSource`);
- a fixture regenerated from `AcquiredManualPaidFixture`;
- native runs of a same-candidate single-owner chain (Manual-derived, then acquired derivative,
  then calibration) and of an assisted chain.

Because it changes the product, it needs a new candidate, which costs a fresh campaign plus the
matrix. The only gain is the same-candidate calibration noted above.

## Follow-up, 2026-10-24: `Test-ScenarioDispatchEvidence.ps1` (offline only)

This check is in neither `scripts/Test.ps1` nor the Chunk 7 umbrella, so FULL does not run it. Earlier
records noted that it "fails identically at `9cf18707`". It was reproduced once more, in a temporary
detached worktree (LF re-materialized, removed afterwards), with the root checkout untouched:

| Commit | Result |
| --- | --- |
| preview.211 tag `7fa667b2` | PASS 82/0 |
| `ee2bdfc9~1` | PASS 165/0 |
| `ee2bdfc9` (October 14, "Add guarded assisted fixture preparation and cold inventory") | FAIL: "Compound registry differs from reviewed exact families" |
| `9cf18707`, HEAD `c2c451d3` | FAIL, same message |

**Classification: stale test expectation; harness-only; product unaffected.** `ee2bdfc9` registered
`chunk7-assisted-fixture-preparation` and `chunk7-assisted-cold-inventory` as compound runtime scenarios
in `scripts/runtime/ScenarioDispatchEvidence.ps1` without adding them to the test's reviewed family list
(`$chunk7Families`). `38318d57` (`chunk7-small-fixture-preparation`) and `fbb1116a`
(`chunk7-stock-mammoth-fixture-preparation`) added two more the same way. The registry at HEAD holds
those four plus the 20 reviewed Chunk 7 families and the eight 6C/6D/6E roots. The check stops at that
first mismatch, so the later per-family reader checks did not run at HEAD. Fixing it means adding the
four families with their dedicated readers to the list and confirming the later checks pass; this is a
harness change, to be made before the 7E freeze or reported as classified.

**Classification update (2026-10-24): fixed.** The four families are now in `$chunk7Families`, each with
its one dedicated reader (`Test-KmcChunk7AssistedFixtureScenario`, `Test-KmcChunk7AssistedColdInventoryScenario`,
`Test-KmcChunk7SmallFixtureScenario`, `Test-KmcChunk7StockMammothFixtureScenario`), matching the registry
exactly; no matching was broadened and no product or reader changed. The later per-family checks now run
and pass: **PASS 181/0** (165/0 at `ee2bdfc9~1` plus four checks for each added family).
