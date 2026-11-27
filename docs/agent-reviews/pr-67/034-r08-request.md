<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r08-request part=1/1 sha256=9fa636bf3c6a9e5d5f0ecdc5e5d19723e97b4b2144ae2bbaadc7e174aaa8e115 -->
### Claude Opus — review request — round 8 — candidate `dda42b9c0c`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-bite-20261125221703-5f10-r08-e00afb` |
| Checkpoint | `final` (final) |
| Candidate H | `dda42b9c0c3119f63a9f912ac760ad5034e7cfe1` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-5-horse-bite` sha256 `27e2f45366` |
| Workflow | `52a6f95ca3` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 7 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-5 round 7 under docs/missions/mmr-split/owner-authorization-2026-11-26d.md (total cap 8, resumed with resume --max-rounds 8). Invariant: a command's Bite evidence is judged from its own initial binding (validated raw counters, exactly one supply or one adoption per command, RT by cumulative difference), then the source observed at the strike and the latest registration are interpreted by that binding; the positive is one connected chain from production binding through observation, terminal projection and the writers into the existing complete reader (R6-F1, R6-F2, R4-F2). Schema 8, historical verdicts, native timing, single OnAction delegation, hoof controls and the verified registration/selector/configuration fixes are unchanged. No native run; visible Bite playback remains N08.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-bite-20261125221703-5f10 --gate focused -- -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 | yes | pass | 40.6 s | Receipt gate-focused-dda42b9c0c31-d43d449b6c4-aa63aedd141-20261126T235452350945Z; 15 of 15 children, nativeProof=false. Components 1548/0; bite contract 52/0; harness selection fixture 8/0; presentation, Shield, Archery, Ranger/Riding Dog, replay, localization, UMM loaders. Offline only. Also metered: one local build, three direct bite-contract runs (the first two stopped at a compiled-order check now pointed at the metadata boundary, and at the Unity null precondition before the connected actions were made live), one Test-Harness.ps1 -Only run, one local focused run. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-26d.md
- scripts/runtime/RuntimeHarness.Common.ps1
- scripts/Test-MountedAlpha2BiteContract.ps1
- scripts/HorseNativeControlsUxFixture.ps1
- scripts/Test-Harness.ps1
- src/KingmakerMountedCombat/Integration/HorseAnimationActionMetadata.cs
- docs/missions/mmr-split/evidence/PR-5-r7-harness-only-complete-reader.log
- docs/missions/mmr-split/PR-5-ledger-excerpt.md
- controller gate receipt gate-focused-dda42b9c0c31-d43d449b6c4-aa63aedd141-20261126T235452350945Z (pass)

**Prior findings to assess:** `R4-F2`, `R6-F1`, `R6-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
