<!-- agent-review:v1 enrollment=kmc-codex-mmr-horse-bite-20261125221703-5f10 key=r02-review part=1/1 sha256=39d66fdc4dd57b6e1cdd4a9eec17024ec23328b5819bcf15c8d3edee597d60fd -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `97dce9a945`

**Decision:** `changes_required` — The candidate-bound Release gate passed, including the Mounted Shield checks. S07 still has a binding defect and reporting defects. An independently identifiable Bite can lose its fallback, a diagnostic read can prevent native OnAction from running, and the structured terminal evidence can credit a handle absent from the strike. The metadata-fault check and extraction provenance also have specific evidence gaps.

Continuation for Claude

Round 2 uses the final allocated PR-5 review. Preserve that stop. For any further owner-authorized correction, identify the physical Bite from its registered weapon and runtime slot, then validate the Horse manager's Bite action separately. Contain the complete strike observation and use the observed handle, its source and any unreadable state consistently in the strike line and both terminal outcome paths. Keep native attack planning, timing, expenditure, hooves and the existing evidence thresholds intact.

Exercise the affected transitions through the existing offline checks, including the actual terminal producer and a metadata fault that passes the command's validation guard. Supply evidence tying the extraction checks to their source and test inputs. Bind the focused Fast gate to the corrected candidate. N08 remains Howie's playtest observation, and packaging belongs to PR-6.

#### Findings

**R2-F1** · P1 · defect · blocks readiness — An unset visual tag disables the exact Bite fallback
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:185`; acceptance: MMR-07, S07
- Trigger: The registered Bite occupies limb0, stock supplies no handle, the weapon's visual tag is None, and the Horse manager exposes one eligible Bite action.
- Expected: The registered Bite weapon in its proven slot is recognizable independently of its presentation tag. A missing handle can receive the single verified native Bite action.
- Actual: IsBiteLimb requires VisualParameters.SpecialAnimation == Bite even after matching the runtime slot and blueprint. None returns NotExact before candidate selection. The same predicate causes a delivered physical Bite to leave the terminal observation at not-reached. The base adapter supported a single-action fallback in this case, and the repository's Horse animation contract documents the native no-handle path for an unmarked additional limb.
- Why it blocks / does not: The correction excludes the exact missing-handle case that the fallback exists to repair and can misreport a delivered Bite.
- Requested correction: Use the registered Bite weapon identity and actual slot to identify the strike. For fallback, require the unique matching Bite action on the Horse manager. Preserve rejection of foreign weapons, hoof-only sets and ambiguous Bite actions.
- Requested evidence: Exercise the proven Bite weapon in limb0 with SpecialAnimation.None, no handle and one genuine Bite candidate. Verify one supply and a Bite observation. Repeat with only a Hoof candidate and with an unrelated untyped weapon, and verify zero writes.

**R2-F2** · P1 · defect · blocks readiness — A failed planned-attack read can bypass native OnAction
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1031`; acceptance: MMR-07, S07
- Trigger: The exact Horse reaches the act callback while its plan or limb metadata cannot be read.
- Expected: The observer returns control to the native action after a failed read and records uncertainty for a recognized Horse callback.
- Actual: The correction moved `var planned = PlannedAttack` outside both guards. An exception exits ObserveHorseStrike before the subsequent base.OnAction call. The repository's NativeOrdinaryAttackTrace already guards this native getter when the backing plan is unavailable. A later failure during IsHorseBiteSlot is swallowed and leaves the previous observation or the default not-reached state.
- Why it blocks / does not: A presentation probe can interrupt attack execution or turn a failed observation into a definite claim about which strike occurred.
- Requested correction: Include plan acquisition and limb classification in the contained observation. Retain the available strike identity and an explicit unreadable disposition when classification fails. Keep the single native delegation.
- Requested evidence: Inject a failing plan read and a failing limb read at the observation boundary. Verify one native delegation, one identifiable diagnostic where the Horse is known, and an unreadable terminal disposition. Include a prior observed Bite to catch stale reuse.

