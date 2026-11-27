# Child mission: PR-1: attack orders and real-time melee (MMR-03/04/05)

Branch `codex/mmr-attack-orders`, base `main`. Project `howardreith/KingmakerMountedCombat`. Plan `single`, foreground.
Formal review allowance for this enrollment: **3 rounds** (`--max-rounds 3`), a ceiling, not a target.

## 1. Authority and context

This is one of six child missions that replace the cancelled enrollment `kmc-codex-mounted-alpha2-melee-pose-20261124102246-64c1`
(PR #64, draft, kept open as a historical reference; 7 of 7 formal rounds spent, every round `changes_required`). The owner approved
the decomposition on 2026-11-25; the instruction is preserved verbatim outside the repository at
`analysis-cache/mounted-alpha2-melee-pose-20261124/owner-continuations/owner-continuation-2026-11-25-decomposition-approval.md`
(SHA-256 `1d32c03a87a48c8fc9e7bfc0095d3ca3b937ef03276d0cf47307424159d56d7d`). The original mission packet is
`docs/missions/mounted-alpha2-melee-pose/` at `46444b302ea9a0c77950dcf28f31d5472b9d3992` (its `SHA256SUMS.txt` has SHA-256 `2039eaf677ef79cea85d86955b1e970bbf50c1d1f7c42187b3b116e36cd84abd`); it is
delivered to the repository by PR-6. The sections quoted below are verbatim from it.

| PR | Branch | Requirement(s) |
| --- | --- | --- |
| PR-1 (this mission) | `codex/mmr-attack-orders` | MMR-03, MMR-04, MMR-05 |
| PR-2 | `codex/mmr-tb-approach` | MMR-02 |
| PR-3 | `codex/mmr-horse-seat` | MMR-01 |
| PR-4 | `codex/mmr-horse-trip` | MMR-06 |
| PR-5 | `codex/mmr-horse-bite` | MMR-07 |
| PR-6 | `codex/mounted-alpha2-integration` | assembly of the accepted changes, private-alpha delivery and records |

**Scope of review.** This PR's entire diff against its target is under review, including code extracted from PR #64 at
`46444b30` (alpha.6). That code was reviewed seven times there and was **never accepted**; nothing in it is accepted by
virtue of its history. The first commit after the base is the extraction (no behavior change, described in section 5); later
commits are corrections. The reviewer is asked to review the inherited implementation from scratch, with the history below
as context, not as a verdict.

## 2. Requirements (verbatim from the original MISSION.md)

### MMR-03 — horse participation in ordinary pair attacks

An ordinary melee attack order for the mounted pair should carry the intended target through the **current paired activation** so that the horse follows through with its own legally available native attack or routine, without a redundant mount order. Sequential rider/horse execution is acceptable; simultaneous or free actions are not required.

The present source restricts default TB ordinary rider intent to the rider. Treat changing that user-facing order semantic as deliberate scope, with an explicit behavioral test. Preserve explicitly actor-specific commands and newer player overrides. At every dispatch, recheck that the horse is addressable, granted/prepared as required, not ended or already spent, capable, and in legal range. Do not turn this into indefinite cross-round TB autoattack, reactivate an already-used partner, call native Prepare to manufacture entitlement, or create a second action budget.

Retain the native distinction between a single attack and a full natural routine. Identify actual horse-owned attacks/rolls/damage by actor, target, and weapon; a hoof animation or rider damage is not proof of horse participation. Preserve bite and hoof attack availability/cardinality under the actual creature level, body, and native plan. Do not force all natural attacks after movement when only a single attack is legal.

### MMR-04 — real-time automatic melee engagement

When combat begins with an idle mounted melee pair, AI enabled, and no stronger player order or native restriction, the pair should acquire the nearest valid reachable hostile in its legitimate awareness/engagement set and approach/attack as ordinary native melee characters do.

First establish whether a native rider-brain proposal is absent, refused, or accepted but stalled. Inspect the existing automatic-intent policy and refusal telemetry before adding another targeting system. Preserve an explicit selected target until invalidated or replaced; define how nearest eligible candidates are compared and avoid repeated target oscillation. Do not acquire unseen or unreachable enemies solely because their entity positions exist.

Respect AI-off, Pause, Stop, Hold, explicit movement/casting orders, disabled/incapacitated states, queued player work, and targets that die or leave eligibility. Do not simply force-enable the mount's independent brain while the pair owns movement; preserve a single command/movement authority. If a native policy differs from the requested selection behavior, document the actual difference and implement only the narrow pair behavior justified by this request.

### MMR-05 — prompt real-time attack-command movement

Investigate the pause between a player attack order and the pair's first movement. The current source checks Standard-action readiness before dispatching the wrapper that later creates approach movement; this is a concrete lead, not a proven explanation.

Separate intent acceptance, legal movement start, and actual attack execution. Where a comparable native unmounted actor can move while its attack/initiative/cooldown is not yet ready, the mounted pair should also start legal movement promptly. Preserve the real attack's preparation, initiative, cooldown, range, animation event, reaction, and action expenditure. Do not reset timers or release damage early to make a graph look faster.

Measure input/acceptance, carrier creation, first nonzero movement, attack readiness, and first attack on a comparable control where runtime is available. Offline regressions must exercise the actual readiness/dispatch integration or shared production seam, not a standalone copy of the desired timing formula. A still image cannot establish a latency in milliseconds; do not invent a performance threshold from it.

### Regression and architecture constraints (verbatim, section 4 of the original mission)

- Preserve the single passive Mounted Shield feat and its prerequisites, feat-list publication/rollback behavior, existing icon bytes, equipment/lifecycle invalidation, nonstacking shield AC semantics, and independent Mounted Combat Mobility contribution. No additional toggle or icon is requested.
- The owner screenshots show `Mounted Shield +2` on Horse AC and a Mounted Combat Mobility total of 23 including `+2` against attack 19. Preserve those observations without treating them as proof of all shield types, enhancements, touch/flat-footed/Brilliant Energy, or save/load cases.
- Preserve the repository's .NET Framework 4.7/C# 7.3, Kingmaker 2.1.7b and Harmony12 contracts. Keep UMM 0.31.1 as build reference/minimum; do not downgrade or otherwise alter the installed loader. Compilation/binding checks are not Linux runtime certification.
- Keep native authority over actions, turns, preparation, reactions, attack planning and damage. Preserve current paired configuration and separate integration, domain, presentation and diagnostics responsibilities. Hooks delegate to the existing service layer.
- Preserve stable blueprint/save identities, exact lease ownership, unload/disable/dismount/exception cleanup, and unrelated actors/mods. A movement fix must not silently bypass the actual coordinator still responsible for movement accounting.
- Use existing libraries and behavior/contract infrastructure. No new dependency unless it replaces substantial code and is justified. Prefer real production paths and engine objects where available; mock only a genuine external boundary. Avoid source-text assertions or tests that merely restate implementation.

## 3. Acceptance (verbatim from the original ACCEPTANCE.md)

The factory checkpoint is **source/offline correctness and delivery of the next private alpha**. Every core issue requires a supported source disposition and relevant checks; native visual/gameplay confirmation may remain pending for Howie. Unknown behavior must remain unknown. A known unresolved core defect, missing required current gate, unsafe transaction, or un-restored state is a blocker.

Use `templates/acceptance-ledger.json` for issue status and evidence. Its issue IDs are not controller finding IDs. The strict developer report uses its own unchanged schema and only actual findings from the new enrollment.

Do not turn each row below into a separate launch or full qualification campaign. Select checks for changed behavior and relevant neighbors. The owner playtest groups many observations into the same encounter.

| ID | Required evidence and positive outcome | Boundary that must be preserved |
| --- | --- | --- |
| S04 | MMR-03 ordinary pair intent reaches the horse on its own legal activation without a redundant order; native attack planning/cardinality is retained. | Explicit actor-specific override stays actor-specific; already-spent/ended/unprepared/disabled partner is protected; no indefinite TB cross-round replay. |
| S05 | MMR-04 automatic proposal/acquisition path and MMR-05 readiness/approach integration cover accepted-and-progressing ordinary melee intent; compare two eligible targets through the real selection seam where practical. | AI-off, Stop/Hold, Pause, competing commands, invalid target, cooldown and attack timing remain authoritative. Test movement readiness separately from strike readiness. |
| S08 | Existing Mounted Shield behavior tests and affected neighbors retained; icon identity checked in actual output. | Passive feat amount, AC descriptor/stacking/attack-type behavior, Mobility addition, registration rollback, equipment/lifecycle cleanup; do not rerun unrelated full matrices by rote. |

Owner-pending native observations for this area (planned, initially pending; never counted as passing without execution):

| ID | Observation | Essential comparison / negative case |
| --- | --- | --- |
| N04 | Horse follows the pair target and performs its eligible native attack(s), attributed to Horse in logs/rules. | Rider-first and mount-first arrangements or their real equivalent; used/ended horse does not attack again. |
| N05 | RT idle AI-enabled pair acquires/approaches the nearest eligible reachable hostile when combat begins; use two eligible targets in an available encounter where practical. | AI-off and explicit Stop/Hold remain respected; a chosen target is not replaced by a different nearest enemy. |
| N06 | RT explicit attack order begins legal movement with native-like responsiveness. | Compare equivalent unmounted control and attack cooldown/preparation state; attack itself never executes early. |

S09 (package) and S10 (factory acceptance of the delivered alpha) belong to PR-6. Native gameplay, visual and save/load rows
N01-N10 stay pending for the owner's playtest unless performed through an available protected path; the vanished historical
save pair and a new game-driving framework are not prerequisites.

## 4. Historical findings carried by this PR

Source: `analysis-cache/.../decomposition/FINDINGS-CATALOGUE.json` (read-only copy of the cancelled enrollment's findings;
SHA-256 `2efabc22015ac145aba4199fbbaf7d36d0d9605b6331d8a2de02591698c12aa5`). These IDs belong to PR #64; this enrollment issues its own.

### 4.2 Verified findings: regression obligations

Each was verified in PR #64 by the independent reviewer; the behavior it describes must not be reintroduced by the extraction
or by any correction here, and the reviewer may reject a regression of any of them.

| ID | Title | Verified | Verifying assessment |
| --- | --- | --- | --- |
| R1-F4 | An adjacent ranged target now consumes the horse's turn | round 2 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7138840145)) | The reported adjacent bow case with unchanged equipment now excludes the horse under paired activation. The focused comparison dispatches the eligible horse for melee. R2-F1 covers a subsequent change of equipment. |
| R1-F5 | Directly queued orders still escape the follow-through override | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | The private Run and AddToQueueInternal postfixes check actual container membership. Accepted competing work latches for the retained generation even after completion. The classifier distinguishes approach-requiring Free or Swift actions, coexisting work, reactions and exact pair commands. |
| R1-F6 | Participant selection still disagrees with dispatch about who can attack | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | Turn-based selection now uses CaptureContext and the same action evaluator as dispatch, including paired addressability and the acting unit’s capability block. Completion consults that refusal when the actor still has a Standard. |
| R1-F7 | Early movement cleanup still uses the current mount instead of the admitting owner | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | The early-move allocation and retirement path was removed. Its release through a replacement mount and loss of the original cleanup reference can no longer occur. |
| R1-F8 | A horse held before the pair observed the input can still travel automatically | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | An already-held horse now restricts automatic travel. The exception requires an observed write by the pair’s own carrier, and the focused cases cover pre-existing Hold, carrier provenance and player release. |
| R1-F9 | Early movement still disagrees with strike geometry and misclassifies route failures | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | With the early movement loop removed, its separate reach calculation and blanket classification of an interrupted route as a successor are gone. R3-F1 covers the missing movement behavior. |
| R1-F10 | An available Standard can still hand movement to a stalled wrapper | round 2 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7138840145)) | ActorStrikeReady now reads native initiative, hand animation, and scheduled equipment state. The production decision permits lawful travel during the reported usable-Standard equipment wait. R2-F2 covers a separate wait introduced during native queue admission. |
| R1-F11 | Initial target ties and movement attribution remain unresolved | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | The farther-first melee tie now selects the nearer eligible equal-weight candidate, and movement attribution requires the same exact carrier at both ends of an interval. R5-F7 and R5-F8 address the newly introduced ranged behavior and native-proposal labeling. |
| R2-F1 | The attack kind is still captured after acceptance on a deferred drive | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | Both player and automatic acceptance capture the attack kind in the retained generation. Deferred dispatch reads that value for horse participation, and promotion preserves it. |
| R2-F2 | Queue-only attack admission leaves the pair without approach movement | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | Withdrawal removes the specific handoff that released an active early carrier on queue-only admission. It also removes movement during that wait, which remains part of R3-F1. |
| R2-F3 | Early pursuit can track a target after it leaves perception | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | While both attacks wait, the removed early pursuit loop can no longer read a hidden target’s new position and issue another destination. |
| R2-F5 | Real-time strike dispatch still misses a newer mount-slot order | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | A newer player command in a mount slot is now detected before strike dispatch. Exact carrier cleanup protects the successor. R4-F3 covers the reverse ordering, where the slot occupant predates the rider click. |
| R3-F1 | The horse’s first pre-start tick classifies movement with LOS disabled | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | The horse strike sets NeedLoS before its first admission classification. The queued-horse harness covers promotion, first-tick ownership behind an obstruction, release, and one start after sight clears. |
| R3-F3 | The rider’s own approach suppresses horse follow-through | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | The dispatcher records the wrapper and subscribes before native Run. Both move issuers announce the exact command before submission, including repath replacements. Those commands no longer latch a competing override against horse follow-through. |
| R4-F2 | An automatic target that leaves perception can still be struck on arrival | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | Automatic perception is checked before in-range start, arrival, and child release. The harness recorded zero starts on perception loss, released the exact owned move, and preserved a successor move and explicit-order controls. |
| R4-F3 | An older mount-slot action can retire a newer explicit rider click | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | A stationary player Standard already occupying the horse’s slot no longer retires a fresh explicit rider click. A genuinely later acceptance still supersedes it. R5-F5 covers the distinct queue-promotion case. |
| R4-F8 | A sustained real-time order retains every finished command | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | Controller updates release finished retained identities, and issuing a new move prunes finished carriers from the command. The repeated-strike and repath case preserves recognition of the live wrapper and carrier. |
| R5-F5 | Promoting older queued work is misclassified as a new player override | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | The private Run hook forwards native fromQueue. Promotion no longer latches a new override, while a direct queue insertion after the rider click still does. The focused cases cover both orderings. |
| R5-F6 | Early RT dispatch can convert a waiting rider order into a horse attack | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | With both Standards cooling, the early-striker policy selects no substitute for a temporarily unable rider. The ordinary selector can dispatch the rider upon recovery. The separate horse-selected path retains the horse as executor. Spending was traced in source. |
| R5-F7 | The new melee tie-break also retargets mounted archers | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | The production tie-break receives the resolved rider weapon mode. The same farther-first equal-weight list selected the nearer melee target and preserved the native ranged proposal. |
| R5-F8 | The engagement report labels the tie-break winner as the native proposal | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | The ranking reader receives the original brain proposal, and the engagement record stores and prints the selected target separately. The farther-proposed, nearer-selected report case passed. |
| R5-F9 | An active RT routine rebuilds a throwaway attack plan every update | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | An active command, an available Standard, or forbidden movement now short-circuits before PlannedStrikeAdmissibleNow can build a plan. This verifies the optional correction by source inspection. No measured performance gain is claimed. |

