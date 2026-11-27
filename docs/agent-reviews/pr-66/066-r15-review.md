<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r15-review part=1/1 sha256=e8db15de120ec8e4484fab439d97cce9d4331bc6ffa5c03549ca0f2dcbecb257 -->
### Codex GPT-6.2 Astra — review — round 15 — candidate `b9de2358ae`

**Decision:** `changes_required` — The Release gate passed. It verifies the two-click repair, revocation across both successful stop paths, and the compiled maintenance callback reaching the reader. The focused Debug selection still uses Release for six checks. Two changes to the lifecycle tests also leave their safety evidence incomplete.

## Continuation for Claude

Forward the selected configuration to every child of Test-MmrHorseTripFocused.ps1 that reads the product DLL. Capture one metered Debug run with only the Debug product available. Its output can establish both the reader identity requested in R14-F2 and the wrapper correction in R15-F1.

Keep the immediate re-enable cases. Give the lifecycle and repair fixtures a shared save/load state, and restore the controls for an update while stopped and for consent taken just before a stop. Run the required Release gate at the corrected candidate. These corrections concern offline validation of the confirmed in-game action authorized by OWNER AMENDMENT 1.

#### Findings

**R14-F2** · P2 · evidence_gap · blocks readiness — The Debug reader run lacks a reviewable execution record
- Location: `scripts/Test-Finalize5CompanionReaders.ps1:97`; acceptance: MMR-06
- Trigger: The reported standalone Debug run is used to close the previous artifact-selection finding.
- Expected: A recorded Debug selection loads the Debug product with the Release product unavailable.
- Actual: The reader now opens bin/<Configuration>/KingmakerMountedCombat.dll and logs its identity. The only supplied execution log identifies Release. The Debug result and abbreviated identity appear in the developer report and ledger.
- Why it blocks / does not: The requested Debug artifact observation cannot be confirmed from the supplied execution evidence.
- Requested correction: Include the reader's machine-recorded Debug output in the review evidence. The run requested in R15-F1 can supply it.
- Requested evidence: Show the Debug invocation, loaded path, SHA-256, MVID and reader result with bin/Release unavailable. Retain the required Release receipt for the corrected candidate.

**R15-F1** · P2 · defect · blocks readiness — Six child checks in the Debug focused run still select Release
- Location: `scripts/Test-MmrHorseTripFocused.ps1:31`; acceptance: MMR-06, S08
- Trigger: Test-Tiers selects Test-MmrHorseTripFocused.ps1 with -Configuration Debug.
- Expected: Each product-bound child inspects the DLL built by the selected tier.
- Actual: The wrapper forwards Configuration only to the Trip contract and companion reader. Mounted Shield, Shield rollback, Mounted Archery, replay subscriber and both UMM profile invocations default to Release. A clean Debug output therefore fails the focused selection. An existing Release DLL can supply verdicts for different bytes.
- Why it blocks / does not: The advertised Debug invocation can fail after a successful build or attribute protected-neighbor checks to the wrong artifact.
- Requested correction: Forward Configuration to the six remaining invocations that accept it, preserving their existing arguments and both loader profiles.
- Requested evidence: Run the complete focused wrapper in Debug with bin/Release unavailable. Show all 13 children completing, the Debug artifact identity and both UMM product-binding results.

**R15-F2** · P2 · defect · blocks readiness — The refused-disable fixture expects a removal during an active save or load
- Location: `tests/HorseTripRepairPolicyTests.cs:308`; acceptance: MMR-06, S06
- Trigger: DisableAndFailureStop runs its save-in-flight or load-in-flight case and immediately dispatches the armed confirmation.
- Expected: A refused disable leaves the confirmation armed. If the save or load is still active at dispatch, the update consumes the confirmation with zero native removals.
- Actual: The fixture sets life.Busy or life.Loading and leaves it set. Host.GateAdmits reads separate flags that stay open. The assertion consequently requires a removed result while the case still reports an active save or load.
- Why it blocks / does not: The new test passes by asserting a write that the production persistence gate forbids. It misrepresents the interaction between a refused disable and the next repair update.
- Requested correction: Use a common simulated persistence state for the lifecycle and repair hosts. Assert retained consent immediately after the refused disable. Make settlement explicit when testing a successful subsequent dispatch.
- Requested evidence: With the save or load held open, show a guarded refusal, zero native calls and no replay after settlement. For the retained-consent success control, settle before the first repair update and show one exact removal.

