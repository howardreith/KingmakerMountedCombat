<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r06-review part=1/1 sha256=b56b37e4b82f17269eb0aed683cc6a8438e6f255018bbf8d6255c7d0e2fe35db -->
### Codex GPT-6.2 Astra — review — round 6 — candidate `e5735eb363`

**Decision:** `changes_required` — The focused gate passed on the submitted commit. Three source defects remain: a handled click can relinquish the rider’s target without assigning it, a held pair can discard a legal stationary target, and controller-first invalidation records the wrong terminal cause. The harness also leaves required parts of S04 and S05 unobserved.

## Continuation for Claude

Round six is the last recorded PR-1 allocation. Any further formal round requires owner authorization. If work continues, observe the click handler’s per-actor target write, apply stationary strike admission when choosing between native-ranked melee candidates, and carry the observed invalidation cause through exact-wrapper retirement. Preserve the corrected protection for a horse strike that has already delivered.

Use the existing offline harness to follow the production terminal outcome through the next controller dispatch and warning. Advance the horse’s planned attacks through completion, and drive a restricted automatic proposal from acceptance or refusal through its first act. Record actor, target, attack count and Standard expenditure at their outputs. Keep the changes within ordinary pair orders, preserve native action and movement authority, and run the enrolled focused gate on any authorized successor candidate. Native play observations remain with the owner.

#### Findings

**R1-F11** · P2 · defect · blocks readiness — A handled click can release a skipped rider’s target ownership
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1165`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: The rider already targets T and is selected during a real, handled click on T whose attack loop skips that rider.
- Expected: Only an actual later assignment to this actor transfers ownership of T.
- Actual: EndClickObservation releases ownership when the actor was selected, the overall click returned true and the final ManualTarget equals the clicked unit. Kingmaker can return true after skipping a selected unit in its attack loop, as documented in planning/MOUNTED-RANGED-COMBAT-CONTRACT.md line 19. An unchanged, pair-owned T satisfies all three conditions. Round6ClickAttribution assigns T in every case and omits a handled click that skips the rider.
- Why it blocks / does not: Retirement can leave the expired order’s target installed as a player override.
- Requested correction: Bind ownership transfer to the per-actor assignment in the click handler. Preserve the click identity that distinguishes the pair’s own write from a later same-target write.
- Requested evidence: With the rider selected and the order owning T, handle a click on T that skips the rider’s assignment, then observe retirement clear T. Repeat with an actual same-target rider write and observe T survive. Retain the simulated, unhandled and companion controls.

**R4-F1** · P2 · evidence_gap · blocks readiness — The retarget test sets Success before observing the rest of the horse routine
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:2007`; acceptance: MMR-03, S04
- Trigger: The rider replaces an order after the horse’s first delivery while the horse still owns its Standard command.
- Expected: A horse with attacks remaining after its first delivery completes those attacks on the original target before the replacement order dispatches.
- Actual: The test confirms zero interruptions through CancelCore and confirms the hard-cancellation control. It then calls FinishSuccess(), which sets Result and invokes OnEnded. The facade builds one attack and GetAttackIndex() always returns zero. The spliced production OnEnded therefore cannot take its completed-plan branch. The replacement-invalidation control uses the same shortcut.
- Why it blocks / does not: The measured interruption counts verify the cleanup correction. The claimed completion of the remaining legal attacks, expressly requested for S04, is still unobserved.
- Requested correction: Give the focused control a native-shaped multi-attack plan and advance its attack index and deliveries through the production terminal condition.
- Requested evidence: Observe the horse’s remaining legal attack on A, its completed transaction and one Standard expenditure. Then observe one dispatch for valid B or zero for invalid B. Keep the hard-cancellation control.