### 4.3 Out of scope here (owned elsewhere)

- MMR-01: PR-3 (`codex/mmr-horse-seat`).
- MMR-02: PR-2 (`codex/mmr-tb-approach`).
- MMR-06: PR-4 (`codex/mmr-horse-trip`).
- MMR-07: PR-5 (`codex/mmr-horse-bite`).
- Mission packet, version, package, HANDOFF/PRIVATE-ALPHA/ledger records, R1-F14/F15/F16, R2-F6, R3-F4, R7-F6: PR-6.

## 5. Extraction from PR #64

Method (owner instruction 2026-11-25 section 5): final content from `46444b30`, not a replay of PR #64's interleaved commits.

- **Whole files owned by PR-1, byte-identical to `46444b30`:** `Domain/MountedRetainedOrderLedger.cs`,
  `Domain/MountedPreStartApproachPolicy.cs`, `Domain/MountedHoldProvenance.cs`, `Domain/MountedEngagementDiagnostic.cs`,
  `Domain/MountedStockAttack.cs` (its only alpha change is `MountJoinsIntent`, MMR-03), `Integration/NativeBrainRankingReader.cs`.
- **Shared files, PR-1's exact contributions** (the `46444b30` content minus the documented contributions of other PRs):
  - `Integration/MountedPairAttackCommand.cs`: own-move registry (R3-F3/R4-F8), pre-start approach for the rider's carrier and the
    horse's native approach (MMR-05, R3-F1), automatic perception guards at start/arrival/release/pre-start/repath (R4-F2),
    `CurrentMovementOwner` (R1-F11), OnStart adoption of the live pre-start carrier and the newer-order check, successor-safe
    `StopDelegatedMove` (R1-F7/R2-F5). Not included: the route-bound carrier fields and block (PR-2) and the Horse strike-line
    members `AttackAnimationSpeedScale`, `RoutineAttackCount`, `RecordedHorsePrimaryAnimationAction`, `ObserveHorseStrike`,
    `HorseStrikeSource` (PR-5).
  - `Integration/MountedCombatController.cs`: retained-order ledger, accepted-work observation, Hold provenance and restoration,
    engagement record, early striker and planned-strike probe, readiness-gated dispatch, melee-only equal-rank tie-break with
    native proposed vs selected, mount-slot player work, follow-through eligibility. Not included: `RiderMeleeClickAdmitted`, the
    validated click-route memory, `RevalidateAdmittedClickRoute` and the route hand-off (PR-2); the `horseBite` terminal fields (PR-5).
  - `Integration/MountedPatchController.cs`: accepted-work postfixes (slot with native `fromQueue`, queue), the UnitMoveTo.OnAction
    Hold bracket, the Hold input prefix, the pre-start approach prefix. Not included: the IgnoreClick postfix (PR-2).
  - Project files and `tests/Program.cs`: only PR-1's compile items and registration.
