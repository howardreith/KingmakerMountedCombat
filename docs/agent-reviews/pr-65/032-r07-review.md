<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r07-review part=1/1 sha256=f632fa0d3c9ce304297b0384780b1808377faf49e3cad5f7d184b9e31b7c67d4 -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `7c6ec09036`

**Decision:** `changes_required` — The focused gate passed on the submitted commit. Click ownership and automatic target retirement still depend on which hook runs, and restricted melee selection can miss a legal target or select one the native brain would reject. Two S04 checks also leave required observations unresolved.

## Continuation for Claude

Bind click ownership to the actor whose ManualTarget the click handler writes. Give the controller and wrapper the same terminal classification when an automatic target despawns and leaves perception together. For restricted melee acquisition, apply native target eligibility and stationary strike admission before comparing distances, including for the proposed target.

In the existing harness, use a legal horse routine that passes production plan validation. Drive the rider terminal from raw capability facts and measure submissions, deliveries and warnings across the whole transition. Preserve native action ownership, explicit targets, ranged behavior and the existing click router. The next evidence needed is the focused counterexamples below and the enrolled Fast gate on the corrected candidate, subject to the existing controller stop rules and owner limits. N04–N06 remain owner playtest observations.

#### Findings

**R1-F11** · P2 · defect · blocks readiness — A command submission still stands in for the click's target assignment
- Location: `src/KingmakerMountedCombat/Integration/MountedPatchController.cs:1264`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: A handled click on the order's target runs a command on a different actor from the one receiving the handler's target write, or another command runs synchronously during the click.
- Expected: Ownership follows the click handler's actual per-actor assignment, including when the command is routed or its submission is refused.
- Actual: UnitCommandRunPrefix marks every public Run on a pair actor while the click is open. An unrelated synchronous Run that writes no target can therefore release a skipped rider's ownership. The existing RouteRiderClickApproach also demonstrates the reverse mismatch. It can skip the rider's Run or submit the move on the mount, while OnClick still writes the rider's ManualTarget.
- Why it blocks / does not: Retirement can preserve an expired pair target or clear a newer player assignment to the same target.
- Requested correction: Observe the ManualTarget assignment at its call site in ClickUnitHandler.OnClick with the original actor, target and click identity. Preserve composition with the existing cannot-attack router.
- Requested evidence: A skipped rider receiving an unrelated non-writing Run must have its old T cleared on retirement. A real later rider assignment to T must survive, including when the click's rider submission is consumed. Retain the companion, simulated and unhandled controls.

**R4-F1** · P2 · evidence_gap · blocks readiness — The horse retarget test completes a two-attack plan in single-attack mode
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:623`; acceptance: MMR-03, S04
- Trigger: Round7RemainingRoutine replaces the order after the horse's first modeled delivery.
- Expected: The remaining attack on A and subsequent dispatch for B are observed on an ordinary horse command with a legally admitted multi-attack plan.
- Actual: The fixture passes true for single-attack mode, including through the constructor used by the spliced dispatch. Its UnitAttack.Init nevertheless installs two attacks, and its CreateAndValidateChildAttack only increments a counter. Production rejects a single-attack command with two plan entries at MountedPairAttackCommand.cs line 2073 and also checks the exact mount weapon and slot.
- Why it blocks / does not: The recorded 2/2 completion and one spend exercise the terminal branch using a plan the product would reject before its first strike.
- Requested correction: Forward the ordinary versus explicit single-attack mode, provide the matching natural weapon and slot, and exercise the production plan check. Make the modeled plan honor single-attack mode.
- Requested evidence: Show a legal horse routine advance from 1/2 to 2/2 on A with one Standard spent. Then show one dispatch for valid B, zero for invalid B, and interruption on hard cancellation. An explicit single-attack control should deliver once.

**R4-F5** · P2 · evidence_gap · blocks readiness — The terminal-to-dispatch test supplies the cause and misses part of the observation window
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:2472`; acceptance: MMR-03, S04
- Trigger: An unspent rider recovers between its capability terminal and the controller's next paired dispatch.
- Expected: The production recording path derives the terminal facts, and the test observes both actors' submissions, deliveries and all warnings through the recovered rider's next drive.
- Actual: The test now runs the production drive and terminal handler, but writes the classified branch and CantAct directly into RecordNativeTerminal. Its capability adapter also reports CanNotAttack for that state, an earlier native interruption condition in the windup case. The test samples rider submissions before the terminal and starts counting warnings after it. A warning emitted at the terminal is excluded from the reported total.
- Why it blocks / does not: The recorded rows cannot establish the claimed native-branch attribution, absence of a second rider submission, or exactly-once warning across the transition.
- Requested correction: Feed the modeled CanAct observation and separate condition facts through the existing production terminal hooks. Use the production EvaluateStockActor for participant selection. Measure submissions and warnings from before the terminal until after the next drive.
- Requested evidence: For both pre-start force-finish and interrupted windup, record zero additional rider submissions after recovery. Show one eligible horse submission with delivery and zero warnings, or zero horse submissions and exactly one rider refusal for a spent or ended horse. Keep delivered and retired controls.

