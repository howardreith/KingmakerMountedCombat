<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r12-dispositions-bdeefa9e05 part=1/1 sha256=bdeefa9e059c96556d5e128e79b3f2bbf67d4f06875c63f604206b7a8aa86e17 -->
### Claude Opus — finding dispositions — round 12

Candidate after corrections: `59eda25267446aad9b6d6e45986875a13fdef1f3`

- **R12-F1** — `fixed` — commits: `59eda25267`
  ClassifyArrangementsForCapture now selects PublicBindingIndex(slots), the same RecoveredPublicSlot index that LivePublicRows writes. It decides 'written' by IsCallableLiveBinding on that exact selected slot, within the archive's 0 to 127 bound. The old any-slot check, LiveCaptureWritesPublicSlot, is removed. A stale first canonical binding at 3 is therefore marked interrupted and carried by CaptureAbsentIntent as kind 6 at 3, even when a later binding at 9 is callable.

Component regression StaleThenCurrentCanonical: a recorded rider saves first with stale data at 3 and current data at 9. The archive holds one public binding, at 3, one placement record and unique keys. A cold load restores one control at 3, and slot 0 is unchanged.

Production codec check: rows emitted by LivePublicRows and AbsentRowsAfterLive for that bar round-trip through MountedArchiveCodec with exactly one binding, at 3, and one placement record.

The mutation restoring the any-slot callability check failed exactly this regression. The controller gate passed on 59eda252.
- **R12-F2** — `fixed` — commits: `59eda25267`
  ObserveArrangementOnBar now passes PublicBindingIndex(slots) to ObserveAtBoundary. A returned older alias still on the bar is observed as bound at its index, not as an empty slot, while canonical precedence is kept.

Regression AliasSecondSave:
- Untouched: a save scope returns the same companion's alias at 9, and a second save runs before the next update. That archive holds one kind-6 row at 9 with unique keys, and the continuing world gets the shortcut back. After updates the world holds one control at 9, a third save keeps the row at 9, and a reload restores one control at 9.
- Removed before the second save: no public binding row is written, the continuing world stays empty after updates, and a reload keeps it removed.

The mutation restoring canonical-only observation failed exactly this regression ('The second save did not return the shortcut to the continuing world'). The controller gate passed on 59eda252.
- **R12-F3** — `fixed` — commits: `59eda25267`
  RetireOnCompanionLoss decides whether the bar holds the control with IsPublicBindingSlot (IsPublicControlKind of the resolved blueprint), and ClearPublicControlSlots clears every such slot: the canonical control and both exact older aliases. Retirement runs at the start of each candidate's update, before live repair, and at every bar observation, before capture. A retired alias is therefore gone before ReconcilePublicControlSlots or the live writer could adopt it.

Regression AliasCompanionReplacement, update-first and save-first: pet A's returned alias waits at 9 and the link switches directly to eligible pet B. Afterwards there is no binding or alias at 9 and no public-binding archive row, and slots 0 (Attack) and 5 (Potion) are unchanged. Same-pet control: the alias migrates once to one control at 9.

The mutation restoring canonical-only cleanup failed exactly this regression ('Pet B inherited pet A's slot 9 (saveFirst=False)'). The controller gate passed on 59eda252.
- **R12-F4** — `fixed` — commits: `59eda25267`
  Native contract read from the owner's local decompilation of UnitUISettings: CleanupBadSlots rebinds a bad ability slot to an active fact with the same blueprint and SourceItem, or empties it, while GetSlot always empties it.

New exact patch: MountedPatchController.NativeCleanupBadSlotsToken = 0x06003004, a prefix and postfix on UnitUISettings.CleanupBadSlots. A new assembly-contract row requires an instance void method with no parameters and that token; it passes (821/0).

ObserveInvalidatedPublicSlot queues the report as a pending entry while CleanupBadSlots runs on that bar. EndNativeBadSlotCleanup resolves it: if the slot now holds a different binding of the same blueprint, native rebound it and ObserveNativeCleanupOutcome(rebound: true) records it as live (ObserveBound, companion check first). Otherwise the ordinary invalidation or alias recovery applies. GetSlot reports are applied immediately, the fenced outgoing world keeps its existing path, and Update resolves leftovers if the native call did not return.

Regression NativeRebindOutcome:
- A stale canonical slot at 9 is rebound by native CleanupBadSlots, and the player removes it before the next update. It stays removed, the save holds only the placement record, and a load leaves it empty.
- Control: native emptying (GetSlot) without an edit returns one shortcut at 9.

The mutation that ignores the rebound outcome in the domain failed exactly this regression (Expected=-1 Actual=9). The controller gate passed on 59eda252.

Limit: the new native patch and the CleanupBadSlots call timing are NOT RUN in game.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 45.4s | Result of record on commit 59eda252 (log gate-focused-59eda2526744-d43d449b6c4-a030b738800-20261121T173729739497Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including the assembly contracts with the new CleanupBadSlots token row and archive bound 8/0. Offline only; the new Harmony patch is not exercised natively. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (token discovery, then the fixed tree with PASS lines temporarily silenced) | no | pass | 0.73 min | Development runs. In the first, a temporary non-zero contract check printed the CleanupBadSlots token 06003004, which was then pinned; the R12-F1 to F3 changes passed in that run (components 1464/0). On the fixed tree: components 1468/0, Validate-Source 153/0, assembly contracts 821/0, click composition 21/0, archive bound 8/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation: domain rebind outcome ignored; double with canonical-only observation, canonical-only retirement and any-slot callability) | no | fail | ~1 min | The intended failing reproduction, with FAIL lines visible: 1464/4. Exactly the four new regressions failed: stale-then-current canonical, second save of a returned alias, companion replacement with a waiting alias, and native rebind followed by removal. Every mutation was reverted, and a grep confirmed none remained. The F1 to F3 mutations sit in the toolbar double, mirroring the pre-fix production lines; the production service code itself is checked only through the double and the codec fixtures. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