**R15-F3** · P2 · evidence_gap · blocks readiness — The lifecycle test dropped two previously exercised stop boundaries
- Location: `tests/HorseTripRepairPolicyTests.cs:279`; acceptance: MMR-06, S06
- Trigger: A repair update occurs while services remain stopped, or a stop occurs after TakeConsent and before dispatch.
- Expected: While stopped, an eligible loaded horse produces no repair offer. A confirmation already taken before a stop cannot remove its fact afterward.
- Actual: The revised test re-enables before every repair update in the successful-stop cases. It removes the prior zero-offer assertion while stopped and the taken-consent dispatch check. The separate selection test checks only that the old token fails, which would also pass if a stopped update exposed a fresh offer.
- Why it blocks / does not: OWNER AMENDMENT 1 preserves behavioral tests that still apply. These removed controls covered distinct intervals that the new immediate-restart test cannot observe.
- Requested correction: Keep the immediate-restart regression and restore the stopped-interval and already-taken-consent controls alongside it, using the production lifecycle sequence for the stop.
- Requested evidence: Request presentation and update while stopped with an eligible horse loaded. Verify zero offers and native calls. Separately dispatch consent taken before the stop and verify the recorded stopped refusal and zero removals.

**R15-F4** · P3 · optional_polish · non-blocking — The test mapping overstates which cases separate the two clicks
- Location: `docs/missions/mmr-split/PR-4-amendment-test-mapping.md:65`; acceptance: MMR-06
- Trigger: The mapping is used to identify the evidence for the corrected interaction.
- Expected: The test mapping identifies which cases establish the multi-update interaction, rapid clicks and callback classification.
- Actual: The mapping describes every listed case as running through the updater with separated clicks. RepeatedClicksAndUpdates deliberately issues back-to-back clicks, and UmmPanelClassification invokes the classifier directly.
- Why it blocks / does not: SelectionSurvivesUpdates independently establishes the required multi-update interaction.
- Requested correction: Narrow the mapping's description to the cases that actually separate selection and confirmation.
- Requested evidence: A source comparison with the named test methods is sufficient.

#### Prior finding assessments

- **R12-F1** — `fix_verified` — The tests now execute the same Disable, FailureCleanup and Enable sequences used by CompositionRoot. For each successful stop, re-enable precedes the next repair update. The gate verified zero removals, rejection of the old token and one removal after fresh confirmation.
- **R12-F6** — `fix_verified` — The Release gate bound the actual compiled Main.OnMaintenanceGui delegate, passed it through Chunk7PersistenceScenario.UmmPanelObservation and fed its returned values to the reader. Maintenance and historical no-panel controls passed. The unrelated handler, both mixed orders, inactive entry and disabled Mounted Charge controls were refused.
- **R14-F1** — `fix_verified` — TakeConsent preserves an unconfirmed selection. The recorded test kept A selected through five updates with zero native calls, confirmed A later, removed exactly once, preserved B and observed no replay over four further updates.
- **R14-F2** — `fix_rejected` — The hard-coded path in the named reader is corrected in source. Its requested Debug execution remains unverified because the supplied receipt and log cover Release only. R15-F1 identifies the separate configuration defect in sibling invocations.

#### Evidence assessment

