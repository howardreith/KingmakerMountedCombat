<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r10-review part=1/1 sha256=938c5cb79cf38addf1830a583fce5001d6b38e4a8f5e0975805f8f9cf5f100ff -->
### Codex GPT-6.2 Astra — review — round 10 — candidate `6e51224edd`

**Decision:** `changes_required` — The focused gate passed on the candidate. It confirms that the controller releases an unreachable target before first dispatch in the reported round-9 case. An installed wrapper can still start moving before that check runs, and an ineligible retained target can still veto a fresh proposal. The check also uses stale weapon reach and can mask an actual target invalidation. The queued invalidation output is correct, but its regression assertion still accepts an extra rejection code.

## Continuation for Claude

The operator ledger records round 10 as PR-1’s final allocated review. Any further formal round requires owner authorization. If work resumes, use the current rider action range and sight for the retained target wherever the automatic order can authorize movement, delivery, or refusal of a replacement proposal. Give the exact unreleased wrapper’s target invalidation precedence over either geometry-only retirement. Preserve native completion of released strikes, player orders, turn-based follow-through and ordinary advance toward reachable enemies.

Exercise the affected event orderings through the existing attack-path harness. Record wrapper and move submissions, first delivery, spending, and the exact terminal code list. Bind the required focused gate to the corrected candidate. Native play and packaging remain outside this checkpoint.

#### Findings

**R10-F1** · P2 · defect · blocks readiness — An installed automatic wrapper can approach an unreachable target before the controller rechecks it
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1803`; acceptance: MMR-04, S05
- Trigger: The rider’s automatic wrapper is in its native Standard slot for an in-range, perceived B marked unreachable. B moves outside action range before the wrapper’s next native approach tick, which runs before the controller drive.
- Expected: The unreleased wrapper ends before issuing or adopting movement toward B.
- Actual: AutomaticTargetLeftNativeAdmission is called only by TryRetireStockIntent. The installed wrapper checks group memory and pair-wide movement permission. Both still pass for this mobile pair. TickPreStartApproach can therefore call BeginPreStartMove and submit a mount move toward B before the controller retires it.
- Why it blocks / does not: The horse can receive a movement order toward an enemy the native brain currently excludes. Later cancellation cannot undo movement already taken.
- Requested correction: Make current target-specific admission available to the automatic wrapper at its pre-start, approach and first-delivery boundaries. Preserve exact carrier cleanup and native handling after delivery.
- Requested evidence: Install the automatic wrapper while B is legal, keep native start waiting, move B out of range, then tick the wrapper before the controller. Record zero new move submissions, displacement and spending. Retain the reachable-target control.

**R10-F2** · P2 · defect · blocks readiness — The new range retirement can mask an unconscious target
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1812`; acceptance: MMR-04, S05
- Trigger: A mobile pair’s remembered, marked-unreachable target becomes unconscious and moves outside native action range while its automatic wrapper is waiting.
- Expected: All three observers record TargetInvalid with the bounded unconscious-target reason.
- Actual: The new branch sets the reason to 'automatic target outside native admission'. Only HoldOutOfReach enters the TargetInvalidNow precedence check. The drive and queue promotion therefore retire the wrapper as Cancelled and produce CommandCancelled. The wrapper’s own tick checks target validity first and produces TargetInvalid.
- Why it blocks / does not: The same unconscious target produces different terminal feedback and rejection codes depending on which observer runs first.
- Requested correction: Apply the existing target-invalidation precedence to retirement for target-specific range or sight loss as well as failed stationary reach. Keep geometry-only retirement for a conscious, otherwise valid target.
- Requested evidence: Use a mobile pair, a remembered target in UnreachableUnits and an unreleased wrapper. Compare controller-first, wrapper-first and queued promotion after the target becomes unconscious and moves out of range. Assert identical cause, reason and code, with zero movement and spending.

