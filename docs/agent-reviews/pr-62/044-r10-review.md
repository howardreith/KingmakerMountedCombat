<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r10-review part=1/1 sha256=cdc7f478e67de1e326c5726e6871879aff50c5bdcf90d7cf9cc6a87b8db5d865 -->
### Codex GPT-6.2 Astra — review — round 10 — candidate `060d21c12c`

**Decision:** `ready_for_owner_review` — R9-F1 is fixed. A zero-second receipt check leaves the first Full run’s estimate unknown, so an unmeasured interruption blocks the next admission. The candidate passed the required focused gate. The build, package and static API evidence supports the UMM 0.31.1 baseline for both shipping products under OWNER AMENDMENT 1.

#### Findings

None.

#### Prior finding assessments

- **R9-F1** — `fix_verified` — With a current Debug receipt, zero-second receipt history and empty Full history, the fixture checked that the selector persisted estimatedSeconds as null. It then simulated process death before a measured heartbeat. The same entry became unreconciled with null spend, and the next selection was refused before a child ran.

#### Evidence assessment

- **Inspected:** The fixed feature diff, round 9 ledger, round 10 correction, mission, owner amendment and repository validation policy.; Both manifests and production projects, compiler reference guards, standalone package validators, build-reuse checks, loader profiles and compatibility records.; The controller receipt and matching gate-log SHA-256. All 39 indexed evidence exports matched their committed Git-blob SHA-256 values.
- **Executed verification:** The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 at 060d21c1. It exited 0 in 243.3 seconds. Both compiler-input checks passed, along with 1,488 component checks, 153 source checks and 65 focused checks.; The gate accepted both genuine packages. It rejected a released main DLL compiled against UMM 0.33.0.0 paired with a 0.31.1 manifest, extra dependency DLLs and stale build receipts.; With 600 seconds of receipt history and 60 seconds available, the gate refused Full before scanning or starting the runner. With a recorded inclusive Full duration of 200 seconds, it admitted 270 seconds of allowance and refused 180.; The retained round 10 tier-selection run passed 30/30. Its separate plan probe showed an unknown estimate for a zero-second receipt and a one-second lower bound for a 0.4-second receipt. The tested scripts at 441ce4ee are identical at the candidate.; The earlier Windows direct-MSBuild checks passed 12/12. The package records at 11df053b match the static audits by DLL SHA-256 and MVID for both products. Across all four loader profiles, the audits resolved all 42 referenced members for the mod and all 20 for the observer.
- **Source inspection only:** The strictly positive receipt check controls both selection and step admission when Full history is absent. The budget reconciler preserves unknown spend when both the persisted estimate and a measured interval are unavailable.; The launcher carries the measured owner profile into dependency admission and result readers. This establishes how the code selects and checks the profile.
- **Required gaps:** none
- **Limitations:** The 30-case selector fixture used a file clock and stub child processes. Its log comes from the round 10 batch before commit. The controller receipt at the final commit covers the focused Fast gate.; Native checks on UMM 0.31.1, 0.32.4, installed 0.33.0 and 0.33.4 are NOT RUN. The guarded launcher stages into the owner’s live Mods tree and Mounted lacks an approved disposable game-copy harness. Combined-stack behavior is also untested.; The earlier Test-Harness result retains three documented pre-existing failures alongside 280 passes.

#### Summary

- **Delivered behavior:** Mounted Combat and its separately shipped removal observer declare UMM 0.31.1 and compile against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Their packaged DLLs passed static API checks on UMM 0.31.1, 0.32.4, the owner’s 0.33.0 and 0.33.4.
- **Scope reviewed:** Mounted Combat draft PR #62 at the fixed candidate, including the round 10 correction to Full -SkipBuild budget accounting and its interruption regression.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide how to integrate draft PR #62 with main after the Mounted Shield merge. Both branches changed the compiler reference settings, and this verdict applies to 060d21c1.; Decide whether to authorize a disposable native qualification before making gameplay compatibility claims.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `c1d37362aca1706c8f63d7320af1d64fa8b2b79917b6a73f5a966e2048277e43`.

