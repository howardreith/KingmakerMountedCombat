<!-- agent-review:v1 enrollment=kmc-codex-mounted-shield-20261123114646-4433 key=r03-review part=1/1 sha256=6dfef8036bfc8a762277e2372de27a20a4777dbaaffeaf489690fcbce41d8316 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `41a49a5b8c`

**Decision:** `changes_required` — All four pending fixes are verified, and the private-alpha package matches the handoff. One defect remains in the revised MS-14 evidence check. It can credit Mounted Shield with preventing a hit on a natural 20, which Kingmaker resolves as a hit at either AC.

## Continuation for Claude

Restrict the Shield scenario’s AC-miss selection and reader to ordinary attack rolls whose outcome can depend on AC. Add the paired control in R3-F1 to the existing focused reader tests. Keep the four valid native miss classifications and the current attack identity, damage and reaction checks. Run the focused gate and refresh the package and handoff identities for the changed producer. This correction can be verified offline. OWNER AMENDMENT 2 assigns the broader gameplay and UI matrix to the owner’s alpha playtest.

#### Findings

**R3-F1** · P2 · defect · blocks readiness — The AC-miss reader can credit an automatic hit to Mounted Shield
- Location: `scripts/runtime/Sprint6ShieldEvidence.ps1:445`; acceptance: MS-14
- Trigger: A selected attack reports ShieldAC at the event and completion, no damage or defense attempt, AC 24, effective shield delta 3, and an attack die of 20 with bonus 2.
- Expected: Reject an automatic hit as evidence of an AC-caused miss. Kingmaker 2.1.7b’s RuleAttackRoll.IsSuccessRoll returns true on a natural 20 before comparing the attack total with TargetAC.
- Actual: ShieldBandMiss accepts a die above 1, and the reader at line 445 excludes only values at or below 1. With a newly accepted ShieldAC result, a natural 20 plus 2 totals 22, inside the permitted interval from 21 through 23. The row satisfies the predicates even though changing AC from 21 to 24 cannot make that roll miss. The band predicates also omit the recorded autoHit flag.
- Why it blocks / does not: The reader can issue a passing MS-14 verdict when the observed die rules out shield AC as the cause. OWNER AMENDMENT 2 defers live execution and retains correctness of the delivered diagnostic tooling.
- Requested correction: Require an ordinary, non-automatic roll from 2 through 19 in both ShieldBandMiss and the external band-miss reader.
- Requested evidence: Use the existing ShieldAC fixture at AC 24 with a +3 effective contribution. Accept 10 + 12 and reject 20 + 2 with the same total and otherwise identical observations. Also reject the ordinary-roll fixture with autoHit set. Retain the passing controls for all four native AC-miss classifications.

#### Prior finding assessments

- **R1-F2** — `fix_verified` — Setup and attack sampling now use EffectiveShieldDelta. The bound AC rule supplies the touch and Brilliant Energy flags. The gate confirms that KMC +3 adds three beside a force-stacking +4, adds zero beside an ordinary +4, and contributes zero to a bypassing or touch calculation.
- **R1-F6** — `fix_verified` — Every defense in a Shield check row compares the native event and final result, hit flag and critical confirmation with the defense record. The gate rejected both the second pair’s event-time CriticalHit and the planned negation with only its event critical flag set. Valid Parried and ordinary Miss controls passed.
- **R2-F1** — `fix_verified` — The producer recognizes Miss, DodgeAC, ArmorAC and ShieldAC. The reader checks those classifications at both observations and requires zero damage rules and no defense record. All four ordinary-roll controls passed, while MirrorImage, Concealment, a final hit and damage were rejected. R3-F1 concerns the separate automatic-hit boundary.
- **R2-F2** — `fix_verified` — The active resume record, CURRENT.md, handoff, private-alpha guide and ledger identify the fedb7008 package consistently. Direct archive readback confirmed the digest and byte count, the packaged DLL identity, version 0.2.16-alpha.1 and ManagerVersion 0.31.1.

#### Evidence assessment

