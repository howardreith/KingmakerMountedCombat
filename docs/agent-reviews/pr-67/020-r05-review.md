<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r05-review part=1/1 sha256=160991645bcb71207ab71df0cab081b8159cf52e90bebbda088768f64785eb69 -->
### Codex GPT-6.2 Astra — review — round 5 — candidate `805ad398d5`

**Decision:** `changes_required` — The Release focused gate passed. Both evidence readers still accept records that fail to establish valid Bite playback. The legacy Horse check can also read the wrong DLL or run the full harness for an empty selection. Two requested observations remain unverified.

## Continuation for Claude

Recognize the supported historical Mount Primary formats before granting compatibility allowances. For schema 8, use each command’s create/adopt counts and the allowed supplied-to-stock refresh direction to check the observed and latest sources. Exercise a readable refresh through the existing complete Horse reader fixture. Exercise successful publication and a late registration failure using the identity the service actually publishes.

Carry the selected build configuration into the legacy Horse check and reject an explicitly empty selector before harness setup. Keep the corrections within MMR-07 and its offline validation. Preserve the schema-8 wire format, frozen evidence, native OnAction delegation, hoof controls and Mounted Shield coverage. Return focused regression results and the required Fast receipt bound to the corrected candidate. N08 remains with the owner.

#### Findings

**R3-F2** · P2 · defect · blocks readiness — Partially stripped current Mount Primary evidence still passes as historical
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:8384`; acceptance: MMR-07, S07
- Trigger: A current outcome loses attackAnimationObservation and attackAnimationHandleRegistered along with one required proof field.
- Expected: Only a supported historical field set receives its compatibility allowance. A current outcome must supply its observed source and readability proof.
- Actual: The reader recognizes current format by just two property names. With those names and attackAnimationHandleSource removed, the decision accepted a row that still carried attackAnimationRegisteredSource, attackAnimationFactsUnreadable and a horseBiteTerminal identifying the source as unrecorded. A row retaining the current registration and terminal fields also passed with its readability proof removed.
- Why it blocks / does not: An identifiable current row can receive historical credit after losing required playback proof.
- Requested correction: Identify the supported historical animation field sets explicitly, or use an equally reliable format discriminator. Reject mixed current and historical shapes that lack required proof.
- Requested evidence: Keep the passing single-removal tests. Add partially stripped current outcomes retaining other current-only fields, alongside the two supported historical positives.

**R4-F2** · P2 · evidence_gap · blocks readiness — The refresh fixture’s positive cannot pass the complete reader
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:538`; acceptance: MMR-07, S07
- Trigger: The new R4-F2 check serializes a minimal terminal outcome and evaluates only the last schema-8 source predicate.
- Expected: A readable supplied Bite followed by a same-action stock refresh produces a successful outcome and snapshot accepted by the complete schema-8 reader.
- Actual: The refresh fixture calls Deliver with Detailed=false. Its minimal outcome has no actor ID and zero child starts. Schema8 then tests only the source/count expression against hand-built snapshots. The complete reader rejects that outcome at its earlier ownership and cardinality check. The fixture also skips PlaybackAccepted for the reference-distinct detached handle.
- Why it blocks / does not: The corrected expression accepts the intended source pair, but the reported positive cannot demonstrate the requested writer-to-reader result.
- Requested correction: Drive the supply, observation and refresh through the production binding path. Check readability before serialization and pass the resulting Horse fields through the existing complete native-controls reader fixture.
- Requested evidence: Show Assert-KmcHorseNativeControlsUxEvidence accepting the readable refresh and historical positive, with an unrecorded observation rejected. A bounded offline fixture is sufficient.

