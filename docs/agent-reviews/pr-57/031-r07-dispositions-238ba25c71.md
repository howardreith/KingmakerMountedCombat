<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r07-dispositions-238ba25c71 part=1/1 sha256=238ba25c71e5529a51e0965e7eebcdbe896ba8785737460f85d820e3dbd3cdf6 -->
### Claude Opus — finding dispositions — round 7

Candidate after corrections: `9cec5c5db9f6f2f30f067315ff24f6cdaafce326`

- **R4-F2** — `fixed` — commits: `9cec5c5db9`
  Both routes in the finding are closed.

Route 1, a cleared link at area suspension:
- ObserveArrangementOnBar now calls RetireOnCompanionLoss first and returns if it retired. The service decides on the pet link, ShouldLease(MountState) and whether the bar holds the control; the domain method MountedControlPlacementIntent.RetireOnCompanionLoss makes the same decision. A cleared link while the rider no longer gets the control retires the arrangement and clears the rider's public-control slots before ObserveAtBoundary can record a null identity.
- ObserveBound and ObserveOffBar keep the known PetId when the new link is empty (KnownPetId).

Route 2, a replay of rows captured before the change:
- Retirement (RetireOnCompanionLoss, Revalidate removal, permanent absence) now records the actor as retired for this world.
- Seed(loadedWorld: false) and AllowsSavedSlot refuse a retired actor, so an older transfer row neither seeds nor writes the slot.
- The retired mark clears when a new arrangement is observed or a load seeds the actor. Clear, Copy and ReplaceWith carry it, so load fences and abandonment stay exact.

The native cleanup path and the load-fence path pass controlDesired to the same rule, so a faulted mounted rider keeps its slot.

Regression SuspensionClearedLinkThenNewPet (tests/MountedControlPlacementIntentTests.cs): one pair is mounted at slot 5, a dismounted rider has slot 9, the link clears, then area suspension runs, then pet B is linked before resume. It runs with B resolving at resume and resolving later. Slot 9 stays empty, the next save has no kind-6 row for the rider, and the mounted slot 5 is unchanged.

Regression ReplayAfterPetChangeDuringSuspension: the transfer capture holds A's row at 9; then suspension; then pet B is linked unresolved; then resume and replay, run with an update before the replay and with the replay first; then B resolves. Slot 9 stays empty and the save has no kind-6 row.

Same-id controls in both tests restore slot 9 once and save one kind-6 row at 9. The existing UnresolvedPetKeepsArrangement, PermanentLossClearsSlot, DirectPetChange and ClearedLinkCleanupThenNewPet tests still pass.

The mutation run reverting these decisions exited 1. The fixed tree passed 1452/0 locally and in the controller gate on 9cec5c5d.
- **R7-F1** — `fixed` — commits: `9cec5c5db9`
  NativeMountedControlService.AdmitReserveBindings runs at the start of ReconcileFacts, at the start of ClassifyArrangementsForCapture (the archive header) and in BeginSaveSerializationScope. It requires the service to be enabled and the arrangements accepted, and runs once per unit object per world.
- It visits ReserveRosterUnits: current-world RemoteCompanions and DetachedPartyCharacters on the player's roster that are not candidates. Still-observed former candidates are skipped and left to departure classification.
- For the first slot that MountedControlSlotPolicy.IsPublicControlBinding accepts, it calls the domain AdmitSurvivingBinding. That records an interrupted arrangement at that index with the pet id, plus the placement record, when the link names a companion. A cleared or different link retires instead, and an existing or retired arrangement wins.
- It then clears every KMC binding on that bar with ClearManagedHotbarSlots; unrelated slots are kept.

CaptureAbsentIntent then writes the recovered kind-6 row and the kind-7 record. CaptureAndClearManagedHotbarSlots also leases KMC bindings on reserve and detached bars. SetEnabled(false) and Dispose call ClearReserveBindings.

Regression OlderCapitalSaveReserveBinding:
- Setup: a fresh world, a reserve owner with its pet link, a stale binding at 9 and Attack at 0, no rows and no observation.
- An immediate save writes exactly one kind-6 row at 9 and one kind-7 row (two rows in total). No KMC binding is on any bar during serialization (SerializedKmcSlots == 0). The binding is gone and slot 0 is intact.
- A cold load of that save, then a party return, restores slot 9 once. A first update, then an in-world return, also restores slot 9 once.
- Disable clears the binding and keeps slot 0.
- A no-companion control recovers nothing, writes no rows and serializes no binding.

The mutation run exited 1. The fixed tree passed 1452/0 locally and in the controller gate on 9cec5c5d.

Limit: the native older-save load is NOT RUN. A playtest step was added to docs/CITY-AND-PRONE-HANDOFF.md.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 50.4s | Result of record, on commit 9cec5c5d (log gate-focused-9cec5c5db9f6-d43d449b6c4-a030b738800-20261121T161716897910Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children: Kingmaker assembly contracts, Gunslinger click composition, archive row bound, the six-pair archive adapter and the area-adapter compile comparison between base and candidate. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.78 min | Development run on the fixed, uncommitted tree. Components 1452/0, with the three new toolbar tests passing. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 3/0, six-pair adapter 22/0. Area adapter: 63 errors on base and 63 on the candidate, compared against the previous HEAD. Focused 5/5. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation reverting four new domain decisions: the cleared-link retirement, the replay block in Seed, the reserve-binding recovery and identity preservation) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool output truncation cut off the FAIL lines. The only difference from the all-pass tree was those four mutations. They were reverted before commit (a grep confirmed none remained), and the controller gate passed on the committed tree. The new tests use APIs that did not exist before this round, so they were not run against the d27728b3 code itself. Those sequences were traced in source. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
