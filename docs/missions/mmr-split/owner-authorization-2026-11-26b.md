Continue the existing Mounted Melee Repair program with the bounded authorizations below. Preserve the full original functionality, acceptance criteria, PRs, enrollments and history.

## 1. Correct the status report first

The final handoff misclassified priority as blocking status.

The latest accepted review results contain:

- PR #65 at e5735eb3: 7 blocking findings, 0 optional.
- PR #66 at b36f08a7: 4 blocking findings, 0 optional.
- PR #67 at 97dce9a9: 7 blocking findings, 2 optional.
- PR #68 at 642cb481: 1 blocking finding, 0 optional.

Total: 19 blockers and 2 optional items. The blockers comprise 12 defect findings and 7 evidence gaps.

Count readiness from `blocks_readiness`, retaining `priority` and `category` separately. P2 does not mean non-blocking.

Correct the current handoff and append an explicit correction to the supplemental ledger. Preserve the original records and review JSON. Do not edit reviewer flags, controller findings or historical receipts.

Use PR-qualified finding IDs in the consolidated report. No new reporting framework is necessary.

## 2. Authorized review limits and unchanged validation budget

I authorize these total review ceilings:

| Child | PR | Total ceiling |
| --- | --- | --- |
| PR-1 attack orders | #65 | 8 |
| PR-2 turn-based approach | not yet enrolled | 3, unchanged |
| PR-3 seating | #68 | 5 |
| PR-4 Trip | #66 | 4 |
| PR-5 bite | #67 | 4 |
| PR-6 integration | not yet enrolled | 2, unchanged |

This adds eight formal reviews across the four existing capped children. The program ceilings become 26 child reviews and 33 lifetime reviews, including PR #64’s seven historical reviews.

Preserve the 20 lifetime reviews already spent. Thirteen remain available under the new allocation: eight newly authorized and five already reserved for PR-2 and PR-6.

Do not redistribute these allocations or create replacement enrollments.

The validation allowance remains 150 minutes under:

`mounted-alpha2-melee-pose-20261124`

Preserve the actual cumulative spend, reconciliation entries and budget ID. Read and report the current balance. No further minute increase is authorized, and no historical accounting reconstruction is needed absent a concrete discrepancy.

## 3. Sequence and supported continuation

Work in this order:

1. PR #68 seating.
2. PR #66 Trip.
3. PR #67 bite.
4. PR #65 attack orders.
5. PR-2 against PR-1’s accepted source.
6. PR-6 after all required feature acceptances.

Keep only one child actively developing/reviewing at a time. Resume each enrollment when selecting it for work, rather than activating all four immediately.

Use the supported `resume --max-rounds` operation with these existing enrollments:

- #68: `kmc-codex-mmr-horse-seat-20261126011011-d3c5`, total cap 5.
- #66: `kmc-codex-mmr-horse-trip-20261125213249-4b8b`, total cap 4.
- #67: `kmc-codex-mmr-horse-bite-20261125221703-5f10`, total cap 4.
- #65: `kmc-codex-mmr-attack-orders-20261125103400-6cef`, total cap 8.

Record this explicit owner authorization in the reason and supplemental program accounting. Verify resulting limits and status.

Preserve the controller’s normal no-progress handling. An extension from a round-cap stop does not erase the existing finding streaks. If a new stop occurs, preserve it; do not automatically resume it, replace the enrollment or manually reset counters.

Save this instruction verbatim as an ordinary tracked authorization/approach record outside the controller transcript directory, and reference it in the developer report and relevant dispositions. These are foreground enrollments; do not invoke the headless-only `--guidance` route or invent report fields.

Scope and acceptance remain unchanged. Use the explicit budget operation rather than rewriting the pinned mission to change its limits.

## 4. Close the seat evidence gap first

PR #68 has one remaining blocker: R1-F1.

