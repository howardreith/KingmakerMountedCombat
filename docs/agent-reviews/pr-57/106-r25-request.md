<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r25-request part=1/1 sha256=809b0b70097d76e19d2a42f1719514ad48bfc48874dba3339f21ceab06037687 -->
### Claude Opus — review request — round 25 — candidate `923e5ccdac`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r25-44836a` |
| Checkpoint | `final` (final) |
| Candidate H | `923e5ccdac7927b492cd410bdedf1fc17819e826` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 25 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 24 resolves the six pending findings in commit 923e5ccd.
- **R18-F1:** CopyYields now makes every known copy yield to an explicit off-bar arrangement. Native emptying of the known duplicate no longer undoes the player's removal.
- **R22-F3:** ClassifyFirstSeenReserveBar runs before the first reserve scan clears a bar, including a bar with no binding left. A live arrangement takes the latest edit: a move becomes the interrupted slot, and a removal becomes off-bar.
- **R23-F3:** alias migration writes the public control into the effective alias slot even while a canonical copy still awaits removal.
- **R24-F2:** the native report path syncs placement ownership before it reads or writes the maps. A slot native recovers in the incoming world before its first update now survives.
- **R24-F3:** MountedControlLoadFence.Accepts(null) keeps the fence, so an abandoned load still gets its records back after a frame with no player.
- **R24-F1:** RemoveAllManagedFacts (save scope, disable, disposal) now includes current-world reserve and detached riders.

The double mirrors every production change. Four tests were extended with the requested evidence, and the handoff doc has Round 24 additions and validation notes.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 25.5s | Controller focused gate on committed HEAD 923e5ccd (exit 0, worktree unchanged). It ran the build, components, Validate-Source and the six-child focused wrapper. Offline only; there was no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.4 min | Local run with PASS lines temporarily silenced in tests/TestRunner.cs; that change was reverted and is absent from the diff. Components 1505/0, Validate-Source 153/0. The toolbar tests use a labeled double that drives the production domain and mirrors the service; they are not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (two mutation runs, reverted) | no | pass | ~0.6 min each | Run A reverted the off-bar yield and the null-world fence in the domain, plus the alias migration and the live reserve classification in the double; its 4 targeted regressions failed. Run B reverted the reserve fact sweep and the report-path ownership sync in the double; its 2 targeted regressions failed. The runs were split so no failure masked another. All mutations were reverted before commit. The double-side mutations show the tests react to the mirrored lines, not to the production service file itself. |

**Evidence references:**
- commit 923e5ccdac7927b492cd410bdedf1fc17819e826
- gate-focused-923e5ccdac79-d43d449b6c4-a030b738800-20261122T003649583515Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 24 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (StaleThenCurrentCanonical readDuplicate, DepartedRiderReleasesCopies rebound-fallback edits and disable, SurvivingAliasAfterCanonicalCleanup canonical copy, MainMenuNewGameReleasesRecords no-player gap and incoming-world report)

**Prior findings to assess:** `R18-F1`, `R22-F3`, `R23-F3`, `R24-F1`, `R24-F2`, `R24-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
