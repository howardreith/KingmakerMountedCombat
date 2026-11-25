# Child mission: PR-4: Horse Trip construction and existing-save repair (MMR-06)

Branch `codex/mmr-horse-trip`, base `main`. Project `howardreith/KingmakerMountedCombat`. Plan `single`, foreground.
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
| PR-4 (this mission) | `codex/mmr-horse-trip` | MMR-06 |
| PR-5 | `codex/mmr-horse-bite` | MMR-07 |
| PR-6 | `codex/mounted-alpha2-integration` | assembly of the accepted changes, private-alpha delivery and records |

**Scope of review.** This PR's entire diff against its target is under review, including code extracted from PR #64 at
`46444b30` (alpha.6). That code was reviewed seven times there and was **never accepted**; nothing in it is accepted by
virtue of its history. The first commit after the base is the extraction (no behavior change, described in section 5); later
commits are corrections. The reviewer is asked to review the inherited implementation from scratch, with the history below
as context, not as a verdict.

## 2. Requirements (verbatim from the original MISSION.md)

### MMR-06 — unintended horse Trip

The supplied log shows a Horse Bite hit and damage, followed by a Trip maneuver. A plain Pathfinder horse companion has no inherent bite-triggered Trip; wolves and riding dogs legitimately have it. `RULES_AND_ASSETS.md` includes primary sources and actual construction details.

Bind the maneuver to the actual horse blueprint/instance, command, weapon, originating fact/component, and any relevant feat/item/buff/mod. Determine whether it is an unintended automatic trigger or a legitimately granted/selected maneuver. Do not assume a wolf clone, donor contamination, or a shared weapon bug without evidence.

Remove any unintended intrinsic Horse trigger at its narrow ownership/source boundary. Do not mutate the shared stock bite globally or veto every Trip by horse-type creatures. Preserve wolf/dog/Riding Dog trip, legitimate explicit maneuvers and separately granted effects. Preserve the horse's actual successful bite and damage; zero attacks is not a passing no-Trip test.

Consider existing created horses as well as fresh creation. If the defect persists as a saved fact, repair only the attributable KMC-owned or erroneously inherited fact with an idempotent, evidence-based cleanup; do not erase learned feats, foreign buffs, unrelated companions, or rewrite arbitrary save content. Do not undertake a general horse-stat or tabletop-maneuver rewrite.

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
| S06 | MMR-06 triggering source identified or tightly isolated; plain horse can bite successfully without unintended intrinsic Trip. Fresh and persisted defect paths considered. | Wolf/dog/Riding Dog and legitimate independently granted/selected maneuvers still work; no global shared-bite mutation or deletion of arbitrary facts. |
| S08 | Existing Mounted Shield behavior tests and affected neighbors retained; icon identity checked in actual output. | Passive feat amount, AC descriptor/stacking/attack-type behavior, Mobility addition, registration rollback, equipment/lifecycle cleanup; do not rerun unrelated full matrices by rote. |

Owner-pending native observations for this area (planned, initially pending; never counted as passing without execution):

| ID | Observation | Essential comparison / negative case |
| --- | --- | --- |
| N07 | Ordinary plain-horse bite hits without unintended automatic Trip; natural attacks still occur. | Legitimate dog/wolf trip and explicit/granted maneuver preserved if that native control is available. |
| N10 | One changed-lifecycle save/reload or area/view transition preserves a single pair and correct effects. | Existing pre-repair horse remains usable; no stale attachment/duplicate shield/extra action after restoration. |

S09 (package) and S10 (factory acceptance of the delivered alpha) belong to PR-6. Native gameplay, visual and save/load rows
N01-N10 stay pending for the owner's playtest unless performed through an available protected path; the vanished historical
save pair and a new game-driving framework are not prerequisites.

## 4. Historical findings carried by this PR

Source: `analysis-cache/.../decomposition/FINDINGS-CATALOGUE.json` (read-only copy of the cancelled enrollment's findings;
SHA-256 `2efabc22015ac145aba4199fbbaf7d36d0d9605b6331d8a2de02591698c12aa5`). These IDs belong to PR #64; this enrollment issues its own.

### 4.1 Carried OPEN findings (must be closed with the stated evidence)

#### R7-F5 (P2, blocking): A failed archive rename can reopen a terminal Trip repair decision

