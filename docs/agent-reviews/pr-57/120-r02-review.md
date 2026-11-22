<!-- agent-review:v1 enrollment=kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f key=r02-review part=1/1 sha256=3932737ee2a1d3f70ee1ca9cba156ab2fab4e1179898deb143178b5c92134d93 -->
### Codex GPT-6.2 Astra — review — round 2 — candidate `b34616ffa2`

**Decision:** `changes_required` — The focused gate passed on the exact candidate, and all six inherited acceptance cases passed at the required component-test level. The toolbar can still lose a valid older shortcut, let a former rider overwrite its replacement’s choice, or give a replacement companion the former companion’s slot. Several archived regression tests also omit the event that caused the original failure. These are required development fixes. The documented native playtest deferral is permitted.

#### Findings

**R1-F10** · P2 · defect · blocks readiness — A former rider retained for pair cleanup can overwrite its replacement’s placement
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:753`; acceptance: §4, Addendum C3, Addendum D2, Addendum D3
- Trigger: An observed rider at 9 leaves the roster but stays in detached pair custody. A new instance with the same actor and pet IDs has a live placement at 11.
- Expected: Only the current roster instance authors the actor’s placement. The former instance remains reachable for cleanup.
- Actual: CollectCurrentCandidateUnits appends retained pair members to the party list. PlacementUnits admits both object references, and OwnsRecords immediately accepts any object in the rider list. With the replacement in the party, capture can observe its 11 and then overwrite it with the retained object’s 9. With the replacement in reserve, repeated updates can erase 11 after the reserve sweep clears its native binding.
- Why it blocks / does not: A discarded object can still change what the current rider saves and restores.
- Requested correction: Resolve placement authority by exact current-world roster identity before either reconciliation or capture. Keep retained former objects in the sanitation sweep.
- Requested evidence: Extend FormerInstanceKeepsReplacement with the old object still in pair custody. Cover a party replacement on immediate save and a reserve replacement through two updates. Require only 11 and one marker on save and cold restore.

**R2-F1** · P2 · defect · blocks readiness — An occupied canonical archive row hides a usable legacy row
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:629`; acceptance: §4, Addendum C4, Addendum E
- Trigger: An older archive contains a canonical row at 3 and a legacy public-control row at 9 for the same rider. On cold load, 3 is occupied and 9 is empty.
- Expected: Fireball stays at 3 and exactly one public control restores at the usable saved slot, 9.
- Actual: SeedFromRows excludes all legacy Mount and Dismount rows whenever any kind-6 row exists. It selects occupied 3, reconciliation gives it up, and RestorePersistentSlots skips individual replay of all public rows. The previous reader considered every row that restores as MountState. The new test for two saved positions uses two kind-6 rows.
- Why it blocks / does not: Upgrading a structurally valid save can permanently lose the shortcut despite an available saved destination.
- Requested correction: Apply the existing first-restorable-row rule across all accepted public-binding kinds on a fresh load. Preserve the separate protection for replay over a later in-world edit.
- Requested evidence: Cold-load mixed kind-6 and kind-1 or kind-2 rows with autofill off, Fireball at 3 and 9 free. Check Fireball and the sole shortcut after two updates, resave and reload. Keep the marker-only removal control.

**R2-F2** · P2 · defect · blocks readiness — Archive seeding overwrites a confirmed observation from the incoming world
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:635`; acceptance: §4, Addendum D1, Addendum D3, Addendum D6, Addendum E
- Trigger: During a selected load, native reads an incoming reserve bar before RestorePersistentSlots processes that rider’s own archive rows.
- Expected: The incoming rider keeps the archived choice at 3 and its known copy continues to yield. An accepted observation belongs to the incoming world through archive restore.
- Actual: The native report records 3 as pending and identifies 9 as a copy. Fresh-load seeding unconditionally creates another arrangement for the named actor, erasing the copy identity. The first reserve observation promotes 9. If seeding happens before the same native read, the service keeps 3. A marker-only group can likewise erase an incoming slot already recovered by native. LoadLifetime seeds an empty row array for its incoming rider.
- Why it blocks / does not: Loading identical data can produce different saved and restored slots solely because the native UI read the incoming bar earlier.
- Requested correction: Reconcile restored rows with observations already made for that exact incoming world. Preserve confirmed invalidations and copy identities while keeping outgoing-world records isolated.
- Requested evidence: Run both orders with an incoming archived 3 and marker, stale native bindings at 3 and 9, and GetSlot emptying 3. Require only 3 after immediate save, party return and cold restore. Also exercise a marker-only incoming group beside a genuinely recovered binding and an actual off-bar control.

**R2-F3** · P2 · defect · blocks readiness — A missing toolbar hides a confirmed companion replacement from the save header
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:662`; acceptance: §4, Addendum D2, Addendum D3, Addendum E
- Trigger: A current rider has an arrangement for pet A. The toolbar is temporarily unavailable when the persistent pet link changes to B.
- Expected: An unavailable bar defers slot inspection. Positive evidence of companion replacement still retires the old companion’s arrangement.
- Actual: Observe returns on a zero slot count before checking PetLink. RevalidateAbsent skips current riders. The header therefore writes A’s slot 9 after the link already names B. Public archive rows carry no pet ID, so a cold restore can bind 9 for B. Waiting for the bar to return before saving retires A correctly.
- Why it blocks / does not: A replacement companion can inherit a shortcut that should have retired, depending on when the player saves.
- Requested correction: Validate the known companion identity independently of bar availability. Preserve pending placement through a temporary bar outage for the same companion.
- Requested evidence: Observe A at 9, hide the bar, replace A with B and save before the bar returns. Require no slot for B on cold restore. Repeat with the same pet and require exactly one restoration at 9.

