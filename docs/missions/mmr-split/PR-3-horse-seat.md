# Child mission: PR-3: Horse seating lifecycle (MMR-01)

Branch `codex/mmr-horse-seat`, base `main`. Project `howardreith/KingmakerMountedCombat`. Plan `single`, foreground.
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
| PR-2 | `codex/mmr-tb-approach` | MMR-02 |
| PR-3 (this mission) | `codex/mmr-horse-seat` | MMR-01 |
| PR-4 | `codex/mmr-horse-trip` | MMR-06 |
| PR-5 | `codex/mmr-horse-bite` | MMR-07 |
| PR-6 | `codex/mounted-alpha2-integration` | assembly of the accepted changes, private-alpha delivery and records |

**Scope of review.** This PR's entire diff against its target is under review, including code extracted from PR #64 at
`46444b30` (alpha.6). That code was reviewed seven times there and was **never accepted**; nothing in it is accepted by
virtue of its history. The first commit after the base is the extraction (no behavior change, described in section 5); later
commits are corrections. The reviewer is asked to review the inherited implementation from scratch, with the history below
as context, not as a verdict.

## 2. Requirements (verbatim from the original MISSION.md)

### MMR-01 — rider seating and mounting lifecycle

The rider appears unusually high above the horse; dismount/remount improves it. Diagnose first attachment, mounted-save restoration, view readiness/replacement, source-bone calibration, root-space offset, pose baseline, and attachment ownership. Distinguish a high attachment/root from a wrong skeleton pose. Use matched camera/profile/state observations when comparing height.

Make the supported Horse rider sit consistently without offset accumulation or stale calibration, including first mount and remount. Preserve clean unmounted restoration and stable rider/mount identities. A load/area/view transition that exercises the same lifecycle must not reintroduce a duplicate adapter or stale offset.

Do not begin with a global height reduction, scale change, or broad retuning of other mounts. The new owner report authorizes repairing this Horse seating defect; historical exhausted pose-tuning cycles do not prohibit a justified new repair. Change an existing calibration constant only after proving its coordinate basis and why lifecycle correction alone is insufficient. Preserve Mammoth and other profiles unless a demonstrated shared defect requires a narrowly justified correction.

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
| S02 | MMR-01 repair exercises actual attachment/pose baseline/calibration lifecycle; repeated acquire/release and view replacement do not retain a wrong baseline. | Unmounted pose restored; unrelated mounts/profiles unchanged absent a demonstrated shared defect. A numeric offset test alone is insufficient. |
| S08 | Existing Mounted Shield behavior tests and affected neighbors retained; icon identity checked in actual output. | Passive feat amount, AC descriptor/stacking/attack-type behavior, Mobility addition, registration rollback, equipment/lifecycle cleanup; do not rerun unrelated full matrices by rote. |

Owner-pending native observations for this area (planned, initially pending; never counted as passing without execution):

| ID | Observation | Essential comparison / negative case |
| --- | --- | --- |
| N01 | First mount, dismount/remount and mounted reload have consistent seating at matched camera angles. | Idle then short move/turn/stop; normal unmounted restoration. A replayed old screenshot is not new evidence. |
| N10 | One changed-lifecycle save/reload or area/view transition preserves a single pair and correct effects. | Existing pre-repair horse remains usable; no stale attachment/duplicate shield/extra action after restoration. |

S09 (package) and S10 (factory acceptance of the delivered alpha) belong to PR-6. Native gameplay, visual and save/load rows
N01-N10 stay pending for the owner's playtest unless performed through an available protected path; the vanished historical
save pair and a new game-driving framework are not prerequisites.

## 4. Historical findings carried by this PR

Source: `analysis-cache/.../decomposition/FINDINGS-CATALOGUE.json` (read-only copy of the cancelled enrollment's findings;
SHA-256 `2efabc22015ac145aba4199fbbaf7d36d0d9605b6331d8a2de02591698c12aa5`). These IDs belong to PR #64; this enrollment issues its own.

### 4.1 Carried OPEN findings (must be closed with the stated evidence)

#### R5-F4 (P1, blocking): Culled frames still exhaust the seat's recovery time

