<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r13-review part=1/1 sha256=4d57995e84e518369bf92981088b26076b7000e3a162c5e72cef59d77794ece0 -->
### Codex GPT-6.2 Astra — review — round 13 — candidate `915f2fe738`

**Decision:** `changes_required` — All four round-12 findings are fixed. One S05 defect remains. A held pair can discard a target that is still in reach after the rider’s final hit, while its animation finishes. A separate regression check reads the wrapper’s cancellation cause before the controller action it claims to verify.

Continuation for Claude

For any owner-authorized continuation, evaluate the retained target’s stationary eligibility independently of an exhausted attack plan. Preserve AI-off retirement, genuine target loss, native animation completion and each actor’s action cost. Extend the existing offline harness to drive the controller between the final act and OnEnded. In the range-loss control, inspect the released wrapper immediately after controller retirement. Run the existing focused Fast gate on the corrected candidate. Round 13 exhausts the recorded PR-1 review allocation. Further formal review requires owner authorization.

#### Findings

**R13-F1** · P2 · defect · blocks readiness — A held pair drops an in-range target during the rider’s recovery animation
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1939`; acceptance: MMR-04, S05
- Trigger: An AI-enabled rider delivers the final act of an automatic melee routine. The horse is held, the target remains conscious, perceived and in stationary reach, and the controller drives before the animation finishes.
- Expected: The order keeps the same target through the rider’s recovery animation. Once the rider finishes, the eligible horse can strike that target from its current position.
- Actual: StockIntentStrikeAdmissibleNow uses the unfinished wrapper. After its last act, PlannedAttack is null and EvaluateCurrentNativeAdmission returns Unavailable. The controller records “hold out of reach” and clears the order even though neither the target nor the pair moved.
- Why it blocks / does not: The eligible horse loses follow-through on the retained target. A fresh brain proposal can select another enemy even though the original target stayed eligible.
- Requested correction: When the released wrapper has exhausted its plan, use the retained target’s current stationary eligibility for future dispatch. Preserve retirement on AI-off, newer player work and actual loss of eligibility.
- Requested evidence: Hold only the horse and keep the target in reach. After the rider’s final act, drive once with Result=Success, attack index equal to plan count and IsFinished=false. Verify the same generation and target, zero retirements and zero travel. Finish the animation and observe the eligible horse attack that target without a fresh proposal, with one Standard charged per acting unit.

**R13-F2** · P2 · evidence_gap · blocks readiness — The released-strike control checks cancellation before the controller can cause it
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:3137`; acceptance: MMR-04, S05
- Trigger: The released wrapper’s target leaves native action range and the controller retires the automatic generation.
- Expected: The released-strike control observes whether the controller changed the wrapper’s cause or transaction when it retired the generation.
- Actual: The test captures wrapper.EndCause at line 3120, drives the controller at line 3122, then asserts the earlier value at line 3137. Gate line 1977 records generation retirement, but its printed cause is the pre-drive sample. The AI-off replay exercises a different retirement branch.
- Why it blocks / does not: This control would still pass if the range-loss branch called RecordAutomaticAdmissionLoss on the released wrapper and cancelled its transaction.
- Requested correction: Sample the wrapper immediately after the retiring controller drive and before the next simulated engine tick. Assert its current cause, transaction state and continued ownership of the remaining native routine.
- Requested evidence: Show one range-based retirement with the wrapper still running and its transaction uncancelled. Return the target to legal reach before the next act and observe native completion with the original Standard cost and zero redispatches from the retired generation.

