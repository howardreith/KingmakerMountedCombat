<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r03-review part=1/1 sha256=f61fbed45047e23135014ccc15d4efeb0dd6e88da4238f1304a12d14242451b5 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `293668a4c0`

**Decision:** `changes_required` — Both shipping DLLs have credible UMM 0.31.1 compile and package evidence. The focused gate passed on the submitted commit. Readiness is blocked because the launcher still cannot admit a matching baseline environment, and both local and archived binaries can remain eligible after material build inputs change.

Continuation for Claude

Finish profile selection at the guarded launcher. Compare the reviewed profile with the actual files in the selected environment, including Harmony 2, before staging. Carry that verified identity into the applicable readers and receipts. Preserve existing positional calls, the owner-install default and the original identities of historical evidence. Exercise the owner and baseline paths with offline fixtures.

Complete the input checks for local and archived reuse, including the two build files introduced in this correction. Enforce the receipt at supported reuse entry points. Make the plan report whether the artifact is eligible, and perform execution-time hashing inside the admitted step. Extend the existing focused tests at those boundaries.

Record the gate counts and duration in the current handoff, and supply the existing authorization record for the mission budget cited in the report. Keep follow-up within the available allowances and the Mounted Combat draft PR. Preserve the live installation, protected saves and separate Mounted Shield work. The four native rows can remain NOT RUN under the documented disposable-environment prerequisite.

#### Findings

