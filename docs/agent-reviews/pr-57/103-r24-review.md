<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r24-review part=1/1 sha256=3d3f337d9938675d7ee8b88752d998a979ce7ae02f8a6419de096d6c8f0400db -->
### Codex GPT-6.2 Astra — review — round 24 — candidate `703eebc17c`

**Decision:** `changes_required` — The focused gate passed at the reviewed commit. Source inspection found that a reserve rider’s temporary control ability can survive save cleanup, and that toolbar reconciliation can still reverse a removal or discard a move. Two load-order cases also lose placement records. These defects block the final checkpoint independently of the documented native playtest deferral.

#### Findings

**R24-F1** · P1 · defect · blocks readiness — A reserve rider’s temporary control ability can survive save and disable cleanup
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1280`; acceptance: §4 mod disable/removal cleanup, §4 save/load, §7 affected save/lifecycle paths
- Trigger: Native cleanup rebinds a shortcut to a live Mount / Dismount fact on a rider KMC has never reconciled. The rider is in reserve when a save or disable begins.
- Expected: Save sanitation and disable cleanup remove the exact temporary KMC facts from that rider, including when its shortcut is already gone. Unrelated abilities and the recoverable shortcut arrangement survive.
- Actual: CaptureAndClearManagedHotbarSlots includes reserve riders, but RemoveAllManagedFacts iterates only current candidates and previously observed units. ClearReserveBindings on disable and disposal also clears slots only. The exact ability can therefore remain on the reserve rider when native serialization starts. DepartedRiderReleasesCopies explicitly sets FactLive to false at test line 3188 before saving.
- Why it blocks / does not: A save prepared for mod removal can retain a reference to a temporary KMC ability, and disabling the mod can leave that ability on the rider.
- Requested correction: Include current-world reserve and detached riders in the exact managed-fact sweep at save, disable and disposal. Preserve unrelated facts and record the player’s arrangement before clearing its native binding.
- Requested evidence: Keep the fact live after native cleanup rebinds slot 9 in DepartedRiderReleasesCopies. Assert zero exact managed facts and slots inside serialization and after disable. Cover a rider whose shortcut was already removed, and check that Fireball and the expected archive rows survive.

**R18-F1** · P2 · defect · blocks readiness — Reading the surviving duplicate can undo a deliberate removal
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:131`; acceptance: §4 deliberate removal, §4 temporary mount unavailability, §4 save/load
- Trigger: Native cleanup rebinds 3 beside a duplicate at 9. The player removes 3, the same pet becomes unresolved for one update, and native GetSlot(9) empties the now-stale duplicate before save or return.
- Expected: Reading and emptying the known duplicate preserves the off-bar decision. A save carries only the placement marker.
- Actual: The new code retains the duplicate’s identity, but CopyYields returns false for an off-bar entry because its index is negative. The confirmed GetSlot write consequently calls ObserveBound for 9 and marks it temporarily absent. A save can now carry slot 9, and the same pet’s return restores it.
- Why it blocks / does not: A normal native toolbar read can reverse an explicit removal during temporary pet loss.
- Requested correction: Make a native report for a known duplicate yield to an explicit off-bar arrangement. Continue to recognize a genuinely new placement by its binding identity.
- Requested evidence: Extend the withdrawal case in StaleThenCurrentCanonical with GetSlot(9) after the first pet-absent update. Check save-first and return-first order, a marker-only archive, Attack at 0 and no shortcut after cold restore.

