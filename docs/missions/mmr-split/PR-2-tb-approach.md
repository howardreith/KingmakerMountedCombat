# Child mission: PR-2: turn-based approach and movement accounting (MMR-02)

Branch `codex/mmr-tb-approach`, base `codex/mmr-attack-orders (PR-1 accepted frozen head)`. Project `howardreith/KingmakerMountedCombat`. Plan `single`, foreground.
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
| PR-1 | `codex/mmr-attack-orders` | MMR-03, MMR-04, MMR-05 |
| PR-2 (this mission) | `codex/mmr-tb-approach` | MMR-02 |
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

### MMR-02 — turn-based attack approach and legal movement

An ordinary melee attack order on a reachable target outside legal reach should retain the target and move the mounted pair into legal attack range without requiring a separate ground-move click. Recheck the exact native attack plan, actor, movement grant, remaining actions, path, line of sight, stopping radius, mount/target corpulence, and mount-origin versus executor distance.

Use an eligible five-foot step when appropriate under the game's actual step mode and legal action state; otherwise use lawful available movement. Never grant a free step after incompatible movement, refund or duplicate Move/Standard actions, bypass turn/phase authority, increase weapon reach to hide a movement defect, or ignore LOS/path obstacles. If legal movement is insufficient, fail or wait in the same explainable way as the native control. The screenshot's `Move 1` display does not establish remaining entitlement or exact distance.

Exercise the current paired activation configuration, not just legacy experimental schedulers. Preserve both initiative arrangements and correct cancellation, target death, retargeting, and movement-carrier cleanup.

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
| S03 | MMR-02 actual attack admission/approach integration reaches legal range using eligible mount movement and stops its exact carrier. | Exhausted/ended actor, invalid path/LOS and step-vs-move restrictions do not create reach or actions; both initiative arrangements. |
| S08 | Existing Mounted Shield behavior tests and affected neighbors retained; icon identity checked in actual output. | Passive feat amount, AC descriptor/stacking/attack-type behavior, Mobility addition, registration rollback, equipment/lifecycle cleanup; do not rerun unrelated full matrices by rote. |

Owner-pending native observations for this area (planned, initially pending; never counted as passing without execution):

| ID | Observation | Essential comparison / negative case |
| --- | --- | --- |
| N02 | On a fresh eligible TB activation, one ordinary pair attack order just outside reach closes distance and attacks. | Record planned weapon reach, actual path, step mode and remaining movement; no extra ground click. |
| N03 | Legal five-foot-step and ordinary-movement cases use native budgets correctly. | After incompatible movement or exhaustion, no extra free step/full attack; blocked path fails cleanly. |

S09 (package) and S10 (factory acceptance of the delivered alpha) belong to PR-6. Native gameplay, visual and save/load rows
N01-N10 stay pending for the owner's playtest unless performed through an available protected path; the vanished historical
save pair and a new game-driving framework are not prerequisites.

## 4. Historical findings carried by this PR

Source: `analysis-cache/.../decomposition/FINDINGS-CATALOGUE.json` (read-only copy of the cancelled enrollment's findings;
SHA-256 `2efabc22015ac145aba4199fbbaf7d36d0d9605b6331d8a2de02591698c12aa5`). These IDs belong to PR #64; this enrollment issues its own.

### 4.1 Carried OPEN findings (must be closed with the stated evidence)

#### R6-F1 (P1, blocking): The carrier can finish just short of the first legal strike point

- Raised: round 6 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787); acceptance rows S03.
- Trigger: A TB melee click needs movement for rider LOS even though the mount is already within the delegated point move's radius of the enemy.
- Actual: BeginDelegatedMove constructs UnitMoveTo against the enemy snapshot with a body-radius completion distance and LOS disabled. Revalidation changes only ForcedPath. The documented native point-command gate can succeed where the mount stands if it is already inside that radius. The harness checks point proximity only when ForcedPath is absent, and the installed-contract check establishes path preference only.
- Expected: The native carrier's start and completion rules deliver it to a strike-admissible point on the verified route.
- Requested correction: Align the route-bound point command's destination and arrival criterion with the verified strike endpoint, or establish from the installed forced-path contract that the existing point and radius cannot complete it early.
- Requested evidence: Exercise a mount already within the enemy point-command radius whose rider lacks LOS until the flank, plus a verified strike endpoint outside that radius. Include the native proximity decision, actual carrier displacement, exact release, and one admitted strike.
- Last rejection: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791): The destination now equals the verified endpoint, and the recorded flank traversed 1.4 m successfully. The native proximity rule can still complete the carrier anywhere inside its 0.05 m radius, including a point at which the rider lacks sight.

