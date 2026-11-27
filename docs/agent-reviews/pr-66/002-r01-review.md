<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-trip-20261125213249-4b8b key=r01-review part=1/1 sha256=e4a89e2303d33e96c934435413f35f3cdb0a47d1298761812deb6bd050b56791 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `8fc887ad2c`

**Decision:** `changes_required` — The candidate passes the required focused gate and fixes the carried R7-F5 archive-retry case. Two other paths still put owner state at risk. A guarded fixture run can consume a repair file from the player's profile, and a failed verification after native removal can allow the same request to remove a fact again.

## Continuation for Claude

Limit the correction to PR-4. Resolve the repair directory from the actual run context before accessing any request files. Require the evidenced stock component shape before removing a whole feature. Retain the outcome of a removal attempt across verification failures, bind decisions and archives to the claimed bytes, and handle a newer request arriving during the return rename. Use the existing real-folder host and detached Kingmaker objects to exercise the cases in the findings. Correct the offline output and reconcile the pre-review limits. Batch the changes into one candidate and run the enrolled Fast focused gate within the applicable allowance. N07 and N10 belong to the owner's playtest.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — A guarded Working run can consume the player's repair file
- Location: `src/KingmakerMountedCombat/CompositionRoot.cs:81`; acceptance: MMR-06, S06
- Trigger: A guarded scenario such as fixture-intake loads its Working party without binding NativePersistenceIsolation while the player's profile contains a stranded repair claim.
- Expected: An automated run accesses repair files only in its own isolated data parent. Ordinary play uses the player's data parent.
- Actual: RepairDirectory always uses NativePersistenceIsolation.UserDataParent. With no bound isolation authority that resolves to ApplicationPaths.persistentDataPath. RuntimeAutomationHost creates an isolation bootstrap only for selected scenarios. Once a party is available, the repair poll can write an Interrupted result and rename the player's .claimed file even with zero matching horses and a closed world-write gate.
- Why it blocks / does not: Running a disposable fixture can consume a player's pending repair and overwrite its result outside the fixture transaction.
- Requested correction: Make repair-folder selection distinguish ordinary play, automation with a bound isolated root, and automation without one. Suppress repair-file processing in the last case, including recovery of previously claimed files.
- Requested evidence: Exercise the production folder-selection path with separate player and run folders. Seed the player folder with a stranded claim and an existing result. Verify both remain byte-identical in a non-isolated guarded run, then verify ordinary play and an isolated run each process only their own folder.

