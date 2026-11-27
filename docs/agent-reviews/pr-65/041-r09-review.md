<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r09-review part=1/1 sha256=36fba8257b5f876499e7ee6f769f09277f8f1123d1428db4886d6f8945bd032c -->
### Codex GPT-6.2 Astra — review — round 9 — candidate `f2f2fa554a`

**Decision:** `changes_required` — The three round-8 corrections are present and the focused gate passed. Two automatic melee cases still violate MMR-04. A retained order can pursue a marked-unreachable target after it leaves range, and a held pair can reject its only legal stationary target. The queued invalidation test also bypasses the controller that reports the rejection code.

## Continuation for Claude

Keep the correction in PR-1’s existing automatic-order and test paths. Recheck the selected target’s reachability restriction when a waiting order resumes. Apply the native dislike preference among targets the pair can actually strike. Preserve ordinary advance toward reachable targets, manual and ranged target priority, and native completion of released attacks. Connect the queued test wrapper to the production terminal handler and assert its exact rejection code. Exercise the transitions in R9-F1 through R9-F3 through the existing harness and retain the metered focused gate receipt for the corrected candidate. R9-F4 is optional.

#### Findings

**R9-F1** · P2 · defect · blocks readiness — A waiting automatic order can chase an unreachable target after it leaves range
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1979`; acceptance: MMR-04, S05
- Trigger: A mobile pair selects B while B is in native action range and sight despite its UnreachableUnits entry. Both Standards are cooling. Before the first dispatch, B moves outside range while remaining in group memory and UnreachableUnits.
- Expected: Once B leaves its native action range while still marked unreachable, the automatic order releases B before dispatch or movement. A fresh reachable proposal can then proceed.
- Actual: ResolveEqualRankTie checks B’s native range and sight at proposal time. On the later drive, target validity covers life, hostility and attackability, while stationary depends only on pair-wide movement restrictions. PlanStockDispatch can therefore issue an approach-enabled wrapper for B after B leaves range.
- Why it blocks / does not: An automatic order can direct the horse toward an enemy the native brain now excludes and keep a reachable proposal from taking over.
- Requested correction: Recheck the target-specific native restriction before a retained automatic order dispatches or starts an approach. Preserve the stationary strike on an in-range, in-sight unreachable target and normal travel toward a reachable target.
- Requested evidence: Start with the round-9 in-range control and both Standards cooling. After B is selected, move it outside range while keeping it perceived and marked unreachable. Resume the drive and observe zero wrappers, carriers or spending for B, followed by acceptance of reachable A.

**R9-F2** · P2 · defect · blocks readiness — Dislike excludes the only legal stationary alternative
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1576`; acceptance: MMR-04, S05
- Trigger: A held pair has two perceived enemies in native range with equal TargetInfo weights and clear cached sight. Native proposes non-disliked A. Mounted command sight is blocked for A and clear for disliked B. There is no manual target or existing order.
- Expected: The pair selects B and strikes without travel. The installed NotImpatient consideration gives B a positive score of 0.5.
- Actual: ResolveEqualRankTie sets B’s Eligible flag to false solely because B is disliked and was not the proposal. With A also ineligible, Choose returns A and ObserveAutomaticProposal refuses it for failed stationary geometry.
- Why it blocks / does not: The pair can remain idle across repeated proposals with a conscious hostile it can legally strike from its current position.
- Requested correction: Apply the dislike preference after determining which comparable candidates pass native and mounted admission. Preserve the native preference when a legal non-disliked candidate exists.
- Requested evidence: Extend the restricted two-target case with B as the rider’s DislikedTarget. Observe one stationary delivery on B when only A is blocked. With A’s command sight restored, observe that the valid native proposal A keeps priority.

