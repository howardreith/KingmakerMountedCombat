<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r04-review part=1/1 sha256=e4ab521e3f60dc0630fa2da2b57a79c0a16dc023cf6d6c0e3f68ea187a7ec0a7 -->
### Codex GPT-6.2 Astra — review — round 4 — candidate `29ffa3a5f0`

**Decision:** `changes_required` — The focused gate passed on the submitted commit. The toolbar can still lose a chosen slot on save, reverse a deliberate removal, or reuse a stale binding after permanent companion loss. The load-boundary regression also skips an interval present in the production load sequence. The city roster projection and the explicit attack-order change have offline coverage. Their native acceptance remains deferred under the mission’s documented prerequisites.

#### Findings

**R2-F1** · P1 · defect · blocks readiness — The save header drops a live slot when its pet becomes temporarily unavailable
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2181`; acceptance: §4 temporary unavailability and save/load, §7 toolbar persistence
- Trigger: The pet stops resolving before the next control update, and the save header runs while the rider’s old public fact is still active.
- Expected: A rider retaining the same persistent pet link saves kind 6 at the chosen index and one kind 7 record, even if the public fact is about to be withdrawn.
- Actual: ClassifyArrangementsForCapture records the slot as bound and exits the active-fact branch when its data is current. CaptureLiveSlots then skips it because ShouldLease is false. TryGetArrangement skips the continuously bound entry as well. Only kind 7 survives, which loads as an explicit off-bar choice. The test double’s CaptureLive omits the production ShouldLease check.
- Why it blocks / does not: Saving during temporary pet unavailability can permanently erase the chosen slot.
- Requested correction: Classify the observed binding using the same eligibility conditions as the live writer. When the pet link proves temporary unavailability, carry the index as placement intent. Keep the callable-data and caster checks intact.
- Requested evidence: With a dismounted party rider bound at 9, make Pet or its reciprocal link temporarily unresolved while PetUniqueId, the active fact and slot data remain intact. Save before the next control update, decode the rows, reload and restore the same pet. Verify one binding at 9. Repeat after a deliberate removal and verify marker-only persistence.

**R4-F1** · P2 · defect · blocks readiness — Losing the fact lease can undo a removal made since the last update
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:125`; acceptance: §4 respect deliberate removal, §4 temporary mount unavailability
- Trigger: The player clears a live slot and the rider temporarily loses lease eligibility before another reconciliation observes the removal.
- Expected: The last arrangement on the still-live bar is observed before KMC withdraws its fact. A removal remains off-bar when the same pet returns.
- Actual: ReconcileFacts removes the undesired fact before sampling the player’s last edit. The !controlLive branch of Reconcile marks the previously remembered index interrupted. When the pet resolves again, ResolveUnbound restores the removed control to that index.
- Why it blocks / does not: The control reappears after the player deliberately removed it.
- Requested correction: Observe the arrangement before withdrawing or replacing the public fact. Preserve a previously observed native invalidation while respecting an edit to a continuously live control.
- Requested evidence: Remove the live control from slot 9, make the same pet unresolved before the next frame, reconcile, then restore the pet. Verify the slot stays empty through reconciliation and save/load. Keep the corresponding native-invalidation case returning to 9.

