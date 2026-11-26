<!-- agent-review:v1 enrollment=kmc-codex-mounted-shield-20261123114646-4433 key=r01-review part=1/1 sha256=807e30abef260543bf406b276d9364e8da17ba9ff582a2e96ac971005bd2dc74 -->
### Codex GPT-6.2 Astra — review — round 1 — candidate `d5fe382096`

**Decision:** `changes_required` — The alpha package and focused-gate receipt match the submitted source. Two defects affect the code delivered for alpha testing. A Shield scenario can bypass profile restoration, and Brilliant Energy can receive the wrong target AC. The focused checks also report reader acceptance in cases where the evidence does not support it.

## Continuation for Claude

Close the unsafe invocation of the two new Shield scenarios at argument admission, or make their existing profile protections unconditional. Correct the Brilliant Energy calculation against the installed engine’s modifier semantics. Exercise the actual replay resolver with the alpha version, bind the Shield evidence to the observed actors and attack outcomes, and add a focused test of the physical-shield adapter. The findings give concrete offline cases for each correction.

Use the existing focused gate and supported packaging path on the corrected source. Retain the current package and historical receipts under their original identities, then update the handoff with the new ZIP, DLL, MVID and icon verification. OWNER AMENDMENT 2 assigns live gameplay, persistence, compatibility and UI acceptance to the owner. These corrections can be verified offline within the existing enrollment and budget.

#### Findings

**R1-F1** · P1 · defect · blocks readiness — Shield gameplay scenarios can bypass profile restoration
- Location: `scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1:318`; acceptance: MS-27
- Trigger: Invoke sprint6-shield-rt or sprint6-shield-tb with an otherwise admitted Working fixture and omit -BaseGameOnly.
- Expected: Every admitted launch of either scenario must protect and restore the profile surfaces it can change.
- Actual: PersistenceProfileAreasIsolation.ps1 selects both Shield scenarios for protection. In Invoke-KingmakerRuntimeScenario.ps1 the profile snapshot at line 887 is conditional on -BaseGameOnly or a list that omits both scenarios. Without the switch, the runner can stage the live Mods clone and load Working while $profileSnapshot stays null. The restoration branch at line 1654 then skips Areas, UMM settings and PlayerPrefs.
- Why it blocks / does not: An admitted invocation of the new tooling can leave changes in the owner’s profile. OWNER AMENDMENT 2 explicitly retains runner safety as a checkpoint requirement.
- Requested correction: Require the protected staging mode for these two scenarios before mutation, or ensure every admitted mode takes the existing profile snapshot and enters Areas isolation. Preserve the historical fixture and retained-dependency guards.
- Requested evidence: Exercise argument admission for both scenarios with and without -BaseGameOnly. For every accepted configuration, show through the offline runner or transaction fixture that snapshot, Areas isolation and restoration are all selected. A guarded refusal before mutation is sufficient for an unsupported configuration.

**R1-F2** · P2 · defect · blocks readiness — Brilliant Energy gives the mount excess AC with a force-stacking shield source
- Location: `src/KingmakerMountedCombat/Integration/MountedShieldService.cs:281`; acceptance: MS-09, MS-10
- Trigger: A Brilliant Energy weapon attacks a mount carrying KMC’s +3 and a separate positive Shield modifier with native StackMode.ForceStack.
- Expected: A Brilliant Energy attack should see the same AC with and without this feat while preserving the native treatment of every other modifier.
- Actual: BrilliantEnergyCorrection treats the largest foreign Shield modifier as a competitor even when its native Stacks property is true. With base AC 10, a force-stacking +4 Shield and KMC’s ordinary +3, native AC is 17. Brilliant Energy subtracts 7 and reaches 10, the same value as without the feat. The candidate then adds 3 back and presents AC 13.
- Why it blocks / does not: This changes a real attack result under an installed Kingmaker modifier contract. The existing genuine-object test covers an ordinary competing +4 only.
- Requested correction: Calculate KMC’s effective AC delta using native stacking eligibility. A force-stacking foreign Shield bonus cannot suppress KMC’s non-stacking bonus. Keep the diagnostic AC-delta calculation consistent with the corrected rule.
- Requested evidence: Extend the existing genuine ModifiableValue and RuleCalculateAC test with a +4 Shield modifier using StackMode.ForceStack. Verify identical Brilliant Energy target AC with and without KMC, alongside the existing ordinary competing-shield and touch cases.

