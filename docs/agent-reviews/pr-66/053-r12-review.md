<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r12-review part=1/1 sha256=14a0bb9bb78cca649cb2cc8a24ad6d43571d0a1e0a6ad0526a5c9ea52afbbeaf -->
### Codex GPT-6.2 Astra — review — round 12 — candidate `e04a342b92`

**Decision:** `changes_required` — The focused gate passed, and OWNER AMENDMENT 1 covers all nine prior findings about the retired file protocol. The replacement still permits consent to survive a disable or world boundary, omits the stable ID from its authorization check, and can lose or misreport a confirmed outcome. The new UMM callback also makes an existing evidence reader reject the product’s own observation.

Continuation for Claude

Correct the repair lifecycle as one change. Invalidate offers and consent at the actual service-stop and world-unload boundaries. At dispatch, check the current horse object, stable ID, feature instance, candidate count and write gate. Give each confirmation a terminal result when validation fails, and distinguish verified no-change from an unverifiable or inconsistent mutation in the player-facing message.

Exercise the production update and lifecycle wiring through an offline harness that substitutes the external game calls. Port the still-relevant removal regressions, and check the companion-name producer and reader against the authorized repair-only UMM surface. Run the required focused gate on the corrected candidate. Keep the correction within PR-4, preserve the verified new-horse filter and historical evidence, and keep legacy repair files inert. UI appearance and gameplay save/load remain assigned to the owner’s integrated-alpha playtest.

#### Findings

**R12-F1** · P1 · defect · blocks readiness — Disabling and re-enabling can execute an earlier confirmation
- Location: `src/KingmakerMountedCombat/CompositionRoot.cs:284`; acceptance: MMR-06, S06
- Trigger: A horse is confirmed, and the mod is disabled and re-enabled before another repair update runs.
- Expected: A successful disable invalidates selected and confirmed tokens immediately. Re-enabling requires a fresh selection and confirmation.
- Actual: SetEnabled(false) clears IsEnabled without invalidating the repair console. PerformFailureCleanup does the same. Only service disposal calls InvalidateTripRepair. On re-enable, the same player, horse and fact retain their binding, so the previously confirmed removal can run. GatesRefuse calls Console.Invalidate directly and misses this path.
- Why it blocks / does not: A saved-horse mutation can run under authorization that the owner explicitly required disabling the mod to revoke.
- Requested correction: Connect repair invalidation to successful disable and to the failure-cleanup path that stops services. Preserve the existing teardown refusal rules.
- Requested evidence: Confirm a repair, disable and re-enable through the production lifecycle with no update in between, then update. Verify zero removals, rejection of the old token and one removal after fresh confirmation. Cover the failure-induced stop as well.

**R12-F2** · P2 · defect · blocks readiness — World invalidation depends on observing a different player or area during a party update
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs:318`; acceptance: MMR-06, S06
- Trigger: The current party becomes unavailable, or an area unload and re-entry occur between successful party scans.
- Expected: Unloading or discarding a world retires its offers and consent even when no party update runs during the transition.
- Actual: A missing party returns before ProcessTripRepairConsole can clear its offers, bindings or cached Player reference. The session marker otherwise compares only Player and CurrentlyLoadedArea references. A new visit to the same area blueprint with carried party objects can retain the old token when no intervening repair pass observes a different marker.
- Why it blocks / does not: The UI can still offer a horse from a discarded world, and authorization can cross a relevant area replacement.
- Requested correction: Invalidate the repair context at an actual unload or world-discard boundary and clear it when the party is unavailable. Release the outgoing player and horse references.
- Requested evidence: Exercise loss of the party and an unload/re-entry using the same area blueprint and carried horse. Show empty offers while the world is unavailable, rejection of the old token and a usable fresh offer after settlement.

**R12-F3** · P2 · defect · blocks readiness — The stable horse ID is displayed and recorded but never validated
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:189`; acceptance: MMR-06, S06
- Trigger: The service observes the same unit and fact with a different or missing stable ID.
- Expected: The confirmed horse has a nonempty stable ID that still matches the live object at dispatch. The result identifies the horse the player confirmed.
- Actual: Observe reuses a binding by unit reference, fact reference and epoch alone. The offer displays the latest horse.Id, while the binding keeps the first Id. Execute never reads the ID again. An absent ID is also eligible. A normal companion rename exposes a related mismatch because the offer shows the current name while the outcome uses the name cached at first discovery.
- Why it blocks / does not: The stable identity presented to the player is not one of the conditions that authorizes removal.
- Requested correction: Require the stable ID when creating and retaining an eligible binding, and compare it with the current horse ID immediately before removal. Capture the presentation name used for confirmation.
- Requested evidence: Keep the session, unit and fact references fixed while the observed ID changes or becomes unavailable. The old token must authorize zero removals. Include an ordinary rename with the same ID and check the horse identified in the result.

