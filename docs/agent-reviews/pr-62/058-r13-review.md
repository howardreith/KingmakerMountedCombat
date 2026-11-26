<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r13-review part=1/1 sha256=bb3714da2f48651982389301568f61120c40e8fdabfffc56986890b398d8a952 -->
### Codex GPT-6.2 Astra — review — round 13 — candidate `dd1cce309c`

**Decision:** `ready_for_owner_review` — Both pending evidence gaps are resolved. The controller’s focused gate passed on dd1cce30, and both frozen release packages remain traceable to their producing commit, 7a9861a5. OWNER AMENDMENT 2 places the merge, prerelease publication and verification of published asset hashes after candidate readiness.

#### Findings

**R13-F1** · P3 · optional_polish · non-blocking — Identify the PR closure record as an extract in the evidence index
- Location: `docs/evidence/release-0.2.16-alpha.2/INDEX.md:18`; acceptance: OWNER AMENDMENT 2 item 6
- Trigger: A maintainer uses the index to compare the full GitHub response with the committed file.
- Expected: The index distinguishes the hash of the full GitHub response from the hash of the committed extract.
- Actual: The index says exports change only paths, line endings and a byte-order mark. The new PR record also selects fields and retains one supersession comment from the full response.
- Why it blocks / does not: The JSON discloses the selection, its committed digest matches the index, and it contains all observations needed to verify closure.
- Requested correction: Qualify the index’s normalization statement to identify pr69-closure.json as a selected-fields extract.
- Requested evidence: The existing closure record and indexed hashes suffice for this wording change.

#### Prior finding assessments

- **R11-F2** — `fix_verified` — Transcript lines 31704–31705 pair the 13:27:27Z Bash call with the classifier’s “Destructive or Unwanted Actions” denial. The command agrees across the JSON and handoff through the final Python expression and the `|| tail -20` fallback, with local path prefixes normalized. This supplies the missing record under OWNER AMENDMENT 2 item 1.
- **R11-F3** — `fix_verified` — The captured GitHub response records draft PR #69 as CLOSED at 2026-11-26T16:44:56Z on ce89fe48, which matches the retained branch. Supersession comment 7198044825 names #62’s existing enrollment one second before closure. Together with the controller’s CANCELLED result and zero reviews spent on #69, this satisfies OWNER AMENDMENT 2 item 6.

#### Evidence assessment

- **Inspected:** The mission, both owner amendments, repository validation policy, current handoff, prior findings ledger, feature diff and four-file correction since 64abec49.; The permission-denial extract, PR #69 closure response, controller continuation records, release evidence index, package manifests, validator outputs and candidate gate receipt.
- **Executed verification:** Matched the supplied 8,845-line feature diff to Git, verified all 45 historical review archive files against 20bf1351, and reproduced all nine committed release-evidence SHA-256 values.; Verified the controller gate log against its receipt for dd1cce30. Fast passed in 78.3 seconds with 1,525 component checks, 153 source checks and 17 of 17 Mounted Shield children. The offline loader checks passed 25 of 25 on both UMM 0.31.1 and the installed 0.33.0. The release budget closed the run at 14.97 of 30 minutes spent.; Matched the frozen ZIP hashes to the retained standalone validator outputs, which report 12 of 12 for the mod and 8 of 8 for the observer. Both manifests name clean producer 7a9861a5. Their DLL hashes and MVIDs match the static audits for UMM 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.; The round-13 diff passed git diff --check. The full feature diff reported trailing spaces only in five preserved baseline evidence files.
- **Source inspection only:** Both shipping projects import the pinned-reference checks, including the final compiler-input check after Microsoft.CSharp.targets.; The release plan targets v0.2.16-alpha.2 at 7a9861a5 and specifies a prerelease that preserves 0.2.15 as Latest.
- **Required gaps:** none
- **Limitations:** The snapshot provides a path-normalized transcript extract and selected GitHub response fields. Independent hash checks covered the committed records. The full session and unfiltered GitHub response were unavailable for re-hashing.; Native startup, gameplay, save/reload, removal-observer behavior in the game and combined-stack testing remain unverified on every loader.

#### Summary

- **Delivered behavior:** Mounted Combat 0.2.16-alpha.2 retains Mounted Shield. Both the mod and the separately shipped removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1.
- **Scope reviewed:** Mounted PR #62’s integrated candidate, the frozen alpha packages and the records establishing continuation through #62 and retirement of #69.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Owner play acceptance remains open for Mounted Shield in combat, dismount and save/reload.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff`.