**R1-F2** · P1 · defect · blocks readiness — Verify the selected loader against the target environment before staging
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:405`; acceptance: mission §7
- Trigger: A disposable target actually contains the baseline stack, or the owner target has the expected UMM and a different Harmony 2.
- Expected: The guarded entry point admits a matching reviewed environment and refuses a mixed loader stack before staging. Readers and receipts use the identity established at admission.
- Actual: The launcher always reads planning/ENVIRONMENT-FINGERPRINT.json, whose UMM role is 0.33.0. It rejects 0.31.1 against that file before inspecting the selected game root. At line 467 it passes only the fingerprint to Get-KmcBaseGameDependencies. That helper compares gameplay, UMM and Harmony12, but only inventories the observed 0Harmony.dll hash. The admission receipt can therefore name the expected Harmony 2 beside dependencies containing a different hash. The explicit inventory check occurs after launch on the applicable base-game path.
- Why it blocks / does not: A correctly prepared baseline environment is unreachable at this commit, and a mixed owner environment can pass dependency admission.
- Requested correction: Resolve the reviewed selection against the authorized target's actual loader files. Preserve the game, path, candidate and historical identity checks. Pass the verified profile through dependency admission, applicable result readers and receipts.
- Requested evidence: Exercise the real admission and reader helpers with matching owner and baseline fixtures. Show the resulting receipt, and show that wrong UMM or Harmony 2 bytes fail before staging, including the direct-load case without cache or shared state.

**R3-F1** · P1 · defect · blocks readiness — Include the split compiler guard and pin parser in frozen-source admission
- Location: `scripts/runtime/FrozenProductSource.ps1:40`; acceptance: mission §5, mission §6
- Trigger: Only LoaderReferences.Final.targets or scripts/LoaderPins.ps1 changes after a package is frozen.
- Expected: Changing either shared build input invalidates the affected archived package.
- Actual: The frozen-source path list includes LoaderReferences.targets and Get-PinnedLoaderReferences.ps1. It omits LoaderReferences.Final.targets and scripts/LoaderPins.ps1, which this round introduced and both production builds consume. The observer's additional subtree comparison also excludes these shared files.
- Why it blocks / does not: A descendant can change the last compiler check or the parser that decides which references are valid and still admit either archived product.
- Requested correction: Include both new shared build files in the frozen-source comparison for the main product and observer.
- Requested evidence: Extend the temporary Git fixture so a change to each file independently refuses archived reuse. Retain the documentation-only acceptance and the existing observer-only distinction.

**R1-F8** · P2 · defect · blocks readiness — Bind local output reuse to all material inputs and its direct consumer
- Location: `scripts/LoaderInputStamp.ps1:16`; acceptance: mission §5, mission §6
- Trigger: The build bootstrap, local reference selection or main shipping manifest changes while the output files remain in place.
- Expected: Reuse is bound to the material inputs and outputs of the build being credited, and the check runs before those outputs are consumed.
- Actual: The local stamp now covers both DLLs, the runner and compiled sources. Its input selection still excludes scripts/Build-Local.ps1, the imported LocalGamePaths.props and the root Info.json that the build verifier reads. Changing the main ManagerVersion alone leaves the stamp current. A direct scripts/Test.ps1 -SkipBuild also starts the component runner after checking only that it exists.
- Why it blocks / does not: A supported reuse path can credit outputs under build settings or a manifest that a fresh build would reject.
- Requested correction: Bind the receipt to the build bootstrap, effective local build settings and required shipping manifests. Enforce it for direct Test.ps1 reuse as well as tiered reuse.
- Requested evidence: Show refusal after a bootstrap option change, a material local props change and a main-manifest-only mismatch. Show that direct -SkipBuild refuses stale outputs before starting the runner, using a bounded fixture.

**R2-F2** · P2 · defect · blocks readiness — Report the same reuse verdict throughout the tier plan
- Location: `scripts/Test-Tiers.ps1:161`; acceptance: mission §6, docs/VALIDATION-POLICY.md
- Trigger: A developer runs -Plan -SkipBuild with an existing DLL and a missing or stale receipt.
- Expected: The artifact line and the planned reuse prerequisite agree for current, missing and stale receipts.
- Actual: The plan correctly lists the reuse check first and identifies a missing or stale receipt. The artifact line independently prints '(reused)' whenever -SkipBuild is set and the DLL exists. The focused test checks the prerequisite line and misses this conflicting verdict.
- Why it blocks / does not: The documented planning command still identifies an ineligible artifact as reused.
- Requested correction: Use the computed receipt state when describing the artifact's eligibility.
- Requested evidence: Check both the prerequisite and artifact lines for all three receipt states, with the plan remaining read-only.

**R3-F2** · P2 · defect · blocks readiness — Admit the workflow budget before hashing reused outputs
- Location: `scripts/Test-Tiers.ps1:114`; acceptance: mission §6, docs/VALIDATION-POLICY.md
- Trigger: An ordinary Fast, Candidate or Full -SkipBuild invocation finds an existing build receipt.
- Expected: Execution performs the receipt validation once, inside the admitted reuse step. Plan mode can inspect the receipt to describe its prerequisite.
- Actual: Line 114 calls Test-KmcLoaderInputStamp for every -SkipBuild invocation. With an existing receipt this hashes sources, references and outputs before Request-KmcWorkflowAdmission at line 188. An admitted execution repeats the scan at line 246.
- Why it blocks / does not: A refused or exhausted budget still incurs the validation work. An admitted run performs that work twice and meters only the second scan.
- Requested correction: Compute the detailed pre-execution receipt state only for -Plan. Keep the step in the ordinary selection and estimate, then validate inside the admitted step.
- Requested evidence: Show that a denied execution reaches budget refusal before the stamp scan and that an admitted execution performs one scan.

**R3-F3** · P2 · defect · blocks readiness — Preserve positional scenario selection
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:4`; acceptance: mission §7
- Trigger: An existing caller supplies the scenario as the first positional argument.
- Expected: Existing positional and named scenario calls retain their meaning when the optional profile is introduced.
- Actual: RuntimeLoaderProfileId was inserted before Scenario in a script with implicit positional binding. A previously valid positional mount-dismount-user-flow argument now binds as the profile ID, leaves Scenario at mod-load-smoke and is refused as an unknown profile.
- Why it blocks / does not: Adding the profile option breaks the launcher's existing call contract.
- Requested correction: Preserve the existing parameter positions, for example by appending the new optional parameter.
- Requested evidence: Verify positional scenario selection and named profile selection against the launcher's parameter contract without starting a transaction.