**R12-F4** · P2 · defect · blocks readiness — Refreshing offers can erase a confirmed action before its outcome is recorded
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs:134`; acceptance: MMR-06, S06
- Trigger: The candidate changes or a live eligibility read fails on the update after confirmation.
- Expected: The ordinary update consumes the confirmation once and records a stale, refused or not-attempted result when it cannot validate that action.
- Actual: The service calls Observe before Execute. When the confirmed fact disappears, becomes sourced or gains a second candidate, Observe clears confirmed, so Execute returns null and LastRecord can still describe an earlier horse. A snapshot or gate exception before Execute leaves consent armed. A lookup exception inside Execute consumes it without storing a record. The changed-fact test calls Execute directly and bypasses the first behavior.
- Why it blocks / does not: After a deliberate confirmation, the player can receive a previous result or a failure log followed by a later removal without another confirmation.
- Requested correction: Take ownership of the pending confirmation before fallible refresh and validation. Preserve enough identity to record why execution was refused, then publish the stored result.
- Requested evidence: Use the ordinary update sequence after confirming A with an earlier result for B. Replace A’s fact, add a second candidate, and inject a discovery or eligibility-read failure. Check the new record names A, zero removals occur and later updates cannot execute that consent.

**R12-F5** · P2 · defect · blocks readiness — An uncertain or inexact mutation is reported as “not removed”
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:250`; acceptance: MMR-06, S06
- Trigger: Native removal changes more than the requested fact, or the post-removal collection cannot be read.
- Expected: The result distinguishes a verified unchanged collection from an unexpected change or an outcome that could not be verified.
- Actual: Every outcome except Removed becomes `not-removed:<outcome>`, which the GUI displays directly. In the existing neighbor-removal case, the target and neighbor are both gone but the player sees `not-removed:Inconsistent`. A failed post-removal read likewise produces `not-removed:Unreadable` even if the target was removed.
- Why it blocks / does not: The owner can decide whether to save based on a false statement about a mutation that may already have happened.
- Requested correction: Map Inconsistent and Unreadable to truthful terminal messages. Keep the exact-removal count and one-shot behavior.
- Requested evidence: Assert the stored and player-facing result after target-plus-neighbor removal and after an unreadable post-state. Verify one native invocation and retain exact-removal and verified-no-change controls.

**R12-F6** · P2 · defect · blocks readiness — The companion-name evidence reader rejects the authorized repair panel
- Location: `scripts/runtime/Finalize5CompanionEvidence.ps1:181`; acceptance: MMR-06
- Trigger: The existing `companion-names-tack-manual` producer captures the UMM entry with this candidate loaded.
- Expected: The evidence contract accepts the repair-only UMM surface authorized by OWNER AMENDMENT 1 and continues to detect an unexpected options surface.
- Actual: Main now always assigns OnMaintenanceGui. Chunk7PersistenceScenario.CompanionNames.cs line 334 consequently emits `optionsPanel=true`, and Assert-KmcFinalize5CompanionNames rejects that value. Its existing negative fixture confirms the conflict.
- Why it blocks / does not: The product’s own companion-name evidence will deterministically fail its reader when this candidate exercises that scenario.
- Requested correction: Make the producer and reader distinguish the authorized repair callback from an arbitrary OnGUI callback. Preserve historical observations and the existing name, active-state and Mounted Charge checks.
- Requested evidence: Run focused producer-to-reader cases for the actual maintenance callback, an unrelated callback and the historical no-panel observation. A native campaign is unnecessary for this contract check.

