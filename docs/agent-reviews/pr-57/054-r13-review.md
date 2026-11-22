<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r13-review part=1/1 sha256=232204e8b185f8e2848c029eda8f84f5fb68a15d710ca15d88ff4deb050bfcb0 -->
### Codex GPT-6.2 Astra — review — round 13 — candidate `59eda25267`

**Decision:** `changes_required` — The focused gate passed on the requested commit, and the candidate fixes R12-F1 through R12-F3. Toolbar persistence still has paths that lose a chosen shortcut, duplicate it, or produce conflicting archive rows. The new native cleanup observer also needs evidence for how it recognizes a rebind and a guard against applying a delayed observation to another world. These are development issues that can be addressed offline.

#### Findings

**R12-F4** · P2 · evidence_gap · blocks readiness — The rebind regression bypasses the production decision
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2036`; acceptance: §4 deliberate removal, §4 stale ability data, §7 focused contract evidence
- Trigger: CleanupBadSlots repairs a stale canonical shortcut, then the player removes it before KMC's next update.
- Expected: The production classifier recognizes the rebind operation used by the installed Kingmaker build and distinguishes it from native emptying.
- Actual: ResolvePendingBadSlots requires a different slot object with the same blueprint. The assembly checks establish the method token, signature and call to IsBad. They do not establish that a successful rebind replaces the slot object. NativeCleanupRebind in the fixture changes the modeled data and passes true directly to the domain, bypassing the production predicate. An in-place AbilityData replacement would therefore pass the regression while production classified it as emptying.
- Why it blocks / does not: The fix for R12-F4 depends on an unverified object-identity assumption at the exact boundary that decides whether a player's removal is reversed.
- Requested correction: Verify the rebind operation against the pinned assembly. If slot replacement is not guaranteed, classify the outcome using the resulting live ability data. Exercise the classifier that production calls.
- Requested evidence: Provide an offline assembly-contract observation identifying what native cleanup writes on a successful rebind. Drive that shape and native emptying through the production classifier. After a rebind, removal before the next update must yield a record-only save. After emptying, the same pet must recover one shortcut.

**R13-F1** · P2 · defect · blocks readiness — Native emptying of a returned alias is mistaken for player removal
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2074`; acceptance: §4 temporary mount unavailability, §4 existing save compatibility, §4 player removal
- Trigger: A save returns the legacy Mount or Dismount alias. Its shell becomes stale before the next regular update while the same rider's canonical control remains available.
- Expected: Native emptying interrupts the known live placement. The same pet gets one canonical shortcut at its chosen index. A player removal still stays removed.
- Actual: Lease release calls ObserveBound for the returned alias. If GetSlot then empties it, ApplySlotInvalidation calls AdmitSurvivingBinding, which declines any actor with an existing entry. The entry remains continuously bound. With the canonical fact active and no binding on the bar, ResolveUnbound or the next save boundary changes the arrangement to OffBar.
- Why it blocks / does not: A native UI read can permanently erase an untouched shortcut for the same companion. The passing older-alias cleanup test starts with no placement entry, while the returned-alias tests leave native cleanup out.
- Requested correction: Distinguish discovery of an old unrecorded alias from native invalidation of the alias currently representing a recorded live placement. Apply the same distinction to the outgoing world's saved records during a load.
- Requested evidence: Return the same pet's alias at 9 from a save scope, invalidate its shell and have native cleanup empty it before migration. Check both update-first and save-first ordering, one restored binding at 9 after reload, and a deliberate-removal control.

