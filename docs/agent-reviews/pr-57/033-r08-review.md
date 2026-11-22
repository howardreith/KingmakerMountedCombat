<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r08-review part=1/1 sha256=bf4fe3cebf272ed4e3b946341175ce62c9243262d7642f999a4b068d4508b5bc -->
### Codex GPT-6.2 Astra — review — round 8 — candidate `9cec5c5db9`

**Decision:** `changes_required` — The focused gate passed on the exact candidate, and both pending reproductions are addressed. Two toolbar defects still block readiness. Recovering an older reserve bar can change the chosen Mount / Dismount slot or erase a Charge shortcut. A faulted rider can also lose the chosen slot while still entitled to the Dismount control.

#### Findings

**R8-F1** · P2 · defect · blocks readiness — Older reserve bars bypass the existing shortcut migration rules
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2018`; acceptance: §4 chosen toolbar slot, §4 unrelated slots remain unchanged, §4 existing save compatibility
- Trigger: The first update or save scans an older capital save whose reserve rider still has legacy KMC bindings on the native bar.
- Expected: As in ReconcilePublicControlSlots, an existing public Mount / Dismount binding wins over a legacy Mount or Dismount alias. A legacy Mounted Charge becomes the rider's stock Charge at the same index when stock Charge is available and absent elsewhere on the bar.
- Actual: AdmitReserveBindings takes the first index matching either a legacy alias or the public control. A legacy Mount at 3 therefore wins over the player's public control at 9. It then calls ClearManagedHotbarSlots, which also erases a legacy Mounted Charge at 5 without applying the stock Charge migration. An immediate city resave makes both changes persistent.
- Why it blocks / does not: The newly added recovery path can permanently move the reported shortcut and delete another recoverable shortcut in a supported older save.
- Requested correction: Apply the existing migration precedence before clearing reserve bindings. Recover the canonical public slot first, fall back to an exact legacy Mount or Dismount when needed, and preserve the existing stock Charge migration and duplicate rule.
- Requested evidence: Start an unobserved reserve rider with legacy Mount at 3, legacy Mounted Charge at 5, public Mount / Dismount at 9, an available stock Charge fact and no owned rows. Save immediately and reload. Verify one kind-6 row at 9, stock Charge at 5, no native KMC binding during serialization, and one public control at 9 on return. Repeat with stock Charge already bound elsewhere and verify it stays there once.

**R8-F2** · P2 · defect · blocks readiness — A faulted rider loses an interrupted shortcut while Dismount is still available
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:193`; acceptance: §4 temporary mount unavailability, §4 forced dismount and chosen-slot persistence, §7 affected lifecycle paths
- Trigger: Native bad-slot cleanup empties the chosen slot while the rider's pet link is empty and NativeMountedControlPolicy.ShouldLease still grants MountState through the mounted or faulted relationship.
- Expected: An interrupted slot returns to its chosen empty index while the relationship still grants the public control. A confirmed companion replacement, or a cleared link after entitlement ends, still retires it.
- Actual: The invalidation observer preserves the interrupted index because ShouldLease is true. On the next update, Reconcile reaches the live, unbound branch and calls Revalidate unconditionally. Revalidate retires the entry for an empty pet ID. ResolveUnbound then records an already-placed rider as off-bar, losing the index.
- Why it blocks / does not: Native cleanup can remove the player's Dismount shortcut precisely while the relationship still provides it as an escape from a faulted state.
- Requested correction: Use the same entitlement-aware companion-loss decision in live unbound reconciliation as in the invalidation observer. Preserve the known companion identity and interrupted index while the relationship grants the control.
- Requested evidence: Record slot 9 for pet A, clear the pet link while the mounted or faulted relationship still grants Dismount, then invalidate the bound slot and reconcile the live control. Verify exactly one binding and one saved kind-6 row at 9. Keep the dismounted cleared-link and different-pet controls passing.

#### Prior finding assessments

