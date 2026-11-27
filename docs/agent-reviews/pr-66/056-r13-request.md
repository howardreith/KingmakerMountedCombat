<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r13-request part=1/1 sha256=70fe33f0dc854e6ca86e2f1416f787086c56b207eed362b585b80e94cf02f7a6 -->
### Claude Opus — review request — round 13 — candidate `69623d76c1`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r13-574a99` |
| Checkpoint | `final` (final) |
| Candidate H | `69623d76c1eda12f3d3fc50cc438cf81e93579c5` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-4-horse-trip` sha256 `29184130a7` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | revision 1, sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28` |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 13 of 14 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-4 round 13 under OWNER AMENDMENT 1 (prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28). Round 12 accepted all nine rebuttals; this round corrects the confirmed in-game repair action's lifecycle (consent dropped on disable, failure stop, unload and party loss; session keyed to the loaded area state), identity (stable id required and rechecked; confirmed name recorded), ordering (consent executed before presentation refresh; every refusal a terminal record), truthful outcome text, the companion-name evidence for the repair-only panel, and the evidence path (the shared HorseTripRepairUpdater the service and the tests both drive). The new-horse filter, candidate predicate, gate and exact removal are unchanged; legacy repair files stay inert.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-trip-20261125213249-4b8b --gate focused -- -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 | yes | pass | 37.5 s | Receipt gate-focused-69623d76c1ed-d43d449b6c4-a3357fdbeb8-20261127T164151634698Z; nativeProof=false. Also metered: build, components 1541/0, trip native contract 51/0, Validate-Source 153/0, Finalize5 companion readers. UI appearance and gameplay save/load pending the owner's integrated-alpha playtest. |

**Evidence references:**
- prepared under owner amendments revision 1 sha256 f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28
- docs/missions/mmr-split/owner-authorization-2026-11-27b.md
- docs/missions/mmr-split/PR-4-repair-action.md
- docs/missions/mmr-split/PR-4-amendment-test-mapping.md
- src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs (HorseTripRepairUpdater, HorseTripRepairConsole, HorseTripRepairRecord)
- src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs
- src/KingmakerMountedCombat/CompositionRoot.cs
- tests/HorseTripRepairPolicyTests.cs
- scripts/Test-MmrHorseTripNativeContract.ps1
- scripts/Test-Finalize5CompanionReaders.ps1
- controller gate receipt gate-focused-69623d76c1ed-d43d449b6c4-a3357fdbeb8-20261127T164151634698Z (pass)

**Prior findings to assess:** `R12-F1`, `R12-F10`, `R12-F2`, `R12-F3`, `R12-F4`, `R12-F5`, `R12-F6`, `R12-F7`, `R12-F8`, `R12-F9`

**Owner amendments to the mission — revision 1, captured-source sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`.** Applied by the owner with `resume --amend-mission`; each owner message is preserved verbatim, in order. The effective task is the original mission plus these amendments. The reported work comes from a developer invocation whose input included them, and the reviewer of this round receives a byte-exact copy of the captured document. Public rendering after the controller's standard sanitization; the digest identifies the private captured bytes, which this rendering can differ from.

```text
# Owner amendments to the mission

Cumulative record kept by the agent-review controller. Each owner message below was applied by the owner through `resume --amend-mission` and is preserved verbatim, in order, between its labeled marker lines. Later amendments supersede conflicting earlier requirements; requirements they do not change still apply.

----- BEGIN OWNER AMENDMENT 1 (captured 2026-11-27T15:28:12Z; message sha256 d98eab757e164fe411777b8f2061932ecea5d45450d4bdfaadbef63d06492a34) -----

Continue the existing Mounted Combat remediation program toward the complete integrated private alpha. Preserve full MMR-01 through MMR-07 functionality, the six-child decomposition, existing enrollments, findings, review history and cumulative spending.

This is new owner authority beyond the 2026-11-27a instruction. It authorizes implementation and the bounded reviews below. Do not stop after producing another plan.

The principal change is an explicit amendment to PR-4: replace its file-command repair protocol with a deliberate, confirmed in-game repair action while preserving the complete new-horse and existing-horse functionality.

## 1. Preserve accepted work and record this instruction

Keep these accepted checkpoints frozen:

