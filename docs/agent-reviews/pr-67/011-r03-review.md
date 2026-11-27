<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r03-review part=1/1 sha256=7d45aae0074b528e74264a83c8266f17e63485188a797c5832bc9fe24a50d7c3 -->
### Codex GPT-6.2 Astra — review — round 3 — candidate `eb35a47588`

**Decision:** `changes_required` — The focused gate passed on the fixed candidate. The new Horse outcome writes 27 fields into a format whose reader requires exactly 24. A separate turn-based check can credit a handle the Bite binder rejected. Missing strike data and later handle refreshes can also change what the terminal report says about an earlier Bite.

## Continuation for Claude

Keep the correction within MMR-07 and its directly affected readers. Preserve the registered Bite weapon identity and capture handle provenance when the strike is observed. Carry unreadable values through the terminal outcome. Align the Horse evidence writer, its strict reader and both playback checks while retaining historical artifact support and the existing acted, finished and interruption checks.

Exercise the real terminal callback and serialized outcomes offline, including the minimal outcome, an unregistered handle and a missing classification input after a known Bite. Update the affected legacy harness assertion to exercise the current binding path. Run the enrolled focused gate on the resulting candidate within the existing limits. Preserve native attack ownership, the working hoof sequence and the recorded evidence. Visible playback remains owner observation N08.

#### Findings

**R3-F1** · P1 · defect · blocks readiness — The Horse evidence writer emits three fields its reader rejects
- Location: `src/KingmakerMountedCombat/Diagnostics/HorseCompanionUnmountedScenarioEngine.cs:3098`; acceptance: MMR-07, S07
- Trigger: The horse-native-controls-ux-suite produces a PASS artifact with animation details.
- Expected: The writer emits a supported exact property set and the reader continues to accept frozen historical artifacts.
- Actual: CaptureMountedOutcome adds attackAnimationObservation, attackAnimationHandleRegistered and attackAnimationRegisteredSource. Complete still emits schema 8. RuntimeHarness.Common.ps1 line 9022 requires exactly the previous 24 keys for all four rider and Horse outcomes. The candidate emits 27.
- Why it blocks / does not: Every otherwise passing new Horse native-controls artifact with these outcomes fails its evidence reader.
- Requested correction: Preserve the schema-8 wire format or version the expanded format together with its strict reader and fixtures.
- Requested evidence: Pass candidate-shaped rider and Horse outcomes through the production reader, retain a historical schema-8 positive and show that an unexpected field still fails.

**R3-F2** · P2 · defect · blocks readiness — The explicit Mount Primary check credits an unregistered handle
- Location: `src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs:3094`; acceptance: MMR-07, S07
- Trigger: A physical Bite strikes with an unregistered handle that acted and finished without interruption.
- Expected: The turn-based explicit Mount Primary row credits a validated Bite handle and retains its acted, finished and interruption requirements.
- Actual: AttachHorseBite now sets AttackAnimationHandleCreated for any observed handle, including source unrecorded. AwaitMountPrimaryTb checks that boolean and the playback flags. Its reader at RuntimeHarness.Common.ps1 line 8378 makes the same decision. Neither checks the observed source. A rejected Hoof action on a physical Bite can satisfy both animation checks.
- Why it blocks / does not: A handle expressly rejected by the binder can receive credit as exact Horse Bite playback.
- Requested correction: Apply the exact observed-source requirement to this row and the corresponding reader. Preserve historical evidence semantics for older artifacts.
- Requested evidence: An acted, finished, uninterrupted handle rejected for its action or manager must fail this row and its reader. A registered Bite with the same state flags must pass.

