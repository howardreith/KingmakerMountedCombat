<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r21-review part=1/1 sha256=604b0715a2923178c04f27a29386343db06e3bffca2fde1696f595a5db0f6586 -->
### Codex GPT-6.2 Astra — review — round 21 — candidate `a009bd952a`

**Decision:** `changes_required` — The focused gate passed on the requested commit. The immediate-save fix is verified. Source inspection still shows supported toolbar sequences that save the wrong slot, undo a deliberate removal, or leave two callable shortcuts. The copy registry also retains native references across two cleanup paths. The mission permits deferring the documented game checks to the owner.

#### Findings

**R19-F2** · P2 · defect · blocks readiness — An occupied former slot lets a lower copy replace a moved shortcut
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2044`; acceptance: §4 player moves, §4 occupied slots, §4 save/load
- Trigger: Native empties 3 with copies at 7 and 9. Slot 3 receives Fireball, the player moves 9 to 11, and CleanupBadSlots rebinds 7 and 11 before KMC reconciles.
- Expected: Fireball stays at 3. The moved shortcut at 11 is the sole live and saved public binding.
- Actual: With slot 3 occupied, CopyYields promotes the rebound copy at 7 even though the player's moved binding exists at 11. That promotion clears the interrupted state and removes 7 from the copy registry. The next report uses PublicBindingIndex, which selects 7 and ignores 11. Both bindings survive live repair, and capture writes 7.
- Why it blocks / does not: Saving can reverse a deliberate move and reload the wrong toolbar slot.
- Requested correction: Use the bar's effective player binding when classifying native reports. Promote a recorded copy as the fallback only when the occupied former slot has no newer player binding elsewhere.
- Requested evidence: Extend MoveBeyondSurvivingCopy with Fireball at 3 and the existing pass that rebinds 7 and 11. Check update-first and save-first order, one kind-6 row at 11, one placement marker, and a cold restore at 11.

**R18-F1** · P2 · defect · blocks readiness — Removing the fallback before repair promotes another recorded copy
- Location: `src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs:166`; acceptance: §4 deliberate removal, §4 occupied slots, §4 save/load
- Trigger: In the existing Fireball-at-3 fixture, native rebinds 7 and 9. The player removes 7 as soon as that pass returns.
- Expected: Removing the live fallback leaves Fireball at 3 and only the placement marker in the save.
- Actual: After native rebinds the fallback at 7, its arrangement is live and 9 remains a recorded secondary. If the player removes 7 immediately, ChosenSlotFree returns false because the arrangement is live. EffectiveBindingIndex then returns the sole remaining copy at 9. Repair or capture adopts 9.
- Why it blocks / does not: A deliberate removal persists or disappears depending on whether the player acts before the next KMC update.
- Requested correction: Distinguish a removed live choice from an interrupted choice whose slot is occupied. Discard surviving secondary copies when the player removes the live choice.
- Requested evidence: In OccupiedChosenSlotWithTwoCopies, remove 7 immediately after the rebind pass, before the first repair or save. Verify zero shortcuts, Fireball at 3 and a marker-only archive through cold restore. Retain the existing after-repair control.

**R21-F1** · P2 · defect · blocks readiness — Repairing a stale canonical binding can create two callable shortcuts
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1923`; acceptance: §4 stale ability data, §4 no duplicate shortcuts, §4 save/load
- Trigger: A rider has stale canonical data at 3 and current data at 9. KMC updates before the first save or native cleanup report.
- Expected: Live repair and save-first recovery agree on the documented first canonical slot, with one callable shortcut at 3.
- Actual: ReconcilePublicControlSlots rebinds stale slot 3 to the live fact and leaves the already-callable slot at 9. No native emptying has populated recordedCopies, so the copy-removal loop skips this bar. Both slots remain callable on later updates, while the archive selects only 3.
- Why it blocks / does not: KMC's repair creates a second callable shortcut. Removing the selected shortcut can then leave an unintended control on the bar.
- Requested correction: Reconcile stale canonical bindings against the effective slot even when KMC performs the first repair and the native observer has recorded no copies.
- Requested evidence: Run the existing StaleThenCurrentCanonical setup update-first. Assert one callable shortcut at 3 after two updates. Remove it and verify a marker-only save and reload, with the ordinary slot at 0 preserved.