**R1-F2** · P1 · defect · blocks readiness — Failed post-removal verification can cause a second removal or a false success
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:167`; acceptance: MMR-06, S06
- Trigger: Native removal changes the feature collection and the subsequent presence read throws, or the adapter detects an unexpected feature-count change.
- Expected: Only a verified unchanged state permits another removal attempt. An exact removal can finish as applied. An uncertain or inconsistent outcome preserves the failure and prevents further deletion under that claim.
- Actual: If StillPresent throws after Remove, the session remains Undecided. The next poll can report NoCandidate for the removal it just made or remove a reloaded copy. Conversely, the adapter throws when its feature-count check detects an unexpected change, but Process discards that exception whenever the target is absent. An unavailable feature collection also makes StillPresent return false.
- Why it blocks / does not: One owner request can perform two removals or produce an Applied record after the adapter detected that more than the requested fact changed.
- Requested correction: Retain the attempted target and distinguish exact removal, verified no change, and failed verification. A retry after an uncertain mutation must resolve or report that attempt without selecting another candidate. Preserve the adapter's exact-delta failure in the terminal outcome.
- Requested evidence: Inject a presence-read failure after removal, reload the same ID with one candidate, and verify one total removal and a truthful result. Also inject removal of the target plus a neighboring fact and verify the integrity failure is reported. Retain the passing control where removal completes exactly and a later callback throws.

**R1-F3** · P2 · defect · blocks readiness — The stock-fact predicate accepts a feature carrying additional effects
- Location: `src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs:58`; acceptance: MMR-06, S06
- Trigger: Another installed mod adds a component to the stock TrippingBite blueprint while retaining its GUID and original Bite/Trip component.
- Expected: Whole-fact omission or removal requires the evidenced stock feature shape. Additional effects on a modified blueprint must be preserved.
- Actual: IsStockTrippingBite uses ComponentsArray.Any. The installed-data check establishes exactly one component, but a feature containing that component and an additional effect passes the production predicate. HorseInheritedFacts omits the entire feature, and the saved-horse path can remove the entire instance.
- Why it blocks / does not: The repair can discard an identifiable independent effect along with Trip.
- Requested correction: Restrict whole-fact treatment to the verified stock component list. Preserve a mixed or otherwise unrecognized feature when its effects cannot safely be separated.
- Requested evidence: Use a genuine stock-GUID feature containing both the Bite/Trip component and a second effect. Verify that fresh construction preserves the mixed feature and saved-horse repair excludes it. Keep the single-component stock positive control.

**R1-F4** · P2 · defect · blocks readiness — The session does not retain exact claim-file identity
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:332`; acceptance: MMR-06, S06
- Trigger: A bound .claimed file is re-encoded between polls, or its attributes temporarily cannot be read while publication is pending.
- Expected: A retained claim resumes only for its exact bytes. A failed observation preserves the session record until file absence, replacement or successful archival can be established.
- Actual: TryRead uses File.ReadAllText and Owns compares decoded strings. UTF-8 files with and without a byte-order mark compare equal. Close then archives the file at the claim path. In the opposite direction, File.Exists can return false on an attribute-access error, causing Process to clear the session's claim even though the file still exists.
- Why it blocks / does not: A replacement file can inherit an earlier decision, and a transient observation failure can discard a terminal decision that should survive reporting retries.
- Requested correction: Read and bind the raw claim bytes or their digest independently of decoding the request. Keep that identity through archival and distinguish confirmed absence from transient read or access errors.
- Requested evidence: After FailClose, replace the bound claim with the same visible line in a different encoding. Verify the replacement cannot receive the old disposition and compare the archive as bytes. Inject a transient failure observing an existing held claim and verify the original terminal outcome survives.

**R1-F5** · P2 · defect · blocks readiness — A newer request can lose the supersession race during the return rename
- Location: `src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs:351`; acceptance: MMR-06, S06
- Trigger: The owner writes a new request after ReleaseClaim checks that the request filename is free and before it moves the waiting claim back.
- Expected: When a newer request wins the request filename while an older claim is being returned, the waiting claim is superseded and the newer bytes are preserved for their own poll.
- Actual: ReleaseClaim checks File.Exists and then calls File.Move. If the newer request appears between those operations, Move throws instead of returning false. The policy never records Superseded. If the waiting horse loads before the next poll, DecideClaimed can remove its fact while the newer request is still present.
- Why it blocks / does not: The outcome for the older horse depends on which side of a filesystem race the newer request arrives.
- Requested correction: Handle a destination-exists collision from the non-overwriting return rename as supersession. Preserve ordinary retry behavior for unrelated I/O failures.
- Requested evidence: Create request B at the return-rename boundary for unloaded horse A. Load A before the next poll. Verify zero removals from A, A's original bytes in the superseded archive, and exactly one later application to B.

**R1-F6** · P2 · evidence_gap · blocks readiness — The focused contract does not exercise two controls named in its output
- Location: `scripts/Test-MmrHorseTripNativeContract.ps1:146`; acceptance: S06
- Trigger: The Trip contract reports that the horse retains its bite and hooves and that a foreign fact with the same trigger survives.
- Expected: The offline check observes the constructed bite and hooves and isolates GUID as the distinguishing property of the foreign Bite/Trip control.
- Actual: The limb check only looks up the CopyBody method. It would pass if the method returned zero limbs. The foreign-GUID fixture sets Maneuver on an uninitialized component but neither sets nor verifies Category against the installed Bite value.
- Why it blocks / does not: The gate labels claim two S06 controls that the assertions do not establish. In particular, the positive bite control would pass with no bite.
- Requested correction: Exercise the compiled body construction with distinct bite and hoof objects and inspect the resulting limb order. Give the foreign-GUID control the same installed category and maneuver as the stock control.
- Requested evidence: Show the new candidate's focused output verifying one bite followed by two hooves, and preservation of a different-GUID fact carrying the same Bite/Trip trigger. Live hit and damage remain N07 owner observations.

