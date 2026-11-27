<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r06-review part=1/1 sha256=85e78d6cd69a39aca100d060c2c07d3869235c295e61abe29b39cb43f5cceeab -->
### Codex GPT-6.2 Astra — review — round 6 — candidate `84036b7c32`

**Decision:** `changes_required` — Five of the six prior findings are resolved. The focused gate passed, but the schema-8 reader rejects a valid stock refresh before the Bite strike and accepts impossible per-command counts. R4-F2 also remains open because the complete reader receives a synthetic outcome separate from the one the production writer serialized.

## Continuation for Claude

If further work is authorized under the existing limits, derive each command’s initial binding from exact create and adopt counts, using the difference between the RT and TB snapshots for RT. Validate the observed and latest sources against that initial binding, including a same-action stock refresh before or after observation. Drive both orders through one offline fixture that substitutes the Unity metadata read, checks PlaybackAccepted, and passes the resulting Horse fields through CaptureMountedOutcome and the complete reader. Preserve schema 8, historical verdicts, the single native OnAction delegation and native attack timing. Record the positive and negative cases in the findings against the corrected candidate. N08 remains an owner playtest observation.

#### Findings

**R4-F2** · P2 · evidence_gap · blocks readiness — The complete-reader positive still uses a different outcome from the writer
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:548`; acceptance: MMR-07, S07
- Trigger: The fixture observes a supplied Bite and then tests a stock refresh after that strike.
- Expected: One supplied Bite is observed, its registration refreshes to a distinct stock handle of the same action, and the successful readable outcome from that sequence passes the complete schema-8 reader.
- Actual: Deliver seeds the two handles and source labels directly and requests the minimal outcome, which lacks an actor ID and has zero child starts. The gate prints `refreshed playback accepted offline=False (SecurityException)`. Test-Harness.ps1 separately fills in successful Horse outcomes, sources and counts for its complete-reader positive.
- Why it blocks / does not: The tests establish that the reader accepts the chosen labels. They leave unverified whether a readable refresh produced by the binding and writer reaches that verdict.
- Requested correction: Drive supply, observation and refresh through the production binding and projection. Substitute the engine-only metadata read at its boundary. Assert readability before serialization and feed those resulting Horse fields and counts into the existing complete-reader fixture.
- Requested evidence: Show one creation, a same-action refresh, PlaybackAccepted=true, a successful Horse outcome with one child start, and Assert-KmcHorseNativeControlsUxEvidence accepting the serialized result. Retain the historical positive and the unrecorded negative.

**R6-F1** · P2 · defect · blocks readiness — A stock refresh before the Bite strike is rejected as a missing adoption
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:8802`; acceptance: MMR-07, S07
- Trigger: KMC supplies the first handle and the stock planner refreshes it to the same Bite action before OnAction captures the observed source.
- Expected: An initial supply followed by a same-action stock refresh before observation is valid with one creation and zero adoptions for that command.
- Actual: A refresh increments HandleRefreshCount and changes the recorded source to stock-created. If the Bite then acts with that refreshed handle, the observed and latest sources are both stock-created. The reader requires at least one adoption and rejected this sequence in both TB and RT.
- Why it blocks / does not: A supported binding sequence can produce a readable successful outcome that fails external validation. The historical writer could also emit this stock/stock combination after an initial supply.
- Requested correction: Use the counts to identify how the command first bound a handle. For an initial supply, allow stock-created at both observation and terminal when the refresh has already occurred. Continue rejecting an observed stock handle followed by a claimed supply.
- Requested evidence: Show the reader accepting supply → refresh → observe and supply → observe → refresh for the relevant TB and RT counts. Keep ordinary adoption and stock-to-supplied rejection as controls.

