<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c key=r26-review part=1/1 sha256=02994c5522fdb077d590fbd179690a2c93210c7e2d02d944b3820b8bd4be4c93 -->
### Codex GPT-6.2 Astra — review — round 26 — candidate `6298f2a527`

**Decision:** `changes_required` — The focused gate passed on the reviewed commit. Source inspection still shows three ways to lose a deliberate reserve-toolbar placement. Save and disable cleanup can also leave managed ability references on persistent actors, and returning to the main menu can retain a discarded player world. These are required lifecycle behaviors under mission §4.

#### Findings

**R26-F4** · P1 · defect · blocks readiness — Save and disable cleanup skip a reserve owner's persistent pet
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2681`; acceptance: §4 existing save compatibility, §4 mod disable/removal cleanup, §7 affected save/lifecycle paths
- Trigger: An older capital save restores a remote owner whose horse holds a legacy Mount Primary reference. The player saves again or disables KMC before taking the owner adventuring.
- Expected: Cleanup reaches the current owner's persistent pet and clears the managed reference while preserving ordinary abilities and equipment.
- Actual: CollectCurrentCandidateUnits follows a travelling owner's pet. ReserveRosterUnits adds only the remote or detached roster entry itself. The recorded capital roster shape puts the owner in RemoteCompanions and the horse outside that list. On a cold load with no active pair or previous observation, the horse is absent from both the slot and fact sweeps and from the new auto-use cleanup.
- Why it blocks / does not: An older capital save can carry an exact KMC ability reference on the persistent animal through serialization or mod disable.
- Requested correction: Include the exact current-world pets of reserve and detached owners in the actor set used for sanitation. Keep toolbar placement eligibility scoped to riders.
- Requested evidence: Use the capital fixture's separate remote owner and horse membership. Put a stale Mount Primary slot and auto-use selection on the horse with no backing fact. Inspect both inside serialization and after disable, alongside an ordinary ability that must survive.

**R22-F3** · P2 · defect · blocks readiness — The first reserve scan loses a fresh placement when the old shortcut had no duplicate
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2487`; acceptance: §4 player moves, §4 older city saves, §4 save/load
- Trigger: Native cleanup empties the rider's only shortcut. The player places the current control elsewhere before KMC first scans the reserve bar.
- Expected: The new placement at 11 becomes the recoverable choice. Leaving the bar untouched preserves 3.
- Actual: Native invalidation records slot 3 as interrupted. With no sibling binding, RecordCopies discards the empty copy list. EffectivePublicIndex subsequently finds the player's canonical binding at 11, but ClassifyFirstSeenReserveBar requires HasRecordedCopies to accept it. The scan clears 11 and the archive carries 3.
- Why it blocks / does not: A player with a single shortcut can still have a deliberate new placement reversed by the first reserve scan.
- Requested correction: Preserve enough information about the observed bar to recognize a subsequent placement when the interrupted shortcut had zero siblings. Retain the existing precedence for untouched duplicates and older archive rows.
- Requested evidence: Start with only slot 3 on a never-observed reserve rider. Let GetSlot empty it, place the current live control at 11 and save before the first reserve scan. Check 11 and one placement marker in the archive and one shortcut at 11 after both same-world return and cold restore. Keep the no-edit and duplicate controls.