**R3-F3** · P2 · defect · blocks readiness — The structured terminal outcome drops the handle-read failure
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1929`; acceptance: MMR-07, S07
- Trigger: A terminal action or playback getter throws after the observed handle and its source have been identified.
- Expected: The outcome preserves a failed fact read, and playback acceptance requires readable facts.
- Actual: HorseBiteTerminal.Project sets FactsUnreadable when a getter throws. AttachHorseBite discards that field and copies the remaining booleans, including default false values for getters never reached. The TB and RT checks call the three-argument PlaybackAccepted overload, which cannot see the fault. The component overload that checks FactsUnreadable can therefore reject a projection the scenario accepts.
- Why it blocks / does not: Terminal evidence can report a definite interruption state and accept playback after the observation failed.
- Requested correction: Carry readability through the actual outcome and make the scenario checks use the same complete playback decision as the component check.
- Requested evidence: Inject a terminal fact-read failure on an observed handle. Inspect the emitted outcome and show that both scenario checks reject it while the readable positive still passes.

**R2-F2** · P2 · defect · blocks readiness — Missing plan or limb data can still preserve a stale Bite
- Location: `src/KingmakerMountedCombat/Domain/HorseStrikeDiagnostic.cs:135`; acceptance: MMR-07, S07
- Trigger: The Horse reaches observation and a required plan or limb read returns no value without throwing.
- Expected: A recognized Horse callback distinguishes an unavailable classification from a confirmed non-Bite limb.
- Actual: A null return sets planRead to true. IsHorseBiteSlot also returns false when required hand, weapon or limb metadata is absent. Observe(false, ...) retains the previous Bite, exactly as it does for a positively identified hoof. Only a thrown exception takes the unreadable path.
- Why it blocks / does not: An unclassifiable Horse callback can still leave stale positive Bite evidence in the terminal report.
- Requested correction: Distinguish missing classification inputs from a readable non-Bite result. Keep binding fail-closed and preserve the single native delegation.
- Requested evidence: After a known Bite, return a null plan and supply a plan missing required limb evidence. Verify one identifiable diagnostic and an explicit unavailable disposition. A positively identified hoof should retain the prior Bite.

**R3-F4** · P2 · defect · blocks readiness — A later handle refresh erases the source of an observed Bite
- Location: `src/KingmakerMountedCombat/Domain/HorseStrikeDiagnostic.cs:223`; acceptance: MMR-07, S07
- Trigger: The binder refreshes its registration after a Bite acts and before another Bite is observed.
- Expected: The observed Bite retains the stock-created or KMC-supplied source it had at its own act boundary. The latest command registration remains separately identifiable.
- Actual: Project and Describe determine source at terminal time by comparing the observed handle with the command's latest registered handle. A legitimate same-action refresh replaces that registration. If the command ends before the new Bite strikes, the previously observed, registered Bite is reported as unrecorded.
- Why it blocks / does not: Later planning can rewrite the reported provenance of a Bite that already happened.
- Requested correction: Capture source together with the observed Bite handle. Use that captured source in both terminal representations while continuing to read terminal state from the observed handle.
- Requested evidence: Observe a registered Bite, refresh to a different handle of the same action, then terminate before another Bite acts. Check the original source in text and structured output and retain the genuinely unregistered control.

**R3-F5** · P2 · defect · blocks readiness — Replacing the first blueprint limb can turn a hoof into a Bite
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:187`; acceptance: MMR-07, S07
- Trigger: Another blueprint edit replaces the Horse's first limb after registration and the runtime slot carries that replacement.
- Expected: The planned weapon matches the Bite registered by KMC as well as the actual first limb slot.
- Actual: IsBiteLimb compares the planned weapon with the current blueprintLimbs[0]. If that entry and the runtime weapon are both replaced by a hoof, the references agree and the predicate accepts the hoof. HorseCompanionBlueprintService retains the independently registered Bite GUID at line 671, but this check does not use that identity.
- Why it blocks / does not: With a replaced first limb, a missing hoof handle can receive the unique Bite action. That violates the exact-weapon boundary.
- Requested correction: Use the registration-time Bite identity in the exact-limb proof. Retain support for that weapon with SpecialAnimation.None.
- Requested evidence: After registration, substitute the same foreign weapon in the blueprint's first limb and the runtime slot. Verify zero adoption, creation and assignment, alongside successful fallback for the original untagged Bite.

