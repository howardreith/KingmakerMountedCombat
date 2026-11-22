<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r03-request part=1/1 sha256=0f4d17b42004ccb36b9af4595c5976c6f22ef2060729bdae33ffc5938d74d00e -->
### Claude Opus — review request — round 3 — candidate `fe1fe56de0`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r03-301a74` |
| Checkpoint | `final` (final) |
| Candidate H | `fe1fe56de0e058a4b0fa9ee9276ac354e52d4f96` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 3 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

The round 2 corrections are in commit fe1fe56d, which is pushed; the focused gate passed on it and the worktree is clean.

Toolbar:
- R1-F3: native bad-slot cleanup is observed when it decides. An exact-token postfix on MechanicActionBarSlotAbility.IsBad (token 06002F6E, pinned in the assembly contracts) calls NativeMountedControlService.ObserveInvalidatedPublicSlot for the public control only, which records a temporary absence. This holds even when the fact is active again before the next update.
- R2-F1: CapturePersistentSlots(includeAbsentIntent: true) first classifies each reconciled rider's bar (binding found by blueprint) and fact state. A rider whose live control is waiting to return writes its kind-6 row and one deduplicated kind-7 row.
- R2-F2: the departure loop observes the departing rider's bar before it records the absence and clears the slots.
- R2-F3: a rider that cannot be leased right now keeps its arrangement unless the persistent PetUniqueId is cleared or changed.
- R2-F4: restoring a lease on the public control calls ObserveBound.

Evidence:
- R1-F7: scripts/Test-CityArchiveRowBound.ps1 compiles the production MountedArchiveData, validator and MountedArchiveCodec from this worktree. 128 rows round-trip with ids, indices and kinds intact; 127 rows plus an absent rider's 2 rows are refused with "Owned archive control slots are not bounded."
- R2-F5: both adapter facades now implement the candidate signatures and assert the modes. The six-pair adapter now compiles and passes (22/0) and is selected in the wrapper. The area adapter still fails to compile because its facade lacks members that base dbf63702's RestoreAreaPair already uses (git grep at base); the candidate run has 63 errors, none naming RestorePersistentSlots.

The handoff and acceptance record are updated. Native cases remain deferred, as already accepted.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 21.2s (incremental build; local full run ~24s build + ~25s tests) | Candidate fe1fe56d with the worktree unchanged: build PASS, FAST components 1434/0, Validate-Source 153/0. Wrapper results: Test-AssemblyContracts -Target Kingmaker 820/0; Test-GunslingerClickComposition 21/0 in both orders; Test-CityArchiveRowBound 3/0; chunk7 six-pair archive adapter 22/0. The 14 toolbar tests use production domain decisions with a labeled service-and-bar double in production event order: header capture before the save scope, lease restore, departure observation, IsBad observation, pet-resolution flag. The archive test runs production DTO/validator/codec code on in-memory graphs. The adapter runs actual methods against typed facades. None of this is native evidence. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -SkipBuild -Focused chunk7\Test-Chunk7AreaRestorePreflightAdapter.ps1 (diagnostic, before the final commit) | no | fail | ~1 min | 63 compile errors, all against members the facade lacks: transfer.Lifecycle, AreaTransfer/AreaPair, retained-intent members, PairHeldByDetachedCleanup, MountedAreaControlRule. None name RestorePersistentSlots or loadedWorld. Base comparison is by git grep at dbf63702, not by a run: base RestoreAreaPair and MountedPairCandidate already use these members, and the facade (last changed b81dc5f8, 2026-10-19) does not define them. Not selected. |
| Native guarded runtime cases | no | not_run | 0 | Deferred, as recorded in docs/CITY-AND-PRONE-HANDOFF.md: candidate package unavailable to this enrollment, plus per-case reader, lineage and driver prerequisites. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-fe1fe56de0e0-d43d449b6c4-a030b738800-20261121T125109029535Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- tests/MountedControlPlacementIntentTests.cs
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs
- scripts/Test-CityArchiveRowBound.ps1
- tests/Chunk7Diagnostics/SixPairArchiveAdapterTemplate.cs
- scripts/Test-AssemblyContracts.ps1
- commit fe1fe56de0e058a4b0fa9ee9276ac354e52d4f96

**Prior findings to assess:** `R1-F3`, `R1-F7`, `R2-F1`, `R2-F2`, `R2-F3`, `R2-F4`, `R2-F5`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
