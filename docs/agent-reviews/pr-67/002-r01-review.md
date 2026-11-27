<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r01-review part=1/1 sha256=fe8e8b1a2f1107215b030947035c2956ad093706a0e8ae18e2adaf5055fc6e5a -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `22de1f7dec`

**Decision:** `changes_required` — The required Release gate passed on the reviewed commit. The source still allows a hoof to populate the Bite record, a reporting exception to terminate an attack, and the terminal line to describe a handle the acting strike never used. These are offline correctness issues within S07. Visible playback remains an owner observation.

## Continuation for Claude

Use the actual planned weapon and limb to establish Bite eligibility before recording or supplying a handle. Make assignment and recording consistent under failure, and contain failures from outcome reporting on both adapter entry paths. Keep the command's registration history distinct from the handle observed at the strike. Report the actual limb, retain a strike line when optional playback data is unreadable, and keep log-suppression state on the command's lifetime.

Exercise those transitions through the existing focused infrastructure, including the hoof, missing-handle and fault cases in the findings. Correct configuration forwarding in the focused wrapper, remove the Trip-only fixture from this extraction, and supply the preserved extraction-run evidence. Run the enrolled focused gate on the corrected candidate within the existing limits. Preserve native attack planning, timing, rolls, damage, spending and the working hoof sequence. Packaging belongs to PR-6 and N08 remains with the owner.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — The Bite binding accepts hoof actions
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:78`; acceptance: MMR-07, S07
- Trigger: A hoof is the current planned attack before any Bite handle has been recorded, or a missing handle has no expected special type and the sole candidate is Hoof.
- Expected: Only the verified native Bite limb and matching Bite action may populate the Bite registration or receive its fallback. The strike reader can still describe each hoof.
- Actual: The context check compares the planned attack and weapon with themselves. The binder accepts the planned weapon's special type, including Hoof, and treats None as a wildcard during fallback. The passing hoof control explicitly adopts a hoof, and the component test supplies a missing hoof. Both outcomes feed reporting labelled as the exact KMC Bite.
- Why it blocks / does not: A hoof can acquire a new handle or become the command's Bite record. That violates the explicit hoof-preservation and Bite-attribution requirements.
- Requested correction: Prove the planned Bite weapon and slot independently of the current planned-attack reference. Require a proven Bite action before adoption or creation, including when the weapon reports None.
- Requested evidence: Cover a current hoof with and without a stock handle before any Bite registration, and an untyped weapon with only a Hoof candidate. Show zero Bite records and assignments for those cases alongside successful exact Bite adoption and fallback.

**R1-F2** · P1 · defect · blocks readiness — Outcome reporting can still fault the attack
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:97`; acceptance: MMR-07, S07
- Trigger: An adoption, rejection or missing-handle report throws, or the logger fails while reporting an earlier binding exception.
- Expected: Presentation reporting returns control to native attack processing even when metadata access or logging fails.
- Actual: The catch ends before the outcome switch. Remember reads Unity metadata and the switch calls Info or Error outside containment. The catch itself calls logger.Exception without protection. On the direct CreateAndValidateChildAttack path, an escaping exception reaches OnStart's EndAfterFault.
- Why it blocks / does not: A diagnostic failure can end an otherwise legal Horse attack. The compiled check covers the context test, constructor and Bind call only.
- Requested correction: Contain the outcome reads and logging as well as binding. Make reporting the original exception safe if the logger also throws.
- Requested evidence: Inject a throwing outcome logger and a throwing exception logger. Show that SupplyExact returns normally through the command-start path and preserves the native plan.

**R1-F3** · P2 · defect · blocks readiness — A recording fault can leave a partially committed fallback
- Location: `src/KingmakerMountedCombat/Domain/HorseStrikeHandleBinding.cs:154`; acceptance: MMR-07, S07
- Trigger: Handle creation succeeds and the subsequent command registration fails.
- Expected: A failed binding leaves the prior slot and registration consistent. A committed fallback always retains its correct source.
- Actual: Bind assigns the native handle before calling Record. The production record also publishes handle, action and source before reading the Unity action name. A recording fault can therefore leave an assigned fallback or partial command record while the catch reports that the native strike was left as planned.
- Why it blocks / does not: Catching the exception does not restore consistency between the native slot and the exactly-once record. A retry can observe the wrong source or an incomplete registration.
- Requested correction: Complete fallible validation and metadata reads before publishing the slot and record, or undo the exact partial writes on failure. Apply the same consistency rule to refresh.
- Requested evidence: Inject a recording or metadata fault and inspect both the planned slot and command record immediately and on retry. Verify the retained source and assignment count.