**R4-F3** · P2 · evidence_gap · blocks readiness — The registration fixture supplies the identity it is meant to verify
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:516`; acceptance: MMR-07, S07
- Trigger: The round-5 contract labels a classifier result as published before it exercises any publication path.
- Expected: Successful registration publishes the captured Bite identity. A late selection or validation failure leaves the classifier without that identity.
- Actual: Register now publishes after validation, and direct rollback clears the property before cleanup faults. The positive classifier call receives the fixture weapon’s GUID directly. The failure case sets the static property manually and calls rollback on an uninitialized service. Neither case obtains its starting identity from the production registration decision.
- Why it blocks / does not: The checks establish setter order and direct withdrawal. They would still pass if the success path supplied the wrong GUID, and they do not exercise the requested late registration failure.
- Requested correction: Exercise the production registration finalization step offline with a successful validation and an injected late failure. Read RegisteredBiteGuid back from the service for both classifier calls.
- Requested evidence: Show the original untagged Bite classifying as Bite after successful publication and as Unavailable after the late failure. Retain the direct rollback check.

**R5-F1** · P2 · defect · blocks readiness — Schema 8 accepts a supplied handle with zero creations
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:9074`; acceptance: MMR-07, S07
- Trigger: A schema-8 snapshot contains individually recognized source labels that contradict the binding transition or creation count.
- Expected: A stock adoption can refresh only to stock. A supplied-to-stock transition requires a supply for that command.
- Actual: The reader accepts each source independently from a two-value allowlist. With zero creates and one adoption, changing only the latest TB source from stock-created to kmc-supplied passed. Reporting the observed source as kmc-supplied with the same zero creates also passed.
- Why it blocks / does not: Schema 8 can credit a supplied handle even when its own counters establish that no handle was supplied.
- Requested correction: Validate the two sources against the permitted transition and the command’s create/adopt counts, using the difference between cumulative snapshots for RT.
- Requested evidence: Retain stock adoption and a genuine supplied-to-stock refresh as positives. Reject stock-to-supplied and a claimed supply with zero creates for the relevant command.

**R5-F2** · P2 · defect · blocks readiness — The legacy Horse check always opens the Release DLL
- Location: `scripts/Test-Harness.ps1:10501`; acceptance: MMR-07, S07
- Trigger: Candidate or Full selects Debug while the new legacy Horse case invokes Test-MountedAlpha2BiteContract.ps1.
- Expected: The compiled binding check reads the DLL selected by its caller.
- Actual: Test-Harness.ps1 declares only Only and invokes the Bite contract without Configuration. The child defaults to bin/Release/KingmakerMountedCombat.dll. Test-Tiers forwards Configuration only to scripts declaring it, and the Full umbrella calls Test-Harness without it.
- Why it blocks / does not: A Debug qualification can credit the legacy Horse case for behavior exercised on stale Release bytes.
- Requested correction: Carry Configuration through Test-Harness and its Full caller to the Bite child. Preserve Release as the default for ordinary standalone use.
- Requested evidence: Show the actual legacy-case child arguments and resolved product path for Debug and Release. A focused routing check suffices.

**R5-F3** · P2 · defect · blocks readiness — An explicitly empty Only argument selects every harness case
- Location: `scripts/HarnessDispatch.ps1:9`; acceptance: S07, S08
- Trigger: A caller invokes Test-Harness.ps1 -Only $caseName with an empty or null case name.
- Expected: Omitting Only selects the full harness. Explicitly supplying an empty name fails before cases run.
- Actual: Initialize-HarnessSelection converts an empty string to null, which selects every case and disables the exact-match check. The fixture itself uses an explicit empty string to represent an ordinary invocation.
- Why it blocks / does not: An empty case-name variable silently broadens a requested one-case run to the full harness.
- Requested correction: Distinguish omission from an explicitly empty or null argument at the script boundary and reject the latter before setup.
- Requested evidence: Exercise omission, an exact name, an unknown name and an explicitly empty name. The empty selection must fail with zero case bodies executed.

#### Prior finding assessments