**R13-F2** · P2 · defect · blocks readiness — Save release can autofill a second copy of a first manual placement
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2271`; acceptance: §4 chosen toolbar slot, §4 one-time placement, §4 no duplicates
- Trigger: A save observes a manual canonical binding before the regular update has recorded it, with native autofill enabled at release.
- Expected: Observing a manually placed control satisfies the actor's one-time placement. Save release returns exactly that shortcut.
- Actual: ObserveArrangementOnBar records a visible canonical index in placementIntent without adding the actor to placementRecorded. At save release, ReconcileChosenSlot respects the pending lease, but TryInitialPlacement still sees an unrecorded actor and an empty bar. It can place the control in the first empty main-bar slot. Lease release then returns the original binding to its chosen index. ReconcilePublicControlSlots subsequently keeps both canonical bindings.
- Why it blocks / does not: Saving can turn one player-authored binding into two callable shortcuts. A later save can select the lower autofilled index and persist the wrong arrangement.
- Requested correction: Account for an observed public binding in the one-time placement record before stripping the bar. Ensure initial placement cannot compete with a pending public-slot lease.
- Requested evidence: Start with one canonical binding at 9, no placement record, autofill enabled and a lower empty main-bar slot. Save before ordinary reconciliation, inspect the continuing bar through two updates, then save and reload. Require one shortcut at 9 throughout and one placement record.

**R13-F3** · P1 · defect · blocks readiness — A remembered shortcut can collide with an occupied archive index
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2567`; acceptance: §4 occupied slots, §4 save/load, §7 archive validation
- Trigger: A save starts before reconciliation while a remembered public-control slot is occupied by another KMC binding that the live writer serializes.
- Expected: The occupied slot belongs to its current binding. The archive preserves that binding and records the mount shortcut as off the bar.
- Actual: CaptureAbsentIntent passes only the kinds of previously written rows to AbsentRowsAfterLive. If a live non-public KMC binding, such as a callable legacy Mounted Charge awaiting migration, occupies the interrupted shortcut's remembered index, the live writer emits its row there and the absent writer adds kind 6 at the same index. MountedArchiveData.ValidateSlots rejects the duplicate actor/index key at the save header.
- Why it blocks / does not: An occupied slot can make the archive invalid and fail a save. The existing duplicate-key validator correctly exposes the conflict.
- Requested correction: Check the remembered index against occupied slots and emitted actor/index keys before adding the absent public row. Preserve the current occupant, the placement record and the archive's duplicate-key refusal.
- Requested evidence: Capture a rider with interrupted intent at 9 and a callable legacy Mounted Charge at 9 before reconciliation. Pass the resulting rows through the production codec. Require one Charge row at 9, one placement record, successful decode and no mount shortcut over the restored Charge.