**R10-F3** · P2 · defect · blocks readiness — Retained orders use proposal-time weapon reach
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1569`; acceptance: MMR-04, S05
- Trigger: The rider’s effective melee reach shortens while an automatic order waits, without a competing player command. B remains between the old and current reach limits.
- Expected: A retained order uses the proposal action’s range for the rider’s current weapon when deciding whether a restricted target is still eligible.
- Actual: The controller saves one numeric ActionRange when it accepts the proposal. Later checks refresh distance, body sizes and sight while continuing to use that saved range. If the ranking reader returned no candidates, the NaN guard skips the restriction entirely, including the sight check.
- Why it blocks / does not: Even with the controller running first, a target can pass on obsolete reach and receive an approach-enabled dispatch.
- Requested correction: Read the supported action’s current range when the target-specific restriction applies, or invalidate the saved value when its weapon or reach inputs change. Keep this check independent of whether the optional ranking list was available.
- Requested evidence: Select B inside the rider’s initial reach with both Standards cooling. Shorten the effective melee reach while B stays still, perceived and marked unreachable. Show release before dispatch or movement, then acceptance of reachable A. Cover an unavailable ranking with a restricted target.

**R10-F4** · P2 · defect · blocks readiness — An ineligible retained target still vetoes the next reachable proposal
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1459`; acceptance: MMR-04, S05
- Trigger: Both Standards are cooling and no wrapper exists. Marked-unreachable B leaves range but stays in memory. The native brain proposes reachable A before the controller’s next drive.
- Expected: The pair releases ineligible B before deciding whether its retained order can veto A.
- Actual: ObserveAutomaticProposal retires an old target at entry only for loss of perception. With B still remembered, FirstRefusal returns 'automatic-intent-holds-its-target' for A. The next drive can release B, but A’s proposal has already been discarded.
- Why it blocks / does not: Acquisition of a legal enemy depends on receiving another native proposal after the controller update.
- Requested correction: Check the retained automatic target’s current native admission before using its generation to refuse a new proposal. Keep the existing protection for a valid retained target and for player intent.
- Requested evidence: Reverse the final two events in the waiting-target case. After B leaves range, submit A before calling Drive. Observe that this same proposal is accepted and that B receives no wrapper, move or spend.

**R9-F3** · P2 · evidence_gap · blocks readiness — The invalidation regression still accepts extra rejection codes
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:2937`; acceptance: MMR-04, S05
- Trigger: All three terminal paths report the expected cause and reason followed by the same extra rejection code.
- Expected: Each invalidation path asserts that its complete rejection-code list contains exactly TargetInvalid.
- Actual: The queue handler is now connected and the recorded outputs are correct. The test compares the three strings to each other, then uses StartsWith for the expected value. A shared code list of TargetInvalid,CommandCancelled passes.
- Why it blocks / does not: The requested exact-code assertion remains incomplete. A contradictory second rejection code shared by all three paths would leave the gate green.
- Requested correction: Compare the full triple to the expected literal with ordinal equality, or assert a single typed TargetInvalid code on each path.
- Requested evidence: Retain the three observed TargetInvalid rows and their zero movement and spending. The assertion must reject an appended CommandCancelled code.

**R10-F5** · P3 · optional_polish · non-blocking — The tie-break comment contradicts the corrected dislike behavior
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1591`; acceptance: MMR-04
- Trigger: A maintainer reads the admission rule immediately above the candidate comparison.
- Expected: The comment describes dislike as a preference among admitted candidates.
- Actual: The comment says a disliked alternative never replaces the proposal. The corrected branch and the R9-F2 gate deliberately select that alternative when it is the only legal target.
- Why it blocks / does not: The discrepancy affects the explanation of the branch. The selection controls pass.
- Requested correction: Update the comment to match the selection rule demonstrated by the blocked-A and clear-A controls.
- Requested evidence: Source inspection is sufficient.

#### Prior finding assessments