**R4-F2** · P2 · defect · blocks readiness — A different pet can revive a binding left on an uninspected toolbar
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1831`; acceptance: §4 genuine companion removal, §4 stale binding cleanup
- Trigger: A party rider permanently loses its pet while its toolbar retains the old slot object, then acquires another supported pet.
- Expected: Confirmed permanent pet loss clears the rider’s managed binding as well as the remembered arrangement. A different pet cannot revive the old choice.
- Actual: When publicActive is false, the slot loop leaves the old ability object in place. Revalidate can retire the arrangement, but a later supported pet makes publicActive true again. The loop recognizes the old blueprint, writes the new fact data into that slot and records it as the current arrangement. The permanent-loss test calls NativeInvalidate before reconciliation, which removes the stale slot from the fixture.
- Why it blocks / does not: Permanent removal can leave a recoverable binding to the discarded companion’s arrangement.
- Requested correction: Clear the exact public-control slot when permanent loss is established, including on a bar native UI has not inspected. Preserve unrelated slots and same-pet temporary absence.
- Requested evidence: Leave slot 9 bound on an unselected party rider, clear its pet link and reconcile without calling native bad-slot cleanup. Verify the managed slot is cleared. Acquire a different supported pet and verify slot 9 stays empty.

**R4-F3** · P2 · defect · blocks readiness — Area suspension clears departed riders that its boundary observation skips
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1918`; acceptance: §4 city and interior transitions, §4 independent riders and player edits
- Trigger: A second rider leaves the travelling roster after a toolbar edit, then area suspension runs before the normal departure loop.
- Expected: Area suspension observes the latest arrangement of each current-world rider whose bar it is about to empty.
- Actual: ObserveArrangementsAtBoundary visits current candidates only. SuspendAreaControls then calls RemoveAllManagedFacts(true), which also empties still-observed former candidates and clears their observation records. For a rider already moved to reserve, this can interrupt the old index and destroy the only record of a newer move or removal.
- Why it blocks / does not: An area transition can restore a reserve rider’s old layout even though the save-header and normal departure paths now preserve the edit.
- Requested correction: Apply the current-world departure classification to observed former candidates before area suspension clears their slots. Use the same ownership checks as the save-header path.
- Requested evidence: Keep one pair mounted so area suspension runs. Move a second rider’s control from 9 to 3, move that rider to reserve and suspend before departure reconciliation. Verify the return restores only 3. Repeat with removal and verify it stays off-bar.

**R3-F1** · P2 · evidence_gap · blocks readiness — The cross-world regression switches players earlier than production does
- Location: `src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs:731`; acceptance: §4 world disposal and save/load, §3 stable actor identity
- Trigger: A load starts while the outgoing player object is still current and a control observation occurs before native player replacement.
- Expected: Once placement state is cleared for a selected load, an outgoing rider cannot write into it, including while native loading is replacing the player object.
- Actual: WrapLoadRoutine calls BeginLoadedWorld before the native iterator’s first MoveNext. The test switches Current to the incoming world before clearing the maps. Production Update and the IsBad observer can accept the outgoing rider as long as Game.Instance.Player still points to its world. An observation in that interval can repopulate the cleared actor-ID entry. The gate does not establish that the interval admits no such callback.
- Why it blocks / does not: The test verifies cleanup after the world switch but leaves the load-start boundary, where the maps are actually cleared, unverified.
- Requested correction: Bind acceptance of placement observations to the incoming world across the load boundary, or establish from the installed load and callback contracts that an outgoing observation in this interval is impossible.
- Requested evidence: Exercise production ordering: begin the load while the old player is current, attempt an update or slot invalidation on its rider, then install a distinct incoming player with a same-ID reserve rider. Verify no outgoing index appears on resave or party return, including for an incoming save with no row.

**R3-F2** · P2 · required_gap · blocks readiness — The owner playtest can classify a legal full attack as an extra attack
- Location: `docs/CITY-AND-PRONE-HANDOFF.md:323`; acceptance: §6 negative controls and native costs, §7 owner-verification deferral
- Trigger: The owner follows item 5 with an ordinary longbow user eligible for native multiple attacks or without a tracked arrow stack.
- Expected: The owner can distinguish an extra attack or resource charge from the weapon’s normal native routine in the selected fixture and mode.
- Actual: Item 5 requires one roll for several ordinary clicks and one arrow decrement per shot without specifying a single-attack setup or a tracked ammunition source. The repository’s ordinary longbow fixture explicitly varies Rapid Shot, BAB and Haste. A valid native full attack can produce multiple rolls for one action expenditure.
- Why it blocks / does not: The deferred test can report a failure for correct native attack resolution and cannot establish unchanged ammunition behavior without a matched baseline.
- Requested correction: Specify a verified single-attack setup where one roll is the criterion, or compare the whole native routine and action cost with the matched control. Compare ammunition or charge use only for a fixture that actually tracks it. Give the real-time observation a bounded stopping point.
- Requested evidence: Revised NOT RUN steps that identify the fixture, mode, expected routine and observable expenditure. A legal full attack should pass, while an additional routine or second action spend should fail.

