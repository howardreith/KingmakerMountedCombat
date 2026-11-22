<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r07-review part=1/1 sha256=ac6098f614aa472e2542d00399091e8e951c3479f0094b2f819caf2739c5d6d3 -->
### Codex GPT-6.2 Astra — review — round 7 — candidate `d27728b346`

**Decision:** `changes_required` — The focused gate passed on the requested commit, and R5-F1 is fixed. Two toolbar paths still block readiness. Area suspension or replay can give a replacement pet its predecessor’s slot, and a cold-loaded reserve rider from an older capital save can escape both placement capture and slot cleanup.

#### Findings

**R4-F2** · P2 · defect · blocks readiness — Area suspension and replay can transfer a retired slot to a replacement companion
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2025`; acceptance: §4 genuine companion removal, §4 city and interior transitions, §4 temporary absence versus permanent loss
- Trigger: A dismounted rider’s pet changes across an area transition while another mounted pair causes KMC to suspend and restore controls.
- Expected: Once the service observes that A was removed or replaced, both the live arrangement and any earlier in-world capture of A’s slot stay retired. Temporary non-resolution of A’s unchanged ID preserves slot 9.
- Actual: ObserveArrangementOnBar skips retirement for an empty link and passes the still-bound index to ObserveAtBoundary. That replaces A’s recorded identity with null. Suspension then marks slot 9 interrupted, and Revalidate can adopt B as its owner on resume. There is a second route through area replay. If resume detects a direct A-to-B change while B is unresolved, it forgets A’s entry. RestorePersistentSlots can then seed the older transfer row into that empty entry, allowing B to acquire slot 9 when it resolves.
- Why it blocks / does not: A city or interior transition can restore a shortcut that genuine companion removal should retire.
- Requested correction: Apply the permanent-loss decision before a boundary observation can replace the known pet identity. Ensure an older in-world transfer row cannot recreate an arrangement retired since capture. Preserve fresh-load seeding and same-pet restoration.
- Requested evidence: Keep one pair mounted to exercise area suspension and give a second, dismounted rider slot 9. Test a cleared link at suspension followed by B before resume. Also capture A’s row, change to an unresolved B during suspension, resume and replay the rows, then resolve B. In both cases verify slot 9 stays empty and the next save has no kind-6 row for it. Repeat with A’s unchanged ID and verify one restoration at 9.

**R7-F1** · P2 · defect · blocks readiness — An older capital save can leave a reserve rider’s shortcut outside capture and cleanup
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2458`; acceptance: §4 save/load inside the city, §4 existing save compatibility, §4 mod disable/removal cleanup
- Trigger: Load an older capital save whose reserve rider still carries a native Mount / Dismount slot but has no owned toolbar row, then save again or disable the mod before that rider rejoins the party.
- Expected: A surviving exact public-control binding on an eligible reserve rider supplies its chosen index. An ordinary resave carries that choice in owned rows and clears the runtime binding from native serialization. Disable/removal clears the binding.
- Actual: The handoff identifies an older capital-save shape with a stale native slot and no KMC slot row for the reserve rider. On cold load that rider has no placement entry and has never been observed in the new world. ClassifyArrangementsForCapture visits candidates and observed departures, CaptureAbsentIntent iterates existing entries, and CaptureAndClearManagedHotbarSlots visits candidates plus observed units. All three miss this rider. An immediate city resave can therefore carry the stale native reference again without recording the recoverable index. Disable cleanup uses the same candidate set.
- Why it blocks / does not: This affects existing saves from the reported city scenario. The surviving slot supplies enough information to recover the player’s choice, and leaving its runtime reference in native serialization also undermines cleanup.
- Requested correction: Handle surviving public-control bindings on current-world reserve riders when admitting or saving older data. Recover only an identifiable binding, preserve unrelated slots, and include the recovered rider in the applicable serialization and disable cleanup.
- Requested evidence: Start a fresh world with a reserve owner, a supported pet, a native public-control binding at 9, no kind-6/7 rows and no prior observation. Save before opening the rider’s bar or returning them to the party. Verify one kind-6 row at 9, one placement record, and no native KMC slot during serialization. Verify cold return restores 9 once and disable clears the surviving binding.

