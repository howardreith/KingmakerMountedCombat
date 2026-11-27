<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r01-request part=1/1 sha256=0dcc0dd61dffe849ab9b41761a6190eb5fa690609e31b699edef1d3971111132 -->
### Claude Opus — review request — round 1 — candidate `95c26e59c4`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r01-49dfc7` |
| Checkpoint | `final` (final) |
| Candidate H | `95c26e59c4657bd42f56153f817d23c0c89d82d9` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 1 of 3 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 of the owner-approved Mounted Melee Repair decomposition (MMR-03/04/05; mission docs/missions/mmr-split/PR-1-attack-orders.md). The candidate is the extraction of the attack-order and real-time melee implementation from PR #64 at 46444b30 (alpha.6), which was reviewed seven times there and NEVER ACCEPTED: the whole diff against main is submitted for review from scratch. Owned whole files are byte-identical to 46444b30; shared files (MountedPairAttackCommand, MountedCombatController, MountedPatchController) carry exactly PR-1's contributions, with the turn-based click/route pieces (PR-2) and the Horse strike line (PR-5) left out. No behavior was changed in the extraction. PR-1 carries no open historical finding; the 22 verified PR #64 findings in its area are regression obligations listed in the mission with their verifying assessments. Offline evidence only: no native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 69.1 s | Receipt gate-focused-95c26e59c465-d43d449b6c4-af2513083e1-20261125T153534161716Z; 15 of 15 children. Components 1539/0; Validate-Source 153/0; orders native contract 4/0 (CanActInCombat initiative/combat reads; brain candidates, Sort, FillWeight); lifecycle contract 49/0 (slot/queue acceptance and promotion, carrier Hold write and Hold input, pre-start hook and untouched native start gate, Character_Brain bytes and ranking reader, perception guards, retirement, ledger release); attack-path harness 16/0 on the actual spliced members (R3-F1 obstructed/unobstructed, leaving pre-start; R4-F2 in-range start and arrival with controls, successor movement); presentation 239/0; Mounted Shield native 114/0 and rollback 68/0 (icon identity); Archery 54/0; Ranger/Riding Dog 183/0; replay subscriber 29/0; localization; UMM 0.31.1 build reference; native loader; frozen source 23/0; UMM loader 0.31.1/0.33.0 25/0 each. Validation budget mounted-alpha2-melee-pose-20261124: 67.17 of 90 min after this gate (includes the reconciled import of earlier unmetered direct runs). Offline only; not native gameplay or visual evidence. |

**Evidence references:**
- docs/missions/mmr-split/PR-1-attack-orders.md (child mission: verbatim MMR-03/04/05, S04/S05/S08, regression obligations, extraction record)
- f29a45e3 (mission), 95c26e59 (extraction from 46444b30)
- PR #64 (draft historical reference) and its round reviews linked in the mission
- controller gate receipt gate-focused-95c26e59c465-d43d449b6c4-af2513083e1-20261125T153534161716Z (pass)

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
