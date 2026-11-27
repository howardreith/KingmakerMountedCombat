<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r08-request part=1/1 sha256=600dbaa07cf66e69e5a245f1d333244e2efdebe8d2b0ee35d8b2cc0cd3e8ef5b -->
### Claude Opus — review request — round 8 — candidate `3eb8503648`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r08-9e7b89` |
| Checkpoint | `final` (final) |
| Candidate H | `3eb85036481e17f87b01c577ccd81d9695fb3dd1` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-4-horse-trip` sha256 `29184130a7` |
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

PR-4 round 8, the last under owner instruction 2026-11-26e. Windows publication and retirement now go straight to kernel32 with an explicit DELETE access mask and without following links (R7-F1, R7-F2); the pathname branch for other platforms never deletes by pathname and writes the payload through the exclusively created terminal name (R4-F3, R5-F1). R6-F1, R6-F2 and all earlier verified behaviour preserved. Ledger: the round-7 result was appended late (seq 115) with an order correction (seq 116), reflected in the excerpt. Evidence kinds: component tests on real temporary folders (Windows), compiled-wiring checks. No native run; no Unity-Mono or Linux runtime run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-trip-20261125213249-4b8b --gate focused -- -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 | yes | pass | 36.9 s | Receipt gate-focused-3eb85036481e-d43d449b6c4-a3357fdbeb8-20261127T032801063214Z; 12 of 12 children, nativeProof=false. Components 1561/0; trip native contract 52/0; Shield; loaders. Windows only. Also metered: four local builds, three component runs, one failing local focused run (a PowerShell integer-literal cast in the new contract check), two direct contract runs, one Kingmaker-mscorlib trace attempt, and an earlier gate on b783166d superseded by this records-only candidate. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26e.md
- src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs
- tests/HorseTripRepairPolicyTests.cs
- scripts/Test-MmrHorseTripNativeContract.ps1
- docs/missions/mmr-split/PR-4-ledger-excerpt.md
- controller gate receipt gate-focused-3eb85036481e-d43d449b6c4-a3357fdbeb8-20261127T032801063214Z (pass)

**Prior findings to assess:** `R4-F3`, `R5-F1`, `R7-F1`, `R7-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
