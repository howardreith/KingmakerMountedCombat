<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-seat-20261126011011-d3c5 key=r01-request part=1/1 sha256=3463388b359d57a7423950f1b1a7a7d39674d792769b41230e865cdbaadac38b -->
### Claude Opus — review request — round 1 — candidate `bbb0eeb6bc`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mmr-horse-seat-20261126011011-d3c5-r01-bc619d` |
| Checkpoint | `final` (final) |
| Candidate H | `bbb0eeb6bcec63808753a071876764034a7baa41` |
| Target B (`main`) | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Merge base M | `783b2b3dea3303e10cc4379dfa0a36ceac66a76b` |
| Mission | `pr-3-horse-seat` sha256 `b9a45f4ade` |
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

PR-3 of the owner-approved Mounted Melee Repair decomposition (MMR-01, Horse seat; mission docs/missions/mmr-split/PR-3-horse-seat.md). The whole diff against main is submitted for review from scratch. f67c3b16 reproduces PR #64's MMR-01 code at 46444b30 (never accepted there). c930f85c corrects the three PR #64 findings that were still open in this area, as the mission requires: R5-F4 (culled frames exhausted the seat's recovery time): SettledSeatSampler.Observe now takes the frame's evaluated animation advance and rest eligibility; a frame whose animation did not advance (the adapter's SeatAnimationAdvanceSeconds: Animator AlwaysAnimate, PlayableGraph valid and playing, view not disabled; installed UnitEntityView.OnDataAttached sets cullingMode 0 and AnimationManager.OnEnable plays the graph) consumes no settling time and there is no give-up, so a culled or paused horse still settles once frames advance. R7-F2 (successful tests required synthetic pelvis motion): a freshly evaluated, completely stationary idle is a positive case; the sampler confirms from a window of evaluated rest frames without requiring motion. R7-F3 (later action poses could overwrite a settled seat): a window confirms its mean once and the confirmed reference is never overwritten; frames that are not rest-eligible (the adapter's SeatRestEligible: installed UnitAnimationManager.CanRunIdleAction, Speed <= 0 with only locomotion/additive/released actions, and no blend-out) are counted (status field seatIneligibleFrames) and never sampled. An Unsettled notice is logged while the seat waits. The seven verified PR #64 findings in this area are regression obligations in the mission. Pre-review commit 0ba4962c: the focused wrapper forwards the tier configuration; nativeProof=false markers; the owner instruction of 2026-11-26 recorded verbatim (it set the shared validation allowance to 150 minutes and the program to 18 child / 25 lifetime reviews; PR-3 keeps 3 rounds; the hash-pinned mission states the earlier limits by the owner's direction). Scope boundary as the mission states: this PR delivers the seat computation and its offline evidence; the visible seated pose at a normal play camera remains an owner observation. No native run.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mmr-horse-seat-20261126011011-d3c5 --gate focused -- -Tier Fast -Focused Test-MmrHorseSeatFocused.ps1 | yes | pass | 34.7 s | Receipt gate-focused-bbb0eeb6bcec-d43d449b6c4-a6c9412a46f-20261126T061120509695Z; Release forwarded; 14 of 14 children, nativeProof=false. Components 1526/0 (settled-seat sampler cases incl. R5-F4 culled frames, R7-F2 stationary idle, R7-F3 later action poses); Validate-Source 153/0; seat native contract 19/0 (installed AlwaysAnimate/PlayableGraph/CanRunIdleAction/speed chain); seat harness 16/0; Mounted Shield 114/0, rollback 68/0; Archery 54/0; Ranger/Riding Dog 183/0; replay; localization; UMM loaders. Offline only; one local metered focused run preceded the gate. |

**Evidence references:**
- docs/missions/mmr-split/PR-3-horse-seat.md
- docs/missions/mmr-split/owner-authorization-2026-11-26.md
- docs/missions/mmr-split/PR-3-ledger-excerpt.md (operator ledger: owner decision, enrollment, round-1 check)
- f67c3b16 (extraction), c930f85c (R5-F4, R7-F2, R7-F3), bec00314 (mission), 0ba4962c (pre-review), bbb0eeb6 (ledger excerpt)
- controller gate receipt gate-focused-bbb0eeb6bcec-d43d449b6c4-a6c9412a46f-20261126T061120509695Z (pass)

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