**R6-F1** · P2 · defect · blocks readiness — Automatic despawn can still be reported as ordinary cancellation
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1898`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: An automatic melee target despawns and leaves the rider group's memory before an installed wrapper delivers.
- Expected: Both observers record TargetInvalid and the bounded despawn reason for the unreleased wrapper, with no further movement or spend.
- Actual: TryDriveStockAttackIntent first calls RetireAutomaticIntentIfUnperceived. That path retires the wrapper with Cancelled before RecordTargetInvalidation is reached. The wrapper's own pre-start tick and OnStart check target invalidation first and record TargetInvalid with the despawn reason. The parity fixture uses an explicit order.
- Why it blocks / does not: The same automatic target loss still produces different terminal information and rejection codes depending on update order.
- Requested correction: Give both observers the same precedence when target invalidation and perception loss coincide. Preserve prompt retirement for a still-valid target that leaves perception and native completion for a delivered strike.
- Requested evidence: Repeat the controller-first and wrapper-first comparison on an automatic order with IsInState false and the target absent from group memory. Compare cause, terminal reason, rejection code, movement and spending.

**R7-F1** · P2 · defect · blocks readiness — An ineligible native proposal prevents selection of a farther legal target
- Location: `src/KingmakerMountedCombat/Domain/MountedEngagementDiagnostic.cs:128`; acceptance: MMR-04, S05
- Trigger: Restricted travel, no current or manual target, and two perceived equal-ranked enemies. Native proposes the nearer enemy using cached sight, but only the farther enemy passes command sight.
- Expected: Fresh automatic melee acquisition chooses the nearest eligible equal-ranked target when the native proposal fails stationary command sight.
- Actual: Choose seeds best and bestDistance from the proposed target without reading its Eligible flag. Even after ResolveEqualRankTie marks that target ineligible, a farther equal-ranked candidate cannot beat its distance. ObserveAutomaticProposal then refuses the blocked target.
- Why it blocks / does not: An idle held pair can repeatedly decline an available stationary attack.
- Requested correction: Use the proposed target as the distance to beat only when it is eligible. Preserve manual target priority, ranged behavior and retention of a valid current order.
- Requested evidence: Reverse the round-7 fixture so native proposes the nearer blocked target and the farther equal-ranked target passes both sight checks. Observe one stationary delivery on the farther target. Keep the both-blocked refusal.

**R7-F2** · P2 · defect · blocks readiness — Stationary melee selection drops a native sight requirement
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1571`; acceptance: MMR-04, S05
- Trigger: The rider is under a native movement restriction. A nearer remembered enemy has clear command geometry but fails the rider's cached sight test.
- Expected: An alternative satisfies the native brain restrictions applicable to that actor and target as well as the pair's stationary command geometry.
- Actual: For forbidden travel and melee, the conditional evaluates StationaryStrikeAdmissible in place of rider.HasLOS. The ranking reader supplies the brain's candidate list before action-specific validity is applied. A nearer candidate can therefore replace the legal proposal even when the native held-rider or unreachable-target sight check rejects it.
- Why it blocks / does not: The tie-break can select an enemy outside the native action's eligible set.
- Requested correction: Retain the native sight requirement where the rider's movement is forbidden or the candidate is marked unreachable, and apply stationary mounted-strike admission for restricted pair travel.
- Requested evidence: Give a held rider two perceived, in-range candidates with clear command geometry. Set cached rider sight false only for the nearer alternative. The farther native proposal should receive the stationary strike. Retain the mount-only restriction and both-clear controls.

