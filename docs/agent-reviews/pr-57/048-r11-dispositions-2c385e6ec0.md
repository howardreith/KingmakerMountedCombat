<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r11-dispositions-2c385e6ec0 part=1/1 sha256=2c385e6ec029546aab900babc3293538101971ccc410cc53f1312dab5f0ac338 -->
### Claude Opus — finding dispositions — round 11

Candidate after corrections: `d431ea7f19b783e7e0e1a062d461275ea113cd51`

- **R11-F1** — `fixed` — commits: `d431ea7f19`
  The new MountedControlSlotPolicy.LivePublicRows chooses the public binding before emitting any row, using RecoveredPublicSlot: the bar's first canonical binding, otherwise its first exact older alias. It emits that slot as kind 6 only when it is callable live data.

In the current schema, NativeMountedControlService.CaptureLiveSlots skips every public-kind slot in its per-slot loop and writes the public binding through LivePublicRows. IsCallableLiveBinding is the existing callable rule, now factored out. A live alias at 3 therefore never stands in for the canonical binding at 9. When the canonical binding holds stale AbilityData, nothing is written live: classification carries index 9 as interrupted, and CaptureAbsentIntent writes kind 6 at 9. The legacy-schema writer and RestorePersistentSlots are unchanged, so older archives restore as before.

Production-codec evidence: Test-CityArchiveRowBound.ps1 has a mixed-bar check for current and for stale canonical data. The rows come from LivePublicRows and AbsentRowsAfterLive over a bar in production slot order. After MountedArchiveCodec Encode/Decode there is exactly one restorable public binding, at 9, one placement record, and no binding row at 3.

Component regression MixedBarSaveFirst, current and stale: callable alias at 3, canonical binding at 9, Attack at 0 and Potion at 5. A save before any update writes one public binding row, at 9, with unique keys. The saving world keeps the control at 9 with slot 3 empty. A cold load restores one control at 9, slot 3 stays empty, there is exactly one KMC binding, and the unrelated slots are unchanged.

A mutation restoring slot-order choice failed exactly this regression. The controller gate passed on d431ea7f.
- **R10-F1** — `fixed` — commits: `d431ea7f19`
  LivePublicRows decides the placement record separately from the binding. It writes the record when the rider has one (enabled && placementRecorded) and either the public control is live or this capture wrote the binding. A live alias whose canonical fact is missing or inactive therefore emits both its binding and its record; RecoverLegacyAlias recorded the placement during the header's classification.

The double now models shell activity independently of the canonical fact (ShellLiveOverride).

Regression LiveAliasCanonicalInactive, for both the Mount and the Dismount alias: canonical fact inactive, alias at 9 with a live shell, no prior record, save before migration. The archive holds exactly two rows, one binding at 9 and one placement record, with unique keys. A reload restores one control at 9.

Production-codec checks for both alias kinds: the rows are emitted by LivePublicRows with publicControlActive=false and the record recorded, so the fixture no longer supplies the record itself. After Encode/Decode there is exactly one binding at 9 and one placement record. Duplicate rejection is intact: an extra alias row at 9 is refused with 'Invalid or duplicate owned archive control slot.'

A mutation tying the record to the canonical fact failed exactly LiveAliasCanonicalInactive. The controller gate passed on d431ea7f.
- **R2-F4** — `fixed` — commits: `d431ea7f19`
  ClearSerializationHotbarLeases now calls placementIntent.ObserveBound for any restored lease whose kind passes IsPublicControlKind, so a returned older alias counts as well as the canonical control. The placeholder check, the active-fact check and the AllowsLeaseRestore companion and retirement check are unchanged. A same-companion alias returned to slot 9 is therefore the live arrangement, no longer interrupted. If the player removes it before migration, ResolveUnbound records it off the bar.

The double's ReleaseLeases mirrors this.

Regression AliasRemovedAfterLeaseRelease:
- The scope ends with the same pet and returns the Mount alias at 9, and the player removes it before the next update.
- After two updates there is no control or alias on the bar and slot 0 is unchanged. The next save holds only the placement record, and a load leaves slot 9 empty.
- Control: the untouched returned alias migrates once to one canonical control at 9.

A mutation restoring the canonical-only ObserveBound in the double's release failed exactly this regression. The controller gate passed on d431ea7f.

**Validation (developer-reported):**

| Command | Required | Result | Duration | Scope and limits |
|---|---|---|---|---|
| python C:/Dev/agent-review/versions/617060d8f8782b811b1dd7bdca1f07abcbc488e338433e0d0cb4511287696954/agent_review.py verify --enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c --gate focused -- -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 | yes | pass | 24.9s | Result of record, on commit d431ea7f (log gate-focused-d431ea7f19b7-d43d449b6c4-a030b738800-20261121T172018569240Z.log, exit 0, worktree unchanged). Covers the build, FAST components, Validate-Source and the focused wrapper's five children, including archive bound 7/0. Offline only; no native gameplay. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (fixed tree; PASS lines temporarily silenced in tests/TestRunner.cs) | no | pass | 0.38 min | Development runs. The first run failed on my own assertion: the mixed-bar test checked the earlier world's rider object after a load, and departure cleanup empties that object by design. I moved the assertion before the load. The next run passed: components 1464/0, Validate-Source 153/0, assembly contracts 820/0, click composition 21/0, archive bound 7/0, six-pair adapter 22/0, area baseline 63/63, focused 5/5. The TestRunner change was reverted and is absent from the commit. |
| .\scripts\Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 (mutation: slot-order binding choice plus a placement record tied only to the canonical fact) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible. Components 1462/2, failing exactly 'a mixed bar saved first ... restores one control at 9' (stale=False) and 'a live alias with the canonical fact inactive saves its binding and its placement record' (KMC_Mount#1). Reverted. |
| .\scripts\Test-Tiers.ps1 -Tier Fast (mutation: double's lease release records ObserveBound only for the canonical control, the pre-fix production line) | no | fail | ~1 min | Intended failing reproduction, with FAIL lines visible. Components 1463/1, failing exactly 'removing an alias the save scope returned, before the next update, stays removed through save and load'. Reverted. The production line itself is service code that the double mirrors. |

_Model-generated developer response (Claude opus / high), posted by the agent-review controller through the owner's GitHub account; not a human review._
