<!-- agent-review:v1 enrollment=kmc-codex-mounted-shield-20261123114646-4433 key=r02-review part=1/1 sha256=4da50418dd380bec8c9814d8d68d3ecf81189ae7a6ee8fd272399d06f7061ffa -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `39c5d755d3`

**Decision:** `changes_required` — The exact candidate passed its focused gate, and I verified the private-alpha ZIP and its DLL. The Shield diagnostics still discard valid native AC misses, miscalculate the shield’s effect in two attack contexts, and can credit a negation while the hit-event observer still reports a confirmed critical. The owner handoff also mixes identities from two packages.

## Continuation for Claude

Correct the MS-14 producer and reader together. Use Kingmaker’s native AC-miss reasons, use the same stacking calculation at setup and during observation, and account for attacks that ignore shield AC. Complete the event and final-state checks for every credited negation. Add the focused positive and negative cases in the findings.

Run the existing focused gate on the corrected source and package the changed DLL with a fresh immutable qualifier. Read the ZIP size, hashes, MVID and metadata from that artifact when updating the active handoff and resume record. Preserve historical receipts and budget entries. OWNER AMENDMENT 2 assigns broader gameplay and UI acceptance to the owner. These corrections can be verified offline within the private-alpha scope.

#### Findings

**R1-F6** · P2 · defect · blocks readiness — A negated defense can still expose a critical hit at the hit event
- Location: `scripts/runtime/Sprint6ShieldEvidence.ps1:308`; acceptance: MS-11, MS-15
- Trigger: A successful Shield-row defense has a clean final outcome after the hit-event observer saw a hit or retained critical confirmation.
- Expected: Each credited negation agrees with its bound native attack at the hit event and at completion. Critical confirmation is clear at both points and the attack deals zero damage.
- Actual: The new all-records branch checks the final result, final hit flag, damage-rule count and final critical flag. It omits the native event result, hit flag and critical flag. A second rider’s natural defense can therefore report CriticalHit at the event and Parried at completion and pass these predicates. For a planned parry, changing only criticalAtEvent to true also passes the shared planned-success helper.
- Why it blocks / does not: The reader can certify critical cleanup despite the observer recording a confirmed critical at the point other hit-event consumers see it.
- Requested correction: Validate the recorded negated outcome, hit and critical flags at both observations for planned and natural defenses. Preserve legitimate melee Parried and ordinary Miss outcomes.
- Requested evidence: Reject a natural second-pair success with a confirmed CriticalHit at the event followed by a clean final Parried. Reject a planned success with only criticalAtEvent changed. Retain passing controls for both valid negation outcomes.

**R2-F1** · P2 · defect · blocks readiness — The miss scenario rejects Kingmaker’s native shield, armor and dodge AC results
- Location: `src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs:967`; acceptance: MS-14
- Trigger: An ordinary failed attack inside the shield’s AC margin receives a specific native AC-miss reason.
- Expected: The driver and reader recognize the native AC-miss classifications and use the measured AC margin to establish whether Mounted Shield prevented the hit.
- Actual: ShieldBandMiss filters for the literal Miss, and Sprint6ShieldEvidence.ps1 line 432 requires that same literal at both observations. Kingmaker also returns ShieldAC, ArmorAC and DodgeAC from its ordinary failed-roll path. A roll totaling 22 against AC 24, which would hit AC 21 without Mounted Shield, is discarded if the engine labels it ShieldAC.
- Why it blocks / does not: The producer can consume its bounded strike allowance while discarding qualifying observations. The reader also rejects valid evidence for the required AC-miss case.
- Requested correction: Use the verified AC-miss result set in both producer and reader. Check the event and final observations, retain the attack identity and numeric margin checks, and require zero damage and no defense attempt.
- Requested evidence: Show otherwise identical qualifying rows for Miss, ShieldAC, ArmorAC and DodgeAC. Keep negative controls for MirrorImage, concealment, a final hit and damage on the selected attack.