- **Inspected:** The mission, both owner amendments, repository validation policy, feature diff, prior findings ledger, developer report, candidate gate receipt and log.; The correction from 39c5d755 through fedb7008, its production and diagnostic callers, the subsequent records-only commit, the package manifest and the active owner handoff.; The installed Kingmaker 2.1.7b method contracts for RuleAttackRoll.IsSuccessRoll, IsHit, AC calculation and native miss classification.
- **Executed verification:** The controller receipt binds the focused Fast gate to review head 41a49a5b. Its log hash matches the receipt. The gate exited 0 in 63.6 seconds with 1,525 component checks, 153 source checks and all 18 focused children passing. These included 217 Shield reader checks, 114 offline native-contract checks, 68 selection rollback checks and compiled binding against UMM 0.31.1 and 0.33.0.; I read the private-alpha ZIP and independently matched its 4,283,148 bytes and SHA-256 8c1c5ae99c03dc53eab0cb64cd4a6f67685b61af0d26aebddb32a798d08c653c. It contains only Info.json and KingmakerMountedCombat.dll. The DLL matches the gate build, with SHA-256 2822469373d20a47eaff21c305c8325329dfbc10f5138ea95ce46a8b9476fe41 and MVID c4e178d8-355d-4a79-93ad-ff1af7cb7c7b.; I read the named Mounted Shield resource from the packaged DLL. Its 4,408 bytes hash to fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0, matching both the packet and production PNGs.
- **Source inspection only:** Traced the shared effective shield delta from band setup through the attack’s own RuleCalculateAC, including touch and Brilliant Energy handling.; Traced negated defenses through the hit-event and final observations, and the selected AC miss through its attack identity, numeric margin, damage and reaction checks. Compared those predicates with the native natural-20 rule.
- **Required gaps:** R3-F1 requires an offline control showing that an automatic hit cannot qualify as a miss caused by Mounted Shield.
- **Limitations:** The foreground developer transcript was withheld. The executed gate evidence comes from the controller receipt. Local script execution policy prevented a separate reviewer replay of the reader, so R3-F1 is a source counterexample checked against the installed engine contract.; Zero guarded game starts occurred. No disposable seed or prepared save was created. The documented investigation found that save-backed routes require the historical qualification suite and the no-save smoke lacks the required profile protection. No live restoration was due.; The supplemental clearance applies to bb903669 and predates the controller corrections. The historical broad Candidate failures retain their recorded baseline classification.

#### Summary

- **Delivered behavior:** The private alpha implements one passive rider feat. The active physical shield’s base defense and applicable focus bonuses supply the mount’s shield AC and the rider’s designated Mounted Combat check. The package, icon and loader identities are verified offline.
- **Scope reviewed:** Final private-alpha checkpoint for draft PR #61 under OWNER AMENDMENTS 1 and 2. The packaged production source is fedb70086495599273824a500da3b2b5c7122c08. The review head adds six records files.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Native feat selection, prerequisites, Back/cancel, respec, icon appearance and character-sheet attribution.; In-game +3/+4 amounts, enhancement exclusion, competing shields, suppression, equipment changes and the Mounted Combat reaction in real-time and turn-based play.; Multiple pairs, dismount, death, area transitions, save/reload, compatibility and the eventual merge or release decision.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73`.

