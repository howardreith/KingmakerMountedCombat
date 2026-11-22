<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r22-review part=1/1 sha256=20385ded1cc90b6c2d54cffd86efd74903cc364548da14c9e29c21a0d02d3776 -->
### Codex GPT-6.2 Astra — review — round 22 — candidate `3ff7fd1446`

**Decision:** `changes_required` — The focused gate passed on the fixed candidate. The toolbar still fails a required save/load case. With Fireball in the former slot and a surviving shortcut at 7, the save header selects 7 on its first read and treats it as removed on its second read. Native cleanup, reserve migration and older-save restoration also have paths that lose a placement or retain a discarded slot. These are implementation defects that can be checked offline before owner playtesting.

#### Findings

**R22-F1** · P2 · defect · blocks readiness — Saving before repair can erase the surviving shortcut
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2405`; acceptance: §4 chosen supported toolbar slot, §4 occupied slots, §4 save/load
- Trigger: In the existing two-copy fixture, GetSlot empties stale slot 3, callable copies remain at 7 and 9, and Fireball takes 3. Save before the first KMC update or native rebind.
- Expected: Fireball stays at 3. The archive contains one kind-6 row at 7 and one placement marker. The continuing game and cold restore each retain one shortcut at 7.
- Actual: ObserveArrangementOnBar adopts 7 and marks it live. IsPublicCopyAt then queries TryGetArrangement, which returns an index only for an interrupted choice or an off-bar record. On the next read, both 7 and 9 count as copies and EffectiveBindingIndex returns -1. The header writes only the placement marker. The next save-scope observation records the shortcut off the bar.
- Why it blocks / does not: Saving can erase a visible shortcut that an update taken first would preserve. Reload makes the loss permanent.
- Requested correction: Make an adopted fallback recognizable as the current live choice on subsequent reads of the same bar. Keep the archive query for absent arrangements separate from the decision about which slot the player currently owns.
- Requested evidence: Extend OccupiedChosenSlotWithTwoCopies with an immediate save in its rebind=false case. Check the rows, the bar after save release and the cold-loaded bar against the existing update-first result.

**R18-F1** · P2 · defect · blocks readiness — Removing a natively rebound shortcut can still promote its duplicate
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2279`; acceptance: §4 deliberate removal, §4 no duplicate shortcuts, §4 save/load
- Trigger: Start with stale canonical data at 3, current data at 9 and Attack at 0. Native CleanupBadSlots rebinds 3. The player removes 3 before KMC's first repair.
- Expected: The rider has no Mount / Dismount shortcut after removal. The archive contains only the placement marker and Attack stays at 0.
- Actual: The native report for 3 captures 9 in CopySlots, but TrackCopies returns immediately on a successful rebind of the selected slot. With no earlier emptying, 9 was never registered as a copy. Removing 3 makes 9 the next effective player binding, and capture writes it.
- Why it blocks / does not: Whether a deliberate removal persists still depends on which component first repaired the stale binding.
- Requested correction: When native first rebinds the selected shortcut, retain enough information about the other public bindings already observed on that bar to respect an immediate removal.
- Requested evidence: Use StaleThenCurrentCanonical with native CleanupBadSlots first. Remove 3 immediately after the rebind, then check update-first and save-first removal through cold restore.

