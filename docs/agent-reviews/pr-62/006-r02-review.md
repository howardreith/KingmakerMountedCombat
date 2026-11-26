<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r02-review part=1/1 sha256=df0d93893247416e77d3791c973abe87aeb4f71f73fa301ec1bf2c5a719b859f -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `50c25f257e`

**Decision:** `changes_required` — Both shipping DLLs have credible 0.31.1 build, package and static API evidence. The focused gate passed at the requested commit. Runtime profile selection, observer package admission and binary reuse still have paths that can accept the wrong identity or refuse the intended one.

Continuation for Claude

Carry one reviewed loader profile from the guarded launcher through dependency admission, result reading and receipts. Share the validated pin definition across MSBuild and both package validators. Check the final compiler inputs after supported project hooks, compare measured package identities on every admission path, and bind reuse to the actual outputs and their build inputs. Make the tier plan describe that reuse check.

Use the existing combined packaging route for the focused regressions. Retain the earlier full loader inventory, correct the test counts and report the actual prerequisite for each unrun native row. Keep this work within PR #62, the two shipping products and their affected tooling. Preserve the Mounted Shield branches, historical evidence, installation and save guards. The last gate closed with 91.42 of 90 development minutes spent. Further validation needs admission under an authorized allowance. The findings specify the focused observations needed for review.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Run the compiler-input check after the last project reference hook
- Location: `LoaderReferences.targets:14`; acceptance: mission §5, mission §6
- Trigger: A project adjusts ReferencePathWithRefAssemblies in a conventional pre-compilation target imported after the KMC target.
- Expected: The verified UMM and Harmony12 paths and hashes are the compiler inputs after the supported reference-selection hooks have run.
- Actual: Both projects import LoaderReferences.targets before Microsoft.CSharp.targets. A Directory.Build.targets hook registered later for BeforeTargets="CoreCompile" can change the list after the KMC assertion. The 9/9 test rewrites it at AfterTargets="FindReferenceAssembliesForReferences", which the assertion does catch.
- Why it blocks / does not: A supported MSBuild extension point still permits the list consumed by Csc to differ from the list that passed verification.
- Requested correction: Order the final assertion after the standard and repository-level compiler-reference hooks in both projects. Keep early cache verification.
- Requested evidence: Extend the existing direct-build fixture with a Directory.Build.targets adjustment at BeforeTargets="CoreCompile". Show refusal before emission for both projects alongside the passing controls.

**R1-F2** · P1 · defect · blocks readiness — Carry the selected runtime profile through the guarded launcher
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:403`; acceptance: mission §7
- Trigger: A disposable environment selects the baseline, or a loader inventory contains the staged DLL without a cache or shared-state assembly.
- Expected: One reviewed selection controls preflight, dependency admission, result reading, cache and shared-state checks, and the resulting receipt.
- Actual: The launcher still opens planning/ENVIRONMENT-FINGERPRINT.json at line 403 and passes no profile to the readers at lines 1351 and 1367. BaseGameIsolation pins three roles to that fingerprint and records no selected profile. The inventory checks the profile's UMM identity only on the cache branch and its Harmony2 identity only when HarmonySharedState appears. The 0.32.4 and 0.33.4 target IDs still lack selectable records.
- Why it blocks / does not: A matching alternate environment cannot use the normal launch path. A direct-load inventory without shared state can also pass without establishing the selected Harmony2 identity.
- Requested correction: Resolve the profile at the guarded entry point, retain the owner installation as default, and propagate its identity through admission and evidence. Verify the relevant loader stack independently of whether the optional cache and shared-state observations appear. Identify the reviewed comparison profiles and their native eligibility explicitly.
- Requested evidence: Show offline launcher-to-reader admission for the owner and baseline profiles, mixed UMM/Harmony refusal with and without a cache or shared state, and a receipt identifying the selected profile. Keep unexecuted native rows marked NOT RUN.

**R1-F4** · P1 · defect · blocks readiness — Bind the normal-player observer and receipt version to the measured ZIP
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:515`; acceptance: mission §5, mission §6
- Trigger: A valid ZIP is accompanied by a receipt with stale observer DLL fields or a stale product version.
- Expected: Every admission binds the measured DLL hash, MVID, product version and entries to the receipt before those fields reach a request or suite.
- Actual: The normal-player path discards Validate-ObserverPackage's result and calls Assert-KmcNormalPlayerObserverManifest, which checks the outer ZIP and returns the receipt's DLL fields. The common admission path now checks DLL hash, MVID and entries, but omits version. New-KmcQualificationSuiteSnapshot still copies manifest.version into the suite.
- Why it blocks / does not: A stale receipt can misidentify the observer before staging or put the wrong product version into a new qualification record.
- Requested correction: Reuse one measured validator result across the common and normal-player admissions. Return and compare the measured version for both products while retaining the observer's distinct source-ownership and frozen-identity checks.
- Requested evidence: Exercise normal-player observer admission with a correct outer hash and stale DLL fields, and common admission with only the receipt version changed. Show both refusals before staging or snapshot creation and acceptance of matching receipts.

