<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r08-review part=1/1 sha256=5f479a9a2a051fb3944ca7a58b67ef6a2a1d042301bea686417680924a622ab8 -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `b157556882`

**Decision:** `changes_required` — The required focused gate passed at the candidate. Two source defects still block S05. Automatic selection can replace the rider brain’s reachable target with an unreachable one. A travel restriction can also cause an invalid target to be reported as an ordinary cancellation.

## Continuation for Claude

For R8-F1, apply the native action-range and sight requirements before an alternative can replace the brain’s proposal. For R8-F2, give an unreleased wrapper’s target invalidation precedence over retirement for failed stationary geometry, including at queued promotion. Keep both corrections within the existing rider-brain and exact-wrapper paths. Preserve manual selection, valid stationary strikes, player overrides and native completion of released attacks.

The requested evidence is a production-path comparison of the two equal-ranked targets and an observer-order comparison for a still-remembered invalid target. Use the existing focused gate on any authorized successor. Round 8 reaches the recorded PR-1 review ceiling. Further formal review requires owner direction.

#### Findings

**R8-F1** · P2 · defect · blocks readiness — An unreachable enemy can replace the brain’s reachable proposal
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1573`; acceptance: MMR-04, S05
- Trigger: A mobile real-time melee pair has two equally distant remembered enemies outside attack range. Native proposes reachable A, its DislikedTarget. B is in UnreachableUnits and has cached line of sight.
- Expected: A remains the target. Native scoring discounts a disliked target but still permits it. A target marked unreachable must already be in native action range and in sight.
- Actual: ResolveEqualRankTie marks A ineligible because it is disliked. It accepts B on cached sight alone because pair travel is unrestricted. Both targets have the same distance-derived negative weight. Choose gives the ineligible proposal an infinite distance to beat and selects B. Final proposal admission permits approach to B.
- Why it blocks / does not: An idle pair can pursue a target the native brain has already marked unreachable while a reachable hostile is available.
- Requested correction: Apply native action-range admission together with cached sight wherever the native action requires both. Preserve the valid in-range unreachable case and the stationary mounted-strike check.
- Requested evidence: Drive ObserveAutomaticProposal with the two equal-distance, out-of-range targets. Record that A owns the order and B receives no dispatch or carrier. Retain an in-range, in-sight control and the blocked stationary-proposal cases.

**R8-F2** · P2 · defect · blocks readiness — Restricted travel can mask invalidation of a remembered target
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1932`; acceptance: MMR-04, S05
- Trigger: An automatic stationary attack is waiting to deliver. Its target remains in group memory but becomes unconscious and is displaced outside strike range while the mount cannot move.
- Expected: Either observer retires the unreleased wrapper with TargetInvalid, the reason that the target became unconscious, and zero further movement or spending.
- Actual: Group memory still contains the target, so the perception check returns. The drive then calls TryRetireStockIntent before validating the target. Failed stationary geometry retires the wrapper for 'hold out of reach' with Cancelled and CommandCancelled. The wrapper’s own tick checks target validity first and records TargetInvalid. Queued promotion also calls the geometry-first retirement path.
- Why it blocks / does not: The player-facing rejection and recorded terminal cause depend on update order for the same invalid target.
- Requested correction: Check the exact unreleased wrapper’s target validity before retiring it solely for failed stationary geometry. Apply that ordering to the drive and queued promotion. Keep the existing retirement for a valid target that moves out of reach.
- Requested evidence: Keep the target in group memory and compare controller-first and wrapper-first observation after it becomes unconscious outside stationary reach. Assert cause, bounded reason, rejection code, movement and spending. Exercise the same condition at queued promotion and retain a valid out-of-reach control.

**R8-F3** · P3 · optional_polish · non-blocking — The harness receipt omits the terminal-hook source file
- Location: `scripts/Test-MountedAlpha2AttackPathHarness.ps1:117`; acceptance: S04
- Trigger: The harness writes its input inventory after compiling the production terminal hooks used by the rider-refusal cases.
- Expected: The receipt lists the path and hash of each production source file used to assemble the harness.
- Actual: The script reads NativeCommandTerminalBranches.cs and extracts four members from it. The receipt’s inputs array omits $terminalBranchesPath.
- Why it blocks / does not: The candidate commit and generated-source hash still identify the executed code. The omission affects the standalone receipt’s per-file provenance.
- Requested correction: Include $terminalBranchesPath in the existing receipt input list.
- Requested evidence: When the harness next runs, its receipt should contain the source hash for NativeCommandTerminalBranches.cs.

#### Prior finding assessments