**R1-F2** · P2 · defect · blocks readiness — The miss scenario still miscalculates Mounted Shield’s effective AC change
- Location: `src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs:931`; acceptance: MS-09, MS-10, MS-14
- Trigger: The mount has a force-stacking Shield source during band setup, or the sampled melee attack ignores shield AC through Brilliant Energy.
- Expected: The setup and bound attack observation agree on the AC change attributable to Mounted Shield. A force-stacking source leaves KMC’s ordinary bonus effective. A Brilliant Energy attack ignores it.
- Actual: The production Brilliant Energy fix passes. The diagnostic setup at line 931 still counts every foreign Shield modifier as a competitor, so a force-stacking +4 makes it record a zero delta and skip the band even though KMC adds +3. The attack-time sample has the converse problem for Brilliant Energy. It receives only the target and can record an effective +3 on a melee attack whose native target AC is identical with and without the feat.
- Why it blocks / does not: The diagnostic can skip a measurable bonus or attribute a miss to a bonus the attack ignored. R1-F2 explicitly required the diagnostic AC delta to agree with native stacking behavior.
- Requested correction: Use the verified non-stacking competitor calculation in both diagnostic paths. Carry the bound AC rule’s shield-bypass state into the observation so a Brilliant Energy attack cannot qualify as a shield-caused miss.
- Requested evidence: With KMC +3, show that a force-stacking +4 leaves a measurable band of three, an ordinary competing +4 leaves zero, and a Brilliant Energy melee attack contributes zero to the measured band. Retain the passing production AC controls.

**R2-F2** · P2 · required_gap · blocks readiness — The active handoff carries the previous alpha’s digest and size
- Location: `AUTONOMOUS-RESUME.md:8`; acceptance: MS-25, MS-27
- Trigger: The owner verifies the current alpha using the resume record or private-alpha package table.
- Expected: Every active package reference identifies the same immutable ZIP by filename, digest and byte count.
- Actual: AUTONOMOUS-RESUME.md pairs the 6fb53633 filename with fa5fb284, the digest prefix of the superseded bb903669 ZIP. PRIVATE-ALPHA.md line 28 and HANDOFF.md line 137 retain its 4,282,758-byte size. The current archive is 4,282,777 bytes with SHA-256 c3d80a58afa52d0506b001c2cfbddc776af1ade9b6796a5d5f3522c2f725b0bb.
- Why it blocks / does not: OWNER AMENDMENT 2 requires an accurate handoff with exact artifact identities. Following the active records gives the owner conflicting verification values for the same filename.
- Requested correction: Populate the active resume record, handoff and private-alpha guide from the final archive and manifest. Keep earlier identities in their dated historical entries.
- Requested evidence: Read back the final ZIP length and SHA-256 and compare them with each active owner-facing reference.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — Both Shield scenarios refuse an invocation without BaseGameOnly before workflow admission. The admitted path selects the profile snapshot, Areas isolation and post-exit restoration. The gate records both real refusals and the offline Areas restoration checks.
- **R1-F2** — `fix_rejected` — The game-facing correction is verified. The genuine force-stacking +4 control produces melee AC 10 with and without KMC, and the touch control passes. The requested diagnostic consistency is incomplete because the pre-strike calculation still treats that same +4 as a competitor.
- **R1-F3** — `fix_verified` — The focused check now invokes the actual v3 resolver. The resolver admits the packaged 0.2.16-alpha.1 under kmc-global-subscribers-r2 and rejects earlier releases and malformed versions in the recorded gate.
- **R1-F4** — `fix_verified` — The producer records the acquired owner and pet. The reader binds the ordinary, disabled and reciprocal cross-pair snapshots to those actors and the measured pair. The duplicated-snapshot and missing-identity controls pass.
- **R1-F5** — `fix_verified` — A MirrorImage event can no longer satisfy the band check, and the reader rejects a final hit. R2-F1 covers the distinct regression introduced by restricting valid AC misses to a single enum value.
- **R1-F6** — `fix_rejected` — The added tests reject a damaging natural negation and a retained final critical. The event-only critical and natural event-result contradictions remain unchecked.
- **R1-F7** — `fix_verified` — The gate invokes production EffectiveShield on genuine hand-slot, shield and modifier objects. It accepts the matching active shield, rejects the inactive slot, removed modifier and foreign sources, and accepts the restored matching modifier.

#### Evidence assessment