**R1-F4** · P2 · defect · blocks readiness — Structured terminal evidence still credits the superseded handle
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1921`; acceptance: MMR-07, S07
- Trigger: Stock or KMC registers a Bite handle and the replanned Bite subsequently strikes without it.
- Expected: Terminal evidence describing the strike agrees with the handle actually observed at its act boundary. Command registration remains available for idempotence and binding counts.
- Actual: HorseBiteTerminal can correctly print observed=no-handle while DescribeOutcome still sets AttackAnimationHandleCreated, source, action and state from horsePrimaryAnimationHandle. HorseCompanionUnmountedScenarioEngine captures those fields and uses them in its TB and RT animation checks. The same handle-less strike can therefore produce a negative text observation and positive structured handle evidence.
- Why it blocks / does not: The superseded-handle attribution persists in a terminal outcome consumed as animation evidence.
- Requested correction: Carry the observed Bite disposition and its source through the structured terminal outcome. Keep historical binding facts distinguishable wherever the current evidence consumer judges playback.
- Requested evidence: After adoption followed by a handle-less Bite, inspect the actual terminal outcome and its existing structured projection as well as the formatted line. The animation check must reject that observation. Retain a positive observed Bite followed by a hoof.

**R2-F3** · P2 · defect · blocks readiness — The minimal terminal outcome drops the Bite observation
- Location: `src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs:1791`; acceptance: MMR-07, S07
- Trigger: An exact Horse command terminates and DescribeOutcome throws before its final property assignment.
- Expected: Both terminal paths publish the command's Bite disposition, including an explicit unreadable value when necessary.
- Actual: HorseBiteTerminal is assigned only at the end of DescribeOutcome. ReportTerminalOnce constructs a separate minimal outcome when an earlier read throws. That initializer omits HorseBiteTerminal, so the controller drops the entire field even if the command already captured the Bite.
- Why it blocks / does not: An unrelated terminal reporting fault erases the required Bite observation on an exact Horse command.
- Requested correction: Populate the contained Bite description on the common terminal path after choosing the detailed or minimal outcome.
- Requested evidence: Force an unrelated detailed-outcome read to fail after a known Bite observation. Verify one terminal callback carrying the observed disposition. Include a non-Horse command as a control.

**R2-F4** · P2 · defect · blocks readiness — A handle metadata fault suppresses readable playback
- Location: `src/KingmakerMountedCombat/Domain/HorseStrikeHandleBinding.cs:277`; acceptance: MMR-07, S07
- Trigger: The handle is present and a metadata getter fails while its active animation remains readable.
- Expected: A readable handle permits independent sampling of playback even when a handle metadata getter fails.
- Actual: The handle-facts catch returns immediately, before ActiveClip, ActiveWeight or ActiveState is sampled. HandleText also returns immediately on HandleFactsUnreadable. For example, a throwing VariantsCount getter discards the already-read action and speed and suppresses a separately readable BiteAttack clip.
- Why it blocks / does not: The owner's strike record can lose the available evidence needed to distinguish a playing Bite clip from an absent animation.
- Requested correction: Continue to the independent playback reads after a handle-facts fault, and format the available values alongside the unreadable marker.
- Requested evidence: Make the variant-count getter throw on a handle with a readable BiteAttack clip, weight and state. Verify that the single strike line retains those playback facts and identifies the failed read.

**R2-F5** · P2 · evidence_gap · blocks readiness — The metadata-fault check can pass at the validation guard
- Location: `scripts/Test-MountedAlpha2BiteContract.ps1:282`; acceptance: MMR-07, S07
- Trigger: The new detached-object regression invokes RecordHorsePrimaryAnimation with its uninitialized action.
- Expected: The regression reaches the metadata read after the production preconditions succeed and detects publication before that read.
- Actual: The detached test passes any exception plus empty fields. New-Action creates an uninitialized Unity object. Its zero native pointer can satisfy the command's `animationAction == null` rejection before the name getter runs. The neighboring Shield rollback fixture explicitly accounts for this Unity equality behavior. The assertion does not distinguish the two exceptions.
- Why it blocks / does not: The new check can pass with the original partial-publication bug restored. Its PASS label overstates what the genuine-object run establishes.
- Requested correction: Use a safe, distinguishable metadata fault after validation and assert its cause and the resulting production record. Keep the existing fallback rollback and retry case.
- Requested evidence: Show that the test reached the intended metadata operation and that moving the record writes before that operation makes the check fail.

**R1-F9** · P2 · evidence_gap · blocks readiness — The extraction log is preserved but its tested tree is unbound
- Location: `docs/missions/mmr-split/evidence/PR-5-extraction-run-b1c82f31.md:15`; acceptance: MMR-07
- Trigger: Verifying the separately required 28-check extraction result at b1c82f31.
- Expected: The metered extraction result is traceable to the production and test inputs at b1c82f31.
- Actual: The preserved log proves the command, counts, DLL hash and budget entry. It ended 42 seconds before b1c82f31 was committed and contains no tree identity. The reflog confirms a later checkout for the reported rebuild. Its matching DLL digest is stated in the provenance note, and even a matching production DLL leaves the component executable and focused PowerShell inputs unidentified.
- Why it blocks / does not: Section 5 requires extraction-commit proof. The final candidate receipt and the pre-commit log establish different runs.
- Requested correction: Attach available execution evidence binding the tested tree or its production and test inputs to b1c82f31. If a separately authorized check on that frozen commit supplies the proof, retain its own date and identity alongside the original run.
- Requested evidence: A metered focused receipt naming the extraction commit, or contemporaneous input identities and independently inspectable comparison output covering the DLL, component executable and selected scripts.

**R2-F6** · P3 · optional_polish · non-blocking — The routing probe depends on child source spelling
- Location: `scripts/Test-MmrHorseBiteFocused.ps1:56`; acceptance: S07, S08
- Trigger: A product-reading child changes how it constructs the same configuration-specific DLL path.
- Expected: The routing check describes the argument or resolved path it actually observed.
- Actual: The wrapper searches the entire child script for one of three literal spellings and prints a resolved path assembled from the probe name. Equivalent path construction can fail this check, while a matching comment can satisfy it.
- Why it blocks / does not: Configuration forwarding is correct in this candidate. The issue concerns maintenance and the precision of the probe's output.
- Requested correction: Keep the argument-routing assertion and use a path-resolution probe where available, or limit the output to the forwarding it verified.
- Requested evidence: A bounded Debug/Release routing check that tolerates an equivalent change in path-expression spelling.

**R2-F7** · P3 · optional_polish · non-blocking — A reporting fault can be logged as a failed binding
- Location: `src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs:151`; acceptance: S07
- Trigger: The fallback commits successfully and the subsequent metadata or Info report throws.
- Expected: The fault message states which operation failed or leaves the binding outcome open.
- Actual: ReportFault says binding failed and the strike was left as planned even when the fallback was successfully assigned and recorded and only its outcome report threw.
- Why it blocks / does not: The committed handle and command record survive this reporting failure.
- Requested correction: Use fault wording valid for both binding and post-binding reporting failures, or distinguish the two stages.
- Requested evidence: A successful supply followed by a throwing outcome reporter and a working fault logger produces a message consistent with the retained fallback.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — The runtime slot and blueprint checks exclude both hooves. The binder also requires the action's own Bite type. Component and genuine-object cases confirm zero records and assignments for the original hoof cases. R2-F1 covers the distinct loss of a valid fallback.
- **R1-F2** — `fix_verified` — SupplyExact routes construction, binding and outcome reporting through SupplyContained. Its nested handler absorbs a throwing fault logger. The injected report failures passed, and both production callers reach this containment.
- **R1-F3** — `fix_verified` — Record and refresh read metadata before publishing fields. Bind restores the prior slot if Record throws. The component fault-and-retry case verifies slot state, source and assignment counts. R2-F5 concerns the separate genuine-object test's claim about its fault location.
- **R1-F4** — `fix_rejected` — The three requested normal text dispositions are correct. On the same adopted-then-handle-less sequence, the structured terminal outcome still exposes the earlier handle to the existing Horse animation checks.
- **R1-F5** — `fix_verified` — HorseStrikeSource resolves the planned Hand by reference against the Horse's body. The checked cases distinguish limb1 from limb2 and retain the slot across repeated strikes with different routine ordinals.
- **R1-F6** — `fix_verified` — The throwing-clip case now produces an identifiable strike line with its basic handle facts and an explicit unreadable playback marker. R2-F4 covers the converse failure, where handle metadata suppresses readable playback.
- **R1-F7** — `fix_verified` — Missing-handle suppression now belongs to the command. Repeated callbacks reported once, a subsequent command reported independently, and the adapter's retained command reference is gone.
- **R1-F8** — `fix_verified` — The wrapper accepts the tier configuration and forwards it to each declaring child. I checked the product path expressions and loader resolver. The Release receipt and two-configuration argument probe support the routing fix.
- **R1-F9** — `fix_rejected` — The original output, meter entry and timeline are now inspectable. The missing link is the identity of the pre-commit product and test inputs as a set. The later checkout and reported product rebuild do not establish that link.
- **R1-F10** — `fix_verified` — The shared test-file diff contains the strike formatter case, its helper and registration. The unused Trip fixture was removed.
- **R1-F11** — `fix_verified` — Terminal and strike numeric fields use invariant formatting. The executed de-DE case verifies decimal points in SpeedScale and clip weight.

#### Evidence assessment

- **Inspected:** The enrolled mission, repository validation policy and handoffs, full feature diff, round-2 correction diff, developer report, prior ledger, controller receipt and raw gate logs.; Horse blueprint construction and native limb-selection contract, both binding entry points, the strike reader, OnAction, normal and minimal terminal outcomes, and the existing consumer of structured Horse animation evidence.; Component and detached-object tests, focused-wrapper configuration forwarding, child product paths, extraction records and relevant Git provenance.
- **Executed verification:** Verified the detached HEAD, merge base and enrolled mission digest. The five whole files assigned to the extraction are identical between 46444b30 and b1c82f31.; Verified the controller receipt for 97dce9a945394306e0bf365095c640ae1739cb4b against log SHA-256 2e62506979c20d117dbaff470e403dd75379f014f01a00b98c5c0e40a2925202. The focused Release gate exited 0 in 37.7 seconds with the worktree unchanged. Fast passed 4/4 and the focused wrapper passed 14/14.; The candidate log records components 1541/0, Validate-Source 153/0, Bite native contract 3/0, Bite binding contract 35/0, presentation 239/0, Shield 114/0, rollback 68/0, Archery 54/0 and Ranger/Riding Dog 183/0. Both UMM profiles bound all 20 referenced loader members. The tested DLL SHA-256 was 0aaa0f87e5fa0cd6e786bdde5490dd8758bad9779996452e8d7b22400b72963a. The packet, production and embedded Shield icon all hashed to fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.; Verified the archived extraction log's Git blob SHA-256 c2619aafd873583b62e38cbeca869c836b2808d1d3ca5b49af8f2191989374f4. It records the original budget entry, 1534/0 components, 28/0 Bite checks and 14/14 focused children. The reflog corroborates the commit timing and later checkout of b1c82f31.
- **Source inspection only:** Traced the command-start call and native postfix to SupplyExact. The throwing report delegates ran through the production containment helper offline. The full game command-start sequence was inspected in source.; Traced the failure paths in the findings from the incident-producing reads through their callers and evidence consumers. The candidate gate did not exercise those complete transitions.; The installed bundle identity check passed. The BiteAttack mapping has historical asset evidence. Current visible playback remains unobserved.
- **Required gaps:** Restore the exact fallback for a proven Bite with an unset visual tag and retain native delegation when strike observation faults.; Make readable playback and actual Bite observations survive partial reads, structured reporting and the minimal terminal path.; Demonstrate a fault after production record validation and bind the historical extraction gate to its tested inputs.
- **Limitations:** This was a read-only review. Executed gate results come from the controller receipt and log. The supplied developer transcript withholds the mixed foreground session.; The candidate gate executed Release. Its Debug probe checked argument routing. Native handle creation, rendered playback, live attack timing and damage were outside the offline run.; The shared meter printed 86.87 minutes spent at gate completion. PR-5 has used both allocated formal rounds.

#### Summary

- **Delivered behavior:** For a typed Bite in the verified Horse slot, the candidate adopts a stock handle, refreshes the same action and supplies a single eligible fallback. Hooves retain their native handles. Normal strike lines identify the limb and carry playback facts, and normal terminal text distinguishes an observed handle, a handle-less Bite and an unreached Bite.
- **Scope reviewed:** Final PR-5 MMR-07, S07 and the S08 regression boundary at the fixed candidate, including all eleven prior findings and directly affected evidence consumers.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the installed BiteAttack clip is readable at normal zoom and whether native routine SpeedScale accounts for the owner's observation.
- **Remaining owner review:** N08 at normal play zoom. Correlate the real Bite with its strike log and compare the hoof sequence, facing, rider attachment, timing and attack or damage count.; Any additional formal PR-5 review requires an owner decision after the two-round allocation.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: none.

Codex CLI `0.170.0`, thread `01a215fe-d49a-70c2-b38d-6b1f30400fba`, duration 907.3s, response sha256 `03aa1f2b8373a13926395000e8f1e235db831216b5ac048bba376770dcd7e819`, request `kmc-codex-mmr-horse-bite-20261125221703-5f10-r02-e8061d`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "97dce9a945394306e0bf365095c640ae1739cb4b",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The candidate-bound Release gate passed, including the Mounted Shield checks. S07 still has a binding defect and reporting defects. An independently identifiable Bite can lose its fallback, a diagnostic read can prevent native OnAction from running, and the structured terminal evidence can credit a handle absent from the strike. The metadata-fault check and extraction provenance also have specific evidence gaps.\n\nContinuation for Claude\n\nRound 2 uses the final allocated PR-5 review. Preserve that stop. For any further owner-authorized correction, identify the physical Bite from its registered weapon and runtime slot, then validate the Horse manager's Bite action separately. Contain the complete strike observation and use the observed handle, its source and any unreadable state consistently in the strike line and both terminal outcome paths. Keep native attack planning, timing, expenditure, hooves and the existing evidence thresholds intact.\n\nExercise the affected transitions through the existing offline checks, including the actual terminal producer and a metadata fault that passes the command's validation guard. Supply evidence tying the extraction checks to their source and test inputs. Bind the focused Fast gate to the corrected candidate. N08 remains Howie's playtest observation, and packaging belongs to PR-6.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the detached HEAD, merge base and enrolled mission digest. The five whole files assigned to the extraction are identical between 46444b30 and b1c82f31.",
      "Verified the controller receipt for 97dce9a945394306e0bf365095c640ae1739cb4b against log SHA-256 2e62506979c20d117dbaff470e403dd75379f014f01a00b98c5c0e40a2925202. The focused Release gate exited 0 in 37.7 seconds with the worktree unchanged. Fast passed 4/4 and the focused wrapper passed 14/14.",
      "The candidate log records components 1541/0, Validate-Source 153/0, Bite native contract 3/0, Bite binding contract 35/0, presentation 239/0, Shield 114/0, rollback 68/0, Archery 54/0 and Ranger/Riding Dog 183/0. Both UMM profiles bound all 20 referenced loader members. The tested DLL SHA-256 was 0aaa0f87e5fa0cd6e786bdde5490dd8758bad9779996452e8d7b22400b72963a. The packet, production and embedded Shield icon all hashed to fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.",
      "Verified the archived extraction log's Git blob SHA-256 c2619aafd873583b62e38cbeca869c836b2808d1d3ca5b49af8f2191989374f4. It records the original budget entry, 1534/0 components, 28/0 Bite checks and 14/14 focused children. The reflog corroborates the commit timing and later checkout of b1c82f31."
    ],
    "inspected": [
      "The enrolled mission, repository validation policy and handoffs, full feature diff, round-2 correction diff, developer report, prior ledger, controller receipt and raw gate logs.",
      "Horse blueprint construction and native limb-selection contract, both binding entry points, the strike reader, OnAction, normal and minimal terminal outcomes, and the existing consumer of structured Horse animation evidence.",
      "Component and detached-object tests, focused-wrapper configuration forwarding, child product paths, extraction records and relevant Git provenance."
    ],
    "limitations": [
      "This was a read-only review. Executed gate results come from the controller receipt and log. The supplied developer transcript withholds the mixed foreground session.",
      "The candidate gate executed Release. Its Debug probe checked argument routing. Native handle creation, rendered playback, live attack timing and damage were outside the offline run.",
      "The shared meter printed 86.87 minutes spent at gate completion. PR-5 has used both allocated formal rounds."
    ],
    "required_gaps": [
      "Restore the exact fallback for a proven Bite with an unset visual tag and retain native delegation when strike observation faults.",
      "Make readable playback and actual Bite observations survive partial reads, structured reporting and the minimal terminal path.",
      "Demonstrate a fault after production record validation and bind the historical extraction gate to its tested inputs."
    ],
    "source_inspection_only": [
      "Traced the command-start call and native postfix to SupplyExact. The throwing report delegates ran through the production containment helper offline. The full game command-start sequence was inspected in source.",
      "Traced the failure paths in the findings from the incident-producing reads through their callers and evidence consumers. The candidate gate did not exercise those complete transitions.",
      "The installed bundle identity check passed. The BiteAttack mapping has historical asset evidence. Current visible playback remains unobserved."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "IsBiteLimb requires VisualParameters.SpecialAnimation == Bite even after matching the runtime slot and blueprint. None returns NotExact before candidate selection. The same predicate causes a delivered physical Bite to leave the terminal observation at not-reached. The base adapter supported a single-action fallback in this case, and the repository's Horse animation contract documents the native no-handle path for an unmarked additional limb.",
      "blocking_rationale": "The correction excludes the exact missing-handle case that the fallback exists to repair and can misreport a delivered Bite.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The registered Bite weapon in its proven slot is recognizable independently of its presentation tag. A missing handle can receive the single verified native Bite action.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 185
      },
      "priority": "P1",
      "requested_correction": "Use the registered Bite weapon identity and actual slot to identify the strike. For fallback, require the unique matching Bite action on the Horse manager. Preserve rejection of foreign weapons, hoof-only sets and ambiguous Bite actions.",
      "requested_evidence": "Exercise the proven Bite weapon in limb0 with SpecialAnimation.None, no handle and one genuine Bite candidate. Verify one supply and a Bite observation. Repeat with only a Hoof candidate and with an unrelated untyped weapon, and verify zero writes.",
      "title": "An unset visual tag disables the exact Bite fallback",
      "trigger": "The registered Bite occupies limb0, stock supplies no handle, the weapon's visual tag is None, and the Horse manager exposes one eligible Bite action."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The correction moved `var planned = PlannedAttack` outside both guards. An exception exits ObserveHorseStrike before the subsequent base.OnAction call. The repository's NativeOrdinaryAttackTrace already guards this native getter when the backing plan is unavailable. A later failure during IsHorseBiteSlot is swallowed and leaves the previous observation or the default not-reached state.",
      "blocking_rationale": "A presentation probe can interrupt attack execution or turn a failed observation into a definite claim about which strike occurred.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The observer returns control to the native action after a failed read and records uncertainty for a recognized Horse callback.",
      "id": "R2-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1031
      },
      "priority": "P1",
      "requested_correction": "Include plan acquisition and limb classification in the contained observation. Retain the available strike identity and an explicit unreadable disposition when classification fails. Keep the single native delegation.",
      "requested_evidence": "Inject a failing plan read and a failing limb read at the observation boundary. Verify one native delegation, one identifiable diagnostic where the Horse is known, and an unreadable terminal disposition. Include a prior observed Bite to catch stale reuse.",
      "title": "A failed planned-attack read can bypass native OnAction",
      "trigger": "The exact Horse reaches the act callback while its plan or limb metadata cannot be read."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "HorseBiteTerminal can correctly print observed=no-handle while DescribeOutcome still sets AttackAnimationHandleCreated, source, action and state from horsePrimaryAnimationHandle. HorseCompanionUnmountedScenarioEngine captures those fields and uses them in its TB and RT animation checks. The same handle-less strike can therefore produce a negative text observation and positive structured handle evidence.",
      "blocking_rationale": "The superseded-handle attribution persists in a terminal outcome consumed as animation evidence.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Terminal evidence describing the strike agrees with the handle actually observed at its act boundary. Command registration remains available for idempotence and binding counts.",
      "id": "R1-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1921
      },
      "priority": "P2",
      "requested_correction": "Carry the observed Bite disposition and its source through the structured terminal outcome. Keep historical binding facts distinguishable wherever the current evidence consumer judges playback.",
      "requested_evidence": "After adoption followed by a handle-less Bite, inspect the actual terminal outcome and its existing structured projection as well as the formatted line. The animation check must reject that observation. Retain a positive observed Bite followed by a hoof.",
      "title": "Structured terminal evidence still credits the superseded handle",
      "trigger": "Stock or KMC registers a Bite handle and the replanned Bite subsequently strikes without it."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "HorseBiteTerminal is assigned only at the end of DescribeOutcome. ReportTerminalOnce constructs a separate minimal outcome when an earlier read throws. That initializer omits HorseBiteTerminal, so the controller drops the entire field even if the command already captured the Bite.",
      "blocking_rationale": "An unrelated terminal reporting fault erases the required Bite observation on an exact Horse command.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Both terminal paths publish the command's Bite disposition, including an explicit unreadable value when necessary.",
      "id": "R2-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPairAttackCommand.cs",
        "line": 1791
      },
      "priority": "P2",
      "requested_correction": "Populate the contained Bite description on the common terminal path after choosing the detailed or minimal outcome.",
      "requested_evidence": "Force an unrelated detailed-outcome read to fail after a known Bite observation. Verify one terminal callback carrying the observed disposition. Include a non-Horse command as a control.",
      "title": "The minimal terminal outcome drops the Bite observation",
      "trigger": "An exact Horse command terminates and DescribeOutcome throws before its final property assignment."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The handle-facts catch returns immediately, before ActiveClip, ActiveWeight or ActiveState is sampled. HandleText also returns immediately on HandleFactsUnreadable. For example, a throwing VariantsCount getter discards the already-read action and speed and suppresses a separately readable BiteAttack clip.",
      "blocking_rationale": "The owner's strike record can lose the available evidence needed to distinguish a playing Bite clip from an absent animation.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A readable handle permits independent sampling of playback even when a handle metadata getter fails.",
      "id": "R2-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/HorseStrikeHandleBinding.cs",
        "line": 277
      },
      "priority": "P2",
      "requested_correction": "Continue to the independent playback reads after a handle-facts fault, and format the available values alongside the unreadable marker.",
      "requested_evidence": "Make the variant-count getter throw on a handle with a readable BiteAttack clip, weight and state. Verify that the single strike line retains those playback facts and identifies the failed read.",
      "title": "A handle metadata fault suppresses readable playback",
      "trigger": "The handle is present and a metadata getter fails while its active animation remains readable."
    },
    {
      "acceptance_ids": [
        "MMR-07",
        "S07"
      ],
      "actual": "The detached test passes any exception plus empty fields. New-Action creates an uninitialized Unity object. Its zero native pointer can satisfy the command's `animationAction == null` rejection before the name getter runs. The neighboring Shield rollback fixture explicitly accounts for this Unity equality behavior. The assertion does not distinguish the two exceptions.",
      "blocking_rationale": "The new check can pass with the original partial-publication bug restored. Its PASS label overstates what the genuine-object run establishes.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The regression reaches the metadata read after the production preconditions succeed and detects publication before that read.",
      "id": "R2-F5",
      "location": {
        "file": "scripts/Test-MountedAlpha2BiteContract.ps1",
        "line": 282
      },
      "priority": "P2",
      "requested_correction": "Use a safe, distinguishable metadata fault after validation and assert its cause and the resulting production record. Keep the existing fallback rollback and retry case.",
      "requested_evidence": "Show that the test reached the intended metadata operation and that moving the record writes before that operation makes the check fail.",
      "title": "The metadata-fault check can pass at the validation guard",
      "trigger": "The new detached-object regression invokes RecordHorsePrimaryAnimation with its uninitialized action."
    },
    {
      "acceptance_ids": [
        "MMR-07"
      ],
      "actual": "The preserved log proves the command, counts, DLL hash and budget entry. It ended 42 seconds before b1c82f31 was committed and contains no tree identity. The reflog confirms a later checkout for the reported rebuild. Its matching DLL digest is stated in the provenance note, and even a matching production DLL leaves the component executable and focused PowerShell inputs unidentified.",
      "blocking_rationale": "Section 5 requires extraction-commit proof. The final candidate receipt and the pre-commit log establish different runs.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The metered extraction result is traceable to the production and test inputs at b1c82f31.",
      "id": "R1-F9",
      "location": {
        "file": "docs/missions/mmr-split/evidence/PR-5-extraction-run-b1c82f31.md",
        "line": 15
      },
      "priority": "P2",
      "requested_correction": "Attach available execution evidence binding the tested tree or its production and test inputs to b1c82f31. If a separately authorized check on that frozen commit supplies the proof, retain its own date and identity alongside the original run.",
      "requested_evidence": "A metered focused receipt naming the extraction commit, or contemporaneous input identities and independently inspectable comparison output covering the DLL, component executable and selected scripts.",
      "title": "The extraction log is preserved but its tested tree is unbound",
      "trigger": "Verifying the separately required 28-check extraction result at b1c82f31."
    },
    {
      "acceptance_ids": [
        "S07",
        "S08"
      ],
      "actual": "The wrapper searches the entire child script for one of three literal spellings and prints a resolved path assembled from the probe name. Equivalent path construction can fail this check, while a matching comment can satisfy it.",
      "blocking_rationale": "Configuration forwarding is correct in this candidate. The issue concerns maintenance and the precision of the probe's output.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The routing check describes the argument or resolved path it actually observed.",
      "id": "R2-F6",
      "location": {
        "file": "scripts/Test-MmrHorseBiteFocused.ps1",
        "line": 56
      },
      "priority": "P3",
      "requested_correction": "Keep the argument-routing assertion and use a path-resolution probe where available, or limit the output to the forwarding it verified.",
      "requested_evidence": "A bounded Debug/Release routing check that tolerates an equivalent change in path-expression spelling.",
      "title": "The routing probe depends on child source spelling",
      "trigger": "A product-reading child changes how it constructs the same configuration-specific DLL path."
    },
    {
      "acceptance_ids": [
        "S07"
      ],
      "actual": "ReportFault says binding failed and the strike was left as planned even when the fallback was successfully assigned and recorded and only its outcome report threw.",
      "blocking_rationale": "The committed handle and command record survive this reporting failure.",
      "blocks_readiness": false,
      "category": "optional_polish",
      "expected": "The fault message states which operation failed or leaves the binding outcome open.",
      "id": "R2-F7",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/HorsePrimaryAttackAnimationAdapter.cs",
        "line": 151
      },
      "priority": "P3",
      "requested_correction": "Use fault wording valid for both binding and post-binding reporting failures, or distinguish the two stages.",
      "requested_evidence": "A successful supply followed by a throwing outcome reporter and a working fault logger produces a message consistent with the retained fallback.",
      "title": "A reporting fault can be logged as a failed binding",
      "trigger": "The fallback commits successfully and the subsequent metadata or Info report throws."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "The runtime slot and blueprint checks exclude both hooves. The binder also requires the action's own Bite type. Component and genuine-object cases confirm zero records and assignments for the original hoof cases. R2-F1 covers the distinct loss of a valid fallback."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "SupplyExact routes construction, binding and outcome reporting through SupplyContained. Its nested handler absorbs a throwing fault logger. The injected report failures passed, and both production callers reach this containment."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "Record and refresh read metadata before publishing fields. Bind restores the prior slot if Record throws. The component fault-and-retry case verifies slot state, source and assignment counts. R2-F5 concerns the separate genuine-object test's claim about its fault location."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F4",
      "rationale": "The three requested normal text dispositions are correct. On the same adopted-then-handle-less sequence, the structured terminal outcome still exposes the earlier handle to the existing Horse animation checks."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "HorseStrikeSource resolves the planned Hand by reference against the Horse's body. The checked cases distinguish limb1 from limb2 and retain the slot across repeated strikes with different routine ordinals."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "The throwing-clip case now produces an identifiable strike line with its basic handle facts and an explicit unreadable playback marker. R2-F4 covers the converse failure, where handle metadata suppresses readable playback."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "Missing-handle suppression now belongs to the command. Repeated callbacks reported once, a subsequent command reported independently, and the adapter's retained command reference is gone."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "The wrapper accepts the tier configuration and forwards it to each declaring child. I checked the product path expressions and loader resolver. The Release receipt and two-configuration argument probe support the routing fix."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F9",
      "rationale": "The original output, meter entry and timeline are now inspectable. The missing link is the identity of the pre-commit product and test inputs as a set. The later checkout and reported product rebuild do not establish that link."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F10",
      "rationale": "The shared test-file diff contains the strike formatter case, its helper and registration. The unused Trip fixture was removed."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F11",
      "rationale": "Terminal and strike numeric fields use invariant formatting. The executed de-DE case verifies decimal points in SpeedScale and clip weight."
    }
  ],
  "request_id": "kmc-codex-mmr-horse-bite-20261125221703-5f10-r02-e8061d",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "For a typed Bite in the verified Horse slot, the candidate adopts a stock handle, refreshes the same action and supplies a single eligible fallback. Hooves retain their native handles. Normal strike lines identify the limb and carry playback facts, and normal terminal text distinguishes an observed handle, a handle-less Bite and an unreached Bite.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "N08 at normal play zoom. Correlate the real Bite with its strike log and compare the hoof sequence, facing, rider attachment, timing and attack or damage count.",
      "Any additional formal PR-5 review requires an owner decision after the two-round allocation."
    ],
    "scope_reviewed": "Final PR-5 MMR-07, S07 and the S08 regression boundary at the fixed candidate, including all eleven prior findings and directly affected evidence consumers.",
    "unresolved_questions": [
      "Whether the installed BiteAttack clip is readable at normal zoom and whether native routine SpeedScale accounts for the owner's observation."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