- **R1-F11** — `fix_verified` — The patch observes the click handler’s sole ManualTarget setter and reports the combat state actually written. The recorded harness clears the old assignment after an unrelated non-writing Run and preserves the rider’s assignment when the cannot-attack router consumes the rider submission. Genuine same-target, skipped, simulated and unhandled click controls passed.
- **R4-F1** — `fix_verified` — The harness forwards ordinary versus single-attack mode, supplies the modeled natural weapon and slot, and executes production plan validation and OnEnded. The ordinary horse routine completed 2/2 on A with one Standard spent. Valid B received one dispatch and invalid B received zero. Dismount stopped the routine after one delivery, and the explicit single control completed 1/1.
- **R4-F5** — `fix_verified` — The harness supplies the CanAct observation through the production force-finish and running-interrupt hooks, then uses production actor evaluation. Across the full observation window, both branches recorded zero rider resubmissions. An eligible horse received one submission and delivered two modeled attacks with zero warnings. Spent and ended horses received zero submissions and produced one rider refusal.
- **R6-F1** — `fix_verified` — For the reported despawn with simultaneous removal from group memory, the controller now asks the wrapper whether its target is invalid before cancelling for perception loss. Gate rows 1980–1988 record matching TargetInvalid cause, despawn reason and rejection code, with zero movement and spending in both observation orders. R8-F2 concerns a target that stays in memory and reaches a different retirement guard.
- **R7-F1** — `fix_verified` — A blocked native proposal no longer sets the maximum candidate distance. The recorded restricted-travel case selects and strikes the farther admissible target. With both candidates blocked, the proposal is refused and neither receives a delivery.
- **R7-F2** — `fix_verified` — The selector now requires cached rider sight for a held rider as well as stationary mounted-strike admission. In the recorded in-range cases, the farther target was struck when only it passed cached sight. The nearer target was struck with both clear and in the mount-only restriction control. R8-F1 concerns the separate native action-range requirement.
- **R7-F3** — `deferral_accepted` — Restricted proposals still construct strike probes for candidates of unrelated ranks. The extra work is documented, and the record establishes neither a selection error nor a measured frame-time impact from it.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoffs, full feature diff, round-8 correction diff, seven pending findings, developer report, transcript notice, controller receipt and gate log.; The rider-brain ranking reader and selector, stock-order drive and retirement paths, click assignment patch, native terminal hooks, horse plan validation and the harness members exercised by the gate.
- **Executed verification:** The controller receipt identifies b1575568827096453af5697f7f94b9f3d9ad814d and records scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 exiting 0 in 37.9 seconds with an unchanged worktree. The supplied log matches the receipt’s SHA-256.; The log records Fast 4/4, focused children 15/15, component checks 1542/0, installed attack-order contract checks 4/0, lifecycle checks 78/0 and attack-path harness checks 159/0. The Mounted Shield checks include icon identity in the built output. Both UMM binding profiles passed.; Read-only git diff --check passed for the feature diff with review transcripts excluded.
- **Source inspection only:** R8-F1 follows the ranked candidates through native action validity, ResolveEqualRankTie, NativeAttackTieBreak.Choose and final proposal admission.; R8-F2 follows the same target state through TryDriveStockAttackIntent, TryRetireStockIntent, queued promotion and the wrapper’s own invalidation check. Neither new trigger appears in the gate log.
- **Required gaps:** S05 needs a passing selection case in which an equal-ranked, visible but out-of-range unreachable enemy competes with the native reachable proposal.; S05 needs matching terminal cause, reason and rejection code when an unreleased target stays remembered but loses both eligibility and stationary strike admission.
- **Limitations:** The harness runs production members against a modeled engine. The installed-contract checks inspect bindings and IL. The gate records nativeProof=false.; The controller withheld the mixed-session developer transcript. The candidate-bound receipt and log confirm the required gate. The additional local runs listed in the developer report could not be independently confirmed.

#### Summary