**R2-F3** · P2 · evidence_gap · blocks readiness — The fallback test observes a test-owned outcome instead of the terminal callback
- Location: `tests/HorseStrikeHandleBindingTests.cs:438`; acceptance: MMR-07, S07
- Trigger: Assessing the claimed fallback delivery after an unrelated detailed-outcome read throws.
- Expected: A forced detailed-read failure delivers exactly one real terminal outcome carrying the previously observed Bite disposition.
- Actual: MinimalTerminalKeepsBite adds the generic composer's return value to a test list. Its test-owned attachment writes the Bite string and its non-Horse attachment assigns null explicitly. The compiled check confirms call presence. Neither observation captures ReportTerminalOnce delivering the actual MountedPairAttackOutcome.
- Why it blocks / does not: The common attachment is visible in source, but the requested fallback callback and non-Horse control have not been observed through the production delivery path.
- Requested correction: Exercise the command's terminal delivery and capture its outcome using the existing offline infrastructure.
- Requested evidence: Show the actual callback count and terminal fields for a known Bite after a detailed-read failure, with a detailed-path positive and a non-Horse control using the production attachment.

**R3-F6** · P2 · defect · blocks readiness — The legacy Horse harness still requires the removed inline binding
- Location: `scripts/Test-Harness.ps1:10527`; acceptance: MMR-07, S07
- Trigger: A validation tier selects the existing Horse native-controls contract in Test-Harness.ps1.
- Expected: The existing Horse contract check continues to validate the current exact binding path.
- Actual: The existing Horse native-controls harness assertion still searches the adapter for the old inline candidate count, assignment, adoption and refresh statements. Seven literal predicates that match main fail on this branch. The focused wrapper does not select Test-Harness.ps1.
- Why it blocks / does not: A directly affected repository check will reject the candidate whenever selected, even when the new binder performs the required operations.
- Requested correction: Replace the obsolete spelling checks with coverage of the corresponding behavior or compiled contract. Retain the exact action, ownership and idempotence assertions.
- Requested evidence: Exercise the affected harness case in isolation and retain a negative for an inexact or duplicate binding.

**R3-F7** · P3 · optional_polish · non-blocking — Two evidence notes overstate what their artifacts identify
- Location: `docs/missions/mmr-split/evidence/PR-5-b1c82f31-verification-2026-11-26.md:43`; acceptance: MMR-07
- Trigger: Reproducing the recorded digests and tracing the original extraction run.
- Expected: The evidence notes identify which bytes each digest covers and distinguish observed output from inference.
- Actual: The documented mutation-log digest 1fc58edc… matches the bytes with lines 39 through 50 encoded as CRLF. The committed LF blob hashes to dd6dfd6d…. The log shows the failing check and IL order, but does not print the mutated field value. The earlier extraction note at line 22 also attributes the whole pre-commit tree to a matching product DLL.
- Why it blocks / does not: The frozen-run proof, candidate gate and mutation rejection are independently inspectable.
- Requested correction: Label the raw and committed mutation-log digests, describe the recorded mutation result precisely and clarify the earlier extraction provenance sentence.
- Requested evidence: A documentation-only comparison against the existing logs and Git blobs is sufficient.

#### Prior finding assessments

- **R1-F4** — `fix_verified` — For the original adoption followed by a handle-less Bite, the production projection clears the observed handle and source, retains the historical registration separately and fails both native-controls predicates. The positive Bite followed by a hoof is covered. R3-F2 through R3-F4 concern different observations and consumers.
- **R1-F9** — `fix_verified` — The separately dated, metered frozen run identifies b1c82f31, its Git tree, the component executable, product DLL and selected scripts. Its 28-check result supplies the requested extraction-commit proof. The earlier pre-commit tree remains unidentified.
- **R2-F1** — `fix_verified` — The registered first-slot Bite with SpecialAnimation.None now reaches the unique Bite fallback. The detached gate observed one creation, assignment and record, and zero writes for the hoof-only and unrelated-weapon controls. R3-F5 concerns a subsequent replacement of the blueprint slot itself.
- **R2-F2** — `fix_rejected` — Thrown plan, limb and handle reads now produce an unreadable observation and return to the single native delegation. Missing classification data returned normally still takes the non-Bite path and can retain stale evidence.
- **R2-F3** — `fix_rejected` — Source inspection confirms the common attachment after detailed or minimal construction. The executed fallback test supplies its own attachment and simulates delivery by adding a value to a list. The requested observation of the command's actual terminal callback remains outstanding.
- **R2-F4** — `fix_verified` — The throwing VariantsCount case now emits one strike line containing the failure marker, readable action and speed, BiteAttack clip, weight 0.8 and Playing state.
- **R2-F5** — `fix_verified` — The candidate check confirms the Unity equality precondition passes, distinguishes the ECall fault from the validation exception and verifies empty record fields. The archived early-write mutation fails the strengthened check.
- **R2-F6** — `deferral_accepted` — The Debug and Release arguments are forwarded correctly. Dependence on literal path spelling remains an optional maintenance issue.
- **R2-F7** — `deferral_accepted` — The fault message can misdescribe a successful binding when its subsequent report throws. The committed handle survives and the exception is contained, consistent with the prior optional classification.