**R1-F3** · P2 · defect · blocks readiness — The focused gate accepts an alpha version the replay reader rejects
- Location: `scripts/chunk7/Test-Chunk7ReplaySubscriberContract.ps1:142`; acceptance: MS-25, MS-26
- Trigger: Resolve a v3 repeated-world declaration from the packaged 0.2.16-alpha.1 producer.
- Expected: The gate’s declared-evidence verdict must agree with the production resolver for the delivered product version and subscriber revision.
- Actual: The new test-only Get-KmcReleaseNumber accepts 0.2.16-alpha.1. Resolve-KmcChunk7ReplaySubscriberContract in scripts/runtime/Chunk7RepeatedWorldEvidence.ps1 still accepts only numeric 0.2.N versions or the synthetic sentinel for this contract. The gate log nevertheless prints that the reader accepts the compiled producer as declared evidence.
- Why it blocks / does not: The required focused gate makes a false compatibility claim. Its passing receipt is genuine, but the changed assertion bypasses the decision it purports to verify.
- Requested correction: Use the actual v3 resolver for the candidate-version assertion and align alpha admission with the intended version contract. Preserve the historical revision boundaries and malformed-version refusals.
- Requested evidence: Show the real resolver’s verdict for 0.2.16-alpha.1 with kmc-global-subscribers-r2, plus the existing release, too-early and malformed controls. This is an offline check.

**R1-F4** · P2 · defect · blocks readiness — The two-pair reader can count the same mount twice
- Location: `scripts/runtime/Sprint6ShieldEvidence.ps1:203`; acceptance: MS-16, MS-19
- Trigger: Supply a valid MS-pairs row with one disabled or cross-pair snapshot substituted for the other.
- Expected: Each snapshot must name the established rider and mount for that step. The two cross-pair checks must observe reciprocal combinations.
- Actual: Assert-KmcShieldPairs checks amounts and modifier counts without binding the cross-pair or disabled snapshots to their expected rider and mount. Copying serviceDisabledFirst into serviceDisabledSecond satisfies both cleanup checks. Likewise, the same cross-pair snapshot can satisfy both directions.
- Why it blocks / does not: The new reader can certify two-pair isolation and cleanup with no observation of one mount at disable, or no observation of one cross-pair direction.
- Requested correction: Bind the first pair to the measured actors and the second pair to the recorded companion acquisition. Verify those identities on the ordinary, cross-pair and service-disable snapshots before crediting their amounts.
- Requested evidence: Add negative fixtures that duplicate the first disabled snapshot into the second and that duplicate one cross-pair direction. Retain a passing fixture with both distinct pairs.

**R1-F5** · P2 · defect · blocks readiness — A Mirror Image result can qualify as a miss caused by Mounted Shield
- Location: `scripts/runtime/Sprint6ShieldEvidence.ps1:410`; acceptance: MS-14
- Trigger: The designated hostile’s attack falls within the shield AC margin and a separate native defense changes its result.
- Expected: The selected attack must be an AC miss at the hit event and at rule completion, with no defense attempt or damage.
- Actual: ShieldBandMiss and its reader accept a non-hit whose total falls inside the AC margin without checking its native result. Kingmaker can resolve a near miss as MirrorImage. Such a row has isHitAtEvent false, zero damage and no Mounted Combat record, so it meets the current predicates even though a separate defense affected the outcome.
- Why it blocks / does not: A passing MS-14 row can attribute an attack to Mounted Shield without establishing that shield AC prevented the hit.
- Requested correction: Have the producer select, and the reader require, an ordinary native AC-miss outcome. Check the final outcome as well as the existing attack identity, AC margin and absence of a reaction.
- Requested evidence: Use the existing positive band-miss fixture and add negative cases with the same arithmetic but a MirrorImage result or a final hit.

