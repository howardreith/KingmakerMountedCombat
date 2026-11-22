<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r20-review part=1/1 sha256=c96738da023281a75949f50c0f71784b58017e78d8dfbe237cd3a51f38bdafb7 -->
### Codex GPT-6.2 Astra — review — round 20 — candidate `36f60e8130`

**Decision:** `changes_required` — The focused gate passed on the requested commit. Four toolbar defects still block readiness. A native rebind can leave duplicate shortcuts or undo a player’s move, an immediate save can lose a completed slot cleanup, and the copy registry can retain discarded slots after a rider leaves. The documented native playtest deferral is acceptable under the mission.

#### Findings

**R19-F2** · P2 · defect · blocks readiness — Rebinding a moved shortcut lets a lower copy take its place
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2249`; acceptance: §4 player moves, §4 no duplicate shortcuts, §4 save/load
- Trigger: Native empties 3, the player moves the copy at 9 to 11 while 7 survives, and native rebinds the moved slot before KMC reconciles or saves.
- Expected: The player’s moved binding at 11 remains the only shortcut and the only saved public slot.
- Actual: When native rebinds 11 to the current fact, the report correctly records the player’s choice at 11. `TrackCopies` then calls `ForgetCopies(id)`, losing the identity of the surviving copy at 7. Live repair sees two ordinary canonical bindings and leaves both. Capture selects 7 and a cold load restores the wrong slot.
- Why it blocks / does not: A supported native repair can reverse a deliberate move in the save and leave two callable shortcuts in the continuing game.
- Requested correction: Keep the identities of surviving secondary slots when native rebinds the player’s effective slot. Release each identity when its copy is removed or replaced.
- Requested evidence: Extend `MoveBeyondSurvivingCopy` with a live replacement fact and a `CleanupBadSlots` pass that rebinds the stale controls at 7 and 11. Exercise save-first and update-first order. Require one shortcut and one public row at 11 through cold restore.

**R18-F1** · P2 · defect · blocks readiness — Two native rebinds leave duplicate shortcuts beside an occupied slot
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2041`; acceptance: §4 occupied slots, §4 no duplicate shortcuts, §4 deliberate removal
- Trigger: The original choice at 3 is occupied and `CleanupBadSlots` rebinds both surviving copies before the next KMC update.
- Expected: Fireball stays at 3. Exactly one fallback remains at 7, and removing it leaves only the placement marker.
- Actual: Settling the rebind at 7 records it as live and clears `Interrupted`. The report for 9 then fails the selected-index guard because 7 is first. Native still writes a new slot object at 9. The registry holds the old object, so reconciliation no longer recognizes 9 as a copy. Both shortcuts survive, and removing 7 promotes 9.
- Why it blocks / does not: The player cannot reliably remove the fallback, and the live toolbar can contain two callable controls where the archive represents one.
- Requested correction: Continue tracking writes to already identified copies after a fallback becomes live. A later rebind in the same pass must preserve that slot’s secondary status until repair removes it.
- Requested evidence: Use stale canonical bindings at 3, 7 and 9. Empty 3, place Fireball there, then rebind both 7 and 9 in one native cleanup pass. After two updates require only 7. Remove it and verify a marker-only save and reload.