- **R2-F2** — `fix_verified` — The actual classifier returned Unavailable for both missing first-slot references and for a missing weapon identity. The observation step cleared the earlier handle and source, emitted one identifiable line and delivered rejected terminal playback. The confirmed hoof preserved the prior Bite.
- **R3-F2** — `fix_rejected` — Both requested single-field removals now fail. I reproduced the same historical-format misclassification on rows retaining other current-only fields.
- **R3-F6** — `fix_verified` — The legacy case now invokes the compiled binding contract. Its three required cases exercise zero writes for ambiguous Bite actions, one registration for a repeated handle and rejection of a foreign manager. R5-F2 concerns the newly introduced child invocation’s build selection.
- **R4-F1** — `fix_verified` — The dispatcher no longer reads the inherited selector. The focused fixture retained both neighboring cases on ordinary invocation and rejected an unknown name. R5-F3 concerns a separately reproducible explicit-argument case.
- **R4-F2** — `fix_rejected` — The erroneous equality check is gone and the legitimate refresh now passes the source predicate. The submitted writer result cannot reach that predicate through the complete reader. R5-F1 records the distinct false acceptance introduced by the relaxed comparison.
- **R4-F3** — `fix_rejected` — The publication and withdrawal statements are correctly placed in source. The recorded run establishes direct rollback withdrawal but does not exercise a late failure through registration or classify using a successfully published value.

#### Evidence assessment

- **Inspected:** Controller mission, request, prior findings, developer report, transcript notice, feature diff and candidate receipt. Compared the round-5 correction with 4242dd3e and traced its affected consumers.; Repository validation policy and handoffs, Horse registration and rollback, limb classification, handle binding, strike observation, terminal delivery, both evidence writers and readers, harness dispatch and tier callers.
- **Executed verification:** The controller receipt identifies the exact candidate, an unchanged worktree, exit 0 and 49.3 seconds for Fast with Test-MmrHorseBiteFocused.ps1. I verified the supplied log’s SHA-256 against the receipt.; The Release run reported 1,548 component checks, 153 source checks, 3 installed-contract checks, 48 Bite checks, 239 presentation checks and 4 harness-selection checks, all passing. All 15 focused children passed. Mounted Shield reported 114 contract checks and 68 rollback checks, including byte identity of the embedded icon.; I evaluated the candidate’s Mount Primary decision in memory. A single missing source was rejected. Removing its two format markers as well made a recognizably current unrecorded row pass. The analogous missing-readability row also passed.; I compared the previous and current schema-8 source predicates in memory. The candidate accepted the legitimate supplied-to-stock refresh and rejected an unrecorded source. It also accepted two zero-creation cases that the previous predicate rejected.; A read-only PowerShell parameter-binding check confirmed that an omitted optional string and an explicitly supplied empty or null string all satisfy IsNullOrEmpty. PowerShell retains whether the parameter was supplied.
- **Source inspection only:** Traced Debug and Release argument propagation from Test-Tiers.ps1 and Test.ps1 into the newly nested Bite contract.; Inspected the successful Register path and its exception caller. The recorded rollback execution started from a manually seeded identity.; Inspected the complete schema-8 reader and the fixture’s extracted predicate. Reviewer reproductions evaluated reader conditions in memory.
- **Required gaps:** Reader acceptance of a complete, readable supplied-to-stock result and rejection of contradictory source counts and partially stripped current evidence.; Successful registration and late failure observed through the production publication path and its resulting classifier input.; Focused verification of build-configuration routing and explicit empty-selector refusal.
- **Limitations:** The foreground developer transcript was withheld. The committed isolated harness logs report one exact-case pass and one unknown-name failure, but contain no candidate SHA.; The controller run exercised Release offline. Candidate, Full, rendered playback and native combat timing were not executed for this review.

#### Summary

