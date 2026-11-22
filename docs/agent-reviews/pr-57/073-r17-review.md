<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r17-review part=1/1 sha256=048e4942d43978e29efb640222eba5ba20c28ba3fcdcb9d30ac4352b1844ea43 -->
### Codex GPT-6.2 Astra — review — round 17 — candidate `d4b3c2d4f2`

**Decision:** `changes_required` — The focused gate passed on the reviewed commit, and the first automatic placement now records its companion immediately. Toolbar recovery still depends on which slot native UI reads first and whether a save precedes reconciliation. A read-only slot query can also reverse a deliberate removal, and an abandoned load can transfer a retired shortcut to a replacement companion. These source-level defects block readiness. The documented native playtest deferral is acceptable under the mission.

#### Findings

**R14-F2** · P2 · defect · blocks readiness — A native rebind can still promote the surviving legacy alias
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:126`; acceptance: §4 chosen toolbar slot, §4 temporary unavailability, §4 existing save compatibility
- Trigger: GetSlot empties the canonical shortcut at 4. Before KMC reconciles, CleanupBadSlots repairs the older Mount alias at 9 using a live fact of that blueprint.
- Expected: The canonical choice remains at 4. A native rebind of the surviving older alias preserves its status as a secondary copy.
- Actual: ApplySlotInvalidation sends the successful alias rebind to ObserveNativeCleanupOutcome. Its rebound branch calls ObserveBound at 9 after checking the companion. That replaces the interrupted choice at 4, so the next migration or save accepts 9.
- Why it blocks / does not: Native repair of an older binding can still move the player's shortcut without a toolbar edit.
- Requested correction: Carry the selected slot through the native rebind callback. Preserve alias-only migration and continue to recognize a rebind at the selected slot as live.
- Requested evidence: Start with the mixed bar at 4 and 9, empty 4 with GetSlot, then let CleanupBadSlots rebind the stale alias at 9 to a live shell fact. Check that reconciliation and a save/reload each produce one public shortcut at 4.

**R15-F1** · P2 · defect · blocks readiness — An already-live canonical copy still changes the archived slot
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2217`; acceptance: §4 chosen toolbar slot, §4 player moves, §7 toolbar persistence and intent
- Trigger: Use the existing StaleThenCurrentCanonical bar with stale data at 3 and current data at 9. Native GetSlot empties 3 immediately before the save.
- Expected: Capture before and after the read both preserve the first canonical slot at 3. A genuinely new player placement remains authoritative.
- Actual: IsStaleOrAlias returns false for the existing copy at 9 because it already holds the live fact's data. ObserveBindingAtBoundary consequently records 9 as a player placement, and LivePublicRows writes 9. The candidate's own save-first control for this same bar writes 3.
- Why it blocks / does not: Reading a stale slot changes what the next save restores even though the player made no edit.
- Requested correction: Distinguish a copy already present when the selected slot was emptied from a later player placement. Use that distinction in observation, reconciliation and capture.
- Requested evidence: Extend StaleThenCurrentCanonical by calling NativeInvalidateAt(3) before saving while 9 holds current data. Require one kind-6 row and one restored shortcut at 3. Retain the manual-move control.

**R17-F1** · P2 · defect · blocks readiness — An occupied chosen slot produces different live and saved arrangements
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1930`; acceptance: §4 occupied slots, §4 save/load, §7 toolbar persistence and intent
- Trigger: The player fills the interrupted slot at 4 while an older secondary binding survives at 9.
- Expected: The unrelated ability remains at 4, and the continuing game and cold-loaded save agree on the shortcut. The handoff's stated fallback would keep one copy at 9.
- Actual: Live reconciliation keeps the surviving copy at 9 when the interrupted index is occupied. Capture skips that copy as secondary, finds the occupant at 4 and calls GiveUpOccupiedSlot. The archive then carries only the placement marker, so cold load loses the shortcut that live reconciliation keeps.
- Why it blocks / does not: The same bar and companion produce different arrangements depending on whether the player saves before the next update.
- Requested correction: Apply one occupied-slot decision to live repair, archive capture and save-scope restoration.
- Requested evidence: After GetSlot empties 4 on MixedOlderBar, put Fireball at 4 and return the same pet. Compare update-first and save-first runs, including cold load and resave. Require identical public bindings and Fireball at 4.

**R17-F2** · P2 · defect · blocks readiness — A read-only IsBad query is recorded as a native slot removal
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2016`; acceptance: §4 deliberate removal, §4 temporary unavailability, §7 affected lifecycle paths
- Trigger: A caller inspects a stale public shortcut directly, then deliberately removes it before KMC's next update.
- Expected: A validity query leaves placement intent untouched. An observed native replacement or emptying updates it.
- Actual: Every true IsBad result outside the CleanupBadSlots bracket calls ApplySlotInvalidation with rebound=false. Finalize2ControlsScenario.F2SlotKey directly calls IsBad on the raw slot array. Its removal case reads F2Bar immediately before explicitly emptying the slot. If the binding is stale, that inspection marks its index interrupted, allowing the next update to restore the deliberately removed shortcut.
- Why it blocks / does not: A diagnostic read can change the toolbar behavior it is observing and reverse a removal.
- Requested correction: Bind invalidation to the actual GetSlot or CleanupBadSlots operation and its outcome. Keep a standalone IsBad query free of placement changes.
- Requested evidence: Query IsBad on a stale canonical slot with a live replacement fact, leave the slot in place, then deliberately remove it before reconciliation. Require a marker-only save and an empty slot after reload. Keep actual GetSlot emptying as the recovery control.

