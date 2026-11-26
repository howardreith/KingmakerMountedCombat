<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r07-review part=1/1 sha256=243bb30e7cde6c4b5227f307107c24c19b33a9ae3a0c2f48f66b07a66452f604 -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `85d18da514`

**Decision:** `changes_required` — The UMM baseline is substantiated for both shipping DLLs, and all three pending findings are fixed. Two defects remain in Full-tier budget admission. A failed test run can leave an oversized retry eligible, and a successful reuse run can be counted twice for the time spent checking its receipt.

## Continuation for Claude

Keep the correction within Full-tier timing, planning and the existing disposable fixture. Distinguish a receipt refusal before testing from a failure after work ran. Preserve the measured cost of the latter for retry admission. Keep the receipt verdict visible in the plan and count its cost once. Retain the single scan, actual entry charge and both verified stale-receipt cases. The requested evidence can be produced with stub children under the existing validation and review limits. The compiler pins, packages, native launcher and gameplay are outside this correction.

#### Findings

**R7-F1** · P2 · defect · blocks readiness — Retain the measured cost when Full reaches a test and fails
- Location: `scripts/Test-Tiers.ps1:213`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: An admitted Full invocation reaches a child test and fails after taking longer than its previous Full measurement.
- Expected: Charge the failed entry and make the measured cost of work that ran available to the next admission decision. A refusal at the receipt check should still leave Full history untouched.
- Actual: Line 213 sends only a heartbeat for every failed Full step. In the seeded comparison, a 700-second failure left the prior 600-second estimate in place and allowed a retry with 650 seconds remaining. The merge-base code recorded 700 seconds and refused that retry.
- Why it blocks / does not: The round-7 change weakens the existing safeguard against starting known oversized work. It affects Full runs with a build, where receipt refusal cannot occur.
- Requested correction: Distinguish receipt-only refusal from a Full run that reached a test and failed. Preserve the latter's measured cost for budgeting.
- Requested evidence: Use the existing disposable fixture to fail a child after it records that it started. Show the failed charge and refusal of an identical retry whose remaining allowance is below the measured cost. Retain the empty-history and seeded stale-receipt controls.

**R7-F2** · P2 · defect · blocks readiness — Count the receipt scan once in Full reuse estimates
- Location: `scripts/Test-Tiers.ps1:118`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: A budget has a standalone receipt duration from Fast or Candidate and a successful Full -SkipBuild duration that includes its own receipt scan.
- Expected: Display the receipt's eligibility and use the inclusive Full measurement once for the repeat invocation.
- Actual: Line 118 adds a separate timed receipt item to the Full plan, and line 139 sums both histories. A 600-second Full reuse measurement already includes its scan. With a separate 3-second receipt measurement, admission asks for 603 seconds and refuses a 602-second remainder.
- Why it blocks / does not: Moving the UMM receipt scan inside Full introduced a duplicate cost in the estimate. The selector can refuse an explicitly selected run that fits its recorded allowance.
- Requested correction: Keep the Full receipt prerequisite visible while excluding its standalone estimate when the timed Full step already includes it. Fast and Candidate still execute their separately timed receipt step.
- Requested evidence: Seed the disposable budget with both timings and an allowance between the inclusive and double-counted values. Verify the Full plan, admission to the stub runner with one scan, and continued refusal below the inclusive duration.

#### Prior finding assessments

- **R1-F8** — `fix_verified` — The shared root and observer-only tools comparisons now use case-insensitive exact Git pathspecs. The frozen fixture recorded mixed-case observer refusal, main-only acceptance, restored observer eligibility after removal, and refusal for the mixed-case root file.
- **R6-F1** — `fix_verified` — The fixture copies the real receipt helper, excludes it from tracing stubs and prepares a current Debug receipt. Its existing plan, Candidate and Full forwarding assertions completed in the recorded 29/29 run.
- **R6-F2** — `fix_verified` — The H gate observed one scan, an untouched runner and a failed two-second charge on the first stale Full receipt. The next plan showed an unknown Full duration. The seeded control preserved 600 seconds and refused the shorter allowance. R7-F1 concerns a failure after testing has started.

#### Evidence assessment