- Raised: round 5 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978); acceptance rows S02.
- Trigger: The mounting animation pauses, advances slowly, or blends across the sampler’s minimum frame count.
- Actual: The sampler counts render frames and compares each sample only with the immediately preceding frame. A held cast pose qualifies after 120 frames. A pelvis moving 1 mm per frame also qualifies after travelling 119 mm. Settled then prevents any later idle sample from correcting that choice.
- Expected: A stable calibration requires an advancing animation and a bounded pose change across the observation window.
- Requested correction: Exclude paused or unevaluated animation from settling and test stability across a whole window in animation or game time. Keep the provisional sample eligible for correction when animation resumes.
- Requested evidence: Hold a high cast pose through 120 render updates before resuming to idle, and separately feed sub-2 mm per-frame drift. Both should settle at the later stable pose and match the remount control.
- Last rejection: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791): The four-second frozen-pose case recovered in the gate. Unevaluated frames still consume the full thirty-second timeout, after which a resumed idle cannot correct the provisional seat.

#### R7-F2 (P2, blocking): The successful seat tests now require synthetic pelvis motion

- Raised: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791); acceptance rows S02.
- Trigger: Animation resumes at a lower resting pose whose pelvis stays at a fixed rider-local position.
- Actual: A transition from a high cast to a lower fixed pelvis starts a new window with zero evidence frames. A constant resting pelvis can never supply the three within-window changes required to apply it. This round replaced the earlier constant-idle fixtures with a synthetic 0.3 mm oscillation. The supplied installed-contract evidence gives no minimum idle pelvis displacement.
- Expected: A supported rider that reaches a stable idle after a cast converges to the remount seat, including when other bones animate and the pelvis stays still.
- Requested correction: Restore coverage for an evaluated, constant-height idle. Support that transition or establish an installed rig contract that excludes it for every profile using this sampler.
- Requested evidence: Drive the previous cast-to-constant-idle sequence through the attachment adapter and compare first mount with remount. Keep the frozen-high negative control and the breathing-idle case.

#### R7-F3 (P2, blocking): Later action poses can overwrite a correctly settled seat

- Raised: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791); acceptance rows S02.
- Trigger: The rider settles correctly, then holds two different poses during the correction interval.
- Actual: Once settled, the sampler applies any qualifying stable window more than 1 cm away. It has no input distinguishing idle from a held action pose. An idle at 0.95 m followed by two one-second, lightly animated holds at 1.20 m and 1.10 m consumes both corrections. Returning to 0.95 m then leaves the second hold's basis applied for the rest of the mount.
- Expected: A confirmed resting seat survives later casts and combat poses.
- Requested correction: Qualify later corrections against resting-pose evidence and preserve an established idle basis through temporary action poses.
- Requested evidence: Settle at idle, drive two distinct held action poses, then return to the original idle. Verify the final seat matches the idle remount and retain the existing cast-first recovery case.

Stopping rule (owner, 2026-11-25): if a carried open finding above, or this enrollment's successor finding for it, is
rejected in two consecutive rounds of this enrollment, this child stops after that round for an owner decision.

### 4.2 Verified findings: regression obligations

Each was verified in PR #64 by the independent reviewer; the behavior it describes must not be reintroduced by the extraction
or by any correction here, and the reviewer may reject a regression of any of them.

| ID | Title | Verified | Verifying assessment |
| --- | --- | --- | --- |
| R1-F12 | The new seat height and its advertised diagnostic lack supporting evidence | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | The fixture-derived height literal is gone. The normal attachment logger now emits observed and applied values, and the seat harness exercises those callbacks. R5-F3 and R5-F4 identify separate defects in the replacement measurement. |
| R5-F3 | Settled seating still contains the Chest height from the attachment frame | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | Calibration now combines the attachment-frame anchor-to-Chest relation with the current rider-local pelvis. The harness synchronized the rider root and obtained the same settled seat from two initial Chest heights 10 cm apart, including after view replacement. |

### 4.3 Out of scope here (owned elsewhere)