- Raised: round 7 (https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791); acceptance rows S06.
- Trigger: The result is published, archiving the claim fails, and the loaded horse or candidate set changes before the retry.
- Actual: Only an applied removal is recorded as a terminal outcome in the session. If writing a refusal succeeds and its final rename fails, HoldsClaim remains true. The next poll calls DecideClaimed again. An ambiguous horse that now has one candidate can lose that fact under the request already reported as refused. A reported supersession can likewise be reconsidered if its rename fails.
- Expected: Once a terminal decision is made, a reporting retry completes that decision without selecting a fact again.
- Requested correction: Remember terminal refusals and supersessions before reporting. Retry selection only for an undecided claim, and retry only reporting for a decided claim.
- Requested evidence: Inject FailClose after an ambiguous refusal, reload the same horse ID with one candidate, and verify zero removals and a refused archive. Exercise failed supersession reporting with the newer request present, alongside the existing pre-removal and post-removal controls.

Stopping rule (owner, 2026-11-25): if a carried open finding above, or this enrollment's successor finding for it, is
rejected in two consecutive rounds of this enrollment, this child stops after that round for an owner decision.

### 4.2 Verified findings: regression obligations

Each was verified in PR #64 by the independent reviewer; the behavior it describes must not be reintroduced by the extraction
or by any correction here, and the reviewer may reject a regression of any of them.