**R6-F2** · P2 · defect · blocks readiness — A decreasing cumulative counter can conceal two bindings in one command
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:9087`; acceptance: MMR-07, S07
- Trigger: The TB and RT totals sum to one and two respectively, but an individual counter decreases between the snapshots.
- Expected: Raw counters are nonnegative JSON integers. The TB counts and the RT-minus-TB counts must each describe exactly one creation or exactly one adoption.
- Actual: The PASS checks accepted TB counts of zero creates and one adoption followed by RT cumulative counts of two creates and zero adoptions. That gives the RT command two creates and minus one adoption. The reverse case passed too. TB counts of 0.4 creates and 0.6 adoptions also passed after conversion to long.
- Why it blocks / does not: A duplicate binding can receive exactly-once credit when a decrease in the other counter offsets it.
- Requested correction: Validate the raw handle counters before casting. Require each command’s create/adopt pair to be exactly (1, 0) or (0, 1) before evaluating its source transition.
- Requested evidence: Reject cumulative sequences (0, 1) → (2, 0) and (1, 0) → (0, 2), plus fractional input. Accept one TB adoption followed by one RT supply with cumulative counts (0, 1) → (1, 1).

#### Prior finding assessments

- **R3-F2** — `fix_verified` — The discriminator now covers all five current-only outcome fields. The requested partial removals fail while the two supported historical shapes retain their verdicts.
- **R4-F2** — `fix_rejected` — The complete-reader fixture now covers the intended labels. The compiled writer fixture still produces an unreadable minimal outcome, and its Horse fields never reach that complete-reader test.
- **R4-F3** — `fix_verified` — Register passes the captured Bite into CompleteRegistration. The gate read the published identity back from the service and classified the untagged weapon as Bite after success. Injected selection and contract failures both yielded Unavailable, and direct rollback withdrew a published identity.
- **R5-F1** — `fix_verified` — Stock-to-supplied and a claimed supply with zero creations now fail. The RT check uses its own increase over TB. R6-F1 and R6-F2 identify distinct errors in the new transition check.
- **R5-F2** — `fix_verified` — Test-Harness declares Configuration with a Release default. Candidate forwards it through Test-Tiers, Full forwards it through Test.ps1, and the legacy case passes it to the Bite child. The gate printed the Debug and Release child arguments and corresponding DLL paths.
- **R5-F3** — `fix_verified` — The script checks whether Only was explicitly bound before setup. The gated dispatcher fixture covered omission, exact match, unknown name and explicit empty input. Empty input ran zero case bodies.

#### Evidence assessment

- **Inspected:** The mission, repository validation policy, current handoff, feature diff, round-5-to-round-6 corrections, six pending findings, developer report, controller receipt and focused log.; The Horse registration, binding, refresh, strike observation, terminal projection, schema-8 writer, PowerShell readers and focused fixtures, including the historical writer’s source semantics.; The Candidate and Full configuration routes, harness selector and committed single-case logs.
- **Executed verification:** The controller receipt binds the Release Fast gate with Test-MmrHorseBiteFocused.ps1 to H. It exited 0 in 40.6 seconds with an unchanged worktree. I verified the log SHA-256 against the receipt.; The gate recorded 1,548 component checks, 153 source checks, 50 Bite contract checks, 8 harness-selection checks and 239 presentation checks, all passing. All 15 focused children passed. The Mounted Shield check matched the packet, production and embedded icon bytes.; I exercised the candidate’s schema-8 PASS observation checks in memory with the existing harness fixture. Ordinary stock adoption and supply followed by refresh after observation passed. Refresh before observation failed in both TB and RT. Two impossible cumulative counter sequences and fractional TB counts passed.; I evaluated the Mount Primary playback clause on current, partially stripped and historical rows. The current positive and both historical positives passed. Missing source, missing readability, both mixed-format negatives and an unrecorded source failed.
- **Source inspection only:** RefreshStockCreatedHorsePrimaryAnimation changes the command’s recorded source to stock-created. The adapter increments HandleRefreshCount on that branch. ObserveHorseStrike captures the source of the handle present at the strike.; CaptureMountedOutcome preserves the 24-field schema-8 outcome. Readability is judged in process before a successful scenario can be credited.
- **Required gaps:** R4-F2 requires a readable refresh from the production binding path whose serialized Horse fields reach the complete reader.; R6-F1 requires acceptance of a supplied handle refreshed to stock before observation.; R6-F2 requires exact integer counts and exactly one initial binding per command, including the RT delta.
- **Limitations:** Reviewer execution was read-only. The in-memory reader checks exercised evidence values without filesystem or manifest operations.; The controller withheld the mixed foreground transcript. The committed single-case logs report their results, while the controller receipt independently identifies the candidate for the Fast gate.; The gate reports nativeProof=false. Visible playback, native timing and gameplay outcomes remain unobserved on H.

#### Summary

- **Delivered behavior:** The candidate scopes binding to the registered Horse Bite limb, preserves stock handles, supplies a single eligible fallback, and records strike and terminal diagnostics. The compiled checks confirm one native OnAction delegation. Registration now publishes the Bite identity after selection and validation succeed.
- **Scope reviewed:** Final PR-5 review of MMR-07 and S07, the S08 regression evidence, and the six pending corrections at the fixed candidate.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether native clip speed or blending makes the installed Bite difficult to see remains open.
- **Remaining owner review:** For N08, compare the logged real Bite with visible playback at normal zoom. Observe facing, the hoof sequence, rider attachment, attack timing and roll/damage count in the same encounter.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a218ed-f2cf-71c2-b701-cfe6dbd1a6db`, duration 654.8s, response sha256 `68fd46aba1ece5d3f012caeb0de42f6a520cde98ed60efe48cd865565447dd7a`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r06-81c518`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "84036b7c324161c7fb9eb4c607cc5f442da1364c",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Five of the six prior findings are resolved. The focused gate passed, but the schema-8 reader rejects a valid stock refresh before the Bite strike and accepts impossible per-command counts. R4-F2 also remains open because the complete reader receives a synthetic outcome separate from the one the production writer serialized.\n\n## Continuation for Claude\n\nIf further work is authorized under the existing limits, derive each command\u2019s initial binding from exact create and adopt counts, using the difference between the RT and TB snapshots for RT. Validate the observed and latest sources against that initial binding, including a same-action stock refresh before or after observation. Drive both orders through one offline fixture that substitutes the Unity metadata read, checks PlaybackAccepted, and passes the resulting Horse fields through CaptureMountedOutcome and the complete reader. Preserve schema 8, historical verdicts, the single native OnAction delegation and native attack timing. Record the positive and negative cases in the findings against the corrected candidate. N08 remains an owner playtest observation.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the Release Fast gate with Test-MmrHorseBiteFocused.ps1 to H. It exited 0 in 40.6 seconds with an unchanged worktree. I verified the log SHA-256 against the receipt.",
      "The gate recorded 1,548 component checks, 153 source checks, 50 Bite contract checks, 8 harness-selection checks and 239 presentation checks, all passing. All 15 focused children passed. The Mounted Shield check matched the packet, production and embedded icon bytes.",
      "I exercised the candidate\u2019s schema-8 PASS observation checks in memory with the existing harness fixture. Ordinary stock adoption and supply followed by refresh after observation passed. Refresh before observation failed in both TB and RT. Two impossible cumulative counter sequences and fractional TB counts passed.",
      "I evaluated the Mount Primary playback clause on current, partially stripped and historical rows. The current positive and both historical positives passed. Missing source, missing readability, both mixed-format negatives and an unrecorded source failed."
    ],
    "inspected": [
      "The mission, repository validation policy, current handoff, feature diff, round-5-to-round-6 corrections, six pending findings, developer report, controller receipt and focused log.",
      "The Horse registration, binding, refresh, strike observation, terminal projection, schema-8 writer, PowerShell readers and focused fixtures, including the historical writer\u2019s source semantics.",
      "The Candidate and Full configuration routes, harness selector and committed single-case logs."
    ],
    "limitations": [
      "Reviewer execution was read-only. The in-memory reader checks exercised evidence values without filesystem or manifest operations.",
      "The controller withheld the mixed foreground transcript. The committed single-case logs report their results, while the controller receipt independently identifies the candidate for the Fast gate.",
      "The gate reports nativeProof=false. Visible playback, native timing and gameplay outcomes remain unobserved on H."
    ],
    "required_gaps": [
      "R4-F2 requires a readable refresh from the production binding path whose serialized Horse fields reach the complete reader.",
      "R6-F1 requires acceptance of a supplied handle refreshed to stock before observation.",
      "R6-F2 requires exact integer counts and exactly one initial binding per command, including the RT delta."
    ],
    "source_inspection_only": [
      "RefreshStockCreatedHorsePrimaryAnimation changes the command\u2019s recorded source to stock-created. The adapter increments HandleRefreshCount on that branch. ObserveHorseStrike captures the source of the handle present at the strike.",
      "CaptureMountedOutcome preserves the 24-field schema-8 outcome. Readability is judged in process before a successful scenario can be credited."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Deliver seeds the two handles and source labels directly and requests the minimal outcome, which lacks an actor ID and has zero child starts. The gate prints `refreshed playback accepted offline=False (SecurityException)`. Test-Harness.ps1 separately fills in successful Horse outcomes, sources and counts for its complete-reader positive.",
      "blocking_rationale": "The tests establish that the reader accepts the chosen labels. They leave unverified whether a readable refresh produced by the binding and writer reaches that verdict.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "One supplied Bite is observed, its registration refreshes to a distinct stock handle of the same action, and the successful readable outcome from that sequence passes the complete schema-8 reader.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 548
      },
      "priority": "P2",
      "requested_correction": "Drive supply, observation and refresh through the production binding and projection. Substitute the engine-only metadata read at its boundary. Assert readability before serialization and feed those resulting Horse fields and counts into the existing complete-reader fixture.",
      "requested_evidence": "Show one creation, a same-action refresh, PlaybackAccepted=true, a successful Horse outcome with one child start, and Assert-KmcHorseNativeControlsUxEvidence accepting the serialized result. Retain the historical positive and the unrecorded negative.",
      "title": "The complete-reader positive still uses a different outcome from the writer",
      "trigger": "The fixture observes a supplied Bite and then tests a stock refresh after that strike."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "A refresh increments HandleRefreshCount and changes the recorded source to stock-created. If the Bite then acts with that refreshed handle, the observed and latest sources are both stock-created. The reader requires at least one adoption and rejected this sequence in both TB and RT.",
      "blocking_rationale": "A supported binding sequence can produce a readable successful outcome that fails external validation. The historical writer could also emit this stock/stock combination after an initial supply.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An initial supply followed by a same-action stock refresh before observation is valid with one creation and zero adoptions for that command.",
      "id": "R6-F1",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 8802
      },
      "priority": "P2",
      "requested_correction": "Use the counts to identify how the command first bound a handle. For an initial supply, allow stock-created at both observation and terminal when the refresh has already occurred. Continue rejecting an observed stock handle followed by a claimed supply.",
      "requested_evidence": "Show the reader accepting supply \u2192 refresh \u2192 observe and supply \u2192 observe \u2192 refresh for the relevant TB and RT counts. Keep ordinary adoption and stock-to-supplied rejection as controls.",
      "title": "A stock refresh before the Bite strike is rejected as a missing adoption",
      "trigger": "KMC supplies the first handle and the stock planner refreshes it to the same Bite action before OnAction captures the observed source."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The PASS checks accepted TB counts of zero creates and one adoption followed by RT cumulative counts of two creates and zero adoptions. That gives the RT command two creates and minus one adoption. The reverse case passed too. TB counts of 0.4 creates and 0.6 adoptions also passed after conversion to long.",
      "blocking_rationale": "A duplicate binding can receive exactly-once credit when a decrease in the other counter offsets it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Raw counters are nonnegative JSON integers. The TB counts and the RT-minus-TB counts must each describe exactly one creation or exactly one adoption.",
      "id": "R6-F2",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 9087
      },
      "priority": "P2",
      "requested_correction": "Validate the raw handle counters before casting. Require each command\u2019s create/adopt pair to be exactly (1, 0) or (0, 1) before evaluating its source transition.",
      "requested_evidence": "Reject cumulative sequences (0, 1) \u2192 (2, 0) and (1, 0) \u2192 (0, 2), plus fractional input. Accept one TB adoption followed by one RT supply with cumulative counts (0, 1) \u2192 (1, 1).",
      "title": "A decreasing cumulative counter can conceal two bindings in one command",
      "trigger": "The TB and RT totals sum to one and two respectively, but an individual counter decreases between the snapshots."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F2",
      "rationale": "The discriminator now covers all five current-only outcome fields. The requested partial removals fail while the two supported historical shapes retain their verdicts."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "The complete-reader fixture now covers the intended labels. The compiled writer fixture still produces an unreadable minimal outcome, and its Horse fields never reach that complete-reader test."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F3",
      "rationale": "Register passes the captured Bite into CompleteRegistration. The gate read the published identity back from the service and classified the untagged weapon as Bite after success. Injected selection and contract failures both yielded Unavailable, and direct rollback withdrew a published identity."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F1",
      "rationale": "Stock-to-supplied and a claimed supply with zero creations now fail. The RT check uses its own increase over TB. R6-F1 and R6-F2 identify distinct errors in the new transition check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F2",
      "rationale": "Test-Harness declares Configuration with a Release default. Candidate forwards it through Test-Tiers, Full forwards it through Test.ps1, and the legacy case passes it to the Bite child. The gate printed the Debug and Release child arguments and corresponding DLL paths."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F3",
      "rationale": "The script checks whether Only was explicitly bound before setup. The gated dispatcher fixture covered omission, exact match, unknown name and explicit empty input. Empty input ran zero case bodies."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r06-81c518",
  "round": 6,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate scopes binding to the registered Horse Bite limb, preserves stock handles, supplies a single eligible fallback, and records strike and terminal diagnostics. The compiled checks confirm one native OnAction delegation. Registration now publishes the Bite identity after selection and validation succeed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "For N08, compare the logged real Bite with visible playback at normal zoom. Observe facing, the hoof sequence, rider attachment, attack timing and roll/damage count in the same encounter."
    ],
    "scope_reviewed": "Final PR-5 review of MMR-07 and S07, the S08 regression evidence, and the six pending corrections at the fixed candidate.",
    "unresolved_questions": [
      "Whether native clip speed or blending makes the installed Bite difficult to see remains open."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