**R19-F2** · P2 · defect · blocks readiness — A lower copy can hide native invalidation of the moved shortcut
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2055`; acceptance: §4 player moves, §4 temporary mount unavailability, §4 save/load
- Trigger: Use the occupied, rebound MoveBeyondSurvivingCopy case. Once native has rebound the moved shortcut at 11, the recorded copy at 7 remains on the bar until repair. Native reads and empties a newly invalidated 11 during that interval.
- Expected: Native emptying interrupts the live arrangement at 11. The same pet's shortcut returns there once, and a save during the interruption carries 11.
- Actual: After the rebind, 11 is live, so hasChoice is false. The native-report guard uses PublicBindingIndex, which selects the first canonical index, 7, even though 7 is a recorded copy. It ignores the report for 11. When native empties 11, the next observation classifies the missing shortcut as a player removal.
- Why it blocks / does not: A confirmed move can be lost during the next native invalidation while the lower copy is waiting for repair.
- Requested correction: Use the effective player binding when deciding whether a native report concerns the selected live slot. Apply the same rule to the records that own the current load scope.
- Requested evidence: After MoveBeyondSurvivingCopy rebinds 7 and 11, make 11 stale and have GetSlot(11) empty it before repair. Verify one saved and restored shortcut at 11 with Fireball still at 3.

**R22-F2** · P2 · defect · blocks readiness — An occupied first row prevents recovery from a later legacy row
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2963`; acceptance: §4 existing save compatibility, §4 occupied slots, §7 affected save paths
- Trigger: An older save carries two public-binding rows for one rider at distinct indices. At cold restore, the earlier position is occupied and the later position is empty.
- Expected: On a fresh load, preserve Fireball at 3 and restore one public control at the available saved position, 9, following the existing F2 multiple-row migration behavior.
- Actual: RestorePersistentSlots seeds the first public row before checking its destination. Reconciliation finds 3 occupied and marks the arrangement off the bar. AllowsSavedSlot then rejects the later row at 9. The base reader skipped the occupied row and continued to the next restorable saved index.
- Why it blocks / does not: Upgrading can discard every shortcut from a valid older archive even though one of its saved positions is available.
- Requested correction: For a fresh load with multiple valid public rows, choose the first restorable saved position. Preserve the protection against replaying older rows over a later in-world edit.
- Requested evidence: Cold-load a valid pre-change archive with public rows at 3 and 9, autofill off, an ordinary ability at 3 and 9 empty. Verify one shortcut at 9 after two updates and after resaving. Retain the marker-only removal control.

**R22-F3** · P2 · defect · blocks readiness — First-time reserve migration clears an available fallback
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2344`; acceptance: §4 older city saves, §4 occupied slots, §4 temporary absence
- Trigger: On the existing older reserve bar, GetSlot empties 3 and records the surviving shortcut at 9. Slot 3 becomes occupied before KMC first scans the rider.
- Expected: Keep the surviving position at 9 as placement intent, clear the native KMC references and restore one shortcut at 9 when the same pet returns. Fireball stays at 3.
- Actual: AdmitReserveBindings forgets the copies before classifying the bar. Its subsequent AdmitSurvivingBinding call refuses 9 because an entry for 3 already exists. The scan then clears 9. Capture gives up occupied slot 3, leaving only the placement marker.
- Why it blocks / does not: First-time reserve cleanup can destroy the last usable position for a returning rider.
- Requested correction: Classify the effective reserve binding before clearing the bar and releasing its copy objects. Preserve the free-chosen-slot, retirement and off-bar cases.
- Requested evidence: Extend the first-time reserve part of DepartedRiderReleasesCopies with Fireball at 3. Check a capital save, zero retained copy objects and a same-pet return at 9.

**R20-F1** · P2 · defect · blocks readiness — A reserve rider removed before its first scan retains copy objects
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1852`; acceptance: §4 discarded entity cleanup, §4 genuine roster removal, §7 affected lifecycle paths
- Trigger: Native cleanup records a secondary slot on a never-observed reserve rider. A roster transition removes the rider before the next KMC scan.
- Expected: Release the removed rider's recorded slot objects in the continuing world, including when there is no longer a reserve bar to scan.
- Actual: A rider first encountered by the native observer is absent from observedUnits. If it leaves the roster before the reserve scan, neither the scan nor the departure loop calls ForgetCopies. RevalidateAbsentArrangements only updates placement intent. Same-world registry lookups keep the discarded slot object and its AbilityData reachable.
- Why it blocks / does not: Permanent roster removal can leave discarded native objects held for the rest of the player world.
- Requested correction: Prune recorded slot references for riders that leave the roster without reaching candidate or reserve cleanup. Keep the existing identity checks for a temporarily absent rider.
- Requested evidence: Record copy 9 on a never-observed reserve rider, remove that rider from the roster before the first scan, then update in the same world. Assert zero recorded copies and release of recordedCopiesWorld when this was its last entry.

#### Prior finding assessments

- **R18-F1** — `fix_rejected` — Immediate removal of fallback 7 after the tested 7/9 rebind now passes. Immediate removal after the first successful native rebind still adopts a sibling that TrackCopies never registered.
- **R19-F2** — `fix_rejected` — PlayerBindingElsewhere preserves the move to 11 through the requested rebind and immediate save. The next native invalidation of that live slot still uses the lower raw canonical index while copy 7 survives.
- **R20-F1** — `fix_rejected` — The requested first reserve scan releases the recorded objects. Same-world removal before that scan still bypasses every registry cleanup path for the rider.
- **R21-F1** — `fix_verified` — KMC's own update-first repair removes every public binding except the effective one. The gate covers two updates, removal of 3, marker-only reload and preservation of Attack at 0.
- **R21-F2** — `fix_verified` — CopiesOf no longer acquires the player on an empty lookup. RecordCopies sets the world only when storing copies, and ForgetCopies releases it when the registry empties. The disabled-save fixture checks both sides of world replacement.
- **R21-F3** — `fix_verified` — The acceptance record now identifies the executed explicit-order and ally checks separately from the source traces for Charge, continuation candidates and automatic target validity.

