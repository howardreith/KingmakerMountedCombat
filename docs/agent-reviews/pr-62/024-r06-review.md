<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r06-review part=1/1 sha256=837d076211f764fea5c46858527d9ea4a4d18477d4249414517707b7173a9099 -->
### Codex GPT-6.2 Astra — review — round 6 — candidate `1384140efa`

**Decision:** `changes_required` — Both shipping products pass the pinned build and package checks. Three issues block readiness. Archived observer reuse can miss an inherited MSBuild file on Windows, the existing tier-selection test replaces the receipt helper with a stub, and a first receipt refusal can create a Full-suite duration before the runner starts.

Continuation for Claude

Use Windows-compatible path matching for the inherited MSBuild files in frozen-source admission. Restore the real receipt helper and a current Debug receipt in the tier-selection fixture. Keep a pre-run receipt refusal chargeable while leaving Full history untouched. Verify these cases through the existing frozen-source, tier-selection and package-reuse fixtures. Keep the correction within these offline paths and preserve the single receipt scan. The ledger has 2.57 minutes remaining. Any additional allowance or review round requires owner/controller disposition.

#### Findings

**R1-F8** · P2 · defect · blocks readiness — Match inherited MSBuild files using Windows filename semantics
- Location: `scripts/runtime/FrozenProductSource.ps1:66`; acceptance: mission §5, mission §6
- Trigger: A clean descendant adds tools/Directory.Build.Targets after the observer package is frozen.
- Expected: Every Windows spelling of an inherited hook invalidates the affected archive. A tools-only hook preserves main-only reuse.
- Actual: FrozenProductSource.ps1 uses case-sensitive Git pathspecs for the observer's inherited files at line 66 and the shared root files at line 41. Windows resolves Directory.Build.Targets as Directory.Build.targets, while Git omits the differently spelled addition from these comparisons.
- Why it blocks / does not: An archived observer can remain eligible after a compiler hook changes the build inputs that its frozen DLL represents.
- Requested correction: Use case-insensitive exact Git pathspecs for the inherited Directory.Build.props and Directory.Build.targets names. Keep the tools paths in the observer-specific comparison.
- Requested evidence: Commit a mixed-case tools hook in the existing frozen fixture. Show observer refusal, main-only acceptance and restored observer eligibility after removal. Check the equivalent shared-root spelling at its existing boundary.

**R6-F1** · P2 · defect · blocks readiness — Restore the receipt helper in the tier-selection test fixture
- Location: `scripts/Test-TierSelection.ps1:38`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: The repository's workflow-runner focused test prepares its private fixture and invokes the selector with -Plan and -SkipBuild.
- Expected: The fixture loads the real receipt helper, and its prepared outputs allow the existing successful selector cases to reach their tracing children.
- Actual: The fixture turns every .ps1 literal in Test.ps1 into a tracing child. That includes the newly dot-sourced LoaderInputStamp.ps1. The stub writes a trace and defines none of the receipt functions. The first Candidate plan calls Test-KmcLoaderInputStamp and cannot produce its expected output.
- Why it blocks / does not: The repository's designated focused regression for Test-Tiers.ps1 and WorkflowBudget.ps1 can no longer exercise its plan, forwarding and budget assertions.
- Requested correction: Exclude the dot-sourced helper from child-stub generation, copy the real helper and prepare a current Debug receipt for the disposable outputs. Preserve the existing assertions.
- Requested evidence: Capture the result of the existing Test-TierSelection.ps1 check, including its side-effect-free plan and traced Candidate and Full cases.