**R1-F7** · P3 · required_gap · blocks readiness — The PR-4 harness summaries omit the required native-evidence marker
- Location: `scripts/Test-MmrHorseTripNativeContract.ps1:228`; acceptance: S06
- Trigger: The Trip contract or its focused wrapper is read independently of the outer Fast-tier summary.
- Expected: The new offline harness outputs state nativeProof=false.
- Actual: The Trip script and focused wrapper print PASS totals without the required nativeProof=false marker. The outer Fast summary does identify the execution as offline.
- Why it blocks / does not: Mission section 7 explicitly requires the marker on offline harness evidence.
- Requested correction: Include nativeProof=false in the Trip contract and focused wrapper summaries.
- Requested evidence: The next focused gate log contains the marker alongside the checks and their actual totals.

**R1-F8** · P2 · required_gap · blocks readiness — The pre-review record uses larger ceilings than the enrolled mission
- Location: `docs/missions/mmr-split/PR-4-ledger-excerpt.md:13`; acceptance: —
- Trigger: The developer or controller relies on the submitted allocation check when admitting further validation or review.
- Expected: The pre-review record reconciles usage against the enrolled limits of 90 minutes, 15 additional child rounds and 22 lifetime rounds, or supplies the applicable owner authorization for changed ceilings.
- Actual: The excerpt records 150 minutes, 18 child rounds and 25 lifetime rounds. It refers to an owner instruction at sequence 14 but includes only sequences 15 and 16. The controller lists no amendment. The actual gate ended at 80.01 minutes, within the enrolled 90-minute allowance, and the two-round child limit agrees.
- Why it blocks / does not: The required pre-review check advertises additional capacity whose authority cannot be verified from this enrollment. The passing gate result itself is supported.
- Requested correction: Preserve the historical entries and add a reconciliation against the enrolled limits. If the larger ceilings govern this enrollment, supply the owner authority covering both the time and program-round changes through the review inputs.
- Requested evidence: A traceable pre-review record showing applicable ceilings, actual spend and round usage, with any superseding authorization available to the reviewer.

#### Evidence assessment

- **Inspected:** Controller mission, request, prior-findings ledger, developer report, transcript notice, candidate receipt and its full gate log.; AGENTS.md, validation policy, current handoff, resume instructions, the complete 16-file feature diff and relevant historical mission evidence.; Horse and Riding Dog construction, party enumeration, the composition root, save isolation and guarded fixture selection, the repair policy and filesystem adapter, component tests and focused contract scripts.
- **Executed verification:** The reviewer verified HEAD, merge base, mission identity, and the gate log SHA-256. The log hash matches the controller receipt. Git found no differences between the eight named extraction files at 46444b30 and 1430b081. The feature diff passed git diff --check.; The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at 8fc887ad. It exited 0 in 34.1 seconds with 1537 component passes, 153 Validate-Source passes, 43 Trip contract passes and 12 of 12 focused children passing.; The recorded R7-F5 test includes FailClose after an ambiguous refusal, reload of the same horse ID with one candidate, zero removals and a refused archive. The supersession cases preserve the newer request through result-write and archive failures. The earlier claim, lookup, removal and restart controls also passed.; The installed-data check identified Mammoth TrippingBite with one ManeuverOnAttack component configured for Bite and Trip. The Mounted Shield contract passed 114 checks and rollback passed 68. Packet, production and embedded icon bytes matched SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.
- **Source inspection only:** Horse registration filters Mammoth AddFacts separately from CopyBody, which constructs one bite followed by two hooves. Riding Dog construction continues to use the stock Dog's facts.; The composition root supplies the enable, save, transition, restoration, worker and load gates. Guarded fixture selection, post-removal error handling, byte identity and rename interleavings were traced through their production callers.
- **Required gaps:** Protect the player's repair files during guarded runs and preserve exactly-once behavior when native removal verification or a claim-file transition fails.; Restrict whole-fact removal to the established stock shape and exercise the positive bite and foreign-grant controls against the compiled product.; Add the required offline evidence marker and reconcile the recorded validation and review ceilings with the enrolled mission.
- **Limitations:** The controller supplied the candidate gate receipt in place of the mixed-session developer transcript. The earlier development-run counts have no separate execution receipt in these inputs.; The reviewer used a read-only snapshot. The newly identified failure paths are source findings. Constrained PowerShell also prevented a standalone encoding probe.; All candidate execution in the supplied evidence was offline. The historical screenshot establishes a bite hit, damage and a subsequent Trip. It does not identify the originating fact on that particular saved horse.

