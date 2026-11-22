<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r25-review part=1/1 sha256=86f34d99d0b7422903ee426a461bc4ecfcb1d69beabb0673ca292be67503945e -->
### Codex GPT-6.2 Astra — review — round 25 — candidate `923e5ccdac`

**Decision:** `changes_required` — The focused gate passed on the requested commit. Production cleanup can still erase the only record of a reserve rider’s chosen slot, discard a later move, or save a retired KMC auto-use reference. An abandoned load can also restore the wrong shortcut. These are source-level failures of the toolbar and save requirements. The documented deferral of native UI and combat checks is acceptable under the mission.

#### Findings

**R25-F1** · P1 · defect · blocks readiness — Area suspension clears reserve bars it never observes
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2851`; acceptance: §4 chosen slot across city and interior transitions, §4 another rider, §7 toolbar transitions
- Trigger: A reserve rider with a linked pet and a surviving shortcut becomes available before its first reserve scan. Another mounted pair starts an area transfer.
- Expected: The reserve rider’s slot is recoverable when the same pet returns, independently of the pair that triggered the transfer.
- Actual: Area capture calls CapturePersistentSlots() with absent-intent classification off. SuspendAreaControls then observes current candidates and previously observed departures. RemoveAllManagedFacts(true) additionally sweeps never-observed reserve riders. For this rider, RecordAbsence has no entry to update and ClearManagedHotbarSlots erases slot 9. The area double at tests/MountedControlPlacementIntentTests.cs line 540 still clears only candidates and observed riders.
- Why it blocks / does not: An ordinary area transition can destroy the only recoverable copy of a player’s chosen slot.
- Requested correction: Classify newly reached reserve bars before area suspension clears them, or limit the expanded sweep to boundaries that already classify those riders. Preserve the reserve fact cleanup used by save and disable.
- Requested evidence: Start with a never-observed reserve rider, a surviving shortcut at 9, no owned row and another mounted pair. Run area capture and suspension before a reserve update. Verify the subsequent save and party return retain 9 exactly once and preserve an unrelated ability. Exercise the same sweep membership as production.

**R22-F3** · P2 · defect · blocks readiness — The first reserve scan ignores a move while the old slot is interrupted
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2453`; acceptance: §4 player moves and removals, §4 older city saves, §4 save/load
- Trigger: Native GetSlot empties 3, CleanupBadSlots rebinds its surviving copy at 9, and the player moves 9 to 11 before the first reserve scan.
- Expected: The moved binding becomes the remembered slot. With no move, the untouched copy continues to yield to the free slot at 3.
- Actual: With 3 free, native rebinding of the known copy at 9 leaves 3 recorded as interrupted. After the move, EffectivePublicIndex identifies 11 as the player’s binding. AdmitSurvivingBinding declines the existing record and the !live return skips 11. The scan clears 11 and the archive carries 3.
- Why it blocks / does not: A player move made before the first reserve scan can still be permanently reversed by a city save.
- Requested correction: Honor an identifiable new binding before clearing the reserve bar even when the previous choice was interrupted. Keep the existing rules for an untouched duplicate and a changed pet.
- Requested evidence: Repeat the rebound reserve case with slot 3 left empty. Compare moving 9 to 11 with leaving 9 untouched. Verify one marker and the correct slot, respectively 11 and 3, in the capital save, same-world return and cold restore.

