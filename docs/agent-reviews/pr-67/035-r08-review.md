<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r08-review part=1/1 sha256=5cb1cf6f6079f6b2ec3b1af5e1f03b879bbe4689174299d41e38dd887bab236a -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `dda42b9c0c`

**Decision:** `changes_required` — The focused gate passed, and both round-6 findings are fixed. Two issues still affect S07 evidence. The connected test validates a fixture-supplied Horse identity, and the schema-8 reader accepts malformed handle sources.

## Continuation for Claude

Confine the correction to the schema-8 reader and its offline fixtures. Give the producer and envelope one Horse identity, then pass both complete CaptureMountedOutcome rows into the reader. Validate the raw observed and latest sources as scalar strings using the same ordinal label comparison as the product. Preserve the verified counter rules, both refresh orders, historical controls and native attack path. Within the existing authorization and budget, capture the focused gate showing that the original writer rows pass and that a mismatched actor or malformed source fails. N08 remains with the owner’s playtest.

#### Findings

**R4-F2** · P2 · evidence_gap · blocks readiness — The connected fixture still substitutes the Horse actor identity
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:732`; acceptance: MMR-07, S07
- Trigger: A successful supply-and-refresh outcome is serialized and inserted into the complete schema-8 fixture.
- Expected: The complete reader accepts the successful, readable Horse outcome emitted by the command, including its actor identity.
- Actual: Test-ConnectedArtifact copies nine fields from each writer row. The producer creates horse-1, but the fixture retains actorId=horse and its own action. The TB ownership check at RuntimeHarness.Common.ps1 line 9085 rejects horse-1 against this envelope. The recorded positive therefore uses a different actor from the serialized outcome.
- Why it blocks / does not: A missing or wrong producer actor ID can still receive a passing connected-test verdict. Horse ownership is an explicit condition of the complete reader.
- Requested correction: Use one Horse ID in the producer and envelope. Assign the complete TB and RT Wire results to their respective outcome rows, retaining the unrelated world and UI fixture data.
- Requested evidence: Show complete-reader acceptance for both refresh orders and the mixed adoption/supply sequence with the writer rows intact. Changing only the TB actor ID should fail the ownership check.

**R8-F1** · P2 · defect · blocks readiness — Malformed handle sources receive exact binding credit
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:8835`; acceptance: MMR-07, S07
- Trigger: A schema-8 artifact reports a supply refreshed to stock before observation, with valid counts but a malformed observed or latest source.
- Expected: Each observed and latest source is a scalar JSON string equal to stock-created or kmc-supplied under ordinal comparison.
- Actual: The reader casts sources to string before validating them. A JSON array containing only "stock-created" passed the observation checks in either position. The transition helper also accepted a string with U+0000 inserted into that label, which the product’s ordinal comparison rejects.
- Why it blocks / does not: The external reader can award exact binding credit for a source value outside the production contract.
- Requested correction: Validate the four raw source values before string conversion or typed parameter binding. Use ordinal equality for the two allowed labels, then apply the existing transition table.
- Requested evidence: Exercise an array-valued TB observed source, an array-valued RT latest source and an ordinally different scalar in the complete-reader fixture. Each should fail while the corresponding canonical strings pass.

