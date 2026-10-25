# Chunk 7 — 7E final exact-package qualification: executable run plan

Status: **PLAN, NOT RUN.** Written offline on October 24, 2026 at HEAD `c2c451d3`; gates G3, G6 and G7 updated
the same day. No final candidate F exists. Nothing here is a PASS. Rows refer to the [7E matrix](CHUNK7-7E-QUALIFICATION-MATRIX.md)
(rows 1–81; row 55 is two launches). Governing rules: [AGENTS.md](../AGENTS.md),
[VALIDATION-POLICY](../docs/VALIDATION-POLICY.md), the [F11 protocol](../docs/CHUNK7-F11-ACCEPTANCE-PROTOCOL-2026-10-23.md)
and the [X6 inventory](../docs/CHUNK7-7E-X6-OFFLINE-INVENTORY-2026-10-23.md).

**Totals (updated October 24 after the folds, rows 75–76 and the mounted6 rows 77–81):** 82 native launches in 17 batches
(B1–B15 and B17 five each, B16 two), about 2.2 h offline tiers plus about 9.6 h native plus about 0.75 h ledger and
closeout: **about 12.6 hours**, excluding failure handling. The folded halves lengthen r57 (heal and stand-up, rounds N+2 to at most N+6)
and cr (round N+1 approach and cast) by an estimated 2–4 min each of game time.

## 0. Pre-freeze gates (all must hold before step 1)

| Gate | Requirement |
| --- | --- |
| G1 | X1–X6 resolved and reviewed; docs deliverables written (matrix "Docs deliverables"). Identities stay `[TBD after 7E freeze]`. |
| G2 | The F11 acceptance arithmetic (§6) is either implemented as a reviewed reader committed before the freeze, or applied by hand in the closeout exactly as §6 states. A reader added after the freeze changes the harness commit and needs a new package (see §8). |
| G3 | **Met (2026-10-24).** `Test-ScenarioDispatchEvidence.ps1` is fixed, harness-only: the four Chunk 7 fixture/cold families registered by `ee2bdfc9` (`chunk7-assisted-fixture-preparation`, `chunk7-assisted-cold-inventory`), `38318d57` (`chunk7-small-fixture-preparation`) and `fbb1116a` (`chunk7-stock-mammoth-fixture-preparation`) are in its reviewed list, each with its one dedicated reader (`Test-KmcChunk7AssistedFixtureScenario`, `…AssistedColdInventoryScenario`, `…SmallFixtureScenario`, `…StockMammothFixtureScenario`). PASS 181/0 (165/0 at `ee2bdfc9~1` plus four checks per family). It is outside FULL and the Chunk 7 umbrella; run it once beside them. |
| G4 | Branch `codex/mounted-combat-chunk7-successor` clean and pushed; the freeze commit is that pushed HEAD. **No commit lands on the root branch from step 1 until the closeout** (the package manifest binds the harness HEAD; b196 refused when root advanced). Batch notes go to `analysis-cache/chunk7-continuation/final-*` receipts, committed only at closeout. |
| G5 | Preview.211 package `KingmakerMountedCombat-0.1.0-chunk6d-preview.211-chunk6-closeout211-b-diagnostic.zip` (`e89f431a…`, manifest `ca54c37c…`, DLL `11e99309…`) and the observer.6 source present and unchanged; owner seeds `KMC_C7_NO_MODS_OWNER_SEED` `3405d7c8…`, `KMC_C7_CHARGE_SEED` `1ec2f321…`, `KMC_C7_LEVEL6_SEED` `b1447ae7…` (row 76; intake `c3e0fc6f…`), `KMC_C7_MOUNTED6_SEED` `2ef426c4…` (rows 77–81; intake `f4e8afd3…`), `KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN` `90789101…` and every fixture source named below hash-verified. |
| G6 | **Deriver written; review recorded in CURRENT.md.** `tools/chunk7-7e/final-derive.ps1` (in the repo, so the freeze commit pins it; it is a selection deriver only, never a run plan or launcher). Offline proofs (receipts in `analysis-cache/chunk7-continuation/`): `-Mode Reproduce -Tag all` re-derives all 69 selection tags from each template's own predecessor receipts, byte-identical to the template request's selection and to every retained selection file (`final-derive-reproduce-01.log`, 69/0; 70/0 on October 24 with the added tag `x3`, row 75); `final-derive-cross.ps1` re-pins templates onto earlier candidates' own runs and reproduces those batches' retained selections or recorded request selections byte for byte (`final-derive-cross-04.log`, 21/0: b198 c2–c5, x4s, r58; b197 c3, c4, x4s, r58; b195/b194 x4c; b194 p2; world25 r02; b100/b89 r34; b117 r36, r54; b150 r44, r45; b151 r45). **Rows 33/34 → 48, 50, 51: yes, one pair suffices.** The progression readers and source admission (`Assert-KmcChunk7OwnedProgressionSelection`, `Get-KmcChunk7PaidProgressionSource`, `Copy-KmcChunk7ProgressionInputs`) only read the producer and cold receipts and copy the archive into the consumer's own run; nothing marks a source consumed, and the pilots made separate pairs only because b89, b100 and b108 were different candidates (the b108 cold already fed rows 48 and 49, the b100 cold rows 51 and 52). `final-shared-small-pair-proof.ps1` derives r48, r50 and r51 from the one b108 pair and runs the selection and descriptor validators and the read-only source admission: 3/3 admitted (`final-shared-small-pair-proof-01.log`). No batch or launch count changes. |
| G7 | **Met (2026-10-24, option (a)).** Row 32 consumes F's own r31. `Get-KmcOwnerStockCleanupSource` (`Chunk7OwnerStockRemovalFixtures.ps1`) keeps the batch65 cleanup Manual (`20261020-c7b-world81-b65s1-owner-stock-removal`, `2c46c270…`) accepted exactly as before, for history only, and admits a final-campaign `<date>-c7e-final-B<n>-r31[r]` run only when: the runner's new `-OwnerStockCleanupRunMapPath` names the executor's `final-runs.json` whose `r31` receipt carries that run id, `readerVerdict` PASS, `restorationVerified` true and the SHA-256 of its `runtime-result.json` (re-hashed before and after); its request is `persistence-p07-save`/`prepare-removal` with `ownerStockRemoval`, on the stock Mammoth input `e475d49c…`, on the consuming run's own commit, DLL, MVID and qualification suite; its owner record, outer PASS, restoration flags and native removal evidence pass `Assert-KmcCurrentRemovalSource`; and its `Manual_306_KMC_CLEANUP.zks` equals the expected SHA-256 (the r31 `removal-prepared` cleanup write). The runner admits the map only for the owner-stock no-DLL consumer and refuses it with the batch65 source. Because F's cleanup bytes are not known in advance, the request validator (`Test-RuntimeRequest.ps1`), the product request contract (`RuntimeProtocol.cs`) and the observer request (`ObserverRequest.cs`) now accept any genuine 64-hex cleanup hash other than the stock input with a bounded length (batch65's hash still requires its exact length); the campaign identity stays exact and the runner's source gate is the provenance authority. Tests: `Test-CurrentRemovalEvidence.ps1` 64/0 (2 positive and 21 refusals of the final source, 2 batch65 refusals), `Test-OwnerStockObserverRequest.ps1` 7/0, components 877/0. |

