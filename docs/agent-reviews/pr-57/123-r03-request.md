<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f key=r03-request part=1/1 sha256=cf4ae484f634fababefb61f1200bee39751b528cf1a8eb8cc495245ad9df00bb -->
### Claude Opus — review request — round 3 — candidate `6251885809`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f-r03-57383d` |
| Checkpoint | `final` (final) |
| Candidate H | `6251885809dfcb7f036974ba1ae98eb96f6817af` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission-with-toolbar-repair-addendum` sha256 `2faba57367` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `b3b4495c-89c0-495f-9967-40a664b6eb37`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 3 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Writer invocation 3 fixes all ten round-2 findings (R1-F10, R1-F11, R1-F13, R2-F1…R2-F7). Code changes are in 3f19c91a and b868cd41 (MountedToolbarLifecycle, MountedControlPlacementIntent, tests and the mutation script); the handoff is in 62518858.
- Placement authority now follows exact roster identity (Authors).
- Late-resolving pets are purged of arrangement and marker.
- On a fresh load, every public archive row (kinds 6, 1 and 2) is considered in archive order, and the first restorable one is chosen. Rows never overwrite what the same incoming world already observed or retired.
- The companion check runs before the bar-availability check.
- A thrown native operation is held while a load is open and no player world exists. Its writes are read against the bar as native started (the operation context), and only native-shape writes count.
- Known copies are held weakly and dropped when a sweep clears their binding.
- The archived R9-F1, R17-F3, R18-F1 r24 and R22-F3 sequences are restored as test subcases.
- The handoff now separates executed target predicates (UNIT) from the traced Charge, continuation and automatic filters (TRACE).
- Two owner playtest steps were added: an identifiable item stays equipped on the same reserve pet through reopen and reload, and the chosen slot survives a forced dismount and the same pet's return.
The save format is unchanged. No validation was weakened. The focused gate passed on the pushed HEAD 62518858: components 1463/0, toolbar lifecycle 43/0, wiring 5/0, and all 18 production mutations killed, including new M2c and M3e. The worktree is clean and in sync with origin.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 35.0s | Candidate 62518858, log gate-focused-6251885809df-d43d449b6c4-a030b738800-20261122T035306903450Z.log. Results: build; components 1463/0; Validate-Source 153/0; assembly contracts 825/0; TRE click composition 21/0; archive row bound 7/0; six-pair archive adapter 22/0; area adapter baseline 63/63; target liveness 3/0; toolbar mutations lifecycle 43/0, wiring 5/0, 18/18 killed, worktree unchanged. Offline only, not native evidence. |
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (on b868cd41) | no | pass | 37.7s | Intermediate code commit; same checks and results. Superseded by the gate on 62518858, which changes only docs. |
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (on 3f19c91a) | no | fail | 26.4s | Intermediate commit. Two test-fixture mistakes: an R22-F3 variant name also matched 'moved', and the replacement control left outgoing objects in the party list. Both fixed in b868cd41. |
| Native runtime scenarios (Invoke-KingmakerRuntimeScenario.ps1) | no | not_run | 0s | No commit-bound candidate package can be built with the available commands; handoff section 11 lists each case's prerequisites. Native acceptance is NOT RUN. |

**Evidence references:**
- ~\.agent-review\state\kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f\evidence\gate-focused-6251885809df-d43d449b6c4-a030b738800-20261122T035306903450Z.log
- ~\.agent-review\state\kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f\evidence\gate-focused-b868cd411099-d43d449b6c4-a030b738800-20261122T034846987360Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- tests/MountedControlPlacementIntentTests.cs
- scripts/Test-ToolbarLifecycleMutations.ps1

**Prior findings to assess:** `R1-F10`, `R1-F11`, `R1-F13`, `R2-F1`, `R2-F2`, `R2-F3`, `R2-F4`, `R2-F5`, `R2-F6`, `R2-F7`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