| ID | Title | Verified | Verifying assessment |
| --- | --- | --- | --- |
| R1-F1 | Saved-Horse cleanup treats feature order as proof of grant ownership | round 4 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7144494607)) | Feature adjacency no longer triggers deletion. Existing matching facts survive ordinary load, and removal requires an explicit horse ID, the exact stock Bite-to-Trip shape, one matching instance and the shared write gate. R4-F4 through R4-F6 concern the newly introduced request mechanism. |
| R1-F2 | The live repair bypasses the root's enable and persistence gates | round 3 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7139773088)) | The composition root now supplies the enable, save-suspension, area-transition, restoration and save-worker gate. The service also checks native load state and records completion per loaded object only after a successful pass. |
| R4-F4 | A repair request naming two horses silently selects the first | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | The parser validates the whole request. Tests cover two horse lines, trailing malformed input, and a single valid ID with comments, including zero removals for malformed requests. |
| R4-F5 | A reporting failure can turn a completed fact removal into a recorded refusal | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | After removal, the claimed file and session outcome survive reporting failures. The filesystem tests exercise result-write and final-rename retries without a second removal, plus explicit interrupted reporting across sessions. R5-F1 concerns selection before claim. |
| R4-F6 | Automatic candidate reporting writes into the guarded Mods tree | round 5 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7145428978)) | Discovery logs the candidate. Request and result files use the persistence data parent, including its isolated form. The staged-directory fixture checks that its mod-tree contents stay identical. |
| R5-F1 | Trip repair can remove one horse’s fact while consuming another horse’s request | round 6 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7146735787)) | Selection, removal, refusal, and archival now use the claimed file's bytes. Real-folder replacement cases covered A changing to B and valid content changing to malformed content. R6-F4 concerns recovery when reading or selecting from that claim fails. |
| R6-F4 | A transient error before Trip removal closes the request as interrupted | round 7 ([review](https://github.com/howardreith/KingmakerMountedCombat/pull/64#issuecomment-7147421791)) | The session records ownership immediately after claiming. The recorded real-folder checks injected read and lookup failures, preserved the claimed bytes through a closed gate, and then applied exactly once. A restarted session retained conservative interruption handling. R7-F5 concerns retries after a terminal decision. |

### 4.3 Out of scope here (owned elsewhere)

- MMR-01: PR-3 (`codex/mmr-horse-seat`).
- MMR-02: PR-2 (`codex/mmr-tb-approach`).
- MMR-03: PR-1 (`codex/mmr-attack-orders`).
- MMR-04: PR-1 (`codex/mmr-attack-orders`).
- MMR-05: PR-1 (`codex/mmr-attack-orders`).
- MMR-07: PR-5 (`codex/mmr-horse-bite`).
- Mission packet, version, package, HANDOFF/PRIVATE-ALPHA/ledger records, R1-F14/F15/F16, R2-F6, R3-F4, R7-F6: PR-6.

## 5. Extraction from PR #64

Method (owner instruction 2026-11-25 section 5): final content from `46444b30`, not a replay of PR #64's commits.

- **Whole files owned by PR-4, byte-identical to `46444b30` at the extraction commit `1430b081`:** `Domain/HorseTripRepairPolicy.cs`,
  `Domain/HorseInheritedFactRepairPolicy.cs`, `Domain/MountedWorldWriteGate.cs`, `Integration/HorseCompanionBlueprintService.cs`,
  `Integration/HorseCompanionBlueprintService.InheritedFacts.cs`, `Integration/NativePersistenceIsolation.cs`, `CompositionRoot.cs`
  (its only alpha changes are the MMR-06 write gate and repair folder), `tests/HorseTripRepairPolicyTests.cs`.
- **Shared files:** `tests/MountedStockAttackPolicyTests.cs` gains the `SavedHorseRepairLedger` case unchanged; project files and
  `tests/Program.cs` carry only PR-4's compile items and registration.
- **Checks split by area, labels verbatim:** `scripts/Test-MmrHorseTripNativeContract.ps1` = the alpha.6 native contract's MMR-06
  sections (installed Mammoth/Dog/Wolf data at pinned offsets; inherited-fact filter, repair-candidate predicate, the single
  owner-request removal path, the user-data folder, no file operation in the Horse service) with the shared preamble: 43 checks.
- **Extraction proof at `1430b081`:** metered Fast run: build clean; components 1536/0; Validate-Source 153/0; Trip native contract 43/0.

## 6. Correction design for the carried open findings

**R7-F5: terminal decisions are frozen before reporting** (`Domain/HorseTripRepairPolicy.cs`, no new framework).

`HorseTripRepairSession` now holds this process's one claim as a record tied to the claimed file's **exact bytes**, with a stage:
`Undecided -> Decided -> Reported -> Archived` (`HorseTripClaimStage`).

- Only an **Undecided** claim may select candidates or authorize a removal (`DecideClaimed` is reached only from that stage).
- A **refusal** (malformed, no candidate, ambiguous) or **supersession** is frozen (`Decide`: outcome, result text, archive suffix)
  before any publication I/O.
- An **application** is frozen as soon as the removal is **verified** (`StillPresent` false), including when `Remove` threw after the
  fact was removed. A removal that leaves the fact present stays undecided (the claim is returned, or superseded by a newer request)
  and the failure propagates, also when `Remove` returned without effect.
- `Publish` writes the result (stage `Reported`) and then archives the claim (stage `Archived`). A failure leaves the stage where it
  was. The next poll for the same bytes retries **only** the step that remains and returns `FinishedApplied` or the new
  `FinishedDecided`. It never selects candidates again, never removes again, and consumes no other request while the claim file
  exists.
- Unchanged: claim-first selection from the claimed bytes (R5-F1); transient read/lookup retry of this process's undecided claim
  (R6-F4; the claim is recorded immediately after the rename and its bytes are bound at the first successful read);
  conservative restart handling (a claim this process does not hold is closed as `.interrupted` with nothing removed); exact-horse
  selection; the provenance predicate; foreign Trip effects (dog, wolf, Riding Dog, granted or sourced facts) are never candidates.

**Evidence** (new component case `TerminalDecisionsStayTerminal`, on the existing real-folder test host with fault injection):
- Ambiguous refusal with a failed archive, then the horse is reloaded with ONE candidate. Result: zero removals and a `.refused`
  archive holding the claimed bytes (the reviewer's requested case).
- No-candidate refusal with a failed result write, then a candidate appears. Result: only the refusal is reported.
- Supersession with a failed result write while the newer request is present, then the waiting horse loads with a candidate.
  Result: the superseded claim is archived unchanged; the newer request is untouched until then and is then applied once on its
  own horse.
- Supersession with a failed archive. Result: the retry only archives.
- Application with a failed result write, then a reload restores the fact. Result: `FinishedApplied`, no second removal.
- Application with a failed archive, then the horse is unloaded. Result: `FinishedApplied`.
- Removal completes although `Remove` throws, plus a failed result write. Result: frozen as applied, then finished once.
- Restart control: a decided-but-unarchived claim seen by a new session is closed `.interrupted` with nothing removed.
- All earlier lifecycle cases still pass unchanged (R4-F4, R4-F5, R5-F1, R6-F4, pending/retry, negative cases).
- Not executed: a mutation run reverting the fix (the pre-fix code re-decides the reloaded horse in the first case by
  construction); no native run.

## 7. Validation

- Focused gate: `verify --gate focused -- -Tier Fast -Focused Test-MmrHorseTripFocused.ps1` (build, components including
  `tests/HorseTripRepairPolicyTests.cs`, Validate-Source, then `Test-MmrHorseTripNativeContract.ps1` and the shared regression
  neighbors: Mounted Shield native contract and rollback with icon identity, Archery, Ranger/Riding Dog selection, replay subscriber,
  localization, UMM 0.31.1 build reference, native loader, frozen source, UMM loader 0.31.1/0.33.0).
- Development run after the correction (metered): components 1537/0; Validate-Source 153/0; Trip native contract 43/0; focused 12 of 12.

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