**R22-F3** · P2 · defect · blocks readiness — The first reserve scan can discard an edit made after native rebind
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2419`; acceptance: §4 player moves and removals, §4 older city saves, §4 save/load
- Trigger: Native cleanup rebinds the rider’s effective shortcut at 9 while Fireball occupies 3. The player moves or removes 9 before KMC next reconciles the rider, and the capital save first sees the rider in reserve.
- Expected: The first scan preserves the latest edit before clearing the native bar. A move saves slot 11 and one marker. A removal saves the marker alone.
- Actual: AdmitSurvivingBinding declines an existing entry. The new branch records temporary absence only when the old chosen index equals the effective binding, so it skips a move from 9 to 11 and then clears 11. With the shortcut removed, the empty-bar branch exits even earlier. Live capture skips the reserve rider, and TryGetArrangement omits its still-live record for 9.
- Why it blocks / does not: Saving in the capital can discard the latest placement or omit the marker that suppresses future autofill.
- Requested correction: Reconcile the effective binding or removal against the existing live choice before clearing a first-seen reserve bar. Preserve companion retirement and a genuinely interrupted choice.
- Requested evidence: Extend the rebound-fallback reserve case with a move to 11 and a removal before the first scan. Check archive contents, same-world return and cold load. Keep Fireball at 3 and enable autofill in the removal control.

**R23-F3** · P2 · defect · blocks readiness — Alias migration clears the player’s selected slot when a canonical copy also survives
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1957`; acceptance: §4 player moves, §4 existing save compatibility, §4 save/load
- Trigger: An older bar contains a stale canonical shortcut at 4, a canonical copy at 7 and a callable Mount alias at 9. Native GetSlot empties 4, then the player moves the alias from 9 to 11.
- Expected: Repair writes the canonical control into the selected slot 11 and removes the other copies. Update and save agree on 11.
- Actual: EffectivePublicIndex correctly selects the moved alias at 11. The migration loop nevertheless empties it because SlotsHold sees the canonical copy at 7. Deduplication then removes 7 because 11 was selected. With the public fact continuously live, reconciliation can restore the old interrupted slot 4. Saving before that update can instead preserve 11.
- Why it blocks / does not: An older mixed toolbar can still lose a player move during migration, depending on whether repair or save runs first.
- Requested correction: Migrate the selected effective alias even if another canonical binding scheduled for removal is still present. Use the same effective-slot decision for migration and deduplication.
- Requested evidence: Add a surviving canonical copy at 7 to SurvivingAliasAfterCanonicalCleanup. Use an alias holding its live fact’s current data, move it from 9 to 11, and verify one shortcut at 11 after repeated updates, save-first and cold restore. Retain the untouched-alias control.