#### R7-F1 (P2, blocking): An affordable sight window can be reported as beyond the movement grant

- Raised: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791); acceptance rows S03.
- Trigger: The rider can see the target through a short opening inside the grant, between two samples of the bounded walk.
- Actual: Clipping a segment to the grant redistributes its samples. On a 3 m segment with a 1.524 m grant, the bounded walk samples near 1.426 and 1.475 m. A sight window at 1.445–1.455 m is missed. The second walk samples 1.45 m and finds the affordable route, but the probe unconditionally replaces its measured cost with infinity. In the existing 1.412–1.428 m fixture, increasing the grant from 1.42 to 1.50 m also changes admission to refusal because both walks miss the window.
- Expected: A verified route whose cost fits the grant is usable. Increasing the grant preserves an already reachable strike position on the same route.
- Requested correction: Use the measured cost and endpoint of any successful walk. Preserve affordable interior sight openings when the grant changes while retaining the exact boundary check.
- Requested evidence: Exercise both distances above through click admission and carrier revalidation. Include a grant ending before the opening and a fully blocked route as refusal controls.

#### R7-F4 (P2, blocking): Single-move admission overstates travel when the mount's Standard is spent

- Raised: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791); acceptance rows S03.
- Trigger: A rider attack needs more than one movement tick while the horse has its first Move available under the single-move limit.
- Actual: With moveUsed=0, singleMove=true and standardUsed=true, RemainingAcrossOrder returns 3 seconds. Advance can debit a first 0.05-second tick. On the next tick Remaining returns zero because singleMove is set and moveUsed is positive. The passing controls exercise single-move and spent-Standard separately.
- Expected: The distance admitted for the selected mode agrees with the mount's executable movement when its Standard has already been spent.
- Requested correction: Reconcile the order allowance with successive native debits for the combined state, or demonstrate from the installed input contract that this flag combination cannot reach paired execution.
- Requested evidence: Start with an untouched Move, spent Standard and single-move selected. Compare admission with several ordinary movement ticks and record the mount's debit and the rider's retained Standard.

Stopping rule (owner, 2026-11-25): if a carried open finding above, or this enrollment's successor finding for it, is
rejected in two consecutive rounds of this enrollment, this child stops after that round for an owner decision.

### 4.2 Verified findings: regression obligations

Each was verified in PR #64 by the independent reviewer; the behavior it describes must not be reintroduced by the extraction
or by any correction here, and the reviewer may reject a regression of any of them.

| ID | Title | Verified | Verifying assessment |
| --- | --- | --- | --- |
| R1-F3 | Turn-based click admission mistakes straight-line distance for a legal route | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | The postfix preserves native IgnoreClick’s result, so straight-line proximity can no longer override a blocked or over-budget route. R3-F2 covers the outstanding legal-approach disposition. |
| R3-F2 | Turn-based click admission still reads the rider’s movement mode | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | PairedMountGrantedMetres now uses the addressable horse, act/move and stand-up checks, paired movement context and remaining allocation. An already-admissible strike bypasses movement eligibility. R4-F1 and R5-F2 concern the route evaluated against that grant. |
| R4-F1 | An overridden TB click can still submit an unvalidated carrier route | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | Every route-bound carrier now recomputes from the current origin and grant. The former 0.2 m cutoff and unchecked fallback are gone. The shifted-start and expired-grant cases passed. R6-F1 and R6-F2 concern how a recomputed route is executed and how its rider position is predicted. |
| R5-F2 | A legal step endpoint can still fall between all sight samples | round 7 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791)) | Both callers pass the grant into the walk, which evaluates its exact endpoint. The recorded 1.42 m case reached the opening, carried the endpoint into execution, and admitted one strike. The 1.40 m control preserved refusal. R7-F1 concerns openings inside a larger grant. |
| R6-F2 | A turning route keeps the rider's starting eye offset in world space | round 7 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791)) | The route predicate rotates both rider and eye offsets from the current mount orientation into the segment heading. In the recorded right-angle case the projected and synchronized admissions agreed, and the translated-offset control remained sight-blocked. |
| R6-F3 | The next movement debit is treated as the entire ordinary travel grant | round 7 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791)) | The original TwoActions case now includes the lawful conversion. With 1.5 seconds already spent and auto-stop off, the recorded production-state check admitted and executed another 4.5 seconds against the mount. R7-F4 concerns a separate overestimate under combined limits. |

### 4.3 Out of scope here (owned elsewhere)