- **Inspected:** The controller mission, OWNER AMENDMENT 1, prior ledger, developer report, transcript notice, candidate-bound gate receipt and log.; AGENTS.md, validation policy, current handoffs, the complete M..H feature diff and the round-7 changes since 1384140e.; Both shipping projects and manifests, compiler guards, package records, loader-profile inventory, frozen-source comparisons, receipt helper, tier selector, budget reader and affected fixtures.
- **Executed verification:** The controller receipt binds the focused Fast gate to 85d18da5. Its log SHA-256 matched. The gate passed in 244.4 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 58 package and reuse checks. Both shipping builds passed the compiler-input guard.; The exported round-7 runs recorded 41/41 frozen-source checks and 29/29 tier-selection checks on the batch attributed to 53c987fa. Git shows only documentation and evidence changes between that commit and H.; All 30 indexed evidence blobs matched their exported SHA-256 values. For each product, the package manifest, standalone measurement and static audit agree on DLL hash and MVID. The recorded audits resolved all 42 main-mod member references and all 20 observer member references on each of four loader profiles.; Read-only PowerShell probes exercised the duration-write and allowance conditions with seeded timings and an in-memory recorder. For a 700-second failure after a prior 600-second measurement, M supplied 700 seconds and H retained 600. A separate probe summed a 600-second inclusive Full measurement and a 3-second standalone scan to 603 seconds.
- **Source inspection only:** Traced the Full child exit through Invoke-Tiered, the history update and the next admission estimate. The read-only probes exercised those decisions in isolation.; Inspected the owner-profile wiring through dependency admission, inventory, result reading and receipts. Runtime behavior remains unobserved.
- **Required gaps:** Show that a Full failure after the runner starts retains its measured cost for the next admission decision.; Show that Full reuse planning and admission count an inclusive Full duration once when a standalone receipt duration is also present.
- **Limitations:** The controller withheld the mixed foreground transcript. The two direct fixture logs describe the precommit round-7 batch. The controller gate ran at H.; Native checks on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4, and the combined-stack check, are NOT RUN. The guarded launcher stages into the live installation, and the repository has no approved disposable game-copy harness.; The retained Test-Harness result is 280 passed and three documented baseline failures. The tier-selection fixture used stub children. The review probes used synthetic durations.; The full diff whitespace check flagged four trailing spaces in wrapped warnings in the exported tier-selection log. The check passed outside the evidence directory.

#### Summary