**R2-F4** · P2 · defect · blocks readiness — A missing-Player update drops a completed native write during a selected load
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:454`; acceptance: §4, Addendum D4, Addendum D5, Addendum D6
- Trigger: Native empties the outgoing rider’s shortcut and throws before its postfix. The next update sees no Player while a selected load is open.
- Expected: The outgoing world retains evidence of the completed native emptying until its load outcome is known.
- Actual: Housekeeping settles the operation before the fenced missing-Player guard. SettleNativeOperation drops the operation, then ApplyNativeOperation returns because CurrentWorld is null. After the same world returns, the empty slot is read as a player removal and the save can contain only the marker.
- Why it blocks / does not: Canceling a load can lose the outgoing shortcut even though the player never edited it.
- Requested correction: Preserve or settle the outgoing operation against its owning world during a fenced absence. Release its references on confirmed replacement, unfenced disposal or disable.
- Requested evidence: Have GetSlot write its native empty at 9 and omit the postfix. Run an update with no Player during a selected load, reinstate the same Player and abandon. Require row 9 and one restored shortcut. Retain the true-replacement and menu-release controls.

**R2-F5** · P2 · defect · blocks readiness — A partially completed native cleanup can turn a known copy into a fresh placement
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:479`; acceptance: §4, Addendum D1, Addendum D6
- Trigger: CleanupBadSlots rebinds a known copy to a new live slot object, then throws before its postfix. The effective slot remains free and pending.
- Expected: A completed native rebind preserves the copy’s identity for placement purposes. A genuine placement after the call remains a player edit.
- Actual: ApplyNativeOperation skips every rebound entry when the postfix is missing. The next observation forgets the old copy object and classifies its native replacement as a fresh placement at 9. There is also an inverse failure after a throwing GetSlot empties 3. If the player places 11 before settlement, Observe reconstructs the pre-write bar using the later binding at 11 and can mark the player’s edit as an old copy.
- Why it blocks / does not: The service can move the shortcut without a player edit or discard a real edit. The handoff acknowledges the skipped rebind, but the resulting placement violates D6.
- Requested correction: Account for completed native writes at the exception boundary or retain enough operation evidence to distinguish them from later player edits. Preserve the original exception behavior.
- Requested evidence: With 3 pending and a known stale copy at 9, rebind 9 and throw before cleanup completes. Require only 3 on save and return. Separately, empty 3 and throw, place the live control at 11 before settlement, and require only 11. Keep the throw-before-write followed by deliberate removal control.