- **Tests:** `tests/MountedAlpha2LifecycleTests.cs` keeps every PR-1 case unchanged (R3-F3, R1-F5, R2-F1, R2-F5, R4-F3, R5-F5,
  R4-F8, R1-F8, R3-F1, R1-F11 x3); its three MMR-02 cases move to PR-2. `tests/MountedStockAttackPolicyTests.cs` gains the MMR-03
  `PairOrderHorseFollowThrough` and R1-F4/F6 `FollowThroughEligibility` cases unchanged.
- **Checks split by area, labels verbatim:** `scripts/Test-MountedAlpha2LifecycleContract.ps1` without its section 4 (turn-based
  click, PR-2): 49 checks; `scripts/Test-MmrAttackOrdersNativeContract.ps1`: the shared preamble plus the MMR-04/05 rows of the
  alpha.6 native contract (CanActInCombat; brain candidates, sort and FillWeight): 4 checks; the attack-path harness
  (`scripts/Test-MountedAlpha2AttackPathHarness.ps1`, `tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs`) with its R3-F1 and
  R4-F2 counterexamples and controls on the actual spliced members: 16 checks; its route cases (R4-F1, R5-F2, R6-F1, R6-F2) and
  the PR-2 members they splice move to PR-2.
- **Extraction proof at the extraction commit:** build clean; components 1539/0; Validate-Source 153/0; the focused aggregate
  below passed except the harness on the first run (a PR-2 helper left in the template, removed before commit; harness rerun
  16/0). The whole-file identity of every PR-1-owned file with `46444b30` is checked mechanically; the composition of all
  shared-file contributions is checked again by PR-6.

