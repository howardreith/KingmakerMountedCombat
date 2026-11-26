<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r08-review part=1/1 sha256=d08e3cabf4662a11e2e45f47fa9ebd7a837b26263d6746b908e3076f600fae91 -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `bc38080d5e`

**Decision:** `changes_required` — Both round-7 findings are fixed. One budget regression blocks readiness. When Full has no timing history, the selector can start a mandatory receipt scan whose recorded duration exceeds the remaining allowance.

Continuation for Claude

Limit the correction to Full -SkipBuild admission. When the Full duration is unknown, use the recorded receipt duration as a lower bound for selection and step admission. Keep the Full total unknown until measured, and count an established inclusive Full duration once. Exercise the existing denied-budget fixture through Full and capture refusal before any scan or runner start. The existing focused gate, review cap and time allowance govern any further validation.

#### Findings

**R8-F1** · P2 · defect · blocks readiness — Preserve the measured receipt cost on the first Full reuse run
- Location: `scripts/Test-Tiers.ps1:138`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: Fast or Candidate has measured the receipt scan. Full -SkipBuild is selected before any Full duration has been recorded, with less time remaining than the scan's measured cost.
- Expected: The Full total stays unknown. Its measured receipt cost supplies a lower bound, so admission refuses the run before hashing.
- Actual: Line 138 filters out the receipt step for every Full run. With the existing fixture-denied values of 600 seconds for the receipt and 60 seconds remaining, both admission checks receive an unknown cost. The gate exercises this fixture only through Fast.
- Why it blocks / does not: The round-8 change drops a known cost that the previous selector supplied to admission. It permits the Full child to begin work already estimated to exceed its allowance.
- Requested correction: Use the standalone receipt duration as the admission lower bound for Full -SkipBuild when the inclusive Full duration is unavailable. Apply it at selection and step admission without recording it as Full history.
- Requested evidence: Run the existing bounded Full fixture with a current receipt, a 600-second receipt measurement, no Full history and 60 seconds available. Show an oversized-work refusal, zero scans and zero runner starts. Retain the existing 270-second admission and 180-second refusal controls.

#### Prior finding assessments

- **R7-F1** — `fix_verified` — The gate recorded 3.4 seconds of Full work and charged the failed entry 4 seconds. A retry with 2 seconds available was refused before a second scan or start. Receipt-only refusals preserved both empty history and a seeded 600-second duration.
- **R7-F2** — `fix_verified` — With a 120-second receipt and a 200-second inclusive Full measurement, the plan reported 200 seconds. The 270-second allowance admitted one scan and the 180-second allowance refused before scanning. Fast and Candidate retained their separate 120-second estimate. R8-F1 concerns the distinct case with no Full measurement.

#### Evidence assessment

- **Inspected:** The mission, OWNER AMENDMENT 1, repository validation policy, prior findings, developer report, complete feature diff and round-8 changes from 85d18da5.; The controller gate receipt and log, disposable budget fixtures, tier-selection and plan logs, and budget ledger.; Both shipping manifests, compiler guards, standalone validators, frozen package identities and static API audits. The audited DLL hashes match the package manifests at product source 11df053b.
- **Executed verification:** The controller receipt binds the focused gate to bc38080d. I verified the log SHA-256 against the receipt. The gate exited 0 in 259.3 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 64 package and reuse checks passing.; The gate built both shipping products, accepted both genuine packages and rejected the released UMM 0.33.0 DLL under the lowered manifest, extra dependency DLLs and mismatched receipts.; I evaluated the candidate's estimate calculation and oversized-work comparisons in memory with a 600-second receipt measurement, no Full measurement and 60 seconds remaining. Selection and step estimates were both null. Neither comparison refused the work.
- **Source inspection only:** I traced the estimate into both admission calls and checked the prior selector's known-cost calculation. The reviewer probe exercised the arithmetic and predicates only.; I inspected the installed 0.33.0 profile checks and propagation into dependency admission and result readers. The four-profile compatibility evidence covers static member resolution.
- **Required gaps:** R8-F1 requires a Full reuse refusal before hashing when the receipt scan is the only measured cost and exceeds the available time.
- **Limitations:** The foreground transcript was withheld. The controller receipt confirms the gate at H. The separate 29/29 tier-selection log belongs to the round-8 code before commit.; The Full cases used an instrumented child. The earlier broader harness record retains three failures also observed on its comparison baseline.; Native checks remain NOT RUN on all four loader profiles because Mounted lacks an approved disposable game-copy harness. The combined mod stack was also untested.

#### Summary

