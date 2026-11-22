<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r16-dispositions-373f9c9f8f part=1/1 sha256=373f9c9f8f3c19dd3e21b394c3263c0e149fe343e143be3e799b7c90e592aa2c -->
### Claude Opus — finding dispositions — round 16

Candidate after corrections: `d4b3c2d4f2789242c867a9c4366705c7ba0f6226`

- **R14-F2** — `fixed` — commits: `d4b3c2d4f2`
  While the canonical choice at 4 is interrupted, a surviving older alias at 9 is now a secondary copy (IsSecondaryBinding with IsStaleOrAlias).
- The bar observation keeps 4 (ObserveBindingAtBoundary).
- Capture classification and LivePublicRows skip the alias, so CaptureAbsentIntent writes kind 6 at 4.
- ReconcilePublicControlSlots identifies the copy before migrating it and removes the migrated copy when slot 4 is free, so ReconcileChosenSlot returns the control to 4.
- An alias-only bar has no interrupted choice and still migrates in place.

Regression SurvivingAliasAfterCanonicalCleanup:
- MixedOlderBar, GetSlot empties only 4, the same pet resolves and two updates run before any save. Result: one shortcut at 4, slot 9 empty, one KMC binding, and the resave holds one public row at 4.
- A save first, with the alias callable at the header. Result: one public row at 4 with unique keys, and the continuing world settles on one shortcut at 4.
- Control: an alias-only bar still migrates at 9.

The mutation run failed this regression. The controller focused gate passed on d4b3c2d4.
- **R15-F1** — `fixed` — commits: `d4b3c2d4f2`
  After GetSlot empties canonical 3, the surviving canonical copy at 9 holds no live data and is therefore a secondary copy of the interrupted choice at 3.
- At the header, observation keeps 3, the live writer skips 9 and the remembered writer saves 3.
- If the pet returns first, the copy at 9 is rebound, recognised as secondary and removed, and the control returns to 3.
- A live player placement is never secondary, and the manual-move control in TwoCanonicalCleanup still passes.

Regression SurvivingCanonicalCopyAfterCleanup (canonical at 3 and 9, pet unavailable, GetSlot(3) only):
- Save between the reads: one kind-6 row at 3, and reload plus pet return restores one shortcut at 3.
- Return before saving: one shortcut at 3 with slot 9 empty, and the resave carries 3.

The pass and reverse-order variants remain in TwoCanonicalCleanup. The mutation run failed this regression. The controller focused gate passed on d4b3c2d4.
- **R16-F1** — `fixed` — commits: `d4b3c2d4f2`
  TryInitialPlacement now calls placementIntent.ObserveBound(unitId, chosen, PetUniqueId) when it writes the first automatic placement, or ObserveOffBar(unitId, PetUniqueId) for a full bar. With the companion recorded, RetireOnCompanionLoss sees a direct change from A to B and retires the placement and clears its slot before the next observation. The double's autofill does the same.

Regression InitialPlacementCompanionChange, run update-first and save-first: after the first automatic placement for A, the link switches directly to eligible B. B gets no shortcut, Attack at 0 is unchanged, and the archive has no public row. Control: the same A, temporarily unavailable, gets its automatic placement back once.

The mutation removing the record failed this regression with 'Pet B inherited pet A's automatic placement'. The controller focused gate passed on d4b3c2d4.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/34fb852974476cfbd48374fc676c822b4e2f97911cee6706a79161c7e9d9d43c/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.4s | Result of record on commit d4b3c2d4 (log gate-focused-d4b3c2d4f278-d43d449b6c4-a030b738800-20261121T184923995103Z.log), exit 0, worktree unchanged. Covers the build, FAST components, Validate-Source and the five children of the focused wrapper. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree before commit, PASS lines temporarily silenced) | no | pass | 0.93 min | Development run: components 1483/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted before commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (mutation: IsSecondaryBinding forced false; initial-placement record removed in the double) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1479/4. The failures were SurvivingAliasAfterCanonicalCleanup, SurvivingCanonicalCopyAfterCleanup, InitialPlacementCompanionChange ('Pet B inherited pet A's automatic placement') and round 15's SaveBetweenNativeReads. Mutations were reverted. The initial-placement line is service code that the double mirrors. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