**R1-F4** · P2 · defect · blocks readiness — The terminal Bite summary can describe a superseded handle
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1888`; acceptance: MMR-07, S07
- Trigger: Native replans the Bite without a handle after an earlier adoption or supply.
- Expected: The terminal line distinguishes the Bite handle observed at the strike, an absent handle and a Bite that never reached the strike boundary.
- Actual: MissingAfterRecorded deliberately preserves the earlier handle. The terminal outcome reads all Bite fields from that handle, even when the replanned Bite acted with no handle. If no handle was ever recorded, the terminal formatter omits the entire field.
- Why it blocks / does not: The terminal evidence can attribute an earlier handle's action, speed and state to the very strike whose missing playback the owner is investigating.
- Requested correction: Keep the historical registration needed for idempotence. Track the actual Bite observation separately and use its disposition in the Horse terminal fields.
- Requested evidence: Check the emitted terminal fields after adoption followed by a handle-less Bite, after a valid Bite followed by a handle-less hoof, and after an exact Horse command with no observed Bite.

**R1-F5** · P2 · required_gap · blocks readiness — The strike line reports sequence position but omits the planned limb
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1016`; acceptance: MMR-07, S07
- Trigger: A routine contains the two Hoof1d4 slots or an additional strike from an existing limb.
- Expected: Each strike identifies its actual native hand or additional-limb slot as well as its position in the routine.
- Actual: GetAttackIndex() + 1 provides the routine ordinal. HorseStrikeSource never reads the planned Hand for the strike line. Both hoof slots can have the same weapon blueprint, and multiple routine entries can use the same limb.
- Why it blocks / does not: The required limb field cannot be recovered reliably from the ordinal and weapon name.
- Requested correction: Resolve the planned Hand against the Horse's actual slots and include that identity in the strike record. Keep the routine ordinal as a separate field.
- Requested evidence: Show distinct slot identities for two hooves sharing a blueprint and the same slot identity for repeated strikes from one limb.

**R1-F6** · P2 · defect · blocks readiness — An unreadable playback field erases the strike record
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1020`; acceptance: MMR-07, S07
- Trigger: An interrupted or invalidated animation object faults while the reader samples its playback data.
- Expected: A recognized Horse strike still emits one line with available facts and an explicit unavailable observation.
- Actual: HorseStrikeReader gathers every field before logging. If a native getter such as the active clip's Unity name throws, ObserveHorseStrike catches the exception and emits nothing, including the already available weapon and handle state.
- Why it blocks / does not: The owner loses the entire strike precisely when a playback read fails. This breaks the one-line-per-strike diagnostic contract.
- Requested correction: Retain the basic strike record when optional playback reads fail. Distinguish an unreadable observation from a positively absent clip or handle.
- Requested evidence: Use a source whose clip or weight getter throws with a working logger. Verify one identifiable strike line and continued native action delegation.

**R1-F7** · P2 · defect · blocks readiness — Missing-handle log suppression retains a finished command
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:103`; acceptance: MMR-07, S07
- Trigger: A MissingAfterRecorded command ends and the pair dismounts or the world is replaced.
- Expected: A once-per-command diagnostic releases ownership with that command.
- Actual: lastMissingCommand is a strong reference with no cleanup path. Pair services are reused across admission and world boundaries. The retained command holds the rider, mount, target and animation references until another missing-handle command replaces it or the service is discarded.
- Why it blocks / does not: Log suppression introduces old-command and actor retention across the lifecycle boundaries the mission requires preserving.
- Requested correction: Put the reported flag on the command or use non-owning tracking whose lifetime cannot retain the command after retirement.
- Requested evidence: Verify repeated callbacks report once, a subsequent command can report independently, and the pair adapter retains no finished command after retirement.

