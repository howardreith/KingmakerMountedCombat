<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r16-request part=1/1 sha256=b9bb801666df8cd0c4fca4d0fe04dcad5e3b7c02d1a0efd437ace38a3c6b2cc0 -->
### Claude Opus — review request — round 16 — candidate `b37d4fb2ef`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r16-0a946b` |
| Checkpoint | `final` (final) |
| Candidate H | `b37d4fb2efd62c24b1ce982e4509ce4bc9f76b11` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 16 of 30 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 15 fixes all three pending findings in commit b37d4fb2. The guarded push succeeded, the controller focused gate passed on that commit (54.2 s, worktree unchanged) and the worktree is clean.

R14-F2 (alias overrides the canonical slot when a save falls between native reads): at a save boundary, a surviving older alias now goes through the new MountedControlPlacementIntent.ObserveAliasAtBoundary. It never overrides a chosen slot already interrupted at another index, such as the canonical slot that native GetSlot emptied just before. An alias-only bar and an alias returned by a save scope are still recognized.

R15-F1 (two canonical bindings): two rules now keep the first canonical index.
- ObserveNativeSlotInvalidated never replaces a chosen slot already interrupted at another index.
- A native report for a canonical slot counts only when that slot is the bar's selected public binding (the new MountedControlSlotPolicy.IsSelectedPublicReport).
A cleanup pass, or GetSlot calls in either order, therefore keeps the first canonical index, the same one capture would save before any cleanup. A single shortcut the player moved is still recognized at its new slot.

R15-F2 (disabled saves and loads): while KMC is disabled, bar observation, absence recording, alias recovery and lease release record nothing. Save sanitation still clears every KMC binding. A disabled selected load now also clears any live placement records (MountedControlLoadFence.Begin with controlsActive false), so the next world starts with its own arrangement.

Three new component regressions cover these sequences. I silenced PASS lines in tests/TestRunner.cs during development runs; that change was reverted and is absent from the commit.

A combined mutation run failed four regressions: the three new ones and round 14's mixed cleanup pass, which now relies on the same rule. Each test stops at its first failing assertion, so later sub-cases such as the 9-then-3 order were not exercised separately under mutation.

Native checks remain NOT RUN; the owner playtest steps are in the handoff.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 54.2s | Result of record, on commit b37d4fb2 (log gate-focused-b37d4fb2efd6-d43d449b6c4-a030b738800-20261121T183244344540Z.log; exit 0; worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children. Offline only. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree, PASS lines temporarily silenced) | no | pass | 0.87 min | Development run: components 1480/0, Validate-Source 153/0, assembly contracts 822/0, click composition 21/0, archive bound 9/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The temporary tests/TestRunner.cs change was reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (combined mutation) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible: 1476/4. Reverted for the run: the alias-at-boundary rule, the interrupted-choice rule, the selected-report rule, the disabled-load clearing, and the double's disabled observation guards. Failed: SaveBetweenNativeReads, TwoCanonicalCleanup (at its first sub-case, 'pass'), DisabledSaveIsolation (at its first assertion), and round 14's MixedCleanupPassKeepsCanonical, which now depends on the same rule. Later sub-cases were not exercised separately under mutation. All mutations were reverted and a grep confirmed none remained. |

**Evidence references:**
- commit b37d4fb2efd62c24b1ce982e4509ce4bc9f76b11
- gate-focused-b37d4fb2efd6-d43d449b6c4-a030b738800-20261121T183244344540Z.log
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs (HoldsInterruptedChoiceElsewhere, ObserveNativeSlotInvalidated, ObserveAliasAtBoundary, MountedControlLoadFence.Begin disabled clearing)
- src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs (IsSelectedPublicReport)
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs (ObserveInvalidatedPublicSlot selected report, ObserveArrangementOnBar alias routing and disabled guard, RecordAbsence, RecoverLegacyAlias, lease release)
- tests/MountedControlPlacementIntentTests.cs (SaveBetweenNativeReads, TwoCanonicalCleanup, DisabledSaveIsolation; double NativeInvalidateAt and disabled guards)
- docs/CITY-AND-PRONE-HANDOFF.md (Round 15 additions, Validation)

**Prior findings to assess:** `R14-F2`, `R15-F1`, `R15-F2`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