**R1-F11** · P2 · required_gap · blocks readiness — Attach the baseline gate counts to their own handoff
- Location: `CURRENT.md:44`; acceptance: mission §6, mission §8, AGENTS.md
- Trigger: A maintainer follows the current UMM validation record or tries to verify the developer's stated counts.
- Expected: The current handoff identifies the applicable gate, duration and observed counts with their producer, alongside the separately reported harness failures and native prerequisites.
- Actual: The current UMM section lists six offline checks but omits the controller gate result. The 1,488 component and 153 source counts that the disposition attributes to that section appear under the earlier 0.2.15 release. The matching current PR handoff is also absent from the review inputs.
- Why it blocks / does not: A maintainer using the current handoff cannot distinguish the candidate's gate execution from the earlier release record without consulting the controller log.
- Requested correction: Add a concise entry for a completed baseline gate with its actual commit, command, duration and counts. Use the controller handoff or draft PR for the receipt on the final documentation commit.
- Requested evidence: Supply the matching current handoff and PR text or controller record. The gate on 293668a4 already establishes 205.2 seconds, 4/4 steps, 1,488 component checks, 153 source checks and 45 focused checks.

**R3-F4** · P2 · evidence_gap · blocks readiness — Substantiate the mission-specific validation allowance
- Location: `—`; acceptance: mission §4, docs/VALIDATION-POLICY.md
- Trigger: The final qualification relies on a separate budget after exhaustion of the previous development allowance.
- Expected: The reported change of validation allowance is traceable to the existing budget record and its authorizing assignment.
- Actual: The gate log confirms 7.12 of 15 minutes charged to umm-0311-baseline. The report says that this budget was created after the development ledger was exhausted and cites an owner-authorized allowanceAdjustments entry. The supplied evidence contains the spend and omits the cited adjustment and authorizing assignment.
- Why it blocks / does not: The gate execution is verified. Compliance with the applicable allowance depends on the separately claimed assignment.
- Requested correction: Provide the relevant excerpt of the existing adjustment and its authorizing assignment through the review evidence channel. Preserve the original ledgers and gate receipt.
- Requested evidence: A record tying the mission budget ID and 15-minute allowance to the assignment that authorized the two gate entries.

