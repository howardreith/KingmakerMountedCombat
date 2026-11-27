<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r04-review part=1/1 sha256=07f462a233d4a02f78744f0167b5da3e1d593fb316c9ad1f6541b7fc6ac870dd -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `4242dd3e7b`

**Decision:** `changes_required` — The candidate passed the required offline gate. Six findings block readiness. Missing limb data can preserve stale Bite evidence, a failed registration can leave its identity published, and the evidence readers can both reject a valid refresh and accept an incomplete outcome. The harness can also report success after selecting zero cases.

## Continuation for Claude

Within any authorized continuation, make the Bite classifier distinguish missing first-limb data from a confirmed hoof, and publish the registered Bite identity only on successful registration. Carry the distinction between the handle observed at the strike and the latest registration through both evidence readers. Preserve the historical reader verdicts and exercise the current writer’s output with a supplied-to-stock refresh and missing-field controls.

Make isolated harness selection explicit, reject an empty selection, and keep Candidate and Full on their complete harness selection. Reuse the existing production-backed binding checks for the affected Horse case. Keep these changes within MMR-07 diagnostics and validation, preserve the single native attack delegation and frozen evidence, and use the existing metered focused gate on the corrected commit. The recorded review ceiling has been reached. Further formal review is subject to owner and controller authorization.

#### Findings

**R4-F1** · P1 · defect · blocks readiness — An inherited case selector can make the full harness pass with zero or one test
- Location: `scripts/Test-Harness.ps1:42`; acceptance: S07, S08
- Trigger: A shell used for an isolated Horse check later starts Candidate or Full with KMC_HARNESS_ONLY still set.
- Expected: An isolated invocation executes its requested case. An unknown case fails, and ordinary Candidate and Full invocations execute the complete harness.
- Actual: Invoke-HarnessTest silently skips every nonmatching name. The script’s final condition checks only the failure count. Candidate and Full inherit KMC_HARNESS_ONLY and accept the harness exit code. Even a valid single-case name can therefore give the complete harness step credit for one case.
- Why it blocks / does not: A qualification run can skip its mandatory safety and regression checks while reporting the harness step as passed. The repository explicitly requires rejection of zero-test selections.
- Requested correction: Use an explicit single-case argument or equivalent invocation-scoped selection. Enforce a nonzero match and keep the tier callers unfiltered.
- Requested evidence: Show one exact match, a misspelled name returning nonzero, and a tier-style invocation retaining a neighboring case when the old environment variable is present. A focused dispatcher fixture is sufficient.

**R2-F2** · P2 · defect · blocks readiness — A missing first-limb entry still preserves a stale Bite
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:202`; acceptance: MMR-07, S07
- Trigger: The Horse’s next plan has a hand and weapon, but a required first-limb entry is missing from an otherwise populated collection.
- Expected: Missing evidence needed to identify the planned limb produces an unavailable observation. A confirmed hoof preserves the earlier Bite.
- Actual: ClassifyBiteLimb checks that the collections are nonempty, then compares their first entries by reference. A null blueprintLimbs[0] with the original planned Bite and matching runtime hand returns NotBite. A missing runtimeLimbs[0] has the same effect. Observe(false, ...) leaves the earlier handle and source intact.
- Why it blocks / does not: The terminal outcome can credit an earlier Bite after a later Horse strike could not be classified.
- Requested correction: Distinguish an absent first-slot reference or required weapon identity from a readable mismatch. Keep binding closed on unavailable inputs and preserve the single native OnAction call.
- Requested evidence: After observing a known Bite, drive the production classifier with a nonempty limb collection whose first entry is null. Show one identifiable unreadable strike line, a cleared prior handle and rejected terminal playback. Retain the confirmed-hoof control.

**R4-F2** · P2 · defect · blocks readiness — The schema-8 reader rejects a valid supplied Bite after a stock refresh
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:9065`; acceptance: MMR-07, S07
- Trigger: A KMC-supplied handle acts and the registration refreshes to a stock handle before the command terminates.
- Expected: The reader credits the validated handle observed at the strike and independently represents the later registration.
- Actual: The outcome correctly retains kmc-supplied from the strike. The refresh updates LastHorsePrimaryHandleSource to stock-created. The schema-8 reader requires these fields to be equal. With valid create, adopt and reject counts, changing the latest source to match that refresh changed the reader clause from accept to reject.
- Why it blocks / does not: A supported refresh can make the external reader reject an otherwise valid Horse native-controls result. The 24-key shape check cannot detect this value mismatch.
- Requested correction: Make the writer and reader compare sources from the same observation. Preserve the historical schema-8 verdicts and the rejection of genuinely unregistered playback.
- Requested evidence: Observe a KMC-supplied Bite, refresh to a different stock handle of the same action, and terminate before another Bite acts. Pass the resulting outcome and animation snapshot through the reader, alongside a historical positive and an unregistered negative.

