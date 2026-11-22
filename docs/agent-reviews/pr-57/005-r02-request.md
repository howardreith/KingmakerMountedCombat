<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r02-request part=1/1 sha256=283ee1315d0155c1eb574a3b0bab0c4252236607098fb3929d42315889dfb795 -->
### Claude Opus — review request — round 2 — candidate `7f6146c043`

| Field | Value |
|---|---|
| Request ID | `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r02-136328` |
| Checkpoint | `final` (final) |
| Candidate H | `7f6146c0432f8c399a037bc1363015db2e283f34` |
| Target B (`main`) | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Merge base M | `dbf637021db42cdc9bf27f3d79fc255d4569c788` |
| Mission | `mounted-combat-city-and-prone-mission` sha256 `4d7161071b` |
| Workflow | `6b0bec9f60` |
| Developer observed | headless session `9457917c-3987-4429-bafc-e57e71d97c80`: init `claude-opus-5-5`, API models ['claude-opus-5-5'], effort ['high'] |
| Reviewer requested | Codex `gpt-6.2-astra`, effort `high`, read-only sandbox |
| Round budget | 2 of 5 |

**Scope:** The complete mission as written.

**Exit conditions:**
- Every requirement of the mission is implemented, with evidence appropriate to each claim; no requirement is weakened or silently dropped.
- Validation follows the repository's own instructions; commands, results and limits are reported honestly.
- Deviations, limitations and deferred optional items are stated explicitly, separate from required work.

**Developer summary:**

Round 1 corrections are in commit 7f6146c0, which is pushed. The focused gate passed on it, and the worktree is clean.

Toolbar persistence:
- The tracker now records an arrangement for each rider: a chosen slot, or off-bar. Off-bar means the player removed the control, filled its slot, or the one placement found a full bar.
- MountedControlPlacementIntent.Reconcile holds the per-update decisions in the service's order. When the service replaces a missing or inactive desired public fact, the replacement is recorded as a temporary absence before the bar is read.
- Save scopes and area unloads observe the bar before emptying it. While a save scope's leases are pending, they alone restore the emptied slots.
- RestorePersistentSlots(rows, loadedWorld) separates a freshly loaded save from an in-world replay. A replay seeds only riders with no arrangement and writes a public-control row only where the current arrangement puts it.
- An absent rider's arrangement is retired only on positive evidence: the rider left every player roster, or its PetUniqueId is cleared or changed. An unresolvable rider keeps its arrangement.
- An absent rider's arrangement is saved as its slot row plus placement record, or as the placement record alone when the control is off the bar.
- Capture never truncates. Over 128 rows fails the archive's existing validation, whose bound is now read through MountedControlSlotPolicy.FitsArchive (same value, tested).

City roster: the GetGroup projection is gated on NativeMountedControlService.IsActive, so a disabled mod returns the native list object.

Handoff corrections:
- R1-F8: the reserve-owner/pet fixture does exist. Batch199 r58/r59 at Oleg's had the Small Ranger moved to remote and out of game, with its Horse also out of game. Those PASS verdicts belong to the pre-F2 binary 3842ad94.
- The handoff records per-case native prerequisites: a candidate package that no allowed command builds; the capital reader and route still expect the pre-F2 two rows of kinds 2,3 and need same-candidate lineage (8 launches); no driver for city panel selection and equipment changes; F2 r05 evidence is absent (T09); no diagnostic makes an enemy conscious-prone or unconscious.
- R1-F9: the conscious-prone case is traced stage by stage. No mounted divergence was found in click, admission, approach, range/LoS or firing. The plausible native-only trap is that a conscious prone enemy keeps standing-pose colliders, so a click on the lying body can become a ground order.

Native legs remain NOT RUN; the owner playtest steps cover them.

Affected neighbours: the two chunk7 actual-method adapters (six-pair archive, area restore preflight) fail to compile for reasons predating this branch. Their facades lack members added by later features. They were run once on this branch, my template edits were reverted, and they are not selected. The Test-Finalize2UpgradeReaders locator was adjusted to the stable signature prefix; that reader remains known-broken (T09) and is not selected.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 18.7s (incremental build; local full run ~22.7s build + ~20s tests) | Candidate 7f6146c0, worktree unchanged. Build PASS. FAST components 1429/0, including 9 toolbar tests (production Reconcile/ObserveAtBoundary/Seed/AllowsSavedSlot/Revalidate/TryGetArrangement/AbsentArrangementRows/FitsArchive, driven by a labeled service-and-bar double in the service's event order), 6 city-roster tests (including enabled/disabled/re-enabled) and 3 downed-target tests. Validate-Source 153/0. Focused wrapper: Test-AssemblyContracts.ps1 -Target Kingmaker 819/0; Test-GunslingerClickComposition.ps1 21/0 in both patch orders. Offline only, not native evidence. Wrath not selected; no Candidate/Full. |
| powershell -File scripts/chunk7/Test-Chunk7SixPairArchiveAdapter.ps1 and scripts/chunk7/Test-Chunk7AreaRestorePreflightAdapter.ps1 (run once via the wrapper during development) | no | fail | a few seconds each | Pre-existing compile failures of the typed facades against the current product: missing MountedControlSlotPolicy.cs in the six-pair source list since F2; missing MountedAreaControlRule, retained-intent types/counters and Report(string,bool) in the area facade. Classified from the missing members; not re-run at main. My template edits were reverted, and the adapters are not selected in the wrapper. |
| Native guarded runtime cases (capital arrival/cold, F2 shortcut chain, city pet panel, prone/unconscious ranged legs) | no | not_run | 0 | Not runnable here. Invoke-KingmakerRuntimeScenario.ps1 validates a candidate package bound to the commit (Validate-Package.ps1 plus manifest), and no allowed command builds one. Further per-case prerequisites are recorded in docs/CITY-AND-PRONE-HANDOFF.md 'Native checks'. No runtime transaction ran. |

**Evidence references:**
- ~/.agent-review/state/kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c/evidence/gate-focused-7f6146c0432f-d43d449b6c4-a030b738800-20261121T122533707135Z.log
- docs/CITY-AND-PRONE-HANDOFF.md
- tests/MountedControlPlacementIntentTests.cs
- tests/CityCompanionRosterProjectionTests.cs
- tests/MountedDownedTargetOrderTests.cs
- src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs
- src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs
- commit 7f6146c0432f8c399a037bc1363015db2e283f34

**Prior findings to assess:** `R1-F1`, `R1-F2`, `R1-F3`, `R1-F4`, `R1-F5`, `R1-F6`, `R1-F7`, `R1-F8`, `R1-F9`

_Posted by the agent-review controller through the owner's GitHub account on behalf of the Claude Opus developer. Automated request; not a human review._