**R1-F6** · P2 · defect · blocks readiness — A successful natural defense can pass even when its native attack deals damage
- Location: `scripts/runtime/Sprint6ShieldEvidence.ps1:292`; acceptance: MS-11, MS-15
- Trigger: An unplanned Shield-row check succeeds, or a planned negation leaves a native critical flag set.
- Expected: Every successful defense credited in a Shield row must agree with its bound native attack at the hit event and at completion, including critical cleanup and damage.
- Actual: The all-records loop validates native outcomes for standing hits. For a successful natural defense outside the named planned steps, it checks the roll, amount and commitment but skips the native negated outcome. A second rider’s record can say negated while its bound attack still reports Hit and damage. Even the planned-success helper omits the observer’s critical flags at the event and completion.
- Why it blocks / does not: The reader can credit a successful hit negation despite direct evidence that the hit or its critical confirmation survived.
- Requested correction: Validate the native outcome of every negated Shield-row attempt, including natural follow-ups. Account for the existing legitimate melee-parry and ordinary-miss outcomes and verify that critical confirmation was cleared.
- Requested evidence: Use the existing second-pair natural-attempt fixture with a passing check and a bound native hit that deals damage. Verify rejection. Also reject a planned negation with criticalAfterRule still true and retain valid negations.

**R1-F7** · P2 · evidence_gap · blocks readiness — The focused gate supplies the answer to the physical-shield eligibility test
- Location: `scripts/Test-MountedShieldNativeContract.ps1:230`; acceptance: MS-03, MS-06, MS-08
- Trigger: A regression selects an inactive shield or accepts a Shield-descriptor modifier from the wrong source.
- Expected: A focused regression should exercise the production decision that distinguishes an effective physical shield from an inactive item or an unrelated magical Shield bonus.
- Actual: The focused script invokes the production base-bonus read, then sets EffectiveShield directly in MountedShieldFacts. The component tests also supply that boolean. Neither exercises MountedShieldService.EffectiveShield, which decides whether the active hand and native AC modifier belong to the same physical item. Both benefits depend on that decision.
- Why it blocks / does not: The mission requires testing the actual eligibility adapter. OWNER AMENDMENT 2 defers the in-game equipment observations, while this production-path check belongs to the focused offline coverage.
- Requested correction: Exercise the production equipment predicate with genuine hand-slot, shield and modifier objects where available. Keep any necessary boundary fixture thin and use the same predicate ordinary play calls.
- Requested evidence: Show a matching active shield being accepted, then show rejection after slot deactivation, removal of that armor component’s modifier, and substitution of another item’s or a spell’s Shield modifier. Restore the matching modifier and verify eligibility returns.

#### Evidence assessment