**R1-F6** · P2 · defect · blocks readiness — Validate the pin definition at standalone package admission
- Location: `scripts/UmmLoaderContracts.ps1:38`; acceptance: mission §5, mission §6
- Trigger: A standalone validator is invoked after a required pin is omitted, renamed or made inconsistent with the minimum.
- Expected: Build and standalone validation require both canonical pins and bind the declared minimum to the UMM reference version.
- Actual: Get-PinnedLoaderReferences now validates the required mapping. Both standalone validators still read the raw JSON themselves. Assert-KmcPinnedLoaderPackageReferences iterates whichever records are present. Omitting the UMM record removes its emitted-version check, and the remaining dependency allowlist checks names only.
- Why it blocks / does not: A malformed pin definition can allow the released higher-UMM DLL through standalone validation with a lowered manifest.
- Requested correction: Share a side-effect-free pin-definition parser across acquisition, compiler checking and both package validators. Standalone ZIP inspection should work without downloading references or requiring a populated cache.
- Requested evidence: Run both standalone validators with missing, duplicated or mismapped pins and with a minimum/UMM-version disagreement. They should reject the definition and continue to accept genuine packages with the canonical mapping.

**R1-F8** · P2 · defect · blocks readiness — Bind reused build outputs to their bytes and source
- Location: `scripts/LoaderInputStamp.ps1:7`; acceptance: mission §5, mission §6
- Trigger: An output is replaced or a compiled source changes after the loader-input stamp was written.
- Expected: A reuse receipt identifies the material build inputs and the exact outputs being credited.
- Actual: The stamp hashes selected project files and cached references. It contains no product DLL, observer DLL or component-runner hash, and omits compiled sources and the build bootstrap. Replacing bin/Release/KingmakerMountedCombat.dll after a good build leaves the stamp unchanged. The Full -SkipBuild branch also delegates to Test.ps1 before reaching the guard.
- Why it blocks / does not: A supported reuse path can credit a different binary or stale compiled source to the verified build.
- Requested correction: Bind the receipt to the relevant source and build inputs and the hashes of the outputs produced. Require it before supported candidate-output reuse, or refuse reuse where that relationship cannot be established.
- Requested evidence: Keep the existing pin/helper/project cases. Also show that replacing or removing a shipping DLL, replacing the component runner, or changing a compiled source rejects reuse. Exercise the Full admission boundary without running the Full campaign.

**R1-F11** · P2 · required_gap · blocks readiness — Make the current handoff agree with the recorded results
- Location: `CURRENT.md:34`; acceptance: mission §6, mission §7, mission §8
- Trigger: A maintainer follows the published build, support or resume instructions.
- Expected: Current documents and the checkpoint report agree on observed counts, product and evidence identities, static results, failures and native prerequisites.
- Actual: CURRENT.md reports 153 component checks. The controller log records 1,488 components and 153 source checks. README.md, AUTONOMOUS-RESUME.md and docs/CURRENT-SUPPORT.md still call the build and API checks pending. The compatibility matrix lists completed work as remaining. The installed native row says only that no run occurred, and the developer report marks a group containing the 280-pass/3-fail harness run as passed.
- Why it blocks / does not: The current entry points give conflicting instructions about completed work, failed checks and the reason the installed native row remains open.
- Requested correction: Reconcile the current entry points and matrix. Identify the actual fixture, guarded environment, admission or budget prerequisite for each unrun case. Report the harness failure separately with its baseline comparison and distinguish dated history from current instructions.
- Requested evidence: Provide consistent local records and a matching draft-PR handoff. For any available and authorized native case, supply the guarded outcome. For an unavailable case, name the missing prerequisite.

**R2-F1** · P2 · defect · blocks readiness — Read compiler digests from parsed JSON
- Location: `LoaderReferences.targets:20`; acceptance: mission §5
- Trigger: A formatter changes whitespace or property order in loader-reference-pins.json.
- Expected: All consumers interpret the same validated JSON records regardless of insignificant whitespace or property order.
- Actual: The new MSBuild expressions search raw JSON for an exact spelling of "File": followed later by "Sha256":. A space before the colon leaves the parsed JSON identical but makes both digest matches empty. Placing Sha256 before File has the same effect.
- Why it blocks / does not: A semantically unchanged pin file can pass acquisition and stop every production compile.
- Requested correction: Obtain the final hash expectations from the parsed pin definition while retaining the check on the actual compiler files.
- Requested evidence: Show that a valid reformatted or reordered copy of the pin definition produces the same accepted direct build, and that altered cache bytes still fail.