**R4-F5** · P2 · evidence_gap · blocks readiness — The harness still supplies parts of the terminal-to-dispatch result
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:1896`; acceptance: MMR-03, S04
- Trigger: A rider terminal is followed by the controller update that should dispatch the horse or settle the refusal.
- Expected: The production terminal and next dispatch determine the actor submitted and any warning from the current paired activation.
- Actual: PlanStockDispatch now runs the rider-ended check in every control. DriveOnce still submits the wrapper itself, bypassing the controller’s turn check, TryExecuteAction and admission result. The fixture begins its TB order with a null turn, supplies the capability cause and refusal text, and reports one completed attack for Success plus a rule even though its attack index is zero. That supplied count changes how the delivered control is classified.
- Why it blocks / does not: The rows can pass when the production drive would retire the fixture’s order before dispatch or when the production terminal would classify its delivery differently.
- Requested correction: Bind the fixture order to its real fixture turn and exercise the production transition through command submission or final refusal. Feed settlement the recorded terminal branch and actual attack progress.
- Requested evidence: For the force-finish and interrupted-windup cases, recover the unspent rider before the next drive. Capture both actors’ submissions and deliveries and the emitted warning for eligible, spent and ended horses. Run delivered and retired controls through the same entry.

**R4-F6** · P2 · evidence_gap · blocks readiness — The spending check reads a helper result instead of the emitted outcome
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:1966`; acceptance: MMR-03, S04
- Trigger: A force-finished or delivered command publishes its terminal outcome.
- Expected: The terminal outcome received by the controller identifies the actor and reports charges consistent with the observed act.
- Actual: Round6Charges calls DescribeStandardCharge from a replacement terminal callback. DescribeOutcome sets ActorId separately and copies the two flags into MountedPairAttackOutcome. The fallback in ReportTerminalOnce omits all three fields. The test can pass even if the outcome received by HandleCommandTerminal loses the attribution.
- Why it blocks / does not: The recorded comparison verifies the helper’s calculation. S04 still lacks the requested comparison at the reporting boundary.
- Requested correction: Capture the actual populated outcome at the terminal callback. A shared operation that assigns the three fields to that outcome can keep the offline test small and preserve attribution on the fallback path.
- Requested evidence: Compare the received ActorId, RiderStandardCharged and ActionStandardCharged with the act count or cooldown for an unstarted rider force-finish, a delivered rider attack and a delivered horse attack.

**R5-F3** · P2 · evidence_gap · blocks readiness — The held-melee test starts after proposal acceptance and stops at windup
- Location: `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs:2100`; acceptance: MMR-04, MMR-05, S05
- Trigger: A travel-restricted automatic melee proposal arrives before a wrapper exists.
- Expected: The restricted automatic proposal is accepted or refused at its real decision. A retained order waits through cooldown and delivers only after readiness.
- Actual: The shared geometry and no-wrapper retirement checks now distinguish 2.6 m from 3.2 m correctly. Round6StationaryAutomatic calls BeginOrder before checking either distance, so the outside control starts with an accepted order. Its positive assertion counts NativeAttackStarts. The case never schedules an act event or observes delivery.
- Why it blocks / does not: Initial proposal refusal and accepted-through-first-strike behavior remain required S05 observations.
- Requested correction: Start the controls at the production proposal decision, keep the Standard unavailable for a drive with no wrapper, then advance readiness and the first act.
- Requested evidence: At 2.6 m observe acceptance, retention, zero travel and one delivery after readiness. At 3.2 m observe refusal, zero travel and zero deliveries.

