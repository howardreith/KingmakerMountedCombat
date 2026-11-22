<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r23-request part=1/1 sha256=eab3ee74d052e3a84484197917a39c9cf055f141d4139448739bea1381c74b9b -->
### Claude Opus — review request — round 23 — candidate `dd7b6efa19`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r23-07ce80` |
| Checkpoint | `final` (final) |
| Candidate H | `dd7b6efa1935ebf6257ee2dd02661d3665e7a361` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 23 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 22 resolves all six pending findings in commit dd7b6efa.
- **R22-F1:** copy identity now asks which slot the arrangement names, live or interrupted (`TryGetChosenSlot`), separate from the archive query `TryGetArrangement`. A fallback adopted at a boundary stays the player's slot on later reads of the same bar.
- **R18-F1:** when native rebinds the player's own binding, `TrackCopies` records the other public bindings as copies.
- **R19-F2:** with no interrupted choice, native reports are attributed by the effective binding of the owning records: the live records, or `MountedControlLoadFence.OutgoingRecords` while fenced. This replaces the raw first canonical index.
- **R22-F2:** a fresh load with several public rows seeds the first restorable row (`MountedControlSlotPolicy.SelectSeededPublicRow`). Replays keep the first row.
- **R22-F3:** the first reserve scan classifies the effective binding before clearing the bar. A fallback copy for an occupied chosen slot is promoted, same companion only, and the copies are released afterwards.
- **R20-F1:** each update releases the recorded copies of actors no longer among the current world's player characters.

The double mirrors every production change. Four tests were extended, one test was added, and the handoff doc was updated.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 27.8s | Controller focused gate on committed HEAD dd7b6efa (exit 0, worktree unchanged): build, components, Validate-Source and the six-child focused wrapper. Offline only; no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.78 min | Local run with PASS lines temporarily silenced in tests/TestRunner.cs (reverted, absent from the diff): components 1503/0 (83 toolbar tests) and Validate-Source 153/0. The toolbar tests use a labeled double that drives the production domain and mirrors the service; this is not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation run, then an isolated run, all reverted) | no | pass | ~0.6 min each | The combined run reverted six decisions. Domain: TryGetChosenSlot for live arrangements, and SelectSeededPublicRow. Double, each mirroring a production line: sibling recording on a rebind, effective-binding attribution, reserve fallback promotion, and departed-rider release. Five targeted regressions failed. The departed-rider release mutation was masked by an earlier failing assertion in the same test; an isolated run with only that mutation failed its assertion. All mutations were reverted before commit. |

**Evidence references:**
- commit dd7b6efa1935ebf6257ee2dd02661d3665e7a361
- gate-focused-dd7b6efa1935-d43d449b6c4-a030b738800-20261121T234143312673Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 22 additions; Validation)
- tests/MountedControlPlacementIntentTests.cs (OccupiedChosenSlotWithTwoCopies immediate save, StaleThenCurrentCanonical native rebind, MoveBeyondSurvivingCopy invalidated 11, DepartedRiderReleasesCopies occupied scan and removal before scan, LegacyRowsSkipOccupiedFirst)

**Prior findings to assess:** `R18-F1`, `R19-F2`, `R20-F1`, `R22-F1`, `R22-F2`, `R22-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