**R4-F4** · P2 · required_gap · blocks readiness — The handoff attributes PASS to a failed pre-commit invocation
- Location: `docs/CITY-AND-PRONE-HANDOFF.md:205`; acceptance: §7 validation reporting, §8 durable handoff
- Trigger: A reader uses the validation table to identify which command passed on which source state.
- Expected: The handoff distinguishes the failed local run and retries from the successful controller verification.
- Actual: The validation table labels the local pre-commit Fast invocation PASS. The CLI transcript shows that invocation ending with a focused-step failure and exit code 1. There were subsequent SkipBuild attempts. The controller-bound execution on the committed candidate is the confirmed PASS.
- Why it blocks / does not: The mission requires an accurate durable record of commands and results. The candidate gate itself is valid.
- Requested correction: Attribute the PASS to the exact committed gate and record the earlier local failure and retries with their observed outcomes. Use the receipt’s 25.7-second duration for the gate.
- Requested evidence: A validation row consistent with the CLI transcript and the commit-bound controller receipt.

#### Prior finding assessments

- **R1-F3** — `fix_verified` — The IsBad observer locates the exact slot object on the current rider’s bar and records its index. The gate includes passing move-before-cleanup and first-manual-placement cases.
- **R2-F1** — `fix_rejected` — The old-data/new-fact regression passes. Header classification still diverges from the live writer when the fact remains active but ShouldLease has become false. That sequence loses the same kind-6 row.
- **R2-F2** — `fix_verified` — Header capture now observes still-observed departing riders before emitting rows. The committed gate covers unchanged, moved and removed arrangements between a reserve roster change and departure cleanup.
- **R2-F5** — `fix_verified` — The controller log identifies both commits, source blobs, compiler exits and matching 63-error multisets. The existing area-adapter failure is classified as requested.
- **R3-F1** — `fix_rejected` — The exact-instance checks protect the incoming world after player replacement. The regression switches worlds before clearing state, while production clears state at iterator entry. The outgoing observation window remains open in the reviewed source.
- **R3-F2** — `fix_rejected` — The requested wall, spent-action, range, melee and penalty steps are present. Their roll-count and ammunition criteria still need a matching native fixture so the owner can interpret the result correctly.

#### Evidence assessment

- **Inspected:** Controller mission, feature diff, changed-file list, developer report, CLI transcript, prior findings, gate receipt and log.; Repository instructions, validation policy, current handoffs, X01 compatibility notes, production changes, affected save/load and area-transition callers, attack routing, focused scripts and regression fixtures.
- **Executed verification:** Confirmed the snapshot HEAD and mission SHA-256. The supplied gate log hashes to fb211a29dd42a65b752e2ed188dac9bd8057e7c0810017c6ad6d9b25f9ed60d4, matching the controller receipt.; The transcript confirms the guarded push and focused verification of 29ffa3a5f0eda518e50fe4067d5170aed685248f. The gate ran Fast with Test-CityToolbarProneFocused.ps1, exited 0 in 25.7 seconds, and left the worktree unchanged.; The gate log records build success, 1438 component passes, 153 source-validation passes, 820 Kingmaker contract passes, 21 click-composition passes including both patch orders, 3 archive-bound passes and 22 six-pair adapter passes. The tested DLL SHA-256 was 8b3b2d443fe9f2ab47d0b9f6357c2edd89edf1d5aa1c5b12c7d55cc1fb372ba4, MVID 73058509-7479-497d-bc4b-f6017a8c9320.; The area-adapter compile exited 1 on both the fixed base and candidate. The log records 63 normalized compiler errors on each, with zero differences in either direction.
- **Source inspection only:** The capital projection adds the eligible owner’s existing reciprocal pet instance to the UI roster. The implementation does not mutate party membership, ownership or equipment.; The combat change carries the explicit player order on an already-unconscious target through intent validation and pair-command execution. The conscious-trip case has a source trace. Neither case has a candidate-bound native attack roll.; The toolbar failure sequences in the findings follow the production control flow. The passing service-double tests omit the specified event orders or eligibility check.
- **Required gaps:** Cover the difference between an active fact and a currently leaseable fact at the save header.; Preserve edits made immediately before lease loss or area suspension, and clear stale slots after confirmed permanent pet loss.; Account for placement observations between load start and player-world replacement.; Correct the owner combat pass criteria and the attribution of the local validation result.
- **Limitations:** Reviewer operations were read-only. Build and test results above come from the controller-bound execution record.; The area adapter’s in-world restore assertion remains unexecuted because the adapter does not compile on either tree.; No native transaction ran. Capital panel interaction, equipment changes and actual attack rolls remain unobserved on this DLL. The handoff identifies the missing candidate package, driver and reader prerequisites.