- **R9-F1** — `fix_verified` — For the reported ordering, the drive returns before PlanStockDispatch. Gate line 1957 shows B selected while waiting, no live wrapper, release after B moves, zero moves and spends, then selection and delivery on A. R10-F1 and R10-F4 concern different callback orderings.
- **R9-F2** — `fix_verified` — Dislike is applied after the candidate eligibility check. Gate lines 1959–1962 show one stationary strike on disliked B when A’s command sight is blocked, and one stationary strike on A when its sight is clear.
- **R9-F3** — `fix_rejected` — The queued wrapper now calls ProductionTerminal. All three logged triples have the exact expected value. The remaining part of the requested fix is the assertion itself: its prefix comparison accepts additional rejection codes.
- **R9-F4** — `fix_verified` — The production reader evaluates info.Distance <= reach before info.InLos(context) in a short-circuit conjunction. An out-of-range candidate cannot invoke that sight query. This conclusion comes from source inspection.

#### Evidence assessment

- **Inspected:** Controller mission, request, developer report, transcript notice, prior findings, candidate-bound receipt and log, repository instructions, validation policy and handoff records.; Full feature diff and round-9-to-round-10 correction, including the ranking reader, tie-break, proposal admission, retained-order drive, queue promotion, native approach hook, wrapper lifecycle, terminal feedback and corresponding harness cases.
- **Executed verification:** Verified HEAD and merge base, matched all 8,596 lines of the supplied feature diff to local Git output, checked the mission hash, and ran git diff --check successfully.; Verified the controller receipt against the candidate and the gate log’s SHA-256, 9c64b1437c279f7464c1a5cff24f1e9a399f3d072f5918ab938257b43383a51e. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 exited 0 in 42.2 seconds. All 15 focused children passed. Components passed 1542/1542, source validation 153/153, lifecycle contracts 80/80 and the attack-path harness 169/169.; Gate lines 1948, 1950 and 1952 contain the exact expected invalidation triple with zero moves and spends. Lines 1957–1962 record release of B before first dispatch, a subsequent strike on A, and both dislike controls. Line 2569 confirms the packet, production and embedded Shield icon bytes matched.; Checked the harness’s ordinal prefix comparison independently. Appending ,CommandCancelled to the expected triple still satisfies it.
- **Source inspection only:** Traced the new failure paths in R10-F1 through R10-F4. The supplied gate does not execute those counterexamples.; Confirmed the R9-F4 sight-query short-circuit from production source. The harness substitutes the ranking reader and does not measure its sight-call count.
- **Required gaps:** Cover target admission loss after wrapper installation and before a fresh proposal, including the ordering where the native callback runs first.; Cover current weapon reach changing during a retained order and loss of the optional ranking data used to capture range.; Cover simultaneous target invalidation and target-specific range loss through the drive, wrapper and queued promotion. Assert the complete expected rejection-code list.
- **Limitations:** The review was read-only. The controller receipt establishes the executed gate. The foreground developer transcript was withheld, so the separately reported direct runs were not independently confirmed.; The attack-path harness uses production members over simulated engine inputs and reports nativeProof=false. Native horse attack attribution, combat acquisition and movement latency remain owner-playtest observations.

#### Summary

