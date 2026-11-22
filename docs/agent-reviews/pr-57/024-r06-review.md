<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r06-review part=1/1 sha256=441564b916f19ffd9a70f85fad339c941d799c20fb9cb05f0a36aaa1ddbf0f16 -->
### Codex GPT-6.2 Astra — review — round 6 — candidate `85d70ff2db`

**Decision:** `changes_required` — The focused gate passed on the requested commit. Two toolbar defects still block readiness. Native slot cleanup can erase the identity needed to retire a replaced pet’s shortcut. It can also erase a valid shortcut when a load is abandoned and play resumes in the original world.

#### Findings

**R4-F2** · P2 · defect · blocks readiness — Native slot cleanup can erase the previous pet’s identity before replacement
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1959`; acceptance: §4 genuine companion removal, §4 temporary absence versus permanent loss
- Trigger: A dismounted rider has pet A’s control in slot 9. The pet link clears, native cleanup reports the slot as bad, and pet B becomes the linked companion before the next control update.
- Expected: Positive evidence that the original companion was removed retires its slot. Temporary non-resolution with the same persistent pet ID preserves it.
- Actual: RetireOnPetChange skips an empty current link. ObserveNativeSlotInvalidated then calls ObserveBound with that empty link, overwriting A’s recorded identity. For a null link, PetLinkChanged subsequently has no previous ID to compare with B. The interrupted slot is still 9, so reconciliation can bind B’s control there. The cleared-link regression runs a reconciliation before assigning B, which hides this ordering.
- Why it blocks / does not: A replacement companion can inherit a toolbar arrangement that should have been retired with its predecessor.
- Requested correction: Handle the cleared pet link before the invalidation observer overwrites the last known companion identity. Preserve the existing same-ID temporary-absence behavior.
- Requested evidence: Starting with pet A and slot 9, clear the link, report native invalidation of slot 9, assign B, then reconcile for the first time. Verify B does not receive slot 9 and the next archive has no kind-6 row for it. Keep the same-ID restoration control.

**R5-F1** · P2 · defect · blocks readiness — An aborted load loses a shortcut cleaned by native UI while the load was active
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1949`; acceptance: §4 save/load and placement intent, §7 affected lifecycle paths
- Trigger: Native bad-slot cleanup empties a live rider’s chosen slot after BeginSelectedLoad, and the load aborts while the original player is still current.
- Expected: If the same player world survives, a slot emptied by native cleanup returns to its chosen index. If a new world replaces it, that world receives only its own arrangement.
- Actual: ObserveInvalidatedPublicSlot returns while the outgoing world is fenced. If native cleanup empties slot 9 during that interval, Abandon restores the pre-load entry with Interrupted still false. With the fact active again and the slot empty, ResolveUnbound records OffBar. The next save can persist that loss as a placement marker alone. LoadFenceProductionOrder already models this invalidation but follows it with a successful world replacement. AbandonedLoadRestores omits the invalidation.
- Why it blocks / does not: An unsuccessful load can still remove a shortcut from the world the player continues using.
- Requested correction: Retain the exact outgoing slot invalidation with the owning load scope so abandonment can restore the latest arrangement. Discard that observation on successful world replacement.
- Requested evidence: Combine the existing load cases. Begin a load with slot 9 bound, invalidate it and reactivate the fact while fenced, then abandon before replacing the player. Verify one binding at 9 and a kind-6 row at 9 on the next save. Retain the successful-load same-ID isolation case and the deliberate-removal control.

#### Prior finding assessments

- **R3-F2** — `fix_verified` — The owner procedure now counts mounted and dismounted rolls over the same bounded interval after the first roll. It defines a single-attack fixture, a matched multi-attack fallback and a conditional ammunition check.
- **R4-F1** — `fix_verified` — ReconcileFacts samples the bar before replacing an inactive desired fact. The candidate regression covers deliberate removal followed by deactivation, with a native-cleanup control that restores slot 9 once.
- **R4-F2** — `fix_rejected` — Update-first and save-first direct A-to-B changes now retire the old slot. A native invalidation observed while the link is null can still overwrite A’s identity before B is compared.
- **R5-F1** — `fix_rejected` — The load scope now restores reserve-slot and off-bar records captured at entry. It still loses a native invalidation that happens after entry when the outgoing world survives.
- **R5-F2** — `fix_verified` — The superseded iterator’s end callback abandons its own fence. The owner comparison prevents an older iterator from releasing a newer fence, and an unstarted replacement acquires none. Both orderings are covered.

#### Evidence assessment

