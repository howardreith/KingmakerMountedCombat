<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r15-request part=1/1 sha256=70ac72096375e9f31f1c2193acfca4a77e09e46a656bc567db86d70348ade9e6 -->
### Claude Opus — review request — round 15 — candidate `83c2aaada8`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r15-5ea03a` |
| Checkpoint | `final` (final) |
| Candidate H | `83c2aaada817ee2880143fa2b6a4f1a07c372b34` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 15 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 14 fixes all five findings in commit 83c2aaad. The guarded push succeeded, the controller focused gate passed on that commit (56.4 s, worktree unchanged) and the worktree is clean.

- **R14-F1:** when native UI empties the player's canonical shortcut, KMC now counts that as the rider's one-time placement. This applies to the live records and to a fenced load's outgoing records (new MountedControlLoadFence.ObserveOutgoingCleanupOutcome). If the chosen slot is filled before the control returns, autofill adds no copy elsewhere.
- **R14-F2:** in one native cleanup pass that empties the canonical slot and then an older alias, the canonical index is kept. ObserveNativeAliasInvalidated never replaces an arrangement already interrupted at another index.
- **R14-F3:** when a save finds the remembered slot occupied, the continuing game now treats the shortcut as off the bar too (RememberedSlotTaken, GiveUpOccupiedSlot), matching the saved archive. Clearing the occupant later does not bring it back, either in the continuing game or after a reload.
- **R14-F4:** MountedControlLoadFence.Begin now takes the controls' active state. A disabled service sets up no load fence and keeps no reference to the outgoing player world. The IsBad observer and the CleanupBadSlots hook record nothing while disabled.
- **R14-F5:** a native rebind seen while a load is in progress is recorded as live in the load scope's saved records. If the load is abandoned, the shortcut comes back live, so a later removal stays removed; if the load completes, the record is dropped.

There are five new component regressions; the test double gained an enabled flag, a mixed cleanup pass and fenced cleanup outcomes. To make failures visible I temporarily silenced PASS lines in tests/TestRunner.cs; that change was reverted and is absent from the commit. A combined mutation run that reverted these fixes failed exactly the five new regressions.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 56.4s | Result of record, on commit 83c2aaad (log gate-focused-83c2aaada817-d43d449b6c4-a030b738800-20261121T180744960315Z.log; exit 0; worktree unchanged). Covers the build, FAST components, Validate-Source and the five children of the focused wrapper. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.81 min | Development run: components 1477/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1472/5. Mutations in the domain: the interrupted-alias rule disabled, GiveUpOccupiedSlot made a no-op, the disabled-fence guard removed, and outgoing rebinds dropped. Mutation in the double: no placement record on canonical emptying, mirroring the production line. Exactly the five new regressions failed. All mutations were reverted and a grep confirmed none remained. The R14-F1 placement line is service code mirrored by the double. |

**Evidence references:**
- commit 83c2aaada817ee2880143fa2b6a4f1a07c372b34
- gate-focused-83c2aaada817-d43d449b6c4-a030b738800-20261121T180744960315Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (ObserveNativeAliasInvalidated interrupted rule, GiveUpOccupiedSlot, MountedControlLoadFence.Begin controlsActive, ObserveOutgoingCleanupOutcome)
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (RememberedSlotTaken)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (ApplySlotInvalidation placement record and fenced rebind, ObserveInvalidatedPublicSlot/BeginNativeBadSlotCleanup disabled guards, BeginSelectedLoad, CaptureAbsentIntent give-up)
- tests/MountedControlPlacementIntentTests.cs (FirstManualCleanupNoAutofill, MixedCleanupPassKeepsCanonical, OccupiedSlotGivenUpEverywhere, DisabledLifecycle, FencedRebindAbandoned)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 14 additions, Validation)

**Prior findings to assess:** `R14-F1`, `R14-F2`, `R14-F3`, `R14-F4`, `R14-F5`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