## 1. Freeze and package (once)

1. `git worktree add --detach worktrees\chunk7-final-<sha8> <sha>`; re-materialize LF with per-command
   `git -c core.autocrlf=false rm --cached -r . ; git -c core.autocrlf=false reset --hard`. Never run
   `git config` against any worktree. Record `git status` clean.
   **Branch fact (b206):** a detached HEAD records an empty branch, which `Test-RuntimeRequest.ps1:349` refuses for
   every runtime request, so the b206 pilot worktree used a local branch `codex/mounted-combat-chunk7-b206-pilot`.
   The final worktree therefore needs a local `codex/mounted-combat-*` branch at the freeze commit (for example
   `git worktree add -b codex/mounted-combat-chunk7-final-<sha8> worktrees\chunk7-final-<sha8> <sha>`); the guard
   stays unchanged and the root branch is untouched.
2. Record the root `bin\Release\KingmakerMountedCombat.dll` SHA-256 before packaging.
3. Package once from the frozen worktree through a detached freeze-step helper (b205 pattern):
   `scripts\Package.ps1 -ArtifactQualifier c7-final-<sha8>`. Record package, manifest, DLL SHA-256,
   MVID, version, commit and source tree; root `bin\Release` DLL hash after.
4. **Payload inspection:** the zip holds exactly `Info.json` and `KingmakerMountedCombat.dll` (b196: 2
   entries). No game or Wrath assembly, decompiled output, save, log, credential or lab material.
   The preview.211 package hash is unchanged.
5. Build observer.6 once from the same commit (`tools/KmcRemovalObserver`, `Validate-ObserverPackage.ps1`;
   `Info.json` and `ObserverIdentity.cs` both `0.1.0-observer.6`), for rows 42, 55 and 56.

## 2. Candidate tier (once)

`scripts\Test-Tiers.ps1 -Tier Candidate -AssemblyTarget Kingmaker -SkipBuild` in the frozen worktree on the
package build. Expected: 6/0 (history: 3.75–3.97 min, 751/0 inner). It contains Fast; do not run Fast
separately. Any failure stops the plan.

## 3. New qualification suite (once)

`scripts\runtime\New-KmcQualificationSuiteSnapshot.ps1 -SuiteId chunk7e-final-<sha8>-<yyyymmdd>` with the
F package and the existing protected-save continuity authority (`new-suite-b205.ps1` pins), `-WhatIf`
dry run first, then the real freeze. Record the suite SHA-256, save digest and Mods digest. A suite pins
the package, DLL, MVID and commit; a new product candidate needs a new SuiteId.

## 4. FULL tier (once, `-SkipBuild`)

1. `scripts\Test-Tiers.ps1 -Tier Full -AssemblyTarget Kingmaker -SkipBuild` in the frozen worktree.
   `scripts/Test.ps1` stops at its first failure. **Expected: FAIL at `Test-Chunk6aSupportingEvidence.ps1`**
   (`Test.ps1` line 114, the first classified check). History: about 32 min. This FULL invocation is
   recorded as **FAIL**, never relabelled PASS.
2. Because the umbrella stops there, complete the inventory **without re-running any child that already
   passed** (owner clarification, 2026-10-24). The FULL prefix outcomes are reused, not repeated: the
   step-1 transcript (its file SHA-256 recorded, with the frozen commit, package and suite it ran on) holds
   one PASS line per `scripts/Test.ps1` step before the stop; those steps count as passed from that
   transcript. Then only the remaining steps run: every `scripts/Test.ps1` step from the stopping check on,
   and every entry of `scripts/Test-Chunk7NativeDiagnostics.ps1` (FULL does not contain it), each in its own
   child process exactly as its umbrella invokes it, continuing past failures (the X6 method), on the same
   frozen worktree and build. Every outcome is reported; no successful child runs twice. About 40–60 min.
3. **Expected failures, exactly and only**, reported in four separate groups (owner clarification): (a) the
   actual FULL failure (step 1); (b) the five historical ledger replays whose cause is unresolved in the
   current lab (`Test-Chunk6aLedgerProtocol`, `…RegressionLedger`, `…LegacyLedger`, `…AreaLedger`,
   `Test-UnrelatedFullLedger`; owner decision 2026-10-24 (c), "Report as classified": they stay FAIL with
   unresolved cause, separate from Chunk 7, with no re-qualification); (c) the superseded Preparing expectation (`Test-Chunk6aSupportingEvidence`:
   CM01 `-refused` → `-admitted`, owner decision `7930313e`) together with its current replacement check;
   (d) every applicable current behaviour check, which must pass. The old Chunk 6 campaign is not restarted.

