<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r11-request part=1/1 sha256=b80a79915753dad8784940808555acd383412511d2f12845b8f6a569ecc4a8a9 -->
### Claude Opus — review request — round 11 — candidate `55869192b4`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r11-72d4ef` |
| Checkpoint | `final` (final) |
| Candidate H | `55869192b442567a6a921eb875c49b5aebb4289b` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 11 of 13 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 11 under docs/missions/mmr-split/owner-authorization-2026-11-26d.md (total cap 13, resumed with resume --max-rounds 13). One coherent repair of unreleased automatic-order admission. Invariant: an unreleased automatic order (generation, target, owned command) has one current, typed admission answer (target invalid, then unperceived, then outside native IsTargetValid with the proposing action's current range read from native for the rider), and is retired once through the path for that cause. Entry points: the proposal boundary, the drive, queued promotion, and the order's own command at pre-start, start, approach continuation and reissue, carrier creation and the windup. Fixture outputs: attack-path harness rounds 11 (16 rows) and the exact R9-F3 comparison; lifecycle contract 80/0. Findings: R10-F1..F4, R9-F3 (R10-F5 comment). Explicit player attacks, ranged behaviour, turn-based authority (native eligibility is checked in real time only) and ordinary advance toward reachable enemies are unchanged. Evidence kinds: offline attack-path harness on production members, compiled-wiring checks, component tests. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 50.8 s | Receipt gate-focused-55869192b442-d43d449b6c4-af2513083e1-20261126T224454606684Z; Fast tier PASS=4 FAIL=0; components 1542/0; attack-path harness 185/0; lifecycle contract 80/0; orders native contract 4/0; presentation 239/0; Shield 114/0; Archery 54/0; Ranger/Riding Dog, replay subscriber, localization, UMM 0.31.1/0.33.0 loaders, frozen product source. nativeProof=false, offline only. Also metered: two local builds (the first stopped at an unqualified type name), one direct harness run, one local focused run. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26d.md
- src/KingmakerMountedCombat/Integration/MountedCombatController.cs
- src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs
- src/KingmakerMountedCombat/Integration/NativeBrainRankingReader.cs
- src/KingmakerMountedCombat/Domain/MountedAutomaticAttackPolicy.cs
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs
- scripts/Test-MountedAlpha2LifecycleContract.ps1
- docs/missions/mmr-split/PR-1-ledger-excerpt.md
- controller gate receipt gate-focused-55869192b442-d43d449b6c4-af2513083e1-20261126T224454606684Z (pass)

**Prior findings to assess:** `R10-F1`, `R10-F2`, `R10-F3`, `R10-F4`, `R10-F5`, `R9-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
