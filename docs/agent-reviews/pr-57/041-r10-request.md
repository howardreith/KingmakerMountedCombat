<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r10-request part=1/1 sha256=2b7cf64233a30ba53dc38bee1138cc16d0f97827accfd628e8aa0055360ca775 -->
### Claude Opus — review request — round 10 — candidate `c856b4e2cf`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r10-993317` |
| Checkpoint | `final` (final) |
| Candidate H | `c856b4e2cf457351271906c71ef1400544cc940b` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 10 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 9 fixes all four toolbar findings in commit c856b4e2. The guarded push succeeded, the controller focused gate passed on that commit (45.9 s, worktree unchanged) and the worktree is clean.

R9-F1 (cleanup before registration): ResolveKind now goes through the new domain rule MountedControlSlotPolicy.ResolveExactKind. It returns None for a missing blueprint before any comparison, so an item or other non-ability slot can never match a control field that is still unregistered (null). Disable, disposal, reserve cleanup and the save scope's lease capture touch only identifiable KMC bindings.

R9-F2 (lease release after a companion change): ClearSerializationHotbarLeases restores the public control only while MountedControlPlacementIntent.AllowsLeaseRestore holds. The leased index must still be the arrangement, the companion must be the same, and the arrangement must not be retired.

R9-F3 (scan bookkeeping lifetime): reserve scan bookkeeping is now MountedScanLedger. It is synchronized with the current player and reserve roster on every update, before any early return, and at each scan. It drops units that leave the roster, and drops everything, the world included, when the world changes or none is current. It is also reset when a selected load begins, on disable and on disposal.

R9-F4 (older alias on a party rider): RecoverLegacyAlias records an older Mount or Dismount alias as the chosen slot before repair empties it. It runs in party reconciliation while the public control is not live, and in every bar observation, so a save before the first update also sees it. It uses the existing precedence and the existing companion-loss rules.

Four new component regressions cover the requested sequences and controls. The handoff doc has a Round 9 section and updated validation.

Native checks remain NOT RUN and are deferred to the owner playtest documented in docs/CITY-AND-PRONE-HANDOFF.md.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 45.9s | Result of record on commit c856b4e2 (log gate-focused-c856b4e2cf45-d43d449b6c4-a030b738800-20261121T165201228429Z.log; exit 0; worktree unchanged). Covers the build, FAST components, Validate-Source and the five children of the focused wrapper. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.83 min | Development run on the fixed tree before the mutation run. Components 1458/0, with all four new toolbar tests passing. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 3/0, six-pair adapter 22/0. The area-adapter baseline matched on 63 errors in both trees. Focused 5/5. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation reverting ResolveExactKind's null rule, AllowsLeaseRestore and the ledger's pruning and release) | no | fail | ~1 min | Intended failing reproduction: exit 1. Tool truncation hid the individual FAIL lines. The mutations were reverted before commit; a grep confirmed no mutation remained, and the controller gate passed on the committed tree. R9-F4's call sites are service code mirrored by the toolbar double, so it has no domain mutation; its pre-fix behavior was traced in source. |

**Evidence references:**
- commit c856b4e2cf457351271906c71ef1400544cc940b
- gate-focused-c856b4e2cf45-d43d449b6c4-a030b738800-20261121T165201228429Z.log
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (ResolveExactKind)
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (AllowsLeaseRestore, AdmitSurvivingBinding controlDesired, MountedScanLedger)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (ResolveKind, ClearSerializationHotbarLeases, ReleaseStaleReserveScan, AdmitReserveBindings, RecoverLegacyAlias, ReconcilePublicControlSlots, ObserveArrangementOnBar, BeginSelectedLoad)
- tests/MountedControlPlacementIntentTests.cs (UnregisteredCleanupKeepsUnrelatedSlots, LeaseReleaseAfterCompanionChange, ReserveScanLedgerLifetime, LegacyAliasWhilePetUnresolved)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 9 additions, Validation)

**Prior findings to assess:** `R9-F1`, `R9-F2`, `R9-F3`, `R9-F4`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
