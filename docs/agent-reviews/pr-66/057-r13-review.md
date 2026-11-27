<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r13-review part=1/1 sha256=d15566d3eae964abc478a52849dd02a0b936ea4ca67288a0754b016600c6416a -->
### Codex GPT-6.2 Astra — review — round 13 — candidate `69623d76c1`

**Decision:** `changes_required` — The required Fast gate passed on the exact candidate. A failed session lookup can still leave a confirmed removal armed for a later update. A closed repair panel can also retain an old horse and its selectable token after the world changes. The companion evidence producer has a callback-classification defect, and its claimed producer-to-reader check lacks a candidate-bound execution record.

Continuation for Claude

Take ownership of a pending confirmation before reading the live session. Record a terminal outcome if that read fails, and report the stored outcome independently of presentation refresh. Retire old bindings on a session change even while the panel is closed. Keep expensive candidate scans on demand. Make the two player-facing messages identified in the findings describe only what the service observed.

Classify the complete UMM callback and pass the production observation through the companion reader. Restore the independent unit-replacement and native-removal controls, and exercise the successful disable and failure-stop transitions through production code. Supply the required focused gate and candidate-bound output for the affected reader. Keep the correction within PR-4's confirmed in-game action, the existing world-write gate and the exact-removal boundary. OWNER AMENDMENT 1 keeps legacy files inert and assigns native UI and gameplay save/load observations to the integrated-alpha playtest.

#### Findings

**R12-F4** · P1 · defect · blocks readiness — A session lookup failure leaves a confirmed removal armed
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:184`; acceptance: MMR-06, S06
- Trigger: The live session lookup throws on the first update after confirmation, then succeeds on the next update.
- Expected: A failure to establish the session consumes the confirmation and records a refusal for the confirmed horse. An unrelated presentation refresh cannot prevent reporting an already classified action.
- Actual: Run calls host.SessionMarker() before Execute takes the confirmation. If that read throws, ProcessTripRepairConsole catches the exception, the prior result remains visible, and the next successful update can remove the fact under the same confirmation. At the other end of Run, a throwing LoadedHorses refresh skips host.Report even after Execute stored a removal.
- Why it blocks / does not: A failed validation can defer a saved-horse mutation to a later frame without fresh confirmation. A separate presentation failure can suppress reporting of a completed action.
- Requested correction: Take the pending intent before the session lookup, classify lookup failure against its captured identity, and give the stored outcome a reporting path independent of presentation discovery.
- Requested evidence: After a prior result for B, confirm A and inject a one-time SessionMarker failure. Show a refusal naming A, zero further removals and no execution on recovery. Separately fail presentation refresh after a valid removal and verify A's stored outcome reaches a working reporter exactly once.

**R12-F2** · P2 · defect · blocks readiness — World replacement leaves old horse bindings alive while the panel is closed
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:186`; acceptance: MMR-06, S06
- Trigger: The session changes between party scans after the player has viewed or selected a horse and closed the panel.
- Expected: A detected world replacement retires outgoing offers, selections and object references regardless of whether the panel is drawn.
- Actual: With no armed confirmation, Execute returns immediately. Only Observe compares the marker and clears the console's bindings, and Run calls Observe only after a presentation request and successful roster read. The old offer, selected token and strong references to the old unit and fact can therefore survive repeated updates in the new world.
- Why it blocks / does not: The maintenance surface can retain and offer a discarded horse. Dispatch's later marker check guards removal, but it does not release the outgoing objects or retire the stale selection at the lifecycle boundary.
- Requested correction: Process session invalidation independently of the presentation request and roster refresh. Preserve the terminal outcome for any confirmation being dispatched.
- Requested evidence: Discover and select A, close the panel, replace the session while keeping a party available, and run an update without requesting presentation. Show empty old offers and rejection of the old token before reopening, then obtain a usable fresh offer.

