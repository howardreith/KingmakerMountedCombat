<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r06-request part=1/1 sha256=7088f1fafadd73fd1a5e9718e13d788ca580b71394c40a082948ffd40bb15890 -->
### Claude Opus — review request — round 6 — candidate `85d70ff2db`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r06-f6ed8d` |
| Checkpoint | `final` (final) |
| Candidate H | `85d70ff2db5e7562027c66c00065ba47506d3328` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 6 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 5 corrections are in commit 85d70ff2, which is pushed. The focused gate passed on it (29.6 s) and the worktree is clean.

Toolbar:
- R4-F1: the pre-update bar observation now runs whenever the public fact will be withdrawn, replaced or re-leased, including a fact native already deactivated. Recorded absences (native cleanup, KMC suspension, a load) stay pending.
- R4-F2: RetireOnPetChange uses PetLinkChanged to detect a nonempty, different link before any observation can overwrite the stored link: at each candidate's reconciliation, in ObserveArrangementOnBar (save header, departures, boundaries) and in the native-cleanup report. It retires the arrangement and clears the public slots.
- R5-F1/R5-F2: MountedControlLoadFence is now owned by its load scope. BeginSelectedLoad(scope) moves the outgoing records (arrangements and placement records) aside. AbandonLoadedWorld(scope) from the owning scope restores them exactly when the outgoing world is still current, and drops them after a world replacement. The superseded-scope end callback now abandons its own fence. An older scope cannot release a newer scope's fence, and a newer scope starting on the same fenced world takes ownership of the moved records.

Handoff:
- R3-F2: real-time rolls are counted in one bounded window, about 5.5 game seconds after the first roll, for both mounted and dismounted runs. A normal multi-roll routine passes; a delayed extra roll is observable; the next automatic round is excluded.

Four new production-ordered scenarios cover these changes.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8.../agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 29.6s (controller receipt) | Candidate 85d70ff2, worktree unchanged. FAST components 1447/0, including 27 toolbar tests: production MountedControlPlacementIntent, MountedControlLoadFence and MountedControlSlotPolicy decisions driven by a labeled service double in production event order. Validate-Source 153/0. Test-AssemblyContracts -Target Kingmaker 820/0. Test-GunslingerClickComposition 21/0 in both orders. Test-CityArchiveRowBound 3/0. chunk7 six-pair adapter 22/0. Test-CityAreaAdapterBaseline PASS (63 vs 63, none only in either; compile step only). Not native evidence. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (local, uncommitted tree, before the commit) | no | pass | about 1.5 minutes including a 27.5 s build | Development run, not the result of record: components 1447/0, Validate-Source 153/0, focused 5/5. Its area comparison ran against the previous HEAD. |
| Native guarded runtime cases | no | not_run | 0 | Deferred to owner verification under the mission's provision; owner steps are in docs/CITY-AND-PRONE-HANDOFF.md, including setup S, the bounded real-time window and the negative controls. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-85d70ff2db5e-d43d449b6c4-a030b738800-20261121T154433873943Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- tests/MountedControlPlacementIntentTests.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs
- src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs
- commit 85d70ff2db5e7562027c66c00065ba47506d3328

**Prior findings to assess:** `R3-F2`, `R4-F1`, `R4-F2`, `R5-F1`, `R5-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