Codex CLI `0.170.0`, thread `01a218ce-d8e9-7930-95d4-63014771b7d1`, duration 480.8s, response sha256 `6adf70266cf07a7d11db86efd09938d15a4330dab131d1b00026edfa693a5c64`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r13-08cc48`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "dd1cce309cf2ebd3abcdca9624cdedcf49caa4a3",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "Both pending evidence gaps are resolved. The controller\u2019s focused gate passed on dd1cce30, and both frozen release packages remain traceable to their producing commit, 7a9861a5. OWNER AMENDMENT 2 places the merge, prerelease publication and verification of published asset hashes after candidate readiness.",
  "evidence_assessment": {
    "executed_verification": [
      "Matched the supplied 8,845-line feature diff to Git, verified all 45 historical review archive files against 20bf1351, and reproduced all nine committed release-evidence SHA-256 values.",
      "Verified the controller gate log against its receipt for dd1cce30. Fast passed in 78.3 seconds with 1,525 component checks, 153 source checks and 17 of 17 Mounted Shield children. The offline loader checks passed 25 of 25 on both UMM 0.31.1 and the installed 0.33.0. The release budget closed the run at 14.97 of 30 minutes spent.",
      "Matched the frozen ZIP hashes to the retained standalone validator outputs, which report 12 of 12 for the mod and 8 of 8 for the observer. Both manifests name clean producer 7a9861a5. Their DLL hashes and MVIDs match the static audits for UMM 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.",
      "The round-13 diff passed git diff --check. The full feature diff reported trailing spaces only in five preserved baseline evidence files."
    ],
    "inspected": [
      "The mission, both owner amendments, repository validation policy, current handoff, prior findings ledger, feature diff and four-file correction since 64abec49.",
      "The permission-denial extract, PR #69 closure response, controller continuation records, release evidence index, package manifests, validator outputs and candidate gate receipt."
    ],
    "limitations": [
      "The snapshot provides a path-normalized transcript extract and selected GitHub response fields. Independent hash checks covered the committed records. The full session and unfiltered GitHub response were unavailable for re-hashing.",
      "Native startup, gameplay, save/reload, removal-observer behavior in the game and combined-stack testing remain unverified on every loader."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "Both shipping projects import the pinned-reference checks, including the final compiler-input check after Microsoft.CSharp.targets.",
      "The release plan targets v0.2.16-alpha.2 at 7a9861a5 and specifies a prerelease that preserves 0.2.15 as Latest."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 2 item 6"
      ],
      "actual": "The index says exports change only paths, line endings and a byte-order mark. The new PR record also selects fields and retains one supersession comment from the full response.",
      "blocking_rationale": "The JSON discloses the selection, its committed digest matches the index, and it contains all observations needed to verify closure.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The index distinguishes the hash of the full GitHub response from the hash of the committed extract.",
      "id": "R13-F1",
      "location": {
        "file": "docs/evidence/release-0.2.16-alpha.2/INDEX.md",
        "line": 18
      },
      "priority": "P3",
      "requested_correction": "Qualify the index\u2019s normalization statement to identify pr69-closure.json as a selected-fields extract.",
      "requested_evidence": "The existing closure record and indexed hashes suffice for this wording change.",
      "title": "Identify the PR closure record as an extract in the evidence index",
      "trigger": "A maintainer uses the index to compare the full GitHub response with the committed file."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F2",
      "rationale": "Transcript lines 31704\u201331705 pair the 13:27:27Z Bash call with the classifier\u2019s \u201cDestructive or Unwanted Actions\u201d denial. The command agrees across the JSON and handoff through the final Python expression and the `|| tail -20` fallback, with local path prefixes normalized. This supplies the missing record under OWNER AMENDMENT 2 item 1."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R11-F3",
      "rationale": "The captured GitHub response records draft PR #69 as CLOSED at 2026-11-26T16:44:56Z on ce89fe48, which matches the retained branch. Supersession comment 7198044825 names #62\u2019s existing enrollment one second before closure. Together with the controller\u2019s CANCELLED result and zero reviews spent on #69, this satisfies OWNER AMENDMENT 2 item 6."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r13-08cc48",
  "round": 13,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat 0.2.16-alpha.2 retains Mounted Shield. Both the mod and the separately shipped removal observer declare UMM 0.31.1 and compile against UnityModManager 0.31.1.0 and Harmony12 1.2.0.1.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Owner play acceptance remains open for Mounted Shield in combat, dismount and save/reload."
    ],
    "scope_reviewed": "Mounted PR #62\u2019s integrated candidate, the frozen alpha packages and the records establishing continuation through #62 and retirement of #69.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