| Check | Classification | Reproduction |
| --- | --- | --- |
| `Test-Chunk6aSupportingEvidence` | Historical evidence under a changed contract (current reader requires `CM01-combat-mount-preparing-admitted`, owner decision `7930313e`) | Passes at the preview.211 tag; fails since that decision |
| `Test-Chunk6aLedgerProtocol` | Historical contract: the released 6A ledger pins its reader harness | Fails identically at tag `7fa667b2`, `worktrees/qualification211` and `7e49e034` |
| `Test-Chunk6aRegressionLedger` | same | same |
| `Test-Chunk6aLegacyLedger` | same | same |
| `Test-Chunk6aAreaLedger` | same | same |
| `Test-UnrelatedFullLedger` | same | same |

   `Test-ScenarioDispatchEvidence.ps1` is not in either umbrella; run it once and expect PASS 181/0 (G3).
   **Any other failure stops the plan** for diagnosis.

## 5. Native batches

### Common procedure for every launch

- **Before each launch:** no Kingmaker process (a human or any other session included), no
  `runtime-state\active-transaction.lock`, no other executor, the development-scoped WhatIf
  (`-DevelopmentScopedAudit -WhatIf`) passed for this exact plan, and the plan and selection hash-pinned.
  Launch detached through WMI `Win32_Process.Create` (never a background Bash task). One executor only.
- **Stop-on-human-session rule:** if a human game session, a non-plan Kingmaker process, a Steam
  authentication, purchase, update, cloud or entitlement dialog, or any unexpected dialog is present,
  stop before or after the current transaction. Never kill or interrupt it, and never kill an active
  transaction for a time budget. Resume only when the owner's session has ended.
- **steam_api64 single-retry rule:** if the run ends before a game result with exactly the known
  signature (`[Steamworks.NET] Could not load [lib]steam_api.dll` then `DllNotFoundException: steam_api64`
  at `SteamManager.Awake`) and restoration is exact, keep the Unity log read-only and launch **one**
  retry under a fresh run id `<tag>r` with the same selection. The retry is the same declared scenario.
  At most one retry per run and one per batch; any second startup failure in a batch stops it. Every
  other failure is never retried: it is retained and diagnosed (two equivalent failures without a new
  observation require diagnosis before another run).
- **Restoration checks after each run (PASS and FAIL alike):** outer result reports Mods restored, save
  protection passed, Working restored, Baseline unchanged and the write allowlist passed; restored save
  inventory digest equal to the batch's recorded value; `Params.xml` and PlayerPrefs (including
  `KingdomDifficulty`) equal the run's own snapshot; `Assert-KmcPersistenceProfileUnchanged` passes with 0
  admitted changes; no Kingmaker process and no lock remain (the b205 `-check.ps1` helper). A run that is
  not restored is **not passed**; follow profile recovery before transaction recovery.
- **Verdict:** the frozen external `scripts\runtime\Test-RuntimeResult.ps1 -ResultPath … -RequestPath …`
  on the run folder (it dispatches the family reader named per row), plus the row binders in §7. Game
  PASS alone is never a row PASS. **One rule for a pre-input "uncovered" refusal** (rows 57, 67, 69, 70, including the folded prone and defensive-cast halves of 57 and 69):
  it is recorded by hand as uncovered, never PASS, and it is **not** completion. Every one of these cases
  passed on its fixture in b205, so such a refusal on F is a new observation: stop and diagnose before
  any further run of that row. The folded halves added on October 24 (row 57 prone stand-up, row 69
  defensive cast) have **no development run**: a first refusal or failure there is expected to be possible;
  it is retained, diagnosed, and an "uncovered" outcome is taken to the owner for a decision.
