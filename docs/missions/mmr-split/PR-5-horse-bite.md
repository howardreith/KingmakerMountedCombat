# Child mission: PR-5: Horse bite binding and presentation diagnostics (MMR-07)

Branch `codex/mmr-horse-bite`, base `main`. Project `howardreith/KingmakerMountedCombat`. Plan `single`, foreground.
Formal review allowance for this enrollment: **2 rounds** (`--max-rounds 2`), a ceiling, not a target.

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
| PR-3 | `codex/mmr-horse-seat` | MMR-01 |
| PR-4 | `codex/mmr-horse-trip` | MMR-06 |
| PR-5 (this mission) | `codex/mmr-horse-bite` | MMR-07 |
| PR-6 | `codex/mounted-alpha2-integration` | assembly of the accepted changes, private-alpha delivery and records |

**Scope of review.** This PR's entire diff against its target is under review, including code extracted from PR #64 at
`46444b30` (alpha.6). That code was reviewed seven times there and was **never accepted**; nothing in it is accepted by
virtue of its history. The first commit after the base is the extraction (no behavior change, described in section 5); later
commits are corrections. The reviewer is asked to review the inherited implementation from scratch, with the history below
as context, not as a verdict.

## 2. Requirements (verbatim from the original MISSION.md)

### MMR-07 — visible native horse bite

The owner sees hoof animation but no bite animation. The repository already has `HorsePrimaryAttackAnimationAdapter` and historical native `HorseAnimationSet_Bite` evidence. The ordinary attack path reaches the adapter. Inspect the actual planned limb, special animation, selected action/variant/clip, handle creation/adoption/refresh, animator playback/blending, interruption, and attack event.

Connect the correct installed Kingmaker horse action to the real bite so it is readable at a normal play camera, retaining facing and the working hoof sequence. Native attack code must remain the sole owner of roll, damage, timing, and expenditure. An acted handle without a visible bite is not sufficient visual acceptance. Do not generate an extra attack event, stall the routine for cosmetic timing, remove the bite, or label a hoof kick as a new bite.

If fresh asset inspection establishes that no usable native bite presentation exists for the installed rig, deliver the bounded evidence and a precise visual limitation. A documented unavailable visual asset may remain optional for this private alpha; missing investigation or a broken existing playback path is not the same exception. Do not build a new animation/retargeting pipeline, import Wrath assets, or redistribute extracted game assets in this mission.

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
| S07 | MMR-07 real ordinary-attack limb/action/handle path investigated and repaired where supported; stock handle preserved or exact fallback supplied once. | Native event/damage count/timing and hooves unchanged; visual acceptance pending until viewed. A verified unavailable asset is an explicit limited exception. |
| S08 | Existing Mounted Shield behavior tests and affected neighbors retained; icon identity checked in actual output. | Passive feat amount, AC descriptor/stacking/attack-type behavior, Mobility addition, registration rollback, equipment/lifecycle cleanup; do not rerun unrelated full matrices by rote. |

Owner-pending native observations for this area (planned, initially pending; never counted as passing without execution):

| ID | Observation | Essential comparison / negative case |
| --- | --- | --- |
| N08 | Visible native bite corresponds to the logged real bite at normal zoom; hooves still animate. | No duplicate damage/roll, stuck handle, artificial delay, rider detachment or animation-only fake attack. |

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
| R1-F13 | The bite checks exercise the formatter instead of handle adoption and refresh | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | The gate now exercises the production binding and strike reader with genuine detached handle, action and manager objects. It covers adoption, same-action refresh, initial fallback, mismatches and a hoof control. R4-F7 concerns a different state transition in that binding. |
| R2-F4 | Every mount's natural attack is logged as a Horse strike | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | ObserveHorseStrike checks the action actor, exact current mount and KMC Horse blueprint GUID before emitting the Horse-labelled record. |
| R4-F7 | A missing handle after an earlier binding can fault the horse attack | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | MissingAfterRecorded returns before fallback creation, assignment and registration. The component and installed-object checks cover both prior handle sources and a later hoof. Visible bite playback remains N08. |

### 4.3 Out of scope here (owned elsewhere)

- MMR-01: PR-3 (`codex/mmr-horse-seat`).
- MMR-02: PR-2 (`codex/mmr-tb-approach`).
- MMR-03: PR-1 (`codex/mmr-attack-orders`).
- MMR-04: PR-1 (`codex/mmr-attack-orders`).
- MMR-05: PR-1 (`codex/mmr-attack-orders`).
- MMR-06: PR-4 (`codex/mmr-horse-trip`).
- Mission packet, version, package, HANDOFF/PRIVATE-ALPHA/ledger records, R1-F14/F15/F16, R2-F6, R3-F4, R7-F6: PR-6.

## 5. Extraction from PR #64

Method (owner instruction 2026-11-25 section 5): final content from `46444b30`, not a replay of PR #64's commits.