**R23-F3** · P2 · defect · blocks readiness — Alias migration loses a moved shortcut while the public control is unavailable
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1935`; acceptance: §4 player moves, §4 temporary mount unavailability, §4 existing save compatibility
- Trigger: Native cleanup empties canonical slot 4. A known canonical copy remains at 7 and the player moves an older Mount alias from 9 to 11 before the same pet resolves.
- Expected: KMC records 11 before removing the alias and restores one canonical shortcut there when the same pet returns.
- Actual: EffectivePublicIndex selects 11. With the public fact absent and the pet unresolved, the pre-withdrawal observation is skipped. The inactive repair branch records only a fallback copy. RecoverLegacyAlias declines the moved alias beside canonical slot 7, then the migration loop empties 11. A subsequent save can carry 4. Saving before that update can carry 11.
- Why it blocks / does not: The saved location of the same player edit depends on whether an update runs before the save.
- Requested correction: Before inactive-control migration empties an effective, player-moved alias, record its index as a temporary absence. Preserve the precedence of an untouched copy and the existing companion-retirement check.
- Requested evidence: Extend the moved-alias fixture with the canonical fact absent and the same linked pet unresolved through the first update. Verify update-first and save-first both carry 11 and restore it once. Keep the untouched-alias control.

**R24-F3** · P2 · defect · blocks readiness — An abandoned load loses the identity of a surviving duplicate
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2205`; acceptance: §4 world disposal and save/load, §7 affected lifecycle paths
- Trigger: A selected load crosses a frame without a Player while the outgoing rider has a free interrupted slot at 3 and a known duplicate at 9.
- Expected: Returning to the same player world restores both the interrupted choice and the information that makes the untouched duplicate yield to it.
- Actual: The fence now preserves the placement map through a null Player. Update still calls ReleaseCopiesOfDepartedRiders before checking the fence. CopiesOf clears the duplicate registry when Player is null. After abandonment restores A’s placement map, the surviving canonical slot at 9 is read as a fresh player binding and can replace 3 in the next save.
- Why it blocks / does not: Canceling a load can change the continuing campaign’s toolbar even when the player did not edit it.
- Requested correction: Keep the outgoing world’s copy identities with its placement records during a fenced interval without a Player. Release them on actual replacement, disable or disposal, and protect them from roster pruning during the interval.
- Requested evidence: In A, let GetSlot empty 3 while a known canonical copy survives at 9. Begin a load, update without a Player, reinstate A and abandon. Verify an immediate save and cold restore retain 3. Verify a genuine transition to B releases A’s copy objects.

**R25-F2** · P2 · defect · blocks readiness — Reserve cleanup leaves a serialized KMC auto-use selection behind
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2854`; acceptance: §4 mod disable/removal cleanup, §4 existing save compatibility, §7 affected save/lifecycle paths
- Trigger: An older capital save restores a managed auto-use selection on a reserve rider, and the player saves again or disables KMC before the rider returns to the party.
- Expected: Save, disable and disposal cleanup clear an exact retired or managed KMC auto-use selection while preserving an ordinary auto-use selection.
- Actual: The expanded sweep removes facts and hotbar slots. The only code clearing UnitEntityData.AutoUseAbility runs during reconciliation of current candidates. A never-reconciled reserve rider can therefore retain a KMC reference in the serialized m_AutoUseAbility field even with zero KMC facts and slots. The existing Sprint4NaturalScenario fixture explicitly constructs this older-save state without a backing fact.
- Why it blocks / does not: A capital save can still carry a mod-owned ability reference through the cleanup intended to support mod removal.
- Requested correction: Apply the existing exact-blueprint auto-use cleanup to reserve and detached riders at these boundaries, including when there is no matching fact or toolbar slot.
- Requested evidence: Use the existing stale Rider Primary auto-use fixture on a never-observed reserve rider. Inspect the reference inside serialization and after disable. Include a second rider with an ordinary auto-use ability and verify it survives.

**R25-F3** · P2 · defect · blocks readiness — A save skips a fresh binding on an already-scanned reserve rider
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2393`; acceptance: §4 player moves, §4 city save/load, §7 toolbar persistence and intent
- Trigger: A reserve rider keeps its live public fact after its first ordinary scan, receives a fresh toolbar placement, and stays in reserve until a capital save.
- Expected: A fresh identifiable binding present when saving supersedes the index remembered from an earlier reserve scan.
- Actual: An ordinary reserve scan clears managed slots but leaves a live fact on a never-observed rider. Once MarkFirst has recorded the unit, the save header skips its bar, and CaptureLiveSlots considers only current candidates. If the live control is placed at 11 after that first scan, the header writes the remembered 9. Serialization then clears 11 and removes the fact.
- Why it blocks / does not: Fixing first-scan classification alone still leaves a later player placement vulnerable to the next save.
- Requested correction: Account for newly present public bindings at the save boundary even on an already-scanned reserve unit. An empty bar left by KMC’s own earlier cleanup must continue to preserve the remembered slot.
- Requested evidence: Run a normal reserve update with a live fact and slot 9, then place that fact at 11 on the same reserve unit and save. Verify 11 and one marker, zero managed facts and slots inside serialization, and one shortcut at 11 after cold restore. Keep a control with no later placement.

