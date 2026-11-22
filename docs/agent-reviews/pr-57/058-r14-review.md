<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r14-review part=1/1 sha256=0986b9a2295a99025fa2f439999e7b5173e3efa1d88b237c732517e3e79662b3 -->
### Codex GPT-6.2 Astra — review — round 14 — candidate `9fd90f6bc4`

**Decision:** `changes_required` — The focused gate passed on the submitted commit, and the five pending findings are fixed for their reported cases. Five additional source-level defects remain in toolbar placement and lifecycle handling. They can move or resurrect a shortcut, make a continuing game disagree with its save, or retain a disposed player world while the mod is disabled.

#### Findings

**R14-F1** · P2 · defect · blocks readiness — Native emptying of a first manual shortcut leaves autofill armed
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2092`; acceptance: §4 chosen toolbar slot, §4 one-time placement, §4 occupied slots
- Trigger: Native UI empties a manually placed canonical shortcut before the first regular observation. Its chosen slot is filled before the same pet's control returns.
- Expected: Observing the player's canonical shortcut satisfies one-time placement even when native UI empties it. If its chosen slot becomes occupied, the shortcut stays off the bar.
- Actual: The canonical emptying branch records interrupted intent and increments NativeSlotInvalidationCount, but leaves placementRecorded unset. ResolveUnbound correctly gives up the occupied slot. TryInitialPlacement then sees an unrecorded rider and places the shortcut in another empty main-bar slot when autofill is enabled. The outgoing-load copy of canonical invalidation also omits the placement record.
- Why it blocks / does not: A first manual placement can turn into an unsolicited shortcut elsewhere after the player gives its slot to another ability.
- Requested correction: Record successful canonical invalidation in the one-time placement set belonging to the same world. Apply the same rule to the outgoing records restored after an abandoned load.
- Requested evidence: Extend the existing first-manual-placement cleanup case. Put an ordinary ability in the emptied slot before recovery, enable autofill, and verify it stays there with no Mount / Dismount shortcut elsewhere through update, save and reload.

**R14-F2** · P2 · defect · blocks readiness — A later legacy alias can override the canonical slot during native cleanup
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1983`; acceptance: §4 chosen toolbar slot, §4 existing save compatibility
- Trigger: A mixed legacy bar loses both public bindings during temporary unavailability, with native cleanup visiting the canonical slot first.
- Expected: The canonical slot wins over an older alias on the same bar, including when native cleanup empties both during one pass.
- Actual: LegacyAliasIndex reads the bar after earlier slots may already have been emptied. If cleanup empties a canonical binding at 4 and then an old Mount alias at 9, the second observation sees 9 as the chosen alias. Applying the pending reports first remembers 4, then ObserveNativeAliasInvalidated overwrites it with 9. The existing mixed-bar control cleans the alias while the canonical slot is still present, so it misses this order.
- Why it blocks / does not: An older supported bar restores the shortcut to a different index depending on native cleanup order.
- Requested correction: Preserve the selected public binding for the cleanup operation before its slot mutations can change precedence. Ensure a later report for a secondary binding cannot replace that choice.
- Requested evidence: Run a cleanup pass over a stale canonical at 4 and a stale alias at 9 with neither fact live. After both slots empty, require one saved and restored public binding at 4. Retain the alias-only recovery control.

