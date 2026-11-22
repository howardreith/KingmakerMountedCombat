<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r13-dispositions-a7c8890f26 part=1/1 sha256=a7c8890f26866f0700ba4a80cfc3e768c84ee304181090fe787615e45e4193d5 -->
### Claude Opus — finding dispositions — round 13

Candidate after corrections: `9fd90f6bc49837d9133547a0b7db17cfd29b74a9`

- **R12-F4** — `fixed` — commits: `9fd90f6bc4`
  Production classifier: ResolvePendingBadSlots now calls MountedControlSlotPolicy.IsNativeRebind(reportedKind, kindNow, nowHoldsLiveFactData). The slot must hold a binding of the same KMC kind whose AbilityData is the active fact's own data. Object identity is no longer used, so an in-place data change would be recognised too.

Assembly evidence: a new row in scripts/Test-AssemblyContracts.ps1 scans the IL of the pinned UnitUISettings.CleanupBadSlots (token 06003004). It requires exactly one newobj of MechanicActionBarSlotAbility's parameterless constructor, followed by exactly one stfld of its AbilityData field 'Ability', then a SetSlot call, plus a newobj of MechanicActionBarSlotEmpty. Gate output: newobj x1, stfld Ability x1, SetSlot calls 3, empty-slot newobj x1. This matches the decompiled lines 302-306 and 323.

The toolbar double's NativeCleanupBadSlots writes that shape (live data, or an empty slot) and classifies it with IsNativeRebind before calling ObserveNativeCleanupOutcome.

Regression NativeRebindOutcome:
- Rebind, then removal before the next update: the save holds only the placement record, and a load leaves the slot empty.
- Emptied through the same classifier (no live fact): the same pet's return recovers one shortcut at 9.
- GetSlot emptying recovers one shortcut.
- Classifier truth table: a same-kind binding with live data is a rebind; empty, non-live or different-kind is not.

The mutation forcing the classifier false failed exactly this regression. The gate passed on 9fd90f6b.

Limit: native call timing is NOT RUN.
- **R13-F1** — `fixed` — commits: `9fd90f6bc4`
  New domain method MountedControlPlacementIntent.ObserveNativeAliasInvalidated. When the actor has a recorded arrangement, it marks that placement as interrupted at the alias index (ObserveNativeSlotInvalidated). Only when nothing is recorded does it recover an older alias (AdmitSurvivingBinding). ApplySlotInvalidation's alias path and MountedControlLoadFence.AdmitOutgoingSurvivingBinding, the outgoing world's records during a load, both use it.

Regression ReturnedAliasNativeEmptying, update-first and save-first:
- Setup: a save scope returns the same pet's alias at 9 and records it as live. Its shell goes stale, and native GetSlot empties it before migration while the canonical control stays available.
- Result: one binding returns at 9, the archive keeps a single public binding at 9 with unique keys, and a reload restores one binding at 9.
- Control: a deliberate removal of the returned alias stays removed.

The mutation that always recovers instead of invalidating failed exactly this regression. The gate passed on 9fd90f6b.
- **R13-F2** — `fixed` — commits: `9fd90f6bc4`
  ObserveArrangementOnBar now adds the actor to placementRecorded whenever a public binding is visible, so an observed manual placement satisfies the one-time placement.

TryInitialPlacement now yields while a save-scope lease of a public binding is pending for the unit: it records the placement and returns. It also treats an older alias on the bar as already placed. The toolbar double mirrors both.

Regression SaveReleaseNoAutofillCopy:
- Setup: one manual canonical binding at 9, no placement record, autofill on, empty lower slots, and a save before ordinary reconciliation.
- Exactly one shortcut is at 9 after the save release and after each of two updates.
- The next save holds one kind-6 row at 9 and one placement record, and a reload restores exactly one shortcut at 9.

The mutation removing the observed and leased placement record failed exactly this regression ('The save release left more or less than one shortcut at 9'). The gate passed on 9fd90f6b.
- **R13-F3** — `fixed` — commits: `9fd90f6bc4`
  MountedControlSlotPolicy.AbsentRowsAfterLive now receives the live writer's (index, kind) rows for the actor and a rememberedSlotOccupied flag. CaptureAbsentIntent sets the flag when the owner's bar holds a non-empty, non-public binding at the remembered index.

When the slot is occupied, or another live row has that index, it writes only the placement record: the occupant keeps the slot and the shortcut is saved off the bar. Without occupancy it writes AbsentArrangementRows as before, and never a second placement record. The archive's duplicate-key validation is unchanged.

Production-codec evidence (Test-CityArchiveRowBound.ps1): the remembered slot is interrupted at 9 and a live older Mounted Charge row sits at 9. Encode and decode succeed with exactly one Charge row at 9, no public binding and one placement record. Control: adding a kind-6 row at 9 is refused with 'Invalid or duplicate owned archive control slot.'

Component regression RememberedSlotOccupied: unique archive keys, the Charge row kept at 9, the shortcut off the bar with one record, and no mount shortcut restored after a load.

The mutation ignoring occupancy failed exactly this regression. The gate passed on 9fd90f6b.
- **R13-F4** — `fixed` — commits: `9fd90f6bc4`
  PendingSlotInvalidation now records the player world (Game.Instance.Player) and the reported kind when it is created. ApplySlotInvalidation first requires MountedControlSlotPolicy.DeferredObservationApplies(originWorld, currentWorld, IsOnPlayerRoster(exact unit)). This check runs before ArrangementsAccepted, so a new world cannot release the fence first. Work from a replaced world is discarded.

Regression DeferredCleanupAcrossWorlds:
- A queued invalidation for outgoing rider A at 9, with the cleanup postfix omitted.
- The world is then replaced by a save in which A's shortcut was removed (a different instance of A), and the pending work is resolved.
- The incoming bar stays empty and its archive has no public binding.
- Control: when the load is abandoned and the outgoing world survives, the deferred cleanup still applies, and the slot returns once at 9.

The mutation forcing the check true failed exactly this regression (Expected=-1 Actual=9). The gate passed on 9fd90f6b.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 63.7s | Result of record, on commit 9fd90f6b (log gate-focused-9fd90f6bc498-d43d449b6c4-a030b738800-20261121T175341221750Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the rebind-shape row and archive bound 9/0. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.96 min | Development run. Components 1472/0, Validate-Source 153/0, assembly contracts 822/0. The rebind-shape row read newobj MechanicActionBarSlotAbility x1, stfld Ability x1, 3 SetSlot calls and one empty-slot newobj. Click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1467/5. The mutations reverted the domain decisions (IsNativeRebind forced false, recorded-alias invalidation disabled, occupancy ignored, DeferredObservationApplies forced true) and, in the double, the observed and leased placement record (the R13-F2 mirror). Exactly the five targeted regressions failed. All mutations were reverted and a grep confirmed none remained. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