- **Inspected:** Controller mission, full feature diff, changed-file list, prior findings, developer report, CLI transcript and candidate gate evidence; Repository instructions, validation policy, current handoffs and 0.2.13 click-compatibility guidance; Toolbar observation, fact reconciliation, archive capture and restore, load-scope ownership, city roster projection, attack admission and command execution paths, with their focused tests
- **Executed verification:** The controller transcript confirms a guarded push of 85d70ff2 and a focused Fast gate on that same commit. The gate exited 0 in 29.6 seconds and left the worktree unchanged.; The candidate gate log records 1,447 component checks, 153 source checks, 820 Kingmaker assembly-contract checks, 21 click-composition checks covering both patch orders, three archive-bound checks and 22 six-pair archive-adapter checks, all passing.; The composition check identifies the tested product DLL by SHA-256 caf8cbed986fd6468b93bff95781fd789fba4b6e801717ff65e82dd61cb78acb and MVID b6248d2c-febc-4b0b-a145-a65b6d59f4aa.; The area-restore adapter compile comparison found the same 63 diagnostics on base and candidate. Its behavioral assertions did not execute on either tree.
- **Source inspection only:** The two blocking sequences follow from the production callback and reconciliation order. The gate exercises neighboring sequences separately.; The capital projection uses the owner’s existing pet instance. The mounted attack change carries an explicit order on an already-unconscious enemy through admission and target validation. Actual panel interaction and attack rolls remain owner checks.
- **Required gaps:** Preserve companion identity through native slot invalidation when a cleared pet link is followed by a replacement before reconciliation.; Preserve native slot invalidations from the outgoing world when a selected load aborts before player replacement.
- **Limitations:** The review used the read-only snapshot and recorded gate evidence. No reviewer build or game run was performed.; Native capital UI, equipment changes, toolbar transitions and attack resolution remain NOT RUN on this candidate. The handoff identifies the package, driver and reader prerequisites and supplies owner playtest steps. No runtime transaction required restoration.

#### Summary