**R2-F6** · P2 · defect · blocks readiness — A continuing world can retain discarded slot objects from a reserve rider
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:737`; acceptance: §4, Addendum C1, Addendum E
- Trigger: A native read records a reserve rider’s duplicate. The rider leaves all resolvable rosters before its first scan, or after the sweep clears its bindings and before another observation.
- Expected: Clearing or permanently discarding a reserve bar releases its native slot objects while preserving the numeric choice needed for a legitimate same-pet return.
- Actual: Arrangement.Copies holds native slot objects strongly. ClearPublicBindings clears the bar without removing those references. A rider seen only by the native callback is absent from the departure list, and RevalidateAbsent skips an actor that no longer resolves. Its slot, AbilityData and unit can remain reachable for the rest of the world. The mapped DisableAndLifetime case uses an already-observed party rider with no recorded duplicate.
- Why it blocks / does not: This reintroduces the continuing-world discarded-object lifetime failure documented under first-enrollment R20-F1. The passing main-menu disposal case exercises a different boundary.
- Requested correction: Release cleared copy identities and make same-world roster exit safe for riders first encountered by a native callback. Keep recoverable index and pet identity independent of discarded native objects.
- Requested evidence: Record copy 9 when GetSlot empties 3 on a never-scanned reserve rider. Verify release immediately after a reserve sweep and, separately, after removal before the first sweep with the same Player still current. Check that a legitimate return restores 3 once.

**R1-F11** · P2 · defect · blocks readiness — Pet eligibility is checked only if the toolbar can resolve the actor during seeding
- Location: `src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs:623`; acceptance: Addendum C3, Addendum C4
- Trigger: A valid archive names a horse before the player roster exposes it to the toolbar adapter.
- Expected: Once the actor resolves as a pet, its placement record and marker are discarded. Legitimate temporarily unresolved riders retain their rows.
- Actual: The archive graph can resolve a unit from game.State.Units while ToolbarHost.ResolveCharacter searches only Player.AllCharacters. SeedFromRows therefore admits the ID as unresolved. When the horse later appears as a current party pet, Observe returns immediately for pets and RevalidateAbsent skips its current-rider ID. Capture filters its output, but the pet still owns the in-memory arrangement and placement marker.
- Why it blocks / does not: A sanitation-only actor can retain rider placement state after its identity is positively known.
- Requested correction: Use the available resolved actor identity at restore or purge previously admitted rider state when reconciliation first identifies a pet.
- Requested evidence: Seed rows while the horse is present in the area graph but absent from AllCharacters. Then expose it as the travelling owner’s pet. Require no pet arrangement, marker or emitted row, alongside a legitimate late-resolving rider that restores.

**R1-F13** · P2 · evidence_gap · blocks readiness — Several mapped tests still skip the archived failure boundary
- Location: `tests/MountedControlPlacementIntentTests.cs:1838`; acceptance: Addendum D1, Addendum E
- Trigger: Compare the current regression map with the triggers and requested evidence in the archived reviews.
- Expected: Each mapped subcase includes the state, event ordering and observable result that distinguished the archived failure.
- Actual: UnregisteredBlueprints represents its potion as an AbilitySlot with a non-null blueprint and only calls Disable. It never passes a non-ability slot’s null blueprint through the production classifier during save or disable. ReserveBars omits the native rebind of 9 before the move or removal in archived R22-F3. KnownCopies omits the pet-absent update before GetSlot(9) in archived R18-F1 round 24. CompanionLoss also omits the selected load and two-slot cleanup from archived R17-F3.
- Why it blocks / does not: The named tests can pass if the original regressions return. Addendum E requires coverage of each distinct archived failure.
- Requested correction: Use an actual non-ability slot shape that forwards a null blueprint. Restore the omitted native rebind, intervening update and selected-load batch-cleanup sequences in their mapped subcases. Update the handoff to describe what each test exercises.
- Requested evidence: Inspect the same item and ordinary ability objects inside serialization and after disable with absent or partial registration and a positive KMC control. For the archived toolbar sequences, assert the original slot or marker outcome through save, return and cold restore where requested.

**R2-F7** · P3 · required_gap · blocks readiness — The handoff omits case-level evidence and two deferred observations
- Location: `docs/CITY-AND-PRONE-HANDOFF.md:302`; acceptance: §4, §5, §7, Addendum E
- Trigger: Use the current handoff to verify the negative combat controls, persistent equipment and temporary mount unavailability.
- Expected: The handoff identifies each essential case’s result and evidence type and gives an observable pass condition for each deferred native behavior.
- Actual: The archived R21-F3 mapping points to section 9, which gives suite totals without identifying the Charge and continuation filters as source traces. The city playtest equips and unequips before saving, then checks portrait count after reload. It does not check that a permitted equipment edit persisted on the same reserve pet. The transition steps also omit a forced-dismount and same-pet recovery observation.
- Why it blocks / does not: The required acceptance record leaves a reader unable to tell which combat filters were exercised or to complete two deferred native checks from the instructions.
- Requested correction: Restore the case-level distinction between executed target predicates and traced integration filters. Have the owner leave an identifiable item equipped through reopen and reload, verify it on the same reserve pet, and check the chosen toolbar slot through forced dismount and return of the same pet.
- Requested evidence: A corrected acceptance record and owner playtest bound to the reviewed candidate are sufficient. Keep the native results marked NOT RUN.

#### Prior finding assessments

- **R1-F1** — `fix_verified` — ApplyBar tracks one chosen slot and recognizes surviving copies by slot-object identity. The corrected fixtures keep the duplicate present during removal and enforce only 3 when untouched or only 11 after the live edit.
- **R1-F2** — `fix_verified` — Reconcile now returns before departure cleanup on a fenced missing-Player frame. MissingPlayerDuringCanceledLoad covers the original live party shortcut through immediate save and cold restore. R2-F4 concerns an outstanding native write that precedes that guard.
- **R1-F3** — `fix_verified` — A normally completed rebind of the chosen slot clears Pending. NativeRebindOfPendingSlot distinguishes a subsequent player removal from a native emptying through cold restore.
- **R1-F4** — `fix_verified` — The live-bar migration now uses the surviving alias at 9 when Fireball occupies interrupted slot 4. LegacyAliases checks the occupant, the single fallback and its cold restoration. R2-F1 concerns archived rows with an empty fallback destination.
- **R1-F5** — `fix_verified` — The save and disable sanitation sweep follows PersistentPet for every gathered owner, including remote and detached owners also in pair custody. The component and adapter fixtures inspect the separately represented horse.
- **R1-F6** — `fix_verified` — Save sanitation now attempts the stock Charge migration before removing the legacy binding. LegacyChargeEitherOrder covers slot 8 inside serialization, after release and after cold load. The adapter fixture exercises save-first forwarding.
- **R1-F7** — `fix_verified` — ProcessDepartures clears exact managed auto-use before releasing the last observed reference. DepartureClearsAutoUse checks the departed rider, former pet and an ordinary selection.
- **R1-F8** — `fix_verified` — The adapter’s independent strong reference is gone. The real prefix fixture omits the postfix and verifies that menu disposal and disable release the lifecycle-owned operation.
- **R1-F9** — `fix_verified` — RestorePending defers when the bar count is zero. The saved 9 survives an intervening capture and restores once when the same rider’s bar returns. R2-F3 concerns positive evidence of a different companion during that interval.
- **R1-F10** — `fix_rejected` — FormerInstanceKeepsReplacement passes once the old object leaves the candidate list. Retained pair custody keeps it on that list and bypasses the same-ID replacement check, allowing it to author the current actor’s record.
- **R1-F11** — `fix_rejected` — PetRowsOwnNothing verifies a horse already visible in AllCharacters at admission. The graph can resolve a horse earlier than that lookup, and no later check removes its admitted arrangement when it becomes a current pet.
- **R1-F12** — `fix_verified` — The disabled-save selection is installed and asserted after disable. Both reserve-edit cases assert current live AbilityData at 11 before the boundary and verify the resulting slot counts.
- **R1-F13** — `fix_rejected` — The area-transfer fixture now keeps its rider in reserve, and the incoming-world fixture continues through seeding, save and return. The non-ability registration case and several other mapped event orderings still omit their original failure conditions.
- **R1-F14** — `fix_verified` — The mutation campaign now compiles the production toolbar adapter partial. Removing real save or native-prefix forwarding, detached or reserve membership, or persistent-pet lookup causes named behavioral failures. The restored baseline passes.
- **R1-F15** — `fix_verified` — Both affected scripts read the extracted toolbar source. The presentation probe passed on the product-equivalent predecessor, and all four affected Finalize2 source predicates hold at H. The outstanding historical r05 input is recorded separately.
- **R1-F16** — `fix_verified` — The handoff contains the exact required two-sentence status and restores the mounted melee, unmounted ranged, ammunition, range, wall and action-cost controls with observable outcomes. Native combat remains marked NOT RUN.

#### Evidence assessment

- **Inspected:** The controller mission and addendum, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, the 7,489-line feature diff, developer report and transcript, prior ledger, focused receipt and log.; The production placement and lifecycle components, native adapter and patch entry points, pair-custody enumeration, archive validation and restore, save and area boundaries, test fakes, adapter fixture and mutation script.; The archived first-enrollment findings and their mapped subcases, the previous archive reader, the city roster projection, downed-target admission and command paths, and the owner handoff.
- **Executed verification:** Verified the detached HEAD, the mission SHA-256 and the focused log SHA-256. The log hash is 652a075ddf24e495b95785ea3a85cf634c2cdbfb8467b97fcca7ef50dbe2b317. The controller receipt records candidate b34616ff, exit 0, 32.9 seconds and an unchanged worktree. The feature diff passed git diff --check.; The recorded Fast run passed 4/4 stages and the focused wrapper passed 7/7 children. It reports 1,456 component checks, 153 source checks, 825 assembly checks, 21 click-composition checks covering both patch orders, 7 archive-bound checks, 22 six-pair adapter checks and 3 target-liveness source checks. The product MVID was d9ef80f6-bc69-4381-ab1e-f8eb831e4a51.; The mutation run passed its 36 lifecycle and 5 adapter baseline checks. All 16 production mutations failed their named checks, including removal of save forwarding, native-prefix forwarding and required roster inputs. The source hashes remained unchanged.; AC-R26-F4 passed as UNIT in AcR26F4ReservePetSanitized. The remote owner and horse had separate roster membership. The horse’s factless Mount Primary slot and auto-use selection were cleared inside serialization and after disable. Ordinary controls survived.; AC-R22-F3 passed as UNIT in AcR22F3FreshPlacementAfterNativeEmpty. After native emptied 3, the asserted live placement at 11 produced one slot row and one marker. Exactly one shortcut returned at 11 in the same world and after cold restore. The untouched and duplicate controls retained 3.; AC-R25-F3 passed as UNIT in AcR25F3FreshShortcutAfterReplacement. An empty scan retired pet A’s slot 9. A later placement at 11 survived save and cold restore for pet B. The no-edit control left 9 retired.; AC-R26-F1 passed as UNIT in AcR26F1CanceledLoadKeepsEdit. The live edit from swept slot 9 to 11 survived load abandonment, immediate capture and cold restore. The no-edit control retained 9.; AC-R26-F2 passed as UNIT in AcR26F2MainMenuReleases. A direct menu return before the first reserve scan released the records and world. The fenced abandonment preserved the effective slot at 3, and a real world replacement released the outgoing state.; AC-R26-F3 passed as UNIT in AcR26F3RejoinedRiderAutoUse. The returning rider had the stale Rider Primary selection immediately before each boundary, including after disable in the disabled-save leg. Serialization and disable cleared it and preserved the ordinary selection.; The developer transcript records the presentation probe passing in 15.3 seconds and the Finalize2 reader probe failing in 16.8 seconds on 56366c41. Only documentation changed between that commit and H. I separately evaluated the four affected removal-safeguard source predicates at H. All four held.
- **Source inspection only:** The city reports share the capital roster transition. The toolbar loses native binding evidence when the control fact is withdrawn. The missing pet portrait comes from the native UI’s controllable-character filter. The candidate projects the eligible owner’s persistent pet into the native group result.; The mounted command path now carries an explicit order on a target already unconscious when ordered. The conscious-prone click path, Charge filtering and continuation-target selection were traced in source. Actual clicks, equipment persistence, attack rolls and native expenditure remain unobserved on H.
- **Required gaps:** Give the current roster instance exclusive placement authority, release discarded copy objects, and enforce rider eligibility as actors and pet links resolve.; Preserve the first restorable position across compatible archive row kinds and preserve confirmed observations made in the incoming world before archive replay.; Preserve completed native writes across exceptions and a missing-Player interval without absorbing subsequent player edits.; Exercise the archived failure orderings through the production path and restore the missing case-level evidence and owner observations in the handoff.
- **Limitations:** This was a read-only review. Build and test counts come from the controller records. The additional defect sequences below were traced in source.; The area-adapter comparison produced the same 63 compiler diagnostics at base and candidate. Its runtime assertions executed in neither tree.; The Finalize2 transcript supplies the failed gate result without its detailed child log. The repository separately records the missing r05 receipt as T09. The full reader run remains unverified.; No native transaction ran on H. The candidate package, updated capital reader, portrait/equipment driver and matched target-posture setup are the documented prerequisites. No installation or save restoration was needed.

#### Summary

- **Delivered behavior:** The native adapter and offline tests now share the production toolbar lifecycle. The six inherited cases pass, including reserve-pet sanitation, live edits before save, companion retirement, load abandonment and menu disposal. The city roster projection and explicit already-unconscious target change remain stable in this enrollment.
- **Scope reviewed:** The complete final mission and toolbar repair addendum for draft PR #57, the feature diff from dbf63702 to b34616ff, all 16 pending findings, all six inherited acceptance cases, focused validation and the native-test deferrals.
- **Pending later-phase work:** none
- **Unresolved questions:** The original weapon and the target’s consciousness at the reported click remain unknown. Whether a mounted-specific failure occurs against a conscious prone enemy still requires matched in-game verification.
- **Remaining owner review:** Verify the chosen shortcut through mounted and dismounted capital arrivals, interiors, forced dismount, saves in both locations and return of the same pet.; Select the exact eligible reserve pet in Inventory and Character. Verify its name, statistics, rename behavior and a permitted equipment change across reopening and cold load.; Compare actual mounted and dismounted longbow rolls against standing, conscious-tripped and already-unconscious enemies at the documented near and normal distances in real time and turn based. Check approach, line of sight, modifiers, roll count, action cost and the mounted melee control.

#### Provenance (controller-recorded)

| | Requested | Observed |
|---|---|---|
| Model | `gpt-6.2-astra` | `gpt-6.2-astra` (turns ['gpt-6.2-astra']) |
| Reasoning effort | `max` | `max` (turns ['max']) |
| Sandbox / approvals | read-only / never | `read-only` / `never` |

Codex CLI `0.170.0`, thread `01a20141-c25f-77e1-898e-22467714d6e5`, duration 1221.0s, response sha256 `0b93dc7b6ed57f76ebfb1b3b58d657c48a422be949f077130973fa5f602e473a`, request `kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f-r02-c1dd22`. Observed values come from the Codex session rollout's recorded thread settings, not from the model's own statements; the serving backend's model identity is not separately exposed.

<details><summary>Complete structured review (agent-review.review.v1)</summary>

```json
{
  "candidate_commit": "b34616ffa234916e04bfb821ca82bf88cd565082",
  "checkpoint_id": "final",
  "checkpoint_type": "final",
  "decision": "changes_required",
  "decision_rationale": "The focused gate passed on the exact candidate, and all six inherited acceptance cases passed at the required component-test level. The toolbar can still lose a valid older shortcut, let a former rider overwrite its replacement\u2019s choice, or give a replacement companion the former companion\u2019s slot. Several archived regression tests also omit the event that caused the original failure. These are required development fixes. The documented native playtest deferral is permitted.",
  "evidence_assessment": {
    "executed_verification": [
      "Verified the detached HEAD, the mission SHA-256 and the focused log SHA-256. The log hash is 652a075ddf24e495b95785ea3a85cf634c2cdbfb8467b97fcca7ef50dbe2b317. The controller receipt records candidate b34616ff, exit 0, 32.9 seconds and an unchanged worktree. The feature diff passed git diff --check.",
      "The recorded Fast run passed 4/4 stages and the focused wrapper passed 7/7 children. It reports 1,456 component checks, 153 source checks, 825 assembly checks, 21 click-composition checks covering both patch orders, 7 archive-bound checks, 22 six-pair adapter checks and 3 target-liveness source checks. The product MVID was d9ef80f6-bc69-4381-ab1e-f8eb831e4a51.",
      "The mutation run passed its 36 lifecycle and 5 adapter baseline checks. All 16 production mutations failed their named checks, including removal of save forwarding, native-prefix forwarding and required roster inputs. The source hashes remained unchanged.",
      "AC-R26-F4 passed as UNIT in AcR26F4ReservePetSanitized. The remote owner and horse had separate roster membership. The horse\u2019s factless Mount Primary slot and auto-use selection were cleared inside serialization and after disable. Ordinary controls survived.",
      "AC-R22-F3 passed as UNIT in AcR22F3FreshPlacementAfterNativeEmpty. After native emptied 3, the asserted live placement at 11 produced one slot row and one marker. Exactly one shortcut returned at 11 in the same world and after cold restore. The untouched and duplicate controls retained 3.",
      "AC-R25-F3 passed as UNIT in AcR25F3FreshShortcutAfterReplacement. An empty scan retired pet A\u2019s slot 9. A later placement at 11 survived save and cold restore for pet B. The no-edit control left 9 retired.",
      "AC-R26-F1 passed as UNIT in AcR26F1CanceledLoadKeepsEdit. The live edit from swept slot 9 to 11 survived load abandonment, immediate capture and cold restore. The no-edit control retained 9.",
      "AC-R26-F2 passed as UNIT in AcR26F2MainMenuReleases. A direct menu return before the first reserve scan released the records and world. The fenced abandonment preserved the effective slot at 3, and a real world replacement released the outgoing state.",
      "AC-R26-F3 passed as UNIT in AcR26F3RejoinedRiderAutoUse. The returning rider had the stale Rider Primary selection immediately before each boundary, including after disable in the disabled-save leg. Serialization and disable cleared it and preserved the ordinary selection.",
      "The developer transcript records the presentation probe passing in 15.3 seconds and the Finalize2 reader probe failing in 16.8 seconds on 56366c41. Only documentation changed between that commit and H. I separately evaluated the four affected removal-safeguard source predicates at H. All four held."
    ],
    "inspected": [
      "The controller mission and addendum, AGENTS.md, validation policy, CURRENT.md, AUTONOMOUS-RESUME.md, the 7,489-line feature diff, developer report and transcript, prior ledger, focused receipt and log.",
      "The production placement and lifecycle components, native adapter and patch entry points, pair-custody enumeration, archive validation and restore, save and area boundaries, test fakes, adapter fixture and mutation script.",
      "The archived first-enrollment findings and their mapped subcases, the previous archive reader, the city roster projection, downed-target admission and command paths, and the owner handoff."
    ],
    "limitations": [
      "This was a read-only review. Build and test counts come from the controller records. The additional defect sequences below were traced in source.",
      "The area-adapter comparison produced the same 63 compiler diagnostics at base and candidate. Its runtime assertions executed in neither tree.",
      "The Finalize2 transcript supplies the failed gate result without its detailed child log. The repository separately records the missing r05 receipt as T09. The full reader run remains unverified.",
      "No native transaction ran on H. The candidate package, updated capital reader, portrait/equipment driver and matched target-posture setup are the documented prerequisites. No installation or save restoration was needed."
    ],
    "required_gaps": [
      "Give the current roster instance exclusive placement authority, release discarded copy objects, and enforce rider eligibility as actors and pet links resolve.",
      "Preserve the first restorable position across compatible archive row kinds and preserve confirmed observations made in the incoming world before archive replay.",
      "Preserve completed native writes across exceptions and a missing-Player interval without absorbing subsequent player edits.",
      "Exercise the archived failure orderings through the production path and restore the missing case-level evidence and owner observations in the handoff."
    ],
    "source_inspection_only": [
      "The city reports share the capital roster transition. The toolbar loses native binding evidence when the control fact is withdrawn. The missing pet portrait comes from the native UI\u2019s controllable-character filter. The candidate projects the eligible owner\u2019s persistent pet into the native group result.",
      "The mounted command path now carries an explicit order on a target already unconscious when ordered. The conscious-prone click path, Charge filtering and continuation-target selection were traced in source. Actual clicks, equipment persistence, attack rolls and native expenditure remain unobserved on H."
    ]
  },
  "findings": [
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum C3",
        "Addendum D2",
        "Addendum D3"
      ],
      "actual": "CollectCurrentCandidateUnits appends retained pair members to the party list. PlacementUnits admits both object references, and OwnsRecords immediately accepts any object in the rider list. With the replacement in the party, capture can observe its 11 and then overwrite it with the retained object\u2019s 9. With the replacement in reserve, repeated updates can erase 11 after the reserve sweep clears its native binding.",
      "blocking_rationale": "A discarded object can still change what the current rider saves and restores.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Only the current roster instance authors the actor\u2019s placement. The former instance remains reachable for cleanup.",
      "id": "R1-F10",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 753
      },
      "priority": "P2",
      "requested_correction": "Resolve placement authority by exact current-world roster identity before either reconciliation or capture. Keep retained former objects in the sanitation sweep.",
      "requested_evidence": "Extend FormerInstanceKeepsReplacement with the old object still in pair custody. Cover a party replacement on immediate save and a reserve replacement through two updates. Require only 11 and one marker on save and cold restore.",
      "title": "A former rider retained for pair cleanup can overwrite its replacement\u2019s placement",
      "trigger": "An observed rider at 9 leaves the roster but stays in detached pair custody. A new instance with the same actor and pet IDs has a live placement at 11."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum C4",
        "Addendum E"
      ],
      "actual": "SeedFromRows excludes all legacy Mount and Dismount rows whenever any kind-6 row exists. It selects occupied 3, reconciliation gives it up, and RestorePersistentSlots skips individual replay of all public rows. The previous reader considered every row that restores as MountState. The new test for two saved positions uses two kind-6 rows.",
      "blocking_rationale": "Upgrading a structurally valid save can permanently lose the shortcut despite an available saved destination.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Fireball stays at 3 and exactly one public control restores at the usable saved slot, 9.",
      "id": "R2-F1",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 629
      },
      "priority": "P2",
      "requested_correction": "Apply the existing first-restorable-row rule across all accepted public-binding kinds on a fresh load. Preserve the separate protection for replay over a later in-world edit.",
      "requested_evidence": "Cold-load mixed kind-6 and kind-1 or kind-2 rows with autofill off, Fireball at 3 and 9 free. Check Fireball and the sole shortcut after two updates, resave and reload. Keep the marker-only removal control.",
      "title": "An occupied canonical archive row hides a usable legacy row",
      "trigger": "An older archive contains a canonical row at 3 and a legacy public-control row at 9 for the same rider. On cold load, 3 is occupied and 9 is empty."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum D1",
        "Addendum D3",
        "Addendum D6",
        "Addendum E"
      ],
      "actual": "The native report records 3 as pending and identifies 9 as a copy. Fresh-load seeding unconditionally creates another arrangement for the named actor, erasing the copy identity. The first reserve observation promotes 9. If seeding happens before the same native read, the service keeps 3. A marker-only group can likewise erase an incoming slot already recovered by native. LoadLifetime seeds an empty row array for its incoming rider.",
      "blocking_rationale": "Loading identical data can produce different saved and restored slots solely because the native UI read the incoming bar earlier.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The incoming rider keeps the archived choice at 3 and its known copy continues to yield. An accepted observation belongs to the incoming world through archive restore.",
      "id": "R2-F2",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 635
      },
      "priority": "P2",
      "requested_correction": "Reconcile restored rows with observations already made for that exact incoming world. Preserve confirmed invalidations and copy identities while keeping outgoing-world records isolated.",
      "requested_evidence": "Run both orders with an incoming archived 3 and marker, stale native bindings at 3 and 9, and GetSlot emptying 3. Require only 3 after immediate save, party return and cold restore. Also exercise a marker-only incoming group beside a genuinely recovered binding and an actual off-bar control.",
      "title": "Archive seeding overwrites a confirmed observation from the incoming world",
      "trigger": "During a selected load, native reads an incoming reserve bar before RestorePersistentSlots processes that rider\u2019s own archive rows."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum D2",
        "Addendum D3",
        "Addendum E"
      ],
      "actual": "Observe returns on a zero slot count before checking PetLink. RevalidateAbsent skips current riders. The header therefore writes A\u2019s slot 9 after the link already names B. Public archive rows carry no pet ID, so a cold restore can bind 9 for B. Waiting for the bar to return before saving retires A correctly.",
      "blocking_rationale": "A replacement companion can inherit a shortcut that should have retired, depending on when the player saves.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "An unavailable bar defers slot inspection. Positive evidence of companion replacement still retires the old companion\u2019s arrangement.",
      "id": "R2-F3",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 662
      },
      "priority": "P2",
      "requested_correction": "Validate the known companion identity independently of bar availability. Preserve pending placement through a temporary bar outage for the same companion.",
      "requested_evidence": "Observe A at 9, hide the bar, replace A with B and save before the bar returns. Require no slot for B on cold restore. Repeat with the same pet and require exactly one restoration at 9.",
      "title": "A missing toolbar hides a confirmed companion replacement from the save header",
      "trigger": "A current rider has an arrangement for pet A. The toolbar is temporarily unavailable when the persistent pet link changes to B."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum D4",
        "Addendum D5",
        "Addendum D6"
      ],
      "actual": "Housekeeping settles the operation before the fenced missing-Player guard. SettleNativeOperation drops the operation, then ApplyNativeOperation returns because CurrentWorld is null. After the same world returns, the empty slot is read as a player removal and the save can contain only the marker.",
      "blocking_rationale": "Canceling a load can lose the outgoing shortcut even though the player never edited it.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "The outgoing world retains evidence of the completed native emptying until its load outcome is known.",
      "id": "R2-F4",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 454
      },
      "priority": "P2",
      "requested_correction": "Preserve or settle the outgoing operation against its owning world during a fenced absence. Release its references on confirmed replacement, unfenced disposal or disable.",
      "requested_evidence": "Have GetSlot write its native empty at 9 and omit the postfix. Run an update with no Player during a selected load, reinstate the same Player and abandon. Require row 9 and one restored shortcut. Retain the true-replacement and menu-release controls.",
      "title": "A missing-Player update drops a completed native write during a selected load",
      "trigger": "Native empties the outgoing rider\u2019s shortcut and throws before its postfix. The next update sees no Player while a selected load is open."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum D1",
        "Addendum D6"
      ],
      "actual": "ApplyNativeOperation skips every rebound entry when the postfix is missing. The next observation forgets the old copy object and classifies its native replacement as a fresh placement at 9. There is also an inverse failure after a throwing GetSlot empties 3. If the player places 11 before settlement, Observe reconstructs the pre-write bar using the later binding at 11 and can mark the player\u2019s edit as an old copy.",
      "blocking_rationale": "The service can move the shortcut without a player edit or discard a real edit. The handoff acknowledges the skipped rebind, but the resulting placement violates D6.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "A completed native rebind preserves the copy\u2019s identity for placement purposes. A genuine placement after the call remains a player edit.",
      "id": "R2-F5",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 479
      },
      "priority": "P2",
      "requested_correction": "Account for completed native writes at the exception boundary or retain enough operation evidence to distinguish them from later player edits. Preserve the original exception behavior.",
      "requested_evidence": "With 3 pending and a known stale copy at 9, rebind 9 and throw before cleanup completes. Require only 3 on save and return. Separately, empty 3 and throw, place the live control at 11 before settlement, and require only 11. Keep the throw-before-write followed by deliberate removal control.",
      "title": "A partially completed native cleanup can turn a known copy into a fresh placement",
      "trigger": "CleanupBadSlots rebinds a known copy to a new live slot object, then throws before its postfix. The effective slot remains free and pending."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "Addendum C1",
        "Addendum E"
      ],
      "actual": "Arrangement.Copies holds native slot objects strongly. ClearPublicBindings clears the bar without removing those references. A rider seen only by the native callback is absent from the departure list, and RevalidateAbsent skips an actor that no longer resolves. Its slot, AbilityData and unit can remain reachable for the rest of the world. The mapped DisableAndLifetime case uses an already-observed party rider with no recorded duplicate.",
      "blocking_rationale": "This reintroduces the continuing-world discarded-object lifetime failure documented under first-enrollment R20-F1. The passing main-menu disposal case exercises a different boundary.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Clearing or permanently discarding a reserve bar releases its native slot objects while preserving the numeric choice needed for a legitimate same-pet return.",
      "id": "R2-F6",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 737
      },
      "priority": "P2",
      "requested_correction": "Release cleared copy identities and make same-world roster exit safe for riders first encountered by a native callback. Keep recoverable index and pet identity independent of discarded native objects.",
      "requested_evidence": "Record copy 9 when GetSlot empties 3 on a never-scanned reserve rider. Verify release immediately after a reserve sweep and, separately, after removal before the first sweep with the same Player still current. Check that a legitimate return restores 3 once.",
      "title": "A continuing world can retain discarded slot objects from a reserve rider",
      "trigger": "A native read records a reserve rider\u2019s duplicate. The rider leaves all resolvable rosters before its first scan, or after the sweep clears its bindings and before another observation."
    },
    {
      "acceptance_ids": [
        "Addendum C3",
        "Addendum C4"
      ],
      "actual": "The archive graph can resolve a unit from game.State.Units while ToolbarHost.ResolveCharacter searches only Player.AllCharacters. SeedFromRows therefore admits the ID as unresolved. When the horse later appears as a current party pet, Observe returns immediately for pets and RevalidateAbsent skips its current-rider ID. Capture filters its output, but the pet still owns the in-memory arrangement and placement marker.",
      "blocking_rationale": "A sanitation-only actor can retain rider placement state after its identity is positively known.",
      "blocks_readiness": true,
      "category": "defect",
      "expected": "Once the actor resolves as a pet, its placement record and marker are discarded. Legitimate temporarily unresolved riders retain their rows.",
      "id": "R1-F11",
      "location": {
        "file": "src/KingmakerMountedCombat/Domain/MountedToolbarLifecycle.cs",
        "line": 623
      },
      "priority": "P2",
      "requested_correction": "Use the available resolved actor identity at restore or purge previously admitted rider state when reconciliation first identifies a pet.",
      "requested_evidence": "Seed rows while the horse is present in the area graph but absent from AllCharacters. Then expose it as the travelling owner\u2019s pet. Require no pet arrangement, marker or emitted row, alongside a legitimate late-resolving rider that restores.",
      "title": "Pet eligibility is checked only if the toolbar can resolve the actor during seeding",
      "trigger": "A valid archive names a horse before the player roster exposes it to the toolbar adapter."
    },
    {
      "acceptance_ids": [
        "Addendum D1",
        "Addendum E"
      ],
      "actual": "UnregisteredBlueprints represents its potion as an AbilitySlot with a non-null blueprint and only calls Disable. It never passes a non-ability slot\u2019s null blueprint through the production classifier during save or disable. ReserveBars omits the native rebind of 9 before the move or removal in archived R22-F3. KnownCopies omits the pet-absent update before GetSlot(9) in archived R18-F1 round 24. CompanionLoss also omits the selected load and two-slot cleanup from archived R17-F3.",
      "blocking_rationale": "The named tests can pass if the original regressions return. Addendum E requires coverage of each distinct archived failure.",
      "blocks_readiness": true,
      "category": "evidence_gap",
      "expected": "Each mapped subcase includes the state, event ordering and observable result that distinguished the archived failure.",
      "id": "R1-F13",
      "location": {
        "file": "tests/MountedControlPlacementIntentTests.cs",
        "line": 1838
      },
      "priority": "P2",
      "requested_correction": "Use an actual non-ability slot shape that forwards a null blueprint. Restore the omitted native rebind, intervening update and selected-load batch-cleanup sequences in their mapped subcases. Update the handoff to describe what each test exercises.",
      "requested_evidence": "Inspect the same item and ordinary ability objects inside serialization and after disable with absent or partial registration and a positive KMC control. For the archived toolbar sequences, assert the original slot or marker outcome through save, return and cold restore where requested.",
      "title": "Several mapped tests still skip the archived failure boundary",
      "trigger": "Compare the current regression map with the triggers and requested evidence in the archived reviews."
    },
    {
      "acceptance_ids": [
        "\u00a74",
        "\u00a75",
        "\u00a77",
        "Addendum E"
      ],
      "actual": "The archived R21-F3 mapping points to section 9, which gives suite totals without identifying the Charge and continuation filters as source traces. The city playtest equips and unequips before saving, then checks portrait count after reload. It does not check that a permitted equipment edit persisted on the same reserve pet. The transition steps also omit a forced-dismount and same-pet recovery observation.",
      "blocking_rationale": "The required acceptance record leaves a reader unable to tell which combat filters were exercised or to complete two deferred native checks from the instructions.",
      "blocks_readiness": true,
      "category": "required_gap",
      "expected": "The handoff identifies each essential case\u2019s result and evidence type and gives an observable pass condition for each deferred native behavior.",
      "id": "R2-F7",
      "location": {
        "file": "docs/CITY-AND-PRONE-HANDOFF.md",
        "line": 302
      },
      "priority": "P3",
      "requested_correction": "Restore the case-level distinction between executed target predicates and traced integration filters. Have the owner leave an identifiable item equipped through reopen and reload, verify it on the same reserve pet, and check the chosen toolbar slot through forced dismount and return of the same pet.",
      "requested_evidence": "A corrected acceptance record and owner playtest bound to the reviewed candidate are sufficient. Keep the native results marked NOT RUN.",
      "title": "The handoff omits case-level evidence and two deferred observations",
      "trigger": "Use the current handoff to verify the negative combat controls, persistent equipment and temporary mount unavailability."
    }
  ],
  "prior_finding_assessments": [
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F1",
      "rationale": "ApplyBar tracks one chosen slot and recognizes surviving copies by slot-object identity. The corrected fixtures keep the duplicate present during removal and enforce only 3 when untouched or only 11 after the live edit."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F2",
      "rationale": "Reconcile now returns before departure cleanup on a fenced missing-Player frame. MissingPlayerDuringCanceledLoad covers the original live party shortcut through immediate save and cold restore. R2-F4 concerns an outstanding native write that precedes that guard."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F3",
      "rationale": "A normally completed rebind of the chosen slot clears Pending. NativeRebindOfPendingSlot distinguishes a subsequent player removal from a native emptying through cold restore."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F4",
      "rationale": "The live-bar migration now uses the surviving alias at 9 when Fireball occupies interrupted slot 4. LegacyAliases checks the occupant, the single fallback and its cold restoration. R2-F1 concerns archived rows with an empty fallback destination."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F5",
      "rationale": "The save and disable sanitation sweep follows PersistentPet for every gathered owner, including remote and detached owners also in pair custody. The component and adapter fixtures inspect the separately represented horse."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F6",
      "rationale": "Save sanitation now attempts the stock Charge migration before removing the legacy binding. LegacyChargeEitherOrder covers slot 8 inside serialization, after release and after cold load. The adapter fixture exercises save-first forwarding."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F7",
      "rationale": "ProcessDepartures clears exact managed auto-use before releasing the last observed reference. DepartureClearsAutoUse checks the departed rider, former pet and an ordinary selection."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F8",
      "rationale": "The adapter\u2019s independent strong reference is gone. The real prefix fixture omits the postfix and verifies that menu disposal and disable release the lifecycle-owned operation."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F9",
      "rationale": "RestorePending defers when the bar count is zero. The saved 9 survives an intervening capture and restores once when the same rider\u2019s bar returns. R2-F3 concerns positive evidence of a different companion during that interval."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F10",
      "rationale": "FormerInstanceKeepsReplacement passes once the old object leaves the candidate list. Retained pair custody keeps it on that list and bypasses the same-ID replacement check, allowing it to author the current actor\u2019s record."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F11",
      "rationale": "PetRowsOwnNothing verifies a horse already visible in AllCharacters at admission. The graph can resolve a horse earlier than that lookup, and no later check removes its admitted arrangement when it becomes a current pet."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F12",
      "rationale": "The disabled-save selection is installed and asserted after disable. Both reserve-edit cases assert current live AbilityData at 11 before the boundary and verify the resulting slot counts."
    },
    {
      "assessment": "fix_rejected",
      "finding_id": "R1-F13",
      "rationale": "The area-transfer fixture now keeps its rider in reserve, and the incoming-world fixture continues through seeding, save and return. The non-ability registration case and several other mapped event orderings still omit their original failure conditions."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F14",
      "rationale": "The mutation campaign now compiles the production toolbar adapter partial. Removing real save or native-prefix forwarding, detached or reserve membership, or persistent-pet lookup causes named behavioral failures. The restored baseline passes."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F15",
      "rationale": "Both affected scripts read the extracted toolbar source. The presentation probe passed on the product-equivalent predecessor, and all four affected Finalize2 source predicates hold at H. The outstanding historical r05 input is recorded separately."
    },
    {
      "assessment": "fix_verified",
      "finding_id": "R1-F16",
      "rationale": "The handoff contains the exact required two-sentence status and restores the mounted melee, unmounted ranged, ammunition, range, wall and action-cost controls with observable outcomes. Native combat remains marked NOT RUN."
    }
  ],
  "request_id": "kmc-codex-mounted-city-and-prone-fixes-20261121203639-898f-r02-c1dd22",
  "round": 2,
  "schema_version": "agent-review.review.v1",
  "summary": {
    "delivered_behavior": "The native adapter and offline tests now share the production toolbar lifecycle. The six inherited cases pass, including reserve-pet sanitation, live edits before save, companion retirement, load abandonment and menu disposal. The city roster projection and explicit already-unconscious target change remain stable in this enrollment.",
    "pending_later_phase_work": [],
    "remaining_owner_review": [
      "Verify the chosen shortcut through mounted and dismounted capital arrivals, interiors, forced dismount, saves in both locations and return of the same pet.",
      "Select the exact eligible reserve pet in Inventory and Character. Verify its name, statistics, rename behavior and a permitted equipment change across reopening and cold load.",
      "Compare actual mounted and dismounted longbow rolls against standing, conscious-tripped and already-unconscious enemies at the documented near and normal distances in real time and turn based. Check approach, line of sight, modifiers, roll count, action cost and the mounted melee control."
    ],
    "scope_reviewed": "The complete final mission and toolbar repair addendum for draft PR #57, the feature diff from dbf63702 to b34616ff, all 16 pending findings, all six inherited acceptance cases, focused validation and the native-test deferrals.",
    "unresolved_questions": [
      "The original weapon and the target\u2019s consciousness at the reported click remain unknown. Whether a mounted-specific failure occurs against a conscious prone enemy still requires matched in-game verification."
    ]
  }
}
```
</details>

_Model-generated review by Codex using the requested model, posted by the agent-review controller through the owner's GitHub account. Not a human review or approval._
