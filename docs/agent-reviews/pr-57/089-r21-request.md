<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r21-request part=1/1 sha256=f7cf2afc269ef0573c17644eaa5af8082da7f4ecdf19879f54d19a78070cf8b9 -->
### Claude Opus — review request — round 21 — candidate `a009bd952a`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r21-ad3a37` |
| Checkpoint | `final` (final) |
| Candidate H | `a009bd952abac9e115746fc114a15bd40f88b4a5` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 21 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 20 fixes the four pending findings in commit a009bd95.
- **R19-F2:** when native rebinds the player's effective slot, `TrackCopies` now keeps the identities of the surviving copies.
- **R18-F1:** a report for a recorded copy is accepted even after the choice turned live again. In the domain, a copy of a live arrangement elsewhere yields (`CopyYields`), whether native empties or rebinds it. Only an interrupted choice with an occupied slot lets a copy become the fallback.
- **R17-F2:** an aborted native scope is now closed, keeping only native's own write, at `CapturePersistentSlots`, at the start of the save scope and at area suspension, as well as at update and slot operations.
- **R20-F1:** the copy registry releases a rider's slot objects whenever KMC clears its bindings (`ClearManagedHotbarSlots` and `ClearPublicControlSlots`, current world only). Stale pairs are pruned at every repair. The placement records keep the chosen index and pet identity.
- The test double mirrors every production change. Three regressions were extended as requested and one was added. The handoff doc has Round 20 additions and validation notes.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 46.0s | Controller focused gate on committed HEAD a009bd95 (exit 0, worktree unchanged). The Fast tier builds and runs components plus Validate-Source, then the six-child focused wrapper: contracts, gunslinger composition, archive bound, six-pair adapter, area adapter baseline and target liveness. Offline only, no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.6 min | Local run on the fixed tree with PASS lines temporarily silenced in tests/TestRunner.cs (reverted, absent from the diff). Components 1502/0 (82 toolbar tests), Validate-Source 153/0. The toolbar tests use a labeled double that drives the production MountedNativeSlotScope, EffectiveBindingIndex and placement intent and mirrors the service's report, apply and registry order. Not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation run, reverted) | no | pass | 0.6 min | Reverted four decisions. In the domain: the live-arrangement case of CopyYields. In the double, each mirroring the production line: keeping copies on a rebind, closing the scope at capture, and releasing copies on clear. Exactly the four targeted regressions failed. All mutations were reverted before commit; 'pass' means each mutation was detected as intended. The double-side mutations show the tests react to the mirrored line, not to the production service file itself. |

**Evidence references:**
- commit a009bd952abac9e115746fc114a15bd40f88b4a5
- gate-focused-a009bd952aba-d43d449b6c4-a030b738800-20261121T224858182070Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 20 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (MoveBeyondSurvivingCopy rebind variant, OccupiedChosenSlotWithTwoCopies rebind variant, ExceptionalNativeOperations immediate save, DepartedRiderReleasesCopies)

**Prior findings to assess:** `R17-F2`, `R18-F1`, `R19-F2`, `R20-F1`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
