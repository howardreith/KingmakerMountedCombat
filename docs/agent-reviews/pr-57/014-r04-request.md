<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r04-request part=1/1 sha256=27e890525a4b6703997bef9796d2f586c974aa8e292acea0cbcf89597db12081 -->
### Claude Opus — review request — round 4 — candidate `29ffa3a5f0`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r04-eeef19` |
| Checkpoint | `final` (final) |
| Candidate H | `29ffa3a5f0eda518e50fe4067d5170aed685248f` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 4 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 3 corrections are in commit 29ffa3a5, which is pushed. The focused gate passed on it and the worktree is clean.

Toolbar:
- R1-F3: ObserveInvalidatedPublicSlot locates the exact invalidated slot object on the current-world rider's own bar (reference identity) and calls ObserveNativeSlotInvalidated(id, index, petId), which binds that index and records the absence.
- R2-F1: the header classification treats a slot still bound to older public-control data as the remembered slot when the active fact's data is not on the bar.
- R2-F2: the header runs ObserveDeparture, shared with the departure cleanup, for still-observed current-world riders that left the candidate roster.
- R3-F1: observedWorlds records the player world each unit was first reconciled in. An earlier world's object is cleaned up but never observed into, never marks an absence, and is ignored by the IsBad observation. Roster checks use the exact instance.
- The toolbar tests now use a shared service double that separates roster change, departure cleanup and world load, and covers each requested scenario.

R2-F5: Test-CityAreaAdapterBaseline.ps1 performs the area adapter's own compile step on base dbf63702 and on HEAD, with the same extraction, template, csc, flags and sources. It pins the adapter's compile line and records input blob ids. Normalized diagnostics are 63 on each tree, none unique to either. The check is in the wrapper. The adapter still executes in neither tree.

R3-F2: the owner playtest item 5 adds pass/fail steps for each deferred negative control.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 25.7s (incremental build; local full run ~28s build + ~60s tests) | Candidate 29ffa3a5, worktree unchanged. Build PASS. FAST components 1438/0, including 18 toolbar tests that use production MountedControlPlacementIntent/MountedControlSlotPolicy decisions driven by a labeled service double in production event order. Validate-Source 153/0. Wrapper results: Test-AssemblyContracts -Target Kingmaker 820/0; Test-GunslingerClickComposition 21/0 in both orders; Test-CityArchiveRowBound 3/0; chunk7 six-pair adapter 22/0; Test-CityAreaAdapterBaseline PASS (base dbf63702 63 errors, candidate 29ffa3a5 63 errors, 0 only in either; compile step only). None of this is native evidence. |
| Native guarded runtime cases | no | not_run | 0 | Deferred to owner verification under the mission's provision; prerequisites and owner steps, including the new negative-control item 5, are in docs/CITY-AND-PRONE-HANDOFF.md. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-29ffa3a5f0ed-d43d449b6c4-a030b738800-20261121T144622875544Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- tests/MountedControlPlacementIntentTests.cs
- scripts/Test-CityAreaAdapterBaseline.ps1
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- commit 29ffa3a5f0eda518e50fe4067d5170aed685248f

**Prior findings to assess:** `R1-F3`, `R2-F1`, `R2-F2`, `R2-F5`, `R3-F1`, `R3-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