**R13-F4** · P2 · defect · blocks readiness — Deferred cleanup can write an outgoing rider's slot into a new world
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2046`; acceptance: §4 world disposal, §4 save/load identity, §7 exception and lifecycle cleanup
- Trigger: Native cleanup queues an observation and exits exceptionally. The player world is replaced before KMC resolves the pending list.
- Expected: A deferred observation applies only to the world and rider instance that produced it. An abandoned load can recover the outgoing arrangement, while a completed replacement discards it.
- Actual: PendingSlotInvalidation holds the old unit and its slot. Roster identity is checked when IsBad reports, but ApplySlotInvalidation does not recheck it. If CleanupBadSlots fails to return normally, Update resolves the pending observations before its early returns. After player replacement, ArrangementsAccepted releases the outgoing fence and the stale observation can write into the incoming placement records under the same actor ID.
- Why it blocks / does not: The new exception-recovery path can import an outgoing toolbar choice into a different save, including one in which the shortcut was removed.
- Requested correction: Bind pending observations to their originating player world and validate the rider instance again before applying them. Discard work from a replaced world and preserve same-world exception recovery.
- Requested evidence: Queue an invalidation for outgoing rider A at 9, omit the cleanup postfix, replace the player with a new world containing a different instance of A, then resolve the pending work. Verify the incoming bar and archive retain their own arrangement. Include an abandoned-load control in which the outgoing world survives.

#### Prior finding assessments

- **R12-F1** — `fix_verified` — Capture classification now checks the exact selected index used by the live writer. The candidate regression covers stale canonical data at 3 followed by callable data at 9, and the codec check preserves the binding at 3.
- **R12-F2** — `fix_verified` — Boundary observation recognizes a visible returned alias. The candidate regression checks the continuing world, a third save and a reload for both the untouched and deliberately removed cases.
- **R12-F3** — `fix_verified` — Companion retirement detects and clears both exact legacy aliases before repair or capture. The candidate regression covers update-first and save-first replacement, unrelated occupied slots and a same-pet control.
- **R12-F4** — `still_open` — The domain now handles a supplied rebound outcome correctly. The recorded test supplies that outcome directly, and the installed-assembly assertions do not establish the object-replacement assumption used to derive it in production.

#### Evidence assessment

- **Inspected:** Controller mission, prior findings ledger, developer report, CLI transcript, gate receipt and log, feature diff, AGENTS.md, validation policy, current handoff and X01 compatibility guidance.; Toolbar observation, fact reconciliation, one-time placement, serialization leases, archive row construction, load fencing, area replay, native patch registration, and the corresponding test doubles and codec fixtures.; Capital roster projection from UIUtility.GetGroup, persistent pet identity and eligibility checks, mounted stock-click admission, pair-command target validation, and focused composition coverage.
- **Executed verification:** Confirmed the guarded push and controller focused gate on 59eda25267446aad9b6d6e45986875a13fdef1f3 in the CLI transcript. The gate ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1, exited 0 in 45.4 seconds, and reported an unchanged worktree. The supplied log's SHA-256 matches the receipt.; The candidate log records 1468 component passes, 153 source-validation passes, 821 Kingmaker contract passes, 21 click-composition passes covering both patch orders, 8 archive-codec passes and 22 six-pair adapter passes. The focused wrapper passed all five children.; The area-adapter comparison found the same 63 compiler errors on base and candidate. Its runtime assertions did not execute.; The pre-commit mutation run printed the four named toolbar failures and exited 1. The candidate gate subsequently printed PASS for all four regressions.; Reviewer read-only checks confirmed the detached HEAD and found no whitespace errors in the feature range.
- **Source inspection only:** The capital issues share a trigger. The toolbar depends on a leased ability fact and remembered placement, while the native city roster gets pets from controllable characters. The projection adds the eligible owner's existing pet instance.; The attack change carries the explicit player's already-unconscious target through mounted admission and execution checks. The conscious-tripped case is supported by a source trace. The owner's exact weapon, target state and original failure stage remain unconfirmed.; The outstanding toolbar sequences below follow from production control flow. They were not reproduced in game.
- **Required gaps:** Establish the installed CleanupBadSlots rebind operation and exercise the production outcome classifier.; Cover native emptying of an already-recorded alias, first-placement autofill during save release, conflicting occupied-slot archive rows, and delayed observations across world replacement.
- **Limitations:** The reviewer ran read-only checks. Executed build and test results come from the supplied controller evidence.; The toolbar fixture models native bars and callbacks. The codec checks exercise production encoding and validation, while supplying the remembered index to the row writer.; No native transaction ran on this candidate. The handoff identifies the missing candidate-bound package and the scenario-specific driver and reader requirements. Native toolbar timing, city equipment interaction and matched attack resolution remain unobserved.

#### Summary

- **Delivered behavior:** The candidate adds per-rider toolbar intent to the existing persistence path, projects eligible reserve owners' persistent pets into the capital UI roster, and admits explicit mounted attacks on enemies already unconscious when ordered. The city projection and attack changes have offline evidence. Toolbar correctness still blocks readiness.
- **Scope reviewed:** Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 59eda25267446aad9b6d6e45986875a13fdef1f3. No native transaction or restoration occurred.
- **Pending later-phase work:** none
- **Unresolved questions:** Does the pinned CleanupBadSlots rebind branch replace the slot object or update the ability on the existing object?; Which weapon and target life state produced the owner's original prone-target report?
- **Remaining owner review:** After the implementation blockers close, verify capital and interior transitions, shortcut edits and cold loads, and selection and equipment changes on the exact reserve pet.; Compare mounted and dismounted longbow attacks on standing, conscious-tripped and unconscious enemies through actual resolution in real-time and turn-based play, including near and normal range, blocked line of sight, spent actions and penalties.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff2f-fd25-7551-875c-54294f70ce32`, duration 356.2s, response sha256 `714d63692dead550934c16e333659f3b43f0da75365176590e2bcc285038d75c`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r13-4b39ca`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "59eda25267446aad9b6d6e45986875a13fdef1f3",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit, and the candidate fixes R12-F1 through R12-F3. Toolbar persistence still has paths that lose a chosen shortcut, duplicate it, or produce conflicting archive rows. The new native cleanup observer also needs evidence for how it recognizes a rebind and a guard against applying a delayed observation to another world. These are development issues that can be addressed offline.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the guarded push and controller focused gate on 59eda25267446aad9b6d6e45986875a13fdef1f3 in the CLI transcript. The gate ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1, exited 0 in 45.4 seconds, and reported an unchanged worktree. The supplied log's SHA-256 matches the receipt.",
      "The candidate log records 1468 component passes, 153 source-validation passes, 821 Kingmaker contract passes, 21 click-composition passes covering both patch orders, 8 archive-codec passes and 22 six-pair adapter passes. The focused wrapper passed all five children.",
      "The area-adapter comparison found the same 63 compiler errors on base and candidate. Its runtime assertions did not execute.",
      "The pre-commit mutation run printed the four named toolbar failures and exited 1. The candidate gate subsequently printed PASS for all four regressions.",
      "Reviewer read-only checks confirmed the detached HEAD and found no whitespace errors in the feature range."
    ],
    "inspected": [
      "Controller mission, prior findings ledger, developer report, CLI transcript, gate receipt and log, feature diff, AGENTS.md, validation policy, current handoff and X01 compatibility guidance.",
      "Toolbar observation, fact reconciliation, one-time placement, serialization leases, archive row construction, load fencing, area replay, native patch registration, and the corresponding test doubles and codec fixtures.",
      "Capital roster projection from UIUtility.GetGroup, persistent pet identity and eligibility checks, mounted stock-click admission, pair-command target validation, and focused composition coverage."
    ],
    "limitations": [
      "The reviewer ran read-only checks. Executed build and test results come from the supplied controller evidence.",
      "The toolbar fixture models native bars and callbacks. The codec checks exercise production encoding and validation, while supplying the remembered index to the row writer.",
      "No native transaction ran on this candidate. The handoff identifies the missing candidate-bound package and the scenario-specific driver and reader requirements. Native toolbar timing, city equipment interaction and matched attack resolution remain unobserved."
    ],
    "required_gaps": [
      "Establish the installed CleanupBadSlots rebind operation and exercise the production outcome classifier.",
      "Cover native emptying of an already-recorded alias, first-placement autofill during save release, conflicting occupied-slot archive rows, and delayed observations across world replacement."
    ],
    "source_inspection_only": [
      "The capital issues share a trigger. The toolbar depends on a leased ability fact and remembered placement, while the native city roster gets pets from controllable characters. The projection adds the eligible owner's existing pet instance.",
      "The attack change carries the explicit player's already-unconscious target through mounted admission and execution checks. The conscious-tripped case is supported by a source trace. The owner's exact weapon, target state and original failure stage remain unconfirmed.",
      "The outstanding toolbar sequences below follow from production control flow. They were not reproduced in game."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 stale ability data",
        "\u00a77 focused contract evidence"
      ],
      "actual": "ResolvePendingBadSlots requires a different slot object with the same blueprint. The assembly checks establish the method token, signature and call to IsBad. They do not establish that a successful rebind replaces the slot object. NativeCleanupRebind in the fixture changes the modeled data and passes true directly to the domain, bypassing the production predicate. An in-place AbilityData replacement would therefore pass the regression while production classified it as emptying.",
      "blocking_rationale": "The fix for R12-F4 depends on an unverified object-identity assumption at the exact boundary that decides whether a player's removal is reversed.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "The production classifier recognizes the rebind operation used by the installed Kingmaker build and distinguishes it from native emptying.",
      "id": "R12-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2036
      },
      "priority": "P2",
      "requested_correction": "Verify the rebind operation against the pinned assembly. If slot replacement is not guaranteed, classify the outcome using the resulting live ability data. Exercise the classifier that production calls.",
      "requested_evidence": "Provide an offline assembly-contract observation identifying what native cleanup writes on a successful rebind. Drive that shape and native emptying through the production classifier. After a rebind, removal before the next update must yield a record-only save. After emptying, the same pet must recover one shortcut.",
      "title": "The rebind regression bypasses the production decision",
      "trigger": "CleanupBadSlots repairs a stale canonical shortcut, then the player removes it before KMC's next update."
    },
    {
      "acceptance_ids": [
        "\u00a74 temporary mount unavailability",
        "\u00a74 existing save compatibility",
        "\u00a74 player removal"
      ],
      "actual": "Lease release calls ObserveBound for the returned alias. If GetSlot then empties it, ApplySlotInvalidation calls AdmitSurvivingBinding, which declines any actor with an existing entry. The entry remains continuously bound. With the canonical fact active and no binding on the bar, ResolveUnbound or the next save boundary changes the arrangement to OffBar.",
      "blocking_rationale": "A native UI read can permanently erase an untouched shortcut for the same companion. The passing older-alias cleanup test starts with no placement entry, while the returned-alias tests leave native cleanup out.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Native emptying interrupts the known live placement. The same pet gets one canonical shortcut at its chosen index. A player removal still stays removed.",
      "id": "R13-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2074
      },
      "priority": "P2",
      "requested_correction": "Distinguish discovery of an old unrecorded alias from native invalidation of the alias currently representing a recorded live placement. Apply the same distinction to the outgoing world's saved records during a load.",
      "requested_evidence": "Return the same pet's alias at 9 from a save scope, invalidate its shell and have native cleanup empty it before migration. Check both update-first and save-first ordering, one restored binding at 9 after reload, and a deliberate-removal control.",
      "title": "Native emptying of a returned alias is mistaken for player removal",
      "trigger": "A save returns the legacy Mount or Dismount alias. Its shell becomes stale before the next regular update while the same rider's canonical control remains available."
    },
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 one-time placement",
        "\u00a74 no duplicates"
      ],
      "actual": "ObserveArrangementOnBar records a visible canonical index in placementIntent without adding the actor to placementRecorded. At save release, ReconcileChosenSlot respects the pending lease, but TryInitialPlacement still sees an unrecorded actor and an empty bar. It can place the control in the first empty main-bar slot. Lease release then returns the original binding to its chosen index. ReconcilePublicControlSlots subsequently keeps both canonical bindings.",
      "blocking_rationale": "Saving can turn one player-authored binding into two callable shortcuts. A later save can select the lower autofilled index and persist the wrong arrangement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Observing a manually placed control satisfies the actor's one-time placement. Save release returns exactly that shortcut.",
      "id": "R13-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2271
      },
      "priority": "P2",
      "requested_correction": "Account for an observed public binding in the one-time placement record before stripping the bar. Ensure initial placement cannot compete with a pending public-slot lease.",
      "requested_evidence": "Start with one canonical binding at 9, no placement record, autofill enabled and a lower empty main-bar slot. Save before ordinary reconciliation, inspect the continuing bar through two updates, then save and reload. Require one shortcut at 9 throughout and one placement record.",
      "title": "Save release can autofill a second copy of a first manual placement",
      "trigger": "A save observes a manual canonical binding before the regular update has recorded it, with native autofill enabled at release."
    },
    {
      "acceptance_ids": [
        "\u00a74 occupied slots",
        "\u00a74 save/load",
        "\u00a77 archive validation"
      ],
      "actual": "CaptureAbsentIntent passes only the kinds of previously written rows to AbsentRowsAfterLive. If a live non-public KMC binding, such as a callable legacy Mounted Charge awaiting migration, occupies the interrupted shortcut's remembered index, the live writer emits its row there and the absent writer adds kind 6 at the same index. MountedArchiveData.ValidateSlots rejects the duplicate actor/index key at the save header.",
      "blocking_rationale": "An occupied slot can make the archive invalid and fail a save. The existing duplicate-key validator correctly exposes the conflict.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The occupied slot belongs to its current binding. The archive preserves that binding and records the mount shortcut as off the bar.",
      "id": "R13-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2567
      },
      "priority": "P1",
      "requested_correction": "Check the remembered index against occupied slots and emitted actor/index keys before adding the absent public row. Preserve the current occupant, the placement record and the archive's duplicate-key refusal.",
      "requested_evidence": "Capture a rider with interrupted intent at 9 and a callable legacy Mounted Charge at 9 before reconciliation. Pass the resulting rows through the production codec. Require one Charge row at 9, one placement record, successful decode and no mount shortcut over the restored Charge.",
      "title": "A remembered shortcut can collide with an occupied archive index",
      "trigger": "A save starts before reconciliation while a remembered public-control slot is occupied by another KMC binding that the live writer serializes."
    },
    {
      "acceptance_ids": [
        "\u00a74 world disposal",
        "\u00a74 save/load identity",
        "\u00a77 exception and lifecycle cleanup"
      ],
      "actual": "PendingSlotInvalidation holds the old unit and its slot. Roster identity is checked when IsBad reports, but ApplySlotInvalidation does not recheck it. If CleanupBadSlots fails to return normally, Update resolves the pending observations before its early returns. After player replacement, ArrangementsAccepted releases the outgoing fence and the stale observation can write into the incoming placement records under the same actor ID.",
      "blocking_rationale": "The new exception-recovery path can import an outgoing toolbar choice into a different save, including one in which the shortcut was removed.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A deferred observation applies only to the world and rider instance that produced it. An abandoned load can recover the outgoing arrangement, while a completed replacement discards it.",
      "id": "R13-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2046
      },
      "priority": "P2",
      "requested_correction": "Bind pending observations to their originating player world and validate the rider instance again before applying them. Discard work from a replaced world and preserve same-world exception recovery.",
      "requested_evidence": "Queue an invalidation for outgoing rider A at 9, omit the cleanup postfix, replace the player with a new world containing a different instance of A, then resolve the pending work. Verify the incoming bar and archive retain their own arrangement. Include an abandoned-load control in which the outgoing world survives.",
      "title": "Deferred cleanup can write an outgoing rider's slot into a new world",
      "trigger": "Native cleanup queues an observation and exits exceptionally. The player world is replaced before KMC resolves the pending list."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F1",
      "rationale": "Capture classification now checks the exact selected index used by the live writer. The candidate regression covers stale canonical data at 3 followed by callable data at 9, and the codec check preserves the binding at 3."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F2",
      "rationale": "Boundary observation recognizes a visible returned alias. The candidate regression checks the continuing world, a third save and a reload for both the untouched and deliberately removed cases."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F3",
      "rationale": "Companion retirement detects and clears both exact legacy aliases before repair or capture. The candidate regression covers update-first and save-first replacement, unrelated occupied slots and a same-pet control."
    },
    {
      "assessment": "still_open",
      "finding_id": "R12-F4",
      "rationale": "The domain now handles a supplied rebound outcome correctly. The recorded test supplies that outcome directly, and the installed-assembly assertions do not establish the object-replacement assumption used to derive it in production."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r13-4b39ca",
  "round": 13,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds per-rider toolbar intent to the existing persistence path, projects eligible reserve owners' persistent pets into the capital UI roster, and admits explicit mounted attacks on enemies already unconscious when ordered. The city projection and attack changes have offline evidence. Toolbar correctness still blocks readiness.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After the implementation blockers close, verify capital and interior transitions, shortcut edits and cold loads, and selection and equipment changes on the exact reserve pet.",
      "Compare mounted and dismounted longbow attacks on standing, conscious-tripped and unconscious enemies through actual resolution in real-time and turn-based play, including near and normal range, blocked line of sight, spent actions and penalties."
    ],
    "scope_reviewed": "Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 59eda25267446aad9b6d6e45986875a13fdef1f3. No native transaction or restoration occurred.",
    "unresolved_questions": [
      "Does the pinned CleanupBadSlots rebind branch replace the slot object or update the ability on the existing object?",
      "Which weapon and target life state produced the owner's original prone-target report?"
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
