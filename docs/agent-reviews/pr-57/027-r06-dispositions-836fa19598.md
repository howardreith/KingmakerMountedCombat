<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r06-dispositions-836fa19598 part=1/1 sha256=836fa195980b301b0617fc881e2435622d1c36d26525edd1798d7e62defed38e -->
### Claude Opus — finding dispositions — round 6

Candidate after corrections: `d27728b3467aba07850dbc66eaeb30f9e1eb5768`

- **R4-F2** — `fixed` — commits: `d27728b346`
  The fix is in src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs. ObserveNativeSlotInvalidated now returns bool and checks the companion identity before recording anything: if string.IsNullOrEmpty(petId) or PetLinkChanged(actorId, petId), it removes the entry and returns false. Only then does it call ObserveBound plus a Temporary absence. A cleared link therefore no longer overwrites pet A's identity with null for pet B to adopt later via Revalidate. NativeMountedControlService.ObserveInvalidatedPublicSlot counts the observation only when it was recorded. The existing direct-change RetireOnPetChange path is unchanged.

Regression: tests/MountedControlPlacementIntentTests.cs ClearedLinkCleanupThenNewPet, run with the fact reactivated and not reactivated. Pet A is in slot 9; the link clears; native cleanup reports slot 9; pet B is linked; then the first update runs. The test checks that B gets no slot, that slot 9 and unrelated slots are untouched, that the next save has no kind-6 row and one kind-7 row, and that the save scope does not restore slot 9.

Same-ID control: the same pet id, unresolved through the same cleanup, returns to slot 9 once, and the save carries a kind-6 row at 9. The existing UnresolvedPetKeepsArrangement, PermanentLossClearsSlot and DirectPetChange tests also pass.

Before the fix the run exited 1 against the stub; after it, 1449/0. The controller focused gate passed on d27728b3.
- **R5-F1** — `fixed` — commits: `d27728b346`
  New MountedControlLoadFence.ObserveOutgoingSlotInvalidated(currentWorld, actorId, slotIndex, petId) applies ObserveNativeSlotInvalidated to the scope's retained outgoing records, but only while the fence is armed and the current world is the fenced one. Abandon already restores those records exactly when the outgoing world survives, and Accepts or Release drops them when a different world replaces it. NativeMountedControlService.ObserveInvalidatedPublicSlot no longer returns early when !ArrangementsAccepted. It still requires the exact slot object on a current-roster rider's bar, then routes the observation to the fence.

Regression: CleanupDuringAbandonedLoad, run with the fact reactivated and not reactivated. Slot 9 is bound; BeginLoad(owner); native cleanup empties slot 9 while fenced; an update in the interval writes nothing; AbandonLoad(owner) runs before any replacement. The test checks for exactly one binding at 9, one kind-6 row at 9 (the only kind-6 row) on the next save, and that the slot is unchanged after the save scope.

Controls: an unobserved deliberate removal before the load stays removed after abandonment and saves a kind-7 row only. The same cleanup followed by a successful InstallAndRestore gives the loaded world no rows and no slot on party return. LoadFenceProductionOrder, AbandonedLoadRestores and SupersededLoadFence still pass.

Before the fix the run exited 1 against the stub; after it, 1449/0. The controller focused gate passed on d27728b3.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 23.7s | Result of record, on commit d27728b3 (log gate-focused-d27728b3467a-d43d449b6c4-a030b738800-20261121T155944394884Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children: Kingmaker assembly contracts, Gunslinger click composition, archive row bound, the six-pair archive adapter, and the area-adapter base/candidate compile comparison. Offline only; this is not native gameplay evidence. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (two new reproductions plus a stub keeping the old behavior; then the same command again with -SkipBuild via powershell.exe) | no | fail | ~1 min | The intended failing reproduction, run before the fix. Both runs exited 1. Tool truncation cut off the FAIL lines, and redirecting output to a file was not permitted. The only changes from the all-pass round-5 tree were the two new tests, the test double's fenced-cleanup routing and a stub that drops the fenced observation and keeps the old empty-link re-record. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.73 min | Development run on the uncommitted fixed tree: components 1449/0, with both new toolbar tests passing; Validate-Source 153/0; assembly contracts 820/0; click composition 21/0; archive bound 3/0; six-pair adapter 22/0; area adapter 63 errors on base and 63 on the candidate, compared against the previous HEAD; focused 5/5. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
