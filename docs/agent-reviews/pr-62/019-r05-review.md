<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r05-review part=1/1 sha256=928169c2abf224532d9c4b60266b7e7b35f797e39c48a29aed720cdb12858b87 -->
### Codex GPT-6.2 Astra — review — round 5 — candidate `e7ec0ef8cd`

**Decision:** `changes_required` — Both shipping products meet the declared and compiled UMM 0.31.1 baseline. Two validation defects remain. The removal observer can reuse old binaries after an inherited MSBuild input changes, and a rejected receipt can overwrite the budget estimate for the entire Full suite.

## Continuation for Claude

Include the observer’s inherited tools/Directory.Build.targets in local and archived observer reuse checks. Preserve main-only reuse when the change affects only the observer. Keep the single receipt scan on Full reuse, charge failed attempts, and preserve the recorded suite duration when the receipt is refused before the runner starts. Exercise both corrections in the existing disposable fixtures. The Full fixture can stop at its runner boundary. Keep the work within the existing focused gate, time allowance and controller round limits.

#### Findings

**R1-F8** · P2 · defect · blocks readiness — Include the observer’s inherited MSBuild hook in reuse checks
- Location: `scripts/LoaderInputStamp.ps1:20`; acceptance: mission §5, mission §6
- Trigger: A developer adds tools/Directory.Build.targets after building the observer. MSBuild finds it one directory above tools/KmcRemovalObserver, just as the existing compiler regression finds src/Directory.Build.targets above the main project.
- Expected: A change to the MSBuild hook inherited by the shipping observer invalidates its local outputs and archived observer reuse.
- Actual: The stamp explicitly hashes the root hook, but its untracked-file search starts at tools/KmcRemovalObserver. The repository-wide targets query includes only tracked files. Once the parent hook is committed, FrozenProductSource.ps1 lines 41 and 65 also omit it from the shared and observer-specific comparisons.
- Why it blocks / does not: The observer can receive credit for a binary built before a material UMM reference change that its compiler guard would refuse.
- Requested correction: Cover tools/Directory.Build.targets independently of Git tracking in the local receipt and include it in the observer-specific frozen comparison.
- Requested evidence: Place the existing loader-reference hook under tools in the disposable clone. Show local refusal while it is untracked and restored eligibility after removal. In the frozen fixture, show that committing the hook rejects the archived observer while preserving main-only reuse.

**R5-F1** · P2 · defect · blocks readiness — Preserve the Full-suite cost estimate after a receipt refusal
- Location: `scripts/Test-Tiers.ps1:252`; acceptance: mission §6, docs/VALIDATION-POLICY.md, OWNER AMENDMENT 1
- Trigger: A budget with a recorded Full-suite duration admits Full -SkipBuild and Test.ps1 rejects a stale receipt.
- Expected: The failed attempt consumes its actual time and leaves the known Full-suite duration available for the next admission decision.
- Actual: The receipt check now runs inside the timed Full step. When Test.ps1 refuses the receipt, Invoke-Tiered still calls Update-KmcWorkflowEntry at line 210. WorkflowBudget.ps1 overwrites the Full duration with the elapsed time of the refusal, even though the first runner was never reached.
- Why it blocks / does not: A later invocation can admit the Full suite using the short refusal time when its previously measured duration exceeds the remaining allowance.
- Requested correction: Keep the single admitted scan and preserve the Full-suite estimate when receipt validation stops execution before the runner.
- Requested evidence: Seed the existing disposable budget with a known Full duration, submit a stale receipt and inspect the ledger. Verify that the attempt is charged, the runner is untouched and the prior duration still governs a later admission with insufficient time.

#### Prior finding assessments

- **R1-F2** — `fix_verified` — The launcher passes its resolved owner profile to dependency admission before staging. The helper compares the measured UMM and Harmony 2 hashes with that profile, and the offline fixture records both a matching receipt and refusal after changing Harmony 2 alone.
- **R1-F8** — `fix_rejected` — The requested repository-root reproduction passes. The same material-input omission persists for the removal observer’s in-repository parent hook. Its location falls outside the untracked-file search and the archived observer comparison.
- **R3-F2** — `fix_verified` — The candidate gate counted one scan for current and stale Full receipts. Only the current receipt reached the runner boundary. Budget denial counted zero scans, and direct Test.ps1 reuse retained its stale-receipt guard. R5-F1 concerns the duration recorded after refusal.
- **R4-F1** — `fix_verified` — The ledger preserves the earlier entries and failed gate, records 396.3 seconds of round-4 direct validation as imports, identifies the estimates and quotes the owner’s separate allowance adjustment. The three round-5 direct runs add 38 seconds, and the final gate adds 182 metered seconds.

#### Evidence assessment