**R12-F7** · P2 · evidence_gap · blocks readiness — The connected test substitutes the service wiring that governs consent
- Location: `tests/HorseTripRepairPolicyTests.cs:81`; acceptance: MMR-06, S06
- Trigger: The round-12 component results are used to establish the replacement action’s end-to-end behavior.
- Expected: An offline case drives the production orchestration from discovery and confirmation through gate evaluation, removal and recorded reporting, with simulation at the external game boundary.
- Actual: Rig.Update reconstructs the service’s orchestration with a fixture session, roster, gate boolean and logger. Its disabled case invokes Invalidate itself. The compiled contract inspects method calls but does not execute that wiring. The test mapping also names RemovalThrowing as successor to cases it never injects, including an unreadable post-state and a native exception whose Message getter throws.
- Why it blocks / does not: The owner required connected production-path evidence and preservation of applicable regressions. The passing fixture bypasses the lifecycle and reporting paths where this review found defects.
- Requested correction: Drive the service’s actual update/lifecycle path or an updater shared with it. Carry forward the non-file failure injections and correct the amendment mapping to identify the cases actually exercised.
- Requested evidence: Supply a candidate-bound gate showing connected two-horse selection/cancellation, real lifecycle gate inputs, stale identity/refusal, exact and uncertain removal, reporting failure and inert legacy files. Include the throwing Message getter and failed post-removal read. Keep UI appearance and gameplay save/load labeled owner-pending.

