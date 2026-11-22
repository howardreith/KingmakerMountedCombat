<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r20-request part=1/1 sha256=adc42e2356a4c099938222c3474b04463e2f3d0c459e807214f7830783dc7267 -->
### Claude Opus — review request — round 20 — candidate `36f60e8130`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r20-a38f1e` |
| Checkpoint | `final` (final) |
| Candidate H | `36f60e813045bae91311905d1a8509ff49b2de54` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 20 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 19 replaces the round 16–18 copy mechanics.
- Copies of an interrupted choice are tracked by slot identity in a service registry for the current world only (`recordedCopies`). Each entry is the exact slot object at its index. Entries are removed when native empties that copy, in any read order, and re-recorded when native rebinds it. The registry is cleared once no choice is interrupted, on world change, on disable and on dispose.
- Live repair, boundary observation, capture classification and the live writer share one decision: `MountedControlSlotPolicy.EffectiveBindingIndex`, passed to `LivePublicRows` as `selectedIndex`. The player's own binding wins wherever it is. Copies yield to a free chosen slot; otherwise the first copy is the fallback. Repair removes every other copy.
- While the control is not live, a fallback copy is promoted (`PromoteFallback`) before KMC's own alias migration clears it.
- A report's copy status and occupancy come from the arrangement that owns it. While a selected load is fenced, that is the outgoing records (`TryGetOutgoingInterruptedChoice`).
- Each native slot operation gets a production `MountedNativeSlotScope` (owner, index, frame).
  - A report is confirmed when native moves to a later slot (`Settle`) or when the operation returns normally.
  - An operation that threw keeps its last report only if the slot holds native's own write: an empty slot bound to the rider, or the live rebind.
  - The player's drag-off removal writes null instead. Two new contract rows pin both premises.
- Retirement now marks only the later reports of the same native operation as stale (`retiredInNativeOperation`). A later operation's report for a new placement establishes the arrangement.
- The test double now drives the production scope and effective-binding policy. All 74 earlier toolbar tests passed against it before 7 regressions were added.
- The handoff doc has a Round 19 section and updated validation records.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 22.8s | Controller focused gate on committed HEAD 36f60e81 (exit 0, worktree unchanged). The Fast tier builds and runs components plus Validate-Source, and the focused wrapper runs six children: assembly contracts, gunslinger click composition, archive row bound, chunk7 six-pair adapter, area adapter baseline and target liveness. Offline only: substituted native boundaries and reflection contracts, no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.39 min | Local run on the fixed tree with PASS lines temporarily silenced in tests/TestRunner.cs (reverted, absent from the diff). Results: components 1501/0 (81 toolbar tests), Validate-Source 153/0, assembly contracts 825/0, archive bound 9/0, target liveness 3/0, focused 6/6. The toolbar tests use a labeled double that drives the production MountedNativeSlotScope, EffectiveBindingIndex and placement intent; this is not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (two production mutation runs, reverted) | no | pass | ~0.6 min each | Run 1 reverted the native-write keep in Close, PromoteFallback, the outgoing interrupted choice and the first-copy fallback. It failed exactly the R17-F2, R18-F1, R19-F1 and R17-F1 regressions plus two earlier tests on the same decisions. Run 2 made every binding a copy, in IsSecondaryBinding and ObserveNativeSlotInvalidated. It failed exactly R15-F1, R19-F2 and R19-F3 plus two earlier new-placement tests. All mutations were reverted before commit; the result 'pass' means each mutation was detected as intended. |

**Evidence references:**
- commit 36f60e813045bae91311905d1a8509ff49b2de54
- gate-focused-36f60e813045-d43d449b6c4-a030b738800-20261121T222709063774Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 19 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (OutOfOrderCopyReads, AbandonedLoadFallbackOccupied, ExceptionalNativeOperations, OccupiedChosenSlotWithTwoCopies, FallbackBeforeOwnMigration, MoveBeyondSurvivingCopy, NativeEmptiesNewPlacement)
- scripts/Test-AssemblyContracts.ps1 (empty slots bound to the unit; drag-off removal writes null before GetSlot)

**Prior findings to assess:** `R15-F1`, `R17-F1`, `R17-F2`, `R18-F1`, `R19-F1`, `R19-F2`, `R19-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
