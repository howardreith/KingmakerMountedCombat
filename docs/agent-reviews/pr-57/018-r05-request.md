<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r05-request part=1/1 sha256=24c5ada1afb4201cd6559806bd7737dcf114b2ae0bb78b5f66b72c5fbdcf10f2 -->
### Claude Opus — review request — round 5 — candidate `9ce8b7c17c`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r05-54aa03` |
| Checkpoint | `final` (final) |
| Candidate H | `9ce8b7c17c55c49b3eb85776f6bcf547fe2e656e` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 5 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

The round 4 corrections are in commit 9ce8b7c1, which is pushed. The focused gate passed on that commit in 22.4 s, and the worktree is clean.

Toolbar fixes:
- R2-F1: header classification now uses the live writer's own conditions through LiveCaptureWritesPublicSlot (ShouldLease, active fact, exact data, caster). A bound slot that the writer would skip is carried while the rider still gets the control or its pet link names the same companion.
- R4-F1: ReconcileFacts observes the bar before it withdraws or replaces a live public fact.
- R4-F2: ProvesPermanentLoss (pet link cleared or naming another unit) triggers ClearPublicControlSlots for a rider that no longer gets the control.
- R4-F3: SuspendAreaControls runs ObserveDepartedRiders before RemoveAllManagedFacts(true).
- R3-F1: MountedPersistenceService calls the new BeginSelectedLoad. It clears the records and arms MountedControlLoadFence on the outgoing player object. The fence lifts when Game.Instance.State.PlayerState is replaced; 2.1.7b ThreadedGameLoader deserializes a new Player. It also lifts on an abandoned load via AbandonLoadedWorld. While the fence is armed, Update, the IsBad observation, bar observation and absences write nothing. BeginLoadedWorld stays fence-free for the two diagnostics that re-arm the first placement within one world.
- Five new scenarios cover these changes, using the shared service double in production order.

Handoff fixes:
- R3-F2: setup S defines a single-attack rider, or a matched routine control against the same rider unmounted. Real-time observation is bounded to one round. Ammunition is compared only if a mod tracks it; the base game's item and attack code has no ammunition consumption.
- R4-F4: PASS is attributed to the controller receipt only, and the local runs and retries are recorded with their real outcomes.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 22.4s (controller receipt; incremental build) | Candidate 9ce8b7c1, worktree unchanged. FAST components 1443/0, including 23 toolbar tests: production MountedControlPlacementIntent, MountedControlLoadFence and MountedControlSlotPolicy decisions driven by a labeled service double in production event order. Validate-Source 153/0. Wrapper results: Test-AssemblyContracts -Target Kingmaker 820/0; Test-GunslingerClickComposition 21/0 in both orders; Test-CityArchiveRowBound 3/0; chunk7 six-pair adapter 22/0; Test-CityAreaAdapterBaseline PASS (base dbf63702 63 errors, candidate 9ce8b7c1 63 errors, 0 only in either; compile step only). Not native evidence. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (local, uncommitted tree, before the commit) | no | pass | about 1.5 minutes including a 23.5 s build | Development run, not the result of record. Components 1443/0, Validate-Source 153/0, focused 5/5. Its area-adapter comparison read the previous commit as HEAD. |
| Native guarded runtime cases | no | not_run | 0 | Deferred to owner verification under the mission's provision. Prerequisites and owner steps are in docs/CITY-AND-PRONE-HANDOFF.md, including setup S and the negative controls. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-9ce8b7c17c55-d43d449b6c4-a030b738800-20261121T150213408133Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- tests/MountedControlPlacementIntentTests.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs
- src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs
- commit 9ce8b7c17c55c49b3eb85776f6bcf547fe2e656e

**Prior findings to assess:** `R2-F1`, `R3-F1`, `R3-F2`, `R4-F1`, `R4-F2`, `R4-F3`, `R4-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