**R13-F1** · P2 · defect · blocks readiness — A mixed UMM callback can be reported as maintenance-only
- Location: `src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.CompanionNames.cs:601`; acceptance: MMR-06
- Trigger: The entry's OnGUI invocation list contains an unrelated handler followed by the repair handler.
- Expected: Only the exact single maintenance callback qualifies for the repair-only exception.
- Actual: IsRepairMaintenancePanel compares callback.Method.Name and DeclaringType. For a multicast delegate, Method describes its last entry. An unrelated handler followed by Main.OnMaintenanceGui therefore yields optionsPanel=false and maintenancePanel=true, which the reader accepts.
- Why it blocks / does not: The changed producer can certify the repair-only UI constraint while the entry also draws an unauthorized options surface.
- Requested correction: Compare the complete delegate with the authorized callback, or check that its invocation list contains exactly that one handler.
- Requested evidence: Feed both handler orders through the production classifier and reader. Both mixed callbacks should be refused and the single maintenance callback accepted.

**R12-F6** · P2 · evidence_gap · blocks readiness — The companion-reader check supplies the expected flags instead of observing the callback
- Location: `scripts/Test-Finalize5CompanionReaders.ps1:80`; acceptance: MMR-06
- Trigger: The round-13 results are used to verify the companion-name evidence contract.
- Expected: A candidate-bound offline check passes observations produced from actual delegates into Assert-KmcFinalize5CompanionNames and retains the historical control.
- Actual: The new fixture sets maintenancePanel and optionsPanel itself. It never calls the production observer. Test-Finalize5CompanionReaders.ps1 is also absent from the 12 executed children, and the supplied evidence contains no standalone reader receipt.
- Why it blocks / does not: The requested producer-to-reader evidence is missing. The fixture would give the same verdict if the product emitted the wrong flags.
- Requested correction: Use the production code that derives the panel fields in the reader fixture and supply its execution output for the corrected candidate.
- Requested evidence: Show acceptance of the actual maintenance callback and historical no-panel observation, and refusal of an unrelated callback. Keep the name, active-state and Mounted Charge checks in effect.

**R12-F1** · P2 · evidence_gap · blocks readiness — The stop test bypasses both lifecycle paths it claims to exercise
- Location: `tests/HorseTripRepairPolicyTests.cs:193`; acceptance: MMR-06, S06
- Trigger: The passing component case is cited as execution of disable/re-enable and failure-stop revocation.
- Expected: The selected confirmation is driven through each production stop transition before the next update.
- Actual: Both iterations of DisableAndFailureStop call host.Console.Invalidate() directly. Neither disables, re-enables or invokes failure cleanup. The compiled contract verifies that the root methods contain an invalidation call.
- Why it blocks / does not: The source correction is present, but the behavioral evidence requested for revocation at the successful production stop boundary is still missing.
- Requested correction: Exercise the production lifecycle transition used by successful disable and failure cleanup with the external teardown boundary supplied by the offline fixture.
- Requested evidence: Confirm A, complete each stop, re-enable before another repair update, and show zero removals, rejection of the old token and one removal after fresh confirmation.

**R12-F7** · P2 · evidence_gap · blocks readiness — The shared updater tests dropped independent identity and removal controls
- Location: `tests/HorseTripRepairPolicyTests.cs:170`; acceptance: MMR-06, S06
- Trigger: The new test suite is used as the successor to the earlier live-object and exact-removal regressions.
- Expected: The shared updater independently demonstrates live-object binding and retains the non-file removal classifications covered before the amendment.
- Actual: Both replacement cases change the session marker. The previous same-session, same-ID replacement of the unit alone was removed. RemovalOutcomes also separates a native throw from over-removal and uses a throwing no-change case in place of the earlier normal no-effect return.
- Why it blocks / does not: The marker check can make the replacement test pass even if live-object admission is broken. Two historical non-file removal controls also lost their distinguishing failure conditions.
- Requested correction: Port the object-only replacement case from e04a342b and the normal no-effect and target-plus-neighbor-then-throw cases from the earlier suite into the shared updater tests. Align the mapping with the executed cases.
- Requested evidence: Keep the marker and ID fixed, replace only the unit, and verify that both objects retain their facts. For each removal control assert its specific stored classification, one native call and no later replay.