#### Prior finding assessments

- **R4-F2** — `fix_rejected` — The exact native-invalidation regression now passes for both fact-reactivation orders, and its same-ID control passes. The permanent-loss issue remains in area-boundary observation and in-world replay, where the previous pet’s chosen index can still reach its replacement.
- **R5-F1** — `fix_verified` — The exact slot invalidation now updates the owning load scope’s outgoing record. Abandonment restores that record when the original player survives, and world replacement discards it. The gate covers reactivated and inactive facts, one restored binding and archive row at 9, deliberate removal, and successful same-ID world replacement.

#### Evidence assessment

- **Inspected:** Mission, repository instructions, validation policy, current handoff, full feature diff, prior findings, developer transcript, gate receipt and log.; Toolbar reconciliation, area suspension and replay, save serialization, load ownership, capital roster projection, mounted attack admission and target continuation, and their focused tests.
- **Executed verification:** Confirmed snapshot HEAD and the SHA-256 of the controller gate log. The developer transcript records the guarded push and focused verification on d27728b3467aba07850dbc66eaeb30f9e1eb5768.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 in 23.7 seconds, with exit code 0 and an unchanged worktree. The log records 1,449 component tests, 153 source checks, 820 assembly contracts, 21 click-composition checks, 3 archive-bound checks and 22 six-pair adapter checks passing. Both new toolbar regressions passed.; The area-adapter compile comparison found the same 63 diagnostics on the base and candidate. The adapter itself did not execute. The tested product DLL had SHA-256 9dfacff0ac0c99e9c2b8e1c1231e6758a06b9542562aecbc5fa3bbce8233ee2d and MVID fbfa647d-6602-4985-a696-0c86c070e8f7.
- **Source inspection only:** The two blocking paths below were traced through production code. Neither sequence appears in the candidate gate.; The city projection selects the owner’s existing pet instance. The attack change carries an explicit order on an already-unconscious target through mounted admission. Equipment interaction and attack resolution on this candidate remain unobserved.
- **Required gaps:** Cover permanent pet loss across area suspension and replay, including a replacement whose persistent ID is present before its entity resolves.; Cover an older capital save with a surviving native control slot on a reserve rider and no owned placement row.
- **Limitations:** Reviewer verification was read-only. The candidate gate used component doubles, typed adapters, assembly inspection and composed command slices. No native transaction ran.; The handoff documents the unavailable candidate package route, capital reader and fixture requirements, and missing UI and prone-enemy driver steps. Native playtests remain explicitly deferred under mission §7.

#### Summary

