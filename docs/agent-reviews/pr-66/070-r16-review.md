<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r16-review part=1/1 sha256=cf1e33ba1573714c5f5efe80e0ade28677d01f9cc2e6f2a7609cac187a2c4621 -->
### Codex GPT-6.2 Astra — review — round 16 — candidate `d615d1d5ce`

**Decision:** `ready_for_owner_review` — All four blocking findings are resolved. The Release gate passed at the candidate, and the Debug run exercised all 13 focused children with bin/Release unavailable. The lifecycle tests now cover both persistence timing and stopped-service dispatch. One optional correction remains in the test mapping. OWNER AMENDMENT 1 reserves native gameplay and save/load acceptance for the integrated-alpha playtest.

#### Findings

**R15-F4** · P3 · optional_polish · non-blocking — The test mapping misses a second back-to-back confirmation
- Location: `docs/missions/mmr-split/PR-4-amendment-test-mapping.md:65`; acceptance: MMR-06
- Trigger: A reader uses the mapping to identify which successful repairs separate selection and confirmation with an update.
- Expected: The mapping identifies which tests demonstrate selection surviving intervening updates and which issue consecutive clicks.
- Actual: TwoHorsesAndCancellation also selects horse B and immediately confirms B at tests/HorseTripRepairPolicyTests.cs lines 156–157. Its next update performs the repair.
- Why it blocks / does not: SelectionSurvivesUpdates independently exercises five intervening updates, one exact removal from the selected horse and zero replays.
- Requested correction: Name the successful B branch of TwoHorsesAndCancellation alongside RepeatedClicksAndUpdates, or cite SelectionSurvivesUpdates directly for the multi-update guarantee.
- Requested evidence: A source comparison with the named methods is sufficient.

#### Prior finding assessments

- **R14-F2** — `fix_verified` — The Debug log shows the invocation, the directory listing with Release moved aside, and 83 passing reader checks. The reader loaded bin/Debug/KingmakerMountedCombat.dll with SHA-256 7b233483f90205f42a051a2fe540484826c0824e697e4f93bd703352c310e88a and MVID 5f8fce8d-1e4f-4a7e-8438-23c1c750dd2e.
- **R15-F1** — `fix_verified` — The wrapper forwards Configuration to every child that declares it and preserves the existing profile and reference arguments. The Debug log shows the six affected invocations selecting Debug, including the UMM 0.31.1 and 0.33.0 product bindings.
- **R15-F2** — `fix_verified` — The lifecycle fixture and repair host now read the same save and load state. The passing component case checks that a refused disable retains armed consent. Holding either operation open produces a guarded refusal, zero removal calls and no replay after settlement. Settling before dispatch produces one classified exact removal.
- **R15-F3** — `fix_verified` — The passing lifecycle case retains immediate re-enable coverage and restores both stop controls through ServiceLifecycleSequence.Disable. A stopped update exposes zero offers and makes zero removal calls. Dispatching consent taken before the stop records the stopped refusal and leaves the fact present.
- **R15-F4** — `fix_rejected` — The mapping now describes the direct classifier test and the rapid-click test correctly. Its claim about every other successful repair still conflicts with the final selection and confirmation of B in TwoHorsesAndCancellation. This remains optional wording polish.

#### Evidence assessment

