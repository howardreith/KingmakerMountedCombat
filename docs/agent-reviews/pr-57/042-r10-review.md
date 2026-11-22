<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r10-review part=1/1 sha256=14e9fb2748ec99ed5b1b7fd62769392b01d137ba3104ac7ccb0f291ae9d0f462 -->
### Codex GPT-6.2 Astra — review — round 10 — candidate `c856b4e2cf`

**Decision:** `changes_required` — The focused gate passed on the reviewed commit. Three toolbar defects still block readiness. Saving before migration of a live Mount or Dismount alias can produce duplicate archive rows and fail the save. Native cleanup can erase an inactive alias before its chosen slot is recorded. A save-scope lease can also give a replacement companion the previous companion’s shortcut.

#### Findings

**R10-F1** · P1 · defect · blocks readiness — Saving a live legacy shortcut before migration can reject the archive
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2490`; acceptance: §4 existing save compatibility, §4 toolbar persistence across save/load, §7 affected save/lifecycle paths
- Trigger: The archive header runs before bar repair. A party rider has an exact older Mount or Dismount slot bound to its active, leaseable shell fact, with no canonical public binding or prior placement record.
- Expected: The save contains one restorable binding at the chosen index and one placement record. It passes the existing archive validator.
- Actual: RecoverLegacyAlias records the alias index as an interrupted public placement. CaptureLiveSlots then writes the callable alias as kind 1 or 2, but adds the actor to its written set only for kind 6. CaptureAbsentIntent consequently adds kind 6 at the same index. MountedArchiveData.ValidateSlots keys both rows by actor and index and rejects the archive.
- Why it blocks / does not: A supported older binding can prevent the native save from passing its header barrier.
- Requested correction: Make the live and remembered row writers agree on the effective public binding during alias migration. Emit the chosen index once and preserve the archive’s duplicate-key check.
- Requested evidence: Run save-first capture with an eligible rider, a live exact Mount alias at slot 9 and no canonical binding. Pass the resulting rows through the production archive codec and verify one restored public binding at 9. Cover the corresponding live Dismount alias.

**R9-F2** · P2 · defect · blocks readiness — A legacy save-scope lease can restore a retired shortcut for a replacement companion
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2642`; acceptance: §4 genuine companion removal, §4 save/load and placement intent
- Trigger: Save-first recovery records an unmounted rider’s older Mount alias for pet A. While its slot is leased, the rider acquires supported pet B and becomes eligible for the Mount shell.
- Expected: After the companion changes, the old index stays empty and the next save omits its public-control row.
- Actual: CaptureAndClearManagedHotbarSlots leases the older Mount alias. At release, arrangementHolds bypasses AllowsLeaseRestore for every blueprint except mountStateAbility. The newly active Mount shell for companion B is therefore written at A’s index. The next ReconcilePublicControlSlots converts it to the canonical control and records it as B’s placement.
- Why it blocks / does not: A supported representation of the same shortcut can still undo retirement and persist the old companion’s placement for its replacement.
- Requested correction: Apply the current placement and companion check to leased Mount and Dismount aliases as well as the canonical public control. Preserve same-companion restoration and the exact placeholder check.
- Requested evidence: Start with pet A temporarily unresolved and its legacy Mount alias at 9. Save before the first update, replace A with eligible pet B during the scope, release leases, reconcile and resave. Verify no alias or public binding at 9 and no kind-6 row for B. With A retained and resolving, verify exactly one public binding returns.