- PR-1 / #65:
  - Reviewed source H: `aa15e1f2277364ed9dc2c2c322462fa4a9166322`
  - Archive A: `f7edd29830a8b89567b6d82a1e5db671ee4b2d2a`
- PR-3 / #68:
  - Reviewed source H: `8bfe701d448325b1d50c6f4ff2619946a8174eb9`
  - Archive A: `54311f1d0fd293028decc148c57a4b9f63e79760`

Do not reopen accepted work for optional wording or request another approval of these checkpoints.

Save this instruction verbatim as a new ordinary tracked owner-authorization record outside controller transcript directories. Preserve the existing ledger and append only necessary new entries. Do not repeat historical accounting reconstruction without a concrete discrepancy.

## 2. Review and validation authority

I authorize the following total allocations:

| Work item | Total allowance |
|---|---:|
| PR-1 attack orders | 15, unchanged and accepted |
| PR-2 turn-based approach | 5, unchanged |
| PR-3 seating | 5, unchanged and accepted |
| PR-4 Trip | 14, increased from 11 |
| PR-5 Bite | Existing completed-round cap 9 and execution ceiling 10, including the known invalidated/replacement-review accounting |
| PR-6 integration | 2, unchanged |
| Transport repair, development-process PR #8 | 6 total independent review executions, increased from 4 |

The resulting execution ceilings are 51 child reviews, 58 MMR lifetime reviews including PR #64’s seven, and six infrastructure reviews: 64 combined. Preserve every execution already spent, including Bite’s unsettled round seven and all four transport reviews. Unused allocations are not transferable.

I explicitly authorize the supported resume of PR-4’s current 11-of-11 cap stop, on its existing enrollment, to a total cap of 14.

PR-2 has already been resumed to five. Verify its current state; do not perform a redundant resume.

The cancelled infrastructure enrollment remains cancelled. Continue the existing transport branch and PR through the already-authorized independent review procedure. Do not recreate an enrollment.

Increase the existing shared validation allowance from 300 to 360 minutes using the locked allowance-adjustment mechanism and this authorization:

`mounted-alpha2-melee-pose-20261124`

Retain all cumulative spend and meter builds, tests and direct diagnostic runs. Do not create another budget ID. The reported 264.6-minute spend is the starting reference; use the actual ledger balance.

Use focused checks during correction and the required complete gate on each stable submission candidate. Do not repeatedly run the approximately 18-minute transport suite after individual parser edits. Do not substitute focused tests for a mandatory final gate or reuse an earlier candidate’s receipt after relevant source changes.

## 3. Complete PR-2 under its existing authorization

Use the existing enrollment:

`kmc-codex-mmr-tb-approach-20261126213805-50cc`

Correct the explanation in the 27a report. Ledger entry 150 records a supported resume from a no-progress stop to ACTIVE, 3 of 5. The pinned controller’s `_resume_updates` resets consecutive-open streaks after that type of resume; it does not erase findings, history or spent reviews.

Read the actual pinned status and relevant counters. If they disagree with the documented transition, investigate and report that specific discrepancy without manually editing state. Otherwise proceed with the two available reviews. The separate unchanged-candidate stop is not a reason to withhold genuine corrections.

Close the remaining cases together before formal submission:

- **R1-F10:** exercise production pairing, actor admission and movement context through the harness. Do not manufacture the expected pairing eligibility in fixture flags. Cover both initiative arrangements and the distinction between an established pair and adoption after the horse has already acted.
- **R1-F6:** derive the first-tick/single-move allowance from the production context and verify that admission and actual spending agree.
- **R1-F7:** bind proof to its producing click. A later same-target click must not adopt proof from an earlier click whose native handler never ran.
- **R1-F9:** validate the actual live command’s attack plan at dispatch, alongside the click proof and current native forecast. A newly constructed probe alone is insufficient.
- **R2-F3:** validate the route’s actual arrival heading; do not reject a usable lateral route solely because it falls between the guessed headings.

Keep tests connected to production behavior. Simulate the external engine boundary where necessary, rather than recreating the algorithm under test.

The implementation and its focused verification are already authorized. Submit when the known corrections and required evidence are complete.

## 4. Owner amendment: replace PR-4’s repair-file protocol

