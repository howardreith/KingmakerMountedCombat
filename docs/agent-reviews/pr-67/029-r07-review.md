_Superseded record: 1 of 1 part(s) were published; it is not a current verdict._

<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r07-review part=1/1 sha256=1f51a8d5d78a00d3be92a9b3771b6f682e789a14019453ca540478bf858b14ca -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `dda42b9c0c`

**Decision:** `changes_required` — Both counter defects are fixed. Two issues still block readiness. The complete-reader test substitutes the Horse actor ID that determines its TB verdict, and the revised reader accepts malformed values as exact handle sources.

### Continuation for Claude

Keep the working binding and counter changes. Align the fixture’s Horse identity with the producer, then pass the serialized TB and RT Horse outcomes directly to the complete reader. Show that both refresh orders pass and that changing the TB actor ID makes the same fixture fail. Validate the four observed and latest source values as scalar JSON strings and compare them ordinally before accepting a transition. Retain the existing historical, adoption, unreadable and counter controls, and run the required focused gate on the corrected candidate. These corrections fit within the existing schema and offline fixture. Visible playback remains owner observation N08.

#### Findings

**R4-F2** · P2 · evidence_gap · blocks readiness — The complete-reader positive still substitutes the Horse actor identity
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:732`; acceptance: MMR-07, S07
- Trigger: The connected TB and RT outcomes are copied into the shared schema-8 fixture.
- Expected: The complete reader accepts the serialized Horse outcome from the binding sequence, including its action and actor ID, against an envelope identifying that same Horse.
- Actual: Test-ConnectedArtifact copies nine outcome fields and leaves action and actorId from the synthetic fixture. The producer’s actor is horse-1. The complete reader checks the fixture’s actor horse against its expected horse. Substituting horse-1 into that same TB ownership check makes it reject.
- Why it blocks / does not: The recorded PASS establishes that the produced animation fields and counts are compatible. It still leaves a reader-tested field from a different Horse outcome in the positive that was requested for R4-F2.
- Requested correction: Give the producer and envelope the same Horse identity and place the complete CaptureMountedOutcome results in mountedTurnHorseOutcome and mountedHorseOutcome. Keep unrelated world and UI observations in the fixture.
- Requested evidence: Show the complete reader accepting both refresh orders with the writer’s action, actor ID, result, child count and animation fields intact. Change only the serialized TB actor ID and show rejection.

**R7-F1** · P2 · defect · blocks readiness — Malformed source values receive exact stock-handle credit
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:8835`; acceptance: MMR-07, S07
- Trigger: A schema-8 artifact reports one initial supply and represents the observed or latest stock source with a singleton array or a string containing an ignored character.
- Expected: Each observed and latest source must be a scalar JSON string exactly equal to stock-created or kmc-supplied.
- Actual: The reader casts the source fields to string and uses PowerShell culture-sensitive comparisons. In the refresh-before-observation case, both ["stock-created"] and the JSON string "stock-\u0000created" passed as stock-created. The previous predicate rejected both for that same supply sequence.
- Why it blocks / does not: The revised validator can certify an exact handle source from malformed evidence. Its verdict then differs from the production playback predicate, which uses ordinal string equality.
- Requested correction: Check the raw types of the TB and RT observed and latest source fields before string conversion. Use ordinal comparisons for the two allowed values and their transitions.
- Requested evidence: On an otherwise valid supply followed by a stock refresh, show the complete reader accepting the exact scalar label and rejecting a singleton array and an embedded-NUL label in the observed and latest source positions.