- **Inspected:** The fixed mission, OWNER AMENDMENT 1, repository validation policy, current handoffs, full feature diff, developer report and four pending findings.; Horse construction, candidate discovery, UMM intent collection, ordinary-update dispatch, exact fact removal, lifecycle delegation and the compiled-observer reader fixture.; The correction since 93d806b0, including the focused wrapper, its child parameter defaults, the prior stopped-state tests and the controller receipt and log.
- **Executed verification:** Matched HEAD to the supplied candidate, the complete feature patch to M..H, and the gate log's SHA-256 to its controller receipt.; The controller recorded the required Release Fast gate at H with exit code 0 in 44.5 seconds. Components passed 1548/1548, Validate-Source passed 153/153, and all 13 focused children passed. The receipt recorded 316.06 minutes of cumulative workflow spend.; The Trip contract passed 51 checks against installed data and compiled code. The component run exercised selection across updates, exact removal, stale consent, reporting failure and immediate re-enable after each successful stop.; The companion reader passed 83 checks using Release DLL SHA-256 c5aa03851d23a7ef30bf6537c5edd2c9029edb64befe236ecf25569331359df5, MVID e7fb0df3-eb14-479d-acb7-7a9b47b7fdeb. The Mounted Shield contract passed 114 checks, including embedded icon identity, and its rollback suite passed 68.
- **Source inspection only:** Traced Test-Tiers configuration forwarding through the wrapper. Two of eight product-DLL invocations receive the selected configuration. The other six default to Release.; Compared the fixture's independent persistence flags with CompositionRoot, where the disable checks and repair write gate read the same persistence service.; Confirmed that fresh construction filters the exact stock Bite-to-Trip grant at the Horse donor boundary and that the active repair path contains no request-file ingestion.
- **Required gaps:** A recorded Debug focused run that identifies the reader's loaded DLL and completes the product-bound child checks without a Release product.; A refused-disable test in which lifecycle admission and repair dispatch see the same active save or load.; Restored observations for the interval while services remain stopped and for a confirmation already taken when the stop occurs.
- **Limitations:** The controller supplied the Release gate log. The standalone Debug, continuous-drive, marker-ownership and extracted harness results appear in developer records without their command output in this packet.; The supplemental ledger records the round-14 no-progress stop at 14/14, the subsequent resume and a failed first gate on 18453069. The passing receipt belongs to the submitted H.; The review was read-only. The evidence is offline and does not establish gameplay, UI appearance, save persistence or native Linux execution.

#### Summary

