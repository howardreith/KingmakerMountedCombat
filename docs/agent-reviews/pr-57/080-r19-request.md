<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r19-request part=1/1 sha256=28385dcbe4f5ef9e862e01adb9e6b06c3e86f279ef663a75a22286ce1e3a9a7a -->
### Claude Opus — review request — round 19 — candidate `751124ab84`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r19-3b8f24` |
| Checkpoint | `final` (final) |
| Candidate H | `751124ab84352b60c07c664b48b22a9b10f8100f` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 19 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 18 fixes all seven pending findings in commit 751124ab. The guarded push succeeded, the controller focused gate passed on that commit (45.2 s, worktree unchanged) and the worktree is clean.

- **R15-F1 / R17-F1 (copies of an interrupted choice):** when native empties a recorded copy, the copy is now dropped if the chosen slot is free, so a later placement in that slot belongs to the player. If the chosen slot is occupied, the copy becomes the interrupted choice and the same pet's control returns there. Copies whose slot no longer holds a public binding are also pruned at every observation and repair.
- **R17-F2 (aborted native operations):** the GetSlot bracket now covers only its own slot index; the contract row pins the `index` parameter name. A report counts only if native actually replaced the slot object. Reports left by an operation that never returned are dropped at the next update, never applied.
- **R17-F3 (retired rider batches):** a native report for a retired actor with no arrangement can no longer recreate one, so a second report in the same cleanup batch cannot give pet A's shortcut to pet B, fenced or not.
- **R18-F1 (multiple copies):** repair now removes every surviving secondary copy, not just the first.
- **R18-F2 (secondary aliases):** an alias report counts only when that alias is the bar's selected alias, in both the live and outgoing records.
- **R18-F3 (combat harness):** the target-liveness assertion in Test-Harness.ps1 now checks the delegated TargetLifeAdmits rule and still checks termination, attack completion, in-state, hostility and CanAttack. A new script, Test-CityTargetLiveness.ps1, evaluates that predicate in the focused wrapper, which now has six children.

Six new toolbar regressions cover these cases. I temporarily silenced PASS lines in tests/TestRunner.cs to see failures during development; that change is reverted and not in the commit. A combined mutation run failed exactly the six new toolbar regressions.

Native checks remain NOT RUN and are deferred to the owner playtest in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 45.2s | Result of record on commit 751124ab (log gate-focused-751124ab8435-d43d449b6c4-a030b738800-20261121T194246132670Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's six children, including the new Test-CityTargetLiveness.ps1. Offline only. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.74 min | Development run: components 1494/0, Validate-Source 153/0, assembly contracts 823/0 (GetSlot row now also pins the 'index' parameter), click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, target liveness 3/0, focused 6/6. The tests/TestRunner.cs change was reverted. The rest of Test-Harness.ps1 was not run; only its changed assertion is reproduced by Test-CityTargetLiveness.ps1. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1488/6. Domain mutations: copy dropping and fallback promotion disabled, retired-actor guard removed. Double mutations, each mirroring the production line: aborted reports applied instead of discarded, only the first copy removed, selected-alias check removed. Exactly the six new toolbar regressions failed. All mutations were reverted and a grep confirmed none remained. R18-F3 is a script change, checked by Test-CityTargetLiveness.ps1 rather than by mutation. |

**Evidence references:**
- commit 751124ab84352b60c07c664b48b22a9b10f8100f
- gate-focused-751124ab8435-d43d449b6c4-a030b738800-20261121T194246132670Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (ObserveNativeSlotInvalidated copy drop/fallback promotion and retired guard, ObserveNativeCleanupOutcome, PruneCopies)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (GetSlot index scope, selected-alias match, ChosenFree, slot-replacement requirement, DiscardPendingBadSlots, PruneCopies calls, removal of all copies)
- src/KingmakerMountedCombat/Integration/MountedPatchController.cs (SlotReadPrefix with index)
- scripts/Test-AssemblyContracts.ps1 (GetSlot 'index' parameter)
- scripts/Test-Harness.ps1 (target-liveness assertion)
- scripts/Test-CityTargetLiveness.ps1 (new)
- scripts/Test-CityToolbarProneFocused.ps1 (six children)
- tests/MountedControlPlacementIntentTests.cs (NewPlacementInFormerCopySlot, FallbackCleanupWithOccupiedChoice, AbortedNativeOperation, RetiredRiderCleanupBatch, RepairRemovesAllCopies, SecondaryAliasReport)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 18 additions, Validation)

**Prior findings to assess:** `R15-F1`, `R17-F1`, `R17-F2`, `R17-F3`, `R18-F1`, `R18-F2`, `R18-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
