<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r15-review part=1/1 sha256=c75eef9055af9b300d5aadb075d32627c5832f4e1e2dc381e4dad92f2a3d10b5 -->
### Codex GPT-6.2 Astra — review — round 15 — candidate `83c2aaada8`

**Decision:** `changes_required` — The focused gate passed on the exact candidate. Three toolbar paths still violate the mission. A save between two native slot reads can replace the chosen canonical slot with an older alias, bulk cleanup can select the last of two canonical bindings, and saving while KMC is disabled can carry placement intent into another loaded world. These are source-level defects that can be checked offline.

#### Findings

**R14-F2** · P2 · defect · blocks readiness — A save between native slot reads can still replace the canonical choice with an older alias
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2293`; acceptance: §4 chosen toolbar slot, §4 existing save compatibility, §4 save/load
- Trigger: An older bar has a stale canonical binding at 4 and a stale Mount alias at 9. Native GetSlot empties 4, and a save starts before native reads 9.
- Expected: The save carries the canonical choice at 4, and the same pet's return restores one shortcut there.
- Actual: GetSlot(4) records interrupted intent at 4. At the save header, RecoverLegacyAlias preserves that entry, but ObserveArrangementOnBar immediately scans again. PublicBindingIndex now finds only the alias at 9, and ObserveAtBoundary calls ObserveBound at 9. With both facts unavailable, capture marks 9 interrupted and writes it as the kind-6 row. The new guard in ObserveNativeAliasInvalidated is never reached in this order.
- Why it blocks / does not: The slot restored from the same older bar still depends on whether native UI reads one slot or cleans the whole bar before saving.
- Requested correction: Preserve the observed canonical choice when classifying a surviving secondary alias at the save boundary. Keep recovery of a genuine alias-only bar and recognition of player moves.
- Requested evidence: Use the existing mixed-bar fixture with the pet temporarily unresolved. Deliver GetSlot cleanup for 4 only, leave the stale alias at 9, then save. Require one kind-6 row at 4 and one restored shortcut there after reload and pet return. Retain the full-pass and alias-only controls.

**R15-F1** · P2 · defect · blocks readiness — Bulk cleanup selects the last of two canonical shortcuts
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:108`; acceptance: §4 chosen toolbar slot, §4 temporary unavailability, §4 save/load
- Trigger: The bar contains canonical bindings at 3 and 9, as in StaleThenCurrentCanonical. The same pet becomes temporarily unavailable and CleanupBadSlots empties both before capture.
- Expected: Native cleanup preserves the first canonical index selected by the existing repair and archive policy.
- Actual: Each canonical emptying calls ObserveNativeSlotInvalidated with its own index. The first call records 3 and the second calls ObserveBound at 9, replacing 3. Once both slots are empty, capture writes 9. Saving the bar before cleanup selects 3 under RecoveredPublicSlot.
- Why it blocks / does not: A toolbar shape already covered by the candidate's compatibility tests restores at different indices depending on cleanup timing.
- Requested correction: Resolve cleanup reports against the bar's selected public binding so a secondary canonical report cannot replace the first choice. Preserve recovery of a single manually moved shortcut.
- Requested evidence: Extend the existing two-canonical fixture at 3 and 9. Make the fact unavailable, empty both slots in one cleanup pass, save and reload. Require one public row and one restored shortcut at 3, matching capture before cleanup.

