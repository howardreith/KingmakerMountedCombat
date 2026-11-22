<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r24-request part=1/1 sha256=574a8432f05b02719d739078b59392bf75fcfd8561cc8677c893ebaeff1340a4 -->
### Claude Opus — review request — round 24 — candidate `703eebc17c`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r24-d664fb` |
| Checkpoint | `final` (final) |
| Candidate H | `703eebc17cdd8476d4dfc095f638caafa3251ad1` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 24 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 23 resolves all five pending findings in commit 703eebc1.
- **R18-F1:** repair no longer forgets copies merely because no choice is interrupted. A copy keeps its identity while its object stays on the bar. PruneCopies releases it when it leaves or is replaced, and clearing, departure or a world change releases it too. An emptied entry holds no object and stays only while a choice is interrupted, as the record that this bar was recorded.
- **R22-F3:** when the first reserve scan clears the rider's live chosen slot, it records a temporary absence. A changed or cleared companion link is still retired by AdmitSurvivingBinding.
- **R23-F1:** a native report's yield test also holds when another binding is the owning records' effective one, so a stale lower-priority copy never displaces the callable fallback.
- **R23-F2:** placement records are scoped to the player world through a weak reference (SyncPlacementWorld on Update, BeginSelectedLoad, CapturePersistentSlots and RestorePersistentSlots). A world change outside a selected load releases chosen slots, off-bar records and retirements; a fenced load is untouched.
- **R23-F3:** once native has recorded a rider's bar, an older alias counts as a copy only at its recorded position. ObserveNativeAliasInvalidated takes the caller's identity decision (`bool? reportIsCopy`).

The double mirrors every production change. Three tests were extended, two were added, and the handoff doc was updated. The in-game RuntimePersistenceCampaignBScenario was not run; R23-F2 is shown only by the component model.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 25.8s | Controller focused gate on committed HEAD 703eebc1: exit 0, worktree unchanged. It ran the build, components, Validate-Source and the six-child focused wrapper. Offline only; no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.23 min | Local run with PASS lines temporarily silenced in tests/TestRunner.cs (reverted; absent from the diff). Components 1505/0 (85 toolbar tests), Validate-Source 153/0. The toolbar tests use a labeled double that drives the production domain and mirrors the service. The main-menu/new-game case models ResetToMainMenu and LoadNewGame as a player-world change; RuntimePersistenceCampaignBScenario was not executed. Not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation run, reverted) | no | pass | 0.6 min | Reverted five lines in the double, each mirroring a production change: the forget-when-not-interrupted rule, the reserve chosen-slot interruption, the effective-binding yield, the world-change release and the recorded-bar alias rule. Exactly the five targeted regressions failed. All mutations were reverted before commit. These mutations show the tests react to the mirrored lines, not to the production service file itself. |

**Evidence references:**
- commit 703eebc17cdd8476d4dfc095f638caafa3251ad1
- gate-focused-703eebc17cdd-d43d449b6c4-a030b738800-20261122T001017519053Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 23 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (StaleThenCurrentCanonical withdrawal, DepartedRiderReleasesCopies rebound fallback, SurvivingAliasAfterCanonicalCleanup moved alias, StaleCopyReadKeepsCallableFallback, MainMenuNewGameReleasesRecords)

**Prior findings to assess:** `R18-F1`, `R22-F3`, `R23-F1`, `R23-F2`, `R23-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
