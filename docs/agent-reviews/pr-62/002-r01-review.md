<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r01-review part=1/1 sha256=4e6eadf6eaaa7640030cd36c81dfb52d7657db25a489dfef273f920024453829 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `215d68fe3d`

**Decision:** `changes_required` — The Windows gate passed, and the main packaged DLL has credible evidence of a 0.31.1.0 build and a four-profile static API check. The final compiler input can still escape verification, runtime readers hard-code one loader, and receipt checks can admit stale artifact identities. The separately shipped removal observer also lacks package and compatibility evidence.

## Continuation for Claude

Correct the compiler and pin checks together, then bind reused outputs and package receipts to the files actually inspected. Carry one reviewed runtime-profile identity through admission, readers and receipts. Exercise those boundaries offline with real files, both shipping ZIPs and the same frozen DLL for each product’s static comparisons. Update the current handoff and PR with the artifact identities, reproducible commands, actual counts and native status. Keep the work within PR #62, preserve historical evidence and the Mounted Shield branches, and respect the existing budgets and live-installation guards. Native cases require an approved disposable setup. Record the specific missing prerequisite wherever a case remains NOT RUN.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Verify the references Csc actually receives
- Location: `LoaderReferences.targets:14`; acceptance: mission §5, mission §6
- Trigger: Reference-assembly selection changes a loader input after ResolveAssemblyReferences.
- Expected: The build verifies the exact UMM and Harmony12 files in the final reference list consumed by Csc.
- Actual: AssertPinnedLoaderReferencesResolved has both AfterTargets="ResolveAssemblyReferences" and BeforeTargets="CoreCompile". MSBuild runs a target once. The earlier hook can therefore complete the check using ReferencePath before ReferencePathWithRefAssemblies is finalized. The passing stray-HintPath case changes the earlier resolution input.
- Why it blocks / does not: A direct build can pass the guard and compile against a different reference assembly.
- Requested correction: Keep early cache verification and perform a distinct check after reference-assembly selection, immediately before compilation. Check the effective compiler list and verify its two loader files against the pins.
- Requested evidence: Show a normal direct build and a build where the initial HintPath is correct but the final compiler reference resolves elsewhere. The second build must stop before emitting a DLL. Cover the shared target used by both shipping projects.

**R1-F2** · P1 · defect · blocks readiness — Carry the selected loader profile through the runtime checks
- Location: `scripts/runtime/Test-RuntimeGameResult.ps1:182`; acceptance: mission §7
- Trigger: An approved disposable environment or a reader fixture selects genuine 0.31.1, 0.32.4 or 0.33.4 bytes.
- Expected: One explicitly selected, reviewed runtime identity governs preflight, dependency admission, native observations, cache/shared-state exceptions and the resulting receipt.
- Actual: The launcher reads one fixed environment fingerprint. Test-RuntimeGameResult takes the UMM hash from its fingerprint argument but requires the literal version 0.33.0.0. Chunk7WorldEvidence separately hard-codes the 0.33.0 cache creator and Harmony 2.3.6.0 shared-state creator. The profile document also lists 0.32.4 and 0.33.4 as targets without selectable profile entries for them.
- Why it blocks / does not: A genuine alternate fingerprint and matching observation fail the result reader. The version and hash can also be checked against different authorities.
- Requested correction: Pass the selected runtime profile through those consumers. Bind the UMM and relevant Harmony identities to it while preserving exact path, hash, MVID, candidate and cache checks. Keep the measured owner installation as the default and historical records bound to their original profiles.
- Requested evidence: Provide focused offline admission and reader results for matching profiles and mixed-profile refusals, including the baseline and owner rows and their different Harmony2 creators. Record native results only for cases admitted and actually run through existing guards.

**R1-F3** · P1 · defect · blocks readiness — Invalidate frozen removal observers when their own project changes
- Location: `scripts/runtime/FrozenProductSource.ps1:40`; acceptance: mission §5, mission §6
- Trigger: A later harness requests an archived observer after an observer-only change that leaves its advertised version unchanged.
- Expected: Frozen observer reuse is invalidated by changes to its project references, sources, manifest or shared loader inputs.
- Actual: The frozen-source diff includes src and the new shared loader files but omits tools/KmcRemovalObserver. Assert-KmcFrozenProductManifest uses this same decision when the generator is scripts/Package-Observer.ps1. Hashing the old observer ZIP proves which old file was supplied, while an observer-only project or source change goes undetected.
- Why it blocks / does not: The archived observer can be admitted after its production inputs have changed.
- Requested correction: Include the observer’s actual build inputs in the decision for observer reuse. Preserve valid main-only reuse when the main package is the sole artifact being qualified.
- Requested evidence: Use the temporary Git fixture to show that an observer-only project-reference change rejects the archived observer and that a documentation-only descendant retains the original observer identity.

