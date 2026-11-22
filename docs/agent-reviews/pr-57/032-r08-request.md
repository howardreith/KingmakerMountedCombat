<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r08-request part=1/1 sha256=7bf94e7adba9fcf2d362f7482b1cf96e77242b94a0b59880cc178ffcf1295edf -->
### Claude Opus — review request — round 8 — candidate `9cec5c5db9`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r08-92237f` |
| Checkpoint | `final` (final) |
| Candidate H | `9cec5c5db9f6f2f30f067315ff24f6cdaafce326` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 8 of 8 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 7 fixes both pending toolbar findings in commit 9cec5c5d. The guarded push succeeded, the controller focused gate passed on that commit (50.4 s, worktree unchanged), and the worktree is clean.

R4-F2 (pet change across area suspension or replay):
- A new RetireOnCompanionLoss check, in both the service and MountedControlPlacementIntent, runs at each update, at every bar observation (save header, area suspension, departures) and on the native cleanup report.
- It retires the rider's arrangement when the pet link names another companion. It also retires when the link is cleared while the rider no longer gets the control. A mounted rider whose relationship faulted still gets the control and keeps its slot.
- An empty pet link never overwrites a companion identity that is already known.
- A retired actor is remembered until a new arrangement is observed or a fresh load arrives. An in-world replay of rows captured before the retirement cannot seed or write that slot.

R7-F1 (older capital save, stale reserve binding):
- A new AdmitReserveBindings step runs at each update, at the save header and at the save scope. It looks once per unit object at reserve or detached riders that are neither candidates nor still observed.
- A surviving Mount / Dismount binding is recovered as that rider's chosen slot, with a placement record. Either way, every KMC binding on that bar is then cleared and unrelated slots are untouched. This applies only when the binding is identifiable (the public control, or an older exact Mount or Dismount binding) and the pet link names a companion.
- The save scope also leases KMC bindings on reserve and detached bars, so none is serialized. Disable and disposal clear them.

Three new component regressions cover the reviewer's requested sequences and controls. The handoff doc has a Round 7 section, updated validation and a new owner playtest step.

Native checks (capital UI, real transitions, older-save loads, attack resolution) remain NOT RUN and are deferred to the owner playtest.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 50.4s | Result of record, on commit 9cec5c5d (log gate-focused-9cec5c5db9f6-d43d449b6c4-a030b738800-20261121T161716897910Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children: Kingmaker assembly contracts, Gunslinger click composition, archive row bound, the six-pair archive adapter and the area-adapter compile comparison between base and candidate. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.78 min | Development run on the fixed, uncommitted tree. Components 1452/0, with the three new toolbar tests passing. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 3/0, six-pair adapter 22/0. Area adapter: 63 errors on base and 63 on the candidate, compared against the previous HEAD. Focused 5/5. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation reverting four new domain decisions: the cleared-link retirement, the replay block in Seed, the reserve-binding recovery and identity preservation) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool output truncation cut off the FAIL lines. The only difference from the all-pass tree was those four mutations. They were reverted before commit (a grep confirmed none remained), and the controller gate passed on the committed tree. The new tests use APIs that did not exist before this round, so they were not run against the d27728b3 code itself. Those sequences were traced in source. |

**Evidence references:**
- commit 9cec5c5db9f6f2f30f067315ff24f6cdaafce326
- gate-focused-9cec5c5db9f6-d43d449b6c4-a030b738800-20261121T161716897910Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (RetireOnCompanionLoss, KnownPetId, retired set, Seed/AllowsSavedSlot, AdmitSurvivingBinding, Retire)
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (IsPublicControlBinding)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (RetireOnCompanionLoss, ObserveArrangementOnBar, ObserveInvalidatedPublicSlot, AdmitReserveBindings, ClearReserveBindings, ReserveRosterUnits, CaptureAndClearManagedHotbarSlots, SetEnabled, Dispose)
- tests/MountedControlPlacementIntentTests.cs (SuspensionClearedLinkThenNewPet, ReplayAfterPetChangeDuringSuspension, OlderCapitalSaveReserveBinding)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 7 additions, Validation, Owner playtest step 2)

**Prior findings to assess:** `R4-F2`, `R7-F1`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
