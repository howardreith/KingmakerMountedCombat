<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r07-request part=1/1 sha256=1daea2cd6b8ea12544db5b706f06284c4663857bd0966549fa43ed52da4e4392 -->
### Claude Opus — review request — round 7 — candidate `d27728b346`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r07-5b5f71` |
| Checkpoint | `final` (final) |
| Candidate H | `d27728b3467aba07850dbc66eaeb30f9e1eb5768` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 7 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 6 fixes both reopened toolbar findings in commit d27728b3.

R4-F2: MountedControlPlacementIntent.ObserveNativeSlotInvalidated now checks the companion identity before it records anything. If the pet link is cleared, or names a different companion, it retires the arrangement instead of re-recording the slot with an empty link that a later pet could adopt. The service routes the native IsBad report through that check.

R5-F1: while a selected load fences the outgoing world, ObserveInvalidatedPublicSlot sends the native cleanup to the load scope's outgoing records through the new MountedControlLoadFence.ObserveOutgoingSlotInvalidated. The current world must be the fenced one. AbandonLoadedWorld restores those records, cleanup included, so the slot returns once in the surviving world. A successful world replacement drops them along with the rest of the outgoing records.

Two new component regressions cover the reviewer's exact sequences, with the requested controls. The handoff doc gained a 'Round 6 additions' section and a validation note. The guarded push succeeded, the focused gate passed on d27728b3 (23.7 s, worktree unchanged), and the worktree is clean.

Native capital UI, toolbar transitions and attack resolution remain NOT RUN. They are deferred to the owner playtest documented in docs/CITY-AND-PRONE-HANDOFF.md.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 23.7s | Result of record, on commit d27728b3 (log gate-focused-d27728b3467a-d43d449b6c4-a030b738800-20261121T155944394884Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children: Kingmaker assembly contracts, Gunslinger click composition, archive row bound, the six-pair archive adapter, and the area-adapter base/candidate compile comparison. Offline only; this is not native gameplay evidence. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (two new reproductions plus a stub keeping the old behavior; then the same command again with -SkipBuild via powershell.exe) | no | fail | ~1 min | The intended failing reproduction, run before the fix. Both runs exited 1. Tool truncation cut off the FAIL lines, and redirecting output to a file was not permitted. The only changes from the all-pass round-5 tree were the two new tests, the test double's fenced-cleanup routing and a stub that drops the fenced observation and keeps the old empty-link re-record. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.73 min | Development run on the uncommitted fixed tree: components 1449/0, with both new toolbar tests passing; Validate-Source 153/0; assembly contracts 820/0; click composition 21/0; archive bound 3/0; six-pair adapter 22/0; area adapter 63 errors on base and 63 on the candidate, compared against the previous HEAD; focused 5/5. |

**Evidence references:**
- commit d27728b3467aba07850dbc66eaeb30f9e1eb5768
- gate-focused-d27728b3467a-d43d449b6c4-a030b738800-20261121T155944394884Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (ObserveNativeSlotInvalidated, MountedControlLoadFence.ObserveOutgoingSlotInvalidated)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (ObserveInvalidatedPublicSlot)
- tests/MountedControlPlacementIntentTests.cs (ClearedLinkCleanupThenNewPet, CleanupDuringAbandonedLoad)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 6 additions, Validation)

**Prior findings to assess:** `R4-F2`, `R5-F1`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