#### Summary

- **Delivered behavior:** The candidate isolates the stock Mammoth Bite/Trip grant, filters it from ordinary new KMC horses, and provides an owner-requested repair for existing horses. The recorded R7-F5 cases preserve terminal refusals and supersessions across publication failures. The Mounted Shield regression and embedded icon checks passed.
- **Scope reviewed:** The complete PR-4 diff against main, including the inherited PR #64 implementation, its correction, the S06 and S08 offline evidence, and the enrollment records.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the cited November 26 owner instruction governs both enlarged ceilings for this enrollment.
- **Remaining owner review:** N07 — observe a successful ordinary horse bite and damage without unintended Trip, with a legitimate dog, wolf or separately granted maneuver as a control where available.; N10 — observe a pre-repair horse through save/reload or an area transition, including pair identity, effects and action ownership.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a215b5-286d-7b31-8c2f-8f2f8a822899`, duration 919.1s, response sha256 `8ad972058c3f67b7f4182c5be2899b800ecf6292e6b5e0d9cc1ca0d396b00634`, request `kmc-codex-mmr-horse-trip-20261125213249-4b8b-r01-490add`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "8fc887ad2ccd3d8f965634cc5a8697f4f6c7391d",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate passes the required focused gate and fixes the carried R7-F5 archive-retry case. Two other paths still put owner state at risk. A guarded fixture run can consume a repair file from the player's profile, and a failed verification after native removal can allow the same request to remove a fact again.\n\n## Continuation for Claude\n\nLimit the correction to PR-4. Resolve the repair directory from the actual run context before accessing any request files. Require the evidenced stock component shape before removing a whole feature. Retain the outcome of a removal attempt across verification failures, bind decisions and archives to the claimed bytes, and handle a newer request arriving during the return rename. Use the existing real-folder host and detached Kingmaker objects to exercise the cases in the findings. Correct the offline output and reconcile the pre-review limits. Batch the changes into one candidate and run the enrolled Fast focused gate within the applicable allowance. N07 and N10 belong to the owner's playtest.",
  "evidence_assessment": {
    "executed_verification": [
      "The reviewer verified HEAD, merge base, mission identity, and the gate log SHA-256. The log hash matches the controller receipt. Git found no differences between the eight named extraction files at 46444b30 and 1430b081. The feature diff passed git diff --check.",
      "The controller ran scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseTripFocused.ps1 at 8fc887ad. It exited 0 in 34.1 seconds with 1537 component passes, 153 Validate-Source passes, 43 Trip contract passes and 12 of 12 focused children passing.",
      "The recorded R7-F5 test includes FailClose after an ambiguous refusal, reload of the same horse ID with one candidate, zero removals and a refused archive. The supersession cases preserve the newer request through result-write and archive failures. The earlier claim, lookup, removal and restart controls also passed.",
      "The installed-data check identified Mammoth TrippingBite with one ManeuverOnAttack component configured for Bite and Trip. The Mounted Shield contract passed 114 checks and rollback passed 68. Packet, production and embedded icon bytes matched SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0."
    ],
    "inspected": [
      "Controller mission, request, prior-findings ledger, developer report, transcript notice, candidate receipt and its full gate log.",
      "AGENTS.md, validation policy, current handoff, resume instructions, the complete 16-file feature diff and relevant historical mission evidence.",
      "Horse and Riding Dog construction, party enumeration, the composition root, save isolation and guarded fixture selection, the repair policy and filesystem adapter, component tests and focused contract scripts."
    ],
    "limitations": [
      "The controller supplied the candidate gate receipt in place of the mixed-session developer transcript. The earlier development-run counts have no separate execution receipt in these inputs.",
      "The reviewer used a read-only snapshot. The newly identified failure paths are source findings. Constrained PowerShell also prevented a standalone encoding probe.",
      "All candidate execution in the supplied evidence was offline. The historical screenshot establishes a bite hit, damage and a subsequent Trip. It does not identify the originating fact on that particular saved horse."
    ],
    "required_gaps": [
      "Protect the player's repair files during guarded runs and preserve exactly-once behavior when native removal verification or a claim-file transition fails.",
      "Restrict whole-fact removal to the established stock shape and exercise the positive bite and foreign-grant controls against the compiled product.",
      "Add the required offline evidence marker and reconcile the recorded validation and review ceilings with the enrolled mission."
    ],
    "source_inspection_only": [
      "Horse registration filters Mammoth AddFacts separately from CopyBody, which constructs one bite followed by two hooves. Riding Dog construction continues to use the stock Dog's facts.",
      "The composition root supplies the enable, save, transition, restoration, worker and load gates. Guarded fixture selection, post-removal error handling, byte identity and rename interleavings were traced through their production callers."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "RepairDirectory always uses NativePersistenceIsolation.UserDataParent. With no bound isolation authority that resolves to ApplicationPaths.persistentDataPath. RuntimeAutomationHost creates an isolation bootstrap only for selected scenarios. Once a party is available, the repair poll can write an Interrupted result and rename the player's .claimed file even with zero matching horses and a closed world-write gate.",
      "blocking_rationale": "Running a disposable fixture can consume a player's pending repair and overwrite its result outside the fixture transaction.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An automated run accesses repair files only in its own isolated data parent. Ordinary play uses the player's data parent.",
      "id": "R1-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/CompositionRoot.cs",
        "line": 81
      },
      "priority": "P1",
      "requested_correction": "Make repair-folder selection distinguish ordinary play, automation with a bound isolated root, and automation without one. Suppress repair-file processing in the last case, including recovery of previously claimed files.",
      "requested_evidence": "Exercise the production folder-selection path with separate player and run folders. Seed the player folder with a stranded claim and an existing result. Verify both remain byte-identical in a non-isolated guarded run, then verify ordinary play and an isolated run each process only their own folder.",
      "title": "A guarded Working run can consume the player's repair file",
      "trigger": "A guarded scenario such as fixture-intake loads its Working party without binding NativePersistenceIsolation while the player's profile contains a stranded repair claim."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "If StillPresent throws after Remove, the session remains Undecided. The next poll can report NoCandidate for the removal it just made or remove a reloaded copy. Conversely, the adapter throws when its feature-count check detects an unexpected change, but Process discards that exception whenever the target is absent. An unavailable feature collection also makes StillPresent return false.",
      "blocking_rationale": "One owner request can perform two removals or produce an Applied record after the adapter detected that more than the requested fact changed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only a verified unchanged state permits another removal attempt. An exact removal can finish as applied. An uncertain or inconsistent outcome preserves the failure and prevents further deletion under that claim.",
      "id": "R1-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 167
      },
      "priority": "P1",
      "requested_correction": "Retain the attempted target and distinguish exact removal, verified no change, and failed verification. A retry after an uncertain mutation must resolve or report that attempt without selecting another candidate. Preserve the adapter's exact-delta failure in the terminal outcome.",
      "requested_evidence": "Inject a presence-read failure after removal, reload the same ID with one candidate, and verify one total removal and a truthful result. Also inject removal of the target plus a neighboring fact and verify the integrity failure is reported. Retain the passing control where removal completes exactly and a later callback throws.",
      "title": "Failed post-removal verification can cause a second removal or a false success",
      "trigger": "Native removal changes the feature collection and the subsequent presence read throws, or the adapter detects an unexpected feature-count change."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "IsStockTrippingBite uses ComponentsArray.Any. The installed-data check establishes exactly one component, but a feature containing that component and an additional effect passes the production predicate. HorseInheritedFacts omits the entire feature, and the saved-horse path can remove the entire instance.",
      "blocking_rationale": "The repair can discard an identifiable independent effect along with Trip.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Whole-fact omission or removal requires the evidenced stock feature shape. Additional effects on a modified blueprint must be preserved.",
      "id": "R1-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorseCompanionBlueprintService.InheritedFacts.cs",
        "line": 58
      },
      "priority": "P2",
      "requested_correction": "Restrict whole-fact treatment to the verified stock component list. Preserve a mixed or otherwise unrecognized feature when its effects cannot safely be separated.",
      "requested_evidence": "Use a genuine stock-GUID feature containing both the Bite/Trip component and a second effect. Verify that fresh construction preserves the mixed feature and saved-horse repair excludes it. Keep the single-component stock positive control.",
      "title": "The stock-fact predicate accepts a feature carrying additional effects",
      "trigger": "Another installed mod adds a component to the stock TrippingBite blueprint while retaining its GUID and original Bite/Trip component."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "TryRead uses File.ReadAllText and Owns compares decoded strings. UTF-8 files with and without a byte-order mark compare equal. Close then archives the file at the claim path. In the opposite direction, File.Exists can return false on an attribute-access error, causing Process to clear the session's claim even though the file still exists.",
      "blocking_rationale": "A replacement file can inherit an earlier decision, and a transient observation failure can discard a terminal decision that should survive reporting retries.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A retained claim resumes only for its exact bytes. A failed observation preserves the session record until file absence, replacement or successful archival can be established.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 332
      },
      "priority": "P2",
      "requested_correction": "Read and bind the raw claim bytes or their digest independently of decoding the request. Keep that identity through archival and distinguish confirmed absence from transient read or access errors.",
      "requested_evidence": "After FailClose, replace the bound claim with the same visible line in a different encoding. Verify the replacement cannot receive the old disposition and compare the archive as bytes. Inject a transient failure observing an existing held claim and verify the original terminal outcome survives.",
      "title": "The session does not retain exact claim-file identity",
      "trigger": "A bound .claimed file is re-encoded between polls, or its attributes temporarily cannot be read while publication is pending."
    },
    {
      "acceptance_ids": [
        "MMR-06",
        "S06"
      ],
      "actual": "ReleaseClaim checks File.Exists and then calls File.Move. If the newer request appears between those operations, Move throws instead of returning false. The policy never records Superseded. If the waiting horse loads before the next poll, DecideClaimed can remove its fact while the newer request is still present.",
      "blocking_rationale": "The outcome for the older horse depends on which side of a filesystem race the newer request arrives.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "When a newer request wins the request filename while an older claim is being returned, the waiting claim is superseded and the newer bytes are preserved for their own poll.",
      "id": "R1-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseTripRepairPolicy.cs",
        "line": 351
      },
      "priority": "P2",
      "requested_correction": "Handle a destination-exists collision from the non-overwriting return rename as supersession. Preserve ordinary retry behavior for unrelated I/O failures.",
      "requested_evidence": "Create request B at the return-rename boundary for unloaded horse A. Load A before the next poll. Verify zero removals from A, A's original bytes in the superseded archive, and exactly one later application to B.",
      "title": "A newer request can lose the supersession race during the return rename",
      "trigger": "The owner writes a new request after ReleaseClaim checks that the request filename is free and before it moves the waiting claim back."
    },
    {
      "acceptance_ids": [
        "S06"
      ],
      "actual": "The limb check only looks up the CopyBody method. It would pass if the method returned zero limbs. The foreign-GUID fixture sets Maneuver on an uninitialized component but neither sets nor verifies Category against the installed Bite value.",
      "blocking_rationale": "The gate labels claim two S06 controls that the assertions do not establish. In particular, the positive bite control would pass with no bite.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The offline check observes the constructed bite and hooves and isolates GUID as the distinguishing property of the foreign Bite/Trip control.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/Test-MmrHorseTripNativeContract.ps1",
        "line": 146
      },
      "priority": "P2",
      "requested_correction": "Exercise the compiled body construction with distinct bite and hoof objects and inspect the resulting limb order. Give the foreign-GUID control the same installed category and maneuver as the stock control.",
      "requested_evidence": "Show the new candidate's focused output verifying one bite followed by two hooves, and preservation of a different-GUID fact carrying the same Bite/Trip trigger. Live hit and damage remain N07 owner observations.",
      "title": "The focused contract does not exercise two controls named in its output",
      "trigger": "The Trip contract reports that the horse retains its bite and hooves and that a foreign fact with the same trigger survives."
    },
    {
      "acceptance_ids": [
        "S06"
      ],
      "actual": "The Trip script and focused wrapper print PASS totals without the required nativeProof=false marker. The outer Fast summary does identify the execution as offline.",
      "blocking_rationale": "Mission section 7 explicitly requires the marker on offline harness evidence.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The new offline harness outputs state nativeProof=false.",
      "id": "R1-F7",
      "location": {
        "file": "scripts/Test-MmrHorseTripNativeContract.ps1",
        "line": 228
      },
      "priority": "P3",
      "requested_correction": "Include nativeProof=false in the Trip contract and focused wrapper summaries.",
      "requested_evidence": "The next focused gate log contains the marker alongside the checks and their actual totals.",
      "title": "The PR-4 harness summaries omit the required native-evidence marker",
      "trigger": "The Trip contract or its focused wrapper is read independently of the outer Fast-tier summary."
    },
    {
      "acceptance_ids": [],
      "actual": "The excerpt records 150 minutes, 18 child rounds and 25 lifetime rounds. It refers to an owner instruction at sequence 14 but includes only sequences 15 and 16. The controller lists no amendment. The actual gate ended at 80.01 minutes, within the enrolled 90-minute allowance, and the two-round child limit agrees.",
      "blocking_rationale": "The required pre-review check advertises additional capacity whose authority cannot be verified from this enrollment. The passing gate result itself is supported.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The pre-review record reconciles usage against the enrolled limits of 90 minutes, 15 additional child rounds and 22 lifetime rounds, or supplies the applicable owner authorization for changed ceilings.",
      "id": "R1-F8",
      "location": {
        "file": "docs/missions/mmr-split/PR-4-ledger-excerpt.md",
        "line": 13
      },
      "priority": "P2",
      "requested_correction": "Preserve the historical entries and add a reconciliation against the enrolled limits. If the larger ceilings govern this enrollment, supply the owner authority covering both the time and program-round changes through the review inputs.",
      "requested_evidence": "A traceable pre-review record showing applicable ceilings, actual spend and round usage, with any superseding authorization available to the reviewer.",
      "title": "The pre-review record uses larger ceilings than the enrolled mission",
      "trigger": "The developer or controller relies on the submitted allocation check when admitting further validation or review."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-mmr-horse-trip-20261125213249-4b8b-r01-490add",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate isolates the stock Mammoth Bite/Trip grant, filters it from ordinary new KMC horses, and provides an owner-requested repair for existing horses. The recorded R7-F5 cases preserve terminal refusals and supersessions across publication failures. The Mounted Shield regression and embedded icon checks passed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N07 \u2014 observe a successful ordinary horse bite and damage without unintended Trip, with a legitimate dog, wolf or separately granted maneuver as a control where available.",
      "N10 \u2014 observe a pre-repair horse through save/reload or an area transition, including pair identity, effects and action ownership."
    ],
    "scope_reviewed": "The complete PR-4 diff against main, including the inherited PR #64 implementation, its correction, the S06 and S08 offline evidence, and the enrollment records.",
    "unresolved_questions": [
      "Whether the cited November 26 owner instruction governs both enlarged ceilings for this enrollment."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
