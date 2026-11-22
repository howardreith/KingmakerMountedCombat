<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r12-request part=1/1 sha256=f87d8c60747d23c4d2e829470a28c64cb7567314d89c7a1aed409e08b1493b26 -->
### Claude Opus — review request — round 12 — candidate `d431ea7f19`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r12-f237a0` |
| Checkpoint | `final` (final) |
| Candidate H | `d431ea7f19b783e7e0e1a062d461275ea113cd51` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 12 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 11 fixes all three pending toolbar findings in commit d431ea7f. The guarded push succeeded, the controller focused gate passed on that commit (24.9 s, worktree unchanged) and the worktree is clean.

- **R11-F1 (wrong slot chosen on a mixed bar):** in the current archive schema, CaptureLiveSlots no longer writes public bindings slot by slot. A new rule, MountedControlSlotPolicy.LivePublicRows, picks one binding using the same precedence as live repair. If the bar has a canonical binding, that one is used; only otherwise is the first exact older alias used. It is written as kind 6, and only when it is callable live data. A canonical binding with stale data is written instead by the remembered-row writer, at its own index.
- **R10-F1 (missing placement record):** the placement record is now decided separately. It is written when the rider has one and either the public control is live or this capture wrote the binding. This covers a live alias whose canonical fact is still inactive.
- **R2-F4 (a removed alias comes back):** when the save scope returns a leased alias, the release now records the live arrangement (ObserveBound), as it already did for the canonical control. Removing the alias before it migrates therefore counts as the player's removal.

The held legacy-schema writer and restore are unchanged.

The toolbar test double now uses production slot order and LivePublicRows, and models shell activity separately from the canonical fact. There are three new component regressions. The archive-codec checks now feed rows emitted by LivePublicRows and AbsentRowsAfterLive through the production codec.

To identify a failing check, I temporarily silenced PASS output in tests/TestRunner.cs; that change was reverted before commit and is absent from the diff. With FAIL lines visible, the mutation runs failed exactly the new regressions.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 24.9s | Result of record, on commit d431ea7f (log gate-focused-d431ea7f19b7-d43d449b6c4-a030b738800-20261121T172018569240Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including archive bound 7/0. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree; PASS lines temporarily silenced in tests/TestRunner.cs) | no | pass | 0.38 min | Development runs. The first run failed on my own assertion: the mixed-bar test checked the earlier world's rider object after a load, and departure cleanup empties that object by design. I moved the assertion before the load. The next run passed: components 1464/0, Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 7/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The TestRunner change was reverted and is absent from the commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (mutation: slot-order binding choice plus a placement record tied only to the canonical fact) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible. Components 1462/2, failing exactly 'a mixed bar saved first ... restores one control at 9' (stale=False) and 'a live alias with the canonical fact inactive saves its binding and its placement record' (KMC_Mount#1). Reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (mutation: double's lease release records ObserveBound only for the canonical control, the pre-fix production line) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible. Components 1463/1, failing exactly 'removing an alias the save scope returned, before the next update, stays removed through save and load'. Reverted. The production line itself is service code that the double mirrors. |

**Evidence references:**
- commit d431ea7f19b783e7e0e1a062d461275ea113cd51
- gate-focused-d431ea7f19b7-d43d449b6c4-a030b738800-20261121T172018569240Z.log
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (LivePublicRows)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (CaptureLiveSlots, IsCallableLiveBinding, ClearSerializationHotbarLeases)
- tests/MountedControlPlacementIntentTests.cs (MixedBarSaveFirst, LiveAliasCanonicalInactive, AliasRemovedAfterLeaseRelease; double CaptureLive/ShellLiveOverride/ReleaseLeases)
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs (live-alias and mixed-bar codec checks with emitted rows)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 11 additions, Validation)

**Prior findings to assess:** `R10-F1`, `R11-F1`, `R2-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
