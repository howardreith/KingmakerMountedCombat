<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f key=r01-request part=1/1 sha256=7fdd9b1c324cf85bd0ed26107df63a1bf6518f82547bbd6527dcffc16cbf9326 -->
### Claude Opus — review request — round 1 — candidate `83fdf4937c`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f-r01-125c08` |
| Checkpoint | `final` (final) |
| Candidate H | `83fdf4937c08fb023896fea23e5ec82ca5a60c12` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission-with-toolbar-repair-addendum` sha256 `2faba57367` |
| Workflow | `17b66fac32` |
| Developer observed | headless session `b3b4495c-89c0-495f-9967-40a664b6eb37`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

The toolbar lifecycle repair required by addendum C–F is implemented. The city companion roster (§5) and the downed-target attack change (§6) are unchanged; only the §6 wording was corrected, exactly as addendum B requires.

What changed
- Design (addendum C). A new production component, src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs, owns every toolbar decision, its sequencing and its actor selection at each boundary:
  - update and observation of player edits;
  - native GetSlot/CleanupBadSlots invalidation, including operations aborted by a throw;
  - save header and save scope; area suspend and resume;
  - selected load begin, abandon and completion by world replacement; unfenced disposal and the main menu;
  - companion replacement and retirement; disable and dispose.
- Its per-world state and the pure evidence rule (ApplyBar) live in MountedControlPlacementIntent.cs.
- NativeMountedControlService.Toolbar.cs implements the narrow IMountedToolbarHost adapter. It reads and writes bars, facts, rosters and the player world; the main service file went from 3,823 to 2,272 lines.
- Placement eligibility (riders, observed former riders, reserve and detached owners) is kept separate from sanitation coverage. Sanitation adds every observed former rider, riders who rejoined before the next update, and reserve owners' exact persistent pets.
- Save format is unchanged: row kinds 6 and 7, the 128-row bound, validation. Native contract pins and the Tabletop Rules Expansion click composition are kept.

Tests (addendum C2, D)
- The duplicated test Service is gone. tests/MountedControlPlacementIntentTests.cs drives the real component through thin fakes: bars and slots, facts and AbilityData generations, roster membership, world identity, archive rows. 28 tests.
- D1–D6 each map to named tests (handoff section 4). D2 runs six edits update-first and save-first on identical games and compares archive rows, bars and cold-loaded bars.

Inherited acceptance cases (addendum F). Each PASS; evidence type UNIT, the production lifecycle through thin fakes; all six pass in bin\Tests 1448/0 and in the mutation baseline 28/0 on 83fdf493:
- AC-R26-F4 — AcR26F4ReservePetSanitized: the horse's stale Mount Primary slot and auto-use are absent inside serialization and after disable; ordinary ability and auto-use survive.
- AC-R22-F3 — AcR22F3FreshPlacementAfterNativeEmpty: archive holds 11 and one record; one shortcut at 11 on same-world return and cold restore; no-edit control gives 3.
- AC-R25-F3 — AcR25F3FreshShortcutAfterReplacement: one shortcut at 11 for pet B after cold restore; without a placement, 9 is never restored.
- AC-R26-F1 — AcR26F1CanceledLoadKeepsEdit: immediate save carries 11 and one record; cold restore gives 11 once; no-edit gives 9.
- AC-R26-F2 — AcR26F2MainMenuReleases: no records, world, observed unit, native operation or load at the main menu; fenced and real-transition controls pass.
- AC-R26-F3 — AcR26F3RejoinedRiderAutoUse: the stale Rider Primary selection is null inside serialization and after disable, including a disabled save; ordinary selection kept.

Regressions and mutations (addendum E)
- The handoff (section 6) maps all 76 finding IDs R1-F1…R26-F4 (round 6 raised none) to a test or subcase, or for non-toolbar findings to its existing evidence.
- scripts/Test-ToolbarLifecycleMutations.ps1 is the seventh child of the focused wrapper. It compiles temp copies of the production component and the tests, requires the unmutated baseline to pass (28/0), then applies ten single-line production mutations in four classes:
  - skipped pre-cleanup observation (M1a, M1b);
  - an omitted sweep actor (M2a, M2b);
  - mishandled outgoing-world lifetime: abandon, replacement, unfenced disposal (M3a–c);
  - player edit confused with native invalidation (M4a–c).
- Each mutation is killed by its named tests, and the worktree sources stay byte-identical. A development gate on 45e799ce showed M4c surviving its named test; the D6 removal subcase now saves and cycles an area after the removal, and M4c is killed (17 tests fail).

Deviations from the first enrollment's candidate (stated, not silent; handoff "Decisions that differ")
1. The arrangement is the set of slots the player keeps. KMC creates no copy and removes none the player placed. The 0.2.13 baseline also never removed duplicates; the previous candidate's "exactly one control" removal was its own addition. Findings that expected exactly one control on a bar where the player kept several now expect the player's bindings; they are marked *set* in the regression map: R12-F1, R15-F1 r16/r17, R17-F1, R18-F1, R19-F2, R21-F1, R22-F1, R23-F1.
2. There is no load fence: records are kept per world and released on replacement or unfenced disposal.
3. The IsBad postfix is removed. Native emptying is observed by bracketing GetSlot and CleanupBadSlots; the IsBad contract rows stay pinned.
4. Reserve bars are observed and swept every update; this replaces the one-time scan ledger.
5. The save scope returns the public control through the lifecycle's pending slots, not through leases.

Validation. The controller focused gate passed on 83fdf493 in 52.3 s:
- build;
- components 1448/0: the 86 old toolbar test-double tests are replaced by 28;
- Validate-Source 153/0; assembly contracts 825/0;
- TRE click composition 21/0, both patch orders;
- archive bound 7/0, now with lifecycle-written rows;
- six-pair adapter 22/0; area adapter baseline matched at 63/63; target liveness 3/0;
- mutations: baseline 28/0, 10/10 killed.

Not run
- No native case ran, and no runtime transaction occurred.
- Native cases are not runnable here because no command available to this enrollment builds the commit-bound candidate package the guarded runner requires. They stay NOT RUN and are deferred to the owner playtest in handoff section 11.
- That playtest compares standing, conscious-prone (tripped) and already-unconscious enemies, mounted and dismounted, with an observed attack roll in each case.

Counts
- First enrollment: 26 review rounds, 27 writer invocations.
- This enrollment: writer invocation 1; review round 1 pending.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (candidate 83fdf493) | yes | pass | 52.3s | Fast tier: build, components 1448/0, Validate-Source 153/0, focused wrapper 7/7 (assembly contracts 825/0 Kingmaker only; TRE click composition 21/0 both orders; archive row bound 7/0; six-pair archive adapter 22/0; area adapter baseline 63/63 matched, compile only; target liveness 3/0 source checks; toolbar lifecycle mutations baseline 28/0 and 10/10 killed). Offline only; no Candidate, Full, package or native run. |
| agent_review.py verify --gate focused (same arguments) on 63d35c13 | no | pass | 53.2s | Same counts on the previous commit (identical code except a no-allocation early exit and docs); superseded by the 83fdf493 receipt. |
| agent_review.py verify --gate focused (same arguments) on 45e799ce | no | fail | 32.9s | Everything passed except the mutation child: M4c survived its named D6 test. That test was strengthened in 63d35c13; retained as failed evidence. |
| powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-Tiers.ps1 -Tier Fast [-SkipBuild] -Focused Test-CityToolbarProneFocused.ps1 (local development runs, uncommitted tree) | no | fail | about 1 min each | Budget entries 20261122T021853947Z (with build, exit 1) and 20261122T022017262Z (-SkipBuild, exit 1): tool output was truncated before any FAIL line. Then, with tests/Program.cs temporarily registering only the toolbar tests: 20261122T022130038Z failed the build (CS0162, warnings as errors), and 20261122T022136817Z showed 28/0 toolbar tests but still exited 1 later. The temporary change was reverted before any commit and is absent from the diff. Development runs, not results of record. |
| dotnet build src/KingmakerMountedCombat/KingmakerMountedCombat.csproj -c Release; dotnet build tests/KingmakerMountedCombat.Tests.csproj -c Release | no | pass | 15.5s / 1s | Development compiles only. |
| Native in-game toolbar, city companion and prone attack cases | no | not_run | 0 | The guarded runner requires a commit-bound candidate package; no command available to this enrollment builds one. Deferred to the owner playtest (handoff section 11) with exact steps; not described as game-tested. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-83fdf4937c08-d43d449b6c4-a030b738800-20261122T023227505294Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-63d35c134931-d43d449b6c4-a030b738800-20261122T022745636297Z.log
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f/evidence/gate-focused-45e799ce893a-d43d449b6c4-a030b738800-20261122T022401281026Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.Toolbar.cs
- tests/MountedControlPlacementIntentTests.cs
- scripts/Test-ToolbarLifecycleMutations.ps1
- tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs
- commits 45e799ce, 63d35c13, 83fdf493 on codex/mounted-city-and-prone-fixes

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