- **Delivered behavior:** Mounted Combat and its removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Their packaged DLLs have static API evidence on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4.
- **Scope reviewed:** Final correction review of Mounted Combat draft PR #62 at the fixed candidate, including both pending findings and the directly affected Full admission paths.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize another review round and sufficient validation time. The gate closed at 37.79 of 40.62 minutes, leaving 2.83 minutes.; Disposition of PR #62's integration with the updated main branch and any later native or human-play qualification.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a215f4-628d-7921-807a-07ac62fc38be`, duration 539.6s, response sha256 `8cfe2dcba1438eed591fb8a9c3bd01c8af10f6d3b589b46a095b45aaa024b5aa`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r08-af6281`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "bc38080d5e634f43d911202b4ddf7534dbdca3e1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both round-7 findings are fixed. One budget regression blocks readiness. When Full has no timing history, the selector can start a mandatory receipt scan whose recorded duration exceeds the remaining allowance.\n\nContinuation for Claude\n\nLimit the correction to Full -SkipBuild admission. When the Full duration is unknown, use the recorded receipt duration as a lower bound for selection and step admission. Keep the Full total unknown until measured, and count an established inclusive Full duration once. Exercise the existing denied-budget fixture through Full and capture refusal before any scan or runner start. The existing focused gate, review cap and time allowance govern any further validation.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the focused gate to bc38080d. I verified the log SHA-256 against the receipt. The gate exited 0 in 259.3 seconds with 4/4 steps, 1,488 component checks, 153 source checks and 64 package and reuse checks passing.",
      "The gate built both shipping products, accepted both genuine packages and rejected the released UMM 0.33.0 DLL under the lowered manifest, extra dependency DLLs and mismatched receipts.",
      "I evaluated the candidate's estimate calculation and oversized-work comparisons in memory with a 600-second receipt measurement, no Full measurement and 60 seconds remaining. Selection and step estimates were both null. Neither comparison refused the work."
    ],
    "inspected": [
      "The mission, OWNER AMENDMENT 1, repository validation policy, prior findings, developer report, complete feature diff and round-8 changes from 85d18da5.",
      "The controller gate receipt and log, disposable budget fixtures, tier-selection and plan logs, and budget ledger.",
      "Both shipping manifests, compiler guards, standalone validators, frozen package identities and static API audits. The audited DLL hashes match the package manifests at product source 11df053b."
    ],
    "limitations": [
      "The foreground transcript was withheld. The controller receipt confirms the gate at H. The separate 29/29 tier-selection log belongs to the round-8 code before commit.",
      "The Full cases used an instrumented child. The earlier broader harness record retains three failures also observed on its comparison baseline.",
      "Native checks remain NOT RUN on all four loader profiles because Mounted lacks an approved disposable game-copy harness. The combined mod stack was also untested."
    ],
    "required_gaps": [
      "R8-F1 requires a Full reuse refusal before hashing when the receipt scan is the only measured cost and exceeds the available time."
    ],
    "source_inspection_only": [
      "I traced the estimate into both admission calls and checked the prior selector's known-cost calculation. The reviewer probe exercised the arithmetic and predicates only.",
      "I inspected the installed 0.33.0 profile checks and propagation into dependency admission and result readers. The four-profile compatibility evidence covers static member resolution."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "Line 138 filters out the receipt step for every Full run. With the existing fixture-denied values of 600 seconds for the receipt and 60 seconds remaining, both admission checks receive an unknown cost. The gate exercises this fixture only through Fast.",
      "blocking_rationale": "The round-8 change drops a known cost that the previous selector supplied to admission. It permits the Full child to begin work already estimated to exceed its allowance.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The Full total stays unknown. Its measured receipt cost supplies a lower bound, so admission refuses the run before hashing.",
      "id": "R8-F1",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 138
      },
      "priority": "P2",
      "requested_correction": "Use the standalone receipt duration as the admission lower bound for Full -SkipBuild when the inclusive Full duration is unavailable. Apply it at selection and step admission without recording it as Full history.",
      "requested_evidence": "Run the existing bounded Full fixture with a current receipt, a 600-second receipt measurement, no Full history and 60 seconds available. Show an oversized-work refusal, zero scans and zero runner starts. Retain the existing 270-second admission and 180-second refusal controls.",
      "title": "Preserve the measured receipt cost on the first Full reuse run",
      "trigger": "Fast or Candidate has measured the receipt scan. Full -SkipBuild is selected before any Full duration has been recorded, with less time remaining than the scan's measured cost."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F1",
      "rationale": "The gate recorded 3.4 seconds of Full work and charged the failed entry 4 seconds. A retry with 2 seconds available was refused before a second scan or start. Receipt-only refusals preserved both empty history and a seeded 600-second duration."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F2",
      "rationale": "With a 120-second receipt and a 200-second inclusive Full measurement, the plan reported 200 seconds. The 270-second allowance admitted one scan and the 180-second allowance refused before scanning. Fast and Candidate retained their separate 120-second estimate. R8-F1 concerns the distinct case with no Full measurement."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r08-af6281",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Their packaged DLLs have static API evidence on 0.31.1, 0.32.4, the owner's 0.33.0 and 0.33.4.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize another review round and sufficient validation time. The gate closed at 37.79 of 40.62 minutes, leaving 2.83 minutes.",
      "Disposition of PR #62's integration with the updated main branch and any later native or human-play qualification."
    ],
    "scope_reviewed": "Final correction review of Mounted Combat draft PR #62 at the fixed candidate, including both pending findings and the directly affected Full admission paths.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