## 7. Validation

- Focused gate: `verify --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1` (build, components,
  Validate-Source, then: `Test-MmrAttackOrdersNativeContract.ps1`, `Test-MountedAlpha2LifecycleContract.ps1`,
  `Test-MountedAlpha2AttackPathHarness.ps1`, `Test-MountedPresentationContracts.ps1`, and the regression neighbors: Mounted Shield
  native contract and selection rollback (icon identity), Archery feat, Ranger/Riding Dog selection, replay subscriber inventory,
  localization keys, UMM 0.31.1 build reference, chunk7 native loader contracts, frozen product source, UMM loader 0.31.1/0.33.0).
- No carried open findings: no new behavior test is required beyond keeping every listed regression green.

- Validation budget: the existing ID `mounted-alpha2-melee-pose-20261124` (90-minute allowance, not renewed). Its record was
  reconciled on 2026-11-25 by importing the previously unmetered direct runs (upper bound); every later direct run is
  metered and imported. Before each formal review the developer checks both this child's round allocation and the program
  totals in the supplemental ledger. No new budget ID is created.
- Native testing: none is required for this checkpoint; any observation must come from an actual protected run and be
  reported as such. Offline harnesses state `nativeProof=false`.

## 8. Limits and records

- This enrollment: `--max-rounds 3`; 3 attempts per round; the controller's 3-round no-progress rule.
- Program (owner, 2026-11-25): 7 formal rounds spent in PR #64; 15 additional across the six child enrollments (PR-1 3, PR-2 3,
  PR-3 3, PR-4 2, PR-5 2, PR-6 2); 22 in total. No redistribution, cap extension or replacement enrollment without further owner
  authorization; one enrollment active at a time.
- Supplemental operator ledger (not controller output, not enforced by the controller):
  `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, pinned here by its immutable prefix:
  6 lines, SHA-256 `969697ad6a27fc7b46058a198b19478794b9dfbedbf1c6e29f79c5a596e79bd9`. Later appends do not change this prefix.
- No merge, tag, release or permanent installation. The PR stays draft for owner review.
