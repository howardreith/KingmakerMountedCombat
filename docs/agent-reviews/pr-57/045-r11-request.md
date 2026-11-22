<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r11-request part=1/1 sha256=0785427feb4f2020713851e373b4a8ee9d86709184fb080c667b07eb0aa2ece2 -->
### Claude Opus — review request — round 11 — candidate `ddbe460f69`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r11-3b19f9` |
| Checkpoint | `final` (final) |
| Candidate H | `ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 11 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 10 fixes all three pending toolbar findings in commit ddbe460f. The guarded push succeeded, the controller focused gate passed on that commit (47.7 s, worktree unchanged) and the worktree is clean.

R10-F1 (live alias saved before migration): CaptureLiveSlots now counts a live older Mount or Dismount row (kind 1 or 2) as the actor's public binding. CaptureAbsentIntent adds rows only through the new MountedControlSlotPolicy.AbsentRowsAfterLive. The archive therefore gets the chosen index once and one placement record, and its duplicate-key validator is unchanged.

R9-F2 (leased alias restored for a replacement companion): the save-scope lease release applies AllowsLeaseRestore to every public binding (the new IsPublicControlKind), legacy aliases included.

R9-F4 (native cleanup of an inactive alias): the IsBad observer now handles exact legacy aliases. Before native empties the slot it records the bar's chosen alias index through RecoverLegacyAlias, with canonical precedence and the companion-loss and existing-arrangement rules. While a selected load fences the world, the index goes to the load scope's outgoing records through the new MountedControlLoadFence.AdmitOutgoingSurvivingBinding.

The toolbar double now models the live alias writer, leases of every KMC binding and alias cleanup. There are three new component regressions, plus two archive checks through the production codec in Test-CityArchiveRowBound.ps1. The handoff doc has a Round 10 section and updated validation.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.7s | Result of record, on commit ddbe460f (log gate-focused-ddbe460f69e0-d43d449b6c4-a030b738800-20261121T170630023526Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including archive bound 5/0. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.75 min | Development run on the fixed tree. Components 1461/0, including the three new toolbar tests. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0. Archive bound 5/0, including both live-alias codec checks. Six-pair adapter 22/0. Area baseline: 63 errors on both trees. Focused 5/5. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation making AbsentRowsAfterLive and IsPublicControlKind canonical-only) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool truncation cut off the FAIL lines. Both mutations were reverted before the commit, and the controller gate passed on the committed tree. Limit: the alias-cleanup call order in the toolbar double goes through RecoverAlias directly. The production IsBad filter's alias coverage (IsPublicControlKind) is checked only by that mutation's effect on the lease gate. |

**Evidence references:**
- commit ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7
- gate-focused-ddbe460f69e0-d43d449b6c4-a030b738800-20261121T170630023526Z.log
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (IsPublicControlKind, IsPublicBindingRow, AbsentRowsAfterLive)
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (MountedControlLoadFence.AdmitOutgoingSurvivingBinding)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (CaptureLiveSlots, CaptureAbsentIntent, ClearSerializationHotbarLeases, ObserveInvalidatedPublicSlot, RecoverLegacyAlias, LegacyAliasIndex)
- tests/MountedControlPlacementIntentTests.cs (LiveAliasSaveFirst, AliasLeaseAfterCompanionChange, AliasNativeCleanupFirst; double Lease/ReleaseLeases/CaptureLive/NativeInvalidateAlias)
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs (live-alias codec checks)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 10 additions, Validation)

**Prior findings to assess:** `R10-F1`, `R9-F2`, `R9-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