- MMR-01: PR-3 (`codex/mmr-horse-seat`).
- MMR-03: PR-1 (`codex/mmr-attack-orders`).
- MMR-04: PR-1 (`codex/mmr-attack-orders`).
- MMR-05: PR-1 (`codex/mmr-attack-orders`).
- MMR-06: PR-4 (`codex/mmr-horse-trip`).
- MMR-07: PR-5 (`codex/mmr-horse-bite`).
- Mission packet, version, package, HANDOFF/PRIVATE-ALPHA/ledger records, R1-F14/F15/F16, R2-F6, R3-F4, R7-F6: PR-6.

## 5. Extraction from PR #64

Method (owner instruction 2026-11-25 section 5): final content from `46444b30`, not a replay of PR #64's commits. **This PR is
stacked on PR-1** (`codex/mmr-attack-orders`); its diff against that branch is the MMR-02 delta only.

- **Whole files owned by PR-2, byte-identical to `46444b30` at the extraction commit `e6b75f29`:** `Domain/PairedAttackClickAdmissionPolicy.cs`,
  `Domain/MountedRouteWalker.cs`, `Domain/MountedHeading.cs`, `Domain/MountedMovementState.cs`, `Integration/PairedAttackClickProbe.cs`,
  `Integration/PairedActivationControls.cs`, `Integration/PairedActivationLifecycle.cs`, `Integration/MountedWorldTurnCoordinator.cs`,
  `Integration/MountedMovementStateAdapter.cs`, `Integration/MountedPairSingleAttack.cs`.
- **Files shared with PR-1, completed to their `46444b30` content** (PR-2 adds exactly the pieces PR-1 left out):
  - `Integration/MountedPairAttackCommand.cs`: the route-bound fields and carrier block.
  - `Integration/MountedCombatController.cs`: `RiderMeleeClickAdmitted`, the validated click-route memory, `RevalidateAdmittedClickRoute`
    and the route hand-off.
  - `Integration/MountedPatchController.cs`: the IgnoreClick postfix.
  - `tests/MountedAlpha2LifecycleTests.cs`: the R3-F2 click admission, route walker and order allowance cases.
  - the attack-path harness: route cases and the members they splice.
  - `scripts/Test-MountedAlpha2LifecycleContract.ps1`: section 4, turn-based click.

  The Horse strike-line pieces (PR-5) stay out of both shared source files.
- **Checks split by area, labels verbatim:** `scripts/Test-MmrTbApproachNativeContract.ps1` = shared preamble + the MMR-02 rows of the
  alpha.6 native contract (IgnoreClick token, prediction flags, highlighted unit, prediction commands, PathVisualizer, product patches).
- **Extraction proof:** the extraction commit was **not run on its own** (budget). Every PR-2-owned file is byte-identical to `46444b30`,
  and the shared files equal `46444b30` minus the PR-5 strike-line pieces. The first metered run is on the corrected tree.

## 6. Correction design for the carried open findings

**R7-F4: admission equals executable movement; execution honours the entitlement.**
- **Native contract read first** (pinned in `Test-MmrTbApproachNativeContract.ps1`):
  - `TurnController.TrySelectMovementLimit` sets the SingleAction limit back to None once the unit has used its Standard
    (`canSingle` needs normal movement, Move cooldown < 3 and an unused Standard).
  - `GetRemainingMovementTime(single)` ends single-action movement once the unit's own Move cooldown is positive. The turn's
    `m_Cooldown` is the unit's `CombatState.Cooldown`, assigned in the constructor.
  - So a native unit never moves under "single move with Standard spent". KMC reached that state only by mirroring the rider's
    single-move choice onto a mount whose own Standard was spent, and then executed one tick: an execution defect, not merely a
    prediction error.
- **Correction:**
  - `MountedMovementState.EffectiveSingleMove(single, standardUsed) = single && !standardUsed` is applied in `Remaining` and
    `Advance`, so execution grants the mount its ordinary single Move action.
  - `RemainingAcrossOrder`, the admission allowance, is now a **dry run** of this state's own `Advance` on an isolated copy,
    slice after slice, with the real restriction, step, single-move, Standard and auto-stop inputs. It never touches the live
    state, cooldowns or actions, and equals successive execution by construction.
- **Evidence** (component `OrderAllowanceMatchesExecution`):
  - **Combined case:** untouched Move, spent Standard, single-move gives admission 3 s and tick-by-tick execution 3 s, all debited
    to the mount's Move cooldown (it reaches 3). This is identical to the ordinary single Move action. A paired mount's
    allocation carries no rider actions, so nothing is taken from the rider; the rider's own Standard is not modelled here.
  - **Single-move with the Standard unused** still stops after one Move action.
  - **Retained R6-F3 cases:** the TwoActions conversion (4.5 s); Standard spent, single-move and auto-stop each 1.5 s; the step
    allowance.
  - **The live state is untouched by admission.**