- **Inspected:** Controller mission, both owner amendments, seven prior findings, developer report, transcript notice, gate receipt and log, repository instructions, validation policy, current-state records and feature diff.; Correction commit 6fb53633, its directly affected production and diagnostic consumers, and the six records-only changes from that commit to 39c5d755. Companion commit 5be0f737 is an ancestor of the candidate.; The current and superseded package manifests, private-alpha guide, acceptance ledger and supplemental review verdict on bb903669.
- **Executed verification:** The controller recorded focused Fast passes on implementation commit 6fb53633 in 87.3 seconds and candidate 39c5d755 in 64.9 seconds. The candidate log hash matches its receipt. It records 1,525 component checks, 153 source checks and 18/18 focused scripts, including Shield native contracts 111/0, selection rollback 68/0, Shield readers 206/0 and replay subscriber checks 29/0.; The candidate build used the pinned UMM 0.31.1 reference. The compiled product bound all 20 loader members against both 0.31.1 and installed 0.33.0, with 25/0 checks for each. The receipt records the mounted-shield budget.; I independently ran the read-only Test-Sprint6ShieldReaders.ps1 suite in the snapshot. It reported 206 passed and zero failed.; I verified the ZIP’s SHA-256 and 4,282,777-byte length, checked both entries’ CRCs, read the packaged Info.json and hashed the packaged DLL. Its SHA-256 is e2fda1f2f0e41618729f59ab855c7848e27500df3b4222589e42e506b4723825. The package manifest and gate agree on MVID 1f6af610-74f3-4c8c-ba9a-421c02b2781e.; All 26 mission-packet checksums matched. The supplied 4,408-byte PNG occurs once in the packaged DLL and matches the repository PNG. The gate independently extracted the named resource with SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.
- **Source inspection only:** Static inspection of the pinned Kingmaker assembly, SHA-256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, confirmed that RuleAttackRoll.OnTrigger calls ModifiableValueArmorClass.SelectMissReason for a failed numeric roll. That method can return Miss, DodgeAC, ArmorAC or ShieldAC.; The remaining band-calculation and negation findings follow from the producer, observer, reader and supplied fixture paths. The runner’s accepted snapshot and restoration branches were inspected alongside its recorded offline checks.
- **Required gaps:** Align the AC-miss driver and reader with the pinned engine’s result classifications and actual shield AC delta.; Validate successful negations at the hit event and rule completion, including critical cleanup.; Update the active artifact records from the corrected package and provide its current focused receipt.
- **Limitations:** Zero guarded game starts were reported. No disposable save, gameplay smoke, UI observation or live restoration receipt was produced. The bounded fixture investigation found that save-backed admission requires the absent qualification suite.; The foreground transcript was withheld. Executed build and gate claims above come from controller receipts and logs. The supplemental clearance applies to bb903669 under its no-live-use plan.; The review shell’s PowerShell language-mode boundary prevented importing the fixture for additional interactive mutations. The shipped 206-case suite ran directly. The new event-only counterexamples below are source traces.; Historical broad Candidate failures involving the missing package and run directory retain their recorded dispositions.

#### Summary

- **Delivered behavior:** The private alpha packages one passive rider feat requiring Mounted Combat and Shield Focus. Its mount-AC contribution and existing Mounted Combat check use the same physical-shield base and focus amounts. The packaged version is 0.2.16-alpha.1 with UMM 0.31.1 as its declared minimum.
- **Scope reviewed:** Final private-alpha checkpoint under OWNER AMENDMENT 2. The packaged implementation is 6fb53633. Candidate 39c5d755 adds records only. The review covered all seven prior findings, their directly affected consumers and the owner handoff.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Playtest ordinary and fighter selection, same-level choices, Back/cancel, respec, icon presentation and character-sheet attribution.; Observe shield amounts and enhancement exclusion, competing bonuses, reaction costs and boundaries in real-time and turn-based play, equipment and pair changes, save/load, cleanup and compatibility.; Judge the artwork and private-alpha gameplay. Draft PR #61 remains under the owner’s merge and release authority.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73`.