**R20-F1** · P2 · defect · blocks readiness — First-time reserve migration retains the slot objects it clears
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2339`; acceptance: §4 discarded entity cleanup, §4 older city saves, §7 affected lifecycle paths
- Trigger: Native observes a bad slot on a reserve rider before KMC first scans that rider's surviving bindings.
- Expected: Clearing a reserve rider's bindings releases their slot objects while preserving the chosen index and pet identity.
- Actual: AdmitReserveBindings clears the reserve bar with its own SetSlot loop. It bypasses both cleanup methods that now call ForgetCopies. ReconcilePublicControlSlots prunes candidates only, so the recorded slot and its AbilityData can remain reachable after the reserve scan clears the bar and even after the rider leaves the roster.
- Why it blocks / does not: The registry keeps discarded native objects alive in a continuing player world.
- Requested correction: Release or prune the rider's recorded copies when reserve migration clears its bindings, including riders first encountered in reserve.
- Requested evidence: Start with an older capital bar on a never-observed reserve rider. Have GetSlot empty 3 and record a surviving copy at 9 before the first reserve scan. Verify the scan releases that object in the same world and a same-pet return restores 3 exactly once.

**R21-F2** · P2 · defect · blocks readiness — A disabled save makes an empty copy registry retain the player
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2161`; acceptance: §4 mod disable, §4 world disposal, §7 save/lifecycle paths
- Trigger: While KMC is disabled, save an older compatible world with a stale public shortcut, then load another world before re-enabling KMC.
- Expected: Disabled save sanitation releases the old world by the time native loading replaces it.
- Actual: Disabled archive capture still calls CaptureLiveSlots. A surviving public slot reaches IsRecordedCopyAt, and CopiesOf assigns Game.Instance.Player to recordedCopiesWorld even on an empty lookup. BeginSelectedLoad clears the disabled load fence but leaves this separate strong reference. UMM stops ordinary updates while disabled.
- Why it blocks / does not: A disabled save can keep an entire outgoing player world reachable after a subsequent load.
- Requested correction: Keep an empty or disabled copy-registry lookup from acquiring a strong player reference. Preserve the world-identity check for active observations.
- Requested evidence: Extend DisabledSaveIsolation with a registry lifetime assertion. Save the stale-slot fixture while disabled, replace the player without an enabled frame, and verify the registry releases the outgoing world.

**R21-F3** · P3 · evidence_gap · blocks readiness — The combat acceptance row labels two source traces as unit-test results
- Location: `docs/CITY-AND-PRONE-HANDOFF.md:720`; acceptance: §7 evidence type for each case, §8 offline and native handoff
- Trigger: Reading the negative-control evidence for automatic proposals, Charge, routine continuation and a downed ally.
- Expected: Each negative control identifies the evidence that actually establishes it.
- Actual: The acceptance row labels all four claims PASS/UNIT. The gate exercises the explicit-order predicate, automatic-intent flag and ally refusal. The Charge and continuation tests take preclassified target-valid or target-down inputs. The checks that exclude unconscious Charge and replacement targets are visible in the integration source.
- Why it blocks / does not: The required acceptance record overstates which target filters were exercised.
- Requested correction: Label the Charge and replacement-target filters as source traces, or cite a focused execution that passes an unconscious unit through those filters.
- Requested evidence: A corrected acceptance row identifying the executed assertions and the traced integration checks is sufficient.

#### Prior finding assessments

- **R17-F2** — `fix_verified` — CapturePersistentSlots settles the pending native write before classifying the bar. The gate covers an immediate save after native emptied 9, cold restore at 9, and a marker-only save when the player removed the slot after an aborted report.
- **R18-F1** — `fix_rejected` — The requested after-repair sequence passes. Removing the same live fallback before the first repair still promotes the recorded copy at 9.
- **R19-F2** — `fix_rejected` — The move to 11 survives the rebind while 3 is empty. With 3 occupied, the first rebind promotes 7 despite the moved binding at 11, and capture selects 7.
- **R20-F1** — `fix_rejected` — An observed rider departing for reserve now releases its copies. A rider first encountered in reserve follows a separate clearing loop that retains the same discarded slot objects.