- **Inspected:** The controller mission, OWNER AMENDMENT 1, repository validation policy, handoff documents, developer report, transcript notice and prior findings ledger.; The 40-file feature diff and the round-16 correction. The commit after the Debug run adds only the run log and ledger update. Product, test and script sources match the candidate.; The Horse donor filter, repair candidate predicate, UMM callback, update-time dispatch, exact fact removal, persistence gate, lifecycle sequence and focused validation wrapper.
- **Executed verification:** Verified the controller receipt against HEAD and its log SHA-256. The required Release Fast gate exited 0 in 98.0 seconds with 1,548 component tests, 153 source checks and 13 of 13 focused children passing. The recorded cumulative spend was 366.47 of 390 minutes.; The Release log records 51 passing Trip contract checks, 83 companion-reader checks, 114 Mounted Shield checks and 68 rollback checks. The packet, production and embedded shield icon bytes matched.; The committed Debug log records a 31.1-second run at acdb7198 with bin/Release moved aside, 13 of 13 children passing and exit status 0. Both UMM profiles bound all 20 referenced members against the Debug product. The developer’s log checksum is reproducible from the committed text with its original mixed line endings.
- **Source inspection only:** Traced how the live party, player and area objects, persistence state and native feature collection feed the tested repair updater.; Compared the mapping’s interaction claims with the actual Select, Confirm and Update calls in the component tests.
- **Required gaps:** none
- **Limitations:** The Release run has a controller receipt. The separate Debug run has a tracked execution log. The foreground session transcript was withheld.; The recorded checks used compiled contracts, installed Kingmaker data and simulated engine boundaries. UI rendering, native combat, gameplay save/load and Linux Unity/Mono execution remain unobserved for this candidate.

#### Summary