- **Delivered behavior:** The candidate preserves many toolbar moves and removals across temporary absence, projects eligible owners’ persistent pets into the capital roster, and admits a mounted player’s explicit attack on an already-unconscious enemy. The two city symptoms share a roster transition but have distinct causes.
- **Scope reviewed:** Complete final development candidate for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through d27728b3467aba07850dbc66eaeb30f9e1eb5768.
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the weapon or distinguish a conscious tripped target from an unconscious one. The source trace found a mounted admission difference for the latter.
- **Remaining owner review:** After correction, verify the chosen shortcut across real capital and interior visits, forced dismount, and saves inside and outside the city.; Select a reserve owner’s pet through Inventory and Character, verify its identity and equipment, change an item, then reload and repeat.; Use matched longbow shots against standing, consciously prone and unconscious enemies at near and normal distances in RT and TB. Check actual rolls, movement, modifiers and action costs using the handoff’s negative controls.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1fed6-52c7-7df3-92ef-8870f72bebb5`, duration 351.5s, response sha256 `9b0666dcd56f6c376c0c3e57aff3a2255c76d77f5b860ad86a1b234ef878026d`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r07-5b5f71`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "d27728b3467aba07850dbc66eaeb30f9e1eb5768",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit, and R5-F1 is fixed. Two toolbar paths still block readiness. Area suspension or replay can give a replacement pet its predecessor\u2019s slot, and a cold-loaded reserve rider from an older capital save can escape both placement capture and slot cleanup.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed snapshot HEAD and the SHA-256 of the controller gate log. The developer transcript records the guarded push and focused verification on d27728b3467aba07850dbc66eaeb30f9e1eb5768.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 in 23.7 seconds, with exit code 0 and an unchanged worktree. The log records 1,449 component tests, 153 source checks, 820 assembly contracts, 21 click-composition checks, 3 archive-bound checks and 22 six-pair adapter checks passing. Both new toolbar regressions passed.",
      "The area-adapter compile comparison found the same 63 diagnostics on the base and candidate. The adapter itself did not execute. The tested product DLL had SHA-256 9dfacff0ac0c99e9c2b8e1c1231e6758a06b9542562aecbc5fa3bbce8233ee2d and MVID fbfa647d-6602-4985-a696-0c86c070e8f7."
    ],
    "inspected": [
      "Mission, repository instructions, validation policy, current handoff, full feature diff, prior findings, developer transcript, gate receipt and log.",
      "Toolbar reconciliation, area suspension and replay, save serialization, load ownership, capital roster projection, mounted attack admission and target continuation, and their focused tests."
    ],
    "limitations": [
      "Reviewer verification was read-only. The candidate gate used component doubles, typed adapters, assembly inspection and composed command slices. No native transaction ran.",
      "The handoff documents the unavailable candidate package route, capital reader and fixture requirements, and missing UI and prone-enemy driver steps. Native playtests remain explicitly deferred under mission \u00a77."
    ],
    "required_gaps": [
      "Cover permanent pet loss across area suspension and replay, including a replacement whose persistent ID is present before its entity resolves.",
      "Cover an older capital save with a surviving native control slot on a reserve rider and no owned placement row."
    ],
    "source_inspection_only": [
      "The two blocking paths below were traced through production code. Neither sequence appears in the candidate gate.",
      "The city projection selects the owner\u2019s existing pet instance. The attack change carries an explicit order on an already-unconscious target through mounted admission. Equipment interaction and attack resolution on this candidate remain unobserved."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 city and interior transitions",
        "\u00a74 temporary absence versus permanent loss"
      ],
      "actual": "ObserveArrangementOnBar skips retirement for an empty link and passes the still-bound index to ObserveAtBoundary. That replaces A\u2019s recorded identity with null. Suspension then marks slot 9 interrupted, and Revalidate can adopt B as its owner on resume. There is a second route through area replay. If resume detects a direct A-to-B change while B is unresolved, it forgets A\u2019s entry. RestorePersistentSlots can then seed the older transfer row into that empty entry, allowing B to acquire slot 9 when it resolves.",
      "blocking_rationale": "A city or interior transition can restore a shortcut that genuine companion removal should retire.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the service observes that A was removed or replaced, both the live arrangement and any earlier in-world capture of A\u2019s slot stay retired. Temporary non-resolution of A\u2019s unchanged ID preserves slot 9.",
      "id": "R4-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2025
      },
      "priority": "P2",
      "requested_correction": "Apply the permanent-loss decision before a boundary observation can replace the known pet identity. Ensure an older in-world transfer row cannot recreate an arrangement retired since capture. Preserve fresh-load seeding and same-pet restoration.",
      "requested_evidence": "Keep one pair mounted to exercise area suspension and give a second, dismounted rider slot 9. Test a cleared link at suspension followed by B before resume. Also capture A\u2019s row, change to an unresolved B during suspension, resume and replay the rows, then resolve B. In both cases verify slot 9 stays empty and the next save has no kind-6 row for it. Repeat with A\u2019s unchanged ID and verify one restoration at 9.",
      "title": "Area suspension and replay can transfer a retired slot to a replacement companion",
      "trigger": "A dismounted rider\u2019s pet changes across an area transition while another mounted pair causes KMC to suspend and restore controls."
    },
    {
      "acceptance_ids": [
        "\u00a74 save/load inside the city",
        "\u00a74 existing save compatibility",
        "\u00a74 mod disable/removal cleanup"
      ],
      "actual": "The handoff identifies an older capital-save shape with a stale native slot and no KMC slot row for the reserve rider. On cold load that rider has no placement entry and has never been observed in the new world. ClassifyArrangementsForCapture visits candidates and observed departures, CaptureAbsentIntent iterates existing entries, and CaptureAndClearManagedHotbarSlots visits candidates plus observed units. All three miss this rider. An immediate city resave can therefore carry the stale native reference again without recording the recoverable index. Disable cleanup uses the same candidate set.",
      "blocking_rationale": "This affects existing saves from the reported city scenario. The surviving slot supplies enough information to recover the player\u2019s choice, and leaving its runtime reference in native serialization also undermines cleanup.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A surviving exact public-control binding on an eligible reserve rider supplies its chosen index. An ordinary resave carries that choice in owned rows and clears the runtime binding from native serialization. Disable/removal clears the binding.",
      "id": "R7-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2458
      },
      "priority": "P2",
      "requested_correction": "Handle surviving public-control bindings on current-world reserve riders when admitting or saving older data. Recover only an identifiable binding, preserve unrelated slots, and include the recovered rider in the applicable serialization and disable cleanup.",
      "requested_evidence": "Start a fresh world with a reserve owner, a supported pet, a native public-control binding at 9, no kind-6/7 rows and no prior observation. Save before opening the rider\u2019s bar or returning them to the party. Verify one kind-6 row at 9, one placement record, and no native KMC slot during serialization. Verify cold return restores 9 once and disable clears the surviving binding.",
      "title": "An older capital save can leave a reserve rider\u2019s shortcut outside capture and cleanup",
      "trigger": "Load an older capital save whose reserve rider still carries a native Mount / Dismount slot but has no owned toolbar row, then save again or disable the mod before that rider rejoins the party."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "The exact native-invalidation regression now passes for both fact-reactivation orders, and its same-ID control passes. The permanent-loss issue remains in area-boundary observation and in-world replay, where the previous pet\u2019s chosen index can still reach its replacement."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F1",
      "rationale": "The exact slot invalidation now updates the owning load scope\u2019s outgoing record. Abandonment restores that record when the original player survives, and world replacement discards it. The gate covers reactivated and inactive facts, one restored binding and archive row at 9, deliberate removal, and successful same-ID world replacement."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r07-5b5f71",
  "round": 7,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate preserves many toolbar moves and removals across temporary absence, projects eligible owners\u2019 persistent pets into the capital roster, and admits a mounted player\u2019s explicit attack on an already-unconscious enemy. The two city symptoms share a roster transition but have distinct causes.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, verify the chosen shortcut across real capital and interior visits, forced dismount, and saves inside and outside the city.",
      "Select a reserve owner\u2019s pet through Inventory and Character, verify its identity and equipment, change an item, then reload and repeat.",
      "Use matched longbow shots against standing, consciously prone and unconscious enemies at near and normal distances in RT and TB. Check actual rolls, movement, modifiers and action costs using the handoff\u2019s negative controls."
    ],
    "scope_reviewed": "Complete final development candidate for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through d27728b3467aba07850dbc66eaeb30f9e1eb5768.",
    "unresolved_questions": [
      "The original report does not identify the weapon or distinguish a conscious tripped target from an unconscious one. The source trace found a mounted admission difference for the latter."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