**R12-F8** · P3 · optional_polish · non-blocking — The repair panel rebuilds its snapshots every frame while closed
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs:134`; acceptance: —
- Trigger: The player continues ordinary play with a party horse and no repair in progress.
- Expected: An idle maintenance surface can reuse its presentation snapshot while retaining fresh validation at dispatch.
- Actual: ProcessTripRepairConsole enumerates the feature collection for each loaded horse and creates new lists, offers and a read-only wrapper on ordinary updates, including while the panel is closed.
- Why it blocks / does not: The recurring work is visible in source. Its gameplay cost was not measured and does not determine readiness.
- Requested correction: Consider refreshing presentation on demand or at a bounded cadence. Keep lifecycle invalidation immediate and re-read the live candidate before removal.
- Requested evidence: If changed, show that an idle closed panel avoids repeated feature scans and that opening it or confirming still uses current data.

**R12-F9** · P3 · optional_polish · non-blocking — The focused gate still advertises tests of the retired file protocol
- Location: `scripts/Test-MmrHorseTripFocused.ps1:7`; acceptance: —
- Trigger: A maintainer reads the focused wrapper to determine its validation scope.
- Expected: The active gate descriptions identify the confirmed in-game action and the boundary exercised offline.
- Actual: The focused wrapper says the component runner exercises claim, publication and archive failures on real folders. The native-contract header still describes a written request.
- Why it blocks / does not: The executable selection and controller receipt identify the checks that actually ran.
- Requested correction: Update the active script comments to describe the current action and tests. Preserve the frozen mission and historical records.
- Requested evidence: Source inspection of the two gate descriptions is sufficient.

**R12-F10** · P3 · optional_polish · non-blocking — The round-11 ledger excerpt counts an optional comment as blocking
- Location: `docs/missions/mmr-split/PR-4-ledger-excerpt.md:213`; acceptance: —
- Trigger: The new ledger excerpt is consulted for the round-11 stop.
- Expected: The supplemental stop record distinguishes the eight blocking findings from the one optional finding.
- Actual: Sequence 149 lists R11-F5 in open_blocking. The controller ledger records it as optional polish with blocks_readiness=false.
- Why it blocks / does not: The round-11 verdict, cap stop and charged execution agree. The discrepancy concerns one finding’s classification in the supplemental record.
- Requested correction: Append a scoped clarification to the supplemental ledger and regenerate its excerpt, preserving sequence 149.
- Requested evidence: The clarification should cite the controller’s R11-F5 disposition and retain the existing execution totals.

#### Prior finding assessments

- **R10-F2** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew filesystem submission and its writer-completion requirement. Production has no request reader. The replacement requires selection and confirmation of one offer, and the two-horse and cancellation controls exercised that decision.
- **R10-F4** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew persistent claim recovery. No production scan or recovery move can consume a settled or retiring entry. Legacy entries are inert, and a fresh console starts without authorization.
- **R10-F5** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew publication of a private repair archive. The replacement creates no terminal file whose effective mode must be observed. Its result is held in memory for reporting.
- **R11-F1** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew file submission and explicitly required removal of its platform lease machinery. The production repair uses neither F_SETLEASE nor a signal handler, eliminating the lease-break trigger.
- **R11-F2** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew archive publication and its retry queue. A missing or resized terminal file has no input to the replacement action. The repeated-update control exercised one native removal for one confirmation.
- **R11-F3** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew retained archive descriptors and on-disk recovery at unload. The product owns no such descriptor, and successful horse-service disposal invalidates its console. R12-F1 concerns the distinct UMM disable path.
- **R11-F4** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew ordered recovery of stranded file claims. The replacement has no recovery queue to overtake. The two-horse control showed that switching offers and confirming B removed only B’s fact.
- **R11-F5** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew the Unix fallback publisher. Its obsolete permission and cleanup comment was removed with the implementation, so the original optional correction has no active target.
- **R5-F1** — `rebuttal_accepted` — OWNER AMENDMENT 1 withdrew publication of the request payload under a terminal archive name. The replacement exposes no partial archive. The exact-removal and logger-failure controls exercised a single native invocation without republishing or replaying a file.

#### Evidence assessment

- **Inspected:** The fixed mission, OWNER AMENDMENT 1, tracked owner authorization, developer report, pending findings ledger, feature diff and round-11-to-round-12 correction.; AGENTS.md, docs/VALIDATION-POLICY.md, CURRENT.md and AUTONOMOUS-RESUME.md.; The UMM callbacks, composition-root enable and failure paths, horse discovery and dispatch, world-boundary callbacks, removal classifier, test mapping, and companion-name evidence producer and reader.
- **Executed verification:** The controller receipt binds `scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1` to e04a342b9214b502e48b19de28d090ed0855aeb0. It records exit 0 in 39.2 seconds and an unchanged worktree. The supplied log SHA-256 matches the receipt.; The log records a successful build, 1,537 component checks with zero failures, 153 source checks with zero failures, 50 Trip contract checks with zero failures and 12 of 12 focused children passing. The shared budget closed at 308.37 of 360 minutes.; The gate exercised the inherited-fact filter and candidate predicate on detached Kingmaker objects, checked the bite and two hoof limbs, and verified Mounted Shield icon identity in the compiled output. The component runner exercised the production console and removal classifier with simulated horses, facts and removal calls.
- **Source inspection only:** The active product has no repair request reader, claim, archive publisher, recovery scan, Unix lease or retained archive descriptor. The repair GUI reads snapshots and records intent. Native feature removal is reached through the service update.; The disable, world-boundary, stable-ID and outcome defects below follow from production call order and state ownership. The supplied gate did not execute those counterexamples through the service.; The companion-name producer derives optionsPanel from whether OnGUI is non-null. The existing reader requires false.
- **Required gaps:** Demonstrate invalidation through the production disable and world-boundary paths, including a disable/re-enable with no intervening update.; Demonstrate live stable-ID binding and truthful terminal outcomes through the same update sequence used by the service, including stale candidates and read failures.; Restore the applicable non-file removal regressions and verify that the companion-name evidence reader accepts the authorized maintenance callback while rejecting an unrelated options callback.
- **Limitations:** The foreground developer transcript was withheld. Executed validation is attributed to the controller receipt and its log. The reviewer performed read-only inspection.; The gate reports nativeProof=false. UI interaction, a gameplay bite, save/load persistence and execution under Linux Unity/Mono were not observed. The historical Linux probe exercised the retired file mechanism.

#### Summary

- **Delivered behavior:** Fresh KMC horse construction filters the exact stock Mammoth TrippingBite grant and preserves the bite, hooves and neighboring facts. Existing horses have a repair-only UMM surface showing horse identity, owner context and the ambiguity of sourceless provenance. Legacy repair files have no active consumer. The one-shot happy path works in the offline console tests.
- **Scope reviewed:** Final PR-4 source/offline checkpoint for MMR-06 at the fixed candidate under OWNER AMENDMENT 1, including the nine pending prior findings, replacement repair action, affected evidence consumers and required regression gate.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** Assess the repair panel’s clarity and the confirmation interaction in the integrated private alpha.; Observe N07 in gameplay, including a successful plain-horse bite and damage without intrinsic Trip and a legitimate maneuver control where available.; Observe N10 on an existing affected horse through repair, ordinary save/load and area transition, including preserved neighboring effects and fresh confirmation when loading an older affected save.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 1, sha256 `f9410bf809e35a83f8293bb5b54cfd344594aa0c2e5b90e633468c216e761b28`.

Codex CLI `0.170.0`, thread `01a21dcd-0a22-7d91-9721-7f7903dba25f`, duration 931.2s, response sha256 `7efea459e5e649a7eec394aedb4225e19f1799cdd2481970b64771c123b9f276`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r12-83bcd5`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "e04a342b9214b502e48b19de28d090ed0855aeb0",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed, and OWNER AMENDMENT 1 covers all nine prior findings about the retired file protocol. The replacement still permits consent to survive a disable or world boundary, omits the stable ID from its authorization check, and can lose or misreport a confirmed outcome. The new UMM callback also makes an existing evidence reader reject the product\u2019s own observation.\n\nContinuation for Claude\n\nCorrect the repair lifecycle as one change. Invalidate offers and consent at the actual service-stop and world-unload boundaries. At dispatch, check the current horse object, stable ID, feature instance, candidate count and write gate. Give each confirmation a terminal result when validation fails, and distinguish verified no-change from an unverifiable or inconsistent mutation in the player-facing message.\n\nExercise the production update and lifecycle wiring through an offline harness that substitutes the external game calls. Port the still-relevant removal regressions, and check the companion-name producer and reader against the authorized repair-only UMM surface. Run the required focused gate on the corrected candidate. Keep the correction within PR-4, preserve the verified new-horse filter and historical evidence, and keep legacy repair files inert. UI appearance and gameplay save/load remain assigned to the owner\u2019s integrated-alpha playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds `scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1` to e04a342b9214b502e48b19de28d090ed0855aeb0. It records exit 0 in 39.2 seconds and an unchanged worktree. The supplied log SHA-256 matches the receipt.",
      "The log records a successful build, 1,537 component checks with zero failures, 153 source checks with zero failures, 50 Trip contract checks with zero failures and 12 of 12 focused children passing. The shared budget closed at 308.37 of 360 minutes.",
      "The gate exercised the inherited-fact filter and candidate predicate on detached Kingmaker objects, checked the bite and two hoof limbs, and verified Mounted Shield icon identity in the compiled output. The component runner exercised the production console and removal classifier with simulated horses, facts and removal calls."
    ],
    "inspected": [
      "The fixed mission, OWNER AMENDMENT 1, tracked owner authorization, developer report, pending findings ledger, feature diff and round-11-to-round-12 correction.",
      "AGENTS.md, docs/VALIDATION-POLICY.md, CURRENT.md and AUTONOMOUS-RESUME.md.",
      "The UMM callbacks, composition-root enable and failure paths, horse discovery and dispatch, world-boundary callbacks, removal classifier, test mapping, and companion-name evidence producer and reader."
    ],
    "limitations": [
      "The foreground developer transcript was withheld. Executed validation is attributed to the controller receipt and its log. The reviewer performed read-only inspection.",
      "The gate reports nativeProof=false. UI interaction, a gameplay bite, save/load persistence and execution under Linux Unity/Mono were not observed. The historical Linux probe exercised the retired file mechanism."
    ],
    "required_gaps": [
      "Demonstrate invalidation through the production disable and world-boundary paths, including a disable/re-enable with no intervening update.",
      "Demonstrate live stable-ID binding and truthful terminal outcomes through the same update sequence used by the service, including stale candidates and read failures.",
      "Restore the applicable non-file removal regressions and verify that the companion-name evidence reader accepts the authorized maintenance callback while rejecting an unrelated options callback."
    ],
    "source_inspection_only": [
      "The active product has no repair request reader, claim, archive publisher, recovery scan, Unix lease or retained archive descriptor. The repair GUI reads snapshots and records intent. Native feature removal is reached through the service update.",
      "The disable, world-boundary, stable-ID and outcome defects below follow from production call order and state ownership. The supplied gate did not execute those counterexamples through the service.",
      "The companion-name producer derives optionsPanel from whether OnGUI is non-null. The existing reader requires false."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "SetEnabled(false) clears IsEnabled without invalidating the repair console. PerformFailureCleanup does the same. Only service disposal calls InvalidateTripRepair. On re-enable, the same player, horse and fact retain their binding, so the previously confirmed removal can run. GatesRefuse calls Console.Invalidate directly and misses this path.",
      "blocking_rationale": "A saved-horse mutation can run under authorization that the owner explicitly required disabling the mod to revoke.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A successful disable invalidates selected and confirmed tokens immediately. Re-enabling requires a fresh selection and confirmation.",
      "id": "R12-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/CompositionRoot.cs",
        "line": 284
      },
      "priority": "P1",
      "requested_correction": "Connect repair invalidation to successful disable and to the failure-cleanup path that stops services. Preserve the existing teardown refusal rules.",
      "requested_evidence": "Confirm a repair, disable and re-enable through the production lifecycle with no update in between, then update. Verify zero removals, rejection of the old token and one removal after fresh confirmation. Cover the failure-induced stop as well.",
      "title": "Disabling and re-enabling can execute an earlier confirmation",
      "trigger": "A horse is confirmed, and the mod is disabled and re-enabled before another repair update runs."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "A missing party returns before ProcessTripRepairConsole can clear its offers, bindings or cached Player reference. The session marker otherwise compares only Player and CurrentlyLoadedArea references. A new visit to the same area blueprint with carried party objects can retain the old token when no intervening repair pass observes a different marker.",
      "blocking_rationale": "The UI can still offer a horse from a discarded world, and authorization can cross a relevant area replacement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Unloading or discarding a world retires its offers and consent even when no party update runs during the transition.",
      "id": "R12-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.cs",
        "line": 318
      },
      "priority": "P2",
      "requested_correction": "Invalidate the repair context at an actual unload or world-discard boundary and clear it when the party is unavailable. Release the outgoing player and horse references.",
      "requested_evidence": "Exercise loss of the party and an unload/re-entry using the same area blueprint and carried horse. Show empty offers while the world is unavailable, rejection of the old token and a usable fresh offer after settlement.",
      "title": "World invalidation depends on observing a different player or area during a party update",
      "trigger": "The current party becomes unavailable, or an area unload and re-entry occur between successful party scans."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Observe reuses a binding by unit reference, fact reference and epoch alone. The offer displays the latest horse.Id, while the binding keeps the first Id. Execute never reads the ID again. An absent ID is also eligible. A normal companion rename exposes a related mismatch because the offer shows the current name while the outcome uses the name cached at first discovery.",
      "blocking_rationale": "The stable identity presented to the player is not one of the conditions that authorizes removal.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The confirmed horse has a nonempty stable ID that still matches the live object at dispatch. The result identifies the horse the player confirmed.",
      "id": "R12-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 189
      },
      "priority": "P2",
      "requested_correction": "Require the stable ID when creating and retaining an eligible binding, and compare it with the current horse ID immediately before removal. Capture the presentation name used for confirmation.",
      "requested_evidence": "Keep the session, unit and fact references fixed while the observed ID changes or becomes unavailable. The old token must authorize zero removals. Include an ordinary rename with the same ID and check the horse identified in the result.",
      "title": "The stable horse ID is displayed and recorded but never validated",
      "trigger": "The service observes the same unit and fact with a different or missing stable ID."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "The service calls Observe before Execute. When the confirmed fact disappears, becomes sourced or gains a second candidate, Observe clears confirmed, so Execute returns null and LastRecord can still describe an earlier horse. A snapshot or gate exception before Execute leaves consent armed. A lookup exception inside Execute consumes it without storing a record. The changed-fact test calls Execute directly and bypasses the first behavior.",
      "blocking_rationale": "After a deliberate confirmation, the player can receive a previous result or a failure log followed by a later removal without another confirmation.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The ordinary update consumes the confirmation once and records a stale, refused or not-attempted result when it cannot validate that action.",
      "id": "R12-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs",
        "line": 134
      },
      "priority": "P2",
      "requested_correction": "Take ownership of the pending confirmation before fallible refresh and validation. Preserve enough identity to record why execution was refused, then publish the stored result.",
      "requested_evidence": "Use the ordinary update sequence after confirming A with an earlier result for B. Replace A\u2019s fact, add a second candidate, and inject a discovery or eligibility-read failure. Check the new record names A, zero removals occur and later updates cannot execute that consent.",
      "title": "Refreshing offers can erase a confirmed action before its outcome is recorded",
      "trigger": "The candidate changes or a live eligibility read fails on the update after confirmation."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Every outcome except Removed becomes `not-removed:<outcome>`, which the GUI displays directly. In the existing neighbor-removal case, the target and neighbor are both gone but the player sees `not-removed:Inconsistent`. A failed post-removal read likewise produces `not-removed:Unreadable` even if the target was removed.",
      "blocking_rationale": "The owner can decide whether to save based on a false statement about a mutation that may already have happened.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The result distinguishes a verified unchanged collection from an unexpected change or an outcome that could not be verified.",
      "id": "R12-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 250
      },
      "priority": "P2",
      "requested_correction": "Map Inconsistent and Unreadable to truthful terminal messages. Keep the exact-removal count and one-shot behavior.",
      "requested_evidence": "Assert the stored and player-facing result after target-plus-neighbor removal and after an unreadable post-state. Verify one native invocation and retain exact-removal and verified-no-change controls.",
      "title": "An uncertain or inexact mutation is reported as \u201cnot removed\u201d",
      "trigger": "Native removal changes more than the requested fact, or the post-removal collection cannot be read."
    },
    {
      "acceptance_ids": [
        "MMR-06"
      ],
      "actual": "Main now always assigns OnMaintenanceGui. Chunk7PersistenceScenario.CompanionNames.cs line 334 consequently emits `optionsPanel=true`, and Assert-KmcFinalize5CompanionNames rejects that value. Its existing negative fixture confirms the conflict.",
      "blocking_rationale": "The product\u2019s own companion-name evidence will deterministically fail its reader when this candidate exercises that scenario.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The evidence contract accepts the repair-only UMM surface authorized by OWNER AMENDMENT 1 and continues to detect an unexpected options surface.",
      "id": "R12-F6",
      "location": {
        "file": "scripts/runtime/Finalize5CompanionEvidence.ps1",
        "line": 181
      },
      "priority": "P2",
      "requested_correction": "Make the producer and reader distinguish the authorized repair callback from an arbitrary OnGUI callback. Preserve historical observations and the existing name, active-state and Mounted Charge checks.",
      "requested_evidence": "Run focused producer-to-reader cases for the actual maintenance callback, an unrelated callback and the historical no-panel observation. A native campaign is unnecessary for this contract check.",
      "title": "The companion-name evidence reader rejects the authorized repair panel",
      "trigger": "The existing `companion-names-tack-manual` producer captures the UMM entry with this candidate loaded."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "Rig.Update reconstructs the service\u2019s orchestration with a fixture session, roster, gate boolean and logger. Its disabled case invokes Invalidate itself. The compiled contract inspects method calls but does not execute that wiring. The test mapping also names RemovalThrowing as successor to cases it never injects, including an unreadable post-state and a native exception whose Message getter throws.",
      "blocking_rationale": "The owner required connected production-path evidence and preservation of applicable regressions. The passing fixture bypasses the lifecycle and reporting paths where this review found defects.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "An offline case drives the production orchestration from discovery and confirmation through gate evaluation, removal and recorded reporting, with simulation at the external game boundary.",
      "id": "R12-F7",
      "location": {
        "file": "tests/HorseTripRepairPolicyTests.cs",
        "line": 81
      },
      "priority": "P2",
      "requested_correction": "Drive the service\u2019s actual update/lifecycle path or an updater shared with it. Carry forward the non-file failure injections and correct the amendment mapping to identify the cases actually exercised.",
      "requested_evidence": "Supply a candidate-bound gate showing connected two-horse selection/cancellation, real lifecycle gate inputs, stale identity/refusal, exact and uncertain removal, reporting failure and inert legacy files. Include the throwing Message getter and failed post-removal read. Keep UI appearance and gameplay save/load labeled owner-pending.",
      "title": "The connected test substitutes the service wiring that governs consent",
      "trigger": "The round-12 component results are used to establish the replacement action\u2019s end-to-end behavior."
    },
    {
      "acceptance_ids": [],
      "actual": "ProcessTripRepairConsole enumerates the feature collection for each loaded horse and creates new lists, offers and a read-only wrapper on ordinary updates, including while the panel is closed.",
      "blocking_rationale": "The recurring work is visible in source. Its gameplay cost was not measured and does not determine readiness.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "An idle maintenance surface can reuse its presentation snapshot while retaining fresh validation at dispatch.",
      "id": "R12-F8",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs",
        "line": 134
      },
      "priority": "P3",
      "requested_correction": "Consider refreshing presentation on demand or at a bounded cadence. Keep lifecycle invalidation immediate and re-read the live candidate before removal.",
      "requested_evidence": "If changed, show that an idle closed panel avoids repeated feature scans and that opening it or confirming still uses current data.",
      "title": "The repair panel rebuilds its snapshots every frame while closed",
      "trigger": "The player continues ordinary play with a party horse and no repair in progress."
    },
    {
      "acceptance_ids": [],
      "actual": "The focused wrapper says the component runner exercises claim, publication and archive failures on real folders. The native-contract header still describes a written request.",
      "blocking_rationale": "The executable selection and controller receipt identify the checks that actually ran.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The active gate descriptions identify the confirmed in-game action and the boundary exercised offline.",
      "id": "R12-F9",
      "location": {
        "file": "scripts/Test-MmrHorseTripFocused.ps1",
        "line": 7
      },
      "priority": "P3",
      "requested_correction": "Update the active script comments to describe the current action and tests. Preserve the frozen mission and historical records.",
      "requested_evidence": "Source inspection of the two gate descriptions is sufficient.",
      "title": "The focused gate still advertises tests of the retired file protocol",
      "trigger": "A maintainer reads the focused wrapper to determine its validation scope."
    },
    {
      "acceptance_ids": [],
      "actual": "Sequence 149 lists R11-F5 in open_blocking. The controller ledger records it as optional polish with blocks_readiness=false.",
      "blocking_rationale": "The round-11 verdict, cap stop and charged execution agree. The discrepancy concerns one finding\u2019s classification in the supplemental record.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The supplemental stop record distinguishes the eight blocking findings from the one optional finding.",
      "id": "R12-F10",
      "location": {
        "file": "docs/missions/mmr-split/PR-4-ledger-excerpt.md",
        "line": 213
      },
      "priority": "P3",
      "requested_correction": "Append a scoped clarification to the supplemental ledger and regenerate its excerpt, preserving sequence 149.",
      "requested_evidence": "The clarification should cite the controller\u2019s R11-F5 disposition and retain the existing execution totals.",
      "title": "The round-11 ledger excerpt counts an optional comment as blocking",
      "trigger": "The new ledger excerpt is consulted for the round-11 stop."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R10-F2",
      "rationale": "OWNER AMENDMENT 1 withdrew filesystem submission and its writer-completion requirement. Production has no request reader. The replacement requires selection and confirmation of one offer, and the two-horse and cancellation controls exercised that decision."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R10-F4",
      "rationale": "OWNER AMENDMENT 1 withdrew persistent claim recovery. No production scan or recovery move can consume a settled or retiring entry. Legacy entries are inert, and a fresh console starts without authorization."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R10-F5",
      "rationale": "OWNER AMENDMENT 1 withdrew publication of a private repair archive. The replacement creates no terminal file whose effective mode must be observed. Its result is held in memory for reporting."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R11-F1",
      "rationale": "OWNER AMENDMENT 1 withdrew file submission and explicitly required removal of its platform lease machinery. The production repair uses neither F_SETLEASE nor a signal handler, eliminating the lease-break trigger."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R11-F2",
      "rationale": "OWNER AMENDMENT 1 withdrew archive publication and its retry queue. A missing or resized terminal file has no input to the replacement action. The repeated-update control exercised one native removal for one confirmation."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R11-F3",
      "rationale": "OWNER AMENDMENT 1 withdrew retained archive descriptors and on-disk recovery at unload. The product owns no such descriptor, and successful horse-service disposal invalidates its console. R12-F1 concerns the distinct UMM disable path."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R11-F4",
      "rationale": "OWNER AMENDMENT 1 withdrew ordered recovery of stranded file claims. The replacement has no recovery queue to overtake. The two-horse control showed that switching offers and confirming B removed only B\u2019s fact."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R11-F5",
      "rationale": "OWNER AMENDMENT 1 withdrew the Unix fallback publisher. Its obsolete permission and cleanup comment was removed with the implementation, so the original optional correction has no active target."
    },
    {
      "assessment": "rebuttal_accepted",
      "finding_id": "R5-F1",
      "rationale": "OWNER AMENDMENT 1 withdrew publication of the request payload under a terminal archive name. The replacement exposes no partial archive. The exact-removal and logger-failure controls exercised a single native invocation without republishing or replaying a file."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r12-83bcd5",
  "round": 12,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Fresh KMC horse construction filters the exact stock Mammoth TrippingBite grant and preserves the bite, hooves and neighboring facts. Existing horses have a repair-only UMM surface showing horse identity, owner context and the ambiguity of sourceless provenance. Legacy repair files have no active consumer. The one-shot happy path works in the offline console tests.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Assess the repair panel\u2019s clarity and the confirmation interaction in the integrated private alpha.",
      "Observe N07 in gameplay, including a successful plain-horse bite and damage without intrinsic Trip and a legitimate maneuver control where available.",
      "Observe N10 on an existing affected horse through repair, ordinary save/load and area transition, including preserved neighboring effects and fresh confirmation when loading an older affected save."
    ],
    "scope_reviewed": "Final PR-4 source/offline checkpoint for MMR-06 at the fixed candidate under OWNER AMENDMENT 1, including the nine pending prior findings, replacement repair action, affected evidence consumers and required regression gate.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