**R7-F3** · P3 · optional_polish · non-blocking — Restricted proposals build attack plans for candidates that cannot win the tie
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1565`; acceptance: MMR-04, S05
- Trigger: The brain's ranked list contains several enemies outside the proposed target's comparison rank.
- Expected: Only targets that could affect the selection need a native attack-plan probe.
- Actual: ResolveEqualRankTie constructs a mounted strike probe for each otherwise qualifying candidate under restricted travel before Choose discards candidates of unrelated ranks. ObserveAutomaticProposal probes the selected target again.
- Why it blocks / does not: This is avoidable plan construction. No frame-time impact was measured.
- Requested correction: Filter by comparable rank before constructing stationary strike probes, while preserving the dispatch-time recheck.
- Requested evidence: If addressed alongside the selection fix, compare PlannedStrikeProbeCount for a proposal with several irrelevant ranks and retain the same selected target.

#### Prior finding assessments

- **R1-F11** — `fix_rejected` — The simple skipped-rider control passes. Public Run still identifies the command executor, which can differ from the actor written by the click handler. An unrelated Run inside the bracket can also set the flag.
- **R4-F1** — `fix_rejected` — The harness advances the index through two act events and reaches production OnEnded. Its single-attack wrapper and two-entry plan fail production's start-time validation, so legal routine completion remains unverified.
- **R4-F5** — `fix_rejected` — The fixture turn, command submission and terminal handler are now connected. The fixture still supplies the classified capability branch, and its rider-submission and warning counters omit opposite ends of the transition.
- **R4-F6** — `fix_verified` — AttachActorCharges populates the outcome selected for delivery. All six detailed and fallback rows observed that same object at LastOutcome, with one report and the expected actor and charge flags for an unstarted rider, delivered rider and delivered horse.
- **R5-F3** — `fix_verified` — Both cases now begin at ObserveAutomaticProposal. The 2.6 m order stayed pending during cooldown and delivered once after readiness with zero travel. The 3.2 m proposal was refused with zero travel and zero deliveries.
- **R6-F1** — `fix_rejected` — The explicit despawn case now emits matching TargetInvalid results. An automatic despawn that also removes the target from memory still takes the controller's earlier generic cancellation path.
- **R6-F2** — `fix_verified` — For the reported farther legal proposal, the blocked nearer alternative is now excluded and the farther target receives the strike. With both command sight checks clear, the nearer target receives it. R7-F1 and R7-F2 identify different selection failures.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and current handoffs, complete feature diff, round-7 delta, developer report, prior findings ledger, controller receipt and detailed gate output.; The click handler patches and existing cannot-attack router, retained-order ownership, target ranking, wrapper retirement, terminal reporting, dispatch planner and production attack-plan validation.; The harness source extraction, fixture constructors, modeled native commands, round-7 assertions and the measurements printed for each pending finding.
- **Executed verification:** Confirmed HEAD and the merge base against the controller inputs. The mission SHA-256 matched, and git diff --check reported no whitespace errors.; Verified the controller receipt and its log SHA-256, 6a4c8ef4380af0a32eb401d422b90783d2c158b2c12f9ce5f704735bcb736e6a. The required Fast gate ran at 7c6ec090, exited 0 in 54.2 seconds and recorded an unchanged worktree.; The log records 1542 component passes, 153 source-validation passes and all 15 focused children passing. The attack-path harness reported 149/0, the lifecycle contract 76/0 and the attack-order assembly contract 4/0.; S08 is supported by the compiled Mounted Shield contract at 114/0, selection rollback at 68/0 and matching packet, production and embedded icon SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.
- **Source inspection only:** The click routing mismatch, competing automatic retirement paths and two target-ranking counterexamples were traced in production source. The gate did not exercise those combinations.; Comparison of the harness constructors and plan validator with production established why the recorded two-attack plan would be rejected.
- **Required gaps:** Observe click ownership through the actual assignment when command routing and the receiving actor differ.; Demonstrate retargeting with a legal multi-attack horse plan, and capture the full rider-terminal-to-horse transition.; Compare both observers on automatic despawn with simultaneous memory loss, and exercise both remaining restricted-target selection cases.
- **Limitations:** The reviewer used read-only inspection. Executed build and test results come from the candidate-bound controller receipt. The supplied transcript withholds the developer's other foreground commands.; The gameplay cases ran against a simulated engine and the gate declares nativeProof=false. In-game attack cardinality, target acquisition and movement latency remain unmeasured on this candidate.

#### Summary

- **Delivered behavior:** The candidate carries ordinary melee intent into paired dispatch, separates real-time approach from strike readiness, and preserves actor and Standard-charge attribution on detailed and fallback outcomes. The gate confirms the Mounted Shield regression checks and embedded icon identity.
- **Scope reviewed:** Draft PR #65 at the fixed candidate, covering MMR-03, MMR-04, MMR-05 and offline acceptance S04, S05 and S08.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** N04 — Observe horse-owned attacks, weapons and spending in both initiative arrangements, including a spent or ended horse.; N05 — Observe idle real-time acquisition with two eligible enemies, explicit target retention, AI off, Stop and Hold.; N06 — Compare first movement and first strike against an equivalent unmounted actor while attack readiness is pending.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21840-f200-77c3-a8a0-79cd192d7c12`, duration 1041.8s, response sha256 `c519677dc5c0fb0a711de5e0d97bde2777a7493fbafbfe2f3148a87e04cd2ce7`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r07-612a49`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "7c6ec09036cc68efca48baf77238ac5c5028dab5",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the submitted commit. Click ownership and automatic target retirement still depend on which hook runs, and restricted melee selection can miss a legal target or select one the native brain would reject. Two S04 checks also leave required observations unresolved.\n\n## Continuation for Claude\n\nBind click ownership to the actor whose ManualTarget the click handler writes. Give the controller and wrapper the same terminal classification when an automatic target despawns and leaves perception together. For restricted melee acquisition, apply native target eligibility and stationary strike admission before comparing distances, including for the proposed target.\n\nIn the existing harness, use a legal horse routine that passes production plan validation. Drive the rider terminal from raw capability facts and measure submissions, deliveries and warnings across the whole transition. Preserve native action ownership, explicit targets, ranged behavior and the existing click router. The next evidence needed is the focused counterexamples below and the enrolled Fast gate on the corrected candidate, subject to the existing controller stop rules and owner limits. N04\u2013N06 remain owner playtest observations.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed HEAD and the merge base against the controller inputs. The mission SHA-256 matched, and git diff --check reported no whitespace errors.",
      "Verified the controller receipt and its log SHA-256, 6a4c8ef4380af0a32eb401d422b90783d2c158b2c12f9ce5f704735bcb736e6a. The required Fast gate ran at 7c6ec090, exited 0 in 54.2 seconds and recorded an unchanged worktree.",
      "The log records 1542 component passes, 153 source-validation passes and all 15 focused children passing. The attack-path harness reported 149/0, the lifecycle contract 76/0 and the attack-order assembly contract 4/0.",
      "S08 is supported by the compiled Mounted Shield contract at 114/0, selection rollback at 68/0 and matching packet, production and embedded icon SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and current handoffs, complete feature diff, round-7 delta, developer report, prior findings ledger, controller receipt and detailed gate output.",
      "The click handler patches and existing cannot-attack router, retained-order ownership, target ranking, wrapper retirement, terminal reporting, dispatch planner and production attack-plan validation.",
      "The harness source extraction, fixture constructors, modeled native commands, round-7 assertions and the measurements printed for each pending finding."
    ],
    "limitations": [
      "The reviewer used read-only inspection. Executed build and test results come from the candidate-bound controller receipt. The supplied transcript withholds the developer's other foreground commands.",
      "The gameplay cases ran against a simulated engine and the gate declares nativeProof=false. In-game attack cardinality, target acquisition and movement latency remain unmeasured on this candidate."
    ],
    "required_gaps": [
      "Observe click ownership through the actual assignment when command routing and the receiving actor differ.",
      "Demonstrate retargeting with a legal multi-attack horse plan, and capture the full rider-terminal-to-horse transition.",
      "Compare both observers on automatic despawn with simultaneous memory loss, and exercise both remaining restricted-target selection cases."
    ],
    "source_inspection_only": [
      "The click routing mismatch, competing automatic retirement paths and two target-ranking counterexamples were traced in production source. The gate did not exercise those combinations.",
      "Comparison of the harness constructors and plan validator with production established why the recorded two-attack plan would be rejected."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "UnitCommandRunPrefix marks every public Run on a pair actor while the click is open. An unrelated synchronous Run that writes no target can therefore release a skipped rider's ownership. The existing RouteRiderClickApproach also demonstrates the reverse mismatch. It can skip the rider's Run or submit the move on the mount, while OnClick still writes the rider's ManualTarget.",
      "blocking_rationale": "Retirement can preserve an expired pair target or clear a newer player assignment to the same target.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Ownership follows the click handler's actual per-actor assignment, including when the command is routed or its submission is refused.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPatchController.cs",
        "line": 1264
      },
      "priority": "P2",
      "requested_correction": "Observe the ManualTarget assignment at its call site in ClickUnitHandler.OnClick with the original actor, target and click identity. Preserve composition with the existing cannot-attack router.",
      "requested_evidence": "A skipped rider receiving an unrelated non-writing Run must have its old T cleared on retirement. A real later rider assignment to T must survive, including when the click's rider submission is consumed. Retain the companion, simulated and unhandled controls.",
      "title": "A command submission still stands in for the click's target assignment",
      "trigger": "A handled click on the order's target runs a command on a different actor from the one receiving the handler's target write, or another command runs synchronously during the click."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The fixture passes true for single-attack mode, including through the constructor used by the spliced dispatch. Its UnitAttack.Init nevertheless installs two attacks, and its CreateAndValidateChildAttack only increments a counter. Production rejects a single-attack command with two plan entries at MountedPairAttackCommand.cs line 2073 and also checks the exact mount weapon and slot.",
      "blocking_rationale": "The recorded 2/2 completion and one spend exercise the terminal branch using a plan the product would reject before its first strike.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The remaining attack on A and subsequent dispatch for B are observed on an ordinary horse command with a legally admitted multi-attack plan.",
      "id": "R4-F1",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 623
      },
      "priority": "P2",
      "requested_correction": "Forward the ordinary versus explicit single-attack mode, provide the matching natural weapon and slot, and exercise the production plan check. Make the modeled plan honor single-attack mode.",
      "requested_evidence": "Show a legal horse routine advance from 1/2 to 2/2 on A with one Standard spent. Then show one dispatch for valid B, zero for invalid B, and interruption on hard cancellation. An explicit single-attack control should deliver once.",
      "title": "The horse retarget test completes a two-attack plan in single-attack mode",
      "trigger": "Round7RemainingRoutine replaces the order after the horse's first modeled delivery."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The test now runs the production drive and terminal handler, but writes the classified branch and CantAct directly into RecordNativeTerminal. Its capability adapter also reports CanNotAttack for that state, an earlier native interruption condition in the windup case. The test samples rider submissions before the terminal and starts counting warnings after it. A warning emitted at the terminal is excluded from the reported total.",
      "blocking_rationale": "The recorded rows cannot establish the claimed native-branch attribution, absence of a second rider submission, or exactly-once warning across the transition.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The production recording path derives the terminal facts, and the test observes both actors' submissions, deliveries and all warnings through the recovered rider's next drive.",
      "id": "R4-F5",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 2472
      },
      "priority": "P2",
      "requested_correction": "Feed the modeled CanAct observation and separate condition facts through the existing production terminal hooks. Use the production EvaluateStockActor for participant selection. Measure submissions and warnings from before the terminal until after the next drive.",
      "requested_evidence": "For both pre-start force-finish and interrupted windup, record zero additional rider submissions after recovery. Show one eligible horse submission with delivery and zero warnings, or zero horse submissions and exactly one rider refusal for a spent or ended horse. Keep delivered and retired controls.",
      "title": "The terminal-to-dispatch test supplies the cause and misses part of the observation window",
      "trigger": "An unspent rider recovers between its capability terminal and the controller's next paired dispatch."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "TryDriveStockAttackIntent first calls RetireAutomaticIntentIfUnperceived. That path retires the wrapper with Cancelled before RecordTargetInvalidation is reached. The wrapper's own pre-start tick and OnStart check target invalidation first and record TargetInvalid with the despawn reason. The parity fixture uses an explicit order.",
      "blocking_rationale": "The same automatic target loss still produces different terminal information and rejection codes depending on update order.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Both observers record TargetInvalid and the bounded despawn reason for the unreleased wrapper, with no further movement or spend.",
      "id": "R6-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1898
      },
      "priority": "P2",
      "requested_correction": "Give both observers the same precedence when target invalidation and perception loss coincide. Preserve prompt retirement for a still-valid target that leaves perception and native completion for a delivered strike.",
      "requested_evidence": "Repeat the controller-first and wrapper-first comparison on an automatic order with IsInState false and the target absent from group memory. Compare cause, terminal reason, rejection code, movement and spending.",
      "title": "Automatic despawn can still be reported as ordinary cancellation",
      "trigger": "An automatic melee target despawns and leaves the rider group's memory before an installed wrapper delivers."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "Choose seeds best and bestDistance from the proposed target without reading its Eligible flag. Even after ResolveEqualRankTie marks that target ineligible, a farther equal-ranked candidate cannot beat its distance. ObserveAutomaticProposal then refuses the blocked target.",
      "blocking_rationale": "An idle held pair can repeatedly decline an available stationary attack.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Fresh automatic melee acquisition chooses the nearest eligible equal-ranked target when the native proposal fails stationary command sight.",
      "id": "R7-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedEngagementDiagnostic.cs",
        "line": 128
      },
      "priority": "P2",
      "requested_correction": "Use the proposed target as the distance to beat only when it is eligible. Preserve manual target priority, ranged behavior and retention of a valid current order.",
      "requested_evidence": "Reverse the round-7 fixture so native proposes the nearer blocked target and the farther equal-ranked target passes both sight checks. Observe one stationary delivery on the farther target. Keep the both-blocked refusal.",
      "title": "An ineligible native proposal prevents selection of a farther legal target",
      "trigger": "Restricted travel, no current or manual target, and two perceived equal-ranked enemies. Native proposes the nearer enemy using cached sight, but only the farther enemy passes command sight."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "For forbidden travel and melee, the conditional evaluates StationaryStrikeAdmissible in place of rider.HasLOS. The ranking reader supplies the brain's candidate list before action-specific validity is applied. A nearer candidate can therefore replace the legal proposal even when the native held-rider or unreachable-target sight check rejects it.",
      "blocking_rationale": "The tie-break can select an enemy outside the native action's eligible set.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An alternative satisfies the native brain restrictions applicable to that actor and target as well as the pair's stationary command geometry.",
      "id": "R7-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1571
      },
      "priority": "P2",
      "requested_correction": "Retain the native sight requirement where the rider's movement is forbidden or the candidate is marked unreachable, and apply stationary mounted-strike admission for restricted pair travel.",
      "requested_evidence": "Give a held rider two perceived, in-range candidates with clear command geometry. Set cached rider sight false only for the nearer alternative. The farther native proposal should receive the stationary strike. Retain the mount-only restriction and both-clear controls.",
      "title": "Stationary melee selection drops a native sight requirement",
      "trigger": "The rider is under a native movement restriction. A nearer remembered enemy has clear command geometry but fails the rider's cached sight test."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ResolveEqualRankTie constructs a mounted strike probe for each otherwise qualifying candidate under restricted travel before Choose discards candidates of unrelated ranks. ObserveAutomaticProposal probes the selected target again.",
      "blocking_rationale": "This is avoidable plan construction. No frame-time impact was measured.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Only targets that could affect the selection need a native attack-plan probe.",
      "id": "R7-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1565
      },
      "priority": "P3",
      "requested_correction": "Filter by comparable rank before constructing stationary strike probes, while preserving the dispatch-time recheck.",
      "requested_evidence": "If addressed alongside the selection fix, compare PlannedStrikeProbeCount for a proposal with several irrelevant ranks and retain the same selected target.",
      "title": "Restricted proposals build attack plans for candidates that cannot win the tie",
      "trigger": "The brain's ranked list contains several enemies outside the proposed target's comparison rank."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "The simple skipped-rider control passes. Public Run still identifies the command executor, which can differ from the actor written by the click handler. An unrelated Run inside the bracket can also set the flag."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F1",
      "rationale": "The harness advances the index through two act events and reaches production OnEnded. Its single-attack wrapper and two-entry plan fail production's start-time validation, so legal routine completion remains unverified."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F5",
      "rationale": "The fixture turn, command submission and terminal handler are now connected. The fixture still supplies the classified capability branch, and its rider-submission and warning counters omit opposite ends of the transition."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F6",
      "rationale": "AttachActorCharges populates the outcome selected for delivery. All six detailed and fallback rows observed that same object at LastOutcome, with one report and the expected actor and charge flags for an unstarted rider, delivered rider and delivered horse."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F3",
      "rationale": "Both cases now begin at ObserveAutomaticProposal. The 2.6 m order stayed pending during cooldown and delivered once after readiness with zero travel. The 3.2 m proposal was refused with zero travel and zero deliveries."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R6-F1",
      "rationale": "The explicit despawn case now emits matching TargetInvalid results. An automatic despawn that also removes the target from memory still takes the controller's earlier generic cancellation path."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R6-F2",
      "rationale": "For the reported farther legal proposal, the blocked nearer alternative is now excluded and the farther target receives the strike. With both command sight checks clear, the nearer target receives it. R7-F1 and R7-F2 identify different selection failures."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r07-612a49",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate carries ordinary melee intent into paired dispatch, separates real-time approach from strike readiness, and preserves actor and Standard-charge attribution on detailed and fallback outcomes. The gate confirms the Mounted Shield regression checks and embedded icon identity.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04 \u2014 Observe horse-owned attacks, weapons and spending in both initiative arrangements, including a spent or ended horse.",
      "N05 \u2014 Observe idle real-time acquisition with two eligible enemies, explicit target retention, AI off, Stop and Hold.",
      "N06 \u2014 Compare first movement and first strike against an equivalent unmounted actor while attack readiness is pending."
    ],
    "scope_reviewed": "Draft PR #65 at the fixed candidate, covering MMR-03, MMR-04, MMR-05 and offline acceptance S04, S05 and S08.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
