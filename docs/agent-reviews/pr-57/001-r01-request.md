<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r01-request part=1/1 sha256=7a6b8dc1fcca5f87c9bdb11d40bfade0a248e3abf5ae365e162b4f62842dd042 -->
### Claude Opus — review request — round 1 — candidate `78a0964e37`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r01-5cd705` |
| Checkpoint | `final` (final) |
| Candidate H | `78a0964e376a690e4b42ff81e5dd110cba5e6b61` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 1 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

All three mission requirements are implemented in source (commits 2e564390 and 78a0964e), and the focused gate passed on the pushed candidate 78a0964e. Every cause below comes from reading code; none was reproduced in the game, and the decompiled game source used is the owner's private local copy.

(1) Toolbar. Cause: the native toolbar empties any slot whose ability fact is removed or inactive (UnitUISettings.GetSlot / CleanupBadSlots, IsBad). The public Mount / Dismount fact goes away temporarily in several ordinary cases: area suspension also clears the KMC slots and has gaps in its restore rows; CapitalCompanionLogic moves companions to RemoteCompanions or takes them out of the game; a save in that state writes no row. The one-time placement record then stops the icon from returning. Which path the owner hit is not established. Fix: MountedControlPlacementIntent (domain) keeps each rider's chosen slot by actor id and index only, wired into the existing reconcile, capture and restore path. Moves, deliberate removal and occupied slots are respected, and there is no duplication. The record is forgotten on permanent loss, disable and load. Saved rows reuse the existing kinds, so the save format does not change. Roster membership is matched by actor id: this load-order defect was found in my own change before review and fixed in 78a0964e.

(2) City companions. Cause: in the capital the full-screen roster uses UIUtility.GetGroup(withRemote, withPet), token 06004281. It lists every party and reserve owner but adds pets only from in-game ControllableCharacters, so the pets of out-of-game reserve owners are missing. Fix: an exact-token postfix projects each listed owner's own reciprocal, non-summon pet into that list, through a domain policy. The two city issues share a trigger (the capital roster change) but not a defect.

(3) Prone target. Cause: unconscious units are prone, and native UnitAttack keeps an explicit order on an enemy that was already unconscious when ordered. The mounted pair path required a conscious target at the click, intent, admission and command gates, and cancelled silently. Fix: TargetLifeAdmits mirrors the native rule for player orders only; automatic proposals, continuation to the next target and Charge are unchanged. Uncertainty: no mounted-specific failure was found in code for a conscious tripped enemy.

Native checks were not run and are deferred to owner verification. The missing prerequisite is a scenario and fixture for a real CapitalCompanionLogic capital with a reserve custom companion owning a pet, and a scenario that downs an enemy before a mounted ranged click. Steps are in docs/CITY-AND-PRONE-HANDOFF.md. Added scripts/Test-CityToolbarProneFocused.ps1, because the gate accepts a single focused script; it runs exactly the assembly contracts (Kingmaker) and the Tabletop Rules Expansion click composition.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (runs powershell.exe -File scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1) | yes | pass | 19.6s (incremental build; full local build run 22.8s build + ~20s tests) | Candidate 78a0964e, worktree unchanged. Build PASS; FAST components 1427/0 (includes 16 new tests: 8 placement-intent, 5 city-roster projection, 3 downed-target order); Validate-Source 153/0; focused wrapper: Test-AssemblyContracts.ps1 -Target Kingmaker 819/0 (4 new rows: GetGroup static/List<UnitEntityData>/token 06004281, GroupController.SetGroup uses GetGroup, GetGroup pets from ControllableCharacters, UnitUISettings.GetSlot/CleanupBadSlots replace IsBad slots) and Test-GunslingerClickComposition.ps1 21/0 in both patch orders. Offline only: unit tests use labeled test doubles of native slots/units; contracts are reflection-only; composition substitutes native execution. Not native gameplay evidence; Wrath not selected; no Candidate/Full. |
| Native guarded runtime checks (scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1): capital toolbar persistence, city pet inspection, mounted ranged vs downed/tripped enemy RT/TB | no | not_run | 0 | Deferred to owner verification per mission section 7. No existing scenario or qualified fixture covers a CapitalCompanionLogic capital with a reserve custom companion owning a pet, or downing an enemy before a mounted ranged click (the existing capital-arrival rows use Oleg's Trading Post, where companions stay in the party). Owner playtest steps are in docs/CITY-AND-PRONE-HANDOFF.md. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-78a0964e376a-d43d449b6c4-a030b738800-20261121T120006815015Z.log
- docs/CITY-AND-PRONE-HANDOFF.md (causes, fixes, uncertainties, acceptance record, owner playtest steps)
- CURRENT.md (new top section)
- scripts/Test-CityToolbarProneFocused.ps1
- scripts/Test-AssemblyContracts.ps1 (city roster / toolbar rows)
- tests/MountedControlPlacementIntentTests.cs
- tests/CityCompanionRosterProjectionTests.cs
- tests/MountedDownedTargetOrderTests.cs
- commit 2e5643906c788abe8d445e7c83327dc512f8ed9c
- commit 78a0964e376a690e4b42ff81e5dd110cba5e6b61

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
