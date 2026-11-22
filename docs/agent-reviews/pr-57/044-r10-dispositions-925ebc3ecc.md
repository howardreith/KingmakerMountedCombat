<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r10-dispositions-925ebc3ecc part=1/1 sha256=925ebc3ecc8e99142ff94a0aee8fce3097115a216cc62c3df686ec7e18a07499 -->
### Claude Opus — finding dispositions — round 10

Candidate after corrections: `ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7`

- **R10-F1** — `fixed` — commits: `ddbe460f69`
  New domain rules: MountedControlSlotPolicy.IsPublicBindingRow(kind) means any non-placement kind that RestoreSavedSlot maps to MountState (kinds 1, 2 and 6). AbsentRowsAfterLive(index, liveKinds) returns no rows when the live writer already wrote a public binding for the actor; otherwise it returns AbsentArrangementRows without a second placement record.

NativeMountedControlService.CaptureLiveSlots adds the actor to `written` for any public binding row, so a live alias counts. CaptureAbsentIntent composes its rows through AbsentRowsAfterLive from the actor's rows already in the result. The archive's ValidateSlots duplicate-key check is unchanged.

Production-codec evidence: tests/Chunk7Diagnostics/ArchiveControlRowBoundTests.cs, run by Test-CityArchiveRowBound.ps1 inside the focused wrapper, has two new checks, one each for kind 1 (Mount) and kind 2 (Dismount). Each takes the live alias row at 9 plus the placement record, adds the absent rows from AbsentRowsAfterLive for the recovered arrangement at 9, and runs MountedArchiveCodec.Encode/Decode with validation. The decoded archive holds exactly one row restoring as the public control, at 9, and one placement record. Control: appending a kind-6 row at 9 is refused with 'Invalid or duplicate owned archive control slot.'

Component regression LiveAliasSaveFirst covers both aliases. An eligible party rider has a live alias at 9, no canonical binding and no placement record, and saves first. The rows have unique actor/index keys (the validator's SavedRowKey), one public binding at 9 and one kind-7 row. The alias migrates to one public control at 9, and a reload restores one binding at 9.

The mutation restoring the kind-6-only check exited 1. The controller gate passed on ddbe460f.
- **R9-F2** — `fixed` — commits: `ddbe460f69`
  ClearSerializationHotbarLeases now applies AllowsLeaseRestore to every lease whose ResolveKind is a public binding. The new MountedControlSlotPolicy.IsPublicControlKind covers the public control and the exact legacy Mount and Dismount aliases. The placeholder check and the active-fact check are unchanged, and other KMC kinds are unaffected.

The toolbar double's Lease now leases every KMC binding with its kind. Its ReleaseLeases restores an alias as its shell string, gated by IsPublicControlKind and AllowsLeaseRestore, as production does.

Regression AliasLeaseAfterCompanionChange:
- Pet A is unresolved with its legacy Mount alias at 9, and the save runs before the first update.
- Pet B (eligible) replaces A during the scope, then the scope releases, reconciles and resaves.
- Slot 9 stays empty with no alias or public binding, slot 0 is unchanged, and the resave has no public-binding row.
- Control: with A retained and resolving, exactly one public binding is at 9 after migration, and the resave has a kind-6 row at 9.

The mutation making IsPublicControlKind canonical-only exited 1. The controller gate passed on ddbe460f.
- **R9-F4** — `fixed` — commits: `ddbe460f69`
  NativeMountedControlService.ObserveInvalidatedPublicSlot now accepts any blueprint for which IsPublicControlKind holds. For an alias slot that is still on the current-world rider's bar (exact reference), it computes LegacyAliasIndex(slots): RecoveredPublicSlot, returning -1 when a canonical binding is on the bar so the canonical binding wins. It then records that index through RecoverLegacyAlias, which uses AdmitSurvivingBinding with ShouldLease. A different companion, or a cleared link while the rider no longer gets the control, recovers nothing, and an existing arrangement wins.

While a selected load fences the world, the index goes to the load scope's outgoing records through MountedControlLoadFence.AdmitOutgoingSurvivingBinding, together with their placement record.

Regression AliasNativeCleanupFirst covers both Mount and Dismount aliases:
- Setup: no rows, an unresolved linked pet and an inactive alias at 9, emptied by native cleanup before any update or capture.
- The resave keeps kind 6 at 9 with unique keys, and the same pet's return restores one canonical binding at 9.
- Controls: a canonical binding at 4 wins (nothing recorded, the control returns at 4 once, slot 9 empty). A cleared link recovers nothing and a later pet does not get slot 9. Cleanup while fenced, followed by an abandoned load, keeps index 9.

The controller gate passed on ddbe460f.

Limits: the double's alias cleanup calls RecoverAlias directly, mirroring the production observer. The native IsBad call order on older bars is NOT RUN.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 47.7s | Result of record, on commit ddbe460f (log gate-focused-ddbe460f69e0-d43d449b6c4-a030b738800-20261121T170630023526Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including archive bound 5/0. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | no | pass | 0.75 min | Development run on the fixed tree. Components 1461/0, including the three new toolbar tests. Validate-Source 153/0, assembly contracts 820/0, click composition 21/0. Archive bound 5/0, including both live-alias codec checks. Six-pair adapter 22/0. Area baseline: 63 errors on both trees. Focused 5/5. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (temporary mutation making AbsentRowsAfterLive and IsPublicControlKind canonical-only) | no | fail | ~1 min | The intended failing reproduction: exit 1. Tool truncation cut off the FAIL lines. Both mutations were reverted before the commit, and the controller gate passed on the committed tree. Limit: the alias-cleanup call order in the toolbar double goes through RecoverAlias directly. The production IsBad filter's alias coverage (IsPublicControlKind) is checked only by that mutation's effect on the lease gate. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