The full product requirement remains:

- Newly created KMC horses do not inherit the erroneous stock Bite-to-Trip grant.
- Existing affected horses can be repaired through a deliberate owner action.
- Repair is narrowly bound to one horse and one eligible fact.
- Legitimate sourced or modified facts, other companions, bite/hoof attacks and protected neighboring features remain intact.
- The repaired live state persists through ordinary save/load.
- Supported-platform functionality, including Linux, remains in scope.

I withdraw the requirement to initiate this repair through a filesystem request and to implement a persistent claim/archive/publication/recovery protocol for that request.

I authorize replacing that interface with a small, confirmed in-game maintenance action. This expressly permits a repair-only UMM action surface or the existing native-confirmation/shortcut pattern despite the prior no-options-page convention. Choose the smallest clear integration with the existing UI. Do not introduce a general settings framework or new dependency.

Apply this as a genuine controller mission amendment on the existing PR-4 enrollment:

`kmc-codex-mmr-horse-trip-20261125213249-4b8b`

Use supported operations separately:

1. Verify the current stopped state and absence of an unfinished reviewer attempt or uncertain publication.
2. Apply the authorized ordinary resume to cap 14.
3. From the enrolled foreground session, apply this exact owner instruction through `resume --amend-mission`.
4. Do not combine the amendment with a cap change or use the headless guidance route.
5. Read the entire returned effective task, including all required parts, and use the controller-provided amendment citation in the fresh developer report.

Do not edit the frozen original mission or controller state to simulate an amendment.

### Replacement behavior

Retain the verified new-horse filter, candidate predicate, world-write gate and exact-removal logic where suitable.

The new action must:

- Identify the loaded horse clearly, with rider/owner context where available.
- Require explicit confirmation for that horse and the specific proposed removal.
- Explain any saved-provenance ambiguity honestly. A sourceless fact is a candidate, not proof of its historical origin.
- Bind consent to the live unit object, stable identity, exact feature instance and current load/session context—not merely a name, row index or stable ID.
- Recheck eligibility, candidate count and the world-write gate immediately before execution.
- Execute through the service during the ordinary update path. UI drawing and dialog callbacks collect intent; they do not mutate game state.
- Consume confirmation once. Record the classified outcome before fallible logging or UI reporting. A reporting failure must not repeat removal.
- Invalidate stale consent after loading, relevant area/session replacement, disabling, unloading or replacement of the candidate.
- Preserve ordinary saving behavior. Loading an older save may require another explicit confirmation; do not persist authorization across loads.

The existing transient list of horses collected during an update must not be exposed as a mutable UI list or captured by row index. Use stable snapshots for presentation and revalidate the actual objects at execution.

### Remove the obsolete protocol completely

Remove the old repair-request ingestion, polling, claim/archive/recovery paths and their platform-specific lease and descriptor machinery from the active implementation.

Old user request/result files must remain untouched and inert. Do not import, replay, archive, delete or automatically act on them.

Do not retain the Linux lease and add a signal handler as a shortcut. Do not replace it with another persistent file-transaction subsystem.

### Findings and evidence

Preserve all eleven spent Trip reviews and every historical finding.

For file-specific obligations withdrawn by this amendment, use the controller’s supported amendment/disposition mechanism, citing the applicable OWNER AMENDMENT revision. Do not label the old implementation correct, erase valid findings or claim the old failing scenarios passed.

The independent reviewer must verify that the affected production paths are removed or unreachable and that the replacement preserves the underlying authorization, exact-removal, idempotence and lifecycle requirements. Any surviving defect remains blocking.

Use a connected production-path test covering candidate discovery, confirmation intent, guarded dispatch, exact removal and outcome reporting. Include:

- Two different horses and cancellation.
- Repeated clicks/updates.
- Reload with the same stable ID but a new unit object.
- Saving/loading/disabled gates.
- Changed, sourced, modified or ambiguous facts.
- Removal throwing before or after mutation.
- Reporting failure after an outcome.
- Old request files causing no mutation.
- The verified new-horse filter and protected neighboring behaviors.

Retire tests specific to the removed file protocol with a documented amendment mapping. Preserve tests that still establish the product’s behavior.

