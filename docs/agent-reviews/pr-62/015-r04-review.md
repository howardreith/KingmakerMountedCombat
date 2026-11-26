<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r04-review part=1/1 sha256=0498a206a49d7d57b968ba4e29ae5203bb53837e8f21a410cfef2718edbff708 -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `13bf09d8e9`

**Decision:** `changes_required` — Both shipping products meet the UMM compile baseline, and the required gate passed. Three defects remain in the owner-profile and build-reuse paths. A separate package-building run also needs to be reconciled with the validation allowance.

## Continuation for Claude

First reconcile the standalone package runs against the existing authorization. The final gate took 211.5 seconds and the ledger reports 0.98 minutes remaining. Preserve the receipts and failed attempt. Any further gate needs admission under the existing limits.

Keep the corrections focused. Compare the retained owner profile with the Harmony 2 hash that dependency admission already measures. Include the repository-level MSBuild hook in local reuse decisions. Give a successful Full reuse invocation one admitted receipt check while preserving direct Test.ps1 protection. The requested evidence can use the existing disposable fixtures and stop before gameplay or the Full suite. Alternate-loader launch support and native qualification remain separate follow-up work under OWNER AMENDMENT 1.

#### Findings

**R1-F2** · P1 · defect · blocks readiness — Verify the owner profile's Harmony 2 hash before recording admission
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:1180`; acceptance: mission §7, OWNER AMENDMENT 1
- Trigger: A base-game target has the expected owner UMM and Harmony12 files but a different 0Harmony.dll.
- Expected: Before staging, the measured Harmony 2 dependency agrees with the owner profile named in the receipt.
- Actual: The launcher resolves the owner profile against the checked-in UMM fingerprint. Get-KmcBaseGameDependencies hashes the target's 0Harmony.dll, but its pin comparison covers only gameplay, UMM and Harmony12. Line 1180 writes the profile's expected Harmony 2 hash beside the independently measured dependency hash. The later inventory check runs after staging and is skipped for the normal-player scenario.
- Why it blocks / does not: This mission added the profile attribution to the admission receipt. The supported owner route can therefore record two different hashes for the same admitted dependency.
- Requested correction: Compare the already measured owner 0Harmony.dll hash with the retained profile during dependency admission and use that verified identity in the receipt. Keep the launcher owner-only.
- Requested evidence: Exercise the real admission helper with matching owner files, then change only Harmony 2. Show a consistent receipt for the first case and refusal before staging for the second. An offline fixture suffices.

**R1-F8** · P2 · defect · blocks readiness — Include the local repository-level compiler hook in the reuse receipt
- Location: `scripts/LoaderInputStamp.ps1:18`; acceptance: mission §5, mission §6
- Trigger: A developer adds a root Directory.Build.targets after the build and invokes -SkipBuild before adding the file to Git.
- Expected: Local reuse detects the presence, contents and removal of the repository-level MSBuild input that controls loader resolution.
- Actual: The stamp enumerates untracked inputs only under src, tools/KmcRemovalObserver and tests. Its repository-wide targets query lists tracked files only. Adding an untracked root Directory.Build.targets leaves the stamp current. Test-PinnedLoaderMsBuild.ps1 already demonstrates that this exact path can change the resolved UMM input and make a fresh build refuse both shipping projects.
- Why it blocks / does not: A supported -SkipBuild consumer can credit outputs under loader-reference settings that the direct compiler guard rejects.
- Requested correction: Bind the local receipt to the effective repository-root Directory.Build.targets independently of its Git tracking state.
- Requested evidence: In the existing disposable clone, start with a current receipt, add the existing root-hook fixture without staging it in Git, and show reuse refusal. Removing the hook should restore eligibility for the original outputs.

**R3-F2** · P2 · defect · blocks readiness — Avoid a second receipt scan on successful Full reuse
- Location: `scripts/Test.ps1:19`; acceptance: mission §6, docs/VALIDATION-POLICY.md
- Trigger: Test-Tiers.ps1 runs Full with -SkipBuild and a current receipt.
- Expected: Each admitted reuse invocation checks the receipt once before the first runner. Direct Test.ps1 reuse retains its guard.
- Actual: Test-Tiers.ps1 checks the receipt at line 247 and passes -SkipBuild to Test.ps1 at lines 250–251. Test.ps1 then hashes the same inputs and outputs again at line 19. Both scans are now metered. The recorded one-scan test uses Fast with stale inputs and exits at the first check.
- Why it blocks / does not: The direct-consumer correction reintroduces duplicate validation on a successful Full invocation. The one-scan criterion from R3-F2 remains unmet on that supported path.
- Requested correction: Coordinate the Full parent and direct Test.ps1 entry so one admitted check protects the reused outputs. Preserve the plan estimate, budget ordering and stale-output refusal.
- Requested evidence: Count scans with a current receipt through the Full parent and stop the disposable fixture at the first runner boundary. Retain the denied-budget and direct stale-receipt cases.

**R4-F1** · P2 · evidence_gap · blocks readiness — Account for package-building validation outside the recorded gates
- Location: `docs/evidence/umm-0311/INDEX.md:20`; acceptance: mission §4, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: The final handoff relies on the mission ledger to establish compliance with the validation allowance.
- Expected: The handoff accounts for build-bearing correction runs against the authorized validation allowance or identifies existing authority covering them separately.
- Actual: The dbe51d82 evidence records a distinct 51-case run that built packages. It falls between the failed gate and the final gate, outside the ledger entries. The developer reports 184 seconds for this direct run. Adding that duration alone to the recorded 14.02 minutes gives approximately 17.09 minutes against the 15-minute allowance.
- Why it blocks / does not: The gate result and its metered total are valid. The standalone check rebuilds shipping products, so the policy's allowance for invoking an external reader directly does not account for it.
- Requested correction: Reconcile the standalone package runs with the existing accounting and state the metered and additional validation time in the handoff. If the work exceeded its authorization, preserve the original ledger and record that for owner disposition.
- Requested evidence: Supply any existing admission record or separate authorization covering the standalone builds. The committed reports, timestamps and gate receipts suffice to document an uncovered run without repeating it.

#### Prior finding assessments

- **R1-F11** — `fix_verified` — CURRENT.md now ties the 205.2-second gate and its 1,488, 153 and 45 check counts to 293668a4. It records the failed correction gate separately. The controller supplied the passing receipt for the final candidate.
- **R1-F2** — `rebuttal_rejected` — OWNER AMENDMENT 1 permits dropping the non-owner launcher selection and deferring admission of other native profiles. The launcher does remove that option. The amendment retains responsibility for regressions introduced by the mission, including the new owner-profile receipt that can disagree with its own measured Harmony 2 dependency.
- **R1-F8** — `fix_rejected` — The gate verifies the new bootstrap, LocalGamePaths.props and main-manifest checks and direct Test.ps1 refusal. The same material-input omission persists for the untracked repository-root compiler hook used in the direct-MSBuild regression.
- **R2-F2** — `fix_verified` — Both plan lines now use the same computed receipt state. The candidate gate covered current, missing and stale receipts and checked that planning preserved the receipt.
- **R3-F1** — `fix_verified` — Frozen-source admission now compares LoaderReferences.Final.targets and scripts/LoaderPins.ps1 for both products. The 32-case record shows independent change and restoration for each, plus documentation-only reuse and the observer-only distinction.
- **R3-F2** — `fix_rejected` — The pre-admission scan is fixed. The gate observed zero scans on budget refusal and one on admitted Fast refusal. A current Full receipt still passes through two unconditional checks in the parent and child.
- **R3-F3** — `fix_verified` — The launcher's parameter block matches the merge base. The recorded parameter-metadata check confirms Scenario is positional argument zero and the removed profile option is absent.
- **R3-F4** — `fix_verified` — The exported budget record identifies the 15-minute allowance, quotes the owner assignment, records its SHA-256 and identifies the prior ledger. This establishes the requested provenance. R4-F1 concerns work outside those entries.
- **R3-F5** — `deferral_accepted` — The candidate gate's stale Full case stopped before running the suite. A fail-fast marker for a future regression remains an optional test improvement under OWNER AMENDMENT 1.

#### Evidence assessment

- **Inspected:** The original mission, OWNER AMENDMENT 1, repository instructions, validation policy, current handoff, prior findings, developer report and controller evidence.; Both shipping manifests and projects, reference acquisition and compiler checks, package validators, frozen and local reuse checks, launcher dependency admission, result readers and the applicable regression fixtures.
- **Executed verification:** The controller receipt binds the focused gate to 13bf09d8. I verified its log SHA-256. Fast passed 4/4 steps in 211.5 seconds with 1,488 component checks, 153 source checks and 51 focused checks.; The gate built both shipping projects and produced two disposable packages. Both standalone validators accepted their baseline package and rejected added dependencies. The main validator rejected the released UMM 0.33.0 DLL under the 0.31.1 manifest. Stale package receipts and the tested stale local outputs were refused.; The indexed records report 32/32 frozen-source cases and 117/117 offline loader-contract cases at d63d4cc0, plus 12/12 direct-MSBuild cases at 11df053b. The static audits resolved 14 types and 42 members for the mod and 8 types and 20 members for the observer on each of four loader profiles.; Read-only comparison matched the complete 4,197-line feature diff to Git and verified all 21 indexed evidence-blob digests. Each static audit's DLL hash and MVID match its package manifest and validator record. The production source, projects, pins and compiler guards match 11df053b.
- **Source inspection only:** I traced the differing sources of the two Harmony 2 hashes in the admission receipt, including the normal-player branch that skips the later native inventory check.; I traced local stamp input enumeration and the successful Full-to-Test.ps1 call path. The final gate's scan counter exercised a stale Fast receipt.
- **Required gaps:** R1-F2 needs a consistent pre-staging owner-profile receipt and a mixed-Harmony refusal at that boundary.; R1-F8 needs local reuse refusal for the repository-root MSBuild hook already exercised by the compiler tests.; R3-F2 needs a bounded observation of a current Full receipt passing exactly one scan before test execution.; R4-F1 needs existing accounting or owner disposition for the separately recorded package-building validation.
- **Limitations:** The foreground transcript was withheld because it mixed repositories. The final gate is independently controller-recorded. Earlier executions are attributed to their indexed records and producer commits.; The review environment was read-only. Native checks on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4, and combined-stack gameplay, remain NOT RUN. The compatibility evidence covers static references.; The earlier harness record reports 280 passes and three failures also present on its comparison baseline. The separate loader-contract checker records two unavailable historical inputs.

#### Summary

- **Delivered behavior:** Mounted Combat and its removal observer declare 0.31.1, compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, and validate emitted references at package admission. Their recorded static API checks pass on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4.
- **Scope reviewed:** Final correction review of the fixed Mounted Combat draft PR #62 candidate under OWNER AMENDMENT 1, including all nine pending findings and the consumers affected by their corrections.
- **Pending later-phase work:** none
- **Unresolved questions:** Which existing authorization, if any, covers the standalone package-building validation outside the mission ledger?
- **Remaining owner review:** Disposition of any uncovered validation spend and any decision about additional allowance or review rounds.; Human gameplay acceptance and any separately authorized native qualification.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a20f90-00cf-7121-a0c3-25b5d597079c`, duration 675.9s, response sha256 `ad1c9e97d2fea97885f1545c13cfadf7792eb8f7c99ac13c214b2f69618f30cf`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r04-542323`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "13bf09d8e90010f26a7a1758efa007455c1ba40d",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both shipping products meet the UMM compile baseline, and the required gate passed. Three defects remain in the owner-profile and build-reuse paths. A separate package-building run also needs to be reconciled with the validation allowance.\n\n## Continuation for Claude\n\nFirst reconcile the standalone package runs against the existing authorization. The final gate took 211.5 seconds and the ledger reports 0.98 minutes remaining. Preserve the receipts and failed attempt. Any further gate needs admission under the existing limits.\n\nKeep the corrections focused. Compare the retained owner profile with the Harmony 2 hash that dependency admission already measures. Include the repository-level MSBuild hook in local reuse decisions. Give a successful Full reuse invocation one admitted receipt check while preserving direct Test.ps1 protection. The requested evidence can use the existing disposable fixtures and stop before gameplay or the Full suite. Alternate-loader launch support and native qualification remain separate follow-up work under OWNER AMENDMENT 1.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the focused gate to 13bf09d8. I verified its log SHA-256. Fast passed 4/4 steps in 211.5 seconds with 1,488 component checks, 153 source checks and 51 focused checks.",
      "The gate built both shipping projects and produced two disposable packages. Both standalone validators accepted their baseline package and rejected added dependencies. The main validator rejected the released UMM 0.33.0 DLL under the 0.31.1 manifest. Stale package receipts and the tested stale local outputs were refused.",
      "The indexed records report 32/32 frozen-source cases and 117/117 offline loader-contract cases at d63d4cc0, plus 12/12 direct-MSBuild cases at 11df053b. The static audits resolved 14 types and 42 members for the mod and 8 types and 20 members for the observer on each of four loader profiles.",
      "Read-only comparison matched the complete 4,197-line feature diff to Git and verified all 21 indexed evidence-blob digests. Each static audit's DLL hash and MVID match its package manifest and validator record. The production source, projects, pins and compiler guards match 11df053b."
    ],
    "inspected": [
      "The original mission, OWNER AMENDMENT 1, repository instructions, validation policy, current handoff, prior findings, developer report and controller evidence.",
      "Both shipping manifests and projects, reference acquisition and compiler checks, package validators, frozen and local reuse checks, launcher dependency admission, result readers and the applicable regression fixtures."
    ],
    "limitations": [
      "The foreground transcript was withheld because it mixed repositories. The final gate is independently controller-recorded. Earlier executions are attributed to their indexed records and producer commits.",
      "The review environment was read-only. Native checks on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4, and combined-stack gameplay, remain NOT RUN. The compatibility evidence covers static references.",
      "The earlier harness record reports 280 passes and three failures also present on its comparison baseline. The separate loader-contract checker records two unavailable historical inputs."
    ],
    "required_gaps": [
      "R1-F2 needs a consistent pre-staging owner-profile receipt and a mixed-Harmony refusal at that boundary.",
      "R1-F8 needs local reuse refusal for the repository-root MSBuild hook already exercised by the compiler tests.",
      "R3-F2 needs a bounded observation of a current Full receipt passing exactly one scan before test execution.",
      "R4-F1 needs existing accounting or owner disposition for the separately recorded package-building validation."
    ],
    "source_inspection_only": [
      "I traced the differing sources of the two Harmony 2 hashes in the admission receipt, including the normal-player branch that skips the later native inventory check.",
      "I traced local stamp input enumeration and the successful Full-to-Test.ps1 call path. The final gate's scan counter exercised a stale Fast receipt."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a77",
        "OWNER AMENDMENT 1"
      ],
      "actual": "The launcher resolves the owner profile against the checked-in UMM fingerprint. Get-KmcBaseGameDependencies hashes the target's 0Harmony.dll, but its pin comparison covers only gameplay, UMM and Harmony12. Line 1180 writes the profile's expected Harmony 2 hash beside the independently measured dependency hash. The later inventory check runs after staging and is skipped for the normal-player scenario.",
      "blocking_rationale": "This mission added the profile attribution to the admission receipt. The supported owner route can therefore record two different hashes for the same admitted dependency.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Before staging, the measured Harmony 2 dependency agrees with the owner profile named in the receipt.",
      "id": "R1-F2",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 1180
      },
      "priority": "P1",
      "requested_correction": "Compare the already measured owner 0Harmony.dll hash with the retained profile during dependency admission and use that verified identity in the receipt. Keep the launcher owner-only.",
      "requested_evidence": "Exercise the real admission helper with matching owner files, then change only Harmony 2. Show a consistent receipt for the first case and refusal before staging for the second. An offline fixture suffices.",
      "title": "Verify the owner profile's Harmony 2 hash before recording admission",
      "trigger": "A base-game target has the expected owner UMM and Harmony12 files but a different 0Harmony.dll."
    },
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The stamp enumerates untracked inputs only under src, tools/KmcRemovalObserver and tests. Its repository-wide targets query lists tracked files only. Adding an untracked root Directory.Build.targets leaves the stamp current. Test-PinnedLoaderMsBuild.ps1 already demonstrates that this exact path can change the resolved UMM input and make a fresh build refuse both shipping projects.",
      "blocking_rationale": "A supported -SkipBuild consumer can credit outputs under loader-reference settings that the direct compiler guard rejects.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Local reuse detects the presence, contents and removal of the repository-level MSBuild input that controls loader resolution.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/LoaderInputStamp.ps1",
        "line": 18
      },
      "priority": "P2",
      "requested_correction": "Bind the local receipt to the effective repository-root Directory.Build.targets independently of its Git tracking state.",
      "requested_evidence": "In the existing disposable clone, start with a current receipt, add the existing root-hook fixture without staging it in Git, and show reuse refusal. Removing the hook should restore eligibility for the original outputs.",
      "title": "Include the local repository-level compiler hook in the reuse receipt",
      "trigger": "A developer adds a root Directory.Build.targets after the build and invokes -SkipBuild before adding the file to Git."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md"
      ],
      "actual": "Test-Tiers.ps1 checks the receipt at line 247 and passes -SkipBuild to Test.ps1 at lines 250\u2013251. Test.ps1 then hashes the same inputs and outputs again at line 19. Both scans are now metered. The recorded one-scan test uses Fast with stale inputs and exits at the first check.",
      "blocking_rationale": "The direct-consumer correction reintroduces duplicate validation on a successful Full invocation. The one-scan criterion from R3-F2 remains unmet on that supported path.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each admitted reuse invocation checks the receipt once before the first runner. Direct Test.ps1 reuse retains its guard.",
      "id": "R3-F2",
      "location": {
        "file": "scripts/Test.ps1",
        "line": 19
      },
      "priority": "P2",
      "requested_correction": "Coordinate the Full parent and direct Test.ps1 entry so one admitted check protects the reused outputs. Preserve the plan estimate, budget ordering and stale-output refusal.",
      "requested_evidence": "Count scans with a current receipt through the Full parent and stop the disposable fixture at the first runner boundary. Retain the denied-budget and direct stale-receipt cases.",
      "title": "Avoid a second receipt scan on successful Full reuse",
      "trigger": "Test-Tiers.ps1 runs Full with -SkipBuild and a current receipt."
    },
    {
      "acceptance_ids": [
        "mission \u00a74",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "The dbe51d82 evidence records a distinct 51-case run that built packages. It falls between the failed gate and the final gate, outside the ledger entries. The developer reports 184 seconds for this direct run. Adding that duration alone to the recorded 14.02 minutes gives approximately 17.09 minutes against the 15-minute allowance.",
      "blocking_rationale": "The gate result and its metered total are valid. The standalone check rebuilds shipping products, so the policy's allowance for invoking an external reader directly does not account for it.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The handoff accounts for build-bearing correction runs against the authorized validation allowance or identifies existing authority covering them separately.",
      "id": "R4-F1",
      "location": {
        "file": "docs/evidence/umm-0311/INDEX.md",
        "line": 20
      },
      "priority": "P2",
      "requested_correction": "Reconcile the standalone package runs with the existing accounting and state the metered and additional validation time in the handoff. If the work exceeded its authorization, preserve the original ledger and record that for owner disposition.",
      "requested_evidence": "Supply any existing admission record or separate authorization covering the standalone builds. The committed reports, timestamps and gate receipts suffice to document an uncovered run without repeating it.",
      "title": "Account for package-building validation outside the recorded gates",
      "trigger": "The final handoff relies on the mission ledger to establish compliance with the validation allowance."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F11",
      "rationale": "CURRENT.md now ties the 205.2-second gate and its 1,488, 153 and 45 check counts to 293668a4. It records the failed correction gate separately. The controller supplied the passing receipt for the final candidate."
    },
    {
      "assessment": "rebuttal_rejected",
      "finding_id": "R1-F2",
      "rationale": "OWNER AMENDMENT 1 permits dropping the non-owner launcher selection and deferring admission of other native profiles. The launcher does remove that option. The amendment retains responsibility for regressions introduced by the mission, including the new owner-profile receipt that can disagree with its own measured Harmony 2 dependency."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The gate verifies the new bootstrap, LocalGamePaths.props and main-manifest checks and direct Test.ps1 refusal. The same material-input omission persists for the untracked repository-root compiler hook used in the direct-MSBuild regression."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "Both plan lines now use the same computed receipt state. The candidate gate covered current, missing and stale receipts and checked that planning preserved the receipt."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F1",
      "rationale": "Frozen-source admission now compares LoaderReferences.Final.targets and scripts/LoaderPins.ps1 for both products. The 32-case record shows independent change and restoration for each, plus documentation-only reuse and the observer-only distinction."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F2",
      "rationale": "The pre-admission scan is fixed. The gate observed zero scans on budget refusal and one on admitted Fast refusal. A current Full receipt still passes through two unconditional checks in the parent and child."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F3",
      "rationale": "The launcher's parameter block matches the merge base. The recorded parameter-metadata check confirms Scenario is positional argument zero and the removed profile option is absent."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F4",
      "rationale": "The exported budget record identifies the 15-minute allowance, quotes the owner assignment, records its SHA-256 and identifies the prior ledger. This establishes the requested provenance. R4-F1 concerns work outside those entries."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R3-F5",
      "rationale": "The candidate gate's stale Full case stopped before running the suite. A fail-fast marker for a future regression remains an optional test improvement under OWNER AMENDMENT 1."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r04-542323",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its removal observer declare 0.31.1, compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1, and validate emitted references at package admission. Their recorded static API checks pass on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition of any uncovered validation spend and any decision about additional allowance or review rounds.",
      "Human gameplay acceptance and any separately authorized native qualification."
    ],
    "scope_reviewed": "Final correction review of the fixed Mounted Combat draft PR #62 candidate under OWNER AMENDMENT 1, including all nine pending findings and the consumers affected by their corrections.",
    "unresolved_questions": [
      "Which existing authorization, if any, covers the standalone package-building validation outside the mission ledger?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