**R7-F2** · P3 · optional_polish · non-blocking — The producer log names three counters and prints two
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:766`; acceptance: S07
- Trigger: The connected fixture prints its per-sequence producer summary.
- Expected: The counter label identifies the values printed.
- Actual: The label says create/adopt/refresh, but the line prints only creation and adoption, for example 1/0.
- Why it blocks / does not: The adapter separately logs the refresh, and the resulting source transition is present in the gate output.
- Requested correction: Label the pair create/adopt, or capture and print the adapter’s refresh count as the third value.
- Requested evidence: A producer line in the next focused run shows a matching label and value count.

#### Prior finding assessments

- **R4-F2** — `fix_rejected` — Supply, observation, refresh, terminal readability and adapter counts now come from the connected sequence. The complete-reader fixture still substitutes the TB action and actor ID. Its actor ID differs from the writer’s and directly affects the verdict.
- **R6-F1** — `fix_verified` — For valid source labels, an initial supply now admits stock/stock after a refresh before observation and supplied/stock after a later refresh. The candidate gate exercises both orders in both positions and retains the stock-to-supplied rejection.
- **R6-F2** — `fix_verified` — The reader validates raw counters before conversion, requires zero rejections and checks each command’s exact creation/adoption pair. The gate rejects both decreasing sequences and fractional counters, and accepts TB adoption followed by RT supply with cumulative counts (0,1) then (1,1).

#### Evidence assessment

- **Inspected:** Enrolled mission, repository validation policy and handoffs, feature diff, prior findings ledger, developer report, retained evidence and candidate-bound controller log.; Horse limb classification, handle supply/adoption/refresh, strike observation, terminal projection, schema-8 writers and reader, and the shared complete-reader fixture.
- **Executed verification:** Verified the controller receipt and matching log SHA-256 a7cde0c8c464c49750bc15dcd7e7a69487e83859aa3a68c90b4485bd2524d197. The required Fast focused gate ran at H, exited 0 in 40.6 seconds and passed 15 of 15 children.; The gate recorded 1,548 component passes, 153 source-validation passes, 52 Bite contract passes and 239 presentation contract passes. Mounted Shield passed 114 contract and 68 rollback checks. The packet, production and embedded icon hashes matched.; The gate recorded readable terminal outcomes with one child start for both refresh orders, three accepted composite artifacts, and PlaybackAccepted=False for the engine-name-read failure.; Independently evaluated the candidate’s PowerShell helpers in memory. All 1,024 TB/RT combinations of initial binding and tested scalar source labels matched the expected transitions. All 256 tested counter combinations matched the expected per-command counts, and all ten malformed counter representations were rejected.; Independently exercised the reader’s ownership and binding clauses. Using the producer’s actor ID with the submitted envelope changed acceptance to rejection. A singleton source array and a source string containing an embedded NUL both received binding credit.
- **Source inspection only:** Traced the native handle postfix and command-start entry points to SupplyExact, and the strike observer to the single base.OnAction delegation. The connected fixture enters at the binding boundary.; Traced the Horse actor ID from New-ConnectedHorse through DescribeOutcome and CaptureMountedOutcome to the fields selected by Test-ConnectedArtifact.
- **Required gaps:** R4-F2 requires complete-reader acceptance with the producer’s serialized Horse identity and action intact.; R7-F1 requires malformed source values to fail on the newly supported refresh-before-observation path.
- **Limitations:** The reviewer used read-only source inspection and in-memory predicate checks. The controller supplied the build and complete focused-gate execution.; The connected fixture uses detached engine objects and supplies native handle state and Unity name metadata. The gate reports nativeProof=false. Camera-visible playback, timing and damage delivery were not observed in game.

#### Summary

- **Delivered behavior:** The binding preserves a stock Bite handle, supplies one fallback for a proven Bite limb, and records a same-action stock refresh. Strike and terminal diagnostics distinguish the observed handle from the latest registration. The focused gate confirms the supported counter transitions and Mounted Shield regressions offline.
- **Scope reviewed:** Final PR-5 correction review for MMR-07, S07 and S08 at dda42b9c, including all three pending findings and the affected evidence consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the native routine’s shared animation speed makes the Bite too brief to read at normal zoom.
- **Remaining owner review:** For N08, correlate the logged Bite with visible playback at normal zoom. Check hoof animation, facing, rider attachment, ordinary attack timing and one roll and damage event per strike.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21a49-28d6-7631-a5ca-6fc6b9c459d2`, duration 600.9s, response sha256 `6c2d85ad88c91aa3a920e9da2d68104259079f45b95f550bf28e4bba8c8c8728`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r07-2943a2`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "dda42b9c0c3119f63a9f912ac760ad5034e7cfe1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both counter defects are fixed. Two issues still block readiness. The complete-reader test substitutes the Horse actor ID that determines its TB verdict, and the revised reader accepts malformed values as exact handle sources.\n\n### Continuation for Claude\n\nKeep the working binding and counter changes. Align the fixture\u2019s Horse identity with the producer, then pass the serialized TB and RT Horse outcomes directly to the complete reader. Show that both refresh orders pass and that changing the TB actor ID makes the same fixture fail. Validate the four observed and latest source values as scalar JSON strings and compare them ordinally before accepting a transition. Retain the existing historical, adoption, unreadable and counter controls, and run the required focused gate on the corrected candidate. These corrections fit within the existing schema and offline fixture. Visible playback remains owner observation N08.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the controller receipt and matching log SHA-256 a7cde0c8c464c49750bc15dcd7e7a69487e83859aa3a68c90b4485bd2524d197. The required Fast focused gate ran at H, exited 0 in 40.6 seconds and passed 15 of 15 children.",
      "The gate recorded 1,548 component passes, 153 source-validation passes, 52 Bite contract passes and 239 presentation contract passes. Mounted Shield passed 114 contract and 68 rollback checks. The packet, production and embedded icon hashes matched.",
      "The gate recorded readable terminal outcomes with one child start for both refresh orders, three accepted composite artifacts, and PlaybackAccepted=False for the engine-name-read failure.",
      "Independently evaluated the candidate\u2019s PowerShell helpers in memory. All 1,024 TB/RT combinations of initial binding and tested scalar source labels matched the expected transitions. All 256 tested counter combinations matched the expected per-command counts, and all ten malformed counter representations were rejected.",
      "Independently exercised the reader\u2019s ownership and binding clauses. Using the producer\u2019s actor ID with the submitted envelope changed acceptance to rejection. A singleton source array and a source string containing an embedded NUL both received binding credit."
    ],
    "inspected": [
      "Enrolled mission, repository validation policy and handoffs, feature diff, prior findings ledger, developer report, retained evidence and candidate-bound controller log.",
      "Horse limb classification, handle supply/adoption/refresh, strike observation, terminal projection, schema-8 writers and reader, and the shared complete-reader fixture."
    ],
    "limitations": [
      "The reviewer used read-only source inspection and in-memory predicate checks. The controller supplied the build and complete focused-gate execution.",
      "The connected fixture uses detached engine objects and supplies native handle state and Unity name metadata. The gate reports nativeProof=false. Camera-visible playback, timing and damage delivery were not observed in game."
    ],
    "required_gaps": [
      "R4-F2 requires complete-reader acceptance with the producer\u2019s serialized Horse identity and action intact.",
      "R7-F1 requires malformed source values to fail on the newly supported refresh-before-observation path."
    ],
    "source_inspection_only": [
      "Traced the native handle postfix and command-start entry points to SupplyExact, and the strike observer to the single base.OnAction delegation. The connected fixture enters at the binding boundary.",
      "Traced the Horse actor ID from New-ConnectedHorse through DescribeOutcome and CaptureMountedOutcome to the fields selected by Test-ConnectedArtifact."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Test-ConnectedArtifact copies nine outcome fields and leaves action and actorId from the synthetic fixture. The producer\u2019s actor is horse-1. The complete reader checks the fixture\u2019s actor horse against its expected horse. Substituting horse-1 into that same TB ownership check makes it reject.",
      "blocking_rationale": "The recorded PASS establishes that the produced animation fields and counts are compatible. It still leaves a reader-tested field from a different Horse outcome in the positive that was requested for R4-F2.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The complete reader accepts the serialized Horse outcome from the binding sequence, including its action and actor ID, against an envelope identifying that same Horse.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 732
      },
      "priority": "P2",
      "requested_correction": "Give the producer and envelope the same Horse identity and place the complete CaptureMountedOutcome results in mountedTurnHorseOutcome and mountedHorseOutcome. Keep unrelated world and UI observations in the fixture.",
      "requested_evidence": "Show the complete reader accepting both refresh orders with the writer\u2019s action, actor ID, result, child count and animation fields intact. Change only the serialized TB actor ID and show rejection.",
      "title": "The complete-reader positive still substitutes the Horse actor identity",
      "trigger": "The connected TB and RT outcomes are copied into the shared schema-8 fixture."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The reader casts the source fields to string and uses PowerShell culture-sensitive comparisons. In the refresh-before-observation case, both [\"stock-created\"] and the JSON string \"stock-\\u0000created\" passed as stock-created. The previous predicate rejected both for that same supply sequence.",
      "blocking_rationale": "The revised validator can certify an exact handle source from malformed evidence. Its verdict then differs from the production playback predicate, which uses ordinal string equality.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each observed and latest source must be a scalar JSON string exactly equal to stock-created or kmc-supplied.",
      "id": "R7-F1",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 8835
      },
      "priority": "P2",
      "requested_correction": "Check the raw types of the TB and RT observed and latest source fields before string conversion. Use ordinal comparisons for the two allowed values and their transitions.",
      "requested_evidence": "On an otherwise valid supply followed by a stock refresh, show the complete reader accepting the exact scalar label and rejecting a singleton array and an embedded-NUL label in the observed and latest source positions.",
      "title": "Malformed source values receive exact stock-handle credit",
      "trigger": "A schema-8 artifact reports one initial supply and represents the observed or latest stock source with a singleton array or a string containing an ignored character."
    },
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "The label says create/adopt/refresh, but the line prints only creation and adoption, for example 1/0.",
      "blocking_rationale": "The adapter separately logs the refresh, and the resulting source transition is present in the gate output.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The counter label identifies the values printed.",
      "id": "R7-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 766
      },
      "priority": "P3",
      "requested_correction": "Label the pair create/adopt, or capture and print the adapter\u2019s refresh count as the third value.",
      "requested_evidence": "A producer line in the next focused run shows a matching label and value count.",
      "title": "The producer log names three counters and prints two",
      "trigger": "The connected fixture prints its per-sequence producer summary."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "Supply, observation, refresh, terminal readability and adapter counts now come from the connected sequence. The complete-reader fixture still substitutes the TB action and actor ID. Its actor ID differs from the writer\u2019s and directly affects the verdict."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F1",
      "rationale": "For valid source labels, an initial supply now admits stock/stock after a refresh before observation and supplied/stock after a later refresh. The candidate gate exercises both orders in both positions and retains the stock-to-supplied rejection."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "The reader validates raw counters before conversion, requires zero rejections and checks each command\u2019s exact creation/adoption pair. The gate rejects both decreasing sequences and fractional counters, and accepts TB adoption followed by RT supply with cumulative counts (0,1) then (1,1)."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r07-2943a2",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The binding preserves a stock Bite handle, supplies one fallback for a proven Bite limb, and records a same-action stock refresh. Strike and terminal diagnostics distinguish the observed handle from the latest registration. The focused gate confirms the supported counter transitions and Mounted Shield regressions offline.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "For N08, correlate the logged Bite with visible playback at normal zoom. Check hoof animation, facing, rider attachment, ordinary attack timing and one roll and damage event per strike."
    ],
    "scope_reviewed": "Final PR-5 correction review for MMR-07, S07 and S08 at dda42b9c, including all three pending findings and the affected evidence consumers.",
    "unresolved_questions": [
      "Whether the native routine\u2019s shared animation speed makes the Bite too brief to read at normal zoom."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