#### Evidence assessment

- **Inspected:** The controller mission, request, prior ledger, report, transcript notice, gate receipt and log, AGENTS.md, validation policy and current handoffs.; The 9,028-line feature diff and round-three correction delta, including the binding adapter, observation and terminal code, focused checks, project registrations and archived evidence.; Horse blueprint registration, both terminal construction paths, the Horse native-controls writer and reader, the Phase 3D turn-based playback check and its Phase 3E evidence reader.
- **Executed verification:** The controller receipt binds the Fast focused gate to eb35a475881fcd24fda838f119fdcce34681cfb8 with an unchanged worktree, exit 0 and duration 90.7 seconds. I verified the receipt's log SHA-256. Components passed 1545/0, Validate-Source 153/0, the Bite checks 3/0 and 39/0, and presentation 239/0. All 14 focused children passed.; The same gate reports Mounted Shield 114/0 and rollback 68/0. The packet, production and embedded icon bytes share SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. The checked product DLL has SHA-256 b45891b78785262256b0d5d63ae2765f7a04318d0b626d96a9cc4ec0093f793f.; The archived November 26 frozen run records 1.48 minutes, components 1534/0, Bite contract 28/0 and 14/14 children at b1c82f31. I matched its tree and six recorded script blob IDs against Git and verified its log digest. The candidate gate separately records get_name at IL 67 before the first record write at 105. The archived mutation check failed with the write at 68 before get_name at 74.; Read-only source comparisons found 27 writer keys versus 24 reader keys. I also compared 31 literal predicates in the affected legacy harness assertion against Git. All 31 matched main and seven fail on the candidate.
- **Source inspection only:** The schema rejection, false playback credit, loss of unreadability and source reassignment after refresh follow from the production writer and consumer code. I did not execute a game scenario.; The native delegation remains a single unguarded base.OnAction call in source and compiled-call evidence. The fault-injection component test exercises the observer.
- **Required gaps:** A bounded check of the actual terminal callback and its serialized Horse outcome, including the detailed-read failure and a non-Horse control.; An offline writer-to-reader check and playback negatives covering rejected handles, unreadable facts, missing classification inputs and a refresh after an observed Bite.; A focused check of the affected legacy harness assertion against the corrected binding.
- **Limitations:** This was a read-only review. The candidate execution evidence comes from the controller receipt. The transcript notice says the mixed foreground session was withheld. The frozen and mutation runs have separate archived outputs.; The detached tests cover handle binding, simulated faults and compiled call sites. Current game-side playback, blending, visible timing and rule counts remain unobserved.

#### Summary