**R7-F1: bounded discovery with an accurate guarantee.**
- **Walker sampling:** `MountedRouteWalker` evaluates admission on a fixed **1 cm arc-length grid from the route start**
  (independent of the grant), at the **exact grant point**, and at any **supplied established position**. From the first admissible
  sample it refines back to the opening's start.
- **Stated limit:** an opening is found when it contains a grid point, the grant point or the established position. Every opening
  of at least 1 cm is found; a narrower one may be missed. Monotonicity is NOT claimed beyond the grid: a 4 mm opening around 1.415 m
  is found at grant 1.415 and missed at 1.42 (test), unless the established position is supplied.
- **Established position:** every carrier revalidation passes the destination established at planning
  (`RevalidateAdmittedClickRoute(target, established)`), so a position proven when the click was planned is re-examined on the
  current route.
- **Measured cost:** the probe uses the measured cost and endpoint of any successful walk. The unbounded walk's affordable result is
  no longer replaced by infinity.
- **Evidence** (attack-path harness, through click admission AND carrier revalidation):
  - opening 1.445–1.455 m at grant 1.524 m;
  - opening 1.412–1.428 m at grants 1.42 and 1.50 m.

  Each is admitted with its measured cost, re-proven, stopped inside the opening, and gives one strike. Refusal controls: a grant
  ending before the opening, and a fully blocked route. The component walker test adds the grid/grant/established cases above.

**R6-F1: carrier completion agrees with strike admission.**
- **Native arrival contract:** `UnitCommand.IsUnitEnoughClose` is `SqrMechanicsDistance(ApproachPoint, Executor.Position) <=
  ApproachRadius²` (horizontal). The previous carrier targeted the opening's first point with 0.05 m, so it could complete anywhere
  in that disc, including before the opening.
- **Destination and radius:** the probe now chooses a destination INSIDE the opening (within the grant) and the largest radius from
  0.05, 0.025, 0.0125, 0.00625 and 0.003125 m for which the strike's production admission passes at the destination and on rings
  at the radius and half of it (32 points, the segment heading). Every position native can accept as arrival therefore admits the
  strike, including near corners. The forced path ends at that destination.
- **Proof object:** revalidation returns this proof (`PairedAttackClickProbe.Approach`: route, arrival radius), and the carrier is
  built from it.
- **Fallback:** if native would complete at once while the strike is not admitted, the approach ends cleanly
  (`RouteArrivalContradictionCount`, never re-issued). This is only a fallback; the reviewer's cases succeed by moving.
- **Evidence** (attack-path harness, native proximity rule in the facade):
  - the reviewer's **z = 0.54 flank**;
  - a mount that **starts 0.04 m before the opening** with sight blocked;
  - the original z = 0.60 flank.

  In each, native's first proximity decision is "not close", the proven disc admits, the horse moves to legal sight, the exact
  carrier is released, and one strike is admitted. Controls: the round-6 carrier completes blind at z = 0.50 for both 0.54 cases.
  In the original geometry it happened to stop exactly on the boundary, which is why round 6 passed.
- **Retained cases:** R4-F1 corner and shifted starts (the destination now lies inside the opening past the corner), R5-F2, R6-F2
  rotation with its translated control, and PR-1's pre-start/perception cases.

**Limits:** no native run. The facade moves 0.25 m per tick along the forced path. The disc check samples 33 points per
candidate, not the continuous disc. Openings too narrow for a 3 mm proven disc inside the grant are refused
(`arrival-unverifiable`), keeping native's decision. No free movement, extra attack or relaxed line of sight is granted.

## 7. Validation

- Focused gate: `verify --gate focused -- -Tier Fast -Focused Test-MmrTbApproachFocused.ps1`. It runs build, components,
  Validate-Source, `Test-MmrTbApproachNativeContract.ps1`, `Test-MmrAttackOrdersNativeContract.ps1`,
  `Test-MountedAlpha2LifecycleContract.ps1`, `Test-MountedAlpha2AttackPathHarness.ps1`, `Test-MountedPresentationContracts.ps1`,
  and the shared regression neighbors (Mounted Shield with icon, Archery, Ranger/Riding Dog, replay subscriber, localization, UMM
  0.31.1 build reference, native loader, frozen source, UMM loader 0.31.1/0.33.0).
- Development runs (metered): components 1542/0; Validate-Source 153/0; lifecycle contract 66/0; attack-path harness 53/0; approach
  native contract 10/0; orders native contract 4/0; presentation 239/0; neighbors pass.

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
  8 lines, SHA-256 `9f9dbc5acb0474acaee8ce10d4d3ac6cfa0df33f44a81bad05f68679ba1bcfff`. Later appends do not change this prefix.
- No merge, tag, release or permanent installation. The PR stays draft for owner review.