- **Whole files owned by PR-5, byte-identical to `46444b30`:** `Domain/HorseStrikeHandleBinding.cs` (adoption, same-action
  refresh, exactly-once recording, single-action fallback, every rejection, `MissingAfterRecorded`), `Domain/HorseStrikeDiagnostic.cs`,
  `Integration/HorsePrimaryAttackAnimationAdapter.cs` (real-type `HandleBoundary`, `SupplyExact` containing binding faults),
  `tests/HorseStrikeHandleBindingTests.cs`, `scripts/Test-MountedAlpha2BiteContract.ps1`.
- **Shared files, PR-5's exact contributions applied to `main`:**
  - `Integration/MountedPairAttackCommand.cs`: outcome fields `AttackAnimationSpeedScale` and `RoutineAttackCount`,
    `RecordedHorsePrimaryAnimationAction`, the `ObserveHorseStrike()` call at the start of `OnAction` (base `OnAction` still runs
    exactly once) and the `HorseStrikeSource` reader source.
  - `Integration/MountedCombatController.cs`: the `horseBite={...}` fields of the pair command terminal line.
  - `tests/MountedStockAttackPolicyTests.cs`: the `HorseStrikeLines` formatter case; project files and `tests/Program.cs`: only
    PR-5's compile items and registration.
- **Checks split by area, labels verbatim:** `scripts/Test-MmrHorseBiteNativeContract.ps1` = shared preamble + the MMR-07 rows of
  the alpha.6 native contract (handle `SpeedScale`; pinned native horse animation bundle identity).
- **Extraction proof at the extraction commit:** metered Fast + focused run: build clean; components 1534/0; Validate-Source 153/0;
  bite native contract 3/0; bite contract 28/0 (as at alpha.6); presentation 239/0; regression neighbors all pass (14 of 14).

**Disposition boundary for MMR-07 (owner instruction 2026-11-25 section 8).** Three separate facts, never merged:
1. *Binding/handle safety* (source, offline evidence): the exact Horse Bite handle is adopted, refreshed with the same action,
   recorded exactly once, supplied once when native planned none, and never assigned twice; faults never reach the native postfix.
2. *Diagnostic evidence* (logged at play time): one `Horse strike` line per native Horse strike (limb, weapon, special animation
   type, handle action/state/SpeedScale/variant, active clip/weight/state) and the terminal `horseBite={...}` fields. The installed
   `HorseAnimationSet_Bite` -> `BiteAttack` asset exists (pinned bundle). The native routine compresses every limb clip into one
   window with one SpeedScale (UnitAttack.ConfigureAnimations); whether that makes the bite unreadably fast is an open question these
   lines are designed to answer.
3. *Visible playback* (owner observation N08, targeted playtest NX-2): **not observed**. A logged bite does not complete visual
   acceptance; this PR claims no visual result.

## 7. Validation

- Focused gate: `verify --gate focused -- -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1` (build, components, Validate-Source,
  then `Test-MmrHorseBiteNativeContract.ps1`, `Test-MountedAlpha2BiteContract.ps1`, `Test-MountedPresentationContracts.ps1` and the
  shared regression neighbors: Mounted Shield native contract and rollback with icon identity, Archery, Ranger/Riding Dog, replay
  subscriber, localization, UMM 0.31.1 build reference, native loader, frozen source, UMM loader 0.31.1/0.33.0).
- No carried open findings: no new behavior test is required beyond keeping R1-F13, R2-F4 and R4-F7 green.

- Validation budget: the existing ID `mounted-alpha2-melee-pose-20261124` (90-minute allowance, not renewed). Its record was
  reconciled on 2026-11-25 by importing the previously unmetered direct runs (upper bound); every later direct run is
  metered and imported. Before each formal review the developer checks both this child's round allocation and the program
  totals in the supplemental ledger. No new budget ID is created.
- Native testing: none is required for this checkpoint; any observation must come from an actual protected run and be
  reported as such. Offline harnesses state `nativeProof=false`.

## 8. Limits and records

- This enrollment: `--max-rounds 2`; 3 attempts per round; the controller's 3-round no-progress rule.
- Program (owner, 2026-11-25): 7 formal rounds spent in PR #64; 15 additional across the six child enrollments (PR-1 3, PR-2 3,
  PR-3 3, PR-4 2, PR-5 2, PR-6 2); 22 in total. No redistribution, cap extension or replacement enrollment without further owner
  authorization; one enrollment active at a time.
- Supplemental operator ledger (not controller output, not enforced by the controller):
  `analysis-cache/mounted-alpha2-melee-pose-20261124/decomposition/TRANSITION-LEDGER.jsonl`, pinned here by its immutable prefix:
  8 lines, SHA-256 `9f9dbc5acb0474acaee8ce10d4d3ac6cfa0df33f44a81bad05f68679ba1bcfff`. Later appends do not change this prefix.
- No merge, tag, release or permanent installation. The PR stays draft for owner review.