**R13-F2** · P2 · defect · blocks readiness — The unchanged result overstates what the feature comparison verifies
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:151`; acceptance: MMR-06, S06
- Trigger: A native call leaves the same feature instances in the collection after changing state on one of them.
- Expected: The result states what was verified about the target and feature collection.
- Actual: SameMultiset compares feature references. An in-place feature change is invisible to both snapshots, yet the panel tells the player that the horse was verified unchanged.
- Why it blocks / does not: This gives the player a stronger assurance than the classifier established when deciding whether to save after a native failure.
- Requested correction: Narrow the Unchanged message to the observed feature membership and continued presence of the target. The existing reference comparison can support that statement.
- Requested evidence: Use the existing native-removal fixture to change a feature field in place and throw. Verify that the reported result describes membership accurately and that removal is invoked once.

**R13-F3** · P3 · defect · blocks readiness — A horse without a stable ID is incorrectly described as having duplicate facts
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs:200`; acceptance: MMR-06, S06
- Trigger: A loaded horse has one candidate and no stable ID, as exercised by StableIdAndRename.
- Expected: The panel identifies the missing stable identity as the reason for refusal and reserves the duplicate-copy explanation for multiple candidates.
- Actual: Observe correctly makes the row ineligible, but DrawTripRepairGui prints “More than one copy” for every ineligible offer. The same row has just reported one fact.
- Why it blocks / does not: The repair correctly refuses the mutation, but gives a false account of the horse's facts and why the action is unavailable.
- Requested correction: Choose the refusal text from the offer's stable ID and candidate count.
- Requested evidence: Check the presentation text for a single candidate with no stable ID and for two candidates on an identified horse. Both offers must stay unselectable.

**R12-F9** · P3 · optional_polish · non-blocking — The native-contract header still describes a written repair request
- Location: `scripts/Test-MmrHorseTripNativeContract.ps1:12`; acceptance: —
- Trigger: A maintainer reads the active contract script to determine its scope.
- Expected: The active header describes the confirmed in-game maintenance action and the checks this script runs.
- Actual: The focused wrapper was updated, but the native-contract header still says removal happens on the owner's written request. The next line also refers to an animation-bundle identity outside this check.
- Why it blocks / does not: This concerns the script description only.
- Requested correction: Update the introductory comment to match the current action and offline scope.
- Requested evidence: Source inspection of the corrected header is sufficient.

#### Prior finding assessments

- **R12-F1** — `fix_rejected` — SetEnabled and PerformFailureCleanup now invalidate immediately after stopping the services. The reported behavioral test calls the console invalidator itself under both case labels. The requested execution through the stop transitions remains open.
- **R12-F10** — `fix_verified` — Sequence 167 identifies R11-F5 as optional and gives the corrected eight-blocking, one-optional breakdown. Sequence 149 and its execution totals are preserved.
- **R12-F2** — `fix_rejected` — A missing party now clears the context and the marker uses LoadedAreaState. With a party available across replacement, retirement of old bindings still depends on a presentation refresh.
- **R12-F3** — `fix_verified` — The binding now requires a nonempty ID, includes it when reusing the token, and checks the live ID again before removal. The candidate-bound changed-ID, missing-ID and rename cases passed. Confirmation captures the displayed name.
- **R12-F4** — `fix_rejected` — Changed facts, additional candidates and exceptions inside Execute now produce terminal records. The session read still precedes consumption, and a presentation exception after classification can skip reporting.
- **R12-F5** — `fix_verified` — Inconsistent and Unreadable now produce uncertain outcomes with guidance to check the horse before saving. The gate exercised neighbor removal and an unreadable post-state with one native call each. R13-F2 concerns the separate message for matching membership.
- **R12-F6** — `fix_rejected` — The single maintenance callback now has an accepted representation in source. The fixture sets both fields itself, and the supplied gate does not run the companion reader. R13-F1 identifies a separate defect in the new classifier.
- **R12-F7** — `fix_rejected` — Production and tests now share HorseTripRepairUpdater.Run, and the unreadable post-state and throwing Message getter were restored. The rewrite dropped the same-session object-replacement control and two distinguishing native-removal cases.
- **R12-F8** — `fix_verified` — Repeated closed-panel updates skip presentation discovery. The gate exercised on-demand observation and live revalidation at dispatch. The separate lifecycle consequence is recorded under R12-F2.
- **R12-F9** — `fix_rejected` — The focused wrapper describes the new action correctly. Line 12 of the native-contract header still describes the withdrawn written request.

