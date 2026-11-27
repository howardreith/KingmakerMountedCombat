<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r09-review part=1/1 sha256=312cb57c9e686466a2241f5621ab4b18f747fbc15f936ed47179ccc526e5cddb -->
### Codex GPT-6.2 Astra — review — round 9 — candidate `1253836aeb`

**Decision:** `ready_for_owner_review` — The candidate meets the S07 and S08 offline requirements. The complete reader accepted the producer’s Horse outcomes for both refresh orders and the mixed adoption/supply sequence. Each requested negative control failed at the expected check. The required focused gate passed on the submitted commit. N08 visible playback remains for the owner’s playtest.

#### Findings

**R9-F1** · P3 · optional_polish · non-blocking — The U+0000 negative control makes the gate log appear binary
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:746`; acceptance: S07
- Trigger: The connected fixture logs the outcome after inserting U+0000 into its observed handle source.
- Expected: The fixture preserves the raw value for validation and prints a readable representation of the control character.
- Actual: The fixture prints the mutated source verbatim. The candidate gate log contains a literal NUL on line 1827, causing default ripgrep searches to skip the later tier summary.
- Why it blocks / does not: The reader rejected the malformed source and the controller captured the correct verdict. The issue affects text searches of the log.
- Requested correction: Escape U+0000 when formatting the diagnostic source value.
- Requested evidence: On the next ordinary focused run, the reader still rejects the raw value and a default text search finds the final tier summary.

**R9-F2** · P3 · optional_polish · non-blocking — The Mount Primary test depends on validator declaration spacing
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:453`; acceptance: S07
- Trigger: A formatting edit adds whitespace to the ConvertTo-KmcHandleSource declaration.
- Expected: The test can load the production validator after a whitespace-only edit.
- Actual: The extraction regex requires exact declaration spacing. In a read-only comparison, adding one space before the opening brace made the lookup fail and would report the function as missing.
- Why it blocks / does not: The current declaration loads successfully, and the complete-reader cases separately exercise the production function.
- Requested correction: Look up ConvertTo-KmcHandleSource by function name in the PowerShell syntax tree already parsed by this script.
- Requested evidence: Both declaration spacings resolve the same function, and the existing Mount Primary cases retain their verdicts.

#### Prior finding assessments

- **R4-F2** — `fix_verified` — The producer and envelope use the same Horse ID. Test-ConnectedArtifact assigns both complete CaptureMountedOutcome rows, including actorId and action. The complete reader accepted both refresh orders in both positions and TB adoption followed by RT supply. Changing only the TB actor from horse to horse-2 failed the ownership check.
- **R8-F1** — `fix_verified` — All four observed and latest sources reach the validator as raw values. It requires a scalar string and compares the two allowed labels with ordinal equality. The complete reader rejected the array-valued TB observation, the array-valued RT registration and the U+0000-modified scalar. Their canonical controls passed.
- **R8-F2** — `fix_verified` — The candidate gate’s producer lines label the two counters create/adopt, including 1/0 for a supply and 0/1 for an adoption.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy, feature diff, round-8-to-round-9 changes, prior findings ledger and developer report.; The Horse binding adapter, strike observer, terminal projection, CaptureMountedOutcome writer, connected fixture, schema-8 reader and Mount Primary reader.; Controller receipt gate-focused-1253836aeb8a-d43d449b6c4-aa63aedd141-20261127T192524527491Z.json and its matching log.
- **Executed verification:** Verified the snapshot HEAD, mission digest and gate-log SHA-256 against the controller inputs. The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 in Release on this commit. It exited 0 after 86.9 seconds with all four stages and all 15 focused children passing.; The log records 1548/0 component checks, 153/0 source checks, 3/0 Bite native-contract checks, 54/0 Bite contract checks and 239/0 presentation checks. The Bite native-contract check matched the pinned Horse animation bundle.; Mounted Shield passed 114/0 contract and 68/0 rollback checks. The gate compared the packet, production and embedded icon bytes and found them identical.
- **Source inspection only:** The binding checks the registered Bite weapon and the Horse’s first additional limb. The strike observer reads presentation facts before the command’s single base.OnAction call, leaving native code in charge of the attack event and expenditure.
- **Required gaps:** none
- **Limitations:** The review was read-only. Execution evidence comes from the controller receipt and log. The controller withheld the mixed foreground transcript.; The connected fixture uses detached Kingmaker objects, substitutes native handle creation and Unity action-name reads, and sets playback flags. Camera visibility and live attack timing require the owner’s observation.

#### Summary