- MMR-02: PR-2 (`codex/mmr-tb-approach`).
- MMR-03: PR-1 (`codex/mmr-attack-orders`).
- MMR-04: PR-1 (`codex/mmr-attack-orders`).
- MMR-05: PR-1 (`codex/mmr-attack-orders`).
- MMR-06: PR-4 (`codex/mmr-horse-trip`).
- MMR-07: PR-5 (`codex/mmr-horse-bite`).
- Mission packet, version, package, HANDOFF/PRIVATE-ALPHA/ledger records, R1-F14/F15/F16, R2-F6, R3-F4, R7-F6: PR-6.

## 5. Extraction from PR #64

Method (owner instruction 2026-11-25 section 5): final content from `46444b30`, not a replay of PR #64's commits.

- **Whole files owned by PR-3, byte-identical to `46444b30` at the extraction commit `f67c3b16`:** `Domain/AnimatedSaddleCalibration.cs`,
  `Integration/MountedRiderPoseAdapter.cs`, `Integration/SupportedMountedProfile.cs`, `Integration/KingmakerMountedPairRuntime.cs`
  (its only alpha changes are the seat notice wiring and status line), `tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs`,
  `tests/Chunk7Diagnostics/AttachmentSlotRuntimeTemplate.cs`, `scripts/Test-MountedAlpha2SeatAttachment.ps1`.
- **Shared files:** `tests/MountedStockAttackPolicyTests.cs` gains the seat case; project files carry only PR-3's compile items.
- **Checks split by area, labels verbatim:** `scripts/Test-MmrHorseSeatNativeContract.ps1` = shared preamble + the alpha.6 native
  contract's MMR-01 section (12 checks), extended by this PR's correction (below).
- **Extraction proof:** the extraction commit was **not run on its own** (budget). Its seven source, test and harness files are
  byte-identical to `46444b30`; the rest is compile items and test registration. The first metered run is on the corrected tree.

## 6. Correction design for the carried open findings

**Native semantics inspected first** (installed Kingmaker 2.1.7b, read-only reflection and IL; records under
`analysis-cache/.../decomposition/seat-investigation/`). Each fact below is pinned by `Test-MmrHorseSeatNativeContract.ps1`:
- `UnitEntityView.OnDataAttached` sets every unit `Animator.cullingMode = 0` (**AlwaysAnimate**). Off-screen renderer culling
  therefore does not stop a unit skeleton's transform writes.
- `AnimationManager.OnEnable` creates and plays a `PlayableGraph` whose `AnimationPlayableOutput` is bound to that Animator, so
  the graph is evaluated by the Animator update while it plays. `Disabled` sets the default mixer's speed to 0 (StandUp,
  resurrection). Pause sets `Time.timeScale` 0.
- `UnitAnimationManager.CanRunIdleAction` (public) is the native idle stage: animation `Speed <= 0`, and every active action is
  the locomotion handle, additive or released. Micro-idles and variant idles are run as actions through `ExecuteIfIdle`, so they
  are not idle while they play.
- `UnitAnimationController.TickOnUnit` sets the animation `Speed` from the view's movement agent `Speed`. KMC's
  `RiderMovementAgent.UpdateVelocity` reports the mount agent's `Speed`, so the pair moving is not native idle. That includes an
  unstarted approach that already moves; a command that only waits plays no action and does not move.

**Three separate facts per frame.** Domain: `SettledSeatSampler.Observe(sample, evaluatedAdvance, restEligible)`. Adapter:
`MountedRiderPoseAdapter`.
1. *Fresh evaluation with advancing time* (`SeatAnimationAdvanceSeconds`): the manager and Animator are active and enabled, the
   manager is not `Disabled`, the culling mode is `AlwaysAnimate`, and the graph is valid and playing. The value is then
   `Time.deltaTime` if positive, else 0.
2. *Rest eligibility* (`SeatRestEligible`): native `CanRunIdleAction()`, and no active animation of a released, non-additive
   handle with weight > 0 (a transition still blending out).
3. *Stability* (sampler): eligible samples within 2 mm of the window's first sample for 0.5 s of evaluated time, after 2 s of
   evaluated time since attachment.