#### Prior finding assessments

- **R18-F1** — `fix_verified` — CopyYields now makes a known duplicate yield to an explicit off-bar choice. The recorded regression reads duplicate slot 9 after withdrawal and checks a marker-only archive, preserved Attack, and no restored shortcut in both operation orders.
- **R22-F3** — `fix_rejected` — The move and removal cases with Fireball occupying 3 now pass. Leaving 3 free keeps the old choice interrupted after native rebind, and the same first scan discards a subsequent move to 11.
- **R23-F3** — `fix_rejected` — Migration now preserves the selected alias beside a canonical copy when the public fact is active. The added fixture activates that fact before the move. With the same pet still unresolved, inactive migration empties the selected alias before recording it.
- **R24-F1** — `fix_verified` — The save, disable and disposal fact sweep now includes current-world reserve and detached riders. The focused cases retain the reserve fact until cleanup and inspect facts and slots inside serialization and after disable. The separate area-sweep and auto-use findings concern other effects of those boundaries.
- **R24-F2** — `fix_verified` — The observer synchronizes placement ownership before reading the maps, and the deferred write synchronizes after validating the reporting world. The incoming-world fixture reads B’s slot 9 before restore or update and verifies its archive row and party return.
- **R24-F3** — `fix_rejected` — Accepts(null) preserves the outgoing placement, removal and retirement records in the reported fixture. The same null-player update still clears the copy identities needed to interpret a surviving canonical duplicate after abandonment.

#### Evidence assessment

- **Inspected:** The mission, AGENTS.md, validation policy, current handoff and resume guidance, all 34 changed files in the feature diff, the developer report and transcript, the controller receipt and log, and the six pending findings.; The production toolbar observation, native slot cleanup, reserve scan, area suspension, archive capture and restore, selected-load fence, and disable paths. Compared their ordering with the toolbar test double and inspected the repository’s existing serialized auto-use fixture.; The capital roster projection and its native GetGroup contract, the mounted attack admission and continuation changes, the 0.2.13 click-handler compatibility path, and the native verification prerequisites in docs/CITY-AND-PRONE-HANDOFF.md.
- **Executed verification:** Read-only Git checks confirmed HEAD and the merge base. git diff --check passed. The mission and focused-gate log matched their supplied SHA-256 identities.; The controller receipt and developer transcript confirm scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 ran on 923e5ccdac7927b492cd410bdedf1fc17819e826. It exited 0 in 25.5 seconds with the worktree unchanged.; The recorded build passed. The component suite reported 1,505 passes, source validation 153, and installed Kingmaker contracts 825. The real Gunslinger 0.0.191 composition check passed 21 cases including both patch orders. The archive row checks passed 9 cases, the archive adapter 22, and the target-liveness source check 3. All six focused children passed.; The gate identified KingmakerMountedCombat.dll by SHA-256 dc54bbac8977955e73560f3e2d928d826a85737ef8df7dbdca6b18469df878c2 and MVID b5550b9d-a295-4e4a-803f-e788030bfb44.
- **Source inspection only:** The failure sequences in the findings follow the candidate’s production call paths. They were not executed in Kingmaker.; The capital adapter uses the existing owner-linked pet instance and the native roster. The attack change distinguishes an explicit order against an already-unconscious living enemy from automatic acquisition. Actual portrait selection, equipment interaction and attack rolls remain native observations.
- **Required gaps:** Preserve the latest identifiable binding before area, reserve and legacy-alias cleanup, including a binding created after the first reserve scan. Verify the resulting archive rows and cold restore.; Preserve the identity of surviving duplicate bindings through an abandoned load and remove exact serialized KMC auto-use references during reserve cleanup.
- **Limitations:** The toolbar tests execute production domain rules through a double that mirrors the native service. Its area-suspension sweep omits the reserve population that production now clears. The pre-commit mutation runs demonstrate sensitivity to the mutated domain or double code.; The area adapter produced the same 63 compilation diagnostics on the base and candidate. That result establishes diagnostic parity only. The separate archive adapter executed with substituted native and service boundaries.; Candidate gameplay was not run. The handoff identifies the missing guarded package operation, capital reader and fixture lineage, reserve portrait/equipment driver, and enemy-posture setup and attack-resolution reader. No installation or save restoration was needed.