**R17-F2** · P2 · defect · blocks readiness — An immediate save misses a slot native emptied before throwing
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2734`; acceptance: §4 temporary unavailability, §4 save/load, §7 affected lifecycle paths
- Trigger: A cleanup pass empties the sole chosen slot at 9, throws later, and the native save header runs before another slot operation or control update.
- Expected: A slot replacement completed before an exception is reflected in the next archive, including when the header is the first subsequent boundary.
- Actual: The last report of the aborted pass is settled at the next slot-operation prefix or `Update`. `CapturePersistentSlots` can run first. Its boundary observation sees an empty bar and a previously live arrangement, so it records deliberate removal. The header writes only kind 7. Recovering the native write later cannot repair that archive.
- Why it blocks / does not: Reloading this save permanently loses the player’s chosen shortcut.
- Requested correction: Account for a completed pending native write before archive classification reads the bar. Preserve the distinction between that write and a report followed by a player removal.
- Requested evidence: In `ExceptionalNativeOperations`, save immediately after the pass empties 9 and throws on a later slot, before any frame or `GetSlot`. Require kind 6 at 9, one kind 7 and a single shortcut at 9 after reload. Keep the no-write removal control marker-only.

**R20-F1** · P2 · defect · blocks readiness — The copy registry retains a departed rider’s discarded slots
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1831`; acceptance: §4 genuine companion removal, §4 discarded entity cleanup, §7 affected lifecycle paths
- Trigger: A rider leaves the candidate roster after native records a surviving copy and before reconciliation removes it.
- Expected: Once KMC clears a departing rider’s bar, it releases the old slot objects. A temporary departure can retain the chosen index and pet identity.
- Actual: `recordedCopies` strongly holds `MechanicActionBarSlot` objects, including their unit and ability data. Departure clears the bar and removes the rider from `observedUnits` and `observedWorlds`, but leaves its copy list in the dictionary. The departed rider no longer passes through the per-candidate `ForgetCopies` call. A continuing player world keeps those discarded objects reachable.
- Why it blocks / does not: The new bookkeeping retains native references after the rider’s required cleanup.
- Requested correction: Prune the departed rider’s native copy references when its bindings are cleared, or use identities that cannot keep discarded slots alive. Preserve the placement information needed for a reserve rider’s return.
- Requested evidence: Record a copy, remove the rider from the roster before repair, and keep the same player world active. Verify the registry releases the old slot. Include a reserve-return control that restores the chosen index once.

#### Prior finding assessments

- **R15-F1** — `fix_verified` — An interrupted arrangement accepts the out-of-order copy report and removes the exact index/object pair. The 3, 9, 7 regression passed in both save-first and update-first order, including a fresh placement at 9.
- **R17-F1** — `fix_verified` — `OwnerInterruptedChoice` reads the outgoing arrangement while the load fence holds it. The abandonment regression preserved Fireball at 4 and restored the fallback at 9 through save and reload.
- **R17-F2** — `fix_rejected` — A subsequent successful call now owns its own report, and an update recovers a write completed before an exception. The regression advances a frame before saving. The archive header can still classify that same completed write as removal if it runs first.
- **R18-F1** — `fix_rejected` — The repair loop removes both initially identified live copies except the selected fallback. Consecutive native rebinds can change the second copy’s object after the observer stops tracking it, leaving the original duplicate-and-removal failure.
- **R19-F1** — `fix_verified` — `PromoteFallback` records the surviving alias before KMC’s own unavailable-control migration empties it. The update-first and save-first cases kept Fireball at 4 and restored the shortcut at 9.
- **R19-F2** — `fix_rejected` — Effective binding selection recognizes the move to 11 while the registry identifies 7 as secondary. A native rebind of 11 clears that registry before repair or capture, allowing 7 to win again.
- **R19-F3** — `fix_verified` — A confirmed emptying of a fresh canonical placement can establish its index. Retirement suppression is scoped to the native operation. The focused cases recovered 9 with no copies and after companion replacement, while retaining the stale-batch control.

#### Evidence assessment

- **Inspected:** The controller-owned mission, all 34 changed files in the feature diff, repository instructions, validation policy, current handoff, X01 compatibility notes, developer report, tool transcript and prior findings ledger.; The native toolbar patch callbacks, placement and copy records, selected-load fence, archive header, save leases, area replay, roster departure and their component doubles.; The capital roster projection, stock attack admission and pair command, focused scripts, assembly contracts and acceptance record.
- **Executed verification:** The controller transcript and receipt confirm `Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1` on 36f60e81. It exited 0 in 22.8 seconds with the worktree unchanged. I verified the receipt’s log SHA-256 against the supplied log.; The gate recorded 1501/0 component checks, 153/0 source checks and 825/0 Kingmaker assembly contracts. All seven new toolbar regressions passed in the component harness.; The focused wrapper passed all six children. Click composition passed 21/0, including both patch orders with Tabletop Rules Expansion 0.0.191. Archive bounds passed 9/0, the six-pair adapter passed 22/0 and target-liveness source checks passed 3/0. The area-adapter comparison found the same 63 compiler diagnostics on base and candidate. That adapter did not execute.; The gate identified the product DLL as SHA-256 d9654f309817f6eef67e0046217d1211fdbb6147dac553d412e265c878e24810, MVID 758dfdd9-6728-4c59-8904-5fcf410d1143. My read-only checks confirmed HEAD, the merge base, the mission hash and a clean `git diff --check`.
- **Source inspection only:** The capital projection reads the listed owner’s persistent pet and reciprocal master link. The two city reports share the capital roster change as a trigger. Toolbar loss follows withdrawal of a leased fact, while pet omission follows the native UI roster’s in-game pet filter.; The combat change admits an explicit mounted order on an enemy already unconscious. The handoff traces a conscious tripped enemy separately and identifies no mounted-specific posture check. Actual clicks, equipment interactions, attack rolls and costs remain owner observations.
- **Required gaps:** Preserve the effective shortcut through consecutive native rebinds, including a player move past a surviving copy.; Carry a completed native emptying into a save whose header precedes the next control update.; Release native copy references when their rider leaves and its bar is cleared.
- **Limitations:** The toolbar tests substitute native writes and mirror parts of the service. The new findings are production control-flow traces. The supplied gate did not execute those sequences.; Native UI and combat cases were NOT RUN. The handoff identifies the missing commit-bound package, capital reader and fixture lineage, reserve-pet panel driver, and enemy-posture setup. No native transaction ran or required restoration.