**R2-F2** · P2 · defect · blocks readiness — Include the reuse check in the tier plan
- Location: `scripts/Test-Tiers.ps1:115`; acceptance: mission §6, docs/VALIDATION-POLICY.md
- Trigger: A developer plans a Fast or Candidate invocation with -SkipBuild.
- Expected: The plan, initial admission estimate and executed steps describe the same reuse check.
- Actual: Fast and Candidate execute a new 'reuse check' at line 257. Their plan omits that step and its prerequisite, so -Plan can label a DLL reused when its stamp is absent. The selection and initial budget estimate also omit the step.
- Why it blocks / does not: The documented planning entry point gives an incorrect account of the changed validation path.
- Requested correction: Include reuse admission in the resolved step list and report its prerequisite accurately in plan mode.
- Requested evidence: Compare Fast or Candidate -Plan -SkipBuild for matching and missing receipts. The plan should describe the same first check as execution and remain read-only.

**R2-F3** · P2 · evidence_gap · blocks readiness — Retain the full measured loader inventory and its provenance
- Location: `docs/evidence/umm-0311/INDEX.md:3`; acceptance: mission §6, mission §7, mission §8
- Trigger: A maintainer uses the round-two evidence index to reconstruct the runtime rows.
- Expected: The handoff identifies the complete measured loader stack and binds each retained result to its original producer.
- Actual: Round two removed loader-profile-inventory.json from the evidence directory and its index. The record at 215d68fe contains the newer rows' measured MVIDs and the full identity of the owner's additional 0Harmony-1.2.dll. The current table gives only a hash prefix for that file. The index also attributes the collection to f5a9f386 although the direct-build record identifies 796935cc.
- Why it blocks / does not: A reader following the current evidence index loses part of the measured Harmony stack and can attribute a run to the wrong source.
- Requested correction: Restore the original inventory with its provenance or link its immutable Git blob and digest. Identify the producer revision for records reused from an earlier commit.
- Requested evidence: An indexed record should recover the exact hashes and MVIDs, including the owner's extra Harmony assembly, and distinguish the 796935cc build test from the f5a9f386 package records. The existing Git record supplies these observations.

**R2-F4** · P2 · required_gap · blocks readiness — Build the two baseline package fixtures together
- Location: `scripts/Test-PackageAdmissionAndReuse.ps1:87`; acceptance: mission §6, docs/VALIDATION-POLICY.md §4
- Trigger: The package-boundary regression creates its main and observer baseline fixtures.
- Expected: Both baseline package fixtures come from one clean qualification build. A deliberately different DLL is built separately for the stale-receipt case.
- Actual: The new focused test invokes Package.ps1 and Package-Observer.ps1 consecutively on the unchanged clone. Both invoke Build-Local.ps1, which rebuilds the whole solution. The existing -IncludeRemovalObserver route produces both manifests and runs both validators from one build.
- Why it blocks / does not: The focused gate repeats an unchanged production build against the repository's explicit build-once policy and consumes the shared validation allowance.
- Requested correction: Use the existing combined packaging route for the unchanged fixtures and retain independent standalone validation of each ZIP.
- Requested evidence: Show both positive packages and the existing negative archive cases from the combined build in the next admitted focused run.

**R2-F5** · P3 · optional_polish · non-blocking — Close the main ZIP after reading its entry name
- Location: `scripts/Test-PackageAdmissionAndReuse.ps1:116`; acceptance: mission §6
- Trigger: The Windows test cleans up before the unreferenced archive handle is finalized.
- Expected: The focused test releases its archive handles before cleaning its temporary files.
- Actual: The expression that reads the main DLL entry name opens a ZipArchive without disposing it. On Windows its file handle can survive until finalization, while final cleanup suppresses deletion errors.
- Why it blocks / does not: The possible residue is confined to the test's temporary directory and does not affect the package verdict or the owner's installation.
- Requested correction: Dispose the archive when looking up the DLL entry, as the neighboring entry helpers already do.
- Requested evidence: On the next Windows run, confirm the temporary package directory is removed.

#### Prior finding assessments