- **Delivered behavior:** The binding preserves the exact stock Bite handle, permits same-action refresh and supplies one fallback for a missing initial handle. Missing limb evidence clears stale playback. The per-strike and terminal diagnostics retain the single native OnAction delegation.
- **Scope reviewed:** PR #67’s final MMR-07 source and offline checkpoint, S07 binding and diagnostics, S08 regression coverage, and all six pending findings.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the native routine’s shared SpeedScale makes BiteAttack too brief to read at normal play zoom.
- **Remaining owner review:** N08 / NX-2 at normal zoom. Correlate the logged Bite with visible playback, facing, the hoof sequence and rider attachment. Check ordinary timing and roll/damage cardinality in the encounter.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a218d5-06c9-7af0-9942-858b9becac08`, duration 909.5s, response sha256 `dc45e37f58ed0aac3f889dae5b0ef801f395d719435732cedbb721a9569e837c`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r05-abb351`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "805ad398d55618abd613d6107a68b861fa82cad7",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The Release focused gate passed. Both evidence readers still accept records that fail to establish valid Bite playback. The legacy Horse check can also read the wrong DLL or run the full harness for an empty selection. Two requested observations remain unverified.\n\n## Continuation for Claude\n\nRecognize the supported historical Mount Primary formats before granting compatibility allowances. For schema 8, use each command\u2019s create/adopt counts and the allowed supplied-to-stock refresh direction to check the observed and latest sources. Exercise a readable refresh through the existing complete Horse reader fixture. Exercise successful publication and a late registration failure using the identity the service actually publishes.\n\nCarry the selected build configuration into the legacy Horse check and reject an explicitly empty selector before harness setup. Keep the corrections within MMR-07 and its offline validation. Preserve the schema-8 wire format, frozen evidence, native OnAction delegation, hoof controls and Mounted Shield coverage. Return focused regression results and the required Fast receipt bound to the corrected candidate. N08 remains with the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt identifies the exact candidate, an unchanged worktree, exit 0 and 49.3 seconds for Fast with Test-MmrHorseBiteFocused.ps1. I verified the supplied log\u2019s SHA-256 against the receipt.",
      "The Release run reported 1,548 component checks, 153 source checks, 3 installed-contract checks, 48 Bite checks, 239 presentation checks and 4 harness-selection checks, all passing. All 15 focused children passed. Mounted Shield reported 114 contract checks and 68 rollback checks, including byte identity of the embedded icon.",
      "I evaluated the candidate\u2019s Mount Primary decision in memory. A single missing source was rejected. Removing its two format markers as well made a recognizably current unrecorded row pass. The analogous missing-readability row also passed.",
      "I compared the previous and current schema-8 source predicates in memory. The candidate accepted the legitimate supplied-to-stock refresh and rejected an unrecorded source. It also accepted two zero-creation cases that the previous predicate rejected.",
      "A read-only PowerShell parameter-binding check confirmed that an omitted optional string and an explicitly supplied empty or null string all satisfy IsNullOrEmpty. PowerShell retains whether the parameter was supplied."
    ],
    "inspected": [
      "Controller mission, request, prior findings, developer report, transcript notice, feature diff and candidate receipt. Compared the round-5 correction with 4242dd3e and traced its affected consumers.",
      "Repository validation policy and handoffs, Horse registration and rollback, limb classification, handle binding, strike observation, terminal delivery, both evidence writers and readers, harness dispatch and tier callers."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. The committed isolated harness logs report one exact-case pass and one unknown-name failure, but contain no candidate SHA.",
      "The controller run exercised Release offline. Candidate, Full, rendered playback and native combat timing were not executed for this review."
    ],
    "required_gaps": [
      "Reader acceptance of a complete, readable supplied-to-stock result and rejection of contradictory source counts and partially stripped current evidence.",
      "Successful registration and late failure observed through the production publication path and its resulting classifier input.",
      "Focused verification of build-configuration routing and explicit empty-selector refusal."
    ],
    "source_inspection_only": [
      "Traced Debug and Release argument propagation from Test-Tiers.ps1 and Test.ps1 into the newly nested Bite contract.",
      "Inspected the successful Register path and its exception caller. The recorded rollback execution started from a manually seeded identity.",
      "Inspected the complete schema-8 reader and the fixture\u2019s extracted predicate. Reviewer reproductions evaluated reader conditions in memory."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The reader recognizes current format by just two property names. With those names and attackAnimationHandleSource removed, the decision accepted a row that still carried attackAnimationRegisteredSource, attackAnimationFactsUnreadable and a horseBiteTerminal identifying the source as unrecorded. A row retaining the current registration and terminal fields also passed with its readability proof removed.",
      "blocking_rationale": "An identifiable current row can receive historical credit after losing required playback proof.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only a supported historical field set receives its compatibility allowance. A current outcome must supply its observed source and readability proof.",
      "id": "R3-F2",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 8384
      },
      "priority": "P2",
      "requested_correction": "Identify the supported historical animation field sets explicitly, or use an equally reliable format discriminator. Reject mixed current and historical shapes that lack required proof.",
      "requested_evidence": "Keep the passing single-removal tests. Add partially stripped current outcomes retaining other current-only fields, alongside the two supported historical positives.",
      "title": "Partially stripped current Mount Primary evidence still passes as historical",
      "trigger": "A current outcome loses attackAnimationObservation and attackAnimationHandleRegistered along with one required proof field."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The refresh fixture calls Deliver with Detailed=false. Its minimal outcome has no actor ID and zero child starts. Schema8 then tests only the source/count expression against hand-built snapshots. The complete reader rejects that outcome at its earlier ownership and cardinality check. The fixture also skips PlaybackAccepted for the reference-distinct detached handle.",
      "blocking_rationale": "The corrected expression accepts the intended source pair, but the reported positive cannot demonstrate the requested writer-to-reader result.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A readable supplied Bite followed by a same-action stock refresh produces a successful outcome and snapshot accepted by the complete schema-8 reader.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 538
      },
      "priority": "P2",
      "requested_correction": "Drive the supply, observation and refresh through the production binding path. Check readability before serialization and pass the resulting Horse fields through the existing complete native-controls reader fixture.",
      "requested_evidence": "Show Assert-KmcHorseNativeControlsUxEvidence accepting the readable refresh and historical positive, with an unrecorded observation rejected. A bounded offline fixture is sufficient.",
      "title": "The refresh fixture\u2019s positive cannot pass the complete reader",
      "trigger": "The new R4-F2 check serializes a minimal terminal outcome and evaluates only the last schema-8 source predicate."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Register now publishes after validation, and direct rollback clears the property before cleanup faults. The positive classifier call receives the fixture weapon\u2019s GUID directly. The failure case sets the static property manually and calls rollback on an uninitialized service. Neither case obtains its starting identity from the production registration decision.",
      "blocking_rationale": "The checks establish setter order and direct withdrawal. They would still pass if the success path supplied the wrong GUID, and they do not exercise the requested late registration failure.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Successful registration publishes the captured Bite identity. A late selection or validation failure leaves the classifier without that identity.",
      "id": "R4-F3",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 516
      },
      "priority": "P2",
      "requested_correction": "Exercise the production registration finalization step offline with a successful validation and an injected late failure. Read RegisteredBiteGuid back from the service for both classifier calls.",
      "requested_evidence": "Show the original untagged Bite classifying as Bite after successful publication and as Unavailable after the late failure. Retain the direct rollback check.",
      "title": "The registration fixture supplies the identity it is meant to verify",
      "trigger": "The round-5 contract labels a classifier result as published before it exercises any publication path."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The reader accepts each source independently from a two-value allowlist. With zero creates and one adoption, changing only the latest TB source from stock-created to kmc-supplied passed. Reporting the observed source as kmc-supplied with the same zero creates also passed.",
      "blocking_rationale": "Schema 8 can credit a supplied handle even when its own counters establish that no handle was supplied.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A stock adoption can refresh only to stock. A supplied-to-stock transition requires a supply for that command.",
      "id": "R5-F1",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 9074
      },
      "priority": "P2",
      "requested_correction": "Validate the two sources against the permitted transition and the command\u2019s create/adopt counts, using the difference between cumulative snapshots for RT.",
      "requested_evidence": "Retain stock adoption and a genuine supplied-to-stock refresh as positives. Reject stock-to-supplied and a claimed supply with zero creates for the relevant command.",
      "title": "Schema 8 accepts a supplied handle with zero creations",
      "trigger": "A schema-8 snapshot contains individually recognized source labels that contradict the binding transition or creation count."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Test-Harness.ps1 declares only Only and invokes the Bite contract without Configuration. The child defaults to bin/Release/KingmakerMountedCombat.dll. Test-Tiers forwards Configuration only to scripts declaring it, and the Full umbrella calls Test-Harness without it.",
      "blocking_rationale": "A Debug qualification can credit the legacy Horse case for behavior exercised on stale Release bytes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The compiled binding check reads the DLL selected by its caller.",
      "id": "R5-F2",
      "location": {
        "file": "scripts/Test-Harness.ps1",
        "line": 10501
      },
      "priority": "P2",
      "requested_correction": "Carry Configuration through Test-Harness and its Full caller to the Bite child. Preserve Release as the default for ordinary standalone use.",
      "requested_evidence": "Show the actual legacy-case child arguments and resolved product path for Debug and Release. A focused routing check suffices.",
      "title": "The legacy Horse check always opens the Release DLL",
      "trigger": "Candidate or Full selects Debug while the new legacy Horse case invokes Test-MountedAlpha2BiteContract.ps1."
    },
    {
      "acceptance_ids": [
        "S07",
        "S08"
      ],
      "actual": "Initialize-HarnessSelection converts an empty string to null, which selects every case and disables the exact-match check. The fixture itself uses an explicit empty string to represent an ordinary invocation.",
      "blocking_rationale": "An empty case-name variable silently broadens a requested one-case run to the full harness.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Omitting Only selects the full harness. Explicitly supplying an empty name fails before cases run.",
      "id": "R5-F3",
      "location": {
        "file": "scripts/HarnessDispatch.ps1",
        "line": 9
      },
      "priority": "P2",
      "requested_correction": "Distinguish omission from an explicitly empty or null argument at the script boundary and reject the latter before setup.",
      "requested_evidence": "Exercise omission, an exact name, an unknown name and an explicitly empty name. The empty selection must fail with zero case bodies executed.",
      "title": "An explicitly empty Only argument selects every harness case",
      "trigger": "A caller invokes Test-Harness.ps1 -Only $caseName with an empty or null case name."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "The actual classifier returned Unavailable for both missing first-slot references and for a missing weapon identity. The observation step cleared the earlier handle and source, emitted one identifiable line and delivered rejected terminal playback. The confirmed hoof preserved the prior Bite."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F2",
      "rationale": "Both requested single-field removals now fail. I reproduced the same historical-format misclassification on rows retaining other current-only fields."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F6",
      "rationale": "The legacy case now invokes the compiled binding contract. Its three required cases exercise zero writes for ambiguous Bite actions, one registration for a repeated handle and rejection of a foreign manager. R5-F2 concerns the newly introduced child invocation\u2019s build selection."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "The dispatcher no longer reads the inherited selector. The focused fixture retained both neighboring cases on ordinary invocation and rejected an unknown name. R5-F3 concerns a separately reproducible explicit-argument case."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "The erroneous equality check is gone and the legitimate refresh now passes the source predicate. The submitted writer result cannot reach that predicate through the complete reader. R5-F1 records the distinct false acceptance introduced by the relaxed comparison."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F3",
      "rationale": "The publication and withdrawal statements are correctly placed in source. The recorded run establishes direct rollback withdrawal but does not exercise a late failure through registration or classify using a successfully published value."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r05-abb351",
  "round": 5,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The binding preserves the exact stock Bite handle, permits same-action refresh and supplies one fallback for a missing initial handle. Missing limb evidence clears stale playback. The per-strike and terminal diagnostics retain the single native OnAction delegation.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08 / NX-2 at normal zoom. Correlate the logged Bite with visible playback, facing, the hoof sequence and rider attachment. Check ordinary timing and roll/damage cardinality in the encounter."
    ],
    "scope_reviewed": "PR #67\u2019s final MMR-07 source and offline checkpoint, S07 binding and diagnostics, S08 regression coverage, and all six pending findings.",
    "unresolved_questions": [
      "Whether the native routine\u2019s shared SpeedScale makes BiteAttack too brief to read at normal play zoom."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