**R9-F4** · P2 · defect · blocks readiness — Native bad-slot cleanup can erase an older shortcut before migration sees it
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1960`; acceptance: §4 temporary mount unavailability, §4 existing save compatibility
- Trigger: Native toolbar access reaches an older party rider’s inactive Mount or Dismount alias before the first KMC reconciliation or archive capture.
- Expected: While the same persistent pet link remains, the chosen index survives native invalidation and the canonical control returns there once.
- Actual: ObserveInvalidatedPublicSlot returns immediately unless the blueprint equals mountStateAbility. When native GetSlot or CleanupBadSlots empties an inactive exact Mount or Dismount alias first, both later RecoverLegacyAlias call sites see an empty slot. With no saved row, the chosen index is lost.
- Why it blocks / does not: Whether an older player-authored shortcut survives temporary pet absence still depends on whether the native UI reads the bar before KMC does.
- Requested correction: Recover an identifiable legacy public alias at the native invalidation boundary before the slot is replaced. Preserve canonical-binding precedence, existing placement intent, companion retirement and the load fence.
- Requested evidence: Start with no owned rows, an unresolved linked pet and an inactive legacy alias at slot 9. Deliver the IsBad observation and empty the slot before either update or capture. Verify a resave preserves 9 and the same pet’s return restores one canonical binding. Include controls for an existing canonical binding elsewhere and a cleared pet link.

#### Prior finding assessments

- **R9-F1** — `fix_verified` — ResolveExactKind returns None for a null input before comparing registration fields. Reserve cleanup and serialization capture both use this classifier. The focused component covers absent, partial and complete registration with unrelated slot identities and a positive KMC control.
- **R9-F2** — `fix_rejected` — The canonical kind-6 lease now respects retirement. Production also leases older public aliases, which bypass the new guard and can recreate the same retired shortcut. The test lease helper selects only the canonical binding.
- **R9-F3** — `fix_verified` — MountedScanLedger prunes units by current reserve membership and releases its player reference on a null world. Update synchronizes before its reconciliation early returns, and load, disable and disposal reset the ledger. The component checks collection and repeated scans of an unchanged roster.
- **R9-F4** — `fix_rejected` — The new recovery handles update-first and save-first order while the alias remains on the bar. The native invalidation observer still ignores both legacy aliases, so cleanup-first order loses the same placement.

#### Evidence assessment

- **Inspected:** Controller mission, feature diff, changed-file list, prior findings, developer report and command transcript; Repository validation policy, current project guidance, X01 compatibility notes and the city/prone acceptance record; Toolbar fact reconciliation, native slot invalidation, reserve migration, save leases, archive capture and validation, load and area restore paths; City roster projection and Harmony adapter, mounted click admission and attack command, focused wrapper and regression fixtures
- **Executed verification:** Reviewer confirmed the snapshot HEAD is c856b4e2cf457351271906c71ef1400544cc940b. The feature diff passed git diff --check.; The developer transcript records a guarded push to codex/mounted-city-and-prone-fixes and a controller focused-gate PASS on that exact commit. The Fast tier with Test-CityToolbarProneFocused.ps1 exited 0 in 45.9 seconds with the worktree unchanged.; The transcript also records a pre-commit Fast run that exited 1. Its truncated output does not identify which individual mutation checks failed.
- **Source inspection only:** Traced the duplicate archive key from alias recovery through both row writers to MountedArchiveData.ValidateSlots.; Traced cleanup-first alias loss and replacement-companion lease restoration through the production service. These failing sequences were not executed.; The city projection returns the existing linked pet instance. The combat change carries an explicit order on an already unconscious target through mounted admission. Actual equipment interaction, conscious-prone targeting and attack rolls still require in-game observation.
- **Required gaps:** Verify that save-first migration of a live legacy alias produces an archive accepted by the production validator.; Verify cleanup-first recovery of an inactive legacy alias and release of a legacy lease across companion replacement.
- **Limitations:** Review was read-only. No build, game launch, deployment or save transaction was performed by the reviewer.; The focused gate is offline. The toolbar fixture implements its own slot capture, lease and invalidation loops. In particular, its live writer and lease helper only select the canonical public control.; The area-restore adapter remains unexecuted. The handoff reports 63 matching compile diagnostics at base and candidate.; Native capital UI interaction and combat resolution remain NOT RUN. The handoff identifies the missing candidate package, fixture and reader prerequisites and supplies owner playtest steps.

#### Summary

- **Delivered behavior:** The candidate tracks per-rider toolbar placement through temporary absence, projects eligible owners’ persistent pets into the capital roster, and admits explicit mounted attacks on already unconscious living enemies. The city issues share a roster transition as a trigger and have separate causes. Legacy toolbar migration still has the three failures listed above.
- **Scope reviewed:** Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through c856b4e2cf457351271906c71ef1400544cc940b.
- **Pending later-phase work:** none
- **Unresolved questions:** The reported weapon and the enemy’s consciousness at the original click are unknown. Source inspection identified an unconscious-target admission failure. The conscious-prone case remains a native playtest question.
- **Remaining owner review:** After correction, verify the chosen toolbar slot across capital visits, interior transitions and saves, including a deliberate move or removal.; Select a reserve owner’s pet in Inventory and Character, change equipment, reopen the panels and cold-load the save to verify identity and equipment.; Compare mounted and dismounted longbow attacks on standing, consciously prone and already unconscious targets in real time and turn based. Confirm actual rolls, normal costs, range and line of sight.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff06-530f-7271-abc4-798048c1725d`, duration 341.1s, response sha256 `b0333b2f8cf56b2dd7125ae4daefa86319d40afc1324cebdc7ed8b141703c721`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r10-993317`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "c856b4e2cf457351271906c71ef1400544cc940b",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the reviewed commit. Three toolbar defects still block readiness. Saving before migration of a live Mount or Dismount alias can produce duplicate archive rows and fail the save. Native cleanup can erase an inactive alias before its chosen slot is recorded. A save-scope lease can also give a replacement companion the previous companion\u2019s shortcut.",
  "evidence_assessment": {
    "executed_verification": [
      "Reviewer confirmed the snapshot HEAD is c856b4e2cf457351271906c71ef1400544cc940b. The feature diff passed git diff --check.",
      "The developer transcript records a guarded push to codex/mounted-city-and-prone-fixes and a controller focused-gate PASS on that exact commit. The Fast tier with Test-CityToolbarProneFocused.ps1 exited 0 in 45.9 seconds with the worktree unchanged.",
      "The transcript also records a pre-commit Fast run that exited 1. Its truncated output does not identify which individual mutation checks failed."
    ],
    "inspected": [
      "Controller mission, feature diff, changed-file list, prior findings, developer report and command transcript",
      "Repository validation policy, current project guidance, X01 compatibility notes and the city/prone acceptance record",
      "Toolbar fact reconciliation, native slot invalidation, reserve migration, save leases, archive capture and validation, load and area restore paths",
      "City roster projection and Harmony adapter, mounted click admission and attack command, focused wrapper and regression fixtures"
    ],
    "limitations": [
      "Review was read-only. No build, game launch, deployment or save transaction was performed by the reviewer.",
      "The focused gate is offline. The toolbar fixture implements its own slot capture, lease and invalidation loops. In particular, its live writer and lease helper only select the canonical public control.",
      "The area-restore adapter remains unexecuted. The handoff reports 63 matching compile diagnostics at base and candidate.",
      "Native capital UI interaction and combat resolution remain NOT RUN. The handoff identifies the missing candidate package, fixture and reader prerequisites and supplies owner playtest steps."
    ],
    "required_gaps": [
      "Verify that save-first migration of a live legacy alias produces an archive accepted by the production validator.",
      "Verify cleanup-first recovery of an inactive legacy alias and release of a legacy lease across companion replacement."
    ],
    "source_inspection_only": [
      "Traced the duplicate archive key from alias recovery through both row writers to MountedArchiveData.ValidateSlots.",
      "Traced cleanup-first alias loss and replacement-companion lease restoration through the production service. These failing sequences were not executed.",
      "The city projection returns the existing linked pet instance. The combat change carries an explicit order on an already unconscious target through mounted admission. Actual equipment interaction, conscious-prone targeting and attack rolls still require in-game observation."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 existing save compatibility",
        "\u00a74 toolbar persistence across save/load",
        "\u00a77 affected save/lifecycle paths"
      ],
      "actual": "RecoverLegacyAlias records the alias index as an interrupted public placement. CaptureLiveSlots then writes the callable alias as kind 1 or 2, but adds the actor to its written set only for kind 6. CaptureAbsentIntent consequently adds kind 6 at the same index. MountedArchiveData.ValidateSlots keys both rows by actor and index and rejects the archive.",
      "blocking_rationale": "A supported older binding can prevent the native save from passing its header barrier.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The save contains one restorable binding at the chosen index and one placement record. It passes the existing archive validator.",
      "id": "R10-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2490
      },
      "priority": "P1",
      "requested_correction": "Make the live and remembered row writers agree on the effective public binding during alias migration. Emit the chosen index once and preserve the archive\u2019s duplicate-key check.",
      "requested_evidence": "Run save-first capture with an eligible rider, a live exact Mount alias at slot 9 and no canonical binding. Pass the resulting rows through the production archive codec and verify one restored public binding at 9. Cover the corresponding live Dismount alias.",
      "title": "Saving a live legacy shortcut before migration can reject the archive",
      "trigger": "The archive header runs before bar repair. A party rider has an exact older Mount or Dismount slot bound to its active, leaseable shell fact, with no canonical public binding or prior placement record."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 save/load and placement intent"
      ],
      "actual": "CaptureAndClearManagedHotbarSlots leases the older Mount alias. At release, arrangementHolds bypasses AllowsLeaseRestore for every blueprint except mountStateAbility. The newly active Mount shell for companion B is therefore written at A\u2019s index. The next ReconcilePublicControlSlots converts it to the canonical control and records it as B\u2019s placement.",
      "blocking_rationale": "A supported representation of the same shortcut can still undo retirement and persist the old companion\u2019s placement for its replacement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "After the companion changes, the old index stays empty and the next save omits its public-control row.",
      "id": "R9-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2642
      },
      "priority": "P2",
      "requested_correction": "Apply the current placement and companion check to leased Mount and Dismount aliases as well as the canonical public control. Preserve same-companion restoration and the exact placeholder check.",
      "requested_evidence": "Start with pet A temporarily unresolved and its legacy Mount alias at 9. Save before the first update, replace A with eligible pet B during the scope, release leases, reconcile and resave. Verify no alias or public binding at 9 and no kind-6 row for B. With A retained and resolving, verify exactly one public binding returns.",
      "title": "A legacy save-scope lease can restore a retired shortcut for a replacement companion",
      "trigger": "Save-first recovery records an unmounted rider\u2019s older Mount alias for pet A. While its slot is leased, the rider acquires supported pet B and becomes eligible for the Mount shell."
    },
    {
      "acceptance_ids": [
        "\u00a74 temporary mount unavailability",
        "\u00a74 existing save compatibility"
      ],
      "actual": "ObserveInvalidatedPublicSlot returns immediately unless the blueprint equals mountStateAbility. When native GetSlot or CleanupBadSlots empties an inactive exact Mount or Dismount alias first, both later RecoverLegacyAlias call sites see an empty slot. With no saved row, the chosen index is lost.",
      "blocking_rationale": "Whether an older player-authored shortcut survives temporary pet absence still depends on whether the native UI reads the bar before KMC does.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "While the same persistent pet link remains, the chosen index survives native invalidation and the canonical control returns there once.",
      "id": "R9-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1960
      },
      "priority": "P2",
      "requested_correction": "Recover an identifiable legacy public alias at the native invalidation boundary before the slot is replaced. Preserve canonical-binding precedence, existing placement intent, companion retirement and the load fence.",
      "requested_evidence": "Start with no owned rows, an unresolved linked pet and an inactive legacy alias at slot 9. Deliver the IsBad observation and empty the slot before either update or capture. Verify a resave preserves 9 and the same pet\u2019s return restores one canonical binding. Include controls for an existing canonical binding elsewhere and a cleared pet link.",
      "title": "Native bad-slot cleanup can erase an older shortcut before migration sees it",
      "trigger": "Native toolbar access reaches an older party rider\u2019s inactive Mount or Dismount alias before the first KMC reconciliation or archive capture."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F1",
      "rationale": "ResolveExactKind returns None for a null input before comparing registration fields. Reserve cleanup and serialization capture both use this classifier. The focused component covers absent, partial and complete registration with unrelated slot identities and a positive KMC control."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F2",
      "rationale": "The canonical kind-6 lease now respects retirement. Production also leases older public aliases, which bypass the new guard and can recreate the same retired shortcut. The test lease helper selects only the canonical binding."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F3",
      "rationale": "MountedScanLedger prunes units by current reserve membership and releases its player reference on a null world. Update synchronizes before its reconciliation early returns, and load, disable and disposal reset the ledger. The component checks collection and repeated scans of an unchanged roster."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R9-F4",
      "rationale": "The new recovery handles update-first and save-first order while the alias remains on the bar. The native invalidation observer still ignores both legacy aliases, so cleanup-first order loses the same placement."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r10-993317",
  "round": 10,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate tracks per-rider toolbar placement through temporary absence, projects eligible owners\u2019 persistent pets into the capital roster, and admits explicit mounted attacks on already unconscious living enemies. The city issues share a roster transition as a trigger and have separate causes. Legacy toolbar migration still has the three failures listed above.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, verify the chosen toolbar slot across capital visits, interior transitions and saves, including a deliberate move or removal.",
      "Select a reserve owner\u2019s pet in Inventory and Character, change equipment, reopen the panels and cold-load the save to verify identity and equipment.",
      "Compare mounted and dismounted longbow attacks on standing, consciously prone and already unconscious targets in real time and turn based. Confirm actual rolls, normal costs, range and line of sight."
    ],
    "scope_reviewed": "Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through c856b4e2cf457351271906c71ef1400544cc940b.",
    "unresolved_questions": [
      "The reported weapon and the enemy\u2019s consciousness at the original click are unknown. Source inspection identified an unconscious-target admission failure. The conscious-prone case remains a native playtest question."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
