<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r14-request part=1/1 sha256=834d4c6b35fa86460929306a9659b63e0cc57f1b6f73c669d426210e78e673c0 -->
### Claude Opus — review request — round 14 — candidate `9fd90f6bc4`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r14-fd0e34` |
| Checkpoint | `final` (final) |
| Candidate H | `9fd90f6bc49837d9133547a0b7db17cfd29b74a9` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 14 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 13 resolves all five pending findings in commit 9fd90f6b. The guarded push succeeded, the controller focused gate passed on that commit (63.7 s, worktree unchanged), and the worktree is clean.

- R12-F4: the native rebind is now recognised from the live data left in the slot, not from object identity (MountedControlSlotPolicy.IsNativeRebind). A new assembly-contract row reads CleanupBadSlots' IL in the pinned build and confirms how a rebind is written: one new MechanicActionBarSlotAbility, one store into its Ability field, then SetSlot. Its other branch writes a new empty slot.
- R13-F1: when native cleanup empties an alias that already has a recorded placement, that placement is now marked as temporarily absent at its index (ObserveNativeAliasInvalidated). An alias is recovered only when nothing is recorded yet. The same rule applies to the load fence's outgoing records.
- R13-F2: observing a shortcut on the bar now counts as the rider's one-time placement. TryInitialPlacement also waits while a save scope holds that shortcut's slot, so no autofill copy appears.
- R13-F3: the remembered-row writer now checks whether another binding already holds the remembered slot. If so, that binding keeps the slot and the shortcut is saved as off the bar (placement record only).
- R13-F4: each deferred native report now records the player world it came from. It applies only in that world and only while the same rider instance is on its roster (DeferredObservationApplies).

Five new component regressions cover these sequences. The test double now drives native cleanup through the production classifier and models deferred work and occupied slots. A new archive check runs an occupied remembered slot through the production codec.

To identify failing checks, I temporarily silenced PASS output in tests/TestRunner.cs; that change was reverted and is not in the commit. A combined mutation run then failed exactly the five targeted regressions.

Native checks remain NOT RUN and are deferred to the owner playtest in the handoff. This includes the new patch's call timing in game, the capital UI and attack resolution.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 63.7s | Result of record, on commit 9fd90f6b (log gate-focused-9fd90f6bc498-d43d449b6c4-a030b738800-20261121T175341221750Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the rebind-shape row and archive bound 9/0. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.96 min | Development run. Components 1472/0, Validate-Source 153/0, assembly contracts 822/0. The rebind-shape row read newobj MechanicActionBarSlotAbility x1, stfld Ability x1, 3 SetSlot calls and one empty-slot newobj. Click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1467/5. The mutations reverted the domain decisions (IsNativeRebind forced false, recorded-alias invalidation disabled, occupancy ignored, DeferredObservationApplies forced true) and, in the double, the observed and leased placement record (the R13-F2 mirror). Exactly the five targeted regressions failed. All mutations were reverted and a grep confirmed none remained. |

**Evidence references:**
- commit 9fd90f6bc49837d9133547a0b7db17cfd29b74a9
- gate-focused-9fd90f6bc498-d43d449b6c4-a030b738800-20261121T175341221750Z.log
- scripts/Test-AssemblyContracts.ps1 (CleanupBadSlots rebind-shape row)
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (IsNativeRebind, DeferredObservationApplies, AbsentRowsAfterLive)
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (ObserveNativeAliasInvalidated; fence AdmitOutgoingSurvivingBinding)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (PendingSlotInvalidation.World/Kind, ResolvePendingBadSlots, ApplySlotInvalidation, CaptureAbsentIntent occupancy, ObserveArrangementOnBar placement record, TryInitialPlacement lease guard)
- tests/MountedControlPlacementIntentTests.cs (NativeRebindOutcome, ReturnedAliasNativeEmptying, SaveReleaseNoAutofillCopy, RememberedSlotOccupied, DeferredCleanupAcrossWorlds; double NativeCleanupBadSlots, PendingNative)
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs (occupied remembered slot codec check)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 13 additions, Validation)
- C:/Dev/KingmakerSkipCinematicLab/private/decompiled/Assembly-CSharp/Kingmaker.UI.UnitSettings/UnitUISettings.cs (CleanupBadSlots lines 282-328)

**Prior findings to assess:** `R12-F4`, `R13-F1`, `R13-F2`, `R13-F3`, `R13-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
