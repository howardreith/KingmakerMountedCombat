Continue the existing Mounted Combat remediation program with full MMR-01 through MMR-07 functionality.

This is a new owner instruction following FINAL-HANDOFF-2026-11-27d.md. It authorizes implementation and the bounded workflow operations below. Do not stop after producing another plan.

The requested deliverable remains the complete integrated private alpha after its required source review. A four-child alpha omitting the turn-based approach changes does not satisfy this instruction.

## 1. Preserve accepted work and record this authority

Save this instruction verbatim as an ordinary tracked owner-authorization record outside the controller transcript directory. Reference it in the relevant developer reports and dispositions.

Preserve these accepted source checkpoints and their archives:

| Child | Reviewed source H | Archive A |
|---|---|---|
| PR-1 / #65 attack orders | aa15e1f2 | f7edd298 |
| PR-3 / #68 seating | 8bfe701d | 54311f1d |
| PR-4 / #66 Trip | d615d1d5 | 78c9bae4 |
| PR-5 / #67 Bite | 1253836a | dc5ef19d |

Use the full identities from their existing acceptance records when composing and recording provenance. These checkpoints are authorized for integration; do not request another approval merely to use them. Do not reopen their deferred optional findings.

Preserve the accepted transport repair and its existing activation scope. Do not repeat its review or full test suite without a concrete new defect requiring that work.

Retain the existing accounting and provenance corrections. Do not reopen settled historical authorization or accounting discussions without a concrete discrepancy.

## 2. Explicitly resume the current PR-2 stop

I acknowledge and authorize lifting this specific current stop:

- PR-2 / PR #70.
- Enrollment: kmc-codex-mmr-tb-approach-20261126213805-50cc.
- Reviewed candidate: 41a785095fedfc7490f85540fa7e182b5c461342.
- Current state: BLOCKED at round 6 of 7.
- Reason: R1-F6 open for three consecutive reviews.

Use the documented foreground resume operation on this existing enrollment and set its total review cap to 9.

This provides three remaining PR-2 reviews. Preserve all findings, prior verdicts, dispositions, and spent review totals. The supported controller’s documented no-progress-resume bookkeeping is authorized. Do not manually edit controller state, erase history, replace the enrollment, or create a new budget.

PR-6 retains its existing allocation of two reviews. All other allocations remain unchanged. Program ceilings become:

- 57 child review executions.
- 64 Mounted Combat lifetime executions, including the seven original PR #64 reviews.
- Eight infrastructure review executions.
- 72 combined executions.

Continue counting the invalidated Bite review as an executed review. Do not transfer unused accepted-child allocations.

The shared validation ceiling remains 450 minutes under:

mounted-alpha2-melee-pose-20261124

The reported closing spend is 374.08 minutes, leaving 75.92. Use the actual existing ledger balance and meter all required builds, tests, and direct diagnostics. No additional validation allowance is authorized by this instruction.

## 3. Complete one connected provisional-order lifecycle

Address R1-F6, R5-F3, and R6-F1 through R6-F7 together. Preserve the accepted R5-F1 and R5-F2 corrections.

The provisional first-tick design may remain, but its complete production lifecycle must work. Passing a test that invokes the decision directly is not evidence that the game can deliver the command to that decision.

### Scheduler reachability — R1-F6

Establish a lawful production path from the click to the actual first movement tick.

Cover both a fresh Preparing turn and an already Acting turn, and both initiative arrangements.

The exact provisional carrier must participate correctly in native command eligibility, paired activity, phase progression, and the movement hook. Installing UnitMoveTo alone does not establish that it can execute.

Do not solve this by globally admitting off-turn mount commands, manufacturing action rights, or forcing Acting in the qualifying test. Adding the carrier to one ownership list is insufficient if Preparing can still stall.

A proposal that loses its ability to progress must settle cleanly rather than indefinitely suppress the retained order.

### Native player state — R6-F1 and R6-F5

Keep the previous accepted order and its player-state ownership intact while replacement is provisional.

Handle the actual native post-Run manual-target assignment and Hold write. Deferring only AcceptStockRequest does not defer those effects.

Apply the replacement’s intended effects once when acceptance succeeds. Refusal or cancellation must not leave behind the proposal’s target or Hold changes. Cleanup must preserve genuinely newer player assignments.

Cover A → pending B → accepted continuation of A. That newer continuation must retire B’s proposal and exact carrier while preserving A’s legitimate identity and target. Distinguish this from the proposal’s own commit.

### Live movement and action authority — R6-F2, R6-F3 and R6-F4

At the decision boundary, use the actual carrier’s current origin and executable path segment. Do not calculate affordable travel solely from the click-time route origin or assume a tick crosses a waypoint that the native mover would stop at.

Keep route-discovery bounds distinct from executable movement authority at every admission, revalidation, replan, and carrier-creation path. A search ceiling must never become an ordinary movement grant because a caller ignores its reason.

Retain immutable facts from the original click, including the target’s relevant life state. Immediately before commitment, recheck current pair/turn ownership, rider action entitlement and attack capability, target validity under those original facts, and movement legality.

Empty rider command slots do not prove the rider still has permission to attack. A target that falls after the click must not be reclassified as having been down when the order was issued.

### Commit, carrier ownership and failure cleanup — R6-F6 and R6-F7

Define the point at which replacement actually succeeds.

Retire previous work only after the required predicates succeed. Establish the accepted order’s ownership of the exact carrier, or complete a proven release of that carrier before strike execution. Do not leave a committed move outside the attack wrapper’s ownership.

Both rider-first and horse-first processing must produce a lawful result. Cleanup must not cancel an unrelated successor command.

Make installation and acceptance exception-safe:

