<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r26-request part=1/1 sha256=185f76f650f84b55ac9b96b31764c2e1cba6187b21a84416505a4121ad3f47ac -->
### Claude Opus — review request — round 26 — candidate `6298f2a527`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r26-b91e50` |
| Checkpoint | `final` (final) |
| Candidate H | `6298f2a5274306e35f8f0ceaf57869693cd0611f` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 26 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 25 resolves all six pending findings in commit 6298f2a5.
- **R25-F1:** SuspendAreaControls runs AdmitReserveBindings before the expanded sweep, so never-observed reserve bars are classified before they are cleared. The component double now uses the production sweep membership.
- **R22-F3:** in the first reserve scan, an interrupted choice yields to an identifiable new binding (a recorded bar, and the binding is not one of its copies). An untouched copy still yields to the free chosen slot.
- **R23-F3:** while the public control is not live, the player's moved effective alias is recorded as the choice before KMC's own migration empties it.
- **R24-F3:** with no current player, the copy registry is neither cleared nor roster-pruned. A distinct world, disable or dispose still releases it.
- **R25-F2:** the reserve part of RemoveAllManagedFacts clears an exact KMC AutoUseAbility selection and keeps an ordinary one.
- **R25-F3:** already-scanned reserve bars holding a fresh KMC binding are classified again (ClassifyFreshReserveBinding) and cleared, so the new binding supersedes the remembered slot.

The double mirrors every production change. Three tests were extended and one added, and the handoff doc has been updated. The Sprint4NaturalScenario and RuntimePersistenceCampaignBScenario runtime fixtures were not run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 26.3s | Controller focused gate on committed HEAD 6298f2a5, exit 0, worktree unchanged. It covered the build, components, Validate-Source and the six-child focused wrapper. Offline only; no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.6 min | Local run with PASS lines temporarily silenced in tests/TestRunner.cs (reverted; absent from the diff). Components 1506/0, Validate-Source 153/0. The toolbar tests use a labeled double that drives the production domain and mirrors the service; this is not native evidence. Runtime scenarios were not executed. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (three mutation runs, reverted) | no | pass | ~0.6 min each | Each run mutated double lines that mirror production, split so that no failure masks another. Run A reverted area classification, the recorded-bar move rule, inactive alias recording and the null-player registry guard; 4 targeted regressions failed. Run B reverted the auto-use clear; its regression failed. Run C reverted the fresh-binding classification; its regression failed. All mutations were reverted before commit. They show the tests react to the mirrored lines, not to the production service file itself. |

**Evidence references:**
- commit 6298f2a5274306e35f8f0ceaf57869693cd0611f
- gate-focused-6298f2a52743-d43d449b6c4-a030b738800-20261122T010248302942Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 25 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (DepartedRiderReleasesCopies 3-free cases, SurvivingAliasAfterCanonicalCleanup inactive moved alias, MainMenuNewGameReleasesRecords copy gap and transition, ReserveBoundaryCoverage)

**Prior findings to assess:** `R22-F3`, `R23-F3`, `R24-F3`, `R25-F1`, `R25-F2`, `R25-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
