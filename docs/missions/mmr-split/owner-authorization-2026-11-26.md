I authorize a bounded continuation of the existing Mounted Melee Repair decomposition. Preserve the full original functionality and acceptance criteria.

Keep PR #65 and its existing enrollment. Do not restart the decomposition, replace enrollments to reset counters, or reduce scope.

## 1. Authorized limits

For PR-1 / PR #65:

- Existing enrollment: `kmc-codex-mmr-attack-orders-20261125103400-6cef`
- Current reported head: `19fef3084980c7159cdb9dba4d96768737986718`
- Increase its total formal review ceiling from 3 to **6**.
- Three rounds are already spent; this authorizes rounds 4, 5 and 6.
- Preserve every finding, disposition, review result and previous stop.

The other child allocations remain unchanged:

- PR-2: 3 rounds.
- PR-3: 3 rounds.
- PR-4: 2 rounds.
- PR-5: 2 rounds.
- PR-6: 2 rounds.

Update the supplemental program accounting to **18 allocated child reviews**, or **25 lifetime reviews including the seven spent on PR #64**. Ten lifetime reviews have been spent at the reported stopping point.

Do not redistribute allocations or extend them further without another owner instruction.

For validation:

- Keep budget ID `mounted-alpha2-melee-pose-20261124`.
- Increase that record’s total allowance from 90 to **150 minutes**.
- Preserve all existing spending and reconciliation entries.
- At the reported 78.52-minute balance, this leaves 71.48 minutes. Report the actual balance after checking the record.

Use the existing locked budget-record update mechanism. Add the required allowance-adjustment record identifying this owner authorization, the old and new limits, and the adjustment time. Do not change the project’s default allowance or create a fresh budget ID.

Preserve the 158-call reconciliation inventory and its conservative accounting basis. Do not repeat the historical reconstruction unless a concrete error is identified.

## 2. Sequence the independent work first

Take the prepared PR-4 Trip branch through its existing enrollment and review allocation first, then PR-5 bite.

PR #65 may remain BLOCKED while one of those independent children is actively developing or reviewing. Keep only one child actively developing/reviewing at a time, and preserve PR #65’s enrollment and claim.

Local passing checks are preparation, not formal acceptance. Recheck each branch’s actual state, extraction, corrections and required candidate-bound evidence before submission. Use the existing bootstrap, enrollment, guarded push and review workflow.

Preserve the distinction between bite handle/binding safety and visible animation playback. The original bite objective and its owner-visible acceptance remain in force.

If an independent child reaches its own limit, preserve that stop and continue other independent authorized work where possible. PR-6 must still wait for all required feature acceptances.

After the independent work, return to PR #65 under the extension below. Do not use this sequencing change to cancel or replace its enrollment.

## 3. Resume PR #65 through the supported operation

Save this instruction verbatim as owner authorization and append the decision to the program records.

When resuming PR #65, use the existing controller operation:

python C:/Dev/agent-review/agent_review.py resume --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --reason "Owner authorizes three additional PR-1 review rounds, raising its total ceiling to six while preserving findings and spent rounds" --max-rounds 6

Verify the resulting status and limits.

The controller’s normal treatment of the acknowledged no-progress stop is authorized. Do not manually reset finding history, edit controller state, or mark unresolved findings fixed.

These budget changes use their explicit supported operations. The implementation approach below preserves scope and acceptance; bind it through the supported guidance route where applicable. Do not rewrite the hash-pinned mission merely to change the budget numbers.

## 4. Make one coherent lifecycle correction before round 4

Address all ten current blockers together within the existing PR-1 production boundaries:

R1-F3, R1-F7, R1-F11, R2-F1, R2-F3, R2-F4, R3-F1, R3-F2, R3-F3 and R3-F4.

R3-F5 remains optional. It may be corrected if straightforward alongside the affected code, but it must not displace required work or expand into a performance project.

Before editing, make a concise working record of the existing lifecycle and the facts each transition requires. Keep this within implementation work; do not return another general plan for approval.

Distinguish accepted order, actor admission, native start, first actual delivery, actor terminal outcome and overall order retirement. Keep order-generation lifetime separate from each participant’s command lifetime.

Do not solve each finding by adding another isolated condition that leaves the corresponding controller or queue path unchanged.

### Attempt, delivery and feedback

- Use the actual native terminal branch and delivery evidence to distinguish a rider-only refusal from a delivered action.
- Do not treat a bookkeeping mark such as IsActed as sufficient proof of attack delivery when the native contract permits that mark without delivery.
- An evidenced rider-only refusal with no delivery should end that rider attempt and permit one independently legal horse follow-through in the same activation.
- Do not redispatch the rider or manufacture horse preparation, movement, actions or attacks.
- Keep pending refusal feedback bound to the exact order generation.
- Settle that feedback from actual delivery or final retirement. Horse admission alone is not proof that anything executed.
- Clear pending feedback on replacement and cancellation so it cannot leak into a later order.
- Retirement caused by newer work must distinguish an unreleased first strike from a released native action. Preserve the engine’s authority over completion and legitimate native cancellation.

### Retirement, queue promotion and selection ownership

