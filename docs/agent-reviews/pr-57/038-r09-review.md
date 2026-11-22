<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r09-review part=1/1 sha256=a65944dad6917c0fe161915c374e3d63ead2b4f3fbd5d1a030e3c2b1f6841ef6 -->
### Codex GPT-6.2 Astra — review — round 9 — candidate `a3462521bd`

**Decision:** `changes_required` — Both round-eight fixes are verified, and the focused gate passed on the requested commit. Four toolbar lifecycle defects remain. The most consequential is that reserve cleanup can erase unrelated shortcuts when control registration is incomplete.

#### Findings

**R9-F1** · P1 · defect · blocks readiness — Reserve cleanup can erase unrelated shortcuts before control registration completes
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2047`; acceptance: §4 ordinary abilities and unrelated slots, §4 mod disable/removal and world disposal
- Trigger: Disable or dispose the service with a populated reserve roster before TryRegister succeeds, or after partial registration. A reserve bar contains an item or another non-ability shortcut.
- Expected: Only a slot with an identifiable KMC blueprint is eligible for cleanup.
- Actual: ClearReserveBindings calls CountManagedHotbarSlots and ClearManagedHotbarSlots without a registration guard. Both pass a null blueprint to ResolveKind for a non-ability slot. ResolveKind compares by ReferenceEquals, so null matches an uninitialized control field. Cleanup then writes an empty slot over the unrelated shortcut. CaptureAndClearManagedHotbarSlots uses the same classification when scanning reserve bars for serialization.
- Why it blocks / does not: The expanded cleanup can erase player-authored reserve toolbar contents, which a subsequent save can make permanent.
- Requested correction: Make ResolveKind return None for a missing input blueprint before comparing it with registered controls. Apply that identity rule to both cleanup and serialization.
- Requested evidence: Exercise reserve disable/disposal and save-scope capture before full registration with an occupied non-ability slot and an ordinary ability. Verify both original slot objects survive. Include a positive control for an exact registered KMC binding.

**R9-F2** · P2 · defect · blocks readiness — Save-scope release can restore a retired shortcut for a different companion
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2605`; acceptance: §4 genuine companion removal, §4 save/load and placement intent
- Trigger: The public slot is leased for serialization, and the rider's companion changes from A to B before the save worker settles. The original placeholder remains in the slot.
- Expected: A lease restores the chosen slot only while that arrangement still belongs to the rider's current companion. Genuine replacement retires it.
- Actual: EndSaveSerializationScope runs Update, which retires A's arrangement and can lease a fresh public fact for B. ClearSerializationHotbarLeases then checks only the old placeholder and the currently active blueprint fact. It restores B's data at A's index and calls ObserveBound, undoing the retirement.
- Why it blocks / does not: A save-scope lease can recreate a shortcut that reconciliation correctly retired and cause the next save to preserve it for the replacement companion.
- Requested correction: Check the current public-control arrangement and companion identity when releasing a serialization lease. Preserve ordinary same-companion restoration and the existing placeholder check.
- Requested evidence: Lease slot 9 for pet A, change the rider's link to supported pet B before scope release, then run the production release order. Verify slot 9 stays empty and the next archive omits its kind-6 row. Repeat with A retained and verify exactly one binding at 9.

**R9-F3** · P2 · defect · blocks readiness — Reserve scan bookkeeping retains removed units and the outgoing player world
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2076`; acceptance: §4 genuine companion removal and world disposal, §3 actor lifecycle
- Trigger: A scanned reserve unit permanently leaves the roster, or the world is disposed before another nonnull Player is scanned.
- Expected: One-time migration bookkeeping releases removed unit instances and the disposed player world.
- Actual: reserveScanned is a strong HashSet of every reserve unit scanned in the world. No roster departure removes an entry. reserveScanWorld also strongly holds the Player, and AdmitReserveBindings returns on a null player before clearing either reference. The cache is released only on disable, service disposal or a later scan with another nonnull player.
- Why it blocks / does not: The new scan cache retains retired entity graphs for the continuing session and can retain a disposed world while no replacement player is present.
- Requested correction: Bound the scan bookkeeping to current roster membership and world lifetime, or use weak identities that cannot keep discarded units or the Player alive.
- Requested evidence: Scan a reserve unit, remove it from the roster, then dispose the world with no replacement player. Verify the service releases both identities. Retain a control showing repeated scans of an unchanged reserve bar do not repeat migration.

**R9-F4** · P2 · defect · blocks readiness — A temporarily unavailable party rider loses an older Mount or Dismount binding
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1897`; acceptance: §4 temporary mount unavailability, §4 existing save compatibility
- Trigger: An older native bar has an exact legacy alias and no owned placement rows. The rider is a current candidate when first reconciled, but its linked pet is temporarily unresolved.
- Expected: An exact legacy Mount or Dismount binding preserves its recoverable index until the same pet resolves. An existing canonical binding takes precedence.
- Actual: The new reserve recovery handles this alias, but the party reconciliation path converts it to an empty slot whenever publicActive is false and records placement as complete. Boundary observation recognizes only the canonical public blueprint. With no earlier arrangement, the alias index is lost and later reconciliation leaves the rider off the bar.
- Why it blocks / does not: An identifiable supported older shortcut is permanently lost on temporary pet non-resolution, depending on which roster handles the rider first.
- Requested correction: Preserve the identifiable legacy index before clearing an unavailable party rider's alias, including when archive capture precedes the first update. Use the existing migration precedence and permanent-loss rules.
- Requested evidence: Start a party rider with a legacy alias at 9, no owned rows, and a persistent pet ID whose unit temporarily fails to resolve. Exercise update-first and save-first order. Verify a resave retains the index and the same pet's return restores the canonical control once. A cleared link should recover nothing.

