<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r10-request part=1/1 sha256=d27aa58ec1adbbc4e60d1f70161cf929a52f7d45b9e4facd5eff6ef5a5d03fa7 -->
### Claude Opus — review request — round 10 — candidate `9f2d8c0978`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r10-008453` |
| Checkpoint | `final` (final) |
| Candidate H | `9f2d8c0978c415a35c19f5ede3e4b0cf2997208f` |
| Target B (`main`) | `11e8e22b32439adf57ab16b9a42791244f0eadc0` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-4-horse-trip` sha256 `29184130a7` |
| Workflow | `6d613defe7` |
| Owner guidance | none |
| Owner amendments | none |
| Developer observed | foreground session `c7869a80-8e15-40a3-879d-92360cb9ff3f`: models ['claude-opus-5-5'], effort ['high'], permission modes ['auto'] (CLI-recorded transcript; the headless permission profile does not apply to a foreground session) |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `max`, read-only sandbox |
| Round budget | 10 of 11 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

PR-4 round 10 (owner instruction 2026-11-27a section 7; supported resume from the no-progress stop at 9 to a completed-round cap of 11). R5-F1 and R9-F3 are corrected together by a descriptor-bound Unix publication with no staging name (unnamed-file link on Linux; an independent permission-0 exclusive create elsewhere or after a failed descriptor link), R9-F2 by a single no-follow descriptor read, R9-F1 by writer-excluding Windows reads, R9-F4 by shape-exact recovery. Exact horse/fact selection, frozen decisions, verified Windows retirement, truthful recovery reporting and new-horse behaviour are unchanged; existing-save repair stays available on Windows, Linux and macOS. Evidence labels: Windows handle branch (native Windows), Unix branch over a simulated POSIX boundary on Windows, Linux syscall semantics observed with Perl on Debian 13 (WSL2 kernel); no Unity Mono or macOS execution.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-trip-20261125213249-4b8b --gate focused -- -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 | yes | pass | 62.5 s | Receipt gate-focused-9f2d8c0978c4-d43d449b6c4-a3357fdbeb8-20261127T140632243027Z; nativeProof=false. Also metered: the same gate at source commit fb5d6b01 (pass, 72.4 s), 3 builds, 2 component runs (final 1568/0), and the Linux syscall probe (3 short container runs). Windows; the Unix branch over a simulated boundary. |

**Evidence references:**
- docs/missions/mmr-split/owner-authorization-2026-11-27a.md
- docs/missions/mmr-split/PR-4-operation-failure-table.md
- docs/missions/mmr-split/PR-4-ledger-excerpt.md
- src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs (UnixFiles, LibcPosixFiles, VerifiedFileOperations.ReadEligible, HorseTripRepairFiles.RecoverRetiring/Commit)
- tests/HorseTripRepairPolicyTests.cs (SimulatedPosix and the round-10 cases)
- docs/missions/mmr-split/PR-4-r10-linux-syscall-probe-output.txt and PR-4-r10-linux-syscall-probe.pl.txt (the probe script and its output; also operator records decomposition/pr4/r10)
- controller gate receipt gate-focused-9f2d8c0978c4-d43d449b6c4-a3357fdbeb8-20261127T140632243027Z (pass)

**Prior findings to assess:** `R5-F1`, `R9-F1`, `R9-F2`, `R9-F3`, `R9-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
