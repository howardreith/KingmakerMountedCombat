<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r13-request part=1/1 sha256=46ae3c2481b42a229ebb5e90decfeaa70eccb3f3fde1a29c503be675246eb530 -->
### Claude Opus — review request — round 13 — candidate `915f2fe738`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r13-362202` |
| Checkpoint | `final` (final) |
| Candidate H | `915f2fe7380b2439c97d5cc5bfccf5e233e3be32` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 13 of 13 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 13 (the last under owner instruction 2026-11-26d). The automatic order's one admission decision now also governs its generation after delivery at the controller's boundaries (released routine left to native completion), and the held-pair evidence runs through the production AutomaticMovementForbidden from the horse's Hold (R12-F1, R12-F2); optional R12-F3/F4 in the same pass. All round-11 and round-12 verified rows pass unchanged. Evidence kinds: offline attack-path harness on production members, compiled-wiring checks, component tests. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 50.4 s | Receipt gate-focused-915f2fe7380b-d43d449b6c4-af2513083e1-20261126T232802077475Z; Fast tier PASS=4 FAIL=0; components 1542/0; attack-path harness 204/0; lifecycle contract 82/0; Shield, Archery, Ranger/Riding Dog, presentation, replay, localization and loader neighbours. nativeProof=false. Also metered: one direct harness run, one local focused run. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26d.md
- src/KingmakerMountedCombat/Integration/MountedCombatController.cs
- src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs
- scripts/Test-MountedAlpha2AttackPathHarness.ps1
- scripts/Test-MountedAlpha2LifecycleContract.ps1
- docs/missions/mmr-split/PR-1-ledger-excerpt.md
- controller gate receipt gate-focused-915f2fe7380b-d43d449b6c4-af2513083e1-20261126T232802077475Z (pass)

**Prior findings to assess:** `R12-F1`, `R12-F2`, `R12-F3`, `R12-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
