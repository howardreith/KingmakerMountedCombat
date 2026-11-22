<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r17-request part=1/1 sha256=da655271221ccb87d223894d7a060052a556d511052b8942a3bde49da054ecd8 -->
### Claude Opus — review request — round 17 — candidate `d4b3c2d4f2`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r17-0475de` |
| Checkpoint | `final` (final) |
| Candidate H | `d4b3c2d4f2789242c867a9c4366705c7ba0f6226` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 17 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 16 is complete on commit d4b3c2d4, which was pushed before the owner-authorized pause. The STOP file is gone, the worktree is clean, and the remote branch head is d4b3c2d4 (checked with git ls-remote). After resuming, I ran the required focused gate on that commit with the new controller path; it passed (exit 0, 47.4 s, worktree unchanged). No code changed between the pause and this gate.

- R14-F2 and R15-F1 (shared root cause): while a chosen slot is interrupted, a leftover older alias or stale canonical copy at another index is now a secondary copy (MountedControlPlacementIntent.IsSecondaryBinding with IsStaleOrAlias).
  - Bar observation keeps the interrupted choice (ObserveBindingAtBoundary).
  - Capture classification and the live writer skip the secondary copy, so the remembered writer saves the chosen index.
  - ReconcilePublicControlSlots removes a migrated or rebound copy when the chosen slot is free, so the control returns there as one shortcut.
  - A binding holding the live fact's data, such as a real player placement, stays authoritative.
- R16-F1: TryInitialPlacement now records the placed slot and its companion immediately (ObserveBound, or ObserveOffBar when the bar is full). A direct change of companions before the next observation therefore retires the placement.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.4s | Result of record on commit d4b3c2d4 (log gate-focused-d4b3c2d4f278-d43d449b6c4-a030b738800-20261121T184923995103Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the five children of the focused wrapper. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree before commit, PASS lines temporarily silenced) | no | pass | 0.93 min | Development run: components 1483/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted before commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (mutation: IsSecondaryBinding forced false; initial-placement record removed in the double) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1479/4. The failures were SurvivingAliasAfterCanonicalCleanup, SurvivingCanonicalCopyAfterCleanup, InitialPlacementCompanionChange ('Pet B inherited pet A's automatic placement') and round 15's SaveBetweenNativeReads. Mutations were reverted. The initial-placement line is service code that the double mirrors. |

**Evidence references:**
- commit d4b3c2d4f2789242c867a9c4366705c7ba0f6226 (remote head confirmed with git ls-remote)
- gate-focused-d4b3c2d4f278-d43d449b6c4-a030b738800-20261121T184923995103Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (IsSecondaryBinding, TryGetInterruptedChoice, ObserveBindingAtBoundary)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (IsStaleOrAlias, IsSecondaryPublicSlot, ObserveArrangementOnBar, ClassifyArrangementsForCapture, CaptureLiveSlots, ReconcilePublicControlSlots secondary removal, TryInitialPlacement identity)
- tests/MountedControlPlacementIntentTests.cs (SurvivingAliasAfterCanonicalCleanup, SurvivingCanonicalCopyAfterCleanup, InitialPlacementCompanionChange)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 16 additions, Validation)

**Prior findings to assess:** `R14-F2`, `R15-F1`, `R16-F1`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
