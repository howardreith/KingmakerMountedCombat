<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r03-request part=1/1 sha256=d357bf06bd12e936e6b79d7022c76d9eee094677e7288c1b480b37aa53a88986 -->
### Claude Opus — review request — round 3 — candidate `642cb4818c`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r03-4fc996` |
| Checkpoint | `final` (final) |
| Candidate H | `642cb4818c567c14e05af3befa486b90e28afec0` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-3-horse-seat` sha256 `b9a45f4ade` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 3 of 3 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-3 round 3 (of 3). R1-F1: the rest predicate is narrowed to the native resting contributor established from the installed contract (the locomotion handle created by TickInternal, its own current composition added by the locomotion action's OnStart, weight ramped over TransitionIn), with five new installed-contract checks and a harness trace through each stage before it contributes; a missing Action no longer counts. R2-F1 and R2-F2: label/header accuracy and exact printed-vector comparisons. Visible seating remains an owner observation. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-seat-20261126011011-d3c5 --gate focused -- -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 | yes | pass | 34.4 s | Receipt gate-focused-642cb4818c56-d43d449b6c4-a6c9412a46f-20261126T070317076959Z; Release forwarded; 14 of 14 children, nativeProof=false. Components 1526/0; Validate-Source 153/0; seat native contract 24/0; seat harness 19/0; Mounted Shield, rollback 68/0; Archery; Ranger/Riding Dog 183/0; replay; localization; UMM loaders. Offline only. Before the gate: metered local focused runs while correcting check errors (contract lookups, fixture order). |

**Evidence references:**
- scripts/Test-MmrHorseSeatNativeContract.ps1 (PR-3 R1-F1 checks)
- tests/Chunk7Diagnostics/HorseSeatAttachmentTemplate.cs (BaseStage, PR-3 R1-F1 case)
- docs/missions/mmr-split/PR-3-ledger-excerpt.md
- controller gate receipt gate-focused-642cb4818c56-d43d449b6c4-a6c9412a46f-20261126T070317076959Z (pass)

**Prior findings to assess:** `R1-F1`, `R2-F1`, `R2-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