- **Delivered behavior:** Mounted Combat and its removal observer declare UMM 0.31.1 and compile against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Both standalone validators passed on genuine packages. The four-loader compatibility evidence is static.
- **Scope reviewed:** Mounted Combat draft PR #62 at the fixed candidate, under the original mission and OWNER AMENDMENT 1. Package and audit evidence remains attached to the frozen DLL identities from product source 11df053b.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Any further controller round or allowance beyond the recorded limits. The gate ended with 2.75 minutes remaining.; Integration of draft PR #62 with current main, where the developer reports a conflict, and authorization of any future native qualification.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a215c5-bfe2-7f90-bfd8-fc69396a833e`, duration 841.3s, response sha256 `18c7674ac8667b26dcd52cea17836efe06528a8cf713b781e7e6bc7ddd19131d`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r07-4f34ef`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "85d18da51490e4e78bc2ef4c1fbc58b658b75057",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The UMM baseline is substantiated for both shipping DLLs, and all three pending findings are fixed. Two defects remain in Full-tier budget admission. A failed test run can leave an oversized retry eligible, and a successful reuse run can be counted twice for the time spent checking its receipt.\n\n## Continuation for Claude\n\nKeep the correction within Full-tier timing, planning and the existing disposable fixture. Distinguish a receipt refusal before testing from a failure after work ran. Preserve the measured cost of the latter for retry admission. Keep the receipt verdict visible in the plan and count its cost once. Retain the single scan, actual entry charge and both verified stale-receipt cases. The requested evidence can be produced with stub children under the existing validation and review limits. The compiler pins, packages, native launcher and gameplay are outside this correction.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the focused Fast gate to 85d18da5. Its log SHA-256 matched. The gate passed in 244.4 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 58 package and reuse checks. Both shipping builds passed the compiler-input guard.",
      "The exported round-7 runs recorded 41/41 frozen-source checks and 29/29 tier-selection checks on the batch attributed to 53c987fa. Git shows only documentation and evidence changes between that commit and H.",
      "All 30 indexed evidence blobs matched their exported SHA-256 values. For each product, the package manifest, standalone measurement and static audit agree on DLL hash and MVID. The recorded audits resolved all 42 main-mod member references and all 20 observer member references on each of four loader profiles.",
      "Read-only PowerShell probes exercised the duration-write and allowance conditions with seeded timings and an in-memory recorder. For a 700-second failure after a prior 600-second measurement, M supplied 700 seconds and H retained 600. A separate probe summed a 600-second inclusive Full measurement and a 3-second standalone scan to 603 seconds."
    ],
    "inspected": [
      "The controller mission, OWNER AMENDMENT 1, prior ledger, developer report, transcript notice, candidate-bound gate receipt and log.",
      "AGENTS.md, validation policy, current handoffs, the complete M..H feature diff and the round-7 changes since 1384140e.",
      "Both shipping projects and manifests, compiler guards, package records, loader-profile inventory, frozen-source comparisons, receipt helper, tier selector, budget reader and affected fixtures."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The two direct fixture logs describe the precommit round-7 batch. The controller gate ran at H.",
      "Native checks on 0.31.1, 0.32.4, owner-installed 0.33.0 and 0.33.4, and the combined-stack check, are NOT RUN. The guarded launcher stages into the live installation, and the repository has no approved disposable game-copy harness.",
      "The retained Test-Harness result is 280 passed and three documented baseline failures. The tier-selection fixture used stub children. The review probes used synthetic durations.",
      "The full diff whitespace check flagged four trailing spaces in wrapped warnings in the exported tier-selection log. The check passed outside the evidence directory."
    ],
    "required_gaps": [
      "Show that a Full failure after the runner starts retains its measured cost for the next admission decision.",
      "Show that Full reuse planning and admission count an inclusive Full duration once when a standalone receipt duration is also present."
    ],
    "source_inspection_only": [
      "Traced the Full child exit through Invoke-Tiered, the history update and the next admission estimate. The read-only probes exercised those decisions in isolation.",
      "Inspected the owner-profile wiring through dependency admission, inventory, result reading and receipts. Runtime behavior remains unobserved."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "Line 213 sends only a heartbeat for every failed Full step. In the seeded comparison, a 700-second failure left the prior 600-second estimate in place and allowed a retry with 650 seconds remaining. The merge-base code recorded 700 seconds and refused that retry.",
      "blocking_rationale": "The round-7 change weakens the existing safeguard against starting known oversized work. It affects Full runs with a build, where receipt refusal cannot occur.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Charge the failed entry and make the measured cost of work that ran available to the next admission decision. A refusal at the receipt check should still leave Full history untouched.",
      "id": "R7-F1",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 213
      },
      "priority": "P2",
      "requested_correction": "Distinguish receipt-only refusal from a Full run that reached a test and failed. Preserve the latter's measured cost for budgeting.",
      "requested_evidence": "Use the existing disposable fixture to fail a child after it records that it started. Show the failed charge and refusal of an identical retry whose remaining allowance is below the measured cost. Retain the empty-history and seeded stale-receipt controls.",
      "title": "Retain the measured cost when Full reaches a test and fails",
      "trigger": "An admitted Full invocation reaches a child test and fails after taking longer than its previous Full measurement."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "Line 118 adds a separate timed receipt item to the Full plan, and line 139 sums both histories. A 600-second Full reuse measurement already includes its scan. With a separate 3-second receipt measurement, admission asks for 603 seconds and refuses a 602-second remainder.",
      "blocking_rationale": "Moving the UMM receipt scan inside Full introduced a duplicate cost in the estimate. The selector can refuse an explicitly selected run that fits its recorded allowance.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Display the receipt's eligibility and use the inclusive Full measurement once for the repeat invocation.",
      "id": "R7-F2",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 118
      },
      "priority": "P2",
      "requested_correction": "Keep the Full receipt prerequisite visible while excluding its standalone estimate when the timed Full step already includes it. Fast and Candidate still execute their separately timed receipt step.",
      "requested_evidence": "Seed the disposable budget with both timings and an allowance between the inclusive and double-counted values. Verify the Full plan, admission to the stub runner with one scan, and continued refusal below the inclusive duration.",
      "title": "Count the receipt scan once in Full reuse estimates",
      "trigger": "A budget has a standalone receipt duration from Fast or Candidate and a successful Full -SkipBuild duration that includes its own receipt scan."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "The shared root and observer-only tools comparisons now use case-insensitive exact Git pathspecs. The frozen fixture recorded mixed-case observer refusal, main-only acceptance, restored observer eligibility after removal, and refusal for the mixed-case root file."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F1",
      "rationale": "The fixture copies the real receipt helper, excludes it from tracing stubs and prepares a current Debug receipt. Its existing plan, Candidate and Full forwarding assertions completed in the recorded 29/29 run."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "The H gate observed one scan, an untouched runner and a failed two-second charge on the first stale Full receipt. The next plan showed an unknown Full duration. The seeded control preserved 600 seconds and refused the shorter allowance. R7-F1 concerns a failure after testing has started."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r07-4f34ef",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its removal observer declare UMM 0.31.1 and compile against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Both standalone validators passed on genuine packages. The four-loader compatibility evidence is static.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Any further controller round or allowance beyond the recorded limits. The gate ended with 2.75 minutes remaining.",
      "Integration of draft PR #62 with current main, where the developer reports a conflict, and authorization of any future native qualification."
    ],
    "scope_reviewed": "Mounted Combat draft PR #62 at the fixed candidate, under the original mission and OWNER AMENDMENT 1. Package and audit evidence remains attached to the frozen DLL identities from product source 11df053b.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