#### Evidence assessment

- **Inspected:** The mission, AGENTS.md, validation policy, current project and resume records, X01 compatibility notes, and the complete 34-file feature diff.; Toolbar fact leasing, native bad-slot callbacks, copy tracking, reserve migration, save capture and restore, area suspension, retained remount and load fencing.; The capital roster projection and its native adapter, mounted attack admission and execution paths, focused scripts and fixtures, acceptance record, controller evidence and prior findings ledger.
- **Executed verification:** Verified the detached HEAD, mission SHA-256 and gate-log SHA-256. The feature diff passed git diff --check.; The controller receipt and CLI transcript confirm `-Tier Fast -Focused Test-CityToolbarProneFocused.ps1` passed on a009bd952abac9e115746fc114a15bd40f88b4a5 in 46.0 seconds with the worktree unchanged. The product DLL SHA-256 was ea0991fa1a9d1837af92269e03b59780c2f14e7371a6bb0039cb813521686fa4.; The gate recorded 1,502 component passes, including 82 toolbar, six city-roster and three downed-target tests. Source validation passed 153/153, assembly contracts 825/825, click composition 21/21 across both patch orders, archive bounds 9/9, the six-pair archive adapter 22/22 and target-liveness source checks 3/3.; The area adapter compiler exited 1 with the same 63 diagnostics on base and candidate. The comparison passed. The adapter itself did not execute.; The pre-commit mutation run printed four targeted failures. Three mutations changed the toolbar test double and one changed the production placement rule.
- **Source inspection only:** The residual toolbar and reference-lifetime failures below follow from the production branches. The supplied gate does not exercise those exact sequences.; The city adapter returns the persistent pet linked to an eligible roster owner. The mounted attack path admits an explicit order on an already-unconscious living enemy. Actual panel selection, equipment changes and attack rolls remain unobserved on this binary.; The conscious-prone trace found no mounted-specific rejection. The reported weapon, the enemy's consciousness and the precise cause of the owner's original combat incident remain unknown.
- **Required gaps:** Preserve the player's effective slot through native rebinds, immediate removal and KMC's own stale-slot repair.; Release copy-registry references during first-time reserve migration and disabled save/load.; Correct the evidence type for the Charge and routine-continuation target filters in the acceptance record.
- **Limitations:** The reviewer used read-only inspection. The toolbar component tests exercise production domain decisions with a double of the native bar and service callbacks.; No native transaction ran on this candidate. The handoff identifies the missing commit-bound package, outdated capital reader and missing UI interaction and enemy-posture drivers. Restoration was inapplicable.

#### Summary