The current predicate may already be correct. Produce a concise, version-bound trace through the installed Kingmaker 2.1.7b stationary humanoid animation path establishing:

1. The branch selecting the stationary clip and the registration call it takes.
2. The concrete animation/composition created.
3. That the same object is placed in ActiveAnimations and assigned to the locomotion handle’s ActiveAnimation.
4. The concrete GetWeight implementation used by that object and the native value it returns.
5. The corresponding weight initialization and transition writes, and their relationship to the registered animation contribution.

Record the assembly and method identities and the relevant operands. An annotated trace or narrowly focused contract check is sufficient. Do not build a general IL-analysis framework.

Call-presence assertions alone do not establish this relationship.

If the trace substantiates the current predicate, retain it. If it reveals a mismatch, correct only the contradicted condition and corresponding fixture assumptions.

Keep the existing long-bind-to-stationary-idle and remount cases, recovery behavior, restoration and profile isolation. Do not add another broad fixture matrix or substitute another timer or bone-motion heuristic.

Describe the condition accurately: the current code requires native idle eligibility, a positively weighted current locomotion composition, and no outgoing released non-additive contribution. It does not require every transition-in to have finished.

Run the required focused gate and submit the resulting candidate. Aim to close this in the next review; reserve the second authorized round for a concrete correction.

Rendered seating and native save/load remain owner observations.

## 5. Repair the Trip transaction coherently

PR #66 blockers are R1-F1, R1-F2, R1-F4 and R2-F1.

Address them together within the existing repair service:

- Recognize the actual observer-only guarded launch, including `-kmcObserverRequest` with a null KMC automation host. Do not poll the player’s repair folder from that run unless a verified run-owned folder has been selected.
- Capture the relevant feature identities before native removal and classify the actual post-state on both normal return and exception.
- Distinguish exact removal, verified unchanged, and inconsistent or unreadable results.
- Publish Applied only after exact removal is established.
- Preserve the existing retry/supersession behavior for a verified no-change result.
- Keep inconsistent or unverifiable outcomes terminal so reporting retries cannot repeat removal.
- Keep archive finalization bound to the exact request bytes that received the decision. A replacement claim must not receive another request’s terminal suffix.

Exercise the reported boundaries in the existing real-file tests:

- Observer-only arguments with seeded player-folder files.
- Native removal changing the target and a neighbor, then throwing before adapter verification.
- Exact target-only removal followed by an exception.
- Removal returning normally with no change.
- Claim replacement during publication after the initial read.

Place failures at the actual affected boundary. An exception injected only after integrity verification cannot test an exception that skips that verification.

Preserve fresh-horse filtering, explicit owner-requested repair, foreign effects and the existing safety guards. No new persistence framework is required.

## 6. Repair bite behavior and make observation consistent

PR #67 blockers are R2-F1, R2-F2, R1-F4, R2-F3, R2-F4, R2-F5 and R1-F9.

First fix the native-delegation regression:

- Contain plan acquisition, limb classification and the complete diagnostic observation.
- A diagnostic failure must not prevent the single legitimate native OnAction delegation.
- Preserve an explicit unreadable observation where appropriate.
- Do not swallow native combat failures or retry native damage as part of diagnostic recovery.
- Include a previous observed Bite in the failure case so stale evidence cannot pass as the current observation.

Recognize the physical Bite by its registered weapon identity and actual slot. A missing visual tag must not disable a valid fallback. Require the unique appropriate native Bite action, preserving rejection of foreign weapons, hoof-only candidates and ambiguous actions.

Use consistent strike evidence:

- Distinguish historical handle registration from the handle actually observed at the Bite.
- Make the structured terminal outcome agree with the observed strike.
- Preserve the Bite disposition through both detailed and minimal terminal paths.
- Continue independent playback reads when unrelated handle metadata fails.
- Keep a positive Bite observation distinguishable from subsequent hoof strikes.