#### Prior finding assessments

- **R8-F1** — `fix_verified` — RecoveredPublicSlot gives the canonical public binding precedence over legacy aliases. The reserve loop migrates legacy Mounted Charge through ResolveStockChargeForSlot and preserves an existing stock Charge elsewhere. The candidate gate records the mixed-bar regression passing.
- **R8-F2** — `fix_verified` — Live, unbound reconciliation now preserves the interrupted slot and known companion identity when the pet link is empty but the relationship still grants Dismount. The candidate gate records the faulted-rider regression passing, including the entitlement-ended and different-pet controls.

#### Evidence assessment

- **Inspected:** The mission, repository instructions, validation policy, current handoffs, feature diff, prior findings, developer transcript, focused gate receipt and log.; The city roster projection and GetGroup hook, mounted stock-click and attack-command paths, toolbar reconciliation, archive capture and replay, save-scope leases, reserve scanning, disable and disposal.; The focused wrapper, changed adapters and component regressions, including their substitutions for native units, slots and rosters.
- **Executed verification:** The controller receipt and CLI transcript confirm that Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 passed on a3462521 in 24.8 seconds, with exit code 0 and an unchanged worktree.; The candidate gate log records 1454 component checks, 153 source checks, 820 assembly contracts, 21 click-composition checks, 3 archive-bound checks and 22 six-pair archive-adapter checks passing. It names both new round-eight regressions as passing.; The gate inspected product DLL SHA-256 81e2ddef82255669c175594b557730bdd2c060c54aade332fec4361bdc651762, MVID 31ec49f8-534f-4874-b468-d1f8e33d3bbf. The area-adapter comparison found the same 63 compiler diagnostics on base and candidate.; Read-only Git checks confirmed HEAD and the merge base. The feature diff passed git diff --check.
- **Source inspection only:** The four findings below follow from production control flow. They were not reproduced in the game.; The city issues share a capital roster transition. Toolbar loss follows temporary control-fact withdrawal, while missing portraits follow the native roster's in-game pet filter.; The combat change handles explicit orders on already-unconscious living enemies. The conscious-trip case has a source trace, and the owner's original weapon and target state remain unknown.
- **Required gaps:** Correct and cover unregistered cleanup, companion replacement during a save lease, reserve-cache lifetime, and legacy alias recovery for a temporarily ineligible party rider. Obtain the focused receipt on the corrected commit.
- **Limitations:** Native gameplay was not run on this candidate. The handoff identifies the missing candidate package, current capital reader and native UI/combat driver coverage, and gives owner playtest steps. No runtime transaction required restoration.; The area-restore adapter's behavioral assertion remains unexecuted because its facade fails to compile on both trees.; The mutation run exited 1, but its transcript is truncated before the failing assertions. The candidate gate independently records the passing assertions.

#### Summary