#### Summary

- **Delivered behavior:** Draft PR #57 adds rider-specific placement intent using existing save rows, projects eligible reserve owners’ persistent pets into the capital UI roster, and admits explicit mounted attacks on enemies already unconscious when ordered. The two city symptoms share a roster transition. Their underlying causes are the toolbar fact lifecycle and the native UI pet filter. The reported enemy’s exact posture and weapon remain unknown.
- **Scope reviewed:** Complete final mission for howardreith/KingmakerMountedCombat draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 29ffa3a5f0eda518e50fe4067d5170aed685248f, including all six pending prior findings.
- **Pending later-phase work:** none
- **Unresolved questions:** Whether the installed load iterator or its callbacks can observe the outgoing rider after placement state is cleared and before the new player becomes current.; Whether the original ranged report involved a conscious trip, an unconscious enemy, or a ground click on the prone model.
- **Remaining owner review:** After correction, verify the chosen slot and deliberate removal through capital visits, interiors and saves on a disposable fixture.; Verify reserve-pet selection, equipment interaction and selection fallback in the actual Inventory and Character panels.; Compare ordinary mounted and dismounted longbow attacks against standing, conscious-prone and unconscious enemies in RT and TB through combat-log resolution.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1fe93-848a-7a40-8d76-baca808a651e`, duration 373.1s, response sha256 `a567e1624e459079572b22a49142e4c6a0af6540644c1e1dd5b79ef7e2e8823e`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r04-eeef19`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "29ffa3a5f0eda518e50fe4067d5170aed685248f",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the submitted commit. The toolbar can still lose a chosen slot on save, reverse a deliberate removal, or reuse a stale binding after permanent companion loss. The load-boundary regression also skips an interval present in the production load sequence. The city roster projection and the explicit attack-order change have offline coverage. Their native acceptance remains deferred under the mission\u2019s documented prerequisites.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the snapshot HEAD and mission SHA-256. The supplied gate log hashes to fb211a29dd42a65b752e2ed188dac9bd8057e7c0810017c6ad6d9b25f9ed60d4, matching the controller receipt.",
      "The transcript confirms the guarded push and focused verification of 29ffa3a5f0eda518e50fe4067d5170aed685248f. The gate ran Fast with Test-CityToolbarProneFocused.ps1, exited 0 in 25.7 seconds, and left the worktree unchanged.",
      "The gate log records build success, 1438 component passes, 153 source-validation passes, 820 Kingmaker contract passes, 21 click-composition passes including both patch orders, 3 archive-bound passes and 22 six-pair adapter passes. The tested DLL SHA-256 was 8b3b2d443fe9f2ab47d0b9f6357c2edd89edf1d5aa1c5b12c7d55cc1fb372ba4, MVID 73058509-7479-497d-bc4b-f6017a8c9320.",
      "The area-adapter compile exited 1 on both the fixed base and candidate. The log records 63 normalized compiler errors on each, with zero differences in either direction."
    ],
    "inspected": [
      "Controller mission, feature diff, changed-file list, developer report, CLI transcript, prior findings, gate receipt and log.",
      "Repository instructions, validation policy, current handoffs, X01 compatibility notes, production changes, affected save/load and area-transition callers, attack routing, focused scripts and regression fixtures."
    ],
    "limitations": [
      "Reviewer operations were read-only. Build and test results above come from the controller-bound execution record.",
      "The area adapter\u2019s in-world restore assertion remains unexecuted because the adapter does not compile on either tree.",
      "No native transaction ran. Capital panel interaction, equipment changes and actual attack rolls remain unobserved on this DLL. The handoff identifies the missing candidate package, driver and reader prerequisites."
    ],
    "required_gaps": [
      "Cover the difference between an active fact and a currently leaseable fact at the save header.",
      "Preserve edits made immediately before lease loss or area suspension, and clear stale slots after confirmed permanent pet loss.",
      "Account for placement observations between load start and player-world replacement.",
      "Correct the owner combat pass criteria and the attribution of the local validation result."
    ],
    "source_inspection_only": [
      "The capital projection adds the eligible owner\u2019s existing reciprocal pet instance to the UI roster. The implementation does not mutate party membership, ownership or equipment.",
      "The combat change carries the explicit player order on an already-unconscious target through intent validation and pair-command execution. The conscious-trip case has a source trace. Neither case has a candidate-bound native attack roll.",
      "The toolbar failure sequences in the findings follow the production control flow. The passing service-double tests omit the specified event orders or eligibility check."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 temporary unavailability and save/load",
        "\u00a77 toolbar persistence"
      ],
      "actual": "ClassifyArrangementsForCapture records the slot as bound and exits the active-fact branch when its data is current. CaptureLiveSlots then skips it because ShouldLease is false. TryGetArrangement skips the continuously bound entry as well. Only kind 7 survives, which loads as an explicit off-bar choice. The test double\u2019s CaptureLive omits the production ShouldLease check.",
      "blocking_rationale": "Saving during temporary pet unavailability can permanently erase the chosen slot.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A rider retaining the same persistent pet link saves kind 6 at the chosen index and one kind 7 record, even if the public fact is about to be withdrawn.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2181
      },
      "priority": "P1",
      "requested_correction": "Classify the observed binding using the same eligibility conditions as the live writer. When the pet link proves temporary unavailability, carry the index as placement intent. Keep the callable-data and caster checks intact.",
      "requested_evidence": "With a dismounted party rider bound at 9, make Pet or its reciprocal link temporarily unresolved while PetUniqueId, the active fact and slot data remain intact. Save before the next control update, decode the rows, reload and restore the same pet. Verify one binding at 9. Repeat after a deliberate removal and verify marker-only persistence.",
      "title": "The save header drops a live slot when its pet becomes temporarily unavailable",
      "trigger": "The pet stops resolving before the next control update, and the save header runs while the rider\u2019s old public fact is still active."
    },
    {
      "acceptance_ids": [
        "\u00a74 respect deliberate removal",
        "\u00a74 temporary mount unavailability"
      ],
      "actual": "ReconcileFacts removes the undesired fact before sampling the player\u2019s last edit. The !controlLive branch of Reconcile marks the previously remembered index interrupted. When the pet resolves again, ResolveUnbound restores the removed control to that index.",
      "blocking_rationale": "The control reappears after the player deliberately removed it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The last arrangement on the still-live bar is observed before KMC withdraws its fact. A removal remains off-bar when the same pet returns.",
      "id": "R4-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 125
      },
      "priority": "P2",
      "requested_correction": "Observe the arrangement before withdrawing or replacing the public fact. Preserve a previously observed native invalidation while respecting an edit to a continuously live control.",
      "requested_evidence": "Remove the live control from slot 9, make the same pet unresolved before the next frame, reconcile, then restore the pet. Verify the slot stays empty through reconciliation and save/load. Keep the corresponding native-invalidation case returning to 9.",
      "title": "Losing the fact lease can undo a removal made since the last update",
      "trigger": "The player clears a live slot and the rider temporarily loses lease eligibility before another reconciliation observes the removal."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 stale binding cleanup"
      ],
      "actual": "When publicActive is false, the slot loop leaves the old ability object in place. Revalidate can retire the arrangement, but a later supported pet makes publicActive true again. The loop recognizes the old blueprint, writes the new fact data into that slot and records it as the current arrangement. The permanent-loss test calls NativeInvalidate before reconciliation, which removes the stale slot from the fixture.",
      "blocking_rationale": "Permanent removal can leave a recoverable binding to the discarded companion\u2019s arrangement.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Confirmed permanent pet loss clears the rider\u2019s managed binding as well as the remembered arrangement. A different pet cannot revive the old choice.",
      "id": "R4-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1831
      },
      "priority": "P2",
      "requested_correction": "Clear the exact public-control slot when permanent loss is established, including on a bar native UI has not inspected. Preserve unrelated slots and same-pet temporary absence.",
      "requested_evidence": "Leave slot 9 bound on an unselected party rider, clear its pet link and reconcile without calling native bad-slot cleanup. Verify the managed slot is cleared. Acquire a different supported pet and verify slot 9 stays empty.",
      "title": "A different pet can revive a binding left on an uninspected toolbar",
      "trigger": "A party rider permanently loses its pet while its toolbar retains the old slot object, then acquires another supported pet."
    },
    {
      "acceptance_ids": [
        "\u00a74 city and interior transitions",
        "\u00a74 independent riders and player edits"
      ],
      "actual": "ObserveArrangementsAtBoundary visits current candidates only. SuspendAreaControls then calls RemoveAllManagedFacts(true), which also empties still-observed former candidates and clears their observation records. For a rider already moved to reserve, this can interrupt the old index and destroy the only record of a newer move or removal.",
      "blocking_rationale": "An area transition can restore a reserve rider\u2019s old layout even though the save-header and normal departure paths now preserve the edit.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Area suspension observes the latest arrangement of each current-world rider whose bar it is about to empty.",
      "id": "R4-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1918
      },
      "priority": "P2",
      "requested_correction": "Apply the current-world departure classification to observed former candidates before area suspension clears their slots. Use the same ownership checks as the save-header path.",
      "requested_evidence": "Keep one pair mounted so area suspension runs. Move a second rider\u2019s control from 9 to 3, move that rider to reserve and suspend before departure reconciliation. Verify the return restores only 3. Repeat with removal and verify it stays off-bar.",
      "title": "Area suspension clears departed riders that its boundary observation skips",
      "trigger": "A second rider leaves the travelling roster after a toolbar edit, then area suspension runs before the normal departure loop."
    },
    {
      "acceptance_ids": [
        "\u00a74 world disposal and save/load",
        "\u00a73 stable actor identity"
      ],
      "actual": "WrapLoadRoutine calls BeginLoadedWorld before the native iterator\u2019s first MoveNext. The test switches Current to the incoming world before clearing the maps. Production Update and the IsBad observer can accept the outgoing rider as long as Game.Instance.Player still points to its world. An observation in that interval can repopulate the cleared actor-ID entry. The gate does not establish that the interval admits no such callback.",
      "blocking_rationale": "The test verifies cleanup after the world switch but leaves the load-start boundary, where the maps are actually cleared, unverified.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Once placement state is cleared for a selected load, an outgoing rider cannot write into it, including while native loading is replacing the player object.",
      "id": "R3-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/MountedPersistenceService.cs",
        "line": 731
      },
      "priority": "P2",
      "requested_correction": "Bind acceptance of placement observations to the incoming world across the load boundary, or establish from the installed load and callback contracts that an outgoing observation in this interval is impossible.",
      "requested_evidence": "Exercise production ordering: begin the load while the old player is current, attempt an update or slot invalidation on its rider, then install a distinct incoming player with a same-ID reserve rider. Verify no outgoing index appears on resave or party return, including for an incoming save with no row.",
      "title": "The cross-world regression switches players earlier than production does",
      "trigger": "A load starts while the outgoing player object is still current and a control observation occurs before native player replacement."
    },
    {
      "acceptance_ids": [
        "\u00a76 negative controls and native costs",
        "\u00a77 owner-verification deferral"
      ],
      "actual": "Item 5 requires one roll for several ordinary clicks and one arrow decrement per shot without specifying a single-attack setup or a tracked ammunition source. The repository\u2019s ordinary longbow fixture explicitly varies Rapid Shot, BAB and Haste. A valid native full attack can produce multiple rolls for one action expenditure.",
      "blocking_rationale": "The deferred test can report a failure for correct native attack resolution and cannot establish unchanged ammunition behavior without a matched baseline.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The owner can distinguish an extra attack or resource charge from the weapon\u2019s normal native routine in the selected fixture and mode.",
      "id": "R3-F2",
      "location": {
        "file": "docs/CITY-AND-PRONE-HANDOFF.md",
        "line": 323
      },
      "priority": "P2",
      "requested_correction": "Specify a verified single-attack setup where one roll is the criterion, or compare the whole native routine and action cost with the matched control. Compare ammunition or charge use only for a fixture that actually tracks it. Give the real-time observation a bounded stopping point.",
      "requested_evidence": "Revised NOT RUN steps that identify the fixture, mode, expected routine and observable expenditure. A legal full attack should pass, while an additional routine or second action spend should fail.",
      "title": "The owner playtest can classify a legal full attack as an extra attack",
      "trigger": "The owner follows item 5 with an ordinary longbow user eligible for native multiple attacks or without a tracked arrow stack."
    },
    {
      "acceptance_ids": [
        "\u00a77 validation reporting",
        "\u00a78 durable handoff"
      ],
      "actual": "The validation table labels the local pre-commit Fast invocation PASS. The CLI transcript shows that invocation ending with a focused-step failure and exit code 1. There were subsequent SkipBuild attempts. The controller-bound execution on the committed candidate is the confirmed PASS.",
      "blocking_rationale": "The mission requires an accurate durable record of commands and results. The candidate gate itself is valid.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The handoff distinguishes the failed local run and retries from the successful controller verification.",
      "id": "R4-F4",
      "location": {
        "file": "docs/CITY-AND-PRONE-HANDOFF.md",
        "line": 205
      },
      "priority": "P2",
      "requested_correction": "Attribute the PASS to the exact committed gate and record the earlier local failure and retries with their observed outcomes. Use the receipt\u2019s 25.7-second duration for the gate.",
      "requested_evidence": "A validation row consistent with the CLI transcript and the commit-bound controller receipt.",
      "title": "The handoff attributes PASS to a failed pre-commit invocation",
      "trigger": "A reader uses the validation table to identify which command passed on which source state."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "The IsBad observer locates the exact slot object on the current rider\u2019s bar and records its index. The gate includes passing move-before-cleanup and first-manual-placement cases."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R2-F1",
      "rationale": "The old-data/new-fact regression passes. Header classification still diverges from the live writer when the fact remains active but ShouldLease has become false. That sequence loses the same kind-6 row."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F2",
      "rationale": "Header capture now observes still-observed departing riders before emitting rows. The committed gate covers unchanged, moved and removed arrangements between a reserve roster change and departure cleanup."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R2-F5",
      "rationale": "The controller log identifies both commits, source blobs, compiler exits and matching 63-error multisets. The existing area-adapter failure is classified as requested."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F1",
      "rationale": "The exact-instance checks protect the incoming world after player replacement. The regression switches worlds before clearing state, while production clears state at iterator entry. The outgoing observation window remains open in the reviewed source."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R3-F2",
      "rationale": "The requested wall, spent-action, range, melee and penalty steps are present. Their roll-count and ammunition criteria still need a matching native fixture so the owner can interpret the result correctly."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r04-eeef19",
  "round": 4,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "Draft PR #57 adds rider-specific placement intent using existing save rows, projects eligible reserve owners\u2019 persistent pets into the capital UI roster, and admits explicit mounted attacks on enemies already unconscious when ordered. The two city symptoms share a roster transition. Their underlying causes are the toolbar fact lifecycle and the native UI pet filter. The reported enemy\u2019s exact posture and weapon remain unknown.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, verify the chosen slot and deliberate removal through capital visits, interiors and saves on a disposable fixture.",
      "Verify reserve-pet selection, equipment interaction and selection fallback in the actual Inventory and Character panels.",
      "Compare ordinary mounted and dismounted longbow attacks against standing, conscious-prone and unconscious enemies in RT and TB through combat-log resolution."
    ],
    "scope_reviewed": "Complete final mission for howardreith/KingmakerMountedCombat draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through 29ffa3a5f0eda518e50fe4067d5170aed685248f, including all six pending prior findings.",
    "unresolved_questions": [
      "Whether the installed load iterator or its callbacks can observe the outgoing rider after placement state is cleared and before the new player becomes current.",
      "Whether the original ranged report involved a conscious trip, an unconscious enemy, or a ground click on the prone model."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