**R3-F2** · P2 · defect · blocks readiness — Missing current Mount Primary proof fields are treated as historical
- Location: `scripts/runtime/RuntimeHarness.Common.ps1:8382`; acceptance: MMR-07, S07
- Trigger: A current-shaped explicit Mount Primary artifact retains the observation and registration fields but omits its observed source or readability field.
- Expected: Current outcomes must provide the observed source and readability evidence. Historical outcomes retain the verdict appropriate to their supported format.
- Actual: The reader independently skips each check when its property is absent. With current observation and registration fields still present, removing attackAnimationHandleSource from an unrecorded outcome made the playback clauses accept it. Removing attackAnimationFactsUnreadable from a failed-read outcome also made those clauses accept it.
- Why it blocks / does not: The explicit Mount Primary reader can credit current evidence that does not establish a validated source or readable playback state.
- Requested correction: Identify supported historical outcomes before applying their compatibility rule. Require both proof fields for current outcomes and retain the acted, finished and interruption checks.
- Requested evidence: Start with a serialized current outcome and remove each required field separately. Both cases must fail while the complete registered positive, explicit unrecorded negative and supported historical positives retain their intended verdicts.

**R4-F3** · P2 · defect · blocks readiness — Horse registration rollback leaves the Bite GUID published
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs:676`; acceptance: MMR-07, S07
- Trigger: A Ranger selection or Horse contract check fails after the static Bite GUID has been assigned.
- Expected: The binding identity becomes available when Horse registration succeeds and is unavailable after a failed registration rolls back.
- Actual: RegisteredBiteGuid is assigned before RequireExactRangerOptions, ApplySelectionDesired and ValidateRegisteredContract. A failure there invokes RollBackPartialRegistration, which removes the staged Horse definitions and leaves the static GUID populated. ClassifyHorseStrike consumes that GUID without checking registration state.
- Why it blocks / does not: The exact-limb proof can use an identity from a registration that rolled back. A retained matching plan can consequently classify as Bite while the service is faulted.
- Requested correction: Publish the identity with successful registration, or bind it to the successful service instance and withdraw it on rollback. Preserve the existing lifetime of successfully registered save-visible definitions.
- Requested evidence: Force a late registration failure after the Bite identity has been read. Verify that rollback leaves the classifier without a registered identity and that successful registration still admits the original untagged Bite.

**R3-F6** · P2 · defect · blocks readiness — The replacement Horse harness predicates still test spelling instead of binding behavior
- Location: `scripts/Test-Harness.ps1:10510`; acceptance: MMR-07, S07
- Trigger: The legacy Horse case evaluates the single-action rule after control flow changes while the searched statements remain present.
- Expected: The affected Horse harness assertion exercises the binding behavior or compiled contract for exact action selection, ownership and idempotence.
- Actual: The isolated case passes on this candidate, but its replacement predicates check C# substrings. In memory, removing the return after RejectedCandidateCount left every new predicate and negative control green, although execution would fall through and supply the last of two Bite actions. Reversing the operands of the equivalent count comparison failed the predicate.
- Why it blocks / does not: The affected harness case still cannot distinguish an exact binding from an ambiguous fallback. The separately passing Bite contract provides behavioral coverage that this case can reuse.
- Requested correction: Replace the newly added binding spelling checks with the existing production-backed cases or a shared compiled contract. Keep the other legacy Horse assertions in place.
- Requested evidence: Run the isolated case against the actual binding decision. Show that two eligible actions produce zero writes, a repeated handle produces one registration, and a foreign-manager handle is rejected.

#### Prior finding assessments

- **R2-F2** — `fix_rejected` — Null plans, absent collections and missing hands now reach Unavailable. A null required entry inside a populated collection still returns NotBite. The component test supplies Unavailable directly and misses this classifier path.
- **R2-F3** — `fix_verified` — The candidate gate invoked the real ReportTerminalOnce twice per detached command. The callback delivered one outcome on each path. Both Horse outcomes carried the observed Bite, and the non-Horse control had no Bite text or observation.
- **R3-F1** — `fix_verified` — CaptureMountedOutcome again emits the 24 schema-8 keys. The exact-property assertion accepted the candidate and historical key sets and rejected an extra field. R4-F2 concerns a separate comparison of values within that valid shape.
- **R3-F2** — `fix_rejected` — The in-process row and reader now reject an explicit unrecorded source. The reader’s compatibility branch still permits missing proof on a recognizably current outcome. I reproduced that remaining false acceptance.
- **R3-F3** — `fix_verified` — AttachHorseBite copies FactsUnreadable to the delivered outcome, and all three scenario checks use the four-argument playback decision. The gate recorded SecurityException with accepted=False and accepted the readable control.
- **R3-F4** — `fix_verified` — The observation captures its source at the strike. Describe and Project use it after refresh, and the component test retains the genuinely unregistered control. R4-F2 identifies the downstream reader that still compares it with the latest registration.
- **R3-F5** — `fix_verified` — The classifier compares the planned weapon GUID with the registration-time Bite GUID as well as the slot references. The detached-object contract rejected a substituted hoof with zero writes and retained the untagged Bite fallback. R4-F3 concerns publication on rollback.
- **R3-F6** — `fix_rejected` — The obsolete inline names are gone and the isolated run reports 1/1. The requested behavior or compiled-contract check is still missing from that case. Its replacement accepts a source variant that falls through the ambiguous-action rejection.
- **R3-F7** — `deferral_accepted` — The prior ledger marks the digest labeling and extraction-provenance wording optional. The frozen verification, mutation rejection and current candidate gate remain independently inspectable.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoff instructions, developer report, prior findings ledger, 32-file feature diff and round-four correction delta.; Horse blueprint registration and rollback, exact-limb classification, handle binding, strike observation, terminal delivery, RT and TB scenario checks, evidence writers and their PowerShell readers.; Focused gate receipt and log, contract and component tests, legacy harness case, Candidate and Full callers, and the preserved extraction and mutation records.
- **Executed verification:** Verified HEAD, merge base, mission SHA-256 and the controller log digest. The log SHA-256 is 47a10161b412b94e25bfe8e08fc4dec93277d3df416572b000840c211b7f5f65.; The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 at the candidate. It exited 0 in 48.6 seconds with 14 of 14 focused children passing. Components passed 1548/1548, source validation 153/153, Bite contracts 3/3 and 44/44, and presentation 239/239.; The gate recorded Mounted Shield 114/114, rollback 68/68, and identical packet, production and embedded icon hashes. The Release DLL SHA-256 was 0318b440480251e3e2ddea9dd02d25680a90fb5e992e888f629e8861832d1ebe.; The candidate log records one real terminal callback on each detached command path, a non-Horse control, rejection of a delivered SecurityException, and agreement on the 24 schema-8 outcome keys.; I evaluated the production PowerShell reader clauses in memory. The schema-8 source check accepted a supplied Bite before refresh and rejected it after changing only the latest registration source to stock-created. The explicit Mount Primary clauses accepted current-shaped rows with the observed source or readability field removed.; I exercised the harness dispatcher and final exit condition in isolation. An unmatched selector produced TOTAL PASS=0 FAIL=0 and exit 0. I also evaluated the legacy Horse predicates against in-memory source variants. They accepted removal of the ambiguous-action early return and rejected an equivalent comparison spelling.
- **Source inspection only:** Traced the missing first-entry cases through ClassifyBiteLimb and Observe(false), and the GUID publication through the late registration checks and rollback.; Traced the captured strike source and latest adapter source into their respective serialized fields. The complete native scenario was not run.; Traced environment inheritance and exit-code handling through Test-Tiers.ps1 and Test.ps1. Candidate and Full were not run.
- **Required gaps:** Demonstrate unavailable classification for missing first-limb entries after a known Bite, and withdrawal of the binding identity after a late registration failure.; Show reader verdicts for a supplied Bite followed by a stock refresh, incomplete current Mount Primary outcomes, and supported historical controls.; Show that an unknown isolated case fails, tier invocations retain their full harness selection, and the affected Horse case detects an actual ambiguous or duplicate binding.
- **Limitations:** The controller withheld the mixed foreground transcript. The current focused run has a candidate-bound receipt and log. The separate 4.5-second legacy harness run has a committed 1/1 log.; The independent reproductions exercised reader expressions and harness predicates in memory. Product execution evidence comes from the controller’s offline gate. Native playback, camera readability and gameplay timing remain unobserved on this candidate.; R3-F7 remains optional evidence-documentation work. The frozen verification and current gate have independent records.

#### Summary

- **Delivered behavior:** The candidate binds the registered Horse Bite by weapon and slot, captures handle provenance at the strike, and carries it through detailed and fallback terminal delivery. The required offline gate passes. The outstanding findings concern missing classification inputs, registration rollback, reader accuracy and harness integrity.
- **Scope reviewed:** Final PR-5 checkpoint for MMR-07 and S07, including the S08 regression evidence, all nine pending findings and the consumers affected by the round-four corrections.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N08/NX-2 remains pending. At normal play zoom, compare the visible Bite with its logged strike and check facing, hoof playback, rider attachment, attack timing and roll/damage cardinality.; The supplemental ledger records round four as this enrollment’s ceiling. The owner determines whether to authorize further formal review.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a2181e-86ce-7882-99e4-843d9dcca906`, duration 927.0s, response sha256 `50e4f327fbc32b736b3743dbf394ef2ba5bdf52cef3e59f2e87df19c99a9ad3d`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r04-f5e518`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "4242dd3e7bc259e610f54f9975e7d3a2a955841e",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate passed the required offline gate. Six findings block readiness. Missing limb data can preserve stale Bite evidence, a failed registration can leave its identity published, and the evidence readers can both reject a valid refresh and accept an incomplete outcome. The harness can also report success after selecting zero cases.\n\n## Continuation for Claude\n\nWithin any authorized continuation, make the Bite classifier distinguish missing first-limb data from a confirmed hoof, and publish the registered Bite identity only on successful registration. Carry the distinction between the handle observed at the strike and the latest registration through both evidence readers. Preserve the historical reader verdicts and exercise the current writer\u2019s output with a supplied-to-stock refresh and missing-field controls.\n\nMake isolated harness selection explicit, reject an empty selection, and keep Candidate and Full on their complete harness selection. Reuse the existing production-backed binding checks for the affected Horse case. Keep these changes within MMR-07 diagnostics and validation, preserve the single native attack delegation and frozen evidence, and use the existing metered focused gate on the corrected commit. The recorded review ceiling has been reached. Further formal review is subject to owner and controller authorization.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified HEAD, merge base, mission SHA-256 and the controller log digest. The log SHA-256 is 47a10161b412b94e25bfe8e08fc4dec93277d3df416572b000840c211b7f5f65.",
      "The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 at the candidate. It exited 0 in 48.6 seconds with 14 of 14 focused children passing. Components passed 1548/1548, source validation 153/153, Bite contracts 3/3 and 44/44, and presentation 239/239.",
      "The gate recorded Mounted Shield 114/114, rollback 68/68, and identical packet, production and embedded icon hashes. The Release DLL SHA-256 was 0318b440480251e3e2ddea9dd02d25680a90fb5e992e888f629e8861832d1ebe.",
      "The candidate log records one real terminal callback on each detached command path, a non-Horse control, rejection of a delivered SecurityException, and agreement on the 24 schema-8 outcome keys.",
      "I evaluated the production PowerShell reader clauses in memory. The schema-8 source check accepted a supplied Bite before refresh and rejected it after changing only the latest registration source to stock-created. The explicit Mount Primary clauses accepted current-shaped rows with the observed source or readability field removed.",
      "I exercised the harness dispatcher and final exit condition in isolation. An unmatched selector produced TOTAL PASS=0 FAIL=0 and exit 0. I also evaluated the legacy Horse predicates against in-memory source variants. They accepted removal of the ambiguous-action early return and rejected an equivalent comparison spelling."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoff instructions, developer report, prior findings ledger, 32-file feature diff and round-four correction delta.",
      "Horse blueprint registration and rollback, exact-limb classification, handle binding, strike observation, terminal delivery, RT and TB scenario checks, evidence writers and their PowerShell readers.",
      "Focused gate receipt and log, contract and component tests, legacy harness case, Candidate and Full callers, and the preserved extraction and mutation records."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. The current focused run has a candidate-bound receipt and log. The separate 4.5-second legacy harness run has a committed 1/1 log.",
      "The independent reproductions exercised reader expressions and harness predicates in memory. Product execution evidence comes from the controller\u2019s offline gate. Native playback, camera readability and gameplay timing remain unobserved on this candidate.",
      "R3-F7 remains optional evidence-documentation work. The frozen verification and current gate have independent records."
    ],
    "required_gaps": [
      "Demonstrate unavailable classification for missing first-limb entries after a known Bite, and withdrawal of the binding identity after a late registration failure.",
      "Show reader verdicts for a supplied Bite followed by a stock refresh, incomplete current Mount Primary outcomes, and supported historical controls.",
      "Show that an unknown isolated case fails, tier invocations retain their full harness selection, and the affected Horse case detects an actual ambiguous or duplicate binding."
    ],
    "source_inspection_only": [
      "Traced the missing first-entry cases through ClassifyBiteLimb and Observe(false), and the GUID publication through the late registration checks and rollback.",
      "Traced the captured strike source and latest adapter source into their respective serialized fields. The complete native scenario was not run.",
      "Traced environment inheritance and exit-code handling through Test-Tiers.ps1 and Test.ps1. Candidate and Full were not run."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "S07",
        "S08"
      ],
      "actual": "Invoke-HarnessTest silently skips every nonmatching name. The script\u2019s final condition checks only the failure count. Candidate and Full inherit KMC_HARNESS_ONLY and accept the harness exit code. Even a valid single-case name can therefore give the complete harness step credit for one case.",
      "blocking_rationale": "A qualification run can skip its mandatory safety and regression checks while reporting the harness step as passed. The repository explicitly requires rejection of zero-test selections.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An isolated invocation executes its requested case. An unknown case fails, and ordinary Candidate and Full invocations execute the complete harness.",
      "id": "R4-F1",
      "location": {
        "file": "scripts/Test-Harness.ps1",
        "line": 42
      },
      "priority": "P1",
      "requested_correction": "Use an explicit single-case argument or equivalent invocation-scoped selection. Enforce a nonzero match and keep the tier callers unfiltered.",
      "requested_evidence": "Show one exact match, a misspelled name returning nonzero, and a tier-style invocation retaining a neighboring case when the old environment variable is present. A focused dispatcher fixture is sufficient.",
      "title": "An inherited case selector can make the full harness pass with zero or one test",
      "trigger": "A shell used for an isolated Horse check later starts Candidate or Full with KMC_HARNESS_ONLY still set."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "ClassifyBiteLimb checks that the collections are nonempty, then compares their first entries by reference. A null blueprintLimbs[0] with the original planned Bite and matching runtime hand returns NotBite. A missing runtimeLimbs[0] has the same effect. Observe(false, ...) leaves the earlier handle and source intact.",
      "blocking_rationale": "The terminal outcome can credit an earlier Bite after a later Horse strike could not be classified.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Missing evidence needed to identify the planned limb produces an unavailable observation. A confirmed hoof preserves the earlier Bite.",
      "id": "R2-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 202
      },
      "priority": "P2",
      "requested_correction": "Distinguish an absent first-slot reference or required weapon identity from a readable mismatch. Keep binding closed on unavailable inputs and preserve the single native OnAction call.",
      "requested_evidence": "After observing a known Bite, drive the production classifier with a nonempty limb collection whose first entry is null. Show one identifiable unreadable strike line, a cleared prior handle and rejected terminal playback. Retain the confirmed-hoof control.",
      "title": "A missing first-limb entry still preserves a stale Bite",
      "trigger": "The Horse\u2019s next plan has a hand and weapon, but a required first-limb entry is missing from an otherwise populated collection."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The outcome correctly retains kmc-supplied from the strike. The refresh updates LastHorsePrimaryHandleSource to stock-created. The schema-8 reader requires these fields to be equal. With valid create, adopt and reject counts, changing the latest source to match that refresh changed the reader clause from accept to reject.",
      "blocking_rationale": "A supported refresh can make the external reader reject an otherwise valid Horse native-controls result. The 24-key shape check cannot detect this value mismatch.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The reader credits the validated handle observed at the strike and independently represents the later registration.",
      "id": "R4-F2",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 9065
      },
      "priority": "P2",
      "requested_correction": "Make the writer and reader compare sources from the same observation. Preserve the historical schema-8 verdicts and the rejection of genuinely unregistered playback.",
      "requested_evidence": "Observe a KMC-supplied Bite, refresh to a different stock handle of the same action, and terminate before another Bite acts. Pass the resulting outcome and animation snapshot through the reader, alongside a historical positive and an unregistered negative.",
      "title": "The schema-8 reader rejects a valid supplied Bite after a stock refresh",
      "trigger": "A KMC-supplied handle acts and the registration refreshes to a stock handle before the command terminates."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The reader independently skips each check when its property is absent. With current observation and registration fields still present, removing attackAnimationHandleSource from an unrecorded outcome made the playback clauses accept it. Removing attackAnimationFactsUnreadable from a failed-read outcome also made those clauses accept it.",
      "blocking_rationale": "The explicit Mount Primary reader can credit current evidence that does not establish a validated source or readable playback state.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Current outcomes must provide the observed source and readability evidence. Historical outcomes retain the verdict appropriate to their supported format.",
      "id": "R3-F2",
      "location": {
        "file": "scripts/runtime/RuntimeHarness.Common.ps1",
        "line": 8382
      },
      "priority": "P2",
      "requested_correction": "Identify supported historical outcomes before applying their compatibility rule. Require both proof fields for current outcomes and retain the acted, finished and interruption checks.",
      "requested_evidence": "Start with a serialized current outcome and remove each required field separately. Both cases must fail while the complete registered positive, explicit unrecorded negative and supported historical positives retain their intended verdicts.",
      "title": "Missing current Mount Primary proof fields are treated as historical",
      "trigger": "A current-shaped explicit Mount Primary artifact retains the observation and registration fields but omits its observed source or readability field."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "RegisteredBiteGuid is assigned before RequireExactRangerOptions, ApplySelectionDesired and ValidateRegisteredContract. A failure there invokes RollBackPartialRegistration, which removes the staged Horse definitions and leaves the static GUID populated. ClassifyHorseStrike consumes that GUID without checking registration state.",
      "blocking_rationale": "The exact-limb proof can use an identity from a registration that rolled back. A retained matching plan can consequently classify as Bite while the service is faulted.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The binding identity becomes available when Horse registration succeeds and is unavailable after a failed registration rolls back.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs",
        "line": 676
      },
      "priority": "P2",
      "requested_correction": "Publish the identity with successful registration, or bind it to the successful service instance and withdraw it on rollback. Preserve the existing lifetime of successfully registered save-visible definitions.",
      "requested_evidence": "Force a late registration failure after the Bite identity has been read. Verify that rollback leaves the classifier without a registered identity and that successful registration still admits the original untagged Bite.",
      "title": "Horse registration rollback leaves the Bite GUID published",
      "trigger": "A Ranger selection or Horse contract check fails after the static Bite GUID has been assigned."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The isolated case passes on this candidate, but its replacement predicates check C# substrings. In memory, removing the return after RejectedCandidateCount left every new predicate and negative control green, although execution would fall through and supply the last of two Bite actions. Reversing the operands of the equivalent count comparison failed the predicate.",
      "blocking_rationale": "The affected harness case still cannot distinguish an exact binding from an ambiguous fallback. The separately passing Bite contract provides behavioral coverage that this case can reuse.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The affected Horse harness assertion exercises the binding behavior or compiled contract for exact action selection, ownership and idempotence.",
      "id": "R3-F6",
      "location": {
        "file": "scripts/Test-Harness.ps1",
        "line": 10510
      },
      "priority": "P2",
      "requested_correction": "Replace the newly added binding spelling checks with the existing production-backed cases or a shared compiled contract. Keep the other legacy Horse assertions in place.",
      "requested_evidence": "Run the isolated case against the actual binding decision. Show that two eligible actions produce zero writes, a repeated handle produces one registration, and a foreign-manager handle is rejected.",
      "title": "The replacement Horse harness predicates still test spelling instead of binding behavior",
      "trigger": "The legacy Horse case evaluates the single-action rule after control flow changes while the searched statements remain present."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F2",
      "rationale": "Null plans, absent collections and missing hands now reach Unavailable. A null required entry inside a populated collection still returns NotBite. The component test supplies Unavailable directly and misses this classifier path."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F3",
      "rationale": "The candidate gate invoked the real ReportTerminalOnce twice per detached command. The callback delivered one outcome on each path. Both Horse outcomes carried the observed Bite, and the non-Horse control had no Bite text or observation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F1",
      "rationale": "CaptureMountedOutcome again emits the 24 schema-8 keys. The exact-property assertion accepted the candidate and historical key sets and rejected an extra field. R4-F2 concerns a separate comparison of values within that valid shape."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F2",
      "rationale": "The in-process row and reader now reject an explicit unrecorded source. The reader\u2019s compatibility branch still permits missing proof on a recognizably current outcome. I reproduced that remaining false acceptance."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F3",
      "rationale": "AttachHorseBite copies FactsUnreadable to the delivered outcome, and all three scenario checks use the four-argument playback decision. The gate recorded SecurityException with accepted=False and accepted the readable control."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F4",
      "rationale": "The observation captures its source at the strike. Describe and Project use it after refresh, and the component test retains the genuinely unregistered control. R4-F2 identifies the downstream reader that still compares it with the latest registration."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F5",
      "rationale": "The classifier compares the planned weapon GUID with the registration-time Bite GUID as well as the slot references. The detached-object contract rejected a substituted hoof with zero writes and retained the untagged Bite fallback. R4-F3 concerns publication on rollback."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F6",
      "rationale": "The obsolete inline names are gone and the isolated run reports 1/1. The requested behavior or compiled-contract check is still missing from that case. Its replacement accepts a source variant that falls through the ambiguous-action rejection."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R3-F7",
      "rationale": "The prior ledger marks the digest labeling and extraction-provenance wording optional. The frozen verification, mutation rejection and current candidate gate remain independently inspectable."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r04-f5e518",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate binds the registered Horse Bite by weapon and slot, captures handle provenance at the strike, and carries it through detailed and fallback terminal delivery. The required offline gate passes. The outstanding findings concern missing classification inputs, registration rollback, reader accuracy and harness integrity.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08/NX-2 remains pending. At normal play zoom, compare the visible Bite with its logged strike and check facing, hoof playback, rider attachment, attack timing and roll/damage cardinality.",
      "The supplemental ledger records round four as this enrollment\u2019s ceiling. The owner determines whether to authorize further formal review."
    ],
    "scope_reviewed": "Final PR-5 checkpoint for MMR-07 and S07, including the S08 regression evidence, all nine pending findings and the consumers affected by the round-four corrections.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
