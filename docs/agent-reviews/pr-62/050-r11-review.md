<!-- agent-review:v1 enrollment=kmc-codex-umm-0311-baseline-20261123-20261123223744-7586 key=r11-review part=1/1 sha256=2c7cb497e3d8e41376bee9233064abb190aff6f7c7a2f868c56a52c424b5b013 -->
### Codex GPT-6.2 Astra — review — round 11 — candidate `082e8063a1`

**Decision:** `changes_required` — The integration preserves Mounted Shield and the accepted UMM baseline. PR #62’s required gate passed on the reviewed commit. Readiness still depends on evidence tying both release ZIPs to their producer and confirming the controller operations required by OWNER AMENDMENT 2.

## Continuation for Claude

Supply the generated manifests and validation evidence for the frozen ZIPs produced at 7a9861a5. Provide the preserved permission-review refusal, the separate round-cap extension record, and the final states of PR #69 and its enrollment. If #69 is still active, complete its authorized supersession through the supported controls. These corrections can use the existing artifacts and records. Keep the original #62 enrollment, release budget and correction ledger. If permission review rejects a supported operation, stop that operation and record the exact command and reason. Merge, prerelease publication and verification of published hashes follow readiness.

#### Findings

**R11-F1** · P1 · evidence_gap · blocks readiness — Bind both proposed alpha ZIPs to the clean release build
- Location: `CURRENT.md:29`; acceptance: Mission §5, Mission §6, OWNER AMENDMENT 2 item 7
- Trigger: Selecting the main and removal-observer archives for publication under v0.2.16-alpha.2.
- Expected: Each proposed release ZIP can be matched to its validated entries, audited DLL and clean producing commit 7a9861a5.
- Actual: CURRENT.md reports ZIP hashes 64a4317c… and 2c0fe382… and validator counts of 12/0 and 8/0. The release evidence contains DLL API audits, which carry no ZIP hash, entry list or producing commit. The available package manifests describe different ZIPs built at 11df053b.
- Why it blocks / does not: The proposed tag and assets depend on establishing which complete archives the clean release build produced. The current gate establishes the tested main DLL’s identity.
- Requested correction: Make the existing release qualification manifests available to review. Include the original validator output if retained, or measure the same frozen ZIPs with the standalone validators.
- Requested evidence: For each ZIP, show the producing commit and branch, clean-worktree result, version, ZIP SHA-256, DLL SHA-256 and MVID, and both allowed entries. Match those values to the release handoff and static audit.

**R11-F2** · P2 · evidence_gap · blocks readiness — Substantiate the required enrollment continuation sequence
- Location: `—`; acceptance: OWNER AMENDMENT 2 items 1–3
- Trigger: Continuing PR #62 after its ten completed reviews and the earlier automatic permission-review rejection.
- Expected: The original refusal is preserved verbatim, and a distinct supported command sets the absolute review cap to 13 after admission of the amendment.
- Actual: The controller input establishes amendment revision 2 and round 11. The supplied records omit the earlier refused command, its stated reason, and the separate extension result showing the absolute cap.
- Why it blocks / does not: These are explicit conditions of continuing the previously completed enrollment. Round 11 alone establishes neither the exact cap nor how it was set.
- Requested correction: Provide narrow excerpts from the existing permission-review and controller records. Retrieve the refusal without re-executing the denied operation.
- Requested evidence: The exact denied command and reason, followed by dated amendment and extension records identifying #62’s original enrollment and the resulting cap of 13.

**R11-F3** · P2 · evidence_gap · blocks readiness — Confirm closure of the redundant #69 route
- Location: `CURRENT.md:9`; acceptance: OWNER AMENDMENT 2 item 6
- Trigger: The original #62 enrollment accepts the integrated candidate for review.
- Expected: The redundant enrollment is cancelled and PR #69 is closed as superseded, with its records and discussion preserved.
- Actual: CURRENT.md calls #69 superseded, while the developer report says it is being closed. The supplied evidence gives its branch head but no terminal controller or PR status.
- Why it blocks / does not: OWNER AMENDMENT 2 requires retiring the redundant route once continuation through #62 is established. The original enrollment has already issued its gate and this review request.
- Requested correction: Confirm both states through supported controls and complete any outstanding supersession action authorized by item 6.
- Requested evidence: Controller status for #69’s enrollment and the corresponding GitHub closed-PR status identifying #62 as the continuing route.