**Sampler behaviour:**
- A window confirms its mean **once**, and the confirmed reference is never overwritten (R7-F3). Movement, attacks, casts,
  idle variants and transitions are ineligible, and nothing after confirmation can replace the reference.
- Unevaluated or non-advancing frames, and ineligible frames, consume nothing and end the current window. A fresh window
  begins when evaluation and eligibility return (R5-F4).
- There is no give-up: after 30 s of evaluated time without a settle, `phase=unsettled` is logged once and sampling continues.
- No motion is required: a perfectly stationary evaluated idle is a positive case (R7-F2). The earlier frame-to-frame
  "evaluation evidence" rule and the post-settle corrections are removed.
- Preserved: the normalized coordinate fix (R5-F3: attach-frame anchor relation + rider-local pelvis), dismount restoration,
  view replacement with one adapter, Small/Mammoth profiles and the mechanics anchor.

**Evidence: seat harness, 16/0**, on the ACTUAL runtime/adapter members. The facade carries the installed member shapes;
the harness drives the inputs of `CanRunIdleAction`, not its result.
- **R5-F4:** a high pose held 31.5 s with the Animator disabled while game time advances does not settle and ignores all 1890
  frames. Once evaluation and rest return, it settles on idle equal to the remount control.
- **R5-F4:** 31.5 s of an evaluated cast (action active) logs `unsettled` once, then the resting idle settles equal to the
  control (not exhausted).
- **R7-F2:** a cast followed by a completely stationary idle settles exactly on it; first mount equals the remount control
  (exact). Controls: a frozen-high unevaluated pose does not settle; a breathing idle settles on its mean.
- **R7-F3:** confirmed idle, then held action poses at 1.20 m and 1.10 m (lightly animated, one second each), then idle. The
  applied reference is unchanged, the final seat equals the idle remount, and no line beyond attach/settled is logged.
- **Eligibility from native stages:** a waiting order (no action, standing) settles; the pair moving on an unstarted approach and
  a released action blending out do not; the seat settles once both end.
- **Retained cases:** the cast-first settle with its log lines, dismount restoration, first mount/remount/mounted-load bind pose,
  view replacement, the R5-F3 Chest-offset cases (also after view replacement), paused/disabled frames, cumulative drift versus
  a stable window, a restless pose that never settles (now with recovery after the notice), the refusal of a second
  configuration, and the Mammoth/Small controls.

**Further evidence:**
- Component case (sampler): stationary idle, breathing, drift, 31.5 s unevaluated, 31.5 s ineligible with one notice, an
  ineligible held pose, a final confirmed reference, consecutive windows across interruptions, pause, and NaN.
- Native contract: 19/0 (12 retained, 7 new).

**Limitations, stated plainly:**
- Not observed in game. Unity's documented `AlwaysAnimate` behaviour is relied on; the harness cannot run Unity.
- The confirmed reference is the first qualifying native idle stance. If the rider's combat idle and out-of-combat idle differ in
  pelvis height, a first mount in one stance and a remount in the other differ by that amount; compare them in a matched stance
  (MISSION: matched state). NX-1 asks the owner for this observation.
- A rider looping idle variants delays the settle (variants are actions); it keeps the attachment sample until a base idle holds.

## 7. Validation

- Focused gate: `verify --gate focused -- -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1`. It runs build, components,
  Validate-Source, `Test-MmrHorseSeatNativeContract.ps1`, `Test-MountedAlpha2SeatAttachment.ps1` and
  `chunk7/Test-Chunk7AttachmentSlotRuntime.ps1`, plus the shared regression neighbors: Mounted Shield native contract and
  rollback with icon identity, Archery, Ranger/Riding Dog, replay subscriber, localization, UMM 0.31.1 build reference, native
  loader, frozen source, UMM loader 0.31.1/0.33.0.
- Development runs (metered):
  - components 1526/0; Validate-Source 153/0; seat harness 16/0; slot harness 8/0; seat native contract 19/0; neighbors pass.
  - Two earlier runs failed on test mistakes, both corrected: a drift tolerance tighter than the stability bound, and an
    interruption placed after the settle point.

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
