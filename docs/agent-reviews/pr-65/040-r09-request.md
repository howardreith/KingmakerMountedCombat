<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r09-request part=1/1 sha256=69fbb12ff3bbb8cb047d1eecd424e71ecbc87ade2750f051cb48d601dfa90e74 -->
### Claude Opus — review request — round 9 — candidate `f2f2fa554a`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r09-68463d` |
| Checkpoint | `final` (final) |
| Candidate H | `f2f2fa554a501d5fb1c2e276a9bd988df697fc58` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 9 of 10 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 9 under docs/missions/mmr-split/owner-authorization-2026-11-26c.md (total cap 10, resumed with resume --max-rounds 10). Two source defects corrected after tracing their producer, decision and effect paths: R8-F1 (native IsTargetValid range and sight for the proposal's action, read through the ranking reader) and R8-F2 (target invalidation before geometry-only retirement, shared by the drive and queued promotion); optional R8-F3 included. Evidence kinds: installed IL facts, compiled-wiring checks, simulated engine (harness). No native run; gameplay remains for the owner's playtest.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 48.7 s | Receipt gate-focused-f2f2fa554a50-d43d449b6c4-af2513083e1-20261126T162727247950Z; 15 of 15 children, nativeProof=false. Components 1542/0; orders native contract; lifecycle contract 79/0; attack-path harness 166/0; Shield, Archery, Ranger/Riding Dog, replay, localization, UMM loaders. Offline only. Also metered: six installed-IL trace runs, two direct harness runs (one stopped at a duplicate splice key), one local focused run. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26c.md
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs
- scripts/Test-MountedAlpha2LifecycleContract.ps1
- src/KingmakerMountedCombat/Integration/NativeBrainRankingReader.cs
- docs/missions/mmr-split/PR-1-ledger-excerpt.md
- controller gate receipt gate-focused-f2f2fa554a50-d43d449b6c4-af2513083e1-20261126T162727247950Z (pass)

**Prior findings to assess:** `R8-F1`, `R8-F2`, `R8-F3`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