**R11-F4** · P3 · optional_polish · non-blocking — Identify the line endings covered by the audit checksums
- Location: `docs/evidence/release-0.2.16-alpha.2/INDEX.md:3`; acceptance: Mission §8
- Trigger: Verifying the release audit JSON from GitHub or an LF checkout.
- Expected: A reader can tell which representation of each report the listed checksum identifies.
- Actual: The listed hashes match the Windows CRLF checkout. The committed LF blobs have SHA-256 values beginning 82cdd029 for the main audit and 25b22eae for the observer audit.
- Why it blocks / does not: The audit contents and audited DLL identities are verifiable. The ambiguity affects reproduction of the index checksums.
- Requested correction: Label the existing hashes as Windows-output hashes. The earlier baseline index provides a convention for listing committed-blob hashes alongside them.
- Requested evidence: The labeled values reproduce against the indicated file representation.

#### Evidence assessment

- **Inspected:** The mission, both owner amendments, repository instructions, validation policy, handoffs, developer report, supplied transcript, prior ledger and archived round-10 verdict.; Both shipping manifests and projects, reference pins and MSBuild checks, package generation and standalone validators, reuse checks, runtime profile handling, release notes and evidence indexes.
- **Executed verification:** The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MountedShieldFocused.ps1 at 082e8063. It exited 0 in 78.7 seconds. The log SHA-256 matches the receipt. Both compiler-input checks passed, along with 1,525 component checks, 153 source checks and all 17 focused children.; The gate measured main DLL SHA-256 2105f305856a65c85b82e0294341462bf6ffe0e89420ea011ef860864fe5ddc8 and MVID 34815fb3-3bd1-4d52-8571-46345f170bea. Its offline UMM binding checks passed 25/25 on both 0.31.1 and installed 0.33.0. The budget closed at 12.52 of 30 minutes.; The retained static audits identify the same main DLL and observer DLL f9697f91. They report all 42 main and 20 observer member references resolved on UMM 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.; Read-only Git checks confirmed the complete 8,634-line feature diff, the archive and merge ancestry, and identical sets of 696 compile and embedded-resource entries between main and the candidate. All 39 historical baseline evidence exports matched their indexed Git-blob hashes.
- **Source inspection only:** Both shipping projects verify the selected loader files before reference resolution and check the compiler’s resolved inputs before CoreCompile. The standalone validators inspect the DLL inside each ZIP.; QualificationPackaging.ps1 writes the two qualification manifests only after validating both archives, comparing their DLLs with the clean build outputs and rechecking source identity. Those manifests would directly establish the missing release provenance.; Only documentation and evidence changed between the reported producing commit 7a9861a5 and the reviewed head.
- **Required gaps:** R11-F1 — Generated provenance and validation evidence for the two proposed release ZIPs.; R11-F2 — The preserved refusal and the separate supported extension to an absolute cap of 13.; R11-F3 — Confirmation that the redundant #69 enrollment and PR reached their required terminal states.
- **Limitations:** The foreground transcript was withheld because it mixes projects. The supplied controller evidence contains the current #62 gate. The cited #69 package-admission run and the release packaging output were unavailable for independent inspection.; Native startup, mount/dismount, combat, observer behavior, save/reload and combined-stack operation remain NOT RUN for this alpha. The release notes disclose that limit. This review used read-only inspection.

#### Summary

- **Delivered behavior:** Mounted Combat 0.2.16-alpha.2 and removal observer 0.1.0-observer.8 declare UMM 0.31.1 and build against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. The integration retains Mounted Shield, its embedded icon and the accepted #62 archive.
- **Scope reviewed:** Final candidate readiness for Mounted PR #62 under both owner amendments. The authorized merge, producer tag, GitHub prerelease and published-asset hash verification follow readiness, with 0.2.15 retained as Latest.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Mounted Shield’s in-game presentation and behavior, including feat selection, AC, hit negation and save/reload, remain for the owner’s alpha playtest.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `0fae88d4efb378d9cd56cb26af6a0f53c92a8a460fe4e32617072804ddaa47ff`.