**R1-F8** · P2 · defect · blocks readiness — The focused wrapper drops the selected build configuration
- Location: `scripts/Test-MmrHorseBiteFocused.ps1:2`; acceptance: S07, S08
- Trigger: Test-Tiers runs the new focused wrapper with -Configuration Debug.
- Expected: Product-reading children inspect the configuration selected by the parent tier.
- Actual: Test-Tiers forwards Configuration to focused scripts that declare it. This wrapper declares no parameters and passes no configuration to its children. With a Debug tier build, product-reading children still open bin/Release and can pass against stale bytes.
- Why it blocks / does not: A supported invocation can report validation for a different artifact. The supplied default Release receipt remains valid.
- Requested correction: Accept the tier configuration and forward it to children that support it, including the bite, presentation, Shield and loader checks.
- Requested evidence: Show with a bounded configuration-routing check that a Debug selection resolves the Debug product and the enrolled Release invocation still resolves the Release product.

**R1-F9** · P2 · evidence_gap · blocks readiness — The extraction run lacks independently inspectable provenance
- Location: `docs/missions/mmr-split/PR-5-horse-bite.md:116`; acceptance: MMR-07
- Trigger: Verifying the mission's extraction proof at b1c82f31.
- Expected: The metered Fast and focused result named for the extraction is traceable to its original commit.
- Actual: The supplied receipt and log bind to 22de1f7d and report 29 bite checks. Section 5 lists a 28-check extraction run, but the supplied evidence contains no run output or meter entry tied to b1c82f31.
- Why it blocks / does not: The final gate establishes the final candidate's result. It cannot establish the separately specified extraction-commit proof.
- Requested correction: Provide the preserved extraction-run evidence through the review evidence channel.
- Requested evidence: The original log or receipt identifying b1c82f31, the selected command, child counts and associated shared-budget entry.

**R1-F10** · P3 · required_gap · blocks readiness — A Trip test fixture was included in the Bite extraction
- Location: `tests/MountedStockAttackPolicyTests.cs:446`; acceptance: MMR-07
- Trigger: Comparing the shared test-file delta with the mission's extraction ownership.
- Expected: PR-5's contribution to MountedStockAttackPolicyTests contains its strike formatter case and registration.
- Actual: The shared test file also adds LoadedUnit. It has no use on this branch and belongs to the saved-Horse repair test on the Trip branch.
- Why it blocks / does not: Section 5 explicitly assigns this shared-file contribution to the formatter case. The extra declaration gives two children ownership of the same fixture.
- Requested correction: Remove the unused LoadedUnit declaration from PR-5.
- Requested evidence: The shared-file diff shows the formatter case and its registration.

**R1-F11** · P3 · optional_polish · non-blocking — Terminal speed formatting varies by locale
- Location: `src/KingmakerMountedCombat/Integration/MountedCombatController.cs:2329`; acceptance: S07
- Trigger: The owner's process uses a decimal-comma culture.
- Expected: Both diagnostic lines render the same numeric value consistently.
- Actual: The terminal speed uses the process culture inside a comma-delimited field list. A speed of 2.4 can appear as speedScale=2,4, while the strike line uses invariant formatting.
- Why it blocks / does not: The per-strike value remains available in invariant form. This is a readability improvement for the terminal line.
- Requested correction: Use invariant culture for terminal SpeedScale.
- Requested evidence: A terminal formatting example under a decimal-comma culture.

#### Evidence assessment

- **Inspected:** Read the enrolled mission, repository instructions and validation policy, current handoffs, developer report, transcript notice, empty prior-findings ledger, 16-file feature diff and candidate-bound gate evidence.; Confirmed HEAD and the commit history. The five whole files specified for extraction at b1c82f31 match their blobs at 46444b30. Inspected the shared-file contributions and the subsequent containment change.; Traced the handle-creation postfix and direct command-start call through binding, command recording, OnAction, terminal formatting and pair-service lifetime. Checked the focused wrapper against Test-Tiers configuration forwarding.
- **Executed verification:** Verified the controller receipt for H against its log SHA-256 66c3425f807877d04f2bfce08af42795bc5085c827545da8a35f744ed40f1ba3. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 exited 0 in 34.7 seconds with the worktree unchanged.; The build passed. Components passed 1534/0, Validate-Source 153/0, the bite native-contract checks 3/0, the bite binding contract 29/0 and presentation 239/0. All 14 focused children passed. The bite contract checked the pinned animation bundle and exercised the production binding with detached Kingmaker handles, actions and managers.; Mounted Shield passed 114/0 and rollback 68/0. The packet, production and embedded icon SHA-256 values all matched fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. Archery, Ranger/Riding Dog, replay, localization, frozen-source and loader checks also passed. The tested product DLL SHA-256 was 575428eafa61692b62cd9febb712d186bacb80b7872082de16ed482c9a5e8070.
- **Source inspection only:** The mounted OnAction override calls its native base once. The Horse-labelled strike reader checks the mount action and KMC Horse blueprint. MissingAfterRecorded returns before a second fallback assignment.; The escaping reporting exception, partial fallback publication, stale terminal attribution and retained command reference follow from the production callers and field writes. They were not reproduced in a running game.
- **Required gaps:** Correct the Bite eligibility, exception handling and binding-state defects in R1-F1 through R1-F3.; Make the strike and terminal evidence accurate across replanning, repeated limbs and unavailable playback data, and release the diagnostic command reference as described in R1-F4 through R1-F7.; Resolve the focused-wrapper artifact selection, extraction evidence and shared-file ownership issues in R1-F8 through R1-F10.
- **Limitations:** The review was read-only. The controller supplied the final gate receipt and log. The foreground developer transcript was withheld, so separate local runs could not be confirmed from it.; The gate reports nativeProof=false. Handle creation and command recording use delegates in the detached binding checks. The run did not observe rendered playback, live damage, timing or spending.; The shared meter recorded 83.41 minutes spent at gate completion, within the enrolled 90-minute allowance. The gate log displays a configured allowance of 150 minutes.