Actual UI appearance and gameplay save/load observations remain pending for my integrated-alpha playtest. Offline evidence must retain its proper label.

## 5. Finish the bounded transport correction before review five

Continue development-process PR #8 on its existing branch, currently at:

`88506708ff14bc63320cebf4cc9f384ef7b4fd34`

The existing four infrastructure review executions remain spent.

Before formal submission:

- Complete the round-four requested positive control through the public paginated reader, including a quoted title containing `=`.
- Reproduce and resolve the related quoted malformed relation case, such as `rel="=next"`, and the missing-relation case under the existing strict completeness contract.
- Define the accepted decoded relation syntax coherently. Preserve valid relation lists, legitimate unknown/extension relations and quoted titles.
- Ensure malformed nonterminal pagination cannot return accumulated pages as a successful complete listing.

Use the existing local-server reader fixture. This is a bounded completion of the identified failure family, not authorization for a general HTTP framework or a dependency change.

Run the required complete unit gate on the stable corrected candidate, then independent review five. If it finds a concrete remaining blocker, correct it and use review six for verification. Both executions count against the six-review total.

Preserve exact comment bodies, identity checks and complete-listing requirements. Do not normalize caret notation back into escape sequences, edit stored verdicts or weaken equality to force acceptance.

Install only an independently accepted exact source with the required gate evidence.

## 6. Recover Bite through the supported workflow

Keep Bite’s source frozen at:

`dda42b9c0c3119f63a9f912ac760ad5034e7cfe1`

until the transport checkpoint is accepted and recovery is ready.

Then use the previously authorized separate immutable controller installation and activation on Bite only. Preserve the original pending verdict, publication evidence and execution history.

Follow the supported invalidation/replacement-review behavior required by the workflow change. Do not present the old unsettled round as accepted merely because the new transport can read it.

Retain the existing completed-round cap of nine and execution ceiling of ten, counting the unsettled execution once. The previously authorized single conditional resume for the known R4-F2 no-progress stop during replacement-review recovery remains available within those limits; it does not authorize unrelated future resumptions.

After recovery, close the actual product/evidence findings, including the complete serialized outcome path and exact plain-string source validation, with connected production-to-reader evidence.

Do not claim that offline reader acceptance establishes a visibly correct bite animation in the game.

## 7. Continue to integration

Start with PR-2’s already-authorized corrections. Complete the transport/Bite dependency and the amended Trip work, then assemble PR-6 once every feature child has an accepted source checkpoint.

If one independent stream reaches a genuine stop, continue other useful work that remains authorized and within budget. Do not describe the entire program as unable to proceed solely because one child is blocked.

Finalize and freeze accepted children through the supported workflow without requesting another approval of the already-authorized source checkpoints.

For integration:

- Compose accepted product deltas with their provenance.
- Keep reviewed source H distinct from archive head A.
- Do not merge controller archive commits as product changes.
- Document the composition of shared files.
- Keep product corrections in their owning child through the supported route.
- Protect Mounted Shield behavior and icon identity, Mounted Archery, Ranger/Riding Dog, other mount profiles, save identities and the UMM 0.31.1 baseline.

The requested result remains a complete integrated private-alpha package with source/package identity, installation and rollback instructions, known limitations and the native playtest checklist.

## 8. Evidence, stopping and delivery

Native gameplay observations remain deferred to my integrated-private-alpha playtest. Do not require the vanished historical save pair or a new game-driving framework for these source checkpoints.

Do not count Windows simulation, a separate OS probe or offline contracts as a native Linux/Unity-Mono or gameplay run.

Preserve the controller’s actual stops and caps. The earlier supplementary operator-only two-rejection rule remains prospectively superseded as already recorded; do not reintroduce it.

This instruction authorizes PR-4’s current cap extension and the specified transport reviews. It does not authorize manually clearing future controller stops, changing counters or replacing enrollments.

No merge, tag, public release or permanent game/mod installation is authorized. The reviewed controller installation described above is authorized only for the specified Bite recovery.

The closing report must distinguish accepted source, remaining defects, pending native observations, actual spend and any exact stop that occurred. A continuation that stops without the integrated alpha remains incomplete work.
----- END OWNER AMENDMENT 1 -----
```

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