**R6-F1** · P2 · defect · blocks readiness — Controller-first target loss is reported as ordinary cancellation
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1269`; acceptance: MMR-03, MMR-04, S04, S05
- Trigger: The controller observes an invalid target before the installed, unreleased wrapper’s next tick.
- Expected: An unreleased wrapper stopped because its target became invalid records TargetInvalid and the applicable target reason.
- Actual: RetireOrderOnTargetInvalidation now interrupts the installed wrapper through RetireOrderWrapper, whose interrupt branches record Cancelled. DescribeTerminalFailure consequently sets CommandCancelled. When the wrapper observes the same loss first, TryEndExpectedTargetInvalidation records TargetInvalid and a target-specific terminal reason.
- Why it blocks / does not: Update order now determines whether the outcome identifies target loss or reports a generic cancellation. This regresses the existing invalidation and feedback contract.
- Requested correction: Carry the observed target-invalidation cause and bounded reason through retirement of the exact wrapper. Preserve the delivered command’s native terminal handling.
- Requested evidence: Run controller-first and wrapper-first target loss before delivery. Compare the emitted cause, terminal reason and rejection code, with zero spending or further movement in both cases.

**R6-F2** · P2 · defect · blocks readiness — A blocked nearer candidate can displace a legal stationary attack
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:1458`; acceptance: MMR-04, S05
- Trigger: Native proposes a legal target for a held pair and an equal-ranked, nearer candidate passes cached sight but fails the strike’s direct sight check.
- Expected: A nearer candidate replaces a legal native proposal only if the pair can perform the stationary melee strike against that candidate.
- Actual: ResolveEqualRankTie permits a nearer candidate using cached rider.HasLOS. ObserveAutomaticProposal then evaluates StationaryStrikeAdmissible only for that winner. If the nearer target is blocked by command sight, the whole proposal is refused and the original legal target is lost. The repository documents cached sight admitting a target that direct command sight rejects in planning/MOUNTED-RANGED-COMBAT-CONTRACT.md line 63.
- Why it blocks / does not: A held pair can repeatedly refuse an attack while a perceived hostile is legally attackable from its current position.
- Requested correction: Use stationary mounted-strike admission when considering melee alternatives under restricted travel. Keep the comparison within the existing native-ranked candidates and preserve manual and ranged target selection.
- Requested evidence: Run a restricted proposal with two equal-ranked perceived targets. Give the nearer one cached sight but blocked command sight, and the farther proposed target clear command sight. Observe selection and stationary delivery on the farther target. With both clear, observe selection of the nearer one.

#### Prior finding assessments

- **R1-F11** — `fix_rejected` — The simulation and false-return guards cover two cases. A true return still includes native paths that skip a selected actor, and the postfix has no evidence of that actor’s assignment.
- **R4-F1** — `fix_rejected` — The mount-wide interrupt defect is corrected. The exercised CancelCore leaves the delivered IsMoveUnit horse command running and hard cancellation ends it. The remaining obligation under this ID is evidence of the horse finishing its legal routine.
- **R4-F5** — `fix_rejected` — The production planner now sees the recovered rider and the retired-order control. The test still supplies terminal facts, warning text and command submission outside the production drive.
- **R4-F6** — `fix_rejected` — All three helper results agree with the facade’s act counts. The callback never receives the production outcome whose actor and charge fields the finding asks to verify.
- **R5-F1** — `fix_verified` — Observed invalidation now retires the generation’s exact installed or queued wrapper. The waiting and first-windup controls restore eligibility before ticking and observe zero further carriers, acts or charges. R6-F1 concerns the different issue of how this termination is reported.
- **R5-F2** — `fix_verified` — Invalidating B while A occupies the active slot now clears B and retires only B’s wrapper. The control observes A still running, zero KMC or move interruptions and zero submissions for B. The shared limitation on evidence of A’s subsequent attack sequence is recorded under R4-F1.
- **R5-F3** — `fix_rejected` — The single-target reach calculation is corrected at both production call sites. The harness exercises retirement after admission and observes a start. It bypasses the requested proposal decision and first delivery.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and instructions, current handoffs, developer report, prior findings ledger, 6,826-line feature diff and controller-bound receipt and log.; The round-six changes since 390f1093 and their consumers in click routing, automatic target selection, paired dispatch, exact-wrapper retirement, terminal reporting and the attack-path harness.; The repository’s installed Kingmaker click and sight contracts, including the handled-but-skipped selection case and the distinction between cached sight and command sight.
- **Executed verification:** Verified the snapshot HEAD, mission SHA-256 and gate-log SHA-256. The log matched the controller receipt at 906b0b3408ee10863c4b787cfd76194e9308708470f65c3ebb60731226db7959.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 on e5735eb36302ef76582ff4a6a38ea9a311b361f6. It exited 0 in 40.3 seconds with 15 of 15 focused children passing. Components passed 1542/0, source validation 153/0, orders contracts 4/0, lifecycle contracts 73/0 and the attack-path harness 124/0.; The same log confirms the Mounted Shield contract at 114/0, selection rollback at 68/0 and identical packet, production and embedded icon bytes. Presentation, Archery, Ranger/Riding Dog, replay, localization and loader checks also passed. The product DLL SHA-256 was 62b920b37ce282eaabc70ae487844624d4cc63def45420867b145113cdf40ba9.; The feature diff passed git diff --check. The six files designated as whole-file extractions had no differences between 46444b30 and the extraction commit 95c26e59.
- **Source inspection only:** The handled-click ownership loss, stationary tie-break failure and inconsistent invalidation cause follow from the candidate’s production paths. The gate did not execute those counterexamples.; The normal DescribeOutcome initializer reads the corrected charge flags. The harness captures a separate helper result before the production outcome is emitted.
- **Required gaps:** An observed per-actor click assignment and skipped-assignment control, followed by retirement.; A production terminal-to-dispatch transition with actual submitted commands, emitted spending fields and warning text, plus progression of a surviving horse routine through its remaining legal attacks.; Restricted automatic proposal admission through first delivery, including a legal stationary alternative when the nearer candidate is obstructed.; Matching target-invalidation cause and feedback for controller-first and wrapper-first observation.
- **Limitations:** The review used read-only inspection. The developer transcript points to the controller receipt and withholds the mixed-session log. The candidate gate is independently attributable to H. The intermediate direct runs are developer-reported.; The focused gate reports nativeProof=false. No current game execution, remaining horse attack sequence or unmounted latency measurement was observed.