- **Delivered behavior:** The candidate restores fallback for an untagged registered Bite, contains throwing strike reads before native delegation, preserves readable playback after a variant-count failure and separates observed handles from historical registration in the normal terminal cases.
- **Scope reviewed:** Final PR-5 MMR-07 source and offline evidence, its direct diagnostics consumers, S07 binding obligations and S08 Mounted Shield regression evidence.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the native routine's shared SpeedScale makes the Bite unreadable at normal camera distance.
- **Remaining owner review:** N08 / NX-2 remains pending. At normal play zoom, correlate the visible Bite with the logged strike and confirm facing, hoof sequence, rider attachment and the native attack and damage sequence.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a21800-d5d7-7ba2-b15e-75bc3a95045c`, duration 1030.6s, response sha256 `766bce593473d7bd6bcec15c57b291f3b75c350b0839782849f259270aa0e307`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r03-1d5c84`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "eb35a475881fcd24fda838f119fdcce34681cfb8",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the fixed candidate. The new Horse outcome writes 27 fields into a format whose reader requires exactly 24. A separate turn-based check can credit a handle the Bite binder rejected. Missing strike data and later handle refreshes can also change what the terminal report says about an earlier Bite.\n\n## Continuation for Claude\n\nKeep the correction within MMR-07 and its directly affected readers. Preserve the registered Bite weapon identity and capture handle provenance when the strike is observed. Carry unreadable values through the terminal outcome. Align the Horse evidence writer, its strict reader and both playback checks while retaining historical artifact support and the existing acted, finished and interruption checks.\n\nExercise the real terminal callback and serialized outcomes offline, including the minimal outcome, an unregistered handle and a missing classification input after a known Bite. Update the affected legacy harness assertion to exercise the current binding path. Run the enrolled focused gate on the resulting candidate within the existing limits. Preserve native attack ownership, the working hoof sequence and the recorded evidence. Visible playback remains owner observation N08.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt binds the Fast focused gate to eb35a475881fcd24fda838f119fdcce34681cfb8 with an unchanged worktree, exit 0 and duration 90.7 seconds. I verified the receipt's log SHA-256. Components passed 1545/0, Validate-Source 153/0, the Bite checks 3/0 and 39/0, and presentation 239/0. All 14 focused children passed.",
      "The same gate reports Mounted Shield 114/0 and rollback 68/0. The packet, production and embedded icon bytes share SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. The checked product DLL has SHA-256 b45891b78785262256b0d5d63ae2765f7a04318d0b626d96a9cc4ec0093f793f.",
      "The archived November 26 frozen run records 1.48 minutes, components 1534/0, Bite contract 28/0 and 14/14 children at b1c82f31. I matched its tree and six recorded script blob IDs against Git and verified its log digest. The candidate gate separately records get_name at IL 67 before the first record write at 105. The archived mutation check failed with the write at 68 before get_name at 74.",
      "Read-only source comparisons found 27 writer keys versus 24 reader keys. I also compared 31 literal predicates in the affected legacy harness assertion against Git. All 31 matched main and seven fail on the candidate."
    ],
    "inspected": [
      "The controller mission, request, prior ledger, report, transcript notice, gate receipt and log, AGENTS.md, validation policy and current handoffs.",
      "The 9,028-line feature diff and round-three correction delta, including the binding adapter, observation and terminal code, focused checks, project registrations and archived evidence.",
      "Horse blueprint registration, both terminal construction paths, the Horse native-controls writer and reader, the Phase 3D turn-based playback check and its Phase 3E evidence reader."
    ],
    "limitations": [
      "This was a read-only review. The candidate execution evidence comes from the controller receipt. The transcript notice says the mixed foreground session was withheld. The frozen and mutation runs have separate archived outputs.",
      "The detached tests cover handle binding, simulated faults and compiled call sites. Current game-side playback, blending, visible timing and rule counts remain unobserved."
    ],
    "required_gaps": [
      "A bounded check of the actual terminal callback and its serialized Horse outcome, including the detailed-read failure and a non-Horse control.",
      "An offline writer-to-reader check and playback negatives covering rejected handles, unreadable facts, missing classification inputs and a refresh after an observed Bite.",
      "A focused check of the affected legacy harness assertion against the corrected binding."
    ],
    "source_inspection_only": [
      "The schema rejection, false playback credit, loss of unreadability and source reassignment after refresh follow from the production writer and consumer code. I did not execute a game scenario.",
      "The native delegation remains a single unguarded base.OnAction call in source and compiled-call evidence. The fault-injection component test exercises the observer."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "CaptureMountedOutcome adds attackAnimationObservation, attackAnimationHandleRegistered and attackAnimationRegisteredSource. Complete still emits schema 8. RuntimeHarness.Common.ps1 line 9022 requires exactly the previous 24 keys for all four rider and Horse outcomes. The candidate emits 27.",
      "blocking_rationale": "Every otherwise passing new Horse native-controls artifact with these outcomes fails its evidence reader.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The writer emits a supported exact property set and the reader continues to accept frozen historical artifacts.",
      "id": "R3-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Diagnostics/HorseCompanionUnmountedScenarioEngine.cs",
        "line": 3098
      },
      "priority": "P1",
      "requested_correction": "Preserve the schema-8 wire format or version the expanded format together with its strict reader and fixtures.",
      "requested_evidence": "Pass candidate-shaped rider and Horse outcomes through the production reader, retain a historical schema-8 positive and show that an unexpected field still fails.",
      "title": "The Horse evidence writer emits three fields its reader rejects",
      "trigger": "The horse-native-controls-ux-suite produces a PASS artifact with animation details."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "AttachHorseBite now sets AttackAnimationHandleCreated for any observed handle, including source unrecorded. AwaitMountPrimaryTb checks that boolean and the playback flags. Its reader at RuntimeHarness.Common.ps1 line 8378 makes the same decision. Neither checks the observed source. A rejected Hoof action on a physical Bite can satisfy both animation checks.",
      "blocking_rationale": "A handle expressly rejected by the binder can receive credit as exact Horse Bite playback.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The turn-based explicit Mount Primary row credits a validated Bite handle and retains its acted, finished and interruption requirements.",
      "id": "R3-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Diagnostics/Phase3dHorseScenarioTranche.cs",
        "line": 3094
      },
      "priority": "P2",
      "requested_correction": "Apply the exact observed-source requirement to this row and the corresponding reader. Preserve historical evidence semantics for older artifacts.",
      "requested_evidence": "An acted, finished, uninterrupted handle rejected for its action or manager must fail this row and its reader. A registered Bite with the same state flags must pass.",
      "title": "The explicit Mount Primary check credits an unregistered handle",
      "trigger": "A physical Bite strikes with an unregistered handle that acted and finished without interruption."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "HorseBiteTerminal.Project sets FactsUnreadable when a getter throws. AttachHorseBite discards that field and copies the remaining booleans, including default false values for getters never reached. The TB and RT checks call the three-argument PlaybackAccepted overload, which cannot see the fault. The component overload that checks FactsUnreadable can therefore reject a projection the scenario accepts.",
      "blocking_rationale": "Terminal evidence can report a definite interruption state and accept playback after the observation failed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The outcome preserves a failed fact read, and playback acceptance requires readable facts.",
      "id": "R3-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1929
      },
      "priority": "P2",
      "requested_correction": "Carry readability through the actual outcome and make the scenario checks use the same complete playback decision as the component check.",
      "requested_evidence": "Inject a terminal fact-read failure on an observed handle. Inspect the emitted outcome and show that both scenario checks reject it while the readable positive still passes.",
      "title": "The structured terminal outcome drops the handle-read failure",
      "trigger": "A terminal action or playback getter throws after the observed handle and its source have been identified."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "A null return sets planRead to true. IsHorseBiteSlot also returns false when required hand, weapon or limb metadata is absent. Observe(false, ...) retains the previous Bite, exactly as it does for a positively identified hoof. Only a thrown exception takes the unreadable path.",
      "blocking_rationale": "An unclassifiable Horse callback can still leave stale positive Bite evidence in the terminal report.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A recognized Horse callback distinguishes an unavailable classification from a confirmed non-Bite limb.",
      "id": "R2-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseStrikeDiagnostic.cs",
        "line": 135
      },
      "priority": "P2",
      "requested_correction": "Distinguish missing classification inputs from a readable non-Bite result. Keep binding fail-closed and preserve the single native delegation.",
      "requested_evidence": "After a known Bite, return a null plan and supply a plan missing required limb evidence. Verify one identifiable diagnostic and an explicit unavailable disposition. A positively identified hoof should retain the prior Bite.",
      "title": "Missing plan or limb data can still preserve a stale Bite",
      "trigger": "The Horse reaches observation and a required plan or limb read returns no value without throwing."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Project and Describe determine source at terminal time by comparing the observed handle with the command's latest registered handle. A legitimate same-action refresh replaces that registration. If the command ends before the new Bite strikes, the previously observed, registered Bite is reported as unrecorded.",
      "blocking_rationale": "Later planning can rewrite the reported provenance of a Bite that already happened.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The observed Bite retains the stock-created or KMC-supplied source it had at its own act boundary. The latest command registration remains separately identifiable.",
      "id": "R3-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseStrikeDiagnostic.cs",
        "line": 223
      },
      "priority": "P2",
      "requested_correction": "Capture source together with the observed Bite handle. Use that captured source in both terminal representations while continuing to read terminal state from the observed handle.",
      "requested_evidence": "Observe a registered Bite, refresh to a different handle of the same action, then terminate before another Bite acts. Check the original source in text and structured output and retain the genuinely unregistered control.",
      "title": "A later handle refresh erases the source of an observed Bite",
      "trigger": "The binder refreshes its registration after a Bite acts and before another Bite is observed."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "IsBiteLimb compares the planned weapon with the current blueprintLimbs[0]. If that entry and the runtime weapon are both replaced by a hoof, the references agree and the predicate accepts the hoof. HorseCompanionBlueprintService retains the independently registered Bite GUID at line 671, but this check does not use that identity.",
      "blocking_rationale": "With a replaced first limb, a missing hoof handle can receive the unique Bite action. That violates the exact-weapon boundary.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The planned weapon matches the Bite registered by KMC as well as the actual first limb slot.",
      "id": "R3-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 187
      },
      "priority": "P2",
      "requested_correction": "Use the registration-time Bite identity in the exact-limb proof. Retain support for that weapon with SpecialAnimation.None.",
      "requested_evidence": "After registration, substitute the same foreign weapon in the blueprint's first limb and the runtime slot. Verify zero adoption, creation and assignment, alongside successful fallback for the original untagged Bite.",
      "title": "Replacing the first blueprint limb can turn a hoof into a Bite",
      "trigger": "Another blueprint edit replaces the Horse's first limb after registration and the runtime slot carries that replacement."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "MinimalTerminalKeepsBite adds the generic composer's return value to a test list. Its test-owned attachment writes the Bite string and its non-Horse attachment assigns null explicitly. The compiled check confirms call presence. Neither observation captures ReportTerminalOnce delivering the actual MountedPairAttackOutcome.",
      "blocking_rationale": "The common attachment is visible in source, but the requested fallback callback and non-Horse control have not been observed through the production delivery path.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A forced detailed-read failure delivers exactly one real terminal outcome carrying the previously observed Bite disposition.",
      "id": "R2-F3",
      "location": {
        "file": "tests/HorseStrikeHandleBindingTests.cs",
        "line": 438
      },
      "priority": "P2",
      "requested_correction": "Exercise the command's terminal delivery and capture its outcome using the existing offline infrastructure.",
      "requested_evidence": "Show the actual callback count and terminal fields for a known Bite after a detailed-read failure, with a detailed-path positive and a non-Horse control using the production attachment.",
      "title": "The fallback test observes a test-owned outcome instead of the terminal callback",
      "trigger": "Assessing the claimed fallback delivery after an unrelated detailed-outcome read throws."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The existing Horse native-controls harness assertion still searches the adapter for the old inline candidate count, assignment, adoption and refresh statements. Seven literal predicates that match main fail on this branch. The focused wrapper does not select Test-Harness.ps1.",
      "blocking_rationale": "A directly affected repository check will reject the candidate whenever selected, even when the new binder performs the required operations.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The existing Horse contract check continues to validate the current exact binding path.",
      "id": "R3-F6",
      "location": {
        "file": "scripts/Test-Harness.ps1",
        "line": 10527
      },
      "priority": "P2",
      "requested_correction": "Replace the obsolete spelling checks with coverage of the corresponding behavior or compiled contract. Retain the exact action, ownership and idempotence assertions.",
      "requested_evidence": "Exercise the affected harness case in isolation and retain a negative for an inexact or duplicate binding.",
      "title": "The legacy Horse harness still requires the removed inline binding",
      "trigger": "A validation tier selects the existing Horse native-controls contract in Test-Harness.ps1."
    },
    {
      "acceptance_ids": [
        "MMR-07"
      ],
      "actual": "The documented mutation-log digest 1fc58edc\u2026 matches the bytes with lines 39 through 50 encoded as CRLF. The committed LF blob hashes to dd6dfd6d\u2026. The log shows the failing check and IL order, but does not print the mutated field value. The earlier extraction note at line 22 also attributes the whole pre-commit tree to a matching product DLL.",
      "blocking_rationale": "The frozen-run proof, candidate gate and mutation rejection are independently inspectable.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The evidence notes identify which bytes each digest covers and distinguish observed output from inference.",
      "id": "R3-F7",
      "location": {
        "file": "docs/missions/mmr-split/evidence/PR-5-b1c82f31-verification-2026-11-26.md",
        "line": 43
      },
      "priority": "P3",
      "requested_correction": "Label the raw and committed mutation-log digests, describe the recorded mutation result precisely and clarify the earlier extraction provenance sentence.",
      "requested_evidence": "A documentation-only comparison against the existing logs and Git blobs is sufficient.",
      "title": "Two evidence notes overstate what their artifacts identify",
      "trigger": "Reproducing the recorded digests and tracing the original extraction run."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "For the original adoption followed by a handle-less Bite, the production projection clears the observed handle and source, retains the historical registration separately and fails both native-controls predicates. The positive Bite followed by a hoof is covered. R3-F2 through R3-F4 concern different observations and consumers."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F9",
      "rationale": "The separately dated, metered frozen run identifies b1c82f31, its Git tree, the component executable, product DLL and selected scripts. Its 28-check result supplies the requested extraction-commit proof. The earlier pre-commit tree remains unidentified."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F1",
      "rationale": "The registered first-slot Bite with SpecialAnimation.None now reaches the unique Bite fallback. The detached gate observed one creation, assignment and record, and zero writes for the hoof-only and unrelated-weapon controls. R3-F5 concerns a subsequent replacement of the blueprint slot itself."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F2",
      "rationale": "Thrown plan, limb and handle reads now produce an unreadable observation and return to the single native delegation. Missing classification data returned normally still takes the non-Bite path and can retain stale evidence."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F3",
      "rationale": "Source inspection confirms the common attachment after detailed or minimal construction. The executed fallback test supplies its own attachment and simulates delivery by adding a value to a list. The requested observation of the command's actual terminal callback remains outstanding."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F4",
      "rationale": "The throwing VariantsCount case now emits one strike line containing the failure marker, readable action and speed, BiteAttack clip, weight 0.8 and Playing state."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F5",
      "rationale": "The candidate check confirms the Unity equality precondition passes, distinguishes the ECall fault from the validation exception and verifies empty record fields. The archived early-write mutation fails the strengthened check."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R2-F6",
      "rationale": "The Debug and Release arguments are forwarded correctly. Dependence on literal path spelling remains an optional maintenance issue."
    },
    {
      "assessment": "deferral_accepted",
      "finding_id": "R2-F7",
      "rationale": "The fault message can misdescribe a successful binding when its subsequent report throws. The committed handle survives and the exception is contained, consistent with the prior optional classification."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r03-1d5c84",
  "round": 3,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate restores fallback for an untagged registered Bite, contains throwing strike reads before native delegation, preserves readable playback after a variant-count failure and separates observed handles from historical registration in the normal terminal cases.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08 / NX-2 remains pending. At normal play zoom, correlate the visible Bite with the logged strike and confirm facing, hoof sequence, rider attachment and the native attack and damage sequence."
    ],
    "scope_reviewed": "Final PR-5 MMR-07 source and offline evidence, its direct diagnostics consumers, S07 binding obligations and S08 Mounted Shield regression evidence.",
    "unresolved_questions": [
      "Whether the native routine's shared SpeedScale makes the Bite unreadable at normal camera distance."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