#### Summary

- **Delivered behavior:** The candidate uses the existing archive rows to remember toolbar intent, adds eligible owners’ persistent pets to the native capital UI roster, and permits an explicit mounted attack order against an already-unconscious living enemy. The two city symptoms have distinct source causes in toolbar lifecycle handling and native roster construction.
- **Scope reviewed:** The complete final mission for draft PR #57, including the feature diff from dbf63702 to 923e5ccd, focused offline evidence, native deferral and all six pending findings.
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the weapon or establish whether the enemy was conscious and prone or unconscious. The source trace found a life-state rejection for the latter. Reproduction of the reported shot remains open.
- **Remaining owner review:** After correction, confirm nondefault slot placement, moves and deliberate removal through city visits, an interior transition, party return and cold load.; Select an eligible reserve owner’s pet in Inventory and Character, verify its identity and permitted equipment changes, then reopen the panels after a transition and load.; Use the documented matched longbow setup in real time and turn-based mode. Observe actual attack rolls against standing, conscious-prone and unconscious enemies, along with line of sight, range, ammunition, penalties and action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a200af-c189-72d2-80b6-cdc418af9cb4`, duration 1083.5s, response sha256 `d5142d86a8a5d4502e9f3961a423a630f825059c9e140e4621a79b5ac78c33a8`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r25-44836a`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "923e5ccdac7927b492cd410bdedf1fc17819e826",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the requested commit. Production cleanup can still erase the only record of a reserve rider\u2019s chosen slot, discard a later move, or save a retired KMC auto-use reference. An abandoned load can also restore the wrong shortcut. These are source-level failures of the toolbar and save requirements. The documented deferral of native UI and combat checks is acceptable under the mission.",
  "evidence_assessment": {
    "executed_verification": [
      "Read-only Git checks confirmed HEAD and the merge base. git diff --check passed. The mission and focused-gate log matched their supplied SHA-256 identities.",
      "The controller receipt and developer transcript confirm scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 ran on 923e5ccdac7927b492cd410bdedf1fc17819e826. It exited 0 in 25.5 seconds with the worktree unchanged.",
      "The recorded build passed. The component suite reported 1,505 passes, source validation 153, and installed Kingmaker contracts 825. The real Gunslinger 0.0.191 composition check passed 21 cases including both patch orders. The archive row checks passed 9 cases, the archive adapter 22, and the target-liveness source check 3. All six focused children passed.",
      "The gate identified KingmakerMountedCombat.dll by SHA-256 dc54bbac8977955e73560f3e2d928d826a85737ef8df7dbdca6b18469df878c2 and MVID b5550b9d-a295-4e4a-803f-e788030bfb44."
    ],
    "inspected": [
      "The mission, AGENTS.md, validation policy, current handoff and resume guidance, all 34 changed files in the feature diff, the developer report and transcript, the controller receipt and log, and the six pending findings.",
      "The production toolbar observation, native slot cleanup, reserve scan, area suspension, archive capture and restore, selected-load fence, and disable paths. Compared their ordering with the toolbar test double and inspected the repository\u2019s existing serialized auto-use fixture.",
      "The capital roster projection and its native GetGroup contract, the mounted attack admission and continuation changes, the 0.2.13 click-handler compatibility path, and the native verification prerequisites in docs/CITY-AND-PRONE-HANDOFF.md."
    ],
    "limitations": [
      "The toolbar tests execute production domain rules through a double that mirrors the native service. Its area-suspension sweep omits the reserve population that production now clears. The pre-commit mutation runs demonstrate sensitivity to the mutated domain or double code.",
      "The area adapter produced the same 63 compilation diagnostics on the base and candidate. That result establishes diagnostic parity only. The separate archive adapter executed with substituted native and service boundaries.",
      "Candidate gameplay was not run. The handoff identifies the missing guarded package operation, capital reader and fixture lineage, reserve portrait/equipment driver, and enemy-posture setup and attack-resolution reader. No installation or save restoration was needed."
    ],
    "required_gaps": [
      "Preserve the latest identifiable binding before area, reserve and legacy-alias cleanup, including a binding created after the first reserve scan. Verify the resulting archive rows and cold restore.",
      "Preserve the identity of surviving duplicate bindings through an abandoned load and remove exact serialized KMC auto-use references during reserve cleanup."
    ],
    "source_inspection_only": [
      "The failure sequences in the findings follow the candidate\u2019s production call paths. They were not executed in Kingmaker.",
      "The capital adapter uses the existing owner-linked pet instance and the native roster. The attack change distinguishes an explicit order against an already-unconscious living enemy from automatic acquisition. Actual portrait selection, equipment interaction and attack rolls remain native observations."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 chosen slot across city and interior transitions",
        "\u00a74 another rider",
        "\u00a77 toolbar transitions"
      ],
      "actual": "Area capture calls CapturePersistentSlots() with absent-intent classification off. SuspendAreaControls then observes current candidates and previously observed departures. RemoveAllManagedFacts(true) additionally sweeps never-observed reserve riders. For this rider, RecordAbsence has no entry to update and ClearManagedHotbarSlots erases slot 9. The area double at tests/MountedControlPlacementIntentTests.cs line 540 still clears only candidates and observed riders.",
      "blocking_rationale": "An ordinary area transition can destroy the only recoverable copy of a player\u2019s chosen slot.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The reserve rider\u2019s slot is recoverable when the same pet returns, independently of the pair that triggered the transfer.",
      "id": "R25-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2851
      },
      "priority": "P1",
      "requested_correction": "Classify newly reached reserve bars before area suspension clears them, or limit the expanded sweep to boundaries that already classify those riders. Preserve the reserve fact cleanup used by save and disable.",
      "requested_evidence": "Start with a never-observed reserve rider, a surviving shortcut at 9, no owned row and another mounted pair. Run area capture and suspension before a reserve update. Verify the subsequent save and party return retain 9 exactly once and preserve an unrelated ability. Exercise the same sweep membership as production.",
      "title": "Area suspension clears reserve bars it never observes",
      "trigger": "A reserve rider with a linked pet and a surviving shortcut becomes available before its first reserve scan. Another mounted pair starts an area transfer."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves and removals",
        "\u00a74 older city saves",
        "\u00a74 save/load"
      ],
      "actual": "With 3 free, native rebinding of the known copy at 9 leaves 3 recorded as interrupted. After the move, EffectivePublicIndex identifies 11 as the player\u2019s binding. AdmitSurvivingBinding declines the existing record and the !live return skips 11. The scan clears 11 and the archive carries 3.",
      "blocking_rationale": "A player move made before the first reserve scan can still be permanently reversed by a city save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The moved binding becomes the remembered slot. With no move, the untouched copy continues to yield to the free slot at 3.",
      "id": "R22-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2453
      },
      "priority": "P2",
      "requested_correction": "Honor an identifiable new binding before clearing the reserve bar even when the previous choice was interrupted. Keep the existing rules for an untouched duplicate and a changed pet.",
      "requested_evidence": "Repeat the rebound reserve case with slot 3 left empty. Compare moving 9 to 11 with leaving 9 untouched. Verify one marker and the correct slot, respectively 11 and 3, in the capital save, same-world return and cold restore.",
      "title": "The first reserve scan ignores a move while the old slot is interrupted",
      "trigger": "Native GetSlot empties 3, CleanupBadSlots rebinds its surviving copy at 9, and the player moves 9 to 11 before the first reserve scan."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 temporary mount unavailability",
        "\u00a74 existing save compatibility"
      ],
      "actual": "EffectivePublicIndex selects 11. With the public fact absent and the pet unresolved, the pre-withdrawal observation is skipped. The inactive repair branch records only a fallback copy. RecoverLegacyAlias declines the moved alias beside canonical slot 7, then the migration loop empties 11. A subsequent save can carry 4. Saving before that update can carry 11.",
      "blocking_rationale": "The saved location of the same player edit depends on whether an update runs before the save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "KMC records 11 before removing the alias and restores one canonical shortcut there when the same pet returns.",
      "id": "R23-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1935
      },
      "priority": "P2",
      "requested_correction": "Before inactive-control migration empties an effective, player-moved alias, record its index as a temporary absence. Preserve the precedence of an untouched copy and the existing companion-retirement check.",
      "requested_evidence": "Extend the moved-alias fixture with the canonical fact absent and the same linked pet unresolved through the first update. Verify update-first and save-first both carry 11 and restore it once. Keep the untouched-alias control.",
      "title": "Alias migration loses a moved shortcut while the public control is unavailable",
      "trigger": "Native cleanup empties canonical slot 4. A known canonical copy remains at 7 and the player moves an older Mount alias from 9 to 11 before the same pet resolves."
    },
    {
      "acceptance_ids": [
        "\u00a74 world disposal and save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "The fence now preserves the placement map through a null Player. Update still calls ReleaseCopiesOfDepartedRiders before checking the fence. CopiesOf clears the duplicate registry when Player is null. After abandonment restores A\u2019s placement map, the surviving canonical slot at 9 is read as a fresh player binding and can replace 3 in the next save.",
      "blocking_rationale": "Canceling a load can change the continuing campaign\u2019s toolbar even when the player did not edit it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Returning to the same player world restores both the interrupted choice and the information that makes the untouched duplicate yield to it.",
      "id": "R24-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2205
      },
      "priority": "P2",
      "requested_correction": "Keep the outgoing world\u2019s copy identities with its placement records during a fenced interval without a Player. Release them on actual replacement, disable or disposal, and protect them from roster pruning during the interval.",
      "requested_evidence": "In A, let GetSlot empty 3 while a known canonical copy survives at 9. Begin a load, update without a Player, reinstate A and abandon. Verify an immediate save and cold restore retain 3. Verify a genuine transition to B releases A\u2019s copy objects.",
      "title": "An abandoned load loses the identity of a surviving duplicate",
      "trigger": "A selected load crosses a frame without a Player while the outgoing rider has a free interrupted slot at 3 and a known duplicate at 9."
    },
    {
      "acceptance_ids": [
        "\u00a74 mod disable/removal cleanup",
        "\u00a74 existing save compatibility",
        "\u00a77 affected save/lifecycle paths"
      ],
      "actual": "The expanded sweep removes facts and hotbar slots. The only code clearing UnitEntityData.AutoUseAbility runs during reconciliation of current candidates. A never-reconciled reserve rider can therefore retain a KMC reference in the serialized m_AutoUseAbility field even with zero KMC facts and slots. The existing Sprint4NaturalScenario fixture explicitly constructs this older-save state without a backing fact.",
      "blocking_rationale": "A capital save can still carry a mod-owned ability reference through the cleanup intended to support mod removal.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Save, disable and disposal cleanup clear an exact retired or managed KMC auto-use selection while preserving an ordinary auto-use selection.",
      "id": "R25-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2854
      },
      "priority": "P2",
      "requested_correction": "Apply the existing exact-blueprint auto-use cleanup to reserve and detached riders at these boundaries, including when there is no matching fact or toolbar slot.",
      "requested_evidence": "Use the existing stale Rider Primary auto-use fixture on a never-observed reserve rider. Inspect the reference inside serialization and after disable. Include a second rider with an ordinary auto-use ability and verify it survives.",
      "title": "Reserve cleanup leaves a serialized KMC auto-use selection behind",
      "trigger": "An older capital save restores a managed auto-use selection on a reserve rider, and the player saves again or disables KMC before the rider returns to the party."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 city save/load",
        "\u00a77 toolbar persistence and intent"
      ],
      "actual": "An ordinary reserve scan clears managed slots but leaves a live fact on a never-observed rider. Once MarkFirst has recorded the unit, the save header skips its bar, and CaptureLiveSlots considers only current candidates. If the live control is placed at 11 after that first scan, the header writes the remembered 9. Serialization then clears 11 and removes the fact.",
      "blocking_rationale": "Fixing first-scan classification alone still leaves a later player placement vulnerable to the next save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A fresh identifiable binding present when saving supersedes the index remembered from an earlier reserve scan.",
      "id": "R25-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2393
      },
      "priority": "P2",
      "requested_correction": "Account for newly present public bindings at the save boundary even on an already-scanned reserve unit. An empty bar left by KMC\u2019s own earlier cleanup must continue to preserve the remembered slot.",
      "requested_evidence": "Run a normal reserve update with a live fact and slot 9, then place that fact at 11 on the same reserve unit and save. Verify 11 and one marker, zero managed facts and slots inside serialization, and one shortcut at 11 after cold restore. Keep a control with no later placement.",
      "title": "A save skips a fresh binding on an already-scanned reserve rider",
      "trigger": "A reserve rider keeps its live public fact after its first ordinary scan, receives a fresh toolbar placement, and stays in reserve until a capital save."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R18-F1",
      "rationale": "CopyYields now makes a known duplicate yield to an explicit off-bar choice. The recorded regression reads duplicate slot 9 after withdrawal and checks a marker-only archive, preserved Attack, and no restored shortcut in both operation orders."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R22-F3",
      "rationale": "The move and removal cases with Fireball occupying 3 now pass. Leaving 3 free keeps the old choice interrupted after native rebind, and the same first scan discards a subsequent move to 11."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R23-F3",
      "rationale": "Migration now preserves the selected alias beside a canonical copy when the public fact is active. The added fixture activates that fact before the move. With the same pet still unresolved, inactive migration empties the selected alias before recording it."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R24-F1",
      "rationale": "The save, disable and disposal fact sweep now includes current-world reserve and detached riders. The focused cases retain the reserve fact until cleanup and inspect facts and slots inside serialization and after disable. The separate area-sweep and auto-use findings concern other effects of those boundaries."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R24-F2",
      "rationale": "The observer synchronizes placement ownership before reading the maps, and the deferred write synchronizes after validating the reporting world. The incoming-world fixture reads B\u2019s slot 9 before restore or update and verifies its archive row and party return."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R24-F3",
      "rationale": "Accepts(null) preserves the outgoing placement, removal and retirement records in the reported fixture. The same null-player update still clears the copy identities needed to interpret a surviving canonical duplicate after abandonment."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r25-44836a",
  "round": 25,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate uses the existing archive rows to remember toolbar intent, adds eligible owners\u2019 persistent pets to the native capital UI roster, and permits an explicit mounted attack order against an already-unconscious living enemy. The two city symptoms have distinct source causes in toolbar lifecycle handling and native roster construction.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After correction, confirm nondefault slot placement, moves and deliberate removal through city visits, an interior transition, party return and cold load.",
      "Select an eligible reserve owner\u2019s pet in Inventory and Character, verify its identity and permitted equipment changes, then reopen the panels after a transition and load.",
      "Use the documented matched longbow setup in real time and turn-based mode. Observe actual attack rolls against standing, conscious-prone and unconscious enemies, along with line of sight, range, ammunition, penalties and action expenditure."
    ],
    "scope_reviewed": "The complete final mission for draft PR #57, including the feature diff from dbf63702 to 923e5ccd, focused offline evidence, native deferral and all six pending findings.",
    "unresolved_questions": [
      "The original report does not identify the weapon or establish whether the enemy was conscious and prone or unconscious. The source trace found a life-state rejection for the latter. Reproduction of the reported shot remains open."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