- **Inspected:** The fixed mission, OWNER AMENDMENT 1, repository validation policy, full feature diff, round-5 delta and four pending findings.; Both shipping projects and manifests, compiler guards, package validators, local and archived reuse checks, runtime dependency admission, tier execution and workflow budget accounting.
- **Executed verification:** The controller’s Fast gate passed all four steps at the exact candidate in 182.8 seconds. It recorded 1,488 component checks, 153 source checks and 53 package and reuse checks, all passing.; The gate accepted both genuine packages, rejected the released UMM 0.33.0 DLL under the baseline manifest, rejected extra dependencies and stale package receipts, and observed the repository-root hook invalidate reuse.; The exported round-5 fixture logs record 121 base-game isolation checks and 118 loader-contract checks passing. Changing only Harmony 2 produced the expected admission refusal.; Read-only verification matched the gate log digest and all 24 indexed evidence blob digests. Both frozen package manifests agree with their validation records and static audits on DLL SHA-256 and MVID. The reconciled ledger and final gate sum to 24.29 of 30.62 minutes.
- **Source inspection only:** The observer-parent omission follows from the local and frozen Git path selections. The recorded compiler test exercises the analogous src/Directory.Build.targets hook for the main project.; The effect of an early refusal on a populated Full budget estimate follows from Test-Tiers.ps1 and WorkflowBudget.ps1. The gate observes the refusal and runner boundary but does not inspect that estimate.
- **Required gaps:** Demonstrate invalidation for the observer’s inherited tools/Directory.Build.targets on both reuse paths.; Demonstrate that an early Full receipt refusal preserves the previously measured suite duration used for later budget admission.
- **Limitations:** The foreground developer transcript was withheld. The direct round-5 fixture results come from exported logs for the uncommitted batch identified as d5268152. The controller gate is bound directly to the final candidate.; The earlier Test-Harness record retains 280 passes and three failures also recorded on its comparison baseline.; Native execution and combined-stack play were NOT RUN. The four loader rows have static API evidence. The reviewer performed read-only checks.

#### Summary

