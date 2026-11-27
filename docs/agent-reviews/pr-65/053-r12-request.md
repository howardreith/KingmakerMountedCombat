<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r12-request part=1/1 sha256=83745bf69425a3e5ced6f729feea06fcce46d85421d494e4dcc6d4617eed5611 -->
### Claude Opus — review request — round 12 — candidate `29816172ad`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r12-1580b1` |
| Checkpoint | `final` (final) |
| Candidate H | `29816172ad6f416f3590e5a8532ceea8edc66123` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 12 of 13 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 12 under docs/missions/mmr-split/owner-authorization-2026-11-26d.md (total cap 13). One pass completing the round-11 invariant: the unreleased automatic order's one typed admission decision now also carries its own retirement boundaries and treats unavailable group memory as perception loss, so every observer applies the same rules (R11-F1..F3; optional R11-F4 in the same pass). Round-11 verified behavior is preserved (harness rows unchanged). Evidence kinds: offline attack-path harness on production members, compiled-wiring checks, component tests. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 44.4 s | Receipt gate-focused-29816172ad6f-d43d449b6c4-af2513083e1-20261126T231005564646Z; Fast tier PASS=4 FAIL=0; components 1542/0; attack-path harness 200/0; lifecycle contract 82/0; Shield, Archery, Ranger/Riding Dog, presentation, replay, localization and loader neighbours. nativeProof=false. Also metered: one direct harness run and two local focused runs (the first stopped at a compiled-wiring count that now expects two RetireOrderWrapper calls in the decision, one per retirement kind). |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26d.md
- src/KingmakerMountedCombat/Integration/MountedCombatController.cs
- src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs
- src/KingmakerMountedCombat/Domain/MountedAutomaticAttackPolicy.cs
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs
- scripts/Test-MountedAlpha2LifecycleContract.ps1
- docs/missions/mmr-split/PR-1-ledger-excerpt.md
- controller gate receipt gate-focused-29816172ad6f-d43d449b6c4-af2513083e1-20261126T231005564646Z (pass)

**Prior findings to assess:** `R11-F1`, `R11-F2`, `R11-F3`, `R11-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