**R25-F3** · P2 · defect · blocks readiness — A scanned reserve rider cannot establish a fresh shortcut after companion replacement
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2519`; acceptance: §4 player moves, §4 genuine companion removal, §4 city save/load
- Trigger: A reserve rider's old arrangement has been retired, and the player places the live control for the replacement companion before saving.
- Expected: The former companion's slot stays retired. A deliberate placement made after that retirement establishes a new arrangement for the current companion.
- Actual: After RevalidateAbsentArrangements retires the old pet's arrangement, the already-scanned bar can receive a new binding for the replacement pet. AdmitSurvivingBinding declines a retired actor and ClassifyFreshReserveBinding immediately returns. Its caller empties the new slot, leaving no recoverable slot row for it.
- Why it blocks / does not: Retiring the former pet's arrangement prevents a subsequent player choice for the replacement pet from surviving the save.
- Requested correction: Allow an identifiable fresh binding on a previously cleared reserve bar to establish a new arrangement after retirement. Continue rejecting stale bindings and archive replays from the former companion.
- Requested evidence: Scan a reserve rider with a live fact at 9, replace pet A with pet B, and update with the bar empty so A's arrangement retires. Place the live control at 11, save and cold-restore. Require one shortcut at 11 for B. With no new placement, require no restoration at 9.

**R26-F1** · P2 · defect · blocks readiness — Canceling a load changes how the next save classifies an edited reserve bar
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:1778`; acceptance: §4 player moves, §4 save/load, §7 affected lifecycle paths
- Trigger: KMC scans a reserve rider and clears slot 9. The player places the live ability at 11, starts a load, and returns to the same world before KMC observes the edit.
- Expected: Returning to the same player world preserves the player's edit and the information needed to recognize it.
- Actual: BeginSelectedLoad resets reserveScan. AbandonLoadedWorld restores the outgoing placement records but omits the fact that KMC already scanned and cleared this bar. On the next save, slot 11 takes the first-scan path and loses to the interrupted record at 9. The component double's BeginLoad at tests/MountedControlPlacementIntentTests.cs line 589 omits the production reset.
- Why it blocks / does not: Attempting and canceling a load can change which shortcut the continuing campaign saves.
- Requested correction: Preserve the outgoing world's reserve classification through an abandoned load, or record the pending player edit before discarding that classification. Release outgoing state on genuine world replacement.
- Requested evidence: Extend the scanned-reserve 9-to-11 case with a selected load abandoned before world replacement. Exercise the production reset and abandonment sequence. Verify that an immediate save carries 11 and one marker, cold restore returns 11 once, and the no-edit control still carries 9.

