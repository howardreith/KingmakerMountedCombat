<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r11-review part=1/1 sha256=cdbe98d02c7d567a4cb1a3bfe5182a1b9aa162c8bdf89e0fb4109fd044d1fc2f -->
### Codex GPT-6.2 Astra — review — round 11 — candidate `ddbe460f69`

**Decision:** `changes_required` — The focused gate passed on the reviewed commit. Three toolbar cases still need correction. Saving before migration can move the player's shortcut to an older alias slot or omit its placement record. Removing an alias immediately after a save can also cause the next update to put the shortcut back. The documented native deferrals are acceptable under the mission.

#### Findings

**R11-F1** · P2 · defect · blocks readiness — Saving a mixed legacy bar can move the canonical shortcut to the alias slot
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2514`; acceptance: §4 chosen supported toolbar slot, §4 existing save compatibility, §7 toolbar persistence and intent
- Trigger: A party rider saves before migration with a callable older alias at a lower index than the player's canonical Mount / Dismount binding.
- Expected: A newly captured archive restores one public control at slot 9, matching the precedence used to repair the live bar.
- Actual: CaptureLiveSlots emits both callable bindings in ascending slot order. With a live legacy Mount at 3 and the canonical control at 9, RestorePersistentSlots seeds 3 from the first row and rejects 9 as a different arrangement. If the canonical slot holds stale AbilityData, the live alias alone marks the actor as written and suppresses the remembered canonical index. In both cases, ordinary live reconciliation would have kept 9.
- Why it blocks / does not: Saving before the first repair changes the player's chosen slot on cold load. The same bar produces a different result if reconciliation runs first.
- Requested correction: Choose the effective public binding before emitting rows for the current archive. Give the canonical slot precedence, including when it needs a fresh AbilityData binding. Preserve compatibility with archives already written.
- Requested evidence: Capture before an update with a live legacy Mount at 3 and the canonical control at 9. Exercise both current and stale canonical AbilityData, using production slot order. Encode, decode and restore the captured rows, then verify one public control at 9, an empty slot 3 and unchanged unrelated slots.

**R10-F1** · P2 · defect · blocks readiness — A live alias can suppress its recovered placement record
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2520`; acceptance: §4 existing save compatibility, §4 one-time placement records, §7 affected save paths
- Trigger: The save header runs before migration while the eligible rider's legacy Mount or Dismount shell is active and the canonical public fact is missing or inactive.
- Expected: The archive contains one restorable public binding at the chosen index and one kind-7 placement record.
- Actual: CaptureLiveSlots marks the actor as written for the live alias, but emits kind 7 only if the separate canonical fact is active. CaptureAbsentIntent then skips the actor. With an inactive or missing canonical fact the archive contains the alias row alone, even though RecoverLegacyAlias recorded the placement. The codec regression starts with kind 7 already present.
- Why it blocks / does not: The duplicate-key rejection is fixed, but the writer still omits the placement record required by the prior finding for a supported save-first state.
- Requested correction: Account for the binding and placement record separately when composing the current archive. A callable alias can satisfy the binding requirement while its recovered placement still needs a kind-7 row.
- Requested evidence: Model shell and canonical fact activity independently. Capture each live alias at 9 with the canonical fact inactive and no prior placement record. Feed the emitted rows to the production codec and verify exactly one public binding at 9 and one placement record, with duplicate-key rejection intact.