- **Delivered behavior:** The candidate preserves toolbar intent across the covered city, reserve and save paths, projects eligible owners’ existing pets into the capital roster, and admits explicit mounted attack orders against already-unconscious living enemies. The two city reports share a roster-transition trigger but have different causes.
- **Scope reviewed:** Complete mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 85d70ff2db5e7562027c66c00065ba47506d3328.
- **Pending later-phase work:** none
- **Unresolved questions:** The owner’s original weapon, the target’s consciousness and the precise city transition that first removed the shortcut remain unidentified.
- **Remaining owner review:** After the toolbar corrections, confirm shortcut placement through capital visits, interior transitions, forced dismount and saves inside and outside the city.; Confirm that a reserve owner’s exact pet opens in Inventory and Character, retains equipment changes after reload, and leaves selection usable when eligibility changes.; Compare mounted and dismounted longbow shots against standing, consciously prone and already-unconscious targets in real time and turn based. Observe actual rolls, costs, modifiers, range and line-of-sight controls.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1fec8-b14a-7fd2-8710-1adc8e18e037`, duration 413.6s, response sha256 `219b873b3b7c73e3d85aeb88415e1ebd85ed0bdfcbb40b9a3ae817611b8810e2`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r06-f6ed8d`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "85d70ff2db5e7562027c66c00065ba47506d3328",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit. Two toolbar defects still block readiness. Native slot cleanup can erase the identity needed to retire a replaced pet\u2019s shortcut. It can also erase a valid shortcut when a load is abandoned and play resumes in the original world.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller transcript confirms a guarded push of 85d70ff2 and a focused Fast gate on that same commit. The gate exited 0 in 29.6 seconds and left the worktree unchanged.",
      "The candidate gate log records 1,447 component checks, 153 source checks, 820 Kingmaker assembly-contract checks, 21 click-composition checks covering both patch orders, three archive-bound checks and 22 six-pair archive-adapter checks, all passing.",
      "The composition check identifies the tested product DLL by SHA-256 caf8cbed986fd6468b93bff95781fd789fba4b6e801717ff65e82dd61cb78acb and MVID b6248d2c-febc-4b0b-a145-a65b6d59f4aa.",
      "The area-restore adapter compile comparison found the same 63 diagnostics on base and candidate. Its behavioral assertions did not execute on either tree."
    ],
    "inspected": [
      "Controller mission, full feature diff, changed-file list, prior findings, developer report, CLI transcript and candidate gate evidence",
      "Repository instructions, validation policy, current handoffs and 0.2.13 click-compatibility guidance",
      "Toolbar observation, fact reconciliation, archive capture and restore, load-scope ownership, city roster projection, attack admission and command execution paths, with their focused tests"
    ],
    "limitations": [
      "The review used the read-only snapshot and recorded gate evidence. No reviewer build or game run was performed.",
      "Native capital UI, equipment changes, toolbar transitions and attack resolution remain NOT RUN on this candidate. The handoff identifies the package, driver and reader prerequisites and supplies owner playtest steps. No runtime transaction required restoration."
    ],
    "required_gaps": [
      "Preserve companion identity through native slot invalidation when a cleared pet link is followed by a replacement before reconciliation.",
      "Preserve native slot invalidations from the outgoing world when a selected load aborts before player replacement."
    ],
    "source_inspection_only": [
      "The two blocking sequences follow from the production callback and reconciliation order. The gate exercises neighboring sequences separately.",
      "The capital projection uses the owner\u2019s existing pet instance. The mounted attack change carries an explicit order on an already-unconscious enemy through admission and target validation. Actual panel interaction and attack rolls remain owner checks."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 temporary absence versus permanent loss"
      ],
      "actual": "RetireOnPetChange skips an empty current link. ObserveNativeSlotInvalidated then calls ObserveBound with that empty link, overwriting A\u2019s recorded identity. For a null link, PetLinkChanged subsequently has no previous ID to compare with B. The interrupted slot is still 9, so reconciliation can bind B\u2019s control there. The cleared-link regression runs a reconciliation before assigning B, which hides this ordering.",
      "blocking_rationale": "A replacement companion can inherit a toolbar arrangement that should have been retired with its predecessor.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Positive evidence that the original companion was removed retires its slot. Temporary non-resolution with the same persistent pet ID preserves it.",
      "id": "R4-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1959
      },
      "priority": "P2",
      "requested_correction": "Handle the cleared pet link before the invalidation observer overwrites the last known companion identity. Preserve the existing same-ID temporary-absence behavior.",
      "requested_evidence": "Starting with pet A and slot 9, clear the link, report native invalidation of slot 9, assign B, then reconcile for the first time. Verify B does not receive slot 9 and the next archive has no kind-6 row for it. Keep the same-ID restoration control.",
      "title": "Native slot cleanup can erase the previous pet\u2019s identity before replacement",
      "trigger": "A dismounted rider has pet A\u2019s control in slot 9. The pet link clears, native cleanup reports the slot as bad, and pet B becomes the linked companion before the next control update."
    },
    {
      "acceptance_ids": [
        "\u00a74 save/load and placement intent",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "ObserveInvalidatedPublicSlot returns while the outgoing world is fenced. If native cleanup empties slot 9 during that interval, Abandon restores the pre-load entry with Interrupted still false. With the fact active again and the slot empty, ResolveUnbound records OffBar. The next save can persist that loss as a placement marker alone. LoadFenceProductionOrder already models this invalidation but follows it with a successful world replacement. AbandonedLoadRestores omits the invalidation.",
      "blocking_rationale": "An unsuccessful load can still remove a shortcut from the world the player continues using.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "If the same player world survives, a slot emptied by native cleanup returns to its chosen index. If a new world replaces it, that world receives only its own arrangement.",
      "id": "R5-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1949
      },
      "priority": "P2",
      "requested_correction": "Retain the exact outgoing slot invalidation with the owning load scope so abandonment can restore the latest arrangement. Discard that observation on successful world replacement.",
      "requested_evidence": "Combine the existing load cases. Begin a load with slot 9 bound, invalidate it and reactivate the fact while fenced, then abandon before replacing the player. Verify one binding at 9 and a kind-6 row at 9 on the next save. Retain the successful-load same-ID isolation case and the deliberate-removal control.",
      "title": "An aborted load loses a shortcut cleaned by native UI while the load was active",
      "trigger": "Native bad-slot cleanup empties a live rider\u2019s chosen slot after BeginSelectedLoad, and the load aborts while the original player is still current."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R3-F2",
      "rationale": "The owner procedure now counts mounted and dismounted rolls over the same bounded interval after the first roll. It defines a single-attack fixture, a matched multi-attack fallback and a conditional ammunition check."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R4-F1",
      "rationale": "ReconcileFacts samples the bar before replacing an inactive desired fact. The candidate regression covers deliberate removal followed by deactivation, with a native-cleanup control that restores slot 9 once."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R4-F2",
      "rationale": "Update-first and save-first direct A-to-B changes now retire the old slot. A native invalidation observed while the link is null can still overwrite A\u2019s identity before B is compared."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R5-F1",
      "rationale": "The load scope now restores reserve-slot and off-bar records captured at entry. It still loses a native invalidation that happens after entry when the outgoing world survives."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R5-F2",
      "rationale": "The superseded iterator\u2019s end callback abandons its own fence. The owner comparison prevents an older iterator from releasing a newer fence, and an unstarted replacement acquires none. Both orderings are covered."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r06-f6ed8d",
  "round": 6,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate preserves toolbar intent across the covered city, reserve and save paths, projects eligible owners\u2019 existing pets into the capital roster, and admits explicit mounted attack orders against already-unconscious living enemies. The two city reports share a roster-transition trigger but have different causes.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After the toolbar corrections, confirm shortcut placement through capital visits, interior transitions, forced dismount and saves inside and outside the city.",
      "Confirm that a reserve owner\u2019s exact pet opens in Inventory and Character, retains equipment changes after reload, and leaves selection usable when eligibility changes.",
      "Compare mounted and dismounted longbow shots against standing, consciously prone and already-unconscious targets in real time and turn based. Observe actual rolls, costs, modifiers, range and line-of-sight controls."
    ],
    "scope_reviewed": "Complete mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 85d70ff2db5e7562027c66c00065ba47506d3328.",
    "unresolved_questions": [
      "The owner\u2019s original weapon, the target\u2019s consciousness and the precise city transition that first removed the shortcut remain unidentified."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
