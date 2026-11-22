<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r15-dispositions-1044f14b55 part=1/1 sha256=1044f14b557758a0e973263b8eea0c2e3f9b7eefcc5c8d1a2b265ed905070b39 -->
### Claude Opus — finding dispositions — round 15

Candidate after corrections: `b37d4fb2efd62c24b1ce982e4509ce4bc9f76b11`

- **R14-F2** — `fixed` — commits: `b37d4fb2ef`
  ObserveArrangementOnBar now routes a visible public binding that is an older alias (not the canonical blueprint) through the new MountedControlPlacementIntent.ObserveAliasAtBoundary. That method does nothing when the entry already holds an interrupted choice at another index (HoldsInterruptedChoiceElsewhere); otherwise it observes the alias as before.

In the reported order, GetSlot(4) records 4 as interrupted. At the save header the alias at 9 cannot replace that, and capture writes kind 6 at 4.

Regression SaveBetweenNativeReads:
- Setup: the mixed older bar (stale canonical at 4, stale Mount alias at 9, pet unresolved). Native GetSlot empties 4 only and the save runs before 9 is read.
- Result: exactly one public binding row, at 4. Reload and pet return restore one shortcut at 4 with slot 9 empty. The continuing world also returns one shortcut at 4.

The full-pass control (MixedCleanupPassKeepsCanonical) and the alias-only control (AliasNativeCleanupFirst) still pass. The mutation removing the rule failed SaveBetweenNativeReads. The gate passed on b37d4fb2.
- **R15-F1** — `fixed` — commits: `b37d4fb2ef`
  Two rules now apply. First, MountedControlPlacementIntent.ObserveNativeSlotInvalidated does not replace a chosen slot already interrupted at another index, so a second report in the same absence cannot move the choice; ObserveNativeAliasInvalidated delegates to it. Second, ObserveInvalidatedPublicSlot ignores a canonical report unless MountedControlSlotPolicy.IsSelectedPublicReport(PublicBindingIndex(slots), index) holds. A report for a secondary copy read while the first canonical binding is still present is therefore dropped.

Together these keep the first canonical index, the one RecoveredPublicSlot chooses at capture. A live arrangement, meaning a shortcut the player moved since the last observation, is still replaced by the reported slot.

Regression TwoCanonicalCleanup:
- Setup: canonical bindings at 3 and 9, then the same pet becomes unavailable.
- Run in three orders: one cleanup pass, GetSlot 3 then 9, and GetSlot 9 then 3.
- Each order saves exactly one public row, at 3, and reload plus pet return restores one shortcut at 3.
- Control: a single shortcut moved from 9 to 3 without being observed returns at 3.

The mutation removing both rules failed this regression at its first sub-case. The gate passed on b37d4fb2.
- **R15-F2** — `fixed` — commits: `b37d4fb2ef`
  The service now returns early from ObserveArrangementOnBar, RecordAbsence, RecoverLegacyAlias and the lease-release ObserveBound when disabled. Save sanitation is unchanged: BeginSaveSerializationScope still leases and empties every KMC binding, including reserve and detached bars. MountedControlLoadFence.Begin with controlsActive false (a disabled selected load) now also clears the live intent and placement set before releasing, so nothing from the outgoing world survives.

Regression DisabledSaveIsolation:
- While disabled, a save of an older world with a surviving canonical shortcut at 9 on a party rider removes the binding, leaves Attack at 0, and leaves both placement collections empty.
- Leftover outgoing state is then seeded and a world with the same rider and pet ids and no shortcut rows is loaded while still disabled. Both collections are cleared and no fence is armed.
- After enabling with autofill off, slot 9 stays empty.

The mutation removing the guards and the clearing failed this regression at its first assertion ('Disabled save sanitation recorded placement state'). The gate passed on b37d4fb2.

Limit: the service's disabled guards are mirrored by the double; the fence clearing is production domain code.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 54.2s | Result of record, on commit b37d4fb2 (log gate-focused-b37d4fb2efd6-d43d449b6c4-a030b738800-20261121T183244344540Z.log; exit 0; worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.87 min | Development run: components 1480/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1476/4. Reverted for the run: the alias-at-boundary rule, the interrupted-choice rule, the selected-report rule, the disabled-load clearing, and the double's disabled observation guards. Failed: SaveBetweenNativeReads, TwoCanonicalCleanup (at its first sub-case, 'pass'), DisabledSaveIsolation (at its first assertion), and round 14's MixedCleanupPassKeepsCanonical, which now depends on the same rule. Later sub-cases were not exercised separately under mutation. All mutations were reverted and a grep confirmed none remained. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