#### Summary

- **Delivered behavior:** The candidate preserves a delivered horse command across ordinary retarget cleanup, retires installed unreleased wrappers on observed invalidation, and isolates an invalid replacement from the preceding command. The focused Shield and compatibility checks passed. S04 and S05 still have blocking defects and evidence gaps.
- **Scope reviewed:** Final PR-1 checkpoint for MMR-03, MMR-04, MMR-05 and S04/S05/S08, including all seven pending findings and the consumers affected by the round-six corrections.
- **Pending later-phase work:** none
- **Unresolved questions:** Further formal review beyond the recorded six-round allocation requires an owner decision.
- **Remaining owner review:** N04: Confirm horse-owned attacks, weapons and legal cardinality under both initiative arrangements, including the spent or ended horse control.; N05: Observe real-time target acquisition and retention with two hostiles, AI off, Stop and Hold.; N06: Compare movement onset and first attack against an equivalent unmounted actor with matched readiness.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a2166a-eb8e-79f0-967f-d49873de2d88`, duration 842.8s, response sha256 `84b1c38339681cf10aade05d1428a3f972203b890502777df9383115562becac`, request `kmc-codex-mmr-attack-orders-20261125103400-6cef-r06-210a1e`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "e5735eb36302ef76582ff4a6a38ea9a311b361f6",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the submitted commit. Three source defects remain: a handled click can relinquish the rider\u2019s target without assigning it, a held pair can discard a legal stationary target, and controller-first invalidation records the wrong terminal cause. The harness also leaves required parts of S04 and S05 unobserved.\n\n## Continuation for Claude\n\nRound six is the last recorded PR-1 allocation. Any further formal round requires owner authorization. If work continues, observe the click handler\u2019s per-actor target write, apply stationary strike admission when choosing between native-ranked melee candidates, and carry the observed invalidation cause through exact-wrapper retirement. Preserve the corrected protection for a horse strike that has already delivered.\n\nUse the existing offline harness to follow the production terminal outcome through the next controller dispatch and warning. Advance the horse\u2019s planned attacks through completion, and drive a restricted automatic proposal from acceptance or refusal through its first act. Record actor, target, attack count and Standard expenditure at their outputs. Keep the changes within ordinary pair orders, preserve native action and movement authority, and run the enrolled focused gate on any authorized successor candidate. Native play observations remain with the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD, mission SHA-256 and gate-log SHA-256. The log matched the controller receipt at 906b0b3408ee10863c4b787cfd76194e9308708470f65c3ebb60731226db7959.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 on e5735eb36302ef76582ff4a6a38ea9a311b361f6. It exited 0 in 40.3 seconds with 15 of 15 focused children passing. Components passed 1542/0, source validation 153/0, orders contracts 4/0, lifecycle contracts 73/0 and the attack-path harness 124/0.",
      "The same log confirms the Mounted Shield contract at 114/0, selection rollback at 68/0 and identical packet, production and embedded icon bytes. Presentation, Archery, Ranger/Riding Dog, replay, localization and loader checks also passed. The product DLL SHA-256 was 62b920b37ce282eaabc70ae487844624d4cc63def45420867b145113cdf40ba9.",
      "The feature diff passed git diff --check. The six files designated as whole-file extractions had no differences between 46444b30 and the extraction commit 95c26e59."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and instructions, current handoffs, developer report, prior findings ledger, 6,826-line feature diff and controller-bound receipt and log.",
      "The round-six changes since 390f1093 and their consumers in click routing, automatic target selection, paired dispatch, exact-wrapper retirement, terminal reporting and the attack-path harness.",
      "The repository\u2019s installed Kingmaker click and sight contracts, including the handled-but-skipped selection case and the distinction between cached sight and command sight."
    ],
    "limitations": [
      "The review used read-only inspection. The developer transcript points to the controller receipt and withholds the mixed-session log. The candidate gate is independently attributable to H. The intermediate direct runs are developer-reported.",
      "The focused gate reports nativeProof=false. No current game execution, remaining horse attack sequence or unmounted latency measurement was observed."
    ],
    "required_gaps": [
      "An observed per-actor click assignment and skipped-assignment control, followed by retirement.",
      "A production terminal-to-dispatch transition with actual submitted commands, emitted spending fields and warning text, plus progression of a surviving horse routine through its remaining legal attacks.",
      "Restricted automatic proposal admission through first delivery, including a legal stationary alternative when the nearer candidate is obstructed.",
      "Matching target-invalidation cause and feedback for controller-first and wrapper-first observation."
    ],
    "source_inspection_only": [
      "The handled-click ownership loss, stationary tie-break failure and inconsistent invalidation cause follow from the candidate\u2019s production paths. The gate did not execute those counterexamples.",
      "The normal DescribeOutcome initializer reads the corrected charge flags. The harness captures a separate helper result before the production outcome is emitted."
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
      "actual": "EndClickObservation releases ownership when the actor was selected, the overall click returned true and the final ManualTarget equals the clicked unit. Kingmaker can return true after skipping a selected unit in its attack loop, as documented in planning/MOUNTED-RANGED-COMBAT-CONTRACT.md line 19. An unchanged, pair-owned T satisfies all three conditions. Round6ClickAttribution assigns T in every case and omits a handled click that skips the rider.",
      "blocking_rationale": "Retirement can leave the expired order\u2019s target installed as a player override.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only an actual later assignment to this actor transfers ownership of T.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1165
      },
      "priority": "P2",
      "requested_correction": "Bind ownership transfer to the per-actor assignment in the click handler. Preserve the click identity that distinguishes the pair\u2019s own write from a later same-target write.",
      "requested_evidence": "With the rider selected and the order owning T, handle a click on T that skips the rider\u2019s assignment, then observe retirement clear T. Repeat with an actual same-target rider write and observe T survive. Retain the simulated, unhandled and companion controls.",
      "title": "A handled click can release a skipped rider\u2019s target ownership",
      "trigger": "The rider already targets T and is selected during a real, handled click on T whose attack loop skips that rider."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "The test confirms zero interruptions through CancelCore and confirms the hard-cancellation control. It then calls FinishSuccess(), which sets Result and invokes OnEnded. The facade builds one attack and GetAttackIndex() always returns zero. The spliced production OnEnded therefore cannot take its completed-plan branch. The replacement-invalidation control uses the same shortcut.",
      "blocking_rationale": "The measured interruption counts verify the cleanup correction. The claimed completion of the remaining legal attacks, expressly requested for S04, is still unobserved.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A horse with attacks remaining after its first delivery completes those attacks on the original target before the replacement order dispatches.",
      "id": "R4-F1",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 2007
      },
      "priority": "P2",
      "requested_correction": "Give the focused control a native-shaped multi-attack plan and advance its attack index and deliveries through the production terminal condition.",
      "requested_evidence": "Observe the horse\u2019s remaining legal attack on A, its completed transaction and one Standard expenditure. Then observe one dispatch for valid B or zero for invalid B. Keep the hard-cancellation control.",
      "title": "The retarget test sets Success before observing the rest of the horse routine",
      "trigger": "The rider replaces an order after the horse\u2019s first delivery while the horse still owns its Standard command."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "PlanStockDispatch now runs the rider-ended check in every control. DriveOnce still submits the wrapper itself, bypassing the controller\u2019s turn check, TryExecuteAction and admission result. The fixture begins its TB order with a null turn, supplies the capability cause and refusal text, and reports one completed attack for Success plus a rule even though its attack index is zero. That supplied count changes how the delivered control is classified.",
      "blocking_rationale": "The rows can pass when the production drive would retire the fixture\u2019s order before dispatch or when the production terminal would classify its delivery differently.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The production terminal and next dispatch determine the actor submitted and any warning from the current paired activation.",
      "id": "R4-F5",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 1896
      },
      "priority": "P2",
      "requested_correction": "Bind the fixture order to its real fixture turn and exercise the production transition through command submission or final refusal. Feed settlement the recorded terminal branch and actual attack progress.",
      "requested_evidence": "For the force-finish and interrupted-windup cases, recover the unspent rider before the next drive. Capture both actors\u2019 submissions and deliveries and the emitted warning for eligible, spent and ended horses. Run delivered and retired controls through the same entry.",
      "title": "The harness still supplies parts of the terminal-to-dispatch result",
      "trigger": "A rider terminal is followed by the controller update that should dispatch the horse or settle the refusal."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "S04"
      ],
      "actual": "Round6Charges calls DescribeStandardCharge from a replacement terminal callback. DescribeOutcome sets ActorId separately and copies the two flags into MountedPairAttackOutcome. The fallback in ReportTerminalOnce omits all three fields. The test can pass even if the outcome received by HandleCommandTerminal loses the attribution.",
      "blocking_rationale": "The recorded comparison verifies the helper\u2019s calculation. S04 still lacks the requested comparison at the reporting boundary.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The terminal outcome received by the controller identifies the actor and reports charges consistent with the observed act.",
      "id": "R4-F6",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 1966
      },
      "priority": "P2",
      "requested_correction": "Capture the actual populated outcome at the terminal callback. A shared operation that assigns the three fields to that outcome can keep the offline test small and preserve attribution on the fallback path.",
      "requested_evidence": "Compare the received ActorId, RiderStandardCharged and ActionStandardCharged with the act count or cooldown for an unstarted rider force-finish, a delivered rider attack and a delivered horse attack.",
      "title": "The spending check reads a helper result instead of the emitted outcome",
      "trigger": "A force-finished or delivered command publishes its terminal outcome."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "MMR-05",
        "S05"
      ],
      "actual": "The shared geometry and no-wrapper retirement checks now distinguish 2.6 m from 3.2 m correctly. Round6StationaryAutomatic calls BeginOrder before checking either distance, so the outside control starts with an accepted order. Its positive assertion counts NativeAttackStarts. The case never schedules an act event or observes delivery.",
      "blocking_rationale": "Initial proposal refusal and accepted-through-first-strike behavior remain required S05 observations.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The restricted automatic proposal is accepted or refused at its real decision. A retained order waits through cooldown and delivers only after readiness.",
      "id": "R5-F3",
      "location": {
        "file": "tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs",
        "line": 2100
      },
      "priority": "P2",
      "requested_correction": "Start the controls at the production proposal decision, keep the Standard unavailable for a drive with no wrapper, then advance readiness and the first act.",
      "requested_evidence": "At 2.6 m observe acceptance, retention, zero travel and one delivery after readiness. At 3.2 m observe refusal, zero travel and zero deliveries.",
      "title": "The held-melee test starts after proposal acceptance and stops at windup",
      "trigger": "A travel-restricted automatic melee proposal arrives before a wrapper exists."
    },
    {
      "acceptance_ids": [
        "MMR-03",
        "MMR-04",
        "S04",
        "S05"
      ],
      "actual": "RetireOrderOnTargetInvalidation now interrupts the installed wrapper through RetireOrderWrapper, whose interrupt branches record Cancelled. DescribeTerminalFailure consequently sets CommandCancelled. When the wrapper observes the same loss first, TryEndExpectedTargetInvalidation records TargetInvalid and a target-specific terminal reason.",
      "blocking_rationale": "Update order now determines whether the outcome identifies target loss or reports a generic cancellation. This regresses the existing invalidation and feedback contract.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An unreleased wrapper stopped because its target became invalid records TargetInvalid and the applicable target reason.",
      "id": "R6-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1269
      },
      "priority": "P2",
      "requested_correction": "Carry the observed target-invalidation cause and bounded reason through retirement of the exact wrapper. Preserve the delivered command\u2019s native terminal handling.",
      "requested_evidence": "Run controller-first and wrapper-first target loss before delivery. Compare the emitted cause, terminal reason and rejection code, with zero spending or further movement in both cases.",
      "title": "Controller-first target loss is reported as ordinary cancellation",
      "trigger": "The controller observes an invalid target before the installed, unreleased wrapper\u2019s next tick."
    },
    {
      "acceptance_ids": [
        "MMR-04",
        "S05"
      ],
      "actual": "ResolveEqualRankTie permits a nearer candidate using cached rider.HasLOS. ObserveAutomaticProposal then evaluates StationaryStrikeAdmissible only for that winner. If the nearer target is blocked by command sight, the whole proposal is refused and the original legal target is lost. The repository documents cached sight admitting a target that direct command sight rejects in planning/MOUNTED-RANGED-COMBAT-CONTRACT.md line 63.",
      "blocking_rationale": "A held pair can repeatedly refuse an attack while a perceived hostile is legally attackable from its current position.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A nearer candidate replaces a legal native proposal only if the pair can perform the stationary melee strike against that candidate.",
      "id": "R6-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 1458
      },
      "priority": "P2",
      "requested_correction": "Use stationary mounted-strike admission when considering melee alternatives under restricted travel. Keep the comparison within the existing native-ranked candidates and preserve manual and ranged target selection.",
      "requested_evidence": "Run a restricted proposal with two equal-ranked perceived targets. Give the nearer one cached sight but blocked command sight, and the farther proposed target clear command sight. Observe selection and stationary delivery on the farther target. With both clear, observe selection of the nearer one.",
      "title": "A blocked nearer candidate can displace a legal stationary attack",
      "trigger": "Native proposes a legal target for a held pair and an equal-ranked, nearer candidate passes cached sight but fails the strike\u2019s direct sight check."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "The simulation and false-return guards cover two cases. A true return still includes native paths that skip a selected actor, and the postfix has no evidence of that actor\u2019s assignment."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F1",
      "rationale": "The mount-wide interrupt defect is corrected. The exercised CancelCore leaves the delivered IsMoveUnit horse command running and hard cancellation ends it. The remaining obligation under this ID is evidence of the horse finishing its legal routine."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F5",
      "rationale": "The production planner now sees the recovered rider and the retired-order control. The test still supplies terminal facts, warning text and command submission outside the production drive."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F6",
      "rationale": "All three helper results agree with the facade\u2019s act counts. The callback never receives the production outcome whose actor and charge fields the finding asks to verify."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F1",
      "rationale": "Observed invalidation now retires the generation\u2019s exact installed or queued wrapper. The waiting and first-windup controls restore eligibility before ticking and observe zero further carriers, acts or charges. R6-F1 concerns the different issue of how this termination is reported."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F2",
      "rationale": "Invalidating B while A occupies the active slot now clears B and retires only B\u2019s wrapper. The control observes A still running, zero KMC or move interruptions and zero submissions for B. The shared limitation on evidence of A\u2019s subsequent attack sequence is recorded under R4-F1."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F3",
      "rationale": "The single-target reach calculation is corrected at both production call sites. The harness exercises retirement after admission and observes a start. It bypasses the requested proposal decision and first delivery."
    }
  ],
  "request_id": "kmc-codex-mmr-attack-orders-20261125103400-6cef-r06-210a1e",
  "round": 6,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate preserves a delivered horse command across ordinary retarget cleanup, retires installed unreleased wrappers on observed invalidation, and isolates an invalid replacement from the preceding command. The focused Shield and compatibility checks passed. S04 and S05 still have blocking defects and evidence gaps.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N04: Confirm horse-owned attacks, weapons and legal cardinality under both initiative arrangements, including the spent or ended horse control.",
      "N05: Observe real-time target acquisition and retention with two hostiles, AI off, Stop and Hold.",
      "N06: Compare movement onset and first attack against an equivalent unmounted actor with matched readiness."
    ],
    "scope_reviewed": "Final PR-1 checkpoint for MMR-03, MMR-04, MMR-05 and S04/S05/S08, including all seven pending findings and the consumers affected by the round-six corrections.",
    "unresolved_questions": [
      "Further formal review beyond the recorded six-round allocation requires an owner decision."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