**R17-F3** · P2 · defect · blocks readiness — An abandoned load can restore a retired binding for a replacement companion
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2089`; acceptance: §4 genuine companion removal, §4 save/load, §7 affected lifecycle paths
- Trigger: A rider has A's recorded shortcut at 9 and now links to B. Native cleanup rebinds the stale slot while a selected load fences the outgoing player, then that load is abandoned.
- Expected: Abandoning the load preserves the confirmed retirement and clears the obsolete binding before B can adopt it.
- Actual: The fenced callback retires A's arrangement in the outgoing records and returns while the rebound shortcut remains on the bar. Abandon restores the retirement. On the next update there is no stored pet ID left to compare, and Reconcile records the live slot for B. ObserveBound then clears the retirement.
- Why it blocks / does not: The surviving world can give a replacement companion the previous companion's retired shortcut and persist it.
- Requested correction: Carry the cleanup required by a fenced retirement through abandonment and apply it to the surviving rider before ordinary bar observation.
- Requested evidence: Extend FencedRebindAbandoned with a direct change from A to B before the rebind. Abandon the load, reconcile and save. Require an empty old slot, no public row for B and an unchanged unrelated slot. Keep the same-A rebind and completed-load controls.

#### Prior finding assessments

- **R14-F2** — `fix_rejected` — The new regression passes for direct KMC migration and capture of a callable alias. Native CleanupBadSlots can also rebind that alias. Its callback still overwrites the interrupted canonical choice.
- **R15-F1** — `fix_rejected` — The partial-read regression passes when the surviving copy is stale. In the existing stale-at-3, live-at-9 fixture, the same partial read promotes the untouched live copy and changes the saved index.
- **R16-F1** — `fix_verified` — TryInitialPlacement now records the chosen index and PetUniqueId immediately after SetSlot. The candidate gate passed the update-first and save-first A-to-B cases and the same-A temporary-absence control.

#### Evidence assessment

- **Inspected:** The mission, AGENTS.md, validation policy, current handoffs, X01 compatibility notes, full 32-file feature diff, prior ledger, developer transcript, gate receipt and retained log.; The production paths for native slot reads and rebinds, archive capture, save leases, load abandonment and companion retirement. Compared them with the toolbar fixtures and the existing F2 diagnostic caller of IsBad.; The capital roster projection, mounted stock-click admission, pair command target checks, focused scripts, acceptance record and owner playtest.
- **Executed verification:** Confirmed snapshot HEAD and merge base with Git. Independently matched the mission SHA-256 and the focused log SHA-256 to the controller inputs. The feature diff passed git diff --check.; The developer transcript and controller receipt confirm Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 ran on d4b3c2d4f2789242c867a9c4366705c7ba0f6226. It passed in 47.4 seconds with exit code 0 and an unchanged worktree.; The retained log records 1,483 component passes, 153 source checks, 822 assembly contracts, 21 click-composition checks including both patch orders, 9 archive-codec checks and 22 six-pair adapter checks. All five focused children passed. The tested DLL had SHA-256 ca253f37c4fd60d7ace137fd058f7629214a8106c075a70ed0ffffe46adb1a4a and MVID 27fac4e2-b019-4413-a298-6118283a10cb.; The transcript separately records two pre-commit mutation runs with 1,479 passes and four failures each. The second exposed the replacement companion inheriting the automatic placement. The final gate records the corresponding regression passing.
- **Source inspection only:** The capital roster adapter projects the persistent pet instance from a listed owner's reciprocal link. Actual portrait selection, equipment interaction and selection fallback remain owner checks.; The mounted attack path carries an explicit order on an already unconscious enemy through admission and execution checks. A conscious trip was traced separately. Ordinary pointer input through an attack roll remains unobserved for this candidate.
- **Required gaps:** Demonstrate consistent placement across partial native cleanup, live secondary copies, occupied slots and save/load using the production placement decisions.; Verify that querying slot validity preserves a subsequent removal and that abandoning a load preserves a confirmed companion retirement. Bind the focused gate to the corrected commit.
- **Limitations:** Reviewer verification was read-only. The additional failure sequences below are source traces. The passing toolbar component tests substitute the native bar and parts of service orchestration.; The area-restore adapter produced the same 63 compiler diagnostics at base and candidate. Its behavioral assertion remains unexecuted.; Native gameplay was NOT RUN. The handoff identifies the unavailable commit-bound package, capital fixture and reader requirements, missing reserve-pet equipment driver and missing enemy-posture setup. No runtime transaction or restoration occurred.

#### Summary

- **Delivered behavior:** The two city issues share the capital roster transition as a trigger. The toolbar change preserves rider placement intent through temporary fact loss. The city UI change projects eligible owners' existing pets into the normal roster. The attack change carries a player's explicit order on an already unconscious enemy through the mounted command path. The remaining implementation defects concern toolbar recovery.
- **Scope reviewed:** Complete final mission for enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c. The transcript records branch codex/mounted-city-and-prone-fixes at the reviewed feature commit and remote head d4b3c2d4f2789242c867a9c4366705c7ba0f6226. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)
- **Pending later-phase work:** none
- **Unresolved questions:** The original weapon, target consciousness and exact click location remain unidentified. The conscious-prone trace and the unconscious-target fix still await matched gameplay observations.
- **Remaining owner review:** After correction, check nondefault slots, moves and removals through capital travel, interiors, forced dismount and save/load with two riders.; Verify the selected reserve pet's identity, statistics, rename display and permitted equipment changes in the capital Inventory and Character screens.; Compare mounted and dismounted longbow shots against standing, consciously tripped and already unconscious enemies at near and normal range in RT and TB. Confirm attack rolls, modifiers, action expenditure and the documented negative controls.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff71-9c9b-74d0-af5e-fb2868e6810f`, duration 756.4s, response sha256 `2208d27d13ade7dc1347b41db79c4f118b3ebc413704404a8ecc01766a09cd97`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r17-0475de`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "d4b3c2d4f2789242c867a9c4366705c7ba0f6226",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the reviewed commit, and the first automatic placement now records its companion immediately. Toolbar recovery still depends on which slot native UI reads first and whether a save precedes reconciliation. A read-only slot query can also reverse a deliberate removal, and an abandoned load can transfer a retired shortcut to a replacement companion. These source-level defects block readiness. The documented native playtest deferral is acceptable under the mission.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed snapshot HEAD and merge base with Git. Independently matched the mission SHA-256 and the focused log SHA-256 to the controller inputs. The feature diff passed git diff --check.",
      "The developer transcript and controller receipt confirm Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 ran on d4b3c2d4f2789242c867a9c4366705c7ba0f6226. It passed in 47.4 seconds with exit code 0 and an unchanged worktree.",
      "The retained log records 1,483 component passes, 153 source checks, 822 assembly contracts, 21 click-composition checks including both patch orders, 9 archive-codec checks and 22 six-pair adapter checks. All five focused children passed. The tested DLL had SHA-256 ca253f37c4fd60d7ace137fd058f7629214a8106c075a70ed0ffffe46adb1a4a and MVID 27fac4e2-b019-4413-a298-6118283a10cb.",
      "The transcript separately records two pre-commit mutation runs with 1,479 passes and four failures each. The second exposed the replacement companion inheriting the automatic placement. The final gate records the corresponding regression passing."
    ],
    "inspected": [
      "The mission, AGENTS.md, validation policy, current handoffs, X01 compatibility notes, full 32-file feature diff, prior ledger, developer transcript, gate receipt and retained log.",
      "The production paths for native slot reads and rebinds, archive capture, save leases, load abandonment and companion retirement. Compared them with the toolbar fixtures and the existing F2 diagnostic caller of IsBad.",
      "The capital roster projection, mounted stock-click admission, pair command target checks, focused scripts, acceptance record and owner playtest."
    ],
    "limitations": [
      "Reviewer verification was read-only. The additional failure sequences below are source traces. The passing toolbar component tests substitute the native bar and parts of service orchestration.",
      "The area-restore adapter produced the same 63 compiler diagnostics at base and candidate. Its behavioral assertion remains unexecuted.",
      "Native gameplay was NOT RUN. The handoff identifies the unavailable commit-bound package, capital fixture and reader requirements, missing reserve-pet equipment driver and missing enemy-posture setup. No runtime transaction or restoration occurred."
    ],
    "required_gaps": [
      "Demonstrate consistent placement across partial native cleanup, live secondary copies, occupied slots and save/load using the production placement decisions.",
      "Verify that querying slot validity preserves a subsequent removal and that abandoning a load preserves a confirmed companion retirement. Bind the focused gate to the corrected commit."
    ],
    "source_inspection_only": [
      "The capital roster adapter projects the persistent pet instance from a listed owner's reciprocal link. Actual portrait selection, equipment interaction and selection fallback remain owner checks.",
      "The mounted attack path carries an explicit order on an already unconscious enemy through admission and execution checks. A conscious trip was traced separately. Ordinary pointer input through an attack roll remains unobserved for this candidate."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 temporary unavailability",
        "\u00a74 existing save compatibility"
      ],
      "actual": "ApplySlotInvalidation sends the successful alias rebind to ObserveNativeCleanupOutcome. Its rebound branch calls ObserveBound at 9 after checking the companion. That replaces the interrupted choice at 4, so the next migration or save accepts 9.",
      "blocking_rationale": "Native repair of an older binding can still move the player's shortcut without a toolbar edit.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The canonical choice remains at 4. A native rebind of the surviving older alias preserves its status as a secondary copy.",
      "id": "R14-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 126
      },
      "priority": "P2",
      "requested_correction": "Carry the selected slot through the native rebind callback. Preserve alias-only migration and continue to recognize a rebind at the selected slot as live.",
      "requested_evidence": "Start with the mixed bar at 4 and 9, empty 4 with GetSlot, then let CleanupBadSlots rebind the stale alias at 9 to a live shell fact. Check that reconciliation and a save/reload each produce one public shortcut at 4.",
      "title": "A native rebind can still promote the surviving legacy alias",
      "trigger": "GetSlot empties the canonical shortcut at 4. Before KMC reconciles, CleanupBadSlots repairs the older Mount alias at 9 using a live fact of that blueprint."
    },
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 player moves",
        "\u00a77 toolbar persistence and intent"
      ],
      "actual": "IsStaleOrAlias returns false for the existing copy at 9 because it already holds the live fact's data. ObserveBindingAtBoundary consequently records 9 as a player placement, and LivePublicRows writes 9. The candidate's own save-first control for this same bar writes 3.",
      "blocking_rationale": "Reading a stale slot changes what the next save restores even though the player made no edit.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Capture before and after the read both preserve the first canonical slot at 3. A genuinely new player placement remains authoritative.",
      "id": "R15-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2217
      },
      "priority": "P2",
      "requested_correction": "Distinguish a copy already present when the selected slot was emptied from a later player placement. Use that distinction in observation, reconciliation and capture.",
      "requested_evidence": "Extend StaleThenCurrentCanonical by calling NativeInvalidateAt(3) before saving while 9 holds current data. Require one kind-6 row and one restored shortcut at 3. Retain the manual-move control.",
      "title": "An already-live canonical copy still changes the archived slot",
      "trigger": "Use the existing StaleThenCurrentCanonical bar with stale data at 3 and current data at 9. Native GetSlot empties 3 immediately before the save."
    },
    {
      "acceptance_ids": [
        "\u00a74 occupied slots",
        "\u00a74 save/load",
        "\u00a77 toolbar persistence and intent"
      ],
      "actual": "Live reconciliation keeps the surviving copy at 9 when the interrupted index is occupied. Capture skips that copy as secondary, finds the occupant at 4 and calls GiveUpOccupiedSlot. The archive then carries only the placement marker, so cold load loses the shortcut that live reconciliation keeps.",
      "blocking_rationale": "The same bar and companion produce different arrangements depending on whether the player saves before the next update.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The unrelated ability remains at 4, and the continuing game and cold-loaded save agree on the shortcut. The handoff's stated fallback would keep one copy at 9.",
      "id": "R17-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1930
      },
      "priority": "P2",
      "requested_correction": "Apply one occupied-slot decision to live repair, archive capture and save-scope restoration.",
      "requested_evidence": "After GetSlot empties 4 on MixedOlderBar, put Fireball at 4 and return the same pet. Compare update-first and save-first runs, including cold load and resave. Require identical public bindings and Fireball at 4.",
      "title": "An occupied chosen slot produces different live and saved arrangements",
      "trigger": "The player fills the interrupted slot at 4 while an older secondary binding survives at 9."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 temporary unavailability",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "Every true IsBad result outside the CleanupBadSlots bracket calls ApplySlotInvalidation with rebound=false. Finalize2ControlsScenario.F2SlotKey directly calls IsBad on the raw slot array. Its removal case reads F2Bar immediately before explicitly emptying the slot. If the binding is stale, that inspection marks its index interrupted, allowing the next update to restore the deliberately removed shortcut.",
      "blocking_rationale": "A diagnostic read can change the toolbar behavior it is observing and reverse a removal.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A validity query leaves placement intent untouched. An observed native replacement or emptying updates it.",
      "id": "R17-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2016
      },
      "priority": "P2",
      "requested_correction": "Bind invalidation to the actual GetSlot or CleanupBadSlots operation and its outcome. Keep a standalone IsBad query free of placement changes.",
      "requested_evidence": "Query IsBad on a stale canonical slot with a live replacement fact, leave the slot in place, then deliberately remove it before reconciliation. Require a marker-only save and an empty slot after reload. Keep actual GetSlot emptying as the recovery control.",
      "title": "A read-only IsBad query is recorded as a native slot removal",
      "trigger": "A caller inspects a stale public shortcut directly, then deliberately removes it before KMC's next update."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "The fenced callback retires A's arrangement in the outgoing records and returns while the rebound shortcut remains on the bar. Abandon restores the retirement. On the next update there is no stored pet ID left to compare, and Reconcile records the live slot for B. ObserveBound then clears the retirement.",
      "blocking_rationale": "The surviving world can give a replacement companion the previous companion's retired shortcut and persist it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Abandoning the load preserves the confirmed retirement and clears the obsolete binding before B can adopt it.",
      "id": "R17-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2089
      },
      "priority": "P2",
      "requested_correction": "Carry the cleanup required by a fenced retirement through abandonment and apply it to the surviving rider before ordinary bar observation.",
      "requested_evidence": "Extend FencedRebindAbandoned with a direct change from A to B before the rebind. Abandon the load, reconcile and save. Require an empty old slot, no public row for B and an unchanged unrelated slot. Keep the same-A rebind and completed-load controls.",
      "title": "An abandoned load can restore a retired binding for a replacement companion",
      "trigger": "A rider has A's recorded shortcut at 9 and now links to B. Native cleanup rebinds the stale slot while a selected load fences the outgoing player, then that load is abandoned."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R14-F2",
      "rationale": "The new regression passes for direct KMC migration and capture of a callable alias. Native CleanupBadSlots can also rebind that alias. Its callback still overwrites the interrupted canonical choice."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R15-F1",
      "rationale": "The partial-read regression passes when the surviving copy is stale. In the existing stale-at-3, live-at-9 fixture, the same partial read promotes the untouched live copy and changes the saved index."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R16-F1",
      "rationale": "TryInitialPlacement now records the chosen index and PetUniqueId immediately after SetSlot. The candidate gate passed the update-first and save-first A-to-B cases and the same-A temporary-absence control."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r17-0475de",
  "round": 17,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The two city issues share the capital roster transition as a trigger. The toolbar change preserves rider placement intent through temporary fact loss. The city UI change projects eligible owners' existing pets into the normal roster. The attack change carries a player's explicit order on an already unconscious enemy through the mounted command path. The remaining implementation defects concern toolbar recovery.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, check nondefault slots, moves and removals through capital travel, interiors, forced dismount and save/load with two riders.",
      "Verify the selected reserve pet's identity, statistics, rename display and permitted equipment changes in the capital Inventory and Character screens.",
      "Compare mounted and dismounted longbow shots against standing, consciously tripped and already unconscious enemies at near and normal range in RT and TB. Confirm attack rolls, modifiers, action expenditure and the documented negative controls."
    ],
    "scope_reviewed": "Complete final mission for enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c. The transcript records branch codex/mounted-city-and-prone-fixes at the reviewed feature commit and remote head d4b3c2d4f2789242c867a9c4366705c7ba0f6226. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)",
    "unresolved_questions": [
      "The original weapon, target consciousness and exact click location remain unidentified. The conscious-prone trace and the unconscious-target fix still await matched gameplay observations."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