**R26-F2** · P2 · defect · blocks readiness — The duplicate registry retains a discarded Player at the main menu
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2220`; acceptance: §4 world disposal, §4 discarded entity cleanup
- Trigger: A world with a recorded duplicate is disposed without a selected load, and the player stays at the main menu.
- Expected: A temporarily missing Player during a selected load keeps the outgoing copies available for abandonment. Unfenced disposal releases them when the world goes away.
- Actual: CopiesOf retains recordedCopiesWorld whenever the current Player is null. ReleaseCopiesOfDepartedRiders also returns on null. SyncPlacementWorld clears the placement maps on an unfenced main-menu transition, but the separate dictionary still holds the slot objects and a strong reference to the discarded Player.
- Why it blocks / does not: The control service can retain a disposed campaign for the entire main-menu interval.
- Requested correction: Tie retention during a null-Player interval to the outgoing selected-load fence. Release the registry and its world reference on an unfenced disposal.
- Requested evidence: Record a duplicate on a never-observed reserve rider, return directly to the main menu before its reserve scan, and update with no replacement Player. Assert zero retained copies and no held world while still at the menu. Keep the fenced abandonment and real-transition controls.

**R26-F3** · P2 · defect · blocks readiness — A newly returned party rider can carry a retired auto-use selection through save or disable
- Location: `src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs:2937`; acceptance: §4 mod disable/removal cleanup, §4 existing save compatibility, §7 affected save paths
- Trigger: A reserve rider rejoins the travelling party with a stale KMC auto-use selection, and save or disable runs before the next control update.
- Expected: The sanitation boundary clears an exact managed auto-use reference on the rider regardless of current roster membership. Ordinary auto-use selections survive.
- Actual: RemoveAllManagedFacts clears AutoUseAbility only in reserveUnits, which explicitly excludes current candidates. A returning rider with an older Rider Primary selection and no backing fact or slot depends on a later ReconcileFacts update. An immediate save can serialize m_AutoUseAbility first. The disabled-save path also skips ordinary reconciliation.
- Why it blocks / does not: Whether a save or disable removes a serialized KMC reference depends on whether one control update ran after the roster change.
- Requested correction: Apply exact-blueprint auto-use sanitation to current riders reached by the save or disable sweep, including when the selection has no backing fact or toolbar slot.
- Requested evidence: Move the stale Rider Primary fixture from reserve to travelling party immediately before save and, separately, disable. Inspect the selection inside serialization and after disable. Extend DisabledSaveIsolation with the same stale selection on a party rider and retain an ordinary auto-use control.

#### Prior finding assessments

- **R22-F3** — `fix_rejected` — The moved and untouched duplicate cases now pass with slot 3 free. The same first-scan decision still discards a fresh canonical placement when native cleanup observed a bar containing just one shortcut.
- **R23-F3** — `fix_verified` — Inactive-control reconciliation records the effective moved alias before migration clears it. SurvivingAliasAfterCanonicalCleanup covers update-first and save-first order, restoration at 11 and the untouched-alias control.
- **R24-F3** — `fix_verified` — The selected-load path preserves the duplicate identity across a null-Player frame, recovers slot 3 when A returns, and releases A's copies when B replaces it. R26-F2 concerns disposal without a selected load.
- **R25-F1** — `fix_verified` — SuspendAreaControls now classifies reserve bars before the sweep. ReserveBoundaryCoverage starts with another transferring pair and verifies that the reserve rider recovers slot 9 once and retains Fireball at 3.
- **R25-F2** — `fix_verified` — For a rider who remains in reserve, the cleanup now checks the exact auto-use blueprint independently of the fact and slot. The focused case inspects the selection during serialization and after disable and preserves an ordinary selection.
- **R25-F3** — `fix_rejected` — The same-pet 9-to-11 case now passes, including the no-edit control. The new fresh-binding path still clears a deliberate placement made after the previous companion's arrangement was retired.

#### Evidence assessment

- **Inspected:** The mission, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, all 8,918 lines of the feature diff, developer report and transcript, focused receipt and log, and prior findings ledger.; Toolbar observation, reserve classification, native slot invalidation, archive capture and restore, selected-load abandonment, area suspension, save and disable cleanup, and their component fixtures.; The capital roster adapter and projection, the existing capital fixture's owner and horse membership, the mounted click and pair-command paths, the composition checks and the acceptance handoff.
- **Executed verification:** Confirmed the detached HEAD and mission hash. The controller receipt names the same commit, reports an unchanged worktree and exit 0, and matches the focused log SHA-256 d31a9c6ecca84800b6299648b2bb4ceb46a9324475e438206915199f0e4f5ac3.; The recorded run of scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 completed in 26.3 seconds. It passed 1,506 component checks, 153 source checks and all six focused children. The product MVID was d7934608-a9a6-4d92-91e4-b2f13b3da5e1.; The focused log records 825 assembly-contract checks, 21 Tabletop Rules Expansion click-composition checks covering both patch orders, nine archive-row checks, 22 six-pair archive-adapter checks and three target-liveness source checks.; The developer transcript confirms that three pre-commit mutation runs produced four, one and one targeted failures respectively. Those mutations exercised the test double. The committed candidate has a separate passing receipt.
- **Source inspection only:** The six findings below follow production control flow. The recorded gate does not exercise their triggering sequences. In particular, the toolbar double omits the reserve-scan reset performed by production BeginSelectedLoad.; The capital UI trace establishes the roster and persistent-instance selection path. The combat trace identifies the mounted rejection of an explicit order on an already-unconscious enemy. Actual inventory interaction, conscious-prone click behavior and attack resolution remain unobserved on this build.
- **Required gaps:** Preserve fresh reserve placements after a sole-slot invalidation, after companion retirement, and after an abandoned load in the same world.; Release recorded slot objects on unfenced world disposal, and clear exact managed references on returning riders and persistent pets of reserve owners at save and disable boundaries.
- **Limitations:** The review used read-only inspection. The controller log supplies the build and test results. The area adapter produced the same 63 compile diagnostics at the base and candidate, so its in-world assertion did not execute.; No native transaction ran on this candidate. The handoff identifies the unavailable candidate-package step, the older capital reader, and the missing reserve-pet equipment and target-posture drivers. Mission §7 permits the documented owner verification. Restoration was inapplicable.

#### Summary

- **Delivered behavior:** The candidate records toolbar intent through temporary fact loss, projects eligible owners' persistent pets into the capital inventory and character roster, and admits an explicit mounted attack on an enemy already unconscious when ordered. The two city reports share the capital roster transition as a trigger. The toolbar loss comes from withdrawing the control fact, while the missing pet portrait comes from the native UI's controllable-character filter.
- **Scope reviewed:** The complete final mission for draft PR #57, including the feature diff from dbf63702 to 6298f2a5, all six pending findings, focused validation and native-test deferrals.
- **Pending later-phase work:** none
- **Unresolved questions:** The reported weapon and the target's consciousness at the original click remain unknown. The source trace found the unconscious-target refusal. Whether clicking a conscious prone enemy reproduces a mounted-specific failure still requires the matched in-game comparison.
- **Remaining owner review:** Verify the toolbar on mounted and dismounted capital visits, interior transitions, forced dismount, save/load and return to adventuring.; Select a reserve owner's pet in Inventory and Character, confirm its identity and equipment, make a permitted equipment change and verify it after reopening and reloading.; Compare actual longbow resolution against standing, conscious-tripped and already-unconscious enemies at the documented near and normal distances in real time and turn based. Check roll counts, action costs, line of sight, approach, penalties and the mounted melee control.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a200c7-83e7-77b3-a1a2-57d06cfa1699`, duration 1110.7s, response sha256 `6d0d2ed7224d89fc2515912c1ea8143526d1c2735faac408005984f8f757a69d`, request `kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r26-b91e50`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "6298f2a5274306e35f8f0ceaf57869693cd0611f",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the reviewed commit. Source inspection still shows three ways to lose a deliberate reserve-toolbar placement. Save and disable cleanup can also leave managed ability references on persistent actors, and returning to the main menu can retain a discarded player world. These are required lifecycle behaviors under mission \u00a74.",
  "evidence_assessment": {
    "executed_verification": [
      "Confirmed the detached HEAD and mission hash. The controller receipt names the same commit, reports an unchanged worktree and exit 0, and matches the focused log SHA-256 d31a9c6ecca84800b6299648b2bb4ceb46a9324475e438206915199f0e4f5ac3.",
      "The recorded run of scripts/Test-Tiers.ps1 -Tier Fast -Focused Test-CityToolbarProneFocused.ps1 completed in 26.3 seconds. It passed 1,506 component checks, 153 source checks and all six focused children. The product MVID was d7934608-a9a6-4d92-91e4-b2f13b3da5e1.",
      "The focused log records 825 assembly-contract checks, 21 Tabletop Rules Expansion click-composition checks covering both patch orders, nine archive-row checks, 22 six-pair archive-adapter checks and three target-liveness source checks.",
      "The developer transcript confirms that three pre-commit mutation runs produced four, one and one targeted failures respectively. Those mutations exercised the test double. The committed candidate has a separate passing receipt."
    ],
    "inspected": [
      "The mission, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, all 8,918 lines of the feature diff, developer report and transcript, focused receipt and log, and prior findings ledger.",
      "Toolbar observation, reserve classification, native slot invalidation, archive capture and restore, selected-load abandonment, area suspension, save and disable cleanup, and their component fixtures.",
      "The capital roster adapter and projection, the existing capital fixture's owner and horse membership, the mounted click and pair-command paths, the composition checks and the acceptance handoff."
    ],
    "limitations": [
      "The review used read-only inspection. The controller log supplies the build and test results. The area adapter produced the same 63 compile diagnostics at the base and candidate, so its in-world assertion did not execute.",
      "No native transaction ran on this candidate. The handoff identifies the unavailable candidate-package step, the older capital reader, and the missing reserve-pet equipment and target-posture drivers. Mission \u00a77 permits the documented owner verification. Restoration was inapplicable."
    ],
    "required_gaps": [
      "Preserve fresh reserve placements after a sole-slot invalidation, after companion retirement, and after an abandoned load in the same world.",
      "Release recorded slot objects on unfenced world disposal, and clear exact managed references on returning riders and persistent pets of reserve owners at save and disable boundaries."
    ],
    "source_inspection_only": [
      "The six findings below follow production control flow. The recorded gate does not exercise their triggering sequences. In particular, the toolbar double omits the reserve-scan reset performed by production BeginSelectedLoad.",
      "The capital UI trace establishes the roster and persistent-instance selection path. The combat trace identifies the mounted rejection of an explicit order on an already-unconscious enemy. Actual inventory interaction, conscious-prone click behavior and attack resolution remain unobserved on this build."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74 existing save compatibility",
        "\u00a74 mod disable/removal cleanup",
        "\u00a77 affected save/lifecycle paths"
      ],
      "actual": "CollectCurrentCandidateUnits follows a travelling owner's pet. ReserveRosterUnits adds only the remote or detached roster entry itself. The recorded capital roster shape puts the owner in RemoteCompanions and the horse outside that list. On a cold load with no active pair or previous observation, the horse is absent from both the slot and fact sweeps and from the new auto-use cleanup.",
      "blocking_rationale": "An older capital save can carry an exact KMC ability reference on the persistent animal through serialization or mod disable.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Cleanup reaches the current owner's persistent pet and clears the managed reference while preserving ordinary abilities and equipment.",
      "id": "R26-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2681
      },
      "priority": "P1",
      "requested_correction": "Include the exact current-world pets of reserve and detached owners in the actor set used for sanitation. Keep toolbar placement eligibility scoped to riders.",
      "requested_evidence": "Use the capital fixture's separate remote owner and horse membership. Put a stale Mount Primary slot and auto-use selection on the horse with no backing fact. Inspect both inside serialization and after disable, alongside an ordinary ability that must survive.",
      "title": "Save and disable cleanup skip a reserve owner's persistent pet",
      "trigger": "An older capital save restores a remote owner whose horse holds a legacy Mount Primary reference. The player saves again or disables KMC before taking the owner adventuring."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 older city saves",
        "\u00a74 save/load"
      ],
      "actual": "Native invalidation records slot 3 as interrupted. With no sibling binding, RecordCopies discards the empty copy list. EffectivePublicIndex subsequently finds the player's canonical binding at 11, but ClassifyFirstSeenReserveBar requires HasRecordedCopies to accept it. The scan clears 11 and the archive carries 3.",
      "blocking_rationale": "A player with a single shortcut can still have a deliberate new placement reversed by the first reserve scan.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The new placement at 11 becomes the recoverable choice. Leaving the bar untouched preserves 3.",
      "id": "R22-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2487
      },
      "priority": "P2",
      "requested_correction": "Preserve enough information about the observed bar to recognize a subsequent placement when the interrupted shortcut had zero siblings. Retain the existing precedence for untouched duplicates and older archive rows.",
      "requested_evidence": "Start with only slot 3 on a never-observed reserve rider. Let GetSlot empty it, place the current live control at 11 and save before the first reserve scan. Check 11 and one placement marker in the archive and one shortcut at 11 after both same-world return and cold restore. Keep the no-edit and duplicate controls.",
      "title": "The first reserve scan loses a fresh placement when the old shortcut had no duplicate",
      "trigger": "Native cleanup empties the rider's only shortcut. The player places the current control elsewhere before KMC first scans the reserve bar."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 genuine companion removal",
        "\u00a74 city save/load"
      ],
      "actual": "After RevalidateAbsentArrangements retires the old pet's arrangement, the already-scanned bar can receive a new binding for the replacement pet. AdmitSurvivingBinding declines a retired actor and ClassifyFreshReserveBinding immediately returns. Its caller empties the new slot, leaving no recoverable slot row for it.",
      "blocking_rationale": "Retiring the former pet's arrangement prevents a subsequent player choice for the replacement pet from surviving the save.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The former companion's slot stays retired. A deliberate placement made after that retirement establishes a new arrangement for the current companion.",
      "id": "R25-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2519
      },
      "priority": "P2",
      "requested_correction": "Allow an identifiable fresh binding on a previously cleared reserve bar to establish a new arrangement after retirement. Continue rejecting stale bindings and archive replays from the former companion.",
      "requested_evidence": "Scan a reserve rider with a live fact at 9, replace pet A with pet B, and update with the bar empty so A's arrangement retires. Place the live control at 11, save and cold-restore. Require one shortcut at 11 for B. With no new placement, require no restoration at 9.",
      "title": "A scanned reserve rider cannot establish a fresh shortcut after companion replacement",
      "trigger": "A reserve rider's old arrangement has been retired, and the player places the live control for the replacement companion before saving."
    },
    {
      "acceptance_ids": [
        "\u00a74 player moves",
        "\u00a74 save/load",
        "\u00a77 affected lifecycle paths"
      ],
      "actual": "BeginSelectedLoad resets reserveScan. AbandonLoadedWorld restores the outgoing placement records but omits the fact that KMC already scanned and cleared this bar. On the next save, slot 11 takes the first-scan path and loses to the interrupted record at 9. The component double's BeginLoad at tests/MountedControlPlacementIntentTests.cs line 589 omits the production reset.",
      "blocking_rationale": "Attempting and canceling a load can change which shortcut the continuing campaign saves.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Returning to the same player world preserves the player's edit and the information needed to recognize it.",
      "id": "R26-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 1778
      },
      "priority": "P2",
      "requested_correction": "Preserve the outgoing world's reserve classification through an abandoned load, or record the pending player edit before discarding that classification. Release outgoing state on genuine world replacement.",
      "requested_evidence": "Extend the scanned-reserve 9-to-11 case with a selected load abandoned before world replacement. Exercise the production reset and abandonment sequence. Verify that an immediate save carries 11 and one marker, cold restore returns 11 once, and the no-edit control still carries 9.",
      "title": "Canceling a load changes how the next save classifies an edited reserve bar",
      "trigger": "KMC scans a reserve rider and clears slot 9. The player places the live ability at 11, starts a load, and returns to the same world before KMC observes the edit."
    },
    {
      "acceptance_ids": [
        "\u00a74 world disposal",
        "\u00a74 discarded entity cleanup"
      ],
      "actual": "CopiesOf retains recordedCopiesWorld whenever the current Player is null. ReleaseCopiesOfDepartedRiders also returns on null. SyncPlacementWorld clears the placement maps on an unfenced main-menu transition, but the separate dictionary still holds the slot objects and a strong reference to the discarded Player.",
      "blocking_rationale": "The control service can retain a disposed campaign for the entire main-menu interval.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A temporarily missing Player during a selected load keeps the outgoing copies available for abandonment. Unfenced disposal releases them when the world goes away.",
      "id": "R26-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2220
      },
      "priority": "P2",
      "requested_correction": "Tie retention during a null-Player interval to the outgoing selected-load fence. Release the registry and its world reference on an unfenced disposal.",
      "requested_evidence": "Record a duplicate on a never-observed reserve rider, return directly to the main menu before its reserve scan, and update with no replacement Player. Assert zero retained copies and no held world while still at the menu. Keep the fenced abandonment and real-transition controls.",
      "title": "The duplicate registry retains a discarded Player at the main menu",
      "trigger": "A world with a recorded duplicate is disposed without a selected load, and the player stays at the main menu."
    },
    {
      "acceptance_ids": [
        "\u00a74 mod disable/removal cleanup",
        "\u00a74 existing save compatibility",
        "\u00a77 affected save paths"
      ],
      "actual": "RemoveAllManagedFacts clears AutoUseAbility only in reserveUnits, which explicitly excludes current candidates. A returning rider with an older Rider Primary selection and no backing fact or slot depends on a later ReconcileFacts update. An immediate save can serialize m_AutoUseAbility first. The disabled-save path also skips ordinary reconciliation.",
      "blocking_rationale": "Whether a save or disable removes a serialized KMC reference depends on whether one control update ran after the roster change.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The sanitation boundary clears an exact managed auto-use reference on the rider regardless of current roster membership. Ordinary auto-use selections survive.",
      "id": "R26-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Integration/NativeMountedControlService.cs",
        "line": 2937
      },
      "priority": "P2",
      "requested_correction": "Apply exact-blueprint auto-use sanitation to current riders reached by the save or disable sweep, including when the selection has no backing fact or toolbar slot.",
      "requested_evidence": "Move the stale Rider Primary fixture from reserve to travelling party immediately before save and, separately, disable. Inspect the selection inside serialization and after disable. Extend DisabledSaveIsolation with the same stale selection on a party rider and retain an ordinary auto-use control.",
      "title": "A newly returned party rider can carry a retired auto-use selection through save or disable",
      "trigger": "A reserve rider rejoins the travelling party with a stale KMC auto-use selection, and save or disable runs before the next control update."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_rejected",
      "finding_id": "R22-F3",
      "rationale": "The moved and untouched duplicate cases now pass with slot 3 free. The same first-scan decision still discards a fresh canonical placement when native cleanup observed a bar containing just one shortcut."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R23-F3",
      "rationale": "Inactive-control reconciliation records the effective moved alias before migration clears it. SurvivingAliasAfterCanonicalCleanup covers update-first and save-first order, restoration at 11 and the untouched-alias control."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R24-F3",
      "rationale": "The selected-load path preserves the duplicate identity across a null-Player frame, recovers slot 3 when A returns, and releases A's copies when B replaces it. R26-F2 concerns disposal without a selected load."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R25-F1",
      "rationale": "SuspendAreaControls now classifies reserve bars before the sweep. ReserveBoundaryCoverage starts with another transferring pair and verifies that the reserve rider recovers slot 9 once and retains Fireball at 3."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R25-F2",
      "rationale": "For a rider who remains in reserve, the cleanup now checks the exact auto-use blueprint independently of the fact and slot. The focused case inspects the selection during serialization and after disable and preserves an ordinary selection."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R25-F3",
      "rationale": "The same-pet 9-to-11 case now passes, including the no-edit control. The new fresh-binding path still clears a deliberate placement made after the previous companion's arrangement was retired."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121063023-e55c-r26-b91e50",
  "round": 26,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The candidate records toolbar intent through temporary fact loss, projects eligible owners' persistent pets into the capital inventory and character roster, and admits an explicit mounted attack on an enemy already unconscious when ordered. The two city reports share the capital roster transition as a trigger. The toolbar loss comes from withdrawing the control fact, while the missing pet portrait comes from the native UI's controllable-character filter.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Verify the toolbar on mounted and dismounted capital visits, interior transitions, forced dismount, save/load and return to adventuring.",
      "Select a reserve owner's pet in Inventory and Character, confirm its identity and equipment, make a permitted equipment change and verify it after reopening and reloading.",
      "Compare actual longbow resolution against standing, conscious-tripped and already-unconscious enemies at the documented near and normal distances in real time and turn based. Check roll counts, action costs, line of sight, approach, penalties and the mounted melee control."
    ],
    "scope_reviewed": "The complete final mission for draft PR #57, including the feature diff from dbf63702 to 6298f2a5, all six pending findings, focused validation and native-test deferrals.",
    "unresolved_questions": [
      "The reported weapon and the target's consciousness at the original click remain unknown. The source trace found the unconscious-target refusal. Whether clicking a conscious prone enemy reproduces a mounted-specific failure still requires the matched in-game comparison."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