- **Delivered behavior:** The Horse Bite path adopts the exact stock handle, supplies one eligible fallback when needed, and records a same-action refresh. Strike and terminal diagnostics preserve which handle was observed at the Bite, even when registration changes afterward.
- **Scope reviewed:** PR #67’s MMR-07 binding and diagnostics, the S07 and S08 offline evidence, and the round-9 corrections to the reader and connected fixture.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** For N08, match the logged Bite to a visible bite at normal camera zoom. Check facing, both hoof animations, rider attachment, ordinary attack timing and handle release, and watch for duplicate rolls or damage.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21e78-e2c6-7ff2-a18b-ecd73730bd3b`, duration 365.3s, response sha256 `9d0fc91974ee5833f77d61b8ad2d64ae26e858e97501e92cfc5ee54a246e3eaa`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r09-c56460`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "1253836aeb8a926b17ec99e7f34bdf024cc8d188",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "The candidate meets the S07 and S08 offline requirements. The complete reader accepted the producer\u2019s Horse outcomes for both refresh orders and the mixed adoption/supply sequence. Each requested negative control failed at the expected check. The required focused gate passed on the submitted commit. N08 visible playback remains for the owner\u2019s playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD, mission digest and gate-log SHA-256 against the controller inputs. The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 in Release on this commit. It exited 0 after 86.9 seconds with all four stages and all 15 focused children passing.",
      "The log records 1548/0 component checks, 153/0 source checks, 3/0 Bite native-contract checks, 54/0 Bite contract checks and 239/0 presentation checks. The Bite native-contract check matched the pinned Horse animation bundle.",
      "Mounted Shield passed 114/0 contract and 68/0 rollback checks. The gate compared the packet, production and embedded icon bytes and found them identical."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy, feature diff, round-8-to-round-9 changes, prior findings ledger and developer report.",
      "The Horse binding adapter, strike observer, terminal projection, CaptureMountedOutcome writer, connected fixture, schema-8 reader and Mount Primary reader.",
      "Controller receipt gate-focused-1253836aeb8a-d43d449b6c4-aa63aedd141-20261127T192524527491Z.json and its matching log."
    ],
    "limitations": [
      "The review was read-only. Execution evidence comes from the controller receipt and log. The controller withheld the mixed foreground transcript.",
      "The connected fixture uses detached Kingmaker objects, substitutes native handle creation and Unity action-name reads, and sets playback flags. Camera visibility and live attack timing require the owner\u2019s observation."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "The binding checks the registered Bite weapon and the Horse\u2019s first additional limb. The strike observer reads presentation facts before the command\u2019s single base.OnAction call, leaving native code in charge of the attack event and expenditure."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "The fixture prints the mutated source verbatim. The candidate gate log contains a literal NUL on line 1827, causing default ripgrep searches to skip the later tier summary.",
      "blocking_rationale": "The reader rejected the malformed source and the controller captured the correct verdict. The issue affects text searches of the log.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The fixture preserves the raw value for validation and prints a readable representation of the control character.",
      "id": "R9-F1",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 746
      },
      "priority": "P3",
      "requested_correction": "Escape U+0000 when formatting the diagnostic source value.",
      "requested_evidence": "On the next ordinary focused run, the reader still rejects the raw value and a default text search finds the final tier summary.",
      "title": "The U+0000 negative control makes the gate log appear binary",
      "trigger": "The connected fixture logs the outcome after inserting U+0000 into its observed handle source."
    },
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "The extraction regex requires exact declaration spacing. In a read-only comparison, adding one space before the opening brace made the lookup fail and would report the function as missing.",
      "blocking_rationale": "The current declaration loads successfully, and the complete-reader cases separately exercise the production function.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The test can load the production validator after a whitespace-only edit.",
      "id": "R9-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 453
      },
      "priority": "P3",
      "requested_correction": "Look up ConvertTo-KmcHandleSource by function name in the PowerShell syntax tree already parsed by this script.",
      "requested_evidence": "Both declaration spacings resolve the same function, and the existing Mount Primary cases retain their verdicts.",
      "title": "The Mount Primary test depends on validator declaration spacing",
      "trigger": "A formatting edit adds whitespace to the ConvertTo-KmcHandleSource declaration."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F2",
      "rationale": "The producer and envelope use the same Horse ID. Test-ConnectedArtifact assigns both complete CaptureMountedOutcome rows, including actorId and action. The complete reader accepted both refresh orders in both positions and TB adoption followed by RT supply. Changing only the TB actor from horse to horse-2 failed the ownership check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F1",
      "rationale": "All four observed and latest sources reach the validator as raw values. It requires a scalar string and compares the two allowed labels with ordinal equality. The complete reader rejected the array-valued TB observation, the array-valued RT registration and the U+0000-modified scalar. Their canonical controls passed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F2",
      "rationale": "The candidate gate\u2019s producer lines label the two counters create/adopt, including 1/0 for a supply and 0/1 for an adoption."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r09-c56460",
  "round": 9,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The Horse Bite path adopts the exact stock handle, supplies one eligible fallback when needed, and records a same-action refresh. Strike and terminal diagnostics preserve which handle was observed at the Bite, even when registration changes afterward.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "For N08, match the logged Bite to a visible bite at normal camera zoom. Check facing, both hoof animations, rider attachment, ordinary attack timing and handle release, and watch for duplicate rolls or damage."
    ],
    "scope_reviewed": "PR #67\u2019s MMR-07 binding and diagnostics, the S07 and S08 offline evidence, and the round-9 corrections to the reader and connected fixture.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