Codex CLI `0.170.0`, thread `01a217be-858c-7521-a2f2-87600ee207f2`, duration 353.3s, response sha256 `dbfa0b79192882bf36d972ca7349b8640bf6437957b2b5ca7b57a99236fa08dc`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r10-32df85`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "060d21c12cebbc93d11af71b110bdefa0a5f94e6",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "R9-F1 is fixed. A zero-second receipt check leaves the first Full run\u2019s estimate unknown, so an unmeasured interruption blocks the next admission. The candidate passed the required focused gate. The build, package and static API evidence supports the UMM 0.31.1 baseline for both shipping products under OWNER AMENDMENT 1.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-PackageAdmissionAndReuse.ps1 at 060d21c1. It exited 0 in 243.3 seconds. Both compiler-input checks passed, along with 1,488 component checks, 153 source checks and 65 focused checks.",
      "The gate accepted both genuine packages. It rejected a released main DLL compiled against UMM 0.33.0.0 paired with a 0.31.1 manifest, extra dependency DLLs and stale build receipts.",
      "With 600 seconds of receipt history and 60 seconds available, the gate refused Full before scanning or starting the runner. With a recorded inclusive Full duration of 200 seconds, it admitted 270 seconds of allowance and refused 180.",
      "The retained round 10 tier-selection run passed 30/30. Its separate plan probe showed an unknown estimate for a zero-second receipt and a one-second lower bound for a 0.4-second receipt. The tested scripts at 441ce4ee are identical at the candidate.",
      "The earlier Windows direct-MSBuild checks passed 12/12. The package records at 11df053b match the static audits by DLL SHA-256 and MVID for both products. Across all four loader profiles, the audits resolved all 42 referenced members for the mod and all 20 for the observer."
    ],
    "inspected": [
      "The fixed feature diff, round 9 ledger, round 10 correction, mission, owner amendment and repository validation policy.",
      "Both manifests and production projects, compiler reference guards, standalone package validators, build-reuse checks, loader profiles and compatibility records.",
      "The controller receipt and matching gate-log SHA-256. All 39 indexed evidence exports matched their committed Git-blob SHA-256 values."
    ],
    "limitations": [
      "The 30-case selector fixture used a file clock and stub child processes. Its log comes from the round 10 batch before commit. The controller receipt at the final commit covers the focused Fast gate.",
      "Native checks on UMM 0.31.1, 0.32.4, installed 0.33.0 and 0.33.4 are NOT RUN. The guarded launcher stages into the owner\u2019s live Mods tree and Mounted lacks an approved disposable game-copy harness. Combined-stack behavior is also untested.",
      "The earlier Test-Harness result retains three documented pre-existing failures alongside 280 passes."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "The strictly positive receipt check controls both selection and step admission when Full history is absent. The budget reconciler preserves unknown spend when both the persisted estimate and a measured interval are unavailable.",
      "The launcher carries the measured owner profile into dependency admission and result readers. This establishes how the code selects and checks the profile."
    ]
  },
  "findings": [],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F1",
      "rationale": "With a current Debug receipt, zero-second receipt history and empty Full history, the fixture checked that the selector persisted estimatedSeconds as null. It then simulated process death before a measured heartbeat. The same entry became unreconciled with null spend, and the next selection was refused before a child ran."
    }
  ],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r10-32df85",
  "round": 10,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat and its separately shipped removal observer declare UMM 0.31.1 and compile against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. Their packaged DLLs passed static API checks on UMM 0.31.1, 0.32.4, the owner\u2019s 0.33.0 and 0.33.4.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide how to integrate draft PR #62 with main after the Mounted Shield merge. Both branches changed the compiler reference settings, and this verdict applies to 060d21c1.",
      "Decide whether to authorize a disposable native qualification before making gameplay compatibility claims."
    ],
    "scope_reviewed": "Mounted Combat draft PR #62 at the fixed candidate, including the round 10 correction to Full -SkipBuild budget accounting and its interruption regression.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