#### Evidence assessment

- **Inspected:** The mission, repository instructions, validation policy, current handoffs, full M..H feature diff, developer report, transcript, gate artifacts and prior findings ledger.; The native toolbar observer, copy registry, placement decisions, save header, serialization leases, reserve migration and cold restore, including the base-version reader and F2 compatibility contract.; The city roster projection and patch, stock-click admission, pair-command target validation, X01 composition check, focused fixtures and acceptance record.
- **Executed verification:** Verified the snapshot HEAD, merge base, mission SHA-256 and gate-log SHA-256. The feature diff passed git diff --check.; The controller transcript and receipt confirm the guarded push and focused gate on 3ff7fd14468556f028206d1000e6267532dccb4c. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 53.3 seconds with the worktree unchanged.; The gate log records 1,502 component checks and 153 source checks passing. The six focused children report 825 assembly contracts, 21 click-composition checks including both patch orders, 9 archive checks, 22 six-pair adapter checks, the matched area-adapter comparison and 3 target-liveness source checks.; The tested DLL had SHA-256 feddca5587c4f697b3a030d4c17025f8fdb19f9507786b54e6b61c9e7f69e561 and MVID cdbf5c43-0382-462a-b24a-c60db3635c47. The pre-commit mutation run separately printed five targeted failures.
- **Source inspection only:** The toolbar fixtures exercise production domain decisions with a double for the native service. Four of the five reported mutations changed mirrored service logic in that double.; The city adapter returns existing companion instances through the capital roster. Actual portrait selection, equipment interaction and selection fallback remain owner observations.; The combat source distinguishes an explicit order on an already unconscious enemy from a conscious trip. Charge, automatic targeting and replacement-target filters were traced. An attack roll on this candidate was not observed.
- **Required gaps:** Verify that an immediate save preserves an adopted fallback, that native cleanup preserves a moved shortcut and respects removal, and that reserve migration preserves the usable slot while releasing discarded objects.; Verify cold restoration from a valid older archive with multiple public rows when the first saved position is occupied. Run the focused gate on the corrected commit.
- **Limitations:** This review used read-only inspection. The controller supplied the executed gate evidence. The newly identified sequences below are source traces.; The area-restore adapter failed to compile with the same 63 diagnostics at base and candidate. Its comparison passed, but its behavior did not execute.; Native UI and combat cases are documented as NOT RUN. The enrollment lacks the candidate-package step, the capital reader expects older control rows, and the existing drivers do not perform the reserve-pet equipment or prone-enemy click cases. No native transaction required restoration.

#### Summary