**R13-F3** · P3 · optional_polish · non-blocking — An out-of-range approach checks the same automatic order twice per tick
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1575`; acceptance: MMR-04
- Trigger: An automatic wrapper continues a permitted approach toward a target outside strike reach.
- Expected: Each approach tick can use one admission decision before applying the generic movement restriction.
- Actual: TickDeferredCarrier asks at lines 1542 and 1551. TickApproach asks at lines 1575 and 1664. On the continuing out-of-range path, each pair of calls repeats target checks and scans the actors’ command containers.
- Why it blocks / does not: Both paths preserve the observed gameplay result. The issue is repeated work on a per-tick path.
- Requested correction: Keep the automatic decision before generic movement cancellation and remove the duplicate call on each continuing path.
- Requested evidence: If adjusted, count one admission decision in an out-of-range active-carrier tick and a deferred-carrier tick. Retain the existing Hold ordering and cleanup checks.

#### Prior finding assessments

- **R12-F1** — `fix_verified` — The AI-off drive clears the generation and leaves the delivered wrapper running. Gate line 2020 records two completed strikes, one Standard spent, zero later dispatches and acceptance of a fresh proposal.
- **R12-F2** — `fix_verified` — The harness now sets Horse.HoldState and calls production AutomaticMovementForbidden. Gate lines 2008 and 2010 show zero pre-readiness spending and zero moves in both cases. A received one stationary delivery when B left reach. B received it when B stayed in reach.
- **R12-F3** — `fix_verified` — The started approach asks the automatic order before generic movement cancellation. Controller-first and wrapper-first Hold observations each recorded “hold out of reach” once, finished the exact carrier and delivered zero attacks. The production predicate also covers loss of the horse’s CanMove.
- **R12-F4** — `fix_verified` — The second retirement call in the controller drive now applies only to explicit orders. Gate line 2026 records one stationary probe, the same retained target, zero moves and zero spending for the held, cooling pair.

#### Evidence assessment

- **Inspected:** Enrolled mission, repository instructions and validation policy, current handoff, feature diff, round-12 ledger, developer report and the correction from 29816172 to the candidate.; Automatic proposal, dispatch and retirement paths, wrapper approach and terminal handling, native strike admission, harness assembly and the candidate-bound gate receipt and log.
- **Executed verification:** Verified the snapshot HEAD and mission SHA-256 against the request. The supplied gate log’s SHA-256 matches its controller receipt.; The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at the exact candidate. It exited 0 in 50.4 seconds with the worktree unchanged. Fast passed 4/4, components 1542/1542, Validate-Source 153/153 and focused children 15/15.; The gate records 204/204 attack-path harness checks, 82/82 lifecycle contract checks and 4/4 installed attack-order contract checks. The Mounted Shield contract passed 114/114 and confirmed identical packet, production and embedded icon bytes.
- **Source inspection only:** Traced the post-delivery stationary check through StockIntentStrikeAdmissibleNow and EvaluateCurrentNativeAdmission. The installed lifecycle record distinguishes the final OnAction from the later OnEnded. The harness combines them at AttackPathHarnessTemplate.cs line 282.; Traced duplicate admission checks in the active and deferred approach ticks. Their frame-time cost was not measured.
- **Required gaps:** Correct and exercise stationary target retention between the final act and animation completion.; Observe the released wrapper’s cause and transaction state after the controller retires an order for loss of native admission.
- **Limitations:** The controller supplied the candidate gate receipt and log in place of the full developer shell transcript. I reviewed those records in the read-only snapshot.; The attack-path harness uses a simulated engine. No candidate gameplay, actor-attributed native damage or mounted-versus-unmounted timing measurement was observed.

#### Summary

- **Delivered behavior:** Offline checks cover ordinary turn-based horse follow-through, native-ranked real-time targeting and approach before strike readiness. The candidate also preserves the rider’s delivered routine when AI is switched off. Stationary target retention during final attack recovery remains incorrect.
- **Scope reviewed:** Final PR-1 checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, the S08 regression gate and all four pending round-12 findings.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Decide whether to authorize further formal PR-1 review after the recorded round cap.; N04 covers actor-attributed horse attacks in both initiative arrangements and the spent or ended horse control.; N05 and N06 cover in-game target acquisition and retention under AI-off, Stop and Hold, and mounted movement and strike timing against a comparable unmounted actor.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21a30-8a20-7863-982f-879f66a7953d`, duration 555.7s, response sha256 `9d18587384a77be34d6688995b8dcdaa0efc2f104d9543143b156276cf513a96`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r13-362202`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "915f2fe7380b2439c97d5cc5bfccf5e233e3be32",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "All four round-12 findings are fixed. One S05 defect remains. A held pair can discard a target that is still in reach after the rider\u2019s final hit, while its animation finishes. A separate regression check reads the wrapper\u2019s cancellation cause before the controller action it claims to verify.\n\nContinuation for Claude\n\nFor any owner-authorized continuation, evaluate the retained target\u2019s stationary eligibility independently of an exhausted attack plan. Preserve AI-off retirement, genuine target loss, native animation completion and each actor\u2019s action cost. Extend the existing offline harness to drive the controller between the final act and OnEnded. In the range-loss control, inspect the released wrapper immediately after controller retirement. Run the existing focused Fast gate on the corrected candidate. Round 13 exhausts the recorded PR-1 review allocation. Further formal review requires owner authorization.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD and mission SHA-256 against the request. The supplied gate log\u2019s SHA-256 matches its controller receipt.",
      "The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 at the exact candidate. It exited 0 in 50.4 seconds with the worktree unchanged. Fast passed 4/4, components 1542/1542, Validate-Source 153/153 and focused children 15/15.",
      "The gate records 204/204 attack-path harness checks, 82/82 lifecycle contract checks and 4/4 installed attack-order contract checks. The Mounted Shield contract passed 114/114 and confirmed identical packet, production and embedded icon bytes."
    ],
    "inspected": [
      "Enrolled mission, repository instructions and validation policy, current handoff, feature diff, round-12 ledger, developer report and the correction from 29816172 to the candidate.",
      "Automatic proposal, dispatch and retirement paths, wrapper approach and terminal handling, native strike admission, harness assembly and the candidate-bound gate receipt and log."
    ],
    "limitations": [
      "The controller supplied the candidate gate receipt and log in place of the full developer shell transcript. I reviewed those records in the read-only snapshot.",
      "The attack-path harness uses a simulated engine. No candidate gameplay, actor-attributed native damage or mounted-versus-unmounted timing measurement was observed."
    ],
    "required_gaps": [
      "Correct and exercise stationary target retention between the final act and animation completion.",
      "Observe the released wrapper\u2019s cause and transaction state after the controller retires an order for loss of native admission."
    ],
    "source_inspection_only": [
      "Traced the post-delivery stationary check through StockIntentStrikeAdmissibleNow and EvaluateCurrentNativeAdmission. The installed lifecycle record distinguishes the final OnAction from the later OnEnded. The harness combines them at AttackPathHarnessTemplate.cs line 282.",
      "Traced duplicate admission checks in the active and deferred approach ticks. Their frame-time cost was not measured."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "StockIntentStrikeAdmissibleNow uses the unfinished wrapper. After its last act, PlannedAttack is null and EvaluateCurrentNativeAdmission returns Unavailable. The controller records \u201chold out of reach\u201d and clears the order even though neither the target nor the pair moved.",
      "blocking_rationale": "The eligible horse loses follow-through on the retained target. A fresh brain proposal can select another enemy even though the original target stayed eligible.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The order keeps the same target through the rider\u2019s recovery animation. Once the rider finishes, the eligible horse can strike that target from its current position.",
      "id": "R13-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1939
      },
      "priority": "P2",
      "requested_correction": "When the released wrapper has exhausted its plan, use the retained target\u2019s current stationary eligibility for future dispatch. Preserve retirement on AI-off, newer player work and actual loss of eligibility.",
      "requested_evidence": "Hold only the horse and keep the target in reach. After the rider\u2019s final act, drive once with Result=Success, attack index equal to plan count and IsFinished=false. Verify the same generation and target, zero retirements and zero travel. Finish the animation and observe the eligible horse attack that target without a fresh proposal, with one Standard charged per acting unit.",
      "title": "A held pair drops an in-range target during the rider\u2019s recovery animation",
      "trigger": "An AI-enabled rider delivers the final act of an automatic melee routine. The horse is held, the target remains conscious, perceived and in stationary reach, and the controller drives before the animation finishes."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "The test captures wrapper.EndCause at line 3120, drives the controller at line 3122, then asserts the earlier value at line 3137. Gate line 1977 records generation retirement, but its printed cause is the pre-drive sample. The AI-off replay exercises a different retirement branch.",
      "blocking_rationale": "This control would still pass if the range-loss branch called RecordAutomaticAdmissionLoss on the released wrapper and cancelled its transaction.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The released-strike control observes whether the controller changed the wrapper\u2019s cause or transaction when it retired the generation.",
      "id": "R13-F2",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 3137
      },
      "priority": "P2",
      "requested_correction": "Sample the wrapper immediately after the retiring controller drive and before the next simulated engine tick. Assert its current cause, transaction state and continued ownership of the remaining native routine.",
      "requested_evidence": "Show one range-based retirement with the wrapper still running and its transaction uncancelled. Return the target to legal reach before the next act and observe native completion with the original Standard cost and zero redispatches from the retired generation.",
      "title": "The released-strike control checks cancellation before the controller can cause it",
      "trigger": "The released wrapper\u2019s target leaves native action range and the controller retires the automatic generation."
    },
    {
      "acceptance_ids": [
        "MMR-04"
      ],
      "actual": "TickDeferredCarrier asks at lines 1542 and 1551. TickApproach asks at lines 1575 and 1664. On the continuing out-of-range path, each pair of calls repeats target checks and scans the actors\u2019 command containers.",
      "blocking_rationale": "Both paths preserve the observed gameplay result. The issue is repeated work on a per-tick path.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Each approach tick can use one admission decision before applying the generic movement restriction.",
      "id": "R13-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1575
      },
      "priority": "P3",
      "requested_correction": "Keep the automatic decision before generic movement cancellation and remove the duplicate call on each continuing path.",
      "requested_evidence": "If adjusted, count one admission decision in an out-of-range active-carrier tick and a deferred-carrier tick. Retain the existing Hold ordering and cleanup checks.",
      "title": "An out-of-range approach checks the same automatic order twice per tick",
      "trigger": "An automatic wrapper continues a permitted approach toward a target outside strike reach."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F1",
      "rationale": "The AI-off drive clears the generation and leaves the delivered wrapper running. Gate line 2020 records two completed strikes, one Standard spent, zero later dispatches and acceptance of a fresh proposal."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F2",
      "rationale": "The harness now sets Horse.HoldState and calls production AutomaticMovementForbidden. Gate lines 2008 and 2010 show zero pre-readiness spending and zero moves in both cases. A received one stationary delivery when B left reach. B received it when B stayed in reach."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F3",
      "rationale": "The started approach asks the automatic order before generic movement cancellation. Controller-first and wrapper-first Hold observations each recorded \u201chold out of reach\u201d once, finished the exact carrier and delivered zero attacks. The production predicate also covers loss of the horse\u2019s CanMove."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F4",
      "rationale": "The second retirement call in the controller drive now applies only to explicit orders. Gate line 2026 records one stationary probe, the same retained target, zero moves and zero spending for the held, cooling pair."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r13-362202",
  "round": 13,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline checks cover ordinary turn-based horse follow-through, native-ranked real-time targeting and approach before strike readiness. The candidate also preserves the rider\u2019s delivered routine when AI is switched off. Stationary target retention during final attack recovery remains incorrect.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Decide whether to authorize further formal PR-1 review after the recorded round cap.",
      "N04 covers actor-attributed horse attacks in both initiative arrangements and the spent or ended horse control.",
      "N05 and N06 cover in-game target acquisition and retention under AI-off, Stop and Hold, and mounted movement and strike timing against a comparable unmounted actor."
    ],
    "scope_reviewed": "Final PR-1 checkpoint for MMR-03, MMR-04 and MMR-05, including S04, S05, the S08 regression gate and all four pending round-12 findings.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