#### Summary

- **Delivered behavior:** The candidate adopts an exact stock Bite, refreshes a later handle of the same action, supplies an initial fallback and prevents a second fallback after registration in the covered cases. It adds per-strike and terminal diagnostics. The Release gate and Shield regression evidence passed.
- **Scope reviewed:** Final PR-5 review of the complete M..H change for MMR-07, S07 and S08. Private-alpha assembly and packaging belong to PR-6.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the shared native routine SpeedScale makes BiteAttack unreadable at normal zoom remains unobserved.
- **Remaining owner review:** N08 / NX-2 remains pending. At normal zoom, correlate the real Bite and following hooves with the strike records and observe facing, rider attachment, cadence, handle completion and the native roll/damage sequence.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a215dd-67c9-76a3-8f58-7a2f81e368e6`, duration 904.2s, response sha256 `6d91556141627e8ebd0b47149277125710235d0efe4f5f6be8643cdf244390e7`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r01-757414`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "22de1f7dec95c60428dc5d8c2e9c66b6f1e295c7",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The required Release gate passed on the reviewed commit. The source still allows a hoof to populate the Bite record, a reporting exception to terminate an attack, and the terminal line to describe a handle the acting strike never used. These are offline correctness issues within S07. Visible playback remains an owner observation.\n\n## Continuation for Claude\n\nUse the actual planned weapon and limb to establish Bite eligibility before recording or supplying a handle. Make assignment and recording consistent under failure, and contain failures from outcome reporting on both adapter entry paths. Keep the command's registration history distinct from the handle observed at the strike. Report the actual limb, retain a strike line when optional playback data is unreadable, and keep log-suppression state on the command's lifetime.\n\nExercise those transitions through the existing focused infrastructure, including the hoof, missing-handle and fault cases in the findings. Correct configuration forwarding in the focused wrapper, remove the Trip-only fixture from this extraction, and supply the preserved extraction-run evidence. Run the enrolled focused gate on the corrected candidate within the existing limits. Preserve native attack planning, timing, rolls, damage, spending and the working hoof sequence. Packaging belongs to PR-6 and N08 remains with the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the controller receipt for H against its log SHA-256 66c3425f807877d04f2bfce08af42795bc5085c827545da8a35f744ed40f1ba3. Test-Tiers.ps1 -Tier Fast -Focused Test-MmrHorseBiteFocused.ps1 exited 0 in 34.7 seconds with the worktree unchanged.",
      "The build passed. Components passed 1534/0, Validate-Source 153/0, the bite native-contract checks 3/0, the bite binding contract 29/0 and presentation 239/0. All 14 focused children passed. The bite contract checked the pinned animation bundle and exercised the production binding with detached Kingmaker handles, actions and managers.",
      "Mounted Shield passed 114/0 and rollback 68/0. The packet, production and embedded icon SHA-256 values all matched fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0. Archery, Ranger/Riding Dog, replay, localization, frozen-source and loader checks also passed. The tested product DLL SHA-256 was 575428eafa61692b62cd9febb712d186bacb80b7872082de16ed482c9a5e8070."
    ],
    "inspected": [
      "Read the enrolled mission, repository instructions and validation policy, current handoffs, developer report, transcript notice, empty prior-findings ledger, 16-file feature diff and candidate-bound gate evidence.",
      "Confirmed HEAD and the commit history. The five whole files specified for extraction at b1c82f31 match their blobs at 46444b30. Inspected the shared-file contributions and the subsequent containment change.",
      "Traced the handle-creation postfix and direct command-start call through binding, command recording, OnAction, terminal formatting and pair-service lifetime. Checked the focused wrapper against Test-Tiers configuration forwarding."
    ],
    "limitations": [
      "The review was read-only. The controller supplied the final gate receipt and log. The foreground developer transcript was withheld, so separate local runs could not be confirmed from it.",
      "The gate reports nativeProof=false. Handle creation and command recording use delegates in the detached binding checks. The run did not observe rendered playback, live damage, timing or spending.",
      "The shared meter recorded 83.41 minutes spent at gate completion, within the enrolled 90-minute allowance. The gate log displays a configured allowance of 150 minutes."
    ],
    "required_gaps": [
      "Correct the Bite eligibility, exception handling and binding-state defects in R1-F1 through R1-F3.",
      "Make the strike and terminal evidence accurate across replanning, repeated limbs and unavailable playback data, and release the diagnostic command reference as described in R1-F4 through R1-F7.",
      "Resolve the focused-wrapper artifact selection, extraction evidence and shared-file ownership issues in R1-F8 through R1-F10."
    ],
    "source_inspection_only": [
      "The mounted OnAction override calls its native base once. The Horse-labelled strike reader checks the mount action and KMC Horse blueprint. MissingAfterRecorded returns before a second fallback assignment.",
      "The escaping reporting exception, partial fallback publication, stale terminal attribution and retained command reference follow from the production callers and field writes. They were not reproduced in a running game."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The context check compares the planned attack and weapon with themselves. The binder accepts the planned weapon's special type, including Hoof, and treats None as a wildcard during fallback. The passing hoof control explicitly adopts a hoof, and the component test supplies a missing hoof. Both outcomes feed reporting labelled as the exact KMC Bite.",
      "blocking_rationale": "A hoof can acquire a new handle or become the command's Bite record. That violates the explicit hoof-preservation and Bite-attribution requirements.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only the verified native Bite limb and matching Bite action may populate the Bite registration or receive its fallback. The strike reader can still describe each hoof.",
      "id": "R1-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 78
      },
      "priority": "P1",
      "requested_correction": "Prove the planned Bite weapon and slot independently of the current planned-attack reference. Require a proven Bite action before adoption or creation, including when the weapon reports None.",
      "requested_evidence": "Cover a current hoof with and without a stock handle before any Bite registration, and an untyped weapon with only a Hoof candidate. Show zero Bite records and assignments for those cases alongside successful exact Bite adoption and fallback.",
      "title": "The Bite binding accepts hoof actions",
      "trigger": "A hoof is the current planned attack before any Bite handle has been recorded, or a missing handle has no expected special type and the sole candidate is Hoof."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The catch ends before the outcome switch. Remember reads Unity metadata and the switch calls Info or Error outside containment. The catch itself calls logger.Exception without protection. On the direct CreateAndValidateChildAttack path, an escaping exception reaches OnStart's EndAfterFault.",
      "blocking_rationale": "A diagnostic failure can end an otherwise legal Horse attack. The compiled check covers the context test, constructor and Bind call only.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Presentation reporting returns control to native attack processing even when metadata access or logging fails.",
      "id": "R1-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 97
      },
      "priority": "P1",
      "requested_correction": "Contain the outcome reads and logging as well as binding. Make reporting the original exception safe if the logger also throws.",
      "requested_evidence": "Inject a throwing outcome logger and a throwing exception logger. Show that SupplyExact returns normally through the command-start path and preserves the native plan.",
      "title": "Outcome reporting can still fault the attack",
      "trigger": "An adoption, rejection or missing-handle report throws, or the logger fails while reporting an earlier binding exception."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "Bind assigns the native handle before calling Record. The production record also publishes handle, action and source before reading the Unity action name. A recording fault can therefore leave an assigned fallback or partial command record while the catch reports that the native strike was left as planned.",
      "blocking_rationale": "Catching the exception does not restore consistency between the native slot and the exactly-once record. A retry can observe the wrong source or an incomplete registration.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A failed binding leaves the prior slot and registration consistent. A committed fallback always retains its correct source.",
      "id": "R1-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseStrikeHandleBinding.cs",
        "line": 154
      },
      "priority": "P2",
      "requested_correction": "Complete fallible validation and metadata reads before publishing the slot and record, or undo the exact partial writes on failure. Apply the same consistency rule to refresh.",
      "requested_evidence": "Inject a recording or metadata fault and inspect both the planned slot and command record immediately and on retry. Verify the retained source and assignment count.",
      "title": "A recording fault can leave a partially committed fallback",
      "trigger": "Handle creation succeeds and the subsequent command registration fails."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "MissingAfterRecorded deliberately preserves the earlier handle. The terminal outcome reads all Bite fields from that handle, even when the replanned Bite acted with no handle. If no handle was ever recorded, the terminal formatter omits the entire field.",
      "blocking_rationale": "The terminal evidence can attribute an earlier handle's action, speed and state to the very strike whose missing playback the owner is investigating.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The terminal line distinguishes the Bite handle observed at the strike, an absent handle and a Bite that never reached the strike boundary.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1888
      },
      "priority": "P2",
      "requested_correction": "Keep the historical registration needed for idempotence. Track the actual Bite observation separately and use its disposition in the Horse terminal fields.",
      "requested_evidence": "Check the emitted terminal fields after adoption followed by a handle-less Bite, after a valid Bite followed by a handle-less hoof, and after an exact Horse command with no observed Bite.",
      "title": "The terminal Bite summary can describe a superseded handle",
      "trigger": "Native replans the Bite without a handle after an earlier adoption or supply."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "GetAttackIndex() + 1 provides the routine ordinal. HorseStrikeSource never reads the planned Hand for the strike line. Both hoof slots can have the same weapon blueprint, and multiple routine entries can use the same limb.",
      "blocking_rationale": "The required limb field cannot be recovered reliably from the ordinal and weapon name.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "Each strike identifies its actual native hand or additional-limb slot as well as its position in the routine.",
      "id": "R1-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1016
      },
      "priority": "P2",
      "requested_correction": "Resolve the planned Hand against the Horse's actual slots and include that identity in the strike record. Keep the routine ordinal as a separate field.",
      "requested_evidence": "Show distinct slot identities for two hooves sharing a blueprint and the same slot identity for repeated strikes from one limb.",
      "title": "The strike line reports sequence position but omits the planned limb",
      "trigger": "A routine contains the two Hoof1d4 slots or an additional strike from an existing limb."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "HorseStrikeReader gathers every field before logging. If a native getter such as the active clip's Unity name throws, ObserveHorseStrike catches the exception and emits nothing, including the already available weapon and handle state.",
      "blocking_rationale": "The owner loses the entire strike precisely when a playback read fails. This breaks the one-line-per-strike diagnostic contract.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A recognized Horse strike still emits one line with available facts and an explicit unavailable observation.",
      "id": "R1-F6",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1020
      },
      "priority": "P2",
      "requested_correction": "Retain the basic strike record when optional playback reads fail. Distinguish an unreadable observation from a positively absent clip or handle.",
      "requested_evidence": "Use a source whose clip or weight getter throws with a working logger. Verify one identifiable strike line and continued native action delegation.",
      "title": "An unreadable playback field erases the strike record",
      "trigger": "An interrupted or invalidated animation object faults while the reader samples its playback data."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "lastMissingCommand is a strong reference with no cleanup path. Pair services are reused across admission and world boundaries. The retained command holds the rider, mount, target and animation references until another missing-handle command replaces it or the service is discarded.",
      "blocking_rationale": "Log suppression introduces old-command and actor retention across the lifecycle boundaries the mission requires preserving.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A once-per-command diagnostic releases ownership with that command.",
      "id": "R1-F7",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 103
      },
      "priority": "P2",
      "requested_correction": "Put the reported flag on the command or use non-owning tracking whose lifetime cannot retain the command after retirement.",
      "requested_evidence": "Verify repeated callbacks report once, a subsequent command can report independently, and the pair adapter retains no finished command after retirement.",
      "title": "Missing-handle log suppression retains a finished command",
      "trigger": "A MissingAfterRecorded command ends and the pair dismounts or the world is replaced."
    },
    {
      "acceptance_ids": [
        "S07",
        "S08"
      ],
      "actual": "Test-Tiers forwards Configuration to focused scripts that declare it. This wrapper declares no parameters and passes no configuration to its children. With a Debug tier build, product-reading children still open bin/Release and can pass against stale bytes.",
      "blocking_rationale": "A supported invocation can report validation for a different artifact. The supplied default Release receipt remains valid.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Product-reading children inspect the configuration selected by the parent tier.",
      "id": "R1-F8",
      "location": {
        "file": "scripts/Test-MmrHorseBiteFocused.ps1",
        "line": 2
      },
      "priority": "P2",
      "requested_correction": "Accept the tier configuration and forward it to children that support it, including the bite, presentation, Shield and loader checks.",
      "requested_evidence": "Show with a bounded configuration-routing check that a Debug selection resolves the Debug product and the enrolled Release invocation still resolves the Release product.",
      "title": "The focused wrapper drops the selected build configuration",
      "trigger": "Test-Tiers runs the new focused wrapper with -Configuration Debug."
    },
    {
      "acceptance_ids": [
        "MMR-07"
      ],
      "actual": "The supplied receipt and log bind to 22de1f7d and report 29 bite checks. Section 5 lists a 28-check extraction run, but the supplied evidence contains no run output or meter entry tied to b1c82f31.",
      "blocking_rationale": "The final gate establishes the final candidate's result. It cannot establish the separately specified extraction-commit proof.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The metered Fast and focused result named for the extraction is traceable to its original commit.",
      "id": "R1-F9",
      "location": {
        "file": "docs/missions/mmr-split/PR-5-horse-bite.md",
        "line": 116
      },
      "priority": "P2",
      "requested_correction": "Provide the preserved extraction-run evidence through the review evidence channel.",
      "requested_evidence": "The original log or receipt identifying b1c82f31, the selected command, child counts and associated shared-budget entry.",
      "title": "The extraction run lacks independently inspectable provenance",
      "trigger": "Verifying the mission's extraction proof at b1c82f31."
    },
    {
      "acceptance_ids": [
        "MMR-07"
      ],
      "actual": "The shared test file also adds LoadedUnit. It has no use on this branch and belongs to the saved-Horse repair test on the Trip branch.",
      "blocking_rationale": "Section 5 explicitly assigns this shared-file contribution to the formatter case. The extra declaration gives two children ownership of the same fixture.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "PR-5's contribution to MountedStockAttackPolicyTests contains its strike formatter case and registration.",
      "id": "R1-F10",
      "location": {
        "file": "tests/MountedStockAttackPolicyTests.cs",
        "line": 446
      },
      "priority": "P3",
      "requested_correction": "Remove the unused LoadedUnit declaration from PR-5.",
      "requested_evidence": "The shared-file diff shows the formatter case and its registration.",
      "title": "A Trip test fixture was included in the Bite extraction",
      "trigger": "Comparing the shared test-file delta with the mission's extraction ownership."
    },
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "The terminal speed uses the process culture inside a comma-delimited field list. A speed of 2.4 can appear as speedScale=2,4, while the strike line uses invariant formatting.",
      "blocking_rationale": "The per-strike value remains available in invariant form. This is a readability improvement for the terminal line.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "Both diagnostic lines render the same numeric value consistently.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedCombatController.cs",
        "line": 2329
      },
      "priority": "P3",
      "requested_correction": "Use invariant culture for terminal SpeedScale.",
      "requested_evidence": "A terminal formatting example under a decimal-comma culture.",
      "title": "Terminal speed formatting varies by locale",
      "trigger": "The owner's process uses a decimal-comma culture."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r01-757414",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adopts an exact stock Bite, refreshes a later handle of the same action, supplies an initial fallback and prevents a second fallback after registration in the covered cases. It adds per-strike and terminal diagnostics. The Release gate and Shield regression evidence passed.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08 / NX-2 remains pending. At normal zoom, correlate the real Bite and following hooves with the strike records and observe facing, rider attachment, cadence, handle completion and the native roll/damage sequence."
    ],
    "scope_reviewed": "Final PR-5 review of the complete M..H change for MMR-07, S07 and S08. Private-alpha assembly and packaging belong to PR-6.",
    "unresolved_questions": [
      "Whether the shared native routine SpeedScale makes BiteAttack unreadable at normal zoom remains unobserved."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