**R14-F3** · P2 · defect · blocks readiness — An occupied slot is given up in the archive but retained in memory
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2595`; acceptance: §4 occupied slots, §4 save/load, §4 respect player arrangement
- Trigger: A player saves while another ability occupies an interrupted shortcut's index, then clears that ability while the pet is still unavailable.
- Expected: Once the save boundary observes a different ability in the remembered slot and classifies the shortcut as off the bar, that decision also governs the continuing game.
- Actual: CaptureAbsentIntent passes occupancy to AbsentRowsAfterLive, which emits a placement record alone. The in-memory entry still contains the interrupted index. While the pet remains unavailable, reconciliation keeps that index. If the player clears the occupant before the pet returns, the continuing game restores the shortcut there. Reloading the save first leaves it off the bar.
- Why it blocks / does not: Continuing after a save and reloading that same save produce different arrangements after the same toolbar edit.
- Requested correction: Apply the occupied-slot decision to the rider's current placement intent as well as its emitted rows. Preserve the occupant and the one-time placement record.
- Requested evidence: With the same pet temporarily unavailable and an ordinary ability occupying remembered slot 9, save. Compare continuing against reloading that save. In each case remove the occupant before the pet returns and require the shortcut to remain off the bar.

**R14-F4** · P2 · defect · blocks readiness — A load while KMC is disabled can retain the outgoing player
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1745`; acceptance: §4 mod disable, §4 world disposal, §7 lifecycle cleanup
- Trigger: The owner disables KMC in a loaded game, loads another save and continues playing with KMC disabled.
- Expected: A disabled control service releases the outgoing player on world replacement and does not acquire placement intent from native toolbar cleanup.
- Actual: WrapLoadRoutine calls BeginSelectedLoad while controls are disabled. The fence takes a strong reference to the outgoing Player. On successful replacement, Update returns at !enabled before reaching ArrangementsAccepted, and the successful load path does not release the fence. A disabled session without a later toolbar operation can therefore retain the previous player graph. The installed IsBad observer also accepts new placement observations while disabled because its entry guard checks only disposal and registration.
- Why it blocks / does not: Disabling the mod clears its records once, but a subsequent ordinary load can recreate toolbar state and keep a disposed world reachable.
- Requested correction: Make load-fence ownership and toolbar observations respect the disabled lifecycle. Preserve save sanitation and the enabled service's abandoned-load recovery.
- Requested evidence: Disable KMC, complete a load from player A to player B, and verify the toolbar service releases A without re-enable or another save. Exercise native cleanup on a surviving legacy slot while disabled and verify it creates no placement record.