- **R1-F1** — `fix_rejected` — The early one-shot hook is gone and the recorded post-selection rewrites are caught for both DLLs. The assertion is still registered before a later-imported hook at the same CoreCompile boundary.
- **R1-F10** — `fix_verified` — The historical default is the immutable fb743911200f150af407198c6c28519e522ef58c. The script checks commit existence and both checkouts, and the retained record identifies the same historical build.
- **R1-F11** — `fix_rejected` — CURRENT.md now identifies both packaged artifacts and the static audit command. The gate contradicts its component count, three current entry points still report completed checks as pending, and the owner native row lacks a concrete prerequisite.
- **R1-F12** — `fix_verified` — git diff --check across the full merge-base-to-candidate diff exited 0.
- **R1-F2** — `fix_rejected` — The two leaf readers accept a profile parameter. The guarded launcher, dependency admission and receipts still use the fixed environment, and exact profile checks in the inventory depend on optional observations.
- **R1-F3** — `fix_verified` — Observer admission now compares the entire tools/KmcRemovalObserver subtree alongside the shared build-input check. The main-only path excludes that subtree. Read-only Git comparisons confirmed the distinction for an observer change and a documentation-only descendant.
- **R1-F4** — `fix_rejected` — The H-bound gate confirms the common path rejects a swapped main DLL. Normal-player observer admission still discards the measurement, and the common comparison still omits the product version.
- **R1-F5** — `fix_verified` — The observer has its own clean-source manifest, two-entry ZIP, standalone validation and four-profile audit. All records agree on DLL f9697f91feeab95b6a095dabf1a901cbd3d0aae3863efed6ea53fba8fe271958 and MVID a5e1bb2f-21fa-4bdd-9bd8-cd0861dc3a96. The audit reports 8 types and 20 members resolved on each profile.
- **R1-F6** — `fix_rejected` — The H gate confirms the acquisition helper refuses omitted and renamed Harmony12 pins. Neither standalone validator applies that definition check before trusting the reference list.
- **R1-F7** — `fix_verified` — Both validators explicitly require a scalar string. The H gate accepted both genuine packages and rejected a one-element ManagerVersion array in each.
- **R1-F8** — `fix_rejected` — The new stamp catches the tested pin, verifier and project changes. It cannot detect replacement of an output or a change to compiled source, and the Full reuse branch does not consult it.
- **R1-F9** — `fix_verified` — The controller log shows real ZIPs accepted by both standalone validators, dependency-injected archives rejected by each, and the released 0.33.0-referencing main DLL rejected with the 0.31.1 declaration. The emitted-version negative was exercised on the main product.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository instructions, validation policy, current handoffs, developer report, prior ledger, full feature diff and round-two commit history.; Both production projects and manifests, MSBuild targets, acquisition and pin helpers, packaging scripts, standalone validators, frozen-source logic and tier reuse paths.; The guarded launcher, base-game dependency admission, inventory and game-result readers, normal-player observer admission, suite snapshot writer, profile records and compatibility evidence.
- **Executed verification:** The controller receipt and matching log SHA-256 confirm Fast with Test-PackageAdmissionAndReuse.ps1 at 50c25f25. It exited 0 in 178.0 seconds with 1,488 component checks, 153 source checks and 24 focused cases passing.; The gate built both shipping projects, accepted both genuine ZIPs, rejected extra UMM, Harmony and game payloads in both, rejected the released higher-UMM main DLL, and rejected a different main DLL behind a receipt with the correct outer ZIP hash.; The retained direct-MSBuild record at 796935cc reports 9/9, including the historical installed-loader control, corrupt cache, stray HintPath and post-reference-selection rewrites for both projects. The affected projects, target and test script are unchanged at H.; I matched all 13 indexed evidence blobs to their exported SHA-256 values. For each product, the f5a9f386 manifest and validator agree on ZIP hash, DLL hash, MVID and entries. The four-profile audit uses that same DLL hash and MVID. Git shows only documentation and evidence changes from f5a9f386 to H.; Read-only Git comparisons confirmed observer-only invalidation selects tools/KmcRemovalObserver/Info.json at 3e10838e, leaves the main input set unchanged, and selects no observer changes from f5a9f386 to H. The full feature diff passes git diff --check.; I applied the two MSBuild digest patterns to the checked-in pin text and a JSON-equivalent whitespace variant in memory. Both patterns matched the original and missed the variant.
- **Source inspection only:** The later CoreCompile hook ordering, normal-player observer receipt path, unchecked manifest version, incomplete standalone pin mapping and omitted reuse inputs follow from the candidate source. I did not run builds, packaging or native workflows in the review snapshot.; The runtime profile fixtures exercise individual readers. Their source and reported totals do not establish that the launcher forwards a selected profile.
- **Required gaps:** End-to-end loader-profile selection and mixed-stack refusal through the actual guarded entry point.; Final compiler-input ordering, validated pin definitions at both standalone validators, and complete measured receipt comparison for normal-player observer admission.; Reuse admission tied to material inputs and output bytes, with matching plan and execution behavior.; A consistent current handoff, an indexed full loader inventory with its original provenance, and qualification fixtures produced from one unchanged build.
- **Limitations:** The controller withheld the mixed foreground transcript. The H-bound Fast gate has a controller log. Other runs are attributed to the source revisions in their retained records.; The reported 28/28 frozen-source and 107/107 inventory totals have no separate execution log in the supplied evidence. The retained harness summary records 280 passes and three failures also listed at a61c1a5c.; All four loader rows and the combined-stack check are native NOT RUN. The static audits establish direct member resolution only. The snapshot does not establish current disposable-fixture availability or the live PR description.

#### Summary

