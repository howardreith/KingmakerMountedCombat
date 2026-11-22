<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r13-request part=1/1 sha256=f7af05a40fa50a011bec4537883085fa8b66bc1c5e90c2d249bed2719d07d899 -->
### Claude Opus — review request — round 13 — candidate `59eda25267`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r13-4b39ca` |
| Checkpoint | `final` (final) |
| Candidate H | `59eda25267446aad9b6d6e45986875a13fdef1f3` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 13 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 12 fixes all four toolbar findings in commit 59eda252. The guarded push succeeded, the controller focused gate passed on that commit (45.4 s, worktree unchanged) and the worktree is clean.

R12-F1, F2 and F3: one rule now picks the rider's public binding everywhere. PublicBindingIndex takes the first canonical binding, otherwise the first exact older alias. Bar observation, save classification and the live archive writer all use it.
- R12-F1: classification checks whether that exact slot is callable. When the writer skips it because of stale data, its index is carried as the remembered slot.
- R12-F2: an alias that a save scope returned, still on the bar, counts as the player's choice at the next save.
- R12-F3: retirement detection and cleanup (RetireOnCompanionLoss, ClearPublicControlSlots) now include both older aliases.

R12-F4: a new exact prefix/postfix wraps native UnitUISettings.CleanupBadSlots (token 06003004, pinned by a new assembly-contract row). An IsBad report inside that call is held until the call returns.
- If native rebound the slot to a live fact of the same blueprint, that slot becomes the live arrangement (MountedControlPlacementIntent.ObserveNativeCleanupOutcome).
- If native emptied it, it stays an ordinary invalidation.
- GetSlot, which always empties, is applied immediately; a call that does not return normally is resolved at the next update.

Four new component regressions and one more production-codec archive check cover the requested sequences. To make individual failures visible, PASS output in tests/TestRunner.cs was silenced during development runs; that change was reverted and is absent from the commit. A combined mutation run that restored each pre-fix line failed exactly the four new regressions.

The new native patch, the capital UI and attack resolution remain NOT RUN natively and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 45.4s | Result of record on commit 59eda252 (log gate-focused-59eda2526744-d43d449b6c4-a030b738800-20261121T173729739497Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the new CleanupBadSlots token row and archive bound 8/0. Offline only; the new Harmony patch is not exercised natively. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (token discovery, then the fixed tree with PASS lines temporarily silenced) | no | pass | 0.73 min | Development runs. In the first, a temporary non-zero contract check printed the CleanupBadSlots token 06003004, which was then pinned; the R12-F1 to F3 changes passed in that run (components 1464/0). On the fixed tree: components 1468/0, Validate-Source 153/0, assembly contracts 821/0, click composition 21/0, archive bound 8/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation: domain rebind outcome ignored; double with canonical-only observation, canonical-only retirement and any-slot callability) | no | fail | ~1 min | The intended failing reproduction, with FAIL lines visible: 1464/4. Exactly the four new regressions failed: stale-then-current canonical, second save of a returned alias, companion replacement with a waiting alias, and native rebind followed by removal. Every mutation was reverted, and a grep confirmed none remained. The F1 to F3 mutations sit in the toolbar double, mirroring the pre-fix production lines; the production service code itself is checked only through the double and the codec fixtures. |

**Evidence references:**
- commit 59eda25267446aad9b6d6e45986875a13fdef1f3
- gate-focused-59eda2526744-d43d449b6c4-a030b738800-20261121T173729739497Z.log
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (PublicBindingIndex, IsPublicBindingSlot, ObserveArrangementOnBar, ClassifyArrangementsForCapture, RetireOnCompanionLoss, ClearPublicControlSlots, ObserveInvalidatedPublicSlot, Begin/EndNativeBadSlotCleanup, ResolvePendingBadSlots, ApplySlotInvalidation)
- src/KingmakerMountedCombat/Integration/MountedPatchController.cs (NativeCleanupBadSlotsToken, BadSlotCleanupPrefix/Postfix)
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (ObserveNativeCleanupOutcome)
- scripts/Test-AssemblyContracts.ps1 (CleanupBadSlots token row)
- tests/MountedControlPlacementIntentTests.cs (StaleThenCurrentCanonical, AliasSecondSave, AliasCompanionReplacement, NativeRebindOutcome)
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs (stale-then-current codec check)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 12 additions, Validation)
- C:/Dev/KingmakerSkipCinematicLab/private/decompiled/Assembly-CSharp/Kingmaker.UI.UnitSettings/UnitUISettings.cs (GetSlot, CleanupBadSlots)

**Prior findings to assess:** `R12-F1`, `R12-F2`, `R12-F3`, `R12-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