- **R4-F2** — `fix_verified` — Boundary observation now retires a dismounted rider's arrangement before a cleared link can overwrite its known companion identity. In-world seeding and slot replay honor that retirement. The gate covers replacement before resume, both replay orderings, and restoration for the unchanged pet ID.
- **R7-F1** — `fix_verified` — The reserve scan reaches a never-observed rider at the save header and before serialization. For the reported single binding at 9, the focused case confirms one kind-6 row, one placement record, zero serialized KMC slots, one restoration on return and disable cleanup. R8-F1 concerns how the new scan handles additional legacy bindings.

#### Evidence assessment

- **Inspected:** Controller mission, full feature diff, prior findings ledger, developer report, CLI transcript, focused receipt and gate log.; Repository validation policy, current handoff, F2 toolbar migration contract, 0.2.13 click compatibility notes, affected production paths and their focused tests.
- **Executed verification:** Verified the snapshot HEAD, mission hash and gate-log SHA-256 against the controller inputs. The developer CLI transcript records the guarded push and successful verification of 9cec5c5d.; The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 on H in 50.4 seconds, exit 0, with the worktree unchanged. Components passed 1452/1452, source checks 153/153, Kingmaker assembly contracts 820/820, click composition 21/21 across both patch orders, archive bounds 3/3 and the six-pair archive adapter 22/22.; The gate log identifies the tested DLL as SHA-256 a434fa0693d2c708083511c68f0e016617bd1b0b2471b75219fdcc86170807ef, MVID 7f4ce0c3-fbd2-4cbc-94d6-7204e96a20e8. It records passing component cases for cleared-link suspension, stale transfer replay and immediate resave of an older reserve binding.
- **Source inspection only:** Traced reserve recovery against ReconcilePublicControlSlots and the existing Charge migration rule. The mixed legacy bar in R8-F1 is absent from the reserve recovery regression.; Traced native invalidation through MountedControlPlacementIntent.Reconcile for a faulted rider. The toolbar double derives eligibility solely from a resolving pet, so it cannot exercise R8-F2.; Reviewed the capital roster projection and explicit downed-target order through admission and command validation. Actual character-sheet selection, equipment interaction and attack rolls remain owner observations.
- **Required gaps:** Preserve the established alias and Charge migration rules when recovering an older reserve bar.; Preserve an interrupted slot while the mounted or faulted relationship still grants the public control despite an empty pet link.; Cover both corrections in focused regression checks and bind the gate receipt to the corrected candidate.
- **Limitations:** The area-restore adapter produced the same 63 compile diagnostics on base and candidate. The comparison passed, but the adapter's behavioral assertion did not execute.; Native capital transitions, reserve-pet equipment interaction and attack resolution were not run on this DLL. The handoff identifies the guarded package, fixture and reader prerequisites and provides owner playtest steps. The mission permits this deferral.

#### Summary