- **Delivered behavior:** The candidate adds per-rider placement intent, projects eligible owners' persistent pets into the capital UI roster, and admits explicit mounted attack orders on already-unconscious living enemies. The city issues share a roster transition. The shortcut loss arises in KMC fact and slot persistence, while missing portraits arise from the native in-game pet filter.
- **Scope reviewed:** Complete mission against main dbf637021db42cdc9bf27f3d79fc255d4569c788 on codex/mounted-city-and-prone-fixes, enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c. The reviewed and pushed feature head is 3ff7fd14468556f028206d1000e6267532dccb4c. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)
- **Pending later-phase work:** none
- **Unresolved questions:** The original weapon, the enemy's consciousness at the click and the exact transition that lost the owner's shortcut remain unidentified.
- **Remaining owner review:** After correction, check a nondefault shortcut through capital and interior transitions, forced dismount, and saves inside and outside the city.; Select an eligible reserve owner's pet in Inventory and Character, change permitted equipment, reopen the panels and cold-load the save.; Compare mounted and dismounted longbow attacks against standing, conscious-prone and already-unconscious enemies in real time and turn based. Observe actual rolls, range, line of sight, modifiers and action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a20064-6c83-7712-ae1a-eb185fdfb672`, duration 967.3s, response sha256 `1b1cff976aae1cb30daf3d1786f09d32361abfeedc9a310ade53207e6ade9602`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r22-a769ea`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "3ff7fd14468556f028206d1000e6267532dccb4c",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the fixed candidate. The toolbar still fails a required save/load case. With Fireball in the former slot and a surviving shortcut at 7, the save header selects 7 on its first read and treats it as removed on its second read. Native cleanup, reserve migration and older-save restoration also have paths that lose a placement or retain a discarded slot. These are implementation defects that can be checked offline before owner playtesting.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the snapshot HEAD, merge base, mission SHA-256 and gate-log SHA-256. The feature diff passed git diff --check.",
      "The controller transcript and receipt confirm the guarded push and focused gate on 3ff7fd14468556f028206d1000e6267532dccb4c. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 53.3 seconds with the worktree unchanged.",
      "The gate log records 1,502 component checks and 153 source checks passing. The six focused children report 825 assembly contracts, 21 click-composition checks including both patch orders, 9 archive checks, 22 six-pair adapter checks, the matched area-adapter comparison and 3 target-liveness source checks.",
      "The tested DLL had SHA-256 feddca5587c4f697b3a030d4c17025f8fdb19f9507786b54e6b61c9e7f69e561 and MVID cdbf5c43-0382-462a-b24a-c60db3635c47. The pre-commit mutation run separately printed five targeted failures."
    ],
    "inspected": [
      "The mission, repository instructions, validation policy, current handoffs, full M..H feature diff, developer report, transcript, gate artifacts and prior findings ledger.",
      "The native toolbar observer, copy registry, placement decisions, save header, serialization leases, reserve migration and cold restore, including the base-version reader and F2 compatibility contract.",
      "The city roster projection and patch, stock-click admission, pair-command target validation, X01 composition check, focused fixtures and acceptance record."
    ],
    "limitations": [
      "This review used read-only inspection. The controller supplied the executed gate evidence. The newly identified sequences below are source traces.",
      "The area-restore adapter failed to compile with the same 63 diagnostics at base and candidate. Its comparison passed, but its behavior did not execute.",
      "Native UI and combat cases are documented as NOT RUN. The enrollment lacks the candidate-package step, the capital reader expects older control rows, and the existing drivers do not perform the reserve-pet equipment or prone-enemy click cases. No native transaction required restoration."
    ],
    "required_gaps": [
      "Verify that an immediate save preserves an adopted fallback, that native cleanup preserves a moved shortcut and respects removal, and that reserve migration preserves the usable slot while releasing discarded objects.",
      "Verify cold restoration from a valid older archive with multiple public rows when the first saved position is occupied. Run the focused gate on the corrected commit."
    ],
    "source_inspection_only": [
      "The toolbar fixtures exercise production domain decisions with a double for the native service. Four of the five reported mutations changed mirrored service logic in that double.",
      "The city adapter returns existing companion instances through the capital roster. Actual portrait selection, equipment interaction and selection fallback remain owner observations.",
      "The combat source distinguishes an explicit order on an already unconscious enemy from a conscious trip. Charge, automatic targeting and replacement-target filters were traced. An attack roll on this candidate was not observed."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen supported toolbar slot",
        "\u00a74 occupied slots",
        "\u00a74 save/load"
      ],
      "actual": "ObserveArrangementOnBar adopts 7 and marks it live. IsPublicCopyAt then queries TryGetArrangement, which returns an index only for an interrupted choice or an off-bar record. On the next read, both 7 and 9 count as copies and EffectiveBindingIndex returns -1. The header writes only the placement marker. The next save-scope observation records the shortcut off the bar.",
      "blocking_rationale": "Saving can erase a visible shortcut that an update taken first would preserve. Reload makes the loss permanent.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Fireball stays at 3. The archive contains one kind-6 row at 7 and one placement marker. The continuing game and cold restore each retain one shortcut at 7.",
      "id": "R22-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2405
      },
      "priority": "P2",
      "requested_correction": "Make an adopted fallback recognizable as the current live choice on subsequent reads of the same bar. Keep the archive query for absent arrangements separate from the decision about which slot the player currently owns.",
      "requested_evidence": "Extend OccupiedChosenSlotWithTwoCopies with an immediate save in its rebind=false case. Check the rows, the bar after save release and the cold-loaded bar against the existing update-first result.",
      "title": "Saving before repair can erase the surviving shortcut",
      "trigger": "In the existing two-copy fixture, GetSlot empties stale slot 3, callable copies remain at 7 and 9, and Fireball takes 3. Save before the first KMC update or native rebind."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 no duplicate shortcuts",
        "\u00a74 save/load"
      ],
      "actual": "The native report for 3 captures 9 in CopySlots, but TrackCopies returns immediately on a successful rebind of the selected slot. With no earlier emptying, 9 was never registered as a copy. Removing 3 makes 9 the next effective player binding, and capture writes it.",
      "blocking_rationale": "Whether a deliberate removal persists still depends on which component first repaired the stale binding.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The rider has no Mount / Dismount shortcut after removal. The archive contains only the placement marker and Attack stays at 0.",
      "id": "R18-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2279
      },
      "priority": "P2",
      "requested_correction": "When native first rebinds the selected shortcut, retain enough information about the other public bindings already observed on that bar to respect an immediate removal.",
      "requested_evidence": "Use StaleThenCurrentCanonical with native CleanupBadSlots first. Remove 3 immediately after the rebind, then check update-first and save-first removal through cold restore.",
      "title": "Removing a natively rebound shortcut can still promote its duplicate",
      "trigger": "Start with stale canonical data at 3, current data at 9 and Attack at 0. Native CleanupBadSlots rebinds 3. The player removes 3 before KMC's first repair."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 temporary mount unavailability",
        "\u00a74 save/load"
      ],
      "actual": "After the rebind, 11 is live, so hasChoice is false. The native-report guard uses PublicBindingIndex, which selects the first canonical index, 7, even though 7 is a recorded copy. It ignores the report for 11. When native empties 11, the next observation classifies the missing shortcut as a player removal.",
      "blocking_rationale": "A confirmed move can be lost during the next native invalidation while the lower copy is waiting for repair.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Native emptying interrupts the live arrangement at 11. The same pet's shortcut returns there once, and a save during the interruption carries 11.",
      "id": "R19-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2055
      },
      "priority": "P2",
      "requested_correction": "Use the effective player binding when deciding whether a native report concerns the selected live slot. Apply the same rule to the records that own the current load scope.",
      "requested_evidence": "After MoveBeyondSurvivingCopy rebinds 7 and 11, make 11 stale and have GetSlot(11) empty it before repair. Verify one saved and restored shortcut at 11 with Fireball still at 3.",
      "title": "A lower copy can hide native invalidation of the moved shortcut",
      "trigger": "Use the occupied, rebound MoveBeyondSurvivingCopy case. Once native has rebound the moved shortcut at 11, the recorded copy at 7 remains on the bar until repair. Native reads and empties a newly invalidated 11 during that interval."
    },
    {
      "acceptance_ids": [
        "\u00a74 existing save compatibility",
        "\u00a74 occupied slots",
        "\u00a77 affected save paths"
      ],
      "actual": "RestorePersistentSlots seeds the first public row before checking its destination. Reconciliation finds 3 occupied and marks the arrangement off the bar. AllowsSavedSlot then rejects the later row at 9. The base reader skipped the occupied row and continued to the next restorable saved index.",
      "blocking_rationale": "Upgrading can discard every shortcut from a valid older archive even though one of its saved positions is available.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "On a fresh load, preserve Fireball at 3 and restore one public control at the available saved position, 9, following the existing F2 multiple-row migration behavior.",
      "id": "R22-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2963
      },
      "priority": "P2",
      "requested_correction": "For a fresh load with multiple valid public rows, choose the first restorable saved position. Preserve the protection against replaying older rows over a later in-world edit.",
      "requested_evidence": "Cold-load a valid pre-change archive with public rows at 3 and 9, autofill off, an ordinary ability at 3 and 9 empty. Verify one shortcut at 9 after two updates and after resaving. Retain the marker-only removal control.",
      "title": "An occupied first row prevents recovery from a later legacy row",
      "trigger": "An older save carries two public-binding rows for one rider at distinct indices. At cold restore, the earlier position is occupied and the later position is empty."
    },
    {
      "acceptance_ids": [
        "\u00a74 older city saves",
        "\u00a74 occupied slots",
        "\u00a74 temporary absence"
      ],
      "actual": "AdmitReserveBindings forgets the copies before classifying the bar. Its subsequent AdmitSurvivingBinding call refuses 9 because an entry for 3 already exists. The scan then clears 9. Capture gives up occupied slot 3, leaving only the placement marker.",
      "blocking_rationale": "First-time reserve cleanup can destroy the last usable position for a returning rider.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Keep the surviving position at 9 as placement intent, clear the native KMC references and restore one shortcut at 9 when the same pet returns. Fireball stays at 3.",
      "id": "R22-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2344
      },
      "priority": "P2",
      "requested_correction": "Classify the effective reserve binding before clearing the bar and releasing its copy objects. Preserve the free-chosen-slot, retirement and off-bar cases.",
      "requested_evidence": "Extend the first-time reserve part of DepartedRiderReleasesCopies with Fireball at 3. Check a capital save, zero retained copy objects and a same-pet return at 9.",
      "title": "First-time reserve migration clears an available fallback",
      "trigger": "On the existing older reserve bar, GetSlot empties 3 and records the surviving shortcut at 9. Slot 3 becomes occupied before KMC first scans the rider."
    },
    {
      "acceptance_ids": [
        "\u00a74 discarded entity cleanup",
        "\u00a74 genuine roster removal",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "A rider first encountered by the native observer is absent from observedUnits. If it leaves the roster before the reserve scan, neither the scan nor the departure loop calls ForgetCopies. RevalidateAbsentArrangements only updates placement intent. Same-world registry lookups keep the discarded slot object and its AbilityData reachable.",
      "blocking_rationale": "Permanent roster removal can leave discarded native objects held for the rest of the player world.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Release the removed rider's recorded slot objects in the continuing world, including when there is no longer a reserve bar to scan.",
      "id": "R20-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1852
      },
      "priority": "P2",
      "requested_correction": "Prune recorded slot references for riders that leave the roster without reaching candidate or reserve cleanup. Keep the existing identity checks for a temporarily absent rider.",
      "requested_evidence": "Record copy 9 on a never-observed reserve rider, remove that rider from the roster before the first scan, then update in the same world. Assert zero recorded copies and release of recordedCopiesWorld when this was its last entry.",
      "title": "A reserve rider removed before its first scan retains copy objects",
      "trigger": "Native cleanup records a secondary slot on a never-observed reserve rider. A roster transition removes the rider before the next KMC scan."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R18-F1",
      "rationale": "Immediate removal of fallback 7 after the tested 7/9 rebind now passes. Immediate removal after the first successful native rebind still adopts a sibling that TrackCopies never registered."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R19-F2",
      "rationale": "PlayerBindingElsewhere preserves the move to 11 through the requested rebind and immediate save. The next native invalidation of that live slot still uses the lower raw canonical index while copy 7 survives."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R20-F1",
      "rationale": "The requested first reserve scan releases the recorded objects. Same-world removal before that scan still bypasses every registry cleanup path for the rider."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R21-F1",
      "rationale": "KMC's own update-first repair removes every public binding except the effective one. The gate covers two updates, removal of 3, marker-only reload and preservation of Attack at 0."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R21-F2",
      "rationale": "CopiesOf no longer acquires the player on an empty lookup. RecordCopies sets the world only when storing copies, and ForgetCopies releases it when the registry empties. The disabled-save fixture checks both sides of world replacement."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R21-F3",
      "rationale": "The acceptance record now identifies the executed explicit-order and ally checks separately from the source traces for Charge, continuation candidates and automatic target validity."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r22-a769ea",
  "round": 22,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds per-rider placement intent, projects eligible owners' persistent pets into the capital UI roster, and admits explicit mounted attack orders on already-unconscious living enemies. The city issues share a roster transition. The shortcut loss arises in KMC fact and slot persistence, while missing portraits arise from the native in-game pet filter.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, check a nondefault shortcut through capital and interior transitions, forced dismount, and saves inside and outside the city.",
      "Select an eligible reserve owner's pet in Inventory and Character, change permitted equipment, reopen the panels and cold-load the save.",
      "Compare mounted and dismounted longbow attacks against standing, conscious-prone and already-unconscious enemies in real time and turn based. Observe actual rolls, range, line of sight, modifiers and action expenditure."
    ],
    "scope_reviewed": "Complete mission against main dbf637021db42cdc9bf27f3d79fc255d4569c788 on codex/mounted-city-and-prone-fixes, enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c. The reviewed and pushed feature head is 3ff7fd14468556f028206d1000e6267532dccb4c. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)",
    "unresolved_questions": [
      "The original weapon, the enemy's consciousness at the click and the exact transition that lost the owner's shortcut remain unidentified."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