**R3-F5** · P3 · optional_polish · non-blocking — Bound the Full refusal test on its failure path
- Location: `scripts/Test-PackageAdmissionAndReuse.ps1:234`; acceptance: mission §6, docs/VALIDATION-POLICY.md
- Trigger: The reuse guard regresses and the focused stale-receipt case unexpectedly reaches Test.ps1.
- Expected: The focused negative test can detect entry into the Full child immediately.
- Actual: The test calls the real Full umbrella in its clone and checks for its execution only after the child returns. A regression in the precise guard under test would start the broad suite.
- Why it blocks / does not: The recorded candidate refused the stale receipt before the umbrella. This affects the cost of diagnosing a future regression.
- Requested correction: Use a fail-fast marker at the Full child boundary in the disposable clone while retaining the real tier admission and receipt check.
- Requested evidence: A broken guard should produce a focused failure as soon as the child is entered.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — Both shipping projects now import the final check after Microsoft.CSharp.targets. The 12-case direct-build record includes refusal of the Directory.Build.targets CoreCompile rewrite for each project, with positive builds and early cache refusal retained.
- **R1-F11** — `fix_rejected` — The README, resume and support entries now distinguish the completed build and static audit from native work. The harness failures and concrete native prerequisites are recorded. The cited component and source counts are still missing from the current baseline section.
- **R1-F2** — `fix_rejected` — The explicit inventory path now verifies UMM and Harmony 2 even without cache or shared state. The launcher still selects against the fixed owner fingerprint, and dependency admission receives no profile. The wiring test searches for source strings and never demonstrates matching baseline admission.
- **R1-F4** — `fix_verified` — The common and normal-player observer paths compare the standalone measurement with the receipt, including version, package and DLL hashes, MVID and entries. The controller gate exercised stale observer fields, a version-only mismatch and matching controls.
- **R1-F6** — `fix_verified` — The acquisition helper and both standalone validators use the same side-effect-free parser. The gate records refusal of missing, duplicate, mismapped and minimum-disagreeing definitions for both ZIPs.
- **R1-F8** — `fix_rejected` — Output replacement, source edits and tiered Full refusal now have passing regressions. Read-only enumeration confirmed that the build bootstrap, local props and root manifest are absent from the stamp. Direct Test.ps1 reuse also lacks the check.
- **R2-F1** — `fix_verified` — The compiler check obtains hashes from parsed records. The direct-build record shows successful compilation with reordered JSON properties and refusal of altered cache bytes.
- **R2-F2** — `fix_rejected` — The reuse step and its prerequisite now enter the plan and initial estimate. An existing DLL is still printed as reused when that same plan reports the receipt missing or stale.
- **R2-F3** — `fix_verified` — The restored inventory has the identical Git blob a9ef6700253a028c25436ead1648e368f9900036 from 215d68fe. It contains the full newer profiles and the owner's extra Harmony assembly. The index distinguishes producer revisions and reused audits.
- **R2-F4** — `fix_verified` — The fixture uses Package.ps1 -IncludeRemovalObserver to produce both baseline packages. The candidate-bound gate confirms the combined route and subsequent standalone positive and negative cases.
- **R2-F5** — `fix_verified` — Both entry-name helpers dispose their ZipArchive in finally. This fixes the identified handle leak by inspection. The gate did not separately observe removal of the temporary directory.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository instructions, validation policy, current and resume handoffs, full feature diff, round-three changes, developer report, transcript notice, controller receipt and all eleven pending findings.; Both projects and manifests, MSBuild imports, pin acquisition and parsing, standalone validators, package admission, frozen-source comparison, local build receipt, tier planning and execution, launcher, dependency admission, result readers and their focused fixtures.; The indexed package records, four-profile loader inventory, static audits for both DLLs, direct-build record, loader-contract results and harness failure comparison.
- **Executed verification:** The controller receipt for 293668a4 records Test-Tiers.ps1 -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1, exit 0 in 205.2 seconds. Its log shows 4/4 tier steps, 1,488 component checks, 153 source checks and 45 package/admission checks. I verified the log SHA-256 against the receipt.; The gate log shows the compiler-input check for both shipping projects. Its disposable package fixture built both ZIPs together, accepted the genuine packages, and rejected the higher-UMM release DLL, extra dependencies, malformed pins and stale receipt fields.; Read-only Git checks confirmed HEAD, a clean feature diff under git diff --check, and only documentation or evidence changes after 11df053b. All 17 exported evidence hashes matched their indexed Git blobs. Both package manifests matched the standalone measurements, and both DLL hashes and MVIDs matched their static audits.; I evaluated the Git path selections used by the two reuse checks and confirmed the omissions in R1-F8 and R3-F1. An isolated PowerShell parameter-binding check reproduced the positional shift in R3-F3 without invoking the launcher.
- **Source inspection only:** The launcher and reuse defects follow from the candidate's parameter order, call sites, comparisons and input lists. No build, installation or game workflow was run during this review.; At 11df053b the retained records report 12/12 direct MSBuild cases, 116/116 loader-contract cases, 28/28 frozen-source cases and 23 loader checks with two unavailable inputs. The launcher-wiring case searches source text.; The harness summary records 280 passed and three failed at e4e32899, with the same named failures at f5a9f386 and a61c1a5c. The static audits are reused from f5a9f386 for the matching DLL bytes.
- **Required gaps:** A working offline admission path for a matching baseline environment, with pre-staging refusal of mixed UMM/Harmony identities and a receipt consumed by the applicable reader.; Reuse refusal for changes to the omitted local and archived build inputs, including coverage of the direct reuse entry point.; Consistent plan output, budget admission before execution-time hashing, and preservation of positional launcher calls.; A current handoff tied to the gate results and evidence of the existing assignment for the 15-minute budget.
- **Limitations:** The controller withheld the mixed foreground transcript. The candidate-bound Fast gate is independently recorded. Other offline runs are supported by their indexed records at the stated producer revisions.; Native behavior, save/reload, observer behavior in game and combined-stack compatibility remain untested on this candidate. The available launcher stages into the installed game, and the handoff identifies no approved Mounted disposable game-copy harness.; The fixed inputs contain neither the current draft-PR body nor the cited budget adjustment. The Windows test also did not record whether its temporary directory was removed after cleanup.

#### Summary

