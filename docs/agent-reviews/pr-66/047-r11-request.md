<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r11-request part=1/1 sha256=b077940adf6387f310e0468711a5efb9f496e6e207a3a226b46f5a20c7f8a50c -->
### Claude Opus — review request — round 11 — candidate `68a19050fa`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r11-49ad08` |
| Checkpoint | `final` (final) |
| Candidate H | `68a19050fa61244cc724988ae8772cb5f2b2d6f1` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-4-horse-trip` sha256 `29184130a7` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 11 of 11 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-4 round 11, the last allowed round (owner instruction 2026-11-27a). R10-F1 and R9-F3 by one owned fallback attempt kept across polls (no unlink by name, no new names on retry, lookup errors kept apart from observed replacements), R5-F1 by an effective-protection check before any payload byte plus read-back and a size-checked final identity, R10-F5 by owner-only access, R10-F2 by a Linux read lease at every deciding read, R9-F4/R10-F3/R10-F4 by whole-name matching, the round-8 retiring shape and a post-move check. Verified R9-F1 and R9-F2 unchanged. Limitations stated: macOS has no writer lease; a filesystem with neither O_TMPFILE nor permission bits keeps the claim without publishing; Unix code ran only over a simulated boundary.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-trip-20261125213249-4b8b --gate focused -- -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 | yes | pass | 43.0 s | Receipt gate-focused-68a19050fa61-d43d449b6c4-a3357fdbeb8-20261127T143058518556Z; nativeProof=false. Also metered: 3 builds and 2 component runs (final 1572/0). Windows; the Unix branch over a simulated boundary. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-27a.md
- docs/missions/mmr-split/PR-4-operation-failure-table.md
- docs/missions/mmr-split/PR-4-ledger-excerpt.md
- src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs
- tests/HorseTripRepairPolicyTests.cs
- controller gate receipt gate-focused-68a19050fa61-d43d449b6c4-a3357fdbeb8-20261127T143058518556Z (pass)

**Prior findings to assess:** `R10-F1`, `R10-F2`, `R10-F3`, `R10-F4`, `R10-F5`, `R5-F1`, `R9-F3`, `R9-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