- A failed native Run before slot installation must leave no active proposal gate.
- A failed acceptance must not leave a falsely Committed proposal.
- Pending cleanup must check actual carrier possession, not only whether a detached command object reports itself finished.
- Repeated settlement must not revive a proposal, spend twice, or cancel newer work.

Do not treat a routing-control return value as proof that an order was successfully accepted. Establish success from the actual accepted owner/generation or an explicit internal acceptance result.

Before commitment, refusal must preserve prior work and avoid spending. After a real commitment, failure handling must describe and settle the actual resulting state; do not claim rollback of native effects that were not actually reversible.

### Reachable prior-order evidence — R5-F3

Replace or substantiate the retained-order fixture using a transition supported by the installed game code.

Do not rely on an unexplained combination of Status = Acting and IsActing = false merely to obtain the desired prior order.

Assert actual intent/ledger generation and target, together with carrier identity. StockAttackIntentStartCount is not a substitute for order generation.

## 4. Prove the connected path before formal submission

Use the existing harness and production code. Substitute only the engine boundaries that cannot run offline, and tie the simulated scheduling behavior to the installed native implementation.

The qualifying sequence must include:

- Production click routing and the real post-Run target/Hold hooks.
- Native paired-command eligibility.
- Native-compatible Preparing/Acting progression.
- The production movement-hook entry.
- The first-tick decision.
- Commit or refusal.
- Carrier ownership/release.
- Strike dispatch where permitted.

Do not call the deciding tick directly or force the phase to bypass a missing production connection.

Keep verification focused on the existing reviewer cases and the concrete failures above:

1. The existing 9 m/s, 0.12 m and 1.2 m opening cases with 1/60-second and 0.1-second first ticks, including fresh and legitimately retained orders.
2. Retained target A and a held horse, followed by pending B: inspect the actual target/Hold writes before commitment, on refusal, and on acceptance.
3. A shifted live origin and a short intermediate waypoint: compare predicted travel with actual permitted displacement.
4. Rider Standard or attack capability becoming unavailable after the click; a previously conscious target falling during the wait; and the valid already-down-at-click control.
5. An ordinarily admitted route becoming first-tick-only before carrier creation or revalidation.
6. A → pending B → accepted continuation A.
7. Rider-first and horse-first processing after the movement decision, including a commit-in-place case.
8. Installation and acceptance failures, repeated cleanup, and a subsequent legitimate ground move.

Observe actual order generation and target, exact carrier ownership, manual target and Hold, movement displacement/debit, rider action expenditure, refusal/retirement reason, and released strike count at the relevant boundaries.

Include a regression that exposes the missing scheduler/progression wiring rather than supplying it from the harness. Preserve the existing five-foot-step, movement-spending, lateral-query, and direct-input regressions.

The lead developer remains responsible for inspecting this complete production path even when implementation is delegated. Perform that bounded inspection before formal submission; do not create another formal review loop or a new game-driving framework.

Update the first-tick boundary document to match the implementation and evidence. Do not defer established source ownership, admission, or scheduling defects to owner playtesting.

## 5. Finish PR-2, then the complete integration

After PR-2 is accepted, finalize and freeze it through the supported workflow and proceed to PR-6.

Preserve the local codex/mmr-integration groundwork. It is an unaccepted composition of four children, not an alternate completed milestone.

For the complete integration:

- Pin the exact main baseline and reviewed H sources.
- Add PR-2’s accepted delta relative to accepted PR-1 without double-applying PR-1.
- Keep controller archive heads A distinct from product-source composition.
- Record shared-file composition and conflict resolutions.

Specifically inspect the two reported manual resolutions:

- At strike delivery, retaining both additions must preserve intended ordering, ownership, retirement/release, action spending, and observation behavior.
- The harness exit condition must retain required terminal observations and failures. A supposedly stricter condition must not exit before the relevant scenario or evidence completes.

Run the existing required child-focused checks and protected-neighbor checks on the actual composed source. Preserve Mounted Shield behavior and icon identity, Mounted Archery, Ranger/Riding Dog, other mount profiles, save identities, and the UMM 0.31.1 baseline and existing loader-binding requirements.

Use targeted behavioral evidence for the shared resolutions. Do not repeat unrelated infrastructure gates or begin a broad stress campaign.

Keep integration focused on composition, packaging, and verification. Do not silently change accepted feature behavior to fix an integration-discovered product defect; use the owning child’s supported workflow and existing authority, or report the precise additional authority required.

## 6. Deliver the integrated private alpha with honest acceptance boundaries

The requested artifact includes:

- The complete private-alpha ZIP.
- Exact package hash, composed source identity, DLL identity, and relevant accepted-source provenance.
- Installation and rollback instructions.
- Known limitations.
- The complete native playtest checklist.

Native observations remain pending for my integrated-alpha playtest. Do not require the vanished historical save pair or a new game-driving framework as a source-acceptance prerequisite.

Do not count offline evidence as gameplay. Seating, visible Bite playback, the Trip repair panel and its lifecycle, mounted attack/retarget/cancel behavior, action spending, and save/load still need native observation.

No merge into main, tag, public release, or permanent installation is authorized.

The previously recorded allowance for a supported finalize retry after a verified transient lock clears remains in effect, with unchanged reviewed-source bindings and no altered findings or review allocation. It does not authorize bypassing a lock or clearing another substantive stop.

If a genuinely new controller stop occurs, preserve it and report its exact reason. This instruction lifts the specific round-six stop identified above; it does not authorize silently clearing future stops.

Proceed with the connected implementation and required verification now. Report completion when the full integrated milestone exists, or identify the concrete remaining defect or exhausted authority that prevented it.