- **Delivered behavior:** Mounted Combat and its removal observer declare UMM 0.31.1 and compile against verified UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Static audits of the packaged DLLs resolved their loader references on 0.31.1, 0.32.4, the owner’s 0.33.0 and 0.33.4.
- **Scope reviewed:** Mounted Combat draft PR #62 at the fixed candidate, including the separately shipped observer and the build, package, reuse and runtime identity paths affected by the baseline change.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Disposition at the existing review-round limit and eventual integration with the Mounted Shield changes already on main.; Authorization of a suitable guarded environment if native compatibility qualification is later selected.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a21249-b73e-70d3-bc1a-bb86ca8ebe92`, duration 883.3s, response sha256 `61f2f7682e290dac258df647215681efcd447413199e7474bad7e2d7489934d9`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r05-4431a8`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "e7ec0ef8cdc4383a1c4a010aee29b16cdc6ac631",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both shipping products meet the declared and compiled UMM 0.31.1 baseline. Two validation defects remain. The removal observer can reuse old binaries after an inherited MSBuild input changes, and a rejected receipt can overwrite the budget estimate for the entire Full suite.\n\n## Continuation for Claude\n\nInclude the observer\u2019s inherited tools/Directory.Build.targets in local and archived observer reuse checks. Preserve main-only reuse when the change affects only the observer. Keep the single receipt scan on Full reuse, charge failed attempts, and preserve the recorded suite duration when the receipt is refused before the runner starts. Exercise both corrections in the existing disposable fixtures. The Full fixture can stop at its runner boundary. Keep the work within the existing focused gate, time allowance and controller round limits.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller\u2019s Fast gate passed all four steps at the exact candidate in 182.8 seconds. It recorded 1,488 component checks, 153 source checks and 53 package and reuse checks, all passing.",
      "The gate accepted both genuine packages, rejected the released UMM 0.33.0 DLL under the baseline manifest, rejected extra dependencies and stale package receipts, and observed the repository-root hook invalidate reuse.",
      "The exported round-5 fixture logs record 121 base-game isolation checks and 118 loader-contract checks passing. Changing only Harmony 2 produced the expected admission refusal.",
      "Read-only verification matched the gate log digest and all 24 indexed evidence blob digests. Both frozen package manifests agree with their validation records and static audits on DLL SHA-256 and MVID. The reconciled ledger and final gate sum to 24.29 of 30.62 minutes."
    ],
    "inspected": [
      "The fixed mission, OWNER AMENDMENT 1, repository validation policy, full feature diff, round-5 delta and four pending findings.",
      "Both shipping projects and manifests, compiler guards, package validators, local and archived reuse checks, runtime dependency admission, tier execution and workflow budget accounting."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. The direct round-5 fixture results come from exported logs for the uncommitted batch identified as d5268152. The controller gate is bound directly to the final candidate.",
      "The earlier Test-Harness record retains 280 passes and three failures also recorded on its comparison baseline.",
      "Native execution and combined-stack play were NOT RUN. The four loader rows have static API evidence. The reviewer performed read-only checks."
    ],
    "required_gaps": [
      "Demonstrate invalidation for the observer\u2019s inherited tools/Directory.Build.targets on both reuse paths.",
      "Demonstrate that an early Full receipt refusal preserves the previously measured suite duration used for later budget admission."
    ],
    "source_inspection_only": [
      "The observer-parent omission follows from the local and frozen Git path selections. The recorded compiler test exercises the analogous src/Directory.Build.targets hook for the main project.",
      "The effect of an early refusal on a populated Full budget estimate follows from Test-Tiers.ps1 and WorkflowBudget.ps1. The gate observes the refusal and runner boundary but does not inspect that estimate."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "mission \u00a75",
        "mission \u00a76"
      ],
      "actual": "The stamp explicitly hashes the root hook, but its untracked-file search starts at tools/KmcRemovalObserver. The repository-wide targets query includes only tracked files. Once the parent hook is committed, FrozenProductSource.ps1 lines 41 and 65 also omit it from the shared and observer-specific comparisons.",
      "blocking_rationale": "The observer can receive credit for a binary built before a material UMM reference change that its compiler guard would refuse.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A change to the MSBuild hook inherited by the shipping observer invalidates its local outputs and archived observer reuse.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/LoaderInputStamp.ps1",
        "line": 20
      },
      "priority": "P2",
      "requested_correction": "Cover tools/Directory.Build.targets independently of Git tracking in the local receipt and include it in the observer-specific frozen comparison.",
      "requested_evidence": "Place the existing loader-reference hook under tools in the disposable clone. Show local refusal while it is untracked and restored eligibility after removal. In the frozen fixture, show that committing the hook rejects the archived observer while preserving main-only reuse.",
      "title": "Include the observer\u2019s inherited MSBuild hook in reuse checks",
      "trigger": "A developer adds tools/Directory.Build.targets after building the observer. MSBuild finds it one directory above tools/KmcRemovalObserver, just as the existing compiler regression finds src/Directory.Build.targets above the main project."
    },
    {
      "acceptance_ids": [
        "mission \u00a76",
        "docs/VALIDATION-POLICY.md",
        "OWNER AMENDMENT 1"
      ],
      "actual": "The receipt check now runs inside the timed Full step. When Test.ps1 refuses the receipt, Invoke-Tiered still calls Update-KmcWorkflowEntry at line 210. WorkflowBudget.ps1 overwrites the Full duration with the elapsed time of the refusal, even though the first runner was never reached.",
      "blocking_rationale": "A later invocation can admit the Full suite using the short refusal time when its previously measured duration exceeds the remaining allowance.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The failed attempt consumes its actual time and leaves the known Full-suite duration available for the next admission decision.",
      "id": "R5-F1",
      "location": {
        "file": "scripts/Test-Tiers.ps1",
        "line": 252
      },
      "priority": "P2",
      "requested_correction": "Keep the single admitted scan and preserve the Full-suite estimate when receipt validation stops execution before the runner.",
      "requested_evidence": "Seed the existing disposable budget with a known Full duration, submit a stale receipt and inspect the ledger. Verify that the attempt is charged, the runner is untouched and the prior duration still governs a later admission with insufficient time.",
      "title": "Preserve the Full-suite cost estimate after a receipt refusal",
      "trigger": "A budget with a recorded Full-suite duration admits Full -SkipBuild and Test.ps1 rejects a stale receipt."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "The launcher passes its resolved owner profile to dependency admission before staging. The helper compares the measured UMM and Harmony 2 hashes with that profile, and the offline fixture records both a matching receipt and refusal after changing Harmony 2 alone."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F8",
      "rationale": "The requested repository-root reproduction passes. The same material-input omission persists for the removal observer\u2019s in-repository parent hook. Its location falls outside the untracked-file search and the archived observer comparison."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F2",
      "rationale": "The candidate gate counted one scan for current and stale Full receipts. Only the current receipt reached the runner boundary. Budget denial counted zero scans, and direct Test.ps1 reuse retained its stale-receipt guard. R5-F1 concerns the duration recorded after refusal."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "The ledger preserves the earlier entries and failed gate, records 396.3 seconds of round-4 direct validation as imports, identifies the estimates and quotes the owner\u2019s separate allowance adjustment. The three round-5 direct runs add 38 seconds, and the final gate adds 182 metered seconds."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r05-4431a8",
  "round": 5,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its removal observer declare UMM 0.31.1 and compile against verified UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Static audits of the packaged DLLs resolved their loader references on 0.31.1, 0.32.4, the owner\u2019s 0.33.0 and 0.33.4.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Disposition at the existing review-round limit and eventual integration with the Mounted Shield changes already on main.",
      "Authorization of a suitable guarded environment if native compatibility qualification is later selected."
    ],
    "scope_reviewed": "Mounted Combat draft PR #62 at the fixed candidate, including the separately shipped observer and the build, package, reuse and runtime identity paths affected by the baseline change.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
