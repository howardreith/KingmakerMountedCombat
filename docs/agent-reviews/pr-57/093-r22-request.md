<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r22-request part=1/1 sha256=001f695151e96676532f0f6ef72937f9bc6fb9611742a9e89069e36bb86025cd -->
### Claude Opus — review request — round 22 — candidate `3ff7fd1446`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r22-a769ea` |
| Checkpoint | `final` (final) |
| Candidate H | `3ff7fd14468556f028206d1000e6267532dccb4c` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 22 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 21 resolves the six pending findings in commit 3ff7fd14.
- **R19-F2:** a native report's ChosenFree is also true while the player holds a canonical binding of its own elsewhere (PlayerBindingElsewhere: not a recorded copy). A copy never becomes the fallback over the moved shortcut.
- **R18-F1:** MountedControlSlotPolicy.EffectiveBindingIndex now takes choiceInterrupted. Copies yield, or one becomes the fallback, only while a choice is interrupted. Copies of a live choice the player removed are no binding, so repair discards them and save-first keeps the removal. The arrangement's own slot is never treated as a copy of it.
- **R21-F1:** while the control is live, ReconcilePublicControlSlots removes every public binding except the effective one, not only recorded copies.
- **R20-F1:** AdmitReserveBindings releases a rider's copy objects on its first scan.
- **R21-F2:** a copy-registry lookup never acquires a world. The registry names its world only while copies are stored (RecordCopies) and clears it once empty.
- **R21-F3:** the acceptance row is split into executed UNIT controls and TRACE source checks.
- The test double mirrors every production change. Five tests were extended with the requested evidence, and the handoff doc has Round 21 additions and validation notes.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 53.3s | Controller focused gate on committed HEAD 3ff7fd14 (exit 0, worktree unchanged): build, components, Validate-Source and the six-child focused wrapper. Offline only, no in-game qualification. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast | no | pass | 0.63 min | Local run with PASS lines temporarily silenced in tests/TestRunner.cs (reverted, absent from the diff): components 1502/0, Validate-Source 153/0. The toolbar tests use a labeled double that drives the production MountedNativeSlotScope, EffectiveBindingIndex and placement intent; not native evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation run, reverted) | no | pass | 0.6 min | Reverted five decisions. In the domain: choiceInterrupted. In the double, each mirroring a production line: PlayerBindingElsewhere, removal of every non-effective binding, the reserve-scan release, and the world-free lookup. Exactly the five targeted regressions failed. All mutations were reverted before commit; 'pass' means each was detected. The double-side mutations show the tests react to the mirrored lines, not to the production service file itself. |

**Evidence references:**
- commit 3ff7fd14468556f028206d1000e6267532dccb4c
- gate-focused-3ff7fd144685-d43d449b6c4-a030b738800-20261121T231435133194Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (Round 21 additions; Validation; Acceptance record negative-control rows)
- tests/MountedControlPlacementIntentTests.cs (MoveBeyondSurvivingCopy occupied variants, OccupiedChosenSlotWithTwoCopies early removal, StaleThenCurrentCanonical update-first, DepartedRiderReleasesCopies reserve scan, DisabledSaveIsolation registry lifetime)

**Prior findings to assess:** `R18-F1`, `R19-F2`, `R20-F1`, `R21-F1`, `R21-F2`, `R21-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