#### Summary

- **Delivered behavior:** The candidate records toolbar placement across temporary absence using the existing save rows, projects eligible owners’ persistent pets into the capital roster, and carries an explicit order on an already-unconscious target through mounted attack admission. The outstanding defects are in toolbar persistence and cleanup.
- **Scope reviewed:** Final review of the complete mission on `codex/mounted-city-and-prone-fixes` from dbf63702. The controller transcript identifies 36f60e81 as the reviewed feature commit and latest verified branch push for [draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57).
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the weapon, whether the prone enemy was conscious, or which transition first removed the toolbar shortcut.
- **Remaining owner review:** Exercise a nondefault shortcut, deliberate removal and a second rider across capital visits, interiors, forced dismount and saves inside and outside the city.; Select a reserve owner’s pet in Inventory and Character, verify identity and equipment changes, then reopen and reload to check selection and portrait count.; Compare ordinary mounted and dismounted longbow clicks against standing, conscious-tripped and already-unconscious enemies at near and normal firing distances in real time and turn based. Observe the rolls, modifiers, action costs, blocked sightline and out-of-range approach.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a20039-16ff-7541-bc38-dd3215f5a314`, duration 794.6s, response sha256 `201d1751c589c5b73171b30417b8851d6bbad551da3d337197d21409092db5e8`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r20-a38f1e`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "36f60e813045bae91311905d1a8509ff49b2de54",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit. Four toolbar defects still block readiness. A native rebind can leave duplicate shortcuts or undo a player\u2019s move, an immediate save can lose a completed slot cleanup, and the copy registry can retain discarded slots after a rider leaves. The documented native playtest deferral is acceptable under the mission.",
  "evidence_assessment": {
    "executed_verification": [
      "The controller transcript and receipt confirm `Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1` on 36f60e81. It exited 0 in 22.8 seconds with the worktree unchanged. I verified the receipt\u2019s log SHA-256 against the supplied log.",
      "The gate recorded 1501/0 component checks, 153/0 source checks and 825/0 Kingmaker assembly contracts. All seven new toolbar regressions passed in the component harness.",
      "The focused wrapper passed all six children. Click composition passed 21/0, including both patch orders with Tabletop Rules Expansion 0.0.191. Archive bounds passed 9/0, the six-pair adapter passed 22/0 and target-liveness source checks passed 3/0. The area-adapter comparison found the same 63 compiler diagnostics on base and candidate. That adapter did not execute.",
      "The gate identified the product DLL as SHA-256 d9654f309817f6eef67e0046217d1211fdbb6147dac553d412e265c878e24810, MVID 758dfdd9-6728-4c59-8904-5fcf410d1143. My read-only checks confirmed HEAD, the merge base, the mission hash and a clean `git diff --check`."
    ],
    "inspected": [
      "The controller-owned mission, all 34 changed files in the feature diff, repository instructions, validation policy, current handoff, X01 compatibility notes, developer report, tool transcript and prior findings ledger.",
      "The native toolbar patch callbacks, placement and copy records, selected-load fence, archive header, save leases, area replay, roster departure and their component doubles.",
      "The capital roster projection, stock attack admission and pair command, focused scripts, assembly contracts and acceptance record."
    ],
    "limitations": [
      "The toolbar tests substitute native writes and mirror parts of the service. The new findings are production control-flow traces. The supplied gate did not execute those sequences.",
      "Native UI and combat cases were NOT RUN. The handoff identifies the missing commit-bound package, capital reader and fixture lineage, reserve-pet panel driver, and enemy-posture setup. No native transaction ran or required restoration."
    ],
    "required_gaps": [
      "Preserve the effective shortcut through consecutive native rebinds, including a player move past a surviving copy.",
      "Carry a completed native emptying into a save whose header precedes the next control update.",
      "Release native copy references when their rider leaves and its bar is cleared."
    ],
    "source_inspection_only": [
      "The capital projection reads the listed owner\u2019s persistent pet and reciprocal master link. The two city reports share the capital roster change as a trigger. Toolbar loss follows withdrawal of a leased fact, while pet omission follows the native UI roster\u2019s in-game pet filter.",
      "The combat change admits an explicit mounted order on an enemy already unconscious. The handoff traces a conscious tripped enemy separately and identifies no mounted-specific posture check. Actual clicks, equipment interactions, attack rolls and costs remain owner observations."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 no duplicate shortcuts",
        "\u00a74 save/load"
      ],
      "actual": "When native rebinds 11 to the current fact, the report correctly records the player\u2019s choice at 11. `TrackCopies` then calls `ForgetCopies(id)`, losing the identity of the surviving copy at 7. Live repair sees two ordinary canonical bindings and leaves both. Capture selects 7 and a cold load restores the wrong slot.",
      "blocking_rationale": "A supported native repair can reverse a deliberate move in the save and leave two callable shortcuts in the continuing game.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The player\u2019s moved binding at 11 remains the only shortcut and the only saved public slot.",
      "id": "R19-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2249
      },
      "priority": "P2",
      "requested_correction": "Keep the identities of surviving secondary slots when native rebinds the player\u2019s effective slot. Release each identity when its copy is removed or replaced.",
      "requested_evidence": "Extend `MoveBeyondSurvivingCopy` with a live replacement fact and a `CleanupBadSlots` pass that rebinds the stale controls at 7 and 11. Exercise save-first and update-first order. Require one shortcut and one public row at 11 through cold restore.",
      "title": "Rebinding a moved shortcut lets a lower copy take its place",
      "trigger": "Native empties 3, the player moves the copy at 9 to 11 while 7 survives, and native rebinds the moved slot before KMC reconciles or saves."
    },
    {
      "acceptance_ids": [
        "\u00a74 occupied slots",
        "\u00a74 no duplicate shortcuts",
        "\u00a74 deliberate removal"
      ],
      "actual": "Settling the rebind at 7 records it as live and clears `Interrupted`. The report for 9 then fails the selected-index guard because 7 is first. Native still writes a new slot object at 9. The registry holds the old object, so reconciliation no longer recognizes 9 as a copy. Both shortcuts survive, and removing 7 promotes 9.",
      "blocking_rationale": "The player cannot reliably remove the fallback, and the live toolbar can contain two callable controls where the archive represents one.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Fireball stays at 3. Exactly one fallback remains at 7, and removing it leaves only the placement marker.",
      "id": "R18-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2041
      },
      "priority": "P2",
      "requested_correction": "Continue tracking writes to already identified copies after a fallback becomes live. A later rebind in the same pass must preserve that slot\u2019s secondary status until repair removes it.",
      "requested_evidence": "Use stale canonical bindings at 3, 7 and 9. Empty 3, place Fireball there, then rebind both 7 and 9 in one native cleanup pass. After two updates require only 7. Remove it and verify a marker-only save and reload.",
      "title": "Two native rebinds leave duplicate shortcuts beside an occupied slot",
      "trigger": "The original choice at 3 is occupied and `CleanupBadSlots` rebinds both surviving copies before the next KMC update."
    },
    {
      "acceptance_ids": [
        "\u00a74 temporary unavailability",
        "\u00a74 save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "The last report of the aborted pass is settled at the next slot-operation prefix or `Update`. `CapturePersistentSlots` can run first. Its boundary observation sees an empty bar and a previously live arrangement, so it records deliberate removal. The header writes only kind 7. Recovering the native write later cannot repair that archive.",
      "blocking_rationale": "Reloading this save permanently loses the player\u2019s chosen shortcut.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A slot replacement completed before an exception is reflected in the next archive, including when the header is the first subsequent boundary.",
      "id": "R17-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2734
      },
      "priority": "P2",
      "requested_correction": "Account for a completed pending native write before archive classification reads the bar. Preserve the distinction between that write and a report followed by a player removal.",
      "requested_evidence": "In `ExceptionalNativeOperations`, save immediately after the pass empties 9 and throws on a later slot, before any frame or `GetSlot`. Require kind 6 at 9, one kind 7 and a single shortcut at 9 after reload. Keep the no-write removal control marker-only.",
      "title": "An immediate save misses a slot native emptied before throwing",
      "trigger": "A cleanup pass empties the sole chosen slot at 9, throws later, and the native save header runs before another slot operation or control update."
    },
    {
      "acceptance_ids": [
        "\u00a74 genuine companion removal",
        "\u00a74 discarded entity cleanup",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "`recordedCopies` strongly holds `MechanicActionBarSlot` objects, including their unit and ability data. Departure clears the bar and removes the rider from `observedUnits` and `observedWorlds`, but leaves its copy list in the dictionary. The departed rider no longer passes through the per-candidate `ForgetCopies` call. A continuing player world keeps those discarded objects reachable.",
      "blocking_rationale": "The new bookkeeping retains native references after the rider\u2019s required cleanup.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once KMC clears a departing rider\u2019s bar, it releases the old slot objects. A temporary departure can retain the chosen index and pet identity.",
      "id": "R20-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1831
      },
      "priority": "P2",
      "requested_correction": "Prune the departed rider\u2019s native copy references when its bindings are cleared, or use identities that cannot keep discarded slots alive. Preserve the placement information needed for a reserve rider\u2019s return.",
      "requested_evidence": "Record a copy, remove the rider from the roster before repair, and keep the same player world active. Verify the registry releases the old slot. Include a reserve-return control that restores the chosen index once.",
      "title": "The copy registry retains a departed rider\u2019s discarded slots",
      "trigger": "A rider leaves the candidate roster after native records a surviving copy and before reconciliation removes it."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R15-F1",
      "rationale": "An interrupted arrangement accepts the out-of-order copy report and removes the exact index/object pair. The 3, 9, 7 regression passed in both save-first and update-first order, including a fresh placement at 9."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R17-F1",
      "rationale": "`OwnerInterruptedChoice` reads the outgoing arrangement while the load fence holds it. The abandonment regression preserved Fireball at 4 and restored the fallback at 9 through save and reload."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R17-F2",
      "rationale": "A subsequent successful call now owns its own report, and an update recovers a write completed before an exception. The regression advances a frame before saving. The archive header can still classify that same completed write as removal if it runs first."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R18-F1",
      "rationale": "The repair loop removes both initially identified live copies except the selected fallback. Consecutive native rebinds can change the second copy\u2019s object after the observer stops tracking it, leaving the original duplicate-and-removal failure."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R19-F1",
      "rationale": "`PromoteFallback` records the surviving alias before KMC\u2019s own unavailable-control migration empties it. The update-first and save-first cases kept Fireball at 4 and restored the shortcut at 9."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R19-F2",
      "rationale": "Effective binding selection recognizes the move to 11 while the registry identifies 7 as secondary. A native rebind of 11 clears that registry before repair or capture, allowing 7 to win again."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R19-F3",
      "rationale": "A confirmed emptying of a fresh canonical placement can establish its index. Retirement suppression is scoped to the native operation. The focused cases recovered 9 with no copies and after companion replacement, while retaining the stale-batch control."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r20-a38f1e",
  "round": 20,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate records toolbar placement across temporary absence using the existing save rows, projects eligible owners\u2019 persistent pets into the capital roster, and carries an explicit order on an already-unconscious target through mounted attack admission. The outstanding defects are in toolbar persistence and cleanup.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Exercise a nondefault shortcut, deliberate removal and a second rider across capital visits, interiors, forced dismount and saves inside and outside the city.",
      "Select a reserve owner\u2019s pet in Inventory and Character, verify identity and equipment changes, then reopen and reload to check selection and portrait count.",
      "Compare ordinary mounted and dismounted longbow clicks against standing, conscious-tripped and already-unconscious enemies at near and normal firing distances in real time and turn based. Observe the rolls, modifiers, action costs, blocked sightline and out-of-range approach."
    ],
    "scope_reviewed": "Final review of the complete mission on `codex/mounted-city-and-prone-fixes` from dbf63702. The controller transcript identifies 36f60e81 as the reviewed feature commit and latest verified branch push for [draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57).",
    "unresolved_questions": [
      "The original report does not identify the weapon, whether the prone enemy was conscious, or which transition first removed the toolbar shortcut."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