- **Delivered behavior:** The candidate adds per-rider placement intent using the existing archive format, projects eligible reserve owners' persistent pets into the capital UI roster, and carries an explicit order on an already-unconscious living enemy through mounted attack admission. Both pending round-eight corrections pass their focused regressions.
- **Scope reviewed:** Complete maintenance mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through a3462521bd66c4f414a14a387b7b7fdebecbbe56.
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the weapon, the enemy's consciousness at the click, or the exact transition that erased the toolbar shortcut.
- **Remaining owner review:** After correction, verify chosen-slot persistence through actual capital visits and saves, and inspect and equip the exact reserve pet in Inventory and Character.; Compare mounted and dismounted longbow attacks against standing, consciously tripped and unconscious enemies in real time and turn based. Observe actual rolls, range, line of sight, penalties and action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1fef7-429a-7573-a403-78de4c4ed89e`, duration 462.6s, response sha256 `fb2d9b8583de44ab5a18fdd548b7928d5a32ba2db06ff670e92b8824b118db5e`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r09-2146f9`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "a3462521bd66c4f414a14a387b7b7fdebecbbe56",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "Both round-eight fixes are verified, and the focused gate passed on the requested commit. Four toolbar lifecycle defects remain. The most consequential is that reserve cleanup can erase unrelated shortcuts when control registration is incomplete.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller receipt and CLI transcript confirm that Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 passed on a3462521 in 24.8 seconds, with exit code 0 and an unchanged worktree.",
      "The candidate gate log records 1454 component checks, 153 source checks, 820 assembly contracts, 21 click-composition checks, 3 archive-bound checks and 22 six-pair archive-adapter checks passing. It names both new round-eight regressions as passing.",
      "The gate inspected product DLL SHA-256 81e2ddef82255669c175594b557730bdd2c060c54aade332fec4361bdc651762, MVID 31ec49f8-534f-4874-b468-d1f8e33d3bbf. The area-adapter comparison found the same 63 compiler diagnostics on base and candidate.",
      "Read-only Git checks confirmed HEAD and the merge base. The feature diff passed git diff --check."
    ],
    "inspected": [
      "The mission, repository instructions, validation policy, current handoffs, feature diff, prior findings, developer transcript, focused gate receipt and log.",
      "The city roster projection and GetGroup hook, mounted stock-click and attack-command paths, toolbar reconciliation, archive capture and replay, save-scope leases, reserve scanning, disable and disposal.",
      "The focused wrapper, changed adapters and component regressions, including their substitutions for native units, slots and rosters."
    ],
    "limitations": [
      "Native gameplay was not run on this candidate. The handoff identifies the missing candidate package, current capital reader and native UI/combat driver coverage, and gives owner playtest steps. No runtime transaction required restoration.",
      "The area-restore adapter's behavioral assertion remains unexecuted because its facade fails to compile on both trees.",
      "The mutation run exited 1, but its transcript is truncated before the failing assertions. The candidate gate independently records the passing assertions."
    ],
    "required_gaps": [
      "Correct and cover unregistered cleanup, companion replacement during a save lease, reserve-cache lifetime, and legacy alias recovery for a temporarily ineligible party rider. Obtain the focused receipt on the corrected commit."
    ],
    "source_inspection_only": [
      "The four findings below follow from production control flow. They were not reproduced in the game.",
      "The city issues share a capital roster transition. Toolbar loss follows temporary control-fact withdrawal, while missing portraits follow the native roster's in-game pet filter.",
      "The combat change handles explicit orders on already-unconscious living enemies. The conscious-trip case has a source trace, and the owner's original weapon and target state remain unknown."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 ordinary abilities and unrelated slots",
        "\u00a74 mod disable/removal and world disposal"
      ],
      "actual": "ClearReserveBindings calls CountManagedHotbarSlots and ClearManagedHotbarSlots without a registration guard. Both pass a null blueprint to ResolveKind for a non-ability slot. ResolveKind compares by ReferenceEquals, so null matches an uninitialized control field. Cleanup then writes an empty slot over the unrelated shortcut. CaptureAndClearManagedHotbarSlots uses the same classification when scanning reserve bars for serialization.",
      "blocking_rationale": "The expanded cleanup can erase player-authored reserve toolbar contents, which a subsequent save can make permanent.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only a slot with an identifiable KMC blueprint is eligible for cleanup.",
      "id": "R9-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2047
      },
      "priority": "P1",
      "requested_correction": "Make ResolveKind return None for a missing input blueprint before comparing it with registered controls. Apply that identity rule to both cleanup and serialization.",
      "requested_evidence": "Exercise reserve disable/disposal and save-scope capture before full registration with an occupied non-ability slot and an ordinary ability. Verify both original slot objects survive. Include a positive control for an exact registered KMC binding.",
      "title": "Reserve cleanup can erase unrelated shortcuts before control registration completes",
      "trigger": "Disable or dispose the service with a populated reserve roster before TryRegister succeeds, or after partial registration. A reserve bar contains an item or another non-ability shortcut."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 save/load and placement intent"
      ],
      "actual": "EndSaveSerializationScope runs Update, which retires A's arrangement and can lease a fresh public fact for B. ClearSerializationHotbarLeases then checks only the old placeholder and the currently active blueprint fact. It restores B's data at A's index and calls ObserveBound, undoing the retirement.",
      "blocking_rationale": "A save-scope lease can recreate a shortcut that reconciliation correctly retired and cause the next save to preserve it for the replacement companion.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A lease restores the chosen slot only while that arrangement still belongs to the rider's current companion. Genuine replacement retires it.",
      "id": "R9-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2605
      },
      "priority": "P2",
      "requested_correction": "Check the current public-control arrangement and companion identity when releasing a serialization lease. Preserve ordinary same-companion restoration and the existing placeholder check.",
      "requested_evidence": "Lease slot 9 for pet A, change the rider's link to supported pet B before scope release, then run the production release order. Verify slot 9 stays empty and the next archive omits its kind-6 row. Repeat with A retained and verify exactly one binding at 9.",
      "title": "Save-scope release can restore a retired shortcut for a different companion",
      "trigger": "The public slot is leased for serialization, and the rider's companion changes from A to B before the save worker settles. The original placeholder remains in the slot."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal and world disposal",
        "\u00a73 actor lifecycle"
      ],
      "actual": "reserveScanned is a strong HashSet of every reserve unit scanned in the world. No roster departure removes an entry. reserveScanWorld also strongly holds the Player, and AdmitReserveBindings returns on a null player before clearing either reference. The cache is released only on disable, service disposal or a later scan with another nonnull player.",
      "blocking_rationale": "The new scan cache retains retired entity graphs for the continuing session and can retain a disposed world while no replacement player is present.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "One-time migration bookkeeping releases removed unit instances and the disposed player world.",
      "id": "R9-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2076
      },
      "priority": "P2",
      "requested_correction": "Bound the scan bookkeeping to current roster membership and world lifetime, or use weak identities that cannot keep discarded units or the Player alive.",
      "requested_evidence": "Scan a reserve unit, remove it from the roster, then dispose the world with no replacement player. Verify the service releases both identities. Retain a control showing repeated scans of an unchanged reserve bar do not repeat migration.",
      "title": "Reserve scan bookkeeping retains removed units and the outgoing player world",
      "trigger": "A scanned reserve unit permanently leaves the roster, or the world is disposed before another nonnull Player is scanned."
    },
    {
      "acceptance_ids": [
        "\u00a74 temporary mount unavailability",
        "\u00a74 existing save compatibility"
      ],
      "actual": "The new reserve recovery handles this alias, but the party reconciliation path converts it to an empty slot whenever publicActive is false and records placement as complete. Boundary observation recognizes only the canonical public blueprint. With no earlier arrangement, the alias index is lost and later reconciliation leaves the rider off the bar.",
      "blocking_rationale": "An identifiable supported older shortcut is permanently lost on temporary pet non-resolution, depending on which roster handles the rider first.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An exact legacy Mount or Dismount binding preserves its recoverable index until the same pet resolves. An existing canonical binding takes precedence.",
      "id": "R9-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1897
      },
      "priority": "P2",
      "requested_correction": "Preserve the identifiable legacy index before clearing an unavailable party rider's alias, including when archive capture precedes the first update. Use the existing migration precedence and permanent-loss rules.",
      "requested_evidence": "Start a party rider with a legacy alias at 9, no owned rows, and a persistent pet ID whose unit temporarily fails to resolve. Exercise update-first and save-first order. Verify a resave retains the index and the same pet's return restores the canonical control once. A cleared link should recover nothing.",
      "title": "A temporarily unavailable party rider loses an older Mount or Dismount binding",
      "trigger": "An older native bar has an exact legacy alias and no owned placement rows. The rider is a current candidate when first reconciled, but its linked pet is temporarily unresolved."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F1",
      "rationale": "RecoveredPublicSlot gives the canonical public binding precedence over legacy aliases. The reserve loop migrates legacy Mounted Charge through ResolveStockChargeForSlot and preserves an existing stock Charge elsewhere. The candidate gate records the mixed-bar regression passing."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R8-F2",
      "rationale": "Live, unbound reconciliation now preserves the interrupted slot and known companion identity when the pet link is empty but the relationship still grants Dismount. The candidate gate records the faulted-rider regression passing, including the entitlement-ended and different-pet controls."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r09-2146f9",
  "round": 9,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds per-rider placement intent using the existing archive format, projects eligible reserve owners' persistent pets into the capital UI roster, and carries an explicit order on an already-unconscious living enemy through mounted attack admission. Both pending round-eight corrections pass their focused regressions.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, verify chosen-slot persistence through actual capital visits and saves, and inspect and equip the exact reserve pet in Inventory and Character.",
      "Compare mounted and dismounted longbow attacks against standing, consciously tripped and unconscious enemies in real time and turn based. Observe actual rolls, range, line of sight, penalties and action expenditure."
    ],
    "scope_reviewed": "Complete maintenance mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through a3462521bd66c4f414a14a387b7b7fdebecbbe56.",
    "unresolved_questions": [
      "The original report does not identify the weapon, the enemy's consciousness at the click, or the exact transition that erased the toolbar shortcut."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