#### Evidence assessment

- **Inspected:** Controller mission, OWNER AMENDMENT 1, developer report, transcript notice, prior-findings ledger, gate receipt and full gate log.; Repository validation instructions, feature diff, round-13 correction diff, repair-action and test-mapping records, and supplemental ledger entries 149 and 167.; Horse construction, candidate discovery, UMM intent, composition-root lifecycle, shared updater, removal classification, companion evidence producer and reader, and their focused tests. Compared the replacement tests with the controls at e04a342b and 68a19050.
- **Executed verification:** Read-only Git checks confirmed H, the specified merge base and all 34 target blob prefixes in the supplied feature diff. The mission, amendment and gate-log SHA-256 values matched their controller identities.; Controller receipt gate-focused-69623d76c1ed-d43d449b6c4-a3357fdbeb8-20261127T164151634698Z records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 37.5 seconds with the worktree unchanged. Components passed 1541/0, Validate-Source passed 153/0, and all 12 focused children passed.; The log records 51/0 offline Trip contract checks, including the stock Mammoth donor, Dog/Wolf controls, preserved bite and hoof limbs, sourced and modified facts, the single native removal boundary and absence of the withdrawn protocol. Mounted Shield passed 114/0, including byte identity of the embedded icon. UMM binding checks passed against 0.31.1 and 0.33.0.; The tested product DLL had SHA-256 14e5e790b622626d4beadd32550823eadcc4339c6e2e167190acae3da71fd232. The receipt closed at 309.79 of 360 validation minutes.
- **Source inspection only:** Both successful stop paths call InvalidateTripRepair. The executed component case invokes the console invalidator directly, and the compiled contract checks for the presence of the calls.; The closed-panel retention, pre-dispatch session-read failure, skipped report after a refresh failure, mixed-delegate classification and inaccurate panel messages are control-flow findings. They were not exercised by the supplied gate.; The companion-reader fixture constructs its panel fields by hand. Its source establishes the intended input shapes only.
- **Required gaps:** Cover a failing session lookup and a closed-panel session replacement through the shared updater. Check the terminal record, old-token rejection, report delivery and native invocation count.; Provide executable coverage of the production stop transitions and restore the same-session unit replacement, normal no-effect removal and inexact removal followed by a throw.; Provide candidate-bound output that connects the production callback observation to the companion reader, including a mixed callback and the historical no-panel observation.
- **Limitations:** The reviewer used read-only inspection. The controller withheld the mixed foreground transcript and supplied one candidate-bound gate receipt. The reported standalone companion-reader run cannot be confirmed from those records.; The gate is offline. UI appearance, native Linux/Unity-Mono execution, bite-hit gameplay and persistence through an actual save/load remain unobserved on H.

#### Summary

- **Delivered behavior:** Offline evidence supports the narrow fresh-horse filter, preserved bite and hoof construction, and exclusion of sourced or modified grants. Existing-horse repair now uses immutable UMM offers, explicit confirmation and a guarded exact-instance removal. The old file protocol is absent from the active path.
- **Scope reviewed:** Final source/offline review of PR-4 in draft PR #66 under OWNER AMENDMENT 1, covering MMR-06, S06, the S08 neighbors, all ten pending findings and the directly affected evidence reader.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Inspect the maintenance panel in the integrated alpha and exercise selection, cancellation and confirmation on distinguishable horses.; For N07, observe a successful plain-horse bite and damage without intrinsic Trip, with legitimate dog, wolf or independently granted maneuvers as available controls.; For N10, observe ordinary save/load after repair and loading an older save that still carries the fact. Actual gameplay and supported-platform UI behavior remain owner observations.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`.