Correct the metadata-fault test so it demonstrably reaches the intended metadata read after validation. Assert the intended failure and resulting state rather than accepting any exception.

For R1-F9, I explicitly authorize one separately metered verification of the relevant existing checks on frozen extraction commit b1c82f31 in an isolated worktree using the supported validation path. Record the exact source and test inputs and the new run’s real date and identity. Preserve the original log; do not backdate evidence or claim the new run proves the identity of the earlier pre-commit tree.

R2-F6 and R2-F7 are optional. They may remain documented and must not consume a separate review cycle.

Visible Bite playback remains owner observation N08.

## 7. Close the actual attack-order behavior and evidence

PR #65 blockers are R1-F11, R4-F1, R4-F5, R4-F6, R5-F3, R6-F1 and R6-F2.

Correct the three source defects:

- Observe the click handler’s actual per-actor target assignment. An overall handled click, actor selection and unchanged target value do not prove that actor received a write.
- Under restricted travel, test candidate alternatives using the actual stationary mounted-strike admission. A nearer candidate with cached visibility but blocked command sight must not displace a legal native proposal.
- Carry target-invalidation cause and reason through exact-wrapper retirement so controller-first and wrapper-first observation produce consistent terminal information. Preserve the handling of an already delivered native action.

Then close the four evidence gaps by exercising the missing production operations:

### Remaining horse routine — R4-F1

Use a legal multi-attack plan and advance its attack index and deliveries through the production completion condition.

Observe the remaining attack on the original target, transaction completion and one Standard expenditure before the replacement order dispatches.

Calling FinishSuccess directly does not demonstrate completion of the remaining routine. Retain valid/invalid replacement-target and hard-cancellation controls.

### Terminal to dispatch — R4-F5

Bind the order to the actual fixture turn and run the production transition through admission and command submission or final refusal.

Do not let a test helper construct and submit the wrapper in place of the production dispatch path being verified.

Use recorded native terminal facts and actual attack progress. Capture both actors’ submissions, deliveries and emitted warning. Preserve the recovered-rider, eligible/spent/ended-horse, delivered and retired controls.

### Emitted expenditure — R4-F6

Capture the actual terminal outcome received by the controller, including ActorId, RiderStandardCharged and ActionStandardCharged.

Cover detailed and fallback outcome construction. Reading the calculation helper’s result does not establish that the real outcome carries those fields.

### Restricted automatic proposal — R5-F3

Start at the production proposal acceptance/refusal decision, with no already-accepted order supplied by the fixture.

Keep Standard unavailable for the waiting control, then advance readiness and first delivery.

Observe legal acceptance and one stationary delivery at the in-range case; refusal and no delivery at the out-of-range case. Preserve native authority over movement, attacks and spending.

Use the existing harness and the smallest necessary production seams. Model the relevant external engine behavior accurately, but do not clone the production decision logic into a test implementation or build a new general testing platform.

## 8. Submission discipline and remaining program

Before each next review, map every existing blocker to its correction and the operation or output actually observed by its evidence.

Do not submit another claim of completion for a known missing operation merely because a helper test passes. Distinguish source reasoning, native-binding evidence, simulated engine behavior and live gameplay.

Run the required focused gate on the exact candidate. Meter qualifying work and avoid repeated unrelated campaigns.

Once a child is accepted, finalize it through the normal controller workflow and record its reviewed source and archive separately. Continue to the next authorized child.

Refresh PR-2 against PR-1’s actual accepted source and shared-file ownership. Preserve the approved source-delta integration procedure, including exclusion of foreign controller transcript changes and review of shared-file composition.

Known source defects and required offline evidence gaps remain blockers. Native visual/gameplay and save/load observations remain pending under the existing private-alpha milestone.

Do not merge, tag, release or permanently install.

The final report must derive blocking counts from the structured reviews, identify accepted children and exact source/archives, preserve cumulative review and validation accounting, and state precisely what remains for owner playtesting.