**R24-F2** · P2 · defect · blocks readiness — The first update can erase a slot already recovered in the incoming world
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:145`; acceptance: §4 older city saves, §4 save/load, §7 affected lifecycle paths
- Trigger: A selected load replaces player A with B. Before KMC updates or restores archive rows, native UI empties the only stale shortcut on a reserve rider in B.
- Expected: Once a confirmed native report is accepted for the incoming player, that player owns the placement maps. Its recovered slot survives the next update and archive restore.
- Actual: The callback accepts B through the load fence and records the invalidated slot in the live placement maps. It does not update placementWorld. The next SyncPlacementWorld still sees A as the previous owner and clears B’s newly recorded arrangement. At that point both the native slot and the only record of its index are gone.
- Why it blocks / does not: Loading the same older capital save can recover or lose the reserve rider’s shortcut depending on whether native UI reads it before the first KMC update.
- Requested correction: Synchronize placement ownership before the first incoming-world callback reads or writes the maps. Keep outgoing reports confined to their own load scope.
- Requested evidence: Begin a load in A, install B with an older reserve bar and no saved row for its slot 9, then invoke native GetSlot(9) before RestorePersistentSlots or Update. Verify the next capital save carries 9 and its marker, and party return restores one shortcut.

**R24-F3** · P2 · defect · blocks readiness — A missing-player frame releases the records needed by an abandoned load
- Location: `src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs:624`; acceptance: §4 world disposal and save/load, §7 affected lifecycle paths
- Trigger: A selected load starts in A. An enabled update observes no current player, then A becomes current again and the load is abandoned.
- Expected: A temporary interval without a player keeps the selected load fenced. Abandoning the load with its original player current restores that player’s records.
- Actual: SyncPlacementWorld explicitly preserves a fenced load while Player is null. Later in the same enabled update, ArrangementsAccepted calls Accepts(null). That method treats null as a replacement, calls Release and discards the outgoing records. AbandonLoadedWorld can no longer restore them when A returns.
- Why it blocks / does not: A failed or canceled load can erase a reserve placement or deliberate removal in the campaign the player resumes.
- Requested correction: Keep the outgoing records fenced during a missing-player interval. Release them when a distinct incoming player is established or the owning load scope resolves.
- Requested evidence: Extend the abandoned-load control with an update while Player is null, then reinstate A and abandon the load. Verify its reserve slot, removal marker and retirement survive. Keep the unfenced main-menu disposal control.

#### Prior finding assessments

- **R18-F1** — `fix_rejected` — The direct save and return cases now pass because the duplicate retains its identity. A native GetSlot call on that same duplicate still promotes it over the recorded removal.
- **R22-F3** — `fix_rejected` — The new equality branch preserves the unchanged rebound slot 9, as the gate confirms. The first reserve scan still loses the effective binding when a player edit makes its index differ from the previously observed choice.
- **R23-F1** — `fix_verified` — The native report now yields when another binding is effective. The focused regression covers stale 9 beside callable 7 with Fireball at 3, including update-first, save-first and cold restore.
- **R23-F2** — `fix_verified` — The weak player-world check releases the outgoing placement and retirement maps on the unfenced main-menu and direct new-campaign paths. MainMenuNewGameReleasesRecords passed. R24-F2 and R24-F3 concern placement loss during selected loads.
- **R23-F3** — `fix_rejected` — The classifier now recognizes the moved alias, and the two-binding regression passed. With a surviving canonical copy, the migration step still clears the selected alias and loses the move.

#### Evidence assessment

- **Inspected:** The controller-supplied mission, full 8,420-line feature diff, changed-file list, prior findings, developer report, tool transcript and focused gate artifacts.; AGENTS.md, docs/VALIDATION-POLICY.md, CURRENT.md, AUTONOMOUS-RESUME.md and the mission handoff, including its acceptance table and owner playtest.; Toolbar placement, native slot cleanup, reserve admission, save sanitation and load ownership in the production service and its component double. Also reviewed the city roster projection, mounted attack admission and execution paths, archive changes and compatibility checks.
- **Executed verification:** Confirmed the detached HEAD, mission digest, focused receipt and matching log digest. The developer transcript records the guarded push and the controller’s focused verification at 703eebc17cdd8476d4dfc095f638caafa3251ad1. The gate exited 0 in 25.8 seconds with the worktree unchanged.; The recorded Fast run built the candidate and passed 1,505 component checks, 153 source checks and all six children of Test-CityToolbarProneFocused.ps1. The component run included 85 toolbar, six city-roster and three downed-target tests.; The focused log records 825 assembly-contract checks, 21 Tabletop Rules Expansion click-composition checks across both patch orders, nine archive-codec checks, 22 six-pair archive-adapter checks and three target-liveness source checks. The tested DLL had MVID 038685c1-83e7-4a2e-9ecb-06cfa24ac9c5.; The area-restore adapter comparison found the same 63 compiler diagnostics in the base and candidate. The comparison passed. Neither adapter executed.
- **Source inspection only:** The two city reports share the capital roster transition. Their failure mechanisms differ. Native toolbar cleanup discards invalid ability data, while the full-screen roster obtains pets from a collection that omits some reserve owners’ out-of-game companions.; The city projection returns the owner’s existing reciprocal pet instance. The combat change carries an explicit order against an already-unconscious living enemy through mounted target checks. The conscious-prone path has no separate rejection identified by the source trace.
- **Required gaps:** Correct reserve-rider fact sanitation and verify the actual roster covered inside the save scope and on disable.; Verify that native reads, alias migration and the first reserve scan preserve the player’s last toolbar edit in the sequences identified below.; Verify placement ownership when an incoming world produces a native slot report before its first update, and when an abandoned load passes through a frame without a player.
- **Limitations:** The reviewer sandbox was read-only. Executed results above come from the controller receipt, log and developer transcript. The additional failure sequences below were traced in source.; The toolbar tests drive production domain decisions through a double that mirrors the service. The recorded mutation run changed that double. It demonstrates that its assertions detect the five mutations.; Native city panels, equipment changes, toolbar transitions and attack rolls were not run on this candidate. The handoff identifies the unavailable commit-bound package operation, the older capital reader and the missing panel and enemy-posture driver cases. No native transaction required restoration.

#### Summary

- **Delivered behavior:** The candidate remembers interrupted toolbar placements, projects eligible owners’ existing pets into the capital character and inventory roster, and allows an explicit mounted attack to retain a living enemy that was already unconscious when ordered. The toolbar and save-lifecycle defects above remain.
- **Scope reviewed:** Complete final mission on branch codex/mounted-city-and-prone-fixes, enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c, at feature commit 703eebc17cdd8476d4dfc095f638caafa3251ad1 against dbf637021db42cdc9bf27f3d79fc255d4569c788. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)
- **Pending later-phase work:** none
- **Unresolved questions:** The original report does not identify the ranged weapon or establish whether the enemy was conscious and tripped or already unconscious. The source establishes a mounted rejection for the latter. Reproduction of the owner’s precise case remains open.
- **Remaining owner review:** After the source corrections, verify chosen slots, moves and removals through actual capital entry, interior travel, save/load and return. Inspect a reserve owner’s exact pet in Character and Inventory, change permitted equipment, and reopen both panels after loading.; Use the handoff’s matched longbow setup to verify actual rider attack rolls against standing, conscious-prone and already-unconscious enemies in real time and turn-based mode. Check range, walls, unavailable actions, ammunition, penalties and native action expenditure.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a20097-6fac-76f3-9e93-5b1f8d1f9bd8`, duration 1073.5s, response sha256 `0cb9c9c75fc118ec9a393c3cf88c061f00ace15a5a685185d9504908ee165d27`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r24-d664fb`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "703eebc17cdd8476d4dfc095f638caafa3251ad1",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed at the reviewed commit. Source inspection found that a reserve rider\u2019s temporary control ability can survive save cleanup, and that toolbar reconciliation can still reverse a removal or discard a move. Two load-order cases also lose placement records. These defects block the final checkpoint independently of the documented native playtest deferral.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the detached HEAD, mission digest, focused receipt and matching log digest. The developer transcript records the guarded push and the controller\u2019s focused verification at 703eebc17cdd8476d4dfc095f638caafa3251ad1. The gate exited 0 in 25.8 seconds with the worktree unchanged.",
      "The recorded Fast run built the candidate and passed 1,505 component checks, 153 source checks and all six children of Test-CityToolbarProneFocused.ps1. The component run included 85 toolbar, six city-roster and three downed-target tests.",
      "The focused log records 825 assembly-contract checks, 21 Tabletop Rules Expansion click-composition checks across both patch orders, nine archive-codec checks, 22 six-pair archive-adapter checks and three target-liveness source checks. The tested DLL had MVID 038685c1-83e7-4a2e-9ecb-06cfa24ac9c5.",
      "The area-restore adapter comparison found the same 63 compiler diagnostics in the base and candidate. The comparison passed. Neither adapter executed."
    ],
    "inspected": [
      "The controller-supplied mission, full 8,420-line feature diff, changed-file list, prior findings, developer report, tool transcript and focused gate artifacts.",
      "AGENTS.md, docs/VALIDATION-POLICY.md, CURRENT.md, AUTONOMOUS-RESUME.md and the mission handoff, including its acceptance table and owner playtest.",
      "Toolbar placement, native slot cleanup, reserve admission, save sanitation and load ownership in the production service and its component double. Also reviewed the city roster projection, mounted attack admission and execution paths, archive changes and compatibility checks."
    ],
    "limitations": [
      "The reviewer sandbox was read-only. Executed results above come from the controller receipt, log and developer transcript. The additional failure sequences below were traced in source.",
      "The toolbar tests drive production domain decisions through a double that mirrors the service. The recorded mutation run changed that double. It demonstrates that its assertions detect the five mutations.",
      "Native city panels, equipment changes, toolbar transitions and attack rolls were not run on this candidate. The handoff identifies the unavailable commit-bound package operation, the older capital reader and the missing panel and enemy-posture driver cases. No native transaction required restoration."
    ],
    "required_gaps": [
      "Correct reserve-rider fact sanitation and verify the actual roster covered inside the save scope and on disable.",
      "Verify that native reads, alias migration and the first reserve scan preserve the player\u2019s last toolbar edit in the sequences identified below.",
      "Verify placement ownership when an incoming world produces a native slot report before its first update, and when an abandoned load passes through a frame without a player."
    ],
    "source_inspection_only": [
      "The two city reports share the capital roster transition. Their failure mechanisms differ. Native toolbar cleanup discards invalid ability data, while the full-screen roster obtains pets from a collection that omits some reserve owners\u2019 out-of-game companions.",
      "The city projection returns the owner\u2019s existing reciprocal pet instance. The combat change carries an explicit order against an already-unconscious living enemy through mounted target checks. The conscious-prone path has no separate rejection identified by the source trace."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 mod disable/removal cleanup",
        "\u00a74 save/load",
        "\u00a77 affected save/lifecycle paths"
      ],
      "actual": "CaptureAndClearManagedHotbarSlots includes reserve riders, but RemoveAllManagedFacts iterates only current candidates and previously observed units. ClearReserveBindings on disable and disposal also clears slots only. The exact ability can therefore remain on the reserve rider when native serialization starts. DepartedRiderReleasesCopies explicitly sets FactLive to false at test line 3188 before saving.",
      "blocking_rationale": "A save prepared for mod removal can retain a reference to a temporary KMC ability, and disabling the mod can leave that ability on the rider.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Save sanitation and disable cleanup remove the exact temporary KMC facts from that rider, including when its shortcut is already gone. Unrelated abilities and the recoverable shortcut arrangement survive.",
      "id": "R24-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1280
      },
      "priority": "P1",
      "requested_correction": "Include current-world reserve and detached riders in the exact managed-fact sweep at save, disable and disposal. Preserve unrelated facts and record the player\u2019s arrangement before clearing its native binding.",
      "requested_evidence": "Keep the fact live after native cleanup rebinds slot 9 in DepartedRiderReleasesCopies. Assert zero exact managed facts and slots inside serialization and after disable. Cover a rider whose shortcut was already removed, and check that Fireball and the expected archive rows survive.",
      "title": "A reserve rider\u2019s temporary control ability can survive save and disable cleanup",
      "trigger": "Native cleanup rebinds a shortcut to a live Mount / Dismount fact on a rider KMC has never reconciled. The rider is in reserve when a save or disable begins."
    },
    {
      "acceptance_ids": [
        "\u00a74 deliberate removal",
        "\u00a74 temporary mount unavailability",
        "\u00a74 save/load"
      ],
      "actual": "The new code retains the duplicate\u2019s identity, but CopyYields returns false for an off-bar entry because its index is negative. The confirmed GetSlot write consequently calls ObserveBound for 9 and marks it temporarily absent. A save can now carry slot 9, and the same pet\u2019s return restores it.",
      "blocking_rationale": "A normal native toolbar read can reverse an explicit removal during temporary pet loss.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Reading and emptying the known duplicate preserves the off-bar decision. A save carries only the placement marker.",
      "id": "R18-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 131
      },
      "priority": "P2",
      "requested_correction": "Make a native report for a known duplicate yield to an explicit off-bar arrangement. Continue to recognize a genuinely new placement by its binding identity.",
      "requested_evidence": "Extend the withdrawal case in StaleThenCurrentCanonical with GetSlot(9) after the first pet-absent update. Check save-first and return-first order, a marker-only archive, Attack at 0 and no shortcut after cold restore.",
      "title": "Reading the surviving duplicate can undo a deliberate removal",
      "trigger": "Native cleanup rebinds 3 beside a duplicate at 9. The player removes 3, the same pet becomes unresolved for one update, and native GetSlot(9) empties the now-stale duplicate before save or return."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves and removals",
        "\u00a74 older city saves",
        "\u00a74 save/load"
      ],
      "actual": "AdmitSurvivingBinding declines an existing entry. The new branch records temporary absence only when the old chosen index equals the effective binding, so it skips a move from 9 to 11 and then clears 11. With the shortcut removed, the empty-bar branch exits even earlier. Live capture skips the reserve rider, and TryGetArrangement omits its still-live record for 9.",
      "blocking_rationale": "Saving in the capital can discard the latest placement or omit the marker that suppresses future autofill.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The first scan preserves the latest edit before clearing the native bar. A move saves slot 11 and one marker. A removal saves the marker alone.",
      "id": "R22-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2419
      },
      "priority": "P2",
      "requested_correction": "Reconcile the effective binding or removal against the existing live choice before clearing a first-seen reserve bar. Preserve companion retirement and a genuinely interrupted choice.",
      "requested_evidence": "Extend the rebound-fallback reserve case with a move to 11 and a removal before the first scan. Check archive contents, same-world return and cold load. Keep Fireball at 3 and enable autofill in the removal control.",
      "title": "The first reserve scan can discard an edit made after native rebind",
      "trigger": "Native cleanup rebinds the rider\u2019s effective shortcut at 9 while Fireball occupies 3. The player moves or removes 9 before KMC next reconciles the rider, and the capital save first sees the rider in reserve."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 existing save compatibility",
        "\u00a74 save/load"
      ],
      "actual": "EffectivePublicIndex correctly selects the moved alias at 11. The migration loop nevertheless empties it because SlotsHold sees the canonical copy at 7. Deduplication then removes 7 because 11 was selected. With the public fact continuously live, reconciliation can restore the old interrupted slot 4. Saving before that update can instead preserve 11.",
      "blocking_rationale": "An older mixed toolbar can still lose a player move during migration, depending on whether repair or save runs first.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Repair writes the canonical control into the selected slot 11 and removes the other copies. Update and save agree on 11.",
      "id": "R23-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1957
      },
      "priority": "P2",
      "requested_correction": "Migrate the selected effective alias even if another canonical binding scheduled for removal is still present. Use the same effective-slot decision for migration and deduplication.",
      "requested_evidence": "Add a surviving canonical copy at 7 to SurvivingAliasAfterCanonicalCleanup. Use an alias holding its live fact\u2019s current data, move it from 9 to 11, and verify one shortcut at 11 after repeated updates, save-first and cold restore. Retain the untouched-alias control.",
      "title": "Alias migration clears the player\u2019s selected slot when a canonical copy also survives",
      "trigger": "An older bar contains a stale canonical shortcut at 4, a canonical copy at 7 and a callable Mount alias at 9. Native GetSlot empties 4, then the player moves the alias from 9 to 11."
    },
    {
      "acceptance_ids": [
        "\u00a74 older city saves",
        "\u00a74 save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "The callback accepts B through the load fence and records the invalidated slot in the live placement maps. It does not update placementWorld. The next SyncPlacementWorld still sees A as the previous owner and clears B\u2019s newly recorded arrangement. At that point both the native slot and the only record of its index are gone.",
      "blocking_rationale": "Loading the same older capital save can recover or lose the reserve rider\u2019s shortcut depending on whether native UI reads it before the first KMC update.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once a confirmed native report is accepted for the incoming player, that player owns the placement maps. Its recovered slot survives the next update and archive restore.",
      "id": "R24-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 145
      },
      "priority": "P2",
      "requested_correction": "Synchronize placement ownership before the first incoming-world callback reads or writes the maps. Keep outgoing reports confined to their own load scope.",
      "requested_evidence": "Begin a load in A, install B with an older reserve bar and no saved row for its slot 9, then invoke native GetSlot(9) before RestorePersistentSlots or Update. Verify the next capital save carries 9 and its marker, and party return restores one shortcut.",
      "title": "The first update can erase a slot already recovered in the incoming world",
      "trigger": "A selected load replaces player A with B. Before KMC updates or restores archive rows, native UI empties the only stale shortcut on a reserve rider in B."
    },
    {
      "acceptance_ids": [
        "\u00a74 world disposal and save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "SyncPlacementWorld explicitly preserves a fenced load while Player is null. Later in the same enabled update, ArrangementsAccepted calls Accepts(null). That method treats null as a replacement, calls Release and discards the outgoing records. AbandonLoadedWorld can no longer restore them when A returns.",
      "blocking_rationale": "A failed or canceled load can erase a reserve placement or deliberate removal in the campaign the player resumes.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A temporary interval without a player keeps the selected load fenced. Abandoning the load with its original player current restores that player\u2019s records.",
      "id": "R24-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedControlPlacementIntent.cs",
        "line": 624
      },
      "priority": "P2",
      "requested_correction": "Keep the outgoing records fenced during a missing-player interval. Release them when a distinct incoming player is established or the owning load scope resolves.",
      "requested_evidence": "Extend the abandoned-load control with an update while Player is null, then reinstate A and abandon the load. Verify its reserve slot, removal marker and retirement survive. Keep the unfenced main-menu disposal control.",
      "title": "A missing-player frame releases the records needed by an abandoned load",
      "trigger": "A selected load starts in A. An enabled update observes no current player, then A becomes current again and the load is abandoned."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R18-F1",
      "rationale": "The direct save and return cases now pass because the duplicate retains its identity. A native GetSlot call on that same duplicate still promotes it over the recorded removal."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R22-F3",
      "rationale": "The new equality branch preserves the unchanged rebound slot 9, as the gate confirms. The first reserve scan still loses the effective binding when a player edit makes its index differ from the previously observed choice."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R23-F1",
      "rationale": "The native report now yields when another binding is effective. The focused regression covers stale 9 beside callable 7 with Fireball at 3, including update-first, save-first and cold restore."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R23-F2",
      "rationale": "The weak player-world check releases the outgoing placement and retirement maps on the unfenced main-menu and direct new-campaign paths. MainMenuNewGameReleasesRecords passed. R24-F2 and R24-F3 concern placement loss during selected loads."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R23-F3",
      "rationale": "The classifier now recognizes the moved alias, and the two-binding regression passed. With a surviving canonical copy, the migration step still clears the selected alias and loses the move."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r24-d664fb",
  "round": 24,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate remembers interrupted toolbar placements, projects eligible owners\u2019 existing pets into the capital character and inventory roster, and allows an explicit mounted attack to retain a living enemy that was already unconscious when ordered. The toolbar and save-lifecycle defects above remain.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "After the source corrections, verify chosen slots, moves and removals through actual capital entry, interior travel, save/load and return. Inspect a reserve owner\u2019s exact pet in Character and Inventory, change permitted equipment, and reopen both panels after loading.",
      "Use the handoff\u2019s matched longbow setup to verify actual rider attack rolls against standing, conscious-prone and already-unconscious enemies in real time and turn-based mode. Check range, walls, unavailable actions, ammunition, penalties and native action expenditure."
    ],
    "scope_reviewed": "Complete final mission on branch codex/mounted-city-and-prone-fixes, enrollment kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c, at feature commit 703eebc17cdd8476d4dfc095f638caafa3251ad1 against dbf637021db42cdc9bf27f3d79fc255d4569c788. [Draft PR #57](https://github.com/howardreith/KingmakerMountedCombat/pull/57)",
    "unresolved_questions": [
      "The original report does not identify the ranged weapon or establish whether the enemy was conscious and tripped or already unconscious. The source establishes a mounted rejection for the latter. Reproduction of the owner\u2019s precise case remains open."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