**R1-F4** · P1 · defect · blocks readiness — Cross-check receipt fields against the DLL inside the supplied ZIP
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:3618`; acceptance: mission §5, mission §6
- Trigger: A partially stale receipt names the current archive and source but carries a previous DLL identity.
- Expected: Package admission checks the receipt’s DLL identity and entry records against the actual validated ZIP before using them for a request or freeze.
- Actual: Assert-KmcPackageManifest compares the source, branch, path and outer ZIP hash, then returns the receipt’s dllSha256, dllMvid and entries without comparing them to the archive. The launcher discards the standalone validator’s measured result. New-KmcQualificationSuiteSnapshot can consequently copy stale DLL fields into a fresh suite record. The normal-player observer admission has the same separation.
- Why it blocks / does not: A receipt with the correct ZIP hash and another build’s DLL identity can pass package admission and misidentify the candidate.
- Requested correction: Compare the measured package result with the source/build receipt at the admission boundary for both products. Reuse that measured result across callers so they agree on the DLL hash, MVID, version and entries.
- Requested evidence: Demonstrate acceptance of a matching ZIP/receipt and pre-staging refusal when the outer ZIP identity is correct but the DLL hash, MVID or entry identity belongs to another build.

**R1-F5** · P1 · evidence_gap · blocks readiness — Qualify the separately shipped removal observer
- Location: `docs/evidence/umm-0311/api-audit.json:3`; acceptance: mission §5, mission §6, mission §7
- Trigger: The evidence for the complete shipping change is checked artifact by artifact.
- Expected: The observer has its own clean-source package receipt, standalone validation and static compatibility assessment on frozen observer bytes.
- Actual: The Fast log lists KmcRemovalObserver.dll as a build output. The retained package command selects the main package only, and both package records and api-audit.json identify KingmakerMountedCombat.dll. There is no observer ZIP, DLL hash/MVID, packaged-reference result or observer member audit in the supplied evidence.
- Why it blocks / does not: The observer ships independently and calls UMM and Harmony APIs itself. The main DLL’s package and 14-type/42-member audit establish coverage for the main DLL alone.
- Requested correction: Use the existing observer packaging route and standalone validator. Assess the packaged observer’s own UMM/Harmony references against the four selected loader profiles.
- Requested evidence: Supply the observer source SHA, DLL and ZIP hashes, MVID, emitted reference versions, entry list, validator result and per-profile API results tied to that same DLL.

**R1-F6** · P2 · defect · blocks readiness — Reject incomplete or mismapped loader pins
- Location: `scripts/Get-PinnedLoaderReferences.ps1:13`; acceptance: mission §5, mission §6
- Trigger: A pin record is omitted or its File changes while the old compiler-named DLL remains in the cache.
- Expected: The pin configuration covers both required compiler inputs under their actual names and files, and ties the manager minimum to the UMM version for standalone validation as well as builds.
- Actual: The pin reader requires one UnityModManager record and otherwise iterates whichever records exist. It does not require the Harmony12 record or require each record’s File to match the filename used by MSBuild. With a populated cache, an omitted or renamed record leaves a compiler input unchecked. The package helper likewise checks only records present in the JSON.
- Why it blocks / does not: A malformed pin configuration can silently remove hash or emitted-version verification for a required shipping dependency.
- Requested correction: Validate the complete required pin mapping once and use that validated mapping in the build and package checks. Reject missing, duplicate or mismapped required records.
- Requested evidence: Show that an omitted Harmony12 pin and a pin naming a different cache file both fail with an already populated cache, alongside a passing genuine configuration.

**R1-F7** · P2 · defect · blocks readiness — Enforce the JSON type of the packaged manager minimum
- Location: `scripts/Validate-Package.ps1:56`; acceptance: mission §5, mission §6
- Trigger: An external ZIP supplies a one-element array for ManagerVersion.
- Expected: The packaged ManagerVersion is the JSON string "0.31.1".
- Actual: Both package validators cast ManagerVersion to string before comparing it. Windows PowerShell converts the JSON array ["0.31.1"] to the same comparison string. An otherwise valid supplied ZIP therefore passes this check with a malformed loader declaration. The build-side manifest check already verifies the scalar type.
- Why it blocks / does not: A ZIP accepted before installation can violate the shipping manifest contract.
- Requested correction: Require a scalar string with the exact baseline value in both standalone validators.
- Requested evidence: For each validator, show that an otherwise valid ZIP passes with the scalar value and fails with a one-element array.

**R1-F8** · P2 · defect · blocks readiness — Bind Fast’s reused outputs to the current loader inputs
- Location: `scripts/Test-Tiers.ps1:246`; acceptance: mission §5, mission §6
- Trigger: Fast is invoked with -SkipBuild after a baseline change in a worktree containing older build outputs.
- Expected: A Fast check credited to a reused candidate verifies that its inputs and output bytes still match the verified build.
- Actual: Fast with -SkipBuild skips the MSBuild targets and proceeds to the existing component executable and source checks. This path has no comparison between the reused outputs and the current compiler pins, helper or project inputs. The actual controller gate for this review did run its build.
- Why it blocks / does not: The supported reuse path can return a Fast pass after a material loader-input change using outputs from the prior build.
- Requested correction: At the candidate-reuse boundary, require a matching build identity or refuse candidate verification with -SkipBuild when the relationship cannot be established.
- Requested evidence: Show that unchanged verified outputs can be reused and that a pin, helper or project-reference change with the old bin directory is refused.

**R1-F9** · P2 · evidence_gap · blocks readiness — Exercise the negative package cases through the standalone validators
- Location: `docs/UMM-0311-BASELINE.md:61`; acceptance: mission §6
- Trigger: An externally supplied archive is validated independently of Package.ps1.
- Expected: The pre-install validators accept real baseline packages and reject mismatched or contaminated archives for the intended reason.
- Actual: The retained main ZIP is a positive case. The documented higher-version DLL rejection exercised the shared metadata helper. The controller gate exercised MSBuild. No supplied run shows the standalone validators rejecting an actual supplied ZIP for a manifest/compiler mismatch or an injected dependency.
- Why it blocks / does not: The mission explicitly requires execution of those failure boundaries. Source inspection establishes that checks exist, but cannot confirm the complete archive path.
- Requested correction: Exercise the actual standalone entry points on temporary ZIPs with a mismatched emitted loader reference and with an extra UMM, Harmony or game payload. Reuse existing focused cases where available.
- Requested evidence: Retain the commands, source and fixture identities, positive controls, rejection reasons and exit results for the affected shipping validators.

**R1-F10** · P2 · defect · blocks readiness — Pin the before-build regression to its historical commit
- Location: `scripts/Test-PinnedLoaderMsBuild.ps1:4`; acceptance: mission §6, mission §8
- Trigger: The focused test is repeated after origin/main advances to a revision with the new build guard.
- Expected: The before-build uses the immutable revision whose installed-loader behavior the test records.
- Actual: Test-PinnedLoaderMsBuild.ps1 defaults its historical control to origin/main. Once that ref contains the pinning change, the first build in the fresh clone requires the cache that this test deliberately has not acquired yet. A correct implementation then fails its own regression test.
- Why it blocks / does not: The documented focused check depends on a moving branch retaining pre-change behavior.
- Requested correction: Default to the recorded pre-baseline commit fb743911200f150af407198c6c28519e522ef58c or another immutable fixture identity. Check that each checkout selected the intended commit before building.
- Requested evidence: Show the test using the same historical control when the clone’s origin/main points to a later pinned revision.

**R1-F11** · P2 · required_gap · blocks readiness — Update the current handoff to match the Windows results
- Location: `CURRENT.md:10`; acceptance: mission §7, mission §8
- Trigger: A maintainer follows the repository’s required current-status and resume documents.
- Expected: Current build and support documentation identifies the tested artifact, method, exact profiles, outcomes, limitations and reproduction commands.
- Actual: CURRENT.md says Windows is unavailable, the build and API sweep are pending, and the fingerprint is unchanged. README, AUTONOMOUS-RESUME and CURRENT-SUPPORT repeat stale status. The compatibility matrix still lists completed main-DLL checks as remaining work. The later Windows section calls 153 source checks component tests and gives no reproducible invocation for the four-profile audit.
- Why it blocks / does not: The designated current handoff contradicts the candidate’s evidence and gives the next developer the wrong work to resume.
- Requested correction: Reconcile the current entry points and matrix with the final evidence. Give the audit tool/version and invocation, link both products’ records, distinguish source and component counts, and identify the precise prerequisite for each unrun native case, including the installed 0.33.0 row. State combined-stack status and preserve dated history.
- Requested evidence: Provide a consistent current handoff and corresponding draft-PR summary with reproducible checks, source and artifact identities, the #63 reconciliation and explicit native limits.

**R1-F12** · P3 · optional_polish · non-blocking — Trim added blank lines at EOF
- Location: `docs/CHUNK7-NATIVE-LOADER-IDENTITY.md:124`; acceptance: —
- Trigger: Checking the feature diff for whitespace changes.
- Expected: Touched files retain their existing end-of-file formatting.
- Actual: git diff --check reports added blank lines at EOF in seven touched files.
- Why it blocks / does not: Cosmetic formatting only.
- Requested correction: Remove the seven extra trailing blank lines when editing the affected files.
- Requested evidence: A clean git diff --check for those paths.

#### Evidence assessment

- **Inspected:** The complete mission, repository instructions, validation policy, current handoffs, all 37 changed files, the feature diff, developer report, transcript notice, prior ledger and controller gate evidence.; Both production projects and manifests, the pin acquisition helper, MSBuild targets, Fast selector, qualification packager, standalone validators, frozen-source checks and their package-admission consumers.; The local PR #63 commit 5be0f737, loader profile and environment records, launcher preflight, dependency admission, runtime result and cache/shared-state readers, focused tests and structured evidence exports.
- **Executed verification:** Confirmed the snapshot HEAD and mission SHA-256 against the controller inputs. Recomputed the controller gate-log SHA-256 and all seven exported evidence-file hashes listed in docs/evidence/umm-0311/INDEX.md. All matched.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-PinnedLoaderMsBuild.ps1 at 215d68fe in 96.0 seconds. The solution build listed both shipping projects. Components passed 1488/1488, Validate-Source passed 153/153 and the focused MSBuild test passed 6/6.; The focused log records a real main-project build against installed UMM 0.33.0.0 before the change and UMM 0.31.1.0 with Harmony12 1.2.0.1 afterward. It records refusals for a missing cache, altered Harmony12 bytes and an off-directory HintPath.; The package manifest, validation output and static audit at 272238cef56ab9307ec115360df62439efcdf072 agree on main DLL SHA-256 d20474bf5e4593c84dbb662f5a552798ab07ffe13853085a8443bd157dfe756d and MVID 9db1be50-bceb-4bb3-a167-14e4ce288e8c. The ZIP SHA-256 is df1eb7718236e80f0f6c1d1012aa49f95c6b51e48567ab7858acf14b885aebd8. Git confirms only documentation and evidence changed between that build commit and the review commit.; The static audit records 14 types and 42 members resolved for that main DLL on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. The inventory identifies the Harmony components and distinguishes the owner-install copy from component-assembled profiles.; A read-only Windows PowerShell check confirmed that the package validators’ string conversion accepts a one-element JSON array as the scalar manager version. git diff --check also identified seven added blank lines at EOF.
- **Source inspection only:** Both production csproj files import the shared targets, retain .NET Framework 4.7 and use Private=False for loader references. The pinned URLs, hashes and assembly versions match the mission. The cache is Git-ignored.; Both standalone validators inspect the DLL extracted from the supplied ZIP and enforce a two-entry payload allowlist. Their receipt consumers, manifest type checks and negative execution coverage have the gaps below.; Compiler target ordering, frozen observer admission, Fast reuse and alternate-profile behavior were assessed from source. No build, game launch or installation workflow was run during this review.
- **Required gaps:** Enforce and test the final Csc input, complete pin mappings and identity-bound reuse on the affected build paths.; Bind runtime-profile selection and supplied package receipts to the exact measured dependencies and DLLs.; Supply the observer package and static matrix, plus executed standalone ZIP refusal cases for the shipping validators.; Make the historical build comparison reproducible and reconcile current documentation with the recorded results and native prerequisites.
- **Limitations:** This was a read-only review. The foreground developer transcript was withheld. The controller gate is independently logged, while the separate 25/25 frozen-source, 101/101 native-reader-contract and 22-pass loader-checker totals appear only in developer-authored summaries among the supplied inputs.; The supplied snapshot contains exported records rather than the original candidate ZIPs or loader binaries. The controller identifies draft PR #62 and its head. Its live description and discussions were unavailable to this offline review.; All native rows are NOT RUN for these artifacts. There is no observed load/settings, mount/dismount, combat, observer, cold-reload or combined-stack result. The last gate receipt records 1.55 minutes remaining in the development allowance.

#### Summary

- **Delivered behavior:** Both shipping manifests declare UMM 0.31.1. The ordinary Windows build uses independently pinned UMM and Harmony12 references. The main ZIP is linked to its clean source commit and its DLL has static results on all four requested loader versions. The current owner loader was measured as 0.33.0.0.
- **Scope reviewed:** Final review of the Mounted Combat portion of the mission, including the removal observer, PR #63 reconciliation, compiler paths, packaging, reuse, loader profiles, evidence and current documentation.
- **Pending later-phase work:** none
- **Unresolved questions:** Which approved disposable runtime, fixture and remaining allowance are available for each focused native case, particularly the owner’s 0.33.0 row?
- **Remaining owner review:** Human gameplay acceptance and disposition of draft PR #62 after the engineering findings close.; Any additional disposable-runtime authorization or validation allowance required by the existing guards.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20ba7-d1e7-7953-bbef-aaf25d5e29d3`, duration 716.4s, response sha256 `9dc9c02a91366bb3596bcf12e031ca03f1fc46dd400ea52fdc6735b03a4706fc`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r01-fd1f0b`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "215d68fe3dea9fc11cdd7d341e428b63d3197c33",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The Windows gate passed, and the main packaged DLL has credible evidence of a 0.31.1.0 build and a four-profile static API check. The final compiler input can still escape verification, runtime readers hard-code one loader, and receipt checks can admit stale artifact identities. The separately shipped removal observer also lacks package and compatibility evidence.\n\n## Continuation for Claude\n\nCorrect the compiler and pin checks together, then bind reused outputs and package receipts to the files actually inspected. Carry one reviewed runtime-profile identity through admission, readers and receipts. Exercise those boundaries offline with real files, both shipping ZIPs and the same frozen DLL for each product\u2019s static comparisons. Update the current handoff and PR with the artifact identities, reproducible commands, actual counts and native status. Keep the work within PR #62, preserve historical evidence and the Mounted Shield branches, and respect the existing budgets and live-installation guards. Native cases require an approved disposable setup. Record the specific missing prerequisite wherever a case remains NOT RUN.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the snapshot HEAD and mission SHA-256 against the controller inputs. Recomputed the controller gate-log SHA-256 and all seven exported evidence-file hashes listed in docs/evidence/umm-0311/INDEX.md. All matched.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-PinnedLoaderMsBuild.ps1 at 215d68fe in 96.0 seconds. The solution build listed both shipping projects. Components passed 1488/1488, Validate-Source passed 153/153 and the focused MSBuild test passed 6/6.",
      "The focused log records a real main-project build against installed UMM 0.33.0.0 before the change and UMM 0.31.1.0 with Harmony12 1.2.0.1 afterward. It records refusals for a missing cache, altered Harmony12 bytes and an off-directory HintPath.",
      "The package manifest, validation output and static audit at 272238cef56ab9307ec115360df62439efcdf072 agree on main DLL SHA-256 d20474bf5e4593c84dbb662f5a552798ab07ffe13853085a8443bd157dfe756d and MVID 9db1be50-bceb-4bb3-a167-14e4ce288e8c. The ZIP SHA-256 is df1eb7718236e80f0f6c1d1012aa49f95c6b51e48567ab7858acf14b885aebd8. Git confirms only documentation and evidence changed between that build commit and the review commit.",
      "The static audit records 14 types and 42 members resolved for that main DLL on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. The inventory identifies the Harmony components and distinguishes the owner-install copy from component-assembled profiles.",
      "A read-only Windows PowerShell check confirmed that the package validators\u2019 string conversion accepts a one-element JSON array as the scalar manager version. git diff --check also identified seven added blank lines at EOF."
    ],
    "inspected": [
      "The complete mission, repository instructions, validation policy, current handoffs, all 37 changed files, the feature diff, developer report, transcript notice, prior ledger and controller gate evidence.",
      "Both production projects and manifests, the pin acquisition helper, MSBuild targets, Fast selector, qualification packager, standalone validators, frozen-source checks and their package-admission consumers.",
      "The local PR #63 commit 5be0f737, loader profile and environment records, launcher preflight, dependency admission, runtime result and cache/shared-state readers, focused tests and structured evidence exports."
    ],
    "limitations": [
      "This was a read-only review. The foreground developer transcript was withheld. The controller gate is independently logged, while the separate 25/25 frozen-source, 101/101 native-reader-contract and 22-pass loader-checker totals appear only in developer-authored summaries among the supplied inputs.",
      "The supplied snapshot contains exported records rather than the original candidate ZIPs or loader binaries. The controller identifies draft PR #62 and its head. Its live description and discussions were unavailable to this offline review.",
      "All native rows are NOT RUN for these artifacts. There is no observed load/settings, mount/dismount, combat, observer, cold-reload or combined-stack result. The last gate receipt records 1.55 minutes remaining in the development allowance."
    ],
    "required_gaps": [
      "Enforce and test the final Csc input, complete pin mappings and identity-bound reuse on the affected build paths.",
      "Bind runtime-profile selection and supplied package receipts to the exact measured dependencies and DLLs.",
      "Supply the observer package and static matrix, plus executed standalone ZIP refusal cases for the shipping validators.",
      "Make the historical build comparison reproducible and reconcile current documentation with the recorded results and native prerequisites."
    ],
    "source_inspection_only": [
      "Both production csproj files import the shared targets, retain .NET Framework 4.7 and use Private=False for loader references. The pinned URLs, hashes and assembly versions match the mission. The cache is Git-ignored.",
      "Both standalone validators inspect the DLL extracted from the supplied ZIP and enforce a two-entry payload allowlist. Their receipt consumers, manifest type checks and negative execution coverage have the gaps below.",
      "Compiler target ordering, frozen observer admission, Fast reuse and alternate-profile behavior were assessed from source. No build, game launch or installation workflow was run during this review."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "AssertPinnedLoaderReferencesResolved has both AfterTargets=\"ResolveAssemblyReferences\" and BeforeTargets=\"CoreCompile\". MSBuild runs a target once. The earlier hook can therefore complete the check using ReferencePath before ReferencePathWithRefAssemblies is finalized. The passing stray-HintPath case changes the earlier resolution input.",
      "blocking_rationale": "A direct build can pass the guard and compile against a different reference assembly.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The build verifies the exact UMM and Harmony12 files in the final reference list consumed by Csc.",
      "id": "R1-F1",
      "location": {
        "file": "LoaderReferences.targets",
        "line": 14
      },
      "priority": "P1",
      "requested_correction": "Keep early cache verification and perform a distinct check after reference-assembly selection, immediately before compilation. Check the effective compiler list and verify its two loader files against the pins.",
      "requested_evidence": "Show a normal direct build and a build where the initial HintPath is correct but the final compiler reference resolves elsewhere. The second build must stop before emitting a DLL. Cover the shared target used by both shipping projects.",
      "title": "Verify the references Csc actually receives",
      "trigger": "Reference-assembly selection changes a loader input after ResolveAssemblyReferences."
    },
    {
      "acceptance_ids": [
        "mission \u00a77"
      ],
      "actual": "The launcher reads one fixed environment fingerprint. Test-RuntimeGameResult takes the UMM hash from its fingerprint argument but requires the literal version 0.33.0.0. Chunk7WorldEvidence separately hard-codes the 0.33.0 cache creator and Harmony 2.3.6.0 shared-state creator. The profile document also lists 0.32.4 and 0.33.4 as targets without selectable profile entries for them.",
      "blocking_rationale": "A genuine alternate fingerprint and matching observation fail the result reader. The version and hash can also be checked against different authorities.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "One explicitly selected, reviewed runtime identity governs preflight, dependency admission, native observations, cache/shared-state exceptions and the resulting receipt.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/runtime/Test-RuntimeGameResult.ps1",
        "line": 182
      },
      "priority": "P1",
      "requested_correction": "Pass the selected runtime profile through those consumers. Bind the UMM and relevant Harmony identities to it while preserving exact path, hash, MVID, candidate and cache checks. Keep the measured owner installation as the default and historical records bound to their original profiles.",
      "requested_evidence": "Provide focused offline admission and reader results for matching profiles and mixed-profile refusals, including the baseline and owner rows and their different Harmony2 creators. Record native results only for cases admitted and actually run through existing guards.",
      "title": "Carry the selected loader profile through the runtime checks",
      "trigger": "An approved disposable environment or a reader fixture selects genuine 0.31.1, 0.32.4 or 0.33.4 bytes."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The frozen-source diff includes src and the new shared loader files but omits tools/KmcRemovalObserver. Assert-KmcFrozenProductManifest uses this same decision when the generator is scripts/Package-Observer.ps1. Hashing the old observer ZIP proves which old file was supplied, while an observer-only project or source change goes undetected.",
      "blocking_rationale": "The archived observer can be admitted after its production inputs have changed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Frozen observer reuse is invalidated by changes to its project references, sources, manifest or shared loader inputs.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/runtime/FrozenProductSource.ps1",
        "line": 40
      },
      "priority": "P1",
      "requested_correction": "Include the observer\u2019s actual build inputs in the decision for observer reuse. Preserve valid main-only reuse when the main package is the sole artifact being qualified.",
      "requested_evidence": "Use the temporary Git fixture to show that an observer-only project-reference change rejects the archived observer and that a documentation-only descendant retains the original observer identity.",
      "title": "Invalidate frozen removal observers when their own project changes",
      "trigger": "A later harness requests an archived observer after an observer-only change that leaves its advertised version unchanged."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Assert-KmcPackageManifest compares the source, branch, path and outer ZIP hash, then returns the receipt\u2019s dllSha256, dllMvid and entries without comparing them to the archive. The launcher discards the standalone validator\u2019s measured result. New-KmcQualificationSuiteSnapshot can consequently copy stale DLL fields into a fresh suite record. The normal-player observer admission has the same separation.",
      "blocking_rationale": "A receipt with the correct ZIP hash and another build\u2019s DLL identity can pass package admission and misidentify the candidate.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Package admission checks the receipt\u2019s DLL identity and entry records against the actual validated ZIP before using them for a request or freeze.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 3618
      },
      "priority": "P1",
      "requested_correction": "Compare the measured package result with the source/build receipt at the admission boundary for both products. Reuse that measured result across callers so they agree on the DLL hash, MVID, version and entries.",
      "requested_evidence": "Demonstrate acceptance of a matching ZIP/receipt and pre-staging refusal when the outer ZIP identity is correct but the DLL hash, MVID or entry identity belongs to another build.",
      "title": "Cross-check receipt fields against the DLL inside the supplied ZIP",
      "trigger": "A partially stale receipt names the current archive and source but carries a previous DLL identity."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76",
        "mission \u00a77"
      ],
      "actual": "The Fast log lists KmcRemovalObserver.dll as a build output. The retained package command selects the main package only, and both package records and api-audit.json identify KingmakerMountedCombat.dll. There is no observer ZIP, DLL hash/MVID, packaged-reference result or observer member audit in the supplied evidence.",
      "blocking_rationale": "The observer ships independently and calls UMM and Harmony APIs itself. The main DLL\u2019s package and 14-type/42-member audit establish coverage for the main DLL alone.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The observer has its own clean-source package receipt, standalone validation and static compatibility assessment on frozen observer bytes.",
      "id": "R1-F5",
      "location": {
        "file": "docs/evidence/umm-0311/api-audit.json",
        "line": 3
      },
      "priority": "P1",
      "requested_correction": "Use the existing observer packaging route and standalone validator. Assess the packaged observer\u2019s own UMM/Harmony references against the four selected loader profiles.",
      "requested_evidence": "Supply the observer source SHA, DLL and ZIP hashes, MVID, emitted reference versions, entry list, validator result and per-profile API results tied to that same DLL.",
      "title": "Qualify the separately shipped removal observer",
      "trigger": "The evidence for the complete shipping change is checked artifact by artifact."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The pin reader requires one UnityModManager record and otherwise iterates whichever records exist. It does not require the Harmony12 record or require each record\u2019s File to match the filename used by MSBuild. With a populated cache, an omitted or renamed record leaves a compiler input unchecked. The package helper likewise checks only records present in the JSON.",
      "blocking_rationale": "A malformed pin configuration can silently remove hash or emitted-version verification for a required shipping dependency.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The pin configuration covers both required compiler inputs under their actual names and files, and ties the manager minimum to the UMM version for standalone validation as well as builds.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/Get-PinnedLoaderReferences.ps1",
        "line": 13
      },
      "priority": "P2",
      "requested_correction": "Validate the complete required pin mapping once and use that validated mapping in the build and package checks. Reject missing, duplicate or mismapped required records.",
      "requested_evidence": "Show that an omitted Harmony12 pin and a pin naming a different cache file both fail with an already populated cache, alongside a passing genuine configuration.",
      "title": "Reject incomplete or mismapped loader pins",
      "trigger": "A pin record is omitted or its File changes while the old compiler-named DLL remains in the cache."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Both package validators cast ManagerVersion to string before comparing it. Windows PowerShell converts the JSON array [\"0.31.1\"] to the same comparison string. An otherwise valid supplied ZIP therefore passes this check with a malformed loader declaration. The build-side manifest check already verifies the scalar type.",
      "blocking_rationale": "A ZIP accepted before installation can violate the shipping manifest contract.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The packaged ManagerVersion is the JSON string \"0.31.1\".",
      "id": "R1-F7",
      "location": {
        "file": "scripts/Validate-Package.ps1",
        "line": 56
      },
      "priority": "P2",
      "requested_correction": "Require a scalar string with the exact baseline value in both standalone validators.",
      "requested_evidence": "For each validator, show that an otherwise valid ZIP passes with the scalar value and fails with a one-element array.",
      "title": "Enforce the JSON type of the packaged manager minimum",
      "trigger": "An external ZIP supplies a one-element array for ManagerVersion."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "Fast with -SkipBuild skips the MSBuild targets and proceeds to the existing component executable and source checks. This path has no comparison between the reused outputs and the current compiler pins, helper or project inputs. The actual controller gate for this review did run its build.",
      "blocking_rationale": "The supported reuse path can return a Fast pass after a material loader-input change using outputs from the prior build.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A Fast check credited to a reused candidate verifies that its inputs and output bytes still match the verified build.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 246
      },
      "priority": "P2",
      "requested_correction": "At the candidate-reuse boundary, require a matching build identity or refuse candidate verification with -SkipBuild when the relationship cannot be established.",
      "requested_evidence": "Show that unchanged verified outputs can be reused and that a pin, helper or project-reference change with the old bin directory is refused.",
      "title": "Bind Fast\u2019s reused outputs to the current loader inputs",
      "trigger": "Fast is invoked with -SkipBuild after a baseline change in a worktree containing older build outputs."
    },
    {
      "acceptance_ids": [
        "mission \u00a76"
      ],
      "actual": "The retained main ZIP is a positive case. The documented higher-version DLL rejection exercised the shared metadata helper. The controller gate exercised MSBuild. No supplied run shows the standalone validators rejecting an actual supplied ZIP for a manifest/compiler mismatch or an injected dependency.",
      "blocking_rationale": "The mission explicitly requires execution of those failure boundaries. Source inspection establishes that checks exist, but cannot confirm the complete archive path.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The pre-install validators accept real baseline packages and reject mismatched or contaminated archives for the intended reason.",
      "id": "R1-F9",
      "location": {
        "file": "docs/UMM-0311-BASELINE.md",
        "line": 61
      },
      "priority": "P2",
      "requested_correction": "Exercise the actual standalone entry points on temporary ZIPs with a mismatched emitted loader reference and with an extra UMM, Harmony or game payload. Reuse existing focused cases where available.",
      "requested_evidence": "Retain the commands, source and fixture identities, positive controls, rejection reasons and exit results for the affected shipping validators.",
      "title": "Exercise the negative package cases through the standalone validators",
      "trigger": "An externally supplied archive is validated independently of Package.ps1."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78"
      ],
      "actual": "Test-PinnedLoaderMsBuild.ps1 defaults its historical control to origin/main. Once that ref contains the pinning change, the first build in the fresh clone requires the cache that this test deliberately has not acquired yet. A correct implementation then fails its own regression test.",
      "blocking_rationale": "The documented focused check depends on a moving branch retaining pre-change behavior.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The before-build uses the immutable revision whose installed-loader behavior the test records.",
      "id": "R1-F10",
      "location": {
        "file": "scripts/Test-PinnedLoaderMsBuild.ps1",
        "line": 4
      },
      "priority": "P2",
      "requested_correction": "Default to the recorded pre-baseline commit fb743911200f150af407198c6c28519e522ef58c or another immutable fixture identity. Check that each checkout selected the intended commit before building.",
      "requested_evidence": "Show the test using the same historical control when the clone\u2019s origin/main points to a later pinned revision.",
      "title": "Pin the before-build regression to its historical commit",
      "trigger": "The focused test is repeated after origin/main advances to a revision with the new build guard."
    },
    {
      "acceptance_ids": [
        "mission \u00a77",
        "mission \u00a78"
      ],
      "actual": "CURRENT.md says Windows is unavailable, the build and API sweep are pending, and the fingerprint is unchanged. README, AUTONOMOUS-RESUME and CURRENT-SUPPORT repeat stale status. The compatibility matrix still lists completed main-DLL checks as remaining work. The later Windows section calls 153 source checks component tests and gives no reproducible invocation for the four-profile audit.",
      "blocking_rationale": "The designated current handoff contradicts the candidate\u2019s evidence and gives the next developer the wrong work to resume.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Current build and support documentation identifies the tested artifact, method, exact profiles, outcomes, limitations and reproduction commands.",
      "id": "R1-F11",
      "location": {
        "file": "CURRENT.md",
        "line": 10
      },
      "priority": "P2",
      "requested_correction": "Reconcile the current entry points and matrix with the final evidence. Give the audit tool/version and invocation, link both products\u2019 records, distinguish source and component counts, and identify the precise prerequisite for each unrun native case, including the installed 0.33.0 row. State combined-stack status and preserve dated history.",
      "requested_evidence": "Provide a consistent current handoff and corresponding draft-PR summary with reproducible checks, source and artifact identities, the #63 reconciliation and explicit native limits.",
      "title": "Update the current handoff to match the Windows results",
      "trigger": "A maintainer follows the repository\u2019s required current-status and resume documents."
    },
    {
      "acceptance_ids": [],
      "actual": "git diff --check reports added blank lines at EOF in seven touched files.",
      "blocking_rationale": "Cosmetic formatting only.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Touched files retain their existing end-of-file formatting.",
      "id": "R1-F12",
      "location": {
        "file": "docs/CHUNK7-NATIVE-LOADER-IDENTITY.md",
        "line": 124
      },
      "priority": "P3",
      "requested_correction": "Remove the seven extra trailing blank lines when editing the affected files.",
      "requested_evidence": "A clean git diff --check for those paths.",
      "title": "Trim added blank lines at EOF",
      "trigger": "Checking the feature diff for whitespace changes."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r01-fd1f0b",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Both shipping manifests declare UMM 0.31.1. The ordinary Windows build uses independently pinned UMM and Harmony12 references. The main ZIP is linked to its clean source commit and its DLL has static results on all four requested loader versions. The current owner loader was measured as 0.33.0.0.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Human gameplay acceptance and disposition of draft PR #62 after the engineering findings close.",
      "Any additional disposable-runtime authorization or validation allowance required by the existing guards."
    ],
    "scope_reviewed": "Final review of the Mounted Combat portion of the mission, including the removal observer, PR #63 reconciliation, compiler paths, packaging, reuse, loader profiles, evidence and current documentation.",
    "unresolved_questions": [
      "Which approved disposable runtime, fixture and remaining allowance are available for each focused native case, particularly the owner\u2019s 0.33.0 row?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