**R6-F2** · P2 · defect · blocks readiness — Keep Full duration unknown after its first receipt refusal
- Location: `scripts/WorkflowBudget.ps1:464`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: A budget with no Full history admits Full -SkipBuild and the receipt is rejected before the runner.
- Expected: The failed attempt consumes its elapsed time. Full-suite duration stays unknown until there is a measurement of the suite.
- Actual: The failed-step condition suppresses a write only when an established duration exists. With empty history it stores the receipt-refusal time under FULL umbrella. A later Full plan with a build reports that value as the estimate for the entire suite.
- Why it blocks / does not: The planner and interruption reconciliation can use a preflight duration as the cost of a Full run whose duration has never been measured.
- Requested correction: Distinguish receipt refusal before the runner from a measured Full step when updating history. Preserve entry charging and the verified behavior for an established duration.
- Requested evidence: Start the existing stale-receipt fixture with empty Full history. Show one scan, a charged failed entry, an untouched runner and a subsequent Full plan with an unknown duration. Retain the seeded 600-second case.

#### Prior finding assessments

- **R1-F8** — `fix_rejected` — The local receipt and archived comparison cover the tested canonical spellings. The same inherited Windows file still escapes the archived comparison when Git records a different capitalization.
- **R5-F1** — `fix_verified` — The candidate gate demonstrated that the two-second refusal preserved the established 600-second duration and that the next admission used it. R6-F2 concerns the separate first-run case with no established duration.

#### Evidence assessment

- **Inspected:** The mission, OWNER AMENDMENT 1, repository validation policy, current handoffs, pending findings, developer report and controller evidence.; The full M..H feature diff and the round-6 correction against e7ec0ef8, including both shipping projects, compiler guards, package validators, reuse checks and their direct consumers.; The workflow impact map, Test-TierSelection.ps1 fixture, budget history and interruption logic, and the original artifact identities used for the compatibility claims.
- **Executed verification:** The controller receipt binds the focused Fast gate to H. Its log digest matches. The gate passed in 214.7 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 57 package and reuse checks.; The gate accepted both genuine packages and rejected extra dependencies, the released UMM 0.33.0 DLL with the lower manifest minimum, stale package receipts and changed observer hooks. Restoring the original inputs restored local reuse.; The gate recorded one scan and a two-second failed entry for a stale Full receipt. The seeded 600-second estimate survived. A later attempt with five minutes available was refused before any scan.; Read-only checks confirmed all 27 indexed evidence blobs match their published digests. The package manifests, validator outputs and static audits agree on each shipping DLL's SHA-256 and MVID.; A read-only Windows probe resolved both spellings of LoaderReferences.Final.targets. Git matched one change with the tracked spelling, zero with the case-varied spelling and one with an explicit case-insensitive pathspec.
- **Source inspection only:** The archived-observer consequence follows from the verified Git path matching and the pathspecs at FrozenProductSource.ps1 lines 41 and 66. The committed fixture log covers canonical spellings.; The tier-selection fixture failure follows from its child-stub generator and the selector's first plan call. The candidate gate did not select that fixture.; An in-memory evaluation confirmed that the history predicate selects a write when the prior estimate is absent. The subsequent Full plan and interruption effects were traced through source.
- **Required gaps:** Frozen observer evidence for a committed inherited hook whose Git spelling differs only in case.; A passing result from the existing tier-selection fixture with the real receipt dependency present.; A disposable-budget result showing that the first pre-run receipt refusal leaves Full duration unknown, followed by the required gate receipt for the corrected candidate within authorized limits.
- **Limitations:** The foreground transcript was withheld. The 37/0 frozen-source result is an exported log from the uncommitted round-6 code batch with a corresponding budget entry. The controller independently attests the gate on H.; Native checks and combined-stack testing were not run. The four loader rows provide static API evidence. The earlier general harness result retains its three baseline failures.; Reviewer verification was read-only. The ledger reports 28.05 of 30.62 minutes spent after the gate.

#### Summary