**R14-F5** · P2 · defect · blocks readiness — An abandoned load discards a native rebind observed in the surviving world
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2063`; acceptance: §4 deliberate removal, §4 save/load, §7 abandoned-load cleanup
- Trigger: Native cleanup repairs a previously interrupted slot while a selected load fences the outgoing player, and the load is abandoned.
- Expected: A successful rebind updates the outgoing world's saved arrangement to live. An abandoned load returns that state, and a completed replacement discards it.
- Actual: ApplySlotInvalidation returns immediately for a successful rebind while the outgoing world is fenced. If its saved intent was interrupted, abandoning the load restores that interrupted entry even though native cleanup made the shortcut live again. A removal before the next update is then treated as recoverable and the shortcut comes back.
- Why it blocks / does not: Canceling a load can reverse a deliberate removal of a repaired shortcut in the surviving world.
- Requested correction: Apply the rebind outcome to the owning fence's outgoing intent and placement record while keeping the incoming world's records isolated.
- Requested evidence: Begin a load with interrupted intent and a stale canonical slot at 9. Rebind it during fenced cleanup, abandon the load, then remove the repaired shortcut before reconciliation. Require a record-only save and an empty slot after reload. Keep completed replacement as the isolation control.

#### Prior finding assessments

- **R12-F4** — `fix_verified` — Production now classifies the resulting slot by KMC kind and live fact data. The focused assembly check inspects the pinned cleanup method's construction and field store, and the regression drives rebind and empty outcomes through the production classifier.
- **R13-F1** — `fix_verified` — For the reported single returned alias, successful invalidation marks its recorded placement interrupted. The update-first and save-first cases recover one shortcut at 9. R14-F2 concerns a second binding in the same cleanup pass.
- **R13-F2** — `fix_verified` — Save-boundary observation now records a visible manual placement, and TryInitialPlacement yields to its pending public-slot lease. The regression checks one shortcut at 9 after release, subsequent updates and reload.
- **R13-F3** — `fix_verified` — The remembered-row writer checks both the occupant and emitted indices. The codec case preserves the Charge row and one placement record, and still rejects an injected duplicate. R14-F3 concerns the continuing in-memory arrangement after that valid save.
- **R13-F4** — `fix_verified` — Each deferred report carries its originating player. The mutation of placement state now requires that player and the exact reporting unit on its roster before the load fence can release. The regression covers replacement and abandonment.

#### Evidence assessment

- **Inspected:** Controller mission, feature diff, changed-file list, prior findings, developer report and CLI transcript, plus AGENTS.md, validation policy, current project records and the mission handoff.; Toolbar observation, live and remembered archive rows, native cleanup patches, save leases, load fences, disable and disposal paths, domain policies, component doubles and production-codec tests.; Capital roster projection, persistent pet identity checks, mounted attack admission and execution guards, focused scripts and assembly contracts.
- **Executed verification:** Confirmed the detached snapshot HEAD with git rev-parse. The feature diff passed git diff --check against dbf637021db42cdc9bf27f3d79fc255d4569c788.; The controller transcript records the guarded push and a passing focused gate on 9fd90f6bc49837d9133547a0b7db17cfd29b74a9. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 63.7 seconds with the worktree unchanged.; The pre-commit development run printed 1472 component passes and zero failures. The separate mutation run printed 1467 passes and the five named regression failures. These runs are distinct from the clean-commit gate.
- **Source inspection only:** The city issues share the capital roster transition as a trigger. Toolbar loss follows temporary control-fact loss. Missing pet portraits follow the native roster's in-game pet filter.; The attack change admits an explicit player order on an already-unconscious living enemy. The conscious-prone case has a source trace and remains open for native observation. The reported weapon and target consciousness are unknown.
- **Required gaps:** Correct the five toolbar and lifecycle paths below, exercise their distinguishing sequences, and obtain a focused receipt on the corrected candidate.
- **Limitations:** This was a read-only review. The five new findings follow from production control flow and were not executed in game.; The focused receipt establishes the gate result. The transcript includes only a preview of the detailed successful development run. The area-adapter comparison classifies a pre-existing compile failure, so its restore assertion remains unexecuted.; Native toolbar timing, capital inventory interaction and prone-target attack resolution remain NOT RUN. The handoff identifies the missing candidate package, reader and driver prerequisites and gives owner playtest steps. No live transaction or restoration took place.

#### Summary

- **Delivered behavior:** The candidate adds per-rider toolbar intent, projects eligible owners' persistent pets into the capital UI roster, and carries an explicit order against an already-unconscious living enemy into mounted attack admission. The focused gate passed. Toolbar intent and cleanup still need the corrections listed above.
- **Scope reviewed:** Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf63702 through 9fd90f6b, including all five pending prior findings.
- **Pending later-phase work:** none
- **Unresolved questions:** The owner's original save, weapon and target state were unavailable. Which toolbar transition caused the reported disappearance and whether the reported enemy was conscious remain unconfirmed.
- **Remaining owner review:** After correction, play the documented capital travel and save/load sequence, including reserve-pet selection, equipment interaction and repeated panel opening.; Compare mounted and dismounted longbow attacks against standing, conscious-prone and unconscious enemies in real time and turn based. Observe actual resolution, range, line of sight, modifiers and action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `high` | `high` (turns ['high']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a1ff3e-d95f-7203-94a7-944632f3960a`, duration 357.9s, response sha256 `7b3f266e8b438692386bd9827ec1814795e654047711709ea4b47ce20d839689`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r14-fd0e34`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "9fd90f6bc49837d9133547a0b7db17cfd29b74a9",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the submitted commit, and the five pending findings are fixed for their reported cases. Five additional source-level defects remain in toolbar placement and lifecycle handling. They can move or resurrect a shortcut, make a continuing game disagree with its save, or retain a disposed player world while the mod is disabled.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the detached snapshot HEAD with git rev-parse. The feature diff passed git diff --check against dbf637021db42cdc9bf27f3d79fc255d4569c788.",
      "The controller transcript records the guarded push and a passing focused gate on 9fd90f6bc49837d9133547a0b7db17cfd29b74a9. Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 exited 0 in 63.7 seconds with the worktree unchanged.",
      "The pre-commit development run printed 1472 component passes and zero failures. The separate mutation run printed 1467 passes and the five named regression failures. These runs are distinct from the clean-commit gate."
    ],
    "inspected": [
      "Controller mission, feature diff, changed-file list, prior findings, developer report and CLI transcript, plus AGENTS.md, validation policy, current project records and the mission handoff.",
      "Toolbar observation, live and remembered archive rows, native cleanup patches, save leases, load fences, disable and disposal paths, domain policies, component doubles and production-codec tests.",
      "Capital roster projection, persistent pet identity checks, mounted attack admission and execution guards, focused scripts and assembly contracts."
    ],
    "limitations": [
      "This was a read-only review. The five new findings follow from production control flow and were not executed in game.",
      "The focused receipt establishes the gate result. The transcript includes only a preview of the detailed successful development run. The area-adapter comparison classifies a pre-existing compile failure, so its restore assertion remains unexecuted.",
      "Native toolbar timing, capital inventory interaction and prone-target attack resolution remain NOT RUN. The handoff identifies the missing candidate package, reader and driver prerequisites and gives owner playtest steps. No live transaction or restoration took place."
    ],
    "required_gaps": [
      "Correct the five toolbar and lifecycle paths below, exercise their distinguishing sequences, and obtain a focused receipt on the corrected candidate."
    ],
    "source_inspection_only": [
      "The city issues share the capital roster transition as a trigger. Toolbar loss follows temporary control-fact loss. Missing pet portraits follow the native roster's in-game pet filter.",
      "The attack change admits an explicit player order on an already-unconscious living enemy. The conscious-prone case has a source trace and remains open for native observation. The reported weapon and target consciousness are unknown."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 one-time placement",
        "\u00a74 occupied slots"
      ],
      "actual": "The canonical emptying branch records interrupted intent and increments NativeSlotInvalidationCount, but leaves placementRecorded unset. ResolveUnbound correctly gives up the occupied slot. TryInitialPlacement then sees an unrecorded rider and places the shortcut in another empty main-bar slot when autofill is enabled. The outgoing-load copy of canonical invalidation also omits the placement record.",
      "blocking_rationale": "A first manual placement can turn into an unsolicited shortcut elsewhere after the player gives its slot to another ability.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Observing the player's canonical shortcut satisfies one-time placement even when native UI empties it. If its chosen slot becomes occupied, the shortcut stays off the bar.",
      "id": "R14-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2092
      },
      "priority": "P2",
      "requested_correction": "Record successful canonical invalidation in the one-time placement set belonging to the same world. Apply the same rule to the outgoing records restored after an abandoned load.",
      "requested_evidence": "Extend the existing first-manual-placement cleanup case. Put an ordinary ability in the emptied slot before recovery, enable autofill, and verify it stays there with no Mount / Dismount shortcut elsewhere through update, save and reload.",
      "title": "Native emptying of a first manual shortcut leaves autofill armed",
      "trigger": "Native UI empties a manually placed canonical shortcut before the first regular observation. Its chosen slot is filled before the same pet's control returns."
    },
    {
      "acceptance_ids": [
        "\u00a74 chosen toolbar slot",
        "\u00a74 existing save compatibility"
      ],
      "actual": "LegacyAliasIndex reads the bar after earlier slots may already have been emptied. If cleanup empties a canonical binding at 4 and then an old Mount alias at 9, the second observation sees 9 as the chosen alias. Applying the pending reports first remembers 4, then ObserveNativeAliasInvalidated overwrites it with 9. The existing mixed-bar control cleans the alias while the canonical slot is still present, so it misses this order.",
      "blocking_rationale": "An older supported bar restores the shortcut to a different index depending on native cleanup order.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The canonical slot wins over an older alias on the same bar, including when native cleanup empties both during one pass.",
      "id": "R14-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1983
      },
      "priority": "P2",
      "requested_correction": "Preserve the selected public binding for the cleanup operation before its slot mutations can change precedence. Ensure a later report for a secondary binding cannot replace that choice.",
      "requested_evidence": "Run a cleanup pass over a stale canonical at 4 and a stale alias at 9 with neither fact live. After both slots empty, require one saved and restored public binding at 4. Retain the alias-only recovery control.",
      "title": "A later legacy alias can override the canonical slot during native cleanup",
      "trigger": "A mixed legacy bar loses both public bindings during temporary unavailability, with native cleanup visiting the canonical slot first."
    },
    {
      "acceptance_ids": [
        "\u00a74 occupied slots",
        "\u00a74 save/load",
        "\u00a74 respect player arrangement"
      ],
      "actual": "CaptureAbsentIntent passes occupancy to AbsentRowsAfterLive, which emits a placement record alone. The in-memory entry still contains the interrupted index. While the pet remains unavailable, reconciliation keeps that index. If the player clears the occupant before the pet returns, the continuing game restores the shortcut there. Reloading the save first leaves it off the bar.",
      "blocking_rationale": "Continuing after a save and reloading that same save produce different arrangements after the same toolbar edit.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the save boundary observes a different ability in the remembered slot and classifies the shortcut as off the bar, that decision also governs the continuing game.",
      "id": "R14-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2595
      },
      "priority": "P2",
      "requested_correction": "Apply the occupied-slot decision to the rider's current placement intent as well as its emitted rows. Preserve the occupant and the one-time placement record.",
      "requested_evidence": "With the same pet temporarily unavailable and an ordinary ability occupying remembered slot 9, save. Compare continuing against reloading that save. In each case remove the occupant before the pet returns and require the shortcut to remain off the bar.",
      "title": "An occupied slot is given up in the archive but retained in memory",
      "trigger": "A player saves while another ability occupies an interrupted shortcut's index, then clears that ability while the pet is still unavailable."
    },
    {
      "acceptance_ids": [
        "\u00a74 mod disable",
        "\u00a74 world disposal",
        "\u00a77 lifecycle cleanup"
      ],
      "actual": "WrapLoadRoutine calls BeginSelectedLoad while controls are disabled. The fence takes a strong reference to the outgoing Player. On successful replacement, Update returns at !enabled before reaching ArrangementsAccepted, and the successful load path does not release the fence. A disabled session without a later toolbar operation can therefore retain the previous player graph. The installed IsBad observer also accepts new placement observations while disabled because its entry guard checks only disposal and registration.",
      "blocking_rationale": "Disabling the mod clears its records once, but a subsequent ordinary load can recreate toolbar state and keep a disposed world reachable.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A disabled control service releases the outgoing player on world replacement and does not acquire placement intent from native toolbar cleanup.",
      "id": "R14-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1745
      },
      "priority": "P2",
      "requested_correction": "Make load-fence ownership and toolbar observations respect the disabled lifecycle. Preserve save sanitation and the enabled service's abandoned-load recovery.",
      "requested_evidence": "Disable KMC, complete a load from player A to player B, and verify the toolbar service releases A without re-enable or another save. Exercise native cleanup on a surviving legacy slot while disabled and verify it creates no placement record.",
      "title": "A load while KMC is disabled can retain the outgoing player",
      "trigger": "The owner disables KMC in a loaded game, loads another save and continues playing with KMC disabled."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 save/load",
        "\u00a77 abandoned-load cleanup"
      ],
      "actual": "ApplySlotInvalidation returns immediately for a successful rebind while the outgoing world is fenced. If its saved intent was interrupted, abandoning the load restores that interrupted entry even though native cleanup made the shortcut live again. A removal before the next update is then treated as recoverable and the shortcut comes back.",
      "blocking_rationale": "Canceling a load can reverse a deliberate removal of a repaired shortcut in the surviving world.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A successful rebind updates the outgoing world's saved arrangement to live. An abandoned load returns that state, and a completed replacement discards it.",
      "id": "R14-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2063
      },
      "priority": "P2",
      "requested_correction": "Apply the rebind outcome to the owning fence's outgoing intent and placement record while keeping the incoming world's records isolated.",
      "requested_evidence": "Begin a load with interrupted intent and a stale canonical slot at 9. Rebind it during fenced cleanup, abandon the load, then remove the repaired shortcut before reconciliation. Require a record-only save and an empty slot after reload. Keep completed replacement as the isolation control.",
      "title": "An abandoned load discards a native rebind observed in the surviving world",
      "trigger": "Native cleanup repairs a previously interrupted slot while a selected load fences the outgoing player, and the load is abandoned."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R12-F4",
      "rationale": "Production now classifies the resulting slot by KMC kind and live fact data. The focused assembly check inspects the pinned cleanup method's construction and field store, and the regression drives rebind and empty outcomes through the production classifier."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F1",
      "rationale": "For the reported single returned alias, successful invalidation marks its recorded placement interrupted. The update-first and save-first cases recover one shortcut at 9. R14-F2 concerns a second binding in the same cleanup pass."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F2",
      "rationale": "Save-boundary observation now records a visible manual placement, and TryInitialPlacement yields to its pending public-slot lease. The regression checks one shortcut at 9 after release, subsequent updates and reload."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F3",
      "rationale": "The remembered-row writer checks both the occupant and emitted indices. The codec case preserves the Charge row and one placement record, and still rejects an injected duplicate. R14-F3 concerns the continuing in-memory arrangement after that valid save."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R13-F4",
      "rationale": "Each deferred report carries its originating player. The mutation of placement state now requires that player and the exact reporting unit on its roster before the load fence can release. The regression covers replacement and abandonment."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r14-fd0e34",
  "round": 14,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate adds per-rider toolbar intent, projects eligible owners' persistent pets into the capital UI roster, and carries an explicit order against an already-unconscious living enemy into mounted attack admission. The focused gate passed. Toolbar intent and cleanup still need the corrections listed above.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, play the documented capital travel and save/load sequence, including reserve-pet selection, equipment interaction and repeated panel opening.",
      "Compare mounted and dismounted longbow attacks against standing, conscious-prone and unconscious enemies in real time and turn based. Observe actual resolution, range, line of sight, modifiers and action expenditure."
    ],
    "scope_reviewed": "Complete final mission for draft PR #57 on codex/mounted-city-and-prone-fixes, from dbf63702 through 9fd90f6b, including all five pending prior findings.",
    "unresolved_questions": [
      "The owner's original save, weapon and target state were unavailable. Which toolbar transition caused the reported disappearance and whether the reported enemy was conscious remain unconfirmed."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