**R2-F4** · P2 · defect · blocks readiness — Removing a returned legacy shortcut can still be undone after a save
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2677`; acceptance: §4 deliberate removal, §7 affected save paths
- Trigger: The player removes the successfully restored legacy alias after the save scope returns and before the next regular control update.
- Expected: Once the save scope returns a usable shortcut, removing it takes effect immediately and survives the next save/load.
- Actual: ClearSerializationHotbarLeases now checks retirement for aliases, but calls ObserveBound only for the canonical blueprint. A same-companion alias returned to its slot therefore leaves the arrangement marked Interrupted. If the player removes it before migration, ResolveUnbound sees an empty remembered slot and puts the canonical control back.
- Why it blocks / does not: A successful save can reverse a deliberate toolbar removal. The passing alias regression advances a frame to migrate the returned alias before the player can edit it.
- Requested correction: Make successful lease release establish the live arrangement for legacy aliases as well as the canonical control. Preserve the exact-placeholder and companion-retirement checks.
- Requested evidence: End the save scope with the same pet and a restored alias at 9. Remove it before the first subsequent update. Reconcile, save and reload, and verify slot 9 stays empty and only the placement record remains. Retain a control in which the untouched alias migrates once at 9.

#### Prior finding assessments

- **R10-F1** — `fix_rejected` — Counting kinds 1 and 2 prevents the original duplicate binding at the same index. The required placement record still depends on an active canonical fact, which the live shell does not establish. The passing codec cases supply that record themselves.
- **R9-F2** — `fix_verified` — Lease release now applies AllowsLeaseRestore to both exact legacy aliases and the canonical control. The focused gate exercised replacement of pet A during the scope and the same-companion control. The separate post-release edit finding concerns a lease that was correctly restored.
- **R9-F4** — `fix_verified` — The IsBad postfix now accepts both legacy aliases, locates the exact slot on the current rider's bar, respects canonical precedence and sends fenced observations to the outgoing load records. The cleanup-first component cases passed. Native callback timing remains part of owner verification.

#### Evidence assessment

- **Inspected:** The fixed mission, repository instructions, validation policy, current handoff, feature diff, X01 compatibility notes, prior ledger, developer transcript and gate artifacts.; The toolbar's native invalidation callback, fact reconciliation, save-scope leases, archive capture and restore, reserve migration and load fence, alongside the corresponding test doubles and production-codec fixtures.; The capital roster projection and its native adapter, plus the explicit player-order path from mounted click admission through pair-command target validation.
- **Executed verification:** The developer transcript confirms the guarded push and focused verification of ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7. The controller receipt records exit 0 in 47.7 seconds with the worktree unchanged. I verified the supplied log's SHA-256 against the receipt.; Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 built the candidate and passed 1,461 component checks, 153 source checks, 820 assembly-contract checks, 21 click-composition checks covering both patch orders, five archive-codec checks and 22 six-pair adapter checks.; The area-adapter comparison found the same 63 compiler diagnostics at the base and candidate. Its in-world restore assertion did not execute.
- **Source inspection only:** The city issues share the capital roster transition as a trigger. The toolbar defect concerns temporary loss of the leased ability. The city UI omission concerns the native roster's source of pets.; The city adapter projects each eligible owner's existing pet instance into the capital UI list. The mounted attack change carries an explicit order on an already-unconscious living target through the shared attack gates. The reported weapon and target consciousness remain unknown.
- **Required gaps:** Verify that save-first capture of a mixed canonical and legacy bar preserves the player's canonical index through cold load.; Verify that an active legacy shell with a temporarily inactive canonical fact emits both its binding and its placement record.; Verify that removing a returned legacy shortcut before the next control update leaves it off the bar through reconciliation and save/load.
- **Limitations:** This was a read-only review. The executed results come from the controller-bound gate. No native transaction ran, so restoration was inapplicable.; The toolbar double writes the canonical row before iterating aliases. Production iterates native slot indices. The codec alias fixtures also supply the placement record as input. These differences matter for the findings below.; Native inventory selection, equipment changes and actual ranged attack rolls remain unobserved on this candidate. The handoff identifies the missing candidate package and the fixture, driver and reader work needed for those checks.

#### Summary

- **Delivered behavior:** The candidate adds per-rider placement memory, projects eligible reserve owners' pets into the capital character interfaces, and admits explicit mounted attacks against living enemies already unconscious when ordered. The remaining source defects concern legacy toolbar bindings at save boundaries.
- **Scope reviewed:** Final development checkpoint for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through the fixed candidate.
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the weapon, whether the prone enemy was conscious, or which city transition first removed the shortcut.
- **Remaining owner review:** After correction, exercise city and interior transitions, save/load, shortcut moves and removals, and a second rider on a disposable fixture.; Select a reserve owner's pet in Inventory and Character, change permitted equipment, then reopen and reload to check selection and equipment persistence.; Compare actual mounted and dismounted longbow resolutions against standing, consciously prone and unconscious enemies at near and normal firing distances in real time and turn based. Check the documented line-of-sight, range, action-cost and penalty controls.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff13-829d-7f93-88a2-3736db7305a7`, duration 296.3s, response sha256 `fa43f3a155f572d0d728cef5b1344941fb6a4f5f3dd10243d6b364cbec3af063`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r11-3b19f9`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the reviewed commit. Three toolbar cases still need correction. Saving before migration can move the player's shortcut to an older alias slot or omit its placement record. Removing an alias immediately after a save can also cause the next update to put the shortcut back. The documented native deferrals are acceptable under the mission.",
  "evidence_assessment": {
    "executed_verification": [
      "The developer transcript confirms the guarded push and focused verification of ddbe460f69e0ae0d753207bb3d0ba27c5b00a8e7. The controller receipt records exit 0 in 47.7 seconds with the worktree unchanged. I verified the supplied log's SHA-256 against the receipt.",
      "Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 built the candidate and passed 1,461 component checks, 153 source checks, 820 assembly-contract checks, 21 click-composition checks covering both patch orders, five archive-codec checks and 22 six-pair adapter checks.",
      "The area-adapter comparison found the same 63 compiler diagnostics at the base and candidate. Its in-world restore assertion did not execute."
    ],
    "inspected": [
      "The fixed mission, repository instructions, validation policy, current handoff, feature diff, X01 compatibility notes, prior ledger, developer transcript and gate artifacts.",
      "The toolbar's native invalidation callback, fact reconciliation, save-scope leases, archive capture and restore, reserve migration and load fence, alongside the corresponding test doubles and production-codec fixtures.",
      "The capital roster projection and its native adapter, plus the explicit player-order path from mounted click admission through pair-command target validation."
    ],
    "limitations": [
      "This was a read-only review. The executed results come from the controller-bound gate. No native transaction ran, so restoration was inapplicable.",
      "The toolbar double writes the canonical row before iterating aliases. Production iterates native slot indices. The codec alias fixtures also supply the placement record as input. These differences matter for the findings below.",
      "Native inventory selection, equipment changes and actual ranged attack rolls remain unobserved on this candidate. The handoff identifies the missing candidate package and the fixture, driver and reader work needed for those checks."
    ],
    "required_gaps": [
      "Verify that save-first capture of a mixed canonical and legacy bar preserves the player's canonical index through cold load.",
      "Verify that an active legacy shell with a temporarily inactive canonical fact emits both its binding and its placement record.",
      "Verify that removing a returned legacy shortcut before the next control update leaves it off the bar through reconciliation and save/load."
    ],
    "source_inspection_only": [
      "The city issues share the capital roster transition as a trigger. The toolbar defect concerns temporary loss of the leased ability. The city UI omission concerns the native roster's source of pets.",
      "The city adapter projects each eligible owner's existing pet instance into the capital UI list. The mounted attack change carries an explicit order on an already-unconscious living target through the shared attack gates. The reported weapon and target consciousness remain unknown."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen supported toolbar slot",
        "\u00a74 existing save compatibility",
        "\u00a77 toolbar persistence and intent"
      ],
      "actual": "CaptureLiveSlots emits both callable bindings in ascending slot order. With a live legacy Mount at 3 and the canonical control at 9, RestorePersistentSlots seeds 3 from the first row and rejects 9 as a different arrangement. If the canonical slot holds stale AbilityData, the live alias alone marks the actor as written and suppresses the remembered canonical index. In both cases, ordinary live reconciliation would have kept 9.",
      "blocking_rationale": "Saving before the first repair changes the player's chosen slot on cold load. The same bar produces a different result if reconciliation runs first.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A newly captured archive restores one public control at slot 9, matching the precedence used to repair the live bar.",
      "id": "R11-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2514
      },
      "priority": "P2",
      "requested_correction": "Choose the effective public binding before emitting rows for the current archive. Give the canonical slot precedence, including when it needs a fresh AbilityData binding. Preserve compatibility with archives already written.",
      "requested_evidence": "Capture before an update with a live legacy Mount at 3 and the canonical control at 9. Exercise both current and stale canonical AbilityData, using production slot order. Encode, decode and restore the captured rows, then verify one public control at 9, an empty slot 3 and unchanged unrelated slots.",
      "title": "Saving a mixed legacy bar can move the canonical shortcut to the alias slot",
      "trigger": "A party rider saves before migration with a callable older alias at a lower index than the player's canonical Mount / Dismount binding."
    },
    {
      "acceptance_ids": [
        "\u00a74 existing save compatibility",
        "\u00a74 one-time placement records",
        "\u00a77 affected save paths"
      ],
      "actual": "CaptureLiveSlots marks the actor as written for the live alias, but emits kind 7 only if the separate canonical fact is active. CaptureAbsentIntent then skips the actor. With an inactive or missing canonical fact the archive contains the alias row alone, even though RecoverLegacyAlias recorded the placement. The codec regression starts with kind 7 already present.",
      "blocking_rationale": "The duplicate-key rejection is fixed, but the writer still omits the placement record required by the prior finding for a supported save-first state.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The archive contains one restorable public binding at the chosen index and one kind-7 placement record.",
      "id": "R10-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2520
      },
      "priority": "P2",
      "requested_correction": "Account for the binding and placement record separately when composing the current archive. A callable alias can satisfy the binding requirement while its recovered placement still needs a kind-7 row.",
      "requested_evidence": "Model shell and canonical fact activity independently. Capture each live alias at 9 with the canonical fact inactive and no prior placement record. Feed the emitted rows to the production codec and verify exactly one public binding at 9 and one placement record, with duplicate-key rejection intact.",
      "title": "A live alias can suppress its recovered placement record",
      "trigger": "The save header runs before migration while the eligible rider's legacy Mount or Dismount shell is active and the canonical public fact is missing or inactive."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a77 affected save paths"
      ],
      "actual": "ClearSerializationHotbarLeases now checks retirement for aliases, but calls ObserveBound only for the canonical blueprint. A same-companion alias returned to its slot therefore leaves the arrangement marked Interrupted. If the player removes it before migration, ResolveUnbound sees an empty remembered slot and puts the canonical control back.",
      "blocking_rationale": "A successful save can reverse a deliberate toolbar removal. The passing alias regression advances a frame to migrate the returned alias before the player can edit it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the save scope returns a usable shortcut, removing it takes effect immediately and survives the next save/load.",
      "id": "R2-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2677
      },
      "priority": "P2",
      "requested_correction": "Make successful lease release establish the live arrangement for legacy aliases as well as the canonical control. Preserve the exact-placeholder and companion-retirement checks.",
      "requested_evidence": "End the save scope with the same pet and a restored alias at 9. Remove it before the first subsequent update. Reconcile, save and reload, and verify slot 9 stays empty and only the placement record remains. Retain a control in which the untouched alias migrates once at 9.",
      "title": "Removing a returned legacy shortcut can still be undone after a save",
      "trigger": "The player removes the successfully restored legacy alias after the save scope returns and before the next regular control update."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R10-F1",
      "rationale": "Counting kinds 1 and 2 prevents the original duplicate binding at the same index. The required placement record still depends on an active canonical fact, which the live shell does not establish. The passing codec cases supply that record themselves."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F2",
      "rationale": "Lease release now applies AllowsLeaseRestore to both exact legacy aliases and the canonical control. The focused gate exercised replacement of pet A during the scope and the same-companion control. The separate post-release edit finding concerns a lease that was correctly restored."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R9-F4",
      "rationale": "The IsBad postfix now accepts both legacy aliases, locates the exact slot on the current rider's bar, respects canonical precedence and sends fenced observations to the outgoing load records. The cleanup-first component cases passed. Native callback timing remains part of owner verification."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r11-3b19f9",
  "round": 11,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds per-rider placement memory, projects eligible reserve owners' pets into the capital character interfaces, and admits explicit mounted attacks against living enemies already unconscious when ordered. The remaining source defects concern legacy toolbar bindings at save boundaries.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, exercise city and interior transitions, save/load, shortcut moves and removals, and a second rider on a disposable fixture.",
      "Select a reserve owner's pet in Inventory and Character, change permitted equipment, then reopen and reload to check selection and equipment persistence.",
      "Compare actual mounted and dismounted longbow resolutions against standing, consciously prone and unconscious enemies at near and normal firing distances in real time and turn based. Check the documented line-of-sight, range, action-cost and penalty controls."
    ],
    "scope_reviewed": "Final development checkpoint for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf637021db42cdc9bf27f3d79fc255d4569c788 through the fixed candidate.",
    "unresolved_questions": [
      "The original report does not identify the weapon, whether the prone enemy was conscious, or which city transition first removed the shortcut."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