- **Delivered behavior:** Fresh KMC horses exclude the stock Mammoth donor’s Bite-triggered Trip grant and retain their bite and two hooves. For an existing horse, the UMM action explains the provenance ambiguity, requests confirmation for one eligible fact, and rechecks the live horse, stable ID, fact instance, session and write gate before removal. Legacy request files are inert.
- **Scope reviewed:** Final PR-4 source and offline checkpoint for MMR-06, S06 and the protected S08 neighbors under OWNER AMENDMENT 1. Integrated packaging belongs to PR-6.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N07. Observe a plain horse land a damaging bite without automatic Trip. Where available, compare a legitimate dog or wolf Trip and an explicit or independently granted maneuver.; Check that the UMM panel clearly identifies the horse, owner and proposed removal, and that confirmation and cancellation are understandable in play.; N10. Save and reload a repaired horse and verify that the repair persists, the pair remains usable, and restoration introduces no duplicate effects or extra action.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`.

Codex CLI `0.170.0`, thread `01a21e63-df27-7fa0-a138-bd2feb816259`, duration 530.0s, response sha256 `a64132745bf9474bbdccdf111e423dc7a9bbf7f5b653520649ccd305af7e52c2`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r16-4636fd`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "d615d1d5ceb650ed13ca4cb918d2214dd6e087a8",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "ready_for_owner_review",
  "decision_rationale": "All four blocking findings are resolved. The Release gate passed at the candidate, and the Debug run exercised all 13 focused children with bin/Release unavailable. The lifecycle tests now cover both persistence timing and stopped-service dispatch. One optional correction remains in the test mapping. OWNER AMENDMENT 1 reserves native gameplay and save/load acceptance for the integrated-alpha playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the controller receipt against HEAD and its log SHA-256. The required Release Fast gate exited 0 in 98.0 seconds with 1,548 component tests, 153 source checks and 13 of 13 focused children passing. The recorded cumulative spend was 366.47 of 390 minutes.",
      "The Release log records 51 passing Trip contract checks, 83 companion-reader checks, 114 Mounted Shield checks and 68 rollback checks. The packet, production and embedded shield icon bytes matched.",
      "The committed Debug log records a 31.1-second run at acdb7198 with bin/Release moved aside, 13 of 13 children passing and exit status 0. Both UMM profiles bound all 20 referenced members against the Debug product. The developer\u2019s log checksum is reproducible from the committed text with its original mixed line endings."
    ],
    "inspected": [
      "The controller mission, OWNER AMENDMENT 1, repository validation policy, handoff documents, developer report, transcript notice and prior findings ledger.",
      "The 40-file feature diff and the round-16 correction. The commit after the Debug run adds only the run log and ledger update. Product, test and script sources match the candidate.",
      "The Horse donor filter, repair candidate predicate, UMM callback, update-time dispatch, exact fact removal, persistence gate, lifecycle sequence and focused validation wrapper."
    ],
    "limitations": [
      "The Release run has a controller receipt. The separate Debug run has a tracked execution log. The foreground session transcript was withheld.",
      "The recorded checks used compiled contracts, installed Kingmaker data and simulated engine boundaries. UI rendering, native combat, gameplay save/load and Linux Unity/Mono execution remain unobserved for this candidate."
    ],
    "required_gaps": [],
    "source_inspection_only": [
      "Traced how the live party, player and area objects, persistence state and native feature collection feed the tested repair updater.",
      "Compared the mapping\u2019s interaction claims with the actual Select, Confirm and Update calls in the component tests."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "TwoHorsesAndCancellation also selects horse B and immediately confirms B at tests/HorseTripRepairPolicyTests.cs lines 156\u2013157. Its next update performs the repair.",
      "blocking_rationale": "SelectionSurvivesUpdates independently exercises five intervening updates, one exact removal from the selected horse and zero replays.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The mapping identifies which tests demonstrate selection surviving intervening updates and which issue consecutive clicks.",
      "id": "R15-F4",
      "location": {
        "file": "docs/missions/mmr-split/PR-4-amendment-test-mapping.md",
        "line": 65
      },
      "priority": "P3",
      "requested_correction": "Name the successful B branch of TwoHorsesAndCancellation alongside RepeatedClicksAndUpdates, or cite SelectionSurvivesUpdates directly for the multi-update guarantee.",
      "requested_evidence": "A source comparison with the named methods is sufficient.",
      "title": "The test mapping misses a second back-to-back confirmation",
      "trigger": "A reader uses the mapping to identify which successful repairs separate selection and confirmation with an update."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F2",
      "rationale": "The Debug log shows the invocation, the directory listing with Release moved aside, and 83 passing reader checks. The reader loaded bin/Debug/KingmakerMountedCombat.dll with SHA-256 7b233483f90205f42a051a2fe540484826c0824e697e4f93bd703352c310e88a and MVID 5f8fce8d-1e4f-4a7e-8438-23c1c750dd2e."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R15-F1",
      "rationale": "The wrapper forwards Configuration to every child that declares it and preserves the existing profile and reference arguments. The Debug log shows the six affected invocations selecting Debug, including the UMM 0.31.1 and 0.33.0 product bindings."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R15-F2",
      "rationale": "The lifecycle fixture and repair host now read the same save and load state. The passing component case checks that a refused disable retains armed consent. Holding either operation open produces a guarded refusal, zero removal calls and no replay after settlement. Settling before dispatch produces one classified exact removal."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R15-F3",
      "rationale": "The passing lifecycle case retains immediate re-enable coverage and restores both stop controls through ServiceLifecycleSequence.Disable. A stopped update exposes zero offers and makes zero removal calls. Dispatching consent taken before the stop records the stopped refusal and leaves the fact present."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R15-F4",
      "rationale": "The mapping now describes the direct classifier test and the rapid-click test correctly. Its claim about every other successful repair still conflicts with the final selection and confirmation of B in TwoHorsesAndCancellation. This remains optional wording polish."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r16-4636fd",
  "round": 16,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh KMC horses exclude the stock Mammoth donor\u2019s Bite-triggered Trip grant and retain their bite and two hooves. For an existing horse, the UMM action explains the provenance ambiguity, requests confirmation for one eligible fact, and rechecks the live horse, stable ID, fact instance, session and write gate before removal. Legacy request files are inert.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07. Observe a plain horse land a damaging bite without automatic Trip. Where available, compare a legitimate dog or wolf Trip and an explicit or independently granted maneuver.",
      "Check that the UMM panel clearly identifies the horse, owner and proposed removal, and that confirmation and cancellation are understandable in play.",
      "N10. Save and reload a repaired horse and verify that the repair persists, the pair remains usable, and restoration introduces no duplicate effects or extra action."
    ],
    "scope_reviewed": "Final PR-4 source and offline checkpoint for MMR-06, S06 and the protected S08 neighbors under OWNER AMENDMENT 1. Integrated packaging belongs to PR-6.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