- **Delivered behavior:** Mounted Combat and the removal observer declare UMM 0.31.1. Their retained Windows package records and static audits identify UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1. Direct API references resolved on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4. Native behavior remains unverified on every row.
- **Scope reviewed:** The final Mounted Combat checkpoint for PR #62, including its separately shipped observer, the #63 baseline reconciliation, affected build and runtime tooling, and all twelve prior findings.
- **Pending later-phase work:** none
- **Unresolved questions:** Which approved disposable environment and fixtures, if any, are currently available for the installed 0.33.0 load/settings, mounted combat and observer checks?
- **Remaining owner review:** Any additional validation allowance required after the recorded development budget was exhausted.; Human-play acceptance and the eventual disposition of draft PR #62.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20bcd-9222-7da0-b2d6-ced7d9354cef`, duration 822.9s, response sha256 `0a84c0748e46a607c64594081bca88c71913f8fef1df14ffe37f685cfe538128`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r02-67d4b6`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "50c25f257e2354140677de1c23f6b8b4a79a24c4",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both shipping DLLs have credible 0.31.1 build, package and static API evidence. The focused gate passed at the requested commit. Runtime profile selection, observer package admission and binary reuse still have paths that can accept the wrong identity or refuse the intended one.\n\nContinuation for Claude\n\nCarry one reviewed loader profile from the guarded launcher through dependency admission, result reading and receipts. Share the validated pin definition across MSBuild and both package validators. Check the final compiler inputs after supported project hooks, compare measured package identities on every admission path, and bind reuse to the actual outputs and their build inputs. Make the tier plan describe that reuse check.\n\nUse the existing combined packaging route for the focused regressions. Retain the earlier full loader inventory, correct the test counts and report the actual prerequisite for each unrun native row. Keep this work within PR #62, the two shipping products and their affected tooling. Preserve the Mounted Shield branches, historical evidence, installation and save guards. The last gate closed with 91.42 of 90 development minutes spent. Further validation needs admission under an authorized allowance. The findings specify the focused observations needed for review.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and matching log SHA-256 confirm Fast with Test-PackageAdmissionAndReuse.ps1 at 50c25f25. It exited 0 in 178.0 seconds with 1,488 component checks, 153 source checks and 24 focused cases passing.",
      "The gate built both shipping projects, accepted both genuine ZIPs, rejected extra UMM, Harmony and game payloads in both, rejected the released higher-UMM main DLL, and rejected a different main DLL behind a receipt with the correct outer ZIP hash.",
      "The retained direct-MSBuild record at 796935cc reports 9/9, including the historical installed-loader control, corrupt cache, stray HintPath and post-reference-selection rewrites for both projects. The affected projects, target and test script are unchanged at H.",
      "I matched all 13 indexed evidence blobs to their exported SHA-256 values. For each product, the f5a9f386 manifest and validator agree on ZIP hash, DLL hash, MVID and entries. The four-profile audit uses that same DLL hash and MVID. Git shows only documentation and evidence changes from f5a9f386 to H.",
      "Read-only Git comparisons confirmed observer-only invalidation selects tools/KmcRemovalObserver/Info.json at 3e10838e, leaves the main input set unchanged, and selects no observer changes from f5a9f386 to H. The full feature diff passes git diff --check.",
      "I applied the two MSBuild digest patterns to the checked-in pin text and a JSON-equivalent whitespace variant in memory. Both patterns matched the original and missed the variant."
    ],
    "inspected": [
      "The enrolled mission, repository instructions, validation policy, current handoffs, developer report, prior ledger, full feature diff and round-two commit history.",
      "Both production projects and manifests, MSBuild targets, acquisition and pin helpers, packaging scripts, standalone validators, frozen-source logic and tier reuse paths.",
      "The guarded launcher, base-game dependency admission, inventory and game-result readers, normal-player observer admission, suite snapshot writer, profile records and compatibility evidence."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The H-bound Fast gate has a controller log. Other runs are attributed to the source revisions in their retained records.",
      "The reported 28/28 frozen-source and 107/107 inventory totals have no separate execution log in the supplied evidence. The retained harness summary records 280 passes and three failures also listed at a61c1a5c.",
      "All four loader rows and the combined-stack check are native NOT RUN. The static audits establish direct member resolution only. The snapshot does not establish current disposable-fixture availability or the live PR description."
    ],
    "required_gaps": [
      "End-to-end loader-profile selection and mixed-stack refusal through the actual guarded entry point.",
      "Final compiler-input ordering, validated pin definitions at both standalone validators, and complete measured receipt comparison for normal-player observer admission.",
      "Reuse admission tied to material inputs and output bytes, with matching plan and execution behavior.",
      "A consistent current handoff, an indexed full loader inventory with its original provenance, and qualification fixtures produced from one unchanged build."
    ],
    "source_inspection_only": [
      "The later CoreCompile hook ordering, normal-player observer receipt path, unchecked manifest version, incomplete standalone pin mapping and omitted reuse inputs follow from the candidate source. I did not run builds, packaging or native workflows in the review snapshot.",
      "The runtime profile fixtures exercise individual readers. Their source and reported totals do not establish that the launcher forwards a selected profile."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Both projects import LoaderReferences.targets before Microsoft.CSharp.targets. A Directory.Build.targets hook registered later for BeforeTargets=\"CoreCompile\" can change the list after the KMC assertion. The 9/9 test rewrites it at AfterTargets=\"FindReferenceAssembliesForReferences\", which the assertion does catch.",
      "blocking_rationale": "A supported MSBuild extension point still permits the list consumed by Csc to differ from the list that passed verification.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The verified UMM and Harmony12 paths and hashes are the compiler inputs after the supported reference-selection hooks have run.",
      "id": "R1-F1",
      "location": {
        "file": "LoaderReferences.targets",
        "line": 14
      },
      "priority": "P1",
      "requested_correction": "Order the final assertion after the standard and repository-level compiler-reference hooks in both projects. Keep early cache verification.",
      "requested_evidence": "Extend the existing direct-build fixture with a Directory.Build.targets adjustment at BeforeTargets=\"CoreCompile\". Show refusal before emission for both projects alongside the passing controls.",
      "title": "Run the compiler-input check after the last project reference hook",
      "trigger": "A project adjusts ReferencePathWithRefAssemblies in a conventional pre-compilation target imported after the KMC target."
    },
    {
      "acceptance_ids": [
        "mission \u00a77"
      ],
      "actual": "The launcher still opens planning/ENVIRONMENT-FINGERPRINT.json at line 403 and passes no profile to the readers at lines 1351 and 1367. BaseGameIsolation pins three roles to that fingerprint and records no selected profile. The inventory checks the profile's UMM identity only on the cache branch and its Harmony2 identity only when HarmonySharedState appears. The 0.32.4 and 0.33.4 target IDs still lack selectable records.",
      "blocking_rationale": "A matching alternate environment cannot use the normal launch path. A direct-load inventory without shared state can also pass without establishing the selected Harmony2 identity.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "One reviewed selection controls preflight, dependency admission, result reading, cache and shared-state checks, and the resulting receipt.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 403
      },
      "priority": "P1",
      "requested_correction": "Resolve the profile at the guarded entry point, retain the owner installation as default, and propagate its identity through admission and evidence. Verify the relevant loader stack independently of whether the optional cache and shared-state observations appear. Identify the reviewed comparison profiles and their native eligibility explicitly.",
      "requested_evidence": "Show offline launcher-to-reader admission for the owner and baseline profiles, mixed UMM/Harmony refusal with and without a cache or shared state, and a receipt identifying the selected profile. Keep unexecuted native rows marked NOT RUN.",
      "title": "Carry the selected runtime profile through the guarded launcher",
      "trigger": "A disposable environment selects the baseline, or a loader inventory contains the staged DLL without a cache or shared-state assembly."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The normal-player path discards Validate-ObserverPackage's result and calls Assert-KmcNormalPlayerObserverManifest, which checks the outer ZIP and returns the receipt's DLL fields. The common admission path now checks DLL hash, MVID and entries, but omits version. New-KmcQualificationSuiteSnapshot still copies manifest.version into the suite.",
      "blocking_rationale": "A stale receipt can misidentify the observer before staging or put the wrong product version into a new qualification record.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every admission binds the measured DLL hash, MVID, product version and entries to the receipt before those fields reach a request or suite.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 515
      },
      "priority": "P1",
      "requested_correction": "Reuse one measured validator result across the common and normal-player admissions. Return and compare the measured version for both products while retaining the observer's distinct source-ownership and frozen-identity checks.",
      "requested_evidence": "Exercise normal-player observer admission with a correct outer hash and stale DLL fields, and common admission with only the receipt version changed. Show both refusals before staging or snapshot creation and acceptance of matching receipts.",
      "title": "Bind the normal-player observer and receipt version to the measured ZIP",
      "trigger": "A valid ZIP is accompanied by a receipt with stale observer DLL fields or a stale product version."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Get-PinnedLoaderReferences now validates the required mapping. Both standalone validators still read the raw JSON themselves. Assert-KmcPinnedLoaderPackageReferences iterates whichever records are present. Omitting the UMM record removes its emitted-version check, and the remaining dependency allowlist checks names only.",
      "blocking_rationale": "A malformed pin definition can allow the released higher-UMM DLL through standalone validation with a lowered manifest.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Build and standalone validation require both canonical pins and bind the declared minimum to the UMM reference version.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/UmmLoaderContracts.ps1",
        "line": 38
      },
      "priority": "P2",
      "requested_correction": "Share a side-effect-free pin-definition parser across acquisition, compiler checking and both package validators. Standalone ZIP inspection should work without downloading references or requiring a populated cache.",
      "requested_evidence": "Run both standalone validators with missing, duplicated or mismapped pins and with a minimum/UMM-version disagreement. They should reject the definition and continue to accept genuine packages with the canonical mapping.",
      "title": "Validate the pin definition at standalone package admission",
      "trigger": "A standalone validator is invoked after a required pin is omitted, renamed or made inconsistent with the minimum."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The stamp hashes selected project files and cached references. It contains no product DLL, observer DLL or component-runner hash, and omits compiled sources and the build bootstrap. Replacing bin/Release/KingmakerMountedCombat.dll after a good build leaves the stamp unchanged. The Full -SkipBuild branch also delegates to Test.ps1 before reaching the guard.",
      "blocking_rationale": "A supported reuse path can credit a different binary or stale compiled source to the verified build.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A reuse receipt identifies the material build inputs and the exact outputs being credited.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/LoaderInputStamp.ps1",
        "line": 7
      },
      "priority": "P2",
      "requested_correction": "Bind the receipt to the relevant source and build inputs and the hashes of the outputs produced. Require it before supported candidate-output reuse, or refuse reuse where that relationship cannot be established.",
      "requested_evidence": "Keep the existing pin/helper/project cases. Also show that replacing or removing a shipping DLL, replacing the component runner, or changing a compiled source rejects reuse. Exercise the Full admission boundary without running the Full campaign.",
      "title": "Bind reused build outputs to their bytes and source",
      "trigger": "An output is replaced or a compiled source changes after the loader-input stamp was written."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a77",
        "mission \u00a78"
      ],
      "actual": "CURRENT.md reports 153 component checks. The controller log records 1,488 components and 153 source checks. README.md, AUTONOMOUS-RESUME.md and docs/CURRENT-SUPPORT.md still call the build and API checks pending. The compatibility matrix lists completed work as remaining. The installed native row says only that no run occurred, and the developer report marks a group containing the 280-pass/3-fail harness run as passed.",
      "blocking_rationale": "The current entry points give conflicting instructions about completed work, failed checks and the reason the installed native row remains open.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Current documents and the checkpoint report agree on observed counts, product and evidence identities, static results, failures and native prerequisites.",
      "id": "R1-F11",
      "location": {
        "file": "CURRENT.md",
        "line": 34
      },
      "priority": "P2",
      "requested_correction": "Reconcile the current entry points and matrix. Identify the actual fixture, guarded environment, admission or budget prerequisite for each unrun case. Report the harness failure separately with its baseline comparison and distinguish dated history from current instructions.",
      "requested_evidence": "Provide consistent local records and a matching draft-PR handoff. For any available and authorized native case, supply the guarded outcome. For an unavailable case, name the missing prerequisite.",
      "title": "Make the current handoff agree with the recorded results",
      "trigger": "A maintainer follows the published build, support or resume instructions."
    },
    {
      "acceptance_ids": [
        "mission \u00a75"
      ],
      "actual": "The new MSBuild expressions search raw JSON for an exact spelling of \"File\": followed later by \"Sha256\":. A space before the colon leaves the parsed JSON identical but makes both digest matches empty. Placing Sha256 before File has the same effect.",
      "blocking_rationale": "A semantically unchanged pin file can pass acquisition and stop every production compile.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "All consumers interpret the same validated JSON records regardless of insignificant whitespace or property order.",
      "id": "R2-F1",
      "location": {
        "file": "LoaderReferences.targets",
        "line": 20
      },
      "priority": "P2",
      "requested_correction": "Obtain the final hash expectations from the parsed pin definition while retaining the check on the actual compiler files.",
      "requested_evidence": "Show that a valid reformatted or reordered copy of the pin definition produces the same accepted direct build, and that altered cache bytes still fail.",
      "title": "Read compiler digests from parsed JSON",
      "trigger": "A formatter changes whitespace or property order in loader-reference-pins.json."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "Fast and Candidate execute a new 'reuse check' at line 257. Their plan omits that step and its prerequisite, so -Plan can label a DLL reused when its stamp is absent. The selection and initial budget estimate also omit the step.",
      "blocking_rationale": "The documented planning entry point gives an incorrect account of the changed validation path.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The plan, initial admission estimate and executed steps describe the same reuse check.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 115
      },
      "priority": "P2",
      "requested_correction": "Include reuse admission in the resolved step list and report its prerequisite accurately in plan mode.",
      "requested_evidence": "Compare Fast or Candidate -Plan -SkipBuild for matching and missing receipts. The plan should describe the same first check as execution and remain read-only.",
      "title": "Include the reuse check in the tier plan",
      "trigger": "A developer plans a Fast or Candidate invocation with -SkipBuild."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a77",
        "mission \u00a78"
      ],
      "actual": "Round two removed loader-profile-inventory.json from the evidence directory and its index. The record at 215d68fe contains the newer rows' measured MVIDs and the full identity of the owner's additional 0Harmony-1.2.dll. The current table gives only a hash prefix for that file. The index also attributes the collection to f5a9f386 although the direct-build record identifies 796935cc.",
      "blocking_rationale": "A reader following the current evidence index loses part of the measured Harmony stack and can attribute a run to the wrong source.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The handoff identifies the complete measured loader stack and binds each retained result to its original producer.",
      "id": "R2-F3",
      "location": {
        "file": "docs/evidence/umm-0311/INDEX.md",
        "line": 3
      },
      "priority": "P2",
      "requested_correction": "Restore the original inventory with its provenance or link its immutable Git blob and digest. Identify the producer revision for records reused from an earlier commit.",
      "requested_evidence": "An indexed record should recover the exact hashes and MVIDs, including the owner's extra Harmony assembly, and distinguish the 796935cc build test from the f5a9f386 package records. The existing Git record supplies these observations.",
      "title": "Retain the full measured loader inventory and its provenance",
      "trigger": "A maintainer uses the round-two evidence index to reconstruct the runtime rows."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md \u00a74"
      ],
      "actual": "The new focused test invokes Package.ps1 and Package-Observer.ps1 consecutively on the unchanged clone. Both invoke Build-Local.ps1, which rebuilds the whole solution. The existing -IncludeRemovalObserver route produces both manifests and runs both validators from one build.",
      "blocking_rationale": "The focused gate repeats an unchanged production build against the repository's explicit build-once policy and consumes the shared validation allowance.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Both baseline package fixtures come from one clean qualification build. A deliberately different DLL is built separately for the stale-receipt case.",
      "id": "R2-F4",
      "location": {
        "file": "scripts/Test-PackageAdmissionAndReuse.ps1",
        "line": 87
      },
      "priority": "P2",
      "requested_correction": "Use the existing combined packaging route for the unchanged fixtures and retain independent standalone validation of each ZIP.",
      "requested_evidence": "Show both positive packages and the existing negative archive cases from the combined build in the next admitted focused run.",
      "title": "Build the two baseline package fixtures together",
      "trigger": "The package-boundary regression creates its main and observer baseline fixtures."
    },
    {
      "acceptance_ids": [
        "mission \u00a76"
      ],
      "actual": "The expression that reads the main DLL entry name opens a ZipArchive without disposing it. On Windows its file handle can survive until finalization, while final cleanup suppresses deletion errors.",
      "blocking_rationale": "The possible residue is confined to the test's temporary directory and does not affect the package verdict or the owner's installation.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The focused test releases its archive handles before cleaning its temporary files.",
      "id": "R2-F5",
      "location": {
        "file": "scripts/Test-PackageAdmissionAndReuse.ps1",
        "line": 116
      },
      "priority": "P3",
      "requested_correction": "Dispose the archive when looking up the DLL entry, as the neighboring entry helpers already do.",
      "requested_evidence": "On the next Windows run, confirm the temporary package directory is removed.",
      "title": "Close the main ZIP after reading its entry name",
      "trigger": "The Windows test cleans up before the unreferenced archive handle is finalized."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F1",
      "rationale": "The early one-shot hook is gone and the recorded post-selection rewrites are caught for both DLLs. The assertion is still registered before a later-imported hook at the same CoreCompile boundary."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F10",
      "rationale": "The historical default is the immutable fb743911200f150af407198c6c28519e522ef58c. The script checks commit existence and both checkouts, and the retained record identifies the same historical build."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "CURRENT.md now identifies both packaged artifacts and the static audit command. The gate contradicts its component count, three current entry points still report completed checks as pending, and the owner native row lacks a concrete prerequisite."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F12",
      "rationale": "git diff --check across the full merge-base-to-candidate diff exited 0."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The two leaf readers accept a profile parameter. The guarded launcher, dependency admission and receipts still use the fixed environment, and exact profile checks in the inventory depend on optional observations."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "Observer admission now compares the entire tools/KmcRemovalObserver subtree alongside the shared build-input check. The main-only path excludes that subtree. Read-only Git comparisons confirmed the distinction for an observer change and a documentation-only descendant."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The H-bound gate confirms the common path rejects a swapped main DLL. Normal-player observer admission still discards the measurement, and the common comparison still omits the product version."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The observer has its own clean-source manifest, two-entry ZIP, standalone validation and four-profile audit. All records agree on DLL f9697f91feeab95b6a095dabf1a901cbd3d0aae3863efed6ea53fba8fe271958 and MVID a5e1bb2f-21fa-4bdd-9bd8-cd0861dc3a96. The audit reports 8 types and 20 members resolved on each profile."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F6",
      "rationale": "The H gate confirms the acquisition helper refuses omitted and renamed Harmony12 pins. Neither standalone validator applies that definition check before trusting the reference list."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "Both validators explicitly require a scalar string. The H gate accepted both genuine packages and rejected a one-element ManagerVersion array in each."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The new stamp catches the tested pin, verifier and project changes. It cannot detect replacement of an output or a change to compiled source, and the Full reuse branch does not consult it."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F9",
      "rationale": "The controller log shows real ZIPs accepted by both standalone validators, dependency-injected archives rejected by each, and the released 0.33.0-referencing main DLL rejected with the 0.31.1 declaration. The emitted-version negative was exercised on the main product."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r02-67d4b6",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and the removal observer declare UMM 0.31.1. Their retained Windows package records and static audits identify UnityModManager 0.31.1.0 and 0Harmony12 1.2.0.1. Direct API references resolved on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4. Native behavior remains unverified on every row.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Any additional validation allowance required after the recorded development budget was exhausted.",
      "Human-play acceptance and the eventual disposition of draft PR #62."
    ],
    "scope_reviewed": "The final Mounted Combat checkpoint for PR #62, including its separately shipped observer, the #63 baseline reconciliation, affected build and runtime tooling, and all twelve prior findings.",
    "unresolved_questions": [
      "Which approved disposable environment and fixtures, if any, are currently available for the installed 0.33.0 load/settings, mounted combat and observer checks?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