- **Delivered behavior:** The candidate remembers per-rider toolbar intent, projects eligible owners' existing pets into the capital inventory and character roster, and carries an explicit attack order on an already-unconscious enemy through mounted command validation. The city issues share a roster transition as a trigger. Toolbar fact loss and the native UI pet filter are distinct causes.
- **Scope reviewed:** Complete final development mission for draft PR #57, branch codex/mounted-city-and-prone-fixes, from dbf63702 through 9cec5c5d.
- **Pending later-phase work:** none
- **Unresolved questions:** The owner's original report does not identify the weapon or establish whether the prone target was conscious. The handoff traces those states separately and uses a longbow for the deferred comparison.
- **Remaining owner review:** After correction, verify shortcut placement through real capital visits, forced dismounts and saves inside and outside the city. Include an older capital save if available.; Select a reserve owner's pet in Inventory and Character, verify its identity and equipment, then repeat after a transition and reload.; Compare mounted and dismounted longbow clicks against standing, consciously prone and already-unconscious enemies in real time and turn based. Check the actual rolls, range, line of sight and action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1fee6-8000-7d31-8777-f2266e082d09`, duration 317.9s, response sha256 `d382070e0bdf86a5b4024b1524b0ae7b7dafe265bb99e9adb183730a85a13df8`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r08-92237f`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "9cec5c5db9f6f2f30f067315ff24f6cdaafce326",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the exact candidate, and both pending reproductions are addressed. Two toolbar defects still block readiness. Recovering an older reserve bar can change the chosen Mount / Dismount slot or erase a Charge shortcut. A faulted rider can also lose the chosen slot while still entitled to the Dismount control.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD, mission hash and gate-log SHA-256 against the controller inputs. The developer CLI transcript records the guarded push and successful verification of 9cec5c5d.",
      "The controller ran Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 on H in 50.4 seconds, exit 0, with the worktree unchanged. Components passed 1452/1452, source checks 153/153, Kingmaker assembly contracts 820/820, click composition 21/21 across both patch orders, archive bounds 3/3 and the six-pair archive adapter 22/22.",
      "The gate log identifies the tested DLL as SHA-256 a434fa0693d2c708083511c68f0e016617bd1b0b2471b75219fdcc86170807ef, MVID 7f4ce0c3-fbd2-4cbc-94d6-7204e96a20e8. It records passing component cases for cleared-link suspension, stale transfer replay and immediate resave of an older reserve binding."
    ],
    "inspected": [
      "Controller mission, full feature diff, prior findings ledger, developer report, CLI transcript, focused receipt and gate log.",
      "Repository validation policy, current handoff, F2 toolbar migration contract, 0.2.13 click compatibility notes, affected production paths and their focused tests."
    ],
    "limitations": [
      "The area-restore adapter produced the same 63 compile diagnostics on base and candidate. The comparison passed, but the adapter's behavioral assertion did not execute.",
      "Native capital transitions, reserve-pet equipment interaction and attack resolution were not run on this DLL. The handoff identifies the guarded package, fixture and reader prerequisites and provides owner playtest steps. The mission permits this deferral."
    ],
    "required_gaps": [
      "Preserve the established alias and Charge migration rules when recovering an older reserve bar.",
      "Preserve an interrupted slot while the mounted or faulted relationship still grants the public control despite an empty pet link.",
      "Cover both corrections in focused regression checks and bind the gate receipt to the corrected candidate."
    ],
    "source_inspection_only": [
      "Traced reserve recovery against ReconcilePublicControlSlots and the existing Charge migration rule. The mixed legacy bar in R8-F1 is absent from the reserve recovery regression.",
      "Traced native invalidation through MountedControlPlacementIntent.Reconcile for a faulted rider. The toolbar double derives eligibility solely from a resolving pet, so it cannot exercise R8-F2.",
      "Reviewed the capital roster projection and explicit downed-target order through admission and command validation. Actual character-sheet selection, equipment interaction and attack rolls remain owner observations."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 unrelated slots remain unchanged",
        "\u00a74 existing save compatibility"
      ],
      "actual": "AdmitReserveBindings takes the first index matching either a legacy alias or the public control. A legacy Mount at 3 therefore wins over the player's public control at 9. It then calls ClearManagedHotbarSlots, which also erases a legacy Mounted Charge at 5 without applying the stock Charge migration. An immediate city resave makes both changes persistent.",
      "blocking_rationale": "The newly added recovery path can permanently move the reported shortcut and delete another recoverable shortcut in a supported older save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "As in ReconcilePublicControlSlots, an existing public Mount / Dismount binding wins over a legacy Mount or Dismount alias. A legacy Mounted Charge becomes the rider's stock Charge at the same index when stock Charge is available and absent elsewhere on the bar.",
      "id": "R8-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2018
      },
      "priority": "P2",
      "requested_correction": "Apply the existing migration precedence before clearing reserve bindings. Recover the canonical public slot first, fall back to an exact legacy Mount or Dismount when needed, and preserve the existing stock Charge migration and duplicate rule.",
      "requested_evidence": "Start an unobserved reserve rider with legacy Mount at 3, legacy Mounted Charge at 5, public Mount / Dismount at 9, an available stock Charge fact and no owned rows. Save immediately and reload. Verify one kind-6 row at 9, stock Charge at 5, no native KMC binding during serialization, and one public control at 9 on return. Repeat with stock Charge already bound elsewhere and verify it stays there once.",
      "title": "Older reserve bars bypass the existing shortcut migration rules",
      "trigger": "The first update or save scans an older capital save whose reserve rider still has legacy KMC bindings on the native bar."
    },
    {
      "acceptance_ids": [
        "\u00a74 temporary mount unavailability",
        "\u00a74 forced dismount and chosen-slot persistence",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "The invalidation observer preserves the interrupted index because ShouldLease is true. On the next update, Reconcile reaches the live, unbound branch and calls Revalidate unconditionally. Revalidate retires the entry for an empty pet ID. ResolveUnbound then records an already-placed rider as off-bar, losing the index.",
      "blocking_rationale": "Native cleanup can remove the player's Dismount shortcut precisely while the relationship still provides it as an escape from a faulted state.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An interrupted slot returns to its chosen empty index while the relationship still grants the public control. A confirmed companion replacement, or a cleared link after entitlement ends, still retires it.",
      "id": "R8-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 193
      },
      "priority": "P2",
      "requested_correction": "Use the same entitlement-aware companion-loss decision in live unbound reconciliation as in the invalidation observer. Preserve the known companion identity and interrupted index while the relationship grants the control.",
      "requested_evidence": "Record slot 9 for pet A, clear the pet link while the mounted or faulted relationship still grants Dismount, then invalidate the bound slot and reconcile the live control. Verify exactly one binding and one saved kind-6 row at 9. Keep the dismounted cleared-link and different-pet controls passing.",
      "title": "A faulted rider loses an interrupted shortcut while Dismount is still available",
      "trigger": "Native bad-slot cleanup empties the chosen slot while the rider's pet link is empty and NativeMountedControlPolicy.ShouldLease still grants MountState through the mounted or faulted relationship."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F2",
      "rationale": "Boundary observation now retires a dismounted rider's arrangement before a cleared link can overwrite its known companion identity. In-world seeding and slot replay honor that retirement. The gate covers replacement before resume, both replay orderings, and restoration for the unchanged pet ID."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R7-F1",
      "rationale": "The reserve scan reaches a never-observed rider at the save header and before serialization. For the reported single binding at 9, the focused case confirms one kind-6 row, one placement record, zero serialized KMC slots, one restoration on return and disable cleanup. R8-F1 concerns how the new scan handles additional legacy bindings."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r08-92237f",
  "round": 8,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate remembers per-rider toolbar intent, projects eligible owners' existing pets into the capital inventory and character roster, and carries an explicit attack order on an already-unconscious enemy through mounted command validation. The city issues share a roster transition as a trigger. Toolbar fact loss and the native UI pet filter are distinct causes.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, verify shortcut placement through real capital visits, forced dismounts and saves inside and outside the city. Include an older capital save if available.",
      "Select a reserve owner's pet in Inventory and Character, verify its identity and equipment, then repeat after a transition and reload.",
      "Compare mounted and dismounted longbow clicks against standing, consciously prone and already-unconscious enemies in real time and turn based. Check the actual rolls, range, line of sight and action expenditure."
    ],
    "scope_reviewed": "Complete final development mission for draft PR #57, branch codex/mounted-city-and-prone-fixes, from dbf63702 through 9cec5c5d.",
    "unresolved_questions": [
      "The owner's original report does not identify the weapon or establish whether the prone target was conscious. The handoff traces those states separately and uses a longbow for the deferred comparison."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