- Apply exact-wrapper retirement consistently to turn-based, real-time and general cancellation paths.
- A retired wrapper must not later install into a native slot or overwrite Hold/manual selection during queue promotion.
- Preserve unrelated queue entries and legitimate actor-specific orders.
- Refuse late promotion without retaining finished wrappers and their actor/target references indefinitely. Prefer retirement state on the exact wrapper or an equivalent lifetime-bounded mechanism.
- Track manual-target ownership from the actual assignment, independently of whether the associated command competes with the attack.
- Handle coexisting same-target player writes, queue insertion without a write, refused promotion, and the click handler’s post-Run assignment according to the installed contract.
- Preserve newer player selections even when their target value equals the retiring order’s target.

### Approach and phase timing

- Evaluate current stationary strike admission before cancelling an approach solely because movement has become forbidden.
- Release the exact carrier and permit a ready legal stationary attack; retain the out-of-range cancellation control.
- Choose sight goals using the carrier’s actual arrival behavior and a heading it can achieve.
- A goal already inside the arrival radius must not be treated as progress if it requires movement or turning that never occurs. Provide a bounded viable alternative where one exists.
- Keep waiting time separate from approach and attack-phase timing.
- Establish the installed native timeout’s applicability to this wrapper rather than assuming an exemption.
- Reset phase accounting when the attack clock restarts. A previous wait must not create negative elapsed time or extend the ordinary bound of a later stalled attack.

Keep native action spending, strike timing, perception, reach, line of sight, the paired coordinator and explicit player restrictions authoritative.

## 5. Repair the missing behavioral evidence

Use the existing harness and production seams, extending the missing boundaries only. A small production seam is acceptable where needed; do not copy controller logic into an independent test implementation or build a general game-testing framework.

The next candidate must exercise the relevant controller behavior through:

- Terminal handling followed by the next dispatch.
- Retirement followed by native queue promotion.
- First delivery followed by supersession/retirement.
- Actual target assignment and later cleanup.

Call-presence checks and isolated wrapper tests may remain useful supporting evidence, but they cannot stand in for those transitions.

Required focused cases:

1. **Rider refusal and horse follow-through:** an unstarted native force-finish and an interrupted first windup; zero rider deliveries; one eligible horse attempt; no attempt for a spent or ended horse.
2. **Delivery and retirement:** a positive control capable of delivering, supersession before first delivery, and controller retirement after delivery. Exercise the relevant event order rather than manually setting a delivery marker and ticking only the wrapper.
3. **Queue lifecycle:** controller-first and promotion-first retirement in real time and turn-based mode; no stale slot installation or target/Hold write; unrelated successors preserved; bounded retention when no future callback arrives.
4. **Manual targets:** same-target coexisting writes, queue insertion with no write, refused promotion, and the native click assignment after Run.
5. **Horse reach:** initially outside; initially inside and moved outside after observing real controller dispatch; continuously inside. Record dispatch, carrier and attack-start outcomes.
6. **Approach:** the sight-goal case already inside arrival radius, plus movement revoked on the tick a stationary strike becomes legal and the still-out-of-range control.
7. **Waiting and clocks:** a wait beyond the relevant native deadline with the actual contract represented, followed by a moved target and freed slot; then verify the ordinary timeout for a deliberately stalled subsequent attack.
8. **Feedback:** an entirely unexecuted order reports the applicable refusal once; successful follow-through and replacement orders do not receive stale feedback.

At the external engine boundary, represent the native behavior necessary for these cases, backed by the installed contract. Do not claim live gameplay proof from the harness.

Correct the misleading lost-reach fixture. Do not relabel its evidence gap as proof that the already-verified production reach correction is still defective.

## 6. Use the additional reviews efficiently

Before submitting round 4, map each current blocker to:

- Its production correction.
- The actual exercised path or other appropriate evidence.
- The remaining limitation, if any.

Run the required focused gate on the exact candidate. Preserve the existing relevant Shield, Archery, Ranger/Riding Dog, presentation, replay and loader checks.

Do not broaden validation merely to increase test counts. Meter all qualifying runs, and do not rerun unchanged broad campaigns without a concrete risk or mandatory gate.

Use subsequent allocated rounds to resolve actual reviewer findings. Keep all existing stopping rules and cumulative accounting. This authorization does not guarantee further extensions.

## 7. Continue the approved dependency chain

After PR-1 is accepted, refresh PR-2 against PR-1’s actual accepted frozen source. Do not submit the previously prepared PR-2 tree unchanged if its parent or shared-file ownership has moved.

Account for helpers already transferred into PR-1 during its reviews, and keep PR-2’s incremental diff and provenance accurate.

Continue PR-2 and PR-3 under their existing allocations, then PR-6 after all required feature acceptances.

Retain the approved integration rules:

- Assemble accepted source changes with explicit shared-file composition.
- Preserve reviewed source and finalized archive identities separately.
- Do not import other children’s controller transcript changes into PR-6.
- Review and verify the exact combined source.
- Return substantive component defects through the supported reopening procedure and the existing authorized limits.

## 8. Delivery and reporting

Native gameplay, visible seating/bite behavior and save/load observations may remain pending for my private-alpha playtest under the existing milestone.

Known source defects remain blockers. Do not close them by relabelling them as unavailable native testing.

Do not merge, tag, publish a release or permanently install the mod.

Report completed child acceptances as they occur. The final handoff must identify the accepted source/archives, unresolved findings if any, review usage, actual validation balance, integration/package identity and the remaining owner playtest observations.

Proceed with this authorized work. Stop only where the actual controller, capability or allocated limits require it, and identify the precise blocking operation and reason.