- **Delivered behavior:** The candidate preserves placement intent using the existing archive rows, projects eligible owners' persistent pets into the capital roster, and carries an explicit order on an already-unconscious living target through mounted attack admission. The two city reports share a roster transition. Shortcut loss follows temporary fact and slot cleanup, while missing portraits follow the native in-game pet filter.
- **Scope reviewed:** The complete maintenance mission on branch codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through a009bd952abac9e115746fc114a15bd40f88b4a5, for [draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57).
- **Pending later-phase work:** none
- **Unresolved questions:** Which transition removed the owner's original shortcut, and which weapon and enemy state produced the reported ranged failure?
- **Remaining owner review:** Confirm shortcut placement through mounted and dismounted city arrivals, interior transitions, and saves inside and outside the capital.; Select a reserve owner's pet in Inventory and Character, check its identity and equipment, make a permitted equipment change, and reopen both panels after a load.; Compare mounted and dismounted longbow attack rolls against standing, conscious-prone and already-unconscious enemies at near and ordinary firing distances in real time and turn based. Check action expenditure, modifiers, blocked sight and out-of-range approach.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a2004c-f962-7893-ac5d-edc9baa782d1`, duration 977.5s, response sha256 `7afd1a84f79e6b6253b1724dbb739c757a169d521baa570ad636f69601b3c046`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r21-ad3a37`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "a009bd952abac9e115746fc114a15bd40f88b4a5",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit. The immediate-save fix is verified. Source inspection still shows supported toolbar sequences that save the wrong slot, undo a deliberate removal, or leave two callable shortcuts. The copy registry also retains native references across two cleanup paths. The mission permits deferring the documented game checks to the owner.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the detached HEAD, mission SHA-256 and gate-log SHA-256. The feature diff passed git diff --check.",
      "The controller receipt and CLI transcript confirm `-Tier Fast -Focused Test-CityToolbarProneFocused.ps1` passed on a009bd952abac9e115746fc114a15bd40f88b4a5 in 46.0 seconds with the worktree unchanged. The product DLL SHA-256 was ea0991fa1a9d1837af92269e03b59780c2f14e7371a6bb0039cb813521686fa4.",
      "The gate recorded 1,502 component passes, including 82 toolbar, six city-roster and three downed-target tests. Source validation passed 153/153, assembly contracts 825/825, click composition 21/21 across both patch orders, archive bounds 9/9, the six-pair archive adapter 22/22 and target-liveness source checks 3/3.",
      "The area adapter compiler exited 1 with the same 63 diagnostics on base and candidate. The comparison passed. The adapter itself did not execute.",
      "The pre-commit mutation run printed four targeted failures. Three mutations changed the toolbar test double and one changed the production placement rule."
    ],
    "inspected": [
      "The mission, AGENTS.md, validation policy, current project and resume records, X01 compatibility notes, and the complete 34-file feature diff.",
      "Toolbar fact leasing, native bad-slot callbacks, copy tracking, reserve migration, save capture and restore, area suspension, retained remount and load fencing.",
      "The capital roster projection and its native adapter, mounted attack admission and execution paths, focused scripts and fixtures, acceptance record, controller evidence and prior findings ledger."
    ],
    "limitations": [
      "The reviewer used read-only inspection. The toolbar component tests exercise production domain decisions with a double of the native bar and service callbacks.",
      "No native transaction ran on this candidate. The handoff identifies the missing commit-bound package, outdated capital reader and missing UI interaction and enemy-posture drivers. Restoration was inapplicable."
    ],
    "required_gaps": [
      "Preserve the player's effective slot through native rebinds, immediate removal and KMC's own stale-slot repair.",
      "Release copy-registry references during first-time reserve migration and disabled save/load.",
      "Correct the evidence type for the Charge and routine-continuation target filters in the acceptance record."
    ],
    "source_inspection_only": [
      "The residual toolbar and reference-lifetime failures below follow from the production branches. The supplied gate does not exercise those exact sequences.",
      "The city adapter returns the persistent pet linked to an eligible roster owner. The mounted attack path admits an explicit order on an already-unconscious living enemy. Actual panel selection, equipment changes and attack rolls remain unobserved on this binary.",
      "The conscious-prone trace found no mounted-specific rejection. The reported weapon, the enemy's consciousness and the precise cause of the owner's original combat incident remain unknown."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 occupied slots",
        "\u00a74 save/load"
      ],
      "actual": "With slot 3 occupied, CopyYields promotes the rebound copy at 7 even though the player's moved binding exists at 11. That promotion clears the interrupted state and removes 7 from the copy registry. The next report uses PublicBindingIndex, which selects 7 and ignores 11. Both bindings survive live repair, and capture writes 7.",
      "blocking_rationale": "Saving can reverse a deliberate move and reload the wrong toolbar slot.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Fireball stays at 3. The moved shortcut at 11 is the sole live and saved public binding.",
      "id": "R19-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2044
      },
      "priority": "P2",
      "requested_correction": "Use the bar's effective player binding when classifying native reports. Promote a recorded copy as the fallback only when the occupied former slot has no newer player binding elsewhere.",
      "requested_evidence": "Extend MoveBeyondSurvivingCopy with Fireball at 3 and the existing pass that rebinds 7 and 11. Check update-first and save-first order, one kind-6 row at 11, one placement marker, and a cold restore at 11.",
      "title": "An occupied former slot lets a lower copy replace a moved shortcut",
      "trigger": "Native empties 3 with copies at 7 and 9. Slot 3 receives Fireball, the player moves 9 to 11, and CleanupBadSlots rebinds 7 and 11 before KMC reconciles."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 occupied slots",
        "\u00a74 save/load"
      ],
      "actual": "After native rebinds the fallback at 7, its arrangement is live and 9 remains a recorded secondary. If the player removes 7 immediately, ChosenSlotFree returns false because the arrangement is live. EffectiveBindingIndex then returns the sole remaining copy at 9. Repair or capture adopts 9.",
      "blocking_rationale": "A deliberate removal persists or disappears depending on whether the player acts before the next KMC update.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Removing the live fallback leaves Fireball at 3 and only the placement marker in the save.",
      "id": "R18-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlSlotPolicy.cs",
        "line": 166
      },
      "priority": "P2",
      "requested_correction": "Distinguish a removed live choice from an interrupted choice whose slot is occupied. Discard surviving secondary copies when the player removes the live choice.",
      "requested_evidence": "In OccupiedChosenSlotWithTwoCopies, remove 7 immediately after the rebind pass, before the first repair or save. Verify zero shortcuts, Fireball at 3 and a marker-only archive through cold restore. Retain the existing after-repair control.",
      "title": "Removing the fallback before repair promotes another recorded copy",
      "trigger": "In the existing Fireball-at-3 fixture, native rebinds 7 and 9. The player removes 7 as soon as that pass returns."
    },
    {
      "acceptance_ids": [
        "\u00a74 stale ability data",
        "\u00a74 no duplicate shortcuts",
        "\u00a74 save/load"
      ],
      "actual": "ReconcilePublicControlSlots rebinds stale slot 3 to the live fact and leaves the already-callable slot at 9. No native emptying has populated recordedCopies, so the copy-removal loop skips this bar. Both slots remain callable on later updates, while the archive selects only 3.",
      "blocking_rationale": "KMC's repair creates a second callable shortcut. Removing the selected shortcut can then leave an unintended control on the bar.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Live repair and save-first recovery agree on the documented first canonical slot, with one callable shortcut at 3.",
      "id": "R21-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1923
      },
      "priority": "P2",
      "requested_correction": "Reconcile stale canonical bindings against the effective slot even when KMC performs the first repair and the native observer has recorded no copies.",
      "requested_evidence": "Run the existing StaleThenCurrentCanonical setup update-first. Assert one callable shortcut at 3 after two updates. Remove it and verify a marker-only save and reload, with the ordinary slot at 0 preserved.",
      "title": "Repairing a stale canonical binding can create two callable shortcuts",
      "trigger": "A rider has stale canonical data at 3 and current data at 9. KMC updates before the first save or native cleanup report."
    },
    {
      "acceptance_ids": [
        "\u00a74 discarded entity cleanup",
        "\u00a74 older city saves",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "AdmitReserveBindings clears the reserve bar with its own SetSlot loop. It bypasses both cleanup methods that now call ForgetCopies. ReconcilePublicControlSlots prunes candidates only, so the recorded slot and its AbilityData can remain reachable after the reserve scan clears the bar and even after the rider leaves the roster.",
      "blocking_rationale": "The registry keeps discarded native objects alive in a continuing player world.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Clearing a reserve rider's bindings releases their slot objects while preserving the chosen index and pet identity.",
      "id": "R20-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2339
      },
      "priority": "P2",
      "requested_correction": "Release or prune the rider's recorded copies when reserve migration clears its bindings, including riders first encountered in reserve.",
      "requested_evidence": "Start with an older capital bar on a never-observed reserve rider. Have GetSlot empty 3 and record a surviving copy at 9 before the first reserve scan. Verify the scan releases that object in the same world and a same-pet return restores 3 exactly once.",
      "title": "First-time reserve migration retains the slot objects it clears",
      "trigger": "Native observes a bad slot on a reserve rider before KMC first scans that rider's surviving bindings."
    },
    {
      "acceptance_ids": [
        "\u00a74 mod disable",
        "\u00a74 world disposal",
        "\u00a77 save/lifecycle paths"
      ],
      "actual": "Disabled archive capture still calls CaptureLiveSlots. A surviving public slot reaches IsRecordedCopyAt, and CopiesOf assigns Game.Instance.Player to recordedCopiesWorld even on an empty lookup. BeginSelectedLoad clears the disabled load fence but leaves this separate strong reference. UMM stops ordinary updates while disabled.",
      "blocking_rationale": "A disabled save can keep an entire outgoing player world reachable after a subsequent load.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Disabled save sanitation releases the old world by the time native loading replaces it.",
      "id": "R21-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2161
      },
      "priority": "P2",
      "requested_correction": "Keep an empty or disabled copy-registry lookup from acquiring a strong player reference. Preserve the world-identity check for active observations.",
      "requested_evidence": "Extend DisabledSaveIsolation with a registry lifetime assertion. Save the stale-slot fixture while disabled, replace the player without an enabled frame, and verify the registry releases the outgoing world.",
      "title": "A disabled save makes an empty copy registry retain the player",
      "trigger": "While KMC is disabled, save an older compatible world with a stale public shortcut, then load another world before re-enabling KMC."
    },
    {
      "acceptance_ids": [
        "\u00a77 evidence type for each case",
        "\u00a78 offline and native handoff"
      ],
      "actual": "The acceptance row labels all four claims PASS/UNIT. The gate exercises the explicit-order predicate, automatic-intent flag and ally refusal. The Charge and continuation tests take preclassified target-valid or target-down inputs. The checks that exclude unconscious Charge and replacement targets are visible in the integration source.",
      "blocking_rationale": "The required acceptance record overstates which target filters were exercised.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each negative control identifies the evidence that actually establishes it.",
      "id": "R21-F3",
      "location": {
        "file": "docs/CITY-AND-PRONE-HANDOFF.md",
        "line": 720
      },
      "priority": "P3",
      "requested_correction": "Label the Charge and replacement-target filters as source traces, or cite a focused execution that passes an unconscious unit through those filters.",
      "requested_evidence": "A corrected acceptance row identifying the executed assertions and the traced integration checks is sufficient.",
      "title": "The combat acceptance row labels two source traces as unit-test results",
      "trigger": "Reading the negative-control evidence for automatic proposals, Charge, routine continuation and a downed ally."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R17-F2",
      "rationale": "CapturePersistentSlots settles the pending native write before classifying the bar. The gate covers an immediate save after native emptied 9, cold restore at 9, and a marker-only save when the player removed the slot after an aborted report."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R18-F1",
      "rationale": "The requested after-repair sequence passes. Removing the same live fallback before the first repair still promotes the recorded copy at 9."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R19-F2",
      "rationale": "The move to 11 survives the rebind while 3 is empty. With 3 occupied, the first rebind promotes 7 despite the moved binding at 11, and capture selects 7."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R20-F1",
      "rationale": "An observed rider departing for reserve now releases its copies. A rider first encountered in reserve follows a separate clearing loop that retains the same discarded slot objects."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r21-ad3a37",
  "round": 21,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate preserves placement intent using the existing archive rows, projects eligible owners' persistent pets into the capital roster, and carries an explicit order on an already-unconscious living target through mounted attack admission. The two city reports share a roster transition. Shortcut loss follows temporary fact and slot cleanup, while missing portraits follow the native in-game pet filter.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Confirm shortcut placement through mounted and dismounted city arrivals, interior transitions, and saves inside and outside the capital.",
      "Select a reserve owner's pet in Inventory and Character, check its identity and equipment, make a permitted equipment change, and reopen both panels after a load.",
      "Compare mounted and dismounted longbow attack rolls against standing, conscious-prone and already-unconscious enemies at near and ordinary firing distances in real time and turn based. Check action expenditure, modifiers, blocked sight and out-of-range approach."
    ],
    "scope_reviewed": "The complete maintenance mission on branch codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through a009bd952abac9e115746fc114a15bd40f88b4a5, for [draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57).",
    "unresolved_questions": [
      "Which transition removed the owner's original shortcut, and which weapon and enemy state produced the reported ranged failure?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
