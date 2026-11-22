<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r18-request part=1/1 sha256=2974cfd9a677d2dcc30e181b0341366375488f4156a5a49dd640b8a46873faa2 -->
### Claude Opus — review request — round 18 — candidate `352c12aca7`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r18-5cefee` |
| Checkpoint | `final` (final) |
| Candidate H | `352c12aca79a1a681f75608a342fdf2edec7a1d8` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 18 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 17 fixes all five pending findings in commit 352c12ac. The guarded push succeeded, the controller focused gate passed on that commit (47.2 s, worktree unchanged) and the worktree is clean.

R14-F2 and R15-F1: when native cleanup empties the rider's chosen slot, the other public bindings still on the bar are now recorded as copies of that choice (new Entry.Copies). A recorded copy stays secondary whether it holds stale or live data, so it cannot become the saved slot, and a native rebind of a secondary copy keeps the interrupted choice. A placement the player makes after the cleanup is not a recorded copy and stays authoritative.

R17-F1: one occupied-slot rule now applies everywhere. A copy gives way only while the chosen slot is free (ChosenSlotFree). If the chosen slot is occupied, the surviving copy becomes the arrangement in bar observation, live repair, capture and lease release, so update-first and save-first runs end with the same bar and save.

R17-F2: a new exact prefix/postfix wraps UnitUISettings.GetSlot(int, UnitEntityData) (token 06002FFE, pinned in the assembly contracts together with its empty-slot construction), alongside CleanupBadSlots. An IsBad report is recorded only inside one of these native slot operations and is resolved when the operation returns. A standalone validity query, such as the F2 diagnostic, changes nothing.

R17-F3: if a companion change is detected while a selected load fences the world, the outgoing arrangement is retired (new MountedControlLoadFence.RetireOutgoingOnCompanionLoss) and the rider's public bindings are cleared immediately.

Five new component regressions cover these sequences. To see individual failures I silenced PASS lines in tests/TestRunner.cs during development runs; that change was reverted and is not in the commit. A combined mutation run failed exactly the five new regressions.

Native checks remain NOT RUN; the owner playtest steps are in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.2s | Result of record on commit 352c12ac (log gate-focused-352c12aca79a-d43d449b6c4-a030b738800-20261121T191233793732Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the GetSlot token row. Offline only; the new GetSlot patch is not exercised natively. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (token discovery, then the fixed tree with PASS lines temporarily silenced) | no | pass | 0.82 min | Development runs. In the first, a temporary non-zero contract check printed the GetSlot token 06002FFE, which was then pinned. Fixed tree: components 1488/0, Validate-Source 153/0, assembly contracts 823/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, FAIL lines visible: 1483/5. Domain mutations: rebound-copy check removed, recorded copies ignored, occupied-slot promotion removed, fenced retirement disabled. Double mutation: the native-operation gate, mirroring the production check. Exactly the five new regressions failed. All mutations were reverted and a grep confirmed none remained. |

**Evidence references:**
- commit 352c12aca79a1a681f75608a342fdf2edec7a1d8
- gate-focused-352c12aca79a-d43d449b6c4-a030b738800-20261121T191233793732Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (Entry.Copies, ObserveNativeSlotInvalidated copies, ObserveNativeCleanupOutcome rebound secondary check, IsSecondaryBinding, ObserveBindingAtBoundary chosenSlotFree, fence RetireOutgoingOnCompanionLoss)
- src/KingmakerMountedCombat/Integration/MountedPatchController.cs (NativeGetSlotToken, GetSlot bracket)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (native slot operation bracket, ObserveInvalidatedPublicSlot gate and copies, ApplySlotInvalidation fenced retirement, IsSecondaryPublicSlot/ChosenSlotFree)
- scripts/Test-AssemblyContracts.ps1 (GetSlot token row)
- tests/MountedControlPlacementIntentTests.cs (AliasRebindAfterCanonicalCleanup, LiveCopyAfterCanonicalCleanup, OccupiedChosenSlotWithCopy, StandaloneIsBadQuery, FencedRebindCompanionChange)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 17 additions, Validation)

**Prior findings to assess:** `R14-F2`, `R15-F1`, `R17-F1`, `R17-F2`, `R17-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