**R15-F2** · P2 · defect · blocks readiness — Disabled save sanitation can carry a shortcut into another world
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1249`; acceptance: §4 mod disable/removal, §4 world disposal, §7 save/lifecycle paths
- Trigger: While KMC is disabled, an older save supplies a surviving canonical or legacy shortcut. The player saves before native toolbar cleanup, then loads another save while KMC is still disabled.
- Expected: Disabled saves sanitize surviving KMC bindings without acquiring recoverable placement state. A replacement world starts with its own arrangement.
- Actual: BeginSaveSerializationScope runs ObserveArrangementsAtBoundary while disabled. A surviving public slot on a candidate rider adds a placement record and intent, and RemoveManagedFacts marks the intent interrupted. On the next disabled load, MountedControlLoadFence.Begin calls Release and returns without clearing those live collections. Re-enabling can therefore restore the previous world's slot for the same rider and pet even with autofill off.
- Why it blocks / does not: A save made with KMC disabled can determine the toolbar arrangement in a different save.
- Requested correction: Keep disabled save sanitation active while preventing it from recording toolbar intent. Ensure a disabled selected load clears any live placement records from the outgoing world.
- Requested evidence: While disabled, save an older world with a surviving shortcut at 9 on a party rider before UI cleanup. Verify sanitation removes the binding and leaves both placement collections empty. Load a world with the same rider and pet IDs and no shortcut rows, then enable with autofill off and verify slot 9 stays empty.

#### Prior finding assessments

- **R14-F1** — `fix_verified` — Successful canonical emptying now marks placement in the live or fenced outgoing set. FirstManualCleanupNoAutofill covers an occupied slot, another empty main-bar slot, save and reload.
- **R14-F2** — `fix_rejected` — MixedCleanupPassKeepsCanonical covers both slots being emptied in one pass. A save after GetSlot empties only the canonical slot still lets the secondary alias overwrite the same choice through ObserveAtBoundary.
- **R14-F3** — `fix_verified` — CaptureAbsentIntent applies the same occupancy decision to the archive and the continuing arrangement. OccupiedSlotGivenUpEverywhere checks both paths after the occupant is cleared.
- **R14-F4** — `fix_verified` — A disabled load no longer arms a fence or retains the outgoing Player, and both native cleanup entry points check the enabled state. DisabledLifecycle exercises the fence lifetime and cleanup cases. R15-F2 concerns a separate writer during save sanitation.
- **R14-F5** — `fix_verified` — A fenced native rebind now records live intent and placement in the outgoing scope. FencedRebindAbandoned covers removal after abandonment and isolation after completed replacement.

#### Evidence assessment

- **Inspected:** The mission, repository validation policy, current handoff and resume instructions, feature diff, developer transcript and five pending findings.; Native toolbar observation, placement intent, reserve migration, save leases, archive capture and restoration, selected-load fencing and disable handling.; City roster projection and its native adapter, stock-click provenance, downed-target admission, pair-command execution gates, focused wrapper and regression fixtures.
- **Executed verification:** Reviewer verified the detached HEAD and mission SHA-256 against the controller inputs. The feature diff passed git diff --check.; The developer tool transcript confirms the guarded push and focused gate for 83c2aaada817ee2880143fa2b6a4f1a07c372b34. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 56.4 seconds with the worktree unchanged.; The separate pre-commit development run printed 1477 component passes and zero failures. The combined mutation run printed 1472 passes and the five named regression failures.
- **Source inspection only:** The three reported failure sequences were traced through the production methods. They were not executed by the reviewer.; The city changes project existing pet instances into the capital roster. The combat changes carry an explicit order on an already-unconscious living target through mounted admission. Native panel selection, equipment interaction and attack rolls remain owner observations.
- **Required gaps:** Cover a save between canonical GetSlot cleanup and cleanup of a secondary alias.; Cover bulk cleanup of two canonical bindings using the same first-slot precedence as archive capture.; Cover disabled save sanitation followed by another load and re-enable with matching actor and pet identities.
- **Limitations:** The reviewer ran read-only checks. The toolbar regressions use production domain decisions with a double for native bar operations and portions of the service.; The area-restore adapter has the same 63 compiler diagnostics at base and candidate according to the handoff. The comparison does not exercise area restoration.; Native gameplay was NOT RUN. The handoff documents the unavailable candidate packaging path, fixture and reader prerequisites, and owner playtest steps. No runtime restoration was needed.

#### Summary

- **Delivered behavior:** The candidate implements toolbar placement recovery, capital roster projection for eligible owners' existing pets, and mounted admission of an explicit attack on an already-unconscious living enemy. Four prior fixes are verified. Toolbar precedence and disabled-save isolation still need correction.
- **Scope reviewed:** Complete mounted-combat-city-and-prone mission for draft PR #57 at 83c2aaada817ee2880143fa2b6a4f1a07c372b34, including all five pending round-14 findings.
- **Pending later-phase work:** none
- **Unresolved questions:** The owner's original weapon and the target's consciousness at the failed click remain unknown. The conscious-prone path has a source trace only.
- **Remaining owner review:** Confirm the nondefault shortcut through capital visits, interiors and cold loads on the owner's configuration.; Select a reserve owner's pet in Inventory and Character, inspect and change equipment, and verify selection after reopening and loading.; Observe actual ranged attack resolution against standing, conscious-prone and unconscious enemies in real time and turn based, including the documented range, line-of-sight, penalty and action-cost controls.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff4b-a83a-7281-b242-878d783c6c9a`, duration 341.6s, response sha256 `da3d076ff2e9a5ffa1d801d2a1ff2b618f3bc56dcac3a875aba4a5217b66d0ec`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r15-5ea03a`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "83c2aaada817ee2880143fa2b6a4f1a07c372b34",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the exact candidate. Three toolbar paths still violate the mission. A save between two native slot reads can replace the chosen canonical slot with an older alias, bulk cleanup can select the last of two canonical bindings, and saving while KMC is disabled can carry placement intent into another loaded world. These are source-level defects that can be checked offline.",
  "evidence_assessment": {
    "executed_verification": [
      "Reviewer verified the detached HEAD and mission SHA-256 against the controller inputs. The feature diff passed git diff --check.",
      "The developer tool transcript confirms the guarded push and focused gate for 83c2aaada817ee2880143fa2b6a4f1a07c372b34. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 56.4 seconds with the worktree unchanged.",
      "The separate pre-commit development run printed 1477 component passes and zero failures. The combined mutation run printed 1472 passes and the five named regression failures."
    ],
    "inspected": [
      "The mission, repository validation policy, current handoff and resume instructions, feature diff, developer transcript and five pending findings.",
      "Native toolbar observation, placement intent, reserve migration, save leases, archive capture and restoration, selected-load fencing and disable handling.",
      "City roster projection and its native adapter, stock-click provenance, downed-target admission, pair-command execution gates, focused wrapper and regression fixtures."
    ],
    "limitations": [
      "The reviewer ran read-only checks. The toolbar regressions use production domain decisions with a double for native bar operations and portions of the service.",
      "The area-restore adapter has the same 63 compiler diagnostics at base and candidate according to the handoff. The comparison does not exercise area restoration.",
      "Native gameplay was NOT RUN. The handoff documents the unavailable candidate packaging path, fixture and reader prerequisites, and owner playtest steps. No runtime restoration was needed."
    ],
    "required_gaps": [
      "Cover a save between canonical GetSlot cleanup and cleanup of a secondary alias.",
      "Cover bulk cleanup of two canonical bindings using the same first-slot precedence as archive capture.",
      "Cover disabled save sanitation followed by another load and re-enable with matching actor and pet identities."
    ],
    "source_inspection_only": [
      "The three reported failure sequences were traced through the production methods. They were not executed by the reviewer.",
      "The city changes project existing pet instances into the capital roster. The combat changes carry an explicit order on an already-unconscious living target through mounted admission. Native panel selection, equipment interaction and attack rolls remain owner observations."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 existing save compatibility",
        "\u00a74 save/load"
      ],
      "actual": "GetSlot(4) records interrupted intent at 4. At the save header, RecoverLegacyAlias preserves that entry, but ObserveArrangementOnBar immediately scans again. PublicBindingIndex now finds only the alias at 9, and ObserveAtBoundary calls ObserveBound at 9. With both facts unavailable, capture marks 9 interrupted and writes it as the kind-6 row. The new guard in ObserveNativeAliasInvalidated is never reached in this order.",
      "blocking_rationale": "The slot restored from the same older bar still depends on whether native UI reads one slot or cleans the whole bar before saving.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The save carries the canonical choice at 4, and the same pet's return restores one shortcut there.",
      "id": "R14-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2293
      },
      "priority": "P2",
      "requested_correction": "Preserve the observed canonical choice when classifying a surviving secondary alias at the save boundary. Keep recovery of a genuine alias-only bar and recognition of player moves.",
      "requested_evidence": "Use the existing mixed-bar fixture with the pet temporarily unresolved. Deliver GetSlot cleanup for 4 only, leave the stale alias at 9, then save. Require one kind-6 row at 4 and one restored shortcut there after reload and pet return. Retain the full-pass and alias-only controls.",
      "title": "A save between native slot reads can still replace the canonical choice with an older alias",
      "trigger": "An older bar has a stale canonical binding at 4 and a stale Mount alias at 9. Native GetSlot empties 4, and a save starts before native reads 9."
    },
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 temporary unavailability",
        "\u00a74 save/load"
      ],
      "actual": "Each canonical emptying calls ObserveNativeSlotInvalidated with its own index. The first call records 3 and the second calls ObserveBound at 9, replacing 3. Once both slots are empty, capture writes 9. Saving the bar before cleanup selects 3 under RecoveredPublicSlot.",
      "blocking_rationale": "A toolbar shape already covered by the candidate's compatibility tests restores at different indices depending on cleanup timing.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Native cleanup preserves the first canonical index selected by the existing repair and archive policy.",
      "id": "R15-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 108
      },
      "priority": "P2",
      "requested_correction": "Resolve cleanup reports against the bar's selected public binding so a secondary canonical report cannot replace the first choice. Preserve recovery of a single manually moved shortcut.",
      "requested_evidence": "Extend the existing two-canonical fixture at 3 and 9. Make the fact unavailable, empty both slots in one cleanup pass, save and reload. Require one public row and one restored shortcut at 3, matching capture before cleanup.",
      "title": "Bulk cleanup selects the last of two canonical shortcuts",
      "trigger": "The bar contains canonical bindings at 3 and 9, as in StaleThenCurrentCanonical. The same pet becomes temporarily unavailable and CleanupBadSlots empties both before capture."
    },
    {
      "acceptance_ids": [
        "\u00a74 mod disable/removal",
        "\u00a74 world disposal",
        "\u00a77 save/lifecycle paths"
      ],
      "actual": "BeginSaveSerializationScope runs ObserveArrangementsAtBoundary while disabled. A surviving public slot on a candidate rider adds a placement record and intent, and RemoveManagedFacts marks the intent interrupted. On the next disabled load, MountedControlLoadFence.Begin calls Release and returns without clearing those live collections. Re-enabling can therefore restore the previous world's slot for the same rider and pet even with autofill off.",
      "blocking_rationale": "A save made with KMC disabled can determine the toolbar arrangement in a different save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Disabled saves sanitize surviving KMC bindings without acquiring recoverable placement state. A replacement world starts with its own arrangement.",
      "id": "R15-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1249
      },
      "priority": "P2",
      "requested_correction": "Keep disabled save sanitation active while preventing it from recording toolbar intent. Ensure a disabled selected load clears any live placement records from the outgoing world.",
      "requested_evidence": "While disabled, save an older world with a surviving shortcut at 9 on a party rider before UI cleanup. Verify sanitation removes the binding and leaves both placement collections empty. Load a world with the same rider and pet IDs and no shortcut rows, then enable with autofill off and verify slot 9 stays empty.",
      "title": "Disabled save sanitation can carry a shortcut into another world",
      "trigger": "While KMC is disabled, an older save supplies a surviving canonical or legacy shortcut. The player saves before native toolbar cleanup, then loads another save while KMC is still disabled."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F1",
      "rationale": "Successful canonical emptying now marks placement in the live or fenced outgoing set. FirstManualCleanupNoAutofill covers an occupied slot, another empty main-bar slot, save and reload."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R14-F2",
      "rationale": "MixedCleanupPassKeepsCanonical covers both slots being emptied in one pass. A save after GetSlot empties only the canonical slot still lets the secondary alias overwrite the same choice through ObserveAtBoundary."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F3",
      "rationale": "CaptureAbsentIntent applies the same occupancy decision to the archive and the continuing arrangement. OccupiedSlotGivenUpEverywhere checks both paths after the occupant is cleared."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F4",
      "rationale": "A disabled load no longer arms a fence or retains the outgoing Player, and both native cleanup entry points check the enabled state. DisabledLifecycle exercises the fence lifetime and cleanup cases. R15-F2 concerns a separate writer during save sanitation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R14-F5",
      "rationale": "A fenced native rebind now records live intent and placement in the outgoing scope. FencedRebindAbandoned covers removal after abandonment and isolation after completed replacement."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r15-5ea03a",
  "round": 15,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate implements toolbar placement recovery, capital roster projection for eligible owners' existing pets, and mounted admission of an explicit attack on an already-unconscious living enemy. Four prior fixes are verified. Toolbar precedence and disabled-save isolation still need correction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Confirm the nondefault shortcut through capital visits, interiors and cold loads on the owner's configuration.",
      "Select a reserve owner's pet in Inventory and Character, inspect and change equipment, and verify selection after reopening and loading.",
      "Observe actual ranged attack resolution against standing, conscious-prone and unconscious enemies in real time and turn based, including the documented range, line-of-sight, penalty and action-cost controls."
    ],
    "scope_reviewed": "Complete mounted-combat-city-and-prone mission for draft PR #57 at 83c2aaada817ee2880143fa2b6a4f1a07c372b34, including all five pending round-14 findings.",
    "unresolved_questions": [
      "The owner's original weapon and the target's consciousness at the failed click remain unknown. The conscious-prone path has a source trace only."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
