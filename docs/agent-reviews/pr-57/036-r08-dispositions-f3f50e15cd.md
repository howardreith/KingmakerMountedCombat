<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r08-dispositions-f3f50e15cd part=1/1 sha256=f3f50e15cd1d1691ca64350bb335d8ef68e51cf0b17f89a84080e4d1da4e0457 -->
### Claude Opus — finding dispositions — round 8

Candidate after corrections: `a3462521bd66c4f414a14a387b7b7fdebecbbe56`

- **R8-F1** — `fixed` — commits: `a3462521bd`
  The domain gains MountedControlSlotPolicy.RecoveredPublicSlot(kinds). It returns the first public-control (MountState) index; only if there is none does it return the first index that RepairLiveSlot maps to MountState (an older Mount or Dismount alias). Otherwise it returns -1.

NativeMountedControlService.AdmitReserveBindings now recovers from that index. It then walks every KMC slot: a kind that RepairLiveSlot maps to StockCharge (the older Mounted Charge) is replaced with ResolveStockChargeForSlot(unit, slots), which is the unit's own active exact Charge unless it is already on the bar. Every other KMC binding becomes empty, and non-KMC slots are untouched.

Regression OlderReserveBarMigration (tests/MountedControlPlacementIntentTests.cs):
- Setup: an unobserved reserve rider with legacy Mount at 3, legacy Mounted Charge at 5, the public control at 9, stock Charge available and no rows.
- An immediate save writes exactly one kind-6 row, at 9, and one kind-7 row. SerializedKmcSlots is 0. Stock Charge is at 5 exactly once, slot 3 is cleared and Attack at 0 is unchanged.
- After reload, a party return gives one public control at 9 and Charge once at 5.
- Repeated with stock Charge already at 7: slot 5 is cleared and Charge stays at 7 once, through the reload too.
- Controls: an alias-only bar with Dismount at 4 recovers 4. A bar with no stock Charge only clears the legacy Charge.

The mutation restoring first-match precedence exited 1. The fixed tree passed 1454/0, and the controller gate passed on a3462521.

Limit: the double mirrors the service's Charge loop; the native reserve bar is NOT RUN.
- **R8-F2** — `fixed` — commits: `a3462521bd`
  In MountedControlPlacementIntent.Reconcile's live, unbound branch, Revalidate now runs only for a nonempty pet link: `if (!string.IsNullOrEmpty(petId)) Revalidate(...)`. Reaching that branch means the control is live, so the rider is still entitled. An empty link therefore keeps the known PetId (ObserveBound and ObserveOffBar already preserve it) and the interrupted index, and ResolveUnbound returns it. A different nonempty link still retires the arrangement via Revalidate, and RetireOnCompanionLoss still retires on a cleared link once ShouldLease is false.

The toolbar double now has RelationshipGrants, so Desired can be true with an empty link, as with ShouldLease in production.

Regression FaultedRiderKeepsInterruptedSlot, run with the fact reactivated and not reactivated:
- Slot 9 is recorded for pet A. The link is cleared while the relationship still grants the control, native cleanup invalidates slot 9, and the live control is reconciled.
- Exactly one binding is at 9. The save carries exactly one kind-6 row, at 9, and the save scope leaves slot 9 unchanged.
- When entitlement ends with the link still cleared, the arrangement retires and the slot is cleared.
- Different-pet control: an entitled rider whose link becomes horse-2 does not keep slot 9.

The existing dismounted cleared-link tests (ClearedLinkCleanupThenNewPet, SuspensionClearedLinkThenNewPet, PermanentLossClearsSlot, UnresolvedPetKeepsArrangement) and the different-pet tests (DirectPetChange, ReplayAfterPetChangeDuringSuspension) still pass.

The mutation restoring the unconditional Revalidate exited 1. The fixed tree passed 1454/0, and the controller gate passed on a3462521.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 24.8s | Result of record, on commit a3462521 (log gate-focused-a3462521bd66-d43d449b6c4-a030b738800-20261121T163541476119Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation restoring the unconditional Revalidate in Reconcile and first-match recovery precedence) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool truncation cut off the FAIL lines. The only difference from the fixed tree was those two mutations, which were reverted before the passing run and the commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.9 min | Development run on the fixed, uncommitted tree. Components 1454/0, with both new toolbar tests passing. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 3/0, six-pair adapter 22/0. Area adapter baseline: 63 errors on both trees. Focused 5/5. Limit: the Charge migration loop in the toolbar double mirrors the service. The service's own ResolveStockChargeForSlot call on a native reserve bar is not exercised offline. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