Codex CLI `0.170.0`, thread `01a21de2-e2c4-70d0-b41f-d62fe9a95dee`, duration 951.9s, response sha256 `1d1c090c1dcab5869b3ce2294a53681ace6acf8861da4b325c152badbd446015`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r13-574a99`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "69623d76c1eda12f3d3fc50cc438cf81e93579c5",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The required Fast gate passed on the exact candidate. A failed session lookup can still leave a confirmed removal armed for a later update. A closed repair panel can also retain an old horse and its selectable token after the world changes. The companion evidence producer has a callback-classification defect, and its claimed producer-to-reader check lacks a candidate-bound execution record.\n\nContinuation for Claude\n\nTake ownership of a pending confirmation before reading the live session. Record a terminal outcome if that read fails, and report the stored outcome independently of presentation refresh. Retire old bindings on a session change even while the panel is closed. Keep expensive candidate scans on demand. Make the two player-facing messages identified in the findings describe only what the service observed.\n\nClassify the complete UMM callback and pass the production observation through the companion reader. Restore the independent unit-replacement and native-removal controls, and exercise the successful disable and failure-stop transitions through production code. Supply the required focused gate and candidate-bound output for the affected reader. Keep the correction within PR-4's confirmed in-game action, the existing world-write gate and the exact-removal boundary. OWNER AMENDMENT 1 keeps legacy files inert and assigns native UI and gameplay save/load observations to the integrated-alpha playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git checks confirmed H, the specified merge base and all 34 target blob prefixes in the supplied feature diff. The mission, amendment and gate-log SHA-256 values matched their controller identities.",
      "Controller receipt gate-focused-69623d76c1ed-d43d449b6c4-a3357fdbeb8-20261127T164151634698Z records Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at H. It exited 0 in 37.5 seconds with the worktree unchanged. Components passed 1541/0, Validate-Source passed 153/0, and all 12 focused children passed.",
      "The log records 51/0 offline Trip contract checks, including the stock Mammoth donor, Dog/Wolf controls, preserved bite and hoof limbs, sourced and modified facts, the single native removal boundary and absence of the withdrawn protocol. Mounted Shield passed 114/0, including byte identity of the embedded icon. UMM binding checks passed against 0.31.1 and 0.33.0.",
      "The tested product DLL had SHA-256 14e5e790b622626d4beadd32550823eadcc4339c6e2e167190acae3da71fd232. The receipt closed at 309.79 of 360 validation minutes."
    ],
    "inspected": [
      "Controller mission, OWNER AMENDMENT 1, developer report, transcript notice, prior-findings ledger, gate receipt and full gate log.",
      "Repository validation instructions, feature diff, round-13 correction diff, repair-action and test-mapping records, and supplemental ledger entries 149 and 167.",
      "Horse construction, candidate discovery, UMM intent, composition-root lifecycle, shared updater, removal classification, companion evidence producer and reader, and their focused tests. Compared the replacement tests with the controls at e04a342b and 68a19050."
    ],
    "limitations": [
      "The reviewer used read-only inspection. The controller withheld the mixed foreground transcript and supplied one candidate-bound gate receipt. The reported standalone companion-reader run cannot be confirmed from those records.",
      "The gate is offline. UI appearance, native Linux/Unity-Mono execution, bite-hit gameplay and persistence through an actual save/load remain unobserved on H."
    ],
    "required_gaps": [
      "Cover a failing session lookup and a closed-panel session replacement through the shared updater. Check the terminal record, old-token rejection, report delivery and native invocation count.",
      "Provide executable coverage of the production stop transitions and restore the same-session unit replacement, normal no-effect removal and inexact removal followed by a throw.",
      "Provide candidate-bound output that connects the production callback observation to the companion reader, including a mixed callback and the historical no-panel observation."
    ],
    "source_inspection_only": [
      "Both successful stop paths call InvalidateTripRepair. The executed component case invokes the console invalidator directly, and the compiled contract checks for the presence of the calls.",
      "The closed-panel retention, pre-dispatch session-read failure, skipped report after a refresh failure, mixed-delegate classification and inaccurate panel messages are control-flow findings. They were not exercised by the supplied gate.",
      "The companion-reader fixture constructs its panel fields by hand. Its source establishes the intended input shapes only."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Run calls host.SessionMarker() before Execute takes the confirmation. If that read throws, ProcessTripRepairConsole catches the exception, the prior result remains visible, and the next successful update can remove the fact under the same confirmation. At the other end of Run, a throwing LoadedHorses refresh skips host.Report even after Execute stored a removal.",
      "blocking_rationale": "A failed validation can defer a saved-horse mutation to a later frame without fresh confirmation. A separate presentation failure can suppress reporting of a completed action.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A failure to establish the session consumes the confirmation and records a refusal for the confirmed horse. An unrelated presentation refresh cannot prevent reporting an already classified action.",
      "id": "R12-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 184
      },
      "priority": "P1",
      "requested_correction": "Take the pending intent before the session lookup, classify lookup failure against its captured identity, and give the stored outcome a reporting path independent of presentation discovery.",
      "requested_evidence": "After a prior result for B, confirm A and inject a one-time SessionMarker failure. Show a refusal naming A, zero further removals and no execution on recovery. Separately fail presentation refresh after a valid removal and verify A's stored outcome reaches a working reporter exactly once.",
      "title": "A session lookup failure leaves a confirmed removal armed",
      "trigger": "The live session lookup throws on the first update after confirmation, then succeeds on the next update."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "With no armed confirmation, Execute returns immediately. Only Observe compares the marker and clears the console's bindings, and Run calls Observe only after a presentation request and successful roster read. The old offer, selected token and strong references to the old unit and fact can therefore survive repeated updates in the new world.",
      "blocking_rationale": "The maintenance surface can retain and offer a discarded horse. Dispatch's later marker check guards removal, but it does not release the outgoing objects or retire the stale selection at the lifecycle boundary.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A detected world replacement retires outgoing offers, selections and object references regardless of whether the panel is drawn.",
      "id": "R12-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 186
      },
      "priority": "P2",
      "requested_correction": "Process session invalidation independently of the presentation request and roster refresh. Preserve the terminal outcome for any confirmation being dispatched.",
      "requested_evidence": "Discover and select A, close the panel, replace the session while keeping a party available, and run an update without requesting presentation. Show empty old offers and rejection of the old token before reopening, then obtain a usable fresh offer.",
      "title": "World replacement leaves old horse bindings alive while the panel is closed",
      "trigger": "The session changes between party scans after the player has viewed or selected a horse and closed the panel."
    },
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "IsRepairMaintenancePanel compares callback.Method.Name and DeclaringType. For a multicast delegate, Method describes its last entry. An unrelated handler followed by Main.OnMaintenanceGui therefore yields optionsPanel=false and maintenancePanel=true, which the reader accepts.",
      "blocking_rationale": "The changed producer can certify the repair-only UI constraint while the entry also draws an unauthorized options surface.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only the exact single maintenance callback qualifies for the repair-only exception.",
      "id": "R13-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Diagnostics/Chunk7PersistenceScenario.CompanionNames.cs",
        "line": 601
      },
      "priority": "P2",
      "requested_correction": "Compare the complete delegate with the authorized callback, or check that its invocation list contains exactly that one handler.",
      "requested_evidence": "Feed both handler orders through the production classifier and reader. Both mixed callbacks should be refused and the single maintenance callback accepted.",
      "title": "A mixed UMM callback can be reported as maintenance-only",
      "trigger": "The entry's OnGUI invocation list contains an unrelated handler followed by the repair handler."
    },
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "The new fixture sets maintenancePanel and optionsPanel itself. It never calls the production observer. Test-Finalize5CompanionReaders.ps1 is also absent from the 12 executed children, and the supplied evidence contains no standalone reader receipt.",
      "blocking_rationale": "The requested producer-to-reader evidence is missing. The fixture would give the same verdict if the product emitted the wrong flags.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A candidate-bound offline check passes observations produced from actual delegates into Assert-KmcFinalize5CompanionNames and retains the historical control.",
      "id": "R12-F6",
      "location": {
        "file": "scripts/Test-Finalize5CompanionReaders.ps1",
        "line": 80
      },
      "priority": "P2",
      "requested_correction": "Use the production code that derives the panel fields in the reader fixture and supply its execution output for the corrected candidate.",
      "requested_evidence": "Show acceptance of the actual maintenance callback and historical no-panel observation, and refusal of an unrelated callback. Keep the name, active-state and Mounted Charge checks in effect.",
      "title": "The companion-reader check supplies the expected flags instead of observing the callback",
      "trigger": "The round-13 results are used to verify the companion-name evidence contract."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Both iterations of DisableAndFailureStop call host.Console.Invalidate() directly. Neither disables, re-enables or invokes failure cleanup. The compiled contract verifies that the root methods contain an invalidation call.",
      "blocking_rationale": "The source correction is present, but the behavioral evidence requested for revocation at the successful production stop boundary is still missing.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The selected confirmation is driven through each production stop transition before the next update.",
      "id": "R12-F1",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 193
      },
      "priority": "P2",
      "requested_correction": "Exercise the production lifecycle transition used by successful disable and failure cleanup with the external teardown boundary supplied by the offline fixture.",
      "requested_evidence": "Confirm A, complete each stop, re-enable before another repair update, and show zero removals, rejection of the old token and one removal after fresh confirmation.",
      "title": "The stop test bypasses both lifecycle paths it claims to exercise",
      "trigger": "The passing component case is cited as execution of disable/re-enable and failure-stop revocation."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Both replacement cases change the session marker. The previous same-session, same-ID replacement of the unit alone was removed. RemovalOutcomes also separates a native throw from over-removal and uses a throwing no-change case in place of the earlier normal no-effect return.",
      "blocking_rationale": "The marker check can make the replacement test pass even if live-object admission is broken. Two historical non-file removal controls also lost their distinguishing failure conditions.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The shared updater independently demonstrates live-object binding and retains the non-file removal classifications covered before the amendment.",
      "id": "R12-F7",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 170
      },
      "priority": "P2",
      "requested_correction": "Port the object-only replacement case from e04a342b and the normal no-effect and target-plus-neighbor-then-throw cases from the earlier suite into the shared updater tests. Align the mapping with the executed cases.",
      "requested_evidence": "Keep the marker and ID fixed, replace only the unit, and verify that both objects retain their facts. For each removal control assert its specific stored classification, one native call and no later replay.",
      "title": "The shared updater tests dropped independent identity and removal controls",
      "trigger": "The new test suite is used as the successor to the earlier live-object and exact-removal regressions."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "SameMultiset compares feature references. An in-place feature change is invisible to both snapshots, yet the panel tells the player that the horse was verified unchanged.",
      "blocking_rationale": "This gives the player a stronger assurance than the classifier established when deciding whether to save after a native failure.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The result states what was verified about the target and feature collection.",
      "id": "R13-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 151
      },
      "priority": "P2",
      "requested_correction": "Narrow the Unchanged message to the observed feature membership and continued presence of the target. The existing reference comparison can support that statement.",
      "requested_evidence": "Use the existing native-removal fixture to change a feature field in place and throw. Verify that the reported result describes membership accurately and that removal is invoked once.",
      "title": "The unchanged result overstates what the feature comparison verifies",
      "trigger": "A native call leaves the same feature instances in the collection after changing state on one of them."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Observe correctly makes the row ineligible, but DrawTripRepairGui prints \u201cMore than one copy\u201d for every ineligible offer. The same row has just reported one fact.",
      "blocking_rationale": "The repair correctly refuses the mutation, but gives a false account of the horse's facts and why the action is unavailable.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The panel identifies the missing stable identity as the reason for refusal and reserves the duplicate-copy explanation for multiple candidates.",
      "id": "R13-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs",
        "line": 200
      },
      "priority": "P3",
      "requested_correction": "Choose the refusal text from the offer's stable ID and candidate count.",
      "requested_evidence": "Check the presentation text for a single candidate with no stable ID and for two candidates on an identified horse. Both offers must stay unselectable.",
      "title": "A horse without a stable ID is incorrectly described as having duplicate facts",
      "trigger": "A loaded horse has one candidate and no stable ID, as exercised by StableIdAndRename."
    },
    {
      "acceptance_ids": [],
      "actual": "The focused wrapper was updated, but the native-contract header still says removal happens on the owner's written request. The next line also refers to an animation-bundle identity outside this check.",
      "blocking_rationale": "This concerns the script description only.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The active header describes the confirmed in-game maintenance action and the checks this script runs.",
      "id": "R12-F9",
      "location": {
        "file": "scripts/Test-MmrHorseTripNativeContract.ps1",
        "line": 12
      },
      "priority": "P3",
      "requested_correction": "Update the introductory comment to match the current action and offline scope.",
      "requested_evidence": "Source inspection of the corrected header is sufficient.",
      "title": "The native-contract header still describes a written repair request",
      "trigger": "A maintainer reads the active contract script to determine its scope."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F1",
      "rationale": "SetEnabled and PerformFailureCleanup now invalidate immediately after stopping the services. The reported behavioral test calls the console invalidator itself under both case labels. The requested execution through the stop transitions remains open."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F10",
      "rationale": "Sequence 167 identifies R11-F5 as optional and gives the corrected eight-blocking, one-optional breakdown. Sequence 149 and its execution totals are preserved."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F2",
      "rationale": "A missing party now clears the context and the marker uses LoadedAreaState. With a party available across replacement, retirement of old bindings still depends on a presentation refresh."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F3",
      "rationale": "The binding now requires a nonempty ID, includes it when reusing the token, and checks the live ID again before removal. The candidate-bound changed-ID, missing-ID and rename cases passed. Confirmation captures the displayed name."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F4",
      "rationale": "Changed facts, additional candidates and exceptions inside Execute now produce terminal records. The session read still precedes consumption, and a presentation exception after classification can skip reporting."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F5",
      "rationale": "Inconsistent and Unreadable now produce uncertain outcomes with guidance to check the horse before saving. The gate exercised neighbor removal and an unreadable post-state with one native call each. R13-F2 concerns the separate message for matching membership."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F6",
      "rationale": "The single maintenance callback now has an accepted representation in source. The fixture sets both fields itself, and the supplied gate does not run the companion reader. R13-F1 identifies a separate defect in the new classifier."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F7",
      "rationale": "Production and tests now share HorseTripRepairUpdater.Run, and the unreadable post-state and throwing Message getter were restored. The rewrite dropped the same-session object-replacement control and two distinguishing native-removal cases."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F8",
      "rationale": "Repeated closed-panel updates skip presentation discovery. The gate exercised on-demand observation and live revalidation at dispatch. The separate lifecycle consequence is recorded under R12-F2."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R12-F9",
      "rationale": "The focused wrapper describes the new action correctly. Line 12 of the native-contract header still describes the withdrawn written request."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r13-574a99",
  "round": 13,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Offline evidence supports the narrow fresh-horse filter, preserved bite and hoof construction, and exclusion of sourced or modified grants. Existing-horse repair now uses immutable UMM offers, explicit confirmation and a guarded exact-instance removal. The old file protocol is absent from the active path.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Inspect the maintenance panel in the integrated alpha and exercise selection, cancellation and confirmation on distinguishable horses.",
      "For N07, observe a successful plain-horse bite and damage without intrinsic Trip, with legitimate dog, wolf or independently granted maneuvers as available controls.",
      "For N10, observe ordinary save/load after repair and loading an older save that still carries the fact. Actual gameplay and supported-platform UI behavior remain owner observations."
    ],
    "scope_reviewed": "Final source/offline review of PR-4 in draft PR #66 under OWNER AMENDMENT 1, covering MMR-06, S06, the S08 neighbors, all ten pending findings and the directly affected evidence reader.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