- **Delivered behavior:** Mounted Combat and its removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Static audits of the identified DLLs resolved their referenced loader members on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4.
- **Scope reviewed:** The final Mounted Combat checkpoint for draft PR #62 at the fixed candidate, under OWNER AMENDMENT 1.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Disposition of draft PR #62 and any additional review or validation allowance needed for correction.; Authorization for a future native qualification path and eventual human-play acceptance.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a21393-a1e4-7a31-9d39-27e6618e1634`, duration 904.8s, response sha256 `fe5ab035c9edf735d8c21dd89dbbd6d3bb92f420f3f9306a59f8e5011e597376`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r06-ad4004`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "1384140efa9ac9b1d8c24909c986a326694bb452",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both shipping products pass the pinned build and package checks. Three issues block readiness. Archived observer reuse can miss an inherited MSBuild file on Windows, the existing tier-selection test replaces the receipt helper with a stub, and a first receipt refusal can create a Full-suite duration before the runner starts.\n\nContinuation for Claude\n\nUse Windows-compatible path matching for the inherited MSBuild files in frozen-source admission. Restore the real receipt helper and a current Debug receipt in the tier-selection fixture. Keep a pre-run receipt refusal chargeable while leaving Full history untouched. Verify these cases through the existing frozen-source, tier-selection and package-reuse fixtures. Keep the correction within these offline paths and preserve the single receipt scan. The ledger has 2.57 minutes remaining. Any additional allowance or review round requires owner/controller disposition.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the focused Fast gate to H. Its log digest matches. The gate passed in 214.7 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 57 package and reuse checks.",
      "The gate accepted both genuine packages and rejected extra dependencies, the released UMM 0.33.0 DLL with the lower manifest minimum, stale package receipts and changed observer hooks. Restoring the original inputs restored local reuse.",
      "The gate recorded one scan and a two-second failed entry for a stale Full receipt. The seeded 600-second estimate survived. A later attempt with five minutes available was refused before any scan.",
      "Read-only checks confirmed all 27 indexed evidence blobs match their published digests. The package manifests, validator outputs and static audits agree on each shipping DLL's SHA-256 and MVID.",
      "A read-only Windows probe resolved both spellings of LoaderReferences.Final.targets. Git matched one change with the tracked spelling, zero with the case-varied spelling and one with an explicit case-insensitive pathspec."
    ],
    "inspected": [
      "The mission, OWNER AMENDMENT 1, repository validation policy, current handoffs, pending findings, developer report and controller evidence.",
      "The full M..H feature diff and the round-6 correction against e7ec0ef8, including both shipping projects, compiler guards, package validators, reuse checks and their direct consumers.",
      "The workflow impact map, Test-TierSelection.ps1 fixture, budget history and interruption logic, and the original artifact identities used for the compatibility claims."
    ],
    "limitations": [
      "The foreground transcript was withheld. The 37/0 frozen-source result is an exported log from the uncommitted round-6 code batch with a corresponding budget entry. The controller independently attests the gate on H.",
      "Native checks and combined-stack testing were not run. The four loader rows provide static API evidence. The earlier general harness result retains its three baseline failures.",
      "Reviewer verification was read-only. The ledger reports 28.05 of 30.62 minutes spent after the gate."
    ],
    "required_gaps": [
      "Frozen observer evidence for a committed inherited hook whose Git spelling differs only in case.",
      "A passing result from the existing tier-selection fixture with the real receipt dependency present.",
      "A disposable-budget result showing that the first pre-run receipt refusal leaves Full duration unknown, followed by the required gate receipt for the corrected candidate within authorized limits."
    ],
    "source_inspection_only": [
      "The archived-observer consequence follows from the verified Git path matching and the pathspecs at FrozenProductSource.ps1 lines 41 and 66. The committed fixture log covers canonical spellings.",
      "The tier-selection fixture failure follows from its child-stub generator and the selector's first plan call. The candidate gate did not select that fixture.",
      "An in-memory evaluation confirmed that the history predicate selects a write when the prior estimate is absent. The subsequent Full plan and interruption effects were traced through source."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "FrozenProductSource.ps1 uses case-sensitive Git pathspecs for the observer's inherited files at line 66 and the shared root files at line 41. Windows resolves Directory.Build.Targets as Directory.Build.targets, while Git omits the differently spelled addition from these comparisons.",
      "blocking_rationale": "An archived observer can remain eligible after a compiler hook changes the build inputs that its frozen DLL represents.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every Windows spelling of an inherited hook invalidates the affected archive. A tools-only hook preserves main-only reuse.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/runtime/FrozenProductSource.ps1",
        "line": 66
      },
      "priority": "P2",
      "requested_correction": "Use case-insensitive exact Git pathspecs for the inherited Directory.Build.props and Directory.Build.targets names. Keep the tools paths in the observer-specific comparison.",
      "requested_evidence": "Commit a mixed-case tools hook in the existing frozen fixture. Show observer refusal, main-only acceptance and restored observer eligibility after removal. Check the equivalent shared-root spelling at its existing boundary.",
      "title": "Match inherited MSBuild files using Windows filename semantics",
      "trigger": "A clean descendant adds tools/Directory.Build.Targets after the observer package is frozen."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "The fixture turns every .ps1 literal in Test.ps1 into a tracing child. That includes the newly dot-sourced LoaderInputStamp.ps1. The stub writes a trace and defines none of the receipt functions. The first Candidate plan calls Test-KmcLoaderInputStamp and cannot produce its expected output.",
      "blocking_rationale": "The repository's designated focused regression for Test-Tiers.ps1 and WorkflowBudget.ps1 can no longer exercise its plan, forwarding and budget assertions.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The fixture loads the real receipt helper, and its prepared outputs allow the existing successful selector cases to reach their tracing children.",
      "id": "R6-F1",
      "location": {
        "file": "scripts/Test-TierSelection.ps1",
        "line": 38
      },
      "priority": "P2",
      "requested_correction": "Exclude the dot-sourced helper from child-stub generation, copy the real helper and prepare a current Debug receipt for the disposable outputs. Preserve the existing assertions.",
      "requested_evidence": "Capture the result of the existing Test-TierSelection.ps1 check, including its side-effect-free plan and traced Candidate and Full cases.",
      "title": "Restore the receipt helper in the tier-selection test fixture",
      "trigger": "The repository's workflow-runner focused test prepares its private fixture and invokes the selector with -Plan and -SkipBuild."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "The failed-step condition suppresses a write only when an established duration exists. With empty history it stores the receipt-refusal time under FULL umbrella. A later Full plan with a build reports that value as the estimate for the entire suite.",
      "blocking_rationale": "The planner and interruption reconciliation can use a preflight duration as the cost of a Full run whose duration has never been measured.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The failed attempt consumes its elapsed time. Full-suite duration stays unknown until there is a measurement of the suite.",
      "id": "R6-F2",
      "location": {
        "file": "scripts/WorkflowBudget.ps1",
        "line": 464
      },
      "priority": "P2",
      "requested_correction": "Distinguish receipt refusal before the runner from a measured Full step when updating history. Preserve entry charging and the verified behavior for an established duration.",
      "requested_evidence": "Start the existing stale-receipt fixture with empty Full history. Show one scan, a charged failed entry, an untouched runner and a subsequent Full plan with an unknown duration. Retain the seeded 600-second case.",
      "title": "Keep Full duration unknown after its first receipt refusal",
      "trigger": "A budget with no Full history admits Full -SkipBuild and the receipt is rejected before the runner."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The local receipt and archived comparison cover the tested canonical spellings. The same inherited Windows file still escapes the archived comparison when Git records a different capitalization."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F1",
      "rationale": "The candidate gate demonstrated that the two-second refusal preserved the established 600-second duration and that the next admission used it. R6-F2 concerns the separate first-run case with no established duration."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r06-ad4004",
  "round": 6,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Static audits of the identified DLLs resolved their referenced loader members on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition of draft PR #62 and any additional review or validation allowance needed for correction.",
      "Authorization for a future native qualification path and eventual human-play acceptance."
    ],
    "scope_reviewed": "The final Mounted Combat checkpoint for draft PR #62 at the fixed candidate, under OWNER AMENDMENT 1.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
