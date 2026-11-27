<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r08-request part=1/1 sha256=3d43ac01772aa83b4b987e0c877997bc55222072cf14be15e2dc67b6ee5d816c -->
### Claude Opus — review request — round 8 — candidate `b157556882`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r08-4551bb` |
| Checkpoint | `final` (final) |
| Candidate H | `b1575568827096453af5697f7f94b9f3d9ad814d` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 8 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 8 (the last under docs/missions/mmr-split/owner-authorization-2026-11-26b.md). Four source defects corrected (R1-F11 call-site assignment observation through an OnClick transpiler hook, R6-F1 invalidation precedence, R7-F1 tie-break seeding, R7-F2 native sight with stationary admission) and two evidence gaps closed in the existing harness (R4-F1 single-attack mode and the production plan check; R4-F5 native CanAct through the production terminal hooks, production participant evaluation, full windows). R7-F3 (optional) is documented. Evidence kinds: installed IL facts, compiled-wiring checks, a component test, simulated engine (harness). No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 37.9 s | Receipt gate-focused-b15755688270-d43d449b6c4-af2513083e1-20261126T145618629472Z; 15 of 15 children, nativeProof=false. Components 1542/0 (with R7-F1 cases); Validate-Source; orders native contract 4/0; lifecycle contract 78/0; attack-path harness 159/0; Shield, rollback 68/0; Archery; Ranger/Riding Dog 183/0; replay; localization; UMM loaders. Offline only. Also metered: three direct harness runs and four local focused runs (three stopped at the build while the new namespace and Harmony API were corrected). |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26b.md
- docs/missions/mmr-split/PR-1-round7-working-record.md
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs
- tests/MountedAlpha2LifecycleTests.cs
- scripts/Test-MountedAlpha2LifecycleContract.ps1
- docs/missions/mmr-split/PR-1-ledger-excerpt.md
- controller gate receipt gate-focused-b15755688270-d43d449b6c4-af2513083e1-20261126T145618629472Z (pass)

**Prior findings to assess:** `R1-F11`, `R4-F1`, `R4-F5`, `R6-F1`, `R7-F1`, `R7-F2`, `R7-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