- **Delivered behavior:** Mounted Combat and the removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Both standalone ZIP validators inspect the packaged DLL. Matching DLL hashes bind the static audits to the packaged artifacts on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. All four native rows and the combined-stack check remain NOT RUN.
- **Scope reviewed:** Mounted Combat draft PR #62 at the fixed final candidate, including its separately shipped observer, all eleven pending findings and the directly affected build, reuse, launcher and evidence paths.
- **Pending later-phase work:** none
- **Unresolved questions:** The supplied materials do not identify the authorizing assignment for the reported 15-minute allowance or show the current draft-PR validation text.
- **Remaining owner review:** Human gameplay acceptance and any decision to authorize a future guarded native qualification environment.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a20e1b-a078-7a72-9ee8-334357682c58`, duration 994.1s, response sha256 `bf8f894e3a37472b26bc5d7e1eb5ae6352512b962db20ce734858c43fbb7b968`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r03-735731`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "293668a4c0250794f95d1dd36afee373ef75412e",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both shipping DLLs have credible UMM 0.31.1 compile and package evidence. The focused gate passed on the submitted commit. Readiness is blocked because the launcher still cannot admit a matching baseline environment, and both local and archived binaries can remain eligible after material build inputs change.\n\nContinuation for Claude\n\nFinish profile selection at the guarded launcher. Compare the reviewed profile with the actual files in the selected environment, including Harmony 2, before staging. Carry that verified identity into the applicable readers and receipts. Preserve existing positional calls, the owner-install default and the original identities of historical evidence. Exercise the owner and baseline paths with offline fixtures.\n\nComplete the input checks for local and archived reuse, including the two build files introduced in this correction. Enforce the receipt at supported reuse entry points. Make the plan report whether the artifact is eligible, and perform execution-time hashing inside the admitted step. Extend the existing focused tests at those boundaries.\n\nRecord the gate counts and duration in the current handoff, and supply the existing authorization record for the mission budget cited in the report. Keep follow-up within the available allowances and the Mounted Combat draft PR. Preserve the live installation, protected saves and separate Mounted Shield work. The four native rows can remain NOT RUN under the documented disposable-environment prerequisite.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt for 293668a4 records Test-Tiers.ps1 -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1, exit 0 in 205.2 seconds. Its log shows 4/4 tier steps, 1,488 component checks, 153 source checks and 45 package/admission checks. I verified the log SHA-256 against the receipt.",
      "The gate log shows the compiler-input check for both shipping projects. Its disposable package fixture built both ZIPs together, accepted the genuine packages, and rejected the higher-UMM release DLL, extra dependencies, malformed pins and stale receipt fields.",
      "Read-only Git checks confirmed HEAD, a clean feature diff under git diff --check, and only documentation or evidence changes after 11df053b. All 17 exported evidence hashes matched their indexed Git blobs. Both package manifests matched the standalone measurements, and both DLL hashes and MVIDs matched their static audits.",
      "I evaluated the Git path selections used by the two reuse checks and confirmed the omissions in R1-F8 and R3-F1. An isolated PowerShell parameter-binding check reproduced the positional shift in R3-F3 without invoking the launcher."
    ],
    "inspected": [
      "The enrolled mission, repository instructions, validation policy, current and resume handoffs, full feature diff, round-three changes, developer report, transcript notice, controller receipt and all eleven pending findings.",
      "Both projects and manifests, MSBuild imports, pin acquisition and parsing, standalone validators, package admission, frozen-source comparison, local build receipt, tier planning and execution, launcher, dependency admission, result readers and their focused fixtures.",
      "The indexed package records, four-profile loader inventory, static audits for both DLLs, direct-build record, loader-contract results and harness failure comparison."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The candidate-bound Fast gate is independently recorded. Other offline runs are supported by their indexed records at the stated producer revisions.",
      "Native behavior, save/reload, observer behavior in game and combined-stack compatibility remain untested on this candidate. The available launcher stages into the installed game, and the handoff identifies no approved Mounted disposable game-copy harness.",
      "The fixed inputs contain neither the current draft-PR body nor the cited budget adjustment. The Windows test also did not record whether its temporary directory was removed after cleanup."
    ],
    "required_gaps": [
      "A working offline admission path for a matching baseline environment, with pre-staging refusal of mixed UMM/Harmony identities and a receipt consumed by the applicable reader.",
      "Reuse refusal for changes to the omitted local and archived build inputs, including coverage of the direct reuse entry point.",
      "Consistent plan output, budget admission before execution-time hashing, and preservation of positional launcher calls.",
      "A current handoff tied to the gate results and evidence of the existing assignment for the 15-minute budget."
    ],
    "source_inspection_only": [
      "The launcher and reuse defects follow from the candidate's parameter order, call sites, comparisons and input lists. No build, installation or game workflow was run during this review.",
      "At 11df053b the retained records report 12/12 direct MSBuild cases, 116/116 loader-contract cases, 28/28 frozen-source cases and 23 loader checks with two unavailable inputs. The launcher-wiring case searches source text.",
      "The harness summary records 280 passed and three failed at e4e32899, with the same named failures at f5a9f386 and a61c1a5c. The static audits are reused from f5a9f386 for the matching DLL bytes."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a77"
      ],
      "actual": "The launcher always reads planning/ENVIRONMENT-FINGERPRINT.json, whose UMM role is 0.33.0. It rejects 0.31.1 against that file before inspecting the selected game root. At line 467 it passes only the fingerprint to Get-KmcBaseGameDependencies. That helper compares gameplay, UMM and Harmony12, but only inventories the observed 0Harmony.dll hash. The admission receipt can therefore name the expected Harmony 2 beside dependencies containing a different hash. The explicit inventory check occurs after launch on the applicable base-game path.",
      "blocking_rationale": "A correctly prepared baseline environment is unreachable at this commit, and a mixed owner environment can pass dependency admission.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The guarded entry point admits a matching reviewed environment and refuses a mixed loader stack before staging. Readers and receipts use the identity established at admission.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 405
      },
      "priority": "P1",
      "requested_correction": "Resolve the reviewed selection against the authorized target's actual loader files. Preserve the game, path, candidate and historical identity checks. Pass the verified profile through dependency admission, applicable result readers and receipts.",
      "requested_evidence": "Exercise the real admission and reader helpers with matching owner and baseline fixtures. Show the resulting receipt, and show that wrong UMM or Harmony 2 bytes fail before staging, including the direct-load case without cache or shared state.",
      "title": "Verify the selected loader against the target environment before staging",
      "trigger": "A disposable target actually contains the baseline stack, or the owner target has the expected UMM and a different Harmony 2."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The frozen-source path list includes LoaderReferences.targets and Get-PinnedLoaderReferences.ps1. It omits LoaderReferences.Final.targets and scripts/LoaderPins.ps1, which this round introduced and both production builds consume. The observer's additional subtree comparison also excludes these shared files.",
      "blocking_rationale": "A descendant can change the last compiler check or the parser that decides which references are valid and still admit either archived product.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Changing either shared build input invalidates the affected archived package.",
      "id": "R3-F1",
      "location": {
        "file": "scripts/runtime/FrozenProductSource.ps1",
        "line": 40
      },
      "priority": "P1",
      "requested_correction": "Include both new shared build files in the frozen-source comparison for the main product and observer.",
      "requested_evidence": "Extend the temporary Git fixture so a change to each file independently refuses archived reuse. Retain the documentation-only acceptance and the existing observer-only distinction.",
      "title": "Include the split compiler guard and pin parser in frozen-source admission",
      "trigger": "Only LoaderReferences.Final.targets or scripts/LoaderPins.ps1 changes after a package is frozen."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The local stamp now covers both DLLs, the runner and compiled sources. Its input selection still excludes scripts/Build-Local.ps1, the imported LocalGamePaths.props and the root Info.json that the build verifier reads. Changing the main ManagerVersion alone leaves the stamp current. A direct scripts/Test.ps1 -SkipBuild also starts the component runner after checking only that it exists.",
      "blocking_rationale": "A supported reuse path can credit outputs under build settings or a manifest that a fresh build would reject.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Reuse is bound to the material inputs and outputs of the build being credited, and the check runs before those outputs are consumed.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/LoaderInputStamp.ps1",
        "line": 16
      },
      "priority": "P2",
      "requested_correction": "Bind the receipt to the build bootstrap, effective local build settings and required shipping manifests. Enforce it for direct Test.ps1 reuse as well as tiered reuse.",
      "requested_evidence": "Show refusal after a bootstrap option change, a material local props change and a main-manifest-only mismatch. Show that direct -SkipBuild refuses stale outputs before starting the runner, using a bounded fixture.",
      "title": "Bind local output reuse to all material inputs and its direct consumer",
      "trigger": "The build bootstrap, local reference selection or main shipping manifest changes while the output files remain in place."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "The plan correctly lists the reuse check first and identifies a missing or stale receipt. The artifact line independently prints '(reused)' whenever -SkipBuild is set and the DLL exists. The focused test checks the prerequisite line and misses this conflicting verdict.",
      "blocking_rationale": "The documented planning command still identifies an ineligible artifact as reused.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The artifact line and the planned reuse prerequisite agree for current, missing and stale receipts.",
      "id": "R2-F2",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 161
      },
      "priority": "P2",
      "requested_correction": "Use the computed receipt state when describing the artifact's eligibility.",
      "requested_evidence": "Check both the prerequisite and artifact lines for all three receipt states, with the plan remaining read-only.",
      "title": "Report the same reuse verdict throughout the tier plan",
      "trigger": "A developer runs -Plan -SkipBuild with an existing DLL and a missing or stale receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "Line 114 calls Test-KmcLoaderInputStamp for every -SkipBuild invocation. With an existing receipt this hashes sources, references and outputs before Request-KmcWorkflowAdmission at line 188. An admitted execution repeats the scan at line 246.",
      "blocking_rationale": "A refused or exhausted budget still incurs the validation work. An admitted run performs that work twice and meters only the second scan.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Execution performs the receipt validation once, inside the admitted reuse step. Plan mode can inspect the receipt to describe its prerequisite.",
      "id": "R3-F2",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 114
      },
      "priority": "P2",
      "requested_correction": "Compute the detailed pre-execution receipt state only for -Plan. Keep the step in the ordinary selection and estimate, then validate inside the admitted step.",
      "requested_evidence": "Show that a denied execution reaches budget refusal before the stamp scan and that an admitted execution performs one scan.",
      "title": "Admit the workflow budget before hashing reused outputs",
      "trigger": "An ordinary Fast, Candidate or Full -SkipBuild invocation finds an existing build receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a77"
      ],
      "actual": "RuntimeLoaderProfileId was inserted before Scenario in a script with implicit positional binding. A previously valid positional mount-dismount-user-flow argument now binds as the profile ID, leaves Scenario at mod-load-smoke and is refused as an unknown profile.",
      "blocking_rationale": "Adding the profile option breaks the launcher's existing call contract.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Existing positional and named scenario calls retain their meaning when the optional profile is introduced.",
      "id": "R3-F3",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 4
      },
      "priority": "P2",
      "requested_correction": "Preserve the existing parameter positions, for example by appending the new optional parameter.",
      "requested_evidence": "Verify positional scenario selection and named profile selection against the launcher's parameter contract without starting a transaction.",
      "title": "Preserve positional scenario selection",
      "trigger": "An existing caller supplies the scenario as the first positional argument."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "mission \u00a78",
        "AGENTS.md"
      ],
      "actual": "The current UMM section lists six offline checks but omits the controller gate result. The 1,488 component and 153 source counts that the disposition attributes to that section appear under the earlier 0.2.15 release. The matching current PR handoff is also absent from the review inputs.",
      "blocking_rationale": "A maintainer using the current handoff cannot distinguish the candidate's gate execution from the earlier release record without consulting the controller log.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The current handoff identifies the applicable gate, duration and observed counts with their producer, alongside the separately reported harness failures and native prerequisites.",
      "id": "R1-F11",
      "location": {
        "file": "CURRENT.md",
        "line": 44
      },
      "priority": "P2",
      "requested_correction": "Add a concise entry for a completed baseline gate with its actual commit, command, duration and counts. Use the controller handoff or draft PR for the receipt on the final documentation commit.",
      "requested_evidence": "Supply the matching current handoff and PR text or controller record. The gate on 293668a4 already establishes 205.2 seconds, 4/4 steps, 1,488 component checks, 153 source checks and 45 focused checks.",
      "title": "Attach the baseline gate counts to their own handoff",
      "trigger": "A maintainer follows the current UMM validation record or tries to verify the developer's stated counts."
    },
    {
      "acceptance_ids": [
        "mission \u00a74",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "The gate log confirms 7.12 of 15 minutes charged to umm-0311-baseline. The report says that this budget was created after the development ledger was exhausted and cites an owner-authorized allowanceAdjustments entry. The supplied evidence contains the spend and omits the cited adjustment and authorizing assignment.",
      "blocking_rationale": "The gate execution is verified. Compliance with the applicable allowance depends on the separately claimed assignment.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The reported change of validation allowance is traceable to the existing budget record and its authorizing assignment.",
      "id": "R3-F4",
      "location": {
        "file": null,
        "line": null
      },
      "priority": "P2",
      "requested_correction": "Provide the relevant excerpt of the existing adjustment and its authorizing assignment through the review evidence channel. Preserve the original ledgers and gate receipt.",
      "requested_evidence": "A record tying the mission budget ID and 15-minute allowance to the assignment that authorized the two gate entries.",
      "title": "Substantiate the mission-specific validation allowance",
      "trigger": "The final qualification relies on a separate budget after exhaustion of the previous development allowance."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "The test calls the real Full umbrella in its clone and checks for its execution only after the child returns. A regression in the precise guard under test would start the broad suite.",
      "blocking_rationale": "The recorded candidate refused the stale receipt before the umbrella. This affects the cost of diagnosing a future regression.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The focused negative test can detect entry into the Full child immediately.",
      "id": "R3-F5",
      "location": {
        "file": "scripts/Test-PackageAdmissionAndReuse.ps1",
        "line": 234
      },
      "priority": "P3",
      "requested_correction": "Use a fail-fast marker at the Full child boundary in the disposable clone while retaining the real tier admission and receipt check.",
      "requested_evidence": "A broken guard should produce a focused failure as soon as the child is entered.",
      "title": "Bound the Full refusal test on its failure path",
      "trigger": "The reuse guard regresses and the focused stale-receipt case unexpectedly reaches Test.ps1."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "Both shipping projects now import the final check after Microsoft.CSharp.targets. The 12-case direct-build record includes refusal of the Directory.Build.targets CoreCompile rewrite for each project, with positive builds and early cache refusal retained."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "The README, resume and support entries now distinguish the completed build and static audit from native work. The harness failures and concrete native prerequisites are recorded. The cited component and source counts are still missing from the current baseline section."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The explicit inventory path now verifies UMM and Harmony 2 even without cache or shared state. The launcher still selects against the fixed owner fingerprint, and dependency admission receives no profile. The wiring test searches for source strings and never demonstrates matching baseline admission."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The common and normal-player observer paths compare the standalone measurement with the receipt, including version, package and DLL hashes, MVID and entries. The controller gate exercised stale observer fields, a version-only mismatch and matching controls."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "The acquisition helper and both standalone validators use the same side-effect-free parser. The gate records refusal of missing, duplicate, mismapped and minimum-disagreeing definitions for both ZIPs."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "Output replacement, source edits and tiered Full refusal now have passing regressions. Read-only enumeration confirmed that the build bootstrap, local props and root manifest are absent from the stamp. Direct Test.ps1 reuse also lacks the check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "The compiler check obtains hashes from parsed records. The direct-build record shows successful compilation with reordered JSON properties and refusal of altered cache bytes."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F2",
      "rationale": "The reuse step and its prerequisite now enter the plan and initial estimate. An existing DLL is still printed as reused when that same plan reports the receipt missing or stale."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F3",
      "rationale": "The restored inventory has the identical Git blob a9ef6700253a028c25436ead1648e368f9900036 from 215d68fe. It contains the full newer profiles and the owner's extra Harmony assembly. The index distinguishes producer revisions and reused audits."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F4",
      "rationale": "The fixture uses Package.ps1 -IncludeRemovalObserver to produce both baseline packages. The candidate-bound gate confirms the combined route and subsequent standalone positive and negative cases."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F5",
      "rationale": "Both entry-name helpers dispose their ZipArchive in finally. This fixes the identified handle leak by inspection. The gate did not separately observe removal of the temporary directory."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r03-735731",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and the removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Both standalone ZIP validators inspect the packaged DLL. Matching DLL hashes bind the static audits to the packaged artifacts on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4. All four native rows and the combined-stack check remain NOT RUN.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Human gameplay acceptance and any decision to authorize a future guarded native qualification environment."
    ],
    "scope_reviewed": "Mounted Combat draft PR #62 at the fixed final candidate, including its separately shipped observer, all eleven pending findings and the directly affected build, reuse, launcher and evidence paths.",
    "unresolved_questions": [
      "The supplied materials do not identify the authorizing assignment for the reported 15-minute allowance or show the current draft-PR validation text."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
