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
| `Test-Chunk7SmallManualEvidence`, `Test-Chunk7SmallManualSource` | **Stale fixture, deferred.** The Small Manual fixture is built on the retired Auto-derived acquired field (`Manual_7`); since `ed40ef95` the reader admits only closed field Manuals (`Manual_2`/`304`). It needs a synthetic Manual projection like `Convert-Chunk7CumulativeManualFixture.ps1`, not a facade. | Introduced by `ed40ef95` (October 21). |
| `Test-Chunk7CombatCleanupAdapter`, `Test-Chunk7ObserverAcquisition` | **Stale combat facades, deferred** to avoid colliding with the parallel X1 combat work. Missing `EndRegisteredDisableObservation` (`8a13f714`), `UnitCombatPrepareController` and `debtGame` (`8685fb5f`). | Introduced by those commits (October 18). |

## For the 7E FULL run

FULL cannot pass as defined while the six Chunk 6A historical checks run against the current
harness. Running them is a historical replay of the released ledger, not a qualification of the
final candidate. 7E reports them as classified, with this reproduction, unless the owner selects
a Chunk 6A ledger re-qualification. The four deferred Chunk 7 checks must be repaired before
the freeze.