- **Selections:** derived per launch by the one unchanged deriver `tools/chunk7-7e/final-derive.ps1`
  from the frozen worktree: `-Tag <tag> -Mode Final -RunMapPath <final-runs.json> -Commit <F.sha>
  -DllSha256 <F.dll> -DllMvid <F.mvid>` prints the compact selection. It starts from the named template
  request's selection and changes only lineage pins (role run ids, evidence artifact and written archive
  SHA-256, minted actor ids, r59 `sourceArea`, r45 assisted readiness facts), each read from the final
  campaign's own settled predecessor in `final-runs.json`. **Settled** means the external
  `Test-RuntimeResult` verdict PASS and every restoration check above passed (the runner's own `status`
  is written before the external reader runs, so it is not enough). Only then the executor appends
  `{"<tag>":{"runId":…,"resultSha256":<SHA-256 of runtime-result.json>,"readerVerdict":"PASS","restorationVerified":true}}`;
  a retry `<tag>r` replaces its tag. The deriver refuses a predecessor that is missing, lacks that receipt,
  whose result file differs from the recorded hash, is not a final-campaign run id, is not PASS, or is not
  on F's commit, DLL and MVID, and any changed field that is not a declared pin.
  Selection tags are those with a "T:" column below; rows 31, 32, 42, 55 and 56 take only the runner
  parameters stated in B14/B15. Rows sourcing a same-batch output are derived after that run settles. Every owner-seed-derived combat or checkpoint row sourced from r01/r02 or k2/k3 must record
  `candidates.sameCandidate=true` (source and checkpoint produced on F), and the progression rows 48–54
  must record `source.sameCandidate=true` (r33–r36). **Explicit fixture-input exceptions** (an earlier
  candidate's output used unchanged as an input, provenance kept, never as behaviour evidence): row 28
  (Manticore producer `20261020-c7b-world71-b54s1-manticore` with accepted cold
  `…world73-b56s1-manticore-cold`, itself derived from the world26 row-1 lineage; its
  `Get-KmcChunk7ManticoreReturnSource` path records no `candidates` field), rows 33, 35 and 66 (Small and
  Medium fixture Manuals), rows 37, 39 and 42 (sixth-Horse Manual), row 31 (stock Mammoth fixture), row 30
  and rows 55–56 (owner seeds and the preview.211 package).
- **Expected pins in every run:** package `<F.pkg>`, manifest `<F.manifest>`, DLL `<F.dll>`, MVID
  `<F.mvid>`, commit `<F.sha>`, suite `chunk7e-final-<sha8>-<date>` `<F.suite>`, `BaseGameOnly`,
  `SaveAccessAllowed` (row 32 alone runs without `BaseGameOnly`), the plan's selection SHA-256. Row 32's source is the same campaign's r31 (G7 (a)); it is not a fixture-input exception.

Run ids: `<yyyymmdd>-c7e-final-B<n>-<tag>`. "T:" names the template request (`runtime-evidence/<run>/runtime-request.json`).

### B1 — acquired chain c1–c5 (~30 min)

| Tag | Row | Scenario / case (PersistenceCase) | Source | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| c1 | 60 | `chunk7-assisted-cold-inventory` / `owner-seed-readonly-inventory` | NO_MODS seed | b199-c1 | `Assert-KmcChunk7AssistedColdInventoryEvidence` |
| c2 | 46 | `chunk7-manual-derived-acquisition` / `existing-ranger-horse` | B1 c1 | b199-c2 | `Assert-KmcChunk7ManualDerivedEvidence` |
| c3 | 47 | `chunk7-acquired-manual-derivative` / `preserve-acquired-field` | B1 c2 | b199-c3 | `Assert-KmcChunk7AcquiredManualEvidence` |
| c4 | 61 | `chunk7-cumulative-acquisition` / `paid-small-ranger-horse` (`owner-seed-one-owner`) | B1 c3 | b199-c4 | `Assert-KmcChunk7CumulativeEvidence` |
| c5 | 62 | `chunk7-acquired-manual-derivative` / `preserve-two-acquired-field` (`post-field-manual`) | B1 c4 | b199-c5 | `Assert-KmcChunk7AcquiredManualEvidence` |

### B2 — owner-seed rows 1–5 (~30 min)

| Tag | Row | Case (PersistenceCase) | Source / checkpoint | T: |
| --- | --- | --- | --- | --- |
| r01 | 1 | `owner-seed-persistence-save` (`multi-exploration-manual`) | NO_MODS seed | `20261018-c7b-world26-ownerseed-b3s1d-save` |
| r02 | 2 | `owner-seed-persistence-load` (`multi-exploration-manual`) | r01 | `…world26-ownerseed-b4s1-load` |
| r03 | 3 | `owner-seed-checkpoint-save` (`multi-exploration-quick`) | r01 / r02 | `…world36-ownerseed-b9s1-quicksave` |
| r04 | 4 | `owner-seed-checkpoint-load` (`multi-exploration-quick`) | r03 | `…b9s2-quickload` |
| r05 | 5 | `owner-seed-checkpoint-save` (`multi-exploration-auto`) | r01 / r02 | `…b9s3-autosave` |

Reader: owned family (`Chunk7OwnedPairsEvidence`) through `Test-RuntimeResult`.

### B3 — rows 6, 11–14 (~30 min)

| Tag | Row | Case (PersistenceCase) | Source | T: |
| --- | --- | --- | --- | --- |
| r06 | 6 | `owner-seed-checkpoint-load` (`multi-exploration-auto`) | r05 | `…b9s4-autoload` |
| r11 | 11 | `owner-seed-checkpoint-save` (`multi-rt-debt-manual`) | r01 / r02 | `…world37-ownerseed-b10s1-rtsave` |
| r12 | 12 | `owner-seed-checkpoint-load` (`multi-rt-debt-manual`) | r11 | `…world38-ownerseed-b10s3-rtload` |
| r13 | 13 | `owner-seed-checkpoint-save` (`multi-tb-debt-manual`) | r01 / r02 | `…world40-ownerseed-b11s1-tbsave` |
| r14 | 14 | `owner-seed-checkpoint-load` (`multi-tb-debt-manual`) | r13 | `…world41-ownerseed-b12s1-tbload` |

Reader: owned family.

### B4 — rows 15–19 (~30 min)

| Tag | Row | Case (PersistenceCase) | Source | T: |
| --- | --- | --- | --- | --- |
| r15 | 15 | `owner-seed-checkpoint-save` (`zero-mounted-tb-retained-manual`) | r01 / r02 | `…world52-ownerseed-b28s1-zerosave` |
| r16 | 16 | `owner-seed-checkpoint-load` (`zero-mounted-tb-retained-manual`) | r15 | `…world52-ownerseed-b30s1-zerocold` |
| r17 | 17 | `owner-seed-checkpoint-save` (`legacy-mounted-pending-tb-manual`) | r01 / r02 | `20261019-c7b-directive4-b41s1-schema3-producer` |
| r18 | 18 | `owner-seed-checkpoint-load` (`legacy-mounted-pending-tb-manual`), schema-2 derivative resave | r17 derivative | `…directive4-b41s2-schema2-resave` |
| r19 | 19 | `owner-seed-checkpoint-load` (`owned-schema2-pending-current-manual`) | r18 | `…directive4-b42s1-schema2-current-cold` (corrected reader) |

Reader: owned family (schema-2 adapter terms for 18–19).

### B5 — rows 20–24 (~30 min)

| Tag | Row | Case (PersistenceCase) | Source | T: |
| --- | --- | --- | --- | --- |
| r20 | 20 | `owner-seed-checkpoint-save` (`owned-schema1-manual`) | r01 / r02 | `…world45-ownerseed-b19s4-schema1save` |
| r21 | 21 | `owner-seed-checkpoint-load` (`owned-schema1-resave-manual`) | r20 | `…b19s5-schema1cold` |
| r22 | 22 | `owner-seed-checkpoint-load` (`owned-schema1-current-manual`) | r21 | `…b20s1-schema1current` |
| r23 | 23 | `owner-seed-load-refusal` (`multi-malformed-refusal-manual`) | r01 derivative / r02 | `…world52-ownerseed-b31s1-malformed` (b31 corrected bound) |
| r24 | 24 | `owner-seed-load-refusal` (`multi-future-refusal-manual`) | r01 derivative / r02 | `…b31s2-future` |

Reader: owned + schema-1 family; 23–24 must refuse without overwrite.

### B6 — rows 25–29 (~30 min)

| Tag | Row | Case (PersistenceCase) | Source | T: |
| --- | --- | --- | --- | --- |
| r25 | 25 | `owner-seed-checkpoint-save` (`multi-owned-worker-cancel-manual`) | r01 / r02 | `…world43-ownerseed-b14s1-worker` |
| r26 | 26 | `owner-seed-checkpoint-save` (`multi-nonselected-mount-unsettled-manual`) | r01 / r02 | `…b14s2-barrier` |
| r27 | 27 | `owner-seed-persistence-load` (`multi-exploration-three-loads-manual`) | r01 | `…world48-ownerseed-b23s2-threeloads` |
| r28 | 28 | `owner-seed-checkpoint-save` (`multi-area-autosave`) | Manticore fixture (producer `…world71-b54s1-manticore`, cold `…world73-b56s1-manticore-cold`; fixture-input exception) | `20261020-c7b-world77-b60s1-manticore-spider-auto` |
| r29 | 29 | `owner-seed-checkpoint-load` (`multi-area-autosave`) | r28 | `…world77-b61s1-manticore-spider-auto-cold` |

Reader: owned + area family.

### B7 — door, X4, capital (~35 min)

| Tag | Row | Case (PersistenceCase, area pins) | Source | T: | Reader / binder |
| --- | --- | --- | --- | --- | --- |
| r30 | 30 | `owner-seed-exploration` on `KMC_C7_DOOR_SEED_OLD_SYCAMORE_OPEN` | door seed (admitted b192b) | `b192-door-seed-exploration-selection.json` (`…b192d1-doorstatic-explore`) | `Assert-KmcChunk7WorldEvidence` |
| x4s | 63 | `persistence-save` (`multi-exploration-manual`) | B1 c5 | b199-x4s | owned |
| x4c | 64 | `persistence-load` (`multi-exploration-three-area-transfers-manual`; enter `dfbc2423…`,`00becd53…`,`dfbc2423…`; areas `314e8c3b…`,`b10e8651…`,`314e8c3b…`) | B1 c5 / x4s | b196-x4c | owned + `Assert-KmcChunk7RepeatedWorldObservation` |
| r58 | 58 | `persistence-load` (`multi-exploration-capital-arrival-manual`; enter `fd225a93…`, target `ead426a6…`) | B1 c5 / x4s | b199-r58 | `Assert-KmcChunk7CapitalArrivalObservation` |
| r59 | 59 | `persistence-load` (`multi-exploration-capital-arrival-cold-manual`; `sourceArea` = r58's recorded `capitalArrival.sourceArea`) | B1 c5 / r58 | b199-r59 | `Assert-KmcChunk7CapitalColdObservation` + `Assert-KmcChunk7CapitalColdSource` |

### B8 — two-pair combat on the owner-seed start (~30 min)

| Tag | Row | Case | Source / checkpoint | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| r07 | 7 | `owner-seed-combat-rider-first` | r01 / r02 | `…world35-ownerseed-b8s2-tbrider` | owned + cleanup |
| r08 | 8 | `owner-seed-combat-mount-first` | r01 / r02 | `…b8s3-tbmount` | owned + cleanup |
| r09 | 9 | `owner-seed-realtime-core` | r01 / r02 | `…b8s1-rt` | `Assert-KmcChunk7RealtimeEvidence` |
| r10 | 10 | `owner-seed-combat-disable` | r01 / r02 | `…world52-ownerseed-b27s1-disable` | owned + cleanup |
| r57 | 57 | `owner-seed-native-life-cleanup` (with the folded prone stand-up from round N+2) | r01 / r02 | `…b197-r57-ownerseed-life` (selection unchanged; the fold is producer behaviour) | `Assert-KmcChunk7NativeLifeEvidence` (incl. `Assert-KmcChunk7ProneStandUp`) + `Assert-KmcChunk7OwnerSeedDerivedCombatStart` |

### B9 — native life on the acquired field and the X1 cases (~30 min)

| Tag | Row | Case | Source / checkpoint | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| p2 | 65 | `native-life-cleanup` | B1 c5 | b197-p2 | `Assert-KmcChunk7NativeLifeEvidence` |
| d1 | 67 | `owner-seed-native-death-cleanup` | r01 / r02 | b205-d1 | death branch (`Assert-KmcChunk7NativeDeathChain`, `Assert-KmcChunk7DeathCleanup`) + `Assert-KmcChunk7OwnerSeedDerivedCombatStart` |
| am | 68 | `owner-seed-attack-modes` | r01 / r02 | b205-am | `Assert-KmcChunk7AttackModesCombat` |
| cr | 69 | `owner-seed-combat-reaction` (contract v2: disengagement in N, the folded defensive scroll cast in N+1) | r01 / r02 | b205-cr (selection unchanged; the fold is producer behaviour) | `Assert-KmcChunk7ReactionCombat` + `Assert-KmcChunk7ReactionDefensiveCast` |
| st | 70 | `owner-seed-staged` | r01 / r02 | b205-st | `Assert-KmcChunk7StagedCombat` |

The pilots used the earlier world26 row-1 lineage as a fixture input; here the start is F's own r01/r02.
A different start can change the recorded geometry or preconditions (Rapid Shot toggle, placement
plans). A resulting pre-input "uncovered" refusal follows the single rule in §5 (recorded, not completion,
stop and diagnose); never fall back to the older lineage silently.

### B10 — RT Charge lineage and Small items (~30 min)

| Tag | Row | Case (PersistenceCase) | Source | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| k1 | 71 | `chunk7-assisted-cold-inventory` / `owner-seed-readonly-inventory` (`KMC_C7_CHARGE_SEED`) | charge seed | b201-k1 | `Assert-KmcChunk7AssistedColdInventoryEvidence` |
| k2 | 72 | `owner-seed-persistence-save` (`multi-exploration-manual`) | charge seed | b201-k2 | owned |
| k3 | 73 | `owner-seed-persistence-load` (`multi-exploration-manual`) | k2 | b201-k3 | owned |
| k4 | 74 | `owner-seed-realtime-charge` | k2 / k3 | b205-k4 | `Assert-KmcChunk7RealtimeEvidence` |
| s1 | 66 | `chunk7-small-profile-calibration` / `compiled-owner-seed-fixture-small-horse-presentation-rt` | `…world71-b53s1-small` (fixture) | b197-s1 | `Assert-KmcChunk7SmallPresentationEvidence` |

k4 must not reuse the b201 k1–k3 lineage (that would make `sameCandidate` false).

### B11 — fixture persistence (~35 min; feeds B12–B13)

| Tag | Row | Case (PersistenceCase) | Source | T: |
| --- | --- | --- | --- | --- |
| r33 | 33 | `small-fixture-persistence-save` (`multi-exploration-manual`) | Small fixture Manual (preview.71 `…world71-b53s1-small`, fixture input) | `…world116-b110s1-small-admission` |
| r34 | 34 | `small-fixture-persistence-load` (`multi-exploration-manual`) | r33 | `…world115-b108s2-small-mounted-cold` |
| r35 | 35 | `medium-fixture-persistence-save` (`multi-exploration-manual`) | Medium fixture Manual (`…world118-b114s1-medium-paid`, fixture input) | `…world124-b120s1-medium-mounted` |
| r36 | 36 | `medium-fixture-persistence-load` (`multi-exploration-manual`) | r35 | `…b120s2-medium-mounted-cold` |
| r37 | 37 | `six-horse-mounted-persistence-save` (`multi-exploration-manual`) | sixth-Horse Manual (b184–185, fixture input) | `…world189-b189s1-sixsave` |

Reader: owned + Small / Medium / six-pair families (`Assert-KmcChunk7SixPairPersistenceObservation`).

### B12 — F10 assisted and leveling (~35 min)

| Tag | Row | Scenario / case | Source | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| r43 | 43 | `chunk7-assisted-fixture-preparation` / `authored-main-ranger4-manual` | assisted fixture | `20261021-c7b-world158-b158s1-assisted-producer` (the b158 lineage the r44/r45 templates source) | assisted |
| r44 | 44 | `chunk7-assisted-cold-inventory` / `assisted-manual-readonly-inventory` | r43 | `…world158-b158s2-assisted-cold` | `Assert-KmcChunk7AssistedColdInventoryEvidence` |
| r45 | 45 | `chunk7-cumulative-acquisition` / `paid-small-ranger-horse` (`assisted-producer-and-cold`) | r43 / r44 | `…world158-b158s3-paid-small` | `Assert-KmcChunk7CumulativeEvidence` |
| r48 | 48 | `chunk7-native-progression` / `small-horse-native-level` | source r33, inventory r34 | `…world115-b108s3-small-native-level` | `Assert-KmcChunk7ProgressionEvidence` |
| r49 | 49 | `chunk7-native-progression` / `small-horse-native-level-cold` | r48, inventory r34 | `…b108s4-small-level-cold` | `Assert-KmcChunk7ProgressionColdEvidence` |

### B13 — F10 dismissal, retraining, replacement (~35 min)

| Tag | Row | Case | Source | T: |
| --- | --- | --- | --- | --- |
| r50 | 50 | `small-horse-native-dismiss` | source r33, inventory r34 | `…world102-b89s3-small-native-dismiss` |
| r51 | 51 | `small-horse-native-retrain-horse` | source r33, inventory r34 | `…world112-b100s3-small-native-retrain` |
| r52 | 52 | `small-horse-native-retrain-cold` | r51, inventory r34 | `…b100s4-small-retrain-cold` |
| r53 | 53 | `medium-horse-native-retrain-mammoth` | source r35, inventory r36 | `…world124-b120s3-medium-mammoth-retrain` |
| r54 | 54 | `medium-horse-native-retrain-cold` | r53, inventory r36 | `…world122-b118s4-medium-mammoth-cold` |

Scenario `chunk7-native-progression`; reader `Assert-KmcChunk7ProgressionEvidence` / `…ColdEvidence`.
The progression readers require `source.sameCandidate=true` (`Chunk7ProgressionFixtures.ps1`,
`Chunk7ProgressionEvidence.ps1`), so B12–B13 can only follow B11 on F. Per G6, the one r33/r34 pair feeds
rows 48, 50 and 51 (and is the inventory of 49 and 52); no extra pairs or batches are needed.

### B14 — X5 upgrade and rollback, removal (~30 min)

| Tag | Row | Run | Source | Reader / binder |
| --- | --- | --- | --- | --- |
| u1 | 55 | `chunk7-normal-player-control -UpgradeRehearsalStep u1-previous-install -UpgradeRehearsalId x5-final-<sha8>` (preview.211 `-b` staged, observer.6, `TimeoutSeconds` 900) | NO_MODS seed | `Test-RuntimeResult` (`Test-KmcNormalPlayerResult`) |
| u2 | 55 | same id, `u2-upgrade -UpgradeCarrySourceRunId <u1>` (F) | u1; sixth-Horse Manual 308 (`…world185-b185s1-sixthhorse-cold` lineage) | same |
| u3 | 56 | same id, `u3-rollback -UpgradeCarrySourceRunId <u1>` | u1 | same; then §7 binder |
| r31 | 31 | `persistence-p07-save` (`prepare-removal`), `-OwnerStockRemoval -BaseGameOnly`, no `-PersistenceSourceRunId`, `-ExpectedPersistenceSourceSha256` or `-ObserverPackagePath` | stock Mammoth fixture (b64) | `Assert-KmcCurrentRemovalEvidence` |
| r32 | 32 | `persistence-p07-load` (`removal-no-dll`), `-OwnerStockRemoval -ObserverPackagePath <observer.6>`, no `-BaseGameOnly`, `-PersistenceSourceRunId <B14 r31 run>`, `-ExpectedPersistenceSourceSha256 <r31's removal-prepared cleanup SHA-256>`, `-OwnerStockCleanupRunMapPath <final-runs.json>` (G7 (a)); launched only after r31 settled and its receipt was appended | B14 r31 (same candidate) | `Assert-KmcCurrentRemovalEvidence` (b67 reader) |

T: `b200-x5-run.ps1` (u1–u3), `20261020-c7b-world81-b65s1-owner-stock-removal`,
`…world82-b67s1-owner-stock-nodll-reader`. u1 records stay at the lab root for u2/u3 and the binder.

### B15 — six-pair and F11 measurements, fresh install (~80 min)

| Tag | Row | Scenario / case | Source | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| r38 | 38 | `six-horse-mounted-persistence-load` (`multi-exploration-manual`) | r37 | `…world189-b189s2-sixcold` | six-pair persistence |
| r39 | 39 | `six-horse-cold-fixture-lifecycle-stress` (`TimeoutSeconds` 1200) | sixth-Horse Manual | `…world190-b190s1-sixmeasure` (`b188-sixslot-selection.json`) | `Assert-KmcChunk7SixPairStressEvidence` + §6 |
| r40 | 40 | `owner-seed-exploration`, matched-pool windows 0/1/2/0 (profiler absent) | NO_MODS seed | `20261021-c7d-world156-b157s1-observer-off` | `Assert-KmcChunk7WorldEvidence` + `Assert-KmcMatchedPoolEvidence` + §6 |
| r41 | 41 | `owner-seed-exploration` + `-F11NativeAllocationProbe` | NO_MODS seed | `…world156-b156s1-f11-native` (outer FAIL run; used only as the selection template, never as a predecessor) | allocation family + §6 |
| r42 | 42 | `chunk7-normal-player-control` (no rehearsal step; observer.6) | sixth-Horse Manual 308 `b1e817a6…` | `…world191-b191s2-normalplayer` | `Test-KmcNormalPlayerResult` + §7 row-42 binder |

Six-pair stress takes 10–40 min of game time. Row 42 is last: it is the fresh-install control.

### B16 — rows added on October 24 (X3 closure; folded combat measurements run inside B8/B9 rows)

| Tag | Row | Case (PersistenceCase) | Source / checkpoint | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| x3 | 75 | `owner-seed-checkpoint-load` (`pending-schema3-control-manual`) | r01 / r17 (B4, same candidate) | `20261019-c7b-directive4-b37s1-schema3-corrected` (outer-FAIL development run; selection shape only, never a predecessor) | owned control branch + combat-persistence reader |
| l6 | 76 | `chunk7-assisted-cold-inventory` / `owner-seed-readonly-inventory` on `KMC_C7_LEVEL6_SEED` | level-6 seed master (registered owner seed, no lineage pins) | none: the selection is the literal `{"case":"owner-seed-readonly-inventory","baseGameOnly":true,"expectedNativeLoadCount":1,"expectedNativeWriteCount":0,"seedId":"KMC_C7_LEVEL6_SEED","seedSha256":"b1447ae77246d418256ec4aae9be1ee97828880a30376ae1985ca880a80e37d5"}` (the b201-k1 shape with the seed fields) | `Assert-KmcChunk7AssistedColdInventoryEvidence` |

x3 may run in any batch after B4 settles and l6 at any time; both are placed here so no earlier batch exceeds five launches.
Row 75 development evidence (b206): x3 PASSed with a cross-candidate source (`20261019-c7b-directive4-b41s1-schema3-producer`).
That is development evidence only; the deriver cannot re-pin x3 to a cross-candidate source, and the final run uses the
same-candidate row 17.

### B17 — KMC_C7_MOUNTED6_SEED lineage (owner decision of October 24, "I'll make a mod seed"; ~35 min)

| Tag | Row | Case (PersistenceCase) | Source / checkpoint | T: | Reader |
| --- | --- | --- | --- | --- | --- |
| m1 | 77 | `chunk7-assisted-cold-inventory` / `owner-seed-readonly-inventory` on `KMC_C7_MOUNTED6_SEED` | mounted6 seed master | none: literal `{"case":"owner-seed-readonly-inventory","baseGameOnly":true,"expectedNativeLoadCount":1,"expectedNativeWriteCount":0,"seedId":"KMC_C7_MOUNTED6_SEED","seedSha256":"2ef426c408e7271c850baab17c4f61a59e2fa34445d297b4b104b88917938f7c"}` | `Assert-KmcChunk7AssistedColdInventoryEvidence` |
| m2 | 78 | `owner-seed-persistence-save` (`multi-exploration-manual`) | mounted6 seed (after m1) | none: literal `ownedPairs` `{"case":"owner-seed-persistence-save","seedId":"KMC_C7_MOUNTED6_SEED","seedSha256":"2ef426c4…","riderAId":"73681ef2-b281-4bc6-bc05-2de68c1616cb","mountAId":"6eb039a1-d500-405e-987c-5a823ee41626","riderBId":"fd213210-71cd-41b0-b35b-68e1b18e9b43","partyIds":[the six field ids of the census: 73681ef2, 6eb039a1, 8265f8cf, 5dd7bd1d, fd213210, 4718d3c6],"xpAssistance":{"unitId":"fd213210-…","targetCharacterLevel":8},"baseGameOnly":true,"expectedNativeLoadCount":1,"expectedNativeWriteCount":1}` | owned (pre-owned branch) |
| m3 | 79 | `owner-seed-persistence-load` (`multi-exploration-manual`) | m2 | the development pilot's m3 (deriver tag added after it settles) | owned |
| m4 | 80 | `owner-seed-prepared-casting` | m2 / m3 | the development pilot's m4 | `Assert-KmcChunk7RealtimeEvidence` |
| m5 | 81 | `owner-seed-attack-modes` | m2 / m3 | the development pilot's m5 | `Assert-KmcChunk7AttackModesCombat` |

No development run of this lineage exists yet. m3–m5 need template runs before the deriver can re-pin them; adding
those tags changes the deriver, so it gets its own review. The seed is admitted with its empty schema-3 KMC archive
only on this lineage, and m4 requires it. The m2 setup refuses before any mutation if rider A's pinned Horse, its
Horse feature and rank, or rider B's exact minimal assistance (CL 4, ready level 7, target 8, 15 978 XP) differ;
m4 refuses as uncovered unless rider A alone holds exactly one memorized prepared Cure Light Wounds slot; m5 refuses
as uncovered below BAB 6 or with other saved hand sets. A first refusal on this lineage goes to the owner.
## 6. F11 acceptance (rows 39–42; criteria fixed, never moved)

Computed from the recorded windows of r39 (0/1/2/6/0 pairs, three windows each) and r40 (0/1/2/0), with
r41 for allocation and r42 for quiet logging:

| Criterion | Computation | Pass rule |
| --- | --- | --- |
| Population stability | per state, compare actor, owner, subscription and residue counts across its windows; each settled cycle returns to the zero-pair counts | all equal (required) |
| Quiet logging | KMC warnings/errors in quiet windows; KMC error/exception lines in r42's UMM receipt | both 0 (required) |
| Absolute budget | median of window-mean KMC Update time per state, instrumentation included | 2 pairs ≤ 0.5 ms (r40 and r39); 6 pairs ≤ 1.0 ms (r39) (required) |
| Scaling | (median at N − 0-pair median) / N for N = 1, 2, 6, against the 0-pair window-to-window spread | increment growth beyond the noise band is investigated; demonstrated super-linear growth is a defect |
| Allocation | r41 bytes per Update and per sample second across repeated windows of one state | no monotonic increase (investigate one) |
| Whole frame | P95 | context only |

Each window also reports mean, P50, P95, maximum and collections inside it. Instrumentation limits from
the protocol are reported with the verdict. A failing required criterion is a defect to fix and requalify.

## 7. Final consumer bindings and ledger

After B15, with every run restored and read:
1. Re-run `Test-RuntimeResult` on all 82 retained run folders from the frozen worktree; record result
   SHA-256 and reader verdict per run (old/new verdicts kept if any reader-only correction occurred).
2. Row 42: `Assert-KmcChunk7NormalPlayerRow42Binding <r42 runId> <resultSha256>`; requires PASS and
   `requestBinding` bound.
3. Rows 55–56: `Assert-KmcChunk7UpgradeRehearsalComplete x5-final-<sha8> @(@('u1-previous-install',<u1>,<sha>),@('u2-upgrade',<u2>,<sha>),@('u3-rollback',<u3>,<sha>))`;
   it requires one id, one candidate F, run order, request binding and u2/u3 bound to the same u1.
4. Rows 58–59: the capital observation and cold-source asserts above (inside the reader) plus the
   recorded `sourceArea` lineage.
5. Row 32 lineage (G7 (a)): r32's request `persistenceLoad.sha256` equals r31's `removal-prepared` cleanup SHA-256 read
   from r31's `persistence-observations.jsonl` (hash-bound by its manifest), r31's `final-runs.json` receipt names the
   r31 run with `readerVerdict` PASS and its result SHA-256, and r31 and r32 record the same commit, DLL, MVID and suite
   (the runner's source gate checked this at launch; the consumer's own records carry only the descriptor hash, so the
   ledger re-derives the link). Then the general lineage check: every row named in §5 records its same-candidate flag; every row's source run id is a
   run of this campaign or one of the named fixture-input exceptions.
6. Ledger: one `final-ledger.json` in `analysis-cache/chunk7-continuation` listing per row: row, run id(s),
   result SHA-256, reader and binder verdicts, uncovered halves, restoration digest. Completion means all
   76 rows PASS; any NOT RUN, BLOCKED, FAIL or newly uncovered row means no completion claim (the
   uncovered halves already in the matrix exclusions are recorded inside passing rows).

## 8. Failure handling

- **Product defect on F:** retain, diagnose, fix, one independent review, freeze successor F′ (new
  package, new SuiteId) and re-run **every** row on F′; no PASS transfers to changed bytes.
- **Reader-only defect:** re-evaluate the immutable evidence when every needed observation exists; keep
  old and new verdicts. The corrected reader changes the harness commit, so later batches need a new
  package and suite binding the same DLL (b205 precedent); runs already made keep their package.
- **Observer or fixture change:** re-run the affected native rows.
- **Unrestored run:** stop; profile recovery before transaction recovery; never a PASS.

## 9. Closeout

`docs/CHUNK7-ENGINEERING-CLOSEOUT-<date>.md` (its audit claims are dependency-scoped per the owner directive, never whole-lab purity): F identities (commit, package, manifest, DLL, MVID,
observer, suite), offline tier receipts and durations, the classified FULL list, per-batch run tables
with restoration, each row's verdict and reader, the F11 table, uncovered halves, retries, and the
owner HUMAN PLAY items still pending. Fill the `[TBD after 7E freeze]` identities in the changelog,
release notes and owner checklist from the frozen worktree (doc-only; no rebuild). Then one commit and
a guarded push.

## Estimate (from b190–b205 history)

| Phase | Basis | Minutes |
| --- | --- | --- |
| Freeze, package, payload, observer | b205 package 0.5 min plus inspection | 15 |
| Candidate | 3.75–3.97 min | 5 |
| Suite dry run and freeze | b205 | 5 |
| FULL `-SkipBuild` | about 32 min (earlier FULL history) | 35 |
| Per-step inventory of both umbrellas | Chunk 7 umbrella about 18 min plus Test.ps1 steps | 70 |
| B1–B6, B8–B10, B14 (10 batches) | b205: 5 launches in 16 min plus WhatIfs and checks; B8/B9 carry the longer r57 and cr | 310 |
| B16 (x3, l6) | two short launches plus WhatIfs and checks | 15 |
| B17 (m1–m5) | cold inventory, the Manual producer with one rider build (about the b201 k2 time), its cold consumer and two combat cases | 35 |
| B7, B11–B13 | x4c 3.6 min, progression and fixture runs | 140 |
| B15 | six-pair stress 10–40 min plus F11 | 80 |
| Bindings, ledger, closeout | | 45 |
| **Total** | | **about 755 (≈ 12.6 h)** |