- **Delivered behavior:** The offline gate covers ordinary paired horse follow-through, movement while strike readiness is pending, stationary target selection and Mounted Shield regressions. The candidate corrects the reported waiting-target and dislike cases. Real-time automatic target eligibility still depends on callback order and cached reach.
- **Scope reviewed:** Final PR-1 source/offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, S08, the four pending findings and directly affected consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize further work after the recorded PR-1 review ceiling.; N04 — Observe horse-attributed native attacks in both initiative arrangements and confirm an already-spent horse stays spent.; N05 — Observe live automatic acquisition and approach with two eligible targets, including AI-off, Stop and Hold controls.; N06 — Compare input, first movement and first attack against an equivalent unmounted actor during a readiness wait.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a218bf-7145-7ec3-a502-ee4c191db27a`, duration 876.6s, response sha256 `47bafe3c7b2f1f5751453687bfb293e5f998d019e36fc2838335185b614124d6`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r10-28cc50`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "6e51224edd3ab3475f79b692d8c5f2ce973f7452",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the candidate. It confirms that the controller releases an unreachable target before first dispatch in the reported round-9 case. An installed wrapper can still start moving before that check runs, and an ineligible retained target can still veto a fresh proposal. The check also uses stale weapon reach and can mask an actual target invalidation. The queued invalidation output is correct, but its regression assertion still accepts an extra rejection code.\n\n## Continuation for Claude\n\nThe operator ledger records round 10 as PR-1\u2019s final allocated review. Any further formal round requires owner authorization. If work resumes, use the current rider action range and sight for the retained target wherever the automatic order can authorize movement, delivery, or refusal of a replacement proposal. Give the exact unreleased wrapper\u2019s target invalidation precedence over either geometry-only retirement. Preserve native completion of released strikes, player orders, turn-based follow-through and ordinary advance toward reachable enemies.\n\nExercise the affected event orderings through the existing attack-path harness. Record wrapper and move submissions, first delivery, spending, and the exact terminal code list. Bind the required focused gate to the corrected candidate. Native play and packaging remain outside this checkpoint.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified HEAD and merge base, matched all 8,596 lines of the supplied feature diff to local Git output, checked the mission hash, and ran git diff --check successfully.",
      "Verified the controller receipt against the candidate and the gate log\u2019s SHA-256, 9c64b1437c279f7464c1a5cff24f1e9a399f3d072f5918ab938257b43383a51e. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 exited 0 in 42.2 seconds. All 15 focused children passed. Components passed 1542/1542, source validation 153/153, lifecycle contracts 80/80 and the attack-path harness 169/169.",
      "Gate lines 1948, 1950 and 1952 contain the exact expected invalidation triple with zero moves and spends. Lines 1957\u20131962 record release of B before first dispatch, a subsequent strike on A, and both dislike controls. Line 2569 confirms the packet, production and embedded Shield icon bytes matched.",
      "Checked the harness\u2019s ordinal prefix comparison independently. Appending ,CommandCancelled to the expected triple still satisfies it."
    ],
    "inspected": [
      "Controller mission, request, developer report, transcript notice, prior findings, candidate-bound receipt and log, repository instructions, validation policy and handoff records.",
      "Full feature diff and round-9-to-round-10 correction, including the ranking reader, tie-break, proposal admission, retained-order drive, queue promotion, native approach hook, wrapper lifecycle, terminal feedback and corresponding harness cases."
    ],
    "limitations": [
      "The review was read-only. The controller receipt establishes the executed gate. The foreground developer transcript was withheld, so the separately reported direct runs were not independently confirmed.",
      "The attack-path harness uses production members over simulated engine inputs and reports nativeProof=false. Native horse attack attribution, combat acquisition and movement latency remain owner-playtest observations."
    ],
    "required_gaps": [
      "Cover target admission loss after wrapper installation and before a fresh proposal, including the ordering where the native callback runs first.",
      "Cover current weapon reach changing during a retained order and loss of the optional ranking data used to capture range.",
      "Cover simultaneous target invalidation and target-specific range loss through the drive, wrapper and queued promotion. Assert the complete expected rejection-code list."
    ],
    "source_inspection_only": [
      "Traced the new failure paths in R10-F1 through R10-F4. The supplied gate does not execute those counterexamples.",
      "Confirmed the R9-F4 sight-query short-circuit from production source. The harness substitutes the ranking reader and does not measure its sight-call count."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "AutomaticTargetLeftNativeAdmission is called only by TryRetireStockIntent. The installed wrapper checks group memory and pair-wide movement permission. Both still pass for this mobile pair. TickPreStartApproach can therefore call BeginPreStartMove and submit a mount move toward B before the controller retires it.",
      "blocking_rationale": "The horse can receive a movement order toward an enemy the native brain currently excludes. Later cancellation cannot undo movement already taken.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The unreleased wrapper ends before issuing or adopting movement toward B.",
      "id": "R10-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1803
      },
      "priority": "P2",
      "requested_correction": "Make current target-specific admission available to the automatic wrapper at its pre-start, approach and first-delivery boundaries. Preserve exact carrier cleanup and native handling after delivery.",
      "requested_evidence": "Install the automatic wrapper while B is legal, keep native start waiting, move B out of range, then tick the wrapper before the controller. Record zero new move submissions, displacement and spending. Retain the reachable-target control.",
      "title": "An installed automatic wrapper can approach an unreachable target before the controller rechecks it",
      "trigger": "The rider\u2019s automatic wrapper is in its native Standard slot for an in-range, perceived B marked unreachable. B moves outside action range before the wrapper\u2019s next native approach tick, which runs before the controller drive."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The new branch sets the reason to 'automatic target outside native admission'. Only HoldOutOfReach enters the TargetInvalidNow precedence check. The drive and queue promotion therefore retire the wrapper as Cancelled and produce CommandCancelled. The wrapper\u2019s own tick checks target validity first and produces TargetInvalid.",
      "blocking_rationale": "The same unconscious target produces different terminal feedback and rejection codes depending on which observer runs first.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "All three observers record TargetInvalid with the bounded unconscious-target reason.",
      "id": "R10-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1812
      },
      "priority": "P2",
      "requested_correction": "Apply the existing target-invalidation precedence to retirement for target-specific range or sight loss as well as failed stationary reach. Keep geometry-only retirement for a conscious, otherwise valid target.",
      "requested_evidence": "Use a mobile pair, a remembered target in UnreachableUnits and an unreleased wrapper. Compare controller-first, wrapper-first and queued promotion after the target becomes unconscious and moves out of range. Assert identical cause, reason and code, with zero movement and spending.",
      "title": "The new range retirement can mask an unconscious target",
      "trigger": "A mobile pair\u2019s remembered, marked-unreachable target becomes unconscious and moves outside native action range while its automatic wrapper is waiting."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The controller saves one numeric ActionRange when it accepts the proposal. Later checks refresh distance, body sizes and sight while continuing to use that saved range. If the ranking reader returned no candidates, the NaN guard skips the restriction entirely, including the sight check.",
      "blocking_rationale": "Even with the controller running first, a target can pass on obsolete reach and receive an approach-enabled dispatch.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A retained order uses the proposal action\u2019s range for the rider\u2019s current weapon when deciding whether a restricted target is still eligible.",
      "id": "R10-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1569
      },
      "priority": "P2",
      "requested_correction": "Read the supported action\u2019s current range when the target-specific restriction applies, or invalidate the saved value when its weapon or reach inputs change. Keep this check independent of whether the optional ranking list was available.",
      "requested_evidence": "Select B inside the rider\u2019s initial reach with both Standards cooling. Shorten the effective melee reach while B stays still, perceived and marked unreachable. Show release before dispatch or movement, then acceptance of reachable A. Cover an unavailable ranking with a restricted target.",
      "title": "Retained orders use proposal-time weapon reach",
      "trigger": "The rider\u2019s effective melee reach shortens while an automatic order waits, without a competing player command. B remains between the old and current reach limits."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ObserveAutomaticProposal retires an old target at entry only for loss of perception. With B still remembered, FirstRefusal returns 'automatic-intent-holds-its-target' for A. The next drive can release B, but A\u2019s proposal has already been discarded.",
      "blocking_rationale": "Acquisition of a legal enemy depends on receiving another native proposal after the controller update.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The pair releases ineligible B before deciding whether its retained order can veto A.",
      "id": "R10-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1459
      },
      "priority": "P2",
      "requested_correction": "Check the retained automatic target\u2019s current native admission before using its generation to refuse a new proposal. Keep the existing protection for a valid retained target and for player intent.",
      "requested_evidence": "Reverse the final two events in the waiting-target case. After B leaves range, submit A before calling Drive. Observe that this same proposal is accepted and that B receives no wrapper, move or spend.",
      "title": "An ineligible retained target still vetoes the next reachable proposal",
      "trigger": "Both Standards are cooling and no wrapper exists. Marked-unreachable B leaves range but stays in memory. The native brain proposes reachable A before the controller\u2019s next drive."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The queue handler is now connected and the recorded outputs are correct. The test compares the three strings to each other, then uses StartsWith for the expected value. A shared code list of TargetInvalid,CommandCancelled passes.",
      "blocking_rationale": "The requested exact-code assertion remains incomplete. A contradictory second rejection code shared by all three paths would leave the gate green.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each invalidation path asserts that its complete rejection-code list contains exactly TargetInvalid.",
      "id": "R9-F3",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 2937
      },
      "priority": "P2",
      "requested_correction": "Compare the full triple to the expected literal with ordinal equality, or assert a single typed TargetInvalid code on each path.",
      "requested_evidence": "Retain the three observed TargetInvalid rows and their zero movement and spending. The assertion must reject an appended CommandCancelled code.",
      "title": "The invalidation regression still accepts extra rejection codes",
      "trigger": "All three terminal paths report the expected cause and reason followed by the same extra rejection code."
    },
    {
      "acceptance_ids": [
        "MMR-04"
      ],
      "actual": "The comment says a disliked alternative never replaces the proposal. The corrected branch and the R9-F2 gate deliberately select that alternative when it is the only legal target.",
      "blocking_rationale": "The discrepancy affects the explanation of the branch. The selection controls pass.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The comment describes dislike as a preference among admitted candidates.",
      "id": "R10-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1591
      },
      "priority": "P3",
      "requested_correction": "Update the comment to match the selection rule demonstrated by the blocked-A and clear-A controls.",
      "requested_evidence": "Source inspection is sufficient.",
      "title": "The tie-break comment contradicts the corrected dislike behavior",
      "trigger": "A maintainer reads the admission rule immediately above the candidate comparison."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F1",
      "rationale": "For the reported ordering, the drive returns before PlanStockDispatch. Gate line 1957 shows B selected while waiting, no live wrapper, release after B moves, zero moves and spends, then selection and delivery on A. R10-F1 and R10-F4 concern different callback orderings."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F2",
      "rationale": "Dislike is applied after the candidate eligibility check. Gate lines 1959\u20131962 show one stationary strike on disliked B when A\u2019s command sight is blocked, and one stationary strike on A when its sight is clear."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F3",
      "rationale": "The queued wrapper now calls ProductionTerminal. All three logged triples have the exact expected value. The remaining part of the requested fix is the assertion itself: its prefix comparison accepts additional rejection codes."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F4",
      "rationale": "The production reader evaluates info.Distance <= reach before info.InLos(context) in a short-circuit conjunction. An out-of-range candidate cannot invoke that sight query. This conclusion comes from source inspection."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r10-28cc50",
  "round": 10,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The offline gate covers ordinary paired horse follow-through, movement while strike readiness is pending, stationary target selection and Mounted Shield regressions. The candidate corrects the reported waiting-target and dislike cases. Real-time automatic target eligibility still depends on callback order and cached reach.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize further work after the recorded PR-1 review ceiling.",
      "N04 \u2014 Observe horse-attributed native attacks in both initiative arrangements and confirm an already-spent horse stays spent.",
      "N05 \u2014 Observe live automatic acquisition and approach with two eligible targets, including AI-off, Stop and Hold controls.",
      "N06 \u2014 Compare input, first movement and first attack against an equivalent unmounted actor during a readiness wait."
    ],
    "scope_reviewed": "Final PR-1 source/offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, S08, the four pending findings and directly affected consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