Codex CLI `0.170.0`, thread `01a20c0e-e5b3-7841-b8ef-e4c26379dc74`, duration 1138.2s, response sha256 `3e01debe3dfd445a7991347a41f2982cba7b94fe5f26e31caed6aa32eaf752e8`, request `kmc-codex-mounted-shield-20261123114646-4433-r03-de3ae8`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "41a49a5b8c9ff49eeeaad397ea3d15ca8e752e14",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "All four pending fixes are verified, and the private-alpha package matches the handoff. One defect remains in the revised MS-14 evidence check. It can credit Mounted Shield with preventing a hit on a natural 20, which Kingmaker resolves as a hit at either AC.\n\n## Continuation for Claude\n\nRestrict the Shield scenario\u2019s AC-miss selection and reader to ordinary attack rolls whose outcome can depend on AC. Add the paired control in R3-F1 to the existing focused reader tests. Keep the four valid native miss classifications and the current attack identity, damage and reaction checks. Run the focused gate and refresh the package and handoff identities for the changed producer. This correction can be verified offline. OWNER AMENDMENT 2 assigns the broader gameplay and UI matrix to the owner\u2019s alpha playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the focused Fast gate to review head 41a49a5b. Its log hash matches the receipt. The gate exited 0 in 63.6 seconds with 1,525 component checks, 153 source checks and all 18 focused children passing. These included 217 Shield reader checks, 114 offline native-contract checks, 68 selection rollback checks and compiled binding against UMM 0.31.1 and 0.33.0.",
      "I read the private-alpha ZIP and independently matched its 4,283,148 bytes and SHA-256 8c1c5ae99c03dc53eab0cb64cd4a6f67685b61af0d26aebddb32a798d08c653c. It contains only Info.json and KingmakerMountedCombat.dll. The DLL matches the gate build, with SHA-256 2822469373d20a47eaff21c305c8325329dfbc10f5138ea95ce46a8b9476fe41 and MVID c4e178d8-355d-4a79-93ad-ff1af7cb7c7b.",
      "I read the named Mounted Shield resource from the packaged DLL. Its 4,408 bytes hash to fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0, matching both the packet and production PNGs."
    ],
    "inspected": [
      "The mission, both owner amendments, repository validation policy, feature diff, prior findings ledger, developer report, candidate gate receipt and log.",
      "The correction from 39c5d755 through fedb7008, its production and diagnostic callers, the subsequent records-only commit, the package manifest and the active owner handoff.",
      "The installed Kingmaker 2.1.7b method contracts for RuleAttackRoll.IsSuccessRoll, IsHit, AC calculation and native miss classification."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. The executed gate evidence comes from the controller receipt. Local script execution policy prevented a separate reviewer replay of the reader, so R3-F1 is a source counterexample checked against the installed engine contract.",
      "Zero guarded game starts occurred. No disposable seed or prepared save was created. The documented investigation found that save-backed routes require the historical qualification suite and the no-save smoke lacks the required profile protection. No live restoration was due.",
      "The supplemental clearance applies to bb903669 and predates the controller corrections. The historical broad Candidate failures retain their recorded baseline classification."
    ],
    "required_gaps": [
      "R3-F1 requires an offline control showing that an automatic hit cannot qualify as a miss caused by Mounted Shield."
    ],
    "source_inspection_only": [
      "Traced the shared effective shield delta from band setup through the attack\u2019s own RuleCalculateAC, including touch and Brilliant Energy handling.",
      "Traced negated defenses through the hit-event and final observations, and the selected AC miss through its attack identity, numeric margin, damage and reaction checks. Compared those predicates with the native natural-20 rule."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MS-14"
      ],
      "actual": "ShieldBandMiss accepts a die above 1, and the reader at line 445 excludes only values at or below 1. With a newly accepted ShieldAC result, a natural 20 plus 2 totals 22, inside the permitted interval from 21 through 23. The row satisfies the predicates even though changing AC from 21 to 24 cannot make that roll miss. The band predicates also omit the recorded autoHit flag.",
      "blocking_rationale": "The reader can issue a passing MS-14 verdict when the observed die rules out shield AC as the cause. OWNER AMENDMENT 2 defers live execution and retains correctness of the delivered diagnostic tooling.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Reject an automatic hit as evidence of an AC-caused miss. Kingmaker 2.1.7b\u2019s RuleAttackRoll.IsSuccessRoll returns true on a natural 20 before comparing the attack total with TargetAC.",
      "id": "R3-F1",
      "location": {
        "file": "scripts/runtime/Sprint6ShieldEvidence.ps1",
        "line": 445
      },
      "priority": "P2",
      "requested_correction": "Require an ordinary, non-automatic roll from 2 through 19 in both ShieldBandMiss and the external band-miss reader.",
      "requested_evidence": "Use the existing ShieldAC fixture at AC 24 with a +3 effective contribution. Accept 10 + 12 and reject 20 + 2 with the same total and otherwise identical observations. Also reject the ordinary-roll fixture with autoHit set. Retain the passing controls for all four native AC-miss classifications.",
      "title": "The AC-miss reader can credit an automatic hit to Mounted Shield",
      "trigger": "A selected attack reports ShieldAC at the event and completion, no damage or defense attempt, AC 24, effective shield delta 3, and an attack die of 20 with bonus 2."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "Setup and attack sampling now use EffectiveShieldDelta. The bound AC rule supplies the touch and Brilliant Energy flags. The gate confirms that KMC +3 adds three beside a force-stacking +4, adds zero beside an ordinary +4, and contributes zero to a bypassing or touch calculation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "Every defense in a Shield check row compares the native event and final result, hit flag and critical confirmation with the defense record. The gate rejected both the second pair\u2019s event-time CriticalHit and the planned negation with only its event critical flag set. Valid Parried and ordinary Miss controls passed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "The producer recognizes Miss, DodgeAC, ArmorAC and ShieldAC. The reader checks those classifications at both observations and requires zero damage rules and no defense record. All four ordinary-roll controls passed, while MirrorImage, Concealment, a final hit and damage were rejected. R3-F1 concerns the separate automatic-hit boundary."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "The active resume record, CURRENT.md, handoff, private-alpha guide and ledger identify the fedb7008 package consistently. Direct archive readback confirmed the digest and byte count, the packaged DLL identity, version 0.2.16-alpha.1 and ManagerVersion 0.31.1."
    }
  ],
  "request_id": "kmc-codex-mounted-shield-20261123114646-4433-r03-de3ae8",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The private alpha implements one passive rider feat. The active physical shield\u2019s base defense and applicable focus bonuses supply the mount\u2019s shield AC and the rider\u2019s designated Mounted Combat check. The package, icon and loader identities are verified offline.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Native feat selection, prerequisites, Back/cancel, respec, icon appearance and character-sheet attribution.",
      "In-game +3/+4 amounts, enhancement exclusion, competing shields, suppression, equipment changes and the Mounted Combat reaction in real-time and turn-based play.",
      "Multiple pairs, dismount, death, area transitions, save/reload, compatibility and the eventual merge or release decision."
    ],
    "scope_reviewed": "Final private-alpha checkpoint for draft PR #61 under OWNER AMENDMENTS 1 and 2. The packaged production source is fedb70086495599273824a500da3b2b5c7122c08. The review head adds six records files.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