**R8-F2** · P3 · optional_polish · non-blocking — The fixture log labels a third counter that it does not print
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:766`; acceptance: S07
- Trigger: The connected producer prints its per-sequence summary.
- Expected: The counter names in the diagnostic match the values printed.
- Actual: The label says create/adopt/refresh, but the line prints only creation and adoption counts.
- Why it blocks / does not: The binding reports and source transitions already establish that the refresh occurred.
- Requested correction: Label the two values create/adopt.
- Requested evidence: The next ordinary focused log should show two names for the two counts.

#### Prior finding assessments

- **R4-F2** — `fix_rejected` — Production binding, observation, refresh and terminal projection now produce the requested readable Bite fields. The remaining gap is the final transfer. The fixture supplies the actor ID that satisfies the complete reader’s ownership check.
- **R6-F1** — `fix_verified` — An initial supply now permits stock at both observation and terminal after a refresh before the strike. The gate exercised both refresh orders in both TB and RT positions and retained the stock-to-supplied rejection.
- **R6-F2** — `fix_verified` — All six raw counters are validated before conversion. TB and the component-wise RT delta each require exactly one creation or one adoption, with zero rejections. Both decreasing sequences and fractional inputs failed, and TB adoption followed by RT supply passed.

#### Evidence assessment

- **Inspected:** Controller mission, prior-findings ledger, developer report, transcript notice, gate receipt and log, repository instructions, and the M..H feature diff.; The correction from 84036b7c through H, including the handle binding, strike observation, metadata boundary, terminal projection, schema-8 writer and reader, connected fixture and Test-Harness consumer.
- **Executed verification:** Confirmed the controller receipt names the exact candidate and its log matches SHA-256 a7cde0c8c464c49750bc15dcd7e7a69487e83859aa3a68c90b4485bd2524d197. Fast exited 0 in 40.6 seconds with 15/15 focused children, 1548/0 component checks, 153/0 source checks and 52/0 Bite contract checks.; The gate recorded readable Success outcomes with one child start for both refresh orders. The unreadable terminal control produced SecurityException and PlaybackAccepted=false. Presentation passed 239/0, Mounted Shield passed 114/0, rollback passed 68/0, and the packet, production and embedded icon hashes matched.; Independently exercised the candidate’s reader functions in memory. All 1,024 scalar-source combinations and 256 small integer-counter combinations matched the expected transitions. Nine malformed counter values were rejected.; Replayed the reader’s observation checks in memory. The fixture’s actor ID passed, substituting the connected producer’s actor ID failed ownership, and aligning the envelope identity passed. Separately, JSON arrays containing one valid source label passed in both the TB observed and RT latest positions.
- **Source inspection only:** The ordinary attack path classifies the registered Bite limb, delegates handle binding to the adapter and calls the native base OnAction once after observation.; DescribeOutcome takes ActorId from the acting unit, and CaptureMountedOutcome serializes it. The connected fixture’s field-copy list omits both actorId and action.
- **Required gaps:** Complete-reader acceptance of the actual serialized Horse outcomes with their producer identity.; Rejection of non-string and ordinally different handle-source values by the schema-8 reader.
- **Limitations:** The review used read-only inspection and in-memory predicate checks. Build and complete-reader execution are attributed to the controller’s candidate-bound gate. The full developer session transcript was withheld.; The offline fixture supplies engine handle creation, action names, playback flags and command completion. Visible playback, native timing and actual roll or damage counts remain unobserved on H.

#### Summary

- **Delivered behavior:** Offline evidence supports stock Bite adoption, same-action refresh and a single fallback for a missing initial handle. The command retains the source observed at the strike separately from the latest registration. The counter checks now distinguish the initial binding of each command.
- **Scope reviewed:** Draft PR #67 at the fixed candidate, covering MMR-07, S07 and the S08 Mounted Shield regression evidence.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N08 / NX-2. At normal play zoom, match the visible Bite to its logged strike, check facing, hooves and rider attachment, and observe whether a duplicate roll or damage event, stuck handle or artificial delay occurs.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21e67-c745-7881-8ff2-2d32282c2262`, duration 699.2s, response sha256 `07ded6afe3400149553303c8ed2a8807c117f23b4d0597457122a48ac2427622`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r08-e00afb`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "dda42b9c0c3119f63a9f912ac760ad5034e7cfe1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed, and both round-6 findings are fixed. Two issues still affect S07 evidence. The connected test validates a fixture-supplied Horse identity, and the schema-8 reader accepts malformed handle sources.\n\n## Continuation for Claude\n\nConfine the correction to the schema-8 reader and its offline fixtures. Give the producer and envelope one Horse identity, then pass both complete CaptureMountedOutcome rows into the reader. Validate the raw observed and latest sources as scalar strings using the same ordinal label comparison as the product. Preserve the verified counter rules, both refresh orders, historical controls and native attack path. Within the existing authorization and budget, capture the focused gate showing that the original writer rows pass and that a mismatched actor or malformed source fails. N08 remains with the owner\u2019s playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the controller receipt names the exact candidate and its log matches SHA-256 a7cde0c8c464c49750bc15dcd7e7a69487e83859aa3a68c90b4485bd2524d197. Fast exited 0 in 40.6 seconds with 15/15 focused children, 1548/0 component checks, 153/0 source checks and 52/0 Bite contract checks.",
      "The gate recorded readable Success outcomes with one child start for both refresh orders. The unreadable terminal control produced SecurityException and PlaybackAccepted=false. Presentation passed 239/0, Mounted Shield passed 114/0, rollback passed 68/0, and the packet, production and embedded icon hashes matched.",
      "Independently exercised the candidate\u2019s reader functions in memory. All 1,024 scalar-source combinations and 256 small integer-counter combinations matched the expected transitions. Nine malformed counter values were rejected.",
      "Replayed the reader\u2019s observation checks in memory. The fixture\u2019s actor ID passed, substituting the connected producer\u2019s actor ID failed ownership, and aligning the envelope identity passed. Separately, JSON arrays containing one valid source label passed in both the TB observed and RT latest positions."
    ],
    "inspected": [
      "Controller mission, prior-findings ledger, developer report, transcript notice, gate receipt and log, repository instructions, and the M..H feature diff.",
      "The correction from 84036b7c through H, including the handle binding, strike observation, metadata boundary, terminal projection, schema-8 writer and reader, connected fixture and Test-Harness consumer."
    ],
    "limitations": [
      "The review used read-only inspection and in-memory predicate checks. Build and complete-reader execution are attributed to the controller\u2019s candidate-bound gate. The full developer session transcript was withheld.",
      "The offline fixture supplies engine handle creation, action names, playback flags and command completion. Visible playback, native timing and actual roll or damage counts remain unobserved on H."
    ],
    "required_gaps": [
      "Complete-reader acceptance of the actual serialized Horse outcomes with their producer identity.",
      "Rejection of non-string and ordinally different handle-source values by the schema-8 reader."
    ],
    "source_inspection_only": [
      "The ordinary attack path classifies the registered Bite limb, delegates handle binding to the adapter and calls the native base OnAction once after observation.",
      "DescribeOutcome takes ActorId from the acting unit, and CaptureMountedOutcome serializes it. The connected fixture\u2019s field-copy list omits both actorId and action."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Test-ConnectedArtifact copies nine fields from each writer row. The producer creates horse-1, but the fixture retains actorId=horse and its own action. The TB ownership check at RuntimeHarness.Common.ps1 line 9085 rejects horse-1 against this envelope. The recorded positive therefore uses a different actor from the serialized outcome.",
      "blocking_rationale": "A missing or wrong producer actor ID can still receive a passing connected-test verdict. Horse ownership is an explicit condition of the complete reader.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The complete reader accepts the successful, readable Horse outcome emitted by the command, including its actor identity.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 732
      },
      "priority": "P2",
      "requested_correction": "Use one Horse ID in the producer and envelope. Assign the complete TB and RT Wire results to their respective outcome rows, retaining the unrelated world and UI fixture data.",
      "requested_evidence": "Show complete-reader acceptance for both refresh orders and the mixed adoption/supply sequence with the writer rows intact. Changing only the TB actor ID should fail the ownership check.",
      "title": "The connected fixture still substitutes the Horse actor identity",
      "trigger": "A successful supply-and-refresh outcome is serialized and inserted into the complete schema-8 fixture."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The reader casts sources to string before validating them. A JSON array containing only \"stock-created\" passed the observation checks in either position. The transition helper also accepted a string with U+0000 inserted into that label, which the product\u2019s ordinal comparison rejects.",
      "blocking_rationale": "The external reader can award exact binding credit for a source value outside the production contract.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each observed and latest source is a scalar JSON string equal to stock-created or kmc-supplied under ordinal comparison.",
      "id": "R8-F1",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 8835
      },
      "priority": "P2",
      "requested_correction": "Validate the four raw source values before string conversion or typed parameter binding. Use ordinal equality for the two allowed labels, then apply the existing transition table.",
      "requested_evidence": "Exercise an array-valued TB observed source, an array-valued RT latest source and an ordinally different scalar in the complete-reader fixture. Each should fail while the corresponding canonical strings pass.",
      "title": "Malformed handle sources receive exact binding credit",
      "trigger": "A schema-8 artifact reports a supply refreshed to stock before observation, with valid counts but a malformed observed or latest source."
    },
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "The label says create/adopt/refresh, but the line prints only creation and adoption counts.",
      "blocking_rationale": "The binding reports and source transitions already establish that the refresh occurred.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The counter names in the diagnostic match the values printed.",
      "id": "R8-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 766
      },
      "priority": "P3",
      "requested_correction": "Label the two values create/adopt.",
      "requested_evidence": "The next ordinary focused log should show two names for the two counts.",
      "title": "The fixture log labels a third counter that it does not print",
      "trigger": "The connected producer prints its per-sequence summary."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "Production binding, observation, refresh and terminal projection now produce the requested readable Bite fields. The remaining gap is the final transfer. The fixture supplies the actor ID that satisfies the complete reader\u2019s ownership check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F1",
      "rationale": "An initial supply now permits stock at both observation and terminal after a refresh before the strike. The gate exercised both refresh orders in both TB and RT positions and retained the stock-to-supplied rejection."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "All six raw counters are validated before conversion. TB and the component-wise RT delta each require exactly one creation or one adoption, with zero rejections. Both decreasing sequences and fractional inputs failed, and TB adoption followed by RT supply passed."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r08-e00afb",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline evidence supports stock Bite adoption, same-action refresh and a single fallback for a missing initial handle. The command retains the source observed at the strike separately from the latest registration. The counter checks now distinguish the initial binding of each command.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08 / NX-2. At normal play zoom, match the visible Bite to its logged strike, check facing, hooves and rider attachment, and observe whether a duplicate roll or damage event, stuck handle or artificial delay occurs."
    ],
    "scope_reviewed": "Draft PR #67 at the fixed candidate, covering MMR-07, S07 and the S08 Mounted Shield regression evidence.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