- **Inspected:** The fixed feature diff, mission and acceptance packet, both owner amendments, repository validation policy, current handoff, developer report, controller receipts and empty prior-findings ledger.; Feat registration and publication, refused-toggle rollback, the shared shield policy, equipment reads, AC modifier ownership, the existing Mobility rule, and cleanup through the composition root.; The Shield scenario producers, synthetic reader fixtures, save-backed launcher, profile snapshot and restoration branches, UMM reference and packaging changes, and the supplemental closure review.; Git confirms bb903669c038c9b35802e467c5ba3394b586997b is the packaged source. The six files changed between that commit and the submitted head are records.
- **Executed verification:** The controller receipt for d5fe3820 records exit 0 in 57.5 seconds with an unchanged worktree. Its log reports 1,525 component checks, 153 source checks and 18 of 18 focused children passing, including 68 selection-rollback checks, 199 Shield-reader checks and 100 Shield native-contract checks.; The compiled product’s 20 UMM member references bound against the pinned 0.31.1 reference and the installed 0.33.0 loader. Each binding check reported 25 passes. The packaged-source gate on bb903669 also exited 0 in 65.9 seconds.; I verified the two-entry alpha ZIP and its manifest. ZIP SHA-256 is fa5fb284e889e414093ee8b939deb330084eb029f3d02f622f094ac6444fd34a. The packaged DLL SHA-256 is 91e87536f7a37270ddac7db2dfd6942926cb385a844e3452a4b2f471626e2918 and its recorded MVID matches the gate, 62f8f8fa-7b05-4d10-91bb-0181635dfa9c. The packet, repository and embedded icon bytes match SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.; I checked both version predicates against the actual product version 0.2.16-alpha.1. The focused test’s predicate accepted it and the production reader’s predicate rejected it.
- **Source inspection only:** The installed 2.1.7b method contracts show how native AC handles per-modifier stacking and how an attack inside the shield AC margin can resolve as MirrorImage. The numerical counterexample and reader failure paths below follow from those contracts and the candidate source.; The candidate wires the same eligibility calculation into mount AC and the check-local bonus before Rulebook.Trigger. The production equipment read and in-game effects were not exercised by this review.; The supplemental PASS on bb903669 covered its stated no-live-use plan. It provides source-review evidence for the PN14 corrections. The original Sprint 6 forced-die behavior remains within the historical diagnostic deferral in OWNER AMENDMENT 2.
- **Required gaps:** Offline evidence that both new Shield scenarios either acquire profile and Areas protection or refuse the invocation before mutation.; A genuine-modifier regression for Brilliant Energy and a production-path test of effective physical-shield selection.; A focused test through the actual alpha replay resolver and negative Shield-reader fixtures for actor substitution, non-AC misses and contradicted defense outcomes.
- **Limitations:** The controller withheld the mixed foreground transcript. I used the controller-recorded gate output, package bytes and manifest for execution and artifact claims. The reviewer environment also prevented direct invocation of the PowerShell replay resolver.; The handoff records zero game starts and no generated save. No live restoration was due. The missing qualified historical fixture and the documented no-save smoke limitation support the bounded stop under OWNER AMENDMENT 2.; The earlier broad Candidate failure on cb74ad58 remains in the record, including the missing historical phase3e package and unavailable manifest-run directory. The current focused receipt establishes only its own scope.

#### Summary

- **Delivered behavior:** The candidate packages one passive Mounted Shield feat with the supplied icon, coordinated three-feat publication, a shared shield-value calculation, a mount AC modifier and a bonus on the existing Mounted Combat check. The focused gate exercised ordinary shield competition, touch and flat-footed AC, check arithmetic, refused selection transitions and loader bindings.
- **Scope reviewed:** The complete fixed candidate against the original mission, OWNER AMENDMENT 1 and the private-alpha milestone in OWNER AMENDMENT 2.
- **Pending later-phase work:** none
- **Unresolved questions:** none
- **Remaining owner review:** After the engineering findings close, the owner’s private-alpha playtest covers startup and feat presentation, physical and enhanced shields, the combat log and reaction behavior, two-pair play, lifecycle and save/reload, compatibility and visual acceptance.; Draft PR #61 remains under the owner’s merge and release authority.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Owner guidance bound to this review: none.

Owner amendments bound to this review: revision 2, sha256 `d735f07ddc3131f5d90ae6094232e474b958614c6f432696956166a25d7d5c73`.

