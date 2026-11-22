<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r09-request part=1/1 sha256=d8cee55076b08f71f4d48d6a85efe2455f7513395637b14bde967c423b53e6ae -->
### Claude Opus — review request — round 9 — candidate `a3462521bd`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r09-2146f9` |
| Checkpoint | `final` (final) |
| Candidate H | `a3462521bd66c4f414a14a387b7b7fdebecbbe56` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 9 of 15 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 8 fixes both pending toolbar findings in commit a3462521. The guarded push succeeded, the controller focused gate passed on that commit (24.8 s, worktree unchanged) and the worktree is clean.

R8-F1 (older reserve bars): reserve recovery now uses the same precedence as ReconcilePublicControlSlots. An existing public Mount / Dismount binding wins over an older Mount or Dismount alias, and the alias is used only as a fallback. The new domain rule is MountedControlSlotPolicy.RecoveredPublicSlot. While the KMC bindings are cleared, an older Mounted Charge becomes the rider's own stock Charge in the same slot. The existing ResolveStockChargeForSlot rule applies: only when that Charge is available and not already on the bar.

R8-F2 (faulted rider loses an interrupted slot): live, unbound reconciliation (MountedControlPlacementIntent.Reconcile) no longer revalidates the arrangement away on an empty pet link. An entitled mounted or faulted rider keeps its known identity and interrupted index, and the control returns to its chosen empty slot. A different companion, or a cleared link after entitlement ends, still retires the arrangement.

Two new component regressions cover the reviewer's requested sequences and controls. The toolbar test double now models the relationship granting the control, the legacy bindings and the stock Charge. The handoff doc has a Round 8 section and updated validation.

Native checks remain NOT RUN and are deferred to the owner playtest documented in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 24.8s | Result of record, on commit a3462521 (log gate-focused-a3462521bd66-d43d449b6c4-a030b738800-20261121T163541476119Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation restoring the unconditional Revalidate in Reconcile and first-match recovery precedence) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool truncation cut off the FAIL lines. The only difference from the fixed tree was those two mutations, which were reverted before the passing run and the commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.9 min | Development run on the fixed, uncommitted tree. Components 1454/0, with both new toolbar tests passing. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 3/0, six-pair adapter 22/0. Area adapter baseline: 63 errors on both trees. Focused 5/5. Limit: the Charge migration loop in the toolbar double mirrors the service. The service's own ResolveStockChargeForSlot call on a native reserve bar is not exercised offline. |

**Evidence references:**
- commit a3462521bd66c4f414a14a387b7b7fdebecbbe56
- gate-focused-a3462521bd66-d43d449b6c4-a030b738800-20261121T163541476119Z.log
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (RecoveredPublicSlot)
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (Reconcile live unbound branch)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (AdmitReserveBindings)
- tests/MountedControlPlacementIntentTests.cs (OlderReserveBarMigration, FaultedRiderKeepsInterruptedSlot, Rider.RelationshipGrants/HasStockCharge, Service.KindOf)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 8 additions, Validation)

**Prior findings to assess:** `R8-F1`, `R8-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