**R9-F3** · P2 · evidence_gap · blocks readiness — The queued invalidation check bypasses the rejection-code handler
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:2799`; acceptance: MMR-04, S05
- Trigger: Round9InvalidationBeforeGeometry promotes the queued automatic wrapper after its remembered target becomes unconscious outside stationary reach.
- Expected: The queued wrapper delivers its terminal outcome to the same production handler as a dispatched wrapper, and the test checks the resulting TargetInvalid rejection code.
- Actual: The queued case constructs its wrapper with Order and binds its automatic-run ownership, but leaves the terminal handler disconnected. Gate log line 1951 prints TargetInvalid and the unconscious reason followed by <none> for the controller code. The final assertion compares only the cause and reason for promotion.
- Why it blocks / does not: The requested queued rejection-code observation is missing. The gate can pass even if the controller reports a different code during promotion.
- Requested correction: Bind the queued wrapper to controller.ProductionTerminal or obtain it through production dispatch. Assert the exact rejection code for all three observation paths.
- Requested evidence: The focused gate should show TargetInvalid as both cause and controller rejection code, the bounded unconscious-target reason, zero movement and zero spending for controller-first, wrapper-first and queued promotion.

**R9-F4** · P3 · optional_polish · non-blocking — Range-rejected candidates still trigger a sight query
- Location: `src/KingmakerMountedCombat/Integration/NativeBrainRankingReader.cs:61`; acceptance: MMR-04
- Trigger: The native candidate list includes a held-rider or marked-unreachable target outside the proposal action’s range.
- Expected: A restricted candidate that already fails action range can be rejected without querying its sight.
- Actual: The reader calls info.InLos(context) for every held-rider or marked-unreachable candidate before the controller compares its distance with ActionRangeWithCorpulence.
- Why it blocks / does not: The extra query has no effect on the demonstrated selection result. Its frame-time cost was unmeasured.
- Requested correction: Short-circuit the sight query when the candidate already exceeds its action range with corpulence.
- Requested evidence: If addressed, retain the in-range and out-of-range controls and confirm the out-of-range candidate causes zero sight queries.

#### Prior finding assessments

- **R8-F1** — `fix_verified` — The original equal-distance case now keeps reachable, disliked A. Gate lines 1943–1946 show one rider wrapper targeting A and movement toward A. The in-range unreachable control selected and struck B with zero moves. R9-F1 concerns B leaving range after valid acceptance.
- **R8-F2** — `fix_verified` — Geometry-only retirement now checks the exact unreleased wrapper’s target validity. Controller-first and wrapper-first runs both emitted TargetInvalid, the unconscious-target reason and the TargetInvalid rejection code with zero movement and spending. Queued promotion emitted the same cause and reason. The conscious out-of-reach control retained its cancellation. R9-F3 covers the missing controller callback in the new queued test.
- **R8-F3** — `fix_verified` — The harness includes terminalBranchesPath in the input list passed through Get-FileHash. The candidate gate reached the success marker after the receipt write.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository instructions and validation policy, current handoffs, M..H feature diff, round-8 findings, round-9 correction diff, developer report and controller evidence.; NativeBrainRankingReader, ResolveEqualRankTie, automatic proposal acceptance, retained-order retirement and dispatch, pre-start movement, queued promotion, terminal reporting and the corresponding harness setup.
- **Executed verification:** Verified the snapshot HEAD, enrolled mission SHA-256 and controller gate log SHA-256. The feature diff passed git diff --check.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 on the exact candidate. It exited 0 in 48.7 seconds with an unchanged worktree. Components passed 1542/1542, source validation 153/153, the orders contract 4/4, the lifecycle contract 79/79, and the attack-path harness 166/166. All 15 focused children passed.; The gate recorded the round-8 target choices, carrier movement, terminal causes, reasons and spending. It also confirmed identical source and embedded Mounted Shield icon bytes and passing UMM 0.31.1 and 0.33.0 binding checks.
- **Source inspection only:** The retained-target and disliked-alternative failures follow from the production selection and dispatch branches. The supplied gate does not exercise those two transitions.; The queued fixture records the wrapper outcome without invoking HandleCommandTerminal. Production TryExecuteAction supplies that handler to the wrapper constructor.
- **Required gaps:** Demonstrate that an in-range unreachable target which leaves range during an accepted order’s cooldown wait receives zero approach commands, and that a subsequent reachable proposal can proceed.; Demonstrate stationary delivery on the sole legal disliked alternative when the native proposal fails mounted command sight.; Observe the exact TargetInvalid rejection code from the production terminal handler during queued promotion.
- **Limitations:** The supplied gameplay checks use a simulated engine and compiled contract inspection. Native gameplay, damage attribution and input-to-movement latency remain owner observations.; The developer session transcript was withheld. The controller receipt and matching log confirm the candidate gate. The separately reported direct diagnostic runs and the harness’s individual per-file receipt were unavailable for independent inspection.

#### Summary

- **Delivered behavior:** Offline checks support turn-based horse follow-through on the retained pair order, native-shaped single versus full routines, and real-time approach during a strike-readiness wait. The round-8 proposal and invalidation counterexamples pass. Automatic target retention, stationary fallback selection and queued rejection-code evidence still need correction.
- **Scope reviewed:** Final PR-1 source and offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05 and S08. The correction review traced changes since b1575568 through their directly affected consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N04: Attribute horse attacks, rolls and damage by actor, target and weapon in both initiative arrangements, including a spent or ended horse.; N05: Observe real-time acquisition and approach with two eligible hostiles, including AI-off, Stop, Hold and explicit-target controls.; N06: Compare order acceptance, first movement and first attack with an equivalent unmounted actor under the same cooldown and preparation conditions.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a218af-b029-71e1-b7bf-bcecf86ea7f7`, duration 620.8s, response sha256 `750635b4ad5012c128ef3a079d1a1b712bead867b379d03a1959615522e494ec`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r09-68463d`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "f2f2fa554a501d5fb1c2e276a9bd988df697fc58",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The three round-8 corrections are present and the focused gate passed. Two automatic melee cases still violate MMR-04. A retained order can pursue a marked-unreachable target after it leaves range, and a held pair can reject its only legal stationary target. The queued invalidation test also bypasses the controller that reports the rejection code.\n\n## Continuation for Claude\n\nKeep the correction in PR-1\u2019s existing automatic-order and test paths. Recheck the selected target\u2019s reachability restriction when a waiting order resumes. Apply the native dislike preference among targets the pair can actually strike. Preserve ordinary advance toward reachable targets, manual and ranged target priority, and native completion of released attacks. Connect the queued test wrapper to the production terminal handler and assert its exact rejection code. Exercise the transitions in R9-F1 through R9-F3 through the existing harness and retain the metered focused gate receipt for the corrected candidate. R9-F4 is optional.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD, enrolled mission SHA-256 and controller gate log SHA-256. The feature diff passed git diff --check.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 on the exact candidate. It exited 0 in 48.7 seconds with an unchanged worktree. Components passed 1542/1542, source validation 153/153, the orders contract 4/4, the lifecycle contract 79/79, and the attack-path harness 166/166. All 15 focused children passed.",
      "The gate recorded the round-8 target choices, carrier movement, terminal causes, reasons and spending. It also confirmed identical source and embedded Mounted Shield icon bytes and passing UMM 0.31.1 and 0.33.0 binding checks."
    ],
    "inspected": [
      "The enrolled mission, repository instructions and validation policy, current handoffs, M..H feature diff, round-8 findings, round-9 correction diff, developer report and controller evidence.",
      "NativeBrainRankingReader, ResolveEqualRankTie, automatic proposal acceptance, retained-order retirement and dispatch, pre-start movement, queued promotion, terminal reporting and the corresponding harness setup."
    ],
    "limitations": [
      "The supplied gameplay checks use a simulated engine and compiled contract inspection. Native gameplay, damage attribution and input-to-movement latency remain owner observations.",
      "The developer session transcript was withheld. The controller receipt and matching log confirm the candidate gate. The separately reported direct diagnostic runs and the harness\u2019s individual per-file receipt were unavailable for independent inspection."
    ],
    "required_gaps": [
      "Demonstrate that an in-range unreachable target which leaves range during an accepted order\u2019s cooldown wait receives zero approach commands, and that a subsequent reachable proposal can proceed.",
      "Demonstrate stationary delivery on the sole legal disliked alternative when the native proposal fails mounted command sight.",
      "Observe the exact TargetInvalid rejection code from the production terminal handler during queued promotion."
    ],
    "source_inspection_only": [
      "The retained-target and disliked-alternative failures follow from the production selection and dispatch branches. The supplied gate does not exercise those two transitions.",
      "The queued fixture records the wrapper outcome without invoking HandleCommandTerminal. Production TryExecuteAction supplies that handler to the wrapper constructor."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ResolveEqualRankTie checks B\u2019s native range and sight at proposal time. On the later drive, target validity covers life, hostility and attackability, while stationary depends only on pair-wide movement restrictions. PlanStockDispatch can therefore issue an approach-enabled wrapper for B after B leaves range.",
      "blocking_rationale": "An automatic order can direct the horse toward an enemy the native brain now excludes and keep a reachable proposal from taking over.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once B leaves its native action range while still marked unreachable, the automatic order releases B before dispatch or movement. A fresh reachable proposal can then proceed.",
      "id": "R9-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1979
      },
      "priority": "P2",
      "requested_correction": "Recheck the target-specific native restriction before a retained automatic order dispatches or starts an approach. Preserve the stationary strike on an in-range, in-sight unreachable target and normal travel toward a reachable target.",
      "requested_evidence": "Start with the round-9 in-range control and both Standards cooling. After B is selected, move it outside range while keeping it perceived and marked unreachable. Resume the drive and observe zero wrappers, carriers or spending for B, followed by acceptance of reachable A.",
      "title": "A waiting automatic order can chase an unreachable target after it leaves range",
      "trigger": "A mobile pair selects B while B is in native action range and sight despite its UnreachableUnits entry. Both Standards are cooling. Before the first dispatch, B moves outside range while remaining in group memory and UnreachableUnits."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ResolveEqualRankTie sets B\u2019s Eligible flag to false solely because B is disliked and was not the proposal. With A also ineligible, Choose returns A and ObserveAutomaticProposal refuses it for failed stationary geometry.",
      "blocking_rationale": "The pair can remain idle across repeated proposals with a conscious hostile it can legally strike from its current position.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The pair selects B and strikes without travel. The installed NotImpatient consideration gives B a positive score of 0.5.",
      "id": "R9-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1576
      },
      "priority": "P2",
      "requested_correction": "Apply the dislike preference after determining which comparable candidates pass native and mounted admission. Preserve the native preference when a legal non-disliked candidate exists.",
      "requested_evidence": "Extend the restricted two-target case with B as the rider\u2019s DislikedTarget. Observe one stationary delivery on B when only A is blocked. With A\u2019s command sight restored, observe that the valid native proposal A keeps priority.",
      "title": "Dislike excludes the only legal stationary alternative",
      "trigger": "A held pair has two perceived enemies in native range with equal TargetInfo weights and clear cached sight. Native proposes non-disliked A. Mounted command sight is blocked for A and clear for disliked B. There is no manual target or existing order."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The queued case constructs its wrapper with Order and binds its automatic-run ownership, but leaves the terminal handler disconnected. Gate log line 1951 prints TargetInvalid and the unconscious reason followed by <none> for the controller code. The final assertion compares only the cause and reason for promotion.",
      "blocking_rationale": "The requested queued rejection-code observation is missing. The gate can pass even if the controller reports a different code during promotion.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The queued wrapper delivers its terminal outcome to the same production handler as a dispatched wrapper, and the test checks the resulting TargetInvalid rejection code.",
      "id": "R9-F3",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 2799
      },
      "priority": "P2",
      "requested_correction": "Bind the queued wrapper to controller.ProductionTerminal or obtain it through production dispatch. Assert the exact rejection code for all three observation paths.",
      "requested_evidence": "The focused gate should show TargetInvalid as both cause and controller rejection code, the bounded unconscious-target reason, zero movement and zero spending for controller-first, wrapper-first and queued promotion.",
      "title": "The queued invalidation check bypasses the rejection-code handler",
      "trigger": "Round9InvalidationBeforeGeometry promotes the queued automatic wrapper after its remembered target becomes unconscious outside stationary reach."
    },
    {
      "acceptance_ids": [
        "MMR-04"
      ],
      "actual": "The reader calls info.InLos(context) for every held-rider or marked-unreachable candidate before the controller compares its distance with ActionRangeWithCorpulence.",
      "blocking_rationale": "The extra query has no effect on the demonstrated selection result. Its frame-time cost was unmeasured.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "A restricted candidate that already fails action range can be rejected without querying its sight.",
      "id": "R9-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeBrainRankingReader.cs",
        "line": 61
      },
      "priority": "P3",
      "requested_correction": "Short-circuit the sight query when the candidate already exceeds its action range with corpulence.",
      "requested_evidence": "If addressed, retain the in-range and out-of-range controls and confirm the out-of-range candidate causes zero sight queries.",
      "title": "Range-rejected candidates still trigger a sight query",
      "trigger": "The native candidate list includes a held-rider or marked-unreachable target outside the proposal action\u2019s range."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F1",
      "rationale": "The original equal-distance case now keeps reachable, disliked A. Gate lines 1943\u20131946 show one rider wrapper targeting A and movement toward A. The in-range unreachable control selected and struck B with zero moves. R9-F1 concerns B leaving range after valid acceptance."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F2",
      "rationale": "Geometry-only retirement now checks the exact unreleased wrapper\u2019s target validity. Controller-first and wrapper-first runs both emitted TargetInvalid, the unconscious-target reason and the TargetInvalid rejection code with zero movement and spending. Queued promotion emitted the same cause and reason. The conscious out-of-reach control retained its cancellation. R9-F3 covers the missing controller callback in the new queued test."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F3",
      "rationale": "The harness includes terminalBranchesPath in the input list passed through Get-FileHash. The candidate gate reached the success marker after the receipt write."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r09-68463d",
  "round": 9,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline checks support turn-based horse follow-through on the retained pair order, native-shaped single versus full routines, and real-time approach during a strike-readiness wait. The round-8 proposal and invalidation counterexamples pass. Automatic target retention, stationary fallback selection and queued rejection-code evidence still need correction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04: Attribute horse attacks, rolls and damage by actor, target and weapon in both initiative arrangements, including a spent or ended horse.",
      "N05: Observe real-time acquisition and approach with two eligible hostiles, including AI-off, Stop, Hold and explicit-target controls.",
      "N06: Compare order acceptance, first movement and first attack with an equivalent unmounted actor under the same cooldown and preparation conditions."
    ],
    "scope_reviewed": "Final PR-1 source and offline checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05 and S08. The correction review traced changes since b1575568 through their directly affected consumers.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