- **Delivered behavior:** The candidate ties player-selection ownership to the actual click assignment and exercises ordinary horse routine completion, rider refusal and eligible horse follow-through through production paths in the offline harness. The remaining required corrections concern real-time automatic target selection and invalidation reporting.
- **Scope reviewed:** Final PR-1 source and offline acceptance for MMR-03, MMR-04 and MMR-05, including S04, S05, the S08 Mounted Shield regression checks and all seven pending prior findings.
- **Pending later-phase work:** none
- **Unresolved questions:** The recorded PR-1 allowance ends at round 8. The owner must decide how to proceed with the two blocking defects.
- **Remaining owner review:** N04. Observe horse-attributed native attacks by actor, target and weapon in both initiative arrangements, with a used or ended horse as the control.; N05. Observe idle real-time acquisition and approach with two reachable hostiles, including AI-off, Stop/Hold and manual-target priority.; N06. Compare mounted and unmounted movement onset at comparable attack readiness and confirm that the first attack respects native timing.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a2185c-5e3f-78b3-9953-d9b14030d9cf`, duration 1281.4s, response sha256 `50eaf0891921d08e77fb5676e1f4179d7e7c191d5c0004f1e6964dde6fe76c68`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r08-4551bb`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "b1575568827096453af5697f7f94b9f3d9ad814d",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The required focused gate passed at the candidate. Two source defects still block S05. Automatic selection can replace the rider brain\u2019s reachable target with an unreachable one. A travel restriction can also cause an invalid target to be reported as an ordinary cancellation.\n\n## Continuation for Claude\n\nFor R8-F1, apply the native action-range and sight requirements before an alternative can replace the brain\u2019s proposal. For R8-F2, give an unreleased wrapper\u2019s target invalidation precedence over retirement for failed stationary geometry, including at queued promotion. Keep both corrections within the existing rider-brain and exact-wrapper paths. Preserve manual selection, valid stationary strikes, player overrides and native completion of released attacks.\n\nThe requested evidence is a production-path comparison of the two equal-ranked targets and an observer-order comparison for a still-remembered invalid target. Use the existing focused gate on any authorized successor. Round 8 reaches the recorded PR-1 review ceiling. Further formal review requires owner direction.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt identifies b1575568827096453af5697f7f94b9f3d9ad814d and records scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 exiting 0 in 37.9 seconds with an unchanged worktree. The supplied log matches the receipt\u2019s SHA-256.",
      "The log records Fast 4/4, focused children 15/15, component checks 1542/0, installed attack-order contract checks 4/0, lifecycle checks 78/0 and attack-path harness checks 159/0. The Mounted Shield checks include icon identity in the built output. Both UMM binding profiles passed.",
      "Read-only git diff --check passed for the feature diff with review transcripts excluded."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoffs, full feature diff, round-8 correction diff, seven pending findings, developer report, transcript notice, controller receipt and gate log.",
      "The rider-brain ranking reader and selector, stock-order drive and retirement paths, click assignment patch, native terminal hooks, horse plan validation and the harness members exercised by the gate."
    ],
    "limitations": [
      "The harness runs production members against a modeled engine. The installed-contract checks inspect bindings and IL. The gate records nativeProof=false.",
      "The controller withheld the mixed-session developer transcript. The candidate-bound receipt and log confirm the required gate. The additional local runs listed in the developer report could not be independently confirmed."
    ],
    "required_gaps": [
      "S05 needs a passing selection case in which an equal-ranked, visible but out-of-range unreachable enemy competes with the native reachable proposal.",
      "S05 needs matching terminal cause, reason and rejection code when an unreleased target stays remembered but loses both eligibility and stationary strike admission."
    ],
    "source_inspection_only": [
      "R8-F1 follows the ranked candidates through native action validity, ResolveEqualRankTie, NativeAttackTieBreak.Choose and final proposal admission.",
      "R8-F2 follows the same target state through TryDriveStockAttackIntent, TryRetireStockIntent, queued promotion and the wrapper\u2019s own invalidation check. Neither new trigger appears in the gate log."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ResolveEqualRankTie marks A ineligible because it is disliked. It accepts B on cached sight alone because pair travel is unrestricted. Both targets have the same distance-derived negative weight. Choose gives the ineligible proposal an infinite distance to beat and selects B. Final proposal admission permits approach to B.",
      "blocking_rationale": "An idle pair can pursue a target the native brain has already marked unreachable while a reachable hostile is available.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A remains the target. Native scoring discounts a disliked target but still permits it. A target marked unreachable must already be in native action range and in sight.",
      "id": "R8-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1573
      },
      "priority": "P2",
      "requested_correction": "Apply native action-range admission together with cached sight wherever the native action requires both. Preserve the valid in-range unreachable case and the stationary mounted-strike check.",
      "requested_evidence": "Drive ObserveAutomaticProposal with the two equal-distance, out-of-range targets. Record that A owns the order and B receives no dispatch or carrier. Retain an in-range, in-sight control and the blocked stationary-proposal cases.",
      "title": "An unreachable enemy can replace the brain\u2019s reachable proposal",
      "trigger": "A mobile real-time melee pair has two equally distant remembered enemies outside attack range. Native proposes reachable A, its DislikedTarget. B is in UnreachableUnits and has cached line of sight."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "Group memory still contains the target, so the perception check returns. The drive then calls TryRetireStockIntent before validating the target. Failed stationary geometry retires the wrapper for 'hold out of reach' with Cancelled and CommandCancelled. The wrapper\u2019s own tick checks target validity first and records TargetInvalid. Queued promotion also calls the geometry-first retirement path.",
      "blocking_rationale": "The player-facing rejection and recorded terminal cause depend on update order for the same invalid target.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Either observer retires the unreleased wrapper with TargetInvalid, the reason that the target became unconscious, and zero further movement or spending.",
      "id": "R8-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1932
      },
      "priority": "P2",
      "requested_correction": "Check the exact unreleased wrapper\u2019s target validity before retiring it solely for failed stationary geometry. Apply that ordering to the drive and queued promotion. Keep the existing retirement for a valid target that moves out of reach.",
      "requested_evidence": "Keep the target in group memory and compare controller-first and wrapper-first observation after it becomes unconscious outside stationary reach. Assert cause, bounded reason, rejection code, movement and spending. Exercise the same condition at queued promotion and retain a valid out-of-reach control.",
      "title": "Restricted travel can mask invalidation of a remembered target",
      "trigger": "An automatic stationary attack is waiting to deliver. Its target remains in group memory but becomes unconscious and is displaced outside strike range while the mount cannot move."
    },
    {
      "acceptance_ids": [
        "S04"
      ],
      "actual": "The script reads NativeCommandTerminalBranches.cs and extracts four members from it. The receipt\u2019s inputs array omits $terminalBranchesPath.",
      "blocking_rationale": "The candidate commit and generated-source hash still identify the executed code. The omission affects the standalone receipt\u2019s per-file provenance.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The receipt lists the path and hash of each production source file used to assemble the harness.",
      "id": "R8-F3",
      "location": {
        "file": "scripts/Test-MountedAlpha2AttackPathHarness.ps1",
        "line": 117
      },
      "priority": "P3",
      "requested_correction": "Include $terminalBranchesPath in the existing receipt input list.",
      "requested_evidence": "When the harness next runs, its receipt should contain the source hash for NativeCommandTerminalBranches.cs.",
      "title": "The harness receipt omits the terminal-hook source file",
      "trigger": "The harness writes its input inventory after compiling the production terminal hooks used by the rider-refusal cases."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F11",
      "rationale": "The patch observes the click handler\u2019s sole ManualTarget setter and reports the combat state actually written. The recorded harness clears the old assignment after an unrelated non-writing Run and preserves the rider\u2019s assignment when the cannot-attack router consumes the rider submission. Genuine same-target, skipped, simulated and unhandled click controls passed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "The harness forwards ordinary versus single-attack mode, supplies the modeled natural weapon and slot, and executes production plan validation and OnEnded. The ordinary horse routine completed 2/2 on A with one Standard spent. Valid B received one dispatch and invalid B received zero. Dismount stopped the routine after one delivery, and the explicit single control completed 1/1."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F5",
      "rationale": "The harness supplies the CanAct observation through the production force-finish and running-interrupt hooks, then uses production actor evaluation. Across the full observation window, both branches recorded zero rider resubmissions. An eligible horse received one submission and delivered two modeled attacks with zero warnings. Spent and ended horses received zero submissions and produced one rider refusal."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F1",
      "rationale": "For the reported despawn with simultaneous removal from group memory, the controller now asks the wrapper whether its target is invalid before cancelling for perception loss. Gate rows 1980\u20131988 record matching TargetInvalid cause, despawn reason and rejection code, with zero movement and spending in both observation orders. R8-F2 concerns a target that stays in memory and reaches a different retirement guard."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F1",
      "rationale": "A blocked native proposal no longer sets the maximum candidate distance. The recorded restricted-travel case selects and strikes the farther admissible target. With both candidates blocked, the proposal is refused and neither receives a delivery."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F2",
      "rationale": "The selector now requires cached rider sight for a held rider as well as stationary mounted-strike admission. In the recorded in-range cases, the farther target was struck when only it passed cached sight. The nearer target was struck with both clear and in the mount-only restriction control. R8-F1 concerns the separate native action-range requirement."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R7-F3",
      "rationale": "Restricted proposals still construct strike probes for candidates of unrelated ranks. The extra work is documented, and the record establishes neither a selection error nor a measured frame-time impact from it."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r08-4551bb",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate ties player-selection ownership to the actual click assignment and exercises ordinary horse routine completion, rider refusal and eligible horse follow-through through production paths in the offline harness. The remaining required corrections concern real-time automatic target selection and invalidation reporting.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04. Observe horse-attributed native attacks by actor, target and weapon in both initiative arrangements, with a used or ended horse as the control.",
      "N05. Observe idle real-time acquisition and approach with two reachable hostiles, including AI-off, Stop/Hold and manual-target priority.",
      "N06. Compare mounted and unmounted movement onset at comparable attack readiness and confirm that the first attack respects native timing."
    ],
    "scope_reviewed": "Final PR-1 source and offline acceptance for MMR-03, MMR-04 and MMR-05, including S04, S05, the S08 Mounted Shield regression checks and all seven pending prior findings.",
    "unresolved_questions": [
      "The recorded PR-1 allowance ends at round 8. The owner must decide how to proceed with the two blocking defects."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