Codex CLI `0.170.0`, thread `01a218ae-43b4-7893-88a3-bf22c21cb61c`, duration 687.3s, response sha256 `2a5c574ec84b61860af6829ad7b52d4a59b358a75e67b5edb4ef5e2687df7849`, request `kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r11-010b60`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "082e8063a1ff55929094d857eaa8bfd072a84fed",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The integration preserves Mounted Shield and the accepted UMM baseline. PR #62\u2019s required gate passed on the reviewed commit. Readiness still depends on evidence tying both release ZIPs to their producer and confirming the controller operations required by OWNER AMENDMENT 2.\n\n## Continuation for Claude\n\nSupply the generated manifests and validation evidence for the frozen ZIPs produced at 7a9861a5. Provide the preserved permission-review refusal, the separate round-cap extension record, and the final states of PR #69 and its enrollment. If #69 is still active, complete its authorized supersession through the supported controls. These corrections can use the existing artifacts and records. Keep the original #62 enrollment, release budget and correction ledger. If permission review rejects a supported operation, stop that operation and record the exact command and reason. Merge, prerelease publication and verification of published hashes follow readiness.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MountedShieldFocused.ps1 at 082e8063. It exited 0 in 78.7 seconds. The log SHA-256 matches the receipt. Both compiler-input checks passed, along with 1,525 component checks, 153 source checks and all 17 focused children.",
      "The gate measured main DLL SHA-256 2105f305856a65c85b82e0294341462bf6ffe0e89420ea011ef860864fe5ddc8 and MVID 34815fb3-3bd1-4d52-8571-46345f170bea. Its offline UMM binding checks passed 25/25 on both 0.31.1 and installed 0.33.0. The budget closed at 12.52 of 30 minutes.",
      "The retained static audits identify the same main DLL and observer DLL f9697f91. They report all 42 main and 20 observer member references resolved on UMM 0.31.1, 0.32.4, owner 0.33.0 and 0.33.4.",
      "Read-only Git checks confirmed the complete 8,634-line feature diff, the archive and merge ancestry, and identical sets of 696 compile and embedded-resource entries between main and the candidate. All 39 historical baseline evidence exports matched their indexed Git-blob hashes."
    ],
    "inspected": [
      "The mission, both owner amendments, repository instructions, validation policy, handoffs, developer report, supplied transcript, prior ledger and archived round-10 verdict.",
      "Both shipping manifests and projects, reference pins and MSBuild checks, package generation and standalone validators, reuse checks, runtime profile handling, release notes and evidence indexes."
    ],
    "limitations": [
      "The foreground transcript was withheld because it mixes projects. The supplied controller evidence contains the current #62 gate. The cited #69 package-admission run and the release packaging output were unavailable for independent inspection.",
      "Native startup, mount/dismount, combat, observer behavior, save/reload and combined-stack operation remain NOT RUN for this alpha. The release notes disclose that limit. This review used read-only inspection."
    ],
    "required_gaps": [
      "R11-F1 \u2014 Generated provenance and validation evidence for the two proposed release ZIPs.",
      "R11-F2 \u2014 The preserved refusal and the separate supported extension to an absolute cap of 13.",
      "R11-F3 \u2014 Confirmation that the redundant #69 enrollment and PR reached their required terminal states."
    ],
    "source_inspection_only": [
      "Both shipping projects verify the selected loader files before reference resolution and check the compiler\u2019s resolved inputs before CoreCompile. The standalone validators inspect the DLL inside each ZIP.",
      "QualificationPackaging.ps1 writes the two qualification manifests only after validating both archives, comparing their DLLs with the clean build outputs and rechecking source identity. Those manifests would directly establish the missing release provenance.",
      "Only documentation and evidence changed between the reported producing commit 7a9861a5 and the reviewed head."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "Mission \u00a75",
        "Mission \u00a76",
        "OWNER AMENDMENT 2 item 7"
      ],
      "actual": "CURRENT.md reports ZIP hashes 64a4317c\u2026 and 2c0fe382\u2026 and validator counts of 12/0 and 8/0. The release evidence contains DLL API audits, which carry no ZIP hash, entry list or producing commit. The available package manifests describe different ZIPs built at 11df053b.",
      "blocking_rationale": "The proposed tag and assets depend on establishing which complete archives the clean release build produced. The current gate establishes the tested main DLL\u2019s identity.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each proposed release ZIP can be matched to its validated entries, audited DLL and clean producing commit 7a9861a5.",
      "id": "R11-F1",
      "location": {
        "file": "CURRENT.md",
        "line": 29
      },
      "priority": "P1",
      "requested_correction": "Make the existing release qualification manifests available to review. Include the original validator output if retained, or measure the same frozen ZIPs with the standalone validators.",
      "requested_evidence": "For each ZIP, show the producing commit and branch, clean-worktree result, version, ZIP SHA-256, DLL SHA-256 and MVID, and both allowed entries. Match those values to the release handoff and static audit.",
      "title": "Bind both proposed alpha ZIPs to the clean release build",
      "trigger": "Selecting the main and removal-observer archives for publication under v0.2.16-alpha.2."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 2 items 1\u20133"
      ],
      "actual": "The controller input establishes amendment revision 2 and round 11. The supplied records omit the earlier refused command, its stated reason, and the separate extension result showing the absolute cap.",
      "blocking_rationale": "These are explicit conditions of continuing the previously completed enrollment. Round 11 alone establishes neither the exact cap nor how it was set.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The original refusal is preserved verbatim, and a distinct supported command sets the absolute review cap to 13 after admission of the amendment.",
      "id": "R11-F2",
      "location": {
        "file": null,
        "line": null
      },
      "priority": "P2",
      "requested_correction": "Provide narrow excerpts from the existing permission-review and controller records. Retrieve the refusal without re-executing the denied operation.",
      "requested_evidence": "The exact denied command and reason, followed by dated amendment and extension records identifying #62\u2019s original enrollment and the resulting cap of 13.",
      "title": "Substantiate the required enrollment continuation sequence",
      "trigger": "Continuing PR #62 after its ten completed reviews and the earlier automatic permission-review rejection."
    },
    {
      "acceptance_ids": [
        "OWNER AMENDMENT 2 item 6"
      ],
      "actual": "CURRENT.md calls #69 superseded, while the developer report says it is being closed. The supplied evidence gives its branch head but no terminal controller or PR status.",
      "blocking_rationale": "OWNER AMENDMENT 2 requires retiring the redundant route once continuation through #62 is established. The original enrollment has already issued its gate and this review request.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The redundant enrollment is cancelled and PR #69 is closed as superseded, with its records and discussion preserved.",
      "id": "R11-F3",
      "location": {
        "file": "CURRENT.md",
        "line": 9
      },
      "priority": "P2",
      "requested_correction": "Confirm both states through supported controls and complete any outstanding supersession action authorized by item 6.",
      "requested_evidence": "Controller status for #69\u2019s enrollment and the corresponding GitHub closed-PR status identifying #62 as the continuing route.",
      "title": "Confirm closure of the redundant #69 route",
      "trigger": "The original #62 enrollment accepts the integrated candidate for review."
    },
    {
      "acceptance_ids": [
        "Mission \u00a78"
      ],
      "actual": "The listed hashes match the Windows CRLF checkout. The committed LF blobs have SHA-256 values beginning 82cdd029 for the main audit and 25b22eae for the observer audit.",
      "blocking_rationale": "The audit contents and audited DLL identities are verifiable. The ambiguity affects reproduction of the index checksums.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A reader can tell which representation of each report the listed checksum identifies.",
      "id": "R11-F4",
      "location": {
        "file": "docs/evidence/release-0.2.16-alpha.2/INDEX.md",
        "line": 3
      },
      "priority": "P3",
      "requested_correction": "Label the existing hashes as Windows-output hashes. The earlier baseline index provides a convention for listing committed-blob hashes alongside them.",
      "requested_evidence": "The labeled values reproduce against the indicated file representation.",
      "title": "Identify the line endings covered by the audit checksums",
      "trigger": "Verifying the release audit JSON from GitHub or an LF checkout."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-umm-0311-baseline-20261123-20261123223744-7586-r11-010b60",
  "round": 11,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Mounted Combat 0.2.16-alpha.2 and removal observer 0.1.0-observer.8 declare UMM 0.31.1 and build against pinned UnityModManager 0.31.1.0 and Harmony12 1.2.0.1. The integration retains Mounted Shield, its embedded icon and the accepted #62 archive.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Mounted Shield\u2019s in-game presentation and behavior, including feat selection, AC, hit negation and save/reload, remain for the owner\u2019s alpha playtest."
    ],
    "scope_reviewed": "Final candidate readiness for Mounted PR #62 under both owner amendments. The authorized merge, producer tag, GitHub prerelease and published-asset hash verification follow readiness, with 0.2.15 retained as Latest.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