- **Delivered behavior:** Fresh KMC Horses filter the stock Mammoth TrippingBite grant and retain the bite and hoof limbs. For an existing horse, the maintenance action explains the provenance ambiguity, binds confirmation to one live horse and fact, and records the guarded removal outcome. The Release gate verifies the ordinary two-click interaction.
- **Scope reviewed:** PR-4 source and offline acceptance for MMR-06, S06 and S08 under OWNER AMENDMENT 1, including the round-15 corrections and their directly affected callers and checks. PR-6 owns integrated packaging.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N07 in the integrated alpha. Observe a successful plain-Horse bite and damage without intrinsic Trip, with a legitimate dog, wolf or separately granted maneuver as a control where available.; Inspect the maintenance UI and perform N10 on an existing horse. Confirm that ordinary save/reload preserves the repair, one pair and its expected effects, and that loading an older save requires fresh consent.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`.

Codex CLI `0.170.0`, thread `01a21e35-5bd9-7213-a2ea-1a4b5c8a9287`, duration 714.2s, response sha256 `2f883170173aec5d1bbd8acf467b15cb649261c6019a87913532a71f3833b0f3`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r15-f53f21`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "b9de2358ae60f0cfda3c421f256dfa82038d7ccc",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The Release gate passed. It verifies the two-click repair, revocation across both successful stop paths, and the compiled maintenance callback reaching the reader. The focused Debug selection still uses Release for six checks. Two changes to the lifecycle tests also leave their safety evidence incomplete.\n\n## Continuation for Claude\n\nForward the selected configuration to every child of Test-MmrHorseTripFocused.ps1 that reads the product DLL. Capture one metered Debug run with only the Debug product available. Its output can establish both the reader identity requested in R14-F2 and the wrapper correction in R15-F1.\n\nKeep the immediate re-enable cases. Give the lifecycle and repair fixtures a shared save/load state, and restore the controls for an update while stopped and for consent taken just before a stop. Run the required Release gate at the corrected candidate. These corrections concern offline validation of the confirmed in-game action authorized by OWNER AMENDMENT 1.",
  "evidence_assessment": {
    "executed_verification": [
      "Matched HEAD to the supplied candidate, the complete feature patch to M..H, and the gate log's SHA-256 to its controller receipt.",
      "The controller recorded the required Release Fast gate at H with exit code 0 in 44.5 seconds. Components passed 1548/1548, Validate-Source passed 153/153, and all 13 focused children passed. The receipt recorded 316.06 minutes of cumulative workflow spend.",
      "The Trip contract passed 51 checks against installed data and compiled code. The component run exercised selection across updates, exact removal, stale consent, reporting failure and immediate re-enable after each successful stop.",
      "The companion reader passed 83 checks using Release DLL SHA-256 c5aa03851d23a7ef30bf6537c5edd2c9029edb64befe236ecf25569331359df5, MVID e7fb0df3-eb14-479d-acb7-7a9b47b7fdeb. The Mounted Shield contract passed 114 checks, including embedded icon identity, and its rollback suite passed 68."
    ],
    "inspected": [
      "The fixed mission, OWNER AMENDMENT 1, repository validation policy, current handoffs, full feature diff, developer report and four pending findings.",
      "Horse construction, candidate discovery, UMM intent collection, ordinary-update dispatch, exact fact removal, lifecycle delegation and the compiled-observer reader fixture.",
      "The correction since 93d806b0, including the focused wrapper, its child parameter defaults, the prior stopped-state tests and the controller receipt and log."
    ],
    "limitations": [
      "The controller supplied the Release gate log. The standalone Debug, continuous-drive, marker-ownership and extracted harness results appear in developer records without their command output in this packet.",
      "The supplemental ledger records the round-14 no-progress stop at 14/14, the subsequent resume and a failed first gate on 18453069. The passing receipt belongs to the submitted H.",
      "The review was read-only. The evidence is offline and does not establish gameplay, UI appearance, save persistence or native Linux execution."
    ],
    "required_gaps": [
      "A recorded Debug focused run that identifies the reader's loaded DLL and completes the product-bound child checks without a Release product.",
      "A refused-disable test in which lifecycle admission and repair dispatch see the same active save or load.",
      "Restored observations for the interval while services remain stopped and for a confirmation already taken when the stop occurs."
    ],
    "source_inspection_only": [
      "Traced Test-Tiers configuration forwarding through the wrapper. Two of eight product-DLL invocations receive the selected configuration. The other six default to Release.",
      "Compared the fixture's independent persistence flags with CompositionRoot, where the disable checks and repair write gate read the same persistence service.",
      "Confirmed that fresh construction filters the exact stock Bite-to-Trip grant at the Horse donor boundary and that the active repair path contains no request-file ingestion."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "The reader now opens bin/<Configuration>/KingmakerMountedCombat.dll and logs its identity. The only supplied execution log identifies Release. The Debug result and abbreviated identity appear in the developer report and ledger.",
      "blocking_rationale": "The requested Debug artifact observation cannot be confirmed from the supplied execution evidence.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A recorded Debug selection loads the Debug product with the Release product unavailable.",
      "id": "R14-F2",
      "location": {
        "file": "scripts/Test-Finalize5CompanionReaders.ps1",
        "line": 97
      },
      "priority": "P2",
      "requested_correction": "Include the reader's machine-recorded Debug output in the review evidence. The run requested in R15-F1 can supply it.",
      "requested_evidence": "Show the Debug invocation, loaded path, SHA-256, MVID and reader result with bin/Release unavailable. Retain the required Release receipt for the corrected candidate.",
      "title": "The Debug reader run lacks a reviewable execution record",
      "trigger": "The reported standalone Debug run is used to close the previous artifact-selection finding."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S08"
      ],
      "actual": "The wrapper forwards Configuration only to the Trip contract and companion reader. Mounted Shield, Shield rollback, Mounted Archery, replay subscriber and both UMM profile invocations default to Release. A clean Debug output therefore fails the focused selection. An existing Release DLL can supply verdicts for different bytes.",
      "blocking_rationale": "The advertised Debug invocation can fail after a successful build or attribute protected-neighbor checks to the wrong artifact.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each product-bound child inspects the DLL built by the selected tier.",
      "id": "R15-F1",
      "location": {
        "file": "scripts/Test-MmrHorseTripFocused.ps1",
        "line": 31
      },
      "priority": "P2",
      "requested_correction": "Forward Configuration to the six remaining invocations that accept it, preserving their existing arguments and both loader profiles.",
      "requested_evidence": "Run the complete focused wrapper in Debug with bin/Release unavailable. Show all 13 children completing, the Debug artifact identity and both UMM product-binding results.",
      "title": "Six child checks in the Debug focused run still select Release",
      "trigger": "Test-Tiers selects Test-MmrHorseTripFocused.ps1 with -Configuration Debug."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The fixture sets life.Busy or life.Loading and leaves it set. Host.GateAdmits reads separate flags that stay open. The assertion consequently requires a removed result while the case still reports an active save or load.",
      "blocking_rationale": "The new test passes by asserting a write that the production persistence gate forbids. It misrepresents the interaction between a refused disable and the next repair update.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A refused disable leaves the confirmation armed. If the save or load is still active at dispatch, the update consumes the confirmation with zero native removals.",
      "id": "R15-F2",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 308
      },
      "priority": "P2",
      "requested_correction": "Use a common simulated persistence state for the lifecycle and repair hosts. Assert retained consent immediately after the refused disable. Make settlement explicit when testing a successful subsequent dispatch.",
      "requested_evidence": "With the save or load held open, show a guarded refusal, zero native calls and no replay after settlement. For the retained-consent success control, settle before the first repair update and show one exact removal.",
      "title": "The refused-disable fixture expects a removal during an active save or load",
      "trigger": "DisableAndFailureStop runs its save-in-flight or load-in-flight case and immediately dispatches the armed confirmation."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The revised test re-enables before every repair update in the successful-stop cases. It removes the prior zero-offer assertion while stopped and the taken-consent dispatch check. The separate selection test checks only that the old token fails, which would also pass if a stopped update exposed a fresh offer.",
      "blocking_rationale": "OWNER AMENDMENT 1 preserves behavioral tests that still apply. These removed controls covered distinct intervals that the new immediate-restart test cannot observe.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "While stopped, an eligible loaded horse produces no repair offer. A confirmation already taken before a stop cannot remove its fact afterward.",
      "id": "R15-F3",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 279
      },
      "priority": "P2",
      "requested_correction": "Keep the immediate-restart regression and restore the stopped-interval and already-taken-consent controls alongside it, using the production lifecycle sequence for the stop.",
      "requested_evidence": "Request presentation and update while stopped with an eligible horse loaded. Verify zero offers and native calls. Separately dispatch consent taken before the stop and verify the recorded stopped refusal and zero removals.",
      "title": "The lifecycle test dropped two previously exercised stop boundaries",
      "trigger": "A repair update occurs while services remain stopped, or a stop occurs after TakeConsent and before dispatch."
    },
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "The mapping describes every listed case as running through the updater with separated clicks. RepeatedClicksAndUpdates deliberately issues back-to-back clicks, and UmmPanelClassification invokes the classifier directly.",
      "blocking_rationale": "SelectionSurvivesUpdates independently establishes the required multi-update interaction.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The test mapping identifies which cases establish the multi-update interaction, rapid clicks and callback classification.",
      "id": "R15-F4",
      "location": {
        "file": "docs/missions/mmr-split/PR-4-amendment-test-mapping.md",
        "line": 65
      },
      "priority": "P3",
      "requested_correction": "Narrow the mapping's description to the cases that actually separate selection and confirmation.",
      "requested_evidence": "A source comparison with the named test methods is sufficient.",
      "title": "The test mapping overstates which cases separate the two clicks",
      "trigger": "The mapping is used to identify the evidence for the corrected interaction."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F1",
      "rationale": "The tests now execute the same Disable, FailureCleanup and Enable sequences used by CompositionRoot. For each successful stop, re-enable precedes the next repair update. The gate verified zero removals, rejection of the old token and one removal after fresh confirmation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F6",
      "rationale": "The Release gate bound the actual compiled Main.OnMaintenanceGui delegate, passed it through Chunk7PersistenceScenario.UmmPanelObservation and fed its returned values to the reader. Maintenance and historical no-panel controls passed. The unrelated handler, both mixed orders, inactive entry and disabled Mounted Charge controls were refused."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F1",
      "rationale": "TakeConsent preserves an unconfirmed selection. The recorded test kept A selected through five updates with zero native calls, confirmed A later, removed exactly once, preserved B and observed no replay over four further updates."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R14-F2",
      "rationale": "The hard-coded path in the named reader is corrected in source. Its requested Debug execution remains unverified because the supplied receipt and log cover Release only. R15-F1 identifies the separate configuration defect in sibling invocations."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r15-f53f21",
  "round": 15,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh KMC Horses filter the stock Mammoth TrippingBite grant and retain the bite and hoof limbs. For an existing horse, the maintenance action explains the provenance ambiguity, binds confirmation to one live horse and fact, and records the guarded removal outcome. The Release gate verifies the ordinary two-click interaction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07 in the integrated alpha. Observe a successful plain-Horse bite and damage without intrinsic Trip, with a legitimate dog, wolf or separately granted maneuver as a control where available.",
      "Inspect the maintenance UI and perform N10 on an existing horse. Confirm that ordinary save/reload preserves the repair, one pair and its expected effects, and that loading an older save requires fresh consent."
    ],
    "scope_reviewed": "PR-4 source and offline acceptance for MMR-06, S06 and S08 under OWNER AMENDMENT 1, including the round-15 corrections and their directly affected callers and checks. PR-6 owns integrated packaging.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