Codex CLI `0.170.0`, thread `01a20bf5-5fe4-7953-bec6-d032d17cdc19`, duration 988.3s, response sha256 `c69b9a5d7be971241e8949582aacbdc9e90068c4a003ba973b55d486c4b4e54f`, request `kmc-codex-mounted-shield-20261123114646-4433-r02-e65be0`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "39c5d755d39236cc0d13d6f7caecead5fed2a324",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The exact candidate passed its focused gate, and I verified the private-alpha ZIP and its DLL. The Shield diagnostics still discard valid native AC misses, miscalculate the shield\u2019s effect in two attack contexts, and can credit a negation while the hit-event observer still reports a confirmed critical. The owner handoff also mixes identities from two packages.\n\n## Continuation for Claude\n\nCorrect the MS-14 producer and reader together. Use Kingmaker\u2019s native AC-miss reasons, use the same stacking calculation at setup and during observation, and account for attacks that ignore shield AC. Complete the event and final-state checks for every credited negation. Add the focused positive and negative cases in the findings.\n\nRun the existing focused gate on the corrected source and package the changed DLL with a fresh immutable qualifier. Read the ZIP size, hashes, MVID and metadata from that artifact when updating the active handoff and resume record. Preserve historical receipts and budget entries. OWNER AMENDMENT 2 assigns broader gameplay and UI acceptance to the owner. These corrections can be verified offline within the private-alpha scope.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller recorded focused Fast passes on implementation commit 6fb53633 in 87.3 seconds and candidate 39c5d755 in 64.9 seconds. The candidate log hash matches its receipt. It records 1,525 component checks, 153 source checks and 18/18 focused scripts, including Shield native contracts 111/0, selection rollback 68/0, Shield readers 206/0 and replay subscriber checks 29/0.",
      "The candidate build used the pinned UMM 0.31.1 reference. The compiled product bound all 20 loader members against both 0.31.1 and installed 0.33.0, with 25/0 checks for each. The receipt records the mounted-shield budget.",
      "I independently ran the read-only Test-Sprint6ShieldReaders.ps1 suite in the snapshot. It reported 206 passed and zero failed.",
      "I verified the ZIP\u2019s SHA-256 and 4,282,777-byte length, checked both entries\u2019 CRCs, read the packaged Info.json and hashed the packaged DLL. Its SHA-256 is e2fda1f2f0e41618729f59ab855c7848e27500df3b4222589e42e506b4723825. The package manifest and gate agree on MVID 1f6af610-74f3-4c8c-ba9a-421c02b2781e.",
      "All 26 mission-packet checksums matched. The supplied 4,408-byte PNG occurs once in the packaged DLL and matches the repository PNG. The gate independently extracted the named resource with SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0."
    ],
    "inspected": [
      "Controller mission, both owner amendments, seven prior findings, developer report, transcript notice, gate receipt and log, repository instructions, validation policy, current-state records and feature diff.",
      "Correction commit 6fb53633, its directly affected production and diagnostic consumers, and the six records-only changes from that commit to 39c5d755. Companion commit 5be0f737 is an ancestor of the candidate.",
      "The current and superseded package manifests, private-alpha guide, acceptance ledger and supplemental review verdict on bb903669."
    ],
    "limitations": [
      "Zero guarded game starts were reported. No disposable save, gameplay smoke, UI observation or live restoration receipt was produced. The bounded fixture investigation found that save-backed admission requires the absent qualification suite.",
      "The foreground transcript was withheld. Executed build and gate claims above come from controller receipts and logs. The supplemental clearance applies to bb903669 under its no-live-use plan.",
      "The review shell\u2019s PowerShell language-mode boundary prevented importing the fixture for additional interactive mutations. The shipped 206-case suite ran directly. The new event-only counterexamples below are source traces.",
      "Historical broad Candidate failures involving the missing package and run directory retain their recorded dispositions."
    ],
    "required_gaps": [
      "Align the AC-miss driver and reader with the pinned engine\u2019s result classifications and actual shield AC delta.",
      "Validate successful negations at the hit event and rule completion, including critical cleanup.",
      "Update the active artifact records from the corrected package and provide its current focused receipt."
    ],
    "source_inspection_only": [
      "Static inspection of the pinned Kingmaker assembly, SHA-256 3b6450ffec440e296e586f71c711b195aed144b28d53e1cbb29406d18fef5afb, confirmed that RuleAttackRoll.OnTrigger calls ModifiableValueArmorClass.SelectMissReason for a failed numeric roll. That method can return Miss, DodgeAC, ArmorAC or ShieldAC.",
      "The remaining band-calculation and negation findings follow from the producer, observer, reader and supplied fixture paths. The runner\u2019s accepted snapshot and restoration branches were inspected alongside its recorded offline checks."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MS-11",
        "MS-15"
      ],
      "actual": "The new all-records branch checks the final result, final hit flag, damage-rule count and final critical flag. It omits the native event result, hit flag and critical flag. A second rider\u2019s natural defense can therefore report CriticalHit at the event and Parried at completion and pass these predicates. For a planned parry, changing only criticalAtEvent to true also passes the shared planned-success helper.",
      "blocking_rationale": "The reader can certify critical cleanup despite the observer recording a confirmed critical at the point other hit-event consumers see it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each credited negation agrees with its bound native attack at the hit event and at completion. Critical confirmation is clear at both points and the attack deals zero damage.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/runtime/Sprint6ShieldEvidence.ps1",
        "line": 308
      },
      "priority": "P2",
      "requested_correction": "Validate the recorded negated outcome, hit and critical flags at both observations for planned and natural defenses. Preserve legitimate melee Parried and ordinary Miss outcomes.",
      "requested_evidence": "Reject a natural second-pair success with a confirmed CriticalHit at the event followed by a clean final Parried. Reject a planned success with only criticalAtEvent changed. Retain passing controls for both valid negation outcomes.",
      "title": "A negated defense can still expose a critical hit at the hit event",
      "trigger": "A successful Shield-row defense has a clean final outcome after the hit-event observer saw a hit or retained critical confirmation."
    },
    {
      "acceptance_ids": [
        "MS-14"
      ],
      "actual": "ShieldBandMiss filters for the literal Miss, and Sprint6ShieldEvidence.ps1 line 432 requires that same literal at both observations. Kingmaker also returns ShieldAC, ArmorAC and DodgeAC from its ordinary failed-roll path. A roll totaling 22 against AC 24, which would hit AC 21 without Mounted Shield, is discarded if the engine labels it ShieldAC.",
      "blocking_rationale": "The producer can consume its bounded strike allowance while discarding qualifying observations. The reader also rejects valid evidence for the required AC-miss case.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The driver and reader recognize the native AC-miss classifications and use the measured AC margin to establish whether Mounted Shield prevented the hit.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs",
        "line": 967
      },
      "priority": "P2",
      "requested_correction": "Use the verified AC-miss result set in both producer and reader. Check the event and final observations, retain the attack identity and numeric margin checks, and require zero damage and no defense attempt.",
      "requested_evidence": "Show otherwise identical qualifying rows for Miss, ShieldAC, ArmorAC and DodgeAC. Keep negative controls for MirrorImage, concealment, a final hit and damage on the selected attack.",
      "title": "The miss scenario rejects Kingmaker\u2019s native shield, armor and dodge AC results",
      "trigger": "An ordinary failed attack inside the shield\u2019s AC margin receives a specific native AC-miss reason."
    },
    {
      "acceptance_ids": [
        "MS-09",
        "MS-10",
        "MS-14"
      ],
      "actual": "The production Brilliant Energy fix passes. The diagnostic setup at line 931 still counts every foreign Shield modifier as a competitor, so a force-stacking +4 makes it record a zero delta and skip the band even though KMC adds +3. The attack-time sample has the converse problem for Brilliant Energy. It receives only the target and can record an effective +3 on a melee attack whose native target AC is identical with and without the feat.",
      "blocking_rationale": "The diagnostic can skip a measurable bonus or attribute a miss to a bonus the attack ignored. R1-F2 explicitly required the diagnostic AC delta to agree with native stacking behavior.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The setup and bound attack observation agree on the AC change attributable to Mounted Shield. A force-stacking source leaves KMC\u2019s ordinary bonus effective. A Brilliant Energy attack ignores it.",
      "id": "R1-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Diagnostics/Sprint6ShieldScenario.cs",
        "line": 931
      },
      "priority": "P2",
      "requested_correction": "Use the verified non-stacking competitor calculation in both diagnostic paths. Carry the bound AC rule\u2019s shield-bypass state into the observation so a Brilliant Energy attack cannot qualify as a shield-caused miss.",
      "requested_evidence": "With KMC +3, show that a force-stacking +4 leaves a measurable band of three, an ordinary competing +4 leaves zero, and a Brilliant Energy melee attack contributes zero to the measured band. Retain the passing production AC controls.",
      "title": "The miss scenario still miscalculates Mounted Shield\u2019s effective AC change",
      "trigger": "The mount has a force-stacking Shield source during band setup, or the sampled melee attack ignores shield AC through Brilliant Energy."
    },
    {
      "acceptance_ids": [
        "MS-25",
        "MS-27"
      ],
      "actual": "AUTONOMOUS-RESUME.md pairs the 6fb53633 filename with fa5fb284, the digest prefix of the superseded bb903669 ZIP. PRIVATE-ALPHA.md line 28 and HANDOFF.md line 137 retain its 4,282,758-byte size. The current archive is 4,282,777 bytes with SHA-256 c3d80a58afa52d0506b001c2cfbddc776af1ade9b6796a5d5f3522c2f725b0bb.",
      "blocking_rationale": "OWNER AMENDMENT 2 requires an accurate handoff with exact artifact identities. Following the active records gives the owner conflicting verification values for the same filename.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Every active package reference identifies the same immutable ZIP by filename, digest and byte count.",
      "id": "R2-F2",
      "location": {
        "file": "AUTONOMOUS-RESUME.md",
        "line": 8
      },
      "priority": "P2",
      "requested_correction": "Populate the active resume record, handoff and private-alpha guide from the final archive and manifest. Keep earlier identities in their dated historical entries.",
      "requested_evidence": "Read back the final ZIP length and SHA-256 and compare them with each active owner-facing reference.",
      "title": "The active handoff carries the previous alpha\u2019s digest and size",
      "trigger": "The owner verifies the current alpha using the resume record or private-alpha package table."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "Both Shield scenarios refuse an invocation without BaseGameOnly before workflow admission. The admitted path selects the profile snapshot, Areas isolation and post-exit restoration. The gate records both real refusals and the offline Areas restoration checks."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F2",
      "rationale": "The game-facing correction is verified. The genuine force-stacking +4 control produces melee AC 10 with and without KMC, and the touch control passes. The requested diagnostic consistency is incomplete because the pre-strike calculation still treats that same +4 as a competitor."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "The focused check now invokes the actual v3 resolver. The resolver admits the packaged 0.2.16-alpha.1 under kmc-global-subscribers-r2 and rejects earlier releases and malformed versions in the recorded gate."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The producer records the acquired owner and pet. The reader binds the ordinary, disabled and reciprocal cross-pair snapshots to those actors and the measured pair. The duplicated-snapshot and missing-identity controls pass."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "A MirrorImage event can no longer satisfy the band check, and the reader rejects a final hit. R2-F1 covers the distinct regression introduced by restricting valid AC misses to a single enum value."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F6",
      "rationale": "The added tests reject a damaging natural negation and a retained final critical. The event-only critical and natural event-result contradictions remain unchecked."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "The gate invokes production EffectiveShield on genuine hand-slot, shield and modifier objects. It accepts the matching active shield, rejects the inactive slot, removed modifier and foreign sources, and accepts the restored matching modifier."
    }
  ],
  "request_id": "kmc-codex-mounted-shield-20261123114646-4433-r02-e65be0",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The private alpha packages one passive rider feat requiring Mounted Combat and Shield Focus. Its mount-AC contribution and existing Mounted Combat check use the same physical-shield base and focus amounts. The packaged version is 0.2.16-alpha.1 with UMM 0.31.1 as its declared minimum.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Playtest ordinary and fighter selection, same-level choices, Back/cancel, respec, icon presentation and character-sheet attribution.",
      "Observe shield amounts and enhancement exclusion, competing bonuses, reaction costs and boundaries in real-time and turn-based play, equipment and pair changes, save/load, cleanup and compatibility.",
      "Judge the artwork and private-alpha gameplay. Draft PR #61 remains under the owner\u2019s merge and release authority."
    ],
    "scope_reviewed": "Final private-alpha checkpoint under OWNER AMENDMENT 2. The packaged implementation is 6fb53633. Candidate 39c5d755 adds records only. The review covered all seven prior findings, their directly affected consumers and the owner handoff.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
