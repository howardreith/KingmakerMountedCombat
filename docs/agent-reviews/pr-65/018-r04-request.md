<!-- agent-review:v1 enrollment=kmc-codex-mmr-attack-orders-20261125103400-6cef key=r04-request part=1/1 sha256=7c8c3c5bb2906b184e8a9e087f0fe52ef9c8d324f13bb4e49f8b53ef0988a231 -->
### Claude Opus — review request — round 4 — candidate `e9ef7f6fc1`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-attack-orders-20261125103400-6cef-r04-7ad274` |
| Checkpoint | `final` (final) |
| Candidate H | `e9ef7f6fc129ae242e89580b401bea4e7432abe8` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-1-attack-orders` sha256 `b518dd67c2` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 4 of 6 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-1 round 4 (ceiling raised to 6 by the owner on 2026-11-26 and resumed through the supported controller operation; owner text committed as docs/missions/mmr-split/owner-authorization-2026-11-26.md). One coherent lifecycle correction for the ten blockers (and R3-F5), recorded in docs/missions/mmr-split/PR-1-lifecycle-record.md with the installed native facts it relies on (UnitCommand.Tick: OnTick, return if finished, Result = OnAction() then IsActed, 9 s deadline; ForceFinish sets IsActed without OnAction; ClickUnitHandler.OnClick writes ManualTarget after Run). The order generation is kept separate from each participant's command: delivery boundary, terminal disposition, generation-bound refusal, one retirement path with retirement held on the wrapper, ownership from the actual assignment, clocks that exclude waiting, admission before movement revocation, sight goals that need movement. Evidence: the attack-path harness now splices the controller's actual lifecycle members next to the wrapper's and drives the eight cases the owner listed; the compiled glue is bound by the lifecycle contract. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-attack-orders-20261125103400-6cef --gate focused -- -Tier Fast -Focused Test-MmrAttackOrdersFocused.ps1 | yes | pass | 42.4 s | Receipt gate-focused-e9ef7f6fc129-d43d449b6c4-af2513083e1-20261126T045053495832Z; Release forwarded; 15 of 15 children, nativeProof=false. Components 1542/0; Validate-Source 153/0; orders native 4/0; lifecycle contract 66/0; attack-path harness 83/0; presentation 239/0; Mounted Shield 114/0, rollback 68/0; Archery 54/0; Ranger/Riding Dog 183/0; replay 29/0; native loader 101/0; frozen source; UMM loaders 0.31.1/0.33.0. Offline only. Before the gate: one metered local focused run and metered direct harness/contract runs while correcting compile and check errors. |

**Evidence references:**
- docs/missions/mmr-split/PR-1-lifecycle-record.md
- docs/missions/mmr-split/owner-authorization-2026-11-26.md
- docs/missions/mmr-split/PR-1-ledger-excerpt.md (operator ledger: decision, resume, round-4 check)
- tests/Chunk7Diagnostics/AttackPathHarnessTemplate.cs (Round4* cases) and scripts/Test-MountedAlpha2AttackPathHarness.ps1 (controller splices)
- scripts/Test-MountedAlpha2LifecycleContract.ps1 section 8
- controller gate receipt gate-focused-e9ef7f6fc129-d43d449b6c4-af2513083e1-20261126T045053495832Z (pass)

**Prior findings to assess:** `R1-F11`, `R1-F3`, `R1-F7`, `R2-F1`, `R2-F3`, `R2-F4`, `R3-F1`, `R3-F2`, `R3-F3`, `R3-F4`, `R3-F5`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