Codex CLI `0.170.0`, thread `01a20bd0-1821-7511-8198-117a3476d024`, duration 1389.0s, response sha256 `2783af4169598081eb1bae3a4f001dee7064916b2e9723807fb2abc92453eaf9`, request `kmc-codex-mounted-shield-20261123114646-4433-r01-42cf6b`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "d5fe38209665f9b8aeb0de7ac14eba789f8b4d55",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The alpha package and focused-gate receipt match the submitted source. Two defects affect the code delivered for alpha testing. A Shield scenario can bypass profile restoration, and Brilliant Energy can receive the wrong target AC. The focused checks also report reader acceptance in cases where the evidence does not support it.\n\n## Continuation for Claude\n\nClose the unsafe invocation of the two new Shield scenarios at argument admission, or make their existing profile protections unconditional. Correct the Brilliant Energy calculation against the installed engine\u2019s modifier semantics. Exercise the actual replay resolver with the alpha version, bind the Shield evidence to the observed actors and attack outcomes, and add a focused test of the physical-shield adapter. The findings give concrete offline cases for each correction.\n\nUse the existing focused gate and supported packaging path on the corrected source. Retain the current package and historical receipts under their original identities, then update the handoff with the new ZIP, DLL, MVID and icon verification. OWNER AMENDMENT 2 assigns live gameplay, persistence, compatibility and UI acceptance to the owner. These corrections can be verified offline within the existing enrollment and budget.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt for d5fe3820 records exit 0 in 57.5 seconds with an unchanged worktree. Its log reports 1,525 component checks, 153 source checks and 18 of 18 focused children passing, including 68 selection-rollback checks, 199 Shield-reader checks and 100 Shield native-contract checks.",
      "The compiled product\u2019s 20 UMM member references bound against the pinned 0.31.1 reference and the installed 0.33.0 loader. Each binding check reported 25 passes. The packaged-source gate on bb903669 also exited 0 in 65.9 seconds.",
      "I verified the two-entry alpha ZIP and its manifest. ZIP SHA-256 is fa5fb284e889e414093ee8b939deb330084eb029f3d02f622f094ac6444fd34a. The packaged DLL SHA-256 is 91e87536f7a37270ddac7db2dfd6942926cb385a844e3452a4b2f471626e2918 and its recorded MVID matches the gate, 62f8f8fa-7b05-4d10-91bb-0181635dfa9c. The packet, repository and embedded icon bytes match SHA-256 fd519874e9d231946aad68c46f93a4e0a3a2ea04d4037807e6c7a4d36629f9d0.",
      "I checked both version predicates against the actual product version 0.2.16-alpha.1. The focused test\u2019s predicate accepted it and the production reader\u2019s predicate rejected it."
    ],
    "inspected": [
      "The fixed feature diff, mission and acceptance packet, both owner amendments, repository validation policy, current handoff, developer report, controller receipts and empty prior-findings ledger.",
      "Feat registration and publication, refused-toggle rollback, the shared shield policy, equipment reads, AC modifier ownership, the existing Mobility rule, and cleanup through the composition root.",
      "The Shield scenario producers, synthetic reader fixtures, save-backed launcher, profile snapshot and restoration branches, UMM reference and packaging changes, and the supplemental closure review.",
      "Git confirms bb903669c038c9b35802e467c5ba3394b586997b is the packaged source. The six files changed between that commit and the submitted head are records."
    ],
    "limitations": [
      "The controller withheld the mixed foreground transcript. I used the controller-recorded gate output, package bytes and manifest for execution and artifact claims. The reviewer environment also prevented direct invocation of the PowerShell replay resolver.",
      "The handoff records zero game starts and no generated save. No live restoration was due. The missing qualified historical fixture and the documented no-save smoke limitation support the bounded stop under OWNER AMENDMENT 2.",
      "The earlier broad Candidate failure on cb74ad58 remains in the record, including the missing historical phase3e package and unavailable manifest-run directory. The current focused receipt establishes only its own scope."
    ],
    "required_gaps": [
      "Offline evidence that both new Shield scenarios either acquire profile and Areas protection or refuse the invocation before mutation.",
      "A genuine-modifier regression for Brilliant Energy and a production-path test of effective physical-shield selection.",
      "A focused test through the actual alpha replay resolver and negative Shield-reader fixtures for actor substitution, non-AC misses and contradicted defense outcomes."
    ],
    "source_inspection_only": [
      "The installed 2.1.7b method contracts show how native AC handles per-modifier stacking and how an attack inside the shield AC margin can resolve as MirrorImage. The numerical counterexample and reader failure paths below follow from those contracts and the candidate source.",
      "The candidate wires the same eligibility calculation into mount AC and the check-local bonus before Rulebook.Trigger. The production equipment read and in-game effects were not exercised by this review.",
      "The supplemental PASS on bb903669 covered its stated no-live-use plan. It provides source-review evidence for the PN14 corrections. The original Sprint 6 forced-die behavior remains within the historical diagnostic deferral in OWNER AMENDMENT 2."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "MS-27"
      ],
      "actual": "PersistenceProfileAreasIsolation.ps1 selects both Shield scenarios for protection. In Invoke-KingmakerRuntimeScenario.ps1 the profile snapshot at line 887 is conditional on -BaseGameOnly or a list that omits both scenarios. Without the switch, the runner can stage the live Mods clone and load Working while $profileSnapshot stays null. The restoration branch at line 1654 then skips Areas, UMM settings and PlayerPrefs.",
      "blocking_rationale": "An admitted invocation of the new tooling can leave changes in the owner\u2019s profile. OWNER AMENDMENT 2 explicitly retains runner safety as a checkpoint requirement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every admitted launch of either scenario must protect and restore the profile surfaces it can change.",
      "id": "R1-F1",
      "location": {
        "file": "scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1",
        "line": 318
      },
      "priority": "P1",
      "requested_correction": "Require the protected staging mode for these two scenarios before mutation, or ensure every admitted mode takes the existing profile snapshot and enters Areas isolation. Preserve the historical fixture and retained-dependency guards.",
      "requested_evidence": "Exercise argument admission for both scenarios with and without -BaseGameOnly. For every accepted configuration, show through the offline runner or transaction fixture that snapshot, Areas isolation and restoration are all selected. A guarded refusal before mutation is sufficient for an unsupported configuration.",
      "title": "Shield gameplay scenarios can bypass profile restoration",
      "trigger": "Invoke sprint6-shield-rt or sprint6-shield-tb with an otherwise admitted Working fixture and omit -BaseGameOnly."
    },
    {
      "acceptance_ids": [
        "MS-09",
        "MS-10"
      ],
      "actual": "BrilliantEnergyCorrection treats the largest foreign Shield modifier as a competitor even when its native Stacks property is true. With base AC 10, a force-stacking +4 Shield and KMC\u2019s ordinary +3, native AC is 17. Brilliant Energy subtracts 7 and reaches 10, the same value as without the feat. The candidate then adds 3 back and presents AC 13.",
      "blocking_rationale": "This changes a real attack result under an installed Kingmaker modifier contract. The existing genuine-object test covers an ordinary competing +4 only.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A Brilliant Energy attack should see the same AC with and without this feat while preserving the native treatment of every other modifier.",
      "id": "R1-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedShieldService.cs",
        "line": 281
      },
      "priority": "P2",
      "requested_correction": "Calculate KMC\u2019s effective AC delta using native stacking eligibility. A force-stacking foreign Shield bonus cannot suppress KMC\u2019s non-stacking bonus. Keep the diagnostic AC-delta calculation consistent with the corrected rule.",
      "requested_evidence": "Extend the existing genuine ModifiableValue and RuleCalculateAC test with a +4 Shield modifier using StackMode.ForceStack. Verify identical Brilliant Energy target AC with and without KMC, alongside the existing ordinary competing-shield and touch cases.",
      "title": "Brilliant Energy gives the mount excess AC with a force-stacking shield source",
      "trigger": "A Brilliant Energy weapon attacks a mount carrying KMC\u2019s +3 and a separate positive Shield modifier with native StackMode.ForceStack."
    },
    {
      "acceptance_ids": [
        "MS-25",
        "MS-26"
      ],
      "actual": "The new test-only Get-KmcReleaseNumber accepts 0.2.16-alpha.1. Resolve-KmcChunk7ReplaySubscriberContract in scripts/runtime/Chunk7RepeatedWorldEvidence.ps1 still accepts only numeric 0.2.N versions or the synthetic sentinel for this contract. The gate log nevertheless prints that the reader accepts the compiled producer as declared evidence.",
      "blocking_rationale": "The required focused gate makes a false compatibility claim. Its passing receipt is genuine, but the changed assertion bypasses the decision it purports to verify.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The gate\u2019s declared-evidence verdict must agree with the production resolver for the delivered product version and subscriber revision.",
      "id": "R1-F3",
      "location": {
        "file": "scripts/chunk7/Test-Chunk7ReplaySubscriberContract.ps1",
        "line": 142
      },
      "priority": "P2",
      "requested_correction": "Use the actual v3 resolver for the candidate-version assertion and align alpha admission with the intended version contract. Preserve the historical revision boundaries and malformed-version refusals.",
      "requested_evidence": "Show the real resolver\u2019s verdict for 0.2.16-alpha.1 with kmc-global-subscribers-r2, plus the existing release, too-early and malformed controls. This is an offline check.",
      "title": "The focused gate accepts an alpha version the replay reader rejects",
      "trigger": "Resolve a v3 repeated-world declaration from the packaged 0.2.16-alpha.1 producer."
    },
    {
      "acceptance_ids": [
        "MS-16",
        "MS-19"
      ],
      "actual": "Assert-KmcShieldPairs checks amounts and modifier counts without binding the cross-pair or disabled snapshots to their expected rider and mount. Copying serviceDisabledFirst into serviceDisabledSecond satisfies both cleanup checks. Likewise, the same cross-pair snapshot can satisfy both directions.",
      "blocking_rationale": "The new reader can certify two-pair isolation and cleanup with no observation of one mount at disable, or no observation of one cross-pair direction.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Each snapshot must name the established rider and mount for that step. The two cross-pair checks must observe reciprocal combinations.",
      "id": "R1-F4",
      "location": {
        "file": "scripts/runtime/Sprint6ShieldEvidence.ps1",
        "line": 203
      },
      "priority": "P2",
      "requested_correction": "Bind the first pair to the measured actors and the second pair to the recorded companion acquisition. Verify those identities on the ordinary, cross-pair and service-disable snapshots before crediting their amounts.",
      "requested_evidence": "Add negative fixtures that duplicate the first disabled snapshot into the second and that duplicate one cross-pair direction. Retain a passing fixture with both distinct pairs.",
      "title": "The two-pair reader can count the same mount twice",
      "trigger": "Supply a valid MS-pairs row with one disabled or cross-pair snapshot substituted for the other."
    },
    {
      "acceptance_ids": [
        "MS-14"
      ],
      "actual": "ShieldBandMiss and its reader accept a non-hit whose total falls inside the AC margin without checking its native result. Kingmaker can resolve a near miss as MirrorImage. Such a row has isHitAtEvent false, zero damage and no Mounted Combat record, so it meets the current predicates even though a separate defense affected the outcome.",
      "blocking_rationale": "A passing MS-14 row can attribute an attack to Mounted Shield without establishing that shield AC prevented the hit.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The selected attack must be an AC miss at the hit event and at rule completion, with no defense attempt or damage.",
      "id": "R1-F5",
      "location": {
        "file": "scripts/runtime/Sprint6ShieldEvidence.ps1",
        "line": 410
      },
      "priority": "P2",
      "requested_correction": "Have the producer select, and the reader require, an ordinary native AC-miss outcome. Check the final outcome as well as the existing attack identity, AC margin and absence of a reaction.",
      "requested_evidence": "Use the existing positive band-miss fixture and add negative cases with the same arithmetic but a MirrorImage result or a final hit.",
      "title": "A Mirror Image result can qualify as a miss caused by Mounted Shield",
      "trigger": "The designated hostile\u2019s attack falls within the shield AC margin and a separate native defense changes its result."
    },
    {
      "acceptance_ids": [
        "MS-11",
        "MS-15"
      ],
      "actual": "The all-records loop validates native outcomes for standing hits. For a successful natural defense outside the named planned steps, it checks the roll, amount and commitment but skips the native negated outcome. A second rider\u2019s record can say negated while its bound attack still reports Hit and damage. Even the planned-success helper omits the observer\u2019s critical flags at the event and completion.",
      "blocking_rationale": "The reader can credit a successful hit negation despite direct evidence that the hit or its critical confirmation survived.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Every successful defense credited in a Shield row must agree with its bound native attack at the hit event and at completion, including critical cleanup and damage.",
      "id": "R1-F6",
      "location": {
        "file": "scripts/runtime/Sprint6ShieldEvidence.ps1",
        "line": 292
      },
      "priority": "P2",
      "requested_correction": "Validate the native outcome of every negated Shield-row attempt, including natural follow-ups. Account for the existing legitimate melee-parry and ordinary-miss outcomes and verify that critical confirmation was cleared.",
      "requested_evidence": "Use the existing second-pair natural-attempt fixture with a passing check and a bound native hit that deals damage. Verify rejection. Also reject a planned negation with criticalAfterRule still true and retain valid negations.",
      "title": "A successful natural defense can pass even when its native attack deals damage",
      "trigger": "An unplanned Shield-row check succeeds, or a planned negation leaves a native critical flag set."
    },
    {
      "acceptance_ids": [
        "MS-03",
        "MS-06",
        "MS-08"
      ],
      "actual": "The focused script invokes the production base-bonus read, then sets EffectiveShield directly in MountedShieldFacts. The component tests also supply that boolean. Neither exercises MountedShieldService.EffectiveShield, which decides whether the active hand and native AC modifier belong to the same physical item. Both benefits depend on that decision.",
      "blocking_rationale": "The mission requires testing the actual eligibility adapter. OWNER AMENDMENT 2 defers the in-game equipment observations, while this production-path check belongs to the focused offline coverage.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "A focused regression should exercise the production decision that distinguishes an effective physical shield from an inactive item or an unrelated magical Shield bonus.",
      "id": "R1-F7",
      "location": {
        "file": "scripts/Test-MountedShieldNativeContract.ps1",
        "line": 230
      },
      "priority": "P2",
      "requested_correction": "Exercise the production equipment predicate with genuine hand-slot, shield and modifier objects where available. Keep any necessary boundary fixture thin and use the same predicate ordinary play calls.",
      "requested_evidence": "Show a matching active shield being accepted, then show rejection after slot deactivation, removal of that armor component\u2019s modifier, and substitution of another item\u2019s or a spell\u2019s Shield modifier. Restore the matching modifier and verify eligibility returns.",
      "title": "The focused gate supplies the answer to the physical-shield eligibility test",
      "trigger": "A regression selects an inactive shield or accepts a Shield-descriptor modifier from the wrong source."
    }
  ],
  "prior_finding_assessments": [],
  "request_id": "kmc-codex-mounted-shield-20261123114646-4433-r01-42cf6b",
  "round": 1,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate packages one passive Mounted Shield feat with the supplied icon, coordinated three-feat publication, a shared shield-value calculation, a mount AC modifier and a bonus on the existing Mounted Combat check. The focused gate exercised ordinary shield competition, touch and flat-footed AC, check arithmetic, refused selection transitions and loader bindings.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After the engineering findings close, the owner\u2019s private-alpha playtest covers startup and feat presentation, physical and enhanced shields, the combat log and reaction behavior, two-pair play, lifecycle and save/reload, compatibility and visual acceptance.",
      "Draft PR #61 remains under the owner\u2019s merge and release authority."
    ],
    "scope_reviewed": "The complete fixed candidate against the original mission, OWNER AMENDMENT 1 and the private-alpha milestone in OWNER AMENDMENT 2.",
    "unresolved_questions": []
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
